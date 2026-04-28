<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "@/utils/cn"
import { countBadgeVariants } from "."

const props = defineProps<{
  variant?: "default" | "info" | "warning" | "error" | "success" | "muted"
  class?: HTMLAttributes["class"]
}>()
</script>

<template>
  <span :class="cn(countBadgeVariants({ variant }), props.class)">
    <slot />
  </span>
</template>
