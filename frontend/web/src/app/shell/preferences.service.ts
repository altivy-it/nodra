import { Injectable } from '@angular/core';

export type Theme = 'system' | 'light' | 'dark' | 'high-contrast';
export type Density = 'compact' | 'comfortable';

export interface WorkspacePreferences {
  version: 1;
  activeModule: string;
  theme: Theme;
  density: Density;
  primaryPanelWidth: number;
  contextPanelWidth: number;
  primaryPanelOpen: boolean;
  contextPanelOpen: boolean;
}

const defaults: WorkspacePreferences = {
  version: 1,
  activeModule: 'files',
  theme: 'system',
  density: 'comfortable',
  primaryPanelWidth: 280,
  contextPanelWidth: 320,
  primaryPanelOpen: true,
  contextPanelOpen: true
};

@Injectable({ providedIn: 'root' })
export class PreferencesService {
  read(subject: string, vaultId: string): WorkspacePreferences {
    try {
      const raw = localStorage.getItem(this.key(subject, vaultId));
      if (!raw) return { ...defaults };
      return this.normalize(JSON.parse(raw) as Partial<WorkspacePreferences>);
    } catch {
      return { ...defaults };
    }
  }

  write(subject: string, vaultId: string, preferences: WorkspacePreferences): void {
    try { localStorage.setItem(this.key(subject, vaultId), JSON.stringify(preferences)); } catch { }
  }

  clear(subject: string, vaultId: string): void {
    try { localStorage.removeItem(this.key(subject, vaultId)); } catch { }
  }

  private key(subject: string, vaultId: string): string {
    return `altivy:preferences:v1:${subject}:${vaultId}`;
  }

  private normalize(value: Partial<WorkspacePreferences>): WorkspacePreferences {
    const theme = ['system', 'light', 'dark', 'high-contrast'].includes(value.theme ?? '')
      ? value.theme as Theme : defaults.theme;
    const density = ['compact', 'comfortable'].includes(value.density ?? '')
      ? value.density as Density : defaults.density;
    return {
      ...defaults,
      activeModule: typeof value.activeModule === 'string' && ['files', 'search', 'tags', 'graph', 'help', 'settings'].includes(value.activeModule)
        ? value.activeModule : defaults.activeModule,
      theme,
      density,
      primaryPanelWidth: this.width(value.primaryPanelWidth, 220, 420, defaults.primaryPanelWidth),
      contextPanelWidth: this.width(value.contextPanelWidth, 240, 480, defaults.contextPanelWidth),
      primaryPanelOpen: value.primaryPanelOpen !== false,
      contextPanelOpen: value.contextPanelOpen !== false
    };
  }

  private width(value: number | undefined, min: number, max: number, fallback: number): number {
    return typeof value === 'number' && Number.isFinite(value)
      ? Math.min(max, Math.max(min, Math.round(value))) : fallback;
  }
}