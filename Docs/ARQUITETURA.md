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

#### Padrão: event handler com fan-out por permissão (notificar grupo)

Quando um evento de domínio deve notificar **todos os usuários com uma ação RBAC** no estabelecimento:

1. O event handler (scoped) injeta `INotificacaoService` + um query repository Dapper (singleton) que resolve os destinatários.
2. O repositório executa uma query SQL `UNION` falha-fechada: dono do estabelecimento (sempre) + profissionais com vínculo `Ativo` cujo modelo de permissão concede a área/ação. **Sempre filtrado por `estabelecimento_id`** — multi-tenant estrito.
3. O handler itera os destinatários e chama `INotificacaoService.EnviarAsync` por usuário (1 notificação cada).
4. Falhas por destinatário são absorvidas individualmente (log de erro estruturado sem PII) — não revertem a operação principal que gerou o evento.
5. A mensagem não inclui PII além do necessário (R5 de LGPD): nome do item/recurso, quantidades/estado, sem campos livres que possam conter dados clínicos.

Exemplo canônico: `EstoqueAbaixoMinimoEventHandler` (briefing 2026-06-10_003) — notifica usuários com ação `estoque` no cruzamento de estoque mínimo via `InventarioNotificacaoQueryRepository.ListarUsuariosComAcaoEstoque`.

Precedente anterior: `NotificarEquipeAoConfirmarHandler` (Cirurgias) — fan-out sobre lista de membros já carregada no evento (sem query de permissão em runtime).

### Geração de PDF server-side via QuestPDF

Dois documentos clínicos são gerados integralmente no servidor via **QuestPDF** (licença Community), devolvendo `application/pdf` como `FileContentResult`:

- **`QuestPdfReceitaService`** (`Infrastructure/Receitas/`) — receita médica. Endpoint: `GET /api/receitas/{id}/pdf`.
- **`QuestPdfTermoService`** (`Infrastructure/Termos/`) — PDF probatório do termo de consentimento emitido. Endpoint: `GET /api/termos/{id}/pdf-gerado`. Conteúdo: cabeçalho institucional + bloco do paciente + snapshot HTML da versão aceita + bloco de evidência do aceite (data/hora, IP/UA, hash SHA-256, últimos 6 chars do token — nunca o token completo) + marca d'água por status.

Ambos os serviços compartilham: identidade visual (fonte Nunito embarcada em `Receitas/Fonts/`, constantes de cor sincronizadas com `PDF_THEME` do frontend, cabeçalho com logo do estabelecimento ou placeholder de iniciais, bloco do paciente, marca d'água por status). O registro de fontes Nunito é idempotente (flag estático interno ao assembly).

O `QuestPdfTermoService` é injetado como `ITermoPdfGeradoService` (scoped) pelo `PacienteTermoController`; o audit LGPD é best-effort via `ITermoAuditLogger` (ação `"termo-pdf-gerado"` em `termo_audit_log`).

Nome do arquivo no `Content-Disposition`: `termo-{id}.pdf` (sem PII — minimização LGPD).

### Leitura agregada multi-aggregate (read-side de consolidação)

A regra geral é **uma query Dapper por aggregate**. A exceção é a **consolidação read-only**: uma view que precisa listar, paginada, registros de N aggregates distintos num mesmo conjunto ordenado. Nesse caso é aceitável uma query agregada que faz `UNION ALL` das tabelas de origem, com `ORDER BY` + `LIMIT/OFFSET` aplicados **sobre o conjunto unificado** (nunca paginar cada tabela isoladamente). Exemplos canônicos: `GET /api/pacientes/{id}/documentos` (briefing 2026-06-09_009) e `GET /api/pacientes/{id}/acessos` (briefing 2026-06-10_007, relatório LGPD) — o primeiro consolida receitas + atestados + pedidos de exame; o segundo consolida `paciente_acesso_log` + `prontuario_acesso_log` com rótulo leigo via CASE WHEN. Premissas obrigatórias deste padrão: filtro `estabelecimento_id` em **todas** as sub-consultas; DTO de resumo sem PII clínica (minimização); audit de leitura registrado **uma vez por carga** via `IProntuarioAcessoLogService` quando o paciente tem prontuário (mesmo precedente de `ListarReceitasDoPacienteQueryHandlers`). O endpoint suporta ainda **busca textual por subconsulta antes do UNION** (predicado `unaccent(coluna) ILIKE unaccent('%' || @Busca || '%')` aplicado em cada aggregate individualmente — receita: itens de medicamento; atestado: tipo + conteúdo; pedido: exames + indicação clínica); a paginação continua aplicada sobre o conjunto unificado pós-filtro, e o termo de busca não é ecoado no DTO nem registrado em log (LGPD).

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

### Login em dois passos (2FA TOTP)

**INVARIANTE DE SEGURANÇA — ANTI-BYPASS**: nenhum caminho do sistema emite cookies de sessão para um usuário com 2FA ativo sem a conclusão bem-sucedida do passo 2. Qualquer desvio é vulnerabilidade, não degradação graciosa.

#### Passo 1 — senha

`POST /api/auth/login` continua validando e-mail + senha contra `auth_credenciais`. **Mudança**: se a credencial autenticada tem 2FA ativo, **nenhum** cookie é emitido. A resposta retorna `{ requerSegundoFator: true }` (200) com um **desafio efêmero** — token opaco cifrado via `IDataProtector.CreateProtector("auth.totp.challenge")` contendo `{ usuario_id, exp: UtcNow + 5min }`. O `usuario_id` nunca transita em claro. O front transiciona para o painel de código.

Se o usuário não tem 2FA ativo: comportamento atual inalterado (passo 2 não existe, cookies setados imediatamente no passo 1).

#### Passo 2 — código TOTP ou código de recuperação

`POST /api/auth/login/2fa` recebe `{ desafio, codigo }`. Fluxo:

1. `IDataProtector.Unprotect("auth.totp.challenge")` decodifica o desafio → valida `exp` (TTL 5 min) e extrai `usuario_id`. Desafio inválido/expirado → 422 genérico sem revelar se o usuário existe.
2. Busca `usuario_2fa` do usuário. Aceita:
   - **Código TOTP**: `IDataProtector.Unprotect("auth.totp.secret")` recupera o segredo em memória, valida o código com janela **±1 step** (RFC 6238, SHA-1, 6 dígitos, period 30s).
   - **Código de recuperação**: busca hash correspondente em `usuario_2fa_codigo_recuperacao`; hash one-time → marca `usado_em` se validar.
3. Sucesso → `SetAuthCookies` (mesmo `AuthResult` do login normal). Código de recuperação → registra linha de auditoria (`UsouCodigoRecuperacao`) em `usuario_seguranca_audit`.
4. Falha → 422 genérico ("Código inválido."). **Não** incrementa `TentativasFalhas` da senha — o lockout de senha não deve ser disparado por erros de código TOTP.

**Rate limit**: endpoint do passo 2 usa política `auth-sensitive` (3 req/IP, sliding window).

#### Ativação do 2FA

`POST /api/auth/2fa/iniciar` (autenticado, passo 1): gera segredo TOTP (≥ 160 bits, base32), persiste cifrado via `IDataProtector.Protect("auth.totp.secret")` com `status = Pendente`, retorna `{ otpauthUri, segredoBase32 }`. O segredo base32 só transita neste momento.

`POST /api/auth/2fa/confirmar` (autenticado, passo 2 da ativação): valida código TOTP contra o segredo pendente (±1 step). Se válido: status → `Ativo`, `ativado_em` preenchido, gera 10 códigos de recuperação aleatórios, persiste apenas seus **hashes** em `usuario_2fa_codigo_recuperacao`, retorna os 10 códigos em claro **uma única vez**. Se inválido: 422 genérico; 2FA permanece inativo.

Pós-ativação, **nenhum** endpoint (`/auth/me`, bootstrap, qualquer GET de perfil) devolve o segredo TOTP nem os códigos de recuperação.

#### Desativação do 2FA

`POST /api/auth/2fa/desativar` (autenticado): exige **senha atual válida** (`ValidarSenhaAsync`) **E** código TOTP válido ou código de recuperação válido. Ambos precisam passar — falha em qualquer um → 422 genérico, 2FA permanece ativo. Sucesso → remove `usuario_2fa` + todos os `usuario_2fa_codigo_recuperacao`.

#### Toggle por estabelecimento (exigir 2FA para o Dono)

Coluna `exigir_2fa_dono boolean NOT NULL DEFAULT false` no aggregate `Estabelecimento`. Somente o **Dono** pode alterá-la (RBAC; 403 para outros papéis). O estado de 2FA do usuário é **global** (da conta, não por tenant); o toggle controla apenas o enforcement para o papel Dono naquele estabelecimento.

Enforcement (R10): quando `exigir_2fa_dono = true` e o Dono sem 2FA ativo conclui o login, o backend inclui `deveConfigurar2fa: true` no bootstrap/me. O front redireciona para Minha Conta e bloqueia navegação para outras rotas `requiresTenant` via guard de rota (espelho de `onboardingPendente`) até o Dono ativar o 2FA.

---

### Bootstrap da SPA e resolução de tenant

`GET /api/auth/bootstrap` retorna em paralelo `{ usuario, profissional, estabelecimentos }` — substitui 3 round-trips serializados. Handler: `BootstrapMeQueryHandlers` (query singleton, sem UoW).

`MeUsuarioDto` expõe `ultimoEstabelecimentoId: long?` — coluna `usuarios.ultimo_estabelecimento_id` (nullable, FK ON DELETE SET NULL). Ausente no `/auth/me` (apenas bootstrap).

**Lógica de resolução de tenant em `tenantStore.popularEstabelecimentos(lista, ultimoEstabelecimentoId)`:**

1. Se `ultimoEstabelecimentoId` está na lista de acessíveis → seleciona esse.
2. Se nulo ou órfão (id não está na lista) → fallback `lista[0]` + a função retorna `true` para sinalizar que o `main.ts` deve gravar o resolvido (E1: `POST /api/auth/ultimo-estabelecimento`).
3. Se sessionStorage já tem tenant válido → re-hidrata permissões do servidor (fonte da verdade) sem trocar.

**Command de gravação:** `RegistrarUltimoEstabelecimentoCommand` → `RegistrarUltimoEstabelecimentoCommandHandler`. Falha-fechada multi-tenant: valida `PodeAtuarComoProfissional` (vínculo não-inativo OU é Dono) antes de qualquer escrita. Sem acesso → `BusinessException("Não encontrado.")`.

**Dois gatilhos de gravação:** (a) troca manual pela modal (`EstabelecimentoSeletorModal`) — sempre grava antes do reload; (b) primeiro boot sem registro (`ultimoEstabelecimentoId == null`) — main.ts dispara a gravação do fallback resolvido.

**Degradação graciosa (R7):** se o POST falhar (rede/500), a troca de tenant acontece mesmo assim. Falha silenciosa com catch, sem erro bloqueante.

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
| Variável pool | `prontuario_variaveis_pool` (`eh_padrao_sistema=true`, `estabelecimento_id=NULL`) | `/api/admin/variaveis-globais` (filtro `?categoria=`) | Idem — categorias clínicas via enum `TipoVariavelPool` (**Alergia, Medicamento, Doenca, Cirurgia, RelacaoFamiliar, Expectativa**). Droga e AtividadeFisica removidos (briefing 2026-06-05_001). |
| Região anatômica | `regioes_anatomicas_catalogo` (sem `estabelecimento_id` — global por construção, 144 registros hierárquicos) | `/api/admin/regioes-globais` | Exame físico consome direto da tabela; admin edita via tree view |

**Consequências:**
- Mudança feita pelo admin reflete em TODOS os tenants imediatamente (sem importação, sem cópia).
- Endpoints `*/importar-do-global/{id}` removidos.
- Aba "Templates do sistema" no tenant removida — padrão-sistema aparece direto nas listagens normais.
- Operações admin (criar/editar/inativar) gravam com `EhPadraoSistema=true` + `EstabelecimentoId=NULL`.
- Soft delete via `Ativo=false` (não exclui físico — preserva audit + histórico do tenant).

**Hierarquia de regiões anatômicas** (`regioes_anatomicas_catalogo`):
- Estrutura: `codigo` (PK lógico) + `pai_codigo` (FK self) + `nivel` (1-3) + `vista` (`anterior` | `posterior` | `circunferencial`).
- Validações ao criar/editar subgrupo: `nivel = pai.nivel + 1` e `vista = pai.vista` (BusinessException 422). **Não se aplica aos nós circunferenciais** — são nível-1 raiz (`pai_codigo = NULL`) e não têm filhos próprios no catálogo.
- Exclusão: só folhas (sem filhos). Região com filhos → 422 `"Esta região tem subgrupos. Inative em vez de excluir, ou remova os subgrupos primeiro."`.
- **Vista circunferencial (briefing 2026-06-08_005 B1)**: 9 nós agregadores nível-1 (`{base}-circunferencial`) foram adicionados ao catálogo. Esses nós não têm filhos próprios nem `svg_coords` (sem hotspot no `BodyMap` em B1 — highlight é B2). A modal resolve os filhos em tempo de execução: `getFilhos("{base}-anterior") + getFilhos("{base}-posterior")`. **Exceção clínica do abdome**: `abdome-circunferencial` → ramo anterior = `abdome-anterior`, ramo posterior = `lombossacra-posterior` (não existe `abdome-posterior` no catálogo — a lombossacra é o correlato clínico posterior do abdome). O `RegiaoTreeView` do admin exibe um terceiro grupo de vista `circunferencial`.

**Frontend admin:**
- Views em `modules/admin/views/`: `ModelosGlobaisListView`, `ModelosGlobaisFormView`, `VariaveisGlobaisListView`, `VariaveisGlobaisFormView`, `RegioesGlobaisListView`, `RegioesGlobaisFormView`.
- Componente novo: `modules/admin/components/regioes/RegiaoTreeView.vue` (tree view recursivo expand/collapse por vista → nível 1 → 2 → 3).
- Stores: `modelosGlobaisStore`, `variaveisGlobaisStore`, `regioesGlobaisStore` (este último carrega árvore completa).
- Rotas sob `/admin/modelos-globais`, `/admin/variaveis-globais`, `/admin/regioes-globais`.

### Catálogos Globais — Exceção ao live-link: Modelos de permissão padrão do sistema (briefing 2026-06-04_001)

Os catálogos globais descritos acima (modelo de prontuário, variável pool, região anatômica) usam **live-link puro**: o tenant lê o registro global diretamente via `WHERE (eh_padrao_sistema=true OR estabelecimento_id=@X)`. O modelo de permissão **não pode** seguir esse padrão.

**Por que não live-link:** `vinculo_profissional_estabelecimento.modelo_permissao_id` é FK para a cópia `eh_padrao=true` **daquele estabelecimento**. As queries de autorização (`UsuarioTemAcao`/`UsuarioTemPermissaoExtra`) fazem JOIN `v.modelo_permissao_id = mp.id` filtrando `v.estabelecimento_id`. Um registro global compartilhado quebraria esse vínculo por tenant — um profissional de um estabelecimento não pode ter FK apontando para um registro sem `estabelecimento_id`.

**Representação:** registro global = `modelo_permissao_estabelecimento` com `estabelecimento_id NULL` + `eh_padrao=true` (template/fonte). Cópias por tenant = `estabelecimento_id NOT NULL` + `eh_padrao=true` (referenciáveis por vínculo). Nenhum vínculo aponta para o registro global.

**Propagação cross-tenant:** criar/editar/excluir o global sincroniza as cópias correlacionadas **por Nome** em todos os tenants, em transação única (R11). Renomear o global renomeia todas as cópias para manter a correlação (R3/R5). `R8`: edição não toca `permissoes_extras` das cópias (preservadas como semeadas).

**Exclusão segura (R6):** bloqueada se qualquer cópia correlacionada estiver em uso por vínculo ativo em qualquer estabelecimento — nunca deixa profissional órfão.

**Clínica nova (CA6):** `CriarModeloPadraoAoCriarEstabelecimentoHandler` semeia as cópias a partir dos registros globais (fallback para hardcode `CriarPadroes()` se o seed ainda não rodou).

**Endpoint admin:** `api/admin/catalogos/permissoes` (policy `ImedtoAdmin`). Controller: `AdminModelosPermissaoGlobaisController`. Handlers em `Application/Admin/ModelosPermissaoPadraoSistema/`.

**Comparativo de catálogos globais:**

| Entidade | Tabela | Escopo global | Como tenant consome | Tipo |
|---|---|---|---|---|
| Modelo de prontuário | `modelo_de_prontuario` | `eh_padrao_sistema=true`, `estabelecimento_id=NULL` | WHERE `eh_padrao_sistema=true OR estabelecimento_id=@X` | Live-link |
| Variável pool | `prontuario_variaveis_pool` | `eh_padrao_sistema=true`, `estabelecimento_id=NULL` | WHERE `eh_padrao_sistema=true OR estabelecimento_id=@X` | Live-link |
| Região anatômica | `regioes_anatomicas_catalogo` | global por construção | acessa diretamente | Live-link |

#### Vínculo pool ↔ prontuário (briefing 2026-06-05_001)

Desde 2026-06-06, o pool de variáveis está conectado aos campos do prontuário de duas formas:

**1. Autocomplete (front):** os campos `nome` das seções HPP (alergias, medicações, cirurgias, doenças) e o campo `parentesco` da história familiar usam `AppAutocompleteCriavel`, que carrega a lista por tipo via `variavelPoolService.listar(tipo)` uma vez por abertura de seção. Filtro client-side — sem request por tecla.

**2. Criação automática (back):** ao salvar uma evolução, `PoolExtratorEvolucao` (injetado em `RegistrarEvolucaoCommandHandler`) percorre o `ConteudoJson` nos 5 campos mapeados e cria itens inéditos no pool do estabelecimento, transacionalmente:

| Campo JSON | Tipo do pool |
|---|---|
| `hpp.alergias[].nome` | Alergia |
| `hpp.medicacoes[].nome` | Medicamento |
| `hpp.cirurgias[].nome` | Cirurgia |
| `hpp.doencas[].nome` | Doenca |
| `h-familiar.parentes[].parentesco` | RelacaoFamiliar |

**Dedup canônica:** trim + lower + remoção de diacríticos (via `NormalizadorPool.Normalizar`), aplicada em memória. Idêntica no CRUD manual (`AdicionarVariavelPoolCommandHandler`) e na extração automática. Considera padrão-sistema como existente — não cria cópia para o estabelecimento.

**Permissão:** criação automática via evolução não exige `ModelosProntuario` (qualquer profissional com acesso ao prontuário). CRUD manual via `ProntuarioTemplateController` continua exigindo `ModelosProntuario`.

**Falha-suave:** JSON inválido ou chave ausente não interrompe o salvamento da evolução — `PoolExtratorEvolucao` degrada silenciosamente.
| **Modelo de permissão** | `modelo_permissao_estabelecimento` | `estabelecimento_id=NULL`, `eh_padrao=true` | FK via cópia `(estabelecimento_id=@X, eh_padrao=true)` | **Cópia materializada + propagação** |

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

## Regra cross-cutting: Especialidade por vínculo/estabelecimento (briefing 2026-06-03_004)

**A especialidade efetiva do profissional é atributo do vínculo com o estabelecimento**, com fallback para o cadastro global: `COALESCE(v.especialidade_convidada, p.especialidade)`.

Essa regra se aplica em **todos os lugares** onde a especialidade do profissional é exibida ou impressa no contexto de um estabelecimento. Os três pontos canônicos de leitura são:

1. **Lista interna de equipe** (`ListarProfissionaisDoEstabelecimento`): `VinculoQueryRepository.cs` — coluna `COALESCE(v.especialidade_convidada, p.especialidade) AS Especialidade`.
2. **Lista pública/seletores** (`ListarProfissionaisPublicoDoEstabelecimento`): `VinculoQueryRepository.cs` — mesma expressão COALESCE na query pública (agenda/prontuário/orçamento).
3. **PDFs/termos clínicos** (`{{profissional.especialidade}}`): `TermoResolverDeVariaveis.cs` — `COALESCE(v.especialidade_convidada, p.especialidade)` na query do `ProfissionalResolver` (o JOIN com `vinculo ... status='Ativo'` já existia para defense-in-depth multi-tenant).

**Edição atômica de profissão+especialidade do vínculo** (briefing 2026-06-04_003): `PUT /api/estabelecimento/profissionais/{vinculoId}/profissao-especialidade` — restrito ao Dono. Grava `profissao_convidada_id` e `especialidade_convidada` num único comando (`AlterarProfissaoEspecialidadeDoVinculoCommand`) para evitar janela de estado inconsistente (profissão nova + especialidade da profissão antiga). Trocar a profissão limpa a especialidade. Valor vazio/nulo limpa os campos (fallback volta ao global). A especialidade deve pertencer ao catálogo da profissão informada — backend valida com `ExisteEspecialidadeAtivaPorNome` (422 BusinessException). Independe do status do vínculo (Convidado/Ativo/Inativo). A linha sintética do Dono (`vinculoId=null`) não é editável — segue usando `p.especialidade`.

**`ProfissionalVinculadoDto`** agora expõe `ProfissaoConvidadaId` (`long?`) além de `Profissao` (nome) — necessário para pré-selecionar o dropdown de profissão no modal de detalhes e filtrar especialidades. O campo **não** está no `ProfissionalPublicoDto` (seletores públicos não precisam).

**Endpoint legado** `PUT /especialidade` é mantido mas marcado como deprecated no frontend — migre para `/profissao-especialidade`. O handler `AlterarEspecialidadeDoVinculoCommandHandler` permanece registrado enquanto há chamadas em circulação.

**Invariante de implementação**: nunca inverter o COALESCE para `COALESCE(p.especialidade, v.especialidade_convidada)` — isso faz o cadastro global vencer e ofusca a especialidade por estabelecimento (era o bug original).

---

## Bounded Context: Agendamentos (briefing 2026-06-02_001)

### Máquina de estados do Agendamento

```
Agendado ──── Confirmar() ───────────────────► Confirmado
   │           (canal interno: recepção)            │
   │                                                │  (mudou horário/profissional)
   │  (mudou horário/profissional)                  │  Atualizar() → LembretePorEmailEnviado=false
   │  Atualizar() → LembretePorEmailEnviado=false   │  → AgendamentoReagendadoEvent → e-mail c/ link
   │  → AgendamentoReagendadoEvent → e-mail c/ link ▼
   │                                             Agendado  ← RESET
   │
   │  ConfirmarPorLinkPublico(token, ip, ua)
   │  (canal anônimo: paciente via link público)
   └──────────────────────────────────────────► Confirmado  (Fase 2)
   │
   ├──── Cancelar(motivo) ──────────────────────► Cancelado  (terminal)
   │
   └──── Concluir() ────────────────────────────► Concluido  (terminal)
```

**Transições válidas:**

| De | Para | Método | Condição |
|---|---|---|---|
| `Agendado` | `Confirmado` | `Confirmar()` | Status == Agendado (canal interno) |
| `Agendado` | `Confirmado` | `ConfirmarPorLinkPublico(token, ip, ua)` | Status == Agendado + token válido + não expirado (canal anônimo, Fase 2) |
| `Confirmado` | `Agendado` | `Atualizar()` | Muda InicioPrevisto, FimPrevisto ou ProfissionalUsuarioId |
| `Agendado` ou `Confirmado` | `Cancelado` | `Cancelar(motivo)` | motivo obrigatório |
| Qualquer (exceto Cancelado) | `Concluido` | `Concluir()` | — |

**Estados terminais**: `Cancelado` e `Concluido` — `Atualizar()` lança `BusinessException`.

### Regra de reset (R1) e evento de domínio (R4)

Quando `Atualizar()` detecta mudança em `InicioPrevisto`, `FimPrevisto` **ou** `ProfissionalUsuarioId` (comparação feita **antes** de sobrescrever os campos):

1. Se `Status == Confirmado` → `Status = Agendado` + `LembretePorEmailEnviado = false` (R1, R3).
2. Se `Status == Agendado` → apenas `LembretePorEmailEnviado = false` (R6, sem reset de status).
3. Em ambos os casos → `AddDomainEvent(new AgendamentoReagendadoEvent(...))` (R4, R5).
4. Se mudar **só** `TipoServico` e/ou `Observacoes` (horário e profissional idênticos) → nenhuma das ações acima (R2).

O `AgendamentoReagendadoEvent` **não carrega PII** (apenas IDs + novo `InicioPrevisto`). O handler `EnviarEmailAgendamentoReagendadoEventHandler`:
- Gera token de confirmação pública via `Agendamento.GerarTokenConfirmacao()` (Fase 2, default TTL 7 dias, `min(agora+TTL, InicioPrevisto)`).
- Persiste o token via `IAgendamentoRepository.Salvar()` antes de enviar o e-mail.
- Monta e envia e-mail ao paciente com o link `{AppBaseUrl}/agendamentos/confirmar/{token}`.
- Aplica degradação graciosa: paciente sem e-mail → pula com `LogInformation` (sem PII); falha SES → `LogWarning` sem corpo, não relança.

### Confirmação por link público (Fase 2)

**Endpoint**: `[AllowAnonymous] + [EnableRateLimiting("agendamentos-publico")]`
- `GET /api/publico/agendamentos/confirmar/{token}` → retorna resumo mínimo (apenas NomeFantasia do estab, profissional, tipo de serviço e data/hora). **Sem PII do paciente, sem paciente_id, sem estabelecimento_id.**
- `POST /api/publico/agendamentos/confirmar/{token}` → confirma presença (Agendado → Confirmado).

**Segurança**:
- Token 256 bits (32 bytes, RFC 4648 §5 url-safe sem padding) — 43 caracteres.
- 10 req/min por IP (política `agendamentos-publico`).
- Token inválido/expirado/cancelado → 410 Gone com mensagem **genérica idêntica** em todos os casos (anti-enumeração).
- Idempotência: já Confirmado → 200 "Presença já confirmada".
- Todo acesso GET/POST grava `AgendamentoConfirmacaoAcessoLog` (`{AgendamentoId, EstabelecimentoId, IP, UserAgent, acao, timestamp}`) — sem PII do paciente.

**Frontend**: rota anônima `/agendamentos/confirmar/:token` → `ConfirmarPresencaPublicaView` (espelho de `AceiteTermoPublicoView`). Sem login, sem menu, mobile-first.

---

## Área de domínio: Cobranças (Financeiro / contas a receber) — briefing 2026-06-10_009 (F1)

Introduz contas a receber do paciente (`cobrado ≠ pago`). **Não confundir** com a área `financeiro.*` (visão agregada da clínica, `Lancamento` avulso) — Cobranças é a conta a receber **por paciente**.

### Agregados
- **`Cobranca`** (aggregate root) — conta a receber: `estabelecimento_id` (tenant), `paciente_id`, `origem` (`Consulta` na F1; enum aberto para `Procedimento`/`Cirurgia` em F4/F5), `tipo_atendimento` (`Particular`|`Convenio`), `agendamento_id?`, `orcamento_id?` (F5), `convenio_id?` (F6), `valor_cobrado`, `desconto`, `status`.
- **`Pagamento`** (entidade filha) — quita a cobrança; reusa `FormaPagamento`. **Imutável** por design (preparado para estorno com histórico na F2/INV-7 — nunca update/delete de valor).
- Config: `TabelaPrecoConsulta` (valor sugerido no check-in) e `ConfigTaxaFormaPagamento` (taxa de cartão por forma, aplicada automaticamente — vive na aba de Config do Financeiro, padrão master-detail de `OrcamentoSettingsView.vue`).

### Invariantes (validadas no Domain, 422 via `BusinessException`)
- **INV-1**: `SUM(pagamentos.valor) ≤ valor_cobrado − desconto`.
- **INV-2**: `status` derivado (Aberta/ParcialmentePaga/Paga) da soma **líquida** (pagamentos − estornos futuros); nunca setado à mão salvo `Cancelada`.
- **INV-3**: registrar `Pagamento` **gera `Lancamento`** (Receita/Pago, `cobranca_id`+`pagamento_id`) na **MESMA transação** (atômico — rollback de ambos em falha).
- **INV-4**: `0 ≤ desconto ≤ valor_cobrado`. **INV-5**: cada `Pagamento.valor > 0`. **INV-6**: `Cobranca` sempre com `estabelecimento_id`+`paciente_id`.
- **INV-8** (RBAC): só aplica desconto quem tem `orcamento.aprovar` OU `financeiro.lancar`/`financeiro_paciente.registrar` OU é Dono.

### Padrões
- **Arredondamento monetário**: helper **único** de domínio — 2 casas, `MidpointRounding.AwayFromZero`. Todo cálculo (taxa/desconto/saldo) passa por ele. `decimal` sempre, nunca `float`/`double`.
- **Taxa de cartão**: derivada da config no ato do pagamento, gravada em `Pagamento.taxa`, **informativa** ("você recebe R$ X") — não reduz o saldo da cobrança (é custo do estabelecimento).
- **Badge na agenda**: estado de pagamento vem **agregado na query da agenda** (join por `agendamento_id`) — nunca request por linha (anti-N+1).
- **Permissões novas** `financeiro_paciente.ver`/`financeiro_paciente.registrar` (catálogo) — separadas de `financeiro.*` (clínica agregada).

---

## Conexão Postgres (RDS)

- Connection string normal Npgsql em `ConnectionStrings:Default` (runtime) e `ConnectionStrings:Migrations` (autoria de migrations via `dotnet ef`).
- Em prod, host/senha vêm do AWS SSM Parameter Store (`/imedto/dev/db-host`, `/imedto/dev/db-password`); a EC2 lê via IAM role.
- Em dev, o valor fica em `appsettings.Development.json` (gitignored).
- O `AppDbContextFactory` usa apenas `ConnectionStrings:Migrations` para o tooling do `dotnet ef`.

> Detalhes de infra do RDS (endpoint, SG, backup) em [INFRA.md](INFRA.md). Comandos de migration em [COMANDOS.md](COMANDOS.md).
