---
titulo: Relatório consolidado — Fase 1 (análise estática global)
status: pronto para revisão
data: 2026-05-02
escopo: backend Imedto (918 arquivos .cs, 24 módulos de domínio)
prerequisito: Fase 0 (BASELINE.md)
---

# Relatório Fase 1 — Análise estática global do backend

Este relatório **não deleta nada**. É a fundação para as Fases 2-7 decidirem o que cortar e o que otimizar, e para as Fases 8-10 priorizarem cobertura de testes.

## TL;DR

**O backend está em estado MUITO melhor do que o esperado.** Diferente de bases legadas com 30%+ de código morto, aqui temos:

- **0 handlers órfãos** entre 89 commands + 76 queries + 26 events.
- **0 commands/queries em Contracts sem registro de handler.**
- **0 repositórios sem injeção de DI.**
- **1 evento de domínio sem handler** (`ReceitaEmitidaEvent`).
- **1 violação ativa de CQRS** (`EstabelecimentoIaSettingsController` chama repositório direto).
- **1 código inalcançável real** (em arquivo de teste).
- **15 DTOs sem consumo no frontend** (~18% dos 82 DTOs).
- **5 campos PII vazando para o front sem uso** (risco LGPD direto).
- **56 arquivos com pequenos issues de formatação** (whitespace).

A maior parte do trabalho da Fase 2-6 não vai ser **deleção em massa** — vai ser:
1. **Auditoria LGPD de DTOs** (minimizar campos retornados).
2. **Otimização de queries** (Fase 7) — onde o ganho real está.
3. **Cobertura de testes** (Fases 8-10) — base atual cobre só ~31 arquivos para 918 .cs.
4. **Atualização de pacotes vulneráveis** (Fase 2 — 2 high + 2 moderate severity).

## Ferramentas e dados

- Build com analyzers (NetAnalyzers `latest-recommended` + Roslynator + Meziantou): [baseline_build_with_analyzers.log](baseline_build_with_analyzers.log) — **0 errors, 1065 warnings**.
- Testes: [baseline_tests.log](baseline_tests.log) — **236 passed, 1 skipped, 0 failed**.
- `dotnet format --verify-no-changes`: [format_check.log](format_check.log) — **186 issues de whitespace em 56 arquivos**.
- Auditoria de handlers órfãos (relatório completo): [fase1_handlers_orfaos.md](fase1_handlers_orfaos.md).
- Auditoria de DTOs sem uso (relatório completo): [fase1_dtos_sem_uso.md](fase1_dtos_sem_uso.md).

---

## 1. Achados de alto valor (acionar nas próximas fases)

### 1.1 Vulnerabilidades de pacotes NuGet (Fase 2 — CRÍTICO)

| Pacote | Versão atual | Severidade | Advisories |
|---|---|---|---|
| `System.Security.Cryptography.Xml` | 9.0.0 | **High** | [GHSA-37gx-xxp4-5rgx](https://github.com/advisories/GHSA-37gx-xxp4-5rgx), [GHSA-w3x6-4m5h-cxqf](https://github.com/advisories/GHSA-w3x6-4m5h-cxqf) |
| `OpenTelemetry.Api` | 1.10.0 | Moderate | [GHSA-8785-wc3w-h8q6](https://github.com/advisories/GHSA-8785-wc3w-h8q6), [GHSA-g94r-2vxg-569j](https://github.com/advisories/GHSA-g94r-2vxg-569j) |

Afeta: `Imedto.Backend.API`, `Imedto.Backend.Infrastructure`, `Imedto.Backend.IntegrationTest`. Atualizar para versões patched (System.Security.Cryptography.Xml ≥ 9.0.5, OpenTelemetry.Api ≥ 1.11.x).

### 1.2 Bug latente — exception em finally (Fase 6)

[backend/src/Services/Imedto.Backend.Infrastructure/Ia/RateLimitedIaService.cs:267](backend/src/Services/Imedto.Backend.Infrastructure/Ia/RateLimitedIaService.cs#L267) — CA2219. Lançar exceção dentro de `finally` engole/mascara a exceção original. Investigar e refatorar.

### 1.3 Bug latente — async incorreto (Fase 6)

[backend/src/Services/Imedto.Backend.Infrastructure/Ia/AnthropicIaService.cs:86](backend/src/Services/Imedto.Backend.Infrastructure/Ia/AnthropicIaService.cs#L86) — CA2024. Uso de `reader.EndOfStream` em método `async` força sync-over-async (pode bloquear thread pool). Trocar pelo padrão `await reader.ReadLineAsync()` em loop.

### 1.4 Único evento de domínio órfão (Fase 4)

`ReceitaEmitidaEvent` é publicado em `Receita.Emitir()` mas o `MemoryEventBus` descarta no `if (!_handlers.TryGetValue)` ([backend/src/Services/Imedto.Backend.Infrastructure/Bus/MemoryEventBus.cs:47](backend/src/Services/Imedto.Backend.Infrastructure/Bus/MemoryEventBus.cs#L47)).

**Recomendação**: criar handler `RegistrarAcessoReceitaEmitidaEventHandler` (audit LGPD) — receita controlada precisa de log obrigatório. Não remover o evento.

### 1.5 Única violação ativa de CQRS (Fase 5/6)

`EstabelecimentoIaSettingsController` chama repositório direto em vez de passar por `_commandBus`/`_requestBus` — quebra o padrão. Criar `ObterEstabelecimentoIaSettingsQuery` + `AtualizarEstabelecimentoIaSettingsCommand`.

### 1.6 Inconsistência de lifetime DI

`ReceitaQueryRepository` está registrado como **Scoped**, enquanto todos os outros 12 `*QueryRepository` são **Singleton**. Validar se há razão (pode ser bug — Singleton + Scoped conflita com a regra de "buses singleton resolvem do scope da request" do CLAUDE.md). Possível fonte de bug latente.

### 1.7 Código inalcançável (Fase 6)

[backend/src/Tests/Imedto.Backend.Test/Infrastructure/Ia/RateLimitedIaServiceTests.cs:411](backend/src/Tests/Imedto.Backend.Test/Infrastructure/Ia/RateLimitedIaServiceTests.cs#L411) — CS0162. Remover ou ajustar a lógica de teste.

### 1.8 Identificadores ruins (Fase 2)

- [backend/src/Core/Imedto.Backend.SharedKernel/Cqrs/IEventBus.cs:9](backend/src/Core/Imedto.Backend.SharedKernel/Cqrs/IEventBus.cs#L9) — parâmetro chamado `event` (palavra reservada).
- [backend/src/Core/Imedto.Backend.SharedKernel/Cqrs/IEventHandler.cs:9](backend/src/Core/Imedto.Backend.SharedKernel/Cqrs/IEventHandler.cs#L9) — idem.

Renomear para `domainEvent` ou `@event` consistente.

### 1.9 Performance hint (Fase 7)

[backend/src/Services/Imedto.Backend.Domain/Cirurgias/ProcedimentoCirurgico.cs:113](backend/src/Services/Imedto.Backend.Domain/Cirurgias/ProcedimentoCirurgico.cs#L113) — CA1859. Trocar tipo do field `_viasAnestesicasValidas` de `IReadOnlySet<string>` para `HashSet<string>` (lookup direto sem call por interface).

---

## 2. LGPD — campos PII vazando para o front (Fase 3 — ALTO RISCO)

Estes 5 campos são retornados em payload mas **não são lidos** em nenhum `.ts`/`.vue`. Cada um é vazamento de dado pessoal por descuido. **Endereçar antes de qualquer outro item de Pacientes/Vínculos/Prontuários.**

| DTO | Campo | Tipo de PII | Local |
|---|---|---|---|
| `SolicitacaoVinculoDto` | `ProfissionalEmail` | E-mail | `Contracts/Vinculos/Queries/Results/` |
| `SolicitacaoVinculoDto` | `EstabelecimentoNomeFantasia` | Identificação institucional | idem |
| `ProfissionalResumidoDto` | `Crm` | Documento profissional | `Contracts/Profissionais/Queries/Results/` |
| `ExameFisicoDto` | `RealizadoPorNome` + `RealizadoPorUsuarioId` | Nome + ID interno do profissional | `Contracts/Prontuarios/Queries/Results/` |
| `ExameFisicoResumoDto` | `RealizadoPorNome` + `RealizadoPorUsuarioId` | idem | idem |

Detalhamento: [fase1_dtos_sem_uso.md](fase1_dtos_sem_uso.md) — seção "Campos PII vazando".

### 2.1 Bug paralelo descoberto (urgente)

`frontend/src/services/exameFisicoService.ts` consome endpoints novos com **interfaces snake_case legadas** — provável quebra silenciosa em runtime. Resolver antes de cortar campos do `ExameFisicoDto` para não mascarar a falha.

---

## 3. DTOs órfãos (Fase 4-6 — médio risco)

15 DTOs sem consumo no frontend, agrupados:

- **Catálogo (3)**: `EspecialidadeListada`, `ProfissaoListada`, `RegiaoCatalogo` — front usa rota legada `/regioes-anatomicas`. Consolidar antes de remover.
- **Receitas (2)**: `ConfiguracaoReceita`, `MedicamentoFavorito` — UI nunca implementada. **Validar com produto** se é feature planejada antes de cortar.
- **Automações (2)**: `RegraAutomacao`, `EventoAutomacao` — só `ConfiguracaoAutomacao` simples é consumida.
- **Relatórios (5)**: provável feature em construção. **Validar com produto** antes de cortar.
- **LGPD export (3)**: consumidos via blob, não como tipo TS — não são órfãos de fato; falso positivo controlado.

Detalhamento: [fase1_dtos_sem_uso.md](fase1_dtos_sem_uso.md) — seção "DTOs órfãos".

### 3.1 Campos não-PII desperdiçados (~30)

Não bloqueia LGPD, mas reduz payload e simplifica contrato. Candidatos:

- `DashboardKpisDto` — 8 de 10 campos não usados.
- `PacientesResumoDto` — alguns campos.
- `InventarioResumoDto` — alguns campos.

Tratar nas Fases 4-5 dos respectivos módulos.

---

## 4. Hotspots de warning (Fase 1.x — formatação + estilo)

### 4.1 Whitespace (`dotnet format`)

56 arquivos com 186 issues. Distribuição:

| Projeto | Arquivos |
|---|---|
| Imedto.Backend.Infrastructure | 23 |
| Imedto.Backend.Application | 15 |
| Imedto.Backend.API | 10 |
| Imedto.Backend.Domain | 4 |
| Imedto.Backend.SharedKernel | 2 |
| Imedto.Backend.IntegrationTest | 2 |

Lista completa: [format_files.txt](format_files.txt).

**Sugestão**: rodar `dotnet format Imedto.Backend.sln` em **um único commit** ("fix: format whitespace via dotnet format") antes de qualquer outra mudança da Fase 2 — evita ruído nos diffs subsequentes.

### 4.2 Top 10 arquivos com mais warnings

| Arquivo | Qtd | Predominante |
|---|---|---|
| [Controllers/OrcamentoController.cs](backend/src/Services/Imedto.Backend.API/Controllers/OrcamentoController.cs) | 56 | CS8632 (nullable annotations) |
| [Domain/Orcamentos/Orcamento.cs](backend/src/Services/Imedto.Backend.Domain/Orcamentos/Orcamento.cs) | 50 | CS8632 + CA1305 |
| [Tests/Domain/Receitas/ReceitaTests.cs](backend/src/Tests/Imedto.Backend.Test/Domain/Receitas/ReceitaTests.cs) | 46 | CA1707 (underscores em testes) |
| [Tests/Domain/Orcamentos/OrcamentoCompletoTests.cs](backend/src/Tests/Imedto.Backend.Test/Domain/Orcamentos/OrcamentoCompletoTests.cs) | 42 | CA1707 |
| [Infrastructure/Receitas/QuestPdfReceitaService.cs](backend/src/Services/Imedto.Backend.Infrastructure/Receitas/QuestPdfReceitaService.cs) | 40 | CS8632 + CA1848 |
| [Domain/Receitas/Receita.cs](backend/src/Services/Imedto.Backend.Domain/Receitas/Receita.cs) | 40 | CS8632 |
| [Infrastructure/Database/Migrations/20260429171108_Fase3Schema.cs](backend/src/Services/Imedto.Backend.Infrastructure/Database/Migrations/20260429171108_Fase3Schema.cs) | 38 | gerado por EF — **suprimir como gerado** |
| [Controllers/ReceitaController.cs](backend/src/Services/Imedto.Backend.API/Controllers/ReceitaController.cs) | 36 | CS8632 |
| [Tests/Domain/OrcamentoTests.cs](backend/src/Tests/Imedto.Backend.Test/Domain/OrcamentoTests.cs) | 32 | CA1707 |
| [Infrastructure/Jobs/JobScheduler.cs](backend/src/Services/Imedto.Backend.Infrastructure/Jobs/JobScheduler.cs) | 30 | CA1848 (logger) |

### 4.3 Distribuição por módulo de domínio (top 10)

| Módulo | Warnings |
|---|---|
| `Infrastructure/Database/` | 392 (a maioria são migrations geradas — suprimir) |
| `API/Controllers/` | 164 |
| `Domain/Receitas/` | 90 |
| `Domain/Orcamentos/` | 86 |
| `Contracts/Receitas/` | 62 |
| `Contracts/Orcamentos/` | 52 |
| `Infrastructure/Jobs/` | 42 |
| `Infrastructure/Receitas/` | 40 |
| `Domain/Catalogo/` | 40 |
| `Contracts/Prontuarios/` | 40 |

**Receitas + Orçamentos lideram** — esses dois módulos vão demandar mais atenção quando chegarem suas fases (Fase 4 Receitas, Fase 5 Orçamentos).

---

## 5. Categorias de warning para tratamento em massa

| Código | Qtd | Estratégia |
|---|---|---|
| CS8632 (1136) | nullable annotation `?` sem `<Nullable>enable</Nullable>` | Tratar em **fase própria** após Fase 6: ativar `Nullable=enable` projeto-a-projeto controladamente |
| CA1707 (486) | underscores em nome de método | **Suprimir em assemblies de teste** via `.editorconfig` — convenção universal de testes |
| CA1848 (170) | usar `LoggerMessage` delegates | Tratar **junto com Fase 7 (perf)** — impacto real em alocação |
| CA1861 (166) | arrays constantes como argumento | Endereçar caso-a-caso em refactor |
| CA1305 (36) | `IFormatProvider` em conversão de string | Resolver junto com Fase 3-6 dos módulos |
| CA1311 (12) + CA1304 (12) + CA1310 (6) + CA1862 (6) | cultura/comparação de string | Resolver junto |

---

## 6. Decisões pendentes para o usuário

Antes de iniciar a Fase 2, **confirmar**:

1. **Sub-tarefa de formatação primeiro?** — rodar `dotnet format` em 1 commit isolado para zerar os 186 issues de whitespace. Recomendado.
2. **Suprimir CA1707 em testes via `.editorconfig`?** — convenção universal de NUnit/xUnit. Recomendado.
3. **Suprimir warnings em arquivos gerados (Migrations)?** — adicionar `<NoWarn>CS8632;CA*</NoWarn>` para `*.Designer.cs` e `<Nome>_Migration.cs`. Recomendado.
4. **15 DTOs órfãos**: posso remover os 8 sem ambiguidade (Catálogo, LGPD blob, parte de Automações) na Fase 6, e listar os 7 restantes (Receitas, Relatórios, parte Automações) como **decisão de produto** antes de cortar?
5. **`ReceitaEmitidaEvent` órfão**: criar handler de audit LGPD agora (Fase 3) ou registrar como decisão de produto?
6. **Inconsistência de DI do `ReceitaQueryRepository`**: investigar e corrigir já na Fase 2 (núcleo) ou esperar Fase 4 (Receitas)?

---

## 7. Próximos passos

Fase 1 entregue. Para iniciar a **Fase 2** (Cleanup do núcleo + atualização de pacotes vulneráveis), eu preciso das respostas das 6 decisões pendentes acima.

**Recomendação de sequência para Fase 2:**

1. (commit 1) `dotnet format` global — zera whitespace.
2. (commit 2) `.editorconfig` global — suprime CA1707 em testes + warnings em migrations.
3. (commit 3) Atualizar `System.Security.Cryptography.Xml` (HIGH severity).
4. (commit 4) Atualizar `OpenTelemetry.Api` (moderate).
5. (commit 5) Renomear parâmetro `event` em `IEventBus` / `IEventHandler`.
6. (commit 6) Investigar `ReceitaQueryRepository` Scoped vs Singleton.
7. (commit 7) Auditoria do `Container.cs`/`Program.cs` por ordem de leitura — confirmar que tudo registrado é usado e nada usado está faltando.
