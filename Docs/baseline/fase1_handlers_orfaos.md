# Fase 1 — Auditoria de código órfão (camada Application + apoio)

> Snapshot tirado em 2026-05-02. Critério: o que **não** está registrado em
> `backend/src/Services/Imedto.Backend.API/Container.cs` ou em
> `backend/src/Services/Imedto.Backend.Infrastructure/Container.cs` é considerado
> órfão (os buses são manuais — não há reflection/`Assembly.GetTypes()`).

## Sumário executivo

| Categoria | Total no disco | Registrado / utilizado | Órfãos |
|-----------|---------------:|-----------------------:|-------:|
| `*CommandHandler` (classes)        | 89  | 89  | **0** |
| `*QueryHandler*` (classes)         | 76  | 76  | **0** |
| `*EventHandler` + `*Handler` reativos | 26  | 26  | **0** |
| Commands em `Contracts`            | 114 | 114 | **0** |
| Queries em `Contracts`             | 74  | 74  | **0** |
| Domain Events em `Domain`          | 21  | 20  | **1** (`ReceitaEmitidaEvent`) |
| Repositórios EF/Dapper             | 83  | 83  | **0** |
| `*Service` em `Infrastructure`     | 26  | 26  | **0** |
| Controllers totalmente bus-less    | 34  | —   | **4** (intencionais — ver §6) |

**Conclusão**: a Application está extremamente limpa — não há handler, command, query, repositório ou service infra abandonado. Há **um único achado real**: o evento de domínio `ReceitaEmitidaEvent` é publicado mas nenhum handler o consome — o `MemoryEventBus` simplesmente descarta. Detalhes abaixo.

---

## 1. Handlers órfãos (`*CommandHandler`, `*QueryHandlers`, `*EventHandler`)

### Metodologia
1. Extraí **classes** (não nomes de arquivo) de `*Handler*.cs` em `Imedto.Backend.Application/`:
   ```
   grep -rhE "^public (sealed )?class [A-Z][A-Za-z0-9_]*(CommandHandler|QueryHandler|QueryHandlers|EventHandler|Handler)\b" Application/
   ```
   → 211 classes.
2. Extraí registros do `Container.cs`:
   ```
   grep -oE "(AddScoped|AddSingleton)<[A-Za-z0-9_]+>" Container.cs
   ```
3. Diff `disk_classes` ∖ `registered_handlers`.

### Resultado

```
$ comm -23 disk_classes registered_handlers
(vazio)

$ comm -13 disk_classes registered_handlers
NotificacaoCriadaSignalRBridge
```

- O único item "registrado mas não em Application" é
  [`NotificacaoCriadaSignalRBridge`](../../backend/src/Services/Imedto.Backend.API/Realtime/NotificacaoCriadaSignalRBridge.cs)
  — vive na camada `API/Realtime/` (bridge de SignalR). **Falso positivo** (não é Application por design).
- **Toda classe `*Handler` da Application é registrada explicitamente em `Container.cs`.**

### Cuidado com nomes de arquivo (não confundir)
Três arquivos têm nome != nome da classe interna — todos registrados:
- `Application/Orcamentos/Catalogos/CatalogoQueryHandlers.cs` → contém 8 classes (`Listar*QueryHandlers`).
- `Application/Orcamentos/Catalogos/CatalogoCommandHandlers.cs` → contém ~17 classes (`Criar/Atualizar/Remover*CommandHandler`).
- `Application/Agendamentos/Commands/ListaEsperaCommandHandlers.cs` → 2 classes (`Adicionar/RemoverListaEsperaCommandHandler`).
- `Application/Automacoes/Commands/AtivarDesativarRegraAutomacaoHandlers.cs` → `AtivarRegraAutomacaoCommandHandler` + `DesativarRegraAutomacaoCommandHandler`.
- `Application/Lgpd/Queries/ExportarMeusDadosQueryHandlers.cs` → classe `ExportarMeusDadosLgpdQueryHandlers` (note o sufixo `Lgpd`).
- `Application/Prontuarios/Queries/ExameFisicoQueryHandlers.cs` → classe `ObterExameFisicoQueryHandlers` (cobre 4 queries: `ObterExameFisicoQuery`, `ObterExameFisicoPorEvolucaoQuery`, `ListarExamesFisicosDoPacienteQuery`, `TimelineExamesFisicosQuery` — todas registradas).
- `Application/Cirurgias/Queries/ObterProcedimentoQueryHandlers.cs` → classe `ObterProcedimentoQueryHandlers` cobre 2 queries (`ObterProcedimentoQuery` + `ListarProcedimentosDoPacienteQuery` — ambas registradas em [Container.cs:763–764](../../backend/src/Services/Imedto.Backend.API/Container.cs)).

---

## 2. Commands / Queries / Events sem handler

### Commands (114 / 114) — OK

Todos os `*Command` em `Imedto.Backend.Contracts/` aparecem em `bus.Register<XCommand, XCommandHandler>()`. Diff vazio.

### Queries (74 / 74) — OK

Idem. Toda `*Query` em `Contracts/` tem registro no `MemoryRequestBus`.

### Events (20 / 21) — **1 órfão**

| Evento | Definição | Publicado em | Handlers registrados |
|--------|-----------|--------------|----------------------|
| `ReceitaEmitidaEvent` | [`Domain/Receitas/Events/ReceitaEmitidaEvent.cs:10`](../../backend/src/Services/Imedto.Backend.Domain/Receitas/Events/ReceitaEmitidaEvent.cs) | [`Domain/Receitas/Receita.cs:247`](../../backend/src/Services/Imedto.Backend.Domain/Receitas/Receita.cs) (`Receita.MarcarEmitida` chama `AddDomainEvent(new ReceitaEmitidaEvent(...))`) | **nenhum** — `MemoryEventBus.Publish` retorna no `if (!_handlers.TryGetValue(...))` ([MemoryEventBus.cs:47–48](../../backend/src/Services/Imedto.Backend.Infrastructure/Bus/MemoryEventBus.cs)) |

**Risco de remover**: **médio**. O evento é semanticamente correto (faz parte do modelo de domínio rico de Receita), e há features futuras óbvias que vão consumi-lo (notificação ao paciente, audit LGPD da emissão de receita controlada, integração com SUS/CFM). **Recomendação**: **manter** o evento e abrir um TODO para criar o handler — não é "código morto", é "feature event aguardando subscriber". A publicação custa praticamente zero (memória).

---

## 3. Handlers registrados que não existem em disco

```
$ comm -13 disk_classes registered_handlers
NotificacaoCriadaSignalRBridge   ← vive em API/Realtime, não Application — OK
```

**Nenhum registro quebrado.** O linker já reclamaria.

Atenção a uma sutileza: `IConfiguracaoReceitaRepository`/`IMedicamentoFavoritoRepository` são registrados em [`Container.cs:347–348`](../../backend/src/Services/Imedto.Backend.API/Container.cs) e suas implementações **não existem em arquivos próprios** — mas as classes `ConfiguracaoReceitaRepository` (linha 44) e `MedicamentoFavoritoRepository` (linha 74) vivem dentro de [`Infrastructure/Database/Repositories/ReceitaRepository.cs`](../../backend/src/Services/Imedto.Backend.Infrastructure/Database/Repositories/ReceitaRepository.cs). Análogo: `ProntuarioEvolucaoRepository` está em [`ProntuarioRepository.cs:42`](../../backend/src/Services/Imedto.Backend.Infrastructure/Database/Repositories/ProntuarioRepository.cs). **Não são órfãos** — só compactação por arquivo.

---

## 4. Repositórios EF/Dapper não injetados

83 classes `*Repository` / `*QueryRepository` em `Infrastructure/Database/Repositories/`.

| Diff | Resultado |
|------|-----------|
| disco ∖ registrado | **vazio** |
| registrado ∖ disco | só nomes de **interface** (`IXxxRepository`) — esperado |

Todas as 83 classes concretas estão registradas em `Container.cs` (API) ou `Container.cs` (Infrastructure). Isso inclui `EstabelecimentoIaSettingsRepository`, `AuditDeleteAttemptRepository`, `IdempotencyRepository` e `JobAgendadoRepository`, que à primeira vista parecem "soltos" mas são consumidos por jobs/interceptors/filters.

---

## 5. Services de Infra não usados

26 classes `*Service`/`*Bridge`/`*Job`/`*HostedService`/`*Interceptor`/`*Factory`/`*Bus` em `Infrastructure/`. Cada uma foi cruzada com `grep -rln "\b<nome>\b"` em todo o backend (excluindo o próprio arquivo).

| Service | Refs externas | Status |
|---------|---------------|--------|
| `AppDbContextFactory` | **0** | **Falso positivo** — implementa `IDesignTimeDbContextFactory<AppDbContext>`, é descoberto por reflection pelo tooling do `dotnet ef` ([CLAUDE.md §Migrations](../../CLAUDE.md)). **Manter.** |
| Todos os outros 25 | ≥1 | Registrados ou referenciados normalmente. |

**Nada para remover.**

---

## 6. Endpoints de controller que não chamam handler nenhum

Quatro controllers não usam `ICommandBus` nem `IRequestBus`:

| Controller | Ação(ões) | Como funciona | Risco | Comentário |
|------------|-----------|---------------|-------|------------|
| [`AdminController.cs`](../../backend/src/Services/Imedto.Backend.API/Controllers/AdminController.cs) | `POST estabelecimentos/{id}/reset` | Injeta `IAdminResetService` direto | **alto** — não tocar | Função admin de DEV, gated por `IWebHostEnvironment.IsDevelopment()`. |
| [`IaController.cs`](../../backend/src/Services/Imedto.Backend.API/Controllers/IaController.cs) | `POST sugestao-secao` | Injeta `IIaService` (decorator) | **alto** | Endpoint passa-thru para o serviço de IA — CQRS aqui seria over-engineering (não há aggregate). Deixar como está. |
| [`TenantController.cs`](../../backend/src/Services/Imedto.Backend.API/Controllers/TenantController.cs) | `GET contexto` | Injeta `ICurrentTenantAccessor` | **alto** | Lê estado da request — não há domínio para consultar. Manter. |
| [`EstabelecimentoIaSettingsController.cs`](../../backend/src/Services/Imedto.Backend.API/Controllers/EstabelecimentoIaSettingsController.cs) | `GET`, `PUT` | Injeta `IEstabelecimentoIaSettingsRepository` direto e chama método estático `EstabelecimentoIaSettings.CriarPadrao(...)` no controller | **médio** — **violação de CQRS** | Único controller que **bypassa o bus** sem justificativa arquitetural. **Recomendação Fase 1.x**: criar `ObterEstabelecimentoIaSettingsQuery` + `AtualizarEstabelecimentoIaSettingsCommand` para alinhar com o resto do backend. Não é "deletar", é "refatorar". |

Os outros 30 controllers usam `_cmd`/`_query` (variantes de nome) — todos auditados. `EstabelecimentoProfissionaisController` é uma façade fina sobre `ListarProfissionaisEstabelecimentoQuery` (4 usos do bus em 2 actions).

---

## 7. Achados colaterais (não solicitados, mas relevantes)

1. **Possível DI inconsistente em `ReceitaQueryRepository`** ([Container.cs:349](../../backend/src/Services/Imedto.Backend.API/Container.cs)): registrado como **Scoped**, enquanto todos os outros `*QueryRepository` são **Singleton**. Justificável se o repo recebe `AppDbContext` ou outro scoped, mas vale conferir — Dapper-only deveria ser Singleton.
2. **`IReceitaPdfService` aponta para `QuestPdfReceitaService` que é "placeholder — Wave 4"** (comment em [Container.cs:350](../../backend/src/Services/Imedto.Backend.API/Container.cs)). Dependência registrada para uma feature ainda não pronta — TODO consciente, não órfã.

---

## Apêndice — Comandos usados para a auditoria

```bash
# Classes Handler em disco
grep -rhE "^public (sealed )?class [A-Z][A-Za-z0-9_]*(CommandHandler|QueryHandler|QueryHandlers|EventHandler|Handler)\b" \
  Application/ | sed -E 's/.*class ([A-Za-z0-9_]+).*/\1/' | sort -u > disk_classes.txt

# Registros em Container
grep -oE "(AddScoped|AddSingleton|AddTransient|AddHostedService)<[^>]+>" \
  API/Container.cs Infrastructure/Container.cs > registered.txt

# Diff
comm -23 disk_classes.txt registered_sorted.txt   # → órfãos
comm -13 disk_classes.txt registered_sorted.txt   # → registros sem classe

# Eventos
grep -rhE "public (sealed )?(record|class) [A-Z][A-Za-z0-9_]*Event\b" Domain/
grep -oE "bus\.Register<[A-Za-z0-9_]+Event," API/Container.cs

# Commands/Queries
grep -rhE "public (sealed )?(record|class) [A-Z][A-Za-z0-9_]*Command\b" Contracts/
grep -oE "bus\.Register<[A-Za-z0-9_]+Command," API/Container.cs
```
