<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue"
import {
    AppButton, AppEmptyState, AppBadge, AppFilterPills,
    AppConfirmDialog, AppTextarea,
} from "@/components/ui"
import { usePermissoesStore } from "@/stores/permissoesStore"
import {
    pacienteTermoService,
    type TermoEmitidoResumo,
    type TermoEmitidoDetalhe,
    type StatusTermoEmitido,
} from "@/services/pacienteTermoService"
import { CATEGORIAS_TERMO } from "@/constants/termoVariaveis"
import EmitirTermoModal from "./EmitirTermoModal.vue"
import TermoVisualizacaoDrawer from "./TermoVisualizacaoDrawer.vue"
import { useTermoPdf } from "@/composables/useTermoPdf"
import type { Paciente } from "@/services/pacienteService"

/**
 * Aba "Termos" do detalhe do paciente. Encapsula:
 *  - lista (filtro por status + paginação client-side, MVP: lista pequena, ≤100)
 *  - botão Emitir termo (gated por `termos.emitir`)
 *  - botão Anexar PDF assinado (somente em status Pendente + tipo PdfAnexado)
 *  - Visualizar via drawer (snapshot + metadados)
 *  - Baixar PDF anexado (presigned URL)
 *  - Gerar PDF de impressão (front via `useTermoPdf`)
 *  - Revogar (motivo obrigatório, gated por `termos.gerenciar_modelos`)
 *
 * Carregamento é disparado pela view pai apenas quando a aba é clicada
 * (lazy). Veja `watch(aba)` em `PacienteDetalheView.vue`.
 */
const props = defineProps<{
    /** Paciente já carregado pela view pai (usado pra gerar PDF + emitir). */
    paciente: Paciente
    /**
     * Quando true, a aba é (re)carregada. A view pai dispara isso ao clicar
     * "Termos" pela primeira vez (premissa: lazy carrega só ao clicar).
     */
    ativa: boolean
}>()

const emit = defineEmits<{
    (e: "notificar", mensagem: string, variante?: "success" | "error" | "info"): void
}>()

// ─── Permissões ────────────────────────────────────────────────────────────
const permissoes = usePermissoesStore()
const podeEmitir = computed(() => permissoes.pode("termos.emitir"))
const podeRevogar = computed(() => permissoes.pode("termos.gerenciar_modelos"))

// ─── Estado ────────────────────────────────────────────────────────────────
const termos = ref<TermoEmitidoResumo[]>([])
const carregando = ref(false)
const carregado = ref(false)
const filtroStatus = ref<"" | StatusTermoEmitido>("")
const acaoEmAndamentoId = ref<number | null>(null)

const modalEmitirAberto = ref(false)

const drawerAberto = ref(false)
const carregandoDrawer = ref(false)
const termoDetalhe = ref<TermoEmitidoDetalhe | null>(null)

const confirmRevogar = ref<{ aberto: boolean; termo: TermoEmitidoResumo | null; motivo: string; salvando: boolean }>({
    aberto: false, termo: null, motivo: "", salvando: false,
})

const fileInput = ref<HTMLInputElement | null>(null)
const termoParaAnexar = ref<TermoEmitidoResumo | null>(null)

// ─── Carregamento ──────────────────────────────────────────────────────────
// Carrega a lista COMPLETA (sem filtro de status). O filtro é client-side
// (lista pequena, ≤100) — assim as pills e suas contagens nunca dependem da
// lista filtrada e não somem ao escolher um status sem resultados.
async function carregar() {
    carregando.value = true
    try {
        termos.value = await pacienteTermoService.listar(props.paciente.id)
        carregado.value = true
    } catch (e: any) {
        emit("notificar", e?.response?.data?.mensagem ?? "Erro ao carregar termos.", "error")
    } finally {
        carregando.value = false
    }
}

// Lazy: dispara só quando a aba é clicada (props.ativa flipa pra true).
watch(() => props.ativa, (ativa) => {
    if (ativa && !carregado.value) void carregar()
}, { immediate: true })

// ─── Filtros ───────────────────────────────────────────────────────────────
const termosFiltrados = computed(() =>
    filtroStatus.value
        ? termos.value.filter(t => t.status === filtroStatus.value)
        : termos.value,
)

const opcoesStatus = computed(() => {
    const counts = contarPorStatus(termos.value)
    return [
        { valor: "" as const, label: "Todos", count: termos.value.length },
        { valor: "Pendente" as const, label: "Pendentes", count: counts.Pendente, dot: "warning" as const },
        { valor: "Assinado" as const, label: "Assinados", count: counts.Assinado, dot: "success" as const },
        { valor: "Recusado" as const, label: "Recusados", count: counts.Recusado, dot: "error" as const },
        { valor: "Revogado" as const, label: "Revogados", count: counts.Revogado, dot: "muted" as const },
        { valor: "Expirado" as const, label: "Expirados", count: counts.Expirado, dot: "muted" as const },
    ]
})

function contarPorStatus(items: TermoEmitidoResumo[]): Record<StatusTermoEmitido, number> {
    const r: Record<StatusTermoEmitido, number> = {
        Pendente: 0, Assinado: 0, Recusado: 0, Revogado: 0, Expirado: 0,
    }
    for (const t of items) r[t.status] = (r[t.status] ?? 0) + 1
    return r
}

// ─── Helpers de render ─────────────────────────────────────────────────────
function statusVariant(s: StatusTermoEmitido): "warning" | "success" | "error" | "muted" | "default" {
    switch (s) {
        case "Pendente": return "warning"
        case "Assinado": return "success"
        case "Recusado": return "error"
        case "Revogado": return "error"
        case "Expirado": return "muted"
        default: return "default"
    }
}

function labelStatus(s: StatusTermoEmitido): string {
    return STATUS_LABELS[s] ?? s
}

function labelCategoria(cat: string): string {
    return CATEGORIAS_TERMO.find(c => c.chave === cat)?.label ?? cat
}

function corCategoria(cat: string): "default" | "success" | "warning" | "error" | "info" | "muted" {
    return CATEGORIAS_TERMO.find(c => c.chave === cat)?.cor ?? "muted"
}

function fmtData(iso: string | null): string {
    if (!iso) return "—"
    const d = new Date(iso)
    return Number.isNaN(d.getTime())
        ? "—"
        : d.toLocaleString("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" })
}

const STATUS_LABELS: Record<StatusTermoEmitido, string> = {
    Pendente: "Pendente",
    Assinado: "Assinado",
    Recusado: "Recusado",
    Revogado: "Revogado",
    Expirado: "Expirado",
}

// ─── Ações ─────────────────────────────────────────────────────────────────
function abrirEmitir() {
    if (!podeEmitir.value) return
    modalEmitirAberto.value = true
}

const { gerarPdf } = useTermoPdf()

async function onEmitido(payload: { termoEmitidoId: number; modeloTitulo: string }) {
    // Recarrega lista pra mostrar o novo item (rápido — lista é pequena).
    await carregar()
    emit("notificar", `Termo "${payload.modeloTitulo}" emitido. Baixando PDF para impressão…`, "success")
    try {
        const detalhe = await pacienteTermoService.obter(props.paciente.id, payload.termoEmitidoId)
        await gerarPdf(detalhe, props.paciente, "download")
    } catch (e: any) {
        emit("notificar", e?.response?.data?.mensagem ?? "Não foi possível gerar o PDF de impressão.", "error")
    }
}

async function abrirDrawer(t: TermoEmitidoResumo) {
    drawerAberto.value = true
    carregandoDrawer.value = true
    termoDetalhe.value = null
    try {
        termoDetalhe.value = await pacienteTermoService.obter(props.paciente.id, t.id)
    } catch (e: any) {
        emit("notificar", e?.response?.data?.mensagem ?? "Erro ao carregar termo.", "error")
        drawerAberto.value = false
    } finally {
        carregandoDrawer.value = false
    }
}

async function baixarPdfAnexado(t: TermoEmitidoResumo) {
    if (acaoEmAndamentoId.value !== null) return
    acaoEmAndamentoId.value = t.id
    try {
        const { url } = await pacienteTermoService.obterUrlPdf(t.id)
        window.open(url, "_blank", "noopener")
    } catch (e: any) {
        emit("notificar", e?.response?.data?.mensagem ?? "Erro ao gerar URL do PDF.", "error")
    } finally {
        acaoEmAndamentoId.value = null
    }
}

/**
 * Briefing 2026-06-10_002 — baixa o PDF probatório gerado pelo servidor.
 * Usado apenas para termos sem anexo manual (`!t.temPdf`).
 */
async function baixarPdfGerado(t: TermoEmitidoResumo) {
    if (acaoEmAndamentoId.value !== null) return
    acaoEmAndamentoId.value = t.id
    try {
        await pacienteTermoService.baixarPdfGerado(t.id)
    } catch (e: any) {
        emit("notificar", e?.response?.data?.mensagem ?? "Erro ao gerar o PDF.", "error")
    } finally {
        acaoEmAndamentoId.value = null
    }
}

// Upload do documento assinado (PDF, JPG ou PNG). Aceita 1-2 arquivos.
function abrirSeletorPdf(t: TermoEmitidoResumo) {
    termoParaAnexar.value = t
    fileInput.value?.click()
}

async function onArquivoSelecionado(ev: Event) {
    const input = ev.target as HTMLInputElement
    const arquivos = Array.from(input.files ?? [])
    input.value = "" // reset pra permitir reescolher o mesmo arquivo
    const t = termoParaAnexar.value
    termoParaAnexar.value = null
    if (!arquivos.length || !t) return
    if (acaoEmAndamentoId.value !== null) return
    acaoEmAndamentoId.value = t.id
    try {
        await pacienteTermoService.anexarPdf(t.id, arquivos)
        emit("notificar", "Documento anexado. Termo marcado como assinado.", "success")
        await carregar()
    } catch (e: any) {
        emit("notificar", e?.response?.data?.mensagem ?? "Erro ao anexar documento.", "error")
    } finally {
        acaoEmAndamentoId.value = null
    }
}

// Revogação
function abrirRevogar(t: TermoEmitidoResumo) {
    if (!podeRevogar.value) return
    confirmRevogar.value = { aberto: true, termo: t, motivo: "", salvando: false }
}

async function confirmarRevogacao() {
    const t = confirmRevogar.value.termo
    if (!t) return
    const motivo = confirmRevogar.value.motivo.trim()
    if (motivo.length < 10 || motivo.length > 500) {
        emit("notificar", "Motivo deve ter entre 10 e 500 caracteres.", "error")
        return
    }
    confirmRevogar.value.salvando = true
    try {
        await pacienteTermoService.revogar(t.id, motivo)
        emit("notificar", "Termo revogado.", "success")
        confirmRevogar.value.aberto = false
        confirmRevogar.value.termo = null
        confirmRevogar.value.motivo = ""
        await carregar()
    } catch (e: any) {
        emit("notificar", e?.response?.data?.mensagem ?? "Erro ao revogar termo.", "error")
    } finally {
        confirmRevogar.value.salvando = false
    }
}

// ─── Drawer → ação encadeada ───────────────────────────────────────────────
function drawerBaixarPdf() {
    if (termoDetalhe.value?.temPdf) {
        void baixarPdfAnexado(termoDetalhe.value as unknown as TermoEmitidoResumo)
    }
}

function drawerGerarPdf() {
    if (termoDetalhe.value) {
        void gerarPdf(termoDetalhe.value, props.paciente, "download")
    }
}

onMounted(() => {
    // Quando a aba já está ativa no mount (e.g. usuário recarrega já nela).
    if (props.ativa && !carregado.value) void carregar()
})
</script>

<template>
    <div class="termos-tab">
        <!-- Header -->
        <header class="tt-head">
            <div class="tt-titulo">
                <h3>Termos de consentimento</h3>
                <span class="tt-count">{{ termos.length }} termo{{ termos.length !== 1 ? 's' : '' }}</span>
            </div>
            <AppButton
                v-if="podeEmitir"
                icon="fa-solid fa-file-signature"
                @click="abrirEmitir"
            >
                Emitir termo
            </AppButton>
        </header>

        <AppFilterPills
            v-if="carregado && termos.length > 0"
            v-model="filtroStatus"
            :opcoes="opcoesStatus"
        />

        <p v-if="carregando" class="msg">Carregando…</p>

        <AppEmptyState
            v-else-if="carregado && termos.length === 0"
            icone="📝"
            titulo="Nenhum termo emitido"
            descricao="Emita um termo para registrar o consentimento do paciente (LGPD, cirúrgico, uso de imagem, etc.)."
        />

        <AppEmptyState
            v-else-if="carregado && termosFiltrados.length === 0"
            icone="🔍"
            titulo="Sem resultados nesse filtro"
            descricao="Não há termos com este status para o paciente."
        />

        <table v-else-if="carregado" class="tt-tabela">
            <thead>
                <tr>
                    <th>Modelo</th>
                    <th>Categoria</th>
                    <th>Status</th>
                    <th>Emitido em</th>
                    <th>Assinado em</th>
                    <th class="acoes">Ações</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="t in termosFiltrados" :key="t.id">
                    <td>
                        <div class="tt-modelo">
                            <b>{{ t.termoModeloTitulo }}</b>
                            <span class="tt-versao">v{{ t.versaoModelo }}</span>
                        </div>
                    </td>
                    <td>
                        <AppBadge :variant="corCategoria(t.categoria)" :label="labelCategoria(t.categoria)" />
                    </td>
                    <td>
                        <AppBadge :variant="statusVariant(t.status)" :label="labelStatus(t.status)" />
                    </td>
                    <td>{{ fmtData(t.criadoEm) }}</td>
                    <td>{{ fmtData(t.assinadoEm) }}</td>
                    <td class="acoes">
                        <button
                            class="btn-icon btn-icon-ver"
                            title="Visualizar termo"
                            @click="abrirDrawer(t)"
                        >
                            <i class="fa-solid fa-eye"></i>
                        </button>

                        <!-- Baixar PDF anexado: só quando tem PDF -->
                        <button
                            v-if="t.temPdf"
                            class="btn-icon"
                            title="Baixar PDF anexado"
                            :disabled="acaoEmAndamentoId === t.id"
                            @click="baixarPdfAnexado(t)"
                        >
                            <i class="fa-solid fa-download"></i>
                        </button>

                        <!-- Baixar PDF gerado pelo servidor: quando não tem anexo manual -->
                        <button
                            v-else
                            class="btn-icon"
                            title="Baixar PDF"
                            :disabled="acaoEmAndamentoId === t.id"
                            @click="baixarPdfGerado(t)"
                        >
                            <i class="fa-solid fa-file-pdf"></i>
                        </button>

                        <!-- Anexar PDF assinado: pendente + PdfAnexado + sem PDF ainda -->
                        <button
                            v-if="podeEmitir && t.status === 'Pendente' && t.assinaturaTipo === 'PdfAnexado' && !t.temPdf"
                            class="btn-icon btn-icon-editar"
                            title="Anexar PDF assinado"
                            :disabled="acaoEmAndamentoId === t.id"
                            @click="abrirSeletorPdf(t)"
                        >
                            <i class="fa-solid fa-upload"></i>
                        </button>

                        <!-- Tooltip educativo quando já tem PDF e está assinado: sem ação -->
                        <button
                            v-else-if="t.status === 'Assinado' && t.temPdf"
                            class="btn-icon"
                            title="Para substituir, revogue e emita novamente"
                            disabled
                        >
                            <i class="fa-solid fa-lock"></i>
                        </button>

                        <!-- Revogar: só assinados, gated por gerenciar_modelos -->
                        <button
                            v-if="podeRevogar && t.status === 'Assinado'"
                            class="btn-icon btn-icon-excluir"
                            title="Revogar termo"
                            :disabled="acaoEmAndamentoId === t.id"
                            @click="abrirRevogar(t)"
                        >
                            <i class="fa-solid fa-ban"></i>
                        </button>
                    </td>
                </tr>
            </tbody>
        </table>

        <!-- Modal emitir -->
        <EmitirTermoModal
            v-model:aberto="modalEmitirAberto"
            :paciente="paciente"
            @fechar="modalEmitirAberto = false"
            @emitido="onEmitido"
        />

        <!-- Drawer visualizar -->
        <TermoVisualizacaoDrawer
            :aberto="drawerAberto"
            :termo="termoDetalhe"
            :carregando="carregandoDrawer"
            @fechar="drawerAberto = false; termoDetalhe = null"
            @baixar-pdf-anexado="drawerBaixarPdf"
            @gerar-pdf="drawerGerarPdf"
        />

        <!-- Confirmar revogação -->
        <AppConfirmDialog
            v-model:aberto="confirmRevogar.aberto"
            titulo="Revogar termo?"
            mensagem="A revogação registra a data, o usuário e o motivo. O paciente deve ser informado."
            confirmar-rotulo="Revogar termo"
            variante="danger"
            icone="fa-solid fa-ban"
            :executando="confirmRevogar.salvando"
            @confirmar="confirmarRevogacao"
            @cancelar="confirmRevogar.motivo = ''"
        >
            <AppTextarea
                v-model="confirmRevogar.motivo"
                rotulo="Motivo da revogação"
                obrigatorio
                placeholder="Ex: paciente solicitou retirar consentimento por mudança de tratamento."
                :rows="4"
                :max-length="500"
            />
            <p class="msg-mini">Mínimo 10, máximo 500 caracteres.</p>
        </AppConfirmDialog>

        <!-- File input invisível para anexar documento (PDF, JPG ou PNG — até 2 arquivos) -->
        <input
            ref="fileInput"
            type="file"
            accept="application/pdf,image/jpeg,image/png"
            multiple
            class="visually-hidden"
            @change="onArquivoSelecionado"
        />
    </div>
</template>

<style scoped>
.termos-tab { display: flex; flex-direction: column; gap: 12px; }

.tt-head {
    display: flex; justify-content: space-between; align-items: center; gap: 12px; flex-wrap: wrap;
}
.tt-titulo { display: flex; align-items: baseline; gap: 10px; }
.tt-titulo h3 {
    margin: 0; font-size: var(--text-lg); font-weight: var(--font-weight-bold); color: hsl(var(--primary-dark));
}
.tt-count {
    font-size: 12px; color: hsl(var(--secondary) / 0.6); font-weight: 600;
}

.tt-tabela {
    width: 100%; border-collapse: collapse;
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 8px;
    overflow: hidden;
}
.tt-tabela thead {
    background: hsl(var(--muted) / 0.5);
}
.tt-tabela th {
    text-align: left;
    font-size: 11px; font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: hsl(var(--secondary) / 0.65);
    padding: 10px 12px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.1);
}
.tt-tabela td {
    padding: 10px 12px;
    font-size: 13px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
    vertical-align: middle;
}
.tt-tabela tr:last-child td { border-bottom: none; }

.tt-modelo { display: flex; align-items: baseline; gap: 6px; }
.tt-modelo b { font-weight: 600; }
.tt-versao { font-size: 11px; color: hsl(var(--secondary) / 0.5); }

.acoes { white-space: nowrap; text-align: right; }
.acoes .btn-icon + .btn-icon { margin-left: 4px; }

.msg { font-size: 13.5px; color: hsl(var(--secondary) / 0.7); }
.msg-mini { font-size: 11.5px; color: hsl(var(--secondary) / 0.55); margin: 6px 0 0; }

.visually-hidden {
    position: absolute; width: 1px; height: 1px;
    padding: 0; margin: -1px; overflow: hidden;
    clip: rect(0,0,0,0); white-space: nowrap; border: 0;
}
</style>
