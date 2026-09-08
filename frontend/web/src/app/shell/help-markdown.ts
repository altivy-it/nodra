export type HelpBlock =
    | { kind: 'heading'; level: 1 | 2 | 3; text: string }
    | { kind: 'paragraph'; text: string }
    | { kind: 'list'; items: string[] };

export function parseHelpMarkdown(markdown: string): HelpBlock[] {
    const blocks: HelpBlock[] = [];
    let paragraph: string[] = [];
    let list: string[] = [];

    const flush = (): void => {
        if (paragraph.length) {
            blocks.push({ kind: 'paragraph', text: paragraph.join(' ') });
            paragraph = [];
        }
        if (list.length) {
            blocks.push({ kind: 'list', items: [...list] });
            list = [];
        }
    };

    for (const line of markdown.replaceAll('\r\n', '\n').split('\n')) {
        const heading = /^(#{1,3})\s+(.+)$/.exec(line.trim());
        const bullet = /^[-*]\s+(.+)$/.exec(line.trim());
        if (heading) {
            flush();
            blocks.push({ kind: 'heading', level: heading[1].length as 1 | 2 | 3, text: heading[2] });
        } else if (bullet) {
            if (paragraph.length) flush();
            list.push(bullet[1]);
        } else if (line.trim()) {
            if (list.length) flush();
            paragraph.push(line.trim());
        } else {
            flush();
        }
    }
    flush();
    return blocks;
}