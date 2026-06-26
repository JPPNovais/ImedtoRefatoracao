<script setup lang="ts">
/**
 * RegioesGlobaisFormView — criação/edição de região anatômica (Wave 4 live-link).
 * Criação: codigo, nome, vista, paiCodigo, nivel, templateTexto, ordem, lateralidade, motivo.
 * Edição: apenas nome, templateTexto, motivo (estrutura não muda).
 * B3 (2026-06-08_007): OPCOES_VISTA corrigido; guard de pai circunferencial (R7/CA6).
 * 2026-06-22_004: seletor visual (BodyMap) como atalho de pai/vista no modo criação.
 */
import { ref, onMounted, computed, watch } from "vue"
import { useRouter } from "vue-router"
import { AppPageHeader, AppCard, AppField, AppInput, AppTextarea, AppButton, AppSelect } from "@/components/ui"
import { useRegioesGlobaisStore } from "../stores/regioesGlobaisStore"
import { regioesGlobaisService, type RegiaoAnatomicaNoDto } from "../services/catalogosService"
import BodyMap, { type ExameFisicoRegiao, type TroncoClique } from "@/components/exame-fisico/BodyMap.vue"
import { PARTE_PARA_TRONCO } from "@/components/exame-fisico/regioesCircunferenciais"

const MSG_CIRCUNFERENCIAL = "Nós circunferenciais são agregadores e não aceitam sub-regiões."

const props = defineProps<{ id?: string }>()
const router = useRouter()
const store = useRegioesGlobaisStore()

const idNumerico = computed(() => props.id ? Number(props.id) : null)
const editando = computed(() => idNumerico.value !== null && !isNaN(idNumerico.value))

// CA2: apenas os 3 valores reais do catálogo (+ sem vista para nós raiz/neutros)
const OPCOES_VISTA = [
    { value: "", label: "Sem vista (raiz ou neutra)" },
    { value: "anterior", label: "Anterior" },
    { value: "posterior", label: "Posterior" },
    { value: "circunferencial", label: "Circunferencial" },
]

// campos de criação (imutáveis após criar)
// Addendum 2026-06-25_001: sufixo é o estado; codigoFinal é derivado.
// No modo raiz (sem pai): sufixo = código completo (comportamento original).
// Com pai: codigoFinal = paiCodigo + "-" + sufixo (R7).
const sufixo = ref("")
// Código lido no modo edição (somente leitura após criação)
const codigoEdicao = ref("")
const vista = ref("")
const paiCodigo = ref("")
const nivel = ref<number>(1)
const ordem = ref<number>(1)
const lateralidade = ref(false)

// estado derivado do pai (R2/R3/R7)
const erroCircunferencial = ref("")

/**
 * Prefixo ativo (paiCodigo + "-") ou "" quando raiz.
 * R8: o prefixo é renderizado como segmento não-editável, não compõe o input do sufixo.
 */
const prefixo = computed(() => paiCodigo.value.trim() ? `${paiCodigo.value.trim()}-` : "")

/**
 * Código final derivado — submetido ao backend.
 * R9: trocar pai troca o prefixo e PRESERVA o sufixo.
 * R11: sem pai → sufixo é o código livre (raiz).
 */
const codigoFinal = computed(() => prefixo.value + sufixo.value.trim())

/** Caracteres restantes para o sufixo antes de estourar 60 chars no código final. */
const maxSufixo = computed(() => 60 - prefixo.value.length)

/** Mensagem de limite efetivo de sufixo (CA25). */
const erroSufixo = computed((): string => {
    const s = sufixo.value.trim()
    if (!s && paiCodigo.value.trim()) return "Sufixo do código é obrigatório."
    if (!s && !paiCodigo.value.trim()) return "" // validado em validar() como campo obrigatório
    if (s.length > maxSufixo.value)
        return `Código completo excede 60 caracteres — encurte o sufixo (máx. ${maxSufixo.value} caracteres para este pai).`
    // CA26: não pode começar ou terminar com "-"
    if (s.startsWith("-") || s.endsWith("-"))
        return 'Sufixo não pode começar nem terminar com "-".'
    return ""
})

// campos sempre editáveis
const nome = ref("")
const templateTexto = ref("")
const motivo = ref("")
const erros = ref<Record<string, string>>({})
const salvando = ref(false)
const erroGeral = ref("")

onMounted(async () => {
    if (editando.value && idNumerico.value !== null) {
        await store.carregarItem(idNumerico.value)
        if (store.itemAtual) {
            nome.value = store.itemAtual.nome
            templateTexto.value = store.itemAtual.templateTexto ?? ""
            // somente leitura no modo edição (CA29: sem prefixo travado):
            codigoEdicao.value = store.itemAtual.codigo
            vista.value = store.itemAtual.vista ?? ""
            paiCodigo.value = store.itemAtual.paiCodigo ?? ""
            nivel.value = store.itemAtual.nivel
            ordem.value = store.itemAtual.ordem
            lateralidade.value = store.itemAtual.lateralidade
        }
    } else {
        // Pré-carrega a árvore para o seletor visual (R8: já exposta pelo service).
        // Silencioso: falha não bloqueia o formulário (CA11).
        if (store.arvore.length === 0) {
            try {
                await store.carregarArvore(true)
            } catch {
                // no-op silencioso — admin pode digitar manualmente (CA11)
            }
        }
    }
})

// CA6 (R7/R2/R3): ao digitar código do pai, verificar circunferencial e derivar vista/nível
watch(paiCodigo, async (codigoPai) => {
    erroCircunferencial.value = ""
    if (!codigoPai.trim()) return

    try {
        // Reutiliza árvore já carregada quando possível
        const arvore = store.arvore.length > 0 ? store.arvore : await regioesGlobaisService.listarArvore(true)
        const encontrado = encontrarNaArvore(arvore, codigoPai.trim())
        if (!encontrado) return

        // R7: bloquear pai circunferencial (CA6)
        if (encontrado.vista === "circunferencial") {
            erroCircunferencial.value = MSG_CIRCUNFERENCIAL
            return
        }

        // R2/R3: derivar vista e nível do pai (CA3/CA5)
        vista.value = encontrado.vista ?? ""
        nivel.value = encontrado.nivel + 1
    } catch {
        // silencia: a validação real está no backend (CA6b)
    }
})

type NoArvore = { codigo: string; vista: string | null; nivel: number; filhos: NoArvore[] }

function encontrarNaArvore(nos: NoArvore[], codigo: string): NoArvore | null {
    for (const no of nos) {
        if (no.codigo.toLowerCase() === codigo.toLowerCase()) return no
        const filho = encontrarNaArvore(no.filhos, codigo)
        if (filho) return filho
    }
    return null
}

// ── Seletor visual (BodyMap como atalho) ─────────────────────────────────────

/**
 * Adapta os nós de nível 1 da árvore do catálogo para o formato ExameFisicoRegiao,
 * usando `codigo` como `id` para que possamos recuperá-lo no handler do clique.
 * Passa apenas nível 1 (os únicos com hotspot no BodyMap).
 */
const regioesParaMapa = computed<ExameFisicoRegiao[]>(() =>
    store.arvore
        .filter((no) => no.nivel === 1)
        .map((no): ExameFisicoRegiao => ({
            id: no.codigo,
            nome: no.nome,
            nivel: 1,
            lateralidade: no.lateralidade,
            pai_id: no.paiCodigo,
            vista: (no.vista as ExameFisicoRegiao["vista"]) ?? null,
            template_texto: no.templateTexto ?? null,
        })),
)

/**
 * Clique em hotspot de nível 1 do mapa: preenche paiCodigo e vista.
 * O preenchimento dispara o watcher existente de paiCodigo, que re-deriva
 * vista e nivel — exatamente como se o admin tivesse digitado o código (R3).
 *
 * CA9: se arvore ainda não carregou (regioesParaMapa vazio), o BodyMap não
 * renderiza hotspots de catálogo e este handler não é chamado — no-op implícito.
 */
function aoClicarRegiaoNoMapa(regiao: ExameFisicoRegiao): void {
    // id === codigo (adaptador acima). Verifica se é nível 1 explicitamente.
    if (regiao.nivel !== 1) return

    // Resolve o nó na árvore pelo codigo para garantir dados frescos.
    const nos = store.arvore as RegiaoAnatomicaNoDto[]
    const no = encontrarNaArvore(nos, regiao.id)
    if (!no || no.nivel !== 1) return

    // Preenche o campo paiCodigo; o watcher existente deriva vista e nivel (R3).
    paiCodigo.value = no.codigo
}

// ── Seletor de tronco (addendum 2026-06-22_004, R9–R12) ─────────────────────

/**
 * Mapa de id-base-de-parte → label amigável para o seletor do tronco.
 * Derivado dos ids do PARTE_PARA_TRONCO — NÃO duplica a lista lógica.
 * Labels espelham os GRUPOS_TRONCO_* do exame físico (SecaoExameFisico.vue).
 */
const LABEL_PARTE_TRONCO: Record<string, string> = {
    "torax-anterior":        "Tórax",
    "abdome-anterior":       "Abdome",
    "pelve-anterior":        "Pelve",
    "torax-posterior":       "Tórax",
    "lombossacra-posterior": "Região lombossacra",
    "pelve-posterior":       "Pelve",
}

/**
 * Opção de seleção de parte do tronco: id da parte no catálogo + label de UI.
 */
interface OpcaoTronco {
    regiaoBaseId: string
    label: string
}

/**
 * Estado do seletor inline do tronco.
 * null = fechado; "tronco-anterior"|"tronco-posterior" = aberto para aquele lado.
 */
const seletorTronco = ref<TroncoClique | null>(null)

/**
 * Opções derivadas de PARTE_PARA_TRONCO (invertido): filtra as partes que
 * pertencem ao lado clicado e cruza com a árvore para confirmar existência.
 * CA20: árvore não carregada → lista vazia (no-op), sem erro global.
 */
const opcoesSeletorTronco = computed<OpcaoTronco[]>(() => {
    if (!seletorTronco.value) return []
    const nomeTronco = seletorTronco.value === "tronco-anterior"
        ? "Tronco (anterior)"
        : "Tronco (posterior)"
    return Object.entries(PARTE_PARA_TRONCO)
        .filter(([, tronco]) => tronco === nomeTronco)
        .map(([regiaoBaseId]) => ({ regiaoBaseId, label: LABEL_PARTE_TRONCO[regiaoBaseId] ?? regiaoBaseId }))
})

/**
 * Clique no pseudo-hotspot de tronco — R9 (supera R4/CA4 do original).
 * No modo criação: abre seletor inline com partes nível-1 daquele lado.
 * R13: só criação (o boneco inteiro só é renderizado na criação via v-if="!editando").
 * CA9 (árvore não carregada): se a árvore ainda não carregou, o seletor abre mas mostra
 * lista vazia — estado vazio tratado no template (CA20). Não bloqueia o formulário.
 */
function aoClicarTroncoNoMapa(vistaId: TroncoClique): void {
    seletorTronco.value = vistaId
}

/**
 * Escolha de uma parte no seletor do tronco: preenche paiCodigo com o codigo
 * da parte escolhida (R10). O watcher existente de paiCodigo re-deriva vista e nivel.
 * Resultado idêntico ao clique direto num hotspot (R3 do original segue válido).
 */
function aoEscolherParteTronco(regiaoBaseId: string): void {
    const no = encontrarNaArvore(store.arvore as RegiaoAnatomicaNoDto[], regiaoBaseId)
    if (no && no.nivel === 1) {
        paiCodigo.value = no.codigo
    }
    seletorTronco.value = null
}

/**
 * Fechar/cancelar o seletor do tronco sem escolher — R11: nenhum campo muda.
 */
function fecharSeletorTronco(): void {
    seletorTronco.value = null
}

// ── Seletor de pai por dropdown (P2 — nível 3) ──────────────────────────────

interface OpcaoPai {
    value: string
    label: string
}

/**
 * Opções achatadas de nível 1 e nível 2 da árvore para o AppSelect de pai.
 * Nós circunferenciais ficam disponíveis no dropdown mas o watcher existente
 * já rejeita a escolha com erroCircunferencial (R7/CA6).
 * CA15: guard no watcher dispara mesmo quando preenchido via AppSelect.
 */
const opcoesPaiDropdown = computed<OpcaoPai[]>(() => {
    const lista: OpcaoPai[] = [{ value: "", label: "Sem pai (nó raiz)" }]
    for (const no1 of store.arvore) {
        if (!no1.ativo) continue
        lista.push({ value: no1.codigo, label: `${no1.codigo} — ${no1.nome}` })
        for (const no2 of no1.filhos ?? []) {
            if (!no2.ativo) continue
            lista.push({ value: no2.codigo, label: `  ${no2.codigo} — ${no2.nome}` })
        }
    }
    return lista
})

function validar(): boolean {
    const e: Record<string, string> = {}
    if (!nome.value.trim()) e.nome = "Nome é obrigatório."
    if (!editando.value) {
        // Valida código final derivado (codigoFinal = prefixo + sufixo)
        if (!codigoFinal.value) e.codigo = "Código é obrigatório."
        else if (codigoFinal.value.length > 60) e.codigo = "Código deve ter no máximo 60 caracteres."
        // Valida sufixo separadamente quando há pai
        if (paiCodigo.value.trim() && erroSufixo.value) e.codigo = erroSufixo.value
        if (nivel.value < 1 || nivel.value > 3) e.nivel = "Nível deve ser entre 1 e 3."
        if (erroCircunferencial.value) e.paiCodigo = erroCircunferencial.value
    }
    if (motivo.value.trim().length < 10) e.motivo = "Motivo deve ter ao menos 10 caracteres."
    erros.value = e
    return Object.keys(e).length === 0
}

const submitBloqueado = computed(() =>
    motivo.value.trim().length < 10 || !!erroCircunferencial.value || !!erroSufixo.value
)

async function salvar() {
    if (!validar()) return
    salvando.value = true
    erroGeral.value = ""
    try {
        if (editando.value && idNumerico.value !== null) {
            await store.atualizar(idNumerico.value, {
                nome: nome.value.trim(),
                templateTexto: templateTexto.value.trim() || null,
                motivo: motivo.value.trim(),
            })
        } else {
            await store.criar({
                codigo: codigoFinal.value, // sempre código final (prefixo + sufixo ou só sufixo se raiz)
                nome: nome.value.trim(),
                paiCodigo: paiCodigo.value.trim() || null,
                nivel: nivel.value,
                vista: vista.value || null,
                templateTexto: templateTexto.value.trim() || null,
                ordem: ordem.value,
                lateralidade: lateralidade.value,
                motivo: motivo.value.trim(),
            })
        }
        router.push({ name: "AdminRegioesGlobais" })
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroGeral.value = msg ?? "Não foi possível salvar a região."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            :titulo="editando ? 'Editar região anatômica' : 'Nova região anatômica'"
        />

        <div v-if="store.carregando && editando" class="estado-info">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>
        <p v-else-if="store.erro && editando" class="estado-erro">{{ store.erro }}</p>

        <AppCard v-else>
            <form @submit.prevent="salvar" class="form-campos">

                <!-- Campos somente criação -->
                <template v-if="!editando">
                    <!--
                        Addendum 2026-06-25_001 — prefixo automático travado (R7/R8/CA20–CA26).
                        Sem pai: campo livre (sufixo = código completo).
                        Com pai: prefixo cinza não-editável + input do sufixo ao lado.
                        Decisão técnica: composição local no form (sem DS — CA30 N/A justificado).
                    -->
                    <AppField
                        label="Código"
                        required
                        :hint="erros.codigo || erroSufixo || (paiCodigo.trim() ? `Código final: ${codigoFinal || '…'} (máx. ${maxSufixo} caracteres de sufixo para este pai)` : 'Identificador único da região (ex.: ABD-SUP-D). Imutável após criação.')"
                    >
                        <!-- Sem pai: campo único livre (comportamento original) -->
                        <AppInput
                            v-if="!paiCodigo.trim()"
                            v-model="sufixo"
                            placeholder="Ex.: ABD-SUP-D"
                            :maxlength="60"
                            :disabled="salvando"
                        />
                        <!-- Com pai: prefixo travado + input do sufixo (R8/CA21) -->
                        <div v-else class="codigo-com-prefixo">
                            <span class="codigo-prefixo" aria-hidden="true">{{ prefixo }}</span>
                            <AppInput
                                v-model="sufixo"
                                :placeholder="`sufixo (máx. ${maxSufixo} chars)`"
                                :maxlength="maxSufixo"
                                :disabled="salvando"
                                class="codigo-sufixo-input"
                                aria-label="Sufixo do código"
                            />
                        </div>
                        <p v-if="erroSufixo && !erros.codigo" class="campo-erro-inline" role="alert">{{ erroSufixo }}</p>
                    </AppField>

                    <!-- Seletor visual de pai (atalho): BodyMap clicável (R1/R2/R5 + addendum R9-R13) -->
                    <div class="seletor-mapa-pai">
                        <p class="seletor-mapa-rotulo">Selecionar pai pelo mapa <span class="seletor-mapa-opcional">(atalho)</span></p>
                        <p class="seletor-mapa-dica">Clique numa parte do corpo para preencher "Código do pai" e "Vista" automaticamente.</p>
                        <BodyMap
                            :regioes="regioesParaMapa"
                            :regioes-examinadas="[]"
                            :sexo="null"
                            @regiaoClicada="aoClicarRegiaoNoMapa"
                            @troncoClicado="aoClicarTroncoNoMapa"
                        />
                        <!-- Seletor inline do tronco (addendum 2026-06-22_004, R9–R11/CA13–CA16) -->
                        <div v-if="seletorTronco" class="seletor-tronco" role="dialog" aria-modal="false" :aria-label="`Selecionar parte do ${seletorTronco === 'tronco-anterior' ? 'tronco anterior' : 'tronco posterior'}`">
                            <div class="seletor-tronco-cabecalho">
                                <span class="seletor-tronco-titulo">{{ seletorTronco === 'tronco-anterior' ? 'Tronco anterior' : 'Tronco posterior' }}</span>
                                <button
                                    type="button"
                                    class="seletor-tronco-fechar btn-icon"
                                    aria-label="Fechar seletor"
                                    @click="fecharSeletorTronco"
                                ><i class="fa-solid fa-xmark"></i></button>
                            </div>
                            <p v-if="opcoesSeletorTronco.length === 0" class="seletor-tronco-vazio">
                                Nenhuma parte disponível no catálogo.
                            </p>
                            <ul v-else class="seletor-tronco-lista" role="listbox">
                                <li
                                    v-for="opcao in opcoesSeletorTronco"
                                    :key="opcao.regiaoBaseId"
                                    class="seletor-tronco-item"
                                    role="option"
                                >
                                    <button
                                        type="button"
                                        class="seletor-tronco-botao"
                                        :aria-label="`Selecionar ${opcao.label}`"
                                        @click="aoEscolherParteTronco(opcao.regiaoBaseId)"
                                    >{{ opcao.label }}</button>
                                </li>
                            </ul>
                        </div>
                    </div>

                    <!-- P2: seletor dropdown de pai (nível 1 e 2) para habilitar nível 3 sem digitação -->
                    <AppField
                        label="Região pai"
                        :hint="erros.paiCodigo || 'Selecione o pai no dropdown OU use o mapa acima OU digite abaixo. Nós circunferenciais não aceitam filhos.'"
                    >
                        <AppSelect
                            v-model="paiCodigo"
                            :options="opcoesPaiDropdown"
                            :disabled="salvando"
                        />
                        <p v-if="erroCircunferencial" class="campo-erro-inline" role="alert">{{ erroCircunferencial }}</p>
                    </AppField>

                    <AppField label="Código do pai" :hint="'Ou digite o código diretamente (alternativa ao dropdown acima).'">
                        <AppInput
                            v-model="paiCodigo"
                            placeholder="Ex.: ABD"
                            maxlength="60"
                            :disabled="salvando"
                        />
                    </AppField>

                    <AppField label="Vista" hint="Vista corporal onde a região aparece no mapa.">
                        <AppSelect v-model="vista" :options="OPCOES_VISTA" :disabled="salvando || !!paiCodigo.trim()" />
                        <p v-if="paiCodigo.trim()" class="hint-derivado">Vista derivada do pai.</p>
                    </AppField>

                    <div class="row-2col">
                        <AppField label="Nível" required :hint="erros.nivel || '1=Raiz, 2=Sub-região, 3=Detalhe'">
                            <AppInput v-model.number="nivel" type="number" min="1" max="3" :disabled="salvando || !!paiCodigo.trim()" />
                        </AppField>
                        <AppField label="Ordem" hint="Ordem de exibição dentro do mesmo pai.">
                            <AppInput v-model.number="ordem" type="number" min="1" max="9999" :disabled="salvando" />
                        </AppField>
                    </div>

                    <AppField label="Lateralidade">
                        <label class="label-check">
                            <input type="checkbox" v-model="lateralidade" :disabled="salvando" />
                            Esta região possui distinção esquerda/direita
                        </label>
                    </AppField>
                </template>

                <!-- Campos somente leitura no modo edição (CA29: sem alteração) -->
                <template v-else>
                    <div class="info-somente-leitura">
                        <span class="info-label">Código</span>
                        <code class="info-valor">{{ codigoEdicao }}</code>
                        <span class="info-label">Nível</span>
                        <span class="info-valor">{{ nivel }}</span>
                        <span class="info-label">Vista</span>
                        <span class="info-valor">{{ vista || "—" }}</span>
                    </div>
                </template>

                <!-- Campos sempre editáveis -->
                <AppField label="Nome" required :hint="erros.nome">
                    <AppInput
                        v-model="nome"
                        placeholder="Ex.: Quadrante superior direito"
                        maxlength="120"
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Template de texto" hint="Texto padrão sugerido ao registrar achados nesta região.">
                    <AppTextarea
                        v-model="templateTexto"
                        :rows="4"
                        placeholder="Ex.: Sem alterações..."
                        :disabled="salvando"
                    />
                </AppField>

                <AppField label="Motivo da alteração" required :hint="erros.motivo || 'Mínimo 10 caracteres.'">
                    <AppInput
                        v-model="motivo"
                        placeholder="Descreva o motivo..."
                        :disabled="salvando"
                    />
                </AppField>

                <p v-if="erroGeral" class="campo-erro" role="alert">{{ erroGeral }}</p>

                <div class="form-acoes">
                    <AppButton variant="ghost" type="button" @click="router.push({ name: 'AdminRegioesGlobais' })">Cancelar</AppButton>
                    <AppButton
                        type="submit"
                        :loading="salvando"
                        :disabled="submitBloqueado"
                    >
                        {{ editando ? "Salvar alterações" : "Criar região" }}
                    </AppButton>
                </div>
            </form>
        </AppCard>
    </main>
</template>

<style scoped>
.estado-info {
    text-align: center;
    padding: 2rem 0;
    color: hsl(var(--muted-foreground));
    font-size: var(--text-sm);
}

.estado-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
}

.form-campos {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.row-2col {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
}

.label-check {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: var(--text-sm);
    cursor: pointer;
    color: hsl(var(--foreground));
}

.hint-derivado {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    margin: 0.25rem 0 0;
}

.info-somente-leitura {
    display: grid;
    grid-template-columns: auto 1fr;
    gap: 0.375rem 0.75rem;
    align-items: center;
    padding: 0.75rem;
    background: hsl(var(--muted) / 0.4);
    border-radius: var(--radius);
    font-size: var(--text-xs);
}

.info-label {
    color: hsl(var(--muted-foreground));
    font-weight: var(--font-weight-semibold);
    font-size: var(--text-xs);
    text-transform: uppercase;
    letter-spacing: 0.03em;
}

.info-valor {
    color: hsl(var(--foreground));
}

.campo-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
    margin: 0;
}

.campo-erro-inline {
    color: hsl(var(--destructive));
    font-size: var(--text-xs);
    margin: 0.25rem 0 0;
}

.form-acoes {
    display: flex;
    gap: 0.75rem;
    justify-content: flex-end;
    padding-top: 0.5rem;
}

/* ── Seletor visual de pai (BodyMap como atalho) ──────────────────────────── */
.seletor-mapa-pai {
    display: flex;
    flex-direction: column;
    gap: var(--space-2);
    padding: var(--space-3);
    background: hsl(var(--muted) / 0.35);
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
}

.seletor-mapa-rotulo {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--foreground));
    text-transform: uppercase;
    letter-spacing: 0.04em;
    margin: 0;
}

.seletor-mapa-opcional {
    font-weight: var(--font-weight-normal);
    text-transform: none;
    letter-spacing: 0;
    color: hsl(var(--muted-foreground));
}

.seletor-mapa-dica {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    margin: 0;
}

/* ── Seletor inline do tronco (addendum 2026-06-22_004) ─────────────────── */
.seletor-tronco {
    margin-top: var(--space-2);
    padding: var(--space-3);
    background: hsl(var(--background));
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
}

.seletor-tronco-cabecalho {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: var(--space-2);
}

.seletor-tronco-titulo {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--foreground));
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.seletor-tronco-fechar {
    padding: 0.25rem 0.375rem;
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
}

.seletor-tronco-vazio {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    margin: 0;
    text-align: center;
    padding: var(--space-2) 0;
}

.seletor-tronco-lista {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: var(--space-1);
}

.seletor-tronco-item {
    display: contents;
}

.seletor-tronco-botao {
    width: 100%;
    text-align: left;
    padding: var(--space-2) var(--space-3);
    background: transparent;
    border: 1px solid hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
    color: hsl(var(--foreground));
    cursor: pointer;
    transition: background 0.12s ease;
}

.seletor-tronco-botao:hover,
.seletor-tronco-botao:focus-visible {
    background: hsl(var(--accent) / 0.6);
    outline: none;
}

/* ── Código com prefixo fixo (addendum 2026-06-25_001, CA20–CA26) ─────────── */
.codigo-com-prefixo {
    display: flex;
    align-items: center;
    gap: 0;
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    overflow: hidden;
    background: hsl(var(--background));
}

/* Segmento não-editável (prefixo travado) */
.codigo-prefixo {
    padding: 0 0.625rem;
    background: hsl(var(--muted) / 0.6);
    color: hsl(var(--muted-foreground));
    font-size: var(--text-sm);
    font-family: monospace;
    white-space: nowrap;
    border-right: 1px solid hsl(var(--border));
    /* Mesma altura que o form-input */
    line-height: 2.25rem;
    user-select: none;
}

/* O AppInput do sufixo ocupa o restante — remove borda própria para fundir com o container */
.codigo-com-prefixo :deep(.form-input) {
    border: none;
    border-radius: 0;
    flex: 1;
    box-shadow: none;
}

.codigo-com-prefixo :deep(.form-input):focus {
    outline: none;
}

/* Foco visual no container composto */
.codigo-com-prefixo:has(:deep(.form-input):focus) {
    outline: 2px solid hsl(var(--ring));
    outline-offset: 2px;
}
</style>
