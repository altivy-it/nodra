import { describe, expect, it } from 'vitest';
import { sanitizeMarkdownPreview } from './markdown-preview-policy';

function render(html: string): HTMLElement {
  const host = document.createElement('div');
  host.innerHTML = sanitizeMarkdownPreview(html);
  return host;
}

describe('política do preview Markdown', () => {
  it('preserva checkboxes de lista de tarefas', () => {
    const host = render('<ul><li><input checked="" disabled="" type="checkbox"> feito</li></ul>');
    const box = host.querySelector('input');
    expect(box?.getAttribute('type')).toBe('checkbox');
    expect(box?.hasAttribute('checked')).toBe(true);
  });

  it('preserva links relativos, âncoras, mailto e imagens data:', () => {
    const host = render(
      '<a href="outra-nota.md">n</a><a href="#secao">s</a><a href="mailto:a@b.c">m</a>' +
      '<img src="anexos/figura.png"><img src="data:image/png;base64,iVBORw0KGgo=">'
    );
    expect([...host.querySelectorAll('a')].map(a => a.getAttribute('href')))
      .toEqual(['outra-nota.md', '#secao', 'mailto:a@b.c']);
    expect([...host.querySelectorAll('img')].map(i => i.getAttribute('src')))
      .toEqual(['anexos/figura.png', 'data:image/png;base64,iVBORw0KGgo=']);
  });

  it('preserva HTML estrutural, estilos, ids, data-* e SVG', () => {
    const host = render(
      '<details open id="d1" data-kind="callout"><summary>Mais</summary><kbd>Ctrl</kbd>' +
      '<mark style="color: red">x</mark></details><svg viewBox="0 0 10 10"><circle cx="5" cy="5" r="4"/></svg>'
    );
    const details = host.querySelector('details');
    expect(details?.id).toBe('d1');
    expect(details?.dataset['kind']).toBe('callout');
    expect(host.querySelector('mark')?.getAttribute('style')).toContain('color');
    expect(host.querySelector('kbd')).toBeTruthy();
    expect(host.querySelector('svg circle')).toBeTruthy();
  });

  it('remove scripts, handlers e URLs javascript:', () => {
    const host = render(
      '<script>alert(1)</script><img src="x.png" onerror="alert(1)">' +
      '<a href="javascript:alert(1)">x</a><svg><script>alert(1)</script></svg>'
    );
    expect(host.querySelector('script')).toBeNull();
    expect(host.querySelector('img')?.hasAttribute('onerror')).toBe(false);
    expect(host.querySelector('a')?.hasAttribute('href')).toBe(false);
  });

  it('isola iframes externos e bloqueia iframes da própria origem', () => {
    const host = render(
      '<iframe src="https://www.youtube.com/embed/abc"></iframe>' +
      `<iframe src="${window.location.origin}/notes/bff/session"></iframe>` +
      '<iframe srcdoc="<script>alert(1)</script>"></iframe>'
    );
    const [external, sameOrigin, inline] = host.querySelectorAll('iframe');
    expect(external.getAttribute('src')).toBe('https://www.youtube.com/embed/abc');
    expect(external.getAttribute('sandbox')).not.toContain('allow-top-navigation');
    expect(sameOrigin.hasAttribute('src')).toBe(false);
    expect(inline.hasAttribute('srcdoc')).toBe(false);
  });
});
