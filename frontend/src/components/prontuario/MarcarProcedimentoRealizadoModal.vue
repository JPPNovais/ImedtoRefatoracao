<!--
  Modal de confirmação: MarcarProcedimentoRealizado (F4 — briefing 2026-06-10_013).

  Exibe preview dos procedimentos, valor total e produtos a baixar do estoque.
  Produtos sem vínculo de inventário são sinalizados com aviso (CA94).
  Ao confirmar, chama marcarProcedimentoRealizado e emite "concluido" com o cobrancaId.

  Props:
    - aberto: boolean
    - pacienteId: number
    - pendenciaId: number

  Emits:
    - fechar
    - concluido(cobrancaId: number) — acionado após sucesso
-->
<script setup lang="ts">
import { ref, watch } from "vue"
import { AppButton, AppModal } from "@/components/ui"
import {
    pendenciaService,
    type PreviewProcedimentoRealizado,
} from "@/services/pendenciaService"

// ── Props / emits ──────────────────────────────────────────────────────────────

const props = defineProps<{
    aberto: boolean
    pacienteId: number
    pendenciaId: number
}>()

const emit = defineEmits<{
    fechar: []
    concluido: [cobrancaId: number]
}>()

// ── Estado ─────────────────────────────────────────────────────────────────────

const preview = ref<PreviewProcedimentoRealizado | null>(null)
const carregandoPreview = ref(false)
const confirmando = ref(false)
const erro = ref<string | null>(null)

// ── Efeitos ────────────────────────────────────────────────────────────────────

watch(() => props.aberto, async (aberto) => {
    if (!aberto) {
        preview.value = null
        erro.value = null
        confirmando.value = false
        return
    }
    await carregarPreview()
}, { immediate: true })

async function carregarPreview() {
    carregandoPreview.value = true
    erro.value = null
    try {
        preview.value = await pendenciaService.previewProcedimentoRealizado(
            props.pacienteId,
            props.pendenciaId,
        )
    } catch (e: unknown) {
        const msg = extrairMensagem(e)
        erro.value = msg
    } finally {
        carregandoPreview.value = false
    }
}

// ── Ações ──────────────────────────────────────────────────────────────────────

async function confirmar() {
    confirmando.value = true
    erro.value = null
    try {
        const resultado = await pendenciaService.marcarProcedimentoRealizado(
            props.pacienteId,
            props.pendenciaId,
        )
        emit("concluido", resultado.cobrancaId)
    } catch (e: unknown) {
        erro.value = extrairMensagem(e)
    } finally {
        confirmando.value = false
    }
}

function fechar() {
    emit("fechar")
}

// ── Helpers ────────────────────────────────────────────────────────────────────

function extrairMensagem(e: unknown): string {
    if (e && typeof e === "object" && "response" in e) {
        const r = (e as { response?: { data?: { mensagem?: string; message?: string } } }).response
        return r?.data?.mensagem ?? r?.data?.message ?? "Ocorreu um erro inesperado."
    }
    return "Ocorreu um erro inesperado."
}

function formatarValor(v: number): string {
    return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function formatarQtd(q: number): string {
    return q % 1 === 0 ? q.toString() : q.toLocaleString("pt-BR")
}
</script>

<template>
    <AppModal :aberto="aberto" largura="md" @fechar="fechar">
        <template #titulo>
            <h2 class="modal-titulo">Marcar procedimento como realizado</h2>
        </template>

        <!-- Loading -->
        <div v-if="carregandoPreview" class="estado-loading" aria-live="polite">
            Carregando preview...
        </div>

        <!-- Erro de preview -->
        <div v-else-if="erro && !preview" class="msg-erro" role="alert">
            {{ erro }}
        </div>

        <!-- Preview carregado -->
        <template v-else-if="preview">
            <!-- Lista de procedimentos -->
            <section class="secao">
                <h3 class="secao-titulo">Procedimentos realizados</h3>
                <ul class="lista-proc">
                    <li
                        v-for="proc in preview.procedimentos"
                        :key="proc.catalogoCirurgiaId"
                        class="proc-item"
                    >
                        <span class="proc-nome">{{ proc.descricao }}</span>
                        <span class="proc-valor">{{ formatarValor(proc.valor) }}</span>
                    </li>
                </ul>
                <div class="total-row">
                    <span class="total-label">Total a cobrar</span>
                    <span class="total-valor">{{ formatarValor(preview.valorTotal) }}</span>
                </div>
                <p class="obs-particular">Tipo: Particular — cobrança registrada automaticamente.</p>
            </section>

            <!-- Produtos a baixar do estoque (CA85/CA94) -->
            <section v-if="preview.produtosABaixar.length > 0" class="secao">
                <h3 class="secao-titulo">Produtos a baixar do estoque</h3>

                <!-- Aviso de sem-vínculo (CA94) -->
                <div v-if="preview.temProdutoSemVinculo" class="aviso-sem-vinculo" role="alert">
                    Atenção: alguns produtos não têm item de inventário vinculado e
                    <strong>não serão baixados automaticamente</strong>.
                </div>

                <ul class="lista-prod">
                    <li
                        v-for="prod in preview.produtosABaixar"
                        :key="prod.produtoId"
                        class="prod-item"
                        :class="{ 'prod-item--sem-vinculo': prod.semVinculo }"
                    >
                        <span class="prod-nome">{{ prod.produtoNome }}</span>
                        <span class="prod-qtd">{{ formatarQtd(prod.quantidade) }} un</span>
                        <span v-if="prod.semVinculo" class="prod-tag-sem-vinculo">sem estoque vinculado</span>
                        <span v-else class="prod-tag-item">{{ prod.itemInventarioNome }}</span>
                    </li>
                </ul>
            </section>

            <!-- Erro de confirmação -->
            <div v-if="erro" class="msg-erro" role="alert">
                {{ erro }}
            </div>
        </template>

        <template #rodape>
            <AppButton variant="ghost" :disabled="confirmando" @click="fechar">
                Cancelar
            </AppButton>
            <AppButton
                variant="primary"
                :disabled="carregandoPreview || !preview || confirmando"
                :loading="confirmando"
                @click="confirmar"
            >
                {{ confirmando ? "Processando..." : "Confirmar" }}
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.modal-titulo {
    font-size: var(--text-lg);
    font-weight: var(--font-weight-bold);
    color: var(--text);
    margin: 0;
}

.estado-loading {
    font-size: var(--text-sm);
    color: var(--text-muted);
    padding: 1rem 0;
    text-align: center;
}

/* ── Seções ───────────────────────────────────── */
.secao {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.secao + .secao {
    margin-top: 1.25rem;
    padding-top: 1rem;
    border-top: 1px solid var(--border);
}

.secao-titulo {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: var(--text-muted);
    margin: 0;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

/* ── Procedimentos ────────────────────────────── */
.lista-proc {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.proc-item {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    gap: 0.5rem;
    padding: 0.375rem 0;
    border-bottom: 1px solid var(--border);
    font-size: var(--text-sm);
}

.proc-nome {
    flex: 1;
    color: var(--text);
}

.proc-valor {
    font-weight: var(--font-weight-medium);
    color: var(--text);
    white-space: nowrap;
}

.total-row {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    padding: 0.5rem 0 0.25rem;
    font-size: var(--text-base);
}

.total-label {
    font-weight: var(--font-weight-semibold);
    color: var(--text);
}

.total-valor {
    font-weight: var(--font-weight-bold);
    color: hsl(var(--primary-hsl));
}

.obs-particular {
    font-size: var(--text-xs);
    color: var(--text-muted);
    margin: 0;
}

/* ── Produtos ─────────────────────────────────── */
.aviso-sem-vinculo {
    font-size: var(--text-sm);
    color: hsl(var(--warning-hsl, 38 92% 40%));
    background: hsl(var(--warning-hsl, 38 92% 50%) / 0.08);
    border: 1px solid hsl(var(--warning-hsl, 38 92% 50%) / 0.3);
    border-radius: var(--radius);
    padding: 0.5rem 0.75rem;
}

.lista-prod {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.prod-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: var(--text-sm);
    padding: 0.3rem 0;
    color: var(--text);
}

.prod-item--sem-vinculo {
    opacity: 0.65;
}

.prod-nome {
    flex: 1;
}

.prod-qtd {
    font-weight: var(--font-weight-medium);
    white-space: nowrap;
}

.prod-tag-sem-vinculo {
    font-size: var(--text-xs);
    color: hsl(var(--warning-hsl, 38 92% 40%));
    border: 1px solid hsl(var(--warning-hsl, 38 92% 50%) / 0.4);
    border-radius: 999px;
    padding: 0.1rem 0.45rem;
    white-space: nowrap;
}

.prod-tag-item {
    font-size: var(--text-xs);
    color: var(--text-muted);
    border: 1px solid var(--border);
    border-radius: 999px;
    padding: 0.1rem 0.45rem;
    white-space: nowrap;
}

/* ── Erro ─────────────────────────────────────── */
.msg-erro {
    font-size: var(--text-sm);
    color: hsl(var(--danger-hsl, 0 70% 50%));
    background: hsl(var(--danger-hsl, 0 70% 50%) / 0.06);
    border: 1px solid hsl(var(--danger-hsl, 0 70% 50%) / 0.2);
    border-radius: var(--radius);
    padding: 0.5rem 0.75rem;
    margin: 0;
}
</style>
