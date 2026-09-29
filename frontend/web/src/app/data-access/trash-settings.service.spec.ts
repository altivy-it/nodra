import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TrashSettingsService } from './trash-settings.service';

describe('TrashSettingsService', () => {
    beforeEach(() => TestBed.configureTestingModule({
        providers: [provideHttpClient(), provideHttpClientTesting()]
    }));

    it('carrega e salva o parâmetro por sessão same-origin', async () => {
        const service = TestBed.inject(TrashSettingsService);
        const http = TestBed.inject(HttpTestingController);
        const loading = service.load();
        http.expectOne('/api/v1/me/trash-settings').flush({ retentionDays: 30 });
        await loading;
        expect(service.retentionDays()).toBe(30);

        const saving = service.save(null);
        const request = http.expectOne('/api/v1/me/trash-settings');
        expect(request.request.body).toEqual({ retentionDays: null });
        request.flush(null);
        await saving;
        expect(service.retentionDays()).toBeNull();
        expect(service.saved()).toBe(true);
        http.verify();
    });

    it('expõe erro de indisponibilidade sem perder o valor atual', async () => {
        const service = TestBed.inject(TrashSettingsService);
        const http = TestBed.inject(HttpTestingController);
        service.retentionDays.set(14);
        const loading = service.load();
        http.expectOne('/api/v1/me/trash-settings').flush(null, { status: 503, statusText: 'Unavailable' });
        await loading;
        expect(service.retentionDays()).toBe(14);
        expect(service.error()).toContain('carregar');
        http.verify();
    });
});
