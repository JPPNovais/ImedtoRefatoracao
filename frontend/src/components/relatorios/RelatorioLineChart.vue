<script setup lang="ts">
/**
 * RelatorioLineChart — gráfico de linha SVG com area fill e linha comparativa opcional.
 * Adaptado de Charts.jsx (design Claude) para Vue 3.
 */
const props = withDefaults(defineProps<{
    dados: number[]
    dadosAnteriores?: number[] | null
    rotulos?: (string | number)[]
    altura?: number
    cor?: string
    formatarY?: (v: number) => string
}>(), {
    dadosAnteriores: null,
    altura: 240,
    cor: 'hsl(var(--primary))',
    formatarY: (v: number) => {
        if (v >= 1000) return `${(v / 1000).toFixed(v >= 10000 ? 0 : 1)}k`
        return String(v)
    },
})

function calcularPontos() {
    const W = 100
    const H = props.altura
    const padding = { t: 16, r: 12, b: 28, l: 48 }

    const todos = [...props.dados, ...(props.dadosAnteriores ?? [])]
    const max = Math.max(...todos) * 1.1
    const min = 0
    const range = max - min || 1

    const xStep = W / (props.dados.length - 1)
    const y = (v: number) => padding.t + (1 - (v - min) / range) * (H - padding.t - padding.b)
    const x = (i: number) => i * xStep

    const linePts = props.dados.map((v, i) => `${x(i)},${y(v)}`).join(' ')
    const areaPts = `0,${H - padding.b} ${linePts} ${x(props.dados.length - 1)},${H - padding.b}`
    const prevPts = props.dadosAnteriores
        ? props.dadosAnteriores.map((v, i) => `${x(i)},${y(v)}`).join(' ')
        : null

    const ticks = [0, 0.25, 0.5, 0.75, 1].map(t => max * t)

    return { W, H, padding, x, y, linePts, areaPts, prevPts, ticks, max, xStep }
}

function viewBox() {
    const { W, padding } = calcularPontos()
    return `0 0 ${W + padding.l + padding.r} ${props.altura}`
}
</script>

<template>
    <div class="rp-chart">
        <svg
            :viewBox="viewBox()"
            preserveAspectRatio="none"
            class="rp-chart-svg"
            :aria-label="`Gráfico de linha com ${dados.length} pontos`"
            role="img"
        >
            <g :transform="`translate(${calcularPontos().padding.l}, 0)`">
                <!-- Y grid + labels -->
                <g v-for="(tick, i) in calcularPontos().ticks" :key="i">
                    <line
                        :x1="0"
                        :x2="calcularPontos().W"
                        :y1="calcularPontos().y(tick)"
                        :y2="calcularPontos().y(tick)"
                        class="rp-chart-grid"
                    />
                    <text
                        :x="-6"
                        :y="calcularPontos().y(tick) + 3"
                        text-anchor="end"
                        class="rp-chart-axis-label"
                    >{{ formatarY(tick) }}</text>
                </g>
                <!-- Linha anterior (tracejada) -->
                <polyline
                    v-if="calcularPontos().prevPts"
                    :points="calcularPontos().prevPts!"
                    fill="none"
                    stroke="hsl(var(--muted-foreground))"
                    stroke-width="0.5"
                    stroke-dasharray="1.2 1.2"
                    vector-effect="non-scaling-stroke"
                />
                <!-- Area fill -->
                <polygon
                    :points="calcularPontos().areaPts"
                    :fill="cor"
                    opacity="0.1"
                />
                <!-- Linha atual -->
                <polyline
                    :points="calcularPontos().linePts"
                    fill="none"
                    :stroke="cor"
                    stroke-width="1"
                    vector-effect="non-scaling-stroke"
                />
                <!-- Ponto final -->
                <circle
                    :cx="calcularPontos().x(dados.length - 1)"
                    :cy="calcularPontos().y(dados[dados.length - 1])"
                    r="0.8"
                    :fill="cor"
                    vector-effect="non-scaling-stroke"
                />
            </g>
            <!-- X labels: primeiro, meio, último -->
            <g v-if="rotulos && rotulos.length">
                <text
                    :x="calcularPontos().padding.l"
                    :y="altura - 8"
                    text-anchor="start"
                    class="rp-chart-axis-label"
                >{{ rotulos[0] }}</text>
                <text
                    :x="calcularPontos().padding.l + (calcularPontos().W * 0.5)"
                    :y="altura - 8"
                    text-anchor="middle"
                    class="rp-chart-axis-label"
                >{{ rotulos[Math.floor(rotulos.length / 2)] }}</text>
                <text
                    :x="calcularPontos().padding.l + calcularPontos().W"
                    :y="altura - 8"
                    text-anchor="end"
                    class="rp-chart-axis-label"
                >{{ rotulos[rotulos.length - 1] }}</text>
            </g>
        </svg>

        <div v-if="dadosAnteriores" class="rp-chart-legenda">
            <span class="rp-chart-legenda-item">
                <i class="rp-chart-legenda-cor" :style="{ background: cor }"></i>
                Período atual
            </span>
            <span class="rp-chart-legenda-item">
                <i class="rp-chart-legenda-cor rp-chart-legenda-cor--tracejado"></i>
                Período anterior
            </span>
        </div>
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
.rp-chart-axis-label {
    font-size: 7px;
    fill: hsl(var(--muted-foreground));
}
.rp-chart-legenda {
    display: flex;
    gap: 20px;
    justify-content: center;
    margin-top: 8px;
    font-size: 12px;
    color: hsl(var(--muted-foreground));
}
.rp-chart-legenda-item {
    display: flex;
    align-items: center;
    gap: 6px;
}
.rp-chart-legenda-cor {
    display: inline-block;
    width: 14px;
    height: 3px;
    border-radius: 2px;
}
.rp-chart-legenda-cor--tracejado {
    background: repeating-linear-gradient(90deg, hsl(var(--muted-foreground)) 0 3px, transparent 3px 6px);
}
</style>
