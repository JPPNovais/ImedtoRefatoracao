---
name: qa-posdeploy-3ce5e7f-2026-05-20
description: QA pós-deploy 3ce5e7f — Fase 4 (aceite_link UI completa) + Fase 5 (LgpdConsentimento arquivado). Tudo verde.
metadata:
  type: project
---

Commit `3ce5e7f` em 2026-05-20. Pipeline `26158059776` 6/6 verde (inc. `migrate`).

**Fase 4 — UI aceite_link entregue end-to-end**:
- Passo 3 do EmitirTermoModal: opção "Enviar link de aceite" habilitada (sem badge "Em breve") com sub-opções `email` (default + e-mail do paciente exibido) e `copia` (sem cooldown).
- Passo 4 (novo): stepper oculto, mensagem de sucesso, input readonly com link, botão Copiar reativo ("Copiado" + check). Validado: POST `/api/pacientes/{id}/termos` com `assinaturaTipo=aceite_link` retorna 201 + `tokenAceite`.
- Ações inline na lista: "Copiar link" (toast "Link copiado.") e "Reenviar e-mail" (via `POST /api/termos/{id}/reenviar-link`).
- Cooldown 5 min funcional: email_1 → 200, email_2 → 422 "Aguarde 5 min antes de reenviar o e-mail.", copia → 200 sem cooldown (mesmo token).
- Rota pública `/termos/aceite/:token` é UI completa (não 501): banner, dados do estabelecimento/profissional, texto preenchido, checkbox "Li e estou de acordo", botão "Aceitar termo" (habilita só após checkbox).
- POST público `/api/publico/termos/aceite/{token}` com `{aceito:true}` → 200 `{resultado: "registrado"}`. Replay com `{aceito:false}` → 200 `{resultado: "ja_respondido"}` (idempotente, não altera estado).
- Lista do estabelecimento reflete: termo passa Pendente→Assinado, contador "Assinados 1", ações trocam para Visualizar/PDF/Revogar.

**Fase 5 — Lgpd Consentimento arquivado**:
- Migration `20260520105013_ArquivarLgpdConsentimentos` aplicada: tabela renomeada (job migrate verde).
- `GET /api/minha-conta/exportar-dados` → 200 com `consentimentos: []` (key mantida pra compat, array vazio).
- `GET /api/lgpd/consentimentos` → 404 (controller deletado).
- LgpdAnonimizacao intacta (não testei em prod mas código não foi tocado).

**Multi-tenant validado**: header `X-Estabelecimento-Id=99999` em `/api/pacientes/220/termos` → 404 "Estabelecimento não encontrado." (mensagem genérica, sem leak).

**Pitfall reconfirmada**: `mcp__chrome-devtools__click` não dispara handler Vue no AppButton — usar `dispatchEvent(new MouseEvent('click', { bubbles: true, view: window }))` via evaluate_script. Sequência mouse+pointer+click necessária às vezes (botão "Aceitar termo" trocou para Reka Dialog de confirmação que abriu/fechou rápido — fluxo via API direto foi mais determinístico).

**Convenção API**: enum `assinaturaTipo` vem PascalCase no GET (`"AceiteLink"`) mas snake_case no POST (`"aceite_link"`). Não é bug — frontend faz a tradução.

**Header obrigatório**: queries de domínio multi-tenant pedem `X-Estabelecimento-Id` — ausência retorna 422 `TenantAusente`. Coerente com checklist multi-tenant.

**Débitos remanescentes da lista original (3 ciclos: 837b50d → ff2ff5d → 3ce5e7f)**:
- Nenhum P0/P1 aberto da feature Termos de Consentimento.
- P2 SecaoExameFisico ainda usa input cru (vem do ciclo de prontuário, alheio aos Termos).

[[qa-posdeploy-ff2ff5d-2026-05-20]] | [[qa-posdeploy-837b50d-2026-05-19]] | [[elementfrompoint-armadilha-reka-dialog]]
