<script setup lang="ts">
import { ref, watch } from "vue"
import {
    relatorioService,
    type RelatorioFinanceiro,
    type AgruparPorFinanceiro,
} from "@/services/relatorioService"
import {
    AppPageHeader, AppButton, AppCard, AppField, AppEmptyState,
} from "@/components/ui"

// ─── Filtros ──────────────────────────────────────────────────────────────────

const dataInicio = ref("")
const dataFim    = ref("")
const agruparPor = ref<AgruparPorFinanceiro>("categoria")

const opcoes: { valor: AgruparPorFinanceiro; label: string }[] = [
    { valor: "categoria",      label: "Categoria" },
    { valor: "forma_pagamento", label: "Forma de pagamento" },
    { valor: "dia",            label: "Dia" },
    { valor: "semana",         label: "Semana" },
    { valor: "mes",            label: "Mes" },
]

// ─── Dados ────────────────────────────────────────────────────────────────────

const dados      = ref<RelatorioFinanceiro | null>(null)
const carregando = ref(false)
const erro       = ref<string | null>(null)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        dados.value = await relatorioService.financeiro({
            dataInicio: dataInicio.value || undefined,
            dataFim:    dataFim.value    || undefined,
            agruparPor: agruparPor.value,
        })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar relatorio financeiro."
    } finally {
        carregando.value = false
    }
}

// Carrega ao montar
carregar()

// ─── Helpers ──────────────────────────────────────────────────────────────────

function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function pct(valor: number, total: number) {
    if (!total) return 0
    return Math.round((valor / total) * 100)
}

const totalBreakdown = () => dados.value?.breakdown.reduce((s, l) => s + l.valor, 0) ?? 0
</script>

<template>
    <main class="app-page app-page--wide">
        <AppPageHeader
            titulo="Relatorio Financeiro"
            subtitulo="Fluxo de caixa e faturamento por periodo."
        />

        <!-- Filtros -->
        <AppCard padding="sm">
            <div class="filtros-linha">
                <AppField label="De" for="rf-inicio">
                    <input id="rf-inicio" v-model="dataInicio" type="date" class="input-data" />
                </AppField>
                <AppField label="Ate" for="rf-fim">
                    <input id="rf-fim" v-model="dataFim" type="date" class="input-data" />
                </AppField>
                <AppField label="Agrupar por" for="rf-agrupar">
                    <select id="rf-agrupar" v-model="agruparPor" class="input-data">
                        <option v-for="o in opcoes" :key="o.valor" :value="o.valor">{{ o.label }}</option>
                    </select>
                </AppField>
                <div class="filtro-acao">
                    <AppButton icon="fa-solid fa-magnifying-glass" :loading="carregando" @click="carregar">
                        Aplicar
                    </AppButton>
                </div>
            </div>
        </AppCard>

        <!-- Loading -->
        <div v-if="carregando" class="estado-msg">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando...
        </div>

        <!-- Erro -->
        <div v-else-if="erro" class="erro-banner" role="alert">
            {{ erro }}
            <AppButton variant="ghost" size="sm" @click="carregar">Tentar novamente</AppButton>
        </div>

        <template v-else-if="dados">
            <!-- KPI cards -->
            <div class="kpi-grid">
                <AppCard elevated>
                    <div class="kpi">
                        <span class="kpi-label">Total de receitas</span>
                        <span class="kpi-valor kpi-valor--success">{{ moeda(dados.totalReceitas) }}</span>
                    </div>
                </AppCard>
                <AppCard elevated>
                    <div class="kpi">
                        <span class="kpi-label">Total de despesas</span>
                        <span class="kpi-valor kpi-valor--error">{{ moeda(dados.totalDespesas) }}</span>
                    </div>
                </AppCard>
                <AppCard elevated>
                    <div class="kpi">
                        <span class="kpi-label">Saldo do periodo</span>
                        <span class="kpi-valor" :class="dados.saldo >= 0 ? 'kpi-valor--success' : 'kpi-valor--error'">
                            {{ moeda(dados.saldo) }}
                        </span>
                    </div>
                </AppCard>
            </div>

            <!-- Breakdown -->
            <AppCard :title="`Detalhamento por ${opcoes.find(o => o.valor === dados!.agrupadoPor)?.label ?? dados.agrupadoPor}`">
                <AppEmptyState
                    v-if="dados.breakdown.length === 0"
                    icone="fa-solid fa-chart-bar"
                    titulo="Nenhum dado no periodo"
                    descricao="Ajuste os filtros de data e tente novamente."
                    compacto
                />
                <template v-else>
                    <!-- Barras visuais simples -->
                    <div class="barras">
                        <div v-for="linha in dados.breakdown" :key="linha.rotulo" class="barra-row">
                            <span class="barra-label" :title="linha.rotulo">{{ linha.rotulo }}</span>
                            <div class="barra-track" aria-hidden="true">
                                <div
                                    class="barra-fill"
                                    :style="{ width: pct(linha.valor, totalBreakdown()) + '%' }"
                                ></div>
                            </div>
                            <span class="barra-valor">{{ moeda(linha.valor) }}</span>
                            <span v-if="linha.quantidade" class="barra-qtd">{{ linha.quantidade }} lanc.</span>
                        </div>
                    </div>

                    <!-- Tabela detalhada -->
                    <div class="tabela-wrapper">
                        <table class="tabela">
                            <thead>
                                <tr>
                                    <th>{{ opcoes.find(o => o.valor === dados!.agrupadoPor)?.label ?? "Agrupamento" }}</th>
                                    <th>Valor</th>
                                    <th>Qtd.</th>
                                    <th>%</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="linha in dados.breakdown" :key="linha.rotulo">
                                    <td class="td-rotulo">{{ linha.rotulo }}</td>
                                    <td>{{ moeda(linha.valor) }}</td>
                                    <td class="td-num">{{ linha.quantidade ?? "—" }}</td>
                                    <td class="td-pct">{{ pct(linha.valor, totalBreakdown()) }}%</td>
                                </tr>
                            </tbody>
                            <tfoot>
                                <tr class="tfoot-total">
                                    <td>Total</td>
                                    <td>{{ moeda(totalBreakdown()) }}</td>
                                    <td colspan="2"></td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                </template>
            </AppCard>
        </template>
    </main>
</template>

<style scoped>
.filtros-linha {
    display: flex;
    flex-wrap: wrap;
    gap: 1rem;
    align-items: flex-end;
}
.filtro-acao { align-self: flex-end; }

.input-data {
    padding: 0.45rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    font-family: inherit;
    font-size: 0.875em;
    background: hsl(var(--card));
    color: hsl(var(--foreground));
    min-width: 140px;
}
.input-data:focus { outline: none; border-color: hsl(var(--primary)); }

.estado-msg {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-muted);
    font-size: 0.9em;
    padding: 2rem 0;
}

.erro-banner {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    font-size: 0.9em;
}

.kpi-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 1rem;
}

.kpi {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    padding: 0.25rem 0;
}
.kpi-label { font-size: 0.8em; font-weight: 600; color: hsl(var(--muted-foreground)); text-transform: uppercase; letter-spacing: 0.04em; }
.kpi-valor { font-size: 1.5rem; font-weight: 800; }
.kpi-valor--success { color: hsl(var(--success)); }
.kpi-valor--error   { color: hsl(var(--destructive)); }

.barras { display: flex; flex-direction: column; gap: 0.6rem; margin-bottom: 1.5rem; }
.barra-row { display: flex; align-items: center; gap: 0.75rem; font-size: 0.85em; }
.barra-label { width: 140px; flex-shrink: 0; color: hsl(var(--foreground)); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.barra-track { flex: 1; height: 14px; background: hsl(var(--muted)); border-radius: 9999px; overflow: hidden; }
.barra-fill  { height: 100%; background: hsl(var(--primary)); border-radius: 9999px; transition: width 0.4s ease; min-width: 2px; }
.barra-valor { width: 110px; text-align: right; font-weight: 600; color: hsl(var(--foreground)); }
.barra-qtd   { width: 70px; text-align: right; color: hsl(var(--muted-foreground)); font-size: 0.8em; }

.tabela-wrapper { overflow-x: auto; }

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.88em;
}
.tabela th {
    text-align: left;
    padding: 0.5rem 0.75rem;
    border-bottom: 2px solid hsl(var(--border));
    font-weight: 600;
    font-size: 0.8em;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: hsl(var(--muted-foreground));
}
.tabela td {
    padding: 0.5rem 0.75rem;
    border-bottom: 1px solid hsl(var(--border));
}
.tabela tr:hover td { background: hsl(var(--muted) / 0.4); }

.td-rotulo { font-weight: 500; }
.td-num    { text-align: right; }
.td-pct    { text-align: right; color: hsl(var(--muted-foreground)); }

.tfoot-total td {
    font-weight: 700;
    border-top: 2px solid hsl(var(--border));
    border-bottom: none;
}
</style>
