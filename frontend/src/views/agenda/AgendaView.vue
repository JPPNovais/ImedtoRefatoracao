<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue"
import { vMaska } from "maska/vue"
import {
    AppDrawer, AppBadge, AppButton, AppCalendar, AppCard, AppField,
    AppPageHeader, AppPillToggle, AppEmptyState, AppSelect,
} from "@/components/ui"
import SlotPicker from "@/components/agenda/SlotPicker.vue"
import PacienteQuickCreate from "@/components/agenda/PacienteQuickCreate.vue"
import AgendamentoDetalhes from "@/components/agenda/AgendamentoDetalhes.vue"
import { agendaService, type Agendamento, type CriarAgendamentoPayload } from "@/services/agendaService"
import { pacienteService, type PacienteListaItem } from "@/services/pacienteService"
import { vinculoService, type ProfissionalVinculado } from "@/services/vinculoService"
import { profissionalService } from "@/services/profissionalService"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"

const auth   = useAuthStore()
const tenant = useTenantStore()

// ─── Calendário / Navegação ───────────────────────────────────────────────────

const hoje = new Date()

function toISO(d: Date) { return d.toISOString().substring(0, 10) }

const dataSel = ref(toISO(hoje))
const mesVis  = ref({ ano: hoje.getFullYear(), mes: hoje.getMonth() })

function onMesMudou({ ano, mes }: { ano: number; mes: number }) {
    const prev = mesVis.value
    mesVis.value = { ano, mes }
    if (prev.ano !== ano || prev.mes !== mes) void carregar()
}

// ─── Agendamentos ─────────────────────────────────────────────────────────────

const agendamentos  = ref<Agendamento[]>([])

const datasComPonto = computed(() =>
    agendamentos.value.map(a => a.inicioPrevisto.substring(0, 10))
)
const profissionais = ref<ProfissionalVinculado[]>([])
const pacientes     = ref<PacienteListaItem[]>([])
const carregando    = ref(false)
const erro          = ref<string | null>(null)

// Especialidade/conselho do próprio usuário logado (carregado quando ele é Dono).
const perfilProfissionalProprio = ref<{ especialidade: string | null; conselho: string } | null>(null)
const filtroProf    = ref("")
const filtroEspec   = ref("")

// Período exibido na direita — compatível com o legado (Dia | Semana | Mês).
type PeriodoVis = "dia" | "semana" | "mes"
const periodoVis = ref<PeriodoVis>("dia")

function isoNaSemana(iso: string, ref: string) {
    // Retorna true se `iso` (YYYY-MM-DDTHH:MM:SS...) está na mesma semana
    // ISO (segunda→domingo) de `ref` (YYYY-MM-DD).
    const [ay, am, ad] = ref.split("-").map(Number)
    const ancora = new Date(ay, am - 1, ad)
    const diaSem = (ancora.getDay() + 6) % 7 // segunda = 0
    const ini = new Date(ancora); ini.setDate(ancora.getDate() - diaSem); ini.setHours(0, 0, 0, 0)
    const fim = new Date(ini);    fim.setDate(ini.getDate() + 6);         fim.setHours(23, 59, 59, 999)
    const t = new Date(iso).getTime()
    return t >= ini.getTime() && t <= fim.getTime()
}

function isoNoMes(iso: string, ref: string) {
    const [y, m] = ref.split("-").map(Number)
    const d = new Date(iso)
    return d.getFullYear() === y && d.getMonth() === m - 1
}

// Especialidade efetiva de um profissional, considerando o Dono (que não é vínculo formal).
function especialidadeDoProfissional(usuarioId: string): string {
    const p = profissionais.value.find(x => x.usuarioId === usuarioId)
    if (p?.especialidade?.trim()) return p.especialidade.trim()
    if (tenant.papel === "Dono" && auth.usuario?.id === usuarioId) {
        return (perfilProfissionalProprio.value?.especialidade ?? "").trim()
    }
    return ""
}

// Lista filtrada pelo período escolhido + filtros.
const listaDia = computed(() => {
    const prof = filtroProf.value
    const esp  = filtroEspec.value

    const base = agendamentos.value.filter(a => {
        if (prof && a.profissionalUsuarioId !== prof) return false
        if (esp && especialidadeDoProfissional(a.profissionalUsuarioId) !== esp) return false
        return true
    })

    const filtradoPorPeriodo = base.filter(a => {
        switch (periodoVis.value) {
            case "dia":    return a.inicioPrevisto.startsWith(dataSel.value)
            case "semana": return isoNaSemana(a.inicioPrevisto, dataSel.value)
            case "mes":    return isoNoMes(a.inicioPrevisto, dataSel.value)
        }
    })

    return filtradoPorPeriodo.sort((a, b) => a.inicioPrevisto.localeCompare(b.inicioPrevisto))
})

// Lista de especialidades únicas dos profissionais vinculados (para o filtro).
// Inclui também a do Dono (que não é vínculo formal) para coerência com a lista
// de profissionais exibida no calendário/select de criação.
const especialidadesFiltro = computed<string[]>(() => {
    const set = new Set<string>()
    for (const p of profissionais.value) {
        if (p.especialidade && p.especialidade.trim()) set.add(p.especialidade.trim())
    }
    const espDono = perfilProfissionalProprio.value?.especialidade?.trim()
    if (tenant.papel === "Dono" && espDono) set.add(espDono)
    return Array.from(set).sort((a, b) => a.localeCompare(b, "pt-BR"))
})

const tituloDia = computed(() => {
    const [y, m, d] = dataSel.value.split("-")
    const s = new Date(+y, +m - 1, +d).toLocaleDateString("pt-BR", {
        weekday: "long", day: "numeric", month: "long", year: "numeric"
    })
    return s.charAt(0).toUpperCase() + s.slice(1)
})

onMounted(async () => {
    profissionais.value = await vinculoService.listarProfissionais()
    if (tenant.papel === "Dono") {
        try {
            const perfil = await profissionalService.obterMeu()
            if (perfil) {
                perfilProfissionalProprio.value = {
                    especialidade: perfil.especialidade,
                    conselho:      perfil.conselho,
                }
            }
        } catch { /* sem perfil ainda */ }
    }
    await carregar()
})

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const { ano, mes } = mesVis.value
        const m = String(mes + 1).padStart(2, "0")
        agendamentos.value = await agendaService.listar({
            dataInicio: `${ano}-${m}-01`,
            dataFim:    toISO(new Date(ano, mes + 1, 0)),
        })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar agenda."
    } finally {
        carregando.value = false
    }
}

// ─── Drawer: Novo agendamento ─────────────────────────────────────────────────

const drawerAberto  = ref(false)
const slotPickerAb  = ref(false)
const salvando      = ref(false)

const novo = reactive({
    pacienteId:            0 as number,
    profissionalUsuarioId: "" as string,
    data:                  "",
    hora:                  "08:00",
    duracaoMin:            30,
    tipoServico:           "",
    especialidade:         "",
    contato:               "",
    observacoes:           null as string | null,
})

watch(() => novo.pacienteId, (id) => {
    const p = pacientes.value.find(pac => pac.id === id)
    if (p?.telefone) novo.contato = p.telefone
})

// Inclui o Dono como profissional mesmo quando ele não é vínculo formal,
// para que ele possa agendar consigo mesmo (caso comum em clínicas pequenas).
const profissionaisDisponiveis = computed<ProfissionalVinculado[]>(() => {
    const lista = [...profissionais.value]
    if (tenant.papel === "Dono" && auth.usuario) {
        const jaIncluido = lista.some(p => p.usuarioId === auth.usuario!.id)
        if (!jaIncluido) {
            lista.unshift({
                vinculoId: 0,
                usuarioId: auth.usuario.id,
                email: auth.usuario.email,
                nomeCompleto: auth.usuario.nomeCompleto ?? auth.usuario.email,
                status: "Dono",
                modeloPermissaoId: 0,
                modeloPermissaoNome: "Dono do estabelecimento",
                especialidade: perfilProfissionalProprio.value?.especialidade ?? null,
                conselho:      perfilProfissionalProprio.value?.conselho      ?? null,
            })
        }
    }
    return lista
})

// Lista de especialidades únicas (não-vazias) dos profissionais do estabelecimento.
const especialidadesDisponiveis = computed<string[]>(() => {
    const set = new Set<string>()
    for (const p of profissionaisDisponiveis.value) {
        if (p.especialidade && p.especialidade.trim()) set.add(p.especialidade.trim())
    }
    return Array.from(set).sort((a, b) => a.localeCompare(b, "pt-BR"))
})

// Auto-preenche a especialidade quando o profissional é selecionado.
watch(() => novo.profissionalUsuarioId, (id) => {
    const p = profissionaisDisponiveis.value.find(x => x.usuarioId === id)
    if (p?.especialidade) novo.especialidade = p.especialidade
})

const profissionalNomeSel = computed(() =>
    profissionaisDisponiveis.value.find(p => p.usuarioId === novo.profissionalUsuarioId)?.nomeCompleto ?? ""
)

const DURACOES = [15, 30, 45, 60, 90, 120]

const TIPOS_CONSULTA = [
    "Consulta",
    "Retorno",
    "Primeira consulta",
    "Exame",
    "Procedimento",
    "Avaliação",
    "Teleconsulta",
    "Emergência",
]

function labelDuracao(min: number) {
    if (min < 60) return `${min} minutos`
    const h = Math.floor(min / 60), m = min % 60
    return m ? `${h}h ${m}min` : `${h} hora${h > 1 ? "s" : ""}`
}

const horaFim = computed(() => {
    if (!novo.data || !novo.hora) return ""
    const ini = new Date(`${novo.data}T${novo.hora}`)
    const fim = new Date(ini.getTime() + novo.duracaoMin * 60000)
    return fim.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })
})

const camposFaltando = computed<string[]>(() => {
    const miss: string[] = []
    if (!novo.data)                      miss.push("data")
    if (!novo.hora)                      miss.push("horário")
    if (!novo.tipoServico)               miss.push("tipo da consulta")
    if (!novo.profissionalUsuarioId)     miss.push("profissional")
    if (!novo.pacienteId || novo.pacienteId <= 0) miss.push("paciente")
    return miss
})

const podeSalvarAgendamento = computed(() => camposFaltando.value.length === 0)

const dataCardLabel = computed(() => {
    if (!novo.data) return ""
    const [y, m, d] = novo.data.split("-")
    return new Date(+y, +m - 1, +d).toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric" })
})

// ─── Autocomplete de paciente ─────────────────────────────────────────────────
const pacienteQuery       = ref("")
const pacienteDropdownAb  = ref(false)

const pacientesFiltrados = computed(() => {
    const q = pacienteQuery.value.trim().toLowerCase()
    const base = pacientes.value
    if (!q) return base.slice(0, 8)
    return base
        .filter(p => p.nomeCompleto.toLowerCase().includes(q)
            || (p.cpf ?? "").includes(q)
            || (p.telefone ?? "").includes(q))
        .slice(0, 8)
})

function selecionarPaciente(p: PacienteListaItem) {
    novo.pacienteId = p.id
    pacienteQuery.value = p.nomeCompleto
    pacienteDropdownAb.value = false
    if (p.telefone) novo.contato = p.telefone
}

function onPacienteInput() {
    pacienteDropdownAb.value = true
    // Auto-seleciona se o texto bater exatamente com algum paciente
    const q = pacienteQuery.value.trim().toLowerCase()
    const exato = pacientes.value.find(p => p.nomeCompleto.toLowerCase() === q)
    novo.pacienteId = exato ? exato.id : 0
}

function fecharPacienteDropdownComDelay() {
    setTimeout(() => {
        pacienteDropdownAb.value = false
        // Se há um único match parcial e o usuário saiu sem clicar, auto-selecionar
        if (novo.pacienteId === 0 && pacientesFiltrados.value.length === 1 && pacienteQuery.value.trim()) {
            selecionarPaciente(pacientesFiltrados.value[0])
        }
    }, 180)
}

// ─── Cadastro rápido de paciente ─────────────────────────────────────────────
const quickCreateAb = ref(false)

function abrirQuickCreate() {
    pacienteDropdownAb.value = false
    quickCreateAb.value = true
}

function onPacienteCriado(p: PacienteListaItem) {
    // Adiciona ao cache local e seleciona
    if (!pacientes.value.some(x => x.id === p.id)) {
        pacientes.value.unshift(p)
    }
    selecionarPaciente(p)
    quickCreateAb.value = false
}

async function abrirDrawer() {
    novo.data = dataSel.value
    novo.hora = "08:00"
    novo.duracaoMin = 30
    novo.tipoServico = ""
    novo.especialidade = ""
    novo.contato = ""
    novo.observacoes = null
    novo.pacienteId = 0
    novo.profissionalUsuarioId = ""
    pacienteQuery.value = ""
    pacienteDropdownAb.value = false

    if (profissionais.value.length === 0) {
        profissionais.value = await vinculoService.listarProfissionais()
    }
    if (tenant.papel === "Dono" && !perfilProfissionalProprio.value) {
        try {
            const perfil = await profissionalService.obterMeu()
            if (perfil) {
                perfilProfissionalProprio.value = {
                    especialidade: perfil.especialidade,
                    conselho:      perfil.conselho,
                }
            }
        } catch { /* sem perfil ainda */ }
    }
    if (pacientes.value.length === 0) {
        const pg = await pacienteService.listar(undefined, 1, 200)
        pacientes.value = pg.itens
    }
    drawerAberto.value = true
}

function onSlotSelecionado(payload: { data: string; hora: string }) {
    novo.data = payload.data
    novo.hora = payload.hora
    slotPickerAb.value = false
}

async function criarAgendamento() {
    if (!novo.data || !novo.hora || !novo.tipoServico || !novo.pacienteId || !novo.profissionalUsuarioId) return
    salvando.value = true
    erro.value = null
    try {
        const ini = new Date(`${novo.data}T${novo.hora}`)
        const fim = new Date(ini.getTime() + novo.duracaoMin * 60000)
        const payload: CriarAgendamentoPayload = {
            pacienteId:            novo.pacienteId,
            profissionalUsuarioId: novo.profissionalUsuarioId,
            inicioPrevisto:        ini.toISOString(),
            fimPrevisto:           fim.toISOString(),
            tipoServico:           novo.tipoServico,
            observacoes:           novo.observacoes || null,
        }
        await agendaService.criar(payload)
        drawerAberto.value = false
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao criar agendamento."
    } finally {
        salvando.value = false
    }
}

// ─── Drawer de detalhes ──────────────────────────────────────────────────────

const detalhesAberto = ref(false)
const detalhesAg     = ref<Agendamento | null>(null)

function abrirDetalhes(a: Agendamento) {
    detalhesAg.value     = a
    detalhesAberto.value = true
}

// ─── Formatação ───────────────────────────────────────────────────────────────

function fmtHora(iso: string) {
    return new Date(iso).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })
}

function fmtDuracao(ini: string, fim: string) {
    return labelDuracao(Math.round((new Date(fim).getTime() - new Date(ini).getTime()) / 60000))
}
</script>

<template>
    <div class="app-page app-page--wide agenda">
        <!-- ── Cabeçalho da página ── -->
        <AppPageHeader
            titulo="Agendamentos"
            subtitulo="Visualize e organize os seus agendamentos por dia, semana ou mês."
        >
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="abrirDrawer">Novo agendamento</AppButton>
            </template>
        </AppPageHeader>

        <!-- ── Card principal (padrão legado: tabs + filtros + conteúdo) ── -->
        <AppCard padding="md">

            <!-- Toggle Dia/Semana/Mês -->
            <div class="periodo-toggle-wrap">
                <AppPillToggle
                    v-model="periodoVis"
                    :opcoes="[
                        { valor: 'dia',    label: 'Dia' },
                        { valor: 'semana', label: 'Semana' },
                        { valor: 'mes',    label: 'Mês' },
                    ]"
                />
            </div>

            <!-- Filtros -->
            <div class="filtros-bar">
                <AppField label="Filtrar por especialidade" class="filtro-grupo">
                    <AppSelect v-model="filtroEspec">
                        <option value="">Todas as especialidades</option>
                        <option v-for="e in especialidadesFiltro" :key="e" :value="e">{{ e }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Filtrar por profissional" class="filtro-grupo">
                    <AppSelect v-model="filtroProf">
                        <option value="">Todos os profissionais</option>
                        <option v-for="p in profissionaisDisponiveis" :key="p.usuarioId" :value="p.usuarioId">
                            {{ p.nomeCompleto || p.email }}
                        </option>
                    </AppSelect>
                </AppField>
            </div>

            <!-- Conteúdo: calendário + agenda -->
            <div class="conteudo">
            <!-- Coluna esquerda: calendário -->
            <div class="esquerda">
                <span class="cal-section-label">Calendário</span>
                <div class="cal-card">
                    <AppCalendar
                        v-model="dataSel"
                        :datas-com-ponto="datasComPonto"
                        @mes-mudou="onMesMudou"
                    />
                </div>
            </div>

            <!-- Coluna direita: lista de agendamentos -->
            <div class="direita">
                <div class="agenda-header">
                    <div class="agenda-titulo-wrap">
                        <h2 class="agenda-titulo">
                            {{ periodoVis === 'dia' ? 'Agenda do dia'
                             : periodoVis === 'semana' ? 'Agenda da semana'
                             : 'Agenda do mês' }}
                        </h2>
                        <span class="agenda-data-label">{{ tituloDia }}</span>
                    </div>
                </div>

                <p v-if="erro" class="msg-erro">{{ erro }}</p>

                <div class="tabela-wrap">
                    <div v-if="carregando" class="estado-msg">Carregando...</div>

                    <AppEmptyState
                        v-else-if="listaDia.length === 0"
                        icone="📅"
                        titulo="Nenhum agendamento"
                        descricao="Não há agendamentos para o período selecionado."
                        compacto
                    />

                    <table v-else class="tabela">
                        <thead>
                            <tr>
                                <th>Horário</th>
                                <th>Paciente</th>
                                <th>Profissional</th>
                                <th>Tipo</th>
                                <th>Situação</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr
                                v-for="a in listaDia" :key="a.id"
                                :class="['linha', 'clicavel', a.status.toLowerCase()]"
                                @click="abrirDetalhes(a)"
                            >
                                <td class="cel-hora">
                                    <span class="hora-ini">{{ fmtHora(a.inicioPrevisto) }}</span>
                                    <span class="hora-dur">{{ fmtDuracao(a.inicioPrevisto, a.fimPrevisto) }}</span>
                                </td>
                                <td class="cel-paciente">{{ a.pacienteNome }}</td>
                                <td class="cel-prof">{{ a.profissionalNome }}</td>
                                <td>{{ a.tipoServico }}</td>
                                <td><AppBadge :status="a.status" /></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
        </AppCard>
    </div>

    <!-- ── Drawer: Novo agendamento ── -->
    <AppDrawer :aberto="drawerAberto" :largura="600" @fechar="drawerAberto = false">
        <template #titulo>Novo agendamento</template>

        <!-- Profissional (antes de data para que o SlotPicker tenha o ID ao abrir) -->
        <div class="campo">
            <label class="campo-label">Profissional <span class="obrig">*</span></label>
            <select v-model="novo.profissionalUsuarioId" class="input-field">
                <option value="" disabled>Selecione...</option>
                <option v-for="p in profissionaisDisponiveis" :key="p.usuarioId" :value="p.usuarioId">
                    {{ p.nomeCompleto || p.email }}
                </option>
            </select>
        </div>

        <!-- Card data e horário -->
        <div class="campo">
            <label class="campo-label">Data e horário <span class="obrig">*</span></label>
            <div
                class="data-card"
                :class="novo.profissionalUsuarioId ? 'data-card--clicavel' : 'data-card--desabilitado'"
                @click="novo.profissionalUsuarioId && (slotPickerAb = true)"
            >
                <span class="data-card-icon">📅</span>
                <div class="data-card-info">
                    <span class="data-card-data">
                        {{ dataCardLabel || (novo.profissionalUsuarioId ? "Selecione a data" : "Selecione um profissional primeiro") }}
                    </span>
                    <span class="data-card-hora" v-if="novo.hora">{{ novo.hora }} — {{ horaFim }}</span>
                </div>
                <AppButton
                    v-if="novo.profissionalUsuarioId"
                    type="button" variant="secondary" size="sm"
                    @click.stop="slotPickerAb = true"
                >
                    ✏️ Alterar
                </AppButton>
            </div>
        </div>

        <!-- Duração -->
        <div class="campo">
            <label class="campo-label">Duração da consulta</label>
            <select v-model="novo.duracaoMin" class="input-field">
                <option v-for="d in DURACOES" :key="d" :value="d">{{ labelDuracao(d) }}</option>
            </select>
        </div>

        <!-- Tipo -->
        <div class="campo">
            <label class="campo-label">Tipo da consulta <span class="obrig">*</span></label>
            <select v-model="novo.tipoServico" class="input-field">
                <option value="" disabled>Selecione...</option>
                <option v-for="t in TIPOS_CONSULTA" :key="t" :value="t">{{ t }}</option>
            </select>
        </div>

        <!-- Especialidade -->
        <div class="campo">
            <label class="campo-label">Especialidade</label>
            <select
                v-model="novo.especialidade"
                class="input-field"
                :disabled="especialidadesDisponiveis.length === 0"
            >
                <option value="">
                    {{ especialidadesDisponiveis.length === 0
                        ? "Nenhum profissional com especialidade cadastrada"
                        : "Selecione..." }}
                </option>
                <option v-for="e in especialidadesDisponiveis" :key="e" :value="e">{{ e }}</option>
            </select>
            <p v-if="especialidadesDisponiveis.length === 0" class="campo-hint">
                Cadastre a especialidade em <strong>Configurações</strong> ou convide profissionais com especialidades definidas.
            </p>
        </div>

        <!-- Paciente (autocomplete) -->
        <div class="campo combobox">
            <label class="campo-label">Nome do paciente <span class="obrig">*</span></label>
            <input
                v-model="pacienteQuery"
                class="input-field"
                placeholder="Digite para buscar ou registrar..."
                autocomplete="off"
                @input="onPacienteInput"
                @focus="pacienteDropdownAb = true"
                @blur="fecharPacienteDropdownComDelay"
            />
            <div v-if="pacienteDropdownAb" class="ac-dropdown">
                <button
                    v-for="p in pacientesFiltrados" :key="p.id"
                    type="button"
                    class="ac-item"
                    @mousedown.prevent="selecionarPaciente(p)"
                >
                    <span class="ac-nome">{{ p.nomeCompleto }}</span>
                    <span v-if="p.telefone || p.cpf" class="ac-meta">
                        {{ p.telefone ?? p.cpf }}
                    </span>
                </button>
                <button
                    type="button"
                    class="ac-item ac-novo"
                    @mousedown.prevent="abrirQuickCreate"
                >
                    <span class="ac-nome">
                        ＋ Cadastrar novo paciente<span v-if="pacienteQuery"> "{{ pacienteQuery }}"</span>
                    </span>
                </button>
            </div>
        </div>

        <!-- Contato (auto-preenchido, editável) -->
        <div class="campo">
            <label class="campo-label">Contato</label>
            <input
                v-model="novo.contato"
                v-maska="'(##) #####-####'"
                class="input-field"
                placeholder="(00) 00000-0000"
                type="tel"
                inputmode="numeric"
            />
        </div>

        <!-- Observações -->
        <div class="campo">
            <label class="campo-label">Observações</label>
            <textarea v-model="novo.observacoes" class="input-field" rows="3" placeholder="Opcional..."></textarea>
        </div>

        <template #rodape>
            <div class="rodape-conteudo">
                <span v-if="camposFaltando.length" class="faltando">
                    Falta preencher: {{ camposFaltando.join(", ") }}
                </span>
                <div class="rodape-botoes">
                    <AppButton variant="ghost" @click="drawerAberto = false">Cancelar</AppButton>
                    <AppButton
                        :disabled="salvando || !podeSalvarAgendamento"
                        :loading="salvando"
                        @click="criarAgendamento"
                    >Salvar</AppButton>
                </div>
            </div>
        </template>
    </AppDrawer>

    <!-- ── Drawer: Detalhes do agendamento ── -->
    <AgendamentoDetalhes
        :aberto="detalhesAberto"
        :agendamento="detalhesAg"
        :agendamentosTodos="agendamentos"
        @fechar="detalhesAberto = false"
        @atualizado="carregar"
    />

    <!-- ── Cadastro rápido de paciente ── -->
    <PacienteQuickCreate
        :aberto="quickCreateAb"
        :nomeInicial="pacienteQuery"
        @fechar="quickCreateAb = false"
        @criado="onPacienteCriado"
    />

    <!-- ── Modal de seleção de horário ── -->
    <SlotPicker
        :aberto="slotPickerAb"
        :titulo="profissionalNomeSel"
        :profissionalId="novo.profissionalUsuarioId"
        :dataInicial="novo.data"
        @fechar="slotPickerAb = false"
        @selecionar="onSlotSelecionado"
    />
</template>

<style scoped>
/* ── Toggle período (estilo pill do legado) ───────────────── */
/* ── Layout da página (tabs + filtros + conteúdo) ── */
.periodo-toggle-wrap { display: flex; }

.filtros-bar {
    display: flex; gap: 1rem; flex-wrap: wrap;
}
.filtro-grupo { min-width: 240px; }

/* ── Layout dois painéis ──────────────────────────────────── */
.conteudo { display: flex; gap: 1.5rem; align-items: flex-start; }

/* ── Painel esquerdo ──────────────────────────────────────── */
.esquerda { width: 290px; flex-shrink: 0; }

.cal-section-label {
    display: block; font-size: 0.75rem; font-weight: 600;
    color: hsl(var(--secondary) / 0.70);
    margin-bottom: 0.6rem;
}

.cal-card {
    display: inline-block;
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.75rem;
    overflow: hidden;
}

/* ── Painel direito ───────────────────────────────────────── */
.direita { flex: 1; min-width: 0; }

.agenda-header { margin-bottom: 0.9rem; }
.agenda-titulo { font-size: 1rem; font-weight: 700; margin: 0 0 0.1rem; }
.agenda-data-label { font-size: 0.85em; color: var(--text-muted); }

.msg-erro { color: var(--danger); font-size: 0.875em; margin-bottom: 0.75rem; }

.tabela-wrap {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); overflow: hidden;
}
.tabela { width: 100%; border-collapse: collapse; font-size: 0.875em; }
.tabela th {
    background: #f9fafb; text-align: left; padding: 0.65rem 1rem;
    font-size: 0.75em; font-weight: 700; text-transform: uppercase;
    letter-spacing: 0.04em; color: var(--text-muted);
    border-bottom: 1px solid var(--border);
}
.tabela td { padding: 0.85rem 1rem; border-bottom: 1px solid var(--border); vertical-align: middle; }
.linha:last-child td { border-bottom: none; }
.linha { transition: background 0.1s; }
.linha.clicavel { cursor: pointer; }
.linha:hover { background: var(--bg-hover); }
.linha.cancelado { opacity: 0.6; }
.linha.concluido  { opacity: 0.75; }

.cel-hora { white-space: nowrap; }
.hora-ini { display: block; font-weight: 700; }
.hora-dur { display: block; font-size: 0.78em; color: var(--text-muted); }
.cel-paciente { font-weight: 600; }
.cel-prof { color: var(--text-muted); font-size: 0.9em; }

.estado-msg { text-align: center; color: var(--text-muted); padding: 2.5rem 1rem; font-size: 0.9em; }

.cel-acoes { white-space: nowrap; }
.btn-ac {
    font-size: 0.75em; padding: 0.25rem 0.65rem; border-radius: 4px;
    cursor: pointer; border: 1px solid var(--border); background: var(--bg-card);
    margin-right: 0.3rem; transition: all 0.12s;
}
.btn-ac.confirmar:hover { background: #dcfce7; border-color: #16a34a; color: #15803d; }
.btn-ac.concluir:hover  { background: #f1f5f9; border-color: #475569; }
.btn-ac.cancelar:hover  { background: #fee2e2; border-color: #b91c1c; color: #b91c1c; }

/* ── Drawer: campos ───────────────────────────────────────── */
.campo { display: flex; flex-direction: column; gap: 0.3rem; }
.campo-label { font-size: 0.82em; font-weight: 600; color: var(--text-muted); }
.campo-hint  { font-size: 0.75em; color: var(--text-faint); margin: 0.2rem 0 0; }
.obrig { color: var(--danger); }

.input-field {
    padding: 0.5rem 0.75rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.875em;
    background: var(--bg-card); color: var(--text); transition: border-color 0.15s;
}
.input-field:focus { outline: none; border-color: hsl(var(--primary)); }
.input-field[readonly] { background: #f9fafb; color: var(--text-muted); cursor: default; }

/* Card de data/hora no drawer */
.data-card {
    display: flex; align-items: center; gap: 0.75rem;
    border: 2px solid var(--border); border-radius: var(--radius);
    padding: 0.75rem 1rem; background: var(--bg-card);
    transition: border-color 0.15s;
}
.data-card--clicavel { cursor: pointer; }
.data-card--clicavel:hover { border-color: hsl(var(--primary)); }
.data-card--desabilitado { cursor: default; opacity: 0.6; }
.data-card--desabilitado .data-card-data { color: var(--text-muted); font-style: italic; }
.data-card-icon { font-size: 1.3em; flex-shrink: 0; }
.data-card-info { flex: 1; display: flex; flex-direction: column; gap: 0.1rem; }
.data-card-data { font-weight: 700; font-size: 0.95em; }
.data-card-hora { font-size: 0.82em; color: var(--text-muted); }


/* ── Autocomplete (combobox) ──────────────────────────── */
.combobox { position: relative; }

.ac-dropdown {
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    margin-top: 4px;
    max-height: 220px;
    overflow-y: auto;
    background: var(--bg-card);
    border: 1px solid var(--border-strong);
    border-radius: var(--radius);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
    z-index: 10;
}

.ac-item {
    display: flex; justify-content: space-between; align-items: center;
    width: 100%; padding: 0.55rem 0.75rem;
    border: none; background: none; cursor: pointer; text-align: left;
    border-bottom: 1px solid var(--border);
    font-family: inherit; font-size: 0.875em;
    transition: background 0.1s;
}
.ac-item:last-child { border-bottom: none; }
.ac-item:hover { background: var(--bg-hover); }
.ac-nome { font-weight: 600; }
.ac-meta { font-size: 0.8em; color: var(--text-muted); }

.ac-item.ac-novo {
    background: var(--bg-hover); font-weight: 700;
}
.ac-item.ac-novo:hover { background: hsl(var(--primary-light)); }
.ac-item.ac-novo .ac-nome { color: hsl(var(--primary)); }

/* Rodapé do drawer com dica de validação */
.rodape-conteudo {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    gap: 1rem;
}
.faltando {
    font-size: 0.75em;
    color: #92400e;
    flex: 1;
    text-align: left;
}
.rodape-botoes {
    display: flex;
    gap: 0.5rem;
    margin-left: auto;
}

</style>
