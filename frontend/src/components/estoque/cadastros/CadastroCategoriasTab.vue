<script setup lang="ts">
import { ref, watch, onMounted } from "vue"
import {
    AppSearchInput, AppButton, AppEmptyState, AppPagination, AppDrawer,
    AppField, AppInput, AppStatusPill, AppToast, AppConfirmDialog,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import {
    estoqueCadastrosService,
    type CategoriaEstoque,
    type CategoriaPayload,
    ICONES_CATEGORIA, CORES_CATEGORIA,
} from "@/services/estoqueCadastrosService"

const emit = defineEmits<{ "total-change": [total: number] }>()

// ─── Estado ──────────────────────────────────────────────────────────
const itens = ref<CategoriaEstoque[]>([])
const total = ref(0)
const pagina = ref(1)
const tamanho = ref(20)
const carregando = ref(false)
const erro = ref<string | null>(null)

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
const apenasAtivos = ref(true)

// Toast e confirmação.
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}
const confirmacao = ref<{ aberto: boolean, alvo: CategoriaEstoque | null, executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

// ─── Drawer ──────────────────────────────────────────────────────────
const drawerAberto = ref(false)
const categoriaEditando = ref<CategoriaEstoque | null>(null)
const form = ref<CategoriaPayload>({ nome: "", cor: CORES_CATEGORIA[0].valor, icone: ICONES_CATEGORIA[0].valor })
const erroForm = ref<string | null>(null)
const salvando = ref(false)

function abrirCriar() {
    categoriaEditando.value = null
    form.value = { nome: "", cor: CORES_CATEGORIA[0].valor, icone: ICONES_CATEGORIA[0].valor }
    erroForm.value = null
    drawerAberto.value = true
}

function abrirEditar(c: CategoriaEstoque) {
    categoriaEditando.value = c
    form.value = { nome: c.nome, cor: c.cor, icone: c.icone }
    erroForm.value = null
    drawerAberto.value = true
}

async function salvar() {
    erroForm.value = null
    if (!form.value.nome.trim()) { erroForm.value = "Nome é obrigatório."; return }
    salvando.value = true
    try {
        if (categoriaEditando.value) {
            await estoqueCadastrosService.categorias.atualizar(categoriaEditando.value.id, form.value)
        } else {
            await estoqueCadastrosService.categorias.criar(form.value)
        }
        drawerAberto.value = false
        await carregar()
    } catch (e: any) {
        erroForm.value = e?.response?.data?.mensagem ?? "Erro ao salvar categoria."
    } finally {
        salvando.value = false
    }
}

function pedirInativacao(c: CategoriaEstoque) {
    confirmacao.value = { aberto: true, alvo: c, executando: false }
}

async function executarInativacao() {
    const alvo = confirmacao.value.alvo
    if (!alvo) return
    confirmacao.value.executando = true
    try {
        await estoqueCadastrosService.categorias.inativar(alvo.id)
        confirmacao.value = { aberto: false, alvo: null, executando: false }
        notificar("Categoria inativada.")
        await carregar()
    } catch (e: any) {
        confirmacao.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Erro ao inativar.", "error")
    }
}

async function reativar(c: CategoriaEstoque) {
    try {
        await estoqueCadastrosService.categorias.reativar(c.id)
        await carregar()
        notificar("Categoria reativada.")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao reativar.", "error")
    }
}

// ─── Carga ───────────────────────────────────────────────────────────
async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const pg = await estoqueCadastrosService.categorias.listar({
            busca: busca.value || undefined,
            apenasAtivos: apenasAtivos.value ? true : undefined,
            pagina: pagina.value,
            tamanho: tamanho.value,
        })
        itens.value = pg.itens
        total.value = pg.total
        emit("total-change", pg.total)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar categorias."
    } finally {
        carregando.value = false
    }
}

watch(busca, () => { pagina.value = 1 })
watch([busca, pagina, tamanho, apenasAtivos], () => carregar(), { immediate: false })

onMounted(carregar)
</script>

<template>
    <div class="cad-tab">
        <div class="filtros-bar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar categoria..." />
            <label class="filtro-ativos">
                <input type="checkbox" v-model="apenasAtivos" />
                Mostrar só ativas
            </label>
            <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Nova categoria</AppButton>
        </div>

        <div v-if="erro" class="msg-erro">{{ erro }}</div>

        <div class="tabela">
            <div class="thead" aria-hidden="true">
                <div>Categoria</div>
                <div>Itens</div>
                <div>Status</div>
                <div></div>
            </div>

            <div v-if="carregando" class="tabela-loading">
                <i class="fa-solid fa-spinner fa-spin"></i> Carregando…
            </div>

            <div v-else-if="itens.length === 0" class="tabela-vazio">
                <AppEmptyState
                    icone="fa-solid fa-tags"
                    titulo="Nenhuma categoria encontrada"
                    descricao="Crie sua primeira categoria para organizar os itens do estoque."
                    :compacto="true"
                >
                    <template #acao>
                        <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Nova categoria</AppButton>
                    </template>
                </AppEmptyState>
            </div>

            <div v-else>
                <div v-for="c in itens" :key="c.id" class="row" :class="{ inativo: !c.ativo }">
                    <div class="cat-nome">
                        <span class="cat-icone" :style="{ background: c.cor, color: '#fff' }">
                            <i :class="`fa-solid ${c.icone}`"></i>
                        </span>
                        <b>{{ c.nome }}</b>
                    </div>
                    <div class="cat-itens">{{ c.quantidadeItens }} {{ c.quantidadeItens === 1 ? "item" : "itens" }}</div>
                    <div>
                        <AppStatusPill
                            :label="c.ativo ? 'Ativa' : 'Inativa'"
                            :variante="c.ativo ? 'success' : 'muted'"
                        />
                    </div>
                    <div class="acoes">
                        <button
                            type="button"
                            class="btn-icon btn-icon-editar"
                            title="Editar"
                            @click="abrirEditar(c)"
                        >
                            <i class="fa-solid fa-pen"></i>
                        </button>
                        <button
                            v-if="c.ativo"
                            type="button"
                            class="btn-icon btn-icon-excluir"
                            title="Inativar"
                            @click="pedirInativacao(c)"
                        >
                            <i class="fa-solid fa-ban"></i>
                        </button>
                        <button
                            v-else
                            type="button"
                            class="btn-icon"
                            title="Reativar"
                            @click="reativar(c)"
                        >
                            <i class="fa-solid fa-rotate-left"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <AppPagination
            v-if="total > 0 && !carregando"
            :pagina="pagina"
            :tamanho="tamanho"
            :total="total"
            rotulo-itens="categorias"
            class="paginacao"
            @update:pagina="(p: number) => (pagina = p)"
            @update:tamanho="(t: number) => (tamanho = t)"
        />

        <AppConfirmDialog
            v-model:aberto="confirmacao.aberto"
            titulo="Inativar categoria?"
            :mensagem="confirmacao.alvo ? `Inativar a categoria ${confirmacao.alvo.nome}?` : ''"
            confirmar-rotulo="Inativar"
            variante="danger"
            :executando="confirmacao.executando"
            @confirmar="executarInativacao"
        />

        <AppToast
            v-if="toast"
            :mensagem="toast.mensagem"
            :variante="toast.variante"
            @fechar="toast = null"
        />

        <!-- Drawer de criar/editar -->
        <AppDrawer
            :aberto="drawerAberto"
            :titulo="categoriaEditando ? `Editar — ${categoriaEditando.nome}` : 'Nova categoria'"
            :largura="500"
            @fechar="drawerAberto = false"
        >
            <div class="form-drawer">
                <AppField label="Nome" required>
                    <AppInput v-model="form.nome" placeholder="Ex: Anestésicos" />
                </AppField>

                <AppField label="Cor" required>
                    <div class="paleta">
                        <button
                            v-for="c in CORES_CATEGORIA"
                            :key="c.valor"
                            type="button"
                            class="swatch"
                            :class="{ ativo: form.cor === c.valor }"
                            :style="{ background: c.valor }"
                            :title="c.rotulo"
                            @click="form.cor = c.valor"
                        ></button>
                    </div>
                </AppField>

                <AppField label="Ícone" required>
                    <div class="icones">
                        <button
                            v-for="i in ICONES_CATEGORIA"
                            :key="i.valor"
                            type="button"
                            class="icone-btn"
                            :class="{ ativo: form.icone === i.valor }"
                            :title="i.rotulo"
                            @click="form.icone = i.valor"
                        >
                            <i :class="`fa-solid ${i.valor}`"></i>
                        </button>
                    </div>
                </AppField>

                <div class="preview">
                    <span class="cat-icone" :style="{ background: form.cor, color: '#fff' }">
                        <i :class="`fa-solid ${form.icone}`"></i>
                    </span>
                    <span class="preview-text">{{ form.nome || "Pré-visualização" }}</span>
                </div>

                <p v-if="erroForm" class="msg-erro">{{ erroForm }}</p>

                <div class="acoes-form">
                    <AppButton variant="secondary" @click="drawerAberto = false">Cancelar</AppButton>
                    <AppButton
                        :icon="categoriaEditando ? 'fa-solid fa-check' : 'fa-solid fa-plus'"
                        :loading="salvando"
                        :disabled="salvando"
                        @click="salvar"
                    >
                        {{ categoriaEditando ? "Salvar" : "Criar" }}
                    </AppButton>
                </div>
            </div>
        </AppDrawer>
    </div>
</template>

<style scoped>
.cad-tab { display: flex; flex-direction: column; gap: 12px; }
.filtros-bar { display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }
.filtro-ativos {
    display: inline-flex; align-items: center; gap: 6px;
    font-size: 13px; color: hsl(var(--secondary) / 0.7);
}
.msg-erro { color: hsl(var(--error)); font-size: 13px; }

.tabela {
    background: var(--bg-card);
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-lg);
    overflow: hidden;
    box-shadow: var(--shadow);
}
.thead, .row {
    display: grid;
    grid-template-columns: 2.5fr 0.8fr 110px auto;
    gap: 12px;
    align-items: center;
    padding: 11px 16px;
}
.thead {
    background: hsl(var(--secondary) / 0.025);
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    font-size: 10px;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: hsl(var(--secondary) / 0.55);
}
.row { border-bottom: 1px solid hsl(var(--secondary) / 0.05); }
.row:last-child { border-bottom: 0; }
.row.inativo { opacity: 0.6; }
.row:hover { background: hsl(var(--primary) / 0.025); }

.cat-nome { display: flex; align-items: center; gap: 10px; }
.cat-nome b { font-size: 14px; color: hsl(var(--primary-dark)); }
.cat-icone {
    display: inline-grid; place-items: center;
    width: 30px; height: 30px;
    border-radius: var(--radius-sm);
    font-size: 13px;
    flex-shrink: 0;
}
.cat-itens { font-size: 12px; color: hsl(var(--secondary) / 0.65); font-variant-numeric: tabular-nums; }

.acoes { display: flex; gap: 4px; }

.tabela-loading, .tabela-vazio { padding: 32px; text-align: center; color: hsl(var(--secondary) / 0.6); }
.paginacao { margin-top: 4px; }

/* Drawer form */
.form-drawer {
    display: flex; flex-direction: column; gap: 16px;
    padding: 20px;
}
.paleta { display: flex; gap: 8px; flex-wrap: wrap; }
.swatch {
    width: 32px; height: 32px; border-radius: 50%;
    border: 2px solid hsl(var(--secondary) / 0.1);
    cursor: pointer; transition: transform 100ms, border-color 100ms;
}
.swatch:hover { transform: scale(1.1); }
.swatch.ativo { border-color: hsl(var(--secondary)); transform: scale(1.15); }

.icones { display: grid; grid-template-columns: repeat(6, 1fr); gap: 6px; }
.icone-btn {
    aspect-ratio: 1; border-radius: var(--radius-sm);
    border: 1px solid hsl(var(--secondary) / 0.1);
    background: var(--bg-card); color: hsl(var(--secondary) / 0.7);
    font-size: 14px; cursor: pointer;
    transition: all 100ms;
}
.icone-btn:hover { background: hsl(var(--primary) / 0.05); color: hsl(var(--primary)); }
.icone-btn.ativo {
    background: hsl(var(--primary) / 0.1);
    border-color: hsl(var(--primary) / 0.4);
    color: hsl(var(--primary));
}

.preview {
    display: flex; align-items: center; gap: 12px;
    padding: 12px;
    background: hsl(var(--secondary) / 0.03);
    border-radius: var(--radius-sm);
}
.preview-text { font-size: 14px; font-weight: 600; color: hsl(var(--primary-dark)); }

.acoes-form { display: flex; gap: 8px; justify-content: flex-end; margin-top: 8px; }
</style>
