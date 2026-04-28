<script setup lang="ts">
import { Input } from "@imedto/ui"

defineProps<{
    modelValue?:   string | number | null
    type?:         string
    placeholder?:  string
    disabled?:     boolean
    readonly?:     boolean
    autocomplete?: string
    min?:          string | number
    max?:          string | number
    step?:         string | number
    class?:        string
}>()

const emit = defineEmits<{ "update:modelValue": [string | number] }>()
</script>

<template>
    <Input
        v-bind="$props"
        :model-value="modelValue ?? undefined"
        @update:model-value="emit('update:modelValue', $event)"
    />
</template>
