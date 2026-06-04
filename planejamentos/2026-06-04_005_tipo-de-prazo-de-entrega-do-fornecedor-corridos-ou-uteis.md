# Tipo de prazo de entrega do fornecedor (dias corridos ou úteis)

**ID**: 2026-06-04_005
**Status**: Aprovado por usuário em 2026-06-04
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P
**Áreas regressivas tocadas**: estoque (cadastro de fornecedor)

## 1. Contexto e motivação

Hoje o fornecedor de estoque tem o campo `PrazoEntregaDias` (inteiro, dias). A tela
mostra "5d", o pedido de compra usa esse número, mas **não há distinção entre dias
corridos e dias úteis**. Para a operação isso importa: "5 dias úteis" e "5 dias
corridos" são prazos diferentes na prática, e quem cadastra o fornecedor sabe qual é
o combinado comercial. Sem essa marcação, o número fica ambíguo e a recepção/comprador
precisa lembrar "de cabeça" qual fornecedor conta de que jeito.

A demanda é **apenas registrar e exibir a natureza do prazo** (corridos vs. úteis) como
rótulo informativo. **Não há cálculo de calendário/feriados nesta entrega** — o sistema
não converte prazo em data prevista nem desconta finais de semana/feriados. É uma
qualificação textual do número que já existe (decisão 1A do usuário).

## 2. Persona-alvo

- **Comprador / responsável de estoque / recepção** que cadastra ou edita fornecedores
  na aba **Fornecedores** (Inventário → Cadastros), e que cria fornecedor no atalho
  rápido "+ Novo" durante o cadastro de produto.
- Frequência: baixa (cadastro de fornecedor é evento esporádico), mas o rótulo é lido
  toda vez que a listagem de fornecedores é consultada.

## 3. Escopo

**Inclui**:
- Novo campo no fornecedor: **tipo de prazo de entrega**, com dois valores possíveis —
  `corridos` (dias corridos) e `uteis` (dias úteis).
- Persistência: nova coluna na tabela `fornecedores_estoque` + migration com backfill
  `'corridos'` para todos os registros existentes (decisão 2A).
- Backend: Domain, Command (criar/atualizar), DTO de leitura, Handler, EF Config,
  validação de valor (`corridos | uteis`).
- Frontend: seletor segmentado **"Corridos | Úteis"** (`AppPillToggle`) nos **dois**
  pontos de cadastro — drawer completo "Novo/Editar fornecedor" (`CadastroFornecedoresTab.vue`)
  e modal rápido (`ModalNovoFornecedorRapido.vue`) — decisão 4A.
- Exibição do tipo de prazo na listagem de fornecedores, junto da coluna "Prazo" que já
  mostra "5d" → passa a "5d corridos" / "5d úteis".

**Não inclui**:
- Cálculo de data prevista de entrega, contagem de dias úteis, integração com calendário
  de feriados (decisão 1A — apenas rótulo).
- Mudança no pedido de compra / fluxo de compras além de carregar o novo dado.
- Configuração de calendário de feriados por estabelecimento.
- Documentar `AppPillToggle` no design system (o componente já existe; reuso puro — fica
  como nota de backlog, ver §10).

## 4. Regras de negócio

- **R1**: O tipo de prazo de entrega só pode assumir os valores `corridos` ou `uteis`.
  Qualquer outro valor é rejeitado. Mora em: **Domain** (`FornecedorEstoque.Criar` /
  `Atualizar`, via método de validação) com `BusinessException`. Validada em: **back**
  (fonte da verdade, 422) **+ front** (toggle só permite as duas opções, default coerente).
- **R2**: Todo fornecedor tem obrigatoriamente um tipo de prazo. Não existe fornecedor
  com tipo nulo. **Default ao criar (novo cadastro)** = `corridos`. Mora em: **Domain**
  (valor default no `Criar` se o command não trouxer) + **Front** (toggle já abre com
  `corridos` selecionado). Validada em: **back + front**.
- **R3**: Registros pré-existentes (criados antes desta entrega) recebem `corridos` via
  backfill da migration (decisão 2A). Mora em: **migration SQL idempotente**.
- **R4**: O tipo de prazo é dado **operacional**, não PII e não dado de saúde. Sem audit
  trail. Segue a minimização do estoque: entra no DTO de listagem porque a tela exibe;
  **não** entra no DTO `/opcoes` (dropdown ultra-leve só id+nome). Mora em: **Query/DTO**.

## 5. Modelo de dados

**Tabela**: `fornecedores_estoque` (já existe; multi-tenant por `estabelecimento_id`).

**Coluna nova**:
- `tipo_prazo_entrega` — `varchar(10)`, `NOT NULL`, `DEFAULT 'corridos'`,
  com `CHECK (tipo_prazo_entrega IN ('corridos','uteis'))`.
  - Decisão de tipo: **varchar com CHECK constraint** (não enum nativo do Postgres), por
    consistência com o padrão do projeto e simplicidade de migration/backfill. O
    `imedto-database` é o autor final da migration e pode ajustar para enum .NET ↔ string
    se preferir, **desde que o valor persistido seja a string `corridos`/`uteis`** e o
    CHECK garanta o domínio.

**Migration** (EF Core + SQL idempotente em `db/migrations/`, autoria `imedto-database`):
1. `ADD COLUMN tipo_prazo_entrega varchar(10) NOT NULL DEFAULT 'corridos'` (o default já
   faz o backfill dos registros existentes — todos viram `corridos`, decisão 2A).
2. `ADD CONSTRAINT ... CHECK (tipo_prazo_entrega IN ('corridos','uteis'))`.
3. Idempotente (`IF NOT EXISTS` / guarda equivalente), padrão do projeto.

**Índice**: nenhum novo. O campo não é filtro de busca nem de ordenação. O índice
existente `ix_fornecedores_estoque_estab_ativo` permanece suficiente.

**Audit / LGPD**: não aplicável (dado operacional, sem PII).

## 6. UX e fluxo

**Componente reutilizado**: `AppPillToggle` (design system, `frontend/src/components/ui/`).
Já é usado em `ListasVariaveisTab.vue`. API:
`<AppPillToggle v-model="tipo" :opcoes="[{ valor: 'corridos', label: 'Corridos' }, { valor: 'uteis', label: 'Úteis' }]" />`.
**Não criar componente novo.** Não usar AppSelect nem par de AppButton — `AppPillToggle`
é exatamente o toggle segmentado pedido (decisão 3A confirmada: componente já existe).

**Ponto 1 — Drawer completo "Novo/Editar fornecedor"** (`CadastroFornecedoresTab.vue`):
- O campo "Prazo de entrega (dias)" hoje é um `AppInput type=number`. Acrescentar, ao
  lado/abaixo dele, o `AppPillToggle` "Corridos | Úteis" dentro de um `AppField`
  (label sugerido: "Tipo de prazo" ou agrupado visualmente com o input de dias).
- **Ao abrir "Novo"**: toggle inicia em `corridos` (default), igual ao reset de `form`
  que já zera os outros campos.
- **Ao abrir "Editar"**: toggle reflete o valor salvo do fornecedor.

**Ponto 2 — Modal rápido** (`ModalNovoFornecedorRapido.vue`):
- Acrescentar o mesmo `AppPillToggle` ao lado do "Prazo de entrega (dias)" existente.
- Ao abrir o modal (`watch props.aberto`), o `form` reseta com `tipoPrazoEntrega: 'corridos'`.

**Listagem** (`CadastroFornecedoresTab.vue`, coluna "Prazo"):
- Hoje: `{{ f.prazoEntregaDias }}d` → "5d".
- Passa a: `{{ f.prazoEntregaDias }}d {{ f.tipoPrazoEntrega === 'uteis' ? 'úteis' : 'corridos' }}`
  → "5d corridos" / "5d úteis". (Texto curto, cabe na coluna estreita; se quebrar layout,
  abreviar para "5d út." / "5d cor." — liberdade do dev manter legível.)

**Estados de UI**:
- loading / erro / vazio: inalterados (já existem na tab).
- O toggle nunca fica vazio — sempre há uma opção selecionada (`corridos` no mínimo).

**Mobile-ready**: `AppPillToggle` já é responsivo; manter dentro do grid existente do form.

**Service/tipos** (`frontend/src/services/estoqueCadastrosService.ts`):
- `interface FornecedorEstoque`: adicionar `tipoPrazoEntrega: 'corridos' | 'uteis'`.
- `interface FornecedorPayload`: adicionar `tipoPrazoEntrega: 'corridos' | 'uteis'`.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — criar com úteis)**: Dado o drawer "Novo fornecedor" aberto,
  Quando o usuário preenche razão social, define prazo 5, seleciona "Úteis" no toggle e
  salva, Então o fornecedor é persistido com `tipo_prazo_entrega = 'uteis'` e a listagem
  exibe "5d úteis".

- **CA2 (default ao criar)**: Dado o drawer ou o modal rápido recém-aberto para novo
  fornecedor, Quando nenhuma interação no toggle ocorre, Então o toggle exibe "Corridos"
  selecionado e o fornecedor salvo fica com `tipo_prazo_entrega = 'corridos'`.

- **CA3 (editar reflete o salvo)**: Dado um fornecedor existente com
  `tipo_prazo_entrega = 'uteis'`, Quando o usuário abre "Editar", Então o toggle abre com
  "Úteis" selecionado; e ao trocar para "Corridos" e salvar, Então o registro passa a
  `corridos`.

- **CA4 (modal rápido também marca)**: Dado o modal `ModalNovoFornecedorRapido` aberto a
  partir do "+ Novo" do cadastro de produto, Quando o usuário seleciona "Úteis" e cria o
  fornecedor, Então o fornecedor é criado com `tipo_prazo_entrega = 'uteis'`.

- **CA5 (validação de valor — backend)**: Dado um command de criar/atualizar fornecedor
  com `tipoPrazoEntrega` fora de `{corridos, uteis}` (ex.: `"semanal"` ou vazio inválido),
  Quando o handler processa, Então o domínio lança `BusinessException` e a API responde
  422 com mensagem genérica de validação, sem persistir.

- **CA6 (backfill da migration)**: Dado fornecedores cadastrados antes desta entrega
  (sem a coluna), Quando a migration é aplicada, Então todos passam a ter
  `tipo_prazo_entrega = 'corridos'` e a coluna é `NOT NULL`.

- **CA7 (multi-tenant)**: Dado um usuário do estabelecimento B, Quando tenta atualizar o
  tipo de prazo de um fornecedor do estabelecimento A (via `AtualizarFornecedorEstoqueCommand`
  com `EstabelecimentoId` de B), Então `ObterPorIdOuNulo(id, B)` retorna nulo, o handler
  lança "Fornecedor não encontrado." (mensagem genérica) e nada é alterado em A.
  (Repo confirmado falha-fechada: todo método de `FornecedorEstoqueRepository` filtra
  `EstabelecimentoId`; query Dapper tem `WHERE estabelecimento_id` em todo SELECT.)

- **CA8 (LGPD / minimização)**: Dado o endpoint `/opcoes` de fornecedores (dropdown
  ultra-leve), Quando consultado, Então o payload continua apenas `{ id, nome }` — o
  `tipoPrazoEntrega` **não** é incluído ali. E o DTO de listagem (`FornecedorEstoqueDto`)
  carrega `tipoPrazoEntrega` porque a tela o exibe — minimização preservada (só o que a
  tela usa).

- **CA9 (estados / toggle sempre preenchido)**: Dado o toggle de tipo de prazo em
  qualquer um dos dois formulários, Quando renderizado, Então sempre há exatamente uma
  opção selecionada (nunca estado vazio) e clicar alterna entre "Corridos" e "Úteis".

- **CA10 (exibição na listagem)**: Dado a listagem de fornecedores carregada, Quando há
  fornecedores com tipos distintos, Então a coluna "Prazo" mostra "Nd corridos" para os
  `corridos` e "Nd úteis" para os `uteis`, sem quebrar o layout da linha.

## 8. Riscos e dependências

- **Migration com `NOT NULL` + `DEFAULT`**: seguro porque o default backfilla os
  existentes no mesmo statement. Sem janela de inconsistência. Padrão idempotente já
  usado no projeto.
- **Dapper SELECT desatualizado**: o novo campo precisa ser adicionado ao SELECT da
  `ListarFornecedores` (`CadastrosEstoqueQueryRepository.cs`, bloco "Fornecedores") **e**
  ao DTO `FornecedorEstoqueDto`. Se esquecer um dos dois, o front recebe `undefined` →
  exibiria "corridos" por fallback, mascarando bug. QA deve validar com fornecedor `uteis`.
- **Espelho back+front**: o front é só UX; a validação de domínio (R1) é a trava real.
  Não confiar apenas no toggle.
- **Áreas regressivas**: apenas cadastro de fornecedor de estoque. Não toca prontuário,
  agenda, financeiro, permissionamento.

## 9. Observações para execução

**Não-negociável**:
- Valor persistido é a string `corridos` / `uteis` (minúsculo, sem acento). O label com
  acento ("Úteis") é só apresentação no front.
- Validação de domínio no `FornecedorEstoque` (Domain), não no controller nem no SQL.
- Default `corridos` tanto no Domain (segurança) quanto no front (UX).
- DTO `/opcoes` permanece `{ id, nome }` — não vazar o campo lá.
- Reuso de `AppPillToggle` — não criar componente novo, não trocar por AppSelect.

**Liberdade técnica** (`imedto-developer` / `imedto-database`):
- Representação .NET do campo: string simples no aggregate (`string TipoPrazoEntrega`) ou
  enum .NET mapeado para string via conversion — escolha do dev/db, desde que persista a
  string do domínio e respeite o CHECK.
- Tipo SQL exato (varchar+CHECK vs enum nativo) — decisão final do `imedto-database`,
  contanto que o domínio fique `{corridos, uteis}` e o backfill seja `corridos`.
- Layout exato do toggle no form (ao lado vs. abaixo do input de dias) — manter coerente
  com o grid existente e legível em mobile.

**Pontos de código mapeados** (para o dev não caçar):
- Domain: `backend/.../Domain/Inventario/Cadastros/FornecedorEstoque.cs` (`Criar`, `Atualizar`, novo validador).
- Command: `backend/.../Contracts/Inventario/Cadastros/Commands/FornecedorEstoqueCommands.cs` (Criar + Atualizar).
- Handler: `backend/.../Application/Inventario/Cadastros/Commands/FornecedorEstoqueHandlers.cs` (passar o campo ao `Criar`/`Atualizar`).
- DTO: `backend/.../Contracts/Inventario/Cadastros/Queries/Results/CadastrosDtos.cs` (`FornecedorEstoqueDto`).
- EF Config: `backend/.../Infrastructure/Database/Configurations/Cadastros/FornecedorEstoqueConfiguration.cs`.
- Query Dapper: `backend/.../Infrastructure/Database/Repositories/Cadastros/CadastrosEstoqueQueryRepository.cs` (bloco "Fornecedores", adicionar `f.tipo_prazo_entrega AS TipoPrazoEntrega` aos dois SELECTs relevantes — o de itens).
- Front service/tipos: `frontend/src/services/estoqueCadastrosService.ts` (`FornecedorEstoque`, `FornecedorPayload`).
- Front forms: `frontend/src/components/estoque/cadastros/CadastroFornecedoresTab.vue` e `frontend/src/components/estoque/cadastros/modais/ModalNovoFornecedorRapido.vue`.
- Testes existentes a atualizar: `FornecedorEstoqueTests.cs`, `FornecedorEstoqueHandlersTests.cs`, `ModalNovoFornecedorRapido.test.ts`.

## 10. Atualização de documentação

- **Nenhum doc de `Docs/` precisa ser atualizado nesta entrega.** A demanda segue padrões
  já documentados (cadastro de estoque multi-tenant, CQRS, design system, minimização) e
  apenas reutiliza o componente `AppPillToggle` já existente.
- **Nota de backlog** (não bloqueia esta entrega): `AppPillToggle` está no design system
  mas ainda **não consta em `Docs/DESIGN.md`** na seção de componentes. Registrar como
  pendência separada para o BA/dev documentar o componente de toggle segmentado num ciclo
  futuro — não inflar esta demanda com isso (Surgical Changes).
