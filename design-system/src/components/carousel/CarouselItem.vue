<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { inject } from "vue"
import { cn } from "@/utils/cn"

const props = defineProps<{ class?: HTMLAttributes["class"] }>()
const carousel = inject<{ orientation: string }>("carousel", { orientation: "horizontal" })
</script>

<template>
  <div
    role="group"
    aria-roledescription="slide"
    :class="cn('min-w-0 shrink-0 grow-0 basis-full', carousel.orientation === 'horizontal' ? 'pl-4' : 'pt-4', props.class)"
  >
    <slot />
  </div>
</template>
