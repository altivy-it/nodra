import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { computed, Inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { appPath } from './app-base-path';

export interface BffSession {
    authenticated: boolean;
    subject: string | null;
    name: string | null;
}

export type SessionState = 'loading' | 'error' | 'anonymous' | 'authenticated' | 'expired';

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
    readonly expired = signal(false);
    readonly state = computed<SessionState>(() => {
        if (this.loading()) return 'loading';
        if (this.error()) return 'error';
        if (this.expired()) return 'expired';
        return this.session()?.authenticated ? 'authenticated' : 'anonymous';
    });

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
            this.expired.set(false);
        } catch {
            this.error.set(true);
        } finally {
            this.loading.set(false);
        }
    }

    /** Marca a sessão como expirada depois de um 401; só uma sessão autenticada pode expirar. */
    expire(): void {
        if (this.session()?.authenticated) this.expired.set(true);
    }

    /**
     * URL de login no BFF. Fora da raiz da aplicação, preserva o caminho atual em
     * `returnUrl`; o BFF aceita somente caminhos locais (`LocalRedirect`).
     */
    loginUrl(): string {
        const login = appPath('/bff/login', this.document.baseURI);
        const location = this.document.location;
        const current = `${location?.pathname ?? ''}${location?.search ?? ''}`;
        const root = `${new URL(this.document.baseURI).pathname.replace(/\/+$/, '')}/`;
        return current && current !== root && current !== root.slice(0, -1)
            ? `${login}?returnUrl=${encodeURIComponent(current)}`
            : login;
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
