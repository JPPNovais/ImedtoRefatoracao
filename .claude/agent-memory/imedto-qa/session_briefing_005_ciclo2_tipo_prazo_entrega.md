---
name: session-briefing-005-ciclo2-tipo-prazo-entrega
description: Ciclo 2 de QA do briefing 2026-06-04_005 — correção dos 2 bugs Tipo A; aprovado e push feito em 187cd33.
metadata:
  type: project
---

Briefing 2026-06-04_005 (tipo de prazo de entrega do fornecedor) aprovado no ciclo 2.

**Correções validadas:**
- Bug 1 (controller não mapeava TipoPrazoEntrega): corrigido em CadastrosEstoqueController.cs linhas 248 e 269. Record FornecedorPayloadDto tem campo com default "corridos". Teste de regressão: FornecedorPayloadDtoMapeamentoTests (2 testes, deserialização JSON→DTO).
- Bug 2 (v-maska quebrando build): corrigido em OnboardingView.vue:711 (forma objeto). Já estava em commit dbae7fb antes do hand-off do ciclo 2.

**Números de suíte:**
- Backend: 1271 passed (Test) + 17 passed (IntegrationTest, 2 novos)
- Frontend: build TS verde | Vitest: 413/414 passed (1 falha em OrcamentoListaView — briefing 006, escopo diferente)

**Commits:**
- 187cd33 feat(estoque): tipo de prazo de entrega do fornecedor (corridos/úteis)
- 3d0a3cf chore(ui): caronas de UI — ajustes visuais e correções menores

**Pendências:**
- Briefing 006 (OrcamentoFormView dropdowns): implementado pelo dev mas com teste falhando. Devolvido ao dev como Tipo A. Teste OrcamentoListaView.test.ts não inicializa Pinia — mount falha com "getActivePinia was called but there was no active Pinia".
- Migration db/migrations/20260604180000_tipo_prazo_entrega_fornecedor.sql: pendente de apply em prod.

**Why:** Pipeline fechada para o 005; 006 bloqueado por falha de teste.
**How to apply:** Na próxima sessão, retomar a partir do briefing 006 após dev corrigir o teste.
