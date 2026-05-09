<script setup lang="ts">
import { AppStatCard } from "@/components/ui"
import { formatarMoedaBrl } from "@/utils/format"

defineProps<{
    valorTotal: number
    totalItens: number
    baixoCount: number
    vencendoCount: number
    movimentacoesHoje: number
}>()
</script>

<template>
    <div class="kpis-grid">
        <AppStatCard
            label="Valor em estoque"
            :valor="formatarMoedaBrl(valorTotal)"
            icone="fa-solid fa-coins"
            cor="primary"
            legenda="custo médio ponderado"
        />
        <AppStatCard
            label="Itens cadastrados"
            :valor="totalItens"
            icone="fa-solid fa-boxes-stacked"
            cor="info"
            legenda="ativos no cadastro"
        />
        <AppStatCard
            label="Estoque baixo"
            :valor="baixoCount"
            icone="fa-solid fa-arrow-trend-down"
            cor="error"
            legenda="requerem reposição"
        />
        <AppStatCard
            label="Vencendo (30d)"
            :valor="vencendoCount"
            icone="fa-solid fa-hourglass-half"
            cor="warning"
            legenda="lotes próximos ao vencimento"
        />
        <AppStatCard
            label="Movimentações hoje"
            :valor="movimentacoesHoje"
            icone="fa-solid fa-clock-rotate-left"
            cor="primary"
            legenda="entradas e saídas"
        />
    </div>
</template>

<style scoped>
.kpis-grid {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 12px;
    margin-bottom: 20px;
}
@media (max-width: 1200px) {
    .kpis-grid { grid-template-columns: repeat(3, 1fr); }
}
@media (max-width: 768px) {
    .kpis-grid { grid-template-columns: repeat(2, 1fr); }
}
</style>
