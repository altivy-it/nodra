import { beforeEach, describe, expect, it } from 'vitest';
import { PreferencesService } from './preferences.service';

describe('PreferencesService', () => {
    let service: PreferencesService;

    beforeEach(() => {
        localStorage.clear();
        service = new PreferencesService();
    });

    it('retorna defaults quando o storage está vazio', () => {
        expect(service.read('user-1', 'vault-1')).toMatchObject({ theme: 'system', density: 'comfortable', activeModule: 'files' });
    });

    it('normaliza valores corrompidos e limites de dimensão', () => {
        localStorage.setItem('altivy:preferences:v1:user-1:vault-1', JSON.stringify({
            version: 99, activeModule: 'unknown', theme: 'invalid', density: 'invalid', primaryPanelWidth: 1, contextPanelWidth: 9999
        }));

        expect(service.read('user-1', 'vault-1')).toMatchObject({
            version: 1, theme: 'system', density: 'comfortable', primaryPanelWidth: 220, contextPanelWidth: 480
        });
    });

    it('mantém preferências isoladas por usuário e vault', () => {
        const preferences = service.read('user-1', 'vault-1');
        preferences.theme = 'dark';
        service.write('user-1', 'vault-1', preferences);

        expect(service.read('user-1', 'vault-1').theme).toBe('dark');
        expect(service.read('user-2', 'vault-1').theme).toBe('system');
        expect(service.read('user-1', 'vault-2').theme).toBe('system');
    });
});