# Modelos de descrição cirúrgica (templates de texto para a evolução)

**ID**: 2026-06-13_002
**Status**: Aprovado por usuário em 2026-06-13
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (seção desc-cirurgica da evolução), Configurações (grupo "Modelos e listas")

## 1. Contexto e motivação

Na tela de evolução do prontuário, a seção **"Descrição cirúrgica"** (`desc-cirurgica`, tipo `texto_longo`) é um campo de texto livre onde o profissional relata o ato cirúrgico — técnica, tempos, intercorrências. Hoje esse relato é digitado do zero a cada cirurgia, mesmo quando o procedimento é padronizado e o texto se repete quase integralmente entre pacientes (ex.: "rinoplastia estruturada", "colecistectomia videolaparoscópica").

A demanda é dar ao profissional um **catálogo de modelos de descrição cirúrgica** reutilizáveis: ele cadastra uma vez o texto-base de cada cirurgia que faz, e na hora da evolução escolhe o modelo pelo título e o corpo popula o textarea — restando só ajustar os detalhes do caso. Reduz tempo de documentação e padroniza o registro clínico.

A feature segue o **mesmo padrão de produto já consolidado** em duas frentes do sistema:
- **Estrutura de dados e escopo**: "Listas de variáveis" (`ProntuarioVariavelPool`) — escopo duplo padrão-sistema + estabelecimento, multi-tenant, `eh_padrao_sistema`, `ativo`, gerência em Configurações → "Modelos e listas".
- **Experiência de consumo**: "Modelos de atestado" (`AtestadoTab`) — escolher um modelo de texto que popula um textarea, com atalho para gerenciar modelos.

## 2. Persona-alvo

- **Profissional** (cirurgião) durante o atendimento, ao preencher a evolução de uma consulta/procedimento que inclui a seção "Descrição cirúrgica". Uso recorrente: toda cirurgia documentada.
- **Dono / quem tem a permissão `modelos_prontuario`**: cadastra e mantém os modelos do estabelecimento em Configurações.
- **Padrão-sistema**: modelos pré-cadastrados pela Imedto (sem dono), visíveis a todos os estabelecimentos, não editáveis pelo tenant.

## 3. Escopo

**Inclui**:
- Nova entidade/tabela `modelos_descricao_cirurgica` com **título + corpo de texto**, multi-tenant (`estabelecimento_id` nullable), `eh_padrao_sistema`, `ativo`, audit columns — espelhando o padrão `ProntuarioVariavelPool`.
- CRUD backend (CQRS: EF na escrita, Dapper na leitura) com RBAC `modelos_prontuario` e multi-tenant falha-fechada.
- Nova seção de gerência em **Configurações → "Modelos e listas"** ("Modelos de descrição cirúrgica"), reusando o padrão da tab existente.
- No consumo: ação **"Usar template"** no topo do textarea da seção `desc-cirurgica` da evolução, abrindo um seletor com a lista de modelos (estabelecimento + padrão-sistema), aplicando o corpo escolhido ao campo.
- Confirmação de substituição quando o textarea já tem texto digitado.
- Atalho **"Cadastrar novo modelo"** a partir do seletor, levando à seção de gerência em Configurações.

**Não inclui**:
- Edição de modelos padrão-sistema pelo tenant (somente leitura/uso).
- Seletor de template em qualquer outra seção da evolução além de `desc-cirurgica`.
- Variáveis/placeholders dinâmicos dentro do corpo do modelo (ex.: `{nome_paciente}`). Corpo é texto puro nesta entrega.
- Versionamento/histórico de modelos.
- Compartilhamento de modelo entre estabelecimentos pelo próprio usuário (só padrão-sistema é cross-tenant, e é cadastrado pela Imedto).
- Aplicar template via API no backend: a aplicação ao textarea é client-side (o backend só serve a lista; a evolução é persistida pelo fluxo já existente).

## 4. Regras de negócio

- **R1 — Escopo duplo (padrão-sistema + estabelecimento)**: um modelo pertence a um estabelecimento (`estabelecimento_id` NOT NULL, `eh_padrao_sistema = false`) **ou** é padrão-sistema (`estabelecimento_id` NULL, `eh_padrao_sistema = true`). A listagem retorna os dois conjuntos juntos. Mora em: Domain (factories `CriarDoEstabelecimento` / `CriarPadraoSistema`) + Query (`WHERE eh_padrao_sistema = true OR estabelecimento_id = @EstabelecimentoId`). Validada em: back.

- **R2 — Multi-tenant falha-fechada**: toda escrita resolve o `estabelecimento_id` do tenant do contexto (`ICurrentTenantAccessor`) — **o cliente nunca envia o tenant**. O repositório de escrita só retorna o registro para edição/exclusão se `Id == id && EstabelecimentoId == estabelecimentoId` (método `ObterPorIdOuNulo`). Registro de outro tenant ou padrão-sistema (NULL) **não é retornado** → handler lança `BusinessException("Modelo não encontrado.")` (mensagem genérica, sem revelar tenant alheio). Mora em: Repositório (escrita) + Handler. Validada em: back.

- **R3 — Padrão-sistema é imutável pelo tenant**: tentativa de editar ou excluir um modelo `eh_padrao_sistema = true` → `BusinessException("Modelos padrão do sistema não podem ser alterados.")`. Como R2 já impede o `ObterPorIdOuNulo` de retornar registro com `estabelecimento_id` NULL, o tenant nunca alcança um padrão-sistema por esse caminho; a guarda explícita é defesa em profundidade. Mora em: Handler. Validada em: back (e no front, ações de editar/excluir ocultas em itens com badge "Padrão do sistema").

- **R4 — RBAC de escrita (`modelos_prontuario`)**: criar/editar/excluir modelo exige a permissão extra `modelos_prontuario`. **Dono sempre passa**; demais papéis só se tiverem a permissão. A **leitura/listagem** (consumo na evolução e exibição na gerência) é aberta a qualquer membro do tenant — não exige a permissão extra (o cirurgião que consome o template pode não ter permissão de gerenciar). Mora em: Controller (`[RequiresPermissaoExtra(PermissoesExtras.ModelosProntuario)]` nos endpoints de escrita) + Front (gate de edição via `permissoes.podeExtra("modelos_prontuario")`). Validada em: back + front.

- **R5 — Título e corpo obrigatórios**: `titulo` e `corpo` não podem ser vazios/só-espaço. `titulo` ≤ 200 caracteres; `corpo` texto longo (sem limite rígido de tamanho de coluna — `text`). Mora em: Domain (`Validar`) + Front (validação de form antes de submeter). Validada em: back + front.

- **R6 — Dedup de título por escopo (acento-insensível)**: dentro do mesmo escopo de listagem (modelos do tenant + padrão-sistema visíveis a ele), não pode haver dois modelos **ativos** com o mesmo título normalizado (trim + lower + remoção de acentos). Reusa o helper `NormalizadorPool.Normalizar` (não há extensão `unaccent` no Postgres — dedup é na aplicação, não no banco). Conflito → `BusinessException("Já existe um modelo com este título.")`. Mora em: Repositório (escrita) + Handler. Validada em: back.

- **R7 — Inativar em vez de apagar (soft-state via `ativo`)**: seguindo o padrão do pool, a entidade carrega `ativo`. A exclusão pelo tenant remove o registro do estabelecimento (`Excluir`), como no pool atual. `ativo` existe para permitir, no futuro, ocultar sem apagar; a listagem de consumo traz **apenas ativos** (`apenasAtivos = true` por padrão). Mora em: Domain (`Inativar`/`Reativar`) + Query (filtro `ativo`). Validada em: back. (Esta entrega expõe na UI apenas criar/editar/excluir, como o pool; `ativo` fica modelado e default `true`.)

- **R8 — Aplicar template substitui o conteúdo (com confirmação condicional)**: ao escolher um modelo no seletor da seção `desc-cirurgica`, o corpo do modelo **substitui** o valor atual do textarea. Se o textarea **já tiver texto** (`novaEvolucao["desc-cirurgica"]` não-vazio após trim), exibir `AppConfirmDialog` antes de substituir; se estiver vazio, aplica direto sem confirmar. Mora em: Front (lógica de consumo). Validada em: front. (Sem regra de backend — aplicação é client-side.)

- **R9 — Seletor restrito à seção desc-cirurgica**: a ação "Usar template" só aparece para a seção cuja `chave === "desc-cirurgica"`, e somente quando o modelo de prontuário selecionado inclui essa seção na estrutura. Mora em: Front. Validada em: front.

## 5. Modelo de dados

**Nova tabela: `public.modelos_descricao_cirurgica`** (espelha `prontuario_variaveis_pool`, trocando `tipo`/`nome` por `titulo`/`corpo`):

| Coluna | Tipo | Nulo | Observação |
|---|---|---|---|
| `id` | `bigint GENERATED BY DEFAULT AS IDENTITY` | NOT NULL | PK |
| `estabelecimento_id` | `bigint` | **NULL** | NULL = padrão-sistema; NOT NULL = do tenant. Sem FK (coluna solta, igual ao pool) |
| `titulo` | `varchar(200)` | NOT NULL | escolha do médico no seletor |
| `corpo` | `text` | NOT NULL | popula o textarea da evolução |
| `ativo` | `boolean` | NOT NULL | default `true` |
| `eh_padrao_sistema` | `boolean` | NOT NULL | default `false` |
| `criado_em` | `timestamptz` | NOT NULL | audit |
| `atualizado_em` | `timestamptz` | NULL | audit |

**Índices**:
- `ix_modelo_desc_cirurgica_estabelecimento ON (estabelecimento_id)` — listagem por tenant.
- `ix_modelo_desc_cirurgica_padrao ON (eh_padrao_sistema)` — recorte padrão-sistema.

(Sem `tipo` — diferente do pool, esta tabela é monotemática. Sem unique constraint de título: a dedup é feita na aplicação com normalização sem acento, espelhando o pool.)

**Multi-tenant**: coluna `estabelecimento_id`; query filtra `eh_padrao_sistema = true OR estabelecimento_id = @EstabelecimentoId`; repositório de escrita falha-fechada (`Id == id && EstabelecimentoId == estabelecimentoId`).

**Audit**: `criado_em`/`atualizado_em`. **Não há `criado_por_usuario_id`** — espelha o pool, que não rastreia autor (decisão de paridade com o padrão existente; não é dado de paciente).

**LGPD**: a tabela **não armazena PII de paciente** — é catálogo de texto técnico-cirúrgico genérico, não vinculado a paciente. O corpo aplicado vira parte da evolução do paciente apenas quando o profissional salva a evolução (fluxo já existente, já auditado). DTO de leitura **não expõe `estabelecimento_id`** (a UI distingue padrão-sistema só via `eh_padrao_sistema`), seguindo a minimização do `VariavelPoolDto`.

## 6. UX e fluxo

### A) Gerência — Configurações → "Modelos e listas" → "Modelos de descrição cirúrgica"

Novo item no grupo de navegação **"Modelos e listas"** de `EstabelecimentoView.vue` (hoje: Modelos de prontuário, Termos, Listas de variáveis). Adicionar:
`{ id: "modelos-cirurgia", label: "Modelos de descrição cirúrgica", icone: "fa-solid fa-file-pen", visivel: true }`.

Atualizar: union `SecaoId`, `TODAS_SECOES`, `secaoPermitida()`, item de nav, e bloco `<section v-else-if="secaoAtiva === 'modelos-cirurgia'">` com `<h2 class="ds-section-title">Modelos de descrição cirúrgica</h2>` + novo componente `<ModelosDescricaoCirurgicaTab :pode-editar="podeEditarModelos" />`.

**Gate de edição (corrige a inconsistência do pool)**: usar `podeEditarModelos = permissoes.podeExtra("modelos_prontuario")` (alinhado ao backend — Dono ou quem tem a permissão), **não** Dono-only. A `ListasVariaveisTab` atual gateia por Dono-only no front, mais restrita que o back; aqui alinhamos ao backend conforme decisão de produto.

**Componente `ModelosDescricaoCirurgicaTab.vue`** (espelha `ListasVariaveisTab` + reaproveita o padrão de form de `AtestadoTab` por ter corpo longo):
- Prop `podeEditar: boolean`.
- DS: `AppButton`, `AppCard`, `AppField`, `AppInput` (título), `AppTextarea` (corpo), `AppConfirmDialog` (excluir), `AppToast`, `AppEmptyState`.
- Lista de modelos do tenant + padrão-sistema; itens padrão-sistema com badge "Padrão do sistema" e **sem** ações editar/excluir.
- Criar/editar via form (título + corpo). Excluir via `AppConfirmDialog`.
- Estados:
  - **loading**: "Carregando...".
  - **vazio**: `AppEmptyState` "Nenhum modelo cadastrado ainda." com slot de ação "Novo modelo" (se `podeEditar`).
  - **erro**: `AppToast`/mensagem de erro genérica.
  - **sucesso**: toast de confirmação.
  - **sem permissão** (`!podeEditar`): lista visível em leitura, ações de escrita ocultas + aviso.

### B) Consumo — seção "Descrição cirúrgica" na evolução

Em `ConsultaAtualTab.vue`, no card da seção cuja `chave === "desc-cirurgica"`, adicionar acima do `SecaoProntuario` (ou no `module-head`) um botão **"Usar template"** (`AppButton variant="ghost" icon="fa-solid fa-file-import"`), visível só para essa seção (R9).

Ao clicar, abrir um seletor (recomendado `AppDrawer` ou `AppPopover`) com:
- Lista dos modelos disponíveis (tenant + padrão-sistema), cada item mostrando o **título** e uma prévia curta do corpo; badge "Padrão do sistema" quando aplicável.
- Estado **vazio**: `AppEmptyState` "Nenhum modelo cadastrado." + botão "Cadastrar novo modelo".
- Estado **loading** ao carregar a lista; estado **erro** com mensagem genérica.
- Rodapé/cabeçalho com atalho **"Cadastrar novo modelo"** → navega para `Configurações → Modelos de descrição cirúrgica` (deep-link `?secao=modelos-cirurgia`, padrão de navegação já existente via `router.push`).

Ao escolher um modelo:
- Se `novaEvolucao["desc-cirurgica"]` estiver vazio → aplica o corpo direto (substitui), fecha o seletor, toast opcional.
- Se já houver texto → `AppConfirmDialog` "Substituir o conteúdo atual da descrição cirúrgica?" (variante `primary`, confirmar "Substituir" / cancelar "Manter"). Confirmar → substitui. Cancelar → mantém o texto e fecha o diálogo.

**Performance/foco**: a lista de modelos só é buscada **quando o seletor é aberto** (clique em "Usar template" ou na entrada da tab de gerência) — não no carregamento da evolução. Reusa `apenasAtivos = true`.

**Aplicação do valor**: seguir o padrão de eventos do `SecaoProntuario`/`ConsultaAtualTab` — a substituição seta `novaEvolucao["desc-cirurgica"]`. Caminho limpo: emitir evento para `ProntuarioView` setar o valor no `reactive` dono (espelha o `update:modeloId` já existente), evitando mutação implícita via prop.

## 7. Critérios de aceite (testáveis)

- **CA1 (criar — caminho feliz)**: Dado um Dono em Configurações → "Modelos de descrição cirúrgica", Quando preenche título "Rinoplastia estruturada" e um corpo e salva, Então o modelo aparece na lista do estabelecimento e fica disponível no seletor da seção `desc-cirurgica` da evolução.

- **CA2 (consumo — aplicar em campo vazio)**: Dado um profissional na evolução com a seção "Descrição cirúrgica" vazia, Quando abre "Usar template" e escolhe um modelo, Então o corpo do modelo é inserido no textarea **sem** diálogo de confirmação.

- **CA3 (consumo — substituir com confirmação)**: Dado um profissional com texto já digitado na "Descrição cirúrgica", Quando escolhe um modelo no seletor, Então aparece `AppConfirmDialog` de substituição; ao confirmar, o corpo do modelo substitui o texto; ao cancelar, o texto digitado permanece intacto.

- **CA4 (escopo padrão-sistema + estabelecimento)**: Dado que existem modelos padrão-sistema (`estabelecimento_id` NULL) e modelos do estabelecimento, Quando o profissional abre o seletor ou a tab de gerência, Então ambos os conjuntos aparecem na mesma lista, com os padrão-sistema marcados com badge "Padrão do sistema".

- **CA5 (multi-tenant — listagem)**: Dado um usuário do estabelecimento B, Quando lista os modelos, Então vê apenas os modelos do tenant B + os padrão-sistema, e **nunca** modelos do estabelecimento A.

- **CA6 (multi-tenant — escrita falha-fechada)**: Dado um usuário do estabelecimento B tentando editar (PUT) ou excluir (DELETE) um modelo cujo `id` pertence ao estabelecimento A, Quando chama o endpoint, Então recebe erro genérico "Modelo não encontrado." (não revela existência do registro alheio) e nada do tenant A é alterado.

- **CA7 (RBAC — escrita bloqueada)**: Dado um usuário com papel sem a permissão `modelos_prontuario` (e que não é Dono), Quando chama POST/PUT/DELETE de modelo, Então recebe 403 (mensagem genérica) e, no front, os botões de criar/editar/excluir ficam ocultos — mas a **lista permanece visível** e o seletor de consumo funciona.

- **CA8 (RBAC — Dono sempre passa)**: Dado o Dono do estabelecimento (sem a permissão extra explicitamente marcada), Quando cria/edita/exclui um modelo, Então a operação é autorizada.

- **CA9 (padrão-sistema imutável)**: Dado um modelo `eh_padrao_sistema = true`, Quando um usuário do tenant tenta editá-lo ou excluí-lo via API, Então recebe `BusinessException` ("não encontrado" ou "não podem ser alterados") e o registro permanece inalterado; no front, esse item não exibe ações de editar/excluir.

- **CA10 (validação — título/corpo obrigatórios)**: Dado o form de criação, Quando o usuário tenta salvar com título vazio ou corpo vazio, Então o back retorna 422 com mensagem genérica e o front bloqueia o submit exibindo o erro no campo.

- **CA11 (dedup de título)**: Dado que já existe um modelo ativo com título "Colecistectomia", Quando o usuário tenta criar outro com título "colecistectomia" (variação de caixa/acento), Então o back retorna 422 "Já existe um modelo com este título." e o modelo não é criado.

- **CA12 (LGPD — sem PII e DTO mínimo)**: Dado a listagem de modelos, Quando o DTO é serializado, Então **não** contém `estabelecimento_id` nem qualquer PII de paciente, e mensagens de erro não revelam tenant alheio nem dado pessoal.

- **CA13 (estado vazio)**: Dado um estabelecimento sem modelos cadastrados e sem padrão-sistema aplicável, Quando o usuário abre o seletor na evolução ou a tab de gerência, Então vê `AppEmptyState` com texto específico e (se tiver permissão) um atalho "Cadastrar novo modelo".

- **CA14 (estado loading/erro)**: Dado o seletor sendo aberto, Quando a lista está carregando, Então mostra estado de loading; Quando a request falha, Então mostra mensagem de erro genérica sem quebrar a tela.

- **CA15 (performance/foco)**: Dado a tela de evolução carregando, Quando o profissional ainda não clicou em "Usar template", Então **nenhuma** request de modelos é disparada; a lista só é buscada ao abrir o seletor.

- **CA16 (restrição de seção)**: Dado um modelo de prontuário que **não** inclui a seção `desc-cirurgica`, Quando o profissional preenche a evolução, Então a ação "Usar template" **não** aparece em nenhuma seção; Dado um modelo que inclui `desc-cirurgica`, Então a ação aparece **apenas** nessa seção.

- **CA17 (atalho cadastrar novo)**: Dado o seletor de templates aberto, Quando o usuário clica em "Cadastrar novo modelo", Então é levado a Configurações → "Modelos de descrição cirúrgica" (deep-link `?secao=modelos-cirurgia`).

## 8. Riscos e dependências

- **Inconsistência de gate herdada do pool**: a `ListasVariaveisTab` gateia front por Dono-only, mas o back autoriza via `modelos_prontuario`. Esta entrega **alinha o novo componente ao backend** (`podeExtra`). Não altera a tab de variáveis existente (fora de escopo) — apenas não repete o erro.
- **Aplicação client-side do template**: a substituição mexe em `novaEvolucao["desc-cirurgica"]`. Risco de quebrar o binding se mutado pela via errada — seguir o padrão de evento (`update:`) do `SecaoProntuario`/`ConsultaAtualTab`, não mutar prop diretamente.
- **Migration idempotente**: o deploy aplica DDL via EF; gotcha conhecido do projeto exige migration EF idempotente (`IF NOT EXISTS` em raw SQL) porque a validação local do QA cria schema no mesmo Postgres sem gravar `__ef_migrations_history`. O DB agent deve gerar a migration idempotente.
- **Dedup sem `unaccent`**: depende do helper `NormalizadorPool.Normalizar` (Application/Domain). Reusar, não recriar.
- **Dependência de catálogo**: `desc-cirurgica` só existe no `novaEvolucao` se o modelo de prontuário selecionado a incluir — a ação é condicional (R9/CA16).

## 9. Observações para execução

**Não-negociável**:
- Espelhar fielmente o padrão `ProntuarioVariavelPool` (Domain factories duplo-escopo, repo escrita falha-fechada, query Dapper com `eh_padrao_sistema = true OR estabelecimento_id = @EstabelecimentoId`, DTO sem `estabelecimento_id`). Trocar `tipo`/`nome` por `titulo`/`corpo`.
- RBAC nos endpoints de **escrita** via `[RequiresPermissaoExtra(PermissoesExtras.ModelosProntuario)]`. Leitura **sem** RBAC extra.
- `estabelecimento_id` injetado pelo controller via `ICurrentTenantAccessor` — cliente nunca envia tenant.
- Reuso obrigatório do DS: `AppDrawer`/`AppPopover`, `AppConfirmDialog`, `AppEmptyState`, `AppButton`, `AppTextarea`, `AppInput`, `AppField`, `AppToast`. Nada de HTML/CSS novo onde houver componente.
- Migration idempotente (DB agent).
- Substituição no consumo via padrão de evento, não mutação de prop.

**Liberdade técnica**:
- Reusar o `ProntuarioTemplateController` (adicionando endpoints `modelos-cirurgia`) **ou** criar controller dedicado — decisão do dev, desde que mantenha os atributos de classe (`[Authorize] [RequiresEstabelecimento] [RequiresAssinaturaAtiva]`) e o padrão de rota.
- Seletor de consumo como `AppDrawer` ou `AppPopover` — escolher o que melhor couber no card da seção; `AtestadoTab` usa drawer e é boa referência.
- Nomes de classes/comandos/queries a critério do dev, seguindo a convenção do namespace `Prontuarios`.

**Referências de arquivo (espelhar)**:
- Domain: `backend/src/Services/Imedto.Backend.Domain/Prontuarios/ProntuarioVariavelPool.cs` + `NormalizadorPool.cs` + `IProntuarioVariavelPoolRepository.cs`.
- Handlers: `backend/src/Services/Imedto.Backend.Application/Prontuarios/Commands/*VariavelPool*` + Queries `ListarVariaveisPoolQueryHandlers.cs`.
- Query repo Dapper: `backend/src/Services/Imedto.Backend.Infrastructure/Database/Repositories/VariavelPoolQueryRepository.cs`.
- Repo escrita EF: `.../Repositories/ProntuarioVariavelPoolRepository.cs` + `Configurations/ProntuarioVariavelPoolConfiguration.cs`.
- Controller: `backend/src/Services/Imedto.Backend.API/Controllers/ProntuarioTemplateController.cs`.
- RBAC: `Domain/ModelosPermissao/PermissoesExtras.cs` (`ModelosProntuario`) + `API/Filters/RequiresPermissaoExtraAttribute.cs`.
- DI: `Infrastructure/Container.cs:84`, `API/Container.cs:533-540`, `API/Container.cs:1068+` (comandos) e `:1264` (query).
- Front gerência: `frontend/src/services/variavelPoolService.ts` + `components/estabelecimento/ListasVariaveisTab.vue` + `views/estabelecimento/EstabelecimentoView.vue` (grupo "Modelos e listas").
- Front consumo: `frontend/src/components/prontuario/tabs/ConsultaAtualTab.vue` (card da seção), `components/prontuario/SecaoProntuario.vue` (binding), `views/pacientes/ProntuarioView.vue` (estado `novaEvolucao`), e `components/prontuario/tabs/AtestadoTab.vue` (referência de UX: modelo→textarea + gerência inline).

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — na seção do design system / padrões de Configurações, registrar o padrão **"Modelos e listas" (escopo padrão-sistema + estabelecimento)** como reutilizável, agora com mais um membro ("Modelos de descrição cirúrgica") e a variação **título + corpo de texto** (vs. só nome do pool). Documentar o padrão de **"Usar template" inline numa seção de evolução** (botão contextual + seletor + `AppConfirmDialog` de substituição) como padrão de consumo de modelo de texto.
- **`Docs/ARQUITETURA.md`** — registrar a nova tabela/aggregate `ModeloDescricaoCirurgica` no bounded context Prontuários, reforçando o padrão duplo-escopo multi-tenant (padrão-sistema NULL + tenant) já usado por `ProntuarioVariavelPool` — entrada incremental, não reescrita.
- **Nenhuma mudança em** `Docs/INFRA.md`, `Docs/COMANDOS.md`, `Docs/LGPD.md` (segue padrões de multi-tenant/minimização já documentados; sem novo tipo de PII, sem novo recurso de infra, sem novo comando).
