import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { SessionService } from './session.service';

describe('SessionService', () => {
    it('carrega uma sessão anônima ao iniciar', async () => {
        const { service, http } = createService();

        http.expectOne('/bff/session').flush({ authenticated: false, subject: null, name: null });

        await vi.waitFor(() => expect(service.loading()).toBe(false));
        expect(service.loading()).toBe(false);
        expect(service.error()).toBe(false);
        expect(service.session()).toEqual({ authenticated: false, subject: null, name: null });
        http.verify();
    });

    it('expõe a identidade retornada pelo BFF', async () => {
        const { service, http } = createService();

        http.expectOne('/bff/session').flush({ authenticated: true, subject: 'user-123', name: 'Ada' });

        await vi.waitFor(() => expect(service.loading()).toBe(false));
        expect(service.session()).toEqual({ authenticated: true, subject: 'user-123', name: 'Ada' });
        http.verify();
    });

    it('permite nova consulta após uma falha do BFF', async () => {
        const { service, http } = createService();

        http.expectOne('/bff/session').flush('indisponível', { status: 503, statusText: 'Service Unavailable' });

        await vi.waitFor(() => expect(service.loading()).toBe(false));
        expect(service.loading()).toBe(false);
        expect(service.error()).toBe(true);

        const retry = service.load();
        http.expectOne('/bff/session').flush({ authenticated: true, subject: 'user-123', name: 'Ada' });
        await retry;

        expect(service.error()).toBe(false);
        expect(service.session()?.authenticated).toBe(true);
        http.verify();
    });

    it('encerra a sessão usando o token antiforgery do BFF', async () => {
        const { service, http } = createService();
        http.expectOne('/bff/session').flush({ authenticated: true, subject: 'user-123', name: 'Ada' });
        await vi.waitFor(() => expect(service.loading()).toBe(false));
        const submit = vi.spyOn(HTMLFormElement.prototype, 'submit').mockImplementation(() => { });

        const logout = service.logout();
        http.expectOne('/bff/antiforgery').flush({ requestToken: 'synthetic-antiforgery-token' });
        await logout;

        const form = submit.mock.instances[0] as HTMLFormElement;
        expect(form.method).toBe('post');
        expect(form.action).toContain('/bff/logout');
        expect(form.querySelector('input')).toMatchObject({
            name: '__RequestVerificationToken',
            value: 'synthetic-antiforgery-token'
        });
        expect(service.loggingOut()).toBe(false);
        expect(service.logoutError()).toBe(false);
        http.verify();
        submit.mockRestore();
    });

    function createService(): { service: SessionService; http: HttpTestingController } {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(), provideHttpClientTesting()]
        });

        return {
            service: TestBed.inject(SessionService),
            http: TestBed.inject(HttpTestingController)
        };
    }
});
