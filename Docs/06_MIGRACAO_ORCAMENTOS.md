# Fase 6 — Migração do módulo de Orçamentos para paridade com legado

**Status geral:** ✅ **CONCLUÍDA** (Fases 6.0–6.4 entregues em 2026-04-30)
**Iniciada em:** 2026-04-30
**Concluída em:** 2026-04-30

> **Objetivo:** alinhar o módulo de Orçamentos do projeto novo com o comportamento e a estrutura do legado. O módulo no novo tem domain/banco/endpoints **funcionais mas divergentes**, e o frontend é uma reescrita simplista que não cobre o produto real (orçamento cirúrgico com cálculos baseados em tempo, settings de catálogo, conversão para procedimento cirúrgico, PDF rico).
>
> **Pré-requisitos:**
> - Fase 3 ✅ concluída (Procedimentos Cirúrgicos existem como aggregate, FK `procedimento_cirurgico_id` já está no schema de orçamentos).
> - Fase 4 ✅ concluída.
>
> **Referências:**
> - Legado: [`ReferenciaLegado/Imedto/src/modules/budgets/`](../ReferenciaLegado/Imedto/src/modules/budgets/) (~3.144 linhas TS/Vue).
> - Migrations legadas: `20251124171000_budgets.sql`, `20260209000002_orcamento_rpcs.sql`, `20260312000002_multi_formas_pagamento.sql`.
> - Novo: [`backend/src/Services/Imedto.Backend.Domain/Orcamentos/`](../backend/src/Services/Imedto.Backend.Domain/Orcamentos/), [`frontend/src/views/orcamentos/`](../frontend/src/views/orcamentos/).

---

## Diagnóstico

### Divergências (precisam ser **removidas/refatoradas**)

| # | Divergência | Onde | Por quê é problema |
|---|---|---|---|
| D1 | Dicotomia `Tipo: Simples \| Cirúrgico` no domain + endpoints `/completo` separados | `Domain/Orcamentos/Orcamento.cs`, `OrcamentoController.cs` | Invenção. Legado tem **um único tipo**; cirurgia é collection opcional. Duplica handlers/queries. |
| D2 | `ConfigPagamentoOrcamento` jsonb (`DescontoPercentual`, `JurosPercentual`, `TaxaParcela`...) | `Domain/Orcamentos/ConfigPagamentoOrcamento.cs`, schema `config_pagamento_json` | Duplica o que `OrcamentoFormaPagamento` já guarda por forma. Legado não tem isso — descontos/acréscimos vão **por forma de pagamento**. |
| D3 | Status enum `Pendente / Aprovado / Recusado / Expirado` | `Domain/Orcamentos/OrcamentoStatus.cs` | Legado é `rascunho / enviado / aprovado / recusado / cancelado / expirado`. Falta `rascunho` (estado inicial pré-envio) e `cancelado`. |
| D4 | `OrcamentosView.vue` com modal de itens livres (descrição/qtd/valor/desconto%) | `frontend/src/views/orcamentos/OrcamentosView.vue` | Não tem equivalente no legado. É um produto à parte, fora do escopo do orçamento cirúrgico. |
| D5 | `OrcamentoCompletoView.vue` single-page com colapsáveis | `frontend/src/views/orcamentos/OrcamentoCompletoView.vue` | Legado é **tabbed** (Lista / Form / Detail / Settings). UX diferente. |

### Lacunas (precisam ser **adicionadas**)

| # | Lacuna | Equivalente legado |
|---|---|---|
| L1 | Aba **Settings** com 6 catálogos | `BudgetSettingsTab.vue` + tabs internos |
| L1.1 | Catálogo de **cirurgias** (descrição, valor base, duração padrão, lista de produtos com `uso_unico`) | `orcamento_cirurgia` + `orcamento_cirurgia_produto` |
| L1.2 | **Valor por profissional** (tempoBase, valorTempoBase, tempoAdicional, valorAdicional, valorPlus, função) | `orcamento_valor_profissional` |
| L1.3 | **Configuração de estabelecimento** por tipo de internação/anestesia (local, peridural, geral, ambulatório, sem internação) | `orcamento_configuracao_estabelecimento` |
| L1.4 | **Equipes especializadas** (descrição, valor padrão) | `orcamento_equipe_especializada` |
| L1.5 | **Implantes catálogo** (descrição, custo unitário) | `orcamento_implante` |
| L1.6 | **Configurações de pagamento** (forma + acréscimo% + entrada% + isenções) | `orcamento_configuracao_pagamento` |
| L2 | Cálculo de valor profissional baseado em **tempo de cirurgia** | `useOrcamentoCalculos.ts:11-186` `calcularValorProfissional()` |
| L3 | Cálculo de valor de **local cirúrgico** em períodos | `useOrcamentoCalculos.ts` `calcularValorLocal()` |
| L4 | Cálculo de **totais por forma de pagamento** com acréscimo + entrada + parcelas | `useOrcamentoCalculos.ts` `calcularTotaisFormaPagamento()` |
| L5 | **Consolidação de produtos** vindos de múltiplas cirurgias (regra `uso_unico`) | `useOrcamentoProdutosConsolidados.ts` |
| L6 | **Conversão orçamento → procedimento cirúrgico real** (gera `ProcedimentoCirurgico` a partir do orçamento aprovado) | (campo `procedimento_cirurgico_id` já existe no schema novo, sem fluxo) |
| L7 | **Integração com prontuário** (puxar procedimentos indicados / descrição cirúrgica) | `useBudgetCreation.ts:11-40` `extractBudgetDataFromMedicalRecord()` |
| L8 | **PDF rico** com múltiplas formas de pagamento, logo do estabelecimento, tabelas detalhadas | `useBudgetPDF.ts:80-416` |
| L9 | RLS policies em todas as tabelas `orcamento*` (defense-in-depth LGPD) | (já existe no legado, falta no novo) |

---

## Plano em 5 fases (6.0 → 6.4)

> Cada fase é **isolada e mergeable**. Não rodar a fase seguinte sem confirmar a anterior em build + smoke test.

### Mapa fixo de agentes para esta fase

| Agente | Modelo | Quando usar |
|--------|--------|-------------|
| `senior-software-engineer` | Opus | Refatorar domain (Orcamento.cs, sub-aggregates novos), unificar handlers, decisões arquiteturais (jsonb vs tabela, evento de domínio para conversão) |
| `software-engineer` | Sonnet | Implementação de handlers/services/CRUDs rotineiros nos catálogos, atualização de DTOs |
| `database-architect` | Opus | Migration de drop/rename + criação de 6 tabelas de catálogo + RLS policies |
| `db-engineer` | Sonnet | Índices, tabelas de catálogo simples, RLS de defense-in-depth |
| `ux-designer` | Opus | Mapear UX de tabbed (Lista / Form / Detail / Settings), decidir quais sub-tabs ter dentro de Settings |
| `ui-implementer` | Sonnet | Componentes Vue: `OrcamentoFormView`, `OrcamentoListaView`, `OrcamentoDetalheView`, `OrcamentoSettingsView` + sub-componentes (Cirurgias, Profissionais, Equipes, Implantes, LocalCirurgia, Totais) |
| `migration-engineer` | Opus | Refatoração da `OrcamentoCompletoView` legada → estrutura tabbed do novo, sem perder estado de form em progresso |
| `qa-engineer` | Sonnet | Testes unitários dos cálculos (calcularValorProfissional, calcularValorLocal, calcularTotaisFormaPagamento) |
| `senior-qa-engineer` | Opus | Estratégia de testes: matriz cirurgias × profissionais × formas de pagamento |
| `security-engineer` | Opus | Revisão de RLS policies (item 9) e enforcement no backend (sempre filtra por estabelecimento_id) |

---

## Fase 6.0 — Limpeza de divergências ✅ CONCLUÍDA (2026-04-30)

### Resumo da execução

- **Domain**: removidos `TipoOrcamento`, `ConfigPagamentoOrcamento` (jsonb), fundida fábrica `Criar` única (sem distinção simples/cirúrgico). `OrcamentoStatus` alinhado ao legado: `Rascunho/Enviado/Aprovado/Recusado/Cancelado/Expirado`. Adicionados métodos `Enviar()` / `Cancelar()`. Edição liberada em `Rascunho` e `Enviado`.
- **Contracts**: `CriarOrcamentoCommand` e `AtualizarOrcamentoCommand` fundidos (collections opcionais). Deletados `*CompletoCommand` e `ObterOrcamentoCompletoQuery`. Criados `EnviarOrcamentoCommand` e `CancelarOrcamentoCommand`. `OrcamentoCompletoDto` virou apenas `OrcamentoDto`.
- **Application**: handlers fundidos. Criados `EnviarOrcamentoCommandHandler` e `CancelarOrcamentoCommandHandler`. `OrcamentoMapping` substitui `OrcamentoCompletoMapping`.
- **Infrastructure**: `OrcamentoConfiguration` sem mapping de `Tipo`/`Configuracao`. `OrcamentoRepository` sem `ObterPorIdComItens` (sempre completo). `OrcamentoQueryRepository` simplificado (split queries puras, sem desserialização de jsonb).
- **API**: controller único com endpoints `POST /orcamentos`, `PUT /orcamentos/{id}`, `POST /{id}/enviar|aprovar|recusar|cancelar`. Endpoints `/completo` removidos.
- **Migration**: [`supabase/migrations/20260430121602_fase6_limpa_orcamento.sql`](../supabase/migrations/20260430121602_fase6_limpa_orcamento.sql) — DROP COLUMN `tipo` e `config_pagamento_json`, UPDATE de status `Pendente → Enviado`, CHECK constraint para os 6 status novos.
- **Frontend**: `OrcamentoCompletoView.vue` renomeado para `OrcamentoFormView.vue` (route `OrcamentoForm`); `OrcamentosView.vue` (lista simplista) deletado; `orcamentoCompletoService.ts` fundido em `orcamentoService.ts`; sidebar/HomeView/PacienteDetalheView atualizados (item "Orçamentos" da nav será reintroduzido na Fase 6.2 com a `OrcamentoListaView`).
- **Side-effects corrigidos**: `ExpirarOrcamentosVencidosCommandHandler` e `DashboardQueryRepository` agora filtram por `IN ('Rascunho','Enviado')` em vez de `'Pendente'`.
- **Testes**: 27 testes de orçamento (Domain) verdes; 209 testes totais verdes; `vue-tsc` sem erros.

### Escopo planejado original (referência)

> **Por que primeiro:** alinha o domínio/schema/UI ao padrão do legado **antes** de adicionar features. Sem isso, qualquer trabalho em cima das divergências amplifica o débito.

### Escopo

| # | Item | Onde | Agente | Modelo |
|---|------|------|--------|--------|
| 6.0.1 | Remover `Tipo: Simples \| Cirúrgico` do domain — orçamento é único | `Domain/Orcamentos/Orcamento.cs`, `Domain/Orcamentos/OrcamentoTipo.cs` | senior-software-engineer | Opus |
| 6.0.2 | Fundir endpoints `/orcamentos` e `/orcamentos/completo` em endpoints únicos | `OrcamentoController.cs` | senior-software-engineer | Opus |
| 6.0.3 | Fundir `CriarOrcamentoCommand` + `CriarOrcamentoCompletoCommand` → único `CriarOrcamentoCommand` (collections opcionais) | `Contracts/Orcamentos/Commands/`, `Application/Orcamentos/` | software-engineer | Sonnet |
| 6.0.4 | Fundir `AtualizarOrcamentoCommand` + `AtualizarOrcamentoCompletoCommand` | idem | software-engineer | Sonnet |
| 6.0.5 | Remover `ConfigPagamentoOrcamento` jsonb — descontos/acréscimos só em `OrcamentoFormaPagamento` | `Domain/Orcamentos/ConfigPagamentoOrcamento.cs` (delete), `Orcamento.cs` (remover prop), `OrcamentoConfiguration.cs` (remover mapping jsonb) | senior-software-engineer | Opus |
| 6.0.6 | Alinhar `OrcamentoStatus` enum: `Rascunho / Enviado / Aprovado / Recusado / Cancelado / Expirado` (estado inicial = `Rascunho`, transição `Rascunho → Enviado → Aprovado/Recusado/Cancelado`) | `Domain/Orcamentos/OrcamentoStatus.cs`, fábricas de `Orcamento` | senior-software-engineer | Opus |
| 6.0.7 | Comando `EnviarOrcamentoCommand` (Rascunho → Enviado) | novo arquivo em `Contracts/`/`Application/` | software-engineer | Sonnet |
| 6.0.8 | Comando `CancelarOrcamentoCommand` (qualquer → Cancelado) | novo arquivo | software-engineer | Sonnet |
| 6.0.9 | Migration: `DROP COLUMN tipo, config_pagamento_json` em `orcamentos`; `UPDATE` para mapear status antigos para novos (`Pendente → Rascunho`); ajustar `CHECK constraint` de status | nova migration `20260501XXXXXX_fase6_limpa_orcamento.sql` | database-architect | Opus |
| 6.0.10 | Deletar `OrcamentosView.vue` (lista simplista) | `frontend/src/views/orcamentos/OrcamentosView.vue` | ui-implementer | Sonnet |
| 6.0.11 | Renomear `OrcamentoCompletoView.vue` → `OrcamentoFormView.vue` (continua sendo a única view, mas vai virar tabbed na Fase 6.2) | `frontend/src/views/orcamentos/` | ui-implementer | Sonnet |
| 6.0.12 | Atualizar router e links (`Orcamentos`, `OrcamentoCompleto` → `Orcamentos`, `OrcamentoForm`) | `frontend/src/router/index.ts`, qualquer view que linke | ui-implementer | Sonnet |
| 6.0.13 | Remover `orcamentoCompletoService.ts` — fundir tudo em `orcamentoService.ts` | `frontend/src/services/` | software-engineer | Sonnet |

### Verificação Fase 6.0

- [ ] `dotnet build Imedto.Backend.sln` sem warnings novos.
- [ ] `dotnet test Tests/Imedto.Backend.Test` verde (handlers fundidos).
- [ ] Migration aplicada via `supabase db push` em homolog sem erro.
- [ ] `npm run build` no frontend sem erro de import quebrado.
- [ ] Smoke test: GET/POST/PUT `/api/orcamentos` continua funcionando para um orçamento mínimo (paciente + 1 forma de pagamento + 1 item).

### Riscos Fase 6.0

- **Dado existente** com `tipo='Simples'`: precisa do UPDATE da migration mapear corretamente. Validar que não quebra orçamentos já criados em homolog.
- **`config_pagamento_json` com valores não-default** já preenchidos: a migration precisa decidir se preserva (criando uma `OrcamentoFormaPagamento` derivada) ou descarta. Default: descartar — Fase 6.0 é limpeza, não preservação.

---

## Fase 6.1 — Settings: catálogos de orçamento ✅ CONCLUÍDA (2026-04-30)

### Resumo da execução

- **Domain (`Domain/Orcamentos/Catalogos/`)**: 6 aggregate roots — `CatalogoCirurgia`, `ValorProfissionalOrcamento`, `ConfiguracaoLocalCirurgia`, `CatalogoEquipeEspecializada`, `CatalogoImplante`, `ConfiguracaoPagamentoCatalogo`. Cada um com fábrica `Criar`, métodos `Atualizar/Inativar/Reativar` (soft-delete via flag `Ativo` para preservar histórico de orçamentos antigos). Interfaces de repositório consolidadas em `ICatalogosRepositories.cs`.
- **Infrastructure**: `CatalogosConfigurations.cs` com 6 EF mappings; `CatalogosRepositories.cs` com 6 implementações; `OrcamentoCatalogoQueryRepository.cs` (Dapper) com listagens otimizadas.
- **Contracts/Application**: `CatalogoCommands.cs` (15 commands — Criar/Atualizar/Remover por catálogo + `SalvarConfiguracaoLocalCommand` upsert), `CatalogoQueries.cs`, DTOs em `Queries/Results/`. Handlers consolidados em 2 arquivos (`CatalogoCommandHandlers.cs`, `CatalogoQueryHandlers.cs`).
- **API**: `OrcamentoCatalogoController` agrega 23 endpoints CRUD em `/api/orcamentos/configuracoes/{cirurgias|valores-profissional|local-cirurgia|equipes|implantes|pagamento}`. Tudo premium (`[FeatureGate("OrcamentoCompleto")]`) e tenant-scoped.
- **Migration**: [`20260430123214_fase6_1_orcamento_catalogos.sql`](../supabase/migrations/20260430123214_fase6_1_orcamento_catalogos.sql) cria 6 tabelas + índices + RLS (tenant-scoped via `vinculo_profissional_estabelecimento` ou dono).
- **DI**: `Container.cs` atualizado com 6 repos + 16 handlers + 6 query handlers + bus registrations (16 commands + 6 queries).
- **Frontend**: `orcamentoCatalogoService.ts` (1 service unificado com 6 sub-grupos de métodos); `OrcamentoSettingsView.vue` com `AppTabs` de 6 abas (Cirurgias / Profissionais / Local cirurgia / Equipes / Implantes / Pagamento), CRUD inline em cada aba.
- **Sidebar/Router**: novo item "Config. orçamento" no footer (apenas Dono); rota `/configuracoes/orcamento` → `OrcamentoSettings`.
- **Testes**: `dotnet build` 0 erros; `dotnet test` 209/209 verdes; `vue-tsc` 0 erros.

> **Nota de UX**: a view atual é um **placeholder funcional** — Fase 6.2 vai polir com sub-componentes dedicados (`SettingsCirurgias.vue`, etc.) e drawers do design system. Por ora, o usuário já consegue povoar todos os 6 catálogos via UI.

### Escopo planejado original (referência)

> **Por que segundo:** o Form (Fase 6.2) **depende** desses catálogos como fonte de dados. Sem cirurgias cadastradas, sem valor de profissional definido, sem configuração de estabelecimento, o Form não tem o que renderizar.

### Escopo (6 sub-aggregates novos)

Cada um é um aggregate root pequeno, com CRUD direto. Em ordem de dependência:

| # | Sub-aggregate | Tabela | Campos principais | Agente |
|---|---|---|---|---|
| 6.1.1 | `OrcamentoCirurgia` (catálogo) | `orcamento_catalogo_cirurgia` | `id`, `estabelecimento_id`, `descricao`, `valor_base`, `duracao_padrao_minutos`, `produtos_padrao` (collection: `produto_inventario_id`, `quantidade`, `uso_unico`) | senior-software-engineer (domain) + software-engineer (handlers) + db-engineer (schema) |
| 6.1.2 | `ValorProfissionalOrcamento` (catálogo) | `orcamento_valor_profissional` | `id`, `estabelecimento_id`, `profissional_id` (FK), `funcao` (string), `tempo_base_minutos`, `valor_tempo_base`, `tempo_adicional_minutos`, `valor_adicional`, `valor_plus` | senior-software-engineer (domain) + software-engineer (handlers) |
| 6.1.3 | `ConfiguracaoEstabelecimentoOrcamento` (1:1 com estabelecimento) | `orcamento_configuracao_estabelecimento` | `estabelecimento_id` (PK), `local_tempo_base`, `local_valor_base`, `local_tempo_adicional`, `local_valor_adicional`, idem para `peridural`, `geral`, `ambulatorio`, `sem_internacao` | software-engineer |
| 6.1.4 | `EquipeEspecializadaOrcamento` (catálogo) | `orcamento_catalogo_equipe` | `id`, `estabelecimento_id`, `descricao`, `valor_padrao` | software-engineer + db-engineer |
| 6.1.5 | `ImplanteCatalogo` (catálogo) | `orcamento_catalogo_implante` | `id`, `estabelecimento_id`, `descricao`, `custo_unitario`, `item_inventario_id` (FK opcional) | software-engineer |
| 6.1.6 | `ConfiguracaoPagamentoOrcamento` (catálogo) | `orcamento_configuracao_pagamento` | `id`, `estabelecimento_id`, `forma_pagamento_id` (FK), `acrescimo_percentual`, `entrada_percentual_padrao`, `taxa_parcela`, `categorias_isentas` (jsonb array) | software-engineer + db-engineer |

### Tarefas por sub-aggregate (template)

Para **cada** sub-aggregate acima, replicar:

1. **Domain** — aggregate root + fábrica `.Criar(...)` + métodos de mutação (`AtualizarValores`, etc.) + `BusinessException` para invariantes (ex: `valor_base >= 0`).
2. **EF configuration** — mapping em `Infrastructure/Database/Configurations/`, `DbSet<>` em `AppDbContext`.
3. **Migration EF** + cópia idempotente para `supabase/migrations/`.
4. **Commands**: `Criar*Command`, `Atualizar*Command`, `Remover*Command`.
5. **Queries**: `Listar*Query`, `Obter*Query`.
6. **Handlers** (commands scoped, queries singleton).
7. **Repositório EF** (escrita) + **Query repository Dapper** (leitura).
8. **Controller** com `[ApiController][Route("api/orcamentos/configuracoes/{recurso}")]`.
9. **`[FeatureGate("OrcamentoCompleto")]`** nos endpoints (premium).
10. **RLS policy** filtrando por `estabelecimento_id` (defense-in-depth).
11. **Frontend service** (`orcamentoCatalogoService.ts`) + **view tabbed** com sub-tabs para cada catálogo.
12. **Testes unitários** dos handlers + dos invariantes do domain.

### Frontend (UI da Settings tab)

| # | Componente | Path |
|---|---|---|
| 6.1.7 | `OrcamentoSettingsView.vue` (tabbed wrapper) | `frontend/src/views/orcamentos/OrcamentoSettingsView.vue` |
| 6.1.8 | `SettingsCirurgias.vue` (lista + drawer de edição) | `frontend/src/components/orcamento/settings/` |
| 6.1.9 | `SettingsValorProfissional.vue` | idem |
| 6.1.10 | `SettingsConfiguracaoEstabelecimento.vue` | idem |
| 6.1.11 | `SettingsEquipes.vue` | idem |
| 6.1.12 | `SettingsImplantes.vue` | idem |
| 6.1.13 | `SettingsConfiguracoesPagamento.vue` | idem |

> Reutilizar **AppTabs**, **AppDrawer**, **AppPagination**, **AppCard**, **AppField** do design system.

### Verificação Fase 6.1

- [ ] Todas as 6 tabelas criadas e com RLS ativa.
- [ ] CRUD funcional via Swagger para cada catálogo.
- [ ] UI permite criar/editar/remover de cada catálogo.
- [ ] Filtro por `estabelecimento_id` funcionando (criar 2 estabs, validar isolamento).
- [ ] Testes unitários para invariantes do domain (≥1 teste por aggregate).

### Riscos Fase 6.1

- **Volume**: 6 aggregates × ~15 arquivos cada = ~90 arquivos novos. Spawn paralelo de software-engineer (cada um pega 1-2 aggregates).
- **Migrations longas**: gerar EF migration com 6 tabelas em uma migration ou 6 migrations separadas. Recomendado: 1 migration por aggregate para rollback granular.

---

## Fase 6.2 — Form completo (tabs + seções) e Detail ✅ CONCLUÍDA (2026-04-30)

### Resumo da execução

- **`OrcamentoListaView.vue`** — listagem com filtros (busca client-side por paciente/número com debounce, filtro de status no servidor), paginação (`AppPagination`), badges de status. Linhas clicáveis levam ao Detalhe. Substitui o que `Budgets.vue` faz no legado.
- **`OrcamentoDetalheView.vue`** — visualização read-only com cards por seção (cabeçalho, cirurgias, equipe, implantes, local & anestesia, formas de pagamento) + resumo sticky lateral. Botões contextuais por status:
  - `Rascunho` → Editar / Enviar / Cancelar
  - `Enviado` → Editar / Aprovar / Recusar / Cancelar
  - `Aprovado` → Cancelar
  - terminais (`Recusado`/`Cancelado`/`Expirado`) → só read-only
- **`OrcamentoFormView.vue`** — refatorado de single-page colapsável para **tabbed** (4 abas: Paciente / Cirurgias / Equipe & Implantes / Local & Pagamento). **Consome todos os 6 catálogos da Fase 6.1** com auto-preenchimento ao selecionar:
  - Cirurgia do catálogo → preenche descrição, valor base, duração padrão.
  - Tabela de valor profissional → preenche função, valor, profissional UUID.
  - Catálogo de implantes → preenche descrição e custo unitário.
  - Catálogo de equipes especializadas → preenche descrição e valor padrão.
  - Configuração de pagamento → preenche acréscimo % e entrada % padrão.
- **Resumo sticky** com cálculo de integridade em tempo real (soma das formas vs. total).
- **Watchers** auto-recalculam internação total ao mudar dias/diária.
- **Router**: 4 rotas — `Orcamentos` (lista), `OrcamentoDetalhe` (`/:id`), `OrcamentoForm` (`/:id/editar`), `OrcamentoSettings` (`/configuracoes/orcamento`).
- **Sidebar**: item "Orçamentos" restaurado (já não era do Dono — todo profissional vinculado pode ver). "Config. orçamento" continua só para Dono.
- **Fluxos restaurados**: `HomeView` recupera card de Orçamentos; `PacienteDetalheView` aponta para `OrcamentoDetalhe` ao abrir um orçamento existente.

### Pendências técnicas (TODOs explícitos)

- **Criação de orçamento via UI** (Fase 6.2.b ou 6.4): hoje o orçamento precisa existir antes de aparecer no Form/Detalhe — POST `/api/orcamentos` é via API/integração com prontuário. Telа dedicada de criação deixada para a Fase 6.4 (integração com prontuário).
- **Filtro `pacienteId` na lista**: query string preparada em `PacienteDetalheView`, mas a `OrcamentoListaView` ainda não lê. Pequeno ajuste pendente.

### Validação
- ✅ `vue-tsc --noEmit` — 0 erros
- ✅ Backend não tocado nesta fase — testes da Fase 6.0/6.1 continuam verdes (209/209).

### Escopo planejado original (referência)

> **Por que terceiro:** depende da Fase 6.1 (catálogos) para os seletores. Reescreve a UX para o padrão tabbed do legado.

### Escopo

| # | Item | Onde | Agente |
|---|---|---|---|
| 6.2.1 | `OrcamentoListaView.vue` — substitui o que `Budgets.vue` faz no legado: lista + filtros (paciente, status, validade) + ordenação + paginação | nova view em `frontend/src/views/orcamentos/` | ui-implementer + ux-designer |
| 6.2.2 | `OrcamentoFormView.vue` — formulário multi-seção (renomeado da `OrcamentoCompletoView` na Fase 6.0) | atualizar | migration-engineer |
| 6.2.3 | `OrcamentoDetalheView.vue` — visualização read-only do orçamento + ações (Editar / Aprovar / Recusar / Cancelar / Gerar PDF / Converter em cirurgia) | nova view | ui-implementer |
| 6.2.4 | Seção `BudgetPatientSection.vue` → `OrcamentoSecaoPaciente.vue` | `frontend/src/components/orcamento/form/` | ui-implementer |
| 6.2.5 | Seção `BudgetCirurgiasSection.vue` → `OrcamentoSecaoCirurgias.vue` (seleciona do catálogo, ajusta quantidade/duração) | idem | ui-implementer |
| 6.2.6 | Seção `BudgetProfissionaisSection.vue` → `OrcamentoSecaoProfissionais.vue` (puxa valor profissional do catálogo, calcula honorário pelo tempo) | idem | ui-implementer |
| 6.2.7 | Seção `BudgetLocalCirurgiaSection.vue` → `OrcamentoSecaoLocal.vue` (tipo de internação/anestesia) | idem | ui-implementer |
| 6.2.8 | Seção `BudgetEquipesSection.vue` → `OrcamentoSecaoEquipes.vue` | idem | ui-implementer |
| 6.2.9 | Seção `BudgetImplantesSection.vue` → `OrcamentoSecaoImplantes.vue` | idem | ui-implementer |
| 6.2.10 | Seção `BudgetTotaisSection.vue` → `OrcamentoSecaoTotais.vue` (subtotais por categoria, formas de pagamento, validação de integridade) | idem | ui-implementer |
| 6.2.11 | Roteamento: `Orcamentos` (lista) / `OrcamentoForm` (criar/editar) / `OrcamentoDetalhe` (read-only) / `OrcamentoSettings` (config — Fase 6.1) | `frontend/src/router/index.ts` | ui-implementer |

### Verificação Fase 6.2

- [ ] Lista com paginação + filtros funcionando.
- [ ] Form salva orçamento com cirurgias + profissionais + equipes + implantes + formas de pagamento.
- [ ] Detalhe exibe tudo read-only e permite ações de status.
- [ ] Navegação Lista ↔ Form ↔ Detalhe sem perda de estado indevida.
- [ ] Mobile: tabs viram drawer / acordeão (decidir com ux-designer).

### Riscos Fase 6.2

- **UX intensiva**: ux-designer precisa validar antes do ui-implementer abrir tickets.
- **Componentes grandes**: cada seção pode ter 200-400 linhas. Sub-componentizar para manter cada arquivo abaixo de 300 linhas.

---

## Fase 6.3 — Cálculos (no backend, fonte da verdade) ✅ CONCLUÍDA (2026-04-30)

### Resumo da execução

- **`Domain/Orcamentos/Calculos/OrcamentoCalculadora.cs`** — classe estática com 3 métodos puros (todos em `decimal`):
  - `CalcularValorProfissional(tempoCirurgia, tempoBase, valorBase, tempoAdicional, valorAdicional, valorPlus)` — honorário com blocos adicionais (ceil para cobrir minutos parciais).
  - `CalcularValorLocal(tempoCirurgia, tempoBase, valorBase, tempoAdicional, valorAdicional)` — mesma estrutura para sala cirúrgica.
  - `CalcularFormaPagamento(subtotal, acrescimo%, entrada%, parcelas) → FormaPagamentoCalculada` — devolve `TotalBruto` (com acréscimo), `Entrada` (% do bruto) e `ValorParcela` (restante / parcelas).
- **Query `PreviewOrcamentoQuery`** + handler — recebe o estado do form em construção e devolve totais por categoria, total geral, soma das formas, diferença e flag de integridade. Sem persistir.
- **Endpoint `POST /api/orcamentos/preview`** — autenticado, tenant-scoped, sem `[Idempotent]` (não cria nada).
- **Testes**: 13 testes unitários cobrindo casos limite (tempo zero, tempo igual ao base, blocos parciais, múltiplos blocos, parcelas zero, acréscimo + entrada combinados). Total geral: **222/222 verdes**.
- **Frontend**:
  - `orcamentoService.preview()` — wrapper HTTP.
  - `composables/usePreviewOrcamento.ts` — compõe `Ref<payload>`, debounce 250ms via `setTimeout`, cancela request anterior via `AbortController`. Devolve `{ preview, carregando, erro }`.
  - `OrcamentoFormView.vue` — substituiu computeds locais de totais pelo `preview.value` do servidor. Resumo lateral mostra "Resumo (calculando…)" durante request em vôo.

### O que mudou na prática

- **Antes**: cada keystroke recalculava localmente — risco de divergência com o backend (regras de arredondamento, cálculo de blocos, etc.).
- **Depois**: o backend é fonte da verdade. O front exibe o que o backend retorna; o `salvar` aplica a mesma regra ao persistir, garantindo invariante "preview = save".

### Não migrado (postergado)

- **`consolidarProdutos`** (item 6.3.4 do plano original) — depende de "produtos padrão por catálogo de cirurgia", não modelado na Fase 6.1 simplificada. Quando os produtos por cirurgia entrarem (sub-fase ou Fase 6.2.b), adicionar o método à calculadora + caso ao preview.

### Validação
- ✅ `dotnet build` — 0 erros
- ✅ `dotnet test` — **222/222** (13 novos da calculadora)
- ✅ `vue-tsc --noEmit` — 0 erros

### Escopo planejado original (referência)

> **Por que quarto:** os cálculos são **regra de negócio** — devem viver no backend (CLAUDE.md: "as regras de negocio devem estar todas no backend"). Frontend apenas pode espelhar para preview, mas o submit final dispara recálculo no domain.

### Escopo

| # | Cálculo | Onde no domain | Equivalente legado | Agente |
|---|---|---|---|---|
| 6.3.1 | `calcularValorProfissional(tempoCirurgia, valorProfissionalCatalogoId) → valorTotal` (períodos adicionais com `valorPlus`) | método em `OrcamentoProfissional` ou serviço de domain | `useOrcamentoCalculos.ts:11-80` | senior-software-engineer |
| 6.3.2 | `calcularValorLocal(tipoInternacao, tempoCirurgia, configEstab) → valorTotal` (tempo base + períodos adicionais) | método em `OrcamentoInternacao` | `useOrcamentoCalculos.ts:82-130` | senior-software-engineer |
| 6.3.3 | `calcularTotaisFormaPagamento(subtotal, forma) → totalBruto, entrada, parcelas` | método em `OrcamentoFormaPagamento` | `useOrcamentoCalculos.ts:132-186` | senior-software-engineer |
| 6.3.4 | `consolidarProdutos(cirurgiasSelecionadas, catalogoCirurgias) → List<ProdutoConsolidado>` (regra `uso_unico=true → max`; `uso_unico=false → soma`) | serviço de domain | `useOrcamentoProdutosConsolidados.ts` | senior-software-engineer |
| 6.3.5 | Endpoint **`POST /api/orcamentos/preview`** — recebe payload do form em construção e retorna totais calculados, sem persistir | novo controller endpoint | software-engineer |
| 6.3.6 | Frontend: apagar qualquer cálculo client-side existente; chamar `/preview` com debounce ao editar form | `frontend/src/composables/useOrcamentoCalculos.ts` (NOVO — apenas wrapper de `/preview`) | ui-implementer |
| 6.3.7 | Testes unitários extensos dos 4 cálculos com casos do legado (valor base, valor com período adicional, valor com 3 períodos, etc.) | `Tests/Imedto.Backend.Test/` | qa-engineer + senior-qa-engineer (estratégia) |

### Verificação Fase 6.3

- [ ] `dotnet test` com matriz de casos passando (≥30 testes só de cálculo).
- [ ] Endpoint `/preview` retorna em <200ms para payload típico (10 cirurgias + 5 profissionais + 3 formas).
- [ ] Frontend renderiza preview em <300ms após última digitação (debounce 250ms).

### Riscos Fase 6.3

- **Aritmética financeira**: usar `decimal` em todos os cálculos. Nunca `double`. Definir cultura `pt-BR` (já configurada globalmente).
- **Concorrência no preview**: backend deve aceitar `cancellationToken` para abortar previews antigos.

---

## Fase 6.4 — Conversão, integração com prontuário e PDF ✅ CONCLUÍDA (2026-04-30)

### Resumo da execução

**Conversão orçamento → cirurgia:**
- `Orcamento.RegistrarConversaoEmProcedimento(procedimentoId)` no domain — exige `Status=Aprovado`, bloqueia conversão dupla, valida `procedimentoId > 0`. Mantém o orçamento como `Aprovado` (não muda status).
- `ConverterOrcamentoEmCirurgiaCommand` + handler — busca o prontuário pelo paciente, cria `ProcedimentoCirurgico.Planejar()` com:
  - **Cirurgia principal**: descrição da primeira `OrcamentoCirurgia` do orçamento.
  - **Equipe inicial**: convertida com mapeamento string→`PapelCirurgia` (Cirurgião/Auxiliar/Anestesista/Instrumentador/Circulante). Não-mapeados são ignorados; duplicatas (mesmo profissional+papel) deduplicadas.
  - **Estabelecimento + paciente**: herdados do orçamento.
  - **Data agendada**: opcional via DTO de entrada.
- Após criar o procedimento, o handler chama `RegistrarConversaoEmProcedimento` que vincula `Orcamento.ProcedimentoCirurgicoId` ao novo procedimento.
- Endpoint **`POST /api/orcamentos/{id}/converter-em-cirurgia`** retorna o ID do procedimento criado.
- 3 testes do domínio cobrindo: status válido, status inválido (rascunho), conversão dupla.

**PDF rico:**
- `useOrcamentoPdf.ts` reescrito com **8 seções condicionais**: cabeçalho, dados do paciente, observações, **Cirurgias**, **Equipe**, **Implantes**, **Internação + Anestesia**, **Itens avulsos**, **Formas de pagamento detalhadas**, **Total final** com linha de destaque.
- Status agora cobre os 6 do enum novo (Rascunho/Enviado/Aprovado/Recusado/Cancelado/Expirado) com cores distintas.
- Numeração de páginas correta (loop pós-render dos `lastAutoTable`).
- Nome do arquivo sanitizado (kebab-case lowercase).

**Frontend (DetalheView):**
- Botão **"Converter em cirurgia"** — visível só quando `status=Aprovado`, sem `procedimentoCirurgicoId` e com pelo menos 1 cirurgia. Confirmação via `confirm`.
- Botão **"PDF"** sempre disponível.
- Quando há `procedimentoCirurgicoId`, exibe **link no cabeçalho** levando ao Detalhe da cirurgia (`CirurgiaDetalhe`).

### Não implementado (postergado para Fase 7+ ou rotina manual)

- **Pré-preenchimento de orçamento a partir do prontuário** (item 6.4.4–5 do plano original): exigia mapear "procedimentos indicados" do prontuário, que ainda usa estrutura JSONB livre. Implementação fica para quando essa estrutura for tipada (Fase 7).
- **PDF server-side** (item 6.4.8): mantido client-side com jsPDF — funciona para o uso atual. Migração para QuestPDF/PdfService no backend faria sentido se houvesse necessidade de assinatura digital ou armazenamento em S3.

### Validação

- ✅ `dotnet build` — 0 erros.
- ✅ `dotnet test` — **225/225** verdes (3 novos da conversão).
- ✅ `vue-tsc --noEmit` — 0 erros.

### Escopo planejado original (referência)

> **Por que último:** features de "fechamento" — não bloqueiam o uso básico, mas elevam o produto.

### Escopo

| # | Item | Onde | Agente |
|---|---|---|---|
| 6.4.1 | Comando `ConverterOrcamentoEmCirurgiaCommand` (orçamento aprovado → cria `ProcedimentoCirurgico` herdando descrição, equipe, implantes, anestesia) | `Application/Orcamentos/`, evento de domain `OrcamentoConvertidoEmCirurgia` | senior-software-engineer |
| 6.4.2 | Endpoint `POST /api/orcamentos/{id}/converter-em-cirurgia` | `OrcamentoController.cs` | software-engineer |
| 6.4.3 | Botão "Converter em cirurgia" no `OrcamentoDetalheView` (visível só se `Status=Aprovado` e `procedimento_cirurgico_id IS NULL`) | `frontend/src/views/orcamentos/OrcamentoDetalheView.vue` | ui-implementer |
| 6.4.4 | Endpoint `GET /api/prontuarios/{id}/sugerir-orcamento` — retorna procedimentos indicados + descrição cirúrgica do prontuário, prontos para pré-preencher form | `ProntuarioController.cs` ou novo `OrcamentoController` action | software-engineer |
| 6.4.5 | Botão "Criar orçamento a partir deste prontuário" na view de prontuário; abre `OrcamentoFormView` pré-preenchido | `frontend/src/views/pacientes/ProntuarioView.vue` | ui-implementer |
| 6.4.6 | PDF rico: usar serviço existente (a Fase 4 deve já ter `PdfService` se houve receitas; senão, usar `QuestPDF` no backend ou `jsPDF` no frontend) | `frontend/src/composables/useOrcamentoPdf.ts` (estender — atualmente é versão simples) | ui-implementer |
| 6.4.7 | PDF: cabeçalho com logo + nome estabelecimento, tabela de cirurgias, tabela de profissionais, tabela de implantes, **múltiplas formas de pagamento** lado a lado, observações, validade, rodapé | `useOrcamentoPdf.ts` | ui-implementer |
| 6.4.8 | Endpoint `GET /api/orcamentos/{id}/pdf` — retorna PDF gerado server-side (LGPD: não expõe dados sensíveis em URL) — **opcional**, manter client-side se PdfService não existir | `OrcamentoController.cs` | software-engineer |

### Verificação Fase 6.4

- [ ] Conversão gera `ProcedimentoCirurgico` com vínculo bidirecional (`procedimento_cirurgico_id` no orçamento).
- [ ] Tentativa de converter orçamento já convertido retorna `BusinessException` (422).
- [ ] PDF gerado abre no Acrobat sem erro e contém todos os dados visualmente legíveis.
- [ ] Pré-preenchimento a partir de prontuário traz procedimentos indicados.

### Riscos Fase 6.4

- **Idempotência da conversão**: garantir que clicar 2x não cria 2 procedimentos cirúrgicos. `BusinessException` se já existe vínculo.
- **PDF**: jsPDF tem limites com fontes Unicode (acentos). Validar render em casos com `ç`, `ã`, `é`.

---

## Itens descartados explicitamente (não migrar)

| Item legado | Razão de não migrar |
|---|---|
| `OrcamentosView.vue` (lista simples do **novo**) e seu modal | Não tem equivalente no legado. Era invenção. **Apagar** na Fase 6.0. |
| `ConfigPagamentoOrcamento` jsonb | Duplica `OrcamentoFormaPagamento`. **Apagar** na Fase 6.0. |
| Tipo `Simples \| Cirúrgico` | Invenção. **Apagar** na Fase 6.0. |
| RPC `save_orcamento_completo()` (legado) | Regra de negócio em SQL — antipattern do projeto novo. **Não portar**. Replicar em handler transacional via `UnitOfWorkAttribute`. |
| Tela única `Budgets.vue` (legado) com tabs internas reativas | Substituída por **rotas separadas** (`/orcamentos`, `/orcamentos/novo`, `/orcamentos/:id`, `/orcamentos/configuracoes`) — alinhado com o resto do app. |

---

## Ordem de execução recomendada

```
Fase 6.0 (limpeza)
    ↓
Fase 6.1 (settings — paralelizável: 6 ondas de aggregates simultâneas)
    ↓
Fase 6.2 (form/lista/detalhe — sequencial, depende de UX)
    ↓
Fase 6.3 (cálculos no backend)
    ↓
Fase 6.4 (conversão + PDF + integração prontuário)
```

**Estimativa de esforço (rough):**

| Fase | Backend | Frontend | DB | Total estimado |
|---|---|---|---|---|
| 6.0 | M | S | S | ~1 dia |
| 6.1 | XL | L | L | ~3-4 dias |
| 6.2 | S | XL | — | ~3 dias |
| 6.3 | L | S | — | ~1-2 dias |
| 6.4 | M | M | — | ~1-2 dias |
| **Total** | — | — | — | **~9-12 dias de trabalho focado** |

---

## Checklist global de saída da fase

- [ ] Todas as divergências da seção "Diagnóstico" foram removidas.
- [ ] Todas as lacunas L1–L9 foram preenchidas.
- [ ] `dotnet build` + `dotnet test` verdes.
- [ ] `npm run build` + `npm test` verdes.
- [ ] Migrations aplicadas em homolog sem erro.
- [ ] RLS ativa em **todas** as tabelas `orcamento*`.
- [ ] Smoke test E2E: criar orçamento de cirurgia complexa (3 cirurgias, 4 profissionais, 2 implantes, 1 equipe, 3 formas de pagamento) → aprovar → converter em cirurgia → gerar PDF.
- [ ] LGPD: validar que nenhum log do backend imprime CPF/telefone/nome do paciente em queries de orçamento.
- [ ] Performance: lista de 1000 orçamentos com filtros aplicados < 500ms.

---

## Observações finais

- **Não criar TODO/feature flag para a Fase 6**: a área é grande, mas mergeable em pedaços. Cada subfase 6.X é um PR fechado.
- **Reusar o máximo do design system** ([frontend/src/components/ui/](../frontend/src/components/ui/)). Se um componente novo for proposto (ex: `OrcamentoTotaisCard`), pesar se vale promover ao design system ou ficar específico do módulo.
- **Não inventar UX**: seguir o legado nas decisões de quais campos exibir. Se algo do legado for ruim, abrir issue separada — esta fase é **paridade**, não redesign.
