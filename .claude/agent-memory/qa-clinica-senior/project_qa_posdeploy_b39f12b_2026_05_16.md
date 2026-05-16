---
name: qa-posdeploy-b39f12b-2026-05-16
description: Loop2 fix P1 toast+AppConfirmDialog em config orçamento. AppConfirmDialog OK. Toast P0 trapped atrás do overlay do drawer.
metadata:
  type: project
---

Commit `b39f12b` (2026-05-16): fix P1 da rodada anterior — substitui `alert()/confirm()` nativos por `AppToast` + novo `AppConfirmDialog` em todos os 6 componentes de `frontend/src/components/orcamento/config/`, e remove `telefone` da listagem de anestesistas via `OrcamentoAnestesistaListaDto` (GET /{id} mantém telefone para o drawer).

**Resultados do QA em prod (https://app.imedto.com, conta `qa-orc-loop2-1778967201@imedtoteste.local`, estab=11):**

- P1.1 AppConfirmDialog (inativar): **OK** — modal centralizado, botão danger vermelho, sem `window.confirm`.
- P1.1 Toast fora do drawer (pós-inativar com dialog fechado): **OK** — sem alert nativo, mensagem visível.
- P1.1 Toast dentro do drawer (validação `Salvar` com campo vazio): **BUG P0** — toast renderiza no DOM em posição correta (bottom-center, z-index:1000) mas fica trapped atrás do overlay `bg-black/80 z-50` do AppDrawer.
- P1.2 LGPD anestesistas: **OK** — `GET /orcamentos/configuracoes/anestesistas` não retorna `telefone`; `GET /.../{id}` retorna; editar+salvar persiste.

**Bug P0 — toast trapped:**
- Raiz: `AppToast` renderiza dentro de `.config-tab` (descendant do `<main>`), e o overlay do `AppDrawer` é teleportado para `<body>` (Reka UI Dialog portal). Como o overlay vive no stacking context root e o toast em um stacking context aninhado (criado por algum ancestral), o z-index:1000 do toast NÃO supera o z-index:50 do overlay no body.
- Sintoma na recepção: ao tentar salvar sem preencher campo, parece que "nada aconteceu" — recepcionista clica de novo achando que travou.
- Correção: `<Teleport to="body">` no AppToast OU mover instâncias de `<AppToast>` para fora do template do drawer pai (para um wrapper `<Teleport to="body">`).
- Confirmado nos componentes: `ProcedimentosTab.vue:299` e `AnestesistasTab.vue:297`. Por inspeção do código (`grep AppToast components/orcamento/config/`) ocorre em todos os 5 tabs + `ProcedimentoProdutosLink.vue:196`.

**How to apply (para próximos QA):** sempre testar toast com drawer aberto (ex: clicar Salvar com campo obrigatório vazio). Toast ser visível no DOM ≠ toast visível na UI.
