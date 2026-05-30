<script setup lang="ts">
/**
 * ModelosGlobaisListView — lista de modelos de prontuário padrão sistema (Wave 4 live-link).
 */
import { ref, onMounted } from "vue"
import { useRouter } from "vue-router"
import {
    AppPageHeader, AppCard, AppSearchInput, AppEmptyState,
    AppPagination, AppButton, AppBadge, AppModal, AppField, AppTextarea,
} from "@/components/ui"
import { useModelosGlobaisStore } from "../stores/modelosGlobaisStore"
import type { ModeloPadraoSistemaListaItemDto } from "../services/catalogosService"

const router = useRouter()
const store = useModelosGlobaisStore()

const filtroInativos = ref(false)
const filtroBusca = ref("")

const modalAcao = ref(false)
const acaoTipo = ref<"inativar" | "reativar">("inativar")
const acaoItem = ref<ModeloPadraoSistemaListaItemDto | null>(null)
const motivoTexto = ref("")
const erroMotivo = ref("")
const salvando = ref(false)

onMounted(() => carregar())

async function carregar() {
    await store.carregar({
        incluirInativos: filtroInativos.value,
        busca: filtroBusca.value || undefined,
        page: store.pagina,
        size: store.tamanho,
    })
}

function irParaForm(id?: number) {
    if (id !== undefined) {
        router.push({ name: "AdminModelosGlobaisEditar", params: { id } })
    } else {
        router.push({ name: "AdminModelosGlobaisNovo" })
    }
}

function abrirAcao(tipo: "inativar" | "reativar", item: ModeloPadraoSistemaListaItemDto) {
    acaoTipo.value = tipo
    acaoItem.value = item
    motivoTexto.value = ""
    erroMotivo.value = ""
    modalAcao.value = true
}

function fecharModal() {
    modalAcao.value = false
    acaoItem.value = null
}

async function confirmarAcao() {
    if (!acaoItem.value) return
    if (motivoTexto.value.trim().length < 10) {
        erroMotivo.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }
    salvando.value = true
    erroMotivo.value = ""
    try {
        if (acaoTipo.value === "inativar") {
            await store.inativar(acaoItem.value.id, motivoTexto.value.trim())
        } else {
            await store.reativar(acaoItem.value.id, motivoTexto.value.trim())
        }
        fecharModal()
        await carregar()
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroMotivo.value = msg ?? "Não foi possível realizar a operação."
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
            titulo="Modelos de prontuário globais"
            subtitulo="Templates de estrutura de prontuário disponíveis para todos os estabelecimentos."
        >
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="irParaForm()">Novo modelo</AppButton>
            </template>
        </AppPageHeader>

        <AppCard>
            <!-- Filtros -->
            <div class="filtros-row">
                <AppSearchInput v-model="filtroBusca" placeholder="Buscar por nome..." style="max-width:320px;" />
                <label class="label-check">
                    <input type="checkbox" v-model="filtroInativos" @change="carregar" />
                    Incluir inativos
                </label>
                <AppButton variant="secondary" @click="carregar">Buscar</AppButton>
            </div>

            <div v-if="store.carregando" class="estado-info">
                <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
            </div>

            <p v-else-if="store.erro" class="estado-erro" role="alert">{{ store.erro }}</p>

            <AppEmptyState
                v-else-if="store.lista.length === 0"
                titulo="Nenhum modelo encontrado."
                descricao="Crie o primeiro modelo usando o botão acima."
            />

            <template v-else>
                <div class="tabela-wrap">
                    <table class="tabela">
                        <thead>
                            <tr>
                                <th>Nome</th>
                                <th>Descrição</th>
                                <th>Status</th>
                                <th>Atualizado em</th>
                                <th>Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="item in store.lista" :key="item.id">
                                <td class="td-nome">{{ item.nome }}</td>
                                <td class="td-desc">{{ item.descricao ?? "—" }}</td>
                                <td>
                                    <AppBadge :variant="item.ativo ? 'success' : 'muted'" :label="item.ativo ? 'Ativo' : 'Inativo'" />
                                </td>
                                <td>{{ formatarData(item.atualizadoEm) }}</td>
                                <td class="td-acoes">
                                    <button class="btn-icon btn-icon-editar" type="button" title="Editar" @click="irParaForm(item.id)">
                                        <i class="fa-solid fa-pen"></i>
                                    </button>
                                    <button
                                        v-if="item.ativo"
                                        class="btn-icon btn-icon-excluir"
                                        type="button"
                                        title="Inativar"
                                        @click="abrirAcao('inativar', item)"
                                    >
                                        <i class="fa-solid fa-ban"></i>
                                    </button>
                                    <button
                                        v-else
                                        class="btn-icon btn-icon-ver"
                                        type="button"
                                        title="Reativar"
                                        @click="abrirAcao('reativar', item)"
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
                    rotulo-itens="modelos"
                    @update:pagina="carregar"
                />
            </template>
        </AppCard>

        <!-- Modal inativar/reativar -->
        <AppModal
            :aberto="modalAcao"
            :titulo="acaoTipo === 'inativar' ? 'Inativar modelo' : 'Reativar modelo'"
            @fechar="fecharModal"
        >
            <p class="modal-desc">{{ acaoItem?.nome }}</p>

            <AppField label="Motivo" required hint="Mínimo 10 caracteres.">
                <AppTextarea
                    v-model="motivoTexto"
                    :rows="3"
                    placeholder="Descreva o motivo..."
                    :disabled="salvando"
                />
            </AppField>

            <p v-if="erroMotivo" class="campo-erro">{{ erroMotivo }}</p>

            <template #rodape>
                <AppButton variant="secondary" :disabled="salvando" @click="fecharModal">Cancelar</AppButton>
                <AppButton
                    :variant="acaoTipo === 'inativar' ? 'danger' : 'primary'"
                    :loading="salvando"
                    :disabled="motivoTexto.trim().length < 10"
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

.label-check {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    font-size: 0.875rem;
    color: hsl(var(--foreground));
    cursor: pointer;
}

.estado-info {
    text-align: center;
    padding: 2rem 0;
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

.td-nome { font-weight: 600; max-width: 220px; }
.td-desc { color: hsl(var(--muted-foreground)); max-width: 280px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.td-acoes { display: flex; gap: 0.25rem; }

.modal-desc {
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
    margin-bottom: 0.25rem;
}

.campo-erro {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin-top: 0.25rem;
}
</style>
