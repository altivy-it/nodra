import { DOCUMENT } from '@angular/common';
import { afterNextRender, Component, computed, ElementRef, EventEmitter, HostListener, inject, Injector, Input, OnChanges, Output, SimpleChanges, ViewChild, signal } from '@angular/core';
import { FolderExplorerService, remapPath, TrashEntry, VaultEntry } from '../../data-access/folder-explorer.service';
import { appPath } from '../../core/app-base-path';

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
    private readonly pendingTrashPurgeStorageSlot = 'altivy.trash-purge.pending-v1';
    private readonly document = inject(DOCUMENT);
    private readonly injector = inject(Injector);
    @Input() vaultId: string | null = null;
    @Input() selectedPath: string | null = null;
    @Input() expandedPaths: string[] = [];
    @Output() readonly noteOpened = new EventEmitter<string>();
    @Output() readonly noteCreated = new EventEmitter<string | null>();
    @Output() readonly noteDropped = new EventEmitter<{ note: VaultEntry; destination: string }>();
    @Output() readonly folderMoved = new EventEmitter<{ from: string; to: string }>();
    @Output() readonly expandedPathsChange = new EventEmitter<string[]>();
    @Output() readonly trashRequested = new EventEmitter<VaultEntry>();
    @ViewChild('viewport') private viewport?: ElementRef<HTMLElement>;
    @ViewChild('contextMenu') private contextMenu?: ElementRef<HTMLElement>;
    @ViewChild('folderNameInput') private folderNameInput?: ElementRef<HTMLInputElement>;
    @ViewChild('movePathInput') private movePathInput?: ElementRef<HTMLInputElement>;
    @ViewChild('confirmDeleteButton') private confirmDeleteButton?: ElementRef<HTMLButtonElement>;
    @ViewChild('restorePathInput') private restorePathInput?: ElementRef<HTMLInputElement>;
    private pendingFocusIndex: number | null = null;

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
    readonly draggedEntry = signal<VaultEntry | null>(null);
    readonly dropTarget = signal<string | null>(null);
    readonly contextEntry = signal<VaultEntry | null>(null);
    readonly contextPosition = signal({ left: 0, top: 0 });
    readonly trashOpen = signal(false);
    readonly restoreEntryId = signal<string | null>(null);
    readonly restoreTarget = signal('');
    readonly restoreError = signal<string | null>(null);
    readonly restoringEntry = signal(false);
    readonly confirmingTrashPurge = signal(false);
    readonly purgingTrash = signal(false);
    readonly purgeError = signal<string | null>(null);
    readonly purgeResult = signal<string | null>(null);
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
            this.closeContextMenu();
            this.activePath.set(null);
            this.scrollTop.set(0);
            this.trashOpen.set(false);
            this.endFolderDrag();
            this.explorer.activateVault(this.vaultId, this.expandedPaths);
            void this.resumePendingTrashPurge();
        }
    }

    reload(): void {
        this.closeContextMenu();
        this.explorer.reload();
    }

    entryAccessibilityLabel(entry: VaultEntry): string | null {
        const statuses: string[] = [];
        if (this.explorer.pendingFolders().has(entry.path)) statuses.push('criando');
        if (this.explorer.movingEntries().has(entry.path)) statuses.push('movendo');
        if (entry.kind === 'note' && entry.isUnclassified) statuses.push('sem classificação');
        if (entry.kind === 'note' && entry.hasMinimumMetadata === false) statuses.push('metadados mínimos ausentes');
        if (entry.kind === 'note' && entry.hasUnresolvedLinks) statuses.push('link não resolvido');
        return statuses.length > 0 ? `${entry.name} (${statuses.join(', ')})` : null;
    }

    openTrash(): void {
        this.closeContextMenu();
        this.trashOpen.set(true);
        this.restoreEntryId.set(null);
        this.restoreError.set(null);
        this.confirmingTrashPurge.set(false);
        this.purgeError.set(null);
        this.purgeResult.set(null);
        void this.explorer.loadTrash();
    }

    closeTrash(): void {
        this.trashOpen.set(false);
        this.restoreEntryId.set(null);
        this.restoreError.set(null);
        this.confirmingTrashPurge.set(false);
        this.purgeError.set(null);
        this.focusActiveRow();
    }

    refreshTrash(): void {
        this.purgeError.set(null);
        void this.explorer.loadTrash();
    }

    async emptyTrash(): Promise<void> {
        if (this.explorer.trashEntries().length === 0 || this.purgingTrash()) return;
        if (!this.confirmingTrashPurge()) {
            this.confirmingTrashPurge.set(true);
            this.purgeError.set(null);
            return;
        }

        this.purgingTrash.set(true);
        this.purgeError.set(null);
        try {
            const error = await this.explorer.emptyTrash();
            if (error === 'reauthentication-required') {
                const returnUrl = `${this.document.location.pathname}${this.document.location.search}${this.document.location.hash}`;
                try {
                    const storage = this.document.defaultView?.sessionStorage;
                    if (!storage || !this.vaultId) throw new Error('Armazenamento temporário indisponível.');
                    storage.setItem(this.pendingTrashPurgeStorageSlot, this.vaultId);
                } catch {
                    this.purgeError.set('Não foi possível preservar a confirmação. Reabra a lixeira e tente novamente após autenticar.');
                    return;
                }
                this.document.location.href = appPath(
                    `/bff/reauthenticate?returnUrl=${encodeURIComponent(returnUrl)}`,
                    this.document.baseURI);
                return;
            }
            if (error) {
                this.purgeError.set(error);
                return;
            }
            this.confirmingTrashPurge.set(false);
            this.purgeResult.set('Lixeira esvaziada.');
        } finally {
            this.purgingTrash.set(false);
        }
    }

    private async resumePendingTrashPurge(): Promise<void> {
        const storage = this.document.defaultView?.sessionStorage;
        if (!storage) return;
        let pendingVaultId: string | null;
        try {
            pendingVaultId = storage.getItem(this.pendingTrashPurgeStorageSlot);
            if (pendingVaultId) storage.removeItem(this.pendingTrashPurgeStorageSlot);
        } catch {
            return;
        }
        if (!pendingVaultId || pendingVaultId !== this.vaultId) return;

        this.trashOpen.set(true);
        this.confirmingTrashPurge.set(true);
        this.purgeError.set(null);
        await this.explorer.loadTrash();
        if (pendingVaultId !== this.vaultId) return;
        if (this.explorer.trashError() || !(await this.explorer.hasRecentReauthentication())) {
            this.confirmingTrashPurge.set(false);
            this.purgeError.set('A reautenticação recente não foi confirmada. Confirme o esvaziamento novamente.');
            return;
        }
        await this.emptyTrash();
    }

    cancelTrashPurge(): void {
        this.confirmingTrashPurge.set(false);
        this.purgeError.set(null);
    }

    beginRestore(entry: TrashEntry): void {
        this.restoreEntryId.set(entry.id);
        this.restoreTarget.set(entry.canRestoreToOriginal ? entry.path : '');
        this.restoreError.set(entry.canRestoreToOriginal ? null : 'O caminho original está ocupado. Informe outro destino.');
        afterNextRender(() => this.restorePathInput?.nativeElement.focus(), { injector: this.injector });
    }

    setRestoreTarget(event: Event): void {
        this.restoreTarget.set((event.target as HTMLInputElement).value);
    }

    async submitRestore(event: Event, entry: TrashEntry): Promise<void> {
        event.preventDefault();
        const target = this.restoreTarget().trim();
        if (!target || this.restoringEntry()) return;
        this.restoringEntry.set(true);
        this.restoreError.set(null);
        try {
            const error = await this.explorer.restoreFromTrash(entry, target);
            if (error) {
                this.restoreError.set(error);
                return;
            }
            this.restoreEntryId.set(null);
            this.restoreTarget.set('');
        } finally {
            this.restoringEntry.set(false);
        }
    }

    toggleFolderForm(): void {
        const opening = !this.folderFormOpen();
        this.folderFormOpen.set(opening);
        this.folderName.set('');
        this.folderError.set(null);
        if (opening) afterNextRender(() => this.folderNameInput?.nativeElement.focus(), { injector: this.injector });
        else this.focusActiveRow();
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
                await this.toggleFolder(parentPath);
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
            afterNextRender(() => this.confirmDeleteButton?.nativeElement.focus(), { injector: this.injector });
            return;
        }

        this.deletingFolder.set(true);
        this.deleteError.set(null);
        try {
            const parentPath = path.includes('/') ? path.slice(0, path.lastIndexOf('/')) : null;
            const entry = this.rows().find(row => row.entry.path === path)?.entry;
            const error = entry ? await this.explorer.moveToTrash(entry) : 'A pasta não está mais disponível.';
            if (error) {
                this.deleteError.set(error);
                this.confirmingFolderDelete.set(false);
                this.focusActiveRow();
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
        this.focusActiveRow();
    }

    openMoveForm(): void {
        const path = this.activeFolderPath();
        if (!path) return;
        this.confirmingFolderDelete.set(false);
        this.deleteError.set(null);
        this.moveTarget.set(path);
        this.moveError.set(null);
        this.moveFormOpen.set(true);
        afterNextRender(() => this.movePathInput?.nativeElement.focus(), { injector: this.injector });
    }

    cancelMove(): void {
        this.moveFormOpen.set(false);
        this.moveError.set(null);
        this.focusActiveRow();
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
            this.expandedPathsChange.emit([...this.explorer.expandedFolders()]);
        } finally {
            this.movingFolder.set(false);
        }
    }

    startEntryDrag(entry: VaultEntry, event: DragEvent): void {
        if (!event.dataTransfer || (entry.kind === 'note' && !entry.eTag)) return;
        this.draggedEntry.set(entry);
        event.dataTransfer.effectAllowed = 'move';
        event.dataTransfer.setData('application/x-altivy-entry', 'move');
    }

    allowFolderDrop(entry: VaultEntry, event: DragEvent): void {
        const source = this.draggedEntry();
        if (entry.kind !== 'folder' || !source || entry.path === source.path ||
            (source.kind === 'folder' && entry.path.startsWith(`${source.path}/`)) ||
            source.path.slice(0, source.path.lastIndexOf('/')) === entry.path) {
            this.dropTarget.set(null);
            return;
        }
        event.preventDefault();
        if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
        this.dropTarget.set(entry.path);
    }

    leaveFolderDrop(entry: VaultEntry, event: DragEvent): void {
        if (this.dropTarget() === entry.path &&
            !(event.currentTarget as HTMLElement).contains(event.relatedTarget as Node | null)) {
            this.dropTarget.set(null);
        }
    }

    endFolderDrag(): void {
        this.draggedEntry.set(null);
        this.dropTarget.set(null);
    }

    async dropFolder(entry: VaultEntry, event: DragEvent): Promise<void> {
        const source = this.draggedEntry();
        const target = this.dropTarget();
        this.endFolderDrag();
        if (!source || entry.kind !== 'folder' || target !== entry.path) return;
        event.preventDefault();
        const destination = `${entry.path}/${source.path.split('/').at(-1)}`;
        if (source.kind === 'note') {
            if (this.explorer.expandedFolders().has(entry.path)) {
                await this.explorer.loadChildren(entry.path);
            } else {
                await this.toggleFolder(entry.path);
            }
            this.noteDropped.emit({ note: source, destination });
            return;
        }
        this.moveError.set(null);
        this.movingFolder.set(true);
        try {
            const error = await this.explorer.moveFolder(source.path, destination);
            if (error) {
                this.moveError.set(error);
                this.activePath.set(source.path);
                return;
            }
            this.activePath.set(destination);
            this.folderMoved.emit({ from: source.path, to: destination });
        } finally {
            this.movingFolder.set(false);
        }
    }

    onScroll(event: Event): void {
        const viewport = event.currentTarget as HTMLElement;
        this.scrollTop.set(viewport.scrollTop);
        this.viewportHeight.set(viewport.clientHeight || 280);
        if (this.pendingFocusIndex !== null) {
            afterNextRender(() => this.focusPendingRow(), { injector: this.injector });
        }
    }

    activate(entry: VaultEntry): void {
        this.pendingFocusIndex = null;
        this.closeContextMenu();
        if (entry.path !== this.activePath()) {
            this.confirmingFolderDelete.set(false);
            this.deleteError.set(null);
            this.moveFormOpen.set(false);
            this.moveError.set(null);
        }
        this.activePath.set(entry.path);
        if (entry.kind === 'folder') void this.toggleFolder(entry.path);
        else this.noteOpened.emit(entry.path);
    }

    openContextMenu(entry: VaultEntry, event: MouseEvent | KeyboardEvent): void {
        event.preventDefault();
        this.activePath.set(entry.path);
        const host = (event.currentTarget as HTMLElement).getBoundingClientRect();
        const x = event instanceof MouseEvent ? event.clientX : host.right;
        const y = event instanceof MouseEvent ? event.clientY : host.top;
        this.contextPosition.set({
            left: Math.max(0, Math.min(x, window.innerWidth - 220)),
            top: Math.max(0, Math.min(y, window.innerHeight - 180))
        });
        this.contextEntry.set(entry);
        afterNextRender(() => this.contextMenu?.nativeElement.querySelector('button')?.focus(), { injector: this.injector });
    }

    closeContextMenu(restoreFocus = false): void {
        const entry = this.contextEntry();
        this.contextEntry.set(null);
        if (restoreFocus && entry) {
            afterNextRender(() => {
                const row = this.rows().find(item => item.entry.path === entry.path);
                if (row) this.viewport?.nativeElement.querySelector<HTMLButtonElement>(`[data-row-index="${row.index}"]`)?.focus();
            }, { injector: this.injector });
        }
    }

    chooseContextAction(action: 'open' | 'new-note' | 'new-folder' | 'move' | 'delete' | 'trash'): void {
        const entry = this.contextEntry();
        if (!entry) return;
        this.closeContextMenu();
        if (action === 'open') {
            this.noteOpened.emit(entry.path);
            this.focusActiveRow();
        } else if (action === 'new-note') {
            this.noteCreated.emit(this.folderParentPath);
            this.focusActiveRow();
        } else if (action === 'new-folder') {
            if (!this.folderFormOpen()) this.toggleFolderForm();
        } else if (action === 'trash') {
            this.trashRequested.emit(entry);
        } else if (entry.kind === 'folder' && action === 'move') {
            this.openMoveForm();
        } else if (entry.kind === 'folder' && action === 'delete') {
            void this.deleteActiveFolder();
        }
    }

    handleContextKeydown(event: KeyboardEvent): void {
        if (event.key === 'Escape') {
            event.preventDefault();
            this.closeContextMenu(true);
        } else if (['ArrowDown', 'ArrowUp', 'Home', 'End', 'Tab'].includes(event.key)) {
            event.preventDefault();
            const buttons = [...(this.contextMenu?.nativeElement.querySelectorAll<HTMLButtonElement>('button') ?? [])];
            const index = Math.max(0, buttons.indexOf(document.activeElement as HTMLButtonElement));
            const direction = event.key === 'ArrowUp' || (event.key === 'Tab' && event.shiftKey) ? -1 : 1;
            const next = event.key === 'Home' ? 0 : event.key === 'End' ? buttons.length - 1 :
                (index + direction + buttons.length) % buttons.length;
            buttons[next]?.focus();
        }
    }

    @HostListener('document:pointerdown', ['$event'])
    dismissContextOutside(event: PointerEvent): void {
        if (this.contextEntry() && !this.contextMenu?.nativeElement.contains(event.target as Node)) this.closeContextMenu();
    }

    private focusActiveRow(): void {
        const row = this.rows().find(item => item.entry.path === this.activePath());
        if (row) afterNextRender(() => this.viewport?.nativeElement.querySelector<HTMLButtonElement>(`[data-row-index="${row.index}"]`)?.focus(), { injector: this.injector });
    }

    handleKeydown(event: KeyboardEvent): void {
        const target = event.target as HTMLElement;
        const currentIndex = Number(target.dataset['rowIndex']);
        const currentRow = this.rows()[currentIndex];
        if (!currentRow) return;

        if (event.key === 'ContextMenu' || (event.key === 'F10' && event.shiftKey)) {
            this.openContextMenu(currentRow.entry, event);
            return;
        }

        if (event.key === 'ArrowDown') this.focusIndex(currentIndex + 1);
        else if (event.key === 'ArrowUp') this.focusIndex(currentIndex - 1);
        else if (event.key === 'Home') this.focusIndex(0);
        else if (event.key === 'End') this.focusIndex(this.rows().length - 1);
        else if (event.key === 'ArrowRight' && currentRow.entry.kind === 'folder') {
            if (!this.explorer.expandedFolders().has(currentRow.entry.path)) {
                void this.toggleFolder(currentRow.entry.path);
            } else if (this.rows()[currentIndex + 1]?.depth > currentRow.depth) {
                this.focusIndex(currentIndex + 1);
            }
        } else if (event.key === 'ArrowLeft') {
            if (currentRow.entry.kind === 'folder' && this.explorer.expandedFolders().has(currentRow.entry.path)) {
                void this.toggleFolder(currentRow.entry.path);
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
        this.pendingFocusIndex = index;
        afterNextRender(() => this.focusPendingRow(), { injector: this.injector });
    }

    private focusPendingRow(): void {
        const index = this.pendingFocusIndex;
        if (index === null) return;
        const row = this.viewport?.nativeElement.querySelector<HTMLButtonElement>(`[data-row-index="${index}"]`);
        row?.focus();
        if (row && this.viewport?.nativeElement.scrollTop === this.scrollTop()) this.pendingFocusIndex = null;
    }

    private async toggleFolder(path: string): Promise<void> {
        await this.explorer.toggleFolder(path);
        this.expandedPathsChange.emit([...this.explorer.expandedFolders()]);
    }
}
