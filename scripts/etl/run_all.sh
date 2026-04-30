#!/usr/bin/env bash
# ============================================================================
# Wrapper de execucao do ETL Imedto -- Wave 2 / Fase 5
#
# Uso:
#   PSQL_URL="postgresql://etl_user:senha@localhost:5432/imedto_intermediario" \
#     ./run_all.sh
#
# Pre-requisitos:
#   1. DB intermediario (Postgres local) com schemas "legado" e "destino_seed"
#      ja populados via pg_dump (legado completo + catalogos do destino).
#   2. _etl.mapping_usuarios pre-populada pelo runbook de convites.
#
# Cada script roda em sua propria transacao -- falha em um aborta o pipeline.
# ============================================================================

set -euo pipefail

PSQL_URL="${PSQL_URL:?defina PSQL_URL apontando para o DB intermediario}"
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

PSQL_OPTS=(
    -v ON_ERROR_STOP=1
    -v VERBOSITY=terse
    -X
    -P pager=off
)

scripts=(
    "00_setup_intermediate.sql"
    "01_ref_data.sql"
    "02_tenant_base.sql"
    "03_dominio_core.sql"
    "04_configuracoes.sql"
    "05_transacional_agenda_pacientes.sql"
    "06_transacional_clinico.sql"
    "07_transacional_financeiro_orcamento.sql"
    "08_audit.sql"
    "99_validacao.sql"
)

start_ts=$(date +%s)
for s in "${scripts[@]}"; do
    echo ""
    echo "=========================================================="
    echo "==> $s ($(date +%H:%M:%S))"
    echo "=========================================================="
    psql "$PSQL_URL" "${PSQL_OPTS[@]}" -f "${DIR}/${s}"
done

echo ""
echo "ETL concluido em $(( $(date +%s) - start_ts ))s."
