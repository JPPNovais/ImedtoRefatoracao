# Aba "Valores profissional" na Configuração de Orçamento

**ID**: 2026-06-04_008
**Status**: Aprovado por usuário em 2026-06-04
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: orçamento (formulário e configuração)

## 1. Contexto e motivação

O formulário de orçamento (`OrcamentoFormView.vue`) tem, em cada profissional da equipe, um select **"Tabela de valor *"** que é populado por `orcamentoCatalogoService.listarValoresProfissional()` (rota `GET /api/orcamentos/configuracoes/valores-profissional`). Hoje esse select **vem sempre vazio na prática**, porque não existe nenhuma tela onde o usuário cadastre esses "valores profissional" — o backend está pronto (controller, commands, handlers, repositório, query, tabela e DTOs existem e estão registrados em `Container.cs`), mas a UI de cadastro nunca foi construída no refactor.

Resultado: ao montar um orçamento, o usuário não consegue escolher a tabela de honorário por tempo do profissional, travando o cálculo de honorário por tempo (`OrcamentoCalculadora.CalcularValorProfissional`). Esta demanda fecha a lacuna criando a aba de cadastro que faltava — fiel ao legado, que tinha os 6 campos editáveis.

**Backend já está pronto** — nada de schema, controller, command, handler ou migration nesta entrega. Só frontend, espelhando o padrão da aba "Equipe" (`EquipeTab.vue`). Não acionar `imedto-database`.

## 2. Persona-alvo

Dono e Recepcionista (perfis com permissão `orcamento.configurar`), no momento de **configurar o estabelecimento** antes/durante a operação de orçamentos. Frequência: baixa (cadastro inicial + ajustes pontuais de tabela de honorário). É pré-requisito operacional para quem monta orçamentos cirúrgicos com honorário por tempo.

## 3. Escopo

**Inclui**:
- Nova aba **"Valores profissional"** em `OrcamentoSettingsView.vue`, posicionada **entre "Equipe" e "Anestesistas"**.
- Novo componente `frontend/src/components/orcamento/config/ValoresProfissionalTab.vue`, espelhando o padrão de `EquipeTab.vue` (lista + drawer de criar/editar + confirmação de excluir + toast + badge de contagem via `@contagem`).
- Listar, criar, editar e excluir registros de valor profissional, consumindo os métodos **já existentes** em `orcamentoCatalogoService`: `listarValoresProfissional()`, `criarValorProfissional()`, `atualizarValorProfissional()`, `removerValorProfissional()`.
- Formulário completo com os **6 campos editáveis** (fiel ao legado): `funcao`, `tempoBaseMinutos`, `valorTempoBase`, `tempoAdicionalMinutos`, `valorAdicional`, `valorPlus`.
- Seletor de profissional via dropdown alimentado por `vinculoService.listarProfissionaisPublico()` (LGPD-safe, sem PII) + opção **"Padrão (sem profissional específico)"** que envia `profissionalUsuarioId = null`.

**Não inclui**:
- Qualquer mudança de backend, schema, migration, controller ou DTO (tudo já existe).
- Gestão de status (inativar/reativar/filtro Todos/Ativos/Inativos). Só lista, criar, editar e excluir. Ver R7.
- Filtro por tipo, cálculo de honorário, ou alteração do `OrcamentoFormView` (o select "Tabela de valor" passa a popular naturalmente assim que houver registros — nenhuma linha de código no form muda).
- Importar/exportar planilha de valores.
- Atualização de `Docs/` (reuso puro de padrão já documentado).

## 4. Regras de negócio

- **R1 — Multi-tenant**: todas as operações (listar/criar/editar/excluir) carregam `EstabelecimentoId` no backend, derivado do tenant claim (`_tenant.EstabelecimentoId`). O front **não envia** `estabelecimentoId` — ele já é injetado server-side. Mora em: Controller/Handler (back). Validada em: back (fonte da verdade) + front (service usa só o tenant ativo implícito via httpClient).
- **R2 — RBAC**: a aba e suas ações de escrita são exclusivas de **Dono ou Recepcionista** com permissão `orcamento.configurar`. Backend já garante: classe `OrcamentoCatalogoController` tem `[RequiresAcao("orcamento","configurar")]` + `[FeatureGate(OrcamentoCompleto)]`, e POST/PUT/DELETE de `valores-profissional` têm `[RequiresPapel(Dono, Recepcionista)]`. Espelho no front: a aba e os botões de criar/editar/excluir seguem o mesmo gate visual usado pelas outras abas de `OrcamentoSettingsView` (mesma view já gateada na rota). Mora em: Controller (back) + gate visual (front). Validada em: back (403/422) + front (UX).
- **R3 — Seleção de profissional sem PII**: o dropdown de profissional é alimentado **exclusivamente** por `vinculoService.listarProfissionaisPublico()` (retorna `usuarioId`, `nomeCompleto`, `especialidade`, `conselho`, `status` — sem e-mail/telefone). O valor enviado é o `usuarioId` (Guid) em `profissionalUsuarioId`; quando o usuário escolhe "Padrão (sem profissional específico)", envia-se `null`. O **nome exibido** na lista vem do backend (`profissionalNome`, via JOIN no DTO) — o front nunca envia nome de profissional. Mora em: Query/DTO (back) + componente (front). Validada em: back (JOIN no DTO) + front (binding do select).
- **R4 — Campos obrigatórios e validações**: ao salvar, são obrigatórios e validados no front antes de chamar o service:
  - `funcao`: texto não-vazio (após `trim()`).
  - `tempoBaseMinutos`, `tempoAdicionalMinutos`: inteiros `>= 0` (minutos).
  - `valorTempoBase`, `valorAdicional`, `valorPlus`: numéricos `>= 0`.
  Se inválido, bloqueia o submit e mostra toast de erro genérico (sem PII). O backend permanece a fonte da verdade (422 do `BusinessException` é repassado como mensagem). Mora em: Handler/Domain (back, já existente) + componente (front). Validada em: front (trava de UX) + back (422).
- **R5 — Criar**: `criarValorProfissional(payload)` com os 6 campos + `profissionalUsuarioId`. Em sucesso: fecha o drawer, recarrega a lista, atualiza o badge de contagem, toast de sucesso. Mora em: service+componente (front). Validada em: front.
- **R6 — Editar**: `atualizarValorProfissional(id, payload)`. O payload de atualização **não** inclui `profissionalUsuarioId` (a interface `AtualizarValorProfissionalPayload` só tem os 6 campos — fiel ao contrato existente). Em sucesso: fecha o drawer, recarrega, toast de sucesso. Mora em: service+componente (front). Validada em: front.
- **R7 — Excluir sem gestão de status**: o botão excluir chama `removerValorProfissional(id)` precedido de `AppConfirmDialog`. A UI **não expõe** filtro Todos/Ativos/Inativos, badge de status na linha, nem ação de reativar. Não importa se o backend faz hard-delete ou soft-delete via `ativo=false` — a UI trata como "removido" e não promete reativação. Em sucesso: recarrega, toast. Mora em: service+componente (front). Validada em: front.
- **R8 — Estado vazio é normal**: lista vazia é o estado inicial esperado (nunca houve cadastro). Mostra `AppEmptyState` com texto específico e CTA "Criar primeiro valor". Mora em: componente (front). Validada em: front.

## 5. Modelo de dados

**Nenhuma mudança de schema.** A tabela de valores profissional, o repositório (`ValorProfissionalOrcamentoRepository`), os commands (`CriarValorProfissionalCommand`/`AtualizarValorProfissionalCommand`/`RemoverValorProfissionalCommand`), os handlers e a query (`ListarValoresProfissionalQuery`) já existem e estão registrados em `Container.cs`. Multi-tenant via `estabelecimento_id` já é garantido pelos handlers/repositório existentes (`EstabelecimentoId` no command + filtro no repositório falha-fechada).

DTO de leitura: `ValorProfissionalOrcamentoDto` (back) / `ValorProfissionalOrcamentoCatalogo` (front) — já expõem `profissionalNome` minimizado (sem PII sensível). Audit trail: não aplicável (dado operacional/financeiro de configuração, não é dado de saúde/paciente).

## 6. UX e fluxo

Espelho fiel de `EquipeTab.vue`. Layout textual:

```
[ StatCards de contagem (ex.: "Cadastrados", "Padrão", "Por profissional") — opcional, seguir EquipeTab ]
[ AppSearchInput "Buscar por função ou profissional..." ]            [ AppButton "Novo valor" ]

┌─ tabela ───────────────────────────────────────────────────────────┐
│ Função | Profissional | Tempo base | Valor base | Adicional | Plus | (ações) │
│  ...   |  Dr. X / —   |  60 min    |  R$ ...     |  R$ ...    | R$.. | ✏ 🗑  │
└────────────────────────────────────────────────────────────────────┘
[ AppPagination ]
```

**Drawer de criar/editar** (`AppDrawer`, ~560px), todos os campos visíveis e obrigatórios:
- `AppField "Profissional"` → `AppSelect` com 1ª opção "Padrão (sem profissional específico)" (value vazio → `null`) + uma opção por profissional de `listarProfissionaisPublico()` (label = `nomeCompleto`, value = `usuarioId`).
- `AppField "Função" required` → `AppInput` (ex.: "Cirurgião principal").
- Grid 2 colunas: `AppField "Tempo base (min)"` → `AppInput type=number`; `AppField "Valor base (R$)"` → `AppInput type=number step=0.01`.
- Grid 2 colunas: `AppField "Tempo adicional (min)"` → number; `AppField "Valor adicional (R$)"` → number step 0.01.
- `AppField "Valor plus (R$)"` → number step 0.01.
- Rodapé: `AppButton secondary "Cancelar"` + `AppButton "Salvar"`.

**Componentes do design system reutilizados** (mesmos de `EquipeTab.vue`): `AppStatCard`, `AppSearchInput`, `AppDrawer`, `AppField`, `AppInput`, `AppSelect`, `AppButton`, `AppPagination`, `AppEmptyState`, `AppToast`, `AppConfirmDialog`. **Nenhum componente novo no design system.**

**Estados**:
- **Loading**: "Carregando…" enquanto a lista carrega (padrão `EquipeTab`).
- **Vazio**: `AppEmptyState` ícone `fa-solid fa-user-clock` (ou similar), título "Nenhum valor profissional cadastrado", descrição explicando que esses valores alimentam o select "Tabela de valor" do orçamento, CTA "Criar primeiro valor".
- **Sem resultado de busca**: `AppEmptyState` "Nenhum resultado".
- **Erro ao salvar/excluir**: toast de erro com mensagem genérica (`e?.response?.data?.mensagem ?? "Falha ao salvar."`), sem PII.
- **Sucesso**: toast de sucesso + recarrega + atualiza badge.

Mobile-ready: herda o responsivo de `EquipeTab` (tabela em `.table-wrap`, toolbar com `flex-wrap`).

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — criar)**: Dado um Dono na aba "Valores profissional" com a lista vazia, Quando clica em "Novo valor", preenche função "Cirurgião principal", tempo base 60, valor base 1500, tempo adicional 30, valor adicional 500, valor plus 200, escolhe um profissional do dropdown e salva, Então o registro aparece na lista, o drawer fecha, o badge da aba incrementa para (1) e um toast de sucesso é exibido.
- **CA2 (caminho feliz — padrão sem profissional)**: Dado o drawer de novo valor aberto, Quando o usuário mantém "Padrão (sem profissional específico)" no dropdown e salva, Então o payload enviado tem `profissionalUsuarioId = null` e a linha na lista exibe "—" na coluna Profissional.
- **CA3 (editar)**: Dado um valor existente, Quando o usuário clica em editar, altera `valorTempoBase` para 1800 e salva, Então a lista reflete o novo valor e o toast de sucesso aparece; o payload de atualização contém os 6 campos e **não** contém `profissionalUsuarioId`.
- **CA4 (excluir)**: Dado um valor existente, Quando o usuário clica em excluir e confirma no `AppConfirmDialog`, Então o registro some da lista, o badge decrementa e um toast aparece; a UI não oferece nenhuma forma de reativar.
- **CA5 (multi-tenant)**: Dado um usuário do estabelecimento B, Quando o front lista/cria/edita/exclui valores, Então todas as requisições resolvem `estabelecimento_id` no backend a partir do tenant ativo (o front nunca envia `estabelecimentoId`), e registros do estabelecimento A nunca aparecem nem podem ser alterados (404/422 genérico do backend, sem vazar dado alheio).
- **CA6 (RBAC — gate de papel, espelho front+back)**: Dado um usuário Profissional (sem `orcamento.configurar`), Quando tenta acessar a configuração de orçamento, Então a view já é bloqueada pelo gate existente da rota e, no backend, qualquer POST/PUT/DELETE em `valores-profissional` retorna 403/422 por `[RequiresPapel(Dono, Recepcionista)]` + `[RequiresAcao("orcamento","configurar")]`. O front não exibe a aba/ações para quem não tem o papel.
- **CA7 (LGPD — sem PII no seletor)**: Dado o dropdown de profissional, Quando é populado, Então usa exclusivamente `vinculoService.listarProfissionaisPublico()` (sem e-mail/telefone) e nenhuma chamada a `listarProfissionais()` (lista completa) é feita por esta tela; o nome exibido na lista vem de `profissionalNome` do DTO (backend via JOIN), não montado no front.
- **CA8 (LGPD — mensagem genérica de erro)**: Dado um erro do backend ao salvar (ex.: 422), Quando o front trata, Então exibe mensagem genérica de toast sem PII e sem identificadores de tenant alheio.
- **CA9 (validação de formulário — todos obrigatórios)**: Dado o drawer aberto com função vazia, ou com algum campo numérico negativo, Quando o usuário tenta salvar, Então o submit é bloqueado no front, um toast de erro indica o campo pendente e nenhuma requisição de criação/edição é disparada.
- **CA10 (estado vazio)**: Dado nenhum valor cadastrado, Quando a aba carrega, Então é exibido `AppEmptyState` com título "Nenhum valor profissional cadastrado" e CTA "Criar primeiro valor" (não uma tabela vazia).
- **CA11 (estado de carregamento)**: Dado o carregamento da lista em andamento, Quando a request ainda não retornou, Então é exibido o indicador "Carregando…" (mesmo padrão de `EquipeTab`).
- **CA12 (integração com o form de orçamento)**: Dado pelo menos um valor profissional cadastrado, Quando o usuário abre `OrcamentoFormView` e adiciona um profissional à equipe, Então o select "Tabela de valor *" lista o(s) registro(s) recém-criado(s) — sem que nenhuma linha de `OrcamentoFormView.vue` tenha sido alterada nesta entrega.
- **CA13 (posição da aba)**: Dado o usuário em `OrcamentoSettingsView`, Quando visualiza as abas, Então "Valores profissional" aparece **entre** "Equipe" e "Anestesistas", com badge de contagem, e o deep-link `?aba=valores-profissional` (ou chave equivalente definida pelo dev) seleciona a aba.
- **CA14 (reuso do padrão)**: Dado o componente `ValoresProfissionalTab.vue`, Quando revisado, Então reutiliza os componentes do design system já usados em `EquipeTab.vue` e não introduz nenhum componente UI novo nem HTML/CSS scoped que duplique algo existente no design system.

## 8. Riscos e dependências

- **Dependência**: métodos de service (`listarValoresProfissional`, `criarValorProfissional`, `atualizarValorProfissional`, `removerValorProfissional`) e interfaces de payload **já existem** em `orcamentoCatalogoService.ts` — reusar, não recriar. `vinculoService.listarProfissionaisPublico()` já existe — reusar.
- **Risco baixo de regressão no form de orçamento**: o select "Tabela de valor" passa a popular. Validar que o `valorProfissionalId = 0` (não selecionado) continua sendo tratado como "não selecionado" no `OrcamentoFormView` (comportamento já existente, não tocar).
- **Dúvida do handler de Atualizar (reativar `ativo=false`)**: explicitamente **fora de escopo** — como não há gestão de status na UI, não precisa ser resolvida nesta entrega.

## 9. Observações para execução

- **Não-negociável**: reuso fiel do padrão `EquipeTab.vue` (estrutura de lista, drawer, confirmação, toast, badge `@contagem`). Sem componente novo no design system. Sem mudança de backend.
- **Não-negociável**: dropdown de profissional via `listarProfissionaisPublico()` (LGPD). Nunca `listarProfissionais()`.
- **Liberdade técnica**: nomes exatos dos StatCards/ícones, label da chave de aba (`valores-profissional` ou similar), e quais colunas exibir na tabela ficam a critério do dev, desde que respeitem os CAs.
- **Registrar a aba** em `OrcamentoSettingsView.vue`: importar o novo componente, adicionar à lista `abas` (entre `equipe` e `anestesistas`), ao `type AbaKey`, ao render `v-else-if`, e ao `setContagem`.
- O `imedto-developer` executa frontend-only. **Não acionar `imedto-database`.** Não atualizar `Docs/`.

## 10. Atualização de documentação

Nenhum. Demanda é reuso puro de padrão já documentado (aba de configuração de orçamento espelhando `EquipeTab.vue`), sem novo componente de design system, sem mudança de arquitetura/infra/LGPD. `Docs/DESIGN.md` e demais docs permanecem corretos.
