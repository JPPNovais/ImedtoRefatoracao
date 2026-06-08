<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "@/utils/cn"

const props = withDefaults(defineProps<{
  class?: HTMLAttributes["class"]
  label?: string
  description?: string
  error?: string
  htmlFor?: string
  required?: boolean
}>(), {
  required: false,
})
</script>

<template>
  <div :class="cn('space-y-2', props.class)">
    <label
      v-if="label"
      :for="htmlFor"
      :class="cn(
        'ds-label',
        error && 'text-destructive',
      )"
    >
      {{ label }}
      <span v-if="required" class="text-destructive">*</span>
    </label>
    <slot />
    <p v-if="description && !error" class="text-[0.8rem] text-muted-foreground">
      {{ description }}
    </p>
    <p v-if="error" class="text-[0.8rem] font-medium text-destructive">
      {{ error }}
    </p>
  </div>
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
