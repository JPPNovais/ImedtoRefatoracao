<!--
  Exame físico estruturado dentro da evolução: sinais vitais, antropometria,
  ectoscopia e descrição da ectoscopia. Alinhado ao legado
  (medical-record/components/ExameFisicoSection.vue): NÃO contém mapa corporal —
  o BodyMap interativo vive apenas na aba dedicada "Exame físico".
-->
<script setup lang="ts">
import { computed } from "vue"

interface EfData {
    paSistolica?: string
    paDiastolica?: string
    fc?: string
    fr?: string
    temperatura?: string
    spo2?: string
    glicemia?: string
    peso?: string
    altura?: string
    estadoGeral?: string
    consciencia?: string
    estadoNutricional?: string
    coloracao?: string
    hidratacao?: string
    cianose?: string
    ictericia?: string
    tempCorporal?: string
    batimentos?: string
    respiracao?: string
    descricaoEctoscopia?: string
}

const props = defineProps<{ modelValue: EfData; readOnly?: boolean }>()
const emit  = defineEmits<{ "update:modelValue": [v: EfData] }>()

function atualizar(patch: Partial<EfData>) {
    emit("update:modelValue", { ...props.modelValue, ...patch })
}

// IMC calculado
const imc = computed<string | null>(() => {
    const p = parseFloat((props.modelValue.peso ?? "").replace(",", "."))
    const h = parseFloat((props.modelValue.altura ?? "").replace(",", "."))
    if (!p || !h) return null
    const alturaM = h > 3 ? h / 100 : h   // aceita cm ou m
    const v = p / (alturaM * alturaM)
    return v.toFixed(1)
})
const classIMC = computed<string | null>(() => {
    if (!imc.value) return null
    const v = parseFloat(imc.value)
    if (v < 18.5) return "Baixo peso"
    if (v < 25)   return "Eutrófico"
    if (v < 30)   return "Sobrepeso"
    if (v < 35)   return "Obesidade grau I"
    if (v < 40)   return "Obesidade grau II"
    return "Obesidade grau III"
})

const ESTADO_GERAL     = ["Bom", "Regular", "Comprometido"]
const CONSCIENCIA      = ["Lúcido(a)", "Confuso", "Sonolento", "Torporoso", "Comatoso"]
const ESTADO_NUTR      = ["Desnutrido", "Eutrófico", "Sobrepeso", "Obeso"]
const COLORACAO        = ["Normocorado", "Hipocorado +/4", "Hipocorado ++/4", "Hipocorado +++/4", "Hipocorado ++++/4", "Hipercorado +/4", "Hipercorado ++/4"]
const HIDRATACAO       = ["Hidratado", "No limiar", "Desidratado +/4", "Desidratado ++/4", "Desidratado +++/4", "Desidratado ++++/4", "Hipervolêmico"]
const CIANOSE          = ["Acianótico", "Cianose periférica +/4", "Cianose periférica ++/4", "Cianose central +/4", "Cianose central ++/4"]
const ICTERICIA        = ["Anictérico", "Ictérico +/4", "Ictérico ++/4", "Ictérico +++/4", "Ictérico ++++/4"]
const TEMP_CORPORAL    = ["Afebril", "Subfebril", "Febril"]
const BATIMENTOS       = ["Bradicárdico", "Normocárdico", "Taquicárdico"]
const RESPIRACAO       = ["Bradipneico", "Eupneico", "Taquipneico", "Dispneico"]
</script>

<template>
    <div class="secao">
        <!-- Sinais vitais -->
        <div class="subsecao">
            <h4 class="subsec-titulo">Sinais vitais</h4>
            <div class="grade-sv">
                <div class="campo-pa">
                    <label>Pressão arterial (mmHg)</label>
                    <div class="pa-row">
                        <input
                            :value="modelValue.paSistolica ?? ''" type="number" class="input-field"
                            placeholder="120" :disabled="readOnly"
                            @input="(e) => atualizar({ paSistolica: (e.target as HTMLInputElement).value })"
                        />
                        <span class="pa-sep">/</span>
                        <input
                            :value="modelValue.paDiastolica ?? ''" type="number" class="input-field"
                            placeholder="80" :disabled="readOnly"
                            @input="(e) => atualizar({ paDiastolica: (e.target as HTMLInputElement).value })"
                        />
                    </div>
                </div>
                <div class="campo">
                    <label>FC (bpm)</label>
                    <input
                        :value="modelValue.fc ?? ''" type="number" class="input-field"
                        placeholder="72" :disabled="readOnly"
                        @input="(e) => atualizar({ fc: (e.target as HTMLInputElement).value })"
                    />
                </div>
                <div class="campo">
                    <label>FR (irpm)</label>
                    <input
                        :value="modelValue.fr ?? ''" type="number" class="input-field"
                        placeholder="16" :disabled="readOnly"
                        @input="(e) => atualizar({ fr: (e.target as HTMLInputElement).value })"
                    />
                </div>
                <div class="campo">
                    <label>Temp. (°C)</label>
                    <input
                        :value="modelValue.temperatura ?? ''" type="number" step="0.1" class="input-field"
                        placeholder="36.5" :disabled="readOnly"
                        @input="(e) => atualizar({ temperatura: (e.target as HTMLInputElement).value })"
                    />
                </div>
                <div class="campo">
                    <label>SpO₂ (%)</label>
                    <input
                        :value="modelValue.spo2 ?? ''" type="number" min="0" max="100" class="input-field"
                        placeholder="98" :disabled="readOnly"
                        @input="(e) => atualizar({ spo2: (e.target as HTMLInputElement).value })"
                    />
                </div>
                <div class="campo">
                    <label>Glicemia (mg/dL)</label>
                    <input
                        :value="modelValue.glicemia ?? ''" type="number" class="input-field"
                        placeholder="95" :disabled="readOnly"
                        @input="(e) => atualizar({ glicemia: (e.target as HTMLInputElement).value })"
                    />
                </div>
            </div>
        </div>

        <!-- Antropometria -->
        <div class="subsecao">
            <h4 class="subsec-titulo">Antropometria</h4>
            <div class="grade-antro">
                <div class="campo">
                    <label>Peso (kg)</label>
                    <input
                        :value="modelValue.peso ?? ''" type="number" step="0.1" class="input-field"
                        placeholder="70.5" :disabled="readOnly"
                        @input="(e) => atualizar({ peso: (e.target as HTMLInputElement).value })"
                    />
                </div>
                <div class="campo">
                    <label>Altura (cm ou m)</label>
                    <input
                        :value="modelValue.altura ?? ''" type="number" step="0.01" class="input-field"
                        placeholder="170 ou 1.70" :disabled="readOnly"
                        @input="(e) => atualizar({ altura: (e.target as HTMLInputElement).value })"
                    />
                </div>
                <div class="campo">
                    <label>IMC (calculado)</label>
                    <input :value="imc ?? ''" class="input-field readonly" readonly />
                </div>
                <div class="campo">
                    <label>Classificação</label>
                    <input :value="classIMC ?? ''" class="input-field readonly" readonly />
                </div>
            </div>
        </div>

        <!-- Ectoscopia -->
        <div class="subsecao">
            <h4 class="subsec-titulo">Ectoscopia (inspeção geral)</h4>
            <div class="grade-ecto">
                <div class="campo">
                    <label>Estado geral</label>
                    <select :value="modelValue.estadoGeral ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ estadoGeral: (e.target as HTMLSelectElement).value })">
                        <option value="">—</option>
                        <option v-for="o in ESTADO_GERAL" :key="o" :value="o">{{ o }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Consciência</label>
                    <select :value="modelValue.consciencia ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ consciencia: (e.target as HTMLSelectElement).value })">
                        <option value="">—</option>
                        <option v-for="o in CONSCIENCIA" :key="o" :value="o">{{ o }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Estado nutricional</label>
                    <select :value="modelValue.estadoNutricional ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ estadoNutricional: (e.target as HTMLSelectElement).value })">
                        <option value="">—</option>
                        <option v-for="o in ESTADO_NUTR" :key="o" :value="o">{{ o }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Coloração</label>
                    <select :value="modelValue.coloracao ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ coloracao: (e.target as HTMLSelectElement).value })">
                        <option value="">—</option>
                        <option v-for="o in COLORACAO" :key="o" :value="o">{{ o }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Hidratação</label>
                    <select :value="modelValue.hidratacao ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ hidratacao: (e.target as HTMLSelectElement).value })">
                        <option value="">—</option>
                        <option v-for="o in HIDRATACAO" :key="o" :value="o">{{ o }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Cianose</label>
                    <select :value="modelValue.cianose ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ cianose: (e.target as HTMLSelectElement).value })">
                        <option value="">—</option>
                        <option v-for="o in CIANOSE" :key="o" :value="o">{{ o }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Icterícia</label>
                    <select :value="modelValue.ictericia ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ ictericia: (e.target as HTMLSelectElement).value })">
                        <option value="">—</option>
                        <option v-for="o in ICTERICIA" :key="o" :value="o">{{ o }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Temp. corporal</label>
                    <select :value="modelValue.tempCorporal ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ tempCorporal: (e.target as HTMLSelectElement).value })">
                        <option value="">—</option>
                        <option v-for="o in TEMP_CORPORAL" :key="o" :value="o">{{ o }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Batimentos</label>
                    <select :value="modelValue.batimentos ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ batimentos: (e.target as HTMLSelectElement).value })">
                        <option value="">—</option>
                        <option v-for="o in BATIMENTOS" :key="o" :value="o">{{ o }}</option>
                    </select>
                </div>
                <div class="campo">
                    <label>Respiração</label>
                    <select :value="modelValue.respiracao ?? ''" class="input-field"
                        :disabled="readOnly"
                        @change="(e) => atualizar({ respiracao: (e.target as HTMLSelectElement).value })">
                        <option value="">—</option>
                        <option v-for="o in RESPIRACAO" :key="o" :value="o">{{ o }}</option>
                    </select>
                </div>
            </div>
            <label class="campo-label">Descrição da ectoscopia</label>
            <textarea
                :value="modelValue.descricaoEctoscopia ?? ''" rows="3" class="input-field"
                placeholder="Observações adicionais sobre a inspeção geral..."
                :disabled="readOnly"
                @input="(e) => atualizar({ descricaoEctoscopia: (e.target as HTMLTextAreaElement).value })"
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
.subsec-titulo { font-weight: 700; font-size: 0.9em; color: var(--primary); margin: 0 0 0.2rem; }

.grade-sv    { display: grid; grid-template-columns: 1.5fr repeat(5, 1fr); gap: 0.6rem; }
.grade-antro { display: grid; grid-template-columns: 1fr 1fr 1fr 1.5fr; gap: 0.6rem; }
.grade-ecto  { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 0.6rem; }

.pa-row { display: flex; align-items: center; gap: 0.35rem; }
.pa-row .input-field { text-align: center; }
.pa-sep { font-weight: 700; color: var(--text-muted); }

.campo, .campo-pa { display: flex; flex-direction: column; gap: 0.15rem; }
.campo label, .campo-pa label { font-size: 0.72em; font-weight: 600; color: var(--text-muted); }
.campo-label { font-size: 0.78em; font-weight: 600; color: var(--text-muted); margin-top: 0.25rem; }

.input-field {
    padding: 0.4rem 0.6rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.85em;
    background: var(--bg-card); color: var(--text); width: 100%; box-sizing: border-box;
}
.input-field:focus { outline: none; border-color: var(--primary); }
.input-field.readonly { background: #f9fafb; color: var(--text-muted); }

@media (max-width: 900px) {
    .grade-sv, .grade-antro { grid-template-columns: 1fr 1fr; }
}
</style>
