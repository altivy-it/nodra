import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class HelpService {
  readonly content = signal<string | null>(null);
  readonly loading = signal(false);
  readonly error = signal(false);
  private loaded = false;

  constructor(private readonly http: HttpClient) { }

  async load(): Promise<void> {
    if (this.loaded || this.loading()) return;
    this.loading.set(true);
    this.error.set(false);
    try {
      this.content.set(await firstValueFrom(this.http.get('/help.md', { responseType: 'text' })));
      this.loaded = true;
    } catch {
      this.error.set(true);
    } finally {
      this.loading.set(false);
    }
  }
}