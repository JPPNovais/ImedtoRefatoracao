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
import { AppField, AppInput, AppSelect, AppTextarea } from "@/components/ui"

// Carregamento lazy: o mapa corporal só baixa quando a seção é montada.
const BodyMap = defineAsyncComponent(() => import("@/components/exame-fisico/BodyMap.vue"))

/**
 * Estrutura local de uma região anatômica selecionada (mesma forma que o
 * `RegionExamCard` espera). Persistida em conteudo["exame-fisico"].regioes.
 */
export interface RegiaoAnatomicaSelecionada {
    regiao_id: string
    caminho: string
    lateralidade: "D" | "E" | "bilateral" | "misto" | null
    /** Vista anatômica resolvida (anterior/posterior/circunferencial). Derivada do nó do catálogo — não vai no payload de persistência. */
    vista?: "anterior" | "posterior" | "circunferencial" | null
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

// Aviso de plausibilidade — não bloqueia, só sinaliza valor fisiologicamente improvável (provável erro de digitação)
const avisoAntropometria = computed<string | null>(() => {
    const p = parseFloat((props.modelValue.peso ?? "").replace(",", "."))
    const h = parseFloat((props.modelValue.altura ?? "").replace(",", "."))
    if (!p || !h) return null
    const alturaM = h > 3 ? h / 100 : h   // mesma heurística cm/m do IMC
    if (p < 0.3 || p > 500)         return "Peso fora da faixa plausível — confira o valor digitado."
    if (alturaM < 0.3 || alturaM > 2.5) return "Altura fora da faixa plausível — confira se digitou em cm ou m."
    const v = p / (alturaM * alturaM)
    if (v < 10 || v > 100)          return "IMC fora da faixa plausível — confira peso e altura."
    return null
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
const erroRegioes = ref<string | null>(null)

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

/**
 * Remove o qualificador de lado ("direito"/"esquerdo") de um caminho anatômico
 * já montado — usado apenas para entradas bilaterais, onde a badge "Bilateral"
 * já comunica a lateralidade e o título deve ser neutro.
 *
 * Ex.: "Membro superior direito (anterior) > Ombro direito"
 *   →  "Membro superior (anterior) > Ombro"
 */
function caminhoNeutro(caminho: string): string {
    return caminho
        .replace(/\s+(?:direito|esquerdo)\b/gi, "")
        .replace(/\s{2,}/g, " ")
        .trim()
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

/**
 * Dado o id de um nível-1 que seja um MEMBRO, retorna o id do nível-1 do lado
 * oposto (direito↔esquerdo). Para regiões que não são membros, retorna null.
 *
 * Necessário para o destaque bilateral no BodyMap: a entrada bilateral guarda
 * o id do lado direito (R5), mas o mapa deve acender os dois membros.
 */
function getOpostoNivel1Id(n1Id: string): string | null {
    const n1 = regioesNivel1.value.find(r => r.id === n1Id)
    if (!n1) return null
    const m = MEMBRO_RE.exec(n1.nome)
    if (!m) return null
    const tipo = m[1]   // "superior" | "inferior"
    const vista = m[2]  // "anterior" | "posterior"
    // Troca direito↔esquerdo no nome para localizar o oposto no catálogo
    const nomeDireito  = `Membro ${tipo} direito (${vista})`
    const nomeEsquerdo = `Membro ${tipo} esquerdo (${vista})`
    const nomeOposto = n1.nome === nomeDireito ? nomeEsquerdo : nomeDireito
    return regioesNivel1.value.find(r => r.nome === nomeOposto)?.id ?? null
}

/**
 * Array de ids de nível-1 destinado EXCLUSIVAMENTE ao BodyMap.
 * Parte de `regioesJaSelecionadas` e, para cada entrada com
 * lateralidade === 'bilateral', acrescenta o nível-1 do lado oposto
 * (quando existir e for membro), garantindo que ambos os membros acendam.
 * `regioesJaSelecionadas` não é alterado — continua servindo o popup.
 */
const regioesExaminadasMapa = computed(() => {
    const ids = new Set(regioesJaSelecionadas.value)
    for (const r of regioes.value) {
        if (r.lateralidade !== "bilateral") continue
        const n1 = getAncestorNivel1Id(r.regiao_id)
        if (!n1) continue
        const oposto = getOpostoNivel1Id(n1)
        if (oposto) ids.add(oposto)
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

/**
 * Resolve o ancestral comum mais próximo de um conjunto de ids de região.
 *
 * Estratégia: para cada região, coleta a cadeia de ancestrais (do nível-1 até
 * ela mesma). O ancestral comum é o nó mais profundo presente em todas as
 * cadeias. Se apenas 1 região for passada, o ancestral comum é ela própria.
 * Se não houver ancestral comum (caso improvável no catálogo), usa o nível-1
 * da primeira região como fallback.
 */
function getAncestralComum(ids: string[]): string | null {
    if (ids.length === 0) return null
    if (ids.length === 1) return ids[0]

    // Monta a cadeia de ancestrais para cada id (do root até o id inclusive).
    function cadeia(regiaoId: string): string[] {
        const result: string[] = []
        let atual = catalogoRegioes.value.find(r => r.id === regiaoId)
        while (atual) {
            result.unshift(atual.id)
            atual = atual.pai_id ? catalogoRegioes.value.find(r => r.id === atual!.pai_id) : undefined
        }
        return result
    }

    const cadeias = ids.map(cadeia)
    // Itera pelos índices do menor comprimento; para no primeiro nó que diverge.
    const minLen = Math.min(...cadeias.map(c => c.length))
    let ancestral: string | null = null
    for (let i = 0; i < minLen; i++) {
        const candidato = cadeias[0][i]
        if (cadeias.every(c => c[i] === candidato)) {
            ancestral = candidato
        } else {
            break
        }
    }
    return ancestral ?? getAncestorNivel1Id(ids[0])
}

/**
 * Mapa determinístico: {base}-circunferencial → {base} (strip do sufixo).
 * Usado para resolver o nó circunferencial a partir das sub-regiões confirmadas.
 */
const SUFIXO_CIRC = '-circunferencial'

/**
 * Dado o id de um nó circunferencial (ex.: 'msd-circunferencial'),
 * retorna o nome canônico do nó buscando no catálogo.
 */
function getNomeCircunferencial(idCirc: string): string {
    const no = catalogoRegioes.value.find(r => r.id === idCirc)
    return no?.nome ?? idCirc
}

function onConfirmarRegioes(
    selecoes: Array<{ regiaoId: string; lateralidade: "D" | "E" | "bilateral" | null; vista?: "anterior" | "posterior" | "circunferencial" | null }>,
) {
    // Defensive: confirmação sem seleções não cria card (R1/UX).
    if (selecoes.length === 0) return

    // Dedupe intra-confirmação: mesma sub-parte (regiaoId + lateralidade) entra
    // uma única vez no agregado (R5 intra-confirmação).
    const vistas = new Set<string>()
    const unicas = selecoes.filter(sel => {
        const chave = `${sel.regiaoId}|${sel.lateralidade ?? ""}`
        if (vistas.has(chave)) return false
        vistas.add(chave)
        return true
    })

    // Vista resolvida: todas as seleções da confirmação têm a mesma vista (vem do popup).
    const vistaResolvida: "anterior" | "posterior" | "circunferencial" | null = unicas[0]?.vista ?? null

    // R2 — texto_exame: concatenação dos templates, uma por linha, texto puro.
    const textoExame = unicas.map(sel => getTemplate(sel.regiaoId)).join("\n")

    let regiaoId: string
    let caminho: string

    if (vistaResolvida === "circunferencial") {
        // R5 — modo circunferencial: regiao_id = {base}-circunferencial.
        // Deriva o nó circunferencial a partir do ancestral nível-1 das sub-regiões.
        // O ancestral nível-1 de uma sub-região anterior (ex.: 'torax-anterior') nos dá
        // o base; trocamos o sufixo '-anterior'/'-posterior' por '-circunferencial'.
        const n1Id = getAncestorNivel1Id(unicas[0].regiaoId)
        const base = n1Id
            ? n1Id.replace(/-anterior$/, '').replace(/-posterior$/, '')
            : unicas[0].regiaoId.replace(/-anterior$/, '').replace(/-posterior$/, '')
        regiaoId = `${base}${SUFIXO_CIRC}`
        // Caminho: nome do nó circunferencial do catálogo (ex.: "Tórax (circunferencial)")
        caminho = getNomeCircunferencial(regiaoId)
    } else {
        // R3 — regiao_id do card = ancestral comum mais profundo das sub-partes.
        regiaoId = getAncestralComum(unicas.map(s => s.regiaoId)) ?? unicas[0].regiaoId
        // Caminho do card: neutralizado quando bilateral ou misto (definido abaixo).
        caminho = getCaminho(regiaoId)
    }

    // R6 — lateralidade do card resolvida das sub-partes.
    const lats = unicas.map(s => s.lateralidade)
    let lateralidade: RegiaoAnatomicaSelecionada["lateralidade"]
    if (lats.every(l => l === null)) {
        lateralidade = null
    } else if (lats.every(l => l === "D")) {
        lateralidade = "D"
    } else if (lats.every(l => l === "E")) {
        lateralidade = "E"
    } else if (lats.every(l => l === "bilateral")) {
        lateralidade = "bilateral"
    } else {
        lateralidade = "misto"
    }

    // Neutraliza caminho quando bilateral ou misto (não-circunferencial).
    if (vistaResolvida !== "circunferencial" && (lateralidade === "bilateral" || lateralidade === "misto")) {
        caminho = caminhoNeutro(caminho)
    }
    // Circunferencial bilateral: caminho já está neutro pelo nome do nó (sem "direito/esquerdo")
    if (vistaResolvida === "circunferencial" && (lateralidade === "bilateral" || lateralidade === "misto")) {
        caminho = caminhoNeutro(caminho)
    }

    substituirRegioes([
        ...regioes.value,
        {
            regiao_id: regiaoId,
            caminho,
            lateralidade,
            vista: vistaResolvida,
            texto_exame: textoExame,
            achados: "",
            observacoes: "",
            timestamp: new Date().toISOString(),
        },
    ])
}

function removerRegiao(index: number) {
    const novas = [...regioes.value]
    novas.splice(index, 1)
    substituirRegioes(novas)
}

/**
 * Aplica um patch parcial em uma região examinada — emitido pelo
 * RegionExamCard a cada alteração de campo (texto_exame/achados/observacoes).
 * Mantemos imutabilidade no array para o v-model do pai detectar a mudança
 * mesmo se ele clonar raso o modelValue.
 */
function atualizarRegiao({ index, patch }: { index: number; patch: Partial<RegiaoAnatomicaSelecionada> }) {
    if (index < 0 || index >= regioes.value.length) return
    const novas = [...regioes.value]
    novas[index] = { ...novas[index], ...patch }
    substituirRegioes(novas)
}

// Carrega catálogo de regiões 1× quando a seção monta. Falha silenciosa — sem
// rede, o mapa fica sem regiões clicáveis mas os campos textuais continuam.
onMounted(async () => {
    if (props.readOnly) return
    carregandoRegioes.value = true
    erroRegioes.value = null
    try {
        catalogoRegioes.value = await exameFisicoService.listarRegioes(undefined, true)
        if (catalogoRegioes.value.length === 0) {
            erroRegioes.value = "Catálogo de regiões anatômicas vazio."
        }
    } catch (err) {
        // Engolir em silêncio escondia falha de API/auth e deixava o mapa sem hotspots
        // clicáveis (paths só renderizam quando o catálogo carrega). Logar + sinalizar
        // visualmente alinha com o comportamento do legado (useExameFisico.loadRegioes).
        console.error("[exame-fisico] falha ao carregar catálogo de regiões:", err)
        erroRegioes.value = "Não foi possível carregar as regiões anatômicas."
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
                <AppField label="Pressão arterial (mmHg)" label-variant="compact">
                    <div class="pa-row">
                        <AppInput
                            :model-value="modelValue.paSistolica ?? ''" type="number"
                            placeholder="120" :disabled="readOnly" class="text-center"
                            @update:model-value="v => atualizar({ paSistolica: String(v) })"
                        />
                        <span class="pa-sep">/</span>
                        <AppInput
                            :model-value="modelValue.paDiastolica ?? ''" type="number"
                            placeholder="80" :disabled="readOnly" class="text-center"
                            @update:model-value="v => atualizar({ paDiastolica: String(v) })"
                        />
                    </div>
                </AppField>
                <AppField label="FC (bpm)" label-variant="compact">
                    <AppInput
                        :model-value="modelValue.fc ?? ''" type="number"
                        placeholder="72" :disabled="readOnly"
                        @update:model-value="v => atualizar({ fc: String(v) })"
                    />
                </AppField>
                <AppField label="FR (irpm)" label-variant="compact">
                    <AppInput
                        :model-value="modelValue.fr ?? ''" type="number"
                        placeholder="16" :disabled="readOnly"
                        @update:model-value="v => atualizar({ fr: String(v) })"
                    />
                </AppField>
                <AppField label="Temp. (°C)" label-variant="compact">
                    <AppInput
                        :model-value="modelValue.temperatura ?? ''" type="number" step="0.1"
                        placeholder="36.5" :disabled="readOnly"
                        @update:model-value="v => atualizar({ temperatura: String(v) })"
                    />
                </AppField>
                <AppField label="SpO₂ (%)" label-variant="compact">
                    <AppInput
                        :model-value="modelValue.spo2 ?? ''" type="number" min="0" max="100"
                        placeholder="98" :disabled="readOnly"
                        @update:model-value="v => atualizar({ spo2: String(v) })"
                    />
                </AppField>
                <AppField label="Glicemia (mg/dL)" label-variant="compact">
                    <AppInput
                        :model-value="modelValue.glicemia ?? ''" type="number"
                        placeholder="95" :disabled="readOnly"
                        @update:model-value="v => atualizar({ glicemia: String(v) })"
                    />
                </AppField>
            </div>
        </div>

        <!-- Antropometria -->
        <div class="subsecao">
            <h4 class="subsec-titulo">Antropometria</h4>
            <div class="grade-antro">
                <AppField label="Peso (kg)" label-variant="compact">
                    <AppInput
                        :model-value="modelValue.peso ?? ''" type="number" step="0.1"
                        placeholder="70.5" :disabled="readOnly"
                        @update:model-value="v => atualizar({ peso: String(v) })"
                    />
                </AppField>
                <AppField label="Altura (cm ou m)" label-variant="compact">
                    <AppInput
                        :model-value="modelValue.altura ?? ''" type="number" step="0.01"
                        placeholder="170 ou 1.70" :disabled="readOnly"
                        @update:model-value="v => atualizar({ altura: String(v) })"
                    />
                </AppField>
                <AppField label="IMC (calculado)" label-variant="compact">
                    <AppInput :model-value="imc ?? ''" readonly />
                </AppField>
                <AppField label="Classificação" label-variant="compact">
                    <AppInput :model-value="classIMC ?? ''" readonly />
                </AppField>
            </div>
            <p v-if="avisoAntropometria" class="aviso-antro">
                <i class="fa-solid fa-triangle-exclamation"></i>
                {{ avisoAntropometria }}
            </p>
        </div>

        <!-- Ectoscopia -->
        <div class="subsecao">
            <h4 class="subsec-titulo">Ectoscopia (inspeção geral)</h4>
            <div class="grade-ecto">
                <AppField label="Estado geral" label-variant="compact">
                    <AppSelect
                        :model-value="modelValue.estadoGeral ?? ''" :disabled="readOnly"
                        @update:model-value="v => atualizar({ estadoGeral: String(v) })"
                    >
                        <option value="">—</option>
                        <option v-for="o in ESTADO_GERAL" :key="o" :value="o">{{ o }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Consciência" label-variant="compact">
                    <AppSelect
                        :model-value="modelValue.consciencia ?? ''" :disabled="readOnly"
                        @update:model-value="v => atualizar({ consciencia: String(v) })"
                    >
                        <option value="">—</option>
                        <option v-for="o in CONSCIENCIA" :key="o" :value="o">{{ o }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Estado nutricional" label-variant="compact">
                    <AppSelect
                        :model-value="modelValue.estadoNutricional ?? ''" :disabled="readOnly"
                        @update:model-value="v => atualizar({ estadoNutricional: String(v) })"
                    >
                        <option value="">—</option>
                        <option v-for="o in ESTADO_NUTR" :key="o" :value="o">{{ o }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Coloração" label-variant="compact">
                    <AppSelect
                        :model-value="modelValue.coloracao ?? ''" :disabled="readOnly"
                        @update:model-value="v => atualizar({ coloracao: String(v) })"
                    >
                        <option value="">—</option>
                        <option v-for="o in COLORACAO" :key="o" :value="o">{{ o }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Hidratação" label-variant="compact">
                    <AppSelect
                        :model-value="modelValue.hidratacao ?? ''" :disabled="readOnly"
                        @update:model-value="v => atualizar({ hidratacao: String(v) })"
                    >
                        <option value="">—</option>
                        <option v-for="o in HIDRATACAO" :key="o" :value="o">{{ o }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Cianose" label-variant="compact">
                    <AppSelect
                        :model-value="modelValue.cianose ?? ''" :disabled="readOnly"
                        @update:model-value="v => atualizar({ cianose: String(v) })"
                    >
                        <option value="">—</option>
                        <option v-for="o in CIANOSE" :key="o" :value="o">{{ o }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Icterícia" label-variant="compact">
                    <AppSelect
                        :model-value="modelValue.ictericia ?? ''" :disabled="readOnly"
                        @update:model-value="v => atualizar({ ictericia: String(v) })"
                    >
                        <option value="">—</option>
                        <option v-for="o in ICTERICIA" :key="o" :value="o">{{ o }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Temp. corporal" label-variant="compact">
                    <AppSelect
                        :model-value="modelValue.tempCorporal ?? ''" :disabled="readOnly"
                        @update:model-value="v => atualizar({ tempCorporal: String(v) })"
                    >
                        <option value="">—</option>
                        <option v-for="o in TEMP_CORPORAL" :key="o" :value="o">{{ o }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Batimentos" label-variant="compact">
                    <AppSelect
                        :model-value="modelValue.batimentos ?? ''" :disabled="readOnly"
                        @update:model-value="v => atualizar({ batimentos: String(v) })"
                    >
                        <option value="">—</option>
                        <option v-for="o in BATIMENTOS" :key="o" :value="o">{{ o }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Respiração" label-variant="compact">
                    <AppSelect
                        :model-value="modelValue.respiracao ?? ''" :disabled="readOnly"
                        @update:model-value="v => atualizar({ respiracao: String(v) })"
                    >
                        <option value="">—</option>
                        <option v-for="o in RESPIRACAO" :key="o" :value="o">{{ o }}</option>
                    </AppSelect>
                </AppField>
            </div>
            <AppField label="Descrição da ectoscopia" label-variant="compact" class="ecto-descricao">
                <AppTextarea
                    :model-value="modelValue.descricaoEctoscopia ?? ''" :rows="3"
                    placeholder="Observações adicionais sobre a inspeção geral..."
                    :disabled="readOnly"
                    @update:model-value="v => atualizar({ descricaoEctoscopia: String(v) })"
                />
            </AppField>
        </div>

        <!-- Mapa corporal -->
        <div v-if="!readOnly" class="subsecao">
            <h4 class="subsec-titulo">
                Mapa corporal
                <span v-if="carregandoRegioes" class="hint">carregando regiões...</span>
                <span v-else-if="erroRegioes" class="hint hint-erro">{{ erroRegioes }} Recarregue a página.</span>
                <span v-else class="hint">clique em uma região para examinar</span>
            </h4>
            <div class="mapa-container">
                <BodyMap
                    :regioes="regioesNivel1"
                    :regioes-examinadas="regioesExaminadasMapa"
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
                :key="`${regiao.regiao_id}-${regiao.lateralidade ?? ''}-${idx}`"
                :regiao="regiao"
                :index="idx"
                :open="true"
                @remover="removerRegiao"
                @atualizar="atualizarRegiao"
            />
        </div>

        <!-- Observações gerais do exame físico -->
        <div v-if="!readOnly" class="subsecao">
            <h4 class="subsec-titulo">Observações gerais do exame físico</h4>
            <AppTextarea
                :model-value="modelValue.observacoesExame ?? ''" :rows="3"
                placeholder="Observações adicionais sobre o exame físico..."
                :disabled="readOnly"
                @update:model-value="v => atualizar({ observacoesExame: String(v) })"
            />
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
/*
    Layout flat: a seção já vive dentro do card "module" do ProntuárioView,
    então sub-blocos não usam mais border/background próprios (evita
    "card dentro de card"). Separação entre eles é apenas um divisor leve
    no topo, igual ao espaçamento das seções textuais (QP, HDA).
*/
.secao { display: flex; flex-direction: column; gap: 1.25rem; }

.subsecao {
    display: flex; flex-direction: column; gap: 0.6rem;
    padding-top: 1.25rem;
    border-top: 1px solid var(--border);
}
.subsecao:first-child {
    padding-top: 0;
    border-top: none;
}

.subsec-titulo {
    font-weight: 700; font-size: 0.9em; color: var(--primary); margin: 0 0 0.2rem;
    display: flex; align-items: center; gap: 8px;
}
.subsec-titulo .hint {
    font-weight: 500; font-size: 0.82em; color: var(--text-muted);
}
.subsec-titulo .hint-erro {
    color: hsl(var(--destructive, 0 84% 50%));
}

.grade-sv    { display: grid; grid-template-columns: 1.5fr repeat(5, 1fr); gap: 0.6rem; align-items: end; }
.grade-antro { display: grid; grid-template-columns: 1fr 1fr 1fr 1.5fr; gap: 0.6rem; align-items: end; }

.aviso-antro {
    display: flex; align-items: center; gap: 0.45rem;
    margin: 0.5rem 0 0;
    font-size: 0.8rem; font-weight: 600;
    color: hsl(38 80% 30%);
    background: hsl(var(--warning) / 0.12);
    border: 1px solid hsl(var(--warning) / 0.35);
    border-radius: 0.375rem;
    padding: 0.4rem 0.6rem;
}
.aviso-antro i { color: hsl(var(--warning)); }
.grade-ecto  { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 0.6rem; align-items: end; }

.pa-row { display: flex; align-items: center; gap: 0.35rem; }
.pa-sep { font-weight: 700; color: var(--text-muted); }

.ecto-descricao { margin-top: 0.25rem; }

.mapa-container { display: flex; justify-content: center; padding: 0.25rem 0; }

@media (max-width: 900px) {
    .grade-sv, .grade-antro { grid-template-columns: 1fr 1fr; }
}
</style>
