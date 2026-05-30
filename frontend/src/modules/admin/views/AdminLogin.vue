<script setup lang="ts">
/**
 * AdminLogin.vue — tela de login full-screen da área administrativa.
 *
 * Fora do shell (sem sidebar/topbar) — padrão auth, como LoginView do app.
 * W3-CA2: usa AppCard + AppField + AppInput + AppButton do DS.
 * Isolamento: usa apenas adminAuthStore. Sem imports de outros módulos do app.
 */
import { ref } from "vue"
import { useRouter } from "vue-router"
import { AppCard, AppField, AppInput, AppButton, AppBadge } from "@/components/ui"
import { useAdminAuthStore } from "../stores/adminAuthStore"

const router = useRouter()
const store = useAdminAuthStore()

const email = ref("")
const senha = ref("")
const carregando = ref(false)
const erro = ref<string | null>(null)

async function handleLogin() {
    erro.value = null
    if (!email.value || !senha.value) {
        erro.value = "Preencha e-mail e senha."
        return
    }

    carregando.value = true
    try {
        const admin = await store.login(email.value, senha.value)

        if (admin.forcePasswordReset) {
            await router.push({ name: "AdminChangePassword" })
        } else {
            await router.push({ name: "AdminDashboard" })
        }
    } catch {
        // Mensagem genérica — CA2.
        erro.value = "Credenciais inválidas."
    } finally {
        carregando.value = false
    }
}
</script>

<template>
    <div class="login-page">
        <AppCard style="width:100%;max-width:400px;">
            <div class="login-brand">
                <span class="login-logo">Imedto</span>
                <AppBadge variant="error">Admin</AppBadge>
            </div>

            <h1 class="login-titulo">Área Administrativa</h1>
            <p class="login-subtitulo">Acesso restrito. Toda ação é auditada.</p>

            <form @submit.prevent="handleLogin" novalidate class="login-form">
                <AppField label="E-mail" required>
                    <AppInput
                        v-model="email"
                        type="email"
                        autocomplete="email"
                        placeholder="admin@imedto.com"
                        :disabled="carregando"
                    />
                </AppField>

                <AppField label="Senha" required>
                    <AppInput
                        v-model="senha"
                        type="password"
                        autocomplete="current-password"
                        placeholder="••••••••"
                        :disabled="carregando"
                    />
                </AppField>

                <p v-if="erro" class="login-erro" role="alert">{{ erro }}</p>

                <AppButton type="submit" :loading="carregando" block>
                    Entrar
                </AppButton>
            </form>
        </AppCard>
    </div>
</template>

<style scoped>
.login-page {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    background: hsl(var(--background));
    padding: 1.5rem;
}

.login-brand {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 1.5rem;
}

.login-logo {
    font-size: 1.25rem;
    font-weight: 700;
    color: hsl(var(--foreground));
    letter-spacing: -0.02em;
}

.login-titulo {
    font-size: 1.5rem;
    font-weight: 700;
    color: hsl(var(--foreground));
    margin: 0 0 0.25rem;
}

.login-subtitulo {
    font-size: 0.875rem;
    color: hsl(var(--muted-foreground));
    margin: 0 0 1.5rem;
}

.login-form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.login-erro {
    padding: 0.625rem 0.875rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    margin: 0;
}
</style>
