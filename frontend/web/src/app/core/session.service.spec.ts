import { DOCUMENT } from '@angular/common';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { sessionExpiryInterceptor } from './session-expiry.interceptor';
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

    it('marca como expirada uma sessão autenticada após 401 de outra chamada', async () => {
        const { service, http } = createService();
        http.expectOne('/bff/session').flush({ authenticated: true, subject: 'user-123', name: 'Ada' });
        await vi.waitFor(() => expect(service.state()).toBe('authenticated'));

        TestBed.inject(HttpClient).get('/api/v1/vaults').subscribe({ error: () => { } });
        http.expectOne('/api/v1/vaults').flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(service.state()).toBe('expired');
        const reload = service.load();
        http.expectOne('/bff/session').flush({ authenticated: true, subject: 'user-123', name: 'Ada' });
        await reload;
        expect(service.state()).toBe('authenticated');
        http.verify();
    });

    it('não expira sessão anônima nem reage a 401 da própria leitura da sessão', async () => {
        const { service, http } = createService();
        http.expectOne('/bff/session').flush({ authenticated: false, subject: null, name: null });
        await vi.waitFor(() => expect(service.state()).toBe('anonymous'));

        TestBed.inject(HttpClient).get('/api/v1/vaults').subscribe({ error: () => { } });
        http.expectOne('/api/v1/vaults').flush(null, { status: 401, statusText: 'Unauthorized' });
        const reload = service.load();
        http.expectOne('/bff/session').flush(null, { status: 401, statusText: 'Unauthorized' });
        await reload;

        expect(service.expired()).toBe(false);
        expect(service.state()).toBe('error');
        http.verify();
    });

    it('gera a URL de login sem returnUrl na raiz e com o caminho atual fora dela', () => {
        const root = createService(fakeDocument('/nodra/', '')).service;
        expect(root.loginUrl()).toBe('/nodra/bff/login');

        TestBed.resetTestingModule();
        const deep = createService(fakeDocument('/nodra/vaults/a', '?nota=x')).service;
        expect(deep.loginUrl()).toBe(`/nodra/bff/login?returnUrl=${encodeURIComponent('/nodra/vaults/a?nota=x')}`);
    });

    function fakeDocument(pathname: string, search: string): Document {
        return { baseURI: 'https://nodra.test/nodra/', location: { pathname, search } } as unknown as Document;
    }

    function createService(doc?: Document): { service: SessionService; http: HttpTestingController } {
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptors([sessionExpiryInterceptor])),
                provideHttpClientTesting(),
                ...(doc ? [{ provide: DOCUMENT, useValue: doc }] : [])
            ]
        });

        return {
            service: TestBed.inject(SessionService),
            http: TestBed.inject(HttpTestingController)
        };
    }
});
