# Recursos AWS — Imedto (ambiente `dev`/teste)

> **Princípio de teste:** 1 EC2 + 1 RDS + 2 buckets S3, **tudo no Free Tier**.
> Quando passar pra produção, tudo isso vira Terraform/CDK e vamos adicionar Multi-AZ, ALB, CloudFront, Aurora etc.

Atualizado em: 2026-05-09

---

## Conta

| Item | Valor |
|---|---|
| Account ID | `155684258219` |
| Região default | `sa-east-1` (São Paulo) |
| IAM user pessoal | `joao-admin` (AdministratorAccess) |
| IAM user legado | `deploy-bot` (não usar — policies erradas) |

---

## Rede

| Recurso | ID |
|---|---|
| VPC default | `vpc-0fdc907d8872f148a` |
| Subnet `sa-east-1a` | `subnet-087dd1619090dac4c` |
| Subnet `sa-east-1b` | `subnet-0bcd539ef613fde1c` |
| Subnet `sa-east-1c` | `subnet-09bbdef60721ee9bd` |

### Security Groups

| Nome | ID | Ingress |
|---|---|---|
| `imedto-ec2-sg` | `sg-0555c057e7d4dc46b` | 22/tcp de `177.55.224.232/32` (laptop) · 80/tcp e 443/tcp de `0.0.0.0/0` |
| `imedto-rds-sg` | `sg-07bfdefa2ffbf14ec` | 5432/tcp **só** do `imedto-ec2-sg` |

---

## Compute (EC2)

| Item | Valor |
|---|---|
| Instance ID | `i-0c4f75999438cc5c2` |
| Tipo | `t3.micro` (2 vCPU, 1 GB RAM) |
| AMI | `ami-093542ba1f132881b` (Amazon Linux 2023, x86_64) |
| EBS | 8 GB gp3 |
| Key pair | `imedto-deploy` (`~/.ssh/imedto-deploy.pem` no laptop) |
| IAM role | `imedto-ec2-role` (instance profile `imedto-ec2-profile`) |
| **Elastic IP** | **`56.125.254.136`** (alloc `eipalloc-0a976f5551c1794ae`) |
| Bootstrap | Docker 25 + Compose v2 + psql 15 (via `user-data`) |
| Acesso | `ssh -i ~/.ssh/imedto-deploy.pem ec2-user@56.125.254.136` |

---

## Database (RDS)

| Item | Valor |
|---|---|
| DB identifier | `imedto-dev` |
| Engine | Postgres 17.2 |
| Tipo | `db.t4g.micro` Single-AZ |
| Storage | 20 GB gp3 (encriptado) |
| Backups | **0 dias** (Free Tier "new plan" não permite >0; ativar em prod) |
| Endpoint | `imedto-dev.cx0648wywxg8.sa-east-1.rds.amazonaws.com:5432` |
| DB master user | `imedto` |
| Master password | `/imedto/dev/db-password` (SSM SecureString) |
| Database de aplicação | `imedto` |
| Extensions instaladas | `pg_trgm`, `unaccent`, `btree_gist`, `pgcrypto`, `citext` |
| Subnet group | `imedto-subnets` |
| Parameter group | `imedto-pg17` (custom — ver tabela abaixo) |

**Parâmetros customizados em `imedto-pg17`:**

| Param | Valor | Aplicação | Para que serve |
|---|---|---|---|
| `shared_preload_libraries` | `pg_stat_statements` | pending-reboot | Habilita view `pg_stat_statements` (top queries por tempo agregado) |
| `log_min_duration_statement` | `500` | immediate | Loga toda query >500ms no CloudWatch (`/aws/rds/instance/imedto-dev/postgresql`) |
| `log_lock_waits` | `1` | immediate | Loga lock waits acima de `deadlock_timeout` (default 1s) — detecta contenção |
| `log_temp_files` | `0` | immediate | Loga uso de arquivos temporários (queries que estouram `work_mem`) |

**Ler logs de queries lentas:**
```bash
AWS_PROFILE=imedto aws logs tail /aws/rds/instance/imedto-dev/postgresql --since 1h --follow
```

> ⚠️ Postgres só responde **de dentro do `imedto-ec2-sg`** — pra acessar do laptop, túnel via EC2:
> `ssh -i ~/.ssh/imedto-deploy.pem -L 5432:imedto-dev.cx0648wywxg8.sa-east-1.rds.amazonaws.com:5432 ec2-user@56.125.254.136`

---

## Storage (S3)

| Bucket | Uso | Política |
|---|---|---|
| `imedto-fotos-155684258219` | Fotos de profissional/estabelecimento | Privado, AES256, sem versionamento |
| `imedto-anexos-155684258219` | Anexos de prontuário (LGPD) | Privado, AES256, sem versionamento |

Ambos com **Block Public Access** ativado em todas as 4 dimensões. Acesso só via **presigned URL** gerada pelo backend.

---

## Segredos (SSM Parameter Store, prefixo `/imedto/dev/`)

| Parâmetro | Tipo | Conteúdo |
|---|---|---|
| `/imedto/dev/db-host` | String | Endpoint do RDS |
| `/imedto/dev/db-password` | SecureString | Senha master do RDS (25 chars) |
| `/imedto/dev/jwt/private-key` | SecureString | EC P-256 private (PEM) |
| `/imedto/dev/jwt/public-key` | SecureString | EC P-256 public (PEM) |
| `/imedto/dev/jwt/issuer` | String | `imedto-backend` |
| `/imedto/dev/jwt/audience` | String | `imedto-app` |
| `/imedto/dev/bcrypt/pepper` | SecureString | Salt secreto pra hash de senha |
| `/imedto/dev/email/from` | String | `noreply@imedto.com` |
| `/imedto/dev/s3/bucket-fotos` | String | `imedto-fotos-155684258219` |
| `/imedto/dev/s3/bucket-anexos` | String | `imedto-anexos-155684258219` |
| `/imedto/dev/aws/region` | String | `sa-east-1` |
| `/imedto/dev/resend/api-key` | SecureString | **TODO** — adicionar token do Resend |
| `/imedto/dev/ghcr-token` | SecureString | **TODO** — Personal Access Token do GitHub (escopo `read:packages`) |

---

## DNS (Route 53 — `imedto.com`)

| Tipo | Nome | Valor | Uso |
|---|---|---|---|
| NS | `imedto.com.` | (4 nameservers AWS) | DNS oficial |
| SOA | `imedto.com.` | (auto) | DNS oficial |
| **A** | `imedto.com.` | `56.125.254.136` | Aponta pra EC2 nova |
| **A** | `www.imedto.com.` | `56.125.254.136` | Aponta pra EC2 nova |
| **A** | `app.imedto.com.` | `56.125.254.136` | Domínio da app |
| MX | `imedto.com.` | secureserver.net | E-mail GoDaddy (recebimento) |
| TXT (SPF) | `imedto.com.` | `v=spf1 include:secureserver.net include:resend.com ...` | Receber/enviar |
| TXT (DMARC) | `_dmarc.imedto.com.` | `v=DMARC1; p=none;` | DMARC monitoring |
| TXT (DKIM) | `resend._domainkey.imedto.com.` | `p=MIGfMA0GCSqGSIb...` | Resend assina e-mails |
| CNAME (DKIM) | `secureserver1._domainkey.imedto.com.` | s1.dkim.imedto_com... | GoDaddy assina |
| CNAME (DKIM) | `secureserver2._domainkey.imedto.com.` | s2.dkim.imedto_com... | GoDaddy assina |
| SRV | `_autodiscover._tcp.imedto.com.` | autodiscover.secureserver.net | E-mail GoDaddy autodiscover |
| CNAME | `email.imedto.com.` | email.secureserver.net | E-mail GoDaddy |
| TXT | `imedto.com.` | `T2365160` | Tag GoDaddy |

> Removidos no cleanup: `send.imedto.com.` MX/TXT (eram do SES, não usamos mais).

---

## ACM (Certificate Manager)

| Item | Valor |
|---|---|
| ID | `c8608181-81e6-493a-8da6-aae9c9d71774` |
| Domínio | `imedto.com` |
| Status | Issued (não em uso) |
| Uso futuro | Quando colocar CloudFront/ALB na frente da EC2 |

> Em dev usamos **Caddy + Let's Encrypt** direto na EC2 (gera o próprio cert).

---

## E-mail transacional

**Resend** (não SES) — DKIM já configurado no DNS desde o início.
- Free tier: 3.000 e-mails/mês, 100/dia
- API: `POST https://api.resend.com/emails` com Bearer token
- Token salvo em `/imedto/dev/resend/api-key` (a popular)
- Identidade do remetente: `noreply@imedto.com`

---

## Limpeza realizada (legado)

- ❌ EC2 `i-0f40310273c84d83b` (terminada)
- ❌ Elastic IP `18.228.75.165` (liberado)
- ❌ Key pair `imedto-app-pem` (deletado)
- ❌ SG `launch-wizard-1` (deletado)
- ❌ SES domain identity `imedto.com` (deletada)
- ❌ DNS `send.imedto.com.` MX/TXT (deletados)

---

## Comandos de "matar tudo" (cleanup futuro se precisar)

Ver seção 12 do [PLANO_SETUP_AWS_PASSO_A_PASSO.md](../Docs/PLANO_SETUP_AWS_PASSO_A_PASSO.md).

---

## Custo estimado (Free Tier — 12 primeiros meses)

| Recurso | Custo |
|---|---|
| EC2 t3.micro 24×7 (≤750h) | US$ 0 |
| RDS db.t4g.micro 24×7 (≤750h) | US$ 0 |
| RDS storage 20 GB | US$ 0 |
| EBS 8 GB | US$ 0 |
| Elastic IP attached | US$ 0 |
| S3 (≤5 GB cada) | US$ 0 |
| SSM Parameter Store standard | US$ 0 |
| Data transfer out (≤100 GB/mês) | US$ 0 |
| **Route 53 hosted zone** | **US$ 0,50/mês** (não é Free Tier — único custo fixo) |
| ACM cert | US$ 0 |
| **TOTAL** | **~US$ 0,50/mês** |
