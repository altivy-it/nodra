import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface NoteSummary { path: string; eTag: string; }
export interface Note { path: string; content: string; eTag: string; }

const invalidPathCharacters = /[<>:"|?*\x00-\x1f]/;
const reservedWindowsNames = /^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\..*)?$/i;

export function normalizeNotePath(path: string): string {
    const normalizedPath = path.trim().replaceAll('\\', '/');
    if (!normalizedPath || normalizedPath.length > 1024 || normalizedPath.startsWith('/') || /^[a-z]:/i.test(normalizedPath)) {
        throw new Error('Informe um caminho relativo válido para a nota.');
    }

    const segments = normalizedPath.split('/');
    if (segments.some(segment => !segment || segment === '.' || segment === '..' || invalidPathCharacters.test(segment) || /[. ]$/.test(segment) || reservedWindowsNames.test(segment))) {
        throw new Error('O nome da nota contém caracteres ou segmentos inválidos.');
    }

    const fileName = segments.at(-1)!;
    if (!/\.md$/i.test(fileName)) segments[segments.length - 1] = `${fileName}.md`;
    return segments.join('/');
}

@Injectable({ providedIn: 'root' })
export class NotesService {
    readonly notes = signal<NoteSummary[]>([]);
    readonly selected = signal<Note | null>(null);
    readonly loading = signal(false);
    readonly error = signal<string | null>(null);
    private vaultId: string | null = null;
    private generation = 0;

    constructor(private readonly http: HttpClient) { }

    activateVault(vaultId: string | null, force = false): void {
        if (!force && this.vaultId === vaultId) return;
        this.vaultId = vaultId;
        this.generation++;
        this.notes.set([]);
        this.selected.set(null);
        this.error.set(null);
        this.loading.set(false);
    }

    async load(): Promise<void> {
        const context = this.context();
        if (!context) return;
        this.loading.set(true); this.error.set(null);
        try {
            const notes = await firstValueFrom(this.http.get<NoteSummary[]>(context.baseUrl));
            if (this.isCurrent(context)) this.notes.set(notes);
        }
        catch { if (this.isCurrent(context)) this.error.set('Não foi possível carregar as notas.'); }
        finally { if (this.isCurrent(context)) this.loading.set(false); }
    }

    async open(path: string): Promise<void> {
        const context = this.context();
        if (!context) return;
        this.error.set(null);
        let normalizedPath: string;
        try { normalizedPath = normalizeNotePath(path); }
        catch (error) { this.error.set((error as Error).message); return; }
        try {
            const response = await firstValueFrom(this.http.get<{ path: string; content: string }>(this.noteUrl(context.baseUrl, normalizedPath), { observe: 'response' }));
            if (this.isCurrent(context)) {
                this.selected.set({ ...response.body!, eTag: response.headers.get('ETag')?.replaceAll('"', '') ?? '' });
            }
        } catch { if (this.isCurrent(context)) this.error.set('Não foi possível abrir a nota.'); }
    }

    newNote(): void { this.selected.set({ path: 'nova-nota.md', content: '# Nova nota\n', eTag: '' }); }

    async save(note: Note): Promise<void> {
        const context = this.context();
        if (!context) return;
        this.error.set(null);
        let normalizedPath: string;
        try { normalizedPath = normalizeNotePath(note.path); }
        catch (error) { this.error.set((error as Error).message); return; }
        const normalizedNote = { ...note, path: normalizedPath };
        try {
            if (normalizedNote.eTag) {
                await firstValueFrom(this.http.put(this.noteUrl(context.baseUrl, normalizedPath), { content: normalizedNote.content }, { headers: new HttpHeaders({ 'If-Match': `"${normalizedNote.eTag}"` }) }));
            } else {
                await firstValueFrom(this.http.post(this.noteUrl(context.baseUrl, normalizedPath), { content: normalizedNote.content }));
            }
            if (!this.isCurrent(context)) return;
            const response = await firstValueFrom(this.http.get<{ path: string; content: string }>(this.noteUrl(context.baseUrl, normalizedPath), { observe: 'response' }));
            if (!this.isCurrent(context)) return;
            const eTag = response.headers.get('ETag')?.replaceAll('"', '') ?? normalizedNote.eTag;
            this.selected.update(current => current &&
                (current.path === note.path || current.path === normalizedPath) &&
                current.eTag === note.eTag
                ? { ...current, path: normalizedPath, eTag }
                : current);
            this.notes.update(items => items.some(item => item.path === normalizedPath)
                ? items.map(item => item.path === normalizedPath ? { ...item, eTag } : item)
                : [...items, { path: normalizedPath, eTag }].sort((left, right) => left.path.localeCompare(right.path)));
        } catch { if (this.isCurrent(context)) this.error.set('Não foi possível salvar. A nota pode ter sido alterada por outra pessoa.'); }
    }

    async delete(note: Note): Promise<void> {
        const context = this.context();
        if (!context) return;
        if (!note.eTag) { this.selected.set(null); return; }
        try {
            await firstValueFrom(this.http.delete(this.noteUrl(context.baseUrl, note.path), { headers: new HttpHeaders({ 'If-Match': `"${note.eTag}"` }) }));
            if (!this.isCurrent(context)) return;
            this.selected.set(null); await this.load();
        } catch { if (this.isCurrent(context)) this.error.set('Não foi possível excluir a nota.'); }
    }

    private context(): { baseUrl: string; generation: number; vaultId: string } | null {
        if (!this.vaultId) return null;
        return {
            baseUrl: `/api/v1/vaults/${encodeURIComponent(this.vaultId)}/notes`,
            generation: this.generation,
            vaultId: this.vaultId
        };
    }

    private isCurrent(context: { generation: number; vaultId: string }): boolean {
        return context.generation === this.generation && context.vaultId === this.vaultId;
    }

    private noteUrl(baseUrl: string, path: string): string {
        return `${baseUrl}/${path.split('/').map(segment => encodeURIComponent(segment)).join('/')}`;
    }
}
