---
name: schema-conflito-planos-assinaturas
description: Tabelas planos/assinaturas existem com bigint IDs (domínio cliente). Admin usa imedto_planos/imedto_assinaturas com UUID.
metadata:
  type: project
---

As tabelas `planos` e `assinaturas` já existem no schema com IDs bigint e estrutura do domínio cliente:
- `planos`: id bigint, nome varchar(80), preco_mensal numeric(12,2), features_json jsonb, ativo, ordem
- `assinaturas`: id bigint, estabelecimento_id bigint, plano_id bigint (FK), status varchar(20), 1:1 com estabelecimento

**Why:** O briefing admin menciona "tabelas novas" com os mesmos nomes, mas na prática colidem. Decisão: usar prefixo `imedto_` para as versões admin.

**How to apply:** Qualquer migration ou query admin deve usar `imedto_planos` e `imedto_assinaturas`. Não confundir com `planos` e `assinaturas` (domínio cliente, bigint, legado). Documentar essa distinção no hand-off ao developer.
