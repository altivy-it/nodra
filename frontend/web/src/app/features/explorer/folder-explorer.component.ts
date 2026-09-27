import { Component, computed, ElementRef, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewChild, signal } from '@angular/core';
import { FolderExplorerService, remapPath, VaultEntry } from '../../data-access/folder-explorer.service';

interface TreeRow {
    entry: VaultEntry;
    depth: number;
    index: number;
}

const rowHeight = 30;
const overscan = 8;

@Component({
    selector: 'app-folder-explorer',
    templateUrl: './folder-explorer.component.html',
    styleUrl: './folder-explorer.component.css'
})
export class FolderExplorerComponent implements OnChanges {
    @Input() vaultId: string | null = null;
    @Input() selectedPath: string | null = null;
    @Output() readonly noteOpened = new EventEmitter<string>();
    @Output() readonly folderMoved = new EventEmitter<{ from: string; to: string }>();
    @ViewChild('viewport') private viewport?: ElementRef<HTMLElement>;

    readonly activePath = signal<string | null>(null);
    readonly folderFormOpen = signal(false);
    readonly folderName = signal('');
    readonly creatingFolder = signal(false);
    readonly folderError = signal<string | null>(null);
    readonly confirmingFolderDelete = signal(false);
    readonly deletingFolder = signal(false);
    readonly deleteError = signal<string | null>(null);
    readonly moveFormOpen = signal(false);
    readonly moveTarget = signal('');
    readonly movingFolder = signal(false);
    readonly moveError = signal<string | null>(null);
    readonly scrollTop = signal(0);
    readonly viewportHeight = signal(280);
    readonly rows = computed(() => {
        const visibleRows: TreeRow[] = [];
        this.appendChildren(null, 0, visibleRows);
        return visibleRows;
    });
    readonly activeFolderPath = computed(() => {
        const path = this.activePath() ?? this.selectedPath;
        return this.rows().find(row => row.entry.path === path && row.entry.kind === 'folder')?.entry.path ?? null;
    });
    readonly windowedRows = computed(() => {
        const allRows = this.rows();
        const firstIndex = Math.max(0, Math.floor(this.scrollTop() / rowHeight) - overscan);
        const lastIndex = Math.min(
            allRows.length,
            firstIndex + Math.ceil(this.viewportHeight() / rowHeight) + overscan * 2);
        return {
            rows: allRows.slice(firstIndex, lastIndex),
            top: firstIndex * rowHeight,
            bottom: Math.max(0, (allRows.length - lastIndex) * rowHeight)
        };
    });
    readonly loadingPath = computed(() => [...this.explorer.loadingFolders()].at(-1) ?? null);
    readonly firstError = computed(() => {
        for (const [path, message] of this.explorer.errors()) return { path, message };
        return null;
    });
    get folderParentPath(): string | null {
        const selectedPath = this.activePath() ?? this.selectedPath;
        if (!selectedPath) return null;
        const selectedEntry = this.rows().find(row => row.entry.path === selectedPath)?.entry;
        if (selectedEntry?.kind === 'folder') return selectedPath;
        const separator = selectedPath.lastIndexOf('/');
        return separator < 0 ? null : selectedPath.slice(0, separator);
    }
    get noteParentPath(): string | null { return this.folderParentPath; }

    constructor(
        readonly explorer: FolderExplorerService) { }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['vaultId']) {
            this.activePath.set(null);
            this.scrollTop.set(0);
            this.explorer.activateVault(this.vaultId);
        }
    }

    reload(): void {
        this.activePath.set(null);
        this.scrollTop.set(0);
        this.explorer.reload();
    }

    toggleFolderForm(): void {
        this.folderFormOpen.update(open => !open);
        this.folderName.set('');
        this.folderError.set(null);
    }

    setFolderName(event: Event): void {
        this.folderName.set((event.target as HTMLInputElement).value);
    }

    async submitFolder(event: Event): Promise<void> {
        event.preventDefault();
        const name = this.folderName().trim();
        if (!name || this.creatingFolder()) return;

        this.creatingFolder.set(true);
        this.folderError.set(null);
        try {
            const parentPath = this.folderParentPath;
            const error = await this.explorer.createFolder(parentPath, name);
            if (error) {
                this.folderError.set(error);
                return;
            }

            const path = parentPath ? `${parentPath}/${name}` : name;
            this.folderFormOpen.set(false);
            this.folderName.set('');
            this.activePath.set(path);
            if (parentPath && !this.explorer.expandedFolders().has(parentPath)) {
                await this.explorer.toggleFolder(parentPath);
            }
        } finally {
            this.creatingFolder.set(false);
        }
    }

    async deleteActiveFolder(): Promise<void> {
        const path = this.activeFolderPath();
        if (!path || this.deletingFolder()) return;
        if (!this.confirmingFolderDelete()) {
            this.confirmingFolderDelete.set(true);
            return;
        }

        this.deletingFolder.set(true);
        this.deleteError.set(null);
        try {
            const parentPath = path.includes('/') ? path.slice(0, path.lastIndexOf('/')) : null;
            const error = await this.explorer.deleteEmptyFolder(path);
            if (error) {
                this.deleteError.set(error);
                this.confirmingFolderDelete.set(false);
                return;
            }

            this.confirmingFolderDelete.set(false);
            this.activePath.set(parentPath);
            if (parentPath) {
                const parentIndex = this.rows().findIndex(row => row.entry.path === parentPath);
                if (parentIndex >= 0) this.focusIndex(parentIndex);
            }
        } finally {
            this.deletingFolder.set(false);
        }
    }

    cancelFolderDelete(): void {
        this.confirmingFolderDelete.set(false);
        this.deleteError.set(null);
    }

    openMoveForm(): void {
        const path = this.activeFolderPath();
        if (!path) return;
        this.confirmingFolderDelete.set(false);
        this.deleteError.set(null);
        this.moveTarget.set(path);
        this.moveError.set(null);
        this.moveFormOpen.set(true);
    }

    cancelMove(): void {
        this.moveFormOpen.set(false);
        this.moveError.set(null);
    }

    setMoveTarget(event: Event): void {
        this.moveTarget.set((event.target as HTMLInputElement).value);
    }

    async submitMove(event: Event): Promise<void> {
        event.preventDefault();
        const source = this.activeFolderPath();
        const target = this.moveTarget().trim().replace(/^\/+|\/+$/g, '');
        if (!source || !target || this.movingFolder()) return;
        if (target === source) {
            this.cancelMove();
            return;
        }

        this.movingFolder.set(true);
        this.moveError.set(null);
        try {
            const error = await this.explorer.moveFolder(source, target);
            if (error) {
                this.moveError.set(error);
                return;
            }

            this.moveFormOpen.set(false);
            this.activePath.set(remapPath(this.activePath() ?? source, source, target));
            this.folderMoved.emit({ from: source, to: target });
        } finally {
            this.movingFolder.set(false);
        }
    }

    onScroll(event: Event): void {
        const viewport = event.currentTarget as HTMLElement;
        this.scrollTop.set(viewport.scrollTop);
        this.viewportHeight.set(viewport.clientHeight || 280);
    }

    activate(entry: VaultEntry): void {
        if (entry.path !== this.activePath()) {
            this.confirmingFolderDelete.set(false);
            this.deleteError.set(null);
            this.moveFormOpen.set(false);
            this.moveError.set(null);
        }
        this.activePath.set(entry.path);
        if (entry.kind === 'folder') void this.explorer.toggleFolder(entry.path);
        else this.noteOpened.emit(entry.path);
    }

    handleKeydown(event: KeyboardEvent): void {
        const target = event.target as HTMLElement;
        const currentIndex = Number(target.dataset['rowIndex']);
        const currentRow = this.rows()[currentIndex];
        if (!currentRow) return;

        if (event.key === 'ArrowDown') this.focusIndex(currentIndex + 1);
        else if (event.key === 'ArrowUp') this.focusIndex(currentIndex - 1);
        else if (event.key === 'Home') this.focusIndex(0);
        else if (event.key === 'End') this.focusIndex(this.rows().length - 1);
        else if (event.key === 'ArrowRight' && currentRow.entry.kind === 'folder') {
            if (!this.explorer.expandedFolders().has(currentRow.entry.path)) {
                void this.explorer.toggleFolder(currentRow.entry.path);
            } else if (this.rows()[currentIndex + 1]?.depth > currentRow.depth) {
                this.focusIndex(currentIndex + 1);
            }
        } else if (event.key === 'ArrowLeft') {
            if (currentRow.entry.kind === 'folder' && this.explorer.expandedFolders().has(currentRow.entry.path)) {
                void this.explorer.toggleFolder(currentRow.entry.path);
            } else {
                const parentPath = currentRow.entry.path.split('/').slice(0, -1).join('/');
                const parentIndex = this.rows().findIndex(row => row.entry.path === parentPath);
                if (parentIndex < 0) return;
                this.focusIndex(parentIndex);
            }
        } else {
            return;
        }

        event.preventDefault();
    }

    retry(): void {
        const error = this.firstError();
        if (error) void this.explorer.loadChildren(error.path || null, true);
    }

    private appendChildren(parentPath: string | null, depth: number, rows: TreeRow[]): void {
        const children = this.explorer.children().get(parentPath ?? '') ?? [];
        for (const entry of children) {
            rows.push({ entry, depth, index: rows.length });
            if (entry.kind === 'folder' && this.explorer.expandedFolders().has(entry.path)) {
                this.appendChildren(entry.path, depth + 1, rows);
            }
        }
    }

    private focusIndex(index: number): void {
        const rows = this.rows();
        if (index < 0 || index >= rows.length || !this.viewport) return;
        const viewport = this.viewport.nativeElement;
        const rowTop = index * rowHeight;
        if (rowTop < viewport.scrollTop) viewport.scrollTop = rowTop;
        else if (rowTop + rowHeight > viewport.scrollTop + viewport.clientHeight) {
            viewport.scrollTop = rowTop + rowHeight - viewport.clientHeight;
        }
        this.scrollTop.set(viewport.scrollTop);
        this.activePath.set(rows[index].entry.path);
        queueMicrotask(() => {
            const items = viewport.querySelectorAll<HTMLButtonElement>('[data-row-index]');
            [...items].find(item => Number(item.dataset['rowIndex']) === index)?.focus();
        });
    }
}