<script setup lang="ts">
import { useUiStore } from "@/stores/ui"
const ui = useUiStore()

function confirmar() {
  const cb = ui.confirmState?.onConfirm
  ui.closeConfirm()
  cb?.()
}
</script>

<template>
  <div class="dialog-scrim" :class="{ show: !!ui.confirmState }" @click="ui.closeConfirm()"></div>
  <div class="dialog" :class="{ show: !!ui.confirmState }">
    <div class="dlg-ic"><i class="fa-solid fa-triangle-exclamation"></i></div>
    <b>{{ ui.confirmState?.title }}</b>
    <p>{{ ui.confirmState?.msg }}</p>
    <div class="dlg-actions">
      <button class="btn-outline" style="margin: 0" @click="ui.closeConfirm()">Cancelar</button>
      <button class="btn-danger" @click="confirmar">{{ ui.confirmState?.confirmLabel || "Confirmar" }}</button>
    </div>
  </div>
</template>
