<script setup lang="ts">
/**
 * AppTabs — tabs genérico do design system.
 * Usa a classe .tab-main / .tab-main-active do main.css.
 * Suporta abas por valor (generic T).
 */
interface Tab<T = string> {
    valor: T
    label: string
    icone?: string
}

interface Props {
    modelValue: string | number
    abas: Tab[]
    variante?: "main" | "sub"
}

const props = withDefaults(defineProps<Props>(), {
    variante: "main",
})

const emit = defineEmits<{
    "update:modelValue": [value: string | number]
}>()
</script>

<template>
    <nav class="tabs-nav" role="tablist" :aria-label="$attrs['aria-label'] as string">
        <button
            v-for="aba in abas"
            :key="String(aba.valor)"
            role="tab"
            :aria-selected="modelValue === aba.valor"
            :class="[
                variante === 'sub' ? 'tab-sub' : 'tab-main',
                modelValue === aba.valor ? (variante === 'sub' ? 'tab-sub-active' : 'tab-main-active') : '',
            ]"
            @click="emit('update:modelValue', aba.valor)"
        >
            <i v-if="aba.icone" :class="aba.icone" aria-hidden="true"></i>
            {{ aba.label }}
        </button>
    </nav>
</template>

<style scoped>
.tabs-nav {
    display: flex;
    gap: 0.25rem;
    flex-wrap: wrap;
}
</style>
