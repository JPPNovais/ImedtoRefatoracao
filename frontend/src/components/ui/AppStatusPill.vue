<script setup lang="ts">
/**
 * Pill de status com pílula colorida + dot. Variantes alinhadas ao design
 * system (success/warning/error/muted). Usado em listas de profissionais,
 * pacientes etc.
 */
defineProps<{
    label: string
    variante: "success" | "warning" | "error" | "muted"
}>()
</script>

<template>
    <span class="status-pill" :class="`pill-${variante}`">
        <span class="dot"></span>
        {{ label }}
    </span>
</template>

<style scoped>
.status-pill {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 4px 10px;
    border-radius: 999px;
    font-size: 11px;
    font-weight: 600;
    line-height: 1.2;
    white-space: nowrap;
}
.dot { width: 7px; height: 7px; border-radius: 50%; flex-shrink: 0; }
.pill-success { background: hsl(var(--success) / 0.1);    color: hsl(160 79% 30%); }
.pill-success .dot { background: hsl(var(--success)); }
.pill-warning { background: hsl(var(--warning) / 0.15);   color: hsl(40 95% 35%); }
.pill-warning .dot { background: hsl(var(--warning)); }
.pill-error   { background: hsl(var(--error) / 0.1);      color: hsl(var(--error)); }
.pill-error   .dot { background: hsl(var(--error)); }
.pill-muted   { background: hsl(var(--secondary) / 0.06); color: hsl(var(--secondary) / 0.7); }
.pill-muted   .dot { background: hsl(var(--secondary) / 0.4); }
</style>
