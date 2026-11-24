import { HttpClient } from '@angular/common/http';
import { DOCUMENT } from '@angular/common';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { appPath } from '../core/app-base-path';

@Injectable({ providedIn: 'root' })
export class HelpService {
  readonly content = signal<string | null>(null);
  readonly loading = signal(false);
  readonly error = signal(false);
  private loaded = false;
  private readonly document = inject(DOCUMENT);

  constructor(private readonly http: HttpClient) { }

  async load(): Promise<void> {
    if (this.loaded || this.loading()) return;
    this.loading.set(true);
    this.error.set(false);
    try {
      this.content.set(await firstValueFrom(this.http.get(appPath('/help.md', this.document.baseURI), { responseType: 'text' })));
      this.loaded = true;
    } catch {
      this.error.set(true);
    } finally {
      this.loading.set(false);
    }
  }
}