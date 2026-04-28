<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue"
import { useRouter } from "vue-router"
import AppBadge from "@/components/ui/AppBadge.vue"
import AppDatePicker from "@/components/ui/AppDatePicker.vue"
import { AppButton } from "@/components/ui"
import EncaixeModal from "@/components/atendimentos/EncaixeModal.vue"
import { agendaService, type Agendamento } from "@/services/agendaService"
import type { PacienteListaItem } from "@/services/pacienteService"
import { useAuthStore } from "@/stores/authStore"

const router = useRouter()
const auth   = useAuthStore()

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

// Apenas os atendimentos DO PROFISSIONAL LOGADO para a data selecionada.
const lista = computed(() =>
    agendamentos.value
        .filter(a => a.profissionalUsuarioId === auth.usuario?.id)
        .filter(a => a.inicioPrevisto.startsWith(dataSel.value))
        .sort((a, b) => a.inicioPrevisto.localeCompare(b.inicioPrevisto)),
)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        agendamentos.value = await agendaService.listar({
            dataInicio: dataSel.value,
            dataFim:    dataSel.value,
        })
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

function fmtDataHora(iso: string) {
    return new Date(iso).toLocaleString("pt-BR", {
        dateStyle: "short",
        timeStyle: "short",
    })
}

function isEncaixe(a: Agendamento) {
    return (a.tipoServico ?? "").toLowerCase() === "encaixe"
}

function podeIniciar(a: Agendamento) {
    return a.status !== "Cancelado" && a.status !== "Concluido"
}

function abrirProntuario(a: Agendamento) {
    router.push({ name: "Prontuario", params: { id: a.pacienteId } })
}

// ─── Novo encaixe ───────────────────────────────────────────────────────────
const modalEncaixe = ref(false)
const criandoEncaixe = ref(false)

async function criarEncaixe(p: PacienteListaItem) {
    if (!auth.usuario?.id) return
    criandoEncaixe.value = true
    try {
        const agora = new Date()
        const fim   = new Date(agora.getTime() + 30 * 60_000)
        await agendaService.criar({
            pacienteId:            p.id,
            profissionalUsuarioId: auth.usuario.id,
            inicioPrevisto:        agora.toISOString(),
            fimPrevisto:           fim.toISOString(),
            tipoServico:           "Encaixe",
            observacoes:           "Atendimento de encaixe criado a partir de Minhas consultas.",
        })
        modalEncaixe.value = false
        router.push({ name: "Prontuario", params: { id: p.id } })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao criar encaixe."
    } finally {
        criandoEncaixe.value = false
    }
}
</script>

<template>
    <div class="app-page worklist">
        <header class="page-header">
            <div class="page-header-texto">
                <h1 class="page-titulo">Minhas consultas</h1>
                <p class="page-sub">Veja as consultas confirmadas para você e inicie o prontuário do paciente.</p>
            </div>

            <div class="header-acoes">
                <AppButton variant="danger" icon="fa-solid fa-bolt" @click="modalEncaixe = true">
                    Novo encaixe
                </AppButton>

                <div class="data-nav">
                    <span class="data-label-inline">Dia:</span>
                    <button class="nav-btn" @click="diaAnterior" title="Dia anterior">‹</button>
                    <AppDatePicker
                        v-model="dataSel"
                        aria-label="Selecionar dia"
                        align="end"
                    />
                    <button class="nav-btn" @click="proximoDia" title="Próximo dia">›</button>
                    <AppButton variant="ghost" size="sm" @click="irHoje">Hoje</AppButton>
                </div>
            </div>
        </header>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <div class="card-lista">
            <p v-if="carregando" class="estado-msg">Carregando consultas...</p>
            <p v-else-if="lista.length === 0" class="estado-msg">
                Nenhuma consulta agendada para o dia selecionado.
            </p>

            <ul v-else class="consultas">
                <li v-for="a in lista" :key="a.id" class="consulta">
                    <div class="consulta-info">
                        <div class="consulta-titulo">
                            <span class="nome">{{ a.pacienteNome }}</span>
                            <span v-if="isEncaixe(a)" class="badge-encaixe">Encaixe</span>
                            <AppBadge :status="a.status" />
                        </div>
                        <p class="consulta-meta">
                            {{ fmtDataHora(a.inicioPrevisto) }}
                            <span class="sep">·</span>
                            {{ a.tipoServico }}
                        </p>
                    </div>

                    <AppButton
                        v-if="podeIniciar(a)"
                        variant="secondary"
                        size="sm"
                        @click="abrirProntuario(a)"
                    >Iniciar atendimento</AppButton>

                    <AppButton
                        v-else
                        variant="ghost"
                        size="sm"
                        icon="fa-solid fa-eye"
                        :title="'Ver prontuário'"
                        @click="abrirProntuario(a)"
                    />
                </li>
            </ul>
        </div>

        <EncaixeModal
            :aberto="modalEncaixe"
            :desabilitado="criandoEncaixe"
            @fechar="modalEncaixe = false"
            @selecionado="criarEncaixe"
        />
    </div>
</template>

<style scoped>
.page-header {
    display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem;
    margin-bottom: 1.25rem; flex-wrap: wrap;
}
.page-header-texto { display: flex; flex-direction: column; gap: 0.15rem; }
.page-titulo { font-size: 1.5rem; font-weight: 800; margin: 0; color: hsl(var(--primary-dark)); }
.page-sub    { margin: 0; color: var(--text-muted); font-size: 0.875em; max-width: 640px; }

.header-acoes { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
.data-nav { display: flex; align-items: center; gap: 0.4rem; }
.data-label-inline { font-size: 0.82em; color: var(--text-muted); font-weight: 600; }

.nav-btn {
    border: 1px solid var(--border-strong); background: var(--bg-card); cursor: pointer;
    border-radius: 6px; padding: 0.25rem 0.55rem; font-size: 1rem; line-height: 1.2;
    color: var(--text); transition: background 0.12s;
}
.nav-btn:hover { background: var(--bg-hover); }

.input-field {
    padding: 0.4rem 0.7rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.85em;
    background: var(--bg-card); color: var(--text);
}
.input-field:focus { outline: none; border-color: hsl(var(--primary)); }
.input-data { width: 155px; text-align: center; }


.card-lista {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 1.5rem;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.03);
}

.msg-erro { color: var(--danger); font-size: 0.875em; margin: 0 0 0.75rem; }
.estado-msg { text-align: left; color: var(--text-muted); font-size: 0.9em; margin: 0; }

.consultas { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; }
.consultas > li + li { border-top: 1px solid var(--border); }

.consulta {
    display: flex; align-items: center; justify-content: space-between;
    gap: 1rem; padding: 0.85rem 0;
}
.consulta-info { display: flex; flex-direction: column; gap: 0.3rem; min-width: 0; }
.consulta-titulo { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }
.nome { font-weight: 600; color: var(--text); font-size: 0.95em; }
.consulta-meta { margin: 0; font-size: 0.82em; color: var(--text-muted); }
.consulta-meta .sep { margin: 0 0.3rem; }

.badge-encaixe {
    display: inline-flex; align-items: center;
    padding: 0.12rem 0.55rem; border-radius: 999px;
    background: hsl(0 84% 60% / 0.12); color: hsl(0 72% 45%);
    font-size: 0.7em; font-weight: 700;
}

</style>
