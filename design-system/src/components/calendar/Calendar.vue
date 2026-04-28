<script lang="ts" setup>
import type { CalendarRootEmits, CalendarRootProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import type { DateValue } from "@internationalized/date"
import { ref, watch, computed } from "vue"
import { reactiveOmit } from "@vueuse/core"
import { CalendarRoot, useForwardPropsEmits } from "reka-ui"
import { today, getLocalTimeZone, CalendarDate, isSameDay } from "@internationalized/date"
import { cn } from "@/utils/cn"
import { CalendarCell, CalendarCellTrigger, CalendarGrid, CalendarGridBody, CalendarGridHead, CalendarGridRow, CalendarHeadCell, CalendarHeader, CalendarHeading, CalendarNextButton, CalendarPrevButton } from "."

const props = defineProps<CalendarRootProps & {
  class?: HTMLAttributes["class"]
  datasComPonto?: string[]
}>()

const emits = defineEmits<CalendarRootEmits>()

const delegatedProps = reactiveOmit(props, "class", "placeholder", "locale", "datasComPonto")

const pontosSet = computed(() => new Set(props.datasComPonto ?? []))

function toISOKey(date: DateValue): string {
  return `${date.year}-${String(date.month).padStart(2, "0")}-${String(date.day).padStart(2, "0")}`
}

function isDotVisible(date: DateValue): boolean {
  if (!pontosSet.value.has(toISOKey(date))) return false
  return true
}

function isDotSelected(date: DateValue): boolean {
  if (!props.modelValue || Array.isArray(props.modelValue)) return false
  return isSameDay(props.modelValue as DateValue, date)
}

const forwarded = useForwardPropsEmits(delegatedProps, emits)

const locale = computed(() => props.locale ?? "pt-BR")

const internalPlaceholder = ref(props.placeholder || today(getLocalTimeZone()))

watch(() => props.placeholder, (newPlaceholder) => {
  if (newPlaceholder) {
    internalPlaceholder.value = newPlaceholder
  }
})

const showTodayIcon = computed(() => {
  if (!props.modelValue) return true
  const todayDate = today(getLocalTimeZone())
  const val = Array.isArray(props.modelValue) ? props.modelValue[0] : props.modelValue
  if (!val) return true
  return !isSameDay(val, todayDate)
})

function goToToday() {
  const todayDate = today(getLocalTimeZone())
  emits('update:modelValue', todayDate)
  internalPlaceholder.value = todayDate
}

function handleYearChange(year: number) {
  const current = internalPlaceholder.value
  const newDate = new CalendarDate(year, current.month, Math.min(current.day, 28))
  internalPlaceholder.value = newDate
  emits('update:placeholder', newDate)
}

function handleMonthChange(month: number) {
  const current = internalPlaceholder.value
  const newDate = new CalendarDate(current.year, month, Math.min(current.day, 28))
  internalPlaceholder.value = newDate
  emits('update:placeholder', newDate)
}
</script>

<template>
  <CalendarRoot
    v-slot="{ grid, weekDays }"
    :class="cn('p-3 w-fit', props.class)"
    :placeholder="(internalPlaceholder as any)"
    :locale="locale"
    v-bind="forwarded"
  >
    <CalendarHeader class="relative">
      <CalendarPrevButton />
      <CalendarHeading
        @update:year="handleYearChange"
        @update:month="handleMonthChange"
      />
      <div class="flex items-center gap-1">
        <button
          v-if="showTodayIcon"
          type="button"
          class="p-1.5 rounded-md border-0 bg-transparent cursor-pointer
                 text-muted-foreground hover:text-foreground hover:bg-accent
                 transition-colors"
          title="Ir para hoje"
          @click="goToToday"
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
          >
            <circle cx="12" cy="12" r="10"/>
            <polyline points="12 6 12 12 16 14"/>
          </svg>
        </button>
        <CalendarNextButton />
      </div>
    </CalendarHeader>

    <div class="flex flex-col gap-y-4 mt-4 sm:flex-row sm:gap-x-4 sm:gap-y-0">
      <CalendarGrid v-for="month in grid" :key="month.value.toString()">
        <CalendarGridHead>
          <CalendarGridRow>
            <CalendarHeadCell
              v-for="day in weekDays" :key="day"
            >
              {{ day }}
            </CalendarHeadCell>
          </CalendarGridRow>
        </CalendarGridHead>
        <CalendarGridBody>
          <CalendarGridRow v-for="(weekDates, index) in month.rows" :key="`weekDate-${index}`" class="mt-2 w-full">
            <CalendarCell
              v-for="weekDate in weekDates"
              :key="weekDate.toString()"
              :date="weekDate"
            >
              <CalendarCellTrigger
                :day="weekDate"
                :month="month.value"
              />
              <span
                v-if="isDotVisible(weekDate)"
                :class="cn(
                  'absolute bottom-0.5 left-1/2 -translate-x-1/2 w-1 h-1 rounded-full pointer-events-none',
                  isDotSelected(weekDate) ? 'bg-primary-foreground' : 'bg-primary'
                )"
              />
            </CalendarCell>
          </CalendarGridRow>
        </CalendarGridBody>
      </CalendarGrid>
    </div>
  </CalendarRoot>
</template>
