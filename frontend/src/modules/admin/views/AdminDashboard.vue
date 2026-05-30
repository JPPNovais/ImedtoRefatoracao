<script setup lang="ts">
/**
 * AdminDashboard.vue — painel operacional do admin global (Wave 6).
 *
 * W6-CA1..CA26: KPIs, gráfico de crescimento, alertas, feed de audit log.
 * W6-CA24: carregamento paralelo via Promise.allSettled no onMounted.
 * W6-CA22: leitura não gera audit.
 * W6-CA3: falha de um bloco não afeta os outros.
 *
 * Refatora o placeholder anterior (Wave 3) mantendo app-page + AppPageHeader.
 */
import { onMounted } from "vue"
import { AppPageHeader } from "@/components/ui"
import { useAdminAuthStore } from "../stores/adminAuthStore"
import { useDashboardStore } from "../stores/dashboardStore"
import KpisGrid from "../components/dashboard/KpisGrid.vue"
import CrescimentoChart from "../components/dashboard/CrescimentoChart.vue"
import AlertasCard from "../components/dashboard/AlertasCard.vue"
import AuditLogFeed from "../components/dashboard/AuditLogFeed.vue"

const authStore = useAdminAuthStore()
const dashboardStore = useDashboardStore()

// W6-CA24: todos os blocos em paralelo — falha de um não bloqueia os outros.
onMounted(() => {
    Promise.allSettled([
        dashboardStore.carregarKpis(),
        dashboardStore.carregarCrescimento(),
        dashboardStore.carregarAlertas(),
        dashboardStore.carregarAuditLog(),
    ])
})
</script>

<template>
    <main class="app-page">
        <AppPageHeader
            :titulo="`Bem-vindo, ${authStore.admin?.nome ?? 'Administrador'}`"
            subtitulo="Painel operacional do Imedto. Toda ação sensível é registrada em audit log."
        />

        <!-- Bloco 1: KPIs -->
        <KpisGrid />

        <!-- Bloco 2: Gráfico de crescimento mensal -->
        <CrescimentoChart />

        <!-- Bloco 3: Alertas acionáveis -->
        <AlertasCard />

        <!-- Bloco 4: Feed de audit log -->
        <AuditLogFeed />
    </main>
</template>
