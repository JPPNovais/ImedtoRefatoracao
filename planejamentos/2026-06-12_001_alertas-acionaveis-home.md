# Alertas acionáveis na Home — bloco "Precisa da sua atenção" com deep-link filtrado

**ID**: 2026-06-12_001
**Status**: Aprovado por usuário em 2026-06-12
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: relatório (dashboard), financeiro (extrato), orçamento (lista), estoque (inventário) — nenhuma mudança de schema.

## 1. Contexto e motivação

Hoje a Home (`HomeView.vue`) mostra alertas operacionais (lançamentos vencidos, itens abaixo do mínimo, orçamentos pendentes) como números soltos dentro da faixa de KPIs e como um painel-lista de estoque. O usuário **vê** o problema mas não consegue **agir** a partir dele: precisa abrir o menu, navegar até a tela certa e refazer o filtro manualmente para chegar nos registros que o alerta apontava. O alerta informa, mas não conduz.

A demanda transforma os 3 alertas em **cartões acionáveis**: cada um leva, em um clique, à tela de destino **já filtrada** no recorte exato que o alerta representa (deep-link). O número deixa de ser decorativo e vira porta de entrada para a ação.

Esta entrega NÃO inclui ação inline (ex.: "dar baixa" direto no card) — é navegação direcionada. Ação inline fica para entrega futura.

## 2. Persona-alvo

- **Dono / Administrador / Financeiro**: abre a Home no início do dia, vê "3 lançamentos vencidos · R$ 150,00 a receber" e quer ir direto aos vencidos para cobrar/baixar.
- **Recepção / Almoxarife (com acesso a estoque)**: vê "2 itens abaixo do mínimo" e quer ir direto à aba de alertas do inventário para repor.
- **Qualquer papel com acesso a Orçamentos**: vê "4 orçamentos pendentes" e quer ir direto à aba de pendentes para dar andamento.

Frequência: diária, primeira tela após login. Médico sem acesso a Financeiro/Estoque **não** deve ver os alertas dessas áreas (ver §RBAC).

## 3. Escopo

**Inclui**:
- Novo bloco visual "Precisa da sua atenção" no topo da Home (logo após o cabeçalho "Olá, [nome]"), com 1 card acionável por pendência existente.
- Os 3 alertas viram acionáveis via deep-link por query param:
  - **Lançamentos vencidos** → `/financeiro?filtro=vencidos`
  - **Itens abaixo do mínimo** → `/inventario?status=baixo`
  - **Orçamentos pendentes** → `/orcamentos?status=pendentes`
- Enriquecer o DTO do dashboard com o **valor de vencidos quebrado por tipo** (a receber × a pagar).
- Mudança de contrato no endpoint de extrato do Financeiro: novo modo "vencidos" que ignora período e lista todas as pendências com vencimento passado.
- As 3 telas de destino passam a **ler `route.query` na montagem** e aplicar o filtro/aba correspondente.
- Remoção dos alertas hoje espalhados na faixa de KPIs e do painel-lista de estoque da Home (detalhe migra para o Inventário).
- Gate de exibição por RBAC reusando `podeAcessarRota` + `routePermissions.ts`.
- Atualização da documentação viva afetada (`Docs/ARQUITETURA.md`, `Docs/DESIGN.md`).

**Não inclui**:
- Ação inline ("dar baixa", "aprovar orçamento", "repor estoque") a partir do card. Apenas navegação.
- Novo gate de RBAC no backend: o DTO do dashboard continua devolvendo as contagens/valores para qualquer usuário com vínculo; o gate é só de **exibição** no front.
- Notificação push, e-mail ou sino para esses alertas.
- Qualquer mudança de schema/migration.
- Novo filtro de "vencidos" como aba persistente no Financeiro — é um modo de entrada via deep-link; depois de aberto, o usuário ajusta livremente os filtros normais.

## 4. Regras de negócio

- **R1 — Recorte de "vencidos" (espelha o card do dashboard)**: vencido = `status = 'Pendente' AND data_vencimento < CURRENT_DATE`, considerando **receitas E despesas**, sempre filtrado por `estabelecimento_id`. Este recorte **ignora o filtro de período** do extrato (mostra todo o histórico de pendências vencidas). Mora em: Query/Repository do Financeiro (`ListarExtratoQuery` + repositório de extrato). Validada em: backend (WHERE no SQL). A definição já existe idêntica no `DashboardQueryRepository` (`status = 'Pendente' AND data_vencimento < CURRENT_DATE`) — reusar a mesma condição para garantir paridade entre o número do card e a lista da tela.

- **R2 — Valor de vencidos quebrado por tipo**: o DTO do dashboard passa a expor a soma de vencidos separada: `vencidosAReceber` = SUM(valor) dos vencidos com `tipo='Receita'`; `vencidosAPagar` = SUM(valor) dos vencidos com `tipo='Despesa'`. Mora em: `DashboardQueryRepository` (mesma passada que já varre `lancamentos` para a contagem `LancamentosVencidos`) + `DashboardDto`. Validada em: backend. Não há mudança de schema — é agregação adicional na query existente.

- **R3 — Deep-link por query param (contrato)**: o atalho de cada alerta navega para a rota de destino com query param na URL. Contrato fixo:
  - Financeiro: `?filtro=vencidos`
  - Inventário: `?status=baixo`
  - Orçamentos: `?status=pendentes`
  A tela de destino LÊ `route.query` na montagem, aplica o filtro/aba correspondente ao estado interno que ela já possui, e o usuário pode ajustar livremente depois. O contrato sobrevive a refresh (F5) e é validável por URL. Mora em: front (HomeView monta o link; cada view destino lê a query). Sem espelho no backend além do parâmetro de extrato (R4).

- **R4 — Parâmetro "vencidos" no endpoint de extrato (mudança de contrato)**: `GET /financeiro/extrato` ganha um parâmetro novo (ex.: `vencidos=true` ou `status=vencidos` — o dev escolhe o nome, documentando-o). Quando ativo: o handler/query **ignora `dataInicio`/`dataFim`** e filtra `status='Pendente' AND data_vencimento < hoje` (R1), retornando receitas e despesas paginadas. Quando ausente: comportamento atual preservado (período + filtros). Mora em: `FinanceiroController.ListarExtrato` + `ListarExtratoQuery` + handler/repositório. Validada em: backend. Mudança de contrato — documentar em `Docs/ARQUITETURA.md`.

- **R5 — Mapeamento de estado das telas de destino (reuso, sem inventar filtro novo)**:
  - **Financeiro / `VisaoGeralTab.vue`**: `?filtro=vencidos` ativa o modo vencidos no carregamento do extrato (chama o serviço com o novo parâmetro de R4, sem período). Os demais filtros (tipo/origem/forma) permanecem disponíveis para ajuste manual.
  - **Inventário / `InventarioView.vue`**: `?status=baixo` seleciona a aba de Alertas (`tabAtiva = 'alertas'`) e ativa `filtroStatusItens = 'baixo'` (estado já existente, `apenasAbaixoMinimo`).
  - **Orçamentos / `OrcamentoListaView.vue`**: `?status=pendentes` seleciona a tab `pendentes` (estado já existente, statuses `['Rascunho','Enviado']`).
  Mora em: `onMounted` de cada view. Inventário e Orçamentos **não exigem mudança de backend** — o filtro já existe na tela.

- **R6 — Gate de exibição por RBAC no front**: cada card de alerta só renderiza se `podeAcessarRota(rotaDestino, helpers)` for verdadeiro, reusando o mesmo mecanismo dos `CARDS_ATALHO` já presentes em `HomeView.vue` e o mapa em `routePermissions.ts` (`Financeiro→financeiro.ver`, `Inventario→estoque.ver`, `Orcamentos→orcamento.ver`). Um Médico sem acesso a Financeiro NÃO vê o card de vencidos mesmo havendo vencidos. Mora em: front (computed de filtragem na Home). Validada em: front (UX). O backend continua devolvendo o dado; este é gate de exibição, não de acesso ao dado.

- **R7 — Estado vazio do bloco**: o bloco "Precisa da sua atenção" só aparece quando há **ao menos um** card de alerta visível (pendência > 0 E rota acessível). Sem nenhuma pendência visível, o bloco inteiro não é renderizado (sem cabeçalho órfão, sem card de "tudo certo"). Decisão explícita: **não** mostrar estado positivo/placeholder — ausência de bloco é o sinal de que está tudo em dia, mantendo a Home limpa. Mora em: front (`v-if` no bloco).

## 5. Modelo de dados

**Sem mudança de schema. Sem migration.**

- Tabela `lancamentos` (já existente): leitura adicional para `vencidosAReceber`/`vencidosAPagar`, mesma varredura que já produz `LancamentosVencidos` no dashboard. Colunas usadas: `estabelecimento_id`, `tipo`, `status`, `valor`, `data_vencimento`.
- Multi-tenant: toda contagem/soma e toda lista filtrada continua com `WHERE estabelecimento_id = @EstabId` (dashboard) / `_tenant.EstabelecimentoId` (extrato). Nenhum novo caminho de dado escapa do filtro de tenant.
- LGPD: contagens e somas agregadas não expõem PII. O extrato de vencidos reusa o `LancamentoExtratoDto` atual (sem novos campos sensíveis). Sem novo audit (dado financeiro operacional agregado, não prontuário).

DTO `DashboardDto` ganha 2 campos:
- `decimal VencidosAReceber`
- `decimal VencidosAPagar`

(`LancamentosVencidos` — contagem — permanece.)

## 6. UX e fluxo

**Bloco "Precisa da sua atenção"** posicionado logo após o cabeçalho "Olá, [nome]" e **antes** da faixa de KPIs neutros.

- Cabeçalho do bloco: título de seção via DS (`<h2 class="ds-section-title">` ou equivalente — sem font-size/font-weight literal; CLAUDE.md §5).
- 1 card por pendência visível, em grid responsivo (mobile-ready). Antes de criar componente novo, **conferir `frontend/src/components/ui/`** — existem `AppCard`, `AppKpiCard`, `AppStatCard`. Preferência: reusar/estender um deles para o card-alerta acionável; só criar `AppAlertCard` (ou nome equivalente) no design system se nenhum atender ao padrão "ícone + contagem + label + contexto + ação de navegação". Se criar, vai para `frontend/src/components/ui/` e é documentado em `Docs/DESIGN.md`.
- Conteúdo de cada card:
  - **Vencidos**: contagem + valor quebrado. Ex.: título "Lançamentos vencidos", linha "1 vencido · R$ 150,00 a receber" (e/ou "· R$ X a pagar" quando houver despesas vencidas). Card inteiro é clicável → `/financeiro?filtro=vencidos`.
  - **Estoque**: contagem + label. Ex.: "Itens abaixo do mínimo", "2 itens precisam de reposição". Clica → `/inventario?status=baixo`. SEM valor monetário.
  - **Orçamentos**: contagem + label. Ex.: "Orçamentos pendentes", "4 aguardando andamento". Clica → `/orcamentos?status=pendentes`. SEM valor monetário.
- Tom visual: cards de alerta com destaque (cor de atenção do DS via token), distintos dos KPIs neutros. Vencidos pode usar tom vermelho/atenção; estoque, laranja; orçamentos, neutro-info — todos via tokens, nunca literais.
- Estados: loading (dashboard ainda carregando → bloco não aparece até ter dado); erro (dashboard falha silenciosa, como hoje → bloco simplesmente não renderiza); vazio (R7 — bloco ausente).

**Remoções na Home** (mesma entrega):
- Remover da faixa de KPIs os cards de alerta soltos: `dashboard.itensAbaixoMinimo` (`.kpi.kpi-alerta`) e `dashboard.lancamentosVencidos` (`.kpi.kpi-alerta`). Se `orcamentosPendentes` está hoje como `.kpi`, migra para o bloco também.
- Remover o painel-lista "⚠ Estoque abaixo do mínimo" (os itens de `dashboard.itensAbaixoMinimoLista`) — o detalhe item-a-item passa a viver só na tela de Inventário, para onde o atalho leva.
- KPIs neutros permanecem intactos: Pacientes ativos, Agendamentos hoje, Próximos 7 dias, Saldo do mês.

**Telas de destino** (deep-link aplicado na montagem):
- Financeiro: ao abrir com `?filtro=vencidos`, o extrato já carrega em modo vencidos (sem período); os controles de filtro continuam editáveis.
- Inventário: ao abrir com `?status=baixo`, abre na aba Alertas com o filtro de abaixo-do-mínimo ativo.
- Orçamentos: ao abrir com `?status=pendentes`, abre na tab Pendentes.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — vencidos)**: Dado um estabelecimento com 1 lançamento de receita vencido (R$ 150,00, status Pendente, vencimento ontem), Quando o Dono abre a Home, Então o bloco "Precisa da sua atenção" exibe um card "Lançamentos vencidos" com "1 vencido · R$ 150,00 a receber", e clicar no card navega para `/financeiro?filtro=vencidos`.

- **CA2 (caminho feliz — estoque)**: Dado um estabelecimento com 2 itens abaixo do mínimo, Quando o usuário com acesso a estoque abre a Home, Então um card "Itens abaixo do mínimo" mostra a contagem 2 (sem valor monetário), e clicar navega para `/inventario?status=baixo`.

- **CA3 (caminho feliz — orçamentos)**: Dado um estabelecimento com 4 orçamentos em Rascunho/Enviado, Quando o usuário com acesso a orçamentos abre a Home, Então um card "Orçamentos pendentes" mostra 4 (sem valor monetário), e clicar navega para `/orcamentos?status=pendentes`.

- **CA4 (deep-link aplica filtro — Financeiro)**: Dado que o usuário acessa diretamente a URL `/financeiro?filtro=vencidos` (ou clica no card), Quando a tela monta, Então o extrato mostra SOMENTE lançamentos com status Pendente e vencimento anterior a hoje (receitas e despesas), ignorando o filtro de período, e o resultado sobrevive a um refresh (F5) na mesma URL.

- **CA5 (deep-link aplica filtro — Inventário)**: Dado `/inventario?status=baixo`, Quando a tela monta, Então ela abre na aba de Alertas com o filtro de itens abaixo do mínimo ativo, listando apenas itens com `estoqueAbaixoMinimo`.

- **CA6 (deep-link aplica filtro — Orçamentos)**: Dado `/orcamentos?status=pendentes`, Quando a tela monta, Então ela abre na tab Pendentes, listando apenas orçamentos em Rascunho ou Enviado.

- **CA7 (multi-tenant — contagem/valor)**: Dado um usuário do estabelecimento B, Quando a Home carrega, Então as contagens e somas de vencidos/estoque/orçamentos refletem apenas dados do estabelecimento B e nenhum lançamento, item ou orçamento do estabelecimento A é contado ou somado.

- **CA8 (multi-tenant — deep-link não vaza tenant)**: Dado um usuário do estabelecimento B que abre `/financeiro?filtro=vencidos` (idem inventário/orçamentos), Quando a tela carrega, Então só aparecem registros do estabelecimento B; tentar manipular a URL não expõe registro de outro tenant (filtro de tenant é aplicado no backend, não no query param).

- **CA9 (RBAC — médico sem financeiro)**: Dado um Médico não-dono sem permissão `financeiro.ver`, Quando a Home carrega com lançamentos vencidos existentes no estabelecimento, Então o card "Lançamentos vencidos" NÃO é renderizado (oculto via `podeAcessarRota`), mesmo que o DTO traga a contagem.

- **CA10 (RBAC — gate por rota em cada card)**: Dado um usuário sem `estoque.ver` e sem `orcamento.ver` mas com `financeiro.ver`, Quando a Home carrega com pendências nas três áreas, Então apenas o card de vencidos aparece; os de estoque e orçamentos ficam ocultos.

- **CA11 (estado vazio — bloco ausente)**: Dado um estabelecimento sem nenhuma pendência visível (nenhum vencido, nenhum item abaixo do mínimo, nenhum orçamento pendente — ou todas as rotas inacessíveis ao usuário), Quando a Home carrega, Então o bloco "Precisa da sua atenção" inteiro NÃO é renderizado (sem cabeçalho órfão, sem card placeholder).

- **CA12 (valor quebrado correto por tipo)**: Dado vencidos com 1 receita de R$ 150,00 e 1 despesa de R$ 80,00 no estabelecimento, Quando a Home carrega, Então o card mostra "R$ 150,00 a receber" e "R$ 80,00 a pagar" (receita conta como a receber, despesa como a pagar), valores calculados por tenant.

- **CA13 (paridade card × tela)**: Dado a contagem de vencidos exibida no card da Home (N), Quando o usuário clica e abre `/financeiro?filtro=vencidos`, Então a quantidade de itens vencidos listada na tela é igual a N (mesma regra `Pendente + vencimento < hoje` em ambos).

- **CA14 (remoção dos alertas antigos da Home)**: Dado a Home renderizada, Quando o usuário a inspeciona, Então NÃO existem mais os KPIs soltos `.kpi.kpi-alerta` de "Itens abaixo do mínimo" e "Lançamentos vencidos" na faixa de KPIs, NEM o painel-lista "Estoque abaixo do mínimo"; os KPIs neutros (pacientes ativos, agendamentos hoje, próximos 7 dias, saldo do mês) permanecem.

- **CA15 (contrato de extrato preservado quando sem o parâmetro)**: Dado uma chamada a `GET /financeiro/extrato` sem o parâmetro de vencidos, Quando processada, Então o comportamento atual (período + filtros tipo/origem/forma + paginação) é idêntico ao de antes desta entrega.

- **CA16 (design system / tipografia)**: Dado o bloco e os cards de alerta, Quando o gate tipográfico roda (`npm run check:typography -- --ci`), Então não há `font-size`/`font-weight` literais introduzidos; títulos e labels usam tokens/classes do DS; qualquer componente novo de card vive em `frontend/src/components/ui/`.

- **CA17 (documentação viva)**: Dado que esta entrega muda o contrato do endpoint de extrato e estabelece o padrão de deep-link por query param, Quando a entrega é concluída, Então `Docs/ARQUITETURA.md` documenta o novo parâmetro de `GET /financeiro/extrato` (modo vencidos, ignora período) e o padrão de deep-link via `route.query` nas telas de lista; e `Docs/DESIGN.md` documenta o bloco "Precisa da sua atenção" e o componente de card-alerta (se um novo for criado no DS).

## 8. Riscos e dependências

- **Regressão no extrato do Financeiro**: o parâmetro novo deve ser estritamente aditivo. Sem o parâmetro, nada muda (CA15). Vigiar a suíte de `ListarExtratoQueryHandler` e o `VisaoGeralTab`.
- **Npgsql — sem queries paralelas na mesma conexão** (gotcha conhecido): se a soma de vencidos por tipo for adicionada como subquery na mesma passada do dashboard (recomendado), não há novo risco; se for query separada, NÃO paralelizar com `Task.WhenAll` na mesma conexão.
- **Paridade card × tela (CA13)**: a regra de "vencidos" precisa ser literalmente a mesma nos dois lugares (`status='Pendente' AND data_vencimento < CURRENT_DATE`). Divergência aqui gera confusão de contagem — reusar a condição do `DashboardQueryRepository`.
- **Reset de query param**: ao o usuário ajustar filtros depois de entrar via deep-link, decidir se o query param permanece na URL ou é limpo. Recomendação não-bloqueante: manter a UX simples — o param só dispara o estado inicial; ajustes posteriores do usuário não precisam reescrever a URL. O dev tem liberdade técnica aqui desde que CA4/CA5/CA6 (estado inicial correto) e refresh sigam válidos.
- **Dependência de dados existentes**: Inventário e Orçamentos já têm o filtro pronto — risco baixo, só leitura de query. Financeiro é o único com superfície de backend.

## 9. Observações para execução

**Não-negociável**:
- Recorte de vencidos idêntico ao do dashboard (R1) — reuso da condição, não reimplementação divergente.
- Filtro de tenant no backend em todas as leituras (contagens, somas, extrato vencidos). Query param nunca é fonte de tenant.
- Gate RBAC de exibição reusa `podeAcessarRota` + `routePermissions.ts` — não criar lógica de permissão paralela.
- Parâmetro de extrato aditivo: sem ele, comportamento idêntico (CA15).
- Tipografia via tokens do DS (CLAUDE.md §5); rodar o gate tipográfico.

**Liberdade técnica do dev**:
- Nome exato do parâmetro de extrato (`vencidos=true` vs `status=vencidos`) — escolher e documentar.
- Reusar `AppCard`/`AppKpiCard`/`AppStatCard` ou criar `AppAlertCard` no DS — decidir conforme aderência; se criar, documentar em `Docs/DESIGN.md`.
- Forma de adicionar `vencidosAReceber`/`vencidosAPagar` (subquery na passada existente é o caminho preferido por custo).
- Política de manutenção/limpeza do query param após o usuário ajustar filtros (ver §8).

**Reuso esperado**:
- `podeAcessarRota` e `routePermissions.ts` (gate dos cards).
- Condição de vencidos do `DashboardQueryRepository`.
- Estados de filtro já existentes em `InventarioView.vue` (`tabAtiva`, `filtroStatusItens`) e `OrcamentoListaView.vue` (`tab`).
- `LancamentoExtratoDto`/`PaginaLancamentosExtratoDto` no extrato de vencidos (sem novo DTO).

**Arquivos esperados de mudança** (orientação, não prescrição de implementação):
- Front:
  - `frontend/src/views/HomeView.vue` — novo bloco "Precisa da sua atenção"; remoção dos KPIs de alerta e do painel-lista de estoque; gate RBAC dos cards; montagem dos deep-links.
  - `frontend/src/views/financeiro/tabs/VisaoGeralTab.vue` — ler `route.query.filtro === 'vencidos'` na montagem e carregar extrato em modo vencidos.
  - `frontend/src/views/inventario/InventarioView.vue` — ler `route.query.status === 'baixo'` → aba Alertas + `filtroStatusItens='baixo'`.
  - `frontend/src/views/orcamentos/OrcamentoListaView.vue` — ler `route.query.status === 'pendentes'` → tab Pendentes.
  - `frontend/src/services/dashboardService.ts` (+ tipo `DashboardData`) — novos campos `vencidosAReceber`/`vencidosAPagar`.
  - `frontend/src/services/financeiroService.ts` — novo parâmetro de vencidos no método `extrato`.
  - Possível novo componente DS `frontend/src/components/ui/AppAlertCard.vue` (+ export em `index.ts`), se justificado.
- Back:
  - `backend/.../Contracts/Dashboard/DashboardQuery.cs` — `DashboardDto` ganha `VencidosAReceber`/`VencidosAPagar`.
  - `backend/.../Infrastructure/Database/Repositories/DashboardQueryRepository.cs` — somar vencidos por tipo na passada existente.
  - `backend/.../Contracts/Financeiro/Queries/ListarExtratoQuery.cs` — novo campo (modo vencidos).
  - `backend/.../Application/Financeiro/Queries/ListarExtratoQueryHandler.cs` (+ repositório de extrato) — quando vencidos, ignorar período e filtrar Pendente + vencimento < hoje.
  - `backend/.../API/Controllers/FinanceiroController.cs` — novo `[FromQuery]` em `ListarExtrato`.

**Schema**: não aciona `imedto-database`. Nenhuma migration.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — (1) documentar o novo parâmetro do endpoint `GET /financeiro/extrato` (modo "vencidos": ignora período, filtra `Pendente + data_vencimento < hoje`, receitas e despesas); (2) registrar o padrão de **deep-link por query param** nas telas de lista (a view lê `route.query` na montagem e aplica filtro/aba; sobrevive a refresh; tenant nunca vem do query param).
- **`Docs/DESIGN.md`** — documentar o bloco "Precisa da sua atenção" na Home (cards de alerta acionáveis com deep-link) e, se um componente novo for criado, adicionar `AppAlertCard` à seção de componentes do design system. Se nenhum componente novo for criado (reuso de `AppCard`/`AppKpiCard`/`AppStatCard`), registrar apenas o padrão do bloco.
- **`Docs/LGPD.md`** — nenhuma alteração (sem novo dado pessoal, sem novo endpoint expondo PII, sem novo audit; contagens/somas agregadas).
