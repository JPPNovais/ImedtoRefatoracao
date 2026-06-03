# Arquitetura

> **Quando ler**: ao tocar código de backend (.NET, CQRS, handlers, EF, Dapper), código de frontend (Vue, store, service), autenticação (JWT/BFF) ou conexão com o Postgres.
>
> **Quando atualizar**: ao introduzir/alterar bounded context, padrão de DI, fluxo de bus, padrão de auth, padrão de store/service. **Responsabilidade primária: `imedto-business-analyst`** (mantém doc viva como parte da entrega).

---

## Backend — .NET 10 + CQRS + DDD

Solução em 3 pastas lógicas (`Core`, `Services`, `Tests`), 7 projetos alinhados a DDD + CQRS:

- **`Core/Imedto.Backend.SharedKernel`** — interfaces CQRS (`ICommand`, `IQuery<T>`, `IDomainEvent`, `ICommandBus`, `IRequestBus`, `IEventBus`), `Entity` base (com `DomainEvents`), `BusinessException`, `GlobalExceptionFilter`, `UnitOfWorkAttribute` + `IUnitOfWorkFactory` (interface async: `Task CommitAsync()` + `IAsyncDisposable`).
- **`Services/Imedto.Backend.Domain`** — aggregate roots (propriedades `virtual` com `protected set`, fábrica estática tipo `.Criar(...)`), domain events, interfaces de repositório, abstrações de auth.
- **`Services/Imedto.Backend.Contracts`** — commands, queries e DTOs (camada pública).
- **`Services/Imedto.Backend.Application`** — handlers: `*CommandHandler` (scoped), `*QueryHandlers` em plural (singleton, usam repositório Dapper), `*EventHandler` (scoped).
- **`Services/Imedto.Backend.Infrastructure`** — `AppDbContext` (EF Core + Npgsql), configurations em `Database/Configurations/`, `EfUnitOfWorkScope` (transação real via `Database.BeginTransaction`), repositórios EF (escrita) + `*QueryRepository` Dapper (leitura), `LocalJwtAuthService` + `EcdsaJwtTokenIssuer` (auth local), `S3FotoStorageService` + `S3AnexoStorageService` (storage), buses em memória, `AppDbContextFactory` (`IDesignTimeDbContextFactory` para o tooling do `dotnet ef` — lê a connection string `Migrations` do `appsettings.Development.json` do API project).
- **`Services/Imedto.Backend.API`** — controllers, `Program.cs`, **Composition Root** em [backend/src/Services/Imedto.Backend.API/Container.cs](../backend/src/Services/Imedto.Backend.API/Container.cs).
- **`Tests/Imedto.Backend.Test`** — NUnit 4 + Moq.

### Fluxo de requisição e escopo DI (crítico)

Controller recebe DTO → `ICommandBus.Send` ou `IRequestBus.Query`. **Os buses são singleton para manter o registro de handlers, mas resolvem handlers do scope da request atual via `IHttpContextAccessor.HttpContext.RequestServices`** (não usam `CreateScope()` — isso criaria um `AppDbContext` paralelo ao do `UnitOfWorkAttribute` e o commit iria no DbContext errado).

`UnitOfWorkAttribute` injeta `IUnitOfWorkFactory` → `EfUnitOfWorkScope` abre transação no `AppDbContext` scoped da request → action executa → `CommitAsync` chama `SaveChangesAsync` + `transaction.Commit`. Exception → rollback implícito no `DisposeAsync`.

`GlobalExceptionFilter` mapeia `BusinessException` → HTTP 422; qualquer outra → 500.

### Adicionar novo domínio

1. Criar Domain / Contracts / Application / Infrastructure.
2. `EntityTypeConfiguration` em `Infrastructure/Database/Configurations/`.
3. `DbSet<T>` em `AppDbContext`.
4. Registrar handler em `Container.RegistrarHandlers` (commands/events: `AddScoped`; query handlers: `AddSingleton`).
5. Registrar no bus em `Container.RegistrarBuses`.
6. Gerar migration EF + copiar SQL idempotente para `db/migrations/` (aplicado pela pipeline em RDS). Ver [COMANDOS.md](COMANDOS.md).
7. SQL custom (functions, triggers, índices CONCURRENTLY) em `.sql` separado dentro de `db/migrations/`.

### Convenções de código

- Idioma: identificadores, mensagens e comentários em **português** (`CriarProdutoCommand`, `BusinessException("Preço deve ser maior que zero.")`). Cultura `pt-BR` global em `Program.cs`.
- Indentação: 4 espaços.
- `BusinessException` → 422 automático; nunca usar para erros técnicos.
- Aggregates: `virtual` + `protected set` + ctor `protected` + fábrica estática que adiciona `DomainEvent` via `AddDomainEvent`.
- Após salvar o aggregate, handler itera `produto.DomainEvents` → `IEventBus.Publish` → `ClearDomainEvents`.
- Nomes de tabela/coluna no Postgres em `snake_case`; `EntityTypeConfiguration` faz o mapeamento.

---

## Frontend — Vue 3 + TS + Pinia + Design System

- **Padrão BFF**: frontend **nunca** vê tokens — cookies HttpOnly gerenciados pelo backend. Estado de auth no cliente = `{ id, email, roles }`.
- **`httpClient`** ([frontend/src/services/httpClient.ts](../frontend/src/services/httpClient.ts)): axios com `baseURL: "/api"`, `withCredentials: true`. Interceptor de 401 → `POST /api/auth/refresh` uma vez, falha → logout + redirect `/login`.
- **Stores Pinia**: `authStore.init()` em [frontend/src/main.ts](../frontend/src/main.ts) **antes do `app.mount`** (await) para reidratar via `GET /api/auth/me`.
- Convenção: views/stores nunca usam `httpClient` diretamente — toda HTTP passa por `*Service` em [frontend/src/services/](../frontend/src/services/).
- Alias `@/*` → `./src/*`.

> **Nota — Botão "Novo encaixe" em `MeusAtendimentosView`**: a aptidão para criar encaixe no momento atual é verificada preventivamente no front via `agendaService.consultarDisponibilidade(profissional, hoje, hoje)` — checagem de UX para evitar atrito operacional. `Estabelecimento.ValidarPodeAgendar` (backend) **permanece a fonte da verdade**: qualquer criação de encaixe ainda passa por `CriarAgendamentoCommandHandler` e retorna 422 se o estabelecimento estiver fechado. O front é espelho; o backend é autoritativo.

> Detalhes de UI, design system, componentes e premissas de produto em [DESIGN.md](DESIGN.md).

---

## Autenticação (BFF + LocalJwt)

1. `POST /api/auth/login` → `LocalJwtAuthService` valida e-mail/senha contra `auth_credenciais` (BCrypt + pepper), emite access token via `EcdsaJwtTokenIssuer` (ECDSA P-256 — chaves em `Auth:Jwt:PrivateKeyPem`/`PublicKeyPem`) e cria refresh token em `auth_refresh_tokens`. Backend seta dois cookies HttpOnly (`access-token` path `/api`, `refresh-token` path `/api/auth/refresh`) e retorna `{ usuario }` (+ `accessToken` em dev).
2. Middleware `AddJwtBearer` valida tokens **ES256** com a chave pública local (`Auth:Jwt:PublicKeyPem`) — não há JWKS remoto.
3. `OnMessageReceived` lê o cookie primeiro, com fallback para header `Authorization: Bearer` (Swagger/testes).
4. Requer `Microsoft.AspNetCore.Authentication.JwtBearer >= 10.0` — versões <10 não suportam ES256 nativamente.
5. Confirmação de e-mail, reset de senha e convite usam tokens em `auth_email_tokens` (TTL 24h confirm, 1h reset, 7d invite). Para testes unitários, usar Moq de `IAuthService`.

---

## Área Admin Global

Módulo separado para operação interna do Imedto (não é tenant/usuário final). Acessa `/api/admin/*` e `/admin/*`.

### Backend

- **`ImedtoAdmin`** — aggregate root em `Domain.Admin`. Sem `estabelecimento_id` — é global.
- **`ImedtoAdminTokenIssuer`** — emite JWT admin com claims `imedto_admin = "true"`, `sub`, `email`. Nunca carrega `estabelecimento_id`. Usa a mesma chave ECDSA P-256 do app (`Auth:Jwt:PrivateKeyPem`).
- **`ImedtoAdminAuditWriter`** — caminho único de auditoria. Salva via `db.SaveChangesAsync()` direto (independente de UnitOfWork). Captura IP por X-Forwarded-For > RemoteIpAddress.
- **`AdminAuthController`** — endpoints em `/api/admin/auth/*` (login, refresh, logout, me, change-password). Cookies: `admin-access-token` (path `/api/admin`) e `admin-refresh-token` (path `/api/admin/auth/refresh`).
- **Políticas de autorização**:
  - `ImedtoAdmin` — requer `imedto_admin = "true"` **e** ausência de `must_reset_password = "true"`.
  - `ImedtoAdminChangePassword` — requer `imedto_admin = "true"` (permite `must_reset_password`).
- **`AdminBlindagemFilter`** — filtro MVC global. Se rota não começa com `/api/admin/` e JWT tem `imedto_admin = "true"` → 403. Impede que admin acesse endpoints de tenant.
- **`SeedAdminCommand`** — CLI interceptado antes de `WebApplication.Build`. Uso: `dotnet run -- seed-admin --email <email>`. Cria admin com senha temporária de 20 chars e `force_password_reset = true`.
- **`AdminSenhaPolicy`** — dev: ≥ 6 chars. Prod: ≥ 10 chars + maiúscula + minúscula + número + especial.
- **Refresh token admin** — TTL 2h (mais curto que usuário). Armazenado como hash SHA-256 em `imedto_admin_refresh_tokens` — nunca o token cru.

### Frontend

Módulo autocontido em `frontend/src/modules/admin/`. **Zero import cruzado** com outras stores/services do app principal — exceto `@/components/ui/*` e `@/utils/*`.

- **`adminApi.ts`** — axios isolado com `baseURL: /api/admin`. Interceptor de 401: tenta refresh automático, falha → limpa sessão + redirect `/admin/login`.
- **`adminAuthStore.ts`** — Pinia store `adminAuth`. Reidrata via `GET /api/admin/auth/me`. Timer de inatividade de 15min (logout automático).
- **Router guard** — embutido no `beforeEach` global: rotas `/admin/*` são interceptadas antes das rotas normais. Redireciona para `/admin/login` se não autenticado; para `/admin/change-password` se `mustResetPassword = true`.

### Configurações Globais (`ImedtoConfig` — Wave 2)

- Tabela `imedto_config` com chaves no formato `secao.nome` (ex: `trial.dias_padrao`).
- Tipos suportados: `numerico`, `texto`, `email`, `toggle`.
- **`IConfigGlobalReader`** (singleton em `Domain.Admin`) — lê via Dapper + `IMemoryCache` (TTL 60s, prefixo `imedto_config:`). Invalida cache após mutação. Fallback ao valor default se chave ausente ou inválida.
- CRUD admin em `/api/admin/configs` (GET lista agrupada por seção, PUT atualiza com motivo ≥10 chars + audit).
- `IniciarTrialAoCriarEstabelecimentoHandler` lê `trial.dias_padrao` via `IConfigGlobalReader` (fallback 14 dias).

### Catálogos Globais (live-link via `EhPadraoSistema=true` — Wave 4)

Wave 2 entregou tabelas paralelas (`imedto_modelo_prontuario_global`, `imedto_variavel_pool_global`, `imedto_regiao_anatomica_global`) com modelo de cópia (tenant importa, edita independente). Wave 4 (2026-05-30, briefing `planejamentos/2026-05-30_004_admin-global-wave4-catalogos-livelink.md`) descobriu que o sistema **já tinha live-link nativo** via flag `EhPadraoSistema=true` nas tabelas legado — tabelas paralelas viraram código órfão e foram dropadas.

**Modelo atual — live-link puro:**

| Entidade | Tabela legado | Endpoint admin | Como tenant consome |
|---|---|---|---|
| Modelo de prontuário | `modelo_de_prontuario` (`eh_padrao_sistema=true`, `estabelecimento_id=NULL`) | `/api/admin/modelos-globais` | Queries do tenant fazem `WHERE (eh_padrao_sistema=true OR estabelecimento_id=@X)` — padrão-sistema aparece automaticamente |
| Variável pool | `prontuario_variaveis_pool` (`eh_padrao_sistema=true`, `estabelecimento_id=NULL`) | `/api/admin/variaveis-globais` (filtro `?categoria=`) | Idem — categorias clínicas via enum `TipoVariavelPool` (Alergia/Medicamento/Doenca/Cirurgia/Droga/RelacaoFamiliar/Expectativa/AtividadeFisica) |
| Região anatômica | `regioes_anatomicas_catalogo` (sem `estabelecimento_id` — global por construção, 144 registros hierárquicos) | `/api/admin/regioes-globais` | Exame físico consome direto da tabela; admin edita via tree view |

**Consequências:**
- Mudança feita pelo admin reflete em TODOS os tenants imediatamente (sem importação, sem cópia).
- Endpoints `*/importar-do-global/{id}` removidos.
- Aba "Templates do sistema" no tenant removida — padrão-sistema aparece direto nas listagens normais.
- Operações admin (criar/editar/inativar) gravam com `EhPadraoSistema=true` + `EstabelecimentoId=NULL`.
- Soft delete via `Ativo=false` (não exclui físico — preserva audit + histórico do tenant).

**Hierarquia de regiões anatômicas** (`regioes_anatomicas_catalogo`):
- Estrutura: `codigo` (PK lógico) + `pai_codigo` (FK self) + `nivel` (1-3) + `vista` (anterior/posterior).
- Validações ao criar/editar subgrupo: `nivel = pai.nivel + 1` e `vista = pai.vista` (BusinessException 422).
- Exclusão: só folhas (sem filhos). Região com filhos → 422 `"Esta região tem subgrupos. Inative em vez de excluir, ou remova os subgrupos primeiro."`.

**Frontend admin:**
- Views em `modules/admin/views/`: `ModelosGlobaisListView`, `ModelosGlobaisFormView`, `VariaveisGlobaisListView`, `VariaveisGlobaisFormView`, `RegioesGlobaisListView`, `RegioesGlobaisFormView`.
- Componente novo: `modules/admin/components/regioes/RegiaoTreeView.vue` (tree view recursivo expand/collapse por vista → nível 1 → 2 → 3).
- Stores: `modelosGlobaisStore`, `variaveisGlobaisStore`, `regioesGlobaisStore` (este último carrega árvore completa).
- Rotas sob `/admin/modelos-globais`, `/admin/variaveis-globais`, `/admin/regioes-globais`.

### Dashboard Admin (Wave 6)

`Application/Admin/Dashboard/` — 4 query handlers (singletons) servindo `/api/admin/dashboard/*`:

- `ObterKpisDashboardAdminQueryHandler` → KPIs: estabelecimentos (ativos/inativos), admins ativos, trials em andamento + expirando em 7 dias, assinaturas vigentes + gratuitas.
- `ObterCrescimentoMensalDashboardAdminQueryHandler` → 12 pontos fixos (mês corrente + 11 anteriores) de novos estabelecimentos por mês; meses zerados preenchidos via `generate_series`.
- `ObterAlertasDashboardAdminQueryHandler` → trials expirando em 7 dias (LIMIT 10) + estabelecimentos ativos sem assinatura vigente (LIMIT 10 + total absoluto).
- `ListarAuditLogDashboardAdminQueryHandler` → feed paginado de `imedto_admin_audit_log` com filtros opcionais (ação, admin_id, período preset: `hoje`/`7d`/`30d`/`90d`/`todos`).

Read repository: `Infrastructure/Admin/QueryRepositories/DashboardAdminQueryRepository.cs` (Dapper sobre `AppReadConnectionString`). Sem joins em `pacientes`/`prontuarios` — só metadados de tenant/admin (LGPD: zero PII de paciente).

Política: `ImedtoAdmin` no controller. **Leitura do dashboard não gera linha em `imedto_admin_audit_log`** (Wave 1 CA16 — só ações sensíveis com efeito de escrita ou exposição de dado sensível geram audit).

Cache: 60s no store Pinia frontend (não no backend — Dapper direto). Botão "Atualizar" força refresh ignorando cache.

Gráfico: SVG inline em `modules/admin/components/dashboard/CrescimentoChart.vue` (sem lib externa). Usa tokens HSL do design system (`--primary`, `--background`, `--muted-foreground`).

Índices utilizados (existentes desde Wave 1): `ix_imedto_admin_audit_log_acao_criado`, `ix_imedto_admin_audit_log_admin_criado`, `ix_imedto_admin_audit_log_criado`, `ix_imedto_admin_audit_log_tenant_criado`.

### Audit admin global — política de retenção (Wave 7)

`imedto_admin_audit_log` registra apenas ações com valor forense real. TTL por categoria:

| Categoria | Exemplos | Retenção | Motivo |
|---|---|---|---|
| Acesso (login/logout/leitura) | LOGIN_OK, LOGOUT, ABRIR_DETALHE_TENANT | **Não registrado** | Alto volume, baixo valor forense isolado. Wave 7 cortou esses 3 audits no código. |
| Segurança | LOGIN_FAIL | 365 dias | Forense de tentativa de invasão |
| LGPD reveal | REVELAR_CPF_DONO | 730 dias | Compliance de acesso a PII de dono (Art. 11) |
| Mutação contratual/financeira | CRIAR_*, DESATIVAR_*, TROCAR_*, etc. | 730 dias | Compliance LGPD — quem fez o quê |
| Catálogo padrão-sistema | CRIAR_MODELO_*, ATUALIZAR_REGIAO_*, etc. | 365 dias | Raramente disputado; estado atual fica no catálogo |

Fonte de verdade: `Imedto.Backend.Domain.Admin.AuditLogRetencao` (mapa estático `PorAcao`). Ação nova sem entrada no mapa cai no default de **365 dias** — conservador mas nunca lixo eterno.

Job `limpar-audit-admin` (1×/dia, batches de 10.000 linhas) aplica DELETE respeitando o mapa. Cleanup retroativo de 158 linhas de ruído realizado via `db/migrations/20260530210000_limpar_audit_admin_retroativo.sql`.

### Isolamento e cross-blindagem (CA8 + CA9)

- JWT de usuário comum em `/api/admin/*` → rejeitado pela policy `ImedtoAdmin` (sem claim `imedto_admin`) → 403.
- JWT admin em rota normal → rejeitado por `AdminBlindagemFilter` → 403.
- As duas direções são garantidas sem sobreposição de roles.

---

## Bounded Context: AssinaturaDigital (briefing 2026-06-01_001)

Permite que o médico prescritor assine digitalmente receitas com validade jurídica ICP-Brasil (CFM Res. 2.299/2021 + Lei 14.063/2020), usando certificado em nuvem sem hardware físico.

### Abstração de provedor

`IAssinaturaDigitalProvider` (Domain) — interface única que isola a integração com qualquer provedor ICP-Brasil:

```csharp
Task<DisparoAssinaturaResult> DispararAssinaturaAsync(receita, medicoId, ct);
Task<ValidacaoCallbackResult> ValidarCallbackAsync(payload, ct);
```

MVP implementa `BirdIdAssinaturaProvider`. Novo provedor (VIDaaS etc.) entra como nova implementação registrada por nome no container — zero mudança nos handlers. Seleção via config `AssinaturaDigital:Provedor`.

### Máquina de estados da receita

```
NaoAssinada → AssinaturaPendente → AssinadaIcp
                               ↘ FalhaAssinatura  (médico recusou PUSH)
                               ↘ AssinaturaExpirada  (job: pendente > 30 min)
```

`AssinaturaExpirada` e `FalhaAssinatura` permitem re-disparo (transicionam de volta para `AssinaturaPendente`). `AssinadaIcp` é estado terminal imutável.

### Fluxo assíncrono (polling + webhook)

A assinatura em nuvem é inerentemente assíncrona: o BirdID dispara PUSH no celular do médico, que confirma em segundos ou minutos.

1. Frontend chama `POST /api/receitas/{id}/assinar` → recebe `202 Accepted` com `{ status: 'AssinaturaPendente' }`.
2. Frontend inicia polling leve: `GET /api/receitas/{id}/status-assinatura` a cada **4 segundos**.
3. BirdID chama `POST /api/webhooks/assinatura/{receita_id}` (callback externo) → backend valida HMAC do payload, atualiza status, persiste PDF assinado no S3.
4. Próxima rodada de polling pega o status atualizado. Frontend para o polling ao resolver.
5. Timeout frontend: **5 minutos** — exibe mensagem orientativa sem loop eterno.

**Webhook sem `[Authorize]`**: único endpoint no sistema sem autenticação JWT de usuário. Segurança é feita pelo handler via validação de assinatura HMAC/JWT do payload do BirdID. Receita de tenant inativo → descarte silencioso (sem mutação, resposta 200 para evitar retry infinito do provedor).

### Job: expirar-assinaturas-pendentes

`ExpirarAssinaturasPendentesJob` — `Nome = "expirar-assinaturas-pendentes"`, intervalo `3600` (1×/hora). Marca como `AssinaturaExpirada` todas as receitas com `status_assinatura = 'AssinaturaPendente'` e `assinatura_solicitada_em < UtcNow - 30 min`. Registro em `assinatura_audit_log` com `acao = 'EXPIRAR_PENDENTE'`.

### Formato e validade

- **PAdES AD_RB** (sem carimbo do tempo) no MVP — atende CFM Res. 2.299/2021. PDF assinado deve passar no `validar.iti.gov.br`.
- **AD-RT** (com carimbo) entra em Fase 2, se buscar certificação SBIS.
- Vínculo do certificado: **por médico (usuário)**, não por estabelecimento — médico que atua em N tenants vincula uma vez.

---

## Conexão Postgres (RDS)

- Connection string normal Npgsql em `ConnectionStrings:Default` (runtime) e `ConnectionStrings:Migrations` (autoria de migrations via `dotnet ef`).
- Em prod, host/senha vêm do AWS SSM Parameter Store (`/imedto/dev/db-host`, `/imedto/dev/db-password`); a EC2 lê via IAM role.
- Em dev, o valor fica em `appsettings.Development.json` (gitignored).
- O `AppDbContextFactory` usa apenas `ConnectionStrings:Migrations` para o tooling do `dotnet ef`.

> Detalhes de infra do RDS (endpoint, SG, backup) em [INFRA.md](INFRA.md). Comandos de migration em [COMANDOS.md](COMANDOS.md).
