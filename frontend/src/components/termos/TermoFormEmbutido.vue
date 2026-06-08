<script setup lang="ts">
/**
 * TermoFormEmbutido — editor de termo de consentimento embarcável no painel inline.
 *
 * Diferença de TermoFormView:
 *   - Entrada: prop `id` (number|null) em vez de rota param.
 *   - Saída: emit `@voltar` em vez de router.push({ name: "TermosModelos" }).
 *   - Após criar novo modelo, em vez de router.replace para TermosEditar, atualiza
 *     o id internamente e permanece no editor (mesmo comportamento observável).
 *
 * Lógica, serviço (termoModeloService), validações e comportamento 422: idênticos
 * ao TermoFormView original — só a camada de navegação mudou.
 */
import { ref, computed, onMounted, watch } from "vue"
import {
    AppButton, AppField, AppInput, AppSelect, AppToast, AppCard,
} from "@/components/ui"
import TermoEditorTipTap from "@/components/termos/TermoEditorTipTap.vue"
import { CATEGORIAS_TERMO, TERMO_VARIAVEIS, GRUPOS_VARIAVEL, resolverVariaveisFake } from "@/constants/termoVariaveis"
import { termoModeloService, type CategoriaTermo } from "@/services/termoModeloService"

const props = defineProps<{
    id?: number | null
}>()

const emit = defineEmits<{
    voltar: []
}>()

// id interno — começa com a prop, muda para o id do novo termo após criação
const idInterno = ref<number | null>(props.id ?? null)

watch(() => props.id, (v) => { idInterno.value = v ?? null })

const modoEdicao = computed(() => idInterno.value !== null)

const titulo = ref("")
const categoria = ref<CategoriaTermo>("geral")
const conteudoHtml = ref("")
const versaoAtual = ref<number | null>(null)

const editorRef = ref<InstanceType<typeof TermoEditorTipTap> | null>(null)
const carregando = ref(false)
const salvando = ref(false)
const toast = ref<{ texto: string; variante: "success" | "error" | "info" } | null>(null)

const opcoesCategoria = computed(() => CATEGORIAS_TERMO.map(c => ({ value: c.chave, label: c.label })))

// ─── Validação espelhada (UX) — idêntica ao TermoFormView ────────────────────
const erroTitulo = computed(() => {
    const t = titulo.value.trim()
    if (!t) return null
    if (t.length < 3) return "Mínimo 3 caracteres."
    if (t.length > 120) return "Máximo 120 caracteres."
    return null
})
const conteudoVazio = computed(() => {
    const tmp = document.createElement("div")
    tmp.innerHTML = conteudoHtml.value
    return (tmp.textContent ?? "").trim().length === 0
})
const podeSalvar = computed(() =>
    !salvando.value
    && titulo.value.trim().length >= 3
    && titulo.value.trim().length <= 120
    && !conteudoVazio.value,
)

// ─── Carregar (modo edição) ─────────────────────────────────────────────────
async function carregar() {
    if (!modoEdicao.value || !idInterno.value) return
    carregando.value = true
    try {
        const m = await termoModeloService.obterModelo(idInterno.value)
        titulo.value = m.titulo
        categoria.value = m.categoria
        conteudoHtml.value = m.conteudoHtml
        versaoAtual.value = m.versaoAtual
    } catch (e: any) {
        toast.value = { texto: e?.response?.data?.mensagem ?? "Erro ao carregar modelo.", variante: "error" }
    } finally {
        carregando.value = false
    }
}

// ─── Salvar ─────────────────────────────────────────────────────────────────
async function salvar() {
    if (!podeSalvar.value) return
    salvando.value = true
    try {
        const payload = {
            categoria: categoria.value,
            titulo: titulo.value.trim(),
            conteudoHtml: conteudoHtml.value,
        }
        if (modoEdicao.value && idInterno.value) {
            await termoModeloService.atualizarModelo(idInterno.value, payload)
            toast.value = { texto: "Modelo salvo.", variante: "success" }
        } else {
            const novoId = await termoModeloService.criarModelo(payload)
            idInterno.value = novoId
            toast.value = { texto: "Modelo criado.", variante: "success" }
        }
    } catch (e: any) {
        const status = e?.response?.status
        if (status === 409) {
            toast.value = { texto: "Este modelo foi atualizado por outra pessoa. Recarregue a página.", variante: "error" }
        } else {
            toast.value = { texto: e?.response?.data?.mensagem ?? "Erro ao salvar.", variante: "error" }
        }
    } finally {
        salvando.value = false
    }
}

function voltar() {
    emit("voltar")
}

// ─── Sidebar: inserir variável ──────────────────────────────────────────────
function inserirVariavel(chave: string) {
    editorRef.value?.inserirVariavel(chave)
}

const variaveisPorGrupo = computed(() =>
    GRUPOS_VARIAVEL.map(g => ({
        ...g,
        itens: TERMO_VARIAVEIS.filter(v => v.grupo === g.chave),
    })),
)

// ─── Preview ────────────────────────────────────────────────────────────────
const previewAberto = ref(false)
const previewHtml = computed(() => resolverVariaveisFake(conteudoHtml.value))

onMounted(carregar)
</script>

<template>
    <div class="termo-form-embutido">
        <!-- Cabeçalho inline (sem AppPageHeader — já está dentro do painel) -->
        <div class="form-header">
            <div class="form-header-info">
                <h3 class="form-titulo">
                    {{ modoEdicao ? "Editar modelo de termo" : "Novo modelo de termo" }}
                </h3>
                <p v-if="modoEdicao && versaoAtual" class="form-sub">Versão atual: v{{ versaoAtual }}</p>
                <p v-else class="form-sub">Preencha título, escolha a categoria e elabore o conteúdo do termo.</p>
            </div>
            <div class="form-acoes">
                <AppButton variant="secondary" icon="fa-solid fa-arrow-left" @click="voltar">Voltar</AppButton>
                <AppButton :loading="salvando" :disabled="!podeSalvar" icon="fa-solid fa-floppy-disk" @click="salvar">
                    {{ modoEdicao ? "Salvar alterações" : "Criar modelo" }}
                </AppButton>
            </div>
        </div>

        <div v-if="carregando" class="estado-msg">Carregando…</div>

        <div v-else class="form-grid">
            <!-- Coluna principal ─────────────────────────────────────────── -->
            <div class="col-principal">
                <AppCard>
                    <div class="grade-meta">
                        <AppField label="Título do modelo" :erro="erroTitulo" required>
                            <AppInput
                                v-model="titulo as any"
                                placeholder="Ex.: Termo de Consentimento Cirúrgico"
                                :disabled="salvando"
                            />
                        </AppField>
                        <AppField label="Categoria" required>
                            <AppSelect
                                v-model="categoria as any"
                                :options="opcoesCategoria as any"
                                :disabled="salvando"
                            />
                        </AppField>
                    </div>

                    <AppField label="Conteúdo do termo" hint="Use as variáveis da barra lateral — elas serão substituídas automaticamente quando o termo for emitido.">
                        <TermoEditorTipTap
                            ref="editorRef"
                            v-model="conteudoHtml"
                            :disabled="salvando"
                            placeholder="Comece digitando o conteúdo do termo. Use a barra lateral para inserir variáveis dinâmicas."
                        />
                    </AppField>

                    <div v-if="conteudoVazio" class="aviso-vazio">
                        O conteúdo é obrigatório.
                    </div>
                </AppCard>
            </div>

            <!-- Coluna lateral ───────────────────────────────────────────── -->
            <aside class="col-sidebar">
                <AppCard>
                    <h3 class="ds-card-title card-cabecalho">
                        <i class="fa-solid fa-puzzle-piece" /> Variáveis disponíveis
                    </h3>
                    <p class="card-sub">Clique para inserir no cursor do editor.</p>

                    <div v-for="g in variaveisPorGrupo" :key="g.chave" class="var-grupo">
                        <h4 class="var-grupo-titulo">
                            <i :class="g.icone" /> {{ g.rotulo }}
                        </h4>
                        <ul class="var-lista">
                            <li v-for="v in g.itens" :key="v.chave">
                                <button
                                    type="button"
                                    class="var-btn"
                                    :title="`Inserir ${v.chave}`"
                                    @click="inserirVariavel(v.chave)"
                                >
                                    <span class="var-rotulo">{{ v.rotulo }}</span>
                                    <code class="var-chave">{{ v.chave }}</code>
                                </button>
                            </li>
                        </ul>
                    </div>
                </AppCard>

                <AppCard class="card-preview">
                    <header class="preview-cab">
                        <h3 class="ds-card-title card-cabecalho">
                            <i class="fa-solid fa-eye" /> Preview
                        </h3>
                        <AppButton variant="ghost" size="sm" @click="previewAberto = !previewAberto">
                            {{ previewAberto ? "Ocultar" : "Mostrar" }}
                        </AppButton>
                    </header>
                    <p class="card-sub">Pré-visualização com dados fictícios. Em produção, os dados reais do paciente, profissional e estabelecimento substituirão as variáveis.</p>
                    <div v-if="previewAberto" class="preview-area" v-html="previewHtml" />
                </AppCard>
            </aside>
        </div>

        <AppToast
            v-if="toast"
            :mensagem="toast.texto"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </div>
</template>

<style scoped>
.termo-form-embutido { display: flex; flex-direction: column; gap: 1rem; }

.form-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
    flex-wrap: wrap;
}
.form-header-info { flex: 1; min-width: 0; }
.form-titulo { font-size: var(--text-md); font-weight: var(--font-weight-bold); margin: 0 0 0.2rem; }
.form-sub { font-size: 0.82em; color: hsl(var(--muted-foreground)); margin: 0; }
.form-acoes { display: flex; gap: 0.5rem; flex-shrink: 0; flex-wrap: wrap; }

.estado-msg { text-align: center; padding: 3rem 1rem; color: hsl(var(--muted-foreground)); }

.form-grid {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 320px;
    gap: 1.25rem;
    align-items: start;
}
@media (max-width: 860px) {
    .form-grid { grid-template-columns: 1fr; }
}

.col-principal { display: flex; flex-direction: column; gap: 1rem; min-width: 0; }
.col-sidebar { display: flex; flex-direction: column; gap: 1rem; position: sticky; top: 1rem; }

.grade-meta {
    display: grid;
    grid-template-columns: 1.6fr 1fr;
    gap: 1rem;
    margin-bottom: 1rem;
}
@media (max-width: 720px) {
    .grade-meta { grid-template-columns: 1fr; }
}

.aviso-vazio {
    margin-top: 0.5rem; padding: 0.5rem 0.8rem;
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.08);
    border-radius: var(--radius);
    font-size: 0.82em;
}

.card-cabecalho {
    display: flex; align-items: center; gap: 0.5rem;
}
.card-cabecalho i { color: hsl(var(--primary)); }
.card-sub {
    margin: 0.25rem 0 0.75rem;
    font-size: 0.78em;
    color: hsl(var(--muted-foreground));
}

.var-grupo { margin-top: 0.85rem; }
.var-grupo-titulo {
    font-size: 0.78em; font-weight: 600; color: hsl(var(--primary));
    text-transform: uppercase; letter-spacing: 0.04em;
    margin: 0 0 0.35rem;
    display: flex; align-items: center; gap: 0.4rem;
}
.var-lista { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 4px; }
.var-btn {
    width: 100%; text-align: left;
    background: hsl(var(--muted));
    border: 1px solid transparent;
    padding: 6px 10px; border-radius: 6px; cursor: pointer;
    display: flex; flex-direction: column; gap: 1px;
    font-family: inherit; font-size: 0.82em;
    transition: all 0.12s;
}
.var-btn:hover {
    background: hsl(var(--primary) / 0.08);
    border-color: hsl(var(--primary) / 0.25);
}
.var-rotulo { font-weight: 600; color: hsl(var(--foreground)); }
.var-chave {
    font-family: ui-monospace, monospace;
    font-size: 0.85em; color: hsl(var(--muted-foreground));
}

.card-preview { max-height: 65vh; overflow-y: auto; }
.preview-cab { display: flex; align-items: center; justify-content: space-between; gap: 0.5rem; }
.preview-area {
    margin-top: 0.5rem; padding: 0.85rem 1rem;
    border: 1px solid hsl(var(--border));
    background: white; border-radius: var(--radius);
    font-size: 0.85em; line-height: 1.55;
    max-height: 50vh; overflow-y: auto;
}
.preview-area :deep(h2) { font-size: 1.15em; font-weight: 700; margin: 0.8em 0 0.3em; }
.preview-area :deep(h3) { font-size: 1.02em; font-weight: 700; margin: 0.6em 0 0.3em; }
.preview-area :deep(p) { margin: 0 0 0.5em; }
.preview-area :deep(blockquote) {
    border-left: 3px solid hsl(var(--primary) / 0.4);
    padding-left: 10px; color: hsl(var(--muted-foreground)); margin: 0.5em 0;
}
.preview-area :deep(ul),
.preview-area :deep(ol) { padding-left: 1.2em; margin: 0 0 0.5em; }
</style>
