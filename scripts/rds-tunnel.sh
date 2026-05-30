#!/usr/bin/env bash
# Abre túnel SSH local → EC2 bastion → RDS (porta 5432).
# Uso:
#   ./scripts/rds-tunnel.sh            # túnel em foreground (Ctrl+C encerra)
#   ./scripts/rds-tunnel.sh -p 15432   # mapeia 15432 local em vez de 5432
#   ./scripts/rds-tunnel.sh status     # mostra se há túnel ativo
#   ./scripts/rds-tunnel.sh stop       # mata túneis ativos pro RDS
set -euo pipefail

KEY="${IMEDTO_SSH_KEY:-$HOME/.ssh/imedto-deploy.pem}"
BASTION_HOST="${IMEDTO_BASTION_HOST:-56.125.254.136}"
BASTION_USER="${IMEDTO_BASTION_USER:-ec2-user}"
RDS_HOST="${IMEDTO_RDS_HOST:-imedto-dev.cx0648wywxg8.sa-east-1.rds.amazonaws.com}"
RDS_PORT=5432
LOCAL_PORT=5432

usage() {
    sed -n '2,7p' "$0" | sed 's/^# \{0,1\}//'
    exit 1
}

cmd="${1:-up}"
shift || true

while [[ $# -gt 0 ]]; do
    case "$1" in
        -p|--port) LOCAL_PORT="$2"; shift 2 ;;
        -h|--help) usage ;;
        *) echo "argumento desconhecido: $1"; usage ;;
    esac
done

tunnel_pids() {
    pgrep -f "ssh.*-L ${LOCAL_PORT}:${RDS_HOST}:${RDS_PORT}" || true
}

case "$cmd" in
    up)
        if [[ ! -f "$KEY" ]]; then
            echo "❌ chave SSH não encontrada em $KEY" >&2
            exit 1
        fi
        if lsof -nP -iTCP:"$LOCAL_PORT" -sTCP:LISTEN >/dev/null 2>&1; then
            echo "❌ porta ${LOCAL_PORT} já está em uso (Postgres local? outro túnel?). Use -p outra-porta." >&2
            exit 1
        fi
        echo "🔌 abrindo túnel: localhost:${LOCAL_PORT} → ${RDS_HOST}:${RDS_PORT} via ${BASTION_USER}@${BASTION_HOST}"
        echo "   Ctrl+C pra encerrar."
        exec ssh -i "$KEY" \
            -o ServerAliveInterval=30 \
            -o ServerAliveCountMax=3 \
            -o ExitOnForwardFailure=yes \
            -L "${LOCAL_PORT}:${RDS_HOST}:${RDS_PORT}" \
            -N "${BASTION_USER}@${BASTION_HOST}"
        ;;
    status)
        pids=$(tunnel_pids)
        if [[ -n "$pids" ]]; then
            echo "✅ túnel ativo (PIDs: $pids) em localhost:${LOCAL_PORT}"
        else
            echo "🚫 nenhum túnel ativo na porta ${LOCAL_PORT}"
            exit 1
        fi
        ;;
    stop)
        pids=$(tunnel_pids)
        if [[ -z "$pids" ]]; then
            echo "🚫 nenhum túnel ativo na porta ${LOCAL_PORT}"
            exit 0
        fi
        echo "🛑 matando PIDs: $pids"
        kill $pids
        ;;
    *)
        usage
        ;;
esac
