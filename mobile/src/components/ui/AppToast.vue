<script setup lang="ts">
import { useUiStore } from "@/stores/ui"
const ui = useUiStore()
</script>

<template>
  <div class="toast" :class="{ show: ui.toastVisible, err: ui.toastKind === 'error' }">
    <i class="fa-solid" :class="ui.toastKind === 'error' ? 'fa-circle-exclamation' : 'fa-circle-check'"></i>
    <span>{{ ui.toastMsg }}</span>
  </div>
</template>
