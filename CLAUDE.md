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
- **Confidencialidade clínica entre profissionais (sigilo médico + LGPD) — premissa primordial**: o conteúdo de uma evolução de prontuário e **tudo vinculado a ela** (anexos, fotos, receitas, atestados, pedidos de exame, termos) só pode ser **lido/baixado** por **(a)** o profissional **autor** daquela evolução (`ProntuarioEvolucao.AutorUsuarioId`) e **(b)** o **Dono** do estabelecimento. Médicos do mesmo estabelecimento são autônomos e **não compartilham prontuário entre si** — nenhum outro profissional, recepcionista ou papel vê evolução/documento de colega. **Falha-fechada**: toda query/handler de leitura de prontuário filtra `(autor_usuario_id = @solicitante OR @papel = Dono)`; sem o claim de usuário/papel → retorna vazio/nega. Acesso negado usa mensagem **genérica** ("não encontrado"), sem vazar existência. Fonte da verdade no **backend** (toda query de leitura); front e **mobile** apenas espelham como UX. Vale para web e mobile. **Exceção — alertas clínicos de segurança** (ex.: alergia grave) seguem a regra própria já existente (Dono + qualquer profissional com vínculo de atendimento ao paciente), pois a segurança do paciente prevalece sobre o sigilo entre médicos. Detalhes em [Docs/LGPD.md](Docs/LGPD.md).
- **Terceiros atrás de provider (ports & adapters)**: toda integração com serviço externo, API de terceiro ou ferramenta (pagamento, e-mail, storage, IA, SMS, assinatura digital, etc.) é consumida pelo domínio/handler **apenas por uma interface (porta) definida na Application/Domain**; a implementação concreta (adapter/provider) vive **isolada na camada de Infraestrutura**. Regra de negócio nunca conhece o SDK, a URL ou o formato do fornecedor — trocar a ferramenta = trocar só o provider, sem tocar Domain, Handler ou contrato. DI registra a implementação ativa. Espelha os providers já existentes (`Resend`/`SES` para e-mail, `S3` para storage, `LocalJwt` para auth). Antes de chamar um SDK externo dentro de um handler → pare: extraia uma porta e mova a chamada pro provider.
- **Reuso > duplicação**: antes de criar endpoint, query, DTO, service, store, componente, helper → `grep`/`Glob` por equivalente existente. Estender > duplicar. Detalhes em [Docs/DESIGN.md §Reuso](Docs/DESIGN.md#reuso--duplicação).
- **Design system primeiro**: antes de escrever HTML/CSS scoped, confira `frontend/src/components/ui/`. Componente reutilizável vai pro design system primeiro.
- **Performance e foco**: buscar só o necessário do momento — aba não clicada não dispara consulta. Páginas centralizadas via `.app-page`. Debounce em busca via `useDebouncedRef`.
- **Branch por feature + aprovação humana antes da `main`**: nenhum agente faz push direto para `origin/main`. Todo trabalho vai para uma branch de feature (`feature/<slug-do-briefing>`, `fix/<slug>` para bugfix); ao fim de **todos os testes locais**, o agente **sempre pergunta** ao usuário se pode mergear na `main`. Só com o "sim" explícito é que faz merge + push (que dispara o deploy). Após o merge, **deleta a branch de feature** (local e remota, se houver) para não poluir o repositório com branches mortas. Sem confirmação, a branch fica disponível para o usuário testar por conta própria. Premissa para todos os agentes.
- **1 push por sessão**: agrupa commits locais na branch, push uma vez no fim (após a aprovação do merge). Pipeline de deploy custa 3-5 min.

### Agents Disponíveis

Pipeline de 6 agentes especializados, com briefing imutável em [`planejamentos/`](planejamentos/) como fonte de verdade. Documento mestre da pipeline em [`.claude/agents/PIPELINE.md`](.claude/agents/PIPELINE.md).

| Agent | Modelo | Responsabilidade |
|-------|--------|------------------|
| [`imedto-business-analyst`](.claude/agents/imedto-business-analyst.md) | Opus | Refinar demanda crua, perguntas direcionadas, escrever briefing imutável com CAs testáveis em `planejamentos/`. Cria addendums em spec gap (Tipo B). **Atualiza `Docs/` quando a demanda altera arquitetura/infra/design/LGPD.** |
| [`imedto-developer`](.claude/agents/imedto-developer.md) | Sonnet | Implementar feature/bugfix fielmente ao briefing — **web (`frontend/`) + backend (`backend/`)**. Vue 3 + TS + design system / .NET 10 CQRS + DDD. Recusa sem briefing. Aciona DB agent se schema muda. |
| [`imedto-database`](.claude/agents/imedto-database.md) | Sonnet | Único autor de migrations. Fluxo EF Core + SQL idempotente em `db/migrations/`. Inspeciona RDS via MCP AWS RDS (ou psql via túnel SSH). Multi-tenant + índices + performance dia 1. |
| [`imedto-qa`](.claude/agents/imedto-qa.md) | Sonnet | Quality gate **de web/backend**. Valida cada CA via chrome-devtools MCP + suíte automatizada. Autorizado a `git commit`/`git push`. Classifica bug Tipo A (volta dev) vs Tipo B (escala BA). Nunca corrige sozinho. |
| [`imedto-mobile-developer`](.claude/agents/imedto-mobile-developer.md) | Sonnet | **Só `mobile/`** (app do médico — Capacitor 6 + Vue 3 + plugins nativos). Implementa telas/serviços/stores consumindo **apenas a API** do backend, reusa o design system mobile, capabilities nativas (câmera/biometria/push/voz/share). Recusa sem briefing. Não toca web/backend. |
| [`imedto-mobile-qa`](.claude/agents/imedto-mobile-qa.md) | Sonnet | Quality gate **do `mobile/`**. Sobe backend local e **aponta o app mobile pra ele**, valida cada CA via chrome-devtools em 375px nos temas claro/escuro, confere multi-tenant/LGPD/RBAC/estados/nativo. Autorizado a `git commit`/`git push`. Classifica A/B. Nunca corrige sozinho. |

**Quando usar a dupla mobile**: a demanda toca a pasta `mobile/` (app do médico). Aí o orquestrador aciona `imedto-mobile-developer` (implementa) → `imedto-mobile-qa` (valida). Para `frontend/` (web) ou `backend/`, use a dupla padrão (`imedto-developer` → `imedto-qa`). O BA (`imedto-business-analyst`) e o DB (`imedto-database`) são compartilhados pelas duas trilhas. Se a feature mobile exigir endpoint/contrato novo no backend, o mobile-developer **para e reporta** — o backend é do `imedto-developer`/`imedto-database`.

### Pipeline

```
USUÁRIO → imedto-business-analyst → planejamentos/YYYY-MM-DD_NNN_titulo.md (imutável)
            ▲                        │
   spec gap │                        ▼
   (Tipo B) │              imedto-developer ──schema?──► imedto-database
            │                        │                          │
            │                        ▼                          ▼
            └─ Tipo A/B ─ imedto-qa ─ commit na branch → PERGUNTA ao usuário → (com OK) merge main + push → CI/CD → deploy
```

1. **`imedto-business-analyst`** entende a demanda, faz perguntas via `AskUserQuestion` até destravar ambiguidade (permissionamento, multi-tenant, conflito de regra, LGPD), valida com o usuário e produz briefing imutável em `planejamentos/`. Quando a demanda altera arquitetura/infra/design, atualiza `Docs/` no mesmo briefing.
2. **`imedto-developer`** executa fielmente os CAs do briefing. Frontend + backend + testes. Aciona `imedto-database` se schema mudou.
3. **`imedto-database`** (quando chamado) modela schema, gera migration EF + SQL idempotente em `db/migrations/`, valida via MCP/psql, devolve ao dev.
4. **`imedto-qa`** valida cada CA com evidência, classifica bugs em Tipo A (volta ao dev) vs Tipo B (escala ao BA para addendum imutável). Se OK, commita na branch de feature com referência ao briefing e **pergunta ao usuário** se pode mergear na `main`; só com o "sim" explícito faz merge + push. Loop fecha aqui.

### Regras dos Pipelines

- **OBRIGATÓRIO: Validar ANTES de commitar** — apenas o `imedto-qa` commita. Dev e DB nunca empurram. Quality gate único.
- **OBRIGATÓRIO: Validação local ANTES do push — nunca validar em produção depois do deploy** — o QA sobe o ambiente com `./dev.sh` (túnel SSH + backend :5050 + front :3000) e valida **cada CA com o app rodando localmente** antes de qualquer push. Vale para UI (chrome-devtools MCP, login em `.claude/qa-credentials.local.json`, gitignored) **e para fluxos sem tela — jobs em background, migração, handlers assíncronos**: dispara o fluxo local e confere o efeito real no banco (registros gravados, status, logs) antes de subir. Suíte verde NÃO basta: bug de SQL/Dapper, coluna/tabela inexistente, lifetime de DI e CSS de runtime só aparecem com o app rodando. **O deploy é consequência de já ter validado local — não é ambiente de teste; validar em prod pós-deploy é proibido como etapa de validação.** Para jobs do `JobScheduler` (advisory lock compartilhado com o backend de prod no mesmo banco da EC2), isole o smoke local: pare o backend de prod durante o teste (fase de testes permite) ou use job/tenant de teste, confirmando nos logs locais que foi o backend local que processou. Nada sobe sem esse smoke local.
- **OBRIGATÓRIO: Branch por feature + aprovação humana antes da `main`** — nenhum agente faz push para `origin/main` sem o usuário mandar. O trabalho vai para uma branch de feature (`feature/<slug-do-briefing>`, `fix/<slug>` para bugfix). Depois de validar **todos os testes locais**, o QA commita na branch e **sempre pergunta**: "Validado localmente — posso mergear na `main` e fazer push (dispara o deploy)?". Só com confirmação explícita é que faz `git checkout main` + merge + push. Após o merge, deleta a branch de feature (local + remota) para não acumular branches mortas. Sem confirmação, a feature fica na branch para o usuário testar por conta própria.
- **Briefing antes de código** — demanda crua sempre passa pelo BA. Trivialidades (ajuste de padding, fix de typo isolado) ficam fora da pipeline e podem ir direto pelo orquestrador.
- **Briefing é imutável** — mudou? Cria addendum (`*-addendum.md`). Nunca edita o original.
- **CA é Dado/Quando/Então** — sem CA testável, briefing é inválido. Multi-tenant + RBAC + LGPD + estados + performance são CAs obrigatórios.
- **QA classifica antes de devolver** — Tipo A vai pro dev; Tipo B escala pro BA. Sem classificação, a pipeline trava em loop de patch de sintoma.
- **QA nunca corrige bug sozinho** — mesmo typo. Devolve com diagnóstico estruturado. A pipeline aprende devolvendo.
- **1 push por sessão** — agrupa commits localmente na branch, push uma vez no fim (após o usuário aprovar o merge). CI/CD pesado (~3-5 min por deploy).
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
