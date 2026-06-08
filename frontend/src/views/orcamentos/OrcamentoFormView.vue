<script setup lang="ts">
/**
 * OrcamentoFormView — formulário paritário com o legado (Budgets.vue do Imedto/Vue+Supabase).
 *
 * Modos:
 * - `OrcamentoNovo` (rota `/orcamentos/novo`): form em branco. POST só no salvar (CA-1).
 * - `OrcamentoForm` (rota `/orcamentos/:id/editar`): carrega o orçamento existente.
 *
 * Estrutura (paridade): paciente + título + validade → cirurgias (com tempo) →
 * produtos consolidados (chamada ao backend) → profissionais (catálogo) →
 * equipes especializadas → implantes → local cirúrgico (5 opções) → anestesia →
 * formas de pagamento (com indicador de diferença).
 *
 * Regras de negócio (cálculos, consolidação, validações) vivem no backend —
 * este arquivo só monta payload e renderiza preview.
 */
import { ref, computed, onMounted, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import {
    orcamentoService,
    type Orcamento,
    type OrcamentoEquipe,
    type OrcamentoImplante,
    type OrcamentoFormaPagamento,
    type OrcamentoCirurgia,
    type OrcamentoAnestesia,
    type PreviewOrcamentoPayload,
    type ProdutoConsolidado,
    type TipoLocalCirurgia,
} from "@/services/orcamentoService"
import { usePreviewOrcamento } from "@/composables/usePreviewOrcamento"
import {
    orcamentoCatalogoService,
    type CatalogoCirurgia,
    type ValorProfissionalOrcamentoCatalogo,
    type CatalogoEquipe,
    type CatalogoImplante,
    type ConfiguracaoLocalCirurgia,
} from "@/services/orcamentoCatalogoService"
import { formaPagamentoService, type FormaPagamento } from "@/services/categoriaFinanceiraService"
import { vinculoService, type ProfissionalPublico } from "@/services/vinculoService"
import { pacienteService, type PacienteBuscaRapida } from "@/services/pacienteService"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import {
    AppButton, AppCard,
    AppField, AppInput, AppDatePicker, AppTextarea, AppSelect, AppEmptyState,
} from "@/components/ui"
import OrcamentoStatusPill from "@/components/orcamento/OrcamentoStatusPill.vue"
import OrcamentoResumoSidebar from "@/components/orcamento/OrcamentoResumoSidebar.vue"

const route = useRoute()
const router = useRouter()

// Modo de operação: "criar" → POST no submit; "editar" → PUT no submit.
const modo = computed<"criar" | "editar">(() => route.name === "OrcamentoNovo" ? "criar" : "editar")
const orcamentoIdEditar = computed<number | null>(() =>
    modo.value === "editar" ? Number(route.params.id) : null)

// Quando vier de uma ficha de agendamento, ?agendamentoId=X&pacienteId=Y são pré-preenchidos.
const agendamentoIdInicial = computed<number | null>(() => {
    const v = route.query.agendamentoId
    return v ? Number(v) : null
})
const pacienteIdInicial = computed<number | null>(() => {
    const v = route.query.pacienteId
    return v ? Number(v) : null
})

// ── Estado do form (criar ou editar — mesmo formato) ────────────────────────
const orcamentoCarregado = ref<Orcamento | null>(null) // só em modo "editar"
const carregando = ref(false)
const salvando = ref(false)
const erro = ref<string | null>(null)

// Bloqueio de permissão: quando catálogos retornam 422, o form não pode ser usado.
const bloqueioPermissao = ref(false)
// Falhas isoladas de rede por catálogo (não-422): os outros selects ainda funcionam.
const falhaCatalogos = ref({
    cirurgias: false,
    valores: false,
    equipes: false,
    implantes: false,
    locais: false,
    formasPagamento: false,
    profissionais: false,
})

// Cabeçalho
const pacienteId  = ref<number | null>(null)
const pacienteNome = ref<string>("")
const titulo      = ref<string>("")
const validade    = ref<string>(formatarHoje(30))
const observacoes = ref<string>("")
const procedimentoCirurgicoId = ref<number | null>(null)
const agendamentoId = ref<number | null>(null)

// Cirurgias (com tempo)
interface CirurgiaForm {
    catalogoId: number          // 0 = não selecionada
    descricao: string
    quantidade: number
    duracaoMinutos: number
    valor: number
}
const cirurgias = ref<CirurgiaForm[]>([])
const tempoEditadoManualmente = ref(false)
const tempoCirurgiaMinutosManual = ref(0)

// Produtos consolidados (vem do backend após selecionar cirurgias)
const produtosConsolidados = ref<ProdutoConsolidado[]>([])
const carregandoProdutos = ref(false)

// Profissionais (catálogo de valor profissional)
interface ProfissionalForm {
    valorProfissionalId: number   // 0 = não selecionado
    funcao: string
    profissionalNome: string
    profissionalUsuarioId: string | null
    tempoBaseMinutos: number
    valorTempoBase: number
    tempoAdicionalMinutos: number
    valorAdicional: number
    valorPlus: number
    tempoCustomMinutos: number   // pode divergir do tempo da cirurgia
    quantidade: number
    valorCalculado: number       // vem do preview
}
const profissionais = ref<ProfissionalForm[]>([])

// Equipes especializadas (catálogo legado)
interface EquipeForm { catalogoId: number; descricao: string; quantidade: number; valorUnitario: number }
const equipes = ref<EquipeForm[]>([])

// Implantes
interface ImplanteForm { catalogoId: number; descricao: string; quantidade: number; valorUnitario: number }
const implantes = ref<ImplanteForm[]>([])

// Local cirúrgico
const localTipo = ref<TipoLocalCirurgia | null>(null)
const valorLocalSnapshot = ref(0)  // vem do preview

// Anestesia (1:1 — opcional)
const anestesia = ref<OrcamentoAnestesia | null>(null)
const TIPOS_ANESTESIA = ["Local", "Sedacao", "Geral", "Raquianestesia", "Peridural", "Bloqueio"]

// Formas de pagamento
const formas = ref<OrcamentoFormaPagamento[]>([])

// ── Catálogos do estabelecimento ────────────────────────────────────────────
const catCirurgias  = ref<CatalogoCirurgia[]>([])
const catValores    = ref<ValorProfissionalOrcamentoCatalogo[]>([])
const catEquipes    = ref<CatalogoEquipe[]>([])
const catImplantes  = ref<CatalogoImplante[]>([])
const catLocais     = ref<ConfiguracaoLocalCirurgia[]>([])
const formasPagamento = ref<FormaPagamento[]>([])
const profissionaisPublicos = ref<ProfissionalPublico[]>([])

// Paciente (modo criar — combobox com busca embutida)
const buscaPacienteInput = ref("")
const buscaPaciente = useDebouncedRef(buscaPacienteInput)
const pacientesEncontrados = ref<PacienteBuscaRapida[]>([])
const buscandoPaciente = ref(false)
const pacienteComboAberto = ref(false)
const itemFocado = ref(-1)
let pacBuscaReqId = 0
// Selecionar/pré-selecionar escreve o nome no próprio input de busca; essa flag
// evita que essa escrita programática dispare uma nova busca redundante.
let ignorarProximaBusca = false

watch(buscaPaciente, (termo) => {
    if (modo.value !== "criar") return
    if (ignorarProximaBusca) { ignorarProximaBusca = false; return }
    void buscarPacientes(termo)
})

async function buscarPacientes(termo: string | undefined) {
    const reqId = ++pacBuscaReqId
    buscandoPaciente.value = true
    try {
        const r = await pacienteService.buscaRapida(termo || undefined, 30)
        if (reqId === pacBuscaReqId) pacientesEncontrados.value = r
    } catch {
        if (reqId === pacBuscaReqId) pacientesEncontrados.value = []
    } finally {
        if (reqId === pacBuscaReqId) buscandoPaciente.value = false
    }
}

function onSelecionarPaciente(id: number) {
    const p = pacientesEncontrados.value.find(x => x.id === id)
    pacienteId.value = id
    pacienteNome.value = p?.nomeCompleto ?? ""
    ignorarProximaBusca = true
    buscaPacienteInput.value = p?.nomeCompleto ?? ""
    pacienteComboAberto.value = false
    itemFocado.value = -1
}

// Digitar no campo reabre a lista; se o texto diverge do paciente já escolhido,
// a seleção é invalidada até o usuário escolher de novo (evita salvar com nome
// editado mas id antigo).
function onBuscaPacienteInput(valor: string) {
    buscaPacienteInput.value = valor
    pacienteComboAberto.value = true
    itemFocado.value = -1
    if (valor !== pacienteNome.value) pacienteId.value = null
}

function onPacienteKeydown(e: KeyboardEvent) {
    if (!pacienteComboAberto.value) {
        if (e.key === "ArrowDown") { e.preventDefault(); pacienteComboAberto.value = true }
        return
    }
    if (e.key === "ArrowDown") {
        e.preventDefault()
        itemFocado.value = Math.min(itemFocado.value + 1, pacientesEncontrados.value.length - 1)
    } else if (e.key === "ArrowUp") {
        e.preventDefault()
        itemFocado.value = Math.max(itemFocado.value - 1, 0)
    } else if (e.key === "Enter") {
        if (itemFocado.value >= 0) {
            e.preventDefault()
            onSelecionarPaciente(pacientesEncontrados.value[itemFocado.value].id)
        }
    } else if (e.key === "Escape") {
        pacienteComboAberto.value = false
    }
}

// Delay para o mousedown do item disparar antes do fechamento.
function onPacienteBlur() {
    setTimeout(() => { pacienteComboAberto.value = false }, 150)
}

// Pré-seleciona paciente vindo via ?pacienteId=. Se não estiver na busca rápida
// inicial (paciente antigo, fora do top 30), faz fetch direto e injeta na lista
// para o <AppSelect> conseguir exibir o nome.
async function preselecionarPaciente(id: number) {
    const existente = pacientesEncontrados.value.find(p => p.id === id)
    if (existente) {
        pacienteId.value = id
        pacienteNome.value = existente.nomeCompleto
        ignorarProximaBusca = true
        buscaPacienteInput.value = existente.nomeCompleto
        return
    }
    try {
        const p = await pacienteService.obter(id)
        pacientesEncontrados.value = [
            { id: p.id, nomeCompleto: p.nomeCompleto },
            ...pacientesEncontrados.value,
        ]
        pacienteId.value = id
        pacienteNome.value = p.nomeCompleto
        ignorarProximaBusca = true
        buscaPacienteInput.value = p.nomeCompleto
    } catch {
        // Acesso negado ou paciente inexistente — deixa o seletor vazio.
        pacienteId.value = null
    }
}

// ── Tempo total da cirurgia (sum automático com override manual) ───────────
const tempoAutoMinutos = computed(() =>
    cirurgias.value.reduce((acc, c) => acc + (c.duracaoMinutos * c.quantidade), 0)
)
const tempoFinalMinutos = computed(() =>
    tempoEditadoManualmente.value ? tempoCirurgiaMinutosManual.value : tempoAutoMinutos.value
)

watch(tempoAutoMinutos, (n) => {
    if (!tempoEditadoManualmente.value) tempoCirurgiaMinutosManual.value = n
})

function onEditarTempoManual(minutos: number) {
    tempoCirurgiaMinutosManual.value = Math.max(0, Math.round(minutos))
    tempoEditadoManualmente.value = tempoCirurgiaMinutosManual.value !== tempoAutoMinutos.value
}
function resetarTempo() {
    tempoEditadoManualmente.value = false
    tempoCirurgiaMinutosManual.value = tempoAutoMinutos.value
}

// ── Cirurgias (handlers) ────────────────────────────────────────────────────
function adicionarCirurgia() {
    cirurgias.value.push({ catalogoId: 0, descricao: "", quantidade: 1, duracaoMinutos: 0, valor: 0 })
}
function removerCirurgia(idx: number) { cirurgias.value.splice(idx, 1) }
function onSelecionarCatCirurgia(idx: number, id: number) {
    const c = catCirurgias.value.find(x => x.id === id)
    if (!c) return
    const item = cirurgias.value[idx]
    item.catalogoId = id
    item.descricao = c.descricao
    item.duracaoMinutos = c.duracaoPadraoMinutos ?? 0
    item.valor = c.valorBase
}

const cirurgiasSelecionadasIds = computed(() => cirurgias.value.map(c => c.catalogoId).filter(Boolean))
function catalogoDisponivel(idCatalogo: number, idxAtual: number): boolean {
    const atual = cirurgias.value[idxAtual]?.catalogoId
    if (idCatalogo === atual) return true
    return !cirurgiasSelecionadasIds.value.includes(idCatalogo)
}

// ── Consolidação de produtos (chamada ao backend, debounce ~300ms) ─────────
let consolidaTimer: ReturnType<typeof setTimeout> | null = null
let consolidaReqId = 0
async function consolidarProdutos() {
    const cirs = cirurgias.value
        .filter(c => c.catalogoId > 0 && c.quantidade > 0)
        .map(c => ({ catalogoCirurgiaId: c.catalogoId, quantidade: c.quantidade }))
    if (cirs.length === 0) {
        produtosConsolidados.value = []
        return
    }
    const reqId = ++consolidaReqId
    carregandoProdutos.value = true
    try {
        const r = await orcamentoService.consolidarProdutos(cirs)
        if (reqId === consolidaReqId) produtosConsolidados.value = r
    } catch {
        if (reqId === consolidaReqId) produtosConsolidados.value = []
    } finally {
        if (reqId === consolidaReqId) carregandoProdutos.value = false
    }
}
watch(() => cirurgias.value.map(c => `${c.catalogoId}:${c.quantidade}`).join(","), () => {
    if (consolidaTimer) clearTimeout(consolidaTimer)
    consolidaTimer = setTimeout(consolidarProdutos, 300)
})

// ── Profissionais ───────────────────────────────────────────────────────────
function adicionarProfissional() {
    profissionais.value.push({
        valorProfissionalId: 0, funcao: "", profissionalNome: "", profissionalUsuarioId: null,
        tempoBaseMinutos: 0, valorTempoBase: 0,
        tempoAdicionalMinutos: 0, valorAdicional: 0, valorPlus: 0,
        tempoCustomMinutos: tempoFinalMinutos.value, quantidade: 1, valorCalculado: 0,
    })
}
function removerProfissional(idx: number) { profissionais.value.splice(idx, 1) }
function onSelecionarValorProfissional(idx: number, id: number) {
    const v = catValores.value.find(x => x.id === id)
    if (!v) return
    const prof = profissionais.value[idx]
    prof.valorProfissionalId = id
    prof.funcao = v.funcao
    prof.profissionalUsuarioId = v.profissionalUsuarioId
    prof.profissionalNome = v.profissionalNome ?? v.funcao
    prof.tempoBaseMinutos = v.tempoBaseMinutos
    prof.valorTempoBase = Number(v.valorTempoBase)
    prof.tempoAdicionalMinutos = v.tempoAdicionalMinutos
    prof.valorAdicional = Number(v.valorAdicional)
    prof.valorPlus = Number(v.valorPlus)
    prof.tempoCustomMinutos = tempoFinalMinutos.value
}

// ── Equipes especializadas (catálogo legado) ───────────────────────────────
function adicionarEquipe() {
    equipes.value.push({ catalogoId: 0, descricao: "", quantidade: 1, valorUnitario: 0 })
}
function removerEquipe(idx: number) { equipes.value.splice(idx, 1) }
function onSelecionarEquipe(idx: number, id: number) {
    const e = catEquipes.value.find(x => x.id === id)
    if (!e) return
    equipes.value[idx].catalogoId = id
    equipes.value[idx].descricao = e.descricao
    equipes.value[idx].valorUnitario = Number(e.valorPadrao)
}

// ── Implantes ───────────────────────────────────────────────────────────────
function adicionarImplante() {
    implantes.value.push({ catalogoId: 0, descricao: "", quantidade: 1, valorUnitario: 0 })
}
function removerImplante(idx: number) { implantes.value.splice(idx, 1) }
function onSelecionarImplante(idx: number, id: number) {
    const i = catImplantes.value.find(x => x.id === id)
    if (!i) return
    implantes.value[idx].catalogoId = id
    implantes.value[idx].descricao = i.descricao
    implantes.value[idx].valorUnitario = Number(i.custoUnitario)
}

// ── Local cirúrgico (5 opções) ──────────────────────────────────────────────
interface OpcaoLocal { tipo: TipoLocalCirurgia; label: string; descricao: string; comInternacao: boolean }
const OPCOES_LOCAL: OpcaoLocal[] = [
    { tipo: "IntLocal",      label: "Anestesia Local + Sedação",   descricao: "Com Internação", comInternacao: true },
    { tipo: "IntPeridural",  label: "Peridural/Raqui + Sedação",   descricao: "Com Internação", comInternacao: true },
    { tipo: "IntGeral",      label: "Anestesia Geral + TOT",        descricao: "Com Internação", comInternacao: true },
    { tipo: "SemInternacao", label: "Anestesia Local",              descricao: "Sem Internação", comInternacao: false },
    { tipo: "Ambulatorio",   label: "Anestesia Local",              descricao: "Ambulatório",    comInternacao: false },
]

function configLocalPorTipo(tipo: TipoLocalCirurgia): ConfiguracaoLocalCirurgia | undefined {
    return catLocais.value.find(c => c.tipoLocal === tipo)
}

function selecionarLocal(tipo: TipoLocalCirurgia) {
    localTipo.value = localTipo.value === tipo ? null : tipo
}

// ── Anestesia (1:1 opcional) ────────────────────────────────────────────────
function alternarAnestesia() {
    anestesia.value = anestesia.value
        ? null
        : { tipoAnestesia: "Local", valor: 0, observacao: null }
}

// ── Formas de pagamento ────────────────────────────────────────────────────
function adicionarForma() {
    formas.value.push({ formaPagamentoId: 0, valor: 0, parcelas: 1, acrescimoPercentual: 0, entradaPercentual: 0, observacao: null })
}
function removerForma(idx: number) { formas.value.splice(idx, 1) }
function onSelecionarFormaCatalogo(idx: number, id: number) {
    const fp = formasPagamento.value.find(x => x.id === id)
    if (!fp) return
    formas.value[idx].formaPagamentoId = id
    formas.value[idx].formaPagamentoNome = fp.nome
}

// Distribui o "resto" para zerar a diferença na última forma adicionada.
function distribuirRestoNaUltimaForma() {
    if (formas.value.length === 0) return
    const ultima = formas.value[formas.value.length - 1]
    const novoValor = Number(ultima.valor) + Number(diferenca.value)
    ultima.valor = Math.max(0, Math.round(novoValor * 100) / 100)
}

// ── Preview payload (sempre reativo) ───────────────────────────────────────
const previewPayload = computed<PreviewOrcamentoPayload>(() => ({
    itens: orcamentoCarregado.value?.itens ?? [],
    equipe: equipes.value
        .filter(e => e.catalogoId > 0)
        .map(e => ({
            profissionalUsuarioId: "00000000-0000-0000-0000-000000000000",
            papel: e.descricao,
            valor: e.quantidade * e.valorUnitario,
        })),
    implantes: implantes.value
        .filter(i => i.catalogoId > 0 || i.descricao)
        .map(i => ({
            itemInventarioId: null,
            descricao: i.descricao,
            quantidade: i.quantidade,
            custoUnitario: i.valorUnitario,
        })),
    formasPagamento: formas.value
        .filter(f => f.formaPagamentoId > 0)
        .map(f => ({
            formaPagamentoId: f.formaPagamentoId, valor: Number(f.valor), parcelas: Number(f.parcelas),
            acrescimoPercentual: Number(f.acrescimoPercentual), entradaPercentual: Number(f.entradaPercentual),
            observacao: f.observacao ?? null,
        })),
    cirurgias: cirurgias.value
        .filter(c => c.catalogoId > 0)
        .map(c => ({
            procedimentoCirurgicoId: null,
            descricao: c.descricao,
            quantidade: c.quantidade,
            duracaoMinutos: c.duracaoMinutos,
            valorTotal: c.valor * c.quantidade,
        })),
    localCirurgia: localTipo.value
        ? { tipo: localTipo.value, tempoMinutos: tempoFinalMinutos.value }
        : null,
    anestesia: anestesia.value
        ? { tipo: anestesia.value.tipoAnestesia, valor: Number(anestesia.value.valor), observacao: anestesia.value.observacao }
        : null,
    equipeComCatalogo: profissionais.value
        .filter(p => p.valorProfissionalId > 0)
        .map(p => ({
            valorProfissionalId: p.valorProfissionalId,
            quantidade: p.quantidade,
            tempoMinutos: p.tempoCustomMinutos,
        })),
}))

const { preview, carregando: calculando } = usePreviewOrcamento(previewPayload)

// Aplica valores calculados de equipe vindos do preview (read-only no input).
watch(() => preview.value?.equipes, (eqs) => {
    if (!eqs) return
    for (const calc of eqs) {
        const p = profissionais.value.find(p => p.valorProfissionalId === calc.valorProfissionalId)
        if (p) p.valorCalculado = calc.valorUnitario
    }
}, { deep: true })

// Aplica snapshot do local cirúrgico.
watch(() => preview.value?.totalLocal, (v) => { valorLocalSnapshot.value = v ?? 0 })

// ── Totais derivados ───────────────────────────────────────────────────────
const totalCirurgias  = computed(() => preview.value?.totalCirurgias ?? 0)
const totalEquipes    = computed(() => preview.value?.totalEquipe ?? 0)
const totalImplantes  = computed(() => preview.value?.totalImplantes ?? 0)
const totalLocal      = computed(() => preview.value?.totalLocal ?? 0)
const totalAnestesia  = computed(() => preview.value?.totalAnestesia ?? 0)
const totalProdutos = computed(() =>
    produtosConsolidados.value.reduce((acc, p) => acc + p.subtotal, 0)
)
// Subtotal exibido na sidebar = soma dos componentes do orçamento. Produtos
// consolidados não fazem parte do payload (são insumos das cirurgias).
const subtotalPreview = computed(() => preview.value?.totalGeral ?? 0)
const somaFormas      = computed(() => preview.value?.somaFormas ?? 0)
const diferenca       = computed(() => preview.value?.diferenca ?? 0)
const integridadeOk   = computed(() => preview.value?.integridadeOk ?? true)

// Desconto inline (apenas display — o backend não aceita desconto livre no aggregate).
const desconto     = ref(0)
const tipoDesconto = ref<"valor" | "percentual">("valor")
const descontoValor = computed(() => tipoDesconto.value === "percentual"
    ? subtotalPreview.value * (desconto.value / 100)
    : desconto.value)
const totalComDesconto = computed(() => Math.max(0, subtotalPreview.value - descontoValor.value))

// ── Carregamento de catálogos / orçamento ─────────────────────────────────

/** Retorna true quando o erro é um 422 de permissão (BusinessException do backend). */
function erroEhBloqueioPermissao(e: unknown): boolean {
    const status = (e as any)?.response?.status
    // Os endpoints de catálogo são GETs puros sem body — qualquer 422 vem de
    // RequiresAcao ou FeatureGate, nunca de validação de dados.
    return status === 422
}

async function carregarCatalogos() {
    bloqueioPermissao.value = false
    falhaCatalogos.value = { cirurgias: false, valores: false, equipes: false, implantes: false, locais: false, formasPagamento: false, profissionais: false }

    const [rCirs, rVals, rEqs, rImps, rLocs, rFps, rProfs] = await Promise.allSettled([
        orcamentoCatalogoService.listarCirurgias(true),
        orcamentoCatalogoService.listarValoresProfissional(true),
        orcamentoCatalogoService.listarEquipes(true),
        orcamentoCatalogoService.listarImplantes(true),
        orcamentoCatalogoService.listarLocais(),
        formaPagamentoService.listar(),
        vinculoService.listarProfissionaisPublico(),
    ])

    const resultados = [rCirs, rVals, rEqs, rImps, rLocs, rFps, rProfs]

    // Qualquer 422 em catálogo de orçamento é bloqueio de permissão (fonte: RequiresAcao
    // no OrcamentoCatalogoController). Tem precedência sobre degradação graciosa (CA5).
    if (resultados.some(r => r.status === "rejected" && erroEhBloqueioPermissao(r.reason))) {
        bloqueioPermissao.value = true
        return
    }

    // Sem bloqueio de permissão: aplica degradação graciosa — os que carregaram funcionam.
    if (rCirs.status === "fulfilled") catCirurgias.value = rCirs.value
    else falhaCatalogos.value.cirurgias = true

    if (rVals.status === "fulfilled") catValores.value = rVals.value
    else falhaCatalogos.value.valores = true

    if (rEqs.status === "fulfilled") catEquipes.value = rEqs.value
    else falhaCatalogos.value.equipes = true

    if (rImps.status === "fulfilled") catImplantes.value = rImps.value
    else falhaCatalogos.value.implantes = true

    if (rLocs.status === "fulfilled") catLocais.value = rLocs.value
    else falhaCatalogos.value.locais = true

    if (rFps.status === "fulfilled") formasPagamento.value = rFps.value
    else falhaCatalogos.value.formasPagamento = true

    if (rProfs.status === "fulfilled") profissionaisPublicos.value = rProfs.value
    else falhaCatalogos.value.profissionais = true
}

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        await carregarCatalogos()

        // Bloqueio de permissão: não prossegue com o restante do carregamento.
        if (bloqueioPermissao.value) return

        if (modo.value === "editar" && orcamentoIdEditar.value) {
            const orc = await orcamentoService.obter(orcamentoIdEditar.value)
            orcamentoCarregado.value = orc
            hidratarFormulario(orc)
        } else {
            // Modo "criar" — pré-popular se vier de agendamento/paciente.
            if (agendamentoIdInicial.value) agendamentoId.value = agendamentoIdInicial.value
            await buscarPacientes(undefined)
            if (pacienteIdInicial.value) {
                await preselecionarPaciente(pacienteIdInicial.value)
            }
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar."
    } finally {
        carregando.value = false
    }
}

function hidratarFormulario(orc: Orcamento) {
    pacienteId.value = orc.pacienteId
    pacienteNome.value = orc.pacienteNome
    titulo.value = orc.titulo ?? ""
    validade.value = orc.validade
    observacoes.value = orc.observacoes ?? ""
    procedimentoCirurgicoId.value = orc.procedimentoCirurgicoId
    agendamentoId.value = orc.agendamentoId

    cirurgias.value = orc.cirurgias.map(c => ({
        catalogoId: 0, // legado não rastreava catálogo na cirurgia salva; usuário pode re-selecionar
        descricao: c.descricao ?? "",
        quantidade: c.quantidade,
        duracaoMinutos: c.duracaoMinutos ?? 0,
        valor: c.quantidade > 0 ? c.valorTotal / c.quantidade : c.valorTotal,
    }))

    // Equipe especializada → equipes (legado misturava). Mantemos formato simples.
    equipes.value = []
    profissionais.value = []
    orc.equipe.forEach(e => {
        // Heurística: se o UUID é o "zero" (equipe especializada do legado), vira equipe.
        if (e.profissionalUsuarioId === "00000000-0000-0000-0000-000000000000") {
            equipes.value.push({ catalogoId: 0, descricao: e.papel, quantidade: 1, valorUnitario: e.valor })
        }
        // Senão, é um profissional — mas precisamos do catálogo de valor pra pré-popular.
        // Como o aggregate só guarda papel+valor, o usuário precisa re-selecionar a tabela.
    })

    implantes.value = orc.implantes.map(i => ({
        catalogoId: 0,
        descricao: i.descricao,
        quantidade: Number(i.quantidade),
        valorUnitario: Number(i.custoUnitario),
    }))

    formas.value = [...orc.formasPagamento]

    if (orc.localCirurgia) {
        localTipo.value = orc.localCirurgia.tipo
        tempoEditadoManualmente.value = true
        tempoCirurgiaMinutosManual.value = orc.localCirurgia.tempoMinutos
        valorLocalSnapshot.value = orc.localCirurgia.valor
    }

    anestesia.value = orc.anestesia ? { ...orc.anestesia } : null
}

// ── Validação client-side (espelhada pelo backend) ─────────────────────────
function validar(): string | null {
    if (!pacienteId.value) return "Selecione um paciente."
    if (cirurgias.value.length === 0)
        return "Adicione ao menos uma cirurgia (ou item/implante/equipe)."
    if (cirurgias.value.some(c => c.catalogoId === 0))
        return "Selecione a cirurgia em todas as linhas adicionadas."
    if (!validade.value) return "Informe a validade."
    if (!integridadeOk.value && formas.value.length > 0)
        return `Soma das formas de pagamento difere do total em ${fmt(diferenca.value)}.`
    return null
}

async function salvar(enviarApos = false) {
    erro.value = null
    const v = validar()
    if (v) { erro.value = v; return }
    salvando.value = true
    try {
        const payload = {
            titulo: titulo.value || null,
            validade: validade.value,
            observacoes: observacoes.value || null,
            procedimentoCirurgicoId: procedimentoCirurgicoId.value,
            agendamentoId: agendamentoId.value,
            itens: orcamentoCarregado.value?.itens ?? [],
            equipe: previewPayload.value.equipe,
            implantes: previewPayload.value.implantes,
            formasPagamento: previewPayload.value.formasPagamento,
            cirurgias: previewPayload.value.cirurgias,
            localCirurgia: previewPayload.value.localCirurgia,
            anestesia: previewPayload.value.anestesia,
        }

        let id: number
        if (modo.value === "criar") {
            const r = await orcamentoService.criar({
                pacienteId: pacienteId.value!,
                ...payload,
            })
            id = r.orcamentoId
        } else {
            await orcamentoService.atualizar(orcamentoIdEditar.value!, payload)
            id = orcamentoIdEditar.value!
        }

        if (enviarApos) {
            try { await orcamentoService.enviar(id) }
            catch (e: any) { erro.value = e?.response?.data?.mensagem ?? "Salvo, mas falhou ao enviar."; salvando.value = false; return }
        }

        router.push({ name: "OrcamentoDetalhe", params: { id: String(id) } })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar."
    } finally {
        salvando.value = false
    }
}

function voltar() { router.push({ name: "Orcamentos" }) }

function formatarHoje(diasAFrente = 0) {
    const d = new Date()
    d.setDate(d.getDate() + diasAFrente)
    return d.toISOString().slice(0, 10)
}

function fmt(v: number) { return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) }
function fmtMinAsHoras(m: number) {
    if (!m) return "0h"
    const h = Math.floor(m / 60), min = m % 60
    return min > 0 ? `${h}h${min}min` : `${h}h`
}

onMounted(carregar)
</script>

<template>
    <div class="app-page app-page--wide">
        <div v-if="carregando" class="estado-loading">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>

        <div v-else-if="bloqueioPermissao" class="bloqueio-permissao">
            <AppEmptyState
                icone="fa-solid fa-lock"
                titulo="Sem permissão"
                descricao="Você não tem permissão para configurar orçamentos."
            >
                <template #acao>
                    <AppButton variant="ghost" icon="fa-solid fa-arrow-left" @click="voltar">Voltar</AppButton>
                </template>
            </AppEmptyState>
        </div>

        <template v-else>
            <!-- Header -->
            <div class="form-header">
                <div class="form-header-l">
                    <button type="button" class="btn-back" @click="voltar" aria-label="Voltar">
                        <i class="fa-solid fa-arrow-left"></i>
                    </button>
                    <div>
                        <div class="form-crumb">
                            Orçamentos / {{ modo === "criar" ? "Novo" : (orcamentoCarregado?.numero || `#${orcamentoIdEditar}`) }}
                        </div>
                        <h1 class="form-titulo">
                            {{ modo === "criar" ? "Novo orçamento" : `Editar ${orcamentoCarregado?.numero || ""}` }}
                        </h1>
                    </div>
                    <OrcamentoStatusPill v-if="orcamentoCarregado" :status="orcamentoCarregado.status" />
                </div>
                <div class="form-header-r">
                    <AppButton variant="ghost" icon="fa-solid fa-arrow-left" @click="voltar">Cancelar</AppButton>
                    <AppButton
                        variant="secondary"
                        icon="fa-solid fa-save"
                        :loading="salvando"
                        @click="salvar(false)"
                    >Salvar rascunho</AppButton>
                    <AppButton
                        icon="fa-solid fa-paper-plane"
                        :loading="salvando"
                        @click="salvar(true)"
                    >Salvar e enviar</AppButton>
                </div>
            </div>

            <div v-if="erro" class="erro-banner" role="alert">{{ erro }}</div>

            <div class="form-grid">
                <div class="col-principal">
                    <!-- 1. Paciente + cabeçalho -->
                    <AppCard title="Paciente e validade">
                        <div class="fg">
                            <AppField label="Paciente *">
                                <template v-if="modo === 'editar'">
                                    <AppInput :model-value="pacienteNome" readonly />
                                </template>
                                <template v-else>
                                    <div
                                        class="paciente-combo"
                                        role="combobox"
                                        :aria-expanded="pacienteComboAberto"
                                        aria-haspopup="listbox"
                                    >
                                        <div class="combo-input-row">
                                            <i class="fa-solid fa-magnifying-glass combo-icon" aria-hidden="true"></i>
                                            <input
                                                type="text"
                                                class="combo-input"
                                                :value="buscaPacienteInput"
                                                placeholder="Buscar paciente por nome..."
                                                autocomplete="off"
                                                aria-autocomplete="list"
                                                @input="onBuscaPacienteInput(($event.target as HTMLInputElement).value)"
                                                @focus="pacienteComboAberto = true"
                                                @keydown="onPacienteKeydown"
                                                @blur="onPacienteBlur"
                                            />
                                            <span v-if="buscandoPaciente" class="combo-spinner" aria-hidden="true">
                                                <i class="fa-solid fa-spinner fa-spin"></i>
                                            </span>
                                            <i
                                                v-else-if="pacienteId"
                                                class="fa-solid fa-circle-check combo-check"
                                                aria-hidden="true"
                                            ></i>
                                        </div>

                                        <ul v-show="pacienteComboAberto" class="combo-dropdown" role="listbox">
                                            <li
                                                v-if="buscandoPaciente && pacientesEncontrados.length === 0"
                                                class="combo-estado"
                                            >Buscando...</li>
                                            <template v-else-if="pacientesEncontrados.length > 0">
                                                <li
                                                    v-for="(p, idx) in pacientesEncontrados"
                                                    :key="p.id"
                                                    class="combo-item"
                                                    :class="{ 'combo-item--focado': idx === itemFocado, 'combo-item--ativo': p.id === pacienteId }"
                                                    role="option"
                                                    :aria-selected="p.id === pacienteId"
                                                    @mousedown.prevent="onSelecionarPaciente(p.id)"
                                                >
                                                    <i class="fa-solid fa-user combo-item-icon" aria-hidden="true"></i>
                                                    <span>{{ p.nomeCompleto }}</span>
                                                </li>
                                            </template>
                                            <li v-else class="combo-estado">Nenhum paciente encontrado</li>
                                        </ul>
                                    </div>
                                </template>
                            </AppField>

                            <AppField label="Título (opcional)">
                                <AppInput v-model="titulo" placeholder="Ex.: Cirurgia bariátrica - dr. Silva" />
                            </AppField>
                            <AppField label="Validade *">
                                <AppDatePicker v-model="validade" placeholder="DD/MM/AAAA" />
                            </AppField>
                            <AppField v-if="agendamentoId" label="Agendamento vinculado">
                                <AppInput :model-value="`#${agendamentoId}`" readonly />
                            </AppField>
                        </div>
                        <AppField label="Observações" class="mt-2">
                            <AppTextarea v-model="observacoes" :rows="2" placeholder="Anotações livres" />
                        </AppField>
                    </AppCard>

                    <!-- 2. Cirurgias -->
                    <AppCard title="Cirurgias">
                        <div v-if="falhaCatalogos.cirurgias" class="aviso-falha-catalogo" role="alert">
                            <i class="fa-solid fa-triangle-exclamation"></i>
                            Não foi possível carregar o catálogo de cirurgias. Tente recarregar.
                        </div>
                        <div class="bloco-vazio" v-if="cirurgias.length === 0">
                            <i class="fa-solid fa-scalpel"></i>
                            <p>Adicione pelo menos uma cirurgia.</p>
                        </div>
                        <div class="lista-cirurgias" v-else>
                            <div v-for="(c, idx) in cirurgias" :key="idx" class="linha-cirurgia">
                                <AppField label="Cirurgia" class="campo-flex">
                                    <AppSelect
                                        :model-value="c.catalogoId"
                                        @update:model-value="(v: unknown) => onSelecionarCatCirurgia(idx, Number(v))"
                                    >
                                        <option :value="0">Selecione...</option>
                                        <option
                                            v-for="cir in catCirurgias.filter(x => catalogoDisponivel(x.id, idx))"
                                            :key="cir.id"
                                            :value="cir.id"
                                        >{{ cir.descricao }}</option>
                                    </AppSelect>
                                </AppField>
                                <AppField label="Qtd" class="campo-min">
                                    <AppInput type="number" :min="1" v-model="c.quantidade" />
                                </AppField>
                                <AppField label="Duração" class="campo-min">
                                    <span class="texto-info">
                                        {{ c.duracaoMinutos }} min ({{ fmtMinAsHoras(c.duracaoMinutos) }})
                                    </span>
                                </AppField>
                                <AppField label="Subtotal" class="campo-min">
                                    <span class="texto-info forte">{{ fmt(c.valor * c.quantidade) }}</span>
                                </AppField>
                                <button class="btn-icon btn-icon-excluir" @click="removerCirurgia(idx)" title="Remover">
                                    <i class="fa-solid fa-trash"></i>
                                </button>
                            </div>
                        </div>
                        <div class="acoes-cirurgias">
                            <AppButton size="sm" icon="fa-solid fa-plus" variant="secondary" @click="adicionarCirurgia">
                                Adicionar cirurgia
                            </AppButton>
                        </div>

                        <div v-if="cirurgias.length > 0" class="tempo-card">
                            <div class="tempo-bloco">
                                <span class="tempo-label">Tempo somado</span>
                                <strong>{{ fmtMinAsHoras(tempoAutoMinutos) }} <small>({{ tempoAutoMinutos }} min)</small></strong>
                            </div>
                            <div class="tempo-bloco">
                                <span class="tempo-label">Tempo para cálculo
                                    <em v-if="tempoEditadoManualmente"> (editado)</em>
                                </span>
                                <div class="tempo-input">
                                    <AppInput
                                        type="number"
                                        :min="0"
                                        :model-value="tempoCirurgiaMinutosManual"
                                        @update:model-value="(v: unknown) => onEditarTempoManual(Number(v))"
                                    />
                                    <span class="texto-info">min</span>
                                    <AppButton v-if="tempoEditadoManualmente" size="sm" variant="ghost" @click="resetarTempo">Reset</AppButton>
                                </div>
                            </div>
                            <div class="tempo-bloco">
                                <span class="tempo-label">Tempo final</span>
                                <strong class="forte-destaque">{{ fmtMinAsHoras(tempoFinalMinutos) }}</strong>
                            </div>
                        </div>

                        <!-- Produtos consolidados (read-only, vêm do backend) -->
                        <div v-if="produtosConsolidados.length > 0" class="bloco-produtos">
                            <h4>
                                Produtos das cirurgias
                                <small>({{ produtosConsolidados.length }})</small>
                                <i v-if="carregandoProdutos" class="fa-solid fa-spinner fa-spin"></i>
                            </h4>
                            <table class="tabela">
                                <thead>
                                    <tr><th>Produto</th><th class="r">Qtd</th><th class="r">Unit</th><th class="r">Subtotal</th><th>Origem</th></tr>
                                </thead>
                                <tbody>
                                    <tr v-for="p in produtosConsolidados" :key="p.produtoId">
                                        <td>
                                            {{ p.produtoNome }}
                                            <span v-if="p.usoUnico" class="badge-uso-unico" title="Uso único: maior quantidade entre cirurgias">uso único</span>
                                        </td>
                                        <td class="r">{{ p.quantidade }}</td>
                                        <td class="r">{{ fmt(p.valorUnitario) }}</td>
                                        <td class="r forte">{{ fmt(p.subtotal) }}</td>
                                        <td>{{ p.origemCirurgiaNomes.join(", ") }}</td>
                                    </tr>
                                </tbody>
                                <tfoot>
                                    <tr>
                                        <td colspan="3" class="r forte">Total produtos:</td>
                                        <td class="r forte-destaque">{{ fmt(totalProdutos) }}</td>
                                        <td></td>
                                    </tr>
                                </tfoot>
                            </table>
                        </div>
                    </AppCard>

                    <!-- 3. Profissionais -->
                    <AppCard title="Profissionais">
                        <div v-if="falhaCatalogos.valores" class="aviso-falha-catalogo" role="alert">
                            <i class="fa-solid fa-triangle-exclamation"></i>
                            Não foi possível carregar a tabela de valores profissionais. Tente recarregar.
                        </div>
                        <div v-if="profissionais.length === 0" class="bloco-vazio">
                            <i class="fa-solid fa-user-doctor"></i>
                            <p>Nenhum profissional adicionado.</p>
                        </div>
                        <div v-else class="lista-vertical">
                            <div v-for="(p, idx) in profissionais" :key="idx" class="card-profissional">
                                <div class="fg">
                                    <AppField label="Tabela de valor *">
                                        <AppSelect
                                            :model-value="p.valorProfissionalId"
                                            @update:model-value="(v: unknown) => onSelecionarValorProfissional(idx, Number(v))"
                                        >
                                            <option :value="0">Selecione...</option>
                                            <option v-for="v in catValores" :key="v.id" :value="v.id">
                                                {{ v.funcao }}{{ v.profissionalNome ? ` — ${v.profissionalNome}` : " (padrão)" }} ({{ fmt(Number(v.valorTempoBase)) }})
                                            </option>
                                        </AppSelect>
                                    </AppField>
                                    <AppField label="Tempo (min)">
                                        <AppInput type="number" :min="0" v-model="p.tempoCustomMinutos" />
                                    </AppField>
                                    <AppField label="Quantidade">
                                        <AppInput type="number" :min="1" v-model="p.quantidade" />
                                    </AppField>
                                    <AppField label="Honorário calculado">
                                        <AppInput :model-value="fmt(p.valorCalculado * p.quantidade)" readonly />
                                    </AppField>
                                </div>
                                <div class="acoes-linha">
                                    <button class="btn-icon btn-icon-excluir" @click="removerProfissional(idx)" title="Remover">
                                        <i class="fa-solid fa-trash"></i>
                                    </button>
                                </div>
                            </div>
                        </div>
                        <div class="acoes-cirurgias">
                            <AppButton size="sm" icon="fa-solid fa-plus" variant="secondary" @click="adicionarProfissional">
                                Adicionar profissional
                            </AppButton>
                        </div>
                    </AppCard>

                    <!-- 4. Equipes especializadas (catálogo legado) -->
                    <AppCard title="Equipes especializadas">
                        <div v-if="falhaCatalogos.equipes" class="aviso-falha-catalogo" role="alert">
                            <i class="fa-solid fa-triangle-exclamation"></i>
                            Não foi possível carregar o catálogo de equipes especializadas. Tente recarregar.
                        </div>
                        <div v-if="equipes.length === 0" class="bloco-vazio">
                            <i class="fa-solid fa-users"></i><p>Nenhuma equipe especializada.</p>
                        </div>
                        <div v-else class="lista-vertical">
                            <div v-for="(e, idx) in equipes" :key="idx" class="linha-cirurgia">
                                <AppField label="Equipe">
                                    <AppSelect
                                        :model-value="e.catalogoId"
                                        @update:model-value="(v: unknown) => onSelecionarEquipe(idx, Number(v))"
                                    >
                                        <option :value="0">Selecione...</option>
                                        <option v-for="ec in catEquipes" :key="ec.id" :value="ec.id">{{ ec.descricao }}</option>
                                    </AppSelect>
                                </AppField>
                                <AppField label="Qtd" class="campo-min">
                                    <AppInput type="number" :min="1" v-model="e.quantidade" />
                                </AppField>
                                <AppField label="Unitário" class="campo-min">
                                    <AppInput type="number" :step="0.01" v-model="e.valorUnitario" />
                                </AppField>
                                <button class="btn-icon btn-icon-excluir" @click="removerEquipe(idx)" title="Remover">
                                    <i class="fa-solid fa-trash"></i>
                                </button>
                            </div>
                        </div>
                        <div class="acoes-cirurgias">
                            <AppButton size="sm" icon="fa-solid fa-plus" variant="secondary" @click="adicionarEquipe">
                                Adicionar equipe
                            </AppButton>
                        </div>
                    </AppCard>

                    <!-- 5. Implantes -->
                    <AppCard title="Implantes">
                        <div v-if="falhaCatalogos.implantes" class="aviso-falha-catalogo" role="alert">
                            <i class="fa-solid fa-triangle-exclamation"></i>
                            Não foi possível carregar o catálogo de implantes. Tente recarregar.
                        </div>
                        <div v-if="implantes.length === 0" class="bloco-vazio">
                            <i class="fa-solid fa-microchip"></i><p>Nenhum implante.</p>
                        </div>
                        <div v-else class="lista-vertical">
                            <div v-for="(i, idx) in implantes" :key="idx" class="linha-cirurgia">
                                <AppField label="Implante (catálogo)">
                                    <AppSelect
                                        :model-value="i.catalogoId"
                                        @update:model-value="(v: unknown) => onSelecionarImplante(idx, Number(v))"
                                    >
                                        <option :value="0">Selecione...</option>
                                        <option v-for="ic in catImplantes" :key="ic.id" :value="ic.id">{{ ic.descricao }}</option>
                                    </AppSelect>
                                </AppField>
                                <AppField label="Qtd" class="campo-min">
                                    <AppInput type="number" :min="1" :step="1" v-model="i.quantidade" />
                                </AppField>
                                <AppField label="Unitário" class="campo-min">
                                    <AppInput type="number" :step="0.01" v-model="i.valorUnitario" />
                                </AppField>
                                <button class="btn-icon btn-icon-excluir" @click="removerImplante(idx)" title="Remover">
                                    <i class="fa-solid fa-trash"></i>
                                </button>
                            </div>
                        </div>
                        <div class="acoes-cirurgias">
                            <AppButton size="sm" icon="fa-solid fa-plus" variant="secondary" @click="adicionarImplante">
                                Adicionar implante
                            </AppButton>
                        </div>
                    </AppCard>

                    <!-- 6. Local cirúrgico (5 opções, paridade legado) -->
                    <AppCard title="Local cirúrgico (anestesia + internação)">
                        <div v-if="falhaCatalogos.locais" class="aviso-falha-catalogo" role="alert">
                            <i class="fa-solid fa-triangle-exclamation"></i>
                            Não foi possível carregar as configurações de local cirúrgico. Tente recarregar.
                        </div>
                        <div v-else-if="catLocais.length === 0" class="bloco-aviso">
                            <i class="fa-solid fa-triangle-exclamation"></i>
                            <p>
                                Local cirúrgico não configurado para este estabelecimento.
                                <RouterLink :to="{ name: 'OrcamentoSettings', query: { aba: 'outras' } }">
                                    Configurar agora
                                </RouterLink>
                            </p>
                        </div>
                        <div v-else class="lista-locais">
                            <div
                                v-for="opcao in OPCOES_LOCAL"
                                :key="opcao.tipo"
                                class="opcao-local"
                                :class="{ ativa: localTipo === opcao.tipo, indisponivel: !configLocalPorTipo(opcao.tipo) }"
                                @click="configLocalPorTipo(opcao.tipo) && selecionarLocal(opcao.tipo)"
                            >
                                <div class="opcao-radio">
                                    <span class="dot" :class="{ on: localTipo === opcao.tipo }"></span>
                                </div>
                                <div class="opcao-texto">
                                    <strong>{{ opcao.descricao }}: <span>{{ opcao.label }}</span></strong>
                                    <small>
                                        {{ opcao.comInternacao ? "Valor por tempo" : "Valor fixo" }}
                                        — Base:
                                        {{ configLocalPorTipo(opcao.tipo) ? fmt(Number(configLocalPorTipo(opcao.tipo)!.valorBase)) : "—" }}
                                    </small>
                                </div>
                                <div class="opcao-valor">
                                    <strong v-if="localTipo === opcao.tipo">{{ fmt(valorLocalSnapshot) }}</strong>
                                    <span v-else-if="configLocalPorTipo(opcao.tipo)">{{ fmt(Number(configLocalPorTipo(opcao.tipo)!.valorBase)) }}</span>
                                    <span v-else class="muted">não configurado</span>
                                </div>
                            </div>
                        </div>
                    </AppCard>

                    <!-- 7. Anestesia (separada, opcional) -->
                    <AppCard title="Anestesia (extra opcional)">
                        <p v-if="!anestesia" class="texto-aux">
                            Sem anestesia configurada como item separado.
                            <AppButton size="sm" variant="ghost" @click="alternarAnestesia">Adicionar</AppButton>
                        </p>
                        <template v-else>
                            <div class="fg">
                                <AppField label="Tipo">
                                    <AppSelect v-model="anestesia.tipoAnestesia">
                                        <option v-for="t in TIPOS_ANESTESIA" :key="t" :value="t">{{ t }}</option>
                                    </AppSelect>
                                </AppField>
                                <AppField label="Valor (R$)">
                                    <AppInput type="number" :step="0.01" v-model="anestesia.valor" />
                                </AppField>
                                <AppField label="Observação">
                                    <AppInput v-model="anestesia.observacao" />
                                </AppField>
                            </div>
                            <div class="acoes-cirurgias">
                                <AppButton size="sm" variant="ghost" @click="alternarAnestesia">Remover</AppButton>
                            </div>
                        </template>
                    </AppCard>

                    <!-- 8. Formas de pagamento -->
                    <AppCard title="Formas de pagamento">
                        <div v-if="falhaCatalogos.formasPagamento" class="aviso-falha-catalogo" role="alert">
                            <i class="fa-solid fa-triangle-exclamation"></i>
                            Não foi possível carregar as formas de pagamento. Tente recarregar.
                        </div>
                        <div v-if="formas.length === 0" class="bloco-vazio">
                            <i class="fa-solid fa-credit-card"></i><p>Nenhuma forma de pagamento.</p>
                        </div>
                        <div v-else class="lista-vertical">
                            <div v-for="(f, idx) in formas" :key="idx" class="linha-cirurgia">
                                <AppField label="Forma">
                                    <AppSelect
                                        :model-value="f.formaPagamentoId"
                                        @update:model-value="(v: unknown) => onSelecionarFormaCatalogo(idx, Number(v))"
                                    >
                                        <option :value="0">Selecione...</option>
                                        <option v-for="fp in formasPagamento" :key="fp.id" :value="fp.id">{{ fp.nome }}</option>
                                    </AppSelect>
                                </AppField>
                                <AppField label="Valor" class="campo-min">
                                    <AppInput type="number" :step="0.01" v-model="f.valor" />
                                </AppField>
                                <AppField label="Parcelas" class="campo-min">
                                    <AppInput type="number" :min="1" v-model="f.parcelas" />
                                </AppField>
                                <AppField label="Acréscimo %" class="campo-min">
                                    <AppInput type="number" :step="0.01" v-model="f.acrescimoPercentual" />
                                </AppField>
                                <AppField label="Entrada %" class="campo-min">
                                    <AppInput type="number" :step="0.01" v-model="f.entradaPercentual" />
                                </AppField>
                                <button class="btn-icon btn-icon-excluir" @click="removerForma(idx)" title="Remover">
                                    <i class="fa-solid fa-trash"></i>
                                </button>
                            </div>
                        </div>
                        <div class="acoes-cirurgias">
                            <AppButton size="sm" icon="fa-solid fa-plus" variant="secondary" @click="adicionarForma">
                                Adicionar forma
                            </AppButton>
                        </div>

                        <div v-if="formas.length > 0" class="diferenca-bar" :class="{ ok: integridadeOk }">
                            <span v-if="integridadeOk">
                                <i class="fa-solid fa-circle-check"></i>
                                Soma das formas confere com o total ({{ fmt(somaFormas) }})
                            </span>
                            <span v-else>
                                <i class="fa-solid fa-triangle-exclamation"></i>
                                Faltam {{ fmt(Math.abs(diferenca)) }} para fechar com o total
                                ({{ fmt(somaFormas) }} de {{ fmt(subtotalPreview) }})
                                <AppButton size="sm" variant="ghost" @click="distribuirRestoNaUltimaForma">
                                    Distribuir resto na última forma
                                </AppButton>
                            </span>
                        </div>
                    </AppCard>
                </div>

                <!-- Sidebar -->
                <aside class="col-resumo">
                    <OrcamentoResumoSidebar
                        :subtotal="subtotalPreview"
                        :desconto="desconto"
                        :tipo-desconto="tipoDesconto"
                        :total-geral="totalComDesconto"
                        :soma-formas="somaFormas"
                        :integridade-ok="integridadeOk"
                        :diferenca="diferenca"
                        :salvando="salvando"
                        :calculando="calculando"
                        @update:desconto="desconto = $event"
                        @update:tipo-desconto="tipoDesconto = $event"
                        @salvar="salvar(false)"
                    />

                    <!-- Breakdown rápido -->
                    <div class="breakdown">
                        <div><span>Cirurgias</span><strong>{{ fmt(totalCirurgias) }}</strong></div>
                        <div><span>Profissionais</span><strong>{{ fmt(totalEquipes) }}</strong></div>
                        <div><span>Implantes</span><strong>{{ fmt(totalImplantes) }}</strong></div>
                        <div><span>Local cirúrgico</span><strong>{{ fmt(totalLocal) }}</strong></div>
                        <div><span>Anestesia</span><strong>{{ fmt(totalAnestesia) }}</strong></div>
                    </div>
                </aside>
            </div>
        </template>
    </div>
</template>

<style scoped>
.estado-loading {
    display: flex; align-items: center; gap: 0.5rem;
    color: var(--text-muted); padding: 3rem 0; font-size: 0.9em;
}
.erro-banner {
    display: flex; align-items: center; gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    font-size: 0.875rem;
}

.form-header {
    display: flex; justify-content: space-between; align-items: center;
    gap: 16px; flex-wrap: wrap; margin-bottom: 1rem;
}
.form-header-l { display: flex; align-items: center; gap: 14px; }
.form-header-r { display: flex; gap: 8px; flex-wrap: wrap; }

.btn-back {
    width: 40px; height: 40px; border-radius: 10px;
    background: hsl(var(--card)); border: 1px solid hsl(var(--secondary) / 0.12);
    display: flex; align-items: center; justify-content: center;
    color: hsl(var(--secondary)); cursor: pointer; font-size: 14px;
}
.btn-back:hover { background: hsl(var(--secondary) / 0.04); }
.form-crumb { font-size: 11.5px; color: hsl(var(--secondary) / 0.55); }
.form-titulo { font-size: var(--text-2xl); font-weight: var(--font-weight-bold); margin: 0; }

.form-grid {
    display: grid; grid-template-columns: 1fr 320px; gap: 22px; align-items: start;
}
@media (max-width: 1100px) {
    .form-grid { grid-template-columns: 1fr; }
    .col-resumo { position: static; }
}
.col-principal { display: flex; flex-direction: column; gap: 16px; }
.col-resumo { position: sticky; top: 80px; display: flex; flex-direction: column; gap: 12px; }

.fg {
    display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 0.75rem;
}
.mt-2 { margin-top: 0.5rem; }

.bloco-vazio {
    display: flex; flex-direction: column; align-items: center; gap: 8px;
    padding: 24px; color: hsl(var(--secondary) / 0.6);
    background: hsl(var(--secondary) / 0.04); border-radius: 8px;
}
.bloco-vazio i { font-size: 28px; }
.bloco-vazio p { margin: 0; font-size: 13px; }

.bloco-aviso {
    background: hsl(45 96% 47% / 0.08); border: 1px solid hsl(45 96% 47% / 0.2);
    color: hsl(35 90% 30%); padding: 12px 16px; border-radius: 8px;
    display: flex; align-items: center; gap: 10px;
}
.bloco-aviso a { color: hsl(var(--primary)); text-decoration: underline; }

/* Aviso de falha de rede isolada em um catálogo — degradação graciosa (CA4) */
.aviso-falha-catalogo {
    background: hsl(45 96% 47% / 0.08); border: 1px solid hsl(45 96% 47% / 0.2);
    color: hsl(35 90% 30%); padding: 10px 14px; border-radius: 6px;
    display: flex; align-items: center; gap: 8px;
    font-size: 0.85rem; margin-bottom: 8px;
}

/* Estado de bloqueio de permissão — centrado na área da tela (CA3) */
.bloqueio-permissao {
    display: flex; justify-content: center; align-items: center;
    padding: 4rem 1rem;
}

.lista-cirurgias, .lista-vertical { display: flex; flex-direction: column; gap: 10px; }
.linha-cirurgia {
    display: grid; grid-template-columns: 1fr auto auto auto auto;
    align-items: end; gap: 10px;
    padding: 10px; background: hsl(var(--secondary) / 0.03); border-radius: 6px;
}
.campo-flex { grid-column: 1 / 2; }
.campo-min  { min-width: 90px; }
.texto-info { color: hsl(var(--secondary) / 0.7); font-size: 13px; padding: 8px 0; display: block; }
.texto-info.forte { color: hsl(var(--primary)); font-weight: 600; }
.acoes-cirurgias { display: flex; justify-content: flex-end; margin-top: 10px; }
.acoes-linha { display: flex; justify-content: flex-end; margin-top: 4px; }

.tempo-card {
    margin-top: 12px; padding: 12px; background: hsl(var(--primary) / 0.05);
    border: 1px solid hsl(var(--primary) / 0.15); border-radius: 8px;
    display: flex; gap: 24px; flex-wrap: wrap;
}
.tempo-bloco { display: flex; flex-direction: column; gap: 4px; }
.tempo-label { font-size: 11px; color: hsl(var(--secondary) / 0.6); }
.tempo-label em { color: hsl(45 96% 47%); font-style: normal; }
.tempo-input { display: flex; align-items: center; gap: 6px; }
.forte-destaque { color: hsl(var(--primary)); font-size: 16px; font-weight: 700; }

.bloco-produtos { margin-top: 16px; }
.bloco-produtos h4 {
    margin: 0 0 8px; font-size: 13px; font-weight: 600;
    display: flex; align-items: center; gap: 6px; color: hsl(var(--secondary));
}
.bloco-produtos h4 small { color: hsl(var(--secondary) / 0.6); font-weight: normal; }

.tabela {
    width: 100%; border-collapse: collapse; font-size: 0.85em;
    background: hsl(var(--card)); border-radius: 6px; overflow: hidden;
}
.tabela th, .tabela td {
    padding: 8px 10px; text-align: left; border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.tabela th {
    font-size: 11px; font-weight: 700; text-transform: uppercase;
    letter-spacing: 0.04em; color: hsl(var(--secondary) / 0.6);
    background: hsl(var(--secondary) / 0.04);
}
.tabela .r { text-align: right; }
.tabela .forte { font-weight: 600; }
.tabela tfoot td { border-top: 1px solid hsl(var(--secondary) / 0.15); border-bottom: none; }
.badge-uso-unico {
    background: hsl(199 89% 48% / 0.1); color: hsl(199 89% 35%);
    padding: 2px 6px; border-radius: 4px; font-size: 10px;
    text-transform: uppercase; letter-spacing: 0.02em; margin-left: 6px;
}

.card-profissional {
    padding: 12px; background: hsl(var(--secondary) / 0.03); border-radius: 8px;
}

/* Local cirúrgico (cards radio) */
.lista-locais { display: flex; flex-direction: column; gap: 8px; }
.opcao-local {
    display: flex; align-items: center; gap: 12px; padding: 12px;
    border: 1.5px solid hsl(var(--secondary) / 0.1); border-radius: 8px;
    cursor: pointer; transition: all 0.12s;
}
.opcao-local:hover:not(.indisponivel) { border-color: hsl(var(--primary) / 0.5); }
.opcao-local.ativa {
    border-color: hsl(var(--primary)); background: hsl(var(--primary) / 0.05);
}
.opcao-local.indisponivel { opacity: 0.5; cursor: not-allowed; }
.opcao-radio { flex-shrink: 0; }
.opcao-radio .dot {
    width: 16px; height: 16px; border-radius: 50%;
    border: 2px solid hsl(var(--secondary) / 0.3); display: block;
    transition: all 0.12s;
}
.opcao-radio .dot.on {
    border-color: hsl(var(--primary));
    background: radial-gradient(circle, hsl(var(--primary)) 40%, transparent 40%);
}
.opcao-texto { flex: 1; display: flex; flex-direction: column; gap: 2px; }
.opcao-texto strong span { color: hsl(var(--primary)); }
.opcao-texto small { color: hsl(var(--secondary) / 0.6); font-size: 11px; }
.opcao-valor { text-align: right; font-weight: 600; color: hsl(var(--primary)); }
.opcao-valor .muted { color: hsl(var(--secondary) / 0.4); font-size: 11px; font-weight: 400; }

.diferenca-bar {
    margin-top: 12px; padding: 10px 14px; border-radius: 6px;
    background: hsl(var(--destructive) / 0.08); color: hsl(var(--destructive));
    font-size: 13px; display: flex; align-items: center; gap: 10px;
}
.diferenca-bar.ok {
    background: hsl(160 79% 39% / 0.08); color: hsl(160 79% 28%);
}

/* Combobox de paciente (busca + lista num único controle) */
.paciente-combo { position: relative; }
.combo-input-row { position: relative; display: flex; align-items: center; }
.combo-icon {
    position: absolute; left: 10px; font-size: 12px;
    color: hsl(var(--secondary) / 0.5); pointer-events: none;
}
.combo-input {
    width: 100%; padding: 8px 30px 8px 30px;
    border: 1px solid hsl(var(--secondary) / 0.15); border-radius: 6px;
    font-family: inherit; font-size: 14px; box-sizing: border-box;
}
.combo-input:focus { outline: none; border-color: hsl(var(--primary)); box-shadow: 0 0 0 2px hsl(var(--primary) / 0.2); }
.combo-spinner, .combo-check {
    position: absolute; right: 10px; font-size: 12px; pointer-events: none;
}
.combo-spinner { color: hsl(var(--secondary) / 0.5); }
.combo-check { color: hsl(160 79% 39%); }

.combo-dropdown {
    position: absolute; top: calc(100% + 4px); left: 0; right: 0; z-index: 200;
    margin: 0; padding: 4px 0; list-style: none;
    background: hsl(var(--card)); border: 1px solid hsl(var(--secondary) / 0.12);
    border-radius: 8px; box-shadow: var(--shadow-md, 0 6px 20px rgb(0 0 0 / 0.12));
    max-height: 260px; overflow-y: auto;
}
.combo-item {
    display: flex; align-items: center; gap: 8px;
    padding: 8px 12px; cursor: pointer; font-size: 14px;
}
.combo-item:hover, .combo-item--focado { background: hsl(var(--secondary) / 0.06); }
.combo-item--ativo { color: hsl(var(--primary)); font-weight: 600; }
.combo-item-icon { font-size: 11px; color: hsl(var(--secondary) / 0.45); }
.combo-item--ativo .combo-item-icon { color: hsl(var(--primary)); }
.combo-estado {
    padding: 10px 12px; font-size: 13px; color: hsl(var(--secondary) / 0.6);
}

.texto-aux { color: var(--text-muted); font-size: 0.85em; margin: 0.5rem 0; }

.breakdown {
    background: hsl(var(--card)); border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 8px; padding: 12px; font-size: 13px;
    display: flex; flex-direction: column; gap: 6px;
}
.breakdown div { display: flex; justify-content: space-between; }
.breakdown span { color: hsl(var(--secondary) / 0.7); }
.breakdown strong { font-weight: 600; }
</style>
