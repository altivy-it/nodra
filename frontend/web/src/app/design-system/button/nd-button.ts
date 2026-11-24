import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { NdIcon } from '../icon/nd-icon';

export type NdButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
export type NdButtonSize = 'sm' | 'md' | 'lg';

/**
 * Botão aplicado sobre `<button>` ou `<a>` nativos, preservando a semântica.
 * Botão só-ícone (`icon`) exige `aria-label` no elemento hospedeiro.
 */
@Component({
  selector: 'button[nd-button], a[nd-button]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NdIcon],
  host: {
    '[class]': 'classes()',
    '[attr.aria-busy]': "loading() ? 'true' : null",
    '[attr.disabled]': "(disabled() || loading()) ? '' : null",
    '[attr.aria-disabled]': "(disabled() || loading()) ? 'true' : null",
  },
  template: `
    @if (loading()) {
      <svg class="nd-spin" [attr.width]="iconSize()" [attr.height]="iconSize()" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" aria-hidden="true"><path d="M21 12a9 9 0 1 1-9-9" /></svg>
    } @else if (icon()) {
      <nd-icon [name]="icon()!" [size]="iconSize()" />
    } @else if (startIcon()) {
      <nd-icon [name]="startIcon()!" [size]="iconSize()" />
    }
    @if (!icon()) {
      <ng-content />
    }
    @if (endIcon() && !icon()) {
      <nd-icon [name]="endIcon()!" [size]="iconSize()" />
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      height: 34px;
      padding: 0 14px;
      border: 1px solid transparent;
      border-radius: var(--radius-sm);
      background: transparent;
      color: var(--text-normal);
      font: 500 13px/1 var(--font-sans);
      text-decoration: none;
      white-space: nowrap;
      cursor: pointer;
      transition: background-color .15s ease, border-color .15s ease, color .15s ease, transform .15s ease;
    }
    :host(:focus-visible) { outline: 2px solid var(--accent); outline-offset: 2px; }
    :host([disabled]) { opacity: .55; cursor: not-allowed; pointer-events: none; }
    :host(.nd-btn--sm) { height: 28px; padding: 0 10px; font-size: 12px; gap: 6px; }
    :host(.nd-btn--lg) { height: 46px; padding: 0 24px; font-size: 15px; }
    :host(.nd-btn--block) { display: flex; width: 100%; }
    :host(.nd-btn--icon) { width: 34px; padding: 0; }
    :host(.nd-btn--icon.nd-btn--sm) { width: 28px; }
    :host(.nd-btn--primary) {
      border-radius: var(--radius-pill);
      background: linear-gradient(180deg, var(--cta-top) 0%, var(--accent) 55%, var(--cta-bottom) 100%);
      box-shadow: var(--shadow-cta);
      color: var(--accent-ink);
      font-weight: 600;
    }
    :host(.nd-btn--primary:hover) { transform: translateY(-1px); }
    :host(.nd-btn--secondary) { border-color: var(--border-strong); background: var(--surface-raised); }
    :host(.nd-btn--secondary:hover) { border-color: var(--accent-border); color: var(--text-strong); }
    :host(.nd-btn--ghost) { color: var(--text-muted); }
    :host(.nd-btn--ghost:hover) { background: var(--hover-wash); color: var(--text-strong); }
    :host(.nd-btn--danger) { border-color: color-mix(in srgb, var(--signal-error) 45%, transparent); color: var(--signal-error); }
    :host(.nd-btn--danger:hover) { background: var(--error-tint); }
    @media (prefers-reduced-motion: reduce) { :host { transition: none; } :host(.nd-btn--primary:hover) { transform: none; } }
  `,
})
export class NdButton {
  readonly variant = input<NdButtonVariant>('secondary');
  readonly size = input<NdButtonSize>('md');
  readonly startIcon = input<string | null>(null);
  readonly endIcon = input<string | null>(null);
  readonly icon = input<string | null>(null);
  readonly loading = input(false);
  readonly disabled = input(false);
  readonly block = input(false);

  protected readonly iconSize = computed(() => (this.size() === 'lg' ? 18 : this.size() === 'sm' ? 14 : 16));

  protected readonly classes = computed(() =>
    [
      'nd-btn',
      `nd-btn--${this.variant()}`,
      this.size() !== 'md' ? `nd-btn--${this.size()}` : '',
      this.icon() ? 'nd-btn--icon' : '',
      this.block() ? 'nd-btn--block' : '',
    ].filter(Boolean).join(' '),
  );
}
