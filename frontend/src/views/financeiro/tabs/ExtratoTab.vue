<script setup lang="ts">
import { ref, watch, computed } from "vue"
import { financeiroService, type KpisFinanceiro, type LancamentoExtrato, type PaginaExtrato } from "@/services/financeiroService"
import { AppButton, AppPagination, AppModal, AppField, AppInput, AppSelect, AppDatePicker, AppToast, AppConfirmDialog } from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"

// ─── Período padrão: mês atual ────────────────────────────────────────────────
function hojeStr() {
    return new Date().toISOString().split("T")[0]
}
function inicioMes() {
    const d = new Date()
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-01`
}

const dataInicio = ref(inicioMes())
const dataFim = ref(hojeStr())

// ─── KPIs ────────────────────────────────────────────────────────────────────
const kpis = ref<KpisFinanceiro | null>(null)
const carregandoKpis = ref(false)

async function carregarKpis() {
    if (!dataInicio.value || !dataFim.value) return
    carregandoKpis.value = true
    try {
        kpis.value = await financeiroService.kpis({ dataInicio: dataInicio.value, dataFim: dataFim.value })
    } finally {
        carregandoKpis.value = false
    }
}

// ─── Extrato paginado ─────────────────────────────────────────────────────────
const itens = ref<LancamentoExtrato[]>([])
const total = ref(0)
const pagina = ref(1)
const tamanho = ref(20)
const carregandoExtrato = ref(false)
const erroExtrato = ref<string | null>(null)

// Filtros — selects diretos, categoria com debounce de 300ms.
const filtroTipoInput = ref("")
const filtroCategoriaInput = ref("")
const filtroFormaPagInput = ref("")
const filtroOrigemInput = ref("")

const filtroTipo      = filtroTipoInput                       // select — sem debounce
const filtroCategoria = useDebouncedRef(filtroCategoriaInput, 300)
const filtroFormaPag  = filtroFormaPagInput
const filtroOrigem    = filtroOrigemInput

async function carregarExtrato() {
    if (!dataInicio.value || !dataFim.value) return
    carregandoExtrato.value = true
    erroExtrato.value = null
    try {
        const pg = await financeiroService.extrato({
            dataInicio: dataInicio.value,
            dataFim: dataFim.value,
            tipo: filtroTipo.value || undefined,
            categoria: filtroCategoria.value || undefined,
            formaPagamento: filtroFormaPag.value || undefined,
            origem: filtroOrigem.value || undefined,
            pagina: pagina.value,
            tamanho: tamanho.value,
        })
        itens.value = pg.itens
        total.value = pg.total
    } catch (e: any) {
        erroExtrato.value = e?.response?.data?.mensagem ?? "Erro ao carregar extrato."
    } finally {
        carregandoExtrato.value = false
    }
}

// Reset página ao mudar filtros.
watch([filtroTipoInput, filtroCategoria, filtroFormaPagInput, filtroOrigemInput, dataInicio, dataFim], () => {
    pagina.value = 1
})
watch([pagina, tamanho, filtroTipoInput, filtroCategoria, filtroFormaPagInput, filtroOrigemInput, dataInicio, dataFim],
    () => { carregarExtrato(); carregarKpis() },
    { immediate: true }
)

// ─── Modal lançamento avulso ──────────────────────────────────────────────────
const modalCriar = ref(false)
const formCriar = ref({ tipo: "Receita", descricao: "", valor: 0, dataVencimento: "", categoria: "" })
const erroCriar = ref<string | null>(null)
const salvando = ref(false)

const lancamentoEditando = ref<LancamentoExtrato | null>(null)
const formEditar = ref({ descricao: "", valor: 0, dataVencimento: "", categoria: "" })
const erroEditar = ref<string | null>(null)

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

async function salvarEditar() {
    if (!lancamentoEditando.value) return
    salvando.value = true; erroEditar.value = null
    try {
        await financeiroService.atualizar(lancamentoEditando.value.id, {
            descricao: formEditar.value.descricao,
            valor: formEditar.value.valor,
            dataVencimento: formEditar.value.dataVencimento,
            categoria: formEditar.value.categoria,
        })
        lancamentoEditando.value = null
        await carregarExtrato()
    } catch (e: any) {
        erroEditar.value = e?.response?.data?.mensagem ?? "Erro ao atualizar."
    } finally {
        salvando.value = false
    }
}

// ─── Pagar / Cancelar ─────────────────────────────────────────────────────────
const toast = ref<{ mensagem: string; variante: "success" | "error" } | null>(null)
const confPagar = ref<{ aberto: boolean; alvo: LancamentoExtrato | null; exec: boolean }>({ aberto: false, alvo: null, exec: false })
const confCancelar = ref<{ aberto: boolean; alvo: LancamentoExtrato | null; exec: boolean }>({ aberto: false, alvo: null, exec: false })

async function executarPagar() {
    const alvo = confPagar.value.alvo; if (!alvo) return
    confPagar.value.exec = true
    try {
        await financeiroService.pagar(alvo.id)
        confPagar.value = { aberto: false, alvo: null, exec: false }
        toast.value = { mensagem: "Pagamento registrado.", variante: "success" }
        await Promise.all([carregarExtrato(), carregarKpis()])
    } catch (e: any) {
        confPagar.value.exec = false
        toast.value = { mensagem: e?.response?.data?.mensagem ?? "Erro ao pagar.", variante: "error" }
    }
}

async function executarCancelar() {
    const alvo = confCancelar.value.alvo; if (!alvo) return
    confCancelar.value.exec = true
    try {
        await financeiroService.cancelar(alvo.id)
        confCancelar.value = { aberto: false, alvo: null, exec: false }
        toast.value = { mensagem: "Lançamento cancelado.", variante: "success" }
        await carregarExtrato()
    } catch (e: any) {
        confCancelar.value.exec = false
        toast.value = { mensagem: e?.response?.data?.mensagem ?? "Erro ao cancelar.", variante: "error" }
    }
}

// ─── Helpers ──────────────────────────────────────────────────────────────────
function moeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}
function formatarData(s: string | null) {
    if (!s) return "—"
    const [y, m, d] = s.split("T")[0].split("-")
    return `${d}/${m}/${y}`
}
const vencido = (l: LancamentoExtrato) =>
    l.status === "Pendente" && l.dataVencimento && new Date(l.dataVencimento) < new Date()
</script>

<template>
    <div class="extrato-tab">
        <!-- Período + botão novo lançamento -->
        <div class="periodo-row">
            <AppDatePicker v-model="dataInicio" aria-label="Data início" />
            <span class="sep">até</span>
            <AppDatePicker v-model="dataFim" aria-label="Data fim" />
            <AppButton icon="fa-solid fa-plus" @click="abrirModalCriar">
                Novo lançamento
            </AppButton>
        </div>

        <!-- KPIs -->
        <section class="kpis-grid" aria-label="KPIs do período">
            <div class="kpi-card receita">
                <span class="kpi-label">Recebido</span>
                <span class="kpi-valor">{{ kpis ? moeda(kpis.recebido) : "—" }}</span>
                <span class="kpi-sub">A receber: {{ kpis ? moeda(kpis.aReceber) : "—" }}</span>
            </div>
            <div class="kpi-card despesa">
                <span class="kpi-label">Despesas</span>
                <span class="kpi-valor">{{ kpis ? moeda(kpis.despesas) : "—" }}</span>
            </div>
            <div class="kpi-card" :class="(kpis?.saldo ?? 0) >= 0 ? 'saldo-pos' : 'saldo-neg'">
                <span class="kpi-label">Saldo</span>
                <span class="kpi-valor">{{ kpis ? moeda(kpis.saldo) : "—" }}</span>
                <span class="kpi-sub">Estornos: {{ kpis ? moeda(kpis.estornos) : "—" }}</span>
            </div>
            <div class="kpi-card neutro">
                <span class="kpi-label">Descontos</span>
                <span class="kpi-valor">{{ kpis ? moeda(kpis.descontosConcedidos) : "—" }}</span>
                <span class="kpi-sub">Taxas: {{ kpis ? moeda(kpis.taxasCartao) : "—" }}</span>
            </div>
        </section>

        <!-- Filtros do extrato -->
        <div class="filtros-extrato">
            <select v-model="filtroTipoInput" aria-label="Tipo">
                <option value="">Todos os tipos</option>
                <option value="Receita">Receita</option>
                <option value="Despesa">Despesa</option>
            </select>
            <select v-model="filtroOrigemInput" aria-label="Origem">
                <option value="">Toda origem</option>
                <option value="Consulta">Consulta</option>
                <option value="Procedimento">Procedimento</option>
                <option value="Cirurgia">Cirurgia</option>
            </select>
            <AppInput v-model="filtroCategoriaInput" placeholder="Categoria..." style="max-width:180px" />
            <AppInput v-model="filtroFormaPagInput" placeholder="Forma de pagamento..." style="max-width:200px" />
        </div>

        <p v-if="erroExtrato" class="msg-erro">{{ erroExtrato }}</p>
        <p v-if="carregandoExtrato" class="info">Carregando...</p>

        <!-- Tabela -->
        <table v-if="!carregandoExtrato && itens.length > 0" class="extrato-table">
            <thead>
                <tr>
                    <th>Tipo</th>
                    <th>Descrição</th>
                    <th>Paciente</th>
                    <th>Categoria</th>
                    <th>Forma</th>
                    <th>Data</th>
                    <th>Valor</th>
                    <th>Status</th>
                    <th>Ações</th>
                </tr>
            </thead>
            <tbody>
                <tr
                    v-for="l in itens"
                    :key="l.id"
                    :class="{ vencido: vencido(l), cancelado: l.status === 'Cancelado' }"
                >
                    <td>
                        <span :class="['tipo-badge', l.tipo.toLowerCase()]">{{ l.tipo }}</span>
                    </td>
                    <td>{{ l.descricao }}</td>
                    <td>{{ l.pacienteNome ?? "—" }}</td>
                    <td>{{ l.categoria }}</td>
                    <td>{{ l.formaPagamento ?? "—" }}</td>
                    <td>{{ formatarData(l.dataPagamento ?? l.dataVencimento) }}</td>
                    <td :class="l.tipo.toLowerCase()">{{ moeda(l.valor) }}</td>
                    <td>
                        <span :class="['badge', `badge-${l.status.toLowerCase()}`]">{{ l.status }}</span>
                    </td>
                    <td class="acoes">
                        <AppButton
                            v-if="l.status === 'Pendente'"
                            variant="success"
                            size="sm"
                            @click="confPagar = { aberto: true, alvo: l, exec: false }"
                        >Pagar</AppButton>
                        <button
                            v-if="l.status === 'Pendente'"
                            class="btn-icon btn-icon-editar"
                            title="Editar"
                            @click="lancamentoEditando = l; formEditar = { descricao: l.descricao, valor: l.valor, dataVencimento: l.dataVencimento ?? '', categoria: l.categoria }"
                        />
                        <button
                            v-if="l.status === 'Pendente'"
                            class="btn-icon btn-icon-excluir"
                            title="Cancelar"
                            @click="confCancelar = { aberto: true, alvo: l, exec: false }"
                        />
                    </td>
                </tr>
            </tbody>
        </table>
        <p v-else-if="!carregandoExtrato" class="vazio">Nenhum lançamento no período.</p>

        <AppPagination
            v-if="total > 0"
            v-model:pagina="pagina"
            v-model:tamanho="tamanho"
            :total="total"
            rotulo-itens="lançamentos"
        />
    </div>

    <!-- Modal criar -->
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

    <!-- Modal editar -->
    <AppModal :aberto="!!lancamentoEditando" titulo="Editar lançamento" @fechar="lancamentoEditando = null">
        <AppField label="Descrição" required>
            <AppInput v-model="formEditar.descricao" />
        </AppField>
        <AppField label="Categoria" required>
            <AppInput v-model="formEditar.categoria" />
        </AppField>
        <AppField label="Valor (R$)" required>
            <AppInput v-model="formEditar.valor" type="number" :min="0.01" :step="0.01" />
        </AppField>
        <AppField label="Vencimento" required>
            <AppDatePicker v-model="formEditar.dataVencimento" placeholder="DD/MM/AAAA" />
        </AppField>
        <p v-if="erroEditar" class="msg-erro">{{ erroEditar }}</p>
        <template #rodape>
            <AppButton variant="secondary" @click="lancamentoEditando = null">Cancelar</AppButton>
            <AppButton :disabled="salvando" :loading="salvando" @click="salvarEditar">Salvar</AppButton>
        </template>
    </AppModal>

    <AppConfirmDialog
        v-model:aberto="confPagar.aberto"
        titulo="Confirmar pagamento?"
        :mensagem="confPagar.alvo ? `Baixar '${confPagar.alvo.descricao}' como pago?` : ''"
        confirmar-rotulo="Confirmar pagamento"
        variante="danger"
        :executando="confPagar.exec"
        @confirmar="executarPagar"
    />
    <AppConfirmDialog
        v-model:aberto="confCancelar.aberto"
        titulo="Cancelar lançamento?"
        :mensagem="confCancelar.alvo ? `Cancelar '${confCancelar.alvo.descricao}'?` : ''"
        confirmar-rotulo="Cancelar"
        variante="danger"
        :executando="confCancelar.exec"
        @confirmar="executarCancelar"
    />

    <AppToast v-if="toast" :mensagem="toast.mensagem" :variante="toast.variante" @fechar="toast = null" />
</template>

<style scoped>
.extrato-tab { display: flex; flex-direction: column; gap: 1rem; }

.periodo-row {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
}
.sep { color: hsl(var(--muted-foreground)); font-size: var(--text-sm); }

.kpis-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 1rem;
}
.kpi-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 8px;
    padding: 1rem 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
}
.kpi-label { font-size: var(--text-sm); color: hsl(var(--muted-foreground)); font-weight: var(--font-weight-medium); }
.kpi-valor { font-size: var(--text-xl); font-weight: var(--font-weight-bold); }
.kpi-sub   { font-size: var(--text-xs); color: hsl(var(--muted-foreground)); }
.kpi-card.receita .kpi-valor   { color: hsl(var(--success)); }
.kpi-card.despesa .kpi-valor   { color: hsl(var(--destructive)); }
.kpi-card.saldo-pos .kpi-valor { color: hsl(var(--success)); }
.kpi-card.saldo-neg .kpi-valor { color: hsl(var(--destructive)); }

.filtros-extrato {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
    align-items: center;
}
.filtros-extrato select {
    padding: 0.35rem 0.6rem;
    border: 1px solid hsl(var(--border));
    border-radius: 6px;
    font-size: var(--text-sm);
    background: hsl(var(--background));
    color: hsl(var(--foreground));
}

.extrato-table { width: 100%; border-collapse: collapse; font-size: var(--text-sm); }
.extrato-table th {
    background: hsl(var(--muted));
    text-align: left;
    padding: 0.5rem 0.75rem;
    border-bottom: 2px solid hsl(var(--border));
    font-weight: var(--font-weight-semibold);
}
.extrato-table td { padding: 0.5rem 0.75rem; border-bottom: 1px solid hsl(var(--border)); vertical-align: middle; }
.extrato-table tr:hover { background: hsl(var(--muted) / 0.4); }
.extrato-table tr.vencido { background: hsl(var(--warning) / 0.1); }
.extrato-table tr.cancelado { opacity: 0.5; }

.tipo-badge { padding: 0.15rem 0.5rem; border-radius: 999px; font-size: var(--text-xs); font-weight: var(--font-weight-semibold); }
.tipo-badge.receita { background: hsl(var(--success) / 0.15); color: hsl(var(--success)); }
.tipo-badge.despesa { background: hsl(var(--destructive) / 0.15); color: hsl(var(--destructive)); }
.receita { color: hsl(var(--success)); }
.despesa { color: hsl(var(--destructive)); }

.badge { padding: 0.15rem 0.5rem; border-radius: 999px; font-size: var(--text-xs); }
.badge-pendente { background: hsl(var(--warning) / 0.15); color: hsl(var(--warning)); }
.badge-pago     { background: hsl(var(--success) / 0.15); color: hsl(var(--success)); }
.badge-cancelado{ background: hsl(var(--muted)); color: hsl(var(--muted-foreground)); }

.acoes { display: flex; gap: 0.3rem; align-items: center; }
.msg-erro { color: hsl(var(--destructive)); font-size: var(--text-sm); margin: 0; }
.info, .vazio { color: hsl(var(--muted-foreground)); font-size: var(--text-sm); }
</style>
