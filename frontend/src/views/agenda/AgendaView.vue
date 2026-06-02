<script setup lang="ts">
/**
 * AgendaView — visão diária de agendamentos com strip de 14 dias e cards de
 * status clicáveis (estilo design Anthropic).
 *
 * Layout:
 *   - Header com botão "Novo agendamento"
 *   - DateStrip (14 dias) com contagens
 *   - Filtros: profissional + especialidade
 *   - Cards de status (filtros)
 *   - Lista de agendamentos do dia (AgendamentoRow)
 *   - Modal de criação: NovoAgendamentoModal (3 steps + cadastro rápido + lista de espera)
 *   - Modal de edição/reagendamento: EditarAgendamentoModal (modal único com toggle inline)
 */
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue"
import {
    AppButton, AppCard, AppPageHeader, AppEmptyState, AppSelect,
    AppDateStrip, AppStatCard, AppField, AppInput, AppAvatarSelect,
} from "@/components/ui"
import AgendamentoRow from "@/components/agenda/AgendamentoRow.vue"
import AgendaRail from "@/components/agenda/AgendaRail.vue"
import NovoAgendamentoModal from "@/components/agenda/NovoAgendamentoModal.vue"
import EditarAgendamentoModal from "@/components/agenda/EditarAgendamentoModal.vue"
import CheckInModal from "@/components/agenda/CheckInModal.vue"
import CancelarAgendamentoModal from "@/components/agenda/CancelarAgendamentoModal.vue"
import PacienteFormModal from "@/components/pacientes/PacienteFormModal.vue"
import { agendaService, type Agendamento } from "@/services/agendaService"
import { salaService, type Sala } from "@/services/salaService"
import { listaEsperaService, type ListaEsperaItem } from "@/services/listaEsperaService"
import { pacienteService, type Paciente, type PacienteListaItem } from "@/services/pacienteService"
import { vinculoService, type ProfissionalPublico } from "@/services/vinculoService"
import { profissionalService } from "@/services/profissionalService"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { dataISO, formatHora, hojeISO } from "@/utils/datetime"

const auth = useAuthStore()
const tenant = useTenantStore()

// ─── Data selecionada ───
// Usa "hoje em Brasília" como fonte da verdade — independente do fuso do navegador
// do usuário. Backend, banco e front falam o mesmo dia.
function toISO(d: Date) {
    return dataISO(d)
}

const dataSel = ref(hojeISO())

// ─── Relógio para marcação "AGORA" (só renderizada quando dataSel é hoje) ───
const agora = ref(new Date())
let agoraTimer: number | null = null

const isHoje = computed(() => dataSel.value === hojeISO())

const horaAgoraLabel = computed(() => formatHora(agora.value))

// ─── Estado base ───
const agendamentos = ref<Agendamento[]>([])
// Usa o DTO publico/minimizado — sem e-mail, sem modelo de permissao, sem
// datas de convite. A Agenda so precisa de nome + especialidade + status.
const profissionais = ref<ProfissionalPublico[]>([])
const pacientes = ref<PacienteListaItem[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)
const perfilProprio = ref<{ especialidade: string | null; conselho: string; fotoUrl: string | null } | null>(null)

// Filtros
const filtroProf = ref("")
const filtroEspec = ref("")
const filtroStatus = ref<Agendamento["status"] | null>(null)
const filtroSalaId = ref<string>("")
const buscaTexto = ref("")

// Salas ativas — carregadas no mount para o filtro.
const salas = ref<Sala[]>([])

// Especialidade efetiva de um profissional (Dono incluso).
function especialidadeDoProfissional(usuarioId: string): string {
    const p = profissionais.value.find(x => x.usuarioId === usuarioId)
    if (p?.especialidade?.trim()) return p.especialidade.trim()
    if (tenant.papel === "Dono" && auth.usuario?.id === usuarioId) {
        return (perfilProprio.value?.especialidade ?? "").trim()
    }
    return ""
}

const profissionaisDisponiveis = computed<ProfissionalPublico[]>(() => {
    const lista = [...profissionais.value]
    if (tenant.papel === "Dono" && auth.usuario && !lista.some(p => p.usuarioId === auth.usuario!.id)) {
        // Fallback de borda: backend deve trazer o Dono na lista publica, mas
        // se algo falhar nessa borda (cache desatualizado, etc.) garantimos
        // que o proprio Dono aparece. Sem expor email (uso apenas nomeCompleto).
        lista.unshift({
            usuarioId: auth.usuario.id,
            nomeCompleto: auth.usuario.nomeCompleto ?? auth.usuario.email,
            status: "Dono",
            especialidade: perfilProprio.value?.especialidade ?? null,
            conselho: perfilProprio.value?.conselho ?? null,
            fotoUrl: perfilProprio.value?.fotoUrl ?? null,
        })
    }
    return lista
})

const especialidadesDisponiveis = computed<string[]>(() => {
    const set = new Set<string>()
    for (const p of profissionaisDisponiveis.value) {
        if (p.especialidade?.trim()) set.add(p.especialidade.trim())
    }
    return [...set].sort((a, b) => a.localeCompare(b, "pt-BR"))
})

// ─── Filtragem ───
const baseFiltrada = computed<Agendamento[]>(() => {
    return agendamentos.value.filter(a => {
        if (filtroProf.value && a.profissionalUsuarioId !== filtroProf.value) return false
        if (filtroEspec.value && especialidadeDoProfissional(a.profissionalUsuarioId) !== filtroEspec.value) return false
        if (filtroSalaId.value && String(a.salaId) !== filtroSalaId.value) return false
        return true
    })
})

const doDia = computed<Agendamento[]>(() =>
    baseFiltrada.value.filter(a => a.inicioPrevisto.startsWith(dataSel.value))
)

const doDiaFiltrado = computed<Agendamento[]>(() => {
    let arr = doDia.value
    if (filtroStatus.value) {
        arr = arr.filter(a => a.status === filtroStatus.value)
    }
    if (buscaTexto.value.trim()) {
        const q = buscaTexto.value.trim().toLowerCase()
        arr = arr.filter(a =>
            a.pacienteNome.toLowerCase().includes(q)
            || a.profissionalNome.toLowerCase().includes(q)
            || a.tipoServico.toLowerCase().includes(q),
        )
    }
    return arr.sort((a, b) => a.inicioPrevisto.localeCompare(b.inicioPrevisto))
})

/** Índice do primeiro agendamento ainda futuro no dia atual. -1 se todos já
 *  começaram (marcador vai ao final) ou se o dia selecionado não é hoje. */
const indiceProximo = computed(() => {
    if (!isHoje.value) return -1
    const ts = agora.value.getTime()
    return doDiaFiltrado.value.findIndex(a => new Date(a.inicioPrevisto).getTime() > ts)
})

const mostrarAgoraNoFinal = computed(() =>
    isHoje.value && doDiaFiltrado.value.length > 0 && indiceProximo.value === -1,
)

// Counts para o DateStrip — vêm do endpoint agregado (ver carregarContagens).
// Filtra por profissional no backend; especialidade não é repassada (filtra
// apenas a lista do dia client-side, para manter o endpoint enxuto).
const contagens = ref<Record<string, number>>({})
const countsPorDia = computed<Record<string, number>>(() => contagens.value)

const stats = computed(() => {
    const arr = doDia.value
    const total = arr.length
    const agendado = arr.filter(a => a.status === "Agendado").length
    const confirmado = arr.filter(a => a.status === "Confirmado").length
    const concluido = arr.filter(a => a.status === "Concluido").length
    const cancelado = arr.filter(a => a.status === "Cancelado").length
    return { total, agendado, confirmado, concluido, cancelado }
})

const tituloDia = computed(() => {
    const [y, m, d] = dataSel.value.split("-").map(Number)
    const s = new Date(y, m - 1, d).toLocaleDateString("pt-BR", {
        weekday: "long", day: "numeric", month: "long", year: "numeric",
    })
    return s.charAt(0).toUpperCase() + s.slice(1)
})

// ─── Carregamento ───
// Lista do dia: sempre refaz ao trocar dataSel (sem cache — outro profissional
// pode ter mexido na agenda). Contagens da strip: cache por mês + filtroProf.
const janelaCarregada = ref<string>("")  // "yyyy-mm|profissionalId" da última carga das contagens

async function carregarDia() {
    carregando.value = true
    erro.value = null
    try {
        const pg = await agendaService.listar({
            dataInicio: dataSel.value,
            dataFim: dataSel.value,
        })
        agendamentos.value = pg.itens
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar agenda."
    } finally {
        carregando.value = false
    }
}

async function carregarContagens(force = false) {
    const [y, m] = dataSel.value.split("-").map(Number)
    const chave = `${y}-${String(m).padStart(2, "0")}|${filtroProf.value}`
    if (!force && janelaCarregada.value === chave) return

    // Janela = mês inteiro ± 7 dias, para a strip de 14 dias (6 antes + atual + 7 depois)
    // continuar com contagens corretas mesmo na virada do mês.
    const inicio = new Date(y, m - 1, 1)
    inicio.setDate(inicio.getDate() - 7)
    const fim = new Date(y, m, 0)
    fim.setDate(fim.getDate() + 7)
    try {
        const lista = await agendaService.contarPorDia({
            dataInicio: toISO(inicio),
            dataFim: toISO(fim),
            profissionalUsuarioId: filtroProf.value || undefined,
        })
        const map: Record<string, number> = {}
        for (const c of lista) map[c.data] = c.total
        contagens.value = map
        janelaCarregada.value = chave
    } catch { /* não crítico — strip apenas perde contagens */ }
}

watch(dataSel, async () => {
    await carregarDia()
    await carregarContagens()
})

watch(filtroProf, () => { void carregarContagens(true) })

onMounted(async () => {
    agoraTimer = window.setInterval(() => { agora.value = new Date() }, 30_000)
    profissionais.value = await vinculoService.listarProfissionaisPublico()
    if (tenant.papel === "Dono") {
        try {
            const perfil = await profissionalService.obterMeu()
            if (perfil) {
                perfilProprio.value = { especialidade: perfil.especialidade, conselho: perfil.conselho, fotoUrl: perfil.fotoUrl ?? null }
            }
        } catch { /* sem perfil ainda */ }
    }
    // carregarPacientes() é lazy — só dispara ao abrir o modal de novo agendamento
    // ou ao encaixar paciente da lista de espera. Evita carga de PII na entrada da agenda.
    // Salas ativas — não crítico (filtro só não aparece se falhar).
    if (tenant.estabelecimentoAtivoId) {
        try {
            salas.value = await salaService.listar(tenant.estabelecimentoAtivoId, true)
        } catch { /* não crítico */ }
    }
    await Promise.all([carregarDia(), carregarContagens(), carregarListaEspera()])
})

onBeforeUnmount(() => {
    if (agoraTimer !== null) {
        window.clearInterval(agoraTimer)
        agoraTimer = null
    }
})

// Lista local de pacientes mantida APENAS para o caminho de encaixe da lista
// de espera (precisa do `PacienteListaItem` para pré-selecionar no modal).
// NÃO é mais usada pelo autocomplete do "Novo agendamento" — esse usa
// `/api/paciente/busca-rapida` server-side (LGPD: minimização).
async function carregarPacientePorId(id: number): Promise<PacienteListaItem | null> {
    const existente = pacientes.value.find(p => p.id === id)
    if (existente) return existente
    try {
        // Busca pontual (server-side, sem trazer toda a lista) — só usa o nome.
        const lst = await pacienteService.buscaRapida("", 30)
        const cand = lst.find(p => p.id === id)
        if (!cand) return null
        const item: PacienteListaItem = {
            id: cand.id,
            nomeCompleto: cand.nomeCompleto,
            cpf: null,
            documentoInternacional: null,
            dataNascimento: null,
            telefone: null,
            criadoEm: "",
            tags: [],
            qtdAlertas: 0,
        }
        pacientes.value.unshift(item)
        return item
    } catch {
        return null
    }
}

// ─── Ações de status ───
async function confirmarAgendamento(a: Agendamento) {
    try {
        await agendaService.confirmar(a.id)
        await recarregarSemCache()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao confirmar."
    }
}

async function concluirAgendamento(a: Agendamento) {
    try {
        await agendaService.concluir(a.id)
        await recarregarSemCache()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao concluir."
    }
}

// ─── Modal de check-in ───
const modalCheckInAberto = ref(false)
const agendamentoCheckIn = ref<Agendamento | null>(null)
// Edição de paciente disparada de dentro do check-in: renderizada aqui (na
// raiz da view) para não aninhar Dialogs do design-system — o DialogOverlay
// é fixo em z-50, e dois portais sobrepostos fazem o backdrop sumir.
const editarPacienteAberto = ref(false)
const pacienteEmEdicao = ref<Paciente | null>(null)
const pacienteAtualizadoCheckin = ref<Paciente | null>(null)

function abrirCheckIn(a: Agendamento) {
    agendamentoCheckIn.value = a
    pacienteAtualizadoCheckin.value = null
    modalCheckInAberto.value = true
}

async function onCheckInRealizado() {
    modalCheckInAberto.value = false
    agendamentoCheckIn.value = null
    pacienteAtualizadoCheckin.value = null
    await recarregarSemCache()
}

function onSolicitarEdicaoPaciente(p: Paciente) {
    pacienteEmEdicao.value = p
    editarPacienteAberto.value = true
}

function onPacienteSalvoNoCheckin(p: Paciente) {
    pacienteAtualizadoCheckin.value = p
    editarPacienteAberto.value = false
    pacienteEmEdicao.value = null
}

const modalCancelarAberto = ref(false)
const agendamentoCancelar = ref<Agendamento | null>(null)

function cancelarAgendamento(a: Agendamento) {
    agendamentoCancelar.value = a
    modalCancelarAberto.value = true
}

async function onAgendamentoCancelado() {
    modalCancelarAberto.value = false
    agendamentoCancelar.value = null
    await recarregarSemCache()
}

async function recarregarSemCache() {
    await Promise.all([carregarDia(), carregarContagens(true)])
}

// ─── Modal: Novo agendamento ───
const modalNovoAberto = ref(false)
const encaixandoListaEsperaId = ref<number | null>(null)
const encaixePaciente = ref<PacienteListaItem | null>(null)
const encaixeProfissionalId = ref<string | null>(null)
const encaixeMotivo = ref<string | null>(null)

async function abrirModalNovo() {
    if (profissionais.value.length === 0) {
        profissionais.value = await vinculoService.listarProfissionaisPublico()
    }
    if (tenant.papel === "Dono" && !perfilProprio.value) {
        try {
            const perfil = await profissionalService.obterMeu()
            if (perfil) {
                perfilProprio.value = { especialidade: perfil.especialidade, conselho: perfil.conselho, fotoUrl: perfil.fotoUrl ?? null }
            }
        } catch { /* sem perfil ainda */ }
    }
    modalNovoAberto.value = true
}

function fecharModalNovo() {
    modalNovoAberto.value = false
    encaixandoListaEsperaId.value = null
    encaixePaciente.value = null
    encaixeProfissionalId.value = null
    encaixeMotivo.value = null
}

function onPacienteCriado(p: PacienteListaItem) {
    if (!pacientes.value.some(x => x.id === p.id)) pacientes.value.unshift(p)
}

async function onAgendamentoCriado(payload: { listaEspera: boolean }) {
    const idEncaixe = encaixandoListaEsperaId.value
    fecharModalNovo()
    if (payload.listaEspera) {
        await carregarListaEspera()
    } else {
        // Se foi encaixe da lista de espera, remove o item original.
        if (idEncaixe) {
            try { await listaEsperaService.remover(idEncaixe) } catch { /* não crítico */ }
            await carregarListaEspera()
        }
        await recarregarSemCache()
    }
}

// ─── Expansão inline (clicar no row mostra detalhe) ───
const expandidoId = ref<number | null>(null)
function alternarExpansao(a: Agendamento) {
    expandidoId.value = expandidoId.value === a.id ? null : a.id
}

// ─── Modal de edição/reagendamento ───
const editarAberto = ref(false)
const editarAg = ref<Agendamento | null>(null)
const editarFocoReagendar = ref(false)

function abrirEditar(a: Agendamento) {
    editarAg.value = a
    editarFocoReagendar.value = false
    editarAberto.value = true
}

function abrirReagendar(a: Agendamento) {
    editarAg.value = a
    editarFocoReagendar.value = true
    editarAberto.value = true
}

async function onAgendamentoEditado() {
    editarAberto.value = false
    await recarregarSemCache()
    await carregarListaEspera()
}

// ─── Lista de espera ───
const listaEspera = ref<ListaEsperaItem[]>([])

async function carregarListaEspera() {
    try {
        const pg = await listaEsperaService.listar()
        listaEspera.value = pg.itens
    } catch { /* não crítico */ }
}

async function removerListaEspera(item: ListaEsperaItem) {
    if (!confirm(`Remover "${item.pacienteNome}" da lista de espera?`)) return
    try {
        await listaEsperaService.remover(item.id)
        await carregarListaEspera()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao remover."
    }
}

/** Encaixar item da lista de espera: abre modal de novo agendamento já no
 *  passo "Detalhes" com paciente/profissional/motivo pré-preenchidos.
 *  Após o submit do modal, removemos o item da lista de espera. */
async function encaixarListaEspera(item: ListaEsperaItem) {
    const pac = await carregarPacientePorId(item.pacienteId)
    encaixandoListaEsperaId.value = item.id
    encaixePaciente.value = pac
    encaixeProfissionalId.value = item.profissionalPreferidoId
    encaixeMotivo.value = item.motivo
    await abrirModalNovo()
}
</script>

<template>
    <div class="agenda-page">
        <AppPageHeader
            titulo="Agenda"
            subtitulo="Visualize e gerencie seus agendamentos do dia."
        >
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="abrirModalNovo">Novo agendamento</AppButton>
            </template>
        </AppPageHeader>

        <AppDateStrip v-model="dataSel" :counts="countsPorDia" />

        <div class="stats-grid">
            <AppStatCard
                label="Total do dia"
                :valor="stats.total"
                icone="fa-solid fa-calendar-check"
                cor="primary"
                legenda="agendamentos"
                :ativo="filtroStatus === null && (stats.total > 0)"
                @click="filtroStatus = null"
            />
            <AppStatCard
                label="Agendados"
                :valor="stats.agendado"
                icone="fa-solid fa-hourglass-half"
                cor="warning"
                :legenda="stats.agendado ? 'aguardando confirmação' : '—'"
                :ativo="filtroStatus === 'Agendado'"
                @click="filtroStatus = filtroStatus === 'Agendado' ? null : 'Agendado'"
            />
            <AppStatCard
                label="Confirmados"
                :valor="stats.confirmado"
                icone="fa-solid fa-circle-check"
                cor="success"
                :legenda="stats.total ? `${Math.round(stats.confirmado * 100 / stats.total)}% do dia` : '—'"
                :ativo="filtroStatus === 'Confirmado'"
                @click="filtroStatus = filtroStatus === 'Confirmado' ? null : 'Confirmado'"
            />
            <AppStatCard
                label="Concluídos"
                :valor="stats.concluido"
                icone="fa-solid fa-flag-checkered"
                cor="info"
                :legenda="stats.concluido ? 'finalizados' : '—'"
                :ativo="filtroStatus === 'Concluido'"
                @click="filtroStatus = filtroStatus === 'Concluido' ? null : 'Concluido'"
            />
            <AppStatCard
                label="Cancelados"
                :valor="stats.cancelado"
                icone="fa-solid fa-ban"
                cor="error"
                :legenda="stats.cancelado ? 'no dia' : 'sem cancelamentos'"
                :ativo="filtroStatus === 'Cancelado'"
                @click="filtroStatus = filtroStatus === 'Cancelado' ? null : 'Cancelado'"
            />
        </div>

        <div class="agenda-layout">
            <AgendaRail
                v-model="dataSel"
                :counts="countsPorDia"
                :lista-espera="listaEspera"
                :agendamentos-do-dia="doDia"
                @encaixar="encaixarListaEspera"
                @remover="removerListaEspera"
            />

            <AppCard padding="none" class="agenda-card">
                <div class="agenda-toolbar">
                    <div class="dia-titulo">
                        <i class="fa-solid fa-calendar-day" aria-hidden="true"></i>
                        <span>{{ tituloDia }}</span>
                        <span class="dia-count">{{ doDiaFiltrado.length }}</span>
                    </div>
                    <div class="filtros">
                        <AppField label="Profissional" class="filt-grupo">
                            <AppAvatarSelect
                                v-model="filtroProf"
                                :opcoes="profissionaisDisponiveis"
                                placeholder="Todos"
                                permite-limpar
                                rotulo-limpar="Todos"
                            />
                        </AppField>
                        <AppField label="Especialidade" class="filt-grupo">
                            <AppSelect v-model="filtroEspec">
                                <option value="">Todas</option>
                                <option v-for="e in especialidadesDisponiveis" :key="e" :value="e">{{ e }}</option>
                            </AppSelect>
                        </AppField>
                        <AppField v-if="salas.length > 0" label="Sala" class="filt-grupo">
                            <AppSelect v-model="filtroSalaId">
                                <option value="">Todas</option>
                                <option v-for="s in salas" :key="s.id" :value="s.id">{{ s.nome }}</option>
                            </AppSelect>
                        </AppField>
                        <AppField label="Buscar" class="filt-grupo">
                            <AppInput v-model="buscaTexto" placeholder="Paciente, profissional ou tipo..." />
                        </AppField>
                    </div>
                </div>

                <div v-if="erro" class="erro-banner">{{ erro }}</div>

                <div v-if="carregando" class="estado">
                    <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i> Carregando agenda...
                </div>

                <AppEmptyState
                    v-else-if="doDiaFiltrado.length === 0"
                    icone="fa-solid fa-calendar-xmark"
                    titulo="Nenhum agendamento"
                    :descricao="filtroStatus
                        ? `Nenhum agendamento com status ${filtroStatus} para esta data.`
                        : 'Não há agendamentos para a data selecionada.'"
                />

                <div v-else class="appts">
                    <template v-for="(a, i) in doDiaFiltrado" :key="a.id">
                        <div v-if="isHoje && i === indiceProximo" class="agora-marker" aria-label="Horário atual">
                            <span class="agora-pulse"><span class="agora-pulse-dot"></span></span>
                            <span class="agora-label">AGORA · {{ horaAgoraLabel }}</span>
                        </div>
                        <AgendamentoRow
                            :agendamento="a"
                            :expandido="expandidoId === a.id"
                            @alternar="alternarExpansao"
                            @editar="abrirEditar"
                            @reagendar="abrirReagendar"
                            @confirmar="confirmarAgendamento"
                            @cancelar="cancelarAgendamento"
                            @concluir="concluirAgendamento"
                            @checkin="abrirCheckIn"
                        />
                    </template>
                    <div v-if="mostrarAgoraNoFinal" class="agora-marker" aria-label="Horário atual">
                        <span class="agora-pulse"><span class="agora-pulse-dot"></span></span>
                        <span class="agora-label">AGORA · {{ horaAgoraLabel }}</span>
                    </div>
                </div>
            </AppCard>
        </div>
    </div>

    <NovoAgendamentoModal
        :aberto="modalNovoAberto"
        :profissionais="profissionaisDisponiveis"
        :data-padrao="dataSel"
        :paciente-pre-selecionado="encaixePaciente"
        :profissional-pre-selecionado-id="encaixeProfissionalId"
        :motivo-pre-selecionado="encaixeMotivo"
        @fechar="fecharModalNovo"
        @criado="onAgendamentoCriado"
        @paciente-criado="onPacienteCriado"
    />

    <EditarAgendamentoModal
        :aberto="editarAberto"
        :agendamento="editarAg"
        :profissionais="profissionaisDisponiveis"
        :agendamentos-todos="agendamentos"
        :foco-reagendar="editarFocoReagendar"
        @fechar="editarAberto = false"
        @atualizado="onAgendamentoEditado"
    />

    <CheckInModal
        :aberto="modalCheckInAberto"
        :agendamento="agendamentoCheckIn"
        :outros-agendamentos-do-dia="doDia"
        :paciente-atualizado="pacienteAtualizadoCheckin"
        @fechar="modalCheckInAberto = false; agendamentoCheckIn = null; pacienteAtualizadoCheckin = null"
        @checkin-realizado="onCheckInRealizado"
        @editar-paciente="onSolicitarEdicaoPaciente"
    />

    <PacienteFormModal
        v-if="pacienteEmEdicao"
        :aberto="editarPacienteAberto"
        :paciente="pacienteEmEdicao"
        @fechar="editarPacienteAberto = false; pacienteEmEdicao = null"
        @salvo="onPacienteSalvoNoCheckin"
    />

    <CancelarAgendamentoModal
        :aberto="modalCancelarAberto"
        :agendamento="agendamentoCancelar"
        @fechar="modalCancelarAberto = false; agendamentoCancelar = null"
        @cancelado="onAgendamentoCancelado"
    />
</template>

<style scoped>
.agenda-page {
    padding: 22px 28px 80px;
    max-width: 1500px;
    margin: 0 auto;
}

.stats-grid {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 10px;
    margin-bottom: 16px;
}
@media (max-width: 1100px) {
    .stats-grid { grid-template-columns: repeat(3, 1fr); }
}
@media (max-width: 720px) {
    .stats-grid { grid-template-columns: repeat(2, 1fr); }
    .agenda-page { padding: 14px; }
}

.agenda-layout {
    display: grid;
    grid-template-columns: 320px 1fr;
    gap: 16px;
    align-items: start;
}
@media (max-width: 1100px) {
    .agenda-layout { grid-template-columns: 1fr; }
}

.agenda-card { overflow: hidden; }

.agenda-toolbar {
    display: flex;
    align-items: flex-end;
    gap: 16px;
    padding: 14px 16px;
    border-bottom: 1px solid hsl(var(--foreground) / 0.06);
    flex-wrap: wrap;
}
.dia-titulo {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 14px;
    font-weight: 700;
    color: hsl(var(--primary-dark, 254 56% 21%));
    flex: 1;
    min-width: 220px;
}
.dia-titulo i { color: hsl(var(--primary, 254 56% 38%)); }
.dia-count {
    background: hsl(var(--primary, 254 56% 38%) / 0.1);
    color: hsl(var(--primary, 254 56% 38%));
    font-size: 11px;
    font-weight: 700;
    padding: 2px 9px;
    border-radius: 999px;
    margin-left: 4px;
}

.filtros {
    display: flex;
    gap: 12px;
    flex-wrap: wrap;
    align-items: flex-end;
}
.filt-grupo { min-width: 180px; }

.erro-banner {
    margin: 12px 16px 0;
    padding: 10px 14px;
    background: hsl(0 84% 60% / 0.08);
    border: 1px solid hsl(0 84% 60% / 0.2);
    border-radius: 8px;
    color: hsl(0 84% 50%);
    font-size: 13px;
}

.estado {
    padding: 3rem;
    text-align: center;
    color: hsl(var(--foreground) / 0.55);
    font-size: 13px;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
}

.appts {
    display: flex;
    flex-direction: column;
}

/* ── Marcador "AGORA" — só aparece quando o dia selecionado é hoje ── */
.agora-marker {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 4px 16px;
    background: hsl(0 84% 60% / 0.04);
    border-top: 1px solid hsl(0 84% 60% / 0.55);
    border-bottom: 1px solid hsl(0 84% 60% / 0.12);
    position: relative;
}
.agora-pulse {
    width: 14px;
    height: 14px;
    border-radius: 50%;
    background: hsl(0 84% 60% / 0.18);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    animation: agora-pulse 2s ease-in-out infinite;
}
.agora-pulse-dot {
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: hsl(0 84% 55%);
    box-shadow: 0 0 0 2px white;
}
.agora-label {
    font-size: 11px;
    font-weight: 800;
    letter-spacing: 0.06em;
    color: hsl(0 78% 45%);
    font-variant-numeric: tabular-nums;
}
@keyframes agora-pulse {
    0%, 100% { box-shadow: 0 0 0 0 hsl(0 84% 60% / 0.45); }
    50%      { box-shadow: 0 0 0 6px hsl(0 84% 60% / 0); }
}
</style>
