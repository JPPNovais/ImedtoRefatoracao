<script setup lang="ts">
/**
 * AppSelectCategoriaInline — seletor de categoria financeira (string) com
 * opção de adicionar nova categoria inline, sem fechar o modal pai.
 *
 * Usado no modal de lançamento financeiro (VisaoGeralTab.vue).
 *
 * - Valor emitido é o NOME da categoria (string), não o ID.
 * - Quando o nome selecionado não está na lista (lançamento antigo), exibe
 *   como opção transitória para não perder o dado.
 * - Ao clicar "+ Adicionar nova categoria", exibe mini-form inline com campo
 *   de nome + botões Confirmar/Cancelar.
 * - Ao confirmar, emite "criar" com o nome digitado; a view faz o POST,
 *   injeta o resultado via :opcoes e passa o novo nome como modelValue.
 * - Erro de criação (422) recebido via prop :erroCriar e exibido inline.
 */

import { ref, computed, watch } from "vue"

const props = defineProps<{
    modelValue: string
    opcoes: ReadonlyArray<string>
    placeholder?: string
    desabilitado?: boolean
    salvandoCriar?: boolean
    erroCriar?: string | null
}>()

const emit = defineEmits<{
    "update:modelValue": [nome: string]
    "criar": [nome: string]
}>()

const modoAdicionar = ref(false)
const novoNome = ref("")

const opcoesComTransitoria = computed<string[]>(() => {
    const lista = [...props.opcoes]
    if (props.modelValue && !lista.includes(props.modelValue)) {
        lista.unshift(props.modelValue)
    }
    return lista
})

function onSelect(ev: Event) {
    const val = (ev.target as HTMLSelectElement).value
    emit("update:modelValue", val)
}

function abrirAdicionar() {
    novoNome.value = ""
    modoAdicionar.value = true
}

function cancelarAdicionar() {
    modoAdicionar.value = false
    novoNome.value = ""
}

function confirmarNovo() {
    const nome = novoNome.value.trim()
    if (!nome) return
    emit("criar", nome)
}

// Fecha o mini-form quando o pai conclui a criação com sucesso:
// salvandoCriar transita de true→false sem erroCriar → criação ok.
watch(
    () => props.salvandoCriar,
    (atual, anterior) => {
        if (anterior && !atual && !props.erroCriar && modoAdicionar.value) {
            modoAdicionar.value = false
            novoNome.value = ""
        }
    },
)
</script>

<template>
    <div class="sca-raiz">
        <!-- Estado normal: select -->
        <div v-if="!modoAdicionar" class="sca-estado-select">
            <select
                :value="modelValue"
                :disabled="desabilitado"
                class="sca-select"
                @change="onSelect"
            >
                <option value="" disabled>{{ placeholder ?? "Selecione…" }}</option>
                <option v-for="nome in opcoesComTransitoria" :key="nome" :value="nome">
                    {{ nome }}
                </option>
            </select>
            <button
                type="button"
                class="sca-btn-adicionar"
                :disabled="desabilitado"
                @click="abrirAdicionar"
            >
                <i class="fa-solid fa-plus" aria-hidden="true"></i>
                Adicionar nova categoria
            </button>
        </div>

        <!-- Estado inline de criação -->
        <div v-else class="sca-novo-form">
            <input
                v-model="novoNome"
                type="text"
                class="sca-novo-input"
                placeholder="Nome da nova categoria"
                maxlength="80"
                :disabled="salvandoCriar"
                autofocus
                @keydown.enter.prevent="confirmarNovo"
                @keydown.esc.prevent="cancelarAdicionar"
            />
            <div class="sca-novo-acoes">
                <button
                    type="button"
                    class="sca-btn-confirmar"
                    :disabled="!novoNome.trim() || salvandoCriar"
                    @click="confirmarNovo"
                >
                    <i v-if="salvandoCriar" class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
                    <i v-else class="fa-solid fa-check" aria-hidden="true"></i>
                    Confirmar
                </button>
                <button
                    type="button"
                    class="sca-btn-cancelar"
                    :disabled="salvandoCriar"
                    @click="cancelarAdicionar"
                >
                    Cancelar
                </button>
            </div>
            <p v-if="erroCriar" class="sca-erro-inline" role="alert">{{ erroCriar }}</p>
        </div>
    </div>
</template>

<style scoped>
.sca-raiz {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
    width: 100%;
}

.sca-select {
    width: 100%;
    height: 36px;
    padding: 0 12px;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius-sm, 6px);
    background: hsl(var(--background));
    color: hsl(var(--foreground));
    font-size: var(--text-sm);
    transition: border-color 120ms;
    cursor: pointer;
}

.sca-select:focus-visible {
    outline: none;
    border-color: hsl(var(--primary));
    box-shadow: 0 0 0 2px hsl(var(--primary) / 0.15);
}

.sca-select:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.sca-btn-adicionar {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
    padding: 0.25rem 0;
    border: none;
    background: transparent;
    color: hsl(var(--primary));
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    cursor: pointer;
    width: fit-content;
    transition: opacity 120ms;
}

.sca-btn-adicionar:hover:not(:disabled) {
    opacity: 0.8;
}

.sca-btn-adicionar:disabled {
    opacity: 0.4;
    cursor: not-allowed;
}

.sca-novo-form {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding: 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius-sm, 6px);
    background: hsl(var(--muted) / 0.3);
}

.sca-novo-input {
    width: 100%;
    height: 34px;
    padding: 0 10px;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius-sm, 6px);
    background: hsl(var(--background));
    color: hsl(var(--foreground));
    font-size: var(--text-sm);
}

.sca-novo-input:focus-visible {
    outline: none;
    border-color: hsl(var(--primary));
    box-shadow: 0 0 0 2px hsl(var(--primary) / 0.15);
}

.sca-novo-input:disabled {
    opacity: 0.5;
}

.sca-novo-acoes {
    display: flex;
    gap: 0.5rem;
}

.sca-btn-confirmar {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    height: 30px;
    padding: 0 12px;
    border: none;
    border-radius: var(--radius-sm, 6px);
    background: hsl(var(--primary));
    color: hsl(var(--primary-foreground));
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    cursor: pointer;
    transition: opacity 120ms;
}

.sca-btn-confirmar:hover:not(:disabled) {
    opacity: 0.88;
}

.sca-btn-confirmar:disabled {
    opacity: 0.4;
    cursor: not-allowed;
}

.sca-btn-cancelar {
    height: 30px;
    padding: 0 12px;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius-sm, 6px);
    background: transparent;
    color: hsl(var(--muted-foreground));
    font-size: var(--text-xs);
    cursor: pointer;
    transition: background 120ms;
}

.sca-btn-cancelar:hover:not(:disabled) {
    background: hsl(var(--muted) / 0.5);
}

.sca-btn-cancelar:disabled {
    opacity: 0.4;
    cursor: not-allowed;
}

.sca-erro-inline {
    color: hsl(var(--destructive));
    font-size: var(--text-xs);
    margin: 0;
}
</style>
