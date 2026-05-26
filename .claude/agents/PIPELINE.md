# Pipeline de agentes Imedto

Pipeline de 4 agentes especializados com **separação clara de papéis**, **briefing imutável como fonte de verdade** e **quality gate único**. Existe porque o produto é complexo (saúde, multi-tenant, LGPD, multi-estabelecimento), o stack é amplo (Vue 3 + .NET 10 CQRS + Postgres RDS + AWS), e a regressão é cara em sistemas de clínica — recepcionista clica 50× por dia; um clique a mais é receita perdida.

## Diagrama de fluxo

```
                                                     ┌────────────────────────┐
                              ┌─────────────────────►│   IMEDTO-BUSINESS-     │
                              │     spec gap          │      ANALYST           │
                              │     (Tipo B)          │   (Opus — refina)      │
                              │                       └────────────┬───────────┘
                              │                                    │
                              │                                    │ briefing
                              │                                    │ imutável
                              │                                    ▼
   ┌─────────────────┐        │              ┌───────────────────────────────────────┐
   │     USUÁRIO     │────────┼─────────────►│       planejamentos/                  │
   │  (você, eu)     │        │              │   YYYY-MM-DD_NNN_titulo.md            │
   └─────────────────┘        │              │   (+ addendums quando aplicável)      │
                              │              └────────────────┬──────────────────────┘
                              │                               │
                              │                               ▼
                              │               ┌────────────────────────────┐
                              │               │      IMEDTO-DEVELOPER      │
                              │               │   (Sonnet — implementa)    │
                              │               └────────┬─────────┬─────────┘
                              │      schema novo?      │         │
                              │                        ▼         │
                              │            ┌──────────────────┐  │
                              │            │ IMEDTO-DATABASE  │  │
                              │            │ (Sonnet — migra) │  │
                              │            └────────┬─────────┘  │
                              │                     │            │
                              │                     ▼            ▼
                              │               ┌──────────────────────┐
                              │  Tipo A ──────┤      IMEDTO-QA       │
                              └───── Tipo B ──┤  (Sonnet — valida)   │
                                              │  ÚNICO autor de      │
                                              │  commit + push       │
                                              └──────────┬───────────┘
                                                         │
                                                         ▼
                                                    git push origin main
                                                  (CI/CD → deploy automático)
```

**Legenda dos retornos**:
- **Tipo A** (falha de implementação) → volta ao `imedto-developer` com diagnóstico estruturado.
- **Tipo B** (lacuna de spec) → escala ao `imedto-business-analyst`, que cria addendum imutável e devolve ao dev.

## Tabela dos agentes

| Agente | Modelo | Por que esse modelo | Responsabilidade |
|---|---|---|---|
| `imedto-business-analyst` | **Opus** | Refinamento ambíguo, regra de negócio complexa, julgamento sobre o que é/não é spec gap. Opus brilha em "pensar antes" — exatamente onde o BA atua. | Refinar demanda, fazer perguntas direcionadas, escrever briefing imutável com CAs testáveis, criar addendums em spec gap. |
| `imedto-developer` | **Sonnet** | Execução fiel contra briefing claro. Sonnet é rápido e bom o suficiente para implementar Vue + .NET + testes sem ambiguidade. | Implementar feature/bugfix fielmente ao briefing. Frontend + backend + testes. Recusa sem briefing. |
| `imedto-database` | **Sonnet** | Schema, índice, migration — território com padrão claro. Convenção do projeto já está em CLAUDE.md. | Único autor de migrations. EF Core + SQL idempotente em `db/migrations/`. Valida via MCP AWS RDS. |
| `imedto-qa` | **Sonnet** | Validação contra checklist explícito (CAs + multi-tenant + LGPD + estados). Decisão Tipo A/B segue regra clara. | Quality gate. Único autorizado a `git commit`/`git push`. Classifica bug A/B antes de devolver. |

## Quando escapar do default

O modelo de cada agente é o caso geral. Em situações específicas, **o orquestrador humano (você) resolve direto no main (Opus 4.7 1M context)** em vez de delegar ao Sonnet:

- **Refatorações grandes em áreas regressivas** — permissionamento, orçamento, prontuário, relatório, estoque. Sonnet pode perder contexto cross-cutting; Opus 1M segura o quadro inteiro.
- **Migrações de schema com impacto em múltiplas features simultâneas** — quando a mudança cruza Domain + Handler + Query + Front + audit + RLS em uma rodada só.
- **Decisões de arquitetura cross-domain** — bounded contexts novos, refatoração de bus, mudança de padrão de DI, política nova de cache, escolha de tecnologia.
- **Bug em região com história longa** — quando o `git log` da área tem 30+ commits e a causa pode estar em qualquer um. O 1M context do Opus segura.
- **Quando o BA já criou 2+ addendums no mesmo briefing** — sinal de que produto e implementação estão dessincronizados; vale uma sessão de Opus pra repensar o briefing inteiro.

Não há escalonamento automático de Sonnet → Opus. É decisão consciente sua, no orquestrador.

## Pasta `planejamentos/`

Documento mestre da convenção em [`planejamentos/README.md`](../../planejamentos/README.md). Resumo:

- **Nome**: `YYYY-MM-DD_NNN_titulo-em-kebab-case.md`.
- **Imutabilidade**: briefing original nunca é editado. Mudanças entram como `*-addendum.md`.
- **Estrutura obrigatória**: 9 seções (contexto, persona, escopo, regras, dados, UX, CAs testáveis, riscos, observações).
- **CAs são Dado/Quando/Então** — nada de "deve funcionar".
- **Multi-tenant + RBAC + LGPD + estados + performance** são CAs obrigatórios, não opcionais.

## MCPs

| MCP | Uso | Estado |
|---|---|---|
| `chrome-devtools` | `imedto-qa` valida CAs via navegador: snapshot, evaluate_script, list_network_requests, list_console_messages, take_screenshot. | ✅ Disponível. |
| **AWS RDS MCP** | `imedto-database` inspeciona estado real do banco em dev/stage: `\d <tabela>`, `EXPLAIN ANALYZE`, validação de índice/tipo, volumetria. | ⏳ A instalar. Enquanto isso, dev/db usa túnel SSH + psql (comando no CLAUDE.md). |

Configuração de MCPs locais fica em `.mcp.json` (gitignored — templates em `.mcp.json.example`).

## Pré-requisitos antes da primeira execução

1. **Ler CLAUDE.md inteiro.** Toda a pipeline pressupõe os princípios e convenções dele.
2. **Conferir `Docs/`** (fonte de verdade para arquitetura e migração): `00_PLANO_MIGRACAO.md`, `01_FASE_1_HARDENING.md`, `03_FASE_3_DOMINIO_CLINICO.md`, `DESIGN_SYSTEM.md`, `PLANO_MIGRACAO_RDS.md`.
3. **Garantir `appsettings.Development.json`** em `backend/src/Services/Imedto.Backend.API/` (connection strings + JWT PEM + buckets S3 + Resend key).
4. **Garantir túnel SSH ao RDS** (ou MCP AWS RDS) funcional para o `imedto-database`.
5. **Garantir Chrome + chrome-devtools MCP** funcional para o `imedto-qa`.
6. **Garantir `git` configurado** com credenciais para push (o QA empurra direto pra `main` em features pequenas, ou cria PR em mudanças maiores — o critério é o tamanho da feature e a estabilidade da `main` no momento).

## Como você dispara a pipeline

### Modo automático (recomendado para demanda crua)

```
> Quero adicionar bloqueio de horário na agenda do profissional.
```

O orquestrador identifica que a demanda é vaga (toca permissionamento, conflito, multi-vínculo) e aciona **`imedto-business-analyst`** automaticamente. BA refina, valida com você, gera briefing, e aciona dev. Dev aciona db (se schema mudou) e qa. QA valida e publica.

### Modo explícito

```
> Execute o pipeline para implementar o briefing planejamentos/2026-05-25_001_bloqueio-agenda-profissional.md.
```

Pula o BA porque o briefing já existe e foi aprovado. Vai direto ao dev.

### Modo handoff manual

```
> Aciona o imedto-qa — terminei a implementação manualmente, está pronto para validar.
```

Útil quando você fez parte do trabalho fora da pipeline e quer só validação. QA roda o ciclo completo de validação contra o briefing referenciado.

### Modo Tipo B (escalonamento de spec gap)

```
> O QA disse que o briefing 2026-05-25_001 não previu o caso X. Resolve aí.
```

Aciona **`imedto-business-analyst`** em Modo B. Ele lê o briefing original + relato do QA, decide entre addendum ou briefing novo, valida com você, e despacha ao dev.

## Regras de ouro do fluxo (8 não-negociáveis)

1. **Briefing antes de código.** Demanda crua não vira commit. Tudo passa pelo BA primeiro (exceto trivialidades documentadas em CLAUDE.md).
2. **Briefing é imutável.** Mudou? Cria addendum. Nunca edita o original.
3. **CA é Dado/Quando/Então.** Não "deve funcionar". Não "deve estar correto". Frase verificável ou não é CA.
4. **Multi-tenant + LGPD + RBAC são CAs obrigatórios.** Mesmo que o usuário/BA não cite, o template do briefing força. QA valida mesmo sem citação explícita.
5. **Único commit point é o QA.** Dev nem DB committam. Isso garante que nada sobe sem passar pela validação completa.
6. **QA classifica bug antes de devolver.** Tipo A → dev. Tipo B → BA. Sem essa classificação, a pipeline trava em loop de patch.
7. **QA nunca corrige bug.** Mesmo typo. Devolve com diagnóstico. A pipeline aprende devolvendo.
8. **1 push por sessão.** Agrupa commits locais; faz `git push` uma vez no fim. Pipeline CI/CD da `main` é pesada (~3-5 min).

## Exemplo concreto — ponta a ponta

**Demanda inicial (usuário)**:
> Quero que o profissional consiga bloquear horários na agenda dele.

**1. `imedto-business-analyst` (Opus)**:
- Pergunta 1: "Quem pode bloquear? (a) só o próprio profissional, (b) profissional + admin, (c) profissional + admin + dono?"
- Pergunta 2: "E se já há paciente confirmado naquele horário? (a) bloqueia operação, (b) sobrescreve com aviso, (c) pergunta caso a caso?"
- Pergunta 3: "O bloqueio vale para um estabelecimento ou todos os vínculos do profissional? (a) por estabelecimento, (b) todos."
- Pergunta 4: "Recorrência? (a) só dia único, (b) recorrência semanal/mensal."
- Resposta do usuário: (a, b, a, b).
- BA monta briefing `planejamentos/2026-05-25_001_bloqueio-agenda-profissional.md` com 7 CAs (caminho feliz + multi-tenant + RBAC + LGPD + estados + performance + recorrência).
- Valida com usuário → OK → salva → despacha ao dev.

**2. `imedto-developer` (Sonnet)**:
- Lê briefing. Mapeia plano: aggregate `AgendaBloqueio`, `CriarBloqueioCommand`, `AgendaBloqueioQueryRepository`, componente `AgendaBloqueioDrawer.vue`, service `agendaBloqueioService.ts`, store `useAgendaBloqueio.ts`, testes back+front.
- Schema mudou (nova tabela) → aciona `imedto-database`.

**3. `imedto-database` (Sonnet)**:
- Modela `agenda_bloqueios (id, estabelecimento_id, profissional_id, inicio_em, fim_em, motivo, recorrencia_jsonb, criado_em, criado_por_usuario_id)`.
- Índice composto `(estabelecimento_id, profissional_id, inicio_em, fim_em)`.
- FK em `estabelecimento_id` e `profissional_id`.
- Gera migration EF + exporta SQL idempotente em `db/migrations/20260525120000_criar_agenda_bloqueios.sql`.
- Valida via psql/MCP. Reporta arquivos criados ao dev.

**4. `imedto-developer` (Sonnet)** *(continuação)*:
- Implementa aggregate + handler + query + DTO + controller + service + store + componente + testes (NUnit + Vitest).
- `dotnet build` + `npm run build` verdes. Despacha ao QA.

**5. `imedto-qa` (Sonnet)**:
- Revisa diff. Roda suíte completa. Sobe app local. Valida CA1-CA7 via chrome-devtools.
- Detecta: CA3 (multi-tenant) falha — usuário B vê bloqueio de A.
- Classifica Tipo A. Devolve ao dev com `AgendaBloqueioQueryRepository.cs:42` + sugestão.

**6. `imedto-developer`** *(re-entrada)*:
- Corrige filtro. Adiciona teste de regressão. Despacha de volta ao QA.

**7. `imedto-qa`** *(re-validação)*:
- Suíte ✓. CA3 ✓. Todos os CAs ✓.
- Commit: `feat(agenda): adicionar bloqueio de horário por profissional` + body com `Briefing: planejamentos/2026-05-25_001_*` + `Co-Authored-By`.
- Push → CI/CD aplica migration no RDS → deploy automático → smoke test.

Total: 1 push, 1 deploy, briefing imutável arquivado, testes de regressão presentes.

## Quando NÃO usar a pipeline

- **Spike / exploração de viabilidade técnica** — não muda código de produção. Vai direto no main (orquestrador humano) ou em branch descartável.
- **Refator interno puro** sem mudança observável para o usuário — pode pular o BA, mas ainda passa pelo QA (que valida ausência de regressão).
- **Hotfix urgente trivial** (1 linha, óbvio, sem regra de negócio) — descreva no commit message; QA valida e empurra.
- **Mudanças exclusivas em documentação** (`Docs/`, `README.md`) — direto, sem pipeline. QA opcional pra revisar texto.
- **Mudanças de configuração de infra** (`infra/aws-resources.md`, `.github/workflows/`) — vai pelo orquestrador humano com cuidado, fora do escopo da pipeline de features.

---

**Localização**: `.claude/agents/PIPELINE.md`. Os 4 arquivos de agente neste mesmo diretório. Briefings em `planejamentos/`. Convenção de briefing em `planejamentos/README.md`.
