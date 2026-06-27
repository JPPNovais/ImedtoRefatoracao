<!--
  Seção Fotos do Paciente (briefing 2026-06-27_002 + addendum).
  Tipos aceitos: imagens (JPEG/PNG/WEBP). Limite: 2MB após redimensionamento.

  MODO PENDENTE (sem evolucaoId):
    - Foto redimensionada a 1600px/0.8 NA SELEÇÃO (CA22).
    - Preview via object URL local (CA22) — revogado ao remover/desmontar (CA32).
    - Sem upload ao S3 nesta fase. Sem gating "Salve primeiro" (CA27/R12).
    - Pai coleta via emit "pendentes" e sobe no salvar.

  MODO IMEDIATO (evolucaoId presente):
    - Upload direto. URLs batch on-demand (anti-N+1). Descartáveis (R6).
    - Soft-delete gated no backend (briefing 001).

  readOnly: placeholders fictícios, sem backend.
-->
<script setup lang="ts">
import { ref, onMounted, watch, computed, onUnmounted } from "vue"
import { AppEmptyState } from "@/components/ui"
import { prontuarioService } from "@/services/prontuarioService"
import { redimensionarImagem } from "@/services/imageUtils"
import type { Anexo, AnexoUrl } from "@/services/prontuarioService"

const MIME_ACEITOS = ["image/jpeg", "image/png", "image/webp"] as const
const EXT_ACEITAS = ".jpg,.jpeg,.png,.webp"
const LIMITE_BYTES = 2 * 1024 * 1024
const MAX_LADO_PX = 1600
const QUALIDADE = 0.8

const PLACEHOLDER_BASE64 =
    "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTYwIiBoZWlnaHQ9IjEyMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTYwIiBoZWlnaHQ9IjEyMCIgZmlsbD0iI2YxZjVmOSIvPjx0ZXh0IHg9IjgwIiB5PSI2MCIgZm9udC1mYW1pbHk9InNhbnMtc2VyaWYiIGZvbnQtc2l6ZT0iMTIiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGZpbGw9IiM5NGExYWYiPkZvdG88L3RleHQ+PC9zdmc+"

interface FotoExemplo { id: number; src: string }
const EXEMPLOS_FOTOS: FotoExemplo[] = [
    { id: -1, src: PLACEHOLDER_BASE64 },
    { id: -2, src: PLACEHOLDER_BASE64 },
    { id: -3, src: PLACEHOLDER_BASE64 },
]

// ── Item pendente ───────────────────────────────────────────────────────────
interface FotoPendente {
    chave: string
    arquivo: File           // já redimensionado (CA22)
    previewUrl: string      // object URL — revogado ao remover/desmontar (CA32)
}

const props = defineProps<{
    modelValue: Record<string, unknown>
    readOnly?: boolean
    pacienteId?: number | null
    evolucaoId?: number | null
}>()

const emit = defineEmits<{
    "update:modelValue": [v: Record<string, unknown>]
    /** Lista de arquivos (já redimensionados) a subir no salvar. */
    pendentes: [arquivos: File[]]
}>()

function notificarAlteracao() {
    emit("update:modelValue", { ...props.modelValue, _fotosAtualizadas: Date.now() })
}

// ── Estado modo pendente ────────────────────────────────────────────────────
const pendentes = ref<FotoPendente[]>([])
let _chaveSeq = 0

function emitirPendentes() {
    emit("pendentes", pendentes.value.map(p => p.arquivo))
}

// ── Estado modo imediato ────────────────────────────────────────────────────
const fotos = ref<Anexo[]>([])
const urls = ref<Map<number, string>>(new Map())
const carregando = ref(false)
const redimensionando = ref(false)
const enviando = ref(false)
const erro = ref<string | null>(null)
const confirmandoRemocao = ref<number | null>(null)
const fotoExpandida = ref<string | null>(null)

const modoPendente = computed(() => !props.evolucaoId)

// ── Carregar (modo imediato) ────────────────────────────────────────────────
async function carregarFotos() {
    if (!props.pacienteId || props.readOnly || modoPendente.value) return
    carregando.value = true
    erro.value = null
    try {
        const todos = await prontuarioService.listarAnexos(props.pacienteId, props.evolucaoId!)
        fotos.value = todos.filter(a => a.marcador === "foto-paciente")
        await carregarUrlsEmBatch()
    } catch {
        erro.value = "Não foi possível carregar as fotos."
    } finally {
        carregando.value = false
    }
}

async function carregarUrlsEmBatch() {
    if (!props.pacienteId || !fotos.value.length) return
    try {
        const ids = fotos.value.map(f => f.id)
        const lote: AnexoUrl[] = await prontuarioService.obterUrlsLote(props.pacienteId, ids)
        const mapa = new Map<number, string>()
        for (const item of lote) mapa.set(item.id, item.url)
        urls.value = mapa
    } catch {
        // Silenciosa: thumbnails ficam com placeholder
    }
}

onMounted(carregarFotos)
watch(() => [props.pacienteId, props.evolucaoId], carregarFotos)

// ── Validação ───────────────────────────────────────────────────────────────
function validarImagem(arquivo: File): string | null {
    if (!MIME_ACEITOS.includes(arquivo.type as any))
        return "Tipo não permitido. Use JPEG, PNG ou WebP."
    if (arquivo.size > LIMITE_BYTES * 5)
        return "Arquivo muito grande. Escolha uma foto menor que 10MB."
    return null
}

// ── Handler de seleção ──────────────────────────────────────────────────────
async function handleSelecao(event: Event) {
    const input = event.target as HTMLInputElement
    const arquivo = input.files?.[0]
    input.value = ""
    if (!arquivo) return

    const erroVal = validarImagem(arquivo)
    if (erroVal) { erro.value = erroVal; return }
    erro.value = null

    // CA22: redimensionar NA SELEÇÃO (não no salvar), em ambos os modos.
    redimensionando.value = true
    let reduzida: File
    try {
        reduzida = await redimensionarImagem(arquivo, MAX_LADO_PX, QUALIDADE)
        if (reduzida.size > LIMITE_BYTES) {
            erro.value = "Arquivo ainda muito grande após redimensionamento (máx. 2MB)."
            return
        }
    } catch {
        erro.value = "Não foi possível processar a imagem."
        return
    } finally {
        redimensionando.value = false
    }

    if (modoPendente.value) {
        // CA1' / CA22 / CA27: preview local, sem upload.
        const previewUrl = URL.createObjectURL(reduzida)
        pendentes.value.push({ chave: `fp-${++_chaveSeq}`, arquivo: reduzida, previewUrl })
        emitirPendentes()
        notificarAlteracao()
        return
    }

    // Modo imediato: upload direto.
    if (!props.pacienteId) return
    enviando.value = true
    try {
        await prontuarioService.uploadAnexoComMarcador(props.pacienteId, reduzida, "foto-paciente", props.evolucaoId!)
        await carregarFotos()
        notificarAlteracao()
    } catch {
        erro.value = "Falha ao enviar a foto."
    } finally {
        enviando.value = false
    }
}

// ── Remoção de pendente (CA23 + CA32) ──────────────────────────────────────
function removerPendente(chave: string) {
    const idx = pendentes.value.findIndex(p => p.chave === chave)
    if (idx === -1) return
    const [removido] = pendentes.value.splice(idx, 1)
    URL.revokeObjectURL(removido.previewUrl) // CA32: sem memory leak
    emitirPendentes()
    notificarAlteracao()
}

// ── Lightbox (modo imediato e pendente) ────────────────────────────────────
function abrirLightbox(src: string) {
    fotoExpandida.value = src
}
function fecharLightbox() { fotoExpandida.value = null }

// ── Soft-delete (modo imediato) ─────────────────────────────────────────────
async function removerFoto(id: number) {
    if (!props.pacienteId || props.readOnly) return
    try {
        await prontuarioService.removerAnexo(props.pacienteId, id)
        fotos.value = fotos.value.filter(f => f.id !== id)
        const m = new Map(urls.value); m.delete(id); urls.value = m
        notificarAlteracao()
    } catch {
        erro.value = "Não foi possível remover a foto."
    } finally {
        confirmandoRemocao.value = null
    }
}

// CA32: revogar todos os object URLs ao desmontar.
onUnmounted(() => {
    pendentes.value.forEach(p => URL.revokeObjectURL(p.previewUrl))
    pendentes.value = []
})
</script>

<template>
    <div class="secao-fotos">
        <!-- Botão upload — sem gating por evolucaoId (CA27 / R12) -->
        <div v-if="!readOnly" class="acao-row">
            <label class="btn-upload" :class="{ 'btn-upload--desabilitado': enviando || redimensionando }">
                <i class="fa-solid fa-camera"></i>
                {{ redimensionando ? "Processando..." : enviando ? "Enviando..." : "Adicionar foto" }}
                <input
                    type="file"
                    :accept="EXT_ACEITAS"
                    :disabled="enviando || redimensionando"
                    class="input-file-hidden"
                    @change="handleSelecao"
                />
            </label>
            <span v-if="modoPendente && !readOnly && pendentes.length > 0" class="aviso-pendente">
                {{ pendentes.length }} foto{{ pendentes.length > 1 ? 's' : '' }} aguardando salvar
            </span>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>
        <p v-if="carregando" class="estado-msg">Carregando...</p>

        <!-- Prévia read-only -->
        <div v-else-if="readOnly" class="grade-fotos">
            <div v-for="e in EXEMPLOS_FOTOS" :key="e.id" class="thumbnail-wrap">
                <img :src="e.src" alt="Foto de exemplo" class="thumbnail" />
            </div>
        </div>

        <!-- Modo pendente: grade de previews locais -->
        <div v-else-if="modoPendente && pendentes.length > 0" class="grade-fotos">
            <div v-for="p in pendentes" :key="p.chave" class="thumbnail-wrap thumbnail-wrap--pendente">
                <img
                    :src="p.previewUrl"
                    :alt="p.arquivo.name"
                    class="thumbnail"
                    @click="abrirLightbox(p.previewUrl)"
                />
                <div class="thumbnail-acoes">
                    <button type="button" class="btn-thumb btn-thumb-danger" title="Remover" @click="removerPendente(p.chave)">
                        <i class="fa-solid fa-xmark"></i>
                    </button>
                </div>
            </div>
        </div>

        <!-- Empty state (modo pendente sem pendentes, ou modo imediato sem fotos) -->
        <AppEmptyState
            v-else-if="!carregando && fotos.length === 0 && !pendentes.length"
            mensagem="Nenhuma foto adicionada."
        />

        <!-- Modo imediato: grade de fotos do servidor -->
        <div v-else-if="!modoPendente" class="grade-fotos">
            <div v-for="foto in fotos" :key="foto.id" class="thumbnail-wrap">
                <img
                    :src="urls.get(foto.id) ?? PLACEHOLDER_BASE64"
                    :alt="foto.nomeOriginal"
                    class="thumbnail"
                    @click="abrirLightbox(urls.get(foto.id) ?? PLACEHOLDER_BASE64)"
                />
                <div v-if="!readOnly" class="thumbnail-acoes">
                    <template v-if="confirmandoRemocao === foto.id">
                        <button type="button" class="btn-thumb btn-thumb-danger" title="Confirmar remoção" @click="removerFoto(foto.id)">
                            <i class="fa-solid fa-check"></i>
                        </button>
                        <button type="button" class="btn-thumb" title="Cancelar" @click="confirmandoRemocao = null">
                            <i class="fa-solid fa-xmark"></i>
                        </button>
                    </template>
                    <button v-else type="button" class="btn-thumb btn-thumb-danger" title="Remover" @click="confirmandoRemocao = foto.id">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </div>
            </div>
        </div>

        <!-- Lightbox -->
        <Teleport to="body">
            <div v-if="fotoExpandida" class="lightbox-overlay" @click.self="fecharLightbox">
                <button type="button" class="lightbox-fechar" @click="fecharLightbox">
                    <i class="fa-solid fa-xmark"></i>
                </button>
                <img :src="fotoExpandida" alt="Foto ampliada" class="lightbox-img" />
            </div>
        </Teleport>
    </div>
</template>

<style scoped>
.secao-fotos { display: flex; flex-direction: column; gap: 0.75rem; }

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

.grade-fotos {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
    gap: 0.5rem;
}

.thumbnail-wrap {
    position: relative; border-radius: var(--radius); overflow: hidden;
    aspect-ratio: 4/3; background: var(--bg-muted);
}
.thumbnail-wrap--pendente { outline: 2px dashed hsl(var(--warning)); }

.thumbnail {
    width: 100%; height: 100%; object-fit: cover;
    cursor: pointer; transition: opacity 0.15s; display: block;
}
.thumbnail:hover { opacity: 0.85; }

.thumbnail-acoes {
    position: absolute; bottom: 0; right: 0;
    display: flex; gap: 0.2rem; padding: 0.25rem;
    background: rgba(0,0,0,0.45); border-radius: var(--radius) 0 var(--radius) 0;
    opacity: 0; transition: opacity 0.15s;
}
.thumbnail-wrap:hover .thumbnail-acoes { opacity: 1; }

.btn-thumb {
    display: inline-flex; align-items: center; justify-content: center;
    width: 1.6rem; height: 1.6rem; border-radius: var(--radius-sm);
    background: rgba(255,255,255,0.15); color: #fff;
    border: none; cursor: pointer; font-size: var(--text-xs);
    transition: background 0.12s;
}
.btn-thumb:hover { background: rgba(255,255,255,0.3); }
.btn-thumb-danger { background: hsl(var(--danger) / 0.7); }
.btn-thumb-danger:hover { background: hsl(var(--danger) / 0.9); }

.lightbox-overlay {
    position: fixed; inset: 0; background: rgba(0,0,0,0.85);
    display: flex; align-items: center; justify-content: center;
    z-index: 1000; padding: 1.5rem;
}
.lightbox-img { max-width: 100%; max-height: 100%; object-fit: contain; border-radius: var(--radius); }
.lightbox-fechar {
    position: absolute; top: 1rem; right: 1rem;
    width: 2.25rem; height: 2.25rem; border-radius: 50%;
    background: rgba(255,255,255,0.15); color: #fff; border: none; cursor: pointer;
    display: flex; align-items: center; justify-content: center;
    font-size: var(--text-base); transition: background 0.15s;
}
.lightbox-fechar:hover { background: rgba(255,255,255,0.3); }
</style>
