<script setup lang="ts">
/**
 * OrcamentoListaView — paridade UX legado:
 *   - Header com botão "+ Novo orçamento" no canto direito
 *   - Sub-tabs: Lista de orçamentos / Configurações
 *   - Filtros: paciente, vencimento de/até, ordenar por
 *   - Lista paginada
 */
import { ref, computed, watch, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import {
    AppPageHeader, AppButton, AppSelect, AppInput, AppBadge, AppTabs,
    AppEmptyState, AppPagination, AppCard, AppModal, AppField,
} from "@/components/ui"
import { orcamentoService, type OrcamentoResumo, type OrcamentoStatus } from "@/services/orcamentoService"
import { pacienteService, type PacienteListaItem } from "@/services/pacienteService"

const route = useRoute()
const router = useRouter()

// ── Estado base
const orcamentos = ref<OrcamentoResumo[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)
const pacientes = ref<PacienteListaItem[]>([])

// ── Filtros (paridade legado)
const pacienteFiltro = ref<number | "">(Number(route.query.pacienteId) || "")
const statusFiltro = ref<OrcamentoStatus | "">("")
const vencimentoDe = ref<string>("")
const vencimentoAte = ref<string>("")
type Ordenacao = "vencimento_proximo" | "vencimento_distante" | "criado_recente" | "criado_antigo" | "total_maior" | "total_menor"
const ordenacao = ref<Ordenacao>("vencimento_proximo")

const pagina = ref(1)
const tamanho = ref(20)

// ── Modal "Novo orçamento"
const modalNovoAberto = ref(false)
const novoPacienteId = ref<number | "">("")
const novoValidade = ref<string>(formatarData(new Date(Date.now() + 30 * 86400_000)))
const criandoNovo = ref(false)

const STATUS_OPTIONS: { value: OrcamentoStatus | ""; label: string }[] = [
    { value: "", label: "Todos os status" },
    { value: "Rascunho", label: "Rascunho" },
    { value: "Enviado", label: "Enviado" },
    { value: "Aprovado", label: "Aprovado" },
    { value: "Recusado", label: "Recusado" },
    { value: "Cancelado", label: "Cancelado" },
    { value: "Expirado", label: "Expirado" },
]

const ORDENACAO_OPTIONS: { value: Ordenacao; label: string }[] = [
    { value: "vencimento_proximo", label: "Próximos a vencer" },
    { value: "vencimento_distante", label: "Vencimento mais distante" },
    { value: "criado_recente", label: "Mais recentes" },
    { value: "criado_antigo", label: "Mais antigos" },
    { value: "total_maior", label: "Maior valor" },
    { value: "total_menor", label: "Menor valor" },
]

const abas = [
    { valor: "lista", label: "Lista de orçamentos" },
    { valor: "config", label: "Configurações" },
]
const aba = ref<"lista" | "config">("lista")

// Configurações é uma rota separada (já existe). Ao clicar na sub-tab, navegamos para lá.
watch(aba, (v) => {
    if (v === "config") router.push({ name: "OrcamentoSettings" })
})

function formatarData(d: Date) {
    return d.toISOString().slice(0, 10)
}

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        orcamentos.value = await orcamentoService.listar({
            pacienteId: pacienteFiltro.value === "" ? undefined : Number(pacienteFiltro.value),
            status: statusFiltro.value || undefined,
        })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar orçamentos."
    } finally {
        carregando.value = false
    }
}

async function carregarPacientes() {
    try {
        const r = await pacienteService.listar(undefined, 1, 500)
        pacientes.value = r.itens
    } catch {
        // Lista de pacientes não-crítica para a tela funcionar.
    }
}

// ── Filtragem & ordenação client-side (vencimento de/até + ordenação).
const filtrados = computed(() => {
    let arr = [...orcamentos.value]
    if (vencimentoDe.value) {
        const de = vencimentoDe.value
        arr = arr.filter(o => o.validade >= de)
    }
    if (vencimentoAte.value) {
        const ate = vencimentoAte.value
        arr = arr.filter(o => o.validade <= ate)
    }
    arr.sort((a, b) => {
        switch (ordenacao.value) {
            case "vencimento_proximo":  return a.validade.localeCompare(b.validade)
            case "vencimento_distante": return b.validade.localeCompare(a.validade)
            case "criado_recente":      return b.criadoEm.localeCompare(a.criadoEm)
            case "criado_antigo":       return a.criadoEm.localeCompare(b.criadoEm)
            case "total_maior":         return b.total - a.total
            case "total_menor":         return a.total - b.total
        }
    })
    return arr
})

const total = computed(() => filtrados.value.length)
const paginados = computed(() => {
    const inicio = (pagina.value - 1) * tamanho.value
    return filtrados.value.slice(inicio, inicio + tamanho.value)
})

watch([pacienteFiltro, statusFiltro], () => { pagina.value = 1; void carregar() })
watch([vencimentoDe, vencimentoAte, ordenacao], () => { pagina.value = 1 })

function limparFiltros() {
    pacienteFiltro.value = ""
    statusFiltro.value = ""
    vencimentoDe.value = ""
    vencimentoAte.value = ""
    ordenacao.value = "vencimento_proximo"
}

function abrir(o: OrcamentoResumo) {
    router.push({ name: "OrcamentoDetalhe", params: { id: String(o.id) } })
}

// ── Novo orçamento
function abrirModalNovo() {
    novoPacienteId.value = pacienteFiltro.value || ""
    novoValidade.value = formatarData(new Date(Date.now() + 30 * 86400_000))
    modalNovoAberto.value = true
}

async function criarNovo() {
    if (!novoPacienteId.value || !novoValidade.value) return
    criandoNovo.value = true
    erro.value = null
    try {
        // Domain exige ao menos uma coleção — criamos com cirurgia placeholder
        // que o usuário ajusta no Form. Status inicial é Rascunho.
        const r = await orcamentoService.criar({
            pacienteId: Number(novoPacienteId.value),
            validade: novoValidade.value,
            cirurgias: [{
                procedimentoCirurgicoId: null,
                descricao: "Cirurgia a definir",
                quantidade: 1,
                duracaoMinutos: null,
                valorTotal: 0,
            }],
        })
        modalNovoAberto.value = false
        router.push({ name: "OrcamentoForm", params: { id: String(r.orcamentoId) } })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao criar orçamento."
    } finally {
        criandoNovo.value = false
    }
}

function fmtMoeda(v: number) { return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) }
function fmtData(s: string) { return new Date(s).toLocaleDateString("pt-BR") }

onMounted(() => {
    void carregar()
    void carregarPacientes()
})
</script>

<template>
    <main class="app-page">
        <AppPageHeader
            titulo="Orçamentos"
            subtitulo="Gere e acompanhe orçamentos cirúrgicos por paciente e estabelecimento."
        >
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="abrirModalNovo">Novo orçamento</AppButton>
            </template>
        </AppPageHeader>

        <AppTabs v-model="aba" :abas="abas" variante="underline" class="mb-3" aria-label="Seções de orçamento" />

        <div v-if="erro" class="erro-banner">{{ erro }}</div>

        <AppCard class="filtros-card">
            <div class="filtros-grid">
                <AppField label="Paciente">
                    <AppSelect v-model="pacienteFiltro">
                        <option value="">Todos os pacientes</option>
                        <option v-for="p in pacientes" :key="p.id" :value="p.id">{{ p.nomeCompleto }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Status">
                    <AppSelect v-model="statusFiltro">
                        <option v-for="opt in STATUS_OPTIONS" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Vencimento de">
                    <AppInput type="date" v-model="vencimentoDe" />
                </AppField>
                <AppField label="Vencimento até">
                    <AppInput type="date" v-model="vencimentoAte" />
                </AppField>
                <AppField label="Ordenar por">
                    <AppSelect v-model="ordenacao">
                        <option v-for="opt in ORDENACAO_OPTIONS" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                    </AppSelect>
                </AppField>
            </div>
            <div class="filtros-acoes">
                <AppButton variant="ghost" icon="fa-solid fa-xmark" @click="limparFiltros">Limpar filtros</AppButton>
            </div>
        </AppCard>

        <div v-if="carregando" class="estado-vazio">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando orçamentos...
        </div>

        <AppEmptyState
            v-else-if="filtrados.length === 0"
            icone="fa-solid fa-file-invoice-dollar"
            titulo="Nenhum orçamento encontrado"
            descricao="Utilize os filtros acima ou crie um novo orçamento."
        />

        <template v-else>
            <table class="tabela">
                <thead>
                    <tr>
                        <th>Número</th>
                        <th>Paciente</th>
                        <th>Status</th>
                        <th>Validade</th>
                        <th>Total</th>
                        <th>Criado por</th>
                        <th>Data</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="o in paginados" :key="o.id" class="linha-orc" @click="abrir(o)">
                        <td>{{ o.numero || `#${o.id}` }}</td>
                        <td>{{ o.pacienteNome }}</td>
                        <td><AppBadge :status="o.status" /></td>
                        <td>{{ fmtData(o.validade) }}</td>
                        <td>{{ fmtMoeda(o.total) }}</td>
                        <td>{{ o.criadoPorNome }}</td>
                        <td>{{ fmtData(o.criadoEm) }}</td>
                        <td>
                            <button class="btn-icon btn-icon-ver" title="Ver">
                                <i class="fa-solid fa-eye"></i>
                            </button>
                        </td>
                    </tr>
                </tbody>
            </table>

            <AppPagination
                v-model:pagina="pagina"
                v-model:tamanho="tamanho"
                :total="total"
            />
        </template>

        <!-- Modal: Novo orçamento -->
        <AppModal :aberto="modalNovoAberto" titulo="Novo orçamento" @fechar="modalNovoAberto = false">
            <div class="form-novo">
                <AppField label="Paciente" for="novo-paciente">
                    <AppSelect id="novo-paciente" v-model="novoPacienteId">
                        <option value="">Selecione...</option>
                        <option v-for="p in pacientes" :key="p.id" :value="p.id">{{ p.nomeCompleto }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Validade" for="novo-validade">
                    <AppInput id="novo-validade" type="date" v-model="novoValidade" />
                </AppField>
                <p class="texto-aux">
                    Será criado um orçamento <strong>em rascunho</strong> com uma cirurgia placeholder.
                    Edite no formulário em seguida para preencher os detalhes.
                </p>
            </div>
            <template #footer>
                <AppButton variant="ghost" @click="modalNovoAberto = false">Cancelar</AppButton>
                <AppButton :loading="criandoNovo" :disabled="!novoPacienteId || !novoValidade" @click="criarNovo">
                    Criar e editar
                </AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.erro-banner {
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    margin-bottom: 1rem;
}
.filtros-card { margin-bottom: 1rem; }
.filtros-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 0.75rem;
    margin-bottom: 0.6rem;
}
.filtros-acoes {
    display: flex;
    justify-content: flex-end;
}
.tabela {
    width: 100%;
    border-collapse: collapse;
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    overflow: hidden;
    font-size: 0.88em;
    margin-bottom: 1rem;
}
.tabela th, .tabela td {
    padding: 0.6rem 0.85rem;
    text-align: left;
    border-bottom: 1px solid var(--border);
}
.tabela th {
    background: var(--bg-muted);
    font-weight: 600;
    font-size: 0.78em;
    text-transform: uppercase;
    color: var(--text-muted);
}
.tabela tr:last-child td { border-bottom: none; }
.linha-orc { cursor: pointer; transition: background 0.12s; }
.linha-orc:hover { background: var(--bg-muted); }

.estado-vazio {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-muted);
    padding: 2rem 0;
    font-size: 0.9em;
}

.form-novo { display: flex; flex-direction: column; gap: 0.85rem; }
.texto-aux { color: var(--text-muted); font-size: 0.82em; margin: 0; }
.mb-3 { margin-bottom: 0.75rem; }
</style>
