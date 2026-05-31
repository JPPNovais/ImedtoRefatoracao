#!/usr/bin/env bash
# Aplica /tmp/migrate.sql no Postgres local (container imedto-postgres na EC2).
set -euo pipefail

if [ ! -s /tmp/migrate.sql ]; then
  echo "SQL vazio — nada a aplicar"
  exit 0
fi

# Garante que o container do banco está de pé antes de migrar.
# (restart=unless-stopped o mantém vivo; sobe sob demanda se necessário.)
if ! docker ps --format '{{.Names}}' | grep -q '^imedto-postgres$'; then
  echo "Container imedto-postgres não está rodando — subindo..."
  cd /home/ec2-user/imedto
  docker compose up -d postgres
fi

# Espera o Postgres aceitar conexões.
for i in $(seq 1 30); do
  if docker exec imedto-postgres pg_isready -U imedto -d imedto >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

docker exec -i imedto-postgres psql -U imedto -d imedto \
    -v ON_ERROR_STOP=1 < /tmp/migrate.sql

rm -f /tmp/migrate.sql
echo "✅ Migrations aplicadas (Postgres local)"
