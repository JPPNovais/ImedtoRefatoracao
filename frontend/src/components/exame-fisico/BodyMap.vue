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

const props = defineProps<{
  regioes: ExameFisicoRegiao[]
  regioesExaminadas: string[]
  sexo?: string | null
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
const TRONCO_HOTSPOTS: Array<{ nome: 'Tronco (anterior)' | 'Tronco (posterior)'; vistaId: TroncoClique }> = [
  { nome: 'Tronco (anterior)',  vistaId: 'tronco-anterior'  },
  { nome: 'Tronco (posterior)', vistaId: 'tronco-posterior' },
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
  <div class="flex flex-col items-center gap-2 max-w-2xl mx-auto w-full">
    <!-- Labels -->
    <div class="flex w-full justify-around text-[10px] font-semibold text-muted-foreground uppercase tracking-wider px-4">
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
              ? 'region-selected'
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
              ? 'region-selected'
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
               text-[11px] px-2.5 py-1 rounded-md shadow
               pointer-events-none whitespace-nowrap z-10"
      >
        {{ hoveredNome }}
      </div>
    </div>

    <p class="text-[10px] text-muted-foreground text-center">
      Clique em uma região para registrar o exame
    </p>
  </div>
</template>

<style scoped>
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

.region-selected {
  fill: hsl(var(--primary) / 0.28);
  stroke: hsl(var(--primary));
  stroke-width: 2;
}

.region-selected:hover {
  fill: hsl(var(--primary) / 0.4);
}
</style>
