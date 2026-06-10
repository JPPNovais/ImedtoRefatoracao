# FASE 0 — Fundação Operacional e Infra AWS Escalável por Estágios

> Parte do roadmap [`README.md`](README.md). Origem: discovery [`roadmap-melhorias-2026`](../Discoverys/roadmap-melhorias-2026/01_discovery.md). **Status: planejado, execução adiada conscientemente** — o produto está em fase de desenvolvimento.
>
> **Modelo do plano**: infra cresce **por estágio, puxada por gatilho de métrica/negócio** — nunca por antecipação. Cada estágio prepara o seguinte sem retrabalho, e o custo só sobe quando o uso (ou a receita) justifica.

---

## Princípios

1. **Pagar só pelo que está em uso.** Upgrade acontece quando um gatilho objetivo dispara (métrica ou marco de negócio), não "por precaução".
2. **Serverless para o esporádico, instância para o constante, gerenciado para o estado.** Tarefas pontuais/spiky (migrations, backups, jobs agendados, processamento) → **Lambda/EventBridge** (paga por execução, centavos). API que recebe tráfego contínuo → instância fixa. Banco → gerenciado (RDS) a partir do momento em que há dado de cliente.
3. **Sem degraus pulados.** Cada estágio é suficiente para a carga do momento; o desenho não cria beco sem saída (ex.: jobs já saem do host via EventBridge no E1, o que torna a 2ª instância do E3 trivial).
4. **Backup e alerta não são estágio — são pré-requisito.** A partir do momento em que existir dado que dói perder, os itens "⚡" valem ser feitos mesmo em dev.

## Estado atual verificado (auditoria 2026-06-09)

Uma única EC2 `t3.micro` (1 GB RAM) com Postgres em container + API .NET + nginx + Caddy co-residentes; **sem backup automatizado**; sem alertas; OTel instrumentado sem coletor; Vitest e integration tests fora do gate de CI; SSH aberto a `0.0.0.0/0`; deploy aplica migration direto em prod sem ponto de restauração. Custo atual ≈ **US$10-15/mês**.

---

## Estágio 0 — Desenvolvimento (AGORA) · custo extra ≈ +US$1-2/mês

Objetivo: proteger o trabalho acumulado e fechar os buracos que custam **zero ou centavos**. Tudo aqui pode ser feito já, sem esperar o gatilho de monetização.

### ⚡ E0.1 Snapshot diário do EBS via DLM (15 min, centavos)
EC2 → Lifecycle Manager → política: snapshot diário do volume, retenção 7 dias. Protege contra falha de disco sem tocar na aplicação (snapshots são incrementais).

### ⚡ E0.2 `pg_dump` diário → S3 (1-2h, <US$1/mês)
Cron/systemd timer no host:
```bash
pg_dump -Fc -h localhost -U imedto imedto | \
  aws s3 cp - "s3://imedto-backups/postgres/daily/imedto-$(date +%F).dump" --sse aws:kms
```
Bucket com versionamento, acesso público bloqueado, lifecycle (Standard-IA aos 30d, expira aos 90d). IAM da EC2 só com `s3:PutObject` nesse prefixo (sem Delete). **Heartbeat**: ping em healthchecks.io (free) ao final; alarme se nenhum backup em 26h — *backup que falha em silêncio = não ter backup*.

### ⚡ E0.3 Runbook de restore testado (0,5 dia; depois ~20 min/mês)
Baixar dump → `pg_restore` em container local → 3-5 queries de sanidade (contagens por tenant, última migration). Registrar data/duração/resultado em `Docs/INFRA.md §Restore`. Repetir mensalmente. **Backup sem restore testado é hipótese, não garantia.**

### E0.4 Itens de código/CI sem custo de infra (2-3 dias, US$0)
Não dependem de AWS e fecham riscos reais agora:
- **`FallbackPolicy` global de autenticação** (default-deny): controller novo esquecido vira 401, não endpoint anônimo. Auditar e marcar `[AllowAnonymous]` explícito nos públicos (links por token, webhook BirdID, signup/login, health).
- **Gates de CI**: `test-frontend` rodando Vitest de verdade (`npm run test -- --run`); remover o filtro `!~Integration` do backend (runner `ubuntu-latest` tem Docker → Testcontainers funciona); garantir que só sobe imagem que passou nos testes.
- **Dump pré-migration no CI**: passo no job `migrate` reusando o script E0.2 com prefixo `pre-migration/<run_id>` (expira em 14 dias). Migration ruim passa a ter volta.
- **Magic bytes no upload**: validar assinatura binária (`%PDF`, `FF D8 FF`, `89 50 4E 47`) contra o MIME declarado antes do S3; lista fechada de tipos.
- **SSH → SSM Session Manager**: anexar `AmazonSSMManagedInstanceCore` à role, testar `aws ssm start-session`, **remover a regra 22 do Security Group**; deploy do CI via `aws ssm send-command` (substituído por Lambda no E1).

**Critério de saída do E0**: restore registrado no runbook · pipeline vermelho bloqueia deploy se teste falhar · porta 22 fechada · dump pré-migration aparecendo no S3 a cada deploy.

---

## Estágio 1 — Primeiros pagantes · custo total ≈ US$30-45/mês

**Gatilho**: primeiro cliente pagante assinado **ou** primeiro dado real de paciente em produção — o que vier primeiro.

### E1.1 Banco sai do host → RDS `db.t4g.micro` single-AZ (~US$15-18/mês)
O item mais importante do estágio: Postgres e .NET deixam de competir pelo 1 GB de RAM, e o banco ganha **backup nativo gerenciado** (snapshot diário + point-in-time recovery de 7 dias, incluídos no preço).
- Migração: `pg_dump`/`pg_restore` com janela curta (banco pequeno) + troca da connection string no SSM.
- Subnet **privada** (sem IP público); SG aceitando 5432 só do SG da app e do SG da Lambda (E1.2).
- O `pg_dump` lógico do E0.2 passa a ser **semanal** (defesa extra e restore portátil — RDS cobre o diário).
- Multi-AZ **não** entra aqui (dobra o custo) — gatilho próprio no E3.

### E1.2 Lambda `imedto-migrator` — migrations sem SSH e sem custo fixo (centavos/mês)
Resolve exatamente o caso "recurso caro/pontual via Lambda":
```
CI (GitHub Actions)
  └─ empacota db/migrations/ → zip
  └─ aws lambda invoke imedto-migrator
        Lambda (VPC privada, SG → RDS:5432, timeout 10-15 min)
          1. dispara dump pré-migration → S3
          2. aplica os .sql idempotentes em ordem
          3. loga resultado no CloudWatch Logs
  └─ CI lê a resposta → segue ou aborta o deploy
```
- Runtime: imagem de container com `psql` (ou .NET + Npgsql executando os scripts — reusa o runner de migrations já existente no repo).
- **Custo**: invocada só em deploy (poucas execuções/mês) → centavos. Sem instância parada, sem túnel, banco nunca exposto.
- Enquanto o banco ainda estiver na EC2 (E0), o equivalente é `aws ssm send-command`; a Lambda assume quando o RDS chega.

### E1.3 Jobs agendados saem do processo → EventBridge Scheduler (+ Lambda fininha) (centavos/mês)
Hoje: endpoints manuais (`/api/automacoes/expirar-orcamentos`, `enviar-lembretes`) e tabela `jobs_agendados`. Passam a ser disparados por EventBridge Scheduler (cron gerenciado) → Lambda mínima que chama o endpoint interno com header secreto (ganha retry + DLQ de graça).
Benefício além do custo: quando houver 2ª instância da app (E3), os jobs **não duplicam** — o disparo é externo ao processo.

### E1.4 Frontend estático → S3 + CloudFront (~US$1-3/mês)
Tira os estáticos da EC2 (libera RAM/CPU do host), entrega via CDN (latência menor no Brasil), e o deploy do front vira `aws s3 sync` + invalidation — independente do backend.

### E1.5 Observabilidade mínima viável (~US$2-4/mês)
- **CloudWatch Agent** (RAM/disco) + alarmes → SNS → e-mail: CPU>80% 15min · RAM>85% · disco>80% · StatusCheckFailed · RDS (CPU, storage, conexões) · heartbeat de backup.
- **OTel → Grafana Cloud free tier** (Tempo/Prometheus/Loki): só configurar `Otel:Endpoint` — a instrumentação já existe no código.
- **Uptime externo** (UptimeRobot/Better Stack free) no `/health` — externo à AWS de propósito: se a conta/região tiver problema, o alerta ainda chega.

**Critério de saída do E1**: app com >40% RAM livre · restore PITR do RDS testado 1× · deploy aplicando migration via Lambda com dump prévio automático · alerta chegando por e-mail em simulação · front servido por CloudFront.

---

## Estágio 2 — Tração · custo total ≈ US$70-130/mês

**Gatilhos** (qualquer um):
- RAM média do host >75% por 7 dias ou swap em uso constante → **E2.1**
- p95 da API >500ms sustentado com CPU do RDS >70% → **E2.2**
- Storage do RDS >70% → aumentar gp3 (online, sem downtime)
- Primeiras features com processamento pesado (PDF em lote, fotos, IA scribe) → **E2.3**

### E2.1 Upsize da app: `t4g.small` (2 GB, ~US$12) ou `t4g.medium` (4 GB, ~US$24)
Migrar para Graviton (t4g) já neste passo — ~20% mais barato por desempenho. Continua docker-compose num host só (simplicidade), agora sem banco e sem estáticos.

### E2.2 Upsize do banco: RDS `db.t4g.small` (~US$24-28)
Decisão por métrica do Performance Insights (grátis no tier básico), não por achismo.

### E2.3 Fila para trabalho pesado: SQS + Lambda/worker (~US$1-5/mês)
Padrão para o que vier de spiky: geração de PDF em lote, processamento de fotos (galeria antes/depois), transcrição IA scribe (chamada de API externa com retry), exportações LGPD grandes. API enfileira → Lambda consome. Paga por execução; nada ocioso.

### E2.4 Savings Plan / Reserved Instances (-30-40%)
Quando o tamanho de instância ficar estável por ~3 meses, travar 1 ano no-upfront. Não antes.

**Critério de saída do E2**: p95 <500ms nas telas de atendimento · zero OOM/swap · trabalho pesado fora do request HTTP.

---

## Estágio 3 — Escala e SLA · custo total ≈ US$250-500/mês

**Gatilhos** (qualquer um):
- Cliente/contrato exigindo SLA formal ou >99,5% de uptime medido como necessário
- Necessidade de deploy sem downtime ou de 2ª instância (redundância)
- ~100+ estabelecimentos ativos

Itens (o caminho já está apontado em `Docs/INFRA.md`):
- **RDS Multi-AZ** (~2× o custo do banco) — failover automático.
- **2 instâncias da app atrás de ALB** (ASG em EC2 ou ECS Fargate ~0.5vCPU/1GB por task ~US$18 cada + ALB ~US$20).
- **ElastiCache Redis** (`t4g.micro` ~US$11) — resolve os dois TODOs que o código já reconhece: cache distribuído (hoje `IMemoryCache` é por processo) e backplane do SignalR/realtime para multi-instância.
- **WAF** no ALB/CloudFront (~US$10-20) + rate limiting de borda.
- Subnets privadas com **VPC endpoints** (S3/SSM/Logs) em vez de NAT Gateway onde possível — NAT custa ~US$32/mês parado; endpoint de S3 é grátis.

**Critério de saída do E3**: deploy sem downtime · failover de banco testado · um host pode morrer sem derrubar o serviço.

---

## Mapa de uso de Lambda/serverless (resumo)

| Tarefa | Quando | Por quê Lambda | Custo |
|---|---|---|---|
| **Migrations no deploy** (`imedto-migrator`) | E1 | Pontual, precisa de acesso privado ao banco, elimina SSH e instância intermediária | centavos/mês |
| Dump lógico semanal pós-RDS | E1 | Cron raro, precisa estar na VPC | centavos |
| Jobs agendados (trials, lembretes, expirar orçamentos) | E1 | Cron gerenciado + retry/DLQ; evita duplicação com 2+ instâncias | centavos |
| PDF em lote / fotos / IA scribe / export LGPD | E2 | Spiky, fora do request HTTP, paga por uso | US$1-5/mês |
| **O que NÃO vai para Lambda** | — | API principal (tráfego contínuo + SignalR/realtime + cold start) fica em instância | — |

## Resumo de custos por estágio

| Estágio | Gatilho | Custo mensal aprox. | Delta |
|---|---|---|---|
| E0 (agora) | — | US$11-17 | +US$1-2 |
| E1 | 1º pagante / dado real | US$30-45 | +US$20-30 |
| E2 | métricas de saturação | US$70-130 | +US$40-85 |
| E3 | SLA / redundância / ~100 estabs | US$250-500 | +US$180-370 |

Referência de sanidade: no E1, **um único cliente no plano de ~R$149/mês já cobre toda a infraestrutura**. O plano nunca exige gasto à frente da receita.

## Relação com a documentação viva

Ao executar cada estágio: atualizar `Docs/INFRA.md` (topologia, backup/restore, alarmes, SSM/Lambda) e `Docs/COMANDOS.md` (runbooks) **na mesma entrega** — premissa do CLAUDE.md. A pipeline de deploy muda no E1 (migrator) e no E3 (ALB/Fargate); tratar cada mudança como demanda própria com briefing.
