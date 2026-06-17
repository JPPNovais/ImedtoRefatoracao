<script setup lang="ts">
/**
 * Aba "Pacotes" — config-orcamento. Pacote = template que agrupa procedimentos,
 * produtos, papéis de equipe e um anestesista.
 */
import { ref, computed, onMounted, watch } from "vue"
import {
    AppStatCard, AppSearchInput, AppFilterPills, AppDrawer, AppField, AppInput,
    AppSelect, AppButton, AppStatusPill, AppPagination, AppEmptyState,
    AppToast, AppConfirmDialog,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { formatarMoedaBrl } from "@/utils/format"
import {
    orcamentoCatalogoService,
    type OrcamentoPacoteResumo, type PacotePayload,
    type CatalogoCirurgia, type CatalogoProduto,
    type OrcamentoTeamRole, type OrcamentoAnestesistaLista,
} from "@/services/orcamentoCatalogoService"

const emit = defineEmits<{ (e: "contagem", v: number): void }>()

const carregando = ref(false)
const lista = ref<OrcamentoPacoteResumo[]>([])
const procedimentos = ref<CatalogoCirurgia[]>([])
const produtos = ref<CatalogoProduto[]>([])
const teamRoles = ref<OrcamentoTeamRole[]>([])
const anestesistas = ref<OrcamentoAnestesistaLista[]>([])

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
const filtroStatus = ref<"todos" | "ativos" | "inativos">("ativos")
const pagina = ref(1)
const tamanho = ref(10)

const drawerAberto = ref(false)
const idEditando = ref<number | null>(null)
const form = ref<PacotePayload>({
    nome: "", descricao: null, anestesistaId: null, valorTotalSugerido: null,
    procedimentoIds: [], produtos: [], teamRoleIds: [],
})

// Auxiliares para o picker no drawer
const novoProcId = ref<number | null>(null)
const novoProdutoId = ref<number | null>(null)
const novoProdutoQtd = ref(1)
const novoRoleId = ref<number | null>(null)

// Toast e confirmação (substituem window.alert/confirm).
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}
const confirmacao = ref<{ aberto: boolean, alvo: OrcamentoPacoteResumo | null, executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

const filtrada = computed(() => {
    let l = lista.value
    if (filtroStatus.value === "ativos") l = l.filter(x => x.ativo)
    else if (filtroStatus.value === "inativos") l = l.filter(x => !x.ativo)
    if (busca.value.trim()) {
        const q = busca.value.trim().toLowerCase()
        l = l.filter(x =>
            x.nome.toLowerCase().includes(q)
            || (x.descricao ?? "").toLowerCase().includes(q),
        )
    }
    return l
})

const total = computed(() => filtrada.value.length)
const inicio = computed(() => (pagina.value - 1) * tamanho.value)
const pagina_itens = computed(() => filtrada.value.slice(inicio.value, inicio.value + tamanho.value))
const totalAtivos = computed(() => lista.value.filter(x => x.ativo).length)
const totalInativos = computed(() => lista.value.length - totalAtivos.value)
const ticketMedio = computed(() => {
    const ativos = lista.value.filter(x => x.ativo && x.valorTotalSugerido)
    if (!ativos.length) return 0
    return ativos.reduce((s, x) => s + (x.valorTotalSugerido ?? 0), 0) / ativos.length
})

// Opções para os pickers no drawer
const opcoesProcedimento = computed(() => [
    { value: "", label: "Adicionar procedimento…" },
    ...procedimentos.value.filter(p => p.ativo && !form.value.procedimentoIds.includes(p.id))
        .map(p => ({ value: String(p.id), label: p.descricao })),
])
const opcoesProduto = computed(() => [
    { value: "", label: "Adicionar produto…" },
    ...produtos.value.filter(p => p.ativo && !form.value.produtos.some(x => x.produtoId === p.id))
        .map(p => ({ value: String(p.id), label: p.nome })),
])
const opcoesRole = computed(() => [
    { value: "", label: "Adicionar papel…" },
    ...teamRoles.value.filter(t => t.ativo && !form.value.teamRoleIds.includes(t.id))
        .map(t => ({ value: String(t.id), label: t.papel })),
])
const opcoesAnestesista = computed(() => [
    { value: "", label: "Sem anestesista" },
    ...anestesistas.value.map(a => ({ value: String(a.id), label: a.nome + (a.ativo ? "" : " (inativo)") })),
])

// Lookup nomes
function nomeProcedimento(id: number) {
    return procedimentos.value.find(p => p.id === id)?.descricao ?? `#${id}`
}
function nomeProduto(id: number) {
    return produtos.value.find(p => p.id === id)?.nome ?? `#${id}`
}
function nomeRole(id: number) {
    return teamRoles.value.find(t => t.id === id)?.papel ?? `#${id}`
}

async function carregar() {
    carregando.value = true
    try {
        const [pcts, procs, prods, rls, ans] = await Promise.all([
            orcamentoCatalogoService.listarPacotes(),
            orcamentoCatalogoService.listarProcedimentos(),
            orcamentoCatalogoService.listarProdutos(),
            orcamentoCatalogoService.listarTeamRoles(),
            orcamentoCatalogoService.listarAnestesistas(),
        ])
        lista.value = pcts
        procedimentos.value = procs
        produtos.value = prods
        teamRoles.value = rls
        anestesistas.value = ans
        emit("contagem", lista.value.length)
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)
watch(busca, () => { pagina.value = 1 })
watch(filtroStatus, () => { pagina.value = 1 })

function novo() {
    idEditando.value = null
    form.value = {
        nome: "", descricao: null, anestesistaId: null, valorTotalSugerido: null,
        procedimentoIds: [], produtos: [], teamRoleIds: [],
    }
    drawerAberto.value = true
}

async function editar(item: OrcamentoPacoteResumo) {
    idEditando.value = item.id
    const det = await orcamentoCatalogoService.obterPacote(item.id)
    form.value = {
        nome: det.nome,
        descricao: det.descricao,
        anestesistaId: det.anestesistaId,
        valorTotalSugerido: det.valorTotalSugerido,
        procedimentoIds: det.procedimentos.map(p => p.catalogoCirurgiaId),
        produtos: det.produtos.map(p => ({ produtoId: p.catalogoProdutoId, quantidade: p.quantidade })),
        teamRoleIds: det.teamRoles.map(t => t.teamRoleId),
    }
    drawerAberto.value = true
}

function adicionarProcedimento() {
    if (!novoProcId.value) return
    form.value.procedimentoIds.push(novoProcId.value)
    novoProcId.value = null
}
function removerProcedimento(id: number) {
    form.value.procedimentoIds = form.value.procedimentoIds.filter(p => p !== id)
}

function adicionarProduto() {
    if (!novoProdutoId.value || novoProdutoQtd.value <= 0) return
    form.value.produtos.push({ produtoId: novoProdutoId.value, quantidade: novoProdutoQtd.value })
    novoProdutoId.value = null
    novoProdutoQtd.value = 1
}
function removerProduto(id: number) {
    form.value.produtos = form.value.produtos.filter(p => p.produtoId !== id)
}

function adicionarRole() {
    if (!novoRoleId.value) return
    form.value.teamRoleIds.push(novoRoleId.value)
    novoRoleId.value = null
}
function removerRole(id: number) {
    form.value.teamRoleIds = form.value.teamRoleIds.filter(r => r !== id)
}

async function salvar() {
    if (!form.value.nome.trim()) { notificar("Nome é obrigatório.", "error"); return }
    try {
        if (idEditando.value === null) {
            await orcamentoCatalogoService.criarPacote(form.value)
            notificar("Pacote criado.", "success")
        } else {
            await orcamentoCatalogoService.atualizarPacote(idEditando.value, form.value)
            notificar("Pacote atualizado.", "success")
        }
        drawerAberto.value = false
        await carregar()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Falha ao salvar pacote.", "error")
    }
}

function pedirRemocao(item: OrcamentoPacoteResumo) {
    confirmacao.value = { aberto: true, alvo: item, executando: false }
}

async function executarRemocao() {
    const alvo = confirmacao.value.alvo
    if (!alvo) return
    confirmacao.value.executando = true
    try {
        await orcamentoCatalogoService.removerPacote(alvo.id)
        confirmacao.value = { aberto: false, alvo: null, executando: false }
        notificar("Pacote inativado.", "success")
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
            <AppStatCard label="Cadastrados" :valor="lista.length" cor="primary" icone="fa-solid fa-box-open" />
            <AppStatCard label="Ativos" :valor="totalAtivos" cor="success" icone="fa-solid fa-circle-check" />
            <AppStatCard label="Inativos" :valor="totalInativos" cor="muted" icone="fa-solid fa-circle-minus" />
            <AppStatCard label="Ticket sugerido médio" :valor="formatarMoedaBrl(ticketMedio)" cor="info" icone="fa-solid fa-coins" />
        </div>

        <div class="toolbar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar pacote por nome..." />
            <AppFilterPills
                v-model="filtroStatus"
                :opcoes="[
                    { valor: 'ativos', label: 'Ativos', count: totalAtivos, dot: 'success' },
                    { valor: 'inativos', label: 'Inativos', count: totalInativos, dot: 'muted' },
                    { valor: 'todos', label: 'Todos', count: lista.length },
                ]"
            />
            <AppButton icon="fa-solid fa-plus" @click="novo">Novo pacote</AppButton>
        </div>

        <div v-if="carregando" class="loading">Carregando…</div>
        <AppEmptyState
            v-else-if="!lista.length"
            icone="fa-solid fa-box-open"
            titulo="Nenhum pacote cadastrado"
            descricao="Pacotes são templates que agrupam procedimentos, produtos, equipe e anestesista — ideal para automatizar orçamentos recorrentes."
        >
            <template #acao>
                <AppButton icon="fa-solid fa-plus" @click="novo">Criar primeiro pacote</AppButton>
            </template>
        </AppEmptyState>
        <AppEmptyState v-else-if="!pagina_itens.length" icone="fa-solid fa-magnifying-glass" titulo="Nenhum resultado" />
        <div v-else class="cards-grid">
            <div v-for="item in pagina_itens" :key="item.id" class="pacote-card">
                <div class="pacote-head">
                    <div class="pacote-title">{{ item.nome }}</div>
                    <AppStatusPill :label="item.ativo ? 'Ativo' : 'Inativo'" :variante="item.ativo ? 'success' : 'muted'" />
                </div>
                <div v-if="item.descricao" class="pacote-desc">{{ item.descricao }}</div>
                <div class="pacote-meta">
                    <span><i class="fa-solid fa-scalpel"></i> {{ item.totalProcedimentos }} procs</span>
                    <span><i class="fa-solid fa-boxes-stacked"></i> {{ item.totalProdutos }} produtos</span>
                    <span><i class="fa-solid fa-users"></i> {{ item.totalTeamRoles }} papéis</span>
                    <span v-if="item.anestesistaNome"><i class="fa-solid fa-user-doctor"></i> {{ item.anestesistaNome }}</span>
                </div>
                <div class="pacote-footer">
                    <span v-if="item.valorTotalSugerido" class="pacote-valor">{{ formatarMoedaBrl(item.valorTotalSugerido) }}</span>
                    <div class="pacote-actions">
                        <button class="btn-icon btn-icon-editar" title="Editar" @click="editar(item)">
                            <i class="fa-solid fa-pen"></i>
                        </button>
                        <button v-if="item.ativo" class="btn-icon btn-icon-excluir" title="Inativar" @click="pedirRemocao(item)">
                            <i class="fa-solid fa-trash"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <AppPagination v-if="lista.length && pagina_itens.length" v-model:pagina="pagina" v-model:tamanho="tamanho" :total="total" />

        <AppDrawer
            :aberto="drawerAberto"
            :titulo="idEditando === null ? 'Novo pacote' : 'Editar pacote'"
            :largura="700"
            @fechar="drawerAberto = false"
        >
            <AppField label="Nome" required>
                <AppInput v-model="form.nome" placeholder="Ex: Pacote completo — Colecistectomia" />
            </AppField>
            <AppField label="Descrição">
                <AppInput v-model="form.descricao" placeholder="Resumo do pacote" />
            </AppField>
            <div class="grid-2">
                <AppField label="Anestesista">
                    <AppSelect
                        :model-value="form.anestesistaId === null ? '' : String(form.anestesistaId)"
                        :options="opcoesAnestesista"
                        @update:model-value="(v: any) => form.anestesistaId = v === '' ? null : Number(v)"
                    />
                </AppField>
                <AppField label="Valor sugerido (R$)">
                    <AppInput
                        type="number"
                        step="0.01"
                        :model-value="form.valorTotalSugerido ?? ''"
                        @update:model-value="(v: any) => form.valorTotalSugerido = v === '' ? null : Number(v)"
                    />
                </AppField>
            </div>

            <!-- Procedimentos -->
            <div class="sec">
                <h4>Procedimentos <span class="count">{{ form.procedimentoIds.length }}</span></h4>
                <div v-for="pid in form.procedimentoIds" :key="pid" class="chip-row">
                    <span>{{ nomeProcedimento(pid) }}</span>
                    <button type="button" class="btn-icon btn-icon-excluir" @click="removerProcedimento(pid)">
                        <i class="fa-solid fa-times"></i>
                    </button>
                </div>
                <div class="add-row">
                    <AppSelect :model-value="novoProcId === null ? '' : String(novoProcId)" :options="opcoesProcedimento" @update:model-value="(v: any) => novoProcId = v ? Number(v) : null" />
                    <AppButton variant="secondary" size="sm" icon="fa-solid fa-plus" @click="adicionarProcedimento" :disabled="!novoProcId">Adicionar</AppButton>
                </div>
            </div>

            <!-- Produtos -->
            <div class="sec">
                <h4>Produtos <span class="count">{{ form.produtos.length }}</span></h4>
                <div v-for="(p, idx) in form.produtos" :key="p.produtoId" class="chip-row">
                    <span>{{ nomeProduto(p.produtoId) }} × {{ p.quantidade }}</span>
                    <button type="button" class="btn-icon btn-icon-excluir" @click="removerProduto(p.produtoId)">
                        <i class="fa-solid fa-times"></i>
                    </button>
                </div>
                <div class="add-row-3">
                    <AppSelect :model-value="novoProdutoId === null ? '' : String(novoProdutoId)" :options="opcoesProduto" @update:model-value="(v: any) => novoProdutoId = v ? Number(v) : null" />
                    <AppInput type="number" :model-value="novoProdutoQtd" @update:model-value="(v: any) => novoProdutoQtd = Math.max(1, Number(v) || 1)" />
                    <AppButton variant="secondary" size="sm" icon="fa-solid fa-plus" @click="adicionarProduto" :disabled="!novoProdutoId">Adicionar</AppButton>
                </div>
            </div>

            <!-- Equipe -->
            <div class="sec">
                <h4>Papéis de equipe <span class="count">{{ form.teamRoleIds.length }}</span></h4>
                <div v-for="rid in form.teamRoleIds" :key="rid" class="chip-row">
                    <span>{{ nomeRole(rid) }}</span>
                    <button type="button" class="btn-icon btn-icon-excluir" @click="removerRole(rid)">
                        <i class="fa-solid fa-times"></i>
                    </button>
                </div>
                <div class="add-row">
                    <AppSelect :model-value="novoRoleId === null ? '' : String(novoRoleId)" :options="opcoesRole" @update:model-value="(v: any) => novoRoleId = v ? Number(v) : null" />
                    <AppButton variant="secondary" size="sm" icon="fa-solid fa-plus" @click="adicionarRole" :disabled="!novoRoleId">Adicionar</AppButton>
                </div>
            </div>

            <template #rodape>
                <AppButton variant="secondary" @click="drawerAberto = false">Cancelar</AppButton>
                <AppButton @click="salvar">Salvar</AppButton>
            </template>
        </AppDrawer>

        <AppConfirmDialog
            v-model:aberto="confirmacao.aberto"
            titulo="Inativar pacote?"
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
.cards-grid {
    display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 12px;
}
.pacote-card {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
    padding: 16px;
    display: flex; flex-direction: column; gap: 12px;
}
.pacote-head { display: flex; justify-content: space-between; gap: 12px; }
.pacote-title { font-weight: 600; font-size: 14px; }
.pacote-desc { font-size: 13px; color: hsl(var(--secondary) / 0.7); }
.pacote-meta {
    display: flex; flex-wrap: wrap; gap: 8px;
    font-size: 12px; color: hsl(var(--secondary) / 0.7);
}
.pacote-meta i { margin-right: 4px; color: hsl(var(--primary) / 0.5); }
.pacote-footer {
    display: flex; justify-content: space-between; align-items: center;
    padding-top: 8px; border-top: 1px solid hsl(var(--secondary) / 0.08);
}
.pacote-valor { font-weight: 700; font-size: 16px; color: hsl(var(--primary)); }
.pacote-actions { display: flex; gap: 4px; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.sec { display: flex; flex-direction: column; gap: 8px; padding-top: 12px; border-top: 1px solid hsl(var(--secondary) / 0.1); }
.sec h4 { margin: 0; font-size: 13px; font-weight: 600; }
.count { background: hsl(var(--primary) / 0.1); color: hsl(var(--primary)); padding: 2px 8px; border-radius: 999px; font-size: 11px; }
.chip-row {
    display: flex; justify-content: space-between; align-items: center;
    padding: 6px 10px;
    background: hsl(var(--secondary) / 0.04); border-radius: 6px;
    font-size: 13px;
}
.add-row { display: grid; grid-template-columns: 1fr auto; gap: 8px; align-items: center; }
.add-row-3 { display: grid; grid-template-columns: 1fr 80px auto; gap: 8px; align-items: center; }
</style>
