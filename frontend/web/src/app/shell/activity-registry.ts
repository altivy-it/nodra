import { Injectable } from '@angular/core';

export interface ActivityItem {
    id: string;
    label: string;
    symbol: string;
    panel: string;
    order: number;
    available: boolean;
}

@Injectable({ providedIn: 'root' })
export class ActivityRegistry {
    readonly items: ActivityItem[] = [
        { id: 'files', label: 'Arquivos', symbol: '▤', panel: 'primary', order: 10, available: true },
        { id: 'search', label: 'Buscar', symbol: '⌕', panel: 'primary', order: 20, available: true },
        { id: 'tags', label: 'Tags', symbol: '#', panel: 'primary', order: 30, available: true },
        { id: 'graph', label: 'Grafo', symbol: '◌', panel: 'context', order: 40, available: true },
        { id: 'help', label: 'Ajuda', symbol: '?', panel: 'central', order: 50, available: true },
        { id: 'settings', label: 'Configurações', symbol: '⚙', panel: 'central', order: 60, available: true }
    ];

    availableItems(): ActivityItem[] {
        return this.items.filter(item => item.available).sort((left, right) => left.order - right.order);
    }
}