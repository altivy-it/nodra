import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface VaultEntry {
    path: string;
    name: string;
    kind: 'folder' | 'note';
    eTag: string | null;
}

@Injectable({ providedIn: 'root' })
export class FolderExplorerService {
    readonly children = signal<Map<string, VaultEntry[]>>(new Map());
    readonly expandedFolders = signal<Set<string>>(new Set());
    readonly loadingFolders = signal<Set<string>>(new Set());
    readonly errors = signal<Map<string, string>>(new Map());
    private vaultId: string | null = null;
    private generation = 0;

    constructor(private readonly http: HttpClient) { }

    activateVault(vaultId: string | null): void {
        if (this.vaultId === vaultId) return;
        this.vaultId = vaultId;
        this.generation++;
        this.children.set(new Map());
        this.expandedFolders.set(new Set());
        this.loadingFolders.set(new Set());
        this.errors.set(new Map());
        if (vaultId) void this.loadChildren(null);
    }

    async loadChildren(parentPath: string | null, force = false): Promise<void> {
        const vaultId = this.vaultId;
        const generation = this.generation;
        const key = parentPath ?? '';
        if (!vaultId || (!force && this.children().has(key))) return;

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
            if (generation !== this.generation || vaultId !== this.vaultId) return;
            this.children.update(children => new Map(children).set(key, entries));
        } catch {
            if (generation !== this.generation || vaultId !== this.vaultId) return;
            this.errors.update(errors => new Map(errors).set(key, 'Não foi possível carregar esta pasta.'));
        } finally {
            if (generation === this.generation && vaultId === this.vaultId) {
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
        const encodedPath = path.split('/').map(segment => encodeURIComponent(segment)).join('/');
        try {
            await firstValueFrom(this.http.put<void>(
                `/api/v1/vaults/${encodeURIComponent(vaultId)}/folders/${encodedPath}`,
                null));
            if (generation !== this.generation || vaultId !== this.vaultId) return 'O vault ativo mudou.';
            await this.loadChildren(parentPath, true);
            return null;
        } catch (error) {
            return error instanceof HttpErrorResponse && error.status === 409
                ? 'Já existe um arquivo com esse nome neste destino.'
                : 'Não foi possível criar a pasta. Verifique o nome, o destino e suas permissões.';
        }
    }

    /** Exclui a pasta vazia; retorna `null` em sucesso ou a mensagem de erro da ação. */
    async deleteEmptyFolder(path: string): Promise<string | null> {
        const vaultId = this.vaultId;
        const generation = this.generation;
        if (!vaultId) return 'Nenhum vault ativo.';

        const segments = path.split('/');
        const parentPath = segments.length > 1 ? segments.slice(0, -1).join('/') : null;
        const encodedPath = segments.map(segment => encodeURIComponent(segment)).join('/');
        try {
            await firstValueFrom(this.http.delete<void>(
                `/api/v1/vaults/${encodeURIComponent(vaultId)}/folders/${encodedPath}`));
            if (generation !== this.generation || vaultId !== this.vaultId) return 'O vault ativo mudou.';
            await this.loadChildren(parentPath, true);
            return null;
        } catch (error) {
            return error instanceof HttpErrorResponse && error.status === 409
                ? 'A pasta contém arquivos ou subpastas e não pode ser excluída.'
                : 'Não foi possível excluir esta pasta.';
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

        const encodedPath = source.split('/').map(segment => encodeURIComponent(segment)).join('/');
        try {
            await firstValueFrom(this.http.patch<void>(
                `/api/v1/vaults/${encodeURIComponent(vaultId)}/folders/${encodedPath}`,
                { path: target }));
        } catch (error) {
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
        this.expandedFolders.update(paths => new Set([...paths].map(path => remapPath(path, source, target))));
        this.reload();
        return null;
    }

    reload(): void {
        this.children.set(new Map());
        this.errors.set(new Map());
        void this.loadChildren(null, true);
        for (const path of this.expandedFolders()) void this.loadChildren(path, true);
    }
}

/** Troca o prefixo `source` por `target` quando `path` é a própria pasta ou está dentro dela. */
export function remapPath(path: string, source: string, target: string): string {
    if (path === source) return target;
    return path.startsWith(`${source}/`) ? `${target}${path.slice(source.length)}` : path;
}