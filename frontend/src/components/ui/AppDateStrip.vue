<script setup lang="ts">
/**
 * AppDateStrip — strip horizontal de 14 dias com seleção e contagens (estilo
 * design Anthropic). v-model na data selecionada (string YYYY-MM-DD ou Date).
 *
 * Uso:
 *   <AppDateStrip v-model="dataSel" :counts="contagensPorDia" />
 *
 * `counts` é um Map ou Record com chave YYYY-MM-DD e valor numérico (ex: 12).
 */
import { computed } from "vue"

const props = defineProps<{
    /** Data selecionada (string YYYY-MM-DD ou Date). */
    modelValue: string | Date
    /** Contagens por dia: { "2026-04-30": 5 }. */
    counts?: Record<string, number>
    /** Quantidade de dias antes da selecionada (default 6 = 7 antes incluindo hoje). */
    diasAntes?: number
    /** Total de dias na strip (default 14). */
    totalDias?: number
}>()

const emit = defineEmits<{
    "update:modelValue": [value: string]
}>()

const DOW = ["DOM", "SEG", "TER", "QUA", "QUI", "SEX", "SÁB"]

const selecionada = computed<Date>(() => {
    if (typeof props.modelValue === "string") {
        const [y, m, d] = props.modelValue.split("-").map(Number)
        return new Date(y, m - 1, d)
    }
    return new Date(props.modelValue)
})

const hoje = (() => {
    const t = new Date()
    t.setHours(0, 0, 0, 0)
    return t
})()

const dias = computed<Date[]>(() => {
    const antes = props.diasAntes ?? 6
    const total = props.totalDias ?? 14
    const base = new Date(selecionada.value)
    base.setDate(base.getDate() - antes)
    return Array.from({ length: total }, (_, i) => {
        const d = new Date(base)
        d.setDate(base.getDate() + i)
        return d
    })
})

const mesLabel = computed(() => {
    const s = selecionada.value.toLocaleDateString("pt-BR", { month: "long", year: "numeric" })
    return s.charAt(0).toUpperCase() + s.slice(1)
})

function toISO(d: Date) {
    const y = d.getFullYear()
    const m = String(d.getMonth() + 1).padStart(2, "0")
    const dd = String(d.getDate()).padStart(2, "0")
    return `${y}-${m}-${dd}`
}

function selecionar(d: Date) {
    emit("update:modelValue", toISO(d))
}

function shift(n: number) {
    const nd = new Date(selecionada.value)
    nd.setDate(nd.getDate() + n)
    emit("update:modelValue", toISO(nd))
}

function irHoje() {
    emit("update:modelValue", toISO(new Date()))
}

function ehMesmoDia(a: Date, b: Date) {
    return a.toDateString() === b.toDateString()
}

function contagem(d: Date): number {
    return props.counts?.[toISO(d)] ?? 0
}
</script>

<template>
    <div class="datestrip-wrap">
        <header class="ds-head">
            <div class="ds-month">
                <i class="fa-solid fa-calendar" aria-hidden="true"></i>
                <span>{{ mesLabel }}</span>
            </div>
            <div class="ds-ctrls">
                <button type="button" class="today-btn" @click="irHoje">Hoje</button>
                <button type="button" class="nav-btn" @click="shift(-7)" aria-label="Semana anterior">
                    <i class="fa-solid fa-angles-left" aria-hidden="true"></i>
                </button>
                <button type="button" class="nav-btn" @click="shift(-1)" aria-label="Dia anterior">
                    <i class="fa-solid fa-chevron-left" aria-hidden="true"></i>
                </button>
                <button type="button" class="nav-btn" @click="shift(1)" aria-label="Próximo dia">
                    <i class="fa-solid fa-chevron-right" aria-hidden="true"></i>
                </button>
                <button type="button" class="nav-btn" @click="shift(7)" aria-label="Próxima semana">
                    <i class="fa-solid fa-angles-right" aria-hidden="true"></i>
                </button>
            </div>
        </header>

        <div class="datestrip">
            <button
                v-for="(d, i) in dias"
                :key="i"
                type="button"
                class="dchip"
                :class="{
                    selected: ehMesmoDia(d, selecionada),
                    today: ehMesmoDia(d, hoje) && !ehMesmoDia(d, selecionada),
                    past: d < hoje && !ehMesmoDia(d, hoje),
                    weekend: d.getDay() === 0 || d.getDay() === 6,
                }"
                @click="selecionar(d)"
            >
                <span class="dow">{{ DOW[d.getDay()] }}</span>
                <span class="dom">{{ d.getDate() }}</span>
                <span class="meta">{{ contagem(d) > 0 ? `${contagem(d)} agend.` : '—' }}</span>
                <div class="pip-row">
                    <span
                        v-for="k in Math.min(contagem(d), 5)"
                        :key="k"
                        class="pip"
                    ></span>
                </div>
            </button>
        </div>
    </div>
</template>

<style scoped>
.datestrip-wrap {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--foreground) / 0.07);
    border-radius: 12px;
    padding: 14px 16px 16px;
    box-shadow: 0 1px 2px hsl(0 0% 0% / 0.04);
    margin-bottom: 16px;
}
.ds-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 12px;
    gap: 12px;
    flex-wrap: wrap;
}
.ds-month {
    font-size: 14px;
    font-weight: 700;
    color: hsl(var(--primary-dark, 254 56% 21%));
    display: flex;
    align-items: center;
    gap: 8px;
}
.ds-month i { color: hsl(var(--primary, 254 56% 38%)); }

.ds-ctrls {
    display: flex;
    align-items: center;
    gap: 6px;
}
.today-btn {
    font-size: 11px;
    font-weight: 700;
    color: hsl(var(--primary, 254 56% 38%));
    background: hsl(var(--primary, 254 56% 38%) / 0.08);
    border: 0;
    padding: 6px 12px;
    border-radius: 999px;
    cursor: pointer;
    font-family: inherit;
}
.today-btn:hover { background: hsl(var(--primary, 254 56% 38%) / 0.14); }

.nav-btn {
    width: 28px;
    height: 28px;
    border-radius: 6px;
    border: 1px solid hsl(var(--foreground) / 0.12);
    background: hsl(var(--card));
    color: hsl(var(--foreground));
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-family: inherit;
}
.nav-btn:hover { background: hsl(var(--foreground) / 0.05); }

.datestrip {
    display: grid;
    grid-template-columns: repeat(14, 1fr);
    gap: 6px;
}

.dchip {
    position: relative;
    padding: 9px 4px 8px;
    border-radius: 8px;
    border: 1.5px solid transparent;
    background: hsl(var(--foreground) / 0.025);
    text-align: center;
    cursor: pointer;
    transition: all 0.15s;
    user-select: none;
    font-family: inherit;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0;
}
.dchip:hover { background: hsl(var(--primary, 254 56% 38%) / 0.06); }

.dow {
    font-size: 10px;
    font-weight: 700;
    color: hsl(var(--foreground) / 0.55);
    text-transform: uppercase;
    letter-spacing: 0.06em;
}
.dom {
    font-size: 18px;
    font-weight: 800;
    color: hsl(var(--foreground));
    margin-top: 2px;
    line-height: 1.1;
    letter-spacing: -0.01em;
}
.meta {
    font-size: 9px;
    font-weight: 700;
    color: hsl(var(--foreground) / 0.5);
    margin-top: 4px;
    min-height: 11px;
}
.dchip.weekend .dow { color: hsl(var(--foreground) / 0.35); }
.dchip.today {
    background: hsl(var(--primary, 254 56% 38%) / 0.06);
    border-color: hsl(var(--primary, 254 56% 38%) / 0.25);
}
.dchip.today .dow,
.dchip.today .meta { color: hsl(var(--primary, 254 56% 38%)); }
.dchip.selected {
    background: hsl(var(--primary, 254 56% 38%));
    border-color: hsl(var(--primary, 254 56% 38%));
    box-shadow: 0 4px 12px hsl(var(--primary, 254 56% 38%) / 0.3);
}
.dchip.selected .dow,
.dchip.selected .dom,
.dchip.selected .meta { color: white; }
.dchip.past { opacity: 0.85; }

.pip-row {
    display: flex;
    gap: 2px;
    justify-content: center;
    margin-top: 4px;
    min-height: 4px;
}
.pip {
    width: 4px;
    height: 4px;
    border-radius: 50%;
    background: hsl(var(--primary, 254 56% 38%) / 0.4);
}
.dchip.selected .pip { background: hsl(var(--card)); }

@media (max-width: 1100px) {
    .datestrip { grid-template-columns: repeat(7, 1fr); }
    .datestrip .dchip:nth-child(n+8) { display: none; }
}
</style>
