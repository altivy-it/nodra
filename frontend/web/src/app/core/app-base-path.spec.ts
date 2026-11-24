import { DOCUMENT } from '@angular/common';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { appBasePathInterceptor, appPath } from './app-base-path';

describe('app base path', () => {
    it('preserves root paths for mocked and local tests', () => {
        expect(appPath('/bff/login', 'http://localhost/')).toBe('/bff/login');
    });

    it('prefixes browser navigation paths when hosted below a base path', () => {
        expect(appPath('/bff/reauthenticate', 'https://localhost:7443/nodra/'))
            .toBe('/nodra/bff/reauthenticate');
    });

    it('prefixes only BFF and API requests using the document base', async () => {
        const document = window.document.implementation.createHTMLDocument();
        const base = document.createElement('base');
        base.href = 'https://localhost:7443/nodra/';
        document.head.append(base);
        TestBed.configureTestingModule({
            providers: [
                { provide: DOCUMENT, useValue: document },
                provideHttpClient(withInterceptors([appBasePathInterceptor])),
                provideHttpClientTesting()
            ]
        });

        const http = TestBed.inject(HttpTestingController);
        const client = TestBed.inject(HttpClient);
        const response = firstValueFrom(client.get('/bff/session'));
        const request = http.expectOne('/nodra/bff/session');
        request.flush({ authenticated: false });
        await response;
        http.verify();
    });
});