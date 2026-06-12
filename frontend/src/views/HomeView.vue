<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { usePermissoesStore } from "@/stores/permissoesStore"
import { dashboardService, type DashboardData } from "@/services/dashboardService"
import { podeAcessarRota } from "@/router/routePermissions"
import { AppToast, AppAlertCard } from "@/components/ui"

const auth = useAuthStore()
const tenant = useTenantStore()
const permissoes = usePermissoesStore()
const route = useRoute()
const router = useRouter()

const dashboard = ref<DashboardData | null>(null)
const carregando = ref(false)

/**
 * Três estados possíveis ao entrar em /home:
 *  - "comVinculo"   → tenant.ativo populado pelo bootstrap → renderiza dashboard.
 *  - "semVinculo"   → backend confirmou lista vazia (tenant.semEstabelecimento=true).
 *  - "indeterminado"→ tenant.ativo null sem confirmação (bootstrap ainda não rodou ou falhou).
 *
 * Crítico: nunca mostrar "sem vínculo" quando o estado é indeterminado — induz o
 * usuário a achar que perdeu o acesso quando na verdade foi falha de boot.
 */
const estadoVinculo = computed<"comVinculo" | "semVinculo" | "indeterminado">(() => {
    if (tenant.ativo) return "comVinculo"
    if (tenant.semEstabelecimento) return "semVinculo"
    return "indeterminado"
})

onMounted(async () => {
    if (estadoVinculo.value !== "comVinculo") return
    carregando.value = true
    try {
        dashboard.value = await dashboardService.obter()
    } catch {
        // dashboard não-crítico — falha silenciosa
    } finally {
        carregando.value = false
    }
})

function recarregar() {
    window.location.reload()
}

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

// Catálogo de cards do dashboard. Cada item declara explicitamente a rota
// nomeada — assim a regra de exibição reusa o mesmo `podeAcessarRota` que o
// router guard e o `AppLayout` consomem (single source of truth em
// `routePermissions.ts`). Sem isso, um Médico via "Financeiro" e "Inventário"
// no Home, clicava e era jogado de volta para Home — UX ruim e confusa.
interface CardAtalho {
    routeName: string
    titulo: string
    descricao: string
}

const CARDS_ATALHO: readonly CardAtalho[] = [
    { routeName: "Agenda",      titulo: "Agenda",      descricao: "Agendar e gerenciar consultas e procedimentos." },
    { routeName: "Pacientes",   titulo: "Pacientes",   descricao: "Gerenciar pacientes deste estabelecimento." },
    { routeName: "Financeiro",  titulo: "Financeiro",  descricao: "Controlar receitas, despesas e fluxo de caixa." },
    { routeName: "Orcamentos",  titulo: "Orçamentos",  descricao: "Cotações de cirurgias e procedimentos." },
    { routeName: "Inventario",  titulo: "Inventário",  descricao: "Controlar estoque de insumos e materiais." },
    { routeName: "Relatorios",  titulo: "Relatórios",  descricao: "Faturamento por categoria e resumo de agendamentos." },
] as const

const permHelpers = computed(() => ({
    ehDono: permissoes.ehDono,
    pode: (k: string) => permissoes.pode(k),
    podeExtra: (k: string) => permissoes.podeExtra(k),
}))

const cardsVisiveis = computed<readonly CardAtalho[]>(() =>
    CARDS_ATALHO.filter(c => podeAcessarRota(c.routeName, permHelpers.value))
)

function moedaCompacta(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 2 })
}

// Cards de alerta — cada um aparece somente se:
//   1. o dashboard foi carregado (dado disponível),
//   2. a pendência é > 0,
//   3. o usuário tem acesso à rota destino (gate RBAC via podeAcessarRota/routePermissions.ts — R6).
const cardAlertas = computed(() => {
    if (!dashboard.value) return []
    const h = permHelpers.value
    const alertas = []

    if (
        dashboard.value.lancamentosVencidos > 0 &&
        podeAcessarRota("Financeiro", h)
    ) {
        alertas.push({ tipo: "vencidos" as const })
    }
    if (
        dashboard.value.itensAbaixoMinimo > 0 &&
        podeAcessarRota("Inventario", h)
    ) {
        alertas.push({ tipo: "estoque" as const })
    }
    if (
        dashboard.value.orcamentosPendentes > 0 &&
        podeAcessarRota("Orcamentos", h)
    ) {
        alertas.push({ tipo: "orcamentos" as const })
    }
    return alertas
})

// Toast pós-redirect: o router insere `?bloqueado=<rota>` quando manda para
// /home por falta de permissão. Aqui consumimos o querystring (mostra toast +
// limpa a URL para não reaparecer no F5 ou ao compartilhar o link).
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)

function tratarRedirectBloqueado() {
    if (!route.query.bloqueado) return
    toast.value = {
        mensagem: "Esta área é restrita ao seu papel.",
        variante: "info",
    }
    // Remove o querystring sem disparar nova navegação no histórico.
    const { bloqueado, ...resto } = route.query
    void bloqueado
    router.replace({ query: resto })
}

watch(() => route.query.bloqueado, () => tratarRedirectBloqueado(), { immediate: true })
</script>

<template>
    <main class="app-page home">
        <!-- Estado indeterminado: bootstrap ainda não terminou ou falhou. NÃO mostrar
             "sem vínculo" aqui — seria mentira pro usuário com vínculo. Em vez disso,
             oferecemos retry. Se o boot terminar OK durante a renderização, este branch
             desaparece via reatividade. -->
        <section v-if="estadoVinculo === 'indeterminado'" class="sem-vinculo sem-vinculo--indet">
            <div class="sv-icone"><i class="fa-solid fa-circle-exclamation" aria-hidden="true"></i></div>
            <h1>Não conseguimos carregar seus dados</h1>
            <p class="sv-msg">
                Houve uma falha ao validar seu vínculo com o estabelecimento.
                Recarregue a página — se persistir, faça login novamente.
            </p>
            <div class="sv-acoes">
                <button type="button" class="sv-btn primario" @click="recarregar">
                    <i class="fa-solid fa-rotate" aria-hidden="true"></i>
                    Recarregar
                </button>
                <router-link :to="{ name: 'MinhaConta' }" class="sv-btn secundario">
                    <i class="fa-solid fa-user-pen" aria-hidden="true"></i>
                    Meus dados
                </router-link>
            </div>
        </section>

        <!-- Modo sem vínculo: backend CONFIRMOU lista vazia. Empty state guiando para
             convites e perfil. -->
        <section v-else-if="estadoVinculo === 'semVinculo'" class="sem-vinculo">
            <div class="sv-icone"><i class="fa-solid fa-hand-holding-heart" aria-hidden="true"></i></div>
            <h1>Olá, {{ auth.usuario?.nomeCompleto?.split(" ")[0] ?? "" }} 👋</h1>
            <p class="sv-msg">
                Você ainda não está vinculado a um estabelecimento.
                Para começar a usar o Imedto, aceite um convite enviado por uma clínica
                ou aguarde até que alguém te convide.
            </p>
            <div class="sv-acoes">
                <router-link :to="{ name: 'MeusConvites' }" class="sv-btn primario">
                    <i class="fa-solid fa-envelope-open-text" aria-hidden="true"></i>
                    Ver meus convites
                </router-link>
                <router-link :to="{ name: 'MinhaConta' }" class="sv-btn secundario">
                    <i class="fa-solid fa-user-pen" aria-hidden="true"></i>
                    Editar meus dados
                </router-link>
            </div>
            <p class="sv-hint">
                <i class="fa-solid fa-circle-info" aria-hidden="true"></i>
                Quando o convite chegar, ele aparecerá automaticamente em
                <router-link :to="{ name: 'MeusConvites' }">Meus convites</router-link>.
            </p>
        </section>

        <template v-else>
        <div class="page-header">
            <h1>Olá, {{ auth.usuario?.nomeCompleto ?? auth.usuario?.email }}</h1>
            <p class="subtitulo">Bem-vindo ao painel.</p>
        </div>

        <!-- Bloco "Precisa da sua atenção" — R7: só renderiza quando há ao menos 1 card visível -->
        <section v-if="cardAlertas.length > 0" class="alertas-bloco">
            <h2 class="ds-section-title">Precisa da sua atenção</h2>
            <div class="alertas-grid">
                <!-- Card: Lançamentos vencidos -->
                <AppAlertCard
                    v-if="cardAlertas.some(c => c.tipo === 'vencidos')"
                    :to="{ name: 'Financeiro', query: { filtro: 'vencidos' } }"
                    titulo="Lançamentos vencidos"
                    icone="fa-solid fa-circle-exclamation"
                    :contagem="dashboard!.lancamentosVencidos"
                    variante="error"
                >
                    <template #contexto>
                        <span v-if="dashboard!.vencidosAReceber > 0">
                            {{ moedaCompacta(dashboard!.vencidosAReceber) }} a receber
                        </span>
                        <span v-if="dashboard!.vencidosAPagar > 0">
                            {{ moedaCompacta(dashboard!.vencidosAPagar) }} a pagar
                        </span>
                    </template>
                </AppAlertCard>

                <!-- Card: Itens abaixo do mínimo -->
                <AppAlertCard
                    v-if="cardAlertas.some(c => c.tipo === 'estoque')"
                    :to="{ name: 'Inventario', query: { status: 'baixo' } }"
                    titulo="Itens abaixo do mínimo"
                    icone="fa-solid fa-box-open"
                    :contagem="dashboard!.itensAbaixoMinimo"
                    variante="warning"
                >
                    <template #contexto>
                        <span>{{ dashboard!.itensAbaixoMinimo }} {{ dashboard!.itensAbaixoMinimo === 1 ? 'item precisa' : 'itens precisam' }} de reposição</span>
                    </template>
                </AppAlertCard>

                <!-- Card: Orçamentos pendentes -->
                <AppAlertCard
                    v-if="cardAlertas.some(c => c.tipo === 'orcamentos')"
                    :to="{ name: 'Orcamentos', query: { status: 'pendentes' } }"
                    titulo="Orçamentos pendentes"
                    icone="fa-solid fa-file-invoice"
                    :contagem="dashboard!.orcamentosPendentes"
                    variante="info"
                >
                    <template #contexto>
                        <span>{{ dashboard!.orcamentosPendentes }} {{ dashboard!.orcamentosPendentes === 1 ? 'aguardando' : 'aguardando' }} andamento</span>
                    </template>
                </AppAlertCard>
            </div>
        </section>

        <!-- KPIs neutros — sem alertas soltos (CA14) -->
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
        </section>
        <p v-else-if="carregando" class="info">Carregando dashboard...</p>

        <!-- Próximos agendamentos -->
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
        </div>

        <!-- Navegação rápida — cards filtrados pelo papel/permissões do
             usuário corrente (single source of truth: routePermissions.ts). -->
        <nav v-if="cardsVisiveis.length > 0" class="menu">
            <router-link
                v-for="card in cardsVisiveis"
                :key="card.routeName"
                :to="{ name: card.routeName }"
                class="card"
            >
                <h3>{{ card.titulo }}</h3>
                <p>{{ card.descricao }}</p>
            </router-link>
        </nav>
        </template>

        <AppToast
            v-if="toast"
            :mensagem="toast.mensagem"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </main>
</template>

<style scoped>
/* Modo "sem vínculo" — empty state centralizado com CTA */
.sem-vinculo {
    max-width: 540px;
    margin: 4rem auto;
    text-align: center;
    padding: 2.5rem 2rem;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 16px;
    box-shadow: 0 1px 2px hsl(var(--primary-dark) / 0.04), 0 16px 40px hsl(var(--primary-dark) / 0.06);
}
.sem-vinculo h1 { margin: 0 0 0.6rem; font-size: var(--text-2xl); color: hsl(var(--primary-dark)); }
.sv-icone {
    width: 64px; height: 64px; border-radius: 16px;
    background: hsl(var(--primary) / 0.1); color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 26px; margin-bottom: 1rem;
}
.sv-msg {
    color: hsl(var(--secondary) / 0.75);
    line-height: 1.55;
    font-size: 0.95rem;
    margin: 0 0 1.5rem;
}
.sv-acoes {
    display: flex; gap: 10px; justify-content: center; flex-wrap: wrap;
    margin-bottom: 1.25rem;
}
.sv-btn {
    display: inline-flex; align-items: center; gap: 8px;
    padding: 11px 18px; border-radius: 10px;
    font-size: 0.9rem; font-weight: 600; text-decoration: none;
    transition: all 160ms;
}
.sv-btn.primario {
    background: hsl(var(--primary)); color: white;
    box-shadow: 0 1px 2px hsl(var(--primary-dark) / 0.2);
}
.sv-btn.primario:hover { background: hsl(var(--primary-dark)); transform: translateY(-1px); }
.sv-btn.secundario {
    background: hsl(var(--card));
    color: hsl(var(--primary-dark));
    border: 1.5px solid hsl(var(--secondary) / 0.15);
}
.sv-btn.secundario:hover { border-color: hsl(var(--primary) / 0.4); }
.sv-hint {
    font-size: 0.78rem;
    color: hsl(var(--secondary) / 0.6);
    margin: 0;
    line-height: 1.5;
}
.sv-hint a { color: hsl(var(--primary)); font-weight: 600; }

.page-header {
    margin-bottom: 1.5rem;
}

/* Bloco "Precisa da sua atenção" */
.alertas-bloco {
    margin-bottom: 1.5rem;
}

.alertas-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
    gap: 0.75rem;
    margin-top: 0.6rem;
}

h1 {
    margin: 0 0 0.2rem;
    font-size: var(--text-2xl);
    font-weight: var(--font-weight-bold);
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
    border-color: hsl(var(--warning) / 0.45);
    background: hsl(var(--warning) / 0.12);
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
    font-size: var(--text-base);
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
    background: hsl(var(--warning) / 0.12);
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
