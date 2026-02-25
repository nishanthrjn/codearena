# Multi-stage build for CodeArena .NET 8 API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore only the main project (not the test sub-project)
COPY CodeArena.Api.csproj .
RUN dotnet restore CodeArena.Api.csproj

# Copy source, exclude Tests directory via .dockerignore
COPY . .
# Publish only the main project to avoid picking up the test sub-project
RUN dotnet publish CodeArena.Api.csproj -c Release -o /app/publish

# ── Runtime image ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Data-protection keys volume mount point
RUN mkdir -p /app/dp-keys && chown appuser:appgroup /app/dp-keys

COPY --from=build /app/publish .
USER appuser

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "CodeArena.Api.dll"]
