<!--
    Drawer lateral de leitura de uma evolução do prontuário.
    Exibe somente as seções preenchidas do modeloSnapshot + conteúdo correspondente.
    Seções vazias (null / "" / whitespace / [] / {}) são omitidas.
    Somente leitura — sem botão Editar.
    Sem audit LGPD ao abrir (decisão consciente — briefing 2026-05-25_001, seção 3, item 4).

    CA-C2 (briefing 2026-06-12_002): quando pacienteId é fornecido, carrega e exibe
    os termos vinculados à evolução. Botão "Emitir termo" emite evento ao pai (CA-C4).
-->
<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { AppButton, AppDrawer, AppEmptyState } from "@/components/ui"
import { formatarSecaoLegivel } from "@/composables/useEvolucaoResumo"
import { prontuarioService, type Evolucao } from "@/services/prontuarioService"
import { pacienteTermoService, type TermoEmitidoResumo } from "@/services/pacienteTermoService"

const props = defineProps<{
    evolucao: Evolucao | null
    aberto: boolean
    pacienteId?: number | null
    /** true quando o usuário tem a ação `termos.emitir` (CA-RBAC1 / CA-RBAC2). */
    podeEmitirTermo?: boolean
}>()

const emit = defineEmits<{
    fechar: []
    /** Disparado quando o usuário clica em "Emitir termo" — pai abre EmitirTermoModal (CA-C4). */
    emitirTermo: [evolucaoId: number]
}>()

function fmtData(iso: string) {
    return new Date(iso).toLocaleString("pt-BR", { dateStyle: "long", timeStyle: "short" })
}

/**
 * Seções do snapshot que produzem texto legível, na ordem original.
 * A decisão de exibir e o texto exibido derivam de `formatarSecaoLegivel`
 * (fonte única, mesma do PDF — briefing 2026-06-09_008, R7/R9): string vazia
 * = seção sem conteúdo legível, omitida.
 */
const secoesPreenchidas = computed(() => {
    if (!props.evolucao) return []
    return props.evolucao.modeloSnapshot
        .map(secao => ({
            chave: secao.chave,
            titulo: secao.titulo,
            texto: formatarSecaoLegivel(secao.chave, props.evolucao!.conteudo[secao.chave]),
        }))
        .filter(s => s.texto.length > 0)
})

const titulo = computed(() => {
    if (!props.evolucao) return "Evolução"
    return `Evolução de ${fmtData(props.evolucao.criadaEm)}`
})

// ─── Termos vinculados (CA-C2) ─────────────────────────────────────────────
const termos = ref<TermoEmitidoResumo[]>([])
const carregandoTermos = ref(false)
const baixandoTermoId = ref<number | null>(null)

watch(
    () => [props.aberto, props.evolucao?.id, props.pacienteId],
    ([aberto, evolId, pacId]) => {
        if (aberto && evolId && pacId) {
            void carregarTermos(pacId as number, evolId as number)
        } else {
            termos.value = []
        }
    },
    { immediate: true },
)

async function carregarTermos(pacienteId: number, evolucaoId: number) {
    carregandoTermos.value = true
    try {
        termos.value = await prontuarioService.listarTermosDaEvolucao(pacienteId, evolucaoId)
    } catch {
        // Falha silenciosa — termos são informação secundária, não bloqueiam a visualização.
        termos.value = []
    } finally {
        carregandoTermos.value = false
    }
}

async function baixarTermo(t: TermoEmitidoResumo) {
    if (baixandoTermoId.value !== null) return
    baixandoTermoId.value = t.id
    try {
        const { url } = await pacienteTermoService.obterUrlPdf(t.id)
        window.open(url, "_blank", "noopener,noreferrer")
    } catch {
        // Sem notificação — drawer não tem sistema de toast próprio.
    } finally {
        baixandoTermoId.value = null
    }
}

function statusLabel(s: TermoEmitidoResumo["status"]): string {
    return { Pendente: "Pendente", Assinado: "Assinado", Revogado: "Revogado", Recusado: "Recusado", Expirado: "Expirado" }[s] ?? s
}
</script>

<template>
    <AppDrawer
        :aberto="aberto"
        :titulo="titulo"
        :largura="560"
        @fechar="emit('fechar')"
    >
        <template v-if="evolucao">
            <!-- Cabeçalho da evolução -->
            <div class="edd-header">
                <div class="edd-meta-row">
                    <i class="fa-solid fa-user-doctor edd-icon"></i>
                    <span class="edd-prof">{{ evolucao.autorNome || "—" }}</span>
                </div>
                <div class="edd-meta-row">
                    <i class="fa-solid fa-file-medical edd-icon"></i>
                    <span class="edd-modelo">{{ evolucao.modeloNome || "Evolução" }}</span>
                </div>
            </div>

            <hr class="edd-divider" />

            <!-- Seções preenchidas -->
            <template v-if="secoesPreenchidas.length > 0">
                <section
                    v-for="secao in secoesPreenchidas"
                    :key="secao.chave"
                    class="edd-secao"
                    :data-test="`secao-${secao.chave}`"
                >
                    <h3 class="ds-section-title">{{ secao.titulo }}</h3>
                    <p class="edd-secao-conteudo">{{ secao.texto }}</p>
                </section>
            </template>

            <!-- Estado vazio: todas as seções estão em branco -->
            <AppEmptyState
                v-else
                icone="fa-solid fa-file-circle-xmark"
                titulo="Nenhuma seção preenchida"
                descricao="Esta evolução não tem seções preenchidas."
                :compacto="true"
                data-test="empty-state"
            />

            <!-- CA-C2: Termos vinculados à evolução -->
            <template v-if="pacienteId">
                <hr class="edd-divider" />
                <div class="edd-termos">
                    <div class="edd-termos-head">
                        <h3 class="ds-card-title">
                            <i class="fa-solid fa-file-contract"></i>
                            Termos de consentimento
                        </h3>
                        <!-- CA-C4: emite evento ao pai para abrir EmitirTermoModal com evolucaoId -->
                        <AppButton
                            v-if="podeEmitirTermo"
                            variant="secondary"
                            size="sm"
                            icon="fa-solid fa-plus"
                            data-test="btn-emitir-termo-evolucao"
                            @click="emit('emitirTermo', evolucao.id)"
                        >
                            Emitir termo
                        </AppButton>
                    </div>

                    <p v-if="carregandoTermos" class="edd-termos-msg">Carregando termos…</p>

                    <ul v-else-if="termos.length > 0" class="edd-termos-lista">
                        <li
                            v-for="t in termos"
                            :key="t.id"
                            class="edd-termo-item"
                            :data-test="`termo-${t.id}`"
                        >
                            <div class="edd-termo-info">
                                <span class="edd-termo-titulo">{{ t.termoModeloTitulo }}</span>
                                <span class="edd-termo-status" :class="`status-${t.status.toLowerCase()}`">
                                    {{ statusLabel(t.status) }}
                                </span>
                            </div>
                            <button
                                v-if="t.temPdf"
                                type="button"
                                class="btn-icon btn-icon-ver"
                                :disabled="baixandoTermoId === t.id"
                                title="Baixar PDF do termo"
                                @click="baixarTermo(t)"
                            >
                                <i class="fa-solid fa-download"></i>
                            </button>
                        </li>
                    </ul>

                    <p v-else class="edd-termos-msg">Nenhum termo vinculado a esta consulta.</p>
                </div>
            </template>
        </template>

        <template #rodape>
            <AppButton
                variant="ghost"
                data-test="btn-fechar"
                @click="emit('fechar')"
            >
                Fechar
            </AppButton>
        </template>
    </AppDrawer>
</template>

<style scoped>
.edd-header {
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding-bottom: 4px;
}

.edd-meta-row {
    display: flex;
    align-items: center;
    gap: 8px;
}

.edd-icon {
    font-size: 12px;
    color: hsl(var(--primary));
    width: 16px;
    flex-shrink: 0;
}

.edd-prof {
    font-size: 14px;
    font-weight: 600;
    color: hsl(var(--primary-dark));
}

.edd-modelo {
    font-size: 13px;
    color: hsl(var(--secondary) / 0.7);
}

.edd-divider {
    border: none;
    border-top: 1px solid hsl(var(--secondary) / 0.1);
    margin: 0;
}

.edd-secao {
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.edd-secao-conteudo {
    margin: 0;
    font-size: 14px;
    line-height: 1.65;
    color: hsl(var(--secondary) / 0.9);
    white-space: pre-wrap;
}

/* ─── Termos vinculados (CA-C2) ────────────────────────────────────────────── */
.edd-termos {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.edd-termos-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
}

.edd-termos-head .ds-card-title {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    margin: 0;
}

.edd-termos-head .ds-card-title i {
    color: hsl(var(--primary));
    font-size: var(--text-xs);
}

.edd-termos-lista {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.edd-termo-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
    padding: 8px 10px;
    background: hsl(var(--muted) / 0.4);
    border-radius: var(--radius-md);
}

.edd-termo-info {
    display: flex;
    align-items: center;
    gap: 8px;
    flex: 1;
    min-width: 0;
}

.edd-termo-titulo {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--primary-dark));
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.edd-termo-status {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    padding: 2px 8px;
    border-radius: 99px;
    white-space: nowrap;
    flex-shrink: 0;
}
.status-assinado  { background: hsl(155 60% 50% / 0.15); color: hsl(155 60% 30%); }
.status-pendente  { background: hsl(var(--warning) / 0.15); color: hsl(var(--warning)); }
.status-revogado  { background: hsl(var(--error) / 0.1); color: hsl(var(--error)); }
.status-recusado  { background: hsl(var(--error) / 0.1); color: hsl(var(--error)); }
.status-expirado  { background: hsl(var(--secondary) / 0.08); color: hsl(var(--secondary) / 0.5); }

.edd-termos-msg {
    font-size: var(--text-sm);
    color: hsl(var(--secondary) / 0.6);
    margin: 0;
    font-style: italic;
}
</style>
