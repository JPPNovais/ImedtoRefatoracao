<!--
  Modal "Próximos passos do atendimento" — exibido após salvar evolução com
  conduta checklist (≥1 ação marcada). Mostra as pendências geradas, com
  atalhos diretos para cada ação e barra de progresso.

  Props:
    - aberto: boolean — controla visibilidade
    - acoesMarcadas: AcaoPendencia[] — ações que geraram pendências
    - pacienteId: number — para montar as rotas de atalho (rotaParaAcao)

  Emits:
    - fechar — botão "Fazer depois" ou X
-->
<script setup lang="ts">
import { computed } from "vue"
import { useRouter } from "vue-router"
import { ACAO_LABELS, rotaParaAcao, type AcaoPendencia } from "@/services/pendenciaService"

// ── Props / emits ──────────────────────────────────────────────────────────────

const props = defineProps<{
    aberto: boolean
    acoesMarcadas: AcaoPendencia[]
    pacienteId: number
}>()
const emit = defineEmits<{ fechar: [] }>()

const router = useRouter()

// ── Lógica ─────────────────────────────────────────────────────────────────────

const totalAcoes = computed(() => props.acoesMarcadas.length)

function irParaAcao(acao: AcaoPendencia) {
    const rota = rotaParaAcao(props.pacienteId, acao)
    if (rota) {
        emit("fechar")
        router.push(rota)
    }
    // MarcarProcedimentoRealizado: sem rota, só conclui pelo painel (CA66)
}

function temRota(acao: AcaoPendencia): boolean {
    return rotaParaAcao(props.pacienteId, acao) !== null
}
</script>

<template>
    <Teleport to="body">
        <div
            v-if="aberto"
            class="modal-overlay"
            @click.self="emit('fechar')"
        >
            <div class="proximos-modal" role="dialog" aria-modal="true" aria-labelledby="ns-titulo">
                <header class="ns-header">
                    <span class="ns-icone" aria-hidden="true">
                        <svg width="20" height="20" fill="none" viewBox="0 0 24 24">
                            <path d="M9 11l3 3L22 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                            <path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        </svg>
                    </span>
                    <h3 id="ns-titulo" class="ns-titulo">Próximos passos do atendimento</h3>
                    <button class="ns-fechar" aria-label="Fechar" @click="emit('fechar')">
                        <svg width="18" height="18" fill="none" viewBox="0 0 24 24">
                            <path d="M18 6L6 18M6 6l12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                        </svg>
                    </button>
                </header>

                <div class="ns-corpo">
                    <p class="ns-subtitulo">
                        Conclua as pendências geradas nesta evolução. Você pode fazer depois — elas não se perdem.
                    </p>

                    <ul class="ns-lista">
                        <li
                            v-for="acao in acoesMarcadas"
                            :key="acao"
                            class="ns-item"
                        >
                            <span class="nsi-label">{{ ACAO_LABELS[acao] }}</span>
                            <button
                                v-if="temRota(acao)"
                                class="nsi-ir"
                                @click="irParaAcao(acao)"
                            >
                                {{ ACAO_LABELS[acao] }}
                                <svg width="14" height="14" fill="none" viewBox="0 0 24 24">
                                    <path d="M5 12h14M12 5l7 7-7 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                                </svg>
                            </button>
                            <span v-else class="nsi-manual">Conclusão manual pelo painel</span>
                        </li>
                    </ul>

                    <div class="ns-rodape-info">
                        <svg width="14" height="14" fill="none" viewBox="0 0 24 24">
                            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                            <path d="M12 8v4M12 16h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                        </svg>
                        {{ totalAcoes }} {{ totalAcoes === 1 ? "pendência gerada" : "pendências geradas" }} — visíveis no painel do paciente
                    </div>
                </div>

                <footer class="ns-footer">
                    <button class="btn btn-secondary" @click="emit('fechar')">
                        Fazer depois
                    </button>
                </footer>
            </div>
        </div>
    </Teleport>
</template>

<style scoped>
.modal-overlay {
    position: fixed;
    inset: 0;
    background: hsl(0 0% 0% / 0.45);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 900;
    padding: 1rem;
}

.proximos-modal {
    background: var(--card);
    border: 1px solid var(--border);
    border-radius: var(--radius-lg, var(--radius));
    box-shadow: 0 8px 32px hsl(0 0% 0% / 0.15);
    width: 100%;
    max-width: 480px;
    display: flex;
    flex-direction: column;
}

/* ── Header ──────────────────────────────────── */
.ns-header {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    padding: 1rem 1.25rem;
    border-bottom: 1px solid var(--border);
}

.ns-icone {
    color: hsl(var(--primary-hsl));
    display: flex;
    flex-shrink: 0;
}

.ns-titulo {
    font-size: var(--text-base);
    font-weight: var(--font-weight-semibold);
    color: var(--text);
    margin: 0;
    flex: 1;
}

.ns-fechar {
    border: none;
    background: transparent;
    color: var(--text-muted);
    cursor: pointer;
    padding: 0.25rem;
    display: flex;
    border-radius: var(--radius);
    transition: color 0.1s;
}

.ns-fechar:hover {
    color: var(--text);
}

/* ── Corpo ───────────────────────────────────── */
.ns-corpo {
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 0.875rem;
}

.ns-subtitulo {
    font-size: var(--text-sm);
    color: var(--text-muted);
    margin: 0;
}

.ns-lista {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
}

.ns-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
    padding: 0.5rem 0.75rem;
    background: var(--muted);
    border: 1px solid var(--border);
    border-radius: var(--radius);
}

.nsi-label {
    font-size: var(--text-sm);
    color: var(--text);
    flex: 1;
}

.nsi-ir {
    display: flex;
    align-items: center;
    gap: 0.3rem;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    color: hsl(var(--primary-hsl));
    background: transparent;
    border: none;
    cursor: pointer;
    padding: 0.25rem 0.5rem;
    border-radius: var(--radius);
    white-space: nowrap;
    transition: background 0.1s;
}

.nsi-ir:hover {
    background: hsl(var(--primary-hsl) / 0.08);
}

.nsi-manual {
    font-size: var(--text-sm);
    color: var(--text-muted);
    white-space: nowrap;
}

.ns-rodape-info {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: var(--text-sm);
    color: var(--text-muted);
}

/* ── Footer ──────────────────────────────────── */
.ns-footer {
    padding: 0.875rem 1.25rem;
    border-top: 1px solid var(--border);
    display: flex;
    justify-content: flex-end;
}
</style>
