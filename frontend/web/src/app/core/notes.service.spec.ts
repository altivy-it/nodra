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
});