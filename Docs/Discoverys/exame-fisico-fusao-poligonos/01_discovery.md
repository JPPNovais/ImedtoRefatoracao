# Discovery técnico — Fusão de polígonos do tronco + coloração por vista (Exame Físico B2)

**ID**: exame-fisico-fusao-poligonos / 01_discovery
**Tipo**: investigação de viabilidade (NÃO implementa a feature)
**Data**: 2026-06-08
**Epic**: vista anterior/posterior/circunferencial (B1 entregue — briefing `2026-06-08_001` no mapa + `2026-06-08_005 B1` no catálogo)
**Escopo deste B2**:
1. **FUNDIR** polígonos do tronco no mapa: tórax+abdome+pelve → 1 polígono anterior; tórax+lombossacra+pelve → 1 polígono posterior.
2. **COLORIR** ("acender") regiões considerando a vista (anterior, posterior, circunferencial — esta acende anterior **E** posterior).

> Foco: destravar o B2 com decisões técnicas resolvidas e isolar o que ainda depende do PO.

---

## 0. Glossário rápido (modelo de dados real verificado)

- O catálogo (`regioes_anatomicas_catalogo`) usa `codigo` como chave semântica (ex.: `torax-anterior`). O service (`exameFisicoService.ts`, `mapCatalogoParaLocal`) mapeia **`codigo` → `id`** e mantém `nome` (ex.: "Tórax (anterior)") separado.
- **`BodyMap.vue` casa path por `nome`** (`currentPaths[r.nome]`), **não** por `id`. Isto é central para a fusão (ver Pergunta 1).
- `SecaoExameFisico.vue` e o popup raciocinam por `id`/`codigo`.
- Nível-1 anterior: `torax-anterior`, `abdome-anterior`, `pelve-anterior`, `cabeca-anterior`, `pescoco-anterior`, `msd/mse/mid/mie-anterior`.
- Nível-1 posterior: `torax-posterior`, `lombossacra-posterior`, `pelve-posterior`, `cabeca-posterior`, `pescoco-posterior`, `msd/mse/mid/mie-posterior`.
- Nível-1 circunferencial (B1, 9 nós **sem path, sem filhos próprios, lateralidade=false**): `cabeca/pescoco/torax/abdome/pelve/msd/mse/mid/mie-circunferencial`.
- `RAMOS_CIRCUNFERENCIAL` (em `RegionSelectorPopup.vue`) é a fonte de verdade do mapeamento circunferencial → (anterior, posterior). Exceção clínica: `abdome-circunferencial → { anterior: abdome-anterior, posterior: lombossacra-posterior }`.

---

## 1. Estado atual (como funciona hoje)

### 1.1 `bodyMapPaths.ts` — origem e o truque do tronco

Cabeçalho diz `// Auto-generated from CORPO-Padrão SVGs - DO NOT EDIT MANUALLY`. **Evidência contrária**: não existe nenhum gerador no repositório.

- Busca por scripts/geradores: `frontend/` não tem diretório `scripts`, `package.json` não referencia geração de paths, e o único arquivo com "DO NOT EDIT" relevante é o próprio `bodyMapPaths.ts` (os demais hits são `node_modules`).
- O SVG fonte (`ReferenciaLegado/.../CORPO-Padrão-Masculino.svg`) é uma **lista plana de `<path>` sem `id`, sem `inkscape:label`, sem `data-name` e sem `clipPath`**. Cabeçalho: `viewBox="0 0 1400 1024"`, paths com `fill="#452B97"`. Ou seja: o SVG fonte **não tem** as sub-regiões de tronco nem os clip-paths.

**Conclusão (verificada):** o cabeçalho "auto-generated / DO NOT EDIT" é **aspiracional/legado** — não há pipeline de geração. As coordenadas foram extraídas manualmente uma vez do SVG, e **toda a engenharia de "tronco = 1 path com clip por faixa" foi escrita à mão diretamente em `bodyMapPaths.ts`**. Mexer no arquivo **não quebra pipeline algum** (não existe).

O truque atual do tronco:
- Existe **um único path geométrico** por sexo+vista: `M_TORSO_ANT` / `M_TORSO_POST` (e `F_TORSO_ANT`/`F_TORSO_POST`).
- Esse mesmo `d` é reusado em 3 entradas anteriores (`Tórax/Abdome/Pelve (anterior)`) e 3 posteriores (`Tórax/Região lombossacra/Pelve (posterior)`), cada uma com um `clipId` diferente.
- Os `clipPath` em `BodyMap.vue` são `<rect>` horizontais que recortam o path inteiro em faixas por faixa-Y:
  - anterior: tórax `y 0–370`, abdome `y 370–452`, pelve `y 452–652` (todos `x 0–700`).
  - posterior: tórax `y 0–387`, lombo `y 387–470`, pelve `y 470–670` (todos `x 700–1400`).

Ou seja: **visualmente o tronco já É um polígono único** (mesmo `d`), apenas fatiado por hitbox de clique. A "fusão visual" já está 90% feita pela geometria; o que existe são 3 hotspots de clique sobre o mesmo desenho.

### 1.2 `BodyMap.vue` — render, hover, seleção, espelhamento

- `viewBox="0 25 1400 970"`. Divisão física: anterior em `x<700`, posterior em `x>700` (linha tracejada em `x=700`).
- `regioesComPath` = nível-1 do catálogo que têm path em `currentPaths` (lookup **por `nome`**), enriquecidos com `isExaminada = regioesExaminadas.includes(r.id)`, ordenados por `zOrder`.
- Render: um `<path>` por região, com `clip-path` quando há `clipId`. Classe resolvida por prioridade: `region-selected` (se examinada) > `region-hover` (se no grupo em hover) > `region-idle`.
- Hover de membro: `getMembroGroup` + `MEMBRO_RE` acende os dois lados do mesmo tipo/vista juntos no hover (visual de "membro inteiro"). Tronco e regiões axiais não têm agrupamento de hover.
- **Acender (seleção)**: dirigido **exclusivamente** por `regioesExaminadas: string[]` (array de `id`/`codigo` de nível-1) vindo do pai. O `BodyMap` não decide nada — só pinta o que está no array.

### 1.3 `SecaoExameFisico.vue` — quem alimenta o highlight

- `regioesJaSelecionadas`: para cada região registrada, adiciona o `regiao_id` e seu **ancestral nível-1** (`getAncestorNivel1Id`). Serve o popup (dedup) e é a base do highlight.
- `regioesExaminadasMapa` (o que vai pro `:regioes-examinadas` do BodyMap): parte de `regioesJaSelecionadas` e, **só para entradas bilaterais de membro**, adiciona o nível-1 do lado oposto via `getOpostoNivel1Id` (regex de membro). É **a única generalização de "acender além do clicado" existente hoje**.
- Resultado prático hoje para circunferencial: o card circunferencial guarda `regiao_id = {base}-circunferencial` (ex.: `torax-circunferencial`), que **não tem path** no `bodyMapPaths.ts`. Logo `getAncestorNivel1Id` retorna o próprio `torax-circunferencial` (é nível-1), que **não casa nenhum `nome` no mapa** → **não acende nada**. **Este é o gap que o B2 fecha.**

### 1.4 Como o B1 deixou o circunferencial "sem acender"

Confirmado: os 9 nós circunferenciais entraram no catálogo com `svg_coords = NULL` e **não foram adicionados a `bodyMapPaths.ts`**. O fluxo de seleção circunferencial funciona (popup lista anterior+posterior agrupados, gera 1 card `{base}-circunferencial`), mas o **mapa não reflete** a seleção. Intencional: B1 era catálogo+modal; pintar o mapa é o B2.

---

## 2. Pergunta 1 — FUSÃO: barato (remover clips) ou caro (regenerar SVG)?

**Resposta: barato. Não há SVG a regenerar; não há pipeline a quebrar.**

A geometria do tronco já é um polígono único (`M_TORSO_ANT/POST`). "Fundir" no sentido visual **já está feito**. O que muda no B2 é o **número de hotspots** e o **mapeamento clique→catálogo** (Pergunta 2) e a **coloração por vista** (Perguntas 3-4).

### Opções de fusão

**Opção A — Hotspot único por vista (recomendada).**
Adicionar em `bodyMapPaths.ts` duas entradas novas (por sexo): `Tronco (anterior)` e `Tronco (posterior)`, ambas usando `M_TORSO_ANT`/`M_TORSO_POST` **sem `clipId`** (path inteiro = 1 hotspot). As 6 entradas atuais (`Tórax/Abdome/Pelve (anterior)`, `Tórax/Lombossacra/Pelve (posterior)`) param de ser renderizadas como hotspots clicáveis.
- Prós: 1 clique = 1 alvo; coloração por vista vira trivial (acende 1 path por vista); some o problema das faixas Y imprecisas (a fronteira tórax/abdome/pelve é uma reta horizontal arbitrária, clinicamente fraca).
- Contras: o tronco deixa de ser pré-segmentado no clique — precisa resolver "qual parte" depois (Pergunta 2). Exige um nível-1 "Tronco" no catálogo **OU** um pseudo-nó só-de-mapa (ver Pergunta 2 e Decisão de Produto DP-1).

**Opção B — Manter 3 hotspots, fundir só a aparência (status quo geométrico).**
Não mexe em `bodyMapPaths.ts` para fusão; o tronco já parece único. Mantém 3 hotspots por vista. A "fusão" seria apenas garantir que hover/selected pintem o tronco inteiro (acender as 3 faixas juntas), não só a faixa sob o cursor.
- Prós: zero mudança de geometria, zero mudança de clique→catálogo, menor risco.
- Contras: não atende ao pedido de "1 polígono" em termos de interação; mantém as faixas Y arbitrárias; coloração por vista precisa acender as 3 faixas (factível, mas é "fingir" fusão).

**Recomendação:** **Opção A**, condicionada à decisão DP-1 (catálogo "Tronco" vs pseudo-nó de mapa). É a que entrega de fato "1 polígono por vista" e simplifica radicalmente a coloração. A geometria já existe — o custo real está em clique→catálogo, não em desenho.

> **`bodyMapPaths.ts` é seguro de editar.** Recomenda-se **corrigir o cabeçalho** na mesma entrega (de "Auto-generated … DO NOT EDIT" para algo como "Path data extraído manualmente dos SVGs CORPO-Padrão; editar à mão"), porque o header atual mente e induz medo de tocar o arquivo. Mudança de comentário, cirúrgica.

---

## 3. Pergunta 2 — CLIQUE→CATÁLOGO após a fusão

Se o tronco vira 1 hotspot, ao clicar o sistema não sabe se o usuário quer tórax, abdome ou pelve. Opções:

**Opção 2a — Modal com 1º passo "qual parte do tronco".**
O hotspot único emite um nó "tronco". O popup ganha um passo inicial (análogo ao passo de lado de membro) listando Tórax/Abdome/Pelve. Depois segue o fluxo de vista+sub-regiões já existente.
- Viabilidade no código atual: **alta**. O popup já tem máquina de passos (`'lado' | 'vista' | 'subregioes'`) e o padrão de "passo extra condicional" foi resolvido no B1. Adicionar `'parte-tronco'` é incremental.
- Contra: +1 clique para o caso comum (examinar tórax). Conflita parcialmente com o passo de vista (ver Pergunta 3).

**Opção 2b — Lista única com as sub-regiões das 3 partes juntas.**
Clicar no tronco abre direto a lista de sub-regiões agrupada por parte (Tórax: pulmão/coração/…; Abdome: epigástrio/…; Pelve: …). Sem passo de "qual parte".
- Viabilidade: **alta** — é exatamente o padrão `filhosCircunferencial` (grupos Anterior/Posterior) já implementado no B1, generalizado para grupos por parte. Reusa `getFilhos('torax-anterior') + getFilhos('abdome-anterior') + getFilhos('pelve-anterior')`.
- Prós: menos cliques; o profissional escolhe a estrutura fina diretamente. O `regiao_id` do card resolve via `getAncestralComum` (já existe) — se marcar só pulmão+coração, ancestral = `torax-anterior`; se misturar tórax+abdome, ancestral = "Tronco".
- Contra: lista mais longa; exige um nó "Tronco" como ancestral comum quando a seleção cruza partes (Decisão DP-1).

**Opção 2c — Fusão só visual, mantendo 3 hotspots internos (= Opção B da Pergunta 1).**
- Viabilidade: **trivial** (quase nada muda no clique). Mas não é "1 hotspot".

**Recomendação:** **Opção 2b** (lista agrupada por parte), reusando o motor `filhosCircunferencial`/grupos do B1. É a que melhor casa com o código existente, minimiza cliques e já tem `getAncestralComum` para resolver o `regiao_id`. Cai bem com a Opção A da Pergunta 1.
**Fallback de menor risco:** Opção 2c (fusão só visual) se o PO quiser preservar o clique-por-faixa atual e o B2 ficar restrito a coloração.

---

## 4. Pergunta 3 — RECONCILIAÇÃO MAPA × VISTA (a tensão central)

**O conflito é real.** O mapa **já** separa fisicamente anterior (`x<700`) e posterior (`x>700`): clicar num lado **já comunica a vista**. Mas o popup do B1 **pergunta a vista de novo** (anterior/posterior/circunferencial). Para o tronco fundido isso fica redundante (cliquei na frente → por que escolher "posterior"?) e potencialmente contraditório (cliquei na frente e escolhi "posterior").

### Modelos de interação avaliados

**Modelo M1 — Mapa pré-determina a vista; popup só oferece "circunferencial" como upgrade (RECOMENDADO).**
- Clicar no tronco anterior → popup abre com **vista = anterior pré-resolvida** (pula o passo de vista). Oferece um único toggle/atalho: **"Estender para circunferencial"** (acrescenta o ramo posterior). Clicar no tronco posterior → vista = posterior, com atalho "estender para circunferencial".
- Para membros: idem — o lado físico clicado não muda, mas a vista (anterior/posterior) vem do path clicado; o passo de vista vira "anterior (já) / circunferencial".
- Prós: elimina a redundância; o caminho comum (anterior simples) fica em menos cliques que hoje; "circunferencial" continua acessível. Coerência espacial: o que vejo no mapa = o que registro.
- Contras: muda o fluxo do B1 (que sempre mostra 3 opções de vista). Requer ajuste no popup (passo de vista condicional/encolhido). Como derivar a vista do clique: trivial — o `nome`/`id` do path clicado já carrega `(anterior)`/`(posterior)`.

**Modelo M2 — Mapa deixa de pré-determinar; popup é a única fonte da vista (status quo B1).**
- O clique no tronco (qualquer lado) abre o popup que pergunta anterior/posterior/circunferencial. O lado físico clicado é ignorado para fins de vista.
- Prós: zero mudança no popup; consistente com B1. Um único hotspot de tronco poderia até nem distinguir anterior/posterior (mas aí perde a metáfora do mapa).
- Contras: ignora informação que o usuário já deu (o lado clicado); mantém redundância; menos intuitivo.

**Modelo M3 — Híbrido: clique pré-seleciona a vista mas permite trocar.**
- Clicar no tronco anterior → popup abre no passo de vista **com "anterior" pré-selecionado**, mas as 3 opções visíveis e editáveis.
- Prós: respeita o clique e permite corrigir; mudança mínima sobre o B1 (só pré-selecionar).
- Contras: ainda mostra o passo de vista (clique extra mental); permite o estado contraditório "cliquei na frente, troquei pra posterior" sem o mapa refletir o lado.

**Recomendação:** **M1** como destino, **M3** como meio-termo de menor risco se o PO quiser preservar a UI de vista do B1 intacta. M1 é o mais coerente e implementável (a vista é derivável do path clicado sem nova estrutura); M3 é a menor delta sobre o B1. Decisão de produto: **DP-2**.

> Nota de coerência: no Modelo M1/M3, "circunferencial" iniciado de um clique anterior deve **acender também o posterior** (Pergunta 4). O mapa mostrando os dois lados acesos é a confirmação visual de que a vista virou circunferencial — fecha o loop UX.

---

## 5. Pergunta 4 — COLORAÇÃO CIRCUNFERENCIAL ("acender a vista oposta")

Hoje a única generalização de "acender além do clicado" é o espelhamento bilateral de membro (`regioesExaminadasMapa` + `getOpostoNivel1Id`). Precisamos generalizar para "acender a vista oposta" quando o card é circunferencial.

### O problema concreto

Um card circunferencial guarda `regiao_id = {base}-circunferencial` (sem path). Para acender, precisamos derivar os **2 nível-1 com path** (anterior + posterior) e injetá-los em `regioesExaminadasMapa`. A tensão é o **abdome**: `abdome-circunferencial` → anterior `abdome-anterior` ("Abdome (anterior)") **mas** posterior `lombossacra-posterior` ("Região lombossacra (posterior)") — nomes/ids diferentes, não um simples flip de sufixo.

### A boa notícia: o mapeamento já existe

`RAMOS_CIRCUNFERENCIAL` (em `RegionSelectorPopup.vue`) **já resolve exatamente isso**, incluindo a exceção do abdome. O B2 deve **promover esse mapa a fonte única compartilhada** (extraí-lo para um módulo, ex.: `frontend/src/components/exame-fisico/regioesCircunferenciais.ts`) e consumi-lo tanto no popup quanto em `SecaoExameFisico.vue` para a coloração. Isso evita duplicar a regra (princípio Reuso > duplicação).

### Como derivar os polígonos a acender

Em `SecaoExameFisico.vue`, dentro de `regioesExaminadasMapa`, para cada card com `vista === 'circunferencial'` (ou cujo `regiao_id` termina em `-circunferencial`):
1. `const ramos = RAMOS_CIRCUNFERENCIAL[regiao_id]` → `{ anterior, posterior }` (ex.: `abdome-circunferencial` → `{ anterior: 'abdome-anterior', posterior: 'lombossacra-posterior' }`).
2. Adicionar **ambos** `ramos.anterior` e `ramos.posterior` ao set de ids do mapa.
3. Como `BodyMap` pinta por `id`/`codigo` (que casa o `nome` via `regioesComPath`), ambos os nível-1 acendem — anterior à esquerda, posterior à direita.

Caso de membro circunferencial (ex.: `msd-circunferencial` → `msd-anterior` + `msd-posterior`): além disso, se for **bilateral**, combinar com o espelhamento existente (acende também `mse-anterior`+`mse-posterior`). A composição é aditiva (tudo entra num `Set`), então bilateral × circunferencial coexistem sem conflito.

### Impacto na fusão do tronco (Pergunta 1/2)

Se o tronco vira hotspot único e o B2 introduzir um nó "Tronco (anterior)"/"Tronco (posterior)" (ou pseudo-nós de mapa), o `RAMOS_CIRCUNFERENCIAL` ganha `tronco-circunferencial → { anterior: 'tronco-anterior', posterior: 'tronco-posterior' }`. **Mas** o catálogo circunferencial atual é por parte (`torax/abdome/pelve-circunferencial`), não por "tronco". Há duas saídas:
- **(i)** Manter circunferencial por parte (tórax/abdome/pelve) e, no mapa, acender o **polígono de tronco inteiro** da vista correspondente quando qualquer parte daquela vista estiver examinada. Ou seja: o highlight do tronco é "OU" das partes — se tórax-anterior OU abdome-anterior OU pelve-anterior examinado → acende "Tronco (anterior)".
- **(ii)** Introduzir `tronco-*` no catálogo (Decisão DP-1).

A saída **(i)** é a mais barata e não exige migration: o mapa mapeia `{torax,abdome,pelve}-anterior → "Tronco (anterior)"` para fins de acender, mantendo o catálogo intacto. Recomendada se DP-1 for "sem novo nó de catálogo".

**Recomendação Pergunta 4:** extrair `RAMOS_CIRCUNFERENCIAL` para módulo compartilhado e generalizar `regioesExaminadasMapa` para resolver circunferencial → 2 vistas. Para o tronco fundido, acender o polígono de tronco da vista via "OU das partes" (saída i), evitando migration.

---

## 6. Pergunta 5 — RISCOS

- **Regressão visual cross-browser (médio).** A fusão usa o mesmo `d` de path já validado; o risco está em *remover* clip-paths (a faixa Y arbitrária some, o que é bom) e em garantir que `region-selected`/`region-hover` pintem o polígono inteiro. `clip-path: url(#…)` é amplamente suportado; remover é mais seguro que adicionar. Sem novas técnicas SVG.
- **QA sem chrome-devtools (alto para validação visual).** Confirmado no histórico: ambiente de QA não roda browser; banco em container EC2 sem túnel. Validação visual fica **para o usuário em prod**. Mitigação: cobrir a *lógica* (deriva de vista, ids acesos, mapeamento circunferencial incluindo abdome) com testes unit em `BodyMap.test.ts`/`SecaoExameFisico` (a montagem do `regioesExaminadasMapa` é testável sem DOM real).
- **Impacto no highlight de membro/bilateral (médio).** `regioesExaminadasMapa` e `getMembroGroup` são o caminho mais sensível. A generalização circunferencial deve ser **aditiva** (Set), nunca substituir o espelhamento bilateral. Teste de regressão: bilateral simples continua acendendo os 2 lados; bilateral+circunferencial acende 4 polígonos.
- **Sexo M/F — dois conjuntos de paths (baixo, mas dobra o trabalho).** Toda entrada nova em `bodyMapPaths.ts` (ex.: `Tronco (anterior/posterior)`) deve ser criada em **ambos** `maleRegionPaths` e `femaleRegionPaths`, reusando `M_TORSO_*`/`F_TORSO_*`. Esquecer um sexo = tronco não acende para metade dos pacientes. Item de checklist.
- **Lookup por `nome` vs `id` (armadilha).** `BodyMap` casa por `nome`. Qualquer nó novo no mapa precisa de uma entrada em `bodyMapPaths.ts` cuja **chave seja exatamente o `nome`** do catálogo (ou, se for pseudo-nó de mapa, um `nome` sintético consistente). Divergência de string = silenciosamente não renderiza.
- **Faixas Y do clip eram clinicamente arbitrárias (dívida que some).** Hoje a fronteira tórax/abdome/pelve é uma reta horizontal fixa — não anatômica. A fusão elimina essa imprecisão, o que é um ganho colateral.

---

## 7. PROPOSTA DE ABORDAGEM recomendada para o B2

Combinação coerente: **Fusão Opção A + Clique Opção 2b + Vista Modelo M1 (ou M3 como fallback) + Coloração via `RAMOS_CIRCUNFERENCIAL` compartilhado (saída i para o tronco)**.

### 7.1 `bodyMapPaths.ts`
- Adicionar, em `maleRegionPaths` **e** `femaleRegionPaths`:
  - `'Tronco (anterior)'` → `{ d: M_TORSO_ANT, zOrder: 0 }` (sem `clipId`).
  - `'Tronco (posterior)'` → `{ d: M_TORSO_POST, zOrder: 0 }` (sem `clipId`).
- Manter as 6 entradas atuais de tórax/abdome/pelve **apenas se** ainda houver lookup por elas (no plano "OU das partes" para acender, elas continuam úteis como nível-1 do catálogo; mas **não** precisam mais ser hotspots clicáveis — ver 7.3).
- Corrigir o cabeçalho enganoso (não é auto-gerado).
- **Risco**: baixo. **Esforço**: baixo (2 entradas × 2 sexos, reusando `d`).

### 7.2 `regioesCircunferenciais.ts` (novo, compartilhado)
- Extrair `RAMOS_CIRCUNFERENCIAL` para cá; importar no popup (substituindo a cópia local) e em `SecaoExameFisico.vue`.
- Acrescentar o mapa auxiliar mapa-only: `PARTE_PARA_TRONCO` = `{ 'torax-anterior':'Tronco (anterior)', 'abdome-anterior':'Tronco (anterior)', 'pelve-anterior':'Tronco (anterior)', 'torax-posterior':'Tronco (posterior)', 'lombossacra-posterior':'Tronco (posterior)', 'pelve-posterior':'Tronco (posterior)' }`.
- **Risco**: baixo (refactor de mover constante). **Esforço**: baixo.

### 7.3 `BodyMap.vue`
- `regioesComPath` passa a renderizar como hotspots os nível-1 que tenham path **exceto** as 6 faixas de tronco (que deixam de ser clicáveis); em vez delas, renderiza os 2 pseudo-hotspots `Tronco (anterior/posterior)`.
  - Como o BodyMap recebe `regioes` (nível-1 do catálogo) e casa por `nome`, será preciso **injetar os pseudo-nós "Tronco (anterior/posterior)"** na lista que o BodyMap consome, ou tratar a fusão internamente no BodyMap (preferível: o BodyMap conhece as 2 entradas de tronco e as renderiza, emitindo um clique "tronco-anterior"/"tronco-posterior" sintético que `SecaoExameFisico` traduz).
- Coloração (`isExaminada`): um path de tronco acende se **qualquer** id da sua vista estiver em `regioesExaminadas` (via `PARTE_PARA_TRONCO`). Implementável como: `isTroncoExaminado(vista) = regioesExaminadas.some(id => PARTE_PARA_TRONCO[id] === nomeDoTronco)`.
- Hover: ao passar sobre o tronco, acende o polígono inteiro da vista (já é 1 path → trivial).
- **Risco**: médio (é o coração da mudança e o ponto sem validação visual em QA). **Esforço**: médio.

### 7.4 `SecaoExameFisico.vue`
- `onRegiaoClicada`: tratar o clique sintético de tronco → abrir popup no modo "lista agrupada por parte" (Opção 2b), com vista pré-resolvida pelo lado clicado (Modelo M1).
- `regioesExaminadasMapa`: generalizar para, além do espelhamento bilateral existente, expandir cards circunferenciais via `RAMOS_CIRCUNFERENCIAL` (adicionar ambos os ramos ao Set). Manter aditivo.
- **Risco**: médio. **Esforço**: médio.

### 7.5 Testes (única validação possível no sandbox)
- `BodyMap.test.ts`: tronco anterior acende quando qualquer parte anterior examinada; idem posterior; circunferencial acende ambos; M e F.
- `SecaoExameFisico` (ou teste de unidade da função): `regioesExaminadasMapa` expande `abdome-circunferencial` para `abdome-anterior` **e** `lombossacra-posterior`; bilateral+circunferencial = 4 polígonos.

### Esforço/risco global
**Esforço: M.** **Risco: médio**, concentrado na ausência de validação visual em QA (mitigar com testes de lógica + validação do usuário em prod, conforme padrão do projeto).

---

## 8. Decisões de PRODUTO pendentes (precisam do PO) vs decisões TÉCNICAS já resolvidas

### Pendentes de PO (não cravar sem decisão)
- **DP-1 — Modelo do "Tronco" no catálogo.** Opções: (a) **sem novo nó** — manter `torax/abdome/pelve` no catálogo e o tronco existe só como conceito visual+"OU das partes" no mapa (recomendado: zero migration); (b) introduzir nó(s) `tronco-anterior/posterior(/circunferencial)` no catálogo (exige migration + acionar `imedto-database` + decidir o que vira filho de quê). Impacta clique→catálogo e o `regiao_id` dos cards.
- **DP-2 — Reconciliação mapa×vista.** M1 (clique determina a vista; popup só estende para circunferencial), M3 (clique pré-seleciona, vista editável) ou M2 (popup é a fonte, status quo B1). Recomendação técnica: M1; fallback M3.
- **DP-3 — Granularidade do clique no tronco.** 2b (lista agrupada por parte, sem passo "qual parte") vs 2a (passo "qual parte do tronco" primeiro) vs 2c (manter 3 hotspots, fusão só visual). Recomendação: 2b; fallback 2c.
- **DP-4 — Semântica clínica da fusão.** "Tórax+abdome+pelve = 1 tronco anterior" é aceitável para o registro clínico do estabelecimento? (A fronteira atual já é arbitrária, então fundir provavelmente *melhora*, mas é decisão clínica/de produto, não técnica.)

### Já resolvidas tecnicamente (não precisam de PO)
- `bodyMapPaths.ts` **não** é auto-gerado e **pode** ser editado com segurança (cabeçalho será corrigido). — Pergunta 1.
- A geometria do tronco já é polígono único; "fusão visual" é essencialmente remover clips. — Pergunta 1.
- Coloração circunferencial é derivável de `RAMOS_CIRCUNFERENCIAL` (incl. exceção do abdome → lombossacra). Extrair para módulo compartilhado. — Pergunta 4.
- Generalização do highlight é **aditiva** sobre o espelhamento bilateral (Set), sem conflito. — Perguntas 4/5.
- Toda entrada de path nova é duplicada em M **e** F; chave do path = `nome` exato. — Pergunta 5.
- Validação visual fica para o usuário em prod; QA cobre lógica por testes. — Pergunta 5.

---

## 9. Protótipo ilustrativo (não roda — só ilustra a forma)

### 9.1 Módulo compartilhado (`regioesCircunferenciais.ts`)
```ts
// Fonte única — consumido pelo popup (resolver filhos) e pelo mapa (acender vistas).
export const RAMOS_CIRCUNFERENCIAL: Record<string, { anterior: string; posterior: string }> = {
  'cabeca-circunferencial':  { anterior: 'cabeca-anterior',  posterior: 'cabeca-posterior' },
  'pescoco-circunferencial': { anterior: 'pescoco-anterior', posterior: 'pescoco-posterior' },
  'torax-circunferencial':   { anterior: 'torax-anterior',   posterior: 'torax-posterior' },
  'abdome-circunferencial':  { anterior: 'abdome-anterior',  posterior: 'lombossacra-posterior' }, // exceção clínica
  'pelve-circunferencial':   { anterior: 'pelve-anterior',   posterior: 'pelve-posterior' },
  'msd-circunferencial':     { anterior: 'msd-anterior',     posterior: 'msd-posterior' },
  'mse-circunferencial':     { anterior: 'mse-anterior',     posterior: 'mse-posterior' },
  'mid-circunferencial':     { anterior: 'mid-anterior',     posterior: 'mid-posterior' },
  'mie-circunferencial':     { anterior: 'mie-anterior',     posterior: 'mie-posterior' },
}

// Mapa-only: qual polígono de tronco acender para cada nível-1 do tronco.
export const PARTE_PARA_TRONCO: Record<string, string> = {
  'torax-anterior': 'Tronco (anterior)', 'abdome-anterior': 'Tronco (anterior)', 'pelve-anterior': 'Tronco (anterior)',
  'torax-posterior': 'Tronco (posterior)', 'lombossacra-posterior': 'Tronco (posterior)', 'pelve-posterior': 'Tronco (posterior)',
}
```

### 9.2 Expansão do highlight (em `SecaoExameFisico.vue`, dentro de `regioesExaminadasMapa`)
```ts
const regioesExaminadasMapa = computed(() => {
  const ids = new Set(regioesJaSelecionadas.value)
  for (const r of regioes.value) {
    // (existente) espelhamento bilateral de membro
    if (r.lateralidade === 'bilateral') {
      const n1 = getAncestorNivel1Id(r.regiao_id)
      const oposto = n1 && getOpostoNivel1Id(n1)
      if (oposto) ids.add(oposto)
    }
    // (NOVO) circunferencial → acende anterior E posterior (incl. abdome→lombossacra)
    const ramos = RAMOS_CIRCUNFERENCIAL[r.regiao_id]
    if (ramos) { ids.add(ramos.anterior); ids.add(ramos.posterior) }
  }
  return Array.from(ids)
})
```

### 9.3 Tronco fundido + highlight por vista (em `BodyMap.vue`, conceitual)
```ts
// pseudo-hotspots de tronco, renderizados no lugar das 6 faixas com clip
const TRONCO = [
  { nome: 'Tronco (anterior)',  cliqueId: 'tronco-anterior'  },
  { nome: 'Tronco (posterior)', cliqueId: 'tronco-posterior' },
]
function troncoAceso(nomeTronco: string): boolean {
  // "OU das partes": acende se qualquer nível-1 daquela vista estiver examinado
  return props.regioesExaminadas.some(id => PARTE_PARA_TRONCO[id] === nomeTronco)
}
// <path :d="currentPaths[t.nome].d"
//       :class="troncoAceso(t.nome) ? 'region-selected' : hover ? 'region-hover' : 'region-idle'"
//       @click="$emit('regiaoClicada', { id: t.cliqueId, nome: t.nome, nivel: 1, ... })" />
```
> O clique sintético `tronco-anterior`/`tronco-posterior` é interpretado por `onRegiaoClicada`, que abre o popup em modo "lista agrupada por parte" com a vista já resolvida pelo lado (Modelo M1). Para circunferencial, o popup oferece estender — e ao confirmar, `RAMOS_CIRCUNFERENCIAL` faz os dois lados acenderem, fechando o loop visual.

---

## 10. Atualização de documentação prevista para o B2 (não nesta discovery)

- `Docs/DESIGN.md` — registrar o padrão "polígono fundido + acender por vista" do mapa corporal, se virar componente reutilizável.
- Se DP-1 = (b) com novo nó de catálogo → `Docs/` de schema + migration via `imedto-database`.
- Briefing imutável do B2 em `planejamentos/` deve travar DP-1..DP-4 antes do dev começar.
