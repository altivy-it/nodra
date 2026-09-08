import { TestBed } from '@angular/core/testing';
import { VaultSelector } from './vault-selector';

describe('VaultSelector', () => {
    it('expõe o vault ativo e emite a troca pelo controle nativo', async () => {
        await TestBed.configureTestingModule({ imports: [VaultSelector] }).compileComponents();
        const fixture = TestBed.createComponent(VaultSelector);
        fixture.componentInstance.vaults = [
            { id: 'vault-1', name: 'Principal' },
            { id: 'vault-2', name: 'Projetos' }
        ];
        fixture.componentInstance.currentVaultId = 'vault-1';
        const selected: string[] = [];
        fixture.componentInstance.vaultSelected.subscribe(value => selected.push(value));
        fixture.detectChanges();

        const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
        select.value = 'vault-2';
        select.dispatchEvent(new Event('change'));

        expect(selected).toEqual(['vault-2']);
        expect(fixture.nativeElement.textContent).toContain('Vault ativo: Principal');
    });
});