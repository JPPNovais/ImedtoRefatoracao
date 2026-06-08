<script setup lang="ts">
import type { LabelProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { Label, useForwardProps } from "reka-ui"
import { cn } from "@/utils/cn"

const props = defineProps<LabelProps & { class?: HTMLAttributes["class"] }>()

const delegatedProps = useForwardProps(props)
</script>

<template>
  <Label
    v-bind="delegatedProps"
    :class="cn('ds-label peer-disabled:cursor-not-allowed peer-disabled:opacity-70', props.class)"
  >
    <slot />
  </Label>
</template>

<style>
/* Label canônico: 12px/600 — Q2 (briefing 2026-06-08_003) */
.ds-label {
  font-size: var(--text-xs, 0.75rem);
  font-weight: var(--font-weight-semibold, 600);
  line-height: var(--line-height-none, 1);
  color: hsl(var(--secondary));
  display: block;
  margin-bottom: 0.25rem;
}
</style>
