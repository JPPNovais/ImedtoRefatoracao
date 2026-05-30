<script setup lang="ts">
/**
 * Formulário de criação/edição de modelo de prontuário global (Wave 4 live-link).
 * Wave 5: textarea de JSON substituído por ModeloProntuarioBuilder visual.
 */
import { ref, onMounted, computed } from "vue"
import { useRouter } from "vue-router"
import { AppPageHeader, AppCard, AppField, AppInput, AppButton, ModeloProntuarioBuilder } from "@/components/ui"
import { useModelosGlobaisStore } from "../stores/modelosGlobaisStore"

const props = defineProps<{ id?: string }>()
const router = useRouter()
const store = useModelosGlobaisStore()

const idNumerico = computed(() => props.id ? Number(props.id) : null)
const editando = computed(() => idNumerico.value !== null && !isNaN(idNumerico.value))

const nome = ref("")
const descricao = ref("")
const estruturaJson = ref("")
const motivo = ref("")
const builderValido = ref(false)
const erros = ref<Record<string, string>>({})
const salvando = ref(false)
const erroGeral = ref("")

onMounted(async () => {
    if (editando.value && idNumerico.value !== null) {
        await store.carregarItem(idNumerico.value)
        if (store.itemAtual) {
            nome.value = store.itemAtual.nome
            descricao.value = store.itemAtual.descricao ?? ""
            estruturaJson.value = store.itemAtual.estruturaJson
        }
    }
})

function validar(): boolean {
    const e: Record<string, string> = {}
    if (!nome.value.trim()) e.nome = "Nome é obrigatório."
    if (!builderValido.value) e.estrutura = "Selecione ao menos uma seção."
    if (motivo.value.trim().length < 10) e.motivo = "Motivo deve ter ao menos 10 caracteres."
    erros.value = e
    return Object.keys(e).length === 0
}

async function salvar() {
    if (!validar()) return
    salvando.value = true
    erroGeral.value = ""
    try {
        const payload = {
            nome: nome.value.trim(),
            descricao: descricao.value.trim() || null,
            estruturaJson: estruturaJson.value,
            motivo: motivo.value.trim(),
        }
        if (editando.value && idNumerico.value !== null) {
            await store.atualizar(idNumerico.value, payload)
        } else {
            await store.criar(payload)
        }
        router.push({ name: "AdminModelosGlobais" })
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroGeral.value = msg ?? "Não foi possível salvar o modelo."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            :titulo="editando ? 'Editar modelo de prontuário' : 'Novo modelo de prontuário'"
        />

        <div v-if="store.carregando && editando" class="estado-info">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>
        <p v-else-if="store.erro && editando" class="estado-erro">{{ store.erro }}</p>

        <AppCard v-else>
            <form @submit.prevent="salvar" class="form-campos">
                <ModeloProntuarioBuilder
                    v-model:nome="nome"
                    v-model:descricao="descricao"
                    v-model:estrutura-json="estruturaJson"
                    :disabled="salvando"
                    @update:valido="builderValido = $event"
                />

                <p v-if="erros.estrutura" class="campo-erro-inline">{{ erros.estrutura }}</p>

                <AppField label="Motivo da alteração" required :hint="erros.motivo || 'Mínimo 10 caracteres.'">
                    <AppInput
                        v-model="motivo"
                        placeholder="Descreva o motivo..."
                        :disabled="salvando"
                    />
                </AppField>

                <p v-if="erroGeral" class="campo-erro" role="alert">{{ erroGeral }}</p>

                <div class="form-acoes">
                    <AppButton variant="ghost" type="button" @click="router.push({ name: 'AdminModelosGlobais' })">Cancelar</AppButton>
                    <AppButton
                        type="submit"
                        :loading="salvando"
                        :disabled="salvando || motivo.trim().length < 10"
                    >
                        {{ editando ? "Salvar alterações" : "Criar modelo" }}
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
    color: var(--text-muted);
    font-size: 0.875rem;
}

.estado-erro {
    padding: 0.75rem 1rem;
    background: color-mix(in srgb, var(--danger) 10%, transparent);
    color: var(--danger);
    border: 1px solid color-mix(in srgb, var(--danger) 30%, transparent);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
}

.form-campos {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.campo-erro-inline {
    font-size: 0.8em;
    color: var(--danger);
    margin: -0.5rem 0 0;
}

.campo-erro {
    padding: 0.75rem 1rem;
    background: color-mix(in srgb, var(--danger) 10%, transparent);
    color: var(--danger);
    border: 1px solid color-mix(in srgb, var(--danger) 30%, transparent);
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
