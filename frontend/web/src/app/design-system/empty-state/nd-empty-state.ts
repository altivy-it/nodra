import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { NdIcon } from '../icon/nd-icon';
import { Tone, TONES } from '../tone';

export type NdEmptyStateKind = 'empty' | 'error' | 'forbidden' | 'offline' | 'expired' | 'loading';

const PRESETS: Record<NdEmptyStateKind, { tone: Tone; icon: string; title: string }> = {
  loading: { tone: 'accent', icon: 'refresh', title: 'Carregando' },
  empty: { tone: 'neutral', icon: 'search', title: 'Nada por aqui ainda' },
  error: { tone: 'error', icon: 'alert', title: 'Não foi possível carregar' },
  forbidden: { tone: 'error', icon: 'lock', title: 'Acesso negado' },
  offline: { tone: 'warning', icon: 'wifi-off', title: 'Sem conexão' },
  expired: { tone: 'warning', icon: 'clock', title: 'Sessão expirada' },
};

/** Estado vazio, erro, carregando ou expirado em cartão de vidro; ação opcional no slot. */
@Component({
  selector: 'nd-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NdIcon],
  host: { style: 'display: block' },
  template: `
    <div class="nd-state" [class.nd-glass]="!bare()" [class.nd-state--compact]="compact()"
      [attr.role]="role()" [attr.aria-live]="kind() === 'loading' ? 'polite' : null">
      <div class="nd-state__icon" [style.color]="tone().color" [style.background]="tone().background">
        @if (kind() === 'loading') {
          <span class="nd-state__spinner nd-spin" aria-hidden="true"></span>
        } @else {
          <nd-icon [name]="icon() ?? preset().icon" [size]="compact() ? 18 : 22" />
        }
      </div>
      <h2 class="nd-state__title">{{ title() ?? preset().title }}</h2>
      @if (detail()) {
        <p class="nd-state__detail">{{ detail() }}</p>
      }
      <div class="nd-state__action"><ng-content /></div>
    </div>
  `,
  styles: `
    .nd-state { display: flex; flex-direction: column; align-items: center; gap: 10px; padding: 36px 32px; text-align: center; }
    .nd-state--compact { padding: 20px; gap: 6px; }
    .nd-state__icon { display: grid; place-items: center; width: 48px; height: 48px; border-radius: var(--radius-lg); }
    .nd-state--compact .nd-state__icon { width: 38px; height: 38px; border-radius: var(--radius-md); }
    .nd-state__spinner { width: 20px; height: 20px; border: 2px solid currentColor; border-right-color: transparent; border-radius: 50%; }
    .nd-state__title { margin: 6px 0 0; color: var(--text-strong); font: 600 18px/1.3 var(--font-sans); }
    .nd-state--compact .nd-state__title { font-size: 15px; }
    .nd-state__detail { max-width: 42ch; margin: 0; color: var(--text-muted); font-size: 14px; line-height: 1.5; }
    .nd-state__action { display: flex; flex-wrap: wrap; justify-content: center; gap: 8px; margin-top: 8px; }
    .nd-state__action:empty { display: none; }
  `,
})
export class NdEmptyState {
  readonly kind = input<NdEmptyStateKind>('empty');
  readonly title = input<string | null>(null);
  readonly detail = input<string | null>(null);
  readonly icon = input<string | null>(null);
  readonly compact = input(false);
  readonly bare = input(false);

  protected readonly preset = computed(() => PRESETS[this.kind()]);
  protected readonly tone = computed(() => TONES[this.preset().tone]);
  protected readonly role = computed(() => {
    const kind = this.kind();
    if (kind === 'error' || kind === 'forbidden') return 'alert';
    return kind === 'loading' || kind === 'expired' || kind === 'offline' ? 'status' : null;
  });
}
