<script setup lang="ts">
import type { AvatarFallbackProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { AvatarFallback } from "reka-ui"
import { cn } from "@/utils/cn"

const props = defineProps<AvatarFallbackProps & { class?: HTMLAttributes["class"] }>()
</script>

<template>
  <AvatarFallback
    v-bind="props"
    :class="cn('flex h-full w-full items-center justify-center rounded-full bg-muted', props.class)"
  >
    <slot />
  </AvatarFallback>
</template>
