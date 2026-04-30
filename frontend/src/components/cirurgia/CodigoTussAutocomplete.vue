<script setup lang="ts">
/**
 * CodigoTussAutocomplete — campo de busca/autocomplete para o catalogo TUSS/CBHPM.
 * Emite `update:modelValue` com o codigo selecionado e `selecionar` com o
 * objeto completo do procedimento (para preencher tambem o nome da cirurgia).
 */
import { ref, watch, computed } from "vue"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { catalogoService, type ProcedimentoCatalogo } from "@/services/catalogoService"

interface Props {
    modelValue?: string
    disabled?: boolean
    placeholder?: string
}

const props = withDefaults(defineProps<Props>(), {
    modelValue:  "",
    disabled:    false,
    placeholder: "Buscar por codigo ou nome...",
})

const emit = defineEmits<{
    "update:modelValue": [value: string]
    selecionar: [procedimento: ProcedimentoCatalogo]
}>()

// ─── Estado interno ───────────────────────────────────────────────────────────

const termoInput   = ref(props.modelValue ?? "")
const termo        = useDebouncedRef(termoInput, 300)
const resultados   = ref<ProcedimentoCatalogo[]>([])
const buscando     = ref(false)
const aberto       = ref(false)
const erroBusca    = ref<string | null>(null)
const itemFocado   = ref(-1)

// Sincroniza prop -> campo quando o valor externo muda programaticamente
watch(() => props.modelValue, (val) => {
    if (val !== termoInput.value) termoInput.value = val ?? ""
})

// Dispara busca quando o debounced ref muda
watch(termo, async (valor) => {
    if (valor.length < 2) {
        resultados.value = []
        aberto.value = false
        return
    }
    buscando.value = true
    erroBusca.value = null
    try {
        resultados.value = await catalogoService.buscarProcedimentos(valor)
        aberto.value = resultados.value.length > 0
        itemFocado.value = -1
    } catch {
        erroBusca.value = "Erro ao buscar procedimentos."
    } finally {
        buscando.value = false
    }
})

// ─── Selecionar item ──────────────────────────────────────────────────────────

function selecionar(proc: ProcedimentoCatalogo) {
    termoInput.value = proc.codigo
    aberto.value = false
    resultados.value = []
    emit("update:modelValue", proc.codigo)
    emit("selecionar", proc)
}

// ─── Teclado ──────────────────────────────────────────────────────────────────

function onKeydown(e: KeyboardEvent) {
    if (!aberto.value) return
    if (e.key === "ArrowDown") {
        e.preventDefault()
        itemFocado.value = Math.min(itemFocado.value + 1, resultados.value.length - 1)
    } else if (e.key === "ArrowUp") {
        e.preventDefault()
        itemFocado.value = Math.max(itemFocado.value - 1, 0)
    } else if (e.key === "Enter") {
        e.preventDefault()
        if (itemFocado.value >= 0) selecionar(resultados.value[itemFocado.value])
    } else if (e.key === "Escape") {
        aberto.value = false
    }
}

// ─── Fechar ao clicar fora ────────────────────────────────────────────────────

function onBlur() {
    // Pequeno delay para permitir que o click no item dispare primeiro
    setTimeout(() => { aberto.value = false }, 150)
}

function onInput(e: Event) {
    const val = (e.target as HTMLInputElement).value
    termoInput.value = val
    emit("update:modelValue", val)
}

const listaId = `tuss-lista-${Math.random().toString(36).slice(2, 7)}`

const ORIGEM_LABEL: Record<string, string> = {
    TUSS:        "TUSS",
    CBHPM:       "CBHPM",
    CUSTOMIZADO: "Custom",
}
</script>

<template>
    <div class="autocomplete-wrapper" role="combobox" :aria-expanded="aberto" aria-haspopup="listbox">
        <div class="input-row">
            <input
                class="tuss-input"
                type="text"
                :value="termoInput"
                :placeholder="placeholder"
                :disabled="disabled"
                autocomplete="off"
                :aria-autocomplete="'list'"
                :aria-controls="listaId"
                :aria-activedescendant="itemFocado >= 0 ? `tuss-item-${itemFocado}` : undefined"
                @input="onInput"
                @keydown="onKeydown"
                @blur="onBlur"
                @focus="() => { if (resultados.length) aberto = true }"
            />
            <span v-if="buscando" class="spinner-inline" aria-hidden="true">
                <i class="fa-solid fa-spinner fa-spin"></i>
            </span>
        </div>

        <p v-if="erroBusca" class="erro-busca" role="alert">{{ erroBusca }}</p>

        <ul
            v-show="aberto && resultados.length > 0"
            :id="listaId"
            class="lista-dropdown"
            role="listbox"
            aria-label="Procedimentos TUSS/CBHPM"
        >
            <li
                v-for="(proc, idx) in resultados"
                :id="`tuss-item-${idx}`"
                :key="proc.id"
                class="lista-item"
                :class="{ 'lista-item--focado': idx === itemFocado }"
                role="option"
                :aria-selected="idx === itemFocado"
                @mousedown.prevent="selecionar(proc)"
            >
                <span class="item-codigo">{{ proc.codigo }}</span>
                <span class="item-nome">{{ proc.nome }}</span>
                <span class="item-origem">{{ ORIGEM_LABEL[proc.origem] ?? proc.origem }}</span>
            </li>
        </ul>
    </div>
</template>

<style scoped>
.autocomplete-wrapper {
    position: relative;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.input-row {
    position: relative;
    display: flex;
    align-items: center;
}

.tuss-input {
    width: 100%;
    padding: 0.5rem 2.25rem 0.5rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    font-family: inherit;
    font-size: 0.875em;
    background: hsl(var(--card));
    color: hsl(var(--foreground));
    transition: border-color 0.15s, box-shadow 0.15s;
    box-sizing: border-box;
}
.tuss-input:focus {
    outline: none;
    border-color: hsl(var(--primary));
    box-shadow: 0 0 0 2px hsl(var(--primary) / 0.2);
}
.tuss-input:disabled {
    background: hsl(var(--muted));
    color: hsl(var(--muted-foreground));
    cursor: not-allowed;
}

.spinner-inline {
    position: absolute;
    right: 0.65rem;
    color: hsl(var(--muted-foreground));
    font-size: 0.8em;
    pointer-events: none;
}

.erro-busca {
    font-size: 0.78em;
    color: hsl(var(--destructive));
    margin: 0;
}

.lista-dropdown {
    position: absolute;
    top: calc(100% + 4px);
    left: 0;
    right: 0;
    background: hsl(var(--popover));
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    box-shadow: var(--shadow-md);
    z-index: 200;
    max-height: 280px;
    overflow-y: auto;
    margin: 0;
    padding: 0.25rem 0;
    list-style: none;
}

.lista-item {
    display: flex;
    align-items: baseline;
    gap: 0.6rem;
    padding: 0.55rem 0.85rem;
    cursor: pointer;
    font-size: 0.875em;
    transition: background 0.1s;
}
.lista-item:hover,
.lista-item--focado {
    background: hsl(var(--accent));
}

.item-codigo {
    font-family: "Courier New", monospace;
    font-size: 0.85em;
    font-weight: 700;
    color: hsl(var(--primary));
    flex-shrink: 0;
    min-width: 90px;
}
.item-nome {
    flex: 1;
    color: hsl(var(--foreground));
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.item-origem {
    font-size: 0.72em;
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    padding: 0.1rem 0.35rem;
    border-radius: var(--radius-sm);
    background: hsl(var(--muted));
    flex-shrink: 0;
}
</style>
