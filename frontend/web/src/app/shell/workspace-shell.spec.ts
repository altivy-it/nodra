import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { routes } from '../app.routes';
import { WorkspaceShell } from './workspace-shell';

describe('WorkspaceShell', () => {
  it('apresenta a marca Altivy Nodra e titulos de rota Nodra', async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShell],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();
    const fixture = TestBed.createComponent(WorkspaceShell);
    fixture.componentInstance.userName = 'Pessoa';
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('footer')?.textContent).toContain('Altivy Nodra');
    expect(fixture.nativeElement.querySelector('.brand-mark svg')).not.toBeNull();
    const activities = fixture.nativeElement.querySelectorAll('.activity-bar button');
    expect(activities[0].getAttribute('aria-label')).toBe('Arquivos');
    expect(activities[0].querySelector('nd-icon svg')).not.toBeNull();
    expect(routes[0].title).toBe('Nodra');
    expect(routes[1].title).toBe('Nodra Workspace');
  });

  it('mostra o ícone do tema atual e liga o usuário ao console de conta', async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShell],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();
    const fixture = TestBed.createComponent(WorkspaceShell);
    fixture.componentInstance.userName = 'Pessoa';
    fixture.componentInstance.workspace.setTheme('dark');
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const themeButton = element.querySelector<HTMLButtonElement>('button[aria-label^="Alternar tema"]')!;
    expect(themeButton.getAttribute('aria-label')).toBe('Alternar tema (atual: Escuro)');
    expect(themeButton.querySelector('nd-icon')?.getAttribute('data-icon')).toBe('moon');

    themeButton.click();
    fixture.detectChanges();
    expect(themeButton.querySelector('nd-icon')?.getAttribute('data-icon')).toBe('contrast');

    const account = element.querySelector<HTMLAnchorElement>('a.account')!;
    expect(account.textContent).toContain('Pessoa');
    expect(new URL(account.href).pathname).toMatch(/\/bff\/account$/);
  });

  it('redimensiona os painéis por teclado respeitando os limites', async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShell],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();
    const fixture = TestBed.createComponent(WorkspaceShell);
    fixture.componentInstance.userName = 'Pessoa';
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.resizeFromKeyboard(new KeyboardEvent('keydown', { key: 'End' }), 'primary');
    component.resizeFromKeyboard(new KeyboardEvent('keydown', { key: 'Home' }), 'context');

    expect(component.workspace.primaryPanelWidth()).toBe(420);
    expect(component.workspace.contextPanelWidth()).toBe(240);
  });

  it('redimensiona o explorador pelo deslocamento do ponteiro', async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShell],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();
    const fixture = TestBed.createComponent(WorkspaceShell);
    fixture.componentInstance.userName = 'Pessoa';
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.startResize(
      {
        clientX: 100,
        pointerId: 1,
        currentTarget: { setPointerCapture() {} },
      } as unknown as PointerEvent,
      'primary',
    );
    component.resizeFromPointer({ clientX: 160 } as PointerEvent);
    component.finishResize();

    expect(component.workspace.primaryPanelWidth()).toBe(340);
  });

  it('retorna ao módulo anterior ao fechar a ajuda', async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShell],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();
    const fixture = TestBed.createComponent(WorkspaceShell);
    fixture.componentInstance.userName = 'Pessoa';
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.selectModule('settings');
    component.selectModule('help');
    component.closeHelp();

    expect(component.workspace.activeModule()).toBe('settings');
  });

  it('não repete a carga da ajuda em loop após falha e mantém a saída disponível', async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShell],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();
    const fixture = TestBed.createComponent(WorkspaceShell);
    fixture.componentInstance.userName = 'Pessoa';
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);

    fixture.componentInstance.selectModule('help');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.close-help')).not.toBeNull();
    http.expectOne(request => request.url.endsWith('/help.md')).flush('x', { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();
    fixture.detectChanges();

    http.expectNone(request => request.url.endsWith('/help.md'));
    expect(fixture.nativeElement.textContent).toContain('Não foi possível carregar a ajuda local.');
    fixture.nativeElement.querySelector('.close-help').click();
    expect(fixture.componentInstance.workspace.activeModule()).not.toBe('help');
  });

  it('abre a ajuda pelo atalho e devolve o foco para a área central', async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShell],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();
    const fixture = TestBed.createComponent(WorkspaceShell);
    fixture.componentInstance.userName = 'Pessoa';
    fixture.detectChanges();

    const component = fixture.componentInstance;
    const event = new KeyboardEvent('keydown', {
      cancelable: true,
      ctrlKey: true,
      shiftKey: true,
      key: '?',
    });
    component.handleShortcut(event);

    expect(event.defaultPrevented).toBe(true);
    expect(component.workspace.activeModule()).toBe('help');
    expect(document.activeElement).toBe(fixture.nativeElement.querySelector('.content'));
  });

  it('move o foco roving da barra de atividades por Home e End', async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShell],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();
    const fixture = TestBed.createComponent(WorkspaceShell);
    fixture.componentInstance.userName = 'Pessoa';
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.moveActivity(new KeyboardEvent('keydown', { key: 'End' }), 'files');
    expect(component.workspace.activeModule()).toBe('settings');
    expect(document.activeElement?.getAttribute('data-activity')).toBe('settings');

    component.moveActivity(new KeyboardEvent('keydown', { key: 'Home' }), 'settings');
    expect(component.workspace.activeModule()).toBe('files');
    expect(document.activeElement?.getAttribute('data-activity')).toBe('files');
  });
});
