<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue"
import { useRouter } from "vue-router"
import {
    AppPageHeader, AppButton, AppSelect, AppSearchInput,
    AppPagination, AppModal, AppField, AppInput, AppDatePicker,
} from "@/components/ui"
import OrcamentoKpis   from "@/components/orcamento/OrcamentoKpis.vue"
import OrcamentoTabela from "@/components/orcamento/OrcamentoTabela.vue"
import { orcamentoService, type OrcamentoResumo, type OrcamentoStatus } from "@/services/orcamentoService"
import { pacienteService, type PacienteBuscaRapida } from "@/services/pacienteService"
import { useDebouncedRef } from "@/composables/useDebouncedRef"

const router = useRouter()

// ── Estado
const orcamentos = ref<OrcamentoResumo[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)
// Apenas {id, nomeCompleto} — o seletor só exibe o nome do paciente.
// Usar `listar()` (DTO completo com CPF/telefone/data nascimento/tags) aqui
// vazaria PII de centenas de pacientes só para popular um seletor (LGPD).
// O endpoint `busca-rapida` enforça LIMIT 30 server-side (anti-exfiltração);
// usuário com mais pacientes refina por nome no input de busca.
const pacientes = ref<PacienteBuscaRapida[]>([])
const buscaPacienteInput = ref("")
const buscaPaciente = useDebouncedRef(buscaPacienteInput)
const buscandoPacientes = ref(false)
let buscaPacienteReqId = 0

// ── Filtros
type TabKey = "todos" | "pendentes" | "aprovados" | "quitados" | "perdidos"
const tab = ref<TabKey>("todos")

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)

const statusFiltro = ref<OrcamentoStatus | "">("")

type Ordenacao = "recente" | "valor_desc" | "valor_asc" | "vencendo"
// AppSelect emite string — mantemos ref<string> e fazemos cast na leitura.
const ordenacaoStr = ref<string>("recente")
const ordenacao = computed(() => ordenacaoStr.value as Ordenacao)

const pagina = ref(1)
const tamanho = ref(20)

// ── Modal novo
const modalNovoAberto = ref(false)
const novoPacienteId = ref<number | "">("")
const novoValidade = ref<string>(formatarData(new Date(Date.now() + 30 * 86400_000)))
const criandoNovo = ref(false)
const erroModal = ref<string | null>(null)

const TABS: { valor: TabKey; label: string; statuses: OrcamentoStatus[] }[] = [
    { valor: "todos",     label: "Todos",     statuses: [] },
    { valor: "pendentes", label: "Pendentes", statuses: ["Rascunho", "Enviado"] },
    { valor: "aprovados", label: "Aprovados", statuses: ["Aprovado"] },
    { valor: "quitados",  label: "Quitados",  statuses: [] }, // sem status "Quitado" no backend — usar como aprovados pagos
    { valor: "perdidos",  label: "Perdidos",  statuses: ["Recusado", "Expirado", "Cancelado"] },
]

const ORDENACAO_OPTIONS: { value: Ordenacao; label: string }[] = [
    { value: "recente",    label: "Mais recentes" },
    { value: "valor_desc", label: "Maior valor" },
    { value: "valor_asc",  label: "Menor valor" },
    { value: "vencendo",   label: "Vencendo antes" },
]

function formatarData(d: Date) { return d.toISOString().slice(0, 10) }

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        orcamentos.value = await orcamentoService.listar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar orçamentos."
    } finally {
        carregando.value = false
    }
}

async function carregarPacientes(termo?: string) {
    const reqId = ++buscaPacienteReqId
    buscandoPacientes.value = true
    try {
        // `busca-rapida` aplica LIMIT 30 server-side. Quando o termo está vazio,
        // mostra os 30 mais recentes; com termo, filtra por nome (índice trigram).
        const resultado = await pacienteService.buscaRapida(termo || undefined, 30)
        if (reqId === buscaPacienteReqId) pacientes.value = resultado
    } catch {
        if (reqId === buscaPacienteReqId) pacientes.value = []
    } finally {
        if (reqId === buscaPacienteReqId) buscandoPacientes.value = false
    }
}

watch(buscaPaciente, (termo) => {
    if (modalNovoAberto.value) void carregarPacientes(termo)
})

// ── Contadores por tab (antes do filtro de busca)
const contagemTab = computed(() => {
    const list = orcamentos.value
    return {
        todos:     list.length,
        pendentes: list.filter(o => ["Rascunho", "Enviado"].includes(o.status)).length,
        aprovados: list.filter(o => o.status === "Aprovado").length,
        quitados:  0,
        perdidos:  list.filter(o => ["Recusado", "Expirado", "Cancelado"].includes(o.status)).length,
    }
})

// ── Filtragem + ordenação client-side
const filtrados = computed(() => {
    let arr = [...orcamentos.value]

    // Tab filter
    const t = TABS.find(x => x.valor === tab.value)
    if (t && t.statuses.length > 0) {
        arr = arr.filter(o => t.statuses.includes(o.status))
    }

    // Status adicional se selecionado
    if (statusFiltro.value) {
        arr = arr.filter(o => o.status === statusFiltro.value)
    }

    // Busca texto
    if (busca.value.trim()) {
        const q = busca.value.toLowerCase()
        arr = arr.filter(o =>
            o.pacienteNome.toLowerCase().includes(q) ||
            (o.numero && o.numero.toLowerCase().includes(q)) ||
            o.criadoPorNome.toLowerCase().includes(q)
        )
    }

    // Ordenação
    switch (ordenacao.value) {
        case "recente":    arr.sort((a, b) => b.criadoEm.localeCompare(a.criadoEm)); break
        case "valor_desc": arr.sort((a, b) => b.total - a.total); break
        case "valor_asc":  arr.sort((a, b) => a.total - b.total); break
        case "vencendo":   arr.sort((a, b) => a.validade.localeCompare(b.validade)); break
    }

    return arr
})

const totalFiltrado = computed(() => filtrados.value.length)
const paginados = computed(() => {
    const inicio = (pagina.value - 1) * tamanho.value
    return filtrados.value.slice(inicio, inicio + tamanho.value)
})

watch([tab, statusFiltro], () => { pagina.value = 1 })
watch(busca, () => { pagina.value = 1 })
watch(ordenacaoStr, () => { pagina.value = 1 })

function trocarTab(t: TabKey) {
    tab.value = t
    statusFiltro.value = ""
}

function abrirOrcamento(o: OrcamentoResumo) {
    router.push({ name: "OrcamentoDetalhe", params: { id: String(o.id) } })
}

function abrirModalNovo() {
    novoPacienteId.value = ""
    novoValidade.value = formatarData(new Date(Date.now() + 30 * 86400_000))
    erroModal.value = null
    buscaPacienteInput.value = ""
    modalNovoAberto.value = true
    void carregarPacientes()
}

async function criarNovo() {
    if (!novoPacienteId.value || !novoValidade.value) return
    criandoNovo.value = true
    erroModal.value = null
    try {
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
        erroModal.value = e?.response?.data?.mensagem ?? "Erro ao criar orçamento."
    } finally {
        criandoNovo.value = false
    }
}

onMounted(() => {
    void carregar()
    // Lista de pacientes é carregada sob demanda quando o modal "Novo orçamento"
    // abre (ver `abrirModalNovo`) — evita request desnecessário no mount.
})
</script>

<template>
    <div class="app-page app-page--wide">
        <AppPageHeader
            titulo="Orçamentos"
            subtitulo="Crie, envie e acompanhe orçamentos de procedimentos"
        >
            <template #acoes>
                <AppButton
                    variant="ghost"
                    icon="fa-solid fa-sliders"
                    @click="router.push('/configuracoes/orcamento')"
                >
                    Configurações
                </AppButton>
                <AppButton variant="secondary" icon="fa-solid fa-file-export">Exportar</AppButton>
                <AppButton icon="fa-solid fa-plus" @click="abrirModalNovo">Novo orçamento</AppButton>
            </template>
        </AppPageHeader>

        <!-- KPIs -->
        <OrcamentoKpis :orcamentos="orcamentos" />

        <!-- Erro de carregamento -->
        <div v-if="erro" class="erro-banner" role="alert">
            {{ erro }}
            <AppButton size="sm" variant="ghost" @click="carregar">Tentar novamente</AppButton>
        </div>

        <!-- Toolbar: tabs + busca + filtros -->
        <div class="toolbar">
            <div class="tabs-wrap">
                <button
                    v-for="t in TABS"
                    :key="t.valor"
                    class="tab-btn"
                    :class="{ active: tab === t.valor }"
                    type="button"
                    @click="trocarTab(t.valor)"
                >
                    {{ t.label }}
                    <span class="tab-count">{{ contagemTab[t.valor] }}</span>
                </button>
            </div>

            <div class="toolbar-end">
                <AppSearchInput
                    v-model="buscaInput"
                    placeholder="Buscar por paciente, número ou criado por..."
                />
                <AppSelect v-model="ordenacaoStr" class="select-sm">
                    <option v-for="opt in ORDENACAO_OPTIONS" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                </AppSelect>
            </div>
        </div>

        <!-- Tabela -->
        <OrcamentoTabela
            :orcamentos="paginados"
            :carregando="carregando"
            @abrir="abrirOrcamento"
        />

        <!-- Paginação -->
        <AppPagination
            v-if="totalFiltrado > tamanho"
            v-model:pagina="pagina"
            v-model:tamanho="tamanho"
            :total="totalFiltrado"
        />

        <!-- Modal: Novo orçamento -->
        <AppModal :aberto="modalNovoAberto" titulo="Novo orçamento" @fechar="modalNovoAberto = false">
            <div class="form-novo">
                <div v-if="erroModal" class="erro-banner" role="alert">{{ erroModal }}</div>

                <AppField label="Paciente" for="novo-paciente">
                    <AppSearchInput
                        v-model="buscaPacienteInput"
                        placeholder="Buscar paciente por nome..."
                        class="paciente-busca"
                    />
                    <AppSelect id="novo-paciente" v-model="novoPacienteId" class="paciente-select">
                        <option value="">
                            {{ buscandoPacientes ? "Buscando..." : pacientes.length === 0 ? "Nenhum paciente encontrado" : "Selecione o paciente..." }}
                        </option>
                        <option v-for="p in pacientes" :key="p.id" :value="p.id">{{ p.nomeCompleto }}</option>
                    </AppSelect>
                    <p class="texto-aux">
                        Exibindo até 30 pacientes — refine pelo nome se não encontrar.
                    </p>
                </AppField>
                <AppField label="Validade">
                    <AppDatePicker v-model="novoValidade" placeholder="DD/MM/AAAA" />
                </AppField>
                <p class="texto-aux">
                    Será criado um orçamento <strong>em rascunho</strong>.
                    Preencha os detalhes no formulário em seguida.
                </p>
            </div>
            <template #footer>
                <AppButton variant="ghost" @click="modalNovoAberto = false">Cancelar</AppButton>
                <AppButton
                    :loading="criandoNovo"
                    :disabled="!novoPacienteId || !novoValidade"
                    @click="criarNovo"
                >
                    Criar e editar
                </AppButton>
            </template>
        </AppModal>
    </div>
</template>

<style scoped>
.erro-banner {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    font-size: 0.875rem;
}

/* Toolbar */
.toolbar {
    display: flex;
    gap: 14px;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
}

.tabs-wrap {
    display: flex;
    gap: 4px;
    background: hsl(var(--secondary) / 0.05);
    padding: 4px;
    border-radius: 10px;
}

.tab-btn {
    border: none;
    background: transparent;
    padding: 8px 14px;
    border-radius: 7px;
    font-size: 13px;
    font-weight: 500;
    color: hsl(var(--secondary) / 0.7);
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    transition: all 0.12s;
}
.tab-btn:hover { color: hsl(var(--secondary)); }
.tab-btn.active {
    background: hsl(var(--card));
    color: hsl(var(--primary));
    box-shadow: 0 1px 3px hsl(var(--secondary) / 0.1);
}

.tab-count {
    font-size: 11px;
    padding: 1px 7px;
    border-radius: 10px;
    background: hsl(var(--secondary) / 0.08);
    color: hsl(var(--secondary) / 0.7);
}
.tab-btn.active .tab-count {
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
}

.toolbar-end {
    display: flex;
    gap: 8px;
    align-items: center;
    flex: 1;
    justify-content: flex-end;
    min-width: 0;
}

.select-sm { min-width: 160px; }

/* Modal */
.form-novo { display: flex; flex-direction: column; gap: 0.85rem; }
.texto-aux { color: var(--text-muted); font-size: 0.82em; margin: 0; }

.paciente-busca { margin-bottom: 8px; }
.paciente-select { width: 100%; }

@media (max-width: 900px) {
    .toolbar { flex-direction: column; align-items: stretch; }
    .toolbar-end { flex-direction: column; }
    .tabs-wrap { overflow-x: auto; }
}
</style>
