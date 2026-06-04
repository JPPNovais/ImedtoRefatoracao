# Descartar rascunho de receita (soft-delete) + trava contra acúmulo de rascunhos

**ID**: 2026-06-03_002
**Status**: Aprovado por usuário em 2026-06-03
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (Receitas), permissionamento, relatório (lista de receitas do paciente)

## 1. Contexto e motivação

Hoje, ao iniciar uma receita, o sistema cria imediatamente um registro em status `Rascunho`. Quando o profissional desiste antes de emitir, ele não tem um caminho próprio de descarte: o front reusa o botão **"Cancelar receita"** no rodapé do rascunho (`ReceitasPacienteTab.vue`), que dispara o fluxo clínico `Receita.Cancelar(motivo)`. Esse método só aceita `Status == Emitida` e devolve `"Apenas receitas emitidas podem ser canceladas."`. Resultado prático observado:

1. O profissional vê uma mensagem de erro sem saída — não consegue se livrar do rascunho.
2. Rascunhos vazios/abandonados **acumulam** na lista de receitas do paciente, poluindo a tela e criando ruído clínico.

A separação de produto (definida pelo dono em 2026-06-03) é clara:
- **RASCUNHO** = documento que nunca existiu clinicamente → deve ser **DESCARTÁVEL** (some, sem rastro clínico, apenas audit operacional).
- **EMITIDA** = documento clínico real → só **CANCELÁVEL** (vira `Cancelada`, nada se apaga; é retificação, não exclusão).

A infraestrutura de descarte **já existe** e está pronta para reuso, sem necessidade de migration:
- Domain: `Receita.MarcarComoDeletado(Guid usuarioId)` (`Receita.cs:299`) — seta `DeletadoEm` + `DeletadoPorUsuarioId`, idempotente (lança se já deletada).
- Colunas `deletado_em` / `deletado_por_usuario_id` já existem na tabela `receitas`.
- Read-side `ReceitaQueryRepository` já filtra `deletado_em IS NULL` **e** `estabelecimento_id` em todas as queries → o rascunho soft-deletado some da lista automaticamente, multi-tenant-safe.

A **lacuna** é só de exposição: não há endpoint nem método de service `descartar`. Esta entrega fecha a lacuna **e** adiciona a trava contra acúmulo (decisão do usuário de incluir nesta mesma entrega).

## 2. Persona-alvo

**Profissional de saúde** (médico, dentista etc.), durante o atendimento ou logo após, na aba Receitas do prontuário do paciente. Frequência: sempre que abre uma receita e desiste de emitir, ou quando começou um rascunho por engano. Secundariamente, a persona se beneficia ao não reencontrar rascunhos antigos acumulados na próxima visita do paciente.

## 3. Escopo

**Inclui**:
- Novo endpoint para descartar rascunho via soft-delete (`MarcarComoDeletado`), distinto do endpoint de cancelamento clínico.
- Novo método no `receitaService.ts` (front) e ação na aba de receitas (`ReceitasPacienteTab.vue`): botão **"Descartar rascunho"** com confirmação simples, **sem pedir motivo**, exibido apenas para `Rascunho`.
- Rascunho descartado some da lista (reuso do filtro `deletado_em IS NULL` já existente).
- Audit operacional do descarte: registro de `deletado_por_usuario_id` + `deletado_em` (já coberto pelo Domain).
- **Trava contra acúmulo**: bloquear a criação de um novo rascunho quando já existe um rascunho não-deletado do mesmo paciente no mesmo estabelecimento (critério definido na seção 4).

**Não inclui**:
- Qualquer alteração no fluxo clínico de cancelamento de receita **Emitida** (`Receita.Cancelar`) — permanece intacto, com motivo obrigatório.
- Exclusão física (hard-delete) de qualquer receita.
- Lixeira / restauração de rascunhos descartados (descarte é terminal para a UX; o soft-delete preserva o dado só para audit).
- Migration de schema (colunas e filtros já existem).
- Mudança no comportamento de `Substituida`.

## 4. Regras de negócio

- **R1 — Descarte só para Rascunho**: o descarte via soft-delete só é permitido quando `Status == Rascunho`. Tentar descartar `Emitida`/`Cancelada`/`Substituida` por esse caminho → `BusinessException` genérica `"Apenas rascunhos podem ser descartados."`. Mora em: **Handler** (novo `DescartarRascunhoReceitaCommandHandler`), apoiado no Domain `MarcarComoDeletado` (que já é idempotente). Validada em: **back** (fonte da verdade) + **front** (botão "Descartar rascunho" só aparece para `Rascunho`).

- **R2 — Descarte usa soft-delete, nunca o caminho clínico**: o botão "Descartar rascunho" chama o **novo** endpoint que executa `MarcarComoDeletado(usuarioId)`. Não toca em `Receita.Cancelar`. Mora em: **Handler + Front**. Validada em: **back + front**.

- **R3 — Cancelamento clínico de Emitida permanece imutável**: `Receita.Cancelar(motivo)` continua exigindo `Status == Emitida` e `motivo` obrigatório (1–500 chars). Nenhuma linha de `Cancelar` é alterada. Mora em: **Domain** (já existe). Validada em: **back** (regressão).

- **R4 — Audit operacional do descarte**: ao descartar, o registro grava `deletado_por_usuario_id` (usuário autenticado da request) e `deletado_em` (UTC). Não é audit clínico de prontuário (rascunho nunca existiu clinicamente) — é audit operacional de quem apagou o quê e quando. Mora em: **Domain** (`MarcarComoDeletado` já faz). Validada em: **back**.

- **R5 — Trava contra acúmulo (critério escolhido: "rascunho aberto = existe Rascunho não-deletado do mesmo paciente no mesmo estabelecimento")**: ao iniciar um novo rascunho, se já existir uma receita com `Status == Rascunho` **e** `deletado_em IS NULL` **e** mesmo `paciente_id` **e** mesmo `estabelecimento_id`, a criação é **bloqueada** com `BusinessException` genérica `"Já existe um rascunho de receita em andamento para este paciente. Continue ou descarte o rascunho atual antes de iniciar outro."`. Mora em: **Handler** (`IniciarRascunhoReceitaCommandHandler`), via novo método no `IReceitaRepository` (ex.: `ExisteRascunhoAbertoDoPaciente(pacienteId, estabelecimentoId)`). Validada em: **back** (fonte da verdade) + **front** (UX: ao tentar "Nova receita", se a trava barrar, o front exibe a mensagem e oferece abrir o rascunho existente em vez de erro cru).

  **Por que este critério (e não "rascunho vazio")**: "vazio" exigiria inspecionar conteúdo (itens? observação? validade?) — ambíguo (rascunho só-com-observação é vazio?), frágil e caro. "Aberto = existe um Rascunho não-deletado" é uma checagem única, barata e indexável sobre `status` + `paciente_id` + `estabelecimento_id` + `deletado_em IS NULL`, sem introspecção de conteúdo. É o critério mais simples e defensável: garante no máximo **um rascunho ativo por paciente por estabelecimento**, que é exatamente o que evita o acúmulo. O profissional sempre tem a saída de **descartar** (R1) ou **continuar** o rascunho existente.

- **R6 — Escopo da trava é por estabelecimento**: a unicidade do rascunho aberto é **por paciente por estabelecimento**, nunca global. O mesmo paciente pode (teoricamente, em multi-vínculo) ter rascunhos em estabelecimentos distintos sem que um bloqueie o outro. Mora em: **Handler/Repository** (filtro `estabelecimento_id`). Validada em: **back**.

- **R7 — Permissão**: descartar rascunho e iniciar rascunho seguem o mesmo permissionamento já aplicado a iniciar/editar receita (profissional autor / papéis com acesso clínico ao prontuário). Não se cria papel novo. Mora em: **Handler/autorização existente**. Validada em: **back + front** (botão oculto sem permissão).

## 5. Modelo de dados

**Sem migration.** Reuso integral:
- Tabela `receitas`: colunas `status`, `deletado_em`, `deletado_por_usuario_id`, `paciente_id`, `estabelecimento_id` já existem.
- Filtro `deletado_em IS NULL` + `estabelecimento_id` já aplicado em todas as queries de leitura (`ReceitaQueryRepository`).
- **Performance da trava (R5)**: a checagem `ExisteRascunhoAbertoDoPaciente` filtra por `paciente_id` + `estabelecimento_id` + `status = Rascunho` + `deletado_em IS NULL`. O `imedto-database` deve **confirmar** se há índice que cubra esse predicado (idealmente composto em `(estabelecimento_id, paciente_id, status)` com `deletado_em` como predicado parcial ou coluna incluída). Se não houver, é o único item que pode demandar o DB agent — avaliar custo/benefício; volume de receitas por paciente é baixo, então um índice já existente em `(estabelecimento_id, paciente_id)` provavelmente basta.
- PII: nenhuma nova coluna de PII. `deletado_por_usuario_id` é identificador de usuário, não dado de paciente.
- Retenção: rascunho descartado permanece no banco (soft-delete) apenas para audit operacional; não aparece em nenhuma tela.

## 6. UX e fluxo

**Aba Receitas do prontuário (`ReceitasPacienteTab.vue`)**

Rodapé de um rascunho aberto:
- Substituir / ajustar o botão atual que hoje chama "Cancelar receita" no contexto de rascunho por **"Descartar rascunho"** (variante destrutiva secundária do design system).
- Ao clicar → **confirmação simples** (reuso do componente de diálogo de confirmação já existente, ex.: `AppConfirmDialog`): título "Descartar rascunho?", corpo "O rascunho será removido e não aparecerá mais na lista. Esta ação não pode ser desfeita.", ações "Cancelar" / "Descartar". **Sem campo de motivo.**
- Confirmado → chama `receitaService.descartarRascunho(id)` → ao sucesso, fecha o rascunho e remove da lista (refetch ou remoção otimista; preferir refetch para refletir o filtro do back).

Receita **Emitida**: o rodapé continua exibindo "Cancelar receita" com o fluxo clínico atual (motivo obrigatório). Nada muda.

**Trava ao iniciar nova receita (R5)**:
- Ao acionar "Nova receita" para um paciente que já tem rascunho aberto, o back devolve 422 com a mensagem de R5.
- O front trata esse 422 de forma amigável: em vez de toast de erro cru, exibe um diálogo "Já existe um rascunho em andamento" com a ação primária **"Abrir rascunho"** (navega ao rascunho existente) e a secundária **"Descartar e criar novo"** (descarta o atual e inicia outro) — ou, no mínimo viável, exibe a mensagem e direciona ao rascunho existente. O dev tem liberdade de implementar a versão mais simples que não deixe o usuário sem saída.

**Estados**:
- *Loading*: botão "Descartar" em estado pendente (spinner) durante a request; bloqueia duplo-clique.
- *Erro*: se o back recusar (ex.: rascunho já descartado por outra aba → R1/idempotência), toast genérico "Não foi possível descartar o rascunho." e refetch da lista.
- *Vazio*: após descartar o único rascunho, a lista de receitas pode ficar vazia → `AppEmptyState` com texto "Nenhuma receita para este paciente.".
- *Sucesso*: rascunho some da lista; toast discreto "Rascunho descartado.".

Mobile-ready: confirmação e botões respeitam o layout responsivo já usado na aba.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — descarte)**: Dado um profissional autenticado com um rascunho de receita aberto para o paciente, Quando clica em "Descartar rascunho" e confirma, Então o back executa `MarcarComoDeletado`, a receita recebe `deletado_em` e `deletado_por_usuario_id`, o endpoint retorna sucesso e o rascunho **desaparece da lista** após o refetch.

- **CA2 (descarte sem motivo)**: Dado o diálogo de confirmação de descarte, Quando ele é exibido, Então **não há campo de motivo** e a request de descarte **não** envia nem exige `motivo`.

- **CA3 (descarte só para Rascunho)**: Dado uma receita `Emitida`, Quando o front é renderizado, Então o botão "Descartar rascunho" **não aparece**; e Dado uma chamada direta ao endpoint de descarte para uma receita não-`Rascunho`, Quando processada, Então o back retorna 422 com mensagem genérica "Apenas rascunhos podem ser descartados." e nada é alterado.

- **CA4 (regressão — cancelamento clínico de Emitida intacto)**: Dado uma receita `Emitida`, Quando o profissional usa "Cancelar receita" informando um motivo válido, Então a receita vira `Cancelada` com `MotivoCancelamento` e `CanceladaEm` preenchidos, **exatamente como antes desta entrega**; e Quando tenta cancelar sem motivo, Então recebe "Motivo do cancelamento é obrigatório.". Nenhuma linha de `Receita.Cancelar` foi alterada.

- **CA5 (regressão — descarte NÃO usa o caminho clínico)**: Dado um rascunho, Quando é descartado, Então o `Status` **não** muda para `Cancelada`, `MotivoCancelamento` permanece nulo, e o registro fica apenas com `deletado_em`/`deletado_por_usuario_id` setados (soft-delete). O caminho de `Cancelar` não é invocado.

- **CA6 (trava contra acúmulo)**: Dado um paciente que já possui um rascunho de receita aberto (`Status == Rascunho`, `deletado_em IS NULL`) no estabelecimento atual, Quando o profissional tenta iniciar uma nova receita para esse paciente, Então o back recusa com 422 e mensagem genérica "Já existe um rascunho de receita em andamento para este paciente. Continue ou descarte o rascunho atual antes de iniciar outro.", e **nenhum** novo rascunho é criado.

- **CA7 (trava libera após descarte)**: Dado um paciente cujo único rascunho aberto foi descartado (CA1), Quando o profissional inicia uma nova receita, Então a criação é permitida e um novo rascunho é gerado (a trava de R5 não barra, pois o rascunho anterior tem `deletado_em` preenchido).

- **CA8 (trava é por estabelecimento — multi-tenant)**: Dado um paciente com rascunho aberto no estabelecimento A, Quando um profissional do estabelecimento B (com vínculo legítimo ao mesmo paciente) inicia uma receita no estabelecimento B, Então a criação é permitida — a trava de R5 só considera rascunhos do **mesmo** `estabelecimento_id`.

- **CA9 (multi-tenant — descarte cross-tenant)**: Dado um usuário autenticado no estabelecimento B, Quando tenta descartar um rascunho cujo `estabelecimento_id` é do A, Então o repositório (filtro de tenant) não encontra o registro e o back retorna mensagem **genérica** "Receita não encontrada." (sem revelar existência), nada é alterado e nada é logado com PII.

- **CA10 (LGPD / audit operacional)**: Dado o descarte de um rascunho, Quando ocorre, Então o registro grava `deletado_por_usuario_id` = id do usuário autenticado e `deletado_em` em UTC; e nenhuma mensagem de erro/sucesso ou log expõe PII do paciente.

- **CA11 (idempotência do descarte)**: Dado um rascunho já descartado (concorrência: duas abas), Quando uma segunda tentativa de descarte chega, Então o back responde de forma controlada (422 "Receita já está deletada." ou retorno genérico de não-encontrado, conforme o filtro de leitura), sem 500 e sem efeito colateral.

- **CA12 (estados de UI)**: Dado a lista de receitas após descartar o único rascunho, Quando recarrega, Então exibe `AppEmptyState` com "Nenhuma receita para este paciente."; e durante a request de descarte o botão fica em estado pendente, bloqueando duplo-clique.

- **CA13 (RBAC)**: Dado um usuário sem permissão de escrita clínica no prontuário, Quando a aba de receitas é renderizada, Então os botões "Descartar rascunho" e "Nova receita" ficam ocultos; e Dado uma chamada direta ao endpoint de descarte por esse usuário, Quando processada, Então o back retorna 403.

## 8. Riscos e dependências

- **Regressão no rodapé do rascunho**: o front hoje reusa o botão "Cancelar receita" para rascunho. A troca para "Descartar rascunho" deve ser **cirúrgica**, sem afetar o rodapé da `Emitida`. Vigiar `ReceitasPacienteTab.vue` (e `ReceitasTab.vue`, se compartilharem rodapé).
- **Trava x fluxo de "duplicar receita"**: confirmar que `DuplicarReceitaCommand`/`IniciarRascunho` não fiquem bloqueados de forma indesejada — duplicar provavelmente também cria um rascunho e deve respeitar a mesma trava (consistente: não acumular). O dev deve aplicar R5 no mesmo ponto de criação de rascunho, cobrindo iniciar e duplicar.
- **Índice da trava**: ver seção 5 — possível (improvável) acionamento do `imedto-database` só para confirmar/criar índice. Volume baixo torna isso provavelmente desnecessário.
- **Dependência de componente**: confirmar existência de `AppConfirmDialog` (ou equivalente) no design system antes de criar diálogo novo. Reuso obrigatório.

## 9. Observações para execução

**Não-negociável**:
- `Receita.Cancelar` **não pode ser tocado** (R3/CA4).
- Descarte usa exclusivamente `MarcarComoDeletado` (R2/CA5).
- Trava R5 mora no **backend** (Handler), com espelho de UX no front — nunca só no front.
- Multi-tenant em toda operação nova (descarte e checagem da trava filtram `estabelecimento_id`).
- Mensagens de erro genéricas, sem PII (CA9/CA10).

**Liberdade técnica do dev**:
- Nome exato do endpoint/command/método de service (sugestões: `DescartarRascunhoReceitaCommand` + `POST .../receitas/{id}/descartar`; `receitaService.descartarRascunho(id)`).
- Nome exato do método no `IReceitaRepository` para a trava (sugestão: `ExisteRascunhoAbertoDoPaciente(pacienteId, estabelecimentoId)`).
- Versão mínima viável vs. completa do tratamento de 422 da trava no front (desde que o usuário nunca fique sem saída — sempre poder abrir ou descartar o rascunho existente).

**Reuso obrigatório**:
- Domain `MarcarComoDeletado` (existe), filtro de leitura `deletado_em IS NULL`/`estabelecimento_id` (existe), diálogo de confirmação do design system (existe), `AppEmptyState`.
- Adicionar a checagem da trava dentro de `IniciarRascunhoReceitaCommandHandler` (e no caminho de duplicar), não criar handler paralelo.

**Aciona `imedto-database`?** Em princípio **não** (sem migration). Apenas se a confirmação de índice da seção 5 indicar gargalo — o que é improvável dado o volume.

## 10. Atualização de documentação

**Nenhum doc de `Docs/` precisa ser atualizado.** A demanda segue padrões já documentados:
- Soft-delete, multi-tenant e audit operacional já estão descritos em `Docs/LGPD.md` e `Docs/ARQUITETURA.md` — esta entrega aplica os padrões existentes, sem introduzir padrão novo.
- Sem novo componente de design system (reuso de `AppConfirmDialog`/`AppEmptyState`), sem mudança de infra, sem migration.
- Caso o `imedto-developer` introduza um componente de diálogo destrutivo **novo** (não existente hoje), aí sim adicionar a `Docs/DESIGN.md` — mas a expectativa é reuso, não criação.
