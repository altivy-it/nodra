import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FolderExplorerService, remapPath, VaultEntry } from './folder-explorer.service';

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

    it('restaura somente pastas existentes na hierarquia autorizada', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1', ['projetos/2026', '../fora']);

        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ]);
        await Promise.resolve();
        await Promise.resolve();
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush([
            { path: 'projetos/2026', name: '2026', kind: 'folder', eTag: null }
        ]);
        await Promise.resolve();
        await Promise.resolve();
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos/2026').flush([
            { path: 'projetos/2026/plano.md', name: 'plano.md', kind: 'note', eTag: 'v1' }
        ]);
        await Promise.resolve();

        expect([...service.expandedFolders()]).toEqual(['projetos', 'projetos/2026']);
        expect(service.children().has('../fora')).toBe(false);
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

    it('explica a falta de permissão na raiz e permite tentar novamente', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush(
            { status: 403 }, { status: 403, statusText: 'Forbidden' });
        await Promise.resolve();

        expect(service.errors().get('')).toContain('permissão');
        expect(service.children().has('')).toBe(false);

        const retry = service.loadChildren(null, true);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await retry;
        expect(service.errors().size).toBe(0);
        expect(service.children().get('')).toEqual([]);
        http.verify();
    });

    it('explica uma sessão expirada sem manter entradas do vault', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ]);
        await Promise.resolve();

        const reload = service.loadChildren(null, true);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush(
            { status: 401 }, { status: 401, statusText: 'Unauthorized' });
        await reload;

        expect(service.errors().get('')).toContain('sessão expirou');
        expect(service.children().size).toBe(0);
        http.verify();
    });

    it('mantém a raiz carregada quando uma subpasta fica sem conexão', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        const root = [{ path: 'projetos', name: 'projetos', kind: 'folder' as const, eTag: null }];
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush(root);
        await Promise.resolve();

        const expansion = service.toggleFolder('projetos');
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush(
            null, { status: 0, statusText: 'Unknown Error' });
        await expansion;

        expect(service.children().get('')).toEqual(root);
        expect(service.errors().get('projetos')).toContain('conexão');
        expect(service.errors().has('')).toBe(false);
        http.verify();
    });

    it('remove filhos em cache após revogação de acesso ao vault', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ]);
        await Promise.resolve();
        const expansion = service.toggleFolder('projetos');
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush([
            { path: 'projetos/plano.md', name: 'plano.md', kind: 'note', eTag: 'v1' }
        ]);
        await expansion;

        const reload = service.loadChildren(null, true);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush(
            { status: 403 }, { status: 403, statusText: 'Forbidden' });
        await reload;

        expect(service.errors().get('')).toContain('permissão');
        expect(service.children().size).toBe(0);
        http.verify();
    });

    it('ignora resposta atrasada da subpasta após negação de acesso à raiz', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ]);
        await Promise.resolve();

        const expansion = service.toggleFolder('projetos');
        const pendingChild = http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos');
        const rootReload = service.loadChildren(null, true);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush(
            { status: 403 }, { status: 403, statusText: 'Forbidden' });
        await rootReload;
        pendingChild.flush([{ path: 'projetos/plano.md', name: 'plano.md', kind: 'note', eTag: 'v1' }]);
        await expansion;

        expect(service.children().size).toBe(0);
        expect([...service.errors().keys()]).toEqual(['']);
        expect(service.loadingFolders().size).toBe(0);
        http.verify();
    });

    it('prioriza o 403 da raiz sobre erros anteriores das subpastas', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ]);
        await Promise.resolve();
        const expansion = service.toggleFolder('projetos');
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush(
            null, { status: 0, statusText: 'Unknown Error' });
        await expansion;

        const reload = service.loadChildren(null, true);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush(
            { status: 403 }, { status: 403, statusText: 'Forbidden' });
        await reload;

        expect([...service.errors().keys()]).toEqual(['']);
        expect(service.errors().get('')).toContain('permissão');
        http.verify();
    });

    it('remove somente o ramo negado e mantém os demais visíveis', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null },
            { path: 'arquivo', name: 'arquivo', kind: 'folder', eTag: null }
        ]);
        await Promise.resolve();
        const expansion = service.toggleFolder('projetos');
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush([
            { path: 'projetos/plano.md', name: 'plano.md', kind: 'note', eTag: 'v1' }
        ]);
        await expansion;

        const reload = service.loadChildren('projetos', true);
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush(
            { status: 403 }, { status: 403, statusText: 'Forbidden' });
        await reload;

        expect(service.children().get('')?.map(item => item.path)).toEqual(['projetos', 'arquivo']);
        expect(service.children().has('projetos')).toBe(false);
        expect(service.errors().get('projetos')).toContain('permissão');
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

    it('exibe a nova pasta durante o PUT e desfaz somente ela se houver conflito', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        const sibling = { path: 'projetos', name: 'projetos', kind: 'folder' as const, eTag: null };
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([sibling]);
        await Promise.resolve();

        const create = service.createFolder(null, 'rascunhos');
        const request = http.expectOne('/api/v1/vaults/vault-1/folders/rascunhos');
        expect(service.children().get('')?.map(entry => entry.path)).toEqual(['projetos', 'rascunhos']);
        expect(request.request.method).toBe('PUT');

        request.flush({ status: 409 }, { status: 409, statusText: 'Conflict' });
        await expect(create).resolves.toContain('Já existe');
        expect(service.children().get('')).toEqual([sibling]);
        http.verify();
    });

    it('mantém a pasta pendente visível durante recarregamento e confirma pelo servidor', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();

        const create = service.createFolder(null, 'rascunhos');
        const put = http.expectOne('/api/v1/vaults/vault-1/folders/rascunhos');
        expect(service.pendingFolders().has('rascunhos')).toBe(true);
        const reload = service.loadChildren(null, true);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await reload;
        expect(service.children().get('')?.map(entry => entry.path)).toEqual(['rascunhos']);

        put.flush(null, { status: 201, statusText: 'Created' });
        await Promise.resolve();
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'rascunhos', name: 'rascunhos', kind: 'folder', eTag: null }
        ]);
        await expect(create).resolves.toBeNull();
        expect(service.pendingFolders().size).toBe(0);
        expect(service.children().get('')?.map(entry => entry.path)).toEqual(['rascunhos']);
        http.verify();
    });

    it('não aplica a resposta de criação pendente depois de trocar de vault', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();
        const create = service.createFolder(null, 'rascunhos');
        const oldPut = http.expectOne('/api/v1/vaults/vault-1/folders/rascunhos');

        service.activateVault('vault-2');
        http.expectOne('/api/v1/vaults/vault-2/entries').flush([]);
        oldPut.flush(null, { status: 201, statusText: 'Created' });
        await expect(create).resolves.toBe('O vault ativo mudou.');
        expect(service.children().get('')).toEqual([]);
        expect(service.pendingFolders().size).toBe(0);
        http.verify();
    });

    it('descarta pasta provisória quando o vault perde autorização durante o PUT', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();
        const create = service.createFolder(null, 'rascunhos');
        const pendingPut = http.expectOne('/api/v1/vaults/vault-1/folders/rascunhos');

        const denied = service.loadChildren(null, true);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush(
            { status: 403 }, { status: 403, statusText: 'Forbidden' });
        await denied;
        expect(service.children().size).toBe(0);
        expect(service.pendingFolders().size).toBe(0);

        pendingPut.flush({ status: 403 }, { status: 403, statusText: 'Forbidden' });
        await create;
        const retry = service.loadChildren(null, true);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await retry;
        expect(service.children().get('')).toEqual([]);
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

    it('retira a pasta vazia durante o DELETE e a restaura após conflito sem perder irmãos', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        const target = { path: 'rascunhos', name: 'rascunhos', kind: 'folder' as const, eTag: null };
        const sibling = { path: 'projetos', name: 'projetos', kind: 'folder' as const, eTag: null };
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([sibling, target]);
        await Promise.resolve();

        const deleting = service.deleteEmptyFolder('rascunhos');
        const request = http.expectOne('/api/v1/vaults/vault-1/folders/rascunhos');
        expect(request.request.method).toBe('DELETE');
        expect(service.children().get('')?.map(entry => entry.path)).toEqual(['projetos']);

        request.flush({ status: 409 }, { status: 409, statusText: 'Conflict' });
        await expect(deleting).resolves.toContain('não pode ser excluída');
        expect(service.children().get('')).toEqual([sibling, target]);
        http.verify();
    });

    it('não reapresenta pasta em exclusão após reload e bloqueia criação no mesmo caminho', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        const target = { path: 'rascunhos', name: 'rascunhos', kind: 'folder' as const, eTag: null };
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([target]);
        await Promise.resolve();

        const deleting = service.deleteEmptyFolder('rascunhos');
        const pendingDelete = http.expectOne('/api/v1/vaults/vault-1/folders/rascunhos');
        const reload = service.loadChildren(null, true);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([target]);
        await reload;
        expect(service.children().get('')).toEqual([]);
        await expect(service.createFolder(null, 'rascunhos')).resolves.toContain('exclusão');
        http.expectNone('/api/v1/vaults/vault-1/folders/rascunhos');

        pendingDelete.flush({ status: 409 }, { status: 409, statusText: 'Conflict' });
        await expect(deleting).resolves.toContain('não pode ser excluída');
        expect(service.children().get('')).toEqual([target]);
        http.verify();
    });

    it('não restaura pasta em exclusão depois da troca de vault', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'rascunhos', name: 'rascunhos', kind: 'folder', eTag: null }
        ]);
        await Promise.resolve();
        const deleting = service.deleteEmptyFolder('rascunhos');
        const oldDelete = http.expectOne('/api/v1/vaults/vault-1/folders/rascunhos');

        service.activateVault('vault-2');
        http.expectOne('/api/v1/vaults/vault-2/entries').flush([]);
        oldDelete.flush({ status: 409 }, { status: 409, statusText: 'Conflict' });
        await deleting;
        expect(service.children().get('')).toEqual([]);
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
        const rootRefresh = http.expectOne('/api/v1/vaults/vault-1/entries');
        const childRefresh = http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos');
        expect(service.children().get('')?.[0].path).toBe('projetos');
        expect(service.children().get('projetos')).toEqual([]);
        expect([...service.expandedFolders()]).toEqual(['projetos']);

        rootRefresh.flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ]);
        childRefresh.flush([
            { path: 'projetos/roadmap.md', name: 'roadmap.md', kind: 'note', eTag: 'v1' }
        ]);
        await Promise.resolve();

        expect(service.children().get('projetos')?.[0].path).toBe('projetos/roadmap.md');
        http.verify();
    });

    it('ignora uma resposta antiga quando duas atualizações da mesma pasta terminam fora de ordem', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();

        const older = service.loadChildren(null, true);
        const newer = service.loadChildren(null, true);
        const [oldRequest, newRequest] = http.match('/api/v1/vaults/vault-1/entries');
        expect(oldRequest).toBeTruthy();
        expect(newRequest).toBeTruthy();
        newRequest.flush([{ path: 'nova.md', name: 'nova.md', kind: 'note', eTag: 'v2' }]);
        await newer;
        oldRequest.flush([{ path: 'antiga.md', name: 'antiga.md', kind: 'note', eTag: 'v1' }]);
        await older;

        expect(service.children().get('')?.map(entry => entry.path)).toEqual(['nova.md']);
        expect(service.loadingFolders().size).toBe(0);
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

    it('mantém o movimento otimista durante reload e restaura somente a pasta em conflito', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        const initial = [
            { path: 'projetos', name: 'projetos', kind: 'folder' as const, eTag: null },
            { path: 'outros', name: 'outros', kind: 'folder' as const, eTag: null }
        ];
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush(initial);
        await Promise.resolve();
        const expandDestination = service.toggleFolder('outros');
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=outros').flush([]);
        await expandDestination;

        const move = service.moveFolder('projetos', 'outros/projetos');
        const request = http.expectOne('/api/v1/vaults/vault-1/folders/projetos');
        expect(service.children().get('')?.map(entry => entry.path)).toEqual(['outros']);
        expect(service.children().get('outros')?.map(entry => entry.path)).toEqual(['outros/projetos']);
        expect(service.movingEntries().has('outros/projetos')).toBe(true);

        const reload = service.loadChildren(null, true);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush(initial);
        await reload;
        expect(service.children().get('')?.map(entry => entry.path)).toEqual(['outros']);
        expect(service.children().get('outros')?.map(entry => entry.path)).toEqual(['outros/projetos']);

        request.flush({ status: 409, code: 'folder-destination-exists' },
            { status: 409, statusText: 'Conflict' });
        await expect(move).resolves.toBe('Já existe um item com esse caminho.');
        expect(service.children().get('')?.map(entry => entry.path)).toEqual(['outros', 'projetos']);
        expect(service.children().get('outros')).toEqual([]);
        expect(service.movingEntries().size).toBe(0);
        http.verify();
    });

    it('reverte somente a nota movida quando a API recusa o destino', () => {
        const service = TestBed.inject(FolderExplorerService);
        const source: VaultEntry = { path: 'plano.md', name: 'plano.md', kind: 'note', eTag: 'v1' };
        service.children.set(new Map([
            ['', [source]],
            ['arquivo', [{ path: 'arquivo/outra.md', name: 'outra.md', kind: 'note', eTag: 'v2' }]]
        ]));

        service.beginOptimisticNoteMove(source, 'arquivo/plano.md');
        expect(service.children().get('')?.map(entry => entry.path)).toEqual([]);
        expect(service.children().get('arquivo')?.map(entry => entry.path)).toEqual([
            'arquivo/outra.md', 'arquivo/plano.md'
        ]);
        expect(service.movingEntries().has('arquivo/plano.md')).toBe(true);

        service.settleOptimisticNoteMove('plano.md', false);

        expect(service.children().get('')?.map(entry => entry.path)).toEqual(['plano.md']);
        expect(service.children().get('arquivo')?.map(entry => entry.path)).toEqual(['arquivo/outra.md']);
        expect(service.movingEntries().size).toBe(0);
    });

    it('rejeita mover a pasta para dentro dela mesma sem chamar a API', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);

        await expect(service.moveFolder('projetos', 'projetos/interno')).resolves.toContain('dentro dela');
        http.verify();
    });

    it('move uma nota para a lixeira com If-Match e a restaura visualmente em conflito', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ]);
        await Promise.resolve();
        const expand = service.toggleFolder('projetos');
        const note: VaultEntry = { path: 'projetos/plano.md', name: 'plano.md', kind: 'note', eTag: 'version-1' };
        http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush([note]);
        await expand;

        const moving = service.moveToTrash(note);
        const request = http.expectOne('/api/v1/vaults/vault-1/notes/projetos/plano.md');
        expect(request.request.method).toBe('DELETE');
        expect(request.request.headers.get('If-Match')).toBe('"version-1"');
        expect(service.children().get('projetos')).toEqual([]);
        request.flush({ code: 'note-version-conflict' }, { status: 409, statusText: 'Conflict' });

        await expect(moving).resolves.toContain('alterada por outra sessão');
        expect(service.children().get('projetos')).toEqual([note]);
        expect(service.movingEntries().size).toBe(0);
        http.verify();
    });

    it('carrega todas as páginas da lixeira sem perder o cursor', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();

        const loading = service.loadTrash();
        http.expectOne('/api/v1/vaults/vault-1/trash?limit=100').flush({
            items: [{ id: '11111111-1111-4111-8111-111111111111', path: 'a.md', name: 'a.md', kind: 'note', deletedAtUtc: '2026-09-28T00:00:00Z', retentionDays: null }],
            nextCursor: '11111111-1111-4111-8111-111111111111'
        });
        await Promise.resolve();
        http.expectOne(request => request.url === '/api/v1/vaults/vault-1/trash' &&
            request.params.get('cursor') === '11111111-1111-4111-8111-111111111111').flush({
            items: [{ id: '22222222-2222-4222-8222-222222222222', path: 'b.md', name: 'b.md', kind: 'note', deletedAtUtc: '2026-09-27T00:00:00Z', retentionDays: 30 }],
            nextCursor: null
        });
        await loading;

        expect(service.trashEntries().map(entry => entry.path)).toEqual(['a.md', 'b.md']);
        expect(service.trashLoading()).toBe(false);
        http.verify();
    });

    it('exige reautenticação recente antes de esvaziar a lixeira', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();

        const empty = service.emptyTrash();
        const request = http.expectOne('/api/v1/vaults/vault-1/trash');
        expect(request.request.method).toBe('DELETE');
        request.flush({ code: 'reauthentication-required' },
            { status: 428, statusText: 'Precondition Required' });

        await expect(empty).resolves.toBe('reauthentication-required');
        http.verify();
    });

    it('limpa a listagem após esvaziar a lixeira', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();
        service.trashEntries.set([{
            id: '11111111-1111-4111-8111-111111111111',
            path: 'a.md',
            name: 'a.md',
            kind: 'note',
            deletedAtUtc: '2026-09-28T00:00:00Z',
            retentionDays: null,
            canRestoreToOriginal: true
        }]);

        const empty = service.emptyTrash();
        http.expectOne('/api/v1/vaults/vault-1/trash').flush({ removedOperations: 1, failedOperations: 0 });
        await empty;

        expect(service.trashEntries()).toEqual([]);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        http.verify();
    });

    it('pede destino explícito e mantém item na lixeira quando há colisão', async () => {
        const service = TestBed.inject(FolderExplorerService);
        const http = TestBed.inject(HttpTestingController);
        service.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();
        const entry = {
            id: '11111111-1111-4111-8111-111111111111',
            path: 'antigo/plano.md',
            name: 'plano.md',
            kind: 'note' as const,
            deletedAtUtc: '2026-09-28T00:00:00Z',
            retentionDays: null,
            canRestoreToOriginal: true
        };
        service.trashEntries.set([entry]);

        const restore = service.restoreFromTrash(entry, 'arquivo/plano.md');
        const request = http.expectOne('/api/v1/vaults/vault-1/trash/11111111-1111-4111-8111-111111111111');
        expect(request.request.method).toBe('PATCH');
        expect(request.request.body).toEqual({ path: 'arquivo/plano.md' });
        request.flush({ status: 409, code: 'trash-destination-exists' },
            { status: 409, statusText: 'Conflict' });

        await expect(restore).resolves.toContain('ocupado');
        expect(service.trashEntries()).toEqual([entry]);
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
