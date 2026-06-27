<!--
  Seção Anexos do prontuário (briefing 2026-06-27_002 + addendum).
  Opera em dois modos conforme contexto:

  MODO PENDENTE (consulta atual, sem evolucaoId):
    - Arquivos ficam pendentes em memória com preview de nome/tamanho.
    - Nenhum upload ao S3 nesta fase.
    - Sem gating de "Salve a evolução primeiro" (CA27 / R12).
    - Remoção de pendente libera o arquivo da lista local.
    - Ao salvar a evolução, o pai coleta via emit "pendentes" e sobe os arquivos.

  MODO IMEDIATO (evolucaoId presente — evolução já existente):
    - Upload imediato ao S3 via uploadAnexoComMarcador.
    - Lista do backend carregada por listarAnexos filtrado por marcador="anexo".
    - Download on-demand via obterUrlAnexo (URL descartável — CA7/R6).
    - Soft-delete via removerAnexo (gating autor-ou-dono no backend — briefing 001).

  readOnly: exemplos fictícios, sem backend.
-->
<script setup lang="ts">
import { ref, onMounted, watch, computed, onUnmounted } from "vue"
import { AppEmptyState } from "@/components/ui"
import { prontuarioService } from "@/services/prontuarioService"
import type { Anexo } from "@/services/prontuarioService"

// ── Tipos aceitos (espelho da whitelist do backend) ─────────────────────────
const MIME_ACEITOS = [
    "application/pdf",
    "image/jpeg", "image/png", "image/webp",
    "application/msword",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    "application/vnd.ms-excel",
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
] as const

const EXT_ACEITAS = ".pdf,.jpg,.jpeg,.png,.webp,.doc,.docx,.xls,.xlsx"
const LIMITE_BYTES = 2 * 1024 * 1024 // 2 MB

// ── Exemplos fictícios para prévia read-only ────────────────────────────────
const EXEMPLOS: Anexo[] = [
    { id: -1, prontuarioId: 0, evolucaoId: null, nomeOriginal: "laudo-ultrassom.pdf",
      mimeType: "application/pdf", tamanhoBytes: 820_000, criadoEm: new Date().toISOString(),
      autorNome: "Dr. Exemplo", marcador: "anexo" },
    { id: -2, prontuarioId: 0, evolucaoId: null, nomeOriginal: "hemograma.pdf",
      mimeType: "application/pdf", tamanhoBytes: 345_000, criadoEm: new Date().toISOString(),
      autorNome: "Dr. Exemplo", marcador: "anexo" },
]

// ── Item pendente (antes de salvar) ────────────────────────────────────────
interface Pendente {
    chave: string          // id local único
    arquivo: File
}

const props = defineProps<{
    modelValue: Record<string, unknown>
    readOnly?: boolean
    pacienteId?: number | null
    evolucaoId?: number | null
}>()

const emit = defineEmits<{
    "update:modelValue": [v: Record<string, unknown>]
    /**
     * Lista de arquivos pendentes — emitida sempre que o usuário adiciona/remove um pendente.
     * O pai (ConsultaAtualTab → ProntuarioView) usa esta lista para subir ao S3 no salvar.
     */
    pendentes: [arquivos: File[]]
}>()

function notificarAlteracao() {
    emit("update:modelValue", { ...props.modelValue, _anexosAtualizados: Date.now() })
}

// ── Estado modo pendente ────────────────────────────────────────────────────
const pendentes = ref<Pendente[]>([])
let _chaveSeq = 0

function emitirPendentes() {
    emit("pendentes", pendentes.value.map(p => p.arquivo))
}

// ── Estado modo imediato (evolucaoId presente) ──────────────────────────────
const anexos = ref<Anexo[]>([])
const carregando = ref(false)
const enviando = ref(false)
const erro = ref<string | null>(null)
const confirmandoRemocao = ref<number | null>(null)

const modoPendente = computed(() => !props.evolucaoId)

// Lista renderizada: readOnly → exemplos; pendente → lista local; imediato → servidor
const itens = computed<Array<{ id: string | number; nome: string; tamanho: number; isPendente?: boolean }>>(() => {
    if (props.readOnly) return EXEMPLOS.map(e => ({ id: e.id, nome: e.nomeOriginal, tamanho: e.tamanhoBytes }))
    if (modoPendente.value) return pendentes.value.map(p => ({ id: p.chave, nome: p.arquivo.name, tamanho: p.arquivo.size, isPendente: true }))
    return anexos.value.map(a => ({ id: a.id, nome: a.nomeOriginal, tamanho: a.tamanhoBytes }))
})

// ── Carregar (modo imediato) ────────────────────────────────────────────────
async function carregarAnexos() {
    if (!props.pacienteId || props.readOnly || modoPendente.value) return
    carregando.value = true
    erro.value = null
    try {
        const todos = await prontuarioService.listarAnexos(props.pacienteId, props.evolucaoId!)
        anexos.value = todos.filter(a => a.marcador === "anexo")
    } catch {
        erro.value = "Não foi possível carregar os anexos."
    } finally {
        carregando.value = false
    }
}

onMounted(carregarAnexos)
watch(() => [props.pacienteId, props.evolucaoId], carregarAnexos)

// ── Validação ───────────────────────────────────────────────────────────────
function validarArquivo(arquivo: File): string | null {
    if (!MIME_ACEITOS.includes(arquivo.type as any))
        return "Tipo de arquivo não permitido. Use PDF, imagem ou documento Office."
    if (arquivo.size > LIMITE_BYTES)
        return "Arquivo muito grande. O limite é 2MB."
    return null
}

// ── Handler de seleção ──────────────────────────────────────────────────────
async function handleSelecao(event: Event) {
    const input = event.target as HTMLInputElement
    const arquivo = input.files?.[0]
    input.value = ""
    if (!arquivo) return

    const erroVal = validarArquivo(arquivo)
    if (erroVal) { erro.value = erroVal; return }
    erro.value = null

    if (modoPendente.value) {
        // CA1' / CA27: adicionar à lista de pendentes, sem upload.
        pendentes.value.push({ chave: `p-${++_chaveSeq}`, arquivo })
        emitirPendentes()
        notificarAlteracao()
        return
    }

    // Modo imediato: upload direto (evolucaoId existe).
    if (!props.pacienteId) return
    enviando.value = true
    try {
        await prontuarioService.uploadAnexoComMarcador(props.pacienteId, arquivo, "anexo", props.evolucaoId!)
        await carregarAnexos()
        notificarAlteracao()
    } catch {
        erro.value = "Falha ao enviar o arquivo."
    } finally {
        enviando.value = false
    }
}

// ── Remoção de pendente (CA23) ──────────────────────────────────────────────
function removerPendente(chave: string) {
    pendentes.value = pendentes.value.filter(p => p.chave !== chave)
    emitirPendentes()
    notificarAlteracao()
}

// ── Download (modo imediato) ────────────────────────────────────────────────
async function baixarAnexo(id: number, nome: string) {
    if (!props.pacienteId || props.readOnly) return
    try {
        const { url } = await prontuarioService.obterUrlAnexo(props.pacienteId, id)
        const a = document.createElement("a")
        a.href = url
        a.download = nome
        a.rel = "noopener noreferrer"
        a.click()
        // URL descartável — não persiste.
    } catch {
        erro.value = "Não foi possível gerar o link de download."
    }
}

// ── Soft-delete (modo imediato, gating no backend) ──────────────────────────
async function removerAnexo(id: number) {
    if (!props.pacienteId || props.readOnly) return
    try {
        await prontuarioService.removerAnexo(props.pacienteId, id)
        anexos.value = anexos.value.filter(a => a.id !== id)
        notificarAlteracao()
    } catch {
        erro.value = "Não foi possível remover o anexo."
    } finally {
        confirmandoRemocao.value = null
    }
}

// ── Helpers ─────────────────────────────────────────────────────────────────
function formatarTamanho(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function iconePorNomeOuMime(nome: string): string {
    const ext = nome.split(".").pop()?.toLowerCase() ?? ""
    if (["doc", "docx"].includes(ext)) return "fa-file-word"
    if (["xls", "xlsx"].includes(ext)) return "fa-file-excel"
    if (ext === "pdf") return "fa-file-pdf"
    if (["jpg", "jpeg", "png", "webp"].includes(ext)) return "fa-file-image"
    return "fa-file"
}

// CA32: limpar pendentes se o componente desmontar (não vazar File em memória).
onUnmounted(() => { pendentes.value = [] })
</script>

<template>
    <div class="secao-anexos">
        <!-- Botão upload — sempre habilitado em modo edição (CA27 / R12) -->
        <div v-if="!readOnly" class="acao-row">
            <label class="btn-upload" :class="{ 'btn-upload--desabilitado': enviando }">
                <i class="fa-solid fa-paperclip"></i>
                {{ enviando ? "Enviando..." : "Adicionar anexo" }}
                <input
                    type="file"
                    :accept="EXT_ACEITAS"
                    :disabled="enviando"
                    class="input-file-hidden"
                    @change="handleSelecao"
                />
            </label>
            <span v-if="modoPendente && !readOnly && pendentes.length > 0" class="aviso-pendente">
                {{ pendentes.length }} arquivo{{ pendentes.length > 1 ? 's' : '' }} aguardando salvar
            </span>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>
        <p v-if="carregando" class="estado-msg">Carregando...</p>

        <AppEmptyState
            v-else-if="!carregando && itens.length === 0"
            mensagem="Nenhum anexo adicionado."
        />

        <ul v-else class="lista-anexos">
            <li
                v-for="item in itens"
                :key="item.id"
                class="item-anexo"
                :class="{ 'item-anexo--pendente': item.isPendente }"
            >
                <i :class="`fa-regular ${iconePorNomeOuMime(item.nome)} icone-tipo`"></i>
                <div class="info-arquivo">
                    <span class="nome-arquivo">{{ item.nome }}</span>
                    <span class="meta-arquivo">
                        {{ formatarTamanho(item.tamanho) }}
                        <span v-if="item.isPendente" class="badge-pendente">pendente</span>
                    </span>
                </div>
                <div v-if="!readOnly" class="acoes-arquivo">
                    <!-- Modo pendente: só remoção local (CA23) -->
                    <template v-if="item.isPendente">
                        <button
                            type="button"
                            class="btn-icon btn-icon-excluir"
                            title="Remover"
                            @click="removerPendente(String(item.id))"
                        >
                            <i class="fa-solid fa-xmark"></i>
                        </button>
                    </template>

                    <!-- Modo imediato: baixar + confirmar soft-delete -->
                    <template v-else-if="typeof item.id === 'number' && item.id > 0">
                        <button
                            v-if="confirmandoRemocao !== item.id"
                            type="button"
                            class="btn-icon btn-icon-ver"
                            title="Baixar"
                            @click="baixarAnexo(item.id as number, item.nome)"
                        >
                            <i class="fa-solid fa-download"></i>
                        </button>
                        <template v-if="confirmandoRemocao === item.id">
                            <span class="confirmacao-texto">Remover?</span>
                            <button type="button" class="btn-icon btn-icon-excluir" title="Confirmar" @click="removerAnexo(item.id as number)">
                                <i class="fa-solid fa-check"></i>
                            </button>
                            <button type="button" class="btn-icon" title="Cancelar" @click="confirmandoRemocao = null">
                                <i class="fa-solid fa-xmark"></i>
                            </button>
                        </template>
                        <button
                            v-else
                            type="button"
                            class="btn-icon btn-icon-excluir"
                            title="Remover"
                            @click="confirmandoRemocao = item.id as number"
                        >
                            <i class="fa-solid fa-trash"></i>
                        </button>
                    </template>
                </div>
            </li>
        </ul>
    </div>
</template>

<style scoped>
.secao-anexos { display: flex; flex-direction: column; gap: 0.75rem; }

.acao-row { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }

.btn-upload {
    display: inline-flex; align-items: center; gap: 0.4rem;
    padding: 0.4rem 0.9rem; border-radius: var(--radius);
    background: var(--primary); color: #fff;
    font-size: var(--text-sm); font-weight: var(--font-weight-medium);
    cursor: pointer; border: none; transition: opacity 0.15s;
}
.btn-upload--desabilitado { opacity: 0.5; cursor: not-allowed; }
.input-file-hidden { display: none; }

.aviso-pendente { font-size: var(--text-xs); color: var(--text-muted); font-style: italic; }

.msg-erro { color: hsl(var(--danger)); font-size: var(--text-sm); }
.estado-msg { font-size: var(--text-sm); color: var(--text-muted); }

.lista-anexos { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.5rem; }

.item-anexo {
    display: flex; align-items: center; gap: 0.75rem;
    padding: 0.6rem 0.8rem; border: 1px solid var(--border);
    border-radius: var(--radius); background: var(--bg-card);
}
.item-anexo--pendente { border-style: dashed; background: var(--bg-muted); }

.icone-tipo { font-size: var(--text-base); color: var(--text-muted); flex-shrink: 0; }

.info-arquivo { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 0.1rem; }
.nome-arquivo {
    font-size: var(--text-sm); font-weight: var(--font-weight-medium);
    white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}
.meta-arquivo {
    font-size: var(--text-xs); color: var(--text-muted);
    display: flex; align-items: center; gap: 0.35rem;
}
.badge-pendente {
    font-size: var(--text-xs); font-weight: var(--font-weight-medium);
    padding: 1px 6px; border-radius: 99px;
    background: hsl(var(--warning) / 0.15); color: hsl(var(--warning));
}

.acoes-arquivo { display: flex; align-items: center; gap: 0.35rem; flex-shrink: 0; }
.confirmacao-texto { font-size: var(--text-xs); color: hsl(var(--danger)); }
</style>
