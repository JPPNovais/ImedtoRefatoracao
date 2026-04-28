<script setup lang="ts">
import { Textarea } from "@imedto/ui"

defineProps<{
    modelValue?:  string | number | null
    placeholder?: string
    disabled?:    boolean
    readonly?:    boolean
    rows?:        number
    class?:       string
}>()

const emit = defineEmits<{ "update:modelValue": [string | number] }>()
</script>

<template>
    <Textarea
        v-bind="$props"
        :model-value="modelValue ?? undefined"
        @update:model-value="emit('update:modelValue', $event)"
    />
</template>
