<script setup lang="ts">
import { ref, watch, onMounted, computed } from "vue"
import {
    AppSearchInput, AppButton, AppEmptyState, AppPagination, AppDrawer,
    AppField, AppInput, AppSelect, AppStatusPill,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { inventarioService, type ItemInventario } from "@/services/inventarioService"
import {
    estoqueCadastrosService,
    type CategoriaEstoque,
    type FabricanteEstoque,
    type FornecedorEstoque,
    type LocalEstoque,
} from "@/services/estoqueCadastrosService"
import { formatarMoedaBrl } from "@/utils/format"

// ─── Estado da lista ─────────────────────────────────────────────────
const itens = ref<ItemInventario[]>([])
const total = ref(0)
const pagina = ref(1)
const tamanho = ref(20)
const carregando = ref(false)
const erro = ref<string | null>(null)

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
const apenasAtivos = ref(true)

// ─── Catálogos (carregados 1× ao montar) ─────────────────────────────
const categorias = ref<CategoriaEstoque[]>([])
const fabricantes = ref<FabricanteEstoque[]>([])
const fornecedores = ref<FornecedorEstoque[]>([])
const locais = ref<LocalEstoque[]>([])

async function carregarCatalogos() {
    try {
        const [c, fb, fn, lc] = await Promise.all([
            estoqueCadastrosService.categorias.listar({ tamanho: 200 }),
            estoqueCadastrosService.fabricantes.listar({ tamanho: 200 }),
            estoqueCadastrosService.fornecedores.listar({ tamanho: 200 }),
            estoqueCadastrosService.locais.listar({ tamanho: 200 }),
        ])
        categorias.value = c.itens
        fabricantes.value = fb.itens
        fornecedores.value = fn.itens
        locais.value = lc.itens
    } catch {
        // silencioso — selects ficam vazios e o usuário cria pela aba dedicada.
    }
}

// ─── Drawer (criar/editar) ───────────────────────────────────────────
const drawerAberto = ref(false)
const editando = ref<ItemInventario | null>(null)
const form = ref({
    codigo: "",
    nome: "",
    categoriaId: 0,
    fabricanteId: 0,
    fornecedorPadraoId: 0,
    localPadraoId: 0,
    unidadeMedida: "",
    quantidadeInicial: 0,
    quantidadeMinima: 0,
    custoUnitarioInicial: 0,
    custoUnitario: 0,
})
const erroForm = ref<string | null>(null)
const salvando = ref(false)

const ehEdicao = computed(() => editando.value !== null)

function abrirCriar() {
    editando.value = null
    form.value = {
        codigo: "",
        nome: "",
        categoriaId: 0,
        fabricanteId: 0,
        fornecedorPadraoId: 0,
        localPadraoId: 0,
        unidadeMedida: "",
        quantidadeInicial: 0,
        quantidadeMinima: 0,
        custoUnitarioInicial: 0,
        custoUnitario: 0,
    }
    erroForm.value = null
    drawerAberto.value = true
}

function abrirEditar(it: ItemInventario) {
    editando.value = it
    form.value = {
        codigo: it.codigo,
        nome: it.nome,
        categoriaId: it.categoriaId ?? 0,
        fabricanteId: it.fabricanteId ?? 0,
        fornecedorPadraoId: it.fornecedorPadraoId ?? 0,
        localPadraoId: it.localPadraoId ?? 0,
        unidadeMedida: it.unidadeMedida,
        quantidadeInicial: 0,
        quantidadeMinima: it.quantidadeMinima,
        custoUnitarioInicial: 0,
        custoUnitario: it.custoUnitario ?? 0,
    }
    erroForm.value = null
    drawerAberto.value = true
}

async function salvar() {
    erroForm.value = null
    if (!ehEdicao.value && !form.value.codigo.trim()) { erroForm.value = "Código é obrigatório."; return }
    if (!form.value.nome.trim()) { erroForm.value = "Nome é obrigatório."; return }
    if (!form.value.categoriaId) { erroForm.value = "Selecione uma categoria."; return }
    if (!form.value.unidadeMedida.trim()) { erroForm.value = "Unidade de medida é obrigatória."; return }
    if (form.value.quantidadeMinima < 0) { erroForm.value = "Quantidade mínima não pode ser negativa."; return }
    if (form.value.quantidadeInicial > 0 && form.value.custoUnitarioInicial <= 0) {
        erroForm.value = "Custo unitário inicial é obrigatório quando há quantidade inicial."
        return
    }

    salvando.value = true
    try {
        const fabricanteId      = form.value.fabricanteId      || undefined
        const fornecedorPadraoId = form.value.fornecedorPadraoId || undefined
        const localPadraoId     = form.value.localPadraoId     || undefined
        const custoUnitario     = form.value.custoUnitario > 0 ? form.value.custoUnitario : undefined

        if (editando.value) {
            await inventarioService.atualizarItem(editando.value.id, {
                nome: form.value.nome,
                categoriaId: form.value.categoriaId,
                fabricanteId,
                fornecedorPadraoId,
                localPadraoId,
                unidadeMedida: form.value.unidadeMedida,
                quantidadeMinima: form.value.quantidadeMinima,
                custoUnitario,
            })
        } else {
            await inventarioService.criarItem({
                codigo: form.value.codigo,
                nome: form.value.nome,
                categoriaId: form.value.categoriaId,
                fabricanteId,
                fornecedorPadraoId,
                localPadraoId,
                unidadeMedida: form.value.unidadeMedida,
                quantidadeInicial: form.value.quantidadeInicial,
                quantidadeMinima: form.value.quantidadeMinima,
                custoUnitarioInicial: form.value.quantidadeInicial > 0 ? form.value.custoUnitarioInicial : undefined,
                custoUnitario,
            })
        }
        drawerAberto.value = false
        await carregar()
    } catch (e: any) {
        erroForm.value = e?.response?.data?.mensagem ?? "Erro ao salvar produto."
    } finally {
        salvando.value = false
    }
}

async function inativar(it: ItemInventario) {
    if (!confirm(`Inativar "${it.nome}"?`)) return
    try {
        await inventarioService.inativarItem(it.id)
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao inativar.")
    }
}

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const pg = await inventarioService.listarItens({
            apenasAtivos: apenasAtivos.value,
            pagina: pagina.value,
            tamanho: tamanho.value,
        })
        // Busca livre é client-side aqui — endpoint de itens não tem search (reuso).
        const buscaLower = (busca.value || "").trim().toLowerCase()
        if (buscaLower) {
            itens.value = pg.itens.filter(
                i => i.nome.toLowerCase().includes(buscaLower) ||
                     i.codigo.toLowerCase().includes(buscaLower),
            )
        } else {
            itens.value = pg.itens
        }
        total.value = pg.total
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar produtos."
    } finally {
        carregando.value = false
    }
}

watch(busca, () => { pagina.value = 1; carregar() })
watch([pagina, tamanho, apenasAtivos], () => carregar(), { immediate: false })

onMounted(async () => {
    await Promise.all([carregar(), carregarCatalogos()])
})

function formatarQtd(n: number) {
    return n % 1 === 0 ? n.toString() : n.toFixed(3).replace(/\.?0+$/, "")
}
</script>

<template>
    <div class="cad-tab">
        <div class="filtros-bar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar produto..." />
            <label class="filtro-ativos">
                <input type="checkbox" v-model="apenasAtivos" /> Mostrar só ativos
            </label>
            <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Novo produto</AppButton>
        </div>

        <div v-if="erro" class="msg-erro">{{ erro }}</div>

        <div class="tabela">
            <div class="thead" aria-hidden="true">
                <div>Produto</div>
                <div>Categoria</div>
                <div>Fabricante</div>
                <div>Fornecedor</div>
                <div>Local</div>
                <div>Estoque / mín</div>
                <div>Custo méd.</div>
                <div>Status</div>
                <div></div>
            </div>

            <div v-if="carregando" class="tabela-loading"><i class="fa-solid fa-spinner fa-spin"></i> Carregando…</div>

            <div v-else-if="itens.length === 0" class="tabela-vazio">
                <AppEmptyState
                    icone="fa-solid fa-boxes-stacked"
                    titulo="Nenhum produto encontrado"
                    descricao="Cadastre o primeiro produto vinculando categoria, fabricante e fornecedor."
                    :compacto="true"
                >
                    <template #acao>
                        <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Novo produto</AppButton>
                    </template>
                </AppEmptyState>
            </div>

            <div v-else>
                <div v-for="it in itens" :key="it.id" class="row" :class="{ inativo: !it.ativo }">
                    <div class="prod-nome">
                        <span
                            class="cat-icone"
                            :style="{ background: it.categoriaCor || 'hsl(var(--secondary) / 0.15)', color: it.categoriaCor ? '#fff' : 'hsl(var(--secondary))' }"
                        >
                            <i :class="`fa-solid ${it.categoriaIcone || 'fa-box'}`"></i>
                        </span>
                        <div>
                            <b>{{ it.nome }}</b>
                            <small>{{ it.codigo }} · {{ it.unidadeMedida }}</small>
                        </div>
                    </div>
                    <div class="muted">{{ it.categoria || "—" }}</div>
                    <div class="muted">{{ it.fabricanteNome || "—" }}</div>
                    <div class="muted">{{ it.fornecedorPadraoNome || "—" }}</div>
                    <div class="muted">{{ it.localPadraoNome || "—" }}</div>
                    <div class="estoque">
                        <b>{{ formatarQtd(it.quantidadeAtual) }}</b>
                        <small>/ {{ formatarQtd(it.quantidadeMinima) }} {{ it.unidadeMedida }}</small>
                    </div>
                    <div class="muted">{{ formatarMoedaBrl(it.custoMedio) }}</div>
                    <div>
                        <AppStatusPill :label="it.ativo ? 'Ativo' : 'Inativo'" :variante="it.ativo ? 'success' : 'muted'" />
                    </div>
                    <div class="acoes">
                        <button type="button" class="btn-icon btn-icon-editar" title="Editar" :disabled="!it.ativo" @click="abrirEditar(it)">
                            <i class="fa-solid fa-pen"></i>
                        </button>
                        <button v-if="it.ativo" type="button" class="btn-icon btn-icon-excluir" title="Inativar" @click="inativar(it)">
                            <i class="fa-solid fa-ban"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <AppPagination
            v-if="total > 0 && !carregando"
            :pagina="pagina"
            :tamanho="tamanho"
            :total="total"
            rotulo-itens="produtos"
            class="paginacao"
            @update:pagina="(p: number) => (pagina = p)"
            @update:tamanho="(t: number) => (tamanho = t)"
        />

        <AppDrawer
            :aberto="drawerAberto"
            :titulo="ehEdicao ? `Editar — ${editando!.nome}` : 'Novo produto'"
            :largura="640"
            @fechar="drawerAberto = false"
        >
            <div class="form-drawer">
                <div class="grid-2">
                    <AppField label="Código" required v-if="!ehEdicao">
                        <AppInput v-model="form.codigo" placeholder="MED-001" />
                    </AppField>
                    <AppField label="Unidade de medida" required>
                        <AppInput v-model="form.unidadeMedida" placeholder="un, kg, L..." />
                    </AppField>
                    <AppField label="Nome do produto" required class="full">
                        <AppInput v-model="form.nome" placeholder="Nome do produto" />
                    </AppField>

                    <AppField label="Categoria" required class="full">
                        <AppSelect v-model="form.categoriaId">
                            <option :value="0" disabled>Selecione</option>
                            <option
                                v-for="c in categorias.filter(c => c.ativo || c.id === form.categoriaId)"
                                :key="c.id"
                                :value="c.id"
                            >
                                {{ c.nome }}
                            </option>
                        </AppSelect>
                    </AppField>

                    <AppField label="Fabricante">
                        <AppSelect v-model="form.fabricanteId">
                            <option :value="0">— Nenhum —</option>
                            <option
                                v-for="f in fabricantes.filter(f => f.ativo || f.id === form.fabricanteId)"
                                :key="f.id"
                                :value="f.id"
                            >
                                {{ f.nome }}
                            </option>
                        </AppSelect>
                    </AppField>

                    <AppField label="Fornecedor padrão">
                        <AppSelect v-model="form.fornecedorPadraoId">
                            <option :value="0">— Nenhum —</option>
                            <option
                                v-for="f in fornecedores.filter(f => f.ativo || f.id === form.fornecedorPadraoId)"
                                :key="f.id"
                                :value="f.id"
                            >
                                {{ f.razaoSocial }}
                            </option>
                        </AppSelect>
                    </AppField>

                    <AppField label="Local padrão">
                        <AppSelect v-model="form.localPadraoId">
                            <option :value="0">— Nenhum —</option>
                            <option
                                v-for="l in locais.filter(l => l.ativo || l.id === form.localPadraoId)"
                                :key="l.id"
                                :value="l.id"
                            >
                                {{ l.nome }}
                            </option>
                        </AppSelect>
                    </AppField>

                    <AppField label="Custo unitário (R$)" hint="Referência para pedidos de compra.">
                        <AppInput v-model="form.custoUnitario" type="number" :min="0" :step="0.01" />
                    </AppField>

                    <AppField label="Qtd. mínima" required>
                        <AppInput v-model="form.quantidadeMinima" type="number" :min="0" :step="0.001" />
                    </AppField>

                    <template v-if="!ehEdicao">
                        <AppField label="Qtd. inicial">
                            <AppInput v-model="form.quantidadeInicial" type="number" :min="0" :step="0.001" />
                        </AppField>
                        <AppField
                            v-if="form.quantidadeInicial > 0"
                            label="Custo unit. inicial (R$)"
                            required
                            class="full"
                            hint="Necessário para o custo médio ponderado da primeira entrada."
                        >
                            <AppInput v-model="form.custoUnitarioInicial" type="number" :min="0.01" :step="0.01" />
                        </AppField>
                    </template>
                </div>

                <p v-if="erroForm" class="msg-erro">{{ erroForm }}</p>

                <div class="acoes-form">
                    <AppButton variant="secondary" @click="drawerAberto = false">Cancelar</AppButton>
                    <AppButton
                        :icon="ehEdicao ? 'fa-solid fa-check' : 'fa-solid fa-plus'"
                        :loading="salvando"
                        :disabled="salvando"
                        @click="salvar"
                    >
                        {{ ehEdicao ? "Salvar" : "Criar" }}
                    </AppButton>
                </div>
            </div>
        </AppDrawer>
    </div>
</template>

<style scoped>
.cad-tab { display: flex; flex-direction: column; gap: 12px; }
.filtros-bar { display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }
.filtro-ativos { display: inline-flex; align-items: center; gap: 6px; font-size: 13px; color: hsl(var(--secondary) / 0.7); }
.msg-erro { color: hsl(var(--error)); font-size: 13px; }

.tabela {
    background: var(--bg-card);
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-lg);
    overflow: hidden;
    box-shadow: var(--shadow);
}
.thead, .row {
    display: grid;
    grid-template-columns: 2fr 1fr 1fr 1.2fr 1fr 1fr 0.8fr 100px auto;
    gap: 10px; align-items: center; padding: 11px 14px;
}
.thead {
    background: hsl(var(--secondary) / 0.025);
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    font-size: 10px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.05em;
    color: hsl(var(--secondary) / 0.55);
}
.row { border-bottom: 1px solid hsl(var(--secondary) / 0.05); }
.row:last-child { border-bottom: 0; }
.row.inativo { opacity: 0.55; }
.row:hover { background: hsl(var(--primary) / 0.025); }

.prod-nome { display: flex; align-items: center; gap: 10px; min-width: 0; }
.prod-nome b { font-size: 13px; color: hsl(var(--primary-dark)); display: block; }
.prod-nome small { font-size: 11px; color: hsl(var(--secondary) / 0.55); font-family: monospace; }
.cat-icone {
    display: inline-grid; place-items: center;
    width: 30px; height: 30px;
    border-radius: var(--radius-sm);
    font-size: 13px;
    flex-shrink: 0;
}

.muted { color: hsl(var(--secondary) / 0.7); font-size: 12px; }
.estoque b { font-size: 14px; color: hsl(var(--primary-dark)); }
.estoque small { display: block; font-size: 10px; color: hsl(var(--secondary) / 0.55); }
.acoes { display: flex; gap: 4px; }

.tabela-loading, .tabela-vazio { padding: 32px; text-align: center; color: hsl(var(--secondary) / 0.6); }
.paginacao { margin-top: 4px; }

.form-drawer { display: flex; flex-direction: column; gap: 16px; padding: 20px; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.grid-2 .full { grid-column: 1 / -1; }
.acoes-form { display: flex; gap: 8px; justify-content: flex-end; margin-top: 8px; }
</style>
