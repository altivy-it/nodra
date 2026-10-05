import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { App } from './app';
import { routes } from './app.routes';
import { VaultRealtimeService } from './core/vault-realtime.service';
import { SessionService } from './core/session.service';

const realtime = {
  status: signal<'disconnected' | 'connecting' | 'connected' | 'error'>('disconnected'),
  joinVaultAsync: vi.fn().mockResolvedValue(undefined),
  disconnectAsync: vi.fn().mockResolvedValue(undefined)
};

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter(routes),
        { provide: VaultRealtimeService, useValue: realtime }
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render anonymous session state', async () => {
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/bff/session').flush({ authenticated: false, subject: null, name: null });
    await fixture.whenStable();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('main')?.textContent).toContain('Entre para abrir seu workspace');
    http.verify();
  });

  it('should allow an authenticated session without vaults to log out', async () => {
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    const logout = vi.spyOn(TestBed.inject(SessionService), 'logout').mockResolvedValue(undefined);

    http.expectOne('/bff/session').flush({ authenticated: true, subject: 'user-empty', name: 'Pessoa' });
    fixture.detectChanges();
    await fixture.whenStable();
    await vi.waitFor(() => {
      http.expectOne(request => request.url === '/api/v1/vaults').flush({ items: [], nextCursor: null });
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Nenhum vault disponível.');
    const button = compiled.querySelector('button') as HTMLButtonElement;
    expect(button.textContent).toContain('Encerrar sessão');
    button.click();
    expect(logout).toHaveBeenCalledOnce();
    http.verify();
  });

  it('should load notes and show the editor for an authenticated session', async () => {
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);

    http.expectOne('/bff/session').flush({ authenticated: true, subject: 'user-1', name: 'Pessoa' });
    fixture.detectChanges();
    await fixture.whenStable();
    await vi.waitFor(() => {
      http.expectOne(request => request.url === '/api/v1/vaults').flush({
        items: [{ id: 'vault-1', name: 'Principal' }],
        nextCursor: null
      });
    });
    await vi.waitFor(() => {
      http.expectOne('/api/v1/vaults/vault-1/notes').flush([{ path: 'projetos/plano.md', eTag: 'version-1' }]);
    });
    await vi.waitFor(() => {
      http.expectOne('/api/v1/vaults/vault-1/entries').flush([
        { path: 'projetos', name: 'projetos', kind: 'folder', eTag: null }
      ]);
    });
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('[role="treeitem"]') as HTMLButtonElement).click();
    await vi.waitFor(() => {
      http.expectOne('/api/v1/vaults/vault-1/entries?parentPath=projetos').flush([
        { path: 'projetos/plano.md', name: 'plano.md', kind: 'note', eTag: 'version-1' }
      ]);
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('projetos');
    expect(compiled.textContent).toContain('plano.md');
    expect(compiled.textContent).toContain('Principal');
    expect(Array.from(compiled.querySelectorAll('button')).some(button => button.textContent?.includes('Nova nota'))).toBe(true);
    http.verify();
  });
});
