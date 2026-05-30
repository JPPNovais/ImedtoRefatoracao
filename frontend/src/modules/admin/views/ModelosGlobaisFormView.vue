<script setup lang="ts">
/**
 * Formulário de criação/edição de modelo de prontuário global.
 * conteudoJson deve ser JSON válido — validado antes de salvar.
 */
import { ref, onMounted, computed } from "vue"
import { useRouter } from "vue-router"
import { useModelosGlobaisStore } from "../stores/modelosGlobaisStore"

const props = defineProps<{ id?: string }>()
const router = useRouter()
const store = useModelosGlobaisStore()

const editando = computed(() => !!props.id)

const nome = ref("")
const descricao = ref("")
const conteudoJson = ref("{\n  \n}")
const motivo = ref("")
const erros = ref<Record<string, string>>({})
const salvando = ref(false)
const erroGeral = ref("")

onMounted(async () => {
    if (props.id) {
        await store.carregarItem(props.id)
        if (store.itemAtual) {
            nome.value = store.itemAtual.nome
            descricao.value = store.itemAtual.descricao ?? ""
            conteudoJson.value = store.itemAtual.conteudoJson
        }
    }
})

function validarJson(valor: string): boolean {
    try {
        JSON.parse(valor)
        return true
    } catch {
        return false
    }
}

function validar(): boolean {
    const e: Record<string, string> = {}
    if (!nome.value.trim()) e.nome = "Nome é obrigatório."
    if (!conteudoJson.value.trim()) e.conteudoJson = "Conteúdo JSON é obrigatório."
    else if (!validarJson(conteudoJson.value)) e.conteudoJson = "JSON inválido."
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
            conteudoJson: conteudoJson.value,
            motivo: motivo.value.trim(),
        }
        if (editando.value && props.id) {
            await store.atualizar(props.id, payload)
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

function cancelar() {
    router.push({ name: "AdminModelosGlobais" })
}

function formatarJson() {
    try {
        conteudoJson.value = JSON.stringify(JSON.parse(conteudoJson.value), null, 2)
        if (erros.value.conteudoJson) delete erros.value.conteudoJson
    } catch {
        erros.value.conteudoJson = "JSON inválido."
    }
}
</script>

<template>
    <div class="form-page">
        <div class="form-header">
            <button class="btn-voltar" @click="cancelar">
                <i class="fa-solid fa-arrow-left"></i> Voltar
            </button>
            <h1 class="page-titulo">
                {{ editando ? "Editar modelo de prontuário" : "Novo modelo de prontuário" }}
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
                    placeholder="Ex.: Consulta médica padrão"
                    maxlength="200"
                />
                <span v-if="erros.nome" class="erro-msg">{{ erros.nome }}</span>
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
                <div class="json-header">
                    <label class="campo-label" for="conteudoJson">Conteúdo JSON <span class="obrigatorio">*</span></label>
                    <button type="button" class="btn-formatar" @click="formatarJson">Formatar</button>
                </div>
                <textarea
                    id="conteudoJson"
                    v-model="conteudoJson"
                    class="campo-json"
                    :class="{ 'campo-erro': erros.conteudoJson }"
                    rows="16"
                    spellcheck="false"
                    placeholder='{ "secoes": [] }'
                />
                <span v-if="erros.conteudoJson" class="erro-msg">{{ erros.conteudoJson }}</span>
                <span class="campo-dica">Cole a estrutura JSON do template de prontuário. Use "Formatar" para indentar.</span>
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
                    {{ salvando ? "Salvando..." : (editando ? "Salvar alterações" : "Criar modelo") }}
                </button>
            </div>
        </form>
    </div>
</template>

<style scoped>
.form-page { padding: 24px 32px; max-width: 900px; }

.form-header { display: flex; align-items: center; gap: 16px; margin-bottom: 24px; }
.btn-voltar {
    background: none; border: none; color: hsl(var(--muted-foreground)); cursor: pointer;
    font-size: 13px; display: flex; align-items: center; gap: 6px; padding: 0;
}
.btn-voltar:hover { color: hsl(var(--foreground)); }
.page-titulo { font-size: 20px; font-weight: 700; margin: 0; color: hsl(var(--foreground)); }

.form-card {
    background: hsl(var(--card)); border: 1px solid hsl(var(--border));
    border-radius: 10px; padding: 28px; display: flex; flex-direction: column; gap: 20px;
}

.campo-grupo { display: flex; flex-direction: column; gap: 6px; }
.campo-label { font-size: 13px; font-weight: 600; color: hsl(var(--foreground)); }
.obrigatorio { color: hsl(var(--destructive)); }
.campo-dica { font-size: 12px; color: hsl(var(--muted-foreground)); }

.campo-input {
    padding: 8px 10px; border: 1px solid hsl(var(--border)); border-radius: 6px;
    font-size: 13px; background: hsl(var(--background)); color: hsl(var(--foreground));
}
.campo-input.campo-erro, .campo-json.campo-erro { border-color: hsl(var(--destructive)); }

.json-header { display: flex; align-items: center; justify-content: space-between; }
.btn-formatar {
    font-size: 12px; padding: 3px 10px; background: hsl(var(--muted));
    color: hsl(var(--foreground)); border: none; border-radius: 5px; cursor: pointer;
}

.campo-json {
    padding: 10px 12px; border: 1px solid hsl(var(--border)); border-radius: 6px;
    font-size: 12px; font-family: 'Courier New', Courier, monospace;
    background: hsl(var(--background)); color: hsl(var(--foreground));
    resize: vertical; box-sizing: border-box; width: 100%;
    line-height: 1.6;
}

.erro-msg { font-size: 12px; color: hsl(var(--destructive)); }
.erro-geral {
    padding: 10px 14px; background: hsl(var(--destructive) / 0.1);
    border: 1px solid hsl(var(--destructive) / 0.3); border-radius: 6px;
    color: hsl(var(--destructive)); font-size: 13px;
}

.form-acoes { display: flex; justify-content: flex-end; gap: 10px; padding-top: 8px; }

.btn-primario {
    padding: 8px 20px; background: hsl(var(--primary)); color: hsl(var(--primary-foreground));
    border: none; border-radius: 6px; font-size: 13px; font-weight: 600; cursor: pointer;
}
.btn-primario:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-secundario {
    padding: 8px 16px; background: hsl(var(--muted)); color: hsl(var(--foreground));
    border: none; border-radius: 6px; font-size: 13px; cursor: pointer;
}

.estado-centro { text-align: center; padding: 48px; color: hsl(var(--muted-foreground)); }
.estado-erro { text-align: center; padding: 48px; color: hsl(var(--destructive)); }
</style>
