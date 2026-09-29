import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FolderExplorerComponent } from './folder-explorer.component';
import { VaultEntry } from '../../data-access/folder-explorer.service';

describe('FolderExplorerComponent', () => {
    it('virtualiza uma lista grande sem criar um botão por entrada', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        const entries = Array.from({ length: 1000 }, (_, index) => ({
            path: `nota-${index}.md`,
            name: `nota-${index}.md`,
            kind: 'note' as const,
            eTag: `v${index}`
        }));
        fixture.componentRef.setInput('vaultId', 'vault-1');
        fixture.detectChanges();

        TestBed.inject(HttpTestingController)
            .expectOne('/api/v1/vaults/vault-1/entries')
            .flush(entries);
        await fixture.whenStable();
        fixture.detectChanges();

        expect(fixture.componentInstance.rows()).toHaveLength(1000);
        expect(fixture.nativeElement.querySelectorAll('[role="treeitem"]').length).toBeLessThan(40);
    });

    it('exibe e anuncia os estados de classificação, metadados mínimos e links não resolvidos', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        fixture.componentRef.setInput('vaultId', 'vault-1');
        fixture.detectChanges();
        TestBed.inject(HttpTestingController)
            .expectOne('/api/v1/vaults/vault-1/entries')
            .flush([{
                path: 'nota.md',
                name: 'nota.md',
                kind: 'note',
                eTag: 'v1',
                isUnclassified: true,
                hasMinimumMetadata: false,
                hasUnresolvedLinks: true
            }]);
        await fixture.whenStable();
        fixture.detectChanges();

        const item = fixture.nativeElement.querySelector('[role="treeitem"]') as HTMLButtonElement;
        expect(item.getAttribute('aria-label')).toBe('nota.md (sem classificação, metadados mínimos ausentes, link não resolvido)');
        expect(item.textContent).toContain('!');
        expect(item.textContent).toContain('M');
        expect(item.textContent).toContain('L');
    });

    it('mantém a árvore de 20 mil entradas virtualizada ao navegar até o fim', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        const entries = Array.from({ length: 20_000 }, (_, index) => {
            const name = `nota-${String(index).padStart(5, '0')}.md`;
            return { path: name, name, kind: 'note' as const, eTag: `v${index}` };
        });
        fixture.componentRef.setInput('vaultId', 'vault-1');
        fixture.detectChanges();
        TestBed.inject(HttpTestingController).expectOne('/api/v1/vaults/vault-1/entries').flush(entries);
        await fixture.whenStable();
        fixture.detectChanges();
        const first = fixture.nativeElement.querySelector('[role="treeitem"]') as HTMLButtonElement;

        first.dispatchEvent(new KeyboardEvent('keydown', { key: 'End', bubbles: true, cancelable: true }));
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(fixture.componentInstance.rows()).toHaveLength(20_000);
        expect(fixture.nativeElement.querySelectorAll('[role="treeitem"]').length).toBeLessThan(40);
        expect(fixture.nativeElement.querySelector('[data-row-index="19999"]')).toBe(document.activeElement);
    });

    it('preserva e recarrega operações restantes quando o purge falha parcialmente', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const component = TestBed.createComponent(FolderExplorerComponent).componentInstance;
        const http = TestBed.inject(HttpTestingController);
        const remaining = {
            id: 'operation-1',
            path: 'nota.md',
            name: 'nota.md',
            kind: 'note' as const,
            deletedAtUtc: '2026-09-28T00:00:00Z',
            retentionDays: null,
            canRestoreToOriginal: true
        };
        component.explorer.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();

        const purge = component.explorer.emptyTrash();
        http.expectOne({ method: 'DELETE', url: '/api/v1/vaults/vault-1/trash' })
            .flush({ removedOperations: 2, failedOperations: 1 });
        await Promise.resolve();
        http.expectOne(request => request.method === 'GET' && request.url.endsWith('/trash'))
            .flush({ items: [remaining], nextCursor: null });
        const result = await purge;

        expect(result).toContain('2 operação(ões) removida(s), 1 com falha');
        expect(component.explorer.trashEntries()).toEqual([remaining]);
        http.verify();
    });

    it('retoma uma confirmação de purge somente com reautenticação recente do BFF', async () => {
        const storageSlot = 'altivy.trash-purge.pending-v1';
        window.sessionStorage.removeItem(storageSlot);
        window.sessionStorage.setItem(storageSlot, 'vault-1');
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        const http = TestBed.inject(HttpTestingController);
        fixture.componentRef.setInput('vaultId', 'vault-1');
        fixture.detectChanges();

        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        http.expectOne(request => request.method === 'GET' && request.url.endsWith('/trash'))
            .flush({ items: [{
                id: 'operation-1',
                path: 'nota.md',
                name: 'nota.md',
                kind: 'note',
                deletedAtUtc: '2026-09-28T00:00:00Z',
                retentionDays: null,
                canRestoreToOriginal: true
            }], nextCursor: null });
        await Promise.resolve();
        await Promise.resolve();
        const statusRequest = await vi.waitFor(() => http.expectOne('/bff/reauthenticate/status'));
        statusRequest.flush({
            authenticated: true,
            subject: 'subject-1',
            authenticatedAtUtc: new Date().toISOString()
        });
        const purgeRequest = await vi.waitFor(() =>
            http.expectOne({ method: 'DELETE', url: '/api/v1/vaults/vault-1/trash' }));
        purgeRequest.flush({ removedOperations: 1, failedOperations: 0 });
        const reloadRequest = await vi.waitFor(() => http.expectOne('/api/v1/vaults/vault-1/entries'));
        reloadRequest.flush([]);
        await fixture.whenStable();

        expect(fixture.componentInstance.trashOpen()).toBe(true);
        expect(fixture.componentInstance.purgeResult()).toBe('Lixeira esvaziada.');
        expect(window.sessionStorage.getItem(storageSlot)).toBeNull();
        window.sessionStorage.removeItem(storageSlot);
        http.verify();
    });

    it('usa a pasta selecionada como destino contextual', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        fixture.componentInstance.explorer.children.set(new Map<string, VaultEntry[]>([
            ['', [{ path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }]]
        ]));
        fixture.componentRef.setInput('selectedPath', 'projetos');
        fixture.componentInstance.toggleFolderForm();
        fixture.detectChanges();

        expect(fixture.componentInstance.folderParentPath).toBe('projetos');
        expect(fixture.nativeElement.textContent).toContain('Nova pasta em: projetos');
    });

    it('mantém a seleção e o foco lógico da árvore durante atualização manual', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        fixture.componentRef.setInput('vaultId', 'vault-1');
        fixture.detectChanges();
        const http = TestBed.inject(HttpTestingController);
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ]);
        await fixture.whenStable();
        fixture.detectChanges();
        const row = fixture.nativeElement.querySelector('[role="treeitem"]') as HTMLButtonElement;
        row.focus();
        fixture.componentInstance.scrollTop.set(35);

        fixture.componentInstance.reload();
        http.expectOne('/api/v1/vaults/vault-1/entries');
        fixture.detectChanges();

        expect(fixture.componentInstance.activePath()).toBe('projetos');
        expect(fixture.componentInstance.scrollTop()).toBe(35);
        expect(fixture.nativeElement.querySelector('[role="treeitem"]')).toBe(row);
        expect(document.activeElement).toBe(row);
    });

    it('move o foco entre os formulários inline e a linha ativa ao cancelar', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        const component = fixture.componentInstance;
        component.explorer.children.set(new Map([[ '', [
            { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
        ] ]]));
        fixture.detectChanges();
        const row = fixture.nativeElement.querySelector('[role="treeitem"]') as HTMLButtonElement;
        row.focus();

        component.toggleFolderForm();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(document.activeElement).toBe(fixture.nativeElement.querySelector('input'));

        component.toggleFolderForm();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(document.activeElement).toBe(row);

        component.openMoveForm();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(document.activeElement).toBe(fixture.nativeElement.querySelector('.move-folder-form input'));

        component.cancelMove();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(document.activeElement).toBe(row);
    });

    it('mostra pasta provisória desabilitada enquanto o PUT está pendente', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        const component = fixture.componentInstance;
        const http = TestBed.inject(HttpTestingController);
        component.explorer.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();

        const create = component.explorer.createFolder(null, 'rascunhos');
        const put = http.expectOne('/api/v1/vaults/vault-1/folders/rascunhos');
        fixture.detectChanges();
        const pending = fixture.nativeElement.querySelector('[role="treeitem"]') as HTMLButtonElement;
        expect(pending.disabled).toBe(true);
        expect(pending.getAttribute('aria-label')).toBe('rascunhos (criando)');
        expect(pending.textContent).toContain('criando');

        put.flush({ status: 409 }, { status: 409, statusText: 'Conflict' });
        await create;
        fixture.detectChanges();
        expect(fixture.nativeElement.querySelector('[role="treeitem"]')).toBeNull();
        http.verify();
    });

    it('exige confirmação antes de excluir e volta o foco lógico para a raiz', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        const component = fixture.componentInstance;
        const http = TestBed.inject(HttpTestingController);
        component.explorer.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await Promise.resolve();
        component.explorer.children.set(new Map([[
            '', [{ path: 'projetos', name: 'projetos', kind: 'folder' as const, eTag: null }]
        ]]));
        component.activePath.set('projetos');
        fixture.detectChanges();

        await component.deleteActiveFolder();
        expect(component.confirmingFolderDelete()).toBe(true);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(document.activeElement).toBe(fixture.nativeElement.querySelector('.folder-actions button'));
        const deleting = component.deleteActiveFolder();
        const deleteRequest = http.expectOne('/api/v1/vaults/vault-1/folders/projetos');
        expect(deleteRequest.request.method).toBe('DELETE');
        fixture.detectChanges();
        expect(fixture.nativeElement.querySelector('[role="treeitem"]')).toBeNull();
        expect(fixture.nativeElement.querySelector('[role="status"]')?.textContent).toContain('Movendo pasta para a lixeira');
        deleteRequest.flush(null, { status: 204, statusText: 'No Content' });
        await Promise.resolve();
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await deleting;

        expect(component.confirmingFolderDelete()).toBe(false);
        expect(component.activePath()).toBeNull();
        fixture.detectChanges();
        expect(fixture.nativeElement.querySelector('[role="status"]')).toBeNull();
        http.verify();
    });

    it('move uma pasta ao soltá-la sobre uma pasta válida e recusa destino descendente', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        const component = fixture.componentInstance;
        const http = TestBed.inject(HttpTestingController);
        component.explorer.activateVault('vault-1');
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        const source: VaultEntry = { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null };
        const child: VaultEntry = { path: 'projetos/filho', name: 'filho', kind: 'folder', eTag: null };
        const target: VaultEntry = { path: 'arquivo', name: 'arquivo', kind: 'folder', eTag: null };
        const transfer = { setData: vi.fn(), effectAllowed: '', dropEffect: '' };
        const event = { dataTransfer: transfer, preventDefault: vi.fn() } as unknown as DragEvent;
        const moved = vi.fn();
        component.folderMoved.subscribe(moved);

        component.startEntryDrag(source, event);
        component.allowFolderDrop(child, event);
        expect(component.dropTarget()).toBeNull();
        component.allowFolderDrop(target, event);
        expect(component.dropTarget()).toBe('arquivo');
        const drop = component.dropFolder(target, event);
        const request = http.expectOne('/api/v1/vaults/vault-1/folders/projetos');
        expect(request.request.method).toBe('PATCH');
        expect(request.request.body).toEqual({ path: 'arquivo/projetos' });
        request.flush({ path: 'arquivo/projetos' });
        await drop;
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);

        expect(moved).toHaveBeenCalledWith({ from: 'projetos', to: 'arquivo/projetos' });
        expect(component.dropTarget()).toBeNull();
        http.verify();
    });

    it('emite o movimento de uma nota salva para uma pasta sem mover a pasta', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const component = TestBed.createComponent(FolderExplorerComponent).componentInstance;
        const note: VaultEntry = { path: 'plano.md', name: 'plano.md', kind: 'note', eTag: 'v1' };
        const folder: VaultEntry = { path: 'arquivo', name: 'arquivo', kind: 'folder', eTag: null };
        const event = {
            dataTransfer: { setData: vi.fn(), effectAllowed: '', dropEffect: '' },
            preventDefault: vi.fn()
        } as unknown as DragEvent;
        const dropped = vi.fn();
        component.noteDropped.subscribe(dropped);

        component.startEntryDrag(note, event);
        component.allowFolderDrop(folder, event);
        await component.dropFolder(folder, event);

        expect(dropped).toHaveBeenCalledWith({ note, destination: 'arquivo/plano.md' });
        expect(event.preventDefault).toHaveBeenCalled();
        expect(component.draggedEntry()).toBeNull();
    });

    it('abre o menu pelo botão direito e cria nota na pasta selecionada', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        const component = fixture.componentInstance;
        component.explorer.children.set(new Map<string, VaultEntry[]>([[
            '', [{ path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }]
        ]]));
        fixture.detectChanges();
        const created = vi.fn();
        component.noteCreated.subscribe(created);
        const row = fixture.nativeElement.querySelector('[role="treeitem"]') as HTMLButtonElement;

        row.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true, clientX: 50, clientY: 80 }));
        fixture.detectChanges();
        await fixture.whenStable();
        const menu = fixture.nativeElement.querySelector('[role="menu"]') as HTMLElement;
        expect(menu).toBeTruthy();
        expect(component.activePath()).toBe('projetos');
        expect(document.activeElement?.textContent).toBe('Nova nota');

        (menu.querySelector('[role="menuitem"]') as HTMLButtonElement).click();
        fixture.detectChanges();
        expect(created).toHaveBeenCalledWith('projetos');
        expect(component.contextEntry()).toBeNull();
    });

    it('abre o menu por Shift+F10, navega e devolve foco com Escape', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        fixture.componentInstance.explorer.children.set(new Map<string, VaultEntry[]>([[
            '', [{ path: 'plano.md', name: 'plano.md', kind: 'note', eTag: 'v1' }]
        ]]));
        fixture.detectChanges();
        const row = fixture.nativeElement.querySelector('[role="treeitem"]') as HTMLButtonElement;
        row.focus();
        row.dispatchEvent(new KeyboardEvent('keydown', { key: 'F10', shiftKey: true, bubbles: true, cancelable: true }));
        fixture.detectChanges();
        await fixture.whenStable();
        const menu = fixture.nativeElement.querySelector('[role="menu"]') as HTMLElement;
        expect(document.activeElement?.textContent).toBe('Abrir nota');

        menu.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }));
        expect(document.activeElement?.textContent).toBe('Mover para a lixeira');
        menu.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
        fixture.detectChanges();
        await fixture.whenStable();
        expect(fixture.componentInstance.contextEntry()).toBeNull();
        expect(document.activeElement).toBe(row);
    });

    it('aciona exclusão pelo menu sem remover a pasta antes da confirmação', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        const component = fixture.componentInstance;
        component.explorer.children.set(new Map<string, VaultEntry[]>([[
            '', [{ path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }]
        ]]));
        fixture.detectChanges();
        const row = fixture.nativeElement.querySelector('[role="treeitem"]') as HTMLButtonElement;

        row.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
        fixture.detectChanges();
        (fixture.nativeElement.querySelector('[role="menuitem"]:last-child') as HTMLButtonElement).click();
        fixture.detectChanges();
        await fixture.whenStable();

        expect(component.confirmingFolderDelete()).toBe(true);
        expect(document.activeElement?.textContent?.trim()).toBe('Confirmar movimentação');
        TestBed.inject(HttpTestingController).expectNone('/api/v1/vaults/vault-1/folders/projetos');
    });

    it('lista a lixeira e pede destino alternativo com foco ao caminho original ocupado', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        const component = fixture.componentInstance;
        const http = TestBed.inject(HttpTestingController);
        fixture.componentRef.setInput('vaultId', 'vault-1');
        fixture.detectChanges();
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await fixture.whenStable();

        const entry = {
            id: '11111111-1111-4111-8111-111111111111',
            path: 'antigo/plano.md',
            name: 'plano.md',
            kind: 'note' as const,
            deletedAtUtc: '2026-09-28T00:00:00Z',
            retentionDays: null,
            canRestoreToOriginal: false
        };
        component.openTrash();
        http.expectOne('/api/v1/vaults/vault-1/trash?limit=100').flush({ items: [entry], nextCursor: null });
        await fixture.whenStable();
        fixture.detectChanges();

        expect(fixture.nativeElement.querySelector('[role="tree"]')).toBeNull();
        expect(fixture.nativeElement.textContent).toContain('plano.md');
        expect(fixture.nativeElement.querySelector('.trash-destination-warning')?.getAttribute('title')).toContain('original está ocupado');
        const restoreButton = fixture.nativeElement.querySelector('.trash-restore-button') as HTMLButtonElement;
        restoreButton.click();
        fixture.detectChanges();
        await fixture.whenStable();
        const destination = fixture.nativeElement.querySelector('.trash-restore-form input') as HTMLInputElement;
        expect(destination.placeholder).toBe('antigo/plano.md');
        expect(destination.value).toBe('');
        expect(document.activeElement).toBe(destination);

        destination.value = 'arquivo/plano.md';
        destination.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        const form = fixture.nativeElement.querySelector('.trash-restore-form') as HTMLFormElement;
        form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        const request = http.expectOne('/api/v1/vaults/vault-1/trash/11111111-1111-4111-8111-111111111111');
        request.flush({ status: 409, code: 'trash-destination-exists' }, { status: 409, statusText: 'Conflict' });
        await fixture.whenStable();
        fixture.detectChanges();

        expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('ocupado');
        expect(component.explorer.trashEntries()).toEqual([entry]);
        http.verify();
    });

    it('mantém Tab e Shift+Tab dentro do menu contextual', async () => {
        await TestBed.configureTestingModule({
            imports: [FolderExplorerComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()]
        }).compileComponents();
        const fixture = TestBed.createComponent(FolderExplorerComponent);
        fixture.componentInstance.explorer.children.set(new Map<string, VaultEntry[]>([[
            '', [{ path: 'plano.md', name: 'plano.md', kind: 'note', eTag: 'v1' }]
        ]]));
        fixture.detectChanges();
        const row = fixture.nativeElement.querySelector('[role="treeitem"]') as HTMLButtonElement;
        row.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
        fixture.detectChanges();
        await fixture.whenStable();
        const menu = fixture.nativeElement.querySelector('[role="menu"]') as HTMLElement;
        const items = [...menu.querySelectorAll<HTMLButtonElement>('[role="menuitem"]')];
        const backwards = new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true, cancelable: true });

        items[0].dispatchEvent(backwards);
        expect(backwards.defaultPrevented).toBe(true);
        expect(document.activeElement).toBe(items.at(-1));

        const forwards = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
        items.at(-1)?.dispatchEvent(forwards);
        expect(forwards.defaultPrevented).toBe(true);
        expect(document.activeElement).toBe(items[0]);
    });
});
