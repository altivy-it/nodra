import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';

export type VaultRealtimeStatus = 'disconnected' | 'connecting' | 'connected' | 'error';

/**
 * Conecta ao hub SignalR de um vault através do proxy autenticado do BFF,
 * sem armazenar token ou cliente Keycloak no browser.
 */
@Injectable({ providedIn: 'root' })
export class VaultRealtimeService {
    readonly status = signal<VaultRealtimeStatus>('disconnected');

    private connection: HubConnection | null = null;
    private generation = 0;

    /**
     * Abre a conexão SignalR com o BFF e entra no grupo do vault informado.
     * A decisão de autorização é avaliada pela API a cada tentativa de entrada no grupo.
     */
    async joinVaultAsync(vaultId: string): Promise<void> {
        const generation = ++this.generation;
        await this.stopConnectionAsync();

        const connection = new HubConnectionBuilder()
            .withUrl('/hubs/vaults', { withCredentials: true })
            .withAutomaticReconnect()
            .build();

        connection.onreconnecting(() => {
            if (generation === this.generation) this.status.set('connecting');
        });
        connection.onreconnected(() => void this.rejoinVaultAsync(connection, vaultId, generation));
        connection.onclose(() => {
            if (generation === this.generation) this.status.set('disconnected');
        });

        this.connection = connection;
        this.status.set('connecting');

        try {
            await connection.start();
            if (generation !== this.generation) {
                await connection.stop();
                return;
            }
            await connection.invoke('JoinVaultAsync', vaultId);
            this.status.set('connected');
        } catch (error) {
            if (generation !== this.generation) return;
            this.status.set('error');
            await this.stopConnectionAsync();
            throw error;
        }
    }

    /** Encerra a conexão ativa, se houver, e limpa o estado local. */
    async disconnectAsync(): Promise<void> {
        this.generation++;
        await this.stopConnectionAsync();
        this.status.set('disconnected');
    }

    private async stopConnectionAsync(): Promise<void> {
        const connection = this.connection;
        this.connection = null;

        if (connection && connection.state !== HubConnectionState.Disconnected) {
            await connection.stop();
        }
    }

    private async rejoinVaultAsync(connection: HubConnection, vaultId: string, generation: number): Promise<void> {
        if (generation !== this.generation) return;
        try {
            await connection.invoke('JoinVaultAsync', vaultId);
            if (generation === this.generation) this.status.set('connected');
        } catch {
            if (generation === this.generation) this.status.set('error');
        }
    }
}
