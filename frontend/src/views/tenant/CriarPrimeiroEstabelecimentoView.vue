<script setup lang="ts">
import { computed, ref } from "vue"
import { useRouter } from "vue-router"
import { estabelecimentoService } from "@/services/estabelecimentoService"
import { useTenantStore } from "@/stores/tenantStore"
import { AppButton, AppInput } from "@/components/ui"
import PreAppShell from "@/components/PreAppShell.vue"

const router = useRouter()
const tenant = useTenantStore()

const nomeFantasia = ref("")
const cnpj = ref("")
const carregando = ref(false)
const erro = ref<string | null>(null)

const formularioValido = computed(() => nomeFantasia.value.trim().length >= 2)

async function criar() {
    erro.value = null
    carregando.value = true
    try {
        await estabelecimentoService.criar({
            nomeFantasia: nomeFantasia.value.trim(),
            cnpj: cnpj.value.trim() || undefined,
        })

        // O backend não retorna o id criado — listamos para obter o vínculo recém-feito.
        const lista = await estabelecimentoService.listarMeus()
        const novo = lista[lista.length - 1] ?? lista[0]
        if (novo) {
            tenant.selecionar({
                id: novo.id,
                nomeFantasia: novo.nomeFantasia,
                papel: novo.papelDoUsuario,
            })
            router.replace({ name: "Home" })
        } else {
            router.replace({ name: "SelecionarEstabelecimento" })
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível criar o estabelecimento."
    } finally {
        carregando.value = false
    }
}
</script>

<template>
    <PreAppShell
        titulo="Crie seu primeiro estabelecimento"
        subtitulo="Para começar a usar o Imedto, cadastre o consultório, clínica ou estabelecimento que você administra."
    >
        <div v-if="erro" class="alerta">{{ erro }}</div>

        <form @submit.prevent="criar" class="form">
            <div class="campo">
                <label for="nome">
                    Nome do estabelecimento
                    <span class="obrig">*</span>
                </label>
                <AppInput
                    id="nome"
                    v-model="nomeFantasia"
                    placeholder="Ex: Clínica Saúde Total"
                    autocomplete="organization"
                    required
                />
                <small>Nome fantasia ou razão social — você pode editar depois.</small>
            </div>

            <div class="campo">
                <label for="cnpj">
                    CNPJ
                    <span class="opcional">(opcional)</span>
                </label>
                <AppInput
                    id="cnpj"
                    v-model="cnpj"
                    placeholder="00.000.000/0000-00"
                    inputmode="numeric"
                    maxlength="18"
                />
                <small>Pode ser preenchido depois nas configurações.</small>
            </div>

            <AppButton
                type="submit"
                block
                :loading="carregando"
                :disabled="!formularioValido"
            >
                {{ carregando ? "Criando…" : "Criar estabelecimento" }}
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

.campo {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
}
.campo label {
    font-size: 0.85rem;
    font-weight: 600;
    color: hsl(var(--secondary));
}
.campo small {
    font-size: 0.75rem;
    color: hsl(var(--muted-foreground));
}
.obrig    { color: hsl(var(--error)); margin-left: 2px; }
.opcional { color: hsl(var(--muted-foreground)); font-weight: 400; margin-left: 4px; }
</style>
