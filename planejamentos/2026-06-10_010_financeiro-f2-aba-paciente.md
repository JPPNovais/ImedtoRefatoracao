# Módulo Financeiro / Cobranças — F2: Aba Financeiro do paciente (+ Estorno)

**ID**: 2026-06-10_010
**Status**: Aprovado por usuário em 2026-06-10 (execução autônoma autorizada — sem rodada de `AskUserQuestion`; decisões de corte tomadas pelo BA e registradas em §9)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M (do plano mestre)
**Áreas regressivas tocadas**: prontuário/página do paciente (abas do `PacienteDetalheView.vue` — regressão crítica), financeiro (`Cobranca`/`Pagamento` ganham estorno; `Lancamento` ganha lançamento de estorno), agenda/check-in (repercussão bidirecional do status — regressão a vigiar), permissionamento (reuso de `financeiro_paciente.*` da F1, sem permissão nova)

> **Fonte de verdade da visão**: `Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md` (§F2, §2.1 `EstornoPagamento`, §2.3 INV-7, §1.4 LGPD/audit, §3.4 histórico de valor da cirurgia). Este briefing é a fonte de verdade da **execução da F2**. É **imutável** — gap vira addendum.
> **Fundação F1 (IMPLEMENTADA)**: `planejamentos/2026-06-10_009_financeiro-f1-fundacao-consulta.md`. A F2 plugа sobre o domínio já entregue: agregados `Cobranca`/`Pagamento`, `PaymentModal.vue`, `cobrancaService.ts`, `cobrancaStore.ts`, permissões `financeiro_paciente.{ver,registrar}`, `CobrancaController`. **Não reabrir** decisões da F1.
> **Referência visual obrigatória**: `Docs/Roadmap/prototipacao-financeiro/design-handoff/` — tela F2/F8 (ver §6 e §11).

---

## 1. Contexto e motivação

A F1 entregou a **primeira porta** para a cobrança do paciente: o ícone/badge na agenda, na consulta particular. Mas o protótipo e a operação real da clínica pedem uma **segunda porta consolidada por paciente** — a aba **Financeiro** dentro da página do paciente (`PacienteDetalheView.vue`), hoje um empty state "em breve".

É aqui que recepção/dono/profissional veem **tudo o que o paciente deve e pagou** num só lugar — consulta, procedimento, cirurgia (os 3 tipos de origem) — independentemente de qual agendamento gerou cada cobrança. É a tela natural para **estornar** um pagamento (a F1 cortou o estorno de propósito, deixando o domínio preparado; a F2 o entrega).

Princípio-âncora do plano (§1.6 "duas portas, uma cobrança"): a aba e o ícone da agenda operam **a MESMA** `Cobranca`. Registrar pagamento numa porta repercute na outra **sem duplicidade**. A F2 **não cria domínio novo de cobrança/pagamento** — reusa o da F1. A única entidade nova é o **`EstornoPagamento`** (INV-7).

Conceito que a F2 acrescenta ao módulo: **`cobrado − pago − estornado = saldo`**. O estorno é histórico, nunca apaga — o pagamento original fica imutável e riscado; o status da cobrança recalcula pela **soma líquida** (já preparado na F1).

## 2. Persona-alvo

- **Recepção / Dono / Profissional** (com `financeiro_paciente.ver`): abre a página do paciente → aba **Financeiro** para conferir saldo, histórico de pagamentos e, na cirurgia, histórico de valor. Uso recorrente (atendimento de retorno, cobrança pendente, conferência).
- **Quem tem `financeiro_paciente.registrar`**: registra pagamento e **estorna** pagamento (com motivo) direto da aba.
- **Usuário sem `financeiro_paciente.ver`**: vê a aba em **estado restrito** (gate do protótipo `fin-restricted.png`) — nada de dado financeiro.

Momento da jornada: **página do paciente** (retorno, conferência de pendência, fechamento). Diferente da F1, que era o balcão da agenda.

## 3. Escopo

**Inclui (IN)**:
- **Implementar a aba Financeiro** em `PacienteDetalheView.vue` (hoje empty state) — **lazy-load**: a consulta só dispara quando a aba é clicada (premissa de performance da casa; aba não clicada = zero request).
- **3 KPIs** no topo: **Total cobrado**, **Total pago** (líquido de estorno), **Saldo** (cobrado − desconto − pago + estornado), do paciente no tenant ativo.
- **Lista de cobranças** do paciente (tenant+paciente), dos **3 tipos de origem** (`Consulta | Procedimento | Cirurgia`), com **expansão** por cobrança (card `ChargeCard` do protótipo): histórico de pagamentos, histórico de estornos, e — **para origem=Cirurgia** — histórico de alteração de valor (de→para, quem, quando).
- **Registrar pagamento** reusando o **`PaymentModal` da F1** (`frontend/src/components/ui/PaymentModal.vue`) — **mesma** `Cobranca`, mesmo endpoint da F1; repercute na agenda sem duplicidade (porta única de domínio).
- **ESTORNO (INV-7)** — entidade nova `EstornoPagamento` + `Lancamento` de estorno **atômico**:
  - Motivo **obrigatório** (texto livre, sem PII).
  - Pagamento original **imutável** e exibido **riscado/atenuado**; linha de estorno destacada (vermelho) com motivo.
  - `status` da cobrança **recalculado por soma líquida** (pagamentos − estornos) via INV-2 (já preparado na F1).
  - Estorno **total** do pagamento (1 estorno = anula 1 `Pagamento` por inteiro) — ver decisão de corte §9.
- **Audit de acesso à aba** no padrão `paciente_acesso_log` — **reusa `IPacienteAcessoLogService.RegistrarAsync(... TipoAcessoPaciente.Leitura ...)`** já existente; **não cria tabela/serviço de audit novo** (decisão de corte §9).
- **Gate de acesso** por `financeiro_paciente.ver` → estado restrito do protótipo (`fin-restricted.png`) quando ausente.
- **Estados**: loading, vazio (paciente sem cobrança), erro.
- **Cobrança de convênio** aparece na lista com **tag "Convênio"** (sem fluxo de guia — F6) e **sem** oferecer pagamento de balcão (mesma regra R12 da F1).

**Não inclui (OUT)**:
- **Recibo de pagamento (PDF)** → **F8** (não há botão/ação de recibo nesta fase).
- **Guia de convênio / autorização** → **F6** (cobrança de convênio só aparece com tag, read-only).
- **Redesign do `/financeiro`** (clínica agregada) → **F7**.
- **Gerar cobrança** de procedimento (F4) ou cirurgia (F5) — a F2 **só lê e opera** cobranças já existentes; não há criação de cobrança de procedimento/cirurgia aqui. (Cobranças de cirurgia que existirem na base já aparecem; sua criação é F5.)
- **Edição de valor da cobrança de cirurgia** (de→para) → a F2 **exibe** o histórico de valor se existir, mas **não edita** valor (criação/edição do histórico é F5).
- **Estorno parcial** de um pagamento (anti-escopo desta versão — §9).

## 4. Regras de negócio

> Toda regra com cálculo/invariante mora no **Domain** (agregado `Cobranca` + `EstornoPagamento` como entidade filha de `Pagamento`/`Cobranca` + métodos puros) e é validada no **back (422 `BusinessException`)**; a trava de front é UX com espelho obrigatório. Cálculo monetário **sempre no backend** (helper único de arredondamento da F1) — o front só faz preview.

- **R1 — Porta única de domínio (não duplicar).** Registrar pagamento pela aba usa **o mesmo** comando/endpoint da F1 (`CobrancaController` / `RegistrarPagamento`) sobre a **mesma** `Cobranca`. Proibido criar lógica de cálculo/cobrança paralela na F2. Mora em: reuso de Handler/Domain da F1 + Front (`cobrancaStore.ts`/`cobrancaService.ts`). Validada em: back + front.
- **R2 — Lazy-load da aba.** A query da aba Financeiro **só dispara ao clicar na aba** (não no load da página do paciente, não em aba não selecionada). Mora em: Front (`PacienteDetalheView.vue`). Validada em: front (CA de lazy-load).
- **R3 — Saldo e KPIs derivados, no backend.** `saldo = valor_cobrado − desconto − SUM(pagamentos.valor) + SUM(estornos.valor)`. `total_pago_liquido = SUM(pagamentos.valor) − SUM(estornos.valor)`. Calculados no backend (query/domínio), nunca no front. Mora em: Query (agregação) + Domain (saldo da cobrança). Validada em: back.
- **R4 — INV-7 (estorno atômico com histórico).** Estornar um `Pagamento` **gera um `EstornoPagamento` + um `Lancamento` de estorno na MESMA transação** (atômico — ou os dois persistem, ou nenhum). **Nunca** apaga/edita o `Pagamento` nem o `Lancamento` originais. Falha em qualquer um faz rollback de ambos. Mora em: Handler (UnitOfWork/transação) + Domain (`Cobranca.EstornarPagamento` / `Pagamento.Estornar`). Validada em: back (teste de rollback obrigatório).
- **R5 — Motivo de estorno obrigatório, sem PII.** Estorno exige `motivo` não-vazio. Mensagem/motivo **não** carrega PII em log de domínio. Mora em: Domain (valida `motivo` não-vazio → 422) + Front (campo obrigatório). Validada em: back + front.
- **R6 — Pagamento imutável (já garantido na F1).** O `Pagamento` original e seu `Lancamento` permanecem intactos após estorno (sem update/delete de valor). A F2 **não** introduz mutação no `Pagamento`; só acrescenta `EstornoPagamento`. Mora em: Domain. Validada em: back.
- **R7 — Status recalculado por soma líquida (INV-2 sobre líquido).** Após estorno, `status` recalcula: soma líquida = 0 → `Aberta`; 0 < líquida < total → `ParcialmentePaga`; líquida = total → `Paga`. Nunca setado à mão (salvo `Cancelada`). Mora em: Domain. Validada em: back.
- **R8 — Não estornar além do pago.** Não é possível estornar um `Pagamento` que já foi estornado (1 estorno total por pagamento — §9). `SUM(estornos.valor) ≤ SUM(pagamentos.valor)`. Excesso/duplicidade → 422. Mora em: Domain. Validada em: back + front (botão de estorno some/desabilita em pagamento já estornado).
- **R9 — RBAC: ver ≠ registrar/estornar.** `financeiro_paciente.ver` libera **leitura** (aba, KPIs, lista, expansão). `financeiro_paciente.registrar` libera **registrar pagamento E estornar**. Sem `ver` → estado restrito (nada de dado). Sem `registrar` → vê tudo mas **sem** botões de pagar/estornar; back retorna 403/422 se chamado direto. Mora em: Handler (checagem) + Front (oculta ações). Validada em: back + front.
- **R10 — Audit de acesso à aba (LGPD).** **Todo** carregamento da aba Financeiro (leitura de dado financeiro vinculado a paciente identificado) registra uma linha em `paciente_acesso_log` via `IPacienteAcessoLogService.RegistrarAsync(pacienteId, usuarioId, estabelecimentoId, TipoAcessoPaciente.Leitura)`. Best-effort (falha do log não quebra o fluxo — comportamento já existente do serviço). Mora em: Handler da query da aba. Validada em: back.
- **R11 — Multi-tenant falha-fechada.** Toda query/comando de `Cobranca`/`Pagamento`/`EstornoPagamento` na F2 filtra `estabelecimento_id` do tenant ativo + `paciente_id`. Sem tenant claim → vazio/throws. Acesso a cobrança/pagamento de outro tenant (id direto) → 404/"não encontrado" genérico. Mora em: Repositório + Handler. Validada em: back.
- **R12 — Convênio read-only na aba (R12 da F1 mantida).** Cobrança `tipo_atendimento=Convenio` aparece na lista com tag "Convênio", **sem** botão de registrar pagamento de balcão e **sem** estorno (não há pagamento de balcão). Mora em: Domain (não oferece pagamento) + Front (tag + sem ações). Validada em: back + front.
- **R13 — DTO mínimo (LGPD).** O DTO da aba traz **só** os campos da tela (cobranças/pagamentos/estornos do paciente no tenant) — nenhum dado clínico (CID/diagnóstico), nenhuma PII além do necessário para a tela. Mora em: Query/DTO. Validada em: back.

## 5. Modelo de dados (resumo — detalhe para o DB agent em §10)

- **Nova tabela `estorno_pagamentos`** (entidade filha; histórico imutável):
  - `id`, `pagamento_id` (FK `pagamentos`), `cobranca_id` (FK `cobrancas` — desnormalizado p/ filtro tenant/agregação), `estabelecimento_id` (tenant), `valor numeric(12,2)` (= valor do pagamento estornado, já que estorno é total nesta versão), `motivo text NOT NULL`, `lancamento_estorno_id` (FK `lancamentos`), `estornado_por_usuario_id uuid`, `data_estorno`, `criado_em`.
- **`lancamentos` (existente, já alterada na F1)**: a F1 adicionou `cobranca_id`/`pagamento_id`. O **lançamento de estorno** reusa essas colunas (`cobranca_id` + `pagamento_id` do pagamento estornado) e é distinguido por **valor negativo** + `categoria`/`tipo` de estorno — **decisão de modelagem do estorno no `Lancamento` em §10** (decisão tomada: valor negativo + categoria, sem coluna/flag nova — ver §9/§10).
- **Sem outras tabelas novas.** Cobranças/pagamentos/preços/taxa são da F1. Audit reusa `paciente_acesso_log` (existente). Histórico de valor da cirurgia é da F5 (a F2 só lê se existir — não cria tabela).
- **Multi-tenant**: `estorno_pagamentos` tem `estabelecimento_id` + índice por tenant; FKs `ON DELETE RESTRICT` (dado financeiro não cascateia).
- **LGPD**: `motivo` é texto operacional — orientação ao dev: não colar PII; DTOs mínimos; sem PII em log.

## 6. UX e fluxo

**Referência visual obrigatória** (recriar o resultado, não copiar HTML; tokens tipográficos vencem o protótipo — CLAUDE.md §5):
- Aba Financeiro: `design-handoff/PacienteDetalhe.html` → `components/PatientDetail.jsx` (TabFinanceiro), `components/PatientDetailExtra.jsx` (`ChargeCard`, `EstornoModal`).
- Screenshots: `fin-tab.png` (lista + KPIs), `02-fin-cirurgia.png` (expansão de cirurgia com histórico de valor), `02-fin-estorno.png` (estorno: original riscado + linha vermelha + motivo), `fin-restricted.png` (gate sem permissão).

**6.1 Aba Financeiro (`PacienteDetalheView.vue` — substitui o empty state)**
- Entrada lazy: ao **clicar na aba**, dispara a query (`R2`). Antes disso, zero request.
- **Topo — 3 KPIs**: Total cobrado · Total pago (líquido) · Saldo. Reusar componentes de card/KPI do DS (não criar tela órfã; se não houver KPI card no DS, usar `app-page`/cards existentes do padrão do produto).
- **Lista de cobranças** (`ChargeCard`), uma por `Cobranca`, com **tag de origem** (Consulta/Procedimento/Cirurgia) e **tag Convênio** quando aplicável. Cada card mostra: descrição, valor cobrado, desconto, pago, saldo, status (badge — reusar estados da F1).
- **Expansão** do card: histórico de **pagamentos** (forma, valor, data, taxa informativa), histórico de **estornos** (valor, motivo, quem, quando — em vermelho), e, **só para Cirurgia**, histórico de **valor** (de→para, quem, quando) — read-only (F5 popula; F2 exibe se houver).
- **Ações por cobrança** (só com `financeiro_paciente.registrar`):
  - **Registrar pagamento** → abre o **`PaymentModal` da F1** (mesma cobrança). Convênio: sem ação (R12).
  - **Estornar** (por linha de pagamento não estornado) → abre `EstornoModal`: campo **motivo obrigatório** + confirmação. Ao confirmar: estorno atômico, original riscado, status recalcula, KPIs atualizam.
- **Estados**: loading (skeleton/spinner do DS), vazio (`AppEmptyState` "Nenhuma cobrança para este paciente"), erro (mensagem genérica + retry), **restrito** (sem `ver` → bloco do protótipo `fin-restricted.png`, sem dado).
- **Regressão**: as demais abas do paciente (prontuário, dados, documentos, etc.) continuam **idênticas**; a aba Financeiro não compartilha estado/carga com elas (lazy isolado).

**6.2 EstornoModal (componente da F2 — reusar padrão de modal do DS)**
- Cabeçalho: "Estornar pagamento" + resumo do pagamento (valor, forma, data — sem PII clínica).
- Campo **Motivo** (`AppField` + textarea), **obrigatório** (botão confirmar desabilitado até preencher).
- Aviso de irreversibilidade ("O pagamento original será mantido e marcado como estornado").
- Estados: loading (durante a transação), erro 422 (mensagem genérica do back), sucesso (fecha + atualiza lista/KPIs/badge).

## 7. Critérios de aceite (testáveis)

> Numeração continua a partir do briefing original da F1 (CA1–CA22). A F2 começa em **CA23**.

- **CA23 (caminho feliz — aba lista cobranças do paciente)**: Dado um paciente com 1 cobrança de consulta (R$ 200, R$ 80 pagos) no tenant ativo, Quando o usuário com `financeiro_paciente.ver` clica na aba Financeiro, Então a aba mostra os 3 KPIs (Cobrado 200 / Pago 80 / Saldo 120) E a cobrança aparece na lista com status ParcialmentePaga.
- **CA24 (lazy-load — performance)**: Dado a página do paciente aberta com a aba Financeiro **não** selecionada, Quando a página carrega, Então **nenhuma** request de cobrança/financeiro é disparada; E somente ao clicar na aba a request acontece (uma vez).
- **CA25 (3 tipos de origem na lista)**: Dado um paciente com cobranças origem=Consulta, =Procedimento e =Cirurgia no tenant, Quando a aba carrega, Então as 3 aparecem na lista, cada uma com sua tag de origem.
- **CA26 (expansão — histórico de pagamentos e estornos)**: Dado uma cobrança com 1 pagamento e 1 estorno, Quando expando o card, Então vejo o pagamento (forma/valor/data) E o estorno (valor/motivo/quem/quando em destaque vermelho), com o pagamento original riscado/atenuado.
- **CA27 (expansão cirurgia — histórico de valor)**: Dado uma cobrança origem=Cirurgia que tem histórico de alteração de valor, Quando expando o card, Então vejo as linhas de→para (quem, quando) read-only; E para cobranças sem histórico de valor, a seção não aparece.
- **CA28 (registrar pagamento pela aba — porta única)**: Dado uma cobrança aberta de R$ 120, Quando registro R$ 120 pela aba (PaymentModal da F1), Então o pagamento persiste na MESMA `Cobranca`, status vira Paga, e **a mesma mudança aparece no badge da agenda** daquele agendamento (sem cobrança duplicada).
- **CA29 (estorno atômico — INV-7)**: Dado um pagamento de R$ 80 numa cobrança paga, Quando estorno com motivo "valor incorreto", Então persiste um `EstornoPagamento` (valor 80, motivo) **e** um `Lancamento` de estorno na MESMA transação; o `Pagamento` e o `Lancamento` originais permanecem **intactos**; E Quando se força falha na criação do `Lancamento` de estorno (teste), Então o `EstornoPagamento` sofre rollback (nenhum dos dois persiste).
- **CA30 (estorno recalcula status — soma líquida)**: Dado uma cobrança de R$ 200 com 1 pagamento de R$ 200 (status Paga), Quando estorno esse pagamento, Então o status recalcula para Aberta E o saldo volta a R$ 200 (soma líquida = 0).
- **CA31 (motivo obrigatório)**: Dado a abertura do EstornoModal, Quando tento confirmar sem motivo, Então o botão fica desabilitado no front E (se chamado direto) o back retorna 422; nenhum estorno persiste.
- **CA32 (não estornar duas vezes — R8)**: Dado um pagamento já estornado, Quando tento estorná-lo de novo, Então o botão de estorno está ausente/desabilitado no front E o back retorna 422; `SUM(estornos) ≤ SUM(pagamentos)` é mantido.
- **CA33 (RBAC — ver vs registrar)**: Dado um usuário com `financeiro_paciente.ver` mas **sem** `financeiro_paciente.registrar`, Quando abre a aba, Então vê KPIs/lista/expansão **sem** botões de Registrar pagamento e Estornar; E se chamar o endpoint de pagamento/estorno direto, recebe 403/422.
- **CA34 (gate — sem permissão de ver)**: Dado um usuário **sem** `financeiro_paciente.ver`, Quando abre a aba Financeiro, Então vê o estado **restrito** (sem nenhum dado financeiro) E nenhuma query de cobrança é executada/retornada.
- **CA35 (multi-tenant falha-fechada)**: Dado um usuário do estabelecimento B, Quando tenta ler/estornar/pagar uma `Cobranca`/`Pagamento` do estabelecimento A (id direto na rota), Então recebe 404/"não encontrado" genérico, nada é persistido E nada é logado com PII; sem tenant claim, o repositório retorna vazio/throws.
- **CA36 (audit de acesso — LGPD)**: Dado o acesso à aba Financeiro de um paciente, Quando a query carrega, Então uma linha é inserida em `paciente_acesso_log` com {paciente_id, usuario_id, estabelecimento_id, TipoAcessoPaciente.Leitura, timestamp} via `IPacienteAcessoLogService`; E a falha do log **não** quebra o carregamento da aba (best-effort).
- **CA37 (LGPD — sem PII, DTO mínimo)**: Dado qualquer erro 422/404 ou log de domínio nos fluxos da aba, Quando ocorre, Então a mensagem é genérica e não contém PII (nome/CPF do paciente, motivo de estorno, valor que revele tenant alheio); E o DTO da aba não traz dado clínico nem campos fora da tela.
- **CA38 (convênio read-only na aba — R12)**: Dado uma cobrança `tipo=Convenio` na lista, Quando o usuário a vê, Então aparece com tag "Convênio" **sem** botão de registrar pagamento de balcão e **sem** estorno.
- **CA39 (estados — vazio/loading/erro)**: Dado um paciente **sem** cobranças, Quando abre a aba, Então mostra `AppEmptyState` com texto específico; durante a carga há loading; em falha de rede, mensagem de erro genérica com retry.
- **CA40 (repercussão bidirecional aba↔agenda)**: Dado uma cobrança paga pela **agenda** (F1), Quando abro a aba Financeiro do mesmo paciente, Então o pagamento aparece lá (mesma `Cobranca`); E vice-versa (estorno feito na aba reflete no badge da agenda) — **uma só** entidade, sem duplicidade.
- **CA41 (regressão das outras abas do paciente)**: Dado a página do paciente, Quando navego entre as abas existentes (prontuário, dados, documentos, etc.), Então todas continuam funcionando exatamente como antes E a introdução da aba Financeiro não altera o carregamento/estado das demais.
- **CA42 (doc viva)**: Dado a entrega da F2, Quando concluída, Então `Docs/ARQUITETURA.md` (entidade `EstornoPagamento` + INV-7) e `Docs/LGPD.md` (audit de acesso à aba financeiro via `paciente_acesso_log`/`Leitura`) foram atualizados conforme §11.

## 8. Riscos e dependências

- **Atomicidade do estorno (INV-7)**: maior risco técnico — transação cobrindo `EstornoPagamento` + `Lancamento` de estorno + recálculo de status. Teste de rollback obrigatório (CA29). Reusar o padrão de transação/UnitOfWork da F1 (que já faz Pagamento↔Lancamento atômico).
- **Porta única de domínio**: o maior risco de produto é **duplicar** lógica de cobrança/pagamento na F2 em vez de reusar a F1. Não-negociável: a aba consome `CobrancaController`/store/service da F1 (R1). CA28/CA40 blindam.
- **Repercussão bidirecional**: pagar/estornar na aba precisa refletir no badge da agenda e vice-versa — é a mesma `Cobranca`, mas exige invalidar/atualizar a store correta. CA40.
- **Regressão da página do paciente**: substituir o empty state e introduzir lazy-load não pode quebrar as outras abas (CA41) nem disparar carga antecipada (CA24).
- **Audit**: reuso de `IPacienteAcessoLogService` (já best-effort) — não criar serviço/tabela novos. Risco baixo.
- **Dependências**: **F1 (IMPLEMENTADA)** — agregados, `PaymentModal.vue`, permissões, controller, store/service. Schema → `imedto-database`: **1 tabela nova** (`estorno_pagamentos`); `lancamentos` **já** tem `cobranca_id`/`pagamento_id` (F1). **F5 não é dependência** — a F2 só exibe histórico de valor de cirurgia se existir (read-only).

## 9. Observações para execução — decisões de corte (não-negociáveis)

> Execução autônoma autorizada pelo usuário; as decisões abaixo foram tomadas pelo BA seguindo o plano mestre, sem `AskUserQuestion`. Mudança vira addendum.

- **DC1 — Audit reusa `paciente_acesso_log`, sem infra nova.** O plano (§1.4) pede audit "no mesmo padrão de `paciente_acesso_log`". Verificado: existe `IPacienteAcessoLogService.RegistrarAsync(... TipoAcessoPaciente.Leitura ...)` (já best-effort, append-only). **Decisão: reusar esse serviço** chamando-o no handler da query da aba — **não criar** tabela/serviço/enum de audit novo. Mais simples e consistente (Reuso > duplicação).
- **DC2 — Estorno no `Lancamento` = valor negativo + categoria, sem coluna/flag nova.** Avaliado: criar uma coluna/flag `is_estorno` em `lancamentos` vs. usar valor negativo + categoria de estorno. **Decisão: valor negativo + `categoria`/`tipo` de estorno**, reusando as colunas `cobranca_id`/`pagamento_id` já criadas na F1. Motivo: `Lancamento` já é a base de fluxo de caixa simples (Relatórios/F7); um lançamento negativo abate naturalmente o consolidado, sem ALTER novo nem lógica especial de leitura. A **rastreabilidade do estorno** vive em `estorno_pagamentos` (motivo/quem/quando + `lancamento_estorno_id`). O DB agent confirma o nome da categoria de estorno (reusar `CategoriaFinanceira` existente; se não houver, criar valor de catálogo — não tabela). **Não criar** flag em `lancamentos`.
- **DC3 — Estorno é TOTAL (1 estorno anula 1 pagamento por inteiro).** Estorno parcial de um pagamento fica fora desta versão (anti-escopo). `EstornoPagamento.valor` = valor cheio do `Pagamento` estornado. Motivo: a esmagadora maioria do caso operacional é "lancei errado, desfaço o pagamento"; estorno parcial multiplica a superfície de cálculo/UX sem demanda. Domínio nasce sem travar evolução (a tabela guarda `valor`, então um futuro estorno parcial é incremental).
- **DC4 — A F2 não cria cobrança nem edita valor de cirurgia.** Só **lê e opera** (pagar/estornar) cobranças existentes. Criação de cobrança de procedimento (F4), de cirurgia (F5) e o histórico de valor de→para (F5) são de outras fases. A F2 **exibe** o histórico de valor se a tabela/coluna existir; se não existir ainda, a seção simplesmente não aparece (sem erro). Dev: não bloquear por ausência do histórico de cirurgia.
- **DC5 — `PaymentModal` é reuso puro da F1.** Nenhuma reescrita; a aba abre o componente existente apontando para a mesma `Cobranca`. Se o `PaymentModal` da F1 precisar de um prop a mais para o contexto "aba" (ex.: callback de refresh), é extensão mínima, não fork.
- **DC6 — Permissões: reuso, nada novo.** `financeiro_paciente.ver` (leitura/gate) e `financeiro_paciente.registrar` (pagar **e** estornar) já existem (F1). **Não criar** permissão de estorno separada — estorno é operação financeira de registro, coberta por `registrar`. RBAC espelhado back+front.
- **DC7 — KPIs e saldo calculados no backend.** O front só exibe; a query agrega cobrado/pago-líquido/saldo no back (R3). Sem cálculo monetário no front (só preview, herdado da F1).
- **Liberdade técnica do dev/db**: nome EF da entidade `EstornoPagamento`, estrutura do handler de estorno, forma de agregar KPIs (query única vs. derivada do domínio), nome do componente `EstornoModal`, forma de invalidar a store cross-porta. **Não-negociável**: decimal sempre + helper de arredondamento da F1, transação atômica do estorno, multi-tenant falha-fechada, audit por acesso, porta única (reuso F1), pagamento/lançamento originais imutáveis.

## 10. Schema para o `imedto-database`

> Migration EF + SQL idempotente em `db/migrations/`. Multi-tenant + índices dia 1. Monetário `numeric(12,2)`. snake_case PT-BR consistente com o schema atual. **`lancamentos` já tem `cobranca_id`/`pagamento_id` (F1) — não re-alterar.**

**Tabela `estorno_pagamentos`** (nova — entidade de histórico imutável; filha de `Pagamento`/`Cobranca`)
- `id bigserial PK`
- `pagamento_id bigint NOT NULL` (FK `pagamentos` — pagamento estornado)
- `cobranca_id bigint NOT NULL` (FK `cobrancas` — desnormalizado p/ filtro/agregação por cobrança)
- `estabelecimento_id bigint NOT NULL` (tenant)
- `valor numeric(12,2) NOT NULL` (= valor do pagamento; estorno total nesta versão)
- `motivo text NOT NULL`
- `lancamento_estorno_id bigint NOT NULL` (FK `lancamentos` — lançamento de estorno gerado atomicamente; valor negativo)
- `estornado_por_usuario_id uuid NOT NULL`
- `data_estorno date NOT NULL`
- `criado_em timestamptz NOT NULL`
- Índices sugeridos: `(cobranca_id)`, `(estabelecimento_id, cobranca_id)`, **unique `(pagamento_id)`** — garante 1 estorno (total) por pagamento (DC3/R8) no nível do banco, além da invariante de domínio.
- FKs `ON DELETE RESTRICT` (dado financeiro não cascateia).

**Lançamento de estorno (sem tabela/coluna nova)**
- O estorno gera um registro em `lancamentos` reusando as colunas existentes: `cobranca_id` + `pagamento_id` (do pagamento estornado), **`valor` negativo**, `categoria`/`tipo` indicando estorno de receita. Confirmar/usar valor de `CategoriaFinanceira` existente para estorno; **se não houver**, adicionar **valor de catálogo/enum** (não tabela nova, não coluna nova em `lancamentos`). Decisão DC2 — **não** criar flag `is_estorno`.

**Sem outras mudanças de schema.** Cobranças/pagamentos/preços/taxa (F1) intactos. `paciente_acesso_log` (existente) reusado para audit — sem alteração.

> Observações ao DB agent: confirmar o tipo real de `valor` em `lancamentos` aceita negativo (numeric aceita; verificar se há CHECK `>= 0` — se houver, ajustar para permitir estorno, alinhando com a leitura do Relatórios/F7). Confirmar nome da tabela de pagamentos (`pagamentos`) e de lancamentos. Sem trigger/function com regra de negócio (regra mora no backend). A unique `(pagamento_id)` em `estorno_pagamentos` é o trilho de banco para "1 estorno por pagamento".

## 11. Atualização de documentação (parte da entrega da F2)

> A F1 já criou as seções de Cobranças em `ARQUITETURA.md` e de dado financeiro sensível em `LGPD.md`. A F2 só acrescenta **deltas cirúrgicos** — não reescrever.

- **`Docs/ARQUITETURA.md`** — na área de domínio **Cobranças** já existente: acrescentar a entidade **`EstornoPagamento`** (histórico imutável, filha de `Pagamento`/`Cobranca`) e a **INV-7** (estorno atômico com `Lancamento` negativo na mesma transação; pagamento original imutável; status por soma líquida). Nota sobre a **porta única de domínio** (aba do paciente e badge da agenda operam a mesma `Cobranca`). Mudança incremental.
- **`Docs/LGPD.md`** — na categoria **dado financeiro do paciente** já existente: registrar que o **audit de acesso à aba Financeiro** é feito via `paciente_acesso_log` (`IPacienteAcessoLogService`, `TipoAcessoPaciente.Leitura`) a cada carregamento da aba — fechando o ponto que a F1 deixou como "audit por paciente entra na F2". Reforçar DTO mínimo (sem dado clínico) e `motivo` de estorno sem PII.
- **`Docs/COMANDOS.md`** — **nenhuma mudança** (sem script/comando novo).
- **`Docs/INFRA.md`** — **nenhuma mudança** (sem recurso AWS novo).
- **`Docs/DESIGN.md`** — **avaliar na implementação**: se `EstornoModal` nascer como componente reutilizável do DS, registrar; o `PaymentModal` já é da F1 (apenas reuso). Provavelmente delta mínimo ou nenhum.
