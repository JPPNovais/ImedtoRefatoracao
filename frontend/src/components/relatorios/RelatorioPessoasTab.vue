<script setup lang="ts">
import { AppCard, AppEmptyState } from '@/components/ui'
import RelatorioKpiCard from './RelatorioKpiCard.vue'
import type { RelatorioPessoas } from '@/services/relatorioService'

const props = defineProps<{
    dados: RelatorioPessoas | null
    carregando: boolean
    erro: string | null
}>()

function moeda(n: number) {
    return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}
</script>

<template>
    <div class="rp-body">
        <div v-if="carregando" class="rp-estado" aria-busy="true" aria-live="polite">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando relatório de pessoas...
        </div>

        <div v-else-if="erro" class="rp-erro" role="alert">{{ erro }}</div>

        <template v-else-if="!dados">
            <AppEmptyState
                icone="fa-solid fa-users"
                titulo="Nenhum dado de pessoas"
                descricao="Selecione um período e aplique o filtro."
                compacto
            />
        </template>

        <template v-else>
            <!-- Top Pacientes -->
            <AppCard v-if="dados.tipo === 'pacientes'">
                <template #header>
                    <div class="rp-card-titulo">
                        <i class="fa-solid fa-user-group" aria-hidden="true"></i>
                        Top 10 pacientes
                    </div>
                </template>
                <AppEmptyState
                    v-if="!dados.topPacientes?.length"
                    icone="fa-solid fa-user-group"
                    titulo="Nenhum dado no período"
                    descricao="Ajuste os filtros de data."
                    compacto
                />
                <table v-else class="rp-tabela">
                    <thead>
                        <tr>
                            <th style="width:40px">#</th>
                            <th>Paciente</th>
                            <th class="r">Consultas</th>
                            <th class="r">Total gasto</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="(p, idx) in dados.topPacientes" :key="p.nome">
                            <td class="td-rank">{{ idx + 1 }}</td>
                            <td class="td-nome">{{ p.nome }}</td>
                            <td class="r">{{ p.totalConsultas }}</td>
                            <td class="r">{{ moeda(p.totalGasto) }}</td>
                        </tr>
                    </tbody>
                </table>
            </AppCard>

            <!-- Ranking Profissionais -->
            <AppCard v-else-if="dados.tipo === 'profissionais'">
                <template #header>
                    <div class="rp-card-titulo">
                        <i class="fa-solid fa-user-doctor" aria-hidden="true"></i>
                        Performance dos profissionais
                    </div>
                </template>
                <AppEmptyState
                    v-if="!dados.rankingProfissionais?.length"
                    icone="fa-solid fa-user-doctor"
                    titulo="Nenhum dado no período"
                    descricao="Ajuste os filtros de data."
                    compacto
                />
                <div v-else>
                    <!-- KPIs rápidos -->
                    <div class="rp-grid rp-grid-3" style="margin-bottom: 16px;">
                        <RelatorioKpiCard
                            icone="fa-user-doctor"
                            label="Profissionais ativos"
                            :valor="String(dados.rankingProfissionais.length)"
                        />
                        <RelatorioKpiCard
                            icone="fa-calendar-check"
                            label="Total atendimentos"
                            :valor="dados.rankingProfissionais.reduce((s,p) => s + p.totalAtendimentos, 0).toLocaleString('pt-BR')"
                        />
                        <RelatorioKpiCard
                            icone="fa-coins"
                            label="Faturamento total"
                            :valor="moeda(dados.rankingProfissionais.reduce((s,p) => s + p.faturamento, 0))"
                        />
                    </div>

                    <table class="rp-tabela">
                        <thead>
                            <tr>
                                <th style="width:40px">#</th>
                                <th>Profissional</th>
                                <th class="r">Atendimentos</th>
                                <th class="r">Faturamento</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="(p, idx) in dados.rankingProfissionais" :key="p.nome">
                                <td class="td-rank">{{ idx + 1 }}</td>
                                <td class="td-nome">{{ p.nome }}</td>
                                <td class="r">{{ p.totalAtendimentos }}</td>
                                <td class="r"><strong>{{ moeda(p.faturamento) }}</strong></td>
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
.rp-grid-3 { grid-template-columns: repeat(3, 1fr); }
@media (max-width: 900px) { .rp-grid-3 { grid-template-columns: 1fr 1fr; } }
@media (max-width: 640px) { .rp-grid-3 { grid-template-columns: 1fr; } }

.rp-card-titulo {
    display: flex; gap: 8px; align-items: center;
    font-size: 14px; font-weight: 600; color: hsl(var(--foreground));
}
.rp-card-titulo i { color: hsl(var(--primary)); }

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
.td-rank { font-weight: 800; color: hsl(var(--primary)); }
.td-nome { font-weight: 500; }
</style>
