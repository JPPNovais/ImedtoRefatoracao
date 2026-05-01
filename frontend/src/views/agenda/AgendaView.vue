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
import { computed, onMounted, ref, watch } from "vue"
import {
    AppButton, AppCard, AppPageHeader, AppEmptyState, AppSelect,
    AppDateStrip, AppStatCard, AppField, AppInput,
} from "@/components/ui"
import AgendamentoRow from "@/components/agenda/AgendamentoRow.vue"
import AgendaRail from "@/components/agenda/AgendaRail.vue"
import NovoAgendamentoModal from "@/components/agenda/NovoAgendamentoModal.vue"
import EditarAgendamentoModal from "@/components/agenda/EditarAgendamentoModal.vue"
import { agendaService, type Agendamento } from "@/services/agendaService"
import { listaEsperaService, type ListaEsperaItem } from "@/services/listaEsperaService"
import { pacienteService, type PacienteListaItem } from "@/services/pacienteService"
import { vinculoService, type ProfissionalVinculado } from "@/services/vinculoService"
import { profissionalService } from "@/services/profissionalService"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"

const auth = useAuthStore()
const tenant = useTenantStore()

// ─── Data selecionada ───
function toISO(d: Date) {
    const y = d.getFullYear()
    const m = String(d.getMonth() + 1).padStart(2, "0")
    const dd = String(d.getDate()).padStart(2, "0")
    return `${y}-${m}-${dd}`
}

const dataSel = ref(toISO(new Date()))

// ─── Estado base ───
const agendamentos = ref<Agendamento[]>([])
const profissionais = ref<ProfissionalVinculado[]>([])
const pacientes = ref<PacienteListaItem[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)
const perfilProprio = ref<{ especialidade: string | null; conselho: string } | null>(null)

// Filtros
const filtroProf = ref("")
const filtroEspec = ref("")
const filtroStatus = ref<Agendamento["status"] | null>(null)
const buscaTexto = ref("")

// Especialidade efetiva de um profissional (Dono incluso).
function especialidadeDoProfissional(usuarioId: string): string {
    const p = profissionais.value.find(x => x.usuarioId === usuarioId)
    if (p?.especialidade?.trim()) return p.especialidade.trim()
    if (tenant.papel === "Dono" && auth.usuario?.id === usuarioId) {
        return (perfilProprio.value?.especialidade ?? "").trim()
    }
    return ""
}

const profissionaisDisponiveis = computed<ProfissionalVinculado[]>(() => {
    const lista = [...profissionais.value]
    if (tenant.papel === "Dono" && auth.usuario && !lista.some(p => p.usuarioId === auth.usuario!.id)) {
        lista.unshift({
            vinculoId: 0,
            usuarioId: auth.usuario.id,
            email: auth.usuario.email,
            nomeCompleto: auth.usuario.nomeCompleto ?? auth.usuario.email,
            status: "Dono",
            modeloPermissaoId: 0,
            modeloPermissaoNome: "Dono do estabelecimento",
            especialidade: perfilProprio.value?.especialidade ?? null,
            conselho: perfilProprio.value?.conselho ?? null,
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

// Counts para o DateStrip e StatCards.
const countsPorDia = computed<Record<string, number>>(() => {
    const m: Record<string, number> = {}
    for (const a of baseFiltrada.value) {
        const k = a.inicioPrevisto.substring(0, 10)
        m[k] = (m[k] ?? 0) + 1
    }
    return m
})

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
const mesCarregado = ref<string>("")  // "yyyy-mm" do mês carregado

async function carregar() {
    const [y, m] = dataSel.value.split("-").map(Number)
    const chave = `${y}-${String(m).padStart(2, "0")}`
    if (mesCarregado.value === chave && !erro.value) {
        return
    }

    carregando.value = true
    erro.value = null
    try {
        agendamentos.value = await agendaService.listar({
            dataInicio: `${chave}-01`,
            dataFim: toISO(new Date(y, m, 0)),
        })
        mesCarregado.value = chave
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar agenda."
    } finally {
        carregando.value = false
    }
}

watch(dataSel, () => { void carregar() })

onMounted(async () => {
    profissionais.value = await vinculoService.listarProfissionais()
    if (tenant.papel === "Dono") {
        try {
            const perfil = await profissionalService.obterMeu()
            if (perfil) {
                perfilProprio.value = { especialidade: perfil.especialidade, conselho: perfil.conselho }
            }
        } catch { /* sem perfil ainda */ }
    }
    await Promise.all([carregar(), carregarListaEspera(), carregarPacientes()])
})

async function carregarPacientes() {
    if (pacientes.value.length > 0) return
    try {
        const pg = await pacienteService.listar(undefined, 1, 200)
        pacientes.value = pg.itens
    } catch { /* não crítico — modal lida com lista vazia */ }
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

async function cancelarAgendamento(a: Agendamento) {
    const motivo = prompt("Motivo do cancelamento:")
    if (motivo === null) return
    try {
        await agendaService.cancelar(a.id, motivo)
        await recarregarSemCache()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao cancelar."
    }
}

async function recarregarSemCache() {
    mesCarregado.value = ""
    await carregar()
}

// ─── Modal: Novo agendamento ───
const modalNovoAberto = ref(false)
const encaixandoListaEsperaId = ref<number | null>(null)

async function abrirModalNovo() {
    if (profissionais.value.length === 0) {
        profissionais.value = await vinculoService.listarProfissionais()
    }
    if (tenant.papel === "Dono" && !perfilProprio.value) {
        try {
            const perfil = await profissionalService.obterMeu()
            if (perfil) {
                perfilProprio.value = { especialidade: perfil.especialidade, conselho: perfil.conselho }
            }
        } catch { /* sem perfil ainda */ }
    }
    await carregarPacientes()
    encaixandoListaEsperaId.value = null
    modalNovoAberto.value = true
}

function onPacienteCriado(p: PacienteListaItem) {
    if (!pacientes.value.some(x => x.id === p.id)) pacientes.value.unshift(p)
}

async function onAgendamentoCriado(payload: { listaEspera: boolean }) {
    modalNovoAberto.value = false
    if (payload.listaEspera) {
        await carregarListaEspera()
    } else {
        // Se foi encaixe da lista de espera, remove o item original.
        if (encaixandoListaEsperaId.value) {
            try { await listaEsperaService.remover(encaixandoListaEsperaId.value) } catch { /* não crítico */ }
            encaixandoListaEsperaId.value = null
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
        listaEspera.value = await listaEsperaService.listar()
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

/** Encaixar item da lista de espera: abre modal de novo agendamento.
 *  O modal não tem API para pré-preencher diretamente — o fluxo é:
 *  guardar o id, abrir modal; após o submit do modal, removemos o item. */
async function encaixarListaEspera(item: ListaEsperaItem) {
    encaixandoListaEsperaId.value = item.id
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
            <AppCard padding="none" class="agenda-card">
                <div class="agenda-toolbar">
                    <div class="dia-titulo">
                        <i class="fa-solid fa-calendar-day" aria-hidden="true"></i>
                        <span>{{ tituloDia }}</span>
                        <span class="dia-count">{{ doDiaFiltrado.length }}</span>
                    </div>
                    <div class="filtros">
                        <AppField label="Profissional" class="filt-grupo">
                            <AppSelect v-model="filtroProf">
                                <option value="">Todos</option>
                                <option v-for="p in profissionaisDisponiveis" :key="p.usuarioId" :value="p.usuarioId">
                                    {{ p.nomeCompleto || p.email }}
                                </option>
                            </AppSelect>
                        </AppField>
                        <AppField label="Especialidade" class="filt-grupo">
                            <AppSelect v-model="filtroEspec">
                                <option value="">Todas</option>
                                <option v-for="e in especialidadesDisponiveis" :key="e" :value="e">{{ e }}</option>
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
                    <AgendamentoRow
                        v-for="a in doDiaFiltrado"
                        :key="a.id"
                        :agendamento="a"
                        :expandido="expandidoId === a.id"
                        @alternar="alternarExpansao"
                        @editar="abrirEditar"
                        @reagendar="abrirReagendar"
                        @confirmar="confirmarAgendamento"
                        @cancelar="cancelarAgendamento"
                        @concluir="concluirAgendamento"
                    />
                </div>
            </AppCard>

            <AgendaRail
                v-model="dataSel"
                :counts="countsPorDia"
                :lista-espera="listaEspera"
                :agendamentos-do-dia="doDia"
                @encaixar="encaixarListaEspera"
                @remover="removerListaEspera"
            />
        </div>
    </div>

    <NovoAgendamentoModal
        :aberto="modalNovoAberto"
        :profissionais="profissionaisDisponiveis"
        :pacientes="pacientes"
        :data-padrao="dataSel"
        @fechar="modalNovoAberto = false"
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
    grid-template-columns: 1fr 320px;
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
    border-bottom: 1px solid hsl(0 0% 0% / 0.06);
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
    color: hsl(0 0% 0% / 0.55);
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
</style>
