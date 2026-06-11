# Financeiro F7 — Consolidação (redesign do /financeiro + caixa diário + comissões + custo/lucro)

**ID**: 2026-06-11_001
**Status**: Aprovado por usuário em 2026-06-11 (decisões abertas resolvidas pelo orquestrador com autorização do usuário; demais decisões já fechadas no plano mestre)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G (fase XL final do épico — 4 frentes: redesign de view, caixa diário, comissões, custo/lucro)
**Áreas regressivas tocadas**: financeiro (view /financeiro substituída), relatório (aba Financeiro de Relatórios estendida), permissionamento (RBAC caixa/comissão), estoque (leitura de custo via movimentações), orçamento (leitura de OrcamentoEquipe)

---

## 1. Contexto e motivação

O épico Financeiro/Cobranças (F1–F8) já entregou a espinha dorsal: `Cobranca`/`Pagamento`/`EstornoPagamento`, geração atômica de `Lancamento`, baixa automática de estoque no procedimento realizado (F4), cobrança de cirurgia na aprovação do orçamento (F5), recibo PDF (F8) e estrutura base de convênio (F6). Falta o que **dá sentido consolidado** a tudo isso para o **dono/administrador**: uma tela `/financeiro` definitiva, o **ritual diário de conferência de caixa**, a visão de **quanto repassar a cada profissional** e o **lucro real por paciente** (cobrado − custo de insumo).

Hoje `FinanceiroView.vue` é uma tela **temporária** de CRUD de `Lancamento` (criar/editar/pagar/cancelar lançamento avulso), sem KPIs de período, sem links para paciente/cobrança, sem caixa, sem comissão. Ela precisa ser **substituída do zero** por uma tela de 4 abas (Extrato, Caixa diário, Comissões, Configurações), conforme o protótipo `Financeiro.html`/`ClinicFinanceTabs.jsx` validado com o usuário.

Dor concreta que isso resolve:
- **Dono**: "no fim do dia, quanto entrou em cada forma de pagamento? Quanto vou repassar de comissão? Qual paciente deu prejuízo (custo de insumo > cobrado)?"
- **Recepção**: "abri o caixa de manhã, confiro à noite e fecho — selo de conferência do dia."

Esta é a **fase final do épico** (F9/NFS-e é bounded context próprio à parte). Princípio não-negociável herdado: cálculo monetário sempre no backend (`decimal`, helper único de arredondamento `ArredondamentoMonetario`), multi-tenant em camadas, LGPD (dado financeiro do paciente é sensível).

---

## 2. Persona-alvo

- **Dono / administrador** (persona primária): abre `/financeiro` diariamente/semanalmente para acompanhar fluxo de caixa, decidir repasses de comissão e avaliar lucratividade por paciente/procedimento. Configura o % de comissão por profissional na Equipe.
- **Recepção** (persona secundária): opera o **caixa diário** — abre de manhã, confere o resumo por forma de pagamento, fecha à noite. Tem `financeiro.ver` + (decisão) `financeiro.fechar`.
- **Profissional**: não é persona desta tela (a comissão é visão do gestor; o profissional não acessa `/financeiro` salvo se tiver `financeiro.ver`).

Momento da jornada: **pós-consulta / pós-cobrança** (consolidação) e **ritual diário** (caixa).

---

## 3. Escopo

### Inclui
1. **Redesign de `/financeiro` do zero** — substituir `FinanceiroView.vue` por uma tela de 4 abas (Extrato, Caixa diário, Comissões, Configurações), lendo de `Lancamento` (+ joins de cobrança/paciente onde a origem é pagamento). KPIs e agregações **calculados no backend** (Dapper agregado — nunca trazer lançamentos para somar no front).
2. **Caixa diário** — entidade `CaixaDiario` (abrir/fechar por estabelecimento, resumo do dia por forma de pagamento + estornos, selo read-only quando fechado, reabrir por Dono com audit).
3. **Comissões** — entidade `ConfigComissaoProfissional` (% por profissional, default de sistema 30%) + UI de config na área de **Equipe** (no `ProfissionalDetalhesModal.vue`, aba Perfil) + aba Comissões no `/financeiro` (tabela por período, regime caixa, cirurgia via `OrcamentoEquipe`).
4. **Custo/lucro por paciente nos Relatórios** — estender a aba Financeiro de `RelatoriosView.vue` (`RelatorioFinanceiroTab.vue`) com uma visão por paciente (cobrado, pago, desconto, taxa, custo de insumo, lucro), com drill-down auditado. Reusa export CSV existente.

### Não inclui (anti-escopo §6 do plano mestre — reforçado)
- NFS-e / qualquer documento fiscal (F9, bounded context próprio).
- Gateway de pagamento, link de pagamento, PIX dinâmico, boleto registrado.
- Conciliação bancária automática (OFX, extrato bancário).
- DRE / relatório contábil / regime de competência sofisticado (o caixa aqui é fluxo de caixa simples sobre `Lancamento`).
- Multi-moeda.
- **Comissão complexa**: faixas, metas, split avançado, `valor_fixo` por profissional (só **percentual** nesta versão). Comissão **não gera `Lancamento` de despesa automático** — é relatório/visão.
- Teto de desconto por papel (já era anti-escopo da F1).
- **Lançamento contábil de comissão / fechamento financeiro de competência** — o caixa é snapshot/ritual, não trava operação.

---

## 4. Regras de negócio

### Bloco A — Extrato (KPIs + lançamentos)

- **R1** — **KPIs do período calculados no backend** sobre `Lancamento` do estabelecimento ativo, filtrados pelo intervalo de datas escolhido (Hoje/Semana/Mês/Personalizado):
  - **Recebido** = `SUM(valor)` de lançamentos `Tipo=Receita` `Status=Pago` **incluindo os negativos de estorno** (o estorno abate o recebido — INV de estorno já grava `Lancamento` de valor negativo, categoria `"Estorno: Pagamento"`). Ou seja: recebido líquido.
  - **A receber** = `SUM(valor)` de `Tipo=Receita` `Status=Pendente`.
  - **Despesas** = `SUM(valor)` de `Tipo=Despesa` `Status=Pago`.
  - **Saldo** = Recebido − Despesas.
  - **Secundários**: **Descontos concedidos** = `SUM(cobrancas.desconto)` das cobranças com pagamento no período; **Taxas de cartão** = `SUM(pagamentos.taxa)` dos pagamentos do período; **Estornos** = `SUM(ABS(valor))` dos lançamentos categoria `"Estorno: Pagamento"` no período.
  - Mora em: Query (Dapper agregado em `FinanceiroQueryRepository` ou novo repo de consolidação). Validada em: back (agregação) + front (apenas exibe).
- **R2** — **Tabela de lançamentos** do extrato lista `Lancamento` do tenant no período, paginada (reusa o padrão de `ListarLancamentosQuery`), com colunas: data, descrição, paciente (quando origem é pagamento — via `cobranca_id` → `paciente_id`), categoria, forma de pagamento, valor (com sinal/cor: entrada verde, saída vermelha, estorno vermelho). Mora em: Query. Validada em: back.
- **R3** — **Link para paciente/cobrança**: quando o lançamento tem `cobranca_id` (origem = pagamento de cobrança), a linha exibe link para a página do paciente (aba Financeiro). Lançamento avulso (sem `cobranca_id`) não tem link. Mora em: Query (traz `paciente_id`+`paciente_nome` no DTO só quando há cobrança) + Front (renderiza link condicional). LGPD: nome do paciente no DTO **só** quando há vínculo de cobrança e o usuário tem `financeiro.ver` (a visão agregada já pressupõe esse acesso).
- **R4** — **Filtros do extrato**: tipo (Receita/Despesa), categoria, forma de pagamento, origem (Consulta/Procedimento/Cirurgia/Avulso/Despesa). Aplicados no backend (WHERE), nunca filtragem client-side de página. Debounce não se aplica (são selects, não busca textual) — recarrega ao mudar select. Mora em: Query. Validada em: back.
- **R5** — **CRUD de lançamento avulso preservado**: o botão "Lançamento" (novo lançamento manual de Receita/Despesa) e as ações pagar/cancelar/editar **continuam existindo** na aba Extrato (reusa `financeiroService.criar/atualizar/pagar/cancelar` — endpoints atuais inalterados). RBAC: criar/editar/pagar/cancelar exige `financeiro.lancar`; só ver exige `financeiro.ver`. Mora em: Handler existente (sem mudança) + Front (gate de botão por `financeiro.lancar`).

### Bloco B — Caixa diário

- **R6** — **`CaixaDiario` é por estabelecimento + data**: 1 caixa por (`estabelecimento_id`, `data`). Estados: **NãoAberto** (não existe registro do dia → tela mostra CTA "Abrir caixa"), **Aberto**, **Fechado**. Mora em: Domain (`CaixaDiario` aggregate) + Handler. Validada em: back (índice UNIQUE `(estabelecimento_id, data)`).
- **R7** — **Abrir caixa**: cria `CaixaDiario` status=Aberto, grava `aberto_por_usuario_id`+`aberto_em`. Só um por dia (tentar abrir 2x no mesmo dia → 422 "Caixa do dia já está aberto" ou no-op idempotente retornando o existente). Mora em: Handler. Validada em: back.
- **R8** — **Resumo do dia** (lido on-the-fly, não materializado): agregação dos `Lancamento` `Status=Pago` com `DataPagamento = data` do caixa, **agrupados por forma de pagamento** (Dinheiro/PIX/Crédito/Débito/Boleto/…), mais **Estornos** (soma dos negativos do dia) e **Total do dia** (soma líquida). O resumo é **sempre calculado sobre os lançamentos reais** — o caixa não materializa snapshot de valores (evita divergência); fechar apenas **congela o status/selo**, não os números. Mora em: Query (Dapper agregado). Validada em: back.
- **R9** — **Fechar caixa**: muda status para Fechado, grava `fechado_por_usuario_id`+`fechado_em`+`observacao?` (opcional). Após fechado, a aba mostra resumo **read-only com selo "Somente leitura"** e o botão "Fechar caixa" some. Mora em: Handler + Domain (`CaixaDiario.Fechar`). Validada em: back (422 se já fechado).
- **R10** — **Reabrir caixa**: permitido **apenas para o Dono** (não basta `financeiro.fechar`), com `audit` da reabertura (registrar quem reabriu/quando — campo `reaberto_por_usuario_id`+`reaberto_em`, e zera `fechado_*` ao reabrir, ou mantém histórico mínimo — ver R10.1). Mora em: Handler + Domain (`CaixaDiario.Reabrir`). Validada em: back (403/422 se não for Dono).
  - **R10.1** — Implementação de histórico de reabertura: manter `fechado_por`/`fechado_em` da **última vez que foi fechado** e adicionar `reaberto_por_usuario_id?`+`reaberto_em?` (último reabrir). Não versionar o ciclo inteiro abrir/fechar/reabrir (anti-escopo — DRE/auditoria contábil fica fora). A reabertura volta o status para Aberto.
- **R11** — **Lançamento retroativo em dia fechado NÃO é bloqueado** (decisão): o caixa é um **snapshot/ritual de conferência**, não uma trava operacional. Criar/pagar/estornar um lançamento cuja data caia num dia já fechado é permitido normalmente — o resumo do dia (R8, calculado on-the-fly) refletirá o novo valor se a aba for reaberta para leitura. Bloquear criaria fricção operacional enorme (recepção não pode parar de registrar porque alguém fechou o caixa). Mora em: ausência de regra (não adicionar bloqueio). Validada em: back (CA confirma que não trava) + nenhum gate de front.
- **R12** — **Isolamento multi-tenant ABSOLUTO** (reforço §5 q11): o `CaixaDiario` pertence ao estabelecimento ativo. Dados de caixa de um estabelecimento **jamais** são visíveis a outro, **mesmo para o mesmo usuário com múltiplos vínculos** — o contexto é sempre o **estabelecimento ativo** (claim de tenant). Repositório falha-fechada (sem tenant claim → vazio/throws). Mora em: Query/Handler (todo SELECT/comando filtra `estabelecimento_id`). Validada em: back. **CA obrigatório com dois estabelecimentos.**
- **R13** — **RBAC do caixa** (decisão do orquestrador, autorizada pelo usuário): **abrir/fechar caixa exige `financeiro.fechar`** (ação **já existente** no `CatalogoPermissoes.cs` — área `financeiro` tem `{ "ver", "lancar", "fechar" }`). O **Dono sempre tem todas as permissões** (AdminPadrao = Todas), então "Dono OU quem tem permissão de financeiro" é satisfeito por `financeiro.fechar`. **Reabrir** exige ser **Dono** (mais restrito que fechar — R10). Mora em: Controller (`[RequiresAcao("financeiro","fechar")]`) + Domain (Dono check na reabertura). Validada em: back (403/422) + front (botão oculto sem permissão).
  - **Nota de decisão**: nenhuma ação nova no catálogo. `financeiro.fechar` já existe e estava reservada exatamente para isso. Adicionar uma `financeiro.fechar_caixa` seria duplicação. Espelhar a chave no front em `frontend/src/constants/permissions.ts` (PERMISSION_AREAS) se ainda não estiver.

### Bloco C — Comissões

- **R14** — **`ConfigComissaoProfissional`**: por (`estabelecimento_id`, `profissional_usuario_id`, `tipo` ∈ {Consulta, Procedimento}), guarda `percentual` (decimal, 0–100). Só **percentual** (sem `valor_fixo` nesta versão — anti-escopo). Mora em: Domain + Handler. Validada em: back (0 ≤ percentual ≤ 100; tenant; profissional vinculado ao estabelecimento).
- **R15** — **Default de sistema 30%** (decisão §5 q9, proposta aplicada): quando não há `ConfigComissaoProfissional` para um (profissional, tipo), aplica-se **30%** sobre o valor da consulta/procedimento. O default é só ponto de partida — editável por quem tem permissão. O default **não é persistido** por profissional (é o fallback no cálculo); editar grava uma linha de config explícita. Mora em: Domain (constante `ComissaoConfig.PercentualPadrao = 30m`) + Query (COALESCE com 30 no cálculo). Validada em: back.
- **R16** — **UI de config de comissão na Equipe**: no `ProfissionalDetalhesModal.vue`, aba **Perfil** (onde já se edita profissão/especialidade do vínculo), adicionar campo(s) de % de comissão por tipo (Consulta / Procedimento) do profissional **no estabelecimento ativo**. Mostra o default (30%) quando não configurado (placeholder/hint "padrão do sistema: 30%"). RBAC para editar: **Dono** (a edição de comissão é decisão de gestão — espelha o acesso já restrito ao Dono da view Equipe). Mora em: Front (campo) + Handler (gate Dono). Validada em: back + front.
- **R17** — **Aba Comissões no `/financeiro`** — tabela por período (mesmo seletor Hoje/Semana/Mês/Personalizado), uma linha por profissional com: nome+especialidade, **atendimentos** (qtd no período), **faturamento** (base de cálculo), **% comissão** (badge), **valor a repassar**. Linha **expansível** mostra o detalhe por atendimento (data, paciente, procedimento/consulta, base, faturamento, comissão). Topo mostra **"Total a repassar"**. Mora em: Query (Dapper agregado). Validada em: back.
- **R18** — **Base de cálculo da comissão = regime caixa** (decisão §F7 / proposta aplicada): a comissão é calculada sobre **pagamentos RECEBIDOS no período** (não sobre o cobrado/faturado em aberto) — coerente com o extrato (recebido líquido). Ou seja: para cada `Pagamento` (líquido de estorno) no período, vinculado a uma `Cobranca` cuja origem determina o profissional executante e o tipo:
  - **Consulta / Procedimento**: comissão = `valor_recebido × percentual` (config do profissional para o tipo, ou 30%). O profissional executante vem do agendamento/cobrança (consulta) ou do procedimento realizado (evolução).
  - **Cirurgia**: a comissão **NÃO** usa `ConfigComissaoProfissional` — usa o **valor definido em `OrcamentoEquipe`** do orçamento aprovado (já existe; é valor absoluto por profissional, não percentual). No detalhe, a base aparece como "valor do orçamento" (vide protótipo). Para regime caixa, a comissão de cirurgia entra **proporcionalmente ao recebido** sobre o total do orçamento (se pago parcial, repassa proporcional) — **DECISÃO SIMPLES aplicada**: nesta versão, a comissão de cirurgia da `OrcamentoEquipe` aparece **quando há pagamento no período sobre aquela cobrança de cirurgia**, rateada na proporção `recebido_no_periodo / valor_cobrado`. Mora em: Query (Dapper) + Domain (helper de rateio). Validada em: back.
- **R19** — **Comissão é relatório/visão, NÃO gera `Lancamento`** (anti-escopo §6): a aba Comissões apenas **calcula e exibe** quanto repassar. Não cria lançamento de despesa, não baixa nada. O repasse efetivo (se a clínica quiser registrá-lo) é um lançamento avulso manual que o usuário faz na aba Extrato. Mora em: ausência de efeito colateral. Validada em: back (CA confirma que nenhum `Lancamento` é criado ao abrir a aba Comissões).

### Bloco D — Custo/lucro por paciente (Relatórios)

- **R20** — **Estender a aba Financeiro de Relatórios** (`RelatorioFinanceiroTab.vue` + `RelatorioFinanceiroQuery`/handler) com uma **visão por paciente** (nova seção/tabela na aba existente — **sem quebrar** os KPIs/breakdown atuais). Colunas por paciente: **cobrado** (`SUM(cobrancas.valor_cobrado − desconto)`), **pago** (recebido líquido), **desconto**, **taxa** (de cartão), **custo** (insumo), **lucro** (= pago − custo). Mora em: Query (Dapper agregado, nova consulta ou extensão da existente) + Front (nova seção na aba). Validada em: back.
- **R21** — **Custo de insumo por paciente**: derivado das `MovimentacaoEstoque` de **saída** geradas pela baixa automática (F4/F5), que carregam `CustoTotal` (= quantidade × custo_unitário snapshot). O vínculo movimentação→cobrança→paciente é rastreável via a `Cobranca` que disparou a baixa. **Verificar**: hoje a `MovimentacaoEstoque` tem `Observacao` ("Baixa automática — ... cobrança #id") mas **pode não ter FK direta `cobranca_id`**. Se não houver, o `imedto-database` decide: (a) adicionar `cobranca_id?` nullable em `movimentacoes_estoque` (preferível — rastreabilidade limpa e indexável) **ou** (b) parsear a observação (NÃO recomendado — frágil). **Decisão aplicada: adicionar coluna `cobranca_id?` nullable** em `movimentacoes_estoque` para a baixa automática gravar o vínculo (a F4/F5 já tem a cobrança em mãos no momento da baixa — o dev ajusta o ponto de criação da movimentação). Movimentação manual fica `cobranca_id=null`. Mora em: schema + Query. Validada em: back.
- **R22** — **Audit no drill-down por paciente** (LGPD, decisão §F7 / proposta aplicada): o relatório **agregado** (KPIs, breakdown por categoria/forma) **não audita** (não expõe paciente identificado individualmente). A **visão por paciente** (R20) e o **drill-down em um paciente específico** **auditam** via `IPacienteAcessoLogService.RegistrarAsync(pacienteId, usuarioId, estabelecimentoId, TipoAcessoPaciente.Leitura)` — best-effort (falha do log não quebra o relatório), mesmo padrão da F2 (`ObterFinanceiroAbaQueryHandler`). **DECISÃO de granularidade**: a tabela por paciente lista vários pacientes de uma vez; auditar **uma linha por paciente listado** seria ruidoso. Aplicar audit **no drill-down de um paciente específico** (quando o usuário clica para ver o detalhe de um paciente) — não na listagem agregada. Se a listagem já traz nome+valores identificáveis, registrar **um** evento de acesso "relatório financeiro por paciente" agregado é aceitável; o detalhe individual sempre audita. Mora em: Handler. Validada em: back.
- **R23** — **Export CSV reusado**: a visão por paciente exporta via o mecanismo CSV já existente (`useRelatorioCsv.ts` / botão Exportar). Sem novo pipeline de export. Mora em: Front. Validada em: front.

### Bloco E — Configurações (aba Config do /financeiro)

- **R24** — **Aba Config do `/financeiro` reusa, não duplica**: a aba "Configurações" da tela `/financeiro` **embute/linka a `FinanceiroConfigView.vue` existente** (tabela de preços + taxa de cartão da F1, que vive em Configurações → `?secao=financeiro`) **e** acrescenta a config de comissão (default do sistema + atalho para configurar por profissional na Equipe). **DECISÃO aplicada**: para evitar duplicação, a aba Config **renderiza o componente `FinanceiroConfigView.vue` inline** (importa o mesmo componente — não recria os formulários) e adiciona um bloco "Comissões" com o default de sistema (read-only informativo "Padrão do sistema: 30%") + link "Configurar por profissional → Equipe". A config de % por profissional **mora na Equipe** (R16), não duplicada aqui. Mora em: Front (composição de componente). Validada em: front. **A navegação `/configuracoes/financeiro` e `?secao=financeiro` continuam funcionando (não remover).**

### Bloco F — Regressão (não quebrar o existente)

- **R25** — **Relatórios atuais não podem quebrar**: `RelatorioFinanceiroTab.vue` e `RelatorioFinanceiroQuery` continuam retornando os KPIs/breakdown/séries atuais. A visão por paciente é **aditiva** (nova seção/campo no DTO, opcional). Mora em: Query (extensão retrocompatível). Validada em: back (testes existentes verdes) + QA.
- **R26** — **Rotas `/financeiro/categorias` e `/financeiro/formas-pagamento` continuam funcionando** (`CategoriasFinanceirasView.vue`, `FormasPagamentoView.vue`, gate `financeiro.ver`). O redesign de `/financeiro` (rota `Financeiro`) **não altera** essas rotas filhas nem seus componentes. Mora em: Router (sem mudança nessas rotas). Validada em: front (testes de router verdes) + QA.
- **R27** — **`Lancamento` permanece a base de dados** do `/financeiro` (sem nova tabela de "movimento financeiro"). Caixa e extrato leem de `Lancamento`; comissão lê de `Pagamento`/`Cobranca`/`OrcamentoEquipe`; custo lê de `MovimentacaoEstoque`. Mora em: Query. Validada em: back.

---

## 5. Modelo de dados

> Espinha multi-tenant: **toda** tabela nova carrega `estabelecimento_id` e é filtrada por ele em todo SELECT/comando. O `imedto-database` é o autor das migrations (EF Core + SQL idempotente em `db/migrations/`).

### Tabelas novas

**`caixa_diario`** (aggregate `CaixaDiario`)
- `id` (PK), `estabelecimento_id` (tenant, NOT NULL, FK)
- `data` (date, NOT NULL) — o dia do caixa
- `status` (enum/text: `Aberto` | `Fechado`, NOT NULL)
- `aberto_por_usuario_id` (uuid, NOT NULL), `aberto_em` (timestamptz, NOT NULL)
- `fechado_por_usuario_id` (uuid, NULL), `fechado_em` (timestamptz, NULL)
- `reaberto_por_usuario_id` (uuid, NULL), `reaberto_em` (timestamptz, NULL) — R10.1
- `observacao` (text, NULL)
- `criado_em`, `atualizado_em`
- **Índice UNIQUE** `(estabelecimento_id, data)` — 1 caixa por dia por estabelecimento (R6).
- **Não materializa valores** — o resumo é lido on-the-fly de `Lancamento` (R8).

**`config_comissao_profissional`** (`ConfigComissaoProfissional`)
- `id` (PK), `estabelecimento_id` (tenant, NOT NULL, FK)
- `profissional_usuario_id` (uuid, NOT NULL) — o usuário do vínculo na Equipe
- `tipo` (enum/text: `Consulta` | `Procedimento`, NOT NULL)
- `percentual` (decimal(5,2), NOT NULL, 0–100)
- `criado_em`, `atualizado_em`
- **Índice UNIQUE** `(estabelecimento_id, profissional_usuario_id, tipo)` — uma config por (profissional, tipo) por tenant.

### Alteração em tabela existente

**`movimentacoes_estoque`** (R21)
- **ADD COLUMN** `cobranca_id` (bigint, NULL) — vínculo da baixa automática à cobrança que a originou (rastreabilidade de custo por paciente). FK fraca `ON DELETE SET NULL`. Movimentação manual = `cobranca_id NULL`.
- **Índice** `(cobranca_id)` para o join de custo por paciente no relatório (`WHERE cobranca_id IS NOT NULL`, índice parcial se ajudar).
- O dev ajusta o ponto de criação da `MovimentacaoEstoque` na baixa automática (F4/F5) para passar `cobranca_id`.

### Índices de agregação (decisão do DB agent conforme EXPLAIN)
- `lancamentos (estabelecimento_id, data_pagamento, status)` — agregação do caixa (R8) e KPIs por período (R1). Verificar se já existe índice cobrindo; criar se faltar.
- `lancamentos (estabelecimento_id, cobranca_id)` — join extrato→paciente (R3) e custo por paciente.
- `pagamentos (estabelecimento_id, data_pagamento)` — base regime-caixa da comissão (R18).

### Sem PII
- Nenhuma tabela nova guarda PII. `caixa_diario` e `config_comissao_profissional` guardam ids e valores. Nomes de paciente/profissional vêm por join no momento da leitura (DTO minimizado).

---

## 6. UX e fluxo

> **Referência visual obrigatória**: protótipo `Financeiro.html` / `ClinicFinanceTabs.jsx` / `ClinicFinanceApp.jsx` + screenshots `screenshots/cf-overview.png`, `cf-extrato.png`, `cf-caixa.png`, `cf-fechado.png`, `02-cf-fechar.png`, `02-cf-comissoes.png`, `cf-config2.png`. Recriar o **resultado visual** com o design system (`frontend/src/components/ui/`), **não** copiar a estrutura React. **Tokens vencem o protótipo** (CLAUDE.md §5 — nunca `font-size`/`font-weight` literal).

### Estrutura da tela `/financeiro` (substitui `FinanceiroView.vue`)
```
<AppPageHeader titulo="Financeiro" subtitulo="Clínica Vita — Unidade Centro · Dados restritos a esta unidade">
  #acoes: [Exportar] [Lançamento]   ← Lançamento só com financeiro.lancar
</AppPageHeader>

Abas (tabset DS): [ Visão geral ] [ Caixa diário ] [ Comissões ] [ Configurações ]
```

**Aba "Visão geral" (Extrato)** — `screenshots/cf-overview.png`, `cf-extrato.png`
- Seletor de período: pílulas **Hoje / Semana / Mês / Personalizado** (Personalizado abre date range).
- 4 KPI cards grandes: Recebido (verde), A receber (âmbar), Despesas (vermelho), Saldo (azul).
- Linha de KPIs secundários: Descontos concedidos · Taxas de cartão · Estornos.
- Filtros: tipo · categoria · forma de pagamento · origem.
- Tabela de lançamentos paginada (`AppPagination`), nome do paciente como **link** quando há cobrança vinculada.
- Estados: loading (skeleton/spinner), erro (mensagem genérica), **vazio** (`AppEmptyState` "Nenhum lançamento no período").

**Aba "Caixa diário"** — `screenshots/cf-caixa.png`, `cf-fechado.png`, `02-cf-fechar.png`
- Estado **NãoAberto**: card com CTA "Abrir caixa" (visível só com `financeiro.fechar`).
- Estado **Aberto**: header com selo verde "Caixa aberto", "Aberto às HH:MM por <nome>", botão **"Fechar caixa"** (à direita; só com `financeiro.fechar`). Abaixo: card "Resumo do dia por forma de pagamento" com mini-cards (Dinheiro, PIX, Crédito, Débito, [outras formas], Estornos em vermelho, **Total do dia** destacado).
- Estado **Fechado**: selo cinza "Caixa fechado", "Fechado por <nome> · às HH:MM", selo **"Somente leitura"**, mesmo resumo (read-only), observação se houver. Botão **"Reabrir caixa"** visível **apenas para o Dono**.
- Fechar abre confirmação (`AppConfirmDialog`) com campo opcional de observação.

**Aba "Comissões"** — `screenshots/02-cf-comissoes.png`
- Mesmo seletor de período.
- Header "Comissões por profissional · <período>" + **"Total a repassar"** à direita.
- Tabela: Profissional (avatar+nome+especialidade) · Atendimentos · Faturamento · % comissão (badge) · A repassar (verde).
- **Linha expansível**: ao expandir, sub-tabela por atendimento (Data · Atendimento[procedimento/consulta]+paciente · Base[badge "% config" / "valor do orçamento"] · Faturamento · Comissão).
- Estado vazio: `AppEmptyState` "Nenhuma comissão no período".

**Aba "Configurações"** — `screenshots/cf-config2.png`
- Renderiza `FinanceiroConfigView.vue` inline (tabela de preços + taxa de cartão — R24) **+** bloco "Comissões" (default de sistema 30% informativo + link "Configurar por profissional" → Equipe).

### Config de comissão na Equipe — `ProfissionalDetalhesModal.vue`, aba Perfil
- No bloco de dados do vínculo (onde já há profissão/especialidade), adicionar campo(s) % de comissão: Consulta e Procedimento. Hint "Padrão do sistema: 30%" quando vazio. Editável só pelo Dono (campo desabilitado/oculto caso contrário). `AppInputDecimal` para o percentual.

### Relatórios — `RelatorioFinanceiroTab.vue`
- Após os KPIs/donut atuais, **nova seção "Por paciente"**: tabela (cobrado · pago · desconto · taxa · custo · lucro), com lucro colorido (verde positivo / vermelho negativo). Drill-down por paciente (clique → audita). Export CSV reusado.

### Geral
- Mobile-ready (cards empilham, tabelas com scroll horizontal — padrão das views existentes).
- **Performance**: cada aba só dispara sua consulta quando **clicada** (lazy por aba — premissa CLAUDE.md "aba não clicada não dispara consulta"). Trocar período recarrega só a aba ativa.

---

## 7. Critérios de aceite (testáveis)

### Extrato / KPIs
- **CA156** (caminho feliz — KPIs): Dado lançamentos pagos no mês (3 receitas pagas somando R$ 700, 1 despesa paga R$ 200), Quando o usuário abre a aba Visão geral com período "Mês", Então Recebido = R$ 700, Despesas = R$ 200, Saldo = R$ 500 — todos calculados no backend (1 query agregada, nenhum lançamento somado no front).
- **CA157** (estorno abate recebido): Dado um pagamento de R$ 100 (lançamento Receita/Pago) e seu estorno (lançamento categoria "Estorno: Pagamento", valor −100) no período, Quando os KPIs são calculados, Então Recebido reflete o líquido (o estorno abate R$ 100) e o KPI "Estornos" mostra R$ 100.
- **CA158** (filtros): Dado lançamentos de categorias e formas variadas, Quando o usuário filtra por forma "PIX" + tipo "Receita", Então a tabela e a contagem refletem só os lançamentos PIX/Receita, com o WHERE aplicado no backend (a query recebe os filtros).
- **CA159** (link paciente): Dado um lançamento com `cobranca_id` (pagamento de cobrança de consulta), Quando a linha é renderizada, Então o nome do paciente aparece como link para a aba Financeiro do paciente; Dado um lançamento avulso (sem `cobranca_id`), Então não há link.
- **CA160** (CRUD avulso preservado): Dado um usuário com `financeiro.lancar`, Quando cria um lançamento manual de Despesa na aba Extrato, Então ele é criado e aparece na lista (endpoint `POST /api/financeiro/lancamentos` inalterado).
- **CA161** (paginação/performance): Dado 1000 lançamentos no período, Quando a aba carrega, Então a tabela vem paginada (page size padrão) e a request usa `LIMIT/OFFSET` — nenhuma resposta traz os 1000 de uma vez.

### Caixa diário
- **CA162** (abrir): Dado nenhum caixa do dia, Quando um usuário com `financeiro.fechar` clica "Abrir caixa", Então cria-se `CaixaDiario` status=Aberto com `aberto_por`+`aberto_em`, e a aba passa a mostrar o resumo do dia.
- **CA163** (resumo por forma): Dado pagamentos do dia (Dinheiro R$ 350, PIX R$ 550, Crédito R$ 200), Quando a aba Caixa abre, Então o resumo mostra cada forma com seu total e "Total do dia" = R$ 1.100 (soma líquida), calculado no backend sobre os lançamentos reais do dia.
- **CA164** (estorno no caixa): Dado um estorno de R$ 50 no dia, Quando o resumo é exibido, Então "Estornos" mostra −R$ 50 e o Total do dia desconta o estorno.
- **CA165** (fechar → read-only): Dado um caixa Aberto, Quando o usuário com `financeiro.fechar` fecha (com observação opcional), Então status=Fechado, grava `fechado_por`+`fechado_em`, a aba mostra selo "Somente leitura" e o botão "Fechar caixa" some.
- **CA166** (fechar 2x bloqueado): Dado um caixa já Fechado, Quando se tenta fechar de novo, Então o backend retorna 422 ("Caixa já está fechado").
- **CA167** (reabrir só Dono): Dado um caixa Fechado, Quando um usuário **com** `financeiro.fechar` mas **não** Dono tenta reabrir, Então recebe 403/422 (sem reabrir); Quando o **Dono** reabre, Então status volta para Aberto, grava `reaberto_por`+`reaberto_em`, e a ação fica auditável.
- **CA168** (lançamento retroativo NÃO trava): Dado um caixa do dia 10/06 já Fechado, Quando a recepção registra/paga um lançamento com data 10/06, Então a operação é permitida normalmente (nenhum 422), e o resumo do caixa (se reaberto para leitura) reflete o novo valor.
- **CA169** (RBAC caixa — botão oculto): Dado um usuário sem `financeiro.fechar`, Quando abre a aba Caixa, Então não vê os botões "Abrir caixa"/"Fechar caixa" (front) e, se chamar o endpoint direto, recebe 403 (back).
- **CA170** (MULTI-TENANT ABSOLUTO — dois estabelecimentos): Dado um usuário com vínculo nos estabelecimentos A e B, e um caixa fechado em A, Quando o usuário troca o contexto para B (estabelecimento ativo = B), Então **nenhum** dado do caixa de A aparece (nem totais, nem status, nem selo); e Quando tenta acessar o `CaixaDiario` de A com tenant=B via API direta, Então recebe 404 genérico e nada com PII é logado. **(CA de isolamento obrigatório.)**

### Comissões
- **CA171** (default 30%): Dado um profissional **sem** `ConfigComissaoProfissional` e pagamentos de consulta recebidos no período somando R$ 1.000, Quando a aba Comissões calcula, Então a comissão aplica 30% = R$ 300 (default de sistema), com badge "30%".
- **CA172** (config por profissional): Dado que o Dono configurou 35% de Consulta para a Dra. Paula, Quando a aba Comissões calcula o período dela, Então usa 35% (não o default).
- **CA173** (cirurgia via OrcamentoEquipe): Dado uma cobrança de cirurgia (orçamento aprovado com `OrcamentoEquipe` definindo R$ 3.750 para o Dr. Ricardo) totalmente paga no período, Quando a comissão é calculada, Então a comissão do Dr. Ricardo para essa cirurgia = R$ 3.750 (valor do `OrcamentoEquipe`, não % config), com base "valor do orçamento"; Dado pagamento **parcial** (50% recebido), Então a comissão entra rateada na proporção do recebido (R$ 1.875).
- **CA174** (regime caixa): Dado uma cobrança de consulta R$ 200 **em aberto** (não paga) no período, Quando a comissão é calculada, Então ela **não** entra (só recebido entra); Quando o pagamento é registrado, Então passa a compor a comissão do período do pagamento.
- **CA175** (comissão NÃO gera lançamento): Dado a aba Comissões aberta e calculada, Quando se inspeciona o financeiro, Então **nenhum** `Lancamento` de despesa de comissão foi criado (é só visão/relatório).
- **CA176** (linha expansível): Dado um profissional com 3 atendimentos comissionáveis no período, Quando o usuário expande a linha, Então vê 3 sub-linhas com data, paciente, base e comissão de cada.
- **CA177** (RBAC edição de comissão): Dado um usuário não-Dono no `ProfissionalDetalhesModal`, Quando abre a aba Perfil, Então o campo de % de comissão é read-only/oculto; o Dono pode editar e salvar.

### Custo / lucro por paciente (Relatórios)
- **CA178** (regressão Relatórios): Dado os testes atuais de `RelatorioFinanceiroQuery`/`RelatorioFinanceiroTab`, Quando a feature é implementada, Então todos continuam verdes (KPIs/breakdown/séries inalterados); a visão por paciente é aditiva.
- **CA179** (custo/lucro correto): Dado um paciente com cobrado R$ 500, pago R$ 500, e baixa automática de insumo de custo total R$ 120 (movimentação de saída vinculada à cobrança), Quando a visão por paciente é calculada, Então mostra cobrado R$ 500, pago R$ 500, custo R$ 120, lucro R$ 380.
- **CA180** (custo via vínculo cobrança): Dado a coluna `cobranca_id` em `movimentacoes_estoque` populada pela baixa automática, Quando o custo por paciente é agregado, Então usa o join `movimentacao → cobranca → paciente` (não parse de texto da observação).
- **CA181** (audit no drill-down): Dado o relatório agregado (KPIs/breakdown), Quando carregado, Então **não** grava `paciente_acesso_log`; Dado o drill-down de um paciente específico na visão por paciente, Quando acessado, Então grava uma linha em `paciente_acesso_log` `{usuario_id, paciente_id, estabelecimento_id, Leitura, timestamp}` (best-effort — falha do log não quebra o relatório).
- **CA182** (export CSV reusado): Dado a visão por paciente carregada, Quando o usuário clica "Exportar", Então o CSV é gerado pelo mecanismo existente (`useRelatorioCsv`), com BOM e separador padrão — sem pipeline novo.

### Config / regressão / LGPD / estados
- **CA183** (config sem duplicação): Dado a aba Configurações do `/financeiro`, Quando aberta, Então renderiza o `FinanceiroConfigView.vue` existente (tabela de preços + taxa de cartão) **inline** (não há formulário duplicado), mais o bloco de comissão; a rota `/configuracoes/financeiro` (`?secao=financeiro`) continua funcionando.
- **CA184** (rotas filhas preservadas): Dado as rotas `/financeiro/categorias` e `/financeiro/formas-pagamento`, Quando acessadas após o redesign, Então carregam normalmente com o gate `financeiro.ver` (testes de router verdes).
- **CA185** (LGPD — mensagens genéricas): Dado qualquer erro de validação/negócio (422) nas operações de caixa/comissão, Quando o backend responde, Então a mensagem é genérica e **não** contém PII (nome de paciente, etc.).
- **CA186** (estados vazios): Dado nenhum lançamento/comissão/caixa no período, Quando cada aba carrega, Então mostra `AppEmptyState` com texto específico ("Nenhum lançamento no período" / "Nenhuma comissão no período" / CTA "Abrir caixa").
- **CA187** (performance — agregação no banco): Dado as abas de KPI/caixa/comissão, Quando carregam, Então cada uma faz **agregação no banco** (SUM/GROUP BY) — nenhuma traz a coleção bruta de lançamentos/pagamentos para somar no front (verificável no SQL/handler).
- **CA188** (lazy por aba): Dado a tela `/financeiro` recém-aberta na aba Visão geral, Quando o usuário não clica em Comissões, Então a query de comissão **não** é disparada (só a aba ativa consulta).
- **CA189** (doc viva): Dado a entrega, Quando concluída, Então `Docs/ARQUITETURA.md` contém a subseção F7 (caixa diário, comissão, custo/lucro, RBAC `financeiro.fechar`) — validado pelo QA.

---

## 8. Riscos e dependências

**Dependências**: F1–F6 entregues (Cobranca/Pagamento/Estorno/Lancamento vinculado/baixa de estoque F4/F5/OrcamentoEquipe). A baixa automática de estoque (F4/F5) precisa estar gravando o vínculo à cobrança para o custo real funcionar (R21 ajusta o ponto de criação da movimentação).

**Riscos**:
- **Regressão em Relatórios** (R25/CA178): `RelatorioFinanceiroQuery` é lido pela aba Financeiro atual — extensão deve ser aditiva, testes existentes verdes. Vigiar.
- **Regressão em /financeiro**: a view é substituída do zero; o CRUD avulso (criar/pagar/cancelar) deve continuar funcionando (R5/CA160).
- **Multi-tenant do caixa** (R12/CA170): risco de vazar fechamento entre estabelecimentos de um usuário multi-vínculo — CA de isolamento absoluto é obrigatório e bloqueante.
- **Custo por paciente** depende de a `MovimentacaoEstoque` ter vínculo confiável à cobrança — daí a coluna `cobranca_id` (R21) em vez de parse de observação.
- **Rateio de comissão de cirurgia** (R18): regime caixa proporcional ao recebido — atenção ao arredondamento (usar `ArredondamentoMonetario`, half-away-from-zero).
- **Comissão não pode gerar efeito colateral** (R19/CA175): é só visão.
- **Performance** das agregações (R1/R8/R17): índices de período/forma/pagamento (seção 5) — o DB agent valida com EXPLAIN.

**Áreas regressivas a vigiar**: financeiro (view e CRUD avulso), relatório (aba Financeiro), permissionamento (espelho `financeiro.fechar` no front), estoque (vínculo cobrança na movimentação), orçamento (leitura OrcamentoEquipe).

---

## 9. Observações para execução

**Não-negociável**:
- Todo cálculo monetário no backend, `decimal`, via `ArredondamentoMonetario` (helper único, 2 casas, `AwayFromZero`). Nunca somar no front.
- Multi-tenant em camadas: todo SELECT/comando filtra `estabelecimento_id`; repositório falha-fechada; mensagem genérica.
- LGPD: audit no drill-down por paciente (R22), DTOs minimizados, sem PII em log/erro.
- Agregações no banco (SUM/GROUP BY) — anti-N+1, sem trazer coleção bruta.
- Lazy por aba (consulta só na aba clicada) e por seção de config (já é o padrão de `FinanceiroConfigView`).

**Liberdade técnica do dev/db**:
- Onde colocar a query de consolidação (estender `FinanceiroQueryRepository` ou criar `ConsolidacaoFinanceiraQueryRepository`) — escolha do dev/db.
- Como o `CaixaDiario` expõe o resumo (query separada vs. método do repo) — escolha do dev.
- Forma exata do enum `tipo` em `config_comissao_profissional` (text vs. smallint) e de `status` em `caixa_diario` — escolha do DB agent (consistente com o padrão do projeto).
- Índices finais conforme EXPLAIN (seção 5 é proposta).

**Reuso obrigatório (não duplicar)**:
- `FinanceiroConfigView.vue` (aba Config — R24), `useRelatorioCsv.ts` (export — R23), `IPacienteAcessoLogService` (audit — R22), `OrcamentoEquipe` (comissão cirurgia — R18), `AppPagination`/`AppEmptyState`/`AppConfirmDialog`/`AppInputDecimal` (DS), `ProfissionalDetalhesModal.vue` aba Perfil (config comissão — R16), endpoints CRUD de `FinanceiroController` (lançamento avulso — R5).
- **Permissão `financeiro.fechar` JÁ EXISTE** no `CatalogoPermissoes.cs` — reusar, não criar ação nova. Espelhar no front (`permissions.ts`) se faltar.

**Componentes DS novos candidatos** (avaliar antes de criar scoped): um **tabset/segmented tabs** para as 4 abas e o **seletor de período em pílulas** (Hoje/Semana/Mês/Personalizado) podem virar componentes do design system se não existirem equivalentes. Se nascer componente DS novo, **atualizar `Docs/DESIGN.md`** (o dev é responsável quando introduz componente DS). Verificar primeiro se já há tabset no DS (várias views usam abas — `RelatoriosView`, `EquipeView` usam padrões próprios; preferir reaproveitar o padrão dominante).

---

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — na seção "Área de domínio: Cobranças (Financeiro / contas a receber)", **adicionar subseção** "Consolidação F7 (briefing 2026-06-11_001)" cobrindo: (a) `CaixaDiario` (aggregate por estabelecimento+data, resumo on-the-fly de `Lancamento`, RBAC `financeiro.fechar` para fechar / Dono para reabrir, isolamento multi-tenant absoluto); (b) `ConfigComissaoProfissional` (% por profissional+tipo, default de sistema 30%, config na Equipe); (c) cálculo de comissão **regime caixa** (consulta/procedimento via config; cirurgia via `OrcamentoEquipe` rateado pelo recebido; **não gera `Lancamento`**); (d) custo/lucro por paciente via `movimentacoes_estoque.cobranca_id` (coluna nova) joinado à cobrança; (e) `/financeiro` redesenhado lê de `Lancamento` (base inalterada); aba Config reusa `FinanceiroConfigView`. **Atualização incremental, cirúrgica** — não reescrever a seção Cobranças. Responsável: `imedto-business-analyst` (esta entrega) — o briefing já contém o texto-fonte; o dev aplica/ajusta ao implementar.
- **`Docs/DESIGN.md`** — atualizar **somente se** nascer componente DS novo (tabset segmentado e/ou seletor de período em pílulas). Responsável quando ocorrer: `imedto-developer`. Se reaproveitar padrão existente, **nenhuma** atualização.
- **`Docs/LGPD.md`** — não muda (audit reusa `paciente_acesso_log`/`IPacienteAcessoLogService` já documentado; nenhum novo tipo de PII).
- **`Docs/COMANDOS.md` / `Docs/INFRA.md`** — não mudam (sem novo script, sem novo recurso AWS; migrations seguem o fluxo padrão do `imedto-database`).
