import { DOCUMENT } from '@angular/common';
import { Component, effect, EventEmitter, HostListener, Inject, Input, Optional, Output, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { MarkdownComponent } from 'ngx-markdown';
import { filter } from 'rxjs';
import { Note, NoteSummary } from '../core/notes.service';
import { VaultRealtimeStatus } from '../core/vault-realtime.service';
import { VaultSummary } from '../core/vaults.service';
import { ActivityRegistry } from './activity-registry';
import { FolderExplorerComponent } from '../features/explorer/folder-explorer.component';
import { HelpPanel } from './help-panel';
import { HelpService } from './help.service';
import { VaultSelector } from './vault-selector';
import { WorkspaceService } from './workspace.service';

@Component({
  selector: 'app-workspace-shell',
  imports: [FolderExplorerComponent, HelpPanel, MarkdownComponent, VaultSelector],
  templateUrl: './workspace-shell-v2.html',
  styleUrl: './workspace-shell.css'
})
export class WorkspaceShell {
  @Input({ required: true }) userName: string | null = null;
  @Input({ required: true }) notes: NoteSummary[] = [];
  @Input() selected: Note | null = null;
  @Input() loading = false;
  @Input() error: string | null = null;
  @Input() vaults: VaultSummary[] = [];
  @Input() currentVaultId: string | null = null;
  @Input() vaultsLoading = false;
  @Input() realtimeStatus: VaultRealtimeStatus = 'disconnected';
  @Output() readonly noteOpened = new EventEmitter<string>();
  @Output() readonly noteCreated = new EventEmitter<string | null>();
  @Output() readonly notesReloaded = new EventEmitter<void>();
  @Output() readonly noteChanged = new EventEmitter<{ path: string; content: string }>();
  @Output() readonly noteRemoved = new EventEmitter<void>();
  @Output() readonly noteMoved = new EventEmitter<string>();
  @Output() readonly folderMoved = new EventEmitter<{ from: string; to: string }>();
  @Output() readonly loggedOut = new EventEmitter<void>();
  @Output() readonly vaultSelected = new EventEmitter<string>();
  readonly previewMode = signal<'editor' | 'split' | 'preview'>('split');
  private resizing: { panel: 'primary' | 'context'; startX: number; startWidth: number } | null = null;
  private previousModule = 'files';

  constructor(
    readonly workspace: WorkspaceService,
    readonly activities: ActivityRegistry,
    readonly help: HelpService,
    @Inject(DOCUMENT) private readonly document: Document,
    @Optional() private readonly router: Router | null) {
    effect(() => this.syncIcons(this.workspace.theme()));
    effect(() => {
      if (this.workspace.activeModule() === 'help') void this.help.load();
    });
    const initialModule = this.router?.url.split('/').pop();
    if (initialModule) this.selectModuleFromRoute(initialModule);
    this.router?.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(event => {
      const module = (event as NavigationEnd).urlAfterRedirects.split('/').pop();
      if (module) this.selectModuleFromRoute(module);
    });
  }

  save(path: string, content: string): void { this.noteChanged.emit({ path, content }); }

  setPreviewMode(mode: 'editor' | 'split' | 'preview'): void { this.previewMode.set(mode); }

  isModule(module: string): boolean { return this.workspace.activeModule() === module; }

  selectModule(module: string): void {
    if (module === 'help' && this.workspace.activeModule() !== 'help') {
      this.previousModule = this.workspace.activeModule();
    }
    this.workspace.setModule(module);
    if (this.currentVaultId) void this.router?.navigate(['/workspace', this.currentVaultId, module]);
  }

  closeHelp(): void {
    this.selectModule(this.previousModule);
    this.focusContent();
  }

  private selectModuleFromRoute(module: string): void {
    if (this.activities.availableItems().some(item => item.id === module)) this.workspace.setModule(module);
  }

  @HostListener('window:keydown', ['$event'])
  handleShortcut(event: KeyboardEvent): void {
    if (event.ctrlKey && event.shiftKey && event.key === '?') {
      event.preventDefault();
      this.selectModule('help');
      this.focusContent();
      return;
    }
    if (event.ctrlKey && !event.altKey && event.key >= '1' && event.key <= '6') {
      const item = this.activities.availableItems()[Number(event.key) - 1];
      if (!item) return;
      event.preventDefault();
      this.selectModule(item.id);
      this.focusContent();
    }
  }

  private focusContent(): void {
    const content = this.document.querySelector<HTMLElement>('.content');
    content?.setAttribute('tabindex', '-1');
    content?.focus();
  }

  startResize(event: PointerEvent, panel: 'primary' | 'context'): void {
    const startWidth = panel === 'primary' ? this.workspace.primaryPanelWidth() : this.workspace.contextPanelWidth();
    this.resizing = { panel, startX: event.clientX, startWidth };
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
  }

  @HostListener('window:pointermove', ['$event'])
  resizeFromPointer(event: PointerEvent): void {
    if (!this.resizing) return;
    const delta = event.clientX - this.resizing.startX;
    const width = this.resizing.startWidth + (this.resizing.panel === 'primary' ? delta : -delta);
    if (this.resizing.panel === 'primary') this.workspace.setPrimaryPanelWidth(width);
    else this.workspace.setContextPanelWidth(width);
  }

  @HostListener('window:pointerup')
  finishResize(): void { this.resizing = null; }

  resizeFromKeyboard(event: KeyboardEvent, panel: 'primary' | 'context'): void {
    const step = event.shiftKey ? 40 : 10;
    const width = panel === 'primary' ? this.workspace.primaryPanelWidth() : this.workspace.contextPanelWidth();
    if (event.key === 'ArrowRight') {
      event.preventDefault();
      this.resize(panel, width + step);
    } else if (event.key === 'ArrowLeft') {
      event.preventDefault();
      this.resize(panel, width - step);
    } else if (event.key === 'Home') {
      event.preventDefault();
      this.resize(panel, panel === 'primary' ? 220 : 240);
    } else if (event.key === 'End') {
      event.preventDefault();
      this.resize(panel, panel === 'primary' ? 420 : 480);
    }
  }

  private resize(panel: 'primary' | 'context', width: number): void {
    if (panel === 'primary') this.workspace.setPrimaryPanelWidth(width);
    else this.workspace.setContextPanelWidth(width);
  }

  private syncIcons(theme: string): void {
    const dark = theme === 'dark' || theme === 'high-contrast';
    const links = this.document.querySelectorAll<HTMLLinkElement>('[id^="favicon-"], [id^="apple-touch-"]');
    links.forEach(link => {
      const isDarkLink = link.id.includes('dark');
      link.media = theme === 'system' ? (isDarkLink ? '(prefers-color-scheme: dark)' : '(prefers-color-scheme: light)')
        : isDarkLink === dark ? '' : 'not all';
    });
  }


  moveActivity(event: KeyboardEvent, currentId: string): void {
    const keys = ['ArrowDown', 'ArrowRight', 'ArrowUp', 'ArrowLeft', 'Home', 'End'];
    if (!keys.includes(event.key)) return;
    event.preventDefault();
    const items = this.activities.availableItems();
    const currentIndex = items.findIndex(item => item.id === currentId);
    const nextIndex = event.key === 'Home' ? 0 : event.key === 'End' ? items.length - 1
      : (currentIndex + (event.key === 'ArrowDown' || event.key === 'ArrowRight' ? 1 : -1) + items.length) % items.length;
    const next = items[nextIndex];
    this.selectModule(next.id);
    this.document.querySelector<HTMLButtonElement>(`[data-activity="${next.id}"]`)?.focus();
  }
}