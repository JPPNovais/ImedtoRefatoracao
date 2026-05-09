<script setup lang="ts">
import type { OrcamentoStatus } from "@/services/orcamentoService"

defineProps<{ status: OrcamentoStatus }>()

const CONFIG: Record<OrcamentoStatus, { label: string; variante: "success" | "warning" | "error" | "muted" | "info" }> = {
    Rascunho:  { label: "Rascunho",  variante: "muted" },
    Enviado:   { label: "Enviado",   variante: "info" },
    Aprovado:  { label: "Aprovado",  variante: "success" },
    Recusado:  { label: "Recusado",  variante: "error" },
    Cancelado: { label: "Cancelado", variante: "error" },
    Expirado:  { label: "Expirado",  variante: "warning" },
}
</script>

<template>
    <span class="orc-status-pill" :class="`pill-${CONFIG[status].variante}`">
        <span class="dot"></span>
        {{ CONFIG[status].label }}
    </span>
</template>

<style scoped>
.orc-status-pill {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    padding: 3px 9px;
    border-radius: 999px;
    font-size: 11px;
    font-weight: 600;
    white-space: nowrap;
}
.dot { width: 6px; height: 6px; border-radius: 50%; flex-shrink: 0; }
.pill-success { background: hsl(var(--success) / 0.1); color: hsl(160 79% 30%); }
.pill-success .dot { background: hsl(var(--success)); }
.pill-info    { background: hsl(var(--info) / 0.12); color: hsl(199 70% 30%); }
.pill-info    .dot { background: hsl(var(--info)); }
.pill-warning { background: hsl(var(--warning) / 0.15); color: hsl(40 95% 35%); }
.pill-warning .dot { background: hsl(var(--warning)); }
.pill-error   { background: hsl(var(--error) / 0.1); color: hsl(var(--error)); }
.pill-error   .dot { background: hsl(var(--error)); }
.pill-muted   { background: hsl(var(--secondary) / 0.06); color: hsl(var(--secondary) / 0.7); }
.pill-muted   .dot { background: hsl(var(--secondary) / 0.35); }
</style>
