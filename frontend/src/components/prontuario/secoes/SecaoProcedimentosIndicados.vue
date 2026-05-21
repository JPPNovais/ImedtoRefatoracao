<!-- Lista simples de procedimentos indicados. -->
<script setup lang="ts">
import { computed } from "vue"
import { AppButton, AppInput, AppTextarea } from "@/components/ui"

interface Proc { descricao: string; observacao: string }
interface Data { procedimentos?: Proc[]; observacoes?: string }

const props = defineProps<{ modelValue: Data; readOnly?: boolean }>()
const emit  = defineEmits<{ "update:modelValue": [v: Data] }>()

function atualizar(patch: Partial<Data>) {
    emit("update:modelValue", { ...props.modelValue, ...patch })
}

const lista = computed(() => props.modelValue.procedimentos ?? [])

function addProc() {
    atualizar({ procedimentos: [...lista.value, { descricao: "", observacao: "" }] })
}
function removeProc(idx: number) {
    const a = [...lista.value]; a.splice(idx, 1)
    atualizar({ procedimentos: a })
}
function setField(idx: number, field: keyof Proc, valor: string) {
    const a = [...lista.value]
    a[idx] = { ...a[idx], [field]: valor }
    atualizar({ procedimentos: a })
}
</script>

<template>
    <div class="secao">
        <div v-if="lista.length === 0" class="vazio">
            Nenhum procedimento indicado ainda.
        </div>

        <div v-for="(p, i) in lista" :key="i" class="linha">
            <AppInput
                :model-value="p.descricao"
                class="input-principal"
                placeholder="Procedimento (ex: Cirurgia de vesícula, Infiltração articular...)"
                :disabled="readOnly"
                @update:model-value="(v) => setField(i, 'descricao', String(v))"
            />
            <AppInput
                :model-value="p.observacao"
                placeholder="Observação"
                :disabled="readOnly"
                @update:model-value="(v) => setField(i, 'observacao', String(v))"
            />
            <AppButton variant="danger" size="sm" type="button" title="Remover"
                :disabled="readOnly" @click="removeProc(i)">✕</AppButton>
        </div>

        <AppButton size="sm" icon="fa-solid fa-plus" type="button" :disabled="readOnly" @click="addProc">
            Adicionar procedimento
        </AppButton>

        <div class="subsecao-obs">
            <label class="campo-label">Observações gerais</label>
            <AppTextarea
                :model-value="modelValue.observacoes ?? ''" :rows="2"
                placeholder="Outras considerações sobre os procedimentos indicados..."
                :disabled="readOnly"
                @update:model-value="(v) => atualizar({ observacoes: String(v) })"
            />
        </div>
    </div>
</template>

<style scoped>
.secao { display: flex; flex-direction: column; gap: 0.6rem; }
.vazio { text-align: center; color: var(--text-muted); font-size: 0.88em; padding: 1rem; border: 1px dashed var(--border); border-radius: var(--radius); }

.linha {
    display: grid; grid-template-columns: 2fr 1.5fr 32px; gap: 0.5rem; align-items: center;
}

.subsecao-obs { margin-top: 0.5rem; display: flex; flex-direction: column; gap: 0.3rem; }
.campo-label { font-size: 0.78em; font-weight: 600; color: var(--text-muted); }

@media (max-width: 768px) {
    .linha { grid-template-columns: 1fr 32px; }
    .linha .input-principal { grid-column: 1 / -1; }
}
</style>
