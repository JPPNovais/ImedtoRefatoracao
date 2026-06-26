<script setup lang="ts">
import { ref, computed, reactive, watch } from 'vue'
import { AppModal } from '@/components/ui'
import { AppButton } from '@/components/ui'
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
 * TroncoGrupos mantido por compatibilidade de import (SecaoExameFisico.vue legado).
 * O modo agrupado de tronco foi removido na fusão estrutural (briefing 2026-06-25_002).
 * @deprecated Remover quando todos os importadores forem atualizados.
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
 * Dado o nó base ativo (ex.: "membro-superior-direito-anterior"), resolve qual nó
 * circunferencial corresponde (ex.: "membro-superior-direito-circunferencial").
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
    // base ativa já é o nó correto da vista (ex.: membro-superior-direito-anterior ou membro-superior-direito-posterior)
    return props.getFilhos(regiaoAtual.value.id)
  }
  return props.getFilhos(regiaoAtual.value.id)
})

/**
 * Agrupamento para o modo circunferencial.
 * Retorna { anterior: ExameFisicoRegiao[], posterior: ExameFisicoRegiao[] }.
 *
 * Fusão estrutural do tronco (briefing 2026-06-25_002): o caso especial de
 * troncoGrupos (Tórax/Abdome/Pelve) foi removido. O tronco agora funciona como
 * qualquer outra região — seus ramos vêm de RAMOS_CIRCUNFERENCIAL['tronco-circunferencial'].
 */
const filhosCircunferencial = computed<{ anterior: ExameFisicoRegiao[]; posterior: ExameFisicoRegiao[] }>(() => {
  if (vistaEscolhida.value !== 'circunferencial') return { anterior: [], posterior: [] }

  const idCirc = props.membroRegioes ? idCircunferencial.value : idCircunferencialNaoMembro.value
  if (!idCirc) return { anterior: [], posterior: [] }
  const ramos = RAMOS_CIRCUNFERENCIAL[idCirc]
  return {
    anterior: props.getFilhos(ramos.anterior),
    posterior: props.getFilhos(ramos.posterior),
  }
})

/**
 * Lista única do modo circunferencial: une as sub-regiões do ramo anterior e do
 * posterior numa lista só, deduplicando por NOME — o que for igual nos dois ramos
 * aparece 1 vez (mantém a 1ª ocorrência: anterior antes de posterior).
 */
const filhosCircunferencialUnificado = computed<ExameFisicoRegiao[]>(() => {
  const { anterior, posterior } = filhosCircunferencial.value
  const vistos = new Set<string>()
  const unico: ExameFisicoRegiao[] = []
  for (const filho of [...anterior, ...posterior]) {
    const chave = filho.nome.trim().toLowerCase()
    if (vistos.has(chave)) continue
    vistos.add(chave)
    unico.push(filho)
  }
  return unico
})

/**
 * Addendum-002 (CA31–CA40): nó "(geral)" do modo circunferencial.
 * Inclui o tronco (brienfing 2026-06-25_002): tronco-circunferencial agora
 * funciona exatamente como cabeça/pescoço/membro.
 * Usa idCircunferencial (membro) ou idCircunferencialNaoMembro (não-membro/tronco).
 * Retorna null quando o nó não existe no catálogo (guard R15/CA36 → no-op).
 */
const regiaoGeralCircunferencial = computed<ExameFisicoRegiao | null>(() => {
  const idCirc = props.membroRegioes ? idCircunferencial.value : idCircunferencialNaoMembro.value
  if (!idCirc) return null
  return props.regioes.find(r => r.id === idCirc) ?? null
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
    largura="sm"
    @fechar="fechar"
  >
    <!-- Título customizado: bold, cor primary-dark -->
    <template #titulo>
      <span class="rsp-modal-title">{{ dialogTitle }}</span>
    </template>

    <template #default>
      <!-- ── PASSO 1 (membro): Escolha do lado ──────────────────────────────── -->
      <template v-if="membroRegioes && passo === 'lado'">
        <p class="rsp-step-label">Defina como esta região foi examinada.</p>

        <!-- Segmented "LADO" -->
        <div class="rsp-field-block">
          <div class="rsp-fl">Lado</div>
          <div class="rsp-seg">
            <button
              v-for="op in OPCOES_LADO"
              :key="op.valor"
              type="button"
              class="rsp-seg-btn"
              :class="{ 'rsp-seg-btn--on': ladoEscolhido === op.valor }"
              @click="escolherLado(op.valor)"
            >
              {{ op.label }}
            </button>
          </div>
        </div>
      </template>

      <!-- ── PASSO: Escolha da vista (membro após lado, ou não-membro no início) ── -->
      <template v-else-if="passo === 'vista'">
        <p class="rsp-step-label">Defina como esta região foi examinada.</p>

        <!-- Badge do lado (somente membro) -->
        <div v-if="membroRegioes && ladoEscolhido" class="rsp-field-block">
          <div class="rsp-fl">Lado</div>
          <div class="rsp-seg">
            <button
              v-for="op in OPCOES_LADO"
              :key="op.valor"
              type="button"
              class="rsp-seg-btn"
              :class="{ 'rsp-seg-btn--on': ladoEscolhido === op.valor }"
              @click="escolherLado(op.valor)"
            >
              {{ op.label }}
            </button>
          </div>
        </div>

        <!-- Segmented "PLANO DE EXAME" com cor por vista -->
        <div class="rsp-field-block">
          <div class="rsp-fl">Plano de exame</div>
          <div class="rsp-seg rsp-seg--plano">
            <button
              v-for="op in OPCOES_VISTA"
              :key="op.valor"
              type="button"
              class="rsp-seg-btn"
              :class="[
                { 'rsp-seg-btn--on': vistaEscolhida === op.valor },
                vistaEscolhida === op.valor ? `rsp-seg-btn--vista-${op.valor}` : '',
              ]"
              :data-v="op.valor"
              @click="escolherVista(op.valor)"
            >
              {{ op.label }}
            </button>
          </div>
          <!-- Texto auxiliar do plano -->
          <p v-if="vistaEscolhida" class="rsp-plano-help">
            <template v-if="vistaEscolhida === 'anterior'">Colore a face anterior (frente).</template>
            <template v-else-if="vistaEscolhida === 'posterior'">Colore a face posterior (costas).</template>
            <template v-else>Colore as faces anterior e posterior (circunferencial).</template>
          </p>
        </div>
      </template>

      <!-- ── PASSO: Seleção de sub-regiões ─────────────────────────────────── -->
      <template v-else>
        <!-- Chips de resumo: lado + vista -->
        <div class="rsp-chips">
          <span v-if="membroRegioes && ladoEscolhido" class="rsp-chip">
            <i class="fa-solid fa-arrows-left-right" />
            {{ labelLado }}
          </span>
          <span class="rsp-chip">
            <i :class="vistaEscolhida === 'circunferencial' ? 'fa-solid fa-arrows-rotate' : 'fa-solid fa-location-crosshairs'" />
            {{ labelVista }}
          </span>
        </div>

        <!-- Breadcrumb de navegação hierárquica (regiões não-laterais, modo não-circ) -->
        <div
          v-if="breadcrumb.length > 0 && !membroRegioes && vistaEscolhida !== 'circunferencial'"
          class="rsp-breadcrumb"
        >
          <template v-for="(item, idx) in breadcrumb" :key="item.id">
            <button
              type="button"
              class="rsp-breadcrumb-item"
              :class="{ 'rsp-breadcrumb-item--active': idx === breadcrumb.length - 1 }"
              @click="idx < breadcrumb.length - 1 ? voltarParaNivel(idx) : undefined"
            >
              {{ item.nome }}
            </button>
            <i
              v-if="idx < breadcrumb.length - 1"
              class="fa-solid fa-chevron-right rsp-breadcrumb-sep"
            />
          </template>
        </div>

        <div class="rsp-divider" />

        <!-- ── Modo circunferencial: lista agrupada Anterior + Posterior ──── -->
        <template v-if="vistaEscolhida === 'circunferencial'">
          <div class="rsp-sub-list">

            <!--
              Addendum-002 CA31/CA32 — opção "(geral)" no topo do circunferencial,
              apenas para região simples (não-tronco). Espelha a semântica do "geral"
              do anterior/posterior mas usando o nó <base>-circunferencial.
              R15/CA36: não renderiza se regiaoGeralCircunferencial for null (nó ausente).
              R17/CA35: já-selecionado → disabled (badge "Selecionado", sem checkbox).
              R16/CA37: lateralidade espelha o modo circunferencial atual (sem botões D/E novos).
              TRONCO: troncoGrupos → regiaoGeralCircunferencial é null (guard).
            -->
            <template v-if="regiaoGeralCircunferencial">
              <!-- Estado: já selecionado/disabled (R17/CA35) -->
              <div
                v-if="jaFoiSelecionada(regiaoGeralCircunferencial.id)"
                class="rsp-opt rsp-opt--geral rsp-opt--disabled"
              >
                <span class="rsp-opt-box">
                  <i class="fa-solid fa-check" />
                </span>
                <span class="rsp-opt-lbl rsp-opt-lbl--muted">
                  {{ membroRegioes ? dialogTitle : regiaoGeralCircunferencial.nome }} (geral)
                </span>
                <span class="rsp-badge-sel">Selecionado</span>
              </div>

              <!-- Estado: disponível para seleção -->
              <label
                v-else
                class="rsp-opt rsp-opt--geral"
              >
                <input
                  type="checkbox"
                  class="rsp-opt-input"
                  :checked="estaSelecionado(regiaoGeralCircunferencial.id)"
                  @change="toggleRegiao(regiaoGeralCircunferencial)"
                />
                <span class="rsp-opt-box">
                  <i v-if="estaSelecionado(regiaoGeralCircunferencial.id)" class="fa-solid fa-check" />
                </span>
                <span class="rsp-opt-lbl">
                  {{ membroRegioes ? dialogTitle : regiaoGeralCircunferencial.nome }} (geral)
                </span>
              </label>

              <div class="rsp-divider" />
            </template>

            <!-- Lista única: anterior + posterior unificados e deduplicados por nome. -->
            <label
              v-for="filho in filhosCircunferencialUnificado"
              :key="filho.id"
              class="rsp-opt"
              :class="{ 'rsp-opt--disabled': jaFoiSelecionada(filho.id) }"
            >
              <input
                v-if="!jaFoiSelecionada(filho.id)"
                type="checkbox"
                class="rsp-opt-input"
                :checked="estaSelecionado(filho.id)"
                @change="toggleRegiao(filho)"
              />
              <span class="rsp-opt-box">
                <i v-if="estaSelecionado(filho.id) || jaFoiSelecionada(filho.id)" class="fa-solid fa-check" />
              </span>
              <span class="rsp-opt-lbl" :class="{ 'rsp-opt-lbl--muted': jaFoiSelecionada(filho.id) }">
                {{ nomeExibido(filho) }}
              </span>
              <span v-if="jaFoiSelecionada(filho.id)" class="rsp-badge-sel">Selecionado</span>
            </label>

            <p
              v-if="filhosCircunferencialUnificado.length === 0"
              class="rsp-vazio"
            >
              Nenhuma sub-região disponível.
            </p>
          </div>
        </template>

        <!-- ── Modo anterior/posterior: lista normal ────────────────────── -->
        <!-- Tronco agora entra aqui como região normal (briefing 2026-06-25_002). -->
        <template v-else>
          <div class="rsp-sub-list">
            <!-- Opção "geral" -->
            <label
              v-if="regiaoAtual && !jaFoiSelecionada(regiaoAtual.id)"
              class="rsp-opt rsp-opt--geral"
            >
              <input
                type="checkbox"
                class="rsp-opt-input"
                :checked="estaSelecionado(regiaoAtual.id)"
                @change="toggleRegiao(regiaoAtual)"
              />
              <span class="rsp-opt-box">
                <i v-if="estaSelecionado(regiaoAtual.id)" class="fa-solid fa-check" />
              </span>
              <span class="rsp-opt-lbl">
                {{ membroRegioes ? dialogTitle : regiaoAtual.nome }} (geral)
              </span>

              <!-- D/E/Bilateral por sub-região: apenas em regiões não-laterais (R3) -->
              <div
                v-if="!membroRegioes && regiaoAtual.lateralidade && estaSelecionado(regiaoAtual.id)"
                class="rsp-lat-btns"
                @click.stop
              >
                <button type="button" class="rsp-lat-btn"
                  :class="{ 'rsp-lat-btn--on': lateralidades[regiaoAtual.id] === 'D' }"
                  @click="setLateralidade(regiaoAtual.id, 'D')">D</button>
                <button type="button" class="rsp-lat-btn"
                  :class="{ 'rsp-lat-btn--on': lateralidades[regiaoAtual.id] === 'E' }"
                  @click="setLateralidade(regiaoAtual.id, 'E')">E</button>
                <button type="button" class="rsp-lat-btn"
                  :class="{ 'rsp-lat-btn--on': lateralidades[regiaoAtual.id] === 'bilateral' }"
                  @click="setLateralidade(regiaoAtual.id, 'bilateral')">Bilateral</button>
              </div>
            </label>

            <div v-if="filhosAtuais.length > 0" class="rsp-divider" />

            <!-- Sub-regiões com hierarquia -->
            <template v-for="filho in filhosAtuais" :key="filho.id">
              <div class="rsp-opt-row">
                <label
                  class="rsp-opt rsp-opt--flex1"
                  :class="{ 'rsp-opt--disabled': jaFoiSelecionada(filho.id) }"
                >
                  <input
                    v-if="!jaFoiSelecionada(filho.id)"
                    type="checkbox"
                    class="rsp-opt-input"
                    :checked="estaSelecionado(filho.id)"
                    @change="toggleRegiao(filho)"
                  />
                  <span class="rsp-opt-box">
                    <i v-if="estaSelecionado(filho.id) || jaFoiSelecionada(filho.id)" class="fa-solid fa-check" />
                  </span>
                  <span class="rsp-opt-lbl" :class="{ 'rsp-opt-lbl--muted': jaFoiSelecionada(filho.id) }">
                    {{ nomeExibido(filho) }}
                  </span>

                  <!-- D/E/Bilateral: apenas em regiões não-laterais (R3) -->
                  <div
                    v-if="!membroRegioes && filho.lateralidade && estaSelecionado(filho.id)"
                    class="rsp-lat-btns"
                    @click.stop
                  >
                    <button type="button" class="rsp-lat-btn"
                      :class="{ 'rsp-lat-btn--on': lateralidades[filho.id] === 'D' }"
                      @click="setLateralidade(filho.id, 'D')">D</button>
                    <button type="button" class="rsp-lat-btn"
                      :class="{ 'rsp-lat-btn--on': lateralidades[filho.id] === 'E' }"
                      @click="setLateralidade(filho.id, 'E')">E</button>
                    <button type="button" class="rsp-lat-btn"
                      :class="{ 'rsp-lat-btn--on': lateralidades[filho.id] === 'bilateral' }"
                      @click="setLateralidade(filho.id, 'bilateral')">Bilateral</button>
                  </div>

                  <span v-if="jaFoiSelecionada(filho.id)" class="rsp-badge-sel">Selecionado</span>
                </label>

                <!-- Botão drill-down / expandir -->
                <button
                  v-if="temFilhos(filho.id)"
                  type="button"
                  class="rsp-opt-exp"
                  title="Ver sub-regiões"
                  @click.stop="navegarPara(filho)"
                >
                  <i class="fa-solid fa-chevron-right" />
                </button>
              </div>
            </template>

            <p v-if="filhosAtuais.length === 0 && regiaoAtual" class="rsp-vazio">
              Nenhuma sub-região disponível.
            </p>
          </div>
        </template>
      </template>
    </template>

    <template #rodape>
      <div class="rsp-footer">
        <!-- Passo de lado (membro): só Cancelar (avançar = clicar no segmented) -->
        <template v-if="membroRegioes && passo === 'lado'">
          <div class="rsp-footer-grow" />
          <AppButton variant="ghost" size="sm" @click="fechar">Cancelar</AppButton>
        </template>

        <!-- Passo de vista: Voltar (se membro) + Cancelar (avançar = clicar no segmented) -->
        <template v-else-if="passo === 'vista'">
          <AppButton
            v-if="membroRegioes"
            variant="ghost"
            size="sm"
            @click="passo = 'lado'; vistaEscolhida = null"
          >
            <i class="fa-solid fa-arrow-left" /> Voltar
          </AppButton>
          <div class="rsp-footer-grow" />
          <AppButton variant="ghost" size="sm" @click="fechar">Cancelar</AppButton>
        </template>

        <!-- Passo de sub-regiões de membro: Voltar + Cancelar + Confirmar -->
        <template v-else-if="membroRegioes && passo === 'subregioes'">
          <AppButton variant="ghost" size="sm" @click="voltarNivel">
            <i class="fa-solid fa-arrow-left" /> Voltar
          </AppButton>
          <div class="rsp-footer-grow" />
          <AppButton variant="ghost" size="sm" @click="fechar">Cancelar</AppButton>
          <AppButton size="sm" :disabled="totalSelecionados === 0" @click="confirmar">
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
            <i class="fa-solid fa-arrow-left" /> Voltar
          </AppButton>
          <AppButton
            v-else
            variant="ghost"
            size="sm"
            @click="passo = 'vista'; vistaEscolhida = null; limparSelecao()"
          >
            <i class="fa-solid fa-arrow-left" /> Voltar
          </AppButton>
          <div class="rsp-footer-grow" />
          <AppButton variant="ghost" size="sm" @click="fechar">Cancelar</AppButton>
          <AppButton size="sm" :disabled="totalSelecionados === 0" @click="confirmar">
            Confirmar ({{ totalSelecionados }})
          </AppButton>
        </template>
      </div>
    </template>
  </AppModal>
</template>

<style scoped>
/* ── Título da modal ─────────────────────────────────────────────────────── */
.rsp-modal-title {
  font-size: var(--text-lg);
  font-weight: var(--font-weight-extrabold);
  color: hsl(var(--primary-dark));
}

/* ── Passo 1/2: label de instrução ──────────────────────────────────────── */
.rsp-step-label {
  font-size: var(--text-sm);
  color: hsl(var(--secondary) / 0.6);
  margin: 0 0 18px;
}

/* ── Bloco de campo (label + segmented) ─────────────────────────────────── */
.rsp-field-block {
  margin-bottom: 20px;
}

/* Label uppercase acima do segmented */
.rsp-fl {
  font-size: var(--text-2xs);
  font-weight: var(--font-weight-extrabold);
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: hsl(var(--secondary) / 0.45);
  margin-bottom: 9px;
}

/* ── Segmented control ───────────────────────────────────────────────────── */
.rsp-seg {
  display: inline-flex;
  background: hsl(var(--secondary) / 0.05);
  padding: 4px;
  border-radius: var(--radius-full);
  gap: 4px;
  flex-wrap: wrap;
}

.rsp-seg-btn {
  border: 0;
  background: transparent;
  font: inherit;
  font-size: var(--text-sm);
  font-weight: var(--font-weight-bold);
  color: hsl(var(--secondary) / 0.62);
  padding: 8px 18px;
  border-radius: var(--radius-full);
  cursor: pointer;
  transition: all 120ms ease;
}

.rsp-seg-btn:hover {
  color: hsl(var(--secondary));
}

/* Estado ativo — segmented de lado: cor primária */
.rsp-seg-btn--on {
  background: hsl(var(--card));
  color: hsl(var(--primary));
  box-shadow: var(--shadow-sm);
}

/* Estado ativo — segmented de plano: cor por vista */
.rsp-seg--plano .rsp-seg-btn--on.rsp-seg-btn--vista-anterior {
  color: hsl(var(--vista-anterior));
}
.rsp-seg--plano .rsp-seg-btn--on.rsp-seg-btn--vista-posterior {
  color: hsl(var(--vista-posterior));
}
.rsp-seg--plano .rsp-seg-btn--on.rsp-seg-btn--vista-circunferencial {
  color: hsl(var(--vista-circ-text));
}

/* Texto auxiliar abaixo do segmented de plano */
.rsp-plano-help {
  font-size: var(--text-sm);
  color: hsl(var(--secondary) / 0.6);
  margin: 10px 0 0;
}

/* ── Chips (passo de sub-regiões) ───────────────────────────────────────── */
.rsp-chips {
  display: flex;
  gap: 8px;
  margin-bottom: 14px;
  flex-wrap: wrap;
}

.rsp-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: var(--text-xs);
  font-weight: var(--font-weight-bold);
  padding: 5px 11px;
  border-radius: var(--radius-full);
  background: hsl(var(--secondary) / 0.06);
  color: hsl(var(--secondary) / 0.72);
}

/* ── Breadcrumb ─────────────────────────────────────────────────────────── */
.rsp-breadcrumb {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-wrap: wrap;
  margin-bottom: 8px;
}

.rsp-breadcrumb-item {
  background: none;
  border: 0;
  padding: 0;
  cursor: pointer;
  font-size: var(--text-xs);
  color: hsl(var(--secondary) / 0.6);
  transition: color 100ms;
}

.rsp-breadcrumb-item:hover {
  color: hsl(var(--secondary));
}

.rsp-breadcrumb-item--active {
  font-weight: var(--font-weight-semibold);
  color: hsl(var(--secondary));
}

.rsp-breadcrumb-sep {
  font-size: var(--text-2xs);
  color: hsl(var(--secondary) / 0.35);
}

/* ── Divisória ──────────────────────────────────────────────────────────── */
.rsp-divider {
  border: 0;
  border-top: 1px solid hsl(var(--border));
  margin: 4px 0;
}

/* ── Lista de sub-regiões ───────────────────────────────────────────────── */
.rsp-sub-list {
  display: flex;
  flex-direction: column;
  max-height: 400px;
  overflow-y: auto;
}

/* Cabeçalho de grupo (Anterior / Posterior) */
.rsp-sub-head {
  font-size: var(--text-2xs);
  font-weight: var(--font-weight-extrabold);
  letter-spacing: 0.07em;
  text-transform: uppercase;
  margin: 14px 0 4px;
  padding-bottom: 6px;
  border-bottom: 1px solid hsl(var(--border));
  display: flex;
  align-items: center;
  gap: 8px;
}

.rsp-sub-head--ant {
  color: hsl(var(--vista-anterior));
}

.rsp-sub-head--post {
  color: hsl(var(--vista-posterior));
}

.rsp-sub-head-dot {
  font-size: var(--text-2xs);
}

/* ── Item de opção (checkbox custom) ────────────────────────────────────── */
.rsp-opt {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 11px 8px;
  border-radius: var(--radius-md);
  cursor: pointer;
  user-select: none;
  transition: background 120ms;
}

.rsp-opt:hover:not(.rsp-opt--disabled) {
  background: hsl(var(--secondary) / 0.04);
}

.rsp-opt--disabled {
  cursor: default;
}

/* Item "geral" */
.rsp-opt--geral .rsp-opt-lbl {
  font-style: italic;
  color: hsl(var(--secondary) / 0.7);
}

/* Linha com drill-down */
.rsp-opt-row {
  display: flex;
  align-items: center;
  gap: 4px;
}

.rsp-opt--flex1 {
  flex: 1;
}

/* Hidden input nativo */
.rsp-opt-input {
  position: absolute;
  opacity: 0;
  width: 0;
  height: 0;
}

/* Caixa visual do checkbox */
.rsp-opt-box {
  width: 20px;
  height: 20px;
  flex: none;
  border-radius: 6px;
  border: 1.5px solid hsl(var(--border));
  display: flex;
  align-items: center;
  justify-content: center;
  color: hsl(var(--primary-foreground));
  font-size: var(--text-2xs);
  transition: all 120ms ease;
  background: transparent;
}

/* Quando o checkbox nativo está marcado, o .rsp-opt-box fica preenchido */
.rsp-opt-input:checked ~ .rsp-opt-box {
  background: hsl(var(--primary));
  border-color: hsl(var(--primary));
}

/* item disabled (já selecionado antes): box preenchida com check */
.rsp-opt--disabled .rsp-opt-box {
  background: hsl(var(--primary) / 0.35);
  border-color: hsl(var(--primary) / 0.35);
}

.rsp-opt-lbl {
  font-size: var(--text-base);
  color: hsl(var(--secondary));
  flex: 1;
}

.rsp-opt-input:checked ~ .rsp-opt-lbl,
.rsp-opt-input:checked ~ * .rsp-opt-lbl {
  font-weight: var(--font-weight-bold);
}

.rsp-opt-lbl--muted {
  color: hsl(var(--secondary) / 0.6);
}

/* Badge "Selecionado" */
.rsp-badge-sel {
  font-size: var(--text-2xs);
  border: 1px solid hsl(var(--border));
  border-radius: var(--radius-full);
  padding: 2px 7px;
  color: hsl(var(--secondary) / 0.6);
  white-space: nowrap;
}

/* Botão drill-down (chevron direito) */
.rsp-opt-exp {
  width: 32px;
  height: 32px;
  flex: none;
  border: 0;
  background: transparent;
  color: hsl(var(--secondary) / 0.42);
  cursor: pointer;
  border-radius: var(--radius-md);
  font-size: var(--text-xs);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 120ms, color 120ms;
}

.rsp-opt-exp:hover {
  background: hsl(var(--secondary) / 0.07);
  color: hsl(var(--secondary));
}

/* ── Botões D/E/Bilateral (regiões não-laterais) ────────────────────────── */
.rsp-lat-btns {
  display: flex;
  align-items: center;
  gap: 4px;
}

.rsp-lat-btn {
  padding: 2px 6px;
  font-size: var(--text-2xs);
  font-weight: var(--font-weight-semibold);
  border-radius: var(--radius-sm);
  border: 1px solid hsl(var(--border));
  background: transparent;
  color: hsl(var(--secondary));
  cursor: pointer;
  transition: all 100ms;
}

.rsp-lat-btn:hover {
  background: hsl(var(--secondary) / 0.06);
}

.rsp-lat-btn--on {
  background: hsl(var(--primary));
  color: hsl(var(--primary-foreground));
  border-color: hsl(var(--primary));
}

/* ── Mensagem vazia ─────────────────────────────────────────────────────── */
.rsp-vazio {
  font-size: var(--text-xs);
  color: hsl(var(--secondary) / 0.5);
  text-align: center;
  padding: 16px 0;
}

/* ── Footer ─────────────────────────────────────────────────────────────── */
.rsp-footer {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.rsp-footer-grow {
  flex: 1;
}
</style>
