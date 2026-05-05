---
titulo: Domínio, SSL/HTTPS e e-mail transacional (Free Tier)
status: ativo
criado_em: 2026-05-04
escopo: Comprar domínio, apontar pra Cloudflare (DNS grátis), proxiar/expor a EC2, gerar HTTPS automático com Caddy + Let's Encrypt, verificar domínio no SES e sair do sandbox de e-mail. Custo: ~R$ 40/ano de domínio + AWS $0.
companhia: PLANO_MIGRACAO_AWS_GREENFIELD.md, PLANO_SETUP_AWS_PASSO_A_PASSO.md, PLANO_CICD_GITHUB_ACTIONS.md.
---

# Domínio, SSL e e-mail — passo a passo (Free Tier)

## 0. Visão geral

```
                         ┌──────────────────────────────┐
   Usuário ──────────►   │  Cloudflare (Brasil edge)    │
   https://app.imedto    │  • DNS                       │
                         │  • Proxy (laranja, opcional) │
                         │  • WAF/cache grátis          │
                         └──────────────┬───────────────┘
                                        │ TCP 443
                                        ▼
                         ┌──────────────────────────────┐
                         │  EC2 Elastic IP              │
                         │  Caddy (porta 80/443)        │
                         │   ↳ HTTPS automático LE      │
                         │   ↳ proxy → frontend:80      │
                         │   ↳ proxy /api → backend:5000│
                         └──────────────────────────────┘

   E-mail saindo:
   Backend ──► AWS SES ──► caixa do destinatário
                 ↑
                 └─ domínio verificado via DKIM (3 CNAMEs no Cloudflare)
```

**Decisão:** Cloudflare faz DNS + (opcional) proxy/CDN. Caddy no EC2 faz HTTPS direto com Let's Encrypt. SES usa domínio verificado via DKIM. **Tudo grátis.**

---

## 1. Comprar domínio (15 min)

### 1.1 Onde comprar

| Provedor | Custo `.com.br` | Custo `.com` | Notas |
|---|---|---|---|
| **Registro.br** | R$ 40/ano | — | Único oficial pra `.br`. Aceita PIX. |
| Cloudflare Registrar | — | US$ 10/ano | Sem markup, mas só TLDs internacionais. |
| Namecheap | — | US$ 11/ano | TLDs variados. |

**Recomendação:** `.com.br` no Registro.br se for produto BR; `.com` no Cloudflare Registrar se quiser internacional.

### 1.2 Após comprar

- Anote: domínio (ex: `imedto.com.br`), conta no provedor.
- Habilite **MFA** no provedor de domínio (sequestro de domínio é o vetor de ataque mais devastador).

---

## 2. Cloudflare — DNS grátis (10 min)

Mesmo que tenha comprado em outro registrar, o **DNS** é gerenciado no Cloudflare.

### 2.1 Criar conta

1. https://cloudflare.com/sign-up — e-mail + senha forte.
2. Habilitar MFA (Settings → Security → Two-Factor Authentication).

### 2.2 Adicionar o site

1. Dashboard → **"Add a site"** → digitar `imedto.com.br` → **Free plan**.
2. Cloudflare scaneia DNS existente. Pode haver 0 registros se domínio é novo — tudo bem.
3. Cloudflare mostra **2 nameservers** (algo como `cody.ns.cloudflare.com` e `lia.ns.cloudflare.com`).

### 2.3 Apontar nameservers no registrar

**Registro.br:**
1. Login em https://registro.br
2. Ir em **"Meus domínios"** → clicar no domínio.
3. **"DNS"** → trocar pra **"Configurar DNS no Registro.br? Não"** (ou similar) → selecionar nameservers customizados.
4. Colar os 2 nameservers do Cloudflare. Salvar.
5. Aguardar propagação: 30 min a 24h.

**Cloudflare Registrar / Namecheap:** o domínio já vem apontando pro Cloudflare (Registrar) ou tem opção "Use Cloudflare nameservers" (Namecheap).

### 2.4 Verificar propagação

```bash
dig NS imedto.com.br +short
# Esperado: cody.ns.cloudflare.com, lia.ns.cloudflare.com
```

Quando aparecerem, voltar ao Cloudflare → o site fica **"Active"** (banner verde).

---

## 3. Apontar DNS pra EC2 (5 min)

No Cloudflare, dashboard do domínio → **DNS → Records → Add record**:

| Tipo | Nome | Conteúdo | Proxy | TTL |
|---|---|---|---|---|
| A | `app` | `<ELASTIC_IP>` | 🟠 Proxied | Auto |
| A | `@` (root) | `<ELASTIC_IP>` | 🟠 Proxied | Auto |
| CNAME | `www` | `imedto.com.br` | 🟠 Proxied | Auto |

> **Proxy "Proxied" 🟠 vs "DNS only" cinza:**
> - **Proxied:** Cloudflare fica entre o usuário e a EC2 → SSL grátis no edge, DDoS protection, oculta IP da EC2.
> - **DNS only:** o IP real fica visível, conexão direta.
>
> **Para o app, manter Proxied.** Para os CNAMEs do SES (DKIM), tem que ser **DNS only** (ver §6).

### 3.1 SSL/TLS no Cloudflare

Dashboard do domínio → **SSL/TLS → Overview** → modo **"Full (strict)"**.

Significa: navegador → Cloudflare = HTTPS válido (cert do Cloudflare); Cloudflare → EC2 = HTTPS válido (cert do Caddy/Let's Encrypt). Sem isso, ou cai em "Flexible" (HTTP entre CF e EC2 — vulnerável a MITM no datacenter da EC2) ou erro de cert.

### 3.2 Always Use HTTPS

**SSL/TLS → Edge Certificates** → **"Always Use HTTPS" → On**. Redireciona HTTP → HTTPS no edge.

---

## 4. Caddy no EC2 — HTTPS automático (15 min)

Caddy gera certificado Let's Encrypt **sozinho** na primeira request HTTPS válida. Sem renovação manual, sem cron, sem certbot.

### 4.1 Caddyfile no EC2

Cria em `/home/ec2-user/imedto/Caddyfile`:

```caddyfile
# Domínio principal — frontend (Vue) + proxy /api → backend
app.imedto.com.br {
    encode zstd gzip

    # Headers de segurança
    header {
        Strict-Transport-Security "max-age=31536000; includeSubDomains; preload"
        X-Content-Type-Options "nosniff"
        X-Frame-Options "DENY"
        Referrer-Policy "strict-origin-when-cross-origin"
        Permissions-Policy "camera=(), microphone=(), geolocation=()"
        -Server
    }

    # API → backend container
    handle /api/* {
        reverse_proxy backend:5000 {
            header_up X-Real-IP {remote_host}
            header_up X-Forwarded-Proto https
        }
    }

    # SignalR (websocket)
    handle /hubs/* {
        reverse_proxy backend:5000 {
            header_up X-Real-IP {remote_host}
            header_up X-Forwarded-Proto https
        }
    }

    # Front (nginx servindo SPA)
    handle {
        reverse_proxy frontend:80
    }

    log {
        output stdout
        format console
    }
}

# Redirect raiz pro app
imedto.com.br, www.imedto.com.br {
    redir https://app.imedto.com.br{uri} permanent
}
```

### 4.2 Caddy no docker-compose.yml

Já incluído no compose do [PLANO_CICD_GITHUB_ACTIONS.md §4](PLANO_CICD_GITHUB_ACTIONS.md). Pontos chave:

- Caddy **monta `Caddyfile`** read-only.
- Volumes nomeados `caddy-data` e `caddy-config` **persistem o cert** entre restarts (importante! Let's Encrypt tem rate limit de 5 certs/semana por domínio).
- Portas 80 e 443 expostas direto na host (pra LE conseguir validar).

### 4.3 Subir e validar

```bash
ssh ec2-user@<ELASTIC_IP>
[ec2]$ cd ~/imedto
[ec2]$ docker compose up -d caddy
[ec2]$ docker compose logs -f caddy
```

Você verá Caddy resolvendo o desafio ACME e pegando o cert. ~30 segundos.

```bash
# Do laptop:
curl -v https://app.imedto.com.br/api/health
# Esperado: HTTP/2 200, cert válido emitido por Let's Encrypt (via Cloudflare se Proxied).
```

### 4.4 Como o Cloudflare Proxied + Caddy interagem

Com **Proxied 🟠**:
- O usuário vê o cert do **Cloudflare** (válido pra `*.imedto.com.br`).
- Cloudflare fala com EC2 em HTTPS, e vê o cert do **Caddy/Let's Encrypt**.
- Caddy precisa do desafio HTTP-01 funcionando, o que requer Cloudflare encaminhar a request `/.well-known/acme-challenge/...` pra origem. **Cloudflare faz isso automaticamente.**

Se algo der errado na geração do cert do Caddy, **temporariamente** desprossária (cinza "DNS only") até validar, depois religa.

### 4.5 Renovação

Automática. Caddy renova ~30 dias antes do vencimento, sem qualquer intervenção. Volume `caddy-data` precisa **persistir** — se você apagar acidentalmente (`docker volume rm`), próximo restart pode bater rate limit.

---

## 5. Hardening de cabeçalhos no Cloudflare (5 min)

Cloudflare → **Rules → Page Rules** (free tier permite 3 rules):

### 5.1 Cache estático no edge

Pattern: `app.imedto.com.br/assets/*`
- **Cache Level**: Cache Everything
- **Edge Cache TTL**: 1 month

### 5.2 Bypass cache pra API

Pattern: `app.imedto.com.br/api/*`
- **Cache Level**: Bypass

> Por padrão Cloudflare **não cacheia** respostas com `Set-Cookie`, mas é prudente bypassar a API explicitamente — evita cache acidental de algum endpoint público.

### 5.3 Rate limiting (opcional, free tier dá 1 regra básica)

Cloudflare → **Security → Rate limiting rules** → permite 1 regra grátis. Sugestão:

- Path: `/api/auth/login`
- Limite: 10 requests / 1 min por IP
- Ação: Block 10 min

Mitigação básica de brute-force antes de qualquer coisa chegar no SES/RDS.

---

## 6. Amazon SES — verificação de domínio (30 min + espera)

Pré-requisito: domínio no Cloudflare ativo.

### 6.1 Criar identity de domínio

```bash
aws ses verify-domain-identity --domain imedto.com.br --region sa-east-1
# Retorna: { "VerificationToken": "abc123..." }

aws ses verify-domain-dkim --domain imedto.com.br --region sa-east-1
# Retorna: { "DkimTokens": ["token1", "token2", "token3"] }
```

### 6.2 Adicionar registros DNS no Cloudflare

3 CNAMEs DKIM (com **Proxy = DNS only** cinza! ❗):

| Tipo | Nome | Conteúdo | Proxy |
|---|---|---|---|
| CNAME | `<token1>._domainkey` | `<token1>.dkim.amazonses.com` | ☁️ DNS only |
| CNAME | `<token2>._domainkey` | `<token2>.dkim.amazonses.com` | ☁️ DNS only |
| CNAME | `<token3>._domainkey` | `<token3>.dkim.amazonses.com` | ☁️ DNS only |

**Sempre DNS only pros DKIM.** Cloudflare proxiando esses CNAMEs quebra DKIM (CNAME chain não bate).

Mais 1 TXT pra **SPF** (autoriza SES a enviar em nome do domínio):

| Tipo | Nome | Conteúdo |
|---|---|---|
| TXT | `@` | `v=spf1 include:amazonses.com ~all` |

E 1 TXT pra **DMARC** (recomendado, melhora deliverability):

| Tipo | Nome | Conteúdo |
|---|---|---|
| TXT | `_dmarc` | `v=DMARC1; p=quarantine; rua=mailto:dmarc@imedto.com.br; pct=100; aspf=r; adkim=r` |

(Crie a caixa `dmarc@imedto.com.br` em algum lugar — Gmail Workspace, Zoho Mail free, ou um forwarder Cloudflare Email Routing.)

### 6.3 Verificar status

```bash
aws ses get-identity-verification-attributes --identities imedto.com.br
# VerificationStatus: Pending → Success em 5–30 min

aws ses get-identity-dkim-attributes --identities imedto.com.br
# DkimVerificationStatus: Pending → Success
```

---

## 7. SES Production Access (sair do sandbox)

**Conta SES nova começa em sandbox:**
- Só envia pra e-mails verificados individualmente.
- Limite de 200 e-mails/dia, 1/segundo.

Pra produção real:

1. SES Console → **"Account dashboard"** → **"Request production access"**.
2. Preencher form:
   - **Mail type**: Transactional
   - **Website URL**: `https://app.imedto.com.br`
   - **Use case**: "Healthcare SaaS for Brazilian medical clinics. Sending: account confirmation emails, password resets, professional invitations. All recipients are users who explicitly created an account or were invited by a clinic admin. We implement double opt-in for signup, immediate suppression on bounce/complaint, and provide unsubscribe link in non-transactional categories."
   - **Compliance**: confirmar que segue políticas (LGPD, CAN-SPAM, suppression lists).
3. Submit → aprovação tipicamente em 24–48h.

Depois da aprovação:
- Limite inicial: 50.000 e-mails/dia (ajusta com base em reputação).
- Pode enviar pra qualquer destinatário.

### 7.1 Identity policy pra permitir IAM role enviar

Já configuramos a policy `imedto-ec2-role` em [PLANO_SETUP_AWS_PASSO_A_PASSO.md §7.2](PLANO_SETUP_AWS_PASSO_A_PASSO.md) com `ses:SendEmail`. Confirmar:

```bash
aws iam get-role-policy --role-name imedto-ec2-role --policy-name imedto-ec2-policy \
    | grep -A2 SesSend
```

### 7.2 Bounce/complaint handling

SES requer monitorar bounces e complaints (e-mails marcados como spam). Mínimo aceitável:

1. SES Console → **Configuration sets** → **Create**: `imedto-default`.
2. **Event destinations** → adicionar SNS topic `imedto-ses-events` recebendo eventos `Bounce` e `Complaint`.
3. SNS topic com subscription de e-mail (`alerts@imedto.com.br`).

Em código, ao enviar e-mail, passar `ConfigurationSetName = "imedto-default"`.

Se taxa de bounce > 5% ou complaint > 0.1%, AWS suspende o envio. Ter listas internas de **suppression** (e-mails que já bounceram não tentar de novo).

---

## 8. Configurar backend pra usar SES (resumo de código)

A implementação completa entra na Fase 1 do [PLANO_MIGRACAO_AWS_GREENFIELD.md](PLANO_MIGRACAO_AWS_GREENFIELD.md). Esqueleto:

```csharp
public class SesEmailService : IEmailService
{
    private readonly IAmazonSimpleEmailServiceV2 _ses;
    private readonly EmailOptions _opt;

    public async Task EnviarAsync(string para, string assunto, string corpoHtml)
    {
        await _ses.SendEmailAsync(new SendEmailRequest
        {
            FromEmailAddress = _opt.From,                    // "noreply@imedto.com.br"
            ConfigurationSetName = _opt.ConfigurationSet,    // "imedto-default"
            Destination = new Destination { ToAddresses = { para } },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = assunto, Charset = "UTF-8" },
                    Body = new Body
                    {
                        Html = new Content { Data = corpoHtml, Charset = "UTF-8" }
                    }
                }
            },
            ReplyToAddresses = { _opt.ReplyTo ?? _opt.From }
        });
    }
}
```

Credenciais resolvidas via **IAM role** da EC2 (sem access key no código).

---

## 9. Templates de e-mail (mínimo viável)

Manter templates em `backend/src/Services/Imedto.Backend.Infrastructure/Email/Templates/*.html`. Sugestão de 3 inicialmente:

1. `confirmacao-email.html` — "Confirme seu e-mail" + botão com link contendo `?token=...`.
2. `reset-senha.html` — "Você pediu redefinição de senha" + link expira 1h.
3. `convite-profissional.html` — "Você foi convidado a participar de [estabelecimento]" + link.

Variáveis simples (`{{nome}}`, `{{link}}`) substituídas em string. Não complicar com Razor/Liquid agora — voltar nisso quando tiver volume real.

### 9.1 LGPD nos e-mails

- **Footer obrigatório**: "Você está recebendo este e-mail porque criou uma conta em app.imedto.com.br" + link de exclusão da conta.
- **Não logar conteúdo do e-mail** no CloudWatch — só logar `messageId` e categoria.
- **Não vazar** dados de outro paciente em e-mail (verificar templates antes de cada deploy).

---

## 10. Cloudflare Email Routing — receber e-mails (opcional, grátis)

Útil pra criar `contato@imedto.com.br`, `dmarc@imedto.com.br`, `seguranca@imedto.com.br` que **encaminham** pro seu Gmail pessoal.

Cloudflare → **Email → Email Routing** → **Get started** → adiciona MX + SPF automaticamente → criar regras de forwarding.

Sem custo, sem caixa de entrada própria, suficiente até precisar de Google Workspace (US$ 6/usuário/mês).

---

## 11. Validação fim a fim

Depois de tudo configurado:

```bash
# 1. DNS resolvendo via Cloudflare
dig app.imedto.com.br +short
# Esperado: 1-2 IPs do Cloudflare (104.x ou 172.67.x)

# 2. HTTPS válido
curl -I https://app.imedto.com.br/api/health
# Esperado: HTTP/2 200, header strict-transport-security

# 3. Frontend acessível
curl -L https://imedto.com.br/
# Esperado: redirect 308 → https://app.imedto.com.br/, depois HTML do Vue

# 4. SES verificado
aws ses get-identity-verification-attributes --identities imedto.com.br
# VerificationStatus: Success

# 5. Envio de teste (se ainda em sandbox, só pra e-mail verificado)
aws ses send-email \
    --from noreply@imedto.com.br \
    --to seu-email@gmail.com \
    --subject "Teste SES" \
    --text "Funcionando."
```

5 verdes = stack pronta. ✅

---

## 12. Checklist final (em ordem)

- [ ] Domínio comprado.
- [ ] MFA habilitado no provedor de domínio.
- [ ] Conta Cloudflare criada + MFA.
- [ ] Site adicionado no Cloudflare → nameservers trocados no registrar.
- [ ] DNS propagado (`dig NS` retorna Cloudflare).
- [ ] Registros A `app` e `@` apontando pro Elastic IP, Proxied 🟠.
- [ ] CNAME `www` Proxied 🟠.
- [ ] SSL/TLS modo **Full (strict)**.
- [ ] **Always Use HTTPS** = On.
- [ ] Page Rules: cache `/assets/*`, bypass `/api/*`.
- [ ] (Opcional) Rate limit em `/api/auth/login`.
- [ ] Caddy rodando no EC2 com Caddyfile.
- [ ] Volumes `caddy-data` persistentes.
- [ ] HTTPS válido em `https://app.imedto.com.br/api/health`.
- [ ] SES domain identity verificada.
- [ ] DKIM tokens (3 CNAMEs) adicionados, **DNS only ☁️**.
- [ ] SPF TXT adicionado.
- [ ] DMARC TXT adicionado.
- [ ] SES Production Access solicitado.
- [ ] Configuration set `imedto-default` com SNS para Bounce/Complaint.
- [ ] Cloudflare Email Routing pra inboxes administrativas (opcional).

---

## 13. Troubleshooting

| Sintoma | Causa | Resolução |
|---|---|---|
| `dig NS imedto.com.br` ainda mostra registrar antigo | Propagação DNS (até 24h) | Aguardar; testar com `dig @1.1.1.1 NS imedto.com.br` |
| Cloudflare em "Pending nameserver update" | Registrar não atualizou ou typo | Conferir nameservers exatos (sem ponto final, lowercase) |
| Caddy log: `unable to obtain certificate` | Cloudflare bloqueou desafio ACME ou DNS não resolve | Desprossária temporariamente (DNS only), gerar cert, religar Proxied |
| `curl https://app.imedto.com.br` retorna 521 | Cloudflare alcança CF mas não a origem | EC2 SG não libera 443, ou Caddy não rodando — `docker compose ps` no EC2 |
| `curl` retorna 525 (SSL handshake failed) | SSL/TLS mode "Strict" mas cert da origem inválido | Conferir Caddy gerou cert; ou trocar pra "Full" temporariamente |
| SES verificação fica em Pending > 1h | DKIM CNAMEs com Proxy laranja | Trocar pra **DNS only** (cinza) os 3 CNAMEs DKIM |
| E-mails caindo em spam | DKIM/SPF/DMARC mal configurado, ou IP reputação | Validar com https://www.mail-tester.com — pontuação ≥9/10 |
| `Email address is not verified` ao testar SES | Ainda em sandbox | Verificar destinatário com `aws ses verify-email-identity`, ou pedir prod access |
| Renovação de cert Caddy falha | Volume `caddy-data` apagado, rate limit LE | Esperar 1 semana, ou usar staging endpoint LE até resolver |

---

## 14. Próximos passos depois desse guia

Com domínio + HTTPS + SES funcionando, você consegue:

1. ✅ Rodar o pipeline de CI/CD do [PLANO_CICD_GITHUB_ACTIONS.md](PLANO_CICD_GITHUB_ACTIONS.md) com `EC2_HOST=app.imedto.com.br` e `APP_URL=https://app.imedto.com.br`.
2. ✅ Implementar **Fase 1** do [PLANO_MIGRACAO_AWS_GREENFIELD.md](PLANO_MIGRACAO_AWS_GREENFIELD.md) (`LocalJwtAuthService`) com SES funcional pra confirmação de e-mail.
3. ✅ Configurar frontend `VITE_API_BASE_URL=` (vazio — `/api` no mesmo domínio via Caddy).
