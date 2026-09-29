import { Component, effect } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { Note, NotesService } from './core/notes.service';
import { SessionService } from './core/session.service';
import { VaultRealtimeService } from './core/vault-realtime.service';
import { VaultsService } from './core/vaults.service';
import { FolderExplorerService, remapPath, VaultEntry } from './data-access/folder-explorer.service';
import { WorkspaceShell } from './shell/workspace-shell';
import { WorkspaceService } from './shell/workspace.service';

@Component({
  selector: 'app-root',
  imports: [WorkspaceShell],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private subject: string | null = null;
  private activeContext: string | null = null;
  private saveTimer: ReturnType<typeof setTimeout> | null = null;
  private pendingSave: Note | null = null;
  private saving = false;

  constructor(
    protected readonly session: SessionService,
    protected readonly notes: NotesService,
    protected readonly vaults: VaultsService,
    protected readonly realtime: VaultRealtimeService,
    protected readonly workspace: WorkspaceService,
    private readonly explorer: FolderExplorerService,
    private readonly router: Router) {
    effect(() => {
      const currentSession = this.session.session();
      if (!currentSession?.authenticated || !currentSession.subject || currentSession.subject === this.subject) return;
      this.subject = currentSession.subject;
      void this.initialize(currentSession.subject);
    });
    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => {
      if (this.subject && !this.vaults.loading()) void this.synchronizeRoute(this.subject);
    });
  }

  open(path: string): void { void this.notes.open(path); }

  create(parentPath: string | null): void { this.notes.newNote(parentPath); }

  save(path: string, content: string): void {
    const note = this.notes.selected();
    if (!note) return;
    const changedNote = { ...note, path, content };
    this.notes.selected.set(changedNote);
    this.pendingSave = changedNote;
    if (this.saveTimer) clearTimeout(this.saveTimer);
    this.saveTimer = setTimeout(() => void this.flushSave(), 700);
  }

  private async flushSave(): Promise<void> {
    if (this.saving || !this.pendingSave) return;
    this.saving = true;
    try {
      while (this.pendingSave) {
        const note = this.pendingSave;
        this.pendingSave = null;
        await this.notes.save(note);
      }
    } finally {
      this.saving = false;
    }
  }

  async remove(): Promise<void> {
    const note = this.notes.selected();
    if (!note) return;
    if (!note.eTag) {
      this.notes.selected.set(null);
      return;
    }
    await this.flushPendingSave();
    const current = this.notes.selected();
    if (!current) return;
    const error = await this.explorer.moveToTrash({
      path: current.path,
      name: current.path.split('/').at(-1)!,
      kind: 'note',
      eTag: current.eTag
    });
    if (error) {
      this.notes.error.set(error);
      return;
    }
    this.notes.selected.set(null);
    await this.notes.load();
  }

  async trashEntry(entry: VaultEntry): Promise<void> {
    const selected = this.notes.selected();
    const removesSelected = !!selected && (selected.path === entry.path ||
      (entry.kind === 'folder' && selected.path.startsWith(`${entry.path}/`)));
    if (removesSelected) await this.flushPendingSave();
    const current = this.notes.selected();
    const target = removesSelected && entry.kind === 'note' && current?.path === entry.path
      ? { ...entry, eTag: current.eTag }
      : entry;
    const error = await this.explorer.moveToTrash(target);
    if (error) {
      this.notes.error.set(error);
      return;
    }
    if (removesSelected) this.notes.selected.set(null);
    await this.notes.load();
  }

  async move(destination: string): Promise<void> {
    await this.flushPendingSave();
    const note = this.notes.selected();
    if (!note) return;
    const error = await this.notes.move(note, destination);
    if (!error) this.explorer.reload();
  }

  async dropNote(path: string, eTag: string, destination: string): Promise<void> {
    await this.flushPendingSave();
    const selected = this.notes.selected();
    const note = selected?.path === path ? selected : { path, eTag, content: '' };
    const source = [...this.explorer.children().values()].flat()
      .find(entry => entry.path === path && entry.kind === 'note');
    if (source) this.explorer.beginOptimisticNoteMove(source, destination);
    const error = await this.notes.move(note, destination);
    if (source) this.explorer.settleOptimisticNoteMove(path, !error);
    if (!error) this.explorer.reload();
  }

  private async flushPendingSave(): Promise<void> {
    if (this.saveTimer) {
      clearTimeout(this.saveTimer);
      this.saveTimer = null;
    }
    await this.flushSave();
  }

  /** Keeps the open note and any queued save pointing at the note's new location after a folder move. */
  folderMoved(from: string, to: string): void {
    const selected = this.notes.selected();
    if (selected) this.notes.selected.set({ ...selected, path: remapPath(selected.path, from, to) });
    if (this.pendingSave) this.pendingSave = { ...this.pendingSave, path: remapPath(this.pendingSave.path, from, to) };
    void this.notes.load();
  }

  selectVault(vaultId: string): void {
    if (!this.vaults.vaults().some(vault => vault.id === vaultId)) return;
    void this.router.navigate(['/workspace', vaultId, this.workspace.activeModule()]);
  }

  reloadVaults(): void {
    if (this.subject) void this.initialize(this.subject);
  }

  private async initialize(subject: string): Promise<void> {
    await this.vaults.load(subject);
    if (subject !== this.subject) return;
    await this.synchronizeRoute(subject);
  }

  private async synchronizeRoute(subject: string): Promise<void> {
    const segments = this.router.url.split('?')[0].split('/').filter(Boolean);
    const requestedVaultId = segments[0] === 'workspace' && segments.length >= 3 ? segments[1] : null;
    const requestedModule = segments[0] === 'workspace' && segments.length >= 3 ? segments[2] : 'files';
    const vault = this.vaults.resolve(requestedVaultId);

    if (!vault) {
      this.activeContext = null;
      this.notes.activateVault(null);
      await this.realtime.disconnectAsync();
      return;
    }

    const context = `${subject}:${vault.id}`;
    this.vaults.select(vault.id);
    this.workspace.setModule(requestedModule);

    const canonicalUrl = `/workspace/${vault.id}/${this.workspace.activeModule()}`;
    if (this.router.url.split('?')[0] !== canonicalUrl) {
      await this.router.navigateByUrl(canonicalUrl, { replaceUrl: true });
      return;
    }

    if (this.activeContext === context) return;
    this.activeContext = context;
    this.workspace.restore(subject, vault.id);
    this.notes.activateVault(vault.id, true);
    await Promise.allSettled([this.notes.load(), this.realtime.joinVaultAsync(vault.id)]);
  }
}
