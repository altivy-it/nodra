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
        const deleting = component.deleteActiveFolder();
        const deleteRequest = http.expectOne('/api/v1/vaults/vault-1/folders/projetos');
        expect(deleteRequest.request.method).toBe('DELETE');
        deleteRequest.flush(null, { status: 204, statusText: 'No Content' });
        await Promise.resolve();
        http.expectOne('/api/v1/vaults/vault-1/entries').flush([]);
        await deleting;

        expect(component.confirmingFolderDelete()).toBe(false);
        expect(component.activePath()).toBeNull();
        http.verify();
    });
});