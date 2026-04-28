<!--
    Cabeçalho do prontuário.
    Replica o visual do legado (`modules/medical-record/components/MedicalRecordPatientHeader.vue`):
      - Topo: nome do estabelecimento em destaque (azul-escuro).
      - Grade 2×3: Paciente, Documento, Contato / Data horário, Tipo, Profissional.
-->
<script setup lang="ts">
import { computed } from "vue"
import type { Paciente } from "@/services/pacienteService"
import type { Agendamento } from "@/services/agendaService"

const props = defineProps<{
    paciente: Paciente | null
    agendamento?: Agendamento | null
    estabelecimento?: string | null
}>()

function calcularIdade(iso: string | null | undefined) {
    if (!iso) return null
    const nasc = new Date(iso)
    const hoje = new Date()
    let idade = hoje.getFullYear() - nasc.getFullYear()
    const m = hoje.getMonth() - nasc.getMonth()
    if (m < 0 || (m === 0 && hoje.getDate() < nasc.getDate())) idade--
    return idade
}

const idade = computed(() => calcularIdade(props.paciente?.dataNascimento))

const dataHorario = computed(() => {
    const iso = props.agendamento?.inicioPrevisto
    if (!iso) return "—"
    return new Date(iso).toLocaleString("pt-BR", {
        day: "2-digit", month: "2-digit", year: "numeric",
        hour: "2-digit", minute: "2-digit",
    })
})

const tipoConsulta = computed(() => {
    const t = props.agendamento?.tipoServico
    if (!t) return "—"
    return t.toLowerCase()
})

const contato = computed(() => {
    const p = props.paciente
    if (!p) return []
    const linhas: string[] = []
    if (p.telefone) linhas.push(p.telefone)
    if (p.endereco) linhas.push(p.endereco)
    return linhas
})

const documento = computed(() => {
    const p = props.paciente
    if (!p) return []
    const linhas: string[] = []
    if (p.cpf) linhas.push(p.cpf)
    if (p.genero) linhas.push(p.genero.toLowerCase())
    return linhas
})
</script>

<template>
    <section class="ph">
        <h2 v-if="estabelecimento" class="ph-estab">{{ estabelecimento }}</h2>

        <div class="ph-grid">
            <div class="ph-col">
                <span class="ph-label">Paciente</span>
                <span class="ph-val">{{ paciente?.nomeCompleto ?? "—" }}</span>
                <span class="ph-sub">{{ idade !== null ? `${idade} anos` : "—" }}</span>
            </div>

            <div class="ph-col">
                <span class="ph-label">Documento</span>
                <span class="ph-val">{{ documento[0] ?? "—" }}</span>
                <span class="ph-sub">{{ documento[1] ?? "" }}</span>
            </div>

            <div class="ph-col">
                <span class="ph-label">Contato</span>
                <span class="ph-val">{{ contato[0] ?? "—" }}</span>
                <span class="ph-sub">{{ contato[1] ?? "" }}</span>
            </div>

            <div class="ph-col">
                <span class="ph-label">Data / horário da consulta</span>
                <span class="ph-val">{{ dataHorario }}</span>
            </div>

            <div class="ph-col">
                <span class="ph-label">Tipo de consulta</span>
                <span class="ph-val">{{ tipoConsulta }}</span>
            </div>

            <div class="ph-col">
                <span class="ph-label">Profissional responsável</span>
                <span class="ph-val">{{ agendamento?.profissionalNome ?? "—" }}</span>
            </div>
        </div>
    </section>
</template>

<style scoped>
.ph {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 1.25rem 1.5rem;
    margin-bottom: 1.25rem;
}

.ph-estab {
    margin: 0 0 1rem; font-size: 1.1rem; font-weight: 700;
    color: hsl(var(--primary-dark));
}

.ph-grid {
    display: grid; gap: 1.1rem 2.25rem;
    grid-template-columns: repeat(3, minmax(0, 1fr));
}
@media (max-width: 900px) {
    .ph-grid { grid-template-columns: 1fr 1fr; }
}
@media (max-width: 600px) {
    .ph-grid { grid-template-columns: 1fr; }
}

.ph-col { display: flex; flex-direction: column; gap: 0.1rem; min-width: 0; }

.ph-label {
    font-size: 0.82em; font-weight: 700; color: var(--text);
    margin-bottom: 0.15rem;
}
.ph-val {
    font-size: 0.88em; color: var(--text);
    overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.ph-sub {
    font-size: 0.8em; color: var(--text-muted);
    overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
</style>
