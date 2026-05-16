---
name: qa-posdeploy-ede04a6-2026-05-16
description: Re-validação cenário E (relogin sem logout) commit ede04a6 — bug persiste após reload com nuance importante.
metadata:
  type: project
---

Commit `ede04a6` (bundle `index-31018efb.js`) tentou eliminar o vazamento de papel do cenário E (relogin sem logout + reload).

**Resultado**: ❌ AINDA QUEBRADO no fluxo principal. ⚠️ Recupera ao clicar em "Trocar estabelecimento".

**Detalhe do bug pós-reload (Médico → fetch login Dono → reload)**:
- Header user/email: atualizou para Dono ✅
- Badge: continua "Profissional · novaEra" ❌
- Sidebar: degenera para 1 item (só "Painel inicial") ❌
- Cards do home: somem (0 cards) ❌
- `/auth/bootstrap` retorna `papelDoUsuario: "Dono"` corretamente
- sessionStorage `imedto.estabelecimentoAtivo` mantém `papel: "Profissional"` (stale)
- Router-guard bloqueia `/financeiro` corretamente (papel "Profissional" no store)

**Recuperação parcial**: clicar no botão do perfil → "Trocar estabelecimento" dispara `selecionarEstabelecimento` que reconcilia tudo (badge "Dono", sidebar 10 itens, cards 6, sessionStorage com papel "Dono").

**Why**: o backend e o endpoint estão corretos. O bug está na camada Vue/Pinia: `init()` chama `/auth/bootstrap` mas NÃO sobrescreve o `papel` do estabelecimento ativo no `tenantStore` quando o sessionStorage já tem entry stale. Ele provavelmente faz merge/preserva o cached em vez de substituir.

**How to apply**: na próxima iteração de fix, garantir que `init()` (ou o composable de bootstrap) força sobrescrita do `estabelecimentoAtivo` no sessionStorage com o `papelDoUsuario` vindo do `/auth/bootstrap`, mesmo se a chave já existir. Espelhar a lógica do `selecionarEstabelecimento`.

Itens OK no smoke (após recuperação): /financeiro carrega, /minha-conta/lgpd com acentuação OK, busca-rapida sem PII (`{id, nomeCompleto}`).
