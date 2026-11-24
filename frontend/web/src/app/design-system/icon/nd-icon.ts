import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ICONS, IconShape, isIconName } from './icon-paths';

/**
 * Ícone SVG do Nodra. Sem `label` é decorativo (`aria-hidden`); com `label` vira `role="img"`.
 * Nomes desconhecidos caem para `circle` para nunca quebrar o layout.
 */
@Component({
  selector: 'nd-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'nd-icon',
    style: 'display: inline-flex; flex: none; line-height: 0',
    '[attr.role]': "label() ? 'img' : null",
    '[attr.aria-label]': 'label()',
    '[attr.aria-hidden]': "label() ? null : 'true'",
    '[attr.data-icon]': 'name()',
  },
  template: `
    <svg [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      [attr.stroke-width]="stroke()" stroke-linecap="round" stroke-linejoin="round" focusable="false">
      @for (shape of shapes(); track $index) {
        @if (asPath(shape); as p) { <path [attr.d]="p" /> }
        @if (asCircle(shape); as c) { <circle [attr.cx]="c[0]" [attr.cy]="c[1]" [attr.r]="c[2]" /> }
        @if (asRect(shape); as r) { <rect [attr.x]="r[0]" [attr.y]="r[1]" [attr.width]="r[2]" [attr.height]="r[3]" [attr.rx]="r[4]" /> }
      }
    </svg>
  `,
})
export class NdIcon {
  readonly name = input.required<string>();
  readonly size = input(16);
  readonly stroke = input(1.75);
  readonly label = input<string | null>(null);

  protected readonly shapes = computed<readonly IconShape[]>(() => {
    const name = this.name();
    return isIconName(name) ? ICONS[name] : ICONS.circle;
  });

  protected asPath(shape: IconShape): string | null { return 'path' in shape ? shape.path : null; }
  protected asCircle(shape: IconShape): readonly number[] | null { return 'circle' in shape ? shape.circle : null; }
  protected asRect(shape: IconShape): readonly number[] | null { return 'rect' in shape ? shape.rect : null; }
}
