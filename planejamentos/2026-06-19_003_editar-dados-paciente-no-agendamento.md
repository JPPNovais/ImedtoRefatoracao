# Editar dados do paciente direto no agendamento

**ID**: 2026-06-19_003
**Status**: Aprovado por usuário em 2026-06-19 — IMUTÁVEL
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: permissionamento (RBAC do PUT paciente), prontuário/paciente (audit LGPD), agenda (modais de criar/editar agendamento)

## 1. Contexto e motivação

Hoje, ao montar ou editar um agendamento, se a recepção/profissional percebe que um dado essencial do paciente está errado ou desatualizado (telefone trocado, CPF digitado errado, e-mail antigo, opt-in de WhatsApp), a única forma de corrigir é **abandonar o fluxo de agendamento, navegar até a página do paciente, editar, e refazer o agendamento**. Esse desvio quebra o fluxo operacional e é uma fricção real no balcão — o momento em que o paciente está na frente do atendente é justamente quando os dados são confirmados.

O `CheckInModal` já resolveu isso para o momento do check-in: tem um botão "Editar dados" que abre o `PacienteFormModal`. A demanda é **estender o mesmo padrão** para os modais de **criar** (`NovoAgendamentoModal`) e **editar** (`EditarAgendamentoModal`) agendamento, sem reinventar formulário nem permissão.

Evidência: dor relatada diretamente pelo dono do produto ("hoje não é possível a não ser entrando na página do paciente").

## 2. Persona-alvo

- **Recepção/Profissional/Dono** no balcão ou no agendamento online interno, no momento de **criar ou reagendar** uma consulta, com o paciente presente ou ao telefone.
- Frequência: alta — toda vez que um dado precisa ser corrigido durante o agendamento (cenário cotidiano de clínica).
- Observação de RBAC: a **edição** de dados do paciente continua restrita a **Profissional ou Dono** (regra atual do `PUT /api/paciente/{id}`). Recepção pura agenda mas **não** edita — então o atalho **não aparece** para ela.

## 3. Escopo

**Inclui**:
- Botão **"Editar dados"** no card/badge do paciente já selecionado (Step 2) do `NovoAgendamentoModal.vue`.
- Mesmo botão no `EditarAgendamentoModal.vue` (onde o paciente do agendamento é exibido).
- Reuso do `PacienteFormModal` em **modo editar** (cadastro completo: nome, CPF, telefone, data de nascimento, sexo, e-mail, telefone fixo, endereço/CEP, observações, tags, alertas, opt-in WhatsApp) — **sem documentos/anexos**, que já ficam fora desse formulário.
- Reflexo dos dados atualizados (nome/CPF/telefone) no card do paciente **dentro do modal de agendamento**, sem fechar nem reiniciar o fluxo de agendamento em andamento.
- Visibilidade do botão condicionada ao papel (Profissional/Dono vê; Recepção não vê) — espelho do RBAC do backend.

**Não inclui**:
- Edição de **documentos/anexos** do paciente (continua exclusiva da `PacienteDetalheView` aba Documentos e do fluxo de troca no check-in).
- Qualquer alteração no `CheckInModal` (já tem o atalho — não mexer).
- Qualquer alteração no backend de permissão do `PUT /api/paciente/{id}` (mantém `[RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]`).
- Novo endpoint, novo DTO, nova query ou nova variante de formulário de paciente.
- Cadastro de paciente novo pelo atalho (o atalho é só para editar um paciente **já selecionado**).

## 4. Regras de negócio

- **R1 — Atalho condicionado a paciente selecionado**: o botão "Editar dados" só existe quando há um paciente **já selecionado** no Step 2 do `NovoAgendamentoModal` (ou no paciente do agendamento em `EditarAgendamentoModal`). No cadastro inline de paciente novo e antes da seleção, **não aparece**. Mora em: Front (renderização condicional). Validada em: front (UX) — não tem espelho de back porque é só exibição.

- **R2 — RBAC da edição (sem mudança no back)**: editar dados do paciente exige papel **Profissional ou Dono**. O backend já garante via `[RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]` no `PUT /api/paciente/{id}` (retorna 403 para recepção). No front, o botão "Editar dados" fica **oculto** para quem não tem o papel. Mora em: Handler/endpoint (back, fonte da verdade) + Front (ocultar botão). Validada em: **back + front**.

- **R3 — Reuso integral do comando de atualização**: a edição pelo atalho usa exatamente o mesmo caminho da edição na página do paciente — `pacienteService.atualizar(id, payload)` → `PUT /api/paciente/{id}` → `AtualizarPacienteCommand`/`AtualizarPacienteCommandHandler`. Isso garante, sem código novo: multi-tenant (filtro `EstabelecimentoId`), audit (`TipoAcessoPaciente.Edicao`), validação de duplicidade de CPF/documento. Mora em: Handler (back). Validada em: **back** (já existe e coberto) — front apenas consome.

- **R4 — Minimização (LGPD)**: o atalho edita apenas os campos essenciais já presentes no `PacienteFormModal` modo editar. Documentos/anexos (dado de saúde sensível) ficam **fora** — só na página do paciente / check-in. Mora em: Front (escopo do formulário reusado) + premissa de produto. Validada em: front.

- **R5 — Atualização do card sem perder o fluxo**: ao salvar a edição com sucesso, o modal de agendamento atualiza o card do paciente (nome/CPF/telefone) com os dados novos, **sem fechar o modal de agendamento nem perder o que já foi preenchido** (data/hora, profissional, procedimento, etc.). Espelha o padrão já existente: o `CheckInModal`/`AgendaView` observa `pacienteAtualizado` via watch. Mora em: Front (orquestração do estado pai). Validada em: front.

- **R6 — Não aninhar modais (composição via pai)**: a abertura do `PacienteFormModal` é orquestrada pelo componente pai (padrão já usado pelo `AgendaView` para o check-in), evitando aninhar `AppModal` dentro de `AppModal`. O modal de agendamento e o `PacienteFormModal` não coexistem empilhados de forma quebrada. Mora em: Front (orquestração). Validada em: front (visual + comportamento de foco/scroll).

## 5. Modelo de dados

**Nenhuma mudança de schema.** Reuso integral de:
- Tabela `pacientes` (já existente) — escrita via `AtualizarPacienteCommandHandler`.
- Audit de acesso ao paciente (já existente) — registro `TipoAcessoPaciente.Edicao` gravado pelo handler.
- Filtro multi-tenant por `EstabelecimentoId` (já existente no comando/handler).

Não há coluna nova, índice novo, tabela nova nem migration. **Não aciona o `imedto-database`.**

## 6. UX e fluxo

**Step 2 do `NovoAgendamentoModal` (paciente já selecionado)** e **bloco do paciente no `EditarAgendamentoModal`**:

```
┌─────────────────────────────────────────────┐
│  Paciente                                     │
│  ┌─────────────────────────────────────────┐ │
│  │ 🧑 Maria Silva                            │ │
│  │ CPF 123.456.789-09 · (11) 99999-0000     │ │
│  │                          [ Editar dados ]│ │  ← novo botão (só Profissional/Dono)
│  └─────────────────────────────────────────┘ │
│  ...resto do passo (data/hora, proc, etc.)    │
└─────────────────────────────────────────────┘
```

Fluxo:
1. Usuário (Profissional/Dono) com paciente selecionado clica **"Editar dados"**.
2. O pai (`AgendaView` ou orquestrador equivalente) abre o `PacienteFormModal` em **modo editar**, pré-carregado com os dados do paciente.
3. Usuário edita campos essenciais e salva → `PUT /api/paciente/{id}`.
4. Em sucesso, `PacienteFormModal` fecha, o card do paciente no modal de agendamento reflete os novos dados, e o fluxo de agendamento continua de onde estava (nada do que foi preenchido se perde).

- **Componentes reutilizados**: `PacienteFormModal`, `AppModal`, `AppButton`, `AppField` (e demais do DS já usados pelo `PacienteFormModal`). Botão "Editar dados" segue o mesmo estilo visual do botão homônimo do `CheckInModal`.
- **Estados**:
  - **Loading**: durante o submit do `PacienteFormModal`, botão de salvar em estado de carregamento (comportamento já existente do formulário).
  - **Erro de validação (422)**: duplicidade de CPF/documento e validações de campo exibidas no `PacienteFormModal` com mensagem genérica, sem PII em log — comportamento já existente do formulário.
  - **Erro de permissão (403)**: não deve ocorrer pela UI (botão oculto para recepção); se ocorrer via chamada direta, o front trata como erro genérico sem vazar tenant/PII.
  - **Sucesso**: card atualizado + fluxo de agendamento preservado.
  - **Vazio/sem paciente**: botão não renderizado (R1).
- **Tipografia**: usar exclusivamente tokens (`var(--text-*)`, `var(--font-weight-*)`) conforme CLAUDE.md §5; nada de literais.
- **Mobile-ready**: o `PacienteFormModal` já é responsivo; o botão "Editar dados" deve caber no card em telas estreitas (mesmo tratamento do `CheckInModal`).

## 7. Critérios de aceite (testáveis)

- **CA1** (caminho feliz — criar): Dado um usuário Profissional ou Dono no `NovoAgendamentoModal` com um paciente já selecionado no Step 2, Quando clica em "Editar dados", altera o telefone e salva, Então o `PUT /api/paciente/{id}` é chamado com sucesso, o `PacienteFormModal` fecha, e o card do paciente no modal de agendamento passa a exibir o telefone novo.

- **CA2** (caminho feliz — editar): Dado um usuário Profissional ou Dono no `EditarAgendamentoModal` de um agendamento existente, Quando clica em "Editar dados" do paciente, altera o nome e salva, Então o dado é persistido via `PUT /api/paciente/{id}` e o bloco do paciente no modal de editar agendamento reflete o nome atualizado.

- **CA3** (fluxo preservado): Dado que o usuário já preencheu data/hora, profissional e procedimento no `NovoAgendamentoModal`, Quando edita os dados do paciente pelo atalho e salva, Então ao retornar ao modal de agendamento **todos os campos já preenchidos permanecem intactos** (o agendamento em andamento não é reiniciado nem fechado).

- **CA4** (atalho oculto sem paciente): Dado o `NovoAgendamentoModal` antes de selecionar um paciente (ou na etapa de cadastro inline de paciente novo), Quando o usuário olha o modal, Então o botão "Editar dados" **não é renderizado**.

- **CA5** (RBAC — vê e edita): Dado um usuário com papel Profissional ou Dono, Quando abre o modal de agendamento com paciente selecionado, Então o botão "Editar dados" está visível e a edição via `PUT /api/paciente/{id}` retorna sucesso.

- **CA6** (RBAC — recepção não vê): Dado um usuário com papel apenas Recepção (sem permissão de editar paciente), Quando abre o modal de agendamento com paciente selecionado, Então o botão "Editar dados" **não aparece**; e Dado uma chamada direta ao `PUT /api/paciente/{id}` por esse usuário, Então o backend retorna **403** (regra atual mantida, sem alteração no back).

- **CA7** (multi-tenant): Dado um usuário do estabelecimento B, Quando tenta editar (via atalho ou chamada direta) um paciente pertencente ao estabelecimento A, Então o backend retorna **404 genérico** ("não encontrado"), nada é persistido e nenhum log contém PII do paciente alheio.

- **CA8** (LGPD/audit): Dado que um Profissional/Dono edita um paciente pelo atalho do agendamento, Quando o `AtualizarPacienteCommandHandler` processa a alteração, Então é gravado o mesmo registro de audit de acesso `TipoAcessoPaciente.Edicao` que a edição feita pela página do paciente (com `{usuario_id, paciente_id, estabelecimento_id, timestamp}`), sem PII em mensagem ou log.

- **CA9** (minimização — sem documentos): Dado o `PacienteFormModal` aberto pelo atalho do agendamento, Quando o usuário inspeciona os campos disponíveis, Então **não há** edição/upload de documentos/anexos do paciente (estes permanecem exclusivos da página do paciente e do check-in).

- **CA10** (erro de validação — CPF duplicado): Dado que o usuário altera o CPF do paciente para um CPF já usado por outro paciente do mesmo estabelecimento, Quando salva, Então o backend retorna **422** com mensagem genérica de duplicidade exibida no `PacienteFormModal`, nada é persistido, e o fluxo de agendamento continua disponível ao fechar/corrigir.

- **CA11** (estado de loading): Dado o submit da edição em andamento, Quando o usuário aguarda a resposta, Então o botão de salvar do `PacienteFormModal` fica em estado de carregamento e não permite duplo envio.

- **CA12** (performance/reuso de dados): Dado que o paciente já está carregado no modal de agendamento, Quando o usuário abre "Editar dados", Então o `PacienteFormModal` é populado a partir dos dados já disponíveis (sem nova busca redundante além do necessário para o cadastro completo) e nenhuma consulta é disparada por abas/seções não acessadas.

- **CA13** (composição — sem modal aninhado quebrado): Dado o atalho aberto, Quando o `PacienteFormModal` está em primeiro plano, Então ele é orquestrado pelo componente pai (padrão do check-in) sem empilhar `AppModal` dentro de `AppModal` de forma quebrada (foco e scroll corretos), e ao fechar retorna corretamente ao modal de agendamento.

## 8. Riscos e dependências

- **Estado do working tree**: `NovoAgendamentoModal.vue` tem alteração não-commitada (remoção dos toggles de lembrete). O dev deve **partir do estado atual do arquivo**, não de uma versão anterior.
- **Orquestração pai (risco principal)**: confirmar se o `AgendaView` (ou o pai que abre `NovoAgendamentoModal`/`EditarAgendamentoModal`) é o mesmo que orquestra o `PacienteFormModal` do check-in. Reaproveitar esse mecanismo evita modal aninhado e regressão de foco/scroll. Se a orquestração de pai diferir entre check-in e os modais de agendamento, replicar o padrão fielmente.
- **Sincronização do card**: garantir que o evento/`watch` de `pacienteAtualizado` realmente repropague para o card dentro dos modais de agendamento (não só para a lista da agenda).
- **Regressão de RBAC**: a regra de visibilidade do botão deve usar a mesma fonte de papel já consumida no front; não introduzir checagem de papel divergente do back (Profissional/Dono).
- Sem dependência de schema, de novo endpoint ou de novo provider.

## 9. Observações para execução

- **Não-negociável**:
  - Reusar `PacienteFormModal` (modo editar) e `pacienteService.atualizar` → `PUT /api/paciente/{id}`. **Proibido** criar 2ª variante de formulário, novo endpoint, novo DTO ou nova query.
  - Não tocar no backend de permissão (`[RequiresPapel(Profissional, Dono)]` permanece).
  - Não tocar no `CheckInModal`.
  - Não incluir documentos/anexos no escopo do atalho.
  - Tipografia só via tokens (CLAUDE.md §5).
  - Multi-tenant, audit `TipoAcessoPaciente.Edicao` e validações vêm "de graça" pelo reuso do handler — o dev não deve reimplementar nada disso, apenas garantir que o caminho passa pelo comando existente.
- **Liberdade técnica**: a forma exata de propagar o `pacienteAtualizado` de volta ao card (emit/watch/store) fica a critério do dev, desde que espelhe o padrão já validado do check-in e preserve o fluxo de agendamento (CA3/CA5).
- **Reuso (premissa CLAUDE.md)**: antes de escrever qualquer coisa nova, conferir como o `CheckInModal` emite `editar-paciente` e como o `AgendaView` reage — espelhar.
- Como não há mudança de schema, **não acionar `imedto-database`**.

## 10. Atualização de documentação

**Nenhum doc de `Docs/` será atualizado.** Avaliação: esta entrega é **composição de componentes existentes** (`PacienteFormModal` + botão de atalho) reaproveitando endpoint, comando, audit e multi-tenant já documentados. Não introduz: bounded context novo, padrão de DI/store/service novo, componente novo no design system, recurso de infra, comando recorrente novo, nem novo tipo de PII/endpoint que exponha dado pessoal (o acesso a PII já está coberto pelo `PUT /api/paciente/{id}` e seu audit). Portanto não há mudança cross-cutting de arquitetura/design/infra/LGPD a documentar. Caso o dev, durante a execução, perceba que a orquestração de modais via pai (anti-aninhamento) merece virar padrão documentado no design system, deve sinalizar — mas isso é fora do escopo deste briefing.
