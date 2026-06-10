# Briefs de prototipação — Módulo Financeiro/Cobranças

Pacote de briefs para o **Claude Design**, derivado do plano mestre
[`MODULO_FINANCEIRO_COBRANCAS.md`](../MODULO_FINANCEIRO_COBRANCAS.md).

**Como usar:** cole **um brief por vez** (um chat/artefato por brief), na ordem abaixo.
Anexe screenshots das telas atuais correspondentes (agenda, página do paciente,
prontuário, orçamentos) para ancorar o visual. O design system já configurado no
projeto do Claude Design cobre tokens/componentes — os briefs trazem só o que ele
não sabe: fluxo, estados e dados de exemplo.

| # | Brief | Fase do plano |
|---|---|---|
| 01 | Check-in com Particular/Convênio + valor da consulta | F1 |
| 02 | Pagamento pelo agendamento (ícone + modal) | F1 |
| 03 | Aba Financeiro do paciente (+ estorno e recibo) | F2 / F8 |
| 04 | Prontuário: procedimentos indicados + conduta checklist + pendências | F3 / F3B |
| 05 | Financeiro da clínica (redesign) + caixa diário + configurações | F7 / F1 |
| 06 | Convênio — estrutura base (cadastro, carteirinha, guia) | F6 |

> Dica: peça sempre os **estados** (vazio, parcial, erro, "em breve") — protótipo de
> financeiro vive ou morre nos estados, não na tela feliz.

## Resultado (2026-06-10)

Os 6 briefs foram implementados no Claude Design e o handoff está em
[`design-handoff/`](design-handoff/) — páginas navegáveis, componentes, dados de
exemplo e screenshots, mapeados por fase no README de lá. É a **referência visual**
para os briefings de desenvolvimento de cada fase.
