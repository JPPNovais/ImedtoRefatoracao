# Relatórios — exportação CSV por aba (respeitando filtros e minimização LGPD)

**ID**: 2026-06-10_004
**Status**: Aprovado por usuário em 2026-06-10 (modo autônomo — decisões fornecidas; ambiguidades residuais resolvidas com default mais simples em §11)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P (frontend-only; padrão de CSV já estabelecido)
**Áreas regressivas tocadas**: relatório (export). Não toca: backend, schema, financeiro/agenda/orçamento (apenas lê os dados já carregados).

## 1. Contexto e motivação

O roadmap (`Docs/Roadmap/FASE_1_COMPLETUDE.md`, item 1.5) pede "Relatórios 85% → 100%: exportação CSV das 6 abas existentes".

A investigação (2026-06-10) ajustou dois pontos do enunciado:

1. **A tela tem 5 abas, não 6, e não há aba "IA"**. `RelatoriosView.vue` expõe: **Visão geral, Financeiro, Agenda, Pessoas, Orçamentos**. A "Agenda" usa o endpoint `operacional` (não há aba "operacional" separada). Não existe aba "IA". Este briefing trata as **abas reais**.
2. **CSV já existe parcialmente, mas órfão**: `useRelatorioPdf.ts` tem `exportarFaturamentoCsv` e `exportarAgendamentosCsv` — porém operam nos DTOs **legados** (`/relatorios/faturamento` e `/relatorios/agendamentos`), que **não** são os DTOs consolidados que as abas atuais renderizam. Ou seja, hoje não há botão de export funcional ligado às abas que o usuário vê.

A dor: o usuário vê os indicadores na tela mas **não consegue levar os dados para uma planilha** (Excel) para análise própria, conferência contábil ou envio a um contador. Esta demanda adiciona um botão "Exportar CSV" por aba, gerando o arquivo a partir dos **dados já carregados na tela**, respeitando os filtros ativos e a minimização LGPD.

## 2. Persona-alvo

- **Dono / Recepcionista / Financeiro** (perfis com a ação `relatorios`) — no momento de fechamento de período, conferência ou repasse a contador/sócio. Frequência: média (mensal/quinzenal). Pessoas e Orçamentos exigem plano com `RelatoriosAvancados` (FeatureGate já no backend).

## 3. Escopo

**Inclui**:
- Botão **"Exportar CSV"** em cada uma das abas que exibem dados tabulares: **Financeiro, Agenda, Pessoas, Orçamentos** e **Visão geral**.
- Geração **100% no frontend** a partir dos dados já carregados na view (todas as abas hoje carregam o dataset agregado completo — não há paginação no servidor; ver §5). **Nenhum endpoint novo no backend.**
- O CSV reflete exatamente **as colunas que a tela mostra** naquela aba, respeitando o **período/filtros ativos** (os dados já são o resultado do filtro aplicado).
- Formato: **UTF-8 com BOM** (abre certo no Excel BR), separador **ponto-e-vírgula** (`;`), **datas `dd/MM/yyyy`**, números decimais com **vírgula** (R$ sem o símbolo, ou com — ver §11).
- Nome de arquivo por aba: `relatorio-{aba}-{dataInicio}-a-{dataFim}.csv` (kebab, datas `yyyy-MM-dd`).
- Estados: botão desabilitado durante carregamento da aba e quando a aba está vazia (sem dados).
- Reuso/consolidação do helper `baixarCsv` já existente (em `useRelatorioPdf.ts`), promovido a util reutilizável (ver §9).

**Não inclui** (fora — backlog se solicitado):
- Agendamento de relatório por e-mail; geração recorrente.
- Formato **XLSX**; export de gráficos/imagens.
- Endpoint de export no backend (não há dataset paginado/grande que justifique — ver §5).
- Drill-downs novos ou colunas que não existem na tela hoje.
- Export na aba "Visão geral" de blocos que sejam puramente visuais sem tabela subjacente (ver §11 — Visão geral exporta um resumo de KPIs).
- Mudança em endpoints/contratos do backend.

## 4. Regras de negócio

- **R1 — CSV espelha a tela (minimização LGPD)**. Cada export contém **apenas as colunas exibidas** naquela aba. Em particular, a aba **Pessoas** mostra `nome` de paciente/profissional + métricas agregadas (consultas, faturamento) — **nunca CPF, telefone, e-mail ou endereço** (não estão na tela e não devem entrar no CSV). Relatório agregado **não** lista CPF. Mora em: **Front** (montagem do CSV a partir do mesmo objeto que alimenta a tabela). Validada em: front (não há endpoint novo; o backend já minimiza o DTO).

- **R2 — Respeita os filtros ativos**. O CSV é gerado a partir do **estado atual** dos dados da aba, que já é o resultado do período/preset aplicado (`filtroAtual`). Mudar o período e reaplicar atualiza os dados; o export seguinte reflete o novo recorte. Mora em: **Front**. Validada em: front.

- **R3 — RBAC e FeatureGate herdados**. O acesso à tela de Relatórios já exige a ação `relatorios` (`[RequiresAcao("relatorios")]`). As abas **Pessoas** e **Orçamentos** já são `[FeatureGate(RelatoriosAvancados)]` no backend — se o plano não tem o recurso, os dados nunca chegam à tela e, portanto, **não há o que exportar** (o botão fica oculto/desabilitado junto com a aba). O export **não cria** caminho novo de dado: usa só o que o backend já autorizou e entregou. Mora em: **backend já existente** (gate) + **front** (botão segue a disponibilidade dos dados). Validada em: back (403 se forçar o endpoint) + front (UX).

- **R4 — Sem PII em erro e sem dado sensível em arquivo temporário**. O CSV é montado em memória e baixado via Blob/`URL.createObjectURL` (revogado após o clique). Em erro de geração, toast genérico, sem PII. Mora em: **Front**. Validada em: front.

- **R5 — Escaping correto**. Campos que contenham `;`, aspas, quebra de linha são envolvidos em aspas duplas e aspas internas são escapadas (`"` → `""`). Mora em: **Front** (helper de CSV). Validada em: front. (O helper atual envolve tudo em aspas — manter/robustecer.)

## 5. Modelo de dados

**Schema NÃO muda. Nenhum endpoint novo no backend. `imedto-database` NÃO é acionado.**

Justificativa para gerar no front (não no back): cada aba já carrega o **dataset agregado completo** na view (não há paginação server-side em Relatórios):
- **Financeiro** (`RelatorioFinanceiro`): KPIs (totalReceitas/Despesas/saldo) + `breakdown: LinhaRelatorio[]` (dezenas de linhas no máx.).
- **Agenda** (`RelatorioOperacional`): `kpis[]` + `breakdown[]`.
- **Pessoas** (`RelatorioPessoas`): `topPacientes[]` (top 10) ou `rankingProfissionais[]`.
- **Orçamentos** (`RelatorioOrcamentos`): `funil` (6 números) + `breakdown[]`.
- **Visão geral**: composição de financeiro+operacional+orçamentos já carregados.

Como os datasets são pequenos e agregados, o limite-de-segurança de 10.000 linhas citado na decisão **não se aplica** (nunca chega perto). Por isso **não** se cria endpoint de export. Caso futuro: se alguma aba virar listagem detalhada paginada (BI avançado — F3), aí sim avaliar endpoint; fora deste briefing.

## 6. UX e fluxo

**Posição do botão**: "Exportar CSV" por aba. Duas opções equivalentes (dev escolhe a mais limpa, mantendo consistência):
- (a) Um botão no cabeçalho da página (`AppPageHeader #acoes`, ao lado de "Atualizar") que exporta a **aba ativa**; OU
- (b) Um botão dentro de cada componente de aba (no cabeçalho do conteúdo).

Recomendação: **(a)** — um único `AppButton variant="secondary" icon="fa-solid fa-file-csv"` no header que chama o exportador da aba atual (`abaAtual`), evitando repetir o botão em 5 componentes. (Decisão de execução; ver §11.)

**Conteúdo do CSV por aba** (espelhando as tabelas atuais):
- **Financeiro**: bloco de cabeçalho com período + (opcional) linha de KPIs; depois tabela `Item; Valor; Quantidade; % do total` (mesma da "Tabela detalhada") + linha "Total".
- **Agenda**: KPIs (rótulo/valor) + tabela do breakdown (`Rótulo; Valor; Quantidade`).
- **Pessoas**: se `profissionais` → `#; Profissional; Atendimentos; Faturamento`; se `pacientes` → `#; Paciente; Consultas; Total gasto`. Sem nenhuma coluna de PII além do **nome** (que já está na tela).
- **Orçamentos**: bloco do funil (`Etapa; Quantidade; % do criado`) + KPIs (taxa conversão, valor médio aprovado) + breakdown se houver.
- **Visão geral**: resumo dos KPIs consolidados (financeiro + operacional + orçamentos) — uma tabela `Indicador; Valor` (ver §11).

**Estados**:
- **Carregando** a aba: botão "Exportar CSV" **desabilitado**.
- **Aba vazia / sem dados** (ex.: período sem movimento → `breakdown` vazio e KPIs zerados): botão desabilitado **ou** exporta só o cabeçalho de período + KPIs zerados (dev decide; ver §11 — default: desabilitar quando não há nenhuma linha de tabela e KPIs nulos).
- **Erro** ao gerar: toast genérico "Não foi possível gerar o CSV." (sem PII).
- **Sucesso**: download inicia; sem toast obrigatório (o download é o feedback).

**Mobile**: o botão segue o header responsivo já existente; download funciona em mobile (Blob).

## 7. Critérios de aceite (testáveis)

- **CA1 (export financeiro — caminho feliz)**: Dado a aba Financeiro carregada com 3 linhas de breakdown e KPIs preenchidos, Quando o usuário clica em "Exportar CSV", Então baixa um arquivo `relatorio-financeiro-{ini}-a-{fim}.csv` cujo conteúdo contém o período, as 3 linhas (`Item;Valor;Quantidade;% do total`) e a linha "Total", exatamente como na tabela da tela.
- **CA2 (separador, BOM e decimais)**: Dado qualquer export, Quando o arquivo é aberto, Então usa separador `;`, inicia com BOM UTF-8 (acentos corretos no Excel BR), datas no formato `dd/MM/yyyy` e valores monetários com vírgula decimal.
- **CA3 (respeita o período/filtro)**: Dado que o usuário troca o período de 30 dias para "hoje" e reaplica, Quando exporta, Então o CSV contém apenas os dados do novo recorte (e o nome do arquivo reflete as novas datas).
- **CA4 (export por aba — Agenda)**: Dado a aba Agenda ativa, Quando o usuário exporta, Então o CSV contém os KPIs e o breakdown da Agenda — e **não** as colunas do Financeiro (cada aba exporta o seu próprio conjunto).
- **CA5 (export Pessoas — minimização LGPD)**: Dado a aba Pessoas (ranking de profissionais) carregada, Quando o usuário exporta, Então o CSV contém apenas `#; Profissional; Atendimentos; Faturamento` — **sem CPF, telefone, e-mail ou qualquer PII além do nome** que já aparece na tela.
- **CA6 (FeatureGate — Pessoas/Orçamentos sem plano)**: Dado um estabelecimento cujo plano **não** inclui `RelatoriosAvancados`, Quando o usuário acessa Relatórios, Então as abas Pessoas/Orçamentos não trazem dados (backend nega) e o botão "Exportar CSV" dessas abas não está disponível (nada a exportar) — sem erro exposto com PII.
- **CA7 (RBAC — sem ação relatorios)**: Dado um usuário sem a ação `relatorios`, Quando tenta acessar a tela/endpoints de relatórios, Então recebe 403/422 do backend e não chega a ver dados nem botão de export.
- **CA8 (estado carregando)**: Dado que a aba está com `carregando = true`, Quando o usuário olha o botão "Exportar CSV", Então ele está **desabilitado** até os dados chegarem.
- **CA9 (estado vazio)**: Dado um período sem nenhum dado (breakdown vazio, KPIs zerados), Quando o usuário olha a aba, Então o botão "Exportar CSV" está desabilitado (ou exporta só cabeçalho + zeros, conforme decisão §11) — sem gerar arquivo malformado.
- **CA10 (escaping)**: Dado um rótulo de breakdown que contenha `;` ou aspas (ex.: categoria `"Consultas; retornos"`), Quando o usuário exporta, Então o campo é corretamente envolvido em aspas e as aspas internas escapadas, preservando as colunas no Excel.
- **CA11 (erro genérico)**: Dado um erro durante a geração do CSV, Quando ocorre, Então é exibido um toast genérico sem PII e nenhum arquivo corrompido é baixado.
- **CA12 (sem regressão de backend)**: Dado que esta entrega é frontend-only, Quando o build/test do backend roda, Então nada muda nos controllers/queries de relatório (nenhum endpoint novo, nenhum DTO alterado).

## 8. Riscos e dependências

- **CSV legado órfão**: `exportarFaturamentoCsv`/`exportarAgendamentosCsv` (e os PDFs `gerarFaturamentoPdf`/`gerarAgendamentosPdf`) em `useRelatorioPdf.ts` operam nos DTOs legados e **não** correspondem às abas atuais. Decisão: **não** ligar os botões a esses; criar exportadores que consomem os DTOs consolidados das abas. Se ficarem comprovadamente sem uso após esta entrega, **apenas sinalizar** ao orquestrador como dead code (não remover sem aval — política do projeto).
- **Mapeamento aba↔dados**: garantir que cada exportador leia o mesmo objeto que alimenta a respectiva `*Tab.vue` (financeiro/operacional/pessoas/orcamentos refs na `RelatoriosView`).
- **Pessoas (pacientes) gasto zerado**: o mapping atual de `pessoas` para `topPacientes.totalGasto` está hardcoded em 0 no service. O CSV deve refletir **o que está na tela** (0), não inventar valor — coerente com R1. (Se o usuário quiser o gasto real, é backlog do backend, fora daqui.)
- **Visão geral**: é composta de KPIs sem uma única tabela "fonte". Definir um resumo `Indicador; Valor` (ver §11) — risco de over-engineering; manter enxuto.
- **Locale de número**: usar formatação consistente (vírgula decimal). Reusar o padrão do helper atual (`.toFixed(2).replace('.', ',')`) para valores monetários; cuidado com inteiros (quantidades) que não levam decimais.

## 9. Observações para execução

**Não-negociável**:
- **Frontend-only**, sem endpoint/contrato novo no backend (§5/CA12).
- CSV espelha as colunas da tela; **nenhuma PII além do que já aparece** (R1/CA5).
- Formato BOM UTF-8 + `;` + datas `dd/MM/yyyy` + decimal vírgula (CA2).
- Respeitar filtros ativos (R2/CA3).
- Botão desabilitado em loading e vazio (CA8/CA9).

**Liberdade técnica (dev decide)**:
- Botão único no header (recomendado) vs. botão por componente de aba.
- Onde mora o código: estender `useRelatorioPdf.ts` com exportadores por aba **ou** criar um `useRelatorioCsv.ts` dedicado. Recomendação: **`useRelatorioCsv.ts`** novo (separar CSV de PDF) e **mover** o helper `baixarCsv` + escaping para `frontend/src/utils/` (ou um util de CSV) para reuso — confirmar se já existe util de CSV antes de criar (reuso > duplicação).
- Conteúdo exato da aba "Visão geral" (resumo de KPIs) — manter mínimo.
- Incluir ou não o símbolo `R$` nas células monetárias (ver §11).

**Reuso obrigatório (grep antes de criar)**:
- `baixarCsv` (já em `useRelatorioPdf.ts`) — consolidar, não duplicar.
- `AppButton` (`icon="fa-solid fa-file-csv"`), `AppToast`/padrão de toast.
- Os refs de dados já existentes na `RelatoriosView` (`financeiro`, `operacional`, `pessoas`, `orcamentos`).
- Formatação de data/moeda já usada nos componentes de aba.

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — atualizar **somente se** nascer um padrão reutilizável (ex.: util de CSV `utils/csv.ts` ou composable `useRelatorioCsv`) que valha registrar como convenção de export. Adicionar nota curta "Export CSV: BOM UTF-8 + `;` + `dd/MM/yyyy` + decimal vírgula; gerar no front a partir dos dados em tela; minimizar (só colunas exibidas)". Mudança incremental.
- **`Docs/LGPD.md`** — sem alteração estrutural: a regra de minimização (export só com colunas da tela, sem CPF em relatório agregado) já é premissa; o briefing apenas a aplica. Não atualizar salvo se o QA julgar que o caso "export" merece uma linha explícita no checklist — decisão do QA.
- **`Docs/ARQUITETURA.md`** — sem alteração (frontend-only, sem padrão de back novo).
- **Sem `INFRA.md`/`COMANDOS.md`**.

## 11. Decisões e assunções (execução autônoma)

1. **5 abas, não 6; sem aba "IA"**. O enunciado citava 6 abas e uma aba IA que **não existem** na `RelatoriosView`. Escopo = abas reais (Financeiro, Agenda, Pessoas, Orçamentos, Visão geral). Se a aba IA/operacional for criada no futuro, ganha seu próprio export em outro briefing.
2. **Geração no front para todas as abas** (R1 da decisão original: "no front quando o dataset da aba é o que está na tela; se agregada/paginada, endpoint no back"). Como **nenhuma** aba é paginada (todas trazem o agregado completo), **todas** geram no front; **nenhum** endpoint de export é criado. Limite de 10.000 linhas não se aplica.
3. **Botão único no header** exportando a aba ativa (default mais simples e DRY). Se o usuário preferir um botão por aba, vira ajuste trivial.
4. **Estado vazio → botão desabilitado** (default). Evita CSV "fantasma". Alternativa (exportar cabeçalho + zeros) fica a critério se o usuário pedir.
5. **Células monetárias sem o símbolo `R$`** no CSV (só o número com vírgula) — facilita o Excel interpretar como número. O cabeçalho da coluna indica a moeda (ex.: "Valor (R$)"). Seguindo o padrão já existente em `exportarFaturamentoCsv`. Se o usuário preferir com `R$`, é trivial.
6. **`totalGasto` de pacientes** sai como está na tela (0, pois o service hardcoda 0) — não inventar dado. Backlog de backend se quiserem o valor real.
7. **CSV legado órfão não é religado** — exportadores novos consomem os DTOs das abas atuais; o legado é sinalizado como possível dead code (sem remover, conforme política).
