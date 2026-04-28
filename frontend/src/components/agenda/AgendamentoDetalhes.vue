<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue"
import AppDrawer from "@/components/ui/AppDrawer.vue"
import AppBadge  from "@/components/ui/AppBadge.vue"
import { AppButton } from "@/components/ui"
import AgendamentoFormFields, { type AgendamentoFormModel } from "@/components/agenda/AgendamentoFormFields.vue"
import { agendaService, type Agendamento } from "@/services/agendaService"
import { vinculoService, type ProfissionalVinculado } from "@/services/vinculoService"
import { pacienteService, type PacienteListaItem } from "@/services/pacienteService"

const props = defineProps<{
    aberto: boolean
    agendamento: Agendamento | null
    /** Agendamentos do mês/período (usados pelo SlotPicker para excluir horários ocupados). */
    agendamentosTodos?: Agendamento[]
}>()

const emit = defineEmits<{
    fechar: []
    atualizado: []
}>()

const processando      = ref(false)
const erro             = ref<string | null>(null)
const modoCancelamento = ref(false)
const motivoCancelar   = ref("")

// ─── Modo edição ──────────────────────────────────────────────────────────────
const modoEdicao = ref(false)

// Listas auxiliares (carregadas sob demanda ao abrir o modo edição).
const profissionais = ref<ProfissionalVinculado[]>([])
const pacientes     = ref<PacienteListaItem[]>([])

const form = reactive<AgendamentoFormModel>({
    pacienteId:            0,
    profissionalUsuarioId: "",
    data:                  "",
    hora:                  "",
    duracaoMin:            30,
    tipoServico:           "",
    especialidade:         "",
    contato:               "",
    observacoes:           null,
})

function isoLocal(iso: string) {
    const d = new Date(iso)
    const yyyy = d.getFullYear()
    const mm   = String(d.getMonth() + 1).padStart(2, "0")
    const dd   = String(d.getDate()).padStart(2, "0")
    const hh   = String(d.getHours()).padStart(2, "0")
    const mn   = String(d.getMinutes()).padStart(2, "0")
    return { data: `${yyyy}-${mm}-${dd}`, hora: `${hh}:${mn}` }
}

watch(() => props.aberto, (a) => {
    if (!a) {
        modoCancelamento.value = false
        modoEdicao.value       = false
        motivoCancelar.value   = ""
        erro.value             = null
    }
})

// Ao entrar em modoEdicao, popular form e carregar listas auxiliares.
watch(modoEdicao, async (novo) => {
    if (!novo || !props.agendamento) return

    const ag = props.agendamento
    const ini = isoLocal(ag.inicioPrevisto)
    const iniMs = new Date(ag.inicioPrevisto).getTime()
    const fimMs = new Date(ag.fimPrevisto).getTime()

    form.data                  = ini.data
    form.hora                  = ini.hora
    form.duracaoMin            = Math.max(15, Math.round((fimMs - iniMs) / 60_000))
    form.tipoServico           = ag.tipoServico
    form.especialidade         = ""
    form.observacoes           = ag.observacoes
    form.pacienteId            = ag.pacienteId
    form.profissionalUsuarioId = ag.profissionalUsuarioId
    form.contato               = ""

    // Carrega listas se ainda vazias (para que selects mostrem nomes legíveis).
    if (profissionais.value.length === 0) {
        try { profissionais.value = await vinculoService.listarProfissionais() } catch { /* noop */ }
    }
    if (pacientes.value.length === 0) {
        try {
            const pg = await pacienteService.listar(undefined, 1, 200)
            pacientes.value = pg.itens
        } catch { /* noop */ }
    }

    // Auto-preenche especialidade e contato a partir das listas carregadas.
    const prof = profissionais.value.find(p => p.usuarioId === ag.profissionalUsuarioId)
    if (prof?.especialidade) form.especialidade = prof.especialidade

    const pac = pacientes.value.find(p => p.id === ag.pacienteId)
    if (pac?.telefone) form.contato = pac.telefone
})

const podeCancelar = computed(() => {
    if (!props.agendamento) return false
    return props.agendamento.status === "Agendado" || props.agendamento.status === "Confirmado"
})

const podeEditar = computed(() => {
    if (!podeCancelar.value) return false
    // Edição só é permitida enquanto o agendamento ainda não ocorreu.
    return new Date(props.agendamento!.inicioPrevisto).getTime() > Date.now()
})

const dataFmt = computed(() => {
    if (!props.agendamento) return ""
    const ini = new Date(props.agendamento.inicioPrevisto)
    return ini.toLocaleDateString("pt-BR", {
        weekday: "long", day: "numeric", month: "long", year: "numeric",
    })
})

const horaFmt = computed(() => {
    if (!props.agendamento) return ""
    const ini = new Date(props.agendamento.inicioPrevisto)
    const fim = new Date(props.agendamento.fimPrevisto)
    const h = (d: Date) => d.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })
    return `${h(ini)} — ${h(fim)}`
})

// Lista de agendamentos para o SlotPicker (em edição): exclui o próprio agendamento.
const agendamentosParaSlot = computed(() => {
    const todos = props.agendamentosTodos ?? (props.agendamento ? [props.agendamento] : [])
    if (!props.agendamento) return todos
    return todos.filter(a => a.id !== props.agendamento!.id)
})

async function salvarEdicao() {
    if (!props.agendamento) return
    if (!form.data || !form.hora || !form.tipoServico || !form.profissionalUsuarioId) return
    processando.value = true
    erro.value = null
    try {
        const ini = new Date(`${form.data}T${form.hora}`)
        const fim = new Date(ini.getTime() + form.duracaoMin * 60_000)
        await agendaService.atualizar(props.agendamento.id, {
            profissionalUsuarioId: form.profissionalUsuarioId,
            inicioPrevisto:        ini.toISOString(),
            fimPrevisto:           fim.toISOString(),
            tipoServico:           form.tipoServico,
            observacoes:           (form.observacoes ?? "").trim() || null,
        })
        emit("atualizado")
        modoEdicao.value = false
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao atualizar agendamento."
    } finally {
        processando.value = false
    }
}

async function confirmar() {
    if (!props.agendamento) return
    processando.value = true; erro.value = null
    try {
        await agendaService.confirmar(props.agendamento.id)
        emit("atualizado"); emit("fechar")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro."
    } finally { processando.value = false }
}

async function concluir() {
    if (!props.agendamento) return
    processando.value = true; erro.value = null
    try {
        await agendaService.concluir(props.agendamento.id)
        emit("atualizado"); emit("fechar")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro."
    } finally { processando.value = false }
}

async function confirmarCancelamento() {
    if (!props.agendamento || !motivoCancelar.value.trim()) return
    processando.value = true; erro.value = null
    try {
        await agendaService.cancelar(props.agendamento.id, motivoCancelar.value.trim())
        emit("atualizado"); emit("fechar")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro."
    } finally { processando.value = false }
}
</script>

<template>
    <AppDrawer :aberto="aberto" titulo="Detalhes do agendamento" :largura="600" @fechar="$emit('fechar')">
        <div v-if="agendamento" class="detalhes">
            <!-- Status -->
            <div class="status-linha">
                <AppBadge :status="agendamento.status" />
                <span v-if="agendamento.status === 'Cancelado' && agendamento.motivoCancelamento" class="motivo">
                    · {{ agendamento.motivoCancelamento }}
                </span>
            </div>

            <!-- ============ MODO VISUALIZAÇÃO ============ -->
            <template v-if="!modoEdicao">
                <div class="bloco">
                    <span class="bloco-label">Data e horário</span>
                    <span class="bloco-valor">{{ dataFmt }}</span>
                    <span class="bloco-meta">{{ horaFmt }}</span>
                </div>

                <div class="bloco">
                    <span class="bloco-label">Paciente</span>
                    <span class="bloco-valor">{{ agendamento.pacienteNome }}</span>
                </div>

                <div class="bloco">
                    <span class="bloco-label">Profissional</span>
                    <span class="bloco-valor">{{ agendamento.profissionalNome }}</span>
                </div>

                <div class="bloco">
                    <span class="bloco-label">Tipo da consulta</span>
                    <span class="bloco-valor">{{ agendamento.tipoServico }}</span>
                </div>

                <div class="bloco" v-if="agendamento.observacoes">
                    <span class="bloco-label">Observações</span>
                    <span class="bloco-valor bloco-multilinha">{{ agendamento.observacoes }}</span>
                </div>

                <div class="bloco bloco-footer">
                    <span class="bloco-meta">Agendado por {{ agendamento.criadoPorNome }}</span>
                </div>
            </template>

            <!-- ============ MODO EDIÇÃO ============ -->
            <template v-else>
                <p class="aviso-edicao">
                    Editando agendamento. Para trocar o paciente, cancele e crie um novo agendamento.
                </p>

                <AgendamentoFormFields
                    v-model="form"
                    :profissionais="profissionais"
                    :pacientes="pacientes"
                    :agendamentos="agendamentosParaSlot"
                    modo="editar"
                />
            </template>

            <!-- Cancelamento (form inline) -->
            <div v-if="modoCancelamento" class="cancel-box">
                <label class="campo-label">Motivo do cancelamento <span class="obrig">*</span></label>
                <textarea
                    v-model="motivoCancelar"
                    rows="3"
                    class="input-field"
                    placeholder="Informe o motivo..."
                    :disabled="processando"
                ></textarea>
            </div>

            <p v-if="erro" class="msg-erro">{{ erro }}</p>
        </div>

        <template #rodape>
            <template v-if="modoCancelamento">
                <AppButton variant="secondary" :disabled="processando" @click="modoCancelamento = false">
                    Voltar
                </AppButton>
                <AppButton
                    variant="danger"
                    :disabled="processando || !motivoCancelar.trim()"
                    @click="confirmarCancelamento"
                >{{ processando ? "Cancelando..." : "Confirmar cancelamento" }}</AppButton>
            </template>

            <template v-else-if="modoEdicao">
                <AppButton variant="secondary" :disabled="processando" @click="modoEdicao = false">
                    Cancelar edição
                </AppButton>
                <AppButton
                    :disabled="processando || !form.data || !form.hora || !form.tipoServico || !form.profissionalUsuarioId"
                    @click="salvarEdicao"
                >{{ processando ? "Salvando..." : "Salvar alterações" }}</AppButton>
            </template>

            <template v-else-if="agendamento">
                <AppButton
                    v-if="podeEditar"
                    variant="secondary"
                    :disabled="processando"
                    @click="modoEdicao = true"
                >Editar</AppButton>

                <AppButton
                    v-if="podeCancelar"
                    variant="danger"
                    :disabled="processando"
                    @click="modoCancelamento = true"
                >Cancelar</AppButton>

                <AppButton
                    v-if="agendamento.status === 'Agendado'"
                    :disabled="processando"
                    @click="confirmar"
                >Confirmar</AppButton>

                <AppButton
                    v-if="agendamento.status === 'Confirmado'"
                    :disabled="processando"
                    @click="concluir"
                >Concluir atendimento</AppButton>

                <AppButton
                    v-if="agendamento.status === 'Cancelado' || agendamento.status === 'Concluido'"
                    variant="secondary"
                    @click="$emit('fechar')"
                >Fechar</AppButton>
            </template>
        </template>
    </AppDrawer>
</template>

<style scoped>
.detalhes { display: flex; flex-direction: column; gap: 1rem; }

.status-linha { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }
.motivo { font-size: 0.82em; color: var(--text-muted); }

.bloco {
    display: flex; flex-direction: column; gap: 0.2rem;
    padding: 0.7rem 0.9rem; background: var(--bg-hover);
    border-radius: var(--radius);
}
.bloco-label { font-size: 0.72em; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em; color: var(--text-muted); }
.bloco-valor { font-size: 0.92em; font-weight: 600; }
.bloco-meta  { font-size: 0.78em; color: var(--text-muted); }
.bloco-multilinha { white-space: pre-wrap; line-height: 1.4; font-weight: 400; }
.bloco-footer { background: none; padding: 0; }

.aviso-edicao {
    background: #eff6ff; color: #1e3a8a; padding: 0.6rem 0.8rem;
    border-radius: var(--radius); font-size: 0.8em; margin: 0;
    border-left: 3px solid #3b82f6;
}

.cancel-box { display: flex; flex-direction: column; gap: 0.3rem; margin-top: 0.25rem; }

.campo-label { font-size: 0.82em; font-weight: 600; color: var(--text-muted); }
.obrig { color: var(--danger); }

.input-field {
    padding: 0.5rem 0.75rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.875em;
    background: var(--bg-card); color: var(--text); resize: vertical;
}
.input-field:focus { outline: none; border-color: hsl(var(--primary)); }

.msg-erro { color: var(--danger); font-size: 0.875em; margin: 0; }
</style>
