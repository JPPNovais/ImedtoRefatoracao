# Módulo Financeiro / Cobranças — F1: Fundação + Consulta

**ID**: 2026-06-10_009
**Status**: Aprovado por usuário em 2026-06-10
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G (L no plano mestre)
**Áreas regressivas tocadas**: permissionamento (catálogo novo), financeiro (`Lancamento` ganha vínculo), agenda/check-in (regressão crítica), prontuário (nenhuma)

> **Fonte de verdade da visão**: `Docs/Roadmap/MODULO_FINANCEIRO_COBRANCAS.md` (§1 princípios, §2 modelo + INV-1..INV-8, §2.4 config, §3.1 fluxo consulta particular, §5 decisões RESOLVIDAS). Este briefing é a fonte de verdade da **execução da F1**. É **imutável** — gap vira addendum.
> **Referência visual obrigatória**: `Docs/Roadmap/prototipacao-financeiro/design-handoff/` (ver §6 e §11).

---

## 1. Contexto e motivação

Hoje o Imedto não tem **contas a receber do paciente**: o `/financeiro` lê apenas `Lancamento` avulso e o orçamento. Não existe a ponte "o paciente foi atendido → quanto foi cobrado → quanto foi pago". A F1 faz nascer o módulo de cobrança ponta a ponta para o **caso mais simples e mais frequente da clínica** — a **consulta particular** — sem errar centavo e sem vazar dado financeiro entre tenants/papéis.

Conceito-âncora (§1 do plano): **`cobrado ≠ pago`**. Uma `Cobranca` é a conta a receber; um ou mais `Pagamento` a quitam ao longo do tempo. São entidades distintas — não fundir.

A F1 é a fundação de domínio sobre a qual F2 (aba financeiro do paciente), F4 (procedimento), F5 (cirurgia) e F6 (convênio) plugam. Por isso o domínio da F1 **não pode bloquear** o que vem depois (estorno, convênio real, múltiplas origens), mesmo sem implementar tudo agora.

## 2. Persona-alvo

- **Recepção** (principal): faz o check-in, escolhe Particular/Convênio, confirma o valor sugerido, registra pagamento no balcão pelo ícone da agenda. Uso diário, alto volume.
- **Profissional / Dono**: também registra pagamento; só Dono/quem tem permissão financeira/de aprovar orçamento aplica **desconto** (INV-8).
- **Dono / Admin**: cadastra a tabela de preços e a taxa de cartão por forma de pagamento (aba de config do Financeiro).

Momento da jornada: **check-in → atendimento → cobrança no balcão**.

## 3. Escopo

**Inclui (IN)**:
- Agregado `Cobranca` (raiz) + entidade filha `Pagamento` + entidade `TabelaPrecoConsulta` + entidade `ConfigTaxaFormaPagamento`.
- Campo **Particular/Convênio** + **valor sugerido editável** na seção "Atendimento" do `CheckInModal.vue`.
- **Cobrança criada no check-in** (origem=Consulta) na mesma transação do check-in.
- **Badge de pagamento** no card da agenda (`AgendamentoRow.vue`) — 4 estados do protótipo, **agregado na query da agenda** (sem N+1).
- **PaymentModal** (modal Registrar pagamento) aberto pelo badge: pagamento **parcial**, **múltiplas formas**, **parcelas**, **taxa automática informativa** ("você recebe R$ X"), **desconto com RBAC** (INV-8).
- **Geração atômica de `Lancamento`** (Receita/Pago) a cada `Pagamento` (INV-3) + colunas `cobranca_id`/`pagamento_id` em `lancamentos`.
- **Permissões novas** `financeiro_paciente.ver` e `financeiro_paciente.registrar` no `CatalogoPermissoes.cs` + espelho no front (`permissions.ts`).
- **Tela mínima de cadastro da tabela de preços** + **config de taxa por forma de pagamento**, na **aba de Configuração do Financeiro** (ver §6 — decisão de corte: vive na config, não cria tela órfã).
- **Helper único de arredondamento** (2 casas, `MidpointRounding.AwayFromZero`) no domínio.
- INV-1..INV-6 e INV-8 implementadas no domínio.

**Não inclui (OUT)**:
- **Estorno de pagamento** (INV-7) → **F2** (decisão de corte — ver §9). O domínio nasce preparado para não bloquear (Pagamento imutável; status derivado de soma líquida).
- **Aba Financeiro do paciente** → F2.
- **Cadastro de Convênio real / select de convênio** → F6. Na F1, tipo=Convenio apenas **persiste o tipo** e nasce **sem valor de balcão** e **sem `convenio_id`** (decisão de corte — ver §9).
- Recibo PDF (F8), NFS-e (F9), procedimento (F4), cirurgia (F5), redesign `/financeiro` / caixa / comissões (F7).
- Teto de desconto por papel (anti-escopo §6 do plano).

## 4. Regras de negócio

> Todas as regras com cálculo/invariante moram no **Domain** (agregado `Cobranca` + métodos puros) e são validadas no **back (422 `BusinessException`)**; a trava de front é UX e tem espelho obrigatório. Cálculo monetário **sempre no backend** — o front só faz preview.

- **R1 — Cobrança nasce no check-in (origem=Consulta).** Ao confirmar o check-in com tipo=Particular, cria-se uma `Cobranca` (origem=Consulta, tipo=Particular, status=Aberta, `valor_cobrado`=valor informado, `desconto`=0) **na mesma transação** do `RegistrarCheckIn`. Mora em: Domain (`Cobranca.CriarParaConsulta`) + Handler de check-in. Validada em: back + front.
- **R2 — Valor sugerido editável.** No check-in, o sistema sugere o valor da `TabelaPrecoConsulta` (preço do profissional do agendamento; se ausente, preço padrão do estabelecimento; se nenhum, campo vazio editável). O valor é sempre editável pela recepção. Mora em: Query (sugestão) + Front (preenche) + Domain (valida `> 0` ao criar a cobrança). Validada em: back + front.
- **R3 — INV-1 (não pagar além do saldo).** `SUM(pagamentos.valor) ≤ valor_cobrado − desconto`. Excesso → 422. Mora em: Domain (`Cobranca.RegistrarPagamento`). Validada em: back + front (preview do saldo).
- **R4 — INV-2 (status derivado).** `status` é derivado, nunca setado à mão (salvo Cancelada): sem pagamento → Aberta; soma < total líquido → ParcialmentePaga; soma = total líquido → Paga. Recalculado a cada pagamento. Mora em: Domain. Validada em: back.
- **R5 — INV-3 (Lancamento atômico).** Registrar `Pagamento` **gera um `Lancamento`** (Tipo=Receita, Status=Pago, `cobranca_id` e `pagamento_id` preenchidos, `categoria` reusando `CategoriaFinanceira` de receita de consulta) **na MESMA transação**. Falha em qualquer um faz rollback de ambos. Mora em: Handler (UnitOfWork/transação) + Domain. Validada em: back (teste de rollback obrigatório).
- **R6 — INV-4 (desconto).** `0 ≤ desconto ≤ valor_cobrado`. Fora da faixa → 422. Mora em: Domain. Validada em: back + front.
- **R7 — INV-5 (valor do pagamento).** Cada `Pagamento.valor > 0`. Mora em: Domain. Validada em: back + front.
- **R8 — INV-6 (vínculo obrigatório).** `Cobranca` sempre tem `estabelecimento_id` e `paciente_id` não-nulos. Mora em: Domain (factory). Validada em: back.
- **R9 — INV-8 (RBAC de desconto).** Só aplica desconto quem tem `orcamento.aprovar` **OU** `financeiro.lancar`/`financeiro_paciente.registrar` **OU** for **Dono**. Sem permissão → 422 ao tentar gravar cobrança/pagamento com desconto > 0; no front o campo de desconto fica oculto. Mora em: Handler (checagem de permissão) + Domain (recebe flag `podeAplicarDesconto`) + Front (oculta campo). Validada em: back + front.
- **R10 — Taxa automática informativa.** A `taxa` de cada `Pagamento` é **derivada da `ConfigTaxaFormaPagamento`** da forma escolhida (taxa_percentual × valor), calculada no backend e **gravada** no `Pagamento`. Nunca digitada manualmente. No PaymentModal ela é **informativa** ("você recebe R$ X") — **não altera** `valor_cobrado` nem o saldo da cobrança (a taxa é custo do estabelecimento, não desconto ao paciente). Mora em: Domain (cálculo) + Front (exibe). Validada em: back + front.
- **R11 — Múltiplas formas / parcelas.** Um pagamento pode ser registrado em mais de uma forma numa mesma operação (gera 1 `Pagamento` por forma, cada um com seu `Lancamento` atômico, todos na mesma transação). `parcelas` (default 1) e `juros` (default 0) são gravados no `Pagamento`; nesta fase parcelas/juros são **informativos** (não geram lançamentos parcelados — fluxo de caixa simples, anti-escopo §6 do plano). Mora em: Domain + Handler. Validada em: back.
- **R12 — Convênio sem balcão na F1.** Se tipo=Convenio no check-in, a `Cobranca` nasce tipo=Convenio, `valor_cobrado`=0, **sem `convenio_id`** e **sem campos de balcão**; o PaymentModal **não** registra pagamento de balcão para cobrança de convênio (mostra estado "em breve / via convênio na F6"). Mora em: Domain + Front. Validada em: back + front.
- **R13 — Arredondamento único.** Todo arredondamento monetário usa um helper único de domínio (2 casas, `MidpointRounding.AwayFromZero`). Nenhum cálculo de taxa/desconto/saldo arredonda fora dele. Mora em: Domain (helper). Validada em: back.
- **R14 — Multi-tenant falha-fechada.** Toda query/comando de `Cobranca`/`Pagamento`/`TabelaPrecoConsulta`/`ConfigTaxaFormaPagamento` filtra `estabelecimento_id` do tenant ativo. Sem tenant claim → vazio/throws. Mensagem genérica "não encontrado". Mora em: Repositório + Handler. Validada em: back.

## 5. Modelo de dados (resumo — detalhe para o DB agent em §10)

- **`cobrancas`** (nova, aggregate root): tenant `estabelecimento_id`, `paciente_id`, `origem` (enum aberto, F1 só `Consulta`), `agendamento_id?`, `orcamento_id?` (null na F1), `tipo_atendimento` (`Particular|Convenio`), `convenio_id?` (null na F1), `valor_cobrado decimal`, `desconto decimal default 0`, `status` (`Aberta|ParcialmentePaga|Paga|Cancelada`), `descricao`, `criado_por`, audit/timestamps.
- **`pagamentos`** (nova, filha de cobrança): `cobranca_id`, `valor decimal`, `forma_pagamento_id` (FK `FormaPagamento`), `parcelas int default 1`, `juros decimal default 0`, `taxa decimal` (derivada), `data_pagamento`, `registrado_por_usuario_id`, `lancamento_id` (FK), audit.
- **`tabela_preco_consulta`** (nova): `estabelecimento_id`, `profissional_id?` (null = padrão do estabelecimento), `valor_sugerido decimal`, `ativo`.
- **`config_taxa_forma_pagamento`** (nova): `estabelecimento_id`, `forma_pagamento_id` (FK), `taxa_percentual decimal`, `ativo`.
- **ALTER `lancamentos`**: + `cobranca_id bigint null` (FK) + `pagamento_id bigint null` (FK). Mantém `orcamento_id` existente.
- **Multi-tenant**: todas as 4 tabelas novas têm `estabelecimento_id` e índice por tenant.
- **LGPD**: dado financeiro do paciente é sensível. DTOs mínimos (só campos da tela). Sem PII em log/erro. Audit de acesso à aba financeiro fica na **F2** (onde nasce a porta direta ao dado por paciente) — a F1 audita criação de cobrança/pagamento via `criado_por`/`registrado_por_usuario_id` e timestamps.

## 6. UX e fluxo

**Referência visual obrigatória** (recriar o resultado, não copiar HTML; tokens tipográficos vencem o protótipo — CLAUDE.md §5):
- Check-in: `design-handoff/Agenda.html`, `components/CheckInModal.jsx`; screenshots `checkin-atendimento.png`, `checkin-particular.png`, `02-checkin-financeiro.png`.
- Badge + modal: `components/AppointmentRow.jsx` (`.payment-badge`), `components/PaymentModal.jsx`; screenshots `pay-badges.png`, `02-pay-states.png`, `02-pay-modal-parcial.png`, `modal-real.png`.

**6.1 Check-in (estende `CheckInModal.vue`, sem regredir sala/edição de paciente)**
- Nova seção "Atendimento" entre o resumo e a seção de sala: toggle **Particular / Convênio** (usar `AppPillToggle` do DS) + campo de valor (`AppField` + input monetário) **pré-preenchido** pelo valor sugerido (R2), editável.
- Tipo=Convenio: oculta o campo de valor de balcão e mostra hint "Cobrança de convênio será tratada na aba financeiro" (R12).
- Sem tabela de preços configurada e sem preço para o profissional: campo vazio com placeholder + hint "Configure a tabela de preços em Configurações > Financeiro". **Não bloqueia** o check-in (recepção pode digitar o valor).
- Ao confirmar: check-in + cobrança na mesma transação (R1).
- **Regressão**: seleção de sala, alerta de ocupação e edição de paciente continuam idênticos.

**6.2 Badge na linha da agenda (`AgendamentoRow.vue`)** — 4 estados:
- **Aberta**: pill "A receber R$ X" (cor de alerta neutra).
- **Parcial**: pill "R$ X de R$ Y" (cor de atenção).
- **Paga**: check discreto + "Pago".
- **Convênio**: tag "Convênio" sem valor.
- O badge só aparece se há cobrança; vem **agregado na query da agenda** (sem request por linha). Clique no badge abre o PaymentModal (exceto Convênio → estado "em breve").

**6.3 PaymentModal (componente novo no DS, reusável pela F2)**
- Cabeçalho com **saldo em destaque** (valor_cobrado − desconto − pagamentos).
- Histórico de pagamentos da cobrança (lista simples).
- Form: valor pré-preenchido pelo saldo; seleção de forma de pagamento (`AppSelect`, formas ativas do tenant); parcelas; possibilidade de adicionar mais de uma forma; **taxa exibida como info** ("você recebe R$ X") por linha de forma de cartão (R10).
- Campo **desconto** visível **só com permissão** (R9); ao informar desconto, recalcula saldo (preview front, fonte da verdade no back).
- Estados: loading, erro 422 (mensagem genérica do back), vazio (sem pagamentos ainda), sucesso (fecha e atualiza badge), valor excede saldo (bloqueia botão + mensagem).

**6.4 Config do Financeiro (decisão de corte — onde fica a tela)**
- Reusar o padrão master-detail de `OrcamentoSettingsView.vue`. Criar uma entrada **Financeiro** no grupo de Configurações com duas seções:
  1. **Tabela de preços de consulta** — lista por profissional + linha "padrão do estabelecimento"; CRUD mínimo (criar/editar/inativar).
  2. **Taxa de cartão por forma de pagamento** — uma linha por `FormaPagamento` ativa, com percentual editável e toggle ativo.
- Estados: vazio (nenhum preço cadastrado → empty state com CTA), loading, erro, sucesso.
- Não criar tela órfã: tudo vive dentro de Configurações.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — cobrança no check-in)**: Dado um agendamento confirmado e tipo=Particular com valor R$ 200, Quando a recepção confirma o check-in, Então o check-in é registrado E nasce uma `Cobranca` origem=Consulta, tipo=Particular, status=Aberta, valor_cobrado=200, desconto=0, vinculada ao agendamento e ao paciente.
- **CA2 (valor sugerido editável)**: Dado que existe `TabelaPrecoConsulta` com R$ 250 para o profissional do agendamento, Quando o check-in abre, Então o campo de valor vem pré-preenchido com 250 E pode ser editado para outro valor antes de confirmar.
- **CA3 (badge 4 estados, agregado)**: Dado cobranças nos 4 estados na agenda do dia, Quando a lista carrega, Então cada card mostra o badge correto (Aberta "A receber R$ X" / Parcial "R$ X de R$ Y" / Paga check / Convênio tag) E a request da agenda é **uma só** (sem N+1 — o badge vem agregado na query).
- **CA4 (pagamento parcial muda status — INV-1/INV-2)**: Dado cobrança de R$ 200 status Aberta, Quando registro pagamento de R$ 80, Então status vira ParcialmentePaga e saldo=120; Quando registro mais R$ 120, Então status vira Paga e saldo=0.
- **CA5 (INV-1 excesso rejeitado)**: Dado cobrança de R$ 200 com R$ 150 já pagos (saldo 50), Quando tento registrar pagamento de R$ 80, Então recebo 422 com mensagem de negócio E nada é persistido; no front o botão fica bloqueado com aviso.
- **CA6 (atomicidade Pagamento↔Lancamento — INV-3)**: Dado registro de pagamento válido, Quando o `Pagamento` persiste, Então um `Lancamento` (Receita/Pago, cobranca_id + pagamento_id preenchidos) persiste na MESMA transação; E Quando se força falha na criação do `Lancamento` (teste), Então o `Pagamento` sofre rollback (nenhum dos dois persiste).
- **CA7 (taxa automática informativa — R10)**: Dado `ConfigTaxaFormaPagamento` de 3% para Cartão de Crédito, Quando registro pagamento de R$ 100 nessa forma, Então o `Pagamento.taxa`=3,00 gravado, o modal exibe "você recebe R$ 97,00", E o saldo da cobrança é abatido em R$ 100 (a taxa NÃO reduz o valor cobrado).
- **CA8 (desconto RBAC — INV-8/R9 — caminho permitido)**: Dado usuário Dono (ou com `orcamento.aprovar`/`financeiro.lancar`), Quando aplica desconto de R$ 20 em cobrança de R$ 200, Então desconto persiste, saldo=180 e status recalcula corretamente.
- **CA9 (desconto RBAC — bloqueio)**: Dado usuário sem `orcamento.aprovar`, sem `financeiro.lancar`/`financeiro_paciente.registrar` e não-Dono, Quando tenta gravar desconto > 0, Então recebe 422 E o campo de desconto está oculto no front.
- **CA10 (INV-4 desconto fora da faixa)**: Dado cobrança de R$ 100, Quando tento aplicar desconto de R$ 150 (ou negativo), Então recebo 422 e nada persiste.
- **CA11 (INV-5 valor do pagamento)**: Dado uma cobrança, Quando tento registrar pagamento de R$ 0 ou negativo, Então recebo 422.
- **CA12 (multi-tenant — leitura)**: Dado um usuário do estabelecimento B, Quando tenta acessar/registrar pagamento de uma `Cobranca` do estabelecimento A (id direto na rota), Então recebe 404/"não encontrado" genérico E nada é logado com PII.
- **CA13 (multi-tenant — config/preços)**: Dado um usuário do estabelecimento B, Quando lista tabela de preços ou taxas, Então vê **apenas** as do estabelecimento ativo (B), nunca de A; repositório falha-fechada sem tenant claim.
- **CA14 (RBAC permissões novas)**: Dado o `CatalogoPermissoes`, Quando carregado, Então existem `financeiro_paciente.ver` e `financeiro_paciente.registrar` E estão espelhadas em `frontend/src/constants/permissions.ts`; usuário sem `financeiro_paciente.registrar` não vê o botão de registrar pagamento e recebe 403/422 no back.
- **CA15 (LGPD)**: Dado qualquer erro de validação/negócio nesses fluxos, Quando o back retorna 422/404, Então a mensagem é genérica e não contém PII (nome/CPF do paciente, valor não revela tenant alheio); nenhum log de domínio carrega PII.
- **CA16 (estado — sem tabela de preços)**: Dado estabelecimento sem `TabelaPrecoConsulta` para o profissional nem padrão, Quando abre o check-in, Então o campo de valor vem vazio com hint para configurar E o check-in pode ser concluído com valor digitado manualmente (não bloqueia).
- **CA17 (estado — convênio sem balcão — R12)**: Dado check-in com tipo=Convenio, Quando confirma, Então a cobrança nasce tipo=Convenio, valor_cobrado=0, sem convenio_id; Quando clico no badge "Convênio", Então o modal de pagamento de balcão não é oferecido (estado "tratado via convênio — em breve").
- **CA18 (arredondamento — R13)**: Dado taxa de 3,333% sobre R$ 100,00, Quando calculada, Então o valor é arredondado a 2 casas com AwayFromZero (3,33) pelo helper único; nenhum cálculo usa float/double nem arredondamento divergente.
- **CA19 (performance / debounce)**: Dado a tela de tabela de preços com busca por profissional, Quando o usuário digita, Então há debounce (~300ms) e a lista é carregada do tenant ativo (sem consultar abas/dados não exibidos).
- **CA20 (regressão do check-in)**: Dado o fluxo de check-in existente, Quando confirmo check-in (com e sem sala), Então a seleção de sala, o alerta de ocupação e a edição de paciente continuam funcionando exatamente como antes E o `RegistrarCheckIn` mantém suas validações (cancelado/concluído/já feito).
- **CA21 (múltiplas formas — R11)**: Dado pagamento de R$ 200 dividido em R$ 120 dinheiro + R$ 80 cartão numa mesma operação, Quando confirmo, Então são gravados 2 `Pagamento` (cada um com seu `Lancamento` atômico), todos na mesma transação, e o saldo zera.
- **CA22 (doc viva)**: Dado a entrega da F1, Quando concluída, Então `Docs/LGPD.md` e `Docs/ARQUITETURA.md` foram atualizados conforme §10 (área de domínio Cobranças + permissões `financeiro_paciente.*`).

## 8. Riscos e dependências

- **Atomicidade Pagamento↔Lancamento (INV-3)**: maior risco técnico. Exige transação/UnitOfWork cobrindo domínio + EF. Teste de rollback é obrigatório (CA6).
- **Regressão do check-in**: mexer no `CheckInModal.vue` e no handler de check-in pode quebrar agenda/sala. CA20 blinda.
- **N+1 na agenda**: o badge não pode disparar request por linha — precisa vir agregado na query da agenda (CA3). Risco de performance se mal feito.
- **Não bloquear F2/F4/F5/F6**: domínio nasce com `origem` enum aberto, `orcamento_id?`, `convenio_id?`, Pagamento imutável (preparado para estorno F2/INV-7), status derivado de soma líquida. Não simplificar a ponto de travar a evolução.
- **Dependências**: nenhuma dura. Schema → `imedto-database` (4 tabelas novas + ALTER em `lancamentos`). Reusa `FormaPagamento`, `Lancamento`, `CategoriaFinanceira`, padrão `OrcamentoSettingsView.vue`, `CatalogoPermissoes`.

## 9. Observações para execução — decisões de corte (não-negociáveis)

- **Estorno (INV-7) fica na F2**, não na F1. Motivo: o protótipo coloca estorno na aba do paciente (F2); o risco/superfície de estorno não justifica entrar na fundação. **Não-negociável**: o domínio da F1 deve nascer preparado — `Pagamento` é **imutável** (sem update/delete de valor), `Lancamento` do pagamento idem, e o `status` da cobrança é derivado da **soma líquida** (preparado para subtrair estornos depois). Nenhuma decisão da F1 pode forçar reescrita para suportar estorno na F2.
- **Convênio na F1 = corte mínimo (R12)**: tipo=Convenio **persiste só o tipo**, `valor_cobrado`=0, **sem `convenio_id`** (a coluna existe nullable no schema, mas a F1 não a popula — F6 a usa). Sem select de convênio real. Sem balcão. Decidi **omitir o populamento de `convenio_id`** (mais simples que campo texto) — a coluna fica reservada para a F6.
- **Tela da tabela de preços + taxa**: vivem na **aba de Configuração do Financeiro** (grupo Configurações, padrão master-detail de `OrcamentoSettingsView.vue`). **Não criar view órfã** nem rota solta. Esta aba é a mesma §2.4 do plano.
- **Taxa nunca digitada manualmente** (R10): vem 100% da config; no pagamento é só informativa e não altera saldo.
- **Liberdade técnica do dev/db**: nomes EF, estrutura de handlers/queries, forma de agregar o badge na query da agenda (join vs subquery), nome do helper de arredondamento. **Não-negociável**: decimal sempre, cálculo no back, transação atômica, multi-tenant falha-fechada, helper único de arredondamento, reuso de `Lancamento`/`FormaPagamento`.
- **Reuso > duplicação**: o `PaymentModal` deve nascer como componente do DS (reusado pela F2). A config reusa o padrão do orçamento, não duplica tela.

## 10. Schema para o `imedto-database`

> Migration EF + SQL idempotente em `db/migrations/`. Multi-tenant + índices dia 1. Todos os valores monetários `numeric(12,2)` (decimal). Nomes em snake_case PT-BR consistentes com o schema atual.

**Tabela `cobrancas`** (aggregate root)
- `id bigserial PK`
- `estabelecimento_id bigint NOT NULL` (tenant)
- `paciente_id bigint NOT NULL`
- `origem text NOT NULL` (enum aberto; F1 grava `Consulta`)
- `agendamento_id bigint NULL` (FK agendamentos)
- `orcamento_id bigint NULL` (reservado F5; FK orcamentos)
- `tipo_atendimento text NOT NULL` (`Particular`|`Convenio`)
- `convenio_id bigint NULL` (reservado F6 — não populado na F1)
- `valor_cobrado numeric(12,2) NOT NULL`
- `desconto numeric(12,2) NOT NULL DEFAULT 0`
- `status text NOT NULL` (`Aberta`|`ParcialmentePaga`|`Paga`|`Cancelada`)
- `descricao text NULL`
- `criado_por_usuario_id uuid NOT NULL`
- `criado_em timestamptz NOT NULL`, `atualizado_em timestamptz NULL`
- Índices sugeridos: `(estabelecimento_id, paciente_id)`, `(estabelecimento_id, status)`, `(agendamento_id)` — este último para agregar o badge na query da agenda sem N+1.

**Tabela `pagamentos`** (filha de cobrança)
- `id bigserial PK`
- `cobranca_id bigint NOT NULL` (FK cobrancas)
- `valor numeric(12,2) NOT NULL`
- `forma_pagamento_id bigint NOT NULL` (FK formas_pagamento)
- `parcelas int NOT NULL DEFAULT 1`
- `juros numeric(12,2) NOT NULL DEFAULT 0`
- `taxa numeric(12,2) NOT NULL DEFAULT 0` (derivada da config no ato)
- `data_pagamento date NOT NULL`
- `registrado_por_usuario_id uuid NOT NULL`
- `lancamento_id bigint NOT NULL` (FK lancamentos — gerado atomicamente)
- `criado_em timestamptz NOT NULL`
- Índice: `(cobranca_id)`.

**Tabela `tabela_preco_consulta`**
- `id bigserial PK`
- `estabelecimento_id bigint NOT NULL` (tenant)
- `profissional_id uuid NULL` (null = preço padrão do estabelecimento — `profissional_usuario_id`, alinhar tipo ao usado em agendamentos)
- `valor_sugerido numeric(12,2) NOT NULL`
- `ativo boolean NOT NULL DEFAULT true`
- `criado_em timestamptz NOT NULL`, `atualizado_em timestamptz NULL`
- Índice: `(estabelecimento_id, profissional_id)` (parcial `WHERE ativo` opcional). Confirmar unicidade desejada (1 preço ativo por profissional + 1 padrão por estabelecimento).

**Tabela `config_taxa_forma_pagamento`**
- `id bigserial PK`
- `estabelecimento_id bigint NOT NULL` (tenant)
- `forma_pagamento_id bigint NOT NULL` (FK formas_pagamento)
- `taxa_percentual numeric(6,3) NOT NULL DEFAULT 0`
- `ativo boolean NOT NULL DEFAULT true`
- `criado_em timestamptz NOT NULL`, `atualizado_em timestamptz NULL`
- Índice/unique: `(estabelecimento_id, forma_pagamento_id)` único.

**ALTER `lancamentos`** (tabela existente)
- ADD `cobranca_id bigint NULL` (FK cobrancas)
- ADD `pagamento_id bigint NULL` (FK pagamentos)
- Índice: `(cobranca_id)`. Mantém `orcamento_id` intacto.

> Observações ao DB agent: confirmar o nome real da tabela de `FormaPagamento` (`formas_pagamento`?) e o tipo de `profissional` (uuid em agendamentos é `profissional_usuario_id`). FKs com `ON DELETE` conservador (RESTRICT) — dado financeiro não cascateia. Sem trigger/function com regra de negócio (regra mora no backend).

## 11. Atualização de documentação (parte da entrega da F1)

- **`Docs/ARQUITETURA.md`** — adicionar à seção de domínios/bounded contexts a área **Cobranças (Financeiro/contas a receber)**: agregado `Cobranca` (raiz) + `Pagamento` (filha), invariantes INV-1..INV-6/INV-8, regra de geração atômica de `Lancamento` (INV-3) e o padrão de **helper único de arredondamento monetário** (2 casas, AwayFromZero). Mudança incremental, cirúrgica — não reescrever o doc.
- **`Docs/LGPD.md`** — adicionar **dado financeiro do paciente** como categoria sensível: minimização de DTO, sem PII em log/erro de cobrança/pagamento, e nota de que o **audit de acesso por paciente entra na F2** (aba financeiro). Registrar as permissões novas `financeiro_paciente.ver`/`financeiro_paciente.registrar` e a distinção em relação a `financeiro.*` (clínica agregada).
- **`Docs/COMANDOS.md`** — **nenhuma mudança** prevista (sem script/comando novo).
- **`Docs/INFRA.md`** — **nenhuma mudança** (sem recurso AWS novo).
- **`Docs/DESIGN.md`** — adicionar `PaymentModal` (e confirmar `AppPillToggle`) à seção de componentes do design system, **se** o dev confirmar que nascem como componentes reutilizáveis (provável). Avaliar na implementação.
