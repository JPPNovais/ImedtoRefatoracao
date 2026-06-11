# Financeiro F4 — ADDENDUM: vínculo configurável produto↔item de inventário (destrava a baixa automática)

**ID**: 2026-06-10_013-addendum
**Refere-se a**: `2026-06-10_013_financeiro-f4-cobranca-procedimento.md` (briefing original — IMUTÁVEL, não editado)
**Status**: Aprovado (execução autônoma — decisão de produto fechada com o usuário antes do addendum; Opção A)
**Autor**: imedto-business-analyst
**Modo**: B (escalonamento de spec gap Tipo B vindo do dev/QA)
**Estimativa de esforço incremental**: P (1 coluna + 1 FK fraca + 1 campo no DTO/command + 1 select reusando padrão existente)
**Áreas regressivas tocadas**: catálogo de orçamento (aba Produtos do settings), estoque (baixa F4) — multi-tenant

---

## 1. Contexto do gap (por que este addendum existe)

O briefing original assumiu (R5, §5) que o produto do catálogo de orçamento (`orcamento_catalogo_produto` / entidade `CatalogoProduto`) já mapeava para um `ItemInventario`, deixando ao dev "investigar o caminho de mapeamento real" e, se não houvesse, "ignorar o produto" (baixa parcial silenciosa).

**Diagnóstico do dev (confirmado por inspeção de código)**:
- `CatalogoProduto` **não** tem `ItemInventarioId` nem qualquer FK para inventário (`backend/.../Domain/Orcamentos/Catalogos/CatalogoProduto.cs` — sem essa coluna).
- Quem tem o vínculo é o `CatalogoImplante` (`ItemInventarioId long? NULL`, FK `OnDelete(SetNull)`, coluna `item_inventario_id` em `orcamento_catalogo_implante`).
- Não há mapeamento por código/nome confiável entre produto e item de inventário.

**Consequência**: com a R5 original ("produto sem item de inventário → ignora"), **100% dos produtos seriam ignorados** na consolidação da baixa. A baixa automática de estoque — objetivo central declarado da F4 (§1, §3) — **nunca produziria nenhuma `MovimentacaoEstoque`**. A frase da §5 do original ("`orcamento_catalogo_produto` ... já existe e é reusado **sem alteração**") está factualmente superada: o reuso sem alteração não entrega o requisito.

Este addendum fecha a lacuna criando o vínculo que faltava — **exatamente o mecanismo que já existe para implantes**, replicado para produtos. Isso materializa o requisito original do usuário de centralizar custos via a cadeia procedimento↔produto↔estoque.

## 2. Decisão de produto (Opção A — aprovada)

1. **Schema**: adicionar `item_inventario_id bigint NULL` a `orcamento_catalogo_produto` (entidade `CatalogoProduto`), espelhando o `CatalogoImplante`: nullable, **FK fraca com `OnDelete(SetNull)`** (mesmo padrão `fk_catalogo_implante_item_inventario`). Vínculo é **opcional**.
2. **UI**: na aba **Produtos** da configuração de orçamento (`frontend/src/components/orcamento/config/ProdutosTab.vue`, dentro de `OrcamentoSettingsView.vue`), adicionar campo **opcional** "Item de estoque vinculado" — um `select` dos `ItemInventario` **ativos do tenant ativo**. Reusar o padrão **já existente** no config de implantes (`OutrasConfigsTab.vue`): `inventarioService.listarItens({ apenasAtivos: true, tamanho: 500 })`, opções `{ value: String(i.id), label: "{codigo} — {nome}" }`, com opção "Nenhum" (vínculo nulo).
3. **R5 do original permanece em vigor, agora com efeito real**: produto **sem** vínculo (`item_inventario_id IS NULL`) continua **ignorado** na baixa (silencioso, não bloqueia a cobrança — coerente com R6 do original). A diferença é que agora produtos **com** vínculo baixam de fato. Além disso, o **modal de confirmação da F4** (item 3 abaixo / CA94) deve, no preview de "produtos a baixar", **sinalizar discretamente** os produtos do procedimento que estão **sem item de estoque vinculado** — para o usuário perceber a lacuna e ir configurar, em vez de a baixa simplesmente não acontecer sem explicação.
4. A frase "sem alteração" da §5 do briefing original (linha sobre `orcamento_catalogo_produto`) fica **superada por este addendum**. Todo o resto do briefing original (R1–R4, R6–R13, atomicidade, idempotência, CA76–CA89, schema de `cobrancas`) permanece **inalterado e em vigor**.

## 3. Regras de negócio incrementais

- **R14 — Vínculo produto↔item de inventário (novo, substitui a premissa investigativa da R5)**: `CatalogoProduto` ganha `ItemInventarioId long? NULL`. A resolução da baixa (R4 do original) passa a usar **este vínculo** como caminho canônico produto→`ItemInventario`: para cada produto consolidado, se `ItemInventarioId` está setado, baixa do `ItemInventario` correspondente; se `NULL`, **ignora aquele produto** (R5 original preservada, agora bem-definida — não é mais "investigar mapeamento", é "ler a coluna"). Mora em: `CatalogoProduto` (domínio) + handler F4 (consolidação/baixa). Validada em: back + CA.

- **R15 — Vínculo é multi-tenant e só itens do tenant ativo**: o `item_inventario_id` informado na configuração do produto **deve** pertencer ao mesmo `estabelecimento_id` do produto (tenant ativo). O select do front lista apenas `ItemInventario` ativos do tenant ativo (já garantido pelo `inventarioService.listarItens`, que filtra por tenant). O back **valida** que o `ItemInventario` referenciado existe e é do tenant ao criar/atualizar o produto; item de outro tenant → tratado como "não encontrado" genérico (sem revelar existência cross-tenant), e o vínculo é rejeitado/ignorado. Mora em: handler de criar/atualizar produto (`CriarCatalogoProdutoCommandHandler` / `AtualizarCatalogoProdutoCommandHandler`) + repo. Validada em: back + CA.

- **R16 — Vínculo opcional, não-quebra**: o vínculo é **opcional** em criar e atualizar produto. Produtos existentes nascem com `item_inventario_id = NULL` após a migration (sem default que invente vínculo). Salvar produto sem vínculo é válido e não gera erro. Mora em: domínio + handler. Validada em: back + CA (regressão).

- **R17 — Sinalização no preview do modal F4**: o preview de "produtos a baixar do estoque" do modal de confirmação (briefing original §6) lista os produtos resolvidos dos procedimentos. Os produtos **sem item de estoque vinculado** aparecem sinalizados discretamente (ex.: badge/texto auxiliar "sem item de estoque vinculado", visual neutro/informativo — **não** é erro, **não** bloqueia confirmar). Objetivo: tornar a lacuna de configuração visível ao usuário. Os produtos **com** vínculo aparecem com a quantidade a baixar (como no original). Mora em: query de preview + modal (`MarcarProcedimentoRealizadoModal.vue`). Validada em: front + CA. **Não-PII** (R9 original mantém-se): nome de produto/item de estoque não é dado clínico.

- **R18 — RBAC da configuração do vínculo**: configurar o `item_inventario_id` do produto usa **a mesma permissão do catálogo de orçamento** já vigente no `OrcamentoCatalogoController`: `[RequiresAcao("orcamento", "configurar")]` (ou seja, `orcamento.configurar`). **Não** se introduz permissão nova. O botão/campo de vínculo fica oculto/desabilitado sem essa permissão; back retorna 403. Observação: o RBAC da **ação de marcar realizado** (F4, fluxo de baixa) permanece `prontuario.editar` (R12 original) — são duas ações distintas: configurar o catálogo (orcamento.configurar) ≠ executar a baixa ao marcar realizado (prontuario.editar). Mora em: controller. Validada em: back + front + CA.

- **R19 — Regressão da aba Produtos**: o CRUD existente da aba Produtos (listar/criar/editar/inativar/reativar produto, campos `nome`, `descricao`, `valorReferencia`, `usoUnico`, `tipo`, `marca`, `unidade`, `fornecedorNome`, `codigoSku`) continua **intacto**. O `item_inventario_id` é um campo **adicional**, não substitui nem reordena os existentes. A vinculação cirurgia↔produto (`orcamento_catalogo_cirurgia_produto`) e os pacotes (`orcamento_pacote_produto`) **não** mudam. Mora em: DTO/command/handler/front. Validada em: CA de regressão.

## 4. Modelo de dados (delta)

**ALTER `orcamento_catalogo_produto`** (entidade `CatalogoProduto`) — espelha 1:1 o `orcamento_catalogo_implante`:
- `ADD COLUMN item_inventario_id bigint NULL`.
- **FK fraca** para `itens_inventario(id)` com `ON DELETE SET NULL` — mesma semântica de `fk_catalogo_implante_item_inventario` (deletar um item de inventário **não** apaga o produto do catálogo; só desfaz o vínculo). Nome sugerido: `fk_catalogo_produto_item_inventario`.
- **Índice**: criar índice em `(item_inventario_id) WHERE item_inventario_id IS NOT NULL` **somente se** o DB agent julgar útil para a query de baixa (lookup produto→item). Como a baixa parte do produto (PK) e não filtra por `item_inventario_id`, o índice provavelmente **não** é necessário para a F4 — DB agent decide pelo padrão da casa (o implante não tem índice por `item_inventario_id`, então o default é **não criar**, mantendo consistência).

**Sem tabela nova. Schema do briefing original (`cobrancas.evolucao_id` + índice UNIQUE parcial) permanece inalterado** — este addendum apenas adiciona a coluna que faltava no catálogo de produto.

**Migration**: EF Core + SQL idempotente em `db/migrations/`, multi-tenant preservado (a coluna não altera o filtro de tenant; `estabelecimento_id` do produto já existe).

## 5. UX (delta)

**Aba Produtos (`ProdutosTab.vue`)** — no drawer/form de criar/editar produto, adicionar um `AppField`/`AppSelect` (design system) **"Item de estoque vinculado"**, opcional:
- Fonte: `inventarioService.listarItens({ apenasAtivos: true, tamanho: 500 })` (reuso do padrão de `OutrasConfigsTab.vue`).
- Opções: `{ value: "", label: "Nenhum" }` + itens `{ value: String(i.id), label: "{codigo} — {nome}" }`.
- Persistência: envia `itemInventarioId` (number | null) no payload de criar/atualizar produto.
- Edição: pré-seleciona o item vinculado atual (o DTO de listagem do produto passa a expor `itemInventarioId` + `itemInventarioNome`, espelhando `CatalogoImplanteDto`).
- Tipografia via tokens (CLAUDE.md §5), botões `AppButton`, sem CSS literal de fonte.

**Modal de confirmação F4 (`MarcarProcedimentoRealizadoModal.vue`)** — no preview "produtos a baixar":
- Produtos **com** vínculo: nome + quantidade a baixar (como no original).
- Produtos **sem** vínculo: sinalização discreta "sem item de estoque vinculado" (texto auxiliar/badge neutro). Informativo, não bloqueia confirmar.

## 6. Critérios de aceite incrementais (testáveis — CA90+)

- **CA90** (vínculo configurável — persistência): Dado um usuário com `orcamento.configurar` na aba Produtos, Quando edita um produto e seleciona um item de estoque do tenant ativo no campo "Item de estoque vinculado" e salva, Então `orcamento_catalogo_produto.item_inventario_id` passa a referenciar o `ItemInventario` escolhido; Quando reabre o produto, o select vem pré-selecionado com esse item.

- **CA91** (vínculo opcional — não-quebra): Dado a criação/edição de um produto **sem** selecionar item de estoque (opção "Nenhum"), Quando salva, Então o produto é persistido com `item_inventario_id = NULL`, sem erro; e produtos pré-existentes à migration permanecem com `item_inventario_id = NULL`.

- **CA92** (multi-tenant do vínculo): Dado um usuário do estabelecimento B, Quando tenta vincular a um `ItemInventario` do estabelecimento A (id forjado no payload), Então o back rejeita com "não encontrado" genérico (sem revelar a existência do item alheio), o vínculo **não** é gravado, e nenhum log com PII é emitido. E o select do front nunca lista itens de outro tenant.

- **CA93** (baixa usa o vínculo — caminho feliz, integra com CA76): Dado uma evolução com procedimento cujo `catalogoCirurgiaId` tem produto vinculado a `orcamento_catalogo_cirurgia_produto`, e esse produto tem `item_inventario_id` setado e estoque suficiente, Quando o usuário marca o procedimento como realizado (fluxo F4), Então é gerada `MovimentacaoEstoque` de saída para o `ItemInventario` vinculado, com `custo_unitario` = snapshot do `CustoMedio` — ou seja, a baixa automática **produz movimentação real** (o objetivo central da F4, que o gap original impedia).

- **CA94** (produto sem vínculo — ignorado + sinalizado no preview): Dado um procedimento cujo produto consolidado **não** tem `item_inventario_id` (NULL), Quando o usuário abre o modal de confirmação, Então esse produto aparece no preview sinalizado como "sem item de estoque vinculado"; Quando confirma, Então **nenhuma** `MovimentacaoEstoque` é gerada para esse produto, a cobrança é gerada normalmente e não há erro (R5 original preservada). Se **todos** os produtos do procedimento estão sem vínculo, o resultado equivale à baixa vazia (CA79 do original).

- **CA95** (RBAC da config do vínculo): Dado um usuário **sem** `orcamento.configurar`, Quando tenta salvar o vínculo do produto (PUT/POST de produto), Então recebe 403 e o campo "Item de estoque vinculado" fica oculto/desabilitado na aba Produtos. (O RBAC da ação de marcar realizado permanece `prontuario.editar`, CA82 do original — inalterado.)

- **CA96** (regressão — aba Produtos): Dado o CRUD existente de produtos do catálogo (criar/editar/inativar/reativar; campos nome, descrição, valor, uso único, tipo, marca, unidade, fornecedor, SKU), Quando o usuário opera a aba Produtos sem tocar no novo campo, Então tudo funciona como antes da F4; e as vinculações cirurgia↔produto e pacote↔produto permanecem inalteradas.

## 7. Schema delta para o DB agent (resumo)

```
ALTER TABLE orcamento_catalogo_produto
  ADD COLUMN item_inventario_id bigint NULL;

ALTER TABLE orcamento_catalogo_produto
  ADD CONSTRAINT fk_catalogo_produto_item_inventario
  FOREIGN KEY (item_inventario_id) REFERENCES itens_inventario(id)
  ON DELETE SET NULL;
-- Índice por item_inventario_id: NÃO criar (consistente com orcamento_catalogo_implante,
-- que não indexa esse campo; a baixa parte da PK do produto, não filtra por este FK).
-- DB agent confirma o nome real da tabela de inventário (itens_inventario) e ajusta se diferir.
```

Espelha 1:1 `orcamento_catalogo_implante` (`item_inventario_id` + `fk_catalogo_implante_item_inventario` / `OnDelete SetNull`). Migration EF Core + SQL idempotente. Nenhuma tabela nova. O schema de `cobrancas` do briefing original (`evolucao_id` + índice UNIQUE parcial) **continua valendo** e é independente deste delta.

## 8. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — na seção da F4 (Cobranças / baixa automática de estoque) **acrescentar 1 linha incremental**: a baixa automática resolve o `ItemInventario` via `orcamento_catalogo_produto.item_inventario_id` (vínculo opcional configurável na aba Produtos do catálogo de orçamento, espelhando `orcamento_catalogo_implante`); produto sem vínculo é ignorado na baixa. Atualização **cirúrgica** — não reescrever a seção.
- **`Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md`** — na linha da F4, anexar referência ao addendum (`+ addendum vínculo produto↔estoque`).
- **`Docs/LGPD.md`** — **sem delta** (nome de produto/item de estoque não é PII clínica; nada muda no audit de paciente).
- **`Docs/DESIGN.md`** — **sem delta** (o select reusa `AppField`/`AppSelect` existentes; nenhum componente novo).
- **`Docs/COMANDOS.md` / `INFRA.md`** — sem delta.

## 9. Observações para execução

**Não-negociável**:
- Espelhar o padrão `CatalogoImplante` (não inventar mecanismo novo): `ItemInventarioId long? NULL`, FK fraca `OnDelete(SetNull)`, DTO com `itemInventarioId` + `itemInventarioNome`, select via `inventarioService.listarItens`.
- Vínculo **opcional** — R5/R6 do original preservadas (produto sem vínculo → ignorado, não bloqueia cobrança).
- Multi-tenant no vínculo (CA92) — back valida que o item é do tenant.
- Tudo o mais do briefing original (atomicidade, idempotência dupla, CA76–CA89) **continua valendo sem alteração**.

**Liberdade técnica do dev**:
- Reusar `CriarProdutoDto` para carregar `itemInventarioId` (recomendado, espelha o `CriarImplante`) ou campo análogo.
- Se a query de preview de produtos (original §6) já existe, estendê-la para expor o flag "tem vínculo" em vez de duplicar.
- Decisão final do nome do índice/constraint fica com o DB agent (consistência com a casa).

**Aciona `imedto-database`**: SIM — ALTER em `orcamento_catalogo_produto` (add `item_inventario_id` + FK fraca SetNull). Ver §7.
