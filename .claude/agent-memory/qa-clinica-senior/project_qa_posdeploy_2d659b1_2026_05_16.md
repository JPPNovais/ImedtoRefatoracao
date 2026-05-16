---
name: qa-posdeploy-2d659b1-2026-05-16
description: QA loop 3 do fix Teleport AppToast — toast acima do overlay do drawer validado em 5 abas.
metadata:
  type: project
---

QA pós-deploy do commit `2d659b1` em 2026-05-16 — fix do P0 detectado no loop 2 ([[project_qa_posdeploy_b39f12b_2026_05_16]]): AppToast envolvido com `<Teleport to="body">`.

**Pipeline**: 6/6 jobs success (test-backend, test-frontend, build-push, migrate, deploy, smoke).

**Cenários validados em prod (app.imedto.com)**, conta QA `qa-toast-1778968557987@imedto.test`, Estabelecimento Id=12, plano Pro/Trial:

- Procedimentos: Salvar vazio → toast "Descrição é obrigatória." VISÍVEL bottom-center, acima do overlay. ✅
- Anestesistas: Salvar vazio → toast "Nome é obrigatório." visível. ✅
- Produtos: Salvar vazio → toast "Nome é obrigatório." visível. ✅
- Equipe: Salvar vazio → toast "Papel é obrigatório." visível. ✅
- Pacotes: Salvar vazio → toast "Nome é obrigatório." visível. ✅
- Regressão fora do drawer: criar Dr. QA Toast (sucesso) e inativar (AppConfirmDialog OK + sucesso). ✅

**Status final**: ✅ pronto pra produção. P0 do loop 2 eliminado nas 5 abas.

**Aprendizado técnico relevante** (não regressão, mas vale registrar):
- `elementFromPoint` em cima de toast com Reka Dialog aberto retorna o overlay (não o toast). Isso NÃO é regressão — Reka aplica `pointer-events: none` no `<body>`, e o toast herda `pointer-events: none`. Hit testing não pega o toast, mas a renderização visual é correta (confirmado por screenshot). Esse comportamento é intencional: toast não recebe clique, só feedback visual.
- A validação correta do "toast acima do overlay" é por screenshot + propriedade DOM (`parentElement === document.body`, `position: fixed`, `z-index` ≥ 50, `opacity > 0`, retângulo dentro da viewport). NÃO usar `elementFromPoint` como critério.

Screenshots em `.qa-screenshots/config-orcamento-loop3-teleport-toast/`.
