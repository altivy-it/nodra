import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import DOMPurify from 'dompurify';
import { KATEX_OPTIONS, MARKED_OPTIONS, MERMAID_OPTIONS, provideMarkdown, SANITIZE } from 'ngx-markdown';

import { routes } from './app.routes';
import { appBasePathInterceptor } from './core/app-base-path';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([appBasePathInterceptor])),
    provideRouter(routes),
    provideMarkdown({
      markedOptions: {
        provide: MARKED_OPTIONS,
        useValue: { gfm: true, breaks: true }
      },
      katexOptions: {
        provide: KATEX_OPTIONS,
        useValue: {
          throwOnError: false,
          strict: 'error',
          trust: false,
          maxExpand: 100
        }
      },
      mermaidOptions: {
        provide: MERMAID_OPTIONS,
        useValue: {
          startOnLoad: false,
          securityLevel: 'strict',
          theme: 'neutral'
        }
      },
      sanitize: {
        provide: SANITIZE,
        useValue: (html: string) => DOMPurify.sanitize(html, {
          USE_PROFILES: { html: true, mathMl: true },
          ADD_TAGS: ['input'],
          ADD_ATTR: ['type', 'checked', 'disabled'],
          ALLOWED_URI_REGEXP: /^https?:/i
        })
      }
    })
  ]
};
