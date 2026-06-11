# Financeiro F4 — Cobrança de procedimento ao marcar como realizado + baixa automática de estoque

**ID**: 2026-06-10_013
**Status**: Aprovado (execução autônoma — decisões fechadas no plano mestre `Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md` §3.3, §F4, §5 q7; cortes restantes decididos pelo BA e registrados na §11)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (pendências F3B), financeiro (Cobranças), estoque (Inventário) — multi-tenant

## 1. Contexto e motivação

A F3B entregou o checklist de Conduta da evolução, e a ação **"Marcar procedimento realizado"** (`AcaoPendencia.MarcarProcedimentoRealizado`) hoje **só conclui manualmente** (`ConcluirPendenciaManualCommandHandler` → `ConcluirManualmente()`), com gancho explicitamente preparado para a F4 (ver `Docs/ARQUITETURA.md §conduta como checklist`, e o comentário no briefing 2026-06-10_012).

A F3 fez a seção "Procedimentos indicados" gravar, no `ConteudoJson` da evolução, um snapshot por procedimento: `{ catalogoCirurgiaId, descricao, valor, observacao }` (briefing 2026-06-10_011, D2/D5). Esse snapshot é o contrato consumido aqui.

A F4 transforma o gatilho operacional em fluxo financeiro real: ao marcar o procedimento como realizado, nasce uma **`Cobranca` origem=Procedimento** com valor = soma dos snapshots de procedimentos da evolução, **e** dispara a **baixa automática de estoque** dos insumos vinculados (via `orcamento_catalogo_cirurgia_produto` + consolidação já existente), alimentando o **custo real** por paciente que a F7 consolidará. A baixa manual de estoque deixa de ser o único caminho para procedimento realizado.

**Dor resolvida**: hoje, marcar procedimento realizado não gera conta a receber nem baixa o insumo — recepção/profissional precisa lançar cobrança e dar baixa de estoque à mão, com risco de esquecer ou divergir. A F4 automatiza o caos numa única ação atômica.

## 2. Persona-alvo

**Profissional** (ou recepção, no painel de pendências) que, após o atendimento, marca na lista de pendências do paciente que o procedimento indicado foi realizado. Frequência: a cada atendimento com procedimento realizado em consultório. Momento da jornada: pós-atendimento / conduta.

## 3. Escopo

**Inclui**:
- Fluxo dedicado da ação `MarcarProcedimentoRealizado` da pendência (F3B), substituindo a conclusão manual simples **apenas para essa ação**: novo endpoint + handler + modal de confirmação.
- Geração de `Cobranca` origem=`Procedimento`, tipo=`Particular`, valor = soma dos `valor` dos procedimentos-indicados snapshot da evolução vinculada à pendência.
- Baixa automática de estoque: para os `catalogoCirurgiaId` dos procedimentos da evolução, resolve produtos vinculados (`orcamento_catalogo_cirurgia_produto`) + consolidação (`ProdutosConsolidador`: MAX p/ uso único, SOMA p/ múltiplo) → `RegistrarSaida` em cada `ItemInventario` → `MovimentacaoEstoque` de saída com custo (snapshot do `CustoMedio`).
- Conclusão automática da pendência no mesmo fluxo (`ConcluirPorGatilho(cobranca.Id)` → `referencia_id = cobranca.Id`).
- **Atomicidade total**: cobrança + N movimentações de estoque + conclusão da pendência na MESMA transação UoW (ou tudo persiste, ou nada).
- **Idempotência total**: marcar 2x não duplica cobrança nem baixa estoque (trava no domínio + trava dura no banco).
- Badge na agenda e aba Financeiro do paciente exibem a cobrança de procedimento — **reuso integral de F1/F2, nada novo** (a query de badge faz join por `agendamento_id`; a aba lista por `paciente_id` — a cobrança origem=Procedimento já entra).
- Pagamento via ícone do agendamento OU aba Financeiro — reuso integral de F1/F2 (`RegistrarPagamentosCommandHandler`).

**Não inclui** (corte explícito):
- Comissão de profissional sobre procedimento → **F7** (`ConfigComissaoProfissional`).
- Convênio → **F6** (nesta fase a cobrança de procedimento nasce sempre Particular).
- Cirurgia / orçamento pré-preenchido → **F5**.
- **Desfazer "realizado"** (reverter cobrança + estoque) → **fora da F4** (ver R10 e §11 D8). Estorno de pagamento já existe (F2). Cancelamento de cobrança de procedimento + reversão de estoque fica documentado como evolução futura, alinhada à INV-7.
- Saldo negativo de estoque / baixa forçada — não se inventa comportamento novo; segue o domínio existente (R8).

## 4. Regras de negócio

- **R1 — Gatilho**: a ação `MarcarProcedimentoRealizado` de uma `PendenciaAtendimento` (status `Pendente`) é o único gatilho da cobrança de procedimento. Mora em: novo `MarcarProcedimentoRealizadoCommandHandler` (Application/Prontuarios ou Application/Cobrancas — dev decide; recomendado Prontuarios, pois orquestra pendência + cobrança + estoque). Validada em: back (handler) + front (botão da ação no painel/modal).

- **R2 — Valor da cobrança = snapshot da evolução**: `valor_cobrado` = soma dos `valor` de cada item da seção `procedimentos-indicados` do `ConteudoJson` da evolução (`ProntuarioEvolucao.ConteudoJson`), arredondada via `ArredondamentoMonetario.Arredondar` (2 casas, half-away-from-zero). **Não** re-resolve o valor do catálogo (preserva imutabilidade F3 — o valor cobrado é o que foi indicado, não o preço atual). Mora em: handler (leitura do JSON) + `Cobranca` factory. Validada em: back. Ver §11 D1.

- **R3 — Origem e tipo**: a cobrança nasce `Origem="Procedimento"`, `TipoAtendimento=Particular`, `AgendamentoId` = o da pendência (`PendenciaAtendimento.AgendamentoId`, pode ser null), `EvolucaoId` setado (ver R7/schema), `ConvenioId=null`, `OrcamentoId=null`. Mora em: nova factory `Cobranca.CriarParaProcedimento`. Validada em: back. Convênio é F6 (§11 D2).

- **R4 — Baixa automática de estoque**: para cada `catalogoCirurgiaId` distinto presente nos procedimentos-indicados da evolução, resolve os vínculos `orcamento_catalogo_cirurgia_produto` (filtrados por tenant via JOIN com `orcamento_catalogo_cirurgia`) + consolida pela regra `ProdutosConsolidador` (MAX p/ `uso_unico`, SOMA p/ múltiplo). Para cada produto consolidado, localiza o `ItemInventario` correspondente do estabelecimento e chama `RegistrarSaida(quantidade, usuarioId, observacao)`, persistindo a `MovimentacaoEstoque` retornada. A `observacao` da movimentação referencia a origem ("Baixa automática — procedimento realizado, cobrança #{id}"), **sem PII** (R9). Mora em: handler + reuso de `ProdutosConsolidador` + `ItemInventario.RegistrarSaida`. Validada em: back. **Quantidade**: a quantidade da cirurgia/procedimento na consolidação = 1 por procedimento-indicado da evolução (não há "quantidade de cirurgia" no contexto de procedimento; cada procedimento indicado conta como 1 ocorrência do `catalogoCirurgiaId`). Procedimentos repetidos (mesmo `catalogoCirurgiaId` duas vezes na evolução) somam ocorrências.

- **R5 — Vínculo produto↔item de inventário**: o produto do catálogo de orçamento (`orcamento_catalogo_produto`) precisa estar associado a um `ItemInventario` para baixar. **Premissa do código atual**: investigar na implementação como `orcamento_catalogo_produto` mapeia para `ItemInventario` (por `item_inventario_id` no produto, ou por código/nome). Se um produto consolidado **não** tiver item de inventário correspondente no estabelecimento, **ignora aquele produto** (baixa parcial silenciosa — não bloqueia a cobrança; é o mesmo princípio do "procedimento sem produtos" R6). O dev confirma o caminho de mapeamento real e o documenta. Validada em: back.

- **R6 — Procedimento sem produtos vinculados**: baixa vazia é válida. A cobrança é gerada normalmente; nenhuma `MovimentacaoEstoque` é criada. Sem erro. Mora em: handler. Validada em: back + CA.

- **R7 — Idempotência (cobrança E estoque)**: marcar o mesmo procedimento como realizado 2x **não** duplica cobrança nem baixa. Dupla trava:
  1. **Domínio**: o handler carrega a `PendenciaAtendimento`; se `Status == Concluida`, retorna no-op (não cria cobrança, não baixa estoque). Reusa o padrão idempotente de `ConcluirPorGatilho`/`ConcluirManualmente` (já é no-op se concluída).
  2. **Banco (defense-in-depth)**: `cobrancas` ganha coluna `evolucao_id` (nullable) + **índice UNIQUE parcial** `(evolucao_id) WHERE origem='Procedimento' AND evolucao_id IS NOT NULL` — duas cobranças de procedimento para a mesma evolução são fisicamente impossíveis (race condition fecha no banco com 23505 → mapeado para 422 "Procedimento já marcado como realizado").
  Como a baixa de estoque ocorre **no mesmo handler** que cria a cobrança, ambas compartilham a mesma trava: se a cobrança não é criada (idempotência), o estoque não baixa. Mora em: handler + schema. Validada em: back + CA obrigatório.

- **R8 — Estoque insuficiente bloqueia (comportamento existente do domínio)**: `ItemInventario.RegistrarSaida` **lança** `BusinessException("Estoque insuficiente...")` quando `quantidade > QuantidadeAtual`. Como toda a operação é atômica (R-atomicidade), esse erro faz **rollback de tudo** — a marcação inteira falha com 422 explicativo, nada é gravado (nem cobrança, nem baixa parcial, nem conclusão da pendência). **Não** se inventa saldo negativo nem baixa forçada — segue o domínio vigente. A mensagem do 422 expõe o item e o saldo (já é assim no domínio), **sem PII de paciente**. Mora em: domínio (já existe) + handler (propaga). Validada em: back + CA. Ver §11 D7.

- **R9 — LGPD / minimização**: a `MovimentacaoEstoque` e a `Cobranca` **não** carregam conteúdo clínico (CID, diagnóstico, texto da evolução). `Cobranca.Descricao` = texto controlado tipo "Procedimento(s) realizado(s)" (ou a lista de `descricao` dos procedimentos — que é o nome do procedimento do catálogo, não dado clínico sensível; dev usa o mais enxuto). `MovimentacaoEstoque.Observacao` referencia só ids (cobrança/procedimento), nunca nome de paciente. Nenhuma PII em log ou mensagem de erro. Mora em: handler. Validada em: back + CA.

- **R10 — Sem desfazer nesta fase**: não há ação de "desmarcar realizado" / reverter. A pendência concluída por gatilho permanece concluída. Estorno de pagamento (F2) trata pagamentos indevidos; cancelar a cobrança de procedimento + reverter estoque é evolução futura alinhada à INV-7. Registrado como anti-escopo. Validada em: ausência de endpoint de reversão (CA de regressão).

- **R11 — Multi-tenant**: pendência, evolução, cobrança, vínculos de produto e itens de inventário são todos filtrados por `estabelecimento_id` do tenant ativo. Repositórios falha-fechada; mensagem genérica "não encontrado" em acesso cross-tenant. Mora em: handler + repos. Validada em: back + CA obrigatório.

- **R12 — RBAC**: marcar procedimento realizado exige **`prontuario.editar`** (mesma permissão da conclusão manual da pendência — F3B). A cobrança nasce sob essa ação; **não** exige `financeiro_paciente.registrar` (essa governa registrar *pagamento*, não criar a conta a receber). Papéis: `Profissional`, `Dono`, `Recepcionista` (mesmos do `PendenciaAtendimentoController`). Front oculta/desabilita o botão sem permissão; back retorna 403. Mora em: controller (`[RequiresAcao("prontuario.editar")]` + `[RequiresPapel(...)]`). Validada em: back + front. Ver §11 D9.

- **R13 — Regressão preservada**: (a) a conclusão manual da pendência (`POST .../pendencias/{id}/concluir`) continua funcionando para as **outras 5 ações** e para `MarcarProcedimentoRealizado` **não** é mais roteada por ela (o front chama o novo endpoint para essa ação); (b) a baixa **manual** de estoque (`RegistrarSaida` via fluxo de inventário) continua intacta; (c) os 5 event handlers de conclusão automática da F3B continuam intactos; (d) cobrança de consulta (F1) e estorno (F2) intactos. Validada em: CA de regressão.

## 5. Modelo de dados

**ALTER `cobrancas`** (única mudança de schema — DB agent):
- `evolucao_id bigint NULL` — rastreio da evolução de origem (procedimento). Null para cobranças de consulta (F1) e futuras de cirurgia.
- **Índice UNIQUE parcial**: `CREATE UNIQUE INDEX ux_cobrancas_evolucao_procedimento ON cobrancas (evolucao_id) WHERE origem = 'Procedimento' AND evolucao_id IS NOT NULL;` — garante 1 cobrança de procedimento por evolução (idempotência R7, defense-in-depth contra race).
- FK fraca (sem constraint relacional dura para `prontuario_evolucoes`, seguindo o padrão de `pendencias_atendimento` que usa FK fraca multi-origem; dev/DB confirmam — o importante é o índice de unicidade).

**Sem tabela nova.** `pendencias_atendimento` (F3B), `movimentacoes_estoque` e `itens_inventario` (estoque), `orcamento_catalogo_cirurgia_produto` e `orcamento_catalogo_produto` (catálogo) já existem e são reusados sem alteração.

**Reuso de leitura**:
- Snapshot de procedimentos: `ProntuarioEvolucao.ConteudoJson` (jsonb) — seção `procedimentos-indicados`, formato F3.
- Consolidação de produtos: reusa o padrão Dapper de `ConsolidarProdutosOrcamentoQueryHandler` (JOIN tenant-filtered + `ProdutosConsolidador`).
- Custo: `MovimentacaoEstoque` já grava `custo_unitario` (snapshot do `CustoMedio`) na saída — alimenta o custo real da F7 sem schema novo.

**Multi-tenant**: `cobrancas.estabelecimento_id` já existe (F1). Todas as leituras filtram tenant.
**Audit/LGPD**: a aba Financeiro do paciente já audita o acesso (F2, `paciente_acesso_log`). A criação da cobrança de procedimento herda esse padrão quando exibida na aba; a marcação em si é uma ação no prontuário (já auditada como acesso ao paciente). Nenhum novo audit log próprio é necessário — confirmar na implementação que o acesso ao paciente continua sendo logado.

## 6. UX e fluxo

Ponto de contato: **painel de pendências** (`PainelPendencias.vue`) na aba Resumo de `PacienteDetalheView.vue` (F3B) e o **modal pós-salvar** (`ProximosPassosModal.vue`). Para a ação `MarcarProcedimentoRealizado`, o botão deixa de chamar o "concluir manual" simples e abre um **modal de confirmação dedicado** (novo componente, ex.: `MarcarProcedimentoRealizadoModal.vue`).

Protótipo não tem tela específica — seguir o padrão dos modais existentes (`PaymentModal` da F1/F2, `ProximosPassosModal` da F3B): `AppDrawer`/modal do design system, tipografia via tokens (CLAUDE.md §5), botões `AppButton`.

**Conteúdo do modal** (preview antes de confirmar):
- Título: "Marcar procedimento como realizado".
- Lista dos procedimentos da evolução: `descricao` + `valor` (do snapshot F3), com **total** somado (preview client-side, fonte da verdade é o 422/handler).
- Lista dos **produtos a baixar do estoque** (resolvidos via query de preview reutilizando a consolidação — produto + quantidade). Se vazio: "Nenhum insumo vinculado a estes procedimentos."
- Alerta visual se algum item está **abaixo do necessário** (preview informativo — o bloqueio real é no back, R8).
- Botões: "Confirmar" (chama o novo endpoint) e "Cancelar".

**Estados**:
- **loading**: spinner ao abrir (carrega preview de procedimentos + produtos) e ao confirmar.
- **vazio (sem procedimentos)**: o modal **não** deve nem abrir para confirmação de cobrança — exibe mensagem "Esta evolução não tem procedimentos indicados para marcar como realizado." e oferece só "Fechar" (espelha o 422 do back, R-edge / §11 D5). Alternativamente, o front pode esconder a ação quando a evolução não tem procedimentos (dev decide; o back **sempre** valida com 422).
- **erro de estoque insuficiente**: toast/inline com a mensagem do 422 ("Estoque insuficiente. Disponível: X un.") — nada foi gravado (R8).
- **sucesso**: fecha modal, pendência some do painel (concluída), badge de cobrança aparece no agendamento (se houver) e na aba Financeiro. Toast "Procedimento realizado. Cobrança gerada."
- **erro genérico/403/404**: mensagem genérica, sem PII.

**Mobile-ready**: modal responsivo, padrão do design system. **Atalhos**: Esc fecha (padrão dos modais).

## 7. Critérios de aceite (testáveis)

- **CA76** (caminho feliz — cobrança + estoque): Dado um paciente com uma pendência `MarcarProcedimentoRealizado` (status Pendente) cuja evolução tem 2 procedimentos-indicados (snapshot F3 com `valor` 100,00 e 50,00) e produtos vinculados em estoque suficiente, Quando o usuário com `prontuario.editar` confirma no modal, Então (a) nasce 1 `Cobranca` origem=Procedimento, tipo=Particular, `valor_cobrado=150,00`, `evolucao_id` setado, status=Aberta; (b) são geradas `MovimentacaoEstoque` de saída para os produtos consolidados com `custo_unitario` = `CustoMedio` snapshot; (c) a pendência fica status=Concluida com `referencia_id` = id da cobrança; (d) tudo numa única transação.

- **CA77** (idempotência — cobrança E estoque): Dado uma pendência `MarcarProcedimentoRealizado` já concluída (cobrança e baixa já geradas), Quando o usuário tenta marcar realizado novamente (mesma evolução), Então **nenhuma** cobrança nova é criada, **nenhuma** `MovimentacaoEstoque` nova é gerada, o `QuantidadeAtual` dos itens permanece inalterado, e a resposta é no-op idempotente (ou 422 "Procedimento já marcado como realizado" se a corrida bater no índice UNIQUE parcial) — em nenhum caso há duplicação.

- **CA78** (atomicidade — rollback): Dado que a baixa de um dos produtos falha (ex.: estoque insuficiente — R8), Quando a marcação é confirmada, Então a transação inteira sofre rollback: nenhuma `Cobranca` é persistida, nenhuma `MovimentacaoEstoque` é gravada, a pendência permanece Pendente, e o usuário recebe 422 com a mensagem de estoque insuficiente.

- **CA79** (procedimento sem produtos — baixa vazia): Dado uma pendência cuja evolução tem procedimentos sem nenhum produto vinculado, Quando marca realizado, Então a `Cobranca` é gerada normalmente, **nenhuma** `MovimentacaoEstoque` é criada, a pendência conclui, e não há erro.

- **CA80** (evolução sem procedimentos — 422): Dado uma pendência `MarcarProcedimentoRealizado` cuja evolução **não** tem procedimentos-indicados no `ConteudoJson`, Quando o usuário tenta marcar realizado, Então o back retorna 422 "Esta evolução não tem procedimentos indicados para marcar como realizado.", **nenhuma** cobrança/baixa/conclusão ocorre, e a pendência permanece Pendente.

- **CA81** (multi-tenant): Dado um usuário do estabelecimento B, Quando tenta marcar realizado uma pendência (ou referenciar uma evolução/produto) do estabelecimento A, Então recebe "não encontrado" genérico (sem revelar existência cross-tenant), nada é gravado, e nenhum log com PII é emitido.

- **CA82** (RBAC): Dado um usuário com papel sem `prontuario.editar`, Quando chama o endpoint de marcar realizado, Então recebe 403 e o botão de marcar realizado fica oculto/desabilitado no painel/modal.

- **CA83** (LGPD — sem PII): Dado a geração da cobrança e das movimentações, Quando os registros são gravados e quando ocorre erro (estoque insuficiente, não-encontrado), Então `Cobranca.Descricao`, `MovimentacaoEstoque.Observacao`, logs e mensagens de erro **não** contêm nome de paciente, CID, diagnóstico nem texto clínico — só ids e texto controlado.

- **CA84** (estados de UI): Dado o modal de confirmação, Quando carrega, Então exibe loading; com procedimentos, lista descricao+valor+total e produtos a baixar; sem produtos, mostra "Nenhum insumo vinculado"; sem procedimentos, mostra mensagem específica e só "Fechar"; em erro de estoque, exibe a mensagem do 422 sem PII; em sucesso, fecha, remove a pendência do painel e exibe toast.

- **CA85** (performance): Dado a confirmação, Quando o handler resolve produtos, Então a consolidação usa **uma** query batched por `catalogoCirurgiaId` (ANY(@Ids)) — sem N+1 por procedimento; o preview do modal usa a mesma query de consolidação (reaproveitada), com a request única ao abrir.

- **CA86** (badge/aba — reuso F1/F2): Dado uma cobrança de procedimento gerada para um agendamento, Quando o usuário abre a agenda, Então o badge de cobrança aparece no card (mesma query de badge da F1); Quando abre a aba Financeiro do paciente, Então a cobrança origem=Procedimento é listada junto das demais (reuso F2) — sem código de exibição novo.

- **CA87** (pagamento — reuso F1/F2): Dado a cobrança de procedimento Aberta, Quando o usuário registra um pagamento (ícone do agendamento ou aba Financeiro), Então o fluxo `RegistrarPagamentosCommandHandler` opera normalmente (Lancamento atômico, status migra Aberta→ParcialmentePaga→Paga) — sem alteração no fluxo de pagamento.

- **CA88** (regressão — conclusão manual e estoque manual): Dado as outras 5 ações de pendência (CriarReceita, CriarAtestado, PedirExame, CriarOrcamento, AgendarRetorno), Quando concluídas (manual ou por gatilho de evento), Então continuam funcionando como na F3B (sem cobrança/estoque); e a baixa **manual** de estoque pelo módulo de inventário continua operando inalterada.

- **CA89** (sem desfazer — anti-escopo): Dado uma pendência de procedimento já concluída com cobrança gerada, Quando se busca uma ação de "desmarcar/reverter realizado", Então ela **não existe** nesta fase (nenhum endpoint de reversão); o tratamento de pagamento indevido permanece o estorno (F2).

## 8. Riscos e dependências

**Dependências**: F1 (`Cobranca`/`Pagamento`/`Lancamento` atômico), F3 (snapshot `catalogoCirurgiaId`+`valor` no `ConteudoJson`), F3B (`PendenciaAtendimento` + ação `MarcarProcedimentoRealizado`), módulo Estoque (`ItemInventario.RegistrarSaida`, `MovimentacaoEstoque`), catálogo (`orcamento_catalogo_cirurgia_produto` + `ProdutosConsolidador`). **Todas já implementadas no código.**

**Riscos**:
- **Mapeamento produto↔item de inventário** (R5): o vínculo `orcamento_catalogo_produto` → `ItemInventario` precisa ser confirmado no código real pelo dev. Se não existir caminho direto, é o maior risco da fase — pode exigir decisão de produto adicional (escalar como spec gap Tipo B se o mapeamento não for trivial/inexistente). **Atenção do dev: investigar isto primeiro.**
- **Atomicidade da transação multi-agregado** (Cobranca + N MovimentacaoEstoque + Pendencia + índice UNIQUE): garantir que tudo roda no mesmo `UnitOfWork` do controller, como já faz `RegistrarPagamentosCommandHandler`. Ordem de persistência: salvar cobrança (para obter Id) → baixar estoque → concluir pendência com `referencia_id`=cobranca.Id → commit único.
- **Race condition** (dois cliques simultâneos): coberta pelo índice UNIQUE parcial (R7.2) — 23505 vira 422.
- **Regressão de prontuário**: o front da ação `MarcarProcedimentoRealizado` muda de "concluir manual" para o novo endpoint — não quebrar as outras 5 ações (CA88).

## 9. Observações para execução

**Não-negociável**:
- Idempotência dupla (domínio + índice UNIQUE parcial) — CA77 é obrigatório.
- Atomicidade total no mesmo UoW — CA78 é obrigatório.
- Valor = snapshot da evolução (R2), não re-resolução do catálogo.
- Estoque insuficiente **bloqueia** (R8) — não inventar saldo negativo.
- Reuso de `ProdutosConsolidador`, `ItemInventario.RegistrarSaida`, badge/aba F1/F2, pagamento F1/F2 — **zero duplicação**.

**Liberdade técnica do dev**:
- Local do handler (Application/Prontuarios recomendado, mas Application/Cobrancas é aceitável).
- Nome do componente do modal e da query de preview.
- Se a query de preview de produtos é um novo query handler ou reusa `ConsolidarProdutosOrcamentoQuery` adaptado (recomendado reusar/estender, não duplicar).
- Caminho exato do mapeamento produto↔item de inventário (R5) — investigar e documentar.

**Aciona `imedto-database`**: SIM — ALTER em `cobrancas` (add `evolucao_id` + índice UNIQUE parcial). Ver §10.

## 10. Schema para o DB agent

**ALTER única — tabela `cobrancas`:**
1. `ADD COLUMN evolucao_id bigint NULL` (rastreio + idempotência de cobrança de procedimento; null para consulta/cirurgia).
2. `CREATE UNIQUE INDEX ux_cobrancas_evolucao_procedimento ON cobrancas (evolucao_id) WHERE origem = 'Procedimento' AND evolucao_id IS NOT NULL;` — índice UNIQUE parcial (Postgres) que garante 1 cobrança de procedimento por evolução (idempotência R7 / CA77, defense-in-depth contra race / CA78).
3. FK fraca para `prontuario_evolucoes` — seguir o padrão de `pendencias_atendimento` (sem constraint relacional dura multi-origem; a unicidade vem do índice parcial). DB agent confirma se adiciona FK ou mantém fraca, consistente com o padrão do projeto.

Migration EF Core + SQL idempotente em `db/migrations/`, multi-tenant (a coluna não muda o filtro de tenant, que já existe). **Nenhuma tabela nova. Nenhum outro ALTER.**

## 11. Decisões de corte (autônomas — registradas)

- **D1 (valor da cobrança)**: **snapshot da evolução** (`valor` dos procedimentos-indicados gravados na F3), não re-resolução do catálogo. Coerente com a imutabilidade da evolução (F3 D2/D5) e com o conceito de "o que foi indicado é o que foi cobrado". Convênio/reajuste de preço futuro não altera cobrança já gerada.
- **D2 (tipo_atendimento)**: **Particular por default** nesta fase. A evolução não carrega tipo de atendimento; convênio para procedimento entra na F6. Coluna `convenio_id` permanece null (já reservada).
- **D3 (UX)**: a ação `MarcarProcedimentoRealizado` ganha **modal de confirmação dedicado** com preview de procedimentos + valores + produtos a baixar, em vez de roteá-la pelo "concluir manual" simples da F3B. As outras 5 ações continuam pelo fluxo manual/automático da F3B.
- **D4 (idempotência)**: **dupla trava** — (1) no domínio, pendência `Concluida` ⇒ no-op; (2) no banco, índice UNIQUE parcial `(evolucao_id) WHERE origem='Procedimento'`. Mais robusto que confiar só no status da pendência (cobre race). Estoque herda a trava por estar no mesmo handler/transação.
- **D5 (evolução sem procedimentos)**: **422 explicativo**, **não** conclui sem cobrança. Evita pendência "fantasma" concluída sem efeito financeiro e deixa claro ao usuário que não há o que marcar.
- **D6 (procedimento sem produtos)**: **baixa vazia, ok** — cobrança gerada, sem movimentação, sem erro (alinhado ao plano mestre §F4 riscos).
- **D7 (estoque insuficiente)**: **bloqueia** (rollback total) — segue o comportamento existente de `ItemInventario.RegistrarSaida` (lança `BusinessException`). Não se inventa saldo negativo nem baixa forçada; respeita o domínio vigente (Surgical Changes).
- **D8 (desfazer "realizado")**: **OUT da F4**. Estorno de pagamento já existe (F2); cancelamento de cobrança de procedimento + reversão de estoque com histórico fica documentado como evolução futura, alinhada à INV-7. Não criar reversão parcial agora (Simplicity First).
- **D9 (RBAC)**: marcar realizado = **`prontuario.editar`** (mesma da conclusão manual F3B); a cobrança nasce sob essa ação, **não** exige `financeiro_paciente.registrar` (que é para registrar *pagamento*). Coerente: quem edita o prontuário e conduz o atendimento marca o procedimento; registrar o dinheiro é outra permissão, no momento do pagamento.
- **D10 (schema)**: **1 ALTER** em `cobrancas` (evolucao_id + índice UNIQUE parcial), **nenhuma tabela nova**. O `referencia_id` da pendência guarda a cobrança.id; o `evolucao_id` da cobrança fecha a idempotência no banco.

## 12. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — na subseção "Seção `conduta` como checklist → pendências do atendimento" (F3B) e/ou na "Área de domínio: Cobranças": registrar que o gatilho `MarcarProcedimentoRealizado` deixou de ser conclusão manual e passou a um fluxo próprio (F4) que **(a)** cria `Cobranca` origem=Procedimento com valor=snapshot da evolução, **(b)** dispara baixa automática de estoque via `ProdutosConsolidador`+`RegistrarSaida`, **(c)** conclui a pendência (`referencia_id`=cobranca.Id), tudo atômico; e a coluna `cobrancas.evolucao_id` + índice UNIQUE parcial como mecanismo de idempotência. Atualização **incremental/cirúrgica** — não reescrever as seções.
- **`Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md`** — marcar a F4 com status "briefing 2026-06-10_013 escrito" na linha da fase (mesma convenção da F3).
- **`Docs/LGPD.md`** — **sem delta** (a cobrança/movimentação não introduzem novo tipo de PII; o acesso ao paciente já é auditado pela F2/prontuário).
- **`Docs/DESIGN.md`** — só se o dev extrair o modal como componente reutilizável do design system (decisão na implementação).
- **`Docs/COMANDOS.md`/`INFRA.md`** — sem delta.
