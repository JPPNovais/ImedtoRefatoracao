---
titulo: Setup AWS passo a passo (Free Tier, zero conhecimento prévio)
status: ativo
criado_em: 2026-05-04
escopo: Guia "do zero ao deploy" pra alguém que nunca configurou AWS. Cobre conta, IAM, VPC, Security Groups, EC2, RDS, S3, SES, SSM Parameter Store, IAM roles, Cloudflare DNS e billing alarms. Custo final mensal: US$ 0.
companhia: PLANO_MIGRACAO_AWS_GREENFIELD.md (decisões), PLANO_CICD_GITHUB_ACTIONS.md (deploy automatizado), PLANO_DOMINIO_SSL_EMAIL.md (HTTPS + e-mail).
---

# Setup AWS passo a passo — Free Tier, zero conhecimento

## 0. Antes de começar

### 0.1 O que você precisa em mãos

- Cartão de crédito internacional (a AWS faz pré-autorização de US$ 1 — não cobra).
- Documento com foto (CNH, RG, passaporte) — pode ser pedido na verificação.
- Telefone celular com SMS habilitado.
- E-mail dedicado pra conta AWS (sugestão: `aws@<seu-dominio>` ou Gmail novo).
- Domínio próprio (compra na Registro.br ~R$ 40/ano ou Cloudflare/Namecheap).

### 0.2 Convenções deste guia

- Tudo em **`sa-east-1` (São Paulo)** — latência menor pra usuários BR e atende LGPD (residência de dados).
- Quando você ver `<placeholder>`, substitua pelo seu valor real.
- Linhas começando com `aws ...` são comandos do AWS CLI rodando do **seu laptop**.
- Linhas começando com `[ec2]$` rodam **dentro da EC2**.
- Linhas começando com `[psql]>` rodam **dentro do `psql`**.

### 0.3 Custo zero — princípio

Em **toda** tela da AWS, antes de clicar em "Create", verifique se o tier escolhido é **"Free Tier eligible"** (a AWS marca explicitamente). Se o componente não tiver free tier (NAT Gateway, ALB, Aurora, RDS Proxy, ElastiCache), **não crie agora** — está marcado como "produção" no [PLANO_MIGRACAO_AWS_GREENFIELD.md](PLANO_MIGRACAO_AWS_GREENFIELD.md).

---

## 1. Criar conta AWS (15 min)

### 1.1 Cadastro

1. Acessar https://signup.aws.amazon.com/signup
2. **Tipo de conta**: "Personal" (mesmo sendo CNPJ — pra Free Tier não faz diferença, vc pode trocar pra Business depois).
3. E-mail e senha.
4. Endereço de cobrança em PT-BR é OK.
5. Cartão de crédito.
6. Verificação por SMS.
7. **Plano de suporte**: escolher **"Basic Support — Free"**.

A conta fica em "ativando" por até 24h. Geralmente ~5 min.

### 1.2 Habilitar MFA na conta root **HOJE**

1. Login com o e-mail/senha da raiz.
2. Canto superior direito → seu nome → **"Security credentials"**.
3. Em **"Multi-factor authentication (MFA)"** → **"Assign MFA device"**.
4. Tipo: **"Authenticator app"** (Google Authenticator / 1Password / Authy).
5. Escanear QR code, digitar 2 códigos consecutivos do app.

**Por quê:** sem MFA na root, qualquer vazamento de senha = conta perdida + cobrança ilimitada. **Não pule isso.**

### 1.3 Configurar billing alarm — barreira de segurança

A AWS não bloqueia automaticamente cobrança quando você sai do Free Tier. Precisa criar alarme manual.

1. Console → **"Billing and Cost Management"** (na barra de busca).
2. **"Billing preferences"** → marcar:
   - ✅ Receive Free Tier usage alerts
   - ✅ Receive billing alerts
3. **"Budgets"** → **"Create budget"**:
   - Type: **"Cost budget"**
   - Period: **Monthly**
   - Budget amount: **US$ 1.00**
   - Alert threshold: **80%** e **100%**
   - E-mail: o seu
4. Salvar.

Resultado: qualquer cobrança que passe US$ 0,80 dispara e-mail. Você fica sabendo antes de virar problema.

### 1.4 Mudar região default pra `sa-east-1`

Canto superior direito do console → dropdown da região → escolher **"South America (São Paulo) sa-east-1"**.

Sempre confira que está em `sa-east-1` antes de criar qualquer recurso. Recurso criado na região errada **não comunica** com recurso na região certa sem custo extra.

---

## 2. Criar usuário IAM e parar de usar a root (15 min)

**Regra de ouro:** depois deste passo, você **só usa a root pra coisas de billing**. Tudo o mais é com IAM user.

### 2.1 Criar IAM user

1. Console → **"IAM"**.
2. **"Users"** → **"Create user"**.
3. Nome: `joao-admin` (ou similar).
4. ✅ **"Provide user access to the AWS Management Console"**.
5. **"I want to create an IAM user"** (não Identity Center pra simplificar).
6. Senha: gerar forte, salvar no gerenciador (1Password / Bitwarden).
7. ✅ **"Users must create a new password at next sign-in"** — **desmarcar** (pra você mesmo).
8. **Permissions**: anexar a policy **`AdministratorAccess`** (em teste é OK; em prod usar least-privilege).
9. Criar.

### 2.2 Anotar o link de login customizado

A página final mostra:
```
https://<account-id>.signin.aws.amazon.com/console
```

**Salvar esse link** — é por aqui que você vai logar daqui pra frente. Logout da root, login pelo IAM user.

### 2.3 Habilitar MFA no IAM user também

Mesmo procedimento da seção 1.2, mas logado como IAM user.

### 2.4 Criar access keys pro AWS CLI

1. IAM → Users → `joao-admin` → **"Security credentials"**.
2. **"Create access key"** → use case: **"Command Line Interface (CLI)"**.
3. ✅ "I understand the recommendation".
4. **Salvar** `Access key ID` e `Secret access key` no gerenciador de senhas. **Não vai aparecer de novo.**

### 2.5 Instalar AWS CLI no laptop

```bash
# macOS
brew install awscli

# Ubuntu/WSL
sudo apt install awscli  # ou versão mais nova via pip

# Verificar
aws --version
```

### 2.6 Configurar perfil

```bash
aws configure --profile imedto
# AWS Access Key ID:     <cole>
# AWS Secret Access Key: <cole>
# Default region name:   sa-east-1
# Default output format: json
```

Adicionar ao `~/.zshrc` ou `~/.bashrc`:
```bash
export AWS_PROFILE=imedto
```

Reabrir terminal e testar:
```bash
aws sts get-caller-identity
# Esperado: { "Account": "...", "UserId": "...", "Arn": "arn:aws:iam::.../joao-admin" }
```

Se voltar isso, CLI funcional. ✅

---

## 3. Preparar VPC e Security Groups (10 min)

A **VPC default** que vem com toda conta nova já basta pra Free Tier. Vamos só criar Security Groups (firewalls).

### 3.1 Identificar VPC default

```bash
aws ec2 describe-vpcs --filters Name=is-default,Values=true \
    --query "Vpcs[0].VpcId" --output text
```

Anotar o ID retornado (ex: `vpc-0abc123...`). Vamos chamar de `<VPC_ID>`.

### 3.2 Security Group da EC2 (`imedto-ec2-sg`)

```bash
SG_EC2=$(aws ec2 create-security-group \
    --group-name imedto-ec2-sg \
    --description "EC2 Imedto: SSH/HTTP/HTTPS" \
    --vpc-id <VPC_ID> \
    --query GroupId --output text)
echo "SG_EC2=$SG_EC2"
```

Adicionar regras (descobrir seu IP público com `curl ifconfig.me`):

```bash
MEU_IP=$(curl -s ifconfig.me)/32

# SSH só do seu IP
aws ec2 authorize-security-group-ingress --group-id $SG_EC2 \
    --protocol tcp --port 22 --cidr $MEU_IP

# HTTP/HTTPS de qualquer lugar (Cloudflare vai ser o frontdoor)
aws ec2 authorize-security-group-ingress --group-id $SG_EC2 \
    --protocol tcp --port 80 --cidr 0.0.0.0/0
aws ec2 authorize-security-group-ingress --group-id $SG_EC2 \
    --protocol tcp --port 443 --cidr 0.0.0.0/0
```

### 3.3 Security Group do RDS (`imedto-rds-sg`)

```bash
SG_RDS=$(aws ec2 create-security-group \
    --group-name imedto-rds-sg \
    --description "RDS Imedto: Postgres so do EC2" \
    --vpc-id <VPC_ID> \
    --query GroupId --output text)
echo "SG_RDS=$SG_RDS"

# Postgres 5432 SÓ do Security Group da EC2 (não do mundo!)
aws ec2 authorize-security-group-ingress --group-id $SG_RDS \
    --protocol tcp --port 5432 --source-group $SG_EC2
```

> ⚠️ **NUNCA** abra 5432 pro 0.0.0.0/0. Banco exposto = dia ruim.

---

## 4. Criar EC2 (20 min)

### 4.1 Criar key pair (chave SSH)

```bash
aws ec2 create-key-pair --key-name imedto-deploy \
    --query KeyMaterial --output text > ~/.ssh/imedto-deploy.pem
chmod 400 ~/.ssh/imedto-deploy.pem
```

### 4.2 Encontrar AMI Amazon Linux 2023 mais recente (free tier)

```bash
AMI_ID=$(aws ec2 describe-images \
    --owners amazon \
    --filters "Name=name,Values=al2023-ami-2023.*-arm64" \
              "Name=state,Values=available" \
    --query "sort_by(Images, &CreationDate)[-1].ImageId" \
    --output text)
echo "AMI=$AMI_ID"
```

(ARM64 = Graviton, free tier inclui `t4g.micro` e `t3.micro`. Aqui vamos com `t3.micro` x86 pra compatibilidade fácil com imagens Docker amd64. Se preferir Graviton, troca pra `t4g.micro` + AMI arm64 + imagens Docker `linux/arm64`.)

Pra `t3.micro` (x86_64):
```bash
AMI_ID=$(aws ec2 describe-images \
    --owners amazon \
    --filters "Name=name,Values=al2023-ami-2023.*-x86_64" \
              "Name=state,Values=available" \
    --query "sort_by(Images, &CreationDate)[-1].ImageId" \
    --output text)
echo "AMI=$AMI_ID"
```

### 4.3 Lançar a instância

```bash
EC2_ID=$(aws ec2 run-instances \
    --image-id $AMI_ID \
    --instance-type t3.micro \
    --key-name imedto-deploy \
    --security-group-ids $SG_EC2 \
    --block-device-mappings '[{"DeviceName":"/dev/xvda","Ebs":{"VolumeSize":8,"VolumeType":"gp3"}}]' \
    --tag-specifications 'ResourceType=instance,Tags=[{Key=Name,Value=imedto-app}]' \
    --query "Instances[0].InstanceId" --output text)
echo "EC2_ID=$EC2_ID"
```

Esperar até `running`:
```bash
aws ec2 wait instance-running --instance-ids $EC2_ID
```

### 4.4 Alocar e attachar Elastic IP

> Sem Elastic IP, o IP da EC2 muda a cada restart, quebrando o DNS. **Sempre attached ele é grátis**; **não-attached** custa US$ 3,60/mês.

```bash
EIP_ALLOC=$(aws ec2 allocate-address --domain vpc --query AllocationId --output text)
aws ec2 associate-address --instance-id $EC2_ID --allocation-id $EIP_ALLOC

ELASTIC_IP=$(aws ec2 describe-addresses --allocation-ids $EIP_ALLOC \
    --query "Addresses[0].PublicIp" --output text)
echo "ELASTIC_IP=$ELASTIC_IP"
```

### 4.5 SSH na instância

```bash
ssh -i ~/.ssh/imedto-deploy.pem ec2-user@$ELASTIC_IP
```

Da primeira vez, aceita a fingerprint. Se não conectar, conferir se o seu IP atual ainda é o `MEU_IP` que foi liberado no SG.

### 4.6 Instalar Docker e Docker Compose no EC2

```bash
[ec2]$ sudo dnf update -y
[ec2]$ sudo dnf install -y docker git
[ec2]$ sudo systemctl enable --now docker
[ec2]$ sudo usermod -aG docker ec2-user

# Docker Compose v2 plugin (padrão moderno)
[ec2]$ DOCKER_CONFIG=${DOCKER_CONFIG:-/usr/local/lib/docker}
[ec2]$ sudo mkdir -p $DOCKER_CONFIG/cli-plugins
[ec2]$ sudo curl -SL https://github.com/docker/compose/releases/latest/download/docker-compose-linux-x86_64 \
       -o $DOCKER_CONFIG/cli-plugins/docker-compose
[ec2]$ sudo chmod +x $DOCKER_CONFIG/cli-plugins/docker-compose

# Sair e voltar pro grupo docker fazer efeito
[ec2]$ exit
ssh -i ~/.ssh/imedto-deploy.pem ec2-user@$ELASTIC_IP
[ec2]$ docker --version
[ec2]$ docker compose version
```

### 4.7 Instalar PostgreSQL client (pra testar conexão com RDS depois)

```bash
[ec2]$ sudo dnf install -y postgresql15
```

---

## 5. Criar RDS Postgres (20 min)

### 5.1 Subnet group (RDS exige subnets em ≥2 AZs, mesmo Single-AZ)

```bash
SUBNETS=$(aws ec2 describe-subnets --filters Name=vpc-id,Values=<VPC_ID> \
    --query "Subnets[].SubnetId" --output text)
echo $SUBNETS

aws rds create-db-subnet-group \
    --db-subnet-group-name imedto-subnets \
    --db-subnet-group-description "Subnets default p/ RDS Imedto" \
    --subnet-ids $SUBNETS
```

### 5.2 Parameter group (extensions)

```bash
aws rds create-db-parameter-group \
    --db-parameter-group-name imedto-pg17 \
    --db-parameter-group-family postgres17 \
    --description "Parameters Imedto"

aws rds modify-db-parameter-group \
    --db-parameter-group-name imedto-pg17 \
    --parameters "ParameterName=shared_preload_libraries,ParameterValue=pg_stat_statements,ApplyMethod=pending-reboot"
```

(As extensions `pg_trgm`, `unaccent`, `btree_gist`, `pgcrypto`, `citext` já vêm permitidas no Postgres da AWS — basta `CREATE EXTENSION` depois. `pg_stat_statements` é diferente: precisa do parameter pra carregar.)

### 5.3 Senha do banco — guardar no SSM **antes** de criar

```bash
DB_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)

aws ssm put-parameter \
    --name /imedto/dev/db-password \
    --value "$DB_PASSWORD" \
    --type SecureString \
    --description "Senha do RDS Postgres dev"
```

### 5.4 Criar instância

```bash
aws rds create-db-instance \
    --db-instance-identifier imedto-dev \
    --db-instance-class db.t4g.micro \
    --engine postgres \
    --engine-version 17.2 \
    --allocated-storage 20 \
    --storage-type gp3 \
    --master-username imedto \
    --master-user-password "$DB_PASSWORD" \
    --vpc-security-group-ids $SG_RDS \
    --db-subnet-group-name imedto-subnets \
    --db-parameter-group-name imedto-pg17 \
    --backup-retention-period 7 \
    --no-multi-az \
    --no-publicly-accessible \
    --storage-encrypted \
    --enable-performance-insights \
    --performance-insights-retention-period 7 \
    --tags Key=Project,Value=Imedto Key=Env,Value=dev
```

Aguardar (10-15 min):
```bash
aws rds wait db-instance-available --db-instance-identifier imedto-dev
```

### 5.5 Pegar endpoint

```bash
RDS_ENDPOINT=$(aws rds describe-db-instances --db-instance-identifier imedto-dev \
    --query "DBInstances[0].Endpoint.Address" --output text)
echo "RDS=$RDS_ENDPOINT"
```

Salvar no SSM:
```bash
aws ssm put-parameter --name /imedto/dev/db-host --value "$RDS_ENDPOINT" --type String
```

### 5.6 Testar conexão (do EC2, não do laptop)

```bash
ssh -i ~/.ssh/imedto-deploy.pem ec2-user@$ELASTIC_IP
[ec2]$ psql -h <RDS_ENDPOINT> -U imedto -d postgres
# senha = $DB_PASSWORD que vc gerou (aws ssm get-parameter --name /imedto/dev/db-password --with-decryption)

[psql]> CREATE DATABASE imedto;
[psql]> \c imedto
[psql]> CREATE EXTENSION IF NOT EXISTS pg_trgm;
[psql]> CREATE EXTENSION IF NOT EXISTS unaccent;
[psql]> CREATE EXTENSION IF NOT EXISTS btree_gist;
[psql]> CREATE EXTENSION IF NOT EXISTS pgcrypto;
[psql]> CREATE EXTENSION IF NOT EXISTS citext;
[psql]> \dx
[psql]> \q
```

5 extensions listadas = ✅.

---

## 6. Criar buckets S3 (5 min)

```bash
# Bucket de fotos (públicas via CloudFront futuro; em dev fica privado)
aws s3api create-bucket \
    --bucket imedto-fotos-dev-<account-id> \
    --region sa-east-1 \
    --create-bucket-configuration LocationConstraint=sa-east-1

# Bucket de anexos (sempre privado)
aws s3api create-bucket \
    --bucket imedto-anexos-dev-<account-id> \
    --region sa-east-1 \
    --create-bucket-configuration LocationConstraint=sa-east-1
```

> ⚠️ Nome de bucket é **global** (todo S3 do mundo). Por isso o sufixo `<account-id>` (substitua pelo ID da sua conta — `aws sts get-caller-identity --query Account`).

### 6.1 Block public access (sempre)

```bash
for BUCKET in imedto-fotos-dev-<account-id> imedto-anexos-dev-<account-id>; do
  aws s3api put-public-access-block --bucket $BUCKET \
    --public-access-block-configuration \
    "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
done
```

Acesso será só via **presigned URL** gerada pelo backend.

### 6.2 Encriptação SSE-S3

```bash
for BUCKET in imedto-fotos-dev-<account-id> imedto-anexos-dev-<account-id>; do
  aws s3api put-bucket-encryption --bucket $BUCKET \
    --server-side-encryption-configuration \
    '{"Rules":[{"ApplyServerSideEncryptionByDefault":{"SSEAlgorithm":"AES256"}}]}'
done
```

### 6.3 Lifecycle do bucket de anexos (Glacier após 90 dias)

`/tmp/lifecycle-anexos.json`:
```json
{
  "Rules": [{
    "ID": "anexos-glacier-90d",
    "Status": "Enabled",
    "Filter": {},
    "Transitions": [{
      "Days": 90,
      "StorageClass": "GLACIER_IR"
    }]
  }]
}
```

```bash
aws s3api put-bucket-lifecycle-configuration \
    --bucket imedto-anexos-dev-<account-id> \
    --lifecycle-configuration file:///tmp/lifecycle-anexos.json
```

---

## 7. Criar IAM role pra EC2 acessar AWS (10 min)

A EC2 vai precisar acessar S3, SSM e SES. Em vez de colocar access keys no servidor (anti-pattern), criamos uma **role** que a instância "veste".

### 7.1 Criar role

`/tmp/trust-ec2.json`:
```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Principal": { "Service": "ec2.amazonaws.com" },
    "Action": "sts:AssumeRole"
  }]
}
```

```bash
aws iam create-role \
    --role-name imedto-ec2-role \
    --assume-role-policy-document file:///tmp/trust-ec2.json
```

### 7.2 Policy customizada

`/tmp/policy-ec2.json` (substitua `<account-id>`):
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "S3Buckets",
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject", "s3:ListBucket"],
      "Resource": [
        "arn:aws:s3:::imedto-fotos-dev-<account-id>",
        "arn:aws:s3:::imedto-fotos-dev-<account-id>/*",
        "arn:aws:s3:::imedto-anexos-dev-<account-id>",
        "arn:aws:s3:::imedto-anexos-dev-<account-id>/*"
      ]
    },
    {
      "Sid": "SsmParams",
      "Effect": "Allow",
      "Action": ["ssm:GetParameter", "ssm:GetParameters", "ssm:GetParametersByPath"],
      "Resource": "arn:aws:ssm:sa-east-1:<account-id>:parameter/imedto/*"
    },
    {
      "Sid": "SesSend",
      "Effect": "Allow",
      "Action": ["ses:SendEmail", "ses:SendRawEmail"],
      "Resource": "*"
    },
    {
      "Sid": "Logs",
      "Effect": "Allow",
      "Action": ["logs:CreateLogStream", "logs:PutLogEvents", "logs:CreateLogGroup"],
      "Resource": "*"
    }
  ]
}
```

```bash
aws iam put-role-policy \
    --role-name imedto-ec2-role \
    --policy-name imedto-ec2-policy \
    --policy-document file:///tmp/policy-ec2.json
```

### 7.3 Criar instance profile e attachar

```bash
aws iam create-instance-profile --instance-profile-name imedto-ec2-profile
aws iam add-role-to-instance-profile \
    --instance-profile-name imedto-ec2-profile \
    --role-name imedto-ec2-role

# Esperar ~10s pro IAM propagar
sleep 10

aws ec2 associate-iam-instance-profile \
    --instance-id $EC2_ID \
    --iam-instance-profile Name=imedto-ec2-profile
```

### 7.4 Validar

```bash
ssh -i ~/.ssh/imedto-deploy.pem ec2-user@$ELASTIC_IP

[ec2]$ aws sts get-caller-identity
# Esperado: ARN com "assumed-role/imedto-ec2-role"

[ec2]$ aws s3 ls s3://imedto-anexos-dev-<account-id>/
[ec2]$ aws ssm get-parameter --name /imedto/dev/db-password --with-decryption
```

---

## 8. SSM Parameter Store — segredos da app (15 min)

Já criamos `/imedto/dev/db-password` e `/imedto/dev/db-host`. Faltam:

### 8.1 JWT signing key (EC P-256)

No laptop:
```bash
# Chave privada
openssl ecparam -name prime256v1 -genkey -noout -out /tmp/jwt-private.pem

# Chave pública correspondente
openssl ec -in /tmp/jwt-private.pem -pubout -out /tmp/jwt-public.pem

# Subir as duas pro SSM
aws ssm put-parameter --name /imedto/dev/jwt/private-key \
    --value "$(cat /tmp/jwt-private.pem)" --type SecureString
aws ssm put-parameter --name /imedto/dev/jwt/public-key \
    --value "$(cat /tmp/jwt-public.pem)" --type SecureString

# Apagar do disco
shred -u /tmp/jwt-private.pem /tmp/jwt-public.pem  # Linux
# ou: rm -P /tmp/jwt-private.pem /tmp/jwt-public.pem  # macOS
```

### 8.2 BCrypt pepper

```bash
PEPPER=$(openssl rand -base64 32)
aws ssm put-parameter --name /imedto/dev/bcrypt/pepper \
    --value "$PEPPER" --type SecureString
```

### 8.3 Outros parâmetros aplicacionais

```bash
aws ssm put-parameter --name /imedto/dev/jwt/issuer --value "imedto-backend" --type String
aws ssm put-parameter --name /imedto/dev/jwt/audience --value "imedto-app" --type String
aws ssm put-parameter --name /imedto/dev/email/from --value "noreply@<seu-dominio>" --type String
aws ssm put-parameter --name /imedto/dev/s3/bucket-fotos --value "imedto-fotos-dev-<account-id>" --type String
aws ssm put-parameter --name /imedto/dev/s3/bucket-anexos --value "imedto-anexos-dev-<account-id>" --type String
```

### 8.4 Listar tudo pra conferir

```bash
aws ssm get-parameters-by-path --path /imedto/dev --recursive \
    --query "Parameters[].[Name,Type]" --output table
```

Esperado: ~10 parâmetros. SecureString pros sensíveis (senhas, keys, pepper), String pros não-sensíveis.

---

## 9. SES — e-mail transacional (30 min, espera DNS)

Para signup/recuperação de senha. **Detalhado em [PLANO_DOMINIO_SSL_EMAIL.md](PLANO_DOMINIO_SSL_EMAIL.md)** — resumo aqui:

1. Comprar/possuir domínio (ex: `imedto.com.br`).
2. Apontar nameservers pro Cloudflare (DNS grátis).
3. Em SES → **"Verified identities"** → **"Create identity"** → Domain → `imedto.com.br`.
4. SES gera 3 registros CNAME (DKIM). Adicionar no Cloudflare como **"DNS only"** (não proxiar).
5. Esperar verificação (~5–30 min).
6. **Pedir Production Access** (form de "Use case description" — diga: "transactional emails for healthcare SaaS, double opt-in"). Aprovação em 24-48h.

Sem Production Access, SES só envia pra e-mails que você verificou um a um — útil só pra testar consigo mesmo.

### 9.1 Verificar e-mails individuais (pra testar antes do production access)

```bash
aws ses verify-email-identity --email-address seu-email-pessoal@gmail.com
# Você recebe e-mail "AWS — please confirm".
```

---

## 10. Anotar todos os recursos criados

Criar `infra/aws-resources.md` no repo (gitignored se tiver dados sensíveis):

```markdown
# Recursos AWS — Imedto dev

| Recurso | Identificador | Notas |
|---|---|---|
| Account ID | 123456789012 | |
| Região | sa-east-1 | |
| VPC | vpc-0abc123... | default |
| SG EC2 | sg-0xxxxx | imedto-ec2-sg |
| SG RDS | sg-0yyyyy | imedto-rds-sg |
| EC2 instance | i-0zzzzz | imedto-app, t3.micro |
| Elastic IP | 18.230.xxx.xxx | attached |
| Key pair | imedto-deploy | ~/.ssh/imedto-deploy.pem |
| RDS endpoint | imedto-dev.xxxxx.sa-east-1.rds.amazonaws.com | Postgres 17 db.t4g.micro |
| RDS subnet group | imedto-subnets | |
| RDS parameter group | imedto-pg17 | |
| S3 fotos | imedto-fotos-dev-123456789012 | privado, SSE-S3 |
| S3 anexos | imedto-anexos-dev-123456789012 | privado, SSE-S3, lifecycle 90d |
| IAM role EC2 | imedto-ec2-role | |
| Instance profile | imedto-ec2-profile | |
| SSM params | /imedto/dev/* | ~10 parâmetros |
| SES domain | imedto.com.br | sandbox até prod access |
```

**Não comitar senhas/chaves nesse arquivo.** Senhas vivem só no SSM e no gerenciador de senhas pessoal.

---

## 11. Próximos passos

Com tudo isso pronto, F0 do plano de migração está fechada. Continue por:

1. **[PLANO_DOMINIO_SSL_EMAIL.md](PLANO_DOMINIO_SSL_EMAIL.md)** — apontar Cloudflare, configurar Caddy no EC2, validar domínio no SES.
2. **[PLANO_CICD_GITHUB_ACTIONS.md](PLANO_CICD_GITHUB_ACTIONS.md)** — montar pipeline que builda Docker e faz deploy via SSH.
3. **F1 do [PLANO_MIGRACAO_AWS_GREENFIELD.md](PLANO_MIGRACAO_AWS_GREENFIELD.md)** — implementar `LocalJwtAuthService`.

---

## 12. Comandos de "matar tudo" (cleanup se errar feio)

Caso queira recriar do zero (cuidado — apaga tudo):

```bash
# RDS
aws rds delete-db-instance --db-instance-identifier imedto-dev \
    --skip-final-snapshot --delete-automated-backups
aws rds delete-db-subnet-group --db-subnet-group-name imedto-subnets
aws rds delete-db-parameter-group --db-parameter-group-name imedto-pg17

# EC2
aws ec2 terminate-instances --instance-ids $EC2_ID
aws ec2 release-address --allocation-id $EIP_ALLOC

# Security Groups
aws ec2 delete-security-group --group-id $SG_RDS
aws ec2 delete-security-group --group-id $SG_EC2

# S3 (precisa esvaziar antes)
aws s3 rm s3://imedto-fotos-dev-<account-id> --recursive
aws s3 rb s3://imedto-fotos-dev-<account-id>
aws s3 rm s3://imedto-anexos-dev-<account-id> --recursive
aws s3 rb s3://imedto-anexos-dev-<account-id>

# IAM
aws iam remove-role-from-instance-profile \
    --instance-profile-name imedto-ec2-profile --role-name imedto-ec2-role
aws iam delete-instance-profile --instance-profile-name imedto-ec2-profile
aws iam delete-role-policy --role-name imedto-ec2-role --policy-name imedto-ec2-policy
aws iam delete-role --role-name imedto-ec2-role

# SSM (uma a uma)
aws ssm delete-parameters --names $(aws ssm get-parameters-by-path \
    --path /imedto/dev --recursive --query "Parameters[].Name" --output text)
```

---

## 13. Troubleshooting

| Sintoma | Causa provável | Resolução |
|---|---|---|
| `ssh ... Connection timed out` | SG não libera seu IP | `curl ifconfig.me` e atualizar regra do SG |
| `psql: could not connect` (do EC2) | RDS ainda subindo, ou SG do RDS não aponta pro SG da EC2 | `aws rds describe-db-instances` mostrar `available`; conferir `--source-group` |
| `aws s3 ls: AccessDenied` (na EC2) | Instance profile não attached ou IAM ainda propagando | Esperar 30s, conferir com `aws sts get-caller-identity` |
| Cobrança aparece no billing | Recurso fora do free tier | Logs do billing → "Free Tier" → ver qual recurso passou; geralmente NAT Gateway, Multi-AZ, Public IP not-attached, ou Aurora |
| `db.t4g.micro` "not supported" | Engine version não suporta t4g em sa-east-1 | Trocar pra `db.t3.micro` (também free tier) |
| Bucket S3 nome rejeitado | Nome global, já em uso | Adicionar mais sufixos (`imedto-fotos-dev-<account-id>-<random>`) |
