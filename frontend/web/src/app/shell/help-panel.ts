import { Component, computed, EventEmitter, Output, signal } from '@angular/core';
import { HelpBlock, parseHelpMarkdown } from './help-markdown';
import { HelpService } from './help.service';

@Component({
  selector: 'app-help-panel',
  template: `
    <article class="module-page help-page help-runtime" aria-labelledby="help-title">
      @if (help.loading()) {
        <p class="muted">Carregando ajuda...</p>
      } @else if (help.error()) {
        <p class="error" role="alert">Não foi possível carregar a ajuda local.</p>
        <button type="button" class="reset-button" (click)="help.load()">Tentar novamente</button>
      } @else {
        <div class="help-heading"><span class="empty-code">05 / AJUDA</span><button type="button" class="close-help" (click)="closed.emit()">Fechar ajuda</button></div>
        <label class="help-search">Buscar na ajuda <input [value]="query()" (input)="setQuery($event)" type="search" placeholder="Digite um termo"></label>
        @if (filteredBlocks().length === 0) { <p class="muted">Nenhum resultado nesta ajuda.</p> }
        @for (block of filteredBlocks(); track $index) {
          @switch (block.kind) {
            @case ('heading') {
              @if (block.level === 1) { <h1 id="help-title">{{ block.text }}</h1> }
              @else if (block.level === 2) { <h2>{{ block.text }}</h2> }
              @else { <h3>{{ block.text }}</h3> }
            }
            @case ('paragraph') { <p>{{ block.text }}</p> }
            @case ('list') { <ul>@for (item of block.items; track $index) { <li>{{ item }}</li> }</ul> }
          }
        }
      }
    </article>
  `
})
export class HelpPanel {
  @Output() readonly closed = new EventEmitter<void>();
  readonly query = signal('');
  readonly blocks = computed<HelpBlock[]>(() => parseHelpMarkdown(this.help.content() ?? ''));
  readonly filteredBlocks = computed<HelpBlock[]>(() => {
    const query = this.query().trim().toLocaleLowerCase();
    if (!query) return this.blocks();
    return this.blocks().filter(block => {
      const text = block.kind === 'list' ? block.items.join(' ') : block.text;
      return text.toLocaleLowerCase().includes(query);
    });
  });

  constructor(readonly help: HelpService) { }

  setQuery(event: Event): void { this.query.set((event.target as HTMLInputElement).value); }
}
