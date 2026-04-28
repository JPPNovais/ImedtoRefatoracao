<script setup lang="ts">
import { computed } from "vue"
import { CalendarDate, type DateValue } from "@internationalized/date"
import { DatePicker } from "@imedto/ui"

const props = defineProps<{
    modelValue?:  string | null
    placeholder?: string
    disabled?:    boolean
    min?:         string
    max?:         string
    ariaLabel?:   string
    align?:       "start" | "center" | "end"
}>()

const emit = defineEmits<{ "update:modelValue": [value: string] }>()

function toCalendarDate(iso?: string | null): CalendarDate | undefined {
    if (!iso) return undefined
    const [y, m, d] = iso.split("-").map(Number)
    if (!y || !m || !d) return undefined
    return new CalendarDate(y, m, d)
}

function toISO(date: DateValue): string {
    return `${date.year}-${String(date.month).padStart(2, "0")}-${String(date.day).padStart(2, "0")}`
}

const calendarValue = computed(() => toCalendarDate(props.modelValue))
const minValue      = computed(() => toCalendarDate(props.min))
const maxValue      = computed(() => toCalendarDate(props.max))
</script>

<template>
    <DatePicker
        :model-value="calendarValue"
        :placeholder="placeholder"
        :disabled="disabled"
        :min-value="minValue"
        :max-value="maxValue"
        @update:model-value="(v) => v && emit('update:modelValue', toISO(v))"
    />
</template>
