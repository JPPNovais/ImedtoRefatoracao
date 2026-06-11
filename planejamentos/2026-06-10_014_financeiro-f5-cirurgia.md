# Financeiro F5 — Cirurgia (orçamento pré-preenchido pela evolução + cobrança na aprovação)

**ID**: 2026-06-10_014
**Status**: Aprovado por usuário em 2026-06-10 (execução autônoma — decisões fechadas no plano mestre `MODULO_FINANCEIRO_COBRANCAS.md §3.4/§F5/§5 q12`; cortes restantes decididos pelo BA com base no código e registrados na §9)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: orçamento (fluxo criar/aprovar), prontuário (link da conduta "Criar orçamento"), permissionamento (reuso `orcamento.aprovar`), financeiro/cobranças (aba do paciente)

---

## 1. Contexto e motivação

Fechamento da espinha financeira da **cirurgia**: do prontuário ao orçamento à cobrança. Hoje o profissional preenche a evolução, indica os procedimentos (F3 — snapshot `{catalogoCirurgiaId, descricao, valor}` no `ConteudoJson`), marca "Criar orçamento" na conduta (F3B), mas o orçamento nasce **em branco** — o profissional/recepção redigita tudo. E, quando o orçamento é aprovado, **nada vira cobrança**: o `OrcamentoAprovadoEventHandler` é um placeholder vazio (`Task.CompletedTask`), e o bloco `historicoValor` da aba financeiro (F2) renderiza sempre vazio (`HistoricoValorAbaDto` existe, `CobrancaQueryRepository` retorna `Array.Empty`).

F5 destrava as três pontas:
1. **Pré-preenchimento**: o link "Criar orçamento" leva ao form **já com paciente + cirurgias** vindos dos procedimentos indicados da evolução.
2. **Cobrança na aprovação**: aprovar o orçamento gera, **na mesma transação**, uma `Cobranca` origem=Cirurgia com `valor_cobrado` = total do orçamento.
3. **Histórico de valor**: se o total da cobrança de cirurgia muda, registra-se o histórico (de→para, quem, quando) — populando o bloco que a F2 já desenha.

**Evidência de prontidão (verificada no código):**
- `OrcamentoAprovadoEvent(OrcamentoId, EstabelecimentoId, PacienteId, Total)` já carrega exatamente o que a cobrança precisa.
- `OrcamentoAprovadoEventHandler` é o **seam designado** ("Placeholder — integração financeira futura será implementada aqui").
- `EfUnitOfWorkScope.CommitAsync` despacha domain events **dentro da transação** (após 1º SaveChanges, antes do commit) → handler de evento que cria Cobranca é **atômico** com a aprovação; falha → rollback total.
- `OrcamentoFormView` já lê `?agendamentoId=&pacienteId=` da query (precedente exato para `?evolucaoId=`).
- `MarcarProcedimentoRealizadoCommandHandler` já parseia a seção `procedimentos-indicados` do `ConteudoJson` no formato `{ catalogoCirurgiaId, descricao, valor, observacao }` — F5 reusa esse contrato para o pré-preenchimento.
- `Cobranca` já tem `OrcamentoId?` (desde F1) e helpers `TotalPagoLiquido()`, `SaldoDevedor()`, `ArredondamentoMonetario` — base pronta para o histórico/bloqueio de redução.
- `HistoricoValorAbaDto { ValorAnterior, ValorNovo, AlteradoPorNome, AlteradoEm }` já é o contrato de saída; `CobrancaQueryRepository` é o único ponto que retorna `Array.Empty` a substituir.

## 2. Persona-alvo

- **Profissional / Recepção**: ao fechar a evolução de um caso cirúrgico, clica "Criar orçamento" na conduta e chega ao form pré-preenchido — economiza redigitação e erro.
- **Quem aprova orçamento** (papel com `orcamento.aprovar` — tipicamente Dono/Recepção): ao aprovar, a cobrança de cirurgia nasce sozinha e fica visível na aba Financeiro do paciente.
- Frequência: por caso cirúrgico (baixa frequência, alto valor — erro de centavo é caro).

## 3. Escopo

**Inclui**:
- Link "Criar orçamento" da conduta (F3B) passa a abrir `OrcamentoFormView` **pré-preenchido** via `?evolucaoId=&pacienteId=`.
- Endpoint de leitura leve que devolve os procedimentos-indicados de uma evolução (snapshot) para o front pré-popular as linhas de cirurgia + paciente.
- Implementar `OrcamentoAprovadoEventHandler`: na aprovação, criar `Cobranca` origem=Cirurgia, `orcamento_id` setado, `valor_cobrado` = `Total` do orçamento, tipo=Particular, paciente=paciente do orçamento — **idempotente** (1 cobrança ativa por orçamento) e **atômico** (mesma transação da aprovação).
- Nova entidade filha `CobrancaHistoricoValor` + método de domínio `SincronizarValorCobrado(novoTotal, alteradoPor)` na `Cobranca`; INV-1/INV-2 recalculam saldo/status sobre o novo total; bloqueio de redução abaixo do pago líquido (422).
- Popular `HistoricoValorAbaDto` na query da aba financeiro (substituir o `Array.Empty` da F2 no `CobrancaQueryRepository`).
- Pagamento de cirurgia **só** pela aba Financeiro do paciente (o `PaymentModal`/badge do agendamento nunca opera cobrança origem=Cirurgia).
- Atualização de `Docs/ARQUITETURA.md`.

**Não inclui**:
- Comissão da equipe do orçamento (`OrcamentoEquipe`) → F7.
- Convênio (cobrança de cirurgia é sempre Particular nesta fase — D2 da F4) → F6.
- Baixa de estoque da cirurgia → **já coberta pela F4** (ocorre ao marcar os procedimentos como realizados, mesmo gatilho; ver R11).
- Recibo de pagamento → F8.
- **Novo fluxo de edição de orçamento já aprovado** — não se inventa (ver §9, corte 4). O histórico de valor nasce como **gancho preparado**, alimentado apenas por re-disparo do `OrcamentoAprovadoEvent` sobre orçamento que já tem cobrança.
- Custos/lucro detalhados em Relatórios → F7.

## 4. Regras de negócio

- **R1 (pré-preenchimento — mecanismo)**: o link da conduta `CriarOrcamento` navega para `/orcamentos/novo?evolucaoId={evolucaoId}&pacienteId={pacienteId}`. **Query param** (não state de router) — sobrevive a refresh/copiar-link. Mora em: Front (`rotaParaAcao` em `pendenciaService.ts`, `OrcamentoFormView.vue`). Validada em: Front (UX); o backend valida o tenant ao servir o endpoint de leitura (R2).
- **R2 (endpoint de leitura do snapshot)**: novo `GET /api/prontuario/evolucoes/{evolucaoId}/procedimentos-indicados` (ou caminho equivalente sob o controller de prontuário existente) retorna `[{ catalogoCirurgiaId, descricao, valor }]` lidos do `ConteudoJson` da evolução, **filtrado por tenant** (evolução do prontuário do paciente do estabelecimento ativo). Itens **sem** `catalogoCirurgiaId` (legado texto-livre) são ignorados. Mora em: Query/Handler (Application) + Controller. Validada em: back (multi-tenant falha-fechada → "Não encontrado").
- **R3 (mapeamento snapshot → linhas do orçamento)**: cada procedimento indicado vira uma linha de **`OrcamentoCirurgia`** (`descricao`, `valorTotal` = `valor` do snapshot, `quantidade`=1, `procedimentoCirurgicoId` = `catalogoCirurgiaId`). O front pré-popula; nada é persistido até o usuário **salvar** o orçamento (paridade com `/orcamentos/novo` — POST só no salvar). Mora em: Front (montagem do form) + Domain (`Orcamento.Criar` já aceita `CirurgiaPayload`). Validada em: back (`OrcamentoCirurgia.Criar`).
- **R4 (conclusão da pendência — genérica, sem alterar F3B)**: criar o orçamento dispara `OrcamentoCriadoEvent`; o handler **existente** `ConcluirPendenciaAoCriarOrcamentoHandler` conclui a pendência `CriarOrcamento` mais recente do paciente+tenant. **F5 NÃO passa `evolucaoId` ao `CriarOrcamentoCommand`** nem altera esse handler (corte registrado §9). Mora em: já existente. Validada em: já existente (CA64/CA65 da F3B).
- **R5 (cobrança na aprovação — atômica)**: ao aprovar (`AprovarOrcamentoCommandHandler` → `Orcamento.Aprovar()` → `OrcamentoAprovadoEvent`), o `OrcamentoAprovadoEventHandler` cria `Cobranca.CriarParaCirurgia(estabelecimentoId, pacienteId, orcamentoId, valorCobrado=Total, descricao, criadoPor)`. A criação ocorre **dentro da transação** da aprovação (UoW despacha eventos antes do commit) → falha na cobrança reverte a aprovação. Mora em: Handler (`OrcamentoAprovadoEventHandler`) + Domain (`Cobranca`). Validada em: back.
- **R6 (idempotência — 1 cobrança ativa por orçamento)**: defense-in-depth dupla — (a) o handler consulta `ICobrancaRepository.ObterPorOrcamentoOuNulo(orcamentoId, estab)`; se já existe cobrança não-cancelada, **não cria outra** (executa o caminho de sincronização de valor — R8); (b) índice **UNIQUE parcial** `cobrancas (orcamento_id) WHERE origem='Cirurgia' AND orcamento_id IS NOT NULL` fecha a race no banco (23505 → tratado como já-existe, não 500). Mora em: Handler + DB. Validada em: back.
- **R7 (origem/tipo da cobrança de cirurgia)**: `origem='Cirurgia'`, `tipo_atendimento=Particular`, `convenio_id=null`, `agendamento_id=null` (cirurgia não tem agendamento na cobrança), `orcamento_id` setado, `paciente_id` = paciente do orçamento, `valor_cobrado` = `orcamento.Total` arredondado. Mora em: Domain (`CriarParaCirurgia`). Validada em: back.
- **R8 (histórico de valor — sincronização)**: o método `Cobranca.SincronizarValorCobrado(novoTotal, alteradoPorUsuarioId)`:
  - se `novoTotal == valor_cobrado` atual → **no-op** (sem linha de histórico);
  - se `novoTotal != valor_cobrado` → grava `CobrancaHistoricoValor { valor_anterior, valor_novo, alterado_por, alterado_em }`, atualiza `valor_cobrado` e recalcula `status` pela INV-2 sobre o novo total;
  - **bloqueio (R9)** aplicado antes de gravar.
  Disparado pelo `OrcamentoAprovadoEventHandler` quando R6 detecta cobrança já existente com `Total` diferente. Mora em: Domain (`Cobranca` + `CobrancaHistoricoValor`). Validada em: back.
- **R9 (bloqueio de redução abaixo do pago líquido)**: em `SincronizarValorCobrado`, se `novoTotal − desconto < TotalPagoLiquido()` → `BusinessException` 422 com mensagem explicativa ("O novo valor da cirurgia (R$ X) é menor que o total já pago (R$ Y). Estorne pagamentos antes de reduzir o valor."). Nada é alterado (rollback). Mora em: Domain. Validada em: back (422); front exibe a mensagem.
- **R10 (pagamento de cirurgia só pela aba)**: o `PaymentModal`/badge do agendamento **nunca** opera cobrança `origem='Cirurgia'`. Como cobrança de cirurgia tem `agendamento_id=null`, o badge agregado da agenda (join por `agendamento_id`) **naturalmente não a inclui** — sem código extra de exclusão; basta confirmar que o badge não a alcança. A aba Financeiro do paciente (F2) mostra os 3 tipos de origem (já mostra). Mora em: Front (verificação) + Query (badge da agenda, sem alteração esperada). Validada em: back + front.
- **R11 (relação com estoque — F4, sem código novo)**: a baixa de estoque da cirurgia **não** ocorre na aprovação do orçamento; ocorre quando os procedimentos são marcados como realizados na conduta (gatilho F4 — `MarcarProcedimentoRealizado`). F5 **não** mexe em estoque. Registrado para evitar baixa duplicada.
- **R12 (LGPD — descrição sem PII)**: `Cobranca.Descricao` de cirurgia é texto controlado sem dado clínico (ex.: `"Cirurgia — orçamento ORC-202606-0042"` ou título do orçamento se houver). Sem CID/diagnóstico. Mora em: Handler. Validada em: back.

## 5. Modelo de dados

### Tabela nova: `cobranca_historico_valor`
| Coluna | Tipo | Notas |
|---|---|---|
| `id` | bigint PK identity | |
| `cobranca_id` | bigint NOT NULL FK → `cobrancas(id)` ON DELETE CASCADE | entidade filha do aggregate `Cobranca` |
| `estabelecimento_id` | bigint NOT NULL | tenant redundante para defense-in-depth/índice (espelha `estorno_pagamentos`) |
| `valor_anterior` | numeric(14,2) NOT NULL | de→ |
| `valor_novo` | numeric(14,2) NOT NULL | →para |
| `alterado_por_usuario_id` | uuid NOT NULL | quem |
| `alterado_em` | timestamptz NOT NULL DEFAULT now() | quando |

- Índice: `ix_cobranca_historico_valor_cobranca_id (cobranca_id)` (leitura na expansão do card).
- **PII**: nenhuma (só valores + usuário + timestamp). Não entra em export LGPD com dado clínico.

### Alteração em `cobrancas`
- **Nenhuma coluna nova** (`orcamento_id`, `origem`, `agendamento_id` já existem desde F1).
- **Índice UNIQUE parcial novo** (idempotência R6):
  `CREATE UNIQUE INDEX ux_cobrancas_orcamento_cirurgia ON public.cobrancas (orcamento_id) WHERE origem = 'Cirurgia' AND orcamento_id IS NOT NULL;`
  (espelha exatamente o `ux_cobrancas_evolucao_procedimento` da F4.)

### Não muda
- `orcamentos`, `orcamento_cirurgia`, `lancamentos` — sem alteração de schema. A cobrança de cirurgia, quando paga, gera `Lancamento` pela INV-3 já existente (F1).

## 6. UX e fluxo

**Fluxo pré-preenchimento:**
1. Profissional salva evolução com procedimentos indicados → conduta tem item "Criar orçamento" → vira pendência (F3B).
2. No painel de pendências / modal pós-salvar, clica "Criar orçamento" → navega para `/orcamentos/novo?evolucaoId=42&pacienteId=7`.
3. `OrcamentoFormView` detecta `evolucaoId`, chama o endpoint R2, pré-popula: paciente (reusa `preselecionarPaciente` existente) + linhas de cirurgia (reusa o array `cirurgias` do form). Demais campos (equipe, formas de pagamento, local) ficam vazios para o usuário completar.
4. Usuário ajusta e **salva** (POST normal). `OrcamentoCriadoEvent` conclui a pendência (R4).
5. Usuário envia (Rascunho→Enviado) e aprova (Enviado→Aprovado) pelos fluxos existentes.

**Fluxo cobrança/aba:**
6. Na aprovação, a `Cobranca` de cirurgia nasce sozinha (R5). Aparece na aba Financeiro do paciente como card origem=Cirurgia.
7. Pagamento registrado **só** pela aba (R10). Expansão do card mostra pagamentos/estornos (F2) **e** o bloco `historicoValor` (agora populado quando há alterações).

**Componentes reutilizados**: `OrcamentoFormView.vue` (combo de paciente, array de cirurgias, query-param handler), aba Financeiro do paciente (F2 — cards, expansão, `HistoricoValorAba`), `pendenciaService.rotaParaAcao`. **Sem componente novo de design system.**

**Estados**: endpoint R2 vazio (evolução sem procedimentos de catálogo) → form abre só com paciente pré-selecionado, lista de cirurgias vazia (sem erro bloqueante); R2 falha/tenant inválido → form abre em branco com aviso não-bloqueante ("não foi possível carregar os procedimentos da evolução"). Bloqueio R9 → toast com a mensagem 422.

## 7. Critérios de aceite (testáveis)

- **CA97 (pré-preenchimento — caminho feliz)**: Dado uma evolução com 2 procedimentos indicados de catálogo, Quando o usuário clica "Criar orçamento" na conduta, Então navega para `/orcamentos/novo?evolucaoId=…&pacienteId=…` e o form abre com o paciente pré-selecionado e 2 linhas de `OrcamentoCirurgia` (descrição+valor do snapshot), **sem** ter feito POST de criação.
- **CA98 (query param robusto)**: Dado o form aberto com `?evolucaoId=…`, Quando o usuário dá refresh na página, Então o pré-preenchimento é refeito (sobrevive ao reload — não depende de state de router).
- **CA99 (snapshot — itens legado ignorados)**: Dado uma evolução com 1 item de catálogo e 1 item texto-livre (sem `catalogoCirurgiaId`), Quando o form pré-popula, Então só a linha do item de catálogo aparece.
- **CA100 (conclusão da pendência genérica)**: Dado a pendência `CriarOrcamento` aberta, Quando o usuário salva o orçamento pré-preenchido, Então a pendência conclui (via handler existente) com `referencia_id` = id do orçamento — **sem** F5 ter alterado o `CriarOrcamentoCommand` nem o handler.
- **CA101 (cobrança na aprovação — caminho feliz)**: Dado um orçamento Enviado de total R$ 5.000,00 do paciente P, Quando é aprovado, Então existe 1 `Cobranca` origem=Cirurgia, `orcamento_id` setado, `paciente_id`=P, `tipo`=Particular, `valor_cobrado`=5000,00, status=Aberta.
- **CA102 (atomicidade)**: Dado que a criação da cobrança falha (ex.: violação de invariante), Quando o orçamento é aprovado, Então a aprovação **inteira** é revertida (orçamento permanece Enviado, nenhuma cobrança gravada) — rollback verificado.
- **CA103 (idempotência — domínio)**: Dado um orçamento que já gerou cobrança de cirurgia, Quando o `OrcamentoAprovadoEvent` é re-disparado com o mesmo `Total`, Então **nenhuma** segunda cobrança é criada e nenhuma linha de histórico é gravada (no-op).
- **CA104 (idempotência — banco)**: Dado uma corrida que tente inserir 2 cobranças para o mesmo `orcamento_id` origem=Cirurgia, Quando ambas commitam, Então o índice UNIQUE parcial rejeita a segunda (23505) e o handler trata como já-existe (não 500).
- **CA105 (histórico de valor — sincronização)**: Dado uma cobrança de cirurgia de R$ 5.000,00 sem pagamento, Quando o `OrcamentoAprovadoEvent` é disparado com `Total`=R$ 6.000,00, Então `valor_cobrado` vira 6000,00, grava-se 1 `CobrancaHistoricoValor {anterior:5000, novo:6000, alterado_por, alterado_em}` e o status recalcula pela INV-2.
- **CA106 (histórico exibido na aba)**: Dado a cobrança com histórico do CA105, Quando o usuário expande o card na aba Financeiro, Então o bloco `historicoValor` mostra a linha 5.000 → 6.000 (com quem/quando) — substituindo o `Array.Empty` da F2.
- **CA107 (bloqueio de redução abaixo do pago)**: Dado uma cobrança de cirurgia de R$ 5.000,00 com R$ 3.000,00 já pagos (líquido), Quando se tenta sincronizar para um novo total de R$ 2.000,00, Então recebe 422 com mensagem explicativa, `valor_cobrado` permanece 5.000,00 e nenhuma linha de histórico é gravada.
- **CA108 (redução permitida acima do pago)**: Dado a mesma cobrança com R$ 3.000,00 pagos, Quando sincroniza para novo total R$ 4.000,00, Então grava histórico, `valor_cobrado`=4000,00 e status=ParcialmentePaga (4000−0 > 3000).
- **CA109 (pagamento só pela aba)**: Dado uma cobrança origem=Cirurgia (sem `agendamento_id`), Quando o usuário abre a agenda do paciente, Então o badge/`PaymentModal` do agendamento **não** oferece pagamento dessa cobrança; o pagamento só é possível pela aba Financeiro do paciente.
- **CA110 (multi-tenant — endpoint de leitura)**: Dado um usuário do estabelecimento B, Quando chama o endpoint R2 para uma evolução do estabelecimento A, Então recebe "Não encontrado" (genérico) e nada é logado com PII.
- **CA111 (multi-tenant — cobrança)**: Dado um usuário do estabelecimento B, Quando lista a aba Financeiro de um paciente de A, Então não vê cobranças de cirurgia de A (filtro `estabelecimento_id`).
- **CA112 (RBAC — reuso)**: Dado um usuário **sem** `orcamento.aprovar`, Quando tenta aprovar o orçamento, Então recebe 403 (gate `[RequiresAcao("orcamento","aprovar")]` já existente) e nenhuma cobrança é criada. **Nenhuma permissão nova é introduzida.**
- **CA113 (LGPD)**: Dado a cobrança de cirurgia criada, Quando se inspeciona `Cobranca.Descricao` e a tabela de histórico, Então não há CID/diagnóstico/PII — só texto controlado, valores, usuário e timestamp.
- **CA114 (estados vazios)**: Dado uma evolução sem procedimentos de catálogo, Quando o form abre via `?evolucaoId=`, Então o paciente é pré-selecionado, a lista de cirurgias fica vazia e nenhum erro bloqueante aparece.
- **CA115 (regressão — orçamento sem evolucaoId)**: Dado o fluxo `/orcamentos/novo` **sem** query param, Quando o usuário cria um orçamento manualmente, Então o comportamento é idêntico ao atual (nada de pré-preenchimento, nenhuma regressão).
- **CA116 (regressão — aprovação sem F5 ativo no front)**: Dado a aprovação de um orçamento **não-cirúrgico** ou de fluxo legado, Quando aprovado, Então a cobrança de cirurgia é gerada normalmente pelo handler (o handler trata qualquer `OrcamentoAprovadoEvent`) **sem** quebrar conversão-em-cirurgia, expiração, recusa ou os demais comandos de orçamento existentes.
- **CA117 (doc viva)**: Dado o merge da F5, Quando se abre `Docs/ARQUITETURA.md`, Então a área "Cobranças" descreve a cobrança origem=Cirurgia na aprovação, o `CobrancaHistoricoValor`, a idempotência por índice parcial e o seam `OrcamentoAprovadoEventHandler`.

## 8. Riscos e dependências

- **Dependências**: F1 (`Cobranca`/`Pagamento`), F2 (aba financeiro + `HistoricoValorAbaDto`), F3 (snapshot `procedimentos-indicados`), F3B (pendência `CriarOrcamento` + `ConcluirPendenciaAoCriarOrcamentoHandler`), aggregate `Orcamento` (~95%, `Aprovar()` + `OrcamentoAprovadoEvent` existentes).
- **Risco — atomicidade**: a cobrança nasce no event handler dentro do UoW; QA deve validar rollback (CA102). Confirmar que `OrcamentoAprovadoEventHandler` recebe o `ICobrancaRepository` por DI e que o `Salvar` participa do mesmo `AppDbContext`/transação (padrão F4).
- **Risco — histórico "à toa"**: só gravar linha quando o valor **muda** (R8 no-op em igualdade) — não versionar re-aprovações idênticas.
- **Risco — redução com pagamento parcial**: R9/CA107 é a trava de segurança; sem ela, saldo negativo. Não-negociável.
- **Risco — duplicação de baixa de estoque**: F5 **não** mexe em estoque (R11). QA confirma que aprovar orçamento não dispara `MovimentacaoEstoque`.
- **Risco — regressão do fluxo de orçamento**: `OrcamentoFormView` é central; o pré-preenchimento deve ser **aditivo** (só quando `?evolucaoId=` presente). CA115 protege.

## 9. Observações para execução

**Decisões/cortes registrados (execução autônoma):**

1. **Mecanismo de pré-preenchimento = query param `?evolucaoId=&pacienteId=`** (não state de router). Robusto a refresh/link compartilhado; reusa o precedente exato de `?agendamentoId=&pacienteId=` já no `OrcamentoFormView`. **Não-negociável.**

2. **Vínculo orçamento↔evolução = NÃO persistir FK; conclusão de pendência permanece genérica.** O `ConcluirPendenciaAoCriarOrcamentoHandler` já conclui pela pendência `CriarOrcamento` mais recente do paciente+tenant — comportamento suficiente e simples. F5 **não** passa `evolucaoId` ao `CriarOrcamentoCommand` nem cria FK `orcamento.evolucao_id`. Custo: se o paciente tiver 2 pendências `CriarOrcamento` simultâneas, conclui a mais recente — caso raríssimo, aceitável. **Corte deliberado pela simplicidade.**

3. **Cobrança na aprovação vive no `OrcamentoAprovadoEventHandler`** (o placeholder designado), não inline no command handler. Mantém o command handler enxuto e usa o seam de eventos já documentado. Atomicidade garantida pelo `EfUnitOfWorkScope` (eventos despachados dentro da transação). **Reuso do padrão sobre duplicação.**

4. **Histórico de valor = gancho preparado, não fluxo de edição novo.** Verificado no código: `Orcamento` **não permite editar status Aprovado** (`Atualizar`/`GarantirEditavel` exigem Rascunho/Enviado) e **não há re-aprovação** (`Aprovar()` exige Enviado). Logo, hoje, **não existe caminho de produção que altere o `Total` de um orçamento já aprovado** — a entidade `CobrancaHistoricoValor`, o método `SincronizarValorCobrado` e o populamento do DTO são entregues **prontos e testados**, alimentados pelo handler quando o `OrcamentoAprovadoEvent` for re-disparado sobre um orçamento que já tem cobrança (R6→R8). **NÃO inventar** um fluxo de "editar orçamento aprovado" nesta fase — isso é decisão de produto futura (registrar como backlog). A infra de histórico fica pronta para quando esse fluxo existir, e o bloco já aparece na aba (F2 desenhou). Esta é a leitura fiel da decisão q12 ("a cobrança acompanha o orçamento mantendo histórico") sem extrapolar escopo.

5. **Cobrança de cirurgia tem `agendamento_id=null`** → o badge da agenda (join por `agendamento_id`) não a alcança naturalmente; R10 é, na prática, uma **verificação** (não precisa de exclusão explícita de código). Se o QA encontrar que o badge/`PaymentModal` alcança cobrança de cirurgia por algum caminho, é spec gap → escalar.

6. **Tipo sempre Particular** (D2 herdada da F4) — convênio de cirurgia é F6.

**Liberdade técnica do dev/db:** nome exato do método do repositório (`ObterPorOrcamentoOuNulo`), caminho exato do endpoint R2 (pode pendurar sob o controller de prontuário/evolução existente), nome da migration. **Não-negociável:** atomicidade (R5/CA102), idempotência dupla (R6/CA103-104), bloqueio R9/CA107, query param R1, reuso da permissão `orcamento.aprovar` (sem permissão nova), descrição sem PII (R12/CA113).

**Acionar `imedto-database`** para a tabela `cobranca_historico_valor` + índice UNIQUE parcial (ver §10).

## 10. Schema para o imedto-database

```sql
-- Migration F5 — cobrança de cirurgia: histórico de valor + idempotência por orçamento.

-- 1) Tabela de histórico de valor (entidade filha de cobrancas)
CREATE TABLE IF NOT EXISTS public.cobranca_historico_valor (
    id                       bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cobranca_id              bigint      NOT NULL REFERENCES public.cobrancas (id) ON DELETE CASCADE,
    estabelecimento_id       bigint      NOT NULL,
    valor_anterior           numeric(14,2) NOT NULL,
    valor_novo               numeric(14,2) NOT NULL,
    alterado_por_usuario_id  uuid        NOT NULL,
    alterado_em              timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_cobranca_historico_valor_cobranca_id
    ON public.cobranca_historico_valor (cobranca_id);

-- 2) Idempotência F5: 1 cobrança ativa de cirurgia por orçamento
--    (espelha ux_cobrancas_evolucao_procedimento da F4)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'ux_cobrancas_orcamento_cirurgia') THEN
        CREATE UNIQUE INDEX ux_cobrancas_orcamento_cirurgia
            ON public.cobrancas (orcamento_id)
            WHERE origem = 'Cirurgia' AND orcamento_id IS NOT NULL;
    END IF;
END $$;
```

- `numeric(14,2)` para casar com o padrão monetário do módulo (decimal, 2 casas).
- Sem alteração em `cobrancas`/`orcamentos`/`lancamentos`.
- Multi-tenant: `estabelecimento_id` redundante na tabela filha (defense-in-depth + filtro de leitura), espelhando `estorno_pagamentos`.
- Migration EF Core + SQL idempotente, no padrão de `db/migrations/`.
