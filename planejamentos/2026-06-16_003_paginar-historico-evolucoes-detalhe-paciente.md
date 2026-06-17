# Paginar histórico de evoluções na aba "Prontuário" do detalhe do paciente

**ID**: 2026-06-16_003
**Status**: Aprovado por usuário em 2026-06-16
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P
**Áreas regressivas tocadas**: prontuário (aba Prontuário + aba Anamnese da view de detalhe do paciente)

> **Entrega 100% frontend.** Sem mudança de backend, handler, query, repositório, contrato de endpoint ou schema. **Não aciona `imedto-database`** — nenhuma migration. Todo o backend reutilizado já existe e já é usado em produção pela aba "Consultas anteriores" (`ConsultasAnterioresTab.vue`). O trabalho do dev é só na camada Vue de `PacienteDetalheView.vue`.

---

## 1. Contexto e motivação

A seção **"Histórico de evoluções"** (aba "prontuário" da view de detalhe do paciente, `frontend/src/views/pacientes/PacienteDetalheView.vue`, timeline por volta das linhas 732–770) renderiza hoje `EvolucaoTimelineItem` num `v-for` direto sobre `prontuario.evolucoes`, **sem paginação**.

Essa lista vem de `prontuarioService.obter(pacienteId)` → `GET /paciente/{id}/prontuario`, cujo payload (`ProntuarioCompleto.evolucoes`) tem **cap de ~50 evoluções** e **trunca silenciosamente**: um paciente com 60+ evoluções nunca vê as mais antigas, e nem sinaliza que há mais. Para paciente de acompanhamento longo (cirúrgico, crônico), isso é perda de histórico clínico na tela — atrito real para o profissional.

A aba **"Consultas anteriores"** (`frontend/src/components/prontuario/tabs/ConsultasAnterioresTab.vue`) **já resolveu exatamente isso**: usa `prontuarioService.listarEvolucoes(pacienteId, { pagina, tamanho })` → `GET /paciente/{id}/prontuario/evolucoes` (server-side LIMIT/OFFSET) + `AppPagination`. Esta demanda **espelha esse padrão já validado** na seção "Histórico de evoluções" do detalhe do paciente. Não é feature nova — é estender um padrão existente a uma segunda tela que ficou para trás.

> Não confundir: a aba "Consultas anteriores" **já está paginada e está FORA de escopo**. Ela é apenas a REFERÊNCIA de padrão a copiar.

## 2. Persona-alvo

Profissional de saúde (e, conforme RBAC já vigente, Dono/admin) consultando o histórico clínico de um paciente na aba "Prontuário" do detalhe do paciente — tipicamente antes/durante um atendimento de retorno. Frequência: a cada atendimento de paciente recorrente. Mais sensível em pacientes com histórico longo (> 50 evoluções), que hoje têm parte do histórico invisível.

## 3. Escopo

**Inclui**:
- Trocar a fonte de dados da timeline da seção "Histórico de evoluções" (aba `prontuario`) de `prontuario.evolucoes` (capada em 50) para `prontuarioService.listarEvolucoes(pacienteId, { pagina, tamanho })` (paginada server-side).
- Adicionar `AppPagination` após a timeline, espelhando o uso de `ConsultasAnterioresTab.vue` (`v-model:pagina`, `v-model:tamanho`, `:total`, `rotulo-itens="evolução(ões)"`).
- Estado local de paginação (`pagina`, `tamanho`, `total`, `carregando`, `erro`) + `watch([pagina, tamanho], carregar)`, mirroring o padrão.
- Tamanho de página padrão: **10 por página** (consistente com a aba "Consultas anteriores").
- Destaque "Mais recente" passa a valer só na página 1 (`pagina === 1 && evo.id === idMaisRecente`).
- Subtítulo/contagem da seção passa a usar o `total` real do endpoint paginado, não mais `prontuario.evolucoes.length`.
- Estados loading / erro / vazio da própria seção paginada.

**Não inclui**:
- A aba "Consultas anteriores" (`ConsultasAnterioresTab.vue`) — já paginada, intocada.
- Qualquer mudança em backend, handler, query, repositório, contrato de endpoint ou schema.
- A aba **Anamnese** continua consumindo `prontuario.evolucoes` (via `evolucaoMaisAntiga`) — **não migra** (ver R3).
- Os alertas do paciente, dados do prontuário, contagem na badge da tab e no resumo (`contarEvolucoes`/`totalProntuarios`) — permanecem como estão.
- Registro/edição de evolução inline (não existe nesta view) → sem reset de paginação (ver R6).
- Mudança de `prontuarioService.obter()` — permanece alimentando o resto da view.

## 4. Regras de negócio

- **R1 — Fonte da timeline migra para o endpoint paginado.** A lista de evoluções renderizada na seção "Histórico de evoluções" (aba `prontuario`) passa a vir de `prontuarioService.listarEvolucoes(pacienteId, { pagina, tamanho })` (`GET /paciente/{id}/prontuario/evolucoes` → `PaginaEvolucoes { itens, total, pagina, tamanhoPagina }`). Mora em: Front (lógica de fetch da view) + Backend já existente (handler `ListarEvolucoesProntuarioPacienteQueryHandlers`, repo `ProntuarioQueryRepository`). Validada em: front (UX) + back (já é a fonte da verdade — sem alteração).

- **R2 — Tamanho de página padrão 10.** Estado local `pagina = ref(1)`, `tamanho = ref(10)`, espelhando `ConsultasAnterioresTab.vue`. Mora em: Front. O backend clampa `tamanho` em 1–100 (default 20 se omitido) — o front sempre envia 10, então o clamp não muda nada; registrar só como contexto.

- **R3 — `prontuarioService.obter()` PERMANECE.** A chamada a `obter()` continua existindo e continua alimentando o restante da view: a **aba Anamnese** depende de `prontuario.evolucoes` via `evolucaoMaisAntiga` (computed que ordena por `criadaEm` ASC e pega a primeira evolução), além dos alertas e dos dados do prontuário (`ProntuarioResumo`). **Somente a lista de evoluções da timeline da aba `prontuario` migra para o endpoint paginado.** Mora em: Front. **Risco de regressão explícito** — ver §8 e CA8.

- **R4 — Destaque "Mais recente" só na página 1.** Replica a regra de `ConsultasAnterioresTab.vue`: `idMaisRecente = computed(() => itensDaPagina.value[0]?.id ?? null)` e `:destaque="pagina === 1 && evo.id === idMaisRecente"`. O backend ordena `criada_em DESC`, então o primeiro item da página 1 é a evolução mais recente; o destaque some a partir da página 2. Mora em: Front. (Substitui o atual `idEvolucaoMaisRecente`, que ordena sobre a lista capada de `prontuario.evolucoes`.)

- **R5 — Contagem real no subtítulo.** O subtítulo "Linha do tempo cronológica das evoluções clínicas registradas" e qualquer contagem dentro da seção paginada passam a refletir o `total` retornado pelo endpoint paginado (contagem real), não mais `prontuario.evolucoes.length` (capado em 50). A badge da tab e o resumo, que já usam `contarEvolucoes`/`totalProntuarios` (contagem real, via `/contagem-evolucoes`), **permanecem como estão**. Mora em: Front.

- **R6 — Sem reset de paginação por criação/edição.** Esta view **não registra nem edita evolução inline** — o botão "Abrir prontuário completo" navega para fora (`abrirProntuario`). Logo, não há evento de criação/edição de evolução a tratar aqui, e **não há CA de reset de paginação**. Mora em: N/A (premissa registrada).

- **R7 — Multi-tenant e audit LGPD preservados pelo backend.** O endpoint `GET /paciente/{id}/prontuario/evolucoes` já filtra `estabelecimento_id` (repo falha-fechada) e já registra audit de leitura de prontuário via `IProntuarioAcessoLogService`. A migração **não altera** esse comportamento; os CAs apenas verificam que ele se mantém após a troca de fonte de dados. Mora em: Backend (já existente). Validada em: back (premissa não-negociável já implementada).

- **R8 — Performance/foco: seção não visível não consulta.** A aba `prontuario` já é carregada sob demanda: `watch(aba, ...)` chama `carregarProntuario()` só quando `aba === 'prontuario' || aba === 'anamnese'`, com guarda `abasCarregadas`. A nova carga paginada deve respeitar esse princípio — **não disparar `listarEvolucoes` enquanto a aba `prontuario` não estiver visível**. A carga paginada acontece na entrada da seção (ou na primeira vez que `aba === 'prontuario'`), nunca no `onMounted` global da view nem em aba não clicada. Mora em: Front.

## 5. Modelo de dados

**Nenhuma mudança.** Sem tabela, coluna, índice, migration ou ALTER. Todo o backend já existe:
- Endpoint: `GET /paciente/{id}/prontuario/evolucoes?pagina&tamanho`.
- Handler: `ListarEvolucoesProntuarioPacienteQueryHandlers` (clampa `tamanho` 1–100, default 20).
- Repo: `ProntuarioQueryRepository` — `ORDER BY criada_em DESC`, `LIMIT/OFFSET`, filtro `estabelecimento_id`, audit LGPD via `IProntuarioAcessoLogService`.
- Service front: `prontuarioService.listarEvolucoes(pacienteId, { pagina, tamanho })` retornando `PaginaEvolucoes`.

Tudo isso já é consumido em produção por `ConsultasAnterioresTab.vue`.

## 6. UX e fluxo

Wireframe textual da seção "Histórico de evoluções" (aba `prontuario`), depois da mudança:

```
┌─ Histórico de evoluções ─────────────────────────────────┐
│  Linha do tempo cronológica das evoluções clínicas       │
│  registradas.                          [Abrir prontuário] │
│                                                           │
│  • Carregando…                          (estado loading)  │
│  ─ ou ─                                                   │
│  ⚠ <mensagem de erro genérica>          (estado erro)     │
│  ─ ou ─                                                   │
│  [AppEmptyState: "Sem evoluções registradas"] (vazio)     │
│  ─ ou ─                                                   │
│  ◉ Evolução 2026-06-15  [Mais recente]  ← só na página 1  │
│  ○ Evolução 2026-05-30                                    │
│  ○ Evolução 2026-05-10                                    │
│  ... (até 10 por página)                                  │
│                                                           │
│  [AppPagination ‹ 1 2 3 … › | tamanho]  total real        │
└───────────────────────────────────────────────────────────┘
```

- **Componente de design system reutilizado**: `AppPagination` (já importado em `ConsultasAnterioresTab.vue` via `@/components/ui`), com as mesmas props: `v-model:pagina`, `v-model:tamanho`, `:total`, `rotulo-itens="evolução(ões)"`. Reutiliza também `EvolucaoTimelineItem`, `AppEmptyState`, `AppButton`, já presentes na view.
- **Estados**:
  - *loading*: enquanto `carregando` da carga paginada — mensagem "Carregando…" (mesma classe `.msg-info` já usada na seção).
  - *erro*: mensagem genérica (sem PII), padrão `e?.response?.data?.mensagem ?? "Erro ao carregar evoluções."`.
  - *vazio*: `total === 0` → `AppEmptyState` existente ("Sem evoluções registradas" + CTA "Abrir prontuário").
  - *sucesso*: timeline + `AppPagination`.
- **Troca de página**: clicar em outra página dispara `watch([pagina, tamanho])` → `carregar()` → nova request fatiada; o destaque "Mais recente" some a partir da página 2.
- **Mobile-ready**: nada novo de layout — `AppPagination` e a timeline já são responsivos na aba "Consultas anteriores".

## 7. Critérios de aceite (testáveis)

- **CA1 (paginação funcional / caminho feliz)**: Dado um paciente com mais de 10 evoluções e o usuário na aba "Prontuário", Quando a seção "Histórico de evoluções" carrega, Então exibe no máximo 10 itens (`EvolucaoTimelineItem`) e renderiza o `AppPagination` abaixo da timeline com o `total` real (> 10).

- **CA2 (troca de página)**: Dado o usuário na página 1 da timeline, Quando clica na página 2 no `AppPagination`, Então uma nova request `GET /paciente/{id}/prontuario/evolucoes?pagina=2&tamanho=10` é disparada e a timeline passa a exibir as evoluções da página 2 (sem recarregar a view inteira).

- **CA3 (histórico longo deixa de truncar)**: Dado um paciente com mais de 50 evoluções, Quando o usuário pagina até a última página, Então consegue visualizar evoluções além da 50ª (que antes eram silenciosamente cortadas por `prontuario.evolucoes`), e o `total` do `AppPagination` reflete a contagem real.

- **CA4 (estado loading)**: Dado que a request paginada está em andamento, Quando a seção "Histórico de evoluções" é exibida, Então mostra o indicador de carregamento ("Carregando…") e não a timeline nem o empty state.

- **CA5 (estado erro)**: Dado que o backend retorna erro na carga das evoluções, Quando a seção tenta carregar, Então exibe uma mensagem de erro genérica (sem PII do paciente) e não quebra o restante da view.

- **CA6 (estado vazio)**: Dado um paciente sem evoluções (`total === 0`), Quando a seção "Histórico de evoluções" carrega, Então mostra o `AppEmptyState` ("Sem evoluções registradas") com o CTA "Abrir prontuário", e o `AppPagination` não é renderizado.

- **CA7 (destaque só na página 1)**: Dado que a timeline está na página 1, Quando renderiza, Então apenas a evolução mais recente (primeiro item, ordenado `criada_em DESC` pelo backend) recebe o destaque "Mais recente"; Quando o usuário vai para a página 2 ou seguinte, Então nenhum item recebe o destaque "Mais recente".

- **CA8 (não-regressão Anamnese + alertas)**: Dado o mesmo paciente, Quando o usuário abre a aba "Anamnese", Então a anamnese (primeira evolução via `evolucaoMaisAntiga` / `secoesAnamnese`) continua sendo exibida corretamente; E Quando o usuário abre a aba "Resumo"/cabeçalho, Então os alertas do paciente e os dados do prontuário continuam carregando normalmente. (Confirma que `prontuarioService.obter()` permanece intacto após a migração da timeline.)

- **CA9 (contagem real no subtítulo)**: Dado um paciente com mais de 50 evoluções, Quando a seção "Histórico de evoluções" carrega, Então a contagem exibida na seção reflete o `total` real do endpoint paginado (> 50), não o valor capado de `prontuario.evolucoes.length`. (A badge da tab e o resumo, que já usam `contarEvolucoes`, permanecem inalterados.)

- **CA10 (multi-tenant)**: Dado um usuário do estabelecimento B, Quando tenta carregar as evoluções de um paciente do estabelecimento A pela timeline paginada, Então o backend retorna vazio/"não encontrado" genérico (filtro `estabelecimento_id` já vigente) e nenhum dado do tenant A é exibido nem logado com PII.

- **CA11 (audit LGPD por carga de página)**: Dado o usuário paginando o histórico de evoluções, Quando cada página é carregada via `GET /paciente/{id}/prontuario/evolucoes`, Então o backend registra a linha de audit de acesso ao prontuário (`IProntuarioAcessoLogService`) — comportamento já existente, verificado como preservado após a migração.

- **CA12 (performance / aba não visível não consulta)**: Dado o usuário na aba "Resumo" (aba `prontuario` ainda não aberta), Quando a view do detalhe do paciente carrega, Então **nenhuma** request `GET /paciente/{id}/prontuario/evolucoes` é disparada; a primeira request paginada só ocorre quando a aba "Prontuário" passa a ser visível.

## 8. Riscos e dependências

- **Risco de regressão na aba Anamnese e nos alertas** (ver R3): a migração da timeline NÃO pode remover nem alterar a chamada `prontuarioService.obter()`, pois `evolucaoMaisAntiga`/`secoesAnamnese` (Anamnese), os alertas do paciente e os dados do prontuário dependem dela. O dev deve manter `obter()` viva e migrar APENAS a fonte da timeline. QA valida explicitamente via CA8.
- **Risco de duplicidade de estado**: cuidado para não criar um segundo "idEvolucaoMaisRecente" que ainda olhe `prontuario.evolucoes`. O destaque deve passar a derivar dos itens da página paginada (R4). Se o `idEvolucaoMaisRecente` atual (linhas 326–333) ficar órfão após a migração, removê-lo conforme regra de "remover orphans que SUAS mudanças criaram" (CLAUDE.md §3).
- **Dependência**: nenhuma externa. Backend e service já prontos; padrão de referência já em produção (`ConsultasAnterioresTab.vue`).

## 9. Observações para execução

- **Não-negociável**: zero mudança de backend/contrato/schema; manter `prontuarioService.obter()` viva para Anamnese + alertas; multi-tenant e audit LGPD ficam por conta do backend já existente (não reimplementar no front).
- **Reuso obrigatório**: espelhar fielmente o estado e o template de `frontend/src/components/prontuario/tabs/ConsultasAnterioresTab.vue` (refs `pagina/tamanho/total/carregando/erro`, `watch([pagina, tamanho], carregar)`, `idMaisRecente = itens[0]?.id`, `:destaque="pagina === 1 && evo.id === idMaisRecente"`, `<AppPagination v-model:pagina v-model:tamanho :total rotulo-itens="evolução(ões)" />`). Não inventar componente novo nem nova chamada de service.
- **Liberdade técnica do dev**: decidir se a carga paginada vira uma `function carregarEvolucoesTimeline()` separada chamada a partir de `carregarProntuario()`/`watch(aba)`, ou se reaproveita a estrutura de guarda `abasCarregadas`. O essencial é respeitar R8/CA12 (não consultar em aba não visível) e não disparar no `onMounted` global.
- **Tipografia**: seguir CLAUDE.md §5 — usar tokens, nunca `font-size`/`font-weight` literais em CSS novo. Se possível, reutilizar as classes/estilos já existentes da seção e do `AppPagination` (idealmente sem CSS novo).
- **Atenção ao campo de data**: nesta view o tipo `Evolucao` usa `criadaEm`; o backend ordena por `criada_em DESC`, então o primeiro item da página já é o mais recente — o destaque pode derivar de `itens[0].id` sem reordenar no front.

## 10. Atualização de documentação

**Nenhum doc em `Docs/` precisa ser atualizado.** A demanda segue padrões já documentados e já em produção (paginação server-side + `AppPagination`, reuso de endpoint/service existentes). Não há novo componente de design system, novo padrão de service/store, nova regra cross-cutting, nem mudança de arquitetura/infra/LGPD. É reuso de padrão existente numa segunda tela.
