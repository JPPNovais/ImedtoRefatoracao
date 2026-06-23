<script setup lang="ts">
/**
 * CategoriasFinanceirasGlobaisListView — lista e CRUD de categorias financeiras
 * padrão do sistema (briefing 2026-06-22_003 M3).
 * Espelha VariaveisGlobaisListView + PermissoesGlobaisListView.
 * Sem rename (R5) — nome é imutável após criação.
 */
import { ref, onMounted } from "vue"
import { useRouter } from "vue-router"
import {
    AppPageHeader, AppCard, AppEmptyState,
    AppPagination, AppButton, AppBadge, AppModal,
} from "@/components/ui"
import { useCategoriasFinanceirasGlobaisStore } from "../stores/categoriasFinanceirasGlobaisStore"
import type { TipoCategoriaFinanceira } from "../services/catalogosService"

const router = useRouter()
const store = useCategoriasFinanceirasGlobaisStore()

const filtroTipo = ref<"" | TipoCategoriaFinanceira>("")
const filtroAtivo = ref<"" | "ativas" | "inativas">("")

const modalAcao = ref(false)
const acaoTipo = ref<"inativar" | "reativar">("inativar")
const acaoId = ref<number | null>(null)
const acaoNome = ref("")
const salvando = ref(false)
const erroAcao = ref<string | null>(null)

onMounted(() => carregar())

async function carregar() {
    await store.carregar({
        tipo: filtroTipo.value || undefined,
        ativas: filtroAtivo.value === "ativas" ? true : filtroAtivo.value === "inativas" ? false : undefined,
        page: store.pagina,
        size: store.tamanho,
    })
}

function irParaForm() {
    router.push({ name: "AdminCategoriasFinanceirasGlobaisNovo" })
}

function abrirAcao(tipo: "inativar" | "reativar", id: number, nome: string) {
    acaoTipo.value = tipo
    acaoId.value = id
    acaoNome.value = nome
    erroAcao.value = null
    modalAcao.value = true
}

function fecharModal() {
    modalAcao.value = false
    acaoId.value = null
}

async function confirmarAcao() {
    if (acaoId.value === null) return
    salvando.value = true
    erroAcao.value = null
    try {
        if (acaoTipo.value === "inativar") {
            await store.inativar(acaoId.value)
        } else {
            await store.reativar(acaoId.value)
        }
        fecharModal()
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroAcao.value = msg ?? "Não foi possível realizar a operação."
    } finally {
        salvando.value = false
    }
}

function formatarData(iso: string | null): string {
    if (!iso) return "—"
    return new Date(iso).toLocaleDateString("pt-BR")
}
</script>

<template>
    <main class="app-page">
        <AppPageHeader
            titulo="Categorias financeiras padrão"
            subtitulo="Lista padrão global, propagada a todos os estabelecimentos ao criar um novo. Sem rename — para trocar o nome, inative a antiga e crie uma nova."
        >
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="irParaForm">Nova categoria</AppButton>
            </template>
        </AppPageHeader>

        <AppCard>
            <!-- Filtros -->
            <div class="filtros-row">
                <select v-model="filtroTipo" class="select-filtro" @change="carregar">
                    <option value="">Todos os tipos</option>
                    <option value="Receita">Receitas</option>
                    <option value="Despesa">Despesas</option>
                </select>
                <select v-model="filtroAtivo" class="select-filtro" @change="carregar">
                    <option value="">Ativas e inativas</option>
                    <option value="ativas">Somente ativas</option>
                    <option value="inativas">Somente inativas</option>
                </select>
                <AppButton variant="secondary" @click="carregar">Buscar</AppButton>
            </div>

            <div v-if="store.carregando" class="estado-info">
                <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
            </div>

            <p v-else-if="store.erro" class="estado-erro" role="alert">{{ store.erro }}</p>

            <AppEmptyState
                v-else-if="store.lista.length === 0"
                titulo="Nenhuma categoria encontrada."
                descricao="Crie a primeira categoria financeira padrão usando o botão acima."
            />

            <template v-else>
                <div class="tabela-wrap">
                    <table class="tabela">
                        <thead>
                            <tr>
                                <th>Nome</th>
                                <th>Tipo</th>
                                <th>Status</th>
                                <th>Criada em</th>
                                <th>Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="item in store.lista" :key="item.id" :class="{ 'linha-inativa': !item.ativo }">
                                <td class="td-nome">{{ item.nome }}</td>
                                <td>
                                    <AppBadge
                                        :variant="item.tipo === 'Receita' ? 'success' : 'error'"
                                        :label="item.tipo"
                                    />
                                </td>
                                <td>
                                    <AppBadge
                                        :variant="item.ativo ? 'success' : 'muted'"
                                        :label="item.ativo ? 'Ativa' : 'Inativa'"
                                    />
                                </td>
                                <td>{{ formatarData(item.criadaEm) }}</td>
                                <td class="td-acoes">
                                    <!-- Sem botão de editar nome (R5 — nome imutável) -->
                                    <button
                                        v-if="item.ativo"
                                        class="btn-icon btn-icon-excluir"
                                        type="button"
                                        title="Inativar"
                                        @click="abrirAcao('inativar', item.id, item.nome)"
                                    >
                                        <i class="fa-solid fa-ban"></i>
                                    </button>
                                    <button
                                        v-else
                                        class="btn-icon btn-icon-ver"
                                        type="button"
                                        title="Reativar"
                                        @click="abrirAcao('reativar', item.id, item.nome)"
                                    >
                                        <i class="fa-solid fa-rotate-left"></i>
                                    </button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>

                <AppPagination
                    v-model:pagina="store.pagina"
                    v-model:tamanho="store.tamanho"
                    :total="store.total"
                    rotulo-itens="categorias"
                    @update:pagina="carregar"
                />
            </template>
        </AppCard>

        <!-- Modal inativar/reativar -->
        <AppModal
            :aberto="modalAcao"
            :titulo="acaoTipo === 'inativar' ? 'Inativar categoria' : 'Reativar categoria'"
            @fechar="fecharModal"
        >
            <p class="modal-desc">
                <strong>{{ acaoNome }}</strong>
            </p>
            <p class="modal-aviso">
                <template v-if="acaoTipo === 'inativar'">
                    A categoria será inativada e propagada para todos os estabelecimentos — cópias com este nome ficarão ocultas no seletor de lançamentos.
                </template>
                <template v-else>
                    A categoria será reativada e propagada para todos os estabelecimentos — cópias com este nome voltarão a aparecer no seletor de lançamentos.
                </template>
            </p>

            <p v-if="erroAcao" class="campo-erro" role="alert">{{ erroAcao }}</p>

            <template #rodape>
                <AppButton variant="secondary" :disabled="salvando" @click="fecharModal">Cancelar</AppButton>
                <AppButton
                    :variant="acaoTipo === 'inativar' ? 'danger' : 'primary'"
                    :loading="salvando"
                    :disabled="salvando"
                    @click="confirmarAcao"
                >
                    Confirmar
                </AppButton>
            </template>
        </AppModal>
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

.select-filtro {
    padding: 0.5rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
    background: hsl(var(--background));
    color: hsl(var(--foreground));
}

.estado-info {
    text-align: center;
    padding: 2rem 0;
    color: hsl(var(--muted-foreground));
    font-size: var(--text-sm);
}

.estado-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
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
    font-size: var(--text-sm);
}

.tabela th,
.tabela td {
    padding: 0.625rem 0.875rem;
    text-align: left;
    border-bottom: 1px solid hsl(var(--border));
}

.tabela th {
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.5);
    font-size: var(--text-xs);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.tabela tbody tr:last-child td { border-bottom: none; }
.tabela tbody tr:hover { background: hsl(var(--muted) / 0.4); }
.linha-inativa { opacity: 0.6; }

.td-nome { font-weight: var(--font-weight-semibold); }
.td-acoes { display: flex; gap: 0.25rem; }

.modal-desc {
    font-size: var(--text-sm);
    margin-bottom: 0.5rem;
}

.modal-aviso {
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
    line-height: 1.5;
    margin-bottom: 0.25rem;
}

.campo-erro {
    color: hsl(var(--destructive));
    font-size: var(--text-sm);
    margin-top: 0.5rem;
}
</style>
