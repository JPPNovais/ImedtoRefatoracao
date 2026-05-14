<script setup lang="ts">
/**
 * AlterarSenhaModal — troca de senha autenticada.
 *
 * Fluxo:
 *  1. Usuário informa senha atual + nova + confirmação.
 *  2. Backend valida senha atual (reautenticação) e revoga TODAS as sessões.
 *  3. UX: após sucesso, mostra mensagem + emite evento; caller decide se desloga.
 *
 * A trava de "senha atual = nova" e tamanho mínimo é espelhada do backend
 * (defense-in-depth + UX rápida), mas o backend continua sendo a fonte da verdade.
 */
import { computed, ref, watch } from "vue"
import { AppButton, AppModal } from "@/components/ui"
import { useAuthStore } from "@/stores/authStore"

const SENHA_MIN = 8

const props = defineProps<{
    aberto: boolean
}>()

const emit = defineEmits<{
    (e: "fechar"): void
    (e: "alterada"): void
}>()

const auth = useAuthStore()

const senhaAtual = ref("")
const novaSenha = ref("")
const confirmacao = ref("")
const mostrarSenhas = ref(false)
const executando = ref(false)
const erro = ref<string | null>(null)

watch(() => props.aberto, (aberto) => {
    if (!aberto) {
        senhaAtual.value = ""
        novaSenha.value = ""
        confirmacao.value = ""
        erro.value = null
        executando.value = false
        mostrarSenhas.value = false
    }
})

const validacaoLocal = computed<string | null>(() => {
    if (!senhaAtual.value) return null
    if (!novaSenha.value) return null
    if (novaSenha.value.length < SENHA_MIN) return `A nova senha precisa ter no mínimo ${SENHA_MIN} caracteres.`
    if (novaSenha.value === senhaAtual.value) return "A nova senha precisa ser diferente da atual."
    if (confirmacao.value && novaSenha.value !== confirmacao.value) return "A confirmação não confere."
    return null
})

const podeSalvar = computed(() => {
    return !executando.value
        && senhaAtual.value.length > 0
        && novaSenha.value.length >= SENHA_MIN
        && novaSenha.value === confirmacao.value
        && novaSenha.value !== senhaAtual.value
})

async function salvar() {
    if (!podeSalvar.value) return
    executando.value = true
    erro.value = null
    try {
        await auth.alterarSenha(senhaAtual.value, novaSenha.value)
        emit("alterada")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível alterar a senha."
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
    <AppModal :aberto="aberto" largura="sm" @fechar="fechar">
        <template #titulo>
            <div class="titulo">
                <h2>Trocar senha</h2>
                <span>Você continuará logado nesta sessão. Os demais dispositivos serão desconectados.</span>
            </div>
        </template>

        <label class="campo">
            <span class="lbl">Senha atual</span>
            <input
                v-model="senhaAtual"
                :type="mostrarSenhas ? 'text' : 'password'"
                class="input"
                autocomplete="current-password"
                :disabled="executando"
                placeholder="Sua senha atual"
            />
        </label>

        <label class="campo">
            <span class="lbl">Nova senha</span>
            <input
                v-model="novaSenha"
                :type="mostrarSenhas ? 'text' : 'password'"
                class="input"
                autocomplete="new-password"
                :disabled="executando"
                :placeholder="`Mínimo ${SENHA_MIN} caracteres`"
            />
        </label>

        <label class="campo">
            <span class="lbl">Confirmar nova senha</span>
            <input
                v-model="confirmacao"
                :type="mostrarSenhas ? 'text' : 'password'"
                class="input"
                autocomplete="new-password"
                :disabled="executando"
                placeholder="Repita a nova senha"
            />
        </label>

        <label class="toggle">
            <input v-model="mostrarSenhas" type="checkbox" :disabled="executando" />
            <span>Mostrar senhas</span>
        </label>

        <p v-if="validacaoLocal" class="dica">{{ validacaoLocal }}</p>
        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template #rodape>
            <AppButton variant="ghost" :disabled="executando" @click="fechar">Cancelar</AppButton>
            <AppButton
                icon="fa-solid fa-key"
                :loading="executando"
                :disabled="!podeSalvar"
                @click="salvar"
            >
                Trocar senha
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.titulo h2 {
    font-size: 18px;
    font-weight: 700;
    color: hsl(var(--primary-dark));
    margin: 0 0 2px;
}
.titulo span {
    font-size: 13px;
    color: hsl(var(--secondary) / 0.65);
    line-height: 1.4;
}

.campo {
    display: flex;
    flex-direction: column;
    gap: 6px;
}
.lbl {
    font-size: 11px;
    font-weight: 700;
    color: hsl(var(--foreground) / 0.55);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.input {
    padding: 9px 12px;
    border: 1px solid hsl(var(--foreground) / 0.15);
    border-radius: 8px;
    font-family: inherit;
    font-size: 14px;
    background: hsl(var(--card));
    color: hsl(var(--foreground));
}
.input:focus {
    outline: none;
    border-color: hsl(var(--primary));
    box-shadow: 0 0 0 3px hsl(var(--primary) / 0.12);
}
.input:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}

.toggle {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    font-size: 12px;
    color: hsl(var(--foreground) / 0.7);
    cursor: pointer;
    user-select: none;
}
.toggle input { margin: 0; }

.dica {
    color: hsl(var(--warning, 30 90% 30%));
    background: hsl(var(--warning, 30 90% 50%) / 0.08);
    border: 1px solid hsl(var(--warning, 30 90% 50%) / 0.2);
    border-radius: 8px;
    padding: 8px 12px;
    font-size: 12px;
    margin: 0;
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
</style>
