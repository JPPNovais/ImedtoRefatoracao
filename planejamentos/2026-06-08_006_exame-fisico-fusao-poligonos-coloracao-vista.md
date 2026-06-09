# Exame Físico — fusão de polígonos do tronco + coloração por vista no mapa corporal — B2

**ID**: 2026-06-08_006
**Status**: Aprovado por usuário em 2026-06-08
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (seção Exame Físico — mapa corporal, modal de seleção, highlight bilateral), design system (BodyMap)

> **Escopo deste briefing**: APENAS o **B2** da epic de vista no Exame Físico — **fundir os polígonos do tronco** no mapa corporal (`BodyMap`) e **acender ("colorir") as regiões considerando a vista** (anterior / posterior / circunferencial, esta acendendo anterior **E** posterior). **Frontend-only, zero migration** (DP-1). Não cobre a UI admin de sub-regiões (B3) nem qualquer mudança de catálogo/persistência.
> **Relação com 2026-06-08_005 (B1, IMUTÁVEL) e 2026-06-08_004 (IMUTÁVEL)**: este briefing **estende** o B1 (passo de vista com 3 opções na modal; 9 nós circunferenciais já no catálogo, já implementado, validado e migration aplicada) e o 004 (1 confirmação = 1 card, `getAncestralComum`, espelhamento bilateral). **Não reescreve** nem o B1 nem o 004 — apenas adiciona: (a) fusão visual+clique do tronco, (b) pré-preenchimento da vista a partir do lado clicado, (c) coloração do mapa por vista incl. circunferencial. As regras do B1/004 permanecem intocadas e válidas.
> **Base técnica**: `Docs/Discoverys/exame-fisico-fusao-poligonos/01_discovery.md` (Discovery concluído). Decisões técnicas já resolvidas lá não são reabertas aqui.

## 1. Contexto e motivação

No B1 (briefing `2026-06-08_005`) a vista circunferencial entrou no catálogo (9 nós `{base}-circunferencial`) e a modal ganhou o passo de vista (anterior / posterior / circunferencial). Mas o B1 **deixou intencionalmente o mapa corporal sem refletir a vista**: o nó circunferencial não tem path SVG, então `BodyMap` não acende nada para um card circunferencial (B1 R8/CA25). Esse gap é o que o B2 fecha.

Além disso, o tronco no mapa hoje é um único polígono geométrico (`M_TORSO_ANT/POST`, `F_TORSO_ANT/POST`) **fatiado por hitbox** em 3 faixas horizontais via clip-paths (`clip-torax-ant`, `clip-abdome-ant`, `clip-pelve-ant` no anterior; `clip-torax-post`, `clip-lombo-post`, `clip-pelve-post` no posterior). As fronteiras tórax/abdome/pelve são retas horizontais arbitrárias por faixa-Y — clinicamente fracas. O PO aprovou explicitamente (DP-4) a **fusão clínica**: tórax+abdome+pelve = 1 tronco anterior; tórax+lombossacra+pelve = 1 tronco posterior. Fundir elimina a fronteira arbitrária e dá ao mapa 1 alvo de clique por vista do tronco.

Evidência de código (estado atual verificado):
- `frontend/src/components/exame-fisico/bodyMapPaths.ts` — 6 entradas de tronco (3 por vista, por sexo) reusam o mesmo `d` (`M_TORSO_*`/`F_TORSO_*`) cada uma com um `clipId` diferente. Cabeçalho diz "Auto-generated … DO NOT EDIT MANUALLY" — **enganoso**: não existe gerador no repo; editar à mão é seguro (Discovery §1.1/§2).
- `frontend/src/components/exame-fisico/BodyMap.vue` — define os `<clipPath>` `<rect>` no `<defs>`; renderiza 1 `<path>` por `regioesComPath` (nível-1 cujo **`nome`** casa `currentPaths[r.nome]`), com `clip-path` quando há `clipId`; `isExaminada = regioesExaminadas.includes(r.id)`; hover de membro via `getMembroGroup`/`MEMBRO_RE`; divisão física anterior `x<700` / posterior `x>700`.
- `frontend/src/components/prontuario/secoes/SecaoExameFisico.vue` — `regioesExaminadasMapa` (computed): parte de `regioesJaSelecionadas` e, **só para entradas bilaterais de membro**, adiciona o nível-1 oposto via `getOpostoNivel1Id`. É a **única** generalização de "acender além do clicado" existente. Card circunferencial guarda `regiao_id = {base}-circunferencial` (sem path) → hoje não acende nada (o gap).
- `frontend/src/components/exame-fisico/RegionSelectorPopup.vue` — `RAMOS_CIRCUNFERENCIAL` (mapa `{base}-circunferencial → { anterior, posterior }`, incl. exceção `abdome-circunferencial → { anterior: 'abdome-anterior', posterior: 'lombossacra-posterior' }`) vive **local** aqui; máquina de passos `'lado' | 'vista' | 'subregioes'`; `vistaEscolhida` inicia `null`.

## 2. Persona-alvo

Profissional de saúde (médico/fisioterapeuta), durante o atendimento, preenchendo o exame físico estruturado da evolução, usando o mapa corporal. Frequência: a cada consulta que registra exame físico — clica no tronco/membro e espera que o mapa reflita visualmente o que foi examinado, na vista correta.

## 3. Escopo

**Inclui**:

1. **Fusão dos polígonos do tronco no mapa** (apenas o tronco — cabeça, pescoço e membros ficam exatamente como estão):
   - Passar a renderizar **1 hotspot "Tronco (anterior)"** e **1 hotspot "Tronco (posterior)"**, usando o mesmo `d` já existente (`M_TORSO_ANT`/`M_TORSO_POST`, `F_TORSO_ANT`/`F_TORSO_POST`) **sem `clipId`** (path inteiro = 1 alvo).
   - As 6 faixas atuais (`Tórax/Abdome/Pelve (anterior)`, `Tórax/Região lombossacra/Pelve (posterior)`) **deixam de ser hotspots clicáveis**; os 6 `<clipPath>` de tronco no `<defs>` do `BodyMap` são removidos (clip-paths de tronco; nenhum outro clip existe).
   - Aplicar para **M e F** (dois conjuntos de paths — `maleRegionPaths` e `femaleRegionPaths`).
2. **Clique no tronco fundido → modal de tronco**:
   - O clique no tronco abre a modal já existente (B1). O passo de vista **pré-seleciona** a vista conforme o lado clicado (clicou no desenho anterior → vista pré-marcada "anterior"; posterior → "posterior"), **mas mantém as 3 opções (anterior / posterior / circunferencial) visíveis e editáveis** — modelo híbrido M3 (DP-2). Não remove o passo de vista do B1.
   - O passo de sub-regiões **lista as sub-regiões das partes do tronco daquela vista, agrupadas por parte** (DP-3): no anterior, Tórax / Abdome / Pelve; no posterior, Tórax / Região lombossacra / Pelve — reusando o motor de agrupamento por cabeçalho do B1. 1 confirmação = 1 card (004/B1).
3. **Coloração ("acender") do mapa por vista**, ao confirmar um card:
   - Vista **anterior** → acende o polígono anterior da região.
   - Vista **posterior** → acende o polígono posterior.
   - Vista **circunferencial** → acende **ambos** (anterior + posterior), tratando a exceção do abdome (anterior = `abdome-anterior`, posterior = `lombossacra-posterior`).
   - Para o tronco fundido, acender o polígono de tronco da vista correspondente quando **qualquer** parte daquela vista estiver examinada ("OU das partes" — Discovery §5 saída i; sem nó "tronco" no catálogo, DP-1).
   - Generalização **aditiva** (via `Set`), **preservando** o espelhamento bilateral de membros existente.
4. **Extrair `RAMOS_CIRCUNFERENCIAL` para módulo compartilhado** (reuso entre modal e mapa — princípio Reuso > duplicação).
5. **Corrigir o cabeçalho enganoso** de `bodyMapPaths.ts` (de "Auto-generated … DO NOT EDIT MANUALLY" para algo verdadeiro — ex.: "Path data extraído manualmente dos SVGs CORPO-Padrão; editável à mão"). Mudança cirúrgica de comentário, na mesma entrega.

**Não inclui** (explicitamente fora — B3, já resolvido, ou mudança de modelo):
- **UI admin de sub-regiões** (B3).
- **Qualquer mudança no catálogo / seed / persistência / migration** (DP-1 — B2 é **frontend-only**). Nenhum nó "tronco" novo, nenhuma coluna, nenhum endpoint.
- **Reescrita do B1**: o passo de vista com 3 opções permanece; aqui só se acrescenta o **pré-preenchimento** da vista pelo lado clicado e a **coloração** do mapa.
- Fusão de cabeça / pescoço / membros (ficam como estão — clip não existe neles; só o tronco tem clip).
- Edição/split de um card circunferencial de volta em anterior+posterior.

## 4. Regras de negócio

> Todas as regras deste B2 são **frontend** (mapa, modal, highlight). Não há regra de backend nova — a persistência, o multi-tenant e o audit do exame físico permanecem exatamente como no B1/004 (nó agregador resolve a vista pelo código; nenhuma coluna/campo novo).

- **R1 — tronco fundido = 1 hotspot por vista, por sexo**: `bodyMapPaths.ts` passa a expor `'Tronco (anterior)'` (d = `M_TORSO_ANT` / `F_TORSO_ANT`, `zOrder: 0`, **sem `clipId`**) e `'Tronco (posterior)'` (d = `M_TORSO_POST` / `F_TORSO_POST`, `zOrder: 0`, **sem `clipId`**) em **ambos** `maleRegionPaths` e `femaleRegionPaths`. As 6 entradas de faixa (`Tórax/Abdome/Pelve (anterior)`, `Tórax/Região lombossacra/Pelve (posterior)`) **não são mais renderizadas como hotspots clicáveis** no mapa. Mora em: `bodyMapPaths.ts` (paths) + `BodyMap.vue` (render/clique). Validada em: front (lógica testável de quais hotspots de tronco existem; visual fica para validação do usuário em prod — ver §8).

- **R2 — remoção dos clip-paths do tronco**: os 6 `<clipPath>` de tronco no `<defs>` do `BodyMap.vue` (`clip-torax-ant`, `clip-abdome-ant`, `clip-pelve-ant`, `clip-torax-post`, `clip-lombo-post`, `clip-pelve-post`) são removidos; o `clip-path` deixa de ser aplicado ao tronco. Cabeça/pescoço/membros **não usam clip** — não são afetados. Mora em: `BodyMap.vue`. Validada em: front.

- **R3 — clique no tronco pré-seleciona a vista (M3, DP-2)**: clicar no polígono `Tronco (anterior)` abre a modal com a vista **pré-selecionada como "anterior"**; clicar em `Tronco (posterior)` pré-seleciona "posterior". As **3 opções** (anterior / posterior / circunferencial) permanecem **visíveis e editáveis** no passo de vista (não reescreve o B1). O profissional pode trocar para circunferencial (ou para a vista oposta). Mora em: `BodyMap.vue` (emite clique de tronco com a vista) + `SecaoExameFisico.vue` (`onRegiaoClicada`) + `RegionSelectorPopup.vue` (`vistaEscolhida` inicial pré-preenchida, passo de vista permanece). Validada em: front.

- **R4 — modal de tronco lista sub-regiões agrupadas por parte (DP-3)**: ao clicar no tronco, o passo de sub-regiões exibe as sub-regiões das partes do tronco **daquela vista**, **agrupadas por parte** com cabeçalho por parte, reusando o motor de agrupamento do B1 (o B1 já agrupa por "Anterior"/"Posterior" no modo circunferencial; aqui o agrupamento é por parte do tronco). Anterior → grupos **Tórax**, **Abdome**, **Pelve** (`getFilhos('torax-anterior')`, `getFilhos('abdome-anterior')`, `getFilhos('pelve-anterior')`). Posterior → grupos **Tórax**, **Região lombossacra**, **Pelve** (`getFilhos('torax-posterior')`, `getFilhos('lombossacra-posterior')`, `getFilhos('pelve-posterior')`). 1 confirmação = 1 card; o `regiao_id` do card vem de `getAncestralComum` sobre as sub-regiões marcadas (lógica 004/B1). Mora em: `RegionSelectorPopup.vue` (lista agrupada por parte) + `SecaoExameFisico.vue` (`getAncestralComum`). Validada em: front. **DP-1**: se o dev concluir que é **impossível** resolver o `regiao_id` do card sem um nó "tronco" no catálogo, **PARAR e devolver ao BA** — não inventar migration.

- **R5 — coloração por vista (anterior/posterior)**: ao acender o mapa, um card de vista **anterior** acende o polígono **anterior** da região; vista **posterior** acende o **posterior**. Comportamento herdado para cabeça/pescoço/membros/partes do tronco. Mora em: `SecaoExameFisico.vue` (`regioesExaminadasMapa`) + `BodyMap.vue` (`isExaminada`). Validada em: front (lógica) + visual (usuário em prod).

- **R6 — coloração circunferencial acende ambos os lados (incl. exceção abdome)**: para cada card cujo `regiao_id` termina em `-circunferencial` (ou cuja `vista === 'circunferencial'`), o highlight do mapa adiciona **ambos** os ramos `RAMOS_CIRCUNFERENCIAL[regiao_id].anterior` **e** `.posterior` ao conjunto de ids acesos. Inclui a **exceção clínica do abdome**: `abdome-circunferencial → { anterior: 'abdome-anterior', posterior: 'lombossacra-posterior' }` — acende `Abdome (anterior)` à esquerda e `Região lombossacra (posterior)` à direita. Mora em: `SecaoExameFisico.vue` (`regioesExaminadasMapa`, usando o módulo compartilhado). Validada em: front (teste de lógica: `regioesExaminadasMapa` contém os 2 ids certos por vista, incl. abdome↔lombossacra).

- **R7 — coloração do tronco fundido por "OU das partes" (DP-1, sem migration)**: como **não** há nó "tronco" no catálogo, o polígono `Tronco (anterior)` acende quando **qualquer** id de parte anterior do tronco (`torax-anterior`, `abdome-anterior`, `pelve-anterior`) estiver no conjunto de examinadas; `Tronco (posterior)` acende quando qualquer id posterior (`torax-posterior`, `lombossacra-posterior`, `pelve-posterior`) estiver. Um card circunferencial de tórax/abdome/pelve, ao expandir (R6) para anterior+posterior, faz **ambos** os polígonos de tronco acenderem (fecha o loop visual de "circunferencial"). Implementado via mapa auxiliar `PARTE_PARA_TRONCO` (mapa-only, no módulo compartilhado) consumido pelo `BodyMap`. Mora em: módulo compartilhado (`PARTE_PARA_TRONCO`) + `BodyMap.vue` (decisão de `isExaminada` do tronco). Validada em: front (teste de lógica).

- **R8 — generalização do highlight é aditiva, preserva o espelhamento bilateral**: a expansão circunferencial (R6) é adicionada ao **mesmo `Set`** que já recebe o espelhamento bilateral de membro existente (`getOpostoNivel1Id`). Bilateral × circunferencial **coexistem**: um membro circunferencial bilateral acende 4 polígonos (anterior+posterior de cada lado). A expansão circunferencial **nunca substitui** nem remove a lógica bilateral. Mora em: `SecaoExameFisico.vue` (`regioesExaminadasMapa`). Validada em: front (teste de regressão: bilateral simples = 2 polígonos; bilateral+circunferencial = 4).

- **R9 — `RAMOS_CIRCUNFERENCIAL` vira fonte única compartilhada**: o mapa `RAMOS_CIRCUNFERENCIAL` (hoje local em `RegionSelectorPopup.vue`) é **extraído** para um módulo compartilhado (sugerido `frontend/src/components/exame-fisico/regioesCircunferenciais.ts`) e **importado** tanto pela modal (substituindo a cópia local, sem duplicar a regra) quanto por `SecaoExameFisico.vue` (coloração). O conteúdo do mapa **não muda** (mesmas 9 entradas, mesma exceção do abdome). O mapa-only `PARTE_PARA_TRONCO` vive no mesmo módulo. Mora em: novo módulo + `RegionSelectorPopup.vue` (import) + `SecaoExameFisico.vue` (import). Validada em: front (a modal continua resolvendo filhos circunferenciais idêntico ao B1 — não-regressão).

- **R10 — não-regressão do highlight de membro / bilateral / hover**: o hover de membro (`getMembroGroup`/`MEMBRO_RE` acendendo os dois lados do mesmo tipo+vista) e o espelhamento bilateral continuam exatamente como hoje. O hover sobre o tronco fundido acende o polígono inteiro da vista (trivial, pois é 1 path). Mora em: `BodyMap.vue` + `SecaoExameFisico.vue`. Validada em: front.

## 5. Modelo de dados

**Nenhuma alteração de modelo de dados (DP-1).** B2 é **frontend-only**:
- Nenhuma migration, nenhuma coluna nova, nenhum nó novo no catálogo `regioes_anatomicas_catalogo`, nenhum índice. Os 9 nós circunferenciais já existem (B1, migration aplicada).
- Nenhuma alteração em `exame_fisico_regioes`, no payload `RegistrarExameFisicoInput`, em handler ou query.
- Os pseudo-hotspots "Tronco (anterior)"/"Tronco (posterior)" são **nós sintéticos de UI** — existem só em `bodyMapPaths.ts` e na lógica de render do `BodyMap`; não têm correspondente no catálogo. A granularidade clínica (tórax/abdome/pelve) e o `regiao_id` do card continuam vindo da seleção de sub-regiões + `getAncestralComum` (lógica 004/B1).
- `imedto-database` **não é acionado** neste briefing. Se o dev concluir que o `regiao_id` do card de tronco é impossível sem nó de catálogo, **PARA e devolve ao BA** (não inventa migration — DP-1).

**Multi-tenant**: inalterado. B2 não toca query/comando de domínio; o filtro de `estabelecimento_id` na persistência do exame físico permanece como na auditoria multi-tenant concluída.

**LGPD/audit**: inalterado. Nenhuma nova exposição de PII; nenhum endpoint/DTO novo. Mapa corporal, modal e highlight operam sobre metadados clínicos de catálogo (não-PII) já carregados. O audit de acesso/escrita de prontuário existente continua valendo.

## 6. UX e fluxo

**Mapa corporal (BodyMap)**:
- O tronco aparece como **1 área clicável por vista** (anterior à esquerda, `x<700`; posterior à direita, `x>700`). Sem fronteiras horizontais internas (faixas removidas).
- Hover sobre o tronco → realça o polígono inteiro daquela vista.
- Card examinado acende o(s) polígono(s) conforme a vista (R5/R6/R7).

**Fluxo de clique no tronco (ex.: anterior)**:
1. Profissional clica no tronco anterior → modal abre.
2. **Passo vista** (B1): vem com **"Anterior" pré-selecionado**, mas as 3 opções visíveis/editáveis (pode trocar para Circunferencial ou Posterior). (R3, M3)
3. **Passo sub-regiões**: lista agrupada por parte — cabeçalhos **Tórax**, **Abdome**, **Pelve**, cada um com seus filhos. Marca N sub-regiões (pode cruzar partes). (R4)
4. Confirma → **1 card único** em "Regiões examinadas (N)"; `regiao_id` resolvido por `getAncestralComum`; badge de vista (B1) = `Anterior` (ou `Circunferencial`/`Posterior` se trocado).
5. Mapa acende: `Anterior` → polígono `Tronco (anterior)`; `Circunferencial` → `Tronco (anterior)` **e** `Tronco (posterior)`.

**Estados**:
- **Vista não escolhida** → não avança (herdado do B1 CA26); com pré-seleção (R3), o passo de vista já vem com uma opção marcada, então o avanço pode ocorrer, mas as opções permanecem editáveis.
- **Nenhuma sub-parte marcada** → Confirmar desabilitado (`totalSelecionados === 0`); nenhum card; nada acende (herdado 004/B1).
- **Lista de regiões examinadas vazia** → subseção não renderiza; mapa sem nenhum polígono aceso (comportamento atual).
- **Sexo F vs M** → o tronco fundido funciona em ambos (paths `F_TORSO_*` e `M_TORSO_*`); esquecer um sexo = tronco não acende para metade dos pacientes (item de checklist, §8).

**Design system**: reuso de `BodyMap`, `RegionSelectorPopup`, `RegionExamCard`, `AppPillToggle` (passo de vista, do B1), `AppModal`. **Nenhum componente novo.** A mecânica de "hotspot fundido + acender por vista" é registrada em `Docs/DESIGN.md` (§10). Mobile: inalterado (mapa já é responsivo via `viewBox`).

## 7. Critérios de aceite (testáveis)

> Numeração continua a partir do B1 (que terminou em CA29). Estes são CAs incrementais do B2. Onde a validação visual não é possível no sandbox de QA, o CA é redigido sobre a **lógica testável** (montagem de ids, existência de hotspots), com a validação visual delegada ao usuário em prod (§8).

- **CA30 (fusão — tronco vira 1 hotspot por vista, masculino)**: Dado o mapa renderizado para paciente **masculino**, Quando `regioesComPath`/os hotspots de tronco são montados, Então existe exatamente **1** hotspot de nome `Tronco (anterior)` (d = `M_TORSO_ANT`, sem `clipId`) e **1** de nome `Tronco (posterior)` (d = `M_TORSO_POST`, sem `clipId`), e **não** existem hotspots clicáveis para `Tórax/Abdome/Pelve (anterior)` nem `Tórax/Região lombossacra/Pelve (posterior)`.

- **CA31 (fusão — feminino)**: Dado o mapa renderizado para paciente **feminino**, Quando os hotspots de tronco são montados, Então existem `Tronco (anterior)` (d = `F_TORSO_ANT`) e `Tronco (posterior)` (d = `F_TORSO_POST`), ambos sem `clipId`, e nenhuma faixa de tronco clicável — confirmando paridade M/F.

- **CA32 (clip-paths de tronco removidos)**: Dado o `<defs>` do `BodyMap`, Quando o componente renderiza, Então os 6 `<clipPath>` de tronco (`clip-torax-ant`, `clip-abdome-ant`, `clip-pelve-ant`, `clip-torax-post`, `clip-lombo-post`, `clip-pelve-post`) **não** existem mais e nenhum `clip-path` é aplicado ao polígono de tronco; cabeça/pescoço/membros renderizam sem regressão.

- **CA33 (clique no tronco pré-seleciona a vista — M3)**: Dado o clique no polígono `Tronco (anterior)`, Quando a modal abre, Então o passo de vista vem com **"Anterior" pré-selecionado** e as 3 opções (Anterior / Posterior / Circunferencial) permanecem visíveis e editáveis; análogo para `Tronco (posterior)` → "Posterior" pré-selecionado.

- **CA34 (modal de tronco lista sub-regiões agrupadas por parte — anterior)**: Dado o tronco **anterior** clicado, Quando o passo de sub-regiões carrega, Então a lista contém os filhos de `torax-anterior`, `abdome-anterior` e `pelve-anterior` **agrupados por parte** (cabeçalhos Tórax / Abdome / Pelve), na ordem `ordem` de cada parte.

- **CA35 (modal de tronco — posterior usa lombossacra)**: Dado o tronco **posterior** clicado, Quando o passo de sub-regiões carrega, Então os grupos são Tórax (`torax-posterior`), **Região lombossacra** (`lombossacra-posterior`) e Pelve (`pelve-posterior`) — e **não** um inexistente "abdome-posterior".

- **CA36 (1 card por confirmação no tronco)**: Dado sub-regiões de mais de uma parte do tronco marcadas, Quando o profissional confirma, Então **exatamente 1** card é adicionado e o contador incrementa em 1; o `regiao_id` é resolvido por `getAncestralComum` (sem nó "tronco" no catálogo — DP-1).

- **CA37 (coloração anterior)**: Dado um card de vista **anterior** de uma parte do tronco (ex.: `torax-anterior`), Quando o mapa renderiza, Então `regioesExaminadasMapa` faz acender o polígono `Tronco (anterior)` e **não** o `Tronco (posterior)`.

- **CA38 (coloração posterior)**: Dado um card de vista **posterior** (ex.: `lombossacra-posterior`), Quando o mapa renderiza, Então acende `Tronco (posterior)` e não `Tronco (anterior)`.

- **CA39 (coloração circunferencial acende ambos — tórax)**: Dado um card `torax-circunferencial`, Quando o mapa renderiza, Então `regioesExaminadasMapa` contém `torax-anterior` **e** `torax-posterior`, fazendo acender **ambos** `Tronco (anterior)` e `Tronco (posterior)`.

- **CA40 (coloração circunferencial — exceção abdome↔lombossacra)**: Dado um card `abdome-circunferencial`, Quando o mapa renderiza, Então `regioesExaminadasMapa` contém `abdome-anterior` **e** `lombossacra-posterior` (não um `abdome-posterior` inexistente), acendendo `Tronco (anterior)` e `Tronco (posterior)`.

- **CA41 (coloração circunferencial — membro)**: Dado um card `msd-circunferencial`, Quando o mapa renderiza, Então `regioesExaminadasMapa` contém `msd-anterior` **e** `msd-posterior`, acendendo os dois polígonos do membro superior direito (anterior e posterior), sem tocar o tronco.

- **CA42 (não-regressão bilateral)**: Dado um card de membro **bilateral anterior** (ex.: `msd-anterior` bilateral), Quando o mapa renderiza, Então acende `msd-anterior` **e** `mse-anterior` (espelhamento existente), exatamente como antes do B2.

- **CA43 (composição bilateral × circunferencial = 4 polígonos)**: Dado um card de membro superior **bilateral circunferencial**, Quando o mapa renderiza, Então `regioesExaminadasMapa` contém `msd-anterior`, `msd-posterior`, `mse-anterior` e `mse-posterior` (4 polígonos), confirmando que a expansão circunferencial é **aditiva** e não anula o espelhamento bilateral.

- **CA44 (módulo compartilhado — não-regressão da modal B1)**: Dado `RAMOS_CIRCUNFERENCIAL` extraído para o módulo compartilhado e importado pela modal, Quando o modo circunferencial do B1 é exercitado (lista agrupada Anterior/Posterior, exceção abdome→lombossacra, 1 card), Então o comportamento do B1 (CA17/CA18/CA19) permanece idêntico — sem duplicação da regra.

- **CA45 (não-regressão B1/004 — vista pura + lado)**: Dado os fluxos de vista anterior/posterior pura e os fluxos de lado do 004 (D/E/bilateral, badge de lado, agregação de templates, dedupe), Quando exercitados, Então tudo continua funcionando; o pré-preenchimento de vista (R3) não impede a troca de vista nem regride o passo de lado de membro.

- **CA46 (multi-tenant — não-regressão)**: Dado um usuário do estabelecimento B, Quando registra/visualiza exame físico, Então o comportamento multi-tenant do backend é inalterado (B2 é frontend-only, não toca query/comando de domínio); nenhum dado cross-tenant é exposto pela mudança de mapa/modal.

- **CA47 (LGPD/audit — não-regressão)**: Dado a interação com o mapa fundido e a coloração por vista, Quando ocorre, Então nenhuma PII nova aparece em log/mensagem; nenhum endpoint/DTO novo; o audit de prontuário existente continua valendo (sem mudança de backend).

- **CA48 (estados — nada marcado não acende)**: Dado o tronco clicado e a modal aberta sem nenhuma sub-região marcada, Quando o profissional fecha/confirma, Então nenhum card é criado e nenhum polígono de tronco acende; com lista de regiões vazia, o mapa não exibe polígono aceso.

## 8. Riscos e dependências

- **QA sem chrome-devtools (alto, para validação visual)**: confirmado no histórico — o sandbox de QA não roda browser; banco em container EC2 sem túnel. A **validação visual** (tronco fundido aparece como 1 área, acende na vista certa, cores corretas, cross-browser) fica **para o usuário em prod**. Mitigação obrigatória: cobrir a **lógica** com testes unitários — `regioesExaminadasMapa` retorna os ids certos por vista (anterior/posterior/circunferencial, incl. abdome↔lombossacra), `PARTE_PARA_TRONCO` mapeia as 6 partes para os 2 troncos, bilateral×circunferencial = 4 polígonos, hotspots de tronco existem em M e F sem `clipId`. Esses testes não exigem DOM real.
- **Sexo M/F — dobra o trabalho (baixo, mas armadilha)**: toda entrada nova de tronco entra em **ambos** `maleRegionPaths` e `femaleRegionPaths`. Esquecer um sexo = tronco não acende para metade dos pacientes. Checklist no §9; coberto por CA30+CA31.
- **Lookup por `nome` (armadilha)**: `BodyMap` casa path por **`nome`** exato. A chave dos novos paths deve ser exatamente `'Tronco (anterior)'` / `'Tronco (posterior)'` e o `PARTE_PARA_TRONCO` deve usar essas strings idênticas; divergência = silenciosamente não renderiza/não acende. Como os nós de tronco são **sintéticos** (não vêm do catálogo via `regioesComPath`, que filtra nível-1 do catálogo), o `BodyMap` precisa **injetar/renderizar** os 2 troncos por conta própria (o catálogo não tem nó "Tronco"). Coberto por CA30/CA32.
- **Regressão do highlight bilateral (médio)**: `regioesExaminadasMapa` é o caminho mais sensível. A expansão circunferencial deve ser **aditiva** (Set), nunca substituir o espelhamento bilateral. Coberto por CA42/CA43.
- **DP-1 / regiao_id do card de tronco (gatilho de PARADA)**: se o dev concluir que resolver o `regiao_id` do card de tronco é **impossível** sem um nó "tronco" no catálogo (i.e., `getAncestralComum` não cobre uma seleção que cruza tórax+abdome+pelve), **PARAR e devolver ao BA** — não criar migration. O Discovery (§3 Opção 2b) indica que `getAncestralComum` cobre o caso; este risco é o ponto de verificação.
- **Dependência**: B2 depende do B1 (já entregue, validado, migration aplicada). Não há `imedto-database` neste briefing. Não há feature externa bloqueante.

## 9. Observações para execução

**Sequência de agentes**: `imedto-developer` (frontend: `bodyMapPaths.ts`, `BodyMap.vue`, `SecaoExameFisico.vue`, `RegionSelectorPopup.vue`, novo módulo `regioesCircunferenciais.ts`, testes) → `imedto-qa` (valida CAs por testes de lógica + análise de código; validação visual delegada ao usuário em prod). **`imedto-database` NÃO é acionado** (DP-1, frontend-only).

**Não-negociável**:
- **Frontend-only, zero migration** (DP-1). Nenhum nó "tronco" no catálogo. Se impossível, PARAR e devolver ao BA.
- Fusão **só do tronco**; cabeça/pescoço/membros intocados.
- Coloração: anterior→anterior, posterior→posterior, **circunferencial→ambos** (incl. abdome↔lombossacra). Expansão **aditiva** sobre o espelhamento bilateral (Set).
- Modelo **M3** (DP-2): clique pré-seleciona a vista, **mas mantém as 3 opções editáveis**. **Não reescrever o B1.**
- Lista de sub-regiões do tronco **agrupada por parte** (DP-3), reusando o motor de agrupamento do B1.
- Chave dos paths de tronco = exatamente `'Tronco (anterior)'` / `'Tronco (posterior)'`, em **M e F**.
- Briefings 004 e 005 (B1) permanecem **imutáveis**; este os estende.

**Liberdade técnica do dev**:
- Como o `BodyMap` injeta/renderiza os 2 pseudo-hotspots de tronco (lista interna fixa vs. prop) e como emite o clique sintético com a vista (`'tronco-anterior'`/`'tronco-posterior'` ou objeto equivalente) — desde que `SecaoExameFisico.onRegiaoClicada` traduza para abrir a modal no modo "lista agrupada por parte" com vista pré-resolvida.
- Como representar a pré-seleção da vista no `RegionSelectorPopup` (`vistaEscolhida` inicial via prop) preservando as 3 opções.
- Nome/local exato do módulo compartilhado (sugerido `frontend/src/components/exame-fisico/regioesCircunferenciais.ts`) e se `PARTE_PARA_TRONCO` mora nele.
- Onde montar a expansão circunferencial dentro de `regioesExaminadasMapa` (a forma do Discovery §9.2 é ilustrativa, não normativa).

**Reuso (não duplicar)**: `RAMOS_CIRCUNFERENCIAL` (extrair, não copiar); `getFilhos`, `getAncestralComum`, `getAncestorNivel1Id`, `getOpostoNivel1Id`, `MEMBRO_RE` (já em `SecaoExameFisico.vue`); `getMembroGroup` (já em `BodyMap.vue`); motor de agrupamento por cabeçalho e `AppPillToggle` (já em `RegionSelectorPopup.vue`, do B1).

## 10. Atualização de documentação

- **`Docs/DESIGN.md` — seção de componentes do mapa corporal / BodyMap**: registrar, de forma **incremental e cirúrgica**, a **nova mecânica do `BodyMap`**:
  - **Hotspot de tronco fundido**: o tronco é renderizado como **1 polígono clicável por vista** (`Tronco (anterior)` / `Tronco (posterior)`), sem clip-paths; as antigas faixas tórax/abdome/pelve deixaram de ser hotspots. Nota de que os nós de tronco são **sintéticos de UI** (não existem no catálogo).
  - **Highlight ("acender") por vista**: anterior acende o polígono anterior, posterior o posterior, **circunferencial acende ambos**; o tronco acende por "OU das partes". A regra circunferencial usa `RAMOS_CIRCUNFERENCIAL` (módulo compartilhado `regioesCircunferenciais.ts`), incl. a exceção `abdome↔lombossacra`; a expansão é **aditiva** sobre o espelhamento bilateral de membro.
  - Referência ao Discovery `Docs/Discoverys/exame-fisico-fusao-poligonos/01_discovery.md` como base técnica.
- **Demais docs**: **nenhuma alteração**. Sem mudança de infra (`INFRA.md`), comandos (`COMANDOS.md`), arquitetura backend (`ARQUITETURA.md` — frontend-only, sem nova regra de catálogo) ou LGPD (`LGPD.md` — sem novo PII/endpoint). Sem nova migration (DP-1).
