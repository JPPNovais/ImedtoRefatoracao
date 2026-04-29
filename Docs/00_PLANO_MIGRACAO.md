# Plano de Migração — Imedto Legado → Refatoração

> **Documento mestre.** Esse arquivo é a fonte da verdade do estado da migração e da ordem das fases. **Cada fase concluída deve gerar o próximo documento** (`01_FASE_1_HARDENING.md`, `02_FASE_2_PLATAFORMA.md`, etc.) com o detalhamento técnico de execução, para que a migração possa ser pausada e retomada sem perder contexto.
>
> **Última atualização:** 2026-04-28
> **Status:** Fase 1 ✅ concluída (com 9 pendências documentadas em [01_FASE_1_HARDENING.md](01_FASE_1_HARDENING.md)). Próxima ação: aplicar migrations (`supabase db push`) e gerar `02_FASE_2_PLATAFORMA.md` quando iniciar Fase 2 — escopo deve incluir as pendências desta fase.

---

## Como usar esse documento

1. Sempre comece lendo este arquivo + o documento da fase atual (ex: `01_FASE_1_HARDENING.md`).
2. **Antes de iniciar uma nova fase**, gere o `.md` correspondente nesta pasta com:
   - O escopo detalhado dos itens da fase
   - Os arquivos a criar/alterar
   - As migrations a gerar
   - Os critérios de aceite (definition of done) por item
   - Os testes que devem passar
3. Ao concluir um item dentro de uma fase, atualize o status no documento da fase (✅ feito / 🚧 em progresso / ⛔ bloqueado / ⏳ pendente) e descreva no fim do arquivo o que foi entregue.
4. Ao concluir a fase inteira, atualize a seção **Status** deste documento mestre e gere o `.md` da próxima fase.
5. Quando precisar fazer uma decisão arquitetural não prevista, registre como ADR curto dentro do documento da fase (não crie pasta separada).

## Premissas do projeto novo (o "porquê" da migração)

- **Regra de negócio só no backend .NET**: nada de RPC/trigger/edge function com lógica. RLS continua como defense-in-depth.
- **Auth Supabase via JWKS** (ES256). Cookies HttpOnly via BFF. O front nunca vê tokens.
- **DDD + CQRS**: Commands/Queries/Events separados. UoW transacional via `UnitOfWorkAttribute`.
- **EF Core para escrita** (com `AppDbContext` scoped por request) + **Dapper para leitura** (`*QueryRepository` singleton).
- **Migrations**: EF gera o C# → exporta SQL idempotente → arquivo em `supabase/migrations/` → `supabase db push`. Nunca `dotnet ef database update`.
- **LGPD é premissa de design**, não checklist final. Cada feature pensa: o dado é necessário? Está minimizado? Tem RLS? Há audit trail? Pode vazar em log/erro?
- **Reuso > duplicação**: antes de criar endpoint/query/componente, procurar o equivalente.

## Resumo do estado atual (pós-análise)

- **Domínios já migrados** (16): Agendamentos, Pacientes, Prontuários, Financeiro, Inventário, Orçamentos, Vínculos, Salas, Unidades, Estabelecimentos, Profissionais, Usuários, Automações (parcial), Dashboard, Relatórios (parcial), ModelosPermissao.
- **Frontend novo**: views correspondentes aos domínios acima, com BFF e design system iniciado.
- **Stub `Produto`**: já removido (`20260418041025_drop_produtos_stub.sql`).
- **LGPD log de acesso**: implementado e plugado nos handlers de prontuário (`ProntuarioAcessoLogService.cs`). ✅
- **IIaService**: implementação Anthropic existe (`AnthropicIaService.cs`). Falta rate limit/cache/audit.
- **Aprox. 40% das regras de negócio que vivem em SQL/edge functions** ainda não têm contraparte .NET.

---

# FASE 1 — Hardening do que já existe

> **Duração estimada:** 1–2 sprints.
> **Objetivo:** corrigir invariantes faltantes e endurecer endpoints existentes antes de adicionar features novas. Cada item é cirúrgico, mexe em código já estabilizado.
> **Pré-requisitos:** nenhum.
> **Entregável final da fase:** `01_FASE_1_HARDENING.md` consolidado com diff de cada item + status. Atualizar este arquivo quando concluir.

## 1.1 Custo médio ponderado em movimentação de estoque

**Por quê:** o legado tinha trigger `fn_movimento_estoque_set_custo` que calculava custo unitário ponderado a cada movimento de entrada/saída. Sem isso, relatórios financeiros que usam COGS retornam zero ou valor incorreto. **Bloqueia relatórios da Fase 4.**

**Onde mexer:**
- `backend/src/Services/Imedto.Backend.Domain/Inventario/ItemInventario.cs` — adicionar invariante `RegistrarMovimentacao` que recalcula `CustoMedio` (entrada: ponderada com saldo atual; saída: usa custo médio atual).
- `backend/src/Services/Imedto.Backend.Application/Inventario/Commands/RegistrarMovimentacaoEstoqueCommandHandler.cs` — chamar a nova invariante.
- Migration EF + SQL: adicionar coluna `custo_medio` (decimal 18,4) em `itens_inventario`. Adicionar `custo_unitario` (decimal 18,4) em `movimentacoes_estoque` (snapshot do custo no momento do movimento).
- Repositório de leitura: expor `CustoMedio` e `CustoUnitario` nos DTOs de listagem.

**Aceite:**
- Teste unitário cobrindo: entrada 10 un a R$5 + entrada 10 un a R$7 → saldo 20 un, custo médio R$6.
- Saída registra `custo_unitario` = custo médio atual no momento.
- Reset de estoque (saldo zero) zera o custo médio.

## 1.2 Overlap de agenda na atualização

**Por quê:** `CriarAgendamentoCommandHandler.cs:101-106` chama `_agendamentoRepo.ExisteConflito(...)`. Mas `AtualizarAgendamentoCommandHandler` não. Profissional pode ser editado para horário ocupado.

**Onde mexer:**
- `AtualizarAgendamentoCommandHandler.cs` — chamar `ExisteConflito` ignorando o próprio `AgendamentoId` que está sendo atualizado.
- `IAgendamentoRepository.ExisteConflito` — adicionar parâmetro `Guid? ignorarAgendamentoId = null`.

**Aceite:**
- Teste: editar agendamento mantendo o mesmo horário não dispara conflito.
- Teste: editar agendamento para horário ocupado por outro profissional/sala lança `BusinessException`.

## 1.3 Reativação de vínculo Inativo ao re-convidar

**Por quê:** `Vinculo.Aceitar()` ([VinculoProfissionalEstabelecimento.cs:61](../backend/src/Services/Imedto.Backend.Domain/Vinculos/VinculoProfissionalEstabelecimento.cs#L61)) só aceita `Convidado → Ativo`. O legado `aceitar_convite_profissional` reativa vínculo `Inativo`. Hoje, profissional inativado e re-convidado quebra.

**Onde mexer:**
- `ConvidarProfissionalCommandHandler.cs` — antes de criar novo vínculo, buscar vínculo existente (qualquer status) e:
  - Se `Inativo` → reativar via método novo `Vinculo.ReativarComoConvite(modeloPermissaoId)` que muda status para `Convidado` e atualiza modelo de permissão.
  - Se `Ativo` ou `Convidado` → lançar `BusinessException` (já está convidado/vinculado).

**Aceite:**
- Teste: convidar profissional já inativo recoloca em status `Convidado` com novo modelo de permissão.
- Teste: convidar profissional ativo lança erro.
- Teste: histórico de aceitação/inativação anteriores fica preservado em audit log.

## 1.4 Auditoria de tentativas de delete + proteção histórica

**Por quê:** legado tem triggers `audit_delete_attempts`, `protect_movement_history`, `protect_medical_records`. Hoje, novo permite delete livre de prontuário e movimento de estoque — risco LGPD + integridade contábil.

**Onde mexer:**
- Criar tabela `audit_delete_attempts` (id, tabela, registro_id, usuario_id, tentado_em, motivo).
- Adicionar soft delete (`deletado_em`, `deletado_por`) nos aggregates: `Prontuario`, `EvolucaoProntuario`, `MovimentacaoEstoque`, `Anexo`.
- Hard delete nesses aggregates → `BusinessException`. Tentativa registra em `audit_delete_attempts`.
- Para os demais aggregates (paciente, agendamento, profissional, etc), revisar caso a caso se devem ter soft delete.

**Aceite:**
- Tentativa de DELETE em prontuário/evolução/movimento → 422 + linha em `audit_delete_attempts`.
- Soft delete de paciente preserva histórico de prontuário visível mas paciente sai das listagens.

## 1.5 Seed de categorias financeiras + formas de pagamento padrão

**Por quê:** legado tem `criar_categorias_financeiras_padrao` e `criar_formas_pagamento_padrao` chamados ao criar estabelecimento. Hoje, estabelecimento novo nasce sem categorias — `FinanceiroView` quebra.

**Onde mexer:**
- `Imedto.Backend.Application/Estabelecimentos/Events/` — criar `CriarSeedFinanceiroAoCriarEstabelecimentoHandler` que escuta `EstabelecimentoCriadoEvent` e popula categorias + formas de pagamento.
- Lista de categorias e formas pode ser hardcoded em uma classe `SeedsFinanceiro` com método `Padrao()` (manter dados em código, não em migration — facilita ajuste).
- Registrar handler no `Container.cs`.

**Aceite:**
- Criar estabelecimento → categorias "Receita de consulta", "Receita de procedimento", "Despesa fixa", "Despesa variável" + formas "Dinheiro", "PIX", "Crédito", "Débito" populadas.
- Reconvidar handler em estabelecimentos existentes (script idempotente) — opcional na fase.

## 1.6 Rate limit em `/auth/login` e `/auth/refresh`

**Por quê:** BFF expõe diretamente esses endpoints. Sem trava é convite a brute force + envenenamento de cache de token.

**Onde mexer:**
- Adicionar `Microsoft.AspNetCore.RateLimiting` (já vem no .NET 10).
- Configurar policy `auth-login`: sliding window de 5 tentativas / 60s por IP.
- Configurar policy `auth-refresh`: sliding window de 10 / 60s por IP.
- Aplicar via atributo `[EnableRateLimiting("auth-login")]` no `AuthController`.

**Aceite:**
- 6ª tentativa de login no mesmo minuto → 429.
- Header `Retry-After` populado.
- Log estruturado **sem** vazar email/senha em caso de bloqueio.

## 1.7 Rate limit + audit + cache no `IIaService`

**Por quê:** `AnthropicIaService` está exposto sem proteção. Risco de custo descontrolado + LGPD (input clínico vai para LLM externo sem audit).

**Onde mexer:**
- Tabelas: `ai_audit_logs` (id, usuario_id, estabelecimento_id, prompt_hash, output_hash, tokens_in, tokens_out, criado_em), `ai_outputs_cache` (prompt_hash → output, ttl), `ai_rate_limits` (usuario_id, periodo, contagem).
- Decorator `RateLimitedIaService` envolvendo `AnthropicIaService` (decorator pattern via DI).
- Cache de resposta com hash do prompt + estabelecimento (TTL configurável; default 24h).
- Audit log a cada chamada (sem prompt cru — só hash + metadados).
- Permissão `assistente_clinico` no modelo de permissão (legado tinha `has_assistente_clinico_permission`).

**Aceite:**
- 11ª chamada IA por usuário em 1 minuto → 429.
- Mesmo prompt em janela de cache retorna sem chamar Anthropic.
- Tabela `ai_audit_logs` recebe linha por chamada, sem PII no payload.

---

## ✅ Definition of Done — Fase 1

Antes de gerar `02_FASE_2_PLATAFORMA.md`:

- [ ] Todos os 7 itens com testes passando (`dotnet test`).
- [ ] `dotnet build` limpo.
- [ ] Migrations criadas no padrão duplo (EF + supabase).
- [ ] `supabase db push` aplicado em dev.
- [ ] Itens 1.1, 1.2, 1.3 verificados manualmente no front.
- [ ] Documento `01_FASE_1_HARDENING.md` atualizado com status final + commits relevantes.
- [ ] Atualizar a seção **Status** no topo deste arquivo.
- [ ] Gerar `02_FASE_2_PLATAFORMA.md` com o detalhamento da Fase 2.

---

# FASE 2 — Plataforma transversal

> **Duração estimada:** 2–3 sprints.
> **Objetivo:** construir os blocos de plataforma que são pré-requisito de várias features clínicas. Construir uma vez, reusar em várias features.
> **Pré-requisitos:** Fase 1 concluída.
> **Entregável final da fase:** `02_FASE_2_PLATAFORMA.md` com cada bloco implementado + ADRs das decisões de runtime.

## 2.1 Hosted services / scheduler de jobs

**Por quê:** legado usa `pg_cron` + edge functions agendadas (`expire-trials`, lembretes). O novo precisa de runtime de jobs em .NET. Bloqueia automações (2.2), notificações (2.3) e billing (2.7).

**Decisão arquitetural a tomar (registrar no doc da fase):**
- Opção A — `BackgroundService` nativo + tabela `jobs_agendados` controlada pela própria aplicação. Simples, sem dependência externa, mas multi-instância exige lock distribuído (Postgres advisory lock).
- Opção B — Hangfire. Multi-instance native, dashboard pronto, mas adiciona dependência.
- Opção C — externalizar para AWS EventBridge / Supabase Cron chamando endpoints admin. Menor complexidade no app, mas perde transação.

**Recomendação default:** Opção A (BackgroundService) com Postgres advisory lock para liderança. Justificativa: simplicidade + zero deps. Migrar para Hangfire se ficar complexo.

**Aceite:**
- Job `LimparAuditAntigo` rodando a cada 24h (placeholder de validação).
- Apenas uma instância roda em cluster (advisory lock).
- Falha em job não derruba app.

## 2.2 Engine genérica de automações

**Por quê:** legado tem `automation-processor` + tabelas `automation_rules`/`automation_events`/`appointment_checklists` + trigger `schedule_appointment_reminders`. Novo só tem 2 commands específicos. Sem engine, Central de Pendências legado (`CentralPendencias.vue`) deixa de funcionar.

**Modelo proposto:**
- `RegraDeAutomacao` (aggregate): `Id`, `EstabelecimentoId`, `EventoGatilho` (enum: AgendamentoCriado, AgendamentoCancelado, OrcamentoVencido, etc.), `CondicoesJson`, `AcoesJson`, `Ativa`.
- `EventoDeAutomacao` (aggregate): instância de execução de uma regra. `Id`, `RegraId`, `Status` (Pendente/Executando/Concluido/Falhou), `TentativaN`, `ExecutarEm`.
- Worker `ProcessadorDeAutomacoes` (rodando via Fase 2.1) processa `EventoDeAutomacao` pendentes.
- Ações suportadas (V1): EnviarNotificacao, EnviarEmail, MarcarChecklist.

**Onde mexer:**
- `Imedto.Backend.Domain/Automacoes/` — entidades novas.
- `Imedto.Backend.Application/Automacoes/` — handlers de criação/listagem/processamento.
- Registrar handlers de eventos de outros domínios para emitir `EventoDeAutomacao` ao gatilhar.
- Migrations.

## 2.3 Notificações in-app

**Por quê:** legado tem RPC `notify_professional_invite`, tabela `notifications`, `mark_as_read`. Frontend tem ícone de sino. Sem isso, convites/lembretes ficam silenciosos.

**Modelo proposto:**
- `Notificacao` (aggregate): `Id`, `UsuarioId`, `Titulo`, `Mensagem`, `Categoria` (enum: Convite, Agenda, Financeiro, Sistema), `LinkAcao`, `Lida`, `CriadaEm`, `LidaEm`.
- Service `INotificacaoService` com `Enviar(usuarioId, titulo, mensagem, categoria, linkAcao)`.
- Handler `ConvidarProfissional` chama `INotificacaoService.Enviar(...)` ao criar vínculo.
- Endpoints: `GET /api/notificacoes` (paginado), `POST /api/notificacoes/marcar-lida/{id}`, `POST /api/notificacoes/marcar-todas-lidas`.

## 2.4 Realtime

**Por quê:** legado consome Supabase Realtime para agenda e notificações. Sem isso o novo perde reatividade — usuário precisa F5.

**Decisão arquitetural a tomar:**
- Opção A — SignalR no .NET com hubs por estabelecimento.
- Opção B — Manter Supabase Realtime via pass-through (front escuta direto, backend grava no banco).
- Opção C — Polling com SWR (mais simples, mais latência).

**Recomendação default:** Opção A (SignalR). Bate com a premissa "regra/transporte só pelo backend". Hub `EstabelecimentoHub` envia `NotificacaoCriada`, `AgendamentoAlterado`.

## 2.5 Storage de anexos com governança

**Por quê:** anexos de prontuário (PDF, imagem) hoje vão para Supabase Storage. Falta política de signed URL com TTL curto, audit no acesso, retenção.

**Onde mexer:**
- `IStorageService` em `Infrastructure/Storage/` — métodos `GerarUrlAssinada(bucket, path, ttl)` e `Upload(...)`.
- Audit: cada `GerarUrlAssinada` chama `IProntuarioAcessoLogService.RegistrarAsync` se for anexo de prontuário.
- TTL default 5min. Limites: max 50MB por anexo. MIME-type whitelist.
- Bucket `imedto_fotos` (já existe) e novo `imedto_anexos_prontuario` (privado, RLS rígida).

## 2.6 Catálogo Profissões/Especialidades

**Por quê:** ref data simples mas pré-requisito de cadastro de profissional, vínculo, relatórios. Frontend hoje deve estar com dropdown vazio.

**Onde mexer:**
- Tabelas: `profissoes` (id, nome, conselho_sigla), `especialidades` (id, profissao_id, nome).
- Migration de seed com dados reais (legado tinha lista pronta — copiar).
- Query handler `ListarProfissoesQuery`, `ListarEspecialidadesQuery`.
- DTO usado por `Profissional` e `Vinculo`.

## 2.7 Subscription / trial / billing + feature gating

**Por quê:** **antes** das features clínicas. Senão, Receitas/Exame Físico/Cirurgia ficam acessíveis em planos sem direito, e o enforcement vira retroativo (caro).

**Modelo proposto:**
- `Plano` (aggregate ref-data): `Id`, `Nome`, `LimiteProfissionais`, `LimitePacientes`, `FeaturesJson` (lista de feature flags ativas).
- `Assinatura` (aggregate): `Id`, `EstabelecimentoId`, `PlanoId`, `Status` (Trial/Ativa/Suspensa/Cancelada), `IniciadaEm`, `ExpiraEm`, `RenovadaEm`.
- Middleware `FeatureGateAttribute` que valida `Assinatura.Plano.HasFeature("receitas")` antes de executar handler.
- Job (Fase 2.1) `ExpirarTrialsJob` rodando diariamente.
- Endpoint `GET /api/minha-assinatura`.

## 2.8 Idempotência em commands externos

**Por quê:** retry de cliente lento gera duplicado em criar agendamento, criar orçamento, registrar movimentação.

**Onde mexer:**
- Header `Idempotency-Key` aceito no controller (UUID enviado pelo cliente).
- Tabela `idempotency_keys` (key, hash_payload, response_json, criada_em).
- Middleware ou filtro intercepta antes do handler. Mesma key + mesmo payload → retorna response cached.
- Aplicar em commands sensíveis: criar agendamento, criar orçamento, registrar pagamento, registrar movimentação.

## 2.9 Observabilidade básica

**Por quê:** sistema de saúde sob escala precisa de logs/métricas/traces antes de produção, não depois.

**Onde mexer:**
- `Serilog` com sinks: console (dev), arquivo rotativo (prod), filtro `RemovePIIEnricher` (lista bloqueadora: `cpf`, `email`, `senha`, `telefone`, `nome`).
- `OpenTelemetry` instrumentado em controllers + EF + Dapper. Exporter OTLP para Tempo/Jaeger.
- Métricas básicas: requests/s, latência p50/p95/p99 por endpoint, erros 5xx.
- Health checks: `/health` (liveness) e `/health/ready` (readiness com check de DB).

---

## ✅ Definition of Done — Fase 2

- [ ] Todos os 9 blocos com testes ou validação manual.
- [ ] ADRs registrados para decisões 2.1 (scheduler) e 2.4 (realtime).
- [ ] `supabase db push` aplicado.
- [ ] Frontend consumindo notificações e realtime de pelo menos um evento (ex: convite recebido aparece em tempo real).
- [ ] Documento `02_FASE_2_PLATAFORMA.md` atualizado.
- [ ] Atualizar **Status** no topo deste arquivo.
- [ ] Gerar `03_FASE_3_DOMINIO_CLINICO.md` com detalhamento da Fase 3.

---

# FASE 3 — Domínios clínicos pesados

> **Duração estimada:** 3–4 sprints.
> **Objetivo:** entregar as features clínicas que diferenciam o produto. Cada uma é isolada e depende só de Prontuário (já existente) + Plataforma (Fase 2).
> **Pré-requisitos:** Fase 2 concluída.
> **Entregável final da fase:** `03_FASE_3_DOMINIO_CLINICO.md` com cada feature documentada.

## 3.1 Receitas/prescrições

**Modelo legado:** `ReferenciaLegado/Imedto/src/modules/medical-record/components/receitas/` (ReceitaEditor, ReceitasDrawer, ReceitasTab) + `20260110120000_create_receitas_tables.sql`.

**Modelo proposto:**
- `Receita` (aggregate): `Id`, `ProntuarioId`, `PacienteId`, `ProfissionalId`, `EstabelecimentoId`, `Tipo` (Comum/Controlada/Antibiotico/Especial), `EmitidaEm`, `ValidadeAte`, `Itens` (collection).
- `ItemReceita` (entity): `Id`, `Medicamento`, `Posologia`, `Quantidade`, `ViaAdministracao`, `Observacao`.
- Commands: `EmitirReceita`, `CancelarReceita`, `DuplicarReceita`.
- Queries: `ListarReceitasDoPaciente`, `ObterReceita`, `ImprimirReceita` (gera PDF).
- Eventos: `ReceitaEmitida` (gatilha automação de envio por email se configurado).
- LGPD: cada acesso à receita registra em `prontuario_acesso_log`.

## 3.2 Exame físico (body map + regiões)

**Modelo legado:** `medical-record/components/exame-fisico/` (BodyMapSvg, RegionSelectorPopup, ExameFisicoTimeline).

**Modelo proposto:**
- `ExameFisico` (entity child de Prontuario): `Id`, `EvolucaoId`, `RealizadoEm`, `Regioes` (collection).
- `RegiaoExameFisico` (entity): `Id`, `ExameFisicoId`, `RegiaoCodigo` (cabeca, torax, abdomen-quad-superior-direito, etc), `Achados`, `Severidade`.
- Commands: `RegistrarExameFisico` como parte de `RegistrarEvolucaoCommand` (composição) ou command separado.
- Frontend reaproveita o BodyMapSvg legado (componente visual portado para o design system novo).

## 3.3 Procedimentos cirúrgicos + Orçamento completo

**Por quê fazer juntos:** o orçamento legado referencia equipe, implantes e cirurgia. Aggregate `Orcamento` atual é raso demais. Fazer separado gera retrabalho.

### 3.3.A Procedimento Cirúrgico
- `ProcedimentoCirurgico` (aggregate): `Id`, `ProntuarioId`, `PacienteId`, `EstabelecimentoId`, `DataAgendada`, `DataRealizada`, `Status`, `DescricaoCirurgica`, `EquipeCirurgica` (collection), `FichaAnestesica`, `EvolucaoPosOperatoria`.
- Components legado para portar: DescricaoCirurgica, EquipeCirurgica, FichaAnestesica, EvolucaoPosOperatoria.

### 3.3.B Orçamento Completo
- Estender `Orcamento`: `Itens`, `Equipe` (collection — profissional + papel + valor), `Implantes` (collection — item de inventário + qtd), `FormasPagamento` (collection — multi-pagamento), `ValorPorProfissional`, `ConfigPagamento`, `ProcedimentoCirurgicoId` (opcional).
- Migrar lógica do RPC `save_orcamento_completo` para handler transacional.

---

## ✅ Definition of Done — Fase 3

- [ ] Receitas, ExameFísico, ProcedimentoCirurgico e OrcamentoCompleto com testes.
- [ ] LGPD log de acesso plugado nos novos handlers de leitura.
- [ ] Frontend consumindo os 4 módulos.
- [ ] Documento `03_FASE_3_DOMINIO_CLINICO.md` atualizado.
- [ ] Gerar `04_FASE_4_GAPS_SECUNDARIOS.md`.

---

# FASE 4 — Gaps secundários

> **Duração estimada:** 1–2 sprints.

## 4.1 Relatórios SQL faltantes

**Por quê:** legado tem 9 RPCs (`rpc_report_*`). Novo tem 2. Após custo médio (1.1) + automações (2.2), os relatórios podem ser feitos.

**Estratégia:** consolidar em ~3-4 query handlers parametrizados com filtros (`tipo`, `dataInicio`, `dataFim`, `agruparPor`):
- `RelatorioFinanceiroHandler` (substitui `rpc_report_cash_flow`, `rpc_report_financial_summary`, `rpc_report_financial_by_category`).
- `RelatorioOperacionalHandler` (substitui `rpc_report_dashboard_summary`, `rpc_report_agenda_summary`, `rpc_report_inventory_summary`).
- `RelatorioPessoasHandler` (substitui `rpc_report_patients_summary`, `rpc_report_professionals_performance`).
- `RelatorioOrcamentosHandler` (substitui `rpc_report_budgets_summary`).

## 4.2 Solicitação de vínculo inversa (profissional → clínica)

**Por quê:** legado tem `solicitacao_vinculo_profissional_estabelecimento`. Hoje só clínica → profissional.

**Modelo:**
- `SolicitacaoVinculo` (aggregate): `Id`, `ProfissionalUsuarioId`, `EstabelecimentoId`, `Status` (Pendente/Aprovada/Recusada), `Mensagem`, `CriadaEm`.
- Commands: `SolicitarVinculo`, `AprovarSolicitacao`, `RecusarSolicitacao`.
- Aprovar → cria `Vinculo` com modelo de permissão default.

## 4.3 Política de retenção/anonimização LGPD

**Por quê:** Art. 16 LGPD — dado pessoal só fica armazenado pelo necessário. Sistema de saúde tem regras específicas (CFM 1.821/07 — prontuário 20 anos).

**Onde mexer:**
- Job (Fase 2.1) `AnonimizarPacientesInativosJob` rodando mensalmente.
- Anonimização: substituir nome/CPF/telefone/email por hash; manter dados clínicos com `paciente_id_anonimizado`.
- Endpoint `DELETE /api/minha-conta` (já existe? validar) executa anonimização imediata.
- Aviso LGPD: registro em `lgpd_consent` ao criar conta.

---

## ✅ Definition of Done — Fase 4

- [ ] 4 query handlers de relatório parametrizados.
- [ ] Solicitação inversa funcional com 2 telas (profissional solicita / clínica aprova).
- [ ] Job de anonimização rodando.
- [ ] Documento `04_FASE_4_GAPS_SECUNDARIOS.md` atualizado.
- [ ] Gerar `05_FASE_5_MIGRACAO_DADOS.md`.

---

# FASE 5 — Migração de dados do legado

> **Duração estimada:** projeto à parte (escopo a definir após Fase 4).
> **Objetivo:** mover os dados existentes do projeto Supabase atual (`kdoqflrmfgazdgekdbqc`) para o schema novo. Esta fase é **opcional**: pode-se também optar por estratégia greenfield (clínicas novas no novo, legadas continuam no antigo até desligamento).

**Estratégias possíveis:**
- **A. Strangler Fig**: backend novo lê e escreve do mesmo banco, schema antigo é progressivamente desligado.
- **B. ETL big-bang**: janela de manutenção, dump → transform → load.
- **C. Dual-write**: durante transição, gravar nos dois schemas. Maior complexidade, mas zero downtime.

**Decisão a tomar quando chegar o momento.** Detalhar em `05_FASE_5_MIGRACAO_DADOS.md`.

---

# Apêndices

## A. Itens descartados (não migrar)

- **Stub `Produto`**: já dropado.
- **Bloqueio "rebaixar dono via vínculo"**: modelo novo tem `Estabelecimento.DonoUsuarioId` direto, regra do legado perdeu sentido.
- **`handle_new_user` trigger**: já coberto via `AuthController.cs` chamando `CriarRegistroLocalUsuarioCommand`.
- **`criar_paciente_e_vincular`**: modelo novo tem `Paciente.EstabelecimentoId` direto (multi-tenant por FK), conceito de "vincular" não existe.
- **Recuperação de senha + email confirmation**: usar fluxo nativo do Supabase Auth — só configurar redirect URLs.
- **Imedto Admins (superadmin)**: tooling interno, resolve com `psql` + RLS no curto prazo. Construir só se houver demanda real.
- **Suporte (ticket)**: enviar email via SMTP num endpoint resolve, não precisa virar bounded context.
- **Catálogo de estoque rico** (lote/fabricante/fornecedor): só migrar se rastreio regulatório (Anvisa) for requisito confirmado pelo produto. YAGNI até lá.

## B. Decisões pendentes (perguntar ao stakeholder)

1. **Subscription/trial é prioridade alta no curto prazo?** Se não, item 2.7 desce para Fase 4.
2. **Rastreio de lote/validade no estoque é regulatório?** Define se o catálogo rico volta para o plano.
3. **Estratégia de migração de dados** (A/B/C acima)?
4. **Runtime de jobs** (BackgroundService vs Hangfire)?
5. **Estratégia de Realtime** (SignalR vs Supabase Realtime pass-through)?

## C. Convenções para os documentos de fase

Cada `0X_FASE_X_*.md` deve ter:

```markdown
# Fase X — <nome>

**Status geral:** ⏳ pendente / 🚧 em progresso / ✅ concluída
**Iniciada em:** YYYY-MM-DD
**Concluída em:** YYYY-MM-DD

## Itens

### X.1 <nome do item>
**Status:** ⏳/🚧/✅/⛔
**Branch:** feature/...
**Commits:** <hashes>

#### Diff técnico
- Arquivos criados/alterados
- Migrations geradas
- Endpoints expostos

#### Aceite (DoD)
- [ ] critério 1
- [ ] critério 2

#### Notas e decisões
- (qualquer ADR curto que valha registrar)

## Resumo final da fase
- O que foi entregue
- O que ficou postergado e por quê
- Próximos passos → linka 0(X+1)_FASE_(X+1)_*.md
```

## D. Premissas que NÃO podem ser quebradas durante a migração

1. **Regra de negócio nunca volta para o SQL** — qualquer trigger/RPC/edge function legado que sobrevive é só infra (RLS, audit, view de leitura), nunca lógica de negócio.
2. **LGPD log de acesso** continua plugado em qualquer novo handler que toque PII.
3. **Multi-tenant** por `EstabelecimentoId` é regra: nenhuma query nova esquece de filtrar.
4. **Defense-in-depth**: backend valida + RLS valida.
5. **Frontend só consome API via service** — nunca chama Supabase direto (a não ser para auth nativo).
6. **Componentização front pelo design system** — nada de scoped CSS reinventando primitivas existentes.
7. **Container `.app-page`** em toda view interna — não inventar layout próprio.
