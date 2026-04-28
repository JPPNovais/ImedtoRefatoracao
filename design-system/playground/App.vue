<script setup lang="ts">
import { ref, computed } from "vue"
import {
  Button, Badge, CountBadge, Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter,
  Input, Textarea, Label, Field, CurrencyInput, SearchInput,
  Select, SelectTrigger, SelectValue, SelectContent, SelectItem, SelectGroup, SelectLabel,
  Checkbox, Switch, Slider, MultiSelect,
  Separator, Skeleton, Spinner, Progress,
  Alert, AlertTitle, AlertDescription,
  Avatar, AvatarImage, AvatarFallback,
  Tabs, TabsList, TabsTrigger, TabsContent,
  Accordion, AccordionItem, AccordionTrigger, AccordionContent,
  Collapsible, CollapsibleTrigger, CollapsibleContent,
  Table, TableHeader, TableBody, TableHead, TableRow, TableCell,
  Tooltip, TooltipProvider, TooltipTrigger, TooltipContent,
  Popover, PopoverTrigger, PopoverContent,
  Dialog, DialogTrigger, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose,
  Sheet, SheetTrigger, SheetContent, SheetHeader, SheetTitle, SheetDescription, SheetFooter, SheetClose,
  DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuLabel, DropdownMenuGroup,
  Menubar, MenubarMenu, MenubarTrigger, MenubarContent, MenubarItem, MenubarSeparator, MenubarLabel, MenubarShortcut,
  ScrollArea, EmptyState, PageHeader, PillToggle,
  Calendar, DatePicker, Pagination,
  ButtonGroup, Carousel, CarouselItem, CarouselPrevious, CarouselNext,
  Sonner, toast,
} from "../src/index"
import {
  Bell, Edit, Plus, ChevronDown, ChevronsUpDown, Trash2, User,
  AlertCircle, CheckCircle, Info, AlertTriangle, Loader2,
  Sun, Moon, Layers, Palette, Type, Ruler, SquareStack, FormInput,
  LayoutGrid, MessageSquare, Navigation, BarChart3, Search, BookOpen, Lightbulb,
  PuzzleIcon, ChevronRight,
} from "lucide-vue-next"

// ── Theme ──────────────────────────────────────────────
const isDark = ref(false)
function toggleTheme() {
  isDark.value = !isDark.value
  document.documentElement.classList.toggle("dark", isDark.value)
}

// ── Menu ───────────────────────────────────────────────
interface MenuItem  { id: string; label: string; icon: unknown }
interface MenuGroup { label: string; icon: unknown; items: MenuItem[] }

const menuGroups: MenuGroup[] = [
  {
    label: "Fundamentos", icon: Layers,
    items: [
      { id: "cores",       label: "Cores",                icon: Palette },
      { id: "tipografia",  label: "Tipografia",           icon: Type },
      { id: "espacamento", label: "Espaçamento & Bordas", icon: Ruler },
      { id: "tokens",      label: "Tokens CSS",           icon: SquareStack },
    ],
  },
  {
    label: "Componentes", icon: PuzzleIcon,
    items: [
      { id: "botoes",      label: "Botões",       icon: SquareStack },
      { id: "formularios", label: "Formulários",  icon: FormInput },
      { id: "dados",       label: "Dados & Display", icon: BarChart3 },
      { id: "feedback",    label: "Feedback",     icon: MessageSquare },
      { id: "navegacao",   label: "Navegação",    icon: Navigation },
      { id: "overlays",    label: "Overlays",     icon: LayoutGrid },
    ],
  },
  {
    label: "Diretrizes", icon: BookOpen,
    items: [
      { id: "diretrizes",  label: "Boas Práticas", icon: Lightbulb },
    ],
  },
]

const activeSection = ref("cores")
const searchQuery   = ref("")

const filteredMenuGroups = computed(() => {
  const q = searchQuery.value.toLowerCase().trim()
  if (!q) return menuGroups
  return menuGroups
    .map(g => ({ ...g, items: g.items.filter(i => i.label.toLowerCase().includes(q)) }))
    .filter(g => g.items.length)
})

const currentItem = computed(() => {
  for (const g of menuGroups) {
    const found = g.items.find(i => i.id === activeSection.value)
    if (found) return found
  }
  return menuGroups[0].items[0]
})
const currentGroup = computed(() =>
  menuGroups.find(g => g.items.some(i => i.id === activeSection.value))
)

function navigate(id: string) {
  activeSection.value = id
  searchQuery.value = ""
}

// ── Estados dos componentes ─────────────────────────────
const checkboxVal   = ref(false)
const switchVal     = ref(false)
const sliderVal     = ref([40])
const progressVal   = ref(65)
const currencyVal   = ref<number | null>(null)
const searchVal     = ref("")
const multiVal      = ref<string[]>([])
const pillVal       = ref("semana")
const calendarVal   = ref<string | null>(null)
const datePickerVal = ref<string | null>(null)
const pagina        = ref(1)
const tamanho       = ref(10)

const tabelaItens = [
  { id: 1, nome: "Dr. Carlos Mendes",  esp: "Cardiologia",  status: "Ativo",   n: 42 },
  { id: 2, nome: "Dra. Ana Paula",     esp: "Dermatologia", status: "Ativo",   n: 38 },
  { id: 3, nome: "Dr. Roberto Silva",  esp: "Ortopedia",    status: "Inativo", n: 17 },
  { id: 4, nome: "Dra. Juliana Costa", esp: "Pediatria",    status: "Ativo",   n: 55 },
]

// ── Design tokens para exibição ─────────────────────────
const coresmarca = [
  { nome: "primary",      hsl: "254 56% 38%", label: "Primária",      hex: "#4B2D99" },
  { nome: "primary-dark", hsl: "254 56% 21%", label: "Primária Dark", hex: "#29196B" },
  { nome: "secondary",    hsl: "0 0% 24%",    label: "Secundária",    hex: "#3D3D3D" },
  { nome: "muted",        hsl: "240 5% 96%",  label: "Muted",         hex: "#F4F4F5" },
  { nome: "accent",       hsl: "254 40% 95%", label: "Accent",        hex: "#EAE6F7" },
  { nome: "background",   hsl: "240 33% 99%", label: "Background",    hex: "#FAFAFF" },
  { nome: "card",         hsl: "0 0% 100%",   label: "Card",          hex: "#FFFFFF" },
]
const coresSem = [
  { hsl: "160 79% 39%", label: "Sucesso",   hex: "#14865E" },
  { hsl: "45 96% 47%",  label: "Atenção",   hex: "#EEB008" },
  { hsl: "0 84% 60%",   label: "Erro",      hex: "#F04438" },
  { hsl: "199 89% 48%", label: "Informação",hex: "#0EA5E9" },
]
const escalaFont = [
  { cls: "text-xs",   px: "12px",  sample: "Prontuário eletrônico" },
  { cls: "text-sm",   px: "14px",  sample: "Prontuário eletrônico" },
  { cls: "text-base", px: "16px",  sample: "Prontuário eletrônico" },
  { cls: "text-lg",   px: "18px",  sample: "Prontuário eletrônico" },
  { cls: "text-xl",   px: "20px",  sample: "Prontuário eletrônico" },
  { cls: "text-2xl",  px: "24px",  sample: "Prontuário eletrônico" },
  { cls: "text-3xl",  px: "30px",  sample: "Prontuário eletrônico" },
  { cls: "text-4xl",  px: "36px",  sample: "Prontuário" },
]
const tokensCss = [
  { nome: "--background",       valor: "240 33% 99%",  desc: "Fundo da página" },
  { nome: "--foreground",       valor: "0 0% 24%",     desc: "Texto principal" },
  { nome: "--card",             valor: "0 0% 100%",    desc: "Fundo de cards" },
  { nome: "--primary",          valor: "254 56% 38%",  desc: "Cor primária" },
  { nome: "--primary-dark",     valor: "254 56% 21%",  desc: "Primária escura / hover" },
  { nome: "--muted",            valor: "240 5% 96%",   desc: "Fundo suave" },
  { nome: "--muted-foreground", valor: "0 0% 45%",     desc: "Texto secundário" },
  { nome: "--border",           valor: "240 6% 90%",   desc: "Bordas" },
  { nome: "--success",          valor: "160 79% 39%",  desc: "Sucesso" },
  { nome: "--warning",          valor: "45 96% 47%",   desc: "Atenção" },
  { nome: "--error",            valor: "0 84% 60%",    desc: "Erro" },
  { nome: "--info",             valor: "199 89% 48%",  desc: "Informação" },
  { nome: "--radius",           valor: "0.5rem",        desc: "Border-radius base" },
]
const espDemos    = [1, 2, 3, 4, 6, 8, 10, 12, 16, 20, 24]
const radiusDemos = [["rounded-sm","4px"],["rounded","6px"],["rounded-md","6px"],["rounded-lg","8px"],["rounded-xl","12px"],["rounded-2xl","16px"],["rounded-full","9999px"]]
const shadowDemos = [["shadow-sm","sm"],["shadow","default"],["shadow-md","md"],["shadow-lg","lg"],["shadow-xl","xl"]]
</script>

<template>
  <TooltipProvider>
    <Sonner position="top-right" />

    <div class="min-h-screen bg-background text-foreground transition-colors" :class="{ dark: isDark }">

      <!-- ══ HEADER ══════════════════════════════════════════ -->
      <header class="sticky top-0 z-50 border-b border-border bg-card/95 backdrop-blur supports-[backdrop-filter]:bg-card/60">
        <div class="px-6 h-14 flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="h-9 w-9 rounded-xl bg-primary flex items-center justify-center shadow-sm">
              <Layers class="w-4 h-4 text-primary-foreground" />
            </div>
            <div>
              <h1 class="text-base font-bold leading-none tracking-tight">Imedto Design System</h1>
              <p class="text-[11px] text-muted-foreground mt-0.5">v0.1.0 · Fundamentos, componentes e diretrizes</p>
            </div>
          </div>
          <button
            class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-border text-xs font-medium transition-colors hover:bg-muted"
            @click="toggleTheme"
          >
            <component :is="isDark ? Sun : Moon" class="w-3.5 h-3.5" :class="isDark ? 'text-warning' : 'text-primary'" />
            {{ isDark ? "Light" : "Dark" }}
          </button>
        </div>
      </header>

      <div class="flex">

        <!-- ══ SIDEBAR ═════════════════════════════════════════ -->
        <aside class="w-[260px] shrink-0 border-r border-border bg-card/50 sticky top-14 h-[calc(100vh-3.5rem)] overflow-y-auto">

          <!-- Busca -->
          <div class="p-3 border-b border-border">
            <div class="relative">
              <Search class="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground w-3 h-3" />
              <input
                v-model="searchQuery"
                type="text"
                placeholder="Buscar..."
                class="w-full pl-8 pr-3 py-2 text-xs rounded-lg border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-primary/40 transition"
              />
            </div>
          </div>

          <!-- Nav -->
          <nav class="p-3 space-y-5">
            <div v-for="group in filteredMenuGroups" :key="group.label">
              <p class="text-[10px] font-bold uppercase tracking-widest text-muted-foreground mb-1.5 px-3 flex items-center gap-1.5">
                <component :is="group.icon" class="w-3 h-3 opacity-70" />
                {{ group.label }}
              </p>
              <ul class="space-y-0.5">
                <li v-for="item in group.items" :key="item.id">
                  <button
                    :class="[
                      'w-full text-left px-3 py-2 text-[13px] rounded-lg transition-all flex items-center gap-2.5',
                      activeSection === item.id
                        ? 'bg-primary/10 text-primary font-semibold shadow-sm'
                        : 'text-muted-foreground hover:text-foreground hover:bg-muted/70'
                    ]"
                    @click="navigate(item.id)"
                  >
                    <component :is="item.icon" class="w-4 h-4 shrink-0 opacity-70" />
                    {{ item.label }}
                  </button>
                </li>
              </ul>
            </div>

            <div v-if="filteredMenuGroups.length === 0" class="px-3 py-8 text-center">
              <Search class="w-6 h-6 text-muted-foreground/40 mx-auto mb-2" />
              <p class="text-xs text-muted-foreground">Nenhum resultado para "{{ searchQuery }}"</p>
            </div>
          </nav>
        </aside>

        <!-- ══ CONTEÚDO ══════════════════════════════════════════ -->
        <main class="flex-1 min-w-0">

          <!-- Breadcrumb -->
          <div class="border-b border-border px-8 py-3 bg-muted/20">
            <div class="flex items-center gap-2 text-xs">
              <component :is="currentGroup?.icon" class="w-3 h-3 text-muted-foreground" />
              <span class="text-muted-foreground">{{ currentGroup?.label }}</span>
              <ChevronRight class="w-3 h-3 text-muted-foreground/50" />
              <span class="text-foreground font-medium">{{ currentItem?.label }}</span>
            </div>
          </div>

          <!-- Seções -->
          <div class="p-8 max-w-5xl">
            <Transition name="fade" mode="out-in">
              <div :key="activeSection">

                <!-- ═══════ CORES ═══════ -->
                <section v-if="activeSection === 'cores'">
                  <PageHeader title="Cores" subtitle="Paleta do sistema baseada em tokens HSL — use sempre com hsl(var(--x))" />
                  <div class="space-y-8">
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Cores da marca</h3>
                      <div class="grid grid-cols-4 gap-3">
                        <div v-for="c in coresmarca" :key="c.nome" class="rounded-xl overflow-hidden border border-border shadow-sm">
                          <div class="h-20" :style="{ background: `hsl(${c.hsl})` }" />
                          <div class="bg-card p-2.5">
                            <p class="text-xs font-semibold">{{ c.label }}</p>
                            <p class="text-[10px] text-muted-foreground font-mono">{{ c.hex }}</p>
                            <p class="text-[10px] text-muted-foreground font-mono mt-0.5">{{ c.hsl }}</p>
                          </div>
                        </div>
                      </div>
                    </div>
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Cores semânticas</h3>
                      <div class="grid grid-cols-4 gap-3">
                        <div v-for="c in coresSem" :key="c.label" class="rounded-xl overflow-hidden border border-border shadow-sm">
                          <div class="h-20" :style="{ background: `hsl(${c.hsl})` }" />
                          <div class="bg-card p-2.5">
                            <p class="text-xs font-semibold">{{ c.label }}</p>
                            <p class="text-[10px] text-muted-foreground font-mono">{{ c.hex }}</p>
                          </div>
                        </div>
                      </div>
                    </div>
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Badges</h3>
                      <div class="flex flex-wrap gap-2">
                        <Badge>Default</Badge>
                        <Badge variant="secondary">Secondary</Badge>
                        <Badge variant="outline">Outline</Badge>
                        <Badge variant="destructive">Destructive</Badge>
                        <Badge variant="success">Sucesso</Badge>
                        <Badge variant="warning">Atenção</Badge>
                        <Badge variant="info">Info</Badge>
                      </div>
                      <div class="flex flex-wrap gap-2 mt-3">
                        <CountBadge>12</CountBadge>
                        <CountBadge variant="info">5</CountBadge>
                        <CountBadge variant="warning">3</CountBadge>
                        <CountBadge variant="error">99+</CountBadge>
                        <CountBadge variant="success">✓</CountBadge>
                        <CountBadge variant="muted">—</CountBadge>
                      </div>
                    </div>
                  </div>
                </section>

                <!-- ═══════ TIPOGRAFIA ═══════ -->
                <section v-if="activeSection === 'tipografia'">
                  <PageHeader title="Tipografia" subtitle="Nunito — sistema tipográfico do Imedto" />
                  <div class="space-y-8">
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-4">Escala de tamanhos</h3>
                      <div class="divide-y divide-border">
                        <div v-for="f in escalaFont" :key="f.cls" class="flex items-baseline gap-6 py-3">
                          <div class="w-24 shrink-0 text-right">
                            <p class="text-[10px] font-mono text-muted-foreground">.{{ f.cls }}</p>
                            <p class="text-[10px] font-mono text-muted-foreground">{{ f.px }}</p>
                          </div>
                          <p :class="f.cls" class="font-medium">{{ f.sample }}</p>
                        </div>
                      </div>
                    </div>
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-4">Pesos</h3>
                      <div class="space-y-2">
                        <p class="text-lg font-normal">font-normal (400) — Gestão de pacientes e clínicas</p>
                        <p class="text-lg font-medium">font-medium (500) — Gestão de pacientes e clínicas</p>
                        <p class="text-lg font-semibold">font-semibold (600) — Gestão de pacientes e clínicas</p>
                        <p class="text-lg font-bold">font-bold (700) — Gestão de pacientes e clínicas</p>
                        <p class="text-lg font-extrabold">font-extrabold (800) — Gestão de pacientes e clínicas</p>
                      </div>
                    </div>
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-4">Cores de texto</h3>
                      <div class="space-y-1.5">
                        <p class="text-foreground text-sm">text-foreground — Texto principal</p>
                        <p class="text-muted-foreground text-sm">text-muted-foreground — Secundário / dicas</p>
                        <p class="text-primary text-sm">text-primary — Links / destaques</p>
                        <p class="text-success text-sm">text-success — Confirmações</p>
                        <p class="text-warning text-sm">text-warning — Alertas suaves</p>
                        <p class="text-error text-sm">text-error — Erros</p>
                        <p class="text-info text-sm">text-info — Informações</p>
                      </div>
                    </div>
                  </div>
                </section>

                <!-- ═══════ ESPAÇAMENTO ═══════ -->
                <section v-if="activeSection === 'espacamento'">
                  <PageHeader title="Espaçamento & Bordas" subtitle="Escala baseada em múltiplos de 4px (0.25rem)" />
                  <div class="space-y-8">
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-4">Escala de espaços</h3>
                      <div class="space-y-2">
                        <div v-for="n in espDemos" :key="n" class="flex items-center gap-4">
                          <span class="text-[10px] font-mono text-muted-foreground w-6 text-right">{{ n }}</span>
                          <div class="bg-primary/25 rounded" :style="{ width: `${n * 4}px`, height: '18px' }" />
                          <span class="text-xs text-muted-foreground">{{ n * 4 }}px / {{ (n * 0.25).toFixed(2) }}rem</span>
                        </div>
                      </div>
                    </div>
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-4">Border radius</h3>
                      <div class="flex flex-wrap gap-5 items-end">
                        <div v-for="[cls, px] in radiusDemos" :key="cls" class="text-center">
                          <div class="bg-primary/20 border-2 border-primary/30 w-14 h-14 mb-2 mx-auto" :class="cls" />
                          <p class="text-[10px] font-mono text-muted-foreground">.{{ cls }}</p>
                          <p class="text-[10px] text-muted-foreground">{{ px }}</p>
                        </div>
                      </div>
                    </div>
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-4">Sombras</h3>
                      <div class="flex flex-wrap gap-6 items-start">
                        <div v-for="[cls, label] in shadowDemos" :key="cls" class="text-center">
                          <div class="bg-card w-24 h-16 rounded-lg mb-2" :class="cls" />
                          <p class="text-[10px] font-mono text-muted-foreground">.{{ cls }}</p>
                        </div>
                      </div>
                    </div>
                  </div>
                </section>

                <!-- ═══════ TOKENS CSS ═══════ -->
                <section v-if="activeSection === 'tokens'">
                  <PageHeader title="Tokens CSS" subtitle="Variáveis customizadas — use com hsl(var(--x))" />
                  <div class="rounded-lg border border-border overflow-hidden">
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>Token</TableHead>
                          <TableHead>Valor HSL</TableHead>
                          <TableHead>Prévia</TableHead>
                          <TableHead>Descrição</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        <TableRow v-for="t in tokensCss" :key="t.nome">
                          <TableCell><code class="text-xs font-mono text-primary bg-primary/8 px-1.5 py-0.5 rounded">{{ t.nome }}</code></TableCell>
                          <TableCell><code class="text-xs font-mono text-muted-foreground">{{ t.valor }}</code></TableCell>
                          <TableCell>
                            <div v-if="t.valor.includes('%')" class="w-6 h-6 rounded border border-border/60 shadow-sm" :style="{ background: `hsl(${t.valor})` }" />
                            <span v-else class="text-xs text-muted-foreground font-mono">{{ t.valor }}</span>
                          </TableCell>
                          <TableCell class="text-sm text-muted-foreground">{{ t.desc }}</TableCell>
                        </TableRow>
                      </TableBody>
                    </Table>
                  </div>
                </section>

                <!-- ═══════ BOTÕES ═══════ -->
                <section v-if="activeSection === 'botoes'">
                  <PageHeader title="Botões" subtitle="Variantes, tamanhos, estados e agrupamentos" />
                  <div class="space-y-8">
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Variantes</h3>
                      <div class="flex flex-wrap gap-3">
                        <Button>Default</Button>
                        <Button variant="secondary">Secondary</Button>
                        <Button variant="outline">Outline</Button>
                        <Button variant="ghost">Ghost</Button>
                        <Button variant="link">Link</Button>
                        <Button variant="destructive">Destructive</Button>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Tamanhos</h3>
                      <div class="flex flex-wrap gap-3 items-center">
                        <Button size="xs">Extra small</Button>
                        <Button size="sm">Small</Button>
                        <Button>Default</Button>
                        <Button size="lg">Large</Button>
                        <Button size="icon"><Bell class="w-4 h-4" /></Button>
                        <Button size="icon-sm" variant="outline"><Edit class="w-3.5 h-3.5" /></Button>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Com ícones & estados</h3>
                      <div class="flex flex-wrap gap-3 items-center">
                        <Button><Plus class="w-4 h-4" />Novo paciente</Button>
                        <Button variant="outline"><Edit class="w-4 h-4" />Editar</Button>
                        <Button variant="destructive"><Trash2 class="w-4 h-4" />Excluir</Button>
                        <Button disabled>Disabled</Button>
                        <Button><Loader2 class="w-4 h-4 animate-spin" />Salvando...</Button>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">ButtonGroup</h3>
                      <div class="space-y-3">
                        <ButtonGroup>
                          <Button variant="outline">Dia</Button>
                          <Button variant="outline">Semana</Button>
                          <Button>Mês</Button>
                          <Button variant="outline">Ano</Button>
                        </ButtonGroup>
                        <ButtonGroup orientation="vertical">
                          <Button variant="outline" size="sm">Exportar PDF</Button>
                          <Button variant="outline" size="sm">Exportar CSV</Button>
                          <Button variant="outline" size="sm">Imprimir</Button>
                        </ButtonGroup>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">PillToggle</h3>
                      <PillToggle v-model="pillVal" :opcoes="[{valor:'dia',label:'Dia'},{valor:'semana',label:'Semana'},{valor:'mes',label:'Mês'}]" />
                      <p class="text-sm text-muted-foreground mt-2">Selecionado: <strong>{{ pillVal }}</strong></p>
                    </div>
                  </div>
                </section>

                <!-- ═══════ FORMULÁRIOS ═══════ -->
                <section v-if="activeSection === 'formularios'">
                  <PageHeader title="Formulários" subtitle="Campos, seletores e controles interativos" />
                  <div class="space-y-8">
                    <div class="grid grid-cols-2 gap-5">
                      <Field label="Nome completo" required><Input placeholder="Dr. Carlos Mendes" /></Field>
                      <Field label="E-mail" hint="Usado para notificações"><Input type="email" placeholder="medico@clinica.com.br" /></Field>
                      <Field label="Com erro" erro="Este campo é obrigatório"><Input placeholder="Campo inválido" /></Field>
                      <Field label="Desabilitado"><Input disabled placeholder="Não editável" /></Field>
                    </div>
                    <Separator />
                    <div class="grid grid-cols-2 gap-5">
                      <Field label="Busca">
                        <SearchInput v-model="searchVal" :loading="searchVal.length > 0" placeholder="Buscar paciente..." />
                      </Field>
                      <Field label="Valor (R$)">
                        <CurrencyInput v-model="currencyVal" />
                      </Field>
                    </div>
                    <Separator />
                    <div class="grid grid-cols-2 gap-5">
                      <Field label="Especialidade">
                        <Select>
                          <SelectTrigger><SelectValue placeholder="Selecionar" /></SelectTrigger>
                          <SelectContent>
                            <SelectGroup><SelectLabel>Clínicas</SelectLabel>
                              <SelectItem value="cg">Clínica Geral</SelectItem>
                              <SelectItem value="card">Cardiologia</SelectItem>
                              <SelectItem value="derm">Dermatologia</SelectItem>
                            </SelectGroup>
                          </SelectContent>
                        </Select>
                      </Field>
                      <Field label="Especialidades (multi)">
                        <MultiSelect v-model="multiVal" :options="['Cardiologia','Dermatologia','Ortopedia','Pediatria','Neurologia']" placeholder="Adicionar especialidade..." />
                      </Field>
                    </div>
                    <Separator />
                    <div class="space-y-4">
                      <Field label="Observações"><Textarea placeholder="Anamnese, histórico..." class="min-h-20" /></Field>
                      <div class="flex flex-wrap gap-6">
                        <div class="flex items-center gap-2">
                          <Checkbox id="cb1" v-model:checked="checkboxVal" />
                          <Label for="cb1">Aceitar termos de uso</Label>
                        </div>
                        <div class="flex items-center gap-3">
                          <Switch id="sw1" v-model:checked="switchVal" />
                          <Label for="sw1">{{ switchVal ? "Notificações ativas" : "Notificações desligadas" }}</Label>
                        </div>
                      </div>
                      <div class="max-w-xs space-y-2">
                        <Label>Slider — {{ sliderVal[0] }}%</Label>
                        <Slider v-model="sliderVal" :min="0" :max="100" :step="1" />
                      </div>
                    </div>
                  </div>
                </section>

                <!-- ═══════ DADOS & DISPLAY ═══════ -->
                <section v-if="activeSection === 'dados'">
                  <PageHeader title="Dados & Display" subtitle="Tabelas, calendários, avatares, carousel e scroll" />
                  <div class="space-y-8">
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Table</h3>
                      <div class="rounded-md border border-border overflow-hidden">
                        <Table>
                          <TableHeader>
                            <TableRow>
                              <TableHead class="w-10">#</TableHead>
                              <TableHead>Nome</TableHead>
                              <TableHead>Especialidade</TableHead>
                              <TableHead class="text-right">Consultas</TableHead>
                              <TableHead>Status</TableHead>
                              <TableHead class="text-right">Ações</TableHead>
                            </TableRow>
                          </TableHeader>
                          <TableBody>
                            <TableRow v-for="p in tabelaItens" :key="p.id">
                              <TableCell class="text-muted-foreground text-xs">{{ p.id }}</TableCell>
                              <TableCell class="font-medium">{{ p.nome }}</TableCell>
                              <TableCell class="text-muted-foreground">{{ p.esp }}</TableCell>
                              <TableCell class="text-right font-mono text-sm">{{ p.n }}</TableCell>
                              <TableCell><Badge :variant="p.status === 'Ativo' ? 'success' : 'secondary'">{{ p.status }}</Badge></TableCell>
                              <TableCell class="text-right">
                                <div class="flex justify-end gap-1">
                                  <Button size="icon-sm" variant="ghost"><Edit class="w-3.5 h-3.5" /></Button>
                                  <Button size="icon-sm" variant="ghost" class="text-destructive hover:text-destructive"><Trash2 class="w-3.5 h-3.5" /></Button>
                                </div>
                              </TableCell>
                            </TableRow>
                          </TableBody>
                        </Table>
                      </div>
                    </div>
                    <Separator />
                    <div class="grid grid-cols-2 gap-8 items-start">
                      <div>
                        <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Calendar</h3>
                        <Calendar v-model="calendarVal" class="rounded-md border border-border" />
                        <p v-if="calendarVal" class="text-xs mt-2 text-muted-foreground">Selecionado: <strong>{{ calendarVal }}</strong></p>
                      </div>
                      <div class="space-y-4">
                        <div>
                          <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">DatePicker</h3>
                          <Field label="Data da consulta"><DatePicker v-model="datePickerVal" /></Field>
                          <p v-if="datePickerVal" class="text-xs mt-2 text-muted-foreground">Selecionado: <strong>{{ datePickerVal }}</strong></p>
                        </div>
                        <div>
                          <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Avatar</h3>
                          <div class="flex gap-3 items-center">
                            <Avatar class="w-12 h-12"><AvatarImage src="https://avatars.githubusercontent.com/u/1?v=4" alt="User" /><AvatarFallback>CM</AvatarFallback></Avatar>
                            <Avatar><AvatarFallback>AP</AvatarFallback></Avatar>
                            <Avatar class="w-8 h-8"><AvatarFallback class="text-xs">RS</AvatarFallback></Avatar>
                          </div>
                        </div>
                        <div>
                          <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">ScrollArea</h3>
                          <ScrollArea class="h-36 w-48 rounded-md border border-border">
                            <div class="p-3 space-y-1">
                              <div v-for="i in 15" :key="i" class="flex items-center gap-2 py-0.5">
                                <div class="w-1.5 h-1.5 rounded-full bg-primary shrink-0" />
                                <span class="text-xs">Paciente #{{ String(i).padStart(3,'0') }}</span>
                              </div>
                            </div>
                          </ScrollArea>
                        </div>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Carousel</h3>
                      <div class="px-12 relative">
                        <Carousel class="w-full">
                          <template #controls="{ scrollPrev, scrollNext, canScrollPrev, canScrollNext }">
                            <CarouselPrevious :disabled="!canScrollPrev" @click="scrollPrev" />
                            <CarouselNext :disabled="!canScrollNext" @click="scrollNext" />
                          </template>
                          <CarouselItem v-for="n in 6" :key="n" class="basis-1/3">
                            <Card><CardContent class="flex items-center justify-center h-24 p-4">
                              <span class="text-2xl font-bold text-primary">{{ n }}</span>
                            </CardContent></Card>
                          </CarouselItem>
                        </Carousel>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Paginação</h3>
                      <Pagination v-model:pagina="pagina" v-model:tamanho="tamanho" :total="87" rotulo-itens="pacientes" />
                    </div>
                  </div>
                </section>

                <!-- ═══════ FEEDBACK ═══════ -->
                <section v-if="activeSection === 'feedback'">
                  <PageHeader title="Feedback" subtitle="Alertas, toasts, spinners e estados de carregamento" />
                  <div class="space-y-8">
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Alert</h3>
                      <div class="space-y-2 max-w-xl">
                        <Alert><Info class="h-4 w-4" /><AlertTitle>Informação</AlertTitle><AlertDescription>Sua sessão expira em 15 minutos.</AlertDescription></Alert>
                        <Alert variant="success"><CheckCircle class="h-4 w-4" /><AlertTitle>Sucesso</AlertTitle><AlertDescription>Agendamento criado com sucesso!</AlertDescription></Alert>
                        <Alert variant="warning"><AlertTriangle class="h-4 w-4" /><AlertTitle>Atenção</AlertTitle><AlertDescription>Paciente com alergia a Dipirona.</AlertDescription></Alert>
                        <Alert variant="destructive"><AlertCircle class="h-4 w-4" /><AlertTitle>Erro</AlertTitle><AlertDescription>Falha ao salvar. Tente novamente.</AlertDescription></Alert>
                        <Alert variant="info"><Info class="h-4 w-4" /><AlertTitle>Info</AlertTitle><AlertDescription>Novo módulo disponível.</AlertDescription></Alert>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Toast (Sonner)</h3>
                      <div class="flex flex-wrap gap-2">
                        <Button variant="outline" size="sm" @click="toast.success('Prontuário salvo com sucesso!')">Sucesso</Button>
                        <Button variant="outline" size="sm" @click="toast.error('Falha ao excluir registro.')">Erro</Button>
                        <Button variant="outline" size="sm" @click="toast.warning('Estoque abaixo do mínimo.')">Atenção</Button>
                        <Button variant="outline" size="sm" @click="toast.info('Nova atualização disponível.')">Info</Button>
                        <Button variant="outline" size="sm" @click="toast('Agendamento confirmado', { description: 'Dr. Carlos Mendes — 14h00' })">Com descrição</Button>
                      </div>
                    </div>
                    <Separator />
                    <div class="grid grid-cols-2 gap-8">
                      <div>
                        <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Spinner</h3>
                        <div class="flex gap-5 items-center">
                          <div class="text-center"><Spinner size="sm" /><p class="text-[10px] text-muted-foreground mt-1.5">sm</p></div>
                          <div class="text-center"><Spinner /><p class="text-[10px] text-muted-foreground mt-1.5">default</p></div>
                          <div class="text-center"><Spinner size="lg" /><p class="text-[10px] text-muted-foreground mt-1.5">lg</p></div>
                        </div>
                      </div>
                      <div>
                        <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Skeleton</h3>
                        <div class="space-y-2 max-w-xs">
                          <div class="flex items-center gap-3"><Skeleton class="h-9 w-9 rounded-full" /><div class="flex-1 space-y-1.5"><Skeleton class="h-4 w-3/4" /><Skeleton class="h-3 w-1/2" /></div></div>
                          <Skeleton class="h-16 w-full rounded-lg" />
                        </div>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Progress</h3>
                      <div class="max-w-xs space-y-3">
                        <Progress :model-value="progressVal" />
                        <div class="flex items-center gap-2">
                          <Button size="sm" variant="outline" @click="progressVal = Math.max(0, progressVal - 10)">−10</Button>
                          <span class="text-sm font-mono w-12 text-center">{{ progressVal }}%</span>
                          <Button size="sm" variant="outline" @click="progressVal = Math.min(100, progressVal + 10)">+10</Button>
                        </div>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Empty State</h3>
                      <div class="grid grid-cols-2 gap-4">
                        <Card><CardContent class="p-0"><EmptyState icon="📂" title="Nenhum resultado" description="Não encontramos nada com esses filtros."><Button size="sm" variant="outline" class="mt-2">Limpar filtros</Button></EmptyState></CardContent></Card>
                        <Card><CardContent class="p-0"><EmptyState icon="🏥" title="Sem agendamentos" description="Nenhum agendamento para hoje." compact /></CardContent></Card>
                      </div>
                    </div>
                  </div>
                </section>

                <!-- ═══════ NAVEGAÇÃO ═══════ -->
                <section v-if="activeSection === 'navegacao'">
                  <PageHeader title="Navegação" subtitle="Tabs, accordion, menus e paginação" />
                  <div class="space-y-8">
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Tabs</h3>
                      <Tabs default-value="dados">
                        <TabsList>
                          <TabsTrigger value="dados">Dados pessoais</TabsTrigger>
                          <TabsTrigger value="historico">Histórico</TabsTrigger>
                          <TabsTrigger value="documentos">Documentos</TabsTrigger>
                        </TabsList>
                        <TabsContent value="dados"><Card><CardContent class="pt-4"><div class="grid grid-cols-2 gap-4"><Field label="Nome"><Input placeholder="Nome completo" /></Field><Field label="CPF"><Input placeholder="000.000.000-00" /></Field></div></CardContent></Card></TabsContent>
                        <TabsContent value="historico"><Card><CardContent class="pt-4"><EmptyState icon="📋" title="Sem histórico" compact /></CardContent></Card></TabsContent>
                        <TabsContent value="documentos"><Card><CardContent class="pt-4"><EmptyState icon="📄" title="Sem documentos" compact /></CardContent></Card></TabsContent>
                      </Tabs>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Accordion</h3>
                      <div class="max-w-lg">
                        <Accordion type="single" collapsible>
                          <AccordionItem value="a"><AccordionTrigger>Anamnese</AccordionTrigger><AccordionContent>Histórico de doenças, cirurgias e medicamentos em uso.</AccordionContent></AccordionItem>
                          <AccordionItem value="b"><AccordionTrigger>Exames solicitados</AccordionTrigger><AccordionContent>Hemograma completo, glicemia em jejum, colesterol total.</AccordionContent></AccordionItem>
                          <AccordionItem value="c"><AccordionTrigger>Prescrição médica</AccordionTrigger><AccordionContent>Metformina 500mg — 1 comprimido ao dia por 30 dias.</AccordionContent></AccordionItem>
                        </Accordion>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Collapsible</h3>
                      <div class="max-w-xs">
                        <Collapsible>
                          <div class="flex items-center justify-between border border-border rounded-md px-3 py-2">
                            <span class="text-sm font-medium">Filtros avançados</span>
                            <CollapsibleTrigger as-child><Button size="icon-sm" variant="ghost"><ChevronsUpDown class="h-4 w-4" /></Button></CollapsibleTrigger>
                          </div>
                          <CollapsibleContent>
                            <div class="mt-2 space-y-2 px-1">
                              <Field label="Status"><Select><SelectTrigger><SelectValue placeholder="Todos" /></SelectTrigger><SelectContent><SelectItem value="a">Ativo</SelectItem><SelectItem value="i">Inativo</SelectItem></SelectContent></Select></Field>
                            </div>
                          </CollapsibleContent>
                        </Collapsible>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">DropdownMenu</h3>
                      <DropdownMenu>
                        <DropdownMenuTrigger as-child><Button variant="outline">Ações <ChevronDown class="w-4 h-4" /></Button></DropdownMenuTrigger>
                        <DropdownMenuContent>
                          <DropdownMenuLabel>Paciente</DropdownMenuLabel>
                          <DropdownMenuSeparator />
                          <DropdownMenuItem><Edit class="w-4 h-4 mr-2" />Editar</DropdownMenuItem>
                          <DropdownMenuItem><Bell class="w-4 h-4 mr-2" />Enviar lembrete</DropdownMenuItem>
                          <DropdownMenuGroup><DropdownMenuItem><User class="w-4 h-4 mr-2" />Ver prontuário</DropdownMenuItem></DropdownMenuGroup>
                          <DropdownMenuSeparator />
                          <DropdownMenuItem class="text-destructive focus:text-destructive"><Trash2 class="w-4 h-4 mr-2" />Excluir</DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Menubar</h3>
                      <Menubar>
                        <MenubarMenu>
                          <MenubarTrigger>Arquivo</MenubarTrigger>
                          <MenubarContent>
                            <MenubarItem>Novo prontuário</MenubarItem>
                            <MenubarItem>Abrir...</MenubarItem>
                            <MenubarSeparator />
                            <MenubarItem>Exportar PDF</MenubarItem>
                          </MenubarContent>
                        </MenubarMenu>
                        <MenubarMenu>
                          <MenubarTrigger>Editar</MenubarTrigger>
                          <MenubarContent>
                            <MenubarItem>Desfazer <MenubarShortcut>⌘Z</MenubarShortcut></MenubarItem>
                            <MenubarItem>Refazer <MenubarShortcut>⌘Y</MenubarShortcut></MenubarItem>
                            <MenubarSeparator />
                            <MenubarItem>Copiar</MenubarItem>
                          </MenubarContent>
                        </MenubarMenu>
                        <MenubarMenu>
                          <MenubarTrigger>Ajuda</MenubarTrigger>
                          <MenubarContent>
                            <MenubarItem>Documentação</MenubarItem>
                            <MenubarItem>Suporte</MenubarItem>
                          </MenubarContent>
                        </MenubarMenu>
                      </Menubar>
                    </div>
                  </div>
                </section>

                <!-- ═══════ OVERLAYS ═══════ -->
                <section v-if="activeSection === 'overlays'">
                  <PageHeader title="Overlays" subtitle="Tooltips, popovers, dialogs e sheets" />
                  <div class="space-y-8">
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Tooltip</h3>
                      <div class="flex gap-3">
                        <Tooltip><TooltipTrigger as-child><Button size="icon" variant="outline"><Bell class="w-4 h-4" /></Button></TooltipTrigger><TooltipContent>Notificações</TooltipContent></Tooltip>
                        <Tooltip><TooltipTrigger as-child><Button size="icon" variant="outline"><Edit class="w-4 h-4" /></Button></TooltipTrigger><TooltipContent>Editar</TooltipContent></Tooltip>
                        <Tooltip><TooltipTrigger as-child><Button size="icon" variant="outline"><Trash2 class="w-4 h-4" /></Button></TooltipTrigger><TooltipContent side="bottom">Excluir (irreversível)</TooltipContent></Tooltip>
                      </div>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Popover</h3>
                      <Popover>
                        <PopoverTrigger as-child><Button variant="outline">Abrir Popover</Button></PopoverTrigger>
                        <PopoverContent class="w-72">
                          <div class="space-y-3">
                            <h4 class="font-semibold text-sm">Filtros rápidos</h4>
                            <Field label="Status"><Select><SelectTrigger><SelectValue placeholder="Todos" /></SelectTrigger><SelectContent><SelectItem value="a">Ativo</SelectItem><SelectItem value="i">Inativo</SelectItem></SelectContent></Select></Field>
                            <div class="flex justify-end gap-2"><Button size="sm" variant="ghost">Limpar</Button><Button size="sm">Aplicar</Button></div>
                          </div>
                        </PopoverContent>
                      </Popover>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Dialog</h3>
                      <Dialog>
                        <DialogTrigger as-child><Button>Abrir Dialog</Button></DialogTrigger>
                        <DialogContent>
                          <DialogHeader><DialogTitle>Confirmar exclusão</DialogTitle><DialogDescription>Esta ação não pode ser desfeita.</DialogDescription></DialogHeader>
                          <Alert variant="warning"><AlertTriangle class="h-4 w-4" /><AlertDescription>Todos os agendamentos serão excluídos.</AlertDescription></Alert>
                          <DialogFooter>
                            <DialogClose as-child><Button variant="outline">Cancelar</Button></DialogClose>
                            <Button variant="destructive">Excluir</Button>
                          </DialogFooter>
                        </DialogContent>
                      </Dialog>
                    </div>
                    <Separator />
                    <div>
                      <h3 class="text-xs font-bold uppercase tracking-widest text-muted-foreground mb-3">Sheet (Drawer)</h3>
                      <div class="flex gap-3">
                        <Sheet>
                          <SheetTrigger as-child><Button variant="outline">Direita</Button></SheetTrigger>
                          <SheetContent>
                            <SheetHeader><SheetTitle>Novo agendamento</SheetTitle><SheetDescription>Preencha os dados abaixo.</SheetDescription></SheetHeader>
                            <div class="py-6 space-y-4">
                              <Field label="Paciente"><Input placeholder="Buscar paciente..." /></Field>
                              <Field label="Data"><Input type="date" /></Field>
                              <Field label="Observações"><Textarea placeholder="Motivo da consulta..." /></Field>
                            </div>
                            <SheetFooter><SheetClose as-child><Button variant="outline">Cancelar</Button></SheetClose><Button>Criar</Button></SheetFooter>
                          </SheetContent>
                        </Sheet>
                        <Sheet>
                          <SheetTrigger as-child><Button variant="outline">Esquerda</Button></SheetTrigger>
                          <SheetContent side="left">
                            <SheetHeader><SheetTitle>Menu lateral</SheetTitle></SheetHeader>
                            <div class="py-4 space-y-1">
                              <button v-for="item in ['Início','Agenda','Pacientes','Financeiro','Relatórios']" :key="item" class="w-full text-left px-3 py-2 rounded-md text-sm hover:bg-accent transition-colors">{{ item }}</button>
                            </div>
                          </SheetContent>
                        </Sheet>
                      </div>
                    </div>
                  </div>
                </section>

                <!-- ═══════ DIRETRIZES ═══════ -->
                <section v-if="activeSection === 'diretrizes'">
                  <PageHeader title="Boas Práticas" subtitle="Diretrizes de uso do design system Imedto" />
                  <div class="space-y-6 max-w-2xl">
                    <Card>
                      <CardHeader><CardTitle class="text-base">Tokens em vez de valores fixos</CardTitle></CardHeader>
                      <CardContent class="text-sm text-muted-foreground space-y-2">
                        <p>Nunca hardcode cores, espaçamentos ou bordas. Use sempre as classes do Tailwind mapeadas nos tokens CSS.</p>
                        <p class="font-mono text-xs bg-muted px-3 py-2 rounded-md">✓ text-primary &nbsp;&nbsp;&nbsp; ✗ text-[#4B2D99]</p>
                        <p class="font-mono text-xs bg-muted px-3 py-2 rounded-md">✓ bg-muted &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; ✗ bg-[#F4F4F5]</p>
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader><CardTitle class="text-base">Componentes compostos</CardTitle></CardHeader>
                      <CardContent class="text-sm text-muted-foreground">
                        <p>Prefira composição de primitivos (<code class="font-mono text-xs bg-muted px-1 rounded">Card + CardHeader + CardContent</code>) em vez de criar variantes por prop. Isso mantém a API simples e a aparência previsível.</p>
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader><CardTitle class="text-base">Field para todos os campos</CardTitle></CardHeader>
                      <CardContent class="text-sm text-muted-foreground">
                        <p>Sempre envolva inputs com <code class="font-mono text-xs bg-muted px-1 rounded">Field</code> — ele cuida de label, hint, erro e espaçamento. Não crie wrappers paralelos.</p>
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader><CardTitle class="text-base">Dark mode</CardTitle></CardHeader>
                      <CardContent class="text-sm text-muted-foreground">
                        <p>O dark mode é automático via classe <code class="font-mono text-xs bg-muted px-1 rounded">.dark</code> no <code class="font-mono text-xs bg-muted px-1 rounded">&lt;html&gt;</code>. Se usar apenas tokens, o modo escuro funciona sem nenhum código extra.</p>
                      </CardContent>
                    </Card>
                  </div>
                </section>

              </div>
            </Transition>
          </div>

          <!-- Footer -->
          <div class="border-t border-border px-8 py-4 mt-8">
            <p class="text-[11px] text-muted-foreground">
              Imedto Design System · Baseado em shadcn-vue + Reka-UI + Tailwind CSS · Nunito Typeface
            </p>
          </div>
        </main>
      </div>
    </div>
  </TooltipProvider>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.15s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
