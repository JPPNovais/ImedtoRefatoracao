<script setup lang="ts">
/**
 * AppTabs — tabs genérico do design system. 3 variantes:
 *   - "main"      → botão pílula com fundo primary (default).
 *   - "sub"       → chip pequeno arredondado.
 *   - "underline" → sublinhado abaixo da ativa (estilo legado/Prontuário).
 *                   Use em sub-navegação dentro de uma view.
 */
interface Tab<T = string> {
    valor: T
    label: string
    icone?: string
}

interface Props {
    modelValue: string | number
    abas: Tab[]
    variante?: "main" | "sub" | "underline"
}

const props = withDefaults(defineProps<Props>(), {
    variante: "main",
})

const emit = defineEmits<{
    "update:modelValue": [value: string | number]
}>()

function classeBase(): string {
    if (props.variante === "sub") return "tab-sub"
    if (props.variante === "underline") return "tab-underline"
    return "tab-main"
}
function classeAtiva(): string {
    if (props.variante === "sub") return "tab-sub-active"
    if (props.variante === "underline") return "tab-underline-active"
    return "tab-main-active"
}
</script>

<template>
    <nav class="tabs-nav" :class="`tabs-nav--${variante}`" role="tablist" :aria-label="$attrs['aria-label'] as string">
        <button
            v-for="aba in abas"
            :key="String(aba.valor)"
            role="tab"
            :aria-selected="modelValue === aba.valor"
            :class="[classeBase(), modelValue === aba.valor ? classeAtiva() : '']"
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
/* Underline: sem gap entre botões, borda inferior contínua. */
.tabs-nav--underline {
    gap: 0;
    align-items: stretch;
    border-bottom: 1px solid var(--border);
    flex-wrap: wrap;
}
</style>
