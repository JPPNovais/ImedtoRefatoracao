<script setup lang="ts">
/**
 * AppAutocompleteCriavel — typeahead de texto com sugestões do pool de variáveis.
 *
 * Diferença de AppSelectComCriacao (select por id):
 *   - v-model é string (o `nome` do item), não id.
 *   - Valores inéditos são aceitos livremente — a criação no pool ocorre no backend
 *     ao salvar a evolução (PoolExtratorEvolucao), não aqui.
 *   - Filtro é client-side sobre lista já carregada — sem request por tecla.
 *
 * CA10: estado vazio → mensagem "Nenhuma opção cadastrada — digite para criar uma nova".
 * CA11: se opcoes não puder ser carregada, o pai passa opcoes=[] e erro=true → input puro.
 */
import { ref, computed, watch, onMounted, onBeforeUnmount } from "vue"

const props = withDefaults(defineProps<{
    modelValue: string
    opcoes: string[]
    placeholder?: string
    disabled?: boolean
    /** Quando true, o carregamento ainda está em andamento. */
    carregando?: boolean
    /** Quando true, houve erro no carregamento — degrada para input puro (CA11). */
    erro?: boolean
}>(), {
    placeholder: "",
    disabled: false,
    carregando: false,
    erro: false,
})

const emit = defineEmits<{
    "update:modelValue": [string]
}>()

const aberto = ref(false)
const inputEl = ref<HTMLInputElement | null>(null)
const dropdownEl = ref<HTMLElement | null>(null)

// Normalização idêntica à do backend (sem acento, lower, trim)
function normalizar(s: string): string {
    return s
        .trim()
        .toLowerCase()
        .normalize("NFD")
        .replace(/[̀-ͯ]/g, "")
}

const sugestoesFiltradas = computed(() => {
    const termo = normalizar(props.modelValue)
    if (!termo) return props.opcoes
    return props.opcoes.filter(o => normalizar(o).includes(termo))
})

function selecionar(valor: string) {
    emit("update:modelValue", valor)
    aberto.value = false
}

function aoFocar() {
    if (!props.disabled && !props.erro) aberto.value = true
}

function aoDigitar(e: Event) {
    const v = (e.target as HTMLInputElement).value
    emit("update:modelValue", v)
    aberto.value = true
}

// Fecha o dropdown ao clicar fora
function aoClicarFora(e: MouseEvent) {
    const alvo = e.target as Node
    if (
        inputEl.value && !inputEl.value.contains(alvo) &&
        dropdownEl.value && !dropdownEl.value.contains(alvo)
    ) {
        aberto.value = false
    }
}

onMounted(() => document.addEventListener("mousedown", aoClicarFora))
onBeforeUnmount(() => document.removeEventListener("mousedown", aoClicarFora))

// Fecha ao trocar de campo via Tab
function aoBlur() {
    // Pequeno delay para permitir click em opção do dropdown antes de fechar
    setTimeout(() => { aberto.value = false }, 150)
}

// Seta ↑↓ no dropdown
const indiceSelecionado = ref(-1)
watch(sugestoesFiltradas, () => { indiceSelecionado.value = -1 })

function aoKeydown(e: KeyboardEvent) {
    if (!aberto.value) {
        if (e.key === "ArrowDown" || e.key === "ArrowUp") {
            aberto.value = true
            e.preventDefault()
        }
        return
    }
    if (e.key === "ArrowDown") {
        indiceSelecionado.value = Math.min(indiceSelecionado.value + 1, sugestoesFiltradas.value.length - 1)
        e.preventDefault()
    } else if (e.key === "ArrowUp") {
        indiceSelecionado.value = Math.max(indiceSelecionado.value - 1, -1)
        e.preventDefault()
    } else if (e.key === "Enter") {
        if (indiceSelecionado.value >= 0 && sugestoesFiltradas.value[indiceSelecionado.value]) {
            selecionar(sugestoesFiltradas.value[indiceSelecionado.value])
        }
        aberto.value = false
        e.preventDefault()
    } else if (e.key === "Escape") {
        aberto.value = false
    }
}
</script>

<template>
    <div class="autocomplete">
        <input
            ref="inputEl"
            type="text"
            class="autocomplete-input"
            :class="{ 'autocomplete-input--erro': erro }"
            :value="modelValue"
            :placeholder="carregando ? 'Carregando...' : placeholder"
            :disabled="disabled || carregando"
            :aria-expanded="aberto"
            :aria-autocomplete="erro ? 'none' : 'list'"
            role="combobox"
            autocomplete="off"
            @input="aoDigitar"
            @focus="aoFocar"
            @blur="aoBlur"
            @keydown="aoKeydown"
        />

        <div
            v-if="aberto && !erro && !carregando"
            ref="dropdownEl"
            class="dropdown"
            role="listbox"
        >
            <div v-if="sugestoesFiltradas.length === 0" class="dropdown-vazio">
                Nenhuma opção cadastrada — digite para criar uma nova
            </div>
            <button
                v-for="(opcao, idx) in sugestoesFiltradas"
                :key="opcao"
                type="button"
                role="option"
                class="dropdown-item"
                :class="{ 'dropdown-item--selecionado': idx === indiceSelecionado }"
                :aria-selected="idx === indiceSelecionado"
                @mousedown.prevent="selecionar(opcao)"
            >
                {{ opcao }}
            </button>
        </div>
    </div>
</template>

<style scoped>
.autocomplete {
    position: relative;
    width: 100%;
}

.autocomplete-input {
    display: flex;
    height: 36px;
    width: 100%;
    border-radius: var(--radius-sm, 6px);
    border: 1px solid hsl(var(--border, 240 6% 90%));
    background: var(--bg-card, #fff);
    padding: 0 12px;
    font-size: 13px;
    font-family: inherit;
    color: hsl(var(--foreground, 220 50% 10%));
    transition: border-color 120ms;
    box-sizing: border-box;
}

.autocomplete-input:focus-visible {
    outline: none;
    border-color: hsl(var(--primary, 218 70% 50%));
    box-shadow: 0 0 0 2px hsl(var(--primary, 218 70% 50%) / 0.15);
}

.autocomplete-input:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

/* CA11: degradação visual discreta quando há erro */
.autocomplete-input--erro {
    border-color: hsl(var(--border, 240 6% 90%));
}

.dropdown {
    position: absolute;
    top: calc(100% + 4px);
    left: 0;
    right: 0;
    z-index: 50;
    background: var(--bg-card, #fff);
    border: 1px solid hsl(var(--border, 240 6% 90%));
    border-radius: var(--radius-sm, 6px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
    max-height: 220px;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
}

.dropdown-vazio {
    padding: 0.6rem 0.75rem;
    font-size: 0.8em;
    color: hsl(var(--muted-foreground, 240 4% 46%));
    font-style: italic;
}

.dropdown-item {
    display: block;
    width: 100%;
    padding: 0.5rem 0.75rem;
    font-size: 0.875rem;
    font-family: inherit;
    text-align: left;
    background: none;
    border: none;
    cursor: pointer;
    color: hsl(var(--foreground, 220 50% 10%));
    transition: background 100ms;
    border-radius: 0;
}

.dropdown-item:hover,
.dropdown-item--selecionado {
    background: hsl(var(--primary, 218 70% 50%) / 0.08);
    color: hsl(var(--primary, 218 70% 50%));
}
</style>
