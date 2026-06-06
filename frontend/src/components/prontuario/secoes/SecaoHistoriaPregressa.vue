<!--
  História pregressa (HPP): alergias, medicações, cirurgias, doenças.
  Cada uma tem toggle Sim/Não e lista dinâmica de itens. Alinhado ao legado.
  Briefing 2026-06-05_001: campos `nome` usam AppAutocompleteCriavel alimentado
  pelo pool de variáveis por tipo. Campos livres permanecem AppInput.
-->
<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { AppButton, AppInput, AppTextarea, AppSelect, AppAutocompleteCriavel } from "@/components/ui"
import { variavelPoolService } from "@/services/variavelPoolService"

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

// ── Pool de variáveis por tipo ────────────────────────────────────────────────
// Carregadas uma vez por seção; filtro client-side (CA: sem request por tecla).
const opcoesAlergia    = ref<string[]>([])
const opcoesMedicacao  = ref<string[]>([])
const opcoesCirurgia   = ref<string[]>([])
const opcoesDoenca     = ref<string[]>([])
const poolCarregando   = ref(true)
const poolErro         = ref(false)

onMounted(async () => {
    try {
        const [al, med, cir, doe] = await Promise.all([
            variavelPoolService.listar("Alergia"),
            variavelPoolService.listar("Medicamento"),
            variavelPoolService.listar("Cirurgia"),
            variavelPoolService.listar("Doenca"),
        ])
        opcoesAlergia.value   = al.map(i => i.nome)
        opcoesMedicacao.value = med.map(i => i.nome)
        opcoesCirurgia.value  = cir.map(i => i.nome)
        opcoesDoenca.value    = doe.map(i => i.nome)
    } catch {
        poolErro.value = true
        // CA11: degrada para input texto puro — não bloqueia o preenchimento.
    } finally {
        poolCarregando.value = false
    }
})
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
                    <AppAutocompleteCriavel
                        :model-value="a.nome"
                        :opcoes="opcoesAlergia"
                        placeholder="Alergia (ex: Dipirona, Amendoim)"
                        :disabled="readOnly"
                        :carregando="poolCarregando"
                        :erro="poolErro"
                        @update:model-value="(v) => setItemField('alergias', i, 'nome', v)"
                    />
                    <AppInput
                        :model-value="a.observacao"
                        placeholder="Observação (reação, gravidade...)"
                        :disabled="readOnly"
                        @update:model-value="(v) => setItemField('alergias', i, 'observacao', String(v))"
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
                            <AppAutocompleteCriavel
                                :model-value="m.nome"
                                :opcoes="opcoesMedicacao"
                                placeholder="Nome do medicamento"
                                :disabled="readOnly"
                                :carregando="poolCarregando"
                                :erro="poolErro"
                                @update:model-value="(v) => setItemField('medicacoes', i, 'nome', v)"
                            />
                        </div>
                        <div class="campo">
                            <label>Dose</label>
                            <AppInput
                                :model-value="m.dose"
                                placeholder="Ex: 50mg"
                                :disabled="readOnly"
                                @update:model-value="(v) => setItemField('medicacoes', i, 'dose', String(v))"
                            />
                        </div>
                        <div class="campo">
                            <label>Frequência</label>
                            <AppSelect
                                :model-value="m.frequencia"
                                :disabled="readOnly"
                                @update:model-value="(v) => setItemField('medicacoes', i, 'frequencia', String(v))"
                            >
                                <option value="">Selecione...</option>
                                <option v-for="f in FREQUENCIAS" :key="f" :value="f">{{ f }}</option>
                            </AppSelect>
                        </div>
                        <div class="campo">
                            <label>Motivo / Indicação</label>
                            <AppInput
                                :model-value="m.motivo"
                                placeholder="Ex: Hipertensão"
                                :disabled="readOnly"
                                @update:model-value="(v) => setItemField('medicacoes', i, 'motivo', String(v))"
                            />
                        </div>
                    </div>
                    <AppInput
                        :model-value="m.observacoes"
                        placeholder="Observações (opcional)"
                        :disabled="readOnly"
                        @update:model-value="(v) => setItemField('medicacoes', i, 'observacoes', String(v))"
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
                    <AppAutocompleteCriavel
                        :model-value="c.nome"
                        :opcoes="opcoesCirurgia"
                        placeholder="Cirurgia realizada"
                        :disabled="readOnly"
                        :carregando="poolCarregando"
                        :erro="poolErro"
                        @update:model-value="(v) => setItemField('cirurgias', i, 'nome', v)"
                    />
                    <AppInput
                        :model-value="c.ano" type="number" min="1900" max="2100"
                        class="input-ano"
                        placeholder="Ano"
                        :disabled="readOnly"
                        @update:model-value="(v) => setItemField('cirurgias', i, 'ano', String(v))"
                    />
                    <AppInput
                        :model-value="c.observacao"
                        placeholder="Observação"
                        :disabled="readOnly"
                        @update:model-value="(v) => setItemField('cirurgias', i, 'observacao', String(v))"
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
                    <AppAutocompleteCriavel
                        :model-value="d.nome"
                        :opcoes="opcoesDoenca"
                        placeholder="Doença"
                        :disabled="readOnly"
                        :carregando="poolCarregando"
                        :erro="poolErro"
                        @update:model-value="(v) => setItemField('doencas', i, 'nome', v)"
                    />
                    <AppInput
                        :model-value="d.observacao"
                        placeholder="Observação"
                        :disabled="readOnly"
                        @update:model-value="(v) => setItemField('doencas', i, 'observacao', String(v))"
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
            <AppTextarea
                :model-value="modelValue.observacoes ?? ''"
                :rows="3"
                placeholder="Outras informações relevantes da HPP..."
                :disabled="readOnly"
                @update:model-value="(v) => atualizar({ observacoes: String(v) })"
            />
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

@media (max-width: 768px) {
    .grade-med { grid-template-columns: 1fr 1fr; }
    .item-grade-cirurgia { grid-template-columns: 1fr 80px 32px; }
    .item-grade-cirurgia > :nth-child(3) { grid-column: 1 / -1; }
}
</style>
