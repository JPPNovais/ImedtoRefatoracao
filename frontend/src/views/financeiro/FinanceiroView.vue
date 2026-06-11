<script setup lang="ts">
import { ref, computed, defineAsyncComponent } from "vue"
import { AppPageHeader, AppButton } from "@/components/ui"
import { usePermissoesStore } from "@/stores/permissoesStore"
import { useTenantStore } from "@/stores/tenantStore"

// Lazy loading por aba — CA188: aba não clicada não dispara consulta nem carrega JS.
const VisaoGeralTab = defineAsyncComponent(() => import("./tabs/VisaoGeralTab.vue"))
const CaixaTab      = defineAsyncComponent(() => import("./tabs/CaixaTab.vue"))
const ComissoesTab  = defineAsyncComponent(() => import("./tabs/ComissoesTab.vue"))
const ConfigTab     = defineAsyncComponent(() => import("./tabs/FinanceiroConfigTab.vue"))

const permissoesStore = usePermissoesStore()
const tenantStore     = useTenantStore()
const ehDono = computed(() => permissoesStore.ehDono)

type Aba = "visao-geral" | "caixa" | "comissoes" | "config"
const abaAtiva = ref<Aba>("visao-geral")

// Memoize: só instancia cada aba quando visitada pela primeira vez.
const abasCarregadas = ref<Set<Aba>>(new Set<Aba>(["visao-geral"]))

function selecionarAba(aba: Aba) {
    abaAtiva.value = aba
    abasCarregadas.value.add(aba)
}

const abas: { id: Aba; rotulo: string; icone: string }[] = [
    { id: "visao-geral", rotulo: "Visão geral",  icone: "fa-solid fa-chart-line"     },
    { id: "caixa",       rotulo: "Caixa diário", icone: "fa-solid fa-cash-register"  },
    { id: "comissoes",   rotulo: "Comissões",     icone: "fa-solid fa-percent"        },
    { id: "config",      rotulo: "Configurações", icone: "fa-solid fa-gear"           },
]

// Subtítulo do header: nome do estabelecimento + aviso de dado restrito (CA16)
const subtitulo = computed(() =>
    tenantStore.ativo?.nomeFantasia
        ? `${tenantStore.ativo.nomeFantasia} · Dados restritos a esta unidade`
        : "Dados restritos a esta unidade"
)

// Referência para a aba Visão Geral (exportar)
const visaoGeralRef = ref<InstanceType<typeof VisaoGeralTab> | null>(null)

function exportarDoHeader() {
    // Exportar é responsabilidade da VisaoGeralTab.
    // Se a aba ativa não for visão-geral, muda para ela e aguarda a exportação.
    if (abaAtiva.value !== "visao-geral") {
        selecionarAba("visao-geral")
        // O botão Exportar na VisaoGeralTab estará visível ao usuário.
        return
    }
    visaoGeralRef.value?.exportar?.()
}

// Modal lançamento: controlado aqui para permitir abertura do header
const modalLancamento = ref(false)

function abrirModalLancamento() {
    if (abaAtiva.value !== "visao-geral") {
        selecionarAba("visao-geral")
    }
    modalLancamento.value = true
}
</script>

<template>
    <main class="app-page">
        <AppPageHeader
            titulo="Financeiro"
            :subtitulo="subtitulo"
        >
            <template #acoes>
                <AppButton
                    variant="secondary"
                    icon="fa-solid fa-download"
                    @click="exportarDoHeader"
                >
                    Exportar
                </AppButton>
                <AppButton
                    icon="fa-solid fa-plus"
                    @click="abrirModalLancamento"
                >
                    Lançamento
                </AppButton>
            </template>
        </AppPageHeader>

        <!-- Barra de abas estilo underline com ícone -->
        <nav class="cf-tabs" role="tablist" aria-label="Abas do financeiro">
            <button
                v-for="aba in abas"
                :key="aba.id"
                role="tab"
                :aria-selected="abaAtiva === aba.id"
                :aria-controls="`painel-${aba.id}`"
                :id="`aba-${aba.id}`"
                class="cf-tab"
                :class="{ ativo: abaAtiva === aba.id }"
                @click="selecionarAba(aba.id)"
            >
                <i :class="aba.icone" aria-hidden="true" />
                {{ aba.rotulo }}
            </button>
        </nav>

        <!-- Painéis — um elemento por aba (sem v-for) para que os refs funcionem
             corretamente em Vue 3. Lazy load preservado via abasCarregadas + defineAsyncComponent. -->
        <div
            id="painel-visao-geral"
            role="tabpanel"
            aria-labelledby="aba-visao-geral"
            :hidden="abaAtiva !== 'visao-geral'"
        >
            <Suspense v-if="abasCarregadas.has('visao-geral')">
                <VisaoGeralTab
                    ref="visaoGeralRef"
                    :modal-aberto-externo="modalLancamento"
                    @update:modal-aberto-externo="modalLancamento = $event"
                />
                <template #fallback>
                    <p class="carregando-aba">Carregando...</p>
                </template>
            </Suspense>
        </div>

        <div
            id="painel-caixa"
            role="tabpanel"
            aria-labelledby="aba-caixa"
            :hidden="abaAtiva !== 'caixa'"
        >
            <Suspense v-if="abasCarregadas.has('caixa')">
                <CaixaTab :eh-dono="ehDono" />
                <template #fallback>
                    <p class="carregando-aba">Carregando...</p>
                </template>
            </Suspense>
        </div>

        <div
            id="painel-comissoes"
            role="tabpanel"
            aria-labelledby="aba-comissoes"
            :hidden="abaAtiva !== 'comissoes'"
        >
            <Suspense v-if="abasCarregadas.has('comissoes')">
                <ComissoesTab :eh-dono="ehDono" />
                <template #fallback>
                    <p class="carregando-aba">Carregando...</p>
                </template>
            </Suspense>
        </div>

        <div
            id="painel-config"
            role="tabpanel"
            aria-labelledby="aba-config"
            :hidden="abaAtiva !== 'config'"
        >
            <Suspense v-if="abasCarregadas.has('config')">
                <ConfigTab :eh-dono="ehDono" />
                <template #fallback>
                    <p class="carregando-aba">Carregando...</p>
                </template>
            </Suspense>
        </div>
    </main>
</template>

<style scoped>
/* Barra de abas estilo underline com ícone */
.cf-tabs {
    display: flex;
    gap: 0;
    border-bottom: 2px solid hsl(var(--secondary) / 0.1);
    margin-bottom: 20px;
    overflow-x: auto;
}

.cf-tab {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 10px 16px;
    border: none;
    border-bottom: 2px solid transparent;
    background: transparent;
    cursor: pointer;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    color: hsl(var(--secondary) / 0.6);
    white-space: nowrap;
    margin-bottom: -2px;
    transition: color 0.15s, border-color 0.15s;
}
.cf-tab i { font-size: var(--text-sm); }

.cf-tab.ativo {
    color: hsl(var(--primary));
    border-bottom-color: hsl(var(--primary));
    font-weight: var(--font-weight-semibold);
}

.cf-tab:hover:not(.ativo) {
    color: var(--c-primary-dark);
    background: hsl(var(--secondary) / 0.03);
}

.carregando-aba {
    color: hsl(var(--secondary) / 0.6);
    font-size: var(--text-sm);
    padding: 1rem 0;
}
</style>
