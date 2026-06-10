# FASE TRANSVERSAL — Qualidade de Código e Banco (contínua)

> Parte do roadmap [`README.md`](README.md). **Objetivo**: executar as correções das auditorias técnicas de 2026-06-09/10 (schema do banco + limpeza/padronização do backend + débitos do frontend). Roda **em paralelo às fases de produto, reservando ~20% da capacidade** — nunca como "big bang" que para o roadmap.
>
> **Veredito das auditorias** (importante para calibrar expectativa): o codebase está **acima da média** — schema saudável (zero tabelas mortas, zero colunas-lixo, tipos consistentes), CQRS+DDD rigoroso, erro/validação exemplares. O ganho aqui não é remoção em massa: é **fechar riscos pontuais, eliminar a tríplice sincronização manual de handlers e pagar débitos de teste/frontend**.

## O que as auditorias mandaram NÃO fazer (tão importante quanto o resto)

- **Não remover nenhuma tabela** — as 95 têm uso real (14 suspeitas eram falso-positivo: audit trails, auth, jobs, idempotência).
- **Não adicionar CHECK constraints de enum no banco** — contraria a premissa "regra no backend" (validação vive no domínio).
- **Não desnormalizar `estabelecimento_id` nos filhos** (prontuario_evolucoes etc.) — risco de divergência > ganho; o isolamento via JOIN-no-pai é disciplina coberta por QA/testes. **Risco aceito e registrado.**
- **Não renomear tabelas** (PT/EN, singular/plural) — breaking sem ganho.
- **Não introduzir FluentValidation nem `Result<T>`** — o padrão BusinessException→422 funciona e é consistente (1.329 usos).
- **Não criar view/query-builder compartilhado de Dapper** — duplicação de SQL entre repos é baixa; seria abstração especulativa.
- **`ReceitaEmitidaEvent` NÃO é órfão** ⚠️ — corrige a decisão registrada anteriormente ("7 órfãos liberados"): o evento é publicado e testado, só não tem subscriber. Ver item B2.

---

## Bloco A — Banco (rápido e seguro; executor: `imedto-database`)

| # | Item | DDL/ação | Risco | Esforço |
|---|---|---|---|---|
| A1 | Índice para a listagem de pacientes (tela mais usada) | `CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_pacientes_estab_nome_ativo ON pacientes (estabelecimento_id, nome_completo) WHERE deletado_em IS NULL;` | Baixo | P |
| A2 | Índice para lista financeira por período | `CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_lancamento_estab_venc ON lancamentos (estabelecimento_id, data_vencimento DESC);` | Baixo | P |
| A3 | Remover índice redundante (composto `ix_orcamento_estab_paciente` cobre 100% das queries) | `DROP INDEX CONCURRENTLY IF EXISTS "IX_orcamentos_paciente_id";` — antes, conferir `pg_stat_user_indexes` (idx_scan≈0) | Baixo | P |
| A4 | Prune das ~34 linhas órfãs (era Supabase) em `__ef_migrations_history` | DELETE das MigrationIds sem `.cs` no repo; validar lista contra o repo; backup antes; stage → prod | Baixo | P |
| A5 | **Investigar** `usuarios.email` não-único (trava real está em `auth_credenciais`) | Checar duplicatas + se existe caminho de criação de usuário fora do auth (convite?); só então `CREATE UNIQUE INDEX CONCURRENTLY` | Médio | M |
| A6 | **Investigar** `receitas.tipo_notificacao` (derivável de `RegrasAnvisa` — redundância ou snapshot intencional da emissão?) | Decisão de produto; se snapshot, documentar no domínio | Médio | P |

Não fazer agora: trigram GIN em `receita_itens.medicamento`/`atestados.conteudo` (busca é escopada por paciente — poucas linhas); reavaliar se a busca virar cross-paciente.

## Bloco B — Backend: limpeza e unificação (executor: pipeline normal)

| # | Item | Detalhe | Risco | Esforço |
|---|---|---|---|---|
| B0 | **Baseline de análise estática** | Rodar `dotnet build` + Roslynator e capturar CSV de warnings `RCS*`/`CA*` (usings/membros mortos quantificados); suíte verde como baseline | Nulo | P |
| B1 | Arquivar `Tools/Imedto.Backend.EtlValidator` | Validador da migração legado→RDS (concluída 2026-05-09); remover do `.sln` e mover para fora de `src/`. ⚠️ **Aval de produto** — é a testemunha da paridade legado↔novo | Baixo | P |
| B2 | **Decisão de produto: 4 eventos publicados sem subscriber** | `ReceitaEmitidaEvent`, `AtestadoEmitidoEvent`, `PedidoExameEmitidoEvent`, `TermoRevogadoEvent` — todos "documento sensível emitido/revogado" (provável seam de auditoria LGPD planejado). Opções: (a) criar `RegistrarAuditoriaDocumentoEventHandler` que os consome — fecha o ciclo LGPD; (b) remover evento+publish+asserts. **Recomendação: (a)** — alinha com o relatório de acessos da F1.8 | Baixo-Médio | P-M |
| B3 | Unificar clamp de paginação | ~13 query repos com 4 idiomas diferentes → helper `Paginacao.Normalizar(pagina, tamanho, max:100, default:20)` no SharedKernel (PacienteQueryRepository:27, Agendamento:33, ListaEspera, Financeiro, Inventario, CadastrosEstoque, Notificacao, Termo) | Baixo | P-M |
| B4 | Builders/fixtures de teste compartilhados | 216 arquivos de teste, só 3 helpers; `Mock<IEstabelecimentoRepository>` repetido 56×, `IPacienteRepository` 34×, `Paciente` inline em 27 arquivos → Object Mothers (`PacienteMother.Valido()`, `AgendamentoBuilder`) + `HandlerTestFixture` base (mocks comuns + tenant fake). Refatorar incrementalmente | Baixo | G (incremental) |

## Bloco C — Backend: padronização estrutural (maior risco; pré-condições inegociáveis)

| # | Item | Detalhe | Risco | Esforço |
|---|---|---|---|---|
| C1 | `CancellationToken` nas interfaces CQRS | `ICommandHandler<>.Handle`/`IRequestHandler<,>.Handle`/`IEventHandler<>.Handle` ganham `CancellationToken ct = default`; propagar buses → 241 handlers → `CommandDefinition` do Dapper. Hoje só os 39 handlers do Admin têm CT (gargalo é a interface, não os handlers). Refactor mecânico, alto valor (query longa cancela quando o cliente desconecta) | Médio | G |
| C2a | Assembly scanning — parte 1 (commands) | Pré-condições: `ValidateOnBuild=true` + `ValidateScopes=true` no Program.cs (captive dependency vira erro de startup) **e** teste de fechamento (reflection: todo handler resolve do provider e está roteado no bus). Depois: Scrutor scan de `ICommandHandler<>` (203 handlers, todos Scoped — regra uniforme) + auto-popular `MemoryCommandBus`. Excluir `Application.Admin` do scan | Alto | M-G |
| C2b | Assembly scanning — parte 2 (queries + events) | Query handlers têm lifetime **misto de propósito** (90 Singleton + 38 Scoped por causa dos serviços scoped de audit LGPD — ex.: `ExameFisicoQueryHandlers`): marker interface `IScopedReadHandler` nos 38 + Singleton no resto; scan de `IEventHandler<>` (N handlers por evento). Manter manuais: decorators/factories (`IIaService` rate-limited, `IEmailService` por config, `SoftDeleteInterceptor`) e Admin. **Meta: Container.cs de 1.282 → ~300-400 linhas** | Alto | G |
| C3 | Decisão arquitetural: contexto Admin fora do bus | Admin injeta handlers concretos direto nos controllers (~66 handlers fora do bus) e é o único com CT. Decidir: trazer para o bus **ou** documentar oficialmente como back-office fora do padrão em `Docs/ARQUITETURA.md`. Sem decisão, o scan (C2) fica com exceção não-documentada | Médio | M |
| C4 | Auditoria de endpoints sem consumidor | Gerar inventário de rotas resolvidas × 343 chamadas do front × lista de públicos/webhooks intencionais; candidatos viram "investigar com dono" — **sem deleção automática** (falso-positivo é caro) | Médio | M |

**Ordem dentro do bloco C: C1 → C2a → C2b → (C3, C4).** C2 nunca antes de C1 (assinatura estável) nem sem as duas redes de segurança.

## Bloco D — Frontend (contínuo, 1 item por ciclo)

| # | Item | Detalhe | Esforço |
|---|---|---|---|
| D1 | God components — 1 por ciclo | Ordem por tráfego clínico: `NovoAgendamentoModal.vue` (1.841 linhas — casa com o redesenho UX do fluxo de agendamento), `PacienteDetalheView` (1.408), `SecaoExameFisico` (1.142), `OnboardingView` (1.717 — casa com F2.6), `OrcamentoFormView` (1.467 — casa com F1.6). Extrair sub-componentes + composables; suíte Vitest como rede | G (fatiado) |
| D2 | Estoque de violações tipográficas §5 | ~1.300 literais de `font-size`/`font-weight`; o lint da F1.10 congela o estoque — aqui ele **encolhe**: migrar por componente junto com D1 (mesmos arquivos concentram as violações) | M (carona no D1) |
| D3 | `any` → `unknown` nos catches + tipar os 288 `any` de produção | Prioridade nos services/stores (contratos com o back); `doc as any` do jsPDF pode ficar (lib mal tipada) | M (incremental) |
| D4 | **Decisão: design system único** | `@imedto/ui` (pacote 185MB, ~3 imports) vs `components/ui/` local (37 componentes, fonte real + gotcha conhecido do CSS). Recomendação: oficializar o local, arquivar o pacote. Junto: tirar `ReferenciaLegado/` (46MB), `mobile/` (152MB) e `design-system/` (185MB) do monorepo (repos próprios/arquivo) | M |

## Sequenciamento e cadência

```
Imediato (1 sessão):     A1 A2 A3 (índices) + B0 (baseline)
Curto (com aval produto): A4 B1 B2 + B3
Contínuo (1/ciclo):       B4 + D1+D2 + D3
Estrutural (janela calma): C1 → C2a → C2b → C3 C4 + D4 + A5 A6
```

Critério de verificação universal: build + suíte completa verdes antes/depois de cada item; itens de remoção exigem `grep` zero-referências; C2 exige teste de fechamento passando.

## Execução

Bloco A → `imedto-database` (migrations CONCURRENTLY em `db/migrations/`). Blocos B/C/D → demandas técnicas diretas (dev+QA, sem BA), **exceto** B1, B2, C3 e D4 que têm decisão de produto/arquitetura embutida (aval do usuário ou BA em modo decisão antes de executar). Itens com risco Alto (C2) nunca na mesma sessão de features de produto.
