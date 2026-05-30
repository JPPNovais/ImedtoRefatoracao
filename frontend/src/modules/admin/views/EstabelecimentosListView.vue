<script setup lang="ts">
/**
 * EstabelecimentosListView — lista paginada de estabelecimentos para o admin global.
 *
 * CA21: estado vazio quando não há resultados.
 * CA23: estado de falha de rede com mensagem + retry.
 * CA24–CA26: paginação 25/pg, busca debounced 300ms.
 * CA47/CA48: isolamento — sem imports do app principal.
 */
import { ref, watch, onMounted } from "vue"
import { useRouter } from "vue-router"
import { useEstabelecimentosStore } from "../stores/estabelecimentosStore"
import BadgeStatusEstabelecimento from "../components/estabelecimentos/BadgeStatusEstabelecimento.vue"

const router = useRouter()
const store = useEstabelecimentosStore()

const busca = ref("")
const statusFiltro = ref("")

// Debounce manual de 300ms para busca (CA24).
let debounceTimer: ReturnType<typeof setTimeout> | null = null

watch([busca, statusFiltro], () => {
    if (debounceTimer) clearTimeout(debounceTimer)
    debounceTimer = setTimeout(() => {
        store.pagina = 1
        recarregar()
    }, 300)
})

watch([() => store.pagina, () => store.tamanhoPagina], () => {
    recarregar()
})

function recarregar() {
    store.carregarLista({ busca: busca.value || undefined, status: statusFiltro.value || undefined })
}

onMounted(() => recarregar())

function irParaDetalhe(id: number) {
    router.push({ name: "AdminEstabelecimentoDetalhe", params: { id: String(id) } })
}

function formatarData(iso: string): string {
    return new Date(iso).toLocaleDateString("pt-BR")
}
</script>

<template>
    <div class="admin-page">
        <div class="admin-page-header">
            <h1 class="admin-page-titulo">Estabelecimentos</h1>
            <p class="admin-page-subtitulo">{{ store.total }} encontrado(s)</p>
        </div>

        <!-- Filtros -->
        <div class="filtros-bar">
            <input
                v-model="busca"
                type="search"
                class="filtro-busca"
                placeholder="Buscar por nome fantasia..."
                aria-label="Buscar estabelecimento"
            />
            <select v-model="statusFiltro" class="filtro-select" aria-label="Filtrar por status">
                <option value="">Todos os status</option>
                <option value="Ativo">Ativo</option>
                <option value="Inativo">Inativo</option>
                <option value="Suspenso">Suspenso</option>
            </select>
        </div>

        <!-- Loading -->
        <div v-if="store.carregandoLista" class="estado-loading" aria-live="polite">
            Carregando...
        </div>

        <!-- Erro de rede (CA23) -->
        <div v-else-if="store.erroLista" class="estado-erro" role="alert">
            <p>{{ store.erroLista }}</p>
            <button class="btn-retry" type="button" @click="recarregar">Tentar novamente</button>
        </div>

        <!-- Sem resultados (CA21) -->
        <div v-else-if="!store.carregandoLista && store.itens.length === 0" class="estado-vazio">
            <p>Nenhum estabelecimento encontrado.</p>
        </div>

        <!-- Tabela -->
        <div v-else class="tabela-container">
            <table class="tabela">
                <thead>
                    <tr>
                        <th>Nome fantasia</th>
                        <th>Dono</th>
                        <th>CPF (mascarado)</th>
                        <th>Plano</th>
                        <th>Status</th>
                        <th>Profissionais</th>
                        <th>Pacientes</th>
                        <th>Criado em</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="item in store.itens" :key="item.id">
                        <td class="td-nome">{{ item.nomeFantasia }}</td>
                        <td>
                            <div>{{ item.donoNome }}</div>
                            <div class="td-email">{{ item.donoEmail }}</div>
                        </td>
                        <td class="td-cpf">{{ item.donoCpfMascarado }}</td>
                        <td>{{ item.planoNome }}</td>
                        <td>
                            <BadgeStatusEstabelecimento :status="item.status" />
                        </td>
                        <td class="td-num">{{ item.totalProfissionaisAtivos }}</td>
                        <td class="td-num">{{ item.totalPacientes }}</td>
                        <td>{{ formatarData(item.criadoEm) }}</td>
                        <td>
                            <button
                                class="btn-icon btn-icon-ver"
                                type="button"
                                :aria-label="`Ver detalhe de ${item.nomeFantasia}`"
                                @click="irParaDetalhe(item.id)"
                                title="Ver detalhe"
                            />
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Paginação simples -->
        <div v-if="store.total > store.tamanhoPagina" class="paginacao">
            <button
                class="pag-btn"
                type="button"
                :disabled="store.pagina <= 1"
                @click="store.pagina--"
            >
                &lsaquo; Anterior
            </button>
            <span class="pag-info">
                Página {{ store.pagina }} de {{ Math.ceil(store.total / store.tamanhoPagina) }}
            </span>
            <button
                class="pag-btn"
                type="button"
                :disabled="store.pagina >= Math.ceil(store.total / store.tamanhoPagina)"
                @click="store.pagina++"
            >
                Próxima &rsaquo;
            </button>
        </div>
    </div>
</template>

<style scoped>
.admin-page { padding: 24px 32px; }
.admin-page-header { margin-bottom: 20px; }
.admin-page-titulo { font-size: 22px; font-weight: 700; margin: 0 0 4px; }
.admin-page-subtitulo { font-size: 13px; color: hsl(var(--muted-foreground)); margin: 0; }

.filtros-bar { display: flex; gap: 12px; margin-bottom: 20px; }
.filtro-busca {
    flex: 1; max-width: 380px;
    padding: 8px 12px; border: 1px solid hsl(var(--border)); border-radius: 6px; font-size: 13px;
}
.filtro-select {
    padding: 8px 12px; border: 1px solid hsl(var(--border)); border-radius: 6px; font-size: 13px;
}

.estado-loading, .estado-erro, .estado-vazio {
    padding: 48px; text-align: center; color: hsl(var(--muted-foreground)); font-size: 14px;
}
.estado-erro { color: hsl(var(--destructive)); }
.btn-retry {
    margin-top: 12px; padding: 8px 16px;
    border: 1px solid hsl(var(--border)); border-radius: 6px; font-size: 13px;
    cursor: pointer; background: hsl(var(--card));
}

.tabela-container { overflow-x: auto; }
.tabela { width: 100%; border-collapse: collapse; font-size: 13px; }
.tabela th {
    padding: 10px 12px; border-bottom: 2px solid hsl(var(--border));
    text-align: left; font-weight: 600; color: hsl(var(--foreground));
}
.tabela td { padding: 10px 12px; border-bottom: 1px solid hsl(var(--muted) / 0.4); }
.tabela tr:hover td { background: hsl(var(--muted) / 0.3); }
.td-nome { font-weight: 600; }
.td-email { font-size: 12px; color: hsl(var(--muted-foreground)); }
.td-cpf { font-family: monospace; font-size: 12px; }
.td-num { text-align: center; }

.paginacao {
    display: flex; align-items: center; justify-content: center; gap: 16px;
    margin-top: 20px; padding-top: 16px; border-top: 1px solid hsl(var(--muted) / 0.4);
}
.pag-btn {
    padding: 6px 14px; border: 1px solid hsl(var(--border)); border-radius: 6px;
    font-size: 13px; cursor: pointer; background: hsl(var(--card));
}
.pag-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.pag-info { font-size: 13px; color: hsl(var(--muted-foreground)); }
</style>
