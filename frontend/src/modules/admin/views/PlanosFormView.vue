<script setup lang="ts">
/**
 * PlanosFormView — criar/editar plano.
 *
 * W3-CA7 a W3-CA11: app-page--narrow + AppPageHeader + AppCard
 *   + AppField + AppInput + AppTextarea + AppCheckbox + AppButton.
 */
import { ref, computed, onMounted } from "vue"
import { useRouter, useRoute } from "vue-router"
import {
    AppPageHeader, AppCard, AppField, AppInput, AppTextarea, AppCheckbox, AppButton,
} from "@/components/ui"
import { usePlanosStore } from "../stores/planosStore"

const router = useRouter()
const route = useRoute()
const store = usePlanosStore()

const isEdicao = computed(() => !!route.params.id)
const idEdicao = computed(() => route.params.id as string | undefined)

const nome = ref("")
const descricaoCurta = ref("")
const precoTexto = ref("")
const gratuito = ref(false)
const limitesJson = ref("{}")
const motivo = ref("")
const erroLimitesJson = ref("")
const erroGeral = ref("")
const salvando = ref(false)

onMounted(async () => {
    if (isEdicao.value && idEdicao.value) {
        await store.carregarPlano(idEdicao.value)
        const p = store.planoAtual
        if (p) {
            nome.value = p.nome
            descricaoCurta.value = p.descricaoCurta ?? ""
            precoTexto.value = p.precoMensalCentavos != null
                ? (p.precoMensalCentavos / 100).toFixed(2)
                : ""
            gratuito.value = p.gratuito
            limitesJson.value = p.limitesJson ?? "{}"
        }
    }
})

function validarJson(): boolean {
    try {
        JSON.parse(limitesJson.value)
        erroLimitesJson.value = ""
        return true
    } catch {
        erroLimitesJson.value = "JSON inválido. Corrija o formato antes de salvar."
        return false
    }
}

function calcularCentavos(): number | null {
    if (!precoTexto.value.trim()) return null
    const valor = parseFloat(precoTexto.value.replace(",", "."))
    if (isNaN(valor) || valor < 0) return null
    return Math.round(valor * 100)
}

async function salvar() {
    if (!validarJson()) return
    if (motivo.value.trim().length < 10) {
        erroGeral.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }

    salvando.value = true
    erroGeral.value = ""

    const payload = {
        nome: nome.value.trim(),
        descricaoCurta: descricaoCurta.value.trim() || null,
        precoMensalCentavos: calcularCentavos(),
        gratuito: gratuito.value,
        limitesJson: limitesJson.value,
        motivo: motivo.value.trim(),
    }

    try {
        if (isEdicao.value && idEdicao.value) {
            await store.atualizar(idEdicao.value, payload)
        } else {
            await store.criar(payload)
        }
        router.push({ name: "AdminPlanosList" })
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroGeral.value = msg ?? "Não foi possível salvar o plano."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            :titulo="isEdicao ? 'Editar plano' : 'Novo plano'"
            subtitulo="Configure os dados do plano."
        />

        <div v-if="store.carregando" class="estado-info">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>

        <AppCard v-else>
            <form @submit.prevent="salvar" class="form-campos">
                <AppField label="Nome do plano" required>
                    <AppInput
                        v-model="nome"
                        placeholder="Ex: Plano Profissional"
                        :disabled="salvando"
                        maxlength="100"
                    />
                </AppField>

                <AppField label="Descrição curta">
                    <AppInput
                        v-model="descricaoCurta"
                        placeholder="Descrição opcional"
                        :disabled="salvando"
                        maxlength="200"
                    />
                </AppField>

                <AppField label="Preço mensal (R$)" hint="Deixe vazio para 'sob consulta'.">
                    <AppInput
                        v-model="precoTexto"
                        placeholder="Ex: 99.90"
                        :disabled="salvando"
                    />
                </AppField>

                <AppCheckbox v-model="gratuito" label="Plano gratuito (sem cobrança)" :disabled="salvando" />

                <AppField label="Limites (JSON)" :hint="erroLimitesJson || 'JSON com limites e configurações do plano.'">
                    <AppTextarea
                        v-model="limitesJson"
                        :rows="5"
                        placeholder='{"profissionais": 5, "pacientes": 100}'
                        :disabled="salvando"
                        @blur="validarJson"
                    />
                </AppField>

                <AppField label="Motivo da alteração" required hint="Mínimo 10 caracteres.">
                    <AppTextarea
                        v-model="motivo"
                        :rows="2"
                        placeholder="Informe o motivo..."
                        :disabled="salvando"
                    />
                </AppField>

                <p v-if="erroGeral" class="campo-erro" role="alert">{{ erroGeral }}</p>

                <div class="form-acoes">
                    <AppButton variant="ghost" type="button" @click="router.back()">Cancelar</AppButton>
                    <AppButton
                        type="submit"
                        :loading="salvando"
                        :disabled="motivo.trim().length < 10"
                    >
                        {{ isEdicao ? "Salvar alterações" : "Criar plano" }}
                    </AppButton>
                </div>
            </form>
        </AppCard>
    </main>
</template>

<style scoped>
.estado-info {
    text-align: center;
    padding: 2rem 0;
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
}

.form-campos {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.campo-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    margin: 0;
}

.form-acoes {
    display: flex;
    gap: 0.75rem;
    justify-content: flex-end;
    padding-top: 0.5rem;
}
</style>
