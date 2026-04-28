<script lang="ts" setup>
import type { CalendarHeadingProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { ref } from "vue"
import { reactiveOmit } from "@vueuse/core"
import { CalendarHeading, useForwardProps } from "reka-ui"
import { cn } from "@/utils/cn"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/select"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/popover"

const props = defineProps<CalendarHeadingProps & { class?: HTMLAttributes["class"] }>()

const emit = defineEmits<{
  'update:year': [year: number]
  'update:month': [month: number]
}>()

defineSlots<{
  default: (props: { headingValue: string }) => any
}>()

const delegatedProps = reactiveOmit(props, "class")

const forwardedProps = useForwardProps(delegatedProps)

const open = ref(false)

const selectedMonth = ref(1)
const selectedYear = ref(new Date().getFullYear())

const months = [
  { value: 1, label: 'Janeiro' },
  { value: 2, label: 'Fevereiro' },
  { value: 3, label: 'Março' },
  { value: 4, label: 'Abril' },
  { value: 5, label: 'Maio' },
  { value: 6, label: 'Junho' },
  { value: 7, label: 'Julho' },
  { value: 8, label: 'Agosto' },
  { value: 9, label: 'Setembro' },
  { value: 10, label: 'Outubro' },
  { value: 11, label: 'Novembro' },
  { value: 12, label: 'Dezembro' },
]

const monthNameMap: Record<string, number> = {
  'janeiro': 1, 'january': 1,
  'fevereiro': 2, 'february': 2,
  'março': 3, 'march': 3,
  'abril': 4, 'april': 4,
  'maio': 5, 'may': 5,
  'junho': 6, 'june': 6,
  'julho': 7, 'july': 7,
  'agosto': 8, 'august': 8,
  'setembro': 9, 'september': 9,
  'outubro': 10, 'october': 10,
  'novembro': 11, 'november': 11,
  'dezembro': 12, 'december': 12,
}

const lastHeadingValue = ref('')

function syncFromHeading(headingValue: string) {
  if (!headingValue || headingValue === lastHeadingValue.value) return
  lastHeadingValue.value = headingValue

  if (open.value) return

  const parts = headingValue.split(' ')
  const monthName = parts.slice(0, -1).join(' ').toLowerCase()
  const year = parseInt(parts[parts.length - 1])

  const monthNum = monthNameMap[monthName]
  if (monthNum) {
    selectedMonth.value = monthNum
  }
  if (!isNaN(year)) {
    selectedYear.value = year
  }
}

function onInteractOutside(event: Event) {
  const target = event.target as HTMLElement
  if (target.closest('[role="listbox"]') || target.closest('[role="option"]')) {
    event.preventDefault()
  }
}

function handleYearInput(event: Event) {
  const input = event.target as HTMLInputElement
  const year = parseInt(input.value)
  if (!isNaN(year) && year >= 1900 && year <= 2100) {
    selectedYear.value = year
    emit('update:year', year)
  }
}

function handleMonthChange(monthStr: string) {
  const month = parseInt(monthStr)
  if (!isNaN(month)) {
    selectedMonth.value = month
    emit('update:month', month)
  }
}
</script>

<template>
  <CalendarHeading
    v-slot="{ headingValue }"
    :class="cn('text-sm font-medium', props.class)"
    v-bind="forwardedProps"
  >
    {{ void syncFromHeading(headingValue) }}
    <slot :heading-value>
      <Popover v-model:open="open">
        <PopoverTrigger as-child>
          <button
            type="button"
            :class="cn(
              'inline-flex items-center h-7 px-2 rounded-md text-sm font-medium',
              'border-0 bg-transparent cursor-pointer hover:bg-accent transition-colors'
            )"
          >
            {{ headingValue }}
            <svg
              xmlns="http://www.w3.org/2000/svg"
              width="12"
              height="12"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
              class="ml-1 opacity-50"
            >
              <path d="m6 9 6 6 6-6"/>
            </svg>
          </button>
        </PopoverTrigger>
        <PopoverContent
          class="w-auto p-4"
          align="center"
          @interact-outside="onInteractOutside"
          @focus-outside="onInteractOutside"
        >
          <div class="space-y-3">
            <div>
              <label class="text-xs font-medium text-muted-foreground mb-1.5 block">
                Mês
              </label>
              <Select
                :model-value="selectedMonth.toString()"
                @update:model-value="(val) => handleMonthChange(val as string)"
              >
                <SelectTrigger class="h-8 text-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent class="max-h-[200px]">
                  <SelectItem
                    v-for="month in months"
                    :key="month.value"
                    :value="month.value.toString()"
                  >
                    {{ month.label }}
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div>
              <label class="text-xs font-medium text-muted-foreground mb-1.5 block">
                Ano
              </label>
              <input
                type="number"
                :value="selectedYear"
                min="1900"
                max="2100"
                class="flex h-8 w-full rounded-md border border-input bg-background
                       px-3 py-2 text-xs ring-offset-background
                       placeholder:text-muted-foreground
                       focus-visible:outline-none focus-visible:ring-2
                       focus-visible:ring-ring focus-visible:ring-offset-2
                       [appearance:textfield]
                       [&::-webkit-outer-spin-button]:appearance-none
                       [&::-webkit-inner-spin-button]:appearance-none"
                @input="handleYearInput"
              >
            </div>
          </div>
        </PopoverContent>
      </Popover>
    </slot>
  </CalendarHeading>
</template>
