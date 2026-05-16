<script setup lang="ts">
/**
 * Diálogo de confirmação padrão do design system.
 * Substitui o anti-padrão `window.confirm()` em fluxos destrutivos
 * (inativar/excluir/desvincular).
 *
 * Uso típico (controle externo via v-model):
 *   <AppConfirmDialog
 *       v-model:aberto="confirm.aberto"
 *       titulo="Inativar procedimento?"
 *       :mensagem="`Deseja inativar “${confirm.alvo?.descricao}”?`"
 *       confirmar-rotulo="Inativar"
 *       variante="danger"
 *       @confirmar="executarInativacao"
 *   />
 */
import { AppButton, AppModal } from "."

const props = defineProps<{
    aberto:           boolean
    titulo:           string
    mensagem?:        string
    confirmarRotulo?: string
    cancelarRotulo?:  string
    variante?:        "danger" | "primary"
    icone?:           string
    executando?:      boolean
}>()

const emit = defineEmits<{
    (e: "update:aberto", v: boolean): void
    (e: "confirmar"): void
    (e: "cancelar"): void
}>()

function fechar() {
    if (props.executando) return
    emit("update:aberto", false)
    emit("cancelar")
}

function confirmar() {
    emit("confirmar")
}
</script>

<template>
    <AppModal :aberto="aberto" largura="sm" :titulo="titulo" @fechar="fechar">
        <p v-if="mensagem" class="msg">{{ mensagem }}</p>
        <slot />

        <template #rodape>
            <AppButton variant="secondary" :disabled="executando" @click="fechar">
                {{ cancelarRotulo ?? "Cancelar" }}
            </AppButton>
            <AppButton
                :variant="variante === 'primary' ? 'primary' : 'danger'"
                :icon="icone"
                :loading="executando"
                @click="confirmar"
            >
                {{ confirmarRotulo ?? "Confirmar" }}
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.msg {
    margin: 0;
    font-size: 14px;
    line-height: 1.5;
    color: hsl(var(--foreground) / 0.85);
}
</style>
