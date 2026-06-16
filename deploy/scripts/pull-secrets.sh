#!/usr/bin/env bash
# Lê parâmetros do AWS SSM Parameter Store e gera /home/ec2-user/imedto/.env
# Roda na própria EC2 (que tem IAM role imedto-ec2-role com ssm:GetParameter).
set -euo pipefail

PARAM_PREFIX="/imedto/dev"
ENV_FILE="/home/ec2-user/imedto/.env"
REGION="sa-east-1"

cd /home/ec2-user/imedto

get() {
  aws ssm get-parameter --region "$REGION" --name "$1" --with-decryption \
    --query "Parameter.Value" --output text
}

# Multi-line PEMs precisam ser preservados — o docker-compose lê do .env via env_file.
# Como .env não suporta multi-linha, codificamos as PEMs como uma única linha com \n literais.
# O .NET ImportFromPem decoda o \n corretamente.
priv_pem=$(get "$PARAM_PREFIX/jwt/private-key" | awk '{printf "%s\\n", $0}' | sed 's/\\n$//')
pub_pem=$(get "$PARAM_PREFIX/jwt/public-key" | awk '{printf "%s\\n", $0}' | sed 's/\\n$//')

cat > "$ENV_FILE" <<EOF
DB_HOST=$(get "$PARAM_PREFIX/db-host")
DB_PASSWORD=$(get "$PARAM_PREFIX/db-password")
JWT_PRIVATE_KEY_PEM=$priv_pem
JWT_PUBLIC_KEY_PEM=$pub_pem
BCRYPT_PEPPER=$(get "$PARAM_PREFIX/bcrypt/pepper")
RESEND_API_KEY=$(get "$PARAM_PREFIX/resend/api-key")
IA_ANTHROPIC_API_KEY=$(get "$PARAM_PREFIX/ia/anthropic-api-key" 2>/dev/null || echo "")
S3_BUCKET_FOTOS=$(get "$PARAM_PREFIX/s3/bucket-fotos")
S3_BUCKET_ANEXOS=$(get "$PARAM_PREFIX/s3/bucket-anexos")
EMAIL_PROVIDER=$(aws ssm get-parameter --region "$REGION" --name "$PARAM_PREFIX/email/provider" --query "Parameter.Value" --output text 2>/dev/null || echo "Resend")
GHCR_OWNER=${GHCR_OWNER:-jppnovais}
IMAGE_TAG=${IMAGE_TAG:-latest}
EOF

chmod 600 "$ENV_FILE"
echo "✅ .env gerado em $ENV_FILE ($(wc -l < $ENV_FILE) linhas)"
