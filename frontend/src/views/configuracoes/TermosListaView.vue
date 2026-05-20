<script setup lang="ts">
/**
 * TermosListaView — lista paginada de modelos de termo de consentimento.
 *
 * 2 abas:
 *  - "Meus termos": modelos do estabelecimento, com CRUD.
 *  - "Padrões do sistema": modelos compartilhados (ehPadraoDoSistema=true),
 *    apenas para visualizar e clonar.
 *
 * Permissão: rota gateada por `termos.gerenciar_modelos` (ver routePermissions).
 * Backend valida a mesma permissão no controller — 422 é fonte da verdade.
 */
import { ref, computed, onMounted, watch } from "vue"
import { useRouter, useRoute } from "vue-router"
import {
    AppPageHeader, AppButton, AppTabs, AppSearchInput, AppSelect, AppEmptyState,
    AppPagination, AppBadge, AppToast, AppConfirmDialog, AppDrawer, AppCheckbox,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { CATEGORIAS_TERMO, TERMO_VARIAVEIS, GRUPOS_VARIAVEL, resolverVariaveisFake } from "@/constants/termoVariaveis"
import { termoModeloService, type TermoModeloDto, type CategoriaTermo } from "@/services/termoModeloService"
import { usePermissoesStore } from "@/stores/permissoesStore"

const router = useRouter()
const route = useRoute()
const permissoes = usePermissoesStore()

const podeGerenciar = computed(() => permissoes.pode("termos.gerenciar_modelos"))

type AbaKey = "meus" | "padroes"
const aba = ref<AbaKey>((route.query.aba as AbaKey) ?? "meus")

watch(aba, (v) => router.replace({ query: { ...route.query, aba: v } }))

const abas = computed(() => [
    { valor: "meus",    label: "Meus termos",         icone: "fa-solid fa-folder-open" },
    { valor: "padroes", label: "Padrões do sistema",  icone: "fa-solid fa-shield-halved" },
])

// ─── Filtros (aba "meus") ─────────────────────────────────────────────────
const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput, 300)
const filtroCategoria = ref<CategoriaTermo | "">("")
const mostrarInativos = ref(false)

const categoriasOpcoes = computed(() => [
    { value: "", label: "Todas as categorias" },
    ...CATEGORIAS_TERMO.map(c => ({ value: c.chave, label: c.label })),
])

// ─── Estado "meus termos" ─────────────────────────────────────────────────
const itens = ref<TermoModeloDto[]>([])
const total = ref(0)
const pagina = ref(1)
const tamanho = ref(20)
const carregando = ref(false)

async function carregarMeus() {
    carregando.value = true
    try {
        const r = await termoModeloService.listarModelos({
            busca: busca.value || undefined,
            categoria: filtroCategoria.value || undefined,
            somenteAtivos: !mostrarInativos.value,
            incluirPadroes: false,
            pagina: pagina.value,
            tamanho: tamanho.value,
        })
        itens.value = r.itens
        total.value = r.total
    } catch (e: any) {
        toast.value = { texto: e?.response?.data?.mensagem ?? "Erro ao listar modelos.", variante: "error" }
    } finally {
        carregando.value = false
    }
}

watch([busca, filtroCategoria, mostrarInativos], () => {
    pagina.value = 1
})
watch([busca, filtroCategoria, mostrarInativos, pagina, tamanho, aba], () => {
    if (aba.value === "meus") carregarMeus()
}, { immediate: true })

// ─── Estado "padrões" ─────────────────────────────────────────────────────
const padroes = ref<TermoModeloDto[]>([])
const carregandoPadroes = ref(false)

async function carregarPadroes() {
    carregandoPadroes.value = true
    try {
        padroes.value = await termoModeloService.listarPadroes()
    } catch (e: any) {
        toast.value = { texto: e?.response?.data?.mensagem ?? "Erro ao listar padrões.", variante: "error" }
    } finally {
        carregandoPadroes.value = false
    }
}

watch(aba, (v) => {
    if (v === "padroes" && padroes.value.length === 0) carregarPadroes()
}, { immediate: true })

// ─── Ações: novo / editar ─────────────────────────────────────────────────
function novoModelo() {
    router.push({ name: "TermosNovo" })
}
function editarModelo(m: TermoModeloDto) {
    router.push({ name: "TermosEditar", params: { id: m.id } })
}

// ─── Ação: excluir ────────────────────────────────────────────────────────
const confirmExclusao = ref<{ aberto: boolean; alvo: TermoModeloDto | null; executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})
function pedirExclusao(m: TermoModeloDto) {
    confirmExclusao.value = { aberto: true, alvo: m, executando: false }
}
async function executarExclusao() {
    const alvo = confirmExclusao.value.alvo
    if (!alvo) return
    confirmExclusao.value.executando = true
    try {
        await termoModeloService.excluirModelo(alvo.id)
        toast.value = { texto: "Modelo excluído.", variante: "success" }
        confirmExclusao.value = { aberto: false, alvo: null, executando: false }
        await carregarMeus()
    } catch (e: any) {
        toast.value = { texto: e?.response?.data?.mensagem ?? "Erro ao excluir.", variante: "error" }
        confirmExclusao.value.executando = false
    }
}

// ─── Ação: ativar/inativar ────────────────────────────────────────────────
const confirmInativar = ref<{ aberto: boolean; alvo: TermoModeloDto | null; executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})
function pedirToggleAtivo(m: TermoModeloDto) {
    if (m.ativo) {
        // Confirmar apenas ao desativar (ativar é seguro, ato simples).
        confirmInativar.value = { aberto: true, alvo: m, executando: false }
    } else {
        executarToggle(m, true)
    }
}
async function executarToggleConfirmado() {
    const alvo = confirmInativar.value.alvo
    if (!alvo) return
    confirmInativar.value.executando = true
    await executarToggle(alvo, false, () => {
        confirmInativar.value = { aberto: false, alvo: null, executando: false }
    })
}
async function executarToggle(m: TermoModeloDto, novoEstado: boolean, onSucesso?: () => void) {
    try {
        await termoModeloService.alterarAtivo(m.id, novoEstado)
        toast.value = {
            texto: novoEstado ? "Modelo reativado." : "Modelo inativado.",
            variante: "success",
        }
        if (onSucesso) onSucesso()
        await carregarMeus()
    } catch (e: any) {
        toast.value = { texto: e?.response?.data?.mensagem ?? "Erro ao alterar status.", variante: "error" }
        confirmInativar.value.executando = false
    }
}

// ─── Ação: clonar padrão ──────────────────────────────────────────────────
const confirmClonar = ref<{ aberto: boolean; alvo: TermoModeloDto | null; executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})
function pedirClonagem(m: TermoModeloDto) {
    confirmClonar.value = { aberto: true, alvo: m, executando: false }
}
async function executarClonagem() {
    const alvo = confirmClonar.value.alvo
    if (!alvo) return
    confirmClonar.value.executando = true
    try {
        const novoId = await termoModeloService.clonarPadrao(alvo.id)
        confirmClonar.value = { aberto: false, alvo: null, executando: false }
        toast.value = { texto: "Modelo clonado. Edite e salve.", variante: "success" }
        router.push({ name: "TermosEditar", params: { id: novoId } })
    } catch (e: any) {
        toast.value = { texto: e?.response?.data?.mensagem ?? "Erro ao clonar.", variante: "error" }
        confirmClonar.value.executando = false
    }
}

// ─── Ação: visualizar padrão (drawer read-only) ───────────────────────────
const visualizar = ref<{ aberto: boolean; alvo: TermoModeloDto | null }>({ aberto: false, alvo: null })
function abrirVisualizar(m: TermoModeloDto) {
    visualizar.value = { aberto: true, alvo: m }
}
const visualizarHtml = computed(() => {
    if (!visualizar.value.alvo) return ""
    return resolverVariaveisFake(visualizar.value.alvo.conteudoHtml)
})

// ─── Helpers de UI ────────────────────────────────────────────────────────
function metaCategoria(c: string) {
    return CATEGORIAS_TERMO.find(m => m.chave === c) ?? { chave: c, label: c, cor: "muted" as const }
}
function previewTexto(html: string, limite = 150): string {
    const tmp = document.createElement("div")
    tmp.innerHTML = html
    const t = (tmp.textContent ?? "").trim().replace(/\s+/g, " ")
    return t.length > limite ? `${t.substring(0, limite)}…` : t
}
function formatarData(iso: string): string {
    try { return new Date(iso).toLocaleDateString("pt-BR") } catch { return "" }
}

const toast = ref<{ texto: string; variante: "success" | "error" | "info" } | null>(null)

onMounted(() => {
    // Se entrou com ?aba=padroes, força carregamento.
    if (aba.value === "padroes" && padroes.value.length === 0) carregarPadroes()
})
</script>

<template>
    <div class="app-page">
        <AppPageHeader
            titulo="Termos de consentimento"
            subtitulo="Gerencie os modelos de termos que serão emitidos para os pacientes."
        >
            <template #acoes>
                <AppButton
                    v-if="podeGerenciar"
                    icon="fa-solid fa-plus"
                    @click="novoModelo"
                >Novo modelo</AppButton>
            </template>
        </AppPageHeader>

        <AppTabs :model-value="aba" :abas="abas" variante="underline" @update:model-value="(v: any) => aba = v as AbaKey" />

        <!-- ─────────────────────── Aba: Meus termos ─────────────────────── -->
        <section v-if="aba === 'meus'" class="painel">
            <div class="filtros">
                <AppSearchInput v-model="buscaInput" placeholder="Buscar por título…" />
                <AppSelect
                    v-model="filtroCategoria as any"
                    :options="categoriasOpcoes as any"
                    class="filtro-categoria"
                />
                <label class="toggle-inativos">
                    <AppCheckbox v-model="mostrarInativos" />
                    <span>Mostrar inativos</span>
                </label>
            </div>

            <div v-if="carregando && itens.length === 0" class="estado-msg">Carregando…</div>

            <div v-else-if="itens.length === 0" class="estado-vazio">
                <AppEmptyState
                    icone="fa-solid fa-file-signature"
                    titulo="Nenhum modelo encontrado"
                    descricao="Clique em 'Novo modelo' para criar do zero ou abra a aba 'Padrões do sistema' para clonar um modelo pronto."
                />
            </div>

            <div v-else class="tabela-wrapper">
                <table class="tabela">
                    <thead>
                        <tr>
                            <th>Título</th>
                            <th>Categoria</th>
                            <th>Versão</th>
                            <th>Status</th>
                            <th>Criado em</th>
                            <th class="th-acoes">Ações</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="m in itens" :key="m.id">
                            <td>
                                <button
                                    type="button"
                                    class="link-titulo"
                                    :disabled="!podeGerenciar"
                                    @click="podeGerenciar && editarModelo(m)"
                                >{{ m.titulo }}</button>
                            </td>
                            <td>
                                <AppBadge :label="metaCategoria(m.categoria).label" :variant="metaCategoria(m.categoria).cor" />
                            </td>
                            <td><span class="versao">v{{ m.versaoAtual }}</span></td>
                            <td>
                                <AppBadge :label="m.ativo ? 'Ativo' : 'Inativo'" :variant="m.ativo ? 'success' : 'muted'" />
                            </td>
                            <td class="cinza">{{ formatarData(m.criadoEm) }}</td>
                            <td class="cell-acoes">
                                <button
                                    class="btn-icon btn-icon-editar"
                                    :disabled="!podeGerenciar"
                                    title="Editar"
                                    @click="editarModelo(m)"
                                ><i class="fa-solid fa-pen" /></button>
                                <button
                                    class="btn-icon"
                                    :disabled="!podeGerenciar"
                                    :title="m.ativo ? 'Inativar' : 'Reativar'"
                                    @click="pedirToggleAtivo(m)"
                                ><i :class="m.ativo ? 'fa-solid fa-eye-slash' : 'fa-solid fa-eye'" /></button>
                                <button
                                    class="btn-icon btn-icon-excluir"
                                    :disabled="!podeGerenciar"
                                    title="Excluir"
                                    @click="pedirExclusao(m)"
                                ><i class="fa-solid fa-trash" /></button>
                            </td>
                        </tr>
                    </tbody>
                </table>

                <AppPagination
                    v-model:pagina="pagina"
                    v-model:tamanho="tamanho"
                    :total="total"
                />
            </div>
        </section>

        <!-- ────────────────────── Aba: Padrões do sistema ────────────────────── -->
        <section v-else-if="aba === 'padroes'" class="painel">
            <p class="texto-intro">
                Estes são modelos prontos da Imedto. Clone um para customizar
                para o seu estabelecimento — atualizações futuras do padrão não
                refletem no seu clone.
            </p>

            <div v-if="carregandoPadroes" class="estado-msg">Carregando…</div>

            <div v-else-if="padroes.length === 0" class="estado-vazio">
                <AppEmptyState
                    icone="fa-solid fa-shield-halved"
                    titulo="Nenhum padrão disponível"
                    descricao="Tente novamente em alguns instantes."
                />
            </div>

            <div v-else class="cards-padroes">
                <article v-for="m in padroes" :key="m.id" class="card-padrao">
                    <header class="card-cab">
                        <h3 class="card-titulo">{{ m.titulo }}</h3>
                        <AppBadge :label="metaCategoria(m.categoria).label" :variant="metaCategoria(m.categoria).cor" />
                    </header>
                    <p class="card-preview">{{ previewTexto(m.conteudoHtml) }}</p>
                    <footer class="card-acoes">
                        <AppButton
                            variant="secondary"
                            size="sm"
                            icon="fa-solid fa-eye"
                            @click="abrirVisualizar(m)"
                        >Visualizar</AppButton>
                        <AppButton
                            v-if="podeGerenciar"
                            size="sm"
                            icon="fa-solid fa-copy"
                            @click="pedirClonagem(m)"
                        >Clonar</AppButton>
                    </footer>
                </article>
            </div>
        </section>

        <!-- ────────────────────── Diálogos ────────────────────── -->
        <AppConfirmDialog
            v-model:aberto="confirmExclusao.aberto"
            titulo="Excluir modelo?"
            :mensagem="`Tem certeza que deseja excluir “${confirmExclusao.alvo?.titulo ?? ''}”? Termos já emitidos a partir deste modelo continuarão acessíveis (não serão apagados).`"
            confirmar-rotulo="Excluir"
            variante="danger"
            icone="fa-solid fa-trash"
            :executando="confirmExclusao.executando"
            @confirmar="executarExclusao"
        />

        <AppConfirmDialog
            v-model:aberto="confirmInativar.aberto"
            titulo="Inativar modelo?"
            :mensagem="`Modelo inativo não aparece na lista de emissão. Termos já emitidos continuam válidos. Inativar “${confirmInativar.alvo?.titulo ?? ''}”?`"
            confirmar-rotulo="Inativar"
            variante="danger"
            icone="fa-solid fa-eye-slash"
            :executando="confirmInativar.executando"
            @confirmar="executarToggleConfirmado"
        />

        <AppConfirmDialog
            v-model:aberto="confirmClonar.aberto"
            titulo="Clonar padrão do sistema?"
            :mensagem="`Esta cópia ficará independente do padrão. Atualizações futuras do padrão não refletirão no seu clone. Clonar “${confirmClonar.alvo?.titulo ?? ''}”?`"
            confirmar-rotulo="Clonar"
            variante="primary"
            icone="fa-solid fa-copy"
            :executando="confirmClonar.executando"
            @confirmar="executarClonagem"
        />

        <!-- Visualizar padrão -->
        <AppDrawer
            :aberto="visualizar.aberto"
            :titulo="visualizar.alvo?.titulo"
            :largura="720"
            @fechar="visualizar = { aberto: false, alvo: null }"
        >
            <div v-if="visualizar.alvo" class="visualizar">
                <div class="visualizar-meta">
                    <AppBadge
                        :label="metaCategoria(visualizar.alvo.categoria).label"
                        :variant="metaCategoria(visualizar.alvo.categoria).cor"
                    />
                </div>
                <!-- v-html sanitizado pelo backend (Ganss.Xss) -->
                <div class="visualizar-html" v-html="visualizarHtml" />

                <details class="visualizar-vars">
                    <summary>Variáveis usadas</summary>
                    <div v-for="g in GRUPOS_VARIAVEL" :key="g.chave" class="var-grupo">
                        <strong><i :class="g.icone" /> {{ g.rotulo }}</strong>
                        <ul>
                            <li v-for="v in TERMO_VARIAVEIS.filter(x => x.grupo === g.chave)" :key="v.chave">
                                <code>{{ v.chave }}</code> — {{ v.rotulo }}
                            </li>
                        </ul>
                    </div>
                </details>
            </div>
            <template #rodape>
                <AppButton variant="secondary" @click="visualizar = { aberto: false, alvo: null }">Fechar</AppButton>
                <AppButton
                    v-if="podeGerenciar && visualizar.alvo"
                    icon="fa-solid fa-copy"
                    @click="() => { const a = visualizar.alvo!; visualizar = { aberto: false, alvo: null }; pedirClonagem(a) }"
                >Clonar este padrão</AppButton>
            </template>
        </AppDrawer>

        <AppToast
            v-if="toast"
            :mensagem="toast.texto"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </div>
</template>

<style scoped>
.painel { margin-top: 1rem; display: flex; flex-direction: column; gap: 1rem; }

.filtros {
    display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center;
}
.filtro-categoria { max-width: 240px; }
.toggle-inativos {
    display: inline-flex; align-items: center; gap: 0.45rem;
    font-size: 0.85em; color: hsl(var(--foreground));
    cursor: pointer;
}

.estado-msg { text-align: center; padding: 3rem 1rem; color: hsl(var(--muted-foreground)); }
.estado-vazio { padding: 1.5rem 0; }

.tabela-wrapper {
    background: white; border: 1px solid hsl(var(--border));
    border-radius: var(--radius); overflow: hidden;
}
.tabela { width: 100%; border-collapse: collapse; font-size: 0.875em; }
.tabela thead th {
    text-align: left;
    background: hsl(var(--muted));
    padding: 10px 14px;
    font-weight: 600; color: hsl(var(--muted-foreground));
    border-bottom: 1px solid hsl(var(--border));
}
.tabela tbody td {
    padding: 12px 14px;
    border-bottom: 1px solid hsl(var(--border) / 0.6);
}
.tabela tbody tr:last-child td { border-bottom: none; }
.tabela tbody tr:hover { background: hsl(var(--accent) / 0.5); }

.link-titulo {
    background: none; border: none; padding: 0;
    color: hsl(var(--primary)); cursor: pointer; font-weight: 600;
    text-align: left; font-family: inherit; font-size: inherit;
}
.link-titulo:hover:not(:disabled) { text-decoration: underline; }
.link-titulo:disabled { cursor: default; color: hsl(var(--foreground)); }

.versao { font-family: ui-monospace, monospace; color: hsl(var(--muted-foreground)); }
.cinza { color: hsl(var(--muted-foreground)); }

.th-acoes, .cell-acoes { text-align: right; white-space: nowrap; }
.cell-acoes { display: flex; gap: 6px; justify-content: flex-end; }

.texto-intro {
    margin: 0; padding: 0.9rem 1rem;
    background: hsl(var(--primary) / 0.05);
    border-left: 3px solid hsl(var(--primary));
    border-radius: 4px; font-size: 0.875em;
}

.cards-padroes {
    display: grid; gap: 1rem;
    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
}
.card-padrao {
    background: white; border: 1px solid hsl(var(--border));
    border-radius: var(--radius); padding: 1rem 1.25rem;
    display: flex; flex-direction: column; gap: 0.75rem;
}
.card-cab { display: flex; align-items: flex-start; justify-content: space-between; gap: 0.5rem; }
.card-titulo { font-size: 1em; margin: 0; font-weight: 700; }
.card-preview { font-size: 0.85em; color: hsl(var(--muted-foreground)); margin: 0; }
.card-acoes { display: flex; gap: 0.5rem; }

/* Visualizar drawer */
.visualizar { display: flex; flex-direction: column; gap: 1rem; }
.visualizar-meta { display: flex; gap: 0.5rem; }
.visualizar-html {
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    background: white;
    padding: 1.25rem 1.5rem;
    font-size: 0.95em; line-height: 1.6;
}
.visualizar-html :deep(h2) { font-size: 1.2em; font-weight: 700; margin: 1em 0 0.4em; }
.visualizar-html :deep(h3) { font-size: 1.05em; font-weight: 700; margin: 0.8em 0 0.4em; }
.visualizar-html :deep(p) { margin: 0 0 0.6em; }
.visualizar-html :deep(blockquote) {
    border-left: 3px solid hsl(var(--primary) / 0.4);
    padding-left: 12px; color: hsl(var(--muted-foreground)); margin: 0.6em 0;
}
.visualizar-vars { font-size: 0.85em; }
.visualizar-vars summary { cursor: pointer; font-weight: 600; padding: 0.4rem 0; }
.var-grupo { margin-top: 0.6rem; }
.var-grupo strong { display: block; margin-bottom: 0.3rem; color: hsl(var(--primary)); }
.var-grupo ul { margin: 0; padding-left: 1.2rem; }
.var-grupo code {
    background: hsl(var(--muted));
    padding: 1px 6px; border-radius: 4px;
    font-size: 0.85em;
}
</style>
