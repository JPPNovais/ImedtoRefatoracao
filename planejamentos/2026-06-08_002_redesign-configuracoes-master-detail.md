# Redesign de "Configurações do estabelecimento" — layout master-detail com painéis inline lazy-loaded

**ID**: 2026-06-08_002
**Status**: Aprovado por usuário em 2026-06-08
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G
**Áreas regressivas tocadas**: permissionamento (RBAC de itens do submenu) · navegação/rotas (redirects + call-sites) · prontuário (modelos) · termos de consentimento · assinatura · automações · nenhuma mudança de backend

> **Restrição-mãe (não-negociável):** esta entrega é **FRONTEND-ONLY**. Zero mudança em backend, controller, handler, service de API, contrato, DTO, migration ou regra de negócio. Trata-se de **mudança de apresentação/montagem** (layout + reuso de componentes/serviços já existentes). Nenhum service novo. Nenhuma chamada de API nova. O que muda é onde e quando os componentes existentes são montados.

---

## 1. Contexto e motivação

Hoje "Configurações do estabelecimento" (`/estabelecimento`, `EstabelecimentoView.vue`) usa `<AppTabs variante="underline">` com as abas Geral · Dados · Funcionamento · Unidades · Repartições · Listas. A aba **Geral** é apenas uma vitrine de 5 `.atalho-card` que dão `router.push` para 5 páginas separadas (Automações, Modelos de prontuário, Configurações de IA, Termos de consentimento, Assinatura). O usuário sai da tela de configurações para mexer em cada recurso e volta — fricção de navegação, perda de contexto e uma "aba Geral" que não configura nada, só redireciona.

A demanda é consolidar toda a configuração do estabelecimento numa **única página master-detail**: submenu lateral agrupado (~248px) à esquerda + painel de detalhe à direita que troca conforme o item selecionado. As 5 páginas hoje separadas passam a ser **painéis inline** dentro dessa mesma tela, montados sob demanda (lazy-load real). O comportamento de cada recurso (serviços, validações, 422) permanece idêntico — só muda o "estilo de montagem".

**Evidência de design**: mockup `/tmp/design_bundle/imedto/project/Configurações - submenu.html` e transcript de intenção `/tmp/design_bundle/imedto/chats/chat2.md`. O mockup é referência de layout (master-detail, sub-nav em 3 grupos, busca no topo, painel que troca, colapso em coluna única <860px). O CSS do protótipo **NÃO** é reaproveitado — o layout é recriado com o design system real (`frontend/src/components/ui/`).

## 2. Persona-alvo

**Dono / Administrador** do estabelecimento, no momento de configurar a operação (onboarding inicial ou ajustes pontuais). Frequência: baixa por sessão, mas alta importância — é a tela onde se define como a clínica funciona. Secundariamente, papéis com permissões granulares (ex.: quem tem `termos.gerenciar_modelos` mas não é Dono) veem apenas as seções a que têm acesso.

## 3. Escopo

**Inclui**:
- Reescrever a **apresentação** de `EstabelecimentoView.vue` de `AppTabs` para layout **master-detail**: coluna de sub-navegação à esquerda (~248px) com 3 grupos + busca no topo, e painel de detalhe à direita.
- Sub-nav com 3 grupos e 10 seções:
  - **Estabelecimento**: Dados · Funcionamento · Unidades · Repartições
  - **Modelos e listas**: Modelos de prontuário · Termos de consentimento · Listas de variáveis
  - **Recursos**: Automações · Configurações de IA · Assinatura
- Internalizar como **painéis inline lazy-loaded** as 5 views hoje separadas, **reusando** os componentes/views existentes sem reescrever lógica: Automações, Modelos de prontuário, Configurações de IA, Termos de consentimento (lista + editor), Assinatura.
- Manter inline (como já são hoje) os componentes `*Tab.vue` de Dados, Funcionamento, Unidades, Repartições, Listas de variáveis.
- Refatorar **apenas a apresentação** de `TermoFormView.vue` para que o editor de termos funcione embarcado no painel (lista ↔ editor inline, sem navegação de rota), preservando 100% da lógica, serviço (`termoModeloService`) e comportamento (salvar, validar, cancelar, voltar para lista, 422).
- Deep-link interno via `?secao=<id>` (ex.: `/estabelecimento?secao=automacoes`), sincronizado two-way com o router, espelhando o padrão `?aba=` já usado em `TermosListaView`/`OrcamentoSettingsView`/`RelatoriosView`.
- Redirecionar as rotas antigas (`/automacoes`, `/configuracoes/ia`, `/minha-assinatura`, `/configuracoes/modelos-prontuario`, `/configuracoes/termos` + sub-rotas `/configuracoes/termos/novo` e `/configuracoes/termos/:id/editar`) para `/estabelecimento?secao=<id>`, espelhando o padrão de alias-redirect já existente para Relatórios.
- Auditar e atualizar **todos os call-sites** internos do app que navegam para essas rotas (router.push, RouterLink, item de menu/sidebar, botões).
- RBAC por **ocultação** de item do submenu (falha-fechada), reaproveitando exatamente os gates atuais — nenhuma regra nova.
- Estados por painel: loading / erro / vazio / sem-permissão.
- Responsividade: <860px colapsa para coluna única (conforme mockup).
- Atualizar `Docs/DESIGN.md` registrando o padrão master-detail da página de configurações.

**Não inclui**:
- Qualquer mudança de backend, handler, service de API, DTO, contrato, migration, regra de negócio.
- Novas permissões, novos papéis, novas seções de configuração.
- Mudar a lógica interna, os campos, as validações ou o serviço de qualquer um dos painéis internalizados.
- Mudar o comportamento das abas que já eram inline (Dados/Funcionamento/Unidades/Repartições/Listas) — só são reposicionadas no novo layout.
- Publicação/push: o `imedto-qa` valida, mas **não dá push** nesta entrega (decisão do usuário).
- Novo componente genérico de master-detail no design system — esta entrega resolve a página de configurações; a extração para componente reutilizável fica como **backlog separado** (ver §9). O layout é construído na própria view reusando UI existente.

## 4. Regras de negócio

> Todas as regras abaixo são **de apresentação/navegação**. Nenhuma cria ou altera regra de domínio. A fonte da verdade de cada permissão e de cada validação continua sendo o backend (gates de rota + 422 dos serviços existentes).

- **R1 — Lazy-load real por painel.** Apenas o painel da seção ativa é montado. Painel não-aberto não monta o componente nem dispara consulta. Implementação via `defineAsyncComponent` por painel pesado e/ou `v-if` que só monta on-demand (sem `v-show`, que manteria montado). Mora em: Front (`EstabelecimentoView.vue`). Validada em: front (montagem condicional + ausência de request de service da seção fechada).
- **R2 — Seção default.** Ao abrir `/estabelecimento` sem `?secao=`, a seção default é **Dados** (`secao=dados`), preservando o destino histórico da tela de edição do estabelecimento. Mora em: Front. Validada em: front.
- **R3 — Deep-link bidirecional.** `?secao=<id>` define a seção ativa ao carregar; trocar de seção atualiza a query (sem recarregar a página, sem empilhar histórico desnecessário — usar `router.replace` para a sincronização, espelhando `OrcamentoSettingsView`). `?secao=` inválido/desconhecido cai na seção default (R2). Mora em: Front. Validada em: front.
- **R4 — RBAC por ocultação (falha-fechada).** Cada item do submenu só aparece se o usuário tem a permissão da seção, usando **exatamente** os gates já vigentes (sem regra nova):
  - Termos de consentimento → `permissoes.pode("termos.gerenciar_modelos")`.
  - Configurações de IA → gate atual de `IaSettings` (extra `config_estabelecimento`).
  - Modelos de prontuário → gate atual de `ModelosProntuario` (extra `modelos_prontuario`).
  - Automações → gate atual de `Automacoes` (extra `automacao_config`).
  - Dados/Funcionamento/Unidades/Repartições/Listas → gate atual de `Estabelecimento` (extra `config_estabelecimento`) / `podeEditar = papel === 'Dono'` para edição, como hoje.
  Se o usuário acessar `?secao=<id>` de uma seção sem permissão, comporta-se como seção não disponível: cai na seção default (R2) ou exibe estado "sem-permissão" no painel — nunca renderiza conteúdo nem dispara consulta da seção bloqueada. Mora em: Front (espelhando `routePermissions.ts`/`permissoesStore`). Validada em: front (item oculto + sem request). Backend permanece a trava real (cada serviço já é falha-fechada por tenant/permissão).
- **R5 — Multi-tenant preservado.** Cada painel continua consumindo seu service atual, que já filtra `estabelecimento_id`. Nenhum filtro de tenant é movido para o front nem afrouxado. Mora em: serviços existentes (inalterados). Validada em: front (nenhuma query nova fora dos serviços existentes).
- **R6 — Paridade comportamental dos painéis internalizados.** Cada uma das 5 seções internalizadas renderiza o **mesmo conteúdo e comportamento** das views originais. Reuso, não reescrita: o conteúdo de cada painel é o componente existente embarcado, não uma cópia.
- **R7 — Editor de Termos inline (lista ↔ editor).** Dentro do painel "Termos de consentimento", a lista fica inline; "Novo termo"/"Editar" abre o **editor inline** no próprio painel (troca lista↔editor sem navegar de rota); "Salvar"/"Cancelar"/"Voltar" retorna à lista inline. O serviço (`termoModeloService`), as validações e os 422 são os mesmos — só a navegação deixa de ser por rota e passa a ser por estado local do painel. Mora em: Front (refator de apresentação de `TermoFormView.vue` → componente embarcável + `TermosListaView` como painel). Validada em: front (paridade salvar/validar/cancelar/voltar).
- **R8 — Rotas antigas redirecionam.** `/automacoes`, `/configuracoes/ia`, `/minha-assinatura`, `/configuracoes/modelos-prontuario`, `/configuracoes/termos`, `/configuracoes/termos/novo`, `/configuracoes/termos/:id/editar` passam a redirecionar para `/estabelecimento?secao=<id>` correspondente. Deep-links externos antigos não quebram. Mora em: `router/index.ts` (apenas defs de rota → `redirect`). Validada em: front (navegar à URL antiga aterrissa na seção certa).
  - `/automacoes` → `?secao=automacoes`
  - `/configuracoes/ia` → `?secao=ia`
  - `/minha-assinatura` → `?secao=assinatura`
  - `/configuracoes/modelos-prontuario` → `?secao=modelos-prontuario`
  - `/configuracoes/termos` (e `/novo`, `/:id/editar`) → `?secao=termos` (o editor abre por estado interno do painel; as sub-rotas de novo/editar deixam de existir como rota e viram redirect para a seção de termos)
- **R9 — Call-sites atualizados.** Todo ponto do app que hoje navega para as rotas internalizadas é auditado e atualizado para a nova navegação. Inventário conhecido (o dev DEVE confirmar com `grep` por cada `name`/path antes de fechar):
  - `App.vue` → `router.push({ name: "MinhaAssinatura" })` (fluxo de assinatura expirada): atualizar para `/estabelecimento?secao=assinatura` (ou manter via redirect R8, mas preferir destino direto).
  - `layouts/AppLayout.vue` → item de menu `"Automação"` (`to: { name: "Automacoes" }`): redirecionar para `/estabelecimento?secao=automacoes` (decisão de produto: o item de menu "Automação" continua existindo no sidebar, apenas aponta para a nova seção — **não remover o item**).
  - `views/configuracoes/TermoFormView.vue` → `router.push({ name: "TermosModelos" })` no salvar/cancelar: substituído por callback/estado interno (volta para a lista inline — R7), deixa de navegar por rota.
  - `views/estabelecimento/EstabelecimentoView.vue` → os 5 `router.push` dos `.atalho-card` da aba Geral: removidos (a aba Geral deixa de existir; os destinos viram seções).
  Mora em: Front. Validada em: front (nenhum `router.push`/`RouterLink` órfão para as rotas internalizadas que não seja o próprio redirect; busca por cada `name` retorna só defs de redirect + as novas navegações).

## 5. Modelo de dados

**Nenhuma mudança de dados.** Sem tabela nova, sem coluna nova, sem migration, sem índice. Multi-tenant e audit permanecem como estão nos serviços existentes (cada painel continua auditando o que já auditava — ex.: acesso a modelos/termos pelos seus próprios serviços). LGPD: nenhuma nova exposição de PII; os DTOs e mensagens genéricas dos serviços internalizados permanecem intactos. `imedto-database` **não é acionado** nesta entrega.

## 6. UX e fluxo

Layout master-detail recriado com o design system real (sem copiar CSS do protótipo):

```
┌───────────────────────────────────────────────────────────────────┐
│ AppPageHeader: "Configurações do estabelecimento"                   │
├──────────────────────┬────────────────────────────────────────────┤
│ SUB-NAV (~248px)     │ PAINEL DE DETALHE (troca por seção)         │
│ ┌──────────────────┐ │                                             │
│ │ 🔎 buscar seção  │ │   [ conteúdo da seção ativa, montado        │
│ └──────────────────┘ │     sob demanda — lazy ]                    │
│ ESTABELECIMENTO       │                                             │
│  • Dados   (default)  │   estados: loading / erro / vazio /         │
│  • Funcionamento      │            sem-permissão                    │
│  • Unidades           │                                             │
│  • Repartições        │                                             │
│ MODELOS E LISTAS      │   (Termos: lista ↔ editor inline)           │
│  • Modelos de pront.  │                                             │
│  • Termos consent.*   │                                             │
│  • Listas de variáveis│                                             │
│ RECURSOS              │                                             │
│  • Automações*        │                                             │
│  • Configurações IA*  │                                             │
│  • Assinatura         │                                             │
└──────────────────────┴────────────────────────────────────────────┘
   * itens condicionados a permissão (R4) — ocultos sem acesso
```

- **Busca no topo do sub-nav**: filtra os itens visíveis das 3 seções por label (client-side, sobre os itens já permitidos). Sem debounce de rede (não há rede); filtro puramente local sobre a lista estática de itens.
- **Componentes do design system** a reusar (confirmar nomes reais em `components/ui/`): `AppPageHeader` (cabeçalho), e o conteúdo de cada painel são os componentes/views já existentes. A sub-nav é construída com UI existente (botões/itens de navegação). **Não** escrever CSS scoped novo para algo que o design system já resolve. Se um item de sub-nav exigir um padrão visual recorrente novo, registrar em `Docs/DESIGN.md` (ver §10).
- **Estados por painel**: cada painel internalizado já trata seus próprios loading/erro/vazio (vêm de graça com o reuso). O wrapper adiciona o estado **sem-permissão** (R4) para o caso de `?secao=` bloqueada.
- **Responsivo (<860px)**: sub-nav e painel empilham em coluna única (conforme mockup). Em mobile, a seleção de seção pode virar um seletor/colapsável no topo — recriar a intenção do mockup com UI existente, sem CSS copiado.
- **Sem atalho de teclado novo** obrigatório nesta entrega.

## 7. Critérios de aceite (testáveis)

- **CA1 — Layout master-detail (caminho feliz).** Dado um Dono autenticado, Quando abre `/estabelecimento`, Então vê a sub-nav à esquerda (~248px) com 3 grupos rotulados "Estabelecimento", "Modelos e listas" e "Recursos", uma busca no topo do sub-nav, e o painel de detalhe à direita exibindo a seção default "Dados" — fiel à estrutura do mockup, construído com o design system real (sem CSS do protótipo).

- **CA2 — Seção default + deep-link bidirecional.** Dado `/estabelecimento` sem query, Quando carrega, Então a seção ativa é "Dados" e a URL não exige `?secao`. E Dado que o usuário clica em "Automações", Quando a seção troca, Então a URL passa a `/estabelecimento?secao=automacoes` (via `router.replace`) e o painel de Automações é exibido. E Dado `/estabelecimento?secao=ia`, Quando carrega direto por URL, Então a seção ativa é "Configurações de IA". E Dado `?secao=valor-inexistente`, Quando carrega, Então cai na seção default "Dados" sem erro.

- **CA3 — Lazy-load real (não monta painel fechado).** Dado que o usuário abre `/estabelecimento` na seção default, Quando a página termina de carregar, Então apenas o painel "Dados" está montado e nenhuma chamada de service das outras seções (Automações, Modelos, IA, Termos, Assinatura) foi disparada. E Quando o usuário troca para "Modelos de prontuário", Então o painel de Modelos monta sob demanda e só então a consulta de modelos é disparada — verificável por inspeção de código (montagem condicional `v-if`/`defineAsyncComponent`) e por ausência de network request da seção até sua abertura.

- **CA4 — Paridade das 5 seções internalizadas.** Dado cada uma das seções Automações, Modelos de prontuário, Configurações de IA, Termos de consentimento e Assinatura, Quando aberta inline, Então renderiza o mesmo conteúdo, campos, validações e comportamento da view original correspondente, consumindo o mesmo service (reuso do componente existente, não reescrita de lógica).

- **CA5 — Editor de Termos inline (lista ↔ editor, paridade comportamental).** Dado o painel "Termos de consentimento" exibindo a lista inline, Quando o usuário clica "Novo termo" ou "Editar", Então o editor abre inline no próprio painel sem navegar de rota; Quando salva um termo válido, Então o `termoModeloService` é chamado igual ao fluxo original e, ao concluir, retorna à lista inline atualizada; Quando o backend retorna 422, Então a mesma mensagem de validação do fluxo original é exibida (paridade); Quando clica "Cancelar"/"Voltar", Então retorna à lista inline sem persistir. A lógica e o serviço são idênticos aos da `TermoFormView` original — só a navegação mudou de rota para estado local.

- **CA6 — Rotas antigas redirecionam.** Dado um deep-link externo para `/automacoes`, `/configuracoes/ia`, `/minha-assinatura`, `/configuracoes/modelos-prontuario`, `/configuracoes/termos`, `/configuracoes/termos/novo` ou `/configuracoes/termos/:id/editar`, Quando o usuário acessa, Então é redirecionado para `/estabelecimento?secao=<id>` da seção correspondente (espelhando o padrão de alias-redirect de Relatórios) e nenhuma rota antiga renderiza a view standalone.

- **CA7 — Call-sites atualizados (sem órfãos).** Dado o app inteiro, Quando se faz `grep` por `name: "Automacoes"`/`"IaSettings"`/`"TermosModelos"`/`"MinhaAssinatura"`/`"ModelosProntuario"`/`"TermosNovo"`/`"TermosEditar"` e pelos paths antigos, Então toda navegação interna (incluindo `App.vue` fluxo de assinatura expirada e o item de menu "Automação" em `AppLayout.vue`) aponta para o novo destino, e as únicas referências remanescentes aos `name` antigos são as definições de redirect em `router/index.ts`. Nenhum `router.push`/`RouterLink` órfão para view standalone removida.

- **CA8 — RBAC por ocultação (falha-fechada).** Dado um usuário com `termos.gerenciar_modelos` mas que **não** tem o gate de IA, Quando abre Configurações, Então o item "Termos de consentimento" aparece no sub-nav e "Configurações de IA" **não** aparece (oculto). E Dado que esse usuário acessa `/estabelecimento?secao=ia` direto por URL, Quando carrega, Então o painel de IA **não** é montado, nenhuma consulta de IA é disparada, e a tela cai na seção default ou exibe estado "sem-permissão" — nunca conteúdo da seção bloqueada.

- **CA9 — Multi-tenant preservado.** Dado um usuário do estabelecimento B, Quando abre qualquer seção, Então cada painel consome seu service existente que já filtra `estabelecimento_id`; nenhum dado do estabelecimento A é exibido e nenhuma query nova fora dos serviços existentes é introduzida. (A trava real permanece no backend; o front não recebe nem afrouxa filtro de tenant.)

- **CA10 — Estados por painel.** Dado um painel em carregamento, Quando a consulta está pendente, Então exibe o estado de loading do componente reusado; Dado erro de carregamento, Então exibe o estado de erro do componente reusado; Dado lista vazia (ex.: nenhum termo cadastrado), Então exibe o estado vazio do componente reusado; Dado seção sem permissão acessada por URL, Então exibe o estado "sem-permissão" (R4/CA8).

- **CA11 — Responsivo <860px.** Dado viewport <860px, Quando a página de Configurações carrega, Então sub-nav e painel colapsam para coluna única (conforme mockup), permanecendo navegável e legível; Dado viewport ≥860px, Então o layout master-detail de duas colunas é exibido.

- **CA12 — Naming "Modelos e listas".** Dado o sub-nav, Quando renderiza os grupos, Então o grupo central rotula-se exatamente "Modelos e listas" (não "Modelos", não "Modelos e variáveis").

- **CA13 — Zero mudança de backend/contrato.** Dado o diff completo da entrega, Quando revisado, Então não há alteração em arquivos de backend, controller, handler, service de API, DTO, contrato, migration ou regra de negócio; toda mudança está em frontend (views/componentes/router/Docs). Nenhuma chamada de API nova foi adicionada.

- **CA14 — Documentação viva.** Dado que esta entrega introduz o padrão master-detail de página de configurações, Quando concluída, Então `Docs/DESIGN.md` é atualizado de forma cirúrgica registrando o padrão (sub-nav agrupada + painel detail lazy-loaded + deep-link `?secao=`), referenciando este briefing (2026-06-08_002), sem reescrever o documento inteiro.

## 8. Riscos e dependências

- **`TermoFormView.vue` (352 linhas) é o maior risco.** É a única peça que exige refator de apresentação real (não só reposicionamento). Hoje navega por rota (`router.push({ name: "TermosModelos" })` no salvar/cancelar — linha ~121) e provavelmente lê `:id` da rota para o modo edição. Extrair para componente embarcável exige trocar: (a) entrada de `:id` por prop; (b) saída de `router.push` por emit/callback que devolve à lista inline. **Risco**: quebrar paridade de salvar/validar/cancelar/422. Mitigação: CA5 cobre paridade comportamental; manter `termoModeloService` e validações intocados — só trocar a camada de navegação.
- **Call-sites**: inventário conhecido (5 pontos) — `App.vue:15`, `AppLayout.vue:56`, `TermoFormView.vue:121`, e os 5 `router.push` internos da própria `EstabelecimentoView.vue`. O dev DEVE rodar `grep` por cada `name`/path para confirmar que não há outros (ex.: notificações, e-mails, onboarding, telas de assinatura expirada). Sub-rotas de termos (`TermosNovo`, `TermosEditar`) precisam de tratamento de redirect específico (viram `?secao=termos`).
- **Lazy-load + estado de seção**: garantir `v-if` (não `v-show`) e/ou `defineAsyncComponent` para que painel fechado realmente não monte nem consulte. Risco de regressão de performance se um wrapper montar todos os painéis de uma vez.
- **`?secao=` two-way**: usar `router.replace` (não `push`) para a sincronização, evitando poluir o histórico do navegador a cada troca de seção — espelhar `OrcamentoSettingsView`/`RelatoriosView`.
- **RBAC**: reusar os gates de `routePermissions.ts`/`permissoesStore`; não duplicar lógica de permissão dentro da view. Itens condicionados: Termos, IA, Modelos, Automações.
- **Aba Geral deixa de existir**: garantir que nenhum link/onboarding aponte para "aba geral" do estabelecimento.
- **Dependência de design**: o layout deve seguir o mockup em estrutura, mas usar o design system real. Não bloquear por falta de componente — construir com UI existente e registrar no DESIGN.md o padrão emergente.
- **Sem push**: o `imedto-qa` valida tudo, mas **não** comita/empurra ao fim (decisão do usuário). Entrega fica local aguardando aval de publicação.

## 9. Observações para execução

- **Não-negociável**: FRONTEND-ONLY (CA13); reuso dos componentes/views existentes (CA4, R6); paridade comportamental do editor de termos (CA5); lazy-load real (CA3); RBAC por ocultação reusando gates atuais (CA8, R4); naming exato "Modelos e listas" (CA12); sem push.
- **Liberdade técnica do dev**: como estruturar o wrapper de painéis (mapa de seções → componente async), como montar a sub-nav com UI existente, e onde colocar o estado lista↔editor de termos. Desde que respeite `v-if`/`defineAsyncComponent`, `router.replace` para `?secao=`, e os gates existentes.
- **Reuso obrigatório de padrão existente**: o redirect das rotas antigas espelha o bloco de aliases de Relatórios já presente em `router/index.ts` (`redirect: { path: "/relatorios", query: { aba } }`). A sincronização `?secao=` espelha `OrcamentoSettingsView.vue`/`TermosListaView.vue` (`watch(() => route.query.aba)` + ref inicial a partir de `route.query`). Não inventar padrão novo.
- **Backlog separado (NÃO fazer nesta entrega)**: extrair o layout master-detail para um componente genérico do design system (ex.: `AppMasterDetail`/`AppSubNav`) — registrar como demanda futura quando houver 2º caso de uso. Esta entrega resolve só a página de configurações.
- **`imedto-database` não é acionado** (sem schema). **`imedto-qa`** valida todos os CAs por análise de código + suíte (chrome-devtools pode estar indisponível no sandbox — validação visual fica para o usuário em prod), e **não dá push**.

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — adicionar (cirurgicamente, sem reescrever o doc) uma subseção registrando o **padrão master-detail da página de configurações**: sub-nav agrupada (~248px, 3 grupos, busca no topo) + painel de detalhe que troca, **painéis montados sob demanda** (`v-if`/`defineAsyncComponent` — painel fechado não monta nem consulta), e **deep-link via `?secao=`** sincronizado two-way com o router (espelhando o padrão `?aba=` de Relatórios/Orçamento). Referenciar o briefing 2026-06-08_002. Coerência: se este virar padrão reutilizável, anotar o item de backlog de extração para componente do design system. Validado em CA14.
- **`Docs/ARQUITETURA.md`** — nenhuma alteração (sem novo padrão de store/service/DI; reuso de padrões existentes de rota e query-sync).
- **`Docs/INFRA.md` / `Docs/COMANDOS.md` / `Docs/LGPD.md`** — nenhuma alteração (sem infra, sem comando novo, sem nova exposição de PII/retenção/audit; serviços e DTOs internalizados permanecem intactos).
