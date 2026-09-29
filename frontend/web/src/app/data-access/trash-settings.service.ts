import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TrashSettingsService {
    readonly retentionDays = signal<number | null>(null);
    readonly loading = signal(false);
    readonly saving = signal(false);
    readonly error = signal<string | null>(null);
    readonly saved = signal(false);

    constructor(private readonly http: HttpClient) { }

    async load(): Promise<void> {
        this.loading.set(true);
        this.error.set(null);
        try {
            const response = await firstValueFrom(this.http.get<{ retentionDays: number | null }>('/api/v1/me/trash-settings'));
            this.retentionDays.set(response.retentionDays);
        } catch {
            this.error.set('Não foi possível carregar o prazo de retenção.');
        } finally {
            this.loading.set(false);
        }
    }

    async save(value: number | null): Promise<void> {
        this.saving.set(true);
        this.saved.set(false);
        this.error.set(null);
        try {
            await firstValueFrom(this.http.put<void>('/api/v1/me/trash-settings', { retentionDays: value }));
            this.retentionDays.set(value);
            this.saved.set(true);
        } catch {
            this.error.set('Não foi possível salvar o prazo de retenção.');
        } finally {
            this.saving.set(false);
        }
    }
}
