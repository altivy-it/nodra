# syntax=docker/dockerfile:1
FROM docker.io/lzocateli/angular-cli:22.1.0-node24.15.0-bookworm AS build
USER root
WORKDIR /src

COPY frontend/web/package.json frontend/web/package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY frontend/web/ ./
ARG WEB_CONFIGURATION=production
ARG WEB_BASE_HREF=/
RUN npm run build -- --configuration ${WEB_CONFIGURATION} --base-href ${WEB_BASE_HREF}

FROM docker.io/lzocateli/nginx:1.28.0-bookworm AS runtime
USER root
ARG RELEASE_VERSION=0.1.0
ARG IMAGE_VERSION=${RELEASE_VERSION}
LABEL org.opencontainers.image.title="nodra-web" \
  org.opencontainers.image.description="Proxy HTTPS e aplicação web estática do Nodra." \
  org.opencontainers.image.version="${IMAGE_VERSION}" \
  org.opencontainers.image.source="https://github.com/altivy-it/nodra" \
  org.opencontainers.image.documentation="https://github.com/altivy-it/nodra/tree/main/deploy/production" \
  org.opencontainers.image.url="https://github.com/altivy-it/nodra"
COPY deploy/nginx/conf.d/web.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/nodra-web/browser/ /usr/share/nginx/html/
RUN apt-get update \
  && apt-get install --yes --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/* \
  && sed -i 's#^[[:space:]]*pid[[:space:]].*#pid /tmp/nginx.pid;#' /etc/nginx/nginx.conf \
  && chown -R nginx:nginx /usr/share/nginx/html /var/cache/nginx /var/log/nginx /var/run
USER nginx
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=6 CMD curl --fail --silent http://127.0.0.1:8080/ || exit 1
