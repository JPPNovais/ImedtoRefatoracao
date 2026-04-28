<!--
  História pregressa (HPP): alergias, medicações, cirurgias, doenças.
  Cada uma tem toggle Sim/Não e lista dinâmica de itens. Alinhado ao legado.
-->
<script setup lang="ts">
import { computed } from "vue"
import { AppButton } from "@/components/ui"

interface Alergia { nome: string; observacao: string }
interface Medicacao { nome: string; dose: string; frequencia: string; motivo: string; observacoes: string }
interface Cirurgia { nome: string; ano: string; observacao: string }
interface DoencaPrevia { nome: string; observacao: string }

interface HppData {
    alergiasTem?: boolean
    alergias?: Alergia[]
    medicacoesTem?: boolean
    medicacoes?: Medicacao[]
    cirurgiasTem?: boolean
    cirurgias?: Cirurgia[]
    doencasTem?: boolean
    doencas?: DoencaPrevia[]
    observacoes?: string
}

const props = defineProps<{
    modelValue: HppData
    readOnly?: boolean
}>()
const emit = defineEmits<{ "update:modelValue": [v: HppData] }>()

function atualizar(patch: Partial<HppData>) {
    emit("update:modelValue", { ...props.modelValue, ...patch })
}

// Garante arrays populados quando o toggle é "Sim"
function toggleAlergias(v: boolean) {
    const atual = props.modelValue.alergias ?? []
    atualizar({
        alergiasTem: v,
        alergias: v && atual.length === 0 ? [{ nome: "", observacao: "" }] : atual,
    })
}
function toggleMedicacoes(v: boolean) {
    const atual = props.modelValue.medicacoes ?? []
    atualizar({
        medicacoesTem: v,
        medicacoes: v && atual.length === 0
            ? [{ nome: "", dose: "", frequencia: "", motivo: "", observacoes: "" }]
            : atual,
    })
}
function toggleCirurgias(v: boolean) {
    const atual = props.modelValue.cirurgias ?? []
    atualizar({
        cirurgiasTem: v,
        cirurgias: v && atual.length === 0
            ? [{ nome: "", ano: "", observacao: "" }]
            : atual,
    })
}
function toggleDoencas(v: boolean) {
    const atual = props.modelValue.doencas ?? []
    atualizar({
        doencasTem: v,
        doencas: v && atual.length === 0 ? [{ nome: "", observacao: "" }] : atual,
    })
}

const FREQUENCIAS = [
    "1x/dia", "2x/dia", "3x/dia", "4x/dia",
    "6/6h", "8/8h", "12/12h", "Sob demanda",
]

// Helpers imutáveis
function addItem<T>(campo: keyof HppData, novo: T) {
    const lista = [...((props.modelValue as any)[campo] ?? []), novo]
    atualizar({ [campo]: lista } as any)
}
function removeItem(campo: keyof HppData, idx: number) {
    const lista = [...((props.modelValue as any)[campo] ?? [])]
    lista.splice(idx, 1)
    atualizar({ [campo]: lista } as any)
}
function setItemField(campo: keyof HppData, idx: number, field: string, valor: any) {
    const lista = [...((props.modelValue as any)[campo] ?? [])]
    lista[idx] = { ...lista[idx], [field]: valor }
    atualizar({ [campo]: lista } as any)
}

const alergias    = computed(() => props.modelValue.alergias ?? [])
const medicacoes  = computed(() => props.modelValue.medicacoes ?? [])
const cirurgias   = computed(() => props.modelValue.cirurgias ?? [])
const doencas     = computed(() => props.modelValue.doencas ?? [])
</script>

<template>
    <div class="secao">
        <!-- ── Alergias ───────────────────────────────────────────────────── -->
        <div class="subsecao">
            <div class="subsec-header">
                <span class="subsec-titulo">Alergias</span>
                <div class="toggle">
                    <button
                        type="button" class="toggle-btn"
                        :class="{ ativo: modelValue.alergiasTem === false }"
                        :disabled="readOnly"
                        @click="toggleAlergias(false)"
                    >Não</button>
                    <button
                        type="button" class="toggle-btn"
                        :class="{ ativo: modelValue.alergiasTem === true }"
                        :disabled="readOnly"
                        @click="toggleAlergias(true)"
                    >Sim</button>
                </div>
            </div>

            <div v-if="modelValue.alergiasTem" class="lista">
                <div v-for="(a, i) in alergias" :key="i" class="item-linha">
                    <input
                        :value="a.nome" class="input-field"
                        placeholder="Alergia (ex: Dipirona, Amendoim)"
                        :disabled="readOnly"
                        @input="(e) => setItemField('alergias', i, 'nome', (e.target as HTMLInputElement).value)"
                    />
                    <input
                        :value="a.observacao" class="input-field"
                        placeholder="Observação (reação, gravidade...)"
                        :disabled="readOnly"
                        @input="(e) => setItemField('alergias', i, 'observacao', (e.target as HTMLInputElement).value)"
                    />
                    <AppButton
                        v-if="alergias.length > 1" variant="danger" size="sm" type="button" title="Remover"
                        :disabled="readOnly"
                        @click="removeItem('alergias', i)"
                    >✕</AppButton>
                </div>
                <AppButton
                    size="sm" icon="fa-solid fa-plus" type="button"
                    :disabled="readOnly"
                    @click="addItem('alergias', { nome: '', observacao: '' })"
                >Adicionar alergia</AppButton>
            </div>
        </div>

        <!-- ── Medicações ─────────────────────────────────────────────────── -->
        <div class="subsecao">
            <div class="subsec-header">
                <span class="subsec-titulo">Uso de medicamentos</span>
                <div class="toggle">
                    <button
                        type="button" class="toggle-btn"
                        :class="{ ativo: modelValue.medicacoesTem === false }"
                        :disabled="readOnly"
                        @click="toggleMedicacoes(false)"
                    >Não</button>
                    <button
                        type="button" class="toggle-btn"
                        :class="{ ativo: modelValue.medicacoesTem === true }"
                        :disabled="readOnly"
                        @click="toggleMedicacoes(true)"
                    >Sim</button>
                </div>
            </div>

            <div v-if="modelValue.medicacoesTem" class="lista">
                <div v-for="(m, i) in medicacoes" :key="i" class="card-item">
                    <div class="grade-med">
                        <div class="campo">
                            <label>Medicamento</label>
                            <input
                                :value="m.nome" class="input-field"
                                placeholder="Nome do medicamento"
                                :disabled="readOnly"
                                @input="(e) => setItemField('medicacoes', i, 'nome', (e.target as HTMLInputElement).value)"
                            />
                        </div>
                        <div class="campo">
                            <label>Dose</label>
                            <input
                                :value="m.dose" class="input-field"
                                placeholder="Ex: 50mg"
                                :disabled="readOnly"
                                @input="(e) => setItemField('medicacoes', i, 'dose', (e.target as HTMLInputElement).value)"
                            />
                        </div>
                        <div class="campo">
                            <label>Frequência</label>
                            <select
                                :value="m.frequencia" class="input-field"
                                :disabled="readOnly"
                                @change="(e) => setItemField('medicacoes', i, 'frequencia', (e.target as HTMLSelectElement).value)"
                            >
                                <option value="">Selecione...</option>
                                <option v-for="f in FREQUENCIAS" :key="f" :value="f">{{ f }}</option>
                            </select>
                        </div>
                        <div class="campo">
                            <label>Motivo / Indicação</label>
                            <input
                                :value="m.motivo" class="input-field"
                                placeholder="Ex: Hipertensão"
                                :disabled="readOnly"
                                @input="(e) => setItemField('medicacoes', i, 'motivo', (e.target as HTMLInputElement).value)"
                            />
                        </div>
                    </div>
                    <input
                        :value="m.observacoes" class="input-field"
                        placeholder="Observações (opcional)"
                        :disabled="readOnly"
                        @input="(e) => setItemField('medicacoes', i, 'observacoes', (e.target as HTMLInputElement).value)"
                    />
                    <AppButton
                        v-if="medicacoes.length > 1" variant="danger" size="sm" type="button"
                        :disabled="readOnly"
                        @click="removeItem('medicacoes', i)"
                    >Remover</AppButton>
                </div>
                <AppButton
                    size="sm" icon="fa-solid fa-plus" type="button"
                    :disabled="readOnly"
                    @click="addItem('medicacoes', { nome: '', dose: '', frequencia: '', motivo: '', observacoes: '' })"
                >Adicionar medicação</AppButton>
            </div>
        </div>

        <!-- ── Cirurgias anteriores ──────────────────────────────────────── -->
        <div class="subsecao">
            <div class="subsec-header">
                <span class="subsec-titulo">Cirurgias anteriores</span>
                <div class="toggle">
                    <button
                        type="button" class="toggle-btn"
                        :class="{ ativo: modelValue.cirurgiasTem === false }"
                        :disabled="readOnly"
                        @click="toggleCirurgias(false)"
                    >Não</button>
                    <button
                        type="button" class="toggle-btn"
                        :class="{ ativo: modelValue.cirurgiasTem === true }"
                        :disabled="readOnly"
                        @click="toggleCirurgias(true)"
                    >Sim</button>
                </div>
            </div>

            <div v-if="modelValue.cirurgiasTem" class="lista">
                <div v-for="(c, i) in cirurgias" :key="i" class="item-grade-cirurgia">
                    <input
                        :value="c.nome" class="input-field"
                        placeholder="Cirurgia realizada"
                        :disabled="readOnly"
                        @input="(e) => setItemField('cirurgias', i, 'nome', (e.target as HTMLInputElement).value)"
                    />
                    <input
                        :value="c.ano" type="number" min="1900" max="2100"
                        class="input-field input-ano"
                        placeholder="Ano"
                        :disabled="readOnly"
                        @input="(e) => setItemField('cirurgias', i, 'ano', (e.target as HTMLInputElement).value)"
                    />
                    <input
                        :value="c.observacao" class="input-field"
                        placeholder="Observação"
                        :disabled="readOnly"
                        @input="(e) => setItemField('cirurgias', i, 'observacao', (e.target as HTMLInputElement).value)"
                    />
                    <AppButton
                        v-if="cirurgias.length > 1" variant="danger" size="sm" type="button" title="Remover"
                        :disabled="readOnly"
                        @click="removeItem('cirurgias', i)"
                    >✕</AppButton>
                </div>
                <AppButton
                    size="sm" icon="fa-solid fa-plus" type="button"
                    :disabled="readOnly"
                    @click="addItem('cirurgias', { nome: '', ano: '', observacao: '' })"
                >Adicionar cirurgia</AppButton>
            </div>
        </div>

        <!-- ── Doenças prévias ───────────────────────────────────────────── -->
        <div class="subsecao">
            <div class="subsec-header">
                <span class="subsec-titulo">Doenças prévias</span>
                <div class="toggle">
                    <button
                        type="button" class="toggle-btn"
                        :class="{ ativo: modelValue.doencasTem === false }"
                        :disabled="readOnly"
                        @click="toggleDoencas(false)"
                    >Não</button>
                    <button
                        type="button" class="toggle-btn"
                        :class="{ ativo: modelValue.doencasTem === true }"
                        :disabled="readOnly"
                        @click="toggleDoencas(true)"
                    >Sim</button>
                </div>
            </div>

            <div v-if="modelValue.doencasTem" class="lista">
                <div v-for="(d, i) in doencas" :key="i" class="item-linha">
                    <input
                        :value="d.nome" class="input-field"
                        placeholder="Doença"
                        :disabled="readOnly"
                        @input="(e) => setItemField('doencas', i, 'nome', (e.target as HTMLInputElement).value)"
                    />
                    <input
                        :value="d.observacao" class="input-field"
                        placeholder="Observação"
                        :disabled="readOnly"
                        @input="(e) => setItemField('doencas', i, 'observacao', (e.target as HTMLInputElement).value)"
                    />
                    <AppButton
                        v-if="doencas.length > 1" variant="danger" size="sm" type="button" title="Remover"
                        :disabled="readOnly"
                        @click="removeItem('doencas', i)"
                    >✕</AppButton>
                </div>
                <AppButton
                    size="sm" icon="fa-solid fa-plus" type="button"
                    :disabled="readOnly"
                    @click="addItem('doencas', { nome: '', observacao: '' })"
                >Adicionar doença</AppButton>
            </div>
        </div>

        <!-- ── Observações finais ────────────────────────────────────────── -->
        <div class="subsecao">
            <label class="campo-label-solo">Observações gerais</label>
            <textarea
                :value="modelValue.observacoes ?? ''"
                rows="3"
                class="input-field"
                placeholder="Outras informações relevantes da HPP..."
                :disabled="readOnly"
                @input="(e) => atualizar({ observacoes: (e.target as HTMLTextAreaElement).value })"
            ></textarea>
        </div>
    </div>
</template>

<style scoped>
.secao { display: flex; flex-direction: column; gap: 1rem; }

.subsecao {
    border: 1px solid var(--border); border-radius: var(--radius);
    padding: 0.9rem 1.1rem; background: var(--bg-card);
    display: flex; flex-direction: column; gap: 0.75rem;
}
.subsec-header {
    display: flex; justify-content: space-between; align-items: center;
    flex-wrap: wrap; gap: 0.5rem;
}
.subsec-titulo { font-weight: 700; font-size: 0.92em; }

/* Toggle Sim/Não */
.toggle {
    display: inline-flex; padding: 3px;
    background: rgba(30, 27, 75, 0.05); border-radius: 999px;
}
.toggle-btn {
    border: none; background: none; cursor: pointer;
    padding: 0.25rem 0.8rem; border-radius: 999px;
    font-family: inherit; font-size: 0.78em; font-weight: 600;
    color: rgba(30, 27, 75, 0.55); transition: all 0.12s;
}
.toggle-btn.ativo {
    background: var(--primary-light, #ede9fe);
    color: var(--primary-dark, #4c1d95);
}
.toggle-btn:disabled { opacity: 0.5; cursor: not-allowed; }

.lista { display: flex; flex-direction: column; gap: 0.6rem; }

.item-linha {
    display: grid; grid-template-columns: 1fr 1fr 32px; gap: 0.5rem; align-items: center;
}
.item-grade-cirurgia {
    display: grid; grid-template-columns: 1.5fr 80px 1fr 32px; gap: 0.5rem; align-items: center;
}
.input-ano { text-align: center; }

.card-item {
    border: 1px solid var(--border); border-radius: var(--radius);
    padding: 0.75rem; background: #fafafa;
    display: flex; flex-direction: column; gap: 0.5rem;
    position: relative;
}
.grade-med {
    display: grid; grid-template-columns: 2fr 1fr 1fr 1.5fr; gap: 0.5rem;
}
.campo { display: flex; flex-direction: column; gap: 0.15rem; }
.campo label { font-size: 0.72em; font-weight: 600; color: var(--text-muted); }
.campo-label-solo { font-size: 0.82em; font-weight: 600; color: var(--text-muted); }

.input-field {
    padding: 0.4rem 0.6rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.85em;
    background: var(--bg-card); color: var(--text); width: 100%; box-sizing: border-box;
}
.input-field:focus { outline: none; border-color: var(--primary); }
.input-field:disabled { background: #f9fafb; color: var(--text-muted); }

@media (max-width: 768px) {
    .grade-med { grid-template-columns: 1fr 1fr; }
    .item-grade-cirurgia { grid-template-columns: 1fr 80px 32px; }
    .item-grade-cirurgia .input-field:nth-child(3) { grid-column: 1 / -1; }
}
</style>
