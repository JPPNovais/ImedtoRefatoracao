<script setup lang="ts">
/**
 * RelatorioDonutChart — donut SVG com legenda lateral opcional.
 */
export interface DonutFatia {
    label: string
    valor: number
    cor: string
    share?: number
}

const props = withDefaults(defineProps<{
    fatias: DonutFatia[]
    tamanho?: number
    espessura?: number
    valorCentral?: string
    legendaCentral?: string
    mostrarLegenda?: boolean
    formatarValor?: (v: number) => string
}>(), {
    tamanho: 160,
    espessura: 24,
    mostrarLegenda: true,
    formatarValor: (v: number) => v.toLocaleString('pt-BR'),
})

function calcularArcos() {
    const total = props.fatias.reduce((s, f) => s + f.valor, 0) || 1
    const r = props.tamanho / 2 - props.espessura / 2
    const cx = props.tamanho / 2
    const cy = props.tamanho / 2
    const circ = 2 * Math.PI * r
    let acc = 0

    return props.fatias.map(f => {
        const frac = f.valor / total
        const dash = frac * circ
        const offset = -(acc * circ)
        acc += frac
        return { ...f, dash, circ, offset, r, cx, cy }
    })
}
</script>

<template>
    <div class="rp-donut-container">
        <svg
            :width="tamanho"
            :height="tamanho"
            :viewBox="`0 0 ${tamanho} ${tamanho}`"
            role="img"
            aria-label="Gráfico donut"
        >
            <!-- Track de fundo -->
            <circle
                :cx="tamanho / 2"
                :cy="tamanho / 2"
                :r="tamanho / 2 - espessura / 2"
                fill="none"
                stroke="hsl(var(--muted))"
                :stroke-width="espessura"
            />
            <!-- Arcos -->
            <circle
                v-for="(arco, i) in calcularArcos()"
                :key="i"
                :cx="arco.cx"
                :cy="arco.cy"
                :r="arco.r"
                fill="none"
                :stroke="arco.cor"
                :stroke-width="espessura"
                :stroke-dasharray="`${arco.dash} ${arco.circ - arco.dash}`"
                :stroke-dashoffset="arco.offset"
                :transform="`rotate(-90 ${arco.cx} ${arco.cy})`"
                stroke-linecap="butt"
            />
            <!-- Textos centrais -->
            <text
                v-if="valorCentral"
                :x="tamanho / 2"
                :y="tamanho / 2 - 4"
                text-anchor="middle"
                class="rp-donut-valor"
            >{{ valorCentral }}</text>
            <text
                v-if="legendaCentral"
                :x="tamanho / 2"
                :y="tamanho / 2 + 14"
                text-anchor="middle"
                class="rp-donut-legenda"
            >{{ legendaCentral }}</text>
        </svg>

        <!-- Legenda lateral -->
        <div v-if="mostrarLegenda" class="rp-donut-lista">
            <div v-for="(f, i) in fatias" :key="i" class="rp-donut-lista-row">
                <span class="rp-dot" :style="{ background: f.cor }"></span>
                <span class="rp-donut-lista-lbl">{{ f.label }}</span>
                <span v-if="f.share" class="rp-donut-lista-pct">{{ f.share }}%</span>
                <span class="rp-donut-lista-val">{{ formatarValor(f.valor) }}</span>
            </div>
        </div>
    </div>
</template>

<style scoped>
.rp-donut-container {
    display: flex;
    align-items: center;
    gap: 24px;
    flex-wrap: wrap;
}
.rp-donut-valor {
    font-size: 13px;
    font-weight: 700;
    fill: hsl(var(--foreground));
}
.rp-donut-legenda {
    font-size: 8px;
    fill: hsl(var(--muted-foreground));
}
.rp-donut-lista {
    flex: 1;
    min-width: 180px;
    display: flex;
    flex-direction: column;
    gap: 6px;
}
.rp-donut-lista-row {
    display: grid;
    grid-template-columns: 12px 1fr auto auto;
    gap: 8px;
    align-items: center;
    font-size: 12.5px;
    padding: 5px 0;
    border-bottom: 1px solid hsl(var(--border) / 0.5);
}
.rp-donut-lista-row:last-child { border-bottom: 0; }
.rp-dot {
    display: inline-block;
    width: 10px;
    height: 10px;
    border-radius: 3px;
    flex-shrink: 0;
}
.rp-donut-lista-lbl { color: hsl(var(--foreground)); }
.rp-donut-lista-pct { font-weight: 600; color: hsl(var(--primary)); }
.rp-donut-lista-val { color: hsl(var(--muted-foreground)); font-size: 11.5px; }
</style>
