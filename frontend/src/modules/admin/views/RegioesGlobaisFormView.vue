<script setup lang="ts">
/**
 * Formulário de criação/edição de região anatômica global.
 * Sinônimos via tag input — cada Enter adiciona uma tag.
 * W3-CA7 a W3-CA11: app-page--narrow + AppPageHeader + AppCard + AppField + AppInput + AppButton.
 */
import { ref, onMounted, computed } from "vue"
import { useRouter } from "vue-router"
import { AppPageHeader, AppCard, AppField, AppInput, AppButton } from "@/components/ui"
import { useRegioesGlobaisStore } from "../stores/regioesGlobaisStore"

const props = defineProps<{ id?: string }>()
const router = useRouter()
const store = useRegioesGlobaisStore()

const editando = computed(() => !!props.id)

const nome = ref("")
const sistemaCorporal = ref("")
const motivo = ref("")
const sinonimos = ref<string[]>([])
const tagInput = ref("")
const erros = ref<Record<string, string>>({})
const salvando = ref(false)
const erroGeral = ref("")

onMounted(async () => {
    if (props.id) {
        await store.carregarItem(props.id)
        if (store.itemAtual) {
            nome.value = store.itemAtual.nome
            sistemaCorporal.value = store.itemAtual.sistemaCorporal ?? ""
            sinonimos.value = store.itemAtual.sinonimos ? [...store.itemAtual.sinonimos] : []
        }
    }
})

function adicionarSinonimo() {
    const val = tagInput.value.trim()
    if (!val || sinonimos.value.includes(val)) { tagInput.value = ""; return }
    sinonimos.value.push(val)
    tagInput.value = ""
}

function removerSinonimo(idx: number) {
    sinonimos.value.splice(idx, 1)
}

function validar(): boolean {
    const e: Record<string, string> = {}
    if (!nome.value.trim()) e.nome = "Nome é obrigatório."
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
            sistemaCorporal: sistemaCorporal.value.trim() || null,
            sinonimos: sinonimos.value.length > 0 ? sinonimos.value : null,
            motivo: motivo.value.trim(),
        }
        if (editando.value && props.id) {
            await store.atualizar(props.id, payload)
        } else {
            await store.criar(payload)
        }
        router.push({ name: "AdminRegioesGlobais" })
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroGeral.value = msg ?? "Não foi possível salvar a região."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            :titulo="editando ? 'Editar região anatômica' : 'Nova região anatômica'"
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
                        placeholder="Ex.: Abdômen"
                        maxlength="200"
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Sistema corporal">
                    <AppInput
                        v-model="sistemaCorporal"
                        placeholder="Ex.: Digestivo, Cardiovascular..."
                        maxlength="100"
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Sinônimos" hint="Pressione Enter para adicionar cada sinônimo.">
                    <div class="tags-container">
                        <span v-for="(s, idx) in sinonimos" :key="idx" class="tag">
                            {{ s }}
                            <button type="button" class="tag-remover" @click="removerSinonimo(idx)" title="Remover">×</button>
                        </span>
                        <input
                            v-model="tagInput"
                            type="text"
                            class="tag-input"
                            placeholder="Digite e pressione Enter"
                            @keydown.enter.prevent="adicionarSinonimo"
                        />
                    </div>
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
                    <AppButton variant="ghost" type="button" @click="router.push({ name: 'AdminRegioesGlobais' })">Cancelar</AppButton>
                    <AppButton
                        type="submit"
                        :loading="salvando"
                        :disabled="motivo.trim().length < 10"
                    >
                        {{ editando ? "Salvar alterações" : "Criar região" }}
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

.tags-container {
    display: flex;
    flex-wrap: wrap;
    gap: 0.375rem;
    align-items: center;
    padding: 0.5rem;
    border: 1px solid hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
    background: hsl(var(--background));
    min-height: 42px;
}

.tag {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    background: hsl(var(--muted));
    color: hsl(var(--foreground));
    padding: 0.1875rem 0.5rem;
    border-radius: 9999px;
    font-size: 0.75rem;
}

.tag-remover {
    background: none;
    border: none;
    cursor: pointer;
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
    line-height: 1;
    padding: 0;
    display: flex;
    align-items: center;
}
.tag-remover:hover { color: hsl(var(--destructive)); }

.tag-input {
    border: none;
    outline: none;
    background: transparent;
    font-size: 0.8125rem;
    color: hsl(var(--foreground));
    flex: 1;
    min-width: 120px;
    font-family: inherit;
}
</style>
