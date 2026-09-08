import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { VaultsService } from './vaults.service';

describe('VaultsService', () => {
    beforeEach(() => {
        localStorage.clear();
        TestBed.configureTestingModule({
            providers: [provideHttpClient(), provideHttpClientTesting()]
        });
    });

    it('carrega todas as páginas e prioriza o vault autorizado da URL', async () => {
        const service = TestBed.inject(VaultsService);
        const http = TestBed.inject(HttpTestingController);
        const loading = service.load('user-1');

        http.expectOne(request => request.url === '/api/v1/vaults' && request.params.get('limit') === '100')
            .flush({ items: [{ id: 'vault-1', name: 'Principal' }], nextCursor: 'next-page' });
        await vi.waitFor(() => {
            http.expectOne(request => request.url === '/api/v1/vaults' && request.params.get('cursor') === 'next-page')
                .flush({ items: [{ id: 'vault-2', name: 'Projetos' }], nextCursor: null });
        });
        await loading;

        expect(service.resolve('vault-2')?.id).toBe('vault-2');
        expect(service.vaults()).toHaveLength(2);
        http.verify();
    });

    it('ignora o último vault revogado e usa o primeiro autorizado', async () => {
        localStorage.setItem('altivy:last-vault:v1:user-1', 'vault-revogado');
        const service = TestBed.inject(VaultsService);
        const http = TestBed.inject(HttpTestingController);
        const loading = service.load('user-1');

        http.expectOne(request => request.url === '/api/v1/vaults')
            .flush({ items: [{ id: 'vault-1', name: 'Principal' }], nextCursor: null });
        await loading;

        expect(service.resolve(null)?.id).toBe('vault-1');
        expect(service.select('vault-1')).toBe(true);
        expect(localStorage.getItem('altivy:last-vault:v1:user-1')).toBe('vault-1');
        expect(service.select('vault-revogado')).toBe(false);
        http.verify();
    });
});