<script setup lang="ts">
/**
 * Pílula de status do item de estoque.
 * status: "ok" | "low" | "out" | "warn" | "expiring" | "expired"
 */
const STATUS_MAP = {
    ok:       { label: "Normal",      cor: "hsl(160 79% 39%)",  bg: "hsl(160 79% 95%)" },
    low:      { label: "Estoque baixo", cor: "hsl(0 70% 45%)",  bg: "hsl(0 75% 95%)" },
    out:      { label: "Esgotado",    cor: "hsl(0 70% 45%)",    bg: "hsl(0 75% 95%)" },
    warn:     { label: "Atenção",     cor: "hsl(40 90% 38%)",   bg: "hsl(45 95% 94%)" },
    expiring: { label: "Vencendo",    cor: "hsl(35 95% 40%)",   bg: "hsl(40 95% 94%)" },
    expired:  { label: "Vencido",     cor: "hsl(0 70% 40%)",    bg: "hsl(0 75% 95%)" },
} as const

type StatusKey = keyof typeof STATUS_MAP

const props = defineProps<{
    status: StatusKey | string
}>()

function cfg() {
    return STATUS_MAP[props.status as StatusKey] ?? STATUS_MAP.ok
}
</script>

<template>
    <span
        class="status-pill"
        :style="{ background: cfg().bg, color: cfg().cor }"
    >
        {{ cfg().label }}
    </span>
</template>

<style scoped>
.status-pill {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    font-size: 11px;
    font-weight: 700;
    padding: 3px 9px;
    border-radius: 999px;
    white-space: nowrap;
}
.status-pill::before {
    content: '';
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: currentColor;
    flex-shrink: 0;
}
</style>
