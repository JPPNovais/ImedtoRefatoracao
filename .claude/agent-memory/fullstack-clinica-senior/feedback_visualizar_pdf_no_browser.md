---
name: feedback-visualizar-pdf-no-browser
description: Padrão para abrir PDF em nova aba sem ser bloqueado por popup blocker (composable retorna blobUrl, view abre about:blank antes do await).
metadata:
  type: feedback
---

Toda feature de "visualizar PDF em nova aba" precisa lidar com popup blocker do Chrome/Safari: se chamar `window.open` depois de um `await`, o browser bloqueia.

**Why:** chamadas a `window.open` precisam estar dentro do mesmo turno síncrono do clique do usuário. Como o front faz `await registrarExportacao...` (audit LGPD) antes de gerar o PDF, qualquer `window.open` posterior é considerado não-iniciado-por-usuário.

**How to apply:** dividir responsabilidade entre composable e view:
- O composable de PDF aceita `modo: "download" | "visualizar"` e, em "visualizar", **não** chama `window.open` — apenas retorna `{ blobUrl }` via `doc.output("bloburl")`. Também seta `doc.setProperties({ title })` para a aba ficar com nome legível e agenda `URL.revokeObjectURL` em 60s.
- A view chama `window.open("about:blank", "_blank", "noopener,noreferrer")` ANTES de qualquer `await`, guarda a referência, faz audit + PDF, e depois faz `janela.location.href = blobUrl`. Se `window.open` retornar `null` (popup bloqueado), avisa via toast e cai para `"download"` como fallback.

Exemplo canônico no projeto: [[project-pdf-redesign-2026-05-16]] + `useProntuarioPdf.ts` + `ProntuarioView.vue` `exportarHistorico(modo)`.
