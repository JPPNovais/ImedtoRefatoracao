<!-- História social: tabagismo, etilismo, drogas, atividade física, alimentação, sono. -->
<script setup lang="ts">
import { AppInput, AppTextarea, AppSelect } from "@/components/ui"
interface HsData {
    estadoCivil?: string
    filhosTem?: boolean
    filhosQuantos?: string
    filhosIdades?: string
    filhosObs?: string
    tabagismoTem?: boolean
    tabagismoStatus?: string
    tabagismoObs?: string
    etilismoTem?: boolean
    etilismoStatus?: string
    etilismoObs?: string
    drogasTem?: boolean
    drogasObs?: string
    atividadeFisicaTem?: boolean
    atividadeFisicaNivel?: string
    atividadeFisicaObs?: string
    alimentacao?: string
    alimentacaoObs?: string
    sonoQualidade?: string
    sonoObs?: string
}

const props = defineProps<{ modelValue: HsData; readOnly?: boolean }>()
const emit  = defineEmits<{ "update:modelValue": [v: HsData] }>()

function atualizar(patch: Partial<HsData>) {
    emit("update:modelValue", { ...props.modelValue, ...patch })
}

const ESTADOS_CIVIS  = ["Solteiro(a)", "Casado(a)", "União estável", "Divorciado(a)", "Separado(a)", "Viúvo(a)"]
const TABAGISMO      = ["Não fuma / nunca fumou", "Fumante ativo", "Fumante passivo", "Ex-fumante"]
const ETILISMO       = ["Não bebe", "Esporádico", "Social", "Moderado", "Frequente", "Ex-etilista"]
const NIVEIS_ATIV    = ["Leve", "Moderado", "Intenso"]
const TIPOS_ALIM     = ["Equilibrada", "Irregular", "Restritiva", "Vegetariana", "Vegana", "Hipossódica", "Para diabético"]
const QUAL_SONO      = ["Bom (7-8h)", "Regular (5-6h)", "Ruim / insônia", "Irregular (horários)"]
</script>

<template>
    <div class="secao">
        <div class="subsecao">
            <div class="grade-2">
                <div class="campo">
                    <label>Estado civil</label>
                    <AppSelect
                        :model-value="modelValue.estadoCivil ?? ''"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ estadoCivil: String(v) })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="s in ESTADOS_CIVIS" :key="s" :value="s">{{ s }}</option>
                    </AppSelect>
                </div>
                <div class="campo">
                    <label>Filhos?</label>
                    <div class="toggle">
                        <button
                            type="button" class="toggle-btn"
                            :class="{ ativo: modelValue.filhosTem === false }"
                            :disabled="readOnly"
                            @click="atualizar({ filhosTem: false })"
                        >Não</button>
                        <button
                            type="button" class="toggle-btn"
                            :class="{ ativo: modelValue.filhosTem === true }"
                            :disabled="readOnly"
                            @click="atualizar({ filhosTem: true })"
                        >Sim</button>
                    </div>
                </div>
            </div>
            <div v-if="modelValue.filhosTem" class="grade-3">
                <div class="campo">
                    <label>Quantos?</label>
                    <AppInput
                        :model-value="modelValue.filhosQuantos ?? ''" type="number" min="0"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ filhosQuantos: String(v) })"
                    />
                </div>
                <div class="campo">
                    <label>Idades</label>
                    <AppInput
                        :model-value="modelValue.filhosIdades ?? ''"
                        placeholder="Ex: 8, 12, 15"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ filhosIdades: String(v) })"
                    />
                </div>
                <div class="campo">
                    <label>Observações</label>
                    <AppInput
                        :model-value="modelValue.filhosObs ?? ''"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ filhosObs: String(v) })"
                    />
                </div>
            </div>
        </div>

        <div class="subsecao">
            <div class="subsec-header">
                <span class="subsec-titulo">Tabagismo</span>
                <div class="toggle">
                    <button type="button" class="toggle-btn" :class="{ ativo: modelValue.tabagismoTem === false }"
                        :disabled="readOnly" @click="atualizar({ tabagismoTem: false })">Não</button>
                    <button type="button" class="toggle-btn" :class="{ ativo: modelValue.tabagismoTem === true }"
                        :disabled="readOnly" @click="atualizar({ tabagismoTem: true })">Sim</button>
                </div>
            </div>
            <div v-if="modelValue.tabagismoTem" class="grade-2">
                <div class="campo">
                    <label>Status</label>
                    <AppSelect
                        :model-value="modelValue.tabagismoStatus ?? ''"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ tabagismoStatus: String(v) })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="t in TABAGISMO" :key="t" :value="t">{{ t }}</option>
                    </AppSelect>
                </div>
                <div class="campo">
                    <label>Observações</label>
                    <AppInput
                        :model-value="modelValue.tabagismoObs ?? ''"
                        placeholder="Quantidade, tempo de uso..."
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ tabagismoObs: String(v) })"
                    />
                </div>
            </div>
        </div>

        <div class="subsecao">
            <div class="subsec-header">
                <span class="subsec-titulo">Etilismo</span>
                <div class="toggle">
                    <button type="button" class="toggle-btn" :class="{ ativo: modelValue.etilismoTem === false }"
                        :disabled="readOnly" @click="atualizar({ etilismoTem: false })">Não</button>
                    <button type="button" class="toggle-btn" :class="{ ativo: modelValue.etilismoTem === true }"
                        :disabled="readOnly" @click="atualizar({ etilismoTem: true })">Sim</button>
                </div>
            </div>
            <div v-if="modelValue.etilismoTem" class="grade-2">
                <div class="campo">
                    <label>Status</label>
                    <AppSelect
                        :model-value="modelValue.etilismoStatus ?? ''"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ etilismoStatus: String(v) })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="e in ETILISMO" :key="e" :value="e">{{ e }}</option>
                    </AppSelect>
                </div>
                <div class="campo">
                    <label>Observações</label>
                    <AppInput
                        :model-value="modelValue.etilismoObs ?? ''"
                        placeholder="Tipo, frequência..."
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ etilismoObs: String(v) })"
                    />
                </div>
            </div>
        </div>

        <div class="subsecao">
            <div class="subsec-header">
                <span class="subsec-titulo">Outras drogas / substâncias</span>
                <div class="toggle">
                    <button type="button" class="toggle-btn" :class="{ ativo: modelValue.drogasTem === false }"
                        :disabled="readOnly" @click="atualizar({ drogasTem: false })">Não</button>
                    <button type="button" class="toggle-btn" :class="{ ativo: modelValue.drogasTem === true }"
                        :disabled="readOnly" @click="atualizar({ drogasTem: true })">Sim</button>
                </div>
            </div>
            <div v-if="modelValue.drogasTem">
                <AppTextarea
                    :model-value="modelValue.drogasObs ?? ''" :rows="2"
                    placeholder="Tipo, via de uso, frequência, há quanto tempo..."
                    :disabled="readOnly"
                    @update:model-value="(v) => atualizar({ drogasObs: String(v) })"
                />
            </div>
        </div>

        <div class="subsecao">
            <div class="subsec-header">
                <span class="subsec-titulo">Atividade física</span>
                <div class="toggle">
                    <button type="button" class="toggle-btn" :class="{ ativo: modelValue.atividadeFisicaTem === false }"
                        :disabled="readOnly" @click="atualizar({ atividadeFisicaTem: false })">Não</button>
                    <button type="button" class="toggle-btn" :class="{ ativo: modelValue.atividadeFisicaTem === true }"
                        :disabled="readOnly" @click="atualizar({ atividadeFisicaTem: true })">Sim</button>
                </div>
            </div>
            <div v-if="modelValue.atividadeFisicaTem" class="grade-2">
                <div class="campo">
                    <label>Nível</label>
                    <AppSelect
                        :model-value="modelValue.atividadeFisicaNivel ?? ''"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ atividadeFisicaNivel: String(v) })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="n in NIVEIS_ATIV" :key="n" :value="n">{{ n }}</option>
                    </AppSelect>
                </div>
                <div class="campo">
                    <label>Observações</label>
                    <AppInput
                        :model-value="modelValue.atividadeFisicaObs ?? ''"
                        placeholder="Modalidades, frequência semanal..."
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ atividadeFisicaObs: String(v) })"
                    />
                </div>
            </div>
        </div>

        <div class="subsecao">
            <div class="grade-2">
                <div class="campo">
                    <label>Alimentação</label>
                    <AppSelect
                        :model-value="modelValue.alimentacao ?? ''"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ alimentacao: String(v) })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="a in TIPOS_ALIM" :key="a" :value="a">{{ a }}</option>
                    </AppSelect>
                </div>
                <div class="campo">
                    <label>Sono e rotina</label>
                    <AppSelect
                        :model-value="modelValue.sonoQualidade ?? ''"
                        :disabled="readOnly"
                        @update:model-value="(v) => atualizar({ sonoQualidade: String(v) })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="q in QUAL_SONO" :key="q" :value="q">{{ q }}</option>
                    </AppSelect>
                </div>
            </div>
            <AppTextarea
                :model-value="modelValue.alimentacaoObs ?? ''" :rows="2"
                placeholder="Observações de alimentação..."
                :disabled="readOnly"
                @update:model-value="(v) => atualizar({ alimentacaoObs: String(v) })"
            />
            <AppTextarea
                :model-value="modelValue.sonoObs ?? ''" :rows="2"
                placeholder="Observações de sono..."
                :disabled="readOnly"
                @update:model-value="(v) => atualizar({ sonoObs: String(v) })"
            />
        </div>
    </div>
</template>

<style scoped>
.secao { display: flex; flex-direction: column; gap: 1rem; }

.subsecao {
    border: 1px solid var(--border); border-radius: var(--radius);
    padding: 0.9rem 1.1rem; background: var(--bg-card);
    display: flex; flex-direction: column; gap: 0.6rem;
}
.subsec-header { display: flex; justify-content: space-between; align-items: center; }
.subsec-titulo { font-weight: 700; font-size: 0.92em; }

.grade-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 0.6rem; }
.grade-3 { display: grid; grid-template-columns: 100px 1fr 1.5fr; gap: 0.6rem; }

.campo { display: flex; flex-direction: column; gap: 0.15rem; }
.campo label { font-size: 0.72em; font-weight: 600; color: var(--text-muted); }

.toggle {
    display: inline-flex; padding: 3px;
    background: rgba(30, 27, 75, 0.05); border-radius: 999px;
    width: fit-content;
}
.toggle-btn {
    border: none; background: none; cursor: pointer;
    padding: 0.25rem 0.8rem; border-radius: 999px;
    font-family: inherit; font-size: 0.78em; font-weight: 600;
    color: rgba(30, 27, 75, 0.55); transition: all 0.12s;
}
.toggle-btn.ativo {
    background: var(--primary-light, #ede9fe); color: var(--primary-dark, #4c1d95);
}

@media (max-width: 768px) {
    .grade-2, .grade-3 { grid-template-columns: 1fr; }
}
</style>
