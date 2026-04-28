<!-- História familiar: pai, mãe, parentes com doenças hereditárias. -->
<script setup lang="ts">
import { computed } from "vue"
import { AppButton } from "@/components/ui"

interface Parente { parentesco: string; doencas: string; comentario: string }
interface HfData {
    paiDoencas?: string
    paiDescricao?: string
    maeDoencas?: string
    maeDescricao?: string
    parentes?: Parente[]
    observacao?: string
}

const props = defineProps<{ modelValue: HfData; readOnly?: boolean }>()
const emit  = defineEmits<{ "update:modelValue": [v: HfData] }>()

function atualizar(patch: Partial<HfData>) {
    emit("update:modelValue", { ...props.modelValue, ...patch })
}

const PARENTESCOS = [
    "Irmão(ã)", "Meio-irmão materno", "Meio-irmão paterno",
    "Filho(a)", "Neto(a)",
    "Avó materna", "Avô materno", "Avó paterna", "Avô paterno",
    "Tio(a) materno", "Tio(a) paterno",
    "Primo(a)", "Sobrinho(a)",
]

const parentes = computed(() => props.modelValue.parentes ?? [])

function addParente() {
    atualizar({ parentes: [...parentes.value, { parentesco: "", doencas: "", comentario: "" }] })
}
function removeParente(idx: number) {
    const lista = [...parentes.value]; lista.splice(idx, 1)
    atualizar({ parentes: lista })
}
function setParente(idx: number, field: keyof Parente, valor: string) {
    const lista = [...parentes.value]
    lista[idx] = { ...lista[idx], [field]: valor }
    atualizar({ parentes: lista })
}
</script>

<template>
    <div class="secao">
        <div class="grade-2">
            <div class="card-pai">
                <h4 class="card-titulo">Pai</h4>
                <label class="campo-label">Doenças hereditárias</label>
                <input
                    :value="modelValue.paiDoencas ?? ''" class="input-field"
                    placeholder="Ex: Hipertensão, diabetes..."
                    :disabled="readOnly"
                    @input="(e) => atualizar({ paiDoencas: (e.target as HTMLInputElement).value })"
                />
                <label class="campo-label">Descrição</label>
                <textarea
                    :value="modelValue.paiDescricao ?? ''" rows="3" class="input-field"
                    placeholder="Detalhes adicionais"
                    :disabled="readOnly"
                    @input="(e) => atualizar({ paiDescricao: (e.target as HTMLTextAreaElement).value })"
                ></textarea>
            </div>

            <div class="card-mae">
                <h4 class="card-titulo">Mãe</h4>
                <label class="campo-label">Doenças hereditárias</label>
                <input
                    :value="modelValue.maeDoencas ?? ''" class="input-field"
                    placeholder="Ex: Hipertensão, diabetes..."
                    :disabled="readOnly"
                    @input="(e) => atualizar({ maeDoencas: (e.target as HTMLInputElement).value })"
                />
                <label class="campo-label">Descrição</label>
                <textarea
                    :value="modelValue.maeDescricao ?? ''" rows="3" class="input-field"
                    placeholder="Detalhes adicionais"
                    :disabled="readOnly"
                    @input="(e) => atualizar({ maeDescricao: (e.target as HTMLTextAreaElement).value })"
                ></textarea>
            </div>
        </div>

        <div class="subsecao">
            <div class="subsec-header">
                <span class="subsec-titulo">Outros parentes</span>
            </div>

            <div class="lista">
                <div v-for="(p, i) in parentes" :key="i" class="parente-card">
                    <div class="grade-parente">
                        <div class="campo">
                            <label>Grau de parentesco</label>
                            <select
                                :value="p.parentesco" class="input-field"
                                :disabled="readOnly"
                                @change="(e) => setParente(i, 'parentesco', (e.target as HTMLSelectElement).value)"
                            >
                                <option value="">Selecione...</option>
                                <option v-for="g in PARENTESCOS" :key="g" :value="g">{{ g }}</option>
                            </select>
                        </div>
                        <div class="campo">
                            <label>Doenças hereditárias</label>
                            <input
                                :value="p.doencas" class="input-field"
                                placeholder="Ex: Câncer, AVC..."
                                :disabled="readOnly"
                                @input="(e) => setParente(i, 'doencas', (e.target as HTMLInputElement).value)"
                            />
                        </div>
                    </div>
                    <input
                        :value="p.comentario" class="input-field"
                        placeholder="Comentário (opcional)"
                        :disabled="readOnly"
                        @input="(e) => setParente(i, 'comentario', (e.target as HTMLInputElement).value)"
                    />
                    <AppButton
                        variant="danger" size="sm" type="button"
                        :disabled="readOnly"
                        @click="removeParente(i)"
                    >Remover</AppButton>
                </div>

                <AppButton
                    size="sm" icon="fa-solid fa-plus" type="button"
                    :disabled="readOnly"
                    @click="addParente"
                >Adicionar parente</AppButton>
            </div>
        </div>

        <div class="subsecao">
            <label class="campo-label">Observações</label>
            <textarea
                :value="modelValue.observacao ?? ''" rows="3" class="input-field"
                placeholder="Outras informações relevantes da história familiar..."
                :disabled="readOnly"
                @input="(e) => atualizar({ observacao: (e.target as HTMLTextAreaElement).value })"
            ></textarea>
        </div>
    </div>
</template>

<style scoped>
.secao { display: flex; flex-direction: column; gap: 1rem; }

.grade-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.card-pai, .card-mae {
    border: 1px solid var(--border); border-radius: var(--radius);
    padding: 0.9rem 1.1rem; background: var(--bg-card);
    display: flex; flex-direction: column; gap: 0.4rem;
}
.card-titulo { font-size: 0.92em; font-weight: 700; margin: 0 0 0.25rem; color: var(--primary); }

.subsecao {
    border: 1px solid var(--border); border-radius: var(--radius);
    padding: 0.9rem 1.1rem; background: var(--bg-card);
    display: flex; flex-direction: column; gap: 0.6rem;
}
.subsec-header { display: flex; justify-content: space-between; align-items: center; }
.subsec-titulo { font-weight: 700; font-size: 0.92em; }

.lista { display: flex; flex-direction: column; gap: 0.6rem; }

.parente-card {
    border: 1px solid var(--border); border-radius: var(--radius);
    padding: 0.7rem; background: #fafafa;
    display: flex; flex-direction: column; gap: 0.4rem;
}
.grade-parente { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; }

.campo { display: flex; flex-direction: column; gap: 0.15rem; }
.campo label { font-size: 0.72em; font-weight: 600; color: var(--text-muted); }
.campo-label { font-size: 0.78em; font-weight: 600; color: var(--text-muted); margin-top: 0.25rem; }

.input-field {
    padding: 0.4rem 0.6rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.85em;
    background: var(--bg-card); color: var(--text); width: 100%; box-sizing: border-box;
}
.input-field:focus { outline: none; border-color: var(--primary); }

@media (max-width: 768px) {
    .grade-2, .grade-parente { grid-template-columns: 1fr; }
}
</style>
