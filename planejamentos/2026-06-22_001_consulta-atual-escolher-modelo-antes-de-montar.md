# Consulta atual — exigir escolha do tipo de prontuário antes de montar os módulos

**ID**: 2026-06-22_001
**Status**: Aprovado por usuário em 2026-06-22
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P
**Áreas regressivas tocadas**: prontuário (Consulta atual + abas do prontuário)

> Nota operacional: implementar na MESMA branch de feature `feature/estruturar-secoes-pos-op-descricao-cirurgica` (já há trabalho não mergeado tocando `ProntuarioView.vue` e `ConsultaAtualTab.vue`), para evitar conflito. Isso é contexto de execução, não parte do produto.

## 1. Contexto e motivação

Hoje, ao abrir o prontuário de um paciente que **já tem prontuário iniciado**, a aba "Consulta atual" pré-seleciona automaticamente o modelo persistido (`modeloDeProntuarioId`) e **já monta e inicializa todos os módulos** do modelo — no estado atual isso aparece como "TIPO DE PRONTUÁRIO: todos" com os 17 módulos carregados de uma vez.

Esse comportamento força o médico a uma estrutura antes de ele decidir conscientemente o tipo de consulta que vai registrar. O usuário quer **inverter o fluxo**: primeiro o médico escolhe o tipo de prontuário (modelo); só então os módulos são montados/carregados. Isso reduz ruído visual, evita rolar 17 módulos quando a consulta é simples, e torna a escolha do modelo um ato deliberado (que define como a evolução será estruturada).

A inversão **não** pode penalizar quem só quer consultar histórico: ir direto para Consultas anteriores, Receitas, Atestado ou Pedidos de exame deve continuar funcionando sem escolher modelo nenhum (essas abas já são independentes do modelo hoje — esta demanda garante que continuem).

Evidência no código atual (validada): `ProntuarioView.vue` linha 209 faz `modeloConsultaAtual.value = pront.value.prontuario.modeloDeProntuarioId` e linha 211 chama `inicializarFormEvolucao()` — é esse par que pré-monta tudo. A aba só renderiza com `abaAtiva === 'consulta' && modeloConsultaAtual !== null` (linha 519). As demais abas usam `v-else-if abaAtiva === '...'` sem checar o modelo.

## 2. Persona-alvo

**Profissional (médico)** atendendo ou revisando um paciente. Momento da jornada: atendimento / prontuário. Frequência: várias vezes por dia, em todo paciente com prontuário iniciado. É o usuário que mais sofre com a montagem automática dos 17 módulos.

## 3. Escopo

**Inclui**:
- Na aba "Consulta atual", quando o prontuário já existe e **nenhum modelo foi escolhido ainda** na sessão, exibir um **empty-state com o seletor de modelo em destaque** (mensagem orientativa + seletor), reutilizando o padrão visual do card "Iniciar prontuário" e o componente `AppEmptyState` do design system.
- Só montar/carregar os módulos (`ConsultaAtualTab`) **após** a escolha do modelo.
- Manter a troca de modelo depois de escolhido funcionando como hoje (reinicia o form via watch de `modeloConsultaAtual`).
- Garantir (como regressão) que Consultas anteriores / Receitas / Atestado / Pedidos de exame abrem sem exigir escolha de modelo.
- Manter a lista de modelos exatamente como hoje: opção "todos" + modelos PADRÃO do sistema + modelos ESPECÍFICOS do estabelecimento; única diferença é que **nada vem pré-selecionado** (nem "todos").

**Não inclui**:
- Qualquer mudança em **como a lista de modelos é montada/filtrada** (`prontuarioService.listarModelos()` e seu filtro multi-tenant no backend permanecem intocados).
- Qualquer mudança no fluxo de **prontuário NOVO** (card "Iniciar prontuário" já força escolha — só garantir que não regride).
- Qualquer mudança em **backend, schema, migration ou contrato de API**. A evolução continua sendo gravada com o modelo escolhido na hora (via `modeloOverride` já existente em `registrarEvolucao`).
- Persistir a escolha de modelo entre recargas da página (ao recarregar/reabrir, volta ao empty-state — comportamento desejado: escolha é por sessão de abertura).
- Mudança no estado readonly de evoluções já registradas (vivem na aba Consultas anteriores, fora deste escopo).

## 4. Regras de negócio

- **R1** — Ao carregar um prontuário **existente**, a "Consulta atual" inicia **sem modelo escolhido** (`modeloConsultaAtual = null`), exibindo o empty-state. Não montar `ConsultaAtualTab` nem inicializar o form de evolução até a escolha. Mora em: Front (`ProntuarioView.vue`, função `carregar`). Validada em: front (UX). Sem espelho no back (puramente de apresentação; o backend nunca soube nem precisa saber qual modelo a tela "pré-carregou").
- **R2** — A montagem dos módulos da Consulta atual é disparada **exclusivamente** pela escolha de um modelo (qualquer item da lista, incluindo "todos"). Antes disso, nenhuma seção é renderizada e `inicializarFormEvolucao()` não roda. Mora em: Front (render condicional + watch de `modeloConsultaAtual`). Validada em: front.
- **R3** — As abas Consultas anteriores / Receitas / Atestado / Pedidos de exame são **independentes** do modelo: abrem e funcionam com `modeloConsultaAtual === null`. Mora em: Front (`ProntuarioView.vue`, render `v-else-if`). Validada em: front (regressão).
- **R4** — A lista de modelos oferecida no empty-state e no seletor de troca é **a mesma de hoje** ("todos" + padrões do sistema + específicos do estabelecimento), na mesma ordem/composição de `prontuarioService.listarModelos()`. Nenhum item é pré-marcado como selecionado. Mora em: Front (consome a lista existente). Multi-tenant: garantido pelo backend que já filtra a lista por estabelecimento — **não tocar**.
- **R5** — Trocar o modelo depois de já ter escolhido continua funcionando: reinicia o form de evolução (descarta o conteúdo não salvo da seleção anterior) exatamente como hoje (watch `modeloConsultaAtual` → `inicializarFormEvolucao`). Mora em: Front. Validada em: front.
- **R6** — Ao **salvar** a evolução, o modelo persistido é o **escolhido na hora** (não o `modeloDeProntuarioId` antigo do prontuário), via o mecanismo `modeloOverride` já existente em `registrarEvolucao`. Como agora `modeloConsultaAtual` parte de `null` e é definido pela escolha do médico, o override passa a refletir sempre a escolha consciente. Mora em: Front (monta o payload) → Handler de registro de evolução (back, **inalterado**). Validada em: back (fonte da verdade) + front.
- **R7** — Prontuário **novo** (sem prontuário iniciado) mantém o fluxo atual: card "Iniciar prontuário" com `AppSelect` desabilitando o botão até escolher; ao iniciar, recarrega e — pela R1 — a Consulta atual aparece no empty-state pedindo a escolha do modelo da primeira evolução. Mora em: Front. Validada em: front (regressão + consistência).

## 5. Modelo de dados

**Nenhuma alteração.** Sem tabela, coluna, índice, migration ou contrato novo.

- A evolução já é gravada com o modelo via `registrarEvolucao(pacienteId, conteudo, modeloOverride?, eventoId?)` — assinatura existente, sem mudança.
- `prontuario.modeloDeProntuarioId` continua existindo no payload de leitura; apenas **deixa de ser usado para pré-selecionar** a Consulta atual. Nenhum dado é perdido: o registro do prontuário e suas evoluções anteriores permanecem intactos; cada nova evolução grava o modelo escolhido na hora (comportamento que o `modeloOverride` já suportava).
- Multi-tenant: a lista de modelos (padrões do sistema + específicos do estabelecimento) já é filtrada por `estabelecimento_id` no backend. Esta demanda **não** toca essa query.
- Audit/LGPD: o acesso ao prontuário já é auditado nos pontos existentes (abertura/exportação). Esta mudança não cria novo ponto de acesso a PII nem novo dado pessoal — é reorganização de apresentação da mesma tela já auditada. Sem novo audit.

## 6. UX e fluxo

**Wireframe textual — Consulta atual, prontuário existente, antes da escolha:**

```
┌─ Abas: [Consulta atual] Consultas anteriores  Receitas  Atestado  Pedidos de exame ─┐
│                                                                                      │
│                          (área central — AppEmptyState)                              │
│                              [ ícone fa-stethoscope ]                                 │
│                   "Selecione o tipo de prontuário para iniciar a consulta"           │
│            "Escolha um modelo para montar os módulos desta evolução."                │
│                                                                                      │
│                    ┌────────────── slot #acao ──────────────┐                        │
│                    │  Tipo de prontuário                     │                        │
│                    │  [ AppSelect: Selecione um modelo  ▼ ]  │  ← destacado           │
│                    └─────────────────────────────────────────┘                        │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

- Reutilizar `AppEmptyState` (já no DS, suporta slot `#acao`) com o seletor de modelo no slot de ação — espelhando o visual do card "Iniciar prontuário" (`ProntuarioView.vue` linhas 491-509): título, subtítulo orientativo e `AppSelect` ligado ao modelo. A opção placeholder `Selecione um modelo` (disabled) abre a lista; os itens são `modelos` com sufixo "(sistema)" quando `ehPadraoSistema`, como já feito no card de iniciar.
- **Escolha do modelo no empty-state** define `modeloConsultaAtual` → dispara montagem do `ConsultaAtualTab` (seletor "Tipo de prontuário", módulos, sidebar, CTA "Salvar evolução"), exatamente o layout atual de 3 colunas.
- **Troca posterior**: usa o seletor "Tipo de prontuário" que já existe dentro de `ConsultaAtualTab` (popover, linhas 132-171). Comportamento inalterado.
- **Outras abas**: clicar em Consultas anteriores / Receitas / Atestado / Pedidos de exame abre direto, sem passar pelo empty-state (o empty-state é só da aba "consulta").
- **Estados**:
  - *Sem modelo (consulta atual)*: empty-state com seletor (novo estado).
  - *Com modelo (consulta atual)*: módulos montados (estado atual, agora pós-escolha).
  - *Carregando*: mensagem "Carregando prontuário..." já existente (inalterada).
  - *Erro de carga*: mensagem de erro já existente (inalterada).
  - *Prontuário não iniciado*: card "Iniciar prontuário" já existente (inalterado).
- **Tipografia**: qualquer markup/CSS novo usa exclusivamente os tokens (CLAUDE.md §5) — nada de `font-size`/`font-weight` literal. Preferir os componentes do DS (`AppEmptyState`, `AppField`, `AppSelect`, `AppButton`) que já herdam a escala.
- **Mobile/responsivo**: o empty-state do DS já é responsivo; o seletor ocupa largura confortável em telas estreitas. Sem layout custom novo que precise de breakpoint próprio.
- **Atalhos de teclado**: nenhum novo.

## 7. Critérios de aceite (testáveis)

- **CA1** (empty-state ao abrir prontuário existente): Dado um paciente com prontuário já iniciado, Quando o médico abre o prontuário e a aba ativa é "Consulta atual", Então a tela exibe o empty-state com a mensagem "Selecione o tipo de prontuário para iniciar a consulta" e o seletor de modelo em destaque, e **nenhum** módulo é renderizado (a lista de módulos `mod-*` não existe no DOM).

- **CA2** (escolha monta os módulos): Dado o empty-state da Consulta atual, Quando o médico seleciona um modelo qualquer da lista, Então os módulos do modelo escolhido são montados (o `ConsultaAtualTab` aparece com a sidebar de módulos, os cards de seção e o CTA "Salvar evolução") e o empty-state some.

- **CA3** ("todos" não vem pré-selecionado): Dado um prontuário existente cujo modelo persistido é o "todos", Quando a Consulta atual abre, Então o seletor **não** vem pré-marcado com "todos" (nem com nenhum outro) e nenhum módulo está montado até a escolha — confirmando que mesmo "todos" precisa ser escolhido.

- **CA4** (lista de modelos inalterada): Dado o seletor do empty-state, Quando o médico abre a lista, Então ela contém exatamente os mesmos itens de hoje — a opção "todos", os modelos padrão do sistema (marcados/identificáveis como sistema) e os modelos específicos do estabelecimento — na mesma composição retornada por `listarModelos()`, sem item adicional nem removido.

- **CA5** (multi-tenant da lista): Dado um médico logado no estabelecimento B, Quando abre o seletor de modelos, Então vê apenas os modelos padrão do sistema + os específicos do estabelecimento B, e nenhum modelo específico de outro estabelecimento (A) — garantido pela query de backend já existente, que esta entrega **não** altera.

- **CA6** (outras abas independentes — regressão): Dado um prontuário existente em que **nenhum** modelo foi escolhido na Consulta atual, Quando o médico clica em Consultas anteriores, Então a aba carrega normalmente (lista de evoluções/anexos) sem exigir escolha de modelo. O mesmo vale, separadamente, para Receitas, Atestado e Pedidos de exame.

- **CA7** (troca de modelo após escolher): Dado que o médico já escolheu o modelo X e os módulos estão montados, Quando troca para o modelo Y pelo seletor "Tipo de prontuário" do `ConsultaAtualTab`, Então os módulos remontam conforme Y e o form de evolução é reiniciado (conteúdo não salvo de X é descartado) — como hoje.

- **CA8** (salvar grava o modelo escolhido): Dado que o médico escolheu o modelo X (diferente do `modeloDeProntuarioId` antigo do prontuário) e preencheu ao menos uma seção, Quando clica em "Salvar evolução", Então a evolução é registrada com sucesso e o modelo associado à evolução é o X escolhido na hora (via `modeloOverride`), não o modelo antigo do prontuário — sem perda de dados do prontuário ou de evoluções anteriores.

- **CA9** (performance — sem trabalho até a escolha): Dado um prontuário existente recém-aberto na Consulta atual (sem escolha), Quando inspecionamos o estado, Então `inicializarFormEvolucao()` **não** foi executado (o objeto `novaEvolucao` está vazio) e o `ConsultaAtualTab` não foi montado — evitando montar 17 módulos e inicializar o form sem necessidade.

- **CA10** (prontuário novo — regressão): Dado um paciente **sem** prontuário iniciado, Quando o médico abre o prontuário, Então aparece o card "Iniciar prontuário" com o botão desabilitado até escolher um modelo; ao iniciar, o prontuário é criado e a Consulta atual passa a exibir o empty-state da R1 pedindo a escolha do modelo da primeira evolução (consistência de fluxo, sem regressão).

- **CA11** (estado de carga/erro preservado): Dado o carregamento do prontuário, Quando está carregando, Então a mensagem "Carregando prontuário..." aparece como hoje; e Quando a carga falha, Então a mensagem de erro genérica aparece como hoje — o empty-state da Consulta atual só é avaliado após carga bem-sucedida e com prontuário existente.

- **CA12** (tipografia só-token): Dado qualquer markup/CSS novo introduzido para o empty-state, Quando `npm run check:typography -- --ci` roda, Então passa sem novos literais de `font-size`/`font-weight` (CLAUDE.md §5).

## 8. Riscos e dependências

- **Risco — quebrar a montagem condicional existente**: a condição atual `abaAtiva === 'consulta' && modeloConsultaAtual !== null` já cobre o "sem modelo = não monta"; o cuidado é **não** repor `modeloConsultaAtual` em `carregar()` (linha 209) e **não** chamar `inicializarFormEvolucao()` na carga inicial de prontuário existente (linha 211). Vigiar para não introduzir um `v-else` que esconda as outras abas quando `modeloConsultaAtual === null` (a Consulta atual sem modelo deve mostrar o empty-state, mas as outras abas continuam pelo `v-else-if`).
- **Risco — regressão silenciosa nas outras abas**: garantir que o empty-state da Consulta atual seja renderizado **apenas** quando `abaAtiva === 'consulta'`, nunca substituindo o conteúdo de anteriores/receitas/atestado/pedidos-exame.
- **Dependência operacional**: branch compartilhada `feature/estruturar-secoes-pos-op-descricao-cirurgica` (já mexe nesses dois arquivos). Atenção a conflito de merge, mas é só coordenação, não dependência funcional.
- **Sem dependência de backend/DB**: contrato e schema inalterados.

## 9. Observações para execução

- **Não-negociável**:
  - Não alterar `prontuarioService.listarModelos()` nem sua query/filtro multi-tenant no backend.
  - Não alterar a assinatura nem o comportamento de `registrarEvolucao`/handler de evolução — o `modeloOverride` já existe e cobre a R6/CA8.
  - Reutilizar `AppEmptyState` (slot `#acao`) em vez de criar componente novo. Reaproveitar o visual/labels do card "Iniciar prontuário" para o seletor destacado (consistência DS).
  - Empty-state só na aba `consulta`; as outras abas permanecem pelo `v-else-if`.
- **Liberdade técnica do dev**:
  - Decidir se o empty-state vira um pequeno componente local de `ProntuarioView` ou markup inline na própria view (qualquer um serve, desde que use `AppEmptyState` por baixo).
  - Definir a cópia exata da mensagem orientativa (a do CA1 é referência; pode ajustar o subtítulo mantendo o sentido).
- **Ponto-chave de implementação** (já investigado): em `carregar()` (ProntuarioView.vue), no ramo `if (pront.value)`, remover/condicionar a pré-seleção `modeloConsultaAtual.value = pront...modeloDeProntuarioId` e a chamada `inicializarFormEvolucao()`, deixando `modeloConsultaAtual` em `null` para prontuário existente. O watch de `modeloConsultaAtual` e o `inicializarFormEvolucao` passam a rodar só quando o médico escolher. Manter o carregamento de `totalEvolucoes`/contagem que hoje convive nesse ramo (não depende do modelo).
- **Frontend-only**: confirmado. Nenhum acionamento do `imedto-database`. Nenhuma migration.

## 10. Atualização de documentação

- **Nenhuma atualização obrigatória de `Docs/`** para esta entrega: ela reutiliza `AppEmptyState` (já documentado no design system) e segue o padrão de empty-state já descrito em `Docs/DESIGN.md` ("uma tabela vazia usa `AppEmptyState`, não um `<p>` solto"). Não introduz componente novo, padrão de service/store novo, nem regra cross-cutting.
- Se, na implementação, o dev decidir extrair um componente reutilizável novo para "empty-state com seletor em destaque" (em vez de markup local), então `Docs/DESIGN.md §componentes` deve ganhar uma linha registrando esse componente. Caso contrário, sem mudança de doc.
