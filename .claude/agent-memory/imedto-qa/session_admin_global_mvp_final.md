---
name: session-admin-global-mvp-final
description: Pipeline 2026-05-30_001 fechada — commit 3d602e6 pushed com 1 bug Tipo A aberto no AdminResetService
metadata:
  type: project
---

Briefing `planejamentos/2026-05-30_001_admin-global-mvp.md` passou pelo QA em 7 rodadas.
Commit squashado `3d602e6` pushed para origin/main em 2026-05-30.

**Por que:** 140 arquivos, 19k linhas — MVP completo do painel admin global.

**How to apply:** Se o orquestrador perguntar sobre o estado do briefing 2026-05-30_001, está fechado com 1 bug Tipo A pendente.

CAs aprovados: CA1–CA16, CA18–CA20, CA24–CA29, CA33–CA36, CA38–CA52.
CA30/CA31 (reset de tenant): endpoint retorna 500 — bug Tipo A devolvido ao `imedto-developer`. Ver [[bug-reset-service-orcamento-tables]].

Bugs corrigidos durante o ciclo (A1–A12):
- A1: Seed hash errado
- A2: AuditWriter não-virtual
- A3: AdminController.cs legado
- A4: OnboardingCompletadoFilter sem bypass admin
- A5–A10: vários ajustes de DI, seed, filtros
- A11: e.email em SELECT do detalhe (coluna inexistente)
- A12: AssinaturaAdminDto record → class POCO

ESLint quebrado (`@typescript-eslint/recommended`) é pré-existente — não introduzido pelo diff, não bloqueia commit.
