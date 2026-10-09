# syntax=docker/dockerfile:1.7
# One image, one process: the BFF serves the React build from wwwroot, so the browser
# only ever talks to one origin (no CORS) and there is one thing to deploy.

ARG NODE_VERSION=22
ARG DOTNET_VERSION=10.0

# ---- 1. Build the React client ----
FROM node:${NODE_VERSION}-alpine AS web-build
WORKDIR /src/web
# Dependencies first, so this layer is cached until the lockfile changes.
COPY web/package.json web/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY web/ ./
RUN npm run build

# ---- 2. Build and publish the BFF ----
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-noble AS api-build
WORKDIR /src/api
COPY api/global.json api/Directory.Build.props api/Directory.Packages.props api/BupaBooks.slnx ./
COPY api/src/BupaBooks.Application/BupaBooks.Application.csproj src/BupaBooks.Application/
COPY api/src/BupaBooks.Infrastructure/BupaBooks.Infrastructure.csproj src/BupaBooks.Infrastructure/
COPY api/src/BupaBooks.Host/BupaBooks.Host.csproj src/BupaBooks.Host/
RUN dotnet restore src/BupaBooks.Host/BupaBooks.Host.csproj
COPY api/src/ src/
# The OpenAPI document is generated and drift-checked in CI; no need to regenerate it here.
RUN dotnet publish src/BupaBooks.Host/BupaBooks.Host.csproj \
      --configuration Release --no-restore --output /app \
      -p:OpenApiGenerateDocuments=false

# ---- 3. Runtime ----
# Chiseled = distroless Ubuntu: no shell, no package manager, non-root by default, so a
# much smaller attack surface. "-extra" adds ICU, which the en-AU culture-aware sort needs.
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled-extra AS runtime
WORKDIR /app
COPY --from=api-build /app ./
COPY --from=web-build /src/web/dist ./wwwroot

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

# No HEALTHCHECK: a distroless image has no curl/shell to run one. The orchestrator probes
# /health/live and /health/ready over HTTP instead.
ENTRYPOINT ["dotnet", "BupaBooks.Host.dll"]
