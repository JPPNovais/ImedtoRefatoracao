# Financeiro — regime de data unificado + "A receber" completo + métricas

**ID**: 2026-06-24_002
**Status**: Aprovado por usuário em 2026-06-24 (rodada de coerência do financeiro conduzida pelo orquestrador)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: financeiro (KPIs, dashboard) / relatório (custo-lucro por paciente, ranking de pacientes)

## 1. Contexto e motivação

Continuação da rodada de coerência do módulo financeiro (após o `2026-06-24_001`, que trata comissão-vs-estorno e fuso — **prioridade máxima, executar primeiro**). Este briefing fecha as incoerências de **regime de data** e de **completude do KPI "A receber"**, mais duas correções menores de métrica/coluna morta:

- **Frente C — "A receber" incompleto (decisão de produto nova):** hoje o KPI "A receber" (`ObterKpis`) soma **apenas** lançamentos `Lancamento` tipo=Receita, status=Pendente. Mas há dinheiro a receber que **não tem lançamento**: cobranças de atendimento (`cobrancas`) ainda em aberto — o saldo não pago de uma cobrança só vira `Lancamento` quando é **pago** (INV-3: pagamento gera lançamento Receita/Pago). Logo o saldo em aberto de uma cobrança não aparece em lugar nenhum no "A receber". O usuário decidiu que "A receber" deve somar **tudo a receber**: saldo em aberto de cobranças Pendentes + lançamentos Receita Pendentes avulsos — **sem dupla contagem**.

- **Frente D — regime de data inconsistente entre telas (Tipo A + alinhamento de decisão):** o `ObterKpis` já foi corrigido (deploy `0431d47`, 2026-06-24) para usar **regime caixa** (`data_pagamento`) em Recebido/Despesas. Mas duas outras telas continuam fora do padrão:
  - **Dashboard** (`DashboardQueryRepository.cs`): `ReceitasMes`/`DespesasMes` somam por `date_trunc('month', data_vencimento)` — usa **vencimento**, não pagamento. Um "recebido do mês" da Home diverge do "Recebido" da Visão geral.
  - **Custo/lucro por paciente** (`ObterCustoLucroPorPaciente`): filtra o período por `c.criado_em::date` (data de criação da cobrança = **competência**), não por data de pagamento. Diverge do regime caixa adotado nas demais telas.
  - Decisão Q3: "recebido/despesa realizada" = **regime caixa** (`data_pagamento`) em **todas** as telas (Visão geral, Home/Dashboard, Relatórios). "A receber/a pagar" = por vencimento.

- **Q4 — métrica "Atendimentos" das comissões errada (Tipo A):** o SQL conta `COUNT(*)` (cada atendimento) mas o C# sobrescreve com `g.Select(l => l.PacienteId).Distinct().Count()` (pacientes únicos). A coluna "Atendimentos" da aba Comissões fica **menor** que a lista detalhada que expande logo abaixo (cada linha = um atendimento). Decisão: contar **cada atendimento**, alinhando com a lista detalhada.

- **Q5 — coluna "Total gasto" morta no ranking de pacientes (decisão do orquestrador, caminho default):** o ranking de pacientes (`RelatorioPessoasTab.vue`) exibe uma coluna "Total gasto" que **sempre** mostra R$ 0,00 — o front hardcoda `totalGasto: 0` em `relatorioService.ts:131` (o backend `/relatorios/pessoas` não devolve esse valor). Coluna morta confunde o usuário. Decisão: **remover** a coluna (tabela + CSV).

## 2. Persona-alvo

- **Dono / financeiro** consultando a Home (Dashboard, visão rápida do mês), a Visão geral do financeiro (KPIs do período) e os Relatórios (custo/lucro por paciente, ranking). A coerência entre essas telas é o que sustenta a confiança no número — hoje a Home e a Visão geral discordam para o mesmo período.
- Frequência: Home/Dashboard diária; Relatórios no fechamento.

## 3. Escopo

**Inclui**:
- Frente C: nova lógica/query para o KPI "A receber" agregando **saldo em aberto de cobranças Pendentes** + **lançamentos Receita Pendentes** sem dupla contagem.
- Frente D: corrigir Dashboard (`ReceitasMes`/`DespesasMes` → `data_pagamento`) e custo/lucro por paciente (`ObterCustoLucroPorPaciente` → filtrar por data de pagamento, regime caixa). Rótulos explícitos onde ajudar o usuário a entender o regime.
- Q4: corrigir a métrica "Atendimentos" da comissão para `COUNT(*)` (cada atendimento), removendo o `Distinct(PacienteId).Count()` em C#.
- Q5: remover a coluna "Total gasto" do ranking de pacientes (front: `RelatorioPessoasTab.vue` + `useRelatorioCsv.ts` + o campo `totalGasto: 0` em `relatorioService.ts`).
- Atualização de `Docs/ARQUITETURA.md` com tabela canônica "KPI → fonte/regime de data".

**Não inclui**:
- Comissão-vs-estorno e fuso horário → **briefing `2026-06-24_001`** (executar antes). Os dois briefings tocam `ConsolidacaoFinanceiraQueryRepository.cs` e `DashboardQueryRepository.cs` — ver seção 8.
- Saneamento de dívida técnica **não-bloqueante** (não corrigir nesta entrega; registrado para backlog futuro): `Cobranca.Cancelar()` (dead code, sem call-site), o **2º SQL morto** em `ObterCustoLucroPorPaciente` (há um SQL com correlated subqueries comentado/substituído pelo `sqlOtimizado` — manter só o usado), e métodos mortos em `financeiroService.ts`. **Não tocar** salvo se a correção da Frente D obrigar (ex.: ao mexer no `sqlOtimizado`, remover o SQL irmão morto se estiver no mesmo método é limpeza do próprio mexido — aí é permitido; senão não).
- Mudança no regime de "A receber/a pagar" (continua por vencimento — Q3).

## 4. Regras de negócio

- **R1 ("A receber" = tudo a receber, sem dupla contagem — decisão Q2/Frente C):** o KPI "A receber" = `saldo em aberto de cobranças Pendentes` + `lançamentos Receita Pendentes avulsos`. A **não-dupla-contagem** é garantida pela própria mecânica do domínio (INV-3): uma cobrança **paga** vira `Lancamento` Receita/Pago (some do "a receber" das duas pontas); uma cobrança **em aberto** **não** tem lançamento Receita Pendente correspondente (o lançamento só nasce no pagamento). Portanto somar (saldo de cobranças Pendentes) + (lançamentos Receita Pendentes) **não** conta o mesmo dinheiro duas vezes. Mora em: Query (`ConsolidacaoFinanceiraQueryRepository.ObterKpis`, novo agregado Dapper). Validada em: backend (teste que monta cobrança em aberto + lançamento pendente avulso e confere a soma sem duplicar).
  - **Saldo em aberto da cobrança** = `valor_cobrado − desconto − SUM(pagamentos líquidos da cobrança)`, só para cobranças com `status` em aberto (Aberta/ParcialmentePaga, conforme INV-2), filtrado por tenant. Não incluir cobranças Canceladas nem Pagas. Pagamento já considera estornos pela soma líquida (consistente com a INV-7).
- **R2 (regime caixa unificado — decisão Q3/Frente D):** "recebido/despesa realizada" usa `data_pagamento` (regime caixa) em **todas** as telas: Visão geral (já corrigido), Home/Dashboard (`ReceitasMes`/`DespesasMes`), Relatório custo/lucro por paciente. "A receber/a pagar" usa `data_vencimento`. Mora em: Query (SQL Dapper). Validada em: backend.
- **R3 (custo/lucro por paciente em regime caixa):** o relatório de custo/lucro por paciente filtra o período por **data de pagamento** (`pagamentos.data_pagamento`), não por `cobrancas.criado_em`. O "Pago" e o "Lucro" do paciente passam a refletir o que foi efetivamente recebido no período. **Atenção de modelagem:** hoje o agrupamento parte de `cobrancas` e joina `pagamentos` agregados sem filtro de data; ao mudar para regime caixa o filtro de período deve incidir sobre `pagamentos.data_pagamento` (e os custos de insumo via `movimentacoes_estoque.cobranca_id` devem ser atribuídos coerentemente — ver risco na seção 8). Mora em: Query. Validada em: backend.
- **R4 (métrica Atendimentos = COUNT(\*) — Q4):** a coluna "Atendimentos" da comissão conta **cada atendimento** (linha de pagamento/atendimento do período), igual ao `COUNT(*)` do SQL e à lista detalhada que expande. Remover o `g.Select(l => l.PacienteId).Distinct().Count()` em C# e usar a contagem de itens do grupo (ou o `Atendimentos` do raw). Mora em: Query (agregação C# em `ObterComissoes`). Validada em: backend (teste com 2 atendimentos do mesmo paciente → Atendimentos = 2, não 1).
- **R5 (remover coluna "Total gasto" — Q5):** o ranking de pacientes deixa de exibir "Total gasto". Remover do template da tabela (`RelatorioPessoasTab.vue`), do CSV (`useRelatorioCsv.ts`) e o campo `totalGasto` do mapeamento `relatorioService.ts` (que hardcodava `0`). Não criar backend novo para popular o valor (decisão = remover, não preencher). Mora em: Front. Validada em: front.
- **R6 (multi-tenant — premissa):** todo agregado novo/alterado filtra `estabelecimento_id` (falha-fechada). O agregado de "A receber" sobre `cobrancas` deve filtrar `cobrancas.estabelecimento_id`.

## 5. Modelo de dados

**Provável ZERO migration**, a confirmar pelo `imedto-database`:
- Frente C não cria tabela: agrega `cobrancas` (status em aberto, saldo) + `lancamentos` (Receita Pendente) já existentes. O cálculo do saldo usa `cobrancas.valor_cobrado/desconto` e `SUM(pagamentos.valor)` por cobrança (líquido) — todas colunas existentes.
- Frente D não cria tabela: troca a coluna de filtro de período (`data_vencimento`→`data_pagamento` no Dashboard; `criado_em`→`data_pagamento` no custo/lucro).
- **Sinalizar ao `imedto-database` (Frente C — performance):** o novo agregado de "A receber" varre `cobrancas` em aberto + soma de `pagamentos` por cobrança, **sem filtro de data** (é estoque corrente, não fluxo de período). Em estabelecimento com muitas cobranças isso pode ficar caro. `imedto-database` deve rodar EXPLAIN e avaliar se há índice adequado em `cobrancas (estabelecimento_id, status)` e em `pagamentos (cobranca_id)`; criar índice **apenas se o EXPLAIN justificar** (não especulativo). Q5 e Q4 não tocam schema.

## 6. UX e fluxo

- **Visão geral / Home:** os números de "A receber" e "recebido do mês" passam a coincidir entre Home e Visão geral para o mesmo recorte. Onde ajudar, **rótulo explícito** do regime (ex.: tooltip/legenda "valores recebidos por data de pagamento"; "a receber por vencimento"). Sem redesenho — só clareza de rótulo onde o usuário pode se confundir.
- **Comissões:** a coluna "Atendimentos" passa a bater com a contagem de linhas da lista detalhada expandida (consistência interna da tela).
- **Ranking de pacientes:** a coluna "Total gasto" some da tabela e do CSV exportado. O cabeçalho do CSV de pacientes passa a `#; Paciente; Consultas` (sem a coluna de valor).
- Estados loading/erro/vazio das telas permanecem como já implementados (sem regressão).

## 7. Critérios de aceite (testáveis)

### Frente C — "A receber" completo

- **CA1 — decisão de produto (cobrança em aberto entra):** Dado uma cobrança Aberta de R$ 200 sem nenhum pagamento e **nenhum** lançamento Receita Pendente avulso no estabelecimento, Quando consulto `GET /financeiro/kpis`, Então "A receber" = R$ 200 (hoje seria R$ 0 — a cobrança em aberto não tem lançamento).
- **CA2 — decisão de produto (parcial):** Dado uma cobrança de R$ 200 com R$ 50 pagos (status ParcialmentePaga), Quando consulto os KPIs, Então "A receber" inclui o saldo de R$ 150 (não R$ 200 nem R$ 0).
- **CA3 — anti-dupla-contagem (caminho crítico, R1):** Dado uma cobrança de R$ 200 **totalmente paga** (vira `Lancamento` Receita/Pago de R$ 200) **e** um lançamento Receita **Pendente avulso** de R$ 80, Quando consulto os KPIs, Então "A receber" = **R$ 80** (a cobrança paga **não** entra no a receber, e seu lançamento Pago **não** é contado como pendente). O teste deve provar que nenhum dos R$ 200 reaparece no "a receber".
- **CA4 — decisão de produto (soma das duas fontes):** Dado uma cobrança Aberta de R$ 200 (sem lançamento) + um lançamento Receita Pendente avulso de R$ 80, Quando consulto os KPIs, Então "A receber" = R$ 280 (R$ 200 do saldo da cobrança + R$ 80 do lançamento pendente), sem duplicar.
- **CA5 — estorno na soma líquida:** Dado uma cobrança de R$ 200 com um pagamento de R$ 100 que foi **estornado**, Quando consulto os KPIs, Então o saldo em aberto da cobrança volta a R$ 200 (o estorno reabriu o saldo; soma líquida de pagamentos = 0) e isso reflete em "A receber".
- **CA6 — multi-tenant:** Dado um usuário do estabelecimento B, Quando consulta os KPIs, Então o agregado de cobranças em aberto filtra `cobrancas.estabelecimento_id = B` e nenhuma cobrança de A entra no "A receber" de B.
- **CA7 — performance:** Dado um estabelecimento com muitas cobranças/pagamentos, Quando consulto os KPIs, Então o novo agregado de "A receber" não introduz N+1 (uma consulta agregada, no mesmo batch do `ObterKpis` quando possível) e usa índice adequado (sem seq scan problemático no EXPLAIN — validação do `imedto-database`).

### Frente D — regime de data unificado

- **CA8 — Tipo A (Dashboard regime caixa):** Dado um lançamento Receita pago em 2026-06-10 mas com vencimento em 2026-07-05 (mês seguinte), Quando consulto o Dashboard em junho, Então "Receitas do mês" **inclui** esse valor em **junho** (mês do pagamento), não em julho — `ReceitasMes`/`DespesasMes` somam por `data_pagamento`, não `data_vencimento`.
- **CA9 — Tipo A (coerência Home × Visão geral):** Dado o mesmo período recortado como "mês corrente" na Home e na Visão geral, Quando comparo "Receitas do mês" (Dashboard) com "Recebido" (Visão geral), Então os dois usam regime caixa (`data_pagamento`) e são coerentes para o mesmo conjunto de lançamentos.
- **CA10 — Tipo A (custo/lucro por paciente regime caixa):** Dado uma cobrança criada (`criado_em`) em maio mas paga em junho, Quando consulto o relatório custo/lucro por paciente para **junho**, Então o "Pago"/"Lucro" do paciente reflete o pagamento em junho (filtro por `data_pagamento`), não pela data de criação da cobrança.
- **CA11 — rótulo de regime:** Dado a Visão geral/Home/Relatório, Quando exibem "recebido/realizado" vs "a receber/a pagar", Então há rótulo/legenda explícito do regime onde houver risco de confusão (recebido = por pagamento; a receber = por vencimento) — sem PII, texto genérico.

### Q4 — métrica Atendimentos

- **CA12 — Tipo A (contar cada atendimento):** Dado um profissional com 2 atendimentos pagos do **mesmo** paciente no período, Quando consulto `GET /financeiro/comissoes`, Então a coluna "Atendimentos" mostra **2** (não 1) e bate com o número de linhas da lista detalhada expandida; o C# usa a contagem de itens do grupo (`COUNT(*)`), não `Distinct(PacienteId)`.

### Q5 — remover coluna "Total gasto"

- **CA13 — decisão (tabela):** Dado a aba Relatórios → Pessoas → ranking de pacientes, Quando a tabela renderiza, Então **não** há coluna "Total gasto" (o cabeçalho e as células somem); restam "#", "Paciente", "Consultas".
- **CA14 — decisão (CSV):** Dado o export CSV do ranking de pacientes, Quando o arquivo é gerado, Então o cabeçalho é `#; Paciente; Consultas` (sem "Total gasto (R$)") e nenhuma linha tem a coluna de valor; `totalGasto` é removido do mapeamento em `relatorioService.ts` (não fica `0` órfão).

### CAs transversais obrigatórios

- **CA15 — RBAC (inalterado):** Dado um usuário sem `financeiro.ver`, Quando chama `GET /financeiro/kpis`/`/comissoes` ou abre o Dashboard/Relatórios financeiros, Então o gate atual (403 + ocultação no front) é preservado; nenhuma permissão nova.
- **CA16 — LGPD:** Dado o agregado de "A receber" e o relatório de custo/lucro, Quando o backend responde/loga, Então não há PII em log nem mensagem de erro; o DTO de KPIs continua agregado (sem nome de paciente); o ranking/custo-lucro mantém a minimização atual do F7 (nome do paciente só onde a tela já o exibe).
- **CA17 — estados:** Dado um estabelecimento sem cobranças em aberto e sem lançamentos pendentes, Quando consulto os KPIs, Então "A receber" = R$ 0,00 e a tela mostra o estado já existente sem erro; ranking sem pacientes mostra o `AppEmptyState` atual.
- **CA18 — performance (lazy/foco):** Dado a tela `/financeiro` com abas, Quando abro a Visão geral, Então a consulta de KPIs (com o novo agregado de "A receber") roda só para essa aba (lazy por aba do F7 preservado), sem disparar consultas de abas não clicadas.
- **CA19 — documentação viva:** Dado que a entrega define o regime de data como regra cross-cutting, Quando o PR é aberto, Então `Docs/ARQUITETURA.md` contém uma **tabela "KPI → fonte/regime de data"** cobrindo: Recebido (lancamentos Pago / `data_pagamento`), A receber (cobranças em aberto saldo + lancamentos Receita Pendentes / sem dupla contagem / por vencimento na exibição de prazo), Despesas (lancamentos Despesa Pago / `data_pagamento`), Dashboard ReceitasMes/DespesasMes (`data_pagamento`), Custo/lucro por paciente (`data_pagamento`), Vencidos (lancamentos Pendentes / `data_vencimento`).

## 8. Riscos e dependências

- **Dependência de ordem — executar `2026-06-24_001` primeiro.** Os dois briefings tocam `ConsolidacaoFinanceiraQueryRepository.cs` (o _001 mexe nas CTEs de comissão e no modo-vencidos; este mexe no `ObterKpis` "A receber", na agregação C# de Atendimentos e no custo/lucro) e `DashboardQueryRepository.cs` (o _001 troca `CURRENT_DATE`→Brasília nas linhas de vencidos/AgendamentosHoje; este troca `data_vencimento`→`data_pagamento` em ReceitasMes/DespesasMes). São **linhas distintas** no mesmo arquivo — sem conflito lógico, mas o _002 deve rebasar sobre o _001 para o SQL de ReceitasMes/DespesasMes já nascer com horário de Brasília no `date_trunc` (a Frente D troca a **coluna**; a Frente B do _001 troca o **`CURRENT_DATE`**). O dev deve garantir que ReceitasMes/DespesasMes, ao mudar para `data_pagamento`, ancore o mês corrente em horário de Brasília (combinação das duas correções).
- **Risco — custo/lucro por paciente ao mudar para regime caixa (R3):** a query atual (`sqlOtimizado`) parte de `cobrancas` filtradas por `criado_em` e agrega pagamentos sem filtro de data. Ao filtrar por `data_pagamento`, a granularidade muda: um paciente cuja cobrança foi criada fora do período mas **paga** dentro dele deve aparecer; e o "Cobrado"/"Desconto" (que são da cobrança, não do pagamento) precisam de definição coerente. **Decisão de modelagem para o dev:** o recorte do período incide sobre **pagamentos** (regime caixa do "Pago"/"Lucro"); "Cobrado"/"Desconto" passam a ser os da(s) cobrança(s) que tiveram pagamento no período (ou exibidos como contexto). Se o dev julgar que isso muda o significado de "Cobrado" de forma confusa, **parar e reportar como possível spec gap (Tipo B)** antes de inventar — não silenciar a ambiguidade.
- **Risco — 2º SQL morto em `ObterCustoLucroPorPaciente`:** há um SQL alternativo (correlated subqueries) que foi substituído pelo `sqlOtimizado`. Ao mexer no método para a Frente D, remover o SQL morto **se estiver no mesmo método** (é limpeza do próprio trecho mexido, alinhado a Surgical Changes). Não sair caçando outras dívidas.
- **Dependência de DB:** `imedto-database` acionado para **EXPLAIN de validação** do agregado de "A receber" (CA7) — ZERO migration provável; índice só se o plano justificar.

## 9. Observações para execução

- **Não-negociável:** "A receber" não pode dupla-contar (CA3 é o CA-prova). A não-duplicação vem da mecânica INV-3 (cobrança paga = lançamento Pago; cobrança em aberto = sem lançamento) — o dev deve somar **saldo de cobranças em aberto** + **lançamentos Receita Pendentes**, e o teste de CA3 deve provar que dinheiro de cobrança paga não reaparece.
- **Não-negociável:** regime caixa (`data_pagamento`) para recebido/realizado em **todas** as telas; vencimento só para a/receber-a/pagar.
- **Liberdade técnica:** o dev decide se o agregado de "A receber" é um SELECT separado no batch do `ObterKpis` ou um CTE unificado, desde que honre R1/CA3/CA7 e o multi-tenant.
- **Q5 é remoção pura** — não preencher "Total gasto" com backend novo. Tirar a coluna da tabela, do CSV e o `totalGasto: 0` do service.
- **Dívida não-bloqueante (NÃO corrigir agora):** `Cobranca.Cancelar()` sem call-site; métodos mortos em `financeiroService.ts`. Registrar mentalmente; mexer só se a própria correção criar o órfão.
- **Pontos de código (referência da auditoria, confirmados):**
  - Frente C: `backend/.../Database/Repositories/ConsolidacaoFinanceiraQueryRepository.cs` → `ObterKpis` (o CASE de `AReceber` hoje só soma lancamentos Receita Pendentes — linhas ~44-45). Adicionar agregado sobre `cobrancas` em aberto.
  - Frente D Dashboard: `backend/.../Database/Repositories/DashboardQueryRepository.cs` → `ReceitasMes`/`DespesasMes` usam `date_trunc('month', data_vencimento)` (linhas ~33-36) → `data_pagamento`.
  - Frente D custo/lucro: `ConsolidacaoFinanceiraQueryRepository.cs` → `ObterCustoLucroPorPaciente`, `sqlOtimizado`, `WHERE c.criado_em::date BETWEEN ...` (linhas ~717-718) → filtrar por `data_pagamento`.
  - Q4: `ConsolidacaoFinanceiraQueryRepository.cs` → agregação C# em `ObterComissoes`, `Atendimentos = g.Select(l => l.PacienteId).Distinct().Count()` (linha ~482) → contar itens do grupo.
  - Q5: `frontend/src/services/relatorioService.ts:131` (`totalGasto: 0`), `frontend/src/components/relatorios/RelatorioPessoasTab.vue` (coluna `Total gasto`, linhas ~57 e ~65), `frontend/src/composables/useRelatorioCsv.ts` (cabeçalho + `formatarDecimal(p.totalGasto)`, linhas ~107/130/135).

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** (seção "Consolidação F7" / Cobranças): adicionar **tabela canônica "KPI → fonte / regime de data"**:

  | KPI / tela | Fonte | Regime de data |
  |---|---|---|
  | Recebido (Visão geral) | `lancamentos` Receita/Pago | caixa — `data_pagamento` |
  | Despesas (Visão geral) | `lancamentos` Despesa/Pago | caixa — `data_pagamento` |
  | A receber | saldo de `cobrancas` em aberto + `lancamentos` Receita Pendentes (sem dupla contagem) | estoque corrente; prazo exibido por `data_vencimento` |
  | Estornos | `lancamentos` Estorno (valor negativo) | caixa — `data_pagamento` (= `data_estorno`) |
  | Dashboard ReceitasMes/DespesasMes | `lancamentos` Pago | caixa — `data_pagamento` |
  | Custo/lucro por paciente | `cobrancas` + `pagamentos` + `movimentacoes_estoque` | caixa — `data_pagamento` |
  | Vencidos (extrato/dashboard) | `lancamentos` Pendentes | vencimento — `data_vencimento < hoje (BRT)` |

  - Acrescentar à descrição de "A receber": "soma o **saldo em aberto** de cobranças Pendentes + lançamentos Receita Pendentes avulsos; a não-dupla-contagem é garantida pela INV-3 (cobrança paga vira lançamento Pago, cobrança em aberto não tem lançamento)."
- **Demais docs:** nenhum (DESIGN/INFRA/COMANDOS/LGPD inalterados — remoção de coluna no front não introduz componente novo; sem recurso/comando/PII novo).

---

### Referências
- Briefing F7 (base): `planejamentos/2026-06-11_001_financeiro-f7-consolidacao.md`.
- Briefing irmão (prioridade, executar antes): `planejamentos/2026-06-24_001_financeiro-correcao-comissao-estorno-e-fuso.md` (comissão líquida de estorno + fuso BRT).
- Bugfix anterior (deploy `0431d47`, 2026-06-24): "A receber"/extrato por `data_pagamento` no `ObterKpis`/`ListarExtrato` — este briefing **estende** o "A receber" (passa a incluir cobranças em aberto) e propaga o regime caixa às telas que ficaram para trás (Dashboard, custo/lucro).
