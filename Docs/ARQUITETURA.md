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

> Detalhes de UI, design system, componentes e premissas de produto em [DESIGN.md](DESIGN.md).

---

## Autenticação (BFF + LocalJwt)

1. `POST /api/auth/login` → `LocalJwtAuthService` valida e-mail/senha contra `auth_credenciais` (BCrypt + pepper), emite access token via `EcdsaJwtTokenIssuer` (ECDSA P-256 — chaves em `Auth:Jwt:PrivateKeyPem`/`PublicKeyPem`) e cria refresh token em `auth_refresh_tokens`. Backend seta dois cookies HttpOnly (`access-token` path `/api`, `refresh-token` path `/api/auth/refresh`) e retorna `{ usuario }` (+ `accessToken` em dev).
2. Middleware `AddJwtBearer` valida tokens **ES256** com a chave pública local (`Auth:Jwt:PublicKeyPem`) — não há JWKS remoto.
3. `OnMessageReceived` lê o cookie primeiro, com fallback para header `Authorization: Bearer` (Swagger/testes).
4. Requer `Microsoft.AspNetCore.Authentication.JwtBearer >= 10.0` — versões <10 não suportam ES256 nativamente.
5. Confirmação de e-mail, reset de senha e convite usam tokens em `auth_email_tokens` (TTL 24h confirm, 1h reset, 7d invite). Para testes unitários, usar Moq de `IAuthService`.

---

## Conexão Postgres (RDS)

- Connection string normal Npgsql em `ConnectionStrings:Default` (runtime) e `ConnectionStrings:Migrations` (autoria de migrations via `dotnet ef`).
- Em prod, host/senha vêm do AWS SSM Parameter Store (`/imedto/dev/db-host`, `/imedto/dev/db-password`); a EC2 lê via IAM role.
- Em dev, o valor fica em `appsettings.Development.json` (gitignored).
- O `AppDbContextFactory` usa apenas `ConnectionStrings:Migrations` para o tooling do `dotnet ef`.

> Detalhes de infra do RDS (endpoint, SG, backup) em [INFRA.md](INFRA.md). Comandos de migration em [COMANDOS.md](COMANDOS.md).
