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
import { computed, defineAsyncComponent, onMounted, reactive, ref } from "vue"
import {
    exameFisicoService,
    type ExameFisicoRegiao,
} from "@/services/exameFisicoService"
import RegionSelectorPopup from "@/components/exame-fisico/RegionSelectorPopup.vue"
import RegionExamCard from "@/components/exame-fisico/RegionExamCard.vue"
import { AppField, AppInput, AppInputDecimal, AppSelect, AppTextarea } from "@/components/ui"
import { RAMOS_CIRCUNFERENCIAL } from "@/components/exame-fisico/regioesCircunferenciais"
import type { VistaHotspot } from "@/components/exame-fisico/BodyMap.vue"

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

// ─── Estado de colapso das subseções superiores ────────────────────────────
// Estado padrão: fechado. O médico abre quando for usar.
const expandido = reactive({ sinaisVitais: false, antropometria: false, ectoscopia: false })

// Indicadores de "tem dado preenchido" — para o dot de status no header colapsado.
const sinaisVitaisPreenchido = computed(() => {
    const d = props.modelValue
    return !!(d.paSistolica || d.paDiastolica || d.fc || d.fr || d.temperatura || d.spo2 || d.glicemia)
})
const antropometriaPreenchida = computed(() => {
    const d = props.modelValue
    return !!(d.peso || d.altura)
})
const ectoscopiaPreenchida = computed(() => {
    const d = props.modelValue
    return !!(
        d.estadoGeral || d.consciencia || d.estadoNutricional || d.coloracao ||
        d.hidratacao || d.cianose || d.ictericia || d.tempCorporal ||
        d.batimentos || d.respiracao || d.descricaoEctoscopia
    )
})

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
    const vista = m[2]  // "anterior" | "posterior" | "circunferencial"
    // Troca direito↔esquerdo no nome para localizar o oposto no catálogo
    const nomeDireito  = `Membro ${tipo} direito (${vista})`
    const nomeEsquerdo = `Membro ${tipo} esquerdo (${vista})`
    const nomeOposto = n1.nome === nomeDireito ? nomeEsquerdo : nomeDireito
    return regioesNivel1.value.find(r => r.nome === nomeOposto)?.id ?? null
}

/**
 * Array de ids de nível-1 destinado EXCLUSIVAMENTE ao BodyMap.
 *
 * Expansão aditiva (via Set) — preserva o espelhamento bilateral de membro existente e
 * acrescenta, para cards circunferenciais, ambos os ramos (anterior + posterior) via
 * RAMOS_CIRCUNFERENCIAL (incluindo a exceção abdome↔lombossacra).
 *
 * O BodyMap usa esse array para:
 * - Acender hotspots de catálogo (cabeça, pescoço, membros) por id.
 * - Decidir se o polígono "Tronco (anterior/posterior)" acende via PARTE_PARA_TRONCO
 *   (lógica "OU das partes" implementada no BodyMap).
 */
const regioesExaminadasMapa = computed(() => {
    const ids = new Set(regioesJaSelecionadas.value)
    for (const r of regioes.value) {
        // (existente) espelhamento bilateral de membro
        if (r.lateralidade === "bilateral") {
            const n1 = getAncestorNivel1Id(r.regiao_id)
            if (n1) {
                const oposto = getOpostoNivel1Id(n1)
                if (oposto) {
                    ids.add(oposto)
                    // Se o oposto for circunferencial, expande também seus ramos
                    const ramosOposto = RAMOS_CIRCUNFERENCIAL[oposto]
                    if (ramosOposto) {
                        ids.add(ramosOposto.anterior)
                        ids.add(ramosOposto.posterior)
                    }
                }
            }
        }
        // (novo B2) circunferencial → acende anterior E posterior (incl. abdome↔lombossacra)
        const ramos = RAMOS_CIRCUNFERENCIAL[r.regiao_id]
        if (ramos) {
            ids.add(ramos.anterior)
            ids.add(ramos.posterior)
        }
    }
    return Array.from(ids)
})

/**
 * Mapa id-de-nível-1 → vista resolvida para coloração dos hotspots no BodyMap.
 *
 * Construído em paralelo ao regioesExaminadasMapa (mesma lógica de expansão),
 * aplicando a precedência R4: circunferencial > posterior > anterior.
 *
 * Prop nova e opcional — não quebra usos legados de BodyMap nem os testes existentes.
 */
const PRIORIDADE_VISTA: Record<VistaHotspot, number> = {
    circunferencial: 3,
    posterior:       2,
    anterior:        1,
}

function resolverVistaId(mapa: Map<string, VistaHotspot>, id: string, novaVista: VistaHotspot) {
    const atual = mapa.get(id)
    if (!atual || PRIORIDADE_VISTA[novaVista] > PRIORIDADE_VISTA[atual]) {
        mapa.set(id, novaVista)
    }
}

const vistasPorIdMapa = computed<Record<string, VistaHotspot>>(() => {
    const mapa = new Map<string, VistaHotspot>()

    for (const r of regioes.value) {
        const vistaResolvida = (r.vista ?? null) as VistaHotspot | null
        if (!vistaResolvida) continue

        // Vista canônica para a regiao_id e seu ancestral nível-1
        const n1 = getAncestorNivel1Id(r.regiao_id)
        if (n1) resolverVistaId(mapa, n1, vistaResolvida)

        // Espelhamento bilateral de membro: id oposto herda a mesma vista
        if (r.lateralidade === "bilateral" && n1) {
            const oposto = getOpostoNivel1Id(n1)
            if (oposto) {
                resolverVistaId(mapa, oposto, vistaResolvida)
                // Se o oposto for circunferencial, expande seus ramos com a mesma vista
                const ramosOposto = RAMOS_CIRCUNFERENCIAL[oposto]
                if (ramosOposto) {
                    resolverVistaId(mapa, ramosOposto.anterior, vistaResolvida)
                    resolverVistaId(mapa, ramosOposto.posterior, vistaResolvida)
                }
            }
        }

        // Circunferencial → expande ramos anterior+posterior com vista circunferencial
        const ramos = RAMOS_CIRCUNFERENCIAL[r.regiao_id]
        if (ramos) {
            resolverVistaId(mapa, ramos.anterior, 'circunferencial')
            resolverVistaId(mapa, ramos.posterior, 'circunferencial')
        }
    }

    // Fusão estrutural do tronco (briefing 2026-06-25_002):
    // tronco-anterior/tronco-posterior são regiões reais — vistasPorId já os inclui
    // diretamente via regiao_id quando o profissional examina o tronco.
    // O bloco de propagação PARTE_PARA_TRONCO foi removido (não há mais partes sintéticas).

    return Object.fromEntries(mapa)
})

// Estado do popup
const selectorAberto = ref(false)
const regiaoClicada = ref<ExameFisicoRegiao | null>(null)
const membroRegioes = ref<{
    tipo: "superior" | "inferior"
    dirBase: ExameFisicoRegiao | null
    esquBase: ExameFisicoRegiao | null
} | null>(null)
/** Vista pré-selecionada ao abrir (M3 híbrido — mantém 3 opções editáveis). Null para fluxo padrão. */
const vistaInicial = ref<"anterior" | "posterior" | "circunferencial" | null>(null)

const MEMBRO_RE = /^Membro (superior|inferior) (?:direito|esquerdo) \((anterior|posterior|circunferencial)\)$/i

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
        vistaInicial.value = null
    } else {
        membroRegioes.value = null
        regiaoClicada.value = completa
        vistaInicial.value = null
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
 * Dado o id de um nó circunferencial (ex.: 'membro-superior-direito-circunferencial'),
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
        // Strip de QUALQUER sufixo de vista (-anterior/-posterior/-circunferencial) antes de
        // reanexar -circunferencial. Sem remover -circunferencial, marcar a opção "(geral)"
        // (cujo id já é <base>-circunferencial) gerava <base>-circunferencial-circunferencial —
        // id inexistente no catálogo e fora de RAMOS_CIRCUNFERENCIAL, então o boneco não pintava
        // e o título vazava o id cru.
        const n1Id = getAncestorNivel1Id(unicas[0].regiaoId)
        const base = (n1Id ?? unicas[0].regiaoId).replace(/-(anterior|posterior|circunferencial)$/, '')
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
        <!-- Sinais vitais (colapsável) -->
        <div class="subsecao">
            <button
                type="button"
                class="subsec-header"
                :aria-expanded="expandido.sinaisVitais"
                @click="expandido.sinaisVitais = !expandido.sinaisVitais"
            >
                <i
                    class="fa-solid fa-chevron-down subsec-chevron"
                    :class="{ 'rotate-180': expandido.sinaisVitais }"
                    aria-hidden="true"
                />
                <span class="subsec-titulo-texto">Sinais vitais</span>
                <span
                    v-if="sinaisVitaisPreenchido"
                    class="subsec-dot-preenchido"
                    title="Campos preenchidos"
                    aria-label="Seção com dados preenchidos"
                />
            </button>
            <div v-show="expandido.sinaisVitais" class="subsec-corpo">
                <div class="grade-sv">
                    <AppField label="Pressão arterial (mmHg)" label-variant="compact">
                        <div class="pa-row">
                            <AppInput
                                :model-value="modelValue.paSistolica ?? ''" type="number"
                                placeholder="—" :disabled="readOnly" class="text-center"
                                @update:model-value="v => atualizar({ paSistolica: String(v) })"
                            />
                            <span class="pa-sep">/</span>
                            <AppInput
                                :model-value="modelValue.paDiastolica ?? ''" type="number"
                                placeholder="—" :disabled="readOnly" class="text-center"
                                @update:model-value="v => atualizar({ paDiastolica: String(v) })"
                            />
                        </div>
                    </AppField>
                    <AppField label="FC (bpm)" label-variant="compact">
                        <AppInput
                            :model-value="modelValue.fc ?? ''" type="number"
                            placeholder="—" :disabled="readOnly"
                            @update:model-value="v => atualizar({ fc: String(v) })"
                        />
                    </AppField>
                    <AppField label="FR (irpm)" label-variant="compact">
                        <AppInput
                            :model-value="modelValue.fr ?? ''" type="number"
                            placeholder="—" :disabled="readOnly"
                            @update:model-value="v => atualizar({ fr: String(v) })"
                        />
                    </AppField>
                    <AppField label="Temp. (°C)" label-variant="compact">
                        <AppInputDecimal
                            :model-value="modelValue.temperatura ?? ''" :decimals="1"
                            placeholder="—" :disabled="readOnly"
                            @update:model-value="v => atualizar({ temperatura: String(v) })"
                        />
                    </AppField>
                    <AppField label="SpO₂ (%)" label-variant="compact">
                        <AppInput
                            :model-value="modelValue.spo2 ?? ''" type="number" min="0" max="100"
                            placeholder="—" :disabled="readOnly"
                            @update:model-value="v => atualizar({ spo2: String(v) })"
                        />
                    </AppField>
                    <AppField label="Glicemia (mg/dL)" label-variant="compact">
                        <AppInput
                            :model-value="modelValue.glicemia ?? ''" type="number"
                            placeholder="—" :disabled="readOnly"
                            @update:model-value="v => atualizar({ glicemia: String(v) })"
                        />
                    </AppField>
                </div>
            </div>
        </div>

        <!-- Antropometria (colapsável) -->
        <div class="subsecao">
            <button
                type="button"
                class="subsec-header"
                :aria-expanded="expandido.antropometria"
                @click="expandido.antropometria = !expandido.antropometria"
            >
                <i
                    class="fa-solid fa-chevron-down subsec-chevron"
                    :class="{ 'rotate-180': expandido.antropometria }"
                    aria-hidden="true"
                />
                <span class="subsec-titulo-texto">Antropometria</span>
                <span
                    v-if="antropometriaPreenchida"
                    class="subsec-dot-preenchido"
                    title="Campos preenchidos"
                    aria-label="Seção com dados preenchidos"
                />
            </button>
            <div v-show="expandido.antropometria" class="subsec-corpo">
                <div class="grade-antro">
                    <AppField label="Peso (kg)" label-variant="compact">
                        <AppInputDecimal
                            :model-value="modelValue.peso ?? ''" :decimals="1"
                            placeholder="—" :disabled="readOnly"
                            @update:model-value="v => atualizar({ peso: String(v) })"
                        />
                    </AppField>
                    <AppField label="Altura (m)" label-variant="compact">
                        <AppInputDecimal
                            :model-value="modelValue.altura ?? ''" :decimals="2"
                            placeholder="—" :disabled="readOnly"
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
        </div>

        <!-- Ectoscopia (colapsável) -->
        <div class="subsecao">
            <button
                type="button"
                class="subsec-header"
                :aria-expanded="expandido.ectoscopia"
                @click="expandido.ectoscopia = !expandido.ectoscopia"
            >
                <i
                    class="fa-solid fa-chevron-down subsec-chevron"
                    :class="{ 'rotate-180': expandido.ectoscopia }"
                    aria-hidden="true"
                />
                <span class="subsec-titulo-texto">Ectoscopia (inspeção geral)</span>
                <span
                    v-if="ectoscopiaPreenchida"
                    class="subsec-dot-preenchido"
                    title="Campos preenchidos"
                    aria-label="Seção com dados preenchidos"
                />
            </button>
            <div v-show="expandido.ectoscopia" class="subsec-corpo">
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
        </div>

        <!-- Grade lateral: Mapa corporal (esq) + Regiões examinadas (dir) -->
        <div v-if="!readOnly" class="subsecao">
            <h4 class="subsec-titulo">
                Mapa corporal
                <span v-if="carregandoRegioes" class="hint">carregando regiões...</span>
                <span v-else-if="erroRegioes" class="hint hint-erro">{{ erroRegioes }} Recarregue a página.</span>
                <span v-else class="hint">clique em uma região para examinar</span>
            </h4>
            <div class="mapa-grade">
                <!-- Coluna esquerda: SVG -->
                <div class="mapa-col-mapa">
                    <BodyMap
                        :regioes="regioesNivel1"
                        :regioes-examinadas="regioesExaminadasMapa"
                        :vistas-por-id="vistasPorIdMapa"
                        :sexo="pacienteSexo"
                        @regiao-clicada="onRegiaoClicada"
                    />
                </div>

                <!-- Coluna direita: cabeçalho + cards ou estado vazio -->
                <div class="mapa-col-regioes">
                    <div class="regioes-cabecalho">
                        <span class="regioes-titulo">Regiões examinadas</span>
                        <span v-if="regioes.length > 0" class="regioes-contador">{{ regioes.length }}</span>
                    </div>

                    <!-- Estado vazio estilizado (CA11) -->
                    <div v-if="regioes.length === 0" class="regioes-vazio">
                        <i class="fa-regular fa-map regioes-vazio-ico" />
                        <b class="regioes-vazio-titulo">Nenhuma região examinada</b>
                        <span class="regioes-vazio-aux">Clique em uma região do mapa corporal para registrar o exame.</span>
                    </div>

                    <!-- Cards de regiões -->
                    <div v-else class="regioes-lista">
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
                </div>
            </div>
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
            :vista-inicial="vistaInicial"
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

/* Header colapsável das subseções superiores */
.subsec-header {
    display: flex;
    align-items: center;
    gap: 8px;
    width: 100%;
    background: none;
    border: none;
    padding: 0;
    cursor: pointer;
    text-align: left;
    color: hsl(var(--primary));
    margin: 0 0 0.2rem;
}
.subsec-header:focus-visible {
    outline: 2px solid hsl(var(--ring));
    outline-offset: 2px;
    border-radius: var(--radius-sm, 4px);
}

.subsec-titulo-texto {
    font-weight: var(--font-weight-bold);
    font-size: var(--text-sm);
    flex: 1;
}

.subsec-chevron {
    font-size: var(--text-2xs);
    color: hsl(var(--muted-foreground));
    transition: transform 0.2s ease;
    flex-shrink: 0;
}

.subsec-dot-preenchido {
    width: 7px;
    height: 7px;
    border-radius: 9999px;
    background: hsl(var(--success));
    flex-shrink: 0;
}

/* Conteúdo colapsável — leve fade de entrada */
.subsec-corpo {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}

/* Títulos estáticos (mapa corporal, observações gerais) */
.subsec-titulo {
    font-weight: var(--font-weight-bold);
    font-size: var(--text-sm);
    color: hsl(var(--primary));
    margin: 0 0 0.2rem;
    display: flex;
    align-items: center;
    gap: 8px;
}
.subsec-titulo .hint {
    font-weight: var(--font-weight-medium);
    font-size: var(--text-xs);
    color: var(--text-muted);
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

/* ── Grade lateral mapa + regiões ──────────────────────────────────────── */
.mapa-grade {
    display: grid;
    grid-template-columns: minmax(0, 500px) minmax(0, 1fr);
    gap: 26px;
    margin-top: 0.25rem;
    align-items: start;
}

.mapa-col-mapa {
    display: flex;
    justify-content: center;
}

.mapa-col-regioes {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
    min-width: 0;
}

/* Cabeçalho da coluna de regiões */
.regioes-cabecalho {
    display: flex;
    align-items: center;
    gap: 8px;
}

.regioes-titulo {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--secondary) / 0.75);
}

.regioes-contador {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-bold);
    color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.1);
    padding: 2px 8px;
    border-radius: 9999px;
    line-height: 1.4;
}

/* Estado vazio */
.regioes-vazio {
    border: 1px dashed hsl(var(--border));
    border-radius: 0.75rem;
    padding: 2rem 1.25rem;
    text-align: center;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.5rem;
    color: hsl(var(--secondary) / 0.5);
}

.regioes-vazio-ico {
    font-size: var(--text-2xl);
    opacity: 0.4;
}

.regioes-vazio-titulo {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-bold);
    color: hsl(var(--secondary) / 0.6);
}

.regioes-vazio-aux {
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.45);
    line-height: 1.45;
}

/* Lista de cards */
.regioes-lista {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    overflow-y: auto;
    max-height: 60vh;
}

@media (max-width: 900px) {
    .grade-sv, .grade-antro { grid-template-columns: 1fr 1fr; }
    .mapa-grade { grid-template-columns: 1fr; }
}
</style>
