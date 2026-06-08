<script setup lang="ts">
import { computed } from 'vue'
import { AppCard, AppEmptyState } from '@/components/ui'
import RelatorioKpiCard from './RelatorioKpiCard.vue'
import type { RelatorioOrcamentos } from '@/services/relatorioService'

const props = defineProps<{
    dados: RelatorioOrcamentos | null
    carregando: boolean
    erro: string | null
}>()

function moeda(n: number) {
    return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function pct(n: number) {
    return `${(n * 100).toFixed(1)}%`
}

const etapasFunil = computed(() => {
    const f = props.dados?.funil
    if (!f) return []
    return [
        { label: 'Criados',   valor: f.totalCriados,   cor: 'hsl(var(--primary))' },
        { label: 'Enviados',  valor: f.totalEnviados,   cor: '#3aa6c6' },
        { label: 'Aprovados', valor: f.totalAprovados,  cor: 'hsl(var(--success))' },
        { label: 'Recusados', valor: f.totalRecusados,  cor: 'hsl(var(--destructive))' },
    ]
})

const maxFunil = computed(() =>
    etapasFunil.value.reduce((m, e) => Math.max(m, e.valor), 1)
)
</script>

<template>
    <div class="rp-body">
        <div v-if="carregando" class="rp-estado" aria-busy="true" aria-live="polite">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando relatório de orçamentos...
        </div>

        <div v-else-if="erro" class="rp-erro" role="alert">{{ erro }}</div>

        <template v-else-if="!dados">
            <AppEmptyState
                icone="fa-solid fa-file-invoice-dollar"
                titulo="Nenhum dado de orçamentos"
                descricao="Selecione um período e aplique o filtro."
                compacto
            />
        </template>

        <template v-else>
            <!-- KPIs -->
            <div class="rp-grid rp-grid-4">
                <RelatorioKpiCard
                    icone="fa-file-invoice-dollar"
                    label="Total criados"
                    :valor="dados.funil.totalCriados.toLocaleString('pt-BR')"
                />
                <RelatorioKpiCard
                    icone="fa-circle-check"
                    label="Taxa de conversão"
                    :valor="pct(dados.funil.taxaConversao)"
                    acento="ok"
                />
                <RelatorioKpiCard
                    icone="fa-receipt"
                    label="Valor médio aprovado"
                    :valor="moeda(dados.funil.valorMedioAprovado)"
                />
                <RelatorioKpiCard
                    icone="fa-thumbs-up"
                    label="Aprovados"
                    :valor="dados.funil.totalAprovados.toLocaleString('pt-BR')"
                    acento="ok"
                />
            </div>

            <!-- Funil de conversão -->
            <AppCard>
                <template #header>
                    <div class="ds-card-title rp-cabecalho">
                        <i class="fa-solid fa-filter" aria-hidden="true"></i>
                        Funil de conversão
                    </div>
                </template>
                <AppEmptyState
                    v-if="dados.funil.totalCriados === 0"
                    icone="fa-solid fa-filter"
                    titulo="Nenhum orçamento no período"
                    descricao="Ajuste os filtros de data."
                    compacto
                />
                <div v-else class="rp-funil">
                    <div
                        v-for="(etapa, i) in etapasFunil"
                        :key="etapa.label"
                        class="rp-funil-etapa"
                    >
                        <div class="rp-funil-linha">
                            <span class="rp-funil-label">{{ etapa.label }}</span>
                            <div class="rp-funil-barra-wrap">
                                <div
                                    class="rp-funil-barra"
                                    :style="{
                                        width: `${Math.max(4, Math.round((etapa.valor / maxFunil) * 100))}%`,
                                        background: etapa.cor,
                                    }"
                                    :aria-label="`${etapa.label}: ${etapa.valor}`"
                                >
                                    <span>{{ etapa.valor }}</span>
                                </div>
                            </div>
                            <span class="rp-funil-pct">
                                {{ dados.funil.totalCriados > 0 ? Math.round((etapa.valor / dados.funil.totalCriados) * 100) : 0 }}%
                            </span>
                        </div>
                        <div v-if="i < etapasFunil.length - 1" class="rp-funil-seta">
                            <i class="fa-solid fa-arrow-down" aria-hidden="true"></i>
                        </div>
                    </div>
                </div>
            </AppCard>

            <!-- Breakdown adicional -->
            <AppCard v-if="dados.breakdown.length > 0">
                <template #header>
                    <div class="ds-card-title rp-cabecalho">
                        <i class="fa-solid fa-table" aria-hidden="true"></i>
                        Detalhamento
                    </div>
                </template>
                <table class="rp-tabela">
                    <thead>
                        <tr>
                            <th>Item</th>
                            <th class="r">Valor</th>
                            <th class="r">Qtd.</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="l in dados.breakdown" :key="l.rotulo">
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
.rp-grid-4 { grid-template-columns: repeat(4, 1fr); }
@media (max-width: 1000px) { .rp-grid-4 { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 640px)  { .rp-grid-4 { grid-template-columns: 1fr; } }

.rp-cabecalho {
    display: flex; gap: 8px; align-items: center;
}
.rp-cabecalho i { color: hsl(var(--primary)); }

.rp-funil { display: flex; flex-direction: column; gap: 0; }
.rp-funil-etapa { display: flex; flex-direction: column; }
.rp-funil-linha {
    display: grid;
    grid-template-columns: 110px 1fr 50px;
    gap: 14px;
    align-items: center;
    padding: 4px 0;
}
.rp-funil-label {
    font-size: 13px;
    font-weight: 600;
    color: hsl(var(--foreground));
}
.rp-funil-barra-wrap {
    background: hsl(var(--muted));
    border-radius: 8px;
    padding: 4px;
}
.rp-funil-barra {
    height: 34px;
    border-radius: 6px;
    display: flex;
    align-items: center;
    padding: 0 14px;
    color: #fff;
    font-weight: 600;
    font-size: 13px;
    min-width: 60px;
    transition: width 0.4s ease;
}
.rp-funil-pct {
    font-size: 13px;
    font-weight: 700;
    color: hsl(var(--primary));
    text-align: right;
}
.rp-funil-seta {
    display: flex;
    align-items: center;
    margin: 2px 0 2px 110px;
    font-size: 10px;
    color: hsl(var(--muted-foreground));
    gap: 6px;
}
.rp-funil-seta::after {
    content: 'converte para próxima etapa';
    font-size: 11px;
}

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
