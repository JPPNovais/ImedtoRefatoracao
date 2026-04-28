<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "@/utils/cn"
import { ChevronRight } from "lucide-vue-next"

const props = defineProps<{ class?: HTMLAttributes["class"]; disabled?: boolean }>()
const emits = defineEmits<{ click: [event: MouseEvent] }>()
</script>

<template>
  <button
    :disabled="disabled"
    :class="cn('absolute inline-flex h-8 w-8 items-center justify-center rounded-full border bg-background shadow-sm transition-colors hover:bg-accent hover:text-accent-foreground disabled:opacity-50 -right-12 top-1/2 -translate-y-1/2', props.class)"
    @click="emits('click', $event)"
  >
    <ChevronRight class="h-4 w-4" />
    <span class="sr-only">Próximo</span>
  </button>
</template>
