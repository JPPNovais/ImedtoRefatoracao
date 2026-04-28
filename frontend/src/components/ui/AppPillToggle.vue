<script setup lang="ts" generic="T extends string | number">
import { PillToggle } from "@imedto/ui"

defineProps<{
    modelValue: T
    opcoes: Array<{ valor: T; label: string; icone?: string; icon?: string }>
}>()

const emit = defineEmits<{ "update:modelValue": [valor: T] }>()
</script>

<template>
    <PillToggle
        :model-value="modelValue"
        :opcoes="opcoes.map(o => ({ valor: o.valor, label: o.label, icon: o.icon ?? o.icone }))"
        @update:model-value="emit('update:modelValue', $event)"
    />
</template>
