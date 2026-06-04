---
name: feedback_browser_indisponivel
description: Banco EC2 inacessível no sandbox de QA — fallback para análise de código + testes
metadata:
  type: feedback
---

O banco Postgres roda em container na EC2 (migração 2026-05-30). No ambiente de sandbox do QA Agent,
o banco não é acessível, impedindo subir o backend e usar chrome-devtools para validação interativa.

**Why:** Banco movido para EC2 por corte de custo; sandbox não tem acesso de rede à EC2.

**How to apply:** Quando `dotnet run` falhar por conexão ao banco, registrar "Limitação de browser:
banco EC2 inacessível no sandbox" e executar validação completa por:
1. Análise de código (handlers, domain, controllers, DTOs).
2. Suíte de testes (dotnet test + vitest) — toda a lógica de negócio deve ter cobertura de teste.
3. Concentrar análise extra nos pontos de maior risco declarados no briefing (LGPD, multi-tenant, 410 genérico).
