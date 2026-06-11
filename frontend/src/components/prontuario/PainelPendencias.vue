<!--
  Painel persistente de pendências do paciente (CA74).
  Exibido em PacienteDetalheView.vue — invisível se nenhuma pendência aberta.
  Cada item tem botão "Fazer agora" (com rota) ou só "Concluir" (manual).
  Conclusão manual exige confirmação simples (CA68).

  Props:
    - pacienteId: number

  Emits:
    - (nenhum — gerencia estado via pendenciaService)
-->
<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRouter } from "vue-router"
import {
    pendenciaService,
    ACAO_LABELS,
    rotaParaAcao,
    type AcaoPendencia,
    type PendenciaAberta,
} from "@/services/pendenciaService"
import { usePermissoesStore } from "@/stores/permissoesStore"
import MarcarProcedimentoRealizadoModal from "./MarcarProcedimentoRealizadoModal.vue"

// ── Props ──────────────────────────────────────────────────────────────────────

const props = defineProps<{ pacienteId: number }>()

const router = useRouter()

// ── Estado ─────────────────────────────────────────────────────────────────────

const pendencias = ref<PendenciaAberta[]>([])
const carregando = ref(false)
const confirmandoId = ref<number | null>(null) // pendenciaId aguardando confirmação
const concluindoId = ref<number | null>(null)  // pendenciaId em curso (spinner)

// ── Modal MarcarProcedimentoRealizado (F4/CA88) ───────────────────────────────
const modalMarcarAberto = ref(false)
const modalMarcarPendenciaId = ref<number | null>(null)

// ── Dados ──────────────────────────────────────────────────────────────────────

async function carregar() {
    carregando.value = true
    try {
        pendencias.value = await pendenciaService.listarAbertas(props.pacienteId)
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)

// ── Computed ───────────────────────────────────────────────────────────────────

/** Renderiza painel apenas se houver pendências abertas (CA74). */
const temPendencias = computed(() => pendencias.value.length > 0)

// ── Permissões (CA70) ──────────────────────────────────────────────────────────

const permissoes = usePermissoesStore()

/** Pode concluir pendência manualmente (exige prontuario.editar). */
const podeEditar = computed(() => permissoes.pode("prontuario.editar"))

// ── Ações ──────────────────────────────────────────────────────────────────────

function irParaAcao(acao: AcaoPendencia, pendenciaId: number, evolucaoId?: number) {
    // MarcarProcedimentoRealizado abre modal próprio em vez de navegar (F4/CA88)
    if (acao === "MarcarProcedimentoRealizado") {
        modalMarcarPendenciaId.value = pendenciaId
        modalMarcarAberto.value = true
        return
    }
    // F5/R1: CriarOrcamento passa evolucaoId para pré-preenchimento (CA97/CA98)
    const rota = rotaParaAcao(props.pacienteId, acao, evolucaoId)
    if (rota) router.push(rota)
}

function aoMarcarConcluido(cobrancaId: number) {
    modalMarcarAberto.value = false
    // Remove da lista local a pendência que foi concluída pelo modal
    if (modalMarcarPendenciaId.value !== null) {
        pendencias.value = pendencias.value.filter(p => p.id !== modalMarcarPendenciaId.value)
        modalMarcarPendenciaId.value = null
    }
}

function temBotaoFazerAgora(acao: AcaoPendencia): boolean {
    // MarcarProcedimentoRealizado tem modal próprio — sempre mostra "Fazer agora"
    if (acao === "MarcarProcedimentoRealizado") return true
    // CriarOrcamento sempre tem rota (com ou sem evolucaoId)
    return rotaParaAcao(props.pacienteId, acao) !== null
}

function solicitarConclusao(pendenciaId: number) {
    confirmandoId.value = pendenciaId
}

function cancelarConclusao() {
    confirmandoId.value = null
}

async function confirmarConclusao(pendenciaId: number) {
    confirmandoId.value = null
    concluindoId.value = pendenciaId
    try {
        await pendenciaService.concluirManual(props.pacienteId, pendenciaId)
        // Remove da lista local imediatamente (otimístico)
        pendencias.value = pendencias.value.filter(p => p.id !== pendenciaId)
    } finally {
        concluindoId.value = null
    }
}

// Expõe reload para o pai (após salvar evolução nova)
defineExpose({ recarregar: carregar })
</script>

<template>
    <div v-if="temPendencias" class="painel-pendencias">
        <div class="pp-header">
            <span class="pp-icone" aria-hidden="true">
                <svg width="16" height="16" fill="none" viewBox="0 0 24 24">
                    <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
            </span>
            <span class="pp-titulo">
                Pendências do atendimento
                <span class="pp-badge">{{ pendencias.length }}</span>
            </span>
        </div>

        <ul class="pp-lista">
            <li
                v-for="pend in pendencias"
                :key="pend.id"
                class="pp-item"
            >
                <span class="ppi-label">{{ ACAO_LABELS[pend.acao as AcaoPendencia] }}</span>

                <!-- Confirmação inline (CA70: só com prontuario.editar) -->
                <template v-if="podeEditar && confirmandoId === pend.id">
                    <span class="ppi-confirmar-texto">Confirmar conclusão?</span>
                    <button class="btn-icon-sm btn-icon-sm--danger" @click="confirmarConclusao(pend.id)">
                        Sim
                    </button>
                    <button class="btn-icon-sm" @click="cancelarConclusao">
                        Não
                    </button>
                </template>

                <!-- Ações normais -->
                <template v-else>
                    <button
                        v-if="temBotaoFazerAgora(pend.acao as AcaoPendencia)"
                        class="ppi-ir"
                        @click="irParaAcao(pend.acao as AcaoPendencia, pend.id, pend.evolucaoId)"
                    >
                        Fazer agora
                        <svg width="12" height="12" fill="none" viewBox="0 0 24 24">
                            <path d="M5 12h14M12 5l7 7-7 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        </svg>
                    </button>
                    <!-- CA70: botão Concluir só visível com prontuario.editar.
                         MarcarProcedimentoRealizado usa modal próprio (F4) — não mostra "Concluir" manual. -->
                    <button
                        v-if="podeEditar && pend.acao !== 'MarcarProcedimentoRealizado'"
                        class="ppi-concluir"
                        :disabled="concluindoId === pend.id"
                        @click="solicitarConclusao(pend.id)"
                    >
                        {{ concluindoId === pend.id ? "..." : "Concluir" }}
                    </button>
                </template>
            </li>
        </ul>
    </div>

    <!-- Modal F4: confirmação de procedimento realizado (CA88) -->
    <MarcarProcedimentoRealizadoModal
        v-if="modalMarcarPendenciaId !== null"
        :aberto="modalMarcarAberto"
        :paciente-id="pacienteId"
        :pendencia-id="modalMarcarPendenciaId"
        @fechar="modalMarcarAberto = false"
        @concluido="aoMarcarConcluido"
    />
</template>

<style scoped>
.painel-pendencias {
    background: hsl(var(--warning-hsl, 38 92% 50%) / 0.06);
    border: 1px solid hsl(var(--warning-hsl, 38 92% 50%) / 0.35);
    border-radius: var(--radius);
    padding: 0.875rem 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}

/* ── Header ──────────────────────────────────── */
.pp-header {
    display: flex;
    align-items: center;
    gap: 0.4rem;
}

.pp-icone {
    color: hsl(var(--warning-hsl, 38 92% 50%));
    display: flex;
    flex-shrink: 0;
}

.pp-titulo {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: var(--text);
    display: flex;
    align-items: center;
    gap: 0.4rem;
}

.pp-badge {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-medium);
    background: hsl(var(--warning-hsl, 38 92% 50%) / 0.2);
    color: hsl(var(--warning-hsl, 38 92% 50%));
    padding: 0.1rem 0.4rem;
    border-radius: 999px;
    line-height: 1.4;
}

/* ── Lista de itens ──────────────────────────── */
.pp-lista {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
}

.pp-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.45rem 0.6rem;
    background: var(--card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    font-size: var(--text-sm);
}

.ppi-label {
    flex: 1;
    color: var(--text);
}

.ppi-confirmar-texto {
    font-size: var(--text-sm);
    color: var(--text-muted);
}

.ppi-ir {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    color: hsl(var(--primary-hsl));
    background: transparent;
    border: none;
    cursor: pointer;
    padding: 0.2rem 0.4rem;
    border-radius: var(--radius);
    white-space: nowrap;
    transition: background 0.1s;
}

.ppi-ir:hover {
    background: hsl(var(--primary-hsl) / 0.08);
}

.ppi-concluir {
    font-size: var(--text-sm);
    color: var(--text-muted);
    background: transparent;
    border: 1px solid var(--border);
    cursor: pointer;
    padding: 0.2rem 0.5rem;
    border-radius: var(--radius);
    white-space: nowrap;
    transition: border-color 0.1s, color 0.1s;
}

.ppi-concluir:hover:not(:disabled) {
    border-color: hsl(var(--primary-hsl) / 0.5);
    color: hsl(var(--primary-hsl));
}

.ppi-concluir:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

/* Botões de confirmação inline */
.btn-icon-sm {
    font-size: var(--text-sm);
    padding: 0.2rem 0.5rem;
    border: 1px solid var(--border);
    background: transparent;
    border-radius: var(--radius);
    cursor: pointer;
    color: var(--text-muted);
    transition: border-color 0.1s;
}

.btn-icon-sm--danger {
    color: hsl(var(--danger-hsl, 0 70% 50%));
    border-color: hsl(var(--danger-hsl, 0 70% 50%) / 0.4);
}

.btn-icon-sm--danger:hover {
    background: hsl(var(--danger-hsl, 0 70% 50%) / 0.08);
}
</style>
