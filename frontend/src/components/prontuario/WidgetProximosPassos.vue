<!--
  Widget flutuante "Próximos passos do atendimento" — global (addendum 2, CA202–CA215).

  Montado UMA VEZ em AppLayout.vue (R25). Estado e persistência geridos por
  proximosPassosStore (sessionStorage, R26). ProntuarioView deixou de montá-lo
  diretamente; apenas dispara a store ao salvar (CA202/CA213).

  Estados:
    expandido  → mostra header + lista de ações
    minimizado → pílula compacta (cor primária sólida + contador X/N)
    concluido  → transição breve "Tudo concluído" → some sozinho (CA204)
    fechado    → invisível

  Fechar:
    - Com ≥1 pendência aberta → AppConfirmDialog (R28/CA206)
    - Sem pendências abertas  → fecha direto (CA207)

  Re-fetch: a cada troca de route.fullPath (CA205).
-->
<script setup lang="ts">
import { ref, watch, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useProximosPassosStore } from "@/stores/proximosPassosStore"
import { ACAO_LABELS, rotaParaAcao } from "@/services/pendenciaService"
import { AppConfirmDialog } from "@/components/ui"

const store  = useProximosPassosStore()
const route  = useRoute()
const router = useRouter()

// ── Confirmação ao fechar (R28/CA206) ─────────────────────────────────────────
const confirmandoFechar = ref(false)

function solicitarFechar() {
    if (store.temAberta) {
        confirmandoFechar.value = true
    } else {
        store.fechar()
    }
}

function confirmarFechar() {
    confirmandoFechar.value = false
    store.fechar()
}

function cancelarFechar() {
    confirmandoFechar.value = false
}

// ── Navegação para ação (CA195) ────────────────────────────────────────────────
function irParaAcao(acao: string) {
    const rota = rotaParaAcao(
        store.pacienteId!,
        acao as Parameters<typeof rotaParaAcao>[1],
        store.evolucaoId,
    )
    if (rota) router.push(rota)
}

// ── CA205: re-fetch a cada troca de rota ─────────────────────────────────────
watch(
    () => route.fullPath,
    async () => {
        if (store.visivel) {
            await store.atualizarAbertas()
        }
    },
)

// ── CA204: sumiço automático 2s após "concluido" ──────────────────────────────
watch(
    () => store.estado,
    (novoEstado) => {
        if (novoEstado === "concluido") {
            setTimeout(() => {
                store.fechar()
            }, 2000)
        }
    },
)

// ── Expandir a partir da pílula (CA197) ───────────────────────────────────────
async function expandir() {
    store.expandir()
    await store.atualizarAbertas()
}

// ── Indicador de ação concluída ───────────────────────────────────────────────
function estaConcluida(acao: string) {
    return store.estaConcluidaAcao(acao as Parameters<typeof store.estaConcluidaAcao>[0])
}

function temRota(acao: string) {
    return rotaParaAcao(
        store.pacienteId ?? 0,
        acao as Parameters<typeof rotaParaAcao>[1],
    ) !== null
}
</script>

<template>
    <Teleport to="body">
        <!-- Pílula minimizada (CA208/CA209) -->
        <button
            v-if="store.estado === 'minimizado'"
            class="wpp-pilula"
            :aria-label="`Próximos passos: ${store.concluidas} de ${store.total} concluídas — clique para expandir`"
            @click="expandir"
        >
            <!-- ícone checklist -->
            <svg width="16" height="16" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                <path d="M9 11l3 3L22 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <span class="wpp-pilula-contador">{{ store.concluidas }}/{{ store.total }}</span>
        </button>

        <!-- Widget "tudo concluído" — feedback breve (CA204) -->
        <div
            v-else-if="store.estado === 'concluido'"
            class="wpp-widget wpp-widget--concluido"
            role="status"
            aria-live="polite"
        >
            <span class="wpp-concluido-icone" aria-hidden="true">
                <svg width="20" height="20" fill="none" viewBox="0 0 24 24">
                    <path d="M20 6L9 17l-5-5" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
            </span>
            <span class="wpp-concluido-texto">Tudo concluído!</span>
        </div>

        <!-- Widget expandido (CA190/CA192/CA193/CA214) -->
        <div
            v-else-if="store.estado === 'expandido'"
            class="wpp-widget"
            role="complementary"
            aria-label="Próximos passos do atendimento"
        >
            <!-- Header: título + contador + botões (CA194) -->
            <header class="wpp-header">
                <span class="wpp-icone" aria-hidden="true">
                    <svg width="18" height="18" fill="none" viewBox="0 0 24 24">
                        <path d="M9 11l3 3L22 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        <path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                </span>
                <div class="wpp-titulo-grupo">
                    <span class="wpp-titulo">Próximos passos</span>
                    <span class="wpp-contador">{{ store.concluidas }} de {{ store.total }} concluídas</span>
                </div>
                <button
                    class="wpp-btn-icone"
                    aria-label="Minimizar"
                    @click="store.minimizar()"
                >
                    <!-- ícone traço (minimizar) -->
                    <svg width="16" height="16" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                        <path d="M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                    </svg>
                </button>
                <button
                    class="wpp-btn-icone"
                    aria-label="Fechar — Fazer depois"
                    @click="solicitarFechar"
                >
                    <!-- ícone X (fechar) -->
                    <svg width="16" height="16" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                        <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                    </svg>
                </button>
            </header>

            <!-- Corpo: lista de ações (CA195/CA196) -->
            <ul class="wpp-lista">
                <li
                    v-for="acao in store.acoesMarcadas"
                    :key="acao"
                    class="wpp-item"
                    :class="{ 'wpp-item--concluida': estaConcluida(acao) }"
                >
                    <span class="wpp-item-icone" aria-hidden="true">
                        <!-- check quando concluída, círculo quando pendente -->
                        <svg v-if="estaConcluida(acao)" width="14" height="14" fill="none" viewBox="0 0 24 24">
                            <path d="M20 6L9 17l-5-5" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
                        </svg>
                        <svg v-else width="14" height="14" fill="none" viewBox="0 0 24 24">
                            <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="2"/>
                        </svg>
                    </span>
                    <span class="wpp-item-label">{{ ACAO_LABELS[acao as keyof typeof ACAO_LABELS] }}</span>
                    <!-- Botão "ir" só para ações abertas com rota (CA195) -->
                    <button
                        v-if="!estaConcluida(acao) && temRota(acao)"
                        class="wpp-ir"
                        @click="irParaAcao(acao)"
                    >
                        <svg width="13" height="13" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                            <path d="M5 12h14M12 5l7 7-7 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        </svg>
                    </button>
                    <!-- MarcarProcedimentoRealizado: sem rota — conclusão pelo painel -->
                    <span
                        v-else-if="!estaConcluida(acao) && !temRota(acao)"
                        class="wpp-manual"
                    >
                        Pelo painel
                    </span>
                </li>
            </ul>

            <!-- Rodapé informativo -->
            <div class="wpp-rodape">
                <svg width="12" height="12" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                    <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                    <path d="M12 8v4M12 16h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                </svg>
                Visíveis no painel do paciente
            </div>
        </div>

        <!-- Diálogo de confirmação ao fechar com pendências (R28/CA206) -->
        <AppConfirmDialog
            v-model:aberto="confirmandoFechar"
            titulo="Fechar sem concluir?"
            mensagem="Fechar sem concluir as pendências? Elas continuam no painel do paciente."
            confirmar-rotulo="Fechar assim mesmo"
            cancelar-rotulo="Manter aberto"
            variante="primary"
            @confirmar="confirmarFechar"
            @cancelar="cancelarFechar"
        />
    </Teleport>
</template>

<style scoped>
/* ── Pílula minimizada (CA208/CA209) ──────────────────── */
.wpp-pilula {
    position: fixed;
    bottom: 1.25rem;
    right: 1.25rem;
    z-index: 750;
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    /* CA208: fundo primário sólido — token correto do DS é --primary (HSL sem hsl()) */
    background: hsl(var(--primary));
    color: hsl(0 0% 100%);
    border: none;
    border-radius: 999px;
    padding: 0.5rem 0.875rem;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    cursor: pointer;
    box-shadow: 0 4px 16px hsl(0 0% 0% / 0.18);
    transition: opacity 0.15s, transform 0.15s;
}

.wpp-pilula:hover {
    opacity: 0.9;
    transform: translateY(-1px);
}

.wpp-pilula-contador {
    line-height: 1;
}

/* ── Widget expandido (CA190/CA192/CA193) ─────────── */
.wpp-widget {
    position: fixed;
    bottom: 1.25rem;
    right: 1.25rem;
    z-index: 750;
    width: 320px;
    max-width: calc(100vw - 2rem);   /* CA200: não excede a largura útil */
    background: hsl(var(--card));    /* CA192: token HSL embrulhado */
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius-lg, var(--radius));
    box-shadow: 0 8px 32px hsl(0 0% 0% / 0.14);
    display: flex;
    flex-direction: column;
    pointer-events: auto;
}

/* ── "Tudo concluído" — feedback breve (CA204) ───── */
.wpp-widget--concluido {
    flex-direction: row;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    width: auto;
    min-width: 200px;
}

.wpp-concluido-icone {
    display: flex;
    color: hsl(var(--success, 142 72% 42%));
}

.wpp-concluido-texto {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--foreground));
}

/* ── Header ───────────────────────────────────────── */
.wpp-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 0.875rem;
    border-bottom: 1px solid hsl(var(--border));
}

.wpp-icone {
    color: hsl(var(--primary));
    display: flex;
    flex-shrink: 0;
}

.wpp-titulo-grupo {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-width: 0;
}

.wpp-titulo {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--foreground));
    line-height: 1.3;
}

.wpp-contador {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    line-height: 1.3;
}

.wpp-btn-icone {
    display: flex;
    align-items: center;
    justify-content: center;
    background: transparent;
    border: none;
    cursor: pointer;
    padding: 0.25rem;
    border-radius: var(--radius);
    color: hsl(var(--muted-foreground));
    transition: color 0.1s, background 0.1s;
    flex-shrink: 0;
}

.wpp-btn-icone:hover {
    color: hsl(var(--foreground));
    background: hsl(var(--muted));
}

/* ── Lista de ações ───────────────────────────────── */
.wpp-lista {
    list-style: none;
    margin: 0;
    padding: 0.5rem 0.625rem;
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
    max-height: 280px;
    overflow-y: auto;
}

.wpp-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.4rem 0.5rem;
    background: hsl(var(--muted));
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
}

.wpp-item--concluida {
    opacity: 0.6;
}

.wpp-item--concluida .wpp-item-label {
    text-decoration: line-through;
}

.wpp-item-icone {
    display: flex;
    flex-shrink: 0;
    color: hsl(var(--primary));
}

.wpp-item--concluida .wpp-item-icone {
    color: hsl(var(--success, 142 72% 42%));
}

.wpp-item-label {
    font-size: var(--text-sm);
    color: hsl(var(--foreground));
    flex: 1;
    line-height: 1.3;
}

/* Botão "ir" (seta) */
.wpp-ir {
    display: flex;
    align-items: center;
    justify-content: center;
    background: transparent;
    border: none;
    cursor: pointer;
    padding: 0.25rem;
    border-radius: var(--radius);
    color: hsl(var(--primary));
    transition: background 0.1s;
    flex-shrink: 0;
}

.wpp-ir:hover {
    background: hsl(var(--primary) / 0.1);
}

/* "Pelo painel" — MarcarProcedimentoRealizado sem rota */
.wpp-manual {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    white-space: nowrap;
    flex-shrink: 0;
}

/* ── Rodapé informativo ───────────────────────────── */
.wpp-rodape {
    display: flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0.5rem 0.875rem;
    border-top: 1px solid hsl(var(--border));
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
}

/* ── Mobile (CA200) ───────────────────────────────── */
@media (max-width: 480px) {
    .wpp-widget {
        bottom: 0.75rem;
        right: 0.75rem;
        width: calc(100vw - 1.5rem);
    }

    .wpp-pilula {
        bottom: 0.75rem;
        right: 0.75rem;
    }
}
</style>
