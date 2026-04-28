<script setup lang="ts">
import { computed, ref, watch } from "vue"
import AppModal from "@/components/ui/AppModal.vue"
import { agendaService, type DisponibilidadeDia, type DisponibilidadeSlot } from "@/services/agendaService"

const props = defineProps<{
    aberto: boolean
    titulo: string
    profissionalId: string
    dataInicial?: string
}>()

const emit = defineEmits<{
    fechar: []
    selecionar: [{ data: string; hora: string }]
}>()

// ─── Semana ───────────────────────────────────────────────────────────────────

const semanaBase = ref(inicioSemanaAtual())

watch(() => props.dataInicial, (v) => {
    if (v) semanaBase.value = inicioSemana(new Date(v + "T00:00"))
}, { immediate: true })

watch(() => props.aberto, async (aberto) => {
    if (!aberto) return
    if (!props.profissionalId) return
    await carregar()
    // Se a semana atual não tem nenhum slot futuro disponível, avança para a próxima (1x só)
    if (proximoDisponivel.value === null && !erro.value) {
        const proxBase = new Date(semanaBase.value)
        proxBase.setDate(proxBase.getDate() + 7)
        semanaBase.value = proxBase
        await carregar()
    }
})

function inicioSemanaAtual() {
    return inicioSemana(new Date())
}

function inicioSemana(d: Date) {
    const r = new Date(d)
    r.setDate(r.getDate() - r.getDay())
    r.setHours(0, 0, 0, 0)
    return r
}

function toISO(d: Date): string {
    const y = d.getFullYear()
    const m = String(d.getMonth() + 1).padStart(2, "0")
    const day = String(d.getDate()).padStart(2, "0")
    return `${y}-${m}-${day}`
}

const dataInicioSemana = computed(() => toISO(semanaBase.value))
const dataFimSemana = computed(() => {
    const fim = new Date(semanaBase.value)
    fim.setDate(fim.getDate() + 6)
    return toISO(fim)
})

function navSemana(dir: -1 | 1) {
    if (dir === -1) {
        const inicioAtual = inicioSemanaAtual()
        const novaBase = new Date(semanaBase.value)
        novaBase.setDate(novaBase.getDate() - 7)
        if (novaBase < inicioAtual) return
    }
    const d = new Date(semanaBase.value)
    d.setDate(d.getDate() + dir * 7)
    semanaBase.value = d
}

const labelSemana = computed(() => {
    const fmt = (iso: string) => {
        const [y, m, d] = iso.split("-").map(Number)
        return new Date(y, m - 1, d).toLocaleDateString("pt-BR", { day: "numeric", month: "short" })
    }
    return `${fmt(dataInicioSemana.value)} - ${fmt(dataFimSemana.value)}`
})

// ─── Dados do backend ─────────────────────────────────────────────────────────

const dias = ref<DisponibilidadeDia[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)

async function carregar() {
    if (!props.profissionalId || !props.aberto) return
    carregando.value = true
    erro.value = null
    try {
        const result = await agendaService.consultarDisponibilidade(
            props.profissionalId,
            dataInicioSemana.value,
            dataFimSemana.value,
        )
        dias.value = result.dias
    } catch {
        erro.value = "Não foi possível carregar a agenda."
    } finally {
        carregando.value = false
    }
}

watch([() => props.profissionalId, dataInicioSemana], () => {
    if (props.aberto) carregar()
}, { immediate: false })

// ─── Filtro de passado (client-side) ─────────────────────────────────────────

const MARGEM_MINUTOS = 30

function isoHoje(): string {
    const now = new Date()
    const y = now.getFullYear()
    const m = String(now.getMonth() + 1).padStart(2, "0")
    const d = String(now.getDate()).padStart(2, "0")
    return `${y}-${m}-${d}`
}

function ePassado(iso: string, hora: string): boolean {
    const hoje = isoHoje()
    if (iso < hoje) return true
    if (iso > hoje) return false
    const [h, m] = hora.split(":").map(Number)
    const slotMs = new Date()
    slotMs.setHours(h, m, 0, 0)
    return slotMs.getTime() < Date.now() + MARGEM_MINUTOS * 60_000
}

function statusDiaEfetivo(dia: DisponibilidadeDia): "fechado" | "passado" | "disponivel" | "indisponivel" {
    if (dia.status === "fechado") return "fechado"
    if (dia.data < isoHoje()) return "passado"
    const temLivre = dia.slots.some(s => s.disponivel && !ePassado(dia.data, s.hora))
    return temLivre ? "disponivel" : "indisponivel"
}

function slotsEfetivos(dia: DisponibilidadeDia): (DisponibilidadeSlot & { motivo: DisponibilidadeSlot["motivo"] })[] {
    return dia.slots.map(s => ({
        ...s,
        disponivel: s.disponivel && !ePassado(dia.data, s.hora),
        motivo: (s.disponivel && ePassado(dia.data, s.hora) ? "passado" : s.motivo) as DisponibilidadeSlot["motivo"],
    }))
}

// ─── Acordeão ─────────────────────────────────────────────────────────────────

const diaExpandido = ref<string | null>(null)

function toggleDia(data: string, status: string) {
    if (status === "fechado" || status === "passado") return
    diaExpandido.value = diaExpandido.value === data ? null : data
}

// ─── Próximo disponível ───────────────────────────────────────────────────────

const proximoDisponivel = computed(() => {
    for (const dia of dias.value) {
        if (dia.status === "fechado") continue
        if (dia.data < isoHoje()) continue
        for (const s of dia.slots) {
            if (s.disponivel && !ePassado(dia.data, s.hora)) {
                const [y, m, d] = dia.data.split("-").map(Number)
                const fmt = new Date(y, m - 1, d).toLocaleDateString("pt-BR", { day: "numeric", month: "short" })
                return `${fmt} às ${s.hora}`
            }
        }
    }
    return null
})

// ─── Seleção ──────────────────────────────────────────────────────────────────

function selecionar(data: string, hora: string) {
    emit("selecionar", { data, hora })
}

// ─── Labels ──────────────────────────────────────────────────────────────────

const labelStatus: Record<string, string> = {
    fechado:      "Fechado",
    passado:      "Passado",
    disponivel:   "Disponível",
    indisponivel: "Indisponível",
}

function labelSlot(slot: DisponibilidadeSlot): string {
    if (slot.motivo === "agendado") return slot.pacienteNome ?? "Ocupado"
    if (slot.motivo === "bloqueado") return "Bloqueado"
    return ""
}

function classeSlot(slot: DisponibilidadeSlot, disponivel: boolean): string {
    if (!disponivel && slot.motivo === "agendado")  return "ocupado"
    if (!disponivel && slot.motivo === "bloqueado") return "bloqueado"
    if (!disponivel && slot.motivo === "passado")   return "passado"
    if (!disponivel)                                return "ocupado"
    return ""
}
</script>

<template>
    <AppModal
        :aberto="aberto"
        largura="lg"
        acima-de-drawer
        sem-padding-corpo
        @fechar="$emit('fechar')"
    >
        <template #titulo>
            <span class="modal-titulo-wrap">
                <span class="modal-icon">📅</span>
                <span>Selecionar horário — {{ titulo }}</span>
            </span>
        </template>

        <!-- Banner roxo -->
        <div class="banner">
            <span class="banner-titulo">Agenda do Profissional</span>
            <span v-if="proximoDisponivel" class="banner-proximo">
                Próximo disponível: {{ proximoDisponivel }}
            </span>
            <span v-else-if="!carregando" class="banner-proximo sem-horario">
                Sem horários disponíveis esta semana
            </span>
        </div>

        <!-- Navegação de semana -->
        <div class="semana-nav">
            <button class="nav-btn" :disabled="dataInicioSemana <= isoHoje()" @click="navSemana(-1)">‹</button>
            <span>{{ labelSemana }}</span>
            <button class="nav-btn" @click="navSemana(1)">›</button>
        </div>

        <!-- Sem profissional selecionado -->
        <div v-if="!profissionalId" class="estado-msg">
            Selecione um profissional para ver os horários disponíveis.
        </div>

        <!-- Loading / erro -->
        <div v-else-if="carregando" class="estado-msg">Carregando agenda…</div>
        <div v-else-if="erro" class="estado-msg erro">{{ erro }}</div>

        <!-- Lista de dias -->
        <div v-else class="dias-lista">
            <div v-for="dia in dias" :key="dia.data" class="dia-item">
                <button
                    class="dia-header"
                    :class="{ 'sem-interacao': statusDiaEfetivo(dia) === 'fechado' || statusDiaEfetivo(dia) === 'passado' }"
                    @click="toggleDia(dia.data, statusDiaEfetivo(dia))"
                >
                    <div class="dia-info">
                        <span class="dia-dow">{{ dia.diaSemana }}.</span>
                        <span class="dia-data">
                            {{
                                new Date(dia.data + "T00:00").toLocaleDateString("pt-BR", {
                                    day: "numeric", month: "short"
                                })
                            }}
                        </span>
                    </div>
                    <div class="dia-direita">
                        <span :class="['status-badge', statusDiaEfetivo(dia)]">
                            {{ labelStatus[statusDiaEfetivo(dia)] }}
                        </span>
                        <span
                            v-if="statusDiaEfetivo(dia) !== 'fechado' && statusDiaEfetivo(dia) !== 'passado'"
                            class="chevron"
                        >{{ diaExpandido === dia.data ? "∧" : "∨" }}</span>
                    </div>
                </button>

                <div v-if="diaExpandido === dia.data" class="slots-grid">
                    <button
                        v-for="slot in slotsEfetivos(dia)"
                        :key="slot.hora"
                        class="slot"
                        :class="classeSlot(slot, slot.disponivel)"
                        :disabled="!slot.disponivel"
                        :title="!slot.disponivel && slot.motivo === 'agendado' ? slot.pacienteNome ?? '' : ''"
                        @click="selecionar(dia.data, slot.hora)"
                    >
                        <span class="slot-hora">{{ slot.hora }}</span>
                        <span class="slot-label">{{ labelSlot(slot) }}</span>
                    </button>
                </div>
            </div>
        </div>
    </AppModal>
</template>

<style scoped>
.modal-titulo-wrap {
    display: inline-flex; align-items: center; gap: 0.6rem;
    font-weight: 700; font-size: 1rem;
}
.modal-icon { font-size: 1.1em; }

.banner {
    background: linear-gradient(135deg, #241554, #452b97);
    color: #fff; padding: 1rem 1.5rem;
    display: flex; flex-direction: column; gap: 0.35rem; flex-shrink: 0;
}
.banner-titulo { font-weight: 700; font-size: 0.95em; }
.banner-proximo {
    font-size: 0.8em; background: rgba(255,255,255,0.15);
    display: inline-block; padding: 0.2rem 0.7rem; border-radius: 99px; width: fit-content;
}
.banner-proximo.sem-horario { background: rgba(255,255,255,0.08); opacity: 0.7; }

.semana-nav {
    display: flex; align-items: center; justify-content: space-between;
    padding: 0.85rem 1.5rem; border-bottom: 1px solid var(--border);
    font-size: 0.9em; font-weight: 600; flex-shrink: 0;
    background: var(--bg-card);
}
.nav-btn {
    border: 1px solid var(--border); background: var(--bg-card); cursor: pointer;
    padding: 0.3rem 0.7rem; border-radius: 6px; font-size: 1rem;
    transition: background 0.12s;
}
.nav-btn:hover:not(:disabled) { background: var(--bg-hover); }
.nav-btn:disabled { opacity: 0.35; cursor: default; }

.estado-msg { padding: 1.5rem; text-align: center; color: var(--text-muted); font-size: 0.9em; }
.estado-msg.erro { color: hsl(var(--error)); }

.dias-lista { overflow-y: auto; flex: 1; }
.dia-item { border-bottom: 1px solid var(--border); }
.dia-item:last-child { border-bottom: none; }

.dia-header {
    width: 100%; display: flex; justify-content: space-between; align-items: center;
    padding: 0.95rem 1.5rem; border: none; background: none; cursor: pointer;
    text-align: left; transition: background 0.12s;
}
.dia-header:hover:not(.sem-interacao) { background: var(--bg-hover); }
.dia-header.sem-interacao { cursor: default; }
.dia-info { display: flex; gap: 0.75rem; align-items: center; }
.dia-dow { font-weight: 700; font-size: 0.82em; color: hsl(var(--primary)); width: 32px; }
.dia-data { font-weight: 600; font-size: 0.9em; }
.dia-direita { display: flex; align-items: center; gap: 0.9rem; }

.status-badge {
    font-size: 0.72em; font-weight: 700; padding: 0.25rem 0.75rem;
    border-radius: 99px; display: inline-flex; align-items: center; gap: 0.25rem;
}
.status-badge.disponivel::before  { content: "✓ "; }
.status-badge.disponivel    { background: #dcfce7; color: #15803d; }
.status-badge.fechado       { background: #fee2e2; color: #b91c1c; }
.status-badge.passado       { background: #f1f5f9; color: #64748b; }
.status-badge.indisponivel  { background: #fef3c7; color: #92400e; }
.chevron { color: var(--text-muted); font-size: 0.8em; }

.slots-grid {
    display: grid; grid-template-columns: repeat(auto-fill, minmax(80px, 1fr));
    gap: 8px; padding: 1rem 1.5rem 1.25rem;
    background: #fafafa; border-top: 1px solid var(--border);
}
.slot {
    display: flex; flex-direction: column; align-items: center; gap: 3px;
    padding: 0.55rem 0.3rem; border: 1px solid var(--border); border-radius: 6px;
    background: var(--bg-card); cursor: pointer; transition: all 0.12s;
}
.slot:hover:not(:disabled) { border-color: hsl(var(--primary)); background: hsl(var(--primary-light)); }
.slot.ocupado   { background: #fef2f2; border-color: #fecaca; cursor: not-allowed; }
.slot.bloqueado { background: #f8fafc; border-color: #e2e8f0; cursor: not-allowed; }
.slot.passado   { background: #f8fafc; border-color: #e2e8f0; cursor: not-allowed; opacity: 0.45; }
.slot:disabled  { opacity: 0.7; }
.slot-hora  { font-size: 0.8em; font-weight: 700; }
.slot-label {
    font-size: 0.65em; color: var(--text-muted);
    max-width: 72px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    text-align: center;
}
</style>
