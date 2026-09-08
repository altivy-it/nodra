import { describe, expect, it } from 'vitest';
import { ActivityRegistry } from './activity-registry';

describe('ActivityRegistry', () => {
    it('ordena e oculta módulos indisponíveis', () => {
        const registry = new ActivityRegistry();
        registry.items[1].available = false;
        registry.items[0].order = 99;

        expect(registry.availableItems().map(item => item.id)).toEqual(['tags', 'graph', 'help', 'settings', 'files']);
    });

    it('mantém identificadores únicos para foco e roteamento', () => {
        const ids = new ActivityRegistry().availableItems().map(item => item.id);
        expect(new Set(ids).size).toBe(ids.length);
    });

    it('declara painel para cada módulo sem acoplar o registro ao vault ativo', () => {
        for (const item of new ActivityRegistry().items) {
            expect(['primary', 'context', 'central']).toContain(item.panel);
        }
    });
});