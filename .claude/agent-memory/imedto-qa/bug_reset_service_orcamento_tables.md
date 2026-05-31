---
name: bug-reset-service-orcamento-tables
description: Tipo A — AdminResetService.cs linhas 187-206 referenciam tabelas de orçamento inexistentes; causa 500 em todo reset de tenant
metadata:
  type: project
---

**Sintoma:** POST `/api/admin/estabelecimentos/{id}/reset` retorna 500.

**Causa raiz:** `AdminResetService.cs` linhas 187-206 — 4 blocos `ExecuteAsync` com DELETEs para:
- `public.orcamento_anestesia` — nunca existiu no schema
- `public.orcamento_cirurgias` — nunca existiu no schema
- `public.orcamento_implantes` — nunca existiu no schema
- `public.orcamento_internacao` — foi dropada em `20260519020628_orcamento_local_cirurgia_paridade.sql:15`

**Fix:** Remover os 4 blocos `ExecuteAsync` das linhas 187-206. Tabelas reais do módulo Orçamento que existem: `orcamento_equipe`, `orcamento_formas_pagamento`, `itens_orcamento`, `orcamentos`. Confirmar via `SELECT table_name FROM information_schema.tables WHERE table_name LIKE 'orcamento%'` antes de ajustar.

**Por que:** A migration de paridade de local cirúrgico (2026-05-19) dropou `orcamento_internacao`; as outras 3 tabelas nunca foram criadas por nenhuma migration.

**How to apply:** Qualquer validação futura de reset de tenant que retornar 500 — verificar este arquivo primeiro antes de investigar elsewhere.

CAs afetados: CA30 (reset com nome incorreto → 422), CA31 (reset válido → 204).
