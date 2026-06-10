# Estoque — alerta de estoque mínimo (notificação interna) + custo médio na movimentação

**ID**: 2026-06-10_003
**Status**: Aprovado por usuário em 2026-06-10 (modo autônomo — decisões fornecidas pelo orquestrador; ambiguidades residuais resolvidas com default mais simples em §4 e §11)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P-M (menor do que o roadmap sugere — ver §1)
**Áreas regressivas tocadas**: estoque (movimentação), notificações. Não toca: orçamento, prontuário, financeiro, agenda.

## 1. Contexto e motivação

O roadmap (`Docs/Roadmap/FASE_1_COMPLETUDE.md`, item 1.4) lista o módulo de estoque como "70% → 100%", citando como pendências "alertas de estoque mínimo (notificação interna já existe como canal), custo médio na movimentação".

**A investigação do código (2026-06-10) revelou que a maior parte já está pronta** — este briefing corrige o escopo para o que de fato falta, evitando reimplementar o que existe:

Já existe e **não deve ser refeito**:
- `ItemInventario` já tem as colunas `QuantidadeMinima`, `CustoMedio` e `CustoUnitario` (migration `20260516125710_criar_cadastros_estoque.sql`).
- `ItemInventario.RegistrarEntrada()` **já recalcula o custo médio ponderado** corretamente; `RegistrarSaida()`/`Inativar()` não alteram custo médio (saída usa `CustoMedio` como snapshot). 
- `InventarioQueryRepository.ListarItens` já devolve `CustoMedio`, `QuantidadeMinima` e o booleano `EstoqueAbaixoMinimo`, e já suporta o filtro `apenasAbaixoMinimo`.
- O frontend `EstoqueItensTab.vue` já exibe a coluna **Custo médio**, o mínimo (`/ mín X`), destaca a linha (`.alerta`) quando abaixo do mínimo e desenha a barra-medidor colorida; `EstoqueAlertasTab.vue` já lista "estoque baixo" e "esgotados".
- `RegistrarSaida()` **já dispara** o evento `EstoqueAbaixoMinimoEvent`.

O que **realmente falta** (dor de produto): quando um item cruza o estoque mínimo, **ninguém é avisado** — o `EstoqueAbaixoMinimoEventHandler` é um placeholder vazio (`return Task.CompletedTask`). Além disso, o disparo do evento hoje ocorre em **toda** saída que termine abaixo do mínimo (e não só no momento do cruzamento), o que geraria spam de notificação a cada saída de um item já em falta.

Esta demanda fecha essas duas lacunas e garante que o módulo possa ser apresentado como "100%": **o alerta de estoque mínimo realmente notifica a equipe responsável, uma vez, no cruzamento**, e o custo médio (já calculado) fica confirmado e coberto por CA.

## 2. Persona-alvo

- **Dono / Recepcionista / qualquer usuário com a ação RBAC `estoque`** no estabelecimento — quem repõe insumos/medicamentos. Momento da jornada: operação diária (registro de saída por procedimento/consumo). Frequência: alta em clínicas com consumo de insumo; o alerta evita ruptura de estoque (item zerado na hora do atendimento).

## 3. Escopo

**Inclui**:
- **Wire** do `EstoqueAbaixoMinimoEventHandler` (hoje no-op) para **criar notificação interna** (sino) via `INotificacaoService.EnviarAsync`, **uma por usuário** com a ação `estoque` no estabelecimento do item.
- Nova categoria de notificação **`Estoque`** no enum `CategoriaNotificacao` (string — sem migration, ver §5).
- **Correção da semântica de disparo**: o `EstoqueAbaixoMinimoEvent` passa a ser disparado **apenas no cruzamento** de `>= mínimo` para `< mínimo` (não em toda saída que termine abaixo). Sem novo campo de estado — usa-se a quantidade anterior vs. nova, já disponíveis no agregado.
- Nova consulta de leitura para resolver os destinatários: "usuários com a ação `estoque` no estabelecimento" (owner + vínculos ativos cujo modelo concede a área `estoque`).
- Confirmação por CA de que o **custo médio ponderado** recalcula na entrada e **não** muda em saída/ajuste (comportamento já existente).
- Confirmação por CA do **indicador visual** (badge/realce "abaixo do mínimo") na listagem de itens (já existente em `EstoqueItensTab.vue`).

**Não inclui** (explicitamente fora — registrar como backlog F4+ se solicitado):
- Pedido de compra / reposição sugerida automatizada / integração com fornecedor (roadmap já manda para F4+).
- Alerta por **e-mail** (MVP é só notificação interna).
- Tela nova de "reposição sugerida" (o roadmap cita; fora deste MVP — a aba `EstoqueAlertasTab` já cobre a visualização de baixos/esgotados).
- Coluna nova `estoque_minimo` nullable (a decisão original pedia "nullable, default null = sem alerta"; ver §4/R2 — resolvido sem schema, reusando `quantidade_minima = 0` como "sem alerta").
- Tipo de movimentação "Ajuste": o enum atual é `Entrada | Saida | Inativacao` — **não existe "Ajuste"**. Nada a fazer; o comando só aceita Entrada/Saida. (Registrado para não gerar confusão com a decisão original que citava "Ajuste".)

## 4. Regras de negócio

- **R1 — Alerta só no cruzamento (anti-spam)**. O `EstoqueAbaixoMinimoEvent` é disparado por `ItemInventario.RegistrarSaida()` **somente quando** `QuantidadeAnterior >= QuantidadeMinima` **e** `QuantidadeApos < QuantidadeMinima` (a saída fez o saldo cruzar a linha). Se o item já estava abaixo do mínimo antes da saída, **não** dispara de novo. Como entrada só aumenta o saldo, o cruzamento descendente só pode ocorrer em saída — `RegistrarEntrada` não dispara o evento. Mora em: **Domain** (`ItemInventario.RegistrarSaida`). Validada em: back (regra de domínio; é a fonte da verdade — não há trava de front correspondente).
  - **Reabastecer e cair de novo gera novo alerta**: se o item subiu para `>= mínimo` (entrada) e depois uma saída o leva a `< mínimo`, isso é um **novo** cruzamento e **deve** alertar de novo. A regra baseada em anterior-vs-novo já garante isso naturalmente, sem flag persistida.

- **R2 — "Sem alerta" = mínimo zero (sem schema novo)**. A decisão original pedia um campo `estoque_minimo` nullable (null = sem alerta). A coluna existente `quantidade_minima` é `numeric(12,3) NOT NULL` com default 0. Com `quantidade_minima = 0`, a condição de cruzamento `< 0` nunca ocorre → **0 funciona exatamente como "sem alerta"**. Adotamos isso para **evitar uma migration e uma segunda coluna semântica duplicada**. Mora em: **Domain** (a comparação já existe). Sem mudança de schema.

- **R3 — Destinatários do alerta = usuários com ação `estoque`**. A notificação é criada **uma por usuário** que possua a ação granular da área `estoque` no estabelecimento do item: isso inclui o **dono** (sempre) e cada profissional com vínculo **ativo** cujo modelo de permissão conceda a área `estoque` (qualquer ação da área — coerente com `[RequiresAcao("estoque")]` no `InventarioController`). Mora em: **Handler** (`EstoqueAbaixoMinimoEventHandler`) + **nova query** de leitura. Validada em: back (a query é a fonte da verdade dos destinatários).

- **R4 — Multi-tenant**. Todos os destinatários resolvidos são do **mesmo `estabelecimento_id`** do item que cruzou o mínimo. A notificação é criada com `estabelecimentoId` = o do item. Nenhum usuário de outro estabelecimento recebe alerta. Mora em: **query de destinatários** (filtra `estabelecimento_id`) + **Handler**. Repositório falha-fechada: sem estabelecimento → nenhum destinatário.

- **R5 — LGPD / minimização na mensagem**. A mensagem do alerta contém apenas: nome do item, quantidade atual e mínima, unidade. **Não** contém dados de paciente, nem motivo clínico da saída, nem `observacao` da movimentação (que é texto livre e pode conter PII). O `NotificacaoService` já não loga título/mensagem. Mora em: **Handler** (monta a string). Validada em: back.

- **R6 — Custo médio ponderado (confirmação do existente)**. Em **entrada** com custo unitário informado (`> 0`, já obrigatório no command), o `CustoMedio` do item é recalculado: se não havia saldo, `CustoMedio = custoUnitario`; senão, média ponderada `((anterior*CustoMedio)+(qtd*custoUnitario))/(anterior+qtd)`. **Saída e inativação não alteram** o `CustoMedio` (saída registra o `CustoMedio` vigente como snapshot na movimentação). Mora em: **Domain** (`RegistrarEntrada`/`RegistrarSaida`). Já implementado — coberto por CA para travar regressão.

- **R7 — Indicador visual na listagem (confirmação do existente)**. Na listagem de itens, item com `quantidade_atual < quantidade_minima` (e `quantidade_minima > 0`) exibe realce/badge "abaixo do mínimo". Já implementado em `EstoqueItensTab.vue` (classe `.alerta` + medidor + `EstoqueStatusPill status="low"`). Mora em: **Front** (deriva de `estoqueAbaixoMinimo` do DTO). Coberto por CA para travar regressão.

- **R8 — Idempotência do POST de movimentação**. O endpoint `POST /api/inventario/movimentacoes` já é `[Idempotent]`. Reenvio do mesmo request (mesma idempotency-key) não deve gerar segunda movimentação nem segundo alerta. Mora em: **infra de Idempotência já existente** (sem mudança). Coberto por CA.

## 5. Modelo de dados

**Schema NÃO muda — `imedto-database` NÃO é acionado.** Confirmado:
- `itens_inventario`: `quantidade_atual numeric(12,3) NOT NULL`, `quantidade_minima numeric(12,3) NOT NULL`, `custo_medio numeric(18,4) NOT NULL`, `custo_unitario numeric(12,2) NULL` — todas já existem.
- `notificacoes.categoria` é `varchar(40)` **sem CHECK constraint** → adicionar o valor `Estoque` ao enum `CategoriaNotificacao` (persistido como string) **não requer migration**.
- A consulta de destinatários lê `vinculos` + `modelos_permissao` + estabelecimentos (donos) — tabelas já indexadas por `estabelecimento_id`. **Não criar índice novo** (a query roda no evento de saída, baixo volume; sem evidência de gargalo). Se `imedto-database` for consultado pelo dev por dúvida de query, é só para **revisar o SQL de destinatários**, não para schema.

Sem nova tabela de audit (notificação interna já é o registro).

## 6. UX e fluxo

**Backend-cêntrico** — o grosso da entrega é backend; o frontend já está pronto. Fluxo:

1. Usuário registra uma **saída** de um item via `EstoqueMovimentacaoModal.vue` → `POST /api/inventario/movimentacoes`.
2. Se a saída cruza o mínimo (R1), o domínio dispara `EstoqueAbaixoMinimoEvent`; o handler resolve os destinatários (R3) e cria 1 notificação por usuário (categoria `Estoque`, `linkAcao` apontando para a tela de estoque, ex.: `/estoque?aba=alertas` ou `/inventario` — dev confirma a rota real da view).
3. O sino do frontend (notificações in-app, store já existente) exibe o alerta para cada destinatário; clicar leva à tela de estoque.

**Indicador visual (já existe, confirmar)**:
- Na listagem de itens, linha realçada + `EstoqueStatusPill status="low"` quando abaixo do mínimo; `status="out"` quando zerado.

**Estados**:
- Sem destinatário com ação `estoque` (ex.: só o dono e ele é o autor): mesmo assim o dono recebe (é responsável). Se, por configuração, nenhum usuário tiver a ação, o handler **não falha** — apenas não cria notificação (log de info, sem PII).
- Erro ao criar notificação para um destinatário não deve impedir os demais nem reverter a movimentação (a movimentação já foi persistida e commitada antes do publish do evento — ver `RegistrarMovimentacaoEstoqueCommandHandler`). O handler deve ser resiliente.

**Mensagem do alerta** (sugestão, dev pode ajustar texto mantendo R5):
- Título: `Estoque abaixo do mínimo`
- Mensagem: `O item "{nome}" atingiu {quantidadeAtual} {unidade} (mínimo: {quantidadeMinima} {unidade}). Considere repor.`

## 7. Critérios de aceite (testáveis)

- **CA1 (alerta no cruzamento — caminho feliz)**: Dado um item com `quantidade_minima = 10` e `quantidade_atual = 12`, Quando um usuário registra uma **saída de 5** (saldo passa para 7), Então o saldo cruza o mínimo e é criada **notificação interna** (categoria `Estoque`) para cada usuário com ação `estoque` no estabelecimento, com a mensagem citando o item, 7 e 10.
- **CA2 (não re-alerta abaixo do mínimo)**: Dado o mesmo item já com `quantidade_atual = 7` (abaixo do mínimo 10), Quando o usuário registra **outra saída de 2** (saldo 5), Então **nenhuma nova notificação** de estoque mínimo é criada (não houve novo cruzamento).
- **CA3 (re-cruzamento após reabastecer alerta de novo)**: Dado o item em 5 (abaixo de 10), Quando entra uma **entrada de 20** (saldo 25, `>= mínimo`) e depois uma **saída de 16** (saldo 9, `< mínimo`), Então é criada **uma nova** notificação de estoque mínimo (novo cruzamento descendente).
- **CA4 (entrada nunca dispara alerta)**: Dado um item em 5 (abaixo de 10), Quando entra uma **entrada de 2** (saldo 7, ainda abaixo), Então **nenhuma** notificação de estoque mínimo é criada (entrada não dispara o evento).
- **CA5 (mínimo zero = sem alerta)**: Dado um item com `quantidade_minima = 0`, Quando qualquer saída é registrada (saldo cai a qualquer valor >= 0), Então **nenhuma** notificação de estoque mínimo é criada.
- **CA6 (destinatários = ação estoque, incluindo dono)**: Dado um estabelecimento com o dono + 1 recepcionista com ação `estoque` + 1 profissional **sem** a ação `estoque`, Quando ocorre um cruzamento, Então são criadas **exatamente 2** notificações (dono e recepcionista) e **nenhuma** para o profissional sem a ação.
- **CA7 (multi-tenant)**: Dado um cruzamento de mínimo de um item do **estabelecimento A**, Quando os destinatários são resolvidos, Então **nenhum** usuário do estabelecimento B recebe notificação, e a notificação criada tem `estabelecimento_id = A`.
- **CA8 (LGPD — minimização da mensagem)**: Dada a notificação de estoque mínimo gerada, Quando seu conteúdo é inspecionado, Então contém apenas nome do item, quantidades e unidade — **sem** dado de paciente, sem `observacao` da movimentação, sem PII; e o log do backend não imprime título/mensagem (só id/usuário/categoria).
- **CA9 (custo médio recalcula na entrada)**: Dado um item com `quantidade_atual = 10` e `custo_medio = 2,00`, Quando entra **10 unidades a custo unitário 4,00**, Então `custo_medio` passa a **3,00** (média ponderada) e `quantidade_atual` a 20.
- **CA10 (saída não altera custo médio)**: Dado um item com `custo_medio = 3,00`, Quando uma **saída** é registrada, Então `custo_medio` permanece **3,00** e a movimentação de saída grava `custo_unitario = 3,00` (snapshot).
- **CA11 (custo médio na listagem/detalhe)**: Dado um item com `custo_medio = 3,00`, Quando a listagem de itens carrega, Então a coluna "Custo médio" exibe `R$ 3,00` para esse item.
- **CA12 (indicador visual — abaixo do mínimo)**: Dado um item com `quantidade_atual = 7` e `quantidade_minima = 10`, Quando a listagem de itens carrega, Então a linha desse item exibe o realce/badge "abaixo do mínimo" (`status low`); um item com saldo `>= mínimo` não exibe.
- **CA13 (RBAC — endpoint de movimentação)**: Dado um usuário **sem** a ação `estoque` no estabelecimento, Quando ele chama `POST /api/inventario/movimentacoes`, Então recebe 422/403 (gate `[RequiresAcao("estoque")]`) e nenhuma movimentação nem alerta é criada.
- **CA14 (idempotência)**: Dado um `POST /api/inventario/movimentacoes` de saída que cruza o mínimo, Quando o **mesmo** request é reenviado com a mesma idempotency-key, Então a movimentação **não** é duplicada e **não** é criada uma segunda notificação.
- **CA15 (resiliência / não reverte movimentação)**: Dado que a criação de notificação para um destinatário falhe, Quando o evento é processado, Então a **movimentação permanece persistida** (saldo correto) e os demais destinatários ainda recebem (falha de notificação não derruba a operação de estoque).

## 8. Riscos e dependências

- **Semântica do disparo é mudança de domínio**: alterar `RegistrarSaida` para disparar só no cruzamento muda o comportamento atual (que dispara sempre abaixo do mínimo). Garantir que os testes de domínio existentes de `ItemInventario` sejam atualizados/adicionados (CA1-CA5).
- **Fan-out por permissão é query nova**: não existe `IModeloPermissaoRepository.ListarUsuariosComAcao(...)`. Será preciso criar uma consulta de leitura (Dapper, read-side) que una donos + vínculos ativos com modelo que concede a área `estoque`. Validar o SQL contra o schema real (categorias de permissão são "area.acao" string em `modelos_permissao`). Reusar a lógica conceitual de `IModeloPermissaoRepository.UsuarioTemAcao` (dono sempre passa).
- **Volume de destinatários**: estabelecimentos pequenos (1-5 usuários) — custo trivial. Não otimizar prematuramente.
- **Área regressiva — movimentação de estoque**: não alterar o cálculo de saldo nem o de custo médio (R6 é confirmação, não reescrita). Tocar `RegistrarSaida` apenas na condição de disparo do evento.
- **Categoria nova no enum**: garantir que o frontend (sino) trate `Estoque` como categoria válida (ícone/agrupamento). Se o front tiver um `switch` por categoria, adicionar o caso; senão, cai no genérico.
- **Rota do `linkAcao`**: confirmar a rota real da view de estoque (`InventarioView`/`EstoqueCadastrosView`) para o deep-link da notificação.

## 9. Observações para execução

**Não-negociável**:
- Disparo do alerta **só no cruzamento** descendente (R1/CA1-CA5).
- 1 notificação **por usuário** com ação `estoque`, incluindo o dono (R3/CA6); multi-tenant estrito (R4/CA7).
- Mensagem sem PII; sem `observacao` da movimentação (R5/CA8).
- Custo médio: **não** reescrever a fórmula — só cobrir com CA (R6/CA9-CA10).
- **Sem migration** (R2/§5). Não acionar `imedto-database` para schema.
- Reuso da porta `INotificacaoService.EnviarAsync` — **não** instanciar `Notificacao` direto (segue o padrão de todos os handlers de notificação).

**Liberdade técnica (dev decide)**:
- Onde mora a query de destinatários: novo método em `IModeloPermissaoRepository` (read) ou um serviço de domínio dedicado — o que for coerente com a base.
- Texto exato da mensagem (mantendo R5) e ícone da categoria `Estoque` no sino.
- Se o handler resolve destinatários por uma única query ou itera vínculos — desde que multi-tenant e performático.

**Reuso obrigatório (grep antes de criar)**:
- `INotificacaoService.EnviarAsync` e `CategoriaNotificacao` (adicionar `Estoque`).
- Precedente de fan-out por equipe: `NotificarEquipeAoConfirmarHandler` (Cirurgias).
- `IModeloPermissaoRepository.UsuarioTemAcao` (lógica de "dono sempre passa" + área `estoque`).
- Domínio `ItemInventario.RegistrarSaida/RegistrarEntrada` (já existentes).
- Front: `EstoqueItensTab.vue`, `EstoqueAlertasTab.vue`, store de notificações já existente.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — adicionar nota curta (na seção de eventos de domínio / event handlers) sobre o **padrão de notificar um grupo por permissão**: handler de domínio que resolve destinatários via "usuários com ação X no estabelecimento" e faz fan-out por `INotificacaoService`. Citar `EstoqueAbaixoMinimoEventHandler` como exemplo. Mudança incremental, cirúrgica.
- **`Docs/LGPD.md`** — sem alteração estrutural: a regra (mensagem genérica sem PII, sem `observacao`) já está documentada; este alerta apenas a aplica.
- **`Docs/DESIGN.md`** — atualizar **somente se** o dev adicionar tratamento visual novo da categoria `Estoque` no sino que constitua padrão reutilizável. Se for só mais um `case` no componente existente, não atualizar. Decisão do dev; QA valida coerência.
- **Sem `INFRA.md`/`COMANDOS.md`** (sem recurso/infra/comando novo).

## 11. Decisões e assunções (execução autônoma)

1. **"Sem alerta" usa `quantidade_minima = 0`** em vez de uma coluna `estoque_minimo` nullable nova (R2). Default mais simples: zero migration, sem coluna semântica duplicada. Se o usuário exigir distinção entre "mínimo = 0 e quero alerta a cada saída" vs "sem alerta", isso vira addendum (caso de uso não evidenciado hoje).
2. **Rastrear o cruzamento** é feito comparando quantidade anterior vs. nova dentro de `RegistrarSaida` (dados já no agregado) — **sem** flag persistida tipo "já alertado". É a forma mais simples e cobre reabastecimento (CA3) naturalmente.
3. **"Ajuste" não existe** como tipo de movimentação (enum é `Entrada | Saida | Inativacao`). A decisão original citou "saída/ajuste não alteram custo médio" — interpretado como "apenas entrada altera custo médio"; nada a implementar para "ajuste".
4. **Custo na entrada já existe** (`CustoUnitario` obrigatório `> 0` em entradas, validado no command e no domínio) — não entra no schema; a decisão "investigar se existe; se não, entra no schema" resolve-se com "existe".
5. **MVP sem e-mail** e sem tela de "reposição sugerida" (a aba `EstoqueAlertasTab` já cobre a visualização). Notificação interna apenas.
6. **Destinatário inclui o autor da saída** se ele tiver a ação `estoque` — não excluímos o autor (ele pode querer o registro no sino; simplicidade). Se o usuário preferir não notificar o autor, vira addendum.
