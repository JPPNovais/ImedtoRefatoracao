---
titulo: Plano de limpeza, otimização e testes do backend
status: rascunho
criado_em: 2026-05-02
escopo: backend/src completo (918 arquivos .cs, 24 módulos de domínio, 34 controllers)
---

# Plano de limpeza, otimização e cobertura de testes — backend Imedto

## Objetivo geral

1. Remover código morto, duplicado ou redundante sem quebrar comportamento existente.
2. Aplicar otimizações de performance e boas práticas (queries, alocação, async, EF Core).
3. Subir cobertura de testes (unit + integração) para o backend inteiro, com foco em fluxos LGPD-críticos primeiro.

## Princípios-guia (puxados do CLAUDE.md)

- **Cirúrgico**: cada mudança deve ter motivo. Sem refactor especulativo.
- **Sem assumir**: se um símbolo "parece morto", **antes de deletar** confirmar via busca + DI registration + reflexão + uso pelo front.
- **LGPD é premissa**: toda otimização de query revisa se o `WHERE estabelecimento_id` continua presente.
- **Defense-in-depth**: regras do front têm trava no back; manter.
- **Testes validam comportamento, não implementação**: privilegiar testes de integração HTTP + Postgres real (Testcontainers) sobre mocks.

## Pré-requisitos (executar UMA vez antes de começar)

- [ ] **PRÉ-1** — Commitar ou stashar as 19 alterações pendentes no working tree (Vinculo/Convite/Onboarding). Não misturar com cleanup.
- [ ] **PRÉ-2** — Rodar `dotnet build Imedto.Backend.sln /p:TreatWarningsAsErrors=true` e capturar baseline de warnings em `docs/baseline_warnings.txt`. Fica como referência de "o que vamos zerar".
- [ ] **PRÉ-3** — Rodar `dotnet test` atual e capturar resultado em `docs/baseline_tests.txt`. Esse é o ponto de partida.
- [ ] **PRÉ-4** — Adicionar (sem ativar como erro) os analyzers `Microsoft.CodeAnalysis.NetAnalyzers`, `Roslynator.Analyzers` e `Meziantou.Analyzer` no `Directory.Build.props`. Vão guiar muita coisa nas próximas fases.
- [ ] **PRÉ-5** — Garantir Docker rodando local (Testcontainers depende disso para Postgres em testes de integração).

## Estrutura das fases

Cada fase tem: **objetivo**, **escopo**, **passos**, **critério de pronto** e **agente responsável**. Fases são sequenciais — não pular. Dentro de uma fase, módulos podem ir em paralelo.

---

## FASE 1 — Tooling + análise estática global

**Objetivo**: enxergar o problema antes de mexer. Gerar relatório do que está morto/redundante/má prática **sem deletar nada**.

**Escopo**: solução inteira.

**Passos**:
- [ ] 1.1 Criar `backend/src/Directory.Build.props` ativando `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`.
- [ ] 1.2 Adicionar pacotes de analyzer (Roslynator, Meziantou, NetAnalyzers) como `PrivateAssets="all"`.
- [ ] 1.3 Rodar `dotnet build` — capturar **todos os warnings** num CSV/MD por categoria (CA*, RCS*, MA*).
- [ ] 1.4 Rodar `dotnet format --verify-no-changes` — listar arquivos fora do padrão.
- [ ] 1.5 Listar `using` desnecessários, variáveis privadas não lidas, métodos privados sem chamada (CS0414, CS0169, CS8632, CA1822 etc.).
- [ ] 1.6 Identificar **handlers/serviços não registrados** em `Container.cs` — são candidatos a remoção total.
- [ ] 1.7 Identificar **DTOs em Contracts não consumidos pelo front** (cross-check com `frontend/src/services/`).
- [ ] 1.8 Gerar `docs/relatorio_fase1.md` com o inventário completo categorizado por módulo.

**Critério de pronto**: relatório existe e foi revisado pelo dono do produto. Nenhum código foi deletado nesta fase.

**Agente**: `senior-software-engineer` (gera o relatório) + execução manual dos comandos `dotnet`.

**Risco**: baixo (não muta código de produção).

---

## FASE 2 — Cleanup do núcleo (SharedKernel + Infra transversal)

**Objetivo**: deixar a fundação limpa antes de mexer nos módulos.

**Escopo**:
- `Core/Imedto.Backend.SharedKernel/` (Cqrs, Filters, Tenancy, Domain)
- `Services/Imedto.Backend.Infrastructure/Bus/`
- `Services/Imedto.Backend.Infrastructure/Database/` (DbContext, interceptors, factory)
- `Services/Imedto.Backend.Infrastructure/Tenancy/`
- `Services/Imedto.Backend.Infrastructure/Auth/`
- `Services/Imedto.Backend.Infrastructure/Storage/`
- `Services/Imedto.Backend.API/Container.cs`
- `Services/Imedto.Backend.API/Program.cs`

**Passos**:
- [ ] 2.1 Validar que `IRequestBus` / `ICommandBus` / `IEventBus` resolvem do `IHttpContextAccessor.HttpContext.RequestServices` (CLAUDE.md fala disso explicitamente — não pode mudar).
- [ ] 2.2 Auditar `Container.cs`: remover registros de handlers/services órfãos detectados na Fase 1.
- [ ] 2.3 Verificar interceptors do EF (SoftDelete, Auditoria, Tenancy) — confirmar que estão na cadeia certa e sem duplicação.
- [ ] 2.4 Revisar `AppDbContext`: `DbSet<T>` declarados sem `EntityTypeConfiguration` correspondente, ou configurations sem `DbSet`.
- [ ] 2.5 `Program.cs`: remover middlewares e services não usados, consolidar configuração de auth (JWKS).
- [ ] 2.6 Rodar `dotnet build` + `dotnet test` — zero regressão.

**Critério de pronto**: build limpo (sem warnings novos), testes passam, número de linhas reduzido em SharedKernel/Infra/Container, registros de DI batem 1:1 com handlers existentes.

**Agente**: `senior-software-engineer`.

**Risco**: alto (mexer no Container quebra tudo se errar). Cada remoção precisa de `grep` confirmando 0 referências.

---

## FASE 3 — Cleanup módulos LGPD-críticos

**Objetivo**: limpar e otimizar os módulos que tocam dado pessoal sensível primeiro. Aqui é onde um bug custa caro.

**Escopo (em ordem de criticidade)**:
1. `Pacientes` — dado do titular, alvo nº 1 de LGPD.
2. `Prontuarios` (+ `ProntuarioAnexo`, `ExameFisico`, `ProntuarioAcessoLog`) — dado de saúde, audit trail obrigatório.
3. `Auth` + `Usuarios` — autenticação, sessão, JWT.
4. `Vinculos` (+ `SolicitacaoVinculo`) — controla quem vê o quê.
5. `ModelosPermissao` — autorização granular.
6. `Lgpd` (Consentimento, Anonimização) — direito do titular.

**Passos por módulo** (repetir):
- [ ] 3.x.1 Remover handlers/queries/repositórios identificados na Fase 1 como mortos.
- [ ] 3.x.2 Auditar DTOs: remover campos retornados ao front que **não são exibidos** (minimização LGPD). Cross-check com `frontend/src/services/` e `frontend/src/views/`.
- [ ] 3.x.3 Auditar todas as queries Dapper: garantir `WHERE estabelecimento_id = @tenantId` em **todo** SELECT. Sem exceção.
- [ ] 3.x.4 Auditar mensagens de erro: nenhuma vaza CPF/email/nome/ID interno.
- [ ] 3.x.5 Auditar logs (`ILogger<T>`): nenhum `LogInformation` com PII.
- [ ] 3.x.6 Otimizar query: `AsNoTracking()` em leitura, projeção (`Select`) em vez de carregar entidade inteira, `IAsyncEnumerable` para listagens grandes.
- [ ] 3.x.7 Validar que `ProntuarioAcessoLog` está sendo gravado em **todo** acesso a prontuário (entrada de log obrigatória).

**Critério de pronto por módulo**: build limpo, todos os testes existentes passam, code review do agente registrado em `docs/cleanup_<modulo>.md`, queries Dapper auditadas (lista checada em comentário do PR).

**Agente**: `senior-software-engineer` para a remoção/refactor; `security-engineer` para a auditoria LGPD do módulo.

**Risco**: alto. Cada PR de módulo é separado.

---

## FASE 4 — Cleanup módulos de operação clínica

**Escopo**:
1. `Agendamentos` (+ `ListaEsperaAgendamento`)
2. `Cirurgias` (`ProcedimentoCirurgico`)
3. `Receitas` (+ `ItemReceita`, `MedicamentoFavorito`, `ConfiguracaoReceitaEstabelecimento`)
4. `Inventario` (`ItemInventario`, `MovimentacaoEstoque`)

**Passos**: mesmos 3.x.1 a 3.x.6 (sem 3.x.7 — específico de prontuário).

**Critério de pronto**: igual Fase 3.

**Agente**: `senior-software-engineer`.

**Risco**: médio.

---

## FASE 5 — Cleanup módulos comerciais e administrativos

**Escopo**:
1. `Financeiro` (`Lancamento`, `CategoriaFinanceira`, `FormaPagamento`)
2. `Orcamentos`
3. `Assinaturas` (`Plano`, `Assinatura`, `Features`)
4. `Estabelecimentos` (+ `DataBloqueada`)
5. `Unidades`
6. `Salas`
7. `Profissionais`
8. `Catalogo` (`Profissao`, `Especialidade`, `ProcedimentoCatalogo`, `RegiaoAnatomicaCatalogo`)

**Passos**: mesmos 3.x.1 a 3.x.6.

**Critério de pronto**: igual Fase 3.

**Agente**: `senior-software-engineer`.

**Risco**: baixo a médio.

---

## FASE 6 — Cleanup módulos auxiliares e infra de aplicação

**Escopo**:
1. `Automacoes` (Configuracao, Regra, Evento)
2. `Notificacoes`
3. `Ia` (`EstabelecimentoIaSettings`, `AiAuditLog`) + `RateLimitedIaService`
4. `Dashboard`
5. `Relatorios`
6. `Admin` (+ `ResetModulos`)
7. `Auditoria`
8. `Jobs`
9. `Idempotency`
10. `Common`
11. `Tools/Imedto.Backend.EtlValidator/` — verificar se ainda faz sentido manter (ETL de migração legado).

**Passos**: mesmos 3.x.1 a 3.x.6 + decisão explícita sobre o EtlValidator (manter ou arquivar).

**Critério de pronto**: igual Fase 3.

**Agente**: `senior-software-engineer`.

**Risco**: baixo.

---

## FASE 7 — Otimizações de performance horizontais

**Objetivo**: depois que o código está limpo, atacar performance com alvo, não chute.

**Escopo**: solução inteira, mas guiada por evidência.

**Passos**:
- [ ] 7.1 Rodar EF Core com `LogTo(Console.WriteLine, LogLevel.Information)` em dev e exercitar fluxos críticos. Capturar SQLs e identificar N+1.
- [ ] 7.2 Para cada N+1: trocar por `Include()` específico, projeção, ou query Dapper dedicada.
- [ ] 7.3 Auditar **toda** query Dapper para garantir uso de parâmetros (proteção SQL injection — defense-in-depth).
- [ ] 7.4 Adicionar índices Postgres faltantes (gerar migration EF + SQL idempotente para `supabase/migrations/`). Foco em colunas usadas em `WHERE estabelecimento_id`, `WHERE paciente_id`, ordenação por `created_at`.
- [ ] 7.5 `AsNoTracking()` em **todas** as queries de leitura via EF (handlers de query que não usam Dapper).
- [ ] 7.6 Substituir `.ToListAsync()` por `.AsAsyncEnumerable()` em listagens que viram stream (export, relatório).
- [ ] 7.7 Habilitar `EnableDetailedErrors` e `EnableSensitiveDataLogging` **só em dev** — confirmar que está desligado em prod.
- [ ] 7.8 Cache de leitura: avaliar `IMemoryCache` para `Catalogo` (Profissão, Especialidade, Procedimento) — invalidação por evento.
- [ ] 7.9 Connection pooling: validar `Pooling=true;Maximum Pool Size=N` na connection string runtime (transaction mode 6543).
- [ ] 7.10 Resposta HTTP: confirmar gzip/brotli habilitado em `Program.cs` para JSONs grandes.
- [ ] 7.11 Idempotency: garantir TTL configurado e cleanup job rodando.

**Critério de pronto**: lista de SQLs antes/depois, índices criados via migration EF + SQL idempotente, número de queries por request reduzido (medição em fluxo de listar agendamento, listar paciente, dashboard).

**Agente**: `performance-engineer` para análise + `senior-software-engineer` para implementação.

**Risco**: médio. Toda otimização precisa de teste antes/depois.

---

## FASE 8 — Testes unitários de Domain (agregados)

**Objetivo**: cobrir 100% das fábricas e métodos de comportamento dos aggregate roots.

**Escopo**: cada aggregate em `Services/Imedto.Backend.Domain/`.

**Cobertura por agregado** (lista checada):
- [ ] 8.1 `Pacientes/Paciente`
- [ ] 8.2 `Prontuarios/Prontuario`
- [ ] 8.3 `Prontuarios/ProntuarioEvolucao`
- [ ] 8.4 `Prontuarios/ProntuarioAnexo`
- [ ] 8.5 `Prontuarios/ExameFisico`
- [ ] 8.6 `Prontuarios/ModeloDeProntuario`
- [ ] 8.7 `Prontuarios/ProntuarioAcessoLog`
- [ ] 8.8 `Agendamentos/Agendamento`
- [ ] 8.9 `Agendamentos/ListaEsperaAgendamento`
- [ ] 8.10 `Vinculos/VinculoProfissionalEstabelecimento` (já tem teste; revisar e completar)
- [ ] 8.11 `Vinculos/SolicitacaoVinculo`
- [ ] 8.12 `Receitas/Receita`, `ItemReceita`, `MedicamentoFavorito` (já tem teste; revisar e completar)
- [ ] 8.13 `Cirurgias/ProcedimentoCirurgico` (já tem teste; revisar)
- [ ] 8.14 `Financeiro/Lancamento`, `CategoriaFinanceira`, `FormaPagamento`
- [ ] 8.15 `Orcamentos/Orcamento` + calculadora (já tem teste; revisar)
- [ ] 8.16 `Inventario/ItemInventario`, `MovimentacaoEstoque` (parcial; completar)
- [ ] 8.17 `Notificacoes/Notificacao` (já tem teste; revisar)
- [ ] 8.18 `Assinaturas/Assinatura`, `Plano` (já tem teste; revisar)
- [ ] 8.19 `Profissionais/Profissional`
- [ ] 8.20 `Estabelecimentos/Estabelecimento`, `DataBloqueada`
- [ ] 8.21 `Unidades/UnidadeEstabelecimento`
- [ ] 8.22 `Salas/Sala`
- [ ] 8.23 `Catalogo/*`
- [ ] 8.24 `Automacoes/RegraAutomacao`, `ConfiguracaoAutomacao`, `EventoAutomacao`
- [ ] 8.25 `Lgpd/LgpdConsentimento`, `LgpdAnonimizacao`
- [ ] 8.26 `Ia/EstabelecimentoIaSettings`, `AiAuditLog`
- [ ] 8.27 `ModelosPermissao/*`
- [ ] 8.28 `Usuarios/Usuario`
- [ ] 8.29 `Idempotency/IdempotencyKey`

**Padrão dos testes**:
- NUnit 4 + FluentAssertions (avaliar adoção; hoje é só NUnit).
- Um arquivo por aggregate em `Tests/Imedto.Backend.Test/Domain/<Modulo>/`.
- Cobrir: fábrica feliz, fábrica com inputs inválidos (uma asserção por regra), métodos de comportamento, eventos de domínio publicados, invariantes mantidas.
- **Não testar getters/setters**.

**Critério de pronto**: `dotnet test --collect:"XPlat Code Coverage"` mostra ≥ 90% de linhas em `Imedto.Backend.Domain`.

**Agente**: `senior-qa-engineer`.

**Risco**: baixo (testes são aditivos).

---

## FASE 9 — Testes unitários de CommandHandlers (Application)

**Objetivo**: cobrir todos os 88 command handlers com Moq dos repositórios + buses + serviços externos.

**Escopo**: `Services/Imedto.Backend.Application/**/*CommandHandler.cs`.

**Padrão**:
- Um arquivo por handler em `Tests/Imedto.Backend.Test/Application/<Modulo>/<Handler>Tests.cs`.
- Cobrir: caminho feliz, validação de input, autorização (estabelecimento errado → erro), conflitos de invariante, publicação de eventos esperados.
- Mock de `IAuthService`, `ICurrentTenantAccessor`, `INotificacaoService`, `IFotoStorageService`, etc.

**Lista priorizada** (não exaustiva — gerada da Fase 1):
- [ ] 9.1 Auth + Usuarios + Vinculos + ModelosPermissao (autorização — primeiro)
- [ ] 9.2 Pacientes + Prontuarios + Prontuario anexo/evolução
- [ ] 9.3 Agendamentos + Lista de espera
- [ ] 9.4 Cirurgias + Receitas
- [ ] 9.5 Financeiro + Orcamentos + Assinaturas
- [ ] 9.6 Inventario
- [ ] 9.7 Estabelecimentos + Unidades + Salas + Profissionais + Catalogo
- [ ] 9.8 Automacoes + Notificacoes + Ia + Lgpd + Admin

**Critério de pronto**: cobertura ≥ 80% em `Imedto.Backend.Application`. Cada handler tem ≥ 1 teste de caminho feliz e ≥ 1 de erro.

**Agente**: `senior-qa-engineer`.

**Risco**: baixo.

---

## FASE 10 — Testes de integração end-to-end (HTTP + Postgres real)

**Objetivo**: validar a aplicação pelo `WebApplicationFactory` com Postgres real (Testcontainers) — auth, RLS, CQRS, transação, evento, tudo junto.

**Escopo**: `Tests/Imedto.Backend.IntegrationTest/`.

**Setup** (uma vez, no início da fase):
- [ ] 10.0.1 Adicionar `Testcontainers.PostgreSql` ao projeto de integração.
- [ ] 10.0.2 Criar `IntegrationTestFixture` base com:
  - Container Postgres 17 (mesma versão da prod).
  - Aplica todas as migrations de `supabase/migrations/` no startup do container.
  - Substitui `IAuthService` por fake que aceita JWT pré-gerado (ES256 com chave de teste).
  - Substitui `IFotoStorageService` por in-memory.
  - Substitui `INotificacaoService` por spy.
  - Reset de banco entre testes (`Respawn` ou `TRUNCATE` controlado).

**Fluxos a cobrir** (ordem de prioridade — LGPD primeiro):
- [ ] 10.1 **Auth**: signup, login, refresh, logout, expiração de token, papel/permissão (existe `RateLimitTests.cs` — completar).
- [ ] 10.2 **Vinculo**: convite, aceite, recusa, reativação, listagem por estabelecimento (defesa LGPD: usuário A não vê convites de B).
- [ ] 10.3 **Paciente**: criar, listar, atualizar, soft-delete (existe `PacienteSoftDeleteTests` — completar), exportar dados (LGPD), anonimizar (LGPD).
- [ ] 10.4 **Prontuário**: criar evolução, anexar arquivo, registrar exame físico, ler prontuário (assert: `ProntuarioAcessoLog` foi gravado), tentativa de acesso de estabelecimento errado → 403/404.
- [ ] 10.5 **Agendamento**: criar, atualizar (existe `AtualizarAgendamentoCommandHandlerTests` — promover para integração), cancelar, conflito de horário, lista de espera.
- [ ] 10.6 **Cirurgia + Receita**: criar receita controlada, validar regras Anvisa, anexar receita à cirurgia.
- [ ] 10.7 **Financeiro + Orçamento**: criar lançamento, gerar orçamento, conversão orçamento→lançamento, relatório.
- [ ] 10.8 **Assinatura**: criar plano, assinar, validar limites (existe `AssinaturaServiceLimiteTests` — completar), cancelar.
- [ ] 10.9 **Inventário**: entrada, saída, alerta de estoque mínimo.
- [ ] 10.10 **Estabelecimento + Unidade + Sala + Profissional**: CRUD básico, defesa multi-tenant.
- [ ] 10.11 **Automação + Notificação + IA**: trigger de evento, envio de notificação (spy), chamada de IA (mock + audit log gravado).
- [ ] 10.12 **LGPD**: consentimento, anonimização, log de acesso, export de dados.
- [ ] 10.13 **Admin + Auditoria + Idempotency**: reset módulos, query de auditoria, idempotência de endpoint duplicado.

**Padrão**: cada fluxo é um arquivo `<Fluxo>FluxoTests.cs`. Cada teste é HTTP-driven (`HttpClient` do `WebApplicationFactory`).

**Critério de pronto**: todos os fluxos acima têm pelo menos um teste de happy path + um de defesa multi-tenant. CI roda integração em < 5 min.

**Agente**: `senior-qa-engineer` (estratégia + setup) + `qa-engineer` (escrita de testes individuais — pode paralelizar por fluxo).

**Risco**: médio (Testcontainers é peso novo, exige Docker no CI).

---

## FASE 11 — Cobertura final + portões de CI

**Objetivo**: travar regressão.

**Passos**:
- [ ] 11.1 Configurar `coverlet.msbuild` + relatório `cobertura` no `dotnet test`.
- [ ] 11.2 Adicionar GitHub Actions (ou equivalente) com:
  - `dotnet build` com `TreatWarningsAsErrors`.
  - `dotnet test` (unit + integração).
  - Falha se cobertura cair abaixo de: Domain ≥ 90%, Application ≥ 80%, Infrastructure ≥ 60%.
  - `dotnet format --verify-no-changes`.
- [ ] 11.3 Adicionar regra: PR que mexe em `Services/Imedto.Backend.Domain/` ou `Services/Imedto.Backend.Application/` precisa ter teste novo (ou revisar teste existente).
- [ ] 11.4 Documentar em `CONTRIBUTING.md` (criar se não existir) o fluxo "sempre rode `dotnet test` antes de abrir PR".

**Critério de pronto**: CI verde, gates ativos, README atualizado.

**Agente**: `dx-engineer` (estratégia DORA / gates) + `cloud-engineer` (escrever os YAMLs de Actions).

**Risco**: baixo.

---

## FASE 12 — Revisão final e pull request por fase

**Objetivo**: garantir que cada fase virou PR atômico revisável, não um diff de 50k linhas.

**Regras**:
- **Um PR por fase** (ou um PR por módulo dentro de Fase 3-6 se ficar grande).
- PR deve conter: descrição, lista de itens removidos/otimizados, prints de cobertura antes/depois, link pra issue/fase do plano.
- Sem `--no-verify`, sem skipar testes.

---

## Ordem de execução resumida

```
PRÉ → FASE 1 → FASE 2 → FASE 3 → FASE 4 → FASE 5 → FASE 6 → FASE 7
                                        ↓
                                     FASE 8 (pode começar paralelo a 7)
                                        ↓
                                     FASE 9 → FASE 10 → FASE 11 → FASE 12
```

## Estimativa grossa (sessões de trabalho)

- Pré-requisitos: 1 sessão.
- Fase 1: 1 sessão.
- Fase 2: 1-2 sessões.
- Fase 3: 4-6 sessões (1 por módulo crítico).
- Fase 4: 2-3 sessões.
- Fase 5: 2-3 sessões.
- Fase 6: 2 sessões.
- Fase 7: 2-3 sessões.
- Fase 8: 3-4 sessões.
- Fase 9: 4-6 sessões.
- Fase 10: 5-7 sessões.
- Fase 11: 1 sessão.
- Fase 12: distribuído ao longo das anteriores.

**Total**: ~30-40 sessões de trabalho focado.

## Observações finais

- Em cada fase, **rodar `dotnet build` + `dotnet test` antes de declarar pronto**.
- Em cada fase que mexe no domínio, validar `supabase db push` segue verde (no banco de dev) — schema continua compatível.
- LGPD não é fase à parte: é critério em **toda** mudança que toca paciente/prontuário/financeiro.
- Se a Fase 1 mostrar que a quantidade de código morto é baixa (< 5%), reduzir escopo das Fases 3-6 e priorizar Fase 7 (otimização) e Fases 8-10 (testes).
