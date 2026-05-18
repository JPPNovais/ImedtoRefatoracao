<script setup lang="ts">
/**
 * EditarAgendamentoModal — modal único de edição + reagendamento.
 * Implementação fiel ao design Anthropic (EditAppointmentModal.jsx):
 *
 *   Header: avatar do paciente + nome + status + horário/duração atual
 *   Seção "Data e horário":
 *     - Modo padrão: card com horário atual + botão "Reagendar"
 *     - Modo reagendamento: from-to (quando alterado), strip de 14 dias,
 *       grid de horários (vagos / ocupados / horário original)
 *   Seção "Detalhes do atendimento":
 *     - Profissional, Tipo, Duração, Observações
 *   Footer: Cancelar | Salvar alterações / Confirmar reagendamento
 *
 * Backend mapeia em PUT /api/agendamentos/{id}.
 */
import { computed, reactive, ref, watch } from "vue"
import {
    agendaService,
    type Agendamento,
    type DisponibilidadeDia,
} from "@/services/agendaService"
import type { ProfissionalPublico } from "@/services/vinculoService"
import { salaService, type Sala } from "@/services/salaService"
import { useTenantStore } from "@/stores/tenantStore"

const props = defineProps<{
    aberto: boolean
    agendamento: Agendamento | null
    profissionais: ProfissionalPublico[]
    /** Lista global de agendamentos do mês — usada para marcar slots ocupados localmente. */
    agendamentosTodos?: Agendamento[]
    /** Quando true, abre já no modo reagendamento expandido. */
    focoReagendar?: boolean
}>()

const emit = defineEmits<{
    fechar: []
    atualizado: []
}>()

const TIPOS_CONSULTA = [
    { v: "Consulta", l: "Consulta" },
    { v: "Retorno", l: "Retorno" },
    { v: "Primeira consulta", l: "Primeira vez" },
    { v: "Exame", l: "Exame" },
    { v: "Procedimento", l: "Procedimento" },
    { v: "Teleconsulta", l: "Teleconsulta" },
]

const DURACOES = [15, 20, 30, 45, 60, 90, 120]

// ─── Estado ───
const salvando = ref(false)
const erro = ref<string | null>(null)
const mostrarReagendar = ref(false)
const disponibilidade = ref<DisponibilidadeDia[]>([])
const carregandoSlots = ref(false)

const form = reactive({
    profissionalUsuarioId: "" as string,
    tipo: "Consulta",
    duracaoMin: 30,
    observacoes: "",
    data: "",
    hora: "",
    origData: "",
    origHora: "",
    salaId: null as number | null,
    origSalaId: null as number | null,
})

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
function isoDataHora(iso: string) {
    const d = new Date(iso)
    const yyyy = d.getFullYear()
    const mm = String(d.getMonth() + 1).padStart(2, "0")
    const dd = String(d.getDate()).padStart(2, "0")
    const hh = String(d.getHours()).padStart(2, "0")
    const mn = String(d.getMinutes()).padStart(2, "0")
    return { data: `${yyyy}-${mm}-${dd}`, hora: `${hh}:${mn}` }
}

function fmtDataLabel(iso: string) {
    if (!iso) return "—"
    const [y, m, d] = iso.split("-").map(Number)
    return new Date(y, m - 1, d).toLocaleDateString("pt-BR", {
        weekday: "short", day: "2-digit", month: "short",
    })
}

const inicial = computed(() => {
    if (!props.agendamento) return "?"
    const partes = props.agendamento.pacienteNome.trim().split(/\s+/)
    if (partes.length === 1) return partes[0].charAt(0).toUpperCase()
    return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase()
})

const STATUS_META: Record<Agendamento["status"], { label: string; bg: string; fg: string }> = {
    Agendado: { label: "Agendado", bg: "hsl(45 96% 47% / 0.18)", fg: "hsl(35 90% 30%)" },
    Confirmado: { label: "Confirmado", bg: "hsl(160 79% 39% / 0.12)", fg: "hsl(160 79% 28%)" },
    Concluido: { label: "Concluído", bg: "hsl(var(--foreground) / 0.06)", fg: "hsl(var(--foreground) / 0.6)" },
    Cancelado: { label: "Cancelado", bg: "hsl(0 84% 60% / 0.10)", fg: "hsl(0 84% 60%)" },
}

const statusMeta = computed(() => props.agendamento ? STATUS_META[props.agendamento.status] : null)

// 14 dias a partir de hoje
const proximosDias = computed(() => {
    const out: { iso: string; dow: string; dia: number; mes: string; isHoje: boolean }[] = []
    const base = new Date()
    base.setHours(0, 0, 0, 0)
    for (let i = 0; i < 14; i++) {
        const d = new Date(base)
        d.setDate(base.getDate() + i)
        const yyyy = d.getFullYear()
        const mm = String(d.getMonth() + 1).padStart(2, "0")
        const dd = String(d.getDate()).padStart(2, "0")
        out.push({
            iso: `${yyyy}-${mm}-${dd}`,
            dow: d.toLocaleDateString("pt-BR", { weekday: "short" }).slice(0, 3).replace(".", "").toUpperCase(),
            dia: d.getDate(),
            mes: d.toLocaleDateString("pt-BR", { month: "short" }).replace(".", ""),
            isHoje: i === 0,
        })
    }
    return out
})

// Dia atual da disponibilidade (gerado pelo backend respeitando funcionamento do estabelecimento).
const diaAtual = computed<DisponibilidadeDia | null>(() =>
    disponibilidade.value.find(d => d.data === form.data) ?? null,
)

const diaFechado = computed(() =>
    !!diaAtual.value && diaAtual.value.status === "fechado",
)

/**
 * Slots a renderizar no grid de horários.
 *
 * Fonte primária: backend (já respeita horário de funcionamento, dias, bloqueios e ocupação).
 * Fallback: lista de agendamentos do mês passada pelo parent — só se a API falhar.
 *
 * O horário original do próprio agendamento é sempre marcado como disponível
 * (do ponto de vista do backend ele aparece como "agendado", mas para o usuário que está
 * editando esse próprio agendamento ele continua sendo o horário atual).
 */
const slotsDoDia = computed<{ hora: string; disponivel: boolean; motivo: string | null }[]>(() => {
    const dia = diaAtual.value
    if (dia) {
        return dia.slots.map(s => {
            const ehOriginal = form.data === form.origData && s.hora === form.origHora
            return {
                hora: s.hora,
                disponivel: ehOriginal ? true : s.disponivel,
                motivo: ehOriginal ? null : s.motivo,
            }
        })
    }

    // Fallback: gera slots a partir dos agendamentos do mês do parent (sem informação de funcionamento).
    const ocupadas = new Set<string>()
    const todos = props.agendamentosTodos ?? []
    for (const a of todos) {
        if (a.id === props.agendamento?.id) continue
        if (a.profissionalUsuarioId !== form.profissionalUsuarioId) continue
        if (a.status === "Cancelado") continue
        if (!a.inicioPrevisto.startsWith(form.data)) continue
        ocupadas.add(isoDataHora(a.inicioPrevisto).hora)
    }
    if (form.data === form.origData) ocupadas.delete(form.origHora)
    // Sem disponibilidade da API não há funcionamento — devolve lista vazia para forçar o empty state.
    if (ocupadas.size === 0) return []
    return [...ocupadas].sort().map(h => ({ hora: h, disponivel: false, motivo: "agendado" }))
})

const dataAlterada = computed(() => form.data !== form.origData)
const horaAlterada = computed(() => form.hora !== form.origHora)
const isReagendamento = computed(() => dataAlterada.value || horaAlterada.value)

// Validação para salvar
const detalhesAlterados = computed(() => {
    if (!props.agendamento) return false
    const a = props.agendamento
    const iniMs = new Date(a.inicioPrevisto).getTime()
    const fimMs = new Date(a.fimPrevisto).getTime()
    const duracaoOrig = Math.max(15, Math.round((fimMs - iniMs) / 60_000))
    return a.profissionalUsuarioId !== form.profissionalUsuarioId
        || a.tipoServico !== form.tipo
        || duracaoOrig !== form.duracaoMin
        || (a.observacoes ?? "") !== (form.observacoes ?? "")
        || (a.salaId ?? null) !== (form.salaId ?? null)
})

const podeSalvar = computed(() => {
    if (!props.agendamento) return false
    if (!form.profissionalUsuarioId || !form.tipo || form.duracaoMin <= 0) return false
    if (!form.data || !form.hora) return false
    return isReagendamento.value || detalhesAlterados.value
})

// ─── Carregar disponibilidade quando o profissional ou data mudam ───
let disponibilidadeReqId = 0
async function carregarDisponibilidade() {
    if (!form.profissionalUsuarioId) { disponibilidade.value = []; return }
    const reqId = ++disponibilidadeReqId
    carregandoSlots.value = true
    try {
        const ini = proximosDias.value[0].iso
        const fim = proximosDias.value[proximosDias.value.length - 1].iso
        const r = await agendaService.consultarDisponibilidade(form.profissionalUsuarioId, ini, fim)
        if (reqId === disponibilidadeReqId) disponibilidade.value = r.dias
    } catch {
        if (reqId === disponibilidadeReqId) disponibilidade.value = []
    } finally {
        if (reqId === disponibilidadeReqId) carregandoSlots.value = false
    }
}

// ─── Inicialização ao abrir ───
function inicializar() {
    erro.value = null
    salvando.value = false
    if (!props.agendamento) return
    const a = props.agendamento
    const ini = isoDataHora(a.inicioPrevisto)
    const iniMs = new Date(a.inicioPrevisto).getTime()
    const fimMs = new Date(a.fimPrevisto).getTime()
    Object.assign(form, {
        profissionalUsuarioId: a.profissionalUsuarioId,
        tipo: a.tipoServico,
        duracaoMin: Math.max(15, Math.round((fimMs - iniMs) / 60_000)),
        observacoes: a.observacoes ?? "",
        data: ini.data,
        hora: ini.hora,
        origData: ini.data,
        origHora: ini.hora,
        salaId: a.salaId ?? null,
        origSalaId: a.salaId ?? null,
    })
    mostrarReagendar.value = !!props.focoReagendar
    void carregarDisponibilidade()
    void garantirSalas()
}

watch(() => props.aberto, (v) => { if (v) inicializar() })
watch(() => form.profissionalUsuarioId, (novo, antigo) => {
    if (!props.aberto || novo === antigo) return
    void carregarDisponibilidade()
})

// ─── Ações ───
function desfazerReagendamento() {
    form.data = form.origData
    form.hora = form.origHora
}

function selecionarDia(iso: string) {
    form.data = iso
    // Se a hora atual não estiver disponível no novo dia, limpa.
    const dia = disponibilidade.value.find(d => d.data === iso)
    if (dia) {
        const slot = dia.slots.find(s => s.hora === form.hora)
        if (!slot || !slot.disponivel) {
            // Não limpamos automaticamente — usuário escolhe novo horário no grid.
        }
    }
}

function selecionarHora(t: string) {
    form.hora = t
}

async function salvar() {
    if (!props.agendamento || !podeSalvar.value) return
    salvando.value = true
    erro.value = null
    try {
        const ini = new Date(`${form.data}T${form.hora}`)
        const fim = new Date(ini.getTime() + form.duracaoMin * 60_000)
        await agendaService.atualizar(props.agendamento.id, {
            profissionalUsuarioId: form.profissionalUsuarioId,
            inicioPrevisto: ini.toISOString(),
            fimPrevisto: fim.toISOString(),
            tipoServico: form.tipo,
            observacoes: form.observacoes.trim() || null,
        })
        if ((form.salaId ?? null) !== (form.origSalaId ?? null)) {
            await agendaService.alocarSala(
                tenant.estabelecimentoAtivoId ?? 0,
                props.agendamento.id,
                form.salaId ?? null,
            )
        }
        emit("atualizado")
        emit("fechar")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao atualizar agendamento."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <div v-if="aberto && agendamento" class="modal-overlay" @click="emit('fechar')">
        <div class="modal modal-edit" @click.stop>
            <header class="modal-head edit-head">
                <div class="eh-patient">
                    <div class="av">{{ inicial }}</div>
                    <div>
                        <h2>{{ agendamento.pacienteNome }}</h2>
                        <span class="eh-sub">
                            <span
                                v-if="statusMeta"
                                class="pill"
                                :style="{ background: statusMeta.bg, color: statusMeta.fg }"
                            >
                                <span class="dot"></span>{{ statusMeta.label }}
                            </span>
                            <span class="eh-time">
                                <i class="fa-solid fa-clock" aria-hidden="true"></i>
                                {{ form.origHora }} · {{ form.duracaoMin }} min
                            </span>
                        </span>
                    </div>
                </div>
                <button type="button" class="modal-close" @click="emit('fechar')">
                    <i class="fa-solid fa-xmark" aria-hidden="true"></i>
                </button>
            </header>

            <div class="modal-body">
                <!-- ── Seção 1: Data e horário ── -->
                <div class="edit-section">
                    <div class="es-head">
                        <div>
                            <i class="fa-solid fa-calendar-day" aria-hidden="true"></i>
                            <b>Data e horário</b>
                        </div>
                        <button v-if="!mostrarReagendar" type="button" class="btn-text" @click="mostrarReagendar = true">
                            <i class="fa-solid fa-rotate-right" aria-hidden="true"></i> Reagendar
                        </button>
                    </div>

                    <div v-if="!mostrarReagendar" class="current-slot">
                        <div class="cs-time">
                            <span class="hh">{{ form.origHora }}</span>
                            <span class="dur">{{ form.duracaoMin }} min</span>
                        </div>
                        <div class="cs-info">
                            <b>{{ fmtDataLabel(form.origData) }}</b>
                            <span>Mantém data e horário atual</span>
                        </div>
                    </div>

                    <div v-else class="resched-block">
                        <div v-if="isReagendamento" class="from-to">
                            <div class="ft-side from">
                                <span class="lbl">De</span>
                                <b>{{ form.origHora }}</b>
                                <span class="dt">{{ fmtDataLabel(form.origData) }}</span>
                            </div>
                            <i class="fa-solid fa-arrow-right ft-arr" aria-hidden="true"></i>
                            <div class="ft-side to">
                                <span class="lbl">Para</span>
                                <b>{{ form.hora }}</b>
                                <span class="dt">{{ fmtDataLabel(form.data) }}</span>
                            </div>
                            <button type="button" class="ft-undo" title="Desfazer" @click="desfazerReagendamento">
                                <i class="fa-solid fa-rotate-left" aria-hidden="true"></i>
                            </button>
                        </div>

                        <div class="day-strip">
                            <button
                                v-for="d in proximosDias"
                                :key="d.iso"
                                type="button"
                                class="day-btn"
                                :class="{ active: form.data === d.iso }"
                                @click="selecionarDia(d.iso)"
                            >
                                <span class="dow">{{ d.dow }}</span>
                                <span class="dn">{{ d.dia }}</span>
                                <span class="mo">{{ d.mes }}</span>
                            </button>
                        </div>

                        <div class="slots-info">
                            <span>
                                <i class="fa-solid fa-circle slot-dot dot-free" aria-hidden="true"></i>
                                Vago
                            </span>
                            <span>
                                <i class="fa-solid fa-circle slot-dot dot-busy" aria-hidden="true"></i>
                                Ocupado
                            </span>
                            <span class="orig-mark">
                                <i class="fa-solid fa-location-dot" aria-hidden="true"></i>
                                Horário atual
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
                                    active: form.hora === s.hora,
                                    busy: !s.disponivel,
                                    free: s.disponivel,
                                    original: s.hora === form.origHora && form.data === form.origData,
                                }"
                                :disabled="!s.disponivel"
                                :title="!s.disponivel
                                    ? (s.motivo === 'bloqueado'
                                        ? 'Horário bloqueado'
                                        : s.motivo === 'passado'
                                            ? 'Horário no passado'
                                            : 'Ocupado')
                                    : (s.hora === form.origHora && form.data === form.origData ? 'Horário atual' : 'Disponível')"
                                @click="s.disponivel && selecionarHora(s.hora)"
                            >
                                {{ s.hora }}
                                <i
                                    v-if="s.hora === form.origHora && form.data === form.origData"
                                    class="fa-solid fa-location-dot mark"
                                    aria-hidden="true"
                                ></i>
                                <i
                                    v-else-if="!s.disponivel"
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
                </div>

                <!-- ── Seção 2: Detalhes do atendimento ── -->
                <div class="edit-section">
                    <div class="es-head">
                        <div>
                            <i class="fa-solid fa-stethoscope" aria-hidden="true"></i>
                            <b>Detalhes do atendimento</b>
                        </div>
                    </div>

                    <div class="form-grid">
                        <div class="field-group">
                            <label>Profissional</label>
                            <select v-model="form.profissionalUsuarioId">
                                <option v-for="p in profissionais" :key="p.usuarioId" :value="p.usuarioId">
                                    {{ p.nomeCompleto }}{{ p.especialidade ? ` — ${p.especialidade}` : "" }}
                                </option>
                            </select>
                        </div>

                        <div class="field-group">
                            <label>Tipo de atendimento</label>
                            <select v-model="form.tipo">
                                <option v-for="t in TIPOS_CONSULTA" :key="t.v" :value="t.v">{{ t.l }}</option>
                            </select>
                        </div>

                        <div class="field-group">
                            <label>Duração</label>
                            <select v-model.number="form.duracaoMin">
                                <option v-for="d in DURACOES" :key="d" :value="d">{{ d }} minutos</option>
                            </select>
                        </div>

                        <div v-if="salas.length > 0" class="field-group">
                            <label>Sala <span class="opt">opcional</span></label>
                            <select v-model.number="form.salaId">
                                <option :value="null">— Sem sala —</option>
                                <option v-for="s in salas" :key="s.id" :value="s.id">
                                    {{ s.nome }} — {{ s.unidadeNome }}<template v-if="s.tipoSalaNome"> · {{ s.tipoSalaNome }}</template>
                                </option>
                            </select>
                        </div>

                        <div class="field-group full">
                            <label>Observações <span class="opt">opcional</span></label>
                            <textarea
                                rows="3"
                                placeholder="Notas internas sobre o atendimento..."
                                v-model="form.observacoes"
                            ></textarea>
                        </div>
                    </div>
                </div>

                <div v-if="erro" class="erro-banner">{{ erro }}</div>
            </div>

            <footer class="modal-foot">
                <button type="button" class="btn-ghost" @click="emit('fechar')">Cancelar</button>
                <div class="spacer"></div>
                <button
                    type="button"
                    class="btn-primary"
                    :class="{ success: isReagendamento }"
                    :disabled="salvando || !podeSalvar"
                    @click="salvar"
                >
                    <i :class="['fa-solid', isReagendamento ? 'fa-calendar-check' : 'fa-floppy-disk']" aria-hidden="true"></i>
                    {{
                        salvando
                            ? "Salvando..."
                            : (isReagendamento ? "Confirmar reagendamento" : "Salvar alterações")
                    }}
                </button>
            </footer>
        </div>
    </div>
</template>

<style scoped>
/* ── Overlay e container ── */
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
    max-width: 820px;
    max-height: calc(100vh - 64px);
    border-radius: 18px;
    display: flex;
    flex-direction: column;
    box-shadow: 0 30px 80px hsl(var(--primary-dark, 254 56% 21%) / 0.35);
    overflow: hidden;
    animation: mdIn 220ms cubic-bezier(.2,.8,.2,1) both;
}
@keyframes mdIn { from { opacity: 0; transform: translateY(20px) scale(0.98); } to { opacity: 1; transform: translateY(0) scale(1); } }

/* ── Header ── */
.modal-head {
    display: flex;
    align-items: flex-start;
    gap: 14px;
    padding: 18px 24px 16px;
    border-bottom: 1px solid hsl(var(--foreground) / 0.08);
}
.edit-head { align-items: center; }
.eh-patient { display: flex; align-items: center; gap: 14px; flex: 1; }
.eh-patient .av {
    width: 48px;
    height: 48px;
    border-radius: 50%;
    background: linear-gradient(135deg, hsl(var(--primary, 254 56% 38%)), hsl(var(--primary-dark, 254 56% 21%)));
    color: white;
    font-weight: 600;
    font-size: 15px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}
.eh-patient h2 { margin: 0 0 4px; font-size: 17px; font-weight: 600; color: hsl(var(--primary-dark, 254 56% 21%)); }
.eh-sub { display: inline-flex; align-items: center; gap: 10px; font-size: 12px; }
.eh-sub .pill {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    padding: 2px 8px;
    border-radius: 99px;
    font-size: 11px;
    font-weight: 500;
}
.eh-sub .pill .dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
.eh-sub .eh-time { color: hsl(var(--foreground) / 0.65); display: inline-flex; align-items: center; gap: 5px; }

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

/* ── Body + Sections ── */
.modal-body {
    padding: 8px 24px 18px;
    flex: 1;
    overflow-y: auto;
    min-height: 0;
}
.edit-section {
    padding: 18px 0;
    border-bottom: 1px solid hsl(var(--foreground) / 0.08);
}
.edit-section:last-of-type { border-bottom: none; }
.edit-section:first-of-type { padding-top: 4px; }

.es-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 14px;
}
.es-head > div { display: inline-flex; align-items: center; gap: 8px; }
.es-head i { color: hsl(var(--primary, 254 56% 38%)); font-size: 13px; }
.es-head b { font-size: 14px; font-weight: 600; color: hsl(var(--primary-dark, 254 56% 21%)); }

.btn-text {
    background: none;
    border: 1px solid hsl(var(--primary, 254 56% 38%) / 0.25);
    color: hsl(var(--primary-dark, 254 56% 21%));
    font-size: 12px;
    font-weight: 500;
    font-family: inherit;
    padding: 6px 12px;
    border-radius: 8px;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    transition: all 0.15s;
}
.btn-text:hover { background: hsl(var(--primary, 254 56% 38%) / 0.08); border-color: hsl(var(--primary, 254 56% 38%)); }
.btn-text i { font-size: 11px; }

/* ── Slot atual (sem reagendamento) ── */
.current-slot {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 16px 18px;
    background: hsl(var(--primary, 254 56% 38%) / 0.04);
    border: 1px solid hsl(var(--primary, 254 56% 38%) / 0.15);
    border-radius: 12px;
}
.cs-time {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding-right: 16px;
    border-right: 1px solid hsl(var(--primary, 254 56% 38%) / 0.15);
}
.cs-time .hh { font-size: 22px; font-weight: 700; color: hsl(var(--primary-dark, 254 56% 21%)); line-height: 1.1; }
.cs-time .dur { font-size: 11px; color: hsl(var(--foreground) / 0.55); margin-top: 2px; }
.cs-info { display: flex; flex-direction: column; gap: 2px; }
.cs-info b { font-size: 14px; color: hsl(var(--primary-dark, 254 56% 21%)); text-transform: capitalize; }
.cs-info span { font-size: 12px; color: hsl(var(--foreground) / 0.65); }

/* ── Bloco de reagendamento ── */
.resched-block { display: flex; flex-direction: column; gap: 14px; }

.from-to {
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 14px 16px;
    background: hsl(160 79% 39% / 0.06);
    border: 1px solid hsl(160 79% 39% / 0.25);
    border-radius: 12px;
    position: relative;
}
.ft-side { display: flex; flex-direction: column; gap: 2px; flex: 1; }
.ft-side .lbl {
    font-size: 10px;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: hsl(var(--foreground) / 0.55);
    font-weight: 600;
}
.ft-side b { font-size: 18px; color: hsl(var(--primary-dark, 254 56% 21%)); }
.ft-side .dt { font-size: 12px; color: hsl(var(--foreground) / 0.7); text-transform: capitalize; }
.ft-side.from b { color: hsl(var(--foreground) / 0.55); text-decoration: line-through; }
.ft-side.to b { color: hsl(160 79% 39%); }
.ft-arr { color: hsl(160 79% 39%); font-size: 14px; }
.ft-undo {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--foreground) / 0.12);
    width: 30px;
    height: 30px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    color: hsl(var(--foreground) / 0.7);
    transition: all 0.15s;
    font-family: inherit;
}
.ft-undo:hover { color: hsl(0 84% 60%); border-color: hsl(0 84% 60% / 0.3); background: hsl(0 84% 60% / 0.05); }

/* ── Strip de dias ── */
.day-strip {
    display: flex;
    gap: 6px;
    overflow-x: auto;
    padding: 2px 0 6px;
    scrollbar-width: thin;
}
.day-strip::-webkit-scrollbar { height: 6px; }
.day-strip::-webkit-scrollbar-thumb { background: hsl(var(--foreground) / 0.12); border-radius: 3px; }
.day-btn {
    flex-shrink: 0;
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 8px 10px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--foreground) / 0.12);
    border-radius: 10px;
    cursor: pointer;
    font-family: inherit;
    min-width: 56px;
    transition: all 0.15s;
}
.day-btn:hover { border-color: hsl(var(--primary, 254 56% 38%)); }
.day-btn .dow {
    font-size: 10px;
    font-weight: 600;
    color: hsl(var(--foreground) / 0.55);
    letter-spacing: 0.04em;
}
.day-btn .dn {
    font-size: 18px;
    font-weight: 700;
    color: hsl(var(--primary-dark, 254 56% 21%));
    line-height: 1.1;
    margin: 2px 0;
}
.day-btn .mo {
    font-size: 10px;
    color: hsl(var(--foreground) / 0.55);
    text-transform: capitalize;
}
.day-btn.active {
    background: hsl(var(--primary, 254 56% 38%));
    border-color: hsl(var(--primary, 254 56% 38%));
}
.day-btn.active .dow,
.day-btn.active .dn,
.day-btn.active .mo { color: white; }

/* ── Legenda dos slots ── */
.slots-info {
    display: flex;
    gap: 14px;
    flex-wrap: wrap;
    font-size: 11px;
    color: hsl(var(--foreground) / 0.65);
}
.slots-info span { display: inline-flex; align-items: center; gap: 5px; }
.slots-info .orig-mark { color: hsl(var(--primary-dark, 254 56% 21%)); font-weight: 500; }
.slot-dot { font-size: 8px; }
.dot-free { color: hsl(160 79% 39%); }
.dot-busy { color: hsl(var(--foreground) / 0.3); }

/* ── Estado vazio (dia fechado / sem horários) ── */
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

/* ── Grid de horários ── */
.time-slots {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(86px, 1fr));
    gap: 6px;
}
.time-slots .slot {
    padding: 8px 6px;
    border: 1px solid hsl(var(--foreground) / 0.12);
    border-radius: 8px;
    background: hsl(var(--card));
    color: hsl(var(--primary-dark, 254 56% 21%));
    font-size: 13px;
    font-weight: 600;
    font-family: inherit;
    cursor: pointer;
    transition: all 0.15s;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 4px;
}
.time-slots .slot:hover:not(:disabled) {
    border-color: hsl(var(--primary, 254 56% 38%));
    background: hsl(var(--primary, 254 56% 38%) / 0.05);
}
.time-slots .slot.active {
    background: hsl(var(--primary, 254 56% 38%));
    border-color: hsl(var(--primary, 254 56% 38%));
    color: white;
}
.time-slots .slot.busy {
    background: hsl(var(--foreground) / 0.04);
    color: hsl(var(--foreground) / 0.4);
    cursor: not-allowed;
    border-color: hsl(var(--foreground) / 0.08);
}
.time-slots .slot.busy:hover { border-color: hsl(var(--foreground) / 0.08); color: hsl(var(--foreground) / 0.4); background: hsl(var(--foreground) / 0.04); }
.time-slots .slot.original {
    border-color: hsl(var(--primary, 254 56% 38%));
    border-style: dashed;
    position: relative;
}
.time-slots .slot .mark { font-size: 9px; margin-left: 2px; }

/* ── Form grid ── */
.form-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 12px;
}
.field-group { display: flex; flex-direction: column; gap: 6px; min-width: 0; }
.field-group.full { grid-column: 1 / -1; }
.field-group label {
    font-size: 12px;
    font-weight: 600;
    color: hsl(var(--foreground) / 0.7);
    display: inline-flex;
    align-items: center;
    gap: 5px;
}
.field-group label .opt {
    font-size: 11px;
    color: hsl(var(--foreground) / 0.5);
    font-weight: 400;
}
.field-group select,
.field-group textarea,
.field-group input {
    padding: 9px 12px;
    border-radius: 9px;
    border: 1px solid hsl(var(--foreground) / 0.15);
    background: hsl(var(--card));
    font-family: inherit;
    font-size: 13px;
    color: hsl(var(--foreground));
    transition: border 0.15s;
    resize: vertical;
}
.field-group select:focus,
.field-group textarea:focus,
.field-group input:focus {
    outline: none;
    border-color: hsl(var(--primary, 254 56% 38%));
    box-shadow: 0 0 0 3px hsl(var(--primary, 254 56% 38%) / 0.12);
}

/* ── Erro ── */
.erro-banner {
    margin-top: 14px;
    padding: 10px 14px;
    background: hsl(0 84% 60% / 0.08);
    border: 1px solid hsl(0 84% 60% / 0.2);
    border-radius: 8px;
    color: hsl(0 84% 50%);
    font-size: 13px;
}

/* ── Footer ── */
.modal-foot {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 14px 24px;
    border-top: 1px solid hsl(var(--foreground) / 0.08);
    background: hsl(var(--foreground) / 0.02);
}
.spacer { flex: 1; }
.btn-ghost {
    padding: 9px 16px;
    background: none;
    border: 1px solid hsl(var(--foreground) / 0.12);
    border-radius: 9px;
    color: hsl(var(--foreground) / 0.65);
    font-size: 13px;
    font-weight: 500;
    font-family: inherit;
    cursor: pointer;
    transition: all 0.15s;
}
.btn-ghost:hover { background: hsl(var(--foreground) / 0.04); }

.btn-primary {
    padding: 9px 18px;
    background: hsl(var(--primary, 254 56% 38%));
    border: none;
    border-radius: 9px;
    color: white;
    font-size: 13px;
    font-weight: 600;
    font-family: inherit;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    box-shadow: 0 4px 12px hsl(var(--primary, 254 56% 38%) / 0.3);
    transition: all 0.15s;
}
.btn-primary:hover:not(:disabled) { background: hsl(var(--primary-dark, 254 56% 21%)); }
.btn-primary.success { background: hsl(160 79% 39%); box-shadow: 0 4px 12px hsl(160 79% 39% / 0.3); }
.btn-primary.success:hover:not(:disabled) { background: hsl(160 79% 30%); }
.btn-primary:disabled, .btn-primary[disabled] { opacity: 0.5; cursor: not-allowed; box-shadow: none; }

@media (max-width: 600px) {
    .form-grid { grid-template-columns: 1fr; }
}
</style>
