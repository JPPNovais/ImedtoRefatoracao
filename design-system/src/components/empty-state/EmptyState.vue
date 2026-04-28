<script setup lang="ts">
import { cn } from "@/utils/cn"

interface Props {
  icon?: string
  title?: string
  description?: string
  compact?: boolean
}

defineProps<Props>()
</script>

<template>
  <div :class="cn('flex flex-col items-center justify-center text-center text-muted-foreground', compact ? 'py-8 gap-2' : 'py-16 gap-3')">
    <span v-if="icon" class="text-4xl">{{ icon }}</span>
    <p v-if="title" :class="cn('font-semibold', compact ? 'text-sm' : 'text-base')">{{ title }}</p>
    <p v-if="description" class="text-sm max-w-xs">{{ description }}</p>
    <slot />
  </div>
</template>
