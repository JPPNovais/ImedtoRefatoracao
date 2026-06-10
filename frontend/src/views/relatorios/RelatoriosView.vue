<script setup lang="ts">
/**
 * RelatoriosView — view única com tabs (Opção A).
 * Rotas /relatorios/financeiro, /relatorios/operacional, etc.
 * redirecionam para /relatorios?aba=... (ver router/index.ts).
 */
import { ref, watch, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { AppPageHeader, AppTabs, AppButton, AppToast } from '@/components/ui'
import RelatorioPeriodoFiltro from '@/components/relatorios/RelatorioPeriodoFiltro.vue'
import RelatorioVisaoGeral from '@/components/relatorios/RelatorioVisaoGeral.vue'
import RelatorioFinanceiroTab from '@/components/relatorios/RelatorioFinanceiroTab.vue'
import RelatorioAgendaTab from '@/components/relatorios/RelatorioAgendaTab.vue'
import RelatorioPessoasTab from '@/components/relatorios/RelatorioPessoasTab.vue'
import RelatorioOrcamentosTab from '@/components/relatorios/RelatorioOrcamentosTab.vue'
import { useRelatorioCsv } from '@/composables/useRelatorioCsv'
import {
    relatorioService,
    type RelatorioFinanceiro,
    type RelatorioOperacional,
    type RelatorioPessoas,
    type RelatorioOrcamentos,
} from '@/services/relatorioService'

// ─── Router (deep-link por query ?aba=) ──────────────────────────────────────

const route  = useRoute()
const router = useRouter()

type AbaId = 'visao-geral' | 'financeiro' | 'agenda' | 'pessoas' | 'orcamentos'

const abaAtual = ref<AbaId>((route.query.aba as AbaId) ?? 'visao-geral')

watch(abaAtual, (nova) => {
    router.replace({ query: { ...route.query, aba: nova } })
})

// Sincroniza caso a URL seja alterada externamente
watch(() => route.query.aba, (v) => {
    if (v && v !== abaAtual.value) abaAtual.value = v as AbaId
})

const abas = [
    { valor: 'visao-geral',  label: 'Visão geral', icone: 'fa-solid fa-gauge-high' },
    { valor: 'financeiro',   label: 'Financeiro',  icone: 'fa-solid fa-coins' },
    { valor: 'agenda',       label: 'Agenda',       icone: 'fa-solid fa-calendar-check' },
    { valor: 'pessoas',      label: 'Pessoas',      icone: 'fa-solid fa-user-group' },
    { valor: 'orcamentos',   label: 'Orçamentos',  icone: 'fa-solid fa-file-invoice-dollar' },
]

// ─── Filtros de período ───────────────────────────────────────────────────────

type PresetPeriodo = 'hoje' | '7d' | '30d' | 'mes' | 'trim' | 'custom'
const presetAtual  = ref<PresetPeriodo>('30d')
const comparar     = ref(false)

const filtroAtual  = ref({ dataInicio: '', dataFim: '' })

function calcularDatasPadrao(): { dataInicio: string; dataFim: string } {
    const hoje = new Date()
    const fmt = (d: Date) => d.toISOString().slice(0, 10)
    const ini = new Date(hoje)
    ini.setDate(hoje.getDate() - 29)
    return { dataInicio: fmt(ini), dataFim: fmt(hoje) }
}

function onAplicarPeriodo(datas: { dataInicio: string; dataFim: string }) {
    filtroAtual.value = datas
    carregarTudo()
}

// ─── Dados ────────────────────────────────────────────────────────────────────

const financeiro  = ref<RelatorioFinanceiro | null>(null)
const operacional = ref<RelatorioOperacional | null>(null)
const pessoas     = ref<RelatorioPessoas | null>(null)
const orcamentos  = ref<RelatorioOrcamentos | null>(null)

const carregandoFin  = ref(false)
const carregandoOp   = ref(false)
const carregandoPes  = ref(false)
const carregandoOrc  = ref(false)

const erroFin  = ref<string | null>(null)
const erroOp   = ref<string | null>(null)
const erroPes  = ref<string | null>(null)
const erroOrc  = ref<string | null>(null)

const carregandoGeral = computed(() =>
    carregandoFin.value || carregandoOp.value || carregandoPes.value || carregandoOrc.value
)

async function carregarFinanceiro() {
    carregandoFin.value = true
    erroFin.value = null
    try {
        financeiro.value = await relatorioService.financeiro({
            dataInicio: filtroAtual.value.dataInicio || undefined,
            dataFim:    filtroAtual.value.dataFim    || undefined,
            agruparPor: 'categoria',
        })
    } catch (e: any) {
        erroFin.value = e?.response?.data?.mensagem ?? 'Erro ao carregar relatório financeiro.'
    } finally {
        carregandoFin.value = false
    }
}

async function carregarOperacional() {
    carregandoOp.value = true
    erroOp.value = null
    try {
        operacional.value = await relatorioService.operacional({
            dataInicio: filtroAtual.value.dataInicio || undefined,
            dataFim:    filtroAtual.value.dataFim    || undefined,
            tipo: 'agenda',
        })
    } catch (e: any) {
        erroOp.value = e?.response?.data?.mensagem ?? 'Erro ao carregar relatório operacional.'
    } finally {
        carregandoOp.value = false
    }
}

async function carregarPessoas() {
    carregandoPes.value = true
    erroPes.value = null
    try {
        pessoas.value = await relatorioService.pessoas({
            dataInicio: filtroAtual.value.dataInicio || undefined,
            dataFim:    filtroAtual.value.dataFim    || undefined,
            tipo: 'profissionais',
        })
    } catch (e: any) {
        erroPes.value = e?.response?.data?.mensagem ?? 'Erro ao carregar relatório de pessoas.'
    } finally {
        carregandoPes.value = false
    }
}

async function carregarOrcamentos() {
    carregandoOrc.value = true
    erroOrc.value = null
    try {
        orcamentos.value = await relatorioService.orcamentos({
            dataInicio: filtroAtual.value.dataInicio || undefined,
            dataFim:    filtroAtual.value.dataFim    || undefined,
        })
    } catch (e: any) {
        erroOrc.value = e?.response?.data?.mensagem ?? 'Erro ao carregar relatório de orçamentos.'
    } finally {
        carregandoOrc.value = false
    }
}

function carregarTudo() {
    // Carrega em paralelo — lazy: cada tab usa seus próprios dados
    carregarFinanceiro()
    carregarOperacional()
    carregarPessoas()
    carregarOrcamentos()
}

// ─── Toast ───────────────────────────────────────────────────────────────────

const toast = ref<{ msg: string; variante: 'info' | 'success' | 'error' } | null>(null)

// ─── Export CSV ───────────────────────────────────────────────────────────────

const { exportarFinanceiro, exportarAgenda, exportarPessoas, exportarOrcamentos, exportarVisaoGeral } = useRelatorioCsv()

/**
 * Retorna true quando a aba ativa não tem dados tabulares para exportar.
 * Critério conservador: desabilita quando carregando ou quando o objeto
 * de dados está nulo (nunca chegou) ou vazio (sem linhas relevantes).
 */
const csvDesabilitado = computed(() => {
    if (carregandoGeral.value) return true
    switch (abaAtual.value) {
        case 'financeiro':
            return !financeiro.value || financeiro.value.breakdown.length === 0
        case 'agenda':
            return !operacional.value || (operacional.value.kpis.length === 0 && operacional.value.breakdown.length === 0)
        case 'pessoas':
            return !pessoas.value ||
                (pessoas.value.tipo === 'profissionais'
                    ? !pessoas.value.rankingProfissionais?.length
                    : !pessoas.value.topPacientes?.length)
        case 'orcamentos':
            return !orcamentos.value || orcamentos.value.funil.totalCriados === 0
        case 'visao-geral':
            return !financeiro.value && !operacional.value && !orcamentos.value
        default:
            return true
    }
})

function exportarCsvAbaAtual() {
    try {
        const periodo = filtroAtual.value
        switch (abaAtual.value) {
            case 'financeiro':
                if (financeiro.value) exportarFinanceiro(financeiro.value, periodo)
                break
            case 'agenda':
                if (operacional.value) exportarAgenda(operacional.value, periodo)
                break
            case 'pessoas':
                if (pessoas.value) exportarPessoas(pessoas.value, periodo)
                break
            case 'orcamentos':
                if (orcamentos.value) exportarOrcamentos(orcamentos.value, periodo)
                break
            case 'visao-geral':
                exportarVisaoGeral(
                    { financeiro: financeiro.value, operacional: operacional.value, orcamentos: orcamentos.value },
                    periodo,
                )
                break
        }
    } catch {
        // Toast genérico sem PII — erro técnico interno não expõe dado
        toast.value = { msg: 'Não foi possível gerar o CSV.', variante: 'error' }
    }
}

// ─── Mount: calcula período padrão (30d) e carrega ───────────────────────────
onMounted(() => {
    filtroAtual.value = calcularDatasPadrao()
    carregarTudo()
})
</script>

<template>
    <div class="app-page app-page--wide">
        <AppPageHeader titulo="Relatórios" subtitulo="Indicadores e análises da operação">
            <template #acoes>
                <AppButton
                    variant="secondary"
                    size="sm"
                    icon="fa-solid fa-file-csv"
                    :disabled="csvDesabilitado"
                    @click="exportarCsvAbaAtual"
                >
                    Exportar CSV
                </AppButton>
                <AppButton
                    variant="ghost"
                    size="sm"
                    icon="fa-solid fa-rotate-right"
                    :loading="carregandoGeral"
                    @click="carregarTudo"
                >
                    Atualizar
                </AppButton>
            </template>
        </AppPageHeader>

        <!-- Filtro de período -->
        <RelatorioPeriodoFiltro
            v-model="presetAtual"
            v-model:comparar="comparar"
            @aplicar="onAplicarPeriodo"
        />

        <!-- Tabs de navegação -->
        <AppTabs
            v-model="abaAtual"
            :abas="abas"
            variante="underline"
            aria-label="Seções de relatório"
        />

        <!-- Conteúdo da aba ativa -->
        <div class="rp-corpo">
            <RelatorioVisaoGeral
                v-if="abaAtual === 'visao-geral'"
                :financeiro="financeiro"
                :operacional="operacional"
                :orcamentos="orcamentos"
                :carregando="carregandoGeral"
            />

            <RelatorioFinanceiroTab
                v-else-if="abaAtual === 'financeiro'"
                :dados="financeiro"
                :carregando="carregandoFin"
                :erro="erroFin"
                :comparar="comparar"
            />

            <RelatorioAgendaTab
                v-else-if="abaAtual === 'agenda'"
                :dados="operacional"
                :carregando="carregandoOp"
                :erro="erroOp"
            />

            <RelatorioPessoasTab
                v-else-if="abaAtual === 'pessoas'"
                :dados="pessoas"
                :carregando="carregandoPes"
                :erro="erroPes"
            />

            <RelatorioOrcamentosTab
                v-else-if="abaAtual === 'orcamentos'"
                :dados="orcamentos"
                :carregando="carregandoOrc"
                :erro="erroOrc"
            />
        </div>

        <AppToast
            v-if="toast"
            :mensagem="toast.msg"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </div>
</template>

<style scoped>
.rp-corpo {
    display: flex;
    flex-direction: column;
}
</style>
