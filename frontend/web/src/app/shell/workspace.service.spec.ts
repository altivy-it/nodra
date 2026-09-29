import { describe, expect, it, vi } from 'vitest';
import { PreferencesService, WorkspacePreferences } from './preferences.service';
import { WorkspaceService } from './workspace.service';

describe('WorkspaceService', () => {
    it('restaura o layout do usuário e persiste alterações', () => {
        const stored: WorkspacePreferences = {
            version: 1,
            activeModule: 'settings',
            theme: 'dark',
            density: 'compact',
            primaryPanelWidth: 360,
            contextPanelWidth: 400,
            primaryPanelOpen: false,
            contextPanelOpen: true,
            expandedFolders: ['projetos']
        };
        const preferences = {
            read: vi.fn().mockReturnValue(stored),
            write: vi.fn()
        } as unknown as PreferencesService;
        const service = new WorkspaceService(preferences);

        service.restore('user-1', 'vault-1');

        expect(service.theme()).toBe('dark');
        expect(service.activeModule()).toBe('settings');
        expect(service.density()).toBe('compact');
        expect(service.primaryPanelWidth()).toBe(360);
        expect(service.primaryPanelOpen()).toBe(false);
        expect(service.expandedFolders()).toEqual(['projetos']);

        service.cycleTheme();
        service.toggleContextPanel();
        service.resizePrimaryPanel(1000);
        service.resizeContextPanel(-1000);
        service.setPrimaryPanelWidth(300);
        service.setContextPanelWidth(350);
        service.setExpandedFolders(['projetos', 'rascunhos']);

        expect(service.theme()).toBe('high-contrast');
        expect(service.contextPanelOpen()).toBe(false);
        expect(service.primaryPanelWidth()).toBe(300);
        expect(service.contextPanelWidth()).toBe(350);
        expect(preferences.write).toHaveBeenCalledTimes(7);
        expect(preferences.write).toHaveBeenLastCalledWith('user-1', 'vault-1', expect.objectContaining({
            theme: 'high-contrast',
            contextPanelOpen: false,
            expandedFolders: ['projetos', 'rascunhos']
        }));
    });
});