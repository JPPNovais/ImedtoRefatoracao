<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { financeiroService, type KpisFinanceiro, type LancamentoExtrato } from "@/services/financeiroService"
import {
    AppKpiCard, AppFilterPills, AppPagination, AppModal, AppField, AppInput,
    AppSelect, AppDatePicker, AppButton, AppToast
} from "@/components/ui"
import { useTenantStore } from "@/stores/tenantStore"

// Props externas: controle do modal de lançamento pelo header (FinanceiroView)
const props = withDefaults(defineProps<{
    modalAbertoExterno?: boolean
}>(), { modalAbertoExterno: false })

const emit = defineEmits<{
    "update:modalAbertoExterno": [val: boolean]
}>()

// ─── Período ────────────────────────────────────────────────────────────────────
type ChipPeriodo = "hoje" | "semana" | "mes" | "personalizado"

function hoje(): string {
    return new Date().toISOString().split("T")[0]
}
function inicioSemana(): string {
    const d = new Date()
    const dia = d.getDay() // 0=dom
    const diff = dia === 0 ? -6 : 1 - dia // ajusta para segunda
    d.setDate(d.getDate() + diff)
    return d.toISOString().split("T")[0]
}
function inicioMes(): string {
    const d = new Date()
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-01`
}
function labelMesAno(): string {
    const d = new Date()
    return d.toLocaleDateString("pt-BR", { month: "long", year: "numeric" })
}

const chipAtivo = ref<ChipPeriodo>("mes")
const dataInicio = ref(inicioMes())
const dataFim = ref(hoje())

const chipOpcoes = [
    { valor: "hoje" as ChipPeriodo, label: "Hoje" },
    { valor: "semana" as ChipPeriodo, label: "Semana" },
    { valor: "mes" as ChipPeriodo, label: "Mês" },
    { valor: "personalizado" as ChipPeriodo, label: "Personalizado" },
]

function aplicarChip(chip: ChipPeriodo) {
    chipAtivo.value = chip
    if (chip === "hoje") {
        dataInicio.value = hoje()
        dataFim.value = hoje()
    } else if (chip === "semana") {
        dataInicio.value = inicioSemana()
        dataFim.value = hoje()
    } else if (chip === "mes") {
        dataInicio.value = inicioMes()
        dataFim.value = hoje()
    }
    // "personalizado": mantém datas atuais, exibe os pickers
}

const tenantStore = useTenantStore()
const labelPeriodo = computed(() => {
    const nome = tenantStore.ativo?.nomeFantasia ?? ""
    return `${labelMesAno()} · ${nome}`
})

// ─── KPIs ────────────────────────────────────────────────────────────────────────
const kpis = ref<KpisFinanceiro | null>(null)
const carregandoKpis = ref(false)
const erroKpis = ref<string | null>(null)

async function carregarKpis() {
    if (!dataInicio.value || !dataFim.value) return
    carregandoKpis.value = true
    erroKpis.value = null
    try {
        kpis.value = await financeiroService.kpis({ dataInicio: dataInicio.value, dataFim: dataFim.value })
    } catch {
        erroKpis.value = "Erro ao carregar indicadores."
    } finally {
        carregandoKpis.value = false
    }
}

// ─── Extrato ─────────────────────────────────────────────────────────────────────
const itens = ref<LancamentoExtrato[]>([])
const total = ref(0)
const pagina = ref(1)
const tamanho = ref(20)
const carregandoExtrato = ref(false)
const erroExtrato = ref<string | null>(null)

const filtroTipo = ref("")
const filtroOrigem = ref("")
const filtroForma = ref("")

async function carregarExtrato() {
    if (!dataInicio.value || !dataFim.value) return
    carregandoExtrato.value = true
    erroExtrato.value = null
    try {
        const pg = await financeiroService.extrato({
            dataInicio: dataInicio.value,
            dataFim: dataFim.value,
            tipo: filtroTipo.value || undefined,
            origem: filtroOrigem.value || undefined,
            formaPagamento: filtroForma.value || undefined,
            pagina: pagina.value,
            tamanho: tamanho.value,
        })
        itens.value = pg.itens
        total.value = pg.total
    } catch {
        erroExtrato.value = "Erro ao carregar extrato."
    } finally {
        carregandoExtrato.value = false
    }
}

// Reset página ao mudar filtros/período.
watch([filtroTipo, filtroOrigem, filtroForma, dataInicio, dataFim], () => {
    pagina.value = 1
})
watch([pagina, tamanho, filtroTipo, filtroOrigem, filtroForma, dataInicio, dataFim],
    () => { carregarExtrato(); carregarKpis() },
    { immediate: true }
)

// ─── Export ───────────────────────────────────────────────────────────────────────
const exportando = ref(false)
const toastMsg = ref<{ mensagem: string; variante: "success" | "error" } | null>(null)

async function exportar() {
    if (exportando.value) return
    exportando.value = true
    try {
        const blob = await financeiroService.exportarExtrato({
            dataInicio: dataInicio.value,
            dataFim: dataFim.value,
            tipo: filtroTipo.value || undefined,
            origem: filtroOrigem.value || undefined,
            formaPagamento: filtroForma.value || undefined,
        })
        const url = URL.createObjectURL(blob)
        const a = document.createElement("a")
        a.href = url
        a.download = `extrato-financeiro-${dataFim.value}.csv`
        a.click()
        URL.revokeObjectURL(url)
        toastMsg.value = { mensagem: "Extrato exportado com sucesso.", variante: "success" }
    } catch {
        toastMsg.value = { mensagem: "Não foi possível exportar.", variante: "error" }
    } finally {
        exportando.value = false
    }
}

// ─── Modal lançamento avulso ──────────────────────────────────────────────────────
const modalCriar = ref(false)
const formCriar = ref({ tipo: "Receita", descricao: "", valor: 0, dataVencimento: "", categoria: "" })
const erroCriar = ref<string | null>(null)
const salvando = ref(false)

// Sincroniza com prop externa (botão "+ Lançamento" do header)
watch(() => props.modalAbertoExterno, (val) => {
    if (val && !modalCriar.value) abrirModalCriar()
})
watch(modalCriar, (val) => {
    if (!val && props.modalAbertoExterno) emit("update:modalAbertoExterno", false)
})

function abrirModalCriar() {
    formCriar.value = { tipo: "Receita", descricao: "", valor: 0, dataVencimento: "", categoria: "" }
    erroCriar.value = null
    modalCriar.value = true
}

async function salvarCriar() {
    salvando.value = true; erroCriar.value = null
    try {
        await financeiroService.criar(formCriar.value)
        modalCriar.value = false
        pagina.value = 1
        await Promise.all([carregarExtrato(), carregarKpis()])
    } catch (e: any) {
        erroCriar.value = e?.response?.data?.mensagem ?? "Erro ao criar lançamento."
    } finally {
        salvando.value = false
    }
}

// Expõe exportar() para o FinanceiroView acionar via ref
defineExpose({ exportar })

// ─── Helpers ──────────────────────────────────────────────────────────────────────
function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}
function formatarData(s: string | null | undefined) {
    if (!s) return "—"
    const [y, m, d] = s.split("T")[0].split("-")
    return `${d}/${m}/${y}`
}
function tipoExtrato(lancamento: LancamentoExtrato): "in" | "out" | "refund" {
    if (lancamento.status === "Cancelado") return "refund"
    return lancamento.tipo === "Receita" ? "in" : "out"
}
function prefixoValor(tipo: "in" | "out" | "refund"): string {
    return tipo === "in" ? "+ " : "– "
}
</script>

<template>
    <div class="visao-geral-tab">
        <!-- Chips de período -->
        <div class="cf-period">
            <AppFilterPills
                v-model="chipAtivo"
                :opcoes="chipOpcoes"
                @update:modelValue="aplicarChip"
            />
            <template v-if="chipAtivo === 'personalizado'">
                <AppDatePicker v-model="dataInicio" aria-label="Data início" />
                <span class="sep">até</span>
                <AppDatePicker v-model="dataFim" aria-label="Data fim" />
            </template>
            <span class="cf-period-lbl">{{ labelPeriodo }}</span>
        </div>

        <!-- KPIs primários -->
        <div v-if="erroKpis" class="msg-erro">{{ erroKpis }}</div>
        <div class="kpi-grid" aria-label="KPIs do período">
            <AppKpiCard
                label="Recebido"
                :valor="kpis ? moeda(kpis.recebido) : '—'"
                icone="fa-solid fa-arrow-down-long"
                variante="success"
            />
            <AppKpiCard
                label="A receber"
                :valor="kpis ? moeda(kpis.aReceber) : '—'"
                icone="fa-solid fa-hourglass-half"
                variante="warning"
            />
            <AppKpiCard
                label="Despesas"
                :valor="kpis ? moeda(kpis.despesas) : '—'"
                icone="fa-solid fa-arrow-up-long"
                variante="error"
            />
            <AppKpiCard
                label="Saldo"
                :valor="kpis ? moeda(kpis.saldo) : '—'"
                icone="fa-solid fa-scale-balanced"
                variante="primary"
            />
        </div>

        <!-- KPIs secundários -->
        <div class="kpi-sec">
            <div class="kpi-sec-item">
                <span><i class="fa-solid fa-tag" aria-hidden="true" /> Descontos concedidos</span>
                <b>{{ kpis ? moeda(kpis.descontosConcedidos) : '—' }}</b>
            </div>
            <div class="kpi-sec-item">
                <span><i class="fa-solid fa-credit-card" aria-hidden="true" /> Taxas de cartão</span>
                <b>{{ kpis ? moeda(kpis.taxasCartao) : '—' }}</b>
            </div>
            <div class="kpi-sec-item">
                <span><i class="fa-solid fa-rotate-left" aria-hidden="true" /> Estornos</span>
                <b>{{ kpis ? moeda(kpis.estornos) : '—' }}</b>
            </div>
        </div>

        <!-- Card extrato -->
        <div class="cf-card">
            <div class="cf-card-h">
                <div><i class="fa-solid fa-list" aria-hidden="true" /> Extrato de lançamentos</div>
                <div class="cf-filters">
                    <select v-model="filtroTipo" aria-label="Tipo">
                        <option value="">Receitas e despesas</option>
                        <option value="Receita">Só receitas</option>
                        <option value="Despesa">Só despesas</option>
                    </select>
                    <select v-model="filtroOrigem" aria-label="Origem">
                        <option value="">Toda origem</option>
                        <option value="Consulta">Consulta</option>
                        <option value="Procedimento">Procedimento</option>
                        <option value="Cirurgia">Cirurgia</option>
                        <option value="Avulso">Avulso</option>
                    </select>
                    <select v-model="filtroForma" aria-label="Forma de pagamento">
                        <option value="">Toda forma</option>
                        <option value="PIX">PIX</option>
                        <option value="Dinheiro">Dinheiro</option>
                        <option value="Crédito">Crédito</option>
                        <option value="Débito">Débito</option>
                        <option value="Boleto">Boleto</option>
                    </select>
                </div>
            </div>

            <!-- Loading -->
            <div v-if="carregandoExtrato" class="cf-loading">Carregando...</div>

            <!-- Erro -->
            <div v-else-if="erroExtrato" class="msg-erro" style="padding: 1rem 1.125rem">{{ erroExtrato }}</div>

            <!-- Vazio -->
            <div v-else-if="itens.length === 0" class="cf-empty">
                <i class="fa-solid fa-receipt" aria-hidden="true" />
                <b>Nenhum lançamento no período</b>
                <p>Não há movimentação financeira para os filtros selecionados.</p>
            </div>

            <!-- Tabela -->
            <template v-else>
                <div class="cf-table" role="table" aria-label="Extrato de lançamentos">
                    <div class="cf-thead" role="row">
                        <div>Data</div>
                        <div>Descrição</div>
                        <div>Categoria</div>
                        <div>Forma</div>
                        <div class="ta-r">Valor</div>
                        <div>Status</div>
                    </div>
                    <div
                        v-for="l in itens"
                        :key="l.id"
                        class="cf-row"
                        role="row"
                    >
                        <div class="cf-date">{{ formatarData(l.dataPagamento ?? l.dataVencimento) }}</div>
                        <div class="cf-desc">
                            <span>{{ l.descricao }}</span>
                            <a v-if="l.pacienteNome" class="cf-link" :href="`/pacientes/${l.pacienteId}`">
                                <i class="fa-solid fa-user" aria-hidden="true" /> {{ l.pacienteNome }}
                            </a>
                        </div>
                        <div>
                            <span :class="['cf-cat', l.tipo === 'Receita' ? 'in' : 'out']">{{ l.categoria }}</span>
                        </div>
                        <div class="cf-method">{{ l.formaPagamento ?? '—' }}</div>
                        <div :class="['cf-amt', 'ta-r', tipoExtrato(l)]">
                            {{ prefixoValor(tipoExtrato(l)) }}{{ moeda(Math.abs(l.valor)).replace('R$ ', '') }}
                        </div>
                        <div>
                            <span v-if="l.status === 'Cancelado'" class="cf-st refund">Estorno</span>
                            <span v-else-if="l.status === 'Pendente'" class="cf-st pending">Pendente</span>
                            <span v-else class="cf-st ok">Liquidado</span>
                        </div>
                    </div>
                </div>

                <div class="cf-pagination">
                    <AppPagination
                        v-model:pagina="pagina"
                        v-model:tamanho="tamanho"
                        :total="total"
                        rotulo-itens="lançamentos"
                    />
                </div>
            </template>
        </div>
    </div>

    <!-- Modal criar lançamento -->
    <AppModal :aberto="modalCriar" titulo="Novo lançamento" @fechar="modalCriar = false">
        <AppField label="Tipo" required>
            <AppSelect v-model="formCriar.tipo">
                <option value="Receita">Receita</option>
                <option value="Despesa">Despesa</option>
            </AppSelect>
        </AppField>
        <AppField label="Descrição" required>
            <AppInput v-model="formCriar.descricao" />
        </AppField>
        <AppField label="Categoria" required>
            <AppInput v-model="formCriar.categoria" />
        </AppField>
        <AppField label="Valor (R$)" required>
            <AppInput v-model="formCriar.valor" type="number" :min="0.01" :step="0.01" />
        </AppField>
        <AppField label="Vencimento" required>
            <AppDatePicker v-model="formCriar.dataVencimento" placeholder="DD/MM/AAAA" />
        </AppField>
        <p v-if="erroCriar" class="msg-erro">{{ erroCriar }}</p>
        <template #rodape>
            <AppButton variant="secondary" @click="modalCriar = false">Cancelar</AppButton>
            <AppButton :disabled="salvando" :loading="salvando" @click="salvarCriar">Criar</AppButton>
        </template>
    </AppModal>

    <AppToast v-if="toastMsg" :mensagem="toastMsg.mensagem" :variante="toastMsg.variante" @fechar="toastMsg = null" />
</template>

<style scoped>
.visao-geral-tab { display: flex; flex-direction: column; gap: 1rem; }

/* Período */
.cf-period {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
}
.cf-period-lbl {
    margin-left: auto;
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.55);
}
.sep { font-size: var(--text-xs); color: hsl(var(--muted-foreground)); }

/* KPI grid */
.kpi-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 12px;
}

@media (max-width: 1100px) {
    .kpi-grid { grid-template-columns: repeat(2, 1fr); }
}

/* KPIs secundários */
.kpi-sec {
    display: flex;
    gap: 24px;
    flex-wrap: wrap;
    padding: 12px 18px;
    background: hsl(var(--secondary) / 0.03);
    border-radius: var(--radius-lg);
}
.kpi-sec-item {
    display: flex;
    flex-direction: column;
    gap: 2px;
}
.kpi-sec-item span {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.6);
}
.kpi-sec-item span i { font-size: var(--text-2xs); }
.kpi-sec-item b {
    font-size: var(--text-md);
    font-weight: var(--font-weight-bold);
    color: var(--c-primary-dark);
}

/* Card genérico */
.cf-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
    overflow: hidden;
}
.cf-card-h {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 14px 18px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.07);
}
.cf-card-h > div:first-child {
    display: inline-flex;
    align-items: center;
    gap: 9px;
    font-size: var(--text-base);
    font-weight: var(--font-weight-bold);
    color: var(--c-primary-dark);
}
.cf-card-h i { color: hsl(var(--primary)); }

/* Filtros */
.cf-filters {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}
.cf-filters select {
    padding: 7px 28px 7px 10px;
    border: 1px solid hsl(var(--secondary) / 0.15);
    border-radius: var(--radius-md);
    font-size: var(--text-xs);
    font-family: inherit;
    color: var(--c-primary-dark);
    background: hsl(var(--card));
    cursor: pointer;
    outline: none;
}
.cf-filters select:focus { border-color: hsl(var(--primary)); }

/* Tabela */
.cf-loading {
    padding: 1.5rem 1.125rem;
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
}
.cf-table { font-size: var(--text-sm); }
.cf-thead, .cf-row {
    display: grid;
    grid-template-columns: 110px 1fr 130px 90px 130px 110px;
    gap: 12px;
    align-items: center;
    padding: 11px 18px;
}
.cf-thead {
    background: hsl(var(--secondary) / 0.03);
    border-bottom: 1px solid hsl(var(--secondary) / 0.07);
    font-size: var(--text-2xs);
    font-weight: var(--font-weight-bold);
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: hsl(var(--secondary) / 0.5);
}
.cf-row { border-bottom: 1px solid hsl(var(--secondary) / 0.05); }
.cf-row:last-child { border-bottom: none; }
.ta-r { text-align: right; }

.cf-date { font-size: var(--text-xs); color: hsl(var(--secondary) / 0.65); }
.cf-desc { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.cf-desc > span { font-weight: var(--font-weight-semibold); color: var(--c-primary-dark); }
.cf-link {
    font-size: var(--text-2xs);
    color: hsl(var(--primary));
    text-decoration: none;
    display: inline-flex;
    align-items: center;
    gap: 5px;
    width: fit-content;
}
.cf-link:hover { text-decoration: underline; }
.cf-link i { font-size: var(--text-2xs); }

.cf-cat {
    display: inline-block;
    padding: 2px 9px;
    border-radius: 999px;
    font-size: var(--text-2xs);
    font-weight: var(--font-weight-semibold);
}
.cf-cat.in  { background: hsl(var(--success) / 0.1);    color: hsl(160 79% 30%); }
.cf-cat.out { background: hsl(var(--destructive) / 0.08); color: hsl(var(--destructive)); }

.cf-method { font-size: var(--text-xs); color: hsl(var(--secondary) / 0.65); }

.cf-amt { font-weight: var(--font-weight-extrabold); }
.cf-amt.in     { color: hsl(160 79% 32%); }
.cf-amt.out    { color: hsl(var(--destructive)); }
.cf-amt.refund { color: hsl(28 90% 45%); }

.cf-st {
    font-size: var(--text-2xs);
    font-weight: var(--font-weight-semibold);
    padding: 3px 9px;
    border-radius: 999px;
}
.cf-st.ok      { background: hsl(var(--success) / 0.1);    color: hsl(160 79% 30%); }
.cf-st.pending { background: hsl(var(--warning) / 0.14);   color: hsl(28 90% 42%); }
.cf-st.refund  { background: hsl(28 90% 50% / 0.14);       color: hsl(28 90% 40%); }

/* Empty */
.cf-empty {
    text-align: center;
    padding: 48px 24px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
}
.cf-empty > i { font-size: var(--text-3xl); color: hsl(var(--secondary) / 0.25); }
.cf-empty b { font-size: var(--text-md); color: var(--c-primary-dark); }
.cf-empty p { font-size: var(--text-sm); color: hsl(var(--secondary) / 0.6); margin: 0; }

.cf-pagination { padding: 12px 18px; border-top: 1px solid hsl(var(--secondary) / 0.07); }

.msg-erro { color: hsl(var(--destructive)); font-size: var(--text-sm); margin: 0; }
</style>
