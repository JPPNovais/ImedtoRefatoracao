<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRouter } from "vue-router"
import { AppPageHeader, AppButton, AppTabs } from "@/components/ui"
import { estoqueCadastrosService } from "@/services/estoqueCadastrosService"

import CadastroProdutosTab from "@/components/estoque/cadastros/CadastroProdutosTab.vue"
import CadastroCategoriasTab from "@/components/estoque/cadastros/CadastroCategoriasTab.vue"
import CadastroFabricantesTab from "@/components/estoque/cadastros/CadastroFabricantesTab.vue"
import CadastroFornecedoresTab from "@/components/estoque/cadastros/CadastroFornecedoresTab.vue"
import CadastroLocaisTab from "@/components/estoque/cadastros/CadastroLocaisTab.vue"

type TabId = "produtos" | "categorias" | "fabricantes" | "fornecedores" | "locais"
const tabAtiva = ref<TabId>("produtos")

const router = useRouter()

// Contagens iniciais (badges nas tabs) — carregadas em paralelo no mount.
// Só pega total via tamanho=1 para evitar pagar a paginação completa.
const contagens = ref({
    categorias: 0,
    fabricantes: 0,
    fornecedores: 0,
    locais: 0,
})

async function carregarContagens() {
    try {
        const [cat, fab, forn, loc] = await Promise.all([
            estoqueCadastrosService.categorias.listar({ tamanho: 1 }),
            estoqueCadastrosService.fabricantes.listar({ tamanho: 1 }),
            estoqueCadastrosService.fornecedores.listar({ tamanho: 1 }),
            estoqueCadastrosService.locais.listar({ tamanho: 1 }),
        ])
        contagens.value = { categorias: cat.total, fabricantes: fab.total, fornecedores: forn.total, locais: loc.total }
    } catch {
        // silencioso — tabs mostram label sem badge.
    }
}

const abas = computed(() => [
    { valor: "produtos",      label: "Produtos",      icone: "fa-solid fa-boxes-stacked" },
    { valor: "categorias",    label: contagens.value.categorias    > 0 ? `Categorias (${contagens.value.categorias})`       : "Categorias",    icone: "fa-solid fa-tags" },
    { valor: "fabricantes",   label: contagens.value.fabricantes   > 0 ? `Fabricantes (${contagens.value.fabricantes})`     : "Fabricantes",   icone: "fa-solid fa-industry" },
    { valor: "fornecedores",  label: contagens.value.fornecedores  > 0 ? `Fornecedores (${contagens.value.fornecedores})`   : "Fornecedores",  icone: "fa-solid fa-truck" },
    { valor: "locais",        label: contagens.value.locais        > 0 ? `Locais (${contagens.value.locais})`               : "Locais",        icone: "fa-solid fa-warehouse" },
])

onMounted(() => {
    carregarContagens()
})

function aoMudarContagem(tab: keyof typeof contagens.value, total: number) {
    contagens.value[tab] = total
}
</script>

<template>
    <div class="app-page">
        <AppPageHeader
            titulo="Estoque — Cadastros"
            subtitulo="Mantenha categorias, fabricantes, fornecedores e locais consistentes em todo o estoque."
        >
            <template #acoes>
                <AppButton
                    variant="ghost"
                    icon="fa-solid fa-arrow-left"
                    @click="router.push('/inventario')"
                >
                    Voltar para Estoque
                </AppButton>
                <AppButton
                    variant="secondary"
                    icon="fa-solid fa-file-import"
                    disabled
                    title="Em breve — importação via planilha"
                >
                    Importar planilha
                </AppButton>
            </template>
        </AppPageHeader>

        <AppTabs
            v-model="tabAtiva"
            :abas="abas"
            variante="underline"
            aria-label="Cadastros mestres do estoque"
            class="tabs-cadastros"
        />

        <div class="tab-content">
            <CadastroProdutosTab     v-if="tabAtiva === 'produtos'" />
            <CadastroCategoriasTab   v-else-if="tabAtiva === 'categorias'"   @total-change="(t: number) => aoMudarContagem('categorias', t)" />
            <CadastroFabricantesTab  v-else-if="tabAtiva === 'fabricantes'"  @total-change="(t: number) => aoMudarContagem('fabricantes', t)" />
            <CadastroFornecedoresTab v-else-if="tabAtiva === 'fornecedores'" @total-change="(t: number) => aoMudarContagem('fornecedores', t)" />
            <CadastroLocaisTab       v-else-if="tabAtiva === 'locais'"       @total-change="(t: number) => aoMudarContagem('locais', t)" />
        </div>
    </div>
</template>

<style scoped>
.tabs-cadastros { margin-bottom: 16px; }
.tab-content { min-height: 320px; }
</style>
