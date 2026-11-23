import { Injectable } from '@angular/core';
import type { IconName } from '../design-system';

export interface ActivityItem {
    id: string;
    label: string;
    icon: IconName;
    panel: string;
    order: number;
    available: boolean;
}

@Injectable({ providedIn: 'root' })
export class ActivityRegistry {
    readonly items: ActivityItem[] = [
        { id: 'files', label: 'Arquivos', icon: 'files', panel: 'primary', order: 10, available: true },
        { id: 'search', label: 'Buscar', icon: 'search', panel: 'primary', order: 20, available: true },
        { id: 'tags', label: 'Tags', icon: 'tag', panel: 'primary', order: 30, available: true },
        { id: 'graph', label: 'Grafo', icon: 'graph', panel: 'context', order: 40, available: true },
        { id: 'help', label: 'Ajuda', icon: 'help', panel: 'central', order: 50, available: true },
        { id: 'settings', label: 'Configurações', icon: 'settings', panel: 'central', order: 60, available: true }
    ];

    availableItems(): ActivityItem[] {
        return this.items.filter(item => item.available).sort((left, right) => left.order - right.order);
    }
}