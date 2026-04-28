<script setup lang="ts">
import { computed } from "vue"
import type { HTMLAttributes } from "vue"
import { cn } from "@/utils/cn"

const props = withDefaults(defineProps<{
  modelValue?: number | null
  placeholder?: string
  required?: boolean
  disabled?: boolean
  class?: HTMLAttributes["class"]
  autocomplete?: string
}>(), {
  modelValue: null,
  placeholder: "R$ 0,00",
  required: false,
  disabled: false,
  autocomplete: "nope",
})

const emit = defineEmits<{ "update:modelValue": [value: number] }>()

function formatCurrencyInput(value: string): string {
  const digits = value.replace(/\D/g, "")
  if (!digits) return ""
  const numValue = parseInt(digits, 10) / 100
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(numValue)
}

function parseCurrency(value: string): number {
  if (!value) return 0
  const cleaned = value.replace(/R\$\s?/g, "").replace(/\./g, "").replace(",", ".").trim()
  const num = parseFloat(cleaned)
  return isNaN(num) ? 0 : num
}

const displayValue = computed(() => {
  if (props.modelValue == null || props.modelValue === 0) return ""
  const centavos = Math.round(props.modelValue * 100).toString()
  return formatCurrencyInput(centavos)
})

function handleInput(event: Event) {
  const input = event.target as HTMLInputElement
  const formatted = formatCurrencyInput(input.value)
  input.value = formatted
  emit("update:modelValue", parseCurrency(formatted))
}
</script>

<template>
  <input
    :value="displayValue"
    type="text"
    inputmode="numeric"
    :autocomplete="autocomplete"
    :placeholder="placeholder"
    :required="required"
    :disabled="disabled"
    :class="cn(
      'flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors',
      'placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring',
      'disabled:cursor-not-allowed disabled:opacity-50',
      props.class,
    )"
    @input="handleInput"
  />
</template>
