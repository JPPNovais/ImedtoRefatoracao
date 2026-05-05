---
titulo: CI/CD com GitHub Actions — deploy em EC2 via Docker Compose
status: ativo
criado_em: 2026-05-04
escopo: Pipeline completo do GitHub Actions: build do frontend Vue + backend .NET, push de imagem pra ghcr.io, deploy via SSH na EC2, execução de migrations EF, smoke tests pós-deploy. Custo: zero (dentro do free tier do GitHub Actions e ghcr.io).
companhia: PLANO_MIGRACAO_AWS_GREENFIELD.md, PLANO_SETUP_AWS_PASSO_A_PASSO.md, PLANO_DOMINIO_SSL_EMAIL.md.
---

# CI/CD com GitHub Actions

## 0. Arquitetura do pipeline

```
┌─────────────────────────────────────────────────────────────────────┐
│ git push origin main                                                │
└────────────────────────┬────────────────────────────────────────────┘
                         │
            ┌────────────┴────────────┐
            │                         │
            ▼                         ▼
┌──────────────────────┐   ┌──────────────────────┐
│  build-frontend      │   │   build-backend      │
│  vue-tsc + vite      │   │   dotnet build/test  │
│  build → docker img  │   │   → docker img       │
│  → push ghcr.io      │   │   → push ghcr.io     │
└──────────┬───────────┘   └──────────┬───────────┘
           │                          │
           └──────────┬───────────────┘
                      ▼
        ┌─────────────────────────────┐
        │   migrate-database           │
        │   ssh tunnel → RDS           │
        │   dotnet ef database update  │
        └─────────────┬───────────────┘
                      ▼
        ┌─────────────────────────────┐
        │   deploy-ec2                 │
        │   ssh imedto-app             │
        │   docker compose pull        │
        │   docker compose up -d       │
        └─────────────┬───────────────┘
                      ▼
        ┌─────────────────────────────┐
        │   smoke-test                 │
        │   curl /api/health           │
        │   curl /                     │
        └─────────────────────────────┘
```

**Princípio**: build no runner do GitHub (limpo, reproduzível), deploy na EC2 só baixando imagem pronta. **Nunca buildar no EC2** — a `t3.micro` tem só 1 GB RAM, build de Vue ou .NET trava.

---

## 1. Pré-requisitos

- F0 do [PLANO_MIGRACAO_AWS_GREENFIELD.md](PLANO_MIGRACAO_AWS_GREENFIELD.md) feita (EC2 + RDS + S3 + SSM).
- EC2 acessível por SSH com a key pair `imedto-deploy`.
- Repo no GitHub.
- Domínio apontado pra EC2 (ver [PLANO_DOMINIO_SSL_EMAIL.md](PLANO_DOMINIO_SSL_EMAIL.md)).

---

## 2. Estrutura de arquivos a criar no repo

```
.github/
└── workflows/
    ├── ci.yml              # build + test em cada PR/push
    ├── deploy-dev.yml      # deploy automático em push pra main
    └── migrate-db.yml      # roda migrations sob demanda (workflow_dispatch)

docker/
├── frontend.Dockerfile
├── backend.Dockerfile
└── nginx.conf              # config do nginx que serve front + proxy /api → backend

deploy/
├── docker-compose.yml      # versionado, mas sem segredos
├── .env.example            # template das vars que o compose espera
└── scripts/
    ├── pull-secrets.sh     # baixa SSM params e gera /home/ec2-user/imedto/.env
    ├── deploy.sh           # docker compose pull + up
    └── healthcheck.sh
```

---

## 3. Dockerfiles

### 3.1 Frontend (`docker/frontend.Dockerfile`)

```dockerfile
# Build stage
FROM node:20-alpine AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
ARG VITE_API_BASE_URL=""
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

# Runtime stage — nginx servindo estático + proxy /api
FROM nginx:1.27-alpine
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### 3.2 Backend (`docker/backend.Dockerfile`)

```dockerfile
# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/src/Imedto.Backend.sln ./
COPY backend/src/ ./
RUN dotnet restore Imedto.Backend.sln
RUN dotnet publish Services/Imedto.Backend.API/Imedto.Backend.API.csproj \
    -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s \
    CMD wget -qO- http://localhost:5000/api/health || exit 1
ENTRYPOINT ["dotnet", "Imedto.Backend.API.dll"]
```

### 3.3 Nginx (`docker/nginx.conf`)

```nginx
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    # SPA fallback
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Proxy reverso pro backend
    location /api/ {
        proxy_pass http://backend:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        # cookies HttpOnly (BFF)
        proxy_pass_request_headers on;
    }

    # Health
    location = /health {
        return 200 "ok\n";
        add_header Content-Type text/plain;
    }

    # Cache estático
    location ~* \.(js|css|png|jpg|svg|woff2?)$ {
        expires 30d;
        add_header Cache-Control "public, immutable";
    }
}
```

---

## 4. docker-compose.yml (na EC2)

`deploy/docker-compose.yml`:

```yaml
services:
  backend:
    image: ghcr.io/<seu-usuario>/imedto-backend:${IMAGE_TAG:-latest}
    container_name: imedto-backend
    restart: unless-stopped
    env_file: .env
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Default: "Host=${DB_HOST};Database=imedto;Username=imedto;Password=${DB_PASSWORD};SSL Mode=Require;Trust Server Certificate=true"
      Auth__Jwt__Issuer: imedto-backend
      Auth__Jwt__Audience: imedto-app
      Auth__Jwt__PrivateKeyPem: ${JWT_PRIVATE_KEY}
      Auth__Jwt__PublicKeyPem: ${JWT_PUBLIC_KEY}
      Auth__BcryptPepper: ${BCRYPT_PEPPER}
      Storage__S3__Region: sa-east-1
      Storage__S3__BucketFotos: ${S3_BUCKET_FOTOS}
      Storage__S3__BucketAnexos: ${S3_BUCKET_ANEXOS}
      Email__Ses__Region: sa-east-1
      Email__Ses__From: ${EMAIL_FROM}
    expose: ["5000"]
    networks: [imedto]
    healthcheck:
      test: ["CMD-SHELL", "wget -qO- http://localhost:5000/api/health || exit 1"]
      interval: 30s
      timeout: 5s
      retries: 3

  frontend:
    image: ghcr.io/<seu-usuario>/imedto-frontend:${IMAGE_TAG:-latest}
    container_name: imedto-frontend
    restart: unless-stopped
    depends_on:
      backend:
        condition: service_healthy
    networks: [imedto]
    expose: ["80"]

  caddy:
    image: caddy:2-alpine
    container_name: imedto-caddy
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile:ro
      - caddy-data:/data
      - caddy-config:/config
    depends_on: [frontend]
    networks: [imedto]

networks:
  imedto:
    driver: bridge

volumes:
  caddy-data:
  caddy-config:
```

> Caddy serve como reverse proxy externo com **HTTPS automático** (Let's Encrypt). Configuração detalhada em [PLANO_DOMINIO_SSL_EMAIL.md](PLANO_DOMINIO_SSL_EMAIL.md).

---

## 5. Script de pull de segredos (`deploy/scripts/pull-secrets.sh`)

A EC2 tem IAM role `imedto-ec2-role` com permissão de ler `/imedto/dev/*` no SSM. Esse script gera `.env` populado.

```bash
#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="/home/ec2-user/imedto/.env"
PARAM_PREFIX="/imedto/dev"

cd /home/ec2-user/imedto

# Função utilitária
get_param() {
  aws ssm get-parameter --name "$1" --with-decryption \
    --query "Parameter.Value" --output text --region sa-east-1
}

cat > "$ENV_FILE" <<EOF
DB_HOST=$(get_param $PARAM_PREFIX/db-host)
DB_PASSWORD=$(get_param $PARAM_PREFIX/db-password)
JWT_PRIVATE_KEY=$(get_param $PARAM_PREFIX/jwt/private-key | base64 -w0)
JWT_PUBLIC_KEY=$(get_param $PARAM_PREFIX/jwt/public-key | base64 -w0)
BCRYPT_PEPPER=$(get_param $PARAM_PREFIX/bcrypt/pepper)
S3_BUCKET_FOTOS=$(get_param $PARAM_PREFIX/s3/bucket-fotos)
S3_BUCKET_ANEXOS=$(get_param $PARAM_PREFIX/s3/bucket-anexos)
EMAIL_FROM=$(get_param $PARAM_PREFIX/email/from)
IMAGE_TAG=${IMAGE_TAG:-latest}
EOF

chmod 600 "$ENV_FILE"
echo "✅ .env gerado em $ENV_FILE"
```

> O backend lê `JWT_PRIVATE_KEY`/`JWT_PUBLIC_KEY` em base64 e decoda — evita problema de quebra de linha em `.env`.

---

## 6. Workflow de CI (`.github/workflows/ci.yml`)

Roda em todo PR e push. Garante que o código compila e testes passam **antes** de qualquer deploy.

```yaml
name: CI

on:
  pull_request:
    branches: [main]
  push:
    branches: [main]

jobs:
  test-frontend:
    runs-on: ubuntu-latest
    defaults: { run: { working-directory: frontend } }
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: npm
          cache-dependency-path: frontend/package-lock.json
      - run: npm ci
      - run: npm run lint
      - run: npm run build
      - run: npm test -- --run

  test-backend:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:17
        env:
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: imedto_test
        ports: ["5432:5432"]
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - working-directory: backend/src
        run: |
          dotnet restore Imedto.Backend.sln
          dotnet build Imedto.Backend.sln --no-restore -c Release
          dotnet test Tests/Imedto.Backend.Test --no-build -c Release \
              --logger "console;verbosity=normal"
```

---

## 7. Workflow de deploy (`.github/workflows/deploy-dev.yml`)

Roda em push pra `main` quando o CI passa.

```yaml
name: Deploy dev

on:
  push:
    branches: [main]
  workflow_dispatch:        # permite rodar manual

concurrency:
  group: deploy-dev
  cancel-in-progress: false

env:
  REGISTRY: ghcr.io
  IMAGE_FRONT: ghcr.io/${{ github.repository_owner }}/imedto-frontend
  IMAGE_BACK: ghcr.io/${{ github.repository_owner }}/imedto-backend

jobs:
  build-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    outputs:
      tag: ${{ steps.meta.outputs.tag }}
    steps:
      - uses: actions/checkout@v4

      - id: meta
        run: echo "tag=$(git rev-parse --short HEAD)" >> $GITHUB_OUTPUT

      - uses: docker/setup-buildx-action@v3

      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build & push frontend
        uses: docker/build-push-action@v6
        with:
          context: .
          file: docker/frontend.Dockerfile
          push: true
          tags: |
            ${{ env.IMAGE_FRONT }}:${{ steps.meta.outputs.tag }}
            ${{ env.IMAGE_FRONT }}:latest
          cache-from: type=gha
          cache-to: type=gha,mode=max
          build-args: |
            VITE_API_BASE_URL=

      - name: Build & push backend
        uses: docker/build-push-action@v6
        with:
          context: .
          file: docker/backend.Dockerfile
          push: true
          tags: |
            ${{ env.IMAGE_BACK }}:${{ steps.meta.outputs.tag }}
            ${{ env.IMAGE_BACK }}:latest
          cache-from: type=gha
          cache-to: type=gha,mode=max

  migrate:
    needs: build-push
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "10.0.x" }

      - uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_DEPLOY_ACCESS_KEY }}
          aws-secret-access-key: ${{ secrets.AWS_DEPLOY_SECRET_KEY }}
          aws-region: sa-east-1

      - name: Generate idempotent SQL script
        working-directory: backend/src
        run: |
          dotnet tool install --global dotnet-ef
          export PATH="$PATH:/home/runner/.dotnet/tools"
          dotnet ef migrations script --idempotent \
              --project Services/Imedto.Backend.Infrastructure \
              --startup-project Services/Imedto.Backend.API \
              --output /tmp/migrate.sql

      - name: Send to EC2 via SSH and apply
        env:
          SSH_KEY: ${{ secrets.EC2_SSH_KEY }}
          EC2_HOST: ${{ secrets.EC2_HOST }}
        run: |
          mkdir -p ~/.ssh
          echo "$SSH_KEY" > ~/.ssh/deploy.pem
          chmod 600 ~/.ssh/deploy.pem
          ssh-keyscan -H "$EC2_HOST" >> ~/.ssh/known_hosts

          scp -i ~/.ssh/deploy.pem /tmp/migrate.sql \
              ec2-user@$EC2_HOST:/tmp/migrate.sql

          ssh -i ~/.ssh/deploy.pem ec2-user@$EC2_HOST << 'EOF'
            set -e
            DB_HOST=$(aws ssm get-parameter --name /imedto/dev/db-host \
                --query Parameter.Value --output text --region sa-east-1)
            DB_PASSWORD=$(aws ssm get-parameter --name /imedto/dev/db-password \
                --with-decryption --query Parameter.Value --output text --region sa-east-1)
            PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -U imedto -d imedto \
                -v ON_ERROR_STOP=1 -f /tmp/migrate.sql
            rm /tmp/migrate.sql
          EOF

  deploy:
    needs: [build-push, migrate]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Deploy via SSH
        env:
          SSH_KEY: ${{ secrets.EC2_SSH_KEY }}
          EC2_HOST: ${{ secrets.EC2_HOST }}
          IMAGE_TAG: ${{ needs.build-push.outputs.tag }}
        run: |
          mkdir -p ~/.ssh
          echo "$SSH_KEY" > ~/.ssh/deploy.pem
          chmod 600 ~/.ssh/deploy.pem
          ssh-keyscan -H "$EC2_HOST" >> ~/.ssh/known_hosts

          # Sincroniza compose + scripts
          rsync -avz -e "ssh -i ~/.ssh/deploy.pem" \
              deploy/ ec2-user@$EC2_HOST:/home/ec2-user/imedto/

          ssh -i ~/.ssh/deploy.pem ec2-user@$EC2_HOST << EOF
            set -e
            cd /home/ec2-user/imedto

            # Login no ghcr.io (token vem do SSM)
            aws ssm get-parameter --name /imedto/dev/ghcr-token \
                --with-decryption --query Parameter.Value --output text --region sa-east-1 \
                | docker login ghcr.io -u ${{ github.actor }} --password-stdin

            # Pull e generate .env atualizado
            export IMAGE_TAG=$IMAGE_TAG
            ./scripts/pull-secrets.sh
            echo "IMAGE_TAG=$IMAGE_TAG" >> .env

            docker compose pull
            docker compose up -d --remove-orphans

            # Limpa imagens antigas
            docker image prune -af --filter "until=72h"
          EOF

  smoke:
    needs: deploy
    runs-on: ubuntu-latest
    steps:
      - name: Healthcheck público
        env:
          APP_URL: ${{ secrets.APP_URL }}      # https://app.imedto.com.br
        run: |
          for i in 1 2 3 4 5; do
            sleep 10
            if curl -fsSL "$APP_URL/api/health"; then
              echo "✅ healthy"
              exit 0
            fi
            echo "Tentativa $i falhou, retry..."
          done
          echo "❌ Smoke test falhou"
          exit 1
```

---

## 8. Workflow de migration on-demand (`.github/workflows/migrate-db.yml`)

Pra rodar migration **sem deploy** (caso queira aplicar uma migration nova rapidamente):

```yaml
name: Migrate DB

on:
  workflow_dispatch:
    inputs:
      ambiente:
        description: "Ambiente"
        required: true
        default: "dev"
        type: choice
        options: ["dev"]

jobs:
  migrate:
    runs-on: ubuntu-latest
    steps:
      # ... (mesma lógica do job migrate do deploy-dev.yml)
```

---

## 9. Secrets do GitHub (configurar uma vez)

Repo → Settings → Secrets and variables → Actions → **"New repository secret"**.

| Nome | Como obter |
|---|---|
| `EC2_SSH_KEY` | Conteúdo do `~/.ssh/imedto-deploy.pem` (a key inteira, com `-----BEGIN EC PRIVATE KEY-----`) |
| `EC2_HOST` | Elastic IP da EC2 (ex: `18.230.xxx.xxx`) ou domínio (`app.imedto.com.br`) |
| `APP_URL` | `https://app.imedto.com.br` |
| `AWS_DEPLOY_ACCESS_KEY` | Access key de um IAM user **dedicado ao deploy** (não use a sua pessoal) — ver §10 |
| `AWS_DEPLOY_SECRET_KEY` | Idem |

### 9.1 Token do ghcr.io na EC2

```bash
# No GitHub: Settings → Developer settings → Personal access tokens → Fine-grained
# Permissões: read:packages
# Salvar o token (começa com github_pat_...) no SSM:
aws ssm put-parameter \
    --name /imedto/dev/ghcr-token \
    --value "github_pat_..." \
    --type SecureString
```

---

## 10. IAM user dedicado ao deploy (least-privilege)

Pra o GitHub Actions não usar suas credenciais administrativas:

```bash
aws iam create-user --user-name imedto-github-deploy

cat > /tmp/policy-github.json <<'EOF'
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "ReadSsmParams",
      "Effect": "Allow",
      "Action": ["ssm:GetParameter", "ssm:GetParameters", "ssm:GetParametersByPath"],
      "Resource": "arn:aws:ssm:sa-east-1:*:parameter/imedto/*"
    }
  ]
}
EOF

aws iam put-user-policy --user-name imedto-github-deploy \
    --policy-name read-ssm \
    --policy-document file:///tmp/policy-github.json

aws iam create-access-key --user-name imedto-github-deploy
# Salvar AccessKeyId e SecretAccessKey nos secrets do GitHub
```

> O GitHub Actions runner **não precisa** de permissão pra deployar EC2 ou pull de imagem — isso acontece **dentro** da EC2, que tem sua própria role. O runner só precisa do SSH + ler SSM (pra recuperar a senha do banco em `migrate`).

---

## 11. Primeiro deploy manual (para validar antes do automatizado)

Antes de confiar no pipeline, faça um deploy manual:

```bash
# Do laptop, build local
docker build -f docker/frontend.Dockerfile -t imedto-frontend:test .
docker build -f docker/backend.Dockerfile -t imedto-backend:test .

# Login no ghcr.io
echo $GHCR_TOKEN | docker login ghcr.io -u <seu-user> --password-stdin

# Tag e push
docker tag imedto-frontend:test ghcr.io/<seu-user>/imedto-frontend:manual
docker tag imedto-backend:test ghcr.io/<seu-user>/imedto-backend:manual
docker push ghcr.io/<seu-user>/imedto-frontend:manual
docker push ghcr.io/<seu-user>/imedto-backend:manual

# Sincroniza compose pra EC2
rsync -avz -e "ssh -i ~/.ssh/imedto-deploy.pem" \
    deploy/ ec2-user@$ELASTIC_IP:/home/ec2-user/imedto/

# SSH e sobe
ssh -i ~/.ssh/imedto-deploy.pem ec2-user@$ELASTIC_IP
[ec2]$ cd ~/imedto
[ec2]$ ./scripts/pull-secrets.sh
[ec2]$ echo "IMAGE_TAG=manual" >> .env
[ec2]$ docker login ghcr.io -u <seu-user> -p $GHCR_TOKEN
[ec2]$ docker compose pull
[ec2]$ docker compose up -d
[ec2]$ docker compose logs -f backend
```

Esperado: backend sobe sem erro, healthcheck OK em 30s. Acessar `http://$ELASTIC_IP/` retorna o frontend.

---

## 12. Migrations — fluxo completo

### 12.1 Em desenvolvimento local (EF gera o C#)

```bash
cd backend/src
dotnet ef migrations add MinhaNovaMigration \
    --project Services/Imedto.Backend.Infrastructure \
    --startup-project Services/Imedto.Backend.API \
    --output-dir Database/Migrations
```

Commit no PR.

### 12.2 No CI (validação)

CI roda `dotnet build` — se a migration tiver erro de sintaxe, falha aqui.

### 12.3 No deploy

Job `migrate` gera SQL idempotente (`dotnet ef migrations script --idempotent`) e aplica via `psql` na EC2 (que conecta no RDS pela rede privada).

> **Vantagem do idempotente:** se o pipeline rodar 2× por engano, a 2ª roda não dá erro — só pula migrations já aplicadas.

### 12.4 Rollback de migration

Migrations EF têm `Down()` mas em prod nunca confie no rollback automático — é destrutivo. Em vez disso:

1. Criar **nova migration** que desfaz a alteração indesejada (ex: drop de coluna que recém adicionou).
2. Comitar + deploy.

Tem o ganho de manter histórico linear e auditável.

---

## 13. Custo do pipeline (free tier GitHub)

| Item | Free tier | Estimativa |
|---|---|---|
| GitHub Actions minutos (privado) | 2.000 min/mês | ~150 min de build + deploy/dia × 30 = 4.500 min ⚠️ |
| Storage cache GHA | 10 GB | <2 GB |
| ghcr.io storage privado | 500 MB | ~200 MB (2 imagens × tags rotativas) |

**Cuidado**: se você fizer mais de ~13 push/dia em repo privado, vai estourar 2.000 min. Mitigações:

- Usar runner ARM (`ubuntu-22.04-arm`) — não há diferença de minuto no GitHub free tier hoje.
- **Tornar o repo público** → 2.000 vira "ilimitado" pra todos os runners.
- Reduzir matrix de testes em PR vs main (rodar testes pesados só no main).
- Não buildar em PR se não for de `dependabot`/`main` — usar `paths-ignore` em `.github/`, `Docs/`, etc.

---

## 14. Troubleshooting

| Sintoma | Causa | Resolução |
|---|---|---|
| `ssh: Permission denied (publickey)` | Secret `EC2_SSH_KEY` mal formatado (quebra de linha) | Recolar a chave inteira, garantir que tem `-----BEGIN/END EC PRIVATE KEY-----` |
| `docker pull: unauthorized` no EC2 | Token ghcr.io expirado ou faltando permissão `read:packages` | Recriar token, atualizar SSM `/imedto/dev/ghcr-token` |
| `psql: connection refused` no job migrate | EC2 ainda não terminou de iniciar Docker Compose | Rodar healthcheck antes do migrate, ou adicionar `sleep 30` |
| `docker compose up` reinicia em loop | Backend crashando — provavelmente JWT key mal formada | `docker compose logs backend`; conferir base64 da key no SSM |
| Imagem não atualiza após push | Tag `latest` está no cache local; usar tag versionada (`$GITHUB_SHA`) | Já implementado — tag = git short sha |
| Migrate aplicou parcialmente e falhou | Migration não-idempotente, ou erro de schema mid-way | `--idempotent` ja resolve a maioria; se sobrar, conectar manualmente e investigar com `psql` |

---

## 15. Próximos passos

- ✅ Criar Dockerfiles, compose, scripts conforme §3-5.
- ✅ Configurar secrets do GitHub conforme §9.
- ✅ Criar IAM user de deploy conforme §10.
- ✅ Primeiro deploy manual conforme §11.
- ✅ Push pra main → pipeline roda fim a fim.
- ➡️ HTTPS e domínio: [PLANO_DOMINIO_SSL_EMAIL.md](PLANO_DOMINIO_SSL_EMAIL.md).
