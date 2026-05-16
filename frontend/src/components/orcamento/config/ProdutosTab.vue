<script setup lang="ts">
/**
 * Aba "Produtos" — config-orcamento.
 */
import { ref, computed, onMounted, watch } from "vue"
import {
    AppStatCard, AppSearchInput, AppFilterPills, AppDrawer, AppField, AppInput,
    AppSelect, AppCheckbox, AppButton, AppStatusPill, AppPagination, AppEmptyState,
    AppToast, AppConfirmDialog,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { formatarMoedaBrl } from "@/utils/format"
import {
    orcamentoCatalogoService,
    type CatalogoProduto,
    type CatalogoProdutoPayload,
    type TipoOrcamentoProduto,
} from "@/services/orcamentoCatalogoService"

const emit = defineEmits<{ (e: "contagem", v: number): void }>()

const carregando = ref(false)
const lista = ref<CatalogoProduto[]>([])
const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
type FiltroTipo = "todos" | TipoOrcamentoProduto
const filtroTipo = ref<FiltroTipo>("todos")
const pagina = ref(1)
const tamanho = ref(20)

const drawerAberto = ref(false)
const idEditando = ref<number | null>(null)
const form = ref<CatalogoProdutoPayload>({
    nome: "", descricao: null, valorReferencia: null, usoUnico: false,
    tipo: "Outros", marca: null, unidade: "un", fornecedorNome: null, codigoSku: null,
})

// Toast e confirmação (substituem window.alert/confirm).
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}
const confirmacao = ref<{ aberto: boolean, alvo: CatalogoProduto | null, executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

const opcoesTipo = [
    { value: "Outros", label: "Outros" },
    { value: "OPME", label: "OPME" },
    { value: "Descartavel", label: "Descartável" },
    { value: "Curativo", label: "Curativo" },
]

const filtrada = computed(() => {
    let l = lista.value
    if (filtroTipo.value !== "todos") l = l.filter(x => x.tipo === filtroTipo.value)
    if (busca.value.trim()) {
        const q = busca.value.trim().toLowerCase()
        l = l.filter(x =>
            x.nome.toLowerCase().includes(q)
            || (x.marca ?? "").toLowerCase().includes(q)
            || (x.codigoSku ?? "").toLowerCase().includes(q),
        )
    }
    return l
})

const total = computed(() => filtrada.value.length)
const inicio = computed(() => (pagina.value - 1) * tamanho.value)
const pagina_itens = computed(() => filtrada.value.slice(inicio.value, inicio.value + tamanho.value))

const totalAtivos = computed(() => lista.value.filter(x => x.ativo).length)
const totalOpme = computed(() => lista.value.filter(x => x.tipo === "OPME").length)
const totalDesc = computed(() => lista.value.filter(x => x.tipo === "Descartavel").length)
const ticketMedio = computed(() => {
    const ativos = lista.value.filter(x => x.ativo && x.valorReferencia)
    if (!ativos.length) return 0
    return ativos.reduce((s, x) => s + (x.valorReferencia ?? 0), 0) / ativos.length
})

async function carregar() {
    carregando.value = true
    try {
        lista.value = await orcamentoCatalogoService.listarProdutos()
        emit("contagem", lista.value.length)
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)
watch(busca, () => { pagina.value = 1 })
watch(filtroTipo, () => { pagina.value = 1 })

function novo() {
    idEditando.value = null
    form.value = {
        nome: "", descricao: null, valorReferencia: null, usoUnico: false,
        tipo: "Outros", marca: null, unidade: "un", fornecedorNome: null, codigoSku: null,
    }
    drawerAberto.value = true
}

function editar(item: CatalogoProduto) {
    idEditando.value = item.id
    form.value = {
        nome: item.nome, descricao: item.descricao,
        valorReferencia: item.valorReferencia, usoUnico: item.usoUnico,
        tipo: item.tipo, marca: item.marca, unidade: item.unidade,
        fornecedorNome: item.fornecedorNome, codigoSku: item.codigoSku,
    }
    drawerAberto.value = true
}

async function salvar() {
    if (!form.value.nome.trim()) { notificar("Nome é obrigatório.", "error"); return }
    try {
        if (idEditando.value === null) {
            await orcamentoCatalogoService.criarProduto(form.value)
            notificar("Produto criado.", "success")
        } else {
            await orcamentoCatalogoService.atualizarProduto(idEditando.value, form.value)
            notificar("Produto atualizado.", "success")
        }
        drawerAberto.value = false
        await carregar()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Falha ao salvar.", "error")
    }
}

function pedirRemocao(item: CatalogoProduto) {
    confirmacao.value = { aberto: true, alvo: item, executando: false }
}

async function executarRemocao() {
    const alvo = confirmacao.value.alvo
    if (!alvo) return
    confirmacao.value.executando = true
    try {
        await orcamentoCatalogoService.removerProduto(alvo.id)
        confirmacao.value = { aberto: false, alvo: null, executando: false }
        notificar("Produto inativado.", "success")
        await carregar()
    } catch (e: any) {
        confirmacao.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Falha ao inativar.", "error")
    }
}
</script>

<template>
    <div class="config-tab">
        <div class="stats-grid">
            <AppStatCard label="Cadastrados" :valor="lista.length" cor="primary" icone="fa-solid fa-boxes-stacked" />
            <AppStatCard label="Ativos" :valor="totalAtivos" cor="success" icone="fa-solid fa-circle-check" />
            <AppStatCard label="OPMEs" :valor="totalOpme" cor="info" icone="fa-solid fa-cog" />
            <AppStatCard label="Descartáveis" :valor="totalDesc" cor="warning" icone="fa-solid fa-syringe" />
            <AppStatCard label="Ticket médio" :valor="formatarMoedaBrl(ticketMedio)" cor="muted" icone="fa-solid fa-coins" />
        </div>

        <div class="toolbar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar por nome, marca, SKU..." />
            <AppFilterPills
                v-model="filtroTipo"
                :opcoes="[
                    { valor: 'todos', label: 'Todos', count: lista.length },
                    { valor: 'OPME', label: 'OPME', count: totalOpme, dot: 'info' },
                    { valor: 'Descartavel', label: 'Descartáveis', count: totalDesc, dot: 'warning' },
                    { valor: 'Curativo', label: 'Curativos' },
                    { valor: 'Outros', label: 'Outros', dot: 'muted' },
                ]"
            />
            <AppButton icon="fa-solid fa-plus" @click="novo">Novo produto</AppButton>
        </div>

        <div v-if="carregando" class="loading">Carregando…</div>
        <AppEmptyState
            v-else-if="!lista.length"
            icone="fa-solid fa-boxes-stacked"
            titulo="Nenhum produto cadastrado"
            descricao="Cadastre OPMEs, descartáveis e curativos para usar em orçamentos."
        >
            <template #acao>
                <AppButton icon="fa-solid fa-plus" @click="novo">Criar primeiro produto</AppButton>
            </template>
        </AppEmptyState>
        <AppEmptyState v-else-if="!pagina_itens.length" icone="fa-solid fa-magnifying-glass" titulo="Nenhum resultado" />
        <div v-else class="table-wrap">
            <table class="table">
                <thead>
                    <tr>
                        <th>Nome</th>
                        <th>Tipo</th>
                        <th>Marca</th>
                        <th>Unid.</th>
                        <th class="num">Valor ref.</th>
                        <th>Status</th>
                        <th class="acoes-col"></th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="item in pagina_itens" :key="item.id">
                        <td>
                            <div class="cell-desc">{{ item.nome }}</div>
                            <div v-if="item.codigoSku" class="cell-sub">SKU: {{ item.codigoSku }}</div>
                        </td>
                        <td>{{ item.tipo }}</td>
                        <td>{{ item.marca ?? "—" }}</td>
                        <td>{{ item.unidade }}</td>
                        <td class="num">{{ item.valorReferencia ? formatarMoedaBrl(item.valorReferencia) : "—" }}</td>
                        <td><AppStatusPill :label="item.ativo ? 'Ativo' : 'Inativo'" :variante="item.ativo ? 'success' : 'muted'" /></td>
                        <td class="acoes-col">
                            <button class="btn-icon btn-icon-editar" title="Editar" @click="editar(item)">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button v-if="item.ativo" class="btn-icon btn-icon-excluir" title="Inativar" @click="pedirRemocao(item)">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </td>
                    </tr>
                </tbody>
            </table>
            <AppPagination v-model:pagina="pagina" v-model:tamanho="tamanho" :total="total" />
        </div>

        <AppDrawer
            :aberto="drawerAberto"
            :titulo="idEditando === null ? 'Novo produto' : 'Editar produto'"
            :largura="560"
            @fechar="drawerAberto = false"
        >
            <AppField label="Nome" required>
                <AppInput v-model="form.nome" placeholder="Ex: Prótese mamária 350cc" />
            </AppField>
            <AppField label="Descrição">
                <AppInput v-model="form.descricao" placeholder="Detalhes opcionais" />
            </AppField>
            <div class="grid-2">
                <AppField label="Tipo">
                    <AppSelect v-model="form.tipo" :options="opcoesTipo" />
                </AppField>
                <AppField label="Unidade">
                    <AppInput v-model="form.unidade" placeholder="un, cx, kit..." />
                </AppField>
            </div>
            <div class="grid-2">
                <AppField label="Marca">
                    <AppInput v-model="form.marca" placeholder="Ex: Ethicon" />
                </AppField>
                <AppField label="SKU / código">
                    <AppInput v-model="form.codigoSku" placeholder="Ex: OPM-001" />
                </AppField>
            </div>
            <AppField label="Fornecedor">
                <AppInput v-model="form.fornecedorNome" placeholder="Ex: Johnson & Johnson" />
            </AppField>
            <AppField label="Valor de referência (R$)">
                <AppInput
                    type="number"
                    step="0.01"
                    :model-value="form.valorReferencia ?? ''"
                    @update:model-value="(v: any) => form.valorReferencia = v === '' ? null : Number(v)"
                />
            </AppField>
            <AppField>
                <AppCheckbox v-model="form.usoUnico" label="Uso único (cobrado uma vez por orçamento, mesmo se aparecer em múltiplos procedimentos)" />
            </AppField>

            <template #rodape>
                <AppButton variant="secondary" @click="drawerAberto = false">Cancelar</AppButton>
                <AppButton @click="salvar">Salvar</AppButton>
            </template>
        </AppDrawer>

        <AppConfirmDialog
            v-model:aberto="confirmacao.aberto"
            titulo="Inativar produto?"
            :mensagem="confirmacao.alvo ? `Deseja inativar “${confirmacao.alvo.nome}”?` : ''"
            confirmar-rotulo="Inativar"
            variante="danger"
            icone="fa-solid fa-trash"
            :executando="confirmacao.executando"
            @confirmar="executarRemocao"
        />

        <AppToast
            v-if="toast"
            :mensagem="toast.mensagem"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </div>
</template>

<style scoped>
.config-tab { display: flex; flex-direction: column; gap: 16px; }
.stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; }
.toolbar { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
.toolbar > :first-child { flex: 1 1 280px; min-width: 220px; }
.loading { padding: 32px; text-align: center; color: hsl(var(--secondary) / 0.7); }
.table-wrap {
    background: white; border-radius: 14px;
    border: 1px solid hsl(var(--secondary) / 0.1); overflow: hidden;
}
.table { width: 100%; border-collapse: collapse; font-size: 14px; }
.table thead th {
    text-align: left; padding: 12px 16px;
    background: hsl(var(--secondary) / 0.04);
    color: hsl(var(--secondary) / 0.7);
    font-weight: 600; font-size: 12px; text-transform: uppercase; letter-spacing: 0.04em;
}
.table tbody td { padding: 12px 16px; border-top: 1px solid hsl(var(--secondary) / 0.08); }
.table .num { text-align: right; }
.cell-desc { font-weight: 600; }
.cell-sub { font-size: 12px; color: hsl(var(--secondary) / 0.6); }
.acoes-col { width: 90px; text-align: right; white-space: nowrap; }
.acoes-col .btn-icon + .btn-icon { margin-left: 4px; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
</style>
