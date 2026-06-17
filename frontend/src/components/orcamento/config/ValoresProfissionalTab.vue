<script setup lang="ts">
/**
 * Aba "Valores profissional" — tabela de honorário por tempo do profissional.
 * Espelha o padrão de EquipeTab.vue. Backend já existe; esta aba fecha a lacuna de UI.
 * Sem gestão de status (sem filtro Todos/Ativos, sem inativar/reativar).
 */
import { ref, computed, onMounted, watch } from "vue"
import {
    AppStatCard, AppSearchInput, AppDrawer, AppField, AppInput,
    AppSelect, AppButton, AppPagination, AppEmptyState,
    AppToast, AppConfirmDialog,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import { formatarMoedaBrl } from "@/utils/format"
import { usePermissoesStore } from "@/stores/permissoesStore"
import { orcamentoCatalogoService, type ValorProfissionalOrcamentoCatalogo, type CriarValorProfissionalPayload, type AtualizarValorProfissionalPayload } from "@/services/orcamentoCatalogoService"
import { vinculoService, type ProfissionalPublico } from "@/services/vinculoService"

const emit = defineEmits<{ (e: "contagem", v: number): void }>()

const permissoes = usePermissoesStore()
const podeConfigurar = computed(() => permissoes.pode("orcamento.configurar"))

const carregando = ref(false)
const lista = ref<ValorProfissionalOrcamentoCatalogo[]>([])
const profissionais = ref<ProfissionalPublico[]>([])
const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
const pagina = ref(1)
const tamanho = ref(10)

const drawerAberto = ref(false)
const idEditando = ref<number | null>(null)

// Formulário de criação — inclui profissionalUsuarioId (string vazia = padrão/null).
const formCriar = ref<CriarValorProfissionalPayload & { profissionalUsuarioIdStr: string }>({
    profissionalUsuarioId: null,
    profissionalUsuarioIdStr: "",
    funcao: "",
    tempoBaseMinutos: 0,
    valorTempoBase: 0,
    tempoAdicionalMinutos: 0,
    valorAdicional: 0,
    valorPlus: 0,
})

// Formulário de edição — sem profissionalUsuarioId (contrato AtualizarValorProfissionalPayload).
const formEditar = ref<AtualizarValorProfissionalPayload>({
    funcao: "",
    tempoBaseMinutos: 0,
    valorTempoBase: 0,
    tempoAdicionalMinutos: 0,
    valorAdicional: 0,
    valorPlus: 0,
})

const toast = ref<{ mensagem: string; variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}
const confirmacao = ref<{ aberto: boolean; alvo: ValorProfissionalOrcamentoCatalogo | null; executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

const opcoesProfissional = computed(() => [
    { value: "", label: "Padrão (sem profissional específico)" },
    ...profissionais.value.map(p => ({ value: p.usuarioId, label: p.nomeCompleto })),
])

const filtrada = computed(() => {
    let l = lista.value
    if (busca.value.trim()) {
        const q = busca.value.trim().toLowerCase()
        l = l.filter(x =>
            x.funcao.toLowerCase().includes(q)
            || (x.profissionalNome ?? "").toLowerCase().includes(q),
        )
    }
    return l
})

const total = computed(() => filtrada.value.length)
const inicio = computed(() => (pagina.value - 1) * tamanho.value)
const paginaItens = computed(() => filtrada.value.slice(inicio.value, inicio.value + tamanho.value))
const totalPadrao = computed(() => lista.value.filter(x => x.profissionalUsuarioId === null).length)
const totalPorProfissional = computed(() => lista.value.filter(x => x.profissionalUsuarioId !== null).length)

async function carregar() {
    carregando.value = true
    try {
        lista.value = await orcamentoCatalogoService.listarValoresProfissional()
        emit("contagem", lista.value.length)
    } finally {
        carregando.value = false
    }
}

async function carregarProfissionais() {
    profissionais.value = await vinculoService.listarProfissionaisPublico()
}

onMounted(() => {
    carregar()
    carregarProfissionais()
})
watch(busca, () => { pagina.value = 1 })

function novo() {
    idEditando.value = null
    formCriar.value = {
        profissionalUsuarioId: null,
        profissionalUsuarioIdStr: "",
        funcao: "",
        tempoBaseMinutos: 0,
        valorTempoBase: 0,
        tempoAdicionalMinutos: 0,
        valorAdicional: 0,
        valorPlus: 0,
    }
    drawerAberto.value = true
}

function editar(item: ValorProfissionalOrcamentoCatalogo) {
    idEditando.value = item.id
    formEditar.value = {
        funcao: item.funcao,
        tempoBaseMinutos: item.tempoBaseMinutos,
        valorTempoBase: item.valorTempoBase,
        tempoAdicionalMinutos: item.tempoAdicionalMinutos,
        valorAdicional: item.valorAdicional,
        valorPlus: item.valorPlus,
    }
    drawerAberto.value = true
}

function validarCampos(f: { funcao: string; tempoBaseMinutos: number; valorTempoBase: number; tempoAdicionalMinutos: number; valorAdicional: number; valorPlus: number }): boolean {
    if (!f.funcao.trim()) { notificar("Função é obrigatória.", "error"); return false }
    if (f.tempoBaseMinutos < 0 || f.tempoAdicionalMinutos < 0) { notificar("Tempos não podem ser negativos.", "error"); return false }
    if (f.valorTempoBase < 0 || f.valorAdicional < 0 || f.valorPlus < 0) { notificar("Valores não podem ser negativos.", "error"); return false }
    return true
}

async function salvar() {
    if (idEditando.value === null) {
        if (!validarCampos(formCriar.value)) return
        const payload: CriarValorProfissionalPayload = {
            profissionalUsuarioId: formCriar.value.profissionalUsuarioIdStr || null,
            funcao: formCriar.value.funcao,
            tempoBaseMinutos: formCriar.value.tempoBaseMinutos,
            valorTempoBase: formCriar.value.valorTempoBase,
            tempoAdicionalMinutos: formCriar.value.tempoAdicionalMinutos,
            valorAdicional: formCriar.value.valorAdicional,
            valorPlus: formCriar.value.valorPlus,
        }
        try {
            await orcamentoCatalogoService.criarValorProfissional(payload)
            notificar("Valor profissional criado.", "success")
            drawerAberto.value = false
            await carregar()
        } catch (e: any) {
            notificar(e?.response?.data?.mensagem ?? "Falha ao salvar.", "error")
        }
    } else {
        if (!validarCampos(formEditar.value)) return
        try {
            await orcamentoCatalogoService.atualizarValorProfissional(idEditando.value, formEditar.value)
            notificar("Valor profissional atualizado.", "success")
            drawerAberto.value = false
            await carregar()
        } catch (e: any) {
            notificar(e?.response?.data?.mensagem ?? "Falha ao salvar.", "error")
        }
    }
}

function pedirRemocao(item: ValorProfissionalOrcamentoCatalogo) {
    confirmacao.value = { aberto: true, alvo: item, executando: false }
}

async function executarRemocao() {
    const alvo = confirmacao.value.alvo
    if (!alvo) return
    confirmacao.value.executando = true
    try {
        await orcamentoCatalogoService.removerValorProfissional(alvo.id)
        confirmacao.value = { aberto: false, alvo: null, executando: false }
        notificar("Valor profissional excluído.", "success")
        await carregar()
    } catch (e: any) {
        confirmacao.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Falha ao excluir.", "error")
    }
}
</script>

<template>
    <div class="config-tab">
        <div class="stats-grid">
            <AppStatCard label="Cadastrados" :valor="lista.length" cor="primary" icone="fa-solid fa-user-clock" />
            <AppStatCard label="Padrão" :valor="totalPadrao" cor="info" icone="fa-solid fa-table" />
            <AppStatCard label="Por profissional" :valor="totalPorProfissional" cor="warning" icone="fa-solid fa-user-tie" />
        </div>

        <div class="toolbar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar por função ou profissional..." />
            <AppButton v-if="podeConfigurar" icon="fa-solid fa-plus" @click="novo">Novo valor</AppButton>
        </div>

        <div v-if="carregando" class="loading">Carregando…</div>
        <AppEmptyState
            v-else-if="!lista.length"
            icone="fa-solid fa-user-clock"
            titulo="Nenhum valor profissional cadastrado"
            descricao="Cadastre valores de honorário por tempo para que o select 'Tabela de valor' seja populado no formulário de orçamento."
        >
            <template #acao>
                <AppButton v-if="podeConfigurar" icon="fa-solid fa-plus" @click="novo">Criar primeiro valor</AppButton>
            </template>
        </AppEmptyState>
        <AppEmptyState v-else-if="!paginaItens.length" icone="fa-solid fa-magnifying-glass" titulo="Nenhum resultado" />
        <div v-else class="table-wrap">
            <table class="table">
                <thead>
                    <tr>
                        <th>Função</th>
                        <th>Profissional</th>
                        <th>Tempo base</th>
                        <th>Valor base</th>
                        <th>Tempo adic.</th>
                        <th>Valor adic.</th>
                        <th>Plus</th>
                        <th class="acoes-col"></th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="item in paginaItens" :key="item.id">
                        <td><div class="cell-desc">{{ item.funcao }}</div></td>
                        <td>
                            <span v-if="item.profissionalNome" class="cell-prof">{{ item.profissionalNome }}</span>
                            <span v-else class="cell-sub">—</span>
                        </td>
                        <td class="cell-sub">{{ item.tempoBaseMinutos }} min</td>
                        <td>{{ formatarMoedaBrl(item.valorTempoBase) }}</td>
                        <td class="cell-sub">{{ item.tempoAdicionalMinutos }} min</td>
                        <td>{{ formatarMoedaBrl(item.valorAdicional) }}</td>
                        <td>{{ formatarMoedaBrl(item.valorPlus) }}</td>
                        <td class="acoes-col">
                            <button v-if="podeConfigurar" class="btn-icon btn-icon-editar" title="Editar" @click="editar(item)">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button v-if="podeConfigurar" class="btn-icon btn-icon-excluir" title="Excluir" @click="pedirRemocao(item)">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </td>
                    </tr>
                </tbody>
            </table>
            <AppPagination v-model:pagina="pagina" v-model:tamanho="tamanho" :total="total" />
        </div>

        <!-- Drawer de criar -->
        <AppDrawer
            v-if="idEditando === null"
            :aberto="drawerAberto"
            titulo="Novo valor profissional"
            :largura="560"
            @fechar="drawerAberto = false"
        >
            <AppField label="Profissional">
                <AppSelect v-model="formCriar.profissionalUsuarioIdStr" :options="opcoesProfissional" />
            </AppField>
            <AppField label="Função" required>
                <AppInput v-model="formCriar.funcao" placeholder="Ex: Cirurgião principal" />
            </AppField>
            <div class="grid-2">
                <AppField label="Tempo base (min)">
                    <AppInput
                        type="number"
                        :model-value="formCriar.tempoBaseMinutos"
                        @update:model-value="(v: any) => formCriar.tempoBaseMinutos = Number(v) || 0"
                    />
                </AppField>
                <AppField label="Valor base (R$)">
                    <AppInput
                        type="number"
                        step="0.01"
                        :model-value="formCriar.valorTempoBase"
                        @update:model-value="(v: any) => formCriar.valorTempoBase = Number(v) || 0"
                    />
                </AppField>
            </div>
            <div class="grid-2">
                <AppField label="Tempo adicional (min)">
                    <AppInput
                        type="number"
                        :model-value="formCriar.tempoAdicionalMinutos"
                        @update:model-value="(v: any) => formCriar.tempoAdicionalMinutos = Number(v) || 0"
                    />
                </AppField>
                <AppField label="Valor adicional (R$)">
                    <AppInput
                        type="number"
                        step="0.01"
                        :model-value="formCriar.valorAdicional"
                        @update:model-value="(v: any) => formCriar.valorAdicional = Number(v) || 0"
                    />
                </AppField>
            </div>
            <AppField label="Valor plus (R$)">
                <AppInput
                    type="number"
                    step="0.01"
                    :model-value="formCriar.valorPlus"
                    @update:model-value="(v: any) => formCriar.valorPlus = Number(v) || 0"
                />
            </AppField>

            <template #rodape>
                <AppButton variant="secondary" @click="drawerAberto = false">Cancelar</AppButton>
                <AppButton @click="salvar">Salvar</AppButton>
            </template>
        </AppDrawer>

        <!-- Drawer de editar (sem campo de profissional — AtualizarValorProfissionalPayload) -->
        <AppDrawer
            v-else
            :aberto="drawerAberto"
            titulo="Editar valor profissional"
            :largura="560"
            @fechar="drawerAberto = false"
        >
            <AppField label="Função" required>
                <AppInput v-model="formEditar.funcao" placeholder="Ex: Cirurgião principal" />
            </AppField>
            <div class="grid-2">
                <AppField label="Tempo base (min)">
                    <AppInput
                        type="number"
                        :model-value="formEditar.tempoBaseMinutos"
                        @update:model-value="(v: any) => formEditar.tempoBaseMinutos = Number(v) || 0"
                    />
                </AppField>
                <AppField label="Valor base (R$)">
                    <AppInput
                        type="number"
                        step="0.01"
                        :model-value="formEditar.valorTempoBase"
                        @update:model-value="(v: any) => formEditar.valorTempoBase = Number(v) || 0"
                    />
                </AppField>
            </div>
            <div class="grid-2">
                <AppField label="Tempo adicional (min)">
                    <AppInput
                        type="number"
                        :model-value="formEditar.tempoAdicionalMinutos"
                        @update:model-value="(v: any) => formEditar.tempoAdicionalMinutos = Number(v) || 0"
                    />
                </AppField>
                <AppField label="Valor adicional (R$)">
                    <AppInput
                        type="number"
                        step="0.01"
                        :model-value="formEditar.valorAdicional"
                        @update:model-value="(v: any) => formEditar.valorAdicional = Number(v) || 0"
                    />
                </AppField>
            </div>
            <AppField label="Valor plus (R$)">
                <AppInput
                    type="number"
                    step="0.01"
                    :model-value="formEditar.valorPlus"
                    @update:model-value="(v: any) => formEditar.valorPlus = Number(v) || 0"
                />
            </AppField>

            <template #rodape>
                <AppButton variant="secondary" @click="drawerAberto = false">Cancelar</AppButton>
                <AppButton @click="salvar">Salvar</AppButton>
            </template>
        </AppDrawer>

        <AppConfirmDialog
            v-model:aberto="confirmacao.aberto"
            titulo="Excluir valor profissional?"
            :mensagem="confirmacao.alvo ? `Deseja excluir o valor para '${confirmacao.alvo.funcao}'?` : ''"
            confirmar-rotulo="Excluir"
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
.cell-desc { font-weight: 600; }
.cell-sub { font-size: 13px; color: hsl(var(--secondary) / 0.6); }
.cell-prof { font-size: 14px; }
.acoes-col { width: 90px; text-align: right; white-space: nowrap; }
.acoes-col .btn-icon + .btn-icon { margin-left: 4px; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
</style>
