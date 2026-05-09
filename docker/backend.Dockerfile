# syntax=docker/dockerfile:1.7
# Multi-stage build do backend .NET 10.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia projeto para restore + build (cache otimizado).
COPY backend/src/ ./

RUN dotnet restore Imedto.Backend.sln \
    && dotnet publish Services/Imedto.Backend.API/Imedto.Backend.API.csproj \
       -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime mínimo (ASP.NET 10).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Cultura pt-BR (já configurada no Program.cs, mas garante ICU presente)
RUN apt-get update && apt-get install -y --no-install-recommends \
        wget \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_GCHeapHardLimit=300000000

EXPOSE 5000

# Healthcheck via /health (sem dependências externas).
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD wget -q --spider http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "Imedto.Backend.API.dll"]
