<script setup lang="ts">
/**
 * CancelarAgendamentoModal — substitui o window.prompt() do cancelamento.
 *
 * - Motivos pré-definidos (categorizáveis para relatórios futuros).
 * - Campo de observação opcional (livre, até 500 chars — mesmo limite da coluna).
 * - Confirma só com motivo escolhido (validação espelha a do backend: motivo obrigatório).
 */
import { computed, ref, watch } from "vue"
import { AppButton, AppModal } from "@/components/ui"
import { agendaService, type Agendamento } from "@/services/agendaService"
import { formatDataHora } from "@/utils/datetime"

const props = defineProps<{
    aberto: boolean
    agendamento: Agendamento | null
}>()

const emit = defineEmits<{
    (e: "fechar"): void
    (e: "cancelado"): void
}>()

const MOTIVOS_PREDEFINIDOS = [
    { id: "desistencia",   label: "Paciente desistiu",     icone: "fa-user-xmark"        },
    { id: "reagendado",    label: "Reagendado",            icone: "fa-calendar-plus"     },
    { id: "no-show",       label: "Sem comparecimento",    icone: "fa-user-clock"        },
    { id: "emergencia",    label: "Emergência médica",     icone: "fa-triangle-exclamation" },
    { id: "indisponivel",  label: "Profissional indisponível", icone: "fa-user-doctor" },
    { id: "outro",         label: "Outro",                 icone: "fa-ellipsis"          },
] as const

type MotivoId = typeof MOTIVOS_PREDEFINIDOS[number]["id"]

const motivoSelecionado = ref<MotivoId | null>(null)
const observacao = ref("")
const executando = ref(false)
const erro = ref<string | null>(null)

watch(() => props.aberto, (aberto) => {
    if (!aberto) {
        motivoSelecionado.value = null
        observacao.value = ""
        erro.value = null
        executando.value = false
    }
})

const motivoFinal = computed(() => {
    if (!motivoSelecionado.value) return ""
    const label = MOTIVOS_PREDEFINIDOS.find(m => m.id === motivoSelecionado.value)?.label ?? ""
    const obs = observacao.value.trim()
    if (!obs) return label
    return `${label} — ${obs}`.slice(0, 500)
})

const podeConfirmar = computed(() => motivoSelecionado.value !== null && !executando.value)

function fmtDataHora(iso: string) {
    return formatDataHora(iso, {
        weekday: "long",
        day: "2-digit",
        month: "long",
        hour: "2-digit",
        minute: "2-digit",
    })
}

async function confirmar() {
    if (!props.agendamento || !podeConfirmar.value) return
    executando.value = true
    erro.value = null
    try {
        await agendaService.cancelar(props.agendamento.id, motivoFinal.value)
        emit("cancelado")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao cancelar agendamento."
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
                <h2>Cancelar agendamento</h2>
                <span v-if="agendamento">
                    {{ agendamento.pacienteNome }} · {{ fmtDataHora(agendamento.inicioPrevisto) }}
                </span>
            </div>
        </template>

        <p class="aviso">
            Escolha o motivo do cancelamento. Essa informação aparece no histórico
            do paciente e é usada em relatórios de no-show.
        </p>

        <div class="motivos">
            <button
                v-for="m in MOTIVOS_PREDEFINIDOS"
                :key="m.id"
                type="button"
                class="motivo-btn"
                :class="{ ativo: motivoSelecionado === m.id }"
                :disabled="executando"
                @click="motivoSelecionado = m.id"
            >
                <i :class="['fa-solid', m.icone]" aria-hidden="true"></i>
                <span>{{ m.label }}</span>
            </button>
        </div>

        <label class="obs-wrapper">
            <span class="obs-lbl">Observação (opcional)</span>
            <textarea
                v-model="observacao"
                class="obs-input"
                rows="3"
                maxlength="400"
                placeholder="Detalhes adicionais que ajudem a entender o cancelamento."
                :disabled="executando"
            ></textarea>
            <span class="obs-contador">{{ observacao.length }}/400</span>
        </label>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="ghost" :disabled="executando" @click="fechar">Voltar</AppButton>
            <AppButton
                variant="danger"
                icon="fa-solid fa-ban"
                :loading="executando"
                :disabled="!podeConfirmar"
                @click="confirmar"
            >
                Cancelar agendamento
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.modal-titulo h2 {
    font-size: var(--text-lg);
    font-weight: var(--font-weight-bold);
    color: hsl(var(--primary-dark));
    margin: 0 0 2px;
}
.modal-titulo span {
    font-size: 13px;
    color: hsl(var(--secondary) / 0.65);
}

.aviso {
    margin: 0;
    font-size: 13px;
    color: hsl(var(--foreground) / 0.7);
    line-height: 1.45;
}

.motivos {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 8px;
}
.motivo-btn {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 12px 14px;
    border-radius: 8px;
    border: 1px solid hsl(var(--foreground) / 0.12);
    background: hsl(var(--card));
    font-family: inherit;
    font-size: 13px;
    font-weight: 500;
    color: hsl(var(--foreground));
    cursor: pointer;
    transition: all 0.15s;
    text-align: left;
}
.motivo-btn > i {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    background: hsl(var(--foreground) / 0.06);
    color: hsl(var(--foreground) / 0.55);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    flex-shrink: 0;
    transition: all 0.15s;
}
.motivo-btn:hover:not(:disabled) {
    border-color: hsl(var(--primary) / 0.4);
    background: hsl(var(--primary) / 0.04);
}
.motivo-btn.ativo {
    border-color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.08);
    color: hsl(var(--primary-dark));
}
.motivo-btn.ativo > i {
    background: hsl(var(--primary) / 0.15);
    color: hsl(var(--primary));
}
.motivo-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.obs-wrapper {
    display: flex;
    flex-direction: column;
    gap: 6px;
    position: relative;
}
.obs-lbl {
    font-size: 11px;
    font-weight: 700;
    color: hsl(var(--foreground) / 0.55);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.obs-input {
    width: 100%;
    border: 1px solid hsl(var(--foreground) / 0.15);
    border-radius: 8px;
    padding: 10px 12px;
    font-family: inherit;
    font-size: 13px;
    color: hsl(var(--foreground));
    background: hsl(var(--card));
    resize: vertical;
    min-height: 64px;
}
.obs-input:focus {
    outline: none;
    border-color: hsl(var(--primary));
    box-shadow: 0 0 0 3px hsl(var(--primary) / 0.12);
}
.obs-contador {
    align-self: flex-end;
    font-size: 11px;
    color: hsl(var(--foreground) / 0.5);
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

@media (max-width: 540px) {
    .motivos { grid-template-columns: 1fr; }
}
</style>
