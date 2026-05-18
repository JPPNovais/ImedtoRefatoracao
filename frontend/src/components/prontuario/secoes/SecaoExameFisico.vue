<!--
  Exame físico estruturado dentro da evolução. Combina:

  1. Sinais vitais + antropometria + ectoscopia (campos clássicos, salvos no JSON
     da evolução em conteudo["exame-fisico"]).
  2. Mapa corporal interativo + regiões anatômicas com lateralidade
     (salvos no domínio dedicado `exame_fisicos` via
     POST /api/evolucoes/{id}/exame-fisico, após a evolução ser registrada).

  A migração unificou aqui o que antes vivia em duas telas: a seção textual
  da consulta e a aba "Exame físico" dedicada. O PO pediu para que tudo conviva
  na mesma área.

  v-model retorna { ...dadosClassicos, regioes: RegiaoAnatomicaSelecionada[] }.
  ProntuarioView usa `regioes` para encadear `exameFisicoService.registrar`.
-->
<script setup lang="ts">
import { computed, defineAsyncComponent, onMounted, ref } from "vue"
import {
    exameFisicoService,
    type ExameFisicoRegiao,
} from "@/services/exameFisicoService"
import RegionSelectorPopup from "@/components/exame-fisico/RegionSelectorPopup.vue"
import RegionExamCard from "@/components/exame-fisico/RegionExamCard.vue"

// Carregamento lazy: o mapa corporal só baixa quando a seção é montada.
const BodyMap = defineAsyncComponent(() => import("@/components/exame-fisico/BodyMap.vue"))

/**
 * Estrutura local de uma região anatômica selecionada (mesma forma que o
 * `RegionExamCard` espera). Persistida em conteudo["exame-fisico"].regioes.
 */
export interface RegiaoAnatomicaSelecionada {
    regiao_id: string
    caminho: string
    lateralidade: "D" | "E" | "bilateral" | null
    texto_exame: string
    achados: string
    observacoes: string
    timestamp: string
}

interface EfData {
    // ── Sinais vitais ────────────────────────────────────────────────────────
    paSistolica?: string
    paDiastolica?: string
    fc?: string
    fr?: string
    temperatura?: string
    spo2?: string
    glicemia?: string
    // ── Antropometria ────────────────────────────────────────────────────────
    peso?: string
    altura?: string
    // ── Ectoscopia ───────────────────────────────────────────────────────────
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
    // ── Regiões anatômicas (mapa corporal) ───────────────────────────────────
    regioes?: RegiaoAnatomicaSelecionada[]
    // Observações gerais do exame físico (textarea livre abaixo do mapa).
    observacoesExame?: string
}

const props = defineProps<{
    modelValue: EfData
    readOnly?: boolean
    /** Sexo do paciente (M/F) — controla qual silhueta o BodyMap renderiza. */
    pacienteSexo?: string | null
}>()
const emit  = defineEmits<{ "update:modelValue": [v: EfData] }>()

function atualizar(patch: Partial<EfData>) {
    emit("update:modelValue", { ...props.modelValue, ...patch })
}

// Helpers de array de regiões — mutam por substituição (v-model imutável).
const regioes = computed<RegiaoAnatomicaSelecionada[]>(() => props.modelValue.regioes ?? [])

function substituirRegioes(novas: RegiaoAnatomicaSelecionada[]) {
    atualizar({ regioes: novas })
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

// ─── Mapa corporal + regiões anatômicas ───────────────────────────────────────

const catalogoRegioes = ref<ExameFisicoRegiao[]>([])
const carregandoRegioes = ref(false)

const regioesNivel1 = computed(() => catalogoRegioes.value.filter(r => r.nivel === 1))

function getFilhos(regiaoId: string): ExameFisicoRegiao[] {
    return catalogoRegioes.value
        .filter(r => r.pai_id === regiaoId)
        .sort((a, b) => a.ordem - b.ordem)
}

function getCaminho(regiaoId: string): string {
    const partes: string[] = []
    let atual: ExameFisicoRegiao | undefined = catalogoRegioes.value.find(r => r.id === regiaoId)
    while (atual) {
        partes.unshift(atual.nome)
        atual = atual.pai_id ? catalogoRegioes.value.find(r => r.id === atual!.pai_id) : undefined
    }
    return partes.join(" > ")
}

function getTemplate(regiaoId: string): string {
    const r = catalogoRegioes.value.find(x => x.id === regiaoId)
    return r?.template_texto || "Inspeção: ___. Palpação: ___. Achados: ___."
}

function getAncestorNivel1Id(regiaoId: string): string | null {
    let atual = catalogoRegioes.value.find(r => r.id === regiaoId)
    while (atual) {
        if (atual.nivel === 1) return atual.id
        atual = atual.pai_id ? catalogoRegioes.value.find(r => r.id === atual!.pai_id) : undefined
    }
    return null
}

const regioesJaSelecionadas = computed(() => {
    const ids = new Set<string>()
    for (const r of regioes.value) {
        ids.add(r.regiao_id)
        const n1 = getAncestorNivel1Id(r.regiao_id)
        if (n1) ids.add(n1)
    }
    return Array.from(ids)
})

// Estado do popup
const selectorAberto = ref(false)
const regiaoClicada = ref<ExameFisicoRegiao | null>(null)
const membroRegioes = ref<{
    tipo: "superior" | "inferior"
    dirBase: ExameFisicoRegiao | null
    esquBase: ExameFisicoRegiao | null
} | null>(null)

const MEMBRO_RE = /^Membro (superior|inferior) (?:direito|esquerdo) \((anterior|posterior)\)$/i

/**
 * O <c>BodyMap</c> declara seu próprio <c>ExameFisicoRegiao</c> (subset dos
 * campos do service, sem <c>ordem</c>/<c>ativo</c>). Aceitamos o subset e
 * achamos a região completa no catálogo carregado — assim o handler funciona
 * sem precisar duplicar tipos.
 */
type RegiaoMapaSubset = Pick<ExameFisicoRegiao, "id" | "nome" | "nivel" | "lateralidade" | "pai_id" | "vista" | "template_texto">

function onRegiaoClicada(regiao: RegiaoMapaSubset) {
    // Resolve para a entidade completa do catálogo (com ordem/ativo). O BodyMap
    // só conhece o subset declarado nele — aqui buscamos a versão rica que
    // o RegionSelectorPopup espera.
    const completa = catalogoRegioes.value.find(r => r.id === regiao.id) ?? null
    const m = MEMBRO_RE.exec(regiao.nome)
    if (m) {
        const tipo = m[1] as "superior" | "inferior"
        const vista = m[2]
        const base = `Membro ${tipo}`
        const dirBase = regioesNivel1.value.find(r => r.nome === `${base} direito (${vista})`) ?? null
        const esquBase = regioesNivel1.value.find(r => r.nome === `${base} esquerdo (${vista})`) ?? null
        membroRegioes.value = { tipo, dirBase, esquBase }
        regiaoClicada.value = dirBase
    } else {
        membroRegioes.value = null
        regiaoClicada.value = completa
    }
    selectorAberto.value = true
}

function onConfirmarRegioes(
    selecoes: Array<{ regiaoId: string; lateralidade: "D" | "E" | "bilateral" | null }>,
) {
    const novas = [...regioes.value]
    for (const sel of selecoes) {
        const jaExiste = novas.some(r => r.regiao_id === sel.regiaoId && r.lateralidade === sel.lateralidade)
        if (jaExiste) continue
        novas.push({
            regiao_id: sel.regiaoId,
            caminho: getCaminho(sel.regiaoId),
            lateralidade: sel.lateralidade,
            texto_exame: getTemplate(sel.regiaoId),
            achados: "",
            observacoes: "",
            timestamp: new Date().toISOString(),
        })
    }
    substituirRegioes(novas)
}

function removerRegiao(index: number) {
    const novas = [...regioes.value]
    novas.splice(index, 1)
    substituirRegioes(novas)
}

// Carrega catálogo de regiões 1× quando a seção monta. Falha silenciosa — sem
// rede, o mapa fica sem regiões clicáveis mas os campos textuais continuam.
onMounted(async () => {
    if (props.readOnly) return
    carregandoRegioes.value = true
    try {
        catalogoRegioes.value = await exameFisicoService.listarRegioes(undefined, true)
    } catch {
        // ignora — campos textuais continuam funcionando
    } finally {
        carregandoRegioes.value = false
    }
})
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

        <!-- Mapa corporal -->
        <div v-if="!readOnly" class="subsecao mapa-section">
            <h4 class="subsec-titulo">
                Mapa corporal
                <span v-if="carregandoRegioes" class="hint">carregando regiões...</span>
                <span v-else class="hint">clique em uma região para examinar</span>
            </h4>
            <div class="mapa-container">
                <BodyMap
                    :regioes="regioesNivel1"
                    :regioes-examinadas="regioesJaSelecionadas"
                    :sexo="pacienteSexo"
                    @regiao-clicada="onRegiaoClicada"
                />
            </div>
        </div>

        <!-- Regiões anatômicas selecionadas -->
        <div v-if="!readOnly && regioes.length > 0" class="subsecao">
            <h4 class="subsec-titulo">Regiões examinadas ({{ regioes.length }})</h4>
            <RegionExamCard
                v-for="(regiao, idx) in regioes"
                :key="idx"
                :regiao="regiao"
                :index="idx"
                :open="true"
                @remover="removerRegiao"
            />
        </div>

        <!-- Observações gerais do exame físico -->
        <div v-if="!readOnly" class="subsecao">
            <h4 class="subsec-titulo">Observações gerais do exame físico</h4>
            <textarea
                :value="modelValue.observacoesExame ?? ''" rows="3" class="input-field"
                placeholder="Observações adicionais sobre o exame físico..."
                :disabled="readOnly"
                @input="(e) => atualizar({ observacoesExame: (e.target as HTMLTextAreaElement).value })"
            ></textarea>
        </div>

        <!-- Popup de seleção de sub-regiões -->
        <RegionSelectorPopup
            v-if="!readOnly"
            v-model:aberto="selectorAberto"
            :regiao-clicada="regiaoClicada"
            :regioes="catalogoRegioes"
            :regioes-ja-selecionadas="regioesJaSelecionadas"
            :get-filhos="getFilhos"
            :membro-regioes="membroRegioes"
            @confirmar="onConfirmarRegioes"
        />
    </div>
</template>

<style scoped>
.secao { display: flex; flex-direction: column; gap: 1rem; }

.subsecao {
    border: 1px solid var(--border); border-radius: var(--radius);
    padding: 0.9rem 1.1rem; background: var(--bg-card);
    display: flex; flex-direction: column; gap: 0.6rem;
}
.subsec-titulo {
    font-weight: 700; font-size: 0.9em; color: var(--primary); margin: 0 0 0.2rem;
    display: flex; align-items: center; gap: 8px;
}
.subsec-titulo .hint {
    font-weight: 500; font-size: 0.82em; color: var(--text-muted);
}

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

.mapa-section { padding: 1rem 1.1rem; }
.mapa-container { display: flex; justify-content: center; padding: 0.5rem 0; }

@media (max-width: 900px) {
    .grade-sv, .grade-antro { grid-template-columns: 1fr 1fr; }
}
</style>
