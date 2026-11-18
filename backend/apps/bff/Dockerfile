# syntax=docker/dockerfile:1
FROM docker.io/lzocateli/dotnet-sdk:10.0.400-noble AS build
USER root
WORKDIR /src

COPY global.json ./
COPY backend/Directory.Build.props backend/Directory.Packages.props ./backend/
COPY backend/apps/bff/Nodra.Bff.csproj backend/apps/bff/
RUN dotnet restore backend/apps/bff/Nodra.Bff.csproj

COPY backend/apps/bff/ backend/apps/bff/
RUN dotnet publish backend/apps/bff/Nodra.Bff.csproj --configuration Release --no-restore --output /app/publish

FROM docker.io/lzocateli/dotnet-aspnet:10.0.11-noble AS runtime
ARG IMAGE_VERSION=0.1.0
LABEL org.opencontainers.image.title="nodra-bff" \
    org.opencontainers.image.description="BFF interno do Nodra para sessão OIDC e relay autenticado." \
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
HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=6 CMD curl --fail --silent --header "Host: localhost" http://127.0.0.1:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "Nodra.Bff.dll"]
