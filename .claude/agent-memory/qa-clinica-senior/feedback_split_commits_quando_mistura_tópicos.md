---
name: split-commits-quando-mistura-topicos
description: Quando working tree tem 2+ tópicos não relacionados (ex: tarefa atual + hotfix de outro QA), separar em commits distintos com git reset --mixed (não --hard).
metadata:
  type: feedback
---

Quando o working tree contém mudanças de **mais de um tópico** (ex: tarefa atribuída ao fullstack + hotfix descoberto por outro agente em sessão anterior que ficou no workspace), criar **commits separados** — não juntar.

**Why:** Surgical Changes (CLAUDE.md §3) — cada linha do commit precisa rastrear pro tópico anunciado. Misturar PDF redesign + hotfix LGPD num só commit polui a auditoria, dificulta revert seletivo e quebra a regra "every changed line traces directly to the user's request".

**How to apply:**
1. Antes de `git add`, rodar `git status --short` e categorizar cada arquivo em "Tópico A" ou "Tópico B".
2. `git add` paths específicos do tópico A → commit A.
3. `git add` paths do tópico B → commit B.
4. **Não usar `git reset --hard`** se já commitou errado — usa `git reset --soft HEAD~1` (preserva index) ou `--mixed` (preserva working tree). Hard reset é bloqueado por política e descarta trabalho.
5. Push os 2 commits juntos (1 push por sessão de trabalho — CLAUDE.md §CI/CD).

**Caso real (2026-05-16, sessão PDF redesign):**
Working tree tinha o redesign de PDF (tarefa atribuída) + 39 linhas em `authStore.ts` corrigindo bug LGPD diagnosticado pelo `clinica-qa-specialist` no pós-deploy 4cc451e. Primeira tentativa juntou tudo num só commit (e foi pushed automaticamente por hook antes do reset local). O remoto ficou com commit "feat(pdf): ..." carregando 3 arquivos do authStore não mencionados na mensagem — auditoria ruim. Reflexo: na próxima sessão, **inspecionar `git status` completo antes do primeiro `git add`** e perguntar "esses arquivos pertencem todos ao mesmo tópico?".
