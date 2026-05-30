<script setup lang="ts">
/**
 * AdminsFormView — formulário de criação de novo administrador.
 *
 * CA37: cria com senha temporária aleatória.
 * CA38: 422 para e-mail duplicado exibido diretamente.
 * Após sucesso: exibe senha temporária uma única vez, depois redireciona para lista.
 */
import { ref, computed } from "vue"
import { useRouter } from "vue-router"
import { AppButton, AppPageHeader, AppField, AppInput } from "@/components/ui"
import { useAdminsStore } from "../stores/adminsStore"

const router = useRouter()
const store = useAdminsStore()

const nome = ref("")
const email = ref("")
const motivo = ref("")
const carregando = ref(false)
const erro = ref<string | null>(null)

// Resultado de criação — exibido uma vez
const senhaTemporaria = ref<string | null>(null)
const emailCriado = ref("")
const nomeCriado = ref("")

const formularioValido = computed(
    () => nome.value.trim().length > 0
        && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value)
        && motivo.value.trim().length >= 10,
)

async function criar() {
    carregando.value = true
    erro.value = null
    try {
        const result = await store.criar(nome.value.trim(), email.value.trim(), motivo.value.trim())
        senhaTemporaria.value = result.senhaTemporaria
        emailCriado.value = result.email
        nomeCriado.value = result.nome
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível criar o administrador."
    } finally {
        carregando.value = false
    }
}

function voltar() {
    void router.push({ name: "AdminAdmins" })
}
</script>

<template>
    <main class="app-page admins-form">
        <AppPageHeader
            titulo="Novo administrador"
            subtitulo="Crie uma conta de acesso à área administrativa."
        />

        <!-- Formulário de criação -->
        <template v-if="!senhaTemporaria">
            <form class="admins-form-campos" @submit.prevent="criar">
                <AppField label="Nome completo" required>
                    <AppInput
                        v-model="nome"
                        placeholder="Nome do administrador"
                        :disabled="carregando"
                    />
                </AppField>

                <AppField label="E-mail" required>
                    <AppInput
                        v-model="email"
                        type="email"
                        placeholder="admin@exemplo.com"
                        :disabled="carregando"
                    />
                </AppField>

                <AppField
                    label="Motivo"
                    required
                    hint="Por que este acesso está sendo criado? (mínimo 10 caracteres)"
                >
                    <AppInput
                        v-model="motivo"
                        placeholder="Descreva o motivo…"
                        :disabled="carregando"
                    />
                </AppField>

                <p v-if="erro" class="admins-form-erro">{{ erro }}</p>

                <div class="admins-form-acoes">
                    <AppButton variant="secondary" type="button" @click="voltar">
                        Cancelar
                    </AppButton>
                    <AppButton
                        type="submit"
                        :loading="carregando"
                        :disabled="!formularioValido"
                    >
                        Criar administrador
                    </AppButton>
                </div>
            </form>
        </template>

        <!-- Exibição da senha temporária (uma única vez) -->
        <template v-else>
            <div class="admins-form-sucesso">
                <div class="admins-form-sucesso-icone">
                    <i class="fa-solid fa-circle-check"></i>
                </div>
                <h2 class="admins-form-sucesso-titulo">Administrador criado com sucesso</h2>
                <p class="admins-form-sucesso-desc">
                    <strong>{{ nomeCriado }}</strong> ({{ emailCriado }}) foi criado.
                    Copie a senha temporária abaixo e envie ao novo administrador.
                    <strong>Ela não será exibida novamente.</strong>
                </p>

                <div class="admins-form-senha-box">
                    <span class="admins-form-senha-label">Senha temporária:</span>
                    <code class="admins-form-senha-valor">{{ senhaTemporaria }}</code>
                </div>

                <AppButton @click="voltar">
                    Voltar para a lista
                </AppButton>
            </div>
        </template>
    </main>
</template>

<style scoped>
.admins-form {
    max-width: 560px;
}

.admins-form-campos {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    margin-top: 1.5rem;
}

.admins-form-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    margin: 0;
}

.admins-form-acoes {
    display: flex;
    gap: 0.75rem;
    justify-content: flex-end;
}

/* Estado de sucesso */
.admins-form-sucesso {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
    margin-top: 1.5rem;
}

.admins-form-sucesso-icone {
    font-size: 2.5rem;
    color: hsl(142 60% 36%);
}

.admins-form-sucesso-titulo {
    font-size: 1.1rem;
    font-weight: 700;
    margin: 0;
}

.admins-form-sucesso-desc {
    color: hsl(var(--muted-foreground));
    font-size: 0.9rem;
    margin: 0;
    line-height: 1.5;
}

.admins-form-senha-box {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.875rem 1rem;
    background: hsl(var(--muted));
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    width: 100%;
}

.admins-form-senha-label {
    font-size: 0.8rem;
    color: hsl(var(--muted-foreground));
    white-space: nowrap;
}

.admins-form-senha-valor {
    font-family: monospace;
    font-size: 1.05rem;
    color: hsl(var(--foreground));
    user-select: all;
    letter-spacing: 0.05em;
    flex: 1;
}
</style>
