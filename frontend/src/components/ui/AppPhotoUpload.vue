<script setup lang="ts">
/**
 * AppPhotoUpload — avatar/logo upload reutilizável do design system.
 *
 * Renderiza um botão circular com a foto atual (ou iniciais como placeholder),
 * permite trocar via file picker e remover via botão secundário. NÃO faz HTTP
 * direto — emite eventos para a view tratar (consistente com o BFF do produto:
 * stores/services controlam HTTP, componentes ficam burros).
 *
 * Uso típico:
 *   <AppPhotoUpload
 *     :foto-url="estab.fotoUrl"
 *     :iniciais-fallback="estab.nomeFantasia"
 *     titulo="Logo do estabelecimento"
 *     descricao="Aparece nos PDFs e relatórios. Recomendado: 400×400px."
 *     :loading="enviando"
 *     :disabled="!podeEditar"
 *     @upload="onUpload"
 *     @remover="onRemover"
 *   />
 */
import { computed, ref } from "vue"

const props = withDefaults(defineProps<{
    /** URL atual da foto (presigned do S3, ou null/vazio = sem foto). */
    fotoUrl?: string | null
    /** Texto/nome usado para gerar iniciais quando não há foto. */
    iniciaisFallback?: string | null
    /** Título exibido ao lado do avatar. */
    titulo?: string
    /** Descrição/hint exibido sob o título. */
    descricao?: string
    /** Loading enquanto upload/remoção rodam. */
    loading?: boolean
    /** Desabilita toda interação (usuário sem permissão, etc). */
    disabled?: boolean
    /** Mensagem motivo do disabled (aparece no title do avatar). */
    motivoDisabled?: string | null
    /** Mensagem de erro a exibir abaixo da descrição. */
    erro?: string | null
    /** Tipos MIME aceitos. Default: jpeg/png/webp/gif. */
    accept?: string
    /** Tamanho máximo em bytes (default 2 MB — limite do backend para fotos). */
    tamanhoMaxBytes?: number
}>(), {
    fotoUrl: null,
    iniciaisFallback: null,
    titulo: "Foto",
    descricao: "",
    loading: false,
    disabled: false,
    motivoDisabled: null,
    erro: null,
    accept: "image/jpeg,image/png,image/webp,image/gif",
    tamanhoMaxBytes: 2 * 1024 * 1024,
})

const emit = defineEmits<{
    /** Arquivo escolhido pelo usuário (já validado: tipo + tamanho). */
    (e: "upload", arquivo: File): void
    /** Usuário pediu para remover a foto atual. */
    (e: "remover"): void
    /** Erro de validação client-side (tipo/tamanho). View pode logar em toast. */
    (e: "erroValidacao", mensagem: string): void
}>()

const fileInput = ref<HTMLInputElement | null>(null)

const iniciais = computed(() => {
    const nome = (props.iniciaisFallback ?? "").trim()
    if (!nome) return "?"
    const partes = nome.split(/\s+/).filter(Boolean)
    if (partes.length === 1) return partes[0]!.slice(0, 2).toUpperCase()
    return (partes[0]![0]! + partes[partes.length - 1]![0]!).toUpperCase()
})

const temFoto = computed(() => !!props.fotoUrl)

const titleAvatar = computed(() => {
    if (props.disabled && props.motivoDisabled) return props.motivoDisabled
    if (props.disabled) return ""
    return temFoto.value ? "Clique para trocar a foto" : "Clique para enviar uma foto"
})

function abrirSeletor() {
    if (props.disabled || props.loading) return
    fileInput.value?.click()
}

function onArquivoSelecionado(ev: Event) {
    const arquivo = (ev.target as HTMLInputElement).files?.[0]
    if (!arquivo) return

    // Validacao client-side — espelha o backend pra evitar 422 trivial.
    const tiposAceitos = props.accept.split(",").map(s => s.trim().toLowerCase())
    if (!tiposAceitos.includes(arquivo.type.toLowerCase())) {
        emit("erroValidacao", "Formato não suportado. Use JPG, PNG, WebP ou GIF.")
        resetInput()
        return
    }
    if (arquivo.size > props.tamanhoMaxBytes) {
        const mb = (props.tamanhoMaxBytes / (1024 * 1024)).toFixed(0)
        emit("erroValidacao", `Arquivo muito grande. Máximo ${mb} MB.`)
        resetInput()
        return
    }

    emit("upload", arquivo)
    resetInput()
}

function resetInput() {
    // Permite reescolher o mesmo arquivo (caso a view processe e queira refazer).
    if (fileInput.value) fileInput.value.value = ""
}

function onRemoverClick() {
    if (props.disabled || props.loading) return
    emit("remover")
}
</script>

<template>
    <div class="photo-upload">
        <button
            type="button"
            class="avatar"
            :class="{ 'avatar--bloqueado': disabled, 'avatar--enviando': loading }"
            :disabled="disabled || loading"
            :aria-label="temFoto ? `Trocar ${titulo.toLowerCase()}` : `Adicionar ${titulo.toLowerCase()}`"
            :title="titleAvatar"
            @click="abrirSeletor"
        >
            <img v-if="temFoto" :src="fotoUrl!" :alt="titulo" />
            <span v-else class="avatar-iniciais">{{ iniciais }}</span>
            <span class="avatar-overlay">
                <span v-if="loading" class="avatar-spinner" aria-hidden="true"></span>
                <i v-else class="fa-solid fa-camera" aria-hidden="true"></i>
            </span>
        </button>

        <div class="info">
            <span class="info-titulo">{{ titulo }}</span>
            <span v-if="descricao" class="info-desc">{{ descricao }}</span>

            <input
                ref="fileInput"
                type="file"
                :accept="accept"
                hidden
                @change="onArquivoSelecionado"
            />

            <div class="info-acoes">
                <button
                    type="button"
                    class="btn-secundario"
                    :disabled="disabled || loading"
                    @click="abrirSeletor"
                >
                    <i class="fa-solid fa-arrows-rotate" aria-hidden="true" />
                    {{ temFoto ? "Trocar foto" : "Enviar foto" }}
                </button>

                <button
                    v-if="temFoto"
                    type="button"
                    class="btn-remover"
                    :disabled="disabled || loading"
                    @click="onRemoverClick"
                >
                    <i class="fa-solid fa-trash" aria-hidden="true" />
                    Remover
                </button>
            </div>

            <p v-if="erro" class="info-erro">{{ erro }}</p>
        </div>
    </div>
</template>

<style scoped>
.photo-upload {
    display: flex;
    gap: 1.25rem;
    align-items: flex-start;
}

.avatar {
    position: relative;
    width: 96px;
    height: 96px;
    border-radius: 50%;
    border: 2px solid var(--border-strong);
    background: var(--bg-soft, #f1f5f9);
    overflow: hidden;
    cursor: pointer;
    flex-shrink: 0;
    padding: 0;
    transition: border-color 0.15s, transform 0.15s;
}
.avatar:hover:not(:disabled) { border-color: var(--primary); transform: scale(1.02); }
.avatar:focus-visible { outline: 2px solid var(--primary); outline-offset: 2px; }
.avatar:disabled { cursor: not-allowed; opacity: 0.7; }

.avatar img {
    width: 100%; height: 100%; object-fit: cover; display: block;
}
.avatar-iniciais {
    display: flex; align-items: center; justify-content: center;
    width: 100%; height: 100%;
    font-size: 1.75rem; font-weight: 700;
    color: var(--text-muted);
    text-transform: uppercase;
}

.avatar-overlay {
    position: absolute; inset: 0;
    display: flex; align-items: center; justify-content: center;
    background: rgba(0, 0, 0, 0.42);
    color: #fff; font-size: 1.5rem;
    opacity: 0;
    transition: opacity 0.15s;
}
.avatar:hover:not(:disabled) .avatar-overlay,
.avatar--enviando .avatar-overlay {
    opacity: 1;
}

.avatar-spinner {
    width: 22px; height: 22px;
    border: 2px solid rgba(255,255,255,0.4);
    border-top-color: #fff;
    border-radius: 50%;
    animation: photo-spin 0.8s linear infinite;
}
@keyframes photo-spin { to { transform: rotate(360deg); } }

.info {
    flex: 1;
    display: flex; flex-direction: column; gap: 0.35rem;
    min-width: 0;
}
.info-titulo { font-size: 0.95em; font-weight: 700; color: var(--text); }
.info-desc   { font-size: 0.82em; color: var(--text-muted); line-height: 1.4; }

.info-acoes {
    display: flex; gap: 0.5rem; flex-wrap: wrap; margin-top: 0.35rem;
}

.btn-secundario, .btn-remover {
    display: inline-flex; align-items: center; gap: 0.4rem;
    padding: 0.42rem 0.9rem;
    border-radius: var(--radius);
    font-family: inherit; font-size: 0.82em; font-weight: 600;
    cursor: pointer;
    transition: background 0.12s, color 0.12s, border-color 0.12s;
}
.btn-secundario {
    background: var(--bg-card);
    color: var(--primary);
    border: 1px solid var(--border-strong);
}
.btn-secundario:hover:not(:disabled) {
    background: var(--primary-light, #ede9fe);
    border-color: var(--primary);
}
.btn-remover {
    background: transparent;
    color: var(--danger);
    border: 1px solid transparent;
}
.btn-remover:hover:not(:disabled) {
    background: rgba(220, 38, 38, 0.08);
    border-color: rgba(220, 38, 38, 0.25);
}
.btn-secundario:disabled, .btn-remover:disabled {
    opacity: 0.5; cursor: not-allowed;
}

.info-erro {
    color: var(--danger);
    font-size: 0.82em;
    margin: 0.35rem 0 0;
}

@media (max-width: 540px) {
    .photo-upload { gap: 1rem; }
    .avatar { width: 76px; height: 76px; }
    .avatar-iniciais { font-size: 1.4rem; }
}
</style>
