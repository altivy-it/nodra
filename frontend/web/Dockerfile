# syntax=docker/dockerfile:1
FROM lzocateli/angular-cli:22.1.0-node24.15.0-bookworm AS build
WORKDIR /src

COPY frontend/web/package.json frontend/web/package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY frontend/web/ ./
ARG WEB_CONFIGURATION=production
ARG WEB_BASE_HREF=/
RUN npm run build -- --configuration ${WEB_CONFIGURATION} --base-href ${WEB_BASE_HREF}

FROM lzocateli/nginx:1.28.0-bookworm AS runtime
ARG IMAGE_VERSION=0.1.0
LABEL org.opencontainers.image.title="altivy-web" \
  org.opencontainers.image.description="Proxy HTTPS e aplicação web estática do Altivy Notes." \
  org.opencontainers.image.version="${IMAGE_VERSION}" \
  org.opencontainers.image.source="https://github.com/lzocateli/AltivyNotes" \
  org.opencontainers.image.documentation="https://github.com/lzocateli/AltivyNotes/tree/main/deploy/production" \
  org.opencontainers.image.url="https://github.com/lzocateli/AltivyNotes"
COPY deploy/nginx/conf.d/web.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/altivy-web/browser/ /usr/share/nginx/html/
RUN apt-get update \
  && apt-get install --yes --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/* \
  && sed -i 's#^[[:space:]]*pid[[:space:]].*#pid /tmp/nginx.pid;#' /etc/nginx/nginx.conf \
  && chown -R nginx:nginx /usr/share/nginx/html /var/cache/nginx /var/log/nginx /var/run
USER nginx
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=6 CMD curl --fail --silent http://127.0.0.1:8080/ || exit 1
