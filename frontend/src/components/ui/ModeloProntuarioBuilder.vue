<script setup lang="ts">
/**
 * Builder visual de modelos de prontuário — compartilhado entre tenant e admin.
 * Catálogo, tipos e helpers exportados em modeloProntuarioBuilder.ts (fonte única de verdade).
 */
import { computed, ref, watch } from "vue"
import AppField from "./AppField.vue"
import AppInput from "./AppInput.vue"
import AppModal from "./AppModal.vue"
import AppTextarea from "./AppTextarea.vue"
import SecaoProntuario from "@/components/prontuario/SecaoProntuario.vue"
import type { SecaoModelo } from "@/services/prontuarioService"
import {
    SECOES_MODELO_PRONTUARIO,
    EXEMPLOS_SECAO_MODELO,
    parsearEstruturaJson,
    gerarEstruturaJson,
} from "./modeloProntuarioBuilder"

// ─── Props / emits ────────────────────────────────────────────────────────────

const props = withDefaults(defineProps<{
    nome: string
    descricao: string
    estruturaJson: string
    mostrarNomeDescricao?: boolean
    disabled?: boolean
}>(), {
    mostrarNomeDescricao: true,
    disabled: false,
})

const emit = defineEmits<{
    "update:nome": [value: string]
    "update:descricao": [value: string]
    "update:estruturaJson": [value: string]
    "update:valido": [value: boolean]
}>()

// ─── Estado interno ───────────────────────────────────────────────────────────

const emptySecoes = (): Record<string, boolean> =>
    SECOES_MODELO_PRONTUARIO.reduce((acc, s) => { acc[s.key] = false; return acc }, {} as Record<string, boolean>)

const secoes = ref<Record<string, boolean>>(emptySecoes())
const ordem = ref<string[]>([])
const customizadas = ref<SecaoModelo[]>([])
const inicializado = ref(false)

const labelDe = (key: string) =>
    SECOES_MODELO_PRONTUARIO.find(s => s.key === key)?.label ?? key

// ─── Prévia de seção (CA1–CA13) ───────────────────────────────────────────────
// secaoEmPrevia: chave da seção sendo pré-visualizada, ou null quando fechado.
// Só seções do catálogo têm prévia (R6). O modal usa AppModal + SecaoProntuario.
const secaoEmPrevia = ref<string | null>(null)

function abrirPrevia(key: string) {
    // R6: só seções do catálogo
    if (!EXEMPLOS_SECAO_MODELO[key]) return
    secaoEmPrevia.value = key
}

function fecharPrevia() {
    secaoEmPrevia.value = null
}

const previaDados = computed(() => {
    if (!secaoEmPrevia.value) return null
    const def = SECOES_MODELO_PRONTUARIO.find(s => s.key === secaoEmPrevia.value)
    if (!def) return null
    return {
        key: def.key,
        label: def.label,
        tipo: def.tipo,
        exemplo: EXEMPLOS_SECAO_MODELO[def.key],
    }
})

// ─── Parse inicial ────────────────────────────────────────────────────────────

function inicializar(json: string) {
    const { conhecidas, customizadas: custom } = parsearEstruturaJson(json)
    customizadas.value = custom

    const novasSecoes = emptySecoes()
    const chavesSelecionadas = new Set(conhecidas.map(s => s.chave))
    chavesSelecionadas.forEach(k => { if (novasSecoes[k] !== undefined) novasSecoes[k] = true })
    secoes.value = novasSecoes

    if (conhecidas.length > 0) {
        const ordenados = [...conhecidas].sort((a, b) => a.ordem - b.ordem)
        ordem.value = ordenados.map(s => s.chave).filter(k => chavesSelecionadas.has(k))
    } else {
        ordem.value = []
    }
}

inicializar(props.estruturaJson)
inicializado.value = true

// Reage a mudança externa de estruturaJson (ex: carregar modelo ao editar)
watch(() => props.estruturaJson, (novoJson) => {
    // Evita re-parse quando a mudança veio do próprio componente
    const jsonAtual = gerarEstruturaJson(secoesAtivas.value, customizadas.value)
    if (novoJson !== jsonAtual) {
        inicializar(novoJson)
    }
})

// ─── Lógica interna ───────────────────────────────────────────────────────────

function sincronizarOrdem() {
    const ativas = new Set(SECOES_MODELO_PRONTUARIO.map(s => s.key).filter(k => secoes.value[k]))
    const novaOrdem: string[] = []
    ordem.value.forEach(k => { if (ativas.has(k)) { novaOrdem.push(k); ativas.delete(k) } })
    ativas.forEach(k => novaOrdem.push(k))
    ordem.value = novaOrdem
}

watch(
    () => ({ ...secoes.value }),
    () => sincronizarOrdem(),
    { deep: true },
)

function adicionarSecao(key: string) {
    if (props.disabled) return
    secoes.value[key] = true
}

function removerSecao(key: string) {
    if (props.disabled) return
    secoes.value[key] = false
}

// ─── Drag & drop de reordenação ────────────────────────────────────────────────

const arrastando = ref<string | null>(null)

function aoIniciarArraste(key: string) {
    if (props.disabled) return
    arrastando.value = key
}

function aoArrastarSobre(e: DragEvent, alvo: string) {
    if (!arrastando.value || arrastando.value === alvo) return
    e.preventDefault()
    const arr = [...ordem.value]
    const origem = arr.indexOf(arrastando.value)
    if (origem === -1) return
    arr.splice(origem, 1)
    const destino = arr.indexOf(alvo)
    if (destino === -1) return
    const alvoEl = e.currentTarget as HTMLElement
    const box = alvoEl.getBoundingClientRect()
    const depois = e.clientY > box.top + box.height / 2
    arr.splice(depois ? destino + 1 : destino, 0, arrastando.value)
    ordem.value = arr
}

function aoFinalizarArraste() {
    arrastando.value = null
}

const secoesAtivas = computed((): SecaoModelo[] =>
    ordem.value
        .filter(k => secoes.value[k])
        .map((key, idx) => {
            const def = SECOES_MODELO_PRONTUARIO.find(s => s.key === key)!
            return { chave: key, titulo: def.label, tipo: def.tipo, ordem: idx }
        })
)

const secoesDisponiveis = computed(() =>
    SECOES_MODELO_PRONTUARIO.filter(s => !secoes.value[s.key])
)

const totalLabel = computed(() => {
    const n = secoesAtivas.value.length
    return `${n} ${n === 1 ? "seção" : "seções"}`
})

const valido = computed(() => secoesAtivas.value.length > 0 && props.nome.trim().length > 0)

// Emite estruturaJson sempre que a seleção/ordem muda
watch(
    [secoesAtivas, customizadas],
    () => {
        if (!inicializado.value) return
        emit("update:estruturaJson", gerarEstruturaJson(secoesAtivas.value, customizadas.value))
    },
    { deep: true },
)

watch(valido, (v) => emit("update:valido", v), { immediate: true })
</script>

<template>
    <div class="mpb-builder">
        <!-- Campos de nome e descrição -->
        <template v-if="mostrarNomeDescricao">
            <AppField label="Nome do modelo" required>
                <AppInput
                    :model-value="nome"
                    placeholder="Ex.: Primeira consulta, Evolução pós-operatória"
                    :disabled="disabled"
                    @update:model-value="emit('update:nome', $event as string)"
                />
            </AppField>

            <AppField label="Descrição (opcional)">
                <AppTextarea
                    :model-value="descricao"
                    placeholder="Descrição breve do objetivo deste modelo."
                    :rows="3"
                    :disabled="disabled"
                    @update:model-value="emit('update:descricao', $event as string)"
                />
            </AppField>
        </template>

        <!-- Aviso de seções customizadas (W5-CA6) -->
        <div v-if="customizadas.length" class="mpb-aviso-custom">
            <i class="fa-solid fa-circle-info mpb-aviso-icone" />
            <span>
                Este modelo tem {{ customizadas.length }}
                {{ customizadas.length === 1 ? "seção customizada" : "seções customizadas" }}
                que {{ customizadas.length === 1 ? "será preservada" : "serão preservadas" }} ao salvar.
            </span>
        </div>

        <!-- Seções do modelo (ordenáveis) -->
        <div class="mpb-secoes-bloco">
            <div class="mpb-sec-head">
                <label class="mpb-secoes-label">Seções do modelo</label>
                <span class="mpb-sec-count">{{ totalLabel }}</span>
            </div>
            <p class="mpb-sec-hint">
                Arraste pelo <i class="fa-solid fa-grip-vertical" /> para definir a ordem em que as
                seções aparecem no prontuário.
            </p>

            <div class="mpb-ord-list">
                <div
                    v-for="(secao, pos) in secoesAtivas"
                    :key="secao.chave"
                    class="mpb-ord-row"
                    :class="{ 'is-dragging': arrastando === secao.chave }"
                    :draggable="!disabled"
                    @dragstart="aoIniciarArraste(secao.chave)"
                    @dragover="aoArrastarSobre($event, secao.chave)"
                    @dragend="aoFinalizarArraste"
                >
                    <i class="fa-solid fa-grip-vertical mpb-grip" />
                    <span class="mpb-num">{{ pos + 1 }}</span>
                    <span class="mpb-nm">{{ labelDe(secao.chave) }}</span>
                    <button
                        v-if="EXEMPLOS_SECAO_MODELO[secao.chave]"
                        type="button"
                        class="mpb-preview"
                        :title="`Pré-visualizar seção ${labelDe(secao.chave)}`"
                        :aria-label="`Pré-visualizar seção ${labelDe(secao.chave)}`"
                        @click.stop="abrirPrevia(secao.chave)"
                    >
                        <i class="fa-regular fa-eye" />
                    </button>
                    <button
                        type="button"
                        class="mpb-rm"
                        title="Remover seção"
                        :disabled="disabled"
                        @click="removerSecao(secao.chave)"
                    >
                        <i class="fa-solid fa-xmark" />
                    </button>
                </div>

                <div v-if="!secoesAtivas.length" class="mpb-ord-empty">
                    Nenhuma seção adicionada ainda. Selecione abaixo para montar o modelo.
                </div>
            </div>
        </div>

        <!-- Adicionar seção -->
        <div class="mpb-avail-bloco">
            <div class="mpb-avail-head">Adicionar seção</div>
            <div v-if="secoesDisponiveis.length" class="mpb-avail-chips">
                <span
                    v-for="s in secoesDisponiveis"
                    :key="s.key"
                    class="mpb-chip-wrapper"
                >
                    <button
                        type="button"
                        class="mpb-chip-add"
                        :title="s.info"
                        :disabled="disabled"
                        @click="adicionarSecao(s.key)"
                    >
                        <i class="fa-solid fa-plus" /> {{ s.label }}
                    </button>
                    <button
                        v-if="EXEMPLOS_SECAO_MODELO[s.key]"
                        type="button"
                        class="mpb-chip-preview"
                        :title="`Pré-visualizar seção ${s.label}`"
                        :aria-label="`Pré-visualizar seção ${s.label}`"
                        @click.stop="abrirPrevia(s.key)"
                    >
                        <i class="fa-regular fa-eye" />
                    </button>
                </span>
            </div>
            <span v-else class="mpb-all-done">Todas as seções já foram adicionadas.</span>
        </div>

        <!-- Modal de prévia de seção (R1/R5/briefing 2026-06-26_001) -->
        <AppModal
            :aberto="!!previaDados"
            :titulo="previaDados ? `Prévia — ${previaDados.label}` : ''"
            largura="lg"
            @fechar="fecharPrevia"
        >
            <template v-if="previaDados">
                <p class="mpb-previa-aviso">
                    <i class="fa-solid fa-circle-info" /> Exemplo somente leitura — não altera o modelo.
                </p>
                <SecaoProntuario
                    :chave="previaDados.key"
                    :titulo="previaDados.label"
                    :tipo="previaDados.tipo"
                    :model-value="previaDados.exemplo"
                    :read-only="true"
                    paciente-sexo="F"
                    @update:model-value="() => {}"
                />
            </template>
            <template #rodape>
                <button type="button" class="btn btn-secondary" @click="fecharPrevia">
                    Fechar
                </button>
            </template>
        </AppModal>
    </div>
</template>

<style scoped>
.mpb-builder {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

/* ── Aviso de seções customizadas ── */
.mpb-aviso-custom {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.6rem 0.875rem;
    background: hsl(var(--info) / 0.1);
    border: 1px solid hsl(var(--info) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.82em;
    color: var(--text);
}

.mpb-aviso-icone {
    color: hsl(var(--info));
    flex-shrink: 0;
}

/* ── Seções ── */
.mpb-secoes-bloco {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
}

.mpb-sec-head {
    display: flex;
    align-items: baseline;
    justify-content: space-between;
    gap: 0.5rem;
}

.mpb-secoes-label {
    font-size: 0.78em;
    font-weight: 700;
    color: var(--text-muted);
}

.mpb-sec-count {
    font-size: 0.72em;
    font-weight: 700;
    color: hsl(var(--primary));
}

.mpb-sec-hint {
    font-size: 0.76em;
    color: var(--text-muted);
    margin: 0 0 0.25rem;
}

.mpb-sec-hint i {
    font-size: 0.85em;
}

/* ── Lista ordenável ── */
.mpb-ord-list {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    min-height: 8px;
}

.mpb-ord-row {
    display: flex;
    align-items: center;
    gap: 0.65rem;
    padding: 0.55rem 0.7rem;
    border: 1px solid var(--border);
    border-radius: calc(var(--radius) - 2px);
    background: var(--bg-card);
    transition: border-color 0.12s, box-shadow 0.12s;
}

.mpb-ord-row:hover {
    border-color: hsl(var(--primary) / 0.4);
}

.mpb-ord-row.is-dragging {
    opacity: 0.45;
    box-shadow: 0 4px 12px hsl(var(--primary) / 0.16);
}

.mpb-grip {
    color: var(--text-faint);
    cursor: grab;
    font-size: 0.8em;
    flex: none;
    padding: 2px;
}

.mpb-grip:active {
    cursor: grabbing;
}

.mpb-num {
    width: 22px;
    height: 22px;
    flex: none;
    border-radius: var(--radius-full);
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    font-size: 0.7em;
    font-weight: 800;
    display: flex;
    align-items: center;
    justify-content: center;
}

.mpb-nm {
    flex: 1;
    font-size: 0.84em;
    font-weight: 600;
    color: var(--text);
    min-width: 0;
}

.mpb-rm {
    flex: none;
    border: none;
    background: none;
    color: var(--text-faint);
    cursor: pointer;
    font-size: 0.85em;
    padding: 0.25rem;
    border-radius: var(--radius-sm);
    transition: color 0.12s, background 0.12s;
}

.mpb-rm:hover:not(:disabled) {
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.08);
}

.mpb-rm:disabled {
    cursor: not-allowed;
    opacity: 0.5;
}

.mpb-ord-empty {
    border: 1px dashed var(--border);
    border-radius: calc(var(--radius) - 2px);
    padding: 1.1rem;
    text-align: center;
    font-size: 0.8em;
    color: var(--text-muted);
}

/* ── Adicionar seção ── */
.mpb-avail-bloco {
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.mpb-avail-head {
    font-size: 0.7em;
    font-weight: 700;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    color: var(--text-muted);
}

.mpb-avail-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
}

.mpb-chip-add {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
    padding: 0.4rem 0.75rem;
    border: 1px dashed var(--border-strong);
    border-radius: var(--radius-full);
    background: var(--bg-card);
    font: inherit;
    font-size: 0.78em;
    font-weight: 600;
    color: var(--text-muted);
    cursor: pointer;
    transition: all 0.12s;
}

.mpb-chip-add:hover:not(:disabled) {
    border-color: hsl(var(--primary));
    color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.05);
    border-style: solid;
}

.mpb-chip-add:disabled {
    cursor: not-allowed;
    opacity: 0.5;
}

.mpb-chip-add i {
    font-size: 0.8em;
}

.mpb-all-done {
    font-size: 0.78em;
    color: var(--text-muted);
}

/* ── Botão de prévia na linha de seção adicionada ── */
.mpb-preview {
    flex: none;
    border: none;
    background: none;
    color: var(--text-faint);
    cursor: pointer;
    padding: 0.25rem;
    border-radius: var(--radius-sm);
    transition: color 0.12s, background 0.12s;
    line-height: 1;
}

.mpb-preview:hover {
    color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.08);
}

/* ── Wrapper do chip + botão de prévia ── */
.mpb-chip-wrapper {
    display: inline-flex;
    align-items: center;
    gap: 0.15rem;
}

/* ── Botão de prévia adjacente ao chip ── */
.mpb-chip-preview {
    flex: none;
    border: none;
    background: none;
    color: var(--text-faint);
    cursor: pointer;
    padding: 0.25rem 0.3rem;
    border-radius: var(--radius-sm);
    transition: color 0.12s, background 0.12s;
    line-height: 1;
    font-size: var(--text-sm);
}

.mpb-chip-preview:hover {
    color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.08);
}

/* ── Aviso no modal de prévia ── */
.mpb-previa-aviso {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.45rem 0.75rem;
    background: hsl(var(--info) / 0.08);
    border: 1px solid hsl(var(--info) / 0.25);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
    color: var(--text-muted);
}

.mpb-previa-aviso i {
    color: hsl(var(--info));
    flex-shrink: 0;
}
</style>
