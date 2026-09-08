export type NoteInline =
    | { kind: 'text'; value: string }
    | { kind: 'strong'; value: string }
    | { kind: 'emphasis'; value: string }
    | { kind: 'code'; value: string }
    | { kind: 'link'; label: string; value: string; href: string };

export type NoteBlock =
    | { kind: 'heading'; level: 1 | 2 | 3; inline: NoteInline[] }
    | { kind: 'paragraph'; inline: NoteInline[] }
    | { kind: 'list'; ordered: boolean; items: NoteInline[][] }
    | { kind: 'quote'; inline: NoteInline[] }
    | { kind: 'code-block'; value: string };

export function parseNoteMarkdown(markdown: string): NoteBlock[] {
    const blocks: NoteBlock[] = [];
    const lines = markdown.replaceAll('\r\n', '\n').split('\n');
    let paragraph: string[] = [];
    let list: { ordered: boolean; value: string }[] = [];
    let quote: string[] = [];
    let code: string[] | null = null;

    const flush = (): void => {
        if (paragraph.length) blocks.push({ kind: 'paragraph', inline: parseInline(paragraph.join(' ')) });
        if (list.length) blocks.push({ kind: 'list', ordered: list[0].ordered, items: list.map(item => plainInline(item.value)) });
        if (quote.length) blocks.push({ kind: 'quote', inline: plainInline(quote.join(' ')) });
        paragraph = [];
        list = [];
        quote = [];
    };

    for (const line of lines) {
        if (code) {
            if (line.trim().startsWith('```')) {
                blocks.push({ kind: 'code-block', value: code.join('\n') });
                code = null;
            } else code.push(line);
            continue;
        }

        if (line.trim().startsWith('```')) {
            flush();
            code = [];
            continue;
        }

        const heading = /^(#{1,3})\s+(.+)$/.exec(line.trim());
        const bullet = /^[-*+]\s+(.+)$/.exec(line.trim());
        const ordered = /^\d+[.)]\s+(.+)$/.exec(line.trim());
        const quoteLine = /^>\s?(.*)$/.exec(line.trim());
        if (heading) {
            flush();
            blocks.push({ kind: 'heading', level: heading[1].length as 1 | 2 | 3, inline: parseInline(heading[2]) });
        } else if (bullet) {
            if (paragraph.length || quote.length || (list.length && list[0].ordered)) flush();
            list.push({ ordered: false, value: bullet[1] });
        } else if (ordered) {
            if (paragraph.length || quote.length || (list.length && !list[0].ordered)) flush();
            list.push({ ordered: true, value: ordered[1] });
        } else if (quoteLine) {
            if (paragraph.length || list.length) flush();
            quote.push(quoteLine[1]);
        } else if (line.trim()) {
            if (list.length || quote.length) flush();
            paragraph.push(line.trim());
        } else flush();
    }
    if (code) blocks.push({ kind: 'code-block', value: code.join('\n') });
    flush();
    return blocks;
}

function parseInline(value: string): NoteInline[] {
    const tokens: NoteInline[] = [];
    const pattern = /(`[^`]+`|\*\*[^*]+\*\*|__[^_]+__|\*[^*]+\*|_[^_]+_|\[([^\]]+)\]\((https?:\/\/[^\s)]+)\))/g;
    let lastIndex = 0;
    for (const match of value.matchAll(pattern)) {
        const index = match.index ?? 0;
        if (index > lastIndex) tokens.push({ kind: 'text', value: value.slice(lastIndex, index) });
        const token = match[0];
        if (token.startsWith('`')) tokens.push({ kind: 'code', value: token.slice(1, -1) });
        else if (token.startsWith('**') || token.startsWith('__')) tokens.push({ kind: 'strong', value: token.slice(2, -2) });
        else if (token.startsWith('*') || token.startsWith('_')) tokens.push({ kind: 'emphasis', value: token.slice(1, -1) });
        else tokens.push({ kind: 'link', label: match[2], value: match[2], href: match[3] });
        lastIndex = index + token.length;
    }
    if (lastIndex < value.length) tokens.push({ kind: 'text', value: value.slice(lastIndex) });
    return tokens;
}

function plainInline(value: string): NoteInline[] {
    return [{ kind: 'text', value: parseInline(value).map(token => token.kind === 'link' ? token.label : token.value).join('') }];
}