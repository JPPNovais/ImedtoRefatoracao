# syntax=docker/dockerfile:1.7
# Multi-stage build do frontend Vue + Vite + design-system local.
# Build context = raiz do repo (precisa pra acessar design-system/ e frontend/).

FROM node:20-alpine AS build
WORKDIR /repo

# 1) Build design-system primeiro — frontend importa @imedto/ui de ../design-system/dist
COPY design-system/package*.json design-system/
RUN cd design-system \
    && npm ci --no-audit --no-fund --prefer-offline --legacy-peer-deps

COPY design-system/ design-system/
RUN cd design-system && npm run build

# 2) Build frontend (depende de design-system/dist)
COPY frontend/package*.json frontend/
RUN cd frontend \
    && npm ci --no-audit --no-fund --prefer-offline --legacy-peer-deps

COPY frontend/ frontend/

ARG VITE_API_BASE_URL=""
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN cd frontend && npm run build

# Runtime nginx servindo SPA + cache amigável de estáticos.
FROM nginx:1.27-alpine
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /repo/frontend/dist /usr/share/nginx/html

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s \
    CMD wget -q --spider http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
