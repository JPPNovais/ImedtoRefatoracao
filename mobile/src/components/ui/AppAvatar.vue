<script setup lang="ts">
import { computed } from "vue"
import { iniciais } from "@/lib/format"

const props = defineProps<{ nome: string; classe?: string }>()
const ini = computed(() => iniciais(props.nome))
</script>

<template>
  <span class="av" :class="classe">{{ ini }}</span>
</template>
