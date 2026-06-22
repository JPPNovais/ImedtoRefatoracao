<!--
    SeletorModeloProntuario — seletor rico de modelo de prontuário.

    Reutilizado em dois contextos:
      1. Toolbar da ConsultaAtualTab (modelo já selecionado → gatilho mostra nome atual).
      2. Empty-state de escolha (modeloId === null → gatilho mostra placeholder).

    Props:
      modeloId  — id do modelo atualmente selecionado, ou null (sem seleção).
      modelos   — lista completa de ModeloProntuario disponíveis.

    Emite:
      update:modeloId — id escolhido (sempre number — nunca null).
-->
<script setup lang="ts">
import { computed } from "vue"
import { AppPopover } from "@/components/ui"
import type { ModeloProntuario } from "@/services/prontuarioService"

const props = defineProps<{
    modeloId: number | null
    modelos: ModeloProntuario[]
}>()

const emit = defineEmits<{
    "update:modeloId": [id: number]
}>()

const modeloAtual = computed(
    () => props.modelos.find(m => m.id === props.modeloId) ?? null,
)

const cabecalhoMenu = computed(() =>
    props.modeloId === null
        ? `Selecionar modelo · ${props.modelos.length} disponíveis`
        : `Trocar modelo · ${props.modelos.length} disponíveis`,
)

function selecionarModelo(id: number, fechar: () => void) {
    if (id !== props.modeloId) emit("update:modeloId", id)
    fechar()
}
</script>

<template>
    <AppPopover posicao="bottom-start">
        <template #gatilho="{ toggle, aberto }">
            <button
                type="button"
                class="tpl-current"
                :class="{ aberto }"
                :title="modeloAtual?.descricao ?? ''"
                @click="toggle"
            >
                <i class="fa-solid fa-stethoscope"></i>
                <div>
                    <span class="tpl-lbl">Tipo de prontuário</span>
                    <strong v-if="modeloAtual">{{ modeloAtual.nome }}</strong>
                    <strong v-else class="tpl-placeholder">Selecione um modelo</strong>
                </div>
                <i class="fa-solid fa-chevron-down tpl-caret"></i>
            </button>
        </template>
        <template #conteudo="{ fechar }">
            <div class="tpl-menu">
                <p class="tpl-menu-head">{{ cabecalhoMenu }}</p>
                <button
                    v-for="m in modelos"
                    :key="m.id"
                    type="button"
                    class="tpl-menu-item"
                    :class="{ current: m.id === modeloId }"
                    @click="selecionarModelo(m.id, fechar)"
                >
                    <i class="fa-solid fa-stethoscope"></i>
                    <div>
                        <b>{{ m.nome }}</b>
                        <span v-if="m.descricao">{{ m.descricao }}</span>
                        <span v-else-if="m.ehPadraoSistema">Modelo do sistema</span>
                    </div>
                    <i v-if="m.id === modeloId" class="fa-solid fa-check tpl-menu-check"></i>
                </button>
            </div>
        </template>
    </AppPopover>
</template>

<style scoped>
/* ── Gatilho ── */
.tpl-current {
    display: inline-flex; align-items: center; gap: 12px;
    padding: 10px 16px;
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.14);
    border-radius: var(--radius-md);
    font: inherit;
    cursor: pointer;
    text-align: left;
    transition: border-color 150ms, box-shadow 150ms;
}
.tpl-current:hover { border-color: hsl(var(--primary) / 0.6); }
.tpl-current.aberto {
    border-color: hsl(var(--primary));
    box-shadow: 0 0 0 3px hsl(var(--primary) / 0.12);
}

/* Ícone de stethoscope no gatilho */
.tpl-current > i:first-child {
    width: 32px; height: 32px; border-radius: 8px;
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
}

/* Bloco de texto (label + nome/placeholder) */
.tpl-current > div { text-align: left; display: flex; flex-direction: column; }

.tpl-lbl {
    font-size: var(--text-2xs);
    color: hsl(var(--secondary) / 0.5);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.tpl-current strong {
    font-size: var(--text-base);
    color: hsl(var(--primary-dark));
}

.tpl-placeholder {
    color: hsl(var(--secondary) / 0.55) !important;
    font-style: italic;
}

/* Caret */
.tpl-caret {
    margin-left: 6px; font-size: var(--text-2xs);
    color: hsl(var(--secondary) / 0.5);
    transition: transform 150ms;
}
.tpl-current.aberto .tpl-caret { transform: rotate(180deg); }

/* ── Menu (conteúdo do popover) ── */
/* max-height garante que em viewports curtas o menu não estoure para cima.
 * 60vh cobre listas longas de modelos sem flip no sentido inverso. */
.tpl-menu { display: flex; flex-direction: column; gap: 4px; padding: 8px; max-height: 60vh; overflow-y: auto; }

.tpl-menu-head {
    font-size: var(--text-2xs); font-weight: var(--font-weight-bold);
    text-transform: uppercase; letter-spacing: 0.06em;
    color: hsl(var(--secondary) / 0.5);
    margin: 4px 8px 6px;
}

.tpl-menu-item {
    display: flex; align-items: center; gap: 10px;
    padding: 8px 10px;
    background: white;
    border: 1px solid transparent;
    border-radius: var(--radius-md);
    cursor: pointer;
    text-align: left; font: inherit;
    transition: all 150ms;
}
.tpl-menu-item:hover { background: hsl(var(--primary) / 0.05); }
.tpl-menu-item.current {
    background: hsl(var(--primary) / 0.08);
    border-color: hsl(var(--primary) / 0.25);
}

/* Ícone de stethoscope em cada item */
.tpl-menu-item > i:first-child {
    width: 28px; height: 28px; border-radius: 6px;
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
    flex-shrink: 0; font-size: var(--text-xs);
}

.tpl-menu-item > div { flex: 1; min-width: 0; }

.tpl-menu-item b {
    display: block; font-size: var(--text-xs);
    color: hsl(var(--primary-dark));
    font-weight: var(--font-weight-semibold);
}

.tpl-menu-item span {
    font-size: var(--text-2xs);
    color: hsl(var(--secondary) / 0.6);
    line-height: 1.3;
    overflow: hidden; text-overflow: ellipsis;
    display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical;
}

.tpl-menu-check { color: hsl(var(--primary)); font-size: var(--text-xs); flex-shrink: 0; }
</style>
