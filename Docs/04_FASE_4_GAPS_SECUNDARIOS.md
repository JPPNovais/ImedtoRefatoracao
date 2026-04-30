# Fase 4 — Gaps secundários

**Status geral:** ✅ concluída no backend + banco (frontend Fase 4 entregue, paridade revisada e bloqueadores resolvidos)
**Iniciada em:** 2026-04-29
**Concluída em:** 2026-04-30

> **Objetivo:** fechar os gaps secundários do plano original + pendências postergadas das Fases 1, 2 e 3 que não cabem em "domínio core" mas são necessárias para paridade comportamental e operacional com o legado.
>
> **Pré-requisitos:** Fases 1, 2 e 3 ✅ concluídas e aplicadas no banco (incluindo paridade da Fase 3).
>
> **Referência:** [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md), [03_FASE_3_DOMINIO_CLINICO.md](03_FASE_3_DOMINIO_CLINICO.md).

## Escopo total da fase

### Itens do plano original (Fase 4)

| # | Item | Descrição |
|---|------|-----------|
| 4.1 | **9 RPCs de relatórios consolidados** | Substituir RPCs SQL legados por ~3-4 query handlers parametrizados. |
| 4.2 | **Solicitação de vínculo inversa** (profissional → clínica) | Tabela `solicitacoes_vinculo` + workflow. |
| 4.3 | **Política de retenção/anonimização LGPD** | Job mensal de anonimização + endpoint `DELETE /api/minha-conta`. |

### Pendências incorporadas das fases anteriores

#### Da Fase 1
| # | Item | Origem |
|---|------|--------|
| 4.4 | Teste de integração do `SoftDeleteInterceptor` | Fase 1 / Fase 2 (item 2.16) |
| 4.5 | Load test rate limit em `/auth` (k6 ou xUnit + TestServer) | Fase 1 / Fase 2 (item 2.17) |

#### Da Fase 2
| # | Item | Origem |
|---|------|--------|
| 4.6 | Backplane Redis para SignalR (multi-instância) | Fase 2 (item 2.4) |
| 4.7 | Provedor de email real (Resend/SES) para ações de automação e notificações | Fase 2 (item 2.2/2.3) |
| 4.8 | Cookie scope `access-token` revisão (path `/api` vs `/`) para SignalR | Fase 2 (item 2.4) |
| 4.9 | Limpeza de bucket `prontuario-anexos` antigo | Fase 2 (operacional manual) |
| 4.10 | Storage policies em `storage.objects` (via dashboard, exige ownership) | Fase 2 (item 2.5) |

#### Da Fase 3
| # | Item | Origem |
|---|------|--------|
| 4.11 | **PDF real de Receitas** (`QuestPdfReceitaService`) | Fase 3 (item 3.1) |
| 4.12 | **Regras ANVISA 30/90 dias por classe + retenção antibiótico (RDC 471/2021)** | Fase 3 (item 3.1) |
| 4.13 | **Catálogo TUSS/CBHPM** (formal, não só campo livre) | Fase 3 (item 3.3.A) |
| 4.14 | **Aplicar permissões finas** nos handlers (`gerir_permissoes`, `config_estabelecimento`, `gerir_profissionais`, `modelos_prontuario`, `automacao_config`) — substituir TODOs | Fase 3 (item 3.4) |
| 4.15 | **Decisões de UX/produto** verificadas com produto (papéis cirúrgicos extras, modos de comissão de equipe, vínculo de substituição persistido em receita) | Fase 3 (verificações pendentes) |

#### Postergada anterior
| # | Item | Origem |
|---|------|--------|
| 4.16 | Estratégia de `admin-reset-estabelecimento` que faça bypass do `SoftDeleteInterceptor` | Fase 1 (operacional) |

## Plano de agentes

> Segue o **mapa fixo de responsabilidades** do [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md). Atribuição por item desta fase:

| Agente | Modelo | Itens nesta fase |
|--------|--------|------------------|
| `senior-software-engineer` | Opus | 4.1 (consolidação de query handlers de relatórios), 4.2 (solicitação de vínculo inversa workflow), 4.3 (política de retenção/anonimização — design da máquina de estados) |
| `software-engineer` | Sonnet | 4.11 (PDF de receita com QuestPDF), 4.14 (aplicar permissões finas nos handlers — substituir TODOs Fase 3), 4.16 (admin-reset bypass simples) |
| `data-analyst` | Opus | **Antes da implementação de 4.1:** definir KPIs e granularidade dos 9 RPCs legados; produzir DTOs e parâmetros dos query handlers consolidados. Output = ADR curto. |
| `database-architect` | Opus | Migrations + RLS para 4.2 (solicitacoes_vinculo) + retenção LGPD (4.3); otimização das queries pesadas (4.1) seguindo `supabase-postgres-best-practices` |
| `security-engineer` | Opus | 4.3 (anonimização LGPD com mascaramento PII), 4.10 (orientar usuário sobre policies storage), 4.12 (regras ANVISA), 4.14 (auditoria das permissões finas aplicadas) |
| `devops-cloud-engineer` | Opus | 4.6 (backplane Redis SignalR), 4.7 (provedor email real), 4.8 (cookie scope), 4.9 (cleanup operacional bucket antigo) |
| `ui-implementer` | Sonnet | Frontend de relatórios (4.1) + tela de solicitação de vínculo inversa (4.2) + tela de "minha conta" com export+delete LGPD (4.3) |
| `qa-engineer` | Sonnet | 4.4 (integração SoftDeleteInterceptor), 4.5 (load test auth), testes dos itens implementados |
| `senior-qa-engineer` | Opus | Estratégia de teste de carga global (k6 ou similar) — quando 4.5 sair de "load test pontual" para suite completa |
| `migration-engineer` | Opus | Revisão final de paridade ao fechar a fase |

## Ondas de execução

### Wave 1 — paralela (itens isolados, sem dependências)
- **4.4** Teste integração SoftDeleteInterceptor (`qa-engineer`)
- **4.5** Load test rate limit `/auth` (`qa-engineer`)
- **4.11** PDF real Receitas (`software-engineer`)
- **4.14** Aplicar permissões finas nos handlers (`software-engineer`)
- **4.16** Admin-reset bypass simples (`software-engineer`)
- **data-analyst**: ADR de KPIs para 4.1 (paralelo, informa Wave 2)

### Wave 2 — depende de Wave 1
- **4.1** Relatórios consolidados (`senior-software-engineer` + `database-architect` para queries Dapper otimizadas)
- **4.2** Solicitação de vínculo inversa (`senior-software-engineer`)
- **4.3** Retenção/anonimização LGPD (`senior-software-engineer` + `security-engineer`)
- **4.12** Regras ANVISA Receitas (`security-engineer`)

### Wave 3 — paralela, infra
- **4.6** Backplane Redis SignalR (`devops-cloud-engineer`)
- **4.7** Provedor email real (`devops-cloud-engineer`)
- **4.8** Cookie scope (`devops-cloud-engineer`)
- **4.13** Catálogo TUSS/CBHPM (`software-engineer` + `database-architect` para seed)

### Wave 4 — fechamento
- **Migrations consolidadas** (`database-architect`): EF + supabase SQL para `solicitacoes_vinculo` + retenção LGPD + TUSS.
- **Frontend** (`ui-implementer`): relatórios, solicitação inversa, minha-conta.
- **Testes** (`qa-engineer`): cobertura adicional + testes de integração.
- **Revisão de paridade** (`migration-engineer`).
- **Build + test final + aplicar migrations via MCP**.

---

## Schema fechado da Fase 4

### 4.1 Relatórios consolidados — query handlers parametrizados (sem schema novo)

Substituem 9 RPCs legados por ~4 handlers:

1. **`RelatorioFinanceiroQueryHandler`** — fluxo de caixa, faturamento, breakdown por categoria.
   - Filtros: `dataInicio`, `dataFim`, `agruparPor` (`dia`/`semana`/`mes`/`categoria`/`forma_pagamento`).
   - Substitui RPCs: `rpc_report_cash_flow`, `rpc_report_financial_summary`, `rpc_report_financial_by_category`.

2. **`RelatorioOperacionalQueryHandler`** — agenda + dashboard + estoque.
   - Filtros: `dataInicio`, `dataFim`, `tipo` (`agenda`/`dashboard`/`inventario`).
   - Substitui: `rpc_report_dashboard_summary`, `rpc_report_agenda_summary`, `rpc_report_inventory_summary`.

3. **`RelatorioPessoasQueryHandler`** — pacientes + profissionais.
   - Filtros: `dataInicio`, `dataFim`, `tipo` (`pacientes`/`profissionais_performance`).
   - Substitui: `rpc_report_patients_summary`, `rpc_report_professionals_performance`.

4. **`RelatorioOrcamentosQueryHandler`** — funil + conversão + valor médio.
   - Substitui: `rpc_report_budgets_summary`.

DTOs flexíveis (`Dictionary<string, decimal>` para breakdown agrupado + `IList<RowSummary>`).

### 4.2 Solicitação de vínculo inversa

```
solicitacoes_vinculo
  id bigserial PK
  profissional_usuario_id uuid NOT NULL
  estabelecimento_id bigint NOT NULL FK ON DELETE CASCADE
  status varchar(20) NOT NULL          -- Pendente | Aprovada | Recusada | Cancelada
  mensagem varchar(1000) NULL
  criada_em timestamptz NOT NULL DEFAULT now()
  respondida_em timestamptz NULL
  respondida_por_usuario_id uuid NULL
  motivo_recusa varchar(500) NULL
  unique (profissional_usuario_id, estabelecimento_id, status)  -- não permite 2 pendentes
```

Endpoints:
- `POST /api/solicitacoes-vinculo` (profissional pede acesso a um estabelecimento)
- `GET /api/solicitacoes-vinculo/minhas` (do profissional)
- `GET /api/solicitacoes-vinculo/recebidas` (do estabelecimento — apenas dono)
- `POST /api/solicitacoes-vinculo/{id}/aprovar` → cria `Vinculo` + notifica
- `POST /api/solicitacoes-vinculo/{id}/recusar`
- `POST /api/solicitacoes-vinculo/{id}/cancelar` (profissional)

### 4.3 Retenção / Anonimização LGPD

```sql
-- Coluna em pacientes (já tem soft delete; adicionar dado de anonimização)
ALTER TABLE pacientes ADD anonimizado_em timestamptz NULL;
ALTER TABLE pacientes ADD anonimizado_por_usuario_id uuid NULL;

-- Tabela de log de anonimização (audit LGPD)
lgpd_anonimizacoes
  id bigserial PK
  tabela varchar(80) NOT NULL
  registro_id bigint NOT NULL
  motivo varchar(40) NOT NULL          -- Inativacao | DirreitoEsquecimento | RetencaoVencida
  anonimizado_em timestamptz NOT NULL DEFAULT now()
  executado_por_usuario_id uuid NULL   -- null = job automático

-- Tabela de consentimento LGPD (já existe? verificar; se não, criar)
lgpd_consentimentos
  id bigserial PK
  usuario_id uuid NOT NULL
  tipo varchar(40) NOT NULL            -- TermosUso | PoliticaPrivacidade | UsoIA
  versao varchar(20) NOT NULL          -- "v1.0"
  aceito_em timestamptz NOT NULL DEFAULT now()
  ip_origem varchar(45) NULL
  user_agent varchar(500) NULL
```

**Job mensal `AnonimizarPacientesInativosJob`**:
- Critério: `paciente.atualizado_em < now() - INTERVAL '20 anos'` (CFM 1.821/07 — prontuário 20 anos) E `deletado_em IS NOT NULL`.
- Anonimização: substitui `nome`, `cpf`, `email`, `telefone` por hash determinístico ou string vazia. Mantém `id` e dados clínicos com referência (`paciente_id_anonimizado`).
- Registra em `lgpd_anonimizacoes`.

**Endpoints LGPD:**
- `GET /api/minha-conta/exportar-dados` — exporta tudo do usuário (JSON estruturado).
- `DELETE /api/minha-conta` — anonimização imediata (não delete físico).

### 4.13 Catálogo TUSS/CBHPM

```
catalogo_procedimentos
  id bigserial PK
  codigo varchar(20) NOT NULL UNIQUE     -- "30602.025" (formato TUSS)
  nome varchar(300) NOT NULL
  origem varchar(20) NOT NULL            -- TUSS | CBHPM | CUSTOMIZADO
  capitulo varchar(80) NULL
  ativo bool NOT NULL DEFAULT true
```

Seed inicial: 50-100 procedimentos mais comuns (cirurgias plásticas, endoscopias, consultas) com códigos TUSS reais.

### 4.16 Admin-reset bypass

Adicionar interface `IAdminResetService` com método `ResetEstabelecimentoAsync(estabId, motivo)` que:
- Apaga (hard delete) tudo do estabelecimento via SQL bruto (bypass do interceptor).
- Mantém o registro em `audit_delete_attempts` com `motivo = "ADMIN_RESET: <motivo>"`.
- Endpoint `POST /api/admin/estabelecimentos/{id}/reset` — apenas usuários com claim `imedto_admin = true` (ou similar).

---

## Itens detalhados

> Estrutura: **Status / Agente / Branch / Diff técnico / Aceite (DoD)** para cada item.

(Detalhamento item-por-item omitido na geração inicial — será expandido por agente conforme cada Wave dispara, na linha do que foi feito nas Fases 2 e 3.)

---

## Definition of Done — Fase 4

- [ ] 4.1 (relatórios) entregue com 4 handlers parametrizados.
- [ ] 4.2 (solicitação inversa) com endpoints + frontend.
- [ ] 4.3 (LGPD) com job + endpoints minha-conta.
- [ ] 4.4 + 4.5 (testes Fase 1/2 pendentes).
- [ ] 4.6, 4.7, 4.8, 4.9 (operacional/infra).
- [ ] 4.11 (PDF real receita).
- [ ] 4.12 (ANVISA receita).
- [ ] 4.13 (TUSS/CBHPM).
- [ ] 4.14 (permissões finas aplicadas).
- [ ] 4.16 (admin-reset).
- [ ] 4.10, 4.15 (operacional/produto — checklist).
- [ ] Migrations EF + supabase SQL geradas e aplicadas.
- [ ] RLS habilitada em todas as tabelas novas.
- [ ] `dotnet build` limpo + `dotnet test` verde + `npm run build` verde.
- [ ] `migration-engineer` revisão de paridade aprovada.
- [ ] Documento atualizado.
- [ ] **Status** atualizado em [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md).
- [ ] Gerar `05_FASE_5_MIGRACAO_DADOS.md` quando iniciar Fase 5 (migração ETL do legado).

## Status por item

| Item | Status | Aplicado no banco / Frontend |
|------|--------|------------------------------|
| 4.1 Relatórios consolidados | ✅ backend (4 handlers) | endpoints + 4 views frontend (Financeiro, Operacional, Pessoas, Orçamentos) |
| 4.2 Solicitação de vínculo inversa | ✅ backend + RLS | tabela `solicitacoes_vinculo` + UNIQUE parcial + frontend `SolicitarVinculoView` + `SolicitacoesRecebidasView` |
| 4.3 LGPD retenção/anonimização | ✅ backend + RLS | tabelas `lgpd_anonimizacoes`, `lgpd_consentimentos` + colunas `pacientes.anonimizado_*` + VIEW `lgpd_acesso_log` + Job mensal + frontend `MinhaContaLgpdView` |
| 4.4 Teste integração SoftDeleteInterceptor | ✅ | 11 testes integração (`SoftDeleteInterceptorTests`) |
| 4.5 Load test rate limit /auth | ✅ | k6 script + xUnit IntegrationTest |
| 4.6 Backplane Redis SignalR | ✅ | `Microsoft.AspNetCore.SignalR.StackExchangeRedis` + fallback gracioso |
| 4.7 Provedor email Resend | ✅ | `IEmailService` + `ResendEmailService` + `NoOpEmailService` fallback + integrado em `ExecutorAcao` + handlers de notificação |
| 4.8 Cookie scope / | ✅ | `access-token` agora `path=/` (SignalR + API) |
| 4.11 PDF real Receita | ✅ | QuestPDF 2025.4.0 (community license) |
| 4.12 ANVISA Receitas | ✅ | `RegrasAnvisa` helper + auto-cálculo + `requer_retencao` aplicado + backfill |
| 4.13 Catálogo TUSS/CBHPM | ✅ | tabela `catalogo_procedimentos` + 83 procedimentos seedados + `CodigoTussAutocomplete.vue` |
| 4.14 Permissões finas em 5 controllers | ✅ | `[RequiresPermissaoExtra]` em ModeloPermissao, Estabelecimento, Automacao, Vinculo, ProntuarioTemplate |
| 4.16 Admin-reset modular | ✅ | `ResetModulos` com 13 módulos opt-in + `ResetModulos.Tudo()` default + reseed pós-delete + endpoint dev-only |

## Resumo final da fase

### O que foi entregue

**Backend (13 itens):**
- 11 aggregates novos: `SolicitacaoVinculo` + 3 events, `LgpdAnonimizacao`, `LgpdConsentimento`, `ProcedimentoCatalogo`, `IAdminResetService` + `ResetModulos`, `IEmailService` + `ResendEmailService` + `NoOpEmailService`, `RegrasAnvisa` helper.
- 4 query handlers de relatórios consolidados (Financeiro, Operacional, Pessoas, Orçamentos) — mapeiam 9 RPCs legados.
- 5 controllers novos/estendidos: `RelatorioController`, `SolicitacaoVinculoController`, `MinhaContaController`, `LgpdConsentimentoController`, `AdminController`, `CatalogoController` estendido.
- Job `AnonimizarPacientesInativosJob` mensal (CFM 1.821/07 — 20 anos).
- Job `LimparCacheIaJob` 1h (já era da Fase 3).
- Filtro `[RequiresPermissaoExtra]` aplicado em 5 controllers premium.
- QuestPDF 2025.4.0 para PDF real de receita.
- ANVISA Portaria 344/98 + RDC 471/2021 com auto-cálculo de validade e flag `requer_retencao`.
- SignalR backplane Redis (com fallback para single-instance).

**Database:**
- 4 tabelas novas (`solicitacoes_vinculo`, `lgpd_anonimizacoes`, `lgpd_consentimentos`, `catalogo_procedimentos`).
- 1 VIEW `lgpd_acesso_log` (sobre `prontuario_acesso_log` — Art. 46 LGPD).
- 3 colunas novas (`pacientes.anonimizado_em/anonimizado_por_usuario_id`, `receitas.requer_retencao`).
- 83 procedimentos TUSS seedados.
- Backfill `requer_retencao = true` em receitas Controlada/Antibiótico.
- RLS habilitada em todas as 4 tabelas novas.

**Frontend:**
- 4 views de relatórios (Financeiro, Operacional, Pessoas, Orçamentos).
- 2 views de vínculo inverso (`SolicitarVinculoView`, `SolicitacoesRecebidasView`).
- `MinhaContaLgpdView` (export + delete + consentimentos).
- `CodigoTussAutocomplete.vue` para procedimentos cirúrgicos.
- 3 componentes novos no design system: `AppCheckbox`, `AppTabs`, `AppCollapsible`.
- 5 services novos: `relatorioService`, `solicitacaoVinculoService`, `lgpdService`, `catalogoService` estendido, `cirurgiaService`.

**Testes:** 205 unitários + 11 integração verdes.

### Bloqueadores de paridade detectados pela revisão (resolvidos)

A revisão pelo `migration-engineer` detectou 3 bloqueadores ALTO + 6 menores:

**Bloqueadores resolvidos:**
1. ✅ **Admin-reset modular** — `IAdminResetService.ResetEstabelecimentoAsync` agora aceita `ResetModulos` com 13 flags opt-in + reseed pós-delete (modelos de permissão + financeiro). `AdminController` aceita body `{ motivo, modulos }`. `ResetModulos.Tudo()` como default.
2. ✅ **lgpd_acesso_log** — VIEW criada apontando para `prontuario_acesso_log`. Cobre Art. 46 LGPD com nomenclatura canônica.
3. ✅ **solicitacoes_vinculo no AdminReset** — incluída no módulo `Vinculos`.

**Postergados (médios/baixos — Fase 5 ou sub-iteração futura):**
- Item 4.1 — Validar com produto se 4 handlers cobrem todas as telas legadas de `reports/views`. Possível necessidade de mais 1-2 handlers específicos.
- Item 4.12 — Validar com produto se travar receitas > 90 dias quebra reimpressão de receitas legadas (ETL Fase 5).
- Item 4.14 — Doc-comment de `PermissoesExtras.cs` cita "13 chaves" mas legado tem 16 (10 áreas + 6 finas). Apenas comment.
- Item 4.13 — Garantir que `procedimento_id` aceita NULL (já aceita) + UI tolera ausência de código TUSS para dados migrados.
- Item 4.3 — Validar regra "20 anos CFM 1.821/07" com jurídico antes de habilitar job em produção.
- Item 4.11 — QA visual do PDF QuestPDF vs `useReceitaPDF.ts` legado.

### Mapeamento ETL Fase 5 (documentação preventiva)

Tabela legada `solicitacao_vinculo_profissional_estabelecimento` → tabela nova `solicitacoes_vinculo`. Campos a mapear na Fase 5:
- `profissional_id` (legado) → `profissional_usuario_id` (uuid).
- `estabelecimento_id` → `estabelecimento_id` (bigint).
- Status legado → enum novo (`Pendente`/`Aprovada`/`Recusada`/`Cancelada`).

### Build & testes finais

- `dotnet build`: 0 errors.
- `dotnet test` unit: **205 verdes**.
- `dotnet test` integration: **11 verdes**.
- `npm run build`: limpo.

### Próxima ação

**Iniciar Fase 5 — Migração de dados** (greenfield ETL do projeto Supabase legado para o schema novo). Estratégias possíveis:
- **A. Strangler Fig**: dual-read enquanto migra incrementalmente.
- **B. Big-bang ETL**: janela de manutenção com cutover total.
- **C. Dual-write**: app novo escreve em ambos schemas durante transição.

Doc a gerar: `05_FASE_5_MIGRACAO_DADOS.md`.
