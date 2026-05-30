<script setup lang="ts">
/**
 * ModalResetTenant — confirmação dupla para reset de dados do tenant (CA32–CA36).
 *
 * Etapas:
 * 1. Admin digita exatamente o nome fantasia do estabelecimento.
 * 2. Admin preenche motivo (mín. 10 chars).
 * 3. Admin marca checkbox "Entendo que é irreversível".
 */
import { ref, computed } from "vue"
import { AppModal, AppField, AppInput, AppTextarea, AppCheckbox, AppButton } from "@/components/ui"

const props = defineProps<{
    open: boolean
    nomeFantasia: string
    carregando: boolean
    erro: string | null
}>()

const emit = defineEmits<{
    close: []
    confirmar: [motivo: string, confirmarNomeFantasia: string]
}>()

const nomeDigitado = ref("")
const motivo = ref("")
const ciente = ref(false)

const nomeConfere = computed(
    () => nomeDigitado.value.trim().toLowerCase() === props.nomeFantasia.trim().toLowerCase(),
)
const motivoValido = computed(() => motivo.value.trim().length >= 10)
const podeSalvar = computed(() => nomeConfere.value && motivoValido.value && ciente.value && !props.carregando)

function fechar() {
    nomeDigitado.value = ""
    motivo.value = ""
    ciente.value = false
    emit("close")
}

function confirmar() {
    if (!podeSalvar.value) return
    emit("confirmar", motivo.value.trim(), nomeDigitado.value.trim())
}
</script>

<template>
    <AppModal :aberto="open" titulo="Resetar dados do estabelecimento" @fechar="fechar">
        <template #titulo>
            <div class="reset-titulo-wrap">
                <div class="reset-danger-strip">AÇÃO IRREVERSÍVEL</div>
                <span class="reset-titulo">Resetar dados do estabelecimento</span>
            </div>
        </template>

        <p class="reset-desc">
            Esta ação remove <strong>permanentemente</strong> todos os dados operacionais
            (pacientes, prontuários, agendamentos, financeiro, etc.) do estabelecimento
            <strong>{{ nomeFantasia }}</strong>. A casca (conta, vínculos, plano) é preservada.
        </p>

        <div class="reset-campos">
            <AppField
                label="Digite o nome fantasia exato para confirmar"
                required
                :hint="nomeDigitado && !nomeConfere ? 'Nome não confere.' : undefined"
            >
                <AppInput
                    v-model="nomeDigitado"
                    :placeholder="nomeFantasia"
                    :disabled="carregando"
                    autocomplete="off"
                />
            </AppField>

            <AppField label="Motivo do reset" required hint="Mínimo 10 caracteres.">
                <AppTextarea
                    v-model="motivo"
                    :rows="3"
                    placeholder="Ex: Cliente solicitou reset completo antes do golive (mín. 10 caracteres)"
                    :disabled="carregando"
                />
            </AppField>

            <AppCheckbox
                v-model="ciente"
                :disabled="carregando"
            >
                Entendo que esta ação é <strong>irreversível</strong> e fui autorizado a executá-la.
            </AppCheckbox>

            <p v-if="erro" class="reset-erro-global">{{ erro }}</p>
        </div>

        <template #rodape>
            <AppButton variant="secondary" :disabled="carregando" @click="fechar">Cancelar</AppButton>
            <AppButton
                variant="danger"
                :loading="carregando"
                :disabled="!podeSalvar"
                @click="confirmar"
            >
                Confirmar reset
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.reset-titulo-wrap {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.reset-danger-strip {
    background: hsl(var(--destructive));
    color: hsl(var(--primary-foreground));
    padding: 0.25rem 0.75rem;
    font-size: 0.6875rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    border-radius: calc(var(--radius) - 2px);
    align-self: flex-start;
}

.reset-titulo {
    font-size: 1.125rem;
    font-weight: 700;
    color: hsl(var(--foreground));
}

.reset-desc {
    color: hsl(var(--foreground));
    font-size: 0.8125rem;
    line-height: 1.5;
    margin-bottom: 1rem;
}

.reset-campos {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.reset-erro-global {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin: 0;
}
</style>
