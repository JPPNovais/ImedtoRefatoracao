<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  maleRegionPaths,
  femaleRegionPaths,
  type BodyRegionPath,
} from './bodyMapPaths'

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

/** Vista resolvida de um hotspot para fins de coloração. */
export type VistaHotspot = 'anterior' | 'posterior' | 'circunferencial'

/**
 * TroncoClique mantido por compatibilidade com imports existentes (SecaoExameFisico.vue),
 * mas o evento troncoClicado foi removido — tronco agora é região normal (briefing 2026-06-25_002).
 * @deprecated Não usar em código novo.
 */
export type TroncoClique = 'tronco-anterior' | 'tronco-posterior'

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
 * Hotspots do catálogo — todos os nós nível-1 com path disponível.
 * Tronco agora é região real (tronco-anterior/tronco-posterior) e passa aqui como qualquer outra.
 * Indexado por CÓDIGO (r.id), não por nome — resistente a renomeações no admin.
 * Briefing 2026-06-25_002: removido CODIGOS_TRONCO e pseudo-hotspots sintéticos.
 */
const regioesComPath = computed(() =>
  props.regioes
    .filter((r) => r.nivel === 1 && currentPaths.value[r.id])
    .map((r) => ({
      ...r,
      pathData: currentPaths.value[r.id],
      isExaminada: props.regioesExaminadas.includes(r.id),
    }))
    .sort((a, b) => a.pathData.zOrder - b.pathData.zOrder),
)

/** Resolve a classe de cor do hotspot aceso a partir de vistasPorId (R1–R4). */
function classeVista(id: string): string {
  const vista = props.vistasPorId?.[id]
  if (vista === 'circunferencial') return 'region-selected-circ'
  if (vista === 'posterior')       return 'region-selected-post'
  return 'region-selected-ant'  // anterior ou ausente (fallback)
}

const hoveredNome = ref<string | null>(null)
const hoveredGroupIds = ref<string[]>([])
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
  hoveredGroupIds.value = getMembroGroup(regiao)
  const m = /^(Membro (?:superior|inferior)) (?:direito|esquerdo) (\((?:anterior|posterior)\))$/.exec(regiao.nome)
  hoveredNome.value = m ? `${m[1]} ${m[2]}` : regiao.nome
}

function handleMouseLeave() {
  leaveTimer = setTimeout(() => {
    hoveredGroupIds.value = []
    hoveredNome.value = null
    leaveTimer = null
  }, 30)
}

function handleClick(regiao: ExameFisicoRegiao) {
  emit('regiaoClicada', regiao)
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
        <!-- Fusão estrutural do tronco (briefing 2026-06-25_002):
             tronco-anterior/tronco-posterior são regiões reais do catálogo
             e renderizadas junto com os demais hotspots (regioesComPath). -->

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

        <!-- Hotspots clicáveis — todos os nós nível-1 com path (incl. tronco-anterior/tronco-posterior). -->
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
  max-width: 500px;
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
