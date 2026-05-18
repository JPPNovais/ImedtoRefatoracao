<script setup lang="ts">
/**
 * AlocarSalaModal — atribui (ou troca) a sala de um agendamento.
 *
 * - Lista apenas salas ativas (`salaService.listar(estabId, true)`).
 * - Pré-seleciona `agendamento.salaId` (se houver).
 * - Mostra aviso amarelo quando a sala selecionada já está ocupada por outro
 *   agendamento do dia com check-in feito e status não concluído/cancelado.
 *   O aviso é apenas informativo — não bloqueia a alocação.
 * - "Desalocar" só aparece quando o agendamento já tinha sala.
 */
import { computed, ref, watch } from "vue"
import { AppButton, AppField, AppModal, AppSelect } from "@/components/ui"
import { agendaService, type Agendamento } from "@/services/agendaService"
import { salaService, type Sala } from "@/services/salaService"
import { formatHora } from "@/utils/datetime"

const props = defineProps<{
    aberto: boolean
    agendamento: Agendamento | null
    estabId: number
    outrosAgendamentosDoDia?: Agendamento[]
}>()

const emit = defineEmits<{
    "update:aberto": [valor: boolean]
    alocada: []
}>()

const salas = ref<Sala[]>([])
const carregando = ref(false)
const salvando = ref(false)
const erro = ref<string | null>(null)
const salaIdSel = ref<number | null>(null)

watch(
    () => props.aberto,
    async (a) => {
        if (!a) {
            erro.value = null
            return
        }
        salaIdSel.value = props.agendamento?.salaId ?? null
        erro.value = null
        if (salas.value.length === 0) {
            carregando.value = true
            try {
                salas.value = await salaService.listar(props.estabId, true)
            } catch (e: any) {
                erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar salas."
            } finally {
                carregando.value = false
            }
        }
    },
)

const tinhaSala = computed(() => props.agendamento?.salaId != null)

/** Outros agendamentos com a mesma sala selecionada, check-in feito e ainda ativos. */
const ocupacao = computed(() => {
    if (salaIdSel.value == null || !props.agendamento) return null
    const lista = props.outrosAgendamentosDoDia ?? []
    return lista.find(a =>
        a.id !== props.agendamento!.id
        && a.salaId === salaIdSel.value
        && a.checkInEm != null
        && a.status !== "Concluido"
        && a.status !== "Cancelado",
    ) ?? null
})

async function alocar() {
    if (!props.agendamento || salvando.value) return
    salvando.value = true
    erro.value = null
    try {
        await agendaService.alocarSala(props.estabId, props.agendamento.id, salaIdSel.value)
        emit("alocada")
        emit("update:aberto", false)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao alocar sala."
    } finally {
        salvando.value = false
    }
}

async function desalocar() {
    if (!props.agendamento || salvando.value) return
    salvando.value = true
    erro.value = null
    try {
        await agendaService.alocarSala(props.estabId, props.agendamento.id, null)
        emit("alocada")
        emit("update:aberto", false)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao desalocar sala."
    } finally {
        salvando.value = false
    }
}

function fechar() {
    if (salvando.value) return
    emit("update:aberto", false)
}
</script>

<template>
    <AppModal :aberto="aberto" titulo="Alocar sala" largura="sm" @fechar="fechar">
        <p v-if="carregando" class="estado-msg">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando salas...
        </p>

        <template v-else>
            <p v-if="salas.length === 0" class="estado-msg">
                Nenhuma sala ativa cadastrada no estabelecimento.
            </p>

            <AppField v-else label="Sala (opcional)">
                <AppSelect v-model.number="salaIdSel">
                    <option :value="null">— Sem sala —</option>
                    <option v-for="s in salas" :key="s.id" :value="s.id">
                        {{ s.nome }}<template v-if="s.tipoSalaNome"> · {{ s.tipoSalaNome }}</template>
                    </option>
                </AppSelect>
            </AppField>

            <div v-if="ocupacao" class="alerta-ocupacao">
                <i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i>
                <div>
                    <strong>Atenção:</strong>
                    esta sala já está em uso por
                    <strong>{{ ocupacao.pacienteNome }}</strong>
                    (check-in às {{ formatHora(ocupacao.checkInEm!) }}).
                    Você pode alocar mesmo assim.
                </div>
            </div>

            <p v-if="erro" class="msg-erro">{{ erro }}</p>
        </template>

        <template #rodape>
            <AppButton variant="ghost" :disabled="salvando" @click="fechar">Cancelar</AppButton>
            <AppButton
                v-if="tinhaSala"
                variant="danger"
                :disabled="salvando"
                @click="desalocar"
            >
                Desalocar
            </AppButton>
            <AppButton
                :loading="salvando"
                :disabled="salvando || carregando || salas.length === 0"
                @click="alocar"
            >
                Alocar
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.estado-msg {
    text-align: center; padding: 1rem;
    color: hsl(var(--secondary) / 0.7); font-size: 13px;
    display: flex; align-items: center; justify-content: center; gap: 0.5rem;
}

.alerta-ocupacao {
    display: flex; align-items: flex-start; gap: 10px;
    background: hsl(45 96% 47% / 0.12);
    border: 1px solid hsl(45 96% 47% / 0.35);
    border-left: 3px solid hsl(45 96% 47%);
    border-radius: 8px;
    padding: 10px 12px;
    font-size: 13px;
    color: hsl(35 90% 25%);
}
.alerta-ocupacao > i { font-size: 14px; margin-top: 2px; flex-shrink: 0; }
.alerta-ocupacao strong { font-weight: 700; }

.msg-erro {
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-radius: 8px;
    padding: 8px 12px;
    font-size: 13px;
    margin: 0;
}
</style>
