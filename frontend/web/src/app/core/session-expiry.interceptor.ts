import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { tap } from 'rxjs';
import { SessionService } from './session.service';

/**
 * Uma resposta 401 de qualquer chamada, exceto a própria leitura da sessão, marca
 * a sessão como expirada. O `SessionService` é resolvido só no erro para evitar
 * dependência circular com o `HttpClient` usado no construtor dele.
 */
export const sessionExpiryInterceptor: HttpInterceptorFn = (request, next) => {
  const injector = inject(Injector);
  return next(request).pipe(
    tap({
      error: (error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401 && !request.url.endsWith('/bff/session')) {
          injector.get(SessionService).expire();
        }
      },
    }),
  );
};
