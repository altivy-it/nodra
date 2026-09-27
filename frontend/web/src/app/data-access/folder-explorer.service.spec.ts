import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FolderExplorerService, remapPath } from './folder-explorer.service';

describe('FolderExplorerService', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(), provideHttpClientTesting()]
        });
    });

    it('carrega a raiz e solicita filhos somente ao expandir a pasta', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');

        const rootRequest = http.expectOne('/api/v1/vaults/vault-1/entries');
        rootRequest.flush([{ path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }]);
        await Promise.resolve();

        const loadChildren = service.toggleFolder('projetos');
        const childrenRequest = http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos');
        childrenRequest.flush([{ path: 'projetos/plano.md', name: 'plano.md', kind: 'note', eTag: 'v1' }]);
        await loadChildren;

        expect(service.children().get('projetos')).toEqual([
            { path: 'projetos/plano.md', name: 'plano.md', kind: 'note', eTag: 'v1' }
        ]);
        http.verify();
    });

    it('descarta a resposta tardia do vault anterior', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        const oldRequest = http.expectOne('/api/v1/vaults/vault-1/entries');

        service.activateVault('vault-2');
        const currentRequest = http.expectOne('/api/v1/vaults/vault-2/entries');
        currentRequest.flush([{ path: 'atual.md', name: 'atual.md', kind: 'note', eTag: 'v2' }]);
        oldRequest.flush([{ path: 'antiga.md', name: 'antiga.md', kind: 'note', eTag: 'v1' }]);
        await Promise.resolve();

        expect(service.children().get('')).toEqual([
            { path: 'atual.md', name: 'atual.md', kind: 'note', eTag: 'v2' }
        ]);
        http.verify();
    });

    it('cria uma pasta com PUT e atualiza a lista do pai', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();

        const createFolder = service.createFolder(null, 'Notas de produto');
        const createRequest = http.expectOne('/api/v1/vaults/vault-1/folders/Notas%20de%20produto');
        expect(createRequest.request.method).toBe('PUT');
        createRequest.flush(null, { status: 201, statusText: 'Created' });
        await Promise.resolve();
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'Notas de produto', name: 'Notas de produto', kind: 'folder', eTag: null }
        ]);
        await expect(createFolder).resolves.toBeNull();

        expect(service.children().get('')?.[0].path).toBe('Notas de produto');
        http.verify();
    });

    it('exclui somente a pasta selecionada e atualiza a lista do pai', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();

        const deleteFolder = service.deleteEmptyFolder('projetos/rascunhos');
        const request = http.expectOne('/api/v1/vaults/vault-1/folders/projetos/rascunhos');
        expect(request.request.method).toBe('DELETE');
        request.flush(null, { status: 204, statusText: 'No Content' });
        await Promise.resolve();
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush([]);
        await expect(deleteFolder).resolves.toBeNull();

        expect(service.children().get('projetos')).toEqual([]);
        http.verify();
    });

    it('recarrega a raiz e as pastas expandidas ao atualizar', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ]);
        await Promise.resolve();
        const expand = service.toggleFolder('projetos');
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush([]);
        await expand;

        service.reload();
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ]);
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush([
            { path: 'projetos/roadmap.md', name: 'roadmap.md', kind: 'note', eTag: 'v1' }
        ]);
        await Promise.resolve();

        expect(service.children().get('projetos')?.[0].path).toBe('projetos/roadmap.md');
        http.verify();
    });

    it('informa conflito de exclusão sem marcar a árvore com erro de carregamento', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();

        const deleteFolder = service.deleteEmptyFolder('projetos');
        http.expectOne('/api/v1/vaults/vault-1/folders/projetos')
            .flush({ title: 'Conflito', status: 409 }, { status: 409, statusText: 'Conflict' });

        await expect(deleteFolder).resolves.toContain('não pode ser excluída');
        expect(service.errors().size).toBe(0);
        http.verify();
    });

    it('move a pasta com PATCH, preserva a expansão no novo caminho e recarrega', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ]);
        await Promise.resolve();
        const expand = service.toggleFolder('projetos');
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush([]);
        await expand;

        const move = service.moveFolder('projetos', 'Arquivo antigo');
        const request = http.expectOne('/api/v1/vaults/vault-1/folders/projetos');
        expect(request.request.method).toBe('PATCH');
        expect(request.request.body).toEqual({ path: 'Arquivo antigo' });
        request.flush({ path: 'Arquivo antigo' });
        await expect(move).resolves.toBeNull();

        expect([...service.expandedFolders()]).toEqual(['Arquivo antigo']);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=Arquivo%20antigo').flush([]);
        http.verify();
    });

    it('rejeita mover a pasta para dentro dela mesma sem chamar a API', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);

        await expect(service.moveFolder('projetos', 'projetos/interno')).resolves.toContain('dentro dela');
        http.verify();
    });

    it('traduz o conflito de destino existente', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);

        const move = service.moveFolder('projetos', 'arquivo');
        http.expectOne('/api/v1/vaults/vault-1/folders/projetos').flush(
            { title: 'Conflito', status: 409, code: 'folder-destination-exists' },
            { status: 409, statusText: 'Conflict' });

        await expect(move).resolves.toBe('Já existe um item com esse caminho.');
        http.verify();
    });
});

describe('remapPath', () => {
    it('troca somente o prefixo exato da pasta movida', () => {
        expect(remapPath('projetos', 'projetos', 'arquivo')).toBe('arquivo');
        expect(remapPath('projetos/a/b.md', 'projetos', 'arquivo')).toBe('arquivo/a/b.md');
        expect(remapPath('projetos-2/b.md', 'projetos', 'arquivo')).toBe('projetos-2/b.md');
    });
});