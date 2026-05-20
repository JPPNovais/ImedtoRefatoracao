<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue"
import { inventarioService, type ItemInventario, type MovimentacaoEstoque } from "@/services/inventarioService"
import { estoqueCadastrosService, type CategoriaEstoque } from "@/services/estoqueCadastrosService"
import { useRouter } from "vue-router"
import { AppPageHeader, AppButton, AppTabs } from "@/components/ui"

const router = useRouter()
import { formatarMoedaBrl } from "@/utils/format"

import EstoqueKpis from "@/components/estoque/EstoqueKpis.vue"
import EstoqueItensTab from "@/components/estoque/EstoqueItensTab.vue"
import EstoqueMovimentacoesTab from "@/components/estoque/EstoqueMovimentacoesTab.vue"
import EstoqueComprasTab from "@/components/estoque/EstoqueComprasTab.vue"
import EstoqueAlertasTab from "@/components/estoque/EstoqueAlertasTab.vue"
import EstoqueItemDrawer from "@/components/estoque/EstoqueItemDrawer.vue"
import EstoqueMovimentacaoModal from "@/components/estoque/EstoqueMovimentacaoModal.vue"
import EstoqueCriarItemModal from "@/components/estoque/EstoqueCriarItemModal.vue"
import EstoqueEditarItemModal from "@/components/estoque/EstoqueEditarItemModal.vue"

// ─── Estado global ───────────────────────────────────────────────────────────
type TabId = "itens" | "movimentacoes" | "compras" | "alertas"
const tabAtiva = ref<TabId>("itens")

// Itens
const itens = ref<ItemInventario[]>([])
const totalItens = ref(0)
const paginaItens = ref(1)
const tamanhoItens = ref(20)
const carregandoItens = ref(false)
const erroItens = ref<string | null>(null)
const buscaItens = ref("")
const filtroStatusItens = ref("todos")
const filtroCategoriaItens = ref("")

// Movimentações
const movimentacoes = ref<MovimentacaoEstoque[]>([])
const totalMovs = ref(0)
const paginaMovs = ref(1)
const tamanhoMovs = ref(20)
const carregandoMovs = ref(false)

// Movimentações do drawer (item específico)
const movDrawer = ref<MovimentacaoEstoque[]>([])
const carregandoMovsDrawer = ref(false)

// Movimentações de hoje — calculadas a partir das movimentações já carregadas
const hoje = new Date().toISOString().split("T")[0]
const movimentacoesHoje = computed(() =>
    movimentacoes.value.filter(m => m.criadoEm.startsWith(hoje)).length
)

// KPIs derivados dos itens
const valorTotalEstoque = computed(() =>
    itens.value.reduce((s, it) => s + it.custoMedio * it.quantidadeAtual, 0)
)
const baixoCount = computed(() =>
    itens.value.filter(it => it.ativo && it.estoqueAbaixoMinimo).length
)
// Sem dado de vencimento na API atual — mostrar 0 como placeholder
const vencendoCount = computed(() => 0)

// Categorias completas (com id/cor/icone) — necessárias para os modais de criar/editar.
// Fallback para categorias derivadas dos itens enquanto a busca não retorna.
const categoriasCadastradas = ref<CategoriaEstoque[]>([])
const categorias = computed(() => categoriasCadastradas.value)
// Strings únicas para o filtro de categoria (mantém a UX existente do <select> no Itens tab).
const categoriasNomes = computed(() =>
    [...new Set(itens.value.map(i => i.categoria))].sort()
)

const subtitulo = computed(() => {
    const parts: string[] = []
    if (totalItens.value > 0) parts.push(`${totalItens.value} itens cadastrados`)
    if (valorTotalEstoque.value > 0) parts.push(`${formatarMoedaBrl(valorTotalEstoque.value)} em estoque`)
    if (movimentacoesHoje.value > 0) parts.push(`${movimentacoesHoje.value} movimentações hoje`)
    return parts.join(" · ") || "Gerencie e acompanhe o estoque da clínica"
})

const abas = computed(() => [
    { valor: "itens", label: "Itens", icone: "fa-solid fa-boxes-stacked" },
    { valor: "movimentacoes", label: "Movimentações", icone: "fa-solid fa-clock-rotate-left" },
    { valor: "compras", label: "Compras", icone: "fa-solid fa-truck-fast" },
    {
        valor: "alertas",
        label: baixoCount.value > 0 ? `Alertas (${baixoCount.value})` : "Alertas",
        icone: "fa-solid fa-triangle-exclamation",
    },
])

// ─── Drawer ──────────────────────────────────────────────────────────────────
const itemDrawer = ref<ItemInventario | null>(null)
const drawerAberto = ref(false)

async function abrirDrawer(item: ItemInventario) {
    itemDrawer.value = item
    drawerAberto.value = true
    movDrawer.value = []
    carregandoMovsDrawer.value = true
    try {
        const pg = await inventarioService.listarMovimentacoes({ itemInventarioId: item.id, pagina: 1, tamanho: 6 })
        movDrawer.value = pg.itens
    } catch {
        movDrawer.value = []
    } finally {
        carregandoMovsDrawer.value = false
    }
}

function fecharDrawer() {
    drawerAberto.value = false
    itemDrawer.value = null
}

// ─── Modal movimentação ───────────────────────────────────────────────────────
const modalMovAberto = ref(false)
const itemMovimentando = ref<ItemInventario | null>(null)
const tipoMovInicial = ref<"Entrada" | "Saida">("Entrada")
const salvandoMov = ref(false)

function abrirModalMov(item: ItemInventario, tipo: "Entrada" | "Saida" = "Entrada") {
    itemMovimentando.value = item
    tipoMovInicial.value = tipo
    modalMovAberto.value = true
}

async function confirmarMovimentacao(payload: {
    itemInventarioId: number
    tipo: "Entrada" | "Saida"
    quantidade: number
    custoUnitario?: number
    observacao?: string | null
}) {
    salvandoMov.value = true
    try {
        await inventarioService.registrarMovimentacao(payload)
        modalMovAberto.value = false
        itemMovimentando.value = null
        await Promise.all([carregarItens(), carregarMovimentacoes()])
    } catch (e: any) {
        // Propaga para o modal tratar
        throw e
    } finally {
        salvandoMov.value = false
    }
}

// ─── Modal criar item ─────────────────────────────────────────────────────────
const modalCriarAberto = ref(false)
const salvandoCriar = ref(false)

async function confirmarCriar(payload: Parameters<typeof inventarioService.criarItem>[0]) {
    salvandoCriar.value = true
    try {
        await inventarioService.criarItem(payload)
        modalCriarAberto.value = false
        await carregarItens()
    } finally {
        salvandoCriar.value = false
    }
}

// ─── Modal editar item ────────────────────────────────────────────────────────
const itemEditando = ref<ItemInventario | null>(null)
const modalEditarAberto = computed(() => !!itemEditando.value)
const salvandoEditar = ref(false)

function abrirEditar(item: ItemInventario) {
    itemEditando.value = item
    drawerAberto.value = false
}

async function confirmarEditar(payload: { nome: string; categoriaId: number; unidadeMedida: string; quantidadeMinima: number }) {
    if (!itemEditando.value) return
    salvandoEditar.value = true
    try {
        // Mantém os FKs opcionais existentes ao editar via modal simples (não os altera).
        await inventarioService.atualizarItem(itemEditando.value.id, {
            ...payload,
            fabricanteId: itemEditando.value.fabricanteId,
            fornecedorPadraoId: itemEditando.value.fornecedorPadraoId,
            localPadraoId: itemEditando.value.localPadraoId,
            custoUnitario: itemEditando.value.custoUnitario,
        })
        itemEditando.value = null
        await carregarItens()
    } finally {
        salvandoEditar.value = false
    }
}

async function carregarCategorias() {
    try {
        const pg = await estoqueCadastrosService.categorias.listar({ tamanho: 100 })
        categoriasCadastradas.value = pg.itens
    } catch {
        categoriasCadastradas.value = []
    }
}

// ─── Inativar ────────────────────────────────────────────────────────────────
async function inativar(item: ItemInventario) {
    if (!confirm(`Inativar "${item.nome}"?`)) return
    try {
        await inventarioService.inativarItem(item.id)
        // Recarrega itens E movimentações — a inativação cria uma movimentação
        // tipo "Inativacao" para auditoria, que precisa aparecer na aba sem F5.
        await Promise.all([carregarItens(), carregarMovimentacoes()])
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao inativar.")
    }
}

// ─── Carregamentos ────────────────────────────────────────────────────────────
async function carregarItens() {
    carregandoItens.value = true
    erroItens.value = null
    try {
        const params: Parameters<typeof inventarioService.listarItens>[0] = {
            apenasAtivos: true,
            pagina: paginaItens.value,
            tamanho: tamanhoItens.value,
        }
        if (filtroCategoriaItens.value) params.categoria = filtroCategoriaItens.value
        if (filtroStatusItens.value === "baixo" || filtroStatusItens.value === "atencao") {
            params.apenasAbaixoMinimo = true
        }
        const pg = await inventarioService.listarItens(params)
        itens.value = pg.itens
        totalItens.value = pg.total
    } catch (e: any) {
        erroItens.value = e?.response?.data?.mensagem ?? "Erro ao carregar itens."
    } finally {
        carregandoItens.value = false
    }
}

async function carregarMovimentacoes() {
    carregandoMovs.value = true
    try {
        const pg = await inventarioService.listarMovimentacoes({
            pagina: paginaMovs.value,
            tamanho: tamanhoMovs.value,
        })
        movimentacoes.value = pg.itens
        totalMovs.value = pg.total
    } catch {
        movimentacoes.value = []
    } finally {
        carregandoMovs.value = false
    }
}

// Watches
watch([paginaItens, tamanhoItens], carregarItens)
watch([paginaMovs, tamanhoMovs], carregarMovimentacoes)

watch(filtroStatusItens, () => { paginaItens.value = 1; carregarItens() })
watch(filtroCategoriaItens, () => { paginaItens.value = 1; carregarItens() })

// Busca — a API atual de listagem de itens não tem filtro de busca, mas
// passamos o parâmetro como futuro (a API pode suportá-lo sem quebra).
// Por ora buscaItens é filtro client-side dentro dos itens já carregados.

// Carrega ambas as abas na montagem para ter KPIs corretos
onMounted(async () => {
    await Promise.all([carregarItens(), carregarMovimentacoes(), carregarCategorias()])
})
</script>

<template>
    <div class="app-page">
        <AppPageHeader
            titulo="Estoque"
            :subtitulo="subtitulo"
        >
            <template #acoes>
                <AppButton
                    variant="ghost"
                    icon="fa-solid fa-sliders"
                    @click="router.push('/inventario/cadastros')"
                >
                    Cadastros
                </AppButton>
                <AppButton
                    variant="secondary"
                    icon="fa-solid fa-cart-plus"
                >
                    Pedido de compra
                </AppButton>
                <AppButton
                    icon="fa-solid fa-plus"
                    @click="modalCriarAberto = true"
                >
                    Novo item
                </AppButton>
            </template>
        </AppPageHeader>

        <!-- KPIs -->
        <EstoqueKpis
            :valor-total="valorTotalEstoque"
            :total-itens="totalItens"
            :baixo-count="baixoCount"
            :vencendo-count="vencendoCount"
            :movimentacoes-hoje="movimentacoesHoje"
        />

        <!-- Tabs -->
        <AppTabs
            v-model="tabAtiva"
            :abas="abas"
            variante="underline"
            aria-label="Seções do estoque"
            class="tabs-estoque"
        />

        <!-- Conteúdo das tabs -->
        <div class="tab-content">
            <!-- Itens -->
            <EstoqueItensTab
                v-if="tabAtiva === 'itens'"
                :itens="itens"
                :total="totalItens"
                :pagina="paginaItens"
                :tamanho="tamanhoItens"
                :carregando="carregandoItens"
                :categorias="categoriasNomes"
                @update:pagina="paginaItens = $event"
                @update:tamanho="tamanhoItens = $event"
                @abrir-item="abrirDrawer"
                @nova-movimentacao="abrirModalMov"
                @editar="abrirEditar"
                @inativar="inativar"
                @criar="modalCriarAberto = true"
                @busca-change="buscaItens = $event"
                @filtro-status-change="filtroStatusItens = $event"
                @filtro-categoria-change="filtroCategoriaItens = $event"
            />

            <!-- Movimentações -->
            <EstoqueMovimentacoesTab
                v-else-if="tabAtiva === 'movimentacoes'"
                :movimentacoes="movimentacoes"
                :total="totalMovs"
                :pagina="paginaMovs"
                :tamanho="tamanhoMovs"
                :carregando="carregandoMovs"
                @update:pagina="paginaMovs = $event"
                @update:tamanho="tamanhoMovs = $event"
                @busca-change="() => {}"
                @filtro-tipo-change="() => {}"
            />

            <!-- Compras -->
            <EstoqueComprasTab v-else-if="tabAtiva === 'compras'" />

            <!-- Alertas -->
            <EstoqueAlertasTab
                v-else-if="tabAtiva === 'alertas'"
                :itens="itens"
                @abrir-item="abrirDrawer"
                @nova-movimentacao="abrirModalMov"
            />
        </div>

        <!-- Drawer de detalhe -->
        <EstoqueItemDrawer
            :aberto="drawerAberto"
            :item="itemDrawer"
            :movimentacoes="movDrawer"
            :carregando-movs="carregandoMovsDrawer"
            @fechar="fecharDrawer"
            @nova-movimentacao="(item) => { fecharDrawer(); abrirModalMov(item) }"
            @editar="abrirEditar"
        />

        <!-- Modal movimentação -->
        <EstoqueMovimentacaoModal
            :aberto="modalMovAberto"
            :item-pre-selecionado="itemMovimentando"
            :tipo-inicial="tipoMovInicial"
            @fechar="modalMovAberto = false; itemMovimentando = null"
            @confirmar="confirmarMovimentacao"
        />

        <!-- Modal criar item -->
        <EstoqueCriarItemModal
            :aberto="modalCriarAberto"
            :categorias="categorias"
            @fechar="modalCriarAberto = false"
            @confirmar="confirmarCriar"
        />

        <!-- Modal editar item -->
        <EstoqueEditarItemModal
            :aberto="modalEditarAberto"
            :item="itemEditando"
            :categorias="categorias"
            @fechar="itemEditando = null"
            @confirmar="confirmarEditar"
        />
    </div>
</template>

<style scoped>
.tabs-estoque {
    margin-bottom: 16px;
}

.tab-content {
    min-height: 300px;
}
</style>
