<script setup lang="ts">
import type { Column } from "@tanstack/vue-table"
import type { HTMLAttributes } from "vue"
import { cn } from "@/utils/cn"
import { ArrowUp, ArrowDown, ArrowUpDown } from "lucide-vue-next"

const props = defineProps<{
  class?: HTMLAttributes["class"]
  column: Column<unknown, unknown>
  title: string
}>()
</script>

<template>
  <div :class="cn('flex items-center gap-2', props.class)">
    <button
      v-if="column.getCanSort()"
      class="inline-flex items-center gap-1 hover:text-accent-foreground -ml-3 h-8 px-3 rounded-md"
      @click="column.toggleSorting()"
    >
      {{ title }}
      <ArrowUp v-if="column.getIsSorted() === 'asc'" class="h-4 w-4" />
      <ArrowDown v-else-if="column.getIsSorted() === 'desc'" class="h-4 w-4" />
      <ArrowUpDown v-else class="h-4 w-4 opacity-50" />
    </button>
    <span v-else>{{ title }}</span>
  </div>
</template>
