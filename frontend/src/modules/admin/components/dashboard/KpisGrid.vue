<script setup lang="ts">
/**
 * KpisGrid — 4 cards de KPI do dashboard admin.
 * W6-CA1, CA2, CA3.
 * Consome dashboardStore. Loading/erro independente.
 */
import { AppStatCard } from "@/components/ui"
import { useDashboardStore } from "../../stores/dashboardStore"

const store = useDashboardStore()
</script>

<template>
    <section aria-label="Indicadores principais">
        <!-- Erro do bloco KPIs — não afeta os outros blocos -->
        <p v-if="store.erroKpis" class="bloco-erro" role="alert">
            {{ store.erroKpis }}
        </p>

        <div v-else class="kpis-grid">
            <!-- Estabelecimentos -->
            <AppStatCard
                label="Estabelecimentos ativos"
                :valor="store.carregandoKpis ? '...' : (store.kpis?.estabelecimentosAtivos ?? 0)"
                :legenda="store.carregandoKpis ? '' : `${store.kpis?.estabelecimentosInativos ?? 0} inativo(s)`"
                cor="primary"
            />

            <!-- Admins ativos -->
            <AppStatCard
                label="Admins ativos"
                :valor="store.carregandoKpis ? '...' : (store.kpis?.adminsAtivos ?? 0)"
                cor="info"
            />

            <!-- Trials -->
            <AppStatCard
                label="Trials em andamento"
                :valor="store.carregandoKpis ? '...' : (store.kpis?.trialsEmAndamento ?? 0)"
                :legenda="store.carregandoKpis ? '' : `${store.kpis?.trialsExpirandoEm7Dias ?? 0} expirando em 7 dias`"
                cor="warning"
            />

            <!-- Assinaturas vigentes -->
            <AppStatCard
                label="Assinaturas vigentes"
                :valor="store.carregandoKpis ? '...' : (store.kpis?.assinaturasVigentes ?? 0)"
                :legenda="store.carregandoKpis ? '' : `${store.kpis?.assinaturasGratuitas ?? 0} gratuita(s)`"
                cor="success"
            />
        </div>
    </section>
</template>

<style scoped>
.kpis-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 1rem;
}

.bloco-erro {
    color: hsl(var(--error));
    font-size: 0.875rem;
    padding: 0.75rem 1rem;
    border-radius: 8px;
    background: hsl(var(--error) / 0.08);
}
</style>
