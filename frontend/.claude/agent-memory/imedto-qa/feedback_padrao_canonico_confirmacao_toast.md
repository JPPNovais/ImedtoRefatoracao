---
name: feedback_padrao_canonico_confirmacao_toast
description: Padrão canônico de confirmação e feedback no DS — AppConfirmDialog + AppToast local; erradicação de confirm()/alert() nativos confirmada em 2026-06-04
metadata:
  type: feedback
---

O app usa AppConfirmDialog (confirmar antes de agir) e AppToast via notificar() local (avisar depois de agir). Nunca window.confirm()/window.alert()/notificacoesStore para toast.

Gabarito de referência: `src/components/orcamento/config/ProcedimentosTab.vue`.

Padrão de estado local por componente (não composable global — decisão explícita de produto):
- `const confirmacao = ref<{ aberto, alvo, executando }>({ aberto: false, alvo: null, executando: false })`
- `const toast = ref<{ mensagem, variante } | null>(null)` + `function notificar(mensagem, variante)`
- `variante="danger"` para destrutivo; `variante="primary"` só para transição não-destrutiva (ex.: converter orçamento)
- FinanceiroView com 2 ações distintas: dois estados independentes (confirmacaoPagar, confirmacaoCancelar)
- PapelEditorModal em contexto admin: dispara direto sem dialog interno (a list view já confirma impacto cross-tenant)

Erradicação completa validada em 2026-06-04 (briefing 002): grep retornou zero calls nativas em frontend/src/.

**Why:** window.confirm() trava thread do browser, não é estilizável, é hostil em mobile e contorna o DS.
**How to apply:** ao revisar qualquer diff de view/component, grep por `confirm(` e `alert(` logo no início do quality gate — CA1/CA2 do briefing 002 são os critérios canônicos de verificação.
