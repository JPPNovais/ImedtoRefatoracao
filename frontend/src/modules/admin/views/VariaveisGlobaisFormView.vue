<script setup lang="ts">
/**
 * Formulário de criação/edição de variável pool global.
 * Para tipo "lista", exibe campo de valores (tag input).
 * W3-CA7 a W3-CA11: app-page--narrow + AppPageHeader + AppCard + AppField
 *   + AppInput + AppSelect + AppButton.
 */
import { ref, onMounted, computed, watch } from "vue"
import { useRouter } from "vue-router"
import { AppPageHeader, AppCard, AppField, AppInput, AppSelect, AppButton } from "@/components/ui"
import { useVariaveisGlobaisStore } from "../stores/variaveisGlobaisStore"

const props = defineProps<{ id?: string }>()
const router = useRouter()
const store = useVariaveisGlobaisStore()

const editando = computed(() => !!props.id)

const TIPOS_OPCOES = [
    { value: "texto",    label: "Texto livre" },
    { value: "numerico", label: "Numérico" },
    { value: "data",     label: "Data" },
    { value: "lista",    label: "Lista de opções" },
    { value: "booleano", label: "Sim/Não" },
]

const nome = ref("")
const tipo = ref("texto")
const descricao = ref("")
const valoresRaw = ref("")
const motivo = ref("")
const erros = ref<Record<string, string>>({})
const salvando = ref(false)
const erroGeral = ref("")

const tagInput = ref("")

function valoresArray(): string[] {
    try {
        const parsed = JSON.parse(valoresRaw.value || "[]")
        return Array.isArray(parsed) ? parsed : []
    } catch { return [] }
}

function adicionarTag() {
    const val = tagInput.value.trim()
    if (!val) return
    const arr = valoresArray()
    if (!arr.includes(val)) { arr.push(val); valoresRaw.value = JSON.stringify(arr) }
    tagInput.value = ""
}

function removerTag(idx: number) {
    const arr = valoresArray()
    arr.splice(idx, 1)
    valoresRaw.value = arr.length ? JSON.stringify(arr) : ""
}

watch(tipo, () => { valoresRaw.value = ""; tagInput.value = "" })

onMounted(async () => {
    if (props.id) {
        await store.carregarItem(props.id)
        if (store.itemAtual) {
            nome.value = store.itemAtual.nome
            tipo.value = store.itemAtual.tipo
            descricao.value = store.itemAtual.descricao ?? ""
            valoresRaw.value = store.itemAtual.valoresJson ?? ""
        }
    }
})

function validar(): boolean {
    const e: Record<string, string> = {}
    if (!nome.value.trim()) e.nome = "Nome é obrigatório."
    if (!tipo.value) e.tipo = "Tipo é obrigatório."
    if (tipo.value === "lista" && valoresArray().length === 0) {
        e.valores = "Adicione ao menos um valor para tipo Lista."
    }
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
            descricao: descricao.value.trim() || null,
            valoresJson: tipo.value === "lista" && valoresRaw.value ? valoresRaw.value : null,
            motivo: motivo.value.trim(),
        }
        if (editando.value && props.id) {
            await store.atualizar(props.id, payload)
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
                        placeholder="Ex.: Alergias alimentares"
                        maxlength="200"
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Tipo" required :hint="editando ? 'O tipo não pode ser alterado após a criação.' : erros.tipo">
                    <AppSelect
                        v-model="tipo"
                        :options="TIPOS_OPCOES"
                        :disabled="editando || salvando"
                    />
                </AppField>

                <!-- Valores para tipo lista -->
                <AppField v-if="tipo === 'lista'" label="Opções da lista" required :hint="erros.valores || 'Pressione Enter para adicionar cada opção.'">
                    <div class="tags-container">
                        <span v-for="(val, idx) in valoresArray()" :key="idx" class="tag">
                            {{ val }}
                            <button type="button" class="tag-remover" @click="removerTag(idx)" title="Remover">×</button>
                        </span>
                        <input
                            v-model="tagInput"
                            type="text"
                            class="tag-input"
                            placeholder="Digite e pressione Enter"
                            @keydown.enter.prevent="adicionarTag"
                        />
                    </div>
                </AppField>

                <AppField label="Descrição">
                    <AppInput
                        v-model="descricao"
                        placeholder="Descrição opcional"
                        maxlength="500"
                        :disabled="salvando"
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
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary));
    padding: 0.1875rem 0.5rem;
    border-radius: 9999px;
    font-size: 0.75rem;
    font-weight: 600;
}

.tag-remover {
    background: none;
    border: none;
    cursor: pointer;
    color: hsl(var(--primary));
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
