<script setup lang="ts">
/**
 * AgendaRail — sidebar direita da Agenda com 3 cards:
 *   - Mini-calendário do mês (navegação por mês)
 *   - Lista de espera (pacientes pra encaixar)
 *   - Ocupação do dia (slots usados)
 */
import { computed } from "vue"
import type { Agendamento } from "@/services/agendaService"
import type { ListaEsperaItem } from "@/services/listaEsperaService"

const props = defineProps<{
    /** Data selecionada como YYYY-MM-DD. */
    modelValue: string
    /** Contagens por dia para destacar dias com agendamentos. */
    counts?: Record<string, number>
    /** Lista de espera. */
    listaEspera: ListaEsperaItem[]
    /** Agendamentos do dia atual (para calcular ocupação). */
    agendamentosDoDia: Agendamento[]
    /** Capacidade total do dia em slots (default 18). */
    capacidadeDia?: number
}>()

const emit = defineEmits<{
    "update:modelValue": [value: string]
    encaixar: [item: ListaEsperaItem]
    remover: [item: ListaEsperaItem]
}>()

const DOW = ["D", "S", "T", "Q", "Q", "S", "S"]

const selecionada = computed<Date>(() => {
    const [y, m, d] = props.modelValue.split("-").map(Number)
    return new Date(y, m - 1, d)
})

const hoje = (() => {
    const t = new Date()
    t.setHours(0, 0, 0, 0)
    return t
})()

const monthLabel = computed(() => {
    const s = selecionada.value.toLocaleDateString("pt-BR", { month: "long", year: "numeric" })
    return s.charAt(0).toUpperCase() + s.slice(1)
})

interface CelMini {
    day: number
    date: Date
    muted: boolean
}

const cells = computed<CelMini[]>(() => {
    const sel = selecionada.value
    const month = sel.getMonth()
    const year = sel.getFullYear()
    const first = new Date(year, month, 1)
    const startOffset = first.getDay()
    const daysInMonth = new Date(year, month + 1, 0).getDate()
    const prevMonthDays = new Date(year, month, 0).getDate()
    const arr: CelMini[] = []

    for (let i = startOffset - 1; i >= 0; i--) {
        const d = prevMonthDays - i
        arr.push({ day: d, muted: true, date: new Date(year, month - 1, d) })
    }
    for (let d = 1; d <= daysInMonth; d++) {
        arr.push({ day: d, muted: false, date: new Date(year, month, d) })
    }
    while (arr.length < 42) {
        const next = arr.length - (startOffset + daysInMonth) + 1
        arr.push({ day: next, muted: true, date: new Date(year, month + 1, next) })
    }
    return arr
})

function toISO(d: Date) {
    const y = d.getFullYear()
    const m = String(d.getMonth() + 1).padStart(2, "0")
    const dd = String(d.getDate()).padStart(2, "0")
    return `${y}-${m}-${dd}`
}

function ehMesmoDia(a: Date, b: Date) {
    return a.toDateString() === b.toDateString()
}

function temAgendamentos(d: Date) {
    return (props.counts?.[toISO(d)] ?? 0) > 0
}

function clickCell(c: CelMini) {
    emit("update:modelValue", toISO(c.date))
}

function navMes(delta: number) {
    const d = new Date(selecionada.value)
    d.setMonth(d.getMonth() + delta)
    emit("update:modelValue", toISO(d))
}

function fmtTempo(min: number) {
    if (min < 60) return `há ${min} min`
    const h = Math.floor(min / 60)
    if (h < 24) return `há ${h}h`
    const d = Math.floor(h / 24)
    return `há ${d}d`
}

const iniciais = (nome: string) => {
    const partes = nome.trim().split(/\s+/)
    if (partes.length === 1) return partes[0].charAt(0).toUpperCase()
    return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase()
}

// ─── Ocupação ───
const ocupacao = computed(() => {
    const cap = props.capacidadeDia ?? 18
    const usados = props.agendamentosDoDia.filter(
        a => a.status !== "Cancelado",
    ).length
    const pct = cap > 0 ? Math.min(100, Math.round(usados * 100 / cap)) : 0
    return { usados, cap, pct }
})

const tempoMedio = computed(() => {
    if (props.agendamentosDoDia.length === 0) return 0
    const total = props.agendamentosDoDia.reduce((a, c) => {
        const ini = new Date(c.inicioPrevisto).getTime()
        const fim = new Date(c.fimPrevisto).getTime()
        return a + (fim - ini) / 60000
    }, 0)
    return Math.round(total / props.agendamentosDoDia.length)
})
</script>

<template>
    <aside class="rail">
        <!-- Mini-calendário -->
        <section class="rail-card">
            <header class="rh">
                <h3>Calendário</h3>
                <div class="nav">
                    <button type="button" @click="navMes(-1)" aria-label="Mês anterior">‹</button>
                    <button type="button" @click="navMes(1)" aria-label="Próximo mês">›</button>
                </div>
            </header>
            <div class="month-name">{{ monthLabel }}</div>
            <div class="minical">
                <div v-for="(h, i) in DOW" :key="'h' + i" class="h">{{ h }}</div>
                <button
                    v-for="(c, i) in cells"
                    :key="i"
                    type="button"
                    class="d"
                    :class="{
                        muted: c.muted,
                        today: ehMesmoDia(c.date, hoje),
                        selected: ehMesmoDia(c.date, selecionada),
                        has: !c.muted && temAgendamentos(c.date),
                    }"
                    @click="clickCell(c)"
                >{{ c.day }}</button>
            </div>
        </section>

        <!-- Lista de espera -->
        <section class="rail-card">
            <header class="rh">
                <h3><i class="fa-solid fa-list-ul" aria-hidden="true"></i>Lista de espera</h3>
                <span class="count">{{ listaEspera.length }}</span>
            </header>
            <div v-if="listaEspera.length === 0" class="vazio">
                Sem pacientes na fila.
            </div>
            <div v-else class="waitlist">
                <div v-for="w in listaEspera" :key="w.id" class="w-item">
                    <div class="w-av">{{ iniciais(w.pacienteNome) }}</div>
                    <div class="w-info">
                        <b>{{ w.pacienteNome }}</b>
                        <span>{{ w.motivo }} · {{ fmtTempo(w.minutosDesdeQueEntrou) }}</span>
                    </div>
                    <button
                        type="button"
                        class="w-action"
                        title="Encaixar"
                        @click="emit('encaixar', w)"
                    >
                        <i class="fa-solid fa-plus" aria-hidden="true"></i>
                    </button>
                    <button
                        type="button"
                        class="w-action danger"
                        title="Remover"
                        @click="emit('remover', w)"
                    >
                        <i class="fa-solid fa-xmark" aria-hidden="true"></i>
                    </button>
                </div>
            </div>
        </section>

        <!-- Ocupação -->
        <section class="rail-card">
            <header class="rh">
                <h3><i class="fa-solid fa-gauge-high" aria-hidden="true"></i>Ocupação do dia</h3>
            </header>
            <div class="occ-bar">
                <div class="fill" :style="{ width: ocupacao.pct + '%' }"></div>
            </div>
            <div class="occ-meta">
                <span><b>{{ ocupacao.usados }}</b> de {{ ocupacao.cap }} slots</span>
                <span><b>{{ ocupacao.pct }}%</b></span>
            </div>
            <div class="occ-extra">
                <div class="row">
                    <span>Tempo médio</span>
                    <b>{{ tempoMedio }} min</b>
                </div>
                <div class="row">
                    <span>Cancelados</span>
                    <b>{{ agendamentosDoDia.filter(a => a.status === 'Cancelado').length }}</b>
                </div>
            </div>
        </section>
    </aside>
</template>

<style scoped>
.rail {
    display: flex;
    flex-direction: column;
    gap: 14px;
    width: 320px;
    flex-shrink: 0;
}

.rail-card {
    background: white;
    border: 1px solid hsl(0 0% 0% / 0.07);
    border-radius: 12px;
    padding: 14px 14px 12px;
    box-shadow: 0 1px 2px hsl(0 0% 0% / 0.04);
}

.rh {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    margin-bottom: 10px;
}
.rh h3 {
    margin: 0;
    font-size: 13px;
    font-weight: 700;
    color: hsl(var(--primary-dark, 254 56% 21%));
    display: inline-flex;
    align-items: center;
    gap: 6px;
}
.rh h3 i { color: hsl(45 96% 47%); font-size: 11px; }
.rh .nav { display: flex; gap: 4px; }
.rh .nav button {
    width: 24px;
    height: 24px;
    border-radius: 6px;
    border: 1px solid hsl(0 0% 0% / 0.12);
    background: white;
    color: hsl(0 0% 24%);
    cursor: pointer;
    font-family: inherit;
    font-size: 13px;
}
.rh .nav button:hover { background: hsl(0 0% 0% / 0.05); }
.count {
    font-size: 11px;
    font-weight: 700;
    padding: 2px 8px;
    border-radius: 999px;
    background: hsl(var(--primary, 254 56% 38%) / 0.1);
    color: hsl(var(--primary, 254 56% 38%));
}

/* ── Mini-calendário ── */
.month-name {
    font-size: 11px;
    font-weight: 700;
    color: hsl(0 0% 24%);
    text-align: center;
    margin-bottom: 8px;
    text-transform: capitalize;
}
.minical {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 3px;
}
.minical .h {
    font-size: 9px;
    font-weight: 700;
    color: hsl(0 0% 0% / 0.5);
    text-align: center;
    padding: 4px 0;
    text-transform: uppercase;
}
.minical .d {
    aspect-ratio: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    border: 0;
    background: transparent;
    font-size: 11px;
    font-weight: 600;
    color: hsl(0 0% 24%);
    border-radius: 6px;
    cursor: pointer;
    font-family: inherit;
    position: relative;
    transition: background 0.12s;
}
.minical .d:hover { background: hsl(var(--primary, 254 56% 38%) / 0.08); }
.minical .d.muted { color: hsl(0 0% 0% / 0.25); }
.minical .d.today { color: hsl(var(--primary, 254 56% 38%)); font-weight: 800; }
.minical .d.selected {
    background: hsl(var(--primary, 254 56% 38%));
    color: white;
}
.minical .d.has::after {
    content: '';
    position: absolute;
    bottom: 3px;
    width: 4px;
    height: 4px;
    border-radius: 50%;
    background: hsl(var(--primary, 254 56% 38%));
}
.minical .d.selected.has::after { background: white; }

/* ── Lista de espera ── */
.vazio {
    font-size: 12px;
    color: hsl(0 0% 0% / 0.5);
    padding: 14px 0;
    text-align: center;
}
.waitlist {
    display: flex;
    flex-direction: column;
    gap: 4px;
    max-height: 280px;
    overflow-y: auto;
}
.w-item {
    display: grid;
    grid-template-columns: 32px 1fr auto auto;
    align-items: center;
    gap: 8px;
    padding: 6px 8px;
    border-radius: 8px;
    transition: background 0.12s;
}
.w-item:hover { background: hsl(0 0% 0% / 0.03); }
.w-av {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: hsl(var(--primary, 254 56% 38%) / 0.1);
    color: hsl(var(--primary, 254 56% 38%));
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 11px;
    font-weight: 700;
    border: 2px solid hsl(var(--primary, 254 56% 38%) / 0.18);
}
.w-info { min-width: 0; }
.w-info b {
    display: block;
    font-size: 12px;
    font-weight: 600;
    color: hsl(0 0% 24%);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.w-info span {
    display: block;
    font-size: 10px;
    color: hsl(0 0% 0% / 0.6);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.w-action {
    width: 26px;
    height: 26px;
    border-radius: 6px;
    border: 1px solid hsl(0 0% 0% / 0.12);
    background: white;
    color: hsl(var(--primary, 254 56% 38%));
    cursor: pointer;
    font-size: 11px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: inherit;
}
.w-action:hover {
    background: hsl(var(--primary, 254 56% 38%));
    color: white;
    border-color: hsl(var(--primary, 254 56% 38%));
}
.w-action.danger { color: hsl(0 70% 50%); }
.w-action.danger:hover {
    background: hsl(0 70% 50%);
    border-color: hsl(0 70% 50%);
    color: white;
}

/* ── Ocupação ── */
.occ-bar {
    width: 100%;
    height: 8px;
    background: hsl(0 0% 0% / 0.06);
    border-radius: 999px;
    overflow: hidden;
    margin-bottom: 8px;
}
.occ-bar .fill {
    height: 100%;
    background: linear-gradient(
        90deg,
        hsl(160 79% 39%) 0%,
        hsl(45 96% 47%) 70%,
        hsl(0 84% 60%) 100%
    );
    border-radius: 999px;
    transition: width 0.3s;
}
.occ-meta {
    display: flex;
    justify-content: space-between;
    font-size: 11px;
    color: hsl(0 0% 0% / 0.65);
}
.occ-meta b {
    color: hsl(var(--primary-dark, 254 56% 21%));
    font-variant-numeric: tabular-nums;
}
.occ-extra {
    margin-top: 10px;
    padding-top: 10px;
    border-top: 1px dashed hsl(0 0% 0% / 0.1);
    font-size: 11px;
    color: hsl(0 0% 0% / 0.6);
    display: flex;
    flex-direction: column;
    gap: 4px;
}
.occ-extra .row {
    display: flex;
    justify-content: space-between;
}
.occ-extra b { color: hsl(var(--primary-dark, 254 56% 21%)); }

@media (max-width: 1100px) {
    .rail { width: 100%; }
}
</style>
