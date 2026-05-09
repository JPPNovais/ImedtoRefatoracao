<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import imedtoLogo from "@/assets/imedto-logo.png"

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

type Estado = "carregando" | "sucesso" | "erro"

const estado = ref<Estado>("carregando")
const mensagem = ref<string>("")
const reenviando = ref(false)
const emailReenvio = ref("")
const reenvioOk = ref(false)
const cooldownSegundos = ref(0)
let cooldownInterval: number | null = null

onMounted(async () => {
    const token = (route.query.token as string) || ""
    if (!token) {
        estado.value = "erro"
        mensagem.value = "Link inválido — token ausente."
        return
    }

    try {
        await auth.confirmarEmail(token)
        estado.value = "sucesso"
        mensagem.value = "E-mail confirmado com sucesso! Você já pode fazer login."
    } catch (e: any) {
        estado.value = "erro"
        mensagem.value =
            e?.response?.data?.mensagem ??
            "Não foi possível confirmar. O link pode ter expirado ou já ter sido usado."
    }
})

async function reenviar() {
    if (!emailReenvio.value || cooldownSegundos.value > 0) return
    reenviando.value = true
    try {
        await auth.reenviarConfirmacao(emailReenvio.value)
        reenvioOk.value = true
        iniciarCooldown(300)
    } finally {
        reenviando.value = false
    }
}

function iniciarCooldown(segundos: number) {
    cooldownSegundos.value = segundos
    if (cooldownInterval) window.clearInterval(cooldownInterval)
    cooldownInterval = window.setInterval(() => {
        cooldownSegundos.value -= 1
        if (cooldownSegundos.value <= 0 && cooldownInterval) {
            window.clearInterval(cooldownInterval)
            cooldownInterval = null
        }
    }, 1000)
}

onBeforeUnmount(() => {
    if (cooldownInterval) window.clearInterval(cooldownInterval)
})

function irParaLogin() {
    router.push({ name: "Login" })
}
</script>

<template>
    <div class="confirmar-page">
        <div class="card">
            <img :src="imedtoLogo" alt="Imedto" class="logo" />

            <template v-if="estado === 'carregando'">
                <div class="spinner" />
                <p>Confirmando seu e-mail…</p>
            </template>

            <template v-else-if="estado === 'sucesso'">
                <div class="icone icone-sucesso">✓</div>
                <h1>Pronto!</h1>
                <p>{{ mensagem }}</p>
                <button class="btn btn-primary" @click="irParaLogin">Ir para o login</button>
            </template>

            <template v-else>
                <div class="icone icone-erro">!</div>
                <h1>Não foi possível confirmar</h1>
                <p>{{ mensagem }}</p>

                <div class="reenvio">
                    <p v-if="!reenvioOk" class="dica">Solicite um novo e-mail de confirmação:</p>
                    <p v-else class="ok">
                        Se a conta existir, um novo e-mail de confirmação foi enviado. Verifique sua caixa de entrada e spam.
                    </p>
                    <input
                        v-model="emailReenvio"
                        type="email"
                        placeholder="seu@email.com"
                        :disabled="reenviando || cooldownSegundos > 0"
                    />
                    <button
                        class="btn btn-primary"
                        :disabled="reenviando || !emailReenvio || cooldownSegundos > 0"
                        @click="reenviar"
                    >
                        {{
                            reenviando
                                ? "Enviando…"
                                : cooldownSegundos > 0
                                    ? `Aguarde ${Math.floor(cooldownSegundos / 60)}:${String(cooldownSegundos % 60).padStart(2, "0")} para reenviar`
                                    : reenvioOk
                                        ? "Reenviar novamente"
                                        : "Reenviar e-mail"
                        }}
                    </button>
                </div>

                <button class="btn btn-link" @click="irParaLogin">Voltar para o login</button>
            </template>
        </div>
    </div>
</template>

<style scoped>
.confirmar-page {
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
.logo {
    height: 40px;
    margin-bottom: 1.5rem;
}
h1 {
    font-size: 1.5rem;
    margin: 0.5rem 0 0.75rem;
}
p {
    color: var(--color-text-secondary, #5a6878);
    line-height: 1.5;
    margin: 0.5rem 0 1.5rem;
}
.icone {
    width: 64px;
    height: 64px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 2rem;
    font-weight: bold;
    margin: 0 auto 1rem;
}
.icone-sucesso {
    background: #dcfce7;
    color: #15803d;
}
.icone-erro {
    background: #fee2e2;
    color: #b91c1c;
}
.spinner {
    width: 40px;
    height: 40px;
    border: 3px solid #e5e7eb;
    border-top-color: var(--color-primary, #3b82f6);
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
    margin: 1rem auto;
}
@keyframes spin {
    to { transform: rotate(360deg); }
}
.btn {
    width: 100%;
    padding: 0.75rem 1rem;
    border-radius: 8px;
    font-weight: 600;
    cursor: pointer;
    border: none;
    margin-top: 0.5rem;
    transition: background 0.15s;
}
.btn-primary {
    background: var(--color-primary, #3b82f6);
    color: #fff;
}
.btn-primary:hover:not(:disabled) {
    background: var(--color-primary-hover, #2563eb);
}
.btn-primary:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}
.btn-link {
    background: transparent;
    color: var(--color-primary, #3b82f6);
    text-decoration: underline;
    margin-top: 1rem;
}
.reenvio {
    margin-top: 1.5rem;
}
.reenvio input {
    width: 100%;
    padding: 0.75rem 1rem;
    border: 1px solid var(--color-border, #d1d5db);
    border-radius: 8px;
    margin-bottom: 0.75rem;
    font-size: 1rem;
}
.dica {
    font-size: 0.875rem;
    margin-bottom: 0.5rem;
}
.ok {
    color: #15803d;
    font-weight: 500;
}
</style>
