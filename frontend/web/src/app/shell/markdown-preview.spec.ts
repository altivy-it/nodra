import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MarkdownComponent, MarkdownService } from 'ngx-markdown';
import { describe, expect, it } from 'vitest';
import { appConfig } from '../app.config';

const markdownFixture = `# Documento de Teste Markdown

## Listas
1. Primeiro
2. Segundo

- Item
- [x] Concluído

## Tabela
| Nome | Tecnologia |
| --- | --- |
| API | Angular |

## Código
\`\`\`json
{"enabled": true}
\`\`\`

\`\`\`csharp
var enabled = true;
\`\`\`

\`\`\`sql
SELECT 1;
\`\`\`

\`\`\`bash
echo "Altivy"
\`\`\`

## Citação
> Citação simples

## Formatação
**Negrito**

## Diagrama
\`\`\`mermaid
flowchart TD
    A[Início] --> B[Fim]
\`\`\`

## Matemática
$E = mc^2$

<script>alert('xss')</script>`;

@Component({
    standalone: true,
    imports: [MarkdownComponent],
    template: '<markdown katex [data]="content" (ready)="ready = true" />'
})
class MarkdownPreviewHost {
    content = markdownFixture;
    ready = false;
}

describe('Markdown preview component', () => {
    it('renderiza os principais elementos do documento Markdown completo', async () => {
        await TestBed.configureTestingModule({
            imports: [MarkdownPreviewHost],
            providers: appConfig.providers
        }).compileComponents();
        const fixture = TestBed.createComponent(MarkdownPreviewHost);
        fixture.detectChanges();
        await expect.poll(() => fixture.componentInstance.ready).toBe(true);

        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('h1')?.textContent).toContain('Documento de Teste Markdown');
        expect(element.querySelectorAll('h2').length).toBeGreaterThan(3);
        expect(element.querySelectorAll('ol').length).toBeGreaterThan(0);
        expect(element.querySelectorAll('ul').length).toBeGreaterThan(0);
        expect(element.querySelectorAll('table').length).toBeGreaterThan(0);
        expect(element.querySelectorAll('pre code').length).toBeGreaterThan(3);
        expect(element.querySelector('blockquote')?.textContent).toContain('Citação simples');
        expect(element.querySelector('strong')?.textContent).toContain('Negrito');
        expect(element.textContent).toContain('Concluído');
        expect(element.querySelector('.katex')).toBeTruthy();
        expect(element.querySelector('code.language-json')).toBeTruthy();
        expect(element.querySelector('script')).toBeNull();
    });

    it('transforma blocos Mermaid no elemento esperado pelo runtime', async () => {
        await TestBed.configureTestingModule({ providers: appConfig.providers }).compileComponents();
        const markdown = TestBed.inject(MarkdownService);

        const parsed = await markdown.parse('```mermaid\nflowchart TD\nA --> B\n```', { mermaid: true });

        expect(parsed).toContain('<div class="mermaid">');
        expect(parsed).toContain('flowchart TD');
    });
});