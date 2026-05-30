<script setup lang="ts">
/**
 * AdminsListView — lista paginada de administradores do sistema.
 *
 * CAs: CA37, CA38, CA39, CA39b, CA40, CA43.
 * CA28: debounce na busca via useDebouncedRef.
 * CA25: mensagens 422 exibidas diretamente do backend.
 */
import { ref, watch, computed } from "vue"
import { useRouter } from "vue-router"
import {
    AppButton, AppPageHeader, AppPagination, AppEmptyState, AppSearchInput, AppModal,
    AppField, AppInput,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { useAdminsStore } from "../stores/adminsStore"
import type { AdminListItem } from "../services/adminsService"

const router = useRouter()
const store = useAdminsStore()

// Busca com debounce (CA28)
const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput, 300)

watch(busca, () => { store.pagina = 1 })
watch([busca, () => store.pagina, () => store.tamanho], () => {
    void store.carregar(busca.value)
}, { immediate: true })

// Modal de ação com motivo (desativar / reativar / reset senha)
type Acao = "desativar" | "reativar" | "resetarSenha"
const modalAberto = ref(false)
const acaoAtual = ref<Acao>("desativar")
const adminAlvo = ref<AdminListItem | null>(null)
const motivo = ref("")
const acaoEmCurso = ref(false)
const erroModal = ref<string | null>(null)

// Resultado de reset de senha (exibido uma vez)
const senhaResetadaAberto = ref(false)
const senhaResetadaTemp = ref("")

function abrirModal(admin: AdminListItem, acao: Acao) {
    adminAlvo.value = admin
    acaoAtual.value = acao
    motivo.value = ""
    erroModal.value = null
    modalAberto.value = true
}

function fecharModal() {
    modalAberto.value = false
    adminAlvo.value = null
    motivo.value = ""
    erroModal.value = null
}

const titulos: Record<Acao, string> = {
    desativar: "Desativar administrador",
    reativar: "Reativar administrador",
    resetarSenha: "Resetar senha",
}

const confirmarTextos: Record<Acao, string> = {
    desativar: "Desativar",
    reativar: "Reativar",
    resetarSenha: "Resetar senha",
}

async function confirmarAcao() {
    if (!adminAlvo.value) return
    acaoEmCurso.value = true
    erroModal.value = null
    try {
        const id = adminAlvo.value.id
        if (acaoAtual.value === "desativar") {
            await store.desativar(id, motivo.value)
        } else if (acaoAtual.value === "reativar") {
            await store.reativar(id, motivo.value)
        } else {
            const senha = await store.resetarSenha(id, motivo.value)
            senhaResetadaTemp.value = senha
            senhaResetadaAberto.value = true
        }
        fecharModal()
        void store.carregar(busca.value)
    } catch (e: any) {
        erroModal.value = e?.response?.data?.mensagem ?? "Não foi possível executar a ação."
    } finally {
        acaoEmCurso.value = false
    }
}

const motivoValido = computed(() => motivo.value.trim().length >= 10)

function formatarData(iso: string | null): string {
    if (!iso) return "—"
    return new Date(iso).toLocaleDateString("pt-BR")
}
</script>

<template>
    <main class="app-page">
        <AppPageHeader titulo="Administradores" subtitulo="Contas com acesso à área administrativa global.">
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="router.push({ name: 'AdminAdminsNovo' })">
                    Novo administrador
                </AppButton>
            </template>
        </AppPageHeader>

        <div class="admins-toolbar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar por nome ou e-mail…" />
        </div>

        <p v-if="store.erro" class="admins-erro">{{ store.erro }}</p>

        <div v-if="store.carregando" class="admins-carregando">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando…
        </div>

        <template v-else-if="store.itens.length === 0 && !store.erro">
            <AppEmptyState
                titulo="Nenhum administrador encontrado."
                descricao="Crie o primeiro administrador usando o botão acima."
            />
        </template>

        <template v-else>
            <div class="admins-table-wrap">
                <table class="admins-table">
                    <thead>
                        <tr>
                            <th>Nome</th>
                            <th>E-mail</th>
                            <th>Status</th>
                            <th>Senha</th>
                            <th>Último login</th>
                            <th>Criado em</th>
                            <th>Ações</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="admin in store.itens" :key="admin.id">
                            <td>{{ admin.nome }}</td>
                            <td>{{ admin.email }}</td>
                            <td>
                                <span :class="admin.ativo ? 'badge-ativo' : 'badge-inativo'">
                                    {{ admin.ativo ? "Ativo" : "Inativo" }}
                                </span>
                            </td>
                            <td>
                                <span v-if="admin.forcePasswordReset" class="badge-reset">
                                    Reset pendente
                                </span>
                                <span v-else class="badge-ok">OK</span>
                            </td>
                            <td>{{ formatarData(admin.ultimoLoginEm) }}</td>
                            <td>{{ formatarData(admin.criadoEm) }}</td>
                            <td class="admins-acoes">
                                <button
                                    v-if="admin.ativo"
                                    type="button"
                                    class="btn-icon btn-icon-editar"
                                    title="Resetar senha"
                                    @click="abrirModal(admin, 'resetarSenha')"
                                >
                                    <i class="fa-solid fa-key"></i>
                                </button>
                                <button
                                    v-if="admin.ativo"
                                    type="button"
                                    class="btn-icon btn-icon-excluir"
                                    title="Desativar"
                                    @click="abrirModal(admin, 'desativar')"
                                >
                                    <i class="fa-solid fa-ban"></i>
                                </button>
                                <button
                                    v-if="!admin.ativo"
                                    type="button"
                                    class="btn-icon btn-icon-ver"
                                    title="Reativar"
                                    @click="abrirModal(admin, 'reativar')"
                                >
                                    <i class="fa-solid fa-circle-check"></i>
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
                rotulo-itens="administradores"
            />
        </template>

        <!-- Modal de ação com motivo -->
        <AppModal
            :aberto="modalAberto"
            :titulo="adminAlvo ? titulos[acaoAtual] : ''"
            @fechar="fecharModal"
        >
            <template v-if="adminAlvo">
                <p class="modal-desc">
                    Admin: <strong>{{ adminAlvo.nome }} ({{ adminAlvo.email }})</strong>
                </p>

                <AppField label="Motivo" required hint="Mínimo 10 caracteres.">
                    <AppInput
                        v-model="motivo"
                        placeholder="Descreva o motivo da ação…"
                        :disabled="acaoEmCurso"
                    />
                </AppField>

                <p v-if="erroModal" class="modal-erro">{{ erroModal }}</p>
            </template>

            <template #rodape>
                <AppButton variant="secondary" :disabled="acaoEmCurso" @click="fecharModal">
                    Cancelar
                </AppButton>
                <AppButton
                    :variant="acaoAtual === 'desativar' ? 'danger' : 'primary'"
                    :loading="acaoEmCurso"
                    :disabled="!motivoValido"
                    @click="confirmarAcao"
                >
                    {{ confirmarTextos[acaoAtual] }}
                </AppButton>
            </template>
        </AppModal>

        <!-- Modal de exibição de senha temporária (exibida uma vez) -->
        <AppModal
            :aberto="senhaResetadaAberto"
            titulo="Senha temporária gerada"
            @fechar="senhaResetadaAberto = false"
        >
            <p class="modal-desc">
                Copie a senha abaixo e envie ao administrador. Ela não será exibida novamente.
            </p>
            <div class="senha-temp-box">{{ senhaResetadaTemp }}</div>

            <template #rodape>
                <AppButton @click="senhaResetadaAberto = false">
                    Fechar
                </AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.admins-toolbar {
    margin-bottom: 1.25rem;
    max-width: 360px;
}

.admins-carregando {
    color: hsl(var(--muted-foreground));
    padding: 2rem 0;
}

.admins-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    margin-bottom: 1rem;
}

.admins-table-wrap {
    overflow-x: auto;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    margin-bottom: 1rem;
}

.admins-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.875rem;
}

.admins-table th,
.admins-table td {
    padding: 0.625rem 0.875rem;
    text-align: left;
    border-bottom: 1px solid hsl(var(--border));
}

.admins-table th {
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.5);
    font-size: 0.8rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.admins-table tbody tr:last-child td {
    border-bottom: none;
}

.admins-table tbody tr:hover {
    background: hsl(var(--muted) / 0.4);
}

.badge-ativo, .badge-inativo, .badge-reset, .badge-ok {
    display: inline-block;
    padding: 0.15rem 0.5rem;
    border-radius: 9999px;
    font-size: 0.75rem;
    font-weight: 600;
}

.badge-ativo {
    background: hsl(var(--success, 142 76% 36%) / 0.12);
    color: hsl(142 60% 28%);
}

.badge-inativo {
    background: hsl(var(--muted));
    color: hsl(var(--muted-foreground));
}

.badge-reset {
    background: hsl(38 92% 50% / 0.15);
    color: hsl(38 60% 30%);
}

.badge-ok {
    background: hsl(var(--success, 142 76% 36%) / 0.1);
    color: hsl(142 60% 28%);
}

.admins-acoes {
    display: flex;
    gap: 0.25rem;
}

.modal-desc {
    margin: 0 0 0.75rem;
    font-size: 0.9rem;
}

.modal-erro {
    padding: 0.625rem 0.875rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    margin: 0.5rem 0 0;
}

.senha-temp-box {
    font-family: monospace;
    font-size: 1rem;
    padding: 0.75rem 1rem;
    background: hsl(var(--muted));
    border: 1px solid hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
    user-select: all;
    letter-spacing: 0.05em;
}
</style>
