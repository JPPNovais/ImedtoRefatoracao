#!/usr/bin/env bash
#
# dev-mobile.sh — sobe o ambiente de dev do APP MOBILE apontando para o banco da EC2.
#
#   Túnel SSH (localhost:5432 -> container imedto-postgres na EC2)
#   + Backend .NET (http://localhost:5050)
#   + App Mobile Vite (http://localhost:5174) com proxy /api -> backend local
#
# Uso:   ./dev-mobile.sh
# Parar: Ctrl+C
#
# A cada execução REINICIA: mata qualquer instância anterior (túnel/backend/mobile
# aberta em outro terminal) e sobe tudo do zero. Espelha o dev.sh, mas roda o app
# mobile (mobile/) no lugar do frontend web.

set -euo pipefail

# ── Config ───────────────────────────────────────────────────────────────────
EC2_HOST="ec2-user@56.125.254.136"
SSH_KEY="$HOME/.ssh/imedto-deploy.pem"
PG_CONTAINER="imedto-postgres"
LOCAL_PG_PORT=5432
BACKEND_PORT=5050
MOBILE_PORT=5174
DP_KEYS="/tmp/imedto/dp-keys"   # Data Protection keys (default /var/imedto é barrado no macOS)

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_DIR="/tmp/imedto"
BACKEND_LOG="$LOG_DIR/backend.log"
mkdir -p "$DP_KEYS" "$LOG_DIR"

# Cores
B=$'\033[1m'; G=$'\033[32m'; Y=$'\033[33m'; R=$'\033[31m'; C=$'\033[36m'; N=$'\033[0m'
say()  { echo "${C}▶${N} $*"; }
ok()   { echo "${G}✓${N} $*"; }
warn() { echo "${Y}!${N} $*"; }
die()  { echo "${R}✗ $*${N}" >&2; exit 1; }

TUNNEL_PID=""
BACKEND_PID=""

cleanup() {
  echo
  say "Encerrando…"
  [ -n "$BACKEND_PID" ] && { kill "$BACKEND_PID" 2>/dev/null || true; ok "backend parado"; }
  [ -n "$TUNNEL_PID" ]  && { kill "$TUNNEL_PID"  2>/dev/null || true; ok "túnel SSH fechado"; }
}
trap cleanup EXIT INT TERM

port_open() { nc -z localhost "$1" >/dev/null 2>&1; }

# Mata quem estiver escutando numa porta TCP e espera liberar.
kill_port() {
  local port="$1" name="$2" pids
  pids="$(lsof -ti tcp:"$port" -sTCP:LISTEN 2>/dev/null || true)"
  [ -z "$pids" ] && return 0
  warn "reiniciando $name — matando processo(s) em :$port ($(echo "$pids" | tr '\n' ' '))"
  kill $pids 2>/dev/null || true
  for _ in $(seq 1 20); do port_open "$port" || return 0; sleep 0.3; done
  kill -9 $pids 2>/dev/null || true; sleep 0.5
}

# ── 0. Reinício: derruba instâncias anteriores ───────────────────────────────
say "Verificando instâncias anteriores…"
# Túnel SSH antigo (mata pelo padrão do comando, sem tocar num Postgres local real)
if pgrep -f "ssh .*-L ${LOCAL_PG_PORT}:.*${EC2_HOST}" >/dev/null 2>&1; then
  warn "reiniciando túnel SSH anterior"
  pkill -f "ssh .*-L ${LOCAL_PG_PORT}:.*${EC2_HOST}" 2>/dev/null || true
  sleep 0.5
fi
kill_port "$BACKEND_PORT" "backend"
kill_port "$MOBILE_PORT"  "mobile"

# ── 1. Túnel SSH para o Postgres da EC2 ──────────────────────────────────────
if port_open "$LOCAL_PG_PORT"; then
  warn "porta $LOCAL_PG_PORT já ocupada (Postgres local?) — reutilizando, não abro túnel"
else
  [ -f "$SSH_KEY" ] || die "chave SSH não encontrada: $SSH_KEY"
  say "Resolvendo IP do container $PG_CONTAINER na EC2…"
  PG_IP="$(ssh -i "$SSH_KEY" -o ConnectTimeout=12 -o StrictHostKeyChecking=accept-new "$EC2_HOST" \
    "docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $PG_CONTAINER" 2>/dev/null)" \
    || die "falha ao consultar a EC2 (SSH/Docker)"
  [ -n "$PG_IP" ] || die "container $PG_CONTAINER sem IP — está rodando? (docker ps na EC2)"
  ok "container em $PG_IP:5432"

  say "Abrindo túnel localhost:$LOCAL_PG_PORT → $PG_IP:5432…"
  ssh -i "$SSH_KEY" -o ExitOnForwardFailure=yes -o ServerAliveInterval=30 \
      -o StrictHostKeyChecking=accept-new \
      -N -L "$LOCAL_PG_PORT:$PG_IP:5432" "$EC2_HOST" &
  TUNNEL_PID=$!

  for _ in $(seq 1 30); do port_open "$LOCAL_PG_PORT" && break; sleep 0.5; done
  port_open "$LOCAL_PG_PORT" || die "túnel não subiu"
  ok "túnel ativo (pid $TUNNEL_PID)"
fi

# ── 2. Backend .NET ──────────────────────────────────────────────────────────
say "Subindo backend (.NET) em :${BACKEND_PORT} (log em $BACKEND_LOG)…"
( cd "$ROOT/backend/src" && \
  ASPNETCORE_ENVIRONMENT=Development \
  ASPNETCORE_URLS="http://localhost:$BACKEND_PORT" \
  DataProtection__KeysPath="$DP_KEYS" \
  dotnet run --project Services/Imedto.Backend.API --no-launch-profile \
) > "$BACKEND_LOG" 2>&1 &
BACKEND_PID=$!

say "Aguardando /health (compila na 1ª vez, ~30s)…"
for _ in $(seq 1 90); do
  curl -sf "http://localhost:$BACKEND_PORT/health" >/dev/null 2>&1 && break
  kill -0 "$BACKEND_PID" 2>/dev/null || die "backend morreu — veja $BACKEND_LOG"
  sleep 1
done
curl -sf "http://localhost:$BACKEND_PORT/health" >/dev/null 2>&1 \
  || die "backend não respondeu /health — veja $BACKEND_LOG"
ok "backend no ar (pid $BACKEND_PID)"

# ── 3. App Mobile Vite (foreground) ──────────────────────────────────────────
[ -d "$ROOT/mobile/node_modules" ] || { say "Instalando deps do mobile…"; ( cd "$ROOT/mobile" && npm install ); }

echo
ok "${B}Ambiente mobile pronto${N}"
echo "   ${B}Mobile  ${N}  http://localhost:$MOBILE_PORT   (abra no navegador / DevTools mobile 375px)"
echo "   ${B}Backend ${N}  http://localhost:$BACKEND_PORT   (Swagger: /swagger)"
echo "   ${B}Banco   ${N}  container $PG_CONTAINER na EC2 (via túnel)"
echo "   ${Y}Dados são do banco de dev REAL da EC2 — alterações persistem lá.${N}"
echo "   ${C}Ctrl+C para encerrar.${N}"
echo

# O proxy /api do Vite (vite.config.ts) aponta para VITE_API_PROXY_TARGET = backend local.
# Server-side, então o browser não dispara CORS; o cookie de sessão sobrevive entre requests.
cd "$ROOT/mobile"
VITE_API_PROXY_TARGET="http://localhost:$BACKEND_PORT" npm run dev
