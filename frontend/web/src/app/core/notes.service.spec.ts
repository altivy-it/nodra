import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NotesService, normalizeNotePath } from './notes.service';

describe('NotesService', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(), provideHttpClientTesting()]
        });
    });

    it('descarta a resposta tardia do vault anterior', async () => {
        const service = TestBed.inject(NotesService);
        const http = TestBed.inject(HttpTestingController);

        service.activateVault('vault-1');
        const firstLoad = service.load();
        const firstRequest = http.expectOne('/api/v1/vaults/vault-1/notes');

        service.activateVault('vault-2');
        const secondLoad = service.load();
        const secondRequest = http.expectOne('/api/v1/vaults/vault-2/notes');
        secondRequest.flush([{ path: 'atual.md', eTag: 'v2' }]);
        firstRequest.flush([{ path: 'obsoleta.md', eTag: 'v1' }]);
        await Promise.all([firstLoad, secondLoad]);

        expect(service.notes()).toEqual([{ path: 'atual.md', eTag: 'v2' }]);
        http.verify();
    });

    it('inclui a extensão Markdown antes de criar uma nota', async () => {
        const service = TestBed.inject(NotesService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        service.selected.set({ path: 'Pensamentos do dia', content: '# Hoje', eTag: '' });

        const save = service.save({ path: 'Pensamentos do dia', content: '# Hoje', eTag: '' });
        const request = http.expectOne('/api/v1/vaults/vault-1/notes/Pensamentos%20do%20dia.md');
        request.flush({ path: 'Pensamentos do dia.md', content: '# Hoje' });
        await Promise.resolve();
        http.expectOne(request => request.url.includes('Pensamentos') && request.url.endsWith('.md')).flush({ path: 'Pensamentos do dia.md', content: '# Hoje' }, { headers: { ETag: '"v1"' } });
        await save;

        expect(service.selected()?.path).toBe('Pensamentos do dia.md');
    });

    it('rejeita caracteres problemáticos sem enviar request', async () => {
        const service = TestBed.inject(NotesService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');

        await service.save({ path: 'nota?.md', content: 'conteúdo', eTag: '' });

        expect(service.error()).toContain('caracteres');
        expect(() => http.expectNone(() => true)).not.toThrow();
    });

    it('normaliza separadores e rejeita caminhos absolutos', () => {
        expect(normalizeNotePath('pasta\\nota')).toBe('pasta/nota.md');
        expect(() => normalizeNotePath('C:/fora/nota.md')).toThrow();
    });

    it('cria uma nota no diretório selecionado', () => {
        const service = TestBed.inject(NotesService);

        service.newNote('projetos/decisoes');

        expect(service.selected()?.path).toBe('projetos/decisoes/nova-nota.md');
    });

    it('move uma nota salva com PATCH e If-Match e atualiza o caminho selecionado', async () => {
        const service = TestBed.inject(NotesService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        const note = { path: 'rascunho.md', content: '# Rascunho', eTag: 'v1' };
        service.selected.set(note);
        service.notes.set([{ path: 'rascunho.md', eTag: 'v1' }]);

        const move = service.move(note, 'projetos/plano');
        const request = http.expectOne('/api/v1/vaults/vault-1/notes/rascunho.md');
        expect(request.request.method).toBe('PATCH');
        expect(request.request.headers.get('If-Match')).toBe('"v1"');
        expect(request.request.body).toEqual({ path: 'projetos/plano.md' });
        request.flush({ path: 'projetos/plano.md' });

        await expect(move).resolves.toBeNull();
        expect(service.selected()?.path).toBe('projetos/plano.md');
        expect(service.notes()[0].path).toBe('projetos/plano.md');
        http.verify();
    });

    it('mantém o caminho original quando o destino já existe', async () => {
        const service = TestBed.inject(NotesService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        const note = { path: 'a.md', content: '# A', eTag: 'v1' };
        service.selected.set(note);

        const move = service.move(note, 'b.md');
        http.expectOne('/api/v1/vaults/vault-1/notes/a.md').flush(
            { title: 'Conflito', status: 409, code: 'note-destination-exists' },
            { status: 409, statusText: 'Conflict' });

        await expect(move).resolves.toContain('Já existe');
        expect(service.selected()?.path).toBe('a.md');
        http.verify();
    });
});