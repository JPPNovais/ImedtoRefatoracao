---
name: session-wave3-final
description: Wave 3 admin global — pipeline fechada, commit cef073f pushed. Débito técnico W2-CA29/CA32 fechado.
metadata:
  type: project
---

Wave 3 do admin global (briefing 2026-05-30_003) fechada em commit cef073f, push origin/main em 2026-05-30.

24 arquivos alterados (23 views/componentes + Docs/DESIGN.md + briefing).

**Why:** Fecha débito técnico W2-CA29/CA32: AdminLayout agora usa AppTopBar + AppSidebar; todas as 17 views usam componentes reais do DS (AppCard, AppButton, AppField, AppModal, etc.) em vez de CSS custom.

**How to apply:** Gates verde: dotnet build (0 erros), Vitest 42 arquivos/359 testes, frontend build/typecheck verde. ESLint quebrado é pré-existente de ambiente local (não introduzido por este diff). 34 CAs validados via chrome-devtools MCP.

CAs aprovados: W3-CA1 a W3-CA34 (todos).

Observações não-bloqueantes registradas:
- W3-CA20: CSS scoped total 1235 linhas (meta era 400) — excesso são classes de tabela estrutural permitidas pelo §6 Decisão 7, não primitivas proibidas.
- Classes `.admin-layout` e `.admin-main` em AdminLayout.vue — estruturais do shell, aceitáveis.
- Warnings Reka-UI DialogTitle/aria-describedby — pré-existentes no AppModal global.

Bug Tipo A da Wave 1 (AdminResetService.cs tabelas orcamento) permanece em aberto — não era escopo desta Wave.

Senha dev admin: email `admin@imedto.com`, senha `123123` (bcrypt com pepper do appsettings.Development.json).
Token de acesso: 15 min. Usar SPA navigation (router.push via evaluate_script) para preservar cookies HttpOnly entre navegações no Chrome headless.
