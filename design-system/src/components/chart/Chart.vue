<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "@/utils/cn"

const props = withDefaults(defineProps<{
  class?: HTMLAttributes["class"]
  type?: "bar" | "line" | "pie" | "doughnut" | "area"
}>(), { type: "bar" })
</script>

<template>
  <div :class="cn('w-full', props.class)" :data-chart-type="type">
    <slot />
  </div>
</template>
