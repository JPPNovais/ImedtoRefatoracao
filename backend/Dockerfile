# syntax=docker/dockerfile:1.7

# ─── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# 1) Cache de NuGet: copia só os .csproj e a solution para fazer restore primeiro.
#    Isso evita refazer o restore a cada mudança de código.
COPY src/Imedto.Backend.sln ./
COPY src/Core/Imedto.Backend.SharedKernel/Imedto.Backend.SharedKernel.csproj    Core/Imedto.Backend.SharedKernel/
COPY src/Services/Imedto.Backend.API/Imedto.Backend.API.csproj                  Services/Imedto.Backend.API/
COPY src/Services/Imedto.Backend.Application/Imedto.Backend.Application.csproj  Services/Imedto.Backend.Application/
COPY src/Services/Imedto.Backend.Contracts/Imedto.Backend.Contracts.csproj      Services/Imedto.Backend.Contracts/
COPY src/Services/Imedto.Backend.Domain/Imedto.Backend.Domain.csproj            Services/Imedto.Backend.Domain/
COPY src/Services/Imedto.Backend.Infrastructure/Imedto.Backend.Infrastructure.csproj Services/Imedto.Backend.Infrastructure/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore Services/Imedto.Backend.API/Imedto.Backend.API.csproj

# 2) Copia o código completo e publica em Release.
COPY src/Core/        Core/
COPY src/Services/    Services/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish Services/Imedto.Backend.API/Imedto.Backend.API.csproj \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

# ─── Runtime stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Usuário não-root (segurança). A imagem base alpine já vem com o uid 1654 "app".
USER app

# Render injeta a variável PORT em runtime. Bind em 0.0.0.0 obrigatório
# (localhost só aceita conexões internas ao container).
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=true \
    DOTNET_PRINT_TELEMETRY_MESSAGE=false \
    DOTNET_GCHeapHardLimit=419430400

COPY --from=build --chown=app:app /app/publish ./

EXPOSE 8080

# Render seta $PORT (default no plano free é 10000). Substituímos no boot
# via shell para manter compat com o env padrão da imagem.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet Imedto.Backend.API.dll"]
