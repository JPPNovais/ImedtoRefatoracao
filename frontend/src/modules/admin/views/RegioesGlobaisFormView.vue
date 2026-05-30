<script setup lang="ts">
/**
 * Formulário de criação/edição de região anatômica global.
 * Sinônimos via tag input — cada Enter adiciona uma tag.
 */
import { ref, onMounted, computed } from "vue"
import { useRouter } from "vue-router"
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
    if (!val || sinonimos.value.includes(val)) {
        tagInput.value = ""
        return
    }
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

function cancelar() {
    router.push({ name: "AdminRegioesGlobais" })
}
</script>

<template>
    <div class="form-page">
        <div class="form-header">
            <button class="btn-voltar" @click="cancelar">
                <i class="fa-solid fa-arrow-left"></i> Voltar
            </button>
            <h1 class="page-titulo">
                {{ editando ? "Editar região anatômica" : "Nova região anatômica" }}
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
                    placeholder="Ex.: Abdômen"
                    maxlength="200"
                />
                <span v-if="erros.nome" class="erro-msg">{{ erros.nome }}</span>
            </div>

            <div class="campo-grupo">
                <label class="campo-label" for="sistemaCorporal">Sistema corporal</label>
                <input
                    id="sistemaCorporal"
                    v-model="sistemaCorporal"
                    type="text"
                    class="campo-input"
                    placeholder="Ex.: Digestivo, Cardiovascular..."
                    maxlength="100"
                />
            </div>

            <div class="campo-grupo">
                <label class="campo-label">Sinônimos</label>
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
                <span class="campo-dica">Pressione Enter para adicionar cada sinônimo.</span>
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
                    {{ salvando ? "Salvando..." : (editando ? "Salvar alterações" : "Criar região") }}
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
.campo-input { padding: 8px 10px; border: 1px solid hsl(var(--border)); border-radius: 6px; font-size: 13px; background: hsl(var(--background)); color: hsl(var(--foreground)); }
.campo-input.campo-erro { border-color: hsl(var(--destructive)); }
.erro-msg { font-size: 12px; color: hsl(var(--destructive)); }
.erro-geral { padding: 10px 14px; background: hsl(var(--destructive) / 0.1); border: 1px solid hsl(var(--destructive) / 0.3); border-radius: 6px; color: hsl(var(--destructive)); font-size: 13px; }
.form-acoes { display: flex; justify-content: flex-end; gap: 10px; padding-top: 8px; }
.btn-primario { padding: 8px 20px; background: hsl(var(--primary)); color: hsl(var(--primary-foreground)); border: none; border-radius: 6px; font-size: 13px; font-weight: 600; cursor: pointer; }
.btn-primario:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-secundario { padding: 8px 16px; background: hsl(var(--muted)); color: hsl(var(--foreground)); border: none; border-radius: 6px; font-size: 13px; cursor: pointer; }
.estado-centro { text-align: center; padding: 48px; color: hsl(var(--muted-foreground)); }
.estado-erro { text-align: center; padding: 48px; color: hsl(var(--destructive)); }
.tags-container { display: flex; flex-wrap: wrap; gap: 6px; align-items: center; padding: 8px; border: 1px solid hsl(var(--border)); border-radius: 6px; background: hsl(var(--background)); min-height: 42px; }
.tag { display: inline-flex; align-items: center; gap: 4px; background: hsl(var(--muted)); color: hsl(var(--foreground)); padding: 3px 8px; border-radius: 9999px; font-size: 12px; }
.tag-remover { background: none; border: none; cursor: pointer; color: hsl(var(--muted-foreground)); font-size: 14px; line-height: 1; padding: 0; display: flex; align-items: center; }
.tag-remover:hover { color: hsl(var(--destructive)); }
.tag-input { border: none; outline: none; background: transparent; font-size: 13px; color: hsl(var(--foreground)); flex: 1; min-width: 120px; }
</style>
