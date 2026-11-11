# syntax=docker/dockerfile:1
FROM lzocateli/dotnet-sdk:10.0.400-noble AS build
USER root
WORKDIR /src

COPY global.json ./
COPY backend/Directory.Build.props backend/Directory.Packages.props ./backend/
COPY backend/libs/domain/Nodra.Backend.Domain.csproj backend/libs/domain/
COPY backend/libs/application/Nodra.Backend.Application.csproj backend/libs/application/
COPY backend/libs/infrastructure/data/Nodra.Backend.Infra.Data.Postgres.csproj backend/libs/infrastructure/data/
COPY backend/apps/api/Nodra.Notes.Api.csproj backend/apps/api/
RUN dotnet restore backend/apps/api/Nodra.Notes.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/apps/api/Nodra.Notes.Api.csproj --configuration Release --no-restore --output /app/publish

FROM lzocateli/dotnet-aspnet:10.0.11-noble AS runtime
ARG IMAGE_VERSION=0.1.0
LABEL org.opencontainers.image.title="nodra-api" \
    org.opencontainers.image.description="API interna do Nodra." \
    org.opencontainers.image.version="${IMAGE_VERSION}" \
    org.opencontainers.image.source="https://github.com/altivy-it/nodra" \
    org.opencontainers.image.documentation="https://github.com/altivy-it/nodra/tree/main/deploy/production" \
    org.opencontainers.image.url="https://github.com/altivy-it/nodra"
WORKDIR /app
USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish ./
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=6 CMD curl --fail --silent http://127.0.0.1:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Nodra.Notes.Api.dll"]
