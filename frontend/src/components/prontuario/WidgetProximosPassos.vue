<!--
  Widget flutuante "Próximos passos do atendimento" — substitui o modal central (addendum CA190–CA201).

  Renderizado após salvar evolução com ≥1 ação de conduta marcada.
  Ancorado no canto inferior direito (position: fixed), sem overlay — a página
  continua interativa por trás (CA193).

  Estados:
    - expandido: mostra header + lista de ações
    - minimizado: pílula compacta (ícone + contador X/N)
    - fechado: invisível (emite "fechar"; pendências permanecem no banco — CA198)

  Re-fetch de pendências abertas:
    - ao expandir a partir do minimizado (CA197)
    - ao voltar para a rota do paciente após navegar via link de ação (CA196)
    → controlado pelo watch de `route.fullPath` passado como prop.

  Props:
    - acoesMarcadas: AcaoPendencia[] — ações que geraram pendências (do save)
    - pacienteId: number
    - evolucaoId?: number — para CriarOrcamento gerar pré-preenchimento (CA195)
    - rotaAtual: string — repassa route.fullPath para detectar retorno (CA196)

  Emits:
    - fechar — usuário clicou "Fazer depois" / X (CA198)
-->
<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { useRouter } from "vue-router"
import {
    ACAO_LABELS,
    pendenciaService,
    rotaParaAcao,
    type AcaoPendencia,
    type PendenciaAberta,
} from "@/services/pendenciaService"

// ── Props / emits ──────────────────────────────────────────────────────────────

const props = defineProps<{
    acoesMarcadas: AcaoPendencia[]
    pacienteId: number
    /** F5/R1: id da evolução recém-salva — passado para CriarOrcamento (CA195). */
    evolucaoId?: number
    /** Repassa route.fullPath para detectar retorno ao paciente (CA196). */
    rotaAtual: string
}>()

const emit = defineEmits<{ fechar: [] }>()

const router = useRouter()

// ── Estado interno ─────────────────────────────────────────────────────────────

type EstadoWidget = "expandido" | "minimizado"
const estado = ref<EstadoWidget>("expandido")

/** Pendências ainda abertas, conforme último fetch. */
const abertas = ref<PendenciaAberta[]>([])
const buscando = ref(false)

// ── Computed ───────────────────────────────────────────────────────────────────

const total = computed(() => props.acoesMarcadas.length)

/** Quantas das ações do widget foram concluídas (não estão mais em `abertas`). */
const concluidas = computed(() => {
    const abertasAcoes = new Set(abertas.value.map(p => p.acao))
    return props.acoesMarcadas.filter(a => !abertasAcoes.has(a)).length
})

/** Texto do contador: "X de N concluídas". */
const textoContador = computed(() => `${concluidas.value} de ${total.value} concluídas`)

/** Texto compacto da pílula. */
const textoPilula = computed(() => `${concluidas.value}/${total.value}`)

function estaConcluida(acao: AcaoPendencia): boolean {
    return !abertas.value.some(p => p.acao === acao)
}

function temRota(acao: AcaoPendencia): boolean {
    return rotaParaAcao(props.pacienteId, acao) !== null
}

// ── Fetch ──────────────────────────────────────────────────────────────────────

async function buscarAbertas() {
    buscando.value = true
    try {
        abertas.value = await pendenciaService.listarAbertas(props.pacienteId)
    } finally {
        buscando.value = false
    }
}

// Fetch inicial para saber quais já foram concluídas (caso o widget seja montado
// após uma evolução que tinha pendências pré-existentes já concluídas).
buscarAbertas()

// CA196: re-fetch quando a rota volta para a página do paciente após navegar.
// Usa `rotaAtual` repassado pela view para não depender de watch interno de route.
watch(
    () => props.rotaAtual,
    () => {
        if (estado.value === "expandido") {
            buscarAbertas()
        }
    },
)

// ── Ações ──────────────────────────────────────────────────────────────────────

function minimizar() {
    estado.value = "minimizado"
}

async function expandir() {
    estado.value = "expandido"
    // CA197: re-fetch ao expandir para refletir conclusões ocorridas minimizado.
    await buscarAbertas()
}

function fechar() {
    emit("fechar")
}

function irParaAcao(acao: AcaoPendencia) {
    const rota = rotaParaAcao(props.pacienteId, acao, props.evolucaoId)
    if (rota) {
        router.push(rota)
    }
    // MarcarProcedimentoRealizado: sem rota — conclusão pelo painel (CA66)
}
</script>

<template>
    <Teleport to="body">
        <!-- Pílula minimizada (CA197/CA200) -->
        <button
            v-if="estado === 'minimizado'"
            class="wpp-pilula"
            aria-label="Expandir próximos passos"
            @click="expandir"
        >
            <!-- ícone checklist -->
            <svg width="16" height="16" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                <path d="M9 11l3 3L22 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <span class="wpp-pilula-contador">{{ textoPilula }}</span>
        </button>

        <!-- Widget expandido (CA190/CA192/CA193) -->
        <div
            v-else
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
                    <span class="wpp-contador">{{ textoContador }}</span>
                </div>
                <button
                    class="wpp-btn-icone"
                    aria-label="Minimizar"
                    @click="minimizar"
                >
                    <!-- ícone traço (minimizar) -->
                    <svg width="16" height="16" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                        <path d="M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                    </svg>
                </button>
                <button
                    class="wpp-btn-icone"
                    aria-label="Fechar — Fazer depois"
                    @click="fechar"
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
                    v-for="acao in acoesMarcadas"
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
                    <span class="wpp-item-label">{{ ACAO_LABELS[acao] }}</span>
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
    </Teleport>
</template>

<style scoped>
/* ── Pílula minimizada (CA197/CA200) ──────────────── */
.wpp-pilula {
    position: fixed;
    bottom: 1.25rem;
    right: 1.25rem;
    z-index: 750;
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    background: hsl(var(--primary-hsl));
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
    background: hsl(var(--card));    /* CA192: token HSL embrulhado — nunca var(--card) cru */
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius-lg, var(--radius));
    box-shadow: 0 8px 32px hsl(0 0% 0% / 0.14);
    display: flex;
    flex-direction: column;
    /* Sem overlay — a página continua interativa (CA193). pointer-events só no widget. */
    pointer-events: auto;
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
    color: hsl(var(--primary-hsl));
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
    /* Scroll se houver muitos itens (edge case) */
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
    color: hsl(var(--primary-hsl));
}

.wpp-item--concluida .wpp-item-icone {
    color: hsl(var(--success-hsl, 142 72% 42%));
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
    color: hsl(var(--primary-hsl));
    transition: background 0.1s;
    flex-shrink: 0;
}

.wpp-ir:hover {
    background: hsl(var(--primary-hsl) / 0.1);
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
