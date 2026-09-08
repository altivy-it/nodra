import { Injectable, signal } from '@angular/core';
import { Density, PreferencesService, Theme, WorkspacePreferences } from './preferences.service';

@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  readonly theme = signal<Theme>('system');
  readonly density = signal<Density>('comfortable');
  readonly primaryPanelWidth = signal(280);
  readonly contextPanelWidth = signal(320);
  readonly primaryPanelOpen = signal(true);
  readonly contextPanelOpen = signal(true);
  readonly activeModule = signal('files');
  private subject: string | null = null;
  private vaultId: string | null = null;

  constructor(private readonly preferences: PreferencesService) { }

  restore(subject: string, vaultId: string): void {
    this.subject = subject;
    this.vaultId = vaultId;
    this.apply(this.preferences.read(subject, vaultId));
  }

  setTheme(theme: Theme): void { this.theme.set(theme); this.persist(); }
  cycleTheme(): void {
    const themes: Theme[] = ['system', 'light', 'dark', 'high-contrast'];
    const next = themes[(themes.indexOf(this.theme()) + 1) % themes.length];
    this.setTheme(next);
  }
  setDensity(density: Density): void { this.density.set(density); this.persist(); }
  reset(): void {
    if (this.subject && this.vaultId) this.preferences.clear(this.subject, this.vaultId);
    this.activeModule.set('files');
    this.theme.set('system');
    this.density.set('comfortable');
    this.primaryPanelWidth.set(280);
    this.contextPanelWidth.set(320);
    this.primaryPanelOpen.set(true);
    this.contextPanelOpen.set(true);
  }
  togglePrimaryPanel(): void { this.primaryPanelOpen.update(open => !open); this.persist(); }
  toggleContextPanel(): void { this.contextPanelOpen.update(open => !open); this.persist(); }
  resizePrimaryPanel(delta: number): void {
    this.setPrimaryPanelWidth(this.primaryPanelWidth() + delta);
  }
  resizeContextPanel(delta: number): void {
    this.setContextPanelWidth(this.contextPanelWidth() + delta);
  }
  setPrimaryPanelWidth(width: number): void {
    this.primaryPanelWidth.set(this.clamp(width, 220, 420));
    this.persist();
  }
  setContextPanelWidth(width: number): void {
    this.contextPanelWidth.set(this.clamp(width, 240, 480));
    this.persist();
  }
  setModule(module: string): void {
    if (!['files', 'search', 'tags', 'graph', 'help', 'settings'].includes(module)) return;
    this.activeModule.set(module);
    this.persist();
  }

  private apply(value: WorkspacePreferences): void {
    this.activeModule.set(value.activeModule);
    this.theme.set(value.theme);
    this.density.set(value.density);
    this.primaryPanelWidth.set(value.primaryPanelWidth);
    this.contextPanelWidth.set(value.contextPanelWidth);
    this.primaryPanelOpen.set(value.primaryPanelOpen);
    this.contextPanelOpen.set(value.contextPanelOpen);
  }

  private persist(): void {
    if (!this.subject || !this.vaultId) return;
    this.preferences.write(this.subject, this.vaultId, {
      version: 1,
      activeModule: this.activeModule(),
      theme: this.theme(),
      density: this.density(),
      primaryPanelWidth: this.primaryPanelWidth(),
      contextPanelWidth: this.contextPanelWidth(),
      primaryPanelOpen: this.primaryPanelOpen(),
      contextPanelOpen: this.contextPanelOpen()
    });
  }

  private clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
  }
}