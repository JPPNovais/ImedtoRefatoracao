---
name: session-wave2-final
description: Wave 2 admin global — pipeline fechada, commit ad3238a pushed. Débito técnico W2-CA29/CA32 registrado no commit.
metadata:
  type: project
---

Wave 2 do admin global (briefing 2026-05-30_002) fechada em commit ad3238a, push origin main em 2026-05-30.

101 arquivos alterados, 13376 inserções.

**Why:** 40 CAs validados — Frentes 1 (AssinaturaCard), 2 (configs globais + fix serialização), 3 (catálogos globais CRUD + tenant importar), 4 (redesign tokens HSL).

**How to apply:** Gates verde: build .NET (0 erros), 1136 testes passando, frontend build/typecheck verde, Vitest 359 testes, zero hex no módulo admin.

Débito técnico explícito registrado no commit:
- W2-CA29: AdminLayout não usa AppSidebar/AppTopBar do DS — implementação própria com tokens corretos.
- W2-CA32: Maioria das views admin usa CSS local (admin-btn-*, secao-card) em vez de AppCard/AppButton do DS.
- Ação Wave 3: refatorar AdminLayout para usar componentes do DS.

Bug aberto da Wave 1 (AdminResetService.cs tabelas orcamento) permanece em aberto — não foi escopo da Wave 2.
