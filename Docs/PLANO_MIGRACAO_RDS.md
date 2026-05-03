---
titulo: Plano de migração Supabase → AWS RDS
status: rascunho
criado_em: 2026-05-03
escopo: Backend Imedto — substituir Supabase (Auth + Storage + Postgres pooler) por AWS-native (Cognito ou self-hosted JWT + S3 + RDS Postgres)
---

# Plano de migração: Supabase → AWS RDS

## Sumário executivo

**Boa notícia**: a migração **não é catastrófica**. O backend está bem desacoplado:
- `IAuthService` é interface pura — `SupabaseAuthService` é uma implementação trocável.
- `IFotoStorageService` e `IAnexoStorageService` são interfaces puras — mesma situação.
- 100% das migrations Postgres são SQL padrão (sem `pg_net`, `vault`, etc.) — portáveis para RDS sem mudança.
- Realtime já é SignalR (próprio do backend), não Supabase Realtime.

**Pontos sensíveis** (precisam de planejamento):
1. **Tabela `public.usuarios`** tem `FOREIGN KEY (id) REFERENCES auth.users(id)`. RDS não tem `auth.users`.
2. **64 referências a `auth.uid()`** em RLS policies (10 arquivos). RDS Postgres não tem essa função.
3. **JWT do Supabase é assinado por chaves que ficam só lá** (JWKS no domínio Supabase). Cutover de auth precisa de nova issuer + revogação coordenada.
4. **Storage**: 2 buckets (anexos de prontuário, fotos). Migrar arquivos físicos do Supabase Storage para S3.

**Abordagem recomendada — incremental, sem big-bang**:
1. Continuar trabalho atual (limpeza/otimização) sem adicionar novas dependências Supabase-only.
2. **Fase A (1-2 sessões)**: criar `LocalJwtAuthService` + tabelas auth próprias rodando **lado a lado** com Supabase em dev.
3. **Fase B (1 sessão)**: substituir `SupabaseStorageService` por `S3StorageService` (mesma interface).
4. **Fase C (1 sessão)**: simplificar RLS — opções: (a) remover policies `auth.uid()` e confiar no backend (mais simples), (b) reescrever para usar contexto local (`SET LOCAL app.usuario_id`).
5. **Fase D**: cutover.

Estimativa total: **6-10 sessões** de trabalho focado, sem quebrar a aplicação em produção.

---

## 1. Auditoria do estado atual (números reais)

### 1.1 Auth

| Item | Estado |
|---|---|
| `SupabaseAuthService.cs` | wrapper REST: signup / login / refresh / logout / getUser / deleteUser / criarConvite / recuperarSenha |
| JWT — issuer | `https://kdoqflrmfgazdgekdbqc.supabase.co/auth/v1` |
| JWT — algoritmo | ES256 (assimétrico via JWKS) |
| Validação no backend | `AddJwtBearer({ Authority = ... })` — descoberta automática |
| Cookies | HttpOnly, gerenciados pelo backend (BFF) |
| FK `public.usuarios.id → auth.users.id` | sim, `ON DELETE CASCADE` |
| Pacote NuGet `supabase-csharp` | **não instalado** — só HttpClient REST |

### 1.2 Storage

| Item | Estado |
|---|---|
| `SupabaseStorageService.cs` | upload/URL assinada para anexos de prontuário (bucket `imedto_anexos_prontuario`, TTL 5 min) |
| `SupabaseFotoStorageService.cs` | upload de fotos de estabelecimento e profissional |
| Buckets criados em SQL | 1 (`imedto_anexos_prontuario`) |
| Política RLS de storage | sim — só donos/profissionais vinculados leem |

### 1.3 Realtime

**Não usa Supabase Realtime.** O backend tem `EstabelecimentoHub.cs` (SignalR próprio) com backplane Redis opcional.

### 1.4 RLS policies

| Arquivo | `auth.uid()` refs |
|---|---|
| 10 arquivos de migration têm `CREATE POLICY` | 64 chamadas a `auth.uid()` totais |

Política atual: backend usa `service_role` e **bypassa RLS**. RLS é defense-in-depth puro — se o backend errar, RLS impede vazamento. Em RDS, sem `auth.uid()`, opções:
- (a) **Remover policies que usam `auth.uid()`** — backend continua sendo a fonte da verdade (que já é).
- (b) **Reescrever para `current_setting('app.usuario_id')`** — exige que cada conexão setar a variável a cada request. Mais trabalho, mantém defense-in-depth.

Recomendação: **(a) remover** — o overhead de manter RLS sem benefício real (backend bypassa) não compensa.

### 1.5 Postgres (schema/queries)

- Conexão hoje: pooler `aws-1-sa-east-1.pooler.supabase.com` (transaction mode 6543, session mode 5432).
- **100% das queries são Postgres padrão** — nenhuma extensão Supabase-only é usada (ver Fase 7 commit b1f24f8 — `pg_trgm` + `unaccent` são extensions oficiais do Postgres).
- Funções customizadas (`public.imutable_unaccent`) são portáveis.
- Migrations EF + SQL idempotente — fluxo 100% portável.

---

## 2. Estratégia geral

**Princípio**: zero downtime, capacidade de **rollback até a véspera do cutover**.

### Sequência

1. **Não adicionar nova dependência Supabase** a partir de hoje.
2. **Fase A — Auth próprio** (lado a lado): backend ganha capacidade de emitir/validar **JWT próprio**, mantém capacidade de validar JWT do Supabase. Switch via configuração.
3. **Fase B — Storage S3** (lado a lado): novo `S3StorageService`, configuração escolhe qual usar.
4. **Fase C — RLS limpa**: remover policies que dependem de `auth.uid()` (com compromisso explícito de que o backend é a única defesa).
5. **Fase D — RDS provisionamento + dump/restore** (em paralelo às outras fases).
6. **Fase E — Cutover**: troca de DNS / connection string + import de usuários auth.

### Não recomendo Cognito/Auth0

- **Cognito** acopla a AWS (lock-in maior que Supabase) e tem UX de admin ruim para signup/recuperação.
- **Auth0/Keycloak** adiciona vendor terceiro = mais um SLA + mais um custo.
- **JWT próprio no backend** é a opção mais simples para esta arquitetura BFF — você já assina o cookie HttpOnly, já controla o refresh.

---

## 3. FASE A — Substituir Auth por JWT próprio

### A.1 Tabelas novas (migration EF + SQL)

```sql
CREATE TABLE public.auth_credenciais (
    usuario_id          uuid PRIMARY KEY,
    email               citext NOT NULL UNIQUE,
    senha_hash          text NOT NULL,        -- BCrypt cost 12
    email_confirmado_em timestamptz NULL,
    bloqueado_em        timestamptz NULL,
    motivo_bloqueio     text NULL,
    criado_em           timestamptz NOT NULL DEFAULT now(),
    atualizado_em       timestamptz NULL
);

CREATE TABLE public.auth_refresh_tokens (
    id              bigserial PRIMARY KEY,
    usuario_id      uuid NOT NULL REFERENCES public.auth_credenciais(usuario_id) ON DELETE CASCADE,
    token_hash      text NOT NULL UNIQUE,     -- SHA-256 do refresh token (nunca o token cru)
    expira_em       timestamptz NOT NULL,
    revogado_em     timestamptz NULL,
    ip_origem       text NULL,
    user_agent      text NULL,
    criado_em       timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_auth_refresh_tokens_usuario_id ON public.auth_refresh_tokens(usuario_id) WHERE revogado_em IS NULL;
CREATE INDEX ix_auth_refresh_tokens_expira_em ON public.auth_refresh_tokens(expira_em) WHERE revogado_em IS NULL;

CREATE TABLE public.auth_email_tokens (
    id              bigserial PRIMARY KEY,
    usuario_id      uuid NOT NULL REFERENCES public.auth_credenciais(usuario_id) ON DELETE CASCADE,
    tipo            text NOT NULL,            -- 'confirmacao_email' | 'reset_senha'
    token_hash      text NOT NULL UNIQUE,
    expira_em       timestamptz NOT NULL,
    consumido_em    timestamptz NULL,
    criado_em       timestamptz NOT NULL DEFAULT now()
);
```

### A.2 `LocalJwtAuthService` (implementação de `IAuthService`)

```csharp
public class LocalJwtAuthService : IAuthService
{
    // Dependências:
    // - IConfiguration: segredo HMAC ou chave EC privada
    // - IUsuarioRepository: lookup de credencial
    // - IEmailService: enviar confirmação/recuperação
    // - IPasswordHasher: BCrypt wrapper
    // - IRefreshTokenStore: persistência de refresh tokens

    public async Task<AuthResult> LoginAsync(string email, string password) {
        var cred = await _credenciaisRepo.ObterPorEmail(email);
        if (cred is null || !_hasher.Verify(password, cred.SenhaHash))
            throw new BusinessException("Credenciais inválidas.");
        if (cred.BloqueadoEm.HasValue)
            throw new BusinessException("Conta bloqueada.");
        if (cred.EmailConfirmadoEm is null)
            throw new BusinessException("Confirme seu e-mail antes de entrar.");

        var (accessToken, expiresAt) = EmitirAccessToken(cred);
        var refreshToken = await EmitirRefreshToken(cred.UsuarioId);
        return new AuthResult(accessToken, refreshToken, expiresAt, MapearUserInfo(cred));
    }

    // ... idem para signup, refresh, logout, etc.
}
```

### A.3 Configuração JWT no backend

```csharp
// Program.cs
options.Authority = null;  // sem JWKS externo
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = "imedto-backend",
    ValidateAudience = true,
    ValidAudience = "imedto-app",
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    // Em prod considerar EC256 + chave privada KMS-protegida
};
```

### A.4 Coexistência durante transição

`Program.cs` deve aceitar **ambas** as issuers durante a migração:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("supabase", o => { o.Authority = supabaseAuthority; ... })
    .AddJwtBearer("local",    o => { o.TokenValidationParameters = ...; });

// Policy escolhe automaticamente conforme issuer
options.DefaultPolicy = new AuthorizationPolicyBuilder()
    .AddAuthenticationSchemes("supabase", "local")
    .RequireAuthenticatedUser()
    .Build();
```

E `IAuthService` resolvido via factory que escolhe `Supabase` ou `Local` por config. Default em prod = `Supabase` até cutover.

### A.5 Migração de usuários

- **Senhas Supabase não saem em texto** (BCrypt no servidor deles). Estratégia: **migração lazy**:
  - Tabela `auth_credenciais` é populada com `senha_hash = NULL` inicialmente.
  - No primeiro login pós-cutover, o backend **redireciona para "redefinir senha"** (envia e-mail).
  - Após redefinição, `senha_hash` populada.
- Alternativa "rolling": fazer os usuários **redefinirem senha antes do cutover** (anúncio com 2 semanas de antecedência).
- Email_confirmado_em copiado do Supabase.

---

## 4. FASE B — Storage S3

### B.1 Implementação

- Adicionar `AWSSDK.S3` (NuGet).
- Criar `S3AnexoStorageService : IAnexoStorageService` e `S3FotoStorageService : IFotoStorageService`.
- Configuração: `AWS:Region`, `AWS:BucketAnexos`, `AWS:BucketFotos`, IAM role (em produção) ou access key (dev).

### B.2 URL assinada

S3 tem `presigned URL` nativo — equivalente direto ao `signed URL` do Supabase Storage. Mesma semântica (TTL, GET only, etc.).

### B.3 Migração de arquivos existentes

- Job pontual com `aws s3 sync` ou script .NET que itera o bucket Supabase + uploads para S3.
- Manter `storage_path` igual (mesma convenção `est_X/paciente_Y/<uuid>_<nome>`).

### B.4 Coexistência

- `IFotoStorageService` resolvido via factory (Supabase em prod, S3 em staging).
- Switch por feature flag ou `appsettings`.

---

## 5. FASE C — RLS

### C.1 Decisão recomendada: **remover policies `auth.uid()`**

Justificativa:
- Backend já bypassa RLS (`service_role`). RLS atual só protege se o backend errar — defense-in-depth real.
- Em RDS, manter `auth.uid()` exigiria ou (a) reproduzir o esquema `auth` do Supabase, ou (b) reescrever todas as 64 policies para `current_setting('app.usuario_id', true)::uuid` — esforço alto sem ganho proporcional.
- O backend tem hoje (após Fase 3) **defense-in-depth no código**: `ObterPorIdOuNulo(id, estabelecimentoId)`, `[RequiresPapel]`, `RequiresEstabelecimento` filter, mensagens genéricas. Esse já é o nível de proteção real.

### C.2 Plano de remoção

- Migration nova `XXXX_remove_rls_policies_auth_uid.sql` que faz `DROP POLICY` em todas as policies que dependem de `auth.uid()`.
- Tabelas mantêm `ENABLE ROW LEVEL SECURITY` mas sem policies → ninguém lê (defesa máxima). Backend conecta como `service_role` e bypassa — funciona normalmente.
- Em RDS, `service_role` vira o role de aplicação (sem o nome "service_role" mas mesmo conceito).

### C.3 Alternativa: reescrita para `current_setting`

Se o time decidir manter RLS:

```sql
-- Ao iniciar cada conexão (ou via NpgsqlConnection.OpenAsync wrapper):
SET LOCAL app.usuario_id = '<sub do JWT>';

-- Policy:
CREATE POLICY paciente_select ON public.pacientes
    FOR SELECT
    USING (estabelecimento_id IN (
        SELECT v.estabelecimento_id
        FROM public.vinculo_profissional_estabelecimento v
        WHERE v.profissional_usuario_id = current_setting('app.usuario_id', true)::uuid
          AND v.status = 'Ativo'
    ));
```

**Custo**: middleware no backend para `SET LOCAL` em cada request, e refactor das 64 policies. Recomendo **adiar para depois do cutover**.

---

## 6. FASE D — FK `public.usuarios.id → auth.users.id`

Hoje:
```sql
ALTER TABLE public.usuarios
    ADD CONSTRAINT usuarios_id_fkey
    FOREIGN KEY (id) REFERENCES auth.users(id) ON DELETE CASCADE;
```

Em RDS: tabela `auth.users` não existe. Opções:

### D.1 (Recomendada) FK para `public.auth_credenciais`

Trocar para FK em `public.auth_credenciais` (tabela criada na Fase A). `ON DELETE CASCADE` mantido.

Migration:
```sql
ALTER TABLE public.usuarios DROP CONSTRAINT usuarios_id_fkey;
ALTER TABLE public.usuarios
    ADD CONSTRAINT usuarios_id_fkey
    FOREIGN KEY (id) REFERENCES public.auth_credenciais(usuario_id) ON DELETE CASCADE;
```

### D.2 Alternativa: remover FK

Se o time preferir manter `public.usuarios` totalmente desacoplado (auth pode ser substituído de novo no futuro), remover a FK e validar consistência via aplicação.

---

## 7. FASE E — Conexão DB → RDS

### E.1 Provisionar RDS

- Postgres 17 (versão atual do Supabase).
- Multi-AZ em produção.
- Parameter group: extensions já habilitadas no Supabase devem ser whitelistadas no RDS:
  - `pg_trgm` ✅
  - `unaccent` ✅
  - `btree_gist` ✅ (usado em constraint de sobreposição de horário)
- Backups automáticos (mínimo 7 dias, ideal 30).
- Performance Insights ativado.

### E.2 Connection string

Trocar:
```
Host=aws-1-sa-east-1.pooler.supabase.com;Port=6543;...  (transação)
Host=aws-1-sa-east-1.pooler.supabase.com;Port=5432;...  (sessão)
```

Por:
```
Host=imedto-prod.xxxxx.sa-east-1.rds.amazonaws.com;Port=5432;...
```

Sem PgBouncer. Para usar pooling: adicionar **RDS Proxy** ou **PgBouncer self-hosted** (recomendado em ECS sidecar).

### E.3 Dump/restore

```bash
pg_dump -h aws-1-sa-east-1.pooler.supabase.com -U postgres.kdoqflrmfgazdgekdbqc \
        -d postgres -Fc --schema=public --no-owner --no-acl > imedto-prod.dump

pg_restore -h imedto-prod.xxxxx.sa-east-1.rds.amazonaws.com -U imedto -d imedto \
           --no-owner --no-acl imedto-prod.dump
```

Restaurar **só `public`** (não copiar `auth`, `storage`, `realtime`, `vault`).

---

## 8. FASE F — Cutover (sequência sugerida)

**Janela de manutenção (estimativa: 30-60 min)**:

1. Anunciar manutenção 1 semana antes (e-mail + banner no app).
2. Pôr o app em modo read-only (feature flag).
3. `pg_dump` final do Supabase.
4. `pg_restore` no RDS já provisionado.
5. Importar usuários auth (script idempotente: para cada `auth.users`, criar linha em `public.auth_credenciais` com `senha_hash = NULL`).
6. Trocar connection string no `appsettings.Production.json`.
7. Trocar `IAuthService` para `LocalJwtAuthService`.
8. Trocar `IFotoStorageService`/`IAnexoStorageService` para implementações S3.
9. `aws s3 sync` final dos arquivos remanescentes.
10. Restart da aplicação.
11. Smoke tests críticos (login, listar pacientes, criar agendamento).
12. Anúncio de "redefina sua senha" para os usuários (lazy migration).
13. Tirar modo read-only.

**Rollback**: se falhar nos passos 6-11, voltar connection string + IAuthService para Supabase. RDS fica como cópia "morta" até diagnosticar.

---

## 9. O QUE FAZER AGORA (não esperar a migração)

Ações que **antecipam** trabalho de migração e **não bloqueiam** o desenvolvimento atual:

### 9.1 Não adicionar mais dependências Supabase

- ❌ Não usar `auth.uid()` em policies novas. Se RLS for necessária em features novas, escrever sem `auth.*`.
- ❌ Não criar FKs novas para `auth.users`. Usar `uuid` solto.
- ❌ Não usar `storage.objects` policies novas. Backend é a única defesa de storage.
- ❌ Não adicionar pacote NuGet `supabase-csharp` ou `Postgrest`.

### 9.2 Reforçar abstrações

- ✅ `IAuthService` já está abstrato — mantenha o controller usando só ele (não acoplar a tipos do `SupabaseAuthService`).
- ✅ `IFotoStorageService` / `IAnexoStorageService` idem.
- 📝 Adicionar `IRefreshTokenStore` agora (interface), implementação Supabase = no-op (Supabase guarda refresh internamente). Quando vier `LocalJwtAuthService`, ele usa a interface.

### 9.3 Documentação

- ✅ Este documento (vai vivo no repo).
- 📝 Adicionar seção "Provider de auth" no CLAUDE.md indicando as escolhas e que o code path Supabase é temporário.

### 9.4 Configuração

- 📝 Renomear `Supabase:*` no `appsettings` para `Auth:Supabase:*` (group). Quando `Auth:Local:*` aparecer, fica claro o paralelismo.
- 📝 Adicionar `Auth:Provider` = `"Supabase" | "Local"` para alternar via config.

---

## 10. Conclusão

A arquitetura atual **já está pronta** para a migração — o desacoplamento via `IAuthService` + `IFotoStorageService` foi feito desde o início. A maior parte do esforço é:

1. **Implementar** `LocalJwtAuthService` (1-2 sessões).
2. **Implementar** `S3*StorageService` (1 sessão).
3. **Decidir** sobre RLS (1 reunião + migration de remoção).
4. **Provisionar** RDS + cutover (1 sessão de SRE/devops).

**Total**: ~6-10 sessões de trabalho.

Não há nada que precise mudar **agora** no código que está sendo escrito (Fases 7+ do plano de limpeza). Apenas seguir os DON'Ts da seção 9.1 e o atrito futuro será mínimo.

---

## Próximos passos imediatos

1. ✅ Documento criado (este arquivo).
2. 📝 Anotar na memória do agente as restrições de §9.1.
3. ⏭️ Continuar trabalho da Fase 7 do plano de limpeza (pendências: PacienteAcessoLog, IDomainEventDispatcher, defense-in-depth UsuarioId).
4. 🔜 Quando o usuário decidir iniciar a migração formalmente, abrir as Fases A → F deste plano.

---

## Checklist executável (atualização 2026-05-03)

Esta seção complementa o plano A-F com o estado **atual** dos pré-requisitos
e as ações concretas restantes. Use como pista para abrir cards/tasks quando
a migração for formalmente iniciada.

### Pré-requisitos já satisfeitos

- [x] Auth abstraído via `IAuthService` (sem chamada a `Supabase.Client` em handlers).
- [x] Storage abstraído via `IFotoStorageService` + `IAnexoStorageService`.
- [x] **Defense-in-depth multi-tenant** em todos os repositórios sensíveis (LGPD).
- [x] **Audit trail LGPD** (`paciente_acesso_log`, `prontuario_acesso_log`) NÃO depende de RLS.
- [x] **Soft-delete** centralizado no `SoftDeleteInterceptor` (não usa policies).
- [x] **Cobertura de testes** robusta antes de mexer no banco:
      - 705+ unit tests
      - 26+ integration tests (Testcontainers Postgres)
      - Constraints reais validadas (unique parcial, FK, soft-delete interceptor)
- [x] **Performance baseline** com `PerformanceIntegrationTests` — qualquer regressão
      pós-migração aparece nos thresholds.
- [x] **Load tests** k6 prontos para validar latência pós-cutover (`loadtests/k6/`).

### Ações restantes (em ordem)

1. **Fase A — `LocalJwtAuthService`**
   - [ ] Adicionar `BCrypt.Net-Next` ao Infrastructure
   - [ ] Implementar `IAuthService` com hash bcrypt + JWT HS256/RS256 próprio
   - [ ] Endpoints novos: `POST /auth/signup-local`, `POST /auth/forgot-password-local`
   - [ ] Migration: tabela `usuario_credenciais` (id, email, hash, salt, criado_em)
   - [ ] Testes unitários do novo service (já temos pattern em `Tests/`)
   - [ ] Feature flag `Auth:Provider = "Supabase" | "Local"` para alternar sem deploy

2. **Fase B — `S3FotoStorageService` + `S3AnexoStorageService`**
   - [ ] AWSSDK.S3 já é compatível com qualquer bucket S3-compatible
   - [ ] Implementar via presigned URLs (TTL configurável, mantém contrato atual)
   - [ ] Bucket privado para anexos, público para fotos profissionais
   - [ ] Lifecycle policy: anexos → Glacier após 30 dias

3. **Fase C — Decisão RLS**
   - [ ] Reunião: manter RLS (defense-in-depth) ou remover (simplifica deploy RDS)?
   - [ ] Se manter: portar policies para SQL puro (sem `auth.uid()` do Supabase)
   - [ ] Se remover: migration `DROP POLICY` para todas as tabelas, validar que
         os testes de defense-in-depth no backend continuam passando

4. **Fase D — Provisionamento RDS**
   - [ ] Terraform/CDK: instância RDS Postgres 16, multi-AZ, backup 7 dias
   - [ ] Security Group: only-from-VPC do app
   - [ ] Parameter group com `pg_trgm`, `unaccent` habilitados
   - [ ] CloudWatch alarms: CPU > 80%, conexões > 80% pool, replication lag

5. **Fase E — Cutover**
   - [ ] Snapshot logical dump do Supabase (sem RLS, sem `auth.users`)
   - [ ] `pg_restore` no RDS
   - [ ] Atualizar `ConnectionStrings:Default` via env var
   - [ ] Smoke test (k6 `00-smoke.js`)
   - [ ] Load test (k6 `20-listar-pacientes.js`) — comparar com baseline
   - [ ] Cutover em janela de baixa: deploy + DNS swap

6. **Fase F — Cleanup pós-cutover (1 semana depois)**
   - [ ] Remover dependências `Supabase.Client` se ainda houver alguma referência
   - [ ] Remover Storage Supabase config (manter S3 only)
   - [ ] Decommissionar projeto Supabase

### Estimativa atualizada

- **Fase A** (LocalJwt): 1.5 sessão (mais simples agora com `LocalJwtAuthService`
  testável isoladamente — basta mockar `IUsuarioCredenciaisRepository`)
- **Fase B** (S3): 1 sessão
- **Fase C** (RLS): 0.5 sessão de discussão + 0.5 sessão de migration
- **Fase D** (RDS): 1 sessão (mais Terraform que código)
- **Fase E** (cutover): 1 sessão (testar dump→restore em staging primeiro)
- **Fase F** (cleanup): 0.5 sessão

**Total realista**: ~5 sessões de trabalho contíguas + janela de manutenção.
