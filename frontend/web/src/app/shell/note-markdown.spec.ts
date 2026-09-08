import { describe, expect, it } from 'vitest';
import { parseNoteMarkdown } from './note-markdown';

describe('parseNoteMarkdown', () => {
    it('converte títulos, texto, lista, citação e código em blocos seguros', () => {
        const blocks = parseNoteMarkdown('# Título\n\nTexto **forte**.\n\n- primeiro\n- segundo\n\n> atenção\n\n```ts\nconst value = 1;\n```');

        expect(blocks.map(block => block.kind)).toEqual(['heading', 'paragraph', 'list', 'quote', 'code-block']);
        expect(blocks[1]).toMatchObject({ kind: 'paragraph' });
        expect(blocks[4]).toMatchObject({ kind: 'code-block', value: 'const value = 1;' });
    });

    it('aceita somente links HTTP e HTTPS', () => {
        const blocks = parseNoteMarkdown('[documentação](https://example.com) [inseguro](javascript:alert(1))');
        const paragraph = blocks[0];

        expect(paragraph.kind).toBe('paragraph');
        if (paragraph.kind === 'paragraph') {
            expect(paragraph.inline).toContainEqual({ kind: 'link', label: 'documentação', value: 'documentação', href: 'https://example.com' });
            expect(paragraph.inline.filter(token => token.kind === 'link')).toHaveLength(1);
            expect(paragraph.inline.some(token => token.kind === 'text' && token.value.includes('javascript:'))).toBe(true);
        }
    });

    it('reconhece listas numeradas como listas', () => {
        const [block] = parseNoteMarkdown('1. primeiro\n2. segundo\n3. terceiro **completo**');

        expect(block).toMatchObject({ kind: 'list', ordered: true });
        if (block.kind === 'list') {
            expect(block.items).toHaveLength(3);
            expect(block.items[2][0]).toEqual({ kind: 'text', value: 'terceiro completo' });
        }
    });

    it('mantém listas consecutivas com estilos diferentes em blocos separados', () => {
        const blocks = parseNoteMarkdown('1. primeiro\n2. segundo\n\n- terceiro\n- quarto');

        expect(blocks).toHaveLength(2);
        expect(blocks[0]).toMatchObject({ kind: 'list', ordered: true });
        expect(blocks[1]).toMatchObject({ kind: 'list', ordered: false });
    });
});