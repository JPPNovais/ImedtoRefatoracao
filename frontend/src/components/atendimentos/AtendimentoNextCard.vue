<!--
    Card "Próximo paciente" — versão menor do active card. Mostra o próximo
    agendamento da fila com botão "Iniciar atendimento".
-->
<script setup lang="ts">
import { computed } from "vue"
import type { Agendamento } from "@/services/agendaService"

const props = defineProps<{
    agendamento: Agendamento
}>()

defineEmits<{
    "iniciar": []
}>()

const iniciais = computed(() => {
    const partes = props.agendamento.pacienteNome.trim().split(/\s+/).filter(Boolean)
    if (partes.length === 0) return "?"
    if (partes.length === 1) return partes[0].slice(0, 2).toUpperCase()
    return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase()
})

const horaInicio = computed(() =>
    new Date(props.agendamento.inicioPrevisto).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" }),
)

const horaCheckIn = computed(() => {
    if (!props.agendamento.checkInEm) return null
    return new Date(props.agendamento.checkInEm).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })
})
</script>

<template>
    <article class="next-card">
        <div class="next-label">
            <i class="fa-solid fa-arrow-right" aria-hidden="true"></i>
            Próximo
        </div>

        <div class="next-body">
            <div class="next-head">
                <div class="next-avatar" :title="agendamento.pacienteNome">{{ iniciais }}</div>
                <div class="next-info">
                    <h3>{{ agendamento.pacienteNome }}</h3>
                    <div class="next-meta">
                        <span><i class="fa-solid fa-clock" aria-hidden="true"></i> {{ horaInicio }}</span>
                        <span class="dot">·</span>
                        <span>{{ agendamento.tipoServico || "Consulta" }}</span>
                    </div>
                </div>
            </div>

            <div v-if="horaCheckIn" class="arrived-tag">
                <i class="fa-solid fa-door-open" aria-hidden="true"></i>
                Já chegou às {{ horaCheckIn }}
            </div>
        </div>

        <button type="button" class="next-btn" @click="$emit('iniciar')">
            <i class="fa-solid fa-play" aria-hidden="true"></i>
            Iniciar atendimento
        </button>
    </article>
</template>

<style scoped>
.next-card {
    background: white;
    padding: 18px 20px;
    border-radius: var(--radius-xl);
    box-shadow: var(--shadow-sm);
    border: 1px solid hsl(var(--secondary) / 0.06);
    display: flex; flex-direction: column; gap: 14px;
}
.next-label {
    display: flex; align-items: center; gap: 6px;
    font-size: 11px; font-weight: 700;
    text-transform: uppercase; letter-spacing: 0.06em;
    color: hsl(var(--primary));
}
.next-body { display: flex; flex-direction: column; gap: 10px; flex: 1; }
.next-head { display: flex; gap: 12px; align-items: center; }
.next-avatar {
    width: 48px; height: 48px; border-radius: 50%;
    background: linear-gradient(135deg, hsl(var(--primary)), hsl(var(--primary-dark)));
    color: white; font-weight: 700; font-size: 16px;
    display: inline-flex; align-items: center; justify-content: center;
    flex-shrink: 0;
}
.next-info { min-width: 0; flex: 1; }
.next-info h3 {
    margin: 0 0 4px;
    font-size: 16px; font-weight: 700;
    color: hsl(var(--primary-dark));
}
.next-meta {
    display: flex; flex-wrap: wrap; gap: 6px;
    font-size: 12px; color: hsl(var(--secondary) / 0.75);
    align-items: center;
}
.next-meta .dot { opacity: 0.5; }
.next-meta i { font-size: 10px; margin-right: 3px; }

.arrived-tag {
    display: inline-flex; align-items: center; gap: 6px;
    background: hsl(var(--warning) / 0.14); color: hsl(38 80% 30%);
    padding: 4px 10px; border-radius: 99px;
    font-size: 12px; font-weight: 600;
    width: fit-content;
}

.next-btn {
    display: inline-flex; align-items: center; justify-content: center; gap: 8px;
    width: 100%; height: 36px; padding: 0 14px;
    border-radius: var(--radius-md);
    background: hsl(var(--primary)); color: white;
    border: 1px solid hsl(var(--primary));
    font: inherit; font-weight: 600; font-size: 13px;
    cursor: pointer;
    transition: background 150ms, box-shadow 150ms;
}
.next-btn:hover {
    background: hsl(var(--primary-dark));
    box-shadow: var(--shadow-primary);
}
</style>
