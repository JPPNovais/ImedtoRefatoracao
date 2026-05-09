#!/usr/bin/env bash
# Deploy via docker compose pull + up. Roda na EC2.
# Recebe IMAGE_TAG e GHCR_OWNER via env vars.
set -euo pipefail

REGION="sa-east-1"
cd /home/ec2-user/imedto

# Login no ghcr.io (token vem do SSM).
GHCR_TOKEN=$(aws ssm get-parameter --region "$REGION" --name /imedto/dev/ghcr-token \
    --with-decryption --query Parameter.Value --output text)
echo "$GHCR_TOKEN" | docker login ghcr.io -u "${GHCR_USER:-JPPNovais}" --password-stdin

# Atualiza .env com segredos.
chmod +x scripts/pull-secrets.sh
./scripts/pull-secrets.sh

# Sobrescreve IMAGE_TAG e GHCR_OWNER (escopo do compose).
{
    echo "IMAGE_TAG=${IMAGE_TAG:-latest}"
    echo "GHCR_OWNER=${GHCR_OWNER:-jppnovais}"
} >> .env

docker compose pull
docker compose up -d --remove-orphans

# Caddy: bind mount aponta pra inode do Caddyfile, e rsync --delete cria
# arquivo novo (inode novo). Restart força remount com inode atualizado.
docker compose restart caddy

# Limpa imagens antigas (t3.micro tem disco pequeno).
docker image prune -af --filter "until=72h" || true

echo "✅ Deploy concluído (tag=${IMAGE_TAG:-latest})"
