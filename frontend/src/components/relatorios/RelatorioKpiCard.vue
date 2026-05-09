<script setup lang="ts">
/**
 * RelatorioKpiCard — card de KPI com delta (variação vs período anterior).
 * Complementa AppStatCard com suporte a variação percentual e acento visual.
 */
export type AcentoKpi = 'ok' | 'warn' | 'bad' | 'default'

const props = withDefaults(defineProps<{
    icone: string
    label: string
    valor: string
    valorAnterior?: number | null
    valorNumerico?: number | null
    sub?: string
    acento?: AcentoKpi
    inverter?: boolean
}>(), {
    valorAnterior: null,
    valorNumerico: null,
    acento: 'default',
    inverter: false,
})

function calcularDelta(): { texto: string; tipo: 'cima' | 'baixo' | 'plano' } | null {
    const curr = props.valorNumerico
    const prev = props.valorAnterior
    if (curr == null || prev == null || prev === 0) return null

    const diff = curr - prev
    const pct = (diff / prev) * 100
    const isUp = diff > 0
    const bom = props.inverter ? !isUp : isUp

    return {
        texto: `${pct >= 0 ? '+' : ''}${pct.toFixed(1)}%`,
        tipo: diff === 0 ? 'plano' : (bom ? 'cima' : 'baixo'),
    }
}

const delta = calcularDelta()
</script>

<template>
    <div class="rp-kpi" :class="`rp-kpi--${acento}`">
        <div class="rp-kpi-cabecalho">
            <span class="rp-kpi-icone">
                <i :class="`fa-solid ${icone}`" aria-hidden="true"></i>
            </span>
            <span class="rp-kpi-lbl">{{ label }}</span>
        </div>
        <div class="rp-kpi-valor">{{ valor }}</div>
        <div class="rp-kpi-rodape">
            <span
                v-if="delta"
                class="rp-delta"
                :class="`rp-delta--${delta.tipo}`"
                :aria-label="`Variação: ${delta.texto}`"
            >
                <i
                    :class="`fa-solid ${delta.tipo === 'plano' ? 'fa-equals' : delta.tipo === 'cima' ? 'fa-arrow-trend-up' : 'fa-arrow-trend-down'}`"
                    aria-hidden="true"
                ></i>
                {{ delta.texto }}
            </span>
            <span v-if="sub" class="rp-kpi-sub">{{ sub }}</span>
        </div>
    </div>
</template>

<style scoped>
.rp-kpi {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 12px;
    padding: 16px;
    transition: transform 0.15s, box-shadow 0.15s;
}
.rp-kpi:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 12px hsl(var(--foreground) / 0.06);
}
.rp-kpi--ok {
    border-color: hsl(160 70% 80%);
    background: linear-gradient(180deg, hsl(160 70% 98%), hsl(var(--card)));
}
.rp-kpi--warn {
    border-color: hsl(35 90% 80%);
    background: linear-gradient(180deg, hsl(35 95% 98%), hsl(var(--card)));
}
.rp-kpi--bad {
    border-color: hsl(0 70% 85%);
    background: linear-gradient(180deg, hsl(0 80% 99%), hsl(var(--card)));
}

.rp-kpi-cabecalho {
    display: flex;
    gap: 8px;
    align-items: center;
    margin-bottom: 10px;
}
.rp-kpi-icone {
    width: 32px;
    height: 32px;
    border-radius: 8px;
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 14px;
    flex-shrink: 0;
}
.rp-kpi--ok .rp-kpi-icone   { background: hsl(160 70% 92%); color: hsl(160 70% 30%); }
.rp-kpi--warn .rp-kpi-icone { background: hsl(35 95% 92%);  color: hsl(35 90% 35%); }
.rp-kpi--bad .rp-kpi-icone  { background: hsl(0 80% 95%);   color: hsl(0 70% 50%); }

.rp-kpi-lbl {
    font-size: 12px;
    font-weight: 500;
    color: hsl(var(--muted-foreground));
    line-height: 1.3;
}
.rp-kpi-valor {
    font-size: 24px;
    font-weight: 700;
    color: hsl(var(--foreground));
    margin-bottom: 8px;
    line-height: 1.1;
    font-variant-numeric: tabular-nums;
}
.rp-kpi-rodape {
    display: flex;
    gap: 8px;
    align-items: center;
    flex-wrap: wrap;
    font-size: 11.5px;
}
.rp-kpi-sub { color: hsl(var(--muted-foreground)); }

.rp-delta {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    padding: 2px 7px;
    border-radius: 999px;
    font-size: 11px;
    font-weight: 600;
}
.rp-delta i { font-size: 9px; }
.rp-delta--cima  { background: hsl(160 70% 92%); color: hsl(160 70% 28%); }
.rp-delta--baixo { background: hsl(0 80% 95%);   color: hsl(0 70% 45%); }
.rp-delta--plano { background: hsl(var(--muted)); color: hsl(var(--muted-foreground)); }
</style>
