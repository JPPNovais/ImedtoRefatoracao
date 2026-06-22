<script setup lang="ts">
import { computed } from "vue"
import { AppInput, AppTextarea, AppSelect, AppDatePicker, AppPillToggle } from "@/components/ui"

export interface EvolucaoPosOp {
    evolucaoPaciente: "otima" | "boa" | "regular" | "ruim" | ""
    evolucaoComentario: string
    seguindoOrientacoes: "sim" | "nao" | ""
    orientacoesComentario: string
    dataCirurgia: string
    dpo: string | number
    destino: "Enfermaria" | "UTI" | "RPA" | "Alta" | ""
    dieta: "Zero" | "Líquida" | "Pastosa" | "Branda" | "Livre" | ""
    observacao: string
}

const props = defineProps<{
    modelValue: EvolucaoPosOp
    readOnly?: boolean
}>()

const emit = defineEmits<{ "update:modelValue": [v: EvolucaoPosOp] }>()

const OPCOES_EVOLUCAO = [
    { valor: "otima",   label: "Ótima"   },
    { valor: "boa",     label: "Boa"     },
    { valor: "regular", label: "Regular" },
    { valor: "ruim",    label: "Ruim"    },
]

const OPCOES_ORIENTACOES = [
    { valor: "sim", label: "Sim" },
    { valor: "nao", label: "Não" },
]

function calcularDpo(dataCirurgia: string): string {
    if (!dataCirurgia) return ""
    const cirurgia = new Date(dataCirurgia + "T12:00:00")
    const hoje = new Date()
    hoje.setHours(12, 0, 0, 0)
    if (isNaN(cirurgia.getTime())) return ""
    const diff = Math.round((hoje.getTime() - cirurgia.getTime()) / (1000 * 60 * 60 * 24))
    if (diff < 0) return ""
    return String(diff)
}

const dpoCalculado = computed(() => calcularDpo(props.modelValue.dataCirurgia))

function atualizar(patch: Partial<EvolucaoPosOp>) {
    const merged = { ...props.modelValue, ...patch }
    if (patch.dataCirurgia !== undefined) {
        merged.dpo = calcularDpo(patch.dataCirurgia)
    }
    emit("update:modelValue", merged)
}
</script>

<template>
    <div class="secao-pos-op">
        <!-- Linha: evolução do paciente -->
        <div class="linha-toggle">
            <span class="field-label">Como está a evolução do paciente?</span>
            <div class="linha-toggle-controles">
                <AppPillToggle
                    :model-value="modelValue.evolucaoPaciente"
                    :opcoes="OPCOES_EVOLUCAO"
                    @update:model-value="(v) => !readOnly && atualizar({ evolucaoPaciente: v as EvolucaoPosOp['evolucaoPaciente'] })"
                />
                <AppInput
                    :model-value="modelValue.evolucaoComentario"
                    placeholder="Comentário (opcional)"
                    :disabled="readOnly"
                    @update:model-value="(v) => atualizar({ evolucaoComentario: String(v) })"
                />
            </div>
        </div>

        <!-- Linha: seguindo orientações -->
        <div class="linha-toggle">
            <span class="field-label">Seguindo orientações?</span>
            <div class="linha-toggle-controles">
                <AppPillToggle
                    :model-value="modelValue.seguindoOrientacoes"
                    :opcoes="OPCOES_ORIENTACOES"
                    @update:model-value="(v) => !readOnly && atualizar({ seguindoOrientacoes: v as EvolucaoPosOp['seguindoOrientacoes'] })"
                />
                <AppInput
                    :model-value="modelValue.orientacoesComentario"
                    placeholder="Comentário (opcional)"
                    :disabled="readOnly"
                    @update:model-value="(v) => atualizar({ orientacoesComentario: String(v) })"
                />
            </div>
        </div>

        <!-- Grade 4 colunas -->
        <div class="grade-4">
            <div class="campo">
                <span class="field-label">Data da cirurgia</span>
                <AppDatePicker
                    :model-value="modelValue.dataCirurgia"
                    :disabled="readOnly"
                    @update:model-value="(v) => atualizar({ dataCirurgia: String(v) })"
                />
            </div>
            <div class="campo">
                <span class="field-label">DPO (dias)</span>
                <AppInput
                    :model-value="dpoCalculado"
                    :disabled="true"
                    placeholder="—"
                />
            </div>
            <div class="campo">
                <span class="field-label">Destino</span>
                <AppSelect
                    :model-value="modelValue.destino"
                    :disabled="readOnly"
                    @update:model-value="(v) => atualizar({ destino: v as EvolucaoPosOp['destino'] })"
                >
                    <option value="">—</option>
                    <option value="Enfermaria">Enfermaria</option>
                    <option value="UTI">UTI</option>
                    <option value="RPA">RPA</option>
                    <option value="Alta">Alta</option>
                </AppSelect>
            </div>
            <div class="campo">
                <span class="field-label">Dieta</span>
                <AppSelect
                    :model-value="modelValue.dieta"
                    :disabled="readOnly"
                    @update:model-value="(v) => atualizar({ dieta: v as EvolucaoPosOp['dieta'] })"
                >
                    <option value="">—</option>
                    <option value="Zero">Zero</option>
                    <option value="Líquida">Líquida</option>
                    <option value="Pastosa">Pastosa</option>
                    <option value="Branda">Branda</option>
                    <option value="Livre">Livre</option>
                </AppSelect>
            </div>
        </div>

        <!-- Observação -->
        <div class="campo">
            <span class="field-label">Observação</span>
            <AppTextarea
                :model-value="modelValue.observacao"
                :rows="4"
                placeholder="Observações gerais do pós-operatório..."
                :disabled="readOnly"
                @update:model-value="(v) => atualizar({ observacao: String(v) })"
            />
        </div>
    </div>
</template>

<style scoped>
.secao-pos-op {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.linha-toggle {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
}

.linha-toggle-controles {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
}

.grade-4 {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 0.75rem;
    align-items: end;
}

.campo {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

@media (max-width: 768px) {
    .grade-4 {
        grid-template-columns: 1fr;
    }

    .linha-toggle-controles {
        flex-direction: column;
        align-items: stretch;
    }
}
</style>
