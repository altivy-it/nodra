import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { appPath } from './app-base-path';

export interface BffSession {
    authenticated: boolean;
    subject: string | null;
    name: string | null;
}

interface AntiforgeryTokenResponse {
    requestToken: string;
}

@Injectable({ providedIn: 'root' })
export class SessionService {
    readonly session = signal<BffSession | null>(null);
    readonly loading = signal(true);
    readonly error = signal(false);
    readonly loggingOut = signal(false);
    readonly logoutError = signal(false);

    constructor(
        private readonly http: HttpClient,
        @Inject(DOCUMENT) private readonly document: Document) {
        void this.load();
    }

    async load(): Promise<void> {
        this.loading.set(true);
        this.error.set(false);

        try {
            const session = await firstValueFrom(this.http.get<BffSession>('/bff/session'));
            this.session.set(session);
        } catch {
            this.error.set(true);
        } finally {
            this.loading.set(false);
        }
    }

    async logout(): Promise<void> {
        this.loggingOut.set(true);
        this.logoutError.set(false);

        try {
            const antiforgery = await firstValueFrom(this.http.get<AntiforgeryTokenResponse>('/bff/antiforgery'));
            const form = this.document.createElement('form');
            form.method = 'post';
            form.action = appPath('/bff/logout', this.document.baseURI);

            const token = this.document.createElement('input');
            token.type = 'hidden';
            token.name = '__RequestVerificationToken';
            token.value = antiforgery.requestToken;
            form.appendChild(token);
            this.document.body.appendChild(form);
            form.submit();
            form.remove();
        } catch {
            this.logoutError.set(true);
        } finally {
            this.loggingOut.set(false);
        }
    }
}
