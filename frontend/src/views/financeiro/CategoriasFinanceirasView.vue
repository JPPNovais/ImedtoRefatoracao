<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import {
    categoriaFinanceiraService,
    type CategoriaFinanceira,
    type TipoCategoria,
} from "@/services/categoriaFinanceiraService"
import {
    AppPageHeader, AppButton, AppDrawer, AppField, AppInput,
    AppSelect, AppEmptyState, AppBadge, AppModal,
} from "@/components/ui"

const categorias = ref<CategoriaFinanceira[]>([])
const carregando = ref(false)
const drawerAberto = ref(false)
const excluindoId = ref<number | null>(null)
const confirmandoExcluirId = ref<number | null>(null)
const salvando = ref(false)
const erro = ref<string | null>(null)

const modo = ref<"criar" | "editar">("criar")
const editandoId = ref<number | null>(null)

const form = ref({ nome: "", tipo: "Receita" as TipoCategoria })
const erroForm = ref<string | null>(null)

const filtroTipo = ref<"" | TipoCategoria>("")
const filtroAtivo = ref<"" | "ativas" | "inativas">("")

// Ação de inativar/reativar em andamento
const togglingId = ref<number | null>(null)

const categoriasFiltradas = computed(() => {
    let lista = categorias.value
    if (filtroTipo.value) lista = lista.filter((c) => c.tipo === filtroTipo.value)
    if (filtroAtivo.value === "ativas") lista = lista.filter((c) => c.ativo)
    else if (filtroAtivo.value === "inativas") lista = lista.filter((c) => !c.ativo)
    return lista
})

async function carregar() {
    carregando.value = true
    try {
        categorias.value = await categoriaFinanceiraService.listar()
    } catch {
        // silencioso
    } finally {
        carregando.value = false
    }
}

function abrirCriar() {
    modo.value = "criar"
    editandoId.value = null
    form.value = { nome: "", tipo: "Receita" }
    erroForm.value = null
    drawerAberto.value = true
}

function abrirEditar(cat: CategoriaFinanceira) {
    if (cat.padrao) return
    modo.value = "editar"
    editandoId.value = cat.id
    form.value = { nome: cat.nome, tipo: cat.tipo }
    erroForm.value = null
    drawerAberto.value = true
}

function fecharDrawer() {
    drawerAberto.value = false
    erroForm.value = null
}

async function salvar() {
    if (!form.value.nome.trim()) {
        erroForm.value = "Informe o nome da categoria."
        return
    }
    salvando.value = true
    erroForm.value = null
    try {
        if (modo.value === "criar") {
            await categoriaFinanceiraService.criar(form.value)
        } else if (editandoId.value !== null) {
            await categoriaFinanceiraService.atualizar(editandoId.value, form.value)
        }
        fecharDrawer()
        await carregar()
    } catch (e: any) {
        erroForm.value = e?.response?.data?.mensagem ?? "Erro ao salvar categoria."
    } finally {
        salvando.value = false
    }
}

async function excluir(id: number) {
    excluindoId.value = id
    confirmandoExcluirId.value = null
    try {
        await categoriaFinanceiraService.excluir(id)
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao excluir categoria."
        setTimeout(() => (erro.value = null), 4000)
    } finally {
        excluindoId.value = null
    }
}

async function inativar(cat: CategoriaFinanceira) {
    togglingId.value = cat.id
    try {
        await categoriaFinanceiraService.inativar(cat.id)
        cat.ativo = false
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao inativar categoria."
        setTimeout(() => (erro.value = null), 4000)
    } finally {
        togglingId.value = null
    }
}

async function reativar(cat: CategoriaFinanceira) {
    togglingId.value = cat.id
    try {
        await categoriaFinanceiraService.reativar(cat.id)
        cat.ativo = true
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao reativar categoria."
        setTimeout(() => (erro.value = null), 4000)
    } finally {
        togglingId.value = null
    }
}

onMounted(carregar)
</script>

<template>
    <main class="app-page">
        <AppPageHeader titulo="Categorias financeiras" subtitulo="Gerencie as categorias de receitas e despesas.">
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Nova categoria</AppButton>
            </template>
        </AppPageHeader>

        <div v-if="erro" class="erro-banner" role="alert">
            <i class="fa-solid fa-triangle-exclamation"></i>
            {{ erro }}
        </div>

        <!-- Filtros -->
        <div class="filtros">
            <select v-model="filtroTipo" class="filtro-select" aria-label="Filtrar por tipo">
                <option value="">Todos os tipos</option>
                <option value="Receita">Receitas</option>
                <option value="Despesa">Despesas</option>
            </select>
            <select v-model="filtroAtivo" class="filtro-select" aria-label="Filtrar por status">
                <option value="">Ativas e inativas</option>
                <option value="ativas">Somente ativas</option>
                <option value="inativas">Somente inativas</option>
            </select>
        </div>

        <div v-if="carregando" class="carregando">
            <i class="fa-solid fa-spinner fa-spin"></i>
            Carregando...
        </div>

        <AppEmptyState
            v-else-if="categoriasFiltradas.length === 0"
            icone="fa-solid fa-tags"
            titulo="Nenhuma categoria encontrada"
            descricao="Crie a primeira categoria financeira."
        >
            <template #acao>
                <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Nova categoria</AppButton>
            </template>
        </AppEmptyState>

        <table v-else class="tabela">
            <thead>
                <tr>
                    <th>Nome</th>
                    <th>Tipo</th>
                    <th>Status</th>
                    <th>Padrão</th>
                    <th class="acoes-th">Ações</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="cat in categoriasFiltradas" :key="cat.id" :class="{ 'linha-inativa': !cat.ativo }">
                    <td>{{ cat.nome }}</td>
                    <td>
                        <AppBadge
                            :variant="cat.tipo === 'Receita' ? 'success' : 'error'"
                            :label="cat.tipo"
                        />
                    </td>
                    <td>
                        <AppBadge
                            :variant="cat.ativo ? 'success' : 'muted'"
                            :label="cat.ativo ? 'Ativa' : 'Inativa'"
                        />
                    </td>
                    <td>
                        <span v-if="cat.padrao" class="tag-padrao">Padrão</span>
                    </td>
                    <td class="acoes">
                        <!-- Editar: visível apenas para não-padrão -->
                        <button
                            v-if="!cat.padrao"
                            class="btn-icon btn-icon-editar"
                            title="Editar"
                            @click="abrirEditar(cat)"
                        >
                            <i class="fa-solid fa-pen" aria-hidden="true"></i>
                        </button>
                        <button
                            v-else
                            class="btn-icon btn-icon-editar"
                            title="Categoria padrão não pode ser editada"
                            disabled
                        >
                            <i class="fa-solid fa-pen" aria-hidden="true"></i>
                        </button>

                        <!-- Inativar/Reativar: habilitado para TODOS (padrão e não-padrão) — M5 -->
                        <button
                            v-if="cat.ativo"
                            class="btn-icon btn-icon-excluir"
                            title="Inativar"
                            :disabled="togglingId === cat.id"
                            @click="inativar(cat)"
                        >
                            <i class="fa-solid fa-ban" aria-hidden="true"></i>
                        </button>
                        <button
                            v-else
                            class="btn-icon btn-icon-ver"
                            title="Reativar"
                            :disabled="togglingId === cat.id"
                            @click="reativar(cat)"
                        >
                            <i class="fa-solid fa-rotate-left" aria-hidden="true"></i>
                        </button>

                        <!-- Excluir: somente não-padrão -->
                        <button
                            v-if="!cat.padrao"
                            class="btn-icon btn-icon-excluir"
                            title="Excluir"
                            :disabled="excluindoId === cat.id"
                            @click="confirmandoExcluirId = cat.id"
                        >
                            <i class="fa-solid fa-trash" aria-hidden="true"></i>
                        </button>
                    </td>
                </tr>
            </tbody>
        </table>

        <!-- Drawer criar/editar -->
        <AppDrawer
            :aberto="drawerAberto"
            :titulo="modo === 'criar' ? 'Nova categoria' : 'Editar categoria'"
            @fechar="fecharDrawer"
        >
            <div class="form-campos">
                <AppField label="Nome" required :erro="erroForm ?? undefined" for="cat-nome">
                    <AppInput id="cat-nome" v-model="form.nome" placeholder="Ex: Consultas" />
                </AppField>

                <AppField label="Tipo" required for="cat-tipo">
                    <AppSelect id="cat-tipo" v-model="form.tipo">
                        <option value="Receita">Receita</option>
                        <option value="Despesa">Despesa</option>
                    </AppSelect>
                </AppField>
            </div>

            <template #rodape>
                <AppButton variant="secondary" @click="fecharDrawer">Cancelar</AppButton>
                <AppButton :loading="salvando" @click="salvar">
                    {{ modo === "criar" ? "Criar" : "Salvar" }}
                </AppButton>
            </template>
        </AppDrawer>

        <!-- Modal confirmar exclusão -->
        <AppModal
            :aberto="confirmandoExcluirId !== null"
            titulo="Excluir categoria?"
            largura="sm"
            :acima-de-drawer="drawerAberto"
            @fechar="confirmandoExcluirId = null"
        >
            <p>Esta ação é irreversível. Lançamentos vinculados a esta categoria não serão apagados.</p>
            <template #rodape>
                <AppButton variant="secondary" @click="confirmandoExcluirId = null">Cancelar</AppButton>
                <AppButton
                    variant="danger"
                    :loading="excluindoId !== null"
                    @click="confirmandoExcluirId && excluir(confirmandoExcluirId)"
                >
                    Excluir
                </AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.carregando {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: hsl(var(--muted-foreground));
    padding: 2rem 0;
    font-size: var(--text-sm);
}

.erro-banner {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    font-size: var(--text-sm);
    margin-bottom: 1rem;
}

.filtros {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
    margin-bottom: 1rem;
}

.filtro-select {
    padding: 0.4rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius-sm);
    font-size: var(--text-sm);
    background: hsl(var(--background));
    color: hsl(var(--foreground));
    cursor: pointer;
}

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: var(--text-sm);
}
.tabela th, .tabela td {
    padding: 0.7rem 0.9rem;
    text-align: left;
    border-bottom: 1px solid hsl(var(--border));
}
.tabela th {
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.4);
    font-size: var(--text-xs);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.tabela tbody tr:hover { background: hsl(var(--muted) / 0.3); }
.linha-inativa { opacity: 0.6; }

.acoes-th { width: 100px; }
.acoes { display: flex; gap: 0.25rem; }

.tag-padrao {
    display: inline-block;
    padding: 0.1rem 0.45rem;
    border-radius: 999px;
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    background: hsl(var(--muted));
    color: hsl(var(--muted-foreground));
}

.form-campos {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 0.25rem 0;
}
</style>
