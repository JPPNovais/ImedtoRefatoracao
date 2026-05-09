<script setup lang="ts">
/**
 * RelatorioVisaoGeral — tab de visão executiva, consolida dados de todas as APIs.
 * Exibe faturamento, agendamentos e orçamentos lado a lado.
 */
import { computed } from 'vue'
import { AppCard, AppEmptyState } from '@/components/ui'
import RelatorioKpiCard from './RelatorioKpiCard.vue'
import RelatorioHBars from './RelatorioHBars.vue'
import type { RelatorioFinanceiro, RelatorioOperacional, RelatorioOrcamentos } from '@/services/relatorioService'

const props = defineProps<{
    financeiro: RelatorioFinanceiro | null
    operacional: RelatorioOperacional | null
    orcamentos: RelatorioOrcamentos | null
    carregando: boolean
}>()

function moeda(n: number) {
    return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

const temDados = computed(() =>
    props.financeiro != null || props.operacional != null || props.orcamentos != null
)

// Top itens do breakdown financeiro para o HBars
const topFinanceiro = computed(() =>
    (props.financeiro?.breakdown ?? []).slice(0, 5).map(l => ({
        label: l.rotulo,
        valor: l.valor,
    }))
)

// KPI atendimentos do operacional
const totalAtendimentos = computed(() =>
    props.operacional?.kpis.find(k => k.rotulo.toLowerCase().includes('atendimento'))?.valor
    ?? props.operacional?.breakdown.reduce((s, l) => s + (l.quantidade ?? 0), 0)
    ?? null
)
</script>

<template>
    <div class="rp-body">
        <div v-if="carregando" class="rp-estado" aria-busy="true" aria-live="polite">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando visão geral...
        </div>

        <template v-else-if="!temDados">
            <AppEmptyState
                icone="fa-solid fa-gauge-high"
                titulo="Nenhum dado no período"
                descricao="Selecione um período e aplique os filtros."
                compacto
            />
        </template>

        <template v-else>
            <!-- KPIs executivos — 4 cards top -->
            <div class="rp-grid rp-grid-4">
                <RelatorioKpiCard
                    icone="fa-coins"
                    label="Faturamento"
                    :valor="financeiro ? moeda(financeiro.totalReceitas) : '—'"
                    acento="ok"
                />
                <RelatorioKpiCard
                    icone="fa-scale-balanced"
                    label="Saldo líquido"
                    :valor="financeiro ? moeda(financeiro.saldo) : '—'"
                    :acento="financeiro ? (financeiro.saldo >= 0 ? 'ok' : 'bad') : 'default'"
                />
                <RelatorioKpiCard
                    icone="fa-calendar-check"
                    label="Atendimentos"
                    :valor="totalAtendimentos != null ? totalAtendimentos.toLocaleString('pt-BR') : '—'"
                />
                <RelatorioKpiCard
                    icone="fa-file-invoice-dollar"
                    label="Orçamentos aprovados"
                    :valor="orcamentos ? orcamentos.funil.totalAprovados.toLocaleString('pt-BR') : '—'"
                    :sub="orcamentos ? `de ${orcamentos.funil.totalCriados} criados` : undefined"
                    acento="ok"
                />
            </div>

            <!-- Cards de detalhe lado a lado -->
            <div class="rp-grid rp-grid-2">
                <!-- Receita x Despesa -->
                <AppCard v-if="financeiro">
                    <template #header>
                        <div class="rp-card-titulo">
                            <i class="fa-solid fa-chart-pie" aria-hidden="true"></i>
                            Receita vs. Despesa
                        </div>
                    </template>
                    <div class="rp-resumo-fin">
                        <div class="rp-fin-row">
                            <span class="rp-fin-label">Receitas</span>
                            <strong class="rp-fin-val rp-fin-val--receita">{{ moeda(financeiro.totalReceitas) }}</strong>
                        </div>
                        <div class="rp-fin-row">
                            <span class="rp-fin-label">Despesas</span>
                            <strong class="rp-fin-val rp-fin-val--despesa">{{ moeda(financeiro.totalDespesas) }}</strong>
                        </div>
                        <div class="rp-fin-separador"></div>
                        <div class="rp-fin-row">
                            <span class="rp-fin-label rp-fin-label--total">Saldo</span>
                            <strong
                                class="rp-fin-val rp-fin-val--saldo"
                                :class="financeiro.saldo >= 0 ? 'positivo' : 'negativo'"
                            >{{ moeda(financeiro.saldo) }}</strong>
                        </div>
                    </div>
                </AppCard>

                <!-- Top categorias financeiras -->
                <AppCard v-if="topFinanceiro.length > 0">
                    <template #header>
                        <div class="rp-card-titulo">
                            <i class="fa-solid fa-ranking-star" aria-hidden="true"></i>
                            Top categorias
                        </div>
                    </template>
                    <RelatorioHBars :itens="topFinanceiro" :formatarValor="moeda" />
                </AppCard>
            </div>

            <!-- Funil resumo de orçamentos -->
            <AppCard v-if="orcamentos && orcamentos.funil.totalCriados > 0">
                <template #header>
                    <div class="rp-card-titulo">
                        <i class="fa-solid fa-filter" aria-hidden="true"></i>
                        Resumo de orçamentos
                    </div>
                    <span class="rp-card-sub">
                        Taxa de conversão: {{ (orcamentos.funil.taxaConversao * 100).toFixed(1) }}%
                    </span>
                </template>
                <div class="rp-orc-grid">
                    <div v-for="(item, i) in [
                        { label: 'Criados',   valor: orcamentos.funil.totalCriados,   cor: 'hsl(var(--primary))' },
                        { label: 'Enviados',  valor: orcamentos.funil.totalEnviados,   cor: '#3aa6c6' },
                        { label: 'Aprovados', valor: orcamentos.funil.totalAprovados,  cor: 'hsl(var(--success))' },
                        { label: 'Recusados', valor: orcamentos.funil.totalRecusados,  cor: 'hsl(var(--destructive))' },
                    ]" :key="i" class="rp-orc-item">
                        <div class="rp-orc-num" :style="{ color: item.cor }">{{ item.valor }}</div>
                        <div class="rp-orc-label">{{ item.label }}</div>
                    </div>
                </div>
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

.rp-grid { display: grid; gap: 14px; }
.rp-grid-4 { grid-template-columns: repeat(4, 1fr); }
.rp-grid-2 { grid-template-columns: repeat(2, 1fr); }
@media (max-width: 1100px) { .rp-grid-4 { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 640px)  { .rp-grid-4, .rp-grid-2 { grid-template-columns: 1fr; } }

.rp-card-titulo {
    display: flex; gap: 8px; align-items: center;
    font-size: 14px; font-weight: 600; color: hsl(var(--foreground));
}
.rp-card-titulo i { color: hsl(var(--primary)); }
.rp-card-sub {
    font-size: 12.5px;
    color: hsl(var(--muted-foreground));
    font-weight: 500;
}

.rp-resumo-fin { display: flex; flex-direction: column; gap: 8px; }
.rp-fin-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 10px 12px;
    background: hsl(var(--muted) / 0.4);
    border-radius: 8px;
    font-size: 13px;
}
.rp-fin-label { color: hsl(var(--muted-foreground)); }
.rp-fin-label--total { color: hsl(var(--foreground)); font-weight: 600; }
.rp-fin-val { font-size: 14px; font-weight: 700; }
.rp-fin-val--receita { color: hsl(var(--success)); }
.rp-fin-val--despesa { color: hsl(var(--destructive)); }
.rp-fin-val--saldo.positivo { color: hsl(var(--success)); }
.rp-fin-val--saldo.negativo { color: hsl(var(--destructive)); }
.rp-fin-separador { height: 1px; background: hsl(var(--border)); margin: 4px 0; }

.rp-orc-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 12px;
    text-align: center;
}
@media (max-width: 640px) { .rp-orc-grid { grid-template-columns: repeat(2, 1fr); } }
.rp-orc-item { padding: 16px 12px; background: hsl(var(--muted) / 0.4); border-radius: 10px; }
.rp-orc-num { font-size: 28px; font-weight: 800; line-height: 1; font-variant-numeric: tabular-nums; }
.rp-orc-label { font-size: 12px; color: hsl(var(--muted-foreground)); margin-top: 4px; font-weight: 500; }
</style>
