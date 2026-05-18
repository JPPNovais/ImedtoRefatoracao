<script setup lang="ts">
/**
 * NovoAgendamentoModal — fluxo completo de criação de agendamento (3 steps).
 * Implementação fiel ao design Anthropic Agenda.html:
 *
 *   Step 1 — Paciente:
 *     - busca por nome/CPF/telefone
 *     - cards de pacientes existentes (selecionar)
 *     - "Cadastrar novo paciente" sticky → abre cadastro rápido inline
 *       (nome + documento + telefone obrigatórios; nascimento + sexo opcionais)
 *
 *   Step 2 — Detalhes:
 *     - paciente fixo no topo
 *     - toggle "Adicionar à lista de espera" → quando ativo, data/hora/duração
 *       ficam opcionais e aparecem campos de "Preferência de período" (Manhã /
 *       Tarde / Qualquer horário) e "Urgência" (Rotina / Prioritário / Urgente)
 *     - profissional, tipo, data, duração, slots de horário, convênio + plano,
 *       motivo, observações, lembretes (WhatsApp / E-mail)
 *
 *   Step 3 — Confirmar:
 *     - card com resumo (variantes normal e lista de espera)
 *     - botão final "Confirmar agendamento" ou "Adicionar à lista de espera"
 */
import { computed, reactive, ref, watch } from "vue"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { vMaska } from "maska/vue"
import {
    agendaService,
    type CriarAgendamentoPayload,
    type DisponibilidadeDia,
} from "@/services/agendaService"
import {
    listaEsperaService,
    type ListaEsperaPrioridade,
    type ListaEsperaPreferenciaPeriodo,
} from "@/services/listaEsperaService"
import { pacienteService, type PacienteListaItem, type PacienteBuscaRapida } from "@/services/pacienteService"
import { salaService, type Sala } from "@/services/salaService"
import { useTenantStore } from "@/stores/tenantStore"
import type { ProfissionalPublico } from "@/services/vinculoService"
import DocumentoPacienteField, { type DocumentoPacienteValue } from "@/components/pacientes/DocumentoPacienteField.vue"
import { AppDatePicker } from "@/components/ui"
import { cpfValido, somenteDigitos } from "@/utils/cpf"

const props = defineProps<{
    aberto: boolean
    profissionais: ProfissionalPublico[]
    /** Data inicial sugerida (ISO YYYY-MM-DD). */
    dataPadrao?: string
    /**
     * Quando definido, pula o passo 1 (paciente já selecionado) e abre direto
     * em "Detalhes". Usado pelo encaixe da lista de espera.
     */
    pacientePreSelecionado?: PacienteListaItem | null
    /** Profissional pré-selecionado (preferência da lista de espera). */
    profissionalPreSelecionadoId?: string | null
    /** Motivo pré-preenchido (vindo da lista de espera). */
    motivoPreSelecionado?: string | null
}>()

const emit = defineEmits<{
    fechar: []
    /** Disparado quando agendamento OU lista de espera é criado com sucesso. */
    criado: [payload: { listaEspera: boolean }]
    /** Quando criou um paciente novo no fluxo (parent atualiza cache). */
    "paciente-criado": [paciente: PacienteListaItem]
}>()

const step = ref<1 | 2 | 3>(1)
const erro = ref<string | null>(null)
const salvando = ref(false)

// ─── Step 1: Paciente ───
type Modo = "search" | "new"
const modo = ref<Modo>("search")
const busca = ref("")
// Aceita tanto o item da busca rápida (autocomplete LGPD-friendly, só
// nome+id) quanto o item completo (recebido via pacientePreSelecionado em
// encaixes de lista de espera).
type PacienteSelecionado = PacienteBuscaRapida | PacienteListaItem
const pacienteSel = ref<PacienteSelecionado | null>(null)

// Paciente novo (cadastro rápido)
const novoPac = reactive({
    nome: "",
    documento: { tipo: "cpf", valor: "" } as DocumentoPacienteValue,
    telefone: "",
    nascimento: "",
    sexo: "",
})

// Busca server-side com debounce (LGPD: o endpoint retorna apenas {id, nome} —
// sem CPF/telefone, espelhando o que o seletor exibe). Sem `q`, retorna os
// últimos cadastrados; com `q`, faz search ILIKE indexado por trigram no nome.
const buscaDeb = useDebouncedRef(busca, 300)
const pacientesEncontrados = ref<PacienteBuscaRapida[]>([])
const buscandoPacientes = ref(false)
// Guard contra race condition: requisições disparadas em sequência (ex: abrir o
// modal + digitar imediatamente) podem retornar fora de ordem. Só aplica o
// resultado da última requisição emitida.
let buscaReqId = 0

async function carregarBusca() {
    if (!props.aberto) return
    const reqId = ++buscaReqId
    buscandoPacientes.value = true
    try {
        const resultado = await pacienteService.buscaRapida(buscaDeb.value, 8)
        if (reqId === buscaReqId) pacientesEncontrados.value = resultado
    } catch {
        if (reqId === buscaReqId) pacientesEncontrados.value = []
    } finally {
        if (reqId === buscaReqId) buscandoPacientes.value = false
    }
}

watch(buscaDeb, carregarBusca)
watch(() => props.aberto, (a) => { if (a) carregarBusca() })

const pacientesFiltrados = computed<PacienteBuscaRapida[]>(() => pacientesEncontrados.value)

// ─── Step 2: Detalhes ───
const TIPOS_CONSULTA = [
    { v: "Consulta", l: "Consulta" },
    { v: "Retorno", l: "Retorno" },
    { v: "Primeira consulta", l: "Primeira vez" },
    { v: "Exame", l: "Exame" },
    { v: "Procedimento", l: "Procedimento" },
    { v: "Teleconsulta", l: "Teleconsulta" },
]

const DURACOES = [15, 20, 30, 45, 60, 90, 120]

const detalhes = reactive({
    profissionalUsuarioId: "" as string,
    tipo: "Consulta",
    data: "",
    duracaoMin: 30,
    hora: "",
    motivo: "",
    observacoes: "",
    salaId: null as number | null,
    lembreteWA: true,
    lembreteEmail: false,
    listaEspera: false,
    preferenciaPeriodo: "Qualquer" as ListaEsperaPreferenciaPeriodo,
    prioridade: "Rotina" as ListaEsperaPrioridade,
})

// ─── Salas ativas (P1.2 — pré-alocação) ───
const tenant = useTenantStore()
const salas = ref<Sala[]>([])

async function garantirSalas() {
    if (salas.value.length > 0) return
    if (!tenant.estabelecimentoAtivoId) return
    try {
        salas.value = await salaService.listar(tenant.estabelecimentoAtivoId, true)
    } catch { /* não crítico */ }
}

// ─── Helpers ───
function reset() {
    const pacientePre = props.pacientePreSelecionado ?? null
    step.value = pacientePre ? 2 : 1
    modo.value = "search"
    busca.value = ""
    pacienteSel.value = pacientePre
    erro.value = null
    Object.assign(novoPac, {
        nome: "",
        documento: { tipo: "cpf", valor: "" } as DocumentoPacienteValue,
        telefone: "",
        nascimento: "",
        sexo: "",
    })
    Object.assign(detalhes, {
        profissionalUsuarioId:
            props.profissionalPreSelecionadoId
            || props.profissionais[0]?.usuarioId
            || "",
        tipo: "Consulta",
        data: props.dataPadrao || new Date().toISOString().slice(0, 10),
        duracaoMin: 30,
        hora: "",
        motivo: props.motivoPreSelecionado ?? "",
        observacoes: "",
        salaId: null,
        lembreteWA: true,
        lembreteEmail: false,
        listaEspera: false,
        preferenciaPeriodo: "Qualquer",
        prioridade: "Rotina",
    })
    void garantirSalas()
}

watch(() => props.aberto, (v) => { if (v) reset() })

// ─── Disponibilidade (slots ocupados / livres) ───
const disponibilidade = ref<DisponibilidadeDia[]>([])
const carregandoSlots = ref(false)
let dispReqId = 0

async function carregarDisponibilidade() {
    if (!detalhes.profissionalUsuarioId || !detalhes.data) {
        disponibilidade.value = []
        return
    }
    const reqId = ++dispReqId
    carregandoSlots.value = true
    try {
        const r = await agendaService.consultarDisponibilidade(
            detalhes.profissionalUsuarioId,
            detalhes.data,
            detalhes.data,
            detalhes.duracaoMin,
        )
        if (reqId === dispReqId) disponibilidade.value = r.dias
    } catch {
        if (reqId === dispReqId) disponibilidade.value = []
    } finally {
        if (reqId === dispReqId) carregandoSlots.value = false
    }
}

// Dia atual da disponibilidade (gerado pelo backend respeitando funcionamento do estabelecimento).
const diaAtual = computed<DisponibilidadeDia | null>(() =>
    disponibilidade.value.find(d => d.data === detalhes.data) ?? null,
)

const slotsDoDia = computed(() => diaAtual.value?.slots ?? [])

const diaFechado = computed(() =>
    !!diaAtual.value && diaAtual.value.status === "fechado",
)

const horasValidas = computed<Set<string>>(() => {
    const set = new Set<string>()
    for (const s of slotsDoDia.value) set.add(s.hora)
    return set
})

watch(
    () => [detalhes.profissionalUsuarioId, detalhes.data, detalhes.duracaoMin, detalhes.listaEspera] as const,
    ([prof, data, _duracao, listaEspera]) => {
        if (!props.aberto) return
        if (listaEspera) { disponibilidade.value = []; return }
        if (!prof || !data) { disponibilidade.value = []; return }
        void carregarDisponibilidade()
    },
)

// Limpa horário selecionado se ele deixou de existir (fora do funcionamento, dia fechado, ocupado).
watch(slotsDoDia, (slots) => {
    if (!detalhes.hora) return
    const slot = slots.find(s => s.hora === detalhes.hora)
    if (!slot || !slot.disponivel) detalhes.hora = ""
})

const pacienteEfetivo = computed(() => {
    if (modo.value === "new") {
        const partes = (novoPac.nome || "").trim().split(/\s+/).filter(Boolean)
        const inic = partes.length === 0
            ? "?"
            : partes.length === 1
                ? partes[0][0]?.toUpperCase() ?? "?"
                : ((partes[0][0] ?? "") + (partes[partes.length - 1][0] ?? "")).toUpperCase()
        return {
            id: 0,
            nome: novoPac.nome,
            documento: novoPac.documento.valor,
            telefone: novoPac.telefone,
            iniciais: inic,
            novo: true,
        }
    }
    if (!pacienteSel.value) return null
    const p = pacienteSel.value
    const partes = p.nomeCompleto.trim().split(/\s+/)
    const inic = partes.length === 1
        ? partes[0][0]?.toUpperCase() ?? "?"
        : ((partes[0][0] ?? "") + (partes[partes.length - 1][0] ?? "")).toUpperCase()
    // PacienteBuscaRapida não traz CPF/telefone (LGPD: seletor não exibe esses
    // campos). Fallback para "" preserva o template comum entre os dois caminhos.
    const completo = p as Partial<PacienteListaItem>
    return {
        id: p.id,
        nome: p.nomeCompleto,
        documento: completo.cpf ?? completo.documentoInternacional ?? "",
        telefone: completo.telefone ?? "",
        iniciais: inic,
        novo: false,
    }
})

// ─── Validações por step ───
const podeStep1 = computed(() => {
    if (modo.value === "new") {
        if (novoPac.nome.trim().length <= 2) return false
        if (novoPac.telefone.replace(/\D/g, "").length < 10) return false

        // Documento eh OPCIONAL. Se preenchido, precisa ser valido (CPF com DV).
        // Detecção de duplicidade fica delegada ao backend (CadastrarPacienteCommand
        // retorna 422 em CPF/documento duplicado) — evita transferir lista de pacientes
        // com PII apenas para checar duplicidade no cliente (LGPD: minimização).
        const valor = novoPac.documento.valor.trim()
        if (!valor) return true

        if (novoPac.documento.tipo === "cpf") {
            if (!cpfValido(valor)) return false
        } else {
            if (valor.length > 30) return false
        }
        return true
    }
    return !!pacienteSel.value
})

const podeStep2 = computed(() => {
    if (!detalhes.profissionalUsuarioId) return false
    if (!detalhes.motivo.trim()) return false
    if (detalhes.listaEspera) return true
    return !!detalhes.data && !!detalhes.hora && detalhes.duracaoMin > 0
})

// ─── Navegação ───
function avancar() {
    if (step.value === 1 && !podeStep1.value) return
    if (step.value === 2 && !podeStep2.value) return
    if (step.value < 3) step.value = (step.value + 1) as 1 | 2 | 3
}
function voltar() {
    if (step.value > 1) step.value = (step.value - 1) as 1 | 2 | 3
}

function selecionarPaciente(p: PacienteSelecionado) {
    pacienteSel.value = p
}

function abrirCadastroNovo() {
    // Pré-preenche nome se a busca tiver letras.
    if (/[a-zA-Z]/.test(busca.value)) novoPac.nome = busca.value.trim()
    modo.value = "new"
}

// ─── Submit final ───
async function confirmar() {
    erro.value = null
    salvando.value = true
    try {
        // 1) Se é paciente novo, cria primeiro.
        let pacienteId = pacienteSel.value?.id ?? 0
        let pacienteCriado: PacienteListaItem | null = null
        if (modo.value === "new") {
            const nomeNovo = novoPac.nome.trim()
            const docValor = novoPac.documento.valor.trim()
            await pacienteService.criar({
                nomeCompleto: nomeNovo,
                cpf: novoPac.documento.tipo === "cpf" && docValor ? docValor : undefined,
                documentoInternacional: novoPac.documento.tipo === "internacional" && docValor ? docValor : undefined,
                telefone: novoPac.telefone || undefined,
                dataNascimento: novoPac.nascimento || undefined,
                genero: novoPac.sexo || undefined,
            })
            // Backend retorna void; busca o paciente recém-criado por nome.
            const pg = await pacienteService.listar(nomeNovo, 1, 5)
            pacienteCriado = pg.itens.find(p => p.nomeCompleto === nomeNovo) ?? pg.itens[0] ?? null
            pacienteId = pacienteCriado?.id ?? 0
            if (pacienteCriado) emit("paciente-criado", pacienteCriado)
        }

        if (!pacienteId) throw new Error("Paciente não identificado.")

        // 2) Modo lista de espera vs agendamento.
        if (detalhes.listaEspera) {
            await listaEsperaService.adicionar({
                pacienteId,
                motivo: detalhes.motivo + (detalhes.observacoes ? ` — ${detalhes.observacoes}` : ""),
                profissionalPreferidoId: detalhes.profissionalUsuarioId || null,
                prioridade: detalhes.prioridade,
                preferenciaPeriodo: detalhes.preferenciaPeriodo,
            })
            emit("criado", { listaEspera: true })
        } else {
            const ini = new Date(`${detalhes.data}T${detalhes.hora}`)
            const fim = new Date(ini.getTime() + detalhes.duracaoMin * 60000)
            const payload: CriarAgendamentoPayload = {
                pacienteId,
                profissionalUsuarioId: detalhes.profissionalUsuarioId,
                inicioPrevisto: ini.toISOString(),
                fimPrevisto: fim.toISOString(),
                tipoServico: detalhes.tipo,
                observacoes: detalhes.observacoes || null,
                salaId: detalhes.salaId,
            }
            await agendaService.criar(payload)
            emit("criado", { listaEspera: false })
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? e?.message ?? "Erro ao criar."
    } finally {
        salvando.value = false
    }
}

// ─── Formatadores ───
function fmtData(iso: string) {
    if (!iso) return "—"
    const [y, m, d] = iso.split("-").map(Number)
    return new Date(y, m - 1, d).toLocaleDateString("pt-BR", {
        weekday: "long", day: "numeric", month: "long", year: "numeric",
    })
}

const PERIODO_LABEL: Record<ListaEsperaPreferenciaPeriodo, string> = {
    Manha: "Manhã",
    Tarde: "Tarde",
    Qualquer: "Qualquer horário",
}
const URGENCIA_LABEL: Record<ListaEsperaPrioridade, string> = {
    Rotina: "Rotina",
    Prioritario: "Prioritário",
    Urgente: "Urgente",
}

const profSelecionado = computed(() =>
    props.profissionais.find(p => p.usuarioId === detalhes.profissionalUsuarioId)
)
</script>

<template>
    <div v-if="aberto" class="modal-overlay" @click="emit('fechar')">
        <div class="modal" @click.stop>
            <header class="modal-head">
                <div>
                    <h2>Novo agendamento</h2>
                    <span>Crie um agendamento em poucos passos</span>
                </div>
                <button type="button" class="modal-close" @click="emit('fechar')">
                    <i class="fa-solid fa-xmark" aria-hidden="true"></i>
                </button>
            </header>

            <!-- Stepper -->
            <div class="stepper">
                <template v-for="(s, i) in [
                    { n: 1, l: 'Paciente' },
                    { n: 2, l: 'Detalhes' },
                    { n: 3, l: 'Confirmar' },
                ]" :key="s.n">
                    <div class="step-pill" :class="{ active: step === s.n, done: step > s.n }">
                        <span class="num">
                            <i v-if="step > s.n" class="fa-solid fa-check" aria-hidden="true"></i>
                            <template v-else>{{ s.n }}</template>
                        </span>
                        <span class="lbl">{{ s.l }}</span>
                    </div>
                    <div v-if="i < 2" class="step-bar" :class="{ done: step > s.n }"></div>
                </template>
            </div>

            <div class="modal-body">
                <!-- ─── Step 1: Paciente ─── -->
                <section v-if="step === 1 && modo === 'search'" class="modal-step patient-step">
                    <div class="search-patient">
                        <i class="fa-solid fa-magnifying-glass" aria-hidden="true"></i>
                        <input
                            type="text"
                            placeholder="Buscar por nome, CPF ou telefone..."
                            v-model="busca"
                            autofocus
                        />
                        <button v-if="busca" type="button" class="clr" @click="busca = ''">
                            <i class="fa-solid fa-xmark" aria-hidden="true"></i>
                        </button>
                    </div>

                    <div class="patient-list">
                        <template v-if="buscandoPacientes && pacientesFiltrados.length === 0">
                            <div class="no-patient">
                                <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
                                <span>Buscando…</span>
                            </div>
                        </template>
                        <template v-else-if="pacientesFiltrados.length === 0">
                            <div class="no-patient">
                                <i class="fa-solid fa-user-slash" aria-hidden="true"></i>
                                <b>Nenhum paciente encontrado</b>
                                <span v-if="busca">"{{ busca }}" não corresponde a nenhum paciente cadastrado.</span>
                                <span v-else>Cadastre o primeiro paciente para começar a agendar.</span>
                                <button type="button" class="btn-primary sm" @click="abrirCadastroNovo">
                                    <i class="fa-solid fa-user-plus" aria-hidden="true"></i> Cadastrar novo paciente
                                </button>
                            </div>
                        </template>
                        <template v-else>
                            <button
                                v-for="p in pacientesFiltrados"
                                :key="p.id"
                                type="button"
                                class="patient-card"
                                :class="{ selected: pacienteSel?.id === p.id }"
                                @click="selecionarPaciente(p)"
                            >
                                <div class="av">{{ ((p.nomeCompleto[0] ?? '?') + (p.nomeCompleto.split(' ').slice(-1)[0]?.[0] ?? '')).toUpperCase() }}</div>
                                <div class="info">
                                    <b>{{ p.nomeCompleto }}</b>
                                </div>
                                <i v-if="pacienteSel?.id === p.id" class="fa-solid fa-circle-check check" aria-hidden="true"></i>
                            </button>
                        </template>
                    </div>

                    <div v-if="pacientesFiltrados.length > 0" class="add-new-sticky">
                        <button type="button" class="add-new-btn" @click="abrirCadastroNovo">
                            <i class="fa-solid fa-user-plus" aria-hidden="true"></i>
                            <div>
                                <b>Cadastrar novo paciente</b>
                                <span>Cadastro rápido com nome, documento e telefone</span>
                            </div>
                            <i class="fa-solid fa-chevron-right arr" aria-hidden="true"></i>
                        </button>
                    </div>
                </section>

                <section v-else-if="step === 1 && modo === 'new'" class="modal-step">
                    <div class="step-info">
                        <i class="fa-solid fa-user-plus" aria-hidden="true"></i>
                        <div>
                            <b>Cadastro rápido de paciente</b>
                            <span>Preencha os dados essenciais agora — o cadastro completo pode ser feito depois.</span>
                        </div>
                    </div>

                    <div class="form-grid">
                        <div class="field-group full">
                            <label>Nome completo <em>*</em></label>
                            <input
                                type="text"
                                placeholder="Ex: Carla Mendes Souza"
                                v-model="novoPac.nome"
                                autofocus
                            />
                        </div>
                        <div class="field-group">
                            <DocumentoPacienteField
                                v-model="novoPac.documento"
                            />
                        </div>
                        <div class="field-group">
                            <label>Telefone <em>*</em></label>
                            <input
                                type="tel"
                                placeholder="(11) 99999-9999"
                                v-model="novoPac.telefone"
                                v-maska="'(##) #####-####'"
                                inputmode="numeric"
                            />
                        </div>
                        <div class="field-group">
                            <label>Data de nascimento <span class="opt">opcional</span></label>
                            <AppDatePicker v-model="novoPac.nascimento" placeholder="DD/MM/AAAA" />
                        </div>
                        <div class="field-group">
                            <label>Sexo <span class="opt">opcional</span></label>
                            <select v-model="novoPac.sexo">
                                <option value="">Selecione</option>
                                <option value="Feminino">Feminino</option>
                                <option value="Masculino">Masculino</option>
                                <option value="Outro">Outro</option>
                            </select>
                        </div>
                    </div>

                    <div class="quick-info">
                        <i class="fa-solid fa-circle-info" aria-hidden="true"></i>
                        O cadastro completo (endereço, convênios, alergias, histórico) poderá ser concluído pelo paciente
                        ao chegar na clínica ou pela secretaria depois.
                    </div>

                    <button type="button" class="link-back" @click="modo = 'search'">
                        <i class="fa-solid fa-arrow-left" aria-hidden="true"></i> Voltar para busca de paciente
                    </button>
                </section>

                <!-- ─── Step 2: Detalhes ─── -->
                <section v-else-if="step === 2 && pacienteEfetivo" class="modal-step">
                    <div class="patient-pinned">
                        <div class="av">{{ pacienteEfetivo.iniciais }}</div>
                        <div class="info">
                            <b>{{ pacienteEfetivo.nome }}</b>
                            <span>
                                {{ pacienteEfetivo.documento || "Sem CPF" }}
                                <template v-if="pacienteEfetivo.telefone"> · {{ pacienteEfetivo.telefone }}</template>
                            </span>
                        </div>
                    </div>

                    <button
                        type="button"
                        class="waitlist-toggle"
                        :class="{ on: detalhes.listaEspera }"
                        @click="detalhes.listaEspera = !detalhes.listaEspera"
                    >
                        <div class="wt-icon">
                            <i :class="['fa-solid', detalhes.listaEspera ? 'fa-hourglass-half' : 'fa-calendar-check']" aria-hidden="true"></i>
                        </div>
                        <div class="wt-info">
                            <b>{{ detalhes.listaEspera ? "Adicionar à lista de espera" : "Agendar para data/horário específico" }}</b>
                            <span>{{
                                detalhes.listaEspera
                                    ? "O paciente aguardará um encaixe — data, horário e duração ficam opcionais."
                                    : "Sem horário disponível? Marque para colocar na lista de espera."
                            }}</span>
                        </div>
                        <div class="wt-switch" :class="{ on: detalhes.listaEspera }">
                            <span class="knob"></span>
                        </div>
                    </button>

                    <div class="form-grid">
                        <div class="field-group">
                            <label>Profissional <em>*</em></label>
                            <select v-model="detalhes.profissionalUsuarioId">
                                <option value="" disabled>Selecione...</option>
                                <option v-for="p in profissionais" :key="p.usuarioId" :value="p.usuarioId">
                                    {{ p.nomeCompleto }}{{ p.especialidade ? ` — ${p.especialidade}` : "" }}
                                </option>
                            </select>
                        </div>

                        <div class="field-group">
                            <label>Tipo de atendimento <em>*</em></label>
                            <select v-model="detalhes.tipo">
                                <option v-for="t in TIPOS_CONSULTA" :key="t.v" :value="t.v">{{ t.l }}</option>
                            </select>
                        </div>

                        <div v-if="!detalhes.listaEspera && salas.length > 0" class="field-group">
                            <label>Sala <span class="opt">opcional</span></label>
                            <select v-model.number="detalhes.salaId">
                                <option :value="null">— Sem sala —</option>
                                <option v-for="s in salas" :key="s.id" :value="s.id">
                                    {{ s.nome }} — {{ s.unidadeNome }}<template v-if="s.tipoSalaNome"> · {{ s.tipoSalaNome }}</template>
                                </option>
                            </select>
                        </div>

                        <div class="field-group">
                            <label>Data
                                <template v-if="detalhes.listaEspera"><span class="opt">opcional</span></template>
                                <em v-else>*</em>
                            </label>
                            <AppDatePicker v-model="detalhes.data" placeholder="DD/MM/AAAA" />
                        </div>

                        <div class="field-group">
                            <label>Duração
                                <template v-if="detalhes.listaEspera"><span class="opt">opcional</span></template>
                                <em v-else>*</em>
                            </label>
                            <select v-model="detalhes.duracaoMin">
                                <option v-for="d in DURACOES" :key="d" :value="d">{{ d }} minutos</option>
                            </select>
                        </div>

                        <div v-if="!detalhes.listaEspera" class="field-group full">
                            <label>Horário disponível <em>*</em></label>
                            <div class="slots-info">
                                <span>
                                    <i class="fa-solid fa-circle slot-dot dot-free" aria-hidden="true"></i>
                                    Vago
                                </span>
                                <span>
                                    <i class="fa-solid fa-circle slot-dot dot-busy" aria-hidden="true"></i>
                                    Ocupado
                                </span>
                                <span v-if="carregandoSlots" class="slots-loading">
                                    <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i> Carregando...
                                </span>
                            </div>
                            <div v-if="diaFechado" class="slots-empty">
                                <i class="fa-solid fa-store-slash" aria-hidden="true"></i>
                                <span>O estabelecimento não funciona nesta data. Escolha outro dia ou ajuste o horário de funcionamento na configuração do estabelecimento.</span>
                            </div>
                            <div v-else-if="slotsDoDia.length === 0 && !carregandoSlots" class="slots-empty">
                                <i class="fa-solid fa-clock" aria-hidden="true"></i>
                                <span>Nenhum horário configurado para esta data.</span>
                            </div>
                            <div v-else class="time-slots">
                                <button
                                    v-for="s in slotsDoDia"
                                    :key="s.hora"
                                    type="button"
                                    class="slot"
                                    :class="{
                                        active: detalhes.hora === s.hora,
                                        busy: !s.disponivel,
                                        free: s.disponivel,
                                    }"
                                    :disabled="!s.disponivel"
                                    :title="s.disponivel
                                        ? 'Disponível'
                                        : (s.motivo === 'bloqueado'
                                            ? 'Horário bloqueado'
                                            : s.motivo === 'passado'
                                                ? 'Horário no passado'
                                                : 'Ocupado')"
                                    @click="s.disponivel && (detalhes.hora = s.hora)"
                                >
                                    {{ s.hora }}
                                    <i
                                        v-if="!s.disponivel"
                                        :class="['fa-solid mark', s.motivo === 'bloqueado'
                                            ? 'fa-ban'
                                            : s.motivo === 'passado'
                                                ? 'fa-clock-rotate-left'
                                                : 'fa-lock']"
                                        aria-hidden="true"
                                    ></i>
                                </button>
                            </div>
                        </div>

                        <div v-if="detalhes.listaEspera" class="field-group full">
                            <label>Preferência de período <span class="opt">opcional</span></label>
                            <div class="period-prefs">
                                <button
                                    v-for="p in [
                                        { v: 'Manha', l: 'Manhã', i: 'fa-sun' },
                                        { v: 'Tarde', l: 'Tarde', i: 'fa-cloud-sun' },
                                        { v: 'Qualquer', l: 'Qualquer horário', i: 'fa-clock' },
                                    ]"
                                    :key="p.v"
                                    type="button"
                                    class="p-pref"
                                    :class="{ active: detalhes.preferenciaPeriodo === p.v }"
                                    @click="detalhes.preferenciaPeriodo = p.v as ListaEsperaPreferenciaPeriodo"
                                >
                                    <i :class="['fa-solid', p.i]" aria-hidden="true"></i> {{ p.l }}
                                </button>
                            </div>
                        </div>

                        <div v-if="detalhes.listaEspera" class="field-group full">
                            <label>Urgência <span class="opt">opcional</span></label>
                            <div class="urgency-row">
                                <button
                                    v-for="u in [
                                        { v: 'Rotina',      l: 'Rotina',      c: 'success' },
                                        { v: 'Prioritario', l: 'Prioritário', c: 'warning' },
                                        { v: 'Urgente',     l: 'Urgente',     c: 'error' },
                                    ]"
                                    :key="u.v"
                                    type="button"
                                    class="urg"
                                    :class="[u.c, { active: detalhes.prioridade === u.v }]"
                                    @click="detalhes.prioridade = u.v as ListaEsperaPrioridade"
                                >
                                    <span class="d"></span> {{ u.l }}
                                </button>
                            </div>
                        </div>

                        <div class="field-group full">
                            <label>Motivo da consulta <em>*</em></label>
                            <input
                                type="text"
                                placeholder="Ex: Dor no peito ao esforço"
                                v-model="detalhes.motivo"
                            />
                        </div>

                        <div class="field-group full">
                            <label>Observações <span class="opt">opcional</span></label>
                            <textarea
                                rows="3"
                                placeholder="Notas internas sobre o atendimento..."
                                v-model="detalhes.observacoes"
                            ></textarea>
                        </div>

                        <div class="field-group full reminder-row">
                            <label>Lembrete automático</label>
                            <div class="reminder-toggles">
                                <label class="tg" :class="{ on: detalhes.lembreteWA }">
                                    <input type="checkbox" v-model="detalhes.lembreteWA" />
                                    <i class="fa-brands fa-whatsapp" aria-hidden="true"></i> WhatsApp
                                </label>
                                <label class="tg" :class="{ on: detalhes.lembreteEmail }">
                                    <input type="checkbox" v-model="detalhes.lembreteEmail" />
                                    <i class="fa-solid fa-envelope" aria-hidden="true"></i> E-mail
                                </label>
                            </div>
                            <span class="hint">Enviado automaticamente 24h antes do atendimento.</span>
                        </div>
                    </div>
                </section>

                <!-- ─── Step 3: Confirmar ─── -->
                <section v-else-if="step === 3 && pacienteEfetivo" class="modal-step">
                    <div class="confirm-card" :class="{ wait: detalhes.listaEspera }">
                        <div class="confirm-head">
                            <template v-if="detalhes.listaEspera">
                                <div class="big-time wait">
                                    <i class="fa-solid fa-hourglass-half" aria-hidden="true"></i>
                                </div>
                                <div class="when">
                                    <b>Lista de espera</b>
                                    <span>Aguardando encaixe · {{ profSelecionado?.nomeCompleto ?? "—" }}</span>
                                </div>
                            </template>
                            <template v-else>
                                <div class="big-time">
                                    <span class="hh">{{ detalhes.hora || "—" }}</span>
                                    <span class="dur">{{ detalhes.duracaoMin }} min</span>
                                </div>
                                <div class="when">
                                    <b>{{ fmtData(detalhes.data) }}</b>
                                    <span>{{ profSelecionado?.nomeCompleto ?? "—" }}</span>
                                </div>
                            </template>
                        </div>

                        <div class="confirm-body">
                            <div class="kv">
                                <span>Paciente</span>
                                <b>
                                    {{ pacienteEfetivo.nome }}
                                    <em v-if="pacienteEfetivo.novo" class="new-tag">novo cadastro</em>
                                </b>
                            </div>
                            <div v-if="pacienteEfetivo.documento" class="kv">
                                <span>Documento</span>
                                <b>{{ pacienteEfetivo.documento }}</b>
                            </div>
                            <div v-if="pacienteEfetivo.telefone" class="kv">
                                <span>Telefone</span>
                                <b>{{ pacienteEfetivo.telefone }}</b>
                            </div>
                            <div class="kv">
                                <span>Tipo</span>
                                <b>{{ TIPOS_CONSULTA.find(t => t.v === detalhes.tipo)?.l ?? detalhes.tipo }}</b>
                            </div>
                            <template v-if="detalhes.listaEspera">
                                <div class="kv">
                                    <span>Preferência</span>
                                    <b>
                                        {{ PERIODO_LABEL[detalhes.preferenciaPeriodo] }}
                                        <template v-if="detalhes.data">· a partir de {{ fmtData(detalhes.data) }}</template>
                                    </b>
                                </div>
                                <div class="kv">
                                    <span>Urgência</span>
                                    <b>
                                        <span class="urg-pill" :class="{
                                            rotina: detalhes.prioridade === 'Rotina',
                                            priori: detalhes.prioridade === 'Prioritario',
                                            urgente: detalhes.prioridade === 'Urgente',
                                        }">{{ URGENCIA_LABEL[detalhes.prioridade] }}</span>
                                    </b>
                                </div>
                            </template>
                            <div class="kv">
                                <span>Motivo</span>
                                <b>{{ detalhes.motivo || "—" }}</b>
                            </div>
                            <div v-if="detalhes.observacoes" class="kv">
                                <span>Observações</span>
                                <b class="notes-b">{{ detalhes.observacoes }}</b>
                            </div>
                            <div class="kv">
                                <span>Lembrete</span>
                                <b>{{
                                    [detalhes.lembreteWA && "WhatsApp", detalhes.lembreteEmail && "E-mail"]
                                        .filter(Boolean).join(" + ") || "Não enviar"
                                }}</b>
                            </div>
                        </div>
                    </div>

                    <div v-if="detalhes.listaEspera" class="confirm-info wait">
                        <i class="fa-solid fa-hourglass-half" aria-hidden="true"></i>
                        <div>
                            <b>Será adicionado à lista de espera.</b>
                            Quando surgir um encaixe compatível com a preferência do paciente, a secretaria
                            será notificada para entrar em contato.
                        </div>
                    </div>
                    <div v-else class="confirm-info">
                        <i class="fa-solid fa-circle-check" aria-hidden="true"></i>
                        Tudo pronto. Ao confirmar, o agendamento será adicionado à agenda{{
                            (detalhes.lembreteWA || detalhes.lembreteEmail)
                                ? " e o lembrete será disparado 24h antes."
                                : " sem envio de lembrete automático."
                        }}
                    </div>

                    <div v-if="erro" class="erro-confirm">{{ erro }}</div>
                </section>
            </div>

            <footer class="modal-foot">
                <button type="button" class="btn-ghost" @click="emit('fechar')">Cancelar</button>
                <div class="spacer"></div>
                <button
                    v-if="step > (pacientePreSelecionado ? 2 : 1)"
                    type="button"
                    class="btn-secondary"
                    @click="voltar"
                >
                    <i class="fa-solid fa-arrow-left" aria-hidden="true"></i> Voltar
                </button>
                <button
                    v-if="step < 3"
                    type="button"
                    class="btn-primary"
                    :disabled="(step === 1 && !podeStep1) || (step === 2 && !podeStep2)"
                    @click="avancar"
                >
                    Avançar <i class="fa-solid fa-arrow-right" aria-hidden="true"></i>
                </button>
                <button
                    v-if="step === 3"
                    type="button"
                    class="btn-primary"
                    :class="{ success: !detalhes.listaEspera }"
                    :disabled="salvando"
                    @click="confirmar"
                >
                    <i :class="['fa-solid', detalhes.listaEspera ? 'fa-hourglass-half' : 'fa-circle-check']" aria-hidden="true"></i>
                    {{ salvando ? "Salvando..." : (detalhes.listaEspera ? "Adicionar à lista de espera" : "Confirmar agendamento") }}
                </button>
            </footer>
        </div>
    </div>
</template>

<style scoped>
/* ─── Overlay + container ─── */
.modal-overlay {
    position: fixed;
    inset: 0;
    z-index: 100;
    background: hsl(var(--primary-dark, 254 56% 21%) / 0.55);
    backdrop-filter: blur(4px);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 32px 16px;
    animation: ovIn 180ms ease-out both;
}
@keyframes ovIn { from { opacity: 0; } to { opacity: 1; } }

.modal {
    background: hsl(var(--card));
    width: 100%;
    max-width: 760px;
    max-height: calc(100vh - 64px);
    border-radius: 18px;
    display: flex;
    flex-direction: column;
    box-shadow: 0 30px 80px hsl(var(--primary-dark, 254 56% 21%) / 0.35);
    overflow: hidden;
    animation: mdIn 220ms cubic-bezier(.2,.8,.2,1) both;
}
@keyframes mdIn { from { opacity: 0; transform: translateY(20px) scale(0.98); } to { opacity: 1; transform: translateY(0) scale(1); } }

.modal-head {
    display: flex;
    align-items: flex-start;
    gap: 14px;
    padding: 22px 26px 16px;
    border-bottom: 1px solid hsl(var(--foreground) / 0.08);
}
.modal-head h2 {
    margin: 0;
    font-size: 20px;
    font-weight: 700;
    color: hsl(var(--primary-dark, 254 56% 21%));
}
.modal-head span {
    font-size: 12px;
    color: hsl(var(--foreground) / 0.65);
}
.modal-close {
    margin-left: auto;
    width: 34px;
    height: 34px;
    background: hsl(var(--foreground) / 0.06);
    border: none;
    border-radius: 10px;
    color: hsl(var(--foreground) / 0.7);
    cursor: pointer;
    font-size: 14px;
    transition: background 0.15s, color 0.15s;
    font-family: inherit;
}
.modal-close:hover {
    background: hsl(0 84% 60% / 0.1);
    color: hsl(0 84% 60%);
}

/* ─── Stepper ─── */
.stepper {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 16px 26px;
    border-bottom: 1px solid hsl(var(--foreground) / 0.08);
    background: hsl(var(--foreground) / 0.02);
}
.step-pill {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 6px 12px 6px 6px;
    border-radius: 99px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--foreground) / 0.1);
    font-size: 12px;
    font-weight: 500;
    color: hsl(var(--foreground) / 0.65);
    transition: all 0.15s;
}
.step-pill .num {
    width: 22px;
    height: 22px;
    border-radius: 50%;
    background: hsl(var(--foreground) / 0.1);
    color: hsl(var(--foreground) / 0.7);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 11px;
    font-weight: 700;
}
.step-pill.active {
    background: hsl(var(--primary, 254 56% 38%) / 0.1);
    border-color: hsl(var(--primary, 254 56% 38%) / 0.3);
    color: hsl(var(--primary-dark, 254 56% 21%));
}
.step-pill.active .num {
    background: hsl(var(--primary, 254 56% 38%));
    color: white;
}
.step-pill.done {
    background: hsl(160 79% 39% / 0.1);
    border-color: hsl(160 79% 39% / 0.25);
    color: hsl(160 79% 30%);
}
.step-pill.done .num {
    background: hsl(160 79% 39%);
    color: white;
    font-size: 10px;
}
.step-bar {
    flex: 1;
    height: 2px;
    background: hsl(var(--foreground) / 0.1);
    border-radius: 1px;
    transition: background 0.15s;
}
.step-bar.done { background: hsl(160 79% 39%); }

/* ─── Body ─── */
.modal-body {
    padding: 22px 26px;
    flex: 1;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    min-height: 0;
}
.modal-step {
    display: flex;
    flex-direction: column;
    gap: 16px;
}

/* ─── Step 1: Paciente ─── */
.patient-step { flex: 1; min-height: 0; gap: 12px; }
.search-patient {
    display: flex;
    align-items: center;
    gap: 10px;
    height: 44px;
    padding: 0 14px;
    background: hsl(var(--foreground) / 0.04);
    border: 1px solid hsl(var(--foreground) / 0.1);
    border-radius: 12px;
}
.search-patient i { color: hsl(var(--foreground) / 0.5); }
.search-patient input {
    flex: 1;
    border: none;
    outline: none;
    background: transparent;
    font-size: 14px;
    color: hsl(var(--primary-dark, 254 56% 21%));
    font-family: inherit;
}
.search-patient input::placeholder { color: hsl(var(--foreground) / 0.5); }
.search-patient .clr {
    width: 24px;
    height: 24px;
    border: none;
    border-radius: 50%;
    background: hsl(var(--foreground) / 0.1);
    color: hsl(var(--foreground) / 0.6);
    cursor: pointer;
    font-family: inherit;
}

.patient-list {
    flex: 1;
    overflow-y: auto;
    padding-right: 4px;
    margin-right: -4px;
    display: flex;
    flex-direction: column;
    gap: 8px;
}
.patient-card {
    display: grid;
    grid-template-columns: 44px 1fr auto;
    gap: 14px;
    align-items: center;
    padding: 12px 14px;
    border-radius: 12px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--foreground) / 0.1);
    cursor: pointer;
    text-align: left;
    font-family: inherit;
    transition: border 0.15s, background 0.15s, box-shadow 0.15s;
}
.patient-card:hover {
    border-color: hsl(var(--primary, 254 56% 38%) / 0.4);
    background: hsl(var(--primary, 254 56% 38%) / 0.03);
}
.patient-card.selected {
    border-color: hsl(var(--primary, 254 56% 38%));
    background: hsl(var(--primary, 254 56% 38%) / 0.06);
    box-shadow: 0 0 0 3px hsl(var(--primary, 254 56% 38%) / 0.12);
}
.patient-card .av {
    width: 44px;
    height: 44px;
    border-radius: 50%;
    background: linear-gradient(135deg, hsl(var(--primary, 254 56% 38%)) 0%, hsl(var(--primary-dark, 254 56% 21%)) 100%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 14px;
}
.patient-card .info {
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 0;
}
.patient-card .info b {
    font-size: 14px;
    color: hsl(var(--primary-dark, 254 56% 21%));
    font-weight: 600;
}
.patient-card .info .meta {
    display: flex;
    align-items: center;
    gap: 6px;
    flex-wrap: wrap;
    font-size: 11px;
    color: hsl(var(--foreground) / 0.65);
}
.patient-card .info .meta i { font-size: 10px; margin-right: 3px; }
.patient-card .info .meta .dotsep {
    width: 3px;
    height: 3px;
    border-radius: 50%;
    background: hsl(var(--foreground) / 0.3);
}
.patient-card .check {
    color: hsl(var(--primary, 254 56% 38%));
    font-size: 18px;
}

.add-new-sticky {
    flex-shrink: 0;
    padding-top: 12px;
    border-top: 1px solid hsl(var(--foreground) / 0.08);
    margin-top: 4px;
}
.add-new-btn {
    display: grid;
    grid-template-columns: 44px 1fr auto;
    gap: 14px;
    align-items: center;
    padding: 14px;
    border-radius: 12px;
    background: hsl(160 79% 39% / 0.06);
    border: 1px dashed hsl(160 79% 39% / 0.4);
    cursor: pointer;
    text-align: left;
    font-family: inherit;
    transition: all 0.15s;
    width: 100%;
    box-sizing: border-box;
}
.add-new-btn:hover {
    background: hsl(160 79% 39% / 0.12);
    border-style: solid;
}
.add-new-btn > i:first-child {
    width: 44px;
    height: 44px;
    border-radius: 50%;
    background: hsl(160 79% 39%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 16px;
}
.add-new-btn > div { display: flex; flex-direction: column; gap: 2px; }
.add-new-btn b {
    font-size: 13px;
    color: hsl(var(--primary-dark, 254 56% 21%));
    font-weight: 600;
}
.add-new-btn span {
    font-size: 11px;
    color: hsl(var(--foreground) / 0.65);
}
.add-new-btn .arr { color: hsl(160 79% 39%); }

.no-patient {
    text-align: center;
    padding: 30px 20px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
}
.no-patient > i { font-size: 30px; color: hsl(var(--foreground) / 0.25); }
.no-patient b { font-size: 14px; color: hsl(var(--primary-dark, 254 56% 21%)); }
.no-patient span { font-size: 12px; color: hsl(var(--foreground) / 0.6); }

/* ─── Step 1 cadastro novo ─── */
.step-info {
    display: flex;
    gap: 12px;
    align-items: flex-start;
    padding: 14px;
    border-radius: 12px;
    background: hsl(199 89% 48% / 0.06);
    border: 1px solid hsl(199 89% 48% / 0.2);
}
.step-info > i {
    width: 36px;
    height: 36px;
    border-radius: 10px;
    background: hsl(199 89% 48%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 14px;
    flex-shrink: 0;
}
.step-info b {
    display: block;
    font-size: 13px;
    color: hsl(var(--primary-dark, 254 56% 21%));
    margin-bottom: 2px;
}
.step-info span { font-size: 12px; color: hsl(var(--foreground) / 0.7); }

.quick-info {
    font-size: 11px;
    color: hsl(var(--foreground) / 0.6);
    padding: 10px 12px;
    background: hsl(var(--foreground) / 0.03);
    border-radius: 8px;
    display: flex;
    gap: 8px;
    align-items: flex-start;
    line-height: 1.5;
}
.quick-info i { color: hsl(var(--foreground) / 0.5); margin-top: 2px; }

.link-back {
    background: none;
    border: 0;
    padding: 6px 0;
    color: hsl(var(--primary, 254 56% 38%));
    cursor: pointer;
    font-size: 12px;
    font-weight: 600;
    align-self: flex-start;
    font-family: inherit;
}
.link-back:hover { text-decoration: underline; }

/* ─── Form grid ─── */
.form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 14px;
}
.field-group {
    display: flex;
    flex-direction: column;
    gap: 6px;
    min-width: 0;
}
.field-group.full { grid-column: 1 / -1; }
.field-group label {
    font-size: 11px;
    font-weight: 600;
    color: hsl(var(--foreground) / 0.7);
    display: inline-flex;
    align-items: center;
    gap: 4px;
}
.field-group label em {
    color: hsl(0 84% 60%);
    font-style: normal;
    margin-left: 2px;
    font-weight: 700;
}
.field-group label .opt {
    font-size: 10px;
    font-style: italic;
    font-weight: 400;
    color: hsl(var(--foreground) / 0.5);
    margin-left: 4px;
}
.field-group input,
.field-group select,
.field-group textarea {
    height: 40px;
    padding: 0 12px;
    border: 1px solid hsl(var(--foreground) / 0.12);
    border-radius: 10px;
    font-size: 13px;
    font-family: inherit;
    color: hsl(var(--foreground));
    background: hsl(var(--card));
    outline: none;
    transition: border 0.15s, box-shadow 0.15s;
}
.field-group textarea {
    resize: vertical;
    min-height: 76px;
    padding-top: 10px;
}
.field-group input:focus,
.field-group select:focus,
.field-group textarea:focus {
    border-color: hsl(var(--primary, 254 56% 38%));
    box-shadow: 0 0 0 3px hsl(var(--primary, 254 56% 38%) / 0.12);
}

/* ─── Step 2 helpers ─── */
.patient-pinned {
    display: flex;
    gap: 12px;
    align-items: center;
    padding: 12px 14px;
    background: hsl(var(--primary, 254 56% 38%) / 0.06);
    border: 1px solid hsl(var(--primary, 254 56% 38%) / 0.2);
    border-radius: 10px;
}
.patient-pinned .av {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    background: linear-gradient(135deg, hsl(var(--primary, 254 56% 38%)) 0%, hsl(var(--primary-dark, 254 56% 21%)) 100%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 12px;
}
.patient-pinned .info b {
    display: block;
    font-size: 14px;
    color: hsl(var(--primary-dark, 254 56% 21%));
}
.patient-pinned .info span {
    font-size: 11px;
    color: hsl(var(--foreground) / 0.7);
}

/* Toggle Lista de espera */
.waitlist-toggle {
    display: grid;
    grid-template-columns: 44px 1fr auto;
    gap: 12px;
    align-items: center;
    padding: 12px 14px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--foreground) / 0.12);
    border-radius: 12px;
    cursor: pointer;
    text-align: left;
    font-family: inherit;
    transition: border 0.15s, background 0.15s;
}
.waitlist-toggle:hover { border-color: hsl(45 96% 47% / 0.4); }
.waitlist-toggle.on {
    background: hsl(45 96% 47% / 0.06);
    border-color: hsl(45 96% 47% / 0.4);
}
.wt-icon {
    width: 44px;
    height: 44px;
    border-radius: 10px;
    background: hsl(var(--foreground) / 0.06);
    color: hsl(var(--foreground) / 0.6);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 16px;
    transition: all 0.15s;
}
.waitlist-toggle.on .wt-icon {
    background: hsl(45 96% 47%);
    color: white;
}
.wt-info b {
    display: block;
    font-size: 13px;
    color: hsl(var(--primary-dark, 254 56% 21%));
    font-weight: 600;
}
.wt-info span {
    font-size: 11px;
    color: hsl(var(--foreground) / 0.65);
    line-height: 1.4;
}
.waitlist-toggle.on .wt-info b { color: hsl(35 90% 30%); }
.wt-switch {
    width: 36px;
    height: 20px;
    background: hsl(var(--foreground) / 0.15);
    border-radius: 999px;
    position: relative;
    transition: background 0.2s;
}
.wt-switch .knob {
    position: absolute;
    top: 2px;
    left: 2px;
    width: 16px;
    height: 16px;
    background: hsl(var(--card));
    border-radius: 50%;
    box-shadow: 0 1px 4px hsl(0 0% 0% / 0.2);
    transition: transform 0.2s;
}
.wt-switch.on { background: hsl(45 96% 47%); }
.wt-switch.on .knob { transform: translateX(16px); }

/* Slots */
.time-slots {
    display: flex;
    gap: 6px;
    flex-wrap: wrap;
}
.time-slots .slot {
    padding: 8px 12px;
    border-radius: 8px;
    border: 1px solid hsl(var(--foreground) / 0.12);
    background: hsl(var(--card));
    color: hsl(var(--foreground));
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    font-family: inherit;
    transition: all 0.15s;
    font-variant-numeric: tabular-nums;
}
.time-slots .slot:hover {
    border-color: hsl(var(--primary, 254 56% 38%));
    color: hsl(var(--primary-dark, 254 56% 21%));
}
.time-slots .slot.active {
    background: hsl(var(--primary, 254 56% 38%));
    color: white;
    border-color: hsl(var(--primary, 254 56% 38%));
}
.time-slots .slot.busy {
    background: hsl(var(--foreground) / 0.04);
    color: hsl(var(--foreground) / 0.4);
    cursor: not-allowed;
    border-color: hsl(var(--foreground) / 0.08);
}
.time-slots .slot.busy:hover {
    border-color: hsl(var(--foreground) / 0.08);
    color: hsl(var(--foreground) / 0.4);
    background: hsl(var(--foreground) / 0.04);
}
.time-slots .slot .mark { font-size: 9px; margin-left: 4px; }

/* Estado vazio (dia fechado / sem horários) */
.slots-empty {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 14px 16px;
    background: hsl(var(--foreground) / 0.03);
    border: 1px dashed hsl(var(--foreground) / 0.15);
    border-radius: 10px;
    font-size: 12px;
    color: hsl(var(--foreground) / 0.7);
    line-height: 1.5;
}
.slots-empty i { color: hsl(var(--foreground) / 0.45); font-size: 16px; flex-shrink: 0; }

/* Legenda dos slots */
.slots-info {
    display: flex;
    gap: 14px;
    flex-wrap: wrap;
    font-size: 11px;
    color: hsl(var(--foreground) / 0.65);
    margin-bottom: 6px;
}
.slots-info span { display: inline-flex; align-items: center; gap: 5px; }
.slots-info .slots-loading { color: hsl(var(--primary, 254 56% 38%)); }
.slot-dot { font-size: 8px; }
.dot-free { color: hsl(160 79% 39%); }
.dot-busy { color: hsl(var(--foreground) / 0.3); }

/* Period prefs */
.period-prefs {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}
.p-pref {
    padding: 8px 14px;
    border-radius: 999px;
    border: 1px solid hsl(var(--foreground) / 0.12);
    background: hsl(var(--card));
    font-size: 12px;
    font-weight: 600;
    color: hsl(var(--foreground));
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
}
.p-pref:hover { border-color: hsl(var(--primary, 254 56% 38%) / 0.4); }
.p-pref.active {
    background: hsl(var(--primary, 254 56% 38%) / 0.1);
    border-color: hsl(var(--primary, 254 56% 38%));
    color: hsl(var(--primary-dark, 254 56% 21%));
}

/* Urgency */
.urgency-row { display: flex; gap: 8px; }
.urgency-row .urg {
    flex: 1;
    padding: 10px 14px;
    border-radius: 10px;
    border: 1px solid hsl(var(--foreground) / 0.12);
    background: hsl(var(--card));
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 8px;
    color: hsl(var(--foreground));
}
.urgency-row .urg .d {
    width: 8px;
    height: 8px;
    border-radius: 50%;
}
.urgency-row .urg.success .d { background: hsl(160 79% 39%); }
.urgency-row .urg.warning .d { background: hsl(45 96% 47%); }
.urgency-row .urg.error .d { background: hsl(0 84% 60%); }
.urgency-row .urg.success.active { background: hsl(160 79% 39% / 0.1); border-color: hsl(160 79% 39%); }
.urgency-row .urg.warning.active { background: hsl(45 96% 47% / 0.1); border-color: hsl(45 96% 47%); }
.urgency-row .urg.error.active { background: hsl(0 84% 60% / 0.1); border-color: hsl(0 84% 60%); }

/* Reminder */
.reminder-row .reminder-toggles {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}
.reminder-row .tg {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 8px 14px;
    border-radius: 999px;
    border: 1px solid hsl(var(--foreground) / 0.12);
    background: hsl(var(--card));
    cursor: pointer;
    font-size: 12px;
    font-weight: 600;
    color: hsl(var(--foreground) / 0.7);
    transition: all 0.15s;
}
.reminder-row .tg input {
    appearance: none;
    width: 14px;
    height: 14px;
    border: 1.5px solid hsl(var(--foreground) / 0.2);
    border-radius: 4px;
    margin: 0;
}
.reminder-row .tg.on {
    background: hsl(var(--primary, 254 56% 38%) / 0.08);
    border-color: hsl(var(--primary, 254 56% 38%) / 0.5);
    color: hsl(var(--primary-dark, 254 56% 21%));
}
.reminder-row .tg.on input {
    background: hsl(var(--primary, 254 56% 38%));
    border-color: hsl(var(--primary, 254 56% 38%));
}
.reminder-row .hint {
    font-size: 11px;
    color: hsl(var(--foreground) / 0.55);
    margin-top: 4px;
}

/* ─── Step 3 Confirm ─── */
.confirm-card {
    border-radius: 16px;
    overflow: hidden;
    background: linear-gradient(135deg, hsl(var(--primary, 254 56% 38%)) 0%, hsl(var(--primary-dark, 254 56% 21%)) 100%);
    color: white;
    box-shadow: 0 8px 28px hsl(var(--primary-dark, 254 56% 21%) / 0.25);
}
.confirm-card.wait {
    background: linear-gradient(135deg, hsl(45 96% 47%) 0%, hsl(28 90% 40%) 100%);
}
.confirm-head {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 18px 22px;
}
.big-time {
    width: 88px;
    text-align: center;
    background: hsl(0 0% 100% / 0.12);
    border-radius: 12px;
    padding: 10px 6px;
}
.big-time.wait {
    width: 64px;
    height: 64px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 28px;
    padding: 0;
}
.big-time .hh {
    display: block;
    font-size: 24px;
    font-weight: 800;
    line-height: 1;
    font-variant-numeric: tabular-nums;
}
.big-time .dur {
    display: block;
    font-size: 10px;
    margin-top: 4px;
    opacity: 0.85;
}
.when b {
    display: block;
    font-size: 16px;
    font-weight: 700;
    text-transform: capitalize;
}
.when span {
    display: block;
    font-size: 12px;
    opacity: 0.85;
    margin-top: 4px;
}

.confirm-body {
    background: hsl(var(--card));
    color: hsl(var(--foreground));
    padding: 16px 22px;
    display: flex;
    flex-direction: column;
    gap: 8px;
}
.kv {
    display: grid;
    grid-template-columns: 110px 1fr;
    gap: 10px;
    font-size: 12px;
}
.kv span {
    color: hsl(var(--foreground) / 0.55);
    font-weight: 500;
}
.kv b {
    color: hsl(var(--primary-dark, 254 56% 21%));
    font-weight: 600;
}
.notes-b {
    white-space: pre-wrap;
}
.new-tag {
    display: inline-block;
    background: hsl(160 79% 39% / 0.14);
    color: hsl(160 79% 30%);
    font-size: 10px;
    font-weight: 700;
    padding: 2px 6px;
    border-radius: 999px;
    margin-left: 6px;
    font-style: normal;
}
.urg-pill {
    display: inline-flex;
    align-items: center;
    padding: 2px 8px;
    border-radius: 999px;
    font-size: 11px;
    font-weight: 700;
}
.urg-pill.rotina { background: hsl(160 79% 39% / 0.14); color: hsl(160 79% 28%); }
.urg-pill.priori { background: hsl(45 96% 47% / 0.18); color: hsl(28 90% 30%); }
.urg-pill.urgente { background: hsl(0 84% 60% / 0.14); color: hsl(0 84% 50%); }

.confirm-info {
    display: flex;
    gap: 10px;
    align-items: flex-start;
    padding: 12px 14px;
    background: hsl(160 79% 39% / 0.06);
    border: 1px solid hsl(160 79% 39% / 0.2);
    border-radius: 10px;
    font-size: 12px;
    color: hsl(var(--foreground) / 0.75);
    line-height: 1.5;
}
.confirm-info i {
    color: hsl(160 79% 39%);
    margin-top: 2px;
    font-size: 14px;
}
.confirm-info.wait {
    background: hsl(45 96% 47% / 0.08);
    border-color: hsl(45 96% 47% / 0.3);
}
.confirm-info.wait i { color: hsl(28 90% 50%); }
.confirm-info.wait b {
    display: block;
    color: hsl(var(--primary-dark, 254 56% 21%));
    margin-bottom: 4px;
    font-size: 13px;
}

.erro-confirm {
    background: hsl(0 84% 60% / 0.08);
    border: 1px solid hsl(0 84% 60% / 0.2);
    border-radius: 8px;
    padding: 10px 12px;
    color: hsl(0 84% 50%);
    font-size: 12px;
}

/* ─── Footer ─── */
.modal-foot {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 14px 22px;
    border-top: 1px solid hsl(var(--foreground) / 0.08);
    background: hsl(var(--card));
}
.spacer { flex: 1; }
.btn-primary,
.btn-secondary,
.btn-ghost {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 10px 18px;
    border-radius: 10px;
    font-size: 13px;
    font-weight: 700;
    cursor: pointer;
    font-family: inherit;
    border: 1px solid transparent;
    transition: all 0.15s;
}
.btn-primary {
    background: hsl(var(--primary, 254 56% 38%));
    color: white;
}
.btn-primary:hover:not(:disabled) {
    background: hsl(var(--primary-dark, 254 56% 21%));
}
.btn-primary:disabled {
    opacity: 0.45;
    cursor: not-allowed;
}
.btn-primary.success {
    background: hsl(160 79% 39%);
}
.btn-primary.success:hover:not(:disabled) {
    background: hsl(160 79% 32%);
}
.btn-primary.sm {
    padding: 7px 14px;
    font-size: 12px;
}
.btn-secondary {
    background: hsl(var(--card));
    color: hsl(var(--foreground));
    border-color: hsl(var(--foreground) / 0.18);
}
.btn-secondary:hover {
    background: hsl(var(--foreground) / 0.04);
}
.btn-ghost {
    background: transparent;
    color: hsl(var(--foreground) / 0.65);
}
.btn-ghost:hover {
    background: hsl(var(--foreground) / 0.05);
}

@media (max-width: 720px) {
    .modal { max-height: 100vh; height: 100vh; border-radius: 0; }
    .form-grid { grid-template-columns: 1fr; }
    .urgency-row { flex-direction: column; }
    .kv { grid-template-columns: 1fr; }
    .kv span { font-size: 10px; text-transform: uppercase; }
}
</style>
