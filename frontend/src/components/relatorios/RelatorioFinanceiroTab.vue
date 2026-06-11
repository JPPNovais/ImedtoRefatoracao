<script setup lang="ts">
import { computed } from 'vue'
import { AppCard, AppEmptyState, AppButton } from '@/components/ui'
import RelatorioKpiCard from './RelatorioKpiCard.vue'
import RelatorioLineChart from './RelatorioLineChart.vue'
import RelatorioDonutChart from './RelatorioDonutChart.vue'
import type { RelatorioFinanceiro } from '@/services/relatorioService'
import { useRelatorioCsv } from '@/composables/useRelatorioCsv'

const props = defineProps<{
    dados: RelatorioFinanceiro | null
    carregando: boolean
    erro: string | null
    comparar?: boolean
    periodo?: { dataInicio: string; dataFim: string }
}>()

const emit = defineEmits<{
    /** F7/CA181 — emitido quando usuário clica para ver detalhe de um paciente. */
    (e: 'drill-down-paciente', pacienteId: number): void
}>()

const { exportarPorPaciente } = useRelatorioCsv()

function exportarCsvPorPaciente() {
    if (props.dados?.porPaciente && props.periodo) {
        exportarPorPaciente(props.dados.porPaciente, props.periodo)
    }
}

function moeda(n: number) {
    return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function pct(valor: number, total: number) {
    if (!total) return 0
    return Math.round((valor / total) * 100)
}

const totalBreakdown = computed(() =>
    props.dados?.breakdown.reduce((s, l) => s + l.valor, 0) ?? 0
)

// Para o donut, mapear o breakdown para fatias com cores
const cores = [
    'hsl(var(--primary))',
    '#3aa6c6',
    '#5cc6e0',
    '#10b981',
    '#f59e0b',
    '#94a3b8',
]
const fatiasDonut = computed(() =>
    (props.dados?.breakdown ?? []).slice(0, 6).map((l, i) => ({
        label: l.rotulo,
        valor: l.valor,
        cor: cores[i % cores.length],
    }))
)
</script>

<template>
    <div class="rp-body">
        <!-- Loading -->
        <div v-if="carregando" class="rp-estado" aria-busy="true" aria-live="polite">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando relatório financeiro...
        </div>

        <!-- Erro -->
        <div v-else-if="erro" class="rp-erro" role="alert">
            {{ erro }}
        </div>

        <!-- Sem dados -->
        <template v-else-if="!dados">
            <AppEmptyState
                icone="fa-solid fa-chart-bar"
                titulo="Nenhum dado financeiro"
                descricao="Selecione um período e aplique o filtro."
                compacto
            />
        </template>

        <template v-else>
            <!-- KPIs -->
            <div class="rp-grid rp-grid-3">
                <RelatorioKpiCard
                    icone="fa-coins"
                    label="Total de receitas"
                    :valor="moeda(dados.totalReceitas)"
                    acento="ok"
                />
                <RelatorioKpiCard
                    icone="fa-arrow-trend-down"
                    label="Total de despesas"
                    :valor="moeda(dados.totalDespesas)"
                    acento="warn"
                />
                <RelatorioKpiCard
                    icone="fa-scale-balanced"
                    label="Saldo do período"
                    :valor="moeda(dados.saldo)"
                    :acento="dados.saldo >= 0 ? 'ok' : 'bad'"
                />
            </div>

            <!-- Breakdown visual + donut -->
            <div class="rp-grid rp-grid-2">
                <!-- Barras horizontais por categoria -->
                <AppCard>
                    <template #header>
                        <div class="ds-card-title rp-cabecalho">
                            <i class="fa-solid fa-chart-bar" aria-hidden="true"></i>
                            Detalhamento por {{ dados.agrupadoPor === 'forma_pagamento' ? 'forma de pagamento' : dados.agrupadoPor }}
                        </div>
                    </template>

                    <AppEmptyState
                        v-if="dados.breakdown.length === 0"
                        icone="fa-solid fa-chart-bar"
                        titulo="Nenhum dado no período"
                        descricao="Ajuste os filtros e tente novamente."
                        compacto
                    />
                    <div v-else class="rp-hbars">
                        <div v-for="linha in dados.breakdown" :key="linha.rotulo" class="rp-hbar">
                            <span class="rp-hbar-label" :title="linha.rotulo">{{ linha.rotulo }}</span>
                            <div class="rp-hbar-track" aria-hidden="true">
                                <div
                                    class="rp-hbar-fill"
                                    :style="{ width: pct(linha.valor, totalBreakdown) + '%' }"
                                ></div>
                            </div>
                            <span class="rp-hbar-valor">{{ moeda(linha.valor) }}</span>
                        </div>
                    </div>
                </AppCard>

                <!-- Donut de distribuição -->
                <AppCard v-if="fatiasDonut.length > 0">
                    <template #header>
                        <div class="ds-card-title rp-cabecalho">
                            <i class="fa-solid fa-chart-pie" aria-hidden="true"></i>
                            Distribuição por categoria
                        </div>
                        <span class="rp-card-sub">{{ moeda(totalBreakdown) }}</span>
                    </template>
                    <RelatorioDonutChart
                        :fatias="fatiasDonut"
                        :tamanho="150"
                        :espessura="22"
                        :valorCentral="moeda(totalBreakdown)"
                        legendaCentral="total"
                        :formatarValor="moeda"
                    />
                </AppCard>
            </div>

            <!-- Tabela detalhada -->
            <AppCard v-if="dados.breakdown.length > 0">
                <template #header>
                    <div class="ds-card-title rp-cabecalho">
                        <i class="fa-solid fa-table" aria-hidden="true"></i>
                        Tabela detalhada
                    </div>
                </template>
                <div class="rp-tabela-wrapper">
                    <table class="rp-tabela">
                        <thead>
                            <tr>
                                <th>Item</th>
                                <th class="r">Valor</th>
                                <th class="r">Qtd.</th>
                                <th class="r">% do total</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="linha in dados.breakdown" :key="linha.rotulo">
                                <td class="td-nome">{{ linha.rotulo }}</td>
                                <td class="r">{{ moeda(linha.valor) }}</td>
                                <td class="r td-muted">{{ linha.quantidade ?? '—' }}</td>
                                <td class="r">{{ pct(linha.valor, totalBreakdown) }}%</td>
                            </tr>
                        </tbody>
                        <tfoot>
                            <tr class="tfoot-total">
                                <td>Total</td>
                                <td class="r">{{ moeda(totalBreakdown) }}</td>
                                <td colspan="2"></td>
                            </tr>
                        </tfoot>
                    </table>
                </div>
            </AppCard>

            <!-- F7/CA178-CA182 — Custo/lucro por paciente (aditivo — presente quando porPaciente fornecido) -->
            <AppCard v-if="dados.porPaciente && dados.porPaciente.length > 0">
                <template #header>
                    <div class="rp-cabecalho-por-paciente">
                        <div class="ds-card-title rp-cabecalho">
                            <i class="fa-solid fa-user-injured" aria-hidden="true"></i>
                            Custo / lucro por paciente
                        </div>
                        <!-- CA182 — export CSV reusado -->
                        <AppButton
                            v-if="periodo"
                            size="sm"
                            variant="secondary"
                            icon="fa-solid fa-file-csv"
                            @click="exportarCsvPorPaciente"
                        >
                            Exportar
                        </AppButton>
                    </div>
                </template>
                <div class="rp-tabela-wrapper">
                    <table class="rp-tabela">
                        <thead>
                            <tr>
                                <th>Paciente</th>
                                <th class="r">Cobrado</th>
                                <th class="r">Pago</th>
                                <th class="r">Desconto</th>
                                <th class="r">Taxa</th>
                                <th class="r">Custo</th>
                                <th class="r">Lucro</th>
                            </tr>
                        </thead>
                        <tbody>
                            <!-- CA181: drill-down emite evento para o pai auditar -->
                            <tr
                                v-for="p in dados.porPaciente"
                                :key="p.pacienteId"
                                class="rp-por-paciente-row"
                                @click="emit('drill-down-paciente', p.pacienteId)"
                                :title="`Ver detalhe de ${p.pacienteNome}`"
                            >
                                <td class="td-nome">{{ p.pacienteNome }}</td>
                                <td class="r">{{ moeda(p.cobrado) }}</td>
                                <td class="r">{{ moeda(p.pago) }}</td>
                                <td class="r td-muted">{{ moeda(p.desconto) }}</td>
                                <td class="r td-muted">{{ moeda(p.taxa) }}</td>
                                <td class="r td-muted">{{ moeda(p.custo) }}</td>
                                <td class="r" :class="p.lucro >= 0 ? 'lucro-pos' : 'lucro-neg'">
                                    {{ moeda(p.lucro) }}
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </AppCard>
        </template>
    </div>
</template>

<style scoped>
.rp-body { display: flex; flex-direction: column; gap: 16px; }

.rp-estado {
    display: flex;
    align-items: center;
    gap: 8px;
    color: hsl(var(--muted-foreground));
    font-size: 0.9em;
    padding: 2rem 0;
}
.rp-erro {
    padding: 12px 16px;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    font-size: 0.9em;
}

.rp-grid { display: grid; gap: 14px; }
.rp-grid-3 { grid-template-columns: repeat(3, 1fr); }
.rp-grid-2 { grid-template-columns: repeat(2, 1fr); }
@media (max-width: 900px) { .rp-grid-3 { grid-template-columns: 1fr 1fr; } }
@media (max-width: 640px) { .rp-grid-3, .rp-grid-2 { grid-template-columns: 1fr; } }

.rp-cabecalho {
    display: flex;
    gap: 8px;
    align-items: center;
}
.rp-cabecalho i { color: hsl(var(--primary)); }
.rp-card-sub {
    font-size: 12.5px;
    color: hsl(var(--muted-foreground));
    font-weight: 500;
}

.rp-hbars { display: flex; flex-direction: column; gap: 10px; }
.rp-hbar { display: flex; align-items: center; gap: 10px; font-size: 0.85em; }
.rp-hbar-label {
    width: 130px;
    flex-shrink: 0;
    color: hsl(var(--foreground));
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-size: 12.5px;
}
.rp-hbar-track {
    flex: 1;
    height: 16px;
    background: hsl(var(--muted));
    border-radius: 6px;
    overflow: hidden;
}
.rp-hbar-fill {
    height: 100%;
    background: hsl(var(--primary));
    border-radius: 6px;
    transition: width 0.4s ease;
    min-width: 2px;
}
.rp-hbar-valor {
    width: 110px;
    text-align: right;
    font-weight: 600;
    font-size: 12.5px;
    color: hsl(var(--foreground));
}

.rp-tabela-wrapper { overflow-x: auto; }
.rp-tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;
}
.rp-tabela th {
    text-align: left;
    padding: 10px 12px;
    border-bottom: 1px solid hsl(var(--border));
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;
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
.td-nome { font-weight: 500; }
.td-muted { color: hsl(var(--muted-foreground)); }

.tfoot-total td {
    font-weight: 700;
    border-top: 2px solid hsl(var(--border));
    border-bottom: none;
    padding-top: 12px;
}

/* F7/CA178 — Por paciente */
.rp-cabecalho-por-paciente {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    flex-wrap: wrap;
}
.rp-por-paciente-row {
    cursor: pointer;
}
.rp-por-paciente-row:hover td { background: hsl(var(--primary) / 0.04); }
.lucro-pos { color: hsl(var(--success)); font-weight: 600; }
.lucro-neg { color: hsl(var(--destructive)); font-weight: 600; }
</style>
