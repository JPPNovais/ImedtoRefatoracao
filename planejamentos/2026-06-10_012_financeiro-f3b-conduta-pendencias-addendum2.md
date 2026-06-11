# Financeiro F3B — Widget "Próximos passos" global e persistente (Addendum 2)

## Refere-se a: 2026-06-10_012_financeiro-f3b-conduta-pendencias.md

> Addendum 2 ao briefing F3B. Encadeia diretamente o **Addendum 1**
> (`2026-06-10_012_financeiro-f3b-conduta-pendencias-addendum.md`, CA190–CA201),
> que introduziu o widget flutuante "Próximos passos" substituindo o modal central.
>
> **Os dois documentos anteriores permanecem intocados.** Este addendum incrementa
> a numeração de CAs a partir de **CA202**.

**ID**: 2026-06-10_012 (addendum 2)
**Status**: Aprovado por usuário em 2026-06-11
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (widget), permissionamento (multi-tenant na limpeza), nenhuma de banco

---

## 1. Contexto e motivação

O usuário viu o widget "Próximos passos" do Addendum 1 rodando e validou o conceito.
Surgiram três dores reais de uso que pedem refinamento — **decisões já tomadas pelo
usuário**, registradas aqui sem reabertura:

1. **O widget desaparece ao sair do prontuário.** Hoje ele está montado dentro de
   `ProntuarioView.vue` (linhas 485–491) e só vive enquanto a view do prontuário está
   na tela. Na prática, o profissional salva a evolução, navega para criar o orçamento /
   registrar o procedimento / outra página, e o lembrete some — perdendo justamente o
   propósito de "próximos passos do atendimento". O widget precisa **acompanhar o usuário
   por todas as páginas autenticadas** até que as pendências daquela evolução sejam
   resolvidas ou ele feche explicitamente.

2. **Fechar o X descarta o contexto sem aviso.** Com pendências ainda abertas, um clique
   acidental no X faz o widget sumir. Precisa de uma confirmação leve que reforce que as
   pendências continuam vivas no painel do paciente.

3. **A pílula minimizada é ilegível.** Screenshot do usuário mostra um botão branco
   apagado que não comunica que há pendências. Precisa de cor primária sólida, contador
   "X/N" nítido e ícone visível.

**Nenhuma mudança de backend.** Tudo é camada de UX/estado no frontend. As pendências
continuam sendo criadas/concluídas/listadas pelos endpoints já existentes (F3B original +
Addendum 1).

## 2. Persona-alvo

Profissional (ou recepção operando em nome dele) logo após salvar uma evolução com ações
de conduta marcadas — durante o restante daquela sessão de atendimento, transitando entre
prontuário, orçamento, agenda e demais telas do app.

## 3. Escopo

**Inclui**:
- Promover o widget de componente local da `ProntuarioView` para **componente global**,
  montado no layout autenticado (`AppLayout.vue`), controlado por uma **Pinia store dedicada**.
- Persistência do estado do widget na sessão de navegação via **`sessionStorage`**
  (sobrevive a reload na mesma aba; some ao fechar aba/browser).
- Re-fetch das pendências abertas a cada troca de rota (mecânica do Addendum 1 reaproveitada).
- Sumiço automático do widget quando todas as ações daquela evolução estiverem concluídas.
- Diálogo de confirmação ao fechar com pendências ainda abertas.
- Redesign da pílula minimizada (cor primária sólida, contador legível, acessível).
- Limpeza do estado em logout e em troca de estabelecimento (multi-tenant).

**Não inclui**:
- Qualquer mudança de backend (endpoint, migração, evento, DTO).
- Persistência durável entre sessões/navegadores — essa responsabilidade **permanece**
  no painel persistente do paciente (CA68 original). O `sessionStorage` é apenas para o
  widget flutuante sobreviver a um reload na mesma aba.
- Suporte a múltiplas evoluções simultâneas no widget. O widget representa **uma**
  evolução por vez (a última salva). Salvar uma nova evolução com ações substitui o estado.
- Notificações push, e-mail ou canal interno (sino) — fora de escopo.

## 4. Regras de negócio

> Todas as regras deste addendum moram no **Front** (estado/UX). O backend é a fonte
> durável da pendência (painel do paciente), inalterado.

- **R24** (estado global em store) — O estado do widget ("próximos passos da sessão") vive
  em uma **Pinia store dedicada** (ex.: `proximosPassosStore`), não mais no `setup` da
  `ProntuarioView`. A store guarda: `pacienteId`, `evolucaoId`, `acoesMarcadas: AcaoPendencia[]`,
  e o estado de UI (`expandido | minimizado`). Mora em: Front (store). Validada em: front.

- **R25** (montagem global no layout autenticado) — O `WidgetProximosPassos` é montado
  **uma única vez** dentro de `AppLayout.vue` (que só renderiza quando
  `route.meta.layout === "app"`, ver `App.vue`). A `ProntuarioView` deixa de montar o
  widget diretamente — apenas **dispara** a store ao salvar evolução com ações marcadas.
  Mora em: Front (`AppLayout.vue` + `ProntuarioView.vue`). Validada em: front.

- **R26** (persistência por sessão de navegação) — Ao ativar o widget, a store grava seu
  estado em `sessionStorage` (chave própria, ex.: `imedto.proximosPassos`). Ao recarregar
  a página na mesma aba, a store reidrata o estado e o widget reaparece com o mesmo
  conteúdo. Fechar a aba/janela limpa naturalmente (`sessionStorage`). Mora em: Front
  (store, persistência). Validada em: front.
  **Decisão registrada**: `sessionStorage` é o corte natural — escopo "aba/sessão de
  navegação". A fonte **durável** da pendência continua sendo o painel do paciente.
  Não usar `localStorage` (sobreviveria a fechamento de aba e poluiria sessões futuras).

- **R27** ("consulta finalizada" = todas as ações concluídas) — Não existe no domínio hoje
  um conceito explícito de "consulta finalizada". **Interpretação registrada**: o widget
  encerra-se sozinho quando **todas** as ações daquela evolução estão concluídas
  (deixaram de constar em `pendenciaService.listarAbertas`). Esse é o único critério de
  "fim" automático além do fechamento explícito pelo usuário. Mora em: Front (lógica de
  conclusão da store/widget). Validada em: front.

- **R28** (fechar com confirmação só quando há abertas) — Clicar no X / "Fazer depois":
  - **Com ≥1 pendência aberta** → abre diálogo de confirmação "Fechar sem concluir as
    pendências? Elas continuam no painel do paciente." Confirmar fecha de vez e limpa o
    estado/`sessionStorage`; cancelar mantém o widget.
  - **Sem pendências abertas** → fecha direto, sem diálogo.
  Mora em: Front (widget + store). Validada em: front.

- **R29** (pílula minimizada nítida e acessível) — A pílula usa **cor primária sólida**
  (`hsl(var(--primary-hsl))`), texto branco, ícone de checklist + contador "X/N" legível,
  e `aria-label` descritivo (ex.: "Próximos passos: X de N concluídas"). Tokens do DS;
  tipografia via tokens (§5). Mora em: Front (`WidgetProximosPassos.vue`). Validada em: front.

- **R30** (limpeza multi-tenant e de sessão) — O estado do widget é **do tenant ativo**.
  A store é limpa (memória + `sessionStorage`) quando:
  - o usuário faz **logout** (a chave entra na lista de chaves zeradas por
    `authStore.limparSessao`), e
  - o usuário **troca de estabelecimento** (`tenantStore.selecionar` com `trocouEstab`,
    mesmo gancho que já limpa `assinaturaStore`).
  Mora em: Front (store + ganchos em `authStore`/`tenantStore`). Validada em: front.
  **Premissa multi-tenant**: pendência pertence a um paciente de um estabelecimento; o
  widget nunca pode "vazar" de um tenant para outro.

## 5. Modelo de dados

**Nenhuma alteração de schema.** Sem tabela, coluna, índice, migração ou endpoint novo.
A única persistência nova é client-side em `sessionStorage` (não-PII além de ids técnicos
já manipulados no front: `pacienteId`, `evolucaoId`). Sem texto clínico no `sessionStorage`
— apenas ids e os enums de `acoesMarcadas` (LGPD: minimização preservada).

## 6. UX e fluxo

**Onde mora agora**: montado em `AppLayout.vue`, abaixo do `<main>`/`<slot>`, dentro do
`<Teleport to="body">` que o componente já usa. Como `AppLayout` só existe em rotas
`meta.layout === "app"`, o widget **nunca** aparece em login/páginas públicas — sem
guarda extra de rota.

**Fluxo**:
1. Profissional salva evolução com ≥1 ação → `ProntuarioView` chama
   `proximosPassosStore.iniciar({ pacienteId, evolucaoId, acoesMarcadas })`.
2. Store persiste em `sessionStorage`; widget (já montado no layout) reage e aparece
   expandido no canto inferior direito.
3. Profissional navega para qualquer página → widget continua visível (estado global).
4. A cada troca de rota, o widget re-busca `listarAbertas(pacienteId)` e atualiza o
   contador "X de N".
5. Quando o contador chega a N/N → widget mostra brevemente "Tudo concluído" e some
   sozinho (ou some direto — ver CA204), limpando o estado.
6. Fechar manualmente: X com abertas → confirmação; X sem abertas → fecha direto.
7. Minimizar → pílula nítida (cor primária + "X/N").
8. Reload na mesma aba → reidrata e reaparece. Fechar aba / logout / trocar
   estabelecimento → estado some.

**Estados**: expandido / minimizado (pílula) / fechado (invisível) / "tudo concluído"
(transitório antes de sumir). Loading do re-fetch reaproveita o `buscando` interno.

**Componentes do DS**: diálogo de confirmação deve reusar o componente de confirmação já
existente no design system (ex.: `AppConfirmDialog`/`useConfirm`, se presente — o dev
confirma via grep; senão, modal de confirmação padrão do DS). **Não criar modal novo
ad-hoc** se já houver um equivalente.

**Mobile-ready**: mantém o comportamento do Addendum 1 (CA200) — `max-width`, ancorado no
canto, não cobre a tela inteira.

## 7. Critérios de aceite (testáveis) — continuam em CA202

- **CA202 (widget global — persiste entre páginas)**: Dado que o profissional salvou uma
  evolução com ≥1 ação de conduta marcada e o widget está visível, Quando ele navega para
  outra rota autenticada qualquer (ex.: `/orcamentos/novo`, `/agenda`, `/home`), Então o
  widget **permanece visível** ancorado no canto inferior direito, com o mesmo conteúdo,
  sem ser remontado/perdido.

- **CA203 (estado global em store + reidratação por sessão)**: Dado o widget visível,
  Quando o usuário dá reload (F5) na mesma aba, Então o widget **reaparece** com
  `pacienteId`/`evolucaoId`/`acoesMarcadas` reidratados do `sessionStorage`; e Quando o
  usuário fecha a aba e abre uma nova, Então o widget **não** reaparece (sem `localStorage`).

- **CA204 (sumiço automático ao concluir tudo)**: Dado o widget com N ações, Quando a
  última pendência aberta é concluída (em qualquer página, refletida no próximo re-fetch),
  Então o contador chega a "N de N", o widget exibe um feedback breve de conclusão e
  **some sozinho**, limpando o estado da store e do `sessionStorage`. (Feedback simples:
  estado "Tudo concluído" por ~2s, depois some — ou some direto; escolha do dev, desde que
  não fique pendurado.)

- **CA205 (re-fetch a cada troca de rota)**: Dado que o usuário concluiu uma ação em uma
  página diferente do prontuário, Quando a rota muda, Então o widget re-busca
  `pendenciaService.listarAbertas(pacienteId)` e o contador/lista refletem a conclusão —
  sem websocket nem polling (apenas no evento de troca de rota).

- **CA206 (fechar com confirmação — há pendências abertas)**: Dado o widget aberto com ≥1
  pendência ainda aberta, Quando o usuário clica no X / "Fazer depois", Então abre um
  diálogo de confirmação com o texto "Fechar sem concluir as pendências? Elas continuam no
  painel do paciente."; Quando confirma, Então o widget fecha e o estado da store +
  `sessionStorage` são limpos; Quando cancela, Então o widget **permanece** inalterado.

- **CA207 (fechar direto — sem pendências abertas)**: Dado o widget aberto com **zero**
  pendências abertas restantes, Quando o usuário clica no X, Então o widget fecha
  **direto, sem diálogo**, e o estado é limpo.

- **CA208 (pílula minimizada nítida)**: Dado o widget minimizado, Quando renderiza, Então
  a pílula tem **fundo de cor primária sólida** (`hsl(var(--primary-hsl))`), texto/ícone
  brancos legíveis, contador "X/N" visível e ícone de checklist — verificável por CSS
  computado (`background` resolve para a cor primária, não transparente/branco). Nenhuma
  cor usa token HSL sem `hsl()` (gotcha conhecido).

- **CA209 (pílula acessível)**: Dado a pílula renderizada, Quando inspecionada, Então
  possui `aria-label` descritivo informando que há próximos passos e o progresso
  ("X de N concluídas") e é focável/acionável por teclado (é `<button>`).

- **CA210 (regressão — público/login sem widget)**: Dado um usuário em página pública ou
  na tela de login (`route.meta.layout !== "app"`), Quando a página carrega, Então o widget
  **não** é montado nem renderizado (vive só dentro de `AppLayout`).

- **CA211 (regressão — logout limpa)**: Dado o widget ativo com estado em `sessionStorage`,
  Quando o usuário faz logout, Então a chave do widget é zerada por `authStore.limparSessao`
  (entra na lista canônica de chaves de sessão) e o widget não reaparece após novo login.

- **CA212 (multi-tenant — trocar estabelecimento limpa)**: Dado o widget ativo para um
  paciente do estabelecimento A, Quando o usuário troca para o estabelecimento B
  (`tenantStore.selecionar` com `trocouEstab`), Então o estado do widget é limpo (memória +
  `sessionStorage`) e o widget some — nenhuma pendência do tenant A vaza para a sessão do
  tenant B.

- **CA213 (regressão — painel do paciente intacto)**: Dado o fluxo completo, Quando o
  widget é promovido a global, Então: (a) o painel persistente do paciente continua
  listando/concluindo pendências como antes (CA67/CA68 original); (b) a criação de
  pendências ao salvar (CA59) e a conclusão automática por gatilho (CA63/CA64) seguem
  intactas; (c) **nada muda no backend** — nenhum endpoint/migração/evento novo.

- **CA214 (regressão — comportamento do Addendum 1 preservado)**: Dado o widget global,
  Quando exibido, Então mantém: não-bloqueio da página por trás (CA193), fundo sólido via
  token HSL (CA192), link direto para ações incl. `CriarOrcamento` com `evolucaoId` (CA195),
  e o comportamento mobile (CA200). A promoção a global **não** regride nenhum CA190–CA201.

- **CA215 (tipografia §5)**: Dado qualquer texto novo/alterado do widget e da pílula,
  Quando o CSS é inspecionado, Então não há `font-size`/`font-weight` literais — apenas
  tokens (`--text-*`, `--font-weight-*`), conforme CLAUDE.md §5.

## 8. Riscos e dependências

- **Risco — dupla montagem transitória**: durante a refatoração, o widget não pode ficar
  montado tanto na `ProntuarioView` quanto no `AppLayout`. A `ProntuarioView` deve **parar**
  de montar o componente (remover linhas 485–491) e passar a só acionar a store. CA202/CA213
  cobrem.
- **Risco — reidratação com tenant errado**: se o `sessionStorage` for reidratado antes de
  o tenant estar resolvido, o widget poderia mostrar pendências de um tenant trocado.
  Mitigação: a limpeza em `selecionar`/`limparSessao` (R30) precede qualquer reidratação;
  o re-fetch sempre usa o `pacienteId` do estado, e o backend já filtra por tenant
  (multi-tenant falha-fechada). CA212 cobre.
- **Risco — z-index sob `AppTopBar`/`AppDrawer`**: agora que o widget vive no layout, manter
  a faixa de z-index do Addendum 1 (700–800, abaixo de modais `z-index: 900`). Reaproveitar.
- **Dependência**: `pendenciaService.listarAbertas` (já existente). Diálogo de confirmação
  do DS (reuso obrigatório, sem modal novo se houver equivalente).
- **Sem dependência de backend.**

## 9. Observações para execução

- **Não-negociável**:
  - Estado global em **Pinia store dedicada** + persistência em **`sessionStorage`** (não
    `localStorage`). A chave do widget entra na **lista canônica** de chaves de sessão
    zeradas por `authStore.limparSessao` (ver comentário em `authStore.ts` linha ~14) e na
    limpeza por troca de estabelecimento (`tenantStore.selecionar`, mesmo gancho que limpa
    `assinaturaStore`).
  - Widget montado **uma vez** em `AppLayout.vue`; `ProntuarioView` deixa de montá-lo.
  - Reuso do diálogo de confirmação do DS — **grep antes** por `AppConfirmDialog`/`useConfirm`/
    componente de confirmação equivalente. Criar modal novo só se não existir.
  - Tipografia §5 + cores sempre `hsl(var(--token))` (gotcha conhecido do projeto).
  - Multi-tenant: pendência é do tenant; nunca vazar entre estabelecimentos.
- **Liberdade técnica**: mecânica exata do re-fetch por troca de rota (watch de
  `route.fullPath` na store/widget vs. `onBeforeRouteUpdate`), e a forma do feedback de
  "tudo concluído" (transitório vs. some direto) — escolha do dev, desde que CA204/CA205
  passem sem polling/websocket.
- O componente `WidgetProximosPassos.vue` já está ~90% pronto para isso: já usa
  `Teleport to="body"`, já re-busca em `expandir()` e no watch de `rotaAtual`, e a pílula
  já parte de `hsl(var(--primary-hsl))`. O trabalho é (1) extrair o estado para a store,
  (2) mover a montagem para o layout, (3) adicionar `sessionStorage` + ganchos de limpeza,
  (4) adicionar o diálogo de confirmação, (5) reforçar contraste/legibilidade da pílula.

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — adicionar nota curta na seção de componentes/overlays: o
  `WidgetProximosPassos` é um **widget global persistente** montado em `AppLayout`, com
  estado em Pinia store + `sessionStorage` (escopo de sessão de navegação), faixa de
  z-index 700–800 (abaixo de modais). Documentar a pílula minimizada como padrão de
  "indicador flutuante de tarefas pendentes" (cor primária sólida + contador). Mudança
  incremental, cirúrgica — não reescrever o doc.
- **`Docs/ARQUITETURA.md`** — sem mudança: não há novo padrão de store/service além do já
  documentado (Pinia store padrão). Caso o dev considere que a persistência por
  `sessionStorage` em store é um padrão reutilizável digno de registro, adicionar 1 linha
  na seção de stores; caso contrário, "nenhum".
- **`Docs/LGPD.md`** — sem mudança: nada novo de PII; `sessionStorage` guarda apenas ids
  técnicos e enums de ação, sem texto clínico, mantendo minimização.
- **Backend / `Docs/INFRA.md` / `Docs/COMANDOS.md`** — nenhum.
