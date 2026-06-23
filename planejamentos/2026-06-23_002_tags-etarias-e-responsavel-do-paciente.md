# Tags etárias (idoso / menor de idade) e responsável do paciente

**ID**: 2026-06-23_002
**Status**: Aprovado por usuário em 2026-06-23 (decisões-chave fechadas pelo orquestrador)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (cadastro/edição de paciente), agenda (card de agendamento), relatório (não toca), permissionamento (reusa o existente)

---

## 1. Contexto e motivação

A recepção e os profissionais precisam reconhecer **em segundos** se um paciente é idoso ou menor de idade — porque o tratamento, a abordagem e os cuidados legais mudam. Hoje a idade aparece como texto solto ("36 anos") na lista e no detalhe, mas não há sinalização visual rápida, e no card de agenda nem a idade aparece. O atendente precisa abrir a ficha e fazer a conta de cabeça.

Além disso, o sistema não tem onde registrar **quem responde pelo paciente**. Para menores de 18 anos isso é obrigatório na prática clínica (quem assina, quem é avisado, quem autoriza). E o problema não é só de menores: um adulto incapacitado, um idoso dependente ou um paciente em situação de fragilidade também precisa de um responsável cadastrado — por isso o campo tem de estar disponível a qualquer momento, não só quando a idade dispara a obrigatoriedade.

Esta demanda resolve as duas dores com baixo custo: a tag etária é puramente visual (derivada da data de nascimento, sem novo dado persistido) e o responsável são três campos novos no cadastro de paciente.

**Evidência de código (investigação):**
- O card de agenda (`AgendamentoRow.vue`) só recebe `pacienteNome` + `pacienteId` — o `AgendamentoDto` do backend não traz data de nascimento (minimização LGPD).
- A lista (`PacientesView.vue`) e o detalhe (`PacienteDetalheView.vue`) já calculam idade, cada um com sua própria função local (cálculo de idade duplicado em ~5 lugares no front).
- Já existe uma tag **manual** `"idoso"` no catálogo `frontend/src/constants/pacienteTags.ts` (o usuário marca à mão na seção "Tags clínicas"). A tag etária desta demanda é **automática e separada** — não confundir as duas.

---

## 2. Persona-alvo

- **Recepcionista** — na agenda do dia, ao bater o olho no card de cada paciente, e na lista de pacientes ao buscar/triar. Uso: diário, alta frequência.
- **Profissional / Dono** — no cadastro/edição de paciente e no detalhe da ficha, ao registrar o responsável. Uso: no cadastro inicial e em edições pontuais.

---

## 3. Escopo

**Inclui:**
- Tag visual **"Idoso"** (≥ 60 anos) e **"Menor de idade"** (< 18 anos) derivada da data de nascimento, exibida em 3 lugares: card de agendamento na agenda, lista de pacientes, detalhe/perfil do paciente.
- Centralização do cálculo de idade e da determinação da faixa etária em **um único util** reutilizado pelos 3 lugares.
- Três campos novos no paciente — **Nome do responsável**, **Parentesco** (select de lista fixa) e **Telefone do responsável** — no cadastro E na edição, disponíveis **a qualquer momento** (independente da idade).
- Obrigatoriedade de Nome + Parentesco do responsável **quando o paciente for menor de 18 anos**, com trava no front (UX) e no backend (`BusinessException` → 422, fonte da verdade).
- Migration com as 3 colunas novas na tabela `pacientes` (aciona `imedto-database`).
- Exposição da **faixa etária derivada** (`pacienteFaixaEtaria`) no DTO/query de agendamento, para a tag aparecer no card (ver Decisão Pendente / Assunção D1).

**Não inclui:**
- Tag para faixa "adulto" (18–59) — paciente nessa faixa **não** recebe tag (decisão fechada).
- Qualquer regra de negócio acionada pela idade além de UX (não bloqueia agendamento, não muda fluxo de cobrança, não muda prontuário). As tags são puramente visuais.
- Persistência da tag etária (ela é sempre derivada da data de nascimento, nunca salva no array `tags`).
- Validação de telefone do responsável por dígito/operadora — só máscara visual + saneamento de dígitos (espelha o telefone do próprio paciente).
- Notificar/enviar mensagem ao responsável (WhatsApp, e-mail) — fora de escopo; é só cadastro de contato.
- Vincular o responsável a um outro paciente/usuário do sistema (é texto livre + parentesco, não um relacionamento entre registros).
- Migração/backfill de responsável para pacientes já existentes (campos novos nascem vazios).

---

## 4. Regras de negócio

- **R1 — Faixa etária derivada.** A faixa etária é calculada a partir de `DataNascimento`:
  - `< 18 anos` → **menor**; `>= 60 anos` → **idoso**; entre 18 e 59 → **nenhuma**; sem data de nascimento → **nenhuma**.
  - Borda inferior: o paciente **deixa de ser menor exatamente no dia em que completa 18 anos** (no aniversário de 18 ele já não é menor).
  - Borda superior: o paciente **passa a ser idoso exatamente no dia em que completa 60 anos** (no aniversário de 60 ele já é idoso).
  - Idade em anos completos = anos decorridos descontando se o aniversário ainda não chegou no ano corrente (mesma lógica das funções atuais do front).
  - Mora em: **Front** (util único `frontend/src/utils/idade.ts`) para os 3 pontos de exibição **+ espelho no Back** (query Dapper de agendamento, ver D1) para o card de agenda. A faixa etária **nunca é persistida** — é sempre recalculada a partir da data de nascimento. Validada em: front (rende as tags) + back (calcula `pacienteFaixaEtaria` no SQL).

- **R2 — Tag é apresentação, não dado de negócio.** A tag etária não tem efeito em nenhuma regra (não bloqueia, não altera fluxo). Coexiste com as tags manuais do array `tags` (catálogo `pacienteTags.ts`) sem sobrescrevê-las nem com elas se misturar. Mora em: **Front**.

- **R3 — Responsável é opcional por padrão, obrigatório para menor.** Os três campos de responsável ficam **sempre disponíveis** no cadastro e na edição (a UI não os esconde por idade).
  - Se a data de nascimento informada indica **menor de 18 anos**, salvar (cadastro ou edição) **exige Nome do responsável + Parentesco preenchidos**. Telefone do responsável é **recomendado, não obrigatório**.
  - Se o paciente é **maior de idade ou sem data de nascimento**, o responsável é **totalmente opcional** (pode preencher 0, 1, 2 ou 3 campos).
  - Mora em: **Domain** (`Paciente.Cadastrar` e `Paciente.AtualizarDados` aplicam a trava) **+ Handler** (passa os campos) **+ Front** (UX que impede o submit e destaca os campos). Validada em: **back (422, fonte da verdade) + front (botão Salvar desabilitado / mensagem no campo)**.

- **R4 — Coerência do responsável.** Quando o usuário preenche **parcialmente** o responsável de um paciente **maior de idade** (ex.: só o telefone, sem nome nem parentesco), o sistema **aceita e salva como está** — não há trava de "preencheu um, preencha todos" para maiores. A obrigatoriedade de Nome+Parentesco só existe no caso menor (R3). Mora em: **Domain + Front**.

- **R5 — Parentesco vem de lista fixa controlada pelo front.** O select de parentesco oferece uma lista fechada de opções (ver lista na seção 4.1). O valor armazenado é a **chave/label escolhida** (texto). O backend **não** valida contra um enum — apenas sania (trim) e limita tamanho, espelhando o tratamento de `tags` e gênero. Tag fora da lista (ex.: vinda de import futuro) não quebra o front. Mora em: **Front (lista) + Domain (trim + maxlen)**.

- **R6 — Multi-tenant.** Os campos de responsável e a leitura do paciente continuam escopados a `estabelecimento_id`. O handler de atualização já carrega o paciente via `ObterPorIdOuNulo(id, estabelecimentoId)` — paciente de outro tenant retorna null → `BusinessException("Paciente não encontrado.")` (422, ver [[codigos-http-padrao-excecoes]]). Mensagem genérica, sem revelar existência. Mora em: **Repositório (falha-fechada) + Handler**.

- **R7 — LGPD: responsável é PII de terceiro.** Nome e telefone do responsável são dados pessoais de uma terceira pessoa (minimização: só o que a tela usa). Trafegam apenas no `PacienteDto` (detalhe, para o form de edição fazer round-trip) — **não** entram no `PacienteListaItemDto`, **não** entram no `PacienteBuscaRapidaDto`, **não** aparecem em log nem em mensagem de erro. A edição/cadastro do responsável é **operação de escrita auditada** (já existe `IPacienteAcessoLogService.RegistrarAsync(..., TipoAcessoPaciente.Edicao)` no fluxo de Cadastrar/Atualizar — reusar, sem nova trilha). A exportação LGPD (`PacienteExportLgpdDto`, Art. 18) deve incluir os campos de responsável, pois são dados pessoais do titular relacionado. Mora em: **Domain + Handler + DTOs + Anonimização**.

- **R8 — Anonimização limpa o responsável.** Ao anonimizar o paciente (`Paciente.Anonimizar`), os três campos de responsável também são zerados (PII de terceiro associada ao titular). Mora em: **Domain (`Anonimizar`)**.

- **R9 — RBAC inalterado.** Cadastro e edição de paciente (POST/PUT `/api/paciente`) já exigem papel **Profissional ou Dono** (`[RequiresPapel(Profissional, Dono)]`). Não se cria papel novo nem permissão nova. A leitura de paciente (GET) segue a permissão de ação `pacientes` já existente. Mora em: **Controller (atributos já existentes)**.

### 4.1 Lista fixa de parentescos (proposta)

Valores oferecidos no select (na ordem):

1. Mãe
2. Pai
3. Avó / Avô
4. Tio / Tia
5. Irmão / Irmã
6. Filho / Filha
7. Cônjuge / Companheiro(a)
8. Tutor / Responsável legal
9. Outro

> Observação de produto: "Tutor / Responsável legal" cobre guarda/curatela; "Outro" é o escape para casos não previstos sem travar o cadastro. A lista vive em uma constante no front (ex.: `frontend/src/constants/parentescos.ts`) para reuso e fácil ajuste — não vira enum no banco (ver R5 e seção 5).

---

## 5. Modelo de dados

**Tabela afetada: `pacientes` (3 colunas novas, todas nullable).**

| Coluna | Tipo | Nullable | Observação |
|---|---|---|---|
| `responsavel_nome` | `varchar(200)` | sim | PII de terceiro. Trim no domain. |
| `responsavel_parentesco` | `varchar(40)` | sim | Texto simples (não enum). Lista fixa controlada no front. Mesmo maxlen das tags. |
| `responsavel_telefone` | `varchar(20)` | sim | Saneado para dígitos (igual `telefone` do paciente). |

**Decisão: parentesco como `varchar`, não enum no banco.** Justificativa:
- O sistema já trata listas controladas pelo front como texto (`tags` é `text[]`, `genero` é `string` validada por `Enum.TryParse` no handler com fallback). Manter o padrão evita migration de enum a cada novo parentesco e mantém o domínio agnóstico (R5).
- Não há query que filtre/agregue por parentesco (é dado descritivo), então não há ganho de índice/integridade que justifique enum.

**Sem migration em outras tabelas.** A faixa etária do agendamento (D1) é **coluna calculada na query Dapper** a partir de `pacientes.data_nascimento` — não é coluna física, não gera migration.

**Constraints / índices:** nenhum índice novo (os campos de responsável não são chave de busca). As colunas entram no aggregate `Paciente` e na `PacienteConfiguration` (EF). Nenhum default especial além de `NULL`.

**Multi-tenant:** as colunas pertencem à linha do paciente, que já é escopada por `estabelecimento_id` — herdado, sem ação extra.

> `imedto-database` será acionado pelo `imedto-developer` para gerar a migration EF + SQL idempotente (`ADD COLUMN IF NOT EXISTS`), seguindo o gotcha de idempotência de DDL ([[gotcha-ef-addcolumn-nao-idempotente]]).

---

## 6. UX e fluxo

### 6.1 Tag etária (3 lugares)

Componente novo de design system sugerido: **`AppAgeTag.vue`** em `frontend/src/components/ui/` — recebe a faixa (`"idoso" | "menor"`) e renderiza pill com ícone + cor + label, no mesmo estilo visual das `tag-pill` já usadas (`color-mix`, ícone Font Awesome, label). Centraliza o visual e os textos ("Idoso", "Menor de idade") e cores/ícones (idoso: ícone `fa-person-cane`, tom azul/âmbar; menor: ícone `fa-child` ou `fa-child-reaching`, tom destacado distinto da tag manual "idoso"). Se o dev concluir que `AppBadge`/`AppStatusPill` atendem com ajuste mínimo, pode reusar — mas a determinação da faixa vem **sempre** do util único, nunca duplicada no template.

- **Card de agenda** (`frontend/src/components/agenda/AgendamentoRow.vue`): tag ao lado do nome do paciente, discreta (não compete com status/cobrança). Aparece somente quando `pacienteFaixaEtaria` vier preenchida (D1).
- **Lista de pacientes** (`frontend/src/views/pacientes/PacientesView.vue`): tag etária **antes** das tags manuais já renderizadas (`p.tags.slice(0,3)`), reusando a `dataNascimento` que o item já traz.
- **Detalhe do paciente** (`frontend/src/views/pacientes/PacienteDetalheView.vue`): tag etária junto ao bloco de idade no header (`fa-cake-candles`) ou ao lado das tags manuais.

Estados:
- **Sem data de nascimento** → nenhuma tag etária (não renderiza nada, sem placeholder).
- **Loading / erro** das listas → herdam os estados já existentes das views (sem novo estado por causa da tag).

### 6.2 Campo de responsável (cadastro/edição)

No `PacienteFormModal.vue`, **seção nova "Responsável"** no formulário completo (modo editar e modo criar-expandido), reusando os componentes de form existentes:
- **Nome do responsável** — `AppField` + `AppInput`.
- **Parentesco** — `AppField` + `AppSelect` populado pela constante `parentescos.ts` (espelha o `AppSelect` de gênero/UF).
- **Telefone do responsável** — `AppField` + `AppInput` + `v-maska="'(##) #####-####'"` (mesmo padrão do celular do paciente).

Comportamento UX:
- A seção fica **sempre visível** no form completo (não condicionada à idade).
- Quando a `dataNascimento` digitada indica menor de 18, a seção ganha indicação visual de obrigatoriedade (Nome e Parentesco com asterisco/`required`) e um hint curto: "Paciente menor de idade — informe o responsável." O botão **Salvar fica desabilitado** enquanto Nome e Parentesco não estiverem preenchidos (espelha o `valido` computed atual). Se o usuário tentar salvar mesmo assim (ex.: força via API), o back retorna 422 com mensagem genérica e a UI a exibe.
- Para o **cadastro rápido** (modo criar não-expandido, só 5 campos): se a data de nascimento ali digitada indicar menor de idade, ao tentar **Cadastrar paciente** a UI **direciona para o cadastro completo** (`expandirCadastro = true`) com a seção Responsável em destaque, em vez de deixar salvar sem responsável. (Assunção D2.)
- Em modo **editar**, os valores existentes do responsável vêm preenchidos do `PacienteDto` (round-trip).

Estados: loading/erro/sucesso herdam o fluxo de salvar já existente do modal (`salvando`, `erro`, `erroDocumento`). Mobile: o form já é responsivo (grid colapsa para 1 coluna em ≤720px) — a seção nova herda.

### 6.3 Reuso de componentes (não-negociável)

- Cálculo de idade/faixa: **um único util** `frontend/src/utils/idade.ts` — os consumidores (lista, detalhe, agenda, e idealmente os 5 pontos hoje duplicados) passam a importá-lo. Não duplicar a lógica de borda no template.
- Campo de telefone: `AppInput` + `v-maska` (padrão atual, sem componente dedicado).
- Select de parentesco: `AppSelect` (padrão de gênero/UF).
- Tag: `AppAgeTag` novo no design system (ou reuso de badge existente se atender) — não inline-style espalhado.

---

## 7. Critérios de aceite (testáveis)

- **CA1 (idoso — caminho feliz):** Dado um paciente com data de nascimento que resulta em 60 anos completos ou mais, Quando ele é exibido na lista, no detalhe ou no card de agenda, Então aparece a tag "Idoso" (ícone + cor), e **não** aparece a tag "Menor de idade".

- **CA2 (menor — caminho feliz):** Dado um paciente com data de nascimento que resulta em menos de 18 anos, Quando ele é exibido nos 3 lugares, Então aparece a tag "Menor de idade", e **não** a tag "Idoso".

- **CA3 (adulto — sem tag):** Dado um paciente entre 18 e 59 anos completos, Quando exibido em qualquer um dos 3 lugares, Então **nenhuma** tag etária é exibida.

- **CA4 (sem data de nascimento — sem tag):** Dado um paciente sem data de nascimento, Quando exibido em qualquer um dos 3 lugares, Então nenhuma tag etária é exibida (sem placeholder, sem erro).

- **CA5 (borda dos 18):** Dado um paciente que completa 18 anos exatamente hoje, Quando exibido, Então **não** recebe a tag "Menor de idade" (no aniversário de 18 ele já é adulto). E dado um paciente que completa 18 anos amanhã, Então **ainda** recebe "Menor de idade" hoje.

- **CA6 (borda dos 60):** Dado um paciente que completa 60 anos exatamente hoje, Quando exibido, Então **já** recebe a tag "Idoso". E dado um paciente que completa 60 anos amanhã, Então **ainda não** recebe "Idoso" hoje.

- **CA7 (tag no card de agenda via faixa derivada):** Dado um agendamento de paciente menor de idade, Quando a agenda carrega, Então o card mostra a tag "Menor de idade" usando o campo `pacienteFaixaEtaria` retornado pela query — **sem** que o `AgendamentoDto` exponha a data de nascimento completa.

- **CA8 (centralização — sem duplicação):** Dado o código após a entrega, Quando se busca a lógica de determinação de faixa etária e cálculo de idade, Então existe **um único** util (`utils/idade.ts`) consumido pela lista, pelo detalhe e (espelhado) pela query de agenda; nenhum template recalcula a borda de 18/60 inline.

- **CA9 (responsável sempre disponível):** Dado um paciente **maior de idade**, Quando o usuário abre o cadastro completo ou a edição, Então a seção "Responsável" está visível e pode ser preenchida (total ou parcialmente), e o paciente salva sem exigir responsável.

- **CA10 (responsável obrigatório para menor — front):** Dado o form de cadastro/edição com data de nascimento de menor de 18, Quando Nome do responsável **ou** Parentesco estão vazios, Então o botão Salvar fica desabilitado e a UI sinaliza os campos obrigatórios; Quando ambos estão preenchidos, Então Salvar é habilitado.

- **CA11 (responsável obrigatório para menor — back/422):** Dado um POST/PUT `/api/paciente` com `DataNascimento` de menor de 18 e `ResponsavelNome` ou `ResponsavelParentesco` vazios, Quando o handler processa, Então retorna **422** (`BusinessException`) com mensagem genérica (ex.: "Para pacientes menores de idade, informe o nome e o parentesco do responsável."), sem PII no corpo nem no log.

- **CA12 (telefone do responsável é opcional para menor):** Dado um paciente menor com Nome + Parentesco do responsável preenchidos e **Telefone do responsável vazio**, Quando salva, Então o paciente é salvo com sucesso (telefone é recomendado, não obrigatório).

- **CA13 (parentesco — select de lista fixa):** Dado o campo Parentesco, Quando o usuário abre o select, Então vê exatamente as opções da lista fixa (4.1), e o valor escolhido é persistido como texto; Quando edita o paciente depois, Então o valor salvo vem pré-selecionado.

- **CA14 (telefone com máscara):** Dado o campo Telefone do responsável, Quando o usuário digita, Então a máscara `(##) #####-####` é aplicada e o valor é saneado para dígitos no backend (igual ao telefone do paciente).

- **CA15 (round-trip na edição):** Dado um paciente com responsável já cadastrado, Quando o `PacienteDto` é carregado no form de edição, Então Nome, Parentesco e Telefone do responsável vêm preenchidos; Quando salvo sem alterar, Então os valores permanecem intactos.

- **CA16 (multi-tenant):** Dado um usuário do estabelecimento B, Quando tenta atualizar (PUT) um paciente do estabelecimento A informando responsável, Então recebe **422** com "Paciente não encontrado." (mensagem genérica), nada é alterado e nenhum dado do tenant A vaza.

- **CA17 (RBAC):** Dado um usuário com a ação `pacientes` mas **sem** papel Profissional/Dono, Quando chama POST/PUT `/api/paciente`, Então recebe **403** (`ForbiddenException`) e, no front, a ação de cadastrar/editar paciente permanece indisponível conforme o gating já existente.

- **CA18 (LGPD — minimização do responsável):** Dado o `PacienteListaItemDto` e o `PacienteBuscaRapidaDto`, Quando retornados pela API, Então **não** contêm nenhum campo de responsável; o responsável só aparece no `PacienteDto` (detalhe). E dado um erro de validação do responsável, Quando o back retorna 422, Então a mensagem é genérica e não contém o nome/telefone do responsável.

- **CA19 (LGPD — audit de escrita):** Dado o cadastro ou a edição de um paciente com dados de responsável, Quando a operação é concluída, Então é registrada uma linha de audit de **Edição** via `IPacienteAcessoLogService` com `{paciente_id, usuario_id, estabelecimento_id, timestamp}` (reuso do trilho atual, sem PII na trilha).

- **CA20 (LGPD — anonimização):** Dado um paciente com responsável cadastrado, Quando o paciente é anonimizado (`Paciente.Anonimizar`), Então `responsavel_nome`, `responsavel_parentesco` e `responsavel_telefone` ficam vazios/nulos, junto com a demais PII.

- **CA21 (LGPD — exportação Art. 18):** Dado um paciente com responsável, Quando o Dono exporta os dados (`GET /api/paciente/{id}/exportar-dados`), Então os campos de responsável constam no JSON exportado (são dados pessoais do titular relacionado).

- **CA22 (tag etária NÃO polui o array `tags`):** Dado um paciente idoso/menor, Quando salvo, Então o array `tags` persistido **não** recebe automaticamente "idoso"/"menor" — a tag etária é sempre derivada da data de nascimento em tempo de exibição, e a tag manual "idoso" do catálogo continua sendo escolha explícita do usuário.

- **CA23 (estados — sem regressão):** Dado a lista de pacientes vazia / o card de agenda sem paciente associado / o detalhe carregando, Quando renderizam, Então os estados de vazio/erro/loading já existentes continuam funcionando; a tag etária só aparece quando há data de nascimento válida.

---

## 8. Riscos e dependências

- **Card de agenda exige tocar a query Dapper de agendamentos** (ver D1). Risco baixo se for só uma coluna calculada (CASE sobre `data_nascimento`), mas é o ponto que cruza a fronteira UX→backend desta demanda. Vigiar performance: o cálculo é por linha já trazida (sem N+1, a faixa sai da própria `pacientes` que a query já junta para o `pacienteNome`).
- **Não confundir a tag etária automática com a tag manual "idoso"** existente em `pacienteTags.ts` — risco de o dev tentar "preencher" o array `tags`. CA22 trava isso.
- **Cálculo de idade duplicado em ~5 lugares** — a centralização (CA8) deve cobrir lista, detalhe e agenda; os demais consumidores (PDF, prontuário header, termos) podem migrar oportunisticamente, mas **não** é obrigatório nesta entrega (evitar scope creep). Mínimo: os 3 pontos da feature usam o util único.
- **Migration aciona `imedto-database`** — 3 colunas nullable; risco baixo, mas seguir idempotência de DDL ([[gotcha-ef-addcolumn-nao-idempotente]]) e o gotcha de re-seed não se aplica (não há seed).
- Dependência de ordem: o dev implementa front+back; aciona DB para as colunas antes de finalizar o handler.

---

## 9. Observações para execução

- **Não-negociável:** a obrigatoriedade do responsável para menor tem **espelho no backend** (R3/CA11) — não basta a trava de UI. A fonte da verdade é o 422 do `BusinessException` no domain (`Paciente.Cadastrar` / `Paciente.AtualizarDados`). O cálculo da faixa para decidir a obrigatoriedade roda no domain a partir de `DataNascimento` + data atual (UTC) — espelhar a mesma borda do front (CA5).
- **Não-negociável:** faixa etária do card de agenda **não** expõe a data de nascimento completa no DTO — só a classificação derivada (D1).
- **Liberdade técnica:** o dev decide se cria `AppAgeTag` novo ou estende `AppBadge`/`AppStatusPill`, desde que (a) a faixa venha do util único e (b) o componente entre no design system se for novo (não inline-style repetido). Preferência por reuso do estilo `tag-pill` já consolidado.
- **Reuso obrigatório:** `PacienteFormModal.vue` é o único form de cadastro/edição (reusado em PacientesView, PacienteDetalheView e AgendaView via parent). A seção Responsável adicionada ali aparece automaticamente nos 3 contextos — confirmar que funciona dentro do fluxo de "Editar dados" disparado da agenda/check-in.
- **Telefone do responsável** segue o mesmo saneamento do telefone do paciente (`TextSanitizer.DigitosOuNulo` no domain). **Não** reaproveitar validação de CPF nem inventar validação de operadora.
- **Lista de parentescos** em constante front (`frontend/src/constants/parentescos.ts`) para reuso, espelhando `pacienteTags.ts`.
- Suíte: adicionar testes de domain para a trava de menor (CadastrarPaciente/AtualizarPaciente — menor sem responsável lança, menor com responsável passa, maior sem responsável passa) e teste de minimização (responsável fora de Lista/BuscaRapida DTOs).

---

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — adicionar `AppAgeTag` (ou a variante etária) à seção de componentes do design system, documentando que tags etárias são **derivadas** (nunca persistidas) e a regra de borda 18/60; registrar a constante `parentescos.ts` se considerado padrão reutilizável. **(Atualizar nesta entrega se o componente novo for criado.)**
- **`Docs/LGPD.md`** — registrar que o cadastro de paciente passou a coletar **PII de terceiro (responsável: nome, parentesco, telefone)**: minimização (só no `PacienteDto`, fora de lista/busca rápida), audit de escrita reusado, anonimização inclui os campos, e a exportação Art. 18 os inclui. **(Atualizar nesta entrega.)**
- **`Docs/ARQUITETURA.md`** — **não** atualizar, salvo se a exposição de `pacienteFaixaEtaria` no DTO de agendamento for considerada um padrão novo de "campo derivado no DTO de leitura" digno de registro (a critério do dev; provavelmente desnecessário — segue o padrão de campos agregados já existente, ex.: badge de cobrança no `AgendamentoDto`).

> O `imedto-developer`/`imedto-business-analyst` faz as atualizações de `Docs/` no mesmo ciclo da entrega; o `imedto-qa` valida que os docs foram atualizados quando o componente/PII novo entrou.

---

## Decisões pendentes / assunções adotadas

> Nenhuma trava o início da implementação. Cada uma adota a assunção padrão abaixo; se o usuário discordar, vira spec gap → addendum.

- **D1 — Como a tag chega ao card de agenda.** O `AgendamentoDto` não traz data de nascimento (minimização LGPD). **Assunção adotada:** expor um campo derivado **`PacienteFaixaEtaria` (`"idoso" | "menor" | null`)** no `AgendamentoDto`, calculado **na query Dapper** a partir de `pacientes.data_nascimento` (CASE/cálculo de idade no SQL), **sem** expor a data completa. É o mínimo que entrega a tag sem aumentar a superfície de PII no card. Alternativa rejeitada por LGPD: expor `pacienteDataNascimento` inteiro no card. Alternativa rejeitada por escopo: não mostrar a tag na agenda (o usuário pediu explicitamente os 3 lugares).

- **D2 — Cadastro rápido + paciente menor.** O cadastro rápido (modo criar não-expandido) tem só 5 campos e não mostra a seção Responsável. **Assunção adotada:** se a data de nascimento digitada no cadastro rápido indicar menor de idade, ao clicar "Cadastrar paciente" a UI **expande automaticamente para o cadastro completo** com a seção Responsável em foco (em vez de bloquear com erro seco ou permitir salvar sem responsável). O backend continua sendo a trava final (CA11). Alternativa rejeitada: deixar salvar pelo rápido e só travar no 422 (atrito ruim para a recepção).

- **D3 — Texto exato das tags.** **Assunção adotada:** "Idoso" e "Menor de idade". Ajustável sem reabrir o briefing (é label de UI).

- **D4 — Coexistência com a tag manual "idoso" do catálogo.** **Assunção adotada:** as duas coexistem; a automática é separada da manual (CA22). Não removemos a tag manual "idoso" do `pacienteTags.ts` nesta entrega (mudança de catálogo seria scope creep e pode ter uso por filtros). Se o usuário quiser unificar/remover a manual, é demanda separada.
