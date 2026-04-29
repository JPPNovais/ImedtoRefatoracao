# Fase 3 — Domínios clínicos pesados

**Status geral:** ✅ concluída (backend + banco + paridade aplicada — frontend Fase 3 em paralelo via Wave Frontend)
**Iniciada em:** 2026-04-29
**Concluída em:** 2026-04-29 (incluindo sub-iteração de paridade dos 8 bloqueadores)

> **Objetivo:** entregar as features clínicas que diferenciam o Imedto. Cada uma é isolada e depende só de Prontuário (já existente) + Plataforma (Fase 2).
>
> **Pré-requisitos:** Fases 1 ✅ e 2 ✅ concluídas e aplicadas no banco.
>
> **Referência:** [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md), [02_FASE_2_PLATAFORMA.md](02_FASE_2_PLATAFORMA.md).

## Escopo total da fase

### Itens do plano original

| # | Item | Descrição |
|---|------|-----------|
| 3.1 | **Receitas / prescrições** | Aggregate Receita + ItemReceita, fábricas, configuração por estabelecimento, medicamentos favoritos, PDF, audit LGPD. |
| 3.2 | **Exame físico (body map + regiões)** | Aggregate ExameFisico + RegiaoExameFisico, integrado ao Prontuário. Frontend com `BodyMapSvg` portado. |
| 3.3.A | **Procedimentos cirúrgicos** | Aggregate ProcedimentoCirurgico (DescricaoCirurgica, EquipeCirurgica, FichaAnestesica, EvolucaoPosOperatoria). |
| 3.3.B | **Orçamento completo** | Estender Orçamento com Equipe, Implantes, FormasPagamento (multi), ValorPorProfissional, ConfigPagamento, ProcedimentoCirurgicoId. Migrar lógica do RPC `save_orcamento_completo` para handler transacional. |

### Pendências da Fase 2 incorporadas

| # | Item | Origem |
|---|------|--------|
| 3.4 | **Permissão fina `assistente_clinico`** | TODO da Fase 1 (item 1.7) — agora o `ModeloPermissao` está plenamente migrado. |
| 3.5 | **Aplicar `[FeatureGate]`** nos novos controllers (Receitas, ExameFísico, ProcedimentoCirurgico, OrcamentoCompleto premium, IaController) | Item 2.7 da Fase 2. |
| 3.6 | **Seed de catálogo Profissões/Especialidades** (INSERT no banco — estrutura já em código) | Item 2.6 da Fase 2 (parcial). |
| 3.7 | **Limites de plano enforced** (`LimiteProfissionais`, `LimitePacientes`) nos handlers de `ConvidarProfissional` e `CadastrarPaciente` | Item 2.7 da Fase 2 (parcial). |
| 3.8 | **Job de limpeza de cache IA** (`ai_outputs_cache.expira_em < now()`) | Item 1.7 / 2.1 da Fase 2. |
| 3.9 | **Testes adicionais Fase 2** (handlers de Notificacao, Automacao, Assinatura, Idempotency, ExpirarTrialsJob, IdempotencyFilter, decorator com settings, soft delete interceptor item 2.16) | Item 2.16 da Fase 2. |
| 3.10 | **Frontend Fase 2** (telas de assinatura, IA settings, regras de automação, financeiro categorias/formas, sino de notificações com lista paginada) | Item 2.4-2.10 da Fase 2. |

> **Postergados explicitamente para Fase 4 ou 5:**
> - Subscription/billing real (gateway de pagamento) → Fase 4 (gaps secundários).
> - Multi-instância SignalR (Redis backplane) → Fase 4.
> - Estratégia de admin-reset-estabelecimento → Fase 4 / Fase 5.
> - 9 RPCs de relatórios consolidados → Fase 4.
> - Anonimização LGPD → Fase 4.
> - Provedor de email real (Resend/SES) → Fase 4 ou 5.
> - Bucket antigo `prontuario-anexos` cleanup → operacional manual.

## Plano de agentes

> Segue o **mapa fixo de responsabilidades** do [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md). Atribuição por item desta fase:

| Agente | Modelo | Itens nesta fase |
|--------|--------|------------------|
| `senior-software-engineer` | Opus | 3.1, 3.2, 3.3.A, 3.3.B (aggregates DDD pesados, orçamento transacional, receita/exame integrados ao prontuário) |
| `software-engineer` | Sonnet | 3.7 (limites de plano nos handlers), 3.6 (seed catálogo) |
| `database-architect` | Opus | Migrations EF + supabase SQL para 3.1/3.2/3.3, RLS de todas as novas tabelas, INSERT seed catálogo |
| `security-engineer` | Opus | 3.4 (permissão fina `assistente_clinico` + `has_assistente_clinico_permission`), 3.5 (aplicar `[FeatureGate]` em controllers premium), audit LGPD em receitas (medicação é PII clínico), sanitização específica de IA para receitas |
| `ux-designer` | Opus | **Antes da implementação:** revisar fluxos clínicos do legado (BodyMapSvg, ReceitaEditor, ExameFisicoTimeline) e validar que UX do novo segue mesma fidelidade — ou propor melhorias justificadas. Output = ADR curto. |
| `ui-implementer` | Sonnet | Frontend de Receitas (ReceitaEditor + ReceitasDrawer + lista), ExameFísico (BodyMapSvg portado + Timeline), Cirurgia (4 sub-formulários), OrcamentoCompleto (multi-step), gestão de Medicamentos Favoritos. Item 3.10 (telas de Fase 2 pendentes). |
| `qa-engineer` | Sonnet | Testes unitários de aggregates clínicos + handlers. Item 3.9 (cobertura adicional Fase 2). |
| `migration-engineer` | Opus | Revisão de paridade legado→novo ao final, ANTES de fechar a fase. Compara comportamento do prontuário rico no legado com o novo. |

## Ondas de execução

### Wave 1 — paralela, sem dependências
Itens isolados que destravam waves seguintes:
- **3.4** Permissão fina `assistente_clinico` (`security-engineer`)
- **3.6** Seed Profissões/Especialidades (INSERT no banco) (`database-architect`)
- **3.7** Limites de plano enforced (`software-engineer`)
- **3.8** Job de limpeza de cache IA (`software-engineer`)
- **UX revisão prévia** (`ux-designer`) — produz ADR curto sobre Receita/ExameFísico/Cirurgia (paralela; informa Wave 2/3).

### Wave 2 — depende de Wave 1
Domínios clínicos isolados (Prontuário já existe, FeatureGate já existe):
- **3.1** Receitas/prescrições (`senior-software-engineer`)
- **3.2** Exame físico (`senior-software-engineer`)

### Wave 3 — depende de Wave 2
Acoplados (Orçamento referencia Cirurgia):
- **3.3.A** Procedimentos cirúrgicos (`senior-software-engineer`)
- **3.3.B** Orçamento completo estendido (`senior-software-engineer`)

### Wave 4 — fechamento
- **Migrations consolidadas** (`database-architect`): EF + supabase SQL para 3.1/3.2/3.3, RLS.
- **Aplicar `[FeatureGate]`** (`security-engineer`): item 3.5 nos novos controllers.
- **Frontend** (`ui-implementer`): item 3.10 (Fase 2 pendentes) + telas dos 4 domínios clínicos.
- **Testes** (`qa-engineer`): cobertura aggregates clínicos + item 3.9.
- **Paridade** (`migration-engineer`): revisão final.
- **Build + test final + aplicar migrations via MCP**.

---

## Schema fechado da Fase 3

### 3.1 Receitas

**Tabelas (paridade com legado):**

```sql
receitas_configuracao_estabelecimento
  estabelecimento_id  bigint PK FK ON DELETE CASCADE
  cabecalho_html      text NULL                    -- logo, endereço, CRM/CRO
  rodape_html         text NULL                    -- assinatura, observações fixas
  modelo_padrao_id    bigint NULL                  -- modelo de papel timbrado
  emissor_padrao      varchar(80) NULL             -- "DR. JOAO" / nome formal
  atualizada_em       timestamptz NULL

receitas
  id                  bigserial PK
  prontuario_id       bigint FK NOT NULL
  paciente_id         bigint FK NOT NULL
  profissional_usuario_id uuid NOT NULL
  estabelecimento_id  bigint FK NOT NULL
  tipo                varchar(30) NOT NULL          -- Comum | Controlada | Antibiotico | Especial
  emitida_em          timestamptz NOT NULL DEFAULT now()
  validade_ate        timestamptz NULL              -- aplicável a controlada
  observacoes         varchar(2000) NULL
  status              varchar(20) NOT NULL          -- Emitida | Cancelada | Substituida
  cancelada_em        timestamptz NULL
  motivo_cancelamento varchar(500) NULL
  deletado_em         timestamptz NULL              -- ISoftDeletable
  deletado_por_usuario_id uuid NULL
  criada_em           timestamptz NOT NULL DEFAULT now()
  atualizada_em       timestamptz NULL

receita_itens
  id                  bigserial PK
  receita_id          bigint FK ON DELETE CASCADE
  ordem               int NOT NULL                 -- ordem na receita
  medicamento         varchar(200) NOT NULL
  posologia           varchar(500) NOT NULL        -- "1 comp 12/12h por 7 dias"
  quantidade          varchar(80) NULL             -- "30 comp" ou "1 frasco"
  via_administracao   varchar(40) NULL             -- Oral | Tópica | IM | EV | SC | Outra
  observacao          varchar(500) NULL

medicamentos_favoritos
  id                  bigserial PK
  profissional_usuario_id uuid NOT NULL
  estabelecimento_id  bigint FK NOT NULL
  medicamento         varchar(200) NOT NULL
  posologia           varchar(500) NULL
  via_administracao   varchar(40) NULL
  uso_count           int NOT NULL DEFAULT 0
  ultimo_uso          timestamptz NULL
  criado_em           timestamptz NOT NULL DEFAULT now()
  unique (profissional_usuario_id, estabelecimento_id, medicamento, posologia)
```

Índices: `receitas (paciente_id, emitida_em desc)`, `receitas (estabelecimento_id, profissional_usuario_id, emitida_em desc)`, `receita_itens (receita_id, ordem)`, `medicamentos_favoritos (profissional_usuario_id, uso_count desc)`.

**Permissões/RLS:** tenant-scoped, leitura para vínculos ativos + dono. Cada read registra em `prontuario_acesso_log` (LGPD — dado clínico).

### 3.2 Exame físico

```sql
exame_fisico
  id                  bigserial PK
  evolucao_id         bigint FK ON DELETE CASCADE   -- Evolucao do prontuário
  prontuario_id       bigint FK NOT NULL
  paciente_id         bigint FK NOT NULL
  realizado_em        timestamptz NOT NULL DEFAULT now()
  dados_gerais_json   jsonb NULL                    -- peso, altura, PA, FC, FR, sat, etc.
  observacoes_gerais  varchar(2000) NULL
  deletado_em         timestamptz NULL              -- ISoftDeletable
  deletado_por_usuario_id uuid NULL
  criado_em           timestamptz NOT NULL DEFAULT now()
  atualizado_em       timestamptz NULL

exame_fisico_regioes
  id                  bigserial PK
  exame_fisico_id     bigint FK ON DELETE CASCADE
  regiao_codigo       varchar(60) NOT NULL          -- "cabeca", "torax", "abdomen-quad-superior-direito"
  achados             varchar(2000) NULL
  severidade          varchar(20) NULL              -- Normal | LeveAlteracao | Alterado | Critico
  ordem               int NOT NULL DEFAULT 0
  unique (exame_fisico_id, regiao_codigo)
```

Índices: `(prontuario_id, realizado_em desc)`, `(paciente_id, realizado_em desc)`.

**Frontend:** portar o `BodyMapSvg.vue` legado (path-based SVG do corpo humano) para o design-system novo. `bodyMapPaths.ts` deve ser portado integralmente.

### 3.3.A Procedimento Cirúrgico

```sql
procedimentos_cirurgicos
  id                  bigserial PK
  paciente_id         bigint FK NOT NULL
  prontuario_id       bigint FK NOT NULL
  estabelecimento_id  bigint FK NOT NULL
  agendamento_id      bigint FK NULL                -- pode estar associado a uma agenda
  data_agendada       timestamptz NULL
  data_realizada      timestamptz NULL
  status              varchar(20) NOT NULL          -- Planejado | Confirmado | Realizado | Cancelado
  descricao_cirurgica text NULL
  ficha_anestesica    jsonb NULL                    -- estruturada (técnica, drogas, intercorrências)
  evolucao_pos_op     text NULL
  cirurgia_principal  varchar(200) NOT NULL
  cirurgia_codigo     varchar(40) NULL              -- TUSS/CBHPM
  observacoes         varchar(2000) NULL
  deletado_em         timestamptz NULL
  deletado_por_usuario_id uuid NULL
  criado_em           timestamptz NOT NULL DEFAULT now()
  atualizado_em       timestamptz NULL

equipe_cirurgica
  id                  bigserial PK
  procedimento_id     bigint FK ON DELETE CASCADE
  profissional_usuario_id uuid NOT NULL
  papel               varchar(40) NOT NULL          -- Cirurgiao | Auxiliar | Anestesista | Instrumentador | Circulante
  ordem               int NOT NULL DEFAULT 0
```

Índices: `(estabelecimento_id, data_agendada)`, `(paciente_id, data_realizada desc)`, `equipe_cirurgica (procedimento_id, papel)`.

### 3.3.B Orçamento Completo

**Estende** o `Orcamento` aggregate existente. **Não quebra** o que já existe:

```sql
-- Tabela orcamentos já existe. Adicionar colunas:
ALTER TABLE orcamentos ADD COLUMN procedimento_cirurgico_id bigint NULL FK ON DELETE SET NULL;
ALTER TABLE orcamentos ADD COLUMN tipo varchar(20) NOT NULL DEFAULT 'Simples';  -- Simples | Cirurgico
ALTER TABLE orcamentos ADD COLUMN config_pagamento_json jsonb NULL;             -- desconto, juros, condições
ALTER TABLE orcamentos ADD COLUMN custo_implantes_total numeric(12,2) NOT NULL DEFAULT 0;

-- Novas tabelas filhas
orcamento_equipe
  id, orcamento_id FK ON DELETE CASCADE,
  profissional_usuario_id uuid NOT NULL,
  papel varchar(40) NOT NULL,
  valor numeric(12,2) NOT NULL,
  ordem int

orcamento_implantes
  id, orcamento_id FK ON DELETE CASCADE,
  item_inventario_id bigint FK NULL,             -- pode ser item livre se id null
  descricao varchar(200) NOT NULL,
  quantidade decimal(12,3) NOT NULL,
  custo_unitario numeric(18,4) NOT NULL,
  custo_total numeric(18,4) NOT NULL

orcamento_formas_pagamento
  id, orcamento_id FK ON DELETE CASCADE,
  forma_pagamento_id bigint FK NOT NULL,
  valor numeric(12,2) NOT NULL,
  parcelas int NOT NULL DEFAULT 1,
  observacao varchar(200) NULL,
  ordem int
```

**Handler `CriarOrcamentoCompletoCommandHandler`** (ou estender `CriarOrcamentoCommandHandler` com payload mais rico):
- Recebe um payload aggregate (itens + equipe + implantes + formas + procedimento_id) e persiste tudo numa transação. Substitui o RPC `save_orcamento_completo` do legado (que era um upsert em SQL puro).
- Integridade: soma de `formas_pagamento.valor` deve bater com `total - desconto + juros` (validação no aggregate).
- Equipe: `valor` é a comissão do profissional (não confundir com preço do procedimento).
- Implantes: se `item_inventario_id` preenchido, valida que o item pertence ao estabelecimento; se null, permite item livre (ex: implante específico não-catalogado).

### 3.4 Permissão fina `assistente_clinico`

- `ModeloPermissao` (já existe) ganha campo `tipos_acesso_json` (já existe? confirmar) com valores possíveis incluindo `"assistente_clinico"`.
- Se modelo de permissão atual não suporta categorias de acesso fino, adicionar coluna `permissoes_extras jsonb NOT NULL DEFAULT '[]'` para guardar lista de strings tipo `["receitas", "ia", "ia_assistente_clinico"]`.
- `IModeloPermissaoRepository.UsuarioTemPermissaoExtra(usuarioId, estabelecimentoId, permissao)` — Dapper.
- `RateLimitedIaService` substitui o TODO Fase 3 por: `if (!await _modelo.UsuarioTemPermissaoExtra(usuarioId, estabelecimentoId, "ia_assistente_clinico")) throw new BusinessException("Você não tem permissão para usar o assistente de IA neste estabelecimento.");` (mantém o `PodeAtuarComoProfissional` como pré-check).

### 3.5 Aplicar `[FeatureGate]`

Aplicar nos controllers premium criados nesta fase:
- `ReceitaController` → `[FeatureGate(Features.Receitas)]`
- `ExameFisicoController` → `[FeatureGate(Features.ExameFisico)]`
- `ProcedimentoCirurgicoController` → `[FeatureGate(Features.ProcedimentosCirurgicos)]`
- `OrcamentoCompletoController` (ou método específico do `OrcamentoController`) → `[FeatureGate(Features.OrcamentoCompleto)]`
- `IaController.SugestaoSecao` → `[FeatureGate(Features.Ia)]`

### 3.6 Seed Profissões/Especialidades

Migration SQL puro INSERT idempotente das listas de `Application/Catalogo/SeedsCatalogo.cs`. Use `INSERT ... ON CONFLICT (nome) DO NOTHING`. Ordem: profissões primeiro, especialidades depois.

### 3.7 Limites de plano enforced

No `ConvidarProfissionalCommandHandler`: antes de criar vínculo, contar profissionais ativos do estabelecimento + dono = 1. Se passar de `plano.limite_profissionais`, lançar `BusinessException("Plano não permite mais profissionais. Faça upgrade.")`.

No `CadastrarPacienteCommandHandler`: antes de criar paciente, contar pacientes não-deletados. Mesma lógica.

Use `IAssinaturaService.TenantTemFeature` ou novo método `LimiteAtingido(estabelecimentoId, "profissionais")`.

### 3.8 Job de limpeza de cache IA

`Infrastructure/Jobs/Handlers/LimparCacheIaJob : IJobHandler` com `Nome = "limpar-cache-ia"`. Executa `DELETE FROM ai_outputs_cache WHERE expira_em < now()` via Dapper. Registrar em `JobsRegistrados.Todos` com intervalo de 1h.

---

## Itens detalhados

> Cada item segue o template: **Status / Agente / Branch / Diff técnico / Aceite (DoD)**.

### 3.1 Receitas

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`
**Branch:** `feature/fase3-receitas`

**Diff técnico (resumo):**
- `Domain/Receitas/`: Receita (com Itens collection), ItemReceita (entity child), Enum TipoReceita, ConfiguracaoReceitaEstabelecimento, MedicamentoFavorito.
- Fábricas: `Receita.Emitir(prontuario, paciente, profissional, estab, tipo, observacoes, itens)` — itens validados.
- Métodos: `Cancelar(motivo)`, `Substituir(novaReceitaId)`, `MarcarComoDeletado(usuarioId)` (ISoftDeletable).
- Eventos: `ReceitaEmitidaEvent` (gatilho de automação).
- LGPD: cada read passa por `IProntuarioAcessoLogService.RegistrarAsync` (já existe).
- Endpoints: `EmitirReceita`, `CancelarReceita`, `DuplicarReceita`, `ListarReceitasDoPaciente`, `ObterReceita`, `BaixarReceitaPdf` (PDF gerado server-side com `QuestPDF`).
- Medicamentos favoritos: incremento de `uso_count` ao usar; ranking automático.

**Aceite:**
- Emitir receita com 3 itens → registro persistido + audit log + evento publicado.
- Cancelar receita → status muda + motivo registrado.
- Listar do paciente → ordem desc por `emitida_em`, paginada.
- Tentativa de delete físico via API → 422 (soft delete via `MarcarComoDeletado`).
- PDF gerado com cabeçalho do estabelecimento + assinatura placeholder.

### 3.2 Exame Físico

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`
**Branch:** `feature/fase3-exame-fisico`

**Diff técnico:**
- `Domain/Prontuarios/ExameFisico.cs` (entity child da Evolucao): collection `Regioes`.
- `Domain/Prontuarios/RegiaoExameFisico.cs` (entity).
- Enum `SeveridadeExame { Normal, LeveAlteracao, Alterado, Critico }`.
- Command `RegistrarExameFisicoCommand` ou parte de `RegistrarEvolucaoCommand` (composição).
- Repos EF + Dapper (leitura inclui regiões via JOIN).
- Endpoints: `RegistrarExameFisico`, `ObterTimeline(pacienteId, ate)`, `ObterPorEvolucao(evolucaoId)`.
- Frontend: portar `BodyMapSvg.vue` + `bodyMapPaths.ts` legado para componente `AppBodyMap.vue` no design system.

**Aceite:**
- Registrar exame com 5 regiões → persistido com ordem.
- Timeline do paciente retorna últimos 30 exames com summary de severidade.
- Frontend permite clicar na região → popup de edição.

### 3.3.A Procedimentos Cirúrgicos

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`
**Branch:** `feature/fase3-procedimentos-cirurgicos`

**Diff técnico:**
- `Domain/Cirurgias/ProcedimentoCirurgico.cs` (aggregate).
- `Domain/Cirurgias/MembroEquipeCirurgica.cs` (entity child).
- Enum `StatusProcedimento { Planejado, Confirmado, Realizado, Cancelado }`.
- Enum `PapelCirurgia { Cirurgiao, Auxiliar, Anestesista, Instrumentador, Circulante }`.
- Comandos: `Planejar`, `Confirmar`, `RegistrarRealizacao` (preenche `data_realizada` + descrição + ficha + pos-op), `Cancelar`.
- Eventos: `ProcedimentoConfirmadoEvent` (engatilha checklist e notificação).
- Validação: equipe deve ter pelo menos um cirurgião; ficha anestésica obrigatória se realizada.
- Endpoints CRUD + `ObterDoPaciente(pacienteId)` + `ListarPlanejados(estabId, dataInicio, dataFim)`.

**Aceite:**
- Planejar cirurgia → status Planejado, vinculado a prontuário/paciente.
- Confirmar → muda status, dispara evento, pode disparar notificação para equipe.
- Registrar realização sem cirurgião na equipe → `BusinessException`.

### 3.3.B Orçamento Completo

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`
**Branch:** `feature/fase3-orcamento-completo`

**Diff técnico:**
- Estender aggregate `Orcamento` existente: `Equipe`, `Implantes`, `FormasPagamento` collections + `ProcedimentoCirurgicoId`, `Tipo`, `ConfigPagamentoJson`.
- Novos child entities: `OrcamentoEquipe`, `OrcamentoImplante`, `OrcamentoFormaPagamento`.
- Métodos novos no aggregate: `AdicionarMembroEquipe`, `RemoverMembroEquipe`, `AdicionarImplante`, `AdicionarFormaPagamento`, `RecalcularTotais`.
- Validação: soma das formas de pagamento = total - desconto + juros.
- Comando `CriarOrcamentoCompletoCommand` recebe payload aggregate inteiro; handler abre transação (já é o padrão do projeto via `[UnitOfWork]`) e persiste tudo.
- Repositório query: retorna orçamento + tudo via JOIN/multi-mapping Dapper.
- Endpoints: `POST /api/orcamentos/completo`, `PUT /api/orcamentos/{id}/completo`, `GET /api/orcamentos/{id}/completo`.

**Aceite:**
- Criar orçamento cirúrgico com 3 equipe + 2 implantes + 2 formas pagamento → persistido em transação única.
- Total das formas inválido → 422 com mensagem clara.
- Get retorna estrutura completa.
- Migration migra orçamentos existentes (campos novos NULL).

### 3.4 Permissão fina `assistente_clinico`

**Status:** ⏳ pendente
**Agente:** `security-engineer`
**Branch:** `feature/fase3-permissao-assistente-clinico`

**Diff técnico:**
- Confirmar/adicionar `permissoes_extras jsonb` em `modelos_permissao_estabelecimento`.
- `IModeloPermissaoRepository.UsuarioTemPermissaoExtra(usuarioId, estabId, permissao)` — Dapper join `vinculo_profissional_estabelecimento` + `modelos_permissao_estabelecimento`.
- `RateLimitedIaService`: substituir TODO por check.
- Tela admin de permissões ganha checkbox "Assistente clínico de IA" (frontend Wave 4).

**Aceite:**
- Usuário com modelo sem permissão → 422.
- Dono sempre passa.
- Usuário com permissão → IA funciona.

### 3.5 `[FeatureGate]` em controllers premium

**Status:** ⏳ pendente (depende dos controllers existirem)
**Agente:** `security-engineer`
**Branch:** `feature/fase3-feature-gate-clinical`

**Diff técnico:** aplicar atributo nos 5 controllers/métodos listados na seção "Schema fechado 3.5".

**Aceite:**
- Estabelecimento com plano sem `receitas` → POST `/api/receitas` → 402 Payment Required.
- Estabelecimento Trial → todos os endpoints funcionam.

### 3.6 Seed Profissões/Especialidades

**Status:** ⏳ pendente
**Agente:** `database-architect`
**Branch:** `feature/fase3-seed-catalogo`

**Diff técnico:** migration SQL puro com INSERTs idempotentes a partir de `Application/Catalogo/SeedsCatalogo.cs` (que já existe).

### 3.7 Limites de plano enforced

**Status:** ⏳ pendente
**Agente:** `software-engineer`
**Branch:** `feature/fase3-limites-plano`

**Diff técnico:**
- Adicionar método `IAssinaturaService.LimiteAtingidoAsync(estabelecimentoId, "profissionais"|"pacientes")`.
- Implementação consulta plano + counts.
- Plug em `ConvidarProfissionalCommandHandler` e `CadastrarPacienteCommandHandler`.

### 3.8 Job de limpeza de cache IA

**Status:** ⏳ pendente
**Agente:** `software-engineer`
**Branch:** `feature/fase3-cleanup-ia-cache`

**Diff técnico:** `LimparCacheIaJob : IJobHandler` + entrada em `JobsRegistrados.Todos` (`limpar-cache-ia`, 3600).

### 3.9 Cobertura de testes

**Status:** ⏳ pendente
**Agente:** `qa-engineer`
**Branch:** `feature/fase3-tests`

**Escopo:** testes para todos os aggregates desta fase + handlers + decorator atualizado + IdempotencyFilter + ExpirarTrialsJob + AssinaturaService cache + soft delete interceptor (item 2.16 da Fase 2).

### 3.10 Frontend Fase 2 (pendências)

**Status:** ⏳ pendente
**Agente:** `ui-implementer`
**Branch:** `feature/fase3-frontend-fase2`

**Escopo:** views de Assinatura, IA Settings, Regras de Automação, Categorias/Formas Financeiras, sino de Notificações com lista paginada.

---

## Definition of Done — Fase 3

- [ ] 4 domínios clínicos (Receitas, ExameFísico, ProcedimentoCirurgico, OrcamentoCompleto) entregues no backend.
- [ ] Permissão fina `assistente_clinico` funcionando.
- [ ] `[FeatureGate]` aplicado nos controllers premium.
- [ ] Catálogo Profissões/Especialidades populado.
- [ ] Limites de plano enforced.
- [ ] Job de limpeza de cache IA agendado.
- [ ] Migrations EF + supabase SQL geradas e aplicadas.
- [ ] RLS habilitada em todas as tabelas novas.
- [ ] Frontend de Receitas + ExameFísico + Cirurgia + OrcamentoCompleto + pendências Fase 2.
- [ ] `dotnet build` limpo + `dotnet test` verde + `npm run build` verde.
- [ ] `migration-engineer` revisão de paridade comportamental aprovada.
- [ ] Documento atualizado com status final.
- [ ] **Status** atualizado em [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md).
- [ ] Gerar `04_FASE_4_GAPS_SECUNDARIOS.md` quando iniciar Fase 4.

## Status por item

| Item | Status | Aplicado no banco | Observações |
|------|--------|------|-------------|
| 3.1 Receitas | ✅ backend + RLS | tabelas `receitas`/`receita_itens`/`receitas_configuracao_estabelecimento`/`medicamentos_favoritos` | 4 child entities + audit LGPD em todo read. Stub de PDF (Wave futura). 3 paridades a corrigir (TipoNotificacao A/B/C, Concentracao/Duracao no item, status Rascunho). |
| 3.2 Exame Físico | ✅ backend + RLS | tabelas `exame_fisico`/`exame_fisico_regioes` | Hierarquia + lateralidade preservadas. Falta seed do catálogo de regiões legado (~50 regiões com `template_texto`/`vista`). |
| 3.3.A Procedimentos Cirúrgicos | ✅ backend + RLS | tabelas `procedimentos_cirurgicos`/`equipe_cirurgica` | 4 status, 5 papéis, ficha anestésica jsonb, audit. Schema de `FichaAnestesicaJson` por definir. |
| 3.3.B Orçamento Completo | ✅ backend + RLS | ALTER em `orcamentos` + tabelas `orcamento_equipe`/`orcamento_implantes`/`orcamento_formas_pagamento` | `ValidarIntegridade` + `[UnitOfWork]`. **3 lacunas de modelo**: orçamento legado tem multi-cirurgias, internação separada, anestesia separada — não migradas. |
| 3.4 Permissão fina assistente_clinico | ✅ backend + banco | `permissoes_extras jsonb` + backfill Admin/Médico aplicado | Test verde: 1 teste cobrindo bloqueio sem permissão. Lista de 13 permissões finas do legado pode estar parcialmente coberta — auditar. |
| 3.5 [FeatureGate] em controllers premium | ✅ | — | Aplicado em ReceitaController, ExameFisicoController, ProcedimentoCirurgicoController, IaController + 3 métodos do OrcamentoController completo. |
| 3.6 Seed Profissões/Especialidades | ✅ aplicado | 31 profissões + 196 especialidades populadas | — |
| 3.7 Limites de plano enforced | ✅ | — (lógica no AssinaturaService) | Plug em ConvidarProfissional + CadastrarPaciente. |
| 3.8 Job limpeza cache IA | ✅ | — (handler registrado em JobsRegistrados) | `limpar-cache-ia` com intervalo 1h. |
| 3.9 Testes adicionais | ✅ +106 testes | — | 99 → 205 testes. Cobertura clínica + Assinatura + Notificacao. |
| 3.10 Frontend Fase 2 (sino notif, assinatura, IA settings, financeiro categorias) | ⏳ pendente | — | Sub-iteração — `ui-implementer`. |

## Resumo final da fase

### O que foi entregue

**Backend (5 itens core):**
- 11 novos aggregates clínicos (Receita + ItemReceita, ConfiguracaoReceitaEstabelecimento, MedicamentoFavorito, ExameFisico + RegiaoExameFisico, ProcedimentoCirurgico + MembroEquipeCirurgica, Orcamento estendido + 3 child entities).
- 4 controllers novos + 1 estendido.
- `IReceitaPdfService` (stub) + `QuestPdfReceitaService` (placeholder).
- Decorator `RateLimitedIaService` agora valida permissão fina `ia_assistente_clinico` via `IModeloPermissaoRepository.UsuarioTemPermissaoExtra`.
- Limites de plano enforced em `ConvidarProfissionalCommandHandler` e `CadastrarPacienteCommandHandler` via `IAssinaturaService.LimiteAtingidoAsync`.
- `LimparCacheIaJob` registrado com 1h de intervalo.

**Database:**
- 11 tabelas novas + 4 colunas em `orcamentos` + `permissoes_extras` em `modelo_permissao_estabelecimento` + backfill Admin/Médico.
- 17 índices + UNIQUE constraints.
- RLS em 11 tabelas (8 tenant-scoped + 3 cascata via parent).
- Catálogo Profissões (31) + Especialidades (196) populado.

**Frontend:**
- (apenas backend nesta fase — frontend dos 4 domínios fica para sub-iteração / Fase 4 com `ui-implementer`)

**Testes:** +106 testes. Total **205 verdes** (de 99 → +106 cobrindo aggregates clínicos + Assinatura + Notificacao).

**Build & testes finais:**
- `dotnet build`: 0 errors.
- `dotnet test`: Passed 205 / 0 failed.

### Pendências de paridade documentadas (8 bloqueadores + 4 verificações + 4 melhorias)

A revisão pelo `migration-engineer` (em [03B_FASE_3_REVISAO_PARIDADE.md] não foi gerada — está só na resposta do agente nesta sessão) detectou que o modelo do legado é mais rico do que o spec original do doc da Fase 3. Estas pendências precisam ser endereçadas em sub-iteração (recomendado **antes** de iniciar Fase 4):

**Bloqueadores de paridade clínica (corrigir em sub-iteração da Fase 3):**
1. **Receitas** — adicionar `TipoNotificacao { A, B, C, Especial }` separado de `TipoReceita` (Portaria 344/98).
2. **Receitas** — adicionar `Concentracao`, `Duracao` em `ItemReceita` (legado tinha campos estruturados).
3. **Receitas** — adicionar `StatusReceita.Rascunho` + fábrica `IniciarRascunho` para preservar autosave do legado.
4. **Exame Físico** — recriar tabela catálogo de regiões anatômicas + seed legado (~50 regiões com `template_texto`, `vista` anterior/posterior, `nivel`).
5. **Procedimento Cirúrgico** — definir schema fechado de `FichaAnestesicaJson` e validar no aggregate.
6. **Orçamento** — multi-cirurgias por orçamento (`orcamento_cirurgias` legado), internação (`orcamento_internacao`), anestesia (`orcamento_anestesia`). Sem isso, orçamento cirúrgico real não fecha.
7. **Orçamento** — explicitar schema de `ConfigPagamentoJson` ou trazer `acrescimo_percentual`/`entrada_percentual`/`taxa_parcelas` para `OrcamentoFormaPagamento`.
8. **Permissões finas** — auditar lista de 13 permissões legadas e confirmar cobertura em `permissoes` + `permissoes_extras`.

**Verificar com produto antes de implementar:**
9. Receitas — vínculo de substituição persistido vs marca lógica.
10. Cirurgia — papéis adicionais (PrimeiroAuxiliar, SegundoAuxiliar, Residente).
11. Orçamento — modos de cálculo de comissão (hora/fixo/percentual/total).
12. Orçamento — `orcamento_extras tipo='outro'` é usado em produção?

**Postergar para Fase 4 ou 5:**
13. Receitas — regras ANVISA 30/90 dias por classe, retenção de antibiótico (RDC 471/2021).
14. Receitas — PDF real (substituir stub `QuestPdfReceitaService`).
15. TUSS/CBHPM como catálogo formal.
16. Frontend dos 4 domínios clínicos (BodyMapSvg portado, ReceitaEditor, sub-formulários de Cirurgia, Orçamento Completo wizard) + telas pendentes da Fase 2.

**Melhorias do novo modelo (aceitas, sem ação):**
- Severidade enum em Exame Físico (legado tinha texto livre).
- Validação de integridade soma de formas (legado não validava).
- Cancelamento exige motivo (legado não cobrava).
- Cabeçalho/rodapé HTML configurável por estabelecimento.
- Vínculo formal cirurgia ↔ agendamento.

### Próximos passos

1. **Sub-iteração da Fase 3** (recomendada): endereçar os 8 bloqueadores de paridade. Estimativa: 1 sprint.
2. **Frontend dos 4 domínios** (Wave de UI): pode ser feito em paralelo à sub-iteração (porting BodyMap + ReceitaEditor são os mais demorados).
3. **Marcar Fase 3 backend ✅** no [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md).
4. **Gerar `04_FASE_4_GAPS_SECUNDARIOS.md`** quando iniciar Fase 4 — incorporar as 4 pendências postergadas + frontend pendente da Fase 2 + 9 RPCs de relatórios consolidados.
