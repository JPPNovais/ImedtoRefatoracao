# Design System Imedto

> Referência diária para construir telas no Imedto. **Antes de criar componente novo, use esta página para verificar se já existe** o que você precisa. Se existir, importe — não duplique.

---

## 1. Visão geral

O Imedto tem **dois níveis** de design system:

1. **Pacote `@imedto/ui`** ([design-system/package.json](design-system/package.json)) — biblioteca Vue 3 de baixo nível baseada em [reka-ui](https://reka-ui.com) + Tailwind + `class-variance-authority`. Equivalente ao "shadcn-vue" do projeto. Exporta primitivas (`Button`, `Card`, `Sheet`, `Dialog`, `Pagination`, `Calendar`, `DataTable`, etc.).
2. **Wrappers `App*`** em [frontend/src/components/ui/](frontend/src/components/ui/) — adaptam as primitivas do `@imedto/ui` para a API local em português (`titulo`, `aberto`, `fechar`, `erro`) e fixam variantes/comportamentos do produto. **É a camada que as views devem consumir.**

A lista oficial dos wrappers está em [frontend/src/components/ui/index.ts](frontend/src/components/ui/index.ts):

`AppBadge`, `AppButton`, `AppCalendar`, `AppCard`, `AppDatePicker`, `AppDrawer`, `AppEmptyState`, `AppField`, `AppInput`, `AppModal`, `AppPageHeader`, `AppPagination`, `AppPillToggle`, `AppSelect`, `AppTextarea`.

### Regras de ouro

1. **Reuso > duplicação.** Antes de qualquer HTML/CSS scoped, procure aqui. Se algo já cobre 80% do caso, use e estenda — não copie.
2. **Sempre o wrapper, nunca a primitiva direto na view.** `AppButton`, não `Button` de `@imedto/ui`. Isso garante que mudanças de design propagam num único lugar.
3. **Tokens, não hex.** Cores vêm de variáveis CSS HSL em [main.css](frontend/src/assets/main.css). Hardcode de `#fff` ou `#2563eb` é bug.
4. **Componente novo nasce no `design-system/` quando for reutilizável.** Wrapper local só quando o uso é específico daquele domínio.
5. **Português em props/eventos** (`aberto`, `fechar`, `erro`, `titulo`) — convenção do projeto.

### Quando NÃO usar o design system

- Tela "única e descartável" (ex: página de erro 500 estática) — pode usar HTML cru.
- Componente altamente específico de um domínio (ex: `AgendamentoCalendarioMensal`) — vive em `components/<dominio>/`, não em `ui/`.
- Layout interno de uma view com 3 elementos triviais — não vale criar componente.

Em todos os outros casos: **se aparece em duas telas, é componente.**

---

## 2. Tokens

Definidos em [frontend/src/assets/main.css](frontend/src/assets/main.css), bloco `:root` (e `.dark` para tema escuro). Todos os valores de cor são HSL sem `hsl(...)` (para permitir alpha via `hsl(var(--primary) / 0.4)`).

### 2.1 Cores — semântica

| Token | Light (HSL) | Quando usar |
|---|---|---|
| `--background` | `240 33% 99%` | Fundo geral da aplicação |
| `--foreground` | `0 0% 24%` | Texto principal sobre `--background` |
| `--card` / `--card-foreground` | `0 0% 100%` / `0 0% 24%` | Fundo e texto de cards/painéis |
| `--popover` / `--popover-foreground` | idem card | Tooltips, dropdowns, popovers |
| `--primary` / `--primary-foreground` | `254 56% 38%` / `0 0% 100%` | Roxo Imedto. Ações primárias, links, foco |
| `--primary-light` | `240 33% 99%` | Hover suave em fundo roxo |
| `--primary-dark` | `254 56% 21%` | Hover/active de botões primários, títulos `h4` |
| `--secondary` / `--secondary-foreground` | `0 0% 24%` / `0 0% 100%` | Texto/UI secundário (cinza-escuro) |
| `--muted` / `--muted-foreground` | `240 5% 96%` / `0 0% 45%` | Fundos neutros, texto auxiliar, placeholders |
| `--accent` / `--accent-foreground` | `254 40% 95%` / `254 56% 38%` | Highlight roxo claro (item selecionado em menu) |
| `--destructive` / `--destructive-foreground` | `0 84% 60%` / `0 0% 100%` | Excluir, erro crítico |
| `--border` / `--input` | `240 6% 90%` | Bordas de cards, inputs |
| `--ring` | `254 56% 38%` | Anel de foco (acessibilidade) |
| `--success` | `160 79% 39%` | Sucesso (Confirmado, Pago, Ativo) |
| `--warning` | `45 96% 47%` | Aviso (Pendente, Rascunho, Expirado) |
| `--info` | `199 89% 48%` | Informacional (Enviado, Simples) |
| `--error` | `0 84% 60%` | Igual a `--destructive`, contexto de status |
| `--neutral` | `0 0% 100%` | Branco puro (cards, fundo de input) |

Aliases não-HSL (compatibilidade com estilos scoped antigos): `--primary-hover`, `--text`, `--text-muted`, `--text-faint`, `--border-strong`, `--bg`, `--bg-card`, `--bg-hover`, `--danger`. **Para código novo, prefira sempre `hsl(var(--token))`.**

Cores de gráficos: `--chart-1` a `--chart-5` (mesmas que primary/success/info/warning/error).

### 2.2 Tipografia

- **Família**: `'Nunito', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif` — carregada do Google Fonts em [main.css](frontend/src/assets/main.css).
- **Pesos disponíveis**: 400, 500, 600, 700, 800.
- **Body padrão**: 16px, cor `hsl(var(--secondary))`.
- **Escala** (utilitárias prontas em [main.css](frontend/src/assets/main.css)):

| Classe | Tamanho | Uso típico |
|---|---|---|
| `.text-xs` | 12px (0.75rem) | Labels de field, badges |
| `.text-sm` | 14px (0.875rem) | Texto secundário, botões |
| `.text-base` | 16px (1rem) | Body padrão |
| `.text-lg` | 18px (1.125rem) | Botões grandes, subtítulos |
| `.text-xl` | 20px (1.25rem) | Títulos de card |
| `.text-2xl` | 24px (1.5rem) | Títulos de seção |
| `.text-3xl` | 30px (1.875rem) | Títulos de página (`h4`) |

- **Pesos**: `.font-semibold` (600) para labels e botões, `.font-bold` (700) para títulos.
- **Comprimento de linha** ideal: 50–75 caracteres (use `max-w-prose` para corpos longos).

### 2.3 Espaçamentos e raios

- Base: múltiplos de **4px / 8px**.
- `--radius: 0.5rem` (8px) — padrão de cards, botões, inputs.
- `--radius-sm: calc(0.5rem - 4px)` (4px) — chips internos.
- `--radius-lg: calc(0.5rem + 4px)` (12px) — modais grandes.
- Pílulas (badges, tab-sub): `border-radius: 9999px`.

### 2.4 Sombras

- `--shadow`: `0 1px 3px rgba(0,0,0,.08), 0 1px 2px rgba(0,0,0,.04)` — cards padrão.
- `--shadow-md`: `0 4px 6px -1px rgba(0,0,0,.07), 0 2px 4px -2px rgba(0,0,0,.05)` — elevação ativa (tab principal selecionada, cards `elevated`).

### 2.5 Breakpoint principal

- **Mobile**: `max-width: 768px` — usado por `AppDrawer` (vira fullscreen) e por `.app-page` (padding reduzido).
- Outros breakpoints seguem Tailwind padrão (`sm`, `md`, `lg`, `xl`, `2xl`).

### 2.6 Tema escuro

`.dark` no `<html>` reescreve todos os tokens (ver [main.css](frontend/src/assets/main.css), linhas 76–104). **Nunca** hardcode cor — o token já cobre os dois temas.

---

## 3. Layout — container de página

Toda view interna dentro de `AppLayout` deve começar com um destes containers. Eles centralizam o conteúdo, aplicam padding consistente e limitam a largura — evitando o anti-padrão "tela vazia colada à esquerda em monitor wide".

```vue
<template>
    <main class="app-page">              <!-- padrão 1280px: listas, dashboards, cards -->
    <main class="app-page app-page--narrow"> <!-- 880px: formulários, perfil, conta/config -->
    <main class="app-page app-page--wide">   <!-- 1480px: relatórios, prontuário, calendários -->
    <main class="app-page app-page--full">   <!-- 100%: só agenda mensal e similares -->
</template>
```

Definição em [main.css](frontend/src/assets/main.css) (linhas 451–483). Mobile (`max-width: 768px`) reduz padding automaticamente.

**Anti-padrão:** declarar `max-width`, `margin: 0 auto` ou `padding` próprio na raiz da view. Ao tocar uma view legada que faça isso, migrar para `.app-page`.

---

## 4. Componentes

Cada subseção descreve o **wrapper local** (a API que você vai usar). Para a primitiva subjacente, ver [design-system/src/components/](design-system/src/components/).

### 4.1 AppButton

[AppButton.vue](frontend/src/components/ui/AppButton.vue)

Botão de ação. Único botão "comum" do produto — não criar variantes scoped.

**Props**

| Prop | Tipo | Default | Descrição |
|---|---|---|---|
| `variant` | `"primary" \| "secondary" \| "danger" \| "ghost" \| "google" \| "success"` | `"primary"` | Estilo visual |
| `size` | `"sm" \| "md" \| "lg"` | `"md"` | Tamanho |
| `type` | `"button" \| "submit" \| "reset"` | `"button"` | Tipo HTML nativo |
| `loading` | `boolean` | `false` | Mostra spinner e desabilita |
| `disabled` | `boolean` | `false` | Desabilita |
| `icon` | `string` | — | Classe Font Awesome à esquerda (`"fa-solid fa-plus"`) |
| `iconRight` | `string` | — | Classe FA à direita |
| `block` | `boolean` | `false` | `width: 100%` |

**Slots:** default (rótulo do botão).

**Exemplo**

```vue
<script setup lang="ts">
import { AppButton } from "@/components/ui"
import { ref } from "vue"

const salvando = ref(false)
async function salvar() { /* ... */ }
</script>

<template>
    <AppButton icon="fa-solid fa-plus" @click="novo">Novo paciente</AppButton>
    <AppButton variant="secondary" @click="cancelar">Cancelar</AppButton>
    <AppButton variant="danger" :loading="salvando" @click="excluir">Excluir</AppButton>
</template>
```

**Quando NÃO usar:** ações em coluna de tabela (use `.btn-icon` + variantes — ver §5); link textual sem moldura (use `<router-link>` cru com classe `.text-primary`).

---

### 4.2 AppBadge

[AppBadge.vue](frontend/src/components/ui/AppBadge.vue)

Pílula de status. Já vem com **mapeamento automático** de status comuns do domínio.

**Props**

| Prop | Tipo | Descrição |
|---|---|---|
| `status` | `string` | Aciona o mapeamento (`Agendado`, `Confirmado`, `Pendente`, `Pago`, `Vencido`, `Cancelado`, `Ativo`, `Inativo`, `Suspenso`, `SIMPLES`, `CONTROLADA`, etc. — lista completa em [AppBadge.vue](frontend/src/components/ui/AppBadge.vue)) |
| `variant` | `"default" \| "primary" \| "success" \| "warning" \| "error" \| "info" \| "muted"` | Override manual |
| `label` | `string` | Texto exibido (default: o próprio `status`) |

**Exemplo**

```vue
<AppBadge status="Confirmado" />               <!-- verde, "Confirmado" -->
<AppBadge status="DRAFT" label="Rascunho" />   <!-- amarelo, "Rascunho" -->
<AppBadge variant="info" label="Beta" />
```

**Quando NÃO usar:** contador numérico em ícone (use `CountBadge` direto do `@imedto/ui`).

---

### 4.3 AppPageHeader

[AppPageHeader.vue](frontend/src/components/ui/AppPageHeader.vue)

Cabeçalho padrão de view. Vai **sempre** no topo de toda página interna.

**Props:** `titulo: string`, `subtitulo?: string`.
**Slots:** `acoes` (botões à direita).

```vue
<AppPageHeader titulo="Pacientes" subtitulo="Veja todos os pacientes do estabelecimento.">
    <template #acoes>
        <AppButton icon="fa-solid fa-plus" @click="novo">Novo paciente</AppButton>
    </template>
</AppPageHeader>
```

**Quando NÃO usar:** dentro de drawers/modais — eles têm header próprio.

---

### 4.4 AppCard

[AppCard.vue](frontend/src/components/ui/AppCard.vue)

Container retangular com borda, fundo branco e sombra leve.

**Props**

| Prop | Tipo | Default | Descrição |
|---|---|---|---|
| `title` | `string` | — | Título no header |
| `subtitle` | `string` | — | Descrição abaixo do título |
| `padding` | `"none" \| "sm" \| "md" \| "lg"` | `"md"` | Padding do corpo |
| `elevated` | `boolean` | `false` | Sombra média |
| `flat` | `boolean` | `false` | Sem borda/sombra (fundo `muted/40`) |

**Slots:** default (corpo), `header-aside` (canto direito do header), `footer` (rodapé alinhado à direita).

```vue
<AppCard title="Resumo" subtitle="Dados do mês">
    <template #header-aside>
        <AppPillToggle v-model="periodo" :opcoes="periodos" />
    </template>
    <p>Conteúdo do card.</p>
    <template #footer>
        <AppButton variant="secondary">Ver mais</AppButton>
    </template>
</AppCard>
```

**Quando NÃO usar:** layout de listagem com tabela colada às bordas (use `.card` direto + tabela full-width).

---

### 4.5 AppField

[AppField.vue](frontend/src/components/ui/AppField.vue)

Wrapper de label + dica + erro para qualquer input. Usa o slot default para receber o controle.

**Props**

| Prop | Tipo | Descrição |
|---|---|---|
| `label` | `string` | Texto do label |
| `hint` | `string` | Texto auxiliar abaixo do input |
| `erro` | `string \| null` | Mensagem de erro (substitui hint, vermelha) |
| `required` | `boolean` | Marca o label com asterisco |
| `for` | `string` | `id` do input para associação `for`/`id` |
| `labelVariant` | `"default" \| "compact" \| "dense"` | Densidade do label |

```vue
<AppField label="Nome completo" required :erro="erros.nome">
    <AppInput id="nome" v-model="paciente.nome" placeholder="João da Silva" />
</AppField>
```

**Premissa LGPD:** mensagens de erro devem ser **genéricas** (vir do backend via `BusinessException`). Não monte texto no front com PII.

---

### 4.6 AppInput

[AppInput.vue](frontend/src/components/ui/AppInput.vue)

Input de texto/número/email. Pass-through das props nativas para a primitiva.

**Props:** `modelValue`, `type`, `placeholder`, `disabled`, `readonly`, `autocomplete`, `min`, `max`, `step`, `class`.
**Eventos:** `update:modelValue`.

```vue
<AppField label="E-mail">
    <AppInput v-model="email" type="email" autocomplete="email" placeholder="voce@empresa.com" />
</AppField>
```

**Quando NÃO usar:** valor monetário (use `CurrencyInput` do `@imedto/ui`); busca com debounce (combine com `useDebouncedRef` — ver §8); textarea (use `AppTextarea`); seleção de data (use `AppDatePicker`).

---

### 4.7 AppTextarea

[AppTextarea.vue](frontend/src/components/ui/AppTextarea.vue)

Input multilinhas.

**Props:** `modelValue`, `placeholder`, `disabled`, `readonly`, `rows`, `class`.
**Eventos:** `update:modelValue`.

```vue
<AppField label="Observações" hint="Visível apenas para a equipe.">
    <AppTextarea v-model="observacoes" :rows="4" />
</AppField>
```

---

### 4.8 AppSelect

[AppSelect.vue](frontend/src/components/ui/AppSelect.vue)

Wrapper finíssimo do `<select>` nativo, com estilo do design system. Renderiza o slot default como `<option>`s.

**Props:** `modelValue`, `disabled`, `class`.
**Eventos:** `update:modelValue`.

```vue
<AppField label="Estabelecimento" for="estab">
    <AppSelect id="estab" v-model="estabelecimentoId">
        <option value="">Selecione...</option>
        <option v-for="e in estabelecimentos" :key="e.id" :value="e.id">{{ e.nome }}</option>
    </AppSelect>
</AppField>
```

**Quando NÃO usar:** seleção múltipla (use `MultiSelect` do `@imedto/ui`); buscável/com >20 opções (use `Select` rico do `@imedto/ui` ou `MultiSelect` mesmo single).

---

### 4.9 AppDatePicker

[AppDatePicker.vue](frontend/src/components/ui/AppDatePicker.vue)

Input + popover de calendário. Trabalha com **string ISO `YYYY-MM-DD`** — o wrapper converte para `CalendarDate` por baixo.

**Props:** `modelValue: string | null`, `placeholder`, `disabled`, `min`, `max`, `ariaLabel`, `align`.
**Eventos:** `update:modelValue` (sempre string ISO).

```vue
<AppField label="Data de nascimento">
    <AppDatePicker v-model="paciente.dataNascimento" placeholder="Selecione..." :max="hoje" />
</AppField>
```

---

### 4.10 AppCalendar

[AppCalendar.vue](frontend/src/components/ui/AppCalendar.vue)

Calendário inline (sem input). Usado em telas como agenda/disponibilidade.

**Props:** `modelValue: string | null`, `min`, `max`, `datasComPonto: string[]` (dias com indicador).
**Eventos:** `update:modelValue` (string ISO), `mesMudou` (`{ ano, mes }` — `mes` 0-based).

```vue
<AppCalendar
    v-model="diaSelecionado"
    :datas-com-ponto="diasComAgendamento"
    @mes-mudou="carregarAgendamentos"
/>
```

---

### 4.11 AppPillToggle

[AppPillToggle.vue](frontend/src/components/ui/AppPillToggle.vue)

Toggle de poucas opções, em formato pílula. Genérico em `T`.

**Props:**
- `modelValue: T` — valor atual.
- `opcoes: Array<{ valor: T; label: string; icone?: string; icon?: string }>` — `icone` é alias antigo, `icon` é o novo.

**Eventos:** `update:modelValue`.

```vue
<script setup lang="ts">
import { ref } from "vue"
import { AppPillToggle } from "@/components/ui"
type Periodo = "dia" | "semana" | "mes"
const periodo = ref<Periodo>("semana")
const opcoes = [
    { valor: "dia",    label: "Dia",    icon: "fa-solid fa-calendar-day" },
    { valor: "semana", label: "Semana", icon: "fa-solid fa-calendar-week" },
    { valor: "mes",    label: "Mês",    icon: "fa-solid fa-calendar" },
] as const
</script>

<template>
    <AppPillToggle v-model="periodo" :opcoes="opcoes" />
</template>
```

**Quando NÃO usar:** mais de 4–5 opções (vira `AppSelect` ou `Tabs`).

---

### 4.12 AppDrawer

[AppDrawer.vue](frontend/src/components/ui/AppDrawer.vue)

Painel lateral à direita. Usado para criação/edição em fluxos onde o contexto da listagem deve permanecer visível. Vira fullscreen abaixo de 768px.

**Props:** `aberto: boolean`, `titulo?: string`, `largura?: number` (default 500px).
**Eventos:** `fechar`.
**Slots:** default (corpo), `titulo` (substitui o título), `rodape` (botões de ação fixos no fim).

```vue
<AppDrawer :aberto="aberto" titulo="Editar paciente" :largura="640" @fechar="fechar">
    <AppField label="Nome">
        <AppInput v-model="form.nome" />
    </AppField>
    <template #rodape>
        <AppButton variant="secondary" @click="fechar">Cancelar</AppButton>
        <AppButton :loading="salvando" @click="salvar">Salvar</AppButton>
    </template>
</AppDrawer>
```

**Quando NÃO usar:** confirmação curta (use `AppModal`); fluxo com mais de 3 passos (use página dedicada).

---

### 4.13 AppModal

[AppModal.vue](frontend/src/components/ui/AppModal.vue)

Diálogo central. Usar para confirmação, alertas, formulário curto e visualização rápida.

**Props:** `aberto: boolean`, `titulo?: string`, `largura?: "sm" | "md" | "lg"` (420 / 560 / 720), `acimaDeDrawer?: boolean` (z-index 600 — para empilhar sobre `AppDrawer`), `semPaddingCorpo?: boolean`.
**Eventos:** `fechar`.
**Slots:** default (corpo), `titulo`, `rodape`.

```vue
<AppModal :aberto="confirmando" titulo="Excluir paciente?" largura="sm" @fechar="confirmando = false">
    <p>Esta ação é irreversível. Tem certeza?</p>
    <template #rodape>
        <AppButton variant="secondary" @click="confirmando = false">Cancelar</AppButton>
        <AppButton variant="danger" :loading="excluindo" @click="excluir">Excluir</AppButton>
    </template>
</AppModal>
```

**Quando usar `acimaDeDrawer`:** confirmação dentro de drawer aberto (ex: "Cancelar edição? Você tem alterações não salvas").

---

### 4.14 AppEmptyState

[AppEmptyState.vue](frontend/src/components/ui/AppEmptyState.vue)

Estado vazio padrão. **Nunca** use `<p>Nenhum item</p>`.

**Props:** `icone` (FA), `titulo`, `descricao`, `compacto`.
**Slots:** `acao`.

```vue
<AppEmptyState
    icone="fa-solid fa-users"
    titulo="Nenhum paciente cadastrado"
    descricao="Adicione o primeiro paciente para começar."
>
    <template #acao>
        <AppButton icon="fa-solid fa-plus" @click="novo">Novo paciente</AppButton>
    </template>
</AppEmptyState>
```

---

### 4.15 AppPagination

[AppPagination.vue](frontend/src/components/ui/AppPagination.vue)

Paginação completa: navegação numérica com ellipsis, seletor de itens por página, contador "1–20 de 47 itens". **Não reimplemente lógica de página/ellipsis na view** — sempre delegue.

**Props:** `pagina: number`, `tamanho: number`, `total: number`, `tamanhos?: number[]` (default `[10, 20, 30]`), `rotuloItens?: string` (default `"itens"`), `ocultarTamanhos?: boolean`.
**Eventos:** `update:pagina`, `update:tamanho`.

```vue
<AppPagination
    v-model:pagina="pagina"
    v-model:tamanho="tamanho"
    :total="dados.total"
    rotulo-itens="pacientes"
/>
```

---

## 5. Classes utilitárias globais

Definidas em [main.css](frontend/src/assets/main.css). Use-as **diretamente**, não as redefina em `<style scoped>`.

### 5.1 Botões de ação em tabelas (`.btn-icon-*`)

Padrão para colunas de ações de listagem (ver/editar/excluir). Tamanho fixo 32×32px, ícone único, hover colorido.

```vue
<td class="acoes">
    <button class="btn-icon btn-icon-ver"     title="Ver detalhes" @click="ver(item)">
        <i class="fa-solid fa-eye" />
    </button>
    <button class="btn-icon btn-icon-editar"  title="Editar" @click="editar(item)">
        <i class="fa-solid fa-pen" />
    </button>
    <button class="btn-icon btn-icon-excluir" title="Excluir" :disabled="excluindoId === item.id" @click="excluir(item)">
        <i class="fa-solid fa-trash" />
    </button>
</td>
```

**Não criar variantes scoped** — `<style scoped>` com data-attr atinge a raiz do componente filho e quebra silenciosamente. Sempre as classes globais.

### 5.2 Botões textuais base (legado, paridade visual)

`.btn-primary`, `.btn-primary-sm`, `.btn-primary-lg`, `.btn-secondary`, `.btn-secondary-sm`, `.btn-danger`, `.btn-ghost`, `.btn-success`, `.btn-success-sm`, `.btn-google`. **Para código novo, prefira `<AppButton>`** — essas classes existem só para portabilidade do legado.

### 5.3 Inputs/labels base (legado)

`.form-input`, `.field-label`, `.field-label-compact`, `.field-label-dense`. **Para código novo, prefira `<AppInput>` + `<AppField>`.**

### 5.4 Tabs

`.tab-main` + `.tab-main-active` (tabs principais, retangulares com sombra).
`.tab-sub` + `.tab-sub-active` (tabs em pílula). Para tabs novos, prefira `Tabs` do `@imedto/ui`.

### 5.5 Cor / fundo / borda utilitárias

`.text-primary`, `.text-primary-dark`, `.text-secondary`, `.text-muted`, `.text-success`, `.text-warning`, `.text-error`, `.text-info`.
`.bg-primary`, `.bg-primary-light`, `.bg-card`, `.bg-neutral`, `.bg-muted`.
`.border`, `.border-primary`.

### 5.6 Animações

`.animate-fade-in-up`, `.animate-fade-in-right`, `.animate-float`, `.delay-100/200/300/400`. Use com parcimônia — animação é comunicação, não decoração.

---

## 6. Padrões de UI

### 6.1 Listagem padrão (header + busca debounced + tabela + paginação)

Exemplo real em [PacientesView.vue](frontend/src/views/pacientes/PacientesView.vue):

```vue
<script setup lang="ts">
import { ref, watch } from "vue"
import { AppButton, AppPageHeader, AppPagination, AppEmptyState, AppInput, AppField } from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { pacienteService, type PaginaPacientes } from "@/services/pacienteService"

const buscaInput = ref("")
const busca      = useDebouncedRef(buscaInput, 300)
const pagina     = ref(1)
const tamanho    = ref(20)
const dados      = ref<PaginaPacientes | null>(null)
const carregando = ref(false)

watch(busca, () => { pagina.value = 1 })
watch([busca, pagina, tamanho], () => carregar(), { immediate: true })

async function carregar() {
    carregando.value = true
    try {
        dados.value = await pacienteService.listar(busca.value, pagina.value, tamanho.value)
    } finally {
        carregando.value = false
    }
}
</script>

<template>
    <main class="app-page">
        <AppPageHeader titulo="Pacientes">
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="novo">Novo paciente</AppButton>
            </template>
        </AppPageHeader>

        <AppField>
            <AppInput v-model="buscaInput" placeholder="Buscar por nome ou CPF..." />
        </AppField>

        <div v-if="carregando">Carregando...</div>
        <AppEmptyState
            v-else-if="!dados?.itens.length"
            icone="fa-solid fa-users"
            titulo="Nenhum paciente encontrado"
            descricao="Tente outro filtro ou cadastre um novo."
        />
        <table v-else class="tabela">
            <!-- linhas com .btn-icon-ver / -editar / -excluir -->
        </table>

        <AppPagination
            v-if="dados"
            v-model:pagina="pagina"
            v-model:tamanho="tamanho"
            :total="dados.total"
            rotulo-itens="pacientes"
        />
    </main>
</template>
```

### 6.2 Drawer de criação/edição compartilhado

Reuse os mesmos campos para "criar" e "editar" (ex: [AgendamentoFormFields](frontend/src/components/agenda/AgendamentoFormFields.vue) — extrair em componente parametrizado por props.

```vue
<AppDrawer :aberto="aberto" :titulo="modo === 'criar' ? 'Novo paciente' : 'Editar paciente'" @fechar="fechar">
    <PacienteFormFields v-model="form" :erros="erros" />
    <template #rodape>
        <AppButton variant="secondary" @click="fechar">Cancelar</AppButton>
        <AppButton :loading="salvando" @click="salvar">Salvar</AppButton>
    </template>
</AppDrawer>
```

### 6.3 Modal de confirmação

```vue
<AppModal :aberto="confirmando" titulo="Excluir agendamento?" largura="sm" :acima-de-drawer="dentroDeDrawer" @fechar="confirmando = false">
    <p>O paciente será notificado por e-mail.</p>
    <template #rodape>
        <AppButton variant="secondary" @click="confirmando = false">Cancelar</AppButton>
        <AppButton variant="danger" :loading="excluindo" @click="confirmar">Excluir</AppButton>
    </template>
</AppModal>
```

### 6.4 Card de dashboard

```vue
<AppCard title="Atendimentos" subtitle="Últimos 30 dias" elevated>
    <template #header-aside>
        <AppPillToggle v-model="periodo" :opcoes="periodos" />
    </template>
    <p class="text-3xl font-bold">{{ total }}</p>
    <p class="text-muted text-sm">{{ comparado }}</p>
</AppCard>
```

### 6.5 Estados padrão por tela

| Estado | Como representar |
|---|---|
| Loading inicial | Spinner ou `<Skeleton>` (do `@imedto/ui`) — **não** travar a tela com tela branca |
| Vazio | `AppEmptyState` com ação primária |
| Erro de rede | Banner de erro acima do conteúdo + botão "Tentar novamente" |
| Erro de negócio (422) | Mensagem do backend exibida no campo (`AppField :erro="..."`) ou em toast |
| Sucesso (criação/edição) | Toast (`toast.success(...)` do `@imedto/ui` via `vue-sonner`) + atualização da lista |
| Desabilitado | Sempre com motivo claro (tooltip ou hint embaixo) |

---

## 7. Acessibilidade — guia mínimo

Não é checklist de auditoria — é o que **lembrar ao criar componente novo**. Sistema de saúde precisa ser usável por todo mundo.

- **Contraste WCAG AA**: 4.5:1 para texto normal, 3:1 para texto grande/UI. Os tokens já passam — não diminua opacidade abaixo de `0.7` em texto sobre cor.
- **Foco visível**: nunca remova `outline` sem alternativa. As primitivas do `@imedto/ui` já entregam `ring` via `--ring`. Se você customizar, mantenha `:focus-visible { box-shadow: 0 0 0 2px hsl(var(--ring) / 0.4); }`.
- **Labels reais**: input sempre dentro de `<AppField label="...">` ou com `<label for="...">` explícito. Placeholder **não substitui label** (some no foco e quebra leitor de tela).
- **Touch target**: mínimo 44×44px em mobile. `.btn-icon` é 32×32 — adequado para desktop; em mobile, embrulhe num container maior se for ação primária.
- **Semantic HTML**: `<button>` para clique, `<a>` para navegação, `<table>` para dados tabulares (não `<div>`s). Hierarquia de heading correta (`h1` → `h2` → `h3`, sem pular).
- **Teclado em drawer/modal**: `Esc` fecha, foco fica preso dentro, ao fechar volta para o trigger. As primitivas de `Sheet`/`Dialog` (reka-ui) já fazem isso — **não** desabilite com `modal={false}` sem necessidade.
- **Anúncios**: ações destrutivas (excluir) devem usar `AppModal` de confirmação, não confiar em `confirm()` nativo (substituir conforme as views legadas migram). Sucesso/erro via toast com `aria-live` (já cuidado pelo `vue-sonner`).
- **Cor sozinha não comunica**: status sempre tem texto + cor (já é o comportamento do `AppBadge`). Em gráficos, adicione padrão/símbolo além da cor.
- **`prefers-reduced-motion`**: animações decorativas (`.animate-float`, transições longas) precisam respeitar. Adicione em estilos custom:
    ```css
    @media (prefers-reduced-motion: reduce) { .animate-float { animation: none; } }
    ```
- **`lang="pt-BR"`** no `<html>` do `index.html`. **`autocomplete`** correto em inputs sensíveis (`email`, `tel`, `name`, `street-address`).
- **PII em leitor de tela**: nunca exibir CPF/dado sensível em `aria-label` decorativo — o usuário cego não quer ouvir o CPF de cada linha em alta voz. Use `aria-label="Ver detalhes do paciente João da Silva"`, não `"...CPF 123.456.789-00"`.

---

## 8. Convenções de busca e listagem

### 8.1 Debounce em buscas que tocam a API

**Toda** entrada de busca cujo valor dispara request HTTP precisa de debounce (~300ms). Use o composable [useDebouncedRef](frontend/src/composables/useDebouncedRef.ts), **nunca** `setTimeout` manual.

```ts
import { ref, watch } from "vue"
import { useDebouncedRef } from "@/composables/useDebouncedRef"

const buscaInput = ref("")                       // v-model do <AppInput>
const busca      = useDebouncedRef(buscaInput)   // ref atrasado que aciona request

watch(busca, () => { pagina.value = 1 })
watch([busca, pagina, tamanho], () => carregar(), { immediate: true })
```

**Filtros client-side** (lista já carregada e filtrada por `computed`): **não** precisam de debounce.
**Cliques de paginação/ordenação**: imediatos, **não** ficam atrás do debounce.

### 8.2 Paginação

Sempre `AppPagination` com `v-model:pagina` + `v-model:tamanho` + `:total`. O componente já entrega seletor de tamanho (10/20/30 padrão) e ellipsis.

### 8.3 Backend espelhado

Toda regra do front depende do backend (DDD + CQRS, ver [CLAUDE.md](CLAUDE.md)). Validação de input, paginação, ordenação — tudo passa pelo backend. Front é UX; backend é fonte da verdade.

---

## 9. Como adicionar um componente novo

Antes de escrever a primeira linha, faça este checklist:

1. **Já existe?** Procure em [frontend/src/components/ui/index.ts](frontend/src/components/ui/index.ts) e em [design-system/src/index.ts](design-system/src/index.ts). Se sim, importe.
2. **É reutilizável (aparece ou poderia aparecer em duas telas)?**
    - **Sim** → vai pro design system:
        a. Crie em `design-system/src/components/<nome>/` seguindo o padrão dos vizinhos (componente Vue + `index.ts` que exporta).
        b. Registre em [design-system/src/index.ts](design-system/src/index.ts).
        c. `cd design-system && npm run build` (gera `dist/`).
        d. Volte ao frontend e crie o wrapper `App<Nome>.vue` em [frontend/src/components/ui/](frontend/src/components/ui/), traduzindo a API para português (`titulo`, `aberto`, `fechar`, `erro`).
        e. Registre o wrapper em [frontend/src/components/ui/index.ts](frontend/src/components/ui/index.ts).
        f. Atualize **este documento** (§4 — adicione a subseção do componente).
3. **É específico de um domínio?** Cria em `frontend/src/components/<dominio>/` (ex: `agenda/`, `pacientes/`). Não vai pro design system. Se possível, componha com componentes do `App*`.
4. **Type-check antes de declarar pronto:** `npm run build` no frontend, `dotnet build` no backend (se houver mudança de contrato).
5. **Testes**: se for primitiva (filtro de data, parser de CPF), adicione teste. Se for visual puro, screenshot manual basta.

### Playground do design-system

`design-system/playground/` tem um Vite dedicado para isolar componente. Rodar:

```bash
cd design-system && npm run playground
```

---

## 10. Anti-padrões frequentes

Se você está fazendo algo desta lista, pare e revise:

- **Variantes scoped duplicadas.** Reescrever `.btn-icon-excluir` dentro de `<style scoped>` reabre o bug "scoped data-attr atinge root do componente filho". Use a classe global.
- **Hardcode de cor**: `color: #2563eb`, `background: #fff`. Use `hsl(var(--token))`.
- **Layout sem `.app-page`**: declarar `max-width` próprio na raiz da view. Use o container utilitário.
- **`<input>` cru sem `AppInput`/`AppField`**: perde label, hint, erro, foco, contraste, dark mode.
- **Botão refeito do zero**: `<button class="bg-blue-600 text-white px-4 py-2 rounded">Salvar</button>`. Use `<AppButton>`.
- **Listagem sem debounce**: `@input` chamando `service.listar()` direto. Use `useDebouncedRef`.
- **Reimplementar paginação**: lógica de ellipsis no v-for, "página atual" computada manualmente. Use `AppPagination`.
- **`<p>Nenhum resultado</p>`**: vire `AppEmptyState`.
- **`confirm()` nativo para ações destrutivas**: feio, não acessível, não traduzível. Use `AppModal`.
- **Mensagem de erro com PII**: `"Paciente João Silva CPF 123... não pode ser excluído"`. Use mensagem genérica do backend (`BusinessException` → 422). LGPD ([CLAUDE.md](CLAUDE.md), seção LGPD).
- **`setTimeout` manual para debounce**: timer leak. Use `useDebouncedRef`.
- **Importar de `@imedto/ui` direto na view**: pula o wrapper. Sempre `from "@/components/ui"`.
- **Componente novo "criativo" quando o existente cobre 80%**: vire propriedade ou slot do existente, não componente paralelo.
- **Header de página inline**: `<div><h1>...</h1><div class="botoes">...</div></div>`. Use `AppPageHeader` com slot `#acoes`.

---

## Referências

- [CLAUDE.md](CLAUDE.md) — premissas do produto e do código.
- [main.css](frontend/src/assets/main.css) — tokens e classes utilitárias.
- [components/ui/](frontend/src/components/ui/) — wrappers locais.
- [design-system/](design-system/) — pacote `@imedto/ui` (primitivas).
- [useDebouncedRef.ts](frontend/src/composables/useDebouncedRef.ts) — debounce padrão.
