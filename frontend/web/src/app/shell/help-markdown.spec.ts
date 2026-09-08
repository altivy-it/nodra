import { describe, expect, it } from 'vitest';
import { parseHelpMarkdown } from './help-markdown';

describe('parseHelpMarkdown', () => {
  it('preserva headings, parágrafos e listas como blocos estruturados', () => {
    expect(parseHelpMarkdown('# Título\n\nTexto\ncontinuação\n\n- primeiro\n- segundo')).toEqual([
      { kind: 'heading', level: 1, text: 'Título' },
      { kind: 'paragraph', text: 'Texto continuação' },
      { kind: 'list', items: ['primeiro', 'segundo'] }
    ]);
  });

  it('não interpreta HTML ou scripts como markup executável', () => {
    const blocks = parseHelpMarkdown('<script>alert(1)</script>\n\nTexto');
    expect(blocks).toEqual([
      { kind: 'paragraph', text: '<script>alert(1)</script>' },
      { kind: 'paragraph', text: 'Texto' }
    ]);
  });
});