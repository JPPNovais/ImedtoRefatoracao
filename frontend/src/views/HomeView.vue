<script setup lang="ts">
import { onMounted, ref } from "vue"
import { useAuthStore } from "@/stores/authStore"
import { dashboardService, type DashboardData } from "@/services/dashboardService"

const auth = useAuthStore()

const dashboard = ref<DashboardData | null>(null)
const carregando = ref(false)

onMounted(async () => {
    carregando.value = true
    try {
        dashboard.value = await dashboardService.obter()
    } catch {
        // dashboard não-crítico — falha silenciosa
    } finally {
        carregando.value = false
    }
})

function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function formatarHora(s: string) {
    return new Date(s).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" })
}

const statusCor: Record<string, string> = {
    Agendado: "#3b82f6",
    Confirmado: "#10b981",
    Cancelado: "#ef4444",
    Concluido: "#6b7280",
}
</script>

<template>
    <main class="app-page home">
        <div class="page-header">
            <h1>Olá, {{ auth.usuario?.nomeCompleto ?? auth.usuario?.email }}</h1>
            <p class="subtitulo">Bem-vindo ao painel.</p>
        </div>

        <!-- KPIs -->
        <section v-if="dashboard" class="kpis">
            <div class="kpi">
                <span class="kpi-valor">{{ dashboard.totalPacientesAtivos }}</span>
                <span class="kpi-label">Pacientes ativos</span>
            </div>
            <div class="kpi">
                <span class="kpi-valor azul">{{ dashboard.agendamentosHoje }}</span>
                <span class="kpi-label">Agendamentos hoje</span>
            </div>
            <div class="kpi">
                <span class="kpi-valor azul">{{ dashboard.agendamentosSemana }}</span>
                <span class="kpi-label">Próximos 7 dias</span>
            </div>
            <div class="kpi" :class="dashboard.saldoMes >= 0 ? 'kpi-positivo' : 'kpi-negativo'">
                <span class="kpi-valor">{{ moeda(dashboard.saldoMes) }}</span>
                <span class="kpi-label">Saldo do mês</span>
            </div>
            <div v-if="dashboard.itensAbaixoMinimo > 0" class="kpi kpi-alerta">
                <span class="kpi-valor laranja">{{ dashboard.itensAbaixoMinimo }}</span>
                <span class="kpi-label">⚠ Itens abaixo do mínimo</span>
            </div>
            <div v-if="dashboard.lancamentosVencidos > 0" class="kpi kpi-alerta">
                <span class="kpi-valor vermelho">{{ dashboard.lancamentosVencidos }}</span>
                <span class="kpi-label">⚠ Lançamentos vencidos</span>
            </div>
            <div v-if="dashboard.orcamentosPendentes > 0" class="kpi">
                <span class="kpi-valor amarelo">{{ dashboard.orcamentosPendentes }}</span>
                <span class="kpi-label">Orçamentos pendentes</span>
            </div>
        </section>
        <p v-else-if="carregando" class="info">Carregando dashboard...</p>

        <!-- Próximos agendamentos + Alertas -->
        <div v-if="dashboard" class="paineis">
            <section class="painel" v-if="dashboard.proximosAgendamentos.length > 0">
                <h3>Próximos agendamentos</h3>
                <ul>
                    <li v-for="ag in dashboard.proximosAgendamentos" :key="ag.id" class="item-lista">
                        <span class="status-dot" :style="{ background: statusCor[ag.status] ?? '#ccc' }"></span>
                        <div>
                            <strong>{{ ag.pacienteNome }}</strong>
                            <small>{{ ag.tipoServico }} · {{ ag.profissionalNome }}</small>
                        </div>
                        <span class="hora">{{ formatarHora(ag.inicioPrevisto) }}</span>
                    </li>
                </ul>
                <router-link :to="{ name: 'Agenda' }" class="ver-mais">Ver agenda completa →</router-link>
            </section>

            <section class="painel" v-if="dashboard.itensAbaixoMinimoLista.length > 0">
                <h3>⚠ Estoque abaixo do mínimo</h3>
                <ul>
                    <li v-for="item in dashboard.itensAbaixoMinimoLista" :key="item.id" class="item-lista item-alerta">
                        <div>
                            <strong>{{ item.nome }}</strong>
                            <small>{{ item.quantidadeAtual }} / {{ item.quantidadeMinima }} {{ item.unidadeMedida }}</small>
                        </div>
                    </li>
                </ul>
                <router-link :to="{ name: 'Inventario' }" class="ver-mais">Ver inventário →</router-link>
            </section>
        </div>

        <!-- Navegação rápida -->
        <nav class="menu">
            <router-link :to="{ name: 'Agenda' }" class="card">
                <h3>Agenda</h3>
                <p>Agendar e gerenciar consultas e procedimentos.</p>
            </router-link>
            <router-link :to="{ name: 'Pacientes' }" class="card">
                <h3>Pacientes</h3>
                <p>Gerenciar pacientes deste estabelecimento.</p>
            </router-link>
            <router-link :to="{ name: 'Financeiro' }" class="card">
                <h3>Financeiro</h3>
                <p>Controlar receitas, despesas e fluxo de caixa.</p>
            </router-link>
            <router-link :to="{ name: 'Orcamentos' }" class="card">
                <h3>Orçamentos</h3>
                <p>Criar e gerenciar orçamentos para pacientes.</p>
            </router-link>
            <router-link :to="{ name: 'Inventario' }" class="card">
                <h3>Inventário</h3>
                <p>Controlar estoque de insumos e materiais.</p>
            </router-link>
            <router-link :to="{ name: 'Relatorios' }" class="card">
                <h3>Relatórios</h3>
                <p>Faturamento por categoria e resumo de agendamentos.</p>
            </router-link>
        </nav>
    </main>
</template>

<style scoped>
.page-header {
    margin-bottom: 1.5rem;
}

h1 {
    margin: 0 0 0.2rem;
    font-size: 1.4rem;
}

.subtitulo {
    margin: 0;
    color: var(--text-muted);
    font-size: 0.9em;
}

/* KPIs */
.kpis {
    display: flex;
    gap: 1rem;
    flex-wrap: wrap;
    margin-bottom: 1.5rem;
}

.kpi {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 0.85rem 1.1rem;
    min-width: 140px;
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    box-shadow: var(--shadow);
}

.kpi-alerta {
    border-color: #fcd34d;
    background: #fffbeb;
}

.kpi-positivo {
    border-left: 3px solid var(--success);
}

.kpi-negativo {
    border-left: 3px solid var(--danger);
}

.kpi-valor {
    font-size: 1.6rem;
    font-weight: 700;
    line-height: 1;
}

.kpi-label {
    font-size: 0.78em;
    color: var(--text-muted);
}

.azul {
    color: var(--primary);
}

.laranja {
    color: var(--warning);
}

.vermelho {
    color: var(--danger);
}

.amarelo {
    color: #b45309;
}

/* Painéis */
.paineis {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
    gap: 1rem;
    margin-bottom: 1.5rem;
}

.painel {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1rem;
    box-shadow: var(--shadow);
}

.painel h3 {
    margin: 0 0 0.75rem;
    font-size: 0.9rem;
}

.item-lista {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    padding: 0.4rem 0;
    border-bottom: 1px solid #f3f4f6;
}

.item-lista:last-child {
    border-bottom: none;
}

.item-lista div {
    flex: 1;
    display: flex;
    flex-direction: column;
}

.item-lista strong {
    font-size: 0.88em;
}

.item-lista small {
    color: var(--text-muted);
    font-size: 0.78em;
}

.item-lista .hora {
    font-size: 0.78em;
    color: var(--text-muted);
    white-space: nowrap;
}

.item-alerta {
    background: #fffbeb;
    border-radius: var(--radius-sm);
    padding: 0.4rem 0.6rem;
    margin: 2px 0;
}

.status-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    flex-shrink: 0;
}

.ver-mais {
    display: block;
    margin-top: 0.6rem;
    font-size: 0.83em;
    color: var(--primary);
    text-decoration: none;
}

.ver-mais:hover {
    text-decoration: underline;
}

/* Navegação rápida */
.menu {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 1rem;
    margin-top: 0.5rem;
}

.card {
    display: block;
    padding: 1rem;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    text-decoration: none;
    color: inherit;
    background: var(--bg-card);
    box-shadow: var(--shadow);
    transition: border-color 0.15s, background 0.15s;
}

.card:hover {
    border-color: var(--primary);
    background: var(--primary-light);
}

.card h3 {
    margin: 0 0 0.25rem;
    color: var(--primary);
    font-size: 0.92rem;
}

.card p {
    margin: 0;
    font-size: 0.82em;
    color: var(--text-muted);
}

.info {
    color: var(--text-faint);
    font-size: 0.9em;
}
</style>
