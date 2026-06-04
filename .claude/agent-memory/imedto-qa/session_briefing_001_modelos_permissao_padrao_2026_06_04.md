---
name: session_briefing_001_modelos_permissao_padrao_2026_06_04
description: Briefing 2026-06-04_001 modelos de permissão padrão sistema — pipeline fechada commit c73de34 pushed
metadata:
  type: project
---

Pipeline fechada para o briefing 2026-06-04_001 (modelos de permissão padrão do sistema no Admin Global).

**Commit**: c73de34 pushed para main em 2026-06-04.

**Why:** CRUD admin dos 3 modelos de permissão padrão (Admin/Médico/Recepção) com propagação cross-tenant. Antes era hardcoded em CriarPadroes(); agora é gerenciável pelo admin global com propagação imediata para todas as cópias tenant.

**Suíte**: 1251 backend passando (77 skipped integração), 388 frontend passando.

**Novos testes**: 16 — 8 testes de domínio (CriarGlobal/CriarCopiaDeGlobal/SincronizarComGlobal) + 3+3+3 handlers (Criar/Atualizar/Excluir).

**Observação de UX (não bloqueante)**: exclusão no contexto admin dispara confirm() nativo do browser ANTES de emitir @excluido → a view abre AppModal de confirmação. Duas confirmações redundantes. Pode ser refinado em PR de polimento sem breaking change.

**How to apply:** Ao revisar features relacionadas a permissões ou PapelEditorModal, saber que existe o contexto='admin' via prop. Ao criar novos catálogos globais, verificar se a decisão live-link vs cópia materializada se aplica (modelo de permissão é a única exceção ao live-link, documentada em Docs/ARQUITETURA.md).

**Pendente validação manual em prod**:
- Aplicar as 3 migrations: ALTER nullable + CONCURRENTLY indices + seed dos 3 globais
- Smoke-test browser: criar/editar/excluir modelo padrão como admin
- CA6: criar novo estabelecimento e confirmar cópia reflete o global (não hardcode)
- CA3: confirmar que UsuarioTemAcao reflete remoção de permissão do padrão imediatamente

**Atomicidade**: UnitOfWorkFilter abre IDbContextTransaction por request — o _repo.Salvar(global) que faz SaveChangesAsync interno ainda está dentro da mesma transação, logo rollback automático se falhar na propagação.

**Links**: [[session_wave4_admin_global_final]] (precedente live-link dos demais catálogos globais)
