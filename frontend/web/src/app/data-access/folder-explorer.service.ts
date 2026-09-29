import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface VaultEntry {
    path: string;
    name: string;
    kind: 'folder' | 'note';
    eTag: string | null;
}

export interface TrashEntry {
    id: string;
    path: string;
    name: string;
    kind: 'folder' | 'note';
    deletedAtUtc: string;
    retentionDays: number | null;
    canRestoreToOriginal: boolean;
}

@Injectable({ providedIn: 'root' })
export class FolderExplorerService {
    readonly children = signal<Map<string, VaultEntry[]>>(new Map());
    readonly expandedFolders = signal<Set<string>>(new Set());
    readonly loadingFolders = signal<Set<string>>(new Set());
    readonly errors = signal<Map<string, string>>(new Map());
    readonly pendingFolders = signal<Set<string>>(new Set());
    readonly movingEntries = signal<Set<string>>(new Set());
    readonly trashEntries = signal<TrashEntry[]>([]);
    readonly trashLoading = signal(false);
    readonly trashError = signal<string | null>(null);
    private readonly pendingEntries = new Map<string, VaultEntry>();
    private readonly pendingDeletions = new Map<string, VaultEntry>();
    private readonly pendingMoves = new Map<string, {
        targetPath: string;
        sourceParent: string;
        targetParent: string;
        kind: VaultEntry['kind'];
        sourceEntry: VaultEntry;
        targetEntry: VaultEntry;
    }>();
    private readonly pendingTrashMoves = new Map<string, {
        parentKey: string;
        entry: VaultEntry;
        children: Map<string, VaultEntry[]>;
        expanded: Set<string>;
    }>();
    private readonly requestVersions = new Map<string, number>();
    private vaultId: string | null = null;
    private generation = 0;

    constructor(private readonly http: HttpClient) { }

    async moveToTrash(entry: VaultEntry): Promise<string | null> {
        const vaultId = this.vaultId;
        const generation = this.generation;
        if (!vaultId) return 'Nenhum vault ativo.';
        if (entry.kind === 'note' && !entry.eTag) return 'Salve a nota antes de movê-la para a lixeira.';
        if (this.pendingTrashMoves.has(entry.path)) return 'Esta entrada já está sendo movida.';

        const parentKey = entry.path.includes('/') ? entry.path.slice(0, entry.path.lastIndexOf('/')) : '';
        const source = this.children().get(parentKey)?.find(item => item.path === entry.path);
        if (source) {
            const childCache = new Map<string, VaultEntry[]>();
            const expanded = new Set<string>();
            this.children.update(children => {
                const next = new Map(children);
                for (const [path, entries] of children) {
                    if (path === entry.path || path.startsWith(`${entry.path}/`)) {
                        childCache.set(path, entries);
                        next.delete(path);
                    }
                }
                next.set(parentKey, (next.get(parentKey) ?? []).filter(item => item.path !== entry.path));
                return next;
            });
            this.expandedFolders.update(paths => {
                const next = new Set<string>();
                for (const path of paths) {
                    if (path === entry.path || path.startsWith(`${entry.path}/`)) expanded.add(path);
                    else next.add(path);
                }
                return next;
            });
            this.pendingTrashMoves.set(entry.path, { parentKey, entry: source, children: childCache, expanded });
            this.movingEntries.update(paths => new Set(paths).add(entry.path));
        }

        const encodedPath = entry.path.split('/').map(segment => encodeURIComponent(segment)).join('/');
        try {
            if (entry.kind === 'note') {
                await firstValueFrom(this.http.delete<void>(
                    `/api/v1/vaults/${encodeURIComponent(vaultId)}/notes/${encodedPath}`,
                    { headers: { 'If-Match': `"${entry.eTag}"` } }));
            } else {
                await firstValueFrom(this.http.delete<void>(
                    `/api/v1/vaults/${encodeURIComponent(vaultId)}/folders/${encodedPath}`));
            }
            if (generation !== this.generation || vaultId !== this.vaultId) return 'O vault ativo mudou.';
            this.finishTrashMove(entry.path);
            this.reload();
            return null;
        } catch (error) {
            if (generation === this.generation && vaultId === this.vaultId) this.rollbackTrashMove(entry.path);
            if (error instanceof HttpErrorResponse && error.status === 409) {
                return entry.kind === 'note'
                    ? 'A nota foi alterada por outra sessão. Recarregue antes de movê-la.'
                    : 'Não foi possível mover a pasta. Recarregue e tente novamente.';
            }
            if (error instanceof HttpErrorResponse && error.status === 403) return 'Você não tem permissão para mover esta entrada.';
            return 'Não foi possível mover esta entrada para a lixeira.';
        }
    }

    async loadTrash(): Promise<void> {
        const vaultId = this.vaultId;
        const generation = this.generation;
        if (!vaultId) return;
        this.trashLoading.set(true);
        this.trashError.set(null);
        try {
            const collected: TrashEntry[] = [];
            let cursor: string | null = null;
            do {
                let params = new HttpParams().set('limit', 100);
                if (cursor) params = params.set('cursor', cursor);
                const page = await firstValueFrom(this.http.get<{ items: TrashEntry[]; nextCursor: string | null }>(
                    `/api/v1/vaults/${encodeURIComponent(vaultId)}/trash`, { params }));
                collected.push(...page.items);
                cursor = page.nextCursor;
            } while (cursor && generation === this.generation && vaultId === this.vaultId);
            if (generation === this.generation && vaultId === this.vaultId) this.trashEntries.set(collected);
        } catch (error) {
            if (generation === this.generation && vaultId === this.vaultId) {
                const status = error instanceof HttpErrorResponse ? error.status : null;
                this.trashError.set(status === 403
                    ? 'Você não tem permissão para acessar a lixeira deste vault.'
                    : 'Não foi possível carregar a lixeira.');
            }
        } finally {
            if (generation === this.generation && vaultId === this.vaultId) this.trashLoading.set(false);
        }
    }

    async restoreFromTrash(entry: TrashEntry, destination: string): Promise<string | null> {
        const vaultId = this.vaultId;
        const generation = this.generation;
        if (!vaultId) return 'Nenhum vault ativo.';
        const target = destination.trim().replace(/^\/+|\/+$/g, '');
        if (!target) return 'Informe o destino de restauração.';
        try {
            await firstValueFrom(this.http.patch<void>(
                `/api/v1/vaults/${encodeURIComponent(vaultId)}/trash/${encodeURIComponent(entry.id)}`,
                { path: target }));
            if (generation !== this.generation || vaultId !== this.vaultId) return 'O vault ativo mudou.';
            this.trashEntries.update(entries => entries.filter(item => item.id !== entry.id));
            this.reload();
            return null;
        } catch (error) {
            if (error instanceof HttpErrorResponse && error.status === 409) {
                const code = error.error?.code;
                return code === 'trash-destination-folder-missing'
                    ? 'A pasta de destino não existe. Escolha outro destino.'
                    : 'O destino está ocupado. Escolha outro destino para restaurar.';
            }
            if (error instanceof HttpErrorResponse && error.status === 403) return 'Você não tem permissão para restaurar esta entrada.';
            return 'Não foi possível restaurar esta entrada.';
        }
    }

    async emptyTrash(): Promise<string | null> {
        const vaultId = this.vaultId;
        const generation = this.generation;
        if (!vaultId) return 'Nenhum vault ativo.';
        try {
            await firstValueFrom(this.http.delete<{ removedOperations: number; failedOperations: number }>(
                `/api/v1/vaults/${encodeURIComponent(vaultId)}/trash`));
            if (generation !== this.generation || vaultId !== this.vaultId) return 'O vault ativo mudou.';
            this.trashEntries.set([]);
            this.reload();
            return null;
        } catch (error) {
            if (error instanceof HttpErrorResponse && error.status === 428) return 'reauthentication-required';
            if (error instanceof HttpErrorResponse && error.status === 403) return 'Você não tem permissão para esvaziar a lixeira.';
            return 'Não foi possível esvaziar a lixeira.';
        }
    }

    activateVault(vaultId: string | null, expandedPaths: string[] = []): void {
        if (this.vaultId === vaultId) return;
        this.vaultId = vaultId;
        this.generation++;
        this.children.set(new Map());
        this.expandedFolders.set(new Set());
        this.loadingFolders.set(new Set());
        this.errors.set(new Map());
        this.pendingFolders.set(new Set());
        this.movingEntries.set(new Set());
        this.pendingEntries.clear();
        this.pendingDeletions.clear();
        this.pendingMoves.clear();
        this.pendingTrashMoves.clear();
        this.requestVersions.clear();
        this.trashEntries.set([]);
        this.trashLoading.set(false);
        this.trashError.set(null);
        if (vaultId) void this.loadChildren(null).then(() => this.restoreExpandedFolders(expandedPaths));
    }

    private async restoreExpandedFolders(paths: string[]): Promise<void> {
        if (!this.children().has('') || this.errors().has('')) return;
        const generation = this.generation;
        const orderedPaths = [...new Set(paths)].sort((left, right) =>
            left.split('/').length - right.split('/').length);
        for (const path of orderedPaths) {
            if (generation !== this.generation) return;
            let currentPath = '';
            for (const segment of path.split('/')) {
                currentPath = currentPath ? `${currentPath}/${segment}` : segment;
                const parentPath = currentPath.includes('/') ? currentPath.slice(0, currentPath.lastIndexOf('/')) : '';
                const folderExists = this.children().get(parentPath)?.some(entry =>
                    entry.path === currentPath && entry.kind === 'folder');
                if (!folderExists) break;
                this.expandedFolders.update(expanded => new Set(expanded).add(currentPath));
                await this.loadChildren(currentPath);
                if (this.errors().has(currentPath)) break;
            }
        }
    }

    async loadChildren(parentPath: string | null, force = false): Promise<void> {
        const vaultId = this.vaultId;
        const generation = this.generation;
        const key = parentPath ?? '';
        if (!vaultId || (!force && this.children().has(key))) return;
        const requestVersion = (this.requestVersions.get(key) ?? 0) + 1;
        this.requestVersions.set(key, requestVersion);
        const isCurrent = () => generation === this.generation && vaultId === this.vaultId &&
            requestVersion === this.requestVersions.get(key);

        this.loadingFolders.update(paths => new Set(paths).add(key));
        this.errors.update(errors => {
            const nextErrors = new Map(errors);
            nextErrors.delete(key);
            return nextErrors;
        });

        try {
            const options = parentPath === null ? {} : { params: new HttpParams().set('parentPath', parentPath) };
            const entries = await firstValueFrom(this.http.get<VaultEntry[]>(
                `/api/v1/vaults/${encodeURIComponent(vaultId)}/entries`, options));
            if (!isCurrent()) return;
            const visible = entries.filter(entry => !this.pendingDeletions.has(entry.path) &&
                !this.pendingMoves.has(entry.path));
            for (const [path, provisional] of this.pendingEntries) {
                const separator = path.lastIndexOf('/');
                const parent = separator < 0 ? '' : path.slice(0, separator);
                if (parent === key && !visible.some(entry => entry.path === path)) visible.push(provisional);
            }
            for (const move of this.pendingMoves.values()) {
                const parent = move.targetPath.includes('/')
                    ? move.targetPath.slice(0, move.targetPath.lastIndexOf('/')) : '';
                if (parent === key && !visible.some(entry => entry.path === move.targetPath)) visible.push(move.targetEntry);
            }
            if (visible.length !== entries.length) visible.sort((left, right) => left.name.localeCompare(right.name));
            this.children.update(children => new Map(children).set(key, visible));
        } catch (error) {
            if (!isCurrent()) return;
            const status = error instanceof HttpErrorResponse ? error.status : null;
            if (status === 401 || status === 403) {
                if (!key) {
                    this.generation++;
                    this.loadingFolders.set(new Set());
                }
                const denied = (path: string) => !key || path === key || path.startsWith(`${key}/`);
                for (const path of this.pendingEntries.keys()) {
                    if (denied(path)) this.pendingEntries.delete(path);
                }
                for (const path of this.pendingDeletions.keys()) {
                    if (denied(path)) this.pendingDeletions.delete(path);
                }
                for (const [path, move] of this.pendingMoves) {
                    if (denied(path)) {
                        this.pendingMoves.delete(path);
                        this.movingEntries.update(paths => {
                            const next = new Set(paths);
                            next.delete(move.targetPath);
                            return next;
                        });
                    }
                }
                this.pendingFolders.update(paths => new Set([...paths].filter(path => !denied(path))));
                this.children.update(children => {
                    const visible = new Map(children);
                    for (const path of visible.keys()) {
                        if (!key || path === key || path.startsWith(`${key}/`)) visible.delete(path);
                    }
                    return visible;
                });
            }
            const message = status === 401 ? 'A sessão expirou. Entre novamente para acessar esta pasta.'
                : status === 403 ? 'Você não tem permissão para acessar esta pasta.'
                    : status === 0 ? 'Sem conexão. Não foi possível atualizar esta pasta.'
                        : 'Não foi possível carregar esta pasta.';
            this.errors.update(errors => {
                const visible = new Map(errors);
                if (status === 401 || status === 403) {
                    for (const path of visible.keys()) {
                        if (!key || path.startsWith(`${key}/`)) visible.delete(path);
                    }
                }
                visible.set(key, message);
                return visible;
            });
        } finally {
            if (isCurrent()) {
                this.loadingFolders.update(paths => {
                    const nextPaths = new Set(paths);
                    nextPaths.delete(key);
                    return nextPaths;
                });
            }
        }
    }

    async toggleFolder(path: string): Promise<void> {
        if (this.expandedFolders().has(path)) {
            this.expandedFolders.update(paths => {
                const nextPaths = new Set(paths);
                nextPaths.delete(path);
                return nextPaths;
            });
            return;
        }

        this.expandedFolders.update(paths => new Set(paths).add(path));
        await this.loadChildren(path);
    }

    /** Cria a pasta; retorna `null` em sucesso ou a mensagem de erro da ação. */
    async createFolder(parentPath: string | null, name: string): Promise<string | null> {
        const vaultId = this.vaultId;
        const generation = this.generation;
        const folderName = name.trim();
        if (!vaultId || !folderName) return 'Informe o nome da pasta.';

        const path = parentPath ? `${parentPath}/${folderName}` : folderName;
        const parentKey = parentPath ?? '';
        if (this.pendingDeletions.has(path)) return 'Aguarde a exclusão desta pasta.';
        if (this.pendingFolders().has(path)) return 'Esta pasta já está sendo criada.';
        const siblings = this.children().get(parentKey);
        const provisional: VaultEntry = { path, name: folderName, kind: 'folder', eTag: null };
        const previewed = !!siblings && !siblings.some(entry => entry.path === path);
        if (previewed) {
            this.children.update(children => new Map(children).set(parentKey,
                [...(children.get(parentKey) ?? []), provisional].sort((left, right) => left.name.localeCompare(right.name))));
            this.pendingEntries.set(path, provisional);
            this.pendingFolders.update(paths => new Set(paths).add(path));
        }
        const encodedPath = path.split('/').map(segment => encodeURIComponent(segment)).join('/');
        try {
            await firstValueFrom(this.http.put<void>(
                `/api/v1/vaults/${encodeURIComponent(vaultId)}/folders/${encodedPath}`,
                null));
            if (generation !== this.generation || vaultId !== this.vaultId) return 'O vault ativo mudou.';
            await this.loadChildren(parentPath, true);
            return null;
        } catch (error) {
            if (previewed && generation === this.generation && vaultId === this.vaultId) {
                this.children.update(children => new Map(children).set(parentKey,
                    (children.get(parentKey) ?? []).filter(entry => entry !== provisional)));
            }
            return error instanceof HttpErrorResponse && error.status === 409
                ? 'Já existe um arquivo com esse nome neste destino.'
                : 'Não foi possível criar a pasta. Verifique o nome, o destino e suas permissões.';
        } finally {
            if (previewed && generation === this.generation && vaultId === this.vaultId) {
                this.pendingEntries.delete(path);
                this.pendingFolders.update(paths => {
                    const next = new Set(paths);
                    next.delete(path);
                    return next;
                });
            }
        }
    }

    /** Exclui a pasta vazia; retorna `null` em sucesso ou a mensagem de erro da ação. */
    async deleteEmptyFolder(path: string): Promise<string | null> {
        const vaultId = this.vaultId;
        const generation = this.generation;
        if (!vaultId) return 'Nenhum vault ativo.';

        const segments = path.split('/');
        const parentPath = segments.length > 1 ? segments.slice(0, -1).join('/') : null;
        const parentKey = parentPath ?? '';
        if (this.pendingDeletions.has(path)) return 'A exclusão desta pasta já está em andamento.';
        if (this.pendingFolders().has(path)) return 'Aguarde a criação desta pasta.';
        const target = this.children().get(parentKey)?.find(entry => entry.path === path && entry.kind === 'folder');
        if (target) {
            this.pendingDeletions.set(path, target);
            this.children.update(children => new Map(children).set(parentKey,
                (children.get(parentKey) ?? []).filter(entry => entry !== target)));
        }
        const encodedPath = segments.map(segment => encodeURIComponent(segment)).join('/');
        try {
            await firstValueFrom(this.http.delete<void>(
                `/api/v1/vaults/${encodeURIComponent(vaultId)}/folders/${encodedPath}`));
            if (generation !== this.generation || vaultId !== this.vaultId) return 'O vault ativo mudou.';
            await this.loadChildren(parentPath, true);
            return null;
        } catch (error) {
            if (target && generation === this.generation && vaultId === this.vaultId && this.children().has(parentKey)) {
                this.children.update(children => {
                    const siblings = children.get(parentKey) ?? [];
                    return siblings.some(entry => entry.path === path) ? children : new Map(children).set(parentKey,
                        [...siblings, target].sort((left, right) => left.name.localeCompare(right.name)));
                });
            }
            return error instanceof HttpErrorResponse && error.status === 409
                ? 'A pasta contém arquivos ou subpastas e não pode ser excluída.'
                : 'Não foi possível excluir esta pasta.';
        } finally {
            if (target && generation === this.generation && vaultId === this.vaultId) {
                this.pendingDeletions.delete(path);
            }
        }
    }

    /** Move ou renomeia a pasta; retorna `null` em sucesso ou a mensagem de erro da ação. */
    async moveFolder(source: string, destination: string): Promise<string | null> {
        const vaultId = this.vaultId;
        const generation = this.generation;
        const target = destination.trim().replace(/^\/+|\/+$/g, '');
        if (!vaultId) return 'Nenhum vault ativo.';
        if (!target) return 'Informe o novo caminho da pasta.';
        if (target === source) return null;
        if (target.startsWith(`${source}/`)) return 'A pasta não pode ser movida para dentro dela mesma.';

        const sourceParent = source.includes('/') ? source.slice(0, source.lastIndexOf('/')) : '';
        const targetParent = target.includes('/') ? target.slice(0, target.lastIndexOf('/')) : '';
        const sourceEntry = this.children().get(sourceParent)?.find(entry => entry.path === source && entry.kind === 'folder');
        const targetChildren = this.children().get(targetParent);
        const previewed = !!sourceEntry && !!targetChildren && !targetChildren.some(entry => entry.path === target);
        if (previewed && sourceEntry) {
            const targetEntry = { ...sourceEntry, path: target, name: target.slice(target.lastIndexOf('/') + 1) };
            this.pendingMoves.set(source, { targetPath: target, sourceParent, targetParent, kind: 'folder', sourceEntry, targetEntry });
            this.movingEntries.update(paths => new Set(paths).add(target));
            this.children.update(children => {
                const next = new Map(children);
                if (sourceParent === targetParent) {
                    next.set(sourceParent, (next.get(sourceParent) ?? []).filter(entry => entry.path !== source)
                        .concat(targetEntry).sort((left, right) => left.name.localeCompare(right.name)));
                } else {
                    next.set(sourceParent, (next.get(sourceParent) ?? []).filter(entry => entry.path !== source));
                    next.set(targetParent, [...(next.get(targetParent) ?? []), targetEntry]
                        .sort((left, right) => left.name.localeCompare(right.name)));
                }
                return next;
            });
        }

        const encodedPath = source.split('/').map(segment => encodeURIComponent(segment)).join('/');
        try {
            await firstValueFrom(this.http.patch<void>(
                `/api/v1/vaults/${encodeURIComponent(vaultId)}/folders/${encodedPath}`,
                { path: target }));
        } catch (error) {
            if (previewed) this.rollbackFolderMove(source);
            if (!(error instanceof HttpErrorResponse)) return 'Não foi possível mover a pasta.';
            if (error.status === 409) {
                return error.error?.code === 'folder-destination-folder-missing'
                    ? 'A pasta de destino não existe.'
                    : 'Já existe um item com esse caminho.';
            }
            if (error.status === 400) return 'Caminho de destino inválido.';
            if (error.status === 404) return 'A pasta não existe mais.';
            return 'Não foi possível mover a pasta.';
        }

        if (generation !== this.generation || vaultId !== this.vaultId) return 'O vault ativo mudou.';
        if (previewed) this.finishFolderMove(source);
        this.expandedFolders.update(paths => new Set([...paths].map(path => remapPath(path, source, target))));
        this.reload();
        return null;
    }

    beginOptimisticNoteMove(entry: VaultEntry, targetPath: string): void {
        const sourceParent = entry.path.includes('/') ? entry.path.slice(0, entry.path.lastIndexOf('/')) : '';
        const targetParent = targetPath.includes('/') ? targetPath.slice(0, targetPath.lastIndexOf('/')) : '';
        const currentEntry = this.children().get(sourceParent)?.find(item => item.path === entry.path && item.kind === 'note');
        const targetChildren = this.children().get(targetParent);
        if (!currentEntry || !targetChildren || targetChildren.some(item => item.path === targetPath)) return;
        const targetEntry = { ...currentEntry, path: targetPath, name: targetPath.slice(targetPath.lastIndexOf('/') + 1) };
        this.pendingMoves.set(entry.path, {
            targetPath, sourceParent, targetParent, kind: 'note', sourceEntry: currentEntry, targetEntry
        });
        this.movingEntries.update(paths => new Set(paths).add(targetPath));
        this.children.update(children => {
            const next = new Map(children);
            if (sourceParent === targetParent) {
                next.set(sourceParent, (next.get(sourceParent) ?? []).filter(item => item.path !== entry.path)
                    .concat(targetEntry).sort((left, right) => left.name.localeCompare(right.name)));
            } else {
                next.set(sourceParent, (next.get(sourceParent) ?? []).filter(item => item.path !== entry.path));
                next.set(targetParent, [...(next.get(targetParent) ?? []), targetEntry]
                    .sort((left, right) => left.name.localeCompare(right.name)));
            }
            return next;
        });
    }

    settleOptimisticNoteMove(sourcePath: string, succeeded: boolean): void {
        const move = this.pendingMoves.get(sourcePath);
        if (!move || move.kind !== 'note') return;
        if (succeeded) this.finishFolderMove(sourcePath);
        else this.rollbackFolderMove(sourcePath);
    }

    private finishFolderMove(source: string): void {
        const move = this.pendingMoves.get(source);
        if (!move) return;
        this.pendingMoves.delete(source);
        this.movingEntries.update(paths => {
            const next = new Set(paths);
            next.delete(move.targetPath);
            return next;
        });
    }

    private rollbackFolderMove(source: string): void {
        const move = this.pendingMoves.get(source);
        if (!move) return;
        this.pendingMoves.delete(source);
        this.movingEntries.update(paths => {
            const next = new Set(paths);
            next.delete(move.targetPath);
            return next;
        });
        this.children.update(children => {
            const next = new Map(children);
            const targetEntries = next.get(move.targetParent) ?? [];
            next.set(move.targetParent, targetEntries.filter(entry => entry !== move.targetEntry));
            const sourceEntries = next.get(move.sourceParent) ?? [];
            if (!sourceEntries.some(entry => entry.path === source)) {
                next.set(move.sourceParent, [...sourceEntries, move.sourceEntry]
                    .sort((left, right) => left.name.localeCompare(right.name)));
            }
            return next;
        });
    }

    reload(): void {
        this.errors.set(new Map());
        void this.loadChildren(null, true);
        for (const path of this.expandedFolders()) void this.loadChildren(path, true);
    }

    private finishTrashMove(path: string): void {
        this.pendingTrashMoves.delete(path);
        this.movingEntries.update(paths => {
            const next = new Set(paths);
            next.delete(path);
            return next;
        });
    }

    private rollbackTrashMove(path: string): void {
        const pending = this.pendingTrashMoves.get(path);
        if (!pending) return;
        this.finishTrashMove(path);
        this.children.update(children => {
            const next = new Map(children);
            const siblings = next.get(pending.parentKey) ?? [];
            if (!siblings.some(entry => entry.path === path)) {
                next.set(pending.parentKey, [...siblings, pending.entry]
                    .sort((left, right) => left.name.localeCompare(right.name)));
            }
            for (const [childPath, entries] of pending.children) {
                if (!next.has(childPath)) next.set(childPath, entries);
            }
            return next;
        });
        this.expandedFolders.update(paths => new Set([...paths, ...pending.expanded]));
    }
}

/** Troca o prefixo `source` por `target` quando `path` é a própria pasta ou está dentro dela. */
export function remapPath(path: string, source: string, target: string): string {
    if (path === source) return target;
    return path.startsWith(`${source}/`) ? `${target}${path.slice(source.length)}` : path;
}
