<script setup lang="ts">
/**
 * Select com botão "+ Novo" inline à direita.
 *
 * Caso de uso: formulários que vinculam o registro principal a entidades
 * mestre (categoria, fabricante, fornecedor, local). Em vez de forçar o
 * usuário a sair do formulário para cadastrar uma opção nova, ele cria
 * inline via modal e a opção já fica pré-selecionada.
 *
 * O componente apenas emite `criar` — quem abre o modal (e qual modal) é
 * responsabilidade da view, mantendo o componente desacoplado dos cadastros
 * específicos. A view também é responsável por hidratar `opcoes` após
 * receber a nova opção (append local + refetch).
 *
 * Opções são `{ id: number, nome: string }` — mesmo shape de `CadastroOpcao`
 * dos services de estoque/cadastros (reuso intencional).
 */
interface OpcaoLeve {
    id: number
    nome: string
}

const props = withDefaults(defineProps<{
    modelValue?:   number | null
    opcoes:        ReadonlyArray<OpcaoLeve>
    placeholder?:  string
    /** Texto curto no botão (ex: "Nova categoria"). Default: "Novo". */
    rotuloCriar?:  string
    /** Esconde o botão "+ Novo" (default: visível). */
    permiteCriar?: boolean
    desabilitado?: boolean
    /** Quando true, primeira opção do dropdown é "Selecione" desabilitada. */
    obrigatorio?:  boolean
}>(), {
    rotuloCriar: "Novo",
    permiteCriar: true,
    desabilitado: false,
    obrigatorio: false,
})

const emit = defineEmits<{
    "update:modelValue": [number]
    criar: []
}>()

function onChange(ev: Event) {
    const valor = (ev.target as HTMLSelectElement).value
    emit("update:modelValue", Number(valor))
}
</script>

<template>
    <div class="select-com-criacao">
        <select
            :value="modelValue ?? 0"
            :disabled="desabilitado"
            class="select"
            @change="onChange"
        >
            <option v-if="obrigatorio" :value="0" disabled>{{ placeholder ?? "Selecione" }}</option>
            <option v-else :value="0">{{ placeholder ?? "— Nenhum —" }}</option>
            <option v-for="o in opcoes" :key="o.id" :value="o.id">{{ o.nome }}</option>
        </select>

        <button
            v-if="permiteCriar"
            type="button"
            class="btn-novo"
            :disabled="desabilitado"
            :aria-label="rotuloCriar"
            :title="rotuloCriar"
            @click="emit('criar')"
        >
            <i class="fa-solid fa-plus"></i>
            <span class="rotulo">Novo</span>
        </button>
    </div>
</template>

<style scoped>
.select-com-criacao {
    display: flex;
    gap: 6px;
    align-items: stretch;
    width: 100%;
}
.select {
    flex: 1;
    min-width: 0;
    display: flex;
    height: 36px;
    width: 100%;
    border-radius: var(--radius-sm, 6px);
    border: 1px solid hsl(var(--border, 240 6% 90%));
    background: var(--bg-card, #fff);
    padding: 0 12px;
    font-size: 13px;
    color: hsl(var(--primary-dark, 220 50% 20%));
    transition: border-color 120ms;
}
.select:focus-visible {
    outline: none;
    border-color: hsl(var(--primary, 218 70% 50%));
    box-shadow: 0 0 0 2px hsl(var(--primary, 218 70% 50%) / 0.15);
}
.select:disabled { opacity: 0.5; cursor: not-allowed; }

.btn-novo {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    height: 36px;
    padding: 0 10px;
    border-radius: var(--radius-sm, 6px);
    border: 1px dashed hsl(var(--primary, 218 70% 50%) / 0.45);
    background: hsl(var(--primary, 218 70% 50%) / 0.05);
    color: hsl(var(--primary, 218 70% 50%));
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    white-space: nowrap;
    transition: background 120ms, border-color 120ms;
}
.btn-novo:hover:not(:disabled) {
    background: hsl(var(--primary, 218 70% 50%) / 0.12);
    border-style: solid;
}
.btn-novo:disabled { opacity: 0.4; cursor: not-allowed; }
.btn-novo i { font-size: 11px; }
.rotulo { line-height: 1; }
</style>
