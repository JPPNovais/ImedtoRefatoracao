<script setup lang="ts">
/**
 * PlanosListView — lista de planos com ativar/desativar.
 *
 * W3-CA7 a W3-CA16: app-page + AppPageHeader + AppCard + AppSearchInput
 *   + AppEmptyState + AppButton + AppBadge + AppModal + AppField + AppTextarea.
 */
import { ref, onMounted } from "vue"
import { useRouter } from "vue-router"
import {
    AppPageHeader, AppCard, AppSearchInput, AppEmptyState,
    AppButton, AppBadge, AppModal, AppField, AppTextarea,
} from "@/components/ui"
import { usePlanosStore } from "../stores/planosStore"
import type { PlanoAdminDto } from "../services/planosService"

const router = useRouter()
const store = usePlanosStore()

const ID_GRATUIDADE_VITALICIA = "00000000-0000-0000-0000-000000000001"

const filtroBusca = ref("")
const filtroAtivo = ref<boolean | null>(null)

const modalMotivo = ref(false)
const motivoTexto = ref("")
const acaoPendente = ref<{ tipo: "ativar" | "desativar"; plano: PlanoAdminDto } | null>(null)
const erroMotivo = ref("")
const salvando = ref(false)

onMounted(() => carregar())

async function carregar() {
    await store.carregar({
        ativo: filtroAtivo.value,
        busca: filtroBusca.value || null,
    })
}

function abrirNovo() {
    router.push({ name: "AdminPlanosNovo" })
}

function abrirEditar(id: string) {
    router.push({ name: "AdminPlanosEditar", params: { id } })
}

function abrirModalAcao(tipo: "ativar" | "desativar", plano: PlanoAdminDto) {
    acaoPendente.value = { tipo, plano }
    motivoTexto.value = ""
    erroMotivo.value = ""
    modalMotivo.value = true
}

function fecharModal() {
    modalMotivo.value = false
    acaoPendente.value = null
    motivoTexto.value = ""
    erroMotivo.value = ""
}

async function confirmarAcao() {
    if (!acaoPendente.value) return
    if (motivoTexto.value.trim().length < 10) {
        erroMotivo.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }
    salvando.value = true
    erroMotivo.value = ""
    try {
        const { tipo, plano } = acaoPendente.value
        if (tipo === "ativar") {
            await store.ativar(plano.id, motivoTexto.value.trim())
        } else {
            await store.desativar(plano.id, motivoTexto.value.trim())
        }
        fecharModal()
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroMotivo.value = msg ?? "Não foi possível realizar a operação."
    } finally {
        salvando.value = false
    }
}

function formatarPreco(centavos: number | null): string {
    if (centavos == null) return "Sob consulta"
    if (centavos === 0) return "Gratuito"
    return `R$ ${(centavos / 100).toFixed(2).replace(".", ",")}`
}
</script>

<template>
    <main class="app-page">
        <AppPageHeader titulo="Planos" subtitulo="Planos disponíveis para estabelecimentos.">
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="abrirNovo">Novo plano</AppButton>
            </template>
        </AppPageHeader>

        <AppCard>
            <!-- Filtros -->
            <div class="filtros-row">
                <AppSearchInput v-model="filtroBusca" placeholder="Buscar por nome..." style="max-width:320px;" />
                <select v-model="filtroAtivo" class="select-filtro" @change="carregar">
                    <option :value="null">Todos</option>
                    <option :value="true">Ativos</option>
                    <option :value="false">Inativos</option>
                </select>
                <AppButton variant="secondary" @click="carregar">Buscar</AppButton>
            </div>

            <div v-if="store.carregando" class="estado-info">
                <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
            </div>

            <p v-else-if="store.erro" class="estado-erro" role="alert">{{ store.erro }}</p>

            <AppEmptyState
                v-else-if="store.lista.length === 0"
                titulo="Nenhum plano encontrado."
                descricao="Crie o primeiro plano usando o botão acima."
            />

            <template v-else>
                <div class="tabela-wrap">
                    <table class="tabela">
                        <thead>
                            <tr>
                                <th>Nome</th>
                                <th>Preço mensal</th>
                                <th>Status</th>
                                <th>Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="plano in store.lista" :key="plano.id">
                                <td class="td-nome">
                                    {{ plano.nome }}
                                    <AppBadge v-if="plano.id === ID_GRATUIDADE_VITALICIA" variant="success" label="Gratuidade" />
                                </td>
                                <td>{{ formatarPreco(plano.precoMensalCentavos) }}</td>
                                <td>
                                    <AppBadge :variant="plano.ativo ? 'success' : 'muted'" :label="plano.ativo ? 'Ativo' : 'Inativo'" />
                                </td>
                                <td class="td-acoes">
                                    <button
                                        class="btn-icon btn-icon-editar"
                                        type="button"
                                        title="Editar"
                                        @click="abrirEditar(plano.id)"
                                    >
                                        <i class="fa-solid fa-pen"></i>
                                    </button>
                                    <button
                                        v-if="!plano.ativo"
                                        class="btn-icon btn-icon-ver"
                                        type="button"
                                        title="Ativar"
                                        @click="abrirModalAcao('ativar', plano)"
                                    >
                                        <i class="fa-solid fa-circle-check"></i>
                                    </button>
                                    <button
                                        v-else
                                        class="btn-icon btn-icon-excluir"
                                        type="button"
                                        :disabled="plano.id === ID_GRATUIDADE_VITALICIA"
                                        :title="plano.id === ID_GRATUIDADE_VITALICIA ? 'Gratuidade Vitalícia não pode ser desativada' : 'Desativar'"
                                        @click="plano.id !== ID_GRATUIDADE_VITALICIA && abrirModalAcao('desativar', plano)"
                                    >
                                        <i class="fa-solid fa-ban"></i>
                                    </button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </template>
        </AppCard>

        <!-- Modal ativar/desativar -->
        <AppModal
            :aberto="modalMotivo"
            :titulo="acaoPendente?.tipo === 'ativar' ? 'Ativar plano' : 'Desativar plano'"
            @fechar="fecharModal"
        >
            <p class="modal-desc">{{ acaoPendente?.plano.nome }}</p>

            <AppField label="Motivo" required hint="Mínimo 10 caracteres.">
                <AppTextarea
                    v-model="motivoTexto"
                    :rows="3"
                    placeholder="Informe o motivo..."
                    :disabled="salvando"
                />
            </AppField>

            <p v-if="erroMotivo" class="campo-erro">{{ erroMotivo }}</p>

            <template #rodape>
                <AppButton variant="secondary" :disabled="salvando" @click="fecharModal">Cancelar</AppButton>
                <AppButton
                    :variant="acaoPendente?.tipo === 'desativar' ? 'danger' : 'primary'"
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

.select-filtro {
    padding: 0.5rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    background: hsl(var(--background));
    color: hsl(var(--foreground));
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

.td-nome {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-weight: 500;
}

.td-acoes {
    display: flex;
    gap: 0.25rem;
}

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
