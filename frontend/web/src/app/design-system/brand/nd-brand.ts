import { ChangeDetectionStrategy, Component, input } from '@angular/core';

let nextId = 0;

/**
 * Símbolo e nome do Nodra. A geometria é a mesma de `public/brand/nodra-mark.svg`
 * e do tema do Keycloak: um "N" formado por nós ligados por arestas.
 */
@Component({
  selector: 'nd-brand',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'nd-brand' },
  template: `
    <svg class="nd-brand__mark" [attr.width]="size()" [attr.height]="size()" viewBox="0 0 64 64" aria-hidden="true" focusable="false">
      <defs>
        <linearGradient [attr.id]="gradientId" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stop-color="#c4b5fd" />
          <stop offset="1" stop-color="#7c3aed" />
        </linearGradient>
      </defs>
      <rect width="64" height="64" rx="15" [attr.fill]="'url(#' + gradientId + ')'" />
      <path d="M20 45V19L44 45V19" fill="none" stroke="#17112a" stroke-width="4.5" stroke-linecap="round" stroke-linejoin="round" />
      <circle cx="20" cy="45" r="5.5" fill="#17112a" />
      <circle cx="20" cy="19" r="5.5" fill="#17112a" />
      <circle cx="44" cy="45" r="5.5" fill="#17112a" />
      <circle cx="44" cy="19" r="5.5" fill="#f5f3ff" stroke="#17112a" stroke-width="3" />
    </svg>
    @if (showName()) {
      <span class="nd-brand__name">Nodra</span>
    } @else {
      <span class="sr-only">Nodra</span>
    }
  `,
  styles: `
    :host { display: inline-flex; align-items: center; gap: 10px; color: var(--text-strong); }
    .nd-brand__mark { flex: none; border-radius: 24%; }
    .nd-brand__name { font: 600 1.15em/1 var(--font-sans); letter-spacing: -0.01em; }
  `,
})
export class NdBrand {
  readonly size = input(28);
  readonly showName = input(true);
  protected readonly gradientId = `nd-brand-${++nextId}`;
}
