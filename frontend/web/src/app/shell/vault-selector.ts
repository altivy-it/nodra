import { Component, EventEmitter, Input, Output } from '@angular/core';
import { VaultSummary } from '../core/vaults.service';

@Component({
    selector: 'app-vault-selector',
    template: `
    <label>
      <span class="visually-hidden">Vault ativo</span>
      <select [value]="currentVaultId" [disabled]="loading" (change)="vaultSelected.emit($any($event.target).value)">
        @for (vault of vaults; track vault.id) {
          <option [value]="vault.id">{{ vault.name }}</option>
        }
      </select>
    </label>
    <span class="visually-hidden" aria-live="polite">Vault ativo: {{ currentVaultName }}</span>
  `,
    styles: [`
    :host { display: inline-flex; min-width: 0; align-items: center; }
    label { min-width: 0; }
    select { width: min(34vw, 260px); min-width: 120px; padding: 5px 24px 5px 7px; border: 1px solid var(--border); border-radius: var(--radius-sm); background: var(--surface-input); color: var(--text-normal); font: 500 13px var(--font-sans); }
    select:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
    .visually-hidden { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0; }
  `]
})
export class VaultSelector {
    @Input() vaults: VaultSummary[] = [];
    @Input() currentVaultId: string | null = null;
    @Input() loading = false;
    @Output() readonly vaultSelected = new EventEmitter<string>();

    get currentVaultName(): string {
        return this.vaults.find(vault => vault.id === this.currentVaultId)?.name ?? '';
    }
}