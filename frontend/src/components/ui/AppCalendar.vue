<script setup lang="ts">
import { computed } from "vue"
import { CalendarDate, type DateValue } from "@internationalized/date"
import { Calendar } from "@imedto/ui"

const props = defineProps<{
    modelValue?:    string | null
    min?:           string
    max?:           string
    datasComPonto?: string[]
}>()

const emit = defineEmits<{
    "update:modelValue": [value: string]
    "mesMudou":          [payload: { ano: number; mes: number }]
}>()

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

function onPlaceholderChange(ph: DateValue) {
    emit("mesMudou", { ano: ph.year, mes: ph.month - 1 })
}
</script>

<template>
    <Calendar
        :model-value="calendarValue"
        :min-value="minValue"
        :max-value="maxValue"
        :datas-com-ponto="datasComPonto"
        @update:model-value="(v) => v && emit('update:modelValue', toISO(v))"
        @update:placeholder="onPlaceholderChange"
    />
</template>
