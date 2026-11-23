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
import { NotesService } from './core/notes.service';
import { FolderExplorerService } from './data-access/folder-explorer.service';

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

  describe('salvamento automático', () => {
    function setup() {
      const app = TestBed.createComponent(App).componentInstance as unknown as {
        save(path: string, content: string): void;
        flushPendingSave(): Promise<void>;
      };
      const notes = TestBed.inject(NotesService);
      const explorer = TestBed.inject(FolderExplorerService);
      const reload = vi.spyOn(explorer, 'reload').mockImplementation(() => undefined);
      const http = TestBed.inject(HttpTestingController);
      notes.activateVault('vault-1', true);
      return { app, notes, reload, http };
    }

    const noteUrl = '/api/v1/vaults/vault-1/notes/nova.md';

    it('envia PUT com o ETag criado quando a nota nova é editada durante o POST', async () => {
      const { app, notes, reload, http } = setup();
      notes.newNote();
      app.save('nova', '# Nova\n');
      const flushing = app.flushPendingSave();
      const create = http.expectOne(request => request.method === 'POST' && request.url === noteUrl);
      app.save('nova', '# Nova\ntexto');
      create.flush(null, { status: 201, statusText: 'Created' });
      await vi.waitFor(() => http.expectOne(request => request.method === 'GET' && request.url === noteUrl)
        .flush({ path: 'nova.md', content: '# Nova\n' }, { headers: { ETag: '"v1"' } }));
      const update = await vi.waitFor(() => http.expectOne(request => request.method === 'PUT' && request.url === noteUrl));
      expect(update.request.headers.get('If-Match')).toBe('"v1"');
      expect(update.request.body).toEqual({ content: '# Nova\ntexto' });
      update.flush(null, { status: 204, statusText: 'No Content' });
      await vi.waitFor(() => http.expectOne(request => request.method === 'GET' && request.url === noteUrl)
        .flush({ path: 'nova.md', content: '# Nova\ntexto' }, { headers: { ETag: '"v2"' } }));
      await flushing;
      expect(notes.error()).toBeNull();
      expect(notes.selected()?.eTag).toBe('v2');
      expect(reload).toHaveBeenCalledTimes(1);
      http.match('/bff/session');
      http.verify();
    });

    it('mantém o conflito quando a versão enfileirada não veio do salvamento anterior', async () => {
      const { app, notes, http } = setup();
      notes.selected.set({ path: 'nova.md', content: 'a', eTag: 'v1' });
      app.save('nova.md', 'ab');
      const flushing = app.flushPendingSave();
      const first = http.expectOne(request => request.method === 'PUT' && request.url === noteUrl);
      expect(first.request.headers.get('If-Match')).toBe('"v1"');
      first.flush({ title: 'Conflito' }, { status: 412, statusText: 'Precondition Failed' });
      await flushing;
      expect(notes.error()).toContain('Não foi possível salvar');
      http.match('/bff/session');
      http.verify();
    });
  });
});
