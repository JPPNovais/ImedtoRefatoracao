<script setup lang="ts">
import { ref, computed, reactive, watch } from 'vue'
import { AppModal } from '@/components/ui'
import { AppButton } from '@/components/ui'
import { AppPillToggle } from '@/components/ui'
import { RAMOS_CIRCUNFERENCIAL } from './regioesCircunferenciais'

export interface ExameFisicoRegiao {
  id: string
  nome: string
  nivel: 1 | 2 | 3
  lateralidade: boolean
  pai_id: string | null
  vista: 'anterior' | 'posterior' | 'ambos' | 'circunferencial' | null
  template_texto: string | null
  filhos?: ExameFisicoRegiao[]
}

export interface MembroRegioes {
  tipo: 'superior' | 'inferior'
  dirBase: ExameFisicoRegiao | null
  esquBase: ExameFisicoRegiao | null
}

/**
 * Modo de lista agrupada por parte do tronco.
 * Quando não-null, o passo de sub-regiões exibe grupos Tórax/Abdome/Pelve (ou Tórax/Lombossacra/Pelve)
 * em vez da lista padrão de filhos da região clicada.
 */
export interface TroncoGrupos {
  grupos: Array<{ label: string; regiaoBaseId: string }>
}

type LadoMembro = 'D' | 'E' | 'bilateral'
type VistaPasso = 'anterior' | 'posterior' | 'circunferencial'

const OPCOES_LADO = [
  { valor: 'D' as LadoMembro,        label: 'Direito'  },
  { valor: 'E' as LadoMembro,        label: 'Esquerdo' },
  { valor: 'bilateral' as LadoMembro, label: 'Ambos'   },
]

const OPCOES_VISTA = [
  { valor: 'anterior' as VistaPasso,       label: 'Anterior'       },
  { valor: 'posterior' as VistaPasso,      label: 'Posterior'      },
  { valor: 'circunferencial' as VistaPasso, label: 'Circunferencial' },
]

const props = defineProps<{
  aberto: boolean
  regiaoClicada: ExameFisicoRegiao | null
  regioes: ExameFisicoRegiao[]
  regioesJaSelecionadas: string[]
  getFilhos: (regiaoId: string) => ExameFisicoRegiao[]
  membroRegioes?: MembroRegioes | null
  /** Quando fornecida, o passo de vista inicia com esse valor pré-selecionado (M3 híbrido — mantém as 3 opções editáveis). */
  vistaInicial?: VistaPasso | null
  /** Quando fornecida, o passo de sub-regiões opera em modo "lista agrupada por parte do tronco" (DP-3). */
  troncoGrupos?: TroncoGrupos | null
}>()

const emit = defineEmits<{
  'update:aberto': [value: boolean]
  confirmar: [regioes: Array<{ regiaoId: string; lateralidade: 'D' | 'E' | 'bilateral' | null; vista: VistaPasso | null }>]
}>()

// ── Estado de passo ───────────────────────────────────────────────────────────
// Membro:     'lado' → 'vista' → 'subregioes'
// Não-membro: 'vista' → 'subregioes'
// Tronco (sintético): 'vista' → 'subregioes' (agrupado por parte)
const passo = ref<'lado' | 'vista' | 'subregioes'>(
  props.membroRegioes ? 'lado' : 'vista'
)
const ladoEscolhido = ref<LadoMembro | null>(null)
// vistaInicial pré-preenche a vista quando fornecida (M3 híbrido — 3 opções permanecem editáveis).
const vistaEscolhida = ref<VistaPasso | null>(props.vistaInicial ?? null)

// Quando o popup abre, re-inicializa o estado respeitando vistaInicial e troncoGrupos.
watch(() => props.aberto, (abre) => {
  if (!abre) return
  limparSelecao()
  ladoEscolhido.value = null
  passo.value = props.membroRegioes ? 'lado' : 'vista'
  vistaEscolhida.value = props.vistaInicial ?? null
})

// ── Base ativa depende do lado (R4) ──────────────────────────────────────────
const baseAtiva = computed<ExameFisicoRegiao | null>(() => {
  if (!props.membroRegioes) return props.regiaoClicada
  if (ladoEscolhido.value === 'E') return props.membroRegioes.esquBase
  // Direito e Ambos usam dirBase como base canônica (R4/R5)
  return props.membroRegioes.dirBase
})

/**
 * Dado o nó base ativo (ex.: "msd-anterior"), resolve qual nó
 * circunferencial corresponde (ex.: "msd-circunferencial").
 * Derivação por convenção: troca o sufixo "-anterior"/"-posterior" por "-circunferencial".
 */
const idCircunferencial = computed<string | null>(() => {
  if (!baseAtiva.value) return null
  const base = baseAtiva.value.id
    .replace(/-anterior$/, '')
    .replace(/-posterior$/, '')
  const candidato = `${base}-circunferencial`
  return RAMOS_CIRCUNFERENCIAL[candidato] ? candidato : null
})

// ── Navegação hierárquica ─────────────────────────────────────────────────────
const navegacao = ref<ExameFisicoRegiao[]>([])

// ── Estado de seleção ─────────────────────────────────────────────────────────
const idsSelecionados = ref<string[]>([])
const lateralidades = reactive<Record<string, 'D' | 'E' | 'bilateral'>>({})

const totalSelecionados = computed(() => idsSelecionados.value.length)

// Título sem "direito/esquerdo" para membros
const dialogTitle = computed(() => {
  if (!props.membroRegioes || !props.regiaoClicada) return 'Selecionar região'
  return props.regiaoClicada.nome.replace(/\s*(direito|esquerdo)\s+/i, ' ')
})

// Para não-membro no modo circunferencial: precisa da regiaoClicada para achar o id
const idCircunferencialNaoMembro = computed<string | null>(() => {
  if (props.membroRegioes) return null
  if (!props.regiaoClicada) return null
  const base = props.regiaoClicada.id
    .replace(/-anterior$/, '')
    .replace(/-posterior$/, '')
  const candidato = `${base}-circunferencial`
  return RAMOS_CIRCUNFERENCIAL[candidato] ? candidato : null
})

/**
 * No passo de sub-regiões, a região base para navegação depende da vista.
 * No modo circunferencial, não há "região raiz" única — exibimos lista agrupada.
 */
const regiaoAtual = computed(() => {
  if (navegacao.value.length > 0) {
    return navegacao.value[navegacao.value.length - 1]
  }
  if (vistaEscolhida.value === 'circunferencial') return null
  return baseAtiva.value
})

/**
 * Filhos para o modo não-circunferencial — igual ao comportamento original.
 */
const filhosAtuais = computed(() => {
  if (vistaEscolhida.value === 'circunferencial') return []
  if (!regiaoAtual.value) return []
  // No modo anterior/posterior de membro, filtra filhos da vista correta
  if (props.membroRegioes && vistaEscolhida.value && navegacao.value.length === 0) {
    // base ativa já é o nó correto da vista (ex.: msd-anterior ou msd-posterior)
    return props.getFilhos(regiaoAtual.value.id)
  }
  return props.getFilhos(regiaoAtual.value.id)
})

/**
 * Modo "lista agrupada por parte do tronco" (DP-3).
 * Ativo quando troncoGrupos está presente e passo === 'subregioes'.
 * Retorna array de { label, filhos } — um por parte do tronco.
 */
const filhosTroncoGrupos = computed<Array<{ label: string; filhos: ExameFisicoRegiao[] }>>(() => {
  if (!props.troncoGrupos || vistaEscolhida.value === 'circunferencial') return []
  return props.troncoGrupos.grupos.map(g => ({
    label: g.label,
    filhos: props.getFilhos(g.regiaoBaseId),
  }))
})

/**
 * Agrupamento para o modo circunferencial.
 * Retorna { anterior: ExameFisicoRegiao[], posterior: ExameFisicoRegiao[] }.
 *
 * Caso especial do tronco (troncoGrupos presente): agrega filhos de todas as partes
 * anteriores num grupo e todas as posteriores noutro (usando os RAMOS_CIRCUNFERENCIAL de cada parte).
 */
const filhosCircunferencial = computed<{ anterior: ExameFisicoRegiao[]; posterior: ExameFisicoRegiao[] }>(() => {
  if (vistaEscolhida.value !== 'circunferencial') return { anterior: [], posterior: [] }

  // Modo tronco circunferencial: agrega filhos de todas as partes por lado.
  if (props.troncoGrupos) {
    const anterior: ExameFisicoRegiao[] = []
    const posterior: ExameFisicoRegiao[] = []
    for (const grupo of props.troncoGrupos.grupos) {
      // Deriva o nó circunferencial da parte (ex.: 'torax-anterior' → 'torax-circunferencial')
      const base = grupo.regiaoBaseId.replace(/-anterior$/, '').replace(/-posterior$/, '')
      const idCircParte = `${base}-circunferencial`
      const ramos = RAMOS_CIRCUNFERENCIAL[idCircParte]
      if (ramos) {
        anterior.push(...props.getFilhos(ramos.anterior))
        posterior.push(...props.getFilhos(ramos.posterior))
      }
    }
    return { anterior, posterior }
  }

  const idCirc = props.membroRegioes ? idCircunferencial.value : idCircunferencialNaoMembro.value
  if (!idCirc) return { anterior: [], posterior: [] }
  const ramos = RAMOS_CIRCUNFERENCIAL[idCirc]
  return {
    anterior: props.getFilhos(ramos.anterior),
    posterior: props.getFilhos(ramos.posterior),
  }
})

// Breadcrumb usa baseAtiva como raiz (não regiaoClicada fixa)
const breadcrumb = computed(() => {
  const itens: ExameFisicoRegiao[] = []
  const raiz = props.membroRegioes ? baseAtiva.value : props.regiaoClicada
  if (raiz && vistaEscolhida.value !== 'circunferencial') itens.push(raiz)
  itens.push(...navegacao.value)
  return itens
})

// Label do lado escolhido para badge no passo de sub-regiões
const labelLado = computed(() => {
  if (!ladoEscolhido.value) return ''
  if (ladoEscolhido.value === 'D') return 'Direito'
  if (ladoEscolhido.value === 'E') return 'Esquerdo'
  return 'Ambos (bilateral)'
})

const labelVista = computed(() => {
  if (!vistaEscolhida.value) return ''
  if (vistaEscolhida.value === 'anterior') return 'Anterior'
  if (vistaEscolhida.value === 'posterior') return 'Posterior'
  return 'Circunferencial'
})

function jaFoiSelecionada(regiaoId: string): boolean {
  return props.regioesJaSelecionadas.includes(regiaoId)
}

function temFilhos(regiaoId: string): boolean {
  return props.getFilhos(regiaoId).length > 0
}

function estaSelecionado(regiaoId: string): boolean {
  return idsSelecionados.value.includes(regiaoId)
}

function toggleRegiao(regiao: ExameFisicoRegiao) {
  const idx = idsSelecionados.value.indexOf(regiao.id)
  if (idx >= 0) {
    idsSelecionados.value.splice(idx, 1)
    delete lateralidades[regiao.id]
  } else {
    idsSelecionados.value.push(regiao.id)
  }
}

function setLateralidade(regiaoId: string, lat: 'D' | 'E' | 'bilateral') {
  lateralidades[regiaoId] = lat
}

function navegarPara(regiao: ExameFisicoRegiao) {
  navegacao.value.push(regiao)
}

function voltarNivel() {
  if (navegacao.value.length > 0) {
    navegacao.value.pop()
    return
  }
  // No raiz do passo de sub-regiões: Voltar → passo de vista
  limparSelecao()
  passo.value = 'vista'
  vistaEscolhida.value = null
}

function voltarParaNivel(index: number) {
  navegacao.value.splice(index)
}

// Avança do passo de lado para o passo de vista ao escolher o lado
function escolherLado(lado: LadoMembro) {
  ladoEscolhido.value = lado
  limparSelecao()
  passo.value = 'vista'
}

// Avança do passo de vista para sub-regiões ao escolher a vista (CA26)
function escolherVista(vista: VistaPasso) {
  vistaEscolhida.value = vista

  if (vista === 'anterior' || vista === 'posterior') {
    // Troca a baseAtiva para a vista correta (membro)
    // Para não-membro, regiaoClicada já é a base e getFilhos retorna conforme
    limparSelecao()
    passo.value = 'subregioes'
  } else {
    // circunferencial
    limparSelecao()
    passo.value = 'subregioes'
  }
}

// Exibe nome do filho sem sufixo "direito" quando no contexto de membro
function nomeExibido(filho: ExameFisicoRegiao): string {
  if (!props.membroRegioes) return filho.nome
  return filho.nome.replace(/\s+direito\b/gi, '').replace(/\s+D\b/, '').trim()
}

function confirmar() {
  const resultado: Array<{ regiaoId: string; lateralidade: 'D' | 'E' | 'bilateral' | null; vista: VistaPasso | null }> = []

  for (const regiaoId of idsSelecionados.value) {
    if (props.membroRegioes && ladoEscolhido.value) {
      // Fluxo de membro: lateralidade derivada do lado escolhido no passo 1
      resultado.push({ regiaoId, lateralidade: ladoEscolhido.value, vista: vistaEscolhida.value })
    } else {
      // Fluxo não-lateral: lateralidade por sub-região (comportamento original)
      const lat = (lateralidades[regiaoId] ?? null) as 'D' | 'E' | 'bilateral' | null
      resultado.push({ regiaoId, lateralidade: lat, vista: vistaEscolhida.value })
    }
  }

  emit('confirmar', resultado)
  fechar()
}

function limparSelecao() {
  navegacao.value = []
  idsSelecionados.value = []
  Object.keys(lateralidades).forEach((k) => delete lateralidades[k])
}

function fechar() {
  limparSelecao()
  passo.value = props.membroRegioes ? 'lado' : 'vista'
  ladoEscolhido.value = null
  // Reseta para vistaInicial (não para null) — garante que reabrir o popup
  // ainda pré-seleciona a vista conforme o lado clicado no mapa (M3 híbrido).
  vistaEscolhida.value = props.vistaInicial ?? null
  emit('update:aberto', false)
}
</script>

<template>
  <AppModal
    :aberto="aberto"
    :titulo="dialogTitle"
    largura="sm"
    @fechar="fechar"
  >
    <template #default>
      <!-- ── PASSO 1 (membro): Escolha do lado ──────────────────────────────── -->
      <template v-if="membroRegioes && passo === 'lado'">
        <p class="text-xs text-muted-foreground mb-3">
          Selecione o lado a examinar:
        </p>
        <div class="flex justify-center">
          <AppPillToggle
            model-value=""
            :opcoes="OPCOES_LADO"
            @update:model-value="escolherLado($event as LadoMembro)"
          />
        </div>
      </template>

      <!-- ── PASSO: Escolha da vista (membro após lado, ou não-membro no início) ── -->
      <template v-else-if="passo === 'vista'">
        <!-- Badge do lado escolhido (somente membros) -->
        <div
          v-if="membroRegioes && ladoEscolhido"
          class="flex items-center gap-1.5 -mt-1 mb-3"
        >
          <span class="text-[10px] text-muted-foreground">Lado:</span>
          <span class="text-[10px] border border-border rounded px-1.5 py-0.5 text-foreground font-medium">
            {{ labelLado }}
          </span>
        </div>
        <p class="text-xs text-muted-foreground mb-3">
          Selecione a vista a examinar:
        </p>
        <div class="flex justify-center">
          <AppPillToggle
            model-value=""
            :opcoes="OPCOES_VISTA"
            @update:model-value="escolherVista($event as VistaPasso)"
          />
        </div>
      </template>

      <!-- ── PASSO: Seleção de sub-regiões ─────────────────────────────────── -->
      <template v-else>
        <!-- Badges de lado + vista no passo de sub-regiões -->
        <div class="flex items-center gap-1.5 -mt-1 mb-1 flex-wrap">
          <template v-if="membroRegioes && ladoEscolhido">
            <span class="text-[10px] text-muted-foreground">Lado:</span>
            <span class="text-[10px] border border-border rounded px-1.5 py-0.5 text-foreground font-medium">
              {{ labelLado }}
            </span>
          </template>
          <span class="text-[10px] text-muted-foreground" :class="{ 'ml-1': membroRegioes && ladoEscolhido }">Vista:</span>
          <span class="text-[10px] border border-border rounded px-1.5 py-0.5 text-foreground font-medium">
            {{ labelVista }}
          </span>
        </div>

        <!-- Breadcrumb de navegação hierárquica (regiões não-laterais, modo não-circ) -->
        <div
          v-if="breadcrumb.length > 0 && !membroRegioes && vistaEscolhida !== 'circunferencial'"
          class="flex items-center gap-1 text-xs text-muted-foreground flex-wrap -mt-1"
        >
          <template v-for="(item, idx) in breadcrumb" :key="item.id">
            <button
              type="button"
              class="hover:text-foreground transition-colors border-0"
              :class="idx === breadcrumb.length - 1 ? 'font-semibold text-foreground' : ''"
              @click="idx < breadcrumb.length - 1 ? voltarParaNivel(idx) : undefined"
            >
              {{ item.nome }}
            </button>
            <i
              v-if="idx < breadcrumb.length - 1"
              class="fa-solid fa-chevron-right text-[8px] text-muted-foreground/50"
            />
          </template>
        </div>

        <div class="border-t border-border" />

        <!-- ── Modo circunferencial: lista agrupada Anterior + Posterior ──── -->
        <template v-if="vistaEscolhida === 'circunferencial'">
          <div class="space-y-1 max-h-[400px] overflow-y-auto">
            <!-- Grupo Anterior -->
            <template v-if="filhosCircunferencial.anterior.length > 0">
              <p class="text-[10px] font-semibold text-muted-foreground uppercase tracking-wide px-2 pt-2">
                Anterior
              </p>
              <div
                v-for="filho in filhosCircunferencial.anterior"
                :key="filho.id"
                class="flex items-center gap-2 p-2 rounded-md transition-colors"
                :class="!jaFoiSelecionada(filho.id) ? 'hover:bg-muted/50 cursor-pointer' : ''"
                @click="!jaFoiSelecionada(filho.id) && toggleRegiao(filho)"
              >
                <input
                  v-if="!jaFoiSelecionada(filho.id)"
                  type="checkbox"
                  :checked="estaSelecionado(filho.id)"
                  class="h-4 w-4 rounded border-input text-primary focus:ring-primary shrink-0 cursor-pointer"
                  @click.stop
                  @change="toggleRegiao(filho)"
                />
                <i
                  v-else
                  class="fa-solid fa-check text-[10px] text-success w-4 text-center shrink-0"
                />
                <span
                  class="text-xs flex-1 select-none"
                  :class="{ 'text-muted-foreground': jaFoiSelecionada(filho.id) }"
                >
                  {{ nomeExibido(filho) }}
                </span>
                <span
                  v-if="jaFoiSelecionada(filho.id)"
                  class="text-[9px] border border-border rounded px-1.5 py-0.5 text-muted-foreground"
                >
                  Selecionado
                </span>
              </div>
            </template>

            <div v-if="filhosCircunferencial.anterior.length > 0 && filhosCircunferencial.posterior.length > 0" class="border-t border-border" />

            <!-- Grupo Posterior -->
            <template v-if="filhosCircunferencial.posterior.length > 0">
              <p class="text-[10px] font-semibold text-muted-foreground uppercase tracking-wide px-2 pt-2">
                Posterior
              </p>
              <div
                v-for="filho in filhosCircunferencial.posterior"
                :key="filho.id"
                class="flex items-center gap-2 p-2 rounded-md transition-colors"
                :class="!jaFoiSelecionada(filho.id) ? 'hover:bg-muted/50 cursor-pointer' : ''"
                @click="!jaFoiSelecionada(filho.id) && toggleRegiao(filho)"
              >
                <input
                  v-if="!jaFoiSelecionada(filho.id)"
                  type="checkbox"
                  :checked="estaSelecionado(filho.id)"
                  class="h-4 w-4 rounded border-input text-primary focus:ring-primary shrink-0 cursor-pointer"
                  @click.stop
                  @change="toggleRegiao(filho)"
                />
                <i
                  v-else
                  class="fa-solid fa-check text-[10px] text-success w-4 text-center shrink-0"
                />
                <span
                  class="text-xs flex-1 select-none"
                  :class="{ 'text-muted-foreground': jaFoiSelecionada(filho.id) }"
                >
                  {{ nomeExibido(filho) }}
                </span>
                <span
                  v-if="jaFoiSelecionada(filho.id)"
                  class="text-[9px] border border-border rounded px-1.5 py-0.5 text-muted-foreground"
                >
                  Selecionado
                </span>
              </div>
            </template>

            <p
              v-if="filhosCircunferencial.anterior.length === 0 && filhosCircunferencial.posterior.length === 0"
              class="text-xs text-muted-foreground text-center py-4"
            >
              Nenhuma sub-região disponível.
            </p>
          </div>
        </template>

        <!-- ── Modo tronco: lista agrupada por parte (DP-3) ───────────────── -->
        <!-- vistaEscolhida !== 'circunferencial' já garantida pelo v-if anterior -->
        <template v-else-if="troncoGrupos">
          <div class="space-y-1 max-h-[400px] overflow-y-auto">
            <template v-for="(grupo, gi) in filhosTroncoGrupos" :key="grupo.label">
              <div v-if="gi > 0" class="border-t border-border" />
              <p class="text-[10px] font-semibold text-muted-foreground uppercase tracking-wide px-2 pt-2">
                {{ grupo.label }}
              </p>
              <div
                v-for="filho in grupo.filhos"
                :key="filho.id"
                class="flex items-center gap-2 p-2 rounded-md transition-colors"
                :class="!jaFoiSelecionada(filho.id) ? 'hover:bg-muted/50 cursor-pointer' : ''"
                @click="!jaFoiSelecionada(filho.id) && toggleRegiao(filho)"
              >
                <input
                  v-if="!jaFoiSelecionada(filho.id)"
                  type="checkbox"
                  :checked="estaSelecionado(filho.id)"
                  class="h-4 w-4 rounded border-input text-primary focus:ring-primary shrink-0 cursor-pointer"
                  @click.stop
                  @change="toggleRegiao(filho)"
                />
                <i
                  v-else
                  class="fa-solid fa-check text-[10px] text-success w-4 text-center shrink-0"
                />
                <span
                  class="text-xs flex-1 select-none"
                  :class="{ 'text-muted-foreground': jaFoiSelecionada(filho.id) }"
                >
                  {{ filho.nome }}
                </span>
                <span
                  v-if="jaFoiSelecionada(filho.id)"
                  class="text-[9px] border border-border rounded px-1.5 py-0.5 text-muted-foreground"
                >
                  Selecionado
                </span>
              </div>
            </template>
            <p
              v-if="filhosTroncoGrupos.every(g => g.filhos.length === 0)"
              class="text-xs text-muted-foreground text-center py-4"
            >
              Nenhuma sub-região disponível.
            </p>
          </div>
        </template>

        <!-- ── Modo anterior/posterior: lista normal ────────────────────── -->
        <template v-else>
          <div class="space-y-1 max-h-[400px] overflow-y-auto">
            <!-- Opção de selecionar a região atual (geral) -->
            <div
              v-if="regiaoAtual && !jaFoiSelecionada(regiaoAtual.id)"
              class="flex items-center gap-2 p-2 rounded-md hover:bg-muted/50 transition-colors cursor-pointer"
              @click="toggleRegiao(regiaoAtual)"
            >
              <input
                type="checkbox"
                :checked="estaSelecionado(regiaoAtual.id)"
                class="h-4 w-4 rounded border-input text-primary focus:ring-primary shrink-0 cursor-pointer"
                @click.stop
                @change="toggleRegiao(regiaoAtual)"
              />
              <span class="text-xs font-medium flex-1 select-none">
                {{ membroRegioes ? dialogTitle : regiaoAtual.nome }} (geral)
              </span>

              <!-- D/E/Bilateral por sub-região: apenas em regiões não-laterais (R3) -->
              <div
                v-if="!membroRegioes && regiaoAtual.lateralidade && estaSelecionado(regiaoAtual.id)"
                class="flex items-center gap-1"
                @click.stop
              >
                <button
                  type="button"
                  class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                  :class="lateralidades[regiaoAtual.id] === 'D'
                    ? 'bg-primary text-primary-foreground border-primary'
                    : 'border-input text-foreground hover:bg-muted'"
                  @click="setLateralidade(regiaoAtual.id, 'D')"
                >D</button>
                <button
                  type="button"
                  class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                  :class="lateralidades[regiaoAtual.id] === 'E'
                    ? 'bg-primary text-primary-foreground border-primary'
                    : 'border-input text-foreground hover:bg-muted'"
                  @click="setLateralidade(regiaoAtual.id, 'E')"
                >E</button>
                <button
                  type="button"
                  class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                  :class="lateralidades[regiaoAtual.id] === 'bilateral'
                    ? 'bg-primary text-primary-foreground border-primary'
                    : 'border-input text-foreground hover:bg-muted'"
                  @click="setLateralidade(regiaoAtual.id, 'bilateral')"
                >Bilateral</button>
              </div>
            </div>

            <div v-if="filhosAtuais.length > 0" class="border-t border-border" />

            <!-- Sub-regiões -->
            <div
              v-for="filho in filhosAtuais"
              :key="filho.id"
              class="flex items-center gap-2 p-2 rounded-md transition-colors"
              :class="!jaFoiSelecionada(filho.id) ? 'hover:bg-muted/50 cursor-pointer' : ''"
              @click="!jaFoiSelecionada(filho.id) && toggleRegiao(filho)"
            >
              <!-- Checkbox se não foi selecionado antes -->
              <input
                v-if="!jaFoiSelecionada(filho.id)"
                type="checkbox"
                :checked="estaSelecionado(filho.id)"
                class="h-4 w-4 rounded border-input text-primary focus:ring-primary shrink-0 cursor-pointer"
                @click.stop
                @change="toggleRegiao(filho)"
              />
              <i
                v-else
                class="fa-solid fa-check text-[10px] text-success w-4 text-center shrink-0"
              />

              <span
                class="text-xs flex-1 select-none"
                :class="{ 'text-muted-foreground': jaFoiSelecionada(filho.id) }"
              >
                {{ nomeExibido(filho) }}
              </span>

              <!-- D/E/Bilateral: apenas em regiões não-laterais (R3) -->
              <div
                v-if="!membroRegioes && filho.lateralidade && estaSelecionado(filho.id)"
                class="flex items-center gap-1"
                @click.stop
              >
                <button
                  type="button"
                  class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                  :class="lateralidades[filho.id] === 'D'
                    ? 'bg-primary text-primary-foreground border-primary'
                    : 'border-input text-foreground hover:bg-muted'"
                  @click="setLateralidade(filho.id, 'D')"
                >D</button>
                <button
                  type="button"
                  class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                  :class="lateralidades[filho.id] === 'E'
                    ? 'bg-primary text-primary-foreground border-primary'
                    : 'border-input text-foreground hover:bg-muted'"
                  @click="setLateralidade(filho.id, 'E')"
                >E</button>
                <button
                  type="button"
                  class="px-1.5 py-0.5 text-[9px] h-auto rounded border transition-colors"
                  :class="lateralidades[filho.id] === 'bilateral'
                    ? 'bg-primary text-primary-foreground border-primary'
                    : 'border-input text-foreground hover:bg-muted'"
                  @click="setLateralidade(filho.id, 'bilateral')"
                >Bilateral</button>
              </div>

              <!-- Botão drill-down -->
              <button
                v-if="temFilhos(filho.id)"
                type="button"
                class="text-[10px] text-primary hover:text-primary/80 p-1 h-auto rounded border-0"
                title="Ver sub-regiões"
                @click.stop="navegarPara(filho)"
              >
                <i class="fa-solid fa-chevron-right" />
              </button>

              <span
                v-if="jaFoiSelecionada(filho.id)"
                class="text-[9px] border border-border rounded px-1.5 py-0.5 text-muted-foreground"
              >
                Selecionado
              </span>
            </div>

            <p
              v-if="filhosAtuais.length === 0 && regiaoAtual"
              class="text-xs text-muted-foreground text-center py-4"
            >
              Nenhuma sub-região disponível.
            </p>
          </div>
        </template>
      </template>
    </template>

    <template #rodape>
      <div class="flex items-center gap-2 w-full">
        <!-- Passo de lado (membro): só Cancelar -->
        <template v-if="membroRegioes && passo === 'lado'">
          <div class="flex-1" />
          <AppButton variant="ghost" size="sm" @click="fechar">
            Cancelar
          </AppButton>
        </template>

        <!-- Passo de vista: Voltar (se membro) + Cancelar -->
        <template v-else-if="passo === 'vista'">
          <AppButton
            v-if="membroRegioes"
            variant="ghost"
            size="sm"
            @click="passo = 'lado'; vistaEscolhida = null"
          >
            <i class="fa-solid fa-arrow-left mr-1 text-[10px]" />
            Voltar
          </AppButton>
          <div class="flex-1" />
          <AppButton variant="ghost" size="sm" @click="fechar">
            Cancelar
          </AppButton>
        </template>

        <!-- Passo de sub-regiões de membro: Voltar + Cancelar + Confirmar -->
        <template v-else-if="membroRegioes && passo === 'subregioes'">
          <AppButton variant="ghost" size="sm" @click="voltarNivel">
            <i class="fa-solid fa-arrow-left mr-1 text-[10px]" />
            Voltar
          </AppButton>
          <div class="flex-1" />
          <AppButton variant="ghost" size="sm" @click="fechar">
            Cancelar
          </AppButton>
          <AppButton
            size="sm"
            :disabled="totalSelecionados === 0"
            @click="confirmar"
          >
            Confirmar ({{ totalSelecionados }})
          </AppButton>
        </template>

        <!-- Região não-lateral passo subregioes: comportamento original -->
        <template v-else>
          <AppButton
            v-if="navegacao.length > 0"
            variant="ghost"
            size="sm"
            @click="voltarNivel"
          >
            <i class="fa-solid fa-arrow-left mr-1 text-[10px]" />
            Voltar
          </AppButton>
          <AppButton
            v-else
            variant="ghost"
            size="sm"
            @click="passo = 'vista'; vistaEscolhida = null; limparSelecao()"
          >
            <i class="fa-solid fa-arrow-left mr-1 text-[10px]" />
            Voltar
          </AppButton>
          <div class="flex-1" />
          <AppButton variant="ghost" size="sm" @click="fechar">
            Cancelar
          </AppButton>
          <AppButton
            size="sm"
            :disabled="totalSelecionados === 0"
            @click="confirmar"
          >
            Confirmar ({{ totalSelecionados }})
          </AppButton>
        </template>
      </div>
    </template>
  </AppModal>
</template>
