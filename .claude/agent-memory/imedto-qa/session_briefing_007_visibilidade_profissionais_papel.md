---
name: session-briefing-007-visibilidade-profissionais-papel
description: Pipeline fechada briefing 2026-06-04_007 — AppPopover + AbaPapeis; bug CA10 corrigido em 2 rodadas
metadata:
  type: project
---

Pipeline do briefing 2026-06-04_007 fechada com commit 1b8f353 pushed.

**Rodadas**: 2. Rodada 1 devolveu CA10 Tipo A (foco no span display:contents era no-op). Rodada 2 aprovada.

**Fix CA10**: fechar() faz await nextTick() e depois querySelector('button, [tabindex="0"], a[href]') dentro do gatilhoRef para encontrar o elemento focável real (o span wrapper com display:contents não é focável).

**Testes**: 436/436 passing. 34 novos (AppPopover x2, AbaPapeis x32).

**Arquivos commitados** (7):
- frontend/src/components/ui/AppPopover.vue
- frontend/src/components/ui/AppPopover.test.ts
- frontend/src/components/ui/index.ts
- frontend/src/components/equipe/AbaPapeis.vue
- frontend/src/components/equipe/AbaPapeis.test.ts
- Docs/DESIGN.md
- planejamentos/2026-06-04_007_visibilidade-profissionais-por-papel.md

**Observação lint**: ESLint falha com @typescript-eslint/recommended desde antes desta sessão — bug pré-existente na main, não regressão.

**Why:** Referência para regressão futura em AppPopover (foco ao fechar) e padrão de deduplicação por usuarioId em AbaPapeis.

**How to apply:** Se AppPopover for modificado, garantir que fechar() mantém o querySelector antes do focus(). Se AbaPapeis for modificado, garantir que dedup por usuarioId (R3) não é quebrada.
