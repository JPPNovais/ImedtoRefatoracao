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

## Documentos relacionados

- [`DESIGN_SYSTEM.md`](DESIGN_SYSTEM.md) — referência completa de componentes do design system, tokens, e variantes.
- [`03A_FASE_3_UX_ADR.md`](03A_FASE_3_UX_ADR.md) — decisões de UX históricas (ADRs).
