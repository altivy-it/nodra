# syntax=docker/dockerfile:1
FROM docker.io/lzocateli/dotnet-sdk:10.0.400-noble AS build
USER root
WORKDIR /src

COPY global.json dotnet-tools.json ./
COPY backend/Directory.Build.props backend/Directory.Packages.props ./backend/
COPY backend/libs/domain/Nodra.Backend.Domain.csproj backend/libs/domain/
COPY backend/libs/application/Nodra.Backend.Application.csproj backend/libs/application/
COPY backend/libs/infrastructure/data/Nodra.Backend.Infra.Data.Postgres.csproj backend/libs/infrastructure/data/
RUN dotnet restore backend/libs/infrastructure/data/Nodra.Backend.Infra.Data.Postgres.csproj \
  && dotnet tool restore

COPY backend/ backend/
RUN ALTIVY_DESIGNTIME_CONNECTION_STRING='Host=127.0.0.1;Database=altivy;Username=altivy_migrator' \
    dotnet tool run dotnet-ef migrations bundle \
      --project backend/libs/infrastructure/data/Nodra.Backend.Infra.Data.Postgres.csproj \
      --startup-project backend/libs/infrastructure/data/Nodra.Backend.Infra.Data.Postgres.csproj \
      --configuration Release \
      --self-contained \
      --target-runtime linux-x64 \
      --output /out/efbundle

FROM docker.io/lzocateli/dotnet-aspnet:10.0.11-noble AS runtime
ARG RELEASE_VERSION=0.1.0
ARG IMAGE_VERSION=${RELEASE_VERSION}
LABEL org.opencontainers.image.title="nodra-migrations" \
  org.opencontainers.image.description="Bundle de migrations PostgreSQL do Nodra." \
  org.opencontainers.image.version="${IMAGE_VERSION}" \
  org.opencontainers.image.source="https://github.com/altivy-it/nodra" \
  org.opencontainers.image.documentation="https://github.com/altivy-it/nodra/tree/main/deploy/production" \
  org.opencontainers.image.url="https://github.com/altivy-it/nodra"
WORKDIR /app
COPY --from=build --chown=$APP_UID:$APP_UID /out/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["/app/efbundle"]