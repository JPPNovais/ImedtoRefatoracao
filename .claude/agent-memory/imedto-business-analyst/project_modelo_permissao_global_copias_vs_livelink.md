---
name: modelo-permissao-global-copias-vs-livelink
description: Por que modelo de permissão padrão do sistema usa cópias materializadas + propagação, NÃO live-link como os outros catálogos globais.
metadata:
  type: project
---

Modelos de permissão padrão do sistema (Admin/Médico/Recepção) gerenciáveis no Admin Global usam **cópias materializadas por tenant + propagação cross-tenant**, NÃO o live-link dos demais Catálogos Globais (modelo de prontuário, variável pool, região anatômica).

**Why:** `vinculo_profissional_estabelecimento.modelo_permissao_id` é FK para a cópia `eh_padrao=true` daquele estabelecimento, e as queries de autorização (`ModeloPermissaoRepository.UsuarioTemAcao`/`UsuarioTemPermissaoExtra`) fazem JOIN `v.modelo_permissao_id = mp.id` filtrando `v.estabelecimento_id`. Um registro global compartilhado (live-link) quebraria o vínculo por tenant. Por isso o registro global (`estabelecimento_id NULL` + `eh_padrao=true`) é só TEMPLATE; cada tenant mantém sua cópia, e editar o global PROPAGA (UPDATE) para as cópias correlacionadas por Nome.

**How to apply:** ao especificar qualquer feature que torne um modelo padrão de permissão editável/global, exigir propagação por cópias, não live-link. Correlação global↔cópia é por `Nome` (unique `estabelecimento_id, nome`). Registro global tem `estabelecimento_id NULL`, é isolado das queries de tenant (filtro `=@X` nunca casa NULL) e nunca referenciado por vínculo. Briefing canônico: [[planejamentos 2026-06-04_001]]. Contraste documentado em Docs/ARQUITETURA.md §Catálogos Globais (live-link via EhPadraoSistema=true). Regra de exclusão segura: bloquear se em uso por vínculo ativo em qualquer tenant (nunca deixar profissional órfão de permissão).
