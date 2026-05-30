<script setup lang="ts">
/**
 * Builder visual de modelos de prontuário — compartilhado entre tenant e admin.
 * Catálogo, tipos e helpers exportados em modeloProntuarioBuilder.ts (fonte única de verdade).
 */
import { computed, ref, watch } from "vue"
import AppField from "./AppField.vue"
import AppInput from "./AppInput.vue"
import AppTextarea from "./AppTextarea.vue"
import type { SecaoModelo } from "@/services/prontuarioService"
import {
    SECOES_MODELO_PRONTUARIO,
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

function moverSecao(key: string, direcao: "cima" | "baixo") {
    const idx = ordem.value.indexOf(key)
    if (idx === -1) return
    const novoIdx = direcao === "cima" ? idx - 1 : idx + 1
    if (novoIdx < 0 || novoIdx >= ordem.value.length) return
    const arr = [...ordem.value]
    const [item] = arr.splice(idx, 1)
    arr.splice(novoIdx, 0, item)
    ordem.value = arr
}

const secoesAtivas = computed((): SecaoModelo[] =>
    ordem.value
        .filter(k => secoes.value[k])
        .map((key, idx) => {
            const def = SECOES_MODELO_PRONTUARIO.find(s => s.key === key)!
            return { chave: key, titulo: def.label, tipo: def.tipo, ordem: idx }
        })
)

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

        <!-- Grid de checkboxes das seções -->
        <div class="mpb-secoes-bloco">
            <label class="mpb-secoes-label">Seções incluídas no modelo</label>
            <div class="mpb-secoes-grid">
                <label
                    v-for="s in SECOES_MODELO_PRONTUARIO"
                    :key="s.key"
                    class="mpb-secao-item"
                    :title="s.info"
                >
                    <input
                        v-model="secoes[s.key]"
                        type="checkbox"
                        class="mpb-secao-check"
                        :disabled="disabled"
                    />
                    <span>{{ s.label }}</span>
                </label>
            </div>
        </div>

        <!-- Ordem das seções ativas -->
        <div v-if="secoesAtivas.length" class="mpb-ordem-bloco">
            <label class="mpb-secoes-label">Ordem das seções</label>
            <ul class="mpb-ordem-lista">
                <li
                    v-for="secao in secoesAtivas"
                    :key="secao.chave"
                    class="mpb-ordem-item"
                >
                    <span>{{ SECOES_MODELO_PRONTUARIO.find(s => s.key === secao.chave)?.label ?? secao.chave }}</span>
                    <div class="mpb-ordem-btns">
                        <button
                            type="button"
                            class="mpb-ordem-btn"
                            :disabled="disabled"
                            @click="moverSecao(secao.chave, 'cima')"
                        >↑</button>
                        <button
                            type="button"
                            class="mpb-ordem-btn"
                            :disabled="disabled"
                            @click="moverSecao(secao.chave, 'baixo')"
                        >↓</button>
                    </div>
                </li>
            </ul>
        </div>
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
    background: color-mix(in srgb, var(--info) 10%, transparent);
    border: 1px solid color-mix(in srgb, var(--info) 30%, transparent);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.82em;
    color: var(--text);
}

.mpb-aviso-icone {
    color: var(--info);
    flex-shrink: 0;
}

/* ── Seções ── */
.mpb-secoes-bloco {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.mpb-secoes-label {
    font-size: 0.78em;
    font-weight: 700;
    color: var(--text-muted);
}

.mpb-secoes-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.35rem 1rem;
}

@media (max-width: 500px) {
    .mpb-secoes-grid { grid-template-columns: 1fr; }
}

.mpb-secao-item {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.8em;
    cursor: pointer;
    color: var(--text);
}

.mpb-secao-item:has(.mpb-secao-check:disabled) {
    cursor: not-allowed;
    opacity: 0.6;
}

.mpb-secao-check {
    accent-color: var(--primary);
    width: 14px;
    height: 14px;
    flex-shrink: 0;
    cursor: pointer;
}

/* ── Ordem ── */
.mpb-ordem-bloco {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.mpb-ordem-lista {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
}

.mpb-ordem-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
    padding: 0.3rem 0.6rem;
    border: 1px solid var(--border);
    border-radius: calc(var(--radius) - 4px);
    background: var(--bg);
    font-size: 0.8em;
    color: var(--text);
}

.mpb-ordem-btns {
    display: flex;
    gap: 0.2rem;
}

.mpb-ordem-btn {
    border: none;
    background: none;
    cursor: pointer;
    font-size: 0.85em;
    color: var(--text-muted);
    padding: 0.1rem 0.3rem;
    border-radius: 3px;
    transition: color 0.1s, background 0.1s;
}

.mpb-ordem-btn:hover:not(:disabled) {
    color: var(--primary);
    background: color-mix(in srgb, var(--primary) 8%, transparent);
}

.mpb-ordem-btn:disabled {
    cursor: not-allowed;
    opacity: 0.5;
}
</style>
