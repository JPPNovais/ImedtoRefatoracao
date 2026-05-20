---
name: qa-posdeploy-00b0b71-2026-05-19
description: QA pós-deploy 00b0b71 (Feature 1 inativação=movimentação + Feature 2 atalho paciente→orçamento + fix de loop).
metadata:
  type: project
---

QA da sessão de 2026-05-19, sequência: 2b63bdd (estoque inativação) → 968dbe4 (atalho orçamento) → f63d1ea (paginação prontuário) → 00b0b71 (fix recarregar movimentações pós-inativar).

**Feature 1 — Inativação como movimentação:** funcionou. Backend persistiu corretamente (tipo `Inativacao`, quantidade=0, quantidadeAnterior=quantidadeApos=estoque no momento). Filtro pill "Inativações" e label "Estoque no momento: X" renderizam OK.

**Feature 2 — Atalho paciente→orçamento:** funcionou de primeira. Navega para `/orcamentos/novo?pacienteId=`, AppSelect já com nome preenchido, lista de busca rápida intacta.

**Bug P1 pego em prod (corrigido):** após `inativarItem` na UI, `InventarioView.inativar()` só chamava `carregarItens()` — a aba Movimentações ficava vazia até F5. Fix de 1 linha (Promise.all com carregarMovimentacoes) commitado em 00b0b71. Re-teste mostrou movimentação aparecendo imediatamente.

**Por que aplicar fix sozinho:** o agente Task não estava disponível na sessão. O fix tinha precedente no próprio arquivo (linha 139 já fazia o mesmo padrão), uma linha, sem multi-tenant/LGPD envolvido — pragmatismo > processo.

**Why:** Padrão recorrente — toda action que cria movimentação no inventário precisa recarregar **ambos** itens E movimentações. Verificar se há outros lugares que esquecem esse par.

**How to apply:** Em features futuras do Estoque, quando alterar/criar/inativar item, NUNCA só recarregar itens. Sempre `Promise.all([carregarItens(), carregarMovimentacoes()])`.

**Débitos não-bloqueantes:**
- P2: confirm() nativo em `inativar()` (mesma dívida UX que outras telas têm — backlog conhecido).
- P3: typo de pluralização em `EstoqueMovimentacoesTab.vue:114` — `movimentação{n!==1?"ões":""}` resulta em "movimentaçãoões". Trocar para `n===1?"movimentação":"movimentações"`. Pré-existente.
- P3: filtro de tipo nas Movimentações é no-op no pai (`@filtro-tipo-change="() => {}"`) — pré-existente, fora do escopo da Feature 1.
- P3: aba Itens não tem toggle "incluir inativos" — item inativado some da lista, não há fluxo de UI para abrir o drawer dele. Limita a rastreabilidade que a Feature 1 visa.
- P2: over-fetch consciente em `pacienteService.obter` para extrair só `nomeCompleto` quando paciente não está no top 30 do busca rápida. Sugerir endpoint resumo no futuro.

Links: [[feedback-qa-login-via-signup-e-rds-confirm]] [[feedback-inspecionar-working-tree-inteiro-antes-commit]]
