<script setup lang="ts">
/**
 * AppAvatarSelect — dropdown custom de profissional com avatar + nome + especialidade.
 *
 * Substitui o `<select>` nativo nos seletores de profissional da Agenda
 * (novo/editar), Prontuário e Orçamento. O `<select>` nativo não suporta
 * imagens nas `<option>`s — esta é a alternativa mínima viável com
 * acessibilidade básica (teclado, foco, aria).
 *
 * Mantém-se simples: sem virtualização (listas de equipe são pequenas:
 * dezenas, não centenas), sem busca interna (filtros vêm de fora).
 */
import { computed, onBeforeUnmount, ref, watch } from "vue"
import AppAvatar from "./AppAvatar.vue"

export interface AvatarOpcao {
    usuarioId: string
    nomeCompleto: string
    especialidade?: string | null
    fotoUrl?: string | null
}

const props = withDefaults(defineProps<{
    modelValue: string
    opcoes: ReadonlyArray<AvatarOpcao>
    placeholder?: string
    disabled?: boolean
    /** Quando true, primeira opção do dropdown é a "limpar seleção" (valor ""). */
    permiteLimpar?: boolean
    rotuloLimpar?: string
}>(), {
    placeholder: "Selecione...",
    disabled: false,
    permiteLimpar: false,
    rotuloLimpar: "Todos",
})

const emit = defineEmits<{
    "update:modelValue": [string]
}>()

const aberto = ref(false)
const btn = ref<HTMLButtonElement | null>(null)
const root = ref<HTMLDivElement | null>(null)

const selecionado = computed<AvatarOpcao | null>(() =>
    props.opcoes.find(o => o.usuarioId === props.modelValue) ?? null,
)

function abrir() {
    if (props.disabled) return
    aberto.value = true
}

function fechar() {
    aberto.value = false
}

function escolher(opt: AvatarOpcao | null) {
    emit("update:modelValue", opt?.usuarioId ?? "")
    aberto.value = false
    // Devolve o foco ao botão (a11y).
    btn.value?.focus()
}

function onClickFora(e: MouseEvent) {
    if (!aberto.value) return
    if (root.value && !root.value.contains(e.target as Node)) {
        aberto.value = false
    }
}

function onKey(e: KeyboardEvent) {
    if (!aberto.value) return
    if (e.key === "Escape") {
        e.preventDefault()
        aberto.value = false
        btn.value?.focus()
    }
}

watch(aberto, (v) => {
    if (v) {
        document.addEventListener("click", onClickFora)
        document.addEventListener("keydown", onKey)
    } else {
        document.removeEventListener("click", onClickFora)
        document.removeEventListener("keydown", onKey)
    }
})

onBeforeUnmount(() => {
    document.removeEventListener("click", onClickFora)
    document.removeEventListener("keydown", onKey)
})
</script>

<template>
    <div ref="root" class="av-select" :class="{ disabled }">
        <button
            ref="btn"
            type="button"
            class="av-trigger"
            :disabled="disabled"
            :aria-expanded="aberto"
            aria-haspopup="listbox"
            @click="aberto ? fechar() : abrir()"
        >
            <template v-if="selecionado">
                <AppAvatar
                    :nome="selecionado.nomeCompleto"
                    :foto-url="selecionado.fotoUrl"
                    tamanho="sm"
                />
                <span class="av-label">
                    <b>{{ selecionado.nomeCompleto }}</b>
                    <span v-if="selecionado.especialidade" class="av-spec">{{ selecionado.especialidade }}</span>
                </span>
            </template>
            <template v-else>
                <span class="av-placeholder">{{ placeholder }}</span>
            </template>
            <i class="fa-solid fa-chevron-down chev" aria-hidden="true" />
        </button>

        <ul v-if="aberto" class="av-list" role="listbox">
            <li
                v-if="permiteLimpar"
                role="option"
                :aria-selected="modelValue === ''"
                class="av-item av-item--limpar"
                tabindex="0"
                @click="escolher(null)"
                @keydown.enter="escolher(null)"
                @keydown.space.prevent="escolher(null)"
            >
                <span class="av-limpar-icon"><i class="fa-solid fa-xmark" aria-hidden="true" /></span>
                <span class="av-label"><b>{{ rotuloLimpar }}</b></span>
            </li>
            <li
                v-for="opt in opcoes"
                :key="opt.usuarioId"
                role="option"
                :aria-selected="opt.usuarioId === modelValue"
                class="av-item"
                :class="{ ativo: opt.usuarioId === modelValue }"
                tabindex="0"
                @click="escolher(opt)"
                @keydown.enter="escolher(opt)"
                @keydown.space.prevent="escolher(opt)"
            >
                <AppAvatar :nome="opt.nomeCompleto" :foto-url="opt.fotoUrl" tamanho="sm" />
                <span class="av-label">
                    <b>{{ opt.nomeCompleto }}</b>
                    <span v-if="opt.especialidade" class="av-spec">{{ opt.especialidade }}</span>
                </span>
            </li>
            <li v-if="opcoes.length === 0 && !permiteLimpar" class="av-empty">
                Nenhum profissional disponível.
            </li>
        </ul>
    </div>
</template>

<style scoped>
.av-select {
    position: relative;
    width: 100%;
    font-family: inherit;
}
.av-select.disabled .av-trigger { opacity: 0.6; cursor: not-allowed; }

.av-trigger {
    display: flex;
    align-items: center;
    gap: 10px;
    width: 100%;
    min-height: 38px;
    padding: 6px 12px;
    border-radius: var(--radius-sm, 6px);
    border: 1px solid hsl(var(--border, 240 6% 90%));
    background: var(--bg-card, #fff);
    color: hsl(var(--primary-dark, 220 50% 20%));
    font-size: 13px;
    cursor: pointer;
    text-align: left;
    transition: border-color 120ms;
}
.av-trigger:hover:not(:disabled),
.av-trigger:focus-visible {
    border-color: hsl(var(--primary, 218 70% 50%));
    outline: none;
}

.av-label { flex: 1; min-width: 0; display: flex; flex-direction: column; line-height: 1.2; }
.av-label b { font-weight: 600; font-size: 13px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.av-spec { font-size: 11px; color: hsl(var(--secondary, 220 8% 45%)); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.av-placeholder { color: hsl(var(--secondary, 220 8% 55%)); font-size: 13px; }
.chev { font-size: 10px; color: hsl(var(--secondary, 220 8% 50%)); margin-left: auto; }

.av-list {
    position: absolute;
    top: calc(100% + 4px);
    left: 0;
    right: 0;
    max-height: 280px;
    overflow-y: auto;
    z-index: 20;
    margin: 0;
    padding: 4px;
    list-style: none;
    background: white;
    border-radius: var(--radius, 8px);
    border: 1px solid hsl(var(--border, 240 6% 90%));
    box-shadow: 0 10px 24px -6px rgb(0 0 0 / 0.12), 0 4px 8px -2px rgb(0 0 0 / 0.05);
}
.av-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 8px 10px;
    border-radius: 6px;
    cursor: pointer;
    transition: background 120ms;
}
.av-item:hover,
.av-item:focus-visible {
    background: hsl(var(--primary, 218 70% 50%) / 0.08);
    outline: none;
}
.av-item.ativo {
    background: hsl(var(--primary, 218 70% 50%) / 0.12);
}
.av-item--limpar { color: hsl(var(--secondary, 220 8% 45%)); }
.av-limpar-icon {
    width: 24px; height: 24px;
    display: inline-flex; align-items: center; justify-content: center;
    border-radius: 50%;
    background: hsl(var(--secondary, 220 8% 92%));
    color: hsl(var(--secondary, 220 8% 45%));
    font-size: 11px;
    flex-shrink: 0;
}
.av-empty {
    padding: 14px 10px;
    text-align: center;
    color: hsl(var(--secondary, 220 8% 55%));
    font-size: 12px;
    font-style: italic;
}
</style>
