<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { NavigationMenuViewport } from "reka-ui"
import { cn } from "@/utils/cn"
const props = defineProps<{ class?: HTMLAttributes["class"] }>()
</script>
<template>
  <div class="absolute left-0 top-full flex justify-center">
    <NavigationMenuViewport :class="cn('origin-top-center relative mt-1.5 h-[var(--reka-navigation-menu-viewport-height)] w-full overflow-hidden rounded-md border bg-popover text-popover-foreground shadow data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-90 md:w-[var(--reka-navigation-menu-viewport-width)]', props.class)" />
  </div>
</template>
