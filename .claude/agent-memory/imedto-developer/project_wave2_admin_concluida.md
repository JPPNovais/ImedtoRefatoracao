---
name: project-wave2-admin-concluida
description: Wave 2 do módulo admin global implementada em 2026-05-30 — 40 CAs, 4 frentes sequenciais
metadata:
  type: project
---

Wave 2 Admin Global (briefing `planejamentos/2026-05-30_002_admin-global-wave2.md`) implementada em 2026-05-30.

**Why:** Habilitar o admin global a operar de fato durante a fase de testes — mudança de plano, controle de trial, gestão de catálogos.

**How to apply:** Schema aplicado pelo imedto-database. Build verificado: dotnet 0 erros, npm build limpo, 359 testes Vitest verdes.

Frentes entregues:
1. Bug fix — AssinaturaCard montada em EstabelecimentoDetalheView
2. Configs globais — IConfigGlobalReader + CRUD + trial dinâmico
3. Catálogos globais — modelos prontuário, variáveis pool, regiões anatômicas (admin + importação tenant)
4. Redesign — AdminLayout com zero hex codes, tokens HSL, badge Admin, faixa 2px warning

Padrão de tenant import: POST /api/prontuario/modelos/importar-do-global/{id} cria cópia independente.
Mapeamento de tipo: global (lista/texto/etc.) → TipoVariavelPool tenant (best-effort: lista→Medicamento, outros→Doenca).
