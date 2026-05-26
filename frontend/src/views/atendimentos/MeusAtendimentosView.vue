<script setup lang="ts">
/**
 * Meus Atendimentos — central operacional do profissional para o dia.
 *
 * Layout (alinhado ao design do bundle Imedto):
 *   1. Header: título + sub (dia da semana) + atalhos + ações (data + encaixe)
 *   2. Day progress: stats do dia + barra de progresso
 *   3. Active card + Next card (lado a lado em desktop, empilhados em mobile)
 *   4. Queue list: filtros (Todos/Pendentes/Concluídos/Em espera) + linhas
 *
 * "Em atendimento agora" é controlado por marca local (useAtendimentoAtivo) —
 * o backend só sabe de status Confirmado/Concluído/Cancelado. A marca local
 * é limpa em "Finalizar" (chama POST /agendamentos/:id/concluir) ou ao trocar
 * de profissional/dia.
 */
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue"
import { useRouter } from "vue-router"
import { agendaService, type Agendamento } from "@/services/agendaService"
import type { PacienteListaItem } from "@/services/pacienteService"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { useAtendimentoAtivo } from "@/composables/useAtendimentoAtivo"
import { AppButton, AppDatePicker, AppToast } from "@/components/ui"
import EncaixeModal from "@/components/atendimentos/EncaixeModal.vue"
import AtendimentoActiveCard from "@/components/atendimentos/AtendimentoActiveCard.vue"
import AtendimentoNextCard from "@/components/atendimentos/AtendimentoNextCard.vue"
import AtendimentoQueueRow from "@/components/atendimentos/AtendimentoQueueRow.vue"
import AlocarSalaModal from "@/components/agenda/AlocarSalaModal.vue"

const router = useRouter()
const auth   = useAuthStore()
const tenant = useTenantStore()
const { atual: atendimentoAtivo, iniciar: iniciarAtendimento, finalizar: limparAtendimentoLocal } = useAtendimentoAtivo()

// ─── Data e navegação ────────────────────────────────────────────────────────
const hoje = new Date()
function toISO(d: Date) { return d.toISOString().substring(0, 10) }
function addDias(iso: string, d: number) {
    const [y, m, dd] = iso.split("-").map(Number)
    const data = new Date(y, m - 1, dd); data.setDate(data.getDate() + d)
    return toISO(data)
}

const dataSel    = ref(toISO(hoje))
const carregando = ref(false)
const erro       = ref<string | null>(null)
const agendamentos = ref<Agendamento[]>([])

// Mensagem flutuante — usado em ações (iniciar/finalizar/checkin)
const toast = ref<{ msg: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(msg: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { msg, variante }
}

// ─── Filtragem por profissional + dia ────────────────────────────────────────
const filtros = ["Todos", "Pendentes", "Em espera", "Concluídos"] as const
type Filtro = typeof filtros[number]
const filtroAtivo = ref<Filtro>("Todos")

const doDia = computed(() =>
    agendamentos.value
        .filter(a => a.profissionalUsuarioId === auth.usuario?.id)
        .filter(a => a.inicioPrevisto.startsWith(dataSel.value))
        .sort((a, b) => a.inicioPrevisto.localeCompare(b.inicioPrevisto)),
)

const ehAtivo = (a: Agendamento) => atendimentoAtivo.value?.agendamentoId === a.id

// O "ativo" pode existir mas o agendamento pode estar fora da lista atual
// (ex: dia diferente). Renderizamos só se ele estiver no dia selecionado.
const ativoNoDia = computed<Agendamento | null>(() =>
    doDia.value.find(a => ehAtivo(a)) ?? null,
)

// Próximo da fila: confirmado + check-in feito + não está ativo
const proximoNaFila = computed<Agendamento | null>(() => {
    if (ativoNoDia.value) {
        // Já tem alguém em atendimento — próximo é o primeiro na espera
        return doDia.value.find(a =>
            !ehAtivo(a)
            && a.status === "Confirmado"
            && a.checkInEm
        ) ?? null
    }
    // Sem ativo — pegar o primeiro com check-in (que provavelmente é o próximo a entrar)
    return doDia.value.find(a =>
        a.status === "Confirmado"
        && a.checkInEm
    ) ?? null
})

const fila = computed(() => {
    let lista = doDia.value
    switch (filtroAtivo.value) {
        case "Pendentes":
            lista = lista.filter(a => a.status !== "Concluido" && a.status !== "Cancelado")
            break
        case "Em espera":
            lista = lista.filter(a => a.checkInEm != null && a.status === "Confirmado" && !ehAtivo(a))
            break
        case "Concluídos":
            lista = lista.filter(a => a.status === "Concluido")
            break
    }
    return lista
})

const contagens = computed(() => ({
    Todos: doDia.value.length,
    "Pendentes": doDia.value.filter(a => a.status !== "Concluido" && a.status !== "Cancelado").length,
    "Em espera": doDia.value.filter(a => a.checkInEm && a.status === "Confirmado" && !ehAtivo(a)).length,
    "Concluídos": doDia.value.filter(a => a.status === "Concluido").length,
}))

// ─── Day progress ────────────────────────────────────────────────────────────
const diaSemanaLabel = computed(() => {
    const [y, m, d] = dataSel.value.split("-").map(Number)
    const data = new Date(y, m - 1, d)
    return data.toLocaleDateString("pt-BR", { weekday: "long", day: "2-digit", month: "long" })
})

const stats = computed(() => {
    const total = doDia.value.length
    const concluidos = doDia.value.filter(a => a.status === "Concluido").length
    const emEspera   = doDia.value.filter(a => a.checkInEm && a.status === "Confirmado" && !ehAtivo(a)).length
    // Tempo trabalhado e restante: simples, baseados no horário previsto da fila
    const concluidosOuAtivos = doDia.value.filter(a => a.status === "Concluido" || ehAtivo(a))
    const minTrabalhados = concluidosOuAtivos.reduce((sum, a) => {
        const ini = new Date(a.inicioPrevisto).getTime()
        const fim = new Date(a.fimPrevisto).getTime()
        return sum + Math.round((fim - ini) / 60_000)
    }, 0)
    const minRestantes = doDia.value
        .filter(a => a.status !== "Concluido" && a.status !== "Cancelado" && !ehAtivo(a))
        .reduce((sum, a) => {
            const ini = new Date(a.inicioPrevisto).getTime()
            const fim = new Date(a.fimPrevisto).getTime()
            return sum + Math.round((fim - ini) / 60_000)
        }, 0)
    const pct = total === 0 ? 0 : Math.round((concluidos / total) * 100)
    return { total, concluidos, emEspera, minTrabalhados, minRestantes, pct }
})

function fmtHorasMin(min: number) {
    if (min <= 0) return "—"
    const h  = Math.floor(min / 60)
    const m  = min % 60
    if (h === 0) return `${m}min`
    if (m === 0) return `${h}h`
    return `${h}h${String(m).padStart(2, "0")}`
}

// Header sub: profissional logado
const subtitulo = computed(() => {
    const nome = auth.usuario?.nomeCompleto ?? "Você"
    return `${diaSemanaLabel.value} · ${nome}`
})

// ─── Carregar / refresh ──────────────────────────────────────────────────────
async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const pg = await agendaService.listar({
            dataInicio: dataSel.value,
            dataFim:    dataSel.value,
        })
        agendamentos.value = pg.itens
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar atendimentos."
    } finally {
        carregando.value = false
    }
}

function irHoje()      { dataSel.value = toISO(new Date()) }
function diaAnterior() { dataSel.value = addDias(dataSel.value, -1) }
function proximoDia()  { dataSel.value = addDias(dataSel.value,  1) }

watch(dataSel, carregar)
onMounted(carregar)

// ─── Ações sobre agendamento ─────────────────────────────────────────────────
function abrirProntuario(a: Agendamento) {
    router.push({ name: "Prontuario", params: { id: a.pacienteId }, query: { eventoId: a.id } })
}

function iniciar(a: Agendamento) {
    iniciarAtendimento(a.id, a.pacienteId)
    notificar(`Atendimento de ${a.pacienteNome.split(" ")[0]} iniciado`, "success")
    abrirProntuario(a)
}

async function checkin(a: Agendamento) {
    try {
        await agendaService.registrarCheckIn(a.id)
        notificar(`Check-in de ${a.pacienteNome.split(" ")[0]} registrado`, "success")
        await carregar()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao registrar check-in.", "error")
    }
}

async function finalizar(a: Agendamento) {
    try {
        await agendaService.concluir(a.id)
        if (atendimentoAtivo.value?.agendamentoId === a.id) limparAtendimentoLocal()
        notificar(`Atendimento concluído`, "success")
        await carregar()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao concluir atendimento.", "error")
    }
}

// ─── Atalhos de teclado ──────────────────────────────────────────────────────
function onKey(e: KeyboardEvent) {
    if (e.target && (e.target as HTMLElement).matches("input, textarea, select, [contenteditable]")) return
    if (e.key === "F" || e.key === "f") {
        if (ativoNoDia.value) { e.preventDefault(); finalizar(ativoNoDia.value) }
    } else if (e.key === "N" || e.key === "n") {
        if (proximoNaFila.value) { e.preventDefault(); iniciar(proximoNaFila.value) }
    }
}
onMounted(() => window.addEventListener("keydown", onKey))
onBeforeUnmount(() => window.removeEventListener("keydown", onKey))

// ─── Alocação de sala ────────────────────────────────────────────────────────
const modalAlocarSalaAberto = ref(false)
const agendamentoTrocarSala = ref<Agendamento | null>(null)

function abrirAlocarSala(a: Agendamento) {
    agendamentoTrocarSala.value = a
    modalAlocarSalaAberto.value = true
}

async function onSalaAlocada() {
    agendamentoTrocarSala.value = null
    await carregar()
}

// ─── Novo encaixe ────────────────────────────────────────────────────────────
const modalEncaixe = ref(false)
const criandoEncaixe = ref(false)
const erroEncaixe = ref<string | null>(null)

async function criarEncaixe(p: PacienteListaItem) {
    if (!auth.usuario?.id) return
    criandoEncaixe.value = true
    erroEncaixe.value = null
    try {
        // +60s evita que o backend rejeite como "no passado" por causa do tempo
        // entre o new Date() do front e o BrasiliaTime.Now do handler.
        const inicio = new Date(Date.now() + 60_000)
        const fim    = new Date(inicio.getTime() + 30 * 60_000)
        const { agendamentoId } = await agendaService.criar({
            pacienteId:            p.id,
            profissionalUsuarioId: auth.usuario.id,
            inicioPrevisto:        inicio.toISOString(),
            fimPrevisto:           fim.toISOString(),
            tipoServico:           "Encaixe",
            observacoes:           "Atendimento de encaixe criado a partir de Meus Atendimentos.",
        })
        modalEncaixe.value = false
        // Marca como em atendimento já no momento da criação do encaixe
        iniciarAtendimento(agendamentoId, p.id)
        router.push({ name: "Prontuario", params: { id: p.id }, query: { eventoId: agendamentoId } })
    } catch (e: any) {
        erroEncaixe.value = e?.response?.data?.mensagem ?? "Erro ao criar encaixe."
    } finally {
        criandoEncaixe.value = false
    }
}

function fecharEncaixe() {
    modalEncaixe.value = false
    erroEncaixe.value = null
}

</script>

<template>
    <div class="app-page atend-page">
        <!-- ──── Header da página ──── -->
        <header class="atend-header">
            <div>
                <h1 class="atend-h1">Meus atendimentos</h1>
                <div class="atend-sub">{{ subtitulo }}</div>
            </div>

            <div class="atend-header-right">
                <div class="atend-shortcuts" aria-label="Atalhos de teclado">
                    <kbd>F</kbd> finalizar
                    <span class="dot">·</span>
                    <kbd>N</kbd> próximo
                </div>

                <div class="data-nav">
                    <button class="nav-btn" type="button" @click="diaAnterior" aria-label="Dia anterior">‹</button>
                    <AppDatePicker
                        v-model="dataSel"
                        aria-label="Selecionar dia"
                        align="end"
                    />
                    <button class="nav-btn" type="button" @click="proximoDia" aria-label="Próximo dia">›</button>
                    <AppButton variant="ghost" size="sm" @click="irHoje">Hoje</AppButton>
                </div>

                <AppButton variant="danger" icon="fa-solid fa-bolt" @click="modalEncaixe = true">
                    Novo encaixe
                </AppButton>
            </div>
        </header>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <!-- ──── Day progress ──── -->
        <section class="day-progress" aria-label="Progresso do dia">
            <div class="dp-stats">
                <div class="dp-stat">
                    <span class="dp-val">{{ stats.concluidos }}/{{ stats.total }}</span>
                    <span class="dp-lbl">Atendidos</span>
                </div>
                <div class="dp-stat">
                    <span class="dp-val">{{ stats.emEspera }}</span>
                    <span class="dp-lbl">Em espera</span>
                </div>
                <div class="dp-stat">
                    <span class="dp-val">{{ fmtHorasMin(stats.minTrabalhados) }}</span>
                    <span class="dp-lbl">Tempo trabalhado</span>
                </div>
                <div class="dp-stat">
                    <span class="dp-val">{{ fmtHorasMin(stats.minRestantes) }}</span>
                    <span class="dp-lbl">Tempo restante</span>
                </div>
            </div>
            <div class="dp-bar" :aria-valuenow="stats.pct" aria-valuemin="0" aria-valuemax="100" role="progressbar">
                <div class="dp-fill" :style="{ width: `${stats.pct}%` }"></div>
            </div>
        </section>

        <!-- ──── Active + Next ──── -->
        <section v-if="ativoNoDia || proximoNaFila" class="active-row">
            <AtendimentoActiveCard
                v-if="ativoNoDia"
                :agendamento="ativoNoDia"
                :iniciado-em="atendimentoAtivo!.iniciadoEm"
                :alertas-paciente="[]"
                @abrir-prontuario="abrirProntuario(ativoNoDia)"
                @finalizar="finalizar(ativoNoDia)"
                @trocar-sala="abrirAlocarSala(ativoNoDia)"
            />
            <div v-else class="active-empty">
                <i class="fa-solid fa-stethoscope" aria-hidden="true"></i>
                <p>Nenhum atendimento em curso. Inicie pelo card "Próximo" ou pela fila abaixo.</p>
            </div>

            <AtendimentoNextCard
                v-if="proximoNaFila && proximoNaFila.id !== ativoNoDia?.id"
                :agendamento="proximoNaFila"
                @iniciar="iniciar(proximoNaFila)"
                @trocar-sala="abrirAlocarSala(proximoNaFila)"
            />
        </section>

        <!-- ──── Fila do dia ──── -->
        <section class="queue-section">
            <div class="queue-head">
                <h2 class="atend-h2">Fila do dia</h2>
                <div class="queue-filters" role="tablist" aria-label="Filtrar fila">
                    <button
                        v-for="f in filtros"
                        :key="f"
                        type="button"
                        class="fchip"
                        :class="{ active: filtroAtivo === f }"
                        role="tab"
                        :aria-selected="filtroAtivo === f"
                        @click="filtroAtivo = f"
                    >
                        {{ f }} <span class="fc-count">{{ contagens[f] }}</span>
                    </button>
                </div>
            </div>

            <div class="queue-list">
                <p v-if="carregando" class="queue-empty">Carregando consultas...</p>
                <p v-else-if="fila.length === 0" class="queue-empty">
                    <i class="fa-solid fa-calendar-xmark" aria-hidden="true"></i>
                    Nenhum atendimento neste filtro.
                </p>
                <AtendimentoQueueRow
                    v-for="a in fila"
                    :key="a.id"
                    :agendamento="a"
                    :em-atendimento="ehAtivo(a)"
                    @abrir-prontuario="abrirProntuario(a)"
                    @iniciar="iniciar(a)"
                    @finalizar="finalizar(a)"
                    @checkin="checkin(a)"
                    @trocar-sala="abrirAlocarSala(a)"
                />
            </div>
        </section>

        <EncaixeModal
            :aberto="modalEncaixe"
            :desabilitado="criandoEncaixe"
            :erro="erroEncaixe"
            @fechar="fecharEncaixe"
            @selecionado="criarEncaixe"
        />

        <AlocarSalaModal
            v-if="tenant.estabelecimentoAtivoId"
            v-model:aberto="modalAlocarSalaAberto"
            :agendamento="agendamentoTrocarSala"
            :estab-id="tenant.estabelecimentoAtivoId"
            :outros-agendamentos-do-dia="doDia"
            @alocada="onSalaAlocada"
        />

        <AppToast
            v-if="toast"
            :mensagem="toast.msg"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </div>
</template>

<style scoped>
.atend-page {
    /* Reaproveita .app-page (centralização + max-width) e adiciona o gap maior do design */
    gap: 20px;
}

/* ──── Header ──── */
.atend-header {
    display: flex; align-items: flex-end; justify-content: space-between;
    gap: 16px; flex-wrap: wrap;
}
.atend-h1 {
    font-size: 26px; font-weight: 800;
    color: hsl(var(--primary-dark));
    margin: 0; letter-spacing: -0.01em;
}
.atend-sub {
    color: hsl(var(--secondary) / 0.7);
    font-size: 14px; margin-top: 4px;
    /* Não usamos `text-transform: capitalize` (capitaliza CADA palavra,
       gera "Quarta-Feira, 13 De Maio"). Capitalizamos apenas a 1a letra
       via ::first-letter. */
}
.atend-sub::first-letter { text-transform: uppercase; }
.atend-h2 { font-size: 18px; font-weight: 700; color: hsl(var(--primary-dark)); margin: 0; }

.atend-header-right { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }

.atend-shortcuts {
    font-size: 12px; color: hsl(var(--secondary) / 0.6);
    display: flex; align-items: center; gap: 5px;
}
.atend-shortcuts .dot { opacity: 0.4; }
.atend-shortcuts kbd {
    font-family: var(--font-mono); font-size: 11px;
    background: white; border: 1px solid hsl(var(--secondary) / 0.18);
    border-bottom-width: 2px;
    padding: 2px 6px; border-radius: 4px;
    color: hsl(var(--primary-dark));
}

.data-nav { display: flex; align-items: center; gap: 0.4rem; }
.nav-btn {
    border: 1px solid var(--border-strong); background: var(--bg-card); cursor: pointer;
    border-radius: 6px; padding: 0.25rem 0.55rem; font-size: 1rem; line-height: 1.2;
    color: var(--text); transition: background 0.12s;
}
.nav-btn:hover { background: var(--bg-hover); }

/* ──── Day progress ──── */
.day-progress {
    background: white; padding: 16px 20px;
    border-radius: var(--radius-xl);
    box-shadow: var(--shadow-sm);
    border: 1px solid hsl(var(--secondary) / 0.06);
}
.dp-stats {
    display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px;
    margin-bottom: 12px;
}
@media (max-width: 720px) { .dp-stats { grid-template-columns: repeat(2, 1fr); } }
.dp-stat {
    display: flex; flex-direction: column; gap: 2px;
    padding-left: 14px;
    border-left: 3px solid hsl(var(--primary) / 0.2);
}
.dp-stat:first-child { border-left-color: hsl(var(--primary)); }
.dp-val { font-size: 22px; font-weight: 800; color: hsl(var(--primary-dark)); }
.dp-lbl { font-size: 11px; color: hsl(var(--secondary) / 0.65); text-transform: uppercase; letter-spacing: 0.04em; }
.dp-bar { height: 6px; background: hsl(var(--secondary) / 0.08); border-radius: 999px; overflow: hidden; }
.dp-fill {
    height: 100%;
    background: linear-gradient(90deg, hsl(var(--primary)), hsl(var(--info)));
    border-radius: 999px;
    transition: width 400ms var(--ease-out);
}

/* ──── Active row ──── */
.active-row { display: grid; grid-template-columns: 1.6fr 1fr; gap: 16px; }
@media (max-width: 1100px) { .active-row { grid-template-columns: 1fr; } }

.active-empty {
    background: white; border-radius: var(--radius-xl);
    border: 1px dashed hsl(var(--secondary) / 0.15);
    padding: 24px; text-align: center;
    color: hsl(var(--secondary) / 0.65); font-size: 13px;
    display: flex; flex-direction: column; align-items: center; gap: 8px;
}
.active-empty i { font-size: 24px; opacity: 0.4; color: hsl(var(--primary)); }
.active-empty p { margin: 0; max-width: 360px; }

/* ──── Queue ──── */
.queue-section { display: flex; flex-direction: column; gap: 12px; }
.queue-head {
    display: flex; justify-content: space-between; align-items: center;
    gap: 12px; flex-wrap: wrap;
}
.queue-filters { display: flex; gap: 6px; flex-wrap: wrap; }
.fchip {
    background: white; border: 1px solid hsl(var(--secondary) / 0.12);
    height: 32px; padding: 0 12px; border-radius: 999px;
    font: inherit; font-size: 12px; font-weight: 600;
    color: hsl(var(--secondary) / 0.8);
    cursor: pointer; display: inline-flex; align-items: center; gap: 6px;
    transition: all 150ms;
}
.fchip:hover { color: hsl(var(--primary)); border-color: hsl(var(--primary) / 0.4); }
.fchip.active { background: hsl(var(--primary)); color: white; border-color: hsl(var(--primary)); }
.fc-count {
    background: hsl(var(--secondary) / 0.1);
    padding: 1px 7px; border-radius: 99px; font-size: 11px;
}
.fchip.active .fc-count { background: hsl(0 0% 100% / 0.22); }

.queue-list {
    background: white;
    border-radius: var(--radius-xl);
    border: 1px solid hsl(var(--secondary) / 0.06);
    overflow: hidden;
}

.queue-empty {
    padding: 60px 20px; text-align: center;
    color: hsl(var(--secondary) / 0.55); font-size: 13px;
    display: flex; flex-direction: column; gap: 10px; align-items: center;
    margin: 0;
}
.queue-empty i { font-size: 28px; opacity: 0.35; }

.msg-erro { color: var(--danger); font-size: 0.875em; margin: 0; }
</style>
