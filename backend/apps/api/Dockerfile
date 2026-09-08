# syntax=docker/dockerfile:1
FROM lzocateli/dotnet-sdk:10.0.400-noble AS build
USER root
WORKDIR /src

COPY global.json ./
COPY backend/Directory.Build.props backend/Directory.Packages.props ./backend/
COPY backend/libs/domain/Altivy.Backend.Domain.csproj backend/libs/domain/
COPY backend/libs/application/Altivy.Backend.Application.csproj backend/libs/application/
COPY backend/libs/infrastructure/data/Altivy.Backend.Infra.Data.Postgres.csproj backend/libs/infrastructure/data/
COPY backend/apps/api/Altivy.Notes.Api.csproj backend/apps/api/
RUN dotnet restore backend/apps/api/Altivy.Notes.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/apps/api/Altivy.Notes.Api.csproj --configuration Release --no-restore --output /app/publish

FROM lzocateli/dotnet-aspnet:10.0.11-noble AS runtime
LABEL org.opencontainers.image.title="altivy-api" \
    org.opencontainers.image.description="API interna do Altivy Notes." \
    org.opencontainers.image.source="https://github.com/lzocateli/AltivyNotes" \
    org.opencontainers.image.documentation="https://github.com/lzocateli/AltivyNotes/tree/main/deploy/e2e" \
    org.opencontainers.image.url="https://github.com/lzocateli/AltivyNotes"
WORKDIR /app
USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish ./
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=6 CMD curl --fail --silent http://127.0.0.1:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Altivy.Notes.Api.dll"]
