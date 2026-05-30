<script setup lang="ts">
import { ref, onMounted } from "vue"
import { useRouter } from "vue-router"
import { usePlanosStore } from "../stores/planosStore"
import type { PlanoAdminDto } from "../services/planosService"

const router = useRouter()
const store = usePlanosStore()

const ID_GRATUIDADE_VITALICIA = "00000000-0000-0000-0000-000000000001"

const filtroAtivo = ref<boolean | null>(null)
const filtroBusca = ref("")

// Modal desativar/ativar
const modalMotivo = ref(false)
const motivoTexto = ref("")
const acaoPendente = ref<{ tipo: "ativar" | "desativar"; plano: PlanoAdminDto } | null>(null)
const erroMotivo = ref("")

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
    if (!motivoTexto.value.trim()) {
        erroMotivo.value = "Motivo é obrigatório."
        return
    }

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
    }
}

function formatarPreco(centavos: number | null): string {
    if (centavos == null) return "Sob consulta"
    if (centavos === 0) return "Gratuito"
    return `R$ ${(centavos / 100).toFixed(2).replace(".", ",")}`
}
</script>

<template>
    <div class="admin-planos-list">
        <div class="admin-planos-header">
            <h1 class="admin-page-title">Planos</h1>
            <button class="admin-btn-primary" @click="abrirNovo">+ Novo plano</button>
        </div>

        <!-- Filtros -->
        <div class="admin-filtros">
            <input
                v-model="filtroBusca"
                class="admin-input"
                placeholder="Buscar por nome..."
                @keyup.enter="carregar"
            />
            <select v-model="filtroAtivo" class="admin-select" @change="carregar">
                <option :value="null">Todos</option>
                <option :value="true">Ativos</option>
                <option :value="false">Inativos</option>
            </select>
            <button class="admin-btn-secondary" @click="carregar">Buscar</button>
        </div>

        <!-- Lista -->
        <div v-if="store.carregando" class="admin-carregando">Carregando...</div>
        <div v-else-if="store.erro" class="admin-erro">{{ store.erro }}</div>
        <div v-else>
            <table class="admin-table">
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
                        <td>
                            {{ plano.nome }}
                            <span v-if="plano.id === ID_GRATUIDADE_VITALICIA" class="badge-gratuidade">
                                Gratuidade
                            </span>
                        </td>
                        <td>{{ formatarPreco(plano.precoMensalCentavos) }}</td>
                        <td>
                            <span :class="plano.ativo ? 'badge-ativo' : 'badge-inativo'">
                                {{ plano.ativo ? "Ativo" : "Inativo" }}
                            </span>
                        </td>
                        <td class="admin-acoes">
                            <button class="btn-icon btn-icon-editar" @click="abrirEditar(plano.id)" title="Editar">
                                ✏️
                            </button>
                            <button
                                v-if="!plano.ativo"
                                class="btn-icon"
                                @click="abrirModalAcao('ativar', plano)"
                                title="Ativar"
                            >
                                ▶
                            </button>
                            <button
                                v-else
                                class="btn-icon btn-icon-excluir"
                                :disabled="plano.id === ID_GRATUIDADE_VITALICIA"
                                :title="plano.id === ID_GRATUIDADE_VITALICIA ? 'Gratuidade Vitalícia não pode ser desativada' : 'Desativar'"
                                @click="plano.id !== ID_GRATUIDADE_VITALICIA && abrirModalAcao('desativar', plano)"
                            >
                                ⏸
                            </button>
                        </td>
                    </tr>
                    <tr v-if="store.lista.length === 0">
                        <td colspan="4" class="admin-vazio">Nenhum plano encontrado.</td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Modal motivo -->
        <div v-if="modalMotivo" class="admin-modal-overlay" @click.self="fecharModal">
            <div class="admin-modal">
                <h2 class="admin-modal-title">
                    {{ acaoPendente?.tipo === "ativar" ? "Ativar plano" : "Desativar plano" }}
                </h2>
                <p class="admin-modal-desc">{{ acaoPendente?.plano.nome }}</p>

                <label class="admin-label">Motivo</label>
                <textarea v-model="motivoTexto" class="admin-textarea" rows="3" placeholder="Informe o motivo..." />
                <p v-if="erroMotivo" class="admin-campo-erro">{{ erroMotivo }}</p>

                <div class="admin-modal-actions">
                    <button class="admin-btn-secondary" @click="fecharModal">Cancelar</button>
                    <button class="admin-btn-primary" @click="confirmarAcao">Confirmar</button>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.admin-planos-list {
    color: hsl(var(--foreground));
}

.admin-planos-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 1.5rem;
}

.admin-page-title {
    font-size: 1.5rem;
    font-weight: 700;
    color: hsl(var(--foreground));
    margin: 0;
}

.admin-filtros {
    display: flex;
    gap: 0.75rem;
    margin-bottom: 1.5rem;
    flex-wrap: wrap;
}

.admin-input,
.admin-select {
    background: hsl(var(--background));
    border: 1px solid hsl(var(--border));
    color: hsl(var(--foreground));
    border-radius: 6px;
    padding: 0.5rem 0.75rem;
    font-size: 0.875rem;
}

.admin-input {
    flex: 1;
    min-width: 200px;
}

.admin-btn-primary {
    background: hsl(var(--primary));
    color: hsl(var(--primary-foreground));
    border: none;
    border-radius: 6px;
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    font-weight: 600;
    cursor: pointer;
}

.admin-btn-primary:hover {
    opacity: 0.9;
}

.admin-btn-secondary {
    background: hsl(var(--muted));
    color: hsl(var(--foreground));
    border: none;
    border-radius: 6px;
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    cursor: pointer;
}

.admin-btn-secondary:hover {
    opacity: 0.8;
}

.admin-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.875rem;
}

.admin-table th,
.admin-table td {
    text-align: left;
    padding: 0.75rem 1rem;
    border-bottom: 1px solid hsl(var(--border));
}

.admin-table th {
    color: hsl(var(--muted-foreground));
    font-weight: 600;
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.admin-table tbody tr:hover {
    background: hsl(var(--muted) / 0.3);
}

.admin-acoes {
    display: flex;
    gap: 0.5rem;
}

.badge-gratuidade {
    background: hsl(var(--success) / 0.15);
    color: hsl(var(--success));
    font-size: 0.6875rem;
    font-weight: 600;
    padding: 0.15rem 0.5rem;
    border-radius: 9999px;
    margin-left: 0.5rem;
}

.badge-ativo {
    background: hsl(var(--success) / 0.15);
    color: hsl(var(--success));
    font-size: 0.75rem;
    padding: 0.2rem 0.6rem;
    border-radius: 9999px;
}

.badge-inativo {
    background: hsl(var(--destructive) / 0.12);
    color: hsl(var(--destructive));
    font-size: 0.75rem;
    padding: 0.2rem 0.6rem;
    border-radius: 9999px;
}

.admin-vazio,
.admin-carregando,
.admin-erro {
    text-align: center;
    color: hsl(var(--muted-foreground));
    padding: 2rem 0;
}

.admin-erro {
    color: hsl(var(--destructive));
}

/* Modal */
.admin-modal-overlay {
    position: fixed;
    inset: 0;
    background: hsl(var(--foreground) / 0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
}

.admin-modal {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 12px;
    padding: 1.5rem;
    width: 100%;
    max-width: 440px;
}

.admin-modal-title {
    font-size: 1.125rem;
    font-weight: 700;
    color: hsl(var(--foreground));
    margin: 0 0 0.25rem;
}

.admin-modal-desc {
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
    margin: 0 0 1rem;
}

.admin-label {
    display: block;
    color: hsl(var(--muted-foreground));
    font-size: 0.8125rem;
    font-weight: 600;
    margin-bottom: 0.375rem;
}

.admin-textarea {
    width: 100%;
    background: hsl(var(--background));
    border: 1px solid hsl(var(--border));
    color: hsl(var(--foreground));
    border-radius: 6px;
    padding: 0.5rem 0.75rem;
    font-size: 0.875rem;
    resize: vertical;
    box-sizing: border-box;
}

.admin-campo-erro {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin: 0.25rem 0 0;
}

.admin-modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
    margin-top: 1.25rem;
}

.btn-icon {
    background: none;
    border: none;
    cursor: pointer;
    font-size: 1rem;
    padding: 0.25rem;
    border-radius: 4px;
    color: hsl(var(--muted-foreground));
}

.btn-icon:hover {
    background: hsl(var(--muted));
}

.btn-icon:disabled {
    opacity: 0.35;
    cursor: not-allowed;
}

.btn-icon-editar {
    color: hsl(220 80% 65%);
}

.btn-icon-excluir {
    color: hsl(var(--destructive));
}
</style>
