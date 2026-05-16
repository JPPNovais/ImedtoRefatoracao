<script setup lang="ts">
interface AppSelectOption {
    value: string | number
    label: string
}

defineProps<{
    modelValue?:  string | number | null
    disabled?:    boolean
    class?:       string
    options?:     ReadonlyArray<AppSelectOption>
}>()

const emit = defineEmits<{ "update:modelValue": [string | number] }>()
</script>

<template>
    <select
        :value="modelValue ?? ''"
        :disabled="disabled"
        class="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
        :class="$props.class"
        @change="emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
    >
        <template v-if="options && options.length">
            <option v-for="o in options" :key="o.value" :value="o.value">{{ o.label }}</option>
        </template>
        <slot v-else />
    </select>
</template>
