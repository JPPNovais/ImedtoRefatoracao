<script setup lang="ts">
/**
 * SolicitacoesRecebidasView — dono do estabelecimento ve e gerencia solicitacoes
 * de vinculo enviadas por profissionais.
 */
import { ref, onMounted } from "vue"
import {
    solicitacaoVinculoService,
    type SolicitacaoVinculo,
    type StatusSolicitacao,
} from "@/services/solicitacaoVinculoService"
import {
    AppPageHeader, AppButton, AppBadge, AppModal, AppField, AppTextarea, AppEmptyState,
} from "@/components/ui"

const solicitacoes = ref<SolicitacaoVinculo[]>([])
const carregando   = ref(false)
const erro         = ref<string | null>(null)
const msg          = ref<string | null>(null)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        solicitacoes.value = await solicitacaoVinculoService.listarRecebidas()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar solicitacoes."
    } finally {
        carregando.value = false
    }
}

// ─── Aprovar ──────────────────────────────────────────────────────────────────

const aprovando = ref<Set<number>>(new Set())

async function aprovar(s: SolicitacaoVinculo) {
    aprovando.value.add(s.id)
    msg.value = null
    try {
        await solicitacaoVinculoService.aprovar(s.id)
        msg.value = `Solicitacao de ${s.profissionalNome ?? "profissional"} aprovada. Vinculo criado.`
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao aprovar."
    } finally {
        aprovando.value.delete(s.id)
    }
}

// ─── Recusar ──────────────────────────────────────────────────────────────────

const recusando          = ref(false)
const confirmandoRecusar = ref<SolicitacaoVinculo | null>(null)
const motivoRecusa       = ref("")

async function confirmarRecusa() {
    if (!confirmandoRecusar.value) return
    recusando.value = true
    msg.value = null
    try {
        await solicitacaoVinculoService.recusar(
            confirmandoRecusar.value.id,
            motivoRecusa.value || undefined,
        )
        msg.value = "Solicitacao recusada."
        confirmandoRecusar.value = null
        motivoRecusa.value = ""
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao recusar."
    } finally {
        recusando.value = false
    }
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

const BADGE_STATUS: Record<StatusSolicitacao, { variant: "warning" | "success" | "error" | "muted"; label: string }> = {
    Pendente:  { variant: "warning", label: "Pendente" },
    Aprovada:  { variant: "success", label: "Aprovada" },
    Recusada:  { variant: "error",   label: "Recusada" },
    Cancelada: { variant: "muted",   label: "Cancelada" },
}

function fmtData(iso: string) {
    try { return new Date(iso).toLocaleDateString("pt-BR") }
    catch { return iso }
}

onMounted(carregar)
</script>

<template>
    <main class="app-page">
        <AppPageHeader
            titulo="Solicitacoes de vinculo"
            subtitulo="Profissionais que solicitaram acesso a este estabelecimento."
        >
            <template #acoes>
                <AppButton variant="secondary" icon="fa-solid fa-rotate" @click="carregar">
                    Atualizar
                </AppButton>
            </template>
        </AppPageHeader>

        <p v-if="erro" class="msg-erro" role="alert">{{ erro }}</p>
        <p v-if="msg"  class="msg-ok"   role="status">{{ msg }}</p>

        <div v-if="carregando" class="estado-msg">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando...
        </div>

        <AppEmptyState
            v-else-if="solicitacoes.length === 0"
            icone="fa-solid fa-inbox"
            titulo="Nenhuma solicitacao recebida"
            descricao="Quando um profissional solicitar vinculo, a solicitacao aparecera aqui."
        />

        <div v-else class="tabela-wrapper">
            <table class="tabela">
                <thead>
                    <tr>
                        <th>Profissional</th>
                        <th>Data</th>
                        <th>Mensagem</th>
                        <th>Status</th>
                        <th class="acoes-th">Acoes</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="s in solicitacoes" :key="s.id">
                        <td class="td-profissional">
                            {{ s.profissionalNome ?? s.profissionalUsuarioId }}
                        </td>
                        <td class="td-data">{{ fmtData(s.criadaEm) }}</td>
                        <td class="td-msg">
                            <span v-if="s.mensagem" :title="s.mensagem" class="msg-truncada">{{ s.mensagem }}</span>
                            <span v-else class="texto-muted">—</span>
                        </td>
                        <td>
                            <AppBadge
                                :variant="BADGE_STATUS[s.status].variant"
                                :label="BADGE_STATUS[s.status].label"
                            />
                        </td>
                        <td class="acoes">
                            <template v-if="s.status === 'Pendente'">
                                <button
                                    class="btn-icon btn-icon-ver"
                                    title="Aprovar"
                                    :disabled="aprovando.has(s.id)"
                                    @click="aprovar(s)"
                                >
                                    <i class="fa-solid fa-check" />
                                </button>
                                <button
                                    class="btn-icon btn-icon-excluir"
                                    title="Recusar"
                                    @click="confirmandoRecusar = s; motivoRecusa = ''"
                                >
                                    <i class="fa-solid fa-xmark" />
                                </button>
                            </template>
                            <span v-else class="texto-muted">—</span>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Modal recusar -->
        <AppModal
            :aberto="!!confirmandoRecusar"
            titulo="Recusar solicitacao?"
            largura="sm"
            @fechar="confirmandoRecusar = null; motivoRecusa = ''"
        >
            <p class="modal-desc">
                Recusar solicitacao de
                <strong>{{ confirmandoRecusar?.profissionalNome ?? "profissional" }}</strong>.
                O profissional sera notificado.
            </p>
            <AppField label="Motivo (opcional)" for="motivo-recusa" hint="Sera enviado ao profissional.">
                <AppTextarea
                    id="motivo-recusa"
                    v-model="motivoRecusa"
                    :rows="3"
                    placeholder="Ex: Nao ha vagas no momento."
                />
            </AppField>
            <template #rodape>
                <AppButton variant="secondary" @click="confirmandoRecusar = null; motivoRecusa = ''">Voltar</AppButton>
                <AppButton variant="danger" :loading="recusando" @click="confirmarRecusa">Recusar</AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.msg-erro { color: hsl(var(--error));   font-size: 0.875em; margin: 0 0 0.75rem; }
.msg-ok   { color: hsl(var(--success)); font-size: 0.875em; margin: 0 0 0.75rem; }

.estado-msg {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-muted);
    font-size: 0.9em;
    padding: 2rem 0;
}

.tabela-wrapper { overflow-x: auto; }

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.9em;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    overflow: hidden;
}
.tabela th {
    text-align: left;
    padding: 0.65rem 1rem;
    background: hsl(var(--muted));
    border-bottom: 1px solid hsl(var(--border));
    font-weight: 600;
    font-size: 0.8em;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: hsl(var(--muted-foreground));
}
.tabela td {
    padding: 0.75rem 1rem;
    border-bottom: 1px solid hsl(var(--border));
    vertical-align: middle;
}
.tabela tr:last-child td { border-bottom: none; }
.tabela tr:hover td { background: hsl(var(--muted) / 0.4); }

.td-profissional { font-weight: 600; }
.td-data         { white-space: nowrap; color: var(--text-muted); font-size: 0.85em; }
.td-msg          { max-width: 260px; }
.acoes-th        { width: 80px; }
.acoes           { display: flex; gap: 0.35rem; }

.msg-truncada {
    display: block;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 240px;
    font-size: 0.85em;
    color: var(--text-muted);
}
.texto-muted { color: var(--text-muted); }

.modal-desc {
    font-size: 0.9em;
    margin: 0 0 1rem;
}
</style>
