<script setup lang="ts">
/**
 * AppKpiCard — card de KPI com borda superior colorida e chip de ícone.
 *
 * Usado na aba "Visão geral" do Financeiro e em qualquer tela que precise
 * exibir métrica primária com identidade visual por categoria.
 *
 * Uso:
 *   <AppKpiCard
 *     label="Recebido"
 *     :valor="moeda(kpis.recebido)"
 *     icone="fa-solid fa-arrow-down-long"
 *     variante="success"
 *   />
 *
 * Variantes de cor: "success" | "warning" | "error" | "primary" | "muted"
 * Mapeiam diretamente para tokens HSL do design system.
 */
type Variante = "success" | "warning" | "error" | "primary" | "muted"

const props = defineProps<{
    label: string
    valor: string | number
    icone?: string
    variante?: Variante
    sub?: string
}>()
</script>

<template>
    <div class="kpi-card" :class="`kpi--${variante ?? 'muted'}`">
        <div class="kpi-header">
            <span class="kpi-chip">
                <i v-if="icone" :class="icone" aria-hidden="true" />
            </span>
            <span class="kpi-label">{{ label }}</span>
        </div>
        <div class="kpi-value">{{ valor }}</div>
        <div v-if="sub" class="kpi-sub">{{ sub }}</div>
    </div>
</template>

<style scoped>
.kpi-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
    padding: 16px 18px;
    /* Borda superior colorida — sobrescrita por variante */
    border-top: 3px solid hsl(var(--secondary) / 0.15);
}

/* Variantes de borda superior + chip */
.kpi--success { border-top-color: hsl(var(--success)); }
.kpi--warning { border-top-color: hsl(var(--warning)); }
.kpi--error   { border-top-color: hsl(var(--destructive)); }
.kpi--primary { border-top-color: hsl(var(--primary)); }
.kpi--muted   { border-top-color: hsl(var(--secondary) / 0.15); }

.kpi-header {
    display: flex;
    align-items: center;
    gap: 9px;
    margin-bottom: 10px;
}

.kpi-chip {
    width: 30px;
    height: 30px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    /* Fundo padrão, sobrescrito por variante */
    background: hsl(var(--secondary) / 0.06);
    color: hsl(var(--secondary) / 0.6);
    font-size: var(--text-sm);
}

.kpi--success .kpi-chip { background: hsl(var(--success) / 0.12); color: hsl(var(--success)); }
.kpi--warning .kpi-chip { background: hsl(var(--warning) / 0.14); color: hsl(28 90% 42%); }
.kpi--error   .kpi-chip { background: hsl(var(--destructive) / 0.1); color: hsl(var(--destructive)); }
.kpi--primary .kpi-chip { background: hsl(var(--primary) / 0.1); color: hsl(var(--primary)); }

.kpi-label {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--secondary) / 0.65);
}

.kpi-value {
    font-size: var(--text-2xl);
    font-weight: var(--font-weight-extrabold);
    color: var(--c-primary-dark);
    line-height: 1.1;
}

/* Valor em vermelho para variante error */
.kpi--error .kpi-value { color: hsl(var(--destructive)); }

.kpi-sub {
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.55);
    margin-top: 4px;
}
</style>
