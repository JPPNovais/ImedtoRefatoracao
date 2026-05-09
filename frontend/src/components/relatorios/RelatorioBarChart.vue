<script setup lang="ts">
/**
 * RelatorioBarChart — gráfico de barras verticais SVG.
 */
export interface BaraDado {
    label: string
    valor: number
    cor?: string
    sub?: string
}

const props = withDefaults(defineProps<{
    dados: BaraDado[]
    altura?: number
    cor?: string
    formatarY?: (v: number) => string
}>(), {
    altura: 220,
    cor: 'hsl(var(--primary))',
    formatarY: (v: number) => v.toLocaleString('pt-BR'),
})

function calcular() {
    const W = 100
    const H = props.altura
    const padding = { t: 14, r: 8, b: 36, l: 40 }

    const max = Math.max(...props.dados.map(d => d.valor)) * 1.1 || 1
    const range = max
    const barW = W / props.dados.length
    const innerW = barW * 0.6
    const offset = (barW - innerW) / 2

    const y = (v: number) => padding.t + (1 - v / range) * (H - padding.t - padding.b)

    return { W, H, padding, max, barW, innerW, offset, y }
}

function viewBox() {
    const { W, padding } = calcular()
    return `0 0 ${W + padding.l + padding.r} ${props.altura}`
}
</script>

<template>
    <div class="rp-chart">
        <svg
            :viewBox="viewBox()"
            preserveAspectRatio="none"
            class="rp-chart-svg"
            role="img"
            :aria-label="`Gráfico de barras com ${dados.length} categorias`"
        >
            <g :transform="`translate(${calcular().padding.l}, 0)`">
                <!-- Y grid + labels -->
                <g v-for="(t, i) in [0, 0.5, 1]" :key="i">
                    <line
                        :x1="0"
                        :x2="calcular().W"
                        :y1="calcular().y(calcular().max * t)"
                        :y2="calcular().y(calcular().max * t)"
                        class="rp-chart-grid"
                    />
                    <text
                        :x="-4"
                        :y="calcular().y(calcular().max * t) + 3"
                        text-anchor="end"
                        class="rp-chart-axis-label"
                    >{{ formatarY(calcular().max * t) }}</text>
                </g>
                <!-- Barras -->
                <g v-for="(d, i) in dados" :key="i">
                    <rect
                        :x="i * calcular().barW + calcular().offset"
                        :y="calcular().y(d.valor)"
                        :width="calcular().innerW"
                        :height="Math.max(0, calcular().H - calcular().padding.b - calcular().y(d.valor))"
                        :fill="d.cor || cor"
                        rx="1"
                    />
                    <text
                        :x="i * calcular().barW + calcular().barW / 2"
                        :y="calcular().H - 18"
                        text-anchor="middle"
                        class="rp-chart-axis-label"
                    >{{ d.label }}</text>
                    <text
                        v-if="d.sub"
                        :x="i * calcular().barW + calcular().barW / 2"
                        :y="calcular().H - 6"
                        text-anchor="middle"
                        class="rp-chart-axis-sub"
                    >{{ d.sub }}</text>
                </g>
            </g>
        </svg>
    </div>
</template>

<style scoped>
.rp-chart { width: 100%; }
.rp-chart-svg { width: 100%; height: auto; display: block; }
.rp-chart-grid {
    stroke: hsl(var(--border));
    stroke-width: 0.3;
    vector-effect: non-scaling-stroke;
}
.rp-chart-axis-label { font-size: 7px; fill: hsl(var(--muted-foreground)); }
.rp-chart-axis-sub   { font-size: 6px; fill: hsl(var(--muted-foreground) / 0.8); }
</style>
