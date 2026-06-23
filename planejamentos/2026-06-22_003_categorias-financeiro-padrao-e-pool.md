# Categorias financeiras: lista padrão global (admin) + pool por estabelecimento + seletor no modal de lançamento

**ID**: 2026-06-22_003
**Status**: Aprovado por usuário em 2026-06-22
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G (épico em 5 marcos sequenciais)
**Áreas regressivas tocadas**: financeiro (categorias, lançamentos, seed por estabelecimento, admin global Catálogos)

---

## 1. Contexto e motivação

Hoje, no Financeiro, o campo **"Categoria"** do modal **"Novo lançamento"** (`frontend/src/views/financeiro/tabs/VisaoGeralTab.vue`, linha 408) é um `<AppInput>` de **texto livre**. Cada lançamento grava o que o usuário digitou, sem padronização. Consequências:

- Categorias divergem por digitação ("Aluguel", "aluguel", "Aluguél", "ALUGUEL") — relatórios por categoria ficam fragmentados e inúteis.
- O estabelecimento já tem um CRUD de categorias (`/financeiro/categorias` + `CategoriasFinanceirasView.vue`) e um seed automático ao criar o estabelecimento, mas o modal de lançamento **ignora** esse pool. O investimento existente não é aproveitado.
- O seed padrão atual (`SeedsFinanceiro.cs`) é mínimo, hardcoded e com nomes prefixados ("Receita: Consulta", "Despesa: Folha") — feio no seletor e não governável pela plataforma.

A plataforma quer **uma lista de categorias padrão única, gerida pelo admin global**, semeada em todo estabelecimento (espelhando o padrão já provado de `ModelosPermissaoPadraoSistema`), e cada estabelecimento podendo criar categorias próprias. O campo do modal vira um **seletor desse pool** com opção de adicionar categoria nova **inline**.

**Evidência de reuso (premissa do projeto):**
- Entidade `CategoriaFinanceira` (`Domain/Financeiro/CategoriaFinanceira.cs`): `Nome`, `Tipo` (enum `TipoCategoria` Receita|Despesa), `Padrao` (bool), `Ativo` (bool), multi-tenant por `EstabelecimentoId`. Tabela `categorias_financeiras`, EF Configuration e CRUD completo já existem.
- CRUD por estabelecimento: `GET/POST/PUT/POST .../inativar` em `FinanceiroController` sob `[RequiresAcao("financeiro")]`. `ListarCategoriasFinanceirasQuery` **já aceita** filtro por `Tipo`, `Ativas` e `Padrao`. Front: `categoriaFinanceiraService.ts`.
- Seed por estabelecimento: `CriarSeedFinanceiroAoCriarEstabelecimentoHandler` reage a `EstabelecimentoCriadoEvent` e cria via `CategoriaFinanceira.CriarPadrao` a partir do hardcode de `SeedsFinanceiro.cs`.
- Padrão GLOBAL provado: `ModelosPermissaoPadraoSistema` (admin Catálogos) — registro global editável pelo admin propagado **por cópia materializada** a cada estabelecimento; novo estabelecimento recebe as cópias no momento da criação (`CriarModeloPadraoAoCriarEstabelecimentoHandler`). Admin já tem módulo Catálogos (`frontend/src/modules/admin/`) com list+form views, `catalogosService.ts`, rotas `catalogos/*` e auditoria via `ImedtoAdminAuditWriter`.
- `Lancamento.Categoria` é **STRING LIVRE, não FK** (`Domain/Financeiro/Lancamento.cs:16`). O seletor grava o **nome** escolhido → **não exige migration na tabela de lançamentos** e **não exige backfill de dados antigos**.

---

## 2. Persona-alvo

- **Admin da plataforma** (admin global) — gere a lista padrão única no módulo Catálogos. Baixa frequência (ajuste de catálogo).
- **Recepção / Financeiro / Dono** (usuário com permissão `financeiro`) — lança receitas/despesas no dia a dia e, ocasionalmente, cria/oculta categorias próprias do estabelecimento. Alta frequência (no fluxo de cada lançamento).

---

## 3. Escopo

**Inclui (escopo COMPLETO, em marcos sequenciais):**

- **M1 — Schema + seed global**: nova tabela global `categorias_financeiras_padrao_sistema` (nome + tipo + ativo), semeada com a lista aprovada (nomes limpos). Domínio + EF Configuration + migration + seed de dados.
- **M2 — Seed por estabelecimento lê do global + propagação**: `CriarSeedFinanceiroAoCriarEstabelecimentoHandler` passa a ler do global em vez do hardcode de `SeedsFinanceiro.cs`. Editar o catálogo global propaga (por cópia, idempotente) aos estabelecimentos existentes.
- **M3 — Admin global UI (Catálogos)**: tela no admin para listar/criar/inativar/reativar as categorias padrão globais, espelhando `PermissoesGlobais`/`VariaveisGlobais`. Auditoria.
- **M4 — Frontend: seletor + adicionar inline no modal**: o campo "Categoria" do modal "Novo lançamento" vira seletor filtrado pelo **tipo** do lançamento, com opção "+ Adicionar nova categoria" inline (cria SÓ para aquele estabelecimento via `POST /financeiro/categorias` existente, e já seleciona). Continua obrigatório.
- **M5 — Ocultar (inativar) categoria padrão no estabelecimento**: flexibiliza a proteção atual para permitir que o estabelecimento **inative** (Ativo=false) uma categoria padrão que não usa, sem poder renomear/excluir.

**Não inclui:**

- **Migrar/backfill de lançamentos antigos** — texto antigo permanece como está; o seletor governa só novos lançamentos.
- Tornar `Lancamento.Categoria` uma FK — continua string.
- Editar/excluir categoria padrão pelo estabelecimento (segue proibido; só inativar é liberado — M5).
- Categorias de **forma de pagamento** (`FormaPagamento`) — fora de escopo; só categorias.
- Reordenação/cores/ícones de categoria — não pedido.
- Vínculo categoria↔plano de contas / DRE — não pedido.

---

## 4. Regras de negócio

- **R1 — Lista padrão única (global)**: existe uma única lista de categorias padrão da plataforma, gerida pelo admin global. Conteúdo aprovado (nomes limpos, **sem** prefixo "Receita:/Despesa:"):
  - **Receitas**: Consultas, Procedimentos, Exames, Cirurgias, Repasses de convênio, Venda de produtos, Outras receitas.
  - **Despesas**: Folha de pagamento, Pró-labore, Aluguel, Contas de consumo, Insumos e materiais, Equipamentos, Marketing, Impostos e taxas, Manutenção, Limpeza, Software/assinaturas, Outras despesas.
  - Mora em: tabela global `categorias_financeiras_padrao_sistema` + seed de dados. Substitui o hardcode de `SeedsFinanceiro.cs` (Categorias). **`SeedsFinanceiro.FormasPagamento` permanece intocado.**

- **R2 — Seed por estabelecimento lê do global**: ao criar um estabelecimento, o seed cria uma **cópia** (`CategoriaFinanceira.CriarPadrao`, `Padrao=true`) de cada categoria padrão **ativa** do global. Mora em: `CriarSeedFinanceiroAoCriarEstabelecimentoHandler`. Validado em: back.

- **R3 — Propagação ao adicionar nova categoria padrão global**: ao admin **criar** uma categoria padrão global, materializa a cópia (`Padrao=true`, `Ativo=true`) em **cada estabelecimento existente que ainda não a possui** (mesmo `nome`+`tipo`). Idempotente — não duplica em re-execução nem para quem já tem. Mora em: handler do command de criação no admin (espelha `CriarModeloPermissaoPadraoSistemaCommandHandler`). Validado em: back.

- **R4 — Propagação ao inativar categoria padrão global**: ao admin **inativar** uma categoria padrão global, refletir nas cópias dos estabelecimentos: as cópias correspondentes (mesmo `nome`+`tipo`, `Padrao=true`) ficam `Ativo=false`. Não remove a coluna nem afeta lançamentos já gravados. Mora em: handler do command de inativação no admin. Validado em: back.
  - **R4.1 — Reativar global**: ao reativar a padrão global, reflete reativando as cópias `Padrao=true` correspondentes. (Cópias que o estabelecimento tenha inativado manualmente por M5 também voltam a Ativo=true — aceitável: reativação global é um "ligar de novo para todos"; o estabelecimento pode inativar de novo.)

- **R5 — Renomear categoria padrão global**: o nome é a chave de negócio e está gravado como string nas cópias e nos lançamentos históricos. Para **não quebrar histórico** e **não introduzir ambiguidade de identidade**, o nome da categoria padrão global **é imutável** após criada. O admin que quer "trocar o nome" inativa a antiga e cria uma nova. Mora em: handler de atualização do admin (recusa alteração de nome) / ausência de endpoint de rename. Validado em: back.
  - Justificativa: as cópias por estabelecimento são identificadas por `nome`+`tipo` (não há FK do tenant para o registro global); renomear o global sem renomear todas as cópias quebraria a propagação futura, e renomear as cópias não corrige os lançamentos antigos que já gravaram o texto. Imutabilidade do nome é a opção mais simples e segura.

- **R6 — Seletor no modal filtrado por tipo**: o seletor "Categoria" do modal lista as categorias **ativas** do estabelecimento filtradas pelo **tipo** atual do lançamento (Receita/Despesa). Ao trocar o tipo, a lista do seletor atualiza e a categoria selecionada que não pertença ao novo tipo é limpa. Categoria continua **obrigatória**. Mora em: `VisaoGeralTab.vue` (apresentação) + `ListarCategoriasFinanceirasQuery` (filtro `Tipo`+`Ativas=true`, já existente). Validado em: front (UX) — o back não valida que a string seja de uma categoria existente (continua string livre; o seletor é guard-rail de UX, não invariante de domínio).

- **R7 — Adicionar categoria inline (escopo do estabelecimento)**: a opção "+ Adicionar nova categoria" cria uma categoria **customizada** (`Padrao=false`) **só daquele estabelecimento**, via `POST /financeiro/categorias` existente (tipo = tipo atual do lançamento), e já a seleciona no campo. Mora em: `VisaoGeralTab.vue` + `CriarCategoriaFinanceiraCommandHandler` (já existe, com pré-validação de unicidade nome+tipo). Multi-tenant garantido pelo handler/repo existentes (`EstabelecimentoId` do contexto). Validado em: back (criação) + front (fluxo inline).
  - **R7.1 — Colisão de nome inline**: se o nome já existe no estabelecimento (ativa ou inativa), o `POST` retorna `422` ("Já existe uma categoria com este nome e tipo."). O front exibe a mensagem genérica e **não** cria duplicata. **Atenção ao índice único real** (ver §5): a unicidade no banco é por `(estabelecimento_id, nome)` — não inclui tipo. Logo, um mesmo nome não pode coexistir como Receita e Despesa no mesmo estabelecimento. A lista aprovada não tem colisão de nome entre tipos, então o seed é seguro; mas o tratamento de erro do inline deve assumir colisão por nome (não nome+tipo).

- **R8 — Inativar categoria padrão pelo estabelecimento (flexibiliza proteção atual)**: o estabelecimento **pode inativar** (`Ativo=false`) uma categoria `Padrao=true` que não usa, para limpar o seletor; **não pode renomear nem excluir** uma padrão. Isto altera o comportamento atual de `CategoriaFinanceira.Inativar()`, que hoje lança `BusinessException("Categoria padrão não pode ser inativada.")`. Mora em: `CategoriaFinanceira.Inativar()` (Domain) + `InativarCategoriaFinanceiraCommandHandler`. Validado em: back (Domain) + front (botão de inativar habilitado para padrão na `CategoriasFinanceirasView`).
  - `Atualizar()` (rename) **permanece bloqueado** para `Padrao=true`. `Excluir`/`DELETE` **permanece bloqueado** para `Padrao=true`.
  - O estabelecimento pode **reativar** uma padrão que inativou (`Reativar()` já existe e não bloqueia padrão).

- **R9 — Multi-tenant**: categoria de um estabelecimento nunca aparece para outro. O mesmo nome pode existir em estabelecimentos diferentes (cópias da padrão e/ou customizadas homônimas) — a unicidade é por `(estabelecimento_id, nome)`. Catálogo padrão global não tem `estabelecimento_id` (tabela separada, só admin acessa). Mora em: queries/handlers existentes (`ObterPorIdOuNulo(id, estabelecimentoId)`, `Listar(estabelecimentoId, ...)`). Validado em: back (repo falha-fechada por tenant).

- **R10 — RBAC**:
  - Gerir categorias **do estabelecimento** (criar/inativar/reativar/listar, inclusive inline) = permissão `financeiro` (mantém `[RequiresAcao("financeiro")]` do `FinanceiroController`).
  - Gerir **lista padrão global** = **só admin da plataforma** (módulo admin, autenticação admin + `ImedtoAdminAuditWriter`), espelhando `PermissoesGlobais`/`ModelosPermissaoPadraoSistema`.
  - Validado em: back (policies de cada controller) + front (botões só onde há permissão).

---

## 5. Modelo de dados

> **EXIGE MIGRATION** → acionar `imedto-database` na execução (M1). Tabela global nova + seed de dados + ajuste de comportamento de inativação de padrão (este é só Domain/C#, sem DDL).

**Tabela nova (global, escopo plataforma) — `categorias_financeiras_padrao_sistema`:**

| coluna | tipo | nota |
|---|---|---|
| `id` | identity bigint | PK |
| `nome` | varchar(80) | nome limpo (sem prefixo de tipo) |
| `tipo` | varchar(10) | enum `TipoCategoria` como string ("Receita"/"Despesa") — mesma convenção de `categorias_financeiras.tipo` |
| `ativo` | boolean | default true |
| `criada_em` | timestamptz | |
| `atualizada_em` | timestamptz null | |

- **Índice único**: `(nome, tipo)` — chave de negócio do catálogo global. (Aqui o tipo entra na unicidade — diferente da tabela de tenant, por ser tabela separada e para permitir clareza.)
- **Sem `estabelecimento_id`** — é registro global. Acesso só pelo admin. NÃO é PII.
- **Seed de dados** (migration de seed idempotente, espelha `..._seed.sql` de `ModelosPermissaoPadraoSistema`): inserir a lista aprovada de R1 com `ativo=true`. Usar `ON CONFLICT (nome, tipo) DO NOTHING` para idempotência.

**Tabela existente `categorias_financeiras` (por estabelecimento) — sem DDL nova:**
- Unicidade real hoje: `uq_categoria_financeira_estab_nome` = `(estabelecimento_id, nome)` **(não inclui tipo)**. Manter como está. Implicação registrada em R7.1.
- Índice de leitura: `ix_categoria_financeira_estab_tipo_ativo = (estabelecimento_id, tipo, ativo)` — já cobre a query do seletor (R6). **Sem índice novo necessário.**
- Nenhuma coluna nova. O comportamento de "padrão pode inativar" é regra de Domain (`Padrao=true` + `Ativo=false`), não precisa coluna.

**Tabela `lancamentos` — intocada.** `Categoria` continua string; sem migration; sem backfill (decisão fechada).

**LGPD**: catálogo de categorias não é dado de paciente nem PII. Sem audit de paciente. Audit do admin global (criar/inativar/reativar padrão) via `ImedtoAdminAuditWriter` (espelha `AcoesAuditAdmin.CriarModeloPermissaoPadraoSistema`) — registrar payload sem PII (nome da categoria + nº de instâncias propagadas).

---

## 6. UX e fluxo

### 6.1 Modal "Novo lançamento" (`VisaoGeralTab.vue`) — M4

Substituir o `<AppInput v-model="formCriar.categoria">` (linha 408) por um **seletor com adicionar inline**.

```
┌ Novo lançamento ─────────────────────────────┐
│ Tipo *        [ Receita ▾ ]                   │
│ Descrição *   [____________________________]  │
│ Categoria *   [ Selecione…           ▾ ]      │  ← seletor (categorias ATIVAS do tipo atual)
│               └ + Adicionar nova categoria    │  ← opção/ação inline
│ Valor (R$) *  [______]                        │
│ Vencimento *  [DD/MM/AAAA]                     │
└──────────────────────────────────────────────┘
```

- Carregamento **lazy**: as categorias do estabelecimento só são buscadas **quando o modal abre** (não no mount da aba). Reusar `categoriaFinanceiraService.listar()` (acrescentar suporte a `tipo` + `ativas=true` na query — a query backend já aceita; basta o service repassar params).
- **Filtragem por tipo (R6)**: ao trocar `formCriar.tipo`, refiltra a lista exibida e limpa a seleção se a categoria não for do novo tipo.
- **Adicionar inline (R7)**: ação "+ Adicionar nova categoria" abre um campo/mini-form inline (input de nome) dentro do próprio modal; ao confirmar, chama `POST /financeiro/categorias` `{ nome, tipo: formCriar.tipo }`, e ao sucesso recarrega/insere a nova categoria na lista e **já a seleciona**. Erro (422 colisão) mostra mensagem genérica inline e mantém o usuário no fluxo.
- **Componente do design system**: preferir reutilizar um componente de seleção existente. **Decisão técnica (livre, mas justificar no PR):** se já houver um `AppSelect` que aceite ação de rodapé/"adicionar", reusar; se for necessário um componente reutilizável "seletor com criação inline" (`AppComboboxCriavel` / `AppSelectCriavel`), criá-lo no design system (`frontend/src/components/ui/`) e **documentar em `Docs/DESIGN.md`**. Conferir antes se `ProntuarioVariavelPool` / "modelos e listas" já tem um combobox-com-adicionar reutilizável a estender (premissa reuso > duplicação).
- **Edição de lançamento antigo com categoria fora da lista** (UX sensata, decisão 4): se um lançamento antigo tem `Categoria` que não está no pool ativo do tipo, o seletor mostra **o valor atual como opção transitória selecionada** (rótulo do texto bruto), para não perder o dado nem forçar troca. Salvar sem mexer mantém o texto. Se houver tela de edição de lançamento hoje, aplicar lá também; se não houver edição de lançamento, este caso só vale se/quando existir — registrar como nota, não bloquear M4.

### 6.2 Tela do estabelecimento `CategoriasFinanceirasView.vue` — M5

- Categorias `Padrao=true`: botão **Inativar/Reativar habilitado** (antes desabilitado/oculto); botões **Editar** e **Excluir** continuam **ocultos/desabilitados** para padrão (com tooltip "Categoria padrão não pode ser editada/excluída").
- Estados: lista carregando (skeleton/loading), vazia (`AppEmptyState` "Nenhuma categoria cadastrada"), erro (toast genérico).

### 6.3 Admin global — Catálogos → "Categorias financeiras" — M3

- Nova entrada no menu do módulo Catálogos do admin (mesmo grupo de Modelos/Variáveis/Permissões/Regiões).
- `CategoriasFinanceirasGlobaisListView.vue`: lista (nome, tipo, ativo, criada em), com filtro por tipo e por ativo; ações Criar / Inativar / Reativar. **Sem editar nome (R5)** — a tela não oferece campo de rename.
- `CategoriasFinanceirasGlobaisFormView.vue` (ou modal de criação): nome + tipo. Ao salvar, mostra feedback "Categoria padrão criada e propagada para N estabelecimentos".
- Estados: loading, vazio, erro — espelhar `PermissoesGlobaisListView.vue`/`VariaveisGlobaisListView.vue`.
- Rotas em `frontend/src/modules/admin/router/*` no padrão `catalogos/categorias-financeiras` (+ `/novo` se usar form view). Service em `catalogosService.ts` (novos métodos `categoriasFinanceirasGlobaisService`).

---

## 7. Critérios de aceite (testáveis)

### M1 — Schema + seed global

- **CA1** (caminho feliz — schema): Dado o deploy da migration, Quando o banco é inspecionado, Então existe a tabela `categorias_financeiras_padrao_sistema` com colunas `id, nome, tipo, ativo, criada_em, atualizada_em` e índice único `(nome, tipo)`.
- **CA2** (seed): Dado a migration de seed aplicada (idempotente), Quando se lista o catálogo global, Então constam exatamente as 7 receitas e 12 despesas aprovadas (nomes limpos, sem prefixo "Receita:/Despesa:"), todas `ativo=true`; reexecutar o seed (`ON CONFLICT DO NOTHING`) não duplica nada.

### M2 — Seed por estabelecimento lê do global + propagação

- **CA3** (seed novo estabelecimento): Dado um estabelecimento recém-criado, Quando o `CriarSeedFinanceiroAoCriarEstabelecimentoHandler` roda, Então o estabelecimento recebe uma cópia `Padrao=true, Ativo=true` de cada categoria padrão **ativa** do global (7+12), e nenhuma com prefixo antigo.
- **CA4** (propagação ao criar global): Dado N estabelecimentos existentes e o admin cria uma nova categoria padrão global "Telemedicina/Receita", Quando o command conclui, Então cada estabelecimento que ainda não tinha "Telemedicina" recebe a cópia `Padrao=true`, e reexecutar/repropagar não cria duplicata em nenhum (idempotente).
- **CA5** (propagação ao inativar global): Dado a padrão global "Marketing/Despesa" ativa e propagada, Quando o admin a inativa, Então as cópias `Padrao=true` "Marketing" dos estabelecimentos ficam `Ativo=false`, e os lançamentos antigos com texto "Marketing" permanecem inalterados.
- **CA6** (rename bloqueado — R5): Dado uma categoria padrão global, Quando se tenta alterar seu nome, Então a operação é recusada (sem endpoint/ação de rename; ou `422` se rota existir) e nenhuma cópia é renomeada.

### M3 — Admin global UI

- **CA7** (RBAC admin): Dado um usuário **não-admin** (sessão de app regular), Quando tenta acessar `GET/POST` do catálogo global de categorias, Então recebe `403`/`401` (conforme guard admin) e a tela não aparece no menu do app regular.
- **CA8** (criar pelo admin + audit): Dado o admin logado, Quando cria uma categoria padrão global válida, Então ela aparece na lista, é propagada (CA4) e uma linha é registrada via `ImedtoAdminAuditWriter` com payload sem PII (nome + nº de instâncias propagadas).
- **CA9** (estados): Dado o catálogo global vazio (hipótese de teste), Quando a tela carrega, Então mostra `AppEmptyState`; durante a carga mostra loading; em erro mostra mensagem genérica.

### M4 — Frontend seletor + inline no modal

- **CA10** (caminho feliz — seletor): Dado o modal "Novo lançamento" aberto com Tipo=Despesa, Quando o usuário abre "Categoria", Então o seletor lista apenas categorias **ativas** do estabelecimento do tipo **Despesa** (incluindo as padrão e as próprias), e a seleção é obrigatória para criar.
- **CA11** (filtro por tipo dinâmico): Dado uma categoria de Despesa selecionada, Quando o usuário troca Tipo para Receita, Então a lista do seletor passa a mostrar só Receitas e a seleção anterior (Despesa) é limpa.
- **CA12** (lazy load — performance): Dado a aba Visão Geral carregada, Quando o usuário **não** abriu o modal, Então **nenhuma** request a `/financeiro/categorias` é disparada; a busca ocorre **apenas** ao abrir o modal.
- **CA13** (adicionar inline): Dado o modal aberto, Quando o usuário usa "+ Adicionar nova categoria" e informa um nome novo, Então um `POST /financeiro/categorias { nome, tipo }` é feito, a categoria criada (`Padrao=false`) aparece na lista e já fica **selecionada**, e o lançamento pode ser criado em seguida.
- **CA14** (inline colisão — R7.1): Dado já existe a categoria "Aluguel" no estabelecimento, Quando o usuário tenta adicioná-la inline novamente, Então o back retorna `422` com mensagem genérica, o front a exibe inline e **não** cria duplicata.
- **CA15** (multi-tenant): Dado uma categoria customizada criada inline no estabelecimento A, Quando um usuário do estabelecimento B abre o seletor, Então a categoria de A **não** aparece; mesmo nome pode existir em B sem conflito.
- **CA16** (edição de lançamento antigo fora da lista): Dado um lançamento antigo com `Categoria` em texto livre que não está no pool ativo, Quando ele é aberto no seletor, Então o valor atual aparece como opção selecionada (transitória) e salvar sem alterar mantém o texto original. (Aplicar só se existir edição de lançamento; caso contrário, registrar como N/A com evidência de que não há fluxo de edição.)

### M5 — Inativar categoria padrão no estabelecimento

- **CA17** (inativar padrão — R8): Dado uma categoria `Padrao=true` ativa no estabelecimento, Quando o usuário com permissão `financeiro` a inativa, Então `Ativo=false`, ela **some do seletor** do modal, e a operação é aceita (sem `BusinessException`).
- **CA18** (rename/excluir padrão segue bloqueado): Dado uma categoria `Padrao=true`, Quando o usuário tenta **renomear** (`PUT`) ou **excluir** (`DELETE`), Então o back recusa com `422` ("Categoria padrão não pode ser editada.") e o front mantém esses botões ocultos/desabilitados.
- **CA19** (reativar padrão): Dado uma padrão que o estabelecimento inativou, Quando o usuário a reativa, Então `Ativo=true` e volta a aparecer no seletor.

### Cross-cutting obrigatórios

- **CA20** (RBAC estabelecimento): Dado um usuário **sem** permissão `financeiro`, Quando chama qualquer endpoint de `/financeiro/categorias` (listar/criar/inativar), Então recebe `403` e os controles ficam ocultos no front.
- **CA21** (LGPD/mensagens): Dado qualquer erro de validação (colisão, tipo inválido), Quando o back retorna `422`, Então a mensagem é genérica e não contém PII; nenhum log registra dado de paciente (categorias não são dado de paciente).
- **CA22** (regressão seed forma de pagamento): Dado a troca do seed de categorias para ler do global, Quando um estabelecimento é criado, Então o seed de **formas de pagamento** (`SeedsFinanceiro.FormasPagamento`) continua funcionando inalterado.

---

## 8. Riscos e dependências

- **Índice único por `(estabelecimento_id, nome)` sem tipo**: registrado em R7.1/§5. A lista aprovada não colide entre tipos, então o seed é seguro. Mas, se um estabelecimento já tiver uma categoria customizada homônima a uma padrão nova sendo propagada, o `INSERT` da cópia colidiria → a propagação (R3) deve **pular** estabelecimentos que já têm o nome (mesmo que customizado), não falhar. Espelhar a checagem `ExisteNomeEmQualquerEstabelecimento`/idempotência de `ModelosPermissaoPadraoSistema`.
- **Mudança de comportamento de `CategoriaFinanceira.Inativar()`** (R8) é regressiva: há testes existentes (`InativarCategoriaFinanceiraCommandHandlerTests`, `AtualizarCategoriaFinanceiraCommandHandlerTests`) que **esperam** o bloqueio de inativação para padrão. Atualizar esses testes para o novo comportamento (inativar permitido; editar/excluir ainda bloqueado) e adicionar testes de regressão.
- **Seed antigo "Receita: Consulta" etc. nos estabelecimentos já existentes**: não serão renomeados automaticamente (decisão de não migrar dados). Estabelecimentos antigos continuam com os nomes prefixados antigos até o admin/estabelecimento ajustar. Aceitável (decisão 4). Se o usuário quiser higienizar a base antiga, é outro briefing.
- **Dependência de ordem dos marcos**: M2 depende de M1 (tabela+seed global); M3 e M4/M5 dependem de M1/M2. Executar em ordem; M4 e M5 podem ir juntos (mesma área de front).
- **Áreas regressivas a vigiar**: criação de estabelecimento (seed), aba Visão Geral do Financeiro (lazy load + modal), tela de categorias do estabelecimento, módulo admin Catálogos.

---

## 9. Observações para execução

- **Reuso obrigatório (não-negociável)**:
  - Backend padrão global: **espelhar `ModelosPermissaoPadraoSistema`** (`Application/Admin/ModelosPermissaoPadraoSistema/*`, `ImedtoAdminAuditWriter`, propagação idempotente com `ExisteNome...`). Não inventar padrão novo.
  - Estabelecimento: **reusar** `CategoriaFinanceira` (entidade), `CriarCategoriaFinanceiraCommandHandler`, `InativarCategoriaFinanceiraCommandHandler`, `ListarCategoriasFinanceirasQuery` (já filtra `Tipo`/`Ativas`/`Padrao`), `categoriaFinanceiraService.ts`. Estender, não duplicar.
  - Front admin: **espelhar** `PermissoesGlobaisListView`/`VariaveisGlobaisListView` + `catalogosService.ts` + rotas `catalogos/*`.
- **Liberdade técnica (justificar no PR)**: nome exato da tabela global (sugestão `categorias_financeiras_padrao_sistema`), nome do componente de seletor-criável caso seja necessário criá-lo, e a decisão de reusar `AppSelect` vs novo componente DS.
- **`imedto-database`**: acionar para a migration de M1 (tabela global + índice único + seed idempotente `ON CONFLICT DO NOTHING`). Inspecionar via MCP/psql se já existe a tabela antes de criar. **Não** há DDL na tabela `categorias_financeiras` nem em `lancamentos`.
- **Espelhamento back+front**: a proibição de editar/excluir padrão tem espelho (Domain `BusinessException` + UI oculta); a permissão de inativar padrão idem (Domain libera + UI habilita). O seletor é guard-rail de UX — o back **não** valida que a string de `Categoria` seja de uma categoria existente (continua string livre; isto é intencional para não quebrar lançamentos antigos).
- **Tipografia**: usar tokens (CLAUDE.md §5) em qualquer CSS novo.

---

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — na seção do módulo Financeiro / catálogos globais do admin: documentar a nova fonte de verdade global `categorias_financeiras_padrao_sistema` e o padrão de propagação por cópia (reaproveitando a descrição já usada para `ModelosPermissaoPadraoSistema`). Registrar que o seed por estabelecimento passa a ler do global em vez de hardcode.
- **`Docs/DESIGN.md`** — **somente se** for criado um componente reutilizável de "seletor com adicionar inline" (`AppSelectCriavel`/`AppComboboxCriavel`): adicioná-lo à seção de componentes do design system com props e exemplo. Se a feature reusar um componente existente sem novidade, **não** atualizar.
- **`Docs/INFRA.md` / `Docs/COMANDOS.md`** — sem alteração (sem recurso AWS novo, sem comando novo; a migration segue o fluxo já documentado).
- **`Docs/LGPD.md`** — sem alteração (catálogo de categorias não é PII nem dado de paciente; audit do admin já está coberto pelo padrão existente).
