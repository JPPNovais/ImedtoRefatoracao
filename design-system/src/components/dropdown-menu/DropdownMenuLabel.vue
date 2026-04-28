<script setup lang="ts">
import type { DropdownMenuLabelProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { DropdownMenuLabel } from "reka-ui"
import { cn } from "@/utils/cn"

const props = defineProps<DropdownMenuLabelProps & {
  class?: HTMLAttributes["class"]
  inset?: boolean
}>()
</script>

<template>
  <DropdownMenuLabel
    v-bind="props"
    :class="cn('px-2 py-1.5 text-sm font-semibold', inset && 'pl-8', props.class)"
  >
    <slot />
  </DropdownMenuLabel>
</template>
