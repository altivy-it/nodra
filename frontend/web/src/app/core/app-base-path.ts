import { DOCUMENT } from '@angular/common';
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

const proxiedPaths = ['/api/v1/', '/bff/', '/hubs/'];

export function appPath(path: string, baseUri: string): string {
    const basePath = new URL(baseUri).pathname.replace(/\/+$/, '');
    const normalizedPath = path.startsWith('/') ? path : `/${path}`;
    return `${basePath}${normalizedPath}`;
}

export const appBasePathInterceptor: HttpInterceptorFn = (request, next) => {
    const basePath = new URL(inject(DOCUMENT).baseURI).pathname.replace(/\/+$/, '');
    if (!basePath || !proxiedPaths.some(path => request.url.startsWith(path))) {
        return next(request);
    }

    return next(request.clone({ url: `${basePath}${request.url}` }));
};