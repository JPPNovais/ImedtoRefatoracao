<script setup lang="ts">
import { ref } from "vue"

/** Linha com swipe actions (atendido/faltou) — gesto padrão da Agenda. */
const emit = defineEmits<{ open: []; done: []; miss: [] }>()

const aberto = ref(false)
const dx = ref(0)
const noTransition = ref(false)
let x0: number | null = null
let moved = false

function onDown(e: PointerEvent) {
  x0 = e.clientX
  moved = false
  noTransition.value = true
}
function onMove(e: PointerEvent) {
  if (x0 === null) return
  const raw = e.clientX - x0
  if (Math.abs(raw) > 6) moved = true
  let d = raw + (aberto.value ? -144 : 0)
  d = Math.max(-144, Math.min(0, d))
  dx.value = d
}
function onUp() {
  if (x0 === null) return
  noTransition.value = false
  if (!moved) {
    dx.value = aberto.value ? -144 : 0
    x0 = null
    if (!aberto.value) emit("open")
    return
  }
  aberto.value = dx.value < -60
  dx.value = aberto.value ? -144 : 0
  x0 = null
}
function act(kind: "done" | "miss") {
  aberto.value = false
  dx.value = 0
  if (kind === "done") emit("done")
  else emit("miss")
}
</script>

<template>
  <div class="swipe" :class="{ open: aberto }">
    <div class="swipe-actions">
      <button class="a-done" @click.stop="act('done')"><i class="fa-solid fa-check"></i>Atendido</button>
      <button class="a-miss" @click.stop="act('miss')"><i class="fa-solid fa-xmark"></i>Faltou</button>
    </div>
    <div
      class="swipe-card"
      :style="{ transform: `translateX(${dx}px)`, transition: noTransition ? 'none' : '' }"
      @pointerdown="onDown"
      @pointermove="onMove"
      @pointerup="onUp"
      @pointercancel="onUp"
    >
      <slot />
    </div>
  </div>
</template>
