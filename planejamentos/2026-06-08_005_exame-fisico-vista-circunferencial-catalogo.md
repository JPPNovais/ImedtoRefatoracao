# Exame Físico — vista circunferencial (catálogo + modal) — B1

**ID**: 2026-06-08_005
**Status**: Aprovado por usuário em 2026-06-08
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (seção Exame Físico), relatório/PDF de exame físico, mapa corporal, catálogo de regiões anatômicas (seed)

> **Escopo deste briefing**: APENAS o **B1** — introduzir a dimensão **vista circunferencial** como nó agregador no catálogo de regiões anatômicas e o passo de vista na modal de seleção. **NÃO** cobre highlight/fusão de polígonos no mapa corporal (B2, exige Discovery) nem qualquer outra etapa. B2/B3 ficam fora.
> **Relação com 2026-06-08_004 (IMUTÁVEL)**: este briefing **estende** o 004 introduzindo a **dimensão vista** (anterior/posterior/circunferencial) em paralelo à **dimensão lado** (D/E/bilateral/vários lados) que o 004 já estabeleceu. O 004 permanece intocado e válido; aqui referenciamos suas regras (1 confirmação = 1 card, badge de lado, agregação de templates) e somamos a vista.

## 1. Contexto e motivação

Hoje o catálogo de regiões anatômicas (`SeedsRegioesAnatomicas.cs` / tabela `regioes_anatomicas_catalogo`) modela cada região de nível-1 em **duas vistas independentes**: `{base}-anterior` e `{base}-posterior` (ex.: `torax-anterior`, `torax-posterior`). A modal `RegionSelectorPopup` abre numa vista fixa (a vista do nó clicado no `BodyMap`), e o profissional só consegue marcar sub-regiões daquela vista por vez.

Clinicamente, muitos exames são **circunferenciais**: o profissional examina a região "em volta" — anterior **e** posterior juntas — e raciocina sobre o conjunto (ex.: examinar o tórax inteiro, ou o membro superior direito por completo). Forçar duas confirmações separadas (uma anterior, uma posterior) fragmenta o raciocínio e gera dois cards quando deveria existir a opção de **um exame circunferencial único**.

O PO decidiu introduzir uma **terceira vista — circunferencial** — como **nó agregador** no catálogo: ao escolher a vista circunferencial de uma região, a modal lista de uma vez as sub-regiões das **duas** vistas (anterior + posterior), agrupadas, e a confirmação gera **1 card único** (coerente com o briefing 004).

Evidência de código (estado atual):
- Catálogo: `backend/src/Services/Imedto.Backend.Application/Catalogo/SeedsRegioesAnatomicas.cs` — nós nível-1 só têm vista `"anterior"` ou `"posterior"`. Tabela `regioes_anatomicas_catalogo` (global, sem `estabelecimento_id`).
- Frontend: `frontend/src/services/exameFisicoService.ts` mapeia `id = codigo` (slug) e tipa `vista: 'anterior' | 'posterior' | 'ambos' | null`.
- Modal: `frontend/src/components/exame-fisico/RegionSelectorPopup.vue` — fluxo de membro tem passo de **lado** (`'lado' | 'subregioes'`) via `AppPillToggle`; não-membro vai direto para sub-regiões. Resolução de filhos via prop `getFilhos(regiaoId)`.
- Seção: `frontend/src/components/prontuario/secoes/SecaoExameFisico.vue` — `getFilhos` filtra `pai_id === regiaoId` ordenado por `ordem`; `onRegiaoClicada` detecta membro por `MEMBRO_RE` sobre o nome do nó nível-1.
- Mapa: `frontend/src/components/exame-fisico/BodyMap.vue` — só renderiza hotspot para nó nível-1 cujo `nome` casa em `currentPaths[r.nome]` (path SVG).

## 2. Persona-alvo

Profissional de saúde (médico/fisioterapeuta), durante o atendimento, preenchendo o exame físico estruturado da evolução. Frequência: a cada consulta que usa exame físico com mapa corporal, sempre que quiser registrar um exame em volta da região (não só de uma face).

## 3. Escopo

**Inclui**:
- **Catálogo (seed)**: criar **9 nós agregadores nível-1** de vista circunferencial — um por região que tem par anterior/posterior + o abdome com pareamento clínico especial. Aceitar o novo valor `'circunferencial'` no campo `vista` do catálogo.
- **Modal (`RegionSelectorPopup.vue`)**: introduzir um passo de **vista** (anterior / posterior / circunferencial). Ordem dos passos:
  - **Membro**: lado (D/E/bilateral) → **vista** (ant/post/circ) → sub-regiões.
  - **Não-membro**: **vista** (ant/post/circ) → sub-regiões.
- **Resolução determinística dos filhos** no modo circunferencial: a lista exibida = `getFilhos("{base}-anterior")` **+** `getFilhos("{base}-posterior")`, agrupados sob cabeçalhos **"Anterior"** e **"Posterior"**. **Exceção do abdome**: `abdome-circunferencial` = `getFilhos("abdome-anterior")` + `getFilhos("lombossacra-posterior")`.
- **Card** (`SecaoExameFisico.vue` / `RegionExamCard.vue`): no modo circunferencial, a confirmação gera **1 card único** (coerente com 004) cujo `regiao_id` = o nó `{base}-circunferencial`. Card exibe **badge de vista** (Anterior / Posterior / Circunferencial) ao lado da badge de lado do 004 (Direito / Esquerdo / Bilateral / Vários lados).
- **Relatório/PDF**: ao ler um `regiao_id` cujo nó tem `vista = circunferencial`, reagrupar como "{Nome} (circunferencial)".

**Não inclui** (explicitamente fora — B2/B3 ou já resolvido no 004):
- **Highlight/fusão de polígonos no mapa corporal** para a vista circunferencial — é **B2**, exige Discovery. Nesta entrega o nó circunferencial **não tem path SVG** e **não renderiza hotspot** (ver R8). Documentado como dependência/risco para B2.
- Adicionar coluna `vista` à tabela `exame_fisico_regioes` ou campo `vista` ao payload de persistência. **O código do nó (`{base}-circunferencial`) resolve a vista** — não há nova coluna nem novo campo (ver §5).
- Qualquer alteração nas sub-regiões: elas mantêm seus códigos originais (com a vista embutida no próprio código, ex.: `coluna-lombar` continua sendo filha de `lombossacra-posterior`). O nó circunferencial **não duplica** sub-regiões.
- Edição/split de um card circunferencial de volta em anterior+posterior.
- Reabertura das regras do 004 (1 confirmação = 1 card, badge de lado, agregação de templates, dedupe) — permanecem como estão.

## 4. Regras de negócio

> A feature combina **uma mudança de catálogo (seed)** — responsabilidade do `imedto-database` — com **mudança de modal/card/relatório no frontend** — responsabilidade do `imedto-developer`. A camada de persistência estruturada (`exame_fisico_regioes`) **não muda**: o nó agregador resolve a vista pelo próprio código.

- **R1 — nó agregador circunferencial é nível-1, sem filhos próprios, sem path SVG**: cada nó `{base}-circunferencial` tem `codigo = "{base}-circunferencial"`, `nome = "{Nome} (circunferencial)"`, `vista = "circunferencial"`, `pai_codigo = NULL`, `nivel = 1`, `lateralidade = false`, **nenhum filho no catálogo** (não é pai de sub-região alguma). Mora em: catálogo (seed — `imedto-database`). Validada em: seed + leitura no front.

- **R2 — resolução determinística dos filhos no modo circunferencial**: dado um nó `{base}-circunferencial`, a modal NÃO chama `getFilhos("{base}-circunferencial")` (que retornaria vazio); em vez disso une, em ordem, `getFilhos("{base}-anterior")` seguido de `getFilhos("{base}-posterior")`, exibindo dois grupos com cabeçalhos **"Anterior"** e **"Posterior"**. **Exceção do abdome**: para `abdome-circunferencial`, une `getFilhos("abdome-anterior")` + `getFilhos("lombossacra-posterior")` (o "posterior" do abdome é a região lombossacra, por pareamento clínico). As sub-regiões mantêm seus `regiao_id` originais (a vista vive no código da sub-região). Mora em: `RegionSelectorPopup.vue` (resolução de filhos no modo circ) + helper em `SecaoExameFisico.vue`. Validada em: front. **Mapeamento `{base}-circunferencial → (ramoAnterior, ramoPosterior)` é determinístico e testável** (ver tabela em §5).

- **R3 — ordem dos passos da modal**:
  - **Membro** (nó nível-1 cujo nome casa `MEMBRO_RE`): passo 1 = **lado** (D / E / bilateral, via `AppPillToggle`, como no 004) → passo 2 = **vista** (Anterior / Posterior / Circunferencial) → passo 3 = **sub-regiões**.
  - **Não-membro**: passo 1 = **vista** (Anterior / Posterior / Circunferencial) → passo 2 = **sub-regiões**.
  - Mora em: `RegionSelectorPopup.vue` (`passo` ganha estado de vista). Validada em: front.

- **R4 — 1 confirmação = 1 card (herdado do 004) também no modo circunferencial**: no modo circunferencial, a lista é **única, agrupada por vista**; ao confirmar, gera-se **exatamente 1** card. Mora em: `SecaoExameFisico.vue onConfirmarRegioes` + `RegionSelectorPopup.vue confirmar`. Validada em: front. Não-regressão do 004.

- **R5 — `regiao_id` e `caminho` do card circunferencial**: quando a confirmação ocorre no modo circunferencial, o `regiao_id` do card = o nó `{base}-circunferencial` (o agregador), e `caminho`/título = `"{Nome} (circunferencial)"`. Para o caso membro, o lado escolhido (R3) entra na badge de lado do 004; quando "Vários lados"/"Bilateral", o título é neutralizado de "direito/esquerdo" (reuso de `caminhoNeutro`, padrão do 004). Mora em: `SecaoExameFisico.vue`. Validada em: front.

- **R6 — badge de vista no card**: o card exibe uma **badge de vista** com o texto exato conforme a vista resolvida: `Anterior`, `Posterior` ou `Circunferencial`. Essa badge é **independente e adicional** à badge de lado do 004 (`Direito` / `Esquerdo` / `Bilateral` / `Vários lados`). Card de **membro circunferencial** combina as duas dimensões: badge de lado (ex.: `Direito`) **+** badge de vista (`Circunferencial`) — coerente com o 004, **não conflita** (lado e vista são dimensões ortogonais). A vista é derivada do `regiao_id`: se o código termina em `-circunferencial` → `Circunferencial`; senão, do campo `vista` do nó (`anterior`/`posterior`). Mora em: `RegionExamCard.vue` (nova badge) + derivação em `SecaoExameFisico.vue`. Validada em: front.

- **R7 — persistência via nó agregador (sem nova coluna/campo)**: 1 card circunferencial = **1 linha** em `exame_fisico_regioes` com `regiao_codigo = "{base}-circunferencial"`. **NÃO** se adiciona coluna `vista` à tabela nem campo `vista` ao payload `RegistrarExameFisicoInput` — o código do nó já resolve a vista na leitura. O texto agregado (templates das sub-regiões das duas vistas, conforme regra de agregação do 004) vive no JSON da evolução (`conteudo["exame-fisico"].regioes[].texto_exame`), como no 004. Mora em: contrato existente (sem alteração) + `SecaoExameFisico.vue`/`ProntuarioView.vue` (montagem do payload). Validada em: front + leitura back (sem mudança de handler/query além de aceitar o novo `codigo`, que é dado de catálogo).

- **R8 — mapa corporal não quebra (B1 não acende circunferencial)**: o nó `{Nome} (circunferencial)` **não tem path SVG** — `currentPaths[r.nome]` não casa o nome → o `BodyMap` **não renderiza hotspot** para o nó circunferencial e **não quebra**. O highlight do card circunferencial no mapa (acender as duas faces / fundir polígonos) é **B2** e está **fora deste briefing**. A lógica de highlight existente (acender região pai / espelhamento bilateral do 004) permanece para cards anterior/posterior. Mora em: `BodyMap.vue` (sem alteração — comportamento por omissão) + `SecaoExameFisico.vue` (computeds de mapa ignoram o nó circ por não ter ancestral nível-1 com path). Validada em: front. **Risco/dependência declarado para B2.**

- **R9 — agregação de templates (herdado do 004, agora cruzando vistas)**: no modo circunferencial, o `texto_exame` do card concatena os `template_texto` das sub-regiões marcadas das **duas** vistas (anterior primeiro, posterior depois), uma por linha, texto puro (sem prefixo de nome, sem markdown), com dedupe intra-confirmação — exatamente as regras R2/R4/R5 do briefing 004, aplicadas ao conjunto unido. Mora em: `SecaoExameFisico.vue`. Validada em: front. Não-regressão do 004.

- **R10 — estado: vista não escolhida não avança**: enquanto o passo de vista não tiver seleção, a modal **não avança** para sub-regiões e o botão Confirmar permanece indisponível (não há sub-região para marcar). Mora em: `RegionSelectorPopup.vue`. Validada em: front.

## 5. Modelo de dados — exatamente o que o `imedto-database` fará

> **Esta entrega exige o `imedto-database` ANTES (ou junto, mas concluído antes do merge) do `imedto-developer`**: o frontend depende dos 9 nós agregadores já existirem no catálogo para a modal resolver a vista circunferencial. Anunciar e acionar o `imedto-database` primeiro.

**1) Aceitar o novo valor `'circunferencial'` no campo `vista` do catálogo de regiões anatômicas.**
- A tabela `regioes_anatomicas_catalogo` deve aceitar `vista = 'circunferencial'`. Se houver `CHECK`/enum/constraint que limite `vista` a `('anterior','posterior')`, ampliá-lo para incluir `'circunferencial'`. Se `vista` for texto livre sem constraint, nenhuma alteração de schema é necessária além dos INSERTs.
- **Validação de subgrupo (`vista = pai.vista`)**: os nós circunferenciais são **nível-1 com `pai_codigo = NULL`**, então **não disparam** a validação `vista = pai.vista` (documentada em `Docs/ARQUITETURA.md`). Confirmar que nenhuma regra de criação de **subgrupo** quebra — os circunferenciais não têm filhos.

**2) INSERTs idempotentes dos 9 nós agregadores (seed), com `ON CONFLICT (codigo) DO NOTHING`.**

Atualizar o seed canônico `SeedsRegioesAnatomicas.cs` (fonte de verdade portada para SQL) **e** gerar a migration SQL idempotente em `db/migrations/`. Os 9 nós:

| `codigo` | `nome` | `vista` | `pai_codigo` | `nivel` | `lateralidade` | ramo anterior (filhos via getFilhos) | ramo posterior (filhos via getFilhos) |
|---|---|---|---|---|---|---|---|
| `cabeca-circunferencial` | Cabeça (circunferencial) | circunferencial | NULL | 1 | false | `cabeca-anterior` | `cabeca-posterior` |
| `pescoco-circunferencial` | Pescoço (circunferencial) | circunferencial | NULL | 1 | false | `pescoco-anterior` | `pescoco-posterior` |
| `torax-circunferencial` | Tórax (circunferencial) | circunferencial | NULL | 1 | false | `torax-anterior` | `torax-posterior` |
| `abdome-circunferencial` | Abdome (circunferencial) | circunferencial | NULL | 1 | false | `abdome-anterior` | **`lombossacra-posterior`** (exceção clínica) |
| `pelve-circunferencial` | Pelve (circunferencial) | circunferencial | NULL | 1 | false | `pelve-anterior` | `pelve-posterior` |
| `msd-circunferencial` | Membro superior direito (circunferencial) | circunferencial | NULL | 1 | false | `msd-anterior` | `msd-posterior` |
| `mse-circunferencial` | Membro superior esquerdo (circunferencial) | circunferencial | NULL | 1 | false | `mse-anterior` | `mse-posterior` |
| `mid-circunferencial` | Membro inferior direito (circunferencial) | circunferencial | NULL | 1 | false | `mid-anterior` | `mid-posterior` |
| `mie-circunferencial` | Membro inferior esquerdo (circunferencial) | circunferencial | NULL | 1 | false | `mie-anterior` | `mie-posterior` |

- `template_texto`: **NULL** (o nó agregador não tem template próprio — o texto do card vem dos `template_texto` das sub-regiões marcadas, R9). `svg_coords`/path: **NULL** (R8).
- `ordem`: definir uma ordem coerente após os nível-1 existentes (ex.: 19 em diante, na ordem da tabela acima), sem colidir com `ordem` já usados em nível-1.
- `ativo`: `true`.
- **Idempotência obrigatória**: `INSERT ... ON CONFLICT (codigo) DO NOTHING` (ou equivalente) para que rodar a migration duas vezes não duplique nem falhe.

**3) `exame_fisico_regioes` NÃO MUDA.** Confirmar explicitamente: nenhuma coluna nova, nenhum índice novo, nenhuma alteração de constraint nessa tabela. A vista é resolvida pelo `regiao_codigo` do nó (sufixo `-circunferencial`). O contrato de persistência `RegistrarExameFisicoInput` permanece `{ codigo, lateralidade, achados, severidade, ordem }`.

**Multi-tenant**: o catálogo de regiões é **global por construção** (sem `estabelecimento_id`) — os 9 nós são globais, como os demais. Persistência de exame físico continua filtrando por estabelecimento/evolução no backend (auditoria multi-tenant concluída). Esta entrega não toca o filtro de tenant.

**LGPD/audit**: exame físico é prontuário; o audit de acesso/escrita já existe e continua valendo. Nenhuma nova exposição de PII — os nós de catálogo são metadados clínicos genéricos (não-PII). Nenhum novo endpoint/DTO expõe dado pessoal.

## 6. UX e fluxo

Fluxo (membro, ex.: Membro superior direito):
1. Profissional clica no membro no `BodyMap` → modal abre.
2. **Passo lado**: escolhe `Direito` / `Esquerdo` / `Ambos` (`AppPillToggle`, como no 004).
3. **Passo vista** (novo): escolhe `Anterior` / `Posterior` / `Circunferencial`.
4. **Passo sub-regiões**:
   - Anterior/Posterior → lista as sub-regiões daquela vista (comportamento atual).
   - **Circunferencial** → lista única agrupada: cabeçalho **"Anterior"** com `getFilhos("{base}-anterior")`, depois cabeçalho **"Posterior"** com `getFilhos("{base}-posterior")` (ou `lombossacra-posterior` no caso abdome). Marca N sub-regiões das duas faces.
5. Confirma → **1 card único** em "Regiões examinadas (N)", com:
   - Título = `"{Nome} (circunferencial)"`, neutralizado de lado quando "Bilateral"/"Vários lados".
   - **Badge de lado** (Direito/Esquerdo/Bilateral/Vários lados, do 004) **+ badge de vista** (`Circunferencial`).
   - Campo "Exame" pré-preenchido com a agregação dos `template_texto` das sub-regiões das duas faces (R9).

Fluxo (não-membro, ex.: Tórax):
1. Clica no tórax → modal abre direto no **passo vista**.
2. Escolhe `Anterior` / `Posterior` / `Circunferencial`.
3. **Circunferencial** → lista agrupada Anterior + Posterior. Marca, confirma → 1 card com badge de vista `Circunferencial`.

Estados:
- **Vista não escolhida** → não avança para sub-regiões; Confirmar indisponível (R10).
- **Nenhuma sub-parte marcada** → Confirmar desabilitado (`totalSelecionados === 0`), nenhum card criado (herdado do 004).
- **Lista vazia de regiões examinadas** → subseção não renderiza (comportamento atual).

Design system: reuso de `AppPillToggle` (passo de vista, mesmo componente do passo de lado), `RegionExamCard`, `RegionSelectorPopup`, `BodyMap`, `AppField`, `AppModal`, `AppButton`. **Nenhum componente novo.** Mobile: inalterado.

## 7. Critérios de aceite (testáveis)

> Numeração continua a partir do briefing 004 (que terminou em CA14). Estes são CAs incrementais do B1.

- **CA15 (ordem dos passos — membro)**: Dado um membro clicado no mapa, Quando a modal abre, Então o passo 1 é a escolha de **lado**, o passo 2 é a escolha de **vista** (Anterior/Posterior/Circunferencial) e o passo 3 são as **sub-regiões**, nessa ordem.

- **CA16 (ordem dos passos — não-membro)**: Dado uma região não-membro clicada (ex.: tórax), Quando a modal abre, Então o passo 1 é a escolha de **vista** e o passo 2 são as **sub-regiões** (sem passo de lado).

- **CA17 (circunferencial lista filhos das 2 vistas agrupados)**: Dado o tórax no modo **Circunferencial**, Quando o passo de sub-regiões carrega, Então a lista contém os filhos de `torax-anterior` sob o cabeçalho **"Anterior"** seguidos dos filhos de `torax-posterior` sob o cabeçalho **"Posterior"**, na ordem `ordem` de cada ramo.

- **CA18 (exceção abdome → lombossacra)**: Dado o abdome no modo **Circunferencial**, Quando o passo de sub-regiões carrega, Então sob "Anterior" aparecem os filhos de `abdome-anterior` e sob "Posterior" aparecem os filhos de `lombossacra-posterior` (e **não** os de um inexistente `abdome-posterior`).

- **CA19 (1 card por confirmação — circunferencial)**: Dado o modo circunferencial com sub-regiões das duas faces marcadas, Quando o profissional confirma, Então **exatamente 1** card novo é adicionado e o contador incrementa em 1 (não 1 por face).

- **CA20 (badge de vista)**: Dado um card criado no modo circunferencial, Quando o card é exibido, Então mostra uma badge de vista com o texto exato `Circunferencial`; dado um card criado no modo anterior, a badge mostra `Anterior`; no modo posterior, `Posterior`.

- **CA21 (membro circunferencial combina lado + vista)**: Dado o membro superior direito no modo circunferencial, Quando o card é criado, Então exibe **ambas** as badges: a de lado (`Direito`) e a de vista (`Circunferencial`), sem conflito.

- **CA22 (persistência via nó agregador)**: Dado um card circunferencial salvo via "Salvar evolução", Quando a requisição `POST /api/evolucoes/{id}/exame-fisico` é montada, Então o item de regiões correspondente tem `codigo = "{base}-circunferencial"` (ex.: `torax-circunferencial`), e **nenhum** campo `vista` é enviado no payload.

- **CA23 (catálogo aceita 'circunferencial')**: Dado a migration de seed aplicada, Quando se consulta `regioes_anatomicas_catalogo`, Então existem os 9 nós com `vista = 'circunferencial'`, `pai_codigo = NULL`, `nivel = 1`, `lateralidade = false`, e re-aplicar a migration **não** duplica nem falha (idempotência via `ON CONFLICT DO NOTHING`).

- **CA24 (relatório reagrupa como circunferencial)**: Dado uma evolução com um card cujo `regiao_codigo` termina em `-circunferencial`, Quando o relatório/PDF de exame físico é gerado, Então a entrada é reagrupada/rotulada como `"{Nome} (circunferencial)"` (lendo o código → vista), sem quebra de layout.

- **CA25 (mapa não quebra — B1)**: Dado um card circunferencial existente, Quando o `BodyMap` renderiza, Então o nó `{Nome} (circunferencial)` **não** renderiza hotspot (não há path SVG) e o mapa não lança erro; os hotspots de regiões anterior/posterior continuam acendendo como no 004.

- **CA26 (estado — vista não escolhida não avança)**: Dado a modal no passo de vista, Quando nenhuma vista foi escolhida, Então a modal não exibe sub-regiões e o botão Confirmar permanece indisponível.

- **CA27 (multi-tenant — não-regressão)**: Dado um usuário do estabelecimento B, Quando tenta registrar exame físico de evolução do estabelecimento A, Então recebe erro genérico de "não encontrado" e nada é gravado (comportamento existente do backend; o catálogo global de regiões não introduz vazamento cross-tenant pois não tem `estabelecimento_id`).

- **CA28 (LGPD/audit — não-regressão)**: Dado o registro do exame físico circunferencial, Quando ocorre, Então o audit de prontuário existente continua sendo gerado e nenhuma PII nova aparece em log ou mensagem de erro (sem novo endpoint/DTO expondo PII; nós de catálogo são metadados não-PII).

- **CA29 (não-regressão do 004 — lado/misto/card agregado)**: Dado os fluxos do briefing 004 (vista anterior ou posterior pura), Quando o profissional marca sub-partes e confirma, Então tudo do 004 continua funcionando: 1 confirmação = 1 card, badge de lado (`Direito`/`Esquerdo`/`Bilateral`/`Vários lados`), agregação de templates, dedupe intra-confirmação. O passo de vista não regride o passo de lado.

## 8. Riscos e dependências

- **B2 depende deste B1 e está fora dele**: o highlight/fusão de polígonos do nó circunferencial no `BodyMap` é B2 e **exige Discovery** (como acender duas faces ou fundir paths SVG). Em B1 o nó circunferencial intencionalmente **não acende** o mapa (R8/CA25). Registrar isso para o B2.
- **Sequenciamento `imedto-database` → `imedto-developer`**: o frontend só resolve a vista circunferencial se os 9 nós existirem no catálogo. A migration de seed deve estar pronta/aplicável antes da validação do front. Acionar `imedto-database` primeiro.
- **Relatório/PDF**: precisa ler o sufixo `-circunferencial` do código para rotular como circunferencial. Risco de regressão visual — QA valida que o PDF não quebra com card circunferencial (CA24). Reusa o helper `usePdfHeader.ts`/render de exame físico.
- **Regex `MEMBRO_RE`** (`SecaoExameFisico.vue` / `BodyMap.vue`) hoje casa apenas `(anterior|posterior)`. Como o **clique inicial** no mapa continua sendo num nó anterior/posterior (o circunferencial não tem hotspot, R8), a detecção de membro permanece pela vista anterior/posterior — a escolha de circunferencial acontece **dentro** da modal (passo de vista), não pelo clique. Garantir que a modal, ao mudar para o modo circunferencial de um membro, resolva o nó `{base}-circunferencial` correto a partir do lado escolhido (msd/mse/mid/mie).
- **Tipo `vista` no frontend**: `exameFisicoService.ts` tipa `vista: 'anterior' | 'posterior' | 'ambos' | null`. Incluir `'circunferencial'` no union (e remover/avaliar `'ambos'` se órfão — **não** removê-lo nesta entrega sem confirmar uso; apenas adicionar `'circunferencial'`).
- Dependência: nenhuma feature externa bloqueante além do `imedto-database`.

## 9. Observações para execução

**Sequência de agentes**: `imedto-database` (catálogo/seed — §5) **antes ou em paralelo, concluído antes do merge** → `imedto-developer` (modal/card/relatório) → `imedto-qa` (valida CAs + commit/push). Anunciar cada agente.

**Não-negociável**:
- **`exame_fisico_regioes` não muda** (sem coluna `vista`, sem campo no payload). A vista é resolvida pelo `regiao_codigo` (`-circunferencial`).
- Os 9 nós exatamente com os `codigo`/`nome`/`vista`/`pai_codigo` da tabela em §5. **Exceção do abdome** (`abdome-circunferencial` → ramo posterior = `lombossacra-posterior`) é regra clínica, não bug.
- INSERTs **idempotentes** (`ON CONFLICT (codigo) DO NOTHING`).
- 1 confirmação = 1 card mantido (004). Badge de vista é **adicional** à de lado, não a substitui.
- O nó circunferencial **não** acende o mapa em B1 (highlight é B2).
- Briefing 004 permanece **imutável**; este o estende.

**Liberdade técnica do dev**:
- Como modelar o estado de passo de vista na modal (`passo: 'lado' | 'vista' | 'subregioes'` ou flag separada) — sugerido reusar `AppPillToggle` (mesmo do passo de lado).
- Como derivar `(ramoAnterior, ramoPosterior)` a partir do `{base}-circunferencial` — sugerido um mapa explícito no front (incluindo a exceção do abdome) ou derivar por convenção `{base}-anterior`/`{base}-posterior` com override para `abdome`.
- Onde montar a string agregada e a derivação da badge de vista (helpers locais em `SecaoExameFisico.vue`).

**Reuso**: `getFilhos`, `getTemplate`, `getCaminho`, `caminhoNeutro`, `getAncestorNivel1Id` (já em `SecaoExameFisico.vue`); `AppPillToggle` (já em `RegionSelectorPopup.vue`). Não duplicar.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md` — seção "Hierarquia de regiões anatômicas" (~L145-147)**: ampliar a nota para registrar a **vista circunferencial agregadora**:
  - O campo `vista` passa a aceitar **`circunferencial`** além de `anterior`/`posterior`.
  - Um nó `{base}-circunferencial` é **nível-1, `pai_codigo = NULL`, sem filhos próprios**; ele **agrega** as sub-regiões de duas vistas em tempo de leitura na modal (`getFilhos("{base}-anterior") + getFilhos("{base}-posterior")`).
  - **Regra de resolução de filhos** e a **exceção do abdome** (`abdome-circunferencial` une `abdome-anterior` + `lombossacra-posterior`).
  - Observar que a validação de subgrupo `vista = pai.vista` **não** se aplica aos circunferenciais (são raiz, sem filhos), e que o `RegiaoTreeView` do admin passará a exibir um terceiro grupo de vista (`circunferencial`).
  - Mudança **incremental e cirúrgica** — ajustar só essa subseção, sem reescrever o doc.
- Demais docs: **nenhuma alteração**. Sem mudança de infra (`INFRA.md`), comandos (`COMANDOS.md` — a migration segue o fluxo de seed já documentado), LGPD (`LGPD.md` — sem novo PII/endpoint) ou novo componente de design (`DESIGN.md` — reuso de `AppPillToggle`, cuja pendência de doc já está registrada como backlog do 004).
