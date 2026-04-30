<script setup lang="ts">
/**
 * SolicitarVinculoView — profissional envia solicitacao para ingressar em um estabelecimento.
 * Mostra tambem o historico de solicitacoes enviadas (com status Pendente/Aprovada/Recusada).
 */
import { ref, onMounted } from "vue"
import {
    solicitacaoVinculoService,
    type SolicitacaoVinculo,
    type StatusSolicitacao,
} from "@/services/solicitacaoVinculoService"
import {
    AppPageHeader, AppButton, AppBadge, AppCard, AppField, AppInput, AppTextarea,
} from "@/components/ui"

// ─── Formulario de nova solicitacao ──────────────────────────────────────────

const estabelecimentoIdStr = ref("")
const mensagem             = ref("")
const enviando             = ref(false)
const erroEnvio            = ref<string | null>(null)
const msgSucesso           = ref<string | null>(null)

async function enviar() {
    const estabId = parseInt(estabelecimentoIdStr.value, 10)
    if (!estabId || isNaN(estabId)) {
        erroEnvio.value = "Informe o ID do estabelecimento."
        return
    }

    enviando.value = true
    erroEnvio.value = null
    msgSucesso.value = null
    try {
        await solicitacaoVinculoService.criar({
            estabelecimentoId: estabId,
            mensagem: mensagem.value || undefined,
        })
        msgSucesso.value = "Solicitacao enviada. O estabelecimento sera notificado."
        estabelecimentoIdStr.value = ""
        mensagem.value = ""
        await carregarMinhas()
    } catch (e: any) {
        erroEnvio.value = e?.response?.data?.mensagem ?? "Erro ao enviar solicitacao."
    } finally {
        enviando.value = false
    }
}

// ─── Historico de solicitacoes ────────────────────────────────────────────────

const minhas     = ref<SolicitacaoVinculo[]>([])
const carregando = ref(false)
const erroLista  = ref<string | null>(null)

async function carregarMinhas() {
    carregando.value = true
    erroLista.value = null
    try {
        minhas.value = await solicitacaoVinculoService.listarMinhas()
    } catch (e: any) {
        erroLista.value = e?.response?.data?.mensagem ?? "Erro ao carregar historico."
    } finally {
        carregando.value = false
    }
}

// ─── Cancelar solicitacao ─────────────────────────────────────────────────────

const cancelando = ref<Set<number>>(new Set())

async function cancelar(s: SolicitacaoVinculo) {
    if (!confirm(`Cancelar solicitacao para ${s.estabelecimentoNome ?? s.estabelecimentoId}?`)) return
    cancelando.value.add(s.id)
    try {
        await solicitacaoVinculoService.cancelar(s.id)
        await carregarMinhas()
    } catch (e: any) {
        erroLista.value = e?.response?.data?.mensagem ?? "Erro ao cancelar."
    } finally {
        cancelando.value.delete(s.id)
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

onMounted(carregarMinhas)
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            titulo="Solicitar vinculo"
            subtitulo="Envie uma solicitacao para se vincular a um estabelecimento."
        />

        <!-- Formulario -->
        <AppCard title="Nova solicitacao">
            <div class="form-grid">
                <AppField label="ID do estabelecimento" for="estab-id" :required="true" hint="Informe o codigo numerico fornecido pelo gestor do estabelecimento.">
                    <AppInput
                        id="estab-id"
                        v-model="estabelecimentoIdStr"
                        type="number"
                        placeholder="Ex: 42"
                        autocomplete="off"
                    />
                </AppField>
                <AppField label="Mensagem (opcional)" for="mensagem">
                    <AppTextarea
                        id="mensagem"
                        v-model="mensagem"
                        :rows="3"
                        placeholder="Apresente-se brevemente ao gestor do estabelecimento..."
                    />
                </AppField>
            </div>
            <p v-if="erroEnvio" class="msg-erro" role="alert">{{ erroEnvio }}</p>
            <p v-if="msgSucesso" class="msg-ok" role="status">{{ msgSucesso }}</p>
            <template #footer>
                <AppButton
                    icon="fa-solid fa-paper-plane"
                    :loading="enviando"
                    :disabled="!estabelecimentoIdStr"
                    @click="enviar"
                >
                    Enviar solicitacao
                </AppButton>
            </template>
        </AppCard>

        <!-- Historico -->
        <AppCard title="Minhas solicitacoes" subtitle="Historico de solicitacoes enviadas por voce.">
            <div v-if="carregando" class="estado-msg">
                <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
                Carregando...
            </div>
            <p v-else-if="erroLista" class="msg-erro" role="alert">{{ erroLista }}</p>
            <div v-else-if="minhas.length === 0" class="estado-msg">
                Voce ainda nao enviou solicitacoes.
            </div>
            <div v-else class="lista-solicitacoes">
                <div v-for="s in minhas" :key="s.id" class="item-solicitacao">
                    <div class="item-info">
                        <span class="item-estab">{{ s.estabelecimentoNome ?? `Estabelecimento #${s.estabelecimentoId}` }}</span>
                        <span class="item-data">Enviada em {{ fmtData(s.criadaEm) }}</span>
                        <span v-if="s.motivoRecusa" class="item-motivo">
                            Motivo: {{ s.motivoRecusa }}
                        </span>
                    </div>
                    <div class="item-acoes">
                        <AppBadge
                            :variant="BADGE_STATUS[s.status].variant"
                            :label="BADGE_STATUS[s.status].label"
                        />
                        <AppButton
                            v-if="s.status === 'Pendente'"
                            variant="ghost"
                            size="sm"
                            :loading="cancelando.has(s.id)"
                            @click="cancelar(s)"
                        >
                            Cancelar
                        </AppButton>
                    </div>
                </div>
            </div>
        </AppCard>
    </main>
</template>

<style scoped>
.form-grid { display: flex; flex-direction: column; gap: 1rem; }

.msg-erro { color: hsl(var(--error));   font-size: 0.875em; margin: 0.5rem 0 0; }
.msg-ok   { color: hsl(var(--success)); font-size: 0.875em; margin: 0.5rem 0 0; }

.estado-msg {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-muted);
    font-size: 0.9em;
    padding: 0.75rem 0;
}

.lista-solicitacoes { display: flex; flex-direction: column; gap: 0.5rem; }

.item-solicitacao {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1rem;
    padding: 0.85rem 1rem;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
}

.item-info {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
    flex: 1;
    min-width: 0;
}
.item-estab { font-weight: 600; font-size: 0.95em; }
.item-data  { font-size: 0.78em; color: var(--text-muted); }
.item-motivo { font-size: 0.8em; color: hsl(var(--destructive)); font-style: italic; }

.item-acoes { display: flex; align-items: center; gap: 0.5rem; flex-shrink: 0; }
</style>
