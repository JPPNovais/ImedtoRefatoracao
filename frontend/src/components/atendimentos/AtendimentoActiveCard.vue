<!--
    Card grande "Em atendimento agora" — destaque visual roxo com timer ao vivo,
    alergias do paciente como pills e ações principais (Abrir prontuário,
    Pausar, Encaminhar, Finalizar). Pausar/Encaminhar ainda não tem suporte
    no backend e ficam desabilitados ("em breve").
-->
<script setup lang="ts">
import { computed } from "vue"
import type { Agendamento } from "@/services/agendaService"
import { useClockTick, formatarDuracao } from "@/composables/useClockTick"

const props = defineProps<{
    agendamento: Agendamento
    iniciadoEm: string                 // ISO datetime — quando o atendimento começou
    alertasPaciente?: string[]
}>()

defineEmits<{
    "abrir-prontuario": []
    "finalizar": []
}>()

const { agora } = useClockTick()

const iniciais = computed(() => {
    const partes = props.agendamento.pacienteNome.trim().split(/\s+/).filter(Boolean)
    if (partes.length === 0) return "?"
    if (partes.length === 1) return partes[0].slice(0, 2).toUpperCase()
    return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase()
})

const decorridoMs = computed(() => agora.value - new Date(props.iniciadoEm).getTime())

const decorridoLabel = computed(() => formatarDuracao(decorridoMs.value))

const duracaoTotalMin = computed(() => {
    const ini = new Date(props.agendamento.inicioPrevisto).getTime()
    const fim = new Date(props.agendamento.fimPrevisto).getTime()
    return Math.max(1, Math.round((fim - ini) / 60_000))
})

const horaInicio = computed(() =>
    new Date(props.agendamento.inicioPrevisto).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" }),
)
</script>

<template>
    <article class="active-card">
        <div class="active-head">
            <div class="active-label">
                <span class="pulse"></span>
                Em atendimento agora
            </div>
            <div class="active-timer">
                <i class="fa-solid fa-stopwatch" aria-hidden="true"></i>
                <span>{{ decorridoLabel }}</span>
                <span class="timer-sub">/ {{ duracaoTotalMin }}min</span>
            </div>
        </div>

        <div class="active-body">
            <div class="active-avatar lg" :title="agendamento.pacienteNome">{{ iniciais }}</div>

            <div class="active-info">
                <h2>{{ agendamento.pacienteNome }}</h2>
                <div class="active-meta">
                    <span><i class="fa-solid fa-clock" aria-hidden="true"></i> {{ horaInicio }}</span>
                    <span class="dot">·</span>
                    <span>{{ agendamento.tipoServico || "Consulta" }}</span>
                </div>

                <div v-if="agendamento.observacoes" class="active-reason">
                    <strong>{{ agendamento.observacoes }}</strong>
                </div>

                <div v-if="alertasPaciente && alertasPaciente.length > 0" class="active-alerts">
                    <span v-for="a in alertasPaciente" :key="a" class="alert-pill">
                        <i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i>
                        {{ a }}
                    </span>
                </div>
            </div>
        </div>

        <div class="active-actions">
            <button type="button" class="ac-btn ac-primary" @click="$emit('abrir-prontuario')">
                <i class="fa-solid fa-file-medical" aria-hidden="true"></i>
                Abrir prontuário
            </button>
            <button type="button" class="ac-btn ac-secondary" disabled title="Em breve">
                <i class="fa-solid fa-pause" aria-hidden="true"></i>
                Pausar
            </button>
            <button type="button" class="ac-btn ac-secondary" disabled title="Em breve">
                <i class="fa-solid fa-share" aria-hidden="true"></i>
                Encaminhar
            </button>
            <button type="button" class="ac-btn ac-success" @click="$emit('finalizar')">
                <i class="fa-solid fa-circle-check" aria-hidden="true"></i>
                Finalizar
            </button>
        </div>
    </article>
</template>

<style scoped>
.active-card {
    background: linear-gradient(135deg, hsl(265 60% 50%) 0%, hsl(var(--primary-dark)) 100%);
    color: white;
    padding: 22px 24px;
    border-radius: var(--radius-xl);
    box-shadow: 0 12px 36px -8px hsl(265 60% 35% / 0.45);
    position: relative;
    overflow: hidden;
}
.active-card::before {
    content: "";
    position: absolute; inset: -50% -10% auto auto;
    width: 280px; height: 280px; border-radius: 50%;
    background: radial-gradient(circle, hsl(0 0% 100% / 0.15), transparent 70%);
    pointer-events: none;
}
.active-head {
    display: flex; align-items: center; justify-content: space-between;
    margin-bottom: 18px; position: relative;
}
.active-label {
    display: flex; align-items: center; gap: 8px;
    font-size: 12px; font-weight: 700;
    text-transform: uppercase; letter-spacing: 0.06em;
    opacity: 0.92;
}
.pulse {
    width: 8px; height: 8px; border-radius: 50%;
    background: hsl(155 80% 60%);
    box-shadow: 0 0 0 0 hsl(155 80% 60% / 0.7);
    animation: pulse 1.6s infinite;
}
@keyframes pulse {
    70%  { box-shadow: 0 0 0 12px transparent; }
    100% { box-shadow: 0 0 0 0 transparent; }
}
.active-timer {
    display: flex; align-items: baseline; gap: 8px;
    font-family: var(--font-mono); font-size: 22px; font-weight: 700;
}
.active-timer .timer-sub { font-size: 12px; opacity: 0.7; font-weight: 500; }

.active-body {
    display: flex; gap: 18px; align-items: flex-start;
    position: relative; margin-bottom: 18px;
}
.active-info { flex: 1; min-width: 0; }
.active-info h2 { font-size: 22px; font-weight: 700; margin: 0 0 4px; }
.active-meta {
    display: flex; flex-wrap: wrap; align-items: center; gap: 8px;
    font-size: 13px; opacity: 0.88;
}
.active-meta .dot { opacity: 0.5; }
.active-meta i { font-size: 11px; margin-right: 3px; }
.active-reason { margin-top: 10px; display: flex; gap: 8px; align-items: baseline; flex-wrap: wrap; }
.active-reason strong { font-size: 15px; }
.active-alerts { margin-top: 12px; display: flex; gap: 6px; flex-wrap: wrap; }
.active-alerts .alert-pill {
    background: hsl(38 92% 50% / 0.22);
    color: hsl(38 100% 88%);
    border-color: hsl(38 92% 60% / 0.4);
}

.active-avatar {
    width: 76px; height: 76px; border-radius: 50%;
    background: linear-gradient(135deg, hsl(255 70% 70%), hsl(var(--primary-dark)));
    color: white; font-weight: 700; font-size: 24px;
    display: inline-flex; align-items: center; justify-content: center;
    flex-shrink: 0;
    border: 2px solid hsl(0 0% 100% / 0.25);
}

.active-actions { display: flex; gap: 8px; flex-wrap: wrap; position: relative; }
.ac-btn {
    display: inline-flex; align-items: center; justify-content: center; gap: 8px;
    height: 44px; padding: 0 22px;
    border-radius: var(--radius-md);
    font: inherit; font-weight: 600; font-size: 14px;
    border: 1px solid transparent; cursor: pointer;
    transition: background 150ms, border-color 150ms, color 150ms, box-shadow 150ms;
    white-space: nowrap;
}
.ac-btn:disabled { opacity: 0.55; cursor: not-allowed; }
.ac-primary {
    background: white; color: hsl(var(--primary-dark));
}
.ac-primary:hover:not(:disabled) { box-shadow: 0 6px 18px hsl(0 0% 0% / 0.2); }
.ac-secondary {
    background: hsl(0 0% 100% / 0.14); color: white;
    border-color: hsl(0 0% 100% / 0.25);
}
.ac-secondary:hover:not(:disabled) { background: hsl(0 0% 100% / 0.22); }
.ac-success {
    background: hsl(155 60% 50%); color: white;
}
.ac-success:hover:not(:disabled) { background: hsl(155 60% 42%); }

@media (max-width: 720px) {
    .active-actions { flex-direction: column; align-items: stretch; }
    .ac-btn { width: 100%; }
}
</style>
