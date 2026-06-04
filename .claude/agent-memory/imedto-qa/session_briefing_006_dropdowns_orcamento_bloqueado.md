---
name: session-briefing-006-dropdowns-orcamento-bloqueado
description: Briefing 2026-06-04_006 (dropdowns orçamento) implementado mas bloqueado por teste quebrado — devolvido ao dev como Tipo A.
metadata:
  type: project
---

Briefing 2026-06-04_006 (corrigir dropdowns quebrados no form de orçamento) foi implementado
pelo dev junto com o 005, mas não foi hand-off formal. O QA detectou o problema ao rodar Vitest.

**Sintoma:** OrcamentoListaView.test.ts > CA-1 falha com:
  "[🍍]: getActivePinia() was called but there was no active Pinia."

**Causa raiz:** A view OrcamentoListaView.vue foi modificada para usar usePermissoesStore()
(para ocultar botão "Novo orçamento" para quem não tem orcamento.configurar — CA2 do briefing 006),
mas o teste não foi atualizado para incluir createPinia() nos plugins do mount.

**Arquivo/linha:** frontend/src/views/orcamentos/OrcamentoListaView.test.ts, linha 42 — o mount
precisa de `plugins: [router, createPinia()]` em vez de apenas `[router]`.

**Arquivos do 006 não commitados (aguardam correção):**
- frontend/src/views/orcamentos/OrcamentoFormView.vue
- frontend/src/views/orcamentos/OrcamentoListaView.vue (tem o teste quebrado)
- frontend/src/views/orcamentos/OrcamentoDetalheView.vue
- frontend/src/router/routePermissions.ts

**Why:** Teste falhando indica que o teste foi esquecido ao adicionar dependência de store.
**How to apply:** Dev deve adicionar createPinia() no mount do teste e re-submeter.
