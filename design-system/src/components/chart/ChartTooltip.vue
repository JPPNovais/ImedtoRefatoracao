<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "@/utils/cn"

const props = defineProps<{
  class?: HTMLAttributes["class"]
  label?: string
  value?: string | number
  color?: string
}>()
</script>

<template>
  <div :class="cn('rounded-lg border bg-background px-3 py-1.5 text-sm shadow-xl', props.class)">
    <slot>
      <div class="flex items-center gap-2">
        <div v-if="color" class="h-2.5 w-2.5 shrink-0 rounded-[2px]" :style="{ backgroundColor: color }" />
        <div class="flex flex-col">
          <span v-if="label" class="text-muted-foreground">{{ label }}</span>
          <span v-if="value !== undefined" class="font-bold">{{ value }}</span>
        </div>
      </div>
    </slot>
  </div>
</template>
