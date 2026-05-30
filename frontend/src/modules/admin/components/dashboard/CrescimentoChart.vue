<script setup lang="ts">
/**
 * CrescimentoChart — gráfico SVG inline de crescimento mensal.
 * W6-CA4, CA5, CA6.
 * SVG inline sem dependência externa (~50 linhas). Tokens HSL do design system.
 * Tooltip nativo via <title> em cada vértice de ponto.
 * Responsivo via preserveAspectRatio="xMidYMid meet".
 */
import { computed } from "vue"
import { AppCard } from "@/components/ui"
import { useDashboardStore } from "../../stores/dashboardStore"

const store = useDashboardStore()

// ── Constantes do SVG ─────────────────────────────────────────────────────────
const W = 600
const H = 200
const PADDING_TOP = 20
const PADDING_BOTTOM = 40
const PADDING_LEFT = 40
const PADDING_RIGHT = 10
const CHART_W = W - PADDING_LEFT - PADDING_RIGHT
const CHART_H = H - PADDING_TOP - PADDING_BOTTOM

// ── Pontos normalizados ───────────────────────────────────────────────────────
const max = computed(() => {
    const totais = store.crescimento.map((p) => p.total)
    return Math.max(...totais, 1)
})

const pontos = computed(() =>
    store.crescimento.map((p, i) => {
        const x = PADDING_LEFT + (i / 11) * CHART_W
        const y = PADDING_TOP + CHART_H - (p.total / max.value) * CHART_H
        return { x, y, ponto: p }
    }),
)

const polylinePoints = computed(() =>
    pontos.value.map((p) => `${p.x},${p.y}`).join(" "),
)

// Área preenchida: polyline + canto direito inferior + canto esquerdo inferior
const areaPoints = computed(() => {
    const pts = pontos.value
    if (!pts.length) return ""
    const base = PADDING_TOP + CHART_H
    const right = pts[pts.length - 1].x
    const left = pts[0].x
    return `${polylinePoints.value} ${right},${base} ${left},${base}`
})

// Labels do eixo X: "MM/AA"
const labelsX = computed(() =>
    store.crescimento.map((p, i) => {
        const [ano, mes] = p.mes.split("-")
        return { x: PADDING_LEFT + (i / 11) * CHART_W, label: `${mes}/${ano.slice(2)}` }
    }),
)

// Marcadores eixo Y: 0, max/2, max (3 linhas guia)
const guias = computed(() => {
    const m = max.value
    return [0, Math.round(m / 2), m].map((v) => ({
        y: PADDING_TOP + CHART_H - (v / m) * CHART_H,
        label: String(v),
    }))
})

const tudoZero = computed(() => store.crescimento.every((p) => p.total === 0))

function formatarMes(mes: string): string {
    const [ano, m] = mes.split("-")
    return `${m}/${ano}`
}
</script>

<template>
    <AppCard>
        <h3 class="bloco-titulo">Crescimento mensal — novos estabelecimentos (últimos 12 meses)</h3>

        <!-- Loading -->
        <p v-if="store.carregandoCrescimento" class="estado-info" aria-live="polite">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </p>

        <!-- Erro -->
        <p v-else-if="store.erroCrescimento" class="bloco-erro" role="alert">
            {{ store.erroCrescimento }}
        </p>

        <template v-else>
            <!-- Aviso de todos zeros, mas ainda mostra o gráfico -->
            <p v-if="tudoZero" class="estado-vazio">
                Sem novos estabelecimentos no período.
            </p>

            <svg
                :viewBox="`0 0 ${W} ${H}`"
                preserveAspectRatio="xMidYMid meet"
                class="chart-svg"
                role="img"
                aria-label="Gráfico de crescimento mensal de novos estabelecimentos"
            >
                <!-- Linhas guia horizontais -->
                <g class="guias">
                    <line
                        v-for="g in guias"
                        :key="g.label"
                        :x1="PADDING_LEFT"
                        :y1="g.y"
                        :x2="W - PADDING_RIGHT"
                        :y2="g.y"
                        class="guia-linha"
                    />
                    <text
                        v-for="g in guias"
                        :key="'lbl-' + g.label"
                        :x="PADDING_LEFT - 4"
                        :y="g.y + 4"
                        text-anchor="end"
                        class="guia-texto"
                    >{{ g.label }}</text>
                </g>

                <!-- Área sombreada -->
                <polygon v-if="!tudoZero" :points="areaPoints" class="area" />

                <!-- Linha principal -->
                <polyline :points="polylinePoints" class="linha" />

                <!-- Pontos com tooltip nativo -->
                <g v-for="p in pontos" :key="p.ponto.mes">
                    <title>{{ formatarMes(p.ponto.mes) }}: {{ p.ponto.total }} estabelecimento(s)</title>
                    <circle :cx="p.x" :cy="p.y" r="4" class="ponto" />
                    <circle :cx="p.x" :cy="p.y" r="10" class="ponto-hit" />
                </g>

                <!-- Labels eixo X -->
                <g class="eixo-x">
                    <text
                        v-for="(l, i) in labelsX"
                        :key="i"
                        :x="l.x"
                        :y="H - 8"
                        text-anchor="middle"
                        class="eixo-texto"
                    >{{ l.label }}</text>
                </g>
            </svg>
        </template>
    </AppCard>
</template>

<style scoped>
.bloco-titulo {
    font-size: 0.875rem;
    font-weight: 600;
    color: hsl(var(--foreground));
    margin: 0 0 1rem;
}

.estado-info {
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
}

.estado-vazio {
    color: hsl(var(--muted-foreground));
    font-size: 0.85rem;
    margin-bottom: 0.5rem;
}

.bloco-erro {
    color: hsl(var(--error));
    font-size: 0.875rem;
    padding: 0.75rem 1rem;
    border-radius: 8px;
    background: hsl(var(--error) / 0.08);
}

.chart-svg {
    width: 100%;
    height: auto;
    display: block;
}

/* SVG — cores e espessuras */
.guia-linha {
    stroke: hsl(var(--foreground) / 0.08);
    stroke-width: 1;
}

.guia-texto,
.eixo-texto {
    font-size: 10px;
    fill: hsl(var(--muted-foreground));
    font-family: inherit;
}

.area {
    fill: hsl(var(--primary) / 0.1);
    stroke: none;
}

.linha {
    fill: none;
    stroke: hsl(var(--primary));
    stroke-width: 2;
    stroke-linejoin: round;
    stroke-linecap: round;
}

.ponto {
    fill: hsl(var(--primary));
    stroke: hsl(var(--background));
    stroke-width: 2;
}

/* Zona de hover expandida invisível */
.ponto-hit {
    fill: transparent;
    cursor: pointer;
}

.ponto-hit:hover + .ponto,
.ponto:hover {
    r: 6;
}
</style>
