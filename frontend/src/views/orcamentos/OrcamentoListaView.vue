<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import {
    AppPageHeader, AppButton, AppSelect, AppSearchInput, AppPagination,
} from "@/components/ui"
import OrcamentoKpis   from "@/components/orcamento/OrcamentoKpis.vue"
import OrcamentoTabela from "@/components/orcamento/OrcamentoTabela.vue"
import { orcamentoService, type OrcamentoResumo, type OrcamentoStatus } from "@/services/orcamentoService"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { usePermissoesStore } from "@/stores/permissoesStore"

const router = useRouter()
const route = useRoute()
const permissoes = usePermissoesStore()
const podeConfigurar = computed(() => permissoes.pode("orcamento.configurar"))

// ── Estado
const orcamentos = ref<OrcamentoResumo[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)

// ── Filtros (4 abas, sem a tab "Quitados" que tinha statuses: [])
type TabKey = "todos" | "pendentes" | "aprovados" | "perdidos"
const tab = ref<TabKey>("todos")

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)

const statusFiltro = ref<OrcamentoStatus | "">("")

type Ordenacao = "recente" | "valor_desc" | "valor_asc" | "vencendo"
const ordenacaoStr = ref<string>("recente")
const ordenacao = computed(() => ordenacaoStr.value as Ordenacao)

const pagina = ref(1)
const tamanho = ref(10)

const TABS: { valor: TabKey; label: string; statuses: OrcamentoStatus[] }[] = [
    { valor: "todos",     label: "Todos",     statuses: [] },
    { valor: "pendentes", label: "Pendentes", statuses: ["Rascunho", "Enviado"] },
    { valor: "aprovados", label: "Aprovados", statuses: ["Aprovado"] },
    { valor: "perdidos",  label: "Perdidos",  statuses: ["Recusado", "Expirado", "Cancelado"] },
]

const ORDENACAO_OPTIONS: { value: Ordenacao; label: string }[] = [
    { value: "recente",    label: "Mais recentes" },
    { value: "valor_desc", label: "Maior valor" },
    { value: "valor_asc",  label: "Menor valor" },
    { value: "vencendo",   label: "Vencendo antes" },
]

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        orcamentos.value = await orcamentoService.listar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar orçamentos."
    } finally {
        carregando.value = false
    }
}

const contagemTab = computed(() => {
    const list = orcamentos.value
    return {
        todos:     list.length,
        pendentes: list.filter(o => ["Rascunho", "Enviado"].includes(o.status)).length,
        aprovados: list.filter(o => o.status === "Aprovado").length,
        perdidos:  list.filter(o => ["Recusado", "Expirado", "Cancelado"].includes(o.status)).length,
    }
})

const filtrados = computed(() => {
    let arr = [...orcamentos.value]
    const t = TABS.find(x => x.valor === tab.value)
    if (t && t.statuses.length > 0) {
        arr = arr.filter(o => t.statuses.includes(o.status))
    }
    if (statusFiltro.value) {
        arr = arr.filter(o => o.status === statusFiltro.value)
    }
    if (busca.value.trim()) {
        const q = busca.value.toLowerCase()
        arr = arr.filter(o =>
            o.pacienteNome.toLowerCase().includes(q) ||
            (o.numero && o.numero.toLowerCase().includes(q)) ||
            o.criadoPorNome.toLowerCase().includes(q)
        )
    }
    switch (ordenacao.value) {
        case "recente":    arr.sort((a, b) => b.criadoEm.localeCompare(a.criadoEm)); break
        case "valor_desc": arr.sort((a, b) => b.total - a.total); break
        case "valor_asc":  arr.sort((a, b) => a.total - b.total); break
        case "vencendo":   arr.sort((a, b) => a.validade.localeCompare(b.validade)); break
    }
    return arr
})

const totalFiltrado = computed(() => filtrados.value.length)
const paginados = computed(() => {
    const inicio = (pagina.value - 1) * tamanho.value
    return filtrados.value.slice(inicio, inicio + tamanho.value)
})

watch([tab, statusFiltro], () => { pagina.value = 1 })
watch(busca, () => { pagina.value = 1 })
watch(ordenacaoStr, () => { pagina.value = 1 })

function trocarTab(t: TabKey) {
    tab.value = t
    statusFiltro.value = ""
}

function abrirOrcamento(o: OrcamentoResumo) {
    router.push({ name: "OrcamentoDetalhe", params: { id: String(o.id) } })
}

/**
 * Navega para o form vazio em `/orcamentos/novo`. NÃO cria registro no banco —
 * persistência só acontece no submit do formulário (CA-1).
 */
function abrirNovoOrcamento() {
    router.push({ name: "OrcamentoNovo" })
}

// Deep-link CA6: ?status=pendentes → tab Pendentes (Rascunho + Enviado).
onMounted(async () => {
    if (route.query.status === "pendentes") {
        tab.value = "pendentes"
    }
    await carregar()
})
</script>

<template>
    <div class="app-page app-page--wide">
        <AppPageHeader
            titulo="Orçamentos"
            subtitulo="Crie, envie e acompanhe orçamentos de procedimentos"
        >
            <template #acoes>
                <AppButton
                    v-if="podeConfigurar"
                    variant="ghost"
                    icon="fa-solid fa-sliders"
                    @click="router.push('/configuracoes/orcamento')"
                >
                    Configurações
                </AppButton>
                <AppButton variant="secondary" icon="fa-solid fa-file-export">Exportar</AppButton>
                <AppButton v-if="podeConfigurar" icon="fa-solid fa-plus" @click="abrirNovoOrcamento">Novo orçamento</AppButton>
            </template>
        </AppPageHeader>

        <OrcamentoKpis :orcamentos="orcamentos" />

        <div v-if="erro" class="erro-banner" role="alert">
            {{ erro }}
            <AppButton size="sm" variant="ghost" @click="carregar">Tentar novamente</AppButton>
        </div>

        <div class="toolbar">
            <div class="tabs-wrap">
                <button
                    v-for="t in TABS"
                    :key="t.valor"
                    class="tab-btn"
                    :class="{ active: tab === t.valor }"
                    type="button"
                    @click="trocarTab(t.valor)"
                >
                    {{ t.label }}
                    <span class="tab-count">{{ contagemTab[t.valor] }}</span>
                </button>
            </div>

            <div class="toolbar-end">
                <AppSearchInput
                    v-model="buscaInput"
                    placeholder="Buscar por paciente, número ou criado por..."
                />
                <AppSelect v-model="ordenacaoStr" class="select-sm">
                    <option v-for="opt in ORDENACAO_OPTIONS" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                </AppSelect>
            </div>
        </div>

        <OrcamentoTabela
            :orcamentos="paginados"
            :carregando="carregando"
            @abrir="abrirOrcamento"
        />

        <AppPagination
            v-if="totalFiltrado > tamanho"
            v-model:pagina="pagina"
            v-model:tamanho="tamanho"
            :total="totalFiltrado"
        />
    </div>
</template>

<style scoped>
.erro-banner {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    font-size: 0.875rem;
}

.toolbar {
    display: flex;
    gap: 14px;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
}

.tabs-wrap {
    display: flex;
    gap: 4px;
    background: hsl(var(--secondary) / 0.05);
    padding: 4px;
    border-radius: 10px;
}

.tab-btn {
    border: none;
    background: transparent;
    padding: 8px 14px;
    border-radius: 7px;
    font-size: 13px;
    font-weight: 500;
    color: hsl(var(--secondary) / 0.7);
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    transition: all 0.12s;
}
.tab-btn:hover { color: hsl(var(--secondary)); }
.tab-btn.active {
    background: hsl(var(--card));
    color: hsl(var(--primary));
    box-shadow: 0 1px 3px hsl(var(--secondary) / 0.1);
}

.tab-count {
    font-size: 11px;
    padding: 1px 7px;
    border-radius: 10px;
    background: hsl(var(--secondary) / 0.08);
    color: hsl(var(--secondary) / 0.7);
}
.tab-btn.active .tab-count {
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
}

.toolbar-end {
    display: flex;
    gap: 8px;
    align-items: center;
    flex: 1;
    justify-content: flex-end;
    min-width: 0;
}

.select-sm { min-width: 160px; }

@media (max-width: 900px) {
    .toolbar { flex-direction: column; align-items: stretch; }
    .toolbar-end { flex-direction: column; }
    .tabs-wrap { overflow-x: auto; }
}
</style>
