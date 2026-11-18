import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { HelpService } from './help.service';

describe('HelpService', () => {
  it('carrega o asset local somente uma vez', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const service = TestBed.inject(HelpService);
    const http = TestBed.inject(HttpTestingController);
    const first = service.load();
    http.expectOne('/help.md').flush('# Ajuda do Nodra');
    await first;
    await service.load();
    expect(service.content()).toBe('# Ajuda do Nodra');
    http.verify();
  });

  it('permite nova tentativa depois de uma falha de rede', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const service = TestBed.inject(HelpService);
    const http = TestBed.inject(HttpTestingController);

    const first = service.load();
    http.expectOne('/help.md').flush('offline', { status: 503, statusText: 'Offline' });
    await first;
    expect(service.error()).toBe(true);

    const retry = service.load();
    http.expectOne('/help.md').flush('# Recuperada');
    await retry;
    expect(service.content()).toBe('# Recuperada');
    expect(service.error()).toBe(false);
    http.verify();
  });
});