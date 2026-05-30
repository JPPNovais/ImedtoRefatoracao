<script setup lang="ts">
/**
 * EstabelecimentosListView — lista paginada de estabelecimentos para o admin global.
 *
 * W3-CA7 a W3-CA15: app-page + AppPageHeader + AppCard + AppSearchInput + AppSelect
 *                    + AppPagination + AppEmptyState + AppButton + AppStatusPill.
 * CA47/CA48: isolamento — sem imports do app principal.
 * Debounce via useDebouncedRef (substituindo setTimeout manual).
 */
import { ref, watch } from "vue"
import { useRouter } from "vue-router"
import {
    AppPageHeader, AppCard, AppSearchInput, AppEmptyState,
    AppPagination, AppButton, AppStatusPill,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { useEstabelecimentosStore } from "../stores/estabelecimentosStore"

const router = useRouter()
const store = useEstabelecimentosStore()

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput, 300)
const statusFiltro = ref("")

watch([busca, statusFiltro], () => {
    store.pagina = 1
    recarregar()
})

watch([() => store.pagina, () => store.tamanhoPagina], () => {
    recarregar()
})

function recarregar() {
    store.carregarLista({ busca: busca.value || undefined, status: statusFiltro.value || undefined })
}

recarregar()

function irParaDetalhe(id: number) {
    router.push({ name: "AdminEstabelecimentoDetalhe", params: { id: String(id) } })
}

function formatarData(iso: string): string {
    return new Date(iso).toLocaleDateString("pt-BR")
}

function statusVariante(status: string): "success" | "error" | "warning" | "muted" {
    if (status === "Ativo") return "success"
    if (status === "Suspenso") return "warning"
    return "muted"
}
</script>

<template>
    <main class="app-page">
        <AppPageHeader titulo="Estabelecimentos" :subtitulo="`${store.total} encontrado(s)`" />

        <AppCard>
            <!-- Filtros -->
            <div class="filtros-row">
                <AppSearchInput v-model="buscaInput" placeholder="Buscar por nome fantasia..." style="max-width:360px;" />
                <select v-model="statusFiltro" class="select-status" aria-label="Filtrar por status">
                    <option value="">Todos os status</option>
                    <option value="Ativo">Ativo</option>
                    <option value="Inativo">Inativo</option>
                    <option value="Suspenso">Suspenso</option>
                </select>
            </div>

            <!-- Loading -->
            <p v-if="store.carregandoLista" class="estado-info" aria-live="polite">
                <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
            </p>

            <!-- Erro de rede -->
            <p v-else-if="store.erroLista" class="estado-erro" role="alert">
                {{ store.erroLista }}
                <AppButton variant="ghost" size="sm" @click="recarregar">Tentar novamente</AppButton>
            </p>

            <!-- Vazio -->
            <AppEmptyState
                v-else-if="store.itens.length === 0"
                titulo="Nenhum estabelecimento encontrado."
                descricao="Ajuste os filtros ou aguarde novos cadastros."
            />

            <!-- Tabela -->
            <template v-else>
                <div class="tabela-wrap">
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
                                    <div class="td-muted">{{ item.donoEmail }}</div>
                                </td>
                                <td class="td-mono">{{ item.donoCpfMascarado }}</td>
                                <td>{{ item.planoNome }}</td>
                                <td>
                                    <AppStatusPill :label="item.status" :variante="statusVariante(item.status)" />
                                </td>
                                <td class="td-num">{{ item.totalProfissionaisAtivos }}</td>
                                <td class="td-num">{{ item.totalPacientes }}</td>
                                <td>{{ formatarData(item.criadoEm) }}</td>
                                <td>
                                    <button
                                        class="btn-icon btn-icon-ver"
                                        type="button"
                                        :aria-label="`Ver detalhe de ${item.nomeFantasia}`"
                                        title="Ver detalhe"
                                        @click="irParaDetalhe(item.id)"
                                    />
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>

                <AppPagination
                    v-model:pagina="store.pagina"
                    v-model:tamanho="store.tamanhoPagina"
                    :total="store.total"
                    rotulo-itens="estabelecimentos"
                />
            </template>
        </AppCard>
    </main>
</template>

<style scoped>
.filtros-row {
    display: flex;
    gap: 0.75rem;
    margin-bottom: 1.25rem;
    flex-wrap: wrap;
    align-items: center;
}

.select-status {
    padding: 0.5rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    background: hsl(var(--background));
    color: hsl(var(--foreground));
}

.estado-info {
    padding: 2rem 0;
    text-align: center;
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
}

.estado-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    display: flex;
    align-items: center;
    gap: 1rem;
}

.tabela-wrap {
    overflow-x: auto;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    margin-bottom: 1rem;
}

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.875rem;
}

.tabela th,
.tabela td {
    padding: 0.625rem 0.875rem;
    text-align: left;
    border-bottom: 1px solid hsl(var(--border));
}

.tabela th {
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.5);
    font-size: 0.8rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.tabela tbody tr:last-child td { border-bottom: none; }
.tabela tbody tr:hover { background: hsl(var(--muted) / 0.4); }

.td-nome { font-weight: 600; }
.td-muted { font-size: 0.75rem; color: hsl(var(--muted-foreground)); }
.td-mono { font-family: monospace; font-size: 0.8rem; }
.td-num { text-align: center; }
</style>
