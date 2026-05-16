---
name: qa-posdeploy-2af23b6-2026-05-16
description: Pós-deploy 2af23b6 — fix definitivo do Cenário E (relogin sem logout) via sobrescrita de papel no popularEstabelecimentos do tenantStore.
metadata:
  type: project
---

Pós-deploy do commit `2af23b6` em 2026-05-16, bundle `index-06ed1594.js`.

**Resultado**: Cenário E (relogin direto via API sem logout passando de Médico → Dono) RESOLVIDO de ponta a ponta.

**Why**: As rodadas anteriores (8ad2f2c, ede04a6, 19da92c) mitigavam parcialmente — detectavam o conflito mas deixavam UI em estado misto (badge stale, sidebar capada). A causa raiz era `tenantStore.popularEstabelecimentos` fazer spread preservando `papel` antigo do cache em vez de sobrescrever com `papelDoUsuario` retornado pelo `/auth/bootstrap`.

**How to apply**:
- Cenário E entra como teste de regressão obrigatório para qualquer mexida em `authStore.init()`, `tenantStore.popularEstabelecimentos`, ou ordem de hidratação (sessionStorage vs bootstrap).
- Para reproduzir: login Médico → no console `fetch('/api/auth/login', ...)` com creds Dono → reload. Após reload, badge deve ser "Dono", sidebar 10 itens, sessionStorage `papel: "Dono"`, /financeiro abre.
- Evidências em [[qa-screenshots-cenarioE-RESOLVIDO]] (`.qa-screenshots/pos-deploy-cenarioE-RESOLVIDO-*.png`).
- Smoke: LGPD acentuação OK, modal Novo agendamento usa `/api/paciente/busca-rapida` retornando só `{id, nomeCompleto}` (sem PII).

**Estado dos gaps anteriores**:
- ✅ Cenário E (relogin sem logout): resolvido.
- ✅ Badge/sidebar/cards/sessionStorage todos refletem papel correto após reload.
- ✅ /financeiro libera (não redireciona /home).
- Sem regressões em smoke; sem warnings/errors no console.
