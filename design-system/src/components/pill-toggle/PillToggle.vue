<script setup lang="ts" generic="T extends string | number">
import { cn } from "@/utils/cn"
import type { PillToggleOpcao } from "@/components/pill-toggle/types"

defineProps<{
  modelValue: T
  opcoes: PillToggleOpcao<T>[]
}>()

const emit = defineEmits<{ "update:modelValue": [value: T] }>()
</script>

<template>
  <div class="inline-flex items-center gap-1 bg-muted rounded-lg p-1">
    <button
      v-for="o in $props.opcoes"
      :key="String(o.valor)"
      type="button"
      :class="cn(
        'inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-all duration-150 border-0 bg-transparent cursor-pointer',
        $props.modelValue === o.valor
          ? 'bg-primary text-primary-foreground shadow-sm'
          : 'text-muted-foreground hover:text-foreground hover:bg-background/60'
      )"
      @click="emit('update:modelValue', o.valor)"
    >
      <span v-if="o.icon" class="text-xs">{{ o.icon }}</span>
      {{ o.label }}
    </button>
  </div>
</template>
