# syntax=docker/dockerfile:1
FROM lzocateli/dotnet-sdk:10.0.400-noble AS build
USER root
WORKDIR /src

COPY global.json ./
COPY backend/Directory.Build.props backend/Directory.Packages.props ./backend/
COPY backend/apps/bff/Altivy.Bff.csproj backend/apps/bff/
RUN dotnet restore backend/apps/bff/Altivy.Bff.csproj

COPY backend/apps/bff/ backend/apps/bff/
RUN dotnet publish backend/apps/bff/Altivy.Bff.csproj --configuration Release --no-restore --output /app/publish

FROM lzocateli/dotnet-aspnet:10.0.11-noble AS runtime
LABEL org.opencontainers.image.title="altivy-bff" \
    org.opencontainers.image.description="BFF interno do Altivy Notes para sessão OIDC e relay autenticado." \
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
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=6 CMD curl --fail --silent --header "Host: localhost" http://127.0.0.1:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "Altivy.Bff.dll"]
