---
name: session-wave7-final
description: Wave 7 admin global pipeline fechada — commit 6ea752d pushed. Minimização do audit log (corte ruído + job retenção). Armadilha do backend legado em memória (processo Wave 6 na porta 5050 ainda rodando ao iniciar sessão).
metadata:
  type: project
---

Wave 7 admin global (briefing 2026-05-30_007) validada e commitada em 6ea752d.

15 CAs validados, todos passando.

Frentes entregues:
- Frente 1: 3 chamadas de audit removidas (LoginOk, Logout, AbrirDetalheTenant)
- Frente 2: AuditLogRetencao (mapa estático 31 ações) + LimparAuditAdminJob (batches 10k, 1x/dia)
- Frente 3: Migration retroativa removeu 158 linhas de ruído (78% do volume)
- Frente 4: Docs/ARQUITETURA.md + Docs/INFRA.md atualizados

**Why:** Feedback do usuário sobre crescimento descontrolado da tabela audit_log.

**How to apply:** Pipeline seguiu fluxo normal. Todos os testes verdes. Build com 0 warnings.

Armadilha importante detectada nesta sessão:
- Ao iniciar QA, o processo do backend Wave 6 (PID 87199) ainda estava rodando na porta 5050
- O `dotnet run` Wave 7 falhou silenciosamente com "address already in use"
- O primeiro teste de CA1 (LOGIN_OK) FALHOU porque o curl estava batendo no backend Wave 6 (que ainda auditava LoginOk)
- Diagnóstico: verificar PID do processo + timestamp do binário em /bin/Debug/net10.0/
- Fix: `kill <PID>` do processo antigo, depois subir com `ASPNETCORE_URLS=http://localhost:5050 dotnet run`
- Lição: sempre matar processos de sessão anterior antes de iniciar validação QA

Estado do banco dev após Wave 7:
- LOGIN_OK: 0, LOGOUT: 0, ABRIR_DETALHE_TENANT: 0 (zerados pela migration)
- LOGIN_FAIL: 20, mutações diversas: 25 (preservados)
- jobs_agendados: limpar-audit-admin presente, intervalo_seg=86400

ESLint frontend continua com erro pré-existente (@typescript-eslint/recommended) — não é regressão Wave 7.
