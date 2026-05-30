<script setup lang="ts">
/**
 * Formulário de criação/edição de variável pool global (Wave 4 live-link).
 * Campos: nome + categoria (TipoVariavelPool) + motivo.
 */
import { ref, onMounted, computed } from "vue"
import { useRouter } from "vue-router"
import { AppPageHeader, AppCard, AppField, AppInput, AppSelect, AppButton } from "@/components/ui"
import { useVariaveisGlobaisStore } from "../stores/variaveisGlobaisStore"
import { TIPOS_VARIAVEL_POOL } from "../services/catalogosService"

const props = defineProps<{ id?: string }>()
const router = useRouter()
const store = useVariaveisGlobaisStore()

const idNumerico = computed(() => props.id ? Number(props.id) : null)
const editando = computed(() => idNumerico.value !== null && !isNaN(idNumerico.value))

const TIPOS_OPCOES = Object.entries(TIPOS_VARIAVEL_POOL).map(([value, label]) => ({ value, label }))

const nome = ref("")
const tipo = ref(TIPOS_OPCOES[0].value)
const motivo = ref("")
const erros = ref<Record<string, string>>({})
const salvando = ref(false)
const erroGeral = ref("")

onMounted(async () => {
    if (editando.value && idNumerico.value !== null) {
        await store.carregarItem(idNumerico.value)
        if (store.itemAtual) {
            nome.value = store.itemAtual.nome
            tipo.value = store.itemAtual.tipo
        }
    }
})

function validar(): boolean {
    const e: Record<string, string> = {}
    if (!nome.value.trim()) e.nome = "Nome é obrigatório."
    if (!tipo.value) e.tipo = "Categoria é obrigatória."
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
            tipo: tipo.value,
            motivo: motivo.value.trim(),
        }
        if (editando.value && idNumerico.value !== null) {
            await store.atualizar(idNumerico.value, payload)
        } else {
            await store.criar(payload)
        }
        router.push({ name: "AdminVariaveisGlobais" })
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroGeral.value = msg ?? "Não foi possível salvar a variável."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            :titulo="editando ? 'Editar variável pool' : 'Nova variável pool'"
        />

        <div v-if="store.carregando && editando" class="estado-info">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>
        <p v-else-if="store.erro && editando" class="estado-erro">{{ store.erro }}</p>

        <AppCard v-else>
            <form @submit.prevent="salvar" class="form-campos">
                <AppField label="Nome" required :hint="erros.nome">
                    <AppInput
                        v-model="nome"
                        placeholder="Ex.: Dipirona Sódica"
                        maxlength="200"
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Categoria" required :hint="editando ? 'A categoria não pode ser alterada após a criação.' : erros.tipo">
                    <AppSelect
                        v-model="tipo"
                        :options="TIPOS_OPCOES"
                        :disabled="editando || salvando"
                    />
                </AppField>

                <AppField label="Motivo da alteração" required :hint="erros.motivo || 'Mínimo 10 caracteres.'">
                    <AppInput
                        v-model="motivo"
                        placeholder="Descreva o motivo..."
                        :disabled="salvando"
                    />
                </AppField>

                <p v-if="erroGeral" class="campo-erro" role="alert">{{ erroGeral }}</p>

                <div class="form-acoes">
                    <AppButton variant="ghost" type="button" @click="router.push({ name: 'AdminVariaveisGlobais' })">Cancelar</AppButton>
                    <AppButton
                        type="submit"
                        :loading="salvando"
                        :disabled="motivo.trim().length < 10"
                    >
                        {{ editando ? "Salvar alterações" : "Criar variável" }}
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

.estado-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
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
