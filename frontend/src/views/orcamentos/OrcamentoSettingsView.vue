<script setup lang="ts">
/**
 * OrcamentoSettingsView — rewrite 2026-05-16 para o design ConfigOrcamento.
 * 6 abas (5 do design + 1 transitória "Outras configurações"), lazy-load por aba,
 * querystring `?aba=` para deep-link.
 *
 * Cada aba é um componente independente em `components/orcamento/config/*Tab.vue`.
 */
import { ref, computed, onMounted, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { AppPageHeader, AppTabs, AppButton, AppToast } from "@/components/ui"
import ProcedimentosTab from "@/components/orcamento/config/ProcedimentosTab.vue"
import ProdutosTab from "@/components/orcamento/config/ProdutosTab.vue"
import EquipeTab from "@/components/orcamento/config/EquipeTab.vue"
import ValoresProfissionalTab from "@/components/orcamento/config/ValoresProfissionalTab.vue"
import AnestesistasTab from "@/components/orcamento/config/AnestesistasTab.vue"
import PacotesTab from "@/components/orcamento/config/PacotesTab.vue"
import OutrasConfigsTab from "@/components/orcamento/config/OutrasConfigsTab.vue"

type AbaKey = "procedimentos" | "produtos" | "equipe" | "valores-profissional" | "anestesistas" | "pacotes" | "outras"

const route = useRoute()
const router = useRouter()

const aba = ref<AbaKey>(((route.query.aba as AbaKey) ?? "procedimentos"))
const visitadas = ref<Set<AbaKey>>(new Set([aba.value]))

// Contagens de cada aba (vindo dos componentes) — usadas no badge.
const contagens = ref<Record<string, number>>({})

const abas = computed(() => [
    { valor: "procedimentos", label: `Procedimentos${contagens.value.procedimentos != null ? ` (${contagens.value.procedimentos})` : ""}`, icone: "fa-solid fa-scalpel" },
    { valor: "produtos",      label: `Produtos${contagens.value.produtos != null ? ` (${contagens.value.produtos})` : ""}`,                icone: "fa-solid fa-boxes-stacked" },
    { valor: "equipe",              label: `Equipe${contagens.value.equipe != null ? ` (${contagens.value.equipe})` : ""}`,                                                 icone: "fa-solid fa-users" },
    { valor: "valores-profissional", label: `Valores profissional${contagens.value["valores-profissional"] != null ? ` (${contagens.value["valores-profissional"]})` : ""}`, icone: "fa-solid fa-user-clock" },
    { valor: "anestesistas",        label: `Anestesistas${contagens.value.anestesistas != null ? ` (${contagens.value.anestesistas})` : ""}`,                               icone: "fa-solid fa-user-doctor" },
    { valor: "pacotes",       label: `Pacotes${contagens.value.pacotes != null ? ` (${contagens.value.pacotes})` : ""}`,                    icone: "fa-solid fa-box-open" },
    { valor: "outras",        label: "Outras configurações",                                                                                icone: "fa-solid fa-sliders" },
])

function trocarAba(v: AbaKey) {
    aba.value = v
    visitadas.value.add(v)
    router.replace({ query: { ...route.query, aba: v } })
}

watch(() => route.query.aba, (q) => {
    if (q && q !== aba.value) {
        aba.value = q as AbaKey
        visitadas.value.add(aba.value)
    }
})

function setContagem(chave: string, n: number) {
    contagens.value = { ...contagens.value, [chave]: n }
}

const toast = ref<{ visivel: boolean; texto: string }>({ visivel: false, texto: "" })
function mostrarBreve(texto: string) {
    toast.value = { visivel: true, texto }
    setTimeout(() => { toast.value.visivel = false }, 2200)
}
</script>

<template>
    <div class="app-page app-page--wide">
        <AppPageHeader
            titulo="Configurações de orçamento"
            subtitulo="Procedimentos, produtos, equipe, anestesistas e pacotes que alimentam os orçamentos do estabelecimento."
        >
            <template #acoes>
                <AppButton variant="secondary" icon="fa-solid fa-file-import" @click="mostrarBreve('Importação de planilha em breve.')">Importar planilha</AppButton>
                <AppButton variant="secondary" icon="fa-solid fa-file-export" @click="mostrarBreve('Exportação em breve.')">Exportar</AppButton>
            </template>
        </AppPageHeader>

        <AppTabs :model-value="aba" :abas="abas" variante="underline" @update:model-value="(v: any) => trocarAba(v as AbaKey)" />

        <div class="tab-content">
            <ProcedimentosTab v-if="aba === 'procedimentos'" @contagem="(n) => setContagem('procedimentos', n)" />
            <ProdutosTab      v-else-if="aba === 'produtos'"      @contagem="(n) => setContagem('produtos', n)" />
            <EquipeTab               v-else-if="aba === 'equipe'"               @contagem="(n) => setContagem('equipe', n)" />
            <ValoresProfissionalTab  v-else-if="aba === 'valores-profissional'"  @contagem="(n) => setContagem('valores-profissional', n)" />
            <AnestesistasTab         v-else-if="aba === 'anestesistas'"          @contagem="(n) => setContagem('anestesistas', n)" />
            <PacotesTab       v-else-if="aba === 'pacotes'"       @contagem="(n) => setContagem('pacotes', n)" />
            <OutrasConfigsTab v-else-if="aba === 'outras'" />
        </div>

        <AppToast v-if="toast.visivel" :mensagem="toast.texto" tipo="info" />
    </div>
</template>

<style scoped>
.tab-content { margin-top: 16px; }
</style>
