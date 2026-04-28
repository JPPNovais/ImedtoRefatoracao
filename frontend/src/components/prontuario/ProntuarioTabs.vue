<!--
    Abas do prontuário estilo legado (`TabNavigation.vue`).
    Underline abaixo da ativa (linha colorida), contador opcional em cada aba,
    badge "IA" no Assistente.
-->
<script setup lang="ts">
export type AbaProntuario =
    | "consulta"
    | "anteriores"
    | "exame"
    | "receitas"

defineProps<{
    modelValue: AbaProntuario
    contagemAnteriores?: number
}>()

defineEmits<{
    "update:modelValue": [aba: AbaProntuario]
}>()
</script>

<template>
    <nav class="tabs">
        <button
            type="button"
            class="tab"
            :class="{ ativa: modelValue === 'consulta' }"
            @click="$emit('update:modelValue', 'consulta')"
        >
            <span class="tab-label">Consulta atual</span>
        </button>

        <button
            type="button"
            class="tab"
            :class="{ ativa: modelValue === 'anteriores' }"
            @click="$emit('update:modelValue', 'anteriores')"
        >
            <span class="tab-label">Consultas anteriores</span>
            <span class="tab-contador">{{ contagemAnteriores ?? 0 }}</span>
        </button>

        <button
            type="button"
            class="tab"
            :class="{ ativa: modelValue === 'exame' }"
            @click="$emit('update:modelValue', 'exame')"
        >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="tab-icone">
                <path d="M6 2v6a4 4 0 0 0 8 0V2" />
                <circle cx="10" cy="14" r="1" />
                <path d="M10 15v2a5 5 0 0 0 10 0v-2" />
                <circle cx="20" cy="15" r="2" />
            </svg>
            <span class="tab-label">Exame físico</span>
        </button>

        <button
            type="button"
            class="tab"
            :class="{ ativa: modelValue === 'receitas' }"
            @click="$emit('update:modelValue', 'receitas')"
        >
            <span class="tab-icone-rx">Rx</span>
            <span class="tab-label">Receitas</span>
        </button>
    </nav>
</template>

<style scoped>
.tabs {
    display: flex; gap: 0; align-items: stretch;
    border-bottom: 1px solid var(--border);
    margin-bottom: 1.25rem; flex-wrap: wrap;
}

.tab {
    display: inline-flex; align-items: center; gap: 0.45rem;
    padding: 0.85rem 1.1rem; background: none; border: none; cursor: pointer;
    font-family: inherit; font-size: 0.88em; font-weight: 500;
    color: var(--text-muted); white-space: nowrap;
    border-bottom: 2px solid transparent; margin-bottom: -1px;
    transition: color 0.12s, border-color 0.12s;
}
.tab:hover:not(.ativa) { color: var(--text); }
.tab.ativa {
    color: hsl(var(--primary)); font-weight: 700;
    border-bottom-color: hsl(var(--primary));
}

.tab-label { line-height: 1; }

.tab-icone { flex-shrink: 0; opacity: 0.9; }

.tab-icone-rx {
    font-style: italic; font-weight: 700; font-size: 0.95em;
    line-height: 1; letter-spacing: -0.05em;
}

.tab-contador {
    display: inline-flex; align-items: center; justify-content: center;
    min-width: 22px; height: 22px; padding: 0 0.45rem;
    border-radius: 999px; background: hsl(var(--primary) / 0.1);
    color: var(--text-muted);
    font-size: 0.72em; font-weight: 600;
}
.tab.ativa .tab-contador {
    background: hsl(var(--primary) / 0.15); color: hsl(var(--primary-dark));
}

.badge-ia {
    display: inline-flex; align-items: center; justify-content: center;
    padding: 0.12rem 0.45rem; border-radius: 999px;
    background: hsl(210 90% 55% / 0.12); color: hsl(210 90% 45%);
    font-size: 0.65em; font-weight: 700; letter-spacing: 0.05em;
}
</style>
