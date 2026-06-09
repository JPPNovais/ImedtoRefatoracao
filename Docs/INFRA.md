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
| Containers (Docker Compose em `/home/ec2-user/imedto/`) | `imedto-caddy` (TLS Let's Encrypt nas portas 80/443) + `imedto-frontend` (nginx servindo SPA) + `imedto-backend` (.NET 10 API) + `imedto-postgres` (Postgres 17, banco local — ver seção Database) |
| Swap | 2 GB (`/swapfile`, `vm.swappiness=10`) — rede de segurança de RAM com o Postgres co-residente |
| Acesso | `ssh -i ~/.ssh/imedto-deploy.pem ec2-user@56.125.254.136` |

> **Atenção:** SG da EC2 (`sg-0555c057e7d4dc46b`) libera SSH `0.0.0.0/0` (necessário pro GitHub Actions runner). Em prod, restringir a ranges específicos.

> **CORS da API (`Cors__AllowedOrigins` em `deploy/docker-compose.yml`):** além de `app.imedto.com`/`imedto.com`/`www.imedto.com`, inclui as origens do **app mobile** (Capacitor): `capacitor://localhost`, `https://localhost`, `http://localhost`, `ionic://localhost`. No device o app usa `CapacitorHttp` (HTTP nativo, isento de CORS) com cookie de sessão no cookie jar nativo; as origens acima cobrem WebView/preview no browser. O cookie de auth já é `SameSite=None; Secure` em produção (`AuthController`), pré-requisito para sessão cross-site. App em [`mobile/`](../mobile/).

## Database (Postgres 17 — container na própria EC2)

> **2026-05-30 — saída do RDS.** Para reduzir custo no estágio de teste/baixo volume, o banco
> deixou de ser RDS gerenciado e passou a rodar como container `imedto-postgres` na própria EC2
> (Docker Compose). Economia de ~US$ 27/mês (compute RDS) + ~US$ 3/mês (storage). O RDS `imedto-dev`
> foi deletado após snapshot final `imedto-dev-final-pre-ec2-postgres-20260531` (restaurável).
> **Trade-off aceito:** sem backup gerenciado/Multi-AZ. Quando voltar a prod com volume real,
> reavaliar retorno ao RDS (restaurar do snapshot ou `pg_dump` do container → RDS novo).

| Item | Valor |
|---|---|
| Container | `imedto-postgres` — `postgres:17-alpine`, rede interna Docker `imedto` |
| Conexão (backend) | `Host=postgres;Port=5432;Database=imedto;Username=imedto;SSL Mode=Disable` (rede interna, sem TLS) |
| Volume de dados | `pgdata` (Docker named volume, persiste entre deploys) |
| Senha | `DB_PASSWORD` do SSM `/imedto/dev/db-password` (volume inicializado com ela) |
| Tuning t3.micro | `shared_buffers=96MB`, `max_connections=50`, `effective_cache_size=256MB` |
| Extensions | `pg_trgm`, `unaccent`, `btree_gist`, `pgcrypto`, `citext` (vieram no restore do dump) |
| Backups | ⚠️ Nenhum automático. Fazer `pg_dump` manual periódico se os dados de teste importarem. |

**Acesso ao banco** (psql direto na EC2):
```bash
ssh -i ~/.ssh/imedto-deploy.pem ec2-user@56.125.254.136
docker exec -it imedto-postgres psql -U imedto -d imedto
```

**Backup manual** (dump para o laptop):
```bash
ssh -i ~/.ssh/imedto-deploy.pem ec2-user@56.125.254.136 \
    'docker exec imedto-postgres pg_dump -U imedto -d imedto --no-owner --no-privileges' > backup-$(date +%F).sql
```

## Storage (S3 — 2 buckets privados)

| Bucket | Uso | Política |
|---|---|---|
| `imedto-fotos-155684258219` | Fotos de profissional/estabelecimento | Privado, AES256, presigned URL TTL 24h |
| `imedto-anexos-155684258219` | Anexos de prontuário (LGPD sensível) | Privado, AES256, presigned URL TTL 5 min, Glacier após 90d |

**PDFs assinados digitalmente** (feature Assinatura Digital — briefing 2026-06-01_001) são armazenados **no mesmo bucket `imedto-anexos`**, prefixo `receitas-assinadas/`. Mesma política de acesso (presigned URL TTL 5 min). Dado de saúde Art. 11 LGPD — mesma classificação dos anexos de prontuário.

Backend acessa via `IFotoStorageService` / `IAnexoStorageService` (`Domain.Common` / `Domain.Prontuarios`) → implementações `S3FotoStorageService` / `S3AnexoStorageService`. PDFs assinados são salvos diretamente pelo `BirdIdAssinaturaProvider` via `S3AnexoStorageService`. Credenciais via IAM role da EC2.

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
| `assinatura/birdid-client-id` | String | client_id da aplicação registrada no BirdID (Soluti) |
| `assinatura/birdid-client-secret` | SecureString | client_secret do BirdID |
| `assinatura/birdid-webhook-secret` | SecureString | Chave HMAC usada para validar assinatura dos callbacks BirdID |

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
ssh -i ~/.ssh/imedto-deploy.pem ec2-user@56.125.254.136 'cd ~/imedto && ./scripts/pull-secrets.sh && docker compose up -d --force-recreate backend'
```

> ⚠️ **Use `up -d --force-recreate`, não `restart`, ao trocar um segredo/variável no SSM.** O `docker compose restart` reinicia o mesmo container e **não relê o `.env`** (env vars são resolvidas na criação do container). Só `up -d --force-recreate` (ou `down && up`) recria o container carregando o `.env` regenerado pelo `pull-secrets.sh`. Vale para qualquer rotação de secret: `resend/api-key`, `bcrypt/pepper`, `jwt/*`, `db-password` etc.

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
**Em push pra `main`**: roda os 2 testes + `build-push` (Docker → ghcr.io) **em paralelo** → `migrate` (gera SQL idempotente + aplica via SSH no container `imedto-postgres` da EC2) → `deploy` (rsync + docker compose pull/up via SSH na EC2) → `smoke` (`curl /health`).

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

## Área Admin Global — Schema e Bootstrap

### Tabelas criadas (migration `20260530034709` — Wave 1)

Todas globais — sem `estabelecimento_id`. Prefixo `imedto_` para distinguir das tabelas legado.

| Tabela | Propósito |
|---|---|
| `imedto_admins` | Admins globais do SaaS (separados de `usuarios`). Email via `citext`. UUID PK. |
| `imedto_admin_refresh_tokens` | Refresh tokens admin. Armazena SHA-256 hash do token, nunca o token em claro. |
| `imedto_admin_audit_log` | Audit append-only. Política de retenção por ação definida em `AuditLogRetencao` (Domain.Admin) — ver seção abaixo. `tenant_afetado_id` é bigint nullable sem FK física (log permanece se tenant for excluído). **Wave 7 (2026-05-30)**: cleanup retroativo removeu 158 linhas de ruído (`LOGIN_OK` 131 + `ABRIR_DETALHE_TENANT` 16 + `LOGOUT` 11 = 78% do volume). Essas 3 ações foram cortadas no código e não geram novas linhas. Retenção contínua via job `limpar-audit-admin` (1×/dia). |
| `imedto_planos` | Catálogo de planos admin. **Diferente de `planos` (bigint, domínio cliente legado)**. UUID PK, preço em centavos inteiros nullable. |
| `imedto_assinaturas` | Histórico imutável de assinaturas. **Diferente de `assinaturas` (bigint 1:1)**. INSERT nova linha ao trocar plano, nunca UPDATE. |
| `imedto_config` | Key-value global. PK é a chave text. Valor JSONB. Colunas `tipo` e `secao` adicionadas na Wave 2. |

### Tabelas criadas (migration `20260530131404` — Wave 2: catálogos globais) — REMOVIDAS EM WAVE 4

> **Wave 4 (2026-05-30 — briefing 2026-05-30_004)**: as 3 tabelas paralelas abaixo foram **removidas**
> via migration `20260530200000_drop_catalogos_globais_wave2.sql`. O modelo de catálogos globais
> mudou de "cópia por importação" para **live-link nativo** via `EhPadraoSistema=true` nas tabelas
> legado (`modelo_de_prontuario`, `prontuario_variaveis_pool`). `regioes_anatomicas_catalogo` já era
> global por construção. Admin opera diretamente nessas tabelas. Zero duplicação de schema.

| Tabela | Status |
|---|---|
| `imedto_modelo_prontuario_global` | **REMOVIDA** em Wave 4. Admin usa `modelo_de_prontuario` com `EhPadraoSistema=true`. |
| `imedto_variavel_pool_global` | **REMOVIDA** em Wave 4. Admin usa `prontuario_variaveis_pool` com `EhPadraoSistema=true`. |
| `imedto_regiao_anatomica_global` | **REMOVIDA** em Wave 4. Admin usa `regioes_anatomicas_catalogo` (já global por construção). |

### Catálogos globais — modelo atual (Wave 4 em diante)

Live-link nativo: admin global edita as tabelas legado com `EhPadraoSistema=true` + `EstabelecimentoId=NULL`. A mudança reflete em todos os tenants no próximo refresh, sem fluxo de importação.

| Tabela | Colunas-chave admin | Query tenant |
|---|---|---|
| `modelo_de_prontuario` | `eh_padrao_sistema=true`, `estabelecimento_id=NULL` | `WHERE (eh_padrao_sistema=true OR estabelecimento_id=@X) AND ativo=true AND deletado_em IS NULL` |
| `prontuario_variaveis_pool` | `eh_padrao_sistema=true`, `estabelecimento_id=NULL`, `tipo` (enum 8 categorias) | `WHERE (eh_padrao_sistema=true OR estabelecimento_id=@X) AND ativo=true` |
| `regioes_anatomicas_catalogo` | global por construção (sem `estabelecimento_id`) — hierárquico (`codigo/pai_codigo/nivel/vista`) | `WHERE ativo=true` |

### Decisão sobre regiões anatômicas (Wave 4)

`regioes_anatomicas_catalogo` — tabela hierárquica com 144 registros ativos (codigo, pai_codigo, nivel, vista, template_texto, svg_coords, ordem, lateralidade, ativo). Serve o exame físico interativo e o CRUD admin de regiões. Global por construção — sem `estabelecimento_id`. Admin global edita diretamente via endpoints `/api/admin/catalogos/regioes-anatomicas`.

### Configs default em `imedto_config` (Wave 2 — seeds)

8 chaves seedadas via `20260530131406_seed_catalogos_globais_wave2.sql`:

| Chave | Tipo | Seção | Valor default |
|---|---|---|---|
| `trial.dias_padrao` | numerico | Trial | `14` |
| `trial.limite_profissionais` | numerico | Trial | `5` |
| `assinatura.dias_aviso_expiracao` | numerico | Assinatura | `7` |
| `sistema.email_suporte` | email | Sistema | `"suporte@imedto.com.br"` |
| `feature_flags.exemplo` | toggle | Feature Flags | `false` |
| `comunicacao.smtp_remetente` | email | Comunicação | `"noreply@imedto.com.br"` |
| `comunicacao.from_padrao` | texto | Comunicação | `"Imedto"` |
| `seguranca.tempo_sessao_admin_min` | numerico | Segurança | `15` |

Valores armazenados como JSONB (números sem aspas, strings com aspas, booleans sem aspas). ON CONFLICT DO NOTHING.

### Seed de plano "Gratuidade Vitalícia"

ID fixo: `00000000-0000-0000-0000-000000000001`. Inserido em todos os ambientes via `20260530034800_seed_admin_dev_e_plano_gratuidade.sql`. Idempotente (ON CONFLICT DO NOTHING).

### Bootstrap de admin

**Development**: seed automático via `app.environment = 'Development'` setado no startup da API. Insere `admin@imedto.com / 123123`. Migration: `20260530034800_seed_admin_dev_e_plano_gratuidade.sql`.

**Produção**: comando CLI a ser implementado pelo `imedto-developer`:
```bash
dotnet run --project backend/src/Services/Imedto.Backend.API -- seed-admin --email contato.imedto@gmail.com
# Imprime: Senha temporária: <random 20 chars>
# Insere admin com force_password_reset = true
# Gera linha CRIAR_ADMIN em imedto_admin_audit_log
```

### Hash de senha admin

Mesmo algoritmo do `BcryptPasswordHasher` (usuários comuns):
1. `HMAC-SHA256(pepper_bytes, senha_bytes)` → base64
2. `BCrypt.HashPassword(peppered, workFactor=12)`

Pepper vem de `Auth:Bcrypt:Pepper` (dev: `appsettings.Development.json`; prod: SSM `/imedto/dev/bcrypt/pepper`).

### Extensões Postgres relevantes para admin

`pg_trgm` já instalado e ativo. Usado nos índices GIN trigram em `estabelecimentos.nome_fantasia` (busca ILIKE na lista admin). Arquivo de índices CONCURRENTLY: `20260530034900_indices_admin_concurrently.sql`.

### Extensões e padrão de busca textual — prontuário/documentos

`unaccent` e `pg_trgm` habilitadas desde `20260509032304_InitialCreate`. A função wrapper `public.imutable_unaccent(text)` (declarada IMMUTABLE sobre `unaccent('public.unaccent', $1)`) também foi criada naquela migration e é requisito para índices expressionais GIN.

**Padrão `unaccent(coluna) ILIKE unaccent('%' || @Busca || '%')`** — busca insensível a acento e caixa — usado no endpoint `GET /api/pacientes/{id}/documentos` (briefing 2026-06-09_009+addendum) sobre os campos:
- `receita_itens.medicamento` (via `EXISTS` da sub-consulta de receitas)
- `atestados.tipo` e `atestados.conteudo` (`OR`)
- `pedidos_exame.indicacao_clinica` e `pedidos_exame.exames::text` (cast jsonb→text; `OR`)

**Decisão de índice GIN/trigram nestas colunas: não criar.** Justificativa: a busca textual roda sempre escopo `WHERE paciente_id = $1 AND estabelecimento_id = $2`, que já é um subconjunto de dezenas de registros por paciente (não a tabela inteira). Os índices existentes `ix_atestados_paciente_criado`, `ix_pedidos_exame_paciente_criado` e `ix_receitas_paciente_emitida` reduzem o conjunto antes do ILIKE — o seqscan residual é insignificante. Índice GIN trigram sobre essas colunas não melhoraria o plano de execução neste acesso por paciente. Reavaliar se o volume de documentos por paciente superar ~500 registros ou se o endpoint aparecer em relatório de queries lentas.

### FKs e regras de deleção

| FK | Regra | Justificativa |
|---|---|---|
| `refresh_tokens.admin_id` → `imedto_admins` | CASCADE | Tokens sem admin são inválidos |
| `audit_log.admin_id` → `imedto_admins` | SET NULL | Log histórico permanece |
| `assinaturas.estabelecimento_id` → `estabelecimentos` | RESTRICT | Não perder histórico financeiro |
| `assinaturas.plano_id` → `imedto_planos` | RESTRICT | Não deletar plano em uso |
| demais FK de admin_id | SET NULL | Audit e integridade sem cascata destrutiva |

---

## Decisões já tomadas (não reabrir sem motivo forte)

1. **Free Tier first**: 1 EC2 (com Postgres co-residente em container) + S3, tudo em sa-east-1. RDS removido em 2026-05-30 por custo (ver seção Database). Próximo upgrade: voltar ao RDS gerenciado (Multi-AZ) + ECS Fargate quando o volume justificar.
2. **Auth local** (`LocalJwtAuthService` + ECDSA P-256). Sem Cognito (lock-in maior). Refresh tokens persistidos com SHA-256 hash em `auth_refresh_tokens`.
3. **Storage S3 sempre privado**, acesso via presigned URL. Sem CloudFront em dev (não é Free Tier).
4. **Caddy + Let's Encrypt** em vez de ALB + ACM (Caddy renova automático, mantém volume `caddy-data` persistente).
5. **Resend default**, SES opcional via flag. Quando SES sair do sandbox, vira default (4× mais barato).
6. **Migration EF é a única fonte de schema**. Não criar `.sql` solto fora de `db/migrations/`.
7. **Cooldown 5 min** em reenvio de e-mail (`auth_email_tokens` consulta `criado_em`) — anti-spam idempotente.
8. **Anti-enumeração**: forgot-password e reenviar-confirmacao **sempre** retornam 204 (nunca revelam se e-mail existe).
9. **Sem RLS no Postgres**. Defense-in-depth fica no backend (filter multi-tenant + `[RequiresPapel]` + mensagens genéricas).
10. **Custo mensal estimado**: durante o Free Tier, ~US$ 4 (1 Elastic IP + Route 53). Após o Free Tier expirar, ~US$ 18 (EC2 t3.micro + EBS + IP + Route 53) — o RDS (~US$ 27) foi eliminado ao mover o Postgres pra dentro da EC2.

## Pra cleanup ou recriar tudo

Comandos completos em [`infra/aws-resources.md`](../infra/aws-resources.md). Resumo:
- **Não derrubar a EC2** sem aviso — o banco agora vive nela (volume `pgdata`); perder a EC2 = perder os dados de teste. Fazer `pg_dump` antes.
- Pra voltar ao RDS gerenciado: criar RDS novo (ou restaurar o snapshot `imedto-dev-final-pre-ec2-postgres-20260531`), `pg_dump` do container → restore no RDS, apontar `ConnectionStrings__Default` de volta pra `${DB_HOST}` (SSM) no `deploy/docker-compose.yml` e remover o serviço `postgres`.

## Onde ficam os secrets

Todos gitignored (ver `.gitignore`):
- `.mcp.json` — configuração de MCP servers locais (vazia por padrão; templates em `.mcp.json.example`).
- `backend/src/Services/Imedto.Backend.API/appsettings.Development.json` — connection strings (Default + Migrations), chaves PEM do JWT (`Auth:Jwt:PrivateKeyPem`/`PublicKeyPem`), pepper do BCrypt (`Auth:Bcrypt:Pepper`), API key do Resend, buckets S3.
- `frontend/.env` — apenas `VITE_API_BASE_URL` opcional (frontend é BFF puro, sem segredos).

Templates em `.mcp.json.example` e `frontend/.env.example`. Em produção, todos os valores vêm do AWS SSM Parameter Store.
