# Financeiro — comissão líquida de estorno + datas em horário de Brasília

**ID**: 2026-06-24_001
**Status**: Aprovado por usuário em 2026-06-24 (rodada de coerência do financeiro conduzida pelo orquestrador)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: financeiro (comissões, caixa, extrato modo-vencidos, dashboard) / relatório (custo-lucro não toca aqui — vai no _002)

## 1. Contexto e motivação

A rodada de auditoria de coerência do módulo financeiro (consolidação F7, briefing `2026-06-11_001`) achou duas frentes de bug que erram dinheiro e data **hoje, em produção**:

- **Frente A — comissão sobre pagamento estornado (sangra dinheiro):** o cálculo de comissões (`ObterComissoes` em `ConsolidacaoFinanceiraQueryRepository.cs`) lê a tabela `pagamentos` e **nunca cruza** `estorno_pagamentos`. Quando um pagamento é estornado (INV-7 da F2), o domínio grava um `EstornoPagamento` em `estorno_pagamentos` + um `Lancamento` negativo — o registro em `pagamentos` permanece **imutável** por design. Resultado: a comissão continua sendo calculada sobre dinheiro que foi devolvido ao paciente. O profissional aparece com comissão a repassar sobre um valor que a clínica não recebeu de fato. Isso contradiz a invariante R18 do F7 ("comissão sobre o recebido, regime caixa") — o "recebido" não é líquido de estorno na query de comissões (mas já é no KPI "Recebido", que abate o lançamento negativo).

- **Frente B — fuso horário (erra o dia do caixa à noite):** o financeiro mistura `DateTime.Today` (horário do servidor/UTC), `CURRENT_DATE` (UTC no Postgres) e `new Date().toISOString()` (UTC no front). À noite no horário de Brasília (a partir das 21h BRT = 00h UTC do dia seguinte), "hoje" pula para o dia seguinte: o caixa abre/consulta o dia errado, e um lançamento que vence hoje é classificado como vencido. O projeto já tem a diretriz "tudo em horário de Brasília" — a classe `BrasiliaTime` (`backend/.../SharedKernel/Time/BrasiliaTime.cs`) já é usada nos Agendamentos. O financeiro ficou de fora.

Ambas as frentes são correção de bug (Tipo A) contra o comportamento que o F7 já especificou. Nenhuma muda a regra de produto — alinham o código ao que já estava decidido.

## 2. Persona-alvo

- **Dono / financeiro** olhando a aba Comissões (quanto repassar a cada profissional) e o Caixa diário (conferência do dia). Erro de comissão = repasse a maior. Erro de fuso = caixa do dia errado quando o expediente vira a noite.
- Frequência: comissões consultadas no fechamento (mensal/quinzenal); caixa, diariamente.

## 3. Escopo

**Inclui**:
- Frente A: subtrair, do cálculo de comissão, a parte correspondente a pagamentos estornados, fazendo o abatimento incidir **no período em que o estorno ocorreu** (`data_estorno`), espelhando como o KPI "Recebido" já trata o lançamento negativo de estorno. Cobre Consulta/Procedimento (% config) **e** Cirurgia (rateio por `OrcamentoEquipe`).
- Frente B: padronizar todas as datas "de hoje/agora" do financeiro em `America/Sao_Paulo` — backend (`FinanceiroController` caixa abrir/fechar/reabrir/consultar), SQL (`CURRENT_DATE` do modo-vencidos do extrato e do Dashboard), front (`VisaoGeralTab.vue`, `ComissoesTab.vue`).
- Atualização de `Docs/ARQUITETURA.md` (seção Cobranças/F7) com as duas regras canonizadas.
- Teste de regressão para comissão-com-estorno (hoje **inexistente**) e para a fronteira de fuso.

**Não inclui**:
- "A receber" agregando cobranças em aberto → **briefing `2026-06-24_002`** (Frente C).
- Unificação do regime de data caixa/competência no Dashboard ReceitasMes/DespesasMes e no custo-lucro por paciente → **briefing `2026-06-24_002`** (Frente D). **Atenção:** este briefing toca o **modo-vencidos** do Dashboard (a comparação `data_vencimento < hoje`, que é correta por vencimento) apenas para trocar o `CURRENT_DATE` por horário de Brasília. A troca de `data_vencimento` → `data_pagamento` em ReceitasMes/DespesasMes é da Frente D (_002). Os dois briefings tocam `DashboardQueryRepository.cs` — ver seção 8 (ordem de execução).
- Recálculo de meses passados quando há estorno (decisão Q1: NÃO recalcula passado — o abatimento incide só no período do estorno).
- Dívidas técnicas não-bloqueantes (`Cobranca.Cancelar()` dead code etc.) — registradas no _002.

## 4. Regras de negócio

- **R1 (comissão líquida de estorno — decisão Q1):** a base de comissão de um profissional num período = pagamentos recebidos no período **menos** os estornos de pagamento cujo `data_estorno` cai no período. O abatimento **não recalcula meses passados**: se o pagamento foi recebido em maio e estornado em junho, a comissão de maio permanece como foi (já paga/consultada) e o abatimento aparece **em junho** (período do estorno). Espelha exatamente como o KPI "Recebido" do `ObterKpis` trata o lançamento negativo de estorno (`data_pagamento = data_estorno`).
  - Mora em: Query (`ConsolidacaoFinanceiraQueryRepository.ObterComissoes`, SQL Dapper). É leitura/relatório, não há regra de domínio nova — a invariante já existe (R18 do F7); o que falta é a query honrá-la. Validada em: backend (teste de integração/unidade da query com cenário de estorno).
- **R2 (cobre os dois tipos de comissão):** o abatimento incide tanto na comissão por **percentual** (Consulta/Procedimento) quanto na comissão de **Cirurgia** (rateio `OrcamentoEquipe × recebido/cobrado`). Para cirurgia, o "recebido" usado no rateio deve ser **líquido de estorno** no período.
- **R3 (datas do financeiro em America/Sao_Paulo — Frente B):** todo conceito de "hoje"/"agora" do módulo financeiro resolve em horário de Brasília. Backend usa `BrasiliaTime.Today`/`BrasiliaTime.Now`; SQL usa `(now() AT TIME ZONE 'America/Sao_Paulo')::date` no lugar de `CURRENT_DATE`; front deriva `YYYY-MM-DD` de getters **locais** (`getFullYear`/`getMonth`/`getDate`) — **nunca** de `toISOString()` (que devolve UTC). Mora em: Controller + Query (SQL) + Front. Validada em: backend + front.
- **R4 (padrão de chips de data no front):** `VisaoGeralTab.vue` e `ComissoesTab.vue` devem derivar início/fim de período (hoje, semana, mês, trimestre) do padrão **local** já adotado por `fimMes()` em `VisaoGeralTab.vue` (que usa `new Date(y, m+1, 0)` e formata com getters locais). Os helpers `hoje()`/`inicioSemana()` (`VisaoGeralTab.vue`) e `hojeStr()`/início-de-mês/início-de-trimestre (`ComissoesTab.vue`) que hoje usam `toISOString()` passam a formatar via getters locais. Sem mudança visual nem de comportamento de chip — só corrige o dia na virada da noite.
- **R5 (multi-tenant — inalterado, premissa):** toda query continua filtrando `estabelecimento_id` (falha-fechada). O cruzamento com `estorno_pagamentos` deve preservar o filtro de tenant (a tabela tem `estabelecimento_id` e índice `ix_estorno_pagamentos_estab_cobranca`).

## 5. Modelo de dados

**Provável ZERO migration.** A correção da Frente A é uma mudança de SQL de leitura que passa a fazer anti-join/subtração contra `estorno_pagamentos`:
- Tabela `estorno_pagamentos` já existe (migration `20260610211901_criar_estorno_pagamentos_f2.sql`): colunas `id`, `pagamento_id`, `cobranca_id`, `estabelecimento_id`, `valor`, `motivo`, `data_estorno date NOT NULL`, `criado_em`, `lancamento_estorno_id`.
- Índices existentes que sustentam o join sem nova migration: `uq_estorno_pagamentos_pagamento_id` (UNIQUE em `pagamento_id` — permite anti-join 1:1 eficiente pagamento↔estorno) e `ix_estorno_pagamentos_estab_cobranca (estabelecimento_id, cobranca_id)`.
- A subtração por período usa `data_estorno` (já indexável via o índice de estab+cobrança no plano típico). **`imedto-database` deve confirmar via EXPLAIN** que o filtro `data_estorno BETWEEN @DataInicio AND @DataFim` no anti-join não degrada; se degradar com volume, avaliar índice `(estabelecimento_id, data_estorno)` — mas só se o EXPLAIN justificar (não criar índice especulativo).
- Frente B não toca schema: só troca literais de função de data no SQL e no C#.

## 6. UX e fluxo

Nenhuma mudança visual. Frente A corrige números na aba Comissões (`ComissoesTab.vue`) — o valor "A repassar" por profissional e o detalhamento expandido passam a refletir o líquido de estorno. Frente B corrige qual dia o caixa/consulta abre à noite e a classificação de vencidos. Estados loading/erro/vazio das telas permanecem como já implementados no F7 (sem regressão).

## 7. Critérios de aceite (testáveis)

### Frente A — comissão líquida de estorno

- **CA1 — Tipo A (caminho feliz, comissão por %):** Dado um pagamento de R$ 100 de uma cobrança origem=Consulta com profissional de comissão 30%, recebido e **não** estornado dentro do período, Quando consulto `GET /financeiro/comissoes` para o período, Então a comissão do profissional inclui R$ 30 referentes a esse atendimento (comportamento atual preservado).
- **CA2 — Tipo A (estorno no mesmo período):** Dado o mesmo pagamento de R$ 100 recebido e **estornado** dentro do período consultado, Quando consulto as comissões do período, Então a comissão referente a esse pagamento é **R$ 0** (recebido líquido = 0) e o atendimento não infla o "A repassar".
- **CA3 — Tipo A (estorno em período posterior, não recalcula passado — Q1):** Dado um pagamento de R$ 100 recebido em 2026-05-10 (comissão 30%) e estornado em 2026-06-15, Quando consulto as comissões de **maio** (01–31/05), Então a comissão de maio permanece R$ 30 (passado intocado); E Quando consulto as comissões de **junho** (01–30/06), Então aparece o abatimento de R$ 30 referente ao estorno (a comissão líquida de junho reflete a devolução), espelhando como o KPI "Recebido" trata o lançamento negativo em junho.
- **CA4 — Tipo A (cirurgia/rateio):** Dado uma cobrança origem=Cirurgia com `OrcamentoEquipe` e pagamentos parcialmente estornados dentro do período, Quando consulto as comissões, Então o rateio usa o recebido **líquido de estorno** no período (a proporção `recebido_no_periodo / valor_cobrado` desconta o estorno), e a comissão do profissional de equipe reflete a devolução.
- **CA5 — Tipo A (regressão):** Dado que **não existe** hoje teste cobrindo comissão com estorno, Quando a correção é entregue, Então há um teste automatizado (na suíte de integração/query) que reproduz CA2 e CA3 e falha contra o código atual (sem o cruzamento), passando após a correção.
- **CA6 — multi-tenant:** Dado um usuário do estabelecimento B, Quando a query de comissões agrega estornos, Então o cruzamento com `estorno_pagamentos` filtra `estabelecimento_id = B` e nenhum estorno de A entra no cálculo de B (filtro de tenant em todos os CTEs/joins).
- **CA7 — performance:** Dado um período com milhares de pagamentos e estornos, Quando a query roda, Então o cruzamento com `estorno_pagamentos` usa o índice `uq_estorno_pagamentos_pagamento_id`/`ix_estorno_pagamentos_estab_cobranca` (sem seq scan na tabela de estornos no EXPLAIN) e não adiciona round-trip extra (continua no mesmo batch/consulta da query de comissões).

### Frente B — datas em horário de Brasília

- **CA8 — Tipo A (caixa à noite):** Dado o relógio do servidor às 23h do dia 2026-06-24 em horário de Brasília (= 02h UTC de 25/06), Quando chamo `GET /financeiro/caixa` sem `data` ou `POST /financeiro/caixa/abrir` sem data no corpo, Então o caixa operado é o de **24/06** (dia corrente em Brasília), não 25/06.
- **CA9 — Tipo A (abrir/fechar/reabrir):** Dado o mesmo cenário noturno, Quando chamo abrir/fechar/reabrir caixa sem data explícita, Então as quatro rotas (`caixa`, `caixa/abrir`, `caixa/fechar`, `caixa/reabrir`) usam `BrasiliaTime.Today` e operam o dia corrente em Brasília.
- **CA10 — Tipo A (vencidos no extrato/dashboard):** Dado um lançamento Pendente com `data_vencimento = 2026-06-24` e o relógio às 23h BRT de 24/06, Quando consulto o extrato em **modo vencidos** e o Dashboard (contador/valores de vencidos), Então o lançamento **não** é contado como vencido (vence hoje, não ontem) — o SQL usa `(now() AT TIME ZONE 'America/Sao_Paulo')::date` no lugar de `CURRENT_DATE`.
- **CA11 — Tipo A (chips de data no front):** Dado o navegador do usuário às 23h BRT, Quando seleciono o chip "Hoje" na Visão geral ou "Mês"/"Trimestre" nas Comissões, Então as datas enviadas (`dataInicio`/`dataFim`) são o dia/mês/trimestre corrente em horário local, derivadas de getters locais (não `toISOString()`), sem pular para o dia seguinte.
- **CA12 — regressão de fuso:** Dado um teste que fixa o relógio em 02h UTC (= 23h BRT do dia anterior), Quando exercito a resolução de "hoje" do caixa (backend) e dos chips (front), Então o resultado é o dia em Brasília — teste falha contra o código atual (UTC) e passa após a correção.

### CAs transversais obrigatórios

- **CA13 — RBAC (inalterado):** Dado um usuário sem `financeiro.ver`, Quando chama `GET /financeiro/comissoes` ou `GET /financeiro/caixa`, Então recebe 403 e a aba fica oculta no front (comportamento F7 preservado, nenhuma permissão nova). Abrir/fechar/reabrir caixa continua exigindo `financeiro.fechar`; reabrir continua exigindo ser Dono.
- **CA14 — LGPD (sem PII):** Dado erro/log durante o cálculo de comissão ou operação de caixa, Quando o backend registra/responde, Então não há PII (nome de paciente, número de cartão) em log nem em mensagem de erro; o DTO de comissão mantém a minimização atual (nome do profissional e do paciente do atendimento só nos campos que a tela F7 já expõe).
- **CA15 — estados:** Dado um período sem nenhum pagamento líquido (tudo estornado) para um profissional, Quando consulto comissões, Então o profissional sai com "A repassar" R$ 0 (ou não aparece, conforme o agrupamento atual do F7) e a tela mostra o estado vazio/zerado já existente, sem erro.
- **CA16 — documentação viva:** Dado que a entrega muda regra cross-cutting de financeiro, Quando o PR é aberto, Então `Docs/ARQUITETURA.md` (seção Cobranças/F7) contém: (a) "comissão = recebido **líquido de estorno**, abatimento no período do estorno (não recalcula passado)"; (b) "datas do financeiro (hoje/agora) em `America/Sao_Paulo` — backend `BrasiliaTime`, SQL `now() AT TIME ZONE 'America/Sao_Paulo'`, front getters locais".

## 8. Riscos e dependências

- **Risco — paridade do regime entre KPI e comissão:** o ponto delicado da Frente A é fazer o abatimento incidir exatamente como o KPI "Recebido" já faz (por `data_estorno`). O dev deve usar `estorno_pagamentos.data_estorno` (mesma data que vira `data_pagamento` do lançamento negativo, conforme `EstornarPagamentoCommandHandler` → `Lancamento.CriarParaEstorno(dataEstorno: estorno.DataEstorno)`). Não usar `criado_em` do estorno (timestamptz) — usar `data_estorno` (date), que é o que o domínio adota como data canônica do estorno.
- **Risco — colisão em `DashboardQueryRepository.cs`:** este briefing troca `CURRENT_DATE` → horário de Brasília nas linhas de **vencidos** (`data_vencimento < CURRENT_DATE`) e nos contadores `AgendamentosHoje`. O briefing **_002** (Frente D) troca `data_vencimento` → `data_pagamento` nas linhas `ReceitasMes`/`DespesasMes`. **Recomendação:** executar este briefing (_001) primeiro; o _002 rebasa sobre as linhas já com horário de Brasília. Se executados na mesma sessão/branch, o dev deve aplicar os dois conjuntos de mudança no mesmo arquivo com cuidado (são linhas distintas — não há conflito lógico, só proximidade física).
- **Risco — agendamentos no Dashboard:** o batch do Dashboard também tem `inicio_previsto::date = CURRENT_DATE` (AgendamentosHoje). Como a diretriz é "tudo em Brasília" e os agendamentos já usam `BrasiliaTime`, **incluir** essa linha na padronização da Frente B (trocar `CURRENT_DATE` por horário de Brasília) para coerência — está no mesmo SQL e é o mesmo bug de virada de dia. Os agendamentos da semana (`NOW()`/`INTERVAL`) também devem ser avaliados; manter a janela relativa, mas a âncora "hoje" em Brasília.
- **Dependência:** nenhuma de DB schema esperada (ZERO migration provável). `imedto-database` é acionado **apenas para o EXPLAIN de validação** do anti-join com `estorno_pagamentos` (CA7) — se o plano for ruim em volume, aí sim avalia índice. Não bloqueia a entrega da lógica.

## 9. Observações para execução

- **Não-negociável:** o abatimento de comissão por estorno incide no **período do estorno** (`data_estorno`), não no período do pagamento original — espelho exato do KPI "Recebido". Não recalcular meses passados.
- **Não-negociável:** front nunca usa `toISOString()` para derivar data local de chip — usa getters locais (`getFullYear`/`getMonth`/`getDate`), seguindo o padrão de `fimMes()` em `VisaoGeralTab.vue`.
- **Liberdade técnica:** o dev decide se o cruzamento com `estorno_pagamentos` na query de comissões é um CTE de `estornos_periodo` subtraído do `pagamentos_periodo`, ou um `LEFT JOIN ... IS NULL` (anti-join) que exclui o pagamento estornado — desde que o resultado honre R1/R2/CA2/CA3/CA4 e o filtro de tenant (R5). Preferir a abordagem que mantenha o mesmo número de round-trips (CA7).
- **Reuso:** a data canônica do estorno já está disponível em `estorno_pagamentos.data_estorno`; não criar coluna nem helper novo. `BrasiliaTime` já existe e é o helper único — não reimplementar conversão de fuso.
- **Pontos de código (referência da auditoria, confirmados):**
  - Frente A: `backend/.../Database/Repositories/ConsolidacaoFinanceiraQueryRepository.cs` — CTE `pagamentos_periodo` (consulta/procedimento) e `pag_cirurgia` (cirurgia) no método `ObterComissoes`; nenhum cruza `estorno_pagamentos`.
  - Frente B backend: `backend/.../API/Controllers/FinanceiroController.cs` — caixa `consultar`/`abrir`/`fechar`/`reabrir` usam `DateTime.Today` → `BrasiliaTime.Today`.
  - Frente B SQL: `ConsolidacaoFinanceiraQueryRepository.cs` modo-vencidos (`data_vencimento < CURRENT_DATE`) e `DashboardQueryRepository.cs` (vencidos + AgendamentosHoje) → `(now() AT TIME ZONE 'America/Sao_Paulo')::date`.
  - Frente B front: `frontend/src/views/financeiro/tabs/VisaoGeralTab.vue` (`hoje()`, `inicioSemana()`) e `frontend/src/views/financeiro/tabs/ComissoesTab.vue` (`hojeStr()`, início-de-mês, início-de-trimestre, `toISOString`).

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** (seção "Consolidação F7" → subitem Comissões e padrões de Cobranças):
  - Acrescentar à regra de comissão: "Base = recebido **líquido de estorno**; o cruzamento com `estorno_pagamentos` por `data_estorno` abate a comissão **no período do estorno** (regime caixa, espelha o KPI Recebido), **sem** recalcular meses passados."
  - Acrescentar regra cross-cutting de datas: "**Datas do financeiro em `America/Sao_Paulo`** — todo 'hoje/agora' (caixa, vencidos, chips) resolve em horário de Brasília: backend via `BrasiliaTime`, SQL via `now() AT TIME ZONE 'America/Sao_Paulo'`, front via getters locais (nunca `toISOString()`)."
- **Demais docs:** nenhum (INFRA/COMANDOS/LGPD/DESIGN inalterados — não há recurso novo, componente novo, comando novo nem novo tipo de dado/PII).

---

### Referências
- Briefing F7 (base): `planejamentos/2026-06-11_001_financeiro-f7-consolidacao.md` (R18 regime caixa, comissões).
- Bugfix anterior na mesma rodada (deploy `0431d47`, 2026-06-24): "A receber"/extrato por `data_pagamento` no `ObterKpis`/`ListarExtrato` — este briefing **não** desfaz; estende o mesmo princípio (líquido de estorno) ao cálculo de comissões e padroniza o fuso.
