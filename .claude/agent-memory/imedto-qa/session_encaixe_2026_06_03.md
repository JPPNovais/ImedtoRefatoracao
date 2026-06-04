---
name: session-encaixe-2026-06-03
description: Pipeline fechada — briefing 2026-06-03_001 encaixe desabilitado quando estabelecimento fechado; commits bbd81e1 + 5a80b37 pushed
metadata:
  type: project
---

Pipeline fechada em 2026-06-03 com 2 commits no mesmo push.

**Commit 1**: `bbd81e1` — `feat(atendimentos): desabilita "Novo encaixe" quando estabelecimento fechado`
- Briefing: planejamentos/2026-06-03_001_encaixe-desabilitado-estabelecimento-fechado.md
- Bug Tipo A corrigido: heurística de fim de expediente mudou de `s.disponivel` para `s.motivo !== "passado"` — agenda cheia de "agendado" não desabilitava mais o botão indevidamente.
- Helper puro testável: `frontend/src/utils/encaixeUtils.ts` → `calcularPodeEncaixar(dia, horaAtual)`.
- 7 testes em `encaixeUtils.test.ts` (366 total, todos verdes).
- Build verde (vue-tsc + vite). Lint: falha pré-existente em `.eslintrc.cjs` (desde `e2e3e52`, não introduzida aqui).
- Browser: EC2 inacessível no sandbox; validação por análise de código + testes.

**Commit 2**: `5a80b37` — `feat(pacientes): botão "Voltar para cadastro rápido" no PacienteFormModal`
- Sem briefing (melhoria de UX confirmada pelo usuário).
- Botão `v-if="expandirCadastro"` no modo criar-expandido; `:disabled="salvando"`; CSS via tokens HSL.

**Why:** Corrição de bug Tipo A devolvido na rodada anterior; encaixe 2ª rodada aprovada.
**How to apply:** Referência para entender que `calcularPodeEncaixar` é a função de aptidão de encaixe no front; `Estabelecimento.ValidarPodeAgendar` é o backend autoritativo.
