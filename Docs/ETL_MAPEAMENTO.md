# ETL Mapeamento — Legado → Novo

**Origem:** Supabase `rkgxcmubxkcvzhqhllev` (projeto legado, schema definido em [ReferenciaLegado/Imedto/supabase/migrations/](../ReferenciaLegado/Imedto/supabase/migrations/), 157 migrations)
**Destino:** Supabase `kdoqflrmfgazdgekdbqc` (projeto novo, schema definido em [supabase/migrations/](../supabase/migrations/), 41 migrations)
**Estratégia:** Big-bang ETL via `pg_dump` da origem → DB intermediário (staging) → transformação SQL → `INSERT` ordenado no destino, com mapping tables `_etl_mapping_<entidade>` para preservar relações `uuid_legado ↔ id_novo` (uuid).

> **Observação importante:** apesar do destino ter sido autorado pelo EF Core (`bigint` em alguns enums e configs), as PKs de domínio continuam `uuid` — confirmado em [list_tables](../supabase/migrations/) e nos `EntityTypeConfiguration` do backend. Portanto a mapping table só é necessária quando uma coluna de referência sofre split/merge ou quando a chave foi recriada (caso `usuarios`, que passa a apontar para `auth.users.id`).

## Sumário executivo

- **Total de tabelas legado vivas:** 66 (após descartar 6 tabelas explicitamente DROPadas em migrations posteriores).
- **Total de tabelas novo:** 57 (excluindo `__ef_migrations_history`).
- **Paridade direta (rename simples ou copia 1:1 de campos):** 18.
- **Transformação significativa (split, merge, normalização, mudança de domínio):** 22.
- **Tabelas legado a descartar (dado não migra):** 11.
- **Tabelas novo sem origem (seed/dados-padrão a manter):** 9.

## Convenções do mapeamento

- ✅ paridade · 🟡 transformação · ⛔ não migra
- "direct copy" = mesmo tipo, mesmo significado, mesmo nome (ou rename trivial).
- "mapping" = lookup em `_etl_mapping_<entidade>` para resolver FK.
- "discard" = coluna existe no legado mas não no novo — dado **descartado conscientemente**.
- "default" = coluna nova sem origem — usa default do schema ou valor calculado.

---

## Mapeamento detalhado

### 1. Identidade e tenant

#### `usuarios` → `usuarios` 🟡
- **Volume estimado:** ~100 usuários (donos + profissionais).
- **Transformações:**

| Campo legado | Campo novo | Transformação |
|---|---|---|
| `id (uuid, FK auth.users)` | `id (uuid, FK auth.users)` | **Senhas NÃO migram.** Auth é recriada no novo Supabase via convite/reset. Mapping `_etl_mapping_usuarios(legado_id, novo_auth_id)` resolvido após reenvio dos e-mails. |
| `nome_completo` | `nome_completo` | direct copy |
| `cpf_cnpj` | `cpf_cnpj` | direct copy (já único + criptografia LGPD aplicada no backend) |
| `tipo_pessoa` | `tipo_pessoa` | direct copy (`PF`/`PJ`) |
| `onboarding_concluido` | `onboarding_concluido` | direct copy |
| `tutorial_visto` | `tutorial_visto` | direct copy |
| `created_at` | `criado_em` | rename |
| `updated_at` | `atualizado_em` | rename |

- **Decisão LGPD:** usuários sem login há >24 meses **não migram** (não há base legal para manter). Filtro: `WHERE last_sign_in_at > now() - interval '24 months'`.
- **Risco:** sem o `auth.users` recriado, o convite de senha é o ponto de falha — comunicar usuários antes do cutover.

#### `estabelecimentos` → `estabelecimentos` ✅
- **Volume:** ~50 estabelecimentos.
- Direct copy de `nome_fantasia, razao_social, tipo_pessoa, cpf_cnpj, owner_usuario_id (mapping), foto_url`. `created_at→criado_em`, `updated_at→atualizado_em`.
- **Campo novo:** `funcionamento jsonb` (default `'{}'::jsonb` — sem origem, preenchimento manual posterior; ver [20260425043444_adicionar_funcionamento_estabelecimento.sql](../supabase/migrations/20260425043444_adicionar_funcionamento_estabelecimento.sql)).
- **Endereço:** veio como colunas no legado ([20251123130000_estabelecimento_endereco.sql](../ReferenciaLegado/Imedto/supabase/migrations/20251123130000_estabelecimento_endereco.sql)) — mapear cada campo direto.

#### `unidades_estabelecimento` → `unidades_estabelecimento` ✅
- **Volume:** ~80 (1.5 unidades / estabelecimento em média).
- Direct copy. Garantir que toda unidade legada com flag "principal" mantenha `is_principal=true` no novo.

### 2. Profissional + vínculos

#### `profissionais` → `profissionais` 🟡
- **Volume:** ~150.
- **Mudança crítica de modelagem:** legado usa `id (uuid)` próprio + `usuario_id (uuid, FK)` opcional; novo é **1:1 com `usuarios`**, ou seja, `profissionais.usuario_id` **é a PK** (ver comment em `list_tables`).
- Profissionais legado **sem `usuario_id`** (convidados que nunca aceitaram) **não migram** — vão como linhas em `solicitacoes_vinculo` se a solicitação ainda estiver `pendente`, descartados se expirados.
- Campos: `nome_exibicao, profissao_id (mapping global), registro_profissional, conselho, uf_conselho, especialidade_id, foto_url, rqe`.

#### `vinculo_profissional_estabelecimento` → `vinculo_profissional_estabelecimento` 🟡
- **Volume:** ~250.
- Direct copy de `estabelecimento_id, profissional_id, modelo_permissao_estabelecimento_id, is_admin`.
- **Transformação de status:** legado tem `ativo bool`; novo tem fluxo enum `Convidado → Ativo → Inativo`. Mapeamento: `ativo=true → 'Ativo'`, `ativo=false → 'Inativo'`. Convites pendentes não vivem nessa tabela (vão pra `solicitacoes_vinculo`).
- **Salário:** [20251120142000_vinculo_salarios.sql](../ReferenciaLegado/Imedto/supabase/migrations/20251120142000_vinculo_salarios.sql) adicionou colunas de remuneração — copiar direto.

#### `solicitacao_vinculo_profissional_estabelecimento` → `solicitacoes_vinculo` 🟡
- **Volume:** ~30 ativas.
- Rename de tabela. Campos: `email_profissional, status, mensagem, profissao_id, especialidade_id, modelo_permissao_estabelecimento_id` direct copy.
- **Filtro:** apenas linhas com `status='pendente'` migram. `aceito|recusado|cancelado` são descartadas (já consumidas).

#### `modelo_permissao_estabelecimento` → `modelo_permissao_estabelecimento` 🟡 (CRÍTICO)
- **Volume:** ~150 modelos.
- **Maior transformação semântica do projeto.** Legado tem **um único** `permissoes jsonb` array com 16 chaves planas. Novo split em duas colunas:
  - `permissoes (jsonb)` — 10 áreas de alto nível (`agenda`, `pacientes`, `prontuarios`, `financeiro`, `inventario`, `orcamentos`, `relatorios`, `configuracoes`, `equipe`, `automacoes`).
  - `permissoes_extras (jsonb)` — 6 permissões finas (`prontuario_editar_terceiros`, `agenda_ver_outros_profissionais`, `financeiro_ver_valores`, `inventario_movimentar`, `orcamento_aprovar`, `equipe_convidar`).
- **Transformação:** mapping table `etl_permissao_legado_para_novo` (definida em SQL na Wave 2) que recebe array legado e produz `(permissoes, permissoes_extras)`. Chaves legadas que não têm equivalente direto (ex: `dashboard_admin`) viram `permissoes_extras` específicos; chaves removidas (ex: `super_admin` se existir) são descartadas com aviso.
- **Tipo acesso:** [20260418230839_add_tipo_acesso_to_modelo_permissao.sql](../supabase/migrations/20260418230839_add_tipo_acesso_to_modelo_permissao.sql) introduz `tipo_acesso` no novo — para registros legados, default `'Total'` se `is_admin` ou todas as permissões marcadas, senão `'Restrito'`.
- **Risco:** revisão humana obrigatória — produto precisa validar mapping antes do cutover.

### 3. Pacientes

#### `pacientes` → `pacientes` 🟡
- **Volume:** ~5.000 (50 estabelecimentos × 100 pacientes médio).
- **Mudança de tenant:** legado tem `pacientes` global + tabela ponte `paciente_estabelecimento` (M:N) que **foi DROPADA** em [20251120170000_drop_paciente_estabelecimento.sql](../ReferenciaLegado/Imedto/supabase/migrations/20251120170000_drop_paciente_estabelecimento.sql) — depois disso o legado adicionou `estabelecimento_id` direto ([20251120168000_pacientes_por_estabelecimento.sql](../ReferenciaLegado/Imedto/supabase/migrations/20251120168000_pacientes_por_estabelecimento.sql)). Novo já é multi-tenant nativo (`estabelecimento_id NOT NULL`). Direct copy.
- **Telefones:** legado tem `telefone, telefone_celular, telefone_fixo` (split em [20251120171000_pacientes_telefones_duplos.sql](../ReferenciaLegado/Imedto/supabase/migrations/20251120171000_pacientes_telefones_duplos.sql)). Novo tem só `telefone_celular, telefone_fixo`. Descartar `telefone` legado (já backfilled em `telefone_celular`).
- **PII LGPD:** ao migrar, preservar criptografia (já aplicada via [20251209100000_lgpd_encryption_functions.sql](../ReferenciaLegado/Imedto/supabase/migrations/20251209100000_lgpd_encryption_functions.sql)) — backfill no novo via funções `pgsodium` equivalentes.

### 4. Prontuário

#### `modelo_de_prontuario` → `modelo_de_prontuario` ✅
- **Volume:** ~200.
- Direct copy de `nome, descricao, estrutura jsonb, estabelecimento_id, is_default`.

#### `prontuario_variaveis_pool` → `prontuario_variaveis_pool` ✅
- **Volume:** ~10k linhas (alergias, medicamentos, doenças, cirurgias).
- Direct copy.

#### `prontuarios` → `prontuarios` ✅
- **Volume:** ~5.000.
- Direct copy de `paciente_id, estabelecimento_id, modelo_de_prontuario_id`.

#### `evolucao_prontuario` → `prontuario_evolucoes` 🟡
- **Volume:** ~30.000 evoluções.
- Rename de tabela. Direct copy de `prontuario_id, estabelecimento_id, paciente_id, profissional_id, evento_de_agendamento_id, modelo_de_prontuario_id, conteudo (jsonb), criado_em`.
- **Campo novo:** `template_snapshot jsonb` — para registros legados, copiar `modelo_de_prontuario.estrutura` no momento do `criado_em` se conseguirmos versionar; senão usar `'{}'::jsonb` e marcar `migrado_legado=true` em audit.
- **Append-only:** triggers `BEFORE UPDATE/DELETE RAISE EXCEPTION` no novo precisam estar **desabilitadas durante o ETL** e reabilitadas após carga.

#### `exame_fisico` → `exame_fisico` 🟡
- **Volume:** ~5.000.
- Direct copy de `prontuario_id, estabelecimento_id, profissional_id, paciente_id, evolucao_prontuario_id (mapping para prontuario_evolucoes_id), dados_gerais, regioes_examinadas, observacoes`.
- **Atenção:** confusão de nomenclatura — ver `exame_fisico_regioes` abaixo.

#### `exame_fisico_regioes` → ⛔ **NÃO MIGRA dados de usuário** (apenas seed)
- **Por quê:** no legado essa tabela é **catálogo de regiões anatômicas** (com `pai_id`, `nivel`, `svg_coords`). No novo, `exame_fisico_regioes` é **achados de exame** vinculados a `exame_fisico_id` (ver [Doc 04](04_FASE_4_PRONTUARIO_AVANCADO.md) revisão de paridade). **São tabelas com o mesmo nome mas conceitos diferentes.**
- **Catálogo legado** → corresponde no novo a `regioes_anatomicas_catalogo` (já seedada com 144 linhas no destino). Personalizações por estabelecimento (`estabelecimento_id IS NOT NULL`) **migram** se quisermos preservar customizações.
- **Achados de exame** legado: **não existiam como tabela**. Eram serializados no `regioes_examinadas jsonb` da tabela `exame_fisico`. No novo, podem **opcionalmente** ser desnormalizados em linhas — decisão de produto. **Default: deixar em `regioes_examinadas jsonb` para manter paridade comportamental.**

#### `prontuario_acesso_log` (novo, sem legado direto) ⛔
- Novo tem [20260418221604_create_prontuario_acesso_log.sql](../supabase/migrations/20260418221604_create_prontuario_acesso_log.sql); legado tem `lgpd_access_log` (genérico). **Não migrar** logs antigos (audit trail é forward-only — perda aceita; logs legado ficam no projeto antigo como evidência LGPD pelo período legal).

#### `prontuario_anexos` ⛔ (sem origem direta — anexos viviam em Storage)
- Migração de **arquivos** (não DB): copiar bucket Supabase Storage `imedto-anexos` legado → novo, e popular metadata em `prontuario_anexos` lendo o storage do novo após a cópia. Detalhes na Wave 2.

### 5. Agenda

#### `evento_de_agendamento` → `agendamentos` 🟡
- **Volume:** ~24.000 (50 estabelecimentos × 40 agendamentos/mês × 12 meses).
- Rename de tabela. Direct copy de `estabelecimento_id, profissional_id, paciente_id, data_hora_inicio, data_hora_fim, status, titulo, observacoes`.
- **Campos adicionados pelo legado** ([20251120152000_agenda_campos_paciente_especialidade.sql](../ReferenciaLegado/Imedto/supabase/migrations/20251120152000_agenda_campos_paciente_especialidade.sql), [20251120161000_agenda_tipo_consulta.sql](../ReferenciaLegado/Imedto/supabase/migrations/20251120161000_agenda_tipo_consulta.sql)): `especialidade_id, tipo_consulta, paciente_nome_avulso` etc — copiar direto.
- **Status:** legado tem `agendado/confirmado/cancelado/concluido`. Novo (ver [20260418230100_create_agendamentos.sql](../supabase/migrations/20260418230100_create_agendamentos.sql)) usa enum análogo — checar valores e mapear 1:1.
- **Constraint anti-overlap:** [20260429030000_overlap_agenda_constraint.sql](../supabase/migrations/20260429030000_overlap_agenda_constraint.sql) precisa estar desabilitada durante o ETL (legado pode ter overlaps históricos) — após carga, validar e abrir tickets para os conflitos.

#### `appointment_checklists` → ⛔ **NÃO MIGRA**
- Tabela legado pequena, sem equivalente no novo. Confirmar com produto se feature foi descartada.

### 6. Salas

#### `tipo_sala_atendimento` → `tipo_sala_atendimento` ✅
- Catálogo system-wide. Já seedado no novo (19 linhas). **Não migra** dados legado — usar seed do novo.

#### `sala_atendimento` → `sala_atendimento` ✅
- **Volume:** ~150.
- Direct copy de `estabelecimento_id, unidade_id, tipo_sala_atendimento_id (mapping), nome, ativo`.

### 7. Estoque/Inventário

#### `estoque_produto` → `itens_inventario` 🟡
- **Volume:** ~3.000.
- Rename + transformação de modelagem. Legado tem múltiplas tabelas de catálogo (`estoque_categoria, estoque_fabricante, estoque_fornecedor, estoque_tipo_produto`). Novo as colapsa em colunas planas em `itens_inventario` (`categoria text, fabricante text, fornecedor text`) ou as descarta — confirmar com produto.
- **Lotes:** `estoque_lote` legado → no novo, lotes ficam em coluna jsonb dentro de `itens_inventario` ou tabela separada (verificar schema atual: na lista, não vejo `lotes` — provavelmente colapsado). **Pendente confirmação.**

#### `movimento_estoque` → `movimentacoes_estoque` ✅
- **Volume:** ~10.000.
- Rename + direct copy. Inclui `valor_venda` ([20260210120000_add_valor_venda_movimento_estoque.sql](../ReferenciaLegado/Imedto/supabase/migrations/20260210120000_add_valor_venda_movimento_estoque.sql)) e `numero_serie`.

#### `estoque_categoria, estoque_fabricante, estoque_fornecedor, estoque_tipo_produto, estoque_lote` → ⛔
- Catálogos descartados — campos colapsados em `itens_inventario`. Confirmar com produto antes de descartar lotes.

### 8. Orçamentos

#### `orcamentos` → `orcamentos` ✅
- **Volume:** ~3.000.
- Direct copy de `estabelecimento_id, paciente_id, profissional_id, evento_de_agendamento_id, status, valor_total, observacoes, dados_pagamento`.

#### `orcamento_cirurgias` → `orcamento_cirurgias` ✅ / `orcamento_cirurgia` → ⛔
- **Atenção:** legado tem **DUAS tabelas**: `orcamento_cirurgia` (catálogo de cirurgias do estabelecimento) e `orcamento_cirurgias` (cirurgias do orçamento específico).
- `orcamento_cirurgias` (do orçamento) → migra direto.
- `orcamento_cirurgia` (catálogo) → **discutir com produto**: se novo não tem catálogo equivalente (não vejo na lista de tabelas), **descartar** mas avisar; usuários terão que recadastrar templates.

#### `orcamento_internacao, orcamento_anestesia` → idem (✅)
- Direct copy. Estes existem no destino.

#### `orcamento_implante` → `orcamento_implantes` ✅
- Pluralização. Direct copy.

#### `orcamento_equipe_especializada, orcamento_profissionais, orcamento_valor_profissional` → `orcamento_equipe` 🟡
- **Merge de 3 tabelas legado em 1 nova** `orcamento_equipe` ([list_tables](../supabase/migrations/)). Lógica: cada linha legado vira uma linha em `orcamento_equipe` com colunas indicando tipo (`tipo_membro: 'profissional' | 'especializada'`). Wave 2 detalhará o JOIN.

#### `orcamento_extras, orcamento_produtos, orcamento_cirurgia_produto, orcamento_configuracao_estabelecimento, orcamento_configuracao_pagamento` → ⛔
- `orcamento_extras` — feature descartada (confirmado no enunciado).
- `orcamento_produtos` / `orcamento_cirurgia_produto` — relacionamento M:N orçamento↔produto. Se o novo só tem `orcamento_implantes` para itens físicos, esses **descartam**; senão precisam virar linhas em `itens_orcamento`.
- `orcamento_configuracao_*` — configurações por estabelecimento. **Pendente decisão de produto.**

#### `orcamento_formas_pagamento` → `orcamento_formas_pagamento` ✅
- Direct copy.

### 9. Receitas

#### `receitas` → `receitas` 🟡
- **Volume:** ~6.000 (50 × 100/mês × 12).
- Direct copy de `estabelecimento_id, paciente_id, prontuario_id, profissional_id, evento_de_agendamento_id, status, version_of_id (mapping), version_number, observacoes, finalized_at, canceled_at`.
- **Tipo + Notificação:** legado tem `tipo TEXT ('SIMPLES'|'CONTROLADA')` + `tipo_notificacao TEXT ('A'|'B'|'C'|'ESPECIAL')`. Novo tem **um único enum `Tipo` com 4 valores** consolidando os 8 estados possíveis (ver [Doc 04 item 4.12](04_FASE_4_PRONTUARIO_AVANCADO.md) — regras ANVISA). Mapeamento:

| `tipo` legado | `tipo_notificacao` legado | `Tipo` novo |
|---|---|---|
| `SIMPLES` | (null) | `Comum` |
| `CONTROLADA` | `A` | `NotificacaoA` (amarela — entorpecentes) |
| `CONTROLADA` | `B` | `NotificacaoB` (azul — psicotrópicos) |
| `CONTROLADA` | `C` | `NotificacaoC` (branca — antimicrobianos) |
| `CONTROLADA` | `ESPECIAL` | `Especial` (talidomida/retinóides) |

- **Revisão obrigatória produto:** se houver `(SIMPLES, ESPECIAL)` no legado (combinação inválida), tratar como erro e **bloquear ETL** até revisão manual.

#### `receita_itens` → `receita_itens` ✅
- Direct copy.

#### `receitas_configuracao_estabelecimento` → `receitas_configuracao_estabelecimento` ✅
- Direct copy (feature flag por estabelecimento).

#### `medicamentos_favoritos` → `medicamentos_favoritos` ✅
- Direct copy.

### 10. Financeiro

#### `financeiro_categoria` → `categorias_financeiras` 🟡
- Rename. Direct copy de campos.

#### `financeiro_forma_pagamento` → `formas_pagamento` 🟡
- Rename. Direct copy.

#### `financeiro_transacao` → `lancamentos` 🟡
- **Volume:** ~15.000.
- Rename + direct copy de `estabelecimento_id, valor, data_competencia, data_caixa, descricao, categoria_id (mapping), forma_pagamento_id (mapping), tipo (entrada/saida), origem`.

### 11. Automação

#### `automation_rules, automation_events, automation_audit_logs` → `automation_rules, automation_events, configuracoes_automacao` 🟡
- Rename de `automation_audit_logs` → tabela específica de audit; `configuracoes_automacao` é nova consolidada por estabelecimento.
- **Volume baixo** (~50 regras totais). Migração straightforward.

### 12. IA / Rate-limit / Notificações / Audit

#### `ai_audit_logs, ai_rate_limits, establishment_ai_settings` → idem ✅
- Direct copy. Tabelas com mesmo nome no novo.

#### `notifications` → `notificacoes` 🟡
- Rename. Direct copy. **Filtrar:** apenas notificações dos últimos 90 dias (volume + relevância).

#### `lgpd_access_log` → `prontuario_acesso_log` ⛔
- Forward-only (ver acima). Logs legado ficam no projeto antigo.

### 13. Subscription

#### `assinaturas` → `assinaturas` ✅ / `planos` → `planos` ✅
- Catálogo `planos` já seedado no novo — **não migra dados legado**, apenas re-link `assinaturas.plano_id` via mapping `etl_planos_legado_novo` (correspondência por nome do plano).
- `assinaturas` migra direct copy (com mapping de `plano_id`).

### 14. Catálogos / Seeds (não migram do legado — usar seed do novo)

| Tabela novo | Origem |
|---|---|
| `profissoes` | seed [20260429100002_seed_profissoes_especialidades.sql](../supabase/migrations/20260429100002_seed_profissoes_especialidades.sql) |
| `especialidades` | mesmo seed |
| `tipo_sala_atendimento` | seed [20251120133000_seed_tipo_sala_atendimento.sql](../ReferenciaLegado/Imedto/supabase/migrations/20251120133000_seed_tipo_sala_atendimento.sql) (replicado) |
| `regioes_anatomicas_catalogo` | seedado (144 linhas) |
| `catalogo_procedimentos` | seedado (83 linhas) |
| `planos` | seed catalogo de produto |

---

## Tabelas a descartar (não migram)

| Tabela legado | Justificativa |
|---|---|
| `appointment_checklists` | Feature não existe no novo. |
| `paciente_estabelecimento` | Já dropada no próprio legado. |
| `imedto_admin_audit_log`, `imedto_admins`, `imedto_config` | Painel admin interno — recriado de zero no novo. |
| `lgpd_access_log` | Audit forward-only (logs antigos ficam no projeto legado). |
| `orcamento_extras` | Feature descartada (confirmado). |
| `estoque_categoria, estoque_fabricante, estoque_fornecedor, estoque_tipo_produto, estoque_lote` | Catálogos colapsados em colunas de `itens_inventario`. |
| `usuario_estabelecimento_ativo` | Estado de sessão — irrelevante para migração de dados. |

---

## Tabelas-padrão do novo (mantêm — não vêm do legado)

`profissoes`, `especialidades`, `tipo_sala_atendimento`, `regioes_anatomicas_catalogo`, `catalogo_procedimentos`, `planos`, `idempotency_keys`, `audit_delete_attempts`, `procedimentos_cirurgicos` (este último é estrutura nova — não tinha tabela formal no legado, eram seções jsonb).

---

## Ordem de execução do ETL

> Seguir estritamente — quebra de FK aborta tudo.

1. **Catálogos do novo** (já seedados, idempotente): `profissoes, especialidades, tipo_sala_atendimento, regioes_anatomicas_catalogo, catalogo_procedimentos, planos`.
2. **`auth.users`** (recriado via reenvio de convites — NÃO via SQL).
3. **`usuarios`** (espelho).
4. **`estabelecimentos`**.
5. **`unidades_estabelecimento`**.
6. **`sala_atendimento`**.
7. **`profissionais`** (depende de `usuarios`).
8. **`modelo_permissao_estabelecimento`** (transformação crítica — revisão humana antes).
9. **`vinculo_profissional_estabelecimento`** (depende de profissional + modelo).
10. **`solicitacoes_vinculo`** (apenas pendentes).
11. **`pacientes`**.
12. **`modelo_de_prontuario`** + **`prontuario_variaveis_pool`**.
13. **`prontuarios`**.
14. **`prontuario_evolucoes`** (com triggers de imutabilidade desabilitadas).
15. **`exame_fisico`**.
16. **`agendamentos`** (com constraint de overlap desabilitada → reabilitar e validar).
17. **`itens_inventario`** + **`movimentacoes_estoque`**.
18. **`receitas`** + **`receita_itens`** + **`receitas_configuracao_estabelecimento`** + **`medicamentos_favoritos`**.
19. **`orcamentos`** + filhos (`orcamento_cirurgias, orcamento_internacao, orcamento_anestesia, orcamento_implantes, orcamento_equipe, orcamento_formas_pagamento, itens_orcamento`).
20. **Financeiro** (`categorias_financeiras → formas_pagamento → lancamentos`).
21. **Automação** (`automation_rules → automation_events → configuracoes_automacao`).
22. **IA** (`ai_audit_logs, ai_rate_limits, establishment_ai_settings`).
23. **`notificacoes`** (últimos 90 dias).
24. **`assinaturas`**.
25. **Reabilitar triggers e constraints diferidas**, rodar `ANALYZE`.
26. **Storage** (cópia de buckets `imedto-anexos`, `imedto-fotos`).

---

## Riscos identificados

1. **Modelo de permissões (`modelo_permissao_estabelecimento`)** — split de 16 chaves para `permissoes` + `permissoes_extras` é a transformação mais arriscada. Revisão produto obrigatória antes do cutover; teste com 5 estabelecimentos reais antes de generalizar.
2. **Senhas não migram** — exige campanha de comunicação aos usuários **antes** do cutover, com janela de pré-reset. Sem isso, abertura do novo Supabase = todo mundo bloqueado.
3. **Receitas com combinações inválidas** (`SIMPLES`+`tipo_notificacao` preenchido) — bloquear ETL até revisão manual.
4. **Constraint anti-overlap** em `agendamentos` — pode rejeitar histórico legítimo do legado. Política: desabilitar, importar, validar conflitos em relatório, deixar histórico mas bloquear novos overlaps.
5. **Anexos em Storage** — cópia de bucket pode falhar parcialmente; metadata em `prontuario_anexos` precisa ser populada **após** verificação de cópia bem-sucedida (sob pena de orphan rows).
6. **Volume de `prontuario_evolucoes`** (~30k linhas) com `conteudo jsonb` rico — testar performance do INSERT em lote (target: <30 min).
7. **`exame_fisico_regioes`** — confusão de nomenclatura entre catálogo (legado) e achados (novo). Risco de migrar dado errado para tabela errada.
8. **Catálogos de orçamento** (`orcamento_cirurgia`, configurações) — se descartados, usuários perdem templates. Precisa decisão produto.

---

## Tabelas com dúvida — confirmar com produto

- **`appointment_checklists`** — feature realmente removida ou esqueceram de portar?
- **`estoque_lote`** — colapsa em jsonb ou cria tabela `lotes` no novo?
- **`orcamento_cirurgia` (catálogo)** — descartar e perder templates dos usuários?
- **`orcamento_configuracao_estabelecimento` / `orcamento_configuracao_pagamento`** — configurações de fluxo de orçamento. Migrar para onde?
- **`orcamento_produtos` / `orcamento_cirurgia_produto`** — relação M:N produto-orçamento; novo só tem `itens_orcamento` genérico?
- **Personalizações de `exame_fisico_regioes` por estabelecimento** — migrar para `regioes_anatomicas_catalogo` com `estabelecimento_id IS NOT NULL`?
- **Logs LGPD legado** — onde guardar pelo prazo legal (5 anos LGPD)? Snapshot do projeto legado é suficiente ou precisa export?
- **Notificações** — janela de 90 dias está correta ou produto quer mais?

---

**Próximos passos (Wave 2):**

1. Produto valida mapeamentos pendentes acima.
2. Engenharia escreve SQL `_etl_<tabela>.sql` para cada item da ordem de execução.
3. Dry-run em DB intermediário com snapshot de produção.
4. Diff de contagem por tabela legado vs novo (gating: <0,1% perda em entidades não-derivadas).
5. Rehearsal completo + cutover.
