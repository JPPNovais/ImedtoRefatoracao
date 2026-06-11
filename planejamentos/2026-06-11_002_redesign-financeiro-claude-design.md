# Redesign da página Financeiro do estabelecimento (Claude Design)

**ID**: 2026-06-11_002
**Status**: Aprovado por usuário em 2026-06-11 (decisões de escopo pré-validadas)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: relatório (financeiro/extrato/caixa/comissões/config), permissionamento (financeiro.ver), nenhuma mudança de schema exceto o endpoint de export (leitura)

## 1. Contexto e motivação

O módulo Financeiro do estabelecimento foi entregue funcionalmente completo na Fase 7 (briefing `2026-06-11_001`): backend CQRS com KPIs, extrato paginado, caixa diário, comissões e configuração de comissão já existem e respondem. A UI atual (`FinanceiroView.vue` + 4 abas) é funcional mas visualmente defasada em relação ao restante do produto, que vem sendo migrado para o padrão do **Claude Design** (Agenda, Atendimentos, Prontuário, Pacientes, Convênios).

O time de design entregou o protótipo de referência ("Brief 05 — Financeiro da clínica") com a tela completa: header com selo de unidade, abas com ícones em underline, chips de período, KPIs com borda superior colorida, faixa de KPIs secundários, extrato sem coluna de ações, caixa diário com banner de status, comissões expansíveis e configurações em grid. Esta demanda é um **redesign visual fiel ao protótipo, reaproveitando 100% do backend já existente** — com **uma única adição funcional**: o botão "Exportar" passa a exportar de verdade o extrato do período.

Evidência do protótipo (referência visual, não de implementação):
- `/tmp/claude-design/bundle/imedto/project/ClinicFinanceApp.jsx` — header, abas, aba "Visão geral" (KPIs + extrato)
- `/tmp/claude-design/bundle/imedto/project/components/ClinicFinanceTabs.jsx` — Caixa, Comissões, Configurações, modal Fechar caixa
- `/tmp/claude-design/bundle/imedto/project/styles/clinic-finance.css` — especificação visual completa
- `/tmp/claude-design/bundle/imedto/project/clinic-finance-data.js` — modelo de dados fictício do protótipo
- `/tmp/claude-design/bundle/imedto/chats/chat3.md` (~linhas 563-661) — intenção do design

## 2. Persona-alvo

- **Dono / Administrador / Financeiro**: acessa o Financeiro do estabelecimento para acompanhar recebimentos, despesas, caixa do dia e comissões. Uso diário (recepção fecha caixa) a semanal/mensal (dono confere comissões e saldo).
- **Recepção**: tem `financeiro.ver` por padrão — vê a tela e o extrato, e (com `financeiro.fechar`) abre/fecha caixa. A tela é a mesma; o que muda é a habilitação de ações conforme permissão.

## 3. Escopo

**Inclui**:
- Redesign **visual** da página inteira: header, barra de abas, e as 4 abas alinhadas ao protótipo.
- Renomear a aba "Extrato" para **"Visão geral"** (mantendo o id de rota/aba `extrato` no código se conveniente; o rótulo visível muda).
- Header: título + subtítulo com nome do estabelecimento e selo "Dados restritos a esta unidade"; botões **Exportar** (secundário) e **+ Lançamento** (primário) à direita, via slot `acoes` do `AppPageHeader`.
- Chips de período **Hoje / Semana / Mês / Personalizado** (substituindo os dois date pickers); "Personalizado" revela a seleção de intervalo (dois `AppDatePicker`).
- 4 KPIs primários em cards com borda superior colorida + ícone em chip colorido.
- Faixa de KPIs secundários (Descontos concedidos, Taxas de cartão, Estornos) em barra cinza única.
- Card "Extrato de lançamentos" com os 3 selects de filtro no header do card; tabela **sem coluna de ações** (decisão 3); empty state com ícone.
- **Export real do extrato** do período filtrado (botão Exportar) — endpoint backend dedicado (ver decisão 2 e Regras R7-R9).
- Caixa diário: redesign visual dos 3 estados (aberto / fechado read-only / não aberto), grid de resumo por forma, modal "Fechar caixa". Mantém endpoints e regra de negócio atuais.
- Comissões: redesign da tabela com avatar/iniciais, linha expansível com detalhe por atendimento, pill "Total a repassar". Mantém endpoint atual.
- Configurações: redesign em grid 2 colunas (Taxas de cartão, Tabela de preços) + card full-width (Comissão por profissional). **Só renderiza o que o backend já expõe** (ver decisão 4 / escopo negativo).
- Premissas obrigatórias: multi-tenant, RBAC, LGPD, tipografia por tokens, estados loading/empty/erro, lazy por aba.

**Não inclui** (escopo negativo explícito):
- **Nenhuma nova regra de negócio financeira.** O redesign não altera handlers, comandos ou cálculos existentes (KPIs, caixa, comissões, config).
- **Coluna de ações no extrato** (editar/excluir/pagar lançamento direto na tabela) — **removida** da tabela conforme decisão 3. Os fluxos de pagar/cancelar/editar lançamento avulso saem da Visão geral. (Os modais existentes de criar lançamento avulso permanecem, acionados pelo botão "+ Lançamento" do header.)
- **Configurações que o protótipo mostra mas o backend não tem**: a aba Config atual expõe **apenas configuração de comissão por profissional** (`/financeiro/comissoes/config`). Os cards "Taxas de cartão" e "Tabela de preços de consulta" do protótipo **não têm backend** — ver decisão 4: renderizar como **somente leitura / estado "em breve"**, sem inventar persistência.
- **Toggle de taxa ativa/inativa, edição de valor padrão, exceções por profissional, adicionar/remover taxa** — fora de escopo (não há endpoint). Botões correspondentes ficam ocultos ou em estado "em breve".
- Tweaks panel do protótipo (ferramenta de design, não vai pro produto).
- Mudança no menu/sidebar (a entrada do Financeiro já existe).

## 4. Decisões fechadas (pré-validadas com o usuário)

- **D1 — Escopo: página inteira.** Header, abas e as 4 abas (Visão geral, Caixa diário, Comissões, Configurações) alinhadas ao protótipo.
- **D2 — Exportar = export REAL do extrato.** Formato: **CSV** (UTF-8 com BOM, separador `;`, decimal vírgula — mesmo padrão do export legado de relatórios já existente no projeto). Justificativa: CSV é o formato mais simples que atende, sem dependência de lib de planilha; abre no Excel pt-BR direto. Como o extrato é **paginado**, o export **não** pode usar os dados da página corrente — exige **endpoint dedicado** `GET /financeiro/extrato/export` que aplica os **mesmos filtros do período** (datas + tipo/categoria/forma/origem) e retorna o período inteiro, respeitando multi-tenant. Geração do arquivo (montagem do CSV) feita **no frontend** a partir da resposta do endpoint, reusando o helper de CSV já existente; ou no backend retornando `text/csv` — **decisão cravada: backend retorna `text/csv` pronto** (BOM + `;`), para garantir que a serialização monetária/escape de PII fique num só lugar e o front só dispare o download. Ver R7-R9.
- **D3 — Extrato fiel ao design, sem coluna de ações.** Chips de período substituem date pickers. Colunas: Data, Descrição (paciente como link secundário na mesma célula), Categoria (pill verde p/ receita, vermelha p/ despesa), Forma, Valor (+/− colorido), Status (Liquidado/Pendente/Estorno). 3 selects de filtro (Receitas e despesas / origem / forma) no header do card.
- **D4 — Configurações: só o que o backend tem.** Renderizar o card "Comissão por profissional" funcional (endpoint existe). Os cards "Taxas de cartão" e "Tabela de preços" do protótipo são renderizados como **estado informativo "Em breve"** (placeholder visual com o título e ícone, sem switches/edição ativos) — espelhando o padrão já usado nas seções "em breve" de Convênios. **Não** criar tabela, comando ou endpoint para eles.

## 5. Regras de negócio

- **R1 — Visibilidade da página**: a página inteira exige `financeiro.ver`. Mora em: backend (`[RequiresAcao("financeiro.ver")]` já aplicado em todos os GETs) + front (guard de rota / store de permissões). Validada em: back + front. **Sem mudança** — apenas espelhar no front os botões.
- **R2 — Ações de caixa (abrir/fechar/reabrir)**: exigem `financeiro.fechar` (já aplicado). Reabrir caixa: só Dono (`EhDono`, já implementado no command). Mora em: Handler (existente). Front: botões "Abrir caixa" / "Fechar caixa" só renderizam se o usuário tem `financeiro.fechar`; botão "Reabrir" só se Dono. Validada em: back + front.
- **R3 — Config de comissão (salvar)**: exige `financeiro.fechar` (já aplicado). Front: card editável só para quem tem a permissão; demais veem read-only. Validada em: back + front.
- **R4 — KPIs e extrato são por estabelecimento ativo**: toda query já filtra `EstabelecimentoId = _tenant.EstabelecimentoId`. **Sem mudança** — o redesign não toca as queries.
- **R5 — Chips de período mapeiam para intervalos de data**: Hoje = `[hoje, hoje]`; Semana = `[início da semana corrente (segunda), hoje]`; Mês = `[1º dia do mês corrente, hoje]`; Personalizado = intervalo escolhido pelo usuário. O cálculo do intervalo é **client-side** (UX); o backend continua recebendo `dataInicio`/`dataFim`. Mora em: Front. Validada em: front (a request resultante carrega as datas corretas).
- **R6 — Mudar período/chip dispara recarga de KPIs + extrato**: mantém o comportamento atual (watch sobre datas/filtros). Reset de página ao trocar período/filtro. Mora em: Front.
- **R7 — Export respeita os filtros ativos**: o arquivo exportado contém exatamente as linhas que o extrato mostraria para o período + filtros atuais (tipo/origem/forma), **sem paginação** (todo o período). Mora em: Query Dapper nova (`ExportarExtratoQuery`) reaproveitando a lógica de filtro da `ListarExtratoQuery`. Validada em: back (query) + front (botão usa os mesmos filtros do estado).
- **R8 — Export é multi-tenant**: a query de export filtra `EstabelecimentoId = _tenant.EstabelecimentoId`. Usuário de outro estabelecimento nunca exporta dado alheio. Mora em: Handler/Query. Validada em: back.
- **R9 — Export contém PII (nomes de pacientes) e exige minimização + audit**: o CSV inclui a coluna "Paciente" (nome) apenas porque já é exibida na tela e o usuário tem `financeiro.ver`. O export **não** adiciona PII além do que a tela já mostra (sem CPF, sem telefone, sem prontuário). O ato de exportar é **auditado** (registro de acesso a dado financeiro/PII em massa). Mora em: Handler de export (audit) + LGPD. Validada em: back (linha de audit) + revisão LGPD. Ver seção 10.

## 6. Modelo de dados

**Nenhuma tabela nova. Nenhuma migration.** O redesign consome endpoints/queries existentes. A única adição é uma **query de leitura** para export:

- **`ExportarExtratoQuery`** (Dapper, leitura): mesmos parâmetros da `ListarExtratoQuery` **sem** paginação. Retorna todas as linhas do período + filtros, ordenadas por data desc. Reusa o SQL/where da `ListarExtratoQuery` (refatorar o predicado compartilhado, não duplicar). Multi-tenant: `WHERE estabelecimento_id = @EstabelecimentoId` (espelha a query existente). Sem índice novo — a query de extrato já está indexada por `(estabelecimento_id, data)`.
- **Audit do export**: inserir 1 linha na tabela de audit já usada para acesso a dado financeiro/PII (a mesma trilha já usada pelo módulo financeiro — `imedto-database` confirma a tabela canônica; provável `financeiro_acesso_log` ou a trilha de relatórios). Campos mínimos: `{ usuario_id, estabelecimento_id, acao: "ExportarExtrato", periodo_inicio, periodo_fim, total_linhas, timestamp }`. **Sem PII de paciente no log** (só contagem).

## 7. UX e fluxo

Referência visual: `clinic-finance.css`. **Tradução obrigatória para o design system do projeto** — nenhum `font-size`/`font-weight` literal em CSS scoped (CLAUDE.md §5): usar `var(--text-*)` / `var(--font-weight-*)`. Mapear cores `hsl(var(--primary/secondary/success/warning/error/info))` do protótipo para os tokens equivalentes de `frontend/src/assets/main.css` (`--success`, `--warning`, `--destructive`/`--error`, `--info`, `--primary`).

**Header** (`AppPageHeader` com slot `acoes`):
- Título "Financeiro".
- Subtítulo: `<nome do estabelecimento> · 🔒 Dados restritos a esta unidade`. Nome vem de `tenantStore.ativo.nomeFantasia`.
- Slot `acoes`: `AppButton` secundário "Exportar" (ícone planilha/download) + `AppButton` primário "+ Lançamento".

**Barra de abas** (estilo underline com ícone, como o protótipo `.cf-tab`):
- Visão geral (`fa-chart-line`) · Caixa diário (`fa-cash-register`) · Comissões (`fa-percent`) · Configurações (`fa-gear`).
- Manter o lazy-load por aba já existente (CA de performance). Aba ativa: cor primary + borda inferior primary.

**Aba "Visão geral"**:
- Linha de chips de período (`AppFilterPills` reaproveitado, ou pills equivalentes): Hoje / Semana / Mês / Personalizado. À direita da linha, label "`<mês/ano do período> · <estabelecimento>`". "Personalizado" revela dois `AppDatePicker`.
- Grid de 4 KPIs primários (cards com borda superior colorida + chip de ícone): Recebido (verde, `--success`), A receber (âmbar, `--warning`), Despesas (vermelho, `--destructive`, **valor também em vermelho**), Saldo (primário). Avaliar reuso/extensão de `AppStatCard` — se não comportar a borda superior colorida + chip de ícone, criar componente novo de design system (ver seção 10).
- Faixa de KPIs secundários (barra cinza única): Descontos concedidos · Taxas de cartão · Estornos.
- Card "Extrato de lançamentos": header do card com título + 3 selects (Receitas e despesas / Toda origem / Toda forma). Corpo = tabela (decisão 3) ou `AppEmptyState` com ícone de recibo e texto "Nenhum lançamento no período".
- Estados: loading (skeleton/placeholder sutil ou texto "Carregando..."), erro (mensagem genérica, sem PII), vazio (`AppEmptyState`).

**Aba "Caixa diário"** (redesign visual dos estados já implementados em `CaixaTab.vue`):
- Estado **não aberto**: card centralizado com ícone, texto e CTA "Abrir caixa" (só se `financeiro.fechar`).
- Estado **aberto**: banner verde com dot pulsante + badge "Caixa aberto" + info (data, hora, operador) + botão "Fechar caixa" (abre modal). Grid de resumo por forma (3 colunas, card "Total do dia" destacado).
- Estado **fechado**: banner cinza + badge "Caixa fechado" + selo "Somente leitura". Card "Resumo do fechamento" + observação. Botão "Reabrir" só para Dono.
- Modal "Fechar caixa" (`AppModal`): resumo por forma + total + textarea de observação opcional. Reusa `fecharCaixa` existente.

**Aba "Comissões"** (redesign visual de `ComissoesTab.vue`):
- Card com header "Comissões por profissional · `<período>`" + pill "Total a repassar: R$ X".
- Tabela: avatar com iniciais (gradiente primary), nome + especialidade, Atendimentos, Faturamento, % comissão (pill), A repassar (verde). Linha clicável expande detalhe por atendimento (Data, Atendimento, Base "% config" vs "valor do orçamento" como pill `info`, Faturamento, Comissão).

**Aba "Configurações"** (decisão 4 — grid 2 colunas + full-width):
- Card "Comissão por profissional" (full-width, **funcional** — endpoint existe): padrão do sistema em tag, pill % por profissional, tag "padrão", edição só com `financeiro.fechar`.
- Cards "Taxa de cartão por forma de pagamento" e "Tabela de preços de consulta": renderizar em **estado "Em breve"** (título + ícone + texto informativo), sem switches/edição/botões ativos. Padrão visual igual às seções "em breve" de Convênios.

**Mobile-ready**: KPIs colapsam para 2 colunas (`@media max-width:1100px` no protótipo); grid de config vira 1 coluna; abas com scroll horizontal (já implementado). Tabelas mantêm scroll horizontal em telas estreitas.

## 8. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — Visão geral)**: Dado um usuário com `financeiro.ver` no estabelecimento ativo, Quando abre a página Financeiro, Então vê o header com título "Financeiro", subtítulo com o nome do estabelecimento ativo e selo "Dados restritos a esta unidade", a barra de 4 abas com ícones (Visão geral ativa), os chips de período (Mês ativo por padrão), os 4 KPIs primários preenchidos, a faixa de 3 KPIs secundários e o card de extrato com as linhas do mês.

- **CA2 (chips de período)**: Dado a aba Visão geral, Quando o usuário clica em "Hoje", "Semana" ou "Mês", Então as datas `dataInicio`/`dataFim` da request mudam para o intervalo correspondente (Hoje=[hoje,hoje], Semana=[segunda,hoje], Mês=[1º do mês,hoje]) e KPIs + extrato recarregam para o período. Quando clica em "Personalizado", Então aparecem dois `AppDatePicker` e a seleção de intervalo dispara a recarga.

- **CA3 (extrato fiel — sem ações)**: Dado o card de extrato com lançamentos, Quando renderiza, Então mostra as colunas Data, Descrição (com paciente como link secundário na mesma célula quando houver paciente), Categoria (pill verde para receita / vermelha para despesa), Forma, Valor (com prefixo "+ " verde para entrada, "– " vermelho para despesa, "– " âmbar para estorno) e Status (Liquidado/Pendente/Estorno) — **e não existe coluna de ações** (sem botões pagar/editar/excluir na tabela).

- **CA4 (filtros do extrato)**: Dado o card de extrato, Quando o usuário muda o select "Receitas e despesas", "Toda origem" ou "Toda forma", Então a tabela recarrega filtrada pelo backend (parâmetros `tipo`/`origem`/`formaPagamento`) e a paginação reseta para a página 1.

- **CA5 (export real — caminho feliz)**: Dado um usuário com `financeiro.ver` e um período com lançamentos, Quando clica em "Exportar", Então o backend retorna `text/csv` (UTF-8 com BOM, separador `;`, decimal vírgula) com **todas** as linhas do período + filtros ativos (sem paginação), o browser dispara o download de um arquivo `extrato-financeiro-AAAA-MM-DD.csv`, e o conteúdo contém as colunas Data, Descrição, Paciente, Categoria, Forma, Valor, Status.

- **CA6 (export multi-tenant)**: Dado um usuário do estabelecimento B, Quando dispara o export, Então a query filtra `estabelecimento_id = B` e nenhuma linha do estabelecimento A aparece no arquivo; tentativa de manipular o request para outro tenant retorna apenas dados do tenant do claim (falha-fechada).

- **CA7 (RBAC — sem permissão financeiro.ver)**: Dado um usuário com papel sem `financeiro.ver`, Quando tenta acessar a página ou chama `GET /financeiro/extrato/export`, Então o front oculta/redireciona a página e o backend retorna 403; nada com PII é logado.

- **CA8 (RBAC — caixa)**: Dado um usuário com `financeiro.ver` mas **sem** `financeiro.fechar`, Quando abre a aba Caixa diário, Então os botões "Abrir caixa" / "Fechar caixa" não aparecem (ou ficam desabilitados) e a chamada ao endpoint correspondente retorna 403. Dado um usuário não-Dono, Quando o caixa está fechado, Então o botão "Reabrir" não aparece.

- **CA9 (RBAC — config comissão)**: Dado um usuário sem `financeiro.fechar`, Quando abre a aba Configurações, Então o card "Comissão por profissional" é read-only (sem botões de editar) e `POST /financeiro/comissoes/config` retorna 403.

- **CA10 (LGPD — audit do export)**: Dado um export disparado, Quando o handler processa, Então uma linha é inserida na trilha de audit financeiro com `{ usuario_id, estabelecimento_id, acao:"ExportarExtrato", periodo_inicio, periodo_fim, total_linhas, timestamp }` e **sem** nome de paciente no log.

- **CA11 (LGPD — mensagens genéricas)**: Dado um erro no carregamento de KPIs/extrato/export (ex.: 422/500), Quando o front exibe o erro, Então a mensagem é genérica ("Erro ao carregar extrato.", "Não foi possível exportar.") e não contém PII nem detalhe de tenant alheio.

- **CA12 (estados — vazio)**: Dado um período/filtro sem lançamentos, Quando o extrato carrega, Então o card mostra `AppEmptyState` com ícone e texto "Nenhum lançamento no período"; o botão Exportar nesse caso gera um CSV apenas com o cabeçalho (ou exibe aviso "Nada para exportar" — cravar: **gera CSV só com cabeçalho**, sem erro).

- **CA13 (estados — loading/erro)**: Dado o carregamento da Visão geral, Quando a request está pendente, Então há indicação de carregando; Quando falha, Então mensagem de erro genérica e a tela não quebra.

- **CA14 (caixa — 3 estados visuais)**: Dado a aba Caixa diário, Quando o caixa está "não aberto", "aberto" ou "fechado", Então o banner/estado renderiza conforme o protótipo (card CTA / banner verde com dot / banner cinza com selo "Somente leitura"), reusando os dados do endpoint `GET /financeiro/caixa` sem alterar a regra.

- **CA15 (comissões — expansão)**: Dado a aba Comissões com profissionais, Quando o usuário clica numa linha, Então ela expande mostrando o detalhe por atendimento com a Base exibida como pill ("% config" cinza, "valor do orçamento" azul/info), e o header do card mostra a pill "Total a repassar".

- **CA16 (config — em breve)**: Dado a aba Configurações, Quando renderiza, Então o card "Comissão por profissional" é funcional e os cards "Taxa de cartão" e "Tabela de preços" aparecem em estado informativo "Em breve" sem controles ativos; nenhum endpoint inexistente é chamado.

- **CA17 (tipografia por tokens)**: Dado todo o CSS scoped novo/alterado das views e componentes do Financeiro, Quando roda `npm run check:typography -- --ci`, Então passa sem novos literais de `font-size`/`font-weight` (gate de CI verde).

- **CA18 (performance — lazy por aba)**: Dado a página Financeiro recém-aberta na aba Visão geral, Quando o usuário ainda não clicou em Caixa/Comissões/Config, Então nenhuma request dessas abas é disparada (`GET /financeiro/caixa`, `/comissoes` só ocorrem ao abrir a aba), preservando o comportamento lazy atual.

- **CA19 (export — debounce/duplo clique)**: Dado o botão Exportar, Quando clicado, Então fica em estado de carregando/desabilitado até a resposta, evitando disparos duplicados; ao concluir, volta ao normal.

## 9. Riscos e dependências

- **Risco — regressão funcional ao reescrever as abas**: o backend está estável (F7 recém-publicado). O redesign deve preservar exatamente os contratos do `financeiroService.ts`. Vigiar: caixa (abrir/fechar/reabrir), comissões config. **Não** alterar service além de adicionar o método `exportarExtrato`.
- **Risco — coluna de ações removida quebra fluxo de pagar/cancelar lançamento avulso**: confirmar que esses fluxos têm outro ponto de acesso (o módulo de extrato/lançamentos via menu próprio, ou são acionados só pelo "+ Lançamento" para criar). Como a decisão 3 remove ações da tabela, validar com QA que nenhum fluxo fica órfão sem alternativa. Se o fluxo de pagar/cancelar lançamento avulso era usado, registrar como follow-up — **não** reintroduzir na tabela.
- **Dependência — `imedto-database`**: confirmar a tabela de audit canônica para registrar o export (R9/CA10) e que a `ExportarExtratoQuery` reaproveita o predicado da `ListarExtratoQuery` sem duplicação.
- **Dependência — token de borda superior colorida no KPI card**: avaliar se vira componente de design system (seção 10).
- **Risco — selo de unidade no subtítulo**: `AppPageHeader` recebe `subtitulo: string`. Se o selo "🔒 Dados restritos" exigir markup/ícone, pode ser necessário usar texto simples no subtítulo ou estender o header. Preferir texto simples para não tocar o componente compartilhado.

## 10. Observações para execução

**Não-negociável**:
- Backend de KPIs/extrato/caixa/comissões/config **não muda**. Única adição backend: endpoint `GET /financeiro/extrato/export` (`[RequiresAcao("financeiro.ver")]`, multi-tenant, audit) + `ExportarExtratoQuery`.
- Toda regra de export espelha back+front; o front só dispara o download do `text/csv` retornado.
- Tipografia por tokens (CLAUDE.md §5) — gate `check:typography` obrigatório.
- Multi-tenant em todas as queries (já garantido nas existentes; garantir na nova de export).
- LGPD: sem PII em log; audit do export; mensagens genéricas.

**Liberdade técnica**:
- Reuso de design system primeiro: `AppPageHeader` (slot `acoes`), `AppButton`, `AppFilterPills` (chips de período), `AppEmptyState`, `AppModal` (fechar caixa), `AppDatePicker`, `AppSelect`, `AppBadge`/`AppStatusPill` (pills de status/categoria), `AppPagination`, `AppDrawer` se necessário. Conferir `frontend/src/components/ui/` antes de escrever CSS novo.
- Avaliar `AppStatCard` para os KPIs; se não comportar borda superior colorida + chip de ícone, **criar `AppKpiCard`** (ou variante) no design system e documentar.
- O método `exportarExtrato` no `financeiroService.ts` deve usar `responseType: "blob"` e disparar o download (padrão de export já existente no projeto — reusar o helper de download se houver).

## 11. Atualização de documentação

- **`Docs/DESIGN.md`** — **atualizar** se o redesign introduzir componente novo de design system (provável `AppKpiCard` / variante de KPI card com borda superior colorida + chip de ícone, e/ou o padrão de "abas underline com ícone" se ainda não documentado). Adicionar à seção de componentes/variantes. Se o redesign for inteiramente com componentes já documentados, registrar "nenhum componente novo".
- **`Docs/ARQUITETURA.md`** — **atualizar** a seção de leitura/queries do módulo Financeiro para incluir a `ExportarExtratoQuery` (padrão de export de leitura que reusa predicado de query paginada). Mudança incremental/cirúrgica.
- **`Docs/LGPD.md`** — **atualizar** a seção de audit/export para registrar o novo ponto de export de dado financeiro com PII (nome de paciente) e a trilha de audit correspondente (acesso em massa via export). Incremental.
- **`Docs/COMANDOS.md`** — nenhum comando novo (o gate `check:typography` já está documentado).
- **`Docs/INFRA.md`** — nenhuma mudança de infra.
