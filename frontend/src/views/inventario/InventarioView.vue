<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { inventarioService, type ItemInventario, type MovimentacaoEstoque } from "@/services/inventarioService"
import { AppButton, AppField, AppInput, AppModal, AppSelect } from "@/components/ui"

const itens = ref<ItemInventario[]>([])
const movimentacoes = ref<MovimentacaoEstoque[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)

const filtroCategoria = ref("")
const filtroAbaixoMinimo = ref(false)
const filtroInativos = ref(false)

const modalCriar = ref(false)
const formCriar = ref({
    codigo: "",
    nome: "",
    categoria: "",
    unidadeMedida: "",
    quantidadeInicial: 0,
    quantidadeMinima: 0,
})
const erroCriar = ref<string | null>(null)
const salvando = ref(false)

const itemEditando = ref<ItemInventario | null>(null)
const formEditar = ref({ nome: "", categoria: "", unidadeMedida: "", quantidadeMinima: 0 })
const erroEditar = ref<string | null>(null)

const itemMovimentando = ref<ItemInventario | null>(null)
const formMov = ref({ tipo: "Entrada" as "Entrada" | "Saida", quantidade: 0, observacao: "" })
const erroMov = ref<string | null>(null)

const itemHistorico = ref<ItemInventario | null>(null)
const carregandoHist = ref(false)

const categorias = computed(() => [...new Set(itens.value.map(i => i.categoria))].sort())

const itensFiltrados = computed(() => {
    return itens.value.filter(item => {
        if (filtroCategoria.value && item.categoria !== filtroCategoria.value) return false
        if (filtroAbaixoMinimo.value && !item.estoqueAbaixoMinimo) return false
        if (!filtroInativos.value && !item.ativo) return false
        return true
    })
})

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        itens.value = await inventarioService.listarItens({ apenasAtivos: filtroInativos.value ? undefined : true })
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar inventário."
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)

function abrirModalCriar() {
    formCriar.value = { codigo: "", nome: "", categoria: "", unidadeMedida: "", quantidadeInicial: 0, quantidadeMinima: 0 }
    erroCriar.value = null
    modalCriar.value = true
}

async function salvarCriar() {
    salvando.value = true
    erroCriar.value = null
    try {
        await inventarioService.criarItem(formCriar.value)
        modalCriar.value = false
        await carregar()
    } catch (e: any) {
        erroCriar.value = e?.response?.data?.mensagem ?? "Erro ao criar item."
    } finally {
        salvando.value = false
    }
}

function abrirEditar(item: ItemInventario) {
    itemEditando.value = item
    formEditar.value = { nome: item.nome, categoria: item.categoria, unidadeMedida: item.unidadeMedida, quantidadeMinima: item.quantidadeMinima }
    erroEditar.value = null
}

async function salvarEditar() {
    if (!itemEditando.value) return
    salvando.value = true
    erroEditar.value = null
    try {
        await inventarioService.atualizarItem(itemEditando.value.id, formEditar.value)
        itemEditando.value = null
        await carregar()
    } catch (e: any) {
        erroEditar.value = e?.response?.data?.mensagem ?? "Erro ao atualizar item."
    } finally {
        salvando.value = false
    }
}

async function inativar(item: ItemInventario) {
    if (!confirm(`Inativar "${item.nome}"?`)) return
    try {
        await inventarioService.inativarItem(item.id)
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao inativar.")
    }
}

function abrirMovimentacao(item: ItemInventario) {
    itemMovimentando.value = item
    formMov.value = { tipo: "Entrada", quantidade: 0, observacao: "" }
    erroMov.value = null
}

async function salvarMovimentacao() {
    if (!itemMovimentando.value) return
    salvando.value = true
    erroMov.value = null
    try {
        await inventarioService.registrarMovimentacao({
            itemInventarioId: itemMovimentando.value.id,
            tipo: formMov.value.tipo,
            quantidade: formMov.value.quantidade,
            observacao: formMov.value.observacao || null,
        })
        itemMovimentando.value = null
        await carregar()
    } catch (e: any) {
        erroMov.value = e?.response?.data?.mensagem ?? "Erro ao registrar movimentação."
    } finally {
        salvando.value = false
    }
}

async function verHistorico(item: ItemInventario) {
    itemHistorico.value = item
    carregandoHist.value = true
    movimentacoes.value = []
    try {
        movimentacoes.value = await inventarioService.listarMovimentacoes({ itemInventarioId: item.id, limite: 50 })
    } catch {
        movimentacoes.value = []
    } finally {
        carregandoHist.value = false
    }
}

function formatarQtd(n: number) {
    return n % 1 === 0 ? n.toString() : n.toFixed(3).replace(/\.?0+$/, "")
}

function formatarData(s: string) {
    return new Date(s).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" })
}
</script>

<template>
    <main class="app-page inventario">
        <header class="page-header">
            <div>
                <h1 class="page-titulo">Estoque</h1>
                <p class="page-sub">Cadastre e acompanhe os produtos utilizados na clínica ou consultório.</p>
            </div>
            <AppButton icon="fa-solid fa-plus" @click="abrirModalCriar">Novo item</AppButton>
        </header>

        <section class="kpis">
            <div class="kpi">
                <span class="kpi-label">Itens em estoque</span>
                <span class="kpi-valor">{{ itens.filter(i => i.ativo).length }}</span>
            </div>
            <div class="kpi" :class="{ 'kpi-alerta': itens.some(i => i.estoqueAbaixoMinimo) }">
                <span class="kpi-label">Produtos abaixo do mínimo</span>
                <span class="kpi-valor" :class="{ vermelho: itens.some(i => i.estoqueAbaixoMinimo) }">
                    {{ itens.filter(i => i.estoqueAbaixoMinimo).length }}
                </span>
            </div>
            <div class="kpi">
                <span class="kpi-label">Categorias</span>
                <span class="kpi-valor">{{ categorias.length }}</span>
            </div>
        </section>

        <section class="filtros">
            <select v-model="filtroCategoria">
                <option value="">Todas categorias</option>
                <option v-for="cat in categorias" :key="cat" :value="cat">{{ cat }}</option>
            </select>
            <label>
                <input type="checkbox" v-model="filtroAbaixoMinimo" />
                Apenas abaixo do mínimo
            </label>
            <label>
                <input type="checkbox" v-model="filtroInativos" @change="carregar" />
                Incluir inativos
            </label>
        </section>

        <p v-if="erro" class="erro">{{ erro }}</p>
        <p v-if="carregando" class="info">Carregando...</p>

        <table v-if="!carregando && itensFiltrados.length > 0">
            <thead>
                <tr>
                    <th>Código</th>
                    <th>Nome</th>
                    <th>Categoria</th>
                    <th>Unidade</th>
                    <th>Qtd. atual</th>
                    <th>Qtd. mín.</th>
                    <th>Status</th>
                    <th>Ações</th>
                </tr>
            </thead>
            <tbody>
                <tr
                    v-for="item in itensFiltrados"
                    :key="item.id"
                    :class="{ alerta: item.estoqueAbaixoMinimo, inativo: !item.ativo }"
                >
                    <td>{{ item.codigo }}</td>
                    <td>{{ item.nome }}</td>
                    <td>{{ item.categoria }}</td>
                    <td>{{ item.unidadeMedida }}</td>
                    <td :class="{ 'qtd-baixa': item.estoqueAbaixoMinimo }">
                        {{ formatarQtd(item.quantidadeAtual) }}
                        <span v-if="item.estoqueAbaixoMinimo" class="badge-alerta">⚠</span>
                    </td>
                    <td>{{ formatarQtd(item.quantidadeMinima) }}</td>
                    <td>
                        <span :class="item.ativo ? 'badge-ativo' : 'badge-inativo'">
                            {{ item.ativo ? "Ativo" : "Inativo" }}
                        </span>
                    </td>
                    <td class="acoes">
                        <button class="btn-icon" @click="abrirMovimentacao(item)" :disabled="!item.ativo" title="Registrar movimentação">±</button>
                        <button class="btn-icon" @click="verHistorico(item)" title="Ver histórico">📋</button>
                        <button class="btn-icon btn-icon-editar" @click="abrirEditar(item)" :disabled="!item.ativo" title="Editar">✏</button>
                        <button v-if="item.ativo" class="btn-icon btn-icon-excluir" @click="inativar(item)" title="Inativar">✕</button>
                    </td>
                </tr>
            </tbody>
        </table>
        <p v-else-if="!carregando" class="vazio">Nenhum item encontrado.</p>

        <!-- Modal criar item -->
        <AppModal :aberto="modalCriar" titulo="Novo item de inventário" @fechar="modalCriar = false">
            <AppField label="Código" required>
                <AppInput v-model="formCriar.codigo" />
            </AppField>
            <AppField label="Nome" required>
                <AppInput v-model="formCriar.nome" />
            </AppField>
            <AppField label="Categoria" required>
                <AppInput v-model="formCriar.categoria" list="cats-criar" />
                <datalist id="cats-criar">
                    <option v-for="c in categorias" :key="c" :value="c" />
                </datalist>
            </AppField>
            <AppField label="Unidade de medida" required>
                <AppInput v-model="formCriar.unidadeMedida" placeholder="ex: un, kg, L" />
            </AppField>
            <AppField label="Quantidade inicial">
                <AppInput v-model="formCriar.quantidadeInicial" type="number" :min="0" :step="0.001" />
            </AppField>
            <AppField label="Quantidade mínima" required>
                <AppInput v-model="formCriar.quantidadeMinima" type="number" :min="0" :step="0.001" />
            </AppField>
            <p v-if="erroCriar" class="msg-erro">{{ erroCriar }}</p>

            <template #rodape>
                <AppButton variant="secondary" @click="modalCriar = false">Cancelar</AppButton>
                <AppButton :disabled="salvando" :loading="salvando" @click="salvarCriar">Criar</AppButton>
            </template>
        </AppModal>

        <!-- Modal editar item -->
        <AppModal :aberto="!!itemEditando" :titulo="`Editar — ${itemEditando?.nome ?? ''}`" @fechar="itemEditando = null">
            <AppField label="Nome" required>
                <AppInput v-model="formEditar.nome" />
            </AppField>
            <AppField label="Categoria" required>
                <AppInput v-model="formEditar.categoria" list="cats-editar" />
                <datalist id="cats-editar">
                    <option v-for="c in categorias" :key="c" :value="c" />
                </datalist>
            </AppField>
            <AppField label="Unidade de medida" required>
                <AppInput v-model="formEditar.unidadeMedida" />
            </AppField>
            <AppField label="Quantidade mínima" required>
                <AppInput v-model="formEditar.quantidadeMinima" type="number" :min="0" :step="0.001" />
            </AppField>
            <p v-if="erroEditar" class="msg-erro">{{ erroEditar }}</p>

            <template #rodape>
                <AppButton variant="secondary" @click="itemEditando = null">Cancelar</AppButton>
                <AppButton :disabled="salvando" :loading="salvando" @click="salvarEditar">Salvar</AppButton>
            </template>
        </AppModal>

        <!-- Modal movimentação -->
        <AppModal :aberto="!!itemMovimentando" :titulo="`Movimentação — ${itemMovimentando?.nome ?? ''}`" @fechar="itemMovimentando = null">
            <p v-if="itemMovimentando" class="info-estoque">
                Estoque atual: <strong>{{ formatarQtd(itemMovimentando.quantidadeAtual) }} {{ itemMovimentando.unidadeMedida }}</strong>
            </p>
            <AppField label="Tipo" required>
                <AppSelect v-model="formMov.tipo">
                    <option value="Entrada">Entrada</option>
                    <option value="Saida">Saída</option>
                </AppSelect>
            </AppField>
            <AppField label="Quantidade" required>
                <AppInput v-model="formMov.quantidade" type="number" :min="0.001" :step="0.001" />
            </AppField>
            <AppField label="Observação">
                <AppInput v-model="formMov.observacao" placeholder="Opcional" />
            </AppField>
            <p v-if="erroMov" class="msg-erro">{{ erroMov }}</p>

            <template #rodape>
                <AppButton variant="secondary" @click="itemMovimentando = null">Cancelar</AppButton>
                <AppButton :disabled="salvando" :loading="salvando" @click="salvarMovimentacao">Registrar</AppButton>
            </template>
        </AppModal>

        <!-- Modal histórico -->
        <AppModal :aberto="!!itemHistorico" :titulo="`Histórico — ${itemHistorico?.nome ?? ''}`" largura="lg" @fechar="itemHistorico = null">
            <p v-if="carregandoHist" class="info">Carregando...</p>
            <table v-else-if="movimentacoes.length > 0" class="tabela-hist">
                <thead>
                    <tr>
                        <th>Data</th>
                        <th>Tipo</th>
                        <th>Qtd.</th>
                        <th>Antes</th>
                        <th>Depois</th>
                        <th>Observação</th>
                        <th>Usuário</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="m in movimentacoes" :key="m.id">
                        <td>{{ formatarData(m.criadoEm) }}</td>
                        <td :class="m.tipo === 'Entrada' ? 'entrada' : 'saida'">{{ m.tipo }}</td>
                        <td>{{ formatarQtd(m.quantidade) }}</td>
                        <td>{{ formatarQtd(m.quantidadeAnterior) }}</td>
                        <td>{{ formatarQtd(m.quantidadeApos) }}</td>
                        <td>{{ m.observacao ?? "—" }}</td>
                        <td>{{ m.usuarioNome }}</td>
                    </tr>
                </tbody>
            </table>
            <p v-else class="vazio">Sem movimentações registradas.</p>

            <template #rodape>
                <AppButton variant="secondary" @click="itemHistorico = null">Fechar</AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.page-header {
    display: flex; justify-content: space-between; align-items: flex-start;
    margin-bottom: 1.25rem;
}
.page-titulo { font-size: 1.5rem; font-weight: 800; margin: 0 0 0.2rem; }
.page-sub    { margin: 0; color: var(--text-muted); font-size: 0.875em; }

.kpis { display: flex; gap: 1rem; flex-wrap: wrap; margin-bottom: 1.5rem; }
.kpi {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 0.9rem 1.1rem; min-width: 200px;
    display: flex; flex-direction: column; gap: 0.25rem;
}
.kpi-alerta { border-color: #fcd34d; background: #fffbeb; }
.kpi-label  { font-size: 0.78em; color: var(--text-muted); }
.kpi-valor  { font-size: 1.6rem; font-weight: 700; line-height: 1; }
.vermelho   { color: var(--danger); }
.filtros { display: flex; gap: 1rem; align-items: center; margin-bottom: 1rem; flex-wrap: wrap; }
.filtros select { padding: 0.3rem 0.6rem; border: 1px solid #ccc; border-radius: 4px; }
.filtros label { display: flex; align-items: center; gap: 0.4rem; cursor: pointer; }

table { width: 100%; border-collapse: collapse; font-size: 0.9em; }
th { background: #f3f4f6; text-align: left; padding: 0.5rem 0.75rem; border-bottom: 2px solid #e5e7eb; }
td { padding: 0.5rem 0.75rem; border-bottom: 1px solid #f0f0f0; vertical-align: middle; }
tr.alerta { background: #fffbeb; }
tr.inativo { opacity: 0.55; }
tr:hover { background: #f9fafb; }

.qtd-baixa { color: #b45309; font-weight: 600; }
.badge-alerta { color: #d97706; font-size: 0.85em; }
.badge-ativo { background: #d1fae5; color: #065f46; padding: 0.15rem 0.5rem; border-radius: 999px; font-size: 0.8em; }
.badge-inativo { background: #f3f4f6; color: #6b7280; padding: 0.15rem 0.5rem; border-radius: 999px; font-size: 0.8em; }

.acoes { display: flex; gap: 0.3rem; align-items: center; }
.info-estoque { margin: 0; color: #374151; }
.entrada { color: #059669; font-weight: 600; }
.saida { color: #dc2626; font-weight: 600; }
.tabela-hist { width: 100%; border-collapse: collapse; font-size: 0.875em; }
.tabela-hist th { background: #f3f4f6; text-align: left; padding: 0.4rem 0.6rem; border-bottom: 2px solid #e5e7eb; }
.tabela-hist td { padding: 0.4rem 0.6rem; border-bottom: 1px solid #f0f0f0; }

.msg-erro { color: hsl(var(--error)); font-size: 0.875em; margin: 0; }
.erro { color: #b00020; font-size: 0.9em; }
.info { color: #6b7280; }
.vazio { color: #9ca3af; font-style: italic; }
</style>
