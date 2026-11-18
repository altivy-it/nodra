# syntax=docker/dockerfile:1
FROM docker.io/lzocateli/dotnet-sdk:10.0.400-noble AS build
USER root
WORKDIR /src
ARG RELEASE_VERSION=0.1.0

COPY global.json ./
COPY backend/Directory.Build.props backend/Directory.Packages.props ./backend/
COPY backend/libs/domain/Nodra.Backend.Domain.csproj backend/libs/domain/
COPY backend/libs/application/Nodra.Backend.Application.csproj backend/libs/application/
COPY backend/libs/infrastructure/data/Nodra.Backend.Infra.Data.Postgres.csproj backend/libs/infrastructure/data/
COPY backend/apps/worker/Nodra.Notes.Worker.csproj backend/apps/worker/
RUN dotnet restore backend/apps/worker/Nodra.Notes.Worker.csproj

COPY backend/ backend/
RUN dotnet publish backend/apps/worker/Nodra.Notes.Worker.csproj --configuration Release --no-restore --output /app/publish -p:Version=${RELEASE_VERSION}

FROM docker.io/lzocateli/dotnet-aspnet:10.0.15-noble-v1@sha256:de0e9e46416e3aec0ec28e2e51d401fe4b9d3004133bb93876de0ee16e623c93 AS runtime
ARG RELEASE_VERSION=0.1.0
ARG IMAGE_VERSION=${RELEASE_VERSION}
LABEL org.opencontainers.image.title="nodra-worker" \
    org.opencontainers.image.description="Worker interno do Nodra para provisioning durável de vaults." \
    org.opencontainers.image.version="${IMAGE_VERSION}" \
    org.opencontainers.image.source="https://github.com/altivy-it/nodra" \
    org.opencontainers.image.documentation="https://github.com/altivy-it/nodra/tree/main/deploy/production" \
    org.opencontainers.image.url="https://github.com/altivy-it/nodra"
WORKDIR /app
COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Nodra.Notes.Worker.dll"]
