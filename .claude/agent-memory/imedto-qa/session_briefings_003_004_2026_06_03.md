---
name: session-briefings-003-004-2026-06-03
description: QA dos briefings 003+004a+004b — bug Tipo A aberto (CA7 front especialidade). Dev commitou e deu push sem QA. Commits em origin/main d8f7d58.
metadata:
  type: project
---

## Ciclo 1 (referência de sessão anterior)

Pipeline fechada em 2026-06-03 para os briefings:
- `2026-06-03_003_atalho-ativar-desativar-profissional-na-linha.md`
- `2026-06-03_004_templates-mensagem-convite-profissional.md`
- `2026-06-03_004_especialidade-por-vinculo-estabelecimento.md`

**Commits entregues:**
- `8b86b93` feat(equipe): atalho ativar/desativar profissional na linha da lista
- `8585f7a` feat(equipe): templates de mensagem no convite + especialidade por vínculo
- `d8f7d58` docs(equipe): testes e briefings dos briefings 2026-06-03_004

**Suíte:**
- Backend: 1236 passed, 77 skipped, 0 failed (build 0 erros/warnings)
- Frontend: 46 test files, 384 testes, todos passing; build ✓; typecheck ✓
- Lint: erro pré-existente `@typescript-eslint/recommended` não resolvível no ambiente local (não introduzido por esta feature)

**Armadilha**: `git stash -u` durante verificação de lint gerou conflito de merge em `frontend/src/components/ui/index.ts` (marcadores `<<<<<<< Updated upstream` / `>>>>>>> Stashed changes`). Resolvido mantendo a versão "Updated upstream" (que tinha os exports corretos). **Precaução futura**: nunca usar `git stash -u` durante ciclo de QA — use `git stash` simples ou evite stash durante validação.

**Armadilha**: remote tinha 2 commits novos (PR auto-claude #1) após o último push local. Rebase aplicado antes do push: `git stash && git rebase origin/main && git stash pop && git push`.

**Why:** Três briefings desenvolvidos juntos pelo dev no mesmo ciclo. Separação em commits limpos por feature.
**How to apply:** Se novamente houver múltiplos briefings numa sessão, separar em commits distintos mas push único. Usar `git stash` sem `-u` para não capturar arquivos de memória como conflito.

## Ciclo 2 (validação QA desta sessão)

**Dev commitou e deu push sem passar pelo QA** (commits d8f7d58, 8585f7a, 8b86b93 já em origin/main antes da validação QA). Protocolo de pipeline violado.

**Bug Tipo A — CA7 front (briefing 2026-06-03_004_especialidade)**:

Arquivo: `frontend/src/components/equipe/ProfissionalDetalhesModal.vue`, linha 63.

Sintoma: `podeEditarEspecialidade = vinculoId != null && !ehDono.value` usa o status do profissional listado como proxy de "Dono", não o papel do usuário logado.

Esperado (CA7): campo editável oculto para usuário não-Dono.
Observado: para qualquer usuário logado (incluindo Recepção/Profissional) abrindo o modal de um colega com vinculoId, o campo editável aparece.

Fix: importar `usePermissoesStore` e usar `permissoes.ehDono` na guarda:
`const podeEditarEspecialidade = computed(() => props.profissional?.vinculoId != null && permissoes.ehDono)`

Referência de padrão: `AbaProfissionais.vue` usa corretamente `permissoes.ehDono`.

**Suíte ciclo 2**: backend 1236 ✓, frontend 384 ✓, typecheck ✓, lint erro pré-existente.

## Ciclo 3 — fix CA7 aplicado e aprovado

**Commit**: `837b8be` — fix(equipe): corrigir RBAC do campo especialidade no modal de profissional (CA7)
**Push**: `d8f7d58..837b8be  main -> main` — 2026-06-03

Fix cirúrgico: importou `usePermissoesStore`, instanciou `permissoes`, trocou `!ehDono.value` por `permissoes.ehDono` no computed `podeEditarEspecialidade`. Comentário atualizado explicando a distinção entre `ehDono` (status do listado) e `permissoes.ehDono` (papel do logado).

Teste novo: `ProfissionalDetalhesModal.test.ts` — 4 testes (CA7: não-Dono oculta / Dono exibe; CA9: vinculoId null oculto com e sem Dono logado).

**Suíte ciclo 3**: build ✓, vue-tsc ✓, 388/388 Vitest ✓ (47 arquivos), lint erro pré-existente inalterado. Espelho back+front: AlterarEspecialidadeDoVinculoCommandHandler.cs rejeita não-Donos com BusinessException.

Pipeline briefing 2026-06-03_004_especialidade-por-vinculo-estabelecimento FECHADA para CA7+CA9 (CAs front). CAs back (CA1-CA6, CA8, CA10-CA12) validados em ciclos anteriores.
