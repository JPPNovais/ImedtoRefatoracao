<script setup lang="ts">
/**
 * AdminAlterarSenhaModal — troca voluntária da própria senha pelo admin logado.
 *
 * Espelha AlterarSenhaModal.vue (app de estabelecimento) mas usa adminAuthStore
 * e o endpoint /api/admin/auth/change-password.
 * Não importa useAuthStore do app principal — isolamento total do módulo admin.
 *
 * Política de validação local espelha AdminSenhaPolicy do backend:
 *  - Dev: ≥ 6 chars.
 *  - Prod: ≥ 10 chars com maiúscula/minúscula/número/especial.
 * (CA3 — front é UX; backend é fonte da verdade → 422 genérico se falhar.)
 */
import { computed, ref, watch } from "vue"
import { AppButton, AppModal } from "@/components/ui"
import { useAdminAuthStore } from "../stores/adminAuthStore"

const isProd = import.meta.env.PROD
// Mínimo local — espelha AdminSenhaPolicy (dev=6, prod=10).
const SENHA_MIN = isProd ? 10 : 6

const props = defineProps<{
    aberto: boolean
}>()

const emit = defineEmits<{
    (e: "fechar"): void
    (e: "alterada"): void
}>()

const store = useAdminAuthStore()

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
    if (novaSenha.value.length < SENHA_MIN)
        return `A nova senha precisa ter no mínimo ${SENHA_MIN} caracteres.`
    if (novaSenha.value === senhaAtual.value)
        return "A nova senha precisa ser diferente da atual."
    if (confirmacao.value && novaSenha.value !== confirmacao.value)
        return "A confirmação não confere."
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
        await store.changePassword(novaSenha.value, senhaAtual.value)
        emit("alterada")
    } catch (e: unknown) {
        const mensagem = (e as { response?: { data?: { mensagem?: string } } })
            ?.response?.data?.mensagem
        erro.value = mensagem ?? "Não foi possível alterar a senha."
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
    font-size: var(--text-lg);
    font-weight: var(--font-weight-bold);
    color: hsl(var(--primary-dark));
    margin: 0 0 2px;
}
.titulo span {
    font-size: var(--text-sm);
    color: hsl(var(--secondary) / 0.65);
    line-height: 1.4;
}

.campo {
    display: flex;
    flex-direction: column;
    gap: 6px;
}
.lbl {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--foreground) / 0.55);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.input {
    padding: 9px 12px;
    border: 1px solid hsl(var(--foreground) / 0.15);
    border-radius: 8px;
    font-family: inherit;
    font-size: var(--text-sm);
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
    font-size: var(--text-xs);
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
    font-size: var(--text-xs);
    margin: 0;
}
.msg-erro {
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-radius: 8px;
    padding: 8px 12px;
    font-size: var(--text-sm);
    margin: 0;
}
</style>
