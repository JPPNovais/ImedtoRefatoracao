<script setup lang="ts">
/**
 * AdminChangePassword.vue — troca de senha obrigatória no primeiro login.
 *
 * Acessível mesmo com must_reset_password = true (policy ImedtoAdminChangePassword).
 * Após troca bem-sucedida, redireciona para dashboard.
 */
import { ref } from "vue"
import { useRouter } from "vue-router"
import { useAdminAuthStore } from "../stores/adminAuthStore"

const router = useRouter()
const store = useAdminAuthStore()

const novaSenha = ref("")
const confirmacao = ref("")
const carregando = ref(false)
const erro = ref<string | null>(null)
const sucesso = ref(false)

const isProd = import.meta.env.PROD

async function handleSubmit() {
    erro.value = null

    if (!novaSenha.value || !confirmacao.value) {
        erro.value = "Preencha todos os campos."
        return
    }

    if (novaSenha.value !== confirmacao.value) {
        erro.value = "As senhas não coincidem."
        return
    }

    carregando.value = true
    try {
        await store.changePassword(novaSenha.value)
        sucesso.value = true
        setTimeout(() => router.push({ name: "AdminDashboard" }), 1500)
    } catch (err: unknown) {
        const data = (err as { response?: { data?: { mensagem?: string } } })?.response?.data
        erro.value = data?.mensagem ?? "Erro ao alterar senha. Verifique os requisitos."
    } finally {
        carregando.value = false
    }
}
</script>

<template>
    <div class="admin-change-pwd-page">
        <div class="admin-change-pwd-card">
            <h1>Redefinição de Senha Obrigatória</h1>
            <p class="admin-change-pwd-info">
                Por segurança, defina uma nova senha antes de continuar.
            </p>

            <div class="admin-policy">
                <p class="admin-policy-title">Requisitos da senha:</p>
                <ul>
                    <li v-if="isProd">Mínimo de 10 caracteres</li>
                    <li v-if="isProd">Pelo menos uma letra maiúscula</li>
                    <li v-if="isProd">Pelo menos uma letra minúscula</li>
                    <li v-if="isProd">Pelo menos um número</li>
                    <li v-if="isProd">Pelo menos um caractere especial (!@#$%...)</li>
                    <li v-if="!isProd">Mínimo de 6 caracteres (modo dev)</li>
                </ul>
            </div>

            <form @submit.prevent="handleSubmit" novalidate>
                <div class="admin-field">
                    <label for="nova-senha">Nova Senha</label>
                    <input
                        id="nova-senha"
                        v-model="novaSenha"
                        type="password"
                        autocomplete="new-password"
                        :disabled="carregando || sucesso"
                        required
                    />
                </div>

                <div class="admin-field">
                    <label for="confirm">Confirmar Senha</label>
                    <input
                        id="confirm"
                        v-model="confirmacao"
                        type="password"
                        autocomplete="new-password"
                        :disabled="carregando || sucesso"
                        required
                    />
                </div>

                <p v-if="erro" class="admin-erro" role="alert">{{ erro }}</p>
                <p v-if="sucesso" class="admin-sucesso" role="status">
                    Senha alterada com sucesso! Redirecionando...
                </p>

                <button type="submit" :disabled="carregando || sucesso">
                    {{ carregando ? "Salvando..." : "Alterar Senha" }}
                </button>
            </form>
        </div>
    </div>
</template>

<style scoped>
.admin-change-pwd-page {
    display: flex;
    align-items: flex-start;
    justify-content: center;
    padding-top: 2rem;
}

.admin-change-pwd-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 12px;
    padding: 2rem;
    width: 100%;
    max-width: 480px;
}

h1 {
    font-size: 1.375rem;
    font-weight: 700;
    color: hsl(var(--foreground));
    margin: 0 0 0.5rem;
}

.admin-change-pwd-info {
    color: hsl(var(--muted-foreground));
    font-size: 0.9rem;
    margin: 0 0 1.5rem;
}

.admin-policy {
    background: rgba(59, 130, 246, 0.08);
    border: 1px solid rgba(59, 130, 246, 0.2);
    border-radius: 8px;
    padding: 1rem;
    margin-bottom: 1.5rem;
}

.admin-policy-title {
    font-size: 0.8125rem;
    font-weight: 600;
    color: hsl(var(--primary) / 0.8);
    margin: 0 0 0.5rem;
}

.admin-policy ul {
    margin: 0;
    padding-left: 1.25rem;
}

.admin-policy li {
    font-size: 0.825rem;
    color: hsl(var(--muted-foreground));
    margin-bottom: 0.2rem;
}

form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
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
}

.admin-field input:focus {
    border-color: hsl(var(--primary));
}

.admin-field input:disabled {
    opacity: 0.6;
}

.admin-erro {
    background: rgba(220, 38, 38, 0.1);
    border: 1px solid rgba(220, 38, 38, 0.3);
    border-radius: 6px;
    padding: 0.625rem;
    font-size: 0.875rem;
    color: hsl(var(--destructive) / 0.7);
    margin: 0;
}

.admin-sucesso {
    background: rgba(34, 197, 94, 0.1);
    border: 1px solid rgba(34, 197, 94, 0.3);
    border-radius: 6px;
    padding: 0.625rem;
    font-size: 0.875rem;
    color: hsl(var(--success) / 0.8);
    margin: 0;
}

button[type="submit"] {
    background: hsl(var(--primary));
    color: hsl(var(--card));
    border: none;
    border-radius: 8px;
    padding: 0.75rem;
    font-size: 0.9375rem;
    font-weight: 600;
    cursor: pointer;
    margin-top: 0.25rem;
}

button[type="submit"]:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}
</style>
