<script setup lang="ts">
/**
 * RelatorioAgendaTab — tab operacional/agenda.
 * Usa dados de RelatorioOperacional do backend.
 */
import { computed } from 'vue'
import { AppCard, AppEmptyState } from '@/components/ui'
import RelatorioKpiCard from './RelatorioKpiCard.vue'
import RelatorioBarChart from './RelatorioBarChart.vue'
import type { RelatorioOperacional } from '@/services/relatorioService'

const props = defineProps<{
    dados: RelatorioOperacional | null
    carregando: boolean
    erro: string | null
}>()

const kpis = computed(() => props.dados?.kpis ?? [])
const breakdown = computed(() => props.dados?.breakdown ?? [])

function numFmt(n: number, unidade?: string) {
    const fmt = n.toLocaleString('pt-BR')
    return unidade ? `${fmt} ${unidade}` : fmt
}

const iconesPorRotulo: Record<string, string> = {
    'Agendamentos': 'fa-calendar-check',
    'Confirmados':  'fa-circle-check',
    'Cancelados':   'fa-circle-xmark',
    'No-show':      'fa-user-xmark',
    'Concluídos':   'fa-check',
    'Expirado':     'fa-clock-rotate-left',
}

const dadosBarras = computed(() =>
    breakdown.value.map(l => ({
        label: l.rotulo,
        valor: l.valor,
        sub: l.quantidade ? String(l.quantidade) : undefined,
    }))
)
</script>

<template>
    <div class="rp-body">
        <div v-if="carregando" class="rp-estado" aria-busy="true" aria-live="polite">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando relatório operacional...
        </div>

        <div v-else-if="erro" class="rp-erro" role="alert">{{ erro }}</div>

        <template v-else-if="!dados">
            <AppEmptyState
                icone="fa-solid fa-calendar-days"
                titulo="Nenhum dado de agenda"
                descricao="Selecione um período e aplique o filtro."
                compacto
            />
        </template>

        <template v-else>
            <!-- KPIs -->
            <div class="rp-grid" :style="{ gridTemplateColumns: `repeat(${Math.min(kpis.length, 4)}, 1fr)` }">
                <RelatorioKpiCard
                    v-for="kpi in kpis"
                    :key="kpi.rotulo"
                    :icone="iconesPorRotulo[kpi.rotulo] ?? 'fa-chart-simple'"
                    :label="kpi.rotulo"
                    :valor="numFmt(kpi.valor, kpi.unidade)"
                />
            </div>

            <!-- Gráfico de barras breakdown -->
            <AppCard v-if="dadosBarras.length > 0">
                <template #header>
                    <div class="ds-card-title rp-cabecalho">
                        <i class="fa-solid fa-chart-column" aria-hidden="true"></i>
                        Detalhamento por item
                    </div>
                </template>
                <RelatorioBarChart
                    :dados="dadosBarras"
                    :altura="240"
                />
            </AppCard>

            <!-- Tabela de breakdown -->
            <AppCard>
                <template #header>
                    <div class="ds-card-title rp-cabecalho">
                        <i class="fa-solid fa-table" aria-hidden="true"></i>
                        Detalhamento completo
                    </div>
                </template>
                <AppEmptyState
                    v-if="breakdown.length === 0"
                    icone="fa-solid fa-calendar-days"
                    titulo="Nenhum dado no período"
                    descricao="Ajuste os filtros e tente novamente."
                    compacto
                />
                <table v-else class="rp-tabela">
                    <thead>
                        <tr>
                            <th>Item</th>
                            <th class="r">Valor</th>
                            <th class="r">Qtd.</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="l in breakdown" :key="l.rotulo">
                            <td>{{ l.rotulo }}</td>
                            <td class="r">{{ l.valor.toLocaleString('pt-BR') }}</td>
                            <td class="r td-muted">{{ l.quantidade ?? '—' }}</td>
                        </tr>
                    </tbody>
                </table>
            </AppCard>
        </template>
    </div>
</template>

<style scoped>
.rp-body { display: flex; flex-direction: column; gap: 16px; }
.rp-estado {
    display: flex; align-items: center; gap: 8px;
    color: hsl(var(--muted-foreground)); font-size: 0.9em; padding: 2rem 0;
}
.rp-erro {
    padding: 12px 16px;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive)); font-size: 0.9em;
}
.rp-grid { display: grid; gap: 14px; }
@media (max-width: 900px) { .rp-grid { grid-template-columns: 1fr 1fr !important; } }
@media (max-width: 640px) { .rp-grid { grid-template-columns: 1fr !important; } }

.rp-cabecalho {
    display: flex; gap: 8px; align-items: center;
}
.rp-cabecalho i { color: hsl(var(--primary)); }

.rp-tabela { width: 100%; border-collapse: collapse; font-size: 13px; }
.rp-tabela th {
    text-align: left; padding: 10px 12px;
    border-bottom: 1px solid hsl(var(--border));
    font-size: 11px; font-weight: 600;
    text-transform: uppercase; letter-spacing: 0.04em;
    color: hsl(var(--muted-foreground));
}
.rp-tabela th.r, .rp-tabela td.r { text-align: right; }
.rp-tabela td {
    padding: 11px 12px;
    border-bottom: 1px solid hsl(var(--border) / 0.5);
    color: hsl(var(--foreground));
}
.rp-tabela tr:last-child td { border-bottom: 0; }
.rp-tabela tr:hover td { background: hsl(var(--primary) / 0.025); }
.td-muted { color: hsl(var(--muted-foreground)); }
</style>
