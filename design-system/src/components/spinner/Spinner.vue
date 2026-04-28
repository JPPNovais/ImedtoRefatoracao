<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "@/utils/cn"

const props = withDefaults(defineProps<{
  class?: HTMLAttributes["class"]
  size?: "sm" | "default" | "lg"
}>(), {
  size: "default",
})

const sizeClasses = {
  sm: "h-4 w-4 border-2",
  default: "h-6 w-6 border-2",
  lg: "h-8 w-8 border-[3px]",
}
</script>

<template>
  <div
    role="status"
    :class="cn(
      'inline-block animate-spin rounded-full border-current border-r-transparent text-primary',
      sizeClasses[props.size],
      props.class,
    )"
  >
    <span class="sr-only">Carregando...</span>
  </div>
</template>
