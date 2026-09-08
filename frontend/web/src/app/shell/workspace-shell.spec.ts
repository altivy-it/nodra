import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { routes } from '../app.routes';
import { WorkspaceShell } from './workspace-shell';

describe('WorkspaceShell', () => {
    it('redimensiona os painéis por teclado respeitando os limites', async () => {
        await TestBed.configureTestingModule({
            imports: [WorkspaceShell],
            providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)]
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
            providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)]
        }).compileComponents();
        const fixture = TestBed.createComponent(WorkspaceShell);
        fixture.componentInstance.userName = 'Pessoa';
        fixture.detectChanges();

        const component = fixture.componentInstance;
        component.startResize({ clientX: 100, pointerId: 1, currentTarget: { setPointerCapture() { } } } as unknown as PointerEvent, 'primary');
        component.resizeFromPointer({ clientX: 160 } as PointerEvent);
        component.finishResize();

        expect(component.workspace.primaryPanelWidth()).toBe(340);
    });

    it('retorna ao módulo anterior ao fechar a ajuda', async () => {
        await TestBed.configureTestingModule({
            imports: [WorkspaceShell],
            providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)]
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
});
