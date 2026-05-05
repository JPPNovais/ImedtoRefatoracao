---
titulo: Plano de migração Supabase → AWS (greenfield, pré-produção)
status: ativo
criado_em: 2026-05-04
escopo: Substituir Supabase (Auth + Storage + Postgres) por stack 100% AWS antes do go-live, com todo o schema gerenciado por EF Core migrations.
substitui: PLANO_MIGRACAO_RDS.md (esse plano assumia produção rodando + lazy migration de usuários — cenário não se aplica mais)
---

# Plano de migração: Supabase → AWS (greenfield)

## 0. Premissa central

**Não há produção, não há usuários, não há dados a preservar.** Toda complexidade do plano antigo (coexistência de auth, lazy password reset, dump/restore, janela de cutover, rollback) **desaparece**. O que sobra é trabalho de fundação:

> Ao final deste plano, `dotnet ef database update` contra um RDS limpo deve produzir **exatamente** o schema que vai pra produção. A pasta `supabase/migrations/` deixa de existir. A palavra "Supabase" não aparece em código novo.

Cada fase é executada **em sequência** (nada paralelo). Não tem cutover — tem **cut**. Quando o último dia da Fase 5 fecha, o projeto Supabase é apagado.

---

## 1. Decisões já tomadas (não reabrir sem motivo forte)

> **Atualização 2026-05-04 — modo Free Tier ativo.** Vamos começar **zero custo** durante teste/pré-produção (12 meses Free Tier AWS). Decisões marcadas como ⚠️ têm versão "free tier" e versão "produção"; a coluna mostra ambas. Detalhes operacionais estão em [PLANO_SETUP_AWS_PASSO_A_PASSO.md](PLANO_SETUP_AWS_PASSO_A_PASSO.md), [PLANO_CICD_GITHUB_ACTIONS.md](PLANO_CICD_GITHUB_ACTIONS.md) e [PLANO_DOMINIO_SSL_EMAIL.md](PLANO_DOMINIO_SSL_EMAIL.md).

| Tema | Free Tier (agora) | Produção (futuro) | Por quê |
|---|---|---|---|
| Banco ⚠️ | **RDS Postgres `db.t4g.micro` Single-AZ, 20 GB gp3** | Aurora Serverless v2 ou RDS `db.r6g.large` Multi-AZ | Free tier cobre 750h/mês db.t4g.micro + 20 GB. Aurora **não** é free tier. |
| Compute ⚠️ | **1× EC2 `t3.micro` rodando Docker Compose** (front nginx + back .NET no mesmo host) | ECS Fargate + ALB + 2 AZs | EC2 t3.micro = 750h/mês free, suficiente pra 1 instância. ECS/ALB **não** são free tier. |
| Auth | **`LocalJwtAuthService` próprio** (BCrypt + JWT EC256) | (igual) | Sem lock-in adicional. Backend já é BFF, então sign/verify local é trivial. |
| Storage | **S3 + presigned URLs** | (igual) | Free tier 5 GB always-free + 20k GET/2k PUT/mês. |
| Pooling ⚠️ | **Conexão direta** ao endpoint RDS (Npgsql pool padrão) | RDS Proxy | RDS Proxy é US$ 15/mês fixo. Em 1 instância EC2, pool do Npgsql resolve. |
| RLS | **Removida (defense-in-depth fica no backend)** | (igual) | Backend já tem multi-tenant filter em todos os repos sensíveis. |
| Realtime | **SignalR** (já é assim) | (igual) | Nada muda. |
| IaC ⚠️ | **Console AWS + scripts shell versionados** (passo a passo manual) | Terraform | Terraform exige aprender HCL e estado remoto. Pra 1 EC2 + 1 RDS, console é mais rápido. Passamos pra Terraform na hora de subir staging/prod. |
| Segredos ⚠️ | **AWS SSM Parameter Store** (`SecureString`) | AWS Secrets Manager (rotação automática) | Parameter Store é **grátis** até 10k parâmetros standard. Secrets Manager é US$ 0,40/secret/mês. |
| E-mail transacional | **Amazon SES** | (igual) | Free tier 62.000 e-mails/mês se enviado de EC2 (que é o nosso caso). |
| DNS + SSL ⚠️ | **Cloudflare** (DNS grátis + SSL termination grátis) + **Caddy** no EC2 (Let's Encrypt automático) | Route 53 + ACM + ALB | Route 53 cobra US$ 0,50/zona/mês. Cloudflare é grátis e dá CDN de quebra. |
| CI/CD ⚠️ | **GitHub Actions + SSH pra EC2** (build local no runner, deploy via `docker compose pull/up`) | GitHub Actions + ECR + ECS deploy | 2.000 min/mês de Actions grátis em repo público (3.000 em privado free). |
| Container Registry ⚠️ | **GitHub Container Registry (ghcr.io)** | ECR | ghcr.io grátis até 500 MB privado / ilimitado público. ECR cobra US$ 0,10/GB/mês. |
| Multi-AZ / HA ⚠️ | **Não** (Single-AZ EC2 + Single-AZ RDS) | Multi-AZ + auto-scaling | Multi-AZ dobra o preço do RDS. Aceitável em teste, **inaceitável em prod com clientes**. |
| Backups | **RDS automatic backups 7 dias** (free tier) | 30 dias + snapshot manual antes de cada migration | 7 dias é o que o free tier dá. |
| Migrations | **EF Core como única fonte de schema** | (igual) | A pasta `supabase/migrations/` será apagada na Fase 5. |
| Monitoramento ⚠️ | **CloudWatch básico** (free 5 GB log ingest) + Performance Insights RDS (7 dias free) | CloudWatch + alarmes + Grafana opcional | Não pagar por dashboards customizados em teste. |

---

## 2. Estado atual — o que precisa ser substituído

### 2.1 Código (Supabase-acoplado)

| Arquivo | Substituto | Fase |
|---|---|---|
| `Infrastructure/Auth/SupabaseAuthService.cs` | `Infrastructure/Auth/LocalJwtAuthService.cs` | F1 |
| `Infrastructure/Auth/SupabaseOptions.cs` | `Infrastructure/Auth/JwtAuthOptions.cs` | F1 |
| `Infrastructure/Storage/SupabaseFotoStorageService.cs` | `Infrastructure/Storage/S3FotoStorageService.cs` | F2 |
| `Infrastructure/Storage/SupabaseStorageService.cs` (anexo) | `Infrastructure/Storage/S3AnexoStorageService.cs` | F2 |
| `Tests/.../SupabaseAuthServiceTests.cs` | `Tests/.../LocalJwtAuthServiceTests.cs` | F1 |
| `appsettings.*.json` `Supabase:*` | `Auth:Jwt:*`, `Storage:S3:*` | F1, F2 |
| `Program.cs` `options.Authority = supabase…` | `options.TokenValidationParameters` local | F1 |
| `frontend/.env.example` (menção a Supabase) | remover comentários | F4 |

### 2.2 Migrations órfãs do EF (15 arquivos `.sql` em `supabase/migrations/`)

Tudo que está em `supabase/migrations/` mas **não** tem par em `Infrastructure/Database/Migrations/`:

| Arquivo SQL | Destino |
|---|---|
| `20260419120000_rls_policies.sql` | **DELETE** (RLS removida) |
| `20260429030001_rls_fase2_tabelas_novas.sql` | **DELETE** |
| `20260429100001_rls_fase2_demais_tabelas.sql` | **DELETE** |
| `20260429171109_rls_fase3_tabelas.sql` | **DELETE** |
| `20260429200249_rls_fase3_paridade.sql` | **DELETE** |
| `20260430010242_rls_fase4_tabelas.sql` | **DELETE** |
| `20260425041842_bucket_imedto_fotos.sql` | **DELETE** (S3, sem buckets em DB) |
| `20260429040000_bucket_anexos_prontuario.sql` | **DELETE** |
| `20260419180000_atualiza_modelo_padrao_secoes.sql` | Migration EF c/ `migrationBuilder.Sql(...)` (seed) |
| `20260429100002_seed_profissoes_especialidades.sql` | Migration EF c/ `Sql(...)` (seed) |
| `20260430120000_alinha_chaves_template_legado.sql` | **DELETE** (era data fix em legado — não há legado) |
| `20260429023830_criar_ai_cache_e_rate_limit.sql` | Mapear no `AppDbContext` + EF migration |
| `20260429030000_overlap_agenda_constraint.sql` | Migration EF c/ `Sql("CREATE EXTENSION btree_gist; ALTER TABLE ... ADD CONSTRAINT ... EXCLUDE USING GIST ...")` |
| `20260430010243_lgpd_acesso_log_view.sql` | Migration EF c/ `Sql("CREATE VIEW ...")` |
| `20260503004122_pacientes_indice_trigram_nome.sql` | Migration EF c/ `Sql("CREATE EXTENSION pg_trgm; CREATE INDEX ... USING GIN ...")` |

**6 deletados, 6 viram migration EF nova, 3 deletados (legado/buckets).** O restante das migrations (`InitialCreate`, todas com nome PascalCase) já vive no EF — só precisa virar a única coisa que se aplica.

### 2.3 Connection strings / hosts

- Substituir `aws-1-sa-east-1.pooler.supabase.com:5432` (session) e `:6543` (transaction) por endpoint do **RDS Proxy** + endpoint do Aurora (somente para tooling do `dotnet ef`).
- Apagar `Supabase:Url`, `Supabase:Authority`, `Supabase:ServiceRoleKey` do `appsettings.Development.json`.
- `.mcp.json` (MCP do Supabase) — apagar na Fase 5.

---

## 3. Cronograma — 5 fases sequenciais

Estimativa total: **12 a 18 dias úteis solo**. Cada fase tem critério de "pronto" verificável (build, testes, smoke).

```
F0 Infra base                ~3 dias  →  Aurora + S3 + Cognito-livre + Secrets + Terraform
F1 Auth local (LocalJwt)     ~5 dias  →  Substituir SupabaseAuthService completamente
F2 Storage S3                ~2 dias  →  Substituir os dois services de storage
F3 Migrations EF únicas      ~3 dias  →  Converter os 15 .sql + apagar pasta supabase/
F4 Frontend + config         ~1 dia   →  Limpar referências, ajustar variáveis
F5 Cleanup                   ~1 dia   →  Apagar código Supabase, descontratar projeto
```

---

## 4. FASE 0 — Infra base (Free Tier, manual)

**Objetivo:** ter EC2 + RDS + S3 + SES + SSM Parameter Store rodando, conectáveis do laptop, **custo US$ 0**.

### Modo de execução: passo a passo manual (não Terraform)

Pra evitar a curva de Terraform/HCL agora, esta fase é executada **pelo console AWS + AWS CLI** seguindo o guia [PLANO_SETUP_AWS_PASSO_A_PASSO.md](PLANO_SETUP_AWS_PASSO_A_PASSO.md). Cada recurso criado é **registrado num arquivo `infra/aws-resources.md`** (com IDs, ARNs, endpoints) — pra não esquecer o que existe e migrar pra Terraform depois.

### Recursos que precisam existir ao fim da F0

| Recurso | Especificação | Free Tier |
|---|---|---|
| Conta AWS | Nova, com MFA na root + IAM user pessoal | — |
| VPC | **Default VPC** da região sa-east-1 (já vem pronta) | Grátis |
| Security Group `imedto-ec2-sg` | Inbound: 22 (seu IP), 80, 443 (0.0.0.0/0). Outbound: all | Grátis |
| Security Group `imedto-rds-sg` | Inbound: 5432 só do `imedto-ec2-sg` | Grátis |
| EC2 instance | `t3.micro` Amazon Linux 2023, 8 GB gp3, key pair `imedto-deploy` | 750h/mês |
| Elastic IP | 1, attached à EC2 (grátis enquanto attached) | Grátis se attached |
| RDS Postgres | `db.t4g.micro`, Postgres 17, 20 GB gp3, Single-AZ, backup 7 dias | 750h + 20 GB |
| RDS Parameter Group | Custom, com `shared_preload_libraries = pg_stat_statements` | Grátis |
| S3 bucket `imedto-fotos-dev` | Privado, encriptação SSE-S3, versionamento off | 5 GB |
| S3 bucket `imedto-anexos-dev` | Privado, SSE-S3, lifecycle Glacier após 90 dias | 5 GB |
| SES identity | `noreply@<seu-dominio>` + DKIM verificado | 62k e-mails/mês |
| SSM Parameters (`/imedto/dev/*`) | Senhas, JWT keys, pepper, conn strings (todos `SecureString`) | Grátis (≤10k) |
| IAM role `imedto-ec2-role` | Policies: S3 buckets, SSM Parameters, SES SendEmail, CloudWatch Logs | Grátis |
| Cloudflare DNS | Zona do domínio + registro A apontando pra Elastic IP | Grátis |

### Checklist resumida

- [ ] Seguir [PLANO_SETUP_AWS_PASSO_A_PASSO.md](PLANO_SETUP_AWS_PASSO_A_PASSO.md) seção 1–8.
- [ ] Anotar **todos** os IDs/ARNs/endpoints em `infra/aws-resources.md`.
- [ ] Configurar **billing alarm** em US$ 1 — qualquer cobrança fora do free tier dispara e-mail.
- [ ] Validar conexões: SSH no EC2, `psql` do EC2 no RDS, `aws s3 ls` do EC2.

### Verificação de pronto

```bash
# Do laptop:
ssh -i ~/.ssh/imedto-deploy.pem ec2-user@<elastic-ip>

# No EC2:
psql -h <rds-endpoint> -U imedto -d imedto -c "SELECT version();"
aws s3 ls s3://imedto-anexos-dev/
aws ssm get-parameter --name /imedto/dev/db-password --with-decryption
```

Os três comandos retornam sucesso → F0 fechada.

**Não passar pra Fase 1 sem isso pronto.**

---

## 5. FASE 1 — Auth local (LocalJwtAuthService)

**Objetivo:** `IAuthService` deixa de ser implementado por Supabase. Toda autenticação (signup, login, refresh, logout, recuperação, convite, exclusão) roda 100% no backend contra Aurora + SES.

### 5.1 Schema novo (1 migration EF)

```csharp
// Migration: AddAuthLocal
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS citext;");
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
```

Tabelas (configurações em `Infrastructure/Database/Configurations/Auth/`):

```
auth_credenciais
  usuario_id          uuid PK (mesmo id de public.usuarios)
  email               citext UNIQUE NOT NULL
  senha_hash          text NOT NULL          -- bcrypt(senha + pepper) cost 12
  email_confirmado_em timestamptz NULL
  bloqueado_em        timestamptz NULL
  motivo_bloqueio     text NULL
  tentativas_falhas   int NOT NULL DEFAULT 0
  ultimo_login_em     timestamptz NULL
  criado_em           timestamptz NOT NULL DEFAULT now()
  atualizado_em       timestamptz NOT NULL DEFAULT now()

auth_refresh_tokens
  id          bigserial PK
  usuario_id  uuid FK auth_credenciais ON DELETE CASCADE
  token_hash  text UNIQUE NOT NULL           -- SHA-256(refresh_token)
  expira_em   timestamptz NOT NULL
  revogado_em timestamptz NULL
  ip_origem   inet NULL
  user_agent  text NULL
  criado_em   timestamptz NOT NULL DEFAULT now()

  INDEX (usuario_id) WHERE revogado_em IS NULL
  INDEX (expira_em)  WHERE revogado_em IS NULL

auth_email_tokens
  id           bigserial PK
  usuario_id   uuid FK auth_credenciais ON DELETE CASCADE
  tipo         text NOT NULL                 -- 'confirmacao_email' | 'reset_senha' | 'convite'
  token_hash   text UNIQUE NOT NULL
  expira_em    timestamptz NOT NULL
  consumido_em timestamptz NULL
  criado_em    timestamptz NOT NULL DEFAULT now()
```

### 5.2 FK `public.usuarios.id → public.auth_credenciais.usuario_id`

Hoje a FK aponta pra `auth.users`. Substitui:

```csharp
migrationBuilder.Sql(@"
    ALTER TABLE public.usuarios
        ADD CONSTRAINT fk_usuarios_auth_credenciais
        FOREIGN KEY (id) REFERENCES public.auth_credenciais(usuario_id)
        ON DELETE CASCADE;
");
```

(Antes, na fase 3, as migrations já não terão a FK pra `auth.users` — vai nascer correta direto.)

### 5.3 Implementação `LocalJwtAuthService`

Arquivo: `Infrastructure/Auth/LocalJwtAuthService.cs`. Implementa **toda** a interface `IAuthService` (8 métodos):

| Método | Como |
|---|---|
| `SignupAsync` | Cria `auth_credenciais` (com hash bcrypt + pepper), cria `auth_email_tokens` tipo `confirmacao_email` (TTL 24h), envia e-mail via SES, retorna `SignupResult` sem sessão (espera confirmação). |
| `LoginAsync` | Valida hash, checa `bloqueado_em` e `email_confirmado_em`, incrementa `tentativas_falhas` em falha (bloqueio em 10), emite par access (15 min) + refresh (30 dias). Persiste hash do refresh em `auth_refresh_tokens`. |
| `RefreshAsync` | Hash do refresh recebido → busca em `auth_refresh_tokens`, verifica `expira_em` e `revogado_em`, **rotaciona** (revoga atual + emite novo). |
| `LogoutAsync` | Marca `revogado_em = now()` no refresh token correspondente. |
| `GetUserAsync` | Lê claims do JWT (sub, email, email_verified). Não bate banco. |
| `DeleteUserAsync` | DELETE em `auth_credenciais` (FK CASCADE remove tudo). LGPD: log em `lgpd_exclusao_log` com `usuario_id` e `motivo`. |
| `CriarConviteAsync` | Cria `auth_credenciais` com `email_confirmado_em = NULL` e `senha_hash = NULL` + token tipo `convite` (TTL 7d). Retorna URL `https://app.imedto.com.br/convite?t=<token>`. |
| `EnviarRecuperacaoSenhaAsync` | Cria token tipo `reset_senha` (TTL 1h), envia e-mail. **Não revela** se o e-mail existe (LGPD). |

### 5.4 Endpoints novos no `AuthController`

Já existem hoje, mas alguns precisam de ajuste de payload:

- `POST /api/auth/confirmar-email` — body `{ token }`. Marca `email_confirmado_em`.
- `POST /api/auth/redefinir-senha` — body `{ token, novaSenha }`. Valida força (mín 12 chars, regra simples).
- `POST /api/auth/aceitar-convite` — body `{ token, novaSenha, nomeCompleto }`. Define hash + `email_confirmado_em`.

### 5.5 Validação JWT no `Program.cs`

Trocar:
```csharp
options.Authority = "https://kdoqflrmfgazdgekdbqc.supabase.co/auth/v1";
```
Por:
```csharp
var publicKey = await secrets.GetAsync("imedto/jwt/signing-key-public");
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidIssuer = "imedto-backend",
    ValidAudience = "imedto-app",
    IssuerSigningKey = new ECDsaSecurityKey(LoadEC(publicKey)),
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromSeconds(30),
};
```

Manter cookies HttpOnly como hoje. **Frontend não muda nada** — continua chamando `/api/auth/login`, `/api/auth/me`, `/api/auth/refresh`.

### 5.6 BCrypt + pepper

```csharp
public string Hash(string senha)
    => BCrypt.Net.BCrypt.HashPassword(senha + _pepper, workFactor: 12);
```

Pepper vem do Secrets Manager. **Nunca no banco**, nunca em log. Rotação de pepper exige recadastro de senha (problema futuro — deixar TODO documentado).

### 5.7 Testes

- Mover `SupabaseAuthServiceTests.cs` → `LocalJwtAuthServiceTests.cs` adaptando os mocks (sem `IHttpClientFactory`, com `IAuthCredencialRepository` + `IRefreshTokenRepository` + `IEmailService`).
- Adicionar `LocalJwtAuthServiceIntegrationTests` com Testcontainers Postgres testando: signup completo, login com bloqueio após 10 falhas, refresh rotation, expiração de token de e-mail, LGPD delete cascading.

### Verificação de pronto da F1

- [ ] `dotnet build` limpo, zero referências a `Supabase` em código de produção (apenas em arquivos a serem deletados na F5).
- [ ] `dotnet test` 100% verde.
- [ ] Smoke manual: signup → recebe e-mail SES → confirma → login → `GET /api/auth/me` retorna usuário → logout.
- [ ] Container DI: `IAuthService` resolve para `LocalJwtAuthService` (não há mais `if "Supabase"` no `Container.cs`).

---

## 6. FASE 2 — Storage S3

**Objetivo:** os dois services de storage saem 100% AWS. Sem buckets do Supabase, sem chaves de Storage.

### 6.1 Implementação

Pacote: `AWSSDK.S3`.

```csharp
// Infrastructure/Storage/S3FotoStorageService.cs
public class S3FotoStorageService : IFotoStorageService
{
    public async Task<Uri> EnviarFotoEstabelecimentoAsync(int estabelecimentoId, Stream conteudo, string contentType)
    {
        var key = $"estabelecimentos/{estabelecimentoId}/foto-{Guid.NewGuid():N}.jpg";
        await _s3.PutObjectAsync(new PutObjectRequest {
            BucketName = _options.BucketFotos,
            Key = key,
            InputStream = conteudo,
            ContentType = contentType,
            // bucket público + CloudFront na frente em prod
        });
        return new Uri($"https://{_options.CdnDomain}/{key}");
    }

    public async Task ExcluirAsync(string urlOuChave) { /* derive key, DELETE */ }
}

// Infrastructure/Storage/S3AnexoStorageService.cs (privado, presigned URL)
public class S3AnexoStorageService : IAnexoStorageService
{
    public async Task<string> EnviarAsync(int estabelecimentoId, int pacienteId, string nomeArquivo, Stream conteudo, string contentType)
    {
        var key = $"est_{estabelecimentoId}/paciente_{pacienteId}/{Guid.NewGuid():N}_{nomeArquivo}";
        await _s3.PutObjectAsync(new PutObjectRequest {
            BucketName = _options.BucketAnexos, Key = key,
            InputStream = conteudo, ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS,
        });
        return key;
    }

    public Uri GerarUrlAssinada(string storagePath, TimeSpan ttl)
        => new(_s3.GetPreSignedURL(new GetPreSignedUrlRequest {
            BucketName = _options.BucketAnexos, Key = storagePath,
            Expires = DateTime.UtcNow.Add(ttl), Verb = HttpVerb.GET,
        }));
}
```

### 6.2 Configuração

```json
"Storage": {
  "S3": {
    "Region": "sa-east-1",
    "BucketFotos": "imedto-fotos-prod",
    "BucketAnexos": "imedto-anexos-prod",
    "CdnDomain": "cdn.imedto.com.br",
    "PresignedUrlTtlSeconds": 300
  }
}
```

Credenciais: **IAM role** em prod (ECS task role); em dev, perfil AWS local (`~/.aws/credentials`).

### 6.3 DI

`Container.cs` registra direto as implementações S3. Sem fábrica, sem flag — Supabase saiu.

### 6.4 Testes

- `S3FotoStorageServiceTests` mockando `IAmazonS3` (presigned URL é determinístico, dá pra asserir).
- Teste de integração opcional usando **LocalStack** em Testcontainers.

### Verificação de pronto da F2

- [ ] `grep -r "Supabase.*Storage" backend/src/Services/Imedto.Backend.{Application,API}/` retorna vazio.
- [ ] Upload manual de foto via API → arquivo aparece no bucket S3.
- [ ] Presigned URL de anexo válido por 5 min, retorna 403 após.

---

## 7. FASE 3 — Migrations EF únicas

**Objetivo:** zero arquivos `.sql` órfãos. Toda alteração de schema é uma migration EF. Pasta `supabase/migrations/` deletada ao fim.

### 7.1 Reset (uma única vez)

Como **não há dado em produção**, a opção limpa é:

1. Apagar o histórico de migrations EF e SQL atuais.
2. Recriar uma única migration `InitialCreate` que reflete o `AppDbContext` final + as 6 conversões dos `.sql` órfãos.

Não é obrigatório — é o mais limpo. Alternativa preservacionista: manter as 37 migrations EF existentes e adicionar 6 novas convertendo os `.sql`. Recomendo o **reset**: as migrations atuais foram escritas como evolução incremental de um banco vivo — refletem decisões antigas, têm nome esquisito (`Fase6_1bProdutos`), e o histórico não tem valor sem o banco que ele construiu.

### 7.2 Procedimento de reset

```bash
cd backend/src

# 1. Apaga migrations EF e snapshot
rm -rf Services/Imedto.Backend.Infrastructure/Database/Migrations/*
rm Services/Imedto.Backend.Infrastructure/Database/Migrations/AppDbContextModelSnapshot.cs 2>/dev/null

# 2. Apaga todas as migrations Supabase
rm -rf ../../supabase/migrations/*

# 3. Aurora limpa: drop schema public; create schema public;
./scripts/connect-db.sh -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"

# 4. Cria migration única
dotnet ef migrations add InitialCreate \
    --project Services/Imedto.Backend.Infrastructure \
    --startup-project Services/Imedto.Backend.API \
    --output-dir Database/Migrations
```

### 7.3 Adicionar conteúdo dos 6 `.sql` recuperáveis

Editar `InitialCreate.cs` (ou criar migrations subsequentes pequenas) e adicionar `migrationBuilder.Sql(...)` com o conteúdo de:

1. **`atualiza_modelo_padrao_secoes.sql`** → seed do modelo padrão de prontuário.
2. **`seed_profissoes_especialidades.sql`** → seed de profissões e especialidades.
3. **`overlap_agenda_constraint.sql`** → `CREATE EXTENSION btree_gist;` + EXCLUDE constraint na agenda.
4. **`lgpd_acesso_log_view.sql`** → `CREATE VIEW vw_lgpd_acesso_log AS ...`.
5. **`pacientes_indice_trigram_nome.sql`** → `CREATE EXTENSION pg_trgm;` + `CREATE INDEX USING GIN`.
6. **`criar_ai_cache_e_rate_limit.sql`** → mapear no `AppDbContext` (entidades `AiCache`, `AiRateLimit`) **OU** se forem só infraestrutura sem domínio, criar migration EF com `Sql(...)` puro.

Recomendação: as três extensões (`btree_gist`, `pg_trgm`, `pgcrypto`, `citext`) ficam em uma migration `EnableExtensions` rodando **antes** do `InitialCreate` para o EF não brigar com tipos como `citext`.

### 7.4 Aplicação

```bash
# Direto pelo dotnet ef agora — Supabase CLI saiu
dotnet ef database update \
    --project Services/Imedto.Backend.Infrastructure \
    --startup-project Services/Imedto.Backend.API \
    --connection "Host=<aurora-endpoint>;..."
```

Connection string `Migrations` no `appsettings.Development.json` aponta pro endpoint do **Aurora direto** (não pelo Proxy — Proxy é só pra runtime). Endpoint readonly Aurora = não usar; usar writer endpoint.

### 7.5 Atualizar `CLAUDE.md`

Substituir a seção `## Migrations (fluxo DUPLO ...)` por uma seção curta:

```
## Migrations

Fluxo simples — EF Core é a única fonte de schema:

1. dotnet ef migrations add <Nome>
2. dotnet ef database update (dev) ou dotnet ef migrations script + Flyway/manual em prod
```

### Verificação de pronto da F3

- [ ] `ls supabase/migrations/` retorna vazio.
- [ ] `dotnet ef database update` num Aurora limpo cria todas as tabelas + extensions + seeds + views + constraints especiais.
- [ ] Toda a suite de testes (Testcontainers Postgres) passa contra a migration única.
- [ ] App sobe contra Aurora, smoke test passa (criar estabelecimento, profissional, paciente, agendamento — exercita os principais inserts).

---

## 8. FASE 4 — Frontend e config

**Objetivo:** apagar última menção a Supabase fora dos arquivos a deletar.

### Checklist

- [ ] `frontend/.env.example`: remover comentários sobre Supabase. Manter só `VITE_API_BASE_URL`.
- [ ] `frontend/.env`: remover variáveis `VITE_SUPABASE_*` se existirem (sanity check — BFF não devia tê-las).
- [ ] `appsettings.Development.json`: remover bloco `Supabase`, adicionar `Auth:Jwt`, `Storage:S3`, `Email:Ses`.
- [ ] `appsettings.Development.json.example` (criar se não existir): template das novas configs sem segredo.
- [ ] `.mcp.json` e `.mcp.json.example`: remover MCP do Supabase. Adicionar MCP do Aurora se usar (postgres MCP genérico).
- [ ] `Container.cs`: remover `services.Configure<SupabaseOptions>(...)` e `AddHttpClient("supabase", ...)`.
- [ ] `Program.cs`: remover lógica de `Supabase:Authority`.

### Verificação de pronto da F4

```bash
# Não pode retornar nada exceto o próprio plano e arquivos a deletar na F5
grep -ri "supabase" backend/src --include="*.cs" --include="*.json" \
  | grep -v "Auth/Supabase" | grep -v "Storage/Supabase"
```

---

## 9. FASE 5 — Cleanup final (cut, não cutover)

**Objetivo:** apagar todo o código Supabase. Cancelar o projeto.

### Checklist

- [ ] `git rm` em:
  - `Infrastructure/Auth/SupabaseAuthService.cs`
  - `Infrastructure/Auth/SupabaseOptions.cs`
  - `Infrastructure/Storage/SupabaseFotoStorageService.cs`
  - `Infrastructure/Storage/SupabaseStorageService.cs`
  - `Tests/.../SupabaseAuthServiceTests.cs`
  - `supabase/` (a pasta inteira: `config.toml`, `seed.sql`, `migrations/`)
- [ ] `dotnet build` limpo. `dotnet test` 100% verde.
- [ ] CLAUDE.md: remover toda menção a Supabase do "Overview", "Comandos", "Conexão Supabase". Substituir por seção "Conexão AWS / Aurora".
- [ ] Memórias do agente: atualizar `project_migracao_rds_restricoes.md` para refletir "migração concluída" ou remover.
- [ ] **Cancelar projeto Supabase** via dashboard (`kdoqflrmfgazdgekdbqc`).
- [ ] Remover PAT Supabase do gerenciador de secrets local.
- [ ] Atualizar README com nova arquitetura.

### Verificação de pronto da F5

```bash
# Zero. Tem que retornar zero linhas.
grep -ri "supabase" \
  --include="*.cs" --include="*.json" --include="*.ts" --include="*.vue" \
  --include="*.md" --include="*.tf" \
  --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj
```

Se retornar linha, voltar e apagar — sem **nenhuma** referência ao terminar.

---

## 10. Anti-padrões (não regredir após o cut)

Depois da F5, qualquer linha de código nova viola o plano se:

- ❌ Usa `auth.uid()` em SQL.
- ❌ Cria FK pra schema `auth.*` ou `storage.*`.
- ❌ Adiciona NuGet `supabase-csharp`, `Postgrest-csharp`, ou similar.
- ❌ Cria policies em `storage.objects`.
- ❌ Fala em "edge function", "RPC do banco" ou "trigger com lógica de negócio". Lógica vive no backend.
- ❌ Volta o fluxo duplo de migration (EF + SQL na pasta supabase). EF é a única fonte.
- ❌ Mantém o nome `service_role` em qualquer config — Aurora não tem isso.

A revisão de PR deve ter checklist com esses 7 itens.

---

## 11. Decisões em aberto (resolver antes ou durante)

1. **CDN pras fotos**: CloudFront na frente do bucket público? Provavelmente sim em prod, mas dá pra começar sem (S3 público bate direto). **Decidir até F2.**
2. **MFA**: o `IAuthService` atual não tem TOTP. Adicionar agora (junto com signup) ou pós-go-live? **Recomendo pós-go-live** — não trava o cut.
3. **Login social** (Google/Apple): hoje não existe. **Pós-go-live**, requer flow OIDC novo.
4. **Backup**: snapshot diário Aurora é automático. Definir retenção (7 dias dev, 30 dias prod). **Decidir em F0.**
5. **Multi-região / DR**: provavelmente não — Aurora Global Database é caro e Brasil tem só sa-east-1 hoje. **Decidir só se cliente exigir SLA cross-region.**

---

## 12. Riscos e mitigações

| Risco | Probabilidade | Mitigação |
|---|---|---|
| Reset de migrations perde algum detalhe sutil de schema | Média | Diff binário antes/depois com `pg_dump --schema-only` do Aurora vs Supabase staging — comparar antes do cut. |
| BCrypt cost 12 lento em laptop dev | Baixa | É 200-300ms — aceitável. Em prod (ECS), é instantâneo no contexto de uma request. |
| SES sandbox bloqueia e-mail pra qualquer destinatário | **Alta** | Solicitar production access **na F0**, leva 24-48h. Sem isso, signup em dev só funciona pra e-mails verificados. |
| RDS Proxy + IAM auth tem latência maior que pgbouncer Supabase | Baixa | Mensurável (~5ms a mais). RDS Proxy também aceita auth por senha — fallback se IAM auth pesar. |
| Aurora Serverless v2 escala devagar em pico | Média | Configurar mínimo de 1 ACU em prod (não 0,5) para reduzir cold spike. |
| EF migrations não geram SQL idempotente para `EXCLUDE` constraint com `btree_gist` | Média | Usar `Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;")` antes da constraint. Testar idempotência aplicando 2x. |

---

## 13. Resumo executável

```
SEMANA 1: F0 — Terraform up. Aurora, S3, Secrets, SES, IAM funcionais.
SEMANA 2: F1 — LocalJwtAuthService completo. Testes verdes.
SEMANA 3: F1 fim + F2 — S3 storage substituído.
SEMANA 4: F3 — Reset de migrations + 6 conversões. Aurora rodando schema final.
SEMANA 5: F4 + F5 — Limpeza, descontratação Supabase. Stack 100% AWS.
```

**Marco de conclusão**: o último commit fecha com `git rm -r supabase/` e o `grep -ri supabase` do passo 9 retorna zero. A partir daí, toda evolução de schema é uma migration EF aplicada via `dotnet ef database update` (dev) ou `dotnet ef migrations script | psql` (prod). Sem CLI de terceiro, sem fluxo duplo, sem pasta paralela.

---

## Apêndice A — Custo mensal estimado

### A.1 Free Tier (primeiros 12 meses, 1× usuário interno)

| Recurso | Configuração | Custo (sa-east-1) |
|---|---|---|
| EC2 `t3.micro` | 1 instância, 24×7 (744h) | **US$ 0** (≤750h free) |
| EBS gp3 8 GB (volume EC2) | Single-AZ | **US$ 0** (≤30 GB free) |
| Elastic IP | 1, attached | **US$ 0** (attached = grátis) |
| RDS `db.t4g.micro` | Single-AZ, 24×7 | **US$ 0** (≤750h free) |
| RDS storage 20 GB gp3 | + 20 GB backup | **US$ 0** (free) |
| S3 | 5 GB + 20k GET + 2k PUT | **US$ 0** |
| SES | E-mails enviados via EC2 | **US$ 0** (≤62k/mês) |
| SSM Parameter Store | ~20 parâmetros standard | **US$ 0** |
| CloudWatch Logs | <5 GB ingest/mês | **US$ 0** |
| Data transfer out | <100 GB/mês | **US$ 0** |
| Cloudflare DNS + SSL | Plano free | **US$ 0** |
| GitHub Actions | <2.000 min/mês (privado) | **US$ 0** |
| ghcr.io | <500 MB de imagens privadas | **US$ 0** |
| **Total free tier** | | **US$ 0/mês** |

### A.2 Após os 12 meses (mesma config, sem upgrades)

| Recurso | Custo aproximado |
|---|---|
| EC2 t3.micro Linux 24×7 | ~US$ 8/mês |
| RDS db.t4g.micro Single-AZ | ~US$ 13/mês |
| RDS storage 20 GB gp3 | ~US$ 2,50/mês |
| EBS 8 GB | ~US$ 0,80/mês |
| Elastic IP attached | US$ 0 |
| S3 5 GB | ~US$ 0,12/mês |
| Outros | ~US$ 1/mês |
| **Total** | **~US$ 25–30/mês** |

### A.3 Produção real (~100k DAU healthtech)

| Recurso | Configuração | Custo (sa-east-1) |
|---|---|---|
| ECS Fargate | 2× tasks 1 vCPU/2 GB | ~US$ 60/mês |
| ALB | 1 | ~US$ 22/mês |
| Aurora Serverless v2 | 1–4 ACU média | ~US$ 200–400/mês |
| RDS Proxy | 1 endpoint | ~US$ 15/mês |
| S3 + CloudFront | ~50 GB + 200 GB egress | ~US$ 30/mês |
| SES | 100k e-mails | ~US$ 10/mês |
| Secrets Manager | 6 secrets + rotação | ~US$ 2,40/mês |
| CloudWatch | 50 GB logs + alarmes | ~US$ 25/mês |
| Route 53 | 1 zona + queries | ~US$ 1/mês |
| ACM | Certificados gratuitos | US$ 0 |
| Data transfer | inter-AZ + egress | ~US$ 30/mês |
| **Total** | | **~US$ 400–600/mês** |

Reserved Instance / Savings Plan derrubam ~30–40% após estabilizar a carga.
