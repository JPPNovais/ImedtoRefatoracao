<script setup lang="ts">
/**
 * CheckInModal — confirma a chegada do paciente.
 *
 * Fluxo:
 * 1. Exibe resumo do agendamento (profissional, data/hora, tipo de serviço).
 * 2. Exibe dados do paciente (nome, telefone, CPF) carregados via pacienteService.
 * 3. Solicita edição via emit `editar-paciente` — o pai (AgendaView) renderiza o
 *    PacienteFormModal paralelo a este modal, evitando portais aninhados (overlay
 *    do design-system fica em z-50 fixo e some quando um Dialog abre dentro de outro).
 * 4. Ao confirmar: registra o check-in.
 * 5. Emite `checkin-realizado` em caso de sucesso; exibe erro 422 sem fechar.
 */
import { computed, ref, watch } from "vue"
import { AppButton, AppField, AppModal, AppSelect } from "@/components/ui"
import { agendaService, type Agendamento } from "@/services/agendaService"
import { formatDataHora, formatHora } from "@/utils/datetime"
import { pacienteService, type Paciente } from "@/services/pacienteService"
import { salaService, type Sala } from "@/services/salaService"
import { useTenantStore } from "@/stores/tenantStore"

const tenant = useTenantStore()

const props = defineProps<{
    aberto: boolean
    agendamento: Agendamento | null
    outrosAgendamentosDoDia?: Agendamento[]
    // Quando o pai termina a edição do paciente, repassa o objeto atualizado
    // aqui para refletir nome/CPF/telefone sem refetch.
    pacienteAtualizado?: Paciente | null
}>()

const emit = defineEmits<{
    (e: "fechar"): void
    (e: "checkin-realizado"): void
    (e: "editar-paciente", paciente: Paciente): void
}>()

const paciente = ref<Paciente | null>(null)
const carregandoPaciente = ref(false)
const erroPaciente = ref<string | null>(null)
const erroCheckIn = ref<string | null>(null)
const executando = ref(false)

// Salas ativas + seleção
const salas = ref<Sala[]>([])
const salasCarregadas = ref(false)
const salaIdSel = ref<number | null>(null)

async function garantirSalas() {
    if (salasCarregadas.value) return
    const estabId = tenant.estabelecimentoAtivoId
    if (!estabId) return
    try {
        salas.value = await salaService.listar(estabId, true)
        salasCarregadas.value = true
    } catch {
        // Não crítico — check-in funciona sem sala.
    }
}

/** Pré-seleciona a última sala usada pelo profissional do agendamento no dia. */
function sugerirSala(): number | null {
    if (!props.agendamento) return null
    if (props.agendamento.salaId != null) return props.agendamento.salaId
    const profId = props.agendamento.profissionalUsuarioId
    const candidatos = (props.outrosAgendamentosDoDia ?? [])
        .filter(a =>
            a.id !== props.agendamento!.id
            && a.profissionalUsuarioId === profId
            && a.salaId != null
            && (a.status === "Concluido" || (a.status === "Confirmado" && a.checkInEm != null)),
        )
        .sort((a, b) => b.inicioPrevisto.localeCompare(a.inicioPrevisto))
    return candidatos[0]?.salaId ?? null
}

/** Outro agendamento que já ocupa a sala selecionada no momento. */
const ocupacao = computed(() => {
    if (salaIdSel.value == null || !props.agendamento) return null
    return (props.outrosAgendamentosDoDia ?? []).find(a =>
        a.id !== props.agendamento!.id
        && a.salaId === salaIdSel.value
        && a.checkInEm != null
        && a.status !== "Concluido"
        && a.status !== "Cancelado",
    ) ?? null
})

// Carrega o paciente ao abrir o modal
watch(() => props.aberto, async (aberto) => {
    if (!aberto) {
        paciente.value = null
        erroCheckIn.value = null
        erroPaciente.value = null
        salaIdSel.value = null
        return
    }
    if (!props.agendamento) return
    carregandoPaciente.value = true
    erroPaciente.value = null
    // Carrega salas em paralelo com o paciente.
    void garantirSalas().then(() => {
        salaIdSel.value = sugerirSala()
    })
    try {
        paciente.value = await pacienteService.obter(props.agendamento.pacienteId)
    } catch {
        erroPaciente.value = "Não foi possível carregar os dados do paciente."
    } finally {
        carregandoPaciente.value = false
    }
})

function fmtDataHora(iso: string) {
    return formatDataHora(iso, {
        weekday: "long",
        day: "2-digit",
        month: "long",
        year: "numeric",
    })
}

function fmtHora(iso: string) {
    return formatHora(iso)
}

const duracaoMin = computed(() => {
    if (!props.agendamento) return 0
    const ini = new Date(props.agendamento.inicioPrevisto).getTime()
    const fim = new Date(props.agendamento.fimPrevisto).getTime()
    return Math.round((fim - ini) / 60000)
})

// O pai dispara a edição via emit; quando salvar, devolve o paciente
// atualizado via prop `pacienteAtualizado` — refletimos aqui.
watch(() => props.pacienteAtualizado, (p) => {
    if (p) paciente.value = p
})

function solicitarEdicaoPaciente() {
    if (!paciente.value) return
    emit("editar-paciente", paciente.value)
}

async function confirmarCheckIn() {
    if (!props.agendamento || executando.value) return
    executando.value = true
    erroCheckIn.value = null
    try {
        await agendaService.registrarCheckIn(props.agendamento.id, salaIdSel.value)
        emit("checkin-realizado")
    } catch (e: any) {
        erroCheckIn.value = e?.response?.data?.mensagem ?? "Erro ao registrar check-in."
    } finally {
        executando.value = false
    }
}

function fechar() {
    if (executando.value) return
    emit("fechar")
}
</script>

<template>
    <AppModal :aberto="aberto" largura="md" @fechar="fechar">
        <template #titulo>
            <div class="modal-titulo">
                <h2>Check-in do paciente</h2>
                <span>Revise os dados do paciente antes de confirmar a chegada.</span>
            </div>
        </template>

        <!-- Resumo do agendamento -->
        <div v-if="agendamento" class="resumo-agendamento">
            <div class="resumo-linha">
                <i class="fa-solid fa-calendar-day" aria-hidden="true"></i>
                <div>
                    <span class="resumo-lbl">Data e hora</span>
                    <span class="resumo-val">
                        {{ fmtDataHora(agendamento.inicioPrevisto) }}
                        ({{ duracaoMin }} min)
                    </span>
                </div>
            </div>
            <div class="resumo-linha">
                <i class="fa-solid fa-user-doctor" aria-hidden="true"></i>
                <div>
                    <span class="resumo-lbl">Profissional</span>
                    <span class="resumo-val">{{ agendamento.profissionalNome }}</span>
                </div>
            </div>
            <div class="resumo-linha">
                <i class="fa-solid fa-stethoscope" aria-hidden="true"></i>
                <div>
                    <span class="resumo-lbl">Tipo de serviço</span>
                    <span class="resumo-val">{{ agendamento.tipoServico }}</span>
                </div>
            </div>
        </div>

        <!-- Dados do paciente -->
        <div class="secao-paciente">
            <div class="secao-cabecalho">
                <span class="secao-titulo">
                    <i class="fa-solid fa-user" aria-hidden="true"></i>
                    Paciente
                </span>
                <button
                    v-if="paciente"
                    type="button"
                    class="btn-editar-pac"
                    :disabled="executando"
                    @click="solicitarEdicaoPaciente"
                >
                    <i class="fa-solid fa-pen-to-square" aria-hidden="true"></i>
                    Editar dados
                </button>
            </div>

            <div v-if="carregandoPaciente" class="estado-pac">
                <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
                Carregando dados do paciente...
            </div>

            <p v-else-if="erroPaciente" class="msg-erro">{{ erroPaciente }}</p>

            <div v-else-if="paciente" class="dados-paciente">
                <div class="dado-item">
                    <span class="dado-lbl">Nome</span>
                    <span class="dado-val">{{ paciente.nomeCompleto }}</span>
                </div>
                <div v-if="paciente.cpf || paciente.documentoInternacional" class="dado-item">
                    <span class="dado-lbl">{{ paciente.documentoInternacional ? "Doc. Internacional" : "CPF" }}</span>
                    <span class="dado-val">{{ paciente.documentoInternacional ?? paciente.cpf }}</span>
                </div>
                <div v-if="paciente.telefone" class="dado-item">
                    <span class="dado-lbl">Telefone</span>
                    <span class="dado-val">{{ paciente.telefone }}</span>
                </div>
                <div v-if="paciente.dataNascimento" class="dado-item">
                    <span class="dado-lbl">Nascimento</span>
                    <span class="dado-val">
                        {{ new Date(paciente.dataNascimento).toLocaleDateString("pt-BR") }}
                    </span>
                </div>
                <div v-if="paciente.alertas && paciente.alertas.length" class="alertas-resumo">
                    <i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i>
                    <span>{{ paciente.alertas.join(" · ") }}</span>
                </div>
            </div>
        </div>

        <!-- Seleção de sala (opcional) -->
        <div v-if="salasCarregadas && salas.length > 0" class="secao-sala">
            <AppField label="Sala (opcional)">
                <AppSelect v-model.number="salaIdSel">
                    <option :value="null">— Sem sala —</option>
                    <option v-for="s in salas" :key="s.id" :value="s.id">
                        {{ s.nome }} — {{ s.unidadeNome }}<template v-if="s.tipoSalaNome"> · {{ s.tipoSalaNome }}</template>
                    </option>
                </AppSelect>
            </AppField>
            <div v-if="ocupacao" class="alerta-ocupacao">
                <i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i>
                <div>
                    <strong>Atenção:</strong>
                    sala em uso por <strong>{{ ocupacao.pacienteNome }}</strong>
                    (check-in às {{ formatHora(ocupacao.checkInEm!) }}). Você pode alocar mesmo assim.
                </div>
            </div>
        </div>

        <p v-if="erroCheckIn" class="msg-erro">{{ erroCheckIn }}</p>

        <template #rodape>
            <AppButton variant="ghost" :disabled="executando" @click="fechar">Cancelar</AppButton>
            <AppButton
                icon="fa-solid fa-user-check"
                :loading="executando"
                :disabled="executando || carregandoPaciente || !!erroPaciente"
                @click="confirmarCheckIn"
            >
                Confirmar check-in
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.modal-titulo h2 {
    font-size: 18px;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    margin: 0 0 2px;
}
.modal-titulo span {
    font-size: 13px;
    color: hsl(var(--secondary) / 0.65);
}

/* Resumo do agendamento */
.resumo-agendamento {
    background: hsl(var(--primary) / 0.04);
    border: 1px solid hsl(var(--primary) / 0.12);
    border-radius: 8px;
    padding: 12px 14px;
    display: flex;
    flex-direction: column;
    gap: 8px;
}
.resumo-linha {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    font-size: 13px;
}
.resumo-linha > i {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    background: hsl(var(--primary) / 0.10);
    color: hsl(var(--primary));
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    flex-shrink: 0;
    margin-top: 1px;
}
.resumo-linha > div {
    display: flex;
    flex-direction: column;
    gap: 2px;
}
.resumo-lbl {
    font-size: 10px;
    font-weight: 700;
    color: hsl(var(--foreground) / 0.5);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.resumo-val {
    font-size: 13px;
    font-weight: 500;
    color: hsl(var(--foreground));
}

/* Seção paciente */
.secao-paciente {
    display: flex;
    flex-direction: column;
    gap: 10px;
}
.secao-cabecalho {
    display: flex;
    align-items: center;
    justify-content: space-between;
}
.secao-titulo {
    font-size: 11px;
    font-weight: 700;
    color: hsl(var(--foreground) / 0.55);
    text-transform: uppercase;
    letter-spacing: 0.04em;
    display: inline-flex;
    align-items: center;
    gap: 6px;
}
.btn-editar-pac {
    background: transparent;
    border: 1px solid hsl(var(--foreground) / 0.12);
    border-radius: 6px;
    padding: 4px 10px;
    font-family: inherit;
    font-size: 12px;
    font-weight: 600;
    color: hsl(var(--primary));
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 5px;
    transition: all 0.15s;
}
.btn-editar-pac:hover:not(:disabled) {
    background: hsl(var(--primary) / 0.06);
    border-color: hsl(var(--primary) / 0.3);
}
.btn-editar-pac:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.estado-pac {
    padding: 16px;
    text-align: center;
    color: hsl(var(--foreground) / 0.55);
    font-size: 13px;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
}

.dados-paciente {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--foreground) / 0.08);
    border-radius: 8px;
    padding: 12px 14px;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 10px 16px;
}
.dado-item {
    display: flex;
    flex-direction: column;
    gap: 2px;
}
.dado-lbl {
    font-size: 10px;
    font-weight: 700;
    color: hsl(var(--foreground) / 0.5);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.dado-val {
    font-size: 13px;
    font-weight: 500;
    color: hsl(var(--foreground));
}
.alertas-resumo {
    grid-column: 1 / -1;
    display: flex;
    align-items: center;
    gap: 8px;
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-left-width: 3px;
    border-radius: 6px;
    padding: 8px 12px;
    font-size: 12px;
    color: hsl(0 70% 30%);
    font-weight: 500;
}
.alertas-resumo > i {
    color: hsl(var(--error));
    flex-shrink: 0;
    font-size: 12px;
}

.msg-erro {
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-radius: 8px;
    padding: 8px 12px;
    font-size: 13px;
    margin: 0;
}

.secao-sala { display: flex; flex-direction: column; gap: 8px; }
.alerta-ocupacao {
    display: flex; align-items: flex-start; gap: 10px;
    background: hsl(45 96% 47% / 0.12);
    border: 1px solid hsl(45 96% 47% / 0.35);
    border-left: 3px solid hsl(45 96% 47%);
    border-radius: 8px;
    padding: 10px 12px;
    font-size: 12px;
    color: hsl(35 90% 25%);
}
.alerta-ocupacao > i { font-size: 13px; margin-top: 2px; flex-shrink: 0; }
.alerta-ocupacao strong { font-weight: 700; }

@media (max-width: 540px) {
    .dados-paciente { grid-template-columns: 1fr; }
}
</style>
