<script setup lang="ts">
/**
 * AgendamentoRow — linha de agendamento estilo design Anthropic.
 *
 * Layout: hora grande à esquerda + stripe colorido + corpo (paciente, status,
 * profissional, tipo, observações) + ações contextuais por status.
 *
 * Status mapeados ao backend (Agendado | Confirmado | Cancelado | Concluido).
 */
import { computed } from "vue"
import type { Agendamento } from "@/services/agendaService"
import { formatData, formatDataHora, formatHora } from "@/utils/datetime"

const props = defineProps<{
    agendamento: Agendamento
    /** Quando true, mostra o painel inline de detalhes. Controlado pelo parent. */
    expandido?: boolean
}>()

const emit = defineEmits<{
    (e: "alternar", a: Agendamento): void
    (e: "editar", a: Agendamento): void
    (e: "reagendar", a: Agendamento): void
    (e: "confirmar", a: Agendamento): void
    (e: "cancelar", a: Agendamento): void
    (e: "concluir", a: Agendamento): void
    (e: "checkin", a: Agendamento): void
}>()

const STATUS_META: Record<Agendamento["status"], {
    label: string
    cor: string
    pillBg: string
    pillFg: string
}> = {
    Agendado: {
        label: "Agendado",
        cor: "hsl(45 96% 47%)",
        pillBg: "hsl(45 96% 47% / 0.18)",
        pillFg: "hsl(35 90% 30%)",
    },
    Confirmado: {
        label: "Confirmado",
        cor: "hsl(160 79% 39%)",
        pillBg: "hsl(160 79% 39% / 0.12)",
        pillFg: "hsl(160 79% 28%)",
    },
    Concluido: {
        label: "Concluído",
        cor: "hsl(0 0% 60%)",
        pillBg: "hsl(var(--foreground) / 0.06)",
        pillFg: "hsl(var(--foreground) / 0.6)",
    },
    Cancelado: {
        label: "Cancelado",
        cor: "hsl(0 84% 60%)",
        pillBg: "hsl(0 84% 60% / 0.10)",
        pillFg: "hsl(0 84% 60%)",
    },
}

const meta = computed(() => STATUS_META[props.agendamento.status])

function fmtHora(iso: string) {
    return formatHora(iso)
}

const duracaoMin = computed(() => {
    const ini = new Date(props.agendamento.inicioPrevisto).getTime()
    const fim = new Date(props.agendamento.fimPrevisto).getTime()
    return Math.round((fim - ini) / 60000)
})

const inicial = computed(() => {
    const partes = props.agendamento.pacienteNome.trim().split(/\s+/)
    if (partes.length === 1) return partes[0].charAt(0).toUpperCase()
    return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase()
})

const acoes = computed(() => {
    switch (props.agendamento.status) {
        case "Agendado":
            if (props.agendamento.checkInEm == null) {
                return [
                    { tipo: "primary" as const, icon: "fa-solid fa-circle-check", label: "Confirmar", evento: "confirmar" as const },
                    { tipo: "icon" as const, icon: "fa-solid fa-user-check", title: "Check-in", evento: "checkin" as const },
                    { tipo: "icon" as const, icon: "fa-solid fa-ban", title: "Cancelar", evento: "cancelar" as const },
                ]
            }
            return [
                { tipo: "primary" as const, icon: "fa-solid fa-circle-check", label: "Confirmar", evento: "confirmar" as const },
                { tipo: "icon" as const, icon: "fa-solid fa-ban", title: "Cancelar", evento: "cancelar" as const },
            ]
        case "Confirmado":
            if (props.agendamento.checkInEm == null) {
                return [
                    { tipo: "primary" as const, icon: "fa-solid fa-user-check", label: "Check-in", evento: "checkin" as const },
                    { tipo: "icon" as const, icon: "fa-solid fa-ban", title: "Cancelar", evento: "cancelar" as const },
                ]
            }
            return [
                { tipo: "icon" as const, icon: "fa-solid fa-ban", title: "Cancelar", evento: "cancelar" as const },
            ]
        default:
            return []
    }
})

type EventoAcao = "confirmar" | "cancelar" | "concluir" | "checkin"
function emitirAcao(evento: EventoAcao) {
    switch (evento) {
        case "confirmar": emit("confirmar", props.agendamento); return
        case "cancelar": emit("cancelar", props.agendamento); return
        case "checkin": emit("checkin", props.agendamento); return
        case "concluir": emit("concluir", props.agendamento); return
    }
}

const horaFim = computed(() => formatHora(props.agendamento.fimPrevisto))
const dataLabel = computed(() => formatData(props.agendamento.inicioPrevisto, { day: "2-digit", month: "long", year: "numeric" }))

const podeAlterar = computed(() => {
    const s = props.agendamento.status
    if (s !== "Agendado" && s !== "Confirmado") return false
    return new Date(props.agendamento.inicioPrevisto).getTime() > Date.now()
})
</script>

<template>
    <div
        class="appt"
        :class="[agendamento.status.toLowerCase(), { expandido }]"
        @click="emit('alternar', agendamento)"
    >
        <div class="timecol">
            <span class="time">{{ fmtHora(agendamento.inicioPrevisto) }}</span>
            <span class="duration">
                <i class="fa-regular fa-clock" aria-hidden="true"></i>
                {{ duracaoMin }} min
            </span>
        </div>

        <div class="stripe" :style="{ background: meta.cor }"></div>

        <div class="body">
            <div class="head-row">
                <div class="av">{{ inicial }}</div>
                <span class="pat-name">{{ agendamento.pacienteNome }}</span>
                <span
                    class="pill"
                    :style="{ background: meta.pillBg, color: meta.pillFg }"
                >
                    <span class="dot"></span>{{ meta.label }}
                </span>
            </div>
            <div class="meta-row">
                <span>
                    <i class="fa-solid fa-user-doctor" aria-hidden="true"></i>
                    {{ agendamento.profissionalNome }}
                </span>
                <span class="sep"></span>
                <span>
                    <i class="fa-solid fa-stethoscope" aria-hidden="true"></i>
                    {{ agendamento.tipoServico }}
                </span>
                <template v-if="agendamento.observacoes">
                    <span class="sep"></span>
                    <span class="obs">
                        <i class="fa-solid fa-circle-info" aria-hidden="true"></i>
                        {{ agendamento.observacoes }}
                    </span>
                </template>
            </div>
        </div>

        <div class="actions" @click.stop>
            <template v-for="(a, i) in acoes" :key="i">
                <button
                    v-if="a.tipo === 'primary'"
                    type="button"
                    class="btn-cta"
                    @click="emitirAcao(a.evento as EventoAcao)"
                >
                    <i :class="a.icon" aria-hidden="true"></i>
                    {{ a.label }}
                </button>
                <button
                    v-else
                    type="button"
                    class="btn-icon-sm"
                    :title="a.title"
                    @click="emitirAcao(a.evento as EventoAcao)"
                >
                    <i :class="a.icon" aria-hidden="true"></i>
                </button>
            </template>
            <button
                type="button"
                class="btn-icon-sm chev"
                :title="expandido ? 'Recolher' : 'Expandir'"
                @click.stop="emit('alternar', agendamento)"
            >
                <i :class="['fa-solid', expandido ? 'fa-chevron-up' : 'fa-chevron-down']" aria-hidden="true"></i>
            </button>
        </div>

        <!-- Painel inline de detalhes (expansão ao clicar) -->
        <div v-if="expandido" class="detalhe" @click.stop>
            <div class="grid">
                <div class="campo">
                    <span class="lbl">Data e hora</span>
                    <span class="val">{{ dataLabel }} · {{ fmtHora(agendamento.inicioPrevisto) }} – {{ horaFim }}</span>
                </div>
                <div class="campo">
                    <span class="lbl">Duração</span>
                    <span class="val">{{ duracaoMin }} minutos</span>
                </div>
                <div class="campo">
                    <span class="lbl">Profissional</span>
                    <span class="val">{{ agendamento.profissionalNome }}</span>
                </div>
                <div class="campo">
                    <span class="lbl">Tipo de consulta</span>
                    <span class="val">{{ agendamento.tipoServico }}</span>
                </div>
                <div class="campo">
                    <span class="lbl">Criado por</span>
                    <span class="val">{{ agendamento.criadoPorNome }}</span>
                </div>
                <div class="campo">
                    <span class="lbl">Criado em</span>
                    <span class="val">{{ formatDataHora(agendamento.criadoEm) }}</span>
                </div>
            </div>
            <div v-if="agendamento.observacoes" class="obs-block">
                <span class="lbl"><i class="fa-solid fa-circle-info" aria-hidden="true"></i> Observações</span>
                <p>{{ agendamento.observacoes }}</p>
            </div>
            <div v-if="agendamento.checkInEm" class="campo checkin-pill">
                <span class="lbl"><i class="fa-solid fa-user-check" aria-hidden="true"></i> Check-in</span>
                <span class="val checkin-val">
                    Check-in às {{ fmtHora(agendamento.checkInEm) }}
                </span>
            </div>
            <div v-if="agendamento.motivoCancelamento" class="obs-block obs-cancel">
                <span class="lbl"><i class="fa-solid fa-ban" aria-hidden="true"></i> Motivo do cancelamento</span>
                <p>{{ agendamento.motivoCancelamento }}</p>
            </div>
            <div v-if="podeAlterar" class="detalhe-acoes">
                <button
                    type="button"
                    class="btn-acao"
                    @click="emit('reagendar', agendamento)"
                >
                    <i class="fa-solid fa-rotate-right" aria-hidden="true"></i> Reagendar
                </button>
                <button type="button" class="btn-acao" @click="emit('editar', agendamento)">
                    <i class="fa-solid fa-pen-to-square" aria-hidden="true"></i> Editar
                </button>
            </div>
        </div>
    </div>
</template>

<style scoped>
.appt {
    display: grid;
    grid-template-columns: 86px 4px 1fr auto;
    grid-template-rows: auto;
    align-items: start;
    gap: 14px;
    padding: 13px 16px;
    border-bottom: 1px solid hsl(var(--foreground) / 0.05);
    cursor: pointer;
    transition: background 0.15s;
    position: relative;
}
.appt.expandido { background: hsl(var(--primary, 254 56% 38%) / 0.04); }
.appt:last-child { border-bottom: 0; }
.appt:hover { background: hsl(var(--primary, 254 56% 38%) / 0.025); }

.timecol {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    padding-top: 2px;
}
.time {
    font-size: 18px;
    font-weight: 800;
    color: hsl(var(--primary-dark, 254 56% 21%));
    line-height: 1;
    letter-spacing: -0.025em;
    font-variant-numeric: tabular-nums;
}
.duration {
    font-size: 10px;
    color: hsl(var(--foreground) / 0.55);
    font-weight: 600;
    margin-top: 5px;
    display: inline-flex;
    align-items: center;
    gap: 4px;
}
.duration i { font-size: 9px; }

.stripe {
    width: 4px;
    border-radius: 4px;
    align-self: stretch;
    min-height: 36px;
}

.body {
    display: flex;
    flex-direction: column;
    min-width: 0;
}
.head-row {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
}
.av {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: hsl(var(--primary, 254 56% 38%) / 0.10);
    color: hsl(var(--primary, 254 56% 38%));
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 11px;
    flex-shrink: 0;
    border: 2px solid hsl(var(--primary, 254 56% 38%) / 0.18);
}
.pat-name {
    font-size: 14px;
    font-weight: 700;
    color: hsl(var(--foreground));
    letter-spacing: -0.005em;
}

.pill {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    padding: 3px 9px;
    border-radius: 999px;
    font-size: 11px;
    font-weight: 700;
    line-height: 1.4;
    white-space: nowrap;
    letter-spacing: 0.005em;
}
.dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: currentColor;
    flex-shrink: 0;
}

.meta-row {
    font-size: 12px;
    color: hsl(var(--foreground) / 0.65);
    margin-top: 5px;
    display: flex;
    gap: 12px;
    flex-wrap: wrap;
    align-items: center;
}
.meta-row span {
    display: inline-flex;
    gap: 5px;
    align-items: center;
}
.meta-row i {
    font-size: 10px;
    color: hsl(var(--foreground) / 0.45);
}
.meta-row .sep {
    width: 3px;
    height: 3px;
    border-radius: 50%;
    background: hsl(var(--foreground) / 0.2);
}
.obs {
    color: hsl(var(--foreground) / 0.55);
    font-style: italic;
    max-width: 320px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.actions {
    display: flex;
    gap: 6px;
    align-items: center;
    padding-left: 4px;
}
.btn-icon-sm {
    width: 30px;
    height: 30px;
    border-radius: 6px;
    border: 1px solid hsl(var(--foreground) / 0.12);
    background: hsl(var(--card));
    color: hsl(var(--foreground) / 0.6);
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    transition: all 0.15s;
    font-family: inherit;
}
.btn-icon-sm:hover {
    color: hsl(var(--primary, 254 56% 38%));
    border-color: hsl(var(--primary, 254 56% 38%));
    background: hsl(var(--primary, 254 56% 38%) / 0.04);
}

.btn-cta {
    padding: 7px 13px;
    border-radius: 6px;
    border: 1px solid hsl(var(--primary, 254 56% 38%) / 0.3);
    background: hsl(var(--primary, 254 56% 38%));
    color: white;
    font-size: 12px;
    font-weight: 700;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    gap: 6px;
    align-items: center;
    transition: all 0.15s;
    white-space: nowrap;
    box-shadow: 0 1px 4px hsl(var(--primary, 254 56% 38%) / 0.3);
}
.btn-cta:hover {
    background: hsl(var(--primary-dark, 254 56% 21%));
}
.btn-cta i { font-size: 11px; }

.appt.cancelado .pat-name {
    text-decoration: line-through;
    color: hsl(var(--foreground) / 0.45);
}
.appt.cancelado .time {
    color: hsl(var(--foreground) / 0.4);
}

/* ── Painel inline de detalhes ── */
.detalhe {
    grid-column: 3 / -1;
    margin-top: 12px;
    padding: 12px 14px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--foreground) / 0.08);
    border-radius: 8px;
    cursor: default;
    display: flex;
    flex-direction: column;
    gap: 12px;
}
.detalhe .grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 10px 16px;
}
.detalhe .campo {
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 0;
}
.detalhe .lbl {
    font-size: 10px;
    font-weight: 700;
    color: hsl(var(--foreground) / 0.55);
    text-transform: uppercase;
    letter-spacing: 0.04em;
    display: inline-flex;
    align-items: center;
    gap: 5px;
}
.detalhe .val {
    font-size: 12px;
    color: hsl(var(--foreground));
    font-weight: 500;
}
.detalhe .obs-block {
    background: hsl(var(--primary, 254 56% 38%) / 0.04);
    padding: 10px 12px;
    border-radius: 6px;
    border-left: 3px solid hsl(var(--primary, 254 56% 38%));
}
.detalhe .obs-block.obs-cancel {
    background: hsl(0 84% 60% / 0.06);
    border-left-color: hsl(0 84% 60%);
}
.detalhe .obs-block p {
    margin: 4px 0 0;
    font-size: 12px;
    color: hsl(var(--foreground));
    line-height: 1.5;
}
.detalhe-acoes {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
}
.btn-acao {
    padding: 7px 14px;
    border-radius: 6px;
    border: 1px solid hsl(var(--foreground) / 0.12);
    background: hsl(var(--card));
    color: hsl(var(--primary, 254 56% 38%));
    font-size: 12px;
    font-weight: 700;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    gap: 6px;
    align-items: center;
    transition: all 0.15s;
}
.btn-acao:hover {
    background: hsl(var(--primary, 254 56% 38%));
    color: white;
    border-color: hsl(var(--primary, 254 56% 38%));
}

.checkin-pill {
    background: hsl(160 79% 39% / 0.08);
    border: 1px solid hsl(160 79% 39% / 0.25);
    border-radius: 6px;
    padding: 6px 10px;
}
.checkin-val {
    color: hsl(160 79% 28%) !important;
    font-weight: 600 !important;
    display: inline-flex;
    align-items: center;
    gap: 5px;
}
.checkin-pill .lbl {
    color: hsl(160 79% 28%);
}

@media (max-width: 720px) {
    .appt {
        grid-template-columns: 64px 4px 1fr;
        gap: 10px;
    }
    .actions {
        grid-column: 1 / -1;
        margin-top: 8px;
        justify-content: flex-end;
    }
    .obs { max-width: 200px; }
    .detalhe { grid-column: 1 / -1; }
}
</style>
