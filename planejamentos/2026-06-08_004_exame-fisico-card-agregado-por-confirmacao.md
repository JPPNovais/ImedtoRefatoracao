# Exame Físico — 1 card agregado por confirmação da modal de regiões

**ID**: 2026-06-08_004
**Status**: Aprovado por usuário em 2026-06-08
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (seção Exame Físico), relatório/PDF de exame físico, mapa corporal

## 1. Contexto e motivação

Na seção Exame Físico da evolução (prontuário), o profissional clica numa região do mapa corporal e a modal `RegionSelectorPopup` permite marcar várias sub-partes de uma vez antes de confirmar. Hoje **cada sub-parte marcada vira um card "região examinada" separado** (`onConfirmarRegioes` faz `push` de 1 `RegiaoAnatomicaSelecionada` por seleção). Resultado: examinar 5 sub-partes do membro superior gera 5 cards, cada um com seu template, poluindo a tela e fragmentando o raciocínio clínico que, na prática, é único para o conjunto.

O PO quer que **cada confirmação da modal produza 1 único card**, agregando o conteúdo base (`template_texto`) de todas as sub-partes marcadas no campo "Exame" — uma sub-parte por linha. Reabrir a modal e confirmar de novo cria um **novo** card (mesmo que repita sub-partes já examinadas em card anterior).

Evidência de código (estado atual):
- `SecaoExameFisico.vue` `onConfirmarRegioes` (~L280-301): loop com `push` por seleção + dedupe por `regiao_id + lateralidade` entre cards (L285).
- `RegionExamCard.vue` (L62): título do card = `regiao.caminho`.
- `ProntuarioView.vue` (L213-223) + `exameFisicoService.ts` (`RegistrarExameFisicoInput`, L121-133): o payload enviado ao backend é `{ codigo, lateralidade, achados, severidade, ordem }`. **`texto_exame` nunca é persistido hoje** — não há campo no contrato de persistência para o texto do exame.

## 2. Persona-alvo

Profissional de saúde (médico/fisioterapeuta), durante o atendimento, preenchendo o exame físico estruturado da evolução. Frequência: a cada consulta que usa exame físico com mapa corporal.

## 3. Escopo

**Inclui**:
- Alterar `onConfirmarRegioes` em `SecaoExameFisico.vue` para que cada confirmação da modal gere **1 único** `RegiaoAnatomicaSelecionada` (card) agregando os templates das sub-partes marcadas.
- Definir o `regiao_id`, `caminho` (título), `lateralidade` e `texto_exame` do card agregado de forma coerente (ver R2-R6).
- Remover o dedupe entre cards por `regiao_id + lateralidade` (decisão 5b). Manter dedupe **intra-confirmação** (mesma sub-parte marcada não duplica linha).
- Badge de lateralidade do card: "Vários lados" quando as sub-partes confirmadas têm lados diferentes; D/E/Bilateral quando todas têm o mesmo lado.
- Garantir que o comportamento vale para **todas** as regiões (membro e não-membro).

**Não inclui**:
- Persistir `texto_exame` no backend. Hoje não é persistido e **continua não sendo** nesta entrega (mantém contrato atual de `registrar`). O texto agregado vive no JSON da evolução (`conteudo["exame-fisico"].regioes[].texto_exame`), que já é salvo via `registrarEvolucao`.
- Mudança no catálogo de regiões anatômicas, no `BodyMap`, ou na modal `RegionSelectorPopup` (o contrato de `emit('confirmar', [...])` permanece o mesmo).
- Edição/split de um card agregado de volta em sub-partes individuais.
- Qualquer alteração de schema / migration.

## 4. Regras de negócio

> Toda a feature é **frontend-only** (camada de composição do v-model da evolução). Não há regra nova no backend porque o agregado já é texto livre dentro do JSON da evolução, que o backend persiste como blob de conteúdo sem interpretar. O contrato de `exameFisicoService.registrar` (regiões estruturadas) **não muda**.

- **R1 — 1 confirmação = 1 card**: ao receber `@confirmar` com N seleções, `onConfirmarRegioes` cria exatamente **1** novo `RegiaoAnatomicaSelecionada` e o adiciona ao array `regioes` (append, nunca substitui cards anteriores). Mora em: `SecaoExameFisico.vue onConfirmarRegioes`. Validada em: front (não há espelho back — o backend não agrega).

- **R2 — texto_exame agregado**: `texto_exame` do card = concatenação dos `template_texto` (via `getTemplate(regiaoId)`) de cada sub-parte marcada, **uma por linha**, separadas por `\n`, **sem prefixo de nome e sem markdown** (texto puro). Os `template_texto` do seed já começam com o nome da parte (ex.: "Braço sem edema ou lesões. Musculatura com tônus preservado.") — prefixar duplicaria o nome; além disso o campo "Exame" é um `<textarea>` que não renderiza markdown, então `**` apareceria literal. **Ordem** = ordem em que vêm no array `selecoes` recebido da modal (que segue a ordem de marcação do usuário / ordem do catálogo conforme `getFilhos` ordena por `ordem`). Sem linha em branco entre itens. Mora em: `SecaoExameFisico.vue`. Validada em: front.

- **R3 — regiao_id e caminho do card (coerência com título = região pai, decisão 3b)**: o card agregado representa o **conjunto**, então:
  - `regiao_id` = id do **nó pai clicado** que abriu a modal, ou seja o nível-1 (`baseAtiva`) para membros e a `regiaoClicada` para não-membros. Em código: é a `regiaoAtual`/raiz do popup no momento da confirmação. Como `SecaoExameFisico` não recebe esse nó diretamente no `@confirmar`, ele deve derivá-lo do **ancestral comum** das sub-partes: usar `getAncestorNivel1Id(primeiraSelecao)` quando for membro; para não-membro, usar o `pai_id` comum das seleções (ou a própria região se for nível-1). Regra determinística: **`regiao_id` do card = ancestral comum mais profundo das sub-partes marcadas; se as sub-partes forem elas mesmas a região clicada (caso "geral"), usar o id dela**. Validada em: front.
  - `caminho` = `getCaminho(regiao_id)` desse nó pai. Quando o card tiver lateralidade resolvida como "Vários lados" ou "bilateral", aplicar `caminhoNeutro(caminho)` para não fixar lado no título. Quando todas as sub-partes forem do mesmo lado D ou E, manter o caminho com o lado (comportamento atual de `getCaminho`).
  - Mora em: `SecaoExameFisico.vue`. Validada em: front.

- **R4 — highlight do mapa corporal**: o `BodyMap` acende a região via `regioesExaminadasMapa`, derivado do `regiao_id` de cada card + seu ancestral nível-1 (`getAncestorNivel1Id`). Como o card agregado carrega `regiao_id = nó pai`, o mapa **acende a região representante (o nó pai / membro)**. Consequência documentada e aceita pelo PO (decisão 1b/2): o destaque é por região-pai, não mais por sub-parte individual. A lógica bilateral existente (acender membro oposto quando `lateralidade === 'bilateral'`) permanece. Para o card com lateralidade "Vários lados", ver R6. Mora em: `SecaoExameFisico.vue` (computeds `regioesJaSelecionadas` / `regioesExaminadasMapa`). Validada em: front.

- **R5 — dedupe**:
  - **Intra-confirmação**: dentro do mesmo array `selecoes`, a mesma sub-parte (`regiaoId` + `lateralidade` iguais) entra **uma única vez** na agregação (não duplica linha no `texto_exame`).
  - **Entre confirmações**: **não há** dedupe. Reabrir a modal e confirmar sempre cria um card novo, mesmo repetindo sub-partes de um card anterior. Remover a checagem `jaExiste` (L285) que hoje pula seleções já presentes em outros cards.
  - Mora em: `SecaoExameFisico.vue onConfirmarRegioes`. Validada em: front.

- **R6 — lateralidade do card (decisão 4a)**: SEMPRE 1 card por confirmação, mesmo misturando lados. A `lateralidade` do card é resolvida a partir das lateralidades das sub-partes marcadas:
  - Todas iguais a `D` → card `lateralidade = "D"` (badge "Direito").
  - Todas iguais a `E` → card `lateralidade = "E"` (badge "Esquerdo").
  - Todas iguais a `bilateral` → card `lateralidade = "bilateral"` (badge "Bilateral").
  - Lados **diferentes** entre as sub-partes (ex.: uma `D` e outra `E`, ou mistura com `bilateral`) → card recebe um valor especial que aciona a badge **"Vários lados"**.
  - Todas `null` (região não-lateral sem lado escolhido) → `lateralidade = null` (sem badge, como hoje).
  - Mora em: `SecaoExameFisico.vue` (resolução) + `RegionExamCard.vue` (`getLateralidadeLabel`). Validada em: front.

- **R7 — texto exato da badge "Vários lados"**: a badge exibe literalmente `Vários lados`. Implementação: introduzir um valor de lateralidade `"misto"` no tipo local `RegiaoAnatomicaSelecionada.lateralidade` (`"D" | "E" | "bilateral" | "misto" | null`) e mapear em `getLateralidadeLabel` (`misto` → `"Vários lados"`). Como `texto_exame`/`achados` continuam por card e o backend só recebe regiões estruturadas, o valor `"misto"` **não é enviado ao backend** (ver R8). Mora em: `RegionExamCard.vue` + tipo em `SecaoExameFisico.vue`. Validada em: front.

- **R8 — compatibilidade com persistência estruturada (backend) — decisão 1b**: o array de regiões enviado ao backend via `exameFisicoService.registrar` passa a ter **1 item por card** (já era 1 item por entrada; agora há menos entradas porque cada confirmação vira 1 card). O item leva `codigo = regiao_id` (nó pai), `lateralidade` mapeada e `achados`. Para `lateralidade = "misto"`, o mapeamento `LATERALIDADE_LOCAL_PARA_BACKEND` deve enviar `null` (o backend não tem conceito de "vários lados"; o detalhe vive só no texto do card no JSON da evolução). PO aceitou que mapa/relatórios estruturados perdem o detalhe por sub-parte — o card representa o conjunto. Mora em: `ProntuarioView.vue` (mapeamento) + `exameFisicoService.ts`. Validada em: front.

## 5. Modelo de dados

**Sem alteração de schema. Sem migration.**

- O agregado textual vive em `conteudo["exame-fisico"].regioes[].texto_exame` dentro do JSON da evolução já persistido por `prontuarioService.registrarEvolucao` (sem mudança de contrato).
- O domínio estruturado `exame_fisicos` (via `POST /api/evolucoes/{id}/exame-fisico`) continua recebendo 1 linha por card com `{ codigo, lateralidade, achados, severidade, ordem }`. Menos linhas que antes; nenhuma coluna nova.
- Multi-tenant: inalterado — `registrar` e `registrarEvolucao` já filtram por estabelecimento/evolução no backend (auditoria multi-tenant concluída). Esta entrega não toca query/handler de backend.
- LGPD/audit: inalterado — exame físico é prontuário; o audit de acesso/escrita ao prontuário já existe e continua valendo. Nenhuma nova exposição de PII (o card agrega texto clínico que já era inserido manualmente sub-parte a sub-parte).

## 6. UX e fluxo

Fluxo:
1. Profissional clica numa região do mapa → abre `RegionSelectorPopup` (inalterado).
2. Marca N sub-partes (membro: escolhe lado primeiro; não-membro: lado por sub-parte). Confirma.
3. **1 card** "região examinada" aparece em "Regiões examinadas (N)", com:
   - Título (cabeçalho) = caminho da região pai (ex.: `Membro superior (anterior)`), neutralizado de lado quando lateralidade for "Vários lados"/"Bilateral".
   - Badge de lateralidade conforme R6/R7 (`Direito` / `Esquerdo` / `Bilateral` / `Vários lados`, ou nenhuma).
   - Campo "Exame" pré-preenchido com a concatenação dos `template_texto`, **uma sub-parte por linha**, texto puro (sem prefixo de nome e sem markdown — os templates do seed já contêm o nome da parte).
   - Campos "Achados" e "Observações" vazios (como hoje).
4. Reabrir a modal e confirmar de novo → **novo** card, abaixo do anterior.
5. Mapa corporal acende a região pai do card (R4).

Estados:
- **Nenhuma sub-parte marcada**: o botão Confirmar já fica desabilitado (`:disabled="totalSelecionados === 0"`). Logo `onConfirmarRegioes` não é chamado com array vazio. Defensivamente: se `selecoes` chegar vazio, **não cria card** (early return).
- **Lista vazia de regiões examinadas**: a subseção "Regiões examinadas" não renderiza (`v-if="regioes.length > 0"`), comportamento atual mantido.
- **Erro de carregamento do catálogo**: inalterado (hint de erro no título do mapa).

Componentes do design system: nenhum componente novo. Reuso de `RegionExamCard`, `RegionSelectorPopup`, `BodyMap`, `AppField`. Mobile: inalterado (sem novo layout).

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — 1 confirmação = 1 card)**: Dado o catálogo carregado e a modal aberta numa região, Quando o profissional marca 3 sub-partes e clica Confirmar, Então **exatamente 1** card novo é adicionado em "Regiões examinadas" e o contador do título incrementa em 1.

- **CA2 (texto agregado correto)**: Dado que 3 sub-partes (S1, S2, S3) foram marcadas, Quando o card é criado, Então o campo "Exame" contém 3 linhas, cada uma sendo o `template_texto` puro da sub-parte (sem prefixo de nome e sem `**`/markdown), uma por sub-parte, na ordem do array de seleções, separadas por `\n`, sem linha em branco entre elas.

- **CA3 (reabrir = novo card)**: Dado que já existe 1 card de uma confirmação, Quando o profissional reabre a modal, marca sub-partes (mesmo repetindo as do card anterior) e confirma, Então um **segundo** card é criado abaixo do primeiro, e o primeiro permanece inalterado.

- **CA4 (dedupe intra-confirmação)**: Dado que a mesma sub-parte é alcançável duas vezes na mesma confirmação (mesmo `regiaoId` + mesma `lateralidade`), Quando confirma, Então o `texto_exame` do card contém aquela sub-parte **uma única vez** (uma linha só).

- **CA5 (badge — mesmo lado)**: Dado que todas as sub-partes marcadas são do lado Direito, Quando o card é criado, Então a badge exibe `Direito`. (Idem `Esquerdo` para todas E; `Bilateral` para todas bilateral.)

- **CA6 (badge — lados mistos)**: Dado que as sub-partes marcadas têm lados diferentes (ex.: uma D e outra E, ou D + bilateral), Quando o card é criado, Então a badge exibe exatamente `Vários lados`.

- **CA7 (badge — sem lado)**: Dado que as sub-partes são de região não-lateral e nenhum lado foi escolhido, Quando o card é criado, Então **nenhuma** badge de lateralidade é exibida.

- **CA8 (título = região pai)**: Dado um card de membro superior anterior com sub-partes (ombro, braço), Quando o card é criado, Então o título do cabeçalho é o caminho da **região pai** (ex.: `Membro superior (anterior)`), e não o caminho de uma sub-parte; quando a lateralidade for `Vários lados`/`Bilateral`, o título é neutralizado de "direito/esquerdo".

- **CA9 (highlight do mapa)**: Dado um card agregado criado a partir do membro superior, Quando o card existe, Então o `BodyMap` acende a região pai correspondente (membro superior); para lateralidade bilateral, acende ambos os membros (comportamento existente mantido).

- **CA10 (estado vazio — nenhuma sub-parte)**: Dado que nenhuma sub-parte está marcada, Quando o profissional olha o rodapé da modal, Então o botão Confirmar está desabilitado e nenhum card é criado.

- **CA11 (persistência 1b)**: Dado um card agregado salvo via "Salvar evolução", Quando a requisição `POST /api/evolucoes/{id}/exame-fisico` é montada, Então o array `regioes` contém **1 item por card** com `codigo = regiao_id` (nó pai) e `lateralidade` mapeada; para card de lados mistos (`misto`), `lateralidade` enviada é `null`.

- **CA12 (multi-tenant)**: Dado um usuário do estabelecimento B, Quando tenta registrar exame físico de evolução do estabelecimento A, Então recebe erro genérico de "não encontrado" e nada é gravado (comportamento já existente do backend, validado por não-regressão — esta entrega não altera o handler).

- **CA13 (LGPD/audit)**: Dado o registro do exame físico, Quando ocorre, Então o audit de prontuário existente continua sendo gerado e nenhuma PII nova aparece em log ou mensagem de erro (não-regressão — sem novo endpoint/DTO expondo PII).

- **CA14 (não-regressão — região não-membro)**: Dado uma região não-membro (ex.: cabeça/tórax) com sub-partes lateralizadas individualmente, Quando confirma marcando sub-partes com lados diferentes, Então 1 único card é criado com badge `Vários lados` e texto agregado correto — a feature **não** se limita a membros.

## 8. Riscos e dependências

- **PDF/relatório de exame físico** (`usePdfHeader.ts` e helpers de exame físico): o relatório lê `regioes` estruturadas do backend. Como cada card agora é 1 linha estruturada com `regiao_id = nó pai`, o relatório passará a mostrar a região pai (não sub-partes). Risco de regressão visual no PDF — QA deve validar que o PDF não quebra e que o texto agregado (se exibido a partir do JSON da evolução) aparece coerente. Aceito pelo PO (decisão 1b).
- **Mapa corporal**: highlight muda de sub-parte para região pai. Aceito (R4), mas validar que não há sub-parte "órfã" que deixe de acender quando esperado.
- **Tipo `lateralidade` ganha `"misto"`**: garantir que todos os pontos que fazem `switch`/lookup sobre lateralidade (`getLateralidadeLabel`, `LATERALIDADE_LOCAL_PARA_BACKEND`, lógica bilateral do mapa) tratem o novo valor sem quebrar (mapa: `misto` não dispara espelhamento bilateral).
- Dependência: nenhuma feature externa bloqueante.

## 9. Observações para execução

**Não-negociável**:
- Frontend-only. Não criar/alterar endpoint, handler, query ou migration. O backend já persiste o suficiente.
- `texto_exame` continua **não** sendo enviado ao backend (mantém o contrato atual de `RegistrarExameFisicoInput`). Não adicionar campo de texto ao payload nesta entrega.
- Remover o dedupe entre cards (L285 `jaExiste`), mas **manter** dedupe intra-confirmação.
- Manter imutabilidade do array de regiões no v-model (padrão atual `substituirRegioes`).

**Liberdade técnica do dev**:
- Como derivar o "ancestral comum" das sub-partes (R3) — pode reusar `getAncestorNivel1Id`/`getCaminho` existentes. Para o caso "geral" (a própria região clicada marcada), usar o id dela.
- Formato interno do valor `"misto"` (string literal no union de tipo é o sugerido).
- Onde montar a string agregada (helper local em `SecaoExameFisico.vue`).

**Reuso**: usar `getTemplate`, `getCaminho`, `caminhoNeutro`, `getAncestorNivel1Id` já existentes em `SecaoExameFisico.vue`. Não duplicar.

## 10. Atualização de documentação

Nenhum doc em `Docs/` precisa de atualização: a entrega não altera arquitetura, infra, design system (nenhum componente novo), nem contrato de persistência do backend (o array `regioes` mantém o mesmo shape; muda apenas a quantidade de itens). Não há novo tipo de PII nem novo endpoint — `Docs/LGPD.md` permanece válido.

Observação de backlog (não bloqueia esta entrega): o componente `AppPillToggle` usado no fluxo de membro ainda não consta em `Docs/DESIGN.md` (já registrado em memória do BA como pendência de doc) — fora do escopo desta demanda.
