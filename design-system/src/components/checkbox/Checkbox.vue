<script setup lang="ts">
import type { CheckboxRootProps } from "reka-ui"
import type { HTMLAttributes } from "vue"
import { Check } from "lucide-vue-next"
import { CheckboxIndicator, CheckboxRoot } from "reka-ui"
import { cn } from "@/utils/cn"

type CheckedValue = boolean | 'indeterminate'

const props = defineProps<
  Omit<CheckboxRootProps, 'modelValue'> & {
    class?: HTMLAttributes["class"]
    checked?: CheckedValue
  }
>()

const emits = defineEmits<{
  'update:checked': [value: CheckedValue]
}>()

function onUpdate(val: CheckedValue) {
  emits('update:checked', val)
}
</script>

<template>
  <CheckboxRoot
    :model-value="checked"
    :default-value="defaultValue"
    :disabled="disabled"
    :required="required"
    :name="name"
    :value="value"
    :id="id"
    @update:model-value="onUpdate"
    :class="cn('grid place-content-center peer h-4 w-4 shrink-0 rounded-sm border border-primary shadow focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50 data-[state=checked]:bg-primary data-[state=checked]:text-primary-foreground', props.class)"
  >
    <CheckboxIndicator class="grid place-content-center text-current">
      <slot>
        <Check class="h-4 w-4" />
      </slot>
    </CheckboxIndicator>
  </CheckboxRoot>
</template>
