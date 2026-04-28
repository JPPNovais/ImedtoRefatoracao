<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { cn } from "@/utils/cn"
import { Search, Loader2 } from "lucide-vue-next"
import Input from "@/components/input/Input.vue"

const props = withDefaults(defineProps<{
  modelValue?: string
  placeholder?: string
  loading?: boolean
  disabled?: boolean
  class?: HTMLAttributes["class"]
}>(), { placeholder: "Buscar..." })

const emit = defineEmits<{ "update:modelValue": [value: string] }>()
</script>

<template>
  <div class="relative">
    <Search class="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground/50 pointer-events-none z-10 h-4 w-4" />
    <Input
      :model-value="modelValue"
      :placeholder="placeholder"
      :disabled="disabled"
      :class="cn('pl-9 pr-9', props.class)"
      @update:model-value="emit('update:modelValue', $event as string)"
    />
    <Loader2 v-if="loading" class="absolute right-3 top-1/2 -translate-y-1/2 text-primary pointer-events-none h-4 w-4 animate-spin" />
  </div>
</template>
