---
name: project_qa_browser_indisponivel
description: chrome-devtools MCP indisponível no sandbox do QA — validação visual fica para usuário em prod
metadata:
  type: project
---

O sandbox onde o imedto-qa roda não tem acesso ao chrome-devtools MCP nem ao banco de dados (PostgreSQL está em container na EC2 sem túnel ativo).

Consequência: CAs que exigem interação real no navegador (click, screenshot, network tab) são validados por análise de código + suíte automatizada (vue-tsc + Vitest).

O que fica para smoke-test manual do usuário em produção:
- Validação visual de AppConfirmDialog abrindo no click
- Confirmação de toast aparecendo após ação
- Comportamento em mobile (375px)
- Double-click bloqueado pelo estado `executando`

**Why:** banco movido para EC2 em 2026-05-30 (corte de custo); túnel SSH não configurado no sandbox do agente. Sem browser local acessível via MCP.
**How to apply:** nunca bloquear commit só por ausência de evidência visual. Declarar explicitamente no sumário de validação o que fica para smoke-test manual, sem silenciar.
