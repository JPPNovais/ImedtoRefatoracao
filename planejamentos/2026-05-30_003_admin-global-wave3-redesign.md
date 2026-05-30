# Admin Global — Wave 3: redesign profundo (fechamento de débito técnico DS)

**ID**: 2026-05-30_003
**Status**: Aprovado por usuário em 2026-05-30
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M (refator visual em ~15 arquivos, sem mudança de regra/contrato/schema)
**Áreas regressivas tocadas**: módulo admin global (frontend); zero impacto no app principal, em DB, em endpoints

**Referências**:
- `planejamentos/2026-05-30_001_admin-global-mvp.md` (Wave 1 — commit `3d602e6`)
- `planejamentos/2026-05-30_002_admin-global-wave2.md` (Wave 2 — commit `ad3238a`)

---

## 1. Contexto e motivação

Wave 2 entregou o redesign do módulo admin **apenas no nível de tokens** (`hsl(var(--primary))` etc), mas **manteve marcação própria** em `AdminLayout.vue` (sidebar, topbar, banner) e classes CSS locais (`admin-*`, `secao-*`, `page-*`) na maioria das views. QA registrou explicitamente o gap como débito técnico Wave 3 — CAs `W2-CA29` (AdminLayout reusa `AppSidebar`/`AppTopBar`) e `W2-CA32` (views usam `AppCard`/`AppButton`) **falharam** na entrega Wave 2.

Feedback literal do usuário após Wave 2:

> "o layout ainda esta zuado, pois nao tem espaçamentos corretos no conteudo. preciso que siga o padrao do restante da aplicação no uso dos componentes, precisa usar os mesmos coponentes do design system, entao verifique essa parte e ajuste para ficar com a mesma cara da plataforma"

O sintoma é exatamente o sintoma esperado de "tokens só, não componentes": cores certas, mas espaçamentos, tipografia, bordas, sombras e estados (hover/focus/active) divergem do app principal — porque cada classe CSS local reinventou o que `AppCard`, `AppButton`, `AppInput`, etc. já entregam pronto.

Inventário do estado atual (resultado de grep direto):

| View / Layout | Classes `admin-*`/`secao-*`/`page-*` no template | Componentes DS já usados |
|---|---|---|
| `AdminLayout.vue` | 8 (sidebar, banner, header, etc) | 0 |
| `AdminLogin.vue` | 7 | 0 |
| `AdminChangePassword.vue` | 6 | 0 |
| `AdminDashboard.vue` | 4 | 0 |
| `AdminsListView.vue` | 0 (Wave 2 — referência) | **20 — gold standard** |
| `AdminsFormView.vue` | 0 (Wave 2 — referência) | **11 — gold standard** |
| `EstabelecimentosListView.vue` | 3 | 0 |
| `EstabelecimentoDetalheView.vue` | 1 | 0 |
| `PlanosListView.vue` | 6 | 0 |
| `PlanosFormView.vue` | 7 | 0 |
| `ConfigsView.vue` | 5 | 0 |
| `ModelosGlobaisListView.vue` | 3 | 0 |
| `ModelosGlobaisFormView.vue` | 1 | 0 |
| `RegioesGlobaisListView.vue` | 3 | 0 |
| `RegioesGlobaisFormView.vue` | 1 | 0 |
| `VariaveisGlobaisListView.vue` | 3 | 0 |
| `VariaveisGlobaisFormView.vue` | 1 | 0 |

Além disso, 53 ocorrências de classes `admin-*`/`secao-*`/`page-*` aparecem em `frontend/src/modules/admin/components/` (modais e cards reaproveitados pelas views).

---

## 2. Persona-alvo

Administradores Imedto (equipe interna) usando o módulo admin global no dia a dia para suporte, configuração de planos, catálogos globais e operações em estabelecimentos. Para o usuário (dono do produto), o objetivo é que o admin **se sinta parte do mesmo produto que o app principal** — não uma tela de manutenção visualmente esquecida.

---

## 3. Objetivo (uma frase)

Eliminar o débito técnico de redesign aberto na Wave 2 fazendo todo o módulo admin (layout + 15 views + 7 componentes internos) consumir **os componentes reais do design system** — mesmos do app principal — preservando o isolamento físico do módulo e as duas marcações de "área admin" (badge e faixa).

---

## 4. Escopo

### Inclui (IN)

- **Refatorar `AdminLayout.vue`** para reusar `AppTopBar` + `AppSidebar` do design system (não `AppLayout.vue` — ver §6 Decisão 1) compondo um shell admin próprio, fino, sem CSS de sidebar/header próprio.
- **Refatorar `AdminLogin.vue` e `AdminChangePassword.vue`** para usar `AppCard`, `AppButton`, `AppField`, `AppInput`, sem precisar do shell (são telas de auth full-screen como `LoginView` do app).
- **Refatorar `AdminDashboard.vue`** para usar `AppPageHeader` + `AppCard` (ou `AppStatCard` se cabível para os placeholders).
- **Refatorar as 12 views de listagem/formulário/configuração**:
  - `EstabelecimentosListView.vue`, `EstabelecimentoDetalheView.vue`
  - `PlanosListView.vue`, `PlanosFormView.vue`
  - `ConfigsView.vue`
  - `ModelosGlobaisListView.vue`, `ModelosGlobaisFormView.vue`
  - `RegioesGlobaisListView.vue`, `RegioesGlobaisFormView.vue`
  - `VariaveisGlobaisListView.vue`, `VariaveisGlobaisFormView.vue`
  - Todas usam: `.app-page` (container global), `AppPageHeader` (título+subtítulo+ações), `AppCard` (agrupamento), `AppButton` (todas as ações), `AppInput`/`AppSelect`/`AppTextarea`/`AppCheckbox`/`AppField` (forms), `AppEmptyState` (vazio), `AppPagination` (paginação), `AppSearchInput` (busca), `AppBadge`/`AppStatusPill` (badges), `AppToast` (notificações), `AppConfirmDialog` (confirmações), `AppModal` (modais inline).
- **Refatorar os 7 componentes em `modules/admin/components/`** (modais e cards de assinatura/estabelecimento) para usar `AppModal`, `AppCard`, `AppButton`, `AppField`, etc. Eliminar suas 53 classes custom.
- **Manter badge "Admin"** usando `AppBadge` do DS (variante `destructive`) — sem classe custom.
- **Manter faixa de acento de 2px** com `hsl(var(--warning))` no topo do `AppTopBar` admin — única CSS custom permitida (1 wrapper class no shell do admin).
- **Manter banner "Área administrativa"** acima do topbar (faixa larga warning/destructive em dev/prod) — implementação mínima, idealmente reaproveitando `AppBadge` ou bloco simples com tokens.
- **Manter isolamento físico do módulo**: imports permitidos apenas de `@/components/ui/*`, `@/composables/*` utilitários (`useDebouncedRef`), e `@/assets/main.css` (para `.app-page`). Proibido importar de `@/stores`, `@/services`, `@/views`, `@/router` (que não seja o do próprio módulo), `@/layouts/AppLayout.vue`.
- **Sem regressão funcional** em nenhuma das telas existentes.

### Não inclui (OUT)

- Nenhuma feature nova de produto.
- Nenhuma mudança em endpoint, contrato de DTO, regra de negócio, schema, migration.
- Nenhuma mudança em outros módulos do app (estabelecimentos, planos do dono, etc).
- Nenhuma reestruturação de roteamento ou store.
- Nenhuma mudança no comportamento do timer de inatividade, fluxo de login/logout, troca de senha, reset de tenant.
- Nenhuma nova animação, transição ou efeito.
- Nenhuma extração de componente admin para o design system global (admin permanece isolado).

---

## 5. Regras de negócio

Nenhuma regra de negócio nova nesta Wave — é puro redesign visual. As regras de Wave 1 e Wave 2 continuam valendo integralmente:
- **R-Wave1**: autenticação JWT separada, timer 30 min inatividade, RBAC por permissão admin.
- **R-Wave2**: tokens HSL no lugar de hex, faixa warning 2px, badge "Admin" destructive.

Wave 3 **acrescenta uma regra de design system não-negociável** (consequência direta do feedback do usuário):
- **R-Wave3**: o módulo admin consome o design system de componentes do app principal. Toda primitiva visual (página, header, card, botão, input, select, textarea, checkbox, badge, modal, toast, confirm, paginação, busca, empty state) é via componente `App*` exportado de `@/components/ui`. Mora em: Frontend. Validada em: grep no QA (§ CA W3-CA11).

---

## 6. Decisões cravadas (sem perguntas ao usuário — escopo fechado)

### Decisão 1 — Não reusar `AppLayout.vue`, compor shell próprio com `AppTopBar` + `AppSidebar`

`AppLayout.vue` importa 5 stores do app principal (`authStore`, `tenantStore`, `permissoesStore`, `profissionalStore`, `notificacoesStore`) e o router principal — incompatível com o isolamento físico do módulo admin (briefing 2026-05-30_001 §10). Em vez disso, o `AdminLayout.vue` compõe diretamente `<AppTopBar>` e `<AppSidebar>` (componentes puros de UI, zero acoplamento de store), driven pelo `useAdminAuthStore` do próprio módulo. Resultado: mesma cara do app, isolamento preservado, sem dependência ao layout principal.

### Decisão 2 — `AdminLogin` e `AdminChangePassword` ficam fora do shell (telas de auth full-screen)

Espelha o padrão do app principal (login → `app-page app-page--narrow` ou view própria sem sidebar). Login usa `AppCard` centralizado com branding embutido. ChangePassword segue o mesmo molde após autenticar mas antes de ter sessão completa.

### Decisão 3 — Faixa de acento warning 2px e banner "Área administrativa" são as únicas exceções de CSS custom permitidas

A faixa de 2px sobe como `border-top: 2px solid hsl(var(--warning))` aplicada via wrapper do `AppTopBar` no shell admin (ou via `:deep()` se mais limpo). O banner superior (faixa larga "Área administrativa — uso interno Imedto") permanece como `<div>` simples com `hsl(var(--destructive))`/`hsl(var(--warning))` para identificar prod/dev. Nenhuma outra classe CSS custom é tolerada em CSS scoped do módulo (validado por grep no QA).

### Decisão 4 — Componentes do módulo (`modules/admin/components/`) também migram

As 53 classes custom nos 7 modais e cards de assinatura/estabelecimento são parte do mesmo débito visual. Migra junto — não vale fazer Wave 3.1 depois. Sem essas, o usuário continua vendo "espaçamentos zuados" quando abre, por exemplo, `TrocarPlanoModal` ou `AssinaturaCard`.

### Decisão 5 — Gold standard são `AdminsListView` e `AdminsFormView` (Wave 2)

Já adotam DS completo (20 e 11 ocorrências de `App*` respectivamente, zero classes custom). Toda view nova segue **o padrão delas**: imports nomeados de `@/components/ui`, `<main class="app-page">`, `<AppPageHeader>` com slot `acoes`, `<AppCard>` agrupando, `<AppButton>` em ações.

### Decisão 6 — Usar `.app-page` global (importado de `assets/main.css`) — não duplicar container

`.app-page`, `.app-page--narrow`, `.app-page--wide`, `.app-page--full` já são utilitários globais. Admin usa o mesmo padrão. Forms usam `app-page--narrow`, listas usam `app-page`, dashboard pode usar `app-page--wide` se cabível.

### Decisão 7 — Quando o DS não tem componente exato, fallback é HTML mínimo com tokens HSL

Casos previsíveis:
- **Tabelas**: o DS não exporta um `AppTable`. As views Wave 2 (`AdminsListView`) usam `<table>` HTML padrão com classes utility-style escopadas. Wave 3 segue o mesmo padrão (HTML semântico + tokens HSL inline minimal). Isso **não** é classe custom proibida — é estrutura semântica de tabela. O grep do QA (W3-CA11) procura por prefixos `admin-*`/`secao-*`/`page-*`, não por toda classe scoped.
- **KPI cards do Dashboard**: usar `AppStatCard` se existir variante adequada; senão, `AppCard` com slot custom.

### Decisão 8 — `AppToast` e `AppConfirmDialog` substituem alerts/confirms nativos

Wave 2 já migrou em `AdminsList`/`AdminsForm`. Wave 3 espelha em todas as outras views que hoje usam mensagem inline ou `confirm()` nativo.

---

## 7. Modelo de dados

**Nenhuma mudança.** Wave 3 não toca em schema, migration, DTO de back, nem contrato de API.

---

## 8. UX e fluxo

### Shell (`AdminLayout.vue` refatorado)

```
┌────────────────────────────────────────────────────────────────┐
│ [BANNER] Área administrativa — uso interno Imedto              │  ← div simples, warning/destructive
├────────────────────────────────────────────────────────────────┤
│ [2px faixa warning]                                            │  ← border-top do AppTopBar
│ <AppTopBar>                                                    │
│   brand: logo Imedto + <AppBadge variant=destructive>Admin     │
│   default slot: identidade (nome+email) + botão Sair           │
│ </AppTopBar>                                                   │
├────────┬───────────────────────────────────────────────────────┤
│        │                                                       │
│ <App   │  <router-view />  (cada view monta sua <main class=   │
│ Sidebar│   "app-page">...</main>)                              │
│ >      │                                                       │
│  items:│                                                       │
│  - Dashboard                                                   │
│  - Estabelecimentos                                            │
│  - Planos                                                      │
│  - Administradores                                             │
│  - Configurações                                               │
│  ── Catálogos ──                                               │
│  - Modelos                                                     │
│  - Variáveis                                                   │
│  - Regiões                                                     │
└────────┴───────────────────────────────────────────────────────┘
```

### Padrão de view (listagem — molde `AdminsListView` Wave 2)

```html
<main class="app-page">
    <AppPageHeader titulo="Estabelecimentos" subtitulo="234 encontrado(s)">
        <template #acoes>
            <AppButton icon="fa-solid fa-plus">Novo</AppButton>
        </template>
    </AppPageHeader>

    <AppCard padding="md">
        <div class="filtros-row">
            <AppSearchInput v-model="busca" placeholder="Buscar..." />
            <AppSelect v-model="filtroStatus" ... />
        </div>

        <table class="lista">...</table>
        <AppEmptyState v-if="vazio" titulo="Nenhum encontrado" />
        <AppPagination v-model:pagina="pagina" :total="total" />
    </AppCard>

    <AppToast v-if="toast" ... />
    <AppConfirmDialog v-if="confirmando" ... />
</main>
```

### Padrão de view (formulário — molde `AdminsFormView` Wave 2)

```html
<main class="app-page app-page--narrow">
    <AppPageHeader titulo="Novo plano" subtitulo="..." />

    <AppCard padding="md">
        <form @submit.prevent="salvar">
            <AppField label="Nome do plano" required>
                <AppInput v-model="form.nome" />
            </AppField>
            <AppField label="Descrição">
                <AppTextarea v-model="form.descricao" />
            </AppField>
            ...
            <div class="acoes">
                <AppButton variant="ghost" @click="voltar">Cancelar</AppButton>
                <AppButton type="submit" :loading="salvando">Salvar</AppButton>
            </div>
        </form>
    </AppCard>
</main>
```

### Estados sempre presentes

- **Loading**: `AppButton :loading` ou skeleton dentro do `AppCard`.
- **Erro**: `AppToast variante="error"` (não inline `<p class="erro">`).
- **Vazio**: `AppEmptyState` com ícone, título e ação.
- **Sucesso**: `AppToast variante="success"`.

### Mobile-ready

Todos os componentes do DS já são responsivos. Admin herda automaticamente.

---

## 9. Critérios de aceite (testáveis)

### Shell e layout

- **W3-CA1** (sidebar via DS): Dado o módulo admin renderizado, Quando inspecionar o DOM, Então existe exatamente um `<aside class="side">` proveniente do componente `AppSidebar` (de `@/components/ui/AppSidebar.vue`) — e zero elementos com `class="admin-sidebar"`.

- **W3-CA2** (topbar via DS): Dado o módulo admin renderizado, Quando inspecionar o DOM, Então existe exatamente um `<header class="topbar">` proveniente do componente `AppTopBar` — e zero elementos com `class="admin-header"`.

- **W3-CA3** (não importa `AppLayout`): Dado `frontend/src/modules/admin/`, Quando rodar `grep -r "AppLayout" .` no módulo, Então retorna 0 ocorrências (admin compõe `AppSidebar` + `AppTopBar` diretamente, sem acoplar ao layout do app principal).

- **W3-CA4** (badge "Admin" via DS): Dado o topbar admin renderizado, Quando inspecionar o brand, Então o badge "Admin" é uma instância de `AppBadge` (variante destructive) — e não um `<span class="admin-sidebar-badge">` custom.

- **W3-CA5** (faixa de acento 2px preservada): Dado o topbar admin renderizado, Quando inspecionar o estilo computado do topbar, Então `border-top` é `2px solid` com cor derivada de `hsl(var(--warning))`.

- **W3-CA6** (banner área administrativa preservado): Dado o módulo admin renderizado, Quando carregar a página, Então o banner topo está presente com texto correto ("Área administrativa — uso interno Imedto" em prod, com sufixo "(DEV)" fora de prod) e cor warning/destructive conforme ambiente.

### Views — padrão de uso de DS

- **W3-CA7** (page header em todas as views): Dado qualquer view do módulo admin (exceto `AdminLogin` e `AdminChangePassword`), Quando carregar a view, Então existe exatamente um `<AppPageHeader>` no topo com `titulo` e (opcional) `subtitulo` e slot `acoes`.

- **W3-CA8** (`.app-page` em todas as views): Dado qualquer view do módulo admin (exceto `AdminLogin` e `AdminChangePassword`), Quando inspecionar o template, Então o container raiz é `<main class="app-page">` (ou variante `app-page--narrow` para forms, `app-page--wide` para dashboard). Zero ocorrências de `class="admin-page"` ou `class="admin-*-page"`.

- **W3-CA9** (cards via DS): Dado qualquer agrupamento de conteúdo (seção de lista, bloco de form, etc), Quando inspecionar, Então é renderizado via `<AppCard>` — e não `<div class="secao-card">` ou `<div class="admin-card">`.

- **W3-CA10** (botões via DS): Dado qualquer botão de ação nas views admin (exceto botão "Sair" do topbar que mora dentro do `AppTopBar` slot), Quando inspecionar, Então é uma instância de `<AppButton>` com variante apropriada (`primary` ação principal, `secondary` ação secundária, `danger` destrutiva, `ghost` cancelar). Zero `<button class="admin-btn-*">`.

- **W3-CA11** (forms via DS): Dado qualquer formulário admin, Quando inspecionar, Então cada campo é `<AppField label="...">` envolvendo `<AppInput>`/`<AppSelect>`/`<AppTextarea>`/`<AppCheckbox>`. Zero `<input class="admin-input">`, zero `<select class="admin-select">`, zero `<label class="admin-label">`.

- **W3-CA12** (modais via DS): Dado qualquer modal nas views ou componentes admin (TrocarPlano, ConcederGratuidade, EncerrarAssinatura, ModalRevelarCpf, ModalResetTenant, etc), Quando aberto, Então é renderizado via `<AppModal>` — e não via `<div class="admin-modal">` ou overlay próprio.

- **W3-CA13** (estados vazio via DS): Dado uma lista vazia, Quando renderizar, Então é mostrado `<AppEmptyState>` com título e (quando aplicável) slot de ação. Zero `<p class="vazio">` ou `<div class="lista-vazia">` custom.

- **W3-CA14** (paginação via DS): Dado lista paginada, Quando renderizar, Então paginação é `<AppPagination>` — não controles custom.

- **W3-CA15** (busca via DS): Dado campo de busca, Quando renderizar, Então é `<AppSearchInput>` — não `<input class="admin-input">`.

- **W3-CA16** (feedback de ação via DS): Dado uma ação que dispara sucesso/erro/info, Quando completa, Então notifica via `<AppToast>` — não via mensagem inline `<p class="msg">` ou `alert()` nativo.

- **W3-CA17** (confirmação destrutiva via DS): Dado uma ação destrutiva (excluir, resetar, encerrar assinatura), Quando o usuário clicar, Então abre `<AppConfirmDialog>` — não `confirm()` nativo nem modal custom.

### Limpeza de CSS

- **W3-CA18** (zero classes admin-* em CSS scoped): Dado o módulo admin, Quando rodar `grep -rE "\.admin-[a-z]+" frontend/src/modules/admin/` filtrado para arquivos `.vue` (seção `<style scoped>`), Então retorna **0 ocorrências** (excluindo o seletor da faixa warning de 2px e o banner — únicas exceções declaradas em §6 Decisão 3, e que devem ser inline no `AdminLayout.vue` apenas).

- **W3-CA19** (zero classes secao-* / page-*): Dado o módulo admin, Quando rodar `grep -rE "(secao-|page-(titulo|subtitulo|header))" frontend/src/modules/admin/` em `.vue`, Então retorna 0 ocorrências.

- **W3-CA20** (CSS scoped enxuto): Dado qualquer view do módulo admin (exceto `AdminLayout.vue`), Quando contar linhas em `<style scoped>`, Então a soma total de todas as views é **< 400 linhas** (referência: hoje ~2.000 linhas distribuídas em estilos custom). `AdminLayout.vue` fica < 60 linhas de CSS scoped (banner + faixa + composição mínima).

### Isolamento físico

- **W3-CA21** (imports permitidos): Dado o módulo admin, Quando rodar `grep -rE "from \"@/(stores|services|views|router|layouts)" frontend/src/modules/admin/` exceto `from "@/components/ui"` e exceto imports do próprio módulo, Então retorna **0 ocorrências**. Permitido: `@/components/ui/*`, `@/composables/useDebouncedRef`, `@/assets/main.css` (implicito via global).

### Sem regressão funcional

- **W3-CA22** (login funciona idêntico): Dado `/admin/login`, Quando preencher credenciais válidas, Então autentica, redireciona para `/admin/dashboard`, e o timer de inatividade inicia.

- **W3-CA23** (logout funciona): Dado sessão admin ativa, Quando clicar "Sair" no topbar, Então faz logout, limpa store, redireciona para `/admin/login`.

- **W3-CA24** (troca de senha funciona): Dado fluxo de troca de senha, Quando submeter senha válida que atende política, Então salva e redireciona conforme antes.

- **W3-CA25** (CRUD admins funciona): Dado a tela de administradores, Quando criar, editar, ativar/desativar, excluir, Então comportamento idêntico ao antes — mesmas mensagens, mesmos erros, mesma confirmação.

- **W3-CA26** (CRUD estabelecimentos funciona — listar, detalhar, reset tenant, revelar CPF): Dado as telas de estabelecimento, Quando executar cada operação, Então comportamento idêntico.

- **W3-CA27** (CRUD planos funciona): Dado a tela de planos, Quando criar, editar, ativar/desativar, Então comportamento idêntico.

- **W3-CA28** (assinaturas — trocar plano, conceder gratuidade, encerrar): Dado as modais de assinatura no detalhe de estabelecimento, Quando executar cada operação, Então comportamento idêntico.

- **W3-CA29** (CRUD configs do sistema funciona): Dado `/admin/configuracoes`, Quando editar e salvar uma configuração, Então persiste e mostra confirmação.

- **W3-CA30** (CRUD catálogos globais funcionam — modelos, variáveis, regiões): Dado as 3 telas de catálogos, Quando criar, editar, excluir item, Então comportamento idêntico.

### Build e testes

- **W3-CA31** (build verde): Dado o repositório, Quando rodar `pnpm --filter frontend build`, Então build conclui sem erro de tipo, sem warning novo, sem CSS órfão.

- **W3-CA32** (testes verdes): Dado a suíte Vitest, Quando rodar `pnpm --filter frontend vitest run`, Então todos os testes existentes (incluindo `AdminLogin.test.ts`, `adminAuthStore.test.ts`, etc) passam sem mudança.

- **W3-CA33** (`dotnet build` verde): Dado o backend, Quando rodar `dotnet build`, Então compila — Wave 3 não toca backend mas é checagem de não-regressão de regressão acidental.

### Documentação

- **W3-CA34** (`Docs/DESIGN.md` atualizado): Dado o doc de design, Quando ler §"Módulo Admin Global", Então afirma corretamente que o admin **usa de fato** `AppSidebar`, `AppTopBar`, `AppPageHeader`, `AppCard`, `AppButton`, `AppField`, `AppInput`, `AppSelect`, `AppTextarea`, `AppCheckbox`, `AppModal`, `AppToast`, `AppConfirmDialog`, `AppEmptyState`, `AppPagination`, `AppSearchInput`, `AppBadge` do design system (não apenas "tokens HSL") — e referencia este briefing (`2026-05-30_003`).

---

## 10. Arquitetura — mudanças por arquivo (alto nível, dev expande)

### Layout (1 arquivo)

| Arquivo | Mudança |
|---|---|
| `frontend/src/modules/admin/views/AdminLayout.vue` | Reescreve template + script: importa `AppTopBar`, `AppSidebar`, `AppBadge` de `@/components/ui`. Monta nav array `ITENS_ADMIN_MENU` (Dashboard, Estabelecimentos, Planos, Administradores, Configurações + seção "Catálogos" com Modelos/Variáveis/Regiões). Topbar usa slot `brand` (logo + `AppBadge`), slot default ou direct content (identidade + Sair via `AppButton` ghost). CSS scoped permanece só para: banner topo + faixa warning 2px (border-top via wrapper class `.admin-topbar-wrap` ou `:deep(.topbar)`). Mantém `handleInteraction` para timer. |

### Telas de auth fora do shell (2 arquivos)

| Arquivo | Mudança |
|---|---|
| `AdminLogin.vue` | Usa `AppCard` centralizado (max-width 420px, centrado vertical) com `AppPageHeader` interno ou título simples, `AppField` + `AppInput` para email/senha, `AppButton` submit, `AppToast` para erro. Mantém branding mínimo (logo + badge "Admin"). Sem shell. |
| `AdminChangePassword.vue` | Mesmo padrão: `AppCard` centralizado, `AppField` + `AppInput` (3 campos: atual, nova, confirmação), checklist de política de senha como bloco simples dentro do card, `AppButton` submit, `AppToast` para feedback. |

### Dashboard (1 arquivo)

| Arquivo | Mudança |
|---|---|
| `AdminDashboard.vue` | `<main class="app-page">`, `AppPageHeader` com saudação, grid de cards usando `AppStatCard` (ou `AppCard` slim) para os placeholders atuais. |

### Listas e formulários (12 arquivos)

| Arquivo | Mudança |
|---|---|
| `EstabelecimentosListView.vue` | `<main class="app-page">`, `AppPageHeader`, `AppCard` agrupando filtros + tabela, `AppSearchInput`, `AppSelect`, `AppPagination`, `AppEmptyState`, `AppButton` em ações, `AppStatusPill`/`AppBadge` para status. |
| `EstabelecimentoDetalheView.vue` | `<main class="app-page">`, `AppPageHeader` com nome do estabelecimento + subtitulo + botões de ação no slot `acoes` (revelar CPF, reset tenant), `AppCard` para cada bloco de info (Dados gerais, Assinatura, Estatísticas), `AppButton` em ações, integra `AssinaturaCard`, `ModalRevelarCpf`, `ModalResetTenant` já refatorados. |
| `PlanosListView.vue` | Mesmo molde da `AdminsListView` (Wave 2): `app-page`, `AppPageHeader`, `AppCard` com filtros (search + select de status) e tabela, `AppButton` (novo/editar/ativar-desativar), `AppEmptyState`, `AppPagination`, `AppToast`, `AppConfirmDialog`. |
| `PlanosFormView.vue` | Mesmo molde da `AdminsFormView` (Wave 2): `app-page app-page--narrow`, `AppPageHeader`, `AppCard`, `AppField` + `AppInput`/`AppTextarea`/`AppCheckbox`/`AppSelect`, `AppButton` (Salvar + Cancelar ghost), `AppToast`. |
| `ConfigsView.vue` | `app-page`, `AppPageHeader`, lista de seções como vários `AppCard` (um por seção); cada item com `AppField` + componente de input adequado (texto/select/textarea/checkbox conforme tipo). `AppButton` (Salvar global ou por seção). `AppToast`. Mantém comportamento de "seções expansíveis" usando `AppCard` com header slot e v-if no conteúdo (ou `AppTabs` se for limpo). |
| `ModelosGlobaisListView.vue` | `app-page`, `AppPageHeader`, `AppCard` com filtros + tabela, `AppButton`, `AppEmptyState`, `AppPagination`. |
| `ModelosGlobaisFormView.vue` | `app-page app-page--narrow`, `AppPageHeader`, `AppCard`, `AppField`s, `AppButton`. |
| `RegioesGlobaisListView.vue` | Idem padrão lista. |
| `RegioesGlobaisFormView.vue` | Idem padrão form. |
| `VariaveisGlobaisListView.vue` | Idem padrão lista. |
| `VariaveisGlobaisFormView.vue` | Idem padrão form. |

### Componentes internos do módulo (7 arquivos)

| Arquivo | Mudança |
|---|---|
| `modules/admin/components/assinaturas/TrocarPlanoModal.vue` | Usa `AppModal` com slot header + body + footer. `AppField` + `AppSelect`/`AppInput`. `AppButton` confirmar/cancelar. |
| `modules/admin/components/assinaturas/ConcederGratuidadeModal.vue` | `AppModal` + `AppField` + `AppInput`/`AppTextarea` + `AppButton`. |
| `modules/admin/components/assinaturas/EncerrarAssinaturaModal.vue` | `AppModal` + `AppField` + `AppTextarea` (motivo) + `AppButton` (variante danger). |
| `modules/admin/components/assinaturas/AssinaturaCard.vue` | `AppCard` com slots header (plano atual + status pill) e default (dados, próxima cobrança). `AppBadge`/`AppStatusPill` para status. `AppButton` para ações. |
| `modules/admin/components/estabelecimentos/BadgeStatusEstabelecimento.vue` | Usa `AppBadge` ou `AppStatusPill` do DS, passando variante conforme status — deixa de ter CSS próprio. |
| `modules/admin/components/estabelecimentos/ModalRevelarCpf.vue` | `AppModal` + `AppField` + `AppInput` (senha de confirmação) + `AppButton`. |
| `modules/admin/components/estabelecimentos/ModalResetTenant.vue` | `AppModal` + `AppField` + `AppInput` (confirmação digitada) + `AppButton` (variante danger). |

### CSS global

| Arquivo | Mudança |
|---|---|
| Nenhum | `assets/main.css` não é tocado — `.app-page` já existe e é o padrão a ser reusado. |

---

## 11. Riscos e dependências

### Riscos

- **R1 — Regressão visual sutil**: depois da refatoração, espaçamentos/cores podem divergir do esperado em casos específicos (ex: `ConfigsView` com seções colapsáveis). **Mitigação**: revisão visual manual em cada uma das 17 telas + ciclo curto de ajuste; QA usa chrome-devtools MCP para tirar screenshots de cada tela.
- **R2 — Regressão de comportamento em modais**: substituir modal custom por `AppModal` pode mudar comportamento de teclado (ESC), foco, scroll lock, backdrop. **Mitigação**: validar cada modal abrindo, fechando via ESC, via X, via backdrop, via botão cancelar. Re-rodar testes Vitest existentes (`AdminLogin.test.ts`, etc.).
- **R3 — `AppToast` não cobrir caso edge admin**: Wave 2 já adota `AppToast` em `AdminsListView` — risco baixo de incompatibilidade. **Mitigação**: replicar exatamente o uso de Wave 2.
- **R4 — `ConfigsView` complexa com seções dinâmicas**: estrutura de "categoria → seção → itens" pode não casar 1:1 com `AppCard` simples. **Mitigação**: usar múltiplos `AppCard` empilhados com slot de header (uma `AppCard` por seção). Se ficar feio, fallback é `AppTabs` para alternar entre seções — dev decide na implementação se necessário.
- **R5 — `AppStatCard` pode não cobrir o caso do Dashboard atual**: o dashboard só tem placeholders genéricos. **Mitigação**: usar `AppCard` simples — `AppStatCard` é só "se couber". Sem stat real, é só apresentação.
- **R6 — Quebrar isolamento físico do módulo acidentalmente**: dev pode importar de `@/stores` em vez de usar o store próprio do admin. **Mitigação**: CA W3-CA21 valida via grep antes de commitar.
- **R7 — Custo de tempo**: 17 arquivos + 7 componentes = ~25 unidades de refatoração. Estimativa ainda é M, mas o dev deve agrupar listas e formulários por padrão (write once, copy thrice) — molde de Wave 2 já existe.

### Dependências

- **Wave 2 está em produção** (commit `ad3238a`) — base estável para refatorar em cima.
- **Design system existente** (`@/components/ui/*`) — Wave 3 consome, não altera. Se algum componente do DS tiver bug, é problema do DS global, não do admin.
- **Sem dependência de DB / migration** — Wave 3 não aciona `imedto-database`.

---

## 12. Observações para execução

- **Não-negociável**: começar pelo `AdminLayout.vue` (shell) — sem ele, as views ficam sem moldura. Em seguida, telas de auth (`AdminLogin`, `AdminChangePassword`). Em seguida, listas e formulários (replicar molde Wave 2). Em seguida, componentes internos (modais e cards de assinatura). Finalmente, dashboard.
- **Replicar o molde Wave 2 cegamente** em `AdminsListView.vue` e `AdminsFormView.vue` para todas as listas e formulários — copy-adapt, não reinventar. É exatamente o padrão que o usuário pediu ("mesma cara da plataforma").
- **Validação durante refator**: depois de cada view migrada, abrir no browser e comparar lado a lado com a `AdminsListView` ou `AdminsFormView` (gold standard). Espaçamento, cor, tipografia, hover devem bater.
- **CSS scoped**: ao terminar uma view, abrir o `<style scoped>` e confirmar que está mínimo (ideal: 0-20 linhas — só margin/gap específicos do layout daquela view, sem reimplementar primitivas). Se >50 linhas, parar e reconsiderar uso de DS.
- **Manter Vitest verde durante toda a refatoração**: rodar `pnpm --filter frontend vitest run` a cada 3-4 views migradas.
- **Não introduzir novo componente do DS**: se sentir falta de algum (ex: `AppTable`), parar e reportar — extração para o DS global é decisão fora desta Wave.
- **Liberdade técnica para dev**: ordem específica de implementação, micro-decisões de slot/prop, como organizar imports — tudo decisão do dev. O que é fixo são os CAs.
- **QA visual**: chrome-devtools MCP para tirar screenshot de cada tela e comparar com tela equivalente do app principal (ex: Pacientes, Equipe). Espaçamento e estética devem bater.

---

## 13. Atualização de documentação

- **`Docs/DESIGN.md`** — §"Módulo Admin Global" (linhas 67-75): atualizar a frase de abertura para refletir que o admin agora consome de fato todos os componentes principais do DS (lista expandida: `AppSidebar`, `AppTopBar`, `AppPageHeader`, `AppCard`, `AppButton`, `AppField`, `AppInput`, `AppSelect`, `AppTextarea`, `AppCheckbox`, `AppModal`, `AppToast`, `AppConfirmDialog`, `AppEmptyState`, `AppPagination`, `AppSearchInput`, `AppBadge`) — não apenas tokens HSL. Acrescentar referência a este briefing (`2026-05-30_003`) e CA `W3-CA1` a `W3-CA21`. Remover qualquer sugestão de que existe layout/topbar próprios do admin.

Nenhuma outra doc é tocada: arquitetura não muda, infra não muda, LGPD não muda, comandos não mudam.

---

## 14. Hand-off

- **Próximo agente**: `imedto-developer`.
- **Não aciona** `imedto-database` (sem schema/migration).
- **Após dev**: `imedto-qa` valida cada CA via chrome-devtools MCP + suíte automatizada + grep CSS + grep imports + revisão visual de cada uma das 17 telas. Commit + push no fim.
- **Critério de sucesso global**: usuário abre qualquer tela admin e o sentimento é "mesma cara da plataforma" — não "tela de manutenção visualmente esquecida".
