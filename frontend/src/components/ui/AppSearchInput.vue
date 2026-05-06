<script setup lang="ts">
/**
 * Input de busca padronizado: ícone de lupa à esquerda + botão "limpar" quando
 * há texto. Usado em listas com filtros (Equipe, Pacientes, ...).
 *
 * Aceita `placeholder`, `modelValue` (v-model). Emite `update:modelValue` em
 * tempo real — caller deve aplicar debounce com `useDebouncedRef` quando o
 * valor for usado para chamada de API.
 */
defineProps<{
    modelValue: string
    placeholder?: string
}>()

const emit = defineEmits<{
    (e: "update:modelValue", v: string): void
}>()

function onInput(e: Event) {
    emit("update:modelValue", (e.target as HTMLInputElement).value)
}

function limpar() {
    emit("update:modelValue", "")
}
</script>

<template>
    <div class="app-search-input">
        <i class="fa-solid fa-magnifying-glass"></i>
        <input
            type="text"
            :value="modelValue"
            :placeholder="placeholder"
            @input="onInput"
        />
        <button
            v-if="modelValue"
            type="button"
            class="clear"
            aria-label="Limpar busca"
            @click="limpar"
        >
            <i class="fa-solid fa-xmark"></i>
        </button>
    </div>
</template>

<style scoped>
.app-search-input {
    flex: 1;
    min-width: 280px;
    display: flex;
    align-items: center;
    gap: 10px;
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.12);
    border-radius: var(--radius-lg);
    padding: 0 12px;
    height: 40px;
    transition: all var(--dur-fast, 150ms);
}
.app-search-input:focus-within {
    border-color: hsl(var(--primary) / 0.5);
    box-shadow: 0 0 0 3px hsl(var(--primary) / 0.1);
}
.app-search-input > i {
    color: hsl(var(--secondary) / 0.5);
    font-size: 13px;
}
.app-search-input input {
    flex: 1;
    border: none;
    outline: none;
    background: transparent;
    font-size: 13px;
    font-family: inherit;
    color: hsl(var(--secondary));
}
.app-search-input .clear {
    background: none;
    border: none;
    cursor: pointer;
    color: hsl(var(--secondary) / 0.4);
    padding: 4px;
    display: inline-flex;
}
.app-search-input .clear:hover {
    color: hsl(var(--secondary));
}
</style>
