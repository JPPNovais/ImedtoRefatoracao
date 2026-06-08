<!-- Lista de exames realizados (laboratoriais, imagem, etc). -->
<script setup lang="ts">
import { computed } from "vue"
import { AppButton, AppInput, AppTextarea, AppSelect } from "@/components/ui"

interface Exame { tipo: string; material: string; nome: string; comentario: string }
interface Data { itens?: Exame[]; observacoes?: string }

const props = defineProps<{ modelValue: Data; readOnly?: boolean }>()
const emit  = defineEmits<{ "update:modelValue": [v: Data] }>()

function atualizar(patch: Partial<Data>) {
    emit("update:modelValue", { ...props.modelValue, ...patch })
}

const TIPOS = ["Laboratorial", "Imagem", "Funcional", "Endoscópico", "Outro"]

const itens = computed(() => props.modelValue.itens ?? [])

function addItem() {
    atualizar({ itens: [...itens.value, { tipo: "", material: "", nome: "", comentario: "" }] })
}
function removeItem(idx: number) {
    const lista = [...itens.value]; lista.splice(idx, 1)
    atualizar({ itens: lista })
}
function setField(idx: number, field: keyof Exame, valor: string) {
    const lista = [...itens.value]
    lista[idx] = { ...lista[idx], [field]: valor }
    atualizar({ itens: lista })
}
</script>

<template>
    <div class="secao">
        <div v-if="itens.length === 0" class="vazio">
            Nenhum exame adicionado ainda.
        </div>

        <div v-for="(e, i) in itens" :key="i" class="card-exame">
            <div class="grade">
                <div class="campo">
                    <label>Tipo</label>
                    <AppSelect
                        :model-value="e.tipo"
                        :disabled="readOnly"
                        @update:model-value="(v) => setField(i, 'tipo', String(v))"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="t in TIPOS" :key="t" :value="t">{{ t }}</option>
                    </AppSelect>
                </div>
                <div class="campo">
                    <label>Material</label>
                    <AppInput
                        :model-value="e.material"
                        placeholder="Sangue, urina, tecido..."
                        :disabled="readOnly"
                        @update:model-value="(v) => setField(i, 'material', String(v))"
                    />
                </div>
                <div class="campo">
                    <label>Nome do exame</label>
                    <AppInput
                        :model-value="e.nome"
                        placeholder="Hemograma completo, RX tórax..."
                        :disabled="readOnly"
                        @update:model-value="(v) => setField(i, 'nome', String(v))"
                    />
                </div>
            </div>
            <AppInput
                :model-value="e.comentario"
                placeholder="Resultado / comentário"
                :disabled="readOnly"
                @update:model-value="(v) => setField(i, 'comentario', String(v))"
            />
            <AppButton variant="danger" size="sm" type="button" :disabled="readOnly" @click="removeItem(i)">
                Remover exame
            </AppButton>
        </div>

        <AppButton size="sm" icon="fa-solid fa-plus" type="button" :disabled="readOnly" @click="addItem">
            Adicionar exame
        </AppButton>

        <div class="subsecao-obs">
            <label class="field-label">Observações gerais dos exames</label>
            <AppTextarea
                :model-value="modelValue.observacoes ?? ''" :rows="2"
                placeholder="Conclusões, pendências de exames..."
                :disabled="readOnly"
                @update:model-value="(v) => atualizar({ observacoes: String(v) })"
            />
            <p class="hint">
                📎 Para anexar arquivos (PDFs, imagens), use a seção <strong>Anexos</strong> na aba Histórico após salvar a evolução.
            </p>
        </div>
    </div>
</template>

<style scoped>
.secao { display: flex; flex-direction: column; gap: 0.75rem; }
.vazio { text-align: center; color: var(--text-muted); font-size: 0.88em; padding: 1rem; border: 1px dashed var(--border); border-radius: var(--radius); }

.card-exame {
    border: 1px solid var(--border); border-radius: var(--radius);
    padding: 0.75rem; background: #fafafa;
    display: flex; flex-direction: column; gap: 0.5rem;
}
.grade { display: grid; grid-template-columns: 180px 1fr 1.5fr; gap: 0.5rem; }
.campo { display: flex; flex-direction: column; gap: 0.15rem; }
.campo label { font-size: 0.72em; font-weight: 600; color: var(--text-muted); }

.subsecao-obs {
    border-top: 1px solid var(--border); padding-top: 0.75rem;
    display: flex; flex-direction: column; gap: 0.4rem;
}
.hint {
    font-size: 0.78em; color: var(--text-muted);
    background: #eff6ff; border-left: 3px solid #3b82f6;
    padding: 0.5rem 0.75rem; border-radius: 6px; margin: 0;
}

@media (max-width: 768px) { .grade { grid-template-columns: 1fr; } }
</style>
