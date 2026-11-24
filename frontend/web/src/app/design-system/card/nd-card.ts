import { ChangeDetectionStrategy, Component, input } from '@angular/core';

let nextId = 0;

/** Cartão de vidro com título opcional; conteúdo no slot padrão. */
@Component({
  selector: 'nd-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: block; min-width: 0' },
  template: `
    <section class="nd-card nd-glass" [attr.aria-labelledby]="title() ? titleId : null">
      @if (title()) {
        <h2 class="nd-card__title" [id]="titleId">{{ title() }}</h2>
      }
      <ng-content />
    </section>
  `,
  styles: `
    .nd-card { padding: 24px; }
    .nd-card__title { margin: 0 0 12px; color: var(--text-strong); font: 600 16px/1.35 var(--font-sans); }
  `,
})
export class NdCard {
  readonly title = input<string | null>(null);
  protected readonly titleId = `nd-card-${++nextId}`;
}
