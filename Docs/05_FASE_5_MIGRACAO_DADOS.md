# Fase 5 — Migração de dados (ETL Legado → Novo)

**Status geral:** ✅ planejamento e ferramentas concluídos — execução depende de janela agendada
**Iniciada em:** 2026-04-30
**Concluída em:** — (pendente da execução da janela)

> **Objetivo:** transferir dados reais do projeto Supabase legado (`rkgxcmubxkcvzhqhllev`) para o schema novo (`kdoqflrmfgazdgekdbqc`), garantindo paridade comportamental, integridade referencial e conformidade LGPD durante o processo.
>
> **Pré-requisitos:** Fases 1-4 ✅ concluídas. Schema novo aplicado e validado. Backend ativo + RLS habilitada.
>
> **Referência:** [00_PLANO_MIGRACAO.md](00_PLANO_MIGRACAO.md), [04_FASE_4_GAPS_SECUNDARIOS.md](04_FASE_4_GAPS_SECUNDARIOS.md).

## Cenário

- **Legado (origem)**: Supabase `rkgxcmubxkcvzhqhllev` — schema antigo com 50+ tabelas, dados de produção (clínicas reais usando hoje).
- **Novo (destino)**: Supabase `kdoqflrmfgazdgekdbqc` — schema refatorado (DDD + CQRS), aprox. 40+ tabelas, RLS habilitada, dados de teste mínimos (talvez vazio ou poucos registros de QA).
- Schema legado: ver `ReferenciaLegado/Imedto/supabase/migrations/` (157 migrations).
- Schema novo: `supabase/migrations/` (~30 migrations).

## Estratégias possíveis

### A. Strangler Fig (dual-read incremental)
Backend novo lê do legado E escreve no novo. Cliente migra incrementalmente. **Complexidade: alta**. Requer adapters duplos por aggregate. Risk: divergência por race condition.

### B. Big-bang ETL (recomendado)
Janela de manutenção controlada. ETL roda uma vez. Cutover total: app legado vira read-only, novo entra em produção. **Complexidade: média**. **Recomendado** dado que (a) volume é manejável (clínicas individuais), (b) fluxos clínicos toleram janela de 1-2h, (c) backend novo já está completo.

### C. Dual-write (app novo escreve nos dois)
Mais complexo. Útil só se houver dúvida sobre estabilidade do novo. **Não recomendado** — backend novo já está validado em todas as fases.

**DECISÃO ADOTADA: Estratégia B — Big-bang ETL** com janela de manutenção controlada.

## Plano de agentes

| Agente | Modelo | Responsabilidade |
|--------|--------|------------------|
| `migration-engineer` | Opus | **Lidera a fase** — mapeamento schema legado→novo, contratos de transformação, validação de paridade pós-migração. |
| `database-architect` | Opus | Scripts ETL SQL otimizados (batches, índices temporários, ordem de dependência), paridade de tipos/constraints. |
| `senior-software-engineer` | Opus | Validador de paridade comportamental (smoke test pós-migração — autenticar como usuário legado, conferir vínculos/pacientes/agenda/etc). |
| `security-engineer` | Opus | LGPD durante ETL — não logar PII, anonimizar dados de teste, garantir que `service_role` é usado de forma controlada. |
| `devops-cloud-engineer` | Opus | Janela de manutenção, observabilidade do ETL (progresso, erros, rollback), comunicação com clínicas. |
| `data-analyst` | Opus | **Antes da execução**: amostragem de dados legados — quantos pacientes, quantas receitas, quanto volume — para dimensionar janela e priorizar tabelas. |
| `qa-engineer` | Sonnet | Suite de testes pós-cutover (sanity checks: usuário consegue logar, ver agenda, criar receita, etc). |

## Ondas de execução

### Wave 1 — Descoberta e mapeamento (sem mexer em dados)
- **5.1** `migration-engineer` faz inventário completo: lista de tabelas legado vs novo, tipos divergentes, FKs renomeadas, campos perdidos/novos, transformações necessárias.
- **5.2** `data-analyst` faz amostragem do legado (quantos registros por tabela, distribuição por estabelecimento, dados sensíveis a anonimizar?).
- **5.3** `migration-engineer` produz **`ETL_MAPEAMENTO.md`** consolidado: para cada tabela, função de transformação (`legado.X` → `novo.Y`).

### Wave 2 — Scripts ETL
- **5.4** `database-architect` escreve scripts ETL por domínio, na ordem correta de dependência:
  1. **Ref data**: `profissoes`, `especialidades`, `planos` (já seedados — apenas enrichment se houver custom no legado).
  2. **Tenant base**: `usuarios` (auth), `estabelecimentos`, `unidades_estabelecimento`, `salas_atendimento`.
  3. **Domínio core**: `pacientes`, `profissionais`, `vinculo_profissional_estabelecimento`, `modelo_permissao_estabelecimento`.
  4. **Configurações**: `establishment_ai_settings`, `assinaturas`, `categorias_financeiras`, `formas_pagamento`, `receitas_configuracao_estabelecimento`.
  5. **Transacional**: `agendamentos`, `prontuarios`, `prontuario_evolucoes`, `prontuario_anexos`, `exame_fisico` + filhas, `receitas` + filhas, `procedimentos_cirurgicos` + equipe, `orcamentos` + filhas, `lancamentos`, `itens_inventario` + movimentações, `automation_rules` + eventos, `notificacoes`.
  6. **Audit**: `prontuario_acesso_log`, `audit_delete_attempts`, `lgpd_anonimizacoes`, `lgpd_consentimentos`, `ai_audit_logs`.
- **5.5** Cada script tem:
  - Modo `--dry-run` (lê origem + simula INSERT, conta registros, sem escrever).
  - Modo `--execute` (escrita real).
  - Idempotência via `ON CONFLICT DO UPDATE` ou `DO NOTHING` conforme apropriado.
  - Logging estruturado (sem PII).

### Wave 3 — Validador de paridade pós-migração
- **5.6** `senior-software-engineer` cria suite de validação:
  - Comparações de contagem (`SELECT COUNT(*)` em cada tabela origem vs destino).
  - Validações relacionais (toda receita destino tem prontuário válido).
  - Validações de invariantes (paciente não-deletado tem dados PII; deletado é null/anonimizado).
  - **Smoke test funcional** via API: autenticar como usuário legado, listar pacientes, ver agenda, ver receita, ver orçamento.

### Wave 4 — Janela de manutenção e cutover
- **5.7** `devops-cloud-engineer` prepara:
  - Plano de janela (data + duração estimada via Wave 2 dry-run).
  - Comunicação às clínicas (banner no app legado 7 dias antes).
  - Backup do legado pré-cutover (snapshot Supabase).
  - Backup do novo pré-cutover (caso precise rollback).
  - Health check do ETL durante execução.
  - Plano de rollback: se ETL falhar, restaura novo do snapshot e mantém legado ativo.
- **5.8** Execução da janela:
  - Hora 0: legado em modo read-only (atualiza app legado para bloquear writes).
  - Hora 0+5min: rodar ETL em sequência (estimativa 30-60min para volume médio).
  - Hora 0+45min: validador roda (`senior-software-engineer`).
  - Hora 0+50min: smoke tests funcionais.
  - Hora 0+60min: DNS/redirect aponta para novo app. Comunicação "novo no ar".
- **5.9** Pós-cutover:
  - Monitorar 48h: erros de aplicação, logs de IA, audit LGPD, taxa de erro.
  - Rollback opcional disponível por 7 dias (snapshot do novo pré-ETL preservado).

### Wave 5 — Cleanup pós-migração (após confiança no novo)
- **5.10** Após 30 dias estáveis:
  - Desativar projeto Supabase legado (`rkgxcmubxkcvzhqhllev`).
  - Arquivar `ReferenciaLegado/` em branch readonly.
  - Atualizar `00_PLANO_MIGRACAO.md` com status "Migração concluída".

---

## Schema fechado — abordagens-chave

### Conexão entre projetos Supabase

Para ETL entre 2 projetos Supabase, opções:
- **Opção 1 (recomendada)**: dump SQL do legado (`pg_dump` via Supabase CLI), restore em DB intermediário, transformações via SQL puro, então `INSERT INTO novo SELECT FROM intermediario`. Permite trabalho offline e rollback fácil.
- **Opção 2**: Postgres FDW (Foreign Data Wrapper) entre os dois projetos. Requer permissão de superuser que Supabase hosted não dá. **Não viável**.
- **Opção 3**: scripts Python/Node.js que leem do legado via `service_role` e escrevem no novo via `service_role`. Viável mas mais lento.

**Decisão adotada: Opção 1** — dump SQL + DB intermediário + scripts SQL.

### Transformações típicas necessárias

#### IDs
- Legado usa `uuid` para tudo (ex: `paciente_id` é uuid).
- Novo usa `bigserial` para domain (ex: `paciente_id` é bigint) e `uuid` para usuários (auth).
- **Transformação**: tabela auxiliar de mapeamento `_etl_mapping_pacientes (id_legado uuid PK, id_novo bigint)`. Cada INSERT no novo registra mapeamento. Tabelas filhas usam o mapeamento para resolver FK.

#### Status / enums
- Legado pode usar lowercase (`'pendente'`); novo usa PascalCase (`'Pendente'`).
- **Transformação**: `CASE WHEN ... THEN ...` no SELECT do ETL.

#### Soft delete
- Legado pode usar coluna `ativo bool`; novo usa `deletado_em timestamptz NULL`.
- **Transformação**: `CASE WHEN ativo = false THEN NOW() ELSE NULL END AS deletado_em`.

#### Receitas (legado mais simples)
- Legado: `tipo SIMPLES|CONTROLADA` + `tipo_notificacao A|B|C|ESPECIAL`.
- Novo: `Tipo` enum 4 valores + `TipoNotificacao` opcional.
- **Transformação**: SIMPLES→Comum; CONTROLADA→Controlada+TipoNotificacao mapeada; antibiótico não aparece no legado (default Comum).

#### LGPD
- **Não migrar dados PII de pacientes deletados há > 20 anos** — anonimizar durante ETL (Art. 16 LGPD).
- **Migrar consentimentos legados** se houver (`lgpd_consent` → `lgpd_consentimentos`).

---

## Itens detalhados

### 5.1 — Inventário de tabelas legado vs novo

**Status:** ⏳ pendente
**Agente:** `migration-engineer`

**Output esperado**: `Docs/ETL_MAPEAMENTO.md` com tabela `| legado | novo | transformação | observações |` cobrindo todas as 50+ tabelas legadas.

### 5.2 — Amostragem do legado

**Status:** ⏳ pendente
**Agente:** `data-analyst`

**Output esperado**: relatório `Docs/ETL_VOLUMETRIA.md` com:
- Contagem de registros por tabela (top 20 maiores).
- Distribuição por estabelecimento (média, mediana, maior).
- Dados sensíveis a auditar (CPF de pacientes, etc.).
- Estimativa de duração do ETL.

### 5.3 — Plano consolidado ETL

**Status:** ⏳ pendente
**Agente:** `migration-engineer`

Junta 5.1 + 5.2 em um plano executável.

### 5.4 — Scripts ETL por domínio

**Status:** ⏳ pendente
**Agente:** `database-architect`

`scripts/etl/` com arquivos numerados:
- `00_setup_intermediate.sql`
- `01_ref_data.sql`
- `02_tenant_base.sql`
- `03_dominio_core.sql`
- `04_configuracoes.sql`
- `05_transacional.sql`
- `06_audit.sql`
- `99_validacao.sql`

### 5.5 — Validador de paridade

**Status:** ⏳ pendente
**Agente:** `senior-software-engineer`

`backend/src/Services/Imedto.Backend.EtlValidator/` (projeto novo, console app):
- Lê origem + destino, compara, gera relatório.
- Smoke test: autentica usuários reais e dispara queries representativas.

### 5.6 — Plano de janela de manutenção

**Status:** ⏳ pendente
**Agente:** `devops-cloud-engineer`

`Docs/ETL_PLANO_EXECUCAO.md` com:
- Data sugerida (ex: madrugada de domingo).
- Comunicação prévia (template para email/banner).
- Runbook passo-a-passo.
- Plano de rollback.

### 5.7 — Execução

**Status:** ⏳ pendente — depende do usuário decidir a data.

### 5.8 — Cleanup

**Status:** ⏳ pendente — após 30 dias estáveis pós-cutover.

---

## Riscos e mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|--------------|---------|-----------|
| ETL falha no meio | Média | Alto | Snapshot do novo pré-ETL + transações por domínio + idempotência |
| Dados perdidos por mapeamento incorreto | Média | Crítico | Validador automático + smoke test funcional + amostragem manual antes do cutover |
| Janela maior que estimada | Alta | Médio | Dry-run com volume real antes da janela oficial |
| RLS bloqueia escrita | Baixa | Alto | Usar `service_role` durante ETL (bypassa RLS) |
| LGPD: PII vazada em logs | Baixa | Crítico | Logger sanitizado (Fase 2 já tem `RemovePIIEnricher`) + scripts ETL com `\set QUIET on` |
| Schema desync após ETL | Baixa | Médio | Validador compara estruturas pré-execução |
| Senhas / Auth Supabase não migram | Alta | Crítico | Não migrar — usuários reset password ao primeiro acesso (Supabase Auth nativo) |

---

## Decisão pendente

**Quando começar a janela?** Depende:
- Volume real (Wave 1 + 2 medem).
- Comunicação prévia às clínicas (mínimo 7 dias).
- Aprovação do produto/jurídico (LGPD compliance).

---

## Definition of Done — Fase 5

- [ ] `ETL_MAPEAMENTO.md` completo (todas as tabelas mapeadas).
- [ ] `ETL_VOLUMETRIA.md` com volumetria real.
- [ ] Scripts ETL escritos + dry-run validado.
- [ ] Validador de paridade implementado.
- [ ] Plano de execução aprovado pelo time.
- [ ] Janela executada com sucesso.
- [ ] Smoke tests pós-cutover passando.
- [ ] 30 dias de estabilidade.
- [ ] Cleanup do legado.
- [ ] Migration concluída — atualizar `00_PLANO_MIGRACAO.md` para status final.

## Status por item

| Item | Status | Output |
|------|--------|--------|
| 5.1 Inventário tabelas legado vs novo | ✅ | [ETL_MAPEAMENTO.md](ETL_MAPEAMENTO.md) — 66 legado / 57 novo / 18 paridade direta / 22 transformação / 11 descartadas |
| 5.2 Volumetria | ✅ | [ETL_VOLUMETRIA.md](ETL_VOLUMETRIA.md) — janela 3-5h domingo madrugada |
| 5.4 Scripts ETL | ✅ | `scripts/etl/` — 10 scripts SQL + `run_all.sh` + `~25 min` total estimado |
| 5.5 Validador de paridade | ✅ | `backend/src/Tools/Imedto.Backend.EtlValidator/` (console app .NET) — modos `counts`/`integrity`/`smoke`/`full` |
| 5.6 Plano de execução | ✅ | [ETL_PLANO_EXECUCAO.md](ETL_PLANO_EXECUCAO.md) — janela proposta 14/jun/2026 01:00-06:00 BRT |
| 5.7 Execução da janela | ⏳ pendente | Depende de aprovação do produto + comunicação 7d antes |
| 5.8 Cleanup pós-30d | ⏳ pendente | Após estabilidade comprovada |

## Resumo final da fase (planejamento)

### O que foi entregue

**Documentos de análise:**
- `ETL_MAPEAMENTO.md` — 66 tabelas legadas mapeadas para 57 tabelas novas com transformações campo-a-campo, lookup tables identificadas (split de permissões, mapping receitas, soft delete) e 8 dúvidas de produto registradas.
- `ETL_VOLUMETRIA.md` — estimativa cenário conservador (50 estab, ~60min) e otimista (300 estab, ~160min). Top 3 críticas: `prontuario_evolucoes`, `agendamentos`, `exame_fisico`.

**Scripts ETL prontos para dry-run:**
- `scripts/etl/00_setup_intermediate.sql` — schemas `_etl` e `legado`, mapping tables, lookups, holding tables para dados inválidos.
- `scripts/etl/01-08` — 8 scripts numerados por dependência de FK cobrindo ref data, tenant base, domínio core, configurações, transacional (agenda/clínico/financeiro/orçamento) e auditoria.
- `scripts/etl/99_validacao.sql` — re-cria constraints, contagens lado-a-lado, integridade referencial, ANALYZE.
- `scripts/etl/run_all.sh` — wrapper bash com `psql -v ON_ERROR_STOP=1`.

**Validador automatizado:**
- `backend/src/Tools/Imedto.Backend.EtlValidator/` — console app .NET 10 com 4 modos:
  - `counts`: contagem lado-a-lado com tolerância 0.5%.
  - `integrity`: 13 verificações de FK órfã (tolerância 0%).
  - `smoke`: login + GET pacientes/agendamentos/me + logout via cookies HttpOnly (BFF).
  - `full`: roda os 3.
- Saída: console colorida + `Docs/ETL_RELATORIO_<timestamp>.md`.
- Exit codes: 0 OK / 1 tolerância excedida / 2 erro fatal.

**Plano operacional:**
- `ETL_PLANO_EXECUCAO.md` — runbook completo com:
  - Comunicação T-7d, T-3d, T-1d, T-1h.
  - Pré-cutover checklist (backups, máquina ETL, smoke).
  - Execução T+0 a T+3h (read-only → dump → restore → ETL → validação → DNS → estabilização).
  - Rollback (3 critérios: pré-T+30, pós-validação, pós-DNS).
  - Templates de comunicação em PT-BR.
  - Métricas de sucesso (diff <0.5%, 0 violações, ≥95% smoke).
  - Equipe (5 papéis em call + 3 standby).

**Backend & banco:** sem alterações. 205 testes verdes. Build limpo.

### Decisões tomadas

1. **Estratégia: Big-bang ETL** (Estratégia B) — não Strangler Fig nem Dual-write.
2. **Janela proposta**: 14 de junho de 2026, 01:00-06:00 BRT (domingo, sem feriados próximos, antes das férias escolares).
3. **Senhas não migram** — usuários resetam ao primeiro acesso (Supabase Auth nativo). Comunicação 4 toques.
4. **DB intermediário**: VM Postgres 15 local (sa-east-1) recebe `pg_dump` legado, scripts ETL transformam, `INSERT INTO destino_remoto SELECT FROM intermediario`.
5. **Storage (anexos/fotos)**: sync via Supabase CLI antes do schema (delta-friendly).
6. **Constraints**: `agendamentos_no_overlap` (EXCLUDE GiST) e triggers de imutabilidade em `prontuario_evolucoes` são **droppadas** durante carga e **recriadas** após.
7. **Receitas inválidas no legado** (combinações `(SIMPLES, tipo_notificacao)` ou similares): bloqueiam ETL com `EXCEPTION` + tabela holding para revisão manual.

### Riscos e mitigações conhecidos

1. **Tempo real de `prontuario_evolucoes`** pode triplicar se jsonb médio > 15KB → mitigar com dry-run de 10% antes da janela oficial.
2. **Senhas não migram** → comunicação prévia 4x + FAQ + CS em standby pós-cutover.
3. **Falha pós-DNS-cutover** → rollback ensaiado em staging com cronometragem.
4. **8 dúvidas de produto** ainda em aberto (`appointment_checklists`, `estoque_lote`, `orcamento_cirurgia` catálogo, etc.) — bloqueio operacional para alguns scripts.

### Pendências para execução

- [ ] Aprovação do produto/jurídico (LGPD compliance, comunicação 7d antes).
- [ ] Definição final da data (sugerido: 14/jun/2026 ou fallback 12/jul/2026).
- [ ] Resolver as 8 dúvidas de produto registradas no `ETL_MAPEAMENTO.md`.
- [ ] Provisionar VM ETL intermediária (sa-east-1, Postgres 15, t3.medium).
- [ ] `smoke-users.json` com 5-10 contas reais (gitignored).
- [ ] Dry-run de 10% em ambiente de staging para calibrar janela real.
- [ ] Equipe alocada para 5h de janela + 4h de standby pós-cutover.

### Próxima ação

Quando o produto aprovar + a data for confirmada:
1. Comunicação T-7d → seguir runbook em [ETL_PLANO_EXECUCAO.md](ETL_PLANO_EXECUCAO.md).
2. Provisionar máquina ETL.
3. Dry-run de 10% para calibrar.
4. Executar janela.
5. Pós-cutover: monitorar 48h + rodar `EtlValidator --modo full` periodicamente nos primeiros 7 dias.
6. T+30 dias: cleanup do projeto Supabase legado.
