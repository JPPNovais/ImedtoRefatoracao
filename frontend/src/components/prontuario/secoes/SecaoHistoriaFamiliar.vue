<!-- História familiar: pai, mãe, parentes com doenças hereditárias.
     Briefing 2026-06-05_001: campo "parentesco" usa AppAutocompleteCriavel
     (tipo RelacaoFamiliar), substituindo o AppSelect fixo. Label atualizado
     para "Relação familiar". Campos doencas/comentario permanecem livres.
-->
<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { AppButton, AppInput, AppTextarea, AppAutocompleteCriavel } from "@/components/ui"
import { variavelPoolService } from "@/services/variavelPoolService"

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

// ── Pool de variáveis RelacaoFamiliar ─────────────────────────────────────────
const opcoesParentesco  = ref<string[]>([])
const poolCarregando    = ref(true)
const poolErro          = ref(false)

onMounted(async () => {
    try {
        const items = await variavelPoolService.listar("RelacaoFamiliar")
        opcoesParentesco.value = items.map(i => i.nome)
    } catch {
        poolErro.value = true
        // CA11: degrada para input puro — não bloqueia o preenchimento.
    } finally {
        poolCarregando.value = false
    }
})
</script>

<template>
    <div class="secao">
        <div class="grade-2">
            <div class="card-pai">
                <h4 class="card-titulo">Pai</h4>
                <label class="campo-label">Doenças hereditárias</label>
                <AppInput
                    :model-value="modelValue.paiDoencas ?? ''"
                    placeholder="Ex: Hipertensão, diabetes..."
                    :disabled="readOnly"
                    @update:model-value="(v) => atualizar({ paiDoencas: String(v) })"
                />
                <label class="campo-label">Descrição</label>
                <AppTextarea
                    :model-value="modelValue.paiDescricao ?? ''" :rows="3"
                    placeholder="Detalhes adicionais"
                    :disabled="readOnly"
                    @update:model-value="(v) => atualizar({ paiDescricao: String(v) })"
                />
            </div>

            <div class="card-mae">
                <h4 class="card-titulo">Mãe</h4>
                <label class="campo-label">Doenças hereditárias</label>
                <AppInput
                    :model-value="modelValue.maeDoencas ?? ''"
                    placeholder="Ex: Hipertensão, diabetes..."
                    :disabled="readOnly"
                    @update:model-value="(v) => atualizar({ maeDoencas: String(v) })"
                />
                <label class="campo-label">Descrição</label>
                <AppTextarea
                    :model-value="modelValue.maeDescricao ?? ''" :rows="3"
                    placeholder="Detalhes adicionais"
                    :disabled="readOnly"
                    @update:model-value="(v) => atualizar({ maeDescricao: String(v) })"
                />
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
                            <label>Relação familiar</label>
                            <AppAutocompleteCriavel
                                :model-value="p.parentesco"
                                :opcoes="opcoesParentesco"
                                placeholder="Ex: Irmão(ã), Primo(a)..."
                                :disabled="readOnly"
                                :carregando="poolCarregando"
                                :erro="poolErro"
                                @update:model-value="(v) => setParente(i, 'parentesco', v)"
                            />
                        </div>
                        <div class="campo">
                            <label>Doenças hereditárias</label>
                            <AppInput
                                :model-value="p.doencas"
                                placeholder="Ex: Câncer, AVC..."
                                :disabled="readOnly"
                                @update:model-value="(v) => setParente(i, 'doencas', String(v))"
                            />
                        </div>
                    </div>
                    <AppInput
                        :model-value="p.comentario"
                        placeholder="Comentário (opcional)"
                        :disabled="readOnly"
                        @update:model-value="(v) => setParente(i, 'comentario', String(v))"
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
            <AppTextarea
                :model-value="modelValue.observacao ?? ''" :rows="3"
                placeholder="Outras informações relevantes da história familiar..."
                :disabled="readOnly"
                @update:model-value="(v) => atualizar({ observacao: String(v) })"
            />
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

@media (max-width: 768px) {
    .grade-2, .grade-parente { grid-template-columns: 1fr; }
}
</style>
