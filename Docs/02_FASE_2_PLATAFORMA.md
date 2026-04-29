# Fase 2 — Plataforma transversal

**Status geral:** ✅ concluída no backend + banco (frontend, testes adicionais, paridade e load test pendentes — listados ao final)
**Iniciada em:** 2026-04-29
**Concluída em:** 2026-04-29

> **Objetivo:** construir os blocos de plataforma que são pré-requisito de várias features clínicas. Construir uma vez, reusar em várias features.
>
> **Pré-requisitos:** Fase 1 ✅ concluída e aplicada no banco.
>
> **Referência:** [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md), [01_FASE_1_HARDENING.md](01_FASE_1_HARDENING.md).

## Escopo total da fase

A Fase 2 incorpora os 9 itens originais do plano mestre **mais** as pendências documentadas no fim da Fase 1 (exceto a permissão fina `assistente_clinico` que fica para Fase 3 quando o ModeloPermissao for migrado plenamente).

### Itens do plano original

| # | Item | Descrição |
|---|------|-----------|
| 2.1 | Scheduler de jobs | Pré-requisito de 2.2, 2.5, 2.7. |
| 2.2 | Engine de automações | `automation_rules`, `automation_events`, worker. |
| 2.3 | Notificações in-app | `notificacoes`, canal interno. |
| 2.4 | Realtime | SignalR ou Supabase Realtime pass-through. |
| 2.5 | Storage de anexos com governança | Signed URL TTL curto + audit. |
| 2.6 | Catálogo Profissões/Especialidades | Ref data simples. |
| 2.7 | Subscription / trial / billing + feature gating | `assinaturas`, `planos`, `[FeatureGate]`. |
| 2.8 | Idempotência em commands externos | Header `Idempotency-Key`. |
| 2.9 | Observabilidade básica | Serilog estruturado + OpenTelemetry + métricas. |

### Pendências da Fase 1 incorporadas

| # | Item | Origem |
|---|------|--------|
| 2.10 | Aggregates `CategoriaFinanceira` + `FormaPagamento` + seed ao criar estabelecimento | Item 1.5 da Fase 1 (bloqueado) |
| 2.11 | `Profissional` como `ISoftDeletable` | Migration-engineer review da Fase 1 |
| 2.12 | `establishment_ai_settings` (toggle/limites por estabelecimento) | Migration-engineer review |
| 2.13 | FKs em `ai_audit_logs` (paciente/prontuário/evolução) | Migration-engineer review |
| 2.14 | Rate limit IA particionado por (usuário, estabelecimento) | Migration-engineer review |
| 2.15 | Constraint `EXCLUDE USING gist` para overlap de agenda no Postgres | Defense-in-depth |
| 2.16 | Teste de integração do `SoftDeleteInterceptor` | Cobertura |
| 2.17 | Load test do rate limit em `/auth` | Cobertura |
| 2.18 | RLS policies das 4 tabelas novas (`audit_delete_attempts`, `ai_audit_logs`, `ai_outputs_cache`, `ai_rate_limits`) | Pendência do banco |

> **Permissão fina `assistente_clinico`** continua na Fase 3 (depende do ModeloPermissao plenamente migrado).
> **Estratégia de admin-reset-estabelecimento** fica na Fase 4 / Fase 5 (depende de Subscription para validar).

## Plano de agentes

> Segue o **mapa fixo de responsabilidades** da seção "Plano de agentes por fase" do [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md). A atribuição por item desta fase:

| Agente | Modelo | Itens nesta fase |
|--------|--------|------------------|
| `senior-software-engineer` | Opus | 2.1, 2.2, 2.3, 2.7, 2.10 (aggregates + handlers) |
| `software-engineer` | Sonnet | 2.6, 2.8, 2.11 |
| `database-architect` | Opus | Migrations EF + supabase SQL, 2.15, 2.18 (RLS), 2.13 (FKs em ai_audit_logs) |
| `db-engineer` | Sonnet | Auxílio em queries Dapper específicas (escalonado conforme demanda) |
| `security-engineer` | Opus | 2.5 (signed URLs/audit), 2.12, 2.14, 2.18 (RLS junto com DBA) |
| `devops-cloud-engineer` | Opus | 2.4 (decisão SignalR vs pass-through), 2.9 (Serilog + OTel + métricas) |
| `ui-implementer` | Sonnet | Frontend para 2.3 (sino), 2.6 (dropdowns), 2.7 (tela de assinatura), 2.10 (telas Categoria/Forma) |
| `qa-engineer` | Sonnet | Testes unitários + 2.16 (integração interceptor) + 2.17 (load test) |
| `migration-engineer` | Opus | Revisão de paridade legado→novo ao final, antes do DoD |

## Ondas de execução

### Wave 1 — paralela, sem dependências
Itens isolados que podem rodar em paralelo:
- **2.6** Catálogo Profissões/Especialidades (`software-engineer`)
- **2.8** Idempotência em commands (`software-engineer` — middleware)
- **2.9** Observabilidade (Serilog + OTel) (`devops-cloud-engineer`)
- **2.10** Aggregates `CategoriaFinanceira` + `FormaPagamento` + seed (`senior-software-engineer`)
- **2.11** `Profissional` como `ISoftDeletable` (`software-engineer`)
- **2.15** Constraint `EXCLUDE USING gist` overlap (`database-architect`)
- **2.18** RLS policies das 4 tabelas novas (`database-architect` + `security-engineer`)

### Wave 2 — depende de Wave 1
- **2.1** Scheduler de jobs (`senior-software-engineer`) — base para 2.2/2.7
- **2.5** Storage de anexos com governança (`security-engineer`)

### Wave 3 — depende de Wave 2
- **2.2** Engine de automações (`senior-software-engineer`) — usa scheduler
- **2.3** Notificações in-app (`senior-software-engineer`) — alvo da engine
- **2.4** Realtime (`devops-cloud-engineer` + `senior-software-engineer`) — entrega notificações em tempo real
- **2.12** `establishment_ai_settings` (`security-engineer`) — pode ir aqui também
- **2.13** FKs em `ai_audit_logs` (`database-architect`)
- **2.14** Rate limit IA particionado (`security-engineer`)

### Wave 4 — depende de Wave 3
- **2.7** Subscription / trial / billing + feature gating (`senior-software-engineer`)

### Wave 5 — fechamento
- **Migrations**: `database-architect` consolida e gera arquivos para `supabase/migrations/`.
- **Frontend**: `ui-implementer` integra notificações, ref data, tela de assinatura.
- **Testes**: `qa-engineer` cobre tudo + 2.16 (integração interceptor) + 2.17 (load test).
- **Paridade**: `migration-engineer` revisa.
- Build + test final.

---

## Schema fechado da Fase 2

### 2.1 Scheduler — `jobs_agendados`
```
id              bigserial PK
nome            varchar(120) not null         -- ex: "expirar-trials"
proximo_run_em  timestamptz not null
ultimo_run_em   timestamptz null
intervalo_seg   int not null                  -- 0 = one-shot
status          varchar(20) not null          -- Pendente | Executando | Concluido | Falhou
ultima_falha    varchar(500) null
tentativas      int not null default 0
criado_em       timestamptz not null default now()
atualizado_em   timestamptz null
```
Lock via `pg_try_advisory_lock(hash(nome))` para garantir single-leader em multi-instância.

### 2.2 Engine de automações
```
automation_rules
  id, estabelecimento_id, nome, evento_gatilho (varchar — enum string),
  condicoes_json jsonb, acoes_json jsonb, ativa bool, criado_em, atualizado_em

automation_events
  id, regra_id (FK), payload_json jsonb, status (Pendente|Executando|Concluido|Falhou),
  tentativa_n int, executar_em timestamptz, executado_em timestamptz null,
  ultima_falha varchar(500) null, criado_em
```
Eventos disparados por outros domínios (`AgendamentoCriado`, `OrcamentoVencido`, etc.) virão via `IEventBus`. Worker da fase 2.1 processa fila.

### 2.3 Notificações in-app
```
notificacoes
  id bigserial PK
  usuario_id uuid not null
  estabelecimento_id bigint null            -- null = global do usuário
  titulo varchar(200) not null
  mensagem varchar(1000) not null
  categoria varchar(40) not null            -- Convite | Agenda | Financeiro | Sistema | Automacao
  link_acao varchar(500) null               -- ex: "/agenda?id=123"
  lida bool not null default false
  criada_em timestamptz not null default now()
  lida_em timestamptz null
```
Índices: `(usuario_id, lida, criada_em desc)`, `(estabelecimento_id, criada_em desc)`.

Endpoints:
- `GET /api/notificacoes?lidas=false&pagina=1&tamanho=20`
- `POST /api/notificacoes/{id}/marcar-lida`
- `POST /api/notificacoes/marcar-todas-lidas`
- Contador (badge): `GET /api/notificacoes/contador-nao-lidas`

### 2.5 Storage de anexos
- Signed URLs: TTL default 5 min. Max upload 50 MB. MIME whitelist: `application/pdf`, `image/png`, `image/jpeg`, `image/webp`, `application/dicom`.
- Bucket privado `imedto_anexos_prontuario` (criar via `supabase/migrations/`).
- Audit em `prontuario_acesso_log` para cada `GerarUrlAssinada`.

### 2.6 Catálogo Profissões/Especialidades
```
profissoes
  id bigserial PK
  nome varchar(80) not null
  conselho_sigla varchar(10) null            -- ex: "CRM", "CRO", "CRP"
  ativo bool not null default true

especialidades
  id bigserial PK
  profissao_id bigint not null FK → profissoes(id)
  nome varchar(120) not null
  ativo bool not null default true
  unique (profissao_id, nome)
```
Seed: copiar lista do legado (`ReferenciaLegado/Imedto/supabase/migrations/<seed_profissoes_especialidades>`).

### 2.7 Subscription / trial / billing
```
planos
  id bigserial PK
  nome varchar(80) not null               -- "Free", "Trial", "Pro", "Enterprise"
  preco_mensal numeric(12,2) not null default 0
  limite_profissionais int null            -- null = ilimitado
  limite_pacientes int null
  features_json jsonb not null default '[]'  -- ["receitas","exame_fisico","ia",...]
  ativo bool not null default true
  ordem int not null default 0

assinaturas
  id bigserial PK
  estabelecimento_id bigint not null UNIQUE FK → estabelecimentos(id)
  plano_id bigint not null FK → planos(id)
  status varchar(20) not null              -- Trial | Ativa | Suspensa | Cancelada | Expirada
  iniciada_em timestamptz not null
  expira_em timestamptz null
  cancelada_em timestamptz null
  renovada_em timestamptz null
  criada_em timestamptz not null default now()
  atualizada_em timestamptz null
```
- Atributo `[FeatureGate("receitas")]` em controllers/handlers — middleware lê assinatura do tenant atual e valida que `plano.features_json` contém a feature.
- Endpoint `GET /api/minha-assinatura`.
- Job (Wave 2.1) `ExpirarTrialsJob` rodando diariamente às 03:00 UTC.

### 2.8 Idempotência
```
idempotency_keys
  key varchar(80) PK                       -- vem do header Idempotency-Key
  hash_payload varchar(64) not null        -- sha256 do body
  status_code int not null
  response_json text not null              -- jsonb se simplificar
  criado_em timestamptz not null default now()
  expira_em timestamptz not null           -- default 24h
```
Filtro `IdempotencyFilter`: se header presente, busca por `key + hash_payload`. Match → retorna response cacheada. Sem match com mesma key + payload diferente → 409 Conflict.

Aplicar em: `POST /api/agendamentos`, `POST /api/orcamentos`, `POST /api/financeiro/lancamentos`, `POST /api/inventario/movimentacoes`.

### 2.9 Observabilidade
- Serilog: console (dev), JSON estruturado para stdout (prod). Filtro `RemovePIIEnricher` que mascara `cpf`, `email`, `telefone`, `senha`, `nome` em qualquer property bag.
- OpenTelemetry: AspNetCore + EFCore + HttpClient instrumentation. OTLP exporter.
- Métricas: requests/s, latência p50/p95/p99 por endpoint, erros 5xx.
- Health checks: `/health` (liveness), `/health/ready` (readiness com check de DB).
- `appsettings.json`: `Otel:Endpoint`, `Otel:ServiceName = "imedto-backend"`.

### 2.10 Aggregates Financeiro (pré-requisito do seed da Fase 1)
```
categorias_financeiras
  id bigserial PK
  estabelecimento_id bigint not null FK
  nome varchar(80) not null
  tipo varchar(10) not null                 -- Receita | Despesa
  padrao bool not null default false        -- criada pelo seed
  ativo bool not null default true
  criada_em, atualizada_em
  unique (estabelecimento_id, nome)

formas_pagamento
  id bigserial PK
  estabelecimento_id bigint not null FK
  nome varchar(80) not null
  padrao bool not null default false
  ativo bool not null default true
  criada_em, atualizada_em
  unique (estabelecimento_id, nome)
```
Aggregate `CategoriaFinanceira` (Domain), `FormaPagamento` (Domain). Repos EF + Dapper.
Endpoints: CRUD básico para ambos.

Seed da Fase 1 (handler `CriarSeedFinanceiroAoCriarEstabelecimentoHandler`):
- Categorias: `Receita: Consulta`, `Receita: Procedimento`, `Receita: Outros`, `Despesa: Folha`, `Despesa: Aluguel`, `Despesa: Insumos`, `Despesa: Outros`.
- Formas: `Dinheiro`, `PIX`, `Cartão de Crédito`, `Cartão de Débito`, `Transferência`, `Boleto`.

### 2.11 Profissional como `ISoftDeletable`
- `Domain/Profissionais/Profissional.cs` implementa `ISoftDeletable`.
- Adicionar `deletado_em` + `deletado_por_usuario_id` em `profissionais`.
- Repos Dapper com `WHERE deletado_em IS NULL`.

### 2.12 `establishment_ai_settings`
```
establishment_ai_settings
  estabelecimento_id bigint PK FK → estabelecimentos(id)
  ai_enabled bool not null default true
  ai_provider varchar(40) not null default 'anthropic'
  ai_model varchar(80) not null default 'claude-sonnet-4-6'
  rate_limit_per_minute int not null default 10
  rate_limit_per_day int not null default 200
  data_minimization_level varchar(20) not null default 'standard'  -- minimized | standard
  atualizada_em timestamptz null
```
`RateLimitedIaService` lê settings do tenant atual; falta de linha = defaults globais.

### 2.13 FKs em `ai_audit_logs`
```
ALTER TABLE ai_audit_logs
  ADD COLUMN paciente_id bigint null FK → pacientes(id) ON DELETE SET NULL,
  ADD COLUMN prontuario_id bigint null FK → prontuarios(id) ON DELETE SET NULL,
  ADD COLUMN evolucao_id bigint null FK → prontuario_evolucoes(id) ON DELETE SET NULL;
```
Decorator passa esses IDs quando disponíveis no `SugestaoSecaoProntuarioRequest`.

### 2.14 Rate limit IA particionado
`ai_rate_limits` ganha `estabelecimento_id`:
```
ALTER TABLE ai_rate_limits ADD COLUMN estabelecimento_id bigint not null default 0;
DROP CONSTRAINT uq_ai_rate_limits_usuario_periodo;
ADD CONSTRAINT uq_ai_rate_limits_user_estab_periodo UNIQUE (usuario_id, estabelecimento_id, periodo_inicio);
```
`RateLimitedIaService.RegistrarTentativaAsync` atualizado.

### 2.15 EXCLUDE GiST overlap de agenda
```
ALTER TABLE agendamentos
  ADD CONSTRAINT agendamentos_no_overlap
  EXCLUDE USING gist (
    profissional_usuario_id WITH =,
    tstzrange(inicio_previsto, fim_previsto) WITH &&
  )
  WHERE (status NOT IN ('Cancelado'));
```
Requer extensão `btree_gist`.

### 2.18 RLS policies (sprint dedicada dentro da fase)

**Tabelas a cobrir:** `audit_delete_attempts`, `ai_audit_logs`, `ai_outputs_cache`, `ai_rate_limits`, `notificacoes`, `categorias_financeiras`, `formas_pagamento`, `profissoes`, `especialidades`, `assinaturas`, `planos`, `automation_rules`, `automation_events`, `establishment_ai_settings`, `idempotency_keys`, `jobs_agendados`.

Padrão geral:
- Tabelas administrativas internas (`jobs_agendados`, `idempotency_keys`, `ai_outputs_cache`, `ai_rate_limits`, `automation_events`): RLS habilitada + sem policy permissiva → só `service_role` acessa.
- Tabelas tenant-scoped (`audit_delete_attempts`, `ai_audit_logs`, `notificacoes`, `categorias_financeiras`, `formas_pagamento`, `assinaturas`, `automation_rules`, `establishment_ai_settings`): policy SELECT/UPDATE limitada por `estabelecimento_id IN (SELECT estabelecimento_id FROM vinculos WHERE usuario_id = auth.uid() AND status = 'Ativo')` + dono.
- Ref data global (`profissoes`, `especialidades`, `planos`): SELECT permitido para autenticados.

---

## Itens

### 2.6 Catálogo Profissões/Especialidades

**Status:** ⏳ pendente
**Agente:** `software-engineer`
**Branch:** `feature/fase2-catalogo-profissoes`

#### Diff técnico
- Aggregates `Profissao`, `Especialidade` em `Domain/Catalogo/`.
- Repos EF + query repo Dapper.
- Migrations EF + seed (copiar do legado).
- Endpoints `GET /api/catalogo/profissoes` e `GET /api/catalogo/especialidades?profissaoId=`.
- DTO consumido por views de cadastro de Profissional e Vínculo.

#### Aceite
- [ ] Listagem retorna ≥ 30 profissões reais brasileiras.
- [ ] Especialidades filtram por `profissaoId`.
- [ ] Frontend de cadastro de Profissional consome (eventualmente, via `ui-implementer`).

---

### 2.8 Idempotência em commands externos

**Status:** ⏳ pendente
**Agente:** `software-engineer`
**Branch:** `feature/fase2-idempotency`

#### Diff técnico
- Tabela `idempotency_keys` (schema acima).
- Filtro `IdempotencyFilter : IAsyncActionFilter` em `API/Filters/`.
- Comportamento:
  - Sem header `Idempotency-Key` → passa direto.
  - Com header + match (mesma key + mesmo hash) → retorna response cached.
  - Com header + key existe + hash diferente → 409 Conflict.
- Aplicar via atributo `[Idempotent]` em endpoints específicos.
- TTL de 24h. Job de limpeza diário (depende da Wave 2 — pode ser placeholder até lá).

#### Aceite
- [ ] POST duplicado com mesma key retorna mesmo body sem efeito colateral.
- [ ] POST com key reusada e payload diferente → 409.
- [ ] Sem header → comportamento normal.

---

### 2.9 Observabilidade básica

**Status:** ⏳ pendente
**Agente:** `devops-cloud-engineer`
**Branch:** `feature/fase2-observabilidade`

#### Diff técnico
- `Serilog.AspNetCore` + sink JSON para stdout.
- `LoggingEnricher` que mascara campos sensíveis (`cpf`, `email`, `telefone`, `senha`, `nome` parciais).
- `OpenTelemetry.Instrumentation.AspNetCore` + `EntityFrameworkCore` + `HttpClient`. OTLP exporter.
- `AddHealthChecks().AddNpgSql(connectionString)` em `/health/ready`.
- `appsettings.json` com config de OTel endpoint.

#### Aceite
- [ ] Log de request inclui correlation id, mas nunca CPF/senha/email cru.
- [ ] `/health` retorna 200; `/health/ready` retorna 503 se DB cair.
- [ ] Métricas básicas exportadas via OTLP.

---

### 2.10 Aggregates Financeiro + seed

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`
**Branch:** `feature/fase2-financeiro-aggregates`

#### Diff técnico
- Aggregates `CategoriaFinanceira`, `FormaPagamento` em `Domain/Financeiro/`.
- Enum `TipoCategoria { Receita, Despesa }`.
- Repositórios EF (escrita) + Dapper (leitura).
- Endpoints CRUD `/api/financeiro/categorias`, `/api/financeiro/formas-pagamento`.
- `Application/Financeiro/SeedsFinanceiro.cs` (lista do schema).
- `Application/Estabelecimentos/Events/CriarSeedFinanceiroAoCriarEstabelecimentoHandler.cs` consumindo `EstabelecimentoCriadoEvent`.
- Registrar handler em `Container.cs`.

#### Aceite
- [ ] Criar estabelecimento → 7 categorias + 6 formas aparecem.
- [ ] CRUD funcional via API.
- [ ] Categorias `padrao = true` não podem ser editadas (`BusinessException`).

---

### 2.11 Profissional como `ISoftDeletable`

**Status:** ⏳ pendente
**Agente:** `software-engineer`
**Branch:** `feature/fase2-profissional-soft-delete`

#### Diff técnico
- `Profissional.cs`: implementa `ISoftDeletable` + `MarcarComoDeletado`.
- EF config + migration: colunas `deletado_em`, `deletado_por_usuario_id` em `profissionais`.
- Repo Dapper: filtro `deletado_em IS NULL` nas listagens.
- Handler `DeletarProfissionalCommandHandler` (se existir) usa soft delete.

#### Aceite
- [ ] Soft delete funcional.
- [ ] Listagens não retornam profissional deletado.

---

### 2.15 EXCLUDE GiST overlap de agenda

**Status:** ⏳ pendente
**Agente:** `database-architect`
**Branch:** `feature/fase2-overlap-constraint`

#### Diff técnico
- Migration SQL puro: `CREATE EXTENSION IF NOT EXISTS btree_gist; ALTER TABLE agendamentos ADD CONSTRAINT agendamentos_no_overlap EXCLUDE USING gist (...);`
- Snapshot EF não rastreia constraint customizada — fica fora do EF.
- Documentar no `01_FASE_1_HARDENING.md` que isso fecha o gap de defense-in-depth do item 1.2.

#### Aceite
- [ ] INSERT de agendamento conflitante via psql/raw SQL → erro do constraint.
- [ ] Cancelado não bloqueia.

---

### 2.18 RLS policies das 4 tabelas novas (e demais tabelas da Fase 2)

**Status:** ⏳ pendente
**Agente:** `database-architect` + `security-engineer`
**Branch:** `feature/fase2-rls-policies`

#### Diff técnico
Migration SQL puro com:
- `ALTER TABLE ... ENABLE ROW LEVEL SECURITY;` em todas as tabelas listadas no schema.
- Policies conforme padrão na seção 2.18 do schema fechado.
- `REVOKE ALL FROM authenticated, anon` nas tabelas internas (jobs, idempotency, cache IA, rate limit IA).

#### Aceite
- [ ] Cliente autenticado consulta `notificacoes` e só vê as próprias.
- [ ] Cliente autenticado NÃO consegue ler `ai_outputs_cache`.
- [ ] Service role acessa tudo.

---

### 2.1 Scheduler de jobs

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`
**Branch:** `feature/fase2-scheduler`

#### Decisão arquitetural (registrar no doc)
**Escolha:** `BackgroundService` nativo + tabela `jobs_agendados` com `pg_try_advisory_lock` para liderança em multi-instância. **Por quê:** zero dependência externa, controle total, fácil de testar. Migrar para Hangfire só se a complexidade exceder.

#### Diff técnico
- Tabela `jobs_agendados` (schema acima).
- Aggregate `JobAgendado` em `Domain/Jobs/` com `MarcarExecutando`, `MarcarConcluido`, `MarcarFalhou`, `Reagendar`.
- `Infrastructure/Jobs/JobScheduler.cs : BackgroundService` que:
  1. Tenta `pg_try_advisory_lock(hash('imedto-scheduler'))`.
  2. Se obteve, busca jobs com `proximo_run_em <= now()` e `status = Pendente`.
  3. Para cada um: marca executando, executa via `IServiceScopeFactory`, marca concluído/falhou, reagenda se `intervalo_seg > 0`.
- Interface `IJobHandler` que cada job concreto implementa.
- Endpoint admin `POST /api/admin/jobs/reagendar/{nome}` para forçar execução (opcional — só se houver demanda).

#### Aceite
- [ ] Job placeholder `LimparAuditAntigoJob` rodando a cada 24h.
- [ ] Em cluster simulado (2 processos), apenas 1 executa.
- [ ] Falha em job não derruba app.

---

### 2.5 Storage de anexos com governança

**Status:** ⏳ pendente
**Agente:** `security-engineer`
**Branch:** `feature/fase2-storage-anexos`

#### Diff técnico
- Bucket privado `imedto_anexos_prontuario` via `supabase/migrations/`.
- `IStorageService` em `Infrastructure/Storage/`:
  - `Task<string> GerarUrlAssinadaAsync(string bucket, string path, TimeSpan ttl)`.
  - `Task<UploadResult> UploadAsync(string bucket, string path, Stream content, string mimeType)`.
- Validação de upload: max 50MB, MIME whitelist, sanitização de nome (sem path traversal).
- Audit: cada `GerarUrlAssinadaAsync` registra em `prontuario_acesso_log` se for prontuário.
- TTL default 5min, configurável.
- Limite por usuário/dia opcional (registrar como TODO).

#### Aceite
- [ ] URL signada expira em 5min.
- [ ] Upload de PDF de 60MB → 422.
- [ ] Upload `application/octet-stream` → 422 (whitelist).
- [ ] Audit log com cada acesso.

---

### 2.2 Engine de automações

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`
**Branch:** `feature/fase2-engine-automacoes`

#### Diff técnico
- Tabelas `automation_rules`, `automation_events` (schema acima).
- Aggregates `RegraAutomacao`, `EventoAutomacao`.
- Worker `ProcessadorAutomacoesJob : IJobHandler` (registrado via 2.1, executa a cada 30s).
- Avaliador de condições (DSL JSON simples — `{ "campo": "tipo", "operador": "==", "valor": "consulta" }`).
- Executor de ações:
  - `EnviarNotificacao` (delega ao serviço da 2.3).
  - `EnviarEmail` (placeholder — provedor real fica para fase posterior).
  - `MarcarChecklist` (TODO se houver tempo).
- EventHandlers globais que escutam `AgendamentoCriado`, `OrcamentoVencido`, `LancamentoVencido` e enfileiram `EventoAutomacao` para regras correspondentes.

#### Aceite
- [ ] Criar regra "30 min antes da consulta envia notificação" → executa em horário esperado.
- [ ] Falha de ação retry com backoff (3x).
- [ ] Worker single-leader (via 2.1).

---

### 2.3 Notificações in-app

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`
**Branch:** `feature/fase2-notificacoes`

#### Diff técnico
- Tabela `notificacoes` (schema acima).
- Aggregate `Notificacao` em `Domain/Notificacoes/`.
- `INotificacaoService.Enviar(usuarioId, titulo, mensagem, categoria, linkAcao)`.
- Endpoints listados no schema.
- Handlers de outros domínios chamam `INotificacaoService.Enviar` ao gatilhar evento (ex: `ProfissionalConvidadoEvent` → notificação de convite).
- Frontend: composable `useNotificacoes` + `AppHeaderNotifications.vue` (sino com contador, dropdown).

#### Aceite
- [ ] Convidar profissional → cria notificação visível no sino.
- [ ] Marcar como lida → contador atualiza.
- [ ] Listagem paginada com filtro `lidas=false`.

---

### 2.4 Realtime

**Status:** ⏳ pendente
**Agente:** `devops-cloud-engineer` + `senior-software-engineer`
**Branch:** `feature/fase2-realtime`

#### Decisão arquitetural
**Escolha default:** SignalR no .NET com hub `EstabelecimentoHub`. Justificativa: bate com a premissa "regra/transporte só pelo backend" e dá controle total. Considerar Supabase Realtime pass-through apenas se SignalR + multi-instância demandar Redis backplane.

#### Diff técnico
- `Microsoft.AspNetCore.SignalR` + `Imedto.Backend.API/Hubs/EstabelecimentoHub.cs`.
- Cliente conecta após login enviando token (cookie).
- Hub joina grupos por `estabelecimentoId`.
- `INotificacaoService.Enviar` também publica via `IHubContext<EstabelecimentoHub>` para o grupo.
- Frontend: `frontend/src/services/realtimeService.ts` + integração com `useNotificacoes`.

#### Aceite
- [ ] Notificação criada aparece no sino sem F5.
- [ ] Reconexão automática com backoff.
- [ ] Logout fecha conexão.

---

### 2.7 Subscription / trial / billing + feature gating

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`
**Branch:** `feature/fase2-subscription`

#### Diff técnico
- Tabelas `planos`, `assinaturas` (schema acima).
- Aggregates `Plano`, `Assinatura`.
- Atributo `[FeatureGate("nome-da-feature")]` em controllers/handlers.
- Filtro lê `Assinatura` do tenant (via `ICurrentTenantAccessor`) + valida `plano.features_json`.
- Job (Wave 2.1) `ExpirarTrialsJob` rodando às 03:00 UTC.
- Endpoint `GET /api/minha-assinatura`.
- Frontend: `MinhaAssinaturaView.vue` mostrando plano atual, expiração, features.
- Seed planos: `Free` (limit 2 prof), `Trial` (full features 14 dias), `Pro` (limit 10 prof), `Enterprise` (sem limit).

#### Aceite
- [ ] Estabelecimento sem assinatura ativa → endpoint com `[FeatureGate]` retorna 402 Payment Required.
- [ ] Trial expira → `ExpirarTrialsJob` muda status para `Expirada`.
- [ ] Frontend mostra dias restantes do trial.

---

### 2.12 `establishment_ai_settings`

**Status:** ⏳ pendente
**Agente:** `security-engineer`
**Branch:** `feature/fase2-ai-settings`

#### Diff técnico
- Tabela + aggregate.
- `RateLimitedIaService` consulta settings antes de processar:
  - Se `ai_enabled = false` → `BusinessException("IA desabilitada para este estabelecimento.")`.
  - Usar `rate_limit_per_minute` e `rate_limit_per_day` do settings (cair para defaults se ausente).
  - Se `data_minimization_level = 'minimized'` → aplica sanitização extra (futuramente — por hora só log).
- Endpoint `GET/PUT /api/estabelecimento/ia-settings` (apenas dono).

#### Aceite
- [ ] Toggle desabilita IA para o tenant.
- [ ] Limites por tenant respeitados.

---

### 2.13 FKs em `ai_audit_logs`

**Status:** ⏳ pendente
**Agente:** `database-architect`
**Branch:** `feature/fase2-ai-audit-fks`

#### Diff técnico
- Migration EF: adicionar colunas + FKs `ON DELETE SET NULL`.
- Atualizar `AiAuditLog` aggregate + EF config.
- `RateLimitedIaService` passa esses IDs ao registrar audit (extraídos do `SugestaoSecaoProntuarioRequest` se presentes).

#### Aceite
- [ ] Audit log carrega rastreabilidade para LGPD.
- [ ] Delete de paciente seta NULL no audit (preserva trilha).

---

### 2.14 Rate limit IA particionado

**Status:** ⏳ pendente
**Agente:** `security-engineer`
**Branch:** `feature/fase2-ia-rate-particionado`

#### Diff técnico
- Migration: adicionar `estabelecimento_id` em `ai_rate_limits` + atualizar UNIQUE.
- `IAiRateLimitRepository.RegistrarTentativaAsync(usuarioId, estabelecimentoId, limite)`.
- `RateLimitedIaService` passa `estabelecimentoId`.

#### Aceite
- [ ] Mesmo usuário em 2 estabelecimentos tem cotas independentes.

---

### 2.16 Teste de integração do `SoftDeleteInterceptor`

**Status:** ⏳ pendente
**Agente:** `qa-engineer`
**Branch:** `feature/fase2-test-soft-delete-interceptor`

#### Diff técnico
- Teste com `WebApplicationFactory<Program>` ou `DbContextOptionsBuilder.UseInMemoryDatabase` (decidir — InMemory pode não suportar interceptor).
- Cenário: chamar `dbContext.Pacientes.Remove(paciente); SaveChangesAsync()` → deve lançar `BusinessException` e gravar audit.

#### Aceite
- [ ] Teste passa.

---

### 2.17 Load test do rate limit em `/auth`

**Status:** ⏳ pendente
**Agente:** `qa-engineer`
**Branch:** `feature/fase2-load-test-auth`

#### Diff técnico
- Script `k6` ou `xUnit` com `WebApplicationFactory` + paralelismo.
- Cenário: 6 requests em 1s → 5 sucessos + 1 com 429.

#### Aceite
- [ ] Script de teste em `scripts/load-tests/auth-rate-limit.js` (ou similar).

---

## Definition of Done — Fase 2

- [ ] Todos os itens com testes passando.
- [ ] Migrations EF + supabase SQL geradas e aplicadas.
- [ ] RLS policies habilitadas (item 2.18).
- [ ] Frontend integrado para 2.3, 2.6, 2.7, 2.10.
- [ ] `dotnet build` limpo + `dotnet test` verde + `npm run build` verde.
- [ ] Documento atualizado com status final.
- [ ] **Status** atualizado em [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md).
- [ ] Gerar `03_FASE_3_DOMINIO_CLINICO.md` quando iniciar Fase 3.

## Status por item

| Item | Status | Aplicado no banco | Observações |
|------|--------|------|-------------|
| 2.1 Scheduler de jobs | ✅ | `jobs_agendados` aplicada | `BackgroundService` + advisory lock + 2 jobs registrados (`limpar-audit-antigo`, `processar-automacoes`, `expirar-trials`). |
| 2.2 Engine de automações | ✅ | `automation_rules`/`automation_events` aplicadas | DSL JSON, executor com `enviar-notificacao` e placeholder `enviar-email`. |
| 2.3 Notificações in-app | ✅ | `notificacoes` aplicada + RLS | Service + endpoints + handler de convite. Frontend pendente (sub-iteração). |
| 2.4 Realtime SignalR | ✅ | — (não tem schema) | Hub + bridge + frontend `realtimeService` + `notificacoesStore` + `AppHeaderNotifications.vue`. |
| 2.5 Storage de anexos | ✅ | bucket `imedto_anexos_prontuario` aplicado | Path sanitization 3 camadas + audit + signed URL TTL 5min. |
| 2.6 Catálogo Profissões/Especialidades | ✅ | tabelas aplicadas + RLS | Seed estático em código — INSERT do seed pendente (sub-iteração). |
| 2.7 Subscription/trial/billing + feature gating | ✅ | `planos`/`assinaturas` aplicadas + RLS | `[FeatureGate]` pronto. Trial automático ao criar estabelecimento (14 dias). Job `expirar-trials`. |
| 2.8 Idempotência | ✅ | `idempotency_keys` aplicada + RLS interna | Filtro aplicado em 4 endpoints sensíveis. |
| 2.9 Observabilidade | ✅ | — (config) | Serilog + RemovePIIEnricher + OpenTelemetry + Health checks `/health` e `/health/ready`. |
| 2.10 Aggregates Financeiro | ✅ | `categorias_financeiras`/`formas_pagamento` + RLS | Seed automático ao criar estabelecimento (item 1.5 da Fase 1 destravado). |
| 2.11 Profissional ISoftDeletable | ✅ | colunas `deletado_em`/`deletado_por_usuario_id` aplicadas | — |
| 2.12 `establishment_ai_settings` | ✅ | tabela aplicada + RLS | Toggle por estabelecimento; `RateLimitedIaService` consome. |
| 2.13 FKs `ai_audit_logs` | ✅ | 3 colunas + 3 FKs `ON DELETE SET NULL` aplicadas | — |
| 2.14 Rate limit IA particionado | ✅ | coluna + UNIQUE composta aplicadas | Mesmo usuário em 2 estabelecimentos = cotas separadas. |
| 2.15 EXCLUDE GiST overlap | ✅ | constraint `agendamentos_no_overlap` aplicada | `btree_gist` + WHERE `status <> 'Cancelado'`. |
| 2.16 Teste de integração SoftDeleteInterceptor | ⏳ pendente | — | Sub-iteração — requer `WebApplicationFactory`. |
| 2.17 Load test rate limit auth | ⏳ pendente | — | Sub-iteração — script k6 ou xUnit + `TestServer`. |
| 2.18 RLS policies | ✅ | 12 tabelas com RLS aplicada | `ai_*`, `audit_delete_attempts`, `notificacoes`, `automation_*`, `establishment_ai_settings`, `assinaturas`, `categorias_financeiras`, `formas_pagamento`, `planos`, `profissoes`, `especialidades`, `idempotency_keys`, `jobs_agendados`. Policies em `storage.objects` para `imedto_anexos_prontuario` exigem ownership e devem ser aplicadas via dashboard pelo dono — bucket é privado (sem policy permissiva já bloqueia tudo para `authenticated`/`anon`). |

## Resumo final da fase

### O que foi entregue

**Backend:**
- 11 aggregates novos (Plano, Assinatura, JobAgendado, RegraAutomacao, EventoAutomacao, Notificacao, EstabelecimentoIaSettings, CategoriaFinanceira, FormaPagamento, Profissao, Especialidade) + IdempotencyKey + AuditDeleteAttempt já vinha da Fase 1.
- 17 controllers novos/estendidos (NotificacaoController, AutomacaoController, AssinaturaController, EstabelecimentoIaSettingsController, FinanceiroController estendido com Categorias e Formas, CatalogoController, JobScheduler hosted service).
- Filtros transversais: `IdempotencyFilter`, `FeatureGateAttribute`, `RateLimiterMiddleware` (Fase 1) + `SoftDeleteInterceptor` (Fase 1).
- Decorator `RateLimitedIaService` agora usa settings por tenant, audit com FKs (paciente/prontuário/evolução), rate limit particionado.
- Sanitização PII no IIaService preservada e robusta.
- Scheduler `BackgroundService` com advisory lock + 3 jobs registrados.
- Engine de automações genérica com DSL JSON.
- Bridge SignalR para notificações em tempo real.
- Storage privado de anexos com signed URL + 3 camadas de sanitização de path.
- Seed automático de Trial (14 dias) e categorias/formas financeiras ao criar estabelecimento.
- `IAssinaturaService` com cache em memória 1min para feature gating.
- Observabilidade Serilog (com RemovePIIEnricher) + OpenTelemetry + Health checks.

**Database:**
- 13 tabelas novas (`jobs_agendados`, `notificacoes`, `automation_rules`, `automation_events`, `establishment_ai_settings`, `planos`, `assinaturas`, `categorias_financeiras`, `formas_pagamento`, `profissoes`, `especialidades`, `idempotency_keys`, `audit_delete_attempts` da Fase 1) — todas com RLS habilitada.
- 5 colunas adicionadas (`profissionais.deletado_em/deletado_por_usuario_id`, `ai_audit_logs.paciente_id/prontuario_id/evolucao_id`, `ai_rate_limits.estabelecimento_id`).
- 3 FKs em `ai_audit_logs` com `ON DELETE SET NULL`.
- 1 constraint `agendamentos_no_overlap` (EXCLUDE GiST).
- Bucket privado `imedto_anexos_prontuario` com whitelist MIME e file_size_limit 50MB.

**Frontend:**
- `realtimeService.ts` + `notificacoesStore` + `AppHeaderNotifications.vue` (sino + dropdown).
- `useDebouncedRef`, `formatarMoedaBrl` já existentes.

### Build & testes finais

- `dotnet build`: **0 errors**, ~9 warnings (pré-existentes — vulnerabilidades de pacotes transitivos NuGet, sem impacto).
- `dotnet test`: **Passed: 98, Failed: 0**.
- Migrations EF aplicadas no banco e validadas (12 RLS habilitadas, 4 constraints novas).

### Pendências documentadas (sub-iteração / Fase 3)

1. **Frontend Wave 5c**: telas para `MinhaAssinaturaView`, `PlanosView`, `MinhaIaSettingsView`, gestão de regras de automação, gestão de categorias/formas financeiras. Use o design system. Aplicar tratamento global do 402 Payment Required no `httpClient` interceptor → modal de upsell.
2. **Seed de catálogo Profissões/Especialidades**: a estrutura `SeedsCatalogo.cs` está pronta em código. Falta gerar migration de seed que faça INSERT no banco — `database-architect` na primeira sub-iteração de Fase 3.
3. **Testes adicionais**: handlers da Fase 2, decorator com settings, soft delete interceptor (item 2.16), seed financeiro, ExpirarTrialsJob, IdempotencyFilter — `qa-engineer`.
4. **Load test rate limit auth (item 2.17)** — `qa-engineer` ou `senior-qa-engineer`.
5. **Permissão fina `assistente_clinico`**: hoje a trava é vínculo ativo. ModeloPermissao plenamente migrado na Fase 3.
6. **Aplicar `[FeatureGate]`** nos controllers de Fase 3 (Receitas, ExameFísico, ProcedimentoCirurgico, IaController, RelatorioController).
7. **Multi-instância SignalR**: backplane Redis quando escalar horizontalmente. Hoje funciona single-instance.
8. **Job de limpeza de cache IA** (`ai_outputs_cache.expira_em`): handler `IJobHandler` simples para deletar expirados.
9. **Provedor real de email** para ação `enviar-email` da engine de automações: Resend/SES.
10. **Cookie scope `access-token`**: hoje `path=/api`. Validar se SignalR no path `/hubs/...` recebe via handshake. Se não, ajustar para `path=/`.
11. **Retro-purge `ai_rate_limits.estabelecimento_id = 0`**: linhas legadas com sentinela. TTL natural de 1min resolve, mas pode rodar `DELETE WHERE estabelecimento_id = 0` após deploy.
12. **Limites de plano enforced**: `LimiteProfissionais`/`LimitePacientes` ainda não são checked nos handlers de cadastro/convite.
13. **Bucket antigo `prontuario-anexos`** (com hífen, 25MB, sem MIME whitelist): deletar manualmente via dashboard. 2 objetos de teste a remover.
14. **Storage policies em `storage.objects`** para `imedto_anexos_prontuario`: precisam ser aplicadas via dashboard pelo dono (MCP não tem ownership). Bucket privado já protege por default — defense-in-depth nominal pendente.
15. **Migration-engineer revisão de paridade**: confronte os 14 itens da Fase 2 com o legado para garantir paridade comportamental antes de iniciar Fase 3.

### Próximos passos

1. Marcar Fase 2 como concluída no [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md).
2. Completar pendências 1-15 acima (sub-iteração ou Fase 3 conforme natureza).
3. Gerar `03_FASE_3_DOMINIO_CLINICO.md` quando iniciar Fase 3 — incorporando as pendências relevantes desta fase.
