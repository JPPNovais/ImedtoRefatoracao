<script setup lang="ts">
import ReceitasPacienteTab from "@/components/prontuario/ReceitasPacienteTab.vue"

defineProps<{
    pacienteId: number
    pacienteNome: string
}>()
</script>

<template>
    <ReceitasPacienteTab :paciente-id="pacienteId" :paciente-nome="pacienteNome" />
</template>
