# Design e padrão de produto

> **Quando ler**: ao tocar UI, view, componente, layout, design system, fluxo visual, formulário, lista, drawer, modal, tema, cor, tipografia, responsividade.
>
> **Quando atualizar**: ao introduzir novo componente reutilizável, nova variante de `app-page`, novo token de cor, nova regra de UX cross-cutting. **Responsabilidade primária: `imedto-business-analyst`** (atualiza junto com o briefing) e `imedto-developer` (quando cria componente novo no design system).

---

## Premissa central

O Imedto é um produto único — toda tela, fluxo e interação precisa parecer parte do mesmo software, escrita pela mesma equipe. Estas premissas valem para qualquer mudança em qualquer área.

## Experiência consistente em todo o site

- **Container de página padrão**: toda view interna (dentro do `AppLayout`) começa com `<div class="app-page ...">` (definido em [main.css](../frontend/src/assets/main.css)). Centraliza, aplica padding e limita largura — evita o anti-padrão de "espaço em branco colado em um dos lados". Variantes:
  - `.app-page` (padrão, 1280px) — listas, cards, dashboards.
  - `.app-page--narrow` (880px) — formulários, perfil, telas de conta/configuração.
  - `.app-page--wide` (1480px) — relatórios, prontuário, calendários grandes.
  - `.app-page--full` (100%) — apenas para casos que exigem width inteiro (ex: agenda em modo mês).
  - **Não** declarar `max-width`/`margin: 0 auto`/`padding` próprio na raiz da view — usar a classe utilitária. Ao tocar uma view legada que ainda usa container próprio, migrar para `.app-page`.
- Mesmo "shape" de cabeçalho de página: `AppPageHeader` com título, subtítulo opcional e slot de ações.
- Mesmas primitivas para listas, drawers, modais, badges de status, toggles de período, cards e empty states (todos vivem em [frontend/src/components/ui/](../frontend/src/components/ui/) — ver [index.ts](../frontend/src/components/ui/index.ts) para a lista oficial do design system).
- **Listas paginadas**: usar sempre o `AppPagination` (`v-model:pagina` + `v-model:tamanho` + `:total`). O componente já fornece o seletor de itens por página (10/20/30 padrão), navegação numerada com ellipsis e o texto "1–20 de 47 itens" — não reimplementar lógica de página/ellipsis na view.
- **Botões de ação em tabelas**: usar as classes globais `.btn-icon` + `.btn-icon-ver` (olho/primary), `.btn-icon-editar` (lápis/azul), `.btn-icon-excluir` (lixeira/danger) definidas em [main.css](../frontend/src/assets/main.css). Não criar variantes scoped — duplicar em `<style scoped>` reabriria o bug "scoped data-attr atinge root do componente filho".
  - **Botão de ação por linha que alterna ícone (toggle de estado)**: quando a ação da linha deriva do estado atual do item, o ícone deve espelhar o estado atual e a ação é a inversa. Padrão aplicado ao toggle ativar/desativar de profissionais em `AbaProfissionais.vue`: `fa-circle-pause` quando Ativo (ação = Desativar), `fa-circle-play` quando Inativo (ação = Reativar). Regras: (a) o ícone reflete o estado atual — a ação é a inversa; (b) confirmação via `AppConfirmDialog` antes de disparar a request — `variante="danger"` para desativar, `variante="primary"` para reativar; (c) estado `disabled` + `title` orientativo quando a ação é inválida (ex.: reativar vínculo nunca aceito); (d) enquanto a request está em voo, o botão da linha fica `disabled` para evitar duplo disparo. Como `AbaProfissionais.vue` usa `.btn-icon-sm` scoped (consistência com o lápis vizinho pré-existente), variantes de cor do toggle são `.btn-icon-sm--pause` e `.btn-icon-sm--play` definidas localmente no mesmo `<style scoped>`.
- **Confirmações e feedback (AppConfirmDialog / AppToast)**: o app NÃO usa `window.confirm()`/`window.alert()` nativos — eles quebram o tema, travam a thread e são hostis em mobile. Dois padrões distintos do design system, nunca confundir:
  - **Confirmar antes de agir** (excluir, inativar, cancelar, remover, recusar, baixar pagamento, converter): usar [`AppConfirmDialog`](../frontend/src/components/ui/AppConfirmDialog.vue) com `v-model:aberto`, `titulo`, `:mensagem`, `confirmar-rotulo`, `:executando` e `@confirmar`. `variante="danger"` para ação destrutiva/irreversível; `variante="primary"` para transição de estado não-destrutiva (ex.: converter orçamento em cirurgia). A request só dispara no `@confirmar`; enquanto está em voo, `:executando="true"` mostra loading no botão e bloqueia fechar (evita duplo disparo).
  - **Avisar depois de agir** (sucesso/erro de operação): usar [`AppToast`](../frontend/src/components/ui/AppToast.vue), nunca `AppConfirmDialog` para erro. Padrão canônico: estado local `const toast = ref<{ mensagem, variante } | null>(null)` + função `notificar(mensagem, variante)`, renderizado com `<AppToast v-if="toast" ... @fechar="toast = null" />` (ele se auto-fecha em ~3.5s via Teleport). Toast de erro usa a mensagem do backend: `notificar(e?.response?.data?.mensagem ?? "Falha ao ...", "error")` — o 422 do `BusinessException` é a fonte da verdade. `notificacoesStore` é a feature de notificações in-app (sino), NÃO o mecanismo de toast.
  - Gabarito de referência completo: [`ProcedimentosTab.vue`](../frontend/src/components/orcamento/config/ProcedimentosTab.vue).
- **Buscas que tocam a API**: TODO input cujo valor dispara request HTTP precisa de debounce (~300 ms) — caso contrário cada caractere digitado vira uma requisição. Use o composable [useDebouncedRef](../frontend/src/composables/useDebouncedRef.ts), nunca `setTimeout` manual. Padrão:
  ```ts
  const buscaInput = ref("")                       // v-model do <input>
  const busca      = useDebouncedRef(buscaInput)   // ref atrasado que aciona a request

  watch(busca, () => { pagina.value = 1 })
  watch([busca, pagina, tamanho], () => carregar(), { immediate: true })
  ```
  Filtros que rodam **client-side** (lista já carregada e filtrada por `computed`) não precisam de debounce — apenas inputs cujo valor passa para um service/HTTP. Cliques em paginação/ordenação devem ser imediatos (não ficam atrás do debounce).
- Cores, tipografia, espaçamentos, raios e sombras vêm dos tokens HSL em [frontend/src/assets/main.css](../frontend/src/assets/main.css) — **nunca** hardcodar cores ou montar botão/input do zero.
- Estados (loading, vazio, erro, sucesso, desabilitado) sempre presentes e uniformes — uma tabela vazia usa `AppEmptyState`, não um `<p>` solto.
- Mensagens de erro de negócio vêm do backend (`BusinessException` → 422) e são exibidas no mesmo formato em todas as telas.

## Componentização máxima

- Antes de escrever HTML/CSS scoped numa view, perguntar: *"isso aparece em outra tela ou pode aparecer?"*. Se sim → componente em `components/ui/` ou `components/<dominio>/`.
- Trecho de template repetido entre views (mesmo similar) é cheiro: extraia para componente parametrizado por props.
- Evitar componentes "Frankenstein" com 15 props booleanas — quebrar em vários componentes menores compostos é melhor que um componente único configurável.
- Drawers, modais e formulários compartilhados entre fluxos viram componentes (ex: [AgendamentoFormFields](../frontend/src/components/agenda/AgendamentoFormFields.vue) é compartilhado entre criar e editar).
- **Todos os componentes do frontend devem pegar do design system** com objetivo de padronizar. Só criar algo scoped quando for bem específico para aquele cenário e **não** for reutilizável.
- **Caso o componente não exista no design system e seja reutilizável**, deve ser criado no design system primeiro, e depois importado no front. Mantém padronização.
- **Antes de criar componente novo no front**, verifique se ele existe no design system.

## Composables compartilhados

Lógica assíncrona reutilizável entre dois ou mais componentes vive em `frontend/src/composables/`. Composables são a alternativa adequada a copiar lógica de `watch`/`ref`/`computed` entre modais e views.

- **`useProfissaoEspecialidade`** (briefing 2026-06-04_003): mecânica de cascata profissão→especialidade por catálogo estrito. Expõe `profissoes`, `especialidades`, `profissaoId`, `especialidade`, `carregandoEspecialidades`, `profissaoTemEspecialidades`, `conselhoSigla`, `carregarProfissoes`, `reset`, `inicializarComVinculo`. Usado por `ConvidarProfissionalModal.vue` e `ProfissionalDetalhesModal.vue` — fonte única, sem duplicação. Regra: trocar profissão limpa especialidade; `inicializarComVinculo` pré-seleciona sem limpar (abertura do modal de detalhes com dados existentes).

## Padrão de desenvolvimento

- Backend segue DDD + CQRS (commands/queries/events) com **regra de negócio sempre no domain/handler**, nunca no controller, no SQL ou na view.
- Frontend: views consomem **stores Pinia** ou **services**, nunca `httpClient` direto; HTTP só passa por `*Service` em [frontend/src/services/](../frontend/src/services/).
- Toda nova feature é validada com type-check (`vue-tsc`/`dotnet build`) e, quando o caso, testes (`dotnet test`/`vitest`) **antes de declarar pronto**.
- Trava do front sempre tem trava espelhada no back (defense-in-depth: 422 do backend é a fonte da verdade; o front é UX).

## Reuso > duplicação

Antes de criar **qualquer** endpoint, query, repositório, store, service no front, componente UI, helper ou DTO, **procure o que já existe** e reutilize. Duplicar lógica fragmenta regras, divide a verdade entre dois lugares e gera bugs sutis quando um lado muda e o outro não.

Como aplicar:
- **Backend**: antes de adicionar um método em `*QueryRepository` ou um endpoint novo no controller, faça `grep`/`Glob` por algo equivalente. Procure por padrões: nome do conceito (`Profissional`, `Vinculo`, `Dono`), nome do dado (`especialidade`), nome da operação (`Listar*`, `Obter*`, `Tem*`).
- **Frontend**: antes de criar um `*Service` novo, ver se já existe método equivalente em [frontend/src/services/](../frontend/src/services/). Antes de criar componente, ver [frontend/src/components/ui/](../frontend/src/components/ui/) e [frontend/src/components/ui/index.ts](../frontend/src/components/ui/index.ts) (lista oficial do design system).
- **DTOs/queries**: se o DTO retornado já tem o campo que você precisa para uma nova tela, reuse — não crie um DTO paralelo só para "deixar mais limpo". Estenda o existente ou reaproveite.
- **Quando duplicar é inevitável** (ex: a query existente faz join pesado e a nova precisa ser leve): documente o porquê em comentário ou commit message, e cite a outra como referência.

Se um conceito de domínio aparece em **duas operações diferentes** com a mesma regra (ex: "este usuário pode atuar como profissional neste estabelecimento" → vale para criar agendamento, editar agendamento, listar disponibilidade), extraia em **uma** função do repositório e chame nos dois lugares — não copie o `if`.

## Módulo Admin Global

O módulo administrativo (`frontend/src/modules/admin/`) **reusa integralmente o design system do app principal** — `AppTopBar`, `AppSidebar`, `AppPageHeader`, `AppCard`, `AppModal`, `AppButton`, `AppField`, `AppInput`, `AppTextarea`, `AppCheckbox`, `AppSelect`, `AppBadge`, `AppStatusPill`, `AppSearchInput`, `AppEmptyState`, `AppPagination`, `AppToast`. Zero hex code próprio no CSS scoped do módulo; zero classes `admin-*`, `secao-*`, `page-*`, `assinatura-*` (Wave 3 — briefing 2026-05-30_003).

Marcação de zona privilegiada:
- **Badge "Admin"** discreto no topbar com `variant="error"` (AppBadge), ao lado do logo.
- **Faixa de 2px** na borda superior do topbar com `hsl(var(--warning))` — sinal visual de área administrativa sem poluir a UX.

Shell do módulo:
- `AdminLayout.vue` — `AppTopBar` (slot `#brand` + slot `#perfil`) + `AppSidebar` (items estáticos). Sem `AppLayout` do app principal — o admin tem seu próprio shell.
- Todas as views usam `<main class="app-page">` (ou `--narrow` para formulários) como raiz.

Modais internos (assinatura, reset, revelar CPF) usam `AppModal` — sem `Teleport` manual nem overlay customizado.

O isolamento físico do módulo é mantido: só importa de `@/components/ui/`, `@/composables/` e `@/assets/main.css`. Nenhum import cruzado com outros módulos do app. Decisão consciente para preservar extração futura (briefing 2026-05-30_001 §10).

**Componente exclusivo do módulo admin:**
- `RegiaoTreeView.vue` em `modules/admin/components/regioes/` — render hierárquico expand/colapse para `regioes_anatomicas_catalogo`. Agrupamento por `vista` (anterior/posterior); aninhamento por `pai_codigo` até nível 3. Sem virtualização (volume baixo — 144 registros). Sem drag-and-drop; reordenação via input `ordem`. Emite eventos `select`, `criar-filho`, `editar`, `excluir`, `inativar`, `reativar`. Adicionado em Wave 4 (briefing `planejamentos/2026-05-30_004_admin-global-wave4-catalogos-livelink.md`).

**Componentes de domínio compartilhados (tenant + admin):**
- `ModeloProntuarioBuilder.vue` em `components/ui/` — builder visual de modelos de prontuário, compartilhado entre `ModelosProntuarioView` (tenant, `/configuracoes/modelos-prontuario`) e `ModelosGlobaisFormView` (admin global). Aceita `v-model:nome`, `v-model:descricao`, `v-model:estruturaJson` (string JSON, shape array `[{ chave, titulo, tipo, ordem }]`) e emite `update:valido`. A constante exportada `SECOES_MODELO_PRONTUARIO` é a **fonte única de verdade** das 17 seções suportadas pelo prontuário — nenhuma outra cópia desse catálogo deve existir no front. Reordenação via botões ↑/↓ (drag-and-drop é extensão futura). Retrocompatível com JSON manual criado antes do builder: seções com chaves fora das 17 conhecidas são preservadas intactas com aviso visual. Adicionado em Wave 5 (briefing `planejamentos/2026-05-30_005_admin-global-wave5-builder-visual.md`).

## Componentes de seleção segmentada

### `AppPillToggle` (briefing 2026-06-08_001)

Toggle segmentado para escolha entre opções mutuamente exclusivas. Localização: [`frontend/src/components/ui/AppPillToggle.vue`](../frontend/src/components/ui/AppPillToggle.vue) (wrapper do `PillToggle` do design system em `design-system/src/components/pill-toggle/`).

**API (genérica, `T extends string | number`):**
- `modelValue: T` — valor atualmente selecionado. Não precisa estar em `opcoes` para evitar erros; passar `""` como estado neutro é válido.
- `opcoes: Array<{ valor: T; label: string; icone?: string; icon?: string }>` — lista de opções exibidas como pílulas.
- `@update:modelValue` — emitido ao clicar em uma opção.

**Comportamento:** fundo `bg-muted`, opção ativa com `bg-primary text-primary-foreground`. Clique emite imediatamente — adequado para fluxos onde a seleção avança o estado (ex: escolha de lado → avança passo do popup).

**Caso de uso canônico:** passo de lateralidade do `RegionSelectorPopup` (Direito / Esquerdo / Ambos) — a seleção é uma ação que avança o fluxo, não um toggle persistente de estado.

**Exemplo:**
```vue
<AppPillToggle
  model-value=""
  :opcoes="[{ valor: 'D', label: 'Direito' }, { valor: 'E', label: 'Esquerdo' }]"
  @update:model-value="onEscolher($event)"
/>
```

## Componentes de exibição contextual

### `AppPopover` (briefing 2026-06-04_007)

Painel flutuante genérico e reutilizável, ancorado a um elemento gatilho. Localização: [`frontend/src/components/ui/AppPopover.vue`](../frontend/src/components/ui/AppPopover.vue).

**API de slots:**
- `#gatilho="{ abrir, fechar, toggle, aberto }"` — elemento que dispara a abertura. Deve ser focável (botão ou elemento com `tabindex`); o slot-scope expõe as funções para controle manual.
- `#conteudo="{ fechar }"` — corpo do painel flutuante. Renderizado dentro do painel (via Teleport para `<body>`).

**Props:**
- `posicao?: "bottom-start" | "bottom-end" | "top-start" | "top-end"` (default: `"bottom-start"`) — posição preferida; inverte automaticamente se não couber na viewport.
- `offset?: number` (default: `6`) — deslocamento vertical em px entre gatilho e painel.

**Comportamento:**
- Fecha ao clicar fora do painel e do gatilho.
- Fecha ao pressionar `Esc` (propaga `stopPropagation`).
- Devolve foco ao gatilho ao fechar.
- Clampa posição na viewport (nunca estoura borda; inclui margem de 8px).
- Sem overlay/backdrop — uso para listas de detalhe ancoras a um gatilho (não para modais que exijam atenção total).

**Casos de uso esperados:** listas de detalhe só-leitura ancoradas a um contador ou badge (ex: "N profissionais" → popover listando nome + avatar + status); tooltips ricos; menus de contexto simples. Não substituir `AppModal` para fluxos que exigem foco total.

**Exemplo mínimo:**
```vue
<AppPopover posicao="bottom-start">
  <template #gatilho="{ toggle }">
    <button type="button" @click="toggle">3 profissionais</button>
  </template>
  <template #conteudo>
    <div>Conteúdo aqui</div>
  </template>
</AppPopover>
```

### `AppAutocompleteCriavel` (briefing 2026-06-05_001)

Typeahead de texto livre com sugestões do pool de variáveis. Localização: [`frontend/src/components/ui/AppAutocompleteCriavel.vue`](../frontend/src/components/ui/AppAutocompleteCriavel.vue).

**Diferença fundamental de `AppSelectComCriacao`:**
- `AppSelectComCriacao` — select por **id** (`v-model: number`); criação via modal; vincula a entidade mestre pelo id.
- `AppAutocompleteCriavel` — typeahead por **string nome** (`v-model: string`); valores inéditos são aceitos livremente; a criação no pool ocorre no backend ao salvar a evolução (`PoolExtratorEvolucao`).

**Props:**
- `modelValue: string` — o nome como string.
- `opcoes: string[]` — lista de nomes do tipo (padrão-sistema + estabelecimento, já carregada).
- `placeholder?: string`
- `disabled?: boolean`
- `carregando?: boolean` — enquanto verdadeiro, input desabilitado + sem dropdown.
- `erro?: boolean` — quando verdadeiro, degrada para input de texto puro (CA11); preenchimento e salvamento continuam funcionando.

**Comportamento:**
- Dropdown abre ao focar ou digitar.
- Filtro client-side (normalização: trim + lower + sem acento), sem request por tecla.
- Lista vazia → mensagem "Nenhuma opção cadastrada — digite para criar uma nova".
- Navegação por teclado: ↑↓ destaca, Enter seleciona, Esc fecha.
- Fecha ao clicar fora.

**Uso nos prontuários:**
- `SecaoHistoriaPregressa.vue`: campos `nome` de alergias, medicações, cirurgias, doenças.
- `SecaoHistoriaFamiliar.vue`: campo `parentesco` de parentes (tipo RelacaoFamiliar).
- As listas são carregadas uma vez no `onMounted` da seção via `variavelPoolService.listar(tipo)`.

## Padrão master-detail de página de configurações (briefing 2026-06-08_002)

A página `/estabelecimento` usa layout **master-detail**: sub-nav agrupada (~248px) à esquerda + painel de detalhe à direita que troca conforme a seção selecionada. Padrão aplicado em `EstabelecimentoView.vue`.

### Estrutura

- **Sub-nav lateral**: 3 grupos rotulados ("Estabelecimento", "Modelos e listas", "Recursos") com busca client-side no topo. Itens condicionados a permissão são ocultados (RBAC por ocultação, R4 — gates de `permissoesStore`). Largura fixa ~248px; em <860px colapsa para coluna única.
- **Painel de detalhe**: monta o componente da seção ativa. Seções não-abertas **não montam** o componente nem disparam consultas (lazy-load real via `v-if` por seção + `defineAsyncComponent` para painéis pesados externos). **Nunca usar `v-show`** — manteria o componente montado e dispararia consultas de fundo.
- **Deep-link via `?secao=`**: sincronizado bidirecionalmente com o router via `router.replace` (não `push`) para não poluir o histórico. Espelha o padrão `?aba=` de `OrcamentoSettingsView`/`TermosListaView`/`RelatoriosView`. Seção inválida ou sem permissão cai na seção default ("dados").

### Painéis inline (reuso de componentes existentes)

Seções que eram views separadas são embarcadas via `defineAsyncComponent` — o componente existente é **reusado, não reescrito**. Para o editor de Termos, o estado lista ↔ editor é gerenciado localmente no componente `TermosPainelEmbutido.vue`, sem navegação de rota (R7).

### Rotas antigas → redirects

As rotas `/automacoes`, `/configuracoes/ia`, `/minha-assinatura`, `/configuracoes/modelos-prontuario`, `/configuracoes/termos` (e sub-rotas) redirecionam para `/estabelecimento?secao=<id>`, espelhando o padrão de aliases de Relatórios em `router/index.ts`.

### Item de backlog

Quando houver um segundo caso de uso de master-detail, extrair para componente genérico do design system (ex: `AppMasterDetail`/`AppSubNav`). Esta entrega resolve exclusivamente a página de configurações — sem extração prematura.

## Escala tipográfica (fonte única de verdade — briefing 2026-06-08_003)

Toda tipografia do produto usa a escala canônica de tokens definida em `frontend/src/assets/main.css` (`:root`). **Não use valores literais de `font-size` ou `font-weight` em CSS scoped** — consulte a tabela abaixo e use o token correspondente.

### Tabela de tokens

| Token | Valor | px equivalente |
|-------|-------|----------------|
| `--text-2xs` | `0.625rem` | 10px |
| `--text-xs` | `0.75rem` | 12px |
| `--text-sm` | `0.8125rem` | 13px |
| `--text-base` | `0.875rem` | 14px |
| `--text-md` | `0.9375rem` | 15px |
| `--text-lg` | `1.125rem` | 18px |
| `--text-xl` | `1.3125rem` | 21px |
| `--text-2xl` | `1.5rem` | 24px |
| `--text-3xl` | `1.875rem` | 30px |

| Token | Valor |
|-------|-------|
| `--font-weight-regular` | 400 |
| `--font-weight-medium` | 500 |
| `--font-weight-semibold` | 600 |
| `--font-weight-bold` | 700 |
| `--font-weight-extrabold` | 800 |

| Token | Valor |
|-------|-------|
| `--line-height-none` | 1 |
| `--line-height-tight` | 1.15 |
| `--line-height-snug` | 1.3 |
| `--line-height-normal` | 1.5 |
| `--tracking-title` | -0.015em |
| `--tracking-section` | -0.01em |

### Mapa semântico — quando usar cada elemento tipográfico

| Contexto | Elemento | Tamanho | Peso | Classe/componente |
|----------|----------|---------|------|-------------------|
| Título de página | `<AppPageHeader>` | 30px (`--text-3xl`) | 800 | `AppPageHeader` (DS) |
| Título de seção/painel | `h2` / `h3` | 21px (`--text-xl`) | 800 | `.ds-section-title` |
| Título de card inline | `h2` / `h3` | 15px (`--text-md`) | 700 | `.ds-card-title` |
| Label de campo | `<AppField>` / `<AppLabel>` | 12px (`--text-xs`) | 600 | `AppField` / `AppLabel` (DS) |
| Texto de campo/input | `<input>` / `<select>` | 13px (`--text-sm`) | — | `.form-input` global |
| Botões | `.btn-*` | 13px (`--text-sm`) | — | `.btn-*` global em `main.css` |
| Corpo de texto / linhas de tabela | — | 14px (`--text-base`) | — | herda do `body` |
| Sub-texto / caption | `<p class="ds-sub">` | 14px (`--text-base`) | — | `.ds-sub` |

### Regras não-negociáveis

1. **Tokens, nunca literais**: toda declaração `font-size` em CSS scoped deve usar `var(--text-*)`. Nunca `font-size: 18px`, `font-size: 1rem`, etc.
2. **Fonte única**: os tokens vivem apenas em `main.css` (`:root`) e em `design-system/src/styles/tokens.css`. Não redefinir em outros arquivos.
3. **AppPageHeader para títulos de página**: views internas com `class="app-page"` devem usar `<AppPageHeader titulo="..." />` — não `<h1>` cru. Exceção: views públicas/auth onde o layout é customizado.
4. **`.ds-section-title` para seções**: substituir `<h2>Título</h2>` com CSS scoped por `<h2 class="ds-section-title">`.
5. **`.ds-card-title` para cards**: subtítulos de card inline.
6. **Inputs e botões a 13px**: aplicado de forma centralizada no seletor base (`.form-input`, `.btn-*` em `main.css`). Não sobrescrever tela a tela.
7. **Labels a 12px/600**: `AppLabel`/`AppField` já renderizam no padrão. Não criar `.campo-label` scoped.

### Classes utilitárias globais (definidas em `main.css`)

```css
.ds-section-title { font-size: var(--text-xl); font-weight: var(--font-weight-extrabold); letter-spacing: var(--tracking-section); }
.ds-card-title    { font-size: var(--text-md);  font-weight: var(--font-weight-bold); }
.ds-sub           { font-size: var(--text-base); color: hsl(var(--secondary) / 0.65); }
```

### Tailwind preset alinhado

O preset em `design-system/src/tailwind/preset.js` mapeia as classes Tailwind `text-xs` ... `text-3xl` para os tokens: `text-sm` → `var(--text-sm, 0.8125rem)` etc. Ao usar Tailwind de tamanho dentro de componentes DS, os valores resultam nos mesmos px da escala canônica.

## Documentos relacionados

- [`DESIGN_SYSTEM.md`](DESIGN_SYSTEM.md) — referência completa de componentes do design system, tokens, e variantes.
- [`03A_FASE_3_UX_ADR.md`](03A_FASE_3_UX_ADR.md) — decisões de UX históricas (ADRs).
- [`Discoverys/tipografia/01_discovery.md`](Discoverys/tipografia/01_discovery.md) — auditoria de fragmentação e escala canônica adotada.
