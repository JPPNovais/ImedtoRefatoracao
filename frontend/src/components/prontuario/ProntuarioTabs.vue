<!--
    Abas do prontuário — visual do design Imedto care:
    botões com ícone FontAwesome + label + badge de contagem,
    underline na ativa, header sticky abaixo do header do paciente.
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
    contagemReceitas?: number
}>()

defineEmits<{
    "update:modelValue": [aba: AbaProntuario]
}>()
</script>

<template>
    <nav class="pront-tabs" role="tablist" aria-label="Abas do prontuário">
        <button
            type="button"
            role="tab"
            :class="{ active: modelValue === 'consulta' }"
            :aria-selected="modelValue === 'consulta'"
            @click="$emit('update:modelValue', 'consulta')"
        >
            <i class="fa-solid fa-file-medical"></i>
            <span>Consulta atual</span>
        </button>

        <button
            type="button"
            role="tab"
            :class="{ active: modelValue === 'anteriores' }"
            :aria-selected="modelValue === 'anteriores'"
            @click="$emit('update:modelValue', 'anteriores')"
        >
            <i class="fa-solid fa-clock-rotate-left"></i>
            <span>Consultas anteriores</span>
            <span v-if="contagemAnteriores !== undefined" class="tab-badge">{{ contagemAnteriores }}</span>
        </button>

        <button
            type="button"
            role="tab"
            :class="{ active: modelValue === 'exame' }"
            :aria-selected="modelValue === 'exame'"
            @click="$emit('update:modelValue', 'exame')"
        >
            <i class="fa-solid fa-person"></i>
            <span>Exame físico</span>
        </button>

        <button
            type="button"
            role="tab"
            :class="{ active: modelValue === 'receitas' }"
            :aria-selected="modelValue === 'receitas'"
            @click="$emit('update:modelValue', 'receitas')"
        >
            <i class="fa-solid fa-prescription"></i>
            <span>Receitas</span>
            <span v-if="contagemReceitas !== undefined" class="tab-badge">{{ contagemReceitas }}</span>
        </button>
    </nav>
</template>

<style scoped>
.pront-tabs {
    position: sticky;
    /* fica logo abaixo do header sticky do paciente — o header tem ~96px com padding/borda */
    top: calc(var(--topbar-h, var(--top-h)) + 96px);
    z-index: 15;
    display: flex;
    gap: 4px;
    background: white;
    padding: 0 6px;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-lg);
    margin-bottom: 16px;
    box-shadow: var(--shadow-sm);
    overflow-x: auto;
}
.pront-tabs button {
    display: inline-flex; align-items: center; gap: 8px;
    padding: 12px 18px;
    border: 0; background: transparent;
    font: inherit; font-size: 13px; font-weight: 600;
    color: hsl(var(--secondary) / 0.6);
    cursor: pointer; position: relative;
    border-bottom: 2px solid transparent;
    transition: color 150ms;
    white-space: nowrap;
}
.pront-tabs button:hover { color: hsl(var(--primary)); }
.pront-tabs button.active {
    color: hsl(var(--primary));
    border-bottom-color: hsl(var(--primary));
}
.pront-tabs button i { font-size: 13px; }

.tab-badge {
    background: hsl(var(--secondary) / 0.08);
    color: hsl(var(--secondary) / 0.7);
    font-size: 10px; font-weight: 700;
    padding: 2px 6px; border-radius: 99px;
    min-width: 18px; text-align: center;
}
.pront-tabs button.active .tab-badge {
    background: hsl(var(--primary) / 0.14);
    color: hsl(var(--primary));
}

@media (max-width: 640px) {
    .pront-tabs button { padding: 10px 12px; font-size: 12px; }
    .pront-tabs button span:not(.tab-badge) { display: none; }
}
</style>
