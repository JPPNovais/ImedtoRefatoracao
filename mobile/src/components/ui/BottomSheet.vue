<script setup lang="ts">
import { ref, computed, watch } from "vue"

const props = defineProps<{
  open: boolean
  titulo?: string
  sub?: string
  tall?: boolean
  closable?: boolean
}>()
const emit = defineEmits<{ "update:open": [v: boolean] }>()

function close() {
  emit("update:open", false)
}

// ── Arrastar para fechar (swipe-to-dismiss) ─────────────────────────────────
// O gesto é ancorado no cabeçalho/grip (área fixa) para não brigar com o scroll
// do corpo. Enquanto o dedo arrasta para baixo, o sheet acompanha 1:1 via
// transform inline (que sobrepõe o transform das classes); ao soltar, fecha se
// passar do limiar ou for um flick rápido, senão volta animado para a posição.
const dragY = ref(0) // deslocamento vertical atual em px (>= 0)
const dragging = ref(false)
let startY = 0
let startT = 0

const DISMISS_PX = 110 // arrasto mínimo para fechar
const DISMISS_VELOCITY = 0.5 // px/ms — flick rápido fecha mesmo com arrasto curto

function onDragStart(e: TouchEvent) {
  startY = e.touches[0].clientY
  startT = e.timeStamp
  dragging.value = true
  dragY.value = 0
}

function onDragMove(e: TouchEvent) {
  if (!dragging.value) return
  const delta = e.touches[0].clientY - startY
  // só arrasta para baixo; bloqueia o scroll de fundo durante o gesto
  if (delta > 0) e.preventDefault()
  dragY.value = Math.max(0, delta)
}

function onDragEnd(e: TouchEvent) {
  if (!dragging.value) return
  const dt = e.timeStamp - startT
  const velocity = dragY.value / Math.max(dt, 1)
  const deveFechar = dragY.value > DISMISS_PX || velocity > DISMISS_VELOCITY
  dragging.value = false
  dragY.value = 0
  if (deveFechar) close()
}

// Enquanto arrasta, o transform inline acompanha o dedo (sem transição). Ao
// soltar, o style volta a {} e as classes reassumem (com transição → snap-back).
const sheetStyle = computed(() =>
  dragging.value
    ? { transform: `translateY(${dragY.value}px)`, transition: "none" }
    : {},
)

// Trava o scroll do body enquanto o sheet está aberto.
watch(
  () => props.open,
  (v) => {
    document.body.style.overflow = v ? "hidden" : ""
    if (!v) {
      dragging.value = false
      dragY.value = 0
    }
  },
)
</script>

<template>
  <Teleport defer to=".screen">
    <div class="scrim" :class="{ show: open }" @click="close"></div>
    <div class="sheet" :class="{ show: open, 'sheet-tall': tall }" :style="sheetStyle">
      <template v-if="tall">
        <div
          class="sheet-head"
          @touchstart.passive="onDragStart"
          @touchmove="onDragMove"
          @touchend.passive="onDragEnd"
          @touchcancel.passive="onDragEnd"
        >
          <div class="grip"></div>
          <div class="sh-row">
            <div class="sh-title">{{ titulo }}</div>
            <button class="sheet-x" @click="close"><i class="fa-solid fa-xmark"></i></button>
          </div>
        </div>
        <div class="sheet-body"><slot /></div>
        <div v-if="$slots.footer" class="sheet-foot"><slot name="footer" /></div>
      </template>
      <template v-else>
        <div
          class="sheet-drag"
          @touchstart.passive="onDragStart"
          @touchmove="onDragMove"
          @touchend.passive="onDragEnd"
          @touchcancel.passive="onDragEnd"
        >
          <div class="grip"></div>
          <div v-if="titulo" class="sh-row">
            <div class="sh-title">{{ titulo }}</div>
            <button v-if="closable" class="sheet-x" @click="close"><i class="fa-solid fa-xmark"></i></button>
          </div>
          <div v-if="sub" class="sh-sub">{{ sub }}</div>
        </div>
        <slot />
      </template>
    </div>
  </Teleport>
</template>

<style scoped>
/* Áreas de arraste (grip/cabeçalho): touch-action none garante touchmove
   cancelável para o swipe-to-dismiss, sem afetar o tap no X nem o scroll do
   corpo. .sheet-drag agrupa grip + título do sheet curto sem mudar o layout. */
.sheet-head,
.sheet-drag {
  touch-action: none;
}
</style>
