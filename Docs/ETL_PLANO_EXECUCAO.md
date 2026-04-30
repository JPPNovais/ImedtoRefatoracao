# Plano de Execução — ETL Fase 5

**Janela proposta:** **Domingo, 14 de junho de 2026, 01:00–06:00 BRT** (5 horas, dimensionada para o cenário otimista de [ETL_VOLUMETRIA.md](ETL_VOLUMETRIA.md)).
**Duração estimada de execução técnica:** ~2h40 brutas + buffer de 100% (2h20) = **5h totais**.
**Downtime visível ao usuário (read-only):** **3h** (das 02:00 às 05:00 BRT) — janela de 01:00 e 05:00–06:00 são preparação e estabilização interna; o app legado permanece disponível em leitura durante a etapa de cópia de Storage online (ver §3).
**Equipe necessária (mínima):** Tech lead, DBA/Data Engineer, DevOps, QA, CS/Suporte (ver §6).

> Justificativa da data: domingo de meio de mês, fora de feriados nacionais (Corpus Christi 04/jun/2026 já passou), antes das férias escolares de julho — distribuição de uso clínico mínima nesse fim de semana. Mês de junho permite 6 semanas de comunicação prévia a partir da publicação deste plano (2026-04-29). Caso a Wave 2 (scripts ETL e dry-run) atrase, fallback é **domingo 12/jul/2026 01:00–06:00 BRT**.

> *Alternativa considerada e descartada: madrugada de sábado para domingo (00:00 sábado→05:00). Descartada porque algumas clínicas de plantão operam até a noite de sábado; domingo de madrugada oferece a maior tranquilidade clínica.*

---

## 1. Comunicação prévia

A comunicação é o vetor de risco #2 (depois da própria execução técnica): se usuários não souberem da janela e não resetarem senha, o cutover abre o sistema novo com ninguém conseguindo logar (senhas não migram — ver [ETL_MAPEAMENTO.md §1](ETL_MAPEAMENTO.md)). Cadência de 4 toques nos 7 dias anteriores.

### T-7 dias (domingo, 07/jun/2026)
- **Email** para todos os usuários cadastrados (donos de estabelecimento + profissionais com vínculo ativo): template em §8.
- **Banner persistente no app legado** (header global): "Manutenção programada para 14/jun. Sistema indisponível das 02:00 às 05:00. Após a manutenção, será necessário redefinir sua senha — instruções por e-mail."
- **Post no canal de status** ([status.imedto.com.br](https://status.imedto.com.br/) — *configurar se ainda não existir; alternativa: post no Instagram + WhatsApp Business*).

### T-3 dias (quinta, 11/jun/2026)
- Email lembrete (template §8).
- Banner intensificado: "Manutenção em 3 dias — salve trabalhos em andamento, finalize orçamentos abertos."
- Verificação operacional preliminar: equipe confirma disponibilidade para o domingo, snapshots de teste executados, checklist §7 com >80% verde.

### T-1 dia (sábado, 13/jun/2026, 12:00 BRT)
- Email final (template §8) com link explícito para o procedimento de reset de senha pós-cutover.
- Banner: "Manutenção amanhã 02:00–05:00. Reset de senha será necessário após retorno."
- Verificação operacional final: backups validados (restaurabilidade testada em ambiente staging), equipe confirma presença, smoke tests pré-cutover OK em staging, runbook impresso/aberto.

### T-1 hora (sábado, 13/jun/2026, 23:00 BRT) — *já entrando no domingo*
> Ajuste: como a janela começa às 01:00 do domingo, esta etapa fica em **00:00 BRT do domingo**.
- Banner final: "Manutenção em 1 hora — encerre operações."
- Cron / job dispara: nenhum agendamento aberto pode ficar pendente de finalização (notificação aos profissionais com agendamentos `em_andamento` solicitando conclusão).
- Equipe entra em call (Discord/Meet — *recomendado: Discord, mais leve para call longa de 5h*).

---

## 2. Pré-cutover (T-30min — 00:30 BRT)

Checklist obrigatório antes de iniciar T+0. **Sem 100% dos itens marcados, abortar e remarcar janela.**

- [ ] **Backup snapshot manual do legado** via Supabase Dashboard → Database → Backups → Create snapshot. Anotar ID e timestamp. Validar que o snapshot aparece como `completed`.
- [ ] **Backup snapshot manual do novo** (idem). Esse é o snapshot de rollback caso o ETL corrompa o destino.
- [ ] **Máquina ETL intermediária ativa**:
  - **Decisão recomendada:** **VM EC2 `t3.medium` (2 vCPU, 4 GB RAM) em `sa-east-1a`**, mesma região do destino Supabase, com Postgres 15 local em volume EBS gp3 de 50 GB (cabe os ~15 GB do dump otimista com folga). Acesso via SSH + IAM Instance Profile. Custo: <U$ 1 pela janela inteira.
  - *Alternativas consideradas: (a) container Docker em laptop do tech lead — descartado, latência variável e risco de queda de conexão residencial em janela longa; (b) AWS Cloud Run job — descartado, sem persistência de DB local entre passos.*
- [ ] **EtlValidator** (`backend/src/Services/Imedto.Backend.EtlValidator/`) buildado com `dotnet publish -c Release` e binário disponível na máquina ETL.
- [ ] **Acesso confirmado a ambos os Supabase**: senha do pooler legado + senha do pooler novo testadas com `psql -c "select 1"`. Service role keys em variáveis de ambiente da máquina ETL (não em arquivo).
- [ ] **Runbook aberto** (este documento) + planilha de tracking (Google Sheets ou Notion) com cada passo da §3 listado para marcar `start/end` em tempo real.
- [ ] **Equipe online no Discord** — todos os papéis de §6 com microfone testado.
- [ ] **DNS gerenciável**: token / acesso ao registrar (Cloudflare/Route53) confirmado, tempo de TTL atual do domínio principal verificado e reduzido para 60s **3 dias antes** (não na hora — propagação demora).
- [ ] **Página de manutenção estática** publicada em CDN, pronta para ser apontada via DNS em caso de rollback de emergência.

---

## 3. Execução (T+0 = 01:00 BRT até T+5h = 06:00 BRT)

> Cada passo abaixo tem **timestamp esperado**, **responsável principal**, **comando concreto** e **critério de OK**. Tech lead marca cada um na planilha de tracking.

### T+0 (01:00) — Hard cutover do legado para read-only
- **Responsável:** DevOps.
- Setar variável `IMEDTO_READ_ONLY=true` no app legado (Vercel/Cloudflare Pages — depende da hospedagem atual; *recomendado: feature flag via Supabase remoto, atualizada por SQL no projeto legado para refletir imediatamente sem redeploy*).
- Validação: `curl -X POST https://app.imedto.com.br/api/agendamentos -i` deve retornar HTTP 503 com payload `{"erro":"Manutenção programada em curso"}`.
- **Capturar T0** = timestamp UTC exato → grava em `etl_run_metadata` no DB intermediário. Esse timestamp é a "linha de corte" do ETL.
- **Storage online sync (paralelo, já iniciado em T-2h)**: cópia de buckets `imedto-anexos` e `imedto-fotos` do legado para o novo via `rclone copy` com `--checksum` foi iniciada às **23:00 do sábado** e roda em paralelo. A cópia "online" foi feita com app legado ainda em RW; em T+0, dispara o **delta sync** das últimas 2h (apenas arquivos com `created_at > T0 - 2h`). Mitigação prevista em [ETL_VOLUMETRIA.md §"Storage"](ETL_VOLUMETRIA.md). Reduz janela em ~30 min no cenário otimista.

### T+5min (01:05) — `pg_dump` do legado + restore intermediário
- **Responsável:** DBA.
- ```
  pg_dump "postgresql://postgres.[ref-legado]:***@aws-1-sa-east-1.pooler.supabase.com:5432/postgres" \
    --no-owner --no-acl --schema=public --schema=storage \
    --exclude-table-data='auth.*' \
    --format=directory --jobs=4 \
    --file=/data/dump_legado_$(date +%Y%m%d)
  ```
- Restore no DB local da VM em schema `legado`:
  ```
  pg_restore --jobs=4 --schema=public --no-owner --no-acl \
    -d postgresql://etl@localhost:5432/staging /data/dump_legado_*
  ```
- **Critério de OK:** `psql -c "select count(*) from legado.pacientes, legado.agendamentos, legado.evolucao_prontuario"` retorna números próximos da estimativa de [ETL_VOLUMETRIA.md](ETL_VOLUMETRIA.md). Se desviar mais de 50%, **pausar e investigar** antes de prosseguir.
- **Tempo-alvo:** 15 min conservador / 45 min otimista.

### T+50min (01:50) — Storage delta sync conclui
- **Responsável:** DevOps.
- Conferir log do `rclone` — 0 falhas. Se houver falhas, registrar em `etl_storage_failures.json` para retry pós-cutover.

### T+50min (01:50) — Aplicar scripts ETL
- **Responsável:** DBA.
- ```
  cd /home/etl/scripts/etl
  ./run_all.sh --staging-db postgresql://etl@localhost:5432/staging \
               --target-db   postgresql://postgres.[ref-novo]:***@aws-1-sa-east-1.pooler.supabase.com:5432/postgres \
               --service-role $SERVICE_ROLE_NOVO \
               2>&1 | tee /var/log/etl/run_$(date +%Y%m%d_%H%M%S).log
  ```
- O `run_all.sh` segue a ordem documentada em [ETL_MAPEAMENTO.md §"Ordem de execução"](ETL_MAPEAMENTO.md), com paralelismo Wave A/B/C definido em [ETL_VOLUMETRIA.md §"Recomendações de paralelização"](ETL_VOLUMETRIA.md).
- **Em caso de falha de qualquer script**: o orquestrador para, escreve `etl_run_metadata.status = 'failed'`, registra script + linha do erro no log. Tech lead decide em até 10 min: (a) corrigir manualmente e retomar do passo falho (idempotência via `ON CONFLICT` permite), ou (b) executar rollback (§4).
- **Tempo-alvo:** 5 min conservador / 25 min otimista (com 4-way paralelismo).

### T+1h30 (02:30) — Validação automatizada
- **Responsável:** QA + DBA.
- ```
  dotnet /opt/etl-validator/Imedto.Backend.EtlValidator.dll --modo counts \
    --legado /data/dump_legado_* --novo postgresql://postgres.[ref-novo]:***@...
  # Esperado: diff < 0.5% por tabela. Saída non-zero exit code aborta.

  dotnet /opt/etl-validator/Imedto.Backend.EtlValidator.dll --modo integrity
  # Esperado: 0 violações de FK, 0 órfãos, 0 PIIs em texto plano em colunas cifradas.

  dotnet /opt/etl-validator/Imedto.Backend.EtlValidator.dll --modo smoke \
    --smoke-users /etc/etl/smoke-users.json
  # Esperado: ≥95% dos usuários listados conseguem autenticar (após reset) e listar pacientes/agenda/receitas.
  ```
- **Revisão manual amostral (5 casos)** — QA abre o app novo (URL de staging apontando ao destino) e valida visualmente:
  1. 1 paciente com prontuário rico (multi-evoluções + anexos).
  2. 1 agendamento com lembrete + checklist.
  3. 1 receita controlada (notificação A/B).
  4. 1 orçamento cirúrgico (com equipe + implantes).
  5. 1 lançamento financeiro recente.

### T+2h (03:00) — DNS / cutover lógico
- **Responsável:** DevOps.
- Apontar registro `app.imedto.com.br` para frontend novo (Cloudflare / Vercel — alterar CNAME). TTL já está em 60s, propaga rápido.
- Apontar `api.imedto.com.br` para o backend .NET novo (CloudFront ou ALB → ECS task — *recomendado: CloudFront → ALB com 2 tasks ECS Fargate `512 CPU / 1024 MB` em sa-east-1, com auto-scaling até 6 tasks*).
- **Smoke test público:** `curl https://app.imedto.com.br/api/health` retorna `{"status":"ok"}` e Swagger acessível. Login real com 2 contas de equipe interna.
- Anunciar no canal de status: "Imedto v2 no ar. Acesse e redefina sua senha em /esqueci-minha-senha." Email §8 disparado.

### T+2h30 (03:30) — Pós-cutover imediato
- **Responsável:** Toda a equipe.
- Banner do legado removido / app legado redirecionado para a nova URL.
- Habilitar trigger anti-overlap em `agendamentos` no destino (estava desabilitada durante a carga). Rodar relatório `select_overlaps.sql` para gerar lista de conflitos históricos — log para CS abrir tickets posteriormente, **sem bloquear** cutover.
- Reabilitar triggers de imutabilidade em `prontuario_evolucoes` (estavam OFF na carga).
- `ANALYZE` em todas as tabelas tocadas: `psql -c "ANALYZE VERBOSE;"`.
- Equipe permanece em standby até T+5h (06:00). Tech lead, DevOps e CS por mais 4h adicionais (até 10:00 BRT) para captar problemas de "início do dia" — mas isso já é segunda-feira, então se 06:00 estiver tudo verde, time descansa e standby continua remoto via PagerDuty/oncall.

### T+5h (06:00) — Encerramento formal da janela
- Tech lead declara cutover "concluído com sucesso" se todas as métricas de §5 estiverem verdes. Atualiza `etl_run_metadata.status='completed'` e publica resumo no canal de status.

---

## 4. Plano de rollback

Rollback é **gratuito até T+2h** (legado nunca foi tocado em escrita). Após T+2h (DNS apontado para o novo), entra em modo "emergency rollback".

### Critério de decisão (gates)

| Momento | Tipo de falha | Decisão |
|---|---|---|
| **T+0 a T+30min** | `pg_dump` falha, máquina ETL trava, validação de contagens-base do staging diverge >50% | **Cancelar ETL.** Legado intacto. Reverter `IMEDTO_READ_ONLY=false`, retirar banner, anunciar "Manutenção adiada — sistema operacional novamente". Nova janela em ≤14 dias. Custo: 0 dados perdidos, ~30 min de read-only ao usuário. |
| **T+30min a T+2h** | Script ETL falha sem correção rápida (>20 min de tentativa), validação automática reprova (`counts` ou `integrity`), smoke test reprova | **Restore do snapshot do destino + cancelar.** Restaurar Supabase novo do snapshot pré-ETL (~10 min via Dashboard → Backups → Restore). Não apontar DNS. Reabrir legado em RW. Comunicar adiamento. |
| **T+2h em diante** | Falha descoberta após DNS apontado para o novo (usuários relatam erros, dados truncados, performance ruim) | **Procedimento de emergência abaixo.** |

### Procedimento de emergência (rollback completo após DNS cutover)

> Reservar este caminho para falhas críticas que afetam >20% dos usuários ou comprometem integridade de dados clínicos. Falhas pontuais corrigem em hot-fix sem rollback.

1. **Pausar tráfego** apontando DNS de `app.imedto.com.br` para a página de manutenção estática (CDN). 60s de TTL → propagação em <2 min.
2. **Snapshot do estado atual do novo** (preserva qualquer escrita feita pelos usuários nesse intervalo — útil para postmortem e possível replay manual de poucos casos).
3. **Restaurar Supabase novo** do snapshot pré-ETL (Dashboard → Restore). Tempo estimado: 10–20 min.
4. **Reverter app legado para read-write** (`IMEDTO_READ_ONLY=false`).
5. **Apontar DNS** de volta para o legado.
6. **Comunicar usuários** via banner + email + status page: "Detectamos um problema na nova versão. Reverteremos para nova janela em breve. Trabalhos realizados após 03:00 não serão preservados — pedimos que repita após o retorno."
7. Tech lead declara "rollback completo" e abre incidente formal.

**Tempo total do rollback de emergência:** 30–45 min.

### Investigação pós-rollback

- [ ] Logs do ETL em `/var/log/etl/run_*.log` movidos para S3 (`s3://imedto-postmortems/etl-2026-06-14/`) — não só stdout.
- [ ] Snapshot do staging preservado por 30 dias.
- [ ] **Postmortem em até 7 dias** com causa raiz, plano de correção, e nova janela proposta. Modelo blameless. Lições levam a checklist atualizado de §7.

---

## 5. Métricas de sucesso

| Métrica | Alvo | Como medir |
|---|---|---|
| Diff de contagem por tabela | <0.5% | `EtlValidator --modo counts` |
| Violações de integridade referencial | 0 | `EtlValidator --modo integrity` |
| Smoke tests funcionais passando | ≥95% | `EtlValidator --modo smoke` (50 usuários amostra) |
| Janela total | <4h efetiva | T+0 → cutover DNS |
| Reports de bugs críticos em 48h | 0 | Triagem CS + Sentry |
| Taxa de reset de senha em 7d | ≥80% dos usuários ativos | Query no Supabase Auth do novo |
| P95 latency da API novo em 24h pós-cutover | <500ms | OpenTelemetry → CloudWatch |

---

## 6. Equipe e responsabilidades

| Papel | Responsável (preencher) | Responsabilidade |
|-------|--------------------------|------------------|
| Tech lead | — | Coordena execução, decide go/no-go em cada gate, comunica equipe e usuários, declara conclusão ou rollback |
| DBA / Data Engineer | — | Executa `pg_dump`, restore, scripts ETL, monitora locks, queries lentas, ANALYZE pós-carga |
| DevOps | — | Provisiona VM ETL, gerencia DNS, deploy do backend novo, monitora CloudWatch, dispara rollback de DNS |
| QA | — | Roda smoke tests automatizados + manuais nos 5 casos amostrais, valida UX no app público pós-cutover |
| CS / Suporte | — | Triagem de tickets durante e após janela (canal dedicado), atualiza FAQ de "como redefinir senha", escala para tech lead |

**Standby remoto (não precisa estar em call ativo, só atingível em <15 min):** Backend dev, Frontend dev, Security engineer.

---

## 7. Checklist final pré-janela

Tech lead executa em T-1h (sábado 23:00 BRT) e marca cada item.

- [ ] Comunicação T-7d enviada (lista de envio retornou OK >95%).
- [ ] Comunicação T-3d enviada.
- [ ] Comunicação T-1d enviada com instruções de reset de senha.
- [ ] Backup pré-cutover do legado validado (snapshot `completed`, restore-test em staging passou).
- [ ] Backup pré-cutover do novo validado.
- [ ] VM ETL EC2 t3.medium provisionada, Postgres 15 rodando, `psql -c "select 1"` OK.
- [ ] EtlValidator buildado (`dotnet publish -c Release`) e binário em `/opt/etl-validator/` na VM.
- [ ] Smoke tests passando em ambiente staging com dataset de teste real (10% do legado).
- [ ] Equipe disponível e Discord testado.
- [ ] Plano de rollback ensaiado em staging (restore snapshot + DNS swap, cronometrado: <30 min).
- [ ] DNS TTL reduzido para 60s desde T-3d.
- [ ] Página de manutenção estática publicada e acessível por URL direta.
- [ ] Service role keys, senhas de pooler e PATs em variáveis de ambiente da VM (não em arquivo); rotação programada para T+7d.

---

## 8. Templates de comunicação

> Tom profissional, direto, em português brasileiro. Foco em "o que muda para a clínica" e "o que ela precisa fazer", evitando jargão técnico.

### Email T-7d (assunto: "Manutenção programada do Imedto — 14 de junho, 02h às 05h")

Olá, [primeiro_nome],

Informamos que o Imedto passará por uma **manutenção programada** no domingo, **14 de junho de 2026**, das **02h00 às 05h00 (horário de Brasília)**.

Durante esse período, o sistema estará **indisponível para uso**. Recomendamos que você finalize agendamentos, prescrições e orçamentos em aberto antes do início da janela.

**Importante — após a manutenção, será necessário redefinir sua senha** na primeira tentativa de login. Por motivos de segurança, sua senha atual não será preservada. Você receberá um e-mail com as instruções logo após o término.

Em caso de dúvida, responda este e-mail ou entre em contato pelo nosso suporte.

Atenciosamente,
Equipe Imedto

---

### Email T-1d (assunto: "Lembrete: manutenção do Imedto amanhã, 02h às 05h")

Olá, [primeiro_nome],

Lembramos que **amanhã, domingo 14/jun**, o Imedto ficará indisponível das **02h às 05h** para a manutenção que comunicamos anteriormente.

**O que você precisa fazer:**
1. Finalize trabalhos em andamento até hoje à noite.
2. **Após às 05h de domingo**, acesse [app.imedto.com.br/esqueci-minha-senha](https://app.imedto.com.br/esqueci-minha-senha) para definir uma nova senha.
3. Em seguida, faça login normalmente.

Caso encontre qualquer dificuldade após o retorno, nosso time estará em standby até segunda-feira ao meio-dia.

Atenciosamente,
Equipe Imedto

---

### Banner read-only (durante a janela)

> **Sistema em manutenção programada.** O Imedto está temporariamente disponível apenas para consulta. Previsão de retorno: 05h00. Acompanhe o status em [status.imedto.com.br](https://status.imedto.com.br).

---

### Email "concluído" (assunto: "Imedto atualizado — defina sua nova senha")

Olá, [primeiro_nome],

A manutenção do Imedto foi **concluída com sucesso**. O sistema já está operacional.

**Próximo passo (necessário apenas uma vez):**
- Acesse [app.imedto.com.br/esqueci-minha-senha](https://app.imedto.com.br/esqueci-minha-senha)
- Informe o e-mail desta conta
- Receberá um link para criar uma nova senha

Todos os seus dados — pacientes, agenda, prontuários, receitas, orçamentos — foram preservados. Caso identifique qualquer divergência, responda este e-mail e nosso time corrigirá em prioridade máxima.

Obrigado pela paciência durante a janela.

Equipe Imedto
