# Financeiro F6 — Convênio: estrutura base

**ID**: 2026-06-10_016
**Status**: Aprovado por usuário em 2026-06-10 (escopo fechado: estrutura base agora, avançado "em breve" — decisões q13/q14 do plano mestre)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: permissionamento, prontuário (paciente), relatório (aba financeiro/convênios do paciente), check-in/agenda, configurações do estabelecimento

## 1. Contexto e motivação

O módulo Financeiro F1–F8 já está em pé: check-in cria `Cobranca`, a aba Financeiro do paciente mostra o saldo, recibos são emitidos. A F1 deixou o trilho do convênio **reservado mas inerte**: o toggle Particular/Convênio existe no check-in, a `Cobranca` nasce com `tipo=Convenio`, `valor_cobrado=0` e `convenio_id` NULL (coluna reservada — Cobranca.cs:27-28), badge "Convênio" e bloqueio de pagamento de balcão (R12/CA17). A aba Financeiro mostra a cobrança convênio read-only com a nota "Faturada ao convênio" (F2/CA38). A aba Convênios do paciente é só um empty state "em breve" (PacienteDetalheView.vue:1107-1112).

A F6 entrega a **fundação operável** do convênio — sem entrar na operação completa (lote/XML TISS/glosa/conciliação, que são anti-escopo). Na prática hoje, a recepção que atende convênio não tem onde cadastrar a operadora, não registra a carteirinha do paciente, e o `convenio_id` da cobrança nunca é preenchido — o dado nasce cego. Isso destrava: (1) cadastrar convênios/planos da clínica; (2) guardar a carteirinha do paciente com alerta de validade; (3) o check-in passar a listar convênios reais e gravar `convenio_id`; (4) registrar nº de guia/senha de autorização na cobrança. As partes avançadas aparecem como cards "Em breve" (estrutura visual presente, desabilitada — protótipo `cv-soon.png`).

A explicação didática do fluxo TISS (operadora → guia → lote → glosa → repasse) está no `Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md §F6` como insumo para a evolução — **não** é escopo desta fase.

## 2. Persona-alvo

- **Dono / Administrador** (cadastro de convênios/planos em Configurações → grupo Faturamento): tarefa de setup, baixa frequência.
- **Recepção** (check-in com convênio, cadastro de carteirinha do paciente): tarefa diária, no balcão, na chegada do paciente.
- **Recepção / Profissional / Financeiro** (registro de nº de guia e senha de autorização na cobrança convênio): após o atendimento, ao faturar.

## 3. Escopo

**Inclui**:
1. **Cadastro de convênios e planos (CRUD)** — entidade `Convenio` (`estabelecimento_id`, `nome`, `registro_ans?`, `ativo`) com `ConvenioPlano` (1:N, `nome`, `ativo`). Tela lista + drawer no padrão do protótipo (`ConveniosApp.jsx`), rota em Configurações no grupo **Faturamento** (ver §6 — decisão de navegação D1).
2. **Carteirinha no paciente** — aba Convênios do paciente sai do empty state → lista de carteirinhas + CRUD. Entidade `PacienteConvenio` (`paciente_id`, `convenio_id`, `plano_id?`, `numero_carteirinha`, `validade?`, `ativo`). Paciente pode ter N convênios. Alerta visual de validade vencida.
3. **Check-in com convênio real** — o select de convênio (que a F1 deixou sem) passa a listar os convênios **ativos** do estabelecimento; se o paciente tem carteirinha cadastrada, pré-seleciona o convênio dela e mostra número/validade (alerta se vencida — **não bloqueia**, D5). A `Cobranca` convênio passa a gravar `convenio_id`.
4. **Guia / autorização** — campos nº guia, senha de autorização e data, na própria cobrança convênio (aba Financeiro do paciente). Estado "guia pendente" vs "preenchida" + form (protótipo `02-cv-guia-filled.png`/`cv-guia-modal.png`). Armazenamento: **colunas na própria `cobrancas`** (D6).
5. **Seções "em breve"** — cards Coparticipação, Conciliação de repasse e Glosas com selo "Em breve" (estrutura visual presente, desabilitada — `cv-soon.png`). **Sem schema** (D7).

**Não inclui (anti-escopo duro — não nesta fase nem na evolução imediata)**:
- Geração de XML TISS / faturamento em lote automatizado.
- Elegibilidade online / integração com integradora.
- Coparticipação **funcional** (cálculo do valor cobrado do paciente em convênio — fica zero nesta fase, herdado da F1).
- Conciliação de repasse **funcional** (lançar o que a operadora pagou contra o faturado).
- Registro/tratamento de glosa **funcional** (valor glosado + motivo + recurso).
- Tabela de preços por convênio (TUSS/CBHPM), comissionamento por convênio — fora desta fase.

## 4. Regras de negócio

- **R1** (CRUD convênio multi-tenant): `Convenio` e `ConvenioPlano` pertencem a 1 estabelecimento. Toda query/comando filtra `estabelecimento_id` do tenant ativo. Mora em: Domain (`Convenio` aggregate root, `ConvenioPlano` filho via root) + Handler + Query. Validada em: back (422/404 genérico) + front (UX).
- **R2** (nome obrigatório): `Convenio.nome` e `ConvenioPlano.nome` obrigatórios, trim, não-vazios; `registro_ans` opcional (texto livre, sem validação de formato ANS nesta fase). Mora em: Domain (fábrica). Validada em: back + front.
- **R3** (inativar > excluir): convênio/plano usa **soft-delete via `ativo=false`** como ação de remoção preferida — preserva integridade de cobranças e carteirinhas que referenciam o id. Convênio inativo **não** aparece em novas seleções (select do check-in, novo cadastro de carteirinha) mas continua legível no histórico. Exclusão física só permitida se nunca referenciado (sem carteirinha nem cobrança) — caso contrário 422 "Convênio em uso — inative em vez de excluir." Mora em: Domain + Handler. Validada em: back + front.
- **R4** (planos pertencem ao convênio): `ConvenioPlano` é filho de `Convenio` (acessado via root, DDD). Inativar convênio não exige inativar planos um a um, mas plano de convênio inativo não aparece em seleção. Mora em: Domain. Validada em: back.
- **R5** (carteirinha multi-tenant + paciente): `PacienteConvenio` filtra `estabelecimento_id` (carteirinha é por estabelecimento — paciente pode ter convênios diferentes em clínicas diferentes) e vincula `paciente_id` do tenant. `convenio_id` deve apontar para um convênio **do mesmo estabelecimento** (FK + validação no handler — convênio de outro tenant → 404 genérico). `numero_carteirinha` obrigatório; `validade` opcional; `plano_id` opcional mas, se informado, deve pertencer ao `convenio_id`. Mora em: Domain (fábrica) + Handler. Validada em: back + front.
- **R6** (alerta de validade — não bloqueia): carteirinha com `validade < hoje` é exibida com alerta visual de vencida (na lista do paciente e no check-in). **Nunca bloqueia** check-in, cadastro nem faturamento — é informativo (D5). Carteirinha sem `validade` não dispara alerta. Mora em: Front (cálculo de exibição) + DTO expõe `validade`. Validada em: front.
- **R7** (check-in grava `convenio_id`): quando `tipo_atendimento=Convenio`, o check-in passa a aceitar um `convenio_id` opcional; a `Cobranca` nasce com esse `convenio_id` setado (e `valor_cobrado=0`, herdado da F1 — convênio sem balcão). `convenio_id`, se informado, deve ser convênio **ativo do estabelecimento** (validado no handler → 404 genérico se inválido/alheio). `convenio_id` é **opcional** mesmo em convênio (recepção pode confirmar a chegada sem ter o convênio à mão — fica NULL, registrável depois não é escopo). Mora em: Domain (`Cobranca.CriarParaConsulta` ganha param `convenioId`) + Handler. Validada em: back + front.
- **R8** (pré-seleção por carteirinha): no check-in, se o paciente tem carteirinha ativa cadastrada e o tipo é Convênio, o convênio dela é **pré-selecionado** e mostra número/validade. Se tem N carteirinhas ativas, pré-seleciona a de validade mais distante (ou a única); recepção pode trocar. Mora em: Front (UX de conveniência) + Query (lista carteirinhas do paciente). Validada em: front.
- **R9** (R12 da F1 intacta — convênio sem balcão): cobrança convênio continua **sem** pagamento de balcão e **sem** estorno (`Cobranca.RegistrarPagamento`/`EstornarPagamento` já lançam 422 para `Convenio` — Cobranca.cs:255-257,302-304). F6 **não** altera isso. `valor_cobrado` permanece 0. Mora em: Domain (inalterado). Validada em: back (regressão).
- **R10** (guia na cobrança — estado pendente/preenchida): cobrança convênio ganha 3 colunas em `cobrancas`: `guia_numero` (texto), `guia_senha` (texto), `guia_autorizada_em` (date). Estado é **derivado**: "preenchida" se `guia_numero` presente; "pendente" caso contrário. Preencher/editar guia é uma operação de comando própria (não passa por pagamento). Guia só faz sentido em cobrança `tipo=Convenio` — comando rejeita (422 genérico) se a cobrança é Particular. Mora em: Domain (`Cobranca.RegistrarGuia(numero, senha?, autorizadaEm?)` + validação de tipo) + Handler. Validada em: back + front.
- **R11** (RBAC cadastro de convênios): CRUD de `Convenio`/`ConvenioPlano` exige `convenios.gerenciar`. Listar (para o select do check-in e a tela read-only) exige `convenios.ver`. Já no catálogo (`CatalogoPermissoes.cs:30`) — **sem mudança de catálogo**. Recepção já tem `convenios.ver`. Mora em: Handler (gate) + Front (botões ocultos sem permissão). Validada em: back (403) + front (oculta).
- **R12** (RBAC carteirinha): **ver** carteirinha do paciente exige `pacientes.ver` (a aba Convênios é dado do paciente); **editar/criar/excluir** carteirinha exige `pacientes.editar` (D2). Mora em: Handler (gate) + Front. Validada em: back (403) + front (oculta).
- **R13** (RBAC guia): preencher/editar guia na cobrança convênio exige `financeiro_paciente.registrar` — mesma permissão que registra pagamento na F2; guia é o equivalente operacional de "registrar" no fluxo convênio (D3). Mora em: Handler (gate) + Front. Validada em: back (403) + front (oculta).
- **R14** ("em breve" desabilitado): os cards Coparticipação / Conciliação / Glosas são UI estática com selo "Em breve", **sem** ação clicável, sem chamada de rede, sem schema. Mora em: Front (estado visual). Validada em: front.
- **R15** (LGPD — carteirinha é dado pessoal): `numero_carteirinha` é dado pessoal do paciente. DTO minimizado (só campos da tela). Sem PII em log/erro (mensagens genéricas). A aba Convênios do paciente, por ser porta direta a dado pessoal por paciente identificado, registra audit de Leitura (D4). Mora em: Query (audit) + DTO. Validada em: back + front.

## 5. Modelo de dados

> Schema autorado pelo `imedto-database`. Tudo multi-tenant (`estabelecimento_id`), índices dia 1.

**Tabela nova `convenios`**
- `id` (PK, identity), `estabelecimento_id` (FK, NOT NULL), `nome` (text, NOT NULL), `registro_ans` (text, NULL), `ativo` (bool, NOT NULL default true), `criado_em`, `atualizado_em` (NULL).
- Índice: `(estabelecimento_id, ativo)` — listagem do select do check-in e tela. Multi-tenant em todas as queries.

**Tabela nova `convenio_planos`**
- `id` (PK), `convenio_id` (FK → convenios, NOT NULL, `ON DELETE CASCADE`), `estabelecimento_id` (FK, NOT NULL — denormalizado para filtro multi-tenant direto), `nome` (text, NOT NULL), `ativo` (bool, NOT NULL default true), `criado_em`.
- Índice: `(convenio_id, ativo)`.

**Tabela nova `paciente_convenios`**
- `id` (PK), `paciente_id` (FK, NOT NULL), `estabelecimento_id` (FK, NOT NULL), `convenio_id` (FK → convenios, NOT NULL), `plano_id` (FK → convenio_planos, NULL), `numero_carteirinha` (text, NOT NULL), `validade` (date, NULL), `ativo` (bool, NOT NULL default true), `criado_em`, `atualizado_em` (NULL).
- Índice: `(paciente_id, estabelecimento_id, ativo)` — lista carteirinhas do paciente; e `(convenio_id)` para impedir exclusão de convênio em uso (R3).
- **PII**: `numero_carteirinha` é dado pessoal — DTO minimizado, sem PII em log.

**ALTER em `cobrancas`** (tabela existente da F1)
- `+ guia_numero` (text, NULL), `+ guia_senha` (text, NULL), `+ guia_autorizada_em` (date, NULL).
- `convenio_id` **já existe** (reservado na F1) — F6 apenas passa a populá-lo. **Nenhum ALTER para `convenio_id`.**
- FK `cobrancas.convenio_id → convenios.id` (`ON DELETE SET NULL` — fraca; convênio inativado/excluído não apaga histórico de cobrança).

**Sem schema para "em breve"** (D7): Coparticipação/Conciliação/Glosa não ganham tabela — schema só quando a funcionalidade vier.

## 6. UX e fluxo

**Decisão de navegação D1 — Convênios em Configurações:**
O `EstabelecimentoView.vue` já é o master-detail de Configurações com sub-nav por grupos (deep-link `?secao=`). Já existe o grupo **"Financeiro"** com a seção `financeiro` (EstabelecimentoView.vue:100-105). Para seguir o padrão da casa e a navegação real do app (o protótipo coloca Convênios no grupo "Faturamento"): **adicionar uma seção `convenios` ao grupo "Financeiro" e renomear o label do grupo para "Faturamento"** (passa a conter `financeiro` + `convenios`). Visibilidade da seção `convenios`: `convenios.ver`. O conteúdo da seção é a tela CRUD de convênios (lista + drawer), montada como painel async no padrão `defineAsyncComponent` já usado para `FinanceiroConfigView`. **Não criar rota top-level nova** — encaixa no master-detail existente, espelhando o `?secao=` bidirecional.

**Tela CRUD de convênios** (seção `convenios` de Configurações — protótipo `cv-cadastro.png`/`cv-drawer.png`):
- `AppPageHeader` "Convênios" + botão "Novo convênio" (oculto sem `convenios.gerenciar`).
- Lista: cards/linhas com nome, registro ANS, nº de planos, badge ativo/inativo. `AppEmptyState` quando vazio ("Nenhum convênio cadastrado").
- Drawer (`AppDrawer`) de criar/editar: campo nome, registro ANS (opcional), toggle ativo, e sub-lista de planos (adicionar/remover/inativar plano inline). Estados: loading / erro / vazio / sucesso.
- Inativar convênio = toggle ativo (R3). Excluir só habilitado se sem uso (R3) — senão 422 genérico exibido inline.

**Aba Convênios do paciente** (PacienteDetalheView.vue — substitui o empty state atual em :1107-1112; protótipo `cv-patient.png`):
- Lista de carteirinhas (`CarteirinhaCard`): convênio + plano, número, validade. Carteirinha vencida (R6) com tarja/badge de alerta visual ("Carteirinha vencida"). Carteirinha inativa esmaecida.
- Botão "Adicionar convênio" (oculto sem `pacientes.editar`) → drawer/modal de cadastro: select convênio (ativos do estabelecimento), select plano (do convênio escolhido, opcional), nº carteirinha, validade (opcional), toggle ativo.
- Editar/excluir por carteirinha (oculto sem `pacientes.editar`).
- `AppEmptyState` quando paciente sem carteirinha ("Nenhum convênio cadastrado para este paciente").
- **Cards "Em breve"** (R14, protótipo `cv-soon.png`): abaixo das carteirinhas, três cards desabilitados — Coparticipação, Conciliação de repasse, Glosas — com selo "Em breve", sem clique, sem rede.

**Check-in** (CheckInModal.vue — estende a seção Atendimento existente):
- Quando `tipoAtendimento === 'Convenio'`: substituir a hint estática atual (CheckInModal.vue:316-319 "Cobrança de convênio será tratada na aba financeiro.") por um `AppSelect` de convênio listando os **ativos** do estabelecimento (carregado lazy ao abrir, como salas).
- Se o paciente tem carteirinha ativa: pré-seleciona o convênio (R8) e mostra número/validade abaixo; alerta visual se vencida (R6 — não bloqueia).
- Convênio segue **opcional** (pode confirmar sem selecionar — fica NULL). Particular **intacto** (regressão — R9/CA17).
- `registrarCheckIn` passa a enviar `convenioId` no payload de cobrança (estende `RegistrarCheckInAgendamentoCommand` com `long? ConvenioId`).

**Cobrança convênio na aba Financeiro** (FinanceiroTab.vue — estende o bloco convênio existente em :385-389):
- Mantém "Faturada ao convênio — sem pagamento de balcão." (F2/CA38 intacto).
- Adiciona seção **Guia / Autorização** no lugar do fluxo de pagamento (protótipo `02-cv-guia-filled.png`/`cv-guia-modal.png`):
  - Estado **pendente** (sem `guia_numero`): badge "Guia pendente" + botão "Preencher guia" (oculto sem `financeiro_paciente.registrar`).
  - Estado **preenchida**: exibe nº guia, senha, data autorização + botão "Editar guia" (mesmo gate).
  - Form (modal): nº guia (obrigatório), senha de autorização (opcional), data (opcional). Estados loading/erro/sucesso.

Componentes reusados: `AppDrawer`, `AppSelect`, `AppPillToggle`, `AppEmptyState`, `AppBadge`, `AppModal`, `AppButton`, `AppField`. Mobile-ready responsivo. Sem componente novo de design system previsto.

## 7. Critérios de aceite (testáveis)

- **CA131** (CRUD convênio — caminho feliz): Dado um usuário com `convenios.gerenciar` no estabelecimento E, Quando cria um convênio "Unimed" com plano "Nacional" e salva, Então o convênio e o plano são persistidos com `estabelecimento_id=E`, `ativo=true`, e aparecem na lista da seção Convênios.
- **CA132** (convênio multi-tenant): Dado um usuário do estabelecimento B, Quando tenta editar/excluir um `convenio_id` do estabelecimento A, Então recebe 404 genérico ("Convênio não encontrado."), nada é alterado e nenhuma PII/tenant alheio é logado.
- **CA133** (RBAC gerenciar convênio): Dado um usuário com `convenios.ver` mas sem `convenios.gerenciar`, Quando chama o endpoint de criar/editar/excluir convênio, Então recebe 403 e o botão "Novo convênio"/editar fica oculto no front.
- **CA134** (inativar > excluir — R3): Dado um convênio já referenciado por uma carteirinha ou cobrança, Quando o usuário tenta excluí-lo fisicamente, Então recebe 422 genérico ("Convênio em uso — inative em vez de excluir.") e o convênio permanece; Quando em vez disso o inativa (`ativo=false`), Então some das novas seleções (select do check-in/cadastro de carteirinha) mas continua legível no histórico.
- **CA135** (nome obrigatório — R2): Dado o form de convênio com nome vazio, Quando salva, Então o back retorna 422 genérico e o front bloqueia o submit; `registro_ans` vazio é aceito.
- **CA136** (carteirinha CRUD — caminho feliz): Dado um usuário com `pacientes.editar`, Quando adiciona ao paciente uma carteirinha (convênio Unimed, plano Nacional, número 123, validade 2027-01-01), Então é persistida com `paciente_id`+`estabelecimento_id`+`convenio_id`+`plano_id`, `ativo=true`, e aparece na lista da aba Convênios do paciente.
- **CA137** (carteirinha N por paciente): Dado um paciente que já tem 1 carteirinha, Quando adiciona uma segunda de outro convênio, Então ambas coexistem ativas na lista.
- **CA138** (carteirinha — plano deve pertencer ao convênio — R5): Dado o cadastro de carteirinha com `plano_id` que não pertence ao `convenio_id` selecionado, Quando salva, Então o back retorna 422 genérico e a carteirinha não é criada.
- **CA139** (alerta de validade — R6, não bloqueia): Dado uma carteirinha com `validade` anterior a hoje, Quando a aba Convênios do paciente carrega, Então a carteirinha exibe alerta visual "Carteirinha vencida"; Quando essa carteirinha é usada no check-in, Então o check-in mostra o alerta mas **não** bloqueia a confirmação.
- **CA140** (RBAC ver carteirinha — R12): Dado um usuário com `pacientes.ver` mas sem `pacientes.editar`, Quando abre a aba Convênios do paciente, Então vê as carteirinhas (lista carrega) mas os botões adicionar/editar/excluir ficam ocultos; Quando chama o endpoint de criar/editar carteirinha, Então recebe 403.
- **CA141** (carteirinha multi-tenant): Dado um usuário do estabelecimento B, Quando tenta cadastrar carteirinha apontando `convenio_id` do estabelecimento A, Então recebe 404 genérico e nada é criado.
- **CA142** (check-in convênio com select real + grava `convenio_id` — R7): Dado o check-in com tipo Convênio e um convênio ativo selecionado, Quando confirma, Então a `Cobranca` nasce `tipo=Convenio`, `convenio_id` setado, `valor_cobrado=0`, badge "Convênio".
- **CA143** (check-in pré-seleção por carteirinha — R8): Dado um paciente com carteirinha ativa de Unimed, Quando a recepção abre o check-in e seleciona tipo Convênio, Então o convênio Unimed é pré-selecionado e número/validade são exibidos; a recepção pode trocar para outro convênio ativo.
- **CA144** (check-in convênio opcional): Dado o check-in com tipo Convênio e **nenhum** convênio selecionado, Quando confirma, Então o check-in é registrado e a `Cobranca` nasce `tipo=Convenio`, `convenio_id` NULL, `valor_cobrado=0` (não bloqueia).
- **CA145** (regressão — check-in particular intacto): Dado o check-in com tipo Particular e valor R$ 200, Quando confirma, Então a `Cobranca` nasce `tipo=Particular`, `valor_cobrado=200`, `convenio_id` NULL, exatamente como na F1 (nenhum campo de convênio exigido).
- **CA146** (regressão — R12/CA17 F1 intacta, convênio sem balcão): Dado uma cobrança `tipo=Convenio`, Quando o usuário tenta registrar pagamento de balcão ou estorno, Então recebe 422 genérico ("Pagamento de balcão não disponível para cobranças de convênio." / estorno idem) — comportamento da F1/F2 inalterado.
- **CA147** (guia — estado pendente/preenchida — R10): Dado uma cobrança convênio sem guia, Quando a aba Financeiro carrega, Então exibe "Guia pendente" + botão "Preencher guia"; Quando o usuário preenche nº guia "G-1", senha "ABC", data 2026-06-15 e salva, Então a cobrança grava `guia_numero`/`guia_senha`/`guia_autorizada_em` e a aba passa a exibir os dados + botão "Editar guia".
- **CA148** (guia só em convênio — R10): Dado uma cobrança `tipo=Particular`, Quando se tenta chamar o comando de registrar guia, Então recebe 422 genérico e nada é gravado; no front a seção Guia não aparece para cobrança Particular.
- **CA149** (RBAC guia — R13): Dado um usuário sem `financeiro_paciente.registrar`, Quando chama o endpoint de registrar guia, Então recebe 403 e os botões "Preencher/Editar guia" ficam ocultos no front.
- **CA150** ("em breve" visível e desabilitado — R14): Dado a aba Convênios do paciente, Quando carrega, Então os cards Coparticipação, Conciliação de repasse e Glosas aparecem com selo "Em breve", sem ação clicável e sem disparar nenhuma request de rede.
- **CA151** (LGPD — audit da aba Convênios — R15/D4): Dado um usuário com `pacientes.ver`, Quando abre a aba Convênios do paciente, Então 1 linha é registrada em `paciente_acesso_log` via `IPacienteAcessoLogService.RegistrarAsync(paciente_id, usuario_id, estabelecimento_id, Leitura)` (best-effort — falha do log não quebra a aba).
- **CA152** (LGPD — sem PII em erro): Dado qualquer erro de validação de convênio/carteirinha/guia, Quando o back retorna 422/404, Então a mensagem é genérica e não contém nome/CPF do paciente nem `numero_carteirinha`.
- **CA153** (estados — vazio): Dado um estabelecimento sem convênios cadastrados, Quando abre a seção Convênios, Então mostra `AppEmptyState` "Nenhum convênio cadastrado"; Dado um paciente sem carteirinha, a aba Convênios mostra `AppEmptyState` específico.
- **CA154** (performance / foco): Dado a aba Convênios do paciente não clicada, Quando o paciente é aberto, Então nenhuma query de carteirinha é disparada (lazy-load por aba, padrão `PacienteDetalheView`); o select de convênios do check-in carrega lazy ao abrir o modal (padrão das salas), não no boot.
- **CA155** (doc viva atualizada): Dado a entrega da F6, Quando o QA revisa, Então `Docs/ARQUITETURA.md` (bounded context Convênio / fluxo `convenio_id` no check-in / guia na cobrança) e `Docs/LGPD.md` (carteirinha como dado pessoal + audit da aba) foram atualizados.

## 8. Riscos e dependências

- **Dependência F1**: `Cobranca.convenio_id` já reservado (Cobranca.cs:28) e `tipo=Convenio` já cria cobrança sem balcão. F6 popula o que a F1 deixou inerte — risco baixo de regressão se `CriarParaConsulta` ganhar param opcional `convenioId` sem mudar o ramo Particular.
- **Risco regressivo — check-in**: a F6 mexe no `RegistrarCheckInAgendamentoCommand`/Handler e no `CheckInModal`. Particular **não** pode quebrar (CA145). O ramo `valor_cobrado=0` do convênio deve permanecer (R9).
- **Risco regressivo — aba Financeiro**: a F2 já renderiza cobrança convênio read-only (FinanceiroTab.vue:385-389). A seção Guia é **aditiva** — não tocar o bloco "Faturada ao convênio" (CA38 intacto).
- **Risco — navegação Configurações**: renomear o grupo "Financeiro" → "Faturamento" e adicionar seção `convenios` é mudança no `GRUPOS_NAV` do `EstabelecimentoView`; manter o deep-link `?secao=financeiro` funcionando (não renomear o `id` da seção, só o label do grupo).
- **Risco — exclusão de convênio em uso**: a checagem de uso (R3) precisa olhar `paciente_convenios` **e** `cobrancas` antes de permitir exclusão física. Índice `(convenio_id)` em ambas dá suporte.
- **Sem dependência das fases avançadas**: "em breve" é UI pura, não cria acoplamento.

## 9. Observações para execução

- **Não-negociável**: multi-tenant em toda query/comando de convênio/carteirinha/guia (filtro `estabelecimento_id`, 404 genérico para tenant alheio). R9 (convênio sem balcão) e CA17/CA38 da F1/F2 **intactos**. Carteirinha minimizada e auditada (R15/CA151). Particular intacto (CA145).
- **Reuso obrigatório**: `IPacienteAcessoLogService.RegistrarAsync(...Leitura)` para o audit da aba (idêntico ao F2/F8 — **sem** tabela/serviço de audit novo). `AppDrawer`/`AppSelect`/`AppEmptyState`/`AppPillToggle` do DS. Padrão lazy-load por aba do `PacienteDetalheView`. Padrão master-detail + `?secao=` do `EstabelecimentoView`. Padrão `defineAsyncComponent` para o painel de Convênios em Configurações. `Cobranca.CriarParaConsulta` **estendida** (param `convenioId` opcional), não duplicada.
- **Liberdade técnica do dev/db**: nome exato das colunas/índices, organização dos handlers (BA sugere um aggregate `Convenio` com `ConvenioPlano` filho; `PacienteConvenio` pode ser aggregate próprio), forma do DTO da seção Convênios em Configurações.
- **Acionar `imedto-database`**: 3 tabelas novas (`convenios`, `convenio_planos`, `paciente_convenios`) + ALTER `cobrancas` (3 colunas de guia + FK `convenio_id → convenios` ON DELETE SET NULL). **Nenhum ALTER para `convenio_id`** (já existe). Sem schema para "em breve".
- **Catálogo de permissões**: **não muda** — área `convenios (ver/gerenciar)` já existe (CatalogoPermissoes.cs:30). Carteirinha reusa `pacientes.ver`/`pacientes.editar`; guia reusa `financeiro_paciente.registrar`.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — adicionar, na seção de Cobranças/Financeiro (próximo aos blocos das F4/F5 já documentados, linhas ~309+), um parágrafo "Convênio: estrutura base (briefing 2026-06-10_016 — F6)" descrevendo: o aggregate `Convenio` (+`ConvenioPlano` filho) e `PacienteConvenio` (carteirinha), o populamento de `cobrancas.convenio_id` no check-in via `CriarParaConsulta(convenioId)`, a operação `Cobranca.RegistrarGuia` (colunas `guia_numero`/`guia_senha`/`guia_autorizada_em` na própria `cobrancas`, estado derivado pendente/preenchida), o soft-delete (inativar > excluir) de convênio, e que coparticipação/conciliação/glosa são "em breve" sem schema. Registrar a decisão de navegação (seção `convenios` no grupo "Faturamento" do master-detail de Configurações, reuso de `?secao=`).
- **`Docs/LGPD.md`** — adicionar, na seção "Dado financeiro do paciente — sensível" (linha ~131) ou logo após, um bloco "Carteirinha de convênio — dado pessoal (briefing 2026-06-10_016, F6)": `paciente_convenios.numero_carteirinha` é dado pessoal do paciente; DTO minimizado; sem PII em log/erro (mensagens genéricas); a aba Convênios do paciente registra 1 linha por carga em `paciente_acesso_log` (`TipoAcessoPaciente.Leitura`) via `IPacienteAcessoLogService` (best-effort — reuso do serviço existente, sem tabela/serviço novo). Guia (`guia_numero`/`guia_senha`) é metadado operacional de faturamento — sem PII de paciente.
- **`Docs/DESIGN.md`** — não muda (sem componente novo de DS; reuso de `AppDrawer`/`AppSelect`/`AppEmptyState`).
- **`Docs/COMANDOS.md`** / **`Docs/INFRA.md`** — não mudam (sem comando/recurso novo; migration segue fluxo padrão).
