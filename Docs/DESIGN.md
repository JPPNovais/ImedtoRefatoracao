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

- **`useCepAutofill`** (briefing 2026-06-13_001) — **padrão obrigatório para qualquer campo de CEP**. Encerra a divergência de três implementações (fetch cru, @blur, watch manual). API:
  ```ts
  useCepAutofill(cepRef, onEndereco, { onLimpar?, delay? })
    → { buscando: Ref<boolean> }
  ```
  Garantias: R1 (disparo só com 8 dígitos), R2 (limpeza síncrona imediata ao CEP < 8 dígitos via `onLimpar`), R3 (debounce 300–400ms), R4 (guard de race `reqId` — última requisição vence), R5 (não sobrescreve o que o usuário já digitou — cada tela implementa no `onEndereco` com `e.campo || form.campo`), R6 (erro silencioso). Usa o service canônico `viaCepService.buscarPorCep` (campos `cidade`/`uf`). O `utils/viaCep.ts` foi removido (briefing 2026-06-13_001). Telas de edição: abrir com CEP preenchido não apaga campos porque R5 protege; `onLimpar` só dispara quando o usuário apaga dígitos.
  
  UX: exibir hint "buscando..." no label do campo CEP enquanto `buscando.value === true` (padrão visual idêntico ao OnboardingView).

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

Modais internos (assinatura, reset, revelar CPF, troca de senha) usam `AppModal` — sem `Teleport` manual nem overlay customizado.

Slot `#perfil` do `AppTopBar` no `AdminLayout` expõe ações de sessão do admin logado: botão "Alterar senha" (abre `AdminAlterarSenhaModal` no shell, acima do "Sair") e botão "Sair" (logout). Padrão: botão `ghost` para ações neutras, `danger` para logout.

O isolamento físico do módulo é mantido: só importa de `@/components/ui/`, `@/composables/` e `@/assets/main.css`. Nenhum import cruzado com outros módulos do app. Decisão consciente para preservar extração futura (briefing 2026-05-30_001 §10).

**Componente exclusivo do módulo admin:**
- `RegiaoTreeView.vue` em `modules/admin/components/regioes/` — render hierárquico expand/colapse para `regioes_anatomicas_catalogo`. Agrupamento por `vista` (anterior/posterior/circunferencial); aninhamento por `pai_codigo` até nível 3. Sem virtualização (volume baixo — 144 registros). Sem drag-and-drop; reordenação via input `ordem`. Emite eventos: `editar` (lápis, sempre visível), `inativar` (ban, ação primária de remoção — só para nós `ativo=true`), `reativar` (seta-volta, só para nós `ativo=false`), `excluir` (lixeira, ação **secundária** — desabilitada quando o nó tem filhos, com tooltip "Possui sub-regiões — inative ou remova-as primeiro"). Adicionado em Wave 4 (briefing `planejamentos/2026-05-30_004_admin-global-wave4-catalogos-livelink.md`); comportamento de inativar-primário/reativar reconciliado em B3 (briefing `planejamentos/2026-06-08_007_admin-ui-catalogo-regioes-anatomicas.md`).

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

### `AppKpiCard` (briefing 2026-06-11_002)

Card de KPI com borda superior colorida e chip de ícone. Localização: [`frontend/src/components/ui/AppKpiCard.vue`](../frontend/src/components/ui/AppKpiCard.vue).

**Props:**
- `label: string` — rótulo descritivo da métrica (ex.: "Recebido").
- `valor: string | number` — valor formatado exibido em destaque.
- `icone?: string` — classe Font Awesome do ícone (ex.: `"fa-solid fa-arrow-down-long"`).
- `variante?: "success" | "warning" | "error" | "primary" | "muted"` — determina cor da borda superior e do chip.
- `sub?: string` — linha secundária opcional abaixo do valor.

**Variantes de cor:** mapeiam para tokens HSL: `success` → `hsl(var(--success))`, `warning` → `hsl(var(--warning))`, `error` → `hsl(var(--destructive))`, `primary` → `hsl(var(--primary))`, `muted` → cinza.

**Uso canônico:**
```vue
<AppKpiCard
  label="Recebido"
  :valor="moeda(kpis.recebido)"
  icone="fa-solid fa-arrow-down-long"
  variante="success"
/>
```

**Estilo:** `.kpi-card` e variantes de borda (`.kpi--*`) estão declarados em `scoped` do componente. CSS do DS não é carregado automaticamente pelo frontend — qualquer estilo global deve ir em `frontend/src/assets/main.css`.

### `AppAlertCard` (briefing 2026-06-12_001)

Card de alerta acionável. Card inteiro é clicável (router-link). Usado no bloco "Precisa da sua atenção" da Home para conduzir o usuário diretamente à tela relevante já filtrada. Localização: [`frontend/src/components/ui/AppAlertCard.vue`](../frontend/src/components/ui/AppAlertCard.vue).

**Props:**
- `to: RouteLocationRaw` — destino da navegação (passado ao `RouterLink`).
- `titulo: string` — rótulo do alerta (ex.: "Lançamentos vencidos").
- `icone?: string` — classe Font Awesome.
- `contagem: number` — número exibido no badge.
- `variante?: "error" | "warning" | "info"` — determina cor da borda lateral, chip e CTA.

**Slot:**
- `#contexto` — conteúdo secundário abaixo do header (ex.: "R$ 150,00 a receber").

**Variantes de cor:** `error` → `hsl(var(--destructive))`, `warning` → `hsl(var(--warning))`, `info` → `hsl(var(--primary))`.

**Uso canônico:**
```vue
<AppAlertCard
  :to="{ name: 'Financeiro', query: { filtro: 'vencidos' } }"
  titulo="Lançamentos vencidos"
  icone="fa-solid fa-circle-exclamation"
  :contagem="3"
  variante="error"
>
  <template #contexto>
    <span>R$ 150,00 a receber</span>
  </template>
</AppAlertCard>
```

**Diferença de `AppKpiCard`:** enquanto `AppKpiCard` exibe métricas de KPI (não é clicável), `AppAlertCard` é um card de navegação acionável — toda a área é link.

## Bloco "Precisa da sua atenção" — Home (briefing 2026-06-12_001)

Bloco de alerta operacional posicionado **logo após o cabeçalho "Olá, [nome]"** e **antes** da faixa de KPIs neutros em `HomeView.vue`.

**Regras de renderização:**
- O bloco só aparece quando há **ao menos um card de alerta visível** (pendência > 0 E rota destino acessível ao usuário via `podeAcessarRota`). Sem pendência visível, o bloco inteiro não renderiza (sem cabeçalho órfão).
- Cada card é ocultado individualmente se o usuário não tem acesso à rota destino (gate RBAC via `podeAcessarRota` + `routePermissions.ts` — fonte única de verdade).
- Estado de loading: enquanto o dashboard não carregou, o bloco não aparece (não renderiza cards com dados indefinidos).

**Cards disponíveis (contrato de deep-link):**
| Card | Dado | Destino | Query param |
|------|------|---------|-------------|
| Lançamentos vencidos | `lancamentosVencidos` + `vencidosAReceber`/`vencidosAPagar` | Financeiro | `?filtro=vencidos` |
| Itens abaixo do mínimo | `itensAbaixoMinimo` | Inventário | `?status=baixo` |
| Orçamentos pendentes | `orcamentosPendentes` | Orçamentos | `?status=pendentes` |

**Grid responsivo:** `grid-template-columns: repeat(auto-fill, minmax(260px, 1fr))`.

**Tipografia:** cabeçalho do bloco usa `<h2 class="ds-section-title">` (15px/700 via DS). Card usa tokens `--text-sm`, `--text-xs`, `--font-weight-semibold`, `--font-weight-bold`.

## Widget global de tarefas pendentes — `WidgetProximosPassos` (addendum 2, F3B)

`WidgetProximosPassos` é um **widget global persistente** montado uma única vez em `AppLayout.vue`. Fica visível em todas as rotas autenticadas enquanto houver pendências do último atendimento na sessão.

**Responsabilidade de estado:** `proximosPassosStore` (Pinia). A store persiste em `sessionStorage` (chave `imedto.proximosPassos`) — sobrevive a reload na mesma aba; some ao fechar a aba/browser. Reidratação ocorre no boot (`main.ts → proximosPassosStore.reidratar()`).

**Fluxo de uso:**
1. `ProntuarioView` chama `proximosPassosStore.iniciar({ pacienteId, evolucaoId, acoesMarcadas })` ao salvar evolução com ≥1 ação de conduta.
2. O widget (já montado no layout) reage e exibe expandido no canto inferior direito.
3. A cada troca de rota, o widget chama `atualizarAbertas()` para refletir conclusões.
4. Quando todas as ações estão concluídas, exibe feedback breve "Tudo concluído!" e some.

**Estados:** expandido / minimizado (pílula) / concluido (transitório ~2s) / fechado.

**Pílula minimizada:** fundo `hsl(var(--primary))` (sólido, não transparente), texto e ícone brancos, contador "X/N". Padrão de "indicador flutuante de tarefas pendentes" — a pílula é `<button>` com `aria-label` descritivo.

**Fechar com confirmação:** clicar no X com ≥1 pendência aberta → `AppConfirmDialog` ("Fechar sem concluir as pendências? Elas continuam no painel do paciente."). Sem abertas → fecha direto.

**Z-index:** faixa 700–800 (abaixo de modais em 900). `Teleport to="body"`.

**Limpeza multi-tenant (R30):** a store é zerada em logout (`authStore.limparSessao`) e em troca de estabelecimento (`tenantStore.selecionar` com `trocouEstab`), pelo mesmo padrão de import dinâmico da `assinaturaStore`.

**LGPD:** o `sessionStorage` guarda apenas ids técnicos (`pacienteId`, `evolucaoId`) e enums de ação — sem texto clínico, dentro do padrão de minimização.

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

### Padrão "Modelos e listas" — variante título+corpo (briefing 2026-06-13_002)

Seções do grupo "Modelos e listas" em `EstabelecimentoView` que gerenciam registros com **título curto + corpo longo** (ex: `ModelosDescricaoCirurgicaTab.vue`) seguem este padrão:

**CRUD inline:**
- Form em `AppCard` (acima da lista) com `AppInput` para título (máx. 200 chars) e `AppTextarea` para corpo. O mesmo card serve criar e editar — o título muda de "Novo modelo" para "Editar modelo" via `editandoId`.
- Validação client-side (campos obrigatórios) + toast de erro com mensagem do backend (422).
- Botão "Cancelar" visível apenas no modo edição.

**Lista:**
- Itens com `.mdc-item`: `flex` horizontal com `mdc-item-info` (flex:1) e `mdc-item-acoes` (ações de editar/excluir).
- Badge `.badge-padrao` (pílula com `hsl(var(--secondary))`) para itens `ehPadraoSistema = true`.
- Botões `.btn-icon .btn-icon-editar` e `.btn-icon .btn-icon-excluir` **ausentes** para itens padrão do sistema — protege invariante do aggregate.
- Preview truncado: `corpo.slice(0, 120)` + `"…"`.

**Permissão (prop `podeEditar`):**
- `podeEditar = false`: exibe aviso `.aviso-leitura` e esconde form e botões de ação — apenas leitura.
- `podeEditar = true`: form visível + ações habilitadas.
- O pai passa `podeEditar` via `permissoes.podeExtra("modelos_prontuario")`.

**Lazy load da seção:** o componente só monta (e só chama o service) quando a seção está ativa (`v-if` em `EstabelecimentoView`). Segue o padrão de toda seção do master-detail.

### Padrão "Usar template" inline no módulo de prontuário (briefing 2026-06-13_002)

Para seções de texto longo no prontuário que suportam aplicação de template pré-cadastrado (ex: `desc-cirurgica`):

**Botão no header do módulo:**
- `<div class="module-action">` com `AppButton variant="ghost" size="sm"` e ícone `fa-file-import`.
- Condicionado à chave da seção: `v-if="secao.chave === 'desc-cirurgica'"`.

**Componente `SeletorModeloProntuario`:**
- Localização: [`frontend/src/components/prontuario/SeletorModeloProntuario.vue`](../frontend/src/components/prontuario/SeletorModeloProntuario.vue).
- Props: `modeloId: number | null`, `modelos: ModeloProntuario[]`.
- Emits: `update:modeloId: [id: number]` — suporta `v-model:modelo-id`.
- Reutilizado em dois contextos: (1) toolbar de `ConsultaAtualTab` quando modelo já selecionado — gatilho exibe nome atual e cabeçalho do popover usa "Trocar modelo"; (2) slot `#acao` de `AppEmptyState` em `ProntuarioView` quando `modeloConsultaAtual === null` — gatilho exibe placeholder em itálico e cabeçalho usa "Selecionar modelo". Em ambos os casos o visual e a lista de itens são idênticos; a diferença é apenas o texto do gatilho/cabeçalho. Adicionado em briefing `planejamentos/2026-06-22_001`.

**Componente `SeletorTemplateCirurgico`:**
- Localização: [`frontend/src/components/prontuario/SeletorTemplateCirurgico.vue`](../frontend/src/components/prontuario/SeletorTemplateCirurgico.vue).
- Props: `aberto: boolean`, `valorAtual: string`.
- Emits: `update:aberto`, `aplicar: [corpo: string]`.
- Lazy fetch: `watch(() => props.aberto, ...)` — só carrega modelos ao abrir, sem request no mount da view.
- Se `valorAtual.trim()` não vazio → abre `AppConfirmDialog` ("Substituir conteúdo atual?"). Se vazio → aplica diretamente.
- Atalho "Cadastrar novo modelo" faz `router.push({ path: "/estabelecimento", query: { secao: "modelos-cirurgia" } })`.

**Evento `"aplicar-template"`:**
- `ConsultaAtualTab` emite `"aplicar-template": [chave: string, corpo: string]`.
- `ProntuarioView` trata com `@aplicar-template="(chave, corpo) => { novaEvolucao[chave] = corpo }"`.
- Evita prop mutation — mantém o fluxo unidirecional.

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

## Mapa corporal interativo (BodyMap — briefings B2 2026-06-08_006 + redesign 2026-06-09_007)

O componente `BodyMap.vue` (em `frontend/src/components/exame-fisico/`) renderiza o atlas corporal SVG com hotspots clicáveis e **coloração por vista anatômica**.

### Tokens de cor de vista anatômica (briefing 2026-06-09_007)

Definidos em `frontend/src/assets/main.css` `:root` (e `.dark`). Semântica: identificam a vista do hotspot aceso no mapa corporal.

| Token | HSL (light) | Cor | Semântica |
|---|---|---|---|
| `--vista-anterior`  | `217 91% 60%` | azul   | Vista frontal (anterior) |
| `--vista-posterior` | `262 70% 56%` | violeta | Vista dorsal (posterior) |
| `--vista-circ`      | `35 92% 50%`  | âmbar  | Vista circunferencial |

Uso: `hsl(var(--vista-*) / <alpha>)` no `fill`/`stroke` dos hotspots SVG, nos pontos da legenda e nas badges do `RegionExamCard`. **Nunca use valores HSL literais espalhados — sempre via token.**

### Coloração de hotspot por vista (R1–R4)

A cor do hotspot aceso é determinada pela `vista` da(s) região(ões) examinada(s) que o acenderam:

- `anterior` → azul (`--vista-anterior`) → classe CSS `region-selected-ant`
- `posterior` → violeta (`--vista-posterior`) → classe CSS `region-selected-post`
- `circunferencial` → âmbar (`--vista-circ`) → classe CSS `region-selected-circ`

**Precedência R4** (quando um hotspot é alvo de mais de uma vista): `circunferencial > posterior > anterior`. Implementada em `SecaoExameFisico.vistasPorIdMapa` via `PRIORIDADE_VISTA` e propagada ao `BodyMap` via prop `vistasPorId?: Record<string, VistaHotspot>`.

A prop `vistasPorId` é **opcional** — quando ausente, hotspots acesos usam `region-selected-ant` como fallback (não-breaking para usos legados e testes existentes que não passam a prop).

### Layout lateral da seção Exame físico (briefing 2026-06-09_007)

Em `SecaoExameFisico.vue`, o mapa corporal e a coluna de regiões examinadas são renderizados **lado a lado** em uma grade de 2 colunas:

```
┌─ Mapa corporal ────────────────────────────────────────────────────┐
│ ┌───────────────────┐   ┌───────────────────────────────────────┐  │
│ │  SVG (reduzido)   │   │ Regiões examinadas         (N)        │  │
│ │  + legenda        │   │ [cards] ou [estado vazio]             │  │
│ └───────────────────┘   └───────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────┘
  (observações gerais: largura total, abaixo da grade)
```

- Grade CSS: `grid-template-columns: minmax(0, 1.55fr) minmax(330px, 1fr); gap: 26px`
- Classe: `.mapa-grade` (CSS scoped de `SecaoExameFisico.vue`)
- Breakpoint responsivo: `@media (max-width: 900px)` → colapsa para 1 coluna (mapa acima, cards abaixo). Unificado com o breakpoint `.grade-sv`/`.grade-antro` existente (900px).
- SVG: `max-width: 480px` no `.mapa-wrap` do `BodyMap.vue` (reduzido de `max-w-2xl`). `viewBox` e paths **inalterados**.
- Estado vazio da coluna direita: `.regioes-vazio` com borda tracejada, ícone, título "Nenhuma região examinada" e texto auxiliar.

### Legenda de cores (CA8)

Legenda estática abaixo do SVG no `BodyMap.vue` (`.mapa-legenda`). Sempre visível quando o mapa está renderizado. Três itens: Anterior (azul), Posterior (violeta), Circunferencial (âmbar). Cada ponto usa as classes `.legenda-dot--ant`, `.legenda-dot--post`, `.legenda-dot--circ` com os tokens `--vista-*`.

### Hotspot de tronco fundido

O tronco é renderizado como **1 polígono clicável por vista** (`Tronco (anterior)` no lado esquerdo / `Tronco (posterior)` no lado direito), sem clip-paths. As antigas faixas separadas por faixa-Y (Tórax / Abdome / Pelve) deixaram de ser hotspots clicáveis.

Esses 2 pseudo-hotspots são **nós sintéticos de UI** — não existem no catálogo `regioes_anatomicas_catalogo`. Seus paths SVG (`M_TORSO_ANT`, `M_TORSO_POST`, `F_TORSO_ANT`, `F_TORSO_POST`) estão em `bodyMapPaths.ts` sob as chaves `'Tronco (anterior)'` e `'Tronco (posterior)'`, em `maleRegionPaths` e `femaleRegionPaths`.

O clique no pseudo-hotspot emite o evento `troncoClicado: TroncoClique` (`'tronco-anterior'` ou `'tronco-posterior'`), tratado por `SecaoExameFisico.onTroncoClicado`. O modal que se abre exibe as sub-regiões das partes do tronco daquela vista, agrupadas por parte (DP-3 — ver regra R4 do briefing).

### Highlight ("acender") por vista — lógica de expansão

- **Vista anterior** → acende o polígono `Tronco (anterior)`.
- **Vista posterior** → acende o polígono `Tronco (posterior)`.
- **Vista circunferencial** → acende **ambos** (`Tronco (anterior)` e `Tronco (posterior)`).

Para o tronco fundido, a decisão de acender é feita por **"OU das partes"** via `PARTE_PARA_TRONCO` (em `regioesCircunferenciais.ts`): o polígono `Tronco (anterior)` acende quando qualquer id de parte anterior (`torax-anterior`, `abdome-anterior`, `pelve-anterior`) estiver em `regioesExaminadas`; analogamente para o posterior.

A expansão circunferencial usa `RAMOS_CIRCUNFERENCIAL` (módulo compartilhado `regioesCircunferenciais.ts`), que é a **fonte única de verdade** do mapeamento `{base}-circunferencial → { anterior, posterior }`, incluindo a exceção clínica `abdome-circunferencial → { anterior: 'abdome-anterior', posterior: 'lombossacra-posterior' }`.

A expansão é **aditiva** sobre o `Set` que já contém o espelhamento bilateral de membro existente — bilateral × circunferencial coexistem (um membro circunferencial bilateral acende 4 polígonos: anterior + posterior de cada lado).

### Módulo compartilhado

`frontend/src/components/exame-fisico/regioesCircunferenciais.ts` exporta:
- `RAMOS_CIRCUNFERENCIAL` — consumido por `RegionSelectorPopup` (resolver filhos no modo circunferencial) e por `SecaoExameFisico` (expansão de highlight no mapa).
- `PARTE_PARA_TRONCO` — consumido por `BodyMap` (lógica "OU das partes" do tronco fundido).

**Referência técnica:** `Docs/Discoverys/exame-fisico-fusao-poligonos/01_discovery.md`.

## Renderização legível de seções estruturadas de evolução (briefing 2026-06-09_008)

A renderização legível de seções estruturadas de evolução (drawer "Ver" e PDF) é centralizada em `formatarSecaoLegivel(chave: string, valor: unknown): string` em `frontend/src/composables/useEvolucaoResumo.ts`.

**Regras:**
- Nunca renderizar JSON cru (`chave: valor`, `true`/`false`) ao usuário.
- Campos vazios e flags de controle são omitidos.
- Negativas clínicas explícitas ("Nega"/"Não") são registradas **somente** para: alergias, medicações (HPP), tabagismo, etilismo, drogas (História social). Todas as demais flags negativas são omitidas.
- String vazia (`""`) após `formatarSecaoLegivel` = seção sem conteúdo → omitir tanto na modal quanto no PDF.

**Paridade modal ↔ PDF (R9):** ambos os canais (`EvolucaoDetalheDrawer.vue` e `useProntuarioPdf.ts`) devem chamar `formatarSecaoLegivel(s.chave, conteudo[s.chave])` — nunca heurística local independente. A decisão de exibir/omitir a seção deriva do resultado da função.

**Seções curadas:** `hpp`, `h-familiar`, `h-social`, `exame-fisico`, `exames-realizados`, `procedimentos-indicados`, `evolucao-pos-op`, `desc-cirurgica`. Chaves fora dessas 8 caem no fallback genérico (humaniza camelCase, sem chave técnica crua).

## Seções estruturadas de prontuário — pós-op e descrição cirúrgica (briefing 2026-06-21_001)

As seções `evolucao-pos-op` e `desc-cirurgica` possuem formulários estruturados dedicados registrados em `SECOES_ESTRUTURADAS` de `ProntuarioView.vue`. O backend armazena o conteúdo como JSONB opaco — não valida o shape.

### `SecaoEvolucaoPosOperatoria.vue`
- Localização: `frontend/src/components/prontuario/secoes/SecaoEvolucaoPosOperatoria.vue`
- Props: `modelValue: EvolucaoPosOp`, `readOnly?: boolean`
- Interface `EvolucaoPosOp`: `evolucaoPaciente`, `evolucaoComentario`, `seguindoOrientacoes`, `orientacoesComentario`, `dataCirurgia`, `dpo`, `destino`, `dieta`, `observacao`.
- DPO calculado automaticamente ao alterar `dataCirurgia` (retorna `""` se futuro/inválido, `"0"` para hoje).

### `SecaoDescricaoCirurgica.vue`
- Localização: `frontend/src/components/prontuario/secoes/SecaoDescricaoCirurgica.vue`
- Props: `modelValue: DescCirurgica`, `readOnly?: boolean`, `erroCirurgiao?: string | null`
- Dia da semana calculado automaticamente (`new Date(data + "T12:00:00").getDay()`).
- Duração calculada de `cirurgiaInicio`/`cirurgiaFim` com tratamento de virada de dia (`total += 24 * 60` se negativo).
- Campos de anestesia (`tipoAnestesia`, `anestesiaInicio`, `anestesiaFim`) são **deliberadamente omitidos** (R7 do briefing).
- `erroCirurgiao` exibe mensagem inline e borda vermelha no campo cirurgião via `.campo-com-erro :deep(.form-input)`.

**Validação de cirurgião (CA20–CA22):** lógica em `validarCirurgiao()` em `ProntuarioView.vue`. Bloqueia `salvarEvolucao()` se a seção `desc-cirurgica` tem ≥1 campo não-vazio E `cirurgiao` está em branco. Propaga por prop chain: `ProntuarioView → ConsultaAtualTab (prop erroCirurgiao) → SecaoProntuario → SecaoDescricaoCirurgica`. O watch limpa o erro ao preencher o cirurgião.

## Export CSV de relatórios (briefing 2026-06-10_004)

Padrão de exportação CSV gerado **100% no frontend** a partir dos dados já carregados na tela (sem endpoint novo no backend).

- **Formato**: UTF-8 com BOM (`﻿`) + separador `;` + datas `dd/MM/yyyy` + decimais com vírgula. Sem símbolo `R$` nas células monetárias — o cabeçalho da coluna indica a moeda.
- **Escaping**: campos com `;`, aspas ou quebras de linha são envolvidos em aspas duplas; aspas internas são duplicadas (`"" → """`). Implementado em `frontend/src/utils/csv.ts` (funções `escaparCelula`, `construirCsv`, `baixarCsv`, `formatarDecimal`, `formatarData`, `nomeArquivoCsv`).
- **Composable**: `frontend/src/composables/useRelatorioCsv.ts` — exportadores por aba (`exportarFinanceiro`, `exportarAgenda`, `exportarPessoas`, `exportarOrcamentos`, `exportarVisaoGeral`). Cada um recebe os dados já carregados e o período.
- **LGPD — minimização**: o CSV espelha apenas as colunas visíveis na tela. Aba Pessoas: apenas nome + métricas agregadas, **sem CPF, telefone, e-mail ou qualquer PII além do nome já exibido**.
- **Botão único no header**: `AppButton variant="secondary" icon="fa-solid fa-file-csv"` no `AppPageHeader #acoes`. Desabilitado em loading e quando a aba não tem dados tabulares. Toast genérico em erro.
- **Nome de arquivo**: `relatorio-{aba}-{yyyy-MM-dd}-a-{yyyy-MM-dd}.csv`.

## Páginas públicas de confiança — changelog e status (briefing 2026-06-10_008)

Views standalone **sem `AppLayout`**, no mesmo padrão de `views/legal/` (`PrivacidadeView`, `TermosView`): cabeçalho com "← Voltar" para a Landing + logo, artigo centralizado (max-width 780px, padding lateral), footer com links de navegação.

- **Rotas**: `/novidades` → `NovidadesView.vue` · `/status` → `StatusView.vue` — ambas em `src/views/publico/`. Sem `meta.requiresAuth`, sem `...APP`.
- **Conteúdo estático versionado**: `src/content/changelog.ts` e `src/content/status.ts` — importados diretamente pelas views, sem fetch. Atualizar = commit + deploy.
- **Type-safety**: `TagChangelog = "novidade" | "melhoria" | "correção"` e `EstadoSistema = "operacional" | "instabilidade" | "manutenção"` — union types fechados; tag/estado fora do conjunto viram erro de compilação.
- **DS reutilizado**: `AppBadge` (tags de changelog), `AppStatusPill` (estado do sistema), `AppEmptyState` (changelog vazio).
- **Links de entrada**: rodapé da `LandingView` (colunas "Produto" e "Suporte") e footer do `AppSidebar` (junto a Configurações/Ajuda).
- **Sem uptime medido**: status é declarado manualmente até F0-E1 (infra de monitoramento externo).

## Estados de agendamento — label, cor e token (briefing 2026-06-19_001)

Mapa canônico de `AgendamentoStatus` → label PT-BR + cor. Implementado em `STATUS_META` em `AgendamentoRow.vue` e `EditarAgendamentoModal.vue`, e nas classes globais `.status-pill.*` em `main.css`.

| Status     | Label PT-BR | Token / cor                                | Semântica                                   |
|------------|-------------|---------------------------------------------|---------------------------------------------|
| `Agendado` | Agendado    | `hsl(45 96% 47%)` (âmbar)                  | Aguardando confirmação                      |
| `Confirmado` | Confirmado | `hsl(160 79% 39%)` (verde)                 | Paciente confirmou presença                 |
| `Concluido` | Concluído  | `hsl(var(--foreground) / 0.6)` (neutro)    | Atendimento realizado e finalizado          |
| `Cancelado` | Cancelado  | `hsl(0 84% 60%)` (vermelho)                | Cancelamento deliberado (com motivo)        |
| `Expirado` | Expirado    | `hsl(var(--status-expirado-cor, 220 9% 60%))` (acinzentado) | Não finalizado até o fim do dia (job D-1) |

**Tokens do estado Expirado** (definidos em `main.css :root`):
- `--status-expirado-cor: 220 9% 60%` — cor da stripe/borda
- `--status-expirado-bg: 220 9% 60% / 0.12` — fundo da pílula
- `--status-expirado-fg: 220 9% 42%` — texto da pílula

**Regra de negócio**: `Expirado` é estado terminal aplicado automaticamente pelo job noturno (`expirar-agendamentos-nao-finalizados`, 03:00 BRT). Não é cancelamento deliberado — **nunca** somar `Expirado` com `Cancelado` em relatórios; cada um é sua própria categoria. Sem fluxo de desfazer nesta entrega.

## Documentos relacionados

- [`DESIGN_SYSTEM.md`](DESIGN_SYSTEM.md) — referência completa de componentes do design system, tokens, e variantes.
- [`03A_FASE_3_UX_ADR.md`](03A_FASE_3_UX_ADR.md) — decisões de UX históricas (ADRs).
- [`Discoverys/tipografia/01_discovery.md`](Discoverys/tipografia/01_discovery.md) — auditoria de fragmentação e escala canônica adotada.
