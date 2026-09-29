import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => {
    const connection = {
        state: 'Disconnected',
        start: vi.fn(async () => {
            connection.state = 'Connected';
        }),
        stop: vi.fn(async () => {
            connection.state = 'Disconnected';
        }),
        invoke: vi.fn(async () => undefined),
        onreconnecting: vi.fn(),
        onreconnected: vi.fn(),
        onclose: vi.fn()
    };

    const builder = {
        withUrl: vi.fn(() => builder),
        withAutomaticReconnect: vi.fn(() => builder),
        build: vi.fn(() => connection)
    };

    return { builder, connection };
});

vi.mock('@microsoft/signalr', () => ({
    HubConnectionBuilder: class {
        withUrl = mocks.builder.withUrl;
        withAutomaticReconnect = mocks.builder.withAutomaticReconnect;
        build = mocks.builder.build;
    },
    HubConnectionState: { Disconnected: 'Disconnected' }
}));

import { VaultRealtimeService } from './vault-realtime.service';
import { TestBed } from '@angular/core/testing';

describe('VaultRealtimeService', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        mocks.connection.state = 'Disconnected';
        TestBed.configureTestingModule({ providers: [VaultRealtimeService] });
    });

    it('conecta pelo BFF com cookies e entra no grupo do vault', async () => {
        const service = TestBed.inject(VaultRealtimeService);

        await service.joinVaultAsync('lab-0001');

        expect(mocks.builder.withUrl).toHaveBeenCalledWith('/hubs/vaults', { withCredentials: true });
        expect(mocks.builder.withAutomaticReconnect).toHaveBeenCalledOnce();
        expect(mocks.connection.start).toHaveBeenCalledOnce();
        expect(mocks.connection.invoke).toHaveBeenCalledWith('JoinVaultAsync', 'lab-0001');
        expect(service.status()).toBe('connected');
    });

    it('desconecta e informa erro quando a entrada no vault falha', async () => {
        mocks.connection.invoke.mockRejectedValueOnce(new Error('Acesso negado.'));
        const service = TestBed.inject(VaultRealtimeService);

        await expect(service.joinVaultAsync('lab-0001')).rejects.toThrow('Acesso negado.');

        expect(mocks.connection.stop).toHaveBeenCalledOnce();
        expect(service.status()).toBe('error');
    });

    it('reentra no grupo após uma reconexão', async () => {
        const service = TestBed.inject(VaultRealtimeService);
        await service.joinVaultAsync('lab-0001');
        const onReconnected = mocks.connection.onreconnected.mock.calls[0]?.[0] as (() => void) | undefined;

        onReconnected?.();

        await vi.waitFor(() => expect(mocks.connection.invoke).toHaveBeenCalledTimes(2));
        expect(mocks.connection.invoke).toHaveBeenLastCalledWith('JoinVaultAsync', 'lab-0001');
        expect(service.status()).toBe('connected');
    });
});
