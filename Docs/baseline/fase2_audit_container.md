# Fase 2 — Auditoria do Composition Root e Bootstrap

**Data**: 2026-05-02
**Escopo**: `backend/src/Services/Imedto.Backend.API/Container.cs`, `backend/src/Services/Imedto.Backend.API/Program.cs` e dependências de DI relacionadas (`Infrastructure/Container.cs`, buses, hosted services).

**Estado pré-audit**: build verde (0 errors, 742 warnings de analyzer estilo CA1848/CA1305, todos pré-existentes), 236 testes passando, 0 vulnerabilidades NuGet, 0 handlers órfãos.

---

## Resumo executivo

Composition root está **saudável**. Nenhum captive dependency, nenhum registro duplicado funcional, nenhum handler órfão, nenhum service usado por controller sem registro. JWT/Auth coerentes com BFF descrito no CLAUDE.md.

Observações são **majoritariamente baixa severidade** (observabilidade/clareza). Nada que justifique mudança em Fase 2 antes de seguir para Fase 3.

Build pós-audit: verde (`dotnet build Imedto.Backend.sln` — 0 errors).

---

## 1. Lifetime mismatch / captive dependencies

### 1.1 — Buses Singleton resolvendo handlers Scoped (correto, intencional)

**Status**: OK — não é captive.

`MemoryCommandBus`, `MemoryRequestBus`, `MemoryEventBus` são Singleton mas usam `IHttpContextAccessor.HttpContext.RequestServices` para resolver os handlers no scope da request atual (`Bus/Memory*Bus.cs:41`, `:39`, `:50`). Isso é **exatamente** o padrão documentado no CLAUDE.md e o motivo de ser singleton (mantém o registro de handlers em cache).

Único cuidado: handlers de **eventos** publicados fora de uma request HTTP (ex: jobs do `JobScheduler` ou `AutomacaoJob`) cairiam no `_rootProvider` (fallback em `MemoryEventBus.cs:50`) e tentariam resolver handler Scoped do root provider → exceção em runtime. **Como o JobScheduler já abre seu próprio scope antes de invocar handlers (resolve via `IServiceProvider` do scope, não via bus)**, isso só seria problema se algum job no futuro chamar `IEventBus.Publish` diretamente sem ter HttpContext. Sugiro acompanhar isso em uma feedback memory para futura defesa.

### 1.2 — Singletons puros injetando deps Scoped: **nenhum encontrado**.

Auditei todos os `*QueryHandlers` registrados como Singleton em `Container.cs:240-590`. Padrão consistente: cada handler Singleton injeta apenas o `*QueryRepository` correspondente (também Singleton). Os QueryRepositories Singleton dependem só de `AppReadConnectionString` (Singleton) — **sem captive**.

Amostragem verificada (`grep` de `private readonly` em ~15 query handlers Singleton): `ListarPacientesQueryHandlers`, `ListarUnidadesQueryHandlers`, `ListarMeusEstabelecimentosQueryHandlers`, `DashboardQueryHandlers`, `RelatorioFinanceiroQueryHandler`, `RelatorioPessoasQueryHandler`, `ConsultarDisponibilidadeQueryHandlers` (2 repos Singleton), `ListarMeusConvitesQueryHandlers`, `ListarNotificacoesQueryHandlers`, `PreviewOrcamentoQueryHandler` (`AppReadConnectionString` Singleton), `ExportarMeusDadosLgpdQueryHandlers`. Todos limpos.

### 1.3 — Receita e Exame Físico Scoped (intencional, já documentado)

`Container.cs:351, 361-364, 477, 401` — handlers de Receita, Procedimento e Exame Físico são Scoped porque dependem de `IProntuarioAcessoLogService` (Scoped, audit LGPD). `ReceitaQueryRepository` é a única `*QueryRepository` Scoped (`Container.cs:351`) — alinhado e comentado em código (`linhas 349-350`).

### 1.4 — `IIaService` decorator Scoped (correto)

`Container.cs:224-234` — registrado como Scoped via factory. Injeta `IVinculoRepository` (Scoped), `IModeloPermissaoRepository` (Scoped), `IHttpContextAccessor` (Singleton, OK), `IConfiguration` (Singleton, OK). Sem captive.

### 1.5 — `IEmailService` Scoped via factory (correto)

`Container.cs:518-525` — `ResendEmailService`/`NoOpEmailService` resolvidos via `ActivatorUtilities.CreateInstance(sp, ...)`, então respeitam o scope do `sp` recebido pelo factory. OK.

---

## 2. Registros redundantes

**Nenhum encontrado** (cross-file API + Infra). Único tipo registrado N×: `IJobHandler` (5×), intencional — colection injection no `JobScheduler`.

Comando usado:
```bash
grep -hnE "services\.Add(Scoped|Singleton|Transient)" Container.cs Infrastructure/Container.cs \
  | sed -E 's/.*<([^>,]+)(,.*)?>.*/\1/' | sort | uniq -c | awk '$1 > 1'
# → única ocorrência: 5 IJobHandler
```

Bus.Register(): `NotificarSolicitacaoRespondidaHandler` aparece 2× no `IEventBus` (linhas 826, 827) porque escuta `SolicitacaoVinculoAprovadaEvent` E `SolicitacaoVinculoRecusadaEvent`. Correto.

`ObterProcedimentoQueryHandlers` aparece em 2 `bus.Register` (linhas 765, 766) — uma classe implementando 2 `IRequestHandler<>`, AddScoped 1× em 477. Correto.

`ObterExameFisicoQueryHandlers` aparece em 4 `bus.Register` (linhas 803-806) — implementa 4 queries; AddScoped 1× em linha 401. Correto.

---

## 3. Registros faltando (handlers usados sem `Add*` no Container)

**Nenhum encontrado.**

Comparação: `199 handlers definidos em Application` ∩ `202 handlers referenciados em Container`. Diff de classes definidas em Application **não** registradas: `0` (após filtrar `*Handler` puros — todos os 8 event handlers com sufixo `Handler` aparecem 2× no Container = AddScoped + bus.Register).

---

## 4. Ordem de middlewares (Program.cs)

Sequência atual (`Program.cs:378-444`):

```
1. UseExceptionHandler         ← OK (primeiro)
2. UseSwagger / UseSwaggerUI   ← OK
3. MapGet "/" redirect         ← OK
4. Security headers (não-Dev)  ← OK
5. UseSerilogRequestLogging    ← OBS: ver §4.1
6. UseCors("CorsPolicy")       ← OK
7. UseRateLimiter()            ← OK (após CORS — preflight não é rate-limited)
8. UseAuthentication()         ← OK
9. UseAuthorization()          ← OK
10. MapControllers + MapHub    ← OK
11. MapHealthChecks            ← OK (anônimo)
```

### 4.1 — `UseSerilogRequestLogging` antes de `UseAuthentication` (BAIXA)

**Severidade**: baixa (observabilidade).
**Linha**: `Program.cs:438`.
**Impacto**: o log de request gerado pelo Serilog não consegue enriquecer com `User.Identity` (claims) — fica anônimo em todos os logs.
**Recomendação Microsoft**: chamar `UseSerilogRequestLogging` **depois** de `UseAuthentication`, idealmente logo antes de `MapControllers`.
**Não corrigi sozinho** porque envolve reordenar middleware (você pediu autorização explícita pra isso).

### 4.2 — Demais ordens

`UseExceptionHandler` primeiro: ✅ correto, captura tudo.
`UseCors` antes de `UseRateLimiter`: ✅ correto (preflight passa por CORS sem virar 429).
`UseAuthentication` antes de `UseAuthorization`: ✅ correto.
`UseRateLimiter` antes de `UseAuthentication`: ✅ — proteção a brute force em `/auth/login` (rate limit deve ser aplicado **antes** da tentativa cara de validar token/JWKS).

---

## 5. Configuração JWT

`Program.cs:67-136`. **Coerente com a arquitetura BFF descrita no CLAUDE.md.**

| Item | Status | Observação |
|---|---|---|
| `Authority` (Supabase) | ✅ | `Program.cs:73` — discovery automática de JWKS, ES256 nativo |
| `RequireHttpsMetadata = true` | ✅ | `:74` — força HTTPS no JWKS endpoint |
| `MapInboundClaims = false` | ✅ | `:78` — preserva `sub` original do Supabase |
| `ValidateIssuer / Audience / Lifetime / SigningKey` | ✅ | `:82-89` — todos `true` |
| `ClockSkew = TimeSpan.Zero` | ✅ | `:88` — apropriado para BFF (token de curta duração) |
| `OnMessageReceived` cookie → header → query (somente `/hubs`) | ✅ | `:93-122` — exatamente o documentado |
| Authorization vs Bearer fallback | ✅ | `:104-109` |
| Query string só em `/hubs/*` | ✅ | `:114-120` — restrição explícita evita leak de token em URLs REST |
| Logging de falha (`OnAuthenticationFailed`) | ⚠️ baixa | `:130-132` loga o **header Authorization inteiro** como "Token kid" — embora o JWT seja opaco e não contenha PII, é melhor logar apenas hash/prefixo do token. Ver §9.1 |

---

## 6. Cultura pt-BR

✅ `Program.cs:31-33` — `DefaultThreadCurrentCulture` e `DefaultThreadCurrentUICulture` setados no boot, antes de qualquer registro DI ou middleware. Correto.

---

## 7. Ordem de hosted services / startup

3 hosted services registrados:

| Hosted | Registro | Dependência implícita |
|---|---|---|
| `JobScheduler` | `Container.cs:198-199` (Singleton + factory) | Banco — espera 10s no boot antes da 1ª rodada |
| `AutomacaoJob` | `Container.cs:315` | Banco — espera 30s no boot |
| `SeedPlanosHostedService` | `Container.cs:202` | Banco (StartAsync síncrono) |

`SeedPlanos` roda **no `StartAsync`**, então bloqueia o pipeline de hosted services até terminar (ou falhar — tem try/catch). `JobScheduler` e `AutomacaoJob` fazem o trabalho em `ExecuteAsync` (background), então o boot não fica preso.

**Risco baixo**: `SeedPlanosHostedService.StartAsync` (`SeedPlanosHostedService.cs:31-66`) executa antes do `JobScheduler.ExecuteAsync` (background), e antes do scheduler começar a iterar a fila — então o seed de planos ocorre antes do `IniciarTrialAoCriarEstabelecimentoHandler` ser invocado pela primeira vez. OK.

**Não há ordering explícito** entre `JobScheduler`, `AutomacaoJob` e `SeedPlanos` — `IHostedService` no .NET roda em ordem de registro. Hoje a ordem é: `JobScheduler` → `SeedPlanos` → `AutomacaoJob`. Sem dependência crítica entre eles, está OK.

---

## 8. CORS / rate limit / antiforgery

### 8.1 — CORS

`Program.cs:209-220`. Configurado corretamente para BFF com cookies HttpOnly:
- `AllowCredentials()` ✅
- `SetIsOriginAllowed` (origins fixos + regex patterns para previews) ✅ — não usa `AllowAnyOrigin` (que seria incompatível com `AllowCredentials`)
- `WithExposedHeaders("Content-Disposition")` ✅ (necessário para downloads)

### 8.2 — Rate limit

`Program.cs:226-286`. 3 policies (`auth-login` 5/min, `auth-refresh` 10/min, `auth-sensitive` 3/min) particionadas por IP (X-Forwarded-For prioritário). Hash SHA256 do IP no log (LGPD ✅). Nenhum problema.

### 8.3 — Antiforgery

**Não configurado**. Como o backend é API JSON consumida via `withCredentials: true` (não submete forms HTML), não é estritamente necessário. **Mitigação contra CSRF vem de**:
- `SameSite` do cookie HttpOnly (não auditado aqui — verificar em `SupabaseAuthService` na Fase 3 quando tocar autenticação),
- CORS com `AllowCredentials` + lista explícita de origens.

**Recomendação BAIXA**: documentar a decisão de não usar `AddAntiforgery` em ADR para evitar PR futuro adicionar sem necessidade.

---

## 9. Logging / PII

### 9.1 — `OnAuthenticationFailed` loga header Authorization (BAIXA)

`Program.cs:130-132`:
```csharp
"Token kid: {Kid}", ctx.Request.Headers.Authorization.ToString()
```
O parâmetro chama-se `Kid` mas envia o header `Authorization` inteiro (`"Bearer eyJ..."`). JWTs **não são PII em si** (Supabase JWT contém `sub`, `email`, `aud`, `exp`), mas o `email` no payload **é PII**. Mesmo Base64, fica logado em texto plano.

**Recomendação**: logar apenas os primeiros 16 chars do token (suficiente para correlacionar com auditoria) ou parsear o JWT só para extrair o `kid` do header. Não corrigi sem autorização (mexer em logging de auth pode esconder problemas reais durante diagnóstico).

### 9.2 — `RemovePIIEnricher` ativo (✅)

`Program.cs:47` — enricher Serilog mascara cpf/email/telefone em mensagens. Bom.

### 9.3 — `UseSerilogRequestLogging` template default

✅ — não loga body/query/headers por padrão. Seguro para LGPD.

---

## 10. Dispose / IAsyncDisposable

Nenhum problema. Padrões corretos:

- `JobScheduler` (Singleton, BackgroundService) — `IDisposable` herdado de `BackgroundService`, `_pollInterval` é `TimeSpan` (sem recurso).
- `MemoryCommandBus/EventBus/RequestBus` (Singleton) — não detêm recursos diretamente, só o `Dictionary` em memória.
- `SoftDeleteInterceptor` (Scoped) — recebe `AppDbContext` via DI, sem dispose manual.
- `AppDbContext` é Scoped (default `AddDbContext`), dispose automático no fim do scope.

`AppReadConnectionString` (Singleton) é só wrapper de string, sem recurso.

---

## Lista de problemas (resumo)

| # | Severidade | Item | Arquivo:linha |
|---|---|---|---|
| 1 | BAIXA | `UseSerilogRequestLogging` antes de `UseAuthentication` — logs sem User claims | `Program.cs:438` |
| 2 | BAIXA | `OnAuthenticationFailed` loga header Authorization inteiro como "Kid" | `Program.cs:130-132` |
| 3 | BAIXA (info) | Sem antiforgery configurado — depender de CORS+SameSite (decisão a documentar) | `Program.cs` (ausência) |
| 4 | INFO | Buses caem para `_rootProvider` quando publicados fora de HTTP context — armadilha futura se algum job invocar `IEventBus.Publish` direto sem CreateScope() | `Bus/MemoryEventBus.cs:50`, `MemoryCommandBus.cs:41`, `MemoryRequestBus.cs:39` |

---

## Correções aplicadas

**Nenhuma**. Não havia registros duplicados funcionais, nem handlers órfãos, nem comentários ausentes em decisões "estranhas mas intencionais" (todos já estão comentados — Receita/ExameFisico/Procedimento Scoped e `JobScheduler` Singleton+factory já têm comentário explicativo).

Build verificado pós-audit (sem mudanças): `dotnet build Imedto.Backend.sln` → **0 errors**, 742 warnings (todos pré-existentes — analyzers CA1848/CA1305/RCS1139).

---

## Decisões pendentes (precisam de você)

1. **Reordenar `UseSerilogRequestLogging` para depois de `UseAuthentication`?** Mudança de 1 linha, melhora observabilidade (log com user claims). Risco baixo. Recomendo SIM.
2. **Trocar log do header Authorization por hash ou prefixo?** Mudança de 1-2 linhas em `OnAuthenticationFailed`. Risco baixo. Recomendo SIM (LGPD).
3. **Documentar decisão de não usar Antiforgery?** Pode virar uma seção curta no `BASELINE.md` ou um ADR — não é mudança de código.
4. **Incluir guard-rail no `JobScheduler`/`AutomacaoJob` para sempre criar scope antes de Publish?** Hoje funciona porque os jobs já criam scope; risco é cultural (próximo dev pode esquecer). Pode entrar em CLAUDE.md como nota "ao adicionar hosted service novo, sempre `CreateAsyncScope` antes de invocar bus".

Nenhum item bloqueia Fase 3.
