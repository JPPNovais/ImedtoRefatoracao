---
name: session-briefing-004-seletor-estabelecimento-2026-06-04
description: Sessão QA do briefing 2026-06-04_004 — pipeline fechada após 1 ciclo de correção (dead code Tipo A). Commits 44bd3c9+7a15294+7f27933 locais, aguardando push.
metadata:
  type: project
---

Pipeline fechada na re-validação. Bug Tipo A da sessão anterior (dead code IEstabelecimentoRepository) corrigido pelo dev.

**Commits criados (locais, sem push — aguardando decisão do usuário):**
- `44bd3c9` feat(tenant): seletor de troca de estabelecimento com persistência server-side do último acessado
- `7a15294` feat(equipe): visão compacta de permissões herdadas no modal do profissional + fix AppSelect
- `7f27933` docs(dev): script dev.sh para subir ambiente local com túnel SSH + backend + frontend

**Suíte verde:** 1265 backend (0 falhas) + 410 frontend (0 falhas), vue-tsc limpo, vite build OK. Lint ESLint com erro pré-existente (config `@typescript-eslint/recommended`), não é regressão.

**CAs validados (12/12):** todos confirmados por análise de código + suíte. Banco EC2 sem túnel impediu validação funcional de browser (chrome-devtools indisponível no sandbox).

**Decisão de projeto:** `ultimoEstabelecimentoId` exposto apenas via `/auth/bootstrap`, não via `/auth/me`. Reload completo via `window.location.href` ao trocar tenant (CA11 — evita estado stale na SPA). Falha silenciosa em `gravarUltimoEstabelecimento` em todos os pontos de chamada (R7).

**How to apply:** Próximas features que toquem tenantStore ou AppLayout devem respeitar o contrato de `popularEstabelecimentos` (retorna bool indicando uso de fallback) e o endpoint `POST /api/auth/ultimo-estabelecimento`.
