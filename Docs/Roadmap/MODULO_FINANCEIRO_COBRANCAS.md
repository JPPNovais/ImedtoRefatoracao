# Módulo Financeiro / Cobranças — Plano Mestre

> **Natureza**: plano mestre de refinamento. NÃO é briefing executável. Cada fase abaixo
> vira depois um briefing imutável próprio em [`planejamentos/`](../../planejamentos/),
> precedido (nas fases marcadas) de refinamento com o usuário. Este documento é a fonte
> de verdade da visão; os briefings são a fonte de verdade da execução.
>
> **Por que existe**: introduzir cobrança do paciente (contas a receber) ligando
> agendamento → atendimento → prontuário → orçamento → financeiro → relatórios, sem
> errar cálculo monetário e sem vazar dado financeiro sensível entre tenants/papéis.
>
> **Relação com o roadmap**: materializa e detalha o item "financeiro" citado em
> [`FASE_3_DIFERENCIACAO.md`](FASE_3_DIFERENCIACAO.md). É um épico transversal próprio,
> grande demais para um item de fase — por isso ganha plano dedicado.

---

## 1. Visão e princípios

**Objetivo de negócio**: o profissional/recepção registra o que foi **cobrado** do
paciente e, separadamente, o que foi **pago** (com formas de pagamento, parcial e
múltiplo), de forma que cada centavo apareça consolidado no Financeiro da clínica e,
detalhado por paciente, em Relatórios (custo, pagamento, lucro, desconto, juros, taxa).

**Conceito-âncora**: `cobrado ≠ pago`. Uma **Cobrança** é a conta a receber (quanto o
paciente deve). Um ou mais **Pagamentos** quitam a cobrança ao longo do tempo. Esses são
dois conceitos distintos no domínio — não os fundir.

### Princípios não-negociáveis

1. **Cálculo monetário não pode errar.**
   - Todo valor é `decimal` (nunca `float`/`double`), como já é em
     [`Lancamento.cs`](../../backend/src/Services/Imedto.Backend.Domain/Financeiro/Lancamento.cs) (`decimal Valor`).
   - **Todo cálculo no backend**, em métodos puros/testáveis no espírito do
     `OrcamentoCalculadora` já existente. O front nunca decide o valor final — só pré-visualiza
     (preview com debounce, como o orçamento já faz). A fonte da verdade é o 422 do `BusinessException`.
   - **Invariantes do agregado validadas no domínio**, não no controller nem no SQL.
   - **Arredondamento (decisão fechada — §5 q5):** **2 casas decimais, half-away-from-zero**
     (`MidpointRounding.AwayFromZero`), definido em **um único helper de domínio** e sempre no backend.

2. **Regra de negócio no backend** (Domain/Handler). Trava de front é UX; espelho obrigatório no back.

3. **Multi-tenant em camadas.** Toda Cobrança/Pagamento filtra `estabelecimento_id`.
   Procedimento do catálogo só do estabelecimento ativo (premissa explícita do usuário:
   "nunca de outros"). Repositório falha-fechada; mensagem genérica de "não encontrado".

4. **LGPD — dado financeiro do paciente é sensível.**
   - Minimização: DTO só com os campos da tela.
   - **Audit trail** em todo acesso/registro na **aba Financeiro do paciente** (ela é uma porta
     direta ao dado financeiro vinculado a um paciente identificado), no mesmo padrão de
     `paciente_acesso_log` usado pelo relatório de acessos (F1 item 1.8).
   - **RBAC com permissão própria**, separada das demais: proposta `financeiro_paciente.ver`
     e `financeiro_paciente.registrar` (catálogo em
     [`CatalogoPermissoes.cs`](../../backend/src/Services/Imedto.Backend.Domain/ModelosPermissao/CatalogoPermissoes.cs)).
     O Financeiro **da clínica** (`/financeiro`, agregado) é outra permissão, mais ampla — não confundir.
   - Sem PII em log/mensagem de erro.

5. **Reuso > duplicação.** Pagamento **gera `Lancamento`**, não substitui. `FormaPagamento`,
   `CategoriaFinanceira`, `OrcamentoCalculadora`, catálogos de orçamento, `paciente_acesso_log`,
   notificação interna, export CSV de relatórios — tudo já existe e é reutilizado.

6. **Duas portas, uma cobrança.** O ícone no agendamento e a aba Financeiro do paciente
   operam **a mesma** entidade `Cobranca`. Registrar em uma repercute na outra — sem duplicidade.

7. **Recibo ≠ Nota Fiscal.** São documentos distintos e não se confundem:
   - **Recibo de pagamento** (F8) — documento **interno** em PDF, gerado pelo próprio Imedto a partir
     de um `Pagamento` quitado. Sem valor fiscal, sem transmissão a terceiros. É um comprovante para o
     paciente. Reusa o pipeline de PDF do servidor (mesma fundação de `usePdfHeader.ts` / PDF de receita).
   - **NFS-e** (F9) — documento **fiscal** (Nota Fiscal de Serviço Eletrônica), emitido **via provedor
     externo** (gateway), transmitido à prefeitura, com chave de acesso, XML e DANFSE. Exige CNPJ,
     certificado digital e configuração de emissor por estabelecimento. Vive em **bounded context próprio
     `Faturamento`** — não se mistura com Cobrança/Pagamento. Detalhe e viabilidade em
     [`Docs/Discoverys/nota-fiscal/`](../Discoverys/nota-fiscal/).

---

## 2. Modelo de domínio proposto

### 2.1 Novos agregados / entidades

**`Cobranca`** (aggregate root — conta a receber do paciente)
- `id`, `estabelecimento_id` (tenant), `paciente_id`
- `origem`: `Consulta | Procedimento | Cirurgia` (extensível — `enum` aberto à evolução)
- `agendamento_id?` (consulta/procedimento), `orcamento_id?` (cirurgia/procedimento via orçamento)
- `tipo_atendimento`: `Particular | Convenio`; `convenio_id?`
- `valor_cobrado` (decimal), `desconto` (decimal, default 0)
- `status`: `Aberta | ParcialmentePaga | Paga | Cancelada` (derivado dos pagamentos; ver invariantes)
- `descricao`, `criado_por`, timestamps, audit
- coleção `Pagamentos` (1:N)

**`Pagamento`** (entidade filha de `Cobranca`)
- `id`, `cobranca_id`, `valor` (decimal)
- `forma_pagamento_id` → reusa `FormaPagamento` existente
- `parcelas` (int, default 1), `juros` (decimal), `taxa` (decimal — **derivada da config por forma de pagamento**, ver §2.4)
- `data_pagamento`, `registrado_por_usuario_id`, audit
- `lancamento_id` (FK para o `Lancamento` gerado atomicamente)

**`EstornoPagamento`** (entidade de histórico — **nunca apaga/edita o `Pagamento` original**)
- `id`, `pagamento_id` (FK do pagamento estornado), `valor` (decimal), `motivo`, `estornado_por_usuario_id`, `data_estorno`, audit
- `lancamento_estorno_id` (FK para o `Lancamento` de estorno gerado atomicamente — ver INV-7)
- Decisão já fechada: estorno **sempre gera registro próprio + `Lancamento` de estorno**; o pagamento original permanece imutável no histórico.

**`ConfigTaxaFormaPagamento`** (config de taxa de cartão — aba de configuração do Financeiro)
- `estabelecimento_id` (tenant), `forma_pagamento_id` → reusa `FormaPagamento`
- `taxa_percentual` (decimal), `ativo`
- Decisão já fechada: a taxa é **configurada por forma de pagamento** nesta aba e aplicada automaticamente ao registrar o `Pagamento` — **nunca informada manualmente a cada pagamento**.

**`TabelaPrecoConsulta`** (cadastro de preços sugeridos)
- `estabelecimento_id` (tenant), `profissional_id?` (null = preço padrão do estabelecimento)
- `valor_sugerido` (decimal), `ativo`
- Decisão já fechada: valor sugerido e **editável** no check-in.

**`Convenio`** (cadastro — detalhado só na F6, mas reservado aqui)
- `estabelecimento_id`, `nome`, `registro_ans?`, `ativo`, planos (1:N) — escopo real definido no refinamento da F6 (versão simples).

**`PendenciaAtendimento`** (NOVO — F3B; projeção operacional do checklist de conduta)
- `id`, `estabelecimento_id` (tenant), `paciente_id`, `evolucao_id` (origem), `agendamento_id?`
- `acao`: enum fixo `CriarReceita | CriarAtestado | PedirExame | CriarOrcamento | MarcarProcedimentoRealizado | AgendarRetorno`
- `status`: `Pendente | Concluida` (conclui automaticamente quando a ação é executada)
- `concluida_em?`, `referencia_id?` (id da receita/orçamento/etc. que concluiu a pendência), `criado_por`, audit
- A evolução permanece **append-only**; a pendência é mutável só no `status`. Não é financeiro, mas é a
  **ponte** que dispara F4 (procedimento realizado) e F5 (criar orçamento).

**`ConfigComissaoProfissional`** (NOVO — F7; comissão de consulta/procedimento por profissional)
- `estabelecimento_id` (tenant), `profissional_id` (vínculo na **Equipe**), `tipo`: `Consulta | Procedimento`
- `percentual` (decimal) — alternativamente `valor_fixo?` (decimal); editável por quem tem permissão (Dono).
- Decisão fechada (§5 q9): a comissão de **consulta e procedimento** (fora do orçamento) é configurada na
  área de **Equipe** (profissionais vinculados ao estabelecimento), **por profissional**, com **valor default
  do sistema** refletindo o mais comum do mercado (proposta a validar no briefing da F7 — ver §F7). Para
  **cirurgia**, a comissão continua saindo do `OrcamentoEquipe` (já existe) — esta entidade não a substitui.

> **Recibo de pagamento (F8) não é entidade nova.** O recibo é gerado **on-the-fly** em PDF a partir de um
> `Pagamento` já quitado (e seus dados de `Cobranca`/paciente/estabelecimento) — não há agregado próprio.
> Se for preciso registrar que um recibo foi emitido (audit), basta um campo/flag em `Pagamento`
> (`recibo_emitido_em?`) — decisão de schema fica no briefing da F8. **NFS-e**, ao contrário, é agregado
> próprio em bounded context separado (ver §F9 e o discovery).

### 2.2 Relacionamento com o que já existe (diagrama)

```
                         ┌──────────────────────┐
                         │  Agendamento         │  status, check_in_em
                         │  (existe)            │  + NOVO: tipo_atendimento, valor no check-in
                         └──────────┬───────────┘
                                    │ agendamento_id?
                                    ▼
   Paciente ───────────────►  ┌───────────┐  origem={Consulta|Procedimento|Cirurgia}
   (existe)   paciente_id     │ Cobranca  │  tipo_atendimento={Particular|Convenio}
                              │  (NOVO)   │  valor_cobrado, desconto, status
                              └─────┬─────┘
                          orcamento_id?    │ 1:N
                                  ▲        ▼
                  ┌───────────────┘   ┌──────────┐  valor, forma_pagamento_id,
                  │                   │ Pagamento │  parcelas, juros, taxa
            ┌───────────┐            │  (NOVO)   │
            │ Orcamento │            └─────┬─────┘
            │  (~95%)   │                  │ gera atômico (mesma transação)
            │ Itens ↔   │                  ▼
            │ catálogo  │            ┌────────────┐  Tipo=Receita, Status=Pago,
            │ ↔ produto │            │ Lancamento │  cobranca_id?, pagamento_id?
            │ ↔ estoque │            │  (existe)  │  → alimenta /financeiro e Relatórios
            └───────────┘            └────────────┘

   FormaPagamento (existe) ──► usada por Pagamento e por Lancamento
   TabelaPrecoConsulta (NOVO) ──► sugere valor no check-in da consulta
```

> Nota de schema: `Lancamento` hoje tem `orcamento_id?` mas **não** tem `cobranca_id`/`pagamento_id`.
> A F1 deverá adicionar esse vínculo (decisão do `imedto-database` no briefing da F1).

### 2.3 Invariantes explícitas

- **INV-1**: `SUM(pagamentos.valor) ≤ valor_cobrado − desconto`. Excesso é rejeitado (422).
- **INV-2**: `status` é **derivado**: sem pagamento → `Aberta`; soma < total → `ParcialmentePaga`;
  soma = total → `Paga`. Nunca setado "à mão" salvo `Cancelada`.
- **INV-3**: registrar Pagamento **gera `Lancamento` de Receita (Pago) na MESMA transação**
  (atômico — ou os dois persistem, ou nenhum). Falha de um faz rollback do outro.
- **INV-4**: `desconto ≤ valor_cobrado` e `desconto ≥ 0`.
- **INV-5**: `valor` de cada Pagamento `> 0`.
- **INV-6**: `Cobranca` sempre vinculada a `estabelecimento_id` e `paciente_id` (não-nulos).
- **INV-7** (decisão fechada): **estorno é sempre com histórico.** Estornar um Pagamento
  **gera um `EstornoPagamento` + um `Lancamento` de estorno na MESMA transação** (atômico),
  e **nunca** apaga nem edita o `Pagamento`/`Lancamento` originais. O `status` da Cobrança é
  recalculado pela INV-2 sobre a soma líquida (pagamentos − estornos). Cancelar uma Cobrança paga
  exige estornar os pagamentos correspondentes — o histórico financeiro permanece íntegro.
- **INV-8** (desconto — RBAC, decisão fechada): só pode aplicar desconto quem tiver **permissão de
  aprovar orçamento** OU **permissão de financeiro** OU for **Dono**. Validado no backend (422 se sem
  permissão); trava de front é UX. Teto de desconto por papel fica fora desta versão (ver anti-escopo §6).

### 2.4 Configuração do Financeiro (aba de config — decisão fechada)

Nasce uma **aba de Configuração do Financeiro** (reusa o padrão master-detail de
[`OrcamentoSettingsView.vue`](../../frontend/src/views/orcamentos/OrcamentoSettingsView.vue), que já
configura juros/entrada/taxa por forma de pagamento no orçamento). Ela hospeda:
- **Taxa de cartão por forma de pagamento** (`ConfigTaxaFormaPagamento`) — aplicada automaticamente
  ao registrar o pagamento. Sem digitação manual de taxa no ato.
- (futuro) demais parâmetros de caixa/fechamento — ver F1/F7 e questões abertas §5.

> Reuso: a config de taxa por forma de pagamento já tem precedente no orçamento. Estender o padrão,
> não criar tela paralela.

---

## 3. Fluxos ponta a ponta

### 3.1 Consulta — Particular

1. **Dado** um agendamento confirmado, **Quando** a recepção faz o check-in (`CheckInModal.vue`),
   **Então** escolhe `Particular` e o sistema sugere o valor da `TabelaPrecoConsulta`
   (por profissional, senão do estabelecimento), **editável**.
2. **Quando** confirma o check-in, **Então** cria-se uma `Cobranca` origem=Consulta,
   tipo=Particular, status=Aberta, `valor_cobrado` = valor informado.
3. **Quando** o paciente paga, **Então** via **ícone de pagamento no agendamento**
   (`AgendamentoRow.vue`) **ou** pela aba Financeiro do paciente, registra-se 1..N `Pagamento`
   (forma, parcial, múltiplas formas). Cada um gera `Lancamento` atômico.
4. **Então** status migra Aberta→ParcialmentePaga→Paga conforme INV-2; aparece no `/financeiro`
   e, detalhado por paciente, em Relatórios.

### 3.2 Consulta — Convênio (planejado na F6; aqui de alto nível)

1. **Dado** check-in com `Convenio` selecionado, **Então** Cobrança nasce tipo=Convenio,
   `convenio_id` setado; o "valor cobrado" do paciente pode ser zero/coparticipação (definido no refinamento).
2. **Quando** o atendimento ocorre, **Então** entra no fluxo de faturamento de convênio
   (guia/autorização → faturamento em lote → glosa → repasse). Ver explicação didática na §F6.
3. **Então** o **repasse** recebido do convênio entra como Receita; coparticipação do paciente
   (se houver) é um Pagamento normal. (Escopo exato decidido na F6.)

### 3.3 Procedimento

1. **Dado** que a seção "Procedimentos indicados" do prontuário passou a buscar do **catálogo de
   procedimentos** (F3) — `SecaoProcedimentosIndicados.vue` deixa de ser texto livre —, **Quando**
   o profissional indica um procedimento, **Então** ele vem do catálogo do estabelecimento ativo
   (com atalho de criação inline, vinculado só ao tenant ativo).
2. **Quando** o procedimento é **marcado como realizado/completado** (item do checklist de conduta —
   ver F3B; é o mesmo gatilho da baixa de estoque), **Então** gera-se `Cobranca` origem=Procedimento com
   `valor_cobrado` = valor do catálogo (F4) **e** dispara a **baixa automática de estoque** (decisão
   fechada — ver abaixo).
3. **Baixa automática de estoque** (decisão fechada): ao marcar o procedimento como realizado, o vínculo
   `orcamento_catalogo_cirurgia_produto` + a consolidação de produtos já existente determinam os insumos
   consumidos e geram `MovimentacaoEstoque` de saída automaticamente. Isso alimenta o **custo real** por
   paciente nos Relatórios (F7) — não é mais baixa manual.
4. **Quando** o paciente paga, **Então** ícone no agendamento **ou** aba Financeiro — mesmo fluxo de Pagamento.

### 3.4 Cirurgia

1. **Dado** a evolução fechada com procedimentos indicados, **Quando** o fluxo de cirurgia inicia,
   **Então** cria-se um `Orcamento` **pré-preenchido** com os procedimentos indicados (reusa o
   aggregate `Orcamento` ~95% pronto: Itens, Equipe, Implantes, FormasPagamento, `OrcamentoCalculadora`).
2. **Quando** o usuário completa os demais campos e o orçamento é impresso/aprovado, **Então** o
   **valor do orçamento = valor_cobrado** de uma `Cobranca` origem=Cirurgia (`orcamento_id` setado).
3. **Quando** o paciente paga, **Então** o registro de pagamento se dá **direto na aba Financeiro
   do paciente** (decisão do usuário: para cirurgia o ícone do agendamento NÃO registra pagamento;
   a aba financeiro mostra os 3 tipos).
4. **Mudança do orçamento após a cobrança gerada** (decisão fechada): a **cobrança acompanha o orçamento**,
   mantendo **histórico das alterações de valor**. Até ser paga, o valor é apenas **pendente** — nada trava,
   nada se perde. Cada alteração de `valor_cobrado` registra uma linha de histórico (quem, quando, de→para).
   Quando há pagamento parcial e o valor muda, a INV-1/INV-2 recalculam o saldo sobre o novo total.
5. **Então** o vínculo procedimento↔produto↔estoque (já existente em `orcamento_catalogo_cirurgia_produto`)
   alimenta custo/lucro por paciente nos Relatórios (F7) — a baixa de estoque da cirurgia ocorre ao marcar
   os procedimentos como realizados (mesmo gatilho da F3B/F4).

### 3.5 Conduta como checklist → pendências do atendimento (NOVO — F3B)

A seção **Conduta** da evolução do prontuário deixa de ser texto livre e passa a ser um **checklist de
ações fixas do sistema** (lista canônica decidida com o usuário). Ela é a **ponte operacional para o
financeiro**: marcar "procedimento realizado" é o gatilho da cobrança de procedimento (F4) e da baixa de
estoque; marcar "criar orçamento" é a entrada da cirurgia (F5).

1. **Dado** que o profissional preenche a evolução, **Quando** chega na seção Conduta, **Então** vê um
   **checklist de ações** (lista fixa — proposta canônica abaixo, a confirmar no refinamento):
   - **Criar receita**
   - **Criar atestado**
   - **Pedir exame** (solicitação de exames)
   - **Criar orçamento** (entrada do fluxo de cirurgia — F5)
   - **Marcar procedimento como realizado** (gatilho de cobrança de procedimento F4 + baixa de estoque)
   - **Agendar retorno**
2. **Quando** o profissional **salva a evolução**, **Então** os itens marcados viram **pendências do
   atendimento** daquele paciente (entidade nova `PendenciaAtendimento`, vinculada a paciente + evolução +
   estabelecimento). A evolução permanece **append-only** (a Conduta marcada fica registrada no
   `ConteudoJson`; as pendências são uma projeção operacional separada, mutável quanto ao status).
3. **UX (decidida com o usuário)** — dois pontos de contato:
   - **Modal pós-salvar**: empurrão imediato logo após salvar a evolução, com **links diretos** para cada
     ação marcada (ex.: "Criar receita" → aba de receita já no contexto do paciente).
   - **Painel persistente** na página do paciente, **visível até tudo ser concluído** — o profissional/recepção
     não esquece nada.
4. **Conclusão automática**: cada item **marca-se concluído sozinho** quando a ação correspondente é
   realizada — receita criada → "criar receita" concluído; orçamento criado → "criar orçamento" concluído;
   procedimento marcado como realizado → conclui o item **e** dispara cobrança (F4) + baixa de estoque;
   retorno agendado → "agendar retorno" concluído; etc.
5. **Multi-tenant + RBAC + audit** valem como sempre: pendências só do tenant+paciente; só quem tem o
   papel para executar cada ação vê o link ativo; acesso à página do paciente continua auditado.

> **Delta real no código** (verificado): hoje a Conduta é a seção `key="conduta"`, `tipo: "texto_longo"`
> em [`modeloProntuarioBuilder.ts`](../../frontend/src/components/ui/modeloProntuarioBuilder.ts) (catálogo
> das 17 seções), renderizada genericamente como **textarea** por `SecaoProntuario.vue` dentro de
> [`ConsultaAtualTab.vue`](../../frontend/src/components/prontuario/tabs/ConsultaAtualTab.vue) (já tem ícone
> `fa-clipboard-check`). **Não existe** componente dedicado nem qualquer noção de "pendência". A F3B cria:
> (a) um componente de seção dedicado `SecaoCondutaChecklist.vue` (lista fixa de ações), (b) a entidade
> `PendenciaAtendimento` + handlers de criação ao salvar e de conclusão automática nos gatilhos das ações,
> (c) o modal pós-salvar e (d) o painel persistente em `PacienteDetalheView.vue`. Manter retrocompat com
> evoluções antigas em que `conduta` é texto livre (render legado read-only).

---

## 4. Fases

> Cada fase = um briefing imutável próprio depois. Fases marcadas **[refinar antes]** exigem
> rodada de `AskUserQuestion` com o usuário antes do briefing.

### F1 — Fundação + Consulta (domínio, check-in, tabela de preços, ícone de pagamento) — **L**
- **Objetivo**: nascer o módulo de cobrança ponta a ponta para o caso mais simples (consulta particular).
- **IN**: agregados `Cobranca`/`Pagamento`/`TabelaPrecoConsulta`; campo Particular/Convênio + valor no
  check-in (`CheckInModal.vue`, `agendamentos`); ícone de pagamento no card (`AgendamentoRow.vue`);
  geração atômica de `Lancamento`; vínculo `cobranca_id`/`pagamento_id` no `Lancamento`; permissões
  `financeiro_paciente.*`; tela mínima de cadastro da tabela de preços; pagamentos parciais/múltiplos.
- **OUT**: aba Financeiro do paciente (F2); convênio real (F6 — aqui só persiste tipo+convenio_id);
  procedimento/cirurgia.
- **Dependências**: nenhuma dura. Schema → `imedto-database` (migration nova + alter em `lancamentos`).
- **CAs de alto nível**: cobrança criada no check-in; pagamento parcial muda status corretamente;
  Lancamento atômico (rollback verificado); multi-tenant; RBAC; audit; valor sugerido editável; decimal correto.
- **Riscos**: atomicidade Pagamento↔Lancamento; mexer no fluxo de check-in (regressão de agenda).

### F2 — Aba Financeiro do paciente — **M**
- **Objetivo**: segunda porta para a MESMA cobrança; visão por paciente.
- **IN**: implementar a aba (hoje empty state "em breve" em `PacienteDetalheView.vue`); listar
  cobranças do paciente, registrar pagamento; mostrar os 3 tipos de origem; audit a cada acesso.
- **OUT**: redesign do `/financeiro` (F7).
- **Dependências**: F1 (agregado + permissões).
- **CAs**: aba lê só cobranças do tenant+paciente; registrar aqui repercute no agendamento (sem duplicidade);
  acesso auditado; RBAC `financeiro_paciente.ver/registrar`; estados loading/vazio/erro.
- **Riscos**: garantir porta única de domínio (não duplicar lógica entre as duas telas).

### F3 — Procedimentos indicados ligados ao catálogo + atalho de criação — **M** *(pode rodar em paralelo à F1/F2)*
- **Status**: implementado — `planejamentos/2026-06-10_011_financeiro-f3-procedimentos-catalogo.md` (CA43–CA58); build verde; 711 testes verdes; aguarda commit do QA.
- **Objetivo**: preparar o prontuário para gerar cobrança de procedimento.
- **IN**: `SecaoProcedimentosIndicados.vue` passa de texto livre para seletor do catálogo de
  procedimentos do estabelecimento ativo; atalho de criação inline (procedimento só do tenant ativo);
  ajuste do `ConteudoJson` da evolução para guardar referência ao catálogo (mantendo append-only).
- **OUT**: a cobrança em si (F4).
- **Dependências**: catálogo de orçamento (já existe). Independe de F1/F2 — paralelizável.
- **CAs**: seção busca do catálogo do tenant ativo; criação inline grava no tenant ativo; nunca lista
  procedimento de outro estabelecimento; evolução permanece imutável; compatibilidade com evoluções antigas (texto livre).
- **Riscos**: migração/compat de evoluções históricas com texto livre; não quebrar render do prontuário.

### F3B — Conduta como checklist → pendências do atendimento **[refinar antes]** — **M**
- **Objetivo**: transformar a seção Conduta da evolução em checklist de ações fixas que viram pendências do
  atendimento, guiando a sequência clínica e servindo de **ponte para o financeiro** (gatilhos de F4 e F5).
- **IN**: componente de seção `SecaoCondutaChecklist.vue` (lista fixa — criar receita, criar atestado, pedir
  exame, criar orçamento, marcar procedimento realizado, agendar retorno); entidade `PendenciaAtendimento`
  (tenant + paciente + evolução + status); criação das pendências ao salvar a evolução; **modal pós-salvar**
  com links diretos; **painel persistente** na página do paciente (`PacienteDetalheView.vue`) até concluir
  tudo; **conclusão automática** em cada gatilho de ação (receita/orçamento/procedimento/retorno/atestado/exame);
  retrocompat com `conduta` texto livre legado (render read-only).
- **OUT**: a cobrança de procedimento em si (F4) e o orçamento de cirurgia em si (F5) — a F3B só dispara/observa
  os gatilhos; lista de ações configurável pelo usuário (é fixa do sistema nesta versão).
- **Dependências**: F3 (procedimentos indicados ligados ao catálogo) para o item "marcar procedimento realizado".
  Independe de F1/F2 na criação das pendências, mas **F4/F5 plugam nesta fase** (por isso vem antes delas).
- **CAs**: salvar evolução com itens marcados cria pendências do tenant+paciente; modal pós-salvar lista só os
  itens marcados com link correto; painel some quando tudo concluído; criar receita conclui o item
  automaticamente; marcar procedimento realizado conclui o item; multi-tenant; RBAC por ação; audit; evolução
  permanece append-only; evolução antiga com conduta texto livre renderiza sem quebrar.
- **Riscos**: conclusão automática precisa de gancho confiável em cada ação (evento de domínio por ação);
  idempotência (salvar 2x não duplica pendência); não quebrar render do prontuário nem o `ConteudoJson` histórico.
- **Status**: implementado — `planejamentos/2026-06-10_012_financeiro-f3b-conduta-pendencias.md` (CA59–CA75). **Addendum de UX** `..._addendum.md` (CA190–CA201): o pós-save vira **widget flutuante** ancorado no canto inferior direito (estilo widget de chat, não-bloqueante) em vez do modal central; corrige o bug do fundo transparente (`var(--card)` → `hsl(var(--card))`). Sem mudança de backend.

### F4 — Cobrança de procedimento ao marcar como realizado — **M**
- **Status**: implementado e validado (QA aprovado) — `planejamentos/2026-06-10_013_financeiro-f4-cobranca-procedimento.md` (CA76–CA89) + addendum vínculo produto↔estoque `..._addendum.md` (CA90–CA96, spec gap Tipo B resolvido: `orcamento_catalogo_produto.item_inventario_id` adicionado espelhando `orcamento_catalogo_implante`). Migration `20260611011105` pendente de apply em prod.
- **Objetivo**: gerar `Cobranca` origem=Procedimento quando o procedimento é marcado como realizado, com valor
  do catálogo, **e disparar a baixa automática de estoque** (decisão fechada).
- **IN**: gatilho = item "marcar procedimento realizado" do checklist de conduta (F3B); valor do catálogo;
  **baixa automática de estoque** via `orcamento_catalogo_cirurgia_produto` + consolidação existente
  (gera `MovimentacaoEstoque` de saída, alimenta custo real da F7); pagamento via ícone OU aba (reusa F1/F2).
- **OUT**: comissão de profissional (entra na F7 — visão consolidada; ver questão aberta sobre origem do %).
- **Dependências**: F1 (Cobranca/Pagamento) + F3 (procedimento do catálogo) + F3B (gatilho "procedimento realizado").
- **CAs**: marcar procedimento realizado gera cobrança com valor do catálogo **e** baixa de estoque dos insumos
  vinculados; idempotência (marcar 2x não duplica cobrança nem baixa); multi-tenant; status/pagamento como nas
  demais; custo real registrado para a F7.
- **Riscos**: idempotência (cobrança + estoque); procedimento marcado sem produtos vinculados (baixa vazia, ok);
  estorno/desfazer "realizado" (precisa reverter cobrança e estoque com histórico — alinhar com INV-7).

### F5 — Cirurgia (orçamento pré-preenchido pela evolução + cobrança na aprovação) — **L**
- **Objetivo**: do prontuário ao orçamento à cobrança de cirurgia.
- **IN**: criar `Orcamento` pré-preenchido com os procedimentos indicados ao fechar a evolução; ao
  aprovar/imprimir, gerar `Cobranca` origem=Cirurgia (orcamento_id) com valor=total do orçamento;
  pagamento **só pela aba Financeiro do paciente**.
- **OUT**: custos/lucro detalhados em Relatórios (F7).
- **Dependências**: F1, F3, F3B (gatilho "criar orçamento" do checklist) e o aggregate `Orcamento` (existe).
- **CAs**: criar orçamento pelo checklist conclui o item da conduta e nasce com os procedimentos da evolução;
  valor do orçamento = valor cobrado; **mudança de valor do orçamento atualiza a cobrança mantendo histórico
  (de→para, quem, quando) — nada trava, valor fica pendente até pagar**; pagamento de cirurgia indisponível no
  ícone do agendamento; aba financeiro mostra os 3 tipos.
- **Riscos**: histórico de alterações de valor da cobrança (não versionar à toa, registrar o que mudou);
  recalcular saldo quando já há pagamento parcial e o valor muda.

### F6 — Convênio (estrutura base agora, avançado "em breve") **[refinar antes]** — **L**
- **Objetivo**: entregar a **estrutura base** de convênio (cadastro, marcação, campos de guia) com telas e
  schema **preparados** para o fluxo completo, deixando as partes avançadas (coparticipação, conciliação
  detalhada, glosa) sinalizadas como **"em breve"** — funcionalidade completa concluída em refinamento futuro.
  **Escopo decidido com o usuário — entregar a fundação, não a operação completa**:
- **IN** (decisão fechada — estrutura base entregue e funcional): cadastro de **convênios/planos**;
  **carteirinha no paciente** (número + validade + plano); **marcar convênio no check-in** (Cobrança nasce
  tipo=Convenio, convenio_id); **campos de guia / autorização** no atendimento (registrar nº de guia/senha).
  Permissão `convenios.*` já existe no catálogo. Schema e telas das partes avançadas ficam **preparados**
  (campos/abas presentes), mas a operação completa não opera ainda.
- **OUT — "em breve" (estrutura preparada, funcionalidade em refinamento futuro)**: **coparticipação do paciente**
  (cálculo do valor cobrado zero vs. coparticipação), **conciliação detalhada do repasse** (lançar o que a
  operadora pagou contra o faturado) e **registro/tratamento de glosa** (valor glosado + motivo) — telas e
  estrutura de dados nascem preparadas, exibidas como **"em breve"**, com a lógica concluída depois.
- **OUT — anti-escopo duro (não nesta fase nem na evolução imediata)**: **geração de XML TISS / faturamento em
  lote automatizado**; elegibilidade online; integração com integradora; recurso de glosa estruturado. A
  explicação didática do fluxo TISS (abaixo) permanece como insumo para a evolução futura.
- **Dependências**: F1 (Cobranca já guarda tipo+convenio_id desde a F1).

> #### Explicação didática — como funciona convênio no Brasil (insumo para o refinamento)
> 1. **Cadastro de convênios e planos**: a clínica cadastra cada operadora (ex.: Unimed, Bradesco
>    Saúde) e seus planos. Cada convênio tem regras próprias de cobertura, tabela de preços
>    (TUSS/CBHPM) e prazos.
> 2. **Elegibilidade / carteirinha**: na chegada do paciente, confere-se se a carteirinha está ativa
>    e o procedimento é coberto. Algumas operadoras oferecem verificação online; outras, por telefone/portal.
> 3. **Autorização e guias TISS**: o padrão **TISS** (Troca de Informações em Saúde Suplementar, da ANS)
>    define guias eletrônicas. As principais: **Guia de Consulta** (consulta simples) e **Guia SP/SADT**
>    (Serviço Profissional / Serviço Auxiliar de Diagnóstico e Terapia — para procedimentos/exames).
>    Procedimentos eletivos costumam exigir **autorização prévia** da operadora (senha).
> 4. **Atendimento**: realiza-se o serviço; registram-se procedimentos (códigos TUSS), materiais e
>    medicamentos usados.
> 5. **Faturamento em lote / XML TISS**: ao fim do período (geralmente mensal), as guias são agrupadas
>    em um **lote** e exportadas em **arquivo XML no padrão TISS**, conforme a versão exigida pela operadora.
> 6. **Envio**: o lote XML é enviado pelo portal da operadora (ou via integradora). Gera-se protocolo.
> 7. **Glosas**: a operadora analisa e pode **glosar** (recusar/reduzir) itens — por falta de autorização,
>    código inválido, divergência de valor etc. O que é glosado não é pago.
> 8. **Recurso de glosa**: a clínica pode recorrer das glosas indevidas, reapresentando justificativa/documentação.
> 9. **Recebimento do repasse**: a operadora paga o lote (menos glosas) num prazo contratual (ex.: 30-90 dias).
>    Esse **repasse** é a Receita efetiva do convênio — entra no financeiro conciliado contra o que foi faturado.
>
> **Implicação para o produto**: o "valor cobrado" do paciente em convênio frequentemente é zero ou só
> **coparticipação**; a receita real vem do repasse, com defasagem e risco de glosa. Por isso convênio
> exige modelagem própria (lote, guia, glosa, conciliação) — não cabe na Cobrança particular. A F6 decidirá
> quanto desse fluxo entra agora vs. depois.

### F7 — Consolidação (redesign do `/financeiro` + custos/lucro/comissão + caixa diário) — **XL**
- **Objetivo**: substituir a `FinanceiroView.vue` **temporária** por uma tela definitiva e entregar a
  visão consolidada: lucro detalhado por paciente, **comissão por profissional** e **caixa diário/fechamento**.
- **IN**:
  - Redesign do `/financeiro` do zero (mantendo `Lancamento` como base de dados).
  - Relatórios detalhados por paciente (custo via estoque, pagamento, lucro, desconto, juros, taxa de cartão)
    reusando a aba Financeiro de Relatórios e o export CSV (F1 item 1.5 já entregue); vínculo
    procedimento↔produto↔estoque alimentando o **custo real** (baixa automática vinda da F4/F5).
  - **Comissão de profissional** descrita no financeiro (decisão fechada): quem deve receber, lucro de cada um.
    Para **cirurgia** a comissão sai do `OrcamentoEquipe` (já existe). Para **consulta e procedimento** (fora do
    orçamento), a comissão é **configurada na área de Equipe, por profissional** (`ConfigComissaoProfissional`,
    §2.1), com **valor default do sistema** refletindo o mais comum do mercado, **editável** por quem tem
    permissão (normalmente o Dono). **Default de mercado proposto a validar no briefing** (§5 q9):
    **30% sobre o valor da consulta/procedimento** para o profissional executante — patamar mais citado no
    mercado de saúde para clínicas que repassam por produção (faixa usual de mercado: 20%–50%, com 30% como
    ponto médio de partida); cirurgia segue o que estiver no `OrcamentoEquipe`. O default é só ponto de partida
    editável — nenhuma regra complexa (faixas/metas/split) entra nesta versão (anti-escopo §6).
  - **Caixa diário / fechamento** (decisão fechada: precisa existir) — rotina de abrir/fechar caixa com extrato
    do dia sobre `Lancamento`. **Fechamento é por estabelecimento/unidade** (decisão fechada §5 q11): o caixa
    pertence ao estabelecimento ativo; dados de fechamento de um estabelecimento **jamais** são visíveis a outro,
    mesmo para o mesmo usuário com múltiplos vínculos — o contexto é sempre o **estabelecimento ativo**.
    **Quem pode fechar/reabrir** (qual papel/permissão) permanece a definir no briefing (§5 q11).
- **OUT**: NF, gateway, conciliação bancária (anti-escopo §6).
- **Dependências**: F1-F6 (cobrança/pagamento/estorno/custo/comissão/convênio já fluindo).
- **CAs**: `/financeiro` redesenhado lê de `Lancamento`; relatório por paciente mostra custo/pagamento/lucro;
  comissão por profissional consolidada (cirurgia via `OrcamentoEquipe`; consulta/procedimento via
  `ConfigComissaoProfissional` da Equipe, com default editável); caixa diário **por estabelecimento** abre/fecha
  com extrato; **fechamento de um estabelecimento nunca aparece para outro, nem para o mesmo usuário em outro
  vínculo** (contexto = estabelecimento ativo); multi-tenant; LGPD (relatório agregado sem PII desnecessária;
  detalhe por paciente é auditado).
- **Riscos**: não quebrar o que já lê `Lancamento` (Relatórios atuais); custo real depende da baixa automática de
  estoque (F4/F5); definir papel/permissão de fechar-reabrir caixa antes de cravar schema; validar o default de
  comissão de mercado com o usuário no briefing.

### F8 — Recibo de pagamento (PDF interno) — **S**
- **Status**: **implementado** — `planejamentos/2026-06-10_015_financeiro-f8-recibo-pdf.md` (CA118–CA130); backend 0 erros build/1499 testes verdes (+14 novos); frontend build verde/757 testes verdes (+10 novos); migration pendente com `imedto-database` (`ALTER TABLE pagamentos ADD COLUMN IF NOT EXISTS recibo_emitido_em timestamptz NULL`).
- **Objetivo**: emitir, após o pagamento, um **recibo em PDF** (documento **interno**, sem valor fiscal) para
  entregar ao paciente. Decisão fechada (§5 q4): SIM, emitir após o pagamento.
- **Decisão de schema (fechada)**: `Pagamento.recibo_emitido_em timestamptz NULL` — gravado **apenas na 1ª emissão** (idempotente; reemissões livres). Migration: `imedto-database` executa `ALTER TABLE pagamentos ADD COLUMN IF NOT EXISTS recibo_emitido_em timestamptz NULL`.
- **IN**: ação "Emitir recibo" sobre um `Pagamento` quitado — porta 1: `PaymentModal.vue` (quando cobrança está `Paga`); porta 2: linha de pagamento em `FinanceiroTab.vue` (botão oculto se `estornado`). Geração de PDF no servidor com QuestPDF + Nunito, reutilizando `QuestPdfReceitaService.InicializarQuestPdf()` (mesma fundação). Recibo traz: cabeçalho institucional, nome do paciente, valor pago, forma de pagamento, parcelas, data, quem registrou, origem/descrição da cobrança, rótulo "RECIBO — documento sem valor fiscal".
- **OUT**: NFS-e / qualquer documento fiscal (F9); recibo de cobrança ainda não paga (recibo é de **pagamento**).
- **Dependências**: F1 (Pagamento existe) e F2 (aba Financeiro). Pode entrar em paralelo após F1/F2.
- **CAs entregues**: CA118 (botão por porta), CA119 (chama endpoint), CA120 (oculto/422 para estornado), CA121 (não encontrado → genérico), CA122 (download blob no front), CA123 (PDF válido), CA124 (multi-tenant falha-fechada), CA125 (sem PII clínica), CA126 (nome arquivo sem PII), CA127 (audit best-effort), CA128 (flag 1ª emissão idempotente), CA129 (loading/erro no front), CA130 (rótulo sem valor fiscal).
- **Riscos mitigados**: rótulo explícito "sem valor fiscal" no PDF; reuso fiel do pipeline QuestPDF + Nunito existente.

### F9 — NFS-e (Nota Fiscal de Serviço Eletrônica via gateway) **[refinar antes]** — **XL**
- **Objetivo**: emitir **documento fiscal** (NFS-e) a partir de um atendimento pago, via **provedor externo**
  (gateway), com transmissão à prefeitura, retorno de chave + XML + DANFSE, cancelamento e consulta de status.
  Decisão fechada (§5 q4): a NF entra **neste épico** como fase própria, amarrada ao discovery já pronto.
- **Natureza**: **bounded context novo `Faturamento`** — NÃO mistura com Cobrança/Pagamento/Agendamento
  (recomendação do discovery). Agregados: `LancamentoFaturavel`, `NotaFiscal` (estado-máquina
  Rascunho→EmTransmissao→Autorizada→Cancelada/Rejeitada), `EmissorFiscal` (config por estabelecimento).
- **IN** (recorte MVP do discovery): **só NFS-e** (cobre ~95% do faturamento de saúde); emissão a partir de um
  lançamento pago; configuração de emissor **por estabelecimento** (CNPJ, regime, certificado, série, alíquota
  ISS, código de serviço LC 116/2003 item 4); **opt-in por estabelecimento e por tipo de serviço** (nem toda
  clínica emite — MEI/contador externo); disparo **manual** (tela "lançamentos a faturar") com opção de
  automatizar por configuração; cancelamento dentro do prazo legal; consulta de status; download de XML/DANFSE;
  abstração `INfsEmissaoGateway` (uma interface, provedor plugável); Outbox + retry + idempotência por lançamento.
- **OUT** (anti-escopo do discovery): NF-e modelo 55 (produtos), NFC-e modelo 65, carta de correção/substituição,
  apuração tributária/SPED, certificado A3 (só A1 — custódia delegada ao provedor), integração direta com
  municípios (só via gateway). Reforma tributária IBS/CBS fica no road map do provedor.
- **Dependências**: F1 (lançamento pago é a origem do faturável). **Exige refinamento próprio + POC de provedor
  antes do briefing** (escolher gateway, validar custódia de certificado e cobertura municipal da base).
- **CAs**: emissão idempotente (clicar 2x não duplica NF / 1 NF ativa por lançamento); descrição **nunca** contém
  CID/diagnóstico (texto controlado "Consulta em <especialidade>", validado no backend); XML/PDF em storage com
  isolamento por estabelecimento + audit de download; opt-in respeitado (não emite sem config de emissor);
  cancelamento fora do prazo → 422 de negócio; multi-tenant (emissor e NFs só do tenant); LGPD (NFs do paciente
  incluídas no export LGPD onde ele é tomador).
- **Riscos**: gateway externo fora do ar (Outbox + alerta); certificado expira (alerta D-30/15/7); numeração de
  RPS sem gap/duplicação (centralizada no `EmissorFiscal`, lock — nunca no front); custo por NF na margem
  (unit economics); SP híbrido (gateway absorve). Detalhe completo em `Docs/Discoverys/nota-fiscal/01_discovery.md §7`.

### Ordem recomendada
`F1 → F2`; **F3 em paralelo** desde já; **`F3B` após F3** (cria os gatilhos que F4/F5 consomem);
`F4` (após F1 + F3B); `F5` (após F1 + F3B); **`F8` (recibo) em paralelo após F1/F2** (é pequena e independente);
`F6` após refinamento próprio (estrutura base + "em breve"); `F7` consolida tudo (comissão, caixa, custo real,
redesign); **`F9` (NFS-e) por último** — bounded context próprio, exige refinamento + POC de provedor, não
bloqueia nenhuma fase anterior.

> **Por que a F3B vem entre F3 e F4**: o checklist de conduta é a fonte dos gatilhos "marcar procedimento
> realizado" (F4 + baixa de estoque) e "criar orçamento" (F5). Sem ela, F4/F5 não teriam o ponto de partida
> operacional no prontuário. A criação das pendências em si independe do financeiro (paralelizável com F1/F2),
> mas o sequenciamento a posiciona logo após F3 porque depende do procedimento ligado ao catálogo.

---

## 5. Questões abertas para o próximo refinamento

> Decisões tomadas em 2026-06-10 ficam marcadas **[RESOLVIDA]** com a decisão registrada (servem de histórico
> para o briefing da fase). As questões que **ainda** precisam de refinamento ficam marcadas **[ABERTA]**.

**F1 (Fundação/Consulta)**
1. **[RESOLVIDA]** Taxa de cartão: **configurada por forma de pagamento**, numa **aba de configuração do
   Financeiro** (`ConfigTaxaFormaPagamento`, §2.4). Aplicada automaticamente ao registrar o pagamento —
   **nunca informada manualmente a cada pagamento**.
2. **[RESOLVIDA]** Quem pode dar desconto (INV-8): quem tiver **permissão de aprovar orçamento** OU **permissão
   de financeiro** OU for **Dono**. Validado no backend. Teto por papel: fora desta versão (anti-escopo §6).
3. **[RESOLVIDA]** Estorno de pagamento (INV-7): **sempre com histórico** — gera `EstornoPagamento` +
   `Lancamento` de estorno na mesma transação; **nunca apaga nem edita** o pagamento original. Cancelar
   cobrança paga = estornar os pagamentos. Status recalculado sobre a soma líquida (pagamentos − estornos).
4. **[RESOLVIDA]** Recibo para o paciente: **SIM, emitir após o pagamento** — vira **F8** (recibo em PDF,
   documento **interno**, sem valor fiscal, reusa pipeline de PDF do servidor). Distinto da **NFS-e** (documento
   fiscal via provedor — **F9**). Recibo e NF são coisas separadas e posicionadas em fases próprias (ver §F8/§F9).
5. **[RESOLVIDA]** Arredondamento: **2 casas decimais, half-away-from-zero** (`MidpointRounding.AwayFromZero`),
   em um único helper de domínio, **sempre no backend** (§1 princípio 1).

**F3B (Conduta / pendências)**
6. **[RESOLVIDA]** **Lista do checklist de conduta**: lista canônica **confirmada exatamente como proposta** —
   criar receita · criar atestado · pedir exame · criar orçamento · marcar procedimento realizado · agendar
   retorno. É **fixa do sistema** (não configurável) nesta versão.

**F4 (Procedimento)**
7. **[RESOLVIDA]** Baixa automática de estoque: **SIM** — ocorre ao **marcar o procedimento como
   realizado/completado** pela evolução (checklist de conduta), via `orcamento_catalogo_cirurgia_produto` +
   consolidação existente. Alimenta o **custo real** da F7. Substitui a baixa manual para esse caso.

**F7 (Comissão / caixa)**
8. **[RESOLVIDA — escopo]** Comissão de profissional: **entra no escopo deste épico, na F7** (visão
   consolidada — quem recebe, lucro de cada um). Para **cirurgia**, sai do `OrcamentoEquipe` (já existe).
9. **[RESOLVIDA]** **Origem da comissão de consulta e de procedimento** (fora do orçamento): **config na área de
   Equipe, por profissional** (`ConfigComissaoProfissional`), com **valor default do sistema** (mais comum do
   mercado), **editável** por quem tem permissão (Dono). **Default de mercado proposto: 30% sobre o valor da
   consulta/procedimento** (faixa usual 20%–50%; 30% como ponto médio) — a **validar no briefing da F7**.
   Cirurgia continua via `OrcamentoEquipe`.
10. **[RESOLVIDA — existe]** Caixa diário / fechamento: **precisa existir** (entra na F7).
11. **[RESOLVIDA — parcial]** Fechamento de caixa: **por estabelecimento/unidade** (decisão fechada). Reforço
    multi-tenant: fechamento de um estabelecimento **jamais** visível a outro, mesmo para o mesmo usuário com
    múltiplos vínculos (contexto = estabelecimento ativo). **Ainda ABERTA:** **qual papel/permissão pode
    fechar/reabrir** o caixa — definir no briefing da F7.

**F5 (Cirurgia)**
12. **[RESOLVIDA]** Mudança de orçamento após a cobrança gerada: a **cobrança acompanha o orçamento mantendo
    histórico** das alterações de valor. Até ser paga, é apenas valor **pendente** — nada trava, nada se perde.

**F6 (Convênio)**
13. **[RESOLVIDA — abordagem]** **Estrutura base agora, avançado "em breve"**: entregar cadastro de
    convênios/planos + carteirinha no paciente + marcar convênio no check-in + **campos de guia/autorização**,
    com schema/telas **preparados** para as partes avançadas. **Gerar XML TISS / lote automatizado = anti-escopo
    duro** (evolução futura).
14. **[RESOLVIDA — escopo]** **Coparticipação, conciliação detalhada do repasse e registro de glosa** ficam como
    **"em breve"**: estrutura de dados e telas nascem preparadas/sinalizadas, funcionalidade completa concluída em
    refinamento futuro. O refinamento dedicado da F6 detalha cada uma (valor cobrado zero vs. coparticipação,
    estrutura mínima da conciliação, registro de glosa).

**F8 (Recibo) / F9 (NFS-e)**
15. **[RESOLVIDA — escopo]** Recibo (F8) e NFS-e (F9) **entram no épico** como fases próprias e distintas:
    recibo = PDF interno sem valor fiscal; NFS-e = documento fiscal via gateway, bounded context `Faturamento`.
16. **[ABERTA — NOVA, F9]** **Escolha do provedor de NFS-e**: o discovery recomenda **POC com Focus NFe**
    (preço transparente R$ 89,90/mês + R$ 0,10/NF, sandbox imediato, SLA 99,99%) e **fallback NFE.io** (cobertura
    nacional, webhook obrigatório, cliente healthtech). **Wildcard Nota Gateway** (iClinic/Doctoralia já usam) —
    pede call comercial. Decisão final exige **POC antes do briefing da F9** (ver `nota-fiscal/02_pesquisa_mercado.md`).
17. **[ABERTA — NOVA, F9]** **Custódia do certificado digital**: discovery recomenda **delegar ao provedor**
    (só A1, descartar A3) para reduzir superfície LGPD/fiscal — confirmar com o gateway escolhido na POC.
18. **[ABERTA — NOVA, F9]** **Modelo de cobrança da NF** (custo do gateway): feature paga, inclusa no plano ou
    repassada? Define se vamos atrás do menor preço/NF ou do melhor SLA. Também **a definir**: quais municípios da
    base são prioritários (define a cobertura mínima do gateway) — consultar distinct de cidades dos estabelecimentos.

---

## 6. O que NÃO fazer (anti-escopo)

A menos que o usuário peça explicitamente depois:
- ~~Sem emissão de Nota Fiscal (NFS-e)~~ **(revisado 2026-06-10)** — a NFS-e **entrou no épico como F9**
  (bounded context `Faturamento`, via gateway). Continua **fora**: **NF-e modelo 55 (produtos)**, **NFC-e
  modelo 65**, carta de correção/substituição, apuração tributária/SPED, integração direta com municípios,
  certificado A3 — tudo anti-escopo da F9 (recorte MVP do discovery = só NFS-e).
- **Sem gateway de pagamento / cobrança online** (PIX dinâmico, link de pagamento, cartão online, boleto registrado).
- **Sem conciliação bancária automática** (OFX, extrato bancário, integração com banco).
- **Sem antecipação de recebíveis / split de pagamento de adquirente.**
- **Sem TISS XML/lote** — convênio na F6 é versão simples (cadastro + carteirinha + guia/autorização +
  conciliação manual + glosa); gerar XML/lote automatizado é evolução futura (sub-fase própria).
- **Sem teto de desconto por papel** nesta versão (só RBAC de quem pode dar — INV-8).
- **Sem comissão por regra complexa** (faixas, metas, split avançado) — F7 entrega a visão consolidada
  básica; origem da comissão de consulta/procedimento ainda em aberto (§5 q9).
- **Sem relatório fiscal/contábil** (DRE, regime de competência sofisticado) — fluxo de caixa simples sobre `Lancamento`.
- **Sem multi-moeda.**

---

## 7. Referências de código (já mapeado)

| Conceito | Arquivo real |
|---|---|
| Financeiro base | `backend/.../Domain/Financeiro/Lancamento.cs`, `FormaPagamento.cs`, `CategoriaFinanceira` |
| View financeiro (temporária) | `frontend/src/views/.../FinanceiroView.vue` |
| Check-in | `frontend/src/components/agenda/CheckInModal.vue`, `POST /api/agendamentos/{id}/check-in` |
| Card do agendamento | `frontend/src/components/agenda/AgendamentoRow.vue` |
| Procedimentos indicados | `frontend/src/components/prontuario/secoes/SecaoProcedimentosIndicados.vue` |
| Conduta (hoje texto livre) | seção `key="conduta"` em `frontend/src/components/ui/modeloProntuarioBuilder.ts` (catálogo das 17 seções), renderizada por `SecaoProntuario.vue` dentro de `tabs/ConsultaAtualTab.vue` |
| Config do orçamento (taxa/juros por forma) | `frontend/src/views/orcamentos/OrcamentoSettingsView.vue` — padrão a estender para a aba de config do Financeiro |
| Orçamento | `Domain/.../Orcamento`, `OrcamentoCalculadora`, `OrcamentoEquipe` (comissão de cirurgia), `OrcamentoSettingsView.vue`, `orcamento_catalogo_cirurgia*` |
| Permissões | `backend/.../Domain/ModelosPermissao/CatalogoPermissoes.cs` (área `convenios` já existe) |
| Página do paciente | `frontend/src/views/.../PacienteDetalheView.vue` (abas Financeiro/Convênios = empty state) |
| Audit LGPD | `paciente_acesso_log` (relatório de acessos — F1 item 1.8) |
| Estoque | `ItemInventario` (`custo_medio`), `MovimentacaoEstoque` |
| Equipe (config de comissão F7) | área de profissionais vinculados ao estabelecimento — onde mora `ConfigComissaoProfissional` |
| PDF do servidor (recibo F8) | `frontend/.../usePdfHeader.ts` + pipeline de PDF oficial de receita — a estender para o recibo |
| NFS-e (F9) — discovery | `Docs/Discoverys/nota-fiscal/01_discovery.md` (arquitetura, `INfsEmissaoGateway`, Outbox, LGPD) + `02_pesquisa_mercado.md` (gateways: Focus NFe POC, NFE.io fallback) |
| **Protótipo visual (Claude Design)** | [`prototipacao-financeiro/design-handoff/`](prototipacao-financeiro/design-handoff/) — telas navegáveis + screenshots das F1/F2/F3/F3B/F6/F7/F8, mapeadas por fase no README. Referência visual obrigatória nos briefings dessas fases. |
