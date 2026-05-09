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

## Overview

Monorepo do Imedto (refactor do legado Vue+Supabase para arquitetura CQRS):
- `backend/` — API .NET 10, DDD + CQRS, BFF de autenticação, EF Core (escrita) + Dapper (leitura)
- `frontend/` — Vue 3 + TypeScript + Vite + Pinia
- `db/migrations/` — migrations SQL aplicadas em RDS pela pipeline de deploy

Stack de runtime: **AWS RDS Postgres** (banco), **LocalJwt + ECDSA P-256** (auth — implementado no backend, sem provedor externo), **AWS S3** (storage de fotos e anexos). **Toda regra de negócio vive no backend** — nada de RPCs, triggers ou edge functions que implementem lógica.

Detalhes de conexão e credenciais ficam em `appsettings.Development.json`/`.mcp.json` (ambos gitignored). Em produção, vêm do AWS SSM Parameter Store.

## Comandos

### Backend (raiz `backend/src/`)

- Build: `dotnet build Imedto.Backend.sln`
- Rodar API (dev): `ASPNETCORE_ENVIRONMENT=Development dotnet run --project Services/Imedto.Backend.API --no-launch-profile` (Swagger em `/swagger`)
- Testes: `dotnet test Tests/Imedto.Backend.Test`
- Teste único: `dotnet test Tests/Imedto.Backend.Test --filter "FullyQualifiedName~NomeDoTeste"`

### Migrations (EF Core autora, pipeline aplica em RDS)

Toda alteração de schema tem **duas etapas**:

1. **Gerar no EF Core** (código C# em `backend/src/Services/Imedto.Backend.Infrastructure/Database/Migrations/`):
   ```
   cd backend/src
   dotnet ef migrations add <Nome> \
     --project Services/Imedto.Backend.Infrastructure \
     --startup-project Services/Imedto.Backend.API \
     --output-dir Database/Migrations
   ```

2. **Exportar SQL idempotente e salvar em `db/migrations/`**:
   ```
   dotnet ef migrations script <MigrationAnterior> <MigrationNova> \
     --project Services/Imedto.Backend.Infrastructure \
     --startup-project Services/Imedto.Backend.API \
     --idempotent --output /tmp/next.sql
   # Remover BEGIN/COMMIT do script (a pipeline gerencia a transação).
   # Salvar como db/migrations/YYYYMMDDHHMMSS_descricao.sql (mesmo timestamp da migration EF).
   ```

3. **Aplicar no banco**:
   - Em CI/CD: a pipeline envia o SQL para a EC2 e roda [deploy/scripts/migrate.sh](deploy/scripts/migrate.sh), que executa `psql` contra o RDS lendo host/senha do AWS SSM.
   - Em dev local: rodar `psql` direto contra o RDS (ou banco local) com o arquivo SQL. **Nunca usar `dotnet ef database update`** — o `dotnet ef` é só para autoria do SQL.

Functions SQL, triggers, índices CONCURRENTLY e seed data que não tenham equivalente no EF (ou não devam existir no modelo .NET) são escritos **direto como `.sql`** em `db/migrations/` — sem passar pelo EF.

### Frontend (raiz `frontend/`)

- Dev: `npm run dev` (Vite, porta 3000, proxy `/api` → `http://localhost:5000`)
- Build: `npm run build` (type check + vite build)
- Lint/fix: `npm run lint`
- Testes: `npm test` (Vitest)

## Arquitetura do backend

Solução em 3 pastas lógicas (`Core`, `Services`, `Tests`), 7 projetos alinhados a DDD + CQRS:

- **`Core/Imedto.Backend.SharedKernel`** — interfaces CQRS (`ICommand`, `IQuery<T>`, `IDomainEvent`, `ICommandBus`, `IRequestBus`, `IEventBus`), `Entity` base (com `DomainEvents`), `BusinessException`, `GlobalExceptionFilter`, `UnitOfWorkAttribute` + `IUnitOfWorkFactory` (interface async: `Task CommitAsync()` + `IAsyncDisposable`).
- **`Services/Imedto.Backend.Domain`** — aggregate roots (propriedades `virtual` com `protected set`, fábrica estática tipo `.Criar(...)`), domain events, interfaces de repositório, abstrações de auth.
- **`Services/Imedto.Backend.Contracts`** — commands, queries e DTOs (camada pública).
- **`Services/Imedto.Backend.Application`** — handlers: `*CommandHandler` (scoped), `*QueryHandlers` em plural (singleton, usam repositório Dapper), `*EventHandler` (scoped).
- **`Services/Imedto.Backend.Infrastructure`** — `AppDbContext` (EF Core + Npgsql), configurations em `Database/Configurations/`, `EfUnitOfWorkScope` (transação real via `Database.BeginTransaction`), repositórios EF (escrita) + `*QueryRepository` Dapper (leitura), `LocalJwtAuthService` + `EcdsaJwtTokenIssuer` (auth local), `S3FotoStorageService` + `S3AnexoStorageService` (storage), buses em memória, `AppDbContextFactory` (`IDesignTimeDbContextFactory` para o tooling do `dotnet ef` — lê a connection string `Migrations` do `appsettings.Development.json` do API project).
- **`Services/Imedto.Backend.API`** — controllers, `Program.cs`, **Composition Root** em [backend/src/Services/Imedto.Backend.API/Container.cs](backend/src/Services/Imedto.Backend.API/Container.cs).
- **`Tests/Imedto.Backend.Test`** — NUnit 4 + Moq.

### Fluxo de requisição e escopo DI (crítico)

Controller recebe DTO → `ICommandBus.Send` ou `IRequestBus.Query`. **Os buses são singleton para manter o registro de handlers, mas resolvem handlers do scope da request atual via `IHttpContextAccessor.HttpContext.RequestServices`** (não usam `CreateScope()` — isso criaria um `AppDbContext` paralelo ao do `UnitOfWorkAttribute` e o commit iria no DbContext errado).

`UnitOfWorkAttribute` injeta `IUnitOfWorkFactory` → `EfUnitOfWorkScope` abre transação no `AppDbContext` scoped da request → action executa → `CommitAsync` chama `SaveChangesAsync` + `transaction.Commit`. Exception → rollback implícito no `DisposeAsync`.

`GlobalExceptionFilter` mapeia `BusinessException` → HTTP 422; qualquer outra → 500.

### Adicionar novo domínio

1. Criar Domain / Contracts / Application / Infrastructure (seguir `Produto` como referência — **será removido na Fase 1**).
2. `EntityTypeConfiguration` em `Infrastructure/Database/Configurations/`.
3. `DbSet<T>` em `AppDbContext`.
4. Registrar handler em `Container.RegistrarHandlers` (commands/events: `AddScoped`; query handlers: `AddSingleton`).
5. Registrar no bus em `Container.RegistrarBuses`.
6. Gerar migration EF + copiar SQL idempotente para `db/migrations/` (aplicado pela pipeline em RDS).
7. SQL custom (functions, triggers, índices CONCURRENTLY) em `.sql` separado dentro de `db/migrations/`.

## Arquitetura do frontend

- **Padrão BFF**: frontend **nunca** vê tokens — cookies HttpOnly gerenciados pelo backend. Estado de auth no cliente = `{ id, email, roles }`.
- **`httpClient`** ([frontend/src/services/httpClient.ts](frontend/src/services/httpClient.ts)): axios com `baseURL: "/api"`, `withCredentials: true`. Interceptor de 401 → `POST /api/auth/refresh` uma vez, falha → logout + redirect `/login`.
- **Stores Pinia**: `authStore.init()` em [frontend/src/main.ts](frontend/src/main.ts) **antes do `app.mount`** (await) para reidratar via `GET /api/auth/me`.
- Convenção: views/stores nunca usam `httpClient` diretamente — toda HTTP passa por `*Service` em [frontend/src/services/](frontend/src/services/).
- Alias `@/*` → `./src/*`.

## Autenticação (BFF + LocalJwt)

1. `POST /api/auth/login` → `LocalJwtAuthService` valida e-mail/senha contra `auth_credenciais` (BCrypt + pepper), emite access token via `EcdsaJwtTokenIssuer` (ECDSA P-256 — chaves em `Auth:Jwt:PrivateKeyPem`/`PublicKeyPem`) e cria refresh token em `auth_refresh_tokens`. Backend seta dois cookies HttpOnly (`access-token` path `/api`, `refresh-token` path `/api/auth/refresh`) e retorna `{ usuario }` (+ `accessToken` em dev).
2. Middleware `AddJwtBearer` valida tokens **ES256** com a chave pública local (`Auth:Jwt:PublicKeyPem`) — não há JWKS remoto.
3. `OnMessageReceived` lê o cookie primeiro, com fallback para header `Authorization: Bearer` (Swagger/testes).
4. Requer `Microsoft.AspNetCore.Authentication.JwtBearer >= 10.0` — versões <10 não suportam ES256 nativamente.
5. Confirmação de e-mail, reset de senha e convite usam tokens em `auth_email_tokens` (TTL 24h confirm, 1h reset, 7d invite). Para testes unitários, usar Moq de `IAuthService`.

## Conexão Postgres (RDS)

- Connection string normal Npgsql em `ConnectionStrings:Default` (runtime) e `ConnectionStrings:Migrations` (autoria de migrations via `dotnet ef`).
- Em prod, host/senha vêm do AWS SSM Parameter Store (`/imedto/dev/db-host`, `/imedto/dev/db-password`); a EC2 lê via IAM role.
- Em dev, o valor fica em `appsettings.Development.json` (gitignored).
- O `AppDbContextFactory` usa apenas `ConnectionStrings:Migrations` para o tooling do `dotnet ef`.

## Convenções de código

- Idioma: identificadores, mensagens e comentários em **português** (`CriarProdutoCommand`, `BusinessException("Preço deve ser maior que zero.")`). Cultura `pt-BR` global em `Program.cs`.
- Indentação: 4 espaços.
- `BusinessException` → 422 automático; nunca usar para erros técnicos.
- Aggregates: `virtual` + `protected set` + ctor `protected` + fábrica estática que adiciona `DomainEvent` via `AddDomainEvent`.
- Após salvar o aggregate, handler itera `produto.DomainEvents` → `IEventBus.Publish` → `ClearDomainEvents`.
- Nomes de tabela/coluna no Postgres em `snake_case`; `EntityTypeConfiguration` faz o mapeamento.

## Padrão de produto (premissa)

O Imedto é um produto único — toda tela, fluxo e interação precisa parecer parte do mesmo software, escrita pela mesma equipe. Estas premissas valem para qualquer mudança, em qualquer área:

**Experiência consistente em todo o site**
- **Container de página padrão**: toda view interna (dentro do `AppLayout`) começa com `<div class="app-page ...">` (definido em [main.css](frontend/src/assets/main.css)). Centraliza, aplica padding e limita largura — evita o anti-padrão de "espaço em branco colado em um dos lados". Variantes:
  - `.app-page` (padrão, 1280px) — listas, cards, dashboards.
  - `.app-page--narrow` (880px) — formulários, perfil, telas de conta/configuração.
  - `.app-page--wide` (1480px) — relatórios, prontuário, calendários grandes.
  - `.app-page--full` (100%) — apenas para casos que exigem width inteiro (ex: agenda em modo mês).
  - **Não** declarar `max-width`/`margin: 0 auto`/`padding` próprio na raiz da view — usar a classe utilitária. Ao tocar uma view legada que ainda usa container próprio, migrar para `.app-page`.
- Mesmo "shape" de cabeçalho de página: `AppPageHeader` com título, subtítulo opcional e slot de ações.
- Mesmas primitivas para listas, drawers, modais, badges de status, toggles de período, cards e empty states (todos vivem em [frontend/src/components/ui/](frontend/src/components/ui/) — ver [index.ts](frontend/src/components/ui/index.ts) para a lista oficial do design system).
- **Listas paginadas**: usar sempre o `AppPagination` (`v-model:pagina` + `v-model:tamanho` + `:total`). O componente já fornece o seletor de itens por página (10/20/30 padrão), navegação numerada com ellipsis e o texto "1–20 de 47 itens" — não reimplementar lógica de página/ellipsis na view.
- **Botões de ação em tabelas**: usar as classes globais `.btn-icon` + `.btn-icon-ver` (olho/primary), `.btn-icon-editar` (lápis/azul), `.btn-icon-excluir` (lixeira/danger) definidas em [main.css](frontend/src/assets/main.css). Não criar variantes scoped — duplicar em `<style scoped>` reabriria o bug "scoped data-attr atinge root do componente filho".
- **Buscas que tocam a API**: TODO input cujo valor dispara request HTTP precisa de debounce (~300 ms) — caso contrário cada caractere digitado vira uma requisição. Use o composable [useDebouncedRef](frontend/src/composables/useDebouncedRef.ts), nunca `setTimeout` manual. Padrão:
  ```ts
  const buscaInput = ref("")                       // v-model do <input>
  const busca      = useDebouncedRef(buscaInput)   // ref atrasado que aciona a request

  watch(busca, () => { pagina.value = 1 })
  watch([busca, pagina, tamanho], () => carregar(), { immediate: true })
  ```
  Filtros que rodam **client-side** (lista já carregada e filtrada por `computed`) não precisam de debounce — apenas inputs cujo valor passa para um service/HTTP. Cliques em paginação/ordenação devem ser imediatos (não ficam atrás do debounce).
- Cores, tipografia, espaçamentos, raios e sombras vêm dos tokens HSL em [frontend/src/assets/main.css](frontend/src/assets/main.css) — **nunca** hardcodar cores ou montar botão/input do zero.
- Estados (loading, vazio, erro, sucesso, desabilitado) sempre presentes e uniformes — uma tabela vazia usa `AppEmptyState`, não um `<p>` solto.
- Mensagens de erro de negócio vêm do backend (`BusinessException` → 422) e são exibidas no mesmo formato em todas as telas.

**Padrão de desenvolvimento**
- Backend segue DDD + CQRS (commands/queries/events) com **regra de negócio sempre no domain/handler**, nunca no controller, no SQL ou na view.
- Frontend: views consomem **stores Pinia** ou **services**, nunca `httpClient` direto; HTTP só passa por `*Service` em [frontend/src/services/](frontend/src/services/).
- Toda nova feature é validada com type-check (`vue-tsc`/`dotnet build`) e, quando o caso, testes (`dotnet test`/`vitest`) **antes de declarar pronto**.
- Trava do front sempre tem trava espelhada no back (defense-in-depth: 422 do backend é a fonte da verdade; o front é UX).

**Componentização máxima**
- Antes de escrever HTML/CSS scoped numa view, perguntar: *"isso aparece em outra tela ou pode aparecer?"*. Se sim → componente em `components/ui/` ou `components/<dominio>/`.
- Trecho de template repetido entre views (mesmo similar) é cheiro: extraia para componente parametrizado por props.
- Evitar componentes "Frankenstein" com 15 props booleanas — quebrar em vários componentes menores compostos é melhor que um componente único configurável.
- Drawers, modais e formulários compartilhados entre fluxos viram componentes (ex: [AgendamentoFormFields](frontend/src/components/agenda/AgendamentoFormFields.vue) é compartilhado entre criar e editar).

**LGPD ≠ checklist** (ver seção dedicada abaixo)
- Sistema de saúde lida com dados pessoais sensíveis. **Toda** feature nova precisa pensar em LGPD: o dado é necessário? Está sendo logado? Vai aparecer em mensagem de erro? Tem RLS? Há audit trail? Há filtro por estabelecimento?
- Em qualquer mudança que envolva paciente, prontuário, agendamento ou financeiro, validar a seção `## LGPD` antes de enviar.

## Reuso > duplicação (premissa)

Antes de criar **qualquer** endpoint, query, repositório, store, service no front, componente UI, helper ou DTO, **procure o que já existe** e reutilize. Duplicar lógica fragmenta regras, divide a verdade entre dois lugares e gera bugs sutis quando um lado muda e o outro não.

Como aplicar:
- **Backend**: antes de adicionar um método em `*QueryRepository` ou um endpoint novo no controller, faça `grep`/`Glob` por algo equivalente. Procure por padrões: nome do conceito (`Profissional`, `Vinculo`, `Dono`), nome do dado (`especialidade`), nome da operação (`Listar*`, `Obter*`, `Tem*`).
- **Frontend**: antes de criar um `*Service` novo, ver se já existe método equivalente em [frontend/src/services/](frontend/src/services/). Antes de criar componente, ver [frontend/src/components/ui/](frontend/src/components/ui/) e [frontend/src/components/ui/index.ts](frontend/src/components/ui/index.ts) (lista oficial do design system).
- **DTOs/queries**: se o DTO retornado já tem o campo que você precisa para uma nova tela, reuse — não crie um DTO paralelo só para "deixar mais limpo". Estenda o existente ou reaproveite.
- **Quando duplicar é inevitável** (ex: a query existente faz join pesado e a nova precisa ser leve): documente o porquê em comentário ou commit message, e cite a outra como referência.

Se um conceito de domínio aparece em **duas operações diferentes** com a mesma regra (ex: "este usuário pode atuar como profissional neste estabelecimento" → vale para criar agendamento, editar agendamento, listar disponibilidade), extraia em **uma** função do repositório e chame nos dois lugares — não copie o `if`.

## Onde ficam os secrets

Todos gitignored (ver `.gitignore`):
- `.mcp.json` — configuração de MCP servers locais (vazia por padrão; templates em `.mcp.json.example`).
- `backend/src/Services/Imedto.Backend.API/appsettings.Development.json` — connection strings (Default + Migrations), chaves PEM do JWT (`Auth:Jwt:PrivateKeyPem`/`PublicKeyPem`), pepper do BCrypt (`Auth:Bcrypt:Pepper`), API key do Resend, buckets S3.
- `frontend/.env` — apenas `VITE_API_BASE_URL` opcional (frontend é BFF puro, sem segredos).

Templates em `.mcp.json.example` e `frontend/.env.example`. Em produção, todos os valores vêm do AWS SSM Parameter Store.

## LGPD

Sistema de saúde → dados pessoais sensíveis (Art. 5º II). LGPD é **premissa de design**, não checklist de fim de PR — a cada feature nova pergunte: *"este dado é necessário? Está minimizado? Tem RLS? Há audit trail? Pode vazar em log/erro?"*.

Requisitos obrigatórios:
- **Minimização**: query/DTO retorna apenas os campos que a tela usa. Não trazer `cpf`, `data_nascimento`, `telefone`, etc. se a tela não exibe.
- **Direitos do titular**: endpoints de export (`GET /api/minha-conta/exportar-dados`) e exclusão (`DELETE /api/minha-conta`).
- **Audit trail**: log de acesso a dados de paciente/prontuário em audit table (quem, quando, qual registro).
- **Não vazar PII**:
  - Nunca incluir CPF, telefone, e-mail ou nome completo em log estruturado ou em mensagem de erro retornada ao cliente.
  - Nunca retornar token, hash de senha ou ID interno de auth em payload.
  - Mensagens de erro de validação devem ser genéricas ("paciente não encontrado") em vez de descrever o dado consultado.
- **RLS ativo** em todas as tabelas de domínio (defense-in-depth — backend valida permissão, RLS impede vazamento mesmo se o backend errar).
- **Filtro por estabelecimento**: queries de domínio sempre exigem `estabelecimento_id` no `WHERE` ou no `Include`/join. Multi-tenant é regra, não exceção.
- **Consentimento**: se a feature coleta dado novo do titular (ex: novo campo de paciente), avaliar se precisa de aviso/consentimento explícito antes de armazenar.
- **Regras importante**:
  - as regras do front que depende de retorno ou input da api, precisam estar sendo validadas também no backend, para evitar de pessoas tentando passar informação direto pela api, sendo assim, precisa ter essa segurança.
  - as apis precisam respeitar segurança de autenticação, nao pode deixar que uma pessoa altere registros caso ela não tenha a devida permissão para isso
  - todo o site precisa ser pensado de forma que ele pode escalar, ter muitas requisições ao mesmo tempo, entao ele precisa ter resiliencia, e pensar em performance, nao pode demorar o retorno, mas precisa esta muito bem otimizado para nao pesar para o uso do usuário.
  - deve buscar basicamente as informações necessárias para aquele momento, caso nao tenha clicado em uma aba que ainda nao vai utilizar, nao precisa ficar fazendo consulta desnecessária (deve buscar apenas quanto precisar de fato)
  - as rotas que poderem ser reutilizadas, precisa serem feitas, para evitar criação de novos endpoints desnecessários que fazem praticamente a mesma coisa
  - o código precisa ser simples de entendimento, e componentizado para reutilizar o que for possivel em outras partes evitando codigos duplicados.
  - as paginas do site precisam estar centralizadas no meio, evidando deixar espaco em brando ou so na direita ou so na esquerda
  - os componentes do front precisam estar padronizados e reutilizados no site como um todo de acordo com sua necessidade. deve tentar reutilizar o maximo possivel para padronizar como um design system
  - as regras de negocio devem estar todas no backend, sendo transparente para o front, a fim de evitar problemas de segurança também.
  - todos os componentes do frontend do projeto deve pegar do design system, com objetivo de padronizar os componentes atuais. so deve criar algo quando for bem especifico para aquele cenário e nao for algo reutilizavel, mas deve sempre dar prioridade para usar os componentes do design system.
  - caso o componente nao exista ainda, e pode ser reutilizado, deve ser criado no design system primeiro, e depois importado no front, a fim de manter a padronização
  - antes de criar um componente novo no front verifique se ele existe no design system para uso na plataforma e padronização