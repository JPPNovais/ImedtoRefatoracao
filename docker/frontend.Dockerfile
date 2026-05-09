# syntax=docker/dockerfile:1.7
# Multi-stage build do frontend Vue + Vite.

FROM node:20-alpine AS build
WORKDIR /app

# package.json e lock — cache amigável.
COPY frontend/package*.json ./
RUN npm ci --no-audit --no-fund --prefer-offline

# Resto do código.
COPY frontend/ ./

# Em dev local o Vite proxia /api → http://localhost:5050; em prod, o Caddy
# resolve /api dentro do mesmo domínio, então VITE_API_BASE_URL fica vazio.
ARG VITE_API_BASE_URL=""
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

# Runtime nginx servindo SPA + cache amigável de estáticos.
FROM nginx:1.27-alpine
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s \
    CMD wget -q --spider http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
