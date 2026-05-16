---
name: qa-posdeploy-f42ef48-2026-05-16
description: Pós-deploy f42ef48 (bundle index-91970981) — 3 bugs A/B/C resolvidos no backend e router guard; gap residual nos cards do dashboard Home (mostram Financeiro/Inventário p/ Médico).
metadata:
  type: project
---

Rodada de QA pós-deploy `f42ef48` (bundle `/assets/index-91970981.js`) em 2026-05-16 03:30-04:40 UTC, https://app.imedto.com.

Resultados:
- Bug A (sidebar por papel): RESOLVIDO. Dono vê 10 itens completos; Médico vê apenas 6 (Painel/Agendamentos/Minhas consultas/Pacientes/Orçamentos/Relatórios). `Configurações` aparece pra Médico mas aponta direto p/ `/configuracoes/modelos-prontuario` (escopo permitido).
- Bug B (orçamentos sem pré-carga PII): RESOLVIDO. Boot do `/orcamentos` dispara só `/api/orcamentos`. `/api/paciente/busca-rapida?limite=30` só ao abrir modal "Novo orçamento". Response `[{id, nomeCompleto}]` sem PII. Debounce funcional (1 request p/ `q=ab`).
- Bug C (router guard): RESOLVIDO. URLs `/equipe`, `/financeiro`, `/inventario`, `/automacoes`, `/configuracoes/ia` redirecionam silenciosamente p/ `/home` quando Médico. `/orcamentos` abre normal p/ Médico (tem permissão).

Gap novo (não estava no plano):
- Cards do dashboard `/home` NÃO respeitam papel — Médico vê "Financeiro" e "Inventário" como cards clicáveis. Ao clicar, redireciona para `/home` (guard ok), mas sem feedback ao usuário (parece "página piscou"). Inconsistente com a sidebar que já filtra.

Observação LGPD menor:
- `/api/auth/bootstrap` retorna `estabelecimentos[].donoUsuarioId` para Profissional não-dono (`00000000-...` quando o usuário não é o dono — sugere mascaramento, mas o campo continua exposto). Considerar remover do DTO p/ não-donos.

Smoke regressão: todos OK
- `/api/estabelecimento/1/profissionais` 422 p/ Médico.
- LGPD acentuação correta.
- Anti-enumeração (`/api/auth/forgot-password` retorna 204 p/ existente e inexistente).
- Agenda autocomplete `busca-rapida?limite=8` sem PII.
- Card "Trocar senha" em Minha Conta presente.

Veredito: VERDE com pequena ressalva (cards do dashboard home).
