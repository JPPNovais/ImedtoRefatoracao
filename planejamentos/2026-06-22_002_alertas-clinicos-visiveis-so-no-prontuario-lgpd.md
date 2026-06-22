# Alertas clínicos visíveis apenas no prontuário, no contexto do atendimento (LGPD)

**ID**: 2026-06-22_002
**Status**: Aprovado por usuário em 2026-06-22 — IMUTÁVEL
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: permissionamento (novo gating de leitura/escrita de alertas), prontuário/paciente (audit LGPD), agenda (remoção do resumo de alertas no check-in), cadastro de paciente (remoção da seção de alertas do form geral), lista/detalhe de paciente. **Não toca**: orçamento, financeiro, estoque, relatórios.

---

## 1. Contexto e motivação

Hoje os **alertas clínicos** do paciente (campo livre `pacientes.alertas text[]` — ex.: "alergia a penicilina", "anticoagulado", comorbidades) aparecem em **quatro** lugares do produto, e a **gestão** (criar/editar/remover) está no formulário geral do paciente que a **recepção** usa:

- Badge "⚠ X alerta(s)" na **lista de pacientes** — visível para qualquer um que acessa a lista.
- Bloco "Alertas clínicos" nas **informações básicas** do detalhe do paciente.
- Resumo de alertas no **check-in** da agenda — operado pela recepção.
- Cabeçalho do **prontuário**, no momento do atendimento.

Isso fere a LGPD. O conteúdo de um alerta clínico é **dado pessoal sensível de saúde** (Art. 5º II e Art. 11 LGPD) — equiparável ao CPF na fala do dono do produto: "não pode aparecer para qualquer um". Quem faz um agendamento (recepção) não precisa — e não deve — ver que o paciente tem alertas, muito menos o teor deles. A informação de saúde só é necessária para quem está conduzindo (ou conduziu) o ato clínico.

Evidência direta do dono do produto:
1. "remover a parte de alertas... isso fere a LGPD nessa parte de agendas... somente os médicos na hora do atendimento que poderia ver isso."
2. "na parte de pacientes, somente quando for atender de fato ou o médico q tiver permissão é que pode ver os conteúdos dos alertas (dado igual ao cpf que não pode aparecer para qualquer um)."
3. "somente o dono do estabelecimento, ou quem atendeu (ou está atendendo) o paciente é que pode ver essas informações de alertas."
4. "essas informações devem aparecer apenas no cabeçalho do prontuário no momento do atendimento, e não nas informações básicas do paciente."

Esta demanda transforma alertas clínicos em **dado sensível de visibilidade restrita**: removidos de todo lugar exceto o cabeçalho do prontuário, com a leitura **gated no backend** por papel/vínculo de atendimento, e a gestão movida para dentro do prontuário.

## 2. Persona-alvo

- **Profissional que atende** (médico/dentista/etc.) — no momento do atendimento, no cabeçalho do prontuário, **precisa** ver alertas (segurança clínica: alergia, anticoagulação). É também quem cria/edita os alertas a partir do que apura na consulta. Frequência: a cada atendimento.
- **Dono do estabelecimento** — vê e gere alertas de qualquer paciente do seu estabelecimento, **sempre** (responsável clínico/administrativo). Frequência: conforme necessidade.
- **Recepção** — agenda, faz check-in e cadastra/edita dados administrativos do paciente, mas **deixa de ver e de gerir** alertas clínicos. É a persona da qual o dado precisa ser ocultado. Frequência: alta (operação de balcão) — daí a urgência do vazamento.
- **Profissional sem vínculo de atendimento** com aquele paciente (nunca atendeu, não está atendendo) e que **não é Dono** — **não** vê os alertas, mesmo abrindo o prontuário.

## 3. Escopo

**Inclui**:
1. **Remover** o badge "X alerta(s)" da **lista de pacientes** (`PacientesView.vue`) e a contagem `QtdAlertas` do DTO/SQL de listagem (`PacienteListaItemDto`).
2. **Remover** o bloco "Alertas clínicos" das **informações básicas** do detalhe do paciente (`PacienteDetalheView.vue`) e tirar o array `Alertas[]` do payload do detalhe administrativo (`GET /api/paciente/{id}` quando consumido como "dados básicos").
3. **Remover** o resumo de alertas do **check-in** da agenda (`CheckInModal.vue`).
4. **Remover** a seção "Alertas clínicos" do **formulário geral do paciente** (`PacienteFormModal.vue`) — a recepção não cria/edita mais alertas.
5. **Manter** os alertas **apenas** no cabeçalho do prontuário (`ProntuarioPacienteHeader.vue`), com o conteúdo entregue **somente** a quem tem direito de leitura (gating no backend).
6. **Mover a gestão** (criar/editar/remover alertas) para **dentro do prontuário**, restrita a **Dono + Profissional que atende/atendeu** (espelho back+front: endpoint/comando próprio gated, e UI de edição no cabeçalho do prontuário visível só para quem pode).
7. **Enforcement no backend**: a API **não devolve** o conteúdo dos alertas (nem qualquer contagem) para quem não tem direito — independentemente da UI. Trava na fonte.
8. Atualizar `Docs/LGPD.md` com a regra de visibilidade restrita de alertas clínicos.

**Não inclui** (fora de escopo, registrar como não-objetivo):
- Mudança no **modelo de dados de alertas** (continua `pacientes.alertas text[]`) — sem nova tabela/estrutura de alerta, salvo o que o `imedto-database` julgar necessário para a query de "atendeu/atendendo" (ver §5).
- **Migration de schema** — **a confirmar pelo `imedto-database`** (provavelmente nenhuma; a coluna já existe e "atendeu/atendendo" é query de leitura). Não dar como certa.
- **Redesenho da UI do cabeçalho do prontuário** além do necessário para (a) gating do conteúdo e (b) incluir o controle de gestão (criar/editar/remover) restrito.
- Trazer alertas para qualquer outro lugar novo (hover de lista, tooltip de agenda, etc.) — **explicitamente proibido** (já registrado como decisão LGPD anterior: contagem/conteúdo de alerta não vai para a lista).
- Histórico/auditoria de **mudanças** de alerta como entidade versionada — fora do MVP. O audit de **acesso** ao prontuário (leitura/escrita) já cobre o necessário.
- Notificação ao paciente ou a terceiros sobre alertas.

## 4. Regras de negócio

- **R1 — Alertas clínicos são dado sensível de visibilidade restrita.** O conteúdo dos alertas (e qualquer derivado, como a contagem) só pode ser exposto no **contexto do prontuário, no atendimento**, e somente a quem tem direito (R2). Em nenhum outro contexto (lista, informações básicas, check-in, busca) a API devolve alerta ou contagem de alerta. Mora em: **Query/Handler (back, fonte da verdade)** + **Front (UX: não renderiza onde não recebe)**. Validada em: **back + front**.

- **R2 — Quem pode LER os alertas.** Tem direito de leitura quem satisfaz **pelo menos uma** condição, sempre **dentro do tenant** do paciente:
  - é **Dono** do estabelecimento (vê sempre); **ou**
  - **está atendendo** o paciente agora — há contexto de atendimento ativo dele com aquele paciente (ex.: agendamento atribuído ao profissional com check-in feito e status não terminal; **ou** o ato de iniciar/conduzir a evolução atual conta como "está atendendo"); **ou**
  - **já atendeu** o paciente — existe evolução/atendimento passado **autorado por ele** no prontuário daquele paciente.

  Um **Profissional** sem nenhum vínculo de atendimento (passado ou ativo) com o paciente, e que **não é Dono**, **não** lê os alertas — mesmo abrindo o prontuário. **Recepção nunca** lê. Mora em: **Handler/Query (back, fonte da verdade)** + **Front (oculta o que não recebe)**. Validada em: **back + front**.

  > **Nota de segurança clínica (não-negociável):** o ato de iniciar/conduzir o atendimento atual **conta** como "está atendendo". Um médico em **primeira consulta de paciente novo** (sem histórico de evolução) **precisa** ver os alertas — ex.: alergia a penicilina antes de prescrever. O bloqueio recai sobre abrir o prontuário **sem contexto de atendimento e sem histórico, não sendo Dono** — não sobre o atendimento legítimo em curso.

- **R3 — Quem pode GERIR (criar/editar/remover) os alertas.** Apenas **Dono** (sempre) e **Profissional que atende/atendeu** o paciente (mesmas condições da R2). A gestão ocorre **dentro do prontuário** (cabeçalho), nunca pelo formulário geral do paciente. Mora em: **Domain/Handler (back, fonte da verdade)** + **Front (controle de edição visível só para quem pode)**. Validada em: **back + front**.

- **R4 — Recepção perde leitura e gestão de alertas.** A recepção continua cadastrando/editando dados administrativos do paciente (nome, CPF, telefone, e-mail, endereço, tags, opt-in WhatsApp), mas a seção "Alertas clínicos" **sai** do formulário geral (`PacienteFormModal`) e nenhum payload entregue à recepção contém alertas. Mora em: **Front (remoção da seção)** + **Handler/DTO (back não devolve alerta para esse contexto)**. Validada em: **back + front**.

- **R5 — Enforcement na fonte, não só na UI.** A trava é no **backend**: uma chamada direta de API (sem passar pela UI) por usuário sem direito **não** recebe o conteúdo dos alertas nem a contagem — recebe a mesma resposta genérica de quem simplesmente não tem o dado (campo ausente/vazio), sem revelar que existe alerta oculto. Mora em: **Handler/Query**. Validada em: **back** (teste por chamada direta, não só via tela).

- **R6 — Multi-tenant rígido.** Toda leitura/gestão de alerta filtra `estabelecimento_id` do tenant ativo. Paciente de outro estabelecimento → "não encontrado" genérico; nada é devolvido e nada com PII alheio é logado. Mora em: **Query/Handler**. Validada em: **back**.

- **R7 — Mensagem genérica e sem PII.** Negativa de acesso a alerta, paciente inexistente no tenant, ou erro de validação retornam mensagem **genérica** ("não encontrado" / "sem permissão"), sem ecoar conteúdo de alerta, nome ou CPF do paciente, e sem PII em log. A negativa de leitura **não** distingue "não existe alerta" de "existe mas você não pode ver" — para não vazar a existência do dado. Mora em: **Handler** + **Infra (log)**. Validada em: **back**.

- **R8 — Audit de acesso reusa o trilho do prontuário.** A leitura do prontuário já é auditada (`IProntuarioAcessoLogService` → `TipoAcessoProntuario.Leitura`, gravado em `ObterProntuarioDoPacienteQueryHandlers`). A **gestão** de alerta (criar/editar/remover) é **escrita sensível no prontuário** e deve registrar 1 linha de **`TipoAcessoProntuario.Escrita`** via o mesmo serviço, best-effort (falha do audit não bloqueia a operação). **Sem tabela/serviço de audit novo** — reuso do existente. Mora em: **Handler**. Validada em: **back**.

- **R9 — Reuso > duplicação.** A entrega reusa: a coluna `pacientes.alertas`, o serviço de audit `IProntuarioAcessoLogService`, o enum `TenantPapel`/`CurrentTenantAccessor.Papel` para o papel, e os sinais já existentes de atendimento (`ProntuarioEvolucao.AutorUsuarioId`, `Agendamento.ProfissionalUsuarioId`/`CheckInEm`/`Status`). Antes de criar endpoint/query/DTO novos, conferir se dá para estender os existentes. Mora em: transversal. Validada em: revisão de QA.

## 5. Modelo de dados

**Provavelmente nenhuma migration de schema** — a confirmar pelo `imedto-database` (não dar como certa):

- `pacientes.alertas text[]` já existe e continua sendo a fonte do dado. **Não há** mudança na estrutura do alerta.
- O sinal de **"atendeu"** (evolução passada autorada pelo usuário) vem de `prontuario_evolucoes.autor_usuario_id` (= `ProntuarioEvolucao.AutorUsuarioId`), associado ao prontuário do paciente. É **leitura** (EXISTS), não escrita.
- O sinal de **"está atendendo"** vem de `agendamentos` do paciente atribuídos ao profissional (`ProfissionalUsuarioId`) com **check-in feito** (`CheckInEm` não nulo) e **status não terminal** (não `Concluido`/`Cancelado`/`Expirado`) — **ou** o contexto de evolução em curso. Também leitura.

**Ação do `imedto-database` (verificação + eventual índice, registrada como CA):**
1. Confirmar que a checagem de "atendeu/atendendo" pode ser resolvida por **query de leitura** sem schema novo.
2. Avaliar **índice de performance** para a checagem de existência (ex.: `prontuario_evolucoes (prontuario_id, autor_usuario_id)` e/ou um índice em `agendamentos (paciente_id, profissional_usuario_id, status)` — se ainda não houver cobertura) — só criar via migration idempotente em `db/migrations/` **se** a inspeção do plano indicar necessidade real. Não criar índice especulativo.
3. Não há backfill: o dado de alerta já existe e permanece intocado.

**Multi-tenant**: todas as consultas (alerta, "atendeu/atendendo", paciente) filtram `estabelecimento_id`. **LGPD**: alerta clínico é **dado sensível de saúde** — minimização (não trafega fora do contexto do prontuário), audit no trilho do prontuário, sem PII em log/erro, mensagem genérica.

## 6. UX e fluxo

**Lista de pacientes (`PacientesView.vue`)** — remoção:
```
ANTES:  Maria Silva   [vip] [gestante]  ⚠ 2 alertas      (11) 99999-0000
DEPOIS: Maria Silva   [vip] [gestante]                   (11) 99999-0000
```
Sem o badge de alerta. O fallback "—" da célula de tags passa a considerar só tags (sem `qtdAlertas`).

**Detalhe do paciente — informações básicas (`PacienteDetalheView.vue`)** — remoção:
o bloco "Alertas clínicos" sai das informações básicas. As demais seções (dados, tags, documentos, financeiro, convênios, etc.) permanecem.

**Check-in da agenda (`CheckInModal.vue`)** — remoção:
o resumo `alertas-resumo` (linhas ~319-322) sai. O check-in continua exibindo dados administrativos do paciente (telefone, nascimento) — sem alertas.

**Formulário geral do paciente (`PacienteFormModal.vue`)** — remoção da gestão:
a seção "Alertas clínicos" (criar/editar/remover) sai do formulário. Nome, CPF, telefone, e-mail, endereço/CEP, observações, tags e opt-in WhatsApp permanecem. Recepção segue editando o que é administrativo.

**Cabeçalho do prontuário (`ProntuarioPacienteHeader.vue`)** — único ponto de leitura + gestão:
```
┌──────────────────────────────────────────────────────────────┐
│ ← [MS]  Maria Silva                                            │
│         34 anos · F · CPF 123.456.789-09 · (11) 99999-0000     │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │ 🚫 Alergia a penicilina    ⓘ Anticoagulada   [+ Alerta]   │ │ ← só p/ quem pode ler;
│  └──────────────────────────────────────────────────────────┘ │   [+ Alerta]/editar só p/ quem pode gerir
│         📅 22/06/2026 14:00 · Consulta · Dr. House             │
└──────────────────────────────────────────────────────────────┘
```
- Para **quem tem direito de leitura** (R2): o bloco de alertas aparece como hoje (vermelho para "alergia*", âmbar para os demais).
- Para **quem não tem direito** (ex.: Profissional sem vínculo, não-Dono): o bloco **não aparece** — porque o backend **não devolveu** o conteúdo (R5). Não há "cadeado" que revele que existe alerta oculto.
- O controle de **gestão** (adicionar/editar/remover alerta) aparece **apenas** para quem pode gerir (R3). Reusar componentes do design system já usados na edição de alertas do `PacienteFormModal` (chips/inputs de array), movidos para o contexto do prontuário.
- **Estados**: carregando (skeleton do header já existente) / sem alertas e com permissão → não mostra bloco (ou estado vazio discreto ao gerir) / com alertas e permissão → lista / sem permissão → bloco ausente / erro → header sem alertas, sem mensagem que vaze PII.
- **Tipografia**: só tokens (`var(--text-*)`, `var(--font-weight-*)`), CLAUDE.md §5.
- **Mobile-ready**: o bloco de alertas e o controle de gestão acompanham a responsividade já existente do header.

**Ponto de atenção técnico (confirmar com o dev):** hoje o `ProntuarioView.vue` carrega o objeto `paciente` via `pacienteService.obter(pacienteId)` → `GET /api/paciente/{id}`, **o mesmo** endpoint que alimenta as informações básicas do detalhe. Como a R2/R5 exigem que o conteúdo de alerta só vá **no contexto do prontuário e gated**, o dev/db devem decidir o caminho técnico: (a) tirar `Alertas[]` do `GET /api/paciente/{id}` (detalhe administrativo) e entregar o conteúdo gated **via o fluxo do prontuário** (no `ProntuarioCompletoDto` ou endpoint dedicado de alertas do prontuário), ou (b) gated condicional no próprio `GET /paciente/{id}` conforme contexto/permissão. **A regra de negócio (R1/R2/R5) é o que vale; o desenho do endpoint é liberdade técnica do dev/db** — desde que a recepção e o detalhe administrativo nunca recebam alertas e o gating seja no backend.

## 7. Critérios de aceite (testáveis)

- **CA1** (remoção da lista): Dado um paciente com 2 alertas clínicos, Quando qualquer usuário (inclusive Dono) abre a **lista de pacientes** (`PacientesView`), Então **não** existe badge "X alerta(s)" na linha, e o payload de `GET /api/paciente` (listagem) **não** contém o campo `qtdAlertas` (nem qualquer contagem de alerta).

- **CA2** (remoção das informações básicas): Dado um paciente com alertas, Quando qualquer usuário abre o **detalhe do paciente** (`PacienteDetalheView`, informações básicas), Então **não** há bloco "Alertas clínicos" na tela, e o payload do detalhe administrativo **não** traz o array `alertas`.

- **CA3** (remoção do check-in): Dado um agendamento de paciente com alertas, Quando a recepção (ou qualquer um) abre o **check-in** (`CheckInModal`), Então **não** há resumo/linha de alertas no modal.

- **CA4** (remoção da gestão no form geral): Dado o **formulário geral do paciente** (`PacienteFormModal`) aberto por qualquer persona (inclusive Dono, em qualquer ponto que o use — incl. atalho do agendamento), Quando o usuário percorre os campos, Então **não** existe seção "Alertas clínicos" para criar/editar/remover; os demais campos administrativos permanecem.

- **CA5** (leitura no prontuário — Dono sempre): Dado um usuário **Dono** do estabelecimento, Quando abre o prontuário de um paciente do seu tenant (mesmo sem nunca tê-lo atendido), Então o cabeçalho **exibe** os alertas e o backend **devolve** o conteúdo dos alertas.

- **CA6** (leitura — Profissional que já atendeu): Dado um **Profissional** que possui **evolução passada autorada por ele** no prontuário do paciente, Quando abre o prontuário, Então o cabeçalho **exibe** os alertas (backend devolve o conteúdo).

- **CA7** (leitura — Profissional que está atendendo): Dado um **Profissional** com **contexto de atendimento ativo** do paciente (agendamento atribuído a ele com check-in feito e status não terminal, **ou** conduzindo a evolução atual), Quando abre o prontuário, Então o cabeçalho **exibe** os alertas (backend devolve o conteúdo) — **inclusive na primeira consulta de paciente novo, sem histórico** (segurança clínica, R2 nota).

- **CA8** (leitura negada — Profissional sem vínculo, não-Dono): Dado um **Profissional** que **nunca atendeu** o paciente, **não está atendendo** e **não é Dono**, Quando abre o prontuário desse paciente, Então o cabeçalho **não exibe** alertas e o backend **não devolve** o conteúdo nem a contagem — a resposta é indistinguível de "paciente sem alertas" (não revela que há alerta oculto).

- **CA9** (leitura negada — Recepção): Dado um usuário com papel **Recepção**, Quando (por qualquer caminho de UI ou chamada direta) tenta obter os alertas de um paciente, Então **nunca** recebe o conteúdo nem a contagem — independentemente de o paciente ter alertas.

- **CA10** (enforcement no backend — chamada direta): Dado um usuário **sem direito de leitura** (Recepção, ou Profissional sem vínculo não-Dono), Quando chama **diretamente a API** (sem a UI) o endpoint que serve o conteúdo de alertas, Então a resposta **não** inclui o conteúdo dos alertas nem a contagem, retornando o mesmo formato genérico de "sem dado" — validado por **chamada direta**, não só pela tela.

- **CA11** (gestão restrita — caminho feliz): Dado um usuário **Dono** ou **Profissional que atende/atendeu**, Quando, no cabeçalho do prontuário, adiciona/edita/remove um alerta e salva, Então a alteração persiste em `pacientes.alertas` (via o comando/endpoint gated), o cabeçalho reflete o novo estado, e o backend retorna sucesso.

- **CA12** (gestão negada — sem vínculo / Recepção): Dado um usuário **Recepção** ou **Profissional sem vínculo (não-Dono)**, Quando (por chamada direta à API) tenta criar/editar/remover um alerta de um paciente, Então o backend retorna **403** (sem permissão) genérico, nada é persistido, e nenhum log contém PII; e no front o controle de gestão **não** é renderizado para essa persona.

- **CA13** (multi-tenant): Dado um usuário do estabelecimento **B**, Quando tenta ler ou gerir alertas (via UI ou chamada direta) de um paciente do estabelecimento **A**, Então o backend retorna **404 genérico** ("não encontrado"), nada é devolvido/persistido e nenhum log contém PII do paciente alheio.

- **CA14** (mensagem genérica / não-vazamento): Dado qualquer negativa (sem permissão, paciente inexistente no tenant, validação), Quando o backend responde, Então a mensagem é **genérica**, **sem** conteúdo de alerta, nome ou CPF, e **sem PII em log**; e a negativa de leitura **não** revela se o alerta existe ou não.

- **CA15** (audit de gestão — escrita no prontuário): Dado um Dono/Profissional habilitado que cria/edita/remove um alerta pelo prontuário, Quando o handler processa, Então é gravada 1 linha de `TipoAcessoProntuario.Escrita` em `prontuario_acesso_log` via `IProntuarioAcessoLogService` (`{ prontuario_id, usuario_id, estabelecimento_id, timestamp }`), best-effort (falha do audit não bloqueia a operação), **sem** tabela/serviço de audit novo.

- **CA16** (audit de leitura inalterado): Dado o acesso ao prontuário que exibe alertas, Quando ocorre, Então o audit de **leitura** do prontuário (`TipoAcessoProntuario.Leitura`) continua sendo registrado como hoje — esta demanda **não** remove nem duplica o audit de leitura existente.

- **CA17** (sem nova exposição de alerta): Dado o conjunto de telas/endpoints do produto, Quando a entrega é concluída, Então o conteúdo/contagem de alerta **não** aparece em nenhum lugar novo (lista, busca rápida, hover, tooltip de agenda, DTO de listagem/detalhe administrativo) — **apenas** no cabeçalho do prontuário, gated.

- **CA18** (verificação de schema/performance pelo `imedto-database`): Dado o RDS de dev/stage, Quando o `imedto-database` inspeciona a query de "atendeu/atendendo" (EXISTS em `prontuario_evolucoes` por autor + EXISTS em `agendamentos` por profissional/check-in/status), Então confirma que **não há schema novo necessário** e avalia o plano; se a checagem de existência não estiver coberta por índice e o plano indicar custo relevante, gera a **migration idempotente** (índice) em `db/migrations/`. Resultado da inspeção registrado no hand-off; **nenhuma migration sem evidência**.

- **CA19** (documentação viva): Dado que esta demanda altera regra cross-cutting de visibilidade de dado sensível, Quando a entrega é concluída, Então `Docs/LGPD.md` contém a subseção "Alertas clínicos — visibilidade restrita" descrevendo: alerta como dado sensível, exposição só no cabeçalho do prontuário, leitura por Dono + atendeu/atendendo, gestão restrita às mesmas personas, enforcement no backend, audit no trilho do prontuário, e mensagem genérica que não revela a existência do alerta — ver §10.

## 8. Riscos e dependências

- **Risco principal — fonte única do `paciente` no prontuário.** O `ProntuarioView.vue` reaproveita `GET /api/paciente/{id}` (o mesmo do detalhe administrativo) para popular o header. Tirar alertas "do detalhe" sem quebrar o header exige decidir **onde** o conteúdo gated de alerta passa a viajar (DTO do prontuário ou endpoint dedicado). **Mitigação**: §6 deixa a decisão técnica explícita ao dev/db; CA2 (detalhe sem alerta) e CA5–CA8 (prontuário gated) cobrem ambos os lados.
- **Risco — regressão de leitura legítima (segurança clínica).** Se o gating for restritivo demais, um médico em atendimento legítimo (primeira consulta, paciente novo) deixaria de ver alergia crítica. **Mitigação**: R2 nota + CA7 tornam o "está atendendo / conduzindo a evolução atual" um caminho de acesso obrigatório.
- **Risco — vazamento por contagem.** Remover o conteúdo mas deixar a contagem (`qtdAlertas`) ainda revela que o paciente "tem algo". **Mitigação**: CA1/CA8/CA10 exigem remover também a contagem e tornar a negativa indistinguível de "sem alerta".
- **Risco — `qtdAlertas: 0` em mocks da agenda.** `AgendaView.vue` seta `qtdAlertas: 0` em objetos mock (≈ linhas 279, 318). Provavelmente removível junto, mas **confirmar com o dev** que nada depende desse campo após a remoção do DTO — não remover às cegas.
- **Risco — performance da checagem "atendeu/atendendo".** Dois EXISTS por carga de prontuário. **Mitigação**: CA18 — `imedto-database` avalia índice; preferir resolver o gating numa checagem leve (não trazer evoluções/agendamentos inteiros).
- **Dependência — `imedto-database`** para a verificação de schema/índice (CA18). O dev aciona antes do hand-off ao QA.
- **Sem dependência** de novo provider/integração externa.

## 9. Observações para execução

**Não-negociável:**
- **Enforcement no backend** (R1/R5/R10... — R5): a UI ocultar o bloco **não basta**; a API não pode devolver conteúdo/contagem a quem não tem direito. QA valida por **chamada direta**.
- A negativa de leitura **não distingue** "não tem alerta" de "tem mas você não pode ver" (R7/CA8/CA14) — para não vazar a existência do dado.
- **Recepção nunca** lê nem gere alerta (R4); a seção sai do `PacienteFormModal` (CA4) e nenhum payload da recepção traz alerta (CA9).
- **Dono vê e gere sempre**; **Profissional só com vínculo de atendimento** (atendeu/atendendo), **incluindo a primeira consulta de paciente novo** (CA7).
- **Reuso** do serviço de audit `IProntuarioAcessoLogService` (escrita na gestão — R8/CA15); **nenhuma tabela/serviço de audit novo**.
- **Não trazer alerta para lugar novo** (CA17). Respeita a decisão LGPD anterior (contagem/conteúdo de alerta não vai para a lista).
- Tipografia só via tokens (CLAUDE.md §5).
- Multi-tenant em toda query (R6/CA13); mensagens genéricas sem PII (R7/CA14).

**Liberdade técnica do dev/db:**
- **Onde** o conteúdo gated de alerta viaja (no `ProntuarioCompletoDto`, num campo gated do prontuário, ou em endpoint dedicado de alertas do prontuário) — desde que o detalhe administrativo e a recepção nunca o recebam e o gating seja no backend.
- **Como** modelar o comando/endpoint de gestão de alerta no contexto do prontuário (reusar `AtualizarPacienteCommand` com gating específico **ou** comando/endpoint próprio) — desde que gated por Dono+atende/atendeu e auditado como escrita de prontuário.
- **Como** implementar a checagem "atendeu/atendendo" (EXISTS combinados) — desde que leve e multi-tenant.
- Reuso dos componentes de edição de alerta (chips/array) já presentes no `PacienteFormModal`, movidos para o header do prontuário.

**Pontos de mudança (do diagnóstico técnico — código ativo):**

Frontend:
- `frontend/src/views/pacientes/PacientesView.vue` (≈309-313) — remover badge `alert-count` e ajustar o fallback `—` para considerar só tags; remover uso de `p.qtdAlertas`.
- `frontend/src/views/pacientes/PacienteDetalheView.vue` (≈619-625) — remover bloco "Alertas clínicos" das informações básicas.
- `frontend/src/components/agenda/CheckInModal.vue` (≈319-322) — remover `alertas-resumo`.
- `frontend/src/components/pacientes/PacienteFormModal.vue` (≈516-545) — remover a seção "Alertas clínicos" (não enviar `alertas` no payload de cadastro/edição geral).
- `frontend/src/components/prontuario/ProntuarioPacienteHeader.vue` (≈71-124) — manter o bloco de alertas **condicionado** ao conteúdo que o backend devolver (gated); adicionar o controle de gestão (criar/editar/remover) visível só para quem pode gerir.
- `frontend/src/views/pacientes/ProntuarioView.vue` (≈198, 483-484) — ajustar de onde o header recebe os alertas conforme a decisão técnica de §6 (sem alerta no `GET /paciente/{id}` administrativo).
- `frontend/src/views/agenda/AgendaView.vue` (≈279, 318) — **confirmar** remoção de `qtdAlertas: 0` nos mocks (ponto de atenção, não às cegas).
- `frontend/src/services/pacienteService.ts` — ajustar o tipo `Paciente`/contrato se `alertas`/`qtdAlertas` saírem dos DTOs administrativos.

Backend:
- `backend/src/Services/Imedto.Backend.Contracts/Pacientes/Queries/Results/PacienteDto.cs` — remover `PacienteListaItemDto.QtdAlertas` (≈59-60); decidir o destino de `PacienteDto.Alertas[]` (≈28-32) conforme §6 (tirar do detalhe administrativo).
- `backend/src/Services/Imedto.Backend.Infrastructure/Database/Repositories/PacienteQueryRepository.cs` — remover `coalesce(array_length(alertas,1),0) AS QtdAlertas` da listagem (≈61) e tirar `alertas` do detalhe administrativo (≈215); o conteúdo gated passa a ser servido pelo fluxo do prontuário.
- Fluxo do prontuário (`ObterProntuarioDoPacienteQuery(Handlers)` + `ProntuarioCompletoDto`/`ProntuarioDto` ou endpoint dedicado) — passar a entregar o conteúdo de alerta **gated** por R2 (Dono + atendeu/atendendo), com a checagem de "atendeu/atendendo".
- Gestão de alerta no prontuário — comando/endpoint gated por R3 (Dono + atende/atendeu), audit `TipoAcessoProntuario.Escrita` (R8). Avaliar reuso de `AtualizarPacienteCommand`/`CadastrarPacienteCommand` (que hoje carregam `Alertas` — ver `...Commands/AtualizarPacienteCommand.cs:21`, `CadastrarPacienteCommand.cs:18`) vs. comando próprio; **se a gestão sair do fluxo geral do paciente, garantir que o caminho da recepção (`PUT /api/paciente/{id}`) não escreva mais alertas** (a recepção não deve nem ler nem gravar).
- `backend/src/Services/Imedto.Backend.API/Controllers/PacienteController.cs` (≈137, 168, 342) — ajustar o mapeamento de `Alertas` conforme a decisão (a recepção não envia mais alerta pelo cadastro/edição geral).
- Resolução de papel: enum `TenantPapel`/`CurrentTenantAccessor.Papel` (`backend/src/Core/Imedto.Backend.SharedKernel/Tenancy/RequiresEstabelecimentoAttribute.cs:72-79`) — reusar para o gating (Dono vs. Profissional vs. Recepcionista).
- Sinais de atendimento: `ProntuarioEvolucao.AutorUsuarioId` (atendeu) e `Agendamento.ProfissionalUsuarioId`/`CheckInEm`/`Status` (atendendo) — base da checagem.

## 10. Atualização de documentação

Doc a atualizar **na mesma entrega** (cirúrgico, só a seção afetada):

- **`Docs/LGPD.md`** — adicionar a subseção **"Alertas clínicos — visibilidade restrita (briefing 2026-06-22_002)"**, registrando:
  - Alerta clínico (`pacientes.alertas`) é **dado pessoal sensível de saúde** (Art. 5º II / Art. 11 LGPD).
  - **Exposto apenas** no cabeçalho do prontuário, no contexto do atendimento. **Removido** de lista, informações básicas, check-in e formulário geral do paciente.
  - **Leitura** por **Dono (sempre)** ou **Profissional que atendeu** (evolução autorada por ele) / **está atendendo** (atendimento ativo atribuído ou evolução em curso) — **inclusive primeira consulta de paciente novo** (segurança clínica). **Recepção e Profissional sem vínculo (não-Dono) não leem.**
  - **Gestão** (criar/editar/remover) restrita às mesmas personas, **dentro do prontuário**; recepção não gere.
  - **Enforcement no backend**: a API não devolve conteúdo nem contagem a quem não tem direito; a negativa **não revela** a existência do alerta (indistinguível de "sem alerta").
  - **Audit** reusa o trilho do prontuário: leitura → `TipoAcessoProntuario.Leitura`; gestão → `TipoAcessoProntuario.Escrita` (best-effort, sem tabela nova).
  - **Multi-tenant** rígido + mensagens genéricas sem PII.

- **`Docs/ARQUITETURA.md`** / **`Docs/DESIGN.md`** / **`Docs/INFRA.md`** / **`Docs/COMANDOS.md`** — **sem alteração** (sem bounded context novo, sem componente de design system novo, sem recurso de infra, sem comando recorrente novo; reuso de gating por papel já documentado).
</content>
</invoke>
