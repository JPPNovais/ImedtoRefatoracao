# syntax=docker/dockerfile:1.7

# ─── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copia código + projetos. Restore + publish em um único RUN garante que
# os pacotes restaurados pelo BuildKit cache mount estejam visíveis no publish.
# (Splittar em dois RUNs pode falhar com NETSDK1064 em alguns runners se a
# cache mount não persistir corretamente entre layers.)
COPY src/Imedto.Backend.sln ./
COPY src/Core/        Core/
COPY src/Services/    Services/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish Services/Imedto.Backend.API/Imedto.Backend.API.csproj \
        -c Release \
        -o /app/publish \
        /p:UseAppHost=false

# ─── Runtime stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Alpine roda em globalization-invariant por padrão — sem ICU, culturas como
# pt-BR levantam CultureNotFoundException. Instalamos icu-libs + tzdata e
# desligamos o invariant mode.
RUN apk add --no-cache icu-libs icu-data-full tzdata

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
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=America/Sao_Paulo \
    DOTNET_GCHeapHardLimit=419430400

COPY --from=build --chown=app:app /app/publish ./

EXPOSE 8080

# Render seta $PORT (default no plano free é 10000). Substituímos no boot
# via shell para manter compat com o env padrão da imagem.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet Imedto.Backend.API.dll"]
