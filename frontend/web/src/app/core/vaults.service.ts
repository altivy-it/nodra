import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface VaultSummary {
    id: string;
    name: string;
    /** Vault de sistema que recusa qualquer escrita; edições ficam apenas em memória. */
    readOnly?: boolean;
}

interface VaultPage {
    items: VaultSummary[];
    nextCursor: string | null;
}

@Injectable({ providedIn: 'root' })
export class VaultsService {
    readonly vaults = signal<VaultSummary[]>([]);
    readonly current = signal<VaultSummary | null>(null);
    readonly loading = signal(false);
    readonly error = signal<string | null>(null);
    private subject: string | null = null;
    private loadGeneration = 0;

    constructor(private readonly http: HttpClient) { }

    async load(subject: string): Promise<void> {
        const generation = ++this.loadGeneration;
        this.loading.set(true);
        this.error.set(null);
        this.subject = subject;

        try {
            const vaults: VaultSummary[] = [];
            let cursor: string | null = null;

            do {
                let params = new HttpParams().set('limit', 100);
                if (cursor) params = params.set('cursor', cursor);
                const page = await firstValueFrom(this.http.get<VaultPage>('/api/v1/vaults', { params }));
                vaults.push(...page.items);
                cursor = page.nextCursor;
            } while (cursor);

            if (generation !== this.loadGeneration || subject !== this.subject) return;
            this.vaults.set(vaults);
            if (this.current() && !vaults.some(vault => vault.id === this.current()?.id)) {
                this.current.set(null);
            }
        } catch {
            if (generation !== this.loadGeneration || subject !== this.subject) return;
            this.vaults.set([]);
            this.current.set(null);
            this.error.set('Não foi possível carregar os vaults autorizados.');
        } finally {
            if (generation === this.loadGeneration && subject === this.subject) this.loading.set(false);
        }
    }

    resolve(requestedVaultId: string | null): VaultSummary | null {
        const requested = this.find(requestedVaultId);
        if (requested) return requested;

        const lastVaultId = this.subject ? this.readLastVault(this.subject) : null;
        return this.find(lastVaultId) ?? this.vaults()[0] ?? null;
    }

    select(vaultId: string): boolean {
        const vault = this.find(vaultId);
        if (!vault) return false;

        this.current.set(vault);
        if (this.subject) this.writeLastVault(this.subject, vault.id);
        return true;
    }

    private find(vaultId: string | null): VaultSummary | null {
        return this.vaults().find(vault => vault.id === vaultId) ?? null;
    }

    private readLastVault(subject: string): string | null {
        try { return localStorage.getItem(this.storageKey(subject)); } catch { return null; }
    }

    private writeLastVault(subject: string, vaultId: string): void {
        try { localStorage.setItem(this.storageKey(subject), vaultId); } catch { }
    }

    private storageKey(subject: string): string {
        return `altivy:last-vault:v1:${subject}`;
    }
}
