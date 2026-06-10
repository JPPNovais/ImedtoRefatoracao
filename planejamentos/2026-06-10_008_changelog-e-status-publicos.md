# Páginas públicas de confiança: "Novidades" (changelog) e "Status" do sistema

**ID**: 2026-06-10_008
**Status**: Aprovado por usuário em 2026-06-10 (modo autônomo — decisões de produto fornecidas pelo orquestrador; ambiguidade residual fechada com default mais simples e registrada em §11)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P
**Áreas regressivas tocadas**: rotas públicas do front, rodapé da landing, menu de ajuda/conta no app. Não toca: backend, banco, autenticação, qualquer dado de tenant.

> Roadmap: [`Docs/Roadmap/FASE_1_COMPLETUDE.md`](../Docs/Roadmap/FASE_1_COMPLETUDE.md) item **1.9** (diferencial D2, parte 1 — "confiabilidade publicada"). **Nota do roadmap respeitada**: uptime **real/medido** depende da Fase 0 de infra (F0-E1) — **não** publicar números de infra frágil. Até lá, status é **declarado manualmente**.

## 1. Contexto e motivação

Produto de saúde precisa transmitir **confiança** para vender. Dois sinais baratos e de alto impacto: (1) uma página de **"Novidades"** mostrando que o produto evolui de verdade (changelog leigo, sem jargão técnico), e (2) uma página de **"Status"** dizendo se o sistema está operacional. Ambas são páginas **públicas** (acessíveis sem login), porque servem tanto a quem está avaliando o produto quanto a clientes querendo saber "saiu coisa nova?" / "o sistema está no ar?".

O escopo é deliberadamente **mínimo e estático**: **sem banco, sem CMS, sem schema, sem chamada de API autenticada**. As entradas de novidades e o estado de status vivem em **arquivo(s) versionado(s) no repositório** — atualizar é um commit, deploy publica. Isso evita criar infra de conteúdo agora e mantém a entrega na Fase 1 (esforço P).

**Estado atual do front (investigado)**:
- **Rotas públicas** seguem um padrão simples em `frontend/src/router/index.ts`: `{ path, name, component }` **sem** `meta.requiresAuth`, **sem** `...APP` (layout). Exemplos diretos: `/privacidade` → `PrivacidadeView`, `/termos` → `TermosView` (em `frontend/src/views/legal/`). São views **standalone** (sem `AppLayout`/sidebar), com cabeçalho próprio e link "← Voltar" para a `Landing`.
- **`LandingView.vue`** tem um `<footer>` com uma coluna "legal" usando `<router-link to="/termos">` e `<router-link to="/privacidade">`. É exatamente onde os links de "Novidades" e "Status" entram.
- **Como a landing é servida**: SPA Vue (Vite) com `createWebHistory`; a rota `/` (`Landing`) é pública. As novas rotas `/novidades` e `/status` são servidas pelo mesmo SPA, sem nada de servidor adicional.
- **Convenção de dados estáticos**: já existe precedente de **catálogo estático no front** quando o dado é estável e raramente muda — ex.: `PROFISSOES`/`ESPECIALIDADES_POR_PROFISSAO` hardcoded em `MinhaContaView.vue` ("Mantido no front porque é estático e raramente muda — evita endpoint dedicado"). O changelog/status seguem essa filosofia. **Não** há convenção de JSON em `frontend/public/` em uso hoje.

## 2. Persona-alvo

- **Visitante/prospect** (não logado) avaliando o produto: vê que ele evolui e está no ar → sinal de confiança antes de assinar.
- **Cliente** (logado ou não) querendo saber "o que mudou?" ou "está fora do ar ou é só comigo?".
- **Equipe Imedto** (quem mantém): atualizar é editar um arquivo no repo + deploy. Sem painel, sem permissão, sem treinamento.
- Momento da jornada: **descoberta** (prospect) e **pós-venda/suporte** (cliente). Frequência: baixa por usuário; alta importância em incidentes.

## 3. Escopo

**Inclui**:
- **Rota pública `/novidades`** → view `NovidadesView` (padrão das views de `legal/`): renderiza a lista de entradas de changelog lida de um **módulo estático versionado no repo**.
  - Cada entrada: **data**, **título**, **descrição leiga**, **tag** (`novidade` | `melhoria` | `correção`).
  - Ordenação: mais recente primeiro. Agrupamento visual por data (ou lista cronológica simples — ver §6).
- **Rota pública `/status`** → view `StatusView`: renderiza o **estado declarado manualmente** (`operacional` | `instabilidade` | `manutenção`) + um **texto opcional** (ex.: "Manutenção programada às 23h"), lido do **mesmo mecanismo estático**.
  - **Sem uptime automático/medido** (depende de F0-E1) — registrar como evolução futura na própria página e em §8.
- **Links**: no **rodapé da `LandingView`** (coluna apropriada) e no **menu de ajuda/conta do app** (dev localiza o menu canônico — ver §6).
- **Fonte de dados estática**: arquivo(s) versionado(s) no repo. **Decisão (default mais simples)**: módulo **TypeScript** tipado em `frontend/src/content/` (ex.: `changelog.ts` e `status.ts`), importado diretamente pelas views. Justificativa em §5/§11.

**Não inclui** (explicitamente fora):
- **Backend, banco, schema, migration, endpoint** — nada disso. Zero `imedto-database`.
- **Página de status com uptime medido** / health checks automáticos / histórico de incidentes com gráfico. Evolução futura (pós F0-E1).
- **RSS** do changelog.
- **Assinatura de e-mail** de novidades / notificação push de release.
- **CMS / painel de admin** para editar conteúdo. Conteúdo é via commit.
- **i18n** (multi-idioma). Conteúdo em pt-BR só.
- **Qualquer dado de cliente/tenant** nas páginas (são públicas e genéricas).

## 4. Regras de negócio

> Esta é uma feature **puramente de front, estática e pública**. "Regra de negócio no backend" (CLAUDE.md) **não se aplica** — não há lógica de domínio, não há dado de tenant, não há decisão de servidor. As "regras" abaixo são invariantes de produto/UX e de privacidade.

- **R1 — Públicas e sem auth**. `/novidades` e `/status` são acessíveis **sem login** (sem `meta.requiresAuth`, sem `...APP`/layout), exatamente como `/termos` e `/privacidade`. Mora em: **`router/index.ts`** (entradas públicas). Validado em: front (rota carrega deslogado).
- **R2 — Zero dado de tenant/cliente/PII**. As páginas renderizam **apenas** conteúdo estático genérico do produto. Nenhuma chamada a API autenticada, nenhum `usuario`/`tenant`/paciente, nenhum dado pessoal. Mora em: **views** (sem imports de stores de domínio/serviços autenticados). Validado em: front + inspeção de rede (CA).
- **R3 — Sem chamada de API**. O conteúdo vem de import estático (bundle), **não** de `fetch`/serviço. Carregamento instantâneo, funciona mesmo se o backend estiver fora (importante: a página de **status** precisa abrir mesmo com o sistema instável). Mora em: **views + módulo de conteúdo**. Validado em: front (nenhuma request ao abrir).
- **R4 — Estado de status declarado**. O status é um dos três valores fixos (`operacional` | `instabilidade` | `manutenção`) + texto opcional, definido no arquivo estático. **Decisão**: estado **único global** do sistema (não por componente/serviço) no MVP — o roadmap pede "status básico". Mora em: **módulo `status.ts`**. Validado em: front (renderiza o valor declarado).
- **R5 — Tags do changelog são um conjunto fechado**. `novidade` | `melhoria` | `correção` (3 tags, em pt-BR). Cada tag tem um rótulo + estilo visual (badge) consistente. Conjunto fechado evita proliferação. Mora em: **módulo `changelog.ts` (tipo) + view (mapa de estilo)**. Validado em: front.
- **R6 — Tom leigo**. Títulos e descrições do changelog são escritos para o **usuário final** (recepcionista, dono de clínica), **não** para devs. Nada de "refatorou o handler X" — sim "agora o relatório de faturamento exporta em CSV". (Diretriz editorial; o QA valida amostras, não cada texto.) Mora em: **conteúdo**.
- **R7 — Ordenação e datas**. Entradas exibidas da **mais recente para a mais antiga**. Datas em formato pt-BR legível (ex.: "9 de junho de 2026" ou "09/06/2026" — dev escolhe consistente com as views legais). Mora em: **view** (ordenação) + **conteúdo** (campo data ISO para ordenar de forma confiável).
- **R8 — Estado vazio do changelog**. Se não houver entradas (improvável, mas possível no 1º deploy), a página mostra um estado vazio gentil ("Em breve, novidades por aqui."). Mora em: **view**. Validado em: front.
- **R9 — Performance**. Estático, bundle-split por rota (lazy `import()` como as demais rotas). Sem peso para quem não visita. Mora em: **router (lazy) + views**. Validado em: front.

## 5. Modelo de dados

**SCHEMA NÃO MUDA. Nenhum banco. Nenhuma migration. `imedto-database` NÃO é acionado.**

O conteúdo vive em **módulo(s) TypeScript versionado(s)** no front — **decisão de produto: TS sobre JSON** (justificativa em §11). Local sugerido: `frontend/src/content/` (pasta nova; dev confirma a convenção mais alinhada à base — pode ser `src/data/` se já existir).

**Forma sugerida** (tipada — dev ajusta nomes):
```ts
// frontend/src/content/changelog.ts
export type TagChangelog = "novidade" | "melhoria" | "correção"
export interface EntradaChangelog {
  data: string        // ISO "2026-06-09" — usado para ordenar
  titulo: string      // leigo
  descricao: string   // leigo
  tag: TagChangelog
}
export const CHANGELOG: EntradaChangelog[] = [ /* entradas, mais recente primeiro */ ]

// frontend/src/content/status.ts
export type EstadoSistema = "operacional" | "instabilidade" | "manutenção"
export interface StatusSistema {
  estado: EstadoSistema
  texto?: string                 // opcional: detalhe/aviso
  atualizadoEm: string           // ISO — "última atualização declarada"
}
export const STATUS: StatusSistema = { estado: "operacional", atualizadoEm: "2026-06-10" }
```

**Sem PII, sem dado de tenant, sem segredo.** É conteúdo institucional público.

## 6. UX e fluxo

**Padrão base**: espelhar as views de `frontend/src/views/legal/` (`PrivacidadeView`/`TermosView`) — view standalone, sem `AppLayout`, cabeçalho com link "← Voltar" para a `Landing` e o logo. Centralizada (sem espaço em branco só de um lado — Docs/LGPD.md §container / Docs/DESIGN.md). Tipografia via tokens/DS (CLAUDE.md §5 — sem `font-size`/`font-weight` literais).

**`/novidades` (`NovidadesView`)**:
- Cabeçalho: título "Novidades" + subtítulo curto ("Tudo o que melhoramos no Imedto, em ordem cronológica.").
- Lista de entradas (mais recente no topo). Cada entrada:
  - **Badge da tag** (Novidade / Melhoria / Correção) com cor consistente — reusar `AppBadge` (`components/ui/AppBadge.vue`) se couber, ou estilo de pill do DS.
  - **Data** (pt-BR).
  - **Título** (`ds-card-title` ou nível equivalente) + **descrição** leiga.
- Agrupamento por data **opcional** (dev decide: lista simples cronológica é suficiente para o MVP).
- **Estado vazio** (R8): mensagem gentil ("Em breve, novidades por aqui.") — pode reusar `AppEmptyState`.

**`/status` (`StatusView`)**:
- Cabeçalho: título "Status do sistema".
- **Cartão de estado** grande e claro, com cor + ícone por estado:
  - `operacional` → verde, ícone check ("Todos os sistemas operacionais").
  - `instabilidade` → âmbar, ícone alerta ("Estamos com instabilidade").
  - `manutenção` → azul/neutro, ícone ferramenta ("Em manutenção programada").
  - Reusar os tokens de cor/estado do DS (ex.: `--success`/`--danger`/`--warning` se existirem; dev confirma) e, se houver, um componente de pill de status (`AppStatusPill`).
- **Texto opcional** abaixo do estado (R4), quando presente.
- **"Última atualização"**: exibir `atualizadoEm` em pt-BR.
- **Nota de evolução futura** discreta no rodapé da página: "Em breve: monitoramento de disponibilidade em tempo real." (alinha expectativa — não prometemos uptime medido ainda).

**Links de entrada**:
- **Rodapé da `LandingView`**: adicionar "Novidades" e "Status" — coluna apropriada (a coluna "legal" tem Termos/Privacidade; "Novidades" pode ir na coluna de produto e "Status" na de produto/legal — dev escolhe coerente com o layout do footer). Usar `<router-link :to="{ name: 'Novidades' }">` / `{ name: 'Status' }`.
- **Menu de ajuda/conta do app**: dev localiza o ponto canônico (ex.: dropdown de conta na sidebar/topbar, ou um menu "Ajuda"). Adicionar os dois links lá. Como as rotas são públicas, funcionam logado ou não. Se não houver um "menu de ajuda" óbvio, registrar e colocar no menu de conta junto a "Minha conta"/"Sair" (dev decide; ver §11).

**Estados**:
- Loading: instantâneo (estático) — sem spinner necessário.
- Erro: n/a (sem rede).
- Vazio: só o changelog (R8/CA).

**Mobile-ready**: ambas em coluna única, legíveis em telas pequenas (mesmo padrão das views legais).

## 7. Critérios de aceite (testáveis)

- **CA1 (rota pública — novidades)**: Dado um visitante **não logado**, Quando acessa `/novidades`, Então a página carrega normalmente (sem redirect para login) e exibe as entradas de changelog.
- **CA2 (rota pública — status)**: Dado um visitante **não logado**, Quando acessa `/status`, Então a página carrega e exibe o estado atual do sistema.
- **CA3 (sem chamada de API)**: Dado a aba de rede aberta, Quando o usuário abre `/novidades` ou `/status`, Então **nenhuma** requisição a `/api/*` é disparada por essas páginas (conteúdo vem do bundle estático).
- **CA4 (status abre com backend fora)**: Dado o backend indisponível, Quando o usuário acessa `/status`, Então a página **ainda** abre e mostra o estado declarado (porque não depende de API) — exatamente o cenário em que a página de status mais importa.
- **CA5 (ordenação do changelog)**: Dado entradas com datas variadas no arquivo, Quando `/novidades` renderiza, Então as entradas aparecem da **mais recente para a mais antiga**.
- **CA6 (tags renderizam)**: Dado entradas com tags `novidade`, `melhoria` e `correção`, Quando exibidas, Então cada uma mostra o badge correto com rótulo e cor consistentes; nenhuma tag fora do conjunto fechado é aceita pelo tipo (erro de compilação se inventarem uma).
- **CA7 (estado de status renderiza por valor)**: Dado `STATUS.estado = "instabilidade"` com `texto`, Quando `/status` renderiza, Então mostra o cartão âmbar "Estamos com instabilidade" **e** o texto; trocar o valor no arquivo para `"operacional"` (sem texto) muda o cartão para verde "Todos os sistemas operacionais".
- **CA8 (links no rodapé da landing)**: Dado a `LandingView`, Quando o usuário vê o rodapé, Então há links "Novidades" e "Status" que navegam para `/novidades` e `/status` (via `router-link`, sem reload).
- **CA9 (links no menu do app)**: Dado um usuário logado, Quando abre o menu de ajuda/conta, Então encontra os links "Novidades" e "Status" e ambos abrem as páginas públicas.
- **CA10 (LGPD/privacidade — zero dado de cliente)**: Dado o código e o DOM renderizado de `/novidades` e `/status`, Quando inspecionados, Então **não** há nenhum dado de tenant/cliente/paciente/PII, nenhum import de store de domínio ou serviço autenticado, e nenhum token/segredo.
- **CA11 (estado vazio do changelog)**: Dado `CHANGELOG = []`, Quando `/novidades` renderiza, Então mostra um estado vazio gentil ("Em breve, novidades por aqui.") em vez de uma lista quebrada.
- **CA12 (performance — lazy)**: Dado o app, Quando um usuário **não** visita `/novidades` nem `/status`, Então o conteúdo dessas páginas **não** é baixado (rotas com `import()` lazy, bundle-split); ao visitar, o chunk carrega sob demanda.
- **CA13 (regressão — rotas existentes)**: Dado as rotas públicas atuais (`/`, `/termos`, `/privacidade`, etc.), Quando as novas rotas são adicionadas, Então as existentes continuam funcionando e o catch-all 404 (`/:pathMatch(.*)*`) não captura `/novidades`/`/status` por engano.
- **CA14 (mobile)**: Dado um viewport estreito, Quando o usuário abre `/novidades` e `/status`, Então o conteúdo é legível em coluna única, sem overflow horizontal.

## 8. Riscos e dependências

- **Uptime real depende de F0-E1**: a página de status mostra estado **declarado manualmente**. Publicar números de disponibilidade medidos é evolução futura (pós Fase 0 de infra). A nota na própria página alinha expectativa. **Não** prometer SLA/uptime agora (roadmap é explícito).
- **Conteúdo estático = atualização por deploy**: mudar status/changelog exige commit + pipeline (~3-5 min). Em incidente, atualizar o status não é instantâneo. **Aceitável no MVP** (decisão de escopo: sem painel). Se virar dor, evolução futura é um endpoint/painel de status (fora deste briefing). Registrar.
- **Disciplina editorial**: o changelog só tem valor se for mantido. Não é risco técnico, é processo — a equipe Imedto precisa atualizar a cada release relevante. (Fora do escopo de código.)
- **Catch-all do router**: garantir que `/novidades` e `/status` sejam declaradas **antes** do `{ path: "/:pathMatch(.*)*", redirect: "/" }` (em `router/index.ts` o catch-all está antes do bloco admin; as novas rotas públicas devem ficar junto às demais públicas no topo). CA13 cobre.
- **Menu de ajuda/conta**: se não existir um menu "Ajuda" claro, o dev coloca no menu de conta. Decisão registrada em §11 para o QA não tratar como gap.

## 9. Observações para execução

**Não-negociável**:
- Páginas **públicas**, **estáticas**, **sem chamada de API**, **sem dado de tenant/PII** (R1/R2/R3/CA1-CA4/CA10).
- A página de **status precisa abrir com o backend fora** (R3/CA4) — por isso conteúdo no bundle, não fetch.
- Conjunto **fechado** de tags (R5/CA6) e de estados (R4/CA7), tipados em TS.
- Tipografia via tokens/DS (CLAUDE.md §5).
- **Não** acionar `imedto-database`; **não** tocar backend.

**Liberdade técnica (dev decide)**:
- Pasta exata do conteúdo (`src/content/` vs `src/data/`) e nomes dos módulos.
- Lista simples vs agrupada por data no changelog.
- Reuso de `AppBadge`/`AppStatusPill`/`AppEmptyState` vs estilo scoped enxuto (preferir reuso do DS).
- Em qual coluna do footer cada link entra; qual menu do app recebe os links.
- Formato de exibição da data (consistente com as views legais).

**Reuso obrigatório (grep antes de criar)**:
- Padrão de view pública: `frontend/src/views/legal/PrivacidadeView.vue` / `TermosView.vue` (estrutura, "← Voltar", sem layout).
- Padrão de rota pública: entradas `/termos` e `/privacidade` em `frontend/src/router/index.ts`.
- Footer da `LandingView.vue` (bloco de `router-link` legais).
- DS: `AppBadge`, `AppStatusPill`, `AppEmptyState` (`components/ui/`).
- Precedente de conteúdo estático no front: catálogos em `MinhaContaView.vue`.

**Aciona `imedto-database`**: **NÃO**. Sem schema, sem migration, sem backend.

## 10. Atualização de documentação

- **`Docs/DESIGN.md`** — adicionar uma nota curta no índice de views/rotas públicas (se houver tal seção) registrando o **padrão de página pública estática** (`/novidades`, `/status`) seguindo o modelo de `views/legal/`, e a convenção de **conteúdo estático versionado** em `src/content/` para changelog/status (sem API, sem banco). Mudança incremental. **Somente se** nascer um componente de DS novo (ex.: `AppStatusBanner`), promovê-lo e documentá-lo; caso contrário, só a nota de padrão.
- **`Docs/ARQUITETURA.md`** — **opcional/curto**: se o doc tiver uma seção de "rotas e layouts do front", citar que páginas públicas de confiança são SPA estáticas sem `requiresAuth`. Se não houver seção natural, **não** forçar.
- **`Docs/INFRA.md`** — **não** atualizar agora. Quando a página de **status** ganhar uptime **medido** (pós F0-E1), aí sim documentar o monitor externo e a fonte do dado. Registrar como evolução futura (não nesta entrega).
- **`Docs/LGPD.md`** — **sem alteração**: páginas públicas sem dado pessoal não introduzem nada de LGPD.

## 11. Premissas dos pontos abertos (resolvidas pelo BA; usuário pode corrigir via addendum antes do dev)

1. **Formato do arquivo estático** = **módulo TypeScript** (`src/content/changelog.ts`, `status.ts`), **não** JSON em `public/`. Justificativa: (a) **type-safety** — tag/estado fora do conjunto fechado viram erro de compilação (CA6); (b) **zero fetch** — vem no bundle, abre com backend fora (CA4); (c) alinha ao precedente de catálogos estáticos no front; (d) não há convenção de JSON em `public/` hoje. Se o usuário preferir JSON (ex.: para alguém não-dev editar mais fácil), vira addendum — mas perde o type-safety.
2. **Status = estado único global** (não por componente/serviço). O roadmap pede "status básico". Multi-componente é evolução futura.
3. **Sem uptime medido** — explícito no roadmap (depende de F0-E1). A página declara o estado manualmente e tem nota de "em breve, monitoramento em tempo real".
4. **Localização dos links no app** = rodapé da Landing (certo) + menu de ajuda/conta. Se não houver um menu "Ajuda" claro, vão para o menu de conta (junto a "Minha conta"/"Sair"). Dev confirma o ponto; não é gap.
5. **Tags do changelog** = `novidade | melhoria | correção` (3, fechado). **Estados** = `operacional | instabilidade | manutenção` (3, fechado).
6. **Sem i18n / RSS / e-mail de novidades / CMS** — todos fora, evolução futura.
