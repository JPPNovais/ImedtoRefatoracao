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
  vista: 'anterior' | 'posterior' | 'ambos' | null
  template_texto: string | null
  filhos?: ExameFisicoRegiao[]
}

const props = defineProps<{
  regioes: ExameFisicoRegiao[]
  regioesExaminadas: string[]
  sexo?: string | null
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

const regioesComPath = computed(() =>
  props.regioes
    .filter((r) => r.nivel === 1 && currentPaths.value[r.nome])
    .map((r) => ({
      ...r,
      pathData: currentPaths.value[r.nome],
      isExaminada: props.regioesExaminadas.includes(r.id),
    }))
    .sort((a, b) => a.pathData.zOrder - b.pathData.zOrder),
)

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
        <defs>
          <!-- Clip-paths para sub-regiões do tronco -->
          <clipPath id="clip-torax-ant">
            <rect x="0" y="0" width="700" height="370" />
          </clipPath>
          <clipPath id="clip-abdome-ant">
            <rect x="0" y="370" width="700" height="82" />
          </clipPath>
          <clipPath id="clip-pelve-ant">
            <rect x="0" y="452" width="700" height="200" />
          </clipPath>
          <clipPath id="clip-torax-post">
            <rect x="700" y="0" width="700" height="387" />
          </clipPath>
          <clipPath id="clip-lombo-post">
            <rect x="700" y="387" width="700" height="83" />
          </clipPath>
          <clipPath id="clip-pelve-post">
            <rect x="700" y="470" width="700" height="200" />
          </clipPath>
        </defs>

        <!-- Imagem de fundo do corpo -->
        <image
          :href="bgSrc"
          x="0"
          y="0"
          width="1400"
          height="1024"
          preserveAspectRatio="xMidYMid meet"
        />

        <!-- Linha divisória entre anterior e posterior -->
        <line
          x1="700" y1="25" x2="700" y2="995"
          stroke="hsl(var(--border))"
          stroke-width="0.5"
          stroke-dasharray="6,4"
          opacity="0.4"
        />

        <!-- Hotspots clicáveis — paths nativos do SVG -->
        <path
          v-for="regiao in regioesComPath"
          :key="regiao.id"
          :d="regiao.pathData.d"
          :clip-path="regiao.pathData.clipId ? `url(#${regiao.pathData.clipId})` : undefined"
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
