<!--
    Aba "Consultas anteriores" — linha do tempo de evoluções do prontuário.
    Lista paginada server-side via /api/paciente/{id}/prontuario/evolucoes —
    cada troca de página/tamanho dispara uma request fatiada (LIMIT/OFFSET).
    Card de Anexos abaixo (não paginado — geralmente são poucos por paciente).
-->
<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue"
import { AppButton, AppEmptyState, AppPagination } from "@/components/ui"
import EvolucaoTimelineItem from "@/components/prontuario/EvolucaoTimelineItem.vue"
import {
    prontuarioService,
    type Anexo,
    type Evolucao,
} from "@/services/prontuarioService"
import type { PdfSaidaModo } from "@/composables/useProntuarioPdf"

const props = defineProps<{
    pacienteId: number
    anexos: Anexo[]
    uploadando: boolean
    evolucaoSendoBaixada: number | null
    /** Callback do pai que cuida do audit LGPD + geração do PDF do histórico. */
    gerarHistorico: (modo: PdfSaidaModo) => void
}>()

const emit = defineEmits<{
    downloadAnexo: [anexo: Anexo]
    selecionarArquivo: [event: Event]
    enviarAnexo: []
    gerarPdfEvolucao: [payload: { evolucao: Evolucao, modo: PdfSaidaModo }]
    totalAtualizado: [total: number]
}>()

// ─── Carregamento server-side ───────────────────────────────────────────────
const evolucoes = ref<Evolucao[]>([])
const total     = ref(0)
const pagina    = ref(1)
const tamanho   = ref(10)
const carregando = ref(false)
const erro       = ref<string | null>(null)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const r = await prontuarioService.listarEvolucoes(props.pacienteId, {
            pagina: pagina.value, tamanho: tamanho.value,
        })
        evolucoes.value = r.itens
        total.value = r.total
        emit("totalAtualizado", r.total)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar evoluções."
    } finally {
        carregando.value = false
    }
}

defineExpose({ recarregar: carregar })
watch([pagina, tamanho], carregar)
onMounted(carregar)

const idMaisRecente = computed(() => evolucoes.value[0]?.id ?? null)

// ─── Upload local de anexo (delegado ao pai) ────────────────────────────────
const inputFileRef = ref<HTMLInputElement | null>(null)
const nomeArquivoSelecionado = ref<string | null>(null)

function abrirSeletorArquivo() { inputFileRef.value?.click() }
function onArquivoSelecionado(event: Event) {
    const input = event.target as HTMLInputElement
    nomeArquivoSelecionado.value = input.files?.[0]?.name ?? null
    emit("selecionarArquivo", event)
}
function onEnviarAnexo() {
    emit("enviarAnexo")
    nomeArquivoSelecionado.value = null
    if (inputFileRef.value) inputFileRef.value.value = ""
}

// ─── Helpers de formatação (anexos) ─────────────────────────────────────────
function fmtData(iso: string) { return new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" }) }
function fmtTamanho(bytes: number) {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
</script>

<template>
    <div class="anteriores-wrap">
        <!-- Header com ações -->
        <div class="ht-head">
            <div>
                <h2 class="ht-titulo">Linha do tempo de atendimentos</h2>
                <p class="ht-sub">
                    {{ total }} {{ total === 1 ? "evolução registrada" : "evoluções registradas" }}
                </p>
            </div>
            <div v-if="total > 0" class="ht-actions">
                <AppButton
                    variant="secondary"
                    icon="fa-solid fa-eye"
                    aria-label="Visualizar PDF do histórico de evoluções"
                    @click="props.gerarHistorico('visualizar')"
                >
                    Visualizar histórico
                </AppButton>
                <AppButton
                    variant="ghost"
                    icon="fa-solid fa-download"
                    aria-label="Baixar PDF do histórico de evoluções"
                    @click="props.gerarHistorico('download')"
                >
                    Baixar
                </AppButton>
            </div>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>
        <p v-if="carregando" class="estado-msg">Carregando evoluções...</p>

        <!-- Empty -->
        <AppEmptyState
            v-else-if="total === 0"
            mensagem="Nenhuma evolução registrada ainda."
        />

        <!-- Timeline -->
        <template v-else>
            <div class="ht-timeline-full" role="list">
                <EvolucaoTimelineItem
                    v-for="evo in evolucoes"
                    :key="evo.id"
                    :evolucao="evo"
                    :destaque="pagina === 1 && evo.id === idMaisRecente"
                    :gerando-pdf="evolucaoSendoBaixada === evo.id"
                    @gerar-pdf="emit('gerarPdfEvolucao', $event)"
                />
            </div>
            <AppPagination
                v-model:pagina="pagina"
                v-model:tamanho="tamanho"
                :total="total"
                rotulo-itens="evolução(ões)"
            />
        </template>

        <!-- Card de Anexos -->
        <section class="anexos-card">
            <header class="anexos-head">
                <i class="fa-solid fa-paperclip"></i>
                <h3>Anexos do paciente</h3>
                <span class="anexos-count">{{ anexos.length }}</span>
            </header>

            <ul v-if="anexos.length > 0" class="anexos-lista">
                <li v-for="a in anexos" :key="a.id" class="anexo-item">
                    <button type="button" class="anexo-btn" @click="emit('downloadAnexo', a)">
                        <i class="fa-solid fa-file-arrow-down"></i>
                        <span>{{ a.nomeOriginal }}</span>
                    </button>
                    <span class="anexo-meta">{{ fmtTamanho(a.tamanhoBytes) }} · {{ fmtData(a.criadoEm) }}</span>
                </li>
            </ul>
            <p v-else class="anexo-vazio">Nenhum anexo enviado.</p>

            <input
                ref="inputFileRef"
                type="file"
                class="input-file-hidden"
                @change="onArquivoSelecionado"
            />
            <div class="upload-row">
                <button type="button" class="upload-area" @click="abrirSeletorArquivo">
                    <i class="fa-solid fa-cloud-arrow-up"></i>
                    <span>{{ nomeArquivoSelecionado ?? "Escolher arquivo para enviar..." }}</span>
                </button>
                <AppButton
                    size="sm"
                    :loading="uploadando"
                    :disabled="uploadando || !nomeArquivoSelecionado"
                    @click="onEnviarAnexo"
                >
                    {{ uploadando ? "Enviando..." : "Enviar" }}
                </AppButton>
            </div>
        </section>
    </div>
</template>

<style scoped>
.anteriores-wrap { display: flex; flex-direction: column; gap: 18px; }

/* ──── Header ──── */
.ht-head {
    display: flex; align-items: center; justify-content: space-between;
    gap: 16px; flex-wrap: wrap;
}
.ht-titulo {
    margin: 0 0 4px;
    font-size: 22px; font-weight: 700;
    color: hsl(var(--primary-dark));
}
.ht-sub {
    margin: 0;
    font-size: 13px; color: hsl(var(--secondary) / 0.65);
}
.ht-actions { display: flex; gap: 8px; }

.estado-msg { text-align: center; color: var(--text-muted); padding: 1.5rem 1rem; font-size: 0.9em; }
.msg-erro   { color: hsl(var(--error)); font-size: 0.875em; margin: 0; }

/* ──── Wrapper de timeline (compartilhado com PacienteDetalheView via EvolucaoTimelineItem) ──── */
.ht-timeline-full {
    display: flex; flex-direction: column; gap: 16px;
    position: relative; padding-left: 50px;
}
.ht-timeline-full::before {
    content: ""; position: absolute; left: 21px; top: 8px; bottom: 8px;
    width: 2px;
    background: linear-gradient(to bottom,
        hsl(var(--primary) / 0.3),
        hsl(var(--secondary) / 0.08));
}

/* ──── Card de anexos ──── */
.anexos-card {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-lg);
    padding: 16px 18px;
}
.anexos-head {
    display: flex; align-items: center; gap: 10px;
    margin-bottom: 12px;
}
.anexos-head > i {
    width: 32px; height: 32px; border-radius: 8px;
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
}
.anexos-head h3 {
    margin: 0; flex: 1;
    font-size: 15px; font-weight: 700;
    color: hsl(var(--primary-dark));
}
.anexos-count {
    background: hsl(var(--secondary) / 0.06);
    color: hsl(var(--secondary) / 0.7);
    font-size: 11px; font-weight: 700;
    padding: 2px 8px; border-radius: 99px;
}
.anexos-lista { list-style: none; margin: 0 0 8px; padding: 0; display: flex; flex-direction: column; }
.anexo-item {
    display: flex; justify-content: space-between; align-items: center;
    gap: 10px; padding: 8px 0;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.anexo-item:last-child { border-bottom: 0; }
.anexo-btn {
    display: inline-flex; align-items: center; gap: 8px;
    background: none; border: 0; cursor: pointer;
    font: inherit; font-size: 13px;
    color: hsl(var(--primary)); font-weight: 600;
    padding: 4px 0;
}
.anexo-btn:hover { text-decoration: underline; }
.anexo-meta { font-size: 11px; color: hsl(var(--secondary) / 0.55); white-space: nowrap; }
.anexo-vazio { font-size: 13px; color: hsl(var(--secondary) / 0.55); margin: 6px 0 12px; }

.input-file-hidden { display: none; }
.upload-row {
    display: flex; gap: 8px; align-items: center; flex-wrap: wrap;
    margin-top: 10px; padding-top: 12px;
    border-top: 1px solid hsl(var(--secondary) / 0.06);
}
.upload-area {
    flex: 1; min-width: 0;
    display: inline-flex; align-items: center; gap: 8px;
    padding: 8px 12px;
    background: hsl(var(--primary) / 0.04);
    border: 1px dashed hsl(var(--primary) / 0.3);
    border-radius: var(--radius-md);
    cursor: pointer; font: inherit; font-size: 13px;
    color: hsl(var(--secondary) / 0.7);
    transition: border-color 150ms, background 150ms;
}
.upload-area:hover { border-color: hsl(var(--primary) / 0.5); background: hsl(var(--primary) / 0.08); }
.upload-area i { color: hsl(var(--primary)); }
.upload-area span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
