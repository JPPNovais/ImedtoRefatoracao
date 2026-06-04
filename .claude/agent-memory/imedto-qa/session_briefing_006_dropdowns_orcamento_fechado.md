---
name: session-briefing-006-dropdowns-orcamento-fechado
description: Briefing 2026-06-04_006 dropdowns orçamento — pipeline fechada após ciclo 2 (bug Tipo A corrigido pelo dev). Commit 6550bae pushed.
metadata:
  type: project
---

Pipeline fechada no ciclo 2. Commit 6550bae pushed.

Bug Tipo A reportado na sessão anterior: OrcamentoListaView.test.ts falhava porque usePermissoesStore não estava mockado (Pinia não inicializado no mount).

Correção do dev: adicionou vi.mock("@/stores/permissoesStore") com mockPode resetado em beforeEach (padrão do HomeView.test.ts), extraiu criarRouter() e STUBS, adicionou CA-2 de regressão RBAC (usuário sem orcamento.configurar não vê o botão "Novo orçamento").

Resultado final: 48 arquivos de teste, 415 testes passando. Backend 1271/1271. Build limpo. Typecheck limpo. Lint pré-existente (eslintrc desde commit inicial).

Arquivos do briefing 006:
- frontend/src/router/routePermissions.ts (gate elevado para orcamento.configurar)
- frontend/src/views/orcamentos/OrcamentoListaView.vue (v-if podeConfigurar)
- frontend/src/views/orcamentos/OrcamentoListaView.test.ts (mock + CA-2)
- frontend/src/views/orcamentos/OrcamentoDetalheView.vue (v-if podeEditar && podeConfigurar)
- frontend/src/views/orcamentos/OrcamentoFormView.vue (Promise.allSettled + bloqueioPermissao + falhaCatalogos + combobox paciente)
- backend/.../InventarioQueryRepository.cs (carona: filtro tipo movimentação)

Follow-up F1 pendente: fluxo de cadastro de valores-profissional (honorários) — orcamento_valor_profissional vazia. Requer briefing próprio.

**Why:** gate OrcamentoNovo/OrcamentoForm/OrcamentoSettings estava em orcamento.ver mas backend exige orcamento.configurar — usuário com apenas ver entrava na tela e levava 422 em todos os 7 catálogos via Promise.all que derrubava tudo.
**How to apply:** nas próximas features de orçamento, confirmar que routePermissions.ts espelha [RequiresAcao] do backend antes de fechar.
