<!--
  Seção "Procedimentos indicados" do prontuário.

  Formatos coexistentes no ConteudoJson (discriminado por catalogoCirurgiaId):
    - Novo  (catálogo): { catalogoCirurgiaId, descricao, valor, observacao }
    - Legado (texto-livre): { descricao, observacao }

  Regras:
    - R1/R4: lista catálogo do tenant ativo via orcamentoCatalogoService.listarProcedimentos.
      Reusa o gate existente (orcamento.configurar + OrcamentoCompleto). 403/feature-off →
      degrada para modo manual texto-livre (D4/CA50). Sem endpoint paralelo.
    - R3: RBAC da criação inline = mesma permissão do catálogo. Dono ou papel com
      orcamento.configurar. Sem permissão → botão não renderiza (CA48).
    - R5: snapshot (descricao + valor) sempre gravado com catalogoCirurgiaId (D2).
    - R6: evolução é append-only — este componente só monta o objeto novo; não reescreve.
    - R7: itens legados renderizam read-only como texto sem valor (CA52).
-->
<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue"
import { AppButton, AppInput, AppTextarea } from "@/components/ui"
import { AppInputDecimal } from "@/components/ui"
import { orcamentoCatalogoService, type CatalogoCirurgia } from "@/services/orcamentoCatalogoService"
import { usePermissoesStore } from "@/stores/permissoesStore"
import { useDebouncedRef } from "@/composables/useDebouncedRef"

// ── Tipos ──────────────────────────────────────────────────────────────────────

/** Item no formato novo (catálogo). catalogoCirurgiaId presente. */
interface ProcCatalogo {
    catalogoCirurgiaId: number
    descricao: string
    valor: number
    observacao: string
}

/** Item legado (texto-livre). Sem catalogoCirurgiaId. */
interface ProcLegado {
    descricao: string
    observacao: string
}

type ProcItem = ProcCatalogo | ProcLegado

function ehCatalogo(p: ProcItem): p is ProcCatalogo {
    return "catalogoCirurgiaId" in p && (p as ProcCatalogo).catalogoCirurgiaId != null
}

interface Data {
    procedimentos?: ProcItem[]
    observacoes?: string
}

// ── Props / emits ──────────────────────────────────────────────────────────────

const props = defineProps<{ modelValue: Data; readOnly?: boolean }>()
const emit  = defineEmits<{ "update:modelValue": [v: Data] }>()

function atualizar(patch: Partial<Data>) {
    emit("update:modelValue", { ...props.modelValue, ...patch })
}

// ── Permissões (R3/R4) ─────────────────────────────────────────────────────────

const permissoes = usePermissoesStore()

/** Pode criar item no catálogo (D3): Dono ou orcamento.configurar. */
const podeCriarCatalogo = computed(() => permissoes.pode("orcamento.configurar"))

/** Catálogo disponível (true quando GET retornou com sucesso, false após 403/erro). */
const catalogoDisponivel = ref(false)
const carregandoCatalogo = ref(false)

// ── Catálogo ───────────────────────────────────────────────────────────────────

const catalogo = ref<CatalogoCirurgia[]>([])

onMounted(async () => {
    carregandoCatalogo.value = true
    try {
        catalogo.value = await orcamentoCatalogoService.listarProcedimentos(true)
        catalogoDisponivel.value = true
    } catch {
        // 403 / feature-off / erro de rede → degrada para modo manual (D4/CA50)
        catalogoDisponivel.value = false
    } finally {
        carregandoCatalogo.value = false
    }
})

// ── Busca client-side (D1) ─────────────────────────────────────────────────────

const buscaInput = ref("")
const busca      = useDebouncedRef(buscaInput, 150)
const dropdownAberto = ref(false)

const jaAdicionadosIds = computed(() =>
    (props.modelValue.procedimentos ?? [])
        .filter(ehCatalogo)
        .map(p => (p as ProcCatalogo).catalogoCirurgiaId)
)

const resultados = computed(() => {
    if (!catalogoDisponivel.value) return []
    const q = busca.value.trim().toLowerCase()
    return catalogo.value.filter(
        c => !jaAdicionadosIds.value.includes(c.id) &&
            (!q || c.descricao.toLowerCase().includes(q))
    )
})

const semResultado = computed(() =>
    catalogoDisponivel.value &&
    busca.value.trim() !== "" &&
    resultados.value.length === 0
)

function abrirDropdown() {
    if (!props.readOnly && catalogoDisponivel.value) dropdownAberto.value = true
}

function fecharDropdown() {
    dropdownAberto.value = false
    modoCreate.value = false
}

// ── Selecionar item do catálogo (CA43/R5) ─────────────────────────────────────

function selecionarItem(item: CatalogoCirurgia) {
    const novo: ProcCatalogo = {
        catalogoCirurgiaId: item.id,
        descricao: item.descricao,
        valor: item.valorBase,
        observacao: "",
    }
    atualizar({ procedimentos: [...(props.modelValue.procedimentos ?? []), novo] })
    buscaInput.value = ""
    fecharDropdown()
}

// ── Remover item ───────────────────────────────────────────────────────────────

function remover(idx: number) {
    const a = [...(props.modelValue.procedimentos ?? [])]
    a.splice(idx, 1)
    atualizar({ procedimentos: a })
}

// ── Observação de item selecionado ─────────────────────────────────────────────

function setObservacao(idx: number, obs: string) {
    const a = [...(props.modelValue.procedimentos ?? [])]
    a[idx] = { ...a[idx], observacao: obs }
    atualizar({ procedimentos: a })
}

// ── Mini-form "Criar procedimento" inline (CA45/R3) ────────────────────────────

const modoCreate = ref(false)
const createNome  = ref("")
const createValor = ref("")
const createDuracao = ref("")
const criando = ref(false)
const erroCreate = ref("")

function iniciarCreate() {
    // Pré-preenche com o termo buscado (CA53)
    createNome.value = buscaInput.value.trim()
    createValor.value = ""
    createDuracao.value = ""
    erroCreate.value = ""
    modoCreate.value = true
}

function cancelarCreate() {
    modoCreate.value = false
    erroCreate.value = ""
}

async function confirmarCreate() {
    erroCreate.value = ""
    const nome = createNome.value.trim()
    const valor = parseFloat(createValor.value.replace(",", "."))

    // Validação de UX espelhada da back (CA56/R8)
    if (!nome) { erroCreate.value = "Nome obrigatório."; return }
    if (isNaN(valor) || valor < 0) { erroCreate.value = "Valor inválido."; return }

    criando.value = true
    try {
        const { id } = await orcamentoCatalogoService.criarProcedimento({
            descricao: nome,
            valorBase: valor,
            duracaoPadraoMinutos: createDuracao.value ? parseInt(createDuracao.value, 10) : null,
        })
        // Adiciona ao catálogo local e seleciona imediatamente (CA45)
        const novoItem: CatalogoCirurgia = {
            id,
            estabelecimentoId: 0, // preenchido pelo back, opaco ao front
            descricao: nome,
            valorBase: valor,
            duracaoPadraoMinutos: createDuracao.value ? parseInt(createDuracao.value, 10) : null,
            codigoInterno: null,
            codigoTuss: null,
            categoria: null,
            ativo: true,
            criadaEm: new Date().toISOString(),
            atualizadaEm: null,
        }
        catalogo.value = [novoItem, ...catalogo.value]
        selecionarItem(novoItem)
        modoCreate.value = false
        buscaInput.value = ""
    } catch {
        // Mensagem genérica (CA55/LGPD)
        erroCreate.value = "Não foi possível criar o procedimento."
    } finally {
        criando.value = false
    }
}

// Fecha dropdown ao clicar fora
function onClickOutside(e: MouseEvent) {
    const el = document.querySelector(".ip-selector")
    if (el && !el.contains(e.target as Node)) fecharDropdown()
}

watch(dropdownAberto, (aberto) => {
    if (aberto) document.addEventListener("mousedown", onClickOutside)
    else document.removeEventListener("mousedown", onClickOutside)
})

// ── Modo manual texto-livre (legado / degradação graciosa D4/CA50) ─────────────

const lista = computed(() => props.modelValue.procedimentos ?? [])

function addProcManual() {
    atualizar({ procedimentos: [...lista.value, { descricao: "", observacao: "" }] })
}

function setFieldManual(idx: number, field: "descricao" | "observacao", valor: string) {
    const a = [...lista.value]
    a[idx] = { ...a[idx], [field]: valor }
    atualizar({ procedimentos: a })
}

// ── Formatação de valor (exibição) ─────────────────────────────────────────────

function fmtValor(v: number): string {
    return v.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
</script>

<template>
    <div class="secao">

        <!-- ── Modo catálogo (catalogoDisponivel = true ou carregando) ── -->
        <template v-if="carregandoCatalogo">
            <div class="skeleton" aria-busy="true">Carregando catálogo...</div>
        </template>

        <template v-else-if="catalogoDisponivel">

            <!-- Itens já selecionados -->
            <div v-if="lista.length > 0" class="selecionados">
                <div
                    v-for="(p, i) in lista"
                    :key="i"
                    :class="['item-selecionado', { 'item-legado': !ehCatalogo(p) }]"
                >
                    <div class="item-cabeca">
                        <span class="item-desc">{{ p.descricao }}</span>
                        <span v-if="ehCatalogo(p)" class="item-valor">
                            R$ {{ fmtValor((p as any).valor) }}
                        </span>
                        <button
                            v-if="!readOnly"
                            class="btn-icon btn-icon-excluir"
                            type="button"
                            title="Remover"
                            @click="remover(i)"
                        >
                            <i class="fa-solid fa-xmark" aria-hidden="true" />
                        </button>
                    </div>
                    <AppInput
                        v-if="!readOnly"
                        :model-value="p.observacao"
                        class="item-obs"
                        placeholder="Observação (opcional) — ex: joelho D"
                        @update:model-value="(v) => setObservacao(i, String(v))"
                    />
                    <p v-else-if="p.observacao" class="item-obs-readonly">{{ p.observacao }}</p>
                </div>
            </div>

            <div v-else-if="readOnly" class="vazio">
                Nenhum procedimento indicado.
            </div>

            <!-- Seletor com busca (somente em modo edição) -->
            <div v-if="!readOnly" class="ip-selector">
                <div class="ip-search-wrapper">
                    <i class="fa-solid fa-magnifying-glass ip-search-icon" aria-hidden="true" />
                    <input
                        v-model="buscaInput"
                        class="ip-search-input"
                        type="text"
                        placeholder="Buscar procedimento do catálogo..."
                        autocomplete="off"
                        @focus="abrirDropdown"
                        @keydown.escape="fecharDropdown"
                    />
                </div>

                <div v-if="dropdownAberto" class="ip-dropdown" role="listbox">

                    <!-- Mini-form criar -->
                    <div v-if="modoCreate" class="ip-create-form">
                        <p class="ip-create-titulo">
                            <i class="fa-solid fa-circle-plus" aria-hidden="true" />
                            Criar procedimento no estabelecimento
                        </p>
                        <div class="ip-create-grid">
                            <div class="ip-create-field ip-create-full">
                                <label class="field-label">Nome do procedimento <em>*</em></label>
                                <AppInput
                                    v-model="createNome"
                                    placeholder="Ex: Infiltração articular"
                                    autofocus
                                />
                            </div>
                            <div class="ip-create-field">
                                <label class="field-label">Valor (R$) <em>*</em></label>
                                <AppInputDecimal
                                    v-model="createValor"
                                    :decimals="2"
                                    placeholder="0,00"
                                />
                            </div>
                            <div class="ip-create-field">
                                <label class="field-label">Duração (min)</label>
                                <AppInput
                                    v-model="createDuracao"
                                    type="number"
                                    placeholder="30"
                                    min="0"
                                />
                            </div>
                        </div>
                        <p v-if="erroCreate" class="ip-create-erro" role="alert">{{ erroCreate }}</p>
                        <div class="ip-create-acoes">
                            <AppButton variant="ghost" size="sm" type="button" @click="cancelarCreate">
                                Cancelar
                            </AppButton>
                            <AppButton
                                size="sm"
                                type="button"
                                :loading="criando"
                                :disabled="criando"
                                @click="confirmarCreate"
                            >
                                <i class="fa-solid fa-check" aria-hidden="true" />
                                Criar e adicionar
                            </AppButton>
                        </div>
                    </div>

                    <!-- Resultados / sem resultado -->
                    <template v-else>
                        <!-- Sem resultado -->
                        <div v-if="semResultado" class="ip-noresult">
                            <i class="fa-solid fa-magnifying-glass-minus" aria-hidden="true" />
                            <span>
                                Nenhum procedimento encontrado para
                                <strong>"{{ buscaInput.trim() }}"</strong>
                            </span>
                        </div>

                        <!-- Lista de resultados -->
                        <template v-else>
                            <div v-if="resultados.length === 0 && !buscaInput.trim()" class="ip-vazio-catalogo">
                                Catálogo de procedimentos vazio.
                            </div>
                            <button
                                v-for="item in resultados"
                                :key="item.id"
                                class="ip-option"
                                type="button"
                                role="option"
                                @click="selecionarItem(item)"
                            >
                                <i class="fa-solid fa-syringe ip-option-icon" aria-hidden="true" />
                                <span class="ip-option-nome">{{ item.descricao }}</span>
                                <span class="ip-option-valor">R$ {{ fmtValor(item.valorBase) }}</span>
                                <i class="fa-solid fa-plus ip-option-add" aria-hidden="true" />
                            </button>
                        </template>

                        <!-- CTA criar (só com permissão — CA48/R3) -->
                        <button
                            v-if="podeCriarCatalogo"
                            :class="['ip-criar-btn', { 'ip-criar-btn--destaque': semResultado }]"
                            type="button"
                            @click="iniciarCreate"
                        >
                            <i class="fa-solid fa-plus" aria-hidden="true" />
                            Criar procedimento{{ buscaInput.trim() ? ` "${buscaInput.trim()}"` : '' }}
                        </button>
                    </template>
                </div>
            </div>

        </template>

        <!-- ── Modo manual (degradação graciosa D4/CA50) ── -->
        <template v-else>
            <div class="vazio" v-if="lista.length === 0">
                Nenhum procedimento indicado ainda.
            </div>
            <div v-for="(p, i) in lista" :key="i" class="linha-manual">
                <AppInput
                    :model-value="p.descricao"
                    class="input-principal"
                    placeholder="Procedimento"
                    :disabled="readOnly"
                    @update:model-value="(v) => setFieldManual(i, 'descricao', String(v))"
                />
                <AppInput
                    :model-value="p.observacao"
                    placeholder="Observação"
                    :disabled="readOnly"
                    @update:model-value="(v) => setFieldManual(i, 'observacao', String(v))"
                />
                <button
                    v-if="!readOnly"
                    class="btn-icon btn-icon-excluir"
                    type="button"
                    title="Remover"
                    @click="remover(i)"
                >
                    <i class="fa-solid fa-xmark" aria-hidden="true" />
                </button>
            </div>
            <AppButton v-if="!readOnly" size="sm" icon="fa-solid fa-plus" type="button" @click="addProcManual">
                Adicionar procedimento
            </AppButton>
        </template>

        <!-- Observações gerais (sempre presente) -->
        <div class="subsecao-obs">
            <label class="field-label">Observações gerais</label>
            <AppTextarea
                :model-value="modelValue.observacoes ?? ''"
                :rows="2"
                placeholder="Outras considerações sobre os procedimentos indicados..."
                :disabled="readOnly"
                @update:model-value="(v) => atualizar({ observacoes: String(v) })"
            />
        </div>

    </div>
</template>

<style scoped>
.secao {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

/* ── Skeleton ── */
.skeleton {
    padding: 0.75rem;
    border: 1px dashed var(--border);
    border-radius: var(--radius);
    color: var(--text-muted);
    font-size: var(--text-sm);
    text-align: center;
}

/* ── Estado vazio ── */
.vazio {
    text-align: center;
    color: var(--text-muted);
    font-size: var(--text-sm);
    padding: 1rem;
    border: 1px dashed var(--border);
    border-radius: var(--radius);
}

/* ── Itens selecionados ── */
.selecionados {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.item-selecionado {
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 0.5rem 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    background: var(--surface);
}

/* Itens legados ficam com fundo levemente diferente para distinção visual */
.item-legado {
    background: var(--muted);
}

.item-cabeca {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.item-desc {
    flex: 1;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    color: var(--text);
}

.item-valor {
    font-size: var(--text-sm);
    color: var(--text-muted);
    white-space: nowrap;
}

.item-obs {
    font-size: var(--text-sm);
}

.item-obs-readonly {
    font-size: var(--text-sm);
    color: var(--text-muted);
    margin: 0;
}

/* ── Seletor busca ── */
.ip-selector {
    position: relative;
}

.ip-search-wrapper {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 0 0.75rem;
    background: var(--surface);
}

.ip-search-icon {
    color: var(--text-muted);
    font-size: var(--text-sm);
    flex-shrink: 0;
}

.ip-search-input {
    flex: 1;
    border: none;
    outline: none;
    background: transparent;
    padding: 0.5rem 0;
    font-size: var(--text-sm);
    color: var(--text);
}

.ip-search-input::placeholder {
    color: var(--text-muted);
}

/* ── Dropdown ── */
.ip-dropdown {
    position: absolute;
    top: calc(100% + 4px);
    left: 0;
    right: 0;
    z-index: 50;
    border: 1px solid var(--border);
    border-radius: var(--radius);
    background: var(--surface);
    box-shadow: 0 4px 16px hsl(0 0% 0% / 0.12);
    max-height: 280px;
    overflow-y: auto;
}

/* ── Sem resultado ── */
.ip-noresult {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    color: var(--text-muted);
    font-size: var(--text-sm);
}

/* ── Catálogo vazio ── */
.ip-vazio-catalogo {
    padding: 0.75rem 1rem;
    color: var(--text-muted);
    font-size: var(--text-sm);
}

/* ── Opções ── */
.ip-option {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    width: 100%;
    padding: 0.5rem 1rem;
    border: none;
    background: transparent;
    cursor: pointer;
    text-align: left;
    font-size: var(--text-sm);
    color: var(--text);
    transition: background 0.1s;
}

.ip-option:hover {
    background: var(--muted);
}

.ip-option-icon {
    color: var(--text-muted);
    font-size: var(--text-xs);
    flex-shrink: 0;
}

.ip-option-nome {
    flex: 1;
}

.ip-option-valor {
    color: var(--text-muted);
    white-space: nowrap;
    font-size: var(--text-xs);
}

.ip-option-add {
    color: var(--primary);
    font-size: var(--text-xs);
    flex-shrink: 0;
}

/* ── Botão Criar ── */
.ip-criar-btn {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    width: 100%;
    padding: 0.5rem 1rem;
    border: none;
    border-top: 1px solid var(--border);
    background: transparent;
    cursor: pointer;
    text-align: left;
    font-size: var(--text-sm);
    color: var(--primary);
    transition: background 0.1s;
}

.ip-criar-btn:hover {
    background: var(--muted);
}

/* Destacado quando busca sem resultado (CA53) */
.ip-criar-btn--destaque {
    background: hsl(var(--primary-hsl) / 0.06);
    font-weight: var(--font-weight-medium);
}

/* ── Mini-form criar ── */
.ip-create-form {
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.ip-create-titulo {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    color: var(--text);
    margin: 0;
    display: flex;
    align-items: center;
    gap: 0.4rem;
}

.ip-create-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.5rem;
}

.ip-create-full {
    grid-column: 1 / -1;
}

.ip-create-field {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.ip-create-erro {
    font-size: var(--text-xs);
    color: hsl(var(--destructive));
    margin: 0;
}

.ip-create-acoes {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
}

/* ── Modo manual / legado ── */
.linha-manual {
    display: grid;
    grid-template-columns: 2fr 1.5fr 32px;
    gap: 0.5rem;
    align-items: center;
}

/* ── Observações gerais ── */
.subsecao-obs {
    margin-top: 0.25rem;
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
}

@media (max-width: 768px) {
    .linha-manual {
        grid-template-columns: 1fr 32px;
    }
    .linha-manual .input-principal {
        grid-column: 1 / -1;
    }
    .ip-create-grid {
        grid-template-columns: 1fr;
    }
}
</style>
