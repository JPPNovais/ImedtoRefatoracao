<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import { computed } from "vue"
import { X } from "lucide-vue-next"
import { cn } from "@/utils/cn"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/select"

const props = withDefaults(defineProps<{
  modelValue: string[]
  options: string[]
  placeholder?: string
  allowCustom?: boolean
  customLabel?: string
  customValue?: string
  disabled?: boolean
  class?: HTMLAttributes["class"]
}>(), {
  placeholder: "Selecione...",
  allowCustom: false,
  customLabel: "Outra (cadastrar nova...)",
  customValue: "__outra__",
})

const emit = defineEmits<{
  "update:modelValue": [value: string[]]
  "custom": []
}>()

const availableOptions = computed(() =>
  props.options.filter(opt => !props.modelValue.includes(opt))
)

function onSelect(val: string) {
  if (!val) return
  if (val === props.customValue) { emit("custom"); return }
  if (!props.modelValue.includes(val)) emit("update:modelValue", [...props.modelValue, val])
}

function remove(item: string) {
  emit("update:modelValue", props.modelValue.filter(v => v !== item))
}
</script>

<template>
  <div :class="cn('space-y-1.5', props.class)">
    <div v-if="modelValue.length" class="flex flex-wrap gap-1.5">
      <div
        v-for="item in modelValue"
        :key="item"
        class="inline-flex items-center gap-1 rounded-md border-transparent bg-primary text-primary-foreground px-2 py-0.5 text-xs font-semibold shadow transition-colors"
      >
        <span>{{ item }}</span>
        <button
          v-if="!disabled"
          type="button"
          class="ml-0.5 rounded-full p-0.5 hover:bg-primary-foreground/20 transition-colors"
          :aria-label="`Remover ${item}`"
          @click="remove(item)"
        >
          <X class="w-2.5 h-2.5" />
        </button>
      </div>
    </div>
    <Select v-if="!disabled" model-value="" @update:model-value="val => onSelect(val as string)">
      <SelectTrigger class="h-8 text-xs">
        <SelectValue :placeholder="placeholder" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem v-for="opt in availableOptions" :key="opt" :value="opt">{{ opt }}</SelectItem>
        <SelectItem v-if="allowCustom" :value="customValue">{{ customLabel }}</SelectItem>
      </SelectContent>
    </Select>
  </div>
</template>
