# Admin Global Wave 4 — Catálogos Live-Link (Modelos, Variáveis, Regiões Anatômicas)

**ID**: 2026-05-30_004
**Data**: 2026-05-30
**Autor**: imedto-business-analyst
**Status**: Aprovado pelo usuário em 2026-05-30 ("ok" sobre recomendações A em Q1, Q2, Q3+Q3.1, Q4+Q4.1)
**Estimativa de esforço**: G (3 frentes, drops de tabela com inspeção prévia em prod, reescrita de 4 telas admin, atualização de docs)
**Áreas regressivas tocadas**: módulo admin global, prontuário (listagens de modelos no tenant), pool de variáveis (dropdowns clínicos), exame físico (árvore de regiões anatômicas)

**Referências (imutáveis)**:
- [`2026-05-30_001_admin-global-mvp.md`](2026-05-30_001_admin-global-mvp.md) — MVP do módulo admin (estabelecimentos, planos, audit, BFF admin).
- [`2026-05-30_002_admin-global-wave2.md`](2026-05-30_002_admin-global-wave2.md) — Wave 2 (modelos/variáveis/regiões globais via tabelas paralelas + import) — **esta Wave 4 reverte a abordagem de tabelas paralelas**.
- [`2026-05-30_003_admin-global-wave3-redesign.md`](2026-05-30_003_admin-global-wave3-redesign.md) — Wave 3 (redesign visual do módulo admin).

**Agentes downstream**: `imedto-database` → `imedto-developer` → `imedto-qa`.

---

## 1. Contexto e motivação

### Feedback literal do usuário

> "ok" — sobre o conjunto de recomendações A em Q1, Q2, Q3 + Q3.1, Q4 + Q4.1. O usuário pede modelo single-source live-link (admin edita → tenants veem na hora) para modelos de prontuário, variáveis pool e regiões anatômicas.

### Descoberta crítica do BA

A Wave 2 produziu 3 tabelas paralelas (`imedto_modelo_prontuario_global`, `imedto_variavel_pool_global`, `imedto_regiao_anatomica_global`) + handlers `Importar*DoGlobal*` + endpoints `*/importar-do-global/{id}` + views `*GlobaisListView/FormView` partindo da premissa de que "global" e "tenant" eram catálogos distintos que se comunicavam por cópia.

Mas o sistema **já tem** suporte nativo a live-link via:

- `modelo_de_prontuario.eh_padrao_sistema BOOLEAN` (default false) + `estabelecimento_id NULL` quando padrão-sistema.
- `prontuario_variaveis_pool.eh_padrao_sistema BOOLEAN` (default false) + `estabelecimento_id NULL` quando padrão-sistema.
- Queries do tenant já usam `WHERE (eh_padrao_sistema=true OR estabelecimento_id=@X)`.

E `regioes_anatomicas_catalogo` (144 registros, hierárquico com `codigo/pai_codigo/nivel/vista/template_texto/svg_coords/ordem/lateralidade/ativo`, criada em 2026-05-26) **já é global por construção** — consumida direto pelo exame físico de qualquer tenant.

Conclusão: Wave 2 foi desvio arquitetural. Live-link nativo resolve tudo de forma mais simples, sem duplicação de schema, sem fluxo de importação, sem drift entre cópia tenant e original global.

---

## 2. Objetivo

Eliminar o código órfão da Wave 2 (tabelas paralelas, handlers de import, endpoints de import, domain types globais paralelos) e reaproveitar o live-link nativo do sistema — admin cria/edita catálogos padrão-sistema (modelos de prontuário, variáveis pool, regiões anatômicas) e a mudança reflete em todos os tenants no próximo refresh, sem fluxo de importação e sem cópia.

---

## 3. Escopo IN

### Frente 1 — Modelos de prontuário + variáveis pool live-link

- Admin CRUD direto em `modelo_de_prontuario` com `EhPadraoSistema=true` + `EstabelecimentoId=NULL`.
- Admin CRUD direto em `prontuario_variaveis_pool` com `EhPadraoSistema=true` + `EstabelecimentoId=NULL`, filtrável/agrupável pelas 8 categorias do enum `TipoVariavelPool`.
- Views `ModelosGlobaisListView/FormView` e `VariaveisGlobaisListView/FormView` **REAPROVEITADAS** (mesmas rotas, mesmos componentes, mesmo visual da Wave 3), apontando para os novos endpoints admin.
- Drop de tabelas paralelas, handlers de import, endpoints de import (ver Frente 3).

### Frente 2 — Regiões anatômicas hierárquicas live-link

- Admin CRUD direto em `regioes_anatomicas_catalogo` (já é global por construção).
- Tela `RegioesGlobaisListView/FormView` **REESCRITA** como tree view (vista anterior/posterior → nivel 1 → 2 → 3 expansível) — diferente das Wave 2/3 originais.
- MVP CRUD: criar/editar/excluir (folha) /inativar região + criar subgrupos.
- Campos editáveis: `codigo`, `nome`, `pai_codigo`, `nivel`, `vista`, `template_texto`, `ordem` (input numérico), `lateralidade`, `ativo`.
- Drop `imedto_regiao_anatomica_global` (ver Frente 3).

### Frente 3 — Cleanup arquitetural

- Migration drop idempotente para 3 tabelas paralelas + checagem prévia de FK.
- Remoção de Domain types `ImedtoModeloProntuarioGlobal`, `ImedtoVariavelPoolGlobal`, `ImedtoRegiaoAnatomicaGlobal` + EF Configurations + QueryRepositories.
- Remoção de handlers `ImportarModeloDoGlobalCommandHandler`, `ImportarVariavelDoGlobalCommandHandler` (regiões já era global, não tinha import).
- Remoção de endpoints `modelos/importar-do-global/{id}` e `pool/importar-do-global/{id}`.
- Remoção da aba/tela "Templates do sistema importáveis" no frontend tenant (`ModelosProntuarioView.vue` + `ListasVariaveisTab.vue` — confirmar nomes na implementação).
- Atualização de `Docs/ARQUITETURA.md` §"Catálogos Globais" e nota em `Docs/DESIGN.md` (componente `RegiaoTreeView`).

---

## 4. Escopo OUT

- Drag-and-drop para reordenar regiões na árvore (ordem editável via input numérico; D&D fica em backlog).
- Editor visual de `svg_coords` (campo permanece persistente, mas sem UI de edição visual nesta Wave).
- Criar/excluir categoria de variável pool dinamicamente (enum `TipoVariavelPool` congelado; nova categoria = migration + deploy = backlog).
- Impersonate admin para testar live-link como tenant.
- MFA TOTP.
- RBAC admin granular (continua role `admin_global` única).
- Billing real (Stripe, faturamento).
- Logs centralizados (S3 + Athena/OpenSearch).
- Migração de dados de cópias existentes em tenants (cópias permanecem como tenant-owned, sem alteração — ver §8 verificação prévia).

---

## 5. Decisões de produto cravadas

| ID | Decisão | Justificativa |
|---|---|---|
| **D1** (Q1=A) | Admin CRUD direto em `modelo_de_prontuario` com `EhPadraoSistema=true` | Reaproveita live-link nativo; zero duplicação de schema; tenant vê mudança imediatamente. |
| **D2** (Q2=A) | DB agent inspeciona prod antes do drop (count + cópias criadas perto de 2026-05-30) | Visibilidade do impacto real da Wave 2 em produção; cópias permanecem como tenant-owned (não migra). |
| **D3** (Q3=A) | CRUD admin direto em `regioes_anatomicas_catalogo` com tree view (vista → nivel) | Tabela já é global por construção; tree view reflete a hierarquia natural (vista anterior/posterior → grupos → subgrupos). |
| **D4** (Q3.1=A) | MVP tree view sem D&D e sem editor visual de svg_coords | Velocidade de entrega; volume baixo (144 registros) não exige D&D; editor SVG é discovery próprio. |
| **D5** (Q4=A) | UMA tela admin "Variáveis padrão-sistema" com filtro/agrupamento por categoria (8 do enum) | Operação simples para admin; alinhada com modelo single-source. |
| **D6** (Q4.1=A) | Enum `TipoVariavelPool` congelado (8 categorias) | Categorias clínicas estáveis; adicionar nova vira backlog com migration explícita. |

---

## 6. Arquitetura proposta (alto nível)

### Frente 1 — Modelos prontuário + variáveis pool live-link

**Backend**:
- Novos handlers em `Application/Admin/Catalogos/Modelos/`:
  - `CriarModeloPadraoSistemaCommandHandler.cs`
  - `AtualizarModeloPadraoSistemaCommandHandler.cs`
  - `InativarModeloPadraoSistemaCommandHandler.cs`
  - `ListarModelosPadraoSistemaQueryHandler.cs`
- Equivalentes em `Application/Admin/Catalogos/VariaveisPool/`:
  - `CriarVariavelPadraoSistemaCommandHandler.cs`
  - `AtualizarVariavelPadraoSistemaCommandHandler.cs`
  - `InativarVariavelPadraoSistemaCommandHandler.cs`
  - `ListarVariaveisPadraoSistemaQueryHandler.cs` (com filtro por `TipoVariavelPool`)
- Controllers admin (`Api/Admin/Catalogos/ModelosController.cs`, `VariaveisPoolController.cs`) apontam para os novos handlers; rotas mantidas (`/api/admin/catalogos/modelos`, `/api/admin/catalogos/variaveis-pool`).
- Query repositories admin filtram `WHERE eh_padrao_sistema=true AND deletado_em IS NULL`.
- Comandos de criação/atualização gravam `EhPadraoSistema=true` + `EstabelecimentoId=NULL`.

**Frontend**:
- `modules/admin/views/ModelosGlobaisListView.vue` e `ModelosGlobaisFormView.vue` — REAPROVEITADAS (rotas e visual da Wave 3 mantidos), apontando para os novos endpoints.
- `modules/admin/views/VariaveisGlobaisListView.vue` e `VariaveisGlobaisFormView.vue` — REAPROVEITADAS, adicionando filtro/agrupamento por categoria (dropdown com 8 valores do enum).

### Frente 2 — Regiões anatômicas hierárquicas

**Backend**:
- Novos handlers em `Application/Admin/Catalogos/RegioesAnatomicas/`:
  - `CriarRegiaoAnatomicaCommandHandler.cs` (valida `codigo` único, `pai_codigo` existe, `nivel = pai.nivel + 1`, `vista = pai.vista`)
  - `AtualizarRegiaoAnatomicaCommandHandler.cs`
  - `InativarRegiaoAnatomicaCommandHandler.cs`
  - `ExcluirRegiaoAnatomicaCommandHandler.cs` (rejeita se tem filhos, sugere inativar)
  - `RegiaoAnatomicaTreeQueryHandler.cs` (retorna árvore agrupada por vista, montada server-side a partir de single query e nesteada por `pai_codigo`)
- Controller `Api/Admin/Catalogos/RegioesAnatomicasController.cs` em `/api/admin/catalogos/regioes-anatomicas`.

**Frontend**:
- `modules/admin/views/RegioesGlobaisListView.vue` — REESCRITA com tree view (tabs ou colunas vista anterior/posterior → nodes expansíveis).
- `modules/admin/views/RegioesGlobaisFormView.vue` — REESCRITA com seletor de pai (autocomplete por código/nome) + campos `codigo`, `nome`, `template_texto`, `ordem` (input number), `lateralidade` (radio), `ativo` (toggle).
- Novo componente `modules/admin/components/regioes/RegiaoTreeView.vue` — render recursivo expand/colapse, sem virtualização (volume baixo).

### Frente 3 — Cleanup

- Migration SQL idempotente em `db/migrations/YYYYMMDDHHMM_drop_wave2_globais.sql`:
  - `DROP TABLE IF EXISTS imedto_modelo_prontuario_global CASCADE;` (somente após DB agent confirmar zero FK pendente em outras tabelas — esperado: nenhuma; CASCADE como defesa).
  - Idem para `imedto_variavel_pool_global` e `imedto_regiao_anatomica_global`.
- Remoção dos arquivos (lista exaustiva — dev confere via grep no momento):
  - `Domain/Admin/ImedtoModeloProntuarioGlobal.cs`, `ImedtoVariavelPoolGlobal.cs`, `ImedtoRegiaoAnatomicaGlobal.cs`.
  - Suas `EntityConfigurations` em `Infrastructure/Persistence/Configurations/Admin/`.
  - QueryRepositories em `Infrastructure/Persistence/QueryRepositories/Admin/`.
  - Handlers `Application/Admin/Catalogos/.../ImportarModeloDoGlobalCommandHandler.cs` e `ImportarVariavelDoGlobalCommandHandler.cs`.
  - Endpoints/actions correspondentes nos controllers.
  - Aba/seção "Templates do sistema importáveis" em `ModelosProntuarioView.vue` e `ListasVariaveisTab.vue` no frontend tenant (confirmar nomes via grep).
- Atualização docs:
  - `Docs/ARQUITETURA.md` §"Catálogos Globais" reescrita: cópia → live-link nativo via `EhPadraoSistema=true`.
  - `Docs/DESIGN.md` nota sobre `RegiaoTreeView.vue`.

---

## 7. Pontos de extensão futura

- Drag-and-drop para reordenar regiões na árvore.
- Editor visual de `svg_coords` (selecionar polígono sobre SVG do corpo humano).
- Criar/excluir categoria de variável pool dinamicamente (tabela `tipo_variavel_pool_catalogo` + migration do enum).
- Impersonate admin → tenant para validar live-link como recepcionista/profissional.
- MFA TOTP no login admin.
- RBAC admin granular (admin de catálogo ≠ admin de planos ≠ admin de auditoria).
- Logs centralizados (S3 + Athena/OpenSearch) para mutações admin.
- Métricas globais (quantos tenants usam cada modelo padrão-sistema, top variáveis usadas).

---

## 8. Modelo de dados (DB agent detalha)

### Tabelas que SAEM (drop idempotente)

| Tabela | Origem | Ação |
|---|---|---|
| `imedto_modelo_prontuario_global` | Wave 2 (2026-05-30_002) | DROP IF EXISTS CASCADE |
| `imedto_variavel_pool_global` | Wave 2 (2026-05-30_002) | DROP IF EXISTS CASCADE |
| `imedto_regiao_anatomica_global` | Wave 2 (2026-05-30_002) | DROP IF EXISTS CASCADE |

Seeds da Wave 2 (1 modelo + 3 variáveis + 15 regiões) saem junto com as tabelas.

### Tabelas que NÃO MUDAM

| Tabela | Por que comporta uso admin |
|---|---|
| `modelo_de_prontuario` | Já tem `eh_padrao_sistema BOOLEAN` + `estabelecimento_id NULL` (live-link nativo). |
| `prontuario_variaveis_pool` | Já tem `eh_padrao_sistema BOOLEAN` + `estabelecimento_id NULL` + `tipo TipoVariavelPool`. |
| `regioes_anatomicas_catalogo` | Já é global por construção (144 registros, hierárquico, criada em 2026-05-26). |

### Verificação prévia obrigatória pelo DB agent (antes da migration)

```sql
-- Quantos registros nas tabelas Wave 2 (esperado: poucos/zero seeds)
SELECT 'imedto_modelo_prontuario_global' AS tbl, COUNT(*) FROM imedto_modelo_prontuario_global
UNION ALL SELECT 'imedto_variavel_pool_global', COUNT(*) FROM imedto_variavel_pool_global
UNION ALL SELECT 'imedto_regiao_anatomica_global', COUNT(*) FROM imedto_regiao_anatomica_global;

-- Algum tenant importou via Wave 2? (cópias com data próxima a 2026-05-30)
SELECT COUNT(*) AS modelos_copiados FROM modelo_de_prontuario
 WHERE eh_padrao_sistema=false AND criado_em >= '2026-05-30';
SELECT COUNT(*) AS variaveis_copiadas FROM prontuario_variaveis_pool
 WHERE eh_padrao_sistema=false AND criado_em >= '2026-05-30';

-- FKs apontando para as tabelas Wave 2 (esperado: nenhuma)
SELECT conname, conrelid::regclass AS tabela_filha, confrelid::regclass AS tabela_pai
  FROM pg_constraint
 WHERE confrelid::regclass::text IN (
   'imedto_modelo_prontuario_global','imedto_variavel_pool_global','imedto_regiao_anatomica_global');
```

**Política**:
- Se contagem nas tabelas Wave 2 = 0 → drop puro.
- Se contagem > 0 (seeds que ninguém importou) → drop puro (são seeds globais, não tenant-owned).
- Se houver cópias em tenants (`eh_padrao_sistema=false` criadas perto de 2026-05-30) → **permanecem como tenant-owned** (mantém `EhPadraoSistema=false`). Sem migração, sem deduplicação automática. Decisão do tenant manter ou apagar.
- Se houver FK órfã → DB agent registra no relatório e migration falha (erro idempotente: dev investiga).

---

## 9. Critérios de Aceite (Dado / Quando / Então)

### Frente 1 — Modelos de prontuário live-link

**W4-CA1** (criar): Dado um admin autenticado, Quando POST `/api/admin/catalogos/modelos` com `nome`, `conteudo`, `motivo (≥10 chars)`, Então registro é persistido em `modelo_de_prontuario` com `EhPadraoSistema=true`, `EstabelecimentoId=NULL`, e linha de audit `CriarModeloPadraoSistema` é gravada em `imedto_admin_audit_log` com `{usuario_admin_id, modelo_id, nome, motivo, timestamp}`.

**W4-CA2** (live-link create): Dado um tenant qualquer com sessão ativa, Quando admin acaba de criar modelo padrão-sistema (W4-CA1) e tenant abre a tela de prontuário/listagem de modelos, Então o modelo recém-criado aparece imediatamente na listagem (sem necessidade de importação).

**W4-CA3** (editar): Dado modelo padrão-sistema existente, Quando admin faz PUT `/api/admin/catalogos/modelos/{id}` com novos campos + `motivo`, Então registro é atualizado em `modelo_de_prontuario`, mudança reflete em todos os tenants no próximo refresh, e audit `AtualizarModeloPadraoSistema` é gravada.

**W4-CA4** (inativar): Dado modelo padrão-sistema ativo, Quando admin faz POST `/api/admin/catalogos/modelos/{id}/inativar` com `motivo`, Então `ativo=false` (ou equivalente flag de inativação), o modelo some das listagens do tenant (filtro `WHERE ativo=true`), e audit `InativarModeloPadraoSistema` é gravada.

**W4-CA5** (listar admin): Dado admin abre `/admin/modelos-globais`, Quando a tela carrega, Então lista apenas modelos com `EhPadraoSistema=true`, paginada, com busca por nome (debounce 300ms).

**W4-CA6** (nome único): Dado admin tenta criar modelo padrão-sistema com nome já usado por outro padrão-sistema, Quando submete, Então recebe 422 com mensagem "Já existe modelo padrão do sistema com esse nome". Nome NÃO conflita com nomes idênticos em modelos de estabelecimentos (escopos separados).

### Frente 1 — Variáveis pool live-link

**W4-CA7** (listar admin com categoria): Dado admin abre `/admin/variaveis-globais`, Quando a tela carrega, Então lista variáveis com `EhPadraoSistema=true` agrupadas/filtráveis pelas 8 categorias do enum `TipoVariavelPool` (Alergia, Medicamento, Doenca, Cirurgia, Droga, RelacaoFamiliar, Expectativa, AtividadeFisica).

**W4-CA8** (criar): Dado admin autenticado, Quando POST `/api/admin/catalogos/variaveis-pool` com `nome`, `tipo` (enum), `motivo`, Então persistida com `EhPadraoSistema=true`, `EstabelecimentoId=NULL`, e audit `CriarVariavelPadraoSistema` é gravada.

**W4-CA9** (live-link create): Dado tenant qualquer, Quando admin criou variável padrão-sistema na categoria `Alergia` (W4-CA8) e tenant abre dropdown de Alergias no prontuário, Então variável aparece imediatamente.

**W4-CA10** (editar): Dado variável padrão-sistema existente, Quando admin edita nome/categoria + `motivo`, Então mudança reflete em todos os tenants e audit `AtualizarVariavelPadraoSistema` é gravada.

**W4-CA11** (inativar): Dado variável padrão-sistema ativa, Quando admin inativa com `motivo`, Então some das listagens do tenant e audit `InativarVariavelPadraoSistema` é gravada.

**W4-CA12** (nome único por categoria): Dado admin tenta criar variável com `(nome, tipo, eh_padrao_sistema=true)` que já existe, Quando submete, Então recebe 422 com mensagem "Já existe variável padrão do sistema com esse nome nesta categoria".

**W4-CA13** (enum fixo): Dado admin abre form de criar variável, Quando seleciona categoria, Então dropdown mostra exatamente as 8 categorias do enum `TipoVariavelPool` (sem opção de criar categoria nova).

### Frente 2 — Regiões anatômicas hierárquicas

**W4-CA14** (tree view): Dado admin abre `/admin/regioes-globais`, Quando a tela carrega, Então mostra tree view agrupada por `vista` (anterior/posterior) com `nivel 1 → 2 → 3` expansível/colapsável.

**W4-CA15** (criar nível 1): Dado admin autenticado, Quando POST `/api/admin/catalogos/regioes-anatomicas` com `codigo`, `nome`, `vista`, `template_texto`, `lateralidade`, `nivel=1`, `pai_codigo=null`, `motivo`, Então persistido em `regioes_anatomicas_catalogo` e audit `CriarRegiaoAnatomica` é gravada.

**W4-CA16** (criar subgrupo com validação hierárquica): Dado admin cria região com `pai_codigo=X`, Quando submete, Então backend valida: (a) região pai com codigo X existe, (b) `nivel = pai.nivel + 1`, (c) `vista = pai.vista`. Falha em qualquer uma → 422 com mensagem específica ("Pai não encontrado", "Nível inconsistente com pai", "Vista deve ser igual à do pai").

**W4-CA17** (editar): Dado região existente, Quando admin edita `nome`, `template_texto`, `ordem`, `lateralidade` ou `ativo` + `motivo`, Então persistido e audit `AtualizarRegiaoAnatomica` é gravada. `codigo`, `pai_codigo`, `nivel`, `vista` NÃO são editáveis (mover região na árvore vira backlog).

**W4-CA18** (inativar): Dado região ativa, Quando admin inativa (`ativo=false`) + `motivo`, Então some do exame físico do tenant (filtro `WHERE ativo=true`) e audit `InativarRegiaoAnatomica` é gravada.

**W4-CA19** (excluir folha vs com filhos): Dado região FOLHA (sem filhos em `pai_codigo`), Quando admin DELETE com `motivo`, Então remove fisicamente e audit `ExcluirRegiaoAnatomica` é gravada. Dado região COM filhos, Quando admin tenta excluir, Então recebe 422 "Esta região tem subgrupos. Inative em vez de excluir, ou remova os subgrupos primeiro."

**W4-CA20** (codigo único): Dado admin tenta criar região com `codigo` que já existe, Quando submete, Então recebe 422 "Código já em uso".

**W4-CA21** (live-link regiões): Dado tenant qualquer com exame físico aberto, Quando admin cria/edita/inativa região (W4-CA15/17/18), Então mudança reflete no próximo refresh do tenant (catálogo já é global por construção; nada a sincronizar).

### Frente 3 — Cleanup

**W4-CA22** (drop tabelas): Dado migration aplicada em prod, Quando inspeção pós-deploy, Então tabelas `imedto_modelo_prontuario_global`, `imedto_variavel_pool_global`, `imedto_regiao_anatomica_global` não existem. Migration é idempotente (rodar 2x não falha).

**W4-CA23** (drop handlers e endpoints de import): Dado deploy concluído, Quando grep no código, Então não existem mais `ImportarModeloDoGlobalCommandHandler`, `ImportarVariavelDoGlobalCommandHandler`, e endpoints `modelos/importar-do-global/{id}` e `pool/importar-do-global/{id}` retornam 404.

**W4-CA24** (drop domain types globais): Dado deploy concluído, Quando grep no código, Então não existem mais classes `ImedtoModeloProntuarioGlobal`, `ImedtoVariavelPoolGlobal`, `ImedtoRegiaoAnatomicaGlobal` nem suas EF Configurations nem seus QueryRepositories.

**W4-CA25** (remover aba "Templates do sistema importáveis" no tenant): Dado tenant abre tela de Modelos de Prontuário ou Listas de Variáveis no frontend, Quando renderiza, Então NÃO existe mais aba/seção "Templates do sistema importáveis" (ou nome equivalente — dev confirma via grep). Modelos e variáveis padrão-sistema aparecem direto na listagem principal via filtro `WHERE eh_padrao_sistema=true OR estabelecimento_id=@X`.

**W4-CA26** (cópias preservadas): Dado DB agent identificou cópias em tenants (`eh_padrao_sistema=false` criadas perto de 2026-05-30 via Wave 2), Quando deploy concluído, Então essas cópias permanecem como tenant-owned, editáveis pelo tenant normalmente. Sem migração de dados, sem deduplicação.

**W4-CA27** (build verde): Dado código pós-cleanup, Quando `dotnet build` + `pnpm build` + `vitest run` + `dotnet test`, Então passa sem warning ou erro sobre referências a símbolos removidos.

**W4-CA28** (rotas admin): Dado dev atualizou o router admin (`modules/admin/router.ts` ou similar), Quando admin navega para `/admin/modelos-globais`, `/admin/variaveis-globais`, `/admin/regioes-globais`, Então cada rota carrega a view correspondente (reaproveitada nas frentes 1 e 2 ou reescrita na frente 2 para regiões).

### Cross-cutting

**W4-CA29** (motivo obrigatório): Dado qualquer mutação admin (criar/editar/excluir/inativar nas 3 frentes), Quando `motivo` ausente ou com menos de 10 chars, Então recebe 422 com mensagem "Informe o motivo da alteração (mínimo 10 caracteres)".

**W4-CA30** (audit em toda mutação): Dado qualquer mutação admin executada com sucesso, Quando consulto `imedto_admin_audit_log`, Então existe linha com `{usuario_admin_id, action, target_type, target_id, motivo, payload_resumido, timestamp, ip}` correspondente.

**W4-CA31** (RBAC admin): Dado usuário tenant (não-admin) com sessão BFF tenant, Quando chama qualquer endpoint sob `/api/admin/catalogos/*`, Então recebe 403 Forbidden. Cookie admin permanece independente do cookie tenant.

**W4-CA32** (zero regressão tenant): Dado feature deployada, Quando tenant abre listagens de modelos, variáveis e exame físico, Então filtros existentes (`WHERE deletado_em IS NULL`, `WHERE ativo=true`, multi-tenant `(eh_padrao_sistema=true OR estabelecimento_id=@X)`) continuam funcionando idênticos ao pré-Wave 4.

**W4-CA33** (testes verdes): Dado pipeline CI roda Vitest + dotnet test, Quando executa, Então 100% dos testes pré-existentes passam (novos testes desta Wave são aditivos; nenhum teste anterior fica órfão pelos drops).

**W4-CA34** (docs atualizadas): Dado entrega concluída, Quando QA inspeciona `Docs/ARQUITETURA.md`, Então §"Catálogos Globais" foi reescrita: modelo era cópia (Wave 2), agora é live-link nativo via `EhPadraoSistema=true` (com exemplo da query `WHERE eh_padrao_sistema=true OR estabelecimento_id=@X`). `Docs/DESIGN.md` tem nota sobre o componente `RegiaoTreeView.vue` em `modules/admin/components/regioes/`.

---

## 10. Riscos e mitigações

| Risco | Mitigação |
|---|---|
| **R1** — Tenant que importou via Wave 2 espera que a "cópia importada" se atualize sozinha quando admin editar o padrão-sistema. | Comunicação clara no commit/release notes: "Cópias importadas via Wave 2 permanecem como tenant-owned e NÃO refletem mudanças do admin. Tenant pode excluir a cópia para passar a usar o live-link do padrão-sistema." Tenant decide. |
| **R2** — Drop de tabela com FK pendente em produção. | DB agent roda query em `pg_constraint` ANTES do drop. Se FK aparecer, migration falha controladamente e dev investiga (esperado: nenhuma FK, pois tabelas Wave 2 são folhas no grafo). CASCADE no drop como defesa adicional. |
| **R3** — Tree view de regiões com 144 registros lento. | Render expand/colapse server-side-built (1 query → árvore montada no handler), sem virtualização. Volume baixo dispensa otimização inicial. Se crescer >500, considerar virtualização (backlog). |
| **R4** — Enum `TipoVariavelPool` engessado bloqueia categoria nova. | Documentado em pontos de extensão futura. Adicionar categoria nova exige migration + deploy explícitos; admin não cria categorias dinamicamente nesta Wave. |
| **R5** — Dev confunde "reaproveitar view Wave 3" com "criar view nova". | Briefing deixa explícito: ModelosGlobaisListView/FormView e VariaveisGlobaisListView/FormView REAPROVEITADAS (rotas e visual Wave 3 mantidos); RegioesGlobaisListView/FormView REESCRITAS (tree view nova). |
| **R6** — Cópias tenant remanescentes geram confusão visual no front. | Cópias têm `eh_padrao_sistema=false` + `estabelecimento_id` preenchido → aparecem como "modelos do estabelecimento" normalmente. Sem badge/distinção visual nesta Wave (backlog se virar problema). |

---

## 11. Próximos briefings sugeridos (backlog)

- Wave 5 — MFA TOTP no login admin.
- Wave 6 — Impersonate admin → tenant (para validar live-link como recepcionista/profissional sem dados reais alterados).
- Wave 7 — Logs centralizados de mutações admin (S3 + Athena).
- Wave 8 — Editor visual de `svg_coords` para regiões (discovery próprio antes).
- Wave 9 — Categorias de variável pool dinâmicas (tabela `tipo_variavel_pool_catalogo`).
- Wave 10 — Drag-and-drop em árvore de regiões.
- Wave 11 — Métricas globais (quantos tenants usam cada modelo padrão-sistema; ranking).
- Wave 12 — RBAC admin granular (admin de catálogo ≠ admin de planos ≠ admin de auditoria).

---

## 12. Atualizações em `Docs/` exigidas (developer executa na entrega)

### `Docs/ARQUITETURA.md` — reescrever §"Catálogos Globais"

Substituir descrição que assume modelo cópia (Wave 2) por:

- Live-link nativo via `EhPadraoSistema=true` + `EstabelecimentoId=NULL` em `modelo_de_prontuario` e `prontuario_variaveis_pool`.
- Catálogo `regioes_anatomicas_catalogo` é global por construção (sem coluna `estabelecimento_id`).
- Query padrão do tenant: `WHERE (eh_padrao_sistema=true OR estabelecimento_id=@X) AND deletado_em IS NULL AND ativo=true`.
- Admin global mantém CRUD próprio em `/api/admin/catalogos/{modelos|variaveis-pool|regioes-anatomicas}` operando diretamente nas tabelas com filtro `WHERE eh_padrao_sistema=true` (ou direto, no caso de regiões).
- Mudança do admin reflete em qualquer tenant no próximo refresh (sem importação, sem cópia).
- Tabelas paralelas `imedto_*_global` da Wave 2 foram removidas (briefing 2026-05-30_004).

### `Docs/DESIGN.md` — nota sobre `RegiaoTreeView.vue`

Adicionar entrada na seção de componentes do design system / módulo admin:

- `RegiaoTreeView.vue` em `modules/admin/components/regioes/` — render hierárquico expand/colapse para `regioes_anatomicas_catalogo`. Agrupamento por `vista` (anterior/posterior); aninhamento por `pai_codigo` até nível 3. Sem virtualização (volume baixo, ~144 nós). Sem drag-and-drop nesta versão; reordenação via input `ordem`.

---

## 13. Hand-off

**Próximo agente**: `imedto-database`

**O que `imedto-database` deve fazer**:

1. Inspecionar produção via MCP RDS (ou psql via túnel) executando as queries do §8:
   - Contagem em cada uma das 3 tabelas Wave 2.
   - Contagem de cópias em tenants criadas após 2026-05-30 (`eh_padrao_sistema=false` recentes).
   - FKs apontando para as tabelas Wave 2.
2. Registrar relatório curto da inspeção (anexar como `2026-05-30_004_admin-global-wave4-catalogos-livelink-db-inspecao.md` ou comentar inline na entrega ao dev).
3. Criar migration SQL idempotente em `db/migrations/YYYYMMDDHHMM_drop_wave2_globais.sql` com:
   - `DROP TABLE IF EXISTS imedto_modelo_prontuario_global CASCADE;`
   - `DROP TABLE IF EXISTS imedto_variavel_pool_global CASCADE;`
   - `DROP TABLE IF EXISTS imedto_regiao_anatomica_global CASCADE;`
4. Validar que `modelo_de_prontuario`, `prontuario_variaveis_pool`, `regioes_anatomicas_catalogo` permanecem intactas (esquema, dados, índices, RLS-equivalentes via filtro multi-tenant).
5. Confirmar com `imedto-developer` que pode iniciar a implementação.

**Sequência sugerida ao `imedto-developer` (após DB)**:
- **Frente 3 primeiro** (cleanup) — remove código órfão, evita compilar contra symbols que vão sumir.
- **Frente 1 depois** (modelos + variáveis) — reaproveita views Wave 3, baixa complexidade visual.
- **Frente 2 por último** (regiões tree view) — maior complexidade visual, novo componente.

---

## 14. Observações para execução

- Reaproveite componentes do design system (`AppPage`, `AppDataTable`, `AppDrawer`, `AppEmptyState`, `AppPagination`) onde fizer sentido. `RegiaoTreeView` é componente novo justificado pela hierarquia.
- Mensagens 422 em PT-BR, sem PII, claras para o admin.
- Toda chamada admin via cookie BFF admin (já estabelecido no MVP Wave 1).
- Audit `imedto_admin_audit_log` segue padrão definido na Wave 1.
- Não confunda "modelo padrão-sistema" (admin global) com "modelo do estabelecimento" (criado pelo tenant) — separação clara via `EhPadraoSistema` + `EstabelecimentoId NULL/preenchido`.
- Cópias remanescentes Wave 2 em tenants NÃO são tocadas (sem migração de dados); aparecem normalmente como modelos do estabelecimento.
