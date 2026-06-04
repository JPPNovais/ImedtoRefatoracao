<script setup lang="ts">
/**
 * PermissoesGlobaisListView — lista e CRUD de modelos de permissão padrão do sistema.
 * Briefing 2026-06-04_001. CA15, CA16, CA17.
 * Reusa PapelEditorModal no contexto='admin' via injeção de servicoSalvar/servicoExcluir.
 */
import { ref, watch, onMounted } from "vue"
import {
    AppPageHeader, AppCard, AppSearchInput, AppEmptyState,
    AppPagination, AppButton, AppModal,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import PapelEditorModal from "@/components/equipe/PapelEditorModal.vue"
import { usePermissoesGlobaisStore } from "../stores/permissoesGlobaisStore"
import { permissoesGlobaisService, type ModeloPermissaoPadraoListaItemDto } from "../services/catalogosService"
import type { ModeloPermissao } from "@/services/permissaoService"

const store = usePermissoesGlobaisStore()

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput, 300)
const modalEditar = ref(false)
const modeloEditando = ref<ModeloPermissao | null>(null)
const confirmacaoAberta = ref(false)
const confirmacaoMensagem = ref("")
const confirmacaoCb = ref<(() => Promise<void>) | null>(null)
const salvando = ref(false)
const erroConfirmacao = ref<string | null>(null)

// Payloads pendentes para o step de confirmação
const payloadPendente = ref<{
    tipo: "criar" | "atualizar" | "excluir"
    id?: number
    dados?: {
        nome: string
        tipoAcesso: string
        permissoes: string[]
        icone: string | null
        cor: string | null
        descricao: string | null
    }
} | null>(null)

watch(busca, () => carregar())
onMounted(() => carregar())

async function carregar() {
    await store.carregar({
        busca: busca.value || undefined,
        page: store.pagina,
        size: store.tamanho,
    })
}

function abrirNovo() {
    modeloEditando.value = null
    modalEditar.value = true
}

async function abrirEditar(item: ModeloPermissaoPadraoListaItemDto) {
    try {
        const detalhe = await permissoesGlobaisService.obter(item.id)
        modeloEditando.value = {
            id: detalhe.id,
            nome: detalhe.nome,
            tipoAcesso: detalhe.tipoAcesso as "Profissional" | "Recepcionista",
            permissoes: detalhe.permissoes,
            ehPadrao: true,
            criadoEm: detalhe.criadoEm,
            icone: detalhe.icone ?? undefined,
            cor: detalhe.cor ?? undefined,
            descricao: detalhe.descricao ?? undefined,
        }
        modalEditar.value = true
    } catch {
        // silencioso
    }
}

function fecharModal() {
    modalEditar.value = false
    modeloEditando.value = null
}

function aoSalvarModal(modelo: ModeloPermissao) {
    // Ao clicar em Salvar no modal, abrimos confirmação de impacto
    const payload = {
        nome: modelo.nome,
        tipoAcesso: modelo.tipoAcesso,
        permissoes: modelo.permissoes,
        icone: modelo.icone ?? null,
        cor: modelo.cor ?? null,
        descricao: modelo.descricao ?? null,
    }
    const ehNovo = !modeloEditando.value
    payloadPendente.value = {
        tipo: ehNovo ? "criar" : "atualizar",
        id: modeloEditando.value?.id,
        dados: payload,
    }
    const nEstabs = store.total > 0 ? store.total : "todos os"
    confirmacaoMensagem.value = `Esta alteração será aplicada a todas as clínicas (${nEstabs} estabelecimentos). Profissionais com este modelo terão suas permissões atualizadas imediatamente.`
    erroConfirmacao.value = null
    confirmacaoAberta.value = true
}

function aoExcluirModal(modelo: ModeloPermissao) {
    payloadPendente.value = { tipo: "excluir", id: modelo.id }
    confirmacaoMensagem.value = `O modelo "${modelo.nome}" será excluído de todas as clínicas. Esta ação é irreversível.`
    erroConfirmacao.value = null
    confirmacaoAberta.value = true
}

async function executarConfirmacao() {
    if (!payloadPendente.value) return
    salvando.value = true
    erroConfirmacao.value = null
    try {
        const p = payloadPendente.value
        if (p.tipo === "criar" && p.dados) {
            await store.criar(p.dados)
        } else if (p.tipo === "atualizar" && p.id && p.dados) {
            await store.atualizar(p.id, p.dados)
        } else if (p.tipo === "excluir" && p.id) {
            await store.excluir(p.id)
        }
        confirmacaoAberta.value = false
        fecharModal()
        await carregar()
    } catch (e: any) {
        erroConfirmacao.value = e?.response?.data?.mensagem ?? "Não foi possível realizar a operação."
    } finally {
        salvando.value = false
    }
}

// servicoSalvar injetado no PapelEditorModal — não faz a chamada de API,
// apenas emite o evento `salvo` para o pai interceptar e abrir confirmação.
// A chamada real acontece em executarConfirmacao() após confirmação do admin.
async function servicoSalvarAdmin(_payload: unknown) {
    // Vazio — o modal emite @salvo e a view intercepta em aoSalvarModal.
    // O PapelEditorModal emite o evento após chamar este fn, então a view abre a confirmação.
}

async function servicoExcluirAdmin(_id: number) {
    // Idem — delega para o evento @excluido → aoExcluirModal.
}

function formatarData(iso: string | null): string {
    if (!iso) return "—"
    return new Date(iso).toLocaleDateString("pt-BR")
}
</script>

<template>
    <main class="app-page">
        <AppPageHeader
            titulo="Modelos de permissão padrão do sistema"
            subtitulo="Valem para todas as clínicas. Editar aqui altera as permissões em todos os estabelecimentos."
        >
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="abrirNovo">Novo modelo</AppButton>
            </template>
        </AppPageHeader>

        <AppCard>
            <div class="filtros-row">
                <AppSearchInput v-model="buscaInput" placeholder="Buscar por nome..." style="max-width:320px;" />
            </div>

            <div v-if="store.carregando" class="estado-info">
                <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
            </div>

            <p v-else-if="store.erro" class="estado-erro" role="alert">{{ store.erro }}</p>

            <AppEmptyState
                v-else-if="store.lista.length === 0"
                titulo="Nenhum modelo padrão cadastrado."
                descricao="Crie o primeiro modelo usando o botão acima."
            />

            <template v-else>
                <div class="tabela-wrap">
                    <table class="tabela">
                        <thead>
                            <tr>
                                <th>Modelo</th>
                                <th>Tipo</th>
                                <th>Atualizado em</th>
                                <th>Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="item in store.lista" :key="item.id">
                                <td class="td-nome-cell">
                                    <span class="nome-wrap">
                                        <span
                                            v-if="item.icone"
                                            class="nome-icone"
                                            :style="{ color: item.cor ?? undefined }"
                                        >
                                            <i class="fa-solid" :class="item.icone"></i>
                                        </span>
                                        <span class="nome-texto">{{ item.nome }}</span>
                                    </span>
                                    <span v-if="item.descricao" class="td-desc">{{ item.descricao }}</span>
                                </td>
                                <td>{{ item.tipoAcesso === "Profissional" ? "Profissional" : "Recepção" }}</td>
                                <td>{{ formatarData(item.atualizadoEm) }}</td>
                                <td class="td-acoes">
                                    <button class="btn-icon btn-icon-editar" type="button" title="Editar" @click="abrirEditar(item)">
                                        <i class="fa-solid fa-pen"></i>
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

        <!-- Reusa PapelEditorModal no contexto admin (CA17) -->
        <PapelEditorModal
            :aberto="modalEditar"
            :modelo="modeloEditando"
            contexto="admin"
            :servico-salvar="servicoSalvarAdmin"
            :servico-excluir="servicoExcluirAdmin"
            @fechar="fecharModal"
            @salvo="aoSalvarModal"
            @excluido="aoExcluirModal"
        />

        <!-- Confirmação de impacto cross-tenant (operação de alto risco) -->
        <AppModal
            :aberto="confirmacaoAberta"
            titulo="Confirmar impacto em todas as clínicas"
            @fechar="confirmacaoAberta = false"
        >
            <p class="confirmacao-msg">{{ confirmacaoMensagem }}</p>
            <p v-if="erroConfirmacao" class="campo-erro" role="alert">{{ erroConfirmacao }}</p>
            <template #rodape>
                <AppButton variant="secondary" :disabled="salvando" @click="confirmacaoAberta = false">Cancelar</AppButton>
                <AppButton
                    variant="danger"
                    :loading="salvando"
                    :disabled="salvando"
                    @click="executarConfirmacao"
                >
                    Confirmar e aplicar
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

.td-nome-cell {
    max-width: 280px;
    display: flex;
    flex-direction: column;
    gap: 2px;
}
.nome-wrap { display: flex; align-items: center; gap: 6px; }
.nome-icone { font-size: 0.875rem; }
.nome-texto { font-weight: 600; }
.td-desc { font-size: 0.75rem; font-weight: 400; color: hsl(var(--muted-foreground)); }
.td-acoes { display: flex; gap: 0.25rem; }

.confirmacao-msg { font-size: 0.9rem; color: hsl(var(--foreground)); line-height: 1.5; }

.campo-erro {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin-top: 0.5rem;
}
</style>
