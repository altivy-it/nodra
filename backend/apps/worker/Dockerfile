# syntax=docker/dockerfile:1
FROM lzocateli/dotnet-sdk:10.0.400-noble AS build
USER root
WORKDIR /src

COPY global.json ./
COPY backend/Directory.Build.props backend/Directory.Packages.props ./backend/
COPY backend/libs/domain/Altivy.Backend.Domain.csproj backend/libs/domain/
COPY backend/libs/application/Altivy.Backend.Application.csproj backend/libs/application/
COPY backend/libs/infrastructure/data/Altivy.Backend.Infra.Data.Postgres.csproj backend/libs/infrastructure/data/
COPY backend/apps/worker/Altivy.Notes.Worker.csproj backend/apps/worker/
RUN dotnet restore backend/apps/worker/Altivy.Notes.Worker.csproj

COPY backend/ backend/
RUN dotnet publish backend/apps/worker/Altivy.Notes.Worker.csproj --configuration Release --no-restore --output /app/publish

FROM lzocateli/dotnet-aspnet:10.0.11-noble AS runtime
ARG IMAGE_VERSION=0.1.0
LABEL org.opencontainers.image.title="altivy-worker" \
    org.opencontainers.image.description="Worker interno do Altivy Notes para provisioning durável de vaults." \
    org.opencontainers.image.version="${IMAGE_VERSION}" \
    org.opencontainers.image.source="https://github.com/lzocateli/AltivyNotes" \
    org.opencontainers.image.documentation="https://github.com/lzocateli/AltivyNotes/tree/main/deploy/production" \
    org.opencontainers.image.url="https://github.com/lzocateli/AltivyNotes"
WORKDIR /app
COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Altivy.Notes.Worker.dll"]
