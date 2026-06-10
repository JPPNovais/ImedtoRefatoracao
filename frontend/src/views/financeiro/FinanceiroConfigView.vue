<script setup lang="ts">
/**
 * FinanceiroConfigView — Configuração de cobranças (F1).
 * Vive embutida no master-detail de Configurações (?secao=financeiro).
 *
 * Duas seções verticais:
 *   1. Tabela de preços de consulta — CRUD por profissional ou padrão do estabelecimento.
 *   2. Taxa de cartão por forma de pagamento — lista de formas com percentual editável.
 *
 * Lazy (carregado só quando a seção é aberta — R1 de performance).
 */
import { ref, computed, onMounted, watch } from "vue"
import { AppButton, AppField, AppInputDecimal, AppSelect, AppEmptyState, AppModal } from "@/components/ui"
import { useCobrancaConfigStore } from "@/stores/cobrancaStore"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { vinculoService, type ProfissionalPublico } from "@/services/vinculoService"

const store = useCobrancaConfigStore()

// ── Tabela de preços ──────────────────────────────────────────────────────────

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput, 300)
const profissionais = ref<ProfissionalPublico[]>([])

async function carregarProfissionais() {
    try {
        profissionais.value = await vinculoService.listarProfissionaisPublico()
    } catch { /* silencioso */ }
}

onMounted(async () => {
    await Promise.all([
        store.carregarTabelaPreco(),
        store.carregarConfigTaxa(),
        carregarProfissionais(),
    ])
})

// Observa debounce para recarregar (CA19)
watch(busca, (b) => store.carregarTabelaPreco(b || undefined))

// ── Modal de tabela de preços ─────────────────────────────────────────────────

interface FormPreco {
    id?: number
    profissionalId: string | null
    valorSugerido: string
    modoEdicao: boolean
}

const modalPreco = ref(false)
const formPreco = ref<FormPreco>({ profissionalId: null, valorSugerido: "", modoEdicao: false })
const salvandoPreco = ref(false)
const erroPreco = ref<string | null>(null)

function abrirNovoPreco() {
    formPreco.value = { profissionalId: null, valorSugerido: "", modoEdicao: false }
    erroPreco.value = null
    modalPreco.value = true
}

function abrirEditarPreco(item: typeof store.tabelaPreco[0]) {
    formPreco.value = {
        id: item.id,
        profissionalId: item.profissionalId ?? null,
        valorSugerido: item.valorSugerido.toFixed(2),
        modoEdicao: true,
    }
    erroPreco.value = null
    modalPreco.value = true
}

async function salvarPreco() {
    const valor = parseFloat(formPreco.value.valorSugerido) || 0
    if (valor <= 0) {
        erroPreco.value = "Informe um valor maior que zero."
        return
    }
    salvandoPreco.value = true
    erroPreco.value = null
    try {
        await store.salvarTabelaPreco({
            id: formPreco.value.id,
            profissionalId: formPreco.value.profissionalId || null,
            valorSugerido: valor,
        })
        modalPreco.value = false
    } catch (e: any) {
        erroPreco.value = e?.response?.data?.mensagem ?? "Erro ao salvar preço."
    } finally {
        salvandoPreco.value = false
    }
}

async function inativarPreco(id: number) {
    try {
        await store.inativarTabelaPreco(id)
    } catch { /* silencioso — lista recarrega mesmo assim */ }
}

// ── Taxa por forma de pagamento ───────────────────────────────────────────────

// Mantém strings para edição inline (AppInputDecimal emite string)
const taxaStrings = ref<Record<number, string>>({})

watch(() => store.configTaxa, (lista) => {
    for (const item of lista) {
        if (!(item.formaPagamentoId in taxaStrings.value)) {
            taxaStrings.value[item.formaPagamentoId] = item.taxaPercentual.toFixed(3)
        }
    }
}, { immediate: true })

const salvandoTaxa = ref<Record<number, boolean>>({})

async function salvarTaxaInline(item: typeof store.configTaxa[0]) {
    if (item.formaPagamentoId == null) return
    const taxa = parseFloat(taxaStrings.value[item.formaPagamentoId] ?? "0") || 0
    salvandoTaxa.value[item.formaPagamentoId] = true
    try {
        await store.salvarConfigTaxa({
            id: item.id ?? null,
            formaPagamentoId: item.formaPagamentoId,
            taxaPercentual: taxa,
            ativo: item.ativo,
        })
    } catch { /* silencioso */ } finally {
        delete salvandoTaxa.value[item.formaPagamentoId]
    }
}

// Opções de profissional para o AppSelect no modal
const opcoesProfissionais = computed(() => [
    { value: "" as string, label: "Padrão do estabelecimento" },
    ...profissionais.value.map(p => ({ value: p.usuarioId, label: p.nomeCompleto })),
])

function fmt(v: number): string {
    return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}
</script>

<template>
    <div class="fin-config">
        <!-- ── Seção 1: Tabela de preços ───────────────────────────────────── -->
        <header class="secao-head">
            <h2 class="ds-section-title">Tabela de preços de consulta</h2>
            <p class="secao-sub">Valor sugerido ao abrir o check-in. Um preço por profissional + um padrão do estabelecimento.</p>
        </header>

        <div class="secao-acao-bar">
            <div class="busca-wrapper">
                <i class="fa-solid fa-magnifying-glass busca-ic" aria-hidden="true"></i>
                <input
                    v-model="buscaInput"
                    type="text"
                    class="form-input busca-input"
                    placeholder="Buscar profissional…"
                />
            </div>
            <AppButton icon="fa-solid fa-plus" @click="abrirNovoPreco">Novo preço</AppButton>
        </div>

        <div v-if="store.carregando" class="carregando-msg">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando…
        </div>

        <AppEmptyState
            v-else-if="store.tabelaPreco.length === 0"
            icone="fa-solid fa-coins"
            titulo="Nenhum preço cadastrado"
            descricao="Configure o valor sugerido para consultas particulares."
        >
            <AppButton icon="fa-solid fa-plus" @click="abrirNovoPreco">Cadastrar primeiro preço</AppButton>
        </AppEmptyState>

        <table v-else class="tabela">
            <thead>
                <tr>
                    <th>Profissional / escopo</th>
                    <th class="col-valor">Valor sugerido</th>
                    <th class="col-ativo">Ativo</th>
                    <th class="col-acoes"></th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="item in store.tabelaPreco" :key="item.id" :class="{ 'linha-inativa': !item.ativo }">
                    <td>
                        <span v-if="item.profissionalNome" class="prof-nome">{{ item.profissionalNome }}</span>
                        <span v-else class="padrao-tag">Padrão do estabelecimento</span>
                    </td>
                    <td class="col-valor">{{ fmt(item.valorSugerido) }}</td>
                    <td class="col-ativo">
                        <span :class="['status-dot', item.ativo ? 'status-dot--on' : 'status-dot--off']"></span>
                        {{ item.ativo ? "Sim" : "Não" }}
                    </td>
                    <td class="col-acoes">
                        <button class="btn-icon btn-icon-editar" title="Editar" @click="abrirEditarPreco(item)">
                            <i class="fa-solid fa-pen-to-square"></i>
                        </button>
                        <button
                            v-if="item.ativo"
                            class="btn-icon btn-icon-excluir"
                            title="Inativar"
                            @click="inativarPreco(item.id)"
                        >
                            <i class="fa-solid fa-ban"></i>
                        </button>
                    </td>
                </tr>
            </tbody>
        </table>

        <!-- Divider -->
        <hr class="secao-divider" />

        <!-- ── Seção 2: Taxa por forma de pagamento ─────────────────────── -->
        <header class="secao-head">
            <h2 class="ds-section-title">Taxa por forma de pagamento</h2>
            <p class="secao-sub">A taxa é informativa ("você recebe R$ X") e não altera o valor cobrado do paciente.</p>
        </header>

        <div v-if="store.configTaxa.length === 0 && !store.carregando" class="msg-vazio-taxa">
            <i class="fa-solid fa-circle-info"></i>
            Nenhuma forma de pagamento ativa. Cadastre formas em Configurações &rsaquo; Financeiro.
        </div>

        <table v-else-if="store.configTaxa.length > 0" class="tabela">
            <thead>
                <tr>
                    <th>Forma de pagamento</th>
                    <th class="col-taxa">Taxa (%)</th>
                    <th class="col-ativo">Ativo</th>
                    <th class="col-acoes"></th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="item in store.configTaxa" :key="item.formaPagamentoId">
                    <td>{{ item.formaPagamentoNome }}</td>
                    <td class="col-taxa">
                        <AppInputDecimal
                            v-model="taxaStrings[item.formaPagamentoId]"
                            :decimals="3"
                            placeholder="0,000"
                            class="taxa-input"
                        />
                    </td>
                    <td class="col-ativo">
                        <input v-model="item.ativo" type="checkbox" />
                    </td>
                    <td class="col-acoes">
                        <AppButton
                            variant="secondary"
                            size="sm"
                            :loading="!!salvandoTaxa[item.formaPagamentoId]"
                            @click="salvarTaxaInline(item)"
                        >
                            Salvar
                        </AppButton>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>

    <!-- Modal: Novo/editar preço -->
    <AppModal
        :aberto="modalPreco"
        :titulo="formPreco.modoEdicao ? 'Editar preço' : 'Novo preço'"
        largura="sm"
        @fechar="modalPreco = false"
    >
        <AppField label="Profissional">
            <AppSelect
                v-model="formPreco.profissionalId"
                :options="opcoesProfissionais"
            />
        </AppField>

        <AppField label="Valor sugerido (R$)">
            <AppInputDecimal v-model="formPreco.valorSugerido" placeholder="0,00" />
        </AppField>

        <p v-if="erroPreco" class="msg-erro">{{ erroPreco }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="modalPreco = false">Cancelar</AppButton>
            <AppButton :loading="salvandoPreco" @click="salvarPreco">Salvar</AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.fin-config {
    display: flex;
    flex-direction: column;
    gap: var(--spacing-4);
}

.secao-head {
    display: flex;
    flex-direction: column;
    gap: var(--spacing-1);
}

.secao-sub {
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
    margin: 0;
}

.secao-acao-bar {
    display: flex;
    align-items: center;
    gap: var(--spacing-2);
    justify-content: space-between;
}

.busca-wrapper {
    position: relative;
    flex: 1;
    max-width: 320px;
}

.busca-ic {
    position: absolute;
    left: var(--spacing-3);
    top: 50%;
    transform: translateY(-50%);
    color: hsl(var(--muted-foreground));
    font-size: var(--text-sm);
    pointer-events: none;
}

.busca-input {
    padding-left: calc(var(--spacing-3) + 1.2em + var(--spacing-1));
    width: 100%;
}

.carregando-msg {
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
    display: flex;
    align-items: center;
    gap: var(--spacing-2);
}

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: var(--text-sm);
}

.tabela th {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--muted-foreground));
    text-align: left;
    padding: var(--spacing-2);
    border-bottom: 1px solid hsl(var(--border));
}

.tabela td {
    padding: var(--spacing-2);
    color: hsl(var(--foreground));
    border-bottom: 1px solid hsl(var(--border));
    vertical-align: middle;
}

.tabela tr:last-child td { border-bottom: none; }

.col-valor  { width: 140px; text-align: right; }
.col-taxa   { width: 120px; }
.col-ativo  { width: 80px; }
.col-acoes  { width: 80px; text-align: right; }

.linha-inativa td { opacity: 0.5; }

.prof-nome  { font-weight: var(--font-weight-semibold); }
.padrao-tag {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--primary));
    background: hsl(var(--primary) / 0.08);
    padding: 2px 8px;
    border-radius: 999px;
}

.status-dot {
    display: inline-block;
    width: 6px;
    height: 6px;
    border-radius: 50%;
    margin-right: 4px;
    vertical-align: middle;
}
.status-dot--on  { background: hsl(160 79% 39%); }
.status-dot--off { background: hsl(var(--muted-foreground)); }

.taxa-input { max-width: 90px; }

.secao-divider {
    border: none;
    border-top: 1px solid hsl(var(--border));
    margin: var(--spacing-2) 0;
}

.msg-vazio-taxa {
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
    display: flex;
    align-items: center;
    gap: var(--spacing-2);
}

.msg-erro {
    font-size: var(--text-sm);
    color: hsl(var(--destructive));
    margin: 0;
}
</style>
