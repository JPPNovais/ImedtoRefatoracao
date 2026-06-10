<script setup lang="ts">
import { ref, watch } from "vue"
import { AppModal, AppButton } from "@/components/ui"
import { auth2faService } from "@/services/auth2faService"

const props = defineProps<{ aberto: boolean }>()
const emit = defineEmits<{
    fechar: []
    desativado: []
}>()

const senha          = ref("")
const codigo         = ref("")
const carregando     = ref(false)
const erro           = ref<string | null>(null)
const mostrarSenha   = ref(false)

async function desativar() {
    if (!senha.value || !codigo.value) return
    carregando.value = true
    erro.value = null
    try {
        await auth2faService.desativar(senha.value, codigo.value)
        emit("desativado")
        emit("fechar")
        resetarEstado()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível desativar. Verifique os dados e tente novamente."
    } finally {
        carregando.value = false
    }
}

function aoFechar() {
    emit("fechar")
    resetarEstado()
}

function resetarEstado() {
    senha.value = ""
    codigo.value = ""
    erro.value = null
    mostrarSenha.value = false
}

watch(() => props.aberto, (aberto) => {
    if (!aberto) resetarEstado()
})
</script>

<template>
    <AppModal
        :aberto="aberto"
        titulo="Desativar verificação em duas etapas"
        @fechar="aoFechar"
    >
        <p class="instrucao">
            Para desativar o 2FA, confirme sua senha e um código do aplicativo autenticador
            (ou um código de recuperação).
        </p>

        <div class="campo">
            <label class="field-label">Senha atual</label>
            <div class="input-wrap">
                <input
                    v-model="senha"
                    :type="mostrarSenha ? 'text' : 'password'"
                    class="form-input"
                    autocomplete="current-password"
                    placeholder="••••••••"
                    @keydown.enter.prevent="desativar"
                />
                <button
                    type="button"
                    class="toggle-pwd"
                    :aria-label="mostrarSenha ? 'Ocultar senha' : 'Mostrar senha'"
                    tabindex="-1"
                    @click="mostrarSenha = !mostrarSenha"
                >
                    <i :class="mostrarSenha ? 'fa-regular fa-eye-slash' : 'fa-regular fa-eye'" aria-hidden="true"></i>
                </button>
            </div>
        </div>

        <div class="campo">
            <label class="field-label">Código TOTP ou de recuperação</label>
            <input
                v-model="codigo"
                type="text"
                inputmode="numeric"
                class="form-input"
                maxlength="8"
                placeholder="000000"
                autocomplete="one-time-code"
                @keydown.enter.prevent="desativar"
            />
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #footer>
            <AppButton variant="secondary" @click="aoFechar">Cancelar</AppButton>
            <AppButton
                variant="danger"
                :loading="carregando"
                :disabled="!senha || !codigo"
                @click="desativar"
            >
                Desativar 2FA
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.instrucao {
    color: var(--text-muted);
    font-size: var(--text-sm);
    margin: 0 0 1rem;
}

.campo {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    margin-bottom: 0.875rem;
}

.field-label {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: var(--text-muted);
}

.input-wrap {
    position: relative;
}

.form-input {
    width: 100%;
    padding: 0.55rem 0.875rem;
    border: 1px solid var(--border-strong);
    border-radius: var(--radius);
    font-family: inherit;
    font-size: var(--text-sm);
    background: var(--bg-card);
    color: var(--text);
    transition: border-color 0.15s;
}

.form-input:focus {
    outline: none;
    border-color: hsl(var(--primary));
}

.toggle-pwd {
    position: absolute;
    right: 0.75rem;
    top: 50%;
    transform: translateY(-50%);
    background: none;
    border: none;
    color: var(--text-muted);
    cursor: pointer;
    font-size: var(--text-sm);
    padding: 0.25rem;
}

.toggle-pwd:hover { color: var(--text); }

.msg-erro {
    color: var(--danger);
    font-size: var(--text-sm);
    margin: 0;
}
</style>
