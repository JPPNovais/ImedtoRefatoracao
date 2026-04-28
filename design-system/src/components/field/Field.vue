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
        'text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70',
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
