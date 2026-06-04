---
name: project-modelos-permissao-padrao-sistema
description: Briefing 2026-06-04_001 implementado — CRUD admin de modelos de permissão padrão do sistema com propagação cross-tenant. Backend 0 erros, 1251 testes verdes. Frontend build verde, 388 testes verdes. Aguarda migration do imedto-database.
metadata:
  type: project
---

Briefing 2026-06-04_001 implementado em 2026-06-04.

**Por que:** modelos de permissão padrão (Admin/Médico/Recepção) só existiam hardcoded; dono quer gerenciar pelo Admin Global com propagação retroativa.

**Arquitetura chave:** NÃO usa live-link (diferente dos outros catálogos globais). Usa cópias materializadas: registro global tem `estabelecimento_id=NULL`, cópias têm `estabelecimento_id NOT NULL`. Propagação por Nome. FK de vínculo aponta para cópia do tenant.

**Schema muda:** `estabelecimento_id` em `modelo_permissao_estabelecimento` agora nullable + unique parcial `WHERE estabelecimento_id IS NULL` sobre `nome`. Seed dos 3 padrões globais. **Migration pendente com imedto-database.**

**How to apply:** ao tocar modelo de permissão ou propagação, lembrar: registro global = NULL, cópias = NOT NULL, SincronizarComGlobal preserva PermissoesExtrasJson (R8).
