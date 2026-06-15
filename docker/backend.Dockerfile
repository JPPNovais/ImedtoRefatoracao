# syntax=docker/dockerfile:1.7
# Runtime do backend .NET 10.
#
# O publish self-contained é gerado no runner (SDK via setup-dotnet/CDN — não
# depende das imagens do MCR, que sofrem rate-limit 429 em pulls anônimos).
# A base é Ubuntu (Docker Hub) + libs nativas para globalização pt-BR; o
# runtime .NET vem dentro do publish self-contained.
#
# Context de build = pasta publish-backend (binários self-contained).
FROM ubuntu:24.04
WORKDIR /app

# libicu = globalização pt-BR; demais = TLS, tz e healthcheck.
RUN apt-get update && apt-get install -y --no-install-recommends \
        libicu74 ca-certificates tzdata wget \
    && rm -rf /var/lib/apt/lists/*

COPY . ./

ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_GCHeapHardLimit=300000000

EXPOSE 5000

# Healthcheck via /health (sem dependências externas).
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD wget -q --spider http://localhost:5000/health || exit 1

ENTRYPOINT ["./Imedto.Backend.API"]
