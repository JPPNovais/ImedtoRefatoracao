<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "@/utils/cn"

const props = withDefaults(defineProps<{
  class?: HTMLAttributes["class"]
  orientation?: "horizontal" | "vertical"
}>(), { orientation: "horizontal" })
</script>

<template>
  <div
    role="group"
    :class="cn(
      'inline-flex',
      orientation === 'vertical' ? 'flex-col' : 'flex-row',
      '[&>*:not(:first-child):not(:last-child)]:rounded-none',
      orientation === 'horizontal'
        ? '[&>*:first-child]:rounded-r-none [&>*:last-child]:rounded-l-none [&>*:not(:first-child)]:-ml-px'
        : '[&>*:first-child]:rounded-b-none [&>*:last-child]:rounded-t-none [&>*:not(:first-child)]:-mt-px',
      props.class,
    )"
  >
    <slot />
  </div>
</template>
