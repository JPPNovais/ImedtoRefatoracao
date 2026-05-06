<script setup lang="ts" generic="T extends string | number">
/**
 * Barra de pills de filtro mutuamente exclusivos. Cada pill tem chave (valor),
 * label, contador opcional e cor de "dot" (success/error/muted/warning).
 *
 * Usado para filtrar listas (status, tags, tipo, etc.).
 */
type Cor = "success" | "warning" | "error" | "muted" | "info"

defineProps<{
    modelValue: T
    opcoes: Array<{ valor: T, label: string, count?: number, dot?: Cor }>
}>()

const emit = defineEmits<{
    (e: "update:modelValue", v: T): void
}>()
</script>

<template>
    <div class="filter-pills">
        <button
            v-for="op in opcoes"
            :key="String(op.valor)"
            type="button"
            class="fp"
            :class="{ active: modelValue === op.valor }"
            @click="emit('update:modelValue', op.valor)"
        >
            <span v-if="op.dot" class="dot" :class="`dot-${op.dot}`"></span>
            {{ op.label }}
            <span v-if="op.count !== undefined" class="fp-count">{{ op.count }}</span>
        </button>
    </div>
</template>

<style scoped>
.filter-pills { display: flex; gap: 6px; flex-wrap: wrap; }
.fp {
    display: inline-flex; align-items: center; gap: 6px;
    background: white; border: 1px solid hsl(var(--secondary) / 0.12);
    padding: 8px 12px; border-radius: 999px;
    font-family: inherit; font-size: 12px; font-weight: 600;
    color: hsl(var(--secondary) / 0.7); cursor: pointer;
    transition: all 150ms;
}
.fp:hover { border-color: hsl(var(--primary) / 0.3); color: hsl(var(--primary-dark)); }
.fp.active {
    background: hsl(var(--primary) / 0.08);
    border-color: hsl(var(--primary) / 0.4);
    color: hsl(var(--primary));
}
.fp-count { font-size: 11px; opacity: 0.7; }
.dot { width: 6px; height: 6px; border-radius: 50%; flex-shrink: 0; }
.dot-success { background: hsl(var(--success)); }
.dot-warning { background: hsl(var(--warning)); }
.dot-error   { background: hsl(var(--error)); }
.dot-info    { background: hsl(var(--info)); }
.dot-muted   { background: hsl(var(--secondary) / 0.4); }
</style>
