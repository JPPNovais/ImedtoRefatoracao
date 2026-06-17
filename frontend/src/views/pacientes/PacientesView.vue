<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { useRouter } from "vue-router"
import PacienteFormModal from "@/components/pacientes/PacienteFormModal.vue"
import {
    AppButton, AppEmptyState, AppFilterPills, AppPageHeader, AppPagination, AppSearchInput, AppSelect, AppToast, AppConfirmDialog,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import {
    pacienteService,
    type Paciente,
    type PacienteListaItem,
    type PacienteStats,
    type PaginaPacientes,
} from "@/services/pacienteService"
import { PACIENTE_TAGS, resolverTag } from "@/constants/pacienteTags"

const router = useRouter()

// ─── Estado de carregamento e listagem ────────────────────────────────────
const buscaInput = ref("")
const busca      = useDebouncedRef(buscaInput, 300)
const pagina     = ref(1)
const tamanho    = ref(10)
const dados      = ref<PaginaPacientes | null>(null)
const stats      = ref<PacienteStats | null>(null)
const carregando = ref(false)
const erro       = ref<string | null>(null)
const excluindoId = ref<number | null>(null)

const filtroTag  = ref<"todos" | string>("todos")
const ordenacao  = ref<"recentes" | "nome">("recentes")

// Toast.
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(m: string, v: "info" | "success" | "error" = "success") {
    toast.value = { mensagem: m, variante: v }
}

// ─── Modal de cadastro/edição ──────────────────────────────────────────────
const modalAberto = ref(false)
const pacienteEmEdicao = ref<Paciente | null>(null)

// ─── Carregamento ──────────────────────────────────────────────────────────
watch(busca, () => { pagina.value = 1 })
watch([busca, pagina, tamanho], () => void carregar(), { immediate: true })

// Stats carrega só uma vez no mount; recalcula depois de criar/excluir.
async function carregarStats() {
    try {
        stats.value = await pacienteService.stats()
    } catch { /* opcional */ }
}
void carregarStats()

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        dados.value = await pacienteService.listar(busca.value, pagina.value, tamanho.value)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar pacientes."
    } finally {
        carregando.value = false
    }
}

// ─── Filtros local-side ────────────────────────────────────────────────────
// Filtro por tag e ordenação são aplicados em cima da página atual (limitação:
// "ordenação por nome A-Z global" exigiria recurso novo no backend; o ORDER BY
// padrão do backend já é por nome, então `nome` aqui apenas reordena a página
// pelo cliente — mantém a UI consistente sem inflar o backend nesta iteração).
const filtrados = computed<PacienteListaItem[]>(() => {
    let lista = dados.value?.itens ?? []
    if (filtroTag.value !== "todos") {
        lista = lista.filter(p => p.tags.includes(filtroTag.value as string))
    }
    if (ordenacao.value === "nome") {
        lista = [...lista].sort((a, b) => a.nomeCompleto.localeCompare(b.nomeCompleto, "pt-BR"))
    }
    return lista
})

// ─── Tag pill counts (na página atual) ────────────────────────────────────
const tagOpcoes = computed(() => {
    const contar = (chave: string) => (dados.value?.itens ?? []).filter(p => p.tags.includes(chave)).length
    return [
        { valor: "todos" as const, label: "Todos", count: dados.value?.itens.length ?? 0 },
        ...PACIENTE_TAGS.map(t => ({
            valor: t.chave,
            label: t.label,
            count: contar(t.chave),
        })).filter(o => o.count > 0),
    ]
})

// Confirmação de exclusão de paciente (LGPD — irreversível).
const confirmacaoExcluir = ref<{ aberto: boolean, alvo: PacienteListaItem | null, executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

// ─── Ações ─────────────────────────────────────────────────────────────────
function novo() {
    pacienteEmEdicao.value = null
    modalAberto.value = true
}

async function editar(p: PacienteListaItem) {
    try {
        pacienteEmEdicao.value = await pacienteService.obter(p.id)
        modalAberto.value = true
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao carregar paciente.", "error")
    }
}

function verDetalhe(p: PacienteListaItem) {
    router.push({ name: "PacienteDetalhe", params: { id: p.id } })
}

function excluir(p: PacienteListaItem) {
    confirmacaoExcluir.value = { aberto: true, alvo: p, executando: false }
}

async function executarExcluir() {
    const alvo = confirmacaoExcluir.value.alvo
    if (!alvo) return
    confirmacaoExcluir.value.executando = true
    excluindoId.value = alvo.id
    try {
        await pacienteService.deletar(alvo.id)
        confirmacaoExcluir.value = { aberto: false, alvo: null, executando: false }
        await Promise.all([carregar(), carregarStats()])
        notificar("Paciente excluído.")
    } catch (e: any) {
        confirmacaoExcluir.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Erro ao excluir.", "error")
    } finally {
        excluindoId.value = null
    }
}

function onPacienteSalvo() {
    modalAberto.value = false
    pacienteEmEdicao.value = null
    void carregar()
    void carregarStats()
    notificar("Paciente salvo com sucesso.")
}

// ─── Helpers visuais ───────────────────────────────────────────────────────
function iniciais(p: PacienteListaItem): string {
    const partes = (p.nomeCompleto || "?").split(" ").filter(Boolean)
    if (partes.length === 1) return partes[0][0]?.toUpperCase() ?? "?"
    return (partes[0][0] + (partes[partes.length - 1][0] ?? "")).toUpperCase()
}

function corAvatar(p: PacienteListaItem): string {
    const paleta = [
        "hsl(254 56% 38%)", "hsl(190 60% 45%)", "hsl(280 55% 50%)",
        "hsl(140 45% 45%)", "hsl(40 70% 50%)", "hsl(340 55% 55%)",
        "hsl(220 55% 50%)", "hsl(170 50% 40%)",
    ]
    return paleta[p.id % paleta.length]
}

function idade(dataNasc: string | null): string {
    if (!dataNasc) return ""
    const nasc = new Date(dataNasc)
    if (isNaN(nasc.getTime())) return ""
    const hoje = new Date()
    let anos = hoje.getFullYear() - nasc.getFullYear()
    const m = hoje.getMonth() - nasc.getMonth()
    if (m < 0 || (m === 0 && hoje.getDate() < nasc.getDate())) anos--
    return anos > 0 ? `${anos} anos` : ""
}

function formatarCpf(cpf: string | null) {
    if (!cpf) return ""
    return cpf.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, "$1.$2.$3-$4")
}
</script>

<template>
    <main class="app-page app-page--wide pacientes">
        <AppPageHeader
            titulo="Pacientes"
            :subtitulo="(stats?.total ?? 0) + ' pacientes cadastrados · ' + (stats?.novosMesCorrente ?? 0) + ' novos este mês'"
        >
            <template #acoes>
                <AppButton icon="fa-solid fa-user-plus" @click="novo">Cadastrar paciente</AppButton>
            </template>
        </AppPageHeader>

        <!-- KPIs -->
        <div class="kpi-row">
            <div class="kpi">
                <div class="kpi-icon kpi-icon--primary">
                    <i class="fa-solid fa-users"></i>
                </div>
                <div class="kpi-info">
                    <span>Total ativos</span>
                    <b>{{ stats?.total ?? "—" }}</b>
                </div>
            </div>
            <div class="kpi">
                <div class="kpi-icon kpi-icon--success">
                    <i class="fa-solid fa-seedling"></i>
                </div>
                <div class="kpi-info">
                    <span>Novos este mês</span>
                    <b>{{ stats?.novosMesCorrente ?? "—" }}</b>
                </div>
            </div>
            <div class="kpi" :title="'Em breve — depende da integração financeira por paciente'">
                <div class="kpi-icon kpi-icon--warning">
                    <i class="fa-solid fa-circle-exclamation"></i>
                </div>
                <div class="kpi-info">
                    <span>Em débito</span>
                    <b class="muted">—</b>
                </div>
            </div>
            <div class="kpi" :title="'Em breve — depende da integração financeira por paciente'">
                <div class="kpi-icon kpi-icon--error">
                    <i class="fa-solid fa-coins"></i>
                </div>
                <div class="kpi-info">
                    <span>Total em aberto</span>
                    <b class="muted">—</b>
                </div>
            </div>
        </div>

        <!-- Filtros -->
        <div class="filters-bar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar por nome, CPF, telefone ou e-mail..." />
            <AppFilterPills v-model="filtroTag" :opcoes="tagOpcoes" />
            <AppSelect v-model="ordenacao" class="ord-filter">
                <option value="recentes">Recém-cadastrados</option>
                <option value="nome">Nome (A-Z)</option>
            </AppSelect>
        </div>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <!-- Lista -->
        <AppEmptyState
            v-if="!carregando && filtrados.length === 0 && busca.trim()"
            icone="🔎"
            titulo="Nenhum paciente encontrado"
            descricao="Tente outra busca ou ajuste os filtros."
        />
        <AppEmptyState
            v-else-if="!carregando && filtrados.length === 0 && filtroTag !== 'todos'"
            icone="🏷️"
            titulo="Nenhum paciente com esta tag nesta página"
            descricao="Limpe o filtro de tag ou navegue para outra página."
        />
        <AppEmptyState
            v-else-if="!carregando && (dados?.total ?? 0) === 0"
            icone="👥"
            titulo="Nenhum paciente cadastrado"
            descricao="Cadastre o primeiro paciente para começar."
        >
            <template #acao>
                <AppButton icon="fa-solid fa-user-plus" @click="novo">Cadastrar paciente</AppButton>
            </template>
        </AppEmptyState>

        <p v-if="carregando && !dados" class="msg-info">Carregando…</p>

        <div v-if="dados && filtrados.length > 0" class="patients-table">
            <div class="pt-thead">
                <div>Paciente</div>
                <div>Tags clínicas</div>
                <div>Telefone</div>
                <div>CPF / Documento</div>
                <div class="col-acoes">Ações</div>
            </div>
            <div
                v-for="p in filtrados" :key="p.id"
                class="pt-row"
                @click="verDetalhe(p)"
            >
                <div class="pt-name">
                    <div class="pt-avatar" :style="{ background: corAvatar(p) }">{{ iniciais(p) }}</div>
                    <div>
                        <b>{{ p.nomeCompleto }}</b>
                        <span class="pt-meta">
                            <template v-if="idade(p.dataNascimento)">{{ idade(p.dataNascimento) }}</template>
                            <template v-if="idade(p.dataNascimento) && p.cpf">·</template>
                            <template v-if="p.cpf">{{ formatarCpf(p.cpf) }}</template>
                            <template v-else-if="p.documentoInternacional">Doc: {{ p.documentoInternacional }}</template>
                        </span>
                    </div>
                </div>

                <div class="pt-tags">
                    <span
                        v-for="chave in p.tags.slice(0, 3)" :key="chave"
                        class="tag-pill"
                        :style="{ background: `color-mix(in srgb, ${resolverTag(chave).cor} 15%, white)`, color: resolverTag(chave).cor }"
                    >
                        <i class="fa-solid" :class="resolverTag(chave).icone"></i>
                        {{ resolverTag(chave).label }}
                    </span>
                    <span v-if="p.tags.length > 3" class="tag-extra">+{{ p.tags.length - 3 }}</span>
                    <span v-if="p.qtdAlertas > 0" class="alert-count" :title="p.qtdAlertas + ' alerta(s) clínico(s)'">
                        <i class="fa-solid fa-triangle-exclamation"></i>
                        {{ p.qtdAlertas }} alerta{{ p.qtdAlertas > 1 ? 's' : '' }}
                    </span>
                    <span v-if="!p.tags.length && !p.qtdAlertas" class="muted">—</span>
                </div>

                <div class="pt-cell">
                    {{ p.telefone || "—" }}
                </div>

                <div class="pt-cell pt-cell--small">
                    {{ p.cpf ? formatarCpf(p.cpf) : (p.documentoInternacional || "—") }}
                </div>

                <div class="pt-actions" @click.stop>
                    <button class="btn-icon btn-icon-ver" title="Ver detalhes" @click="verDetalhe(p)">
                        <i class="fa-solid fa-eye"></i>
                    </button>
                    <button class="btn-icon btn-icon-editar" title="Editar" @click="editar(p)">
                        <i class="fa-solid fa-pen"></i>
                    </button>
                    <button
                        class="btn-icon btn-icon-excluir" title="Excluir"
                        :disabled="excluindoId === p.id"
                        @click="excluir(p)"
                    >
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </div>
            </div>
        </div>

        <AppPagination
            v-if="dados"
            v-model:pagina="pagina"
            v-model:tamanho="tamanho"
            :total="dados.total"
            rotulo-itens="paciente(s)"
        />

        <!-- Modal cadastrar/editar -->
        <PacienteFormModal
            :aberto="modalAberto"
            :paciente="pacienteEmEdicao"
            @fechar="modalAberto = false; pacienteEmEdicao = null"
            @salvo="onPacienteSalvo"
        />

        <AppConfirmDialog
            v-model:aberto="confirmacaoExcluir.aberto"
            titulo="Excluir paciente?"
            :mensagem="confirmacaoExcluir.alvo ? `Excluir ${confirmacaoExcluir.alvo.nomeCompleto}? Esta ação é irreversível.` : ''"
            confirmar-rotulo="Excluir"
            variante="danger"
            :executando="confirmacaoExcluir.executando"
            @confirmar="executarExcluir"
        />

        <AppToast
            v-if="toast"
            :mensagem="toast.mensagem"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </main>
</template>

<style scoped>
.pacientes {
    display: flex;
    flex-direction: column;
    gap: 18px;
}

/* ─── KPIs ─── */
.kpi-row {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 12px;
}
.kpi {
    display: flex; align-items: center; gap: 14px;
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 12px; padding: 14px 18px;
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
}
.kpi-icon {
    width: 42px; height: 42px; border-radius: 8px;
    display: flex; align-items: center; justify-content: center;
    font-size: 16px; flex-shrink: 0;
}
.kpi-icon--primary { background: hsl(var(--primary) / 0.1);  color: hsl(var(--primary)); }
.kpi-icon--success { background: hsl(var(--success) / 0.12); color: hsl(160 79% 32%); }
.kpi-icon--warning { background: hsl(var(--warning) / 0.15); color: hsl(40 90% 35%); }
.kpi-icon--error   { background: hsl(var(--error) / 0.1);    color: hsl(var(--error)); }

.kpi-info span {
    display: block; font-size: 11px; color: hsl(var(--secondary) / 0.6);
    font-weight: 600; text-transform: uppercase; letter-spacing: 0.04em;
}
.kpi-info b {
    display: block; font-size: 22px; color: hsl(var(--primary-dark));
    font-weight: 800; line-height: 1.1; margin-top: 2px;
}
.kpi-info b.muted { color: hsl(var(--secondary) / 0.35); font-size: 18px; }

/* ─── Filtros ─── */
.filters-bar {
    display: flex; align-items: center; gap: 12px; flex-wrap: wrap;
}
.ord-filter { min-width: 180px; max-width: 220px; }

/* ─── Tabela ─── */
.patients-table {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 12px;
    overflow: hidden;
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
}
.pt-thead, .pt-row {
    display: grid;
    grid-template-columns: 2fr 1.6fr 1fr 1.2fr 130px;
    gap: 14px; align-items: center;
    padding: 12px 18px;
}
.pt-thead {
    background: hsl(var(--secondary) / 0.03);
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em;
    color: hsl(var(--secondary) / 0.55);
}
.pt-row {
    border-bottom: 1px solid hsl(var(--secondary) / 0.05);
    cursor: pointer; transition: background 150ms;
}
.pt-row:last-child { border-bottom: none; }
.pt-row:hover { background: hsl(var(--primary) / 0.025); }

.pt-name { display: flex; align-items: center; gap: 12px; min-width: 0; }
.pt-avatar {
    width: 38px; height: 38px; border-radius: 50%;
    color: white; font-weight: 700; font-size: 13px;
    display: flex; align-items: center; justify-content: center;
    flex-shrink: 0;
}
.pt-name b {
    display: block; color: hsl(var(--primary-dark));
    font-size: 14px; font-weight: 700; line-height: 1.2;
}
.pt-meta {
    display: block; font-size: 11px; color: hsl(var(--secondary) / 0.6);
    margin-top: 2px;
}
.pt-meta > template + template::before { content: " "; }

.pt-tags { display: flex; gap: 4px; flex-wrap: wrap; align-items: center; }
.tag-pill {
    display: inline-flex; align-items: center; gap: 4px;
    font-size: 10px; font-weight: 700;
    padding: 3px 7px; border-radius: 999px; white-space: nowrap;
}
.tag-pill i { font-size: 9px; }
.tag-extra {
    font-size: 10px; font-weight: 700;
    padding: 3px 7px; border-radius: 999px;
    background: hsl(var(--secondary) / 0.08);
    color: hsl(var(--secondary) / 0.6);
}

.alert-count {
    display: inline-flex; align-items: center; gap: 4px;
    font-size: 10px; font-weight: 700;
    background: hsl(var(--error) / 0.08); color: hsl(var(--error));
    padding: 3px 7px; border-radius: 999px;
}
.alert-count i { font-size: 9px; }

.pt-cell { font-size: 12.5px; color: hsl(var(--secondary)); }
.pt-cell--small { font-size: 12px; color: hsl(var(--secondary) / 0.7); }

.pt-actions { white-space: nowrap; text-align: right; }

.muted { color: hsl(var(--secondary) / 0.45); font-size: 12px; }
.msg-info { color: hsl(var(--secondary) / 0.7); margin: 0; }
.msg-erro {
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-radius: 8px; padding: 10px 14px;
    font-size: 13px; margin: 0;
}

@media (max-width: 1100px) {
    .kpi-row { grid-template-columns: repeat(2, 1fr); }
    .pt-thead, .pt-row { grid-template-columns: 1.8fr 1fr 1fr 130px; }
    .pt-thead > div:nth-child(4), .pt-row .pt-cell--small { display: none; }
}
@media (max-width: 720px) {
    .kpi-row { grid-template-columns: 1fr 1fr; }
    .pt-thead, .pt-row { grid-template-columns: 1.6fr 1fr 100px; }
    .pt-thead > div:nth-child(2), .pt-row .pt-tags,
    .pt-thead > div:nth-child(3), .pt-row .pt-cell { display: none; }
}
</style>
