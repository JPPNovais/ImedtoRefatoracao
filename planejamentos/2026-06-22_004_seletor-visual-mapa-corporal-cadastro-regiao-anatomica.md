# Seletor visual (mapa corporal) no cadastro de região anatômica

**ID**: 2026-06-22_004
**Status**: Aprovado por usuário em 2026-06-22 (decisões fechadas pelo orquestrador)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: exame físico (uso atual do BodyMap no prontuário) / catálogo admin de regiões anatômicas

## 1. Contexto e motivação

No catálogo global de regiões anatômicas (admin global), ao cadastrar uma **nova** região o admin precisa informar manualmente o **"Código do pai"** e a **"Vista"** (anterior/posterior/circunferencial). Hoje isso é digitação cega: o admin tem que lembrar/conferir o código exato da região-pai de nível 1 (ex.: `ABD`, `TORAX`) e qual vista corresponde, o que é propenso a erro de digitação e exige conhecer a convenção de códigos do catálogo de cor.

O produto **já desenha um boneco/atlas corporal interativo** (`BodyMap.vue`), usado no exame físico do prontuário, onde o profissional clica numa parte do corpo. A demanda é **reaproveitar esse mesmo boneco** como atalho visual no formulário de cadastro: o admin clica na parte do corpo (bloco de nível 1) e os campos "Código do pai" e "Vista" são preenchidos automaticamente com o código e a vista daquela região nível 1 — reduzindo erro e tornando óbvia a hierarquia.

Premissa de projeto aplicada: **reuso > duplicação**. O boneco não é recriado; o desenho do mapa é **componentizado** para ser compartilhado entre o exame físico (uso atual) e o novo seletor do cadastro.

## 2. Persona-alvo

**Admin global** (operador do catálogo de regiões anatômicas), durante a tarefa de **cadastrar uma nova sub-região** no catálogo. Frequência baixa (tarefa de configuração/curadoria), mas com alto custo de erro: um código de pai errado cria hierarquia incoerente, e a hierarquia é **imutável após a criação** (R3 do catálogo: `nível = nível_pai + 1`, pai não muda). É o único papel com acesso à tela — catálogo é **global**, não pertence a estabelecimento.

## 3. Escopo

**Inclui**:
- Componentizar a parte do mapa corporal de `BodyMap.vue` para que possa ser reutilizada por dois consumidores: o exame físico (atual) e o novo seletor do cadastro — **sem duplicar paths, viewBox nem imagem de fundo, sem refazer o desenho**.
- Adicionar, **somente no fluxo de CRIAÇÃO** de `RegioesGlobaisFormView.vue`, um seletor visual (o mapa corporal) que, ao clicar num **bloco de nível 1**, preenche automaticamente os campos "Código do pai" e "Vista" do formulário com o código e a vista daquela região nível 1.
- O seletor é um **atalho transitório**: o admin pode ajustar/digitar manualmente "Código do pai" e "Vista" depois (o boneco não substitui os campos, apenas os pré-preenche).
- Definir e implementar o comportamento dos **pseudo-hotspots de tronco** (sintéticos, sem código real) ao serem clicados no contexto do cadastro.

**Não inclui**:
- Qualquer mudança de backend (entidade `RegiaoAnatomicaCatalogo`, commands `Criar`/`Atualizar`, banco). Ver R8 (premissa de zero-backend a confirmar).
- Persistir geometria: **não** usar nem gravar `svg_coords`/`SvgCoordsJson` (a coluna `svg_coords` permanece intacta, NULL). Sem migração de dados.
- Marcar/destacar permanentemente o boneco com a região recém-cadastrada (sem "estado aceso" persistido; é só atalho de clique).
- Cores de lateralidade (E/D) e seleção por lado — fora de escopo.
- Seleção de blocos de nível 2 ou 3 pelo boneco (o boneco só desenha hotspots de nível 1).
- Seleção de pai **circunferencial** pelo boneco (agregadores circunferenciais não têm posição no mapa — quem precisar digita manualmente, coerente com a regra atual R7 do catálogo).
- Adicionar o seletor ao fluxo de **EDIÇÃO** (na edição a hierarquia já está fixada → o seletor não se aplica).

## 4. Regras de negócio

- **R1 (reuso, não recriação)**: o desenho do mapa corporal (SVG, viewBox `0 25 1400 970`, imagem de fundo `corpo-bg-{feminino|masculino}.webp`, paths hardcoded de `bodyMapPaths.ts`, hotspots de nível 1 e pseudo-hotspots de tronco) é **componentizado** num componente compartilhado, consumido tanto pela seção de exame físico quanto pelo seletor do cadastro. Mora em: Front (componente do design system / `frontend/src/components/exame-fisico/`). Sem espelho no back (apresentação pura).

- **R2 (ação do clique em bloco de nível 1)**: ao clicar num hotspot de **nível 1** no seletor do cadastro, o formulário preenche automaticamente:
  - **"Código do pai"** ← o `codigo` da região nível 1 clicada (vindo do catálogo),
  - **"Vista"** ← a `vista` da mesma região nível 1 (`anterior` ou `posterior`).
  A "Vista" preenchida deve ser **coerente com o lado do mapa clicado** (lado anterior → `anterior`; lado posterior → `posterior`). Mora em: Front (handler do clique na view de cadastro). Sem espelho no back.

- **R3 (atalho, não substituto)**: o clique no boneco é **atalho**; "Código do pai" e "Vista" permanecem **editáveis manualmente** após o preenchimento automático. A lógica existente que deriva `vista`/`nivel` a partir do "Código do pai" digitado (watcher já existente na view, linhas ~68-90) continua valendo — preencher pelo boneco deve resultar no mesmo estado que digitar o código manualmente teria produzido (vista derivada do pai, nível = nível_pai + 1, guard de pai circunferencial via R7). Mora em: Front. Sem espelho no back.

- **R4 (clique em pseudo-hotspot de tronco)**: os blocos "Tronco (anterior)" / "Tronco (posterior)" são **pseudo-hotspots sintéticos** (não existem no catálogo, não têm `codigo` real). No contexto do cadastro, clicar no tronco **NÃO seleciona um pai** (não há código a preencher). Comportamento definido: o clique no tronco **não preenche nada** nos campos do formulário e **não gera erro** — é um no-op silencioso (o admin que quiser um pai do tronco — tórax/abdome/pelve etc. — digita o código manualmente, coerente com R7/R-circunferencial). Mora em: Front. Sem espelho no back. (Justificativa: o tronco fundido agrega partes que são clicáveis individualmente apenas no modal do exame físico; no cadastro não há esse drill-down e não cabe inventar um, para manter o escopo mínimo.)

- **R5 (somente criação)**: o seletor visual aparece **apenas no fluxo de criação** (`!editando`). No fluxo de edição, "Código do pai", "Vista" e "Nível" já são somente-leitura (a hierarquia é imutável pós-criação) → o seletor **não é renderizado** na edição. Mora em: Front (render condicional `v-if="!editando"`). Sem espelho no back.

- **R6 (sem geometria persistida)**: nada do clique é gravado como geometria; `svg_coords`/`SvgCoordsJson` permanece intacta (NULL). O boneco não fica "aceso"/marcado após o clique no contexto do cadastro (sem estado de seleção persistente no mapa — diferente do exame físico, que acende regiões examinadas). Mora em: Front (não passar `regioesExaminadas`/`vistasPorId` que acendam, ou passar vazio). Sem espelho no back.

- **R7 (catálogo é global; só admin global)**: a tela e o catálogo são **globais** (sem `estabelecimento_id`). Multi-tenant clássico (filtro por estabelecimento) **não se aplica** a este recurso — o controle é por **autorização de admin global**, que já é o contexto da rota/tela. O seletor não muda isso. Mora em: Back (autorização de admin global, já existente na rota do catálogo). Validada em: back (autorização da rota) + front (rota só acessível ao admin).

- **R8 (zero backend — premissa a confirmar)**: a expectativa é **zero mudança de backend** — toda a fonte de dados necessária (árvore do catálogo com `codigo`, `vista`, `nivel`, `nome`) já é exposta por `regioesGlobaisService.listarArvore(...)` → `RegiaoAnatomicaNoDto`. Se durante a implementação o dev descobrir que precisa de algo novo no back (novo endpoint, novo campo no DTO, mudança em command/entidade), **isto é spec gap (Tipo B)** → para e escala ao BA, não improvisa no back. Mora em: confirmação de premissa. Sem mudança esperada.

## 5. Modelo de dados

**Nenhuma mudança de schema.** Confirmações:

- Entidade `RegiaoAnatomicaCatalogo` (backend `Domain/Catalogo/`) — **intacta**. Campos relevantes para esta feature (apenas leitura): `Codigo` (único, imutável), `Nome`, `PaiCodigo`, `Nivel` (1=Raiz, 2=Sub-região, 3=Detalhe), `Vista` (`"anterior"|"posterior"|"circunferencial"|null`).
- Coluna `svg_coords` (JSONB, propriedade `SvgCoordsJson`) — **permanece NULL e não consumida**. Esta feature **não** lê, escreve, nem migra `svg_coords`. O motor de desenho continua sendo os paths hardcoded em `bodyMapPaths.ts` (front), que casam por **nome** (`currentPaths[r.nome]`). `svg_coords` **não é fonte de verdade** do desenho.
- Fonte de dados do seletor (somente leitura): `regioesGlobaisService.listarArvore(true)` → `RegiaoAnatomicaNoDto[]` (já tem `id`, `codigo`, `nome`, `paiCodigo`, `nivel`, `vista`, `lateralidade`, `ativo`, `filhos`). A view já carrega/usa essa árvore no watcher de "Código do pai" (`store.arvore`).
- Audit: as mutações do catálogo já exigem **"Motivo da alteração"** (≥10 chars) e gravam audit — **inalterado**. Esta feature não muda o fluxo de salvar.
- LGPD: catálogo anatômico é **baixo risco, sem PII**. Sem novo dado pessoal, sem novo endpoint que exponha PII. Nada a alterar em `Docs/LGPD.md`.

## 6. UX e fluxo

**Onde aparece**: dentro do `<AppCard>` de `RegioesGlobaisFormView.vue`, no bloco `<template v-if="!editando">` (campos somente-criação), próximo aos campos "Vista" / "Código do pai" — de forma que o admin veja a relação entre clicar no boneco e os campos preenchidos.

**Wireframe textual (modo criação)**:
```
┌─ Nova região anatômica ──────────────────────────────────────────┐
│ Código  [ ABD-SUP-D ]                                             │
│                                                                  │
│ ┌─ Selecionar pai pelo mapa (atalho) ──────────────────────────┐ │
│ │  Frente (anterior)            Costas (posterior)             │ │
│ │  ┌───────────────────────────────────────────────────────┐  │ │
│ │  │        [ boneco SVG com hotspots de nível 1 ]         │  │ │
│ │  │  (hover destaca; clicar preenche Vista + Cód. do pai) │  │ │
│ │  └───────────────────────────────────────────────────────┘  │ │
│ │  Dica: clique numa parte do corpo para preencher             │ │
│ │  "Código do pai" e "Vista" automaticamente.                  │ │
│ └──────────────────────────────────────────────────────────────┘ │
│                                                                  │
│ Vista          [ Anterior ▼ ]  (derivada do pai)                 │
│ Código do pai  [ ABD ]                                           │
│ ...                                                              │
└──────────────────────────────────────────────────────────────────┘
```

- **Componente do design system**: reutilizar o mapa corporal componentizado (R1). O seletor **não** exibe a coluna "Regiões examinadas" nem a legenda de vistas examinadas do exame físico — é só o boneco clicável. (A legenda anterior/posterior dos labels "Frente/Costas" do próprio mapa pode permanecer, pois ajuda a orientar o lado.)
- **Sexo do boneco**: o catálogo é genérico (não há paciente). Usar o boneco **masculino** como padrão (default já existente em `BodyMap` quando `sexo` é ausente). Não introduzir seletor de sexo — fora de escopo.
- **Estados**:
  - **Loading**: enquanto a árvore do catálogo carrega (`store.arvore` vazio), o seletor pode exibir o boneco já renderizado (os paths são estáticos) — o clique só resolve código/vista quando a árvore estiver disponível; se a árvore ainda não carregou ao clicar, o clique é no-op silencioso (sem erro). Preferir disparar o carregamento da árvore ao montar a tela de criação.
  - **Hover**: destaque do hotspot ao passar o mouse (comportamento nativo do `BodyMap`, já existente — `region-hover`).
  - **Pós-clique**: campos "Código do pai" e "Vista" preenchidos; sem marcação persistente no boneco (R6).
  - **Vazio/erro do catálogo**: se `listarArvore` falhar, o boneco continua visível mas o clique não preenche (no-op silencioso); o admin segue podendo digitar manualmente. Não bloquear o formulário por falha do atalho.
- **Tipografia**: qualquer texto novo (título do bloco, dica) via tokens (`--text-*`, `--font-weight-*`) — nunca literais (CLAUDE.md §5). Título do bloco como rótulo de campo/seção do DS.
- **Mobile-ready**: o `BodyMap` já é responsivo (`width: 100%`, `max-width`). O bloco do seletor deve colapsar bem na largura estreita do `app-page--narrow` do formulário.
- **Acessibilidade**: os hotspots já têm `role="button"` + `aria-label` (nome da região) no `BodyMap` — preservar.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — preenche pelo boneco)**: Dado o admin global na tela de **criação** de região, Quando clica num hotspot de **nível 1** do boneco (ex.: a região anterior cujo código de catálogo é `ABD`), Então o campo "Código do pai" passa a conter `ABD` e o campo "Vista" passa a refletir a vista daquela região (`anterior`), exatamente como se o admin tivesse digitado `ABD` no "Código do pai".

- **CA2 (coerência da vista com o lado clicado)**: Dado o boneco renderizado, Quando o admin clica num hotspot de nível 1 do **lado posterior** do mapa, Então a "Vista" preenchida é `posterior` (e não `anterior`); E quando clica num hotspot do **lado anterior**, a "Vista" preenchida é `anterior`.

- **CA3 (campos permanecem editáveis após o clique)**: Dado que o admin preencheu "Código do pai" e "Vista" clicando no boneco, Quando edita manualmente o campo "Código do pai" para outro código válido, Então o valor digitado prevalece e a "Vista"/"Nível" são re-derivados do novo pai (watcher existente), sem o boneco sobrescrever o valor digitado.

- **CA4 (clique no tronco é no-op)**: Dado o boneco no contexto do cadastro, Quando o admin clica num pseudo-hotspot de tronco ("Tronco (anterior)" ou "Tronco (posterior)"), Então **nenhum** campo do formulário é alterado e **nenhum** erro é exibido (no-op silencioso).

- **CA5 (somente na criação)**: Dado o admin abrindo a tela em modo **edição** de uma região existente, Quando a tela carrega, Então o seletor visual (boneco) **não é renderizado** (a hierarquia é imutável e os campos estruturais já são somente-leitura).

- **CA6 (sem marcação persistente / sem geometria)**: Dado que o admin clicou num hotspot, Quando observa o boneco após o clique, Então o boneco **não** fica com a região "acesa"/marcada de forma persistente; E nenhuma chamada de rede grava `svg_coords` (a coluna permanece NULL); E a request de criação enviada ao back **não inclui** geometria/`svg_coords`.

- **CA7 (não-regressão do exame físico)**: Dado o uso atual do `BodyMap` na seção de exame físico (`SecaoExameFisico.vue`) dentro do prontuário, Quando o profissional interage com o mapa após a componentização, Então todo o comportamento atual permanece idêntico — hotspots clicáveis de nível 1, hover, evento `regiaoClicada`, pseudo-hotspots de tronco com `troncoClicado`, coloração por vista (`vistasPorId`), legenda, layout lateral e expansão circunferencial — comprovado pela suíte de testes existente (`BodyMap.test.ts`, `SecaoExameFisico.test.ts`, `RegionSelectorPopup.test.ts`) verde.

- **CA8 (RBAC / autorização — admin global)**: Dado um usuário **sem** privilégio de admin global, Quando tenta acessar a rota do formulário de cadastro de região anatômica, Então o acesso é negado pela autorização já existente da área admin (a feature não cria nova superfície de acesso — o seletor vive dentro de uma tela já protegida).

- **CA9 (clique antes da árvore carregar)**: Dado que a árvore do catálogo ainda não foi carregada (ex.: rede lenta), Quando o admin clica num hotspot do boneco, Então o clique é no-op silencioso (sem erro, sem preenchimento parcial) — e ao recarregar/clicar após a árvore disponível, o preenchimento funciona normalmente (CA1).

- **CA10 (tipografia via tokens)**: Dado qualquer texto novo introduzido pelo bloco do seletor (título/rótulo/dica), Quando o CSS scoped é inspecionado, Então não há `font-size`/`font-weight` literais — apenas tokens (`--text-*`, `--font-weight-*`), e `npm run check:typography -- --ci` permanece verde.

- **CA11 (estados loading/erro do atalho não bloqueiam)**: Dado falha ao carregar a árvore do catálogo, Quando a tela de criação é exibida, Então o formulário permanece utilizável (admin digita "Código do pai" manualmente), o boneco continua visível e o clique apenas não preenche — sem bloquear o submit nem exibir erro global do formulário por causa do atalho.

- **CA12 (documentação viva atualizada)**: Dado que esta entrega componentiza o mapa corporal e adiciona um segundo consumidor, Quando a feature é validada, Então `Docs/DESIGN.md` (seção "Mapa corporal interativo") foi atualizada descrevendo a componentização compartilhada e o novo uso como seletor de pai no cadastro de região anatômica.

## 8. Riscos e dependências

- **Risco principal — regressão do exame físico (CA7)**: o `BodyMap` é usado em produção no prontuário. A componentização é refactor de estrutura; qualquer alteração de assinatura de props/eventos ou de CSS scoped pode quebrar o uso atual. Mitigação: a suíte `BodyMap.test.ts` / `SecaoExameFisico.test.ts` / `RegionSelectorPopup.test.ts` deve passar antes e depois; QA valida o exame físico no app rodando.
- **Atrito de shapes (atenção de implementação)**: o `BodyMap` atual casa região por **nome** (`currentPaths[r.nome]`) e tipa por `ExameFisicoRegiao`, que **não tem o campo `codigo`**. O seletor do cadastro precisa do `codigo` (para "Código do pai"). A árvore do catálogo (`RegiaoAnatomicaNoDto`) **tem `codigo`**. Logo, a fonte de dados que alimenta o mapa no contexto do cadastro precisa carregar o `codigo` junto e, ao receber o clique (que identifica a região por nome/id), resolver o `codigo`+`vista` correspondentes na árvore do catálogo. Isto é decisão de implementação do dev (mapear nome/id clicado → nó do catálogo), citada aqui para destravar — **não** muda contrato de back.
- **Dependência de coerência nome↔path**: o preenchimento depende de as regiões de nível 1 do catálogo terem `nome` que casa com as chaves de `bodyMapPaths.ts` — exatamente a mesma premissa que o exame físico já assume hoje. Regiões nível 1 sem path correspondente simplesmente não aparecem como hotspot (comportamento atual, aceitável).
- **Premissa zero-backend (R8)**: se falsa, vira spec gap Tipo B (volta ao BA) — não improvisar no back.

## 9. Observações para execução

- **Não-negociável**:
  - Reuso real do desenho (R1): **não** duplicar paths/viewBox/imagem; componentizar o que já existe. Antes de criar qualquer coisa nova, conferir `BodyMap.vue`, `bodyMapPaths.ts`, `regioesCircunferenciais.ts`.
  - Zero mudança de backend (R8) e zero uso de `svg_coords` (R6). Se precisar do back → para e escala ao BA.
  - Seletor **só na criação** (R5).
  - Clique no tronco = no-op silencioso (R4).
  - Tipografia via tokens (CA10), confirmações/feedback via DS se aplicável.
- **Liberdade técnica do dev**:
  - **Forma da componentização**: o dev decide a melhor estrutura para compartilhar o desenho sem regressão. Caminhos possíveis (ordem de preferência por menor risco): (a) reutilizar `BodyMap.vue` **as-is** no cadastro, passando uma fonte de regiões derivada da árvore do catálogo e tratando o `regiaoClicada`/`troncoClicado` na view de cadastro (o `BodyMap` já é genérico o bastante: recebe `regioes`, `regioesExaminadas`, emite eventos); ou (b) se for preciso isolar o "miolo" do SVG, extrair um subcomponente reutilizável e fazer tanto a seção de exame físico quanto o seletor consumirem-no. Preferir (a) se atender sem regressão — é o menor toque e já satisfaz "reuso > duplicação". Só ir para (b) se (a) exigir gambiarra. Em ambos os casos, o uso do exame físico precisa permanecer pixel/comportamento-idêntico (CA7).
  - **Como resolver `codigo`/`vista` no clique**: o `regiaoClicada` carrega o objeto da região; o dev resolve o nó correspondente na árvore do catálogo (`store.arvore` / `RegiaoAnatomicaNoDto`) para obter `codigo` e `vista`. Reusar a função `encontrarNaArvore` já existente na view se útil.
  - **Posição exata do bloco** dentro do `<template v-if="!editando">` e o copy da dica ficam a critério do dev, respeitando o DS e a tipografia por tokens.
- **Reuso a conferir antes de codar**: `BodyMap.vue` (props `regioes`/`regioesExaminadas`/`sexo`/`vistasPorId`, eventos `regiaoClicada`/`troncoClicado`), `bodyMapPaths.ts`, `regioesCircunferenciais.ts`, `regioesGlobaisService.listarArvore`, `RegiaoAnatomicaNoDto`, watcher e `encontrarNaArvore` em `RegioesGlobaisFormView.vue`, `useRegioesGlobaisStore` (`store.arvore`).

## 10. Atualização de documentação

- **`Docs/DESIGN.md` — seção "Mapa corporal interativo (BodyMap ...)"** (a partir da linha ~441): adicionar uma subseção curta registrando que (1) o desenho do mapa corporal é **reutilizado** por um segundo consumidor — o seletor visual de "Código do pai" no cadastro de região anatômica (`RegioesGlobaisFormView.vue`, admin global, modo criação); (2) nesse contexto o mapa é usado como **atalho transitório** (clique preenche "Código do pai" + "Vista"), sem coloração persistente, sem `vistasPorId`, e clique no tronco é no-op; (3) `svg_coords` continua **não** sendo fonte de verdade do desenho (paths hardcoded). Atualização **incremental** — não reescrever a seção existente. A subseção "Módulo compartilhado" pode ganhar a linha do novo consumidor se o dev extrair subcomponente (caminho (b)); se ficar no reuso direto (caminho (a)), basta a nota de novo uso.
- **`Docs/LGPD.md`** — nenhuma alteração (catálogo anatômico sem PII, sem novo endpoint/dado pessoal).
- **`Docs/ARQUITETURA.md` / `Docs/INFRA.md` / `Docs/COMANDOS.md`** — nenhuma alteração (sem novo padrão de arquitetura, sem infra, sem comando novo, zero migration).
