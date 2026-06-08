# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

## 5. Tipografia — premissa não-negociável (briefing 2026-06-08_003)

**Nunca declare `font-size` ou `font-weight` como valor literal em CSS scoped de view ou componente.**

Toda declaração tipográfica usa os tokens CSS definidos em `frontend/src/assets/main.css` (`:root`):
- `font-size: var(--text-sm)` — não `font-size: 13px` ou `font-size: 0.8125rem`
- `font-weight: var(--font-weight-bold)` — não `font-weight: 700`

**Regras de uso por nível:**
- Título de página → `<AppPageHeader>` (30px/800 via token `--text-3xl`)
- Título de seção/painel → `<h2 class="ds-section-title">` (21px/800)
- Título de card inline → `<h3 class="ds-card-title">` (15px/700)
- Label de campo → `<AppField>` ou `<AppLabel>` (12px/600 via DS)
- Input/botão → herda de `.form-input`/`.btn-*` em `main.css` (13px)

**Escala completa e contexto:** `Docs/DESIGN.md §Escala tipográfica` e `Docs/Discoverys/tipografia/01_discovery.md`.

## Overview

Monorepo do Imedto (refactor do legado Vue+Supabase para arquitetura CQRS):
- `backend/` — API .NET 10, DDD + CQRS, BFF de autenticação, EF Core (escrita) + Dapper (leitura)
- `frontend/` — Vue 3 + TypeScript + Vite + Pinia
- `db/migrations/` — migrations SQL aplicadas em RDS pela pipeline de deploy

Stack de runtime: **AWS RDS Postgres** (banco), **LocalJwt + ECDSA P-256** (auth — implementado no backend, sem provedor externo), **AWS S3** (storage de fotos e anexos), **Resend** (e-mail transacional, com SES como provider alternativo). **Toda regra de negócio vive no backend** — nada de RPCs, triggers ou edge functions que implementem lógica.

Detalhes de conexão e credenciais ficam em `appsettings.Development.json`/`.mcp.json` (ambos gitignored). Em produção, vêm do AWS SSM Parameter Store.

## Documentação modular (ler sob demanda)

Para reduzir custo de contexto, a documentação detalhada vive em [`Docs/`](Docs/) e é carregada **apenas quando necessária**. Use esta tabela para decidir o que ler antes de cada tarefa:

| Vou tocar... | Ler antes |
|---|---|
| Código backend (.NET, CQRS, handler, EF, Dapper) | [Docs/ARQUITETURA.md](Docs/ARQUITETURA.md) |
| Código frontend (Vue, store, service, view) | [Docs/ARQUITETURA.md](Docs/ARQUITETURA.md) + [Docs/DESIGN.md](Docs/DESIGN.md) |
| UI, componente, layout, design system | [Docs/DESIGN.md](Docs/DESIGN.md) |
| Autenticação (JWT, BFF, cookies, refresh) | [Docs/ARQUITETURA.md §Autenticação](Docs/ARQUITETURA.md#autenticação-bff--localjwt) |
| Migration, schema, índice, function SQL | [Docs/COMANDOS.md §Migrations](Docs/COMANDOS.md#migrations-ef-core-autora-pipeline-aplica-em-rds) + [Docs/ARQUITETURA.md](Docs/ARQUITETURA.md) |
| Deploy, EC2, RDS, S3, SSM, e-mail, DNS, CI/CD, secrets | [Docs/INFRA.md](Docs/INFRA.md) |
| Build, testes, lint, rodar dev local | [Docs/COMANDOS.md](Docs/COMANDOS.md) |
| Paciente, prontuário, PII, audit, mensagem de erro | [Docs/LGPD.md](Docs/LGPD.md) |
| Investigar viabilidade de feature/integração nova | [Docs/Discoverys/](Docs/Discoverys/) |

Índice completo em [Docs/README.md](Docs/README.md).

### Documentação viva — premissa não-negociável

`Docs/` é a **fonte de verdade** do projeto. Toda mudança em **estrutura, arquitetura, infra, design system ou regra cross-cutting** deve atualizar o documento correspondente **na mesma entrega** — documentação parada vira documentação errada, e prompt errado fica caro em produção.

- **`imedto-business-analyst`** é o responsável primário pela documentação viva. Ao receber demanda que altera arquitetura/infra/design/LGPD, ele atualiza o doc correspondente como parte do briefing — não como passo opcional.
- **`imedto-developer`** atualiza quando introduz componente novo no design system, padrão novo de service/store, ou comando recorrente novo.
- **`imedto-database`** atualiza `INFRA.md`/`COMANDOS.md` quando muda extensions, padrão de migration, índice estratégico.
- **`imedto-qa`** valida nos CAs que o doc foi atualizado quando a feature exige.

Discoveries (investigações de viabilidade antes de cravar arquitetura) vão em [`Docs/Discoverys/`](Docs/Discoverys/).

## Premissas não-negociáveis (toda feature respeita)

- **Regra de negócio sempre no backend** (Domain/Handler). Nunca no controller, no SQL puro ou no front. Trava do front sempre tem espelho no back — 422 do `BusinessException` é a fonte da verdade; front é UX.
- **Multi-tenant em camadas**: toda query/comando de domínio filtra `estabelecimento_id`. Mensagem de erro genérica ("não encontrado") em vez de revelar tenant alheio. Repositório falha-fechada: sem tenant claim → retorna vazio/throws. Detalhes em [Docs/LGPD.md §Multi-tenant](Docs/LGPD.md#checklist-multi-tenant--premissa-não-negociável).
- **LGPD é premissa de design, não checklist**: minimização (DTO só com campos da tela), audit trail em paciente/prontuário, sem PII em log/mensagem de erro, mensagens genéricas, espelho back+front. Detalhes em [Docs/LGPD.md](Docs/LGPD.md).
- **Reuso > duplicação**: antes de criar endpoint, query, DTO, service, store, componente, helper → `grep`/`Glob` por equivalente existente. Estender > duplicar. Detalhes em [Docs/DESIGN.md §Reuso](Docs/DESIGN.md#reuso--duplicação).
- **Design system primeiro**: antes de escrever HTML/CSS scoped, confira `frontend/src/components/ui/`. Componente reutilizável vai pro design system primeiro.
- **Performance e foco**: buscar só o necessário do momento — aba não clicada não dispara consulta. Páginas centralizadas via `.app-page`. Debounce em busca via `useDebouncedRef`.
- **1 push por sessão**: agrupa commits locais, push uma vez no fim. Pipeline de deploy custa 3-5 min.

### Agents Disponíveis

Pipeline de 4 agentes especializados, com briefing imutável em [`planejamentos/`](planejamentos/) como fonte de verdade. Documento mestre da pipeline em [`.claude/agents/PIPELINE.md`](.claude/agents/PIPELINE.md).

| Agent | Modelo | Responsabilidade |
|-------|--------|------------------|
| [`imedto-business-analyst`](.claude/agents/imedto-business-analyst.md) | Opus | Refinar demanda crua, perguntas direcionadas, escrever briefing imutável com CAs testáveis em `planejamentos/`. Cria addendums em spec gap (Tipo B). **Atualiza `Docs/` quando a demanda altera arquitetura/infra/design/LGPD.** |
| [`imedto-developer`](.claude/agents/imedto-developer.md) | Sonnet | Implementar feature/bugfix fielmente ao briefing — frontend (Vue 3 + TS + design system) + backend (.NET 10 CQRS + DDD). Recusa sem briefing. Aciona DB agent se schema muda. |
| [`imedto-database`](.claude/agents/imedto-database.md) | Sonnet | Único autor de migrations. Fluxo EF Core + SQL idempotente em `db/migrations/`. Inspeciona RDS via MCP AWS RDS (ou psql via túnel SSH). Multi-tenant + índices + performance dia 1. |
| [`imedto-qa`](.claude/agents/imedto-qa.md) | Sonnet | Quality gate único. Valida cada CA via chrome-devtools MCP + suíte automatizada. **Único autorizado a `git commit`/`git push`.** Classifica bug Tipo A (volta dev) vs Tipo B (escala BA). Nunca corrige sozinho. |

### Pipeline

```
USUÁRIO → imedto-business-analyst → planejamentos/YYYY-MM-DD_NNN_titulo.md (imutável)
            ▲                        │
   spec gap │                        ▼
   (Tipo B) │              imedto-developer ──schema?──► imedto-database
            │                        │                          │
            │                        ▼                          ▼
            └────── Tipo A/B ── imedto-qa ── commit + push → CI/CD → deploy
```

1. **`imedto-business-analyst`** entende a demanda, faz perguntas via `AskUserQuestion` até destravar ambiguidade (permissionamento, multi-tenant, conflito de regra, LGPD), valida com o usuário e produz briefing imutável em `planejamentos/`. Quando a demanda altera arquitetura/infra/design, atualiza `Docs/` no mesmo briefing.
2. **`imedto-developer`** executa fielmente os CAs do briefing. Frontend + backend + testes. Aciona `imedto-database` se schema mudou.
3. **`imedto-database`** (quando chamado) modela schema, gera migration EF + SQL idempotente em `db/migrations/`, valida via MCP/psql, devolve ao dev.
4. **`imedto-qa`** valida cada CA com evidência, classifica bugs em Tipo A (volta ao dev) vs Tipo B (escala ao BA para addendum imutável). Se OK, commita com referência ao briefing e empurra. Loop fecha aqui.

### Regras dos Pipelines

- **OBRIGATÓRIO: Validar ANTES de commitar** — apenas o `imedto-qa` commita. Dev e DB nunca empurram. Quality gate único.
- **Briefing antes de código** — demanda crua sempre passa pelo BA. Trivialidades (ajuste de padding, fix de typo isolado) ficam fora da pipeline e podem ir direto pelo orquestrador.
- **Briefing é imutável** — mudou? Cria addendum (`*-addendum.md`). Nunca edita o original.
- **CA é Dado/Quando/Então** — sem CA testável, briefing é inválido. Multi-tenant + RBAC + LGPD + estados + performance são CAs obrigatórios.
- **QA classifica antes de devolver** — Tipo A vai pro dev; Tipo B escala pro BA. Sem classificação, a pipeline trava em loop de patch de sintoma.
- **QA nunca corrige bug sozinho** — mesmo typo. Devolve com diagnóstico estruturado. A pipeline aprende devolvendo.
- **1 push por sessão** — agrupa commits localmente, push uma vez no fim. CI/CD pesado (~3-5 min por deploy).
- **Documentação viva** — se a entrega muda arquitetura/infra/design/regra cross-cutting, o doc em `Docs/` é atualizado no mesmo PR. O QA valida.
- **Sempre anunciar** qual agent está sendo chamado antes de invocá-lo.
- **Parar e reportar** se um agent encontrar problema crítico.
- **Relatório final** ao terminar o pipeline: resumo de cada agent e status (passou / falhou / avisos).

**Como acionar:**
- Demanda nova: `"Quero implementar X"` → orquestrador aciona o BA automaticamente.
- Briefing já aprovado: `"Execute o pipeline para o briefing planejamentos/YYYY-MM-DD_NNN_*.md"` → pula BA, vai direto ao dev.
- Validação manual: `"Aciona o imedto-qa — terminei a implementação"` → QA roda ciclo completo.
- Spec gap: `"QA achou que falta decisão de produto no caso X"` → aciona BA em Modo B (addendum).

**Quando NÃO usar a pipeline**: spike de viabilidade, refator interno puro sem mudança observável, hotfix urgente trivial (1 linha óbvia), mudança só em `Docs/`. Detalhes em [`.claude/agents/PIPELINE.md`](.claude/agents/PIPELINE.md).
