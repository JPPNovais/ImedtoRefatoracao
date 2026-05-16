<script setup lang="ts">
/**
 * Aba "Anestesistas" — config-orcamento. Drawer com editor de faixas (replace-all).
 */
import { ref, computed, onMounted, watch } from "vue"
import {
    AppStatCard, AppSearchInput, AppFilterPills, AppDrawer, AppField, AppInput,
    AppButton, AppStatusPill, AppPagination, AppEmptyState,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { formatarMoedaBrl } from "@/utils/format"
import {
    orcamentoCatalogoService,
    type OrcamentoAnestesista, type AnestesistaPayload,
} from "@/services/orcamentoCatalogoService"

const emit = defineEmits<{ (e: "contagem", v: number): void }>()

const carregando = ref(false)
const lista = ref<OrcamentoAnestesista[]>([])
const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
type FiltroStatus = "todos" | "ativos" | "inativos"
const filtroStatus = ref<FiltroStatus>("ativos")
const pagina = ref(1)
const tamanho = ref(20)

const drawerAberto = ref(false)
const idEditando = ref<number | null>(null)
const form = ref<AnestesistaPayload>({
    nome: "", profissionalUsuarioId: null,
    crm: null, especialidade: null, telefone: null, tabelaHonorarios: null,
    faixas: [],
})

const filtrada = computed(() => {
    let l = lista.value
    if (filtroStatus.value === "ativos") l = l.filter(x => x.ativo)
    else if (filtroStatus.value === "inativos") l = l.filter(x => !x.ativo)
    if (busca.value.trim()) {
        const q = busca.value.trim().toLowerCase()
        l = l.filter(x =>
            x.nome.toLowerCase().includes(q)
            || (x.crm ?? "").toLowerCase().includes(q)
            || (x.especialidade ?? "").toLowerCase().includes(q),
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
    let soma = 0, count = 0
    for (const a of lista.value.filter(x => x.ativo)) {
        for (const f of a.faixas) { soma += f.valor; count++ }
    }
    return count ? soma / count : 0
})

async function carregar() {
    carregando.value = true
    try {
        lista.value = await orcamentoCatalogoService.listarAnestesistas()
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
        nome: "", profissionalUsuarioId: null,
        crm: null, especialidade: null, telefone: null, tabelaHonorarios: null,
        faixas: [{ descricao: "Pequeno porte (até 1h)", valor: 0 }],
    }
    drawerAberto.value = true
}

async function editar(item: OrcamentoAnestesista) {
    idEditando.value = item.id
    // Carrega detalhe (faixas) — listagem já traz, mas garantimos sincronia.
    const detalhe = await orcamentoCatalogoService.obterAnestesista(item.id)
    form.value = {
        nome: detalhe.nome,
        profissionalUsuarioId: detalhe.profissionalUsuarioId,
        crm: detalhe.crm,
        especialidade: detalhe.especialidade,
        telefone: detalhe.telefone,
        tabelaHonorarios: detalhe.tabelaHonorarios,
        faixas: detalhe.faixas.map(f => ({ descricao: f.descricao, valor: f.valor })),
    }
    drawerAberto.value = true
}

function adicionarFaixa() {
    form.value.faixas.push({ descricao: "", valor: 0 })
}
function removerFaixa(idx: number) {
    form.value.faixas.splice(idx, 1)
}

async function salvar() {
    if (!form.value.nome.trim()) { alert("Nome é obrigatório."); return }
    // Validação de duplicados (espelho da regra do back).
    const descricoes = form.value.faixas.map(f => f.descricao.trim().toLowerCase())
    const dup = descricoes.filter((d, i) => d && descricoes.indexOf(d) !== i)
    if (dup.length) { alert(`Faixa duplicada: ${dup[0]}`); return }

    try {
        if (idEditando.value === null) {
            await orcamentoCatalogoService.criarAnestesista(form.value)
        } else {
            await orcamentoCatalogoService.atualizarAnestesista(idEditando.value, form.value)
        }
        drawerAberto.value = false
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Falha ao salvar.")
    }
}

async function remover(item: OrcamentoAnestesista) {
    if (!confirm(`Inativar "${item.nome}"?`)) return
    try {
        await orcamentoCatalogoService.removerAnestesista(item.id)
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Falha ao inativar.")
    }
}

function iniciais(nome: string): string {
    return nome.split(/\s+/).slice(0, 2).map(p => p[0]?.toUpperCase() ?? "").join("")
}
</script>

<template>
    <div class="config-tab">
        <div class="stats-grid">
            <AppStatCard label="Cadastrados" :valor="lista.length" cor="primary" icone="fa-solid fa-user-doctor" />
            <AppStatCard label="Ativos" :valor="totalAtivos" cor="success" icone="fa-solid fa-circle-check" />
            <AppStatCard label="Inativos" :valor="totalInativos" cor="muted" icone="fa-solid fa-circle-minus" />
            <AppStatCard label="Valor médio/faixa" :valor="formatarMoedaBrl(ticketMedio)" cor="info" icone="fa-solid fa-coins" />
        </div>

        <div class="toolbar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar por nome, CRM, especialidade..." />
            <AppFilterPills
                v-model="filtroStatus"
                :opcoes="[
                    { valor: 'ativos', label: 'Ativos', count: totalAtivos, dot: 'success' },
                    { valor: 'inativos', label: 'Inativos', count: totalInativos, dot: 'muted' },
                    { valor: 'todos', label: 'Todos', count: lista.length },
                ]"
            />
            <AppButton icon="fa-solid fa-plus" @click="novo">Novo anestesista</AppButton>
        </div>

        <div v-if="carregando" class="loading">Carregando…</div>
        <AppEmptyState
            v-else-if="!lista.length"
            icone="fa-solid fa-user-doctor"
            titulo="Nenhum anestesista cadastrado"
            descricao="Cadastre anestesistas com suas tabelas de honorário por faixa de complexidade."
        >
            <template #acao>
                <AppButton icon="fa-solid fa-plus" @click="novo">Criar primeiro anestesista</AppButton>
            </template>
        </AppEmptyState>
        <AppEmptyState v-else-if="!pagina_itens.length" icone="fa-solid fa-magnifying-glass" titulo="Nenhum resultado" />
        <div v-else class="cards-grid">
            <div v-for="item in pagina_itens" :key="item.id" class="anest-card">
                <div class="anest-head">
                    <span class="avatar">{{ iniciais(item.nome) }}</span>
                    <div class="anest-name">
                        <div class="anest-title">{{ item.nome }}</div>
                        <div class="anest-sub">{{ item.especialidade ?? "—" }}</div>
                    </div>
                    <AppStatusPill :label="item.ativo ? 'Ativo' : 'Inativo'" :variante="item.ativo ? 'success' : 'muted'" />
                </div>
                <div class="anest-meta">
                    <span v-if="item.crm"><i class="fa-solid fa-id-card"></i> {{ item.crm }}</span>
                    <span v-if="item.tabelaHonorarios"><i class="fa-solid fa-table"></i> {{ item.tabelaHonorarios }}</span>
                </div>
                <div class="faixas-summary">
                    <strong>{{ item.faixas.length }}</strong> faixas
                </div>
                <div class="anest-actions">
                    <button class="btn-icon btn-icon-editar" title="Editar" @click="editar(item)">
                        <i class="fa-solid fa-pen"></i>
                    </button>
                    <button v-if="item.ativo" class="btn-icon btn-icon-excluir" title="Inativar" @click="remover(item)">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </div>
            </div>
        </div>

        <AppPagination v-if="lista.length && pagina_itens.length" v-model:pagina="pagina" v-model:tamanho="tamanho" :total="total" />

        <AppDrawer
            :aberto="drawerAberto"
            :titulo="idEditando === null ? 'Novo anestesista' : 'Editar anestesista'"
            :largura="640"
            @fechar="drawerAberto = false"
        >
            <AppField label="Nome" required>
                <AppInput v-model="form.nome" placeholder="Ex: Dr. Roberto Mendes" />
            </AppField>
            <div class="grid-2">
                <AppField label="CRM">
                    <AppInput v-model="form.crm" placeholder="Ex: CRM/SP 89432" />
                </AppField>
                <AppField label="Especialidade">
                    <AppInput v-model="form.especialidade" placeholder="Ex: Anestesiologia geral" />
                </AppField>
            </div>
            <div class="grid-2">
                <AppField label="Telefone">
                    <AppInput v-model="form.telefone" placeholder="(11) 99999-9999" />
                </AppField>
                <AppField label="Tabela de honorários">
                    <AppInput v-model="form.tabelaHonorarios" placeholder="Ex: Padrão, Pediátrica..." />
                </AppField>
            </div>

            <div class="faixas-editor">
                <div class="faixas-head">
                    <h4>Faixas de honorário</h4>
                    <AppButton variant="ghost" size="sm" icon="fa-solid fa-plus" @click="adicionarFaixa">Adicionar faixa</AppButton>
                </div>
                <div v-for="(faixa, idx) in form.faixas" :key="idx" class="faixa-row">
                    <AppInput v-model="faixa.descricao" placeholder="Ex: Pequeno porte (até 1h)" />
                    <AppInput
                        type="number"
                        step="0.01"
                        :model-value="faixa.valor"
                        @update:model-value="(v: any) => faixa.valor = Number(v) || 0"
                        style="max-width: 140px;"
                    />
                    <button type="button" class="btn-icon btn-icon-excluir" @click="removerFaixa(idx)">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </div>
                <p v-if="!form.faixas.length" class="hint">
                    Sem faixas — adicione pelo menos uma para que o anestesista apareça em pacotes.
                </p>
            </div>

            <template #rodape>
                <AppButton variant="secondary" @click="drawerAberto = false">Cancelar</AppButton>
                <AppButton @click="salvar">Salvar</AppButton>
            </template>
        </AppDrawer>
    </div>
</template>

<style scoped>
.config-tab { display: flex; flex-direction: column; gap: 16px; }
.stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; }
.toolbar { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
.toolbar > :first-child { flex: 1 1 280px; min-width: 220px; }
.loading { padding: 32px; text-align: center; color: hsl(var(--secondary) / 0.7); }
.cards-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    gap: 12px;
}
.anest-card {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
    padding: 16px;
    display: flex; flex-direction: column; gap: 12px;
    position: relative;
}
.anest-head { display: flex; align-items: center; gap: 12px; }
.avatar {
    width: 40px; height: 40px; border-radius: 50%;
    background: hsl(var(--primary) / 0.12); color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 14px; font-weight: 700;
    flex-shrink: 0;
}
.anest-name { flex: 1; min-width: 0; }
.anest-title { font-weight: 600; font-size: 14px; }
.anest-sub { font-size: 12px; color: hsl(var(--secondary) / 0.6); }
.anest-meta {
    display: flex; flex-wrap: wrap; gap: 8px;
    font-size: 12px; color: hsl(var(--secondary) / 0.7);
}
.anest-meta i { margin-right: 4px; color: hsl(var(--primary) / 0.5); }
.faixas-summary { font-size: 13px; color: hsl(var(--secondary) / 0.7); }
.anest-actions { display: flex; gap: 4px; justify-content: flex-end; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.faixas-editor { display: flex; flex-direction: column; gap: 8px; padding-top: 12px; border-top: 1px solid hsl(var(--secondary) / 0.1); }
.faixas-head { display: flex; justify-content: space-between; align-items: center; }
.faixas-head h4 { margin: 0; font-size: 14px; font-weight: 600; }
.faixa-row { display: grid; grid-template-columns: 1fr auto auto; gap: 8px; align-items: center; }
.hint { font-size: 12px; color: hsl(var(--secondary) / 0.6); padding: 8px; background: hsl(var(--secondary) / 0.04); border-radius: 6px; }
</style>
