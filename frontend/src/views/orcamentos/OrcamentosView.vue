<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { orcamentoService, type OrcamentoResumo, type Orcamento, type ItemPayload } from "@/services/orcamentoService"
import { pacienteService } from "@/services/pacienteService"
import { useOrcamentoPdf } from "@/composables/useOrcamentoPdf"
import { AppButton, AppField, AppInput, AppModal, AppSelect, AppTextarea } from "@/components/ui"

const { gerarPdf } = useOrcamentoPdf()

type PacienteOpcao = { id: number; nome: string }

const orcamentos = ref<OrcamentoResumo[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)

const filtroStatus = ref("")
const filtroPacienteId = ref<number | null>(null)

// --- Pacientes (para seleção) ---
const pacientes = ref<PacienteOpcao[]>([])
const buscaPaciente = ref("")
const pacientesFiltrados = computed(() =>
    pacientes.value.filter(p =>
        p.nome.toLowerCase().includes(buscaPaciente.value.toLowerCase())
    ).slice(0, 20)
)

// --- Modal criar ---
const modalCriar = ref(false)
const formCriar = ref({
    pacienteId: null as number | null,
    validade: "",
    observacoes: "",
    itens: [{ descricao: "", quantidade: 1, valorUnitario: 0, descontoPercent: 0 }] as ItemPayload[]
})
const erroCriar = ref<string | null>(null)
const salvando = ref(false)

// --- Modal detalhe / edição ---
const orcamentoDetalhe = ref<Orcamento | null>(null)
const modoEditar = ref(false)
const formEditar = ref({
    validade: "",
    observacoes: "",
    itens: [] as ItemPayload[]
})
const erroEditar = ref<string | null>(null)

const totalCriar = computed(() =>
    formCriar.value.itens.reduce((acc, i) =>
        acc + i.quantidade * i.valorUnitario * (1 - i.descontoPercent / 100), 0)
)

const totalEditar = computed(() =>
    formEditar.value.itens.reduce((acc, i) =>
        acc + i.quantidade * i.valorUnitario * (1 - i.descontoPercent / 100), 0)
)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        orcamentos.value = await orcamentoService.listar({
            status: filtroStatus.value || undefined,
            pacienteId: filtroPacienteId.value ?? undefined,
        })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar orçamentos."
    } finally {
        carregando.value = false
    }
}

onMounted(async () => {
    await carregar()
    const pg = await pacienteService.listar(undefined, 1, 500)
    pacientes.value = pg.itens.map(p => ({ id: p.id, nome: p.nomeCompleto }))
})

// --- Criar ---
function abrirModalCriar() {
    formCriar.value = {
        pacienteId: null,
        validade: "",
        observacoes: "",
        itens: [{ descricao: "", quantidade: 1, valorUnitario: 0, descontoPercent: 0 }]
    }
    erroCriar.value = null
    buscaPaciente.value = ""
    modalCriar.value = true
}

function adicionarItemCriar() {
    formCriar.value.itens.push({ descricao: "", quantidade: 1, valorUnitario: 0, descontoPercent: 0 })
}

function removerItemCriar(idx: number) {
    formCriar.value.itens.splice(idx, 1)
}

async function salvarCriar() {
    if (!formCriar.value.pacienteId) { erroCriar.value = "Selecione um paciente."; return }
    if (!formCriar.value.validade) { erroCriar.value = "Informe a validade."; return }
    salvando.value = true
    erroCriar.value = null
    try {
        await orcamentoService.criar({
            pacienteId: formCriar.value.pacienteId,
            validade: formCriar.value.validade,
            observacoes: formCriar.value.observacoes || null,
            itens: formCriar.value.itens,
        })
        modalCriar.value = false
        await carregar()
    } catch (e: any) {
        erroCriar.value = e?.response?.data?.mensagem ?? "Erro ao criar orçamento."
    } finally {
        salvando.value = false
    }
}

// --- Detalhe / Editar ---
async function verDetalhe(orc: OrcamentoResumo) {
    try {
        orcamentoDetalhe.value = await orcamentoService.obter(orc.id)
        modoEditar.value = false
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao carregar orçamento.")
    }
}

function entrarModoEditar() {
    if (!orcamentoDetalhe.value) return
    formEditar.value = {
        validade: orcamentoDetalhe.value.validade,
        observacoes: orcamentoDetalhe.value.observacoes ?? "",
        itens: orcamentoDetalhe.value.itens.map(i => ({
            descricao: i.descricao,
            quantidade: i.quantidade,
            valorUnitario: i.valorUnitario,
            descontoPercent: i.descontoPercent,
        }))
    }
    erroEditar.value = null
    modoEditar.value = true
}

function adicionarItemEditar() {
    formEditar.value.itens.push({ descricao: "", quantidade: 1, valorUnitario: 0, descontoPercent: 0 })
}

function removerItemEditar(idx: number) {
    formEditar.value.itens.splice(idx, 1)
}

async function salvarEditar() {
    if (!orcamentoDetalhe.value) return
    salvando.value = true
    erroEditar.value = null
    try {
        await orcamentoService.atualizar(orcamentoDetalhe.value.id, {
            validade: formEditar.value.validade,
            observacoes: formEditar.value.observacoes || null,
            itens: formEditar.value.itens,
        })
        orcamentoDetalhe.value = await orcamentoService.obter(orcamentoDetalhe.value.id)
        modoEditar.value = false
        await carregar()
    } catch (e: any) {
        erroEditar.value = e?.response?.data?.mensagem ?? "Erro ao salvar."
    } finally {
        salvando.value = false
    }
}

// --- Ações de status ---
async function aprovar(orc: OrcamentoResumo) {
    if (!confirm(`Aprovar orçamento ${orc.numero}?`)) return
    try {
        await orcamentoService.aprovar(orc.id)
        if (orcamentoDetalhe.value?.id === orc.id)
            orcamentoDetalhe.value = await orcamentoService.obter(orc.id)
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao aprovar.")
    }
}

async function recusar(orc: OrcamentoResumo) {
    if (!confirm(`Recusar orçamento ${orc.numero}?`)) return
    try {
        await orcamentoService.recusar(orc.id)
        if (orcamentoDetalhe.value?.id === orc.id)
            orcamentoDetalhe.value = await orcamentoService.obter(orc.id)
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao recusar.")
    }
}

// --- Helpers ---
const statusCor: Record<string, string> = {
    Pendente: "badge-pendente",
    Aprovado: "badge-aprovado",
    Recusado: "badge-recusado",
    Expirado: "badge-expirado",
}

function formatarMoeda(n: number) {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function formatarData(s: string) {
    return new Date(s).toLocaleDateString("pt-BR")
}

function formatarDataHora(s: string) {
    return new Date(s).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" })
}
</script>

<template>
    <main class="app-page orcamentos">
        <header class="page-header">
            <div>
                <h1 class="page-titulo">Orçamentos</h1>
                <p class="page-sub">Gere e acompanhe orçamentos por paciente e estabelecimento.</p>
            </div>
            <AppButton icon="fa-solid fa-plus" @click="abrirModalCriar">Novo orçamento</AppButton>
        </header>

        <!-- Card de filtros (estilo legado) -->
        <section class="filtros-card">
            <div class="filtros-grid">
                <div class="filtro-grupo">
                    <label class="campo-label">Status</label>
                    <select v-model="filtroStatus" class="input-field" @change="carregar">
                        <option value="">Todos os status</option>
                        <option value="Pendente">Pendente</option>
                        <option value="Aprovado">Aprovado</option>
                        <option value="Recusado">Recusado</option>
                        <option value="Expirado">Expirado</option>
                    </select>
                </div>
            </div>
        </section>

        <p v-if="erro" class="erro">{{ erro }}</p>
        <p v-if="carregando" class="info">Carregando...</p>

        <!-- Lista de orçamentos -->
        <table v-if="!carregando && orcamentos.length > 0">
            <thead>
                <tr>
                    <th>Número</th>
                    <th>Paciente</th>
                    <th>Status</th>
                    <th>Validade</th>
                    <th>Total</th>
                    <th>Criado em</th>
                    <th>Ações</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="orc in orcamentos" :key="orc.id">
                    <td>
                        <button class="link-btn" @click="verDetalhe(orc)">{{ orc.numero }}</button>
                    </td>
                    <td>{{ orc.pacienteNome }}</td>
                    <td><span :class="['badge', statusCor[orc.status]]">{{ orc.status }}</span></td>
                    <td>{{ formatarData(orc.validade) }}</td>
                    <td>{{ formatarMoeda(orc.total) }}</td>
                    <td>{{ formatarDataHora(orc.criadoEm) }}</td>
                    <td class="acoes">
                        <AppButton v-if="orc.status === 'Pendente'" variant="success" size="sm" @click="aprovar(orc)">✓ Aprovar</AppButton>
                        <AppButton v-if="orc.status === 'Pendente'" variant="danger" size="sm" @click="recusar(orc)">✕ Recusar</AppButton>
                    </td>
                </tr>
            </tbody>
        </table>
        <p v-else-if="!carregando" class="vazio">Nenhum orçamento encontrado.</p>

        <!-- Modal criar orçamento -->
        <AppModal :aberto="modalCriar" titulo="Novo orçamento" largura="lg" @fechar="modalCriar = false">
            <AppField label="Paciente" required>
                <AppInput v-model="buscaPaciente" placeholder="Buscar paciente..." />
                <select v-model="formCriar.pacienteId" size="4" class="lista-select">
                    <option v-for="p in pacientesFiltrados" :key="p.id" :value="p.id">{{ p.nome }}</option>
                </select>
            </AppField>
            <AppField label="Validade" required>
                <AppInput v-model="formCriar.validade" type="date" />
            </AppField>
            <AppField label="Observações">
                <AppTextarea v-model="formCriar.observacoes" :rows="2" />
            </AppField>

            <div class="itens-header">
                <strong>Itens</strong>
                <AppButton type="button" variant="secondary" size="sm" @click="adicionarItemCriar">+ Adicionar</AppButton>
            </div>
            <table class="itens-table">
                <thead>
                    <tr><th>Descrição</th><th>Qtd.</th><th>Valor unit.</th><th>Desc. %</th><th>Subtotal</th><th></th></tr>
                </thead>
                <tbody>
                    <tr v-for="(item, idx) in formCriar.itens" :key="idx">
                        <td><input v-model="item.descricao" required /></td>
                        <td><input type="number" v-model.number="item.quantidade" min="0.001" step="0.001" style="width:70px" /></td>
                        <td><input type="number" v-model.number="item.valorUnitario" min="0" step="0.01" style="width:100px" /></td>
                        <td><input type="number" v-model.number="item.descontoPercent" min="0" max="100" step="0.1" style="width:70px" /></td>
                        <td>{{ formatarMoeda(item.quantidade * item.valorUnitario * (1 - item.descontoPercent / 100)) }}</td>
                        <td><button type="button" class="btn-icon" @click="removerItemCriar(idx)" :disabled="formCriar.itens.length <= 1">✕</button></td>
                    </tr>
                </tbody>
                <tfoot>
                    <tr>
                        <td colspan="4" style="text-align:right"><strong>Total:</strong></td>
                        <td><strong>{{ formatarMoeda(totalCriar) }}</strong></td>
                        <td></td>
                    </tr>
                </tfoot>
            </table>
            <p v-if="erroCriar" class="msg-erro">{{ erroCriar }}</p>

            <template #rodape>
                <AppButton variant="secondary" @click="modalCriar = false">Cancelar</AppButton>
                <AppButton :disabled="salvando" :loading="salvando" @click="salvarCriar">Criar</AppButton>
            </template>
        </AppModal>

        <!-- Modal detalhe / edição -->
        <AppModal
            :aberto="!!orcamentoDetalhe"
            largura="lg"
            @fechar="orcamentoDetalhe = null; modoEditar = false"
        >
            <template #titulo>
                <div class="detalhe-header">
                    <span>{{ orcamentoDetalhe?.numero }}</span>
                    <span v-if="orcamentoDetalhe" :class="['badge', statusCor[orcamentoDetalhe.status]]">{{ orcamentoDetalhe.status }}</span>
                </div>
            </template>

            <div v-if="orcamentoDetalhe && !modoEditar">
                <p><strong>Paciente:</strong> {{ orcamentoDetalhe.pacienteNome }}</p>
                <p><strong>Validade:</strong> {{ formatarData(orcamentoDetalhe.validade) }}</p>
                <p v-if="orcamentoDetalhe.observacoes"><strong>Observações:</strong> {{ orcamentoDetalhe.observacoes }}</p>
                <table class="itens-table">
                    <thead>
                        <tr><th>Descrição</th><th>Qtd.</th><th>Valor unit.</th><th>Desc. %</th><th>Subtotal</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in orcamentoDetalhe.itens" :key="item.id">
                            <td>{{ item.descricao }}</td>
                            <td>{{ item.quantidade }}</td>
                            <td>{{ formatarMoeda(item.valorUnitario) }}</td>
                            <td>{{ item.descontoPercent }}%</td>
                            <td>{{ formatarMoeda(item.subtotal) }}</td>
                        </tr>
                    </tbody>
                    <tfoot>
                        <tr>
                            <td colspan="4" style="text-align:right"><strong>Total:</strong></td>
                            <td><strong>{{ formatarMoeda(orcamentoDetalhe.total) }}</strong></td>
                        </tr>
                    </tfoot>
                </table>
            </div>

            <form v-else-if="orcamentoDetalhe && modoEditar" @submit.prevent="salvarEditar">
                <AppField label="Validade" required>
                    <AppInput v-model="formEditar.validade" type="date" />
                </AppField>
                <AppField label="Observações">
                    <AppTextarea v-model="formEditar.observacoes" :rows="2" />
                </AppField>
                <div class="itens-header">
                    <strong>Itens</strong>
                    <AppButton type="button" variant="secondary" size="sm" @click="adicionarItemEditar">+ Adicionar</AppButton>
                </div>
                <table class="itens-table">
                    <thead>
                        <tr><th>Descrição</th><th>Qtd.</th><th>Valor unit.</th><th>Desc. %</th><th>Subtotal</th><th></th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="(item, idx) in formEditar.itens" :key="idx">
                            <td><input v-model="item.descricao" required /></td>
                            <td><input type="number" v-model.number="item.quantidade" min="0.001" step="0.001" style="width:70px" /></td>
                            <td><input type="number" v-model.number="item.valorUnitario" min="0" step="0.01" style="width:100px" /></td>
                            <td><input type="number" v-model.number="item.descontoPercent" min="0" max="100" step="0.1" style="width:70px" /></td>
                            <td>{{ formatarMoeda(item.quantidade * item.valorUnitario * (1 - item.descontoPercent / 100)) }}</td>
                            <td><button type="button" class="btn-icon" @click="removerItemEditar(idx)" :disabled="formEditar.itens.length <= 1">✕</button></td>
                        </tr>
                    </tbody>
                    <tfoot>
                        <tr>
                            <td colspan="4" style="text-align:right"><strong>Total:</strong></td>
                            <td><strong>{{ formatarMoeda(totalEditar) }}</strong></td>
                            <td></td>
                        </tr>
                    </tfoot>
                </table>
                <p v-if="erroEditar" class="msg-erro">{{ erroEditar }}</p>
            </form>

            <template #rodape>
                <template v-if="!modoEditar">
                    <AppButton variant="secondary" @click="orcamentoDetalhe = null">Fechar</AppButton>
                    <AppButton variant="secondary" size="sm" icon="fa-solid fa-download" @click="gerarPdf(orcamentoDetalhe!)">PDF</AppButton>
                    <AppButton v-if="orcamentoDetalhe?.status === 'Pendente'" variant="secondary" @click="entrarModoEditar">Editar</AppButton>
                    <AppButton v-if="orcamentoDetalhe?.status === 'Pendente'" variant="success" @click="aprovar(orcamentoDetalhe!)">✓ Aprovar</AppButton>
                    <AppButton v-if="orcamentoDetalhe?.status === 'Pendente'" variant="danger" @click="recusar(orcamentoDetalhe!)">✕ Recusar</AppButton>
                </template>
                <template v-else>
                    <AppButton variant="secondary" @click="modoEditar = false">Cancelar</AppButton>
                    <AppButton :disabled="salvando" :loading="salvando" @click="salvarEditar">Salvar</AppButton>
                </template>
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

.filtros-card {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 1rem 1.25rem; margin-bottom: 1.25rem;
}
.filtros-grid { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); }
.filtro-grupo { display: flex; flex-direction: column; gap: 0.25rem; }

.campo-label { font-size: 0.78em; font-weight: 600; color: var(--text-muted); }

.input-field {
    padding: 0.45rem 0.75rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.875em;
    background: var(--bg-card); color: var(--text);
}
.input-field:focus { outline: none; border-color: var(--primary); }

table { width: 100%; border-collapse: collapse; font-size: 0.9em; }
th { background: #f3f4f6; text-align: left; padding: 0.5rem 0.75rem; border-bottom: 2px solid #e5e7eb; }
td { padding: 0.5rem 0.75rem; border-bottom: 1px solid #f0f0f0; vertical-align: middle; }
tr:hover { background: #f9fafb; }

.badge { padding: 0.15rem 0.5rem; border-radius: 999px; font-size: 0.8em; }
.badge-pendente { background: #fef3c7; color: #92400e; }
.badge-aprovado { background: #d1fae5; color: #065f46; }
.badge-recusado { background: #fee2e2; color: #991b1b; }
.badge-expirado { background: #f3f4f6; color: #6b7280; }

.acoes { display: flex; gap: 0.4rem; align-items: center; }
.link-btn { background: none; border: none; color: var(--primary); cursor: pointer; font-size: 0.9em; text-decoration: underline; }

.detalhe-header { display: flex; align-items: center; gap: 0.75rem; }
.lista-select { width: 100%; height: 80px; border: 1px solid #ccc; border-radius: 4px; margin-top: 0.25rem; }

.itens-header { display: flex; justify-content: space-between; align-items: center; margin: 1rem 0 0.5rem; }
.itens-table { width: 100%; border-collapse: collapse; font-size: 0.85em; margin-bottom: 1rem; }
.itens-table th { background: #f3f4f6; padding: 0.4rem 0.5rem; border-bottom: 1px solid #e5e7eb; }
.itens-table td { padding: 0.3rem 0.4rem; border-bottom: 1px solid #f0f0f0; }
.itens-table input { width: 100%; padding: 0.25rem 0.4rem; border: 1px solid #d1d5db; border-radius: 3px; }

.msg-erro { color: hsl(var(--error)); font-size: 0.875em; margin: 0; }
.erro { color: #b00020; font-size: 0.9em; }
.info { color: #6b7280; }
.vazio { color: #9ca3af; font-style: italic; margin-top: 1.5rem; }
</style>
