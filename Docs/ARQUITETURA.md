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
- **`QuestPdfTermoService`** (`Infrastructure/Termos/`) — PDF probatório do termo de consentimento emitido. Endpoint: `GET /api/termos/{id}/pdf-gerado`. Conteúdo: cabeçalho institucional + bloco do paciente + snapshot HTML da versão aceita + bloco de evidência + marca d'água por status. **Desde o briefing 2026-06-12_002 (termo físico-primeiro)**, o bloco de evidência degrada por estado: termo `Assinado` por **documento físico** (foto/PDF anexado) exibe o **hash do PDF anexado** como evidência de integridade, **sem** token/IP de aceite público; termo legado `Assinado` por link (histórico) preserva a evidência antiga (hash SHA-256 + últimos 6 chars do token — nunca o token completo). QuestPDF também é usado para **converter foto (JPG/PNG) em PDF multi-página** no anexo de termo físico (ver seção de Termos físicos).

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
- **`POST /api/admin/auth/change-password`** — atende **dois fluxos** sob a mesma rota e policy (`ImedtoAdminChangePassword`); rate limit `auth-sensitive` (3 req/janela):
  1. **Força-reset** (`must_reset_password = true` no token): `SenhaAtual` ignorada mesmo se enviada; audit `RESET_SENHA_PROPRIA`.
  2. **Troca voluntária** (token regular, sem `must_reset_password`): `SenhaAtual` obrigatória e validada via `IPasswordHasher.Verificar`; nova ≠ atual obrigatório; audit `ALTERAR_SENHA_PROPRIA`.
  Em ambos os fluxos: revoga todas as sessões (`RevogarTodosDoAdminAsync`) e reemite access + refresh atualizados (admin permanece logado). A distinção é feita **no handler pela claim** — nunca pela policy nem pela presença do campo `SenhaAtual` no body.
- **Políticas de autorização**:
  - `ImedtoAdmin` — requer `imedto_admin = "true"` **e** ausência de `must_reset_password = "true"`.
  - `ImedtoAdminChangePassword` — requer `imedto_admin = "true"` (permite `must_reset_password`). Rota `change-password` permanece aqui para aceitar ambos os fluxos.
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

### Assinaturas/Planos — Modelo Unificado (briefing 2026-06-11_003, F1–F6 CONCLUÍDAS)

> **Fonte única de verdade**: `imedto_assinaturas` / `imedto_planos` (uuid, `Domain.Admin`). A estrutura legada (`assinaturas`/`planos`, bigint, `Domain.Assinaturas`) está **descontinuada e read-only desde F6** (2026-06-11): nenhum código de domínio escreve nela. Drop físico das tabelas é fase posterior. `ExpirarTrialsJob` e `SeedPlanosHostedService` legados foram removidos em F6.

**Estado derivado (nunca enum setável):**

O estado efetivo de um estabelecimento é derivado da assinatura vigente (`fim_em IS NULL`):
- **VITALÍCIO**: `expira_em IS NULL` e `suspensa_em IS NULL` → LIBERADO.
- **TEMPORÁRIO**: `expira_em` no futuro e `suspensa_em IS NULL` → LIBERADO até a data.
- **SUSPENSÃO**: `suspensa_em` preenchido → BLOQUEADO (reversível via `Reativar()`).
- **EXPIRADO**: `expira_em` no passado → BLOQUEADO.
- **BLOQUEADO**: sem assinatura vigente → BLOQUEADO.

Método de domínio: `ImedtoAssinatura.EstaAtiva()` / `ObterEstado()`. O enforcement (`AssinaturaService` + `[RequiresAssinaturaAtiva]` + `[FeatureGate]`) lê exclusivamente da estrutura nova.

**Histórico imutável:** mudar plano/estado = INSERT nova linha + `FecharVigencia()` na anterior (exceto suspender/reativar que muta a própria vigência). NUNCA UPDATE in-place do estado.

**Features e limites no plano (`imedto_planos.features_json`):**

8 chaves booleanas: `receitas`, `exame_fisico`, `procedimentos_cirurgicos`, `orcamento_completo`, `ia`, `relatorios_avancados`, `automacoes_ilimitadas`, `anexos_ilimitados`. Limites em `limites_json` (`{"profissionais":N,"pacientes":N}`; ausente/NULL = ilimitado). `[FeatureGate]` e `LimiteAtingidoAsync` consultam o plano vigente da estrutura nova. As constantes de chave de feature vivem em `Domain.Assinaturas.Features` (classe compartilhada — não remover).

**Config de trial (`imedto_config_trial` — singleton):**

Entidade `ImedtoConfigTrial` (id fixo `10000000-0000-0000-0000-000000000001`) governa o trial automático na criação de estabelecimento (F5): `plano_trial_id`, `duracao_trial_dias` (default 14), `trial_habilitado` (bool). Quando `trial_habilitado=false`, novo estabelecimento nasce sem assinatura vigente (BLOQUEADO).

**Seed mínimo:** apenas "Gratuidade Vitalícia" (UUID fixo `00000000-0000-0000-0000-000000000001`) com todas as features habilitadas e limites ilimitados. Planos pagos criados pelo dono via CRUD admin.

**Porta de integração futura (`IProvedorAssinaturaExterna` — contrato, sem implementação):**

Interface em `Application.Admin.Assinaturas`. Nenhum adapter concreto existe — zero SDK, zero DI obrigatório. Quando o gateway for implementado, o adapter concreto vai em `Infrastructure/` (espelhando `BirdIdAssinaturaProvider`, `ResendEmailProvider`, `S3StorageProvider`). As colunas dormentes em `imedto_assinaturas` (`origem`, `referencia_externa`, `status_cobranca`) foram desenhadas para receber os dados do gateway.

**Fases de entrega:** F1 (schema + porta) → F2 (backfill seguro) → F3 (enforcement) → F4 (admin UI) → F5 (trial automático na nova) → **F6 (legada read-only — CONCLUÍDA)**. Drop físico das tabelas `assinaturas`/`planos` é fase autônoma posterior (sem data definida).

**Endpoint `GET /api/minha-assinatura` — gap do épico fechado (hotfix 2026-06-11):**

`ObterMinhaAssinaturaQueryHandlers` agora lê `imedto_assinaturas + imedto_planos` via `MinhaAssinaturaQueryRepository` (Dapper). O estado derivado (status no vocabulário do front: `Ativa`/`Trial`/`Suspensa`/`Expirada`) usa a mesma lógica de `ImedtoAssinatura.ObterEstado()`:
- `expira_em IS NULL` e `suspensa_em IS NULL` → **"Ativa"** (vitalício, não bloqueado).
- `expira_em` no futuro e `suspensa_em IS NULL` → **"Trial"** (não bloqueado, `diasRestantes` preenchido).
- `suspensa_em` preenchido → **"Suspensa"** (bloqueado).
- `expira_em` no passado → **"Expirada"** (bloqueado).
- Sem vigência (`fim_em IS NULL` não encontrado) → retorna `null` → 404 (bloqueado).

Paridade garantida: liberado no enforcement (`EstaAtiva()`) ⟺ não-bloqueado no front (`isBlocked = status in {Expirada, Suspensa, Cancelada}`).

**Código legado remanescente (intencionalmente mantido):**
- `Domain.Assinaturas.{Assinatura, Plano, StatusAssinatura}` + EF configs + `AppDbContext.Assinaturas/Planos` + `AssinaturaQueryRepository`: não mais consumidos pelo endpoint `GET /api/minha-assinatura`. `AssinaturaQueryRepository` permanece registrado como singleton mas não injetado em nenhum handler ativo. Serão removidos junto com o drop físico das tabelas. `GET /api/planos` ainda lê a tabela legada `planos` via `PlanoQueryRepository` (catálogo do usuário — drop em fase posterior).
- `Domain.Assinaturas.Features`: constantes de chave de feature compartilhadas com o enforcement novo — não é legado.

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
| Modelo de descrição cirúrgica | `modelos_descricao_cirurgica` | `eh_padrao_sistema=true`, `estabelecimento_id=NULL` | WHERE `eh_padrao_sistema=true OR estabelecimento_id=@X` | Live-link |

#### Modelos de descrição cirúrgica (briefing 2026-06-13_002)

Aggregate `ModeloDescricaoCirurgica` (bounded context: Prontuários). Armazena templates de texto livre reutilizáveis para a seção `desc-cirurgica` do prontuário.

**Aggregate root:** `Domain/Prontuarios/ModeloDescricaoCirurgica.cs`
- Props: `Titulo` (≤200 chars), `Corpo`, `EstabelecimentoId` (nullable — NULL para padrão-sistema), `EhPadraoSistema`, `Ativo`.
- Fábricas: `CriarDoEstabelecimento(long, string, string)` e `CriarPadraoSistema(string, string)`.
- Métodos: `Editar(titulo, corpo)` (guard: `EhPadraoSistema` → `BusinessException`), `Inativar()`, `Reativar()`.
- Dedup: `ExisteOutroComMesmoTitulo` aplica `NormalizadorPool.Normalizar` em memória (mesma convenção do pool de variáveis — sem unaccent Postgres).

**Endpoints (`ProntuarioTemplateController`, `/api/prontuario/modelos-cirurgia`):**
- `GET` — leitura aberta (qualquer membro do tenant); inclui padrão-sistema + do estabelecimento.
- `POST`, `PUT`, `DELETE` — exigem `[RequiresPermissaoExtra(PermissoesExtras.ModelosProntuario)]`.

**Multi-tenant:** `IModeloDescricaoCirurgicaRepository.ObterPorIdOuNulo(id, estabelecimentoId)` — jamais retorna registro de outro tenant nem padrão-sistema para operações de escrita. Mensagem de erro genérica ("não encontrado").

**Frontend:** `SeletorTemplateCirurgico.vue` aplica template client-side (`novaEvolucao["desc-cirurgica"] = corpo`) — zero request adicional ao aplicar. Confirmação de substituição via `AppConfirmDialog` quando campo já tem conteúdo. Gerenciado em `modeloDescricaoCirurgicaService.ts`.

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

#### Seção `procedimentos-indicados` ligada ao catálogo (briefing 2026-06-10_011 — Financeiro F3)

A seção `procedimentos-indicados` do `ConteudoJson` deixou de ser texto livre e passou a referenciar o **catálogo de procedimentos do estabelecimento ativo** (`CatalogoCirurgia` / `orcamento_catalogo_cirurgia`). O formato dentro do `jsonb` aceita **dois tipos de item por lista, distinguidos pela presença de `catalogoCirurgiaId`**:

- **Novo (catálogo):** `{ catalogoCirurgiaId, descricao, valor, observacao }` — `descricao` e `valor` são **snapshot** copiado do catálogo no momento da indicação.
- **Legado (texto livre):** `{ descricao, observacao }` sem `catalogoCirurgiaId` — evoluções antigas coexistem e renderizam read-only sem conversão.

Premissas: (1) a referência ao catálogo é guardada **como dado no JSON, não como FK relacional** — preserva a imutabilidade append-only da evolução mesmo que o item do catálogo seja editado/removido depois; o render nunca re-resolve o valor pelo `catalogoCirurgiaId` (sempre snapshot). (2) Leitura/criação do catálogo reusa `orcamentoCatalogoService.listarProcedimentos`/`criarProcedimento` (endpoint `api/orcamentos/configuracoes/procedimentos`), já filtrado por tenant e sob o gate `[FeatureGate(OrcamentoCompleto)]` + `[RequiresAcao("orcamento","configurar")]` + `[RequiresPapel(Dono,Recepcionista)]` — sem endpoint paralelo. (3) Sem migration (campo é `jsonb`). `catalogoCirurgiaId` é contrato consumido por F4 (cobrança de procedimento) e F5 (orçamento pré-preenchido).

#### Seção `conduta` como checklist → pendências do atendimento (briefing 2026-06-10_012 — Financeiro F3B)

A seção `conduta` do `ConteudoJson` passou de texto livre para **checklist de 6 ações fixas** (`tipo: "conduta_checklist"`) com campo de observação livre. Cada ação marcada gera uma `PendenciaAtendimento` no mesmo fluxo de salvamento da evolução.

**Entidade `PendenciaAtendimento`** (`pendencias_atendimento`):
- Campos: `estabelecimento_id`, `paciente_id`, `evolucao_id`, `agendamento_id` (null se evolução sem consulta), `acao` (`AcaoPendencia` enum: CriarReceita, CriarAtestado, PedirExame, CriarOrcamento, MarcarProcedimentoRealizado, AgendarRetorno), `status` (Pendente/Concluida), `referencia_id` (id da entidade que concluiu automaticamente), `concluida_em`, `criado_por_usuario_id`, `criado_em`, `atualizado_em`.
- UNIQUE `(evolucao_id, acao)` — idempotência total; índice `(estabelecimento_id, paciente_id, status)` — listagem eficiente.

**Criação — padrão falha-suave:** `PendenciaExtratorEvolucao` (injetado em `RegistrarEvolucaoCommandHandler` após `PoolExtratorEvolucao`) lê `conteudoJson["conduta"]["acoesMarcadas"]`, verifica `ExistePorEvolucaoEAcao` antes de inserir (idempotência dupla: UNIQUE + cheque em memória), nunca lança exceção ao chamador — falha silenciosa via try/catch.

**Conclusão automática — fan-out de eventos:** 5 `IEventHandler` scoped escutam eventos existentes:
| Evento | Handler | Ação concluída |
|---|---|---|
| `ReceitaEmitidaEvent` | `ConcluirPendenciaAoEmitirReceitaHandler` | CriarReceita |
| `AtestadoEmitidoEvent` | `ConcluirPendenciaAoEmitirAtestadoHandler` | CriarAtestado |
| `PedidoExameEmitidoEvent` | `ConcluirPendenciaAoEmitirPedidoExameHandler` | PedirExame |
| `OrcamentoCriadoEvent` | `ConcluirPendenciaAoCriarOrcamentoHandler` | CriarOrcamento |
| `AgendamentoCriadoEvent` | `ConcluirPendenciaAoCriarAgendamentoHandler` | AgendarRetorno (só se `InicioPrevisto > UtcNow`) |

Cada handler chama `ObterAbertaMaisRecentePorAcao` (busca a pendência mais recente do par estab+paciente+ação em status Pendente) → `ConcluirPorGatilho(referenciaId)` → `Salvar`. No-op se não encontrar. Fan-out no bus: event bus já suporta múltiplos handlers para o mesmo evento.

**Conclusão manual:** `POST /api/paciente/{id}/pendencias/{pendenciaId}/concluir` (RBAC: `prontuario.editar`). `ConcluirManualmente()` seta `ReferenciaId=null` (distingue do automático). UI exige confirmação inline. **Exceção (F4):** a ação `MarcarProcedimentoRealizado` **não** usa mais esse fluxo manual — passou a ter endpoint/handler próprios (ver F4 abaixo); as outras 5 ações continuam pelo manual/automático.

**Gatilho `MarcarProcedimentoRealizado` → cobrança + baixa de estoque (briefing 2026-06-10_013 — Financeiro F4):** marcar o procedimento como realizado dispara, no mesmo `UnitOfWork` (atômico), três efeitos: **(a)** cria `Cobranca` origem=`Procedimento`, tipo=Particular, `valor_cobrado` = soma dos `valor` dos procedimentos-indicados do `ConteudoJson` da evolução (snapshot F3 — **não** re-resolve o catálogo), `evolucao_id` setado; **(b)** baixa automática de estoque — resolve produtos vinculados aos `catalogoCirurgiaId` via `orcamento_catalogo_cirurgia_produto` + `ProdutosConsolidador` (MAX uso único / SOMA múltiplo) e chama `ItemInventario.RegistrarSaida` por produto (`MovimentacaoEstoque` de saída com `custo_unitario` = snapshot do `CustoMedio` → alimenta o custo real da F7); **(c)** conclui a pendência via `ConcluirPorGatilho(cobranca.Id)`. **Idempotência dupla:** pendência `Concluida` ⇒ no-op + índice UNIQUE parcial `cobrancas (evolucao_id) WHERE origem='Procedimento'` (race fecha no banco, 23505 → 422). **Estoque insuficiente** (`RegistrarSaida` lança) faz rollback total — bloqueia, sem saldo negativo. **Evolução sem procedimentos** → 422 (não conclui sem cobrança). RBAC: `prontuario.editar`. Sem desfazer nesta fase (pagamento indevido = estorno F2). Badge na agenda e aba Financeiro reusam F1/F2 sem código novo. **Vínculo produto↔estoque (addendum 2026-06-10_013):** a baixa resolve o `ItemInventario` via `orcamento_catalogo_produto.item_inventario_id` — vínculo **opcional** configurável na aba Produtos do catálogo de orçamento, espelhando `orcamento_catalogo_implante.item_inventario_id` (FK fraca `ON DELETE SET NULL`); produto **sem** vínculo é ignorado na baixa (sinalizado no preview do modal), produto **com** vínculo gera a movimentação. RBAC da config do vínculo: `orcamento.configurar` (distinto do `prontuario.editar` da baixa).

**Retrocompat da seção:** evolução com `conduta` como string simples renderiza read-only sem checkboxes (CA73) — o dispatcher `SecaoProntuario.vue` detecta via `ehCondutaLegado computed`. Novas evoluções sempre persistem `{ acoesMarcadas, observacao }` com `tipo: "conduta_checklist"`.

**Frontend:** `SecaoCondutaChecklist.vue` (dispatcher) → `ProximosPassosModal.vue` (pós-salvar, abre se ≥1 ação marcada) → `PainelPendencias.vue` (painel persistente na aba Resumo de `PacienteDetalheView.vue`, invisível se sem pendências).

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

### Job: expirar-agendamentos-nao-finalizados

`ExpirarAgendamentosNaoFinalizadosJob` — `Nome = "expirar-agendamentos-nao-finalizados"`, intervalo `86400` (1×/dia, 03:00 BRT / 06:00 UTC). Varre cross-tenant (global) os agendamentos em status `Agendado` ou `Confirmado` cujo `inicio_previsto` caia na janela de ontem 00:00 BRT até hoje 00:00 BRT (D-1, calculada via `TimeZoneInfo "America/Sao_Paulo"`). Para cada item chama `Agendamento.ExpirarPorFimDoDia(motivo)` — **método de domínio silencioso (sem evento)**, diferente de `Cancelar()`. Processa em lotes de 200. Log agregado por `{ estabelecimento_id, quantidade, timestamp }` sem PII. Janela estritamente D-1: se o job falhar num dia, os registros daquele dia não são varridos em execuções futuras (risco aceito em produto — §8 briefing 2026-06-19_001).

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

### Lembrete de consulta — canal WhatsApp (briefing 2026-06-18_005)

O lembrete de consulta tem **dois canais complementares e independentes** no mesmo job recorrente — **não há mecanismo paralelo**. O `AutomacaoJob` (BackgroundService, `PeriodicTimer` 1h) chama `EnviarLembretesAgendamentosCommandHandler`, que para cada agendamento devido envia o **e-mail** (comportamento histórico, intocado) e **também o WhatsApp** quando todos forem verdadeiros: `configuracoes_automacao.lembretes_habilitados = true` **E** `lembretes_whatsapp_habilitados = true` **E** paciente com opt-in WhatsApp **E** telefone E.164 válido. Faltando qualquer condição, o canal WhatsApp é pulado em silêncio (sem PII) e o e-mail segue. Controle de envio independente por canal: `agendamentos.lembrete_por_whatsapp_enviado` espelha `lembrete_por_email_enviado` — e **ambos** são resetados no mesmo ponto de `Agendamento.Atualizar()` no reagendamento (R1/R6 acima), reabrindo os dois canais. Falha de entrega de um paciente não marca o flag (retry na próxima rodada), loga só hash SHA-256 truncado do destinatário (sem PII) e não derruba o lote nem o e-mail.

**Provider atrás de porta (ports & adapters) — espelho exato do provider de e-mail.** Porta `IWhatsappService` (Application/Domain); adapters `MetaWhatsappService` (Meta WhatsApp Cloud API, oficial) e `NoOpWhatsappService` (loga hash, não envia), selecionados por config `Whatsapp:Provider` via factory condicional no `Container.cs` — o mesmo padrão do switch `Email:Provider` (`ses`/`resend`/NoOp). O handler conhece **apenas** `IWhatsappService`: nenhum tipo, URL ou payload da Meta existe fora de `Infrastructure/`. Credenciais (token, Phone Number ID, WABA ID) vêm de appsettings/SSM (`Whatsapp:*`), configuradas pelo admin global — nunca expostas ao cliente. O toggle de canal por estabelecimento vive na MESMA tela de Automação (`AutomacoesView.vue`), sob a MESMA permissão `automacao_config` do toggle de e-mail. Template Meta categoria **Utility**; corpo **sempre identifica o estabelecimento de origem** (`{{nome_estabelecimento}}`) — multi-tenant correto.

**Fronteira teste-vs-produção (MVP sem CNPJ).** No MVP o provedor roda em **modo teste/sandbox da Meta**: número e template de teste, com envio real chegando **só** aos até 5 números cadastrados como teste (números fora da allowlist retornam erro do provedor, tratado como falha de envio normal). A virada para produção (Business Verification, número/template de produção, volume ilimitado, número por estabelecimento, e novos eventos como confirmação/cancelamento) é destravada **somente por configuração/credenciais** (`Whatsapp:*` no SSM) — **sem reescrita** de Domain, Handler, contrato ou migration.

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

**Frontend**: rota anônima `/agendamentos/confirmar/:token` → `ConfirmarPresencaPublicaView`. Sem login, sem menu, mobile-first. (O espelho original de Termos — `AceiteTermoPublicoView` — foi **removido** no briefing 2026-06-12_002; a confirmação de presença de agendamento permanece como o único fluxo público anônimo por token.)

---

### Termos de consentimento — físico-primeiro (briefing 2026-06-12_002)

O termo de consentimento é **assinado fisicamente** (paciente presente) e arquivado digitalmente. O **aceite por link público foi removido** por completo: não há mais `TermoPublicoController`, query por token, e-mail de link, reenvio, recusa pública nem expiração por link. Para termos novos, `AssinaturaTipo` é só **documento físico** — o valor `PdfAnexado` é mantido no schema (sem migrar histórico) e passa a significar "foto convertida ou PDF assinado". Transição de estados nova: **`Pendente → Assinado → Revogado`** (os estados `Recusado`/`Expirado` permanecem no enum apenas para leitura do histórico legado, incluindo os `AceiteLink` `Pendente` migrados para `Expirado` por migration de transição idempotente).

- **Anexo de documento** (`POST /api/termos/{id}/pdf`, gate `[RequiresAcao("termos", "emitir")]`): aceita `application/pdf`, `image/jpeg` e `image/png`. Quando o upload é imagem(ns), o backend **converte para PDF multi-página via QuestPDF** (1 imagem/página; frente+verso = 2 páginas), calcula o **SHA-256 do PDF resultante** e segue o fluxo `PdfAnexado` (upload no S3 → `TermoEmitido.AnexarPdf` → status `Assinado`). PDF direto não é convertido. Validação por **magic bytes reais** (PDF `%PDF-`, JPG `\xFF\xD8\xFF`, PNG `\x89PNG…`); **HEIC é rejeitado** (backlog). Path no S3 por GUID, sem PII. Reusa `AnexarPdfTermoCommandHandler` (estendido), `ITermoPdfStorageService`, `ITermoAuditLogger` (`termo-pdf-anexado`).
- **Emitir + anexar pela evolução**: o termo pode ser emitido e anexado **dentro da evolução do prontuário** (reusando o passo de seleção de modelo do `EmitirTermoModal`), com vínculo `EvolucaoId`. O documento aparece **na timeline da evolução e na aba de Termos do paciente**, apontando para o **mesmo objeto S3** (binário não duplicado). O anexo pela evolução audita em **dois trilhos**: `termo_audit_log` (`termo-pdf-anexado`) **e** `prontuario_acesso_log` (`Escrita`, via `IProntuarioAcessoLogService`), ambos best-effort. A forma do vínculo (`evolucao_id` em `termos_emitidos` vs. `ProntuarioAnexo` espelho) está registrada no PR da entrega.

## Área de domínio: Cobranças (Financeiro / contas a receber) — briefing 2026-06-10_009 (F1)

Introduz contas a receber do paciente (`cobrado ≠ pago`). **Não confundir** com a área `financeiro.*` (visão agregada da clínica, `Lancamento` avulso) — Cobranças é a conta a receber **por paciente**.

### Agregados
- **`Cobranca`** (aggregate root) — conta a receber: `estabelecimento_id` (tenant), `paciente_id`, `origem` (`Consulta` na F1; enum aberto para `Procedimento`/`Cirurgia` em F4/F5), `tipo_atendimento` (`Particular`|`Convenio`), `agendamento_id?`, `orcamento_id?` (F5), `convenio_id?` (F6), `valor_cobrado`, `desconto`, `status`.
- **`Pagamento`** (entidade filha) — quita a cobrança; reusa `FormaPagamento`. **Imutável** por design (estorno com histórico na F2/INV-7 — nunca update/delete de valor).
- **`EstornoPagamento`** (entidade de histórico — briefing 2026-06-10_010 / F2) — desfaz um `Pagamento` **sem apagá-lo**: `pagamento_id`, `cobranca_id`, `estabelecimento_id`, `valor` (total — 1 estorno anula 1 pagamento por inteiro nesta versão), `motivo` (obrigatório), `lancamento_estorno_id`. O `Lancamento` de estorno reusa as colunas `cobranca_id`/`pagamento_id` com **valor negativo** + categoria de estorno (sem flag/coluna nova em `lancamentos`).
- Config: `TabelaPrecoConsulta` (valor sugerido no check-in) e `ConfigTaxaFormaPagamento` (taxa de cartão por forma, aplicada automaticamente — vive na aba de Config do Financeiro, padrão master-detail de `OrcamentoSettingsView.vue`).
- **Porta única de domínio** (F2): o badge na agenda e a **aba Financeiro do paciente** (`PacienteDetalheView.vue`) operam a **MESMA** `Cobranca` via o mesmo `CobrancaController`/store/service — registrar/estornar numa porta repercute na outra, sem duplicidade.

### Invariantes (validadas no Domain, 422 via `BusinessException`)
- **INV-1**: `SUM(pagamentos.valor) ≤ valor_cobrado − desconto`.
- **INV-2**: `status` derivado (Aberta/ParcialmentePaga/Paga) da soma **líquida** (pagamentos − estornos futuros); nunca setado à mão salvo `Cancelada`.
- **INV-3**: registrar `Pagamento` **gera `Lancamento`** (Receita/Pago, `cobranca_id`+`pagamento_id`) na **MESMA transação** (atômico — rollback de ambos em falha).
- **INV-4**: `0 ≤ desconto ≤ valor_cobrado`. **INV-5**: cada `Pagamento.valor > 0`. **INV-6**: `Cobranca` sempre com `estabelecimento_id`+`paciente_id`.
- **INV-7** (F2): estornar um `Pagamento` **gera `EstornoPagamento` + `Lancamento` de estorno** (valor negativo) na **MESMA transação** (atômico — rollback de ambos em falha); o `Pagamento`/`Lancamento` originais permanecem imutáveis; o `status` recalcula pela soma líquida (pagamentos − estornos). 1 estorno (total) por pagamento (unique `pagamento_id` em `estorno_pagamentos`).
- **INV-8** (RBAC): só aplica desconto quem tem `orcamento.aprovar` OU `financeiro.lancar`/`financeiro_paciente.registrar` OU é Dono.

### Cobrança de cirurgia na aprovação do orçamento (briefing 2026-06-10_014 — Financeiro F5)
- **Cobrança nasce na aprovação**: `Orcamento.Aprovar()` emite `OrcamentoAprovadoEvent(OrcamentoId, EstabelecimentoId, PacienteId, Total)`; o handler `OrcamentoAprovadoEventHandler` (antes placeholder vazio) cria `Cobranca.CriarParaCirurgia(orcamento_id, valor_cobrado=Total)`, origem=`Cirurgia`, tipo=Particular, `agendamento_id=null`. **Atômico** com a aprovação — o `EfUnitOfWorkScope` despacha domain events **dentro da transação** (após o 1º `SaveChanges`, antes do commit), então falha na cobrança reverte a aprovação inteira. Este é o seam designado para integração financeira de orçamento — não fazer inline no command handler.
- **Idempotência dupla** (1 cobrança ativa por orçamento): o handler consulta `ICobrancaRepository.ObterPorOrcamentoOuNulo` (se já existe, sincroniza valor em vez de duplicar) + índice **UNIQUE parcial** `cobrancas (orcamento_id) WHERE origem='Cirurgia' AND orcamento_id IS NOT NULL` (espelha o `ux_cobrancas_evolucao_procedimento` da F4; 23505 tratado como já-existe).
- **Histórico de valor** (`CobrancaHistoricoValor` — entidade filha de `Cobranca`): `Cobranca.SincronizarValorCobrado(novoTotal, alteradoPor)` grava `{valor_anterior, valor_novo, alterado_por, alterado_em}` **só quando o valor muda** (no-op em igualdade), atualiza `valor_cobrado` e recalcula `status` pela INV-2 sobre o novo total. **Bloqueio** (422): reduzir abaixo do `TotalPagoLiquido()` é proibido (estornar primeiro). Popula o bloco `HistoricoValorAbaDto` que a F2 já desenha (substitui o `Array.Empty` em `CobrancaQueryRepository`). **Gancho preparado**: hoje `Orcamento` não permite editar/re-aprovar status Aprovado (`GarantirEditavel` exige Rascunho/Enviado), então a sincronização só dispara em re-emissão do evento — a infra fica pronta para um fluxo de edição-pós-aprovação futuro (não inventado nesta fase).
- **Pré-preenchimento do orçamento pela evolução**: o link `CriarOrcamento` da conduta (F3B) navega para `/orcamentos/novo?evolucaoId=&pacienteId=` (**query param** — sobrevive a refresh; mesmo precedente de `?agendamentoId=`). Um endpoint de leitura leve devolve os `procedimentos-indicados` da evolução (snapshot `{catalogoCirurgiaId, descricao, valor}` do `ConteudoJson` F3, filtrado por tenant) que o `OrcamentoFormView` mapeia em linhas de `OrcamentoCirurgia` — POST só no salvar. A pendência conclui pelo `ConcluirPendenciaAoCriarOrcamentoHandler` **genérico existente** (sem FK orçamento↔evolução, sem alterar o `CriarOrcamentoCommand`).
- **Pagamento de cirurgia só pela aba Financeiro do paciente**: cobrança origem=Cirurgia tem `agendamento_id=null`, logo o badge agregado da agenda (join por `agendamento_id`) não a alcança naturalmente — sem exclusão explícita. RBAC da aprovação reusa `[RequiresAcao("orcamento","aprovar")]` — **nenhuma permissão nova**. Baixa de estoque da cirurgia ocorre na F4 (marcar procedimento realizado), **não** na aprovação.

### Padrões
- **Arredondamento monetário**: helper **único** de domínio — 2 casas, `MidpointRounding.AwayFromZero`. Todo cálculo (taxa/desconto/saldo) passa por ele. `decimal` sempre, nunca `float`/`double`.
- **Taxa de cartão**: derivada da config no ato do pagamento, gravada em `Pagamento.taxa`, **informativa** ("você recebe R$ X") — não reduz o saldo da cobrança (é custo do estabelecimento).
- **Badge na agenda**: estado de pagamento vem **agregado na query da agenda** (join por `agendamento_id`) — nunca request por linha (anti-N+1). Cobrança de cirurgia (`agendamento_id=null`) não aparece no badge (F5).
- **Permissões novas** `financeiro_paciente.ver`/`financeiro_paciente.registrar` (catálogo) — separadas de `financeiro.*` (clínica agregada). Aprovação de orçamento de cirurgia reusa `orcamento.aprovar` (F5 — sem permissão nova).

### Convênio: estrutura base (briefing 2026-06-10_016 — F6)

#### Aggregates
- **`Convenio`** (aggregate root) — operadora de convênio do estabelecimento: `estabelecimento_id` (tenant), `nome`, `registro_ans?`, `ativo`. Possui `ConvenioPlano` (filhos 1:N via root): `convenio_id`, `estabelecimento_id` (denormalizado para filtro multi-tenant direto), `nome`, `ativo`.
- **`PacienteConvenio`** (carteirinha — aggregate próprio) — associação paciente↔convênio por estabelecimento: `paciente_id`, `estabelecimento_id`, `convenio_id` (FK → convênios do mesmo tenant), `plano_id?`, `numero_carteirinha`, `validade?`, `ativo`. Paciente pode ter N carteirinhas (convênios diferentes); `plano_id`, se informado, deve pertencer ao `convenio_id` (validado no handler). `numero_carteirinha` é **dado pessoal** — DTO minimizado, sem PII em log.

#### Soft-delete (inativar > excluir)
`Convenio`/`ConvenioPlano` usam `ativo=false` como ação de remoção preferida — preserva a integridade de cobranças e carteirinhas que referenciam o id. Convênio inativo some dos selects de novas seleções mas continua legível no histórico. Exclusão física permitida apenas se sem uso (sem carteirinha nem cobrança no tenant); caso contrário 422 "Convênio em uso — inative em vez de excluir."

#### Populamento de `cobrancas.convenio_id` no check-in
`Cobranca.CriarParaConsulta` ganhou parâmetro `convenioId` opcional. Quando o check-in confirma `tipo=Convenio`, passa o id do convênio selecionado (pode ser `null` — convênio é opcional no check-in). O handler valida que o `convenio_id` aponta para um convênio **ativo do mesmo estabelecimento** (404 genérico se inválido ou de tenant alheio). `valor_cobrado=0` e bloqueio de pagamento/estorno de balcão permanecem inalterados (R9/INV da F1).

#### Guia / Autorização
`Cobranca.RegistrarGuia(numero, senha?, autorizadaEm?)` grava os campos `guia_numero`/`guia_senha`/`guia_autorizada_em` (3 colunas adicionadas na tabela `cobrancas` pela F6). Estado é **derivado** no DTO: "pendente" se `guia_numero` é null/vazio; "preenchida" se presente. Só válido para cobrança `tipo=Convenio` — 422 "Guia só disponível para cobranças de convênio." para Particular. RBAC: `financeiro_paciente.registrar` (mesmo gate do registro de pagamento).

#### Alerta de validade vencida (R6)
Calculado exclusivamente no **front** a partir do campo `validade` (`string | null` ISO date) via helper `estaVencida(validade)` em `convenioService.ts`. O backend nunca expõe campo derivado `validadeVencida` — minimização de DTO. O alerta é **informativo** (não bloqueia check-in, cadastro nem faturamento).

#### Navegação em Configurações
Seção `convenios` adicionada ao grupo **"Faturamento"** (renomeado de "Financeiro") do master-detail de `EstabelecimentoView.vue`. Visibilidade: `convenios.ver`. Deep-link `?secao=convenios`. Conteúdo: `ConveniosConfigView.vue` carregado via `defineAsyncComponent` (padrão dos outros painéis lazy). A seção `financeiro` (`id`) e seu deep-link permanecem inalterados.

#### "Em breve" (Coparticipação / Conciliação / Glosas)
Cards visuais com selo "Em breve" nas abas de configuração e na aba Convênios do paciente. **Sem schema** — não há tabelas de coparticipação, conciliação nem glosas nesta fase.

### Consolidação F7: caixa diário + comissões + custo/lucro + redesign do `/financeiro` (briefing 2026-06-11_001)

Fase final do épico. Substitui a `FinanceiroView.vue` temporária por uma tela de **4 abas** (Visão geral/Extrato · Caixa diário · Comissões · Configurações) lendo de `Lancamento` (base inalterada). **Toda agregação no backend** (Dapper `SUM`/`GROUP BY`) — nenhuma coleção bruta somada no front; lazy por aba (consulta só na aba clicada).

#### Caixa diário (`CaixaDiario`)
- Aggregate **por estabelecimento + data** (índice UNIQUE `(estabelecimento_id, data)`). Estados: NãoAberto (sem registro) → Aberto → Fechado; reabrir volta a Aberto. Campos: `aberto_por/em`, `fechado_por/em`, `reaberto_por/em?`, `observacao?`, `status`.
- **Resumo do dia lido on-the-fly** de `Lancamento` `Status=Pago` com `DataPagamento = data`, agrupado por forma de pagamento + estornos (negativos do dia) + total líquido. O caixa **não materializa valores** — fechar só congela o status/selo, nunca os números (evita divergência). Lançamento retroativo em dia fechado **não é bloqueado** (caixa é ritual de conferência, não trava operação).
- **RBAC**: abrir/fechar exige **`financeiro.fechar`** (ação **já existente** no `CatalogoPermissoes.cs`, área `financeiro` = `{ver, lancar, fechar}` — Dono a tem via AdminPadrao; **nenhuma ação nova**). **Reabrir** exige ser **Dono** (mais restrito) + audit (`reaberto_por/em`).
- **Isolamento multi-tenant absoluto**: caixa do estabelecimento A jamais visível em B, mesmo para o mesmo usuário multi-vínculo — contexto = estabelecimento ativo (claim de tenant), repositório falha-fechada, 404 genérico cross-tenant.

#### Comissões (`ConfigComissaoProfissional`)
- Config por `(estabelecimento_id, profissional_usuario_id, tipo∈{Consulta,Procedimento})` → `percentual` (só percentual; sem `valor_fixo`). **Default de sistema 30%** quando sem config (fallback no cálculo, não persistido). UI de edição na **Equipe** (`ProfissionalDetalhesModal.vue`, aba Perfil); editar é do **Dono**.
- **Cálculo regime caixa** (sobre pagamentos **recebidos** no período, coerente com o extrato): consulta/procedimento = `valor_recebido × percentual`; **cirurgia usa `OrcamentoEquipe`** (valor absoluto por profissional, **não** % config) **rateado** pela proporção `recebido_no_periodo / valor_cobrado`. Arredondamento via `ArredondamentoMonetario`.
- **Comissão é visão/relatório — NÃO gera `Lancamento`** de despesa (anti-escopo). O repasse efetivo, se desejado, é lançamento avulso manual.

#### Custo/lucro por paciente (Relatórios)
- A aba Financeiro de `RelatorioFinanceiroTab.vue`/`RelatorioFinanceiroQuery` ganha visão **aditiva** por paciente (cobrado · pago · desconto · taxa · custo · lucro) — KPIs/breakdown atuais inalterados (retrocompat).
- **Custo de insumo** via `movimentacoes_estoque.cobranca_id` (**coluna nova** nullable, FK fraca `ON DELETE SET NULL`) que a baixa automática F4/F5 passa a gravar — joina `movimentação → cobrança → paciente` (não parse de observação). Movimentação manual = `cobranca_id NULL`.
- **LGPD**: relatório **agregado não audita**; **drill-down por paciente específico** no frontend navega para `/pacientes/{id}` (rota existente) — o audit via `IPacienteAcessoLogService.RegistrarAsync(..., Leitura)` ocorre no handler de `ObterPacienteQuery` (best-effort, padrão F2). Export CSV reusa `useRelatorioCsv` (`exportarPorPaciente` adicionado a `useRelatorioCsv.ts`).

#### Config sem duplicação
A aba "Configurações" do `/financeiro` tem grid 2-col via `FinanceiroConfigTab.vue`: card de comissões funcional (full-width, só para Dono) + dois cards "Em breve" (Taxa de cartão, Tabela de preços — decisão D4 do briefing 2026-06-11_002). As rotas `/financeiro/categorias` e `/financeiro/formas-pagamento` permanecem **inalteradas**.

#### Export de extrato (`ExportarExtratoQuery`) — briefing 2026-06-11_002

Query singleton `ExportarExtratoQuery → ExportarExtratoQueryHandler` → `ConsolidacaoFinanceiraQueryRepository.ExportarExtrato(...)` (Dapper, reutiliza WHERE da `ListarExtrato`, sem paginação). Retorna `ExportarExtratoResultDto { IReadOnlyList<LancamentoExtratoDto> Itens, int TotalLinhas, DateOnly DataInicio, DateOnly DataFim }`.

**Endpoint:** `GET /financeiro/extrato/export` (antes de `GET /financeiro/extrato` na rota).

**CSV:** UTF-8 com BOM, separador `;`, decimal vírgula (Excel pt-BR). Gerado no controller via `GerarCsv(itens)`.

**Audit best-effort:** `ConsolidacaoFinanceiraQueryRepository.GravarExportAuditAsync(...)` faz INSERT em `financeiro_export_log` — captura toda exceção silenciosamente (não bloqueia o fluxo). A tabela precisa ser criada pelo `imedto-database` (migration pendente).

**Pattern de teste:** `FakeConsolidacaoRepo : ConsolidacaoFinanceiraQueryRepository` sobrescreve os métodos `virtual` (`ExportarExtrato`, `GravarExportAuditAsync`, `ListarExtrato`, `ListarExtratoVencidos`) — sem interface, isolamento por herança.

#### Modo vencidos no extrato — briefing 2026-06-12_001

`GET /financeiro/extrato?somenteVencidos=true` é um **modo aditivo** da rota existente. Quando `somenteVencidos=true`:

- O backend **ignora** `dataInicio`/`dataFim` e filtra `status = 'Pendente' AND data_vencimento < CURRENT_DATE`.
- Retorna receitas **e** despesas vencidas, paginadas, pelo mesmo `LancamentoExtratoDto` (sem DTO novo).
- Multi-tenant: `WHERE l.estabelecimento_id = @EstabelecimentoId` aplicado no `ListarExtratoVencidos` (falha-fechada).
- **Paridade CA13**: a condição é idêntica à subquery `LancamentosVencidos` do `DashboardQueryRepository` — garante que a contagem do card da Home bate com a lista ao clicar.

Sem o parâmetro (default `false`): comportamento original preservado (CA15).

Handler: `ListarExtratoQueryHandler` roteia para `_repo.ListarExtratoVencidos(...)` quando `query.SomenteVencidos = true`; caso contrário, `_repo.ListarExtrato(...)` (inalterado).

#### Dashboard — campos VencidosAReceber/VencidosAPagar

`DashboardDto` ganhou dois campos adicionais calculados na mesma passada SQL do `DashboardQueryRepository`:

- `VencidosAReceber`: `SUM(valor) WHERE tipo='Receita' AND status='Pendente' AND data_vencimento < CURRENT_DATE AND estabelecimento_id = @EstabId`.
- `VencidosAPagar`: idem com `tipo='Despesa'`.

Regra idêntica à contagem `LancamentosVencidos` — garante paridade card × tela (CA13).

#### Padrão deep-link por query param (route.query nas views de lista)

Estabelecido na entrega 2026-06-12_001. Contrato: a URL de destino carrega um `?param=valor` que a view lê no `onMounted` e aplica ao estado interno já existente (aba, filtro, modo). O resultado sobrevive a F5. Regras:

- A **view de destino** lê `route.query` no `onMounted` e aplica o filtro/aba correspondente **antes** do primeiro carregamento.
- O **query param nunca é fonte de tenant** — o estabelecimento_id sempre vem do token via `ICurrentTenantAccessor`.
- Após o usuário ajustar filtros manualmente, o param não precisa ser reescrito na URL (estado inicial only).
- Contratos fixos em vigor:
  - `/financeiro?filtro=vencidos` → `VisaoGeralTab.vue` ativa `modoVencidos = true` (chama `GET /financeiro/extrato?somenteVencidos=true`).
  - `/inventario?status=baixo` → `InventarioView.vue` seta `tabAtiva = 'alertas'` + `filtroStatusItens = 'baixo'`.
  - `/orcamentos?status=pendentes` → `OrcamentoListaView.vue` seta `tab = 'pendentes'`.

---

## Bounded Context: Migração de Dados (briefing 2026-06-15_001 — Marco 1)

Permite que clientes importem dados de outros sistemas (iClinic, Feegow, Clinicorp, planilhas) via upload de ZIP. O processamento é assistido pelo time Imedto.

### Porta `IMapeadorDeMigracao`

Interface em `Domain/Migracao/IMapeadorDeMigracao.cs`. Descreve o contrato para IA inferir o mapeamento de colunas CSV/XLSX → entidade Imedto.

```csharp
Task<PropostaDeMapa> InferirMapaAsync(EsquemaDeArquivo esquema, string entidadeAlvo, CancellationToken ct);
```

`EsquemaDeArquivo` contém `Cabecalhos[]` + `AmostraMascarada` (IReadOnlyList de dicionários) — os dados são anonimizados antes de chegar ao mapper (CA4). `PropostaDeMapa` devolve `DeParaColunas`, `Confianca` e `Duvidas`.

O adapter concreto (Marco 2) implementará esta porta chamando `IaService` (já existente). Trocar provedor de IA = trocar apenas o adapter, sem tocar Domain nem Handler.

### Aggregate `MigracaoJob`

`Domain/Migracao/MigracaoJob.cs`. Estados (string constants):

```
aguardando_arquivo → aguardando_aprovacao → aguardando_mapa → mapa_em_revisao → preview_pronto
                                                                  │           ↗  (materialização)
                                                                  ↘ rejeitado
migrando → concluido / concluido_com_erros / desfeito

aguardando_mapa ──┐
migrando ─────────┴→ falhou → (Reprocessar) → aguardando_mapa / migrando
```

**Materialização na transição `mapa_em_revisao → preview_pronto` (addendum 2026-06-15_007):** a transição para `preview_pronto` dispara a **materialização** — passo de **escrita** (não Query) que relê as linhas reais dos blocos aceitos (via parser), aplica o de-para aprovado (`migracao_mapas` por `nome_bloco_origem`) e cria **N `MigracaoRegistro` pendentes** (com `payload_bruto` canônico). **Sem esse passo, `migracao_registros` ficava vazia e o job concluía com ZERO registros (bug do job #12)** — `MigracaoRegistro.Criar` tinha zero call-sites. Detalhe em "Materialização de registros" abaixo.

**Gate de aprovação antes da IA (addendum 2026-06-15_003):** o upload **não** vai direto para `aguardando_mapa`. Ele para em `aguardando_aprovacao`, e a inferência por IA só roda **após aprovação manual do admin** (`aguardando_aprovacao --AprovarAnalise(admin)--> aguardando_mapa`). O recorrente `InferirMapaMigracaoJob` seleciona **exclusivamente** `aguardando_mapa` (`ObterMaisAntigoAguardandoMapaOuNulo`) — esse é o gate de custo/governança de IA: nenhum job não-aprovado é jamais inferido. Padrão reaproveitável: **aprovação humana por job antes de qualquer chamada de IA cara**.

- `Criar(estabelecimentoId, usuarioId, origem?, onda?)` — estado inicial `aguardando_arquivo`.
  - `onda = null` → Onda 1 (pacientes). `onda = "prontuario"` → Onda 2 (Marco 5).
- `RegistrarArquivoRecebido(s3Key)` — transição `aguardando_arquivo → aguardando_aprovacao` (addendum 003 — antes ia direto para `aguardando_mapa`); seta `ArquivoExpiraEm = UtcNow + 30 dias` (CA24, R12) + `TermoAceitoEm`.
- `AprovarAnalise(adminId)` — transição `aguardando_aprovacao → aguardando_mapa`, libera a inferência por IA. Válido apenas em `aguardando_aprovacao`. Handler: `AprovarAnaliseCommandHandler` (ImedtoAdmin only, RBAC). Endpoint `POST /{jobId}/aprovar-analise` (addendum 003, CA40–CA44).
- `Rejeitar()` — transição de `aguardando_arquivo`/`aguardando_aprovacao`/`aguardando_mapa` → `rejeitado` (addendum 003 ampliou para incluir `aguardando_aprovacao` — D-A4; job rejeitado nunca dispara IA).
- `MarcarArquivoExpirado()` — chamado pelo job `expirar-arquivos-migracao`.
- `MarcarFalhou(motivo)` — transição de `aguardando_mapa` ou `migrando` → `falhou`. Salva `StatusAntesFalha` (para restauração) e `MotivoFalha` (categoria legível sem PII). Chamado pelos handlers de job ao capturar exceção inesperada (addendum 002, CA25/CA26).
- `Reprocessar()` — válido apenas em `falhou`. Restaura `StatusAntesFalha` e limpa `MotivoFalha`/`StatusAntesFalha`. Os schedulers recorrentes re-selecionam automaticamente o job (CA30). Handler: `ReprocessarMigracaoCommandHandler` (ImedtoAdmin only, RBAC).

**Padrão Reprocessar:** ao reprocessar, o handler usa `ObterPorIdAdminOuNulo` (sem filtro de tenant — escopo admin). Após `job.Reprocessar()`, salva e retorna 204. O job volta ao estado anterior e é retomado pelo próximo ciclo do scheduler recorrente (`InferirMapaMigracaoJob` ou `CarregarMigracaoJob`) sem criar nova infra.

### Onda 2 — Prontuário histórico (Marco 5, briefing 2026-06-15_001)

`CarregarOnda2JobHandler` (Application/Migracao/Jobs) espelha `CarregarOnda1JobHandler` com três diferenças centrais:

**Port `IMigracaoPacienteLookup`** (Domain/Migracao/IMigracaoPacienteLookup.cs):
- `ObterPorCpfOuNulo(cpf, estabelecimentoId)` — lookup por CPF.
- `ObterPorDocumentoInternacionalOuNulo(doc, estabelecimentoId)` — lookup para pacientes estrangeiros.
- `ObterIdModeloPadraoProntuarioOuNulo(estabelecimentoId)` — modelo padrão para criação de prontuário.
- Adapter: `DapperPacienteMigracaoLookup` (Infrastructure/Migracao). Singleton — conexão por chamada.

**CA13 — Dependência de Onda 1:** `ExisteOnda1AtivaParaTenant(estabelecimentoId)` bloqueia Onda 2 enquanto houver job de pacientes em `migrando`/`aguardando_mapa`/etc. O job fica no status `migrando` e é reprocessado na próxima rodada do scheduler.

**CA15 — Honestidade estrutural:**
- `prontuario_evolucao` com `conteudo_json` → `RegistrarEvolucaoCommand` (evolução estruturada).
- `prontuario_evolucao` sem `conteudo_json` OU `prontuario_anexo` → `AdicionarAnexoCommand` com `text/plain` (anexo histórico pesquisável). Nunca fabrica evolução estruturada inventada.

**CA21 — Audit:** cada chamada a `RegistrarEvolucaoCommand`/`AdicionarAnexoCommand` usa `AutorSistemaId = 00000000-0000-0000-0000-000000000001` — gerado via `IProntuarioAcessoLogService` interno aos handlers reutilizados. Audit trail separável da escrita clínica normal por este ID fixo.

### Storage S3

`IMigracaoArquivoStorageService` (Domain) / `S3MigracaoArquivoStorageService` (Infrastructure). Reutiliza `BucketAnexosProntuario` com key `migracao/{estabelecimentoId}/{jobId}/arquivo.zip`. Retenção de 30 dias via `ExpirarArquivosMigracaoJob` (CA24, R12).

### Decomposição de dump JSON aninhado em blocos (addendum 4 — CA70-72)

Quando o arquivo enviado é um objeto JSON raiz (dump de sistema legado com múltiplas entidades), o `JsonMigracaoParser` não extrai mais apenas o primeiro array (bug `EncontrarPrimeiroArray` que causou a falha no job #11). O parser agora faz `DecomporObjetoRaiz()`:

- **Array de objetos** → 1 `BlocoCandidato` por propriedade. Nome do bloco = nome da propriedade (ex.: `"pacientes"` → bloco `pacientes`).
- **Objeto único (config)** → bloco com `EhConfig = true` (ex.: `"estabelecimento": {...}`). Não é mapeável — aparece no painel como "Configuração (não migrável)", ignorado por padrão.
- **Campos escalares / arrays vazios** → ignorados.
- **Sub-objetos e arrays internos em registros** → excluídos dos cabeçalhos do bloco (D-S4). Nunca inventados.
- **JSON-array na raiz** → 1 bloco (nome = arquivo sem extensão) — compatibilidade preservada (CA71).
- **CSV** → sempre 1 bloco (nome = arquivo sem extensão).

Cada bloco passa individualmente pela IA via `InferirBlocoAsync()` (1 call = classifica entidade + produz de-para). O nome do arquivo se torna hint apenas — não determina a entidade.

### Classificação de entidade por IA (addendum 4 — CA73-76)

`AnthropicMapeadorDeMigracao.InferirBlocoAsync()` substitui a detecção anterior por nome de arquivo. O prompt instrui a IA a:
1. Classificar a entidade com base no **schema do bloco** (nomes de cabeçalhos), não pelo nome do arquivo.
2. Usar apenas a lista canônica fechada (`EntidadesCanônicas`): `paciente, agendamento, fornecedor_estoque, categoria_estoque, fabricante_estoque, local_estoque, item_estoque, produto_orcamento, procedimento_orcamento, prontuario, sem_equivalente`.
3. Retornar a classificação + confiança + de-para + dúvidas em uma única chamada (D-N2 — sem round-trip extra).
4. Nunca usar IDs internos do dump como campo de mapeamento.

A resposta é validada contra `EntidadesCanônicas.EhValida()` — fallback para `sem_equivalente` se inválida. PII é mascarada antes da chamada (LGPD — CA82).

### Normalização de encoding (addendum 4 — CA80-81)

`MojibakeNormalizador` (Infrastructure) aplica correção determinística Latin-1↔UTF-8 na ingestão de cada valor textual, sem IA:
- Detecta heurística de caracteres Latin-suspeitos (U+00C0–U+00FF).
- Faz round-trip `iso-8859-1` → bytes → `utf-8` → string.
- Compara "problem score" (chars de reposição, chars fora de BMP) entre original e corrigido.
- Se corrigido tem score melhor e sem replacement chars → adota. Caso contrário → mantém original e sinaliza `encoding_suspeito = true`.

### Resiliência da inferência por IA (addendum 5 — CA86-101)

A inferência por bloco (1 chamada de IA por bloco-candidato) é resiliente a limite de taxa e sobrecarga do provider. **Sem mudança de schema** — tudo no `mapa_json` e nos estados de job existentes.

- **Retry/backoff no adapter** (`AnthropicMapeadorDeMigracao`): em **429 (TooManyRequests)**, **529 (overloaded)** ou falha transitória de rede (timeout/`HttpRequestException`), retenta respeitando o header **`Retry-After`** quando presente; senão **backoff exponencial ~1s com jitter**, **teto de 5 tentativas**. **4xx≠429 (401/403 — chave inválida) é permanente, não retenta.** **Espelha `ResendEmailService`** (mesmo padrão de retry; o adapter é o dono da resiliência — ports & adapters: handler/domínio não conhecem status HTTP).
- **Espaçamento entre blocos**: pausa fixa **~1s configurável** entre chamadas de bloco (sequencial, nunca paralelo). O mapeador **não** passa pelo `RateLimitedIaService` — por isso o espaçamento é explícito aqui.
- **Truncamento de valor na amostra**: cada valor é truncado a **500 caracteres** (`…[truncado]`) **após** a máscara de PII e antes do provider — corta `conteudo_html`/base64 que estouravam o TPM. D1/D2 preservados (mascara antes; trunca só comprimento).
- **Degradação graciosa por bloco**: falha de IA em um bloco (após esgotar retry) vira **mapa de erro** (`bloco_com_erro: true` + `motivo_erro` categoria genérica sem PII no `mapa_json`) e a inferência **continua** os demais blocos — os blocos OK são preservados (corrige o bug do job #12, que perdia 5 blocos bons por um 429 no 6º). Com **≥1 sucesso** → `mapa_em_revisao` com aviso de blocos falhos; **zero sucesso** → `falhou` (`MarcarFalhou` do addendum 002).
- **Reprocessar parcial**: reusa `Reprocessar` (addendum 002/003) + o upsert `(jobId, entidade, nome_bloco_origem)` (addendum 004) — a inferência **pula a chamada de IA** dos blocos com mapa bem-sucedido persistido (`bloco_com_erro != true`); só blocos com erro/pendentes voltam à IA.

> **Risco residual (operação, não código):** conta Anthropic de tier muito baixo ainda pode degradar (vários blocos em erro mesmo após retry). Mitigado por degradação graciosa + espaçamento configurável + truncamento; resolvido a médio prazo subindo o tier ou integrando o `RateLimitedIaService` (backlog).

### Materialização de registros (addendum 6 — CA102-118)

**A etapa que faltava entre revisão do mapa e carga.** Até o addendum 6, a Central **nunca salvava registros**: a inferência criava só os `migracao_mapas` (de-para por bloco) e **descartava as linhas**; `MigracaoRegistro.Criar` tinha **zero call-sites de produção**; o preview e a carga achavam `migracao_registros` **vazia** → o job concluía com **ZERO registros** (bug confirmado no job #12 de produção). A peça do discovery "código aplica o mapa às N linhas" nunca havia sido implementada.

A **materialização** é o passo de **escrita** (não Query) disparado na transição `mapa_em_revisao → preview_pronto`:

- **Itera os blocos classificados** (`migracao_mapas` por `nome_bloco_origem`, addendum 004) — não por nome de arquivo. Relê as linhas reais de cada bloco aceito (via parser) e cria **1 `MigracaoRegistro` `pendente` por linha** (`MigracaoRegistro.Criar` — agora com call-site).
- **De-para**: colunas marcadas `ignorar` são descartadas; as demais viram campos canônicos no `payload_bruto`. Usa o **valor real inteiro** da linha — **nunca** a amostra truncada a 500 chars (o truncamento do addendum 006 é exclusivo da chamada à IA). A **validação de obrigatório fica na CARGA** (`BusinessException` do command → `MarcarRejeitado`), não na materialização.
- **Blocos não-materializáveis** (`sem_equivalente`, `ignorado`, `eh_config`, `bloco_com_erro: true`) **não geram registro** — só blocos aceitos com entidade classificada.
- **Idempotência**: re-materializar (re-preview / editar mapa) faz `DELETE WHERE migracao_job_id=@job AND status='pendente'` e regera dos mapas atuais — registros `importado_*`/`rejeitado`/`pulado` **nunca** são tocados (sem duplicar nem desfazer importação). O índice `ix_migracao_registros_job_status` `(migracao_job_id, status)` já existe e cobre o DELETE — **sem migration**.
- **CQRS**: a materialização é **escrita** (command/passo), não cabe no `PreviewOnda1QueryHandler` (Query não escreve). O preview passa a contar registros **reais**; a carga (`CarregarOnda1/2JobHandler`) continua criando entidades **só por commands** (ordem de FK + upsert por chave de negócio — D11 intacto). A materialização só popula o **staging** `migracao_registros`, nunca tabela de domínio.

### Schema (tabelas `migracao_*`)

5 tabelas geradas pelo `imedto-database`:
- `migracao_jobs` — job por upload (multi-tenant: `estabelecimento_id`). Colunas `motivo_falha text NULL` e `status_antes_falha text NULL` adicionadas em `20260615200000_AdicionarMotivoFalhaJob` (addendum 002).
- `migracao_registros` — linhas individuais para importação. `motivo_rejeicao text NULL` é categoria genérica sem PII; usada para agregar `MotivosRejeicao` e `MotivosPulo` (`Dictionary<string,int>`) no relatório (CA34/CA35).
- `migracao_mapas` — proposta de mapeamento por bloco. Coluna `nome_bloco_origem text NOT NULL DEFAULT ''` adicionada em `20260615160000_migracao_mapas_nome_bloco_origem` (addendum 4). Unique constraint alterada de `(migracao_job_id, entidade)` para `(migracao_job_id, entidade, nome_bloco_origem)` — permite dois blocos do mesmo dump classificados como a mesma entidade. `mapa_json` contém campos adicionais: `entidade_classificada`, `confianca_classificacao`, `encoding_suspeito`, `eh_config`, `ignorado`, `entidade_operador` (quando operador reclassificou).
- `migracao_templates` — templates reutilizáveis de mapeamento por sistema de origem.
- `migracao_job_eventos` — trilha de transições de status (addendum 3 — CA53-56). Cada linha = uma mudança de status: `status_anterior text NULL`, `status_novo text`, `usuario_id uuid NULL` (null = sistema/job; preenchido = admin), `criado_em timestamptz`. Multi-tenant via `estabelecimento_id` herdado do job. Ordem ASC = histórico cronológico. Sem PII.

### Endpoint de progresso leve (addendum 3 — CA57-59)

`GET /api/admin/migracao/{jobId}/progresso` — diferente do `/relatorio` (que exige job concluído e agrega por lote), o progresso:
- Funciona em **qualquer status** (incluindo durante `migrando`) — sem gate de `BusinessException`.
- Faz `GROUP BY entidade, status` em `migracao_registros` e calcula percentual em memória.
- Retorna `ProgressoMigracaoResult { porEntidade: Record<string, ProgressoEntidadeDto>, percentualAgregado: int }`.
- Handler: `ObterProgressoMigracaoQueryHandler` (Singleton — leitura Dapper pura).
- Padrão reaproveitável: endpoints de progresso em background devem ser leves e sem gate de estado — o gate (ex: job concluído) é responsabilidade exclusiva do endpoint de relatório.

### Polling no frontend (addendum 3 — CA60)

`MigracaoRevisaoView.vue` usa `setTimeout` recursivo (não `setInterval`) com intervalo de 4 s. Inicia apenas quando `statusJob` é um dos estados ativos: `aguardando_mapa`, `mapa_em_revisao`, `migrando`. Para automaticamente no `onUnmounted` e via `watch(statusJob)` quando o status chega a terminal. O timeout recursivo garante que um request precisa completar antes de agendar o próximo (evita sobreposição em conexões lentas).

### Checklist multi-tenant

`IMigracaoJobRepository.ObterPorIdDoEstabelecimentoOuNulo(jobId, estabelecimentoId)` lança `InvalidOperationException` se `estabelecimentoId <= 0` (falha-fechada). Retorna `null` (→ 404 genérico) se o job não pertencer ao tenant da requisição.

### Frontend

Seção "Migrar meus dados" em `EstabelecimentoView.vue` (grupo "Onboarding", `SecaoId = "migracao-dados"`). Componente `MigracaoDadosTab.vue` + `migracaoService.ts`. Validação de ZIP + 50MB no front antes do POST (`migracaoService.iniciarUpload`). Termo de responsabilidade obrigatório (R12).

---

## Conexão Postgres (RDS)

- Connection string normal Npgsql em `ConnectionStrings:Default` (runtime) e `ConnectionStrings:Migrations` (autoria de migrations via `dotnet ef`).
- Em prod, host/senha vêm do AWS SSM Parameter Store (`/imedto/dev/db-host`, `/imedto/dev/db-password`); a EC2 lê via IAM role.
- Em dev, o valor fica em `appsettings.Development.json` (gitignored).
- O `AppDbContextFactory` usa apenas `ConnectionStrings:Migrations` para o tooling do `dotnet ef`.

> Detalhes de infra do RDS (endpoint, SG, backup) em [INFRA.md](INFRA.md). Comandos de migration em [COMANDOS.md](COMANDOS.md).
