<script setup lang="ts">
/**
 * Aba "Equipe" — papéis padrão de equipe cirúrgica (TeamRoles). Modelo simplificado
 * de honorário: percentual sobre base ou valor fixo.
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
    type OrcamentoTeamRole, type TeamRolePayload, type TipoHonorario,
} from "@/services/orcamentoCatalogoService"

const emit = defineEmits<{ (e: "contagem", v: number): void }>()

const carregando = ref(false)
const lista = ref<OrcamentoTeamRole[]>([])
const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
type FiltroTipo = "todos" | TipoHonorario
const filtroTipo = ref<FiltroTipo>("todos")
const pagina = ref(1)
const tamanho = ref(10)

const drawerAberto = ref(false)
const idEditando = ref<number | null>(null)
const form = ref<TeamRolePayload>({
    papel: "", profissionalUsuarioId: null, nomePadrao: null,
    tipoHonorario: "Percentual", valor: 0, baseCalculo: "procedimento",
})

// Toast e confirmação (substituem window.alert/confirm).
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}
const confirmacao = ref<{ aberto: boolean, alvo: OrcamentoTeamRole | null, executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

const opcoesTipo = [
    { value: "Percentual", label: "Percentual (%)" },
    { value: "Fixo", label: "Valor fixo (R$)" },
]
const opcoesBase = [
    { value: "procedimento", label: "Procedimento" },
    { value: "por cirurgia", label: "Por cirurgia" },
    { value: "honorario cirurgiao", label: "Honorário do cirurgião" },
]

const filtrada = computed(() => {
    let l = lista.value
    if (filtroTipo.value !== "todos") l = l.filter(x => x.tipoHonorario === filtroTipo.value)
    if (busca.value.trim()) {
        const q = busca.value.trim().toLowerCase()
        l = l.filter(x =>
            x.papel.toLowerCase().includes(q)
            || (x.nomePadrao ?? "").toLowerCase().includes(q)
            || (x.profissionalNome ?? "").toLowerCase().includes(q),
        )
    }
    return l
})

const total = computed(() => filtrada.value.length)
const inicio = computed(() => (pagina.value - 1) * tamanho.value)
const pagina_itens = computed(() => filtrada.value.slice(inicio.value, inicio.value + tamanho.value))
const totalAtivos = computed(() => lista.value.filter(x => x.ativo).length)
const totalPct = computed(() => lista.value.filter(x => x.tipoHonorario === "Percentual").length)
const totalFixo = computed(() => lista.value.filter(x => x.tipoHonorario === "Fixo").length)

async function carregar() {
    carregando.value = true
    try {
        lista.value = await orcamentoCatalogoService.listarTeamRoles()
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
        papel: "", profissionalUsuarioId: null, nomePadrao: null,
        tipoHonorario: "Percentual", valor: 0, baseCalculo: "procedimento",
    }
    drawerAberto.value = true
}

function editar(item: OrcamentoTeamRole) {
    idEditando.value = item.id
    form.value = {
        papel: item.papel,
        profissionalUsuarioId: item.profissionalUsuarioId,
        nomePadrao: item.nomePadrao,
        tipoHonorario: item.tipoHonorario,
        valor: item.valor,
        baseCalculo: item.baseCalculo,
    }
    drawerAberto.value = true
}

async function salvar() {
    if (!form.value.papel.trim()) { notificar("Papel é obrigatório.", "error"); return }
    try {
        if (idEditando.value === null) {
            await orcamentoCatalogoService.criarTeamRole(form.value)
            notificar("Papel criado.", "success")
        } else {
            await orcamentoCatalogoService.atualizarTeamRole(idEditando.value, form.value)
            notificar("Papel atualizado.", "success")
        }
        drawerAberto.value = false
        await carregar()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Falha ao salvar.", "error")
    }
}

function pedirRemocao(item: OrcamentoTeamRole) {
    confirmacao.value = { aberto: true, alvo: item, executando: false }
}

async function executarRemocao() {
    const alvo = confirmacao.value.alvo
    if (!alvo) return
    confirmacao.value.executando = true
    try {
        await orcamentoCatalogoService.removerTeamRole(alvo.id)
        confirmacao.value = { aberto: false, alvo: null, executando: false }
        notificar("Papel inativado.", "success")
        await carregar()
    } catch (e: any) {
        confirmacao.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Falha ao inativar.", "error")
    }
}

function iniciais(nome: string): string {
    return nome.split(/\s+/).slice(0, 2).map(p => p[0]?.toUpperCase() ?? "").join("")
}
</script>

<template>
    <div class="config-tab">
        <div class="stats-grid">
            <AppStatCard label="Cadastrados" :valor="lista.length" cor="primary" icone="fa-solid fa-users" />
            <AppStatCard label="Ativos" :valor="totalAtivos" cor="success" icone="fa-solid fa-circle-check" />
            <AppStatCard label="Percentual" :valor="totalPct" cor="info" icone="fa-solid fa-percent" />
            <AppStatCard label="Valor fixo" :valor="totalFixo" cor="warning" icone="fa-solid fa-coins" />
        </div>

        <div class="toolbar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar por papel ou nome..." />
            <AppFilterPills
                v-model="filtroTipo"
                :opcoes="[
                    { valor: 'todos', label: 'Todos', count: lista.length },
                    { valor: 'Percentual', label: 'Percentual', count: totalPct, dot: 'info' },
                    { valor: 'Fixo', label: 'Fixo', count: totalFixo, dot: 'warning' },
                ]"
            />
            <AppButton icon="fa-solid fa-plus" @click="novo">Novo papel</AppButton>
        </div>

        <div v-if="carregando" class="loading">Carregando…</div>
        <AppEmptyState
            v-else-if="!lista.length"
            icone="fa-solid fa-users"
            titulo="Nenhum papel de equipe cadastrado"
            descricao="Defina os papéis padrão de equipe (cirurgião, anestesista, instrumentadora...) para automatizar orçamentos."
        >
            <template #acao>
                <AppButton icon="fa-solid fa-plus" @click="novo">Criar primeiro papel</AppButton>
            </template>
        </AppEmptyState>
        <AppEmptyState v-else-if="!pagina_itens.length" icone="fa-solid fa-magnifying-glass" titulo="Nenhum resultado" />
        <div v-else class="table-wrap">
            <table class="table">
                <thead>
                    <tr>
                        <th>Papel</th>
                        <th>Profissional padrão</th>
                        <th>Honorário</th>
                        <th>Base</th>
                        <th>Status</th>
                        <th class="acoes-col"></th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="item in pagina_itens" :key="item.id">
                        <td>
                            <div class="cell-desc">{{ item.papel }}</div>
                        </td>
                        <td>
                            <div v-if="item.nomePadrao || item.profissionalNome" class="prof">
                                <span class="avatar">{{ iniciais(item.nomePadrao ?? item.profissionalNome ?? "") }}</span>
                                <span>{{ item.nomePadrao ?? item.profissionalNome }}</span>
                            </div>
                            <span v-else class="cell-sub">—</span>
                        </td>
                        <td>
                            <span class="honor">
                                {{ item.tipoHonorario === "Percentual" ? `${item.valor}%` : formatarMoedaBrl(item.valor) }}
                            </span>
                        </td>
                        <td class="cell-sub">{{ item.baseCalculo }}</td>
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
            :titulo="idEditando === null ? 'Novo papel de equipe' : 'Editar papel'"
            :largura="560"
            @fechar="drawerAberto = false"
        >
            <AppField label="Papel" required>
                <AppInput v-model="form.papel" placeholder="Ex: Cirurgião principal" />
            </AppField>
            <AppField label="Nome padrão do profissional (opcional)">
                <AppInput v-model="form.nomePadrao" placeholder="Ex: Dr. Carlos Silva" />
            </AppField>
            <div class="grid-2">
                <AppField label="Tipo de honorário">
                    <AppSelect v-model="form.tipoHonorario" :options="opcoesTipo" />
                </AppField>
                <AppField :label="form.tipoHonorario === 'Percentual' ? 'Valor (%)' : 'Valor (R$)'">
                    <AppInput
                        type="number"
                        step="0.01"
                        :model-value="form.valor"
                        @update:model-value="(v: any) => form.valor = Number(v) || 0"
                    />
                </AppField>
            </div>
            <AppField label="Base de cálculo">
                <AppSelect v-model="form.baseCalculo" :options="opcoesBase" />
            </AppField>

            <template #rodape>
                <AppButton variant="secondary" @click="drawerAberto = false">Cancelar</AppButton>
                <AppButton @click="salvar">Salvar</AppButton>
            </template>
        </AppDrawer>

        <AppConfirmDialog
            v-model:aberto="confirmacao.aberto"
            titulo="Inativar papel?"
            :mensagem="confirmacao.alvo ? `Deseja inativar “${confirmacao.alvo.papel}”?` : ''"
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
.cell-desc { font-weight: 600; }
.cell-sub { font-size: 13px; color: hsl(var(--secondary) / 0.6); }
.prof { display: inline-flex; align-items: center; gap: 8px; }
.avatar {
    width: 28px; height: 28px; border-radius: 50%;
    background: hsl(var(--primary) / 0.12); color: hsl(var(--primary));
    display: inline-flex; align-items: center; justify-content: center;
    font-size: 11px; font-weight: 600;
}
.honor { font-weight: 600; }
.acoes-col { width: 90px; text-align: right; white-space: nowrap; }
.acoes-col .btn-icon + .btn-icon { margin-left: 4px; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
</style>
