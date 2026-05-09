#!/usr/bin/env bash
# Aplica /tmp/migrate.sql no RDS (rodando dentro da EC2).
set -euo pipefail

REGION="sa-east-1"

if [ ! -s /tmp/migrate.sql ]; then
  echo "SQL vazio — nada a aplicar"
  exit 0
fi

DB_HOST=$(aws ssm get-parameter --region "$REGION" --name /imedto/dev/db-host \
    --query Parameter.Value --output text)
DB_PASSWORD=$(aws ssm get-parameter --region "$REGION" --name /imedto/dev/db-password \
    --with-decryption --query Parameter.Value --output text)

PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -U imedto -d imedto \
    -v ON_ERROR_STOP=1 -f /tmp/migrate.sql

rm -f /tmp/migrate.sql
echo "✅ Migrations aplicadas"
