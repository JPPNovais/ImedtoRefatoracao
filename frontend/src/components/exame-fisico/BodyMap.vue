<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  maleRegionPaths,
  femaleRegionPaths,
  type BodyRegionPath,
} from './bodyMapPaths'
import { PARTE_PARA_TRONCO } from './regioesCircunferenciais'

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

/**
 * Evento de clique no tronco: pseudo-hotspot sintético (não existe no catálogo).
 * 'tronco-anterior' = usuário clicou no lado anterior do mapa.
 * 'tronco-posterior' = usuário clicou no lado posterior.
 */
export type TroncoClique = 'tronco-anterior' | 'tronco-posterior'

/** Vista resolvida de um hotspot para fins de coloração. */
export type VistaHotspot = 'anterior' | 'posterior' | 'circunferencial'

const props = defineProps<{
  regioes: ExameFisicoRegiao[]
  regioesExaminadas: string[]
  sexo?: string | null
  /**
   * Mapa de id de nível-1 → vista resolvida, para coloração por vista (R1–R4).
   * Precedência circ > posterior > anterior quando um id recebe múltiplas vistas.
   * Prop opcional — quando ausente o hotspot aceso usa a cor de anterior (comportamento
   * de fallback não-breaking para usos legados).
   */
  vistasPorId?: Record<string, VistaHotspot>
}>()

const emit = defineEmits<{
  regiaoClicada: [regiao: ExameFisicoRegiao]
  troncoClicado: [vista: TroncoClique]
}>()

const isFeminino = computed(() => {
  const s = (props.sexo || '').toLowerCase().trim()
  if (!s) return false
  return s === 'f' || s === 'feminino' || s.includes('feminino')
})

const bgSrc = computed(() =>
  new URL(
    `../../assets/img/corpo-bg-${isFeminino.value ? 'feminino' : 'masculino'}.webp`,
    import.meta.url,
  ).href,
)

const currentPaths = computed<Record<string, BodyRegionPath>>(() =>
  isFeminino.value ? femaleRegionPaths : maleRegionPaths,
)

/**
 * Hotspots do catálogo — exclui as partes do tronco (deixaram de ser clicáveis).
 * Tronco é renderizado como pseudo-hotspots sintéticos (ver abaixo).
 */
const NOMES_TRONCO = new Set([
  'Tórax (anterior)', 'Abdome (anterior)', 'Pelve (anterior)',
  'Tórax (posterior)', 'Região lombossacra (posterior)', 'Pelve (posterior)',
])

const regioesComPath = computed(() =>
  props.regioes
    .filter((r) => r.nivel === 1 && currentPaths.value[r.nome] && !NOMES_TRONCO.has(r.nome))
    .map((r) => ({
      ...r,
      pathData: currentPaths.value[r.nome],
      isExaminada: props.regioesExaminadas.includes(r.id),
    }))
    .sort((a, b) => a.pathData.zOrder - b.pathData.zOrder),
)

/**
 * Pseudo-hotspots de tronco fundido — nós sintéticos de UI (não existem no catálogo).
 * Acendem por "OU das partes" via PARTE_PARA_TRONCO.
 */
const TRONCO_HOTSPOTS: Array<{ nome: 'Tronco (anterior)' | 'Tronco (posterior)'; vistaId: TroncoClique; vistaHotspot: VistaHotspot }> = [
  { nome: 'Tronco (anterior)',  vistaId: 'tronco-anterior',  vistaHotspot: 'anterior'  },
  { nome: 'Tronco (posterior)', vistaId: 'tronco-posterior', vistaHotspot: 'posterior' },
]

const troncoHotspots = computed(() =>
  TRONCO_HOTSPOTS.flatMap((t) => {
    const pathData = currentPaths.value[t.nome]
    if (!pathData) return []
    // Acende quando qualquer nível-1 daquela vista estiver em regioesExaminadas.
    const isExaminada = props.regioesExaminadas.some(
      (id) => PARTE_PARA_TRONCO[id] === t.nome,
    )
    return [{ ...t, pathData, isExaminada }]
  }),
)

/** Resolve a classe de cor do hotspot aceso a partir de vistasPorId (R1–R4). */
function classeVista(id: string): string {
  const vista = props.vistasPorId?.[id]
  if (vista === 'circunferencial') return 'region-selected-circ'
  if (vista === 'posterior')       return 'region-selected-post'
  return 'region-selected-ant'  // anterior ou ausente (fallback)
}

/**
 * Para pseudo-hotspots de tronco: consulta vistasPorId pela chave virtual
 * ('tronco-anterior'/'tronco-posterior') populada por SecaoExameFisico.
 * Fallback para o campo estático vistaHotspot quando a prop está ausente.
 */
function classeVistaTronco(t: typeof TRONCO_HOTSPOTS[number]): string {
  const vistaResolvida = props.vistasPorId?.[t.vistaId] ?? t.vistaHotspot
  if (vistaResolvida === 'circunferencial') return 'region-selected-circ'
  if (vistaResolvida === 'posterior')       return 'region-selected-post'
  return 'region-selected-ant'
}

const hoveredNome = ref<string | null>(null)
const hoveredGroupIds = ref<string[]>([])
const hoveredTronco = ref<TroncoClique | null>(null)
let leaveTimer: ReturnType<typeof setTimeout> | null = null

const MEMBRO_RE = /^Membro (superior|inferior) (?:direito|esquerdo) \((anterior|posterior)\)$/i

function getMembroGroup(regiao: ExameFisicoRegiao): string[] {
  const m = MEMBRO_RE.exec(regiao.nome)
  if (!m) return [regiao.id]
  const tipo = m[1]
  const vista = m[2]
  const ids: string[] = []
  for (const r of regioesComPath.value) {
    const rm = MEMBRO_RE.exec(r.nome)
    if (rm && rm[1] === tipo && rm[2] === vista) ids.push(r.id)
  }
  return ids.length > 0 ? ids : [regiao.id]
}

function handleMouseEnter(regiao: ExameFisicoRegiao) {
  if (leaveTimer !== null) {
    clearTimeout(leaveTimer)
    leaveTimer = null
  }
  hoveredTronco.value = null
  hoveredGroupIds.value = getMembroGroup(regiao)
  const m = /^(Membro (?:superior|inferior)) (?:direito|esquerdo) (\((?:anterior|posterior)\))$/.exec(regiao.nome)
  hoveredNome.value = m ? `${m[1]} ${m[2]}` : regiao.nome
}

function handleMouseEnterTronco(vistaId: TroncoClique, label: string) {
  if (leaveTimer !== null) {
    clearTimeout(leaveTimer)
    leaveTimer = null
  }
  hoveredGroupIds.value = []
  hoveredTronco.value = vistaId
  hoveredNome.value = label
}

function handleMouseLeave() {
  leaveTimer = setTimeout(() => {
    hoveredGroupIds.value = []
    hoveredTronco.value = null
    hoveredNome.value = null
    leaveTimer = null
  }, 30)
}

function handleClick(regiao: ExameFisicoRegiao) {
  emit('regiaoClicada', regiao)
}

function handleTroncoClick(vistaId: TroncoClique) {
  emit('troncoClicado', vistaId)
}
</script>

<template>
  <div class="mapa-wrap">
    <!-- Labels -->
    <div class="mapa-labels">
      <span>Frente (anterior)</span>
      <span>Costas (posterior)</span>
    </div>

    <!-- SVG atlas com hotspots -->
    <div class="relative w-full">
      <svg
        viewBox="0 25 1400 970"
        class="w-full select-none"
        xmlns="http://www.w3.org/2000/svg"
        style="display: block;"
      >
        <!-- Os 6 clipPaths de tronco foram removidos (B2).
             O tronco é agora 1 hotspot fundido por vista, sem recorte. -->

        <!-- Imagem de fundo do corpo -->
        <!--
          pointer-events="none" garante que a <image> opaca não capture o clique
          em navegadores que respeitam `visiblePainted` (Safari/Firefox em
          alguns modos). Sem isso, paths transparentes que ficam por cima
          podem não receber o evento.
        -->
        <image
          :href="bgSrc"
          x="0"
          y="0"
          width="1400"
          height="1024"
          preserveAspectRatio="xMidYMid meet"
          pointer-events="none"
        />

        <!-- Linha divisória entre anterior e posterior -->
        <line
          x1="700" y1="25" x2="700" y2="995"
          stroke="hsl(var(--border))"
          stroke-width="0.5"
          stroke-dasharray="6,4"
          opacity="0.4"
        />

        <!-- Pseudo-hotspots de tronco fundido (sintéticos — não existem no catálogo).
             Renderizados antes dos hotspots do catálogo (zOrder 0) para respeitar empilhamento. -->
        <path
          v-for="t in troncoHotspots"
          :key="t.vistaId"
          :d="t.pathData.d"
          :class="[
            'region-hotspot',
            t.isExaminada
              ? classeVistaTronco(t)
              : hoveredTronco === t.vistaId
                ? 'region-hover'
                : 'region-idle',
          ]"
          role="button"
          :aria-label="t.nome"
          @click="handleTroncoClick(t.vistaId)"
          @mouseenter="handleMouseEnterTronco(t.vistaId, t.nome)"
          @mouseleave="handleMouseLeave()"
        />

        <!-- Hotspots clicáveis — paths nativos do SVG (cabeça, pescoço, membros) -->
        <path
          v-for="regiao in regioesComPath"
          :key="regiao.id"
          :d="regiao.pathData.d"
          :class="[
            'region-hotspot',
            regiao.isExaminada
              ? classeVista(regiao.id)
              : hoveredGroupIds.includes(regiao.id)
                ? 'region-hover'
                : 'region-idle',
          ]"
          role="button"
          :aria-label="regiao.nome"
          @click="handleClick(regiao)"
          @mouseenter="handleMouseEnter(regiao)"
          @mouseleave="handleMouseLeave()"
        />
      </svg>

      <!-- Tooltip -->
      <div
        v-if="hoveredNome"
        class="absolute bottom-2 left-1/2 -translate-x-1/2
               bg-popover border border-border text-popover-foreground
               px-2.5 py-1 rounded-md shadow
               pointer-events-none whitespace-nowrap z-10 tooltip-texto"
      >
        {{ hoveredNome }}
      </div>
    </div>

    <!-- Legenda de vistas (CA8) -->
    <div class="mapa-legenda" aria-label="Legenda de cores por vista">
      <span class="legenda-item">
        <span class="legenda-dot legenda-dot--ant" />
        Anterior
      </span>
      <span class="legenda-item">
        <span class="legenda-dot legenda-dot--post" />
        Posterior
      </span>
      <span class="legenda-item">
        <span class="legenda-dot legenda-dot--circ" />
        Circunferencial
      </span>
    </div>
  </div>
</template>

<style scoped>
.mapa-wrap {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-2);
  /* Largura máxima reduzida para caber na coluna esquerda da grade lateral */
  max-width: 480px;
  width: 100%;
  margin: 0 auto;
}

.mapa-labels {
  display: flex;
  width: 100%;
  justify-content: space-around;
  font-size: var(--text-2xs);
  font-weight: var(--font-weight-semibold);
  color: hsl(var(--muted-foreground));
  text-transform: uppercase;
  letter-spacing: 0.06em;
  padding: 0 var(--space-4);
}

.tooltip-texto {
  font-size: var(--text-2xs);
}

/* ── Legenda ─────────────────────────────────────────────────────────────── */
.mapa-legenda {
  display: flex;
  flex-wrap: wrap;
  gap: 12px 20px;
  justify-content: center;
  font-size: var(--text-xs);
  color: hsl(var(--secondary) / 0.65);
  margin-top: var(--space-1);
}

.legenda-item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.legenda-dot {
  width: 11px;
  height: 11px;
  border-radius: 3px;
  flex-shrink: 0;
}
.legenda-dot--ant  { background: hsl(var(--vista-anterior)  / 0.65); }
.legenda-dot--post { background: hsl(var(--vista-posterior) / 0.65); }
.legenda-dot--circ { background: hsl(var(--vista-circ)      / 0.65); }

/* ── Hotspots ─────────────────────────────────────────────────────────────── */
.region-hotspot {
  cursor: pointer;
  pointer-events: all;
  transition: fill 0.15s ease, stroke 0.15s ease;
}

.region-idle {
  fill: transparent;
  stroke: transparent;
  stroke-width: 1.5;
}

.region-hover {
  fill: hsl(var(--primary) / 0.2);
  stroke: hsl(var(--primary) / 0.7);
  stroke-width: 1.5;
}

/* Cor por vista (R1–R4) */
.region-selected-ant {
  fill: hsl(var(--vista-anterior)  / 0.34);
  stroke: hsl(var(--vista-anterior));
  stroke-width: 2;
}
.region-selected-ant:hover {
  fill: hsl(var(--vista-anterior) / 0.5);
}

.region-selected-post {
  fill: hsl(var(--vista-posterior) / 0.34);
  stroke: hsl(var(--vista-posterior));
  stroke-width: 2;
}
.region-selected-post:hover {
  fill: hsl(var(--vista-posterior) / 0.5);
}

.region-selected-circ {
  fill: hsl(var(--vista-circ) / 0.42);
  stroke: hsl(var(--vista-circ));
  stroke-width: 2;
}
.region-selected-circ:hover {
  fill: hsl(var(--vista-circ) / 0.58);
}
</style>
