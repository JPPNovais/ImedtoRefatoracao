<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "@/utils/cn"

const props = defineProps<{
  class?: HTMLAttributes["class"]
  items?: Array<{ label: string; color: string }>
}>()
</script>

<template>
  <div :class="cn('flex flex-wrap items-center gap-4 text-sm', props.class)">
    <slot>
      <div v-for="item in items" :key="item.label" class="flex items-center gap-1.5">
        <div class="h-2.5 w-2.5 shrink-0 rounded-[2px]" :style="{ backgroundColor: item.color }" />
        <span class="text-muted-foreground">{{ item.label }}</span>
      </div>
    </slot>
  </div>
</template>
