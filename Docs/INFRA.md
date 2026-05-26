# Infraestrutura AWS

> **Quando ler**: ao mexer em deploy, CI/CD, EC2, RDS, S3, SSM, e-mail transacional, DNS, secrets, certificados, configuração de containers Docker, ou ao diagnosticar problemas de produção.
>
> **Quando atualizar**: a cada mudança em recurso AWS (criar/destruir/redimensionar), troca de provider de e-mail, novo parameter no SSM, mudança de SG/IAM/role, alteração no `deploy/`. **Responsabilidade primária: `imedto-business-analyst`** documenta decisão; `imedto-developer` / `imedto-database` atualizam após executar a mudança. **Inventário completo com IDs/ARNs** em [infra/aws-resources.md](../infra/aws-resources.md).

---

## Conta e região

| Item | Valor |
|---|---|
| Account ID | `155684258219` |
| Região default | `sa-east-1` (São Paulo) |
| IAM user pessoal (CLI) | `joao-admin` (AdministratorAccess; sempre `AWS_PROFILE=imedto`) |
| URL pública | **https://app.imedto.com** (também `imedto.com` e `www.imedto.com`) |

## Compute (1× EC2 — Free Tier; deploy via Docker Compose)

| Item | Valor |
|---|---|
| Instance | `i-0c4f75999438cc5c2` — `t3.micro` (2 vCPU, 1 GB RAM), AL2023 x86_64 |
| Elastic IP | **`56.125.254.136`** (alloc `eipalloc-0a976f5551c1794ae`) |
| Key pair | `imedto-deploy` → `~/.ssh/imedto-deploy.pem` no laptop |
| IAM role | `imedto-ec2-role` (S3 buckets + SSM `/imedto/*` + SES Send + CloudWatch Logs) |
| Bootstrap | Docker 25 + Compose v2 + psql 15 instalados via `user-data` |
| Containers (Docker Compose em `/home/ec2-user/imedto/`) | `imedto-caddy` (TLS Let's Encrypt nas portas 80/443) + `imedto-frontend` (nginx servindo SPA) + `imedto-backend` (.NET 10 API) |
| Acesso | `ssh -i ~/.ssh/imedto-deploy.pem ec2-user@56.125.254.136` |

> **Atenção:** SG da EC2 (`sg-0555c057e7d4dc46b`) libera SSH `0.0.0.0/0` (necessário pro GitHub Actions runner). Em prod, restringir a ranges específicos.

## Database (1× RDS Postgres — Free Tier)

| Item | Valor |
|---|---|
| Identifier | `imedto-dev` |
| Engine | Postgres 17.2 — `db.t4g.micro` Single-AZ, 20 GB gp3 (encriptado) |
| Endpoint | `imedto-dev.cx0648wywxg8.sa-east-1.rds.amazonaws.com:5432` |
| Database de aplicação | `imedto` (master user `imedto`, senha em SSM) |
| SG (`imedto-rds-sg`) | porta 5432 **só** do `imedto-ec2-sg` (RDS é privado) |
| Backups | 0 dias (limitação do Free Tier "new plan" — ativar quando sair do Free Tier) |
| Extensions instaladas | `pg_trgm`, `unaccent`, `btree_gist`, `pgcrypto`, `citext` |

**Acesso ao RDS** (sem endpoint público):
```bash
# Túnel via EC2 (laptop):
ssh -i ~/.ssh/imedto-deploy.pem -L 5432:imedto-dev.cx0648wywxg8.sa-east-1.rds.amazonaws.com:5432 ec2-user@56.125.254.136
# Outro terminal:
PGPASSWORD=$(aws ssm get-parameter --name /imedto/dev/db-password --with-decryption --query Parameter.Value --output text) \
    psql -h localhost -U imedto -d imedto
```

## Storage (S3 — 2 buckets privados)

| Bucket | Uso | Política |
|---|---|---|
| `imedto-fotos-155684258219` | Fotos de profissional/estabelecimento | Privado, AES256, presigned URL TTL 24h |
| `imedto-anexos-155684258219` | Anexos de prontuário (LGPD sensível) | Privado, AES256, presigned URL TTL 5 min, Glacier após 90d |

Backend acessa via `IFotoStorageService` / `IAnexoStorageService` (`Domain.Common` / `Domain.Prontuarios`) → implementações `S3FotoStorageService` / `S3AnexoStorageService`. Credenciais via IAM role da EC2.

Limites configuráveis em `Storage:*` (appsettings):
- `TamanhoMaxMb` = 50
- `MimeTypesPermitidos` = pdf, png, jpeg, webp, dicom

**CORS (obrigatório):** ambos os buckets têm `AllowedOrigins` = `https://app.imedto.com`, `https://www.imedto.com`, `https://imedto.com`, `http://localhost:3000`, `http://localhost:5173`; `AllowedMethods` = `GET, HEAD`. Sem isso, o frontend não consegue baixar a presigned URL via `fetch()` (necessário para os PDFs gerados pelo `usePdfHeader.ts` — Prontuário, Orçamento, Relatório). A falha é silenciosa: o helper cai no placeholder com iniciais. Config em [`infra/aws-resources.md`](../infra/aws-resources.md) (seção S3).

## Segredos (AWS SSM Parameter Store, prefixo `/imedto/dev/`)

| Parameter | Tipo | Conteúdo |
|---|---|---|
| `db-host`, `db-password` | String + SecureString | RDS endpoint + senha master |
| `jwt/private-key`, `jwt/public-key` | SecureString × 2 | EC P-256 PEM (assina/valida JWT local) |
| `jwt/issuer`, `jwt/audience` | String × 2 | `imedto-backend` / `imedto-app` |
| `bcrypt/pepper` | SecureString | Pepper aplicado em HMAC-SHA256 antes do bcrypt |
| `email/from` | String | `noreply@imedto.com` |
| `email/provider` | String | `Resend` (default) ou `Ses` — controla o provider em runtime |
| `resend/api-key` | SecureString | API key Resend (escopo Sending + domain `imedto.com`) |
| `s3/bucket-fotos`, `s3/bucket-anexos` | String × 2 | Nomes dos buckets |
| `aws/region` | String | `sa-east-1` |
| `ghcr-token` | SecureString | GitHub PAT (escopo `read:packages`) — EC2 puxa imagens do ghcr.io |

EC2 lê via IAM role; CLI/laptop lê via `aws ssm get-parameter`. Nunca colocar em repo.

## E-mail transacional (provider pluggable)

Provider escolhido em runtime via `Email:Provider` (lido de SSM `/imedto/dev/email/provider`):

| Provider | Quando usar | Limitação |
|---|---|---|
| **Resend** (atual) | Default, dev e prod | Free tier 3.000 e-mails/mês, 100/dia. Sem sandbox. |
| **SES** (alternativo) | Quando volume crescer | Free tier 62.000/mês via EC2; **sandbox** por default — pedir production access no Console SES (24-48h). |
| `NoOp` | Auto se nenhuma key configurada | Só loga, não envia |

DKIM dos dois providers já configurado no Route 53 (`resend._domainkey`, `<token>._domainkey` SES).

**Trocar provider em prod:**
```bash
aws ssm put-parameter --name /imedto/dev/email/provider --value "Ses" --type String --overwrite
ssh -i ~/.ssh/imedto-deploy.pem ec2-user@56.125.254.136 'cd ~/imedto && ./scripts/pull-secrets.sh && docker compose restart backend'
```

## DNS (Route 53)

Zona `imedto.com` (hosted zone `Z01357441MJ00U1TI5J95`):

| Tipo | Nome | Valor | Uso |
|---|---|---|---|
| A | `imedto.com`, `www.imedto.com`, `app.imedto.com` | `56.125.254.136` | Aponta tudo pra EC2 |
| MX + SPF + DKIM `secureserver*` | `imedto.com` | secureserver.net | E-mail GoDaddy (recebimento) |
| TXT (DKIM) | `resend._domainkey` | (Resend) | Resend assina e-mails |
| CNAME (DKIM) ×3 | `<token>._domainkey` | `*.dkim.amazonses.com` | SES Easy DKIM |
| MX + SPF | `send.imedto.com` | `feedback-smtp.sa-east-1.amazonses.com` | SES bounce/complaint |
| TXT (DMARC) | `_dmarc` | `v=DMARC1; p=none;` | DMARC monitoring |

⚠️ **Não apagar `send.imedto.com` MX/TXT** — são necessários pro Resend (que usa infra SES por baixo).

## CI/CD (GitHub Actions — workflow único)

Arquivo: [`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml)

**Em PR**: roda `test-backend` + `test-frontend` (paralelo).
**Em push pra `main`**: roda os 2 testes + `build-push` (Docker → ghcr.io) **em paralelo** → `migrate` (gera SQL idempotente + aplica via SSH no RDS) → `deploy` (rsync + docker compose pull/up via SSH na EC2) → `smoke` (`curl /health`).

⚠️ **1 push só por sessão de trabalho**: cada push em `main` dispara o pipeline de ~3-5 min e um deploy de produção. Se você fez várias mudanças numa mesma sessão, faça vários commits localmente se quiser (1 commit por mudança lógica é OK), mas **agrupe tudo num único `git push`**. Não fazer pushes sequenciais "um por commit" — desperdiça runners, polui o histórico de deploy e atrasa feedback. Se já deu push e percebeu que faltou algo pequeno, ainda assim espere reunir o próximo bloco de mudanças antes de subir de novo.

| Otimizações | |
|---|---|
| Cache NuGet | `actions/cache` em `~/.nuget/packages` |
| Cache Docker | `cache-from/to: type=gha,mode=min,scope=<frontend\|backend>` |
| Tempo típico | ~3-5 min após o cache popular (1ª vez ~8 min) |

GitHub Secrets necessários (já configurados):
- `EC2_SSH_KEY` — conteúdo de `~/.ssh/imedto-deploy.pem`
- `EC2_HOST` — `56.125.254.136`

Imagens publicadas:
- `ghcr.io/jppnovais/imedto-backend:<sha7>` + `:latest`
- `ghcr.io/jppnovais/imedto-frontend:<sha7>` + `:latest`

## Decisões já tomadas (não reabrir sem motivo forte)

1. **Free Tier first**: 1 EC2 + 1 RDS + S3, tudo em sa-east-1. Próximo upgrade: Multi-AZ + ECS Fargate quando sair de teste.
2. **Auth local** (`LocalJwtAuthService` + ECDSA P-256). Sem Cognito (lock-in maior). Refresh tokens persistidos com SHA-256 hash em `auth_refresh_tokens`.
3. **Storage S3 sempre privado**, acesso via presigned URL. Sem CloudFront em dev (não é Free Tier).
4. **Caddy + Let's Encrypt** em vez de ALB + ACM (Caddy renova automático, mantém volume `caddy-data` persistente).
5. **Resend default**, SES opcional via flag. Quando SES sair do sandbox, vira default (4× mais barato).
6. **Migration EF é a única fonte de schema**. Não criar `.sql` solto fora de `db/migrations/`.
7. **Cooldown 5 min** em reenvio de e-mail (`auth_email_tokens` consulta `criado_em`) — anti-spam idempotente.
8. **Anti-enumeração**: forgot-password e reenviar-confirmacao **sempre** retornam 204 (nunca revelam se e-mail existe).
9. **Sem RLS no Postgres**. Defense-in-depth fica no backend (filter multi-tenant + `[RequiresPapel]` + mensagens genéricas).
10. **Custo mensal estimado**: US$ 0,50 (apenas Route 53 hosted zone). Resto Free Tier 12 meses.

## Pra cleanup ou recriar tudo

Comandos completos em [`infra/aws-resources.md`](../infra/aws-resources.md). Resumo:
- **Não derrubar a EC2/RDS** sem aviso — perde dados e quebra produção.
- Pra trocar instance type/migrar pra Aurora: `pg_dump` no RDS atual, `pg_restore` no novo, depois apontar `Storage:Region` e `db-host` SSM. Backend reinicia sozinho com nova connection.

## Onde ficam os secrets

Todos gitignored (ver `.gitignore`):
- `.mcp.json` — configuração de MCP servers locais (vazia por padrão; templates em `.mcp.json.example`).
- `backend/src/Services/Imedto.Backend.API/appsettings.Development.json` — connection strings (Default + Migrations), chaves PEM do JWT (`Auth:Jwt:PrivateKeyPem`/`PublicKeyPem`), pepper do BCrypt (`Auth:Bcrypt:Pepper`), API key do Resend, buckets S3.
- `frontend/.env` — apenas `VITE_API_BASE_URL` opcional (frontend é BFF puro, sem segredos).

Templates em `.mcp.json.example` e `frontend/.env.example`. Em produção, todos os valores vêm do AWS SSM Parameter Store.
