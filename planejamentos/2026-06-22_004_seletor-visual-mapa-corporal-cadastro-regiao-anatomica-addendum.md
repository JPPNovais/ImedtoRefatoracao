# Addendum — Clique no tronco abre seletor de pai nível-1 (substitui o no-op)

**Refere-se a**: 2026-06-22_004_seletor-visual-mapa-corporal-cadastro-regiao-anatomica.md
**ID**: 2026-06-22_004 (addendum)
**Status**: Aprovado por usuário em 2026-06-22 (decisão de produto fechada com o orquestrador)
**Autor**: imedto-business-analyst
**Tipo**: spec gap (Tipo B) — lacuna de cobertura, não bug de código
**Estimativa de esforço**: P
**Áreas regressivas tocadas**: exame físico (uso atual do BodyMap no prontuário) / catálogo admin de regiões anatômicas

> **Imutabilidade**: o briefing original NÃO é editado. Este addendum **supera o CA4 e a R4** do original (ver CA17). Tudo o mais do briefing original que não conflita permanece válido.

## 1. Spec gap descoberto no uso

O briefing original decidiu (R4 / CA4) que clicar nos pseudo-hotspots de tronco no modo criação seria um **no-op silencioso**, sob a justificativa de que o tronco é um nó sintético sem `codigo` no catálogo. A implementação seguiu o CA4 fielmente (`aoClicarTroncoNoMapa()` vazio em `RegioesGlobaisFormView.vue:161-163`) — **não é bug de código**.

O problema é de **cobertura**: no boneco (`BodyMap.vue`), as regiões nível-1 **Tórax**, **Abdome**, **Pelve** (anterior) e **Tórax**, **Região lombossacra**, **Pelve** (posterior) **não têm hotspot próprio** — elas são excluídas dos hotspots clicáveis pela constante `NOMES_TRONCO` (`BodyMap.vue:70-73`) e ficam **fundidas** nos blocos únicos "Tronco (anterior)" / "Tronco (posterior)" (pseudo-hotspots em `BodyMap.vue:90-105`). Consequência do no-op: pelo boneco o admin fica **impossibilitado de selecionar como pai** justamente as regiões do tronco — que são exatamente as que motivaram a feature. O atalho falha no caso central.

O exame físico já resolveu isso há tempos: clicar no tronco abre um **seletor agrupado** das partes nível-1 daquele lado (`SecaoExameFisico.onTroncoClicado`, linhas 426-434, com `GRUPOS_TRONCO_ANTERIOR`/`GRUPOS_TRONCO_POSTERIOR` linhas 382-395). O cadastro precisa espelhar esse comportamento — adaptado para o que o cadastro precisa (escolher só o **pai nível-1**, sem navegar filhos nem marcar achados).

## 2. Persona-alvo

Mantida do original: **admin global** cadastrando uma **nova** sub-região do catálogo, no modo **criação**. Frequência baixa, alto custo de erro (hierarquia imutável pós-criação).

## 3. Decisão de produto (nova regra)

Ao clicar no pseudo-hotspot do tronco, **no modo criação**, abrir um **seletor** com as partes nível-1 daquele lado; ao escolher uma parte, preencher "Código do pai" com o `codigo` dela (e "Vista" deriva pelo watcher existente, idêntico ao clique direto nos outros hotspots — R3 do original permanece). Espelha o comportamento que **já existe** no exame físico.

- **Tronco ANTERIOR** → opções: **Tórax (anterior)**, **Abdome (anterior)**, **Pelve (anterior)**.
- **Tronco POSTERIOR** → opções: **Tórax (posterior)**, **Região lombossacra (posterior)**, **Pelve (posterior)**.

Esses grupos são os mesmos `GRUPOS_TRONCO_ANTERIOR` / `GRUPOS_TRONCO_POSTERIOR` de `SecaoExameFisico.vue:382-395`; o mapa `PARTE_PARA_TRONCO` em `regioesCircunferenciais.ts` é a fonte compartilhada parte→tronco (e sua inversa dá as partes de cada lado).

## 4. Escopo (delta sobre o original)

**Inclui (novo)**:
- Substituir o no-op do clique no tronco (`aoClicarTroncoNoMapa`) por **abertura de um seletor** das partes nível-1 daquele lado.
- Ao escolher uma parte no seletor, preencher "Código do pai" com o `codigo` da parte escolhida (a "Vista" deriva pelo watcher existente).
- Tratar fechar/cancelar o seletor sem escolher (não altera nenhum campo).

**Não inclui (mantém o original e o que já estava fora)**:
- Qualquer mudança de backend (R8 do original) — **zero backend, zero migration** (confirmado: o seletor é derivável da árvore do catálogo `store.arvore` já carregada + `PARTE_PARA_TRONCO`).
- Geometria / `svg_coords` (R6 do original) — segue intacto, NULL.
- Coloração persistente / "estado aceso" no boneco (R6) — segue valendo.
- Modo **edição** — o seletor do mapa (e portanto o do tronco) **não aparece** na edição (R5 do original; hierarquia imutável).
- Seleção de pai **circunferencial** pelo boneco — segue fora (R7 do original); o seletor do tronco lista apenas as partes nível-1 anteriores/posteriores daquele lado, nunca os agregadores circunferenciais.
- Navegar filhos (nível 2/3) ou marcar achados pelo seletor do tronco — fora; o cadastro só escolhe o **pai nível-1**.

## 5. Regras de negócio (delta)

- **R9 (clique no tronco abre seletor — SUPERA R4)**: no modo criação, clicar em "Tronco (anterior)" / "Tronco (posterior)" **abre um seletor** com as partes nível-1 daquele lado (derivadas de `PARTE_PARA_TRONCO` invertido, cruzadas com `store.arvore` para obter `codigo`/`vista`/`nome`). A antiga R4 (no-op) fica **superada por esta R9**. Mora em: Front (`RegioesGlobaisFormView.vue`, handler `aoClicarTroncoNoMapa`). Sem espelho no back.
- **R10 (escolha no seletor preenche o pai)**: ao escolher uma parte no seletor do tronco, "Código do pai" recebe o `codigo` da parte escolhida e a "Vista" é re-derivada pelo **watcher existente** de "Código do pai" — resultado idêntico ao clique direto num hotspot ou à digitação manual do código (reaproveita R3 do original; `Vista` deriva do lado/vista da parte). Mora em: Front. Sem espelho no back.
- **R11 (cancelar não muda nada)**: fechar/cancelar o seletor do tronco sem escolher **não altera** nenhum campo do formulário. Mora em: Front.
- **R12 (campos seguem editáveis)**: após preencher pelo seletor do tronco, "Código do pai" e "Vista" permanecem **editáveis manualmente** (R3 do original segue valendo). Mora em: Front.
- **R13 (só criação)**: o seletor do tronco só existe no modo criação — na edição o boneco inteiro não é renderizado (R5 do original). Mora em: Front.
- **R14 (não-regressão do exame físico)**: o comportamento do tronco no exame físico (`SecaoExameFisico.vue` + `BodyMap.vue`) permanece **idêntico**. Se o dev optar por extrair `GRUPOS_TRONCO_*` para um módulo compartilhado (para reuso entre cadastro e exame físico), a extração é **refactor sem mudança de comportamento** — o exame físico continua abrindo o `RegionSelectorPopup` no modo "lista agrupada por parte" como hoje. Mora em: Front. Sem espelho no back.

## 6. Modelo de dados

**Nenhuma mudança de schema.** Confirmado:
- O seletor do tronco é 100% derivável de dados já carregados no front: `store.arvore` (`RegiaoAnatomicaNoDto[]`, com `codigo`/`vista`/`nome`/`nivel`) + `PARTE_PARA_TRONCO` (constante front em `regioesCircunferenciais.ts`). Nenhum endpoint, DTO, command ou coluna nova. `svg_coords` permanece NULL e não consumida.
- LGPD: catálogo anatômico sem PII — nada a alterar em `Docs/LGPD.md` (mantém o original).

## 7. UX e fluxo (delta)

- O boneco continua exatamente como no original (somente criação, sem coloração persistente). A única mudança observável é que **o clique no tronco deixa de ser inerte e passa a abrir um seletor**.
- **Forma do seletor — liberdade técnica do dev** (o addendum crava o comportamento observável, não a implementação): avaliar reusar o `RegionSelectorPopup` **ou** um seletor/popup mais simples só de nível-1. O cadastro só precisa escolher o **pai nível-1** (não navega filhos, não marca achados), então um seletor enxuto é aceitável e provavelmente mais adequado. Decisão fica com o dev, desde que satisfaça os CAs e não regrida o exame físico (R14).
- **Conteúdo do seletor**: lista das partes nível-1 do lado clicado, com rótulo legível (ex.: "Tórax", "Abdome", "Pelve"). Ao escolher, fecha o seletor e preenche "Código do pai".
- **Estados**:
  - **Vazio**: se, para o lado clicado, nenhuma das partes nível-1 existir/casar na árvore do catálogo (caso improvável), o seletor exibe um estado vazio claro (sem opção a escolher) e não preenche nada — sem erro global, sem travar o formulário.
  - **Erro/árvore não carregada**: se `store.arvore` ainda não carregou ao clicar no tronco, o clique é no-op silencioso (coerente com CA9 do original); ao recarregar/clicar com a árvore disponível, funciona normalmente.
- **Tipografia**: qualquer texto novo (título do seletor, rótulos das partes, estado vazio) via tokens (`--text-*`, `--font-weight-*`) — nunca literais (CLAUDE.md §5).
- **Acessibilidade / DS**: seletor usa componente do design system (popup/modal/lista do DS); botões/itens clicáveis com foco e `aria-label` apropriados.

## 8. Critérios de aceite (testáveis) — numerados a partir de CA13

> Os CAs do original (CA1–CA12) seguem válidos, **exceto o CA4**, superado por CA17 abaixo.

- **CA13 (tronco anterior abre seletor)**: Dado o admin global na tela de **criação**, Quando clica no pseudo-hotspot **"Tronco (anterior)"**, Então abre um seletor com exatamente as opções **Tórax (anterior)**, **Abdome (anterior)** e **Pelve (anterior)** (as partes nível-1 anteriores do tronco).

- **CA14 (escolha no seletor anterior preenche pai + vista)**: Dado o seletor do tronco anterior aberto, Quando o admin escolhe **"Abdome"**, Então "Código do pai" passa a conter o `codigo` de **abdome-anterior** (resolvido na árvore do catálogo) e "Vista" reflete **anterior** — exatamente como se o admin tivesse digitado esse código (watcher existente re-deriva vista/nível).

- **CA15 (tronco posterior abre seletor e preenche)**: Dado o admin em criação, Quando clica em **"Tronco (posterior)"**, Então abre um seletor com **Tórax (posterior)**, **Região lombossacra (posterior)** e **Pelve (posterior)**; E ao escolher **"Região lombossacra"**, "Código do pai" recebe o `codigo` de **lombossacra-posterior** e "Vista" reflete **posterior**.

- **CA16 (cancelar não muda nada)**: Dado o seletor do tronco aberto (anterior ou posterior), Quando o admin **fecha/cancela sem escolher** uma parte, Então **nenhum** campo do formulário é alterado e **nenhum** erro é exibido.

- **CA17 (supera o CA4/R4 do original)**: Dado o boneco no modo criação, Quando o admin clica no tronco, Então o comportamento **não é mais no-op silencioso** — passa a abrir o seletor (CA13/CA15). **Registro de imutabilidade**: o **CA4 e a R4** do briefing original ficam **SUPERADOS por este addendum**; toda validação do clique no tronco no cadastro segue as regras R9–R12 e os CA13–CA16.

- **CA18 (não-regressão do exame físico)**: Dado o uso do `BodyMap`/tronco na seção de exame físico do prontuário (`SecaoExameFisico.vue`), Quando o profissional clica no tronco após esta entrega, Então o comportamento permanece **idêntico** ao atual (abre o popup agrupado por parte, com a vista pré-selecionada pelo lado) — comprovado pelas suítes `BodyMap.test.ts`, `SecaoExameFisico.test.ts` e `RegionSelectorPopup.test.ts` **verdes** antes e depois. Se houver extração de `GRUPOS_TRONCO_*` para módulo compartilhado, ela não altera nenhum comportamento do exame físico.

- **CA19 (só na criação)**: Dado o admin abrindo a tela em modo **edição**, Quando a tela carrega, Então o boneco (e portanto o seletor do tronco) **não é renderizado** — não há como acionar o seletor do tronco na edição.

- **CA20 (tipografia via tokens + estados)**: Dado qualquer texto novo do seletor do tronco (título, rótulos das partes, estado vazio), Quando o CSS scoped é inspecionado, Então não há `font-size`/`font-weight` literais — apenas tokens; E o estado vazio do seletor é tratado (sem opção → mensagem clara, sem erro global); E `npm run check:typography -- --ci` permanece **verde**.

## 9. Riscos e dependências

- **Risco principal — regressão do exame físico (CA18)**: se o dev extrair `GRUPOS_TRONCO_*`/lógica do tronco para um módulo compartilhado, qualquer alteração de comportamento no `SecaoExameFisico`/`RegionSelectorPopup` é regressão. Mitigação: suítes existentes verdes antes/depois + QA valida o tronco no exame físico no app rodando. Preferir o **menor toque**: derivar as opções do tronco no cadastro a partir de `PARTE_PARA_TRONCO` invertido + `store.arvore`, **sem tocar** `SecaoExameFisico.vue`, se atender sem gambiarra.
- **Coerência nome/codigo na árvore**: o seletor depende de as partes nível-1 do tronco existirem na árvore do catálogo com `codigo`/`vista` corretos (mesma premissa que o exame físico já assume). Parte ausente simplesmente não aparece como opção (estado vazio tratado, CA20).
- **Premissa zero-backend**: mantida (R8 do original). Se durante a implementação surgir necessidade de back → para e escala ao BA (novo spec gap).

## 10. Observações para execução

- **Não-negociável**:
  - Reuso > duplicação: derivar as opções do tronco da fonte compartilhada `PARTE_PARA_TRONCO` (`regioesCircunferenciais.ts`) **invertida** + `store.arvore`; **não** hardcodar uma segunda lista de partes. Se extrair `GRUPOS_TRONCO_*` para módulo compartilhado, a extração **não pode** alterar o comportamento do exame físico (R14/CA18).
  - **Não tocar** `SecaoExameFisico.vue`/`BodyMap.vue` de forma que mude o exame físico (CA18).
  - Zero backend / zero `svg_coords` / zero migration (mantém R6/R8 do original). Se precisar do back → para e escala ao BA.
  - Seletor do tronco **só na criação** (R13).
  - Cancelar = nenhum campo muda (R11). Campos seguem editáveis (R12).
  - Tipografia via tokens (CA20).
- **Liberdade técnica do dev**:
  - **Forma do seletor**: reusar `RegionSelectorPopup` **ou** um seletor/popup simples só de nível-1 — decisão do dev. O cadastro só precisa escolher o **pai nível-1**; um seletor enxuto tende a ser o caminho mais limpo. Em qualquer caso, satisfazer CA13–CA16/CA20 e não regredir o exame físico (CA18).
  - **Onde mora a lógica das opções**: handler `aoClicarTroncoNoMapa` em `RegioesGlobaisFormView.vue:161-163` (hoje vazio) passa a abrir o seletor; a resolução parte→codigo reaproveita `encontrarNaArvore` (já existente na view) sobre `store.arvore`.
- **Reuso a conferir antes de codar**: `PARTE_PARA_TRONCO` (`regioesCircunferenciais.ts:28-35`), `GRUPOS_TRONCO_ANTERIOR`/`GRUPOS_TRONCO_POSTERIOR` (`SecaoExameFisico.vue:382-395`), `onTroncoClicado` (`SecaoExameFisico.vue:426-434`) como referência de comportamento, `RegionSelectorPopup.vue`, `encontrarNaArvore` + `store.arvore` + `regioesParaMapa`/`aoClicarRegiaoNoMapa` (`RegioesGlobaisFormView.vue:106-163`), watcher de "Código do pai" (que deriva vista/nível).

## 11. Atualização de documentação

- **`Docs/DESIGN.md` — seção "Segundo consumidor: seletor de pai no cadastro de região anatômica" (a partir da linha ~549)**: atualizar **incrementalmente** o bullet "**Tronco**". Hoje ele diz que o clique no tronco é "no-op silencioso". Trocar para: o clique no tronco, no modo criação, **abre um seletor com as partes nível-1 daquele lado** (Tórax/Abdome/Pelve anterior; Tórax/Região lombossacra/Pelve posterior — derivadas de `PARTE_PARA_TRONCO`); ao escolher uma parte, preenche "Código do pai" + "Vista" (espelha o comportamento do exame físico). Citar o addendum 2026-06-22_004. Não reescrever a seção inteira.
  - Se o dev **extrair** `GRUPOS_TRONCO_*` para um módulo compartilhado, registrar essa extração na subseção "**Módulo compartilhado**" (linha ~541), junto de `RAMOS_CIRCUNFERENCIAL`/`PARTE_PARA_TRONCO`. Se ficar só no reuso direto (sem extração), basta a atualização do bullet acima.
- **`Docs/LGPD.md` / `Docs/ARQUITETURA.md` / `Docs/INFRA.md` / `Docs/COMANDOS.md`** — **nenhuma alteração** (sem PII, sem novo padrão de arquitetura, sem infra, sem comando, zero migration).
