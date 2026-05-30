<script setup lang="ts">
/**
 * Formulário de criação/edição de variável pool global.
 * Para tipo "lista", exibe campo de valores (JSON array de strings).
 */
import { ref, onMounted, computed, watch } from "vue"
import { useRouter } from "vue-router"
import { useVariaveisGlobaisStore } from "../stores/variaveisGlobaisStore"

const props = defineProps<{ id?: string }>()
const router = useRouter()
const store = useVariaveisGlobaisStore()

const editando = computed(() => !!props.id)

const TIPOS = ["texto", "numerico", "data", "lista", "booleano"]

const nome = ref("")
const tipo = ref("texto")
const descricao = ref("")
const valoresRaw = ref("") // JSON array de strings para tipo lista
const motivo = ref("")
const erros = ref<Record<string, string>>({})
const salvando = ref(false)
const erroGeral = ref("")

// Tag input helpers para tipo lista
const tagInput = ref("")

function valoresArray(): string[] {
    try {
        const parsed = JSON.parse(valoresRaw.value || "[]")
        return Array.isArray(parsed) ? parsed : []
    } catch {
        return []
    }
}

function adicionarTag() {
    const val = tagInput.value.trim()
    if (!val) return
    const arr = valoresArray()
    if (!arr.includes(val)) {
        arr.push(val)
        valoresRaw.value = JSON.stringify(arr)
    }
    tagInput.value = ""
}

function removerTag(idx: number) {
    const arr = valoresArray()
    arr.splice(idx, 1)
    valoresRaw.value = arr.length ? JSON.stringify(arr) : ""
}

// Limpa valores ao mudar o tipo
watch(tipo, () => {
    valoresRaw.value = ""
    tagInput.value = ""
})

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

function cancelar() {
    router.push({ name: "AdminVariaveisGlobais" })
}

function labelTipo(t: string): string {
    const map: Record<string, string> = {
        texto: "Texto livre", numerico: "Numérico", data: "Data", lista: "Lista de opções", booleano: "Sim/Não",
    }
    return map[t] ?? t
}
</script>

<template>
    <div class="form-page">
        <div class="form-header">
            <button class="btn-voltar" @click="cancelar">
                <i class="fa-solid fa-arrow-left"></i> Voltar
            </button>
            <h1 class="page-titulo">
                {{ editando ? "Editar variável pool" : "Nova variável pool" }}
            </h1>
        </div>

        <div v-if="store.carregando && editando" class="estado-centro">Carregando...</div>
        <div v-else-if="store.erro && editando" class="estado-erro">{{ store.erro }}</div>

        <form v-else class="form-card" @submit.prevent="salvar">
            <div class="campo-grupo">
                <label class="campo-label" for="nome">Nome <span class="obrigatorio">*</span></label>
                <input
                    id="nome"
                    v-model="nome"
                    type="text"
                    class="campo-input"
                    :class="{ 'campo-erro': erros.nome }"
                    placeholder="Ex.: Alergias alimentares"
                    maxlength="200"
                />
                <span v-if="erros.nome" class="erro-msg">{{ erros.nome }}</span>
            </div>

            <div class="campo-grupo">
                <label class="campo-label" for="tipo">Tipo <span class="obrigatorio">*</span></label>
                <select
                    id="tipo"
                    v-model="tipo"
                    class="campo-select"
                    :class="{ 'campo-erro': erros.tipo }"
                    :disabled="editando"
                >
                    <option v-for="t in TIPOS" :key="t" :value="t">{{ labelTipo(t) }}</option>
                </select>
                <span v-if="editando" class="campo-dica">O tipo não pode ser alterado após a criação.</span>
                <span v-if="erros.tipo" class="erro-msg">{{ erros.tipo }}</span>
            </div>

            <!-- Valores para tipo lista -->
            <div v-if="tipo === 'lista'" class="campo-grupo">
                <label class="campo-label">Opções da lista <span class="obrigatorio">*</span></label>
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
                <span class="campo-dica">Pressione Enter para adicionar cada opção.</span>
                <span v-if="erros.valores" class="erro-msg">{{ erros.valores }}</span>
            </div>

            <div class="campo-grupo">
                <label class="campo-label" for="descricao">Descrição</label>
                <input
                    id="descricao"
                    v-model="descricao"
                    type="text"
                    class="campo-input"
                    placeholder="Descrição opcional"
                    maxlength="500"
                />
            </div>

            <div class="campo-grupo">
                <label class="campo-label" for="motivo">Motivo da alteração <span class="obrigatorio">*</span></label>
                <input
                    id="motivo"
                    v-model="motivo"
                    type="text"
                    class="campo-input"
                    :class="{ 'campo-erro': erros.motivo }"
                    placeholder="Descreva o motivo (mín. 10 caracteres)"
                />
                <span v-if="erros.motivo" class="erro-msg">{{ erros.motivo }}</span>
            </div>

            <div v-if="erroGeral" class="erro-geral" role="alert">{{ erroGeral }}</div>

            <div class="form-acoes">
                <button type="button" class="btn-secundario" @click="cancelar">Cancelar</button>
                <button type="submit" class="btn-primario" :disabled="salvando">
                    {{ salvando ? "Salvando..." : (editando ? "Salvar alterações" : "Criar variável") }}
                </button>
            </div>
        </form>
    </div>
</template>

<style scoped>
.form-page { padding: 24px 32px; max-width: 720px; }
.form-header { display: flex; align-items: center; gap: 16px; margin-bottom: 24px; }
.btn-voltar { background: none; border: none; color: hsl(var(--muted-foreground)); cursor: pointer; font-size: 13px; display: flex; align-items: center; gap: 6px; padding: 0; }
.btn-voltar:hover { color: hsl(var(--foreground)); }
.page-titulo { font-size: 20px; font-weight: 700; margin: 0; color: hsl(var(--foreground)); }
.form-card { background: hsl(var(--card)); border: 1px solid hsl(var(--border)); border-radius: 10px; padding: 28px; display: flex; flex-direction: column; gap: 20px; }
.campo-grupo { display: flex; flex-direction: column; gap: 6px; }
.campo-label { font-size: 13px; font-weight: 600; color: hsl(var(--foreground)); }
.obrigatorio { color: hsl(var(--destructive)); }
.campo-dica { font-size: 12px; color: hsl(var(--muted-foreground)); }
.campo-input, .campo-select {
    padding: 8px 10px; border: 1px solid hsl(var(--border)); border-radius: 6px;
    font-size: 13px; background: hsl(var(--background)); color: hsl(var(--foreground));
}
.campo-select:disabled { opacity: 0.6; cursor: not-allowed; }
.campo-input.campo-erro, .campo-select.campo-erro { border-color: hsl(var(--destructive)); }
.erro-msg { font-size: 12px; color: hsl(var(--destructive)); }
.erro-geral { padding: 10px 14px; background: hsl(var(--destructive) / 0.1); border: 1px solid hsl(var(--destructive) / 0.3); border-radius: 6px; color: hsl(var(--destructive)); font-size: 13px; }
.form-acoes { display: flex; justify-content: flex-end; gap: 10px; padding-top: 8px; }
.btn-primario { padding: 8px 20px; background: hsl(var(--primary)); color: hsl(var(--primary-foreground)); border: none; border-radius: 6px; font-size: 13px; font-weight: 600; cursor: pointer; }
.btn-primario:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-secundario { padding: 8px 16px; background: hsl(var(--muted)); color: hsl(var(--foreground)); border: none; border-radius: 6px; font-size: 13px; cursor: pointer; }
.estado-centro { text-align: center; padding: 48px; color: hsl(var(--muted-foreground)); }
.estado-erro { text-align: center; padding: 48px; color: hsl(var(--destructive)); }

/* Tags */
.tags-container {
    display: flex; flex-wrap: wrap; gap: 6px; align-items: center;
    padding: 8px; border: 1px solid hsl(var(--border)); border-radius: 6px;
    background: hsl(var(--background)); min-height: 42px;
}
.tag {
    display: inline-flex; align-items: center; gap: 4px;
    background: hsl(var(--primary) / 0.15); color: hsl(var(--primary));
    padding: 3px 8px; border-radius: 9999px; font-size: 12px; font-weight: 600;
}
.tag-remover {
    background: none; border: none; cursor: pointer; color: hsl(var(--primary));
    font-size: 14px; line-height: 1; padding: 0; display: flex; align-items: center;
}
.tag-remover:hover { color: hsl(var(--destructive)); }
.tag-input {
    border: none; outline: none; background: transparent;
    font-size: 13px; color: hsl(var(--foreground)); flex: 1; min-width: 120px;
}
</style>
