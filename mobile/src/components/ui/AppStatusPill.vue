<script setup lang="ts">
import { computed } from "vue"
import { statusPill } from "@/lib/format"

const props = defineProps<{ status: string }>()
const p = computed(() => statusPill(props.status))
</script>

<template>
  <span class="pill" :class="p.cls"><i class="fa-solid" :class="p.icon"></i> {{ p.label }}</span>
</template>
