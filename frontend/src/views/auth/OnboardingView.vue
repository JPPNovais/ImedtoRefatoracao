<script setup lang="ts">
import { computed, ref } from "vue"
import { useRouter } from "vue-router"
import { vMaska } from "maska/vue"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { usuarioService } from "@/services/usuarioService"
import { estabelecimentoService } from "@/services/estabelecimentoService"
import { AppButton, AppField, AppInput } from "@/components/ui"
import PreAppShell from "@/components/PreAppShell.vue"

type TipoCadastro = "profissional" | "estabelecimento"

const router    = useRouter()
const auth      = useAuthStore()
const tenant    = useTenantStore()

const tipoCadastro        = ref<TipoCadastro>("profissional")
const nomeCompleto        = ref("")
const cpf                 = ref("")
const telefone            = ref("")
const nomeEstabelecimento = ref("")
const carregando          = ref(false)
const erro                = ref<string | null>(null)

const cpfDigitos = computed(() => cpf.value.replace(/\D/g, ""))

const formularioValido = computed(() => {
    const baseOk = nomeCompleto.value.trim().length >= 3 && cpfDigitos.value.length === 11
    if (tipoCadastro.value === "estabelecimento")
        return baseOk && nomeEstabelecimento.value.trim().length >= 2
    return baseOk
})

async function salvar() {
    erro.value = null
    carregando.value = true
    try {
        await usuarioService.completarOnboarding({
            nomeCompleto: nomeCompleto.value.trim(),
            cpf: cpf.value.trim(),
            telefone: telefone.value.trim() || undefined,
        })

        if (tipoCadastro.value === "estabelecimento") {
            await estabelecimentoService.criar({
                nomeFantasia: nomeEstabelecimento.value.trim(),
            })
        }

        await auth.recarregarMe()

        const lista = await estabelecimentoService.listarMeus()
        if (lista.length === 1) {
            tenant.selecionar({
                id: lista[0].id,
                nomeFantasia: lista[0].nomeFantasia,
                papel: lista[0].papelDoUsuario,
            })
            router.replace({ name: "Home" })
        } else {
            router.replace({ name: "SelecionarEstabelecimento" })
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível concluir o cadastro."
    } finally {
        carregando.value = false
    }
}
</script>

<template>
    <PreAppShell
        titulo="Boas-vindas!"
        subtitulo="Para continuar, complete algumas informações para concluir seu cadastro."
    >
        <div v-if="erro" class="alerta">{{ erro }}</div>

        <form @submit.prevent="salvar" class="form">
            <div class="campo-bloco">
                <p class="campo-label">Tipo de cadastro</p>
                <div class="tipo-grid">
                    <button
                        type="button"
                        :class="['tipo-btn', { ativo: tipoCadastro === 'profissional' }]"
                        @click="tipoCadastro = 'profissional'"
                    >
                        Profissional
                    </button>
                    <button
                        type="button"
                        :class="['tipo-btn', { ativo: tipoCadastro === 'estabelecimento' }]"
                        @click="tipoCadastro = 'estabelecimento'"
                    >
                        Estabelecimento
                    </button>
                </div>
            </div>

            <AppField label="Nome completo" required>
                <AppInput
                    v-model="nomeCompleto"
                    autocomplete="name"
                    placeholder="Informe o seu nome completo"
                />
            </AppField>

            <AppField label="CPF" required>
                <AppInput
                    v-model="cpf"
                    v-maska="'###.###.###-##'"
                    inputmode="numeric"
                    placeholder="000.000.000-00"
                />
            </AppField>

            <AppField label="Telefone">
                <AppInput
                    v-model="telefone"
                    v-maska="'(##) #####-####'"
                    type="tel"
                    autocomplete="tel"
                    placeholder="(00) 00000-0000"
                />
            </AppField>

            <AppField
                v-if="tipoCadastro === 'estabelecimento'"
                label="Nome do estabelecimento"
                required
            >
                <AppInput
                    v-model="nomeEstabelecimento"
                    placeholder="Ex: Clínica Saúde Total"
                    autocomplete="organization"
                />
            </AppField>

            <AppButton
                type="submit"
                block
                :loading="carregando"
                :disabled="!formularioValido || carregando"
            >
                Concluir cadastro
            </AppButton>
        </form>
    </PreAppShell>
</template>

<style scoped>
.alerta {
    background: hsl(var(--error) / 0.1);
    color: hsl(var(--error));
    padding: 0.65rem 0.9rem;
    border-radius: var(--radius);
    font-size: 0.85rem;
}

.form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.campo-bloco {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.campo-label {
    font-size: 0.85rem;
    font-weight: 600;
    color: hsl(var(--secondary));
    margin: 0;
}

.tipo-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.5rem;
}

.tipo-btn {
    padding: 0.55rem 1rem;
    border-radius: var(--radius);
    font-size: 0.875rem;
    font-weight: 500;
    font-family: inherit;
    cursor: pointer;
    transition: background 0.15s, color 0.15s, border-color 0.15s;
    border: 1px solid hsl(var(--border));
    background: hsl(var(--card));
    color: hsl(var(--muted-foreground));
}

.tipo-btn:hover {
    background: hsl(var(--muted));
    color: hsl(var(--foreground));
}

.tipo-btn.ativo {
    background: hsl(var(--primary));
    color: hsl(var(--primary-foreground));
    border-color: hsl(var(--primary));
}
</style>
