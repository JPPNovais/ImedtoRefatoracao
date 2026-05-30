<script setup lang="ts">
/**
 * AdminChangePassword.vue — troca de senha obrigatória no primeiro login.
 *
 * Fora do shell (sem sidebar/topbar) — padrão auth, como LoginView do app.
 * W3-CA11: usa AppCard + AppField + AppInput + AppButton do DS.
 * Acessível mesmo com must_reset_password = true (policy ImedtoAdminChangePassword).
 * Após troca bem-sucedida, redireciona para dashboard.
 */
import { ref } from "vue"
import { useRouter } from "vue-router"
import { AppCard, AppField, AppInput, AppButton } from "@/components/ui"
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
    <div class="change-pwd-page">
        <AppCard style="width:100%;max-width:480px;">
            <h1 class="change-pwd-titulo">Redefinição de Senha Obrigatória</h1>
            <p class="change-pwd-info">
                Por segurança, defina uma nova senha antes de continuar.
            </p>

            <div class="change-pwd-policy">
                <p class="policy-titulo">Requisitos da senha:</p>
                <ul class="policy-lista">
                    <li v-if="isProd">Mínimo de 10 caracteres</li>
                    <li v-if="isProd">Pelo menos uma letra maiúscula</li>
                    <li v-if="isProd">Pelo menos uma letra minúscula</li>
                    <li v-if="isProd">Pelo menos um número</li>
                    <li v-if="isProd">Pelo menos um caractere especial (!@#$%...)</li>
                    <li v-if="!isProd">Mínimo de 6 caracteres (modo dev)</li>
                </ul>
            </div>

            <form @submit.prevent="handleSubmit" novalidate class="change-pwd-form">
                <AppField label="Nova Senha" required>
                    <AppInput
                        v-model="novaSenha"
                        type="password"
                        autocomplete="new-password"
                        :disabled="carregando || sucesso"
                    />
                </AppField>

                <AppField label="Confirmar Senha" required>
                    <AppInput
                        v-model="confirmacao"
                        type="password"
                        autocomplete="new-password"
                        :disabled="carregando || sucesso"
                    />
                </AppField>

                <p v-if="erro" class="change-pwd-erro" role="alert">{{ erro }}</p>
                <p v-if="sucesso" class="change-pwd-sucesso" role="status">
                    Senha alterada com sucesso! Redirecionando...
                </p>

                <AppButton type="submit" :loading="carregando" :disabled="sucesso" block>
                    Alterar Senha
                </AppButton>
            </form>
        </AppCard>
    </div>
</template>

<style scoped>
.change-pwd-page {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    background: hsl(var(--background));
    padding: 1.5rem;
}

.change-pwd-titulo {
    font-size: 1.375rem;
    font-weight: 700;
    color: hsl(var(--foreground));
    margin: 0 0 0.5rem;
}

.change-pwd-info {
    color: hsl(var(--muted-foreground));
    font-size: 0.9rem;
    margin: 0 0 1.25rem;
}

.change-pwd-policy {
    background: hsl(var(--primary) / 0.06);
    border: 1px solid hsl(var(--primary) / 0.18);
    border-radius: calc(var(--radius) - 2px);
    padding: 0.875rem 1rem;
    margin-bottom: 1.25rem;
}

.policy-titulo {
    font-size: 0.8125rem;
    font-weight: 600;
    color: hsl(var(--primary));
    margin: 0 0 0.5rem;
}

.policy-lista {
    margin: 0;
    padding-left: 1.25rem;
}

.policy-lista li {
    font-size: 0.825rem;
    color: hsl(var(--muted-foreground));
    margin-bottom: 0.2rem;
}

.change-pwd-form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.change-pwd-erro {
    padding: 0.625rem 0.875rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    margin: 0;
}

.change-pwd-sucesso {
    padding: 0.625rem 0.875rem;
    background: hsl(var(--success) / 0.1);
    color: hsl(142 60% 30%);
    border: 1px solid hsl(var(--success) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    margin: 0;
}
</style>
