<script setup lang="ts">
/**
 * AdminLogin.vue — tela de login da área administrativa.
 *
 * Isolamento: usa apenas adminAuthStore e adminApi. Sem imports de outros módulos do app.
 * Design: tema escuro/sério, claramente distinto do app principal.
 */
import { ref } from "vue"
import { useRouter } from "vue-router"
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
    <div class="admin-login-page">
        <div class="admin-login-card">
            <div class="admin-login-brand">
                <span class="admin-login-logo">Imedto</span>
                <span class="admin-login-badge">Admin</span>
            </div>

            <h1 class="admin-login-title">Área Administrativa</h1>
            <p class="admin-login-subtitle">Acesso restrito. Toda ação é auditada.</p>

            <form class="admin-login-form" @submit.prevent="handleLogin" novalidate>
                <div class="admin-field">
                    <label for="email">E-mail</label>
                    <input
                        id="email"
                        v-model="email"
                        type="email"
                        autocomplete="email"
                        placeholder="admin@imedto.com"
                        :disabled="carregando"
                        required
                    />
                </div>

                <div class="admin-field">
                    <label for="senha">Senha</label>
                    <input
                        id="senha"
                        v-model="senha"
                        type="password"
                        autocomplete="current-password"
                        placeholder="••••••••"
                        :disabled="carregando"
                        required
                    />
                </div>

                <p v-if="erro" class="admin-login-erro" role="alert">{{ erro }}</p>

                <button type="submit" class="admin-login-btn" :disabled="carregando">
                    {{ carregando ? "Entrando..." : "Entrar" }}
                </button>
            </form>
        </div>
    </div>
</template>

<style scoped>
.admin-login-page {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    background: hsl(var(--background));
    padding: 1.5rem;
}

.admin-login-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 12px;
    padding: 2.5rem;
    width: 100%;
    max-width: 400px;
}

.admin-login-brand {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 1.5rem;
}

.admin-login-logo {
    font-size: 1.25rem;
    font-weight: 700;
    color: hsl(var(--foreground));
    letter-spacing: -0.02em;
}

.admin-login-badge {
    background: hsl(var(--destructive));
    color: hsl(var(--card));
    font-size: 0.65rem;
    font-weight: 600;
    padding: 0.125rem 0.5rem;
    border-radius: 4px;
    letter-spacing: 0.05em;
    text-transform: uppercase;
}

.admin-login-title {
    font-size: 1.5rem;
    font-weight: 700;
    color: hsl(var(--foreground));
    margin: 0 0 0.25rem;
}

.admin-login-subtitle {
    font-size: 0.875rem;
    color: hsl(var(--muted-foreground));
    margin: 0 0 2rem;
}

.admin-login-form {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.admin-field {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
}

.admin-field label {
    font-size: 0.875rem;
    font-weight: 500;
    color: hsl(var(--foreground));
}

.admin-field input {
    background: hsl(var(--background));
    border: 1px solid hsl(var(--border));
    border-radius: 8px;
    padding: 0.625rem 0.875rem;
    font-size: 0.9375rem;
    color: hsl(var(--foreground));
    outline: none;
    transition: border-color 0.15s;
}

.admin-field input:focus {
    border-color: hsl(var(--primary));
}

.admin-field input:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}

.admin-login-erro {
    background: rgba(220, 38, 38, 0.1);
    border: 1px solid rgba(220, 38, 38, 0.3);
    border-radius: 6px;
    padding: 0.625rem 0.875rem;
    font-size: 0.875rem;
    color: hsl(var(--destructive) / 0.7);
    margin: 0;
}

.admin-login-btn {
    background: hsl(var(--primary));
    color: hsl(var(--card));
    border: none;
    border-radius: 8px;
    padding: 0.75rem 1rem;
    font-size: 0.9375rem;
    font-weight: 600;
    cursor: pointer;
    transition: background 0.15s;
    margin-top: 0.25rem;
}

.admin-login-btn:hover:not(:disabled) {
    background: hsl(var(--primary));
}

.admin-login-btn:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}
</style>
