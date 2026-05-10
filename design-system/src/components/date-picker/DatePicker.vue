<script setup lang="ts">
import { CalendarDate, type DateValue } from "@internationalized/date"
import type { HTMLAttributes } from "vue"
import { ref, watch } from "vue"
import { cn } from "@/utils/cn"
import { Calendar } from "@/components/calendar"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/popover"

const props = withDefaults(defineProps<{
  class?: HTMLAttributes["class"]
  modelValue?: DateValue
  placeholder?: string
  disabled?: boolean
  locale?: string
  maxValue?: DateValue
  minValue?: DateValue
}>(), {
  placeholder: "DD/MM/AAAA",
  locale: "pt-BR",
})

const emits = defineEmits<{
  'update:modelValue': [value: DateValue | undefined]
}>()

const open = ref(false)
const inputText = ref('')

function formatDate(date: DateValue) {
  return `${String(date.day).padStart(2, '0')}/${String(date.month).padStart(2, '0')}/${date.year}`
}

function applyDateMask(value: string): string {
  const digits = value.replace(/\D/g, '').slice(0, 8)
  if (digits.length <= 2) return digits
  if (digits.length <= 4) return `${digits.slice(0, 2)}/${digits.slice(2)}`
  return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`
}

function isValidDate(day: number, month: number, year: number): boolean {
  if (month < 1 || month > 12) return false
  if (day < 1) return false
  if (year < 1900 || year > 2100) return false
  const daysInMonth = new Date(year, month, 0).getDate()
  return day <= daysInMonth
}

function parseInputDate(value: string): DateValue | null {
  const match = value.match(/^(\d{2})\/(\d{2})\/(\d{4})$/)
  if (!match) return null
  const day = parseInt(match[1], 10)
  const month = parseInt(match[2], 10)
  const year = parseInt(match[3], 10)
  if (!isValidDate(day, month, year)) return null
  return new CalendarDate(year, month, day)
}

watch(() => props.modelValue, (newVal) => {
  const formatted = newVal ? formatDate(newVal) : ''
  if (formatted !== inputText.value) {
    inputText.value = formatted
  }
}, { immediate: true })

function onInput(event: Event) {
  const input = event.target as HTMLInputElement
  const cursorPos = input.selectionStart ?? 0
  const prevLength = input.value.length

  const masked = applyDateMask(input.value)
  inputText.value = masked
  input.value = masked

  const newLength = masked.length
  const diff = newLength - prevLength

  requestAnimationFrame(() => {
    let newPos = cursorPos + diff
    if (newPos > 0 && masked[newPos - 1] === '/') {
      newPos++
    }
    input.setSelectionRange(newPos, newPos)
  })

  const parsed = parseInputDate(masked)
  if (parsed) {
    const exceedsMax = props.maxValue && parsed.compare(props.maxValue) > 0
    const belowMin = props.minValue && parsed.compare(props.minValue) < 0
    if (!exceedsMax && !belowMin) {
      emits('update:modelValue', parsed)
    }
  } else if (masked === '') {
    emits('update:modelValue', undefined)
  }
}

function onKeydown(event: KeyboardEvent) {
  const input = event.target as HTMLInputElement
  const pos = input.selectionStart ?? 0

  if (event.key === 'Backspace' && pos > 0
    && inputText.value[pos - 1] === '/') {
    event.preventDefault()
    const digits = inputText.value.replace(/\D/g, '')
    const newDigits = digits.slice(0, pos - 1) + digits.slice(pos)
    inputText.value = applyDateMask(newDigits)
    requestAnimationFrame(() => {
      input.setSelectionRange(pos - 1, pos - 1)
    })
  }
}
</script>

<template>
  <Popover v-model:open="open">
    <div :class="cn('relative w-[240px]', props.class)">
      <!-- Ícone do calendário (decorativo). O wrapper inteiro é clicável via PopoverAnchor abaixo. -->
      <span
        class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-muted-foreground"
        aria-hidden="true"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
          class="h-4 w-4"
        >
          <path d="M8 2v4" />
          <path d="M16 2v4" />
          <rect width="18" height="18" x="3" y="4" rx="2" />
          <path d="M3 10h18" />
        </svg>
      </span>
      <!-- O input fica como PopoverTrigger asChild — clicar nele abre o popover,
           mas digitação manual continua funcionando porque o reka-ui só abre/fecha
           em pointerdown, não em keydown. -->
      <PopoverTrigger as-child>
        <input
          type="text"
          inputmode="numeric"
          maxlength="10"
          :value="inputText"
          :placeholder="placeholder"
          :disabled="disabled"
          :class="cn(
            'flex h-9 w-full rounded-md border border-input bg-transparent',
            'pl-9 pr-3 py-1 text-sm shadow-sm transition-colors',
            'placeholder:text-muted-foreground',
            'focus-visible:outline-none focus-visible:ring-1',
            'focus-visible:ring-ring',
            'disabled:cursor-not-allowed disabled:opacity-50',
          )"
          @input="onInput"
          @keydown="onKeydown"
        >
      </PopoverTrigger>
    </div>
    <PopoverContent
      class="w-auto p-0"
      align="start"
      @open-auto-focus="(e) => e.preventDefault()"
    >
      <Calendar
        :model-value="modelValue"
        :placeholder="modelValue"
        :locale="locale"
        :max-value="maxValue"
        :min-value="minValue"
        initial-focus
        @update:model-value="(v) => {
          emits('update:modelValue', v as DateValue | undefined)
          if (v) open = false
        }"
      />
    </PopoverContent>
  </Popover>
</template>
