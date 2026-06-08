<script setup lang="ts">
import { computed, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import imedtoLogo from "@/assets/imedto-logo.png"

/**
 * Tela de redefinição de senha. Aberta via link do e-mail enviado por
 * `EnviarRecuperacaoSenhaAsync` (LocalJwtAuthService) — formato:
 *   https://app.imedto.com/auth/redefinir-senha?token=<cru>
 *
 * Backend (POST /api/auth/redefinir-senha) consome o token, exige senha
 * com 8+ caracteres e revoga sessões existentes do usuário.
 */
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

type Estado = "form" | "enviando" | "sucesso" | "erro"

const token = String(route.query.token ?? "")
const novaSenha = ref("")
const confirmar = ref("")
const estado = ref<Estado>(token ? "form" : "erro")
const mensagem = ref<string>(token ? "" : "Link inválido — token ausente.")

const senhaCurta = computed(() => novaSenha.value.length > 0 && novaSenha.value.length < 8)
const naoConfere = computed(() => confirmar.value.length > 0 && novaSenha.value !== confirmar.value)
const podeEnviar = computed(() =>
    novaSenha.value.length >= 8 && novaSenha.value === confirmar.value && estado.value === "form",
)

async function enviar() {
    if (!podeEnviar.value) return
    estado.value = "enviando"
    try {
        await auth.redefinirSenha(token, novaSenha.value)
        estado.value = "sucesso"
        mensagem.value = "Senha redefinida com sucesso. Você já pode entrar com a nova senha."
    } catch (e: any) {
        estado.value = "erro"
        mensagem.value =
            e?.response?.data?.mensagem
                ?? "Não foi possível redefinir. O link pode ter expirado ou já ter sido usado."
    }
}

function irParaLogin() {
    router.push({ name: "Login" })
}
</script>

<template>
    <div class="redefinir-page">
        <div class="card">
            <img :src="imedtoLogo" alt="Imedto" class="logo" />

            <template v-if="estado === 'form' || estado === 'enviando'">
                <h1>Redefinir senha</h1>
                <p>Escolha uma nova senha para sua conta. Mínimo 8 caracteres.</p>

                <form @submit.prevent="enviar">
                    <label class="campo">
                        <span>Nova senha</span>
                        <input
                            v-model="novaSenha"
                            type="password"
                            autocomplete="new-password"
                            :disabled="estado === 'enviando'"
                            required
                        />
                        <small v-if="senhaCurta" class="erro-inline">A senha precisa ter pelo menos 8 caracteres.</small>
                    </label>

                    <label class="campo">
                        <span>Confirmar nova senha</span>
                        <input
                            v-model="confirmar"
                            type="password"
                            autocomplete="new-password"
                            :disabled="estado === 'enviando'"
                            required
                        />
                        <small v-if="naoConfere" class="erro-inline">As senhas não conferem.</small>
                    </label>

                    <button type="submit" class="btn btn-primary" :disabled="!podeEnviar">
                        {{ estado === "enviando" ? "Salvando…" : "Redefinir senha" }}
                    </button>
                </form>

                <button type="button" class="btn btn-link" @click="irParaLogin">Voltar para o login</button>
            </template>

            <template v-else-if="estado === 'sucesso'">
                <div class="icone icone-sucesso">✓</div>
                <h1>Pronto!</h1>
                <p>{{ mensagem }}</p>
                <button class="btn btn-primary" @click="irParaLogin">Ir para o login</button>
            </template>

            <template v-else>
                <div class="icone icone-erro">!</div>
                <h1>Não foi possível redefinir</h1>
                <p>{{ mensagem }}</p>
                <button class="btn btn-primary" @click="irParaLogin">Voltar para o login</button>
            </template>
        </div>
    </div>
</template>

<style scoped>
.redefinir-page {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 2rem;
    background: var(--color-background, #f7f9fc);
}
.card {
    max-width: 440px;
    width: 100%;
    background: var(--color-surface, #fff);
    border-radius: 12px;
    box-shadow: 0 4px 24px rgba(0, 0, 0, 0.08);
    padding: 2.5rem;
    text-align: center;
}
.logo { height: 40px; margin-bottom: 1.5rem; }
h1 { font-size: var(--text-2xl); margin: 0.5rem 0 0.75rem; }
p { color: var(--color-text-secondary, #5a6878); line-height: 1.5; margin: 0.5rem 0 1.5rem; }

form { text-align: left; margin-top: 1rem; }
.campo { display: block; margin-bottom: 1rem; }
.campo > span {
    display: block; font-size: 0.875rem; font-weight: 600;
    color: var(--color-text-primary, #1f2937); margin-bottom: 0.4rem;
}
.campo input {
    width: 100%; padding: 0.75rem 1rem; box-sizing: border-box;
    border: 1px solid var(--color-border, #d1d5db);
    border-radius: 8px; font-size: 1rem;
}
.campo input:focus { outline: none; border-color: var(--color-primary, #3b82f6); }
.erro-inline { display: block; color: #b91c1c; font-size: 0.8125rem; margin-top: 0.25rem; }

.icone {
    width: 64px; height: 64px; border-radius: 50%;
    display: flex; align-items: center; justify-content: center;
    font-size: 2rem; font-weight: bold; margin: 0 auto 1rem;
}
.icone-sucesso { background: #dcfce7; color: #15803d; }
.icone-erro { background: #fee2e2; color: #b91c1c; }

.btn {
    width: 100%; padding: 0.75rem 1rem; border-radius: 8px;
    font-weight: 600; cursor: pointer; border: none;
    margin-top: 0.5rem; transition: background 0.15s;
}
.btn-primary { background: var(--color-primary, #3b82f6); color: #fff; }
.btn-primary:hover:not(:disabled) { background: var(--color-primary-hover, #2563eb); }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
.btn-link { background: transparent; color: var(--color-primary, #3b82f6); text-decoration: underline; margin-top: 1rem; }
</style>
