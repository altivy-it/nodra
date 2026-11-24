import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { NdButton, NdEmptyState, NdIcon } from '.';

@Component({
    imports: [NdButton, NdEmptyState, NdIcon],
    template: `
        <nd-icon name="search" />
        <nd-icon name="inexistente" label="Buscar notas" />
        <button nd-button variant="primary" [loading]="loading()">Salvar</button>
        <nd-empty-state kind="error" title="Falha ao carregar" />
        <nd-empty-state kind="loading" title="Carregando" />
    `
})
class Host {
    readonly loading = signal(false);
}

describe('design system Nodra', () => {
    function render() {
        TestBed.configureTestingModule({ imports: [Host] });
        const fixture = TestBed.createComponent(Host);
        fixture.detectChanges();
        return fixture;
    }

    it('trata ícone sem rótulo como decorativo e ícone rotulado como imagem', () => {
        const [decorative, labelled] = render().nativeElement.querySelectorAll('nd-icon');

        expect(decorative.getAttribute('aria-hidden')).toBe('true');
        expect(labelled.getAttribute('role')).toBe('img');
        expect(labelled.getAttribute('aria-label')).toBe('Buscar notas');
        expect(labelled.querySelector('svg')).not.toBeNull();
    });

    it('desabilita o botão e sinaliza ocupado durante o carregamento', () => {
        const fixture = render();
        const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
        expect(button.classList).toContain('nd-btn--primary');
        expect(button.disabled).toBe(false);

        fixture.componentInstance.loading.set(true);
        fixture.detectChanges();

        expect(button.disabled).toBe(true);
        expect(button.getAttribute('aria-busy')).toBe('true');
        expect(button.textContent).toContain('Salvar');
    });

    it('anuncia erros como alerta e carregamento como status', () => {
        const roles = Array.from(render().nativeElement.querySelectorAll('nd-empty-state [role]'))
            .map(element => (element as HTMLElement).getAttribute('role'));

        expect(roles).toEqual(['alert', 'status']);
    });
});
