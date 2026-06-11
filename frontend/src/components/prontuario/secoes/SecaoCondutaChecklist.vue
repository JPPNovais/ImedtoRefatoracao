<!--
  Seção "Conduta" do prontuário como checklist de 6 ações fixas + observação livre.
  Tipo: "conduta_checklist" (novo). Evoluções antigas com conduta em texto livre
  renderizam read-only via fallback no SecaoProntuario.vue (CA73/R1).

  ConteudoJson persistido: { acoesMarcadas: AcaoPendencia[], observacao: string }
  LGPD: a observação fica no ConteudoJson da evolução — nunca copiada para a pendência (R4).
-->
<script setup lang="ts">
import { computed } from "vue"
import { AppTextarea } from "@/components/ui"
import { ACAO_LABELS, type AcaoPendencia } from "@/services/pendenciaService"

// ── Tipos ──────────────────────────────────────────────────────────────────────

interface CondutaChecklist {
    acoesMarcadas?: AcaoPendencia[]
    observacao?: string
}

// ── Props / emits ──────────────────────────────────────────────────────────────

const props = defineProps<{
    modelValue: CondutaChecklist
    readOnly?: boolean
}>()
const emit = defineEmits<{ "update:modelValue": [v: CondutaChecklist] }>()

// ── Ações fixas do sistema (R1/briefing §3) ────────────────────────────────────

const ACOES_FIXAS: AcaoPendencia[] = [
    "CriarReceita",
    "CriarAtestado",
    "PedirExame",
    "CriarOrcamento",
    "MarcarProcedimentoRealizado",
    "AgendarRetorno",
]

// ── Estado local ───────────────────────────────────────────────────────────────

const acoesMarcadas = computed(() => props.modelValue.acoesMarcadas ?? [])
const observacao = computed(() => props.modelValue.observacao ?? "")

function isMarcada(acao: AcaoPendencia): boolean {
    return acoesMarcadas.value.includes(acao)
}

function toggleAcao(acao: AcaoPendencia) {
    if (props.readOnly) return
    const atual = [...acoesMarcadas.value]
    const idx = atual.indexOf(acao)
    if (idx >= 0) {
        atual.splice(idx, 1)
    } else {
        atual.push(acao)
    }
    emit("update:modelValue", { ...props.modelValue, acoesMarcadas: atual })
}

function setObservacao(v: string | number) {
    emit("update:modelValue", { ...props.modelValue, observacao: String(v) })
}
</script>

<template>
    <div class="conduta-checklist">
        <p class="conduta-hint">Marque as ações a serem tomadas para este paciente:</p>

        <ul class="acoes-lista">
            <li
                v-for="acao in ACOES_FIXAS"
                :key="acao"
                class="acao-item"
            >
                <label
                    :class="['acao-label', { 'acao-label--marcada': isMarcada(acao), 'acao-label--readonly': readOnly }]"
                >
                    <input
                        type="checkbox"
                        class="acao-checkbox"
                        :checked="isMarcada(acao)"
                        :disabled="readOnly"
                        @change="toggleAcao(acao)"
                    />
                    <span class="acao-texto">{{ ACAO_LABELS[acao] }}</span>
                </label>
            </li>
        </ul>

        <div class="obs-wrapper">
            <label class="field-label">Observação</label>
            <AppTextarea
                :model-value="observacao"
                :rows="3"
                placeholder="Observações complementares sobre a conduta..."
                :disabled="readOnly"
                @update:model-value="setObservacao"
            />
        </div>

        <p v-if="readOnly && acoesMarcadas.length === 0" class="vazio">
            Nenhuma ação de conduta registrada.
        </p>
    </div>
</template>

<style scoped>
.conduta-checklist {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.conduta-hint {
    font-size: var(--text-sm);
    color: var(--text-muted);
    margin: 0;
}

.acoes-lista {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
}

.acao-item {
    display: flex;
}

.acao-label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    cursor: pointer;
    padding: 0.4rem 0.6rem;
    border-radius: var(--radius);
    border: 1px solid transparent;
    transition: background 0.1s;
    width: 100%;
}

.acao-label:hover:not(.acao-label--readonly) {
    background: var(--muted);
    border-color: var(--border);
}

.acao-label--marcada {
    background: hsl(var(--primary-hsl) / 0.06);
    border-color: hsl(var(--primary-hsl) / 0.3);
}

.acao-label--readonly {
    cursor: default;
}

.acao-checkbox {
    width: 1rem;
    height: 1rem;
    flex-shrink: 0;
    accent-color: hsl(var(--primary-hsl));
    cursor: pointer;
}

.acao-checkbox:disabled {
    cursor: default;
    opacity: 0.7;
}

.acao-texto {
    font-size: var(--text-sm);
    color: var(--text);
}

.acao-label--marcada .acao-texto {
    font-weight: var(--font-weight-medium);
    color: hsl(var(--primary-hsl));
}

.obs-wrapper {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
    margin-top: 0.25rem;
}

.vazio {
    font-size: var(--text-sm);
    color: var(--text-muted);
    text-align: center;
    padding: 0.75rem;
    border: 1px dashed var(--border);
    border-radius: var(--radius);
    margin: 0;
}
</style>
