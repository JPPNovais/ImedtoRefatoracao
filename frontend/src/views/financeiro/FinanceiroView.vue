<script setup lang="ts">
import { ref, computed, defineAsyncComponent } from "vue"
import { AppPageHeader } from "@/components/ui"
import { usePermissoesStore } from "@/stores/permissoesStore"

// Lazy loading por aba — CA188: aba não clicada não dispara consulta nem carrega JS.
const ExtratoTab = defineAsyncComponent(() => import("./tabs/ExtratoTab.vue"))
const CaixaTab = defineAsyncComponent(() => import("./tabs/CaixaTab.vue"))
const ComissoesTab = defineAsyncComponent(() => import("./tabs/ComissoesTab.vue"))
const ConfigTab = defineAsyncComponent(() => import("./tabs/FinanceiroConfigTab.vue"))

const permissoesStore = usePermissoesStore()
const ehDono = computed(() => permissoesStore.ehDono)

type Aba = "extrato" | "caixa" | "comissoes" | "config"
const abaAtiva = ref<Aba>("extrato")

// Memoize: só instancia cada aba quando visitada pela primeira vez.
const abasCarregadas = ref<Set<Aba>>(new Set<Aba>(["extrato"]))

function selecionarAba(aba: Aba) {
    abaAtiva.value = aba
    abasCarregadas.value.add(aba)
}

const abas: { id: Aba; rotulo: string; icone: string; soDono?: boolean }[] = [
    { id: "extrato",   rotulo: "Extrato",     icone: "fa-solid fa-list"        },
    { id: "caixa",     rotulo: "Caixa diário", icone: "fa-solid fa-cash-register" },
    { id: "comissoes", rotulo: "Comissões",    icone: "fa-solid fa-percent"     },
    { id: "config",    rotulo: "Configurações", icone: "fa-solid fa-gear"       },
]
</script>

<template>
    <main class="app-page financeiro-v2">
        <AppPageHeader
            titulo="Financeiro"
            subtitulo="Extrato, caixa diário, comissões e configurações."
        />

        <!-- Barra de abas -->
        <nav class="financeiro-tabs" role="tablist" aria-label="Abas do financeiro">
            <button
                v-for="aba in abas"
                :key="aba.id"
                role="tab"
                :aria-selected="abaAtiva === aba.id"
                :aria-controls="`painel-${aba.id}`"
                :id="`aba-${aba.id}`"
                :class="['tab-btn', { ativo: abaAtiva === aba.id }]"
                @click="selecionarAba(aba.id)"
            >
                <i :class="aba.icone" aria-hidden="true" />
                {{ aba.rotulo }}
            </button>
        </nav>

        <!-- Painéis (montados apenas quando visitados — CA188) -->
        <div
            v-for="aba in abas"
            :key="aba.id"
            :id="`painel-${aba.id}`"
            role="tabpanel"
            :aria-labelledby="`aba-${aba.id}`"
            :hidden="abaAtiva !== aba.id"
        >
            <template v-if="abasCarregadas.has(aba.id)">
                <Suspense>
                    <ExtratoTab   v-if="aba.id === 'extrato'"   />
                    <CaixaTab     v-else-if="aba.id === 'caixa'"     :eh-dono="ehDono" />
                    <ComissoesTab v-else-if="aba.id === 'comissoes'" :eh-dono="ehDono" />
                    <ConfigTab    v-else-if="aba.id === 'config'"    :eh-dono="ehDono" />
                    <template #fallback>
                        <p class="carregando-aba">Carregando...</p>
                    </template>
                </Suspense>
            </template>
        </div>
    </main>
</template>

<style scoped>
.financeiro-tabs {
    display: flex;
    gap: 0;
    border-bottom: 2px solid hsl(var(--border));
    margin-bottom: 1.5rem;
    overflow-x: auto;
}

.tab-btn {
    display: flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.65rem 1.1rem;
    border: none;
    border-bottom: 2px solid transparent;
    background: transparent;
    cursor: pointer;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    color: hsl(var(--muted-foreground));
    white-space: nowrap;
    margin-bottom: -2px;
    transition: color 0.15s, border-color 0.15s;
}

.tab-btn.ativo {
    color: hsl(var(--primary));
    border-bottom-color: hsl(var(--primary));
}

.tab-btn:hover:not(.ativo) {
    color: hsl(var(--foreground));
}

.carregando-aba {
    color: hsl(var(--muted-foreground));
    font-size: var(--text-sm);
    padding: 1rem 0;
}
</style>
