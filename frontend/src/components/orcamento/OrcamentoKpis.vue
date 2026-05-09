<script setup lang="ts">
import type { OrcamentoResumo } from "@/services/orcamentoService"
import { computed } from "vue"
import { AppStatCard } from "@/components/ui"

const props = defineProps<{ orcamentos: OrcamentoResumo[] }>()

const hoje = new Date().toISOString().slice(0, 10)

function diasEntre(a: string, b: string) {
    return Math.round((new Date(b + "T00:00:00").getTime() - new Date(a + "T00:00:00").getTime()) / 86400000)
}

const kpis = computed(() => {
    const list = props.orcamentos
    const enviados   = list.filter(o => !["Rascunho"].includes(o.status))
    const aprovados  = list.filter(o => o.status === "Aprovado")
    const totalEnviado  = enviados.reduce((s, o) => s + o.total, 0)
    const totalAprovado = aprovados.reduce((s, o) => s + o.total, 0)
    const conversao  = enviados.length > 0 ? Math.round((aprovados.length / enviados.length) * 100) : 0
    const vencendo   = list.filter(o => {
        if (!["Enviado"].includes(o.status)) return false
        const d = diasEntre(hoje, o.validade)
        return d >= 0 && d <= 7
    })
    return { totalEnviado, totalAprovado, conversao, vencendo: vencendo.length, totalOrcamentos: list.length }
})

function fmtBRL(v: number) { return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) }
</script>

<template>
    <div class="kpis-grid">
        <AppStatCard
            label="Enviados"
            :valor="fmtBRL(kpis.totalEnviado)"
            icone="fa-solid fa-paper-plane"
            cor="info"
            :legenda="`${orcamentos.filter(o => o.status !== 'Rascunho').length} orçamentos`"
        />
        <AppStatCard
            label="Aprovados"
            :valor="fmtBRL(kpis.totalAprovado)"
            icone="fa-solid fa-circle-check"
            cor="success"
            :legenda="`${kpis.conversao}% de conversão`"
        />
        <AppStatCard
            label="Total no período"
            :valor="fmtBRL(orcamentos.reduce((s, o) => s + o.total, 0))"
            icone="fa-solid fa-coins"
            cor="primary"
            :legenda="`${kpis.totalOrcamentos} orçamentos`"
        />
        <AppStatCard
            label="Vencendo em 7 dias"
            :valor="kpis.vencendo"
            icone="fa-solid fa-clock"
            :cor="kpis.vencendo > 0 ? 'warning' : 'muted'"
            :legenda="kpis.vencendo > 0 ? 'Precisa de follow-up' : 'Tudo em dia'"
        />
    </div>
</template>

<style scoped>
.kpis-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 14px;
}
@media (max-width: 900px) { .kpis-grid { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 560px) { .kpis-grid { grid-template-columns: 1fr; } }
</style>
