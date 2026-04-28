<!-- História social: tabagismo, etilismo, drogas, atividade física, alimentação, sono. -->
<script setup lang="ts">
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
                    <select
                        :value="modelValue.estadoCivil ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ estadoCivil: (e.target as HTMLSelectElement).value })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="s in ESTADOS_CIVIS" :key="s" :value="s">{{ s }}</option>
                    </select>
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
                    <input
                        :value="modelValue.filhosQuantos ?? ''" type="number" min="0" class="input-field"
                        :disabled="readOnly"
                        @input="(e) => atualizar({ filhosQuantos: (e.target as HTMLInputElement).value })"
                    />
                </div>
                <div class="campo">
                    <label>Idades</label>
                    <input
                        :value="modelValue.filhosIdades ?? ''" class="input-field"
                        placeholder="Ex: 8, 12, 15"
                        :disabled="readOnly"
                        @input="(e) => atualizar({ filhosIdades: (e.target as HTMLInputElement).value })"
                    />
                </div>
                <div class="campo">
                    <label>Observações</label>
                    <input
                        :value="modelValue.filhosObs ?? ''" class="input-field"
                        :disabled="readOnly"
                        @input="(e) => atualizar({ filhosObs: (e.target as HTMLInputElement).value })"
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
                    <select
                        :value="modelValue.tabagismoStatus ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ tabagismoStatus: (e.target as HTMLSelectElement).value })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="t in TABAGISMO" :key="t" :value="t">{{ t }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Observações</label>
                    <input
                        :value="modelValue.tabagismoObs ?? ''" class="input-field"
                        placeholder="Quantidade, tempo de uso..."
                        :disabled="readOnly"
                        @input="(e) => atualizar({ tabagismoObs: (e.target as HTMLInputElement).value })"
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
                    <select
                        :value="modelValue.etilismoStatus ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ etilismoStatus: (e.target as HTMLSelectElement).value })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="e in ETILISMO" :key="e" :value="e">{{ e }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Observações</label>
                    <input
                        :value="modelValue.etilismoObs ?? ''" class="input-field"
                        placeholder="Tipo, frequência..."
                        :disabled="readOnly"
                        @input="(e) => atualizar({ etilismoObs: (e.target as HTMLInputElement).value })"
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
                <textarea
                    :value="modelValue.drogasObs ?? ''" rows="2" class="input-field"
                    placeholder="Tipo, via de uso, frequência, há quanto tempo..."
                    :disabled="readOnly"
                    @input="(e) => atualizar({ drogasObs: (e.target as HTMLTextAreaElement).value })"
                ></textarea>
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
                    <select
                        :value="modelValue.atividadeFisicaNivel ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ atividadeFisicaNivel: (e.target as HTMLSelectElement).value })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="n in NIVEIS_ATIV" :key="n" :value="n">{{ n }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Observações</label>
                    <input
                        :value="modelValue.atividadeFisicaObs ?? ''" class="input-field"
                        placeholder="Modalidades, frequência semanal..."
                        :disabled="readOnly"
                        @input="(e) => atualizar({ atividadeFisicaObs: (e.target as HTMLInputElement).value })"
                    />
                </div>
            </div>
        </div>

        <div class="subsecao">
            <div class="grade-2">
                <div class="campo">
                    <label>Alimentação</label>
                    <select
                        :value="modelValue.alimentacao ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ alimentacao: (e.target as HTMLSelectElement).value })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="a in TIPOS_ALIM" :key="a" :value="a">{{ a }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Sono e rotina</label>
                    <select
                        :value="modelValue.sonoQualidade ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ sonoQualidade: (e.target as HTMLSelectElement).value })"
                    >
                        <option value="">Selecione...</option>
                        <option v-for="q in QUAL_SONO" :key="q" :value="q">{{ q }}</option>
                    </select>
                </div>
            </div>
            <textarea
                :value="modelValue.alimentacaoObs ?? ''" rows="2" class="input-field"
                placeholder="Observações de alimentação..."
                :disabled="readOnly"
                @input="(e) => atualizar({ alimentacaoObs: (e.target as HTMLTextAreaElement).value })"
            ></textarea>
            <textarea
                :value="modelValue.sonoObs ?? ''" rows="2" class="input-field"
                placeholder="Observações de sono..."
                :disabled="readOnly"
                @input="(e) => atualizar({ sonoObs: (e.target as HTMLTextAreaElement).value })"
            ></textarea>
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

.input-field {
    padding: 0.4rem 0.6rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.85em;
    background: var(--bg-card); color: var(--text); width: 100%; box-sizing: border-box;
}
.input-field:focus { outline: none; border-color: var(--primary); }

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
