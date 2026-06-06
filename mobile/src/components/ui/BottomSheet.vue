<script setup lang="ts">
import { watch } from "vue"

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

// Trava o scroll do body enquanto o sheet está aberto.
watch(
  () => props.open,
  (v) => {
    document.body.style.overflow = v ? "hidden" : ""
  },
)
</script>

<template>
  <Teleport to=".screen">
    <div class="scrim" :class="{ show: open }" @click="close"></div>
    <div class="sheet" :class="{ show: open, 'sheet-tall': tall }">
      <template v-if="tall">
        <div class="sheet-head">
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
        <div class="grip"></div>
        <div v-if="titulo" class="sh-row">
          <div class="sh-title">{{ titulo }}</div>
          <button v-if="closable" class="sheet-x" @click="close"><i class="fa-solid fa-xmark"></i></button>
        </div>
        <div v-if="sub" class="sh-sub">{{ sub }}</div>
        <slot />
      </template>
    </div>
  </Teleport>
</template>
