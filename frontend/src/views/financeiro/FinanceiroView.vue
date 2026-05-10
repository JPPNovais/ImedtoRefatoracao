<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { financeiroService, type Lancamento, type ResumoFinanceiro } from "@/services/financeiroService"
import { AppButton, AppDatePicker, AppField, AppInput, AppModal, AppPagination, AppSelect } from "@/components/ui"

const lancamentos = ref<Lancamento[]>([])
const total = ref(0)
const pagina = ref(1)
const tamanho = ref(20)
const resumo = ref<ResumoFinanceiro>({ totalReceitasPagas: 0, totalDespesasPagas: 0, saldo: 0, receitasPendentes: 0, despesasPendentes: 0 })
const carregando = ref(false)
const erro = ref<string | null>(null)

const filtroTipo = ref("")
const filtroStatus = ref("")
const filtroDataInicio = ref("")
const filtroDataFim = ref("")

const modalCriar = ref(false)
const formCriar = ref({ tipo: "Receita", descricao: "", valor: 0, dataVencimento: "", categoria: "" })
const erroCriar = ref<string | null>(null)
const salvando = ref(false)

const lancamentoEditando = ref<Lancamento | null>(null)
const formEditar = ref({ descricao: "", valor: 0, dataVencimento: "", categoria: "" })
const erroEditar = ref<string | null>(null)

const categoriasSugeridas = computed(() =>
    [...new Set(lancamentos.value.map(l => l.categoria))].sort()
)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const params = {
            tipo: filtroTipo.value || undefined,
            status: filtroStatus.value || undefined,
            dataInicio: filtroDataInicio.value || undefined,
            dataFim: filtroDataFim.value || undefined,
            pagina: pagina.value,
            tamanho: tamanho.value,
        }
        const [pg, res] = await Promise.all([
            financeiroService.listar(params),
            financeiroService.resumo({
                dataInicio: filtroDataInicio.value || undefined,
                dataFim: filtroDataFim.value || undefined,
            }),
        ])
        lancamentos.value = pg.itens
        total.value = pg.total
        resumo.value = res
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar dados financeiros."
    } finally {
        carregando.value = false
    }
}

watch([filtroTipo, filtroStatus, filtroDataInicio, filtroDataFim], () => {
    pagina.value = 1
})
watch([filtroTipo, filtroStatus, filtroDataInicio, filtroDataFim, pagina, tamanho], carregar, { immediate: true })

function abrirModalCriar() {
    formCriar.value = { tipo: "Receita", descricao: "", valor: 0, dataVencimento: "", categoria: "" }
    erroCriar.value = null
    modalCriar.value = true
}

async function salvarCriar() {
    salvando.value = true
    erroCriar.value = null
    try {
        await financeiroService.criar(formCriar.value)
        modalCriar.value = false
        await carregar()
    } catch (e: any) {
        erroCriar.value = e?.response?.data?.mensagem ?? "Erro ao criar lançamento."
    } finally {
        salvando.value = false
    }
}

function abrirEditar(l: Lancamento) {
    lancamentoEditando.value = l
    formEditar.value = { descricao: l.descricao, valor: l.valor, dataVencimento: l.dataVencimento, categoria: l.categoria }
    erroEditar.value = null
}

async function salvarEditar() {
    if (!lancamentoEditando.value) return
    salvando.value = true
    erroEditar.value = null
    try {
        await financeiroService.atualizar(lancamentoEditando.value.id, formEditar.value)
        lancamentoEditando.value = null
        await carregar()
    } catch (e: any) {
        erroEditar.value = e?.response?.data?.mensagem ?? "Erro ao atualizar."
    } finally {
        salvando.value = false
    }
}

async function pagar(l: Lancamento) {
    if (!confirm(`Baixar "${l.descricao}" como pago?`)) return
    try {
        await financeiroService.pagar(l.id)
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao registrar pagamento.")
    }
}

async function cancelar(l: Lancamento) {
    if (!confirm(`Cancelar "${l.descricao}"?`)) return
    try {
        await financeiroService.cancelar(l.id)
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao cancelar.")
    }
}

const statusCor: Record<string, string> = {
    Pendente: "badge-pendente",
    Pago: "badge-pago",
    Cancelado: "badge-cancelado",
}

const tipoCor: Record<string, string> = {
    Receita: "receita",
    Despesa: "despesa",
}

function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function formatarData(s: string) {
    const [y, m, d] = s.split("T")[0].split("-")
    return `${d}/${m}/${y}`
}

const vencidoPendente = (l: Lancamento) =>
    l.status === "Pendente" && new Date(l.dataVencimento) < new Date()
</script>

<template>
    <main class="app-page financeiro">
        <header class="page-header">
            <div>
                <h1 class="page-titulo">Financeiro</h1>
                <p class="page-sub">Controle receitas, despesas e fluxo de caixa do estabelecimento.</p>
            </div>
            <AppButton icon="fa-solid fa-plus" @click="abrirModalCriar">Novo lançamento</AppButton>
        </header>

        <section class="resumo-cards">
            <div class="card-resumo receita">
                <span class="label">Receitas pagas</span>
                <span class="valor">{{ moeda(resumo.totalReceitasPagas) }}</span>
                <span class="sub">Pendentes: {{ moeda(resumo.receitasPendentes) }}</span>
            </div>
            <div class="card-resumo despesa">
                <span class="label">Despesas pagas</span>
                <span class="valor">{{ moeda(resumo.totalDespesasPagas) }}</span>
                <span class="sub">Pendentes: {{ moeda(resumo.despesasPendentes) }}</span>
            </div>
            <div class="card-resumo" :class="resumo.saldo >= 0 ? 'saldo-positivo' : 'saldo-negativo'">
                <span class="label">Saldo</span>
                <span class="valor">{{ moeda(resumo.saldo) }}</span>
                <span class="sub">Receitas − Despesas pagas</span>
            </div>
        </section>

        <section class="filtros">
            <select v-model="filtroTipo">
                <option value="">Todos os tipos</option>
                <option value="Receita">Receita</option>
                <option value="Despesa">Despesa</option>
            </select>
            <select v-model="filtroStatus">
                <option value="">Todos os status</option>
                <option value="Pendente">Pendente</option>
                <option value="Pago">Pago</option>
                <option value="Cancelado">Cancelado</option>
            </select>
            <AppDatePicker v-model="filtroDataInicio" placeholder="DD/MM/AAAA" aria-label="Data vencimento (início)" />
            <AppDatePicker v-model="filtroDataFim" placeholder="DD/MM/AAAA" aria-label="Data vencimento (fim)" />
        </section>

        <p v-if="erro" class="erro">{{ erro }}</p>
        <p v-if="carregando" class="info">Carregando...</p>

        <table v-if="!carregando && lancamentos.length > 0">
            <thead>
                <tr>
                    <th>Tipo</th>
                    <th>Descrição</th>
                    <th>Categoria</th>
                    <th>Vencimento</th>
                    <th>Valor</th>
                    <th>Status</th>
                    <th>Pagamento</th>
                    <th>Ações</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="l in lancamentos" :key="l.id"
                    :class="{ vencido: vencidoPendente(l), cancelado: l.status === 'Cancelado' }">
                    <td><span :class="['tipo-badge', tipoCor[l.tipo]]">{{ l.tipo }}</span></td>
                    <td>
                        {{ l.descricao }}
                        <small v-if="l.orcamentoNumero" class="orc-ref">· {{ l.orcamentoNumero }}</small>
                    </td>
                    <td>{{ l.categoria }}</td>
                    <td :class="{ 'vencido-text': vencidoPendente(l) }">{{ formatarData(l.dataVencimento) }}</td>
                    <td :class="tipoCor[l.tipo]">{{ moeda(l.valor) }}</td>
                    <td><span :class="['badge', statusCor[l.status]]">{{ l.status }}</span></td>
                    <td>{{ l.dataPagamento ? formatarData(l.dataPagamento) : "—" }}</td>
                    <td class="acoes">
                        <AppButton v-if="l.status === 'Pendente'" variant="success" size="sm" @click="pagar(l)">✓ Pagar</AppButton>
                        <button v-if="l.status === 'Pendente'" @click="abrirEditar(l)" class="btn-icon" title="Editar">✏</button>
                        <button v-if="l.status === 'Pendente'" @click="cancelar(l)" class="btn-icon" title="Cancelar">✕</button>
                    </td>
                </tr>
            </tbody>
        </table>
        <p v-else-if="!carregando" class="vazio">Nenhum lançamento encontrado.</p>

        <AppPagination
            v-if="total > 0"
            :pagina="pagina"
            :tamanho="tamanho"
            :total="total"
            rotulo-itens="lançamentos"
            class="paginacao"
            @update:pagina="pagina = $event"
            @update:tamanho="tamanho = $event"
        />

        <!-- Modal criar -->
        <AppModal
            :aberto="modalCriar"
            titulo="Novo lançamento"
            @fechar="modalCriar = false"
        >
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
                <AppInput v-model="formCriar.categoria" list="cats-criar" />
                <datalist id="cats-criar">
                    <option v-for="c in categoriasSugeridas" :key="c" :value="c" />
                </datalist>
            </AppField>
            <AppField label="Valor (R$)" required>
                <AppInput v-model="formCriar.valor" type="number" :min="0.01" :step="0.01" />
            </AppField>
            <AppField label="Data de vencimento" required>
                <AppDatePicker v-model="formCriar.dataVencimento" placeholder="DD/MM/AAAA" />
            </AppField>
            <p v-if="erroCriar" class="msg-erro">{{ erroCriar }}</p>

            <template #rodape>
                <AppButton variant="secondary" @click="modalCriar = false">Cancelar</AppButton>
                <AppButton :disabled="salvando" :loading="salvando" @click="salvarCriar">Criar</AppButton>
            </template>
        </AppModal>

        <!-- Modal editar -->
        <AppModal
            :aberto="!!lancamentoEditando"
            titulo="Editar lançamento"
            @fechar="lancamentoEditando = null"
        >
            <AppField label="Descrição" required>
                <AppInput v-model="formEditar.descricao" />
            </AppField>
            <AppField label="Categoria" required>
                <AppInput v-model="formEditar.categoria" list="cats-editar" />
                <datalist id="cats-editar">
                    <option v-for="c in categoriasSugeridas" :key="c" :value="c" />
                </datalist>
            </AppField>
            <AppField label="Valor (R$)" required>
                <AppInput v-model="formEditar.valor" type="number" :min="0.01" :step="0.01" />
            </AppField>
            <AppField label="Data de vencimento" required>
                <AppDatePicker v-model="formEditar.dataVencimento" placeholder="DD/MM/AAAA" />
            </AppField>
            <p v-if="erroEditar" class="msg-erro">{{ erroEditar }}</p>

            <template #rodape>
                <AppButton variant="secondary" @click="lancamentoEditando = null">Cancelar</AppButton>
                <AppButton :disabled="salvando" :loading="salvando" @click="salvarEditar">Salvar</AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.page-header {
    display: flex; justify-content: space-between; align-items: flex-start;
    margin-bottom: 1.25rem;
}
.page-titulo { font-size: 1.5rem; font-weight: 800; margin: 0 0 0.2rem; }
.page-sub    { margin: 0; color: var(--text-muted); font-size: 0.875em; }

.resumo-cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 1rem; margin-bottom: 1.5rem; }
.card-resumo { background: hsl(var(--card)); border: 1px solid #e5e7eb; border-radius: 8px; padding: 1rem 1.25rem; display: flex; flex-direction: column; gap: 0.15rem; }
.card-resumo .label { font-size: 0.85em; color: #6b7280; font-weight: 500; }
.card-resumo .valor { font-size: 1.5em; font-weight: 700; }
.card-resumo .sub { font-size: 0.8em; color: #9ca3af; }
.card-resumo.receita .valor { color: #059669; }
.card-resumo.despesa .valor { color: #dc2626; }
.card-resumo.saldo-positivo { border-left: 4px solid #059669; }
.card-resumo.saldo-positivo .valor { color: #059669; }
.card-resumo.saldo-negativo { border-left: 4px solid #dc2626; }
.card-resumo.saldo-negativo .valor { color: #dc2626; }

.filtros { display: flex; gap: 0.75rem; margin-bottom: 1rem; flex-wrap: wrap; align-items: center; }
.filtros select, .filtros input[type="date"] { padding: 0.3rem 0.6rem; border: 1px solid #ccc; border-radius: 4px; }

table { width: 100%; border-collapse: collapse; font-size: 0.9em; }
th { background: #f3f4f6; text-align: left; padding: 0.5rem 0.75rem; border-bottom: 2px solid #e5e7eb; }
td { padding: 0.5rem 0.75rem; border-bottom: 1px solid #f0f0f0; vertical-align: middle; }
tr:hover { background: #f9fafb; }
tr.vencido { background: hsl(var(--warning) / 0.10); }
tr.cancelado { opacity: 0.5; }

.tipo-badge { padding: 0.15rem 0.5rem; border-radius: 999px; font-size: 0.8em; font-weight: 600; }
.receita { color: #059669; }
.despesa { color: #dc2626; }
.tipo-badge.receita { background: #d1fae5; }
.tipo-badge.despesa { background: #fee2e2; }
.vencido-text { color: #d97706; font-weight: 600; }
.orc-ref { color: #9ca3af; font-size: 0.85em; }

.badge { padding: 0.15rem 0.5rem; border-radius: 999px; font-size: 0.8em; }
.badge-pendente { background: #fef3c7; color: #92400e; }
.badge-pago { background: #d1fae5; color: #065f46; }
.badge-cancelado { background: #f3f4f6; color: #6b7280; }

.acoes { display: flex; gap: 0.3rem; align-items: center; }
.msg-erro { color: hsl(var(--error)); font-size: 0.875em; margin: 0; }
.erro { color: #b00020; font-size: 0.9em; }
.info { color: #6b7280; }
.vazio { color: #9ca3af; font-style: italic; margin-top: 1.5rem; }
.paginacao { margin-top: 1rem; }
</style>
