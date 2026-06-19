<!--
    Linha da fila do dia. Estilizada conforme o status (em atendimento, em espera,
    confirmado, concluído etc.). Botão de ação muda dinamicamente.
-->
<script setup lang="ts">
import { computed } from "vue"
import type { Agendamento } from "@/services/agendaService"

type StatusVisual = "em-atendimento" | "concluido" | "em-espera" | "confirmado" | "agendado" | "cancelado" | "expirado"

const props = defineProps<{
    agendamento: Agendamento
    /** Quando true, esta linha está em atendimento agora (marca local) */
    emAtendimento?: boolean
}>()

defineEmits<{
    "abrir-prontuario": []
    "iniciar": []
    "finalizar": []
    "checkin": []
    "trocar-sala": []
}>()

const statusVisual = computed<StatusVisual>(() => {
    if (props.emAtendimento) return "em-atendimento"
    switch (props.agendamento.status) {
        case "Concluido":  return "concluido"
        case "Cancelado":  return "cancelado"
        case "Confirmado": return props.agendamento.checkInEm ? "em-espera" : "confirmado"
        case "Agendado":   return "agendado"
        case "Expirado":   return "expirado"
        default:           return "agendado"
    }
})

const iniciais = computed(() => {
    const partes = props.agendamento.pacienteNome.trim().split(/\s+/).filter(Boolean)
    if (partes.length === 0) return "?"
    if (partes.length === 1) return partes[0].slice(0, 2).toUpperCase()
    return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase()
})

const horaInicio = computed(() =>
    new Date(props.agendamento.inicioPrevisto).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" }),
)

const duracaoMin = computed(() => {
    const ini = new Date(props.agendamento.inicioPrevisto).getTime()
    const fim = new Date(props.agendamento.fimPrevisto).getTime()
    return Math.max(1, Math.round((fim - ini) / 60_000))
})

const isEncaixe = computed(() => (props.agendamento.tipoServico ?? "").toLowerCase() === "encaixe")
</script>

<template>
    <div class="q-row" :class="`q-${statusVisual}`">
        <div class="q-time">
            <div class="time-big">{{ horaInicio }}</div>
            <div class="time-sub">{{ duracaoMin }}min</div>
        </div>

        <div class="q-status">
            <span v-if="statusVisual === 'em-atendimento'" class="status-pill info">
                <i class="fa-solid fa-stethoscope"></i> Em atendimento
            </span>
            <span v-else-if="statusVisual === 'concluido'" class="status-pill success">
                <i class="fa-solid fa-circle-check"></i> Concluído
            </span>
            <span v-else-if="statusVisual === 'em-espera'" class="status-pill warning">
                <i class="fa-solid fa-hourglass-half"></i> Em espera
            </span>
            <span v-else-if="statusVisual === 'confirmado'" class="status-pill primary">
                <i class="fa-solid fa-check"></i> Confirmado
            </span>
            <span v-else-if="statusVisual === 'agendado'" class="status-pill neutral">
                <i class="fa-solid fa-calendar"></i> Agendado
            </span>
            <span v-else-if="statusVisual === 'expirado'" class="status-pill expirado">
                <i class="fa-solid fa-clock-rotate-left"></i> Expirado
            </span>
            <span v-else class="status-pill error">
                <i class="fa-solid fa-ban"></i> Cancelado
            </span>
        </div>

        <div class="q-patient">
            <div class="q-avatar" :title="agendamento.pacienteNome">{{ iniciais }}</div>
            <div class="q-patient-info">
                <div class="q-name">
                    {{ agendamento.pacienteNome }}
                    <span v-if="isEncaixe" class="q-badge-encaixe">Encaixe</span>
                </div>
                <div class="q-sub">
                    <span>{{ agendamento.tipoServico || "Consulta" }}</span>
                    <button
                        v-if="agendamento.salaNome"
                        type="button"
                        class="q-sala-chip"
                        title="Trocar sala"
                        @click.stop="$emit('trocar-sala')"
                    >
                        <i class="fa-solid fa-door-open"></i>
                        {{ agendamento.salaNome }}
                    </button>
                    <button
                        v-else-if="agendamento.checkInEm"
                        type="button"
                        class="q-sala-alocar"
                        @click.stop="$emit('trocar-sala')"
                    >
                        <i class="fa-solid fa-door-open"></i>
                        Alocar sala
                    </button>
                </div>
            </div>
        </div>

        <div class="q-reason">
            <div v-if="agendamento.observacoes">{{ agendamento.observacoes }}</div>
            <div v-else class="q-reason-empty">—</div>
        </div>

        <div class="q-actions">
            <template v-if="statusVisual === 'em-atendimento'">
                <button class="q-btn q-btn-success" type="button" @click="$emit('finalizar')">
                    <i class="fa-solid fa-circle-check"></i> Finalizar
                </button>
            </template>
            <template v-else-if="statusVisual === 'em-espera'">
                <button class="q-btn q-btn-primary" type="button" @click="$emit('iniciar')">
                    <i class="fa-solid fa-play"></i> Iniciar
                </button>
            </template>
            <template v-else-if="statusVisual === 'confirmado' || statusVisual === 'agendado'">
                <button class="q-btn q-btn-secondary" type="button" @click="$emit('checkin')">
                    <i class="fa-solid fa-door-open"></i> Check-in
                </button>
            </template>
            <template v-else-if="statusVisual === 'concluido'">
                <button class="q-btn q-btn-ghost" type="button" @click="$emit('abrir-prontuario')" title="Ver prontuário">
                    <i class="fa-solid fa-eye"></i>
                </button>
            </template>
        </div>
    </div>
</template>

<style scoped>
.q-row {
    display: grid;
    grid-template-columns: 80px 140px 1.4fr 1.6fr auto;
    align-items: center; gap: 16px;
    padding: 14px 20px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
    transition: background 150ms;
}
.q-row:last-child { border-bottom: 0; }
.q-row:hover { background: hsl(var(--primary) / 0.03); }
.q-concluido { opacity: 0.65; }
.q-em-atendimento { background: hsl(155 60% 50% / 0.05); border-left: 3px solid hsl(155 60% 50%); padding-left: 17px; }
.q-em-espera     { background: hsl(38 92% 50% / 0.04); }
.q-cancelado     { opacity: 0.55; background: hsl(0 75% 60% / 0.03); }
.q-expirado      { opacity: 0.6; background: hsl(var(--status-expirado-cor, 220 9% 60%) / 0.03); }

.q-time { text-align: center; }
.time-big {
    font-size: 17px; font-weight: 700;
    color: hsl(var(--primary-dark));
    font-family: var(--font-mono);
}
.time-sub { font-size: 11px; color: hsl(var(--secondary) / 0.55); }

.q-status { display: flex; }

.q-patient { display: flex; align-items: center; gap: 10px; min-width: 0; }
.q-patient-info { min-width: 0; }
.q-avatar {
    width: 36px; height: 36px; border-radius: 50%;
    background: linear-gradient(135deg, hsl(var(--primary)), hsl(var(--primary-dark)));
    color: white; font-weight: 700; font-size: 12px;
    display: inline-flex; align-items: center; justify-content: center;
    flex-shrink: 0;
}
.q-name {
    font-weight: 600; color: hsl(var(--primary-dark));
    font-size: 14px;
    overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    display: inline-flex; align-items: center; gap: 6px;
}
.q-badge-encaixe {
    display: inline-flex; align-items: center;
    padding: 1px 8px; border-radius: 999px;
    background: hsl(0 84% 60% / 0.12); color: hsl(0 72% 45%);
    font-size: 10px; font-weight: 700;
}
.q-sub { font-size: 12px; color: hsl(var(--secondary) / 0.65); display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.q-sala-chip,
.q-sala-alocar {
    display: inline-flex; align-items: center; gap: 4px;
    padding: 2px 8px; border-radius: 999px;
    font: inherit; font-size: 11px; font-weight: 600;
    cursor: pointer;
    transition: background 150ms;
}
.q-sala-chip {
    background: hsl(var(--primary) / 0.08);
    color: hsl(var(--primary-dark));
    border: 1px solid hsl(var(--primary) / 0.18);
}
.q-sala-chip:hover { background: hsl(var(--primary) / 0.14); }
.q-sala-chip i { font-size: 9px; }
.q-sala-alocar {
    background: transparent;
    color: hsl(var(--primary));
    border: 1px dashed hsl(var(--primary) / 0.35);
}
.q-sala-alocar:hover { background: hsl(var(--primary) / 0.06); }
.q-sala-alocar i { font-size: 9px; }

.q-reason {
    font-size: 13px; color: hsl(var(--secondary) / 0.9);
    min-width: 0;
}
.q-reason-empty { color: hsl(var(--secondary) / 0.4); }

.q-actions { display: flex; gap: 6px; align-items: center; justify-content: flex-end; }
.q-btn {
    display: inline-flex; align-items: center; gap: 6px;
    height: 32px; padding: 0 12px;
    border-radius: var(--radius-md);
    font: inherit; font-weight: 600; font-size: 12px;
    border: 1px solid transparent; cursor: pointer;
    white-space: nowrap;
    transition: background 150ms, border-color 150ms, color 150ms;
}
.q-btn i { font-size: 11px; }
.q-btn-primary   { background: hsl(var(--primary)); color: white; }
.q-btn-primary:hover { background: hsl(var(--primary-dark)); }
.q-btn-success   { background: hsl(155 60% 50%); color: white; }
.q-btn-success:hover { background: hsl(155 60% 42%); }
.q-btn-secondary { background: white; color: hsl(var(--primary-dark));
    border-color: hsl(var(--secondary) / 0.18); }
.q-btn-secondary:hover { border-color: hsl(var(--primary) / 0.5); color: hsl(var(--primary)); }
.q-btn-ghost     { background: transparent; color: hsl(var(--secondary) / 0.7); padding: 0 10px; }
.q-btn-ghost:hover { background: hsl(var(--secondary) / 0.06); color: hsl(var(--primary-dark)); }

@media (max-width: 980px) {
    .q-row {
        grid-template-columns: 64px 1fr;
        grid-template-areas:
            "time   patient"
            "status patient"
            "reason reason"
            "actions actions";
        row-gap: 8px;
    }
    .q-time { grid-area: time; }
    .q-status { grid-area: status; }
    .q-patient { grid-area: patient; }
    .q-reason { grid-area: reason; }
    .q-actions { grid-area: actions; justify-content: flex-start; }
}
</style>
