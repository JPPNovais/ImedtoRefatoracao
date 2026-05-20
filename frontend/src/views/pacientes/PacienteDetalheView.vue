<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import {
    AppButton, AppEmptyState, AppToast,
} from "@/components/ui"
import PacienteFormModal from "@/components/pacientes/PacienteFormModal.vue"
import { pacienteService, type Paciente } from "@/services/pacienteService"
import { prontuarioService, type ProntuarioCompleto, type Anexo, type Evolucao } from "@/services/prontuarioService"
import { useProntuarioPdf, type PdfSaidaModo } from "@/composables/useProntuarioPdf"
import EvolucaoTimelineItem from "@/components/prontuario/EvolucaoTimelineItem.vue"
import { orcamentoService, type OrcamentoResumo } from "@/services/orcamentoService"
import { agendaService, type Agendamento } from "@/services/agendaService"
import { resolverTag } from "@/constants/pacienteTags"

/**
 * Tela de detalhe de paciente. Header sticky com avatar grande, alertas
 * clínicos em destaque vermelho e quick stats. 8 abas:
 *  - Resumo (próximas ações + plano de tratamento ainda não disponível)
 *  - Prontuário (timeline de evoluções)
 *  - Anamnese (vista resumida da primeira evolução, se houver)
 *  - Orçamentos (lista)
 *  - Anexos (grid)
 *  - Financeiro / Convênios / Termos: empty state "em breve"
 *
 * Carregamento sob demanda por aba (lazy-load) para evitar pesar no boot.
 */
const route  = useRoute()
const router = useRouter()

const pacienteId = computed(() => Number(route.params.id))

const paciente = ref<Paciente | null>(null)
const carregando = ref(false)
const erro = ref<string | null>(null)

type Aba = "resumo" | "prontuario" | "anamnese" | "orcamentos" | "financeiro" | "convenios" | "termos" | "anexos"
const aba = ref<Aba>("resumo")

// Toast.
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(m: string, v: "info" | "success" | "error" = "success") {
    toast.value = { mensagem: m, variante: v }
}

// ─── Lazy-load por aba ─────────────────────────────────────────────────────
const abasCarregadas = new Set<Aba>()
const totalProntuarios = ref(0)
const prontuario = ref<ProntuarioCompleto | null>(null)
const carregandoProntuario = ref(false)

const orcamentos = ref<OrcamentoResumo[]>([])
const carregandoOrc = ref(false)

const anexos = ref<Anexo[]>([])
const carregandoAnexos = ref(false)

const { gerarPdfEvolucao } = useProntuarioPdf()
const evolucaoSendoBaixada = ref<number | null>(null)

/**
 * Para "visualizar" precisamos abrir `window.open` SINCRONICAMENTE ao clique
 * (antes de qualquer await) para evitar popup blocker. Depois do PDF gerado,
 * apontamos a janela para o blob URL. Popup bloqueado → fallback download.
 *
 * NÃO usar "noopener,noreferrer": no Chrome 88+ a janela retorna handle não
 * nulo mas ignora silenciosamente `janela.location.href = blobUrl` posterior,
 * deixando a aba travada em about:blank. O blob é same-origin (sem risco de
 * tabnabbing) e browsers modernos já aplicam noopener implícito em cross-origin.
 */
function abrirJanelaParaVisualizacao(): Window | null {
    return window.open("about:blank", "_blank")
}

async function exportarPdfEvolucao(payload: { evolucao: Evolucao, modo: PdfSaidaModo }) {
    if (!prontuario.value || !paciente.value) return
    if (evolucaoSendoBaixada.value !== null) return
    const { evolucao, modo } = payload
    evolucaoSendoBaixada.value = evolucao.id
    let janela: Window | null = null
    let modoEfetivo: PdfSaidaModo = modo
    if (modo === "visualizar") {
        janela = abrirJanelaParaVisualizacao()
        if (!janela) {
            notificar("Permita pop-ups para visualizar o PDF. Baixando como alternativa.", "info")
            modoEfetivo = "download"
        }
    }
    try {
        // Audit LGPD: registra antes de gerar; 422 impede a geração.
        await prontuarioService.registrarExportacaoEvolucao(pacienteId.value, evolucao.id)
        const { blobUrl } = await gerarPdfEvolucao(prontuario.value, evolucao, paciente.value, modoEfetivo)
        if (modoEfetivo === "visualizar" && janela && blobUrl) {
            janela.location.href = blobUrl
        }
    } catch (e: any) {
        if (janela) janela.close()
        notificar(e?.response?.data?.mensagem ?? "Erro ao exportar evolução.", "error")
    } finally {
        evolucaoSendoBaixada.value = null
    }
}

// Identifica a evolução mais recente (para destaque "Mais recente" no card).
const idEvolucaoMaisRecente = computed(() => {
    const lista = prontuario.value?.evolucoes ?? []
    if (!lista.length) return null
    return [...lista].sort((a, b) =>
        new Date(b.criadaEm).getTime() - new Date(a.criadaEm).getTime(),
    )[0]!.id
})

const proximaConsulta = ref<Agendamento | null>(null)
const totalConsultas = ref(0)

async function carregarPaciente() {
    carregando.value = true
    erro.value = null
    try {
        paciente.value = await pacienteService.obter(pacienteId.value)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Paciente não encontrado."
    } finally {
        carregando.value = false
    }
}

// Resumo: carrega próximas consultas e contagem de evoluções uma vez.
async function carregarResumo() {
    if (abasCarregadas.has("resumo")) return
    try {
        const [pg, total] = await Promise.all([
            agendaService.listar({
                pacienteId: pacienteId.value,
                dataInicio: new Date().toISOString().slice(0, 10),
                pagina: 1, tamanho: 10,
            }),
            prontuarioService.contarEvolucoes(pacienteId.value).catch(() => 0),
        ])
        const agora = Date.now()
        proximaConsulta.value = pg.itens.find(a =>
            new Date(a.inicioPrevisto).getTime() >= agora && a.status !== "Cancelado",
        ) ?? null
        totalConsultas.value = pg.total
        totalProntuarios.value = total
    } catch { /* opcional */ }
    abasCarregadas.add("resumo")
}

async function carregarProntuario() {
    if (abasCarregadas.has("prontuario") && abasCarregadas.has("anamnese")) return
    carregandoProntuario.value = true
    try {
        prontuario.value = await prontuarioService.obter(pacienteId.value)
        if (prontuario.value) {
            totalProntuarios.value = prontuario.value.evolucoes.length
        }
    } catch { /* sem prontuário */ }
    finally { carregandoProntuario.value = false }
    abasCarregadas.add("prontuario")
    abasCarregadas.add("anamnese")
}

async function carregarOrcamentos() {
    if (abasCarregadas.has("orcamentos")) return
    carregandoOrc.value = true
    try {
        orcamentos.value = await orcamentoService.listar({ pacienteId: pacienteId.value })
    } catch { /* ok */ }
    finally { carregandoOrc.value = false }
    abasCarregadas.add("orcamentos")
}

async function carregarAnexos() {
    if (abasCarregadas.has("anexos")) return
    carregandoAnexos.value = true
    try {
        anexos.value = await prontuarioService.listarAnexos(pacienteId.value)
    } catch { /* sem prontuário */ }
    finally { carregandoAnexos.value = false }
    abasCarregadas.add("anexos")
}

watch(aba, (a) => {
    if (a === "resumo")               void carregarResumo()
    else if (a === "prontuario" || a === "anamnese") void carregarProntuario()
    else if (a === "orcamentos")      void carregarOrcamentos()
    else if (a === "anexos")          void carregarAnexos()
}, { immediate: true })

onMounted(carregarPaciente)

// ─── Helpers visuais ───────────────────────────────────────────────────────
function iniciais(p: Paciente | null): string {
    if (!p) return "?"
    const partes = (p.nomeCompleto || "?").split(" ").filter(Boolean)
    if (partes.length === 1) return partes[0][0]?.toUpperCase() ?? "?"
    return (partes[0][0] + (partes[partes.length - 1][0] ?? "")).toUpperCase()
}

function corAvatar(p: Paciente | null): string {
    if (!p) return "hsl(var(--primary))"
    const paleta = [
        "hsl(254 56% 38%)", "hsl(190 60% 45%)", "hsl(280 55% 50%)",
        "hsl(140 45% 45%)", "hsl(40 70% 50%)", "hsl(340 55% 55%)",
        "hsl(220 55% 50%)", "hsl(170 50% 40%)",
    ]
    return paleta[p.id % paleta.length]
}

function idade(): number | null {
    const dataNasc = paciente.value?.dataNascimento
    if (!dataNasc) return null
    const nasc = new Date(dataNasc)
    if (isNaN(nasc.getTime())) return null
    const hoje = new Date()
    let anos = hoje.getFullYear() - nasc.getFullYear()
    const m = hoje.getMonth() - nasc.getMonth()
    if (m < 0 || (m === 0 && hoje.getDate() < nasc.getDate())) anos--
    return anos
}

function fmtData(iso: string | null | undefined): string {
    if (!iso) return "—"
    try { return new Date(iso).toLocaleDateString("pt-BR", { day: "2-digit", month: "short", year: "numeric" }) }
    catch { return iso }
}

function fmtDataHora(iso: string | null | undefined): string {
    if (!iso) return "—"
    try {
        const d = new Date(iso)
        return d.toLocaleDateString("pt-BR", { day: "2-digit", month: "short" })
            + " · " + d.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })
    } catch { return iso }
}

function fmtMoeda(n: number): string {
    return n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

function fmtGenero(g: string | null | undefined): string {
    const map: Record<string, string> = {
        Masculino: "Masculino", Feminino: "Feminino", Outro: "Outro", NaoInformado: "Não informado",
    }
    return g ? (map[g] ?? g) : "—"
}

function formatarCpf(cpf: string | null) {
    if (!cpf) return "—"
    return cpf.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, "$1.$2.$3-$4")
}

const cidadeUf = computed(() => {
    const end = paciente.value?.endereco ?? ""
    if (!end) return "—"
    const m = end.match(/([^,]+?)\s*-\s*([A-Z]{2})/)
    if (m) return `${m[1].trim()} / ${m[2]}`
    return end
})

// ─── Ações ─────────────────────────────────────────────────────────────────
const modalEditarAberto = ref(false)
function editar() { modalEditarAberto.value = true }
function onPacienteSalvo(p: Paciente) {
    paciente.value = p
    modalEditarAberto.value = false
    notificar("Dados do paciente atualizados.")
}

function abrirProntuario() {
    router.push({ name: "Prontuario", params: { id: pacienteId.value } })
}

function criarOrcamento() {
    router.push({ name: "OrcamentoNovo", query: { pacienteId: String(pacienteId.value) } })
}

function abrirOrcamento(o: OrcamentoResumo) {
    router.push({ name: "OrcamentoDetalhe", params: { id: String(o.id) } })
}

function novoAgendamento() {
    router.push({ name: "Agenda", query: { pacienteId: String(pacienteId.value) } })
}

// Para a aba Anamnese: extrai a primeira evolução estruturada.
const evolucaoMaisAntiga = computed(() => {
    const lista = prontuario.value?.evolucoes ?? []
    if (!lista.length) return null
    return [...lista].sort((a, b) =>
        new Date(a.criadaEm).getTime() - new Date(b.criadaEm).getTime(),
    )[0]
})

function valorSecao(secaoChave: string, conteudo: Record<string, unknown>): string {
    const v = conteudo[secaoChave]
    if (v == null || v === "") return ""
    if (typeof v === "string") return v
    if (typeof v === "number" || typeof v === "boolean") return String(v)
    return JSON.stringify(v)
}

// ─── Status helpers de orçamento ───────────────────────────────────────────
function orcStatusLabel(s: string): string {
    return s
}
function orcStatusClass(s: string): string {
    const lower = s.toLowerCase()
    if (lower.includes("aprov") || lower.includes("aceito") || lower.includes("conclu")) return "ok"
    if (lower.includes("recus") || lower.includes("cancel")) return "danger"
    if (lower.includes("expirad") || lower.includes("vencid")) return "muted"
    return "warning"
}
</script>

<template>
    <main class="app-page app-page--wide detalhe-paciente">
        <router-link :to="{ name: 'Pacientes' }" class="pd-back">
            <i class="fa-solid fa-arrow-left"></i>
            Voltar para a lista de pacientes
        </router-link>

        <p v-if="carregando" class="msg-info">Carregando…</p>
        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <template v-if="paciente">
            <!-- Header sticky -->
            <div class="pd-header">
                <div class="pd-head-main">
                    <div class="pd-avatar" :style="{ background: corAvatar(paciente) }">
                        {{ iniciais(paciente) }}
                    </div>
                    <div class="pd-info">
                        <div class="pd-name-row">
                            <h1>{{ paciente.nomeCompleto }}</h1>
                            <span class="pd-id">ID #{{ paciente.id }}</span>
                        </div>
                        <div class="pd-meta-row">
                            <span v-if="idade() !== null"><i class="fa-solid fa-cake-candles"></i> {{ idade() }} anos</span>
                            <span v-if="paciente.dataNascimento"><i class="fa-solid fa-calendar"></i> {{ fmtData(paciente.dataNascimento) }}</span>
                            <span v-if="paciente.genero"><i class="fa-solid fa-venus-mars"></i> {{ fmtGenero(paciente.genero) }}</span>
                            <span v-if="paciente.cpf"><i class="fa-solid fa-id-card"></i> {{ formatarCpf(paciente.cpf) }}</span>
                            <span v-if="paciente.telefone"><i class="fa-solid fa-phone"></i> {{ paciente.telefone }}</span>
                            <span v-if="paciente.email"><i class="fa-solid fa-envelope"></i> {{ paciente.email }}</span>
                            <span v-if="paciente.endereco"><i class="fa-solid fa-location-dot"></i> {{ cidadeUf }}</span>
                        </div>
                        <div v-if="paciente.tags.length" class="pd-tags">
                            <span
                                v-for="t in paciente.tags" :key="t"
                                class="tag-pill"
                                :style="{ background: `color-mix(in srgb, ${resolverTag(t).cor} 15%, white)`, color: resolverTag(t).cor }"
                            >
                                <i class="fa-solid" :class="resolverTag(t).icone"></i>
                                {{ resolverTag(t).label }}
                            </span>
                        </div>
                    </div>
                    <div class="pd-actions">
                        <AppButton variant="secondary" icon="fa-solid fa-pen" @click="editar">Editar</AppButton>
                        <AppButton variant="secondary" icon="fa-solid fa-calendar-plus" @click="novoAgendamento">Agendar consulta</AppButton>
                        <AppButton icon="fa-solid fa-notes-medical" @click="abrirProntuario">Abrir prontuário</AppButton>
                    </div>
                </div>

                <!-- Alertas clínicos -->
                <div v-if="paciente.alertas.length" class="pd-alerts">
                    <i class="fa-solid fa-triangle-exclamation"></i>
                    <div class="pd-alerts-content">
                        <b>Alertas clínicos</b>
                        <ul>
                            <li v-for="(a, i) in paciente.alertas" :key="i">{{ a }}</li>
                        </ul>
                    </div>
                </div>

                <!-- Quick stats -->
                <div class="pd-stats">
                    <div class="pd-stat">
                        <i class="fa-solid fa-clipboard-list"></i>
                        <b>{{ totalConsultas }}</b>
                        <span>consulta{{ totalConsultas !== 1 ? 's' : '' }}</span>
                    </div>
                    <div v-if="proximaConsulta" class="pd-stat">
                        <i class="fa-solid fa-calendar-check"></i>
                        <b>Próxima</b>
                        <span>{{ fmtDataHora(proximaConsulta.inicioPrevisto) }}</span>
                    </div>
                    <div class="pd-stat">
                        <i class="fa-solid fa-file-medical"></i>
                        <b>{{ totalProntuarios }}</b>
                        <span>evolução{{ totalProntuarios !== 1 ? 'ões' : '' }}</span>
                    </div>
                    <div class="pd-stat">
                        <i class="fa-solid fa-file-invoice-dollar"></i>
                        <b>{{ orcamentos.length || '—' }}</b>
                        <span>orçamento{{ orcamentos.length !== 1 ? 's' : '' }}</span>
                    </div>
                </div>

                <!-- Tabs -->
                <div class="pd-tabs">
                    <button class="pd-tab" :class="{ active: aba === 'resumo' }" @click="aba = 'resumo'">
                        <i class="fa-solid fa-chart-line"></i> Resumo
                    </button>
                    <button class="pd-tab" :class="{ active: aba === 'prontuario' }" @click="aba = 'prontuario'">
                        <i class="fa-solid fa-file-medical"></i> Prontuário
                        <span v-if="totalProntuarios > 0" class="badge">{{ totalProntuarios }}</span>
                    </button>
                    <button class="pd-tab" :class="{ active: aba === 'anamnese' }" @click="aba = 'anamnese'">
                        <i class="fa-solid fa-clipboard-user"></i> Anamnese
                    </button>
                    <button class="pd-tab" :class="{ active: aba === 'orcamentos' }" @click="aba = 'orcamentos'">
                        <i class="fa-solid fa-file-invoice-dollar"></i> Orçamentos
                        <span v-if="orcamentos.length > 0" class="badge">{{ orcamentos.length }}</span>
                    </button>
                    <button class="pd-tab" :class="{ active: aba === 'financeiro' }" @click="aba = 'financeiro'">
                        <i class="fa-solid fa-coins"></i> Financeiro
                    </button>
                    <button class="pd-tab" :class="{ active: aba === 'convenios' }" @click="aba = 'convenios'">
                        <i class="fa-solid fa-shield-halved"></i> Convênios
                    </button>
                    <button class="pd-tab" :class="{ active: aba === 'termos' }" @click="aba = 'termos'">
                        <i class="fa-solid fa-file-signature"></i> Termos
                    </button>
                    <button class="pd-tab" :class="{ active: aba === 'anexos' }" @click="aba = 'anexos'">
                        <i class="fa-solid fa-paperclip"></i> Anexos
                        <span v-if="anexos.length > 0" class="badge">{{ anexos.length }}</span>
                    </button>
                </div>
            </div>

            <!-- Conteúdo das abas -->
            <div class="pd-content">
                <!-- Resumo -->
                <section v-if="aba === 'resumo'" class="resumo-grid">
                    <div class="pd-card">
                        <div class="pd-card-head">
                            <h3><i class="fa-solid fa-bolt"></i> Próximas ações</h3>
                        </div>
                        <div class="next-actions">
                            <div v-if="proximaConsulta" class="na-item" @click="router.push({ name: 'Agenda' })">
                                <div class="na-icon na-icon--appointment">
                                    <i class="fa-solid fa-calendar-check"></i>
                                </div>
                                <div class="na-info">
                                    <b>Próxima consulta</b>
                                    <span>com {{ proximaConsulta.profissionalNome }} · {{ proximaConsulta.tipoServico }}</span>
                                </div>
                                <div class="na-when">{{ fmtDataHora(proximaConsulta.inicioPrevisto) }}</div>
                            </div>
                            <div class="na-item" @click="abrirProntuario">
                                <div class="na-icon na-icon--prontuario">
                                    <i class="fa-solid fa-notes-medical"></i>
                                </div>
                                <div class="na-info">
                                    <b>Continuar prontuário</b>
                                    <span>{{ totalProntuarios > 0 ? `${totalProntuarios} evoluções registradas` : 'Sem evoluções ainda' }}</span>
                                </div>
                                <i class="fa-solid fa-chevron-right na-arrow"></i>
                            </div>
                            <div class="na-item" @click="criarOrcamento">
                                <div class="na-icon na-icon--budget">
                                    <i class="fa-solid fa-file-invoice-dollar"></i>
                                </div>
                                <div class="na-info">
                                    <b>Criar orçamento</b>
                                    <span>{{ orcamentos.length > 0 ? `${orcamentos.length} orçamento(s) registrados` : 'Sem orçamentos ainda' }}</span>
                                </div>
                                <i class="fa-solid fa-chevron-right na-arrow"></i>
                            </div>
                        </div>
                    </div>

                    <div class="pd-card side-card">
                        <h4>Dados do paciente</h4>
                        <div class="stat-line"><span>CPF</span><b>{{ paciente.cpf ? formatarCpf(paciente.cpf) : "—" }}</b></div>
                        <div class="stat-line"><span>Nascimento</span><b>{{ fmtData(paciente.dataNascimento) }}</b></div>
                        <div class="stat-line"><span>Sexo</span><b>{{ fmtGenero(paciente.genero) }}</b></div>
                        <div class="stat-line"><span>Telefone</span><b>{{ paciente.telefone || "—" }}</b></div>
                        <div class="stat-line"><span>E-mail</span><b>{{ paciente.email || "—" }}</b></div>
                        <div class="stat-line"><span>Endereço</span><b>{{ cidadeUf }}</b></div>
                    </div>
                </section>

                <!-- Prontuário (timeline) -->
                <section v-else-if="aba === 'prontuario'">
                    <div class="prontuario-head">
                        <div>
                            <h2>Histórico de evoluções</h2>
                            <p>Linha do tempo cronológica das evoluções clínicas registradas.</p>
                        </div>
                        <AppButton icon="fa-solid fa-notes-medical" @click="abrirProntuario">
                            Abrir prontuário completo
                        </AppButton>
                    </div>

                    <p v-if="carregandoProntuario" class="msg-info">Carregando…</p>

                    <AppEmptyState
                        v-else-if="!prontuario || prontuario.evolucoes.length === 0"
                        icone="📋"
                        titulo="Sem evoluções registradas"
                        descricao="Abra o prontuário do paciente para iniciar uma evolução clínica."
                    >
                        <template #acao>
                            <AppButton icon="fa-solid fa-notes-medical" @click="abrirProntuario">
                                Abrir prontuário
                            </AppButton>
                        </template>
                    </AppEmptyState>

                    <div v-else class="ht-timeline-full" role="list">
                        <EvolucaoTimelineItem
                            v-for="ev in prontuario.evolucoes"
                            :key="ev.id"
                            :evolucao="ev"
                            :destaque="ev.id === idEvolucaoMaisRecente"
                            :gerando-pdf="evolucaoSendoBaixada === ev.id"
                            @gerar-pdf="exportarPdfEvolucao"
                        />
                    </div>
                </section>

                <!-- Anamnese -->
                <section v-else-if="aba === 'anamnese'">
                    <div class="prontuario-head">
                        <div>
                            <h2>Anamnese</h2>
                            <p>Vista resumida da primeira evolução registrada (anamnese inicial).</p>
                        </div>
                    </div>

                    <p v-if="carregandoProntuario" class="msg-info">Carregando…</p>

                    <AppEmptyState
                        v-else-if="!evolucaoMaisAntiga"
                        icone="🩺"
                        titulo="Sem anamnese registrada"
                        descricao="A anamnese aparece aqui após a primeira evolução do paciente."
                    >
                        <template #acao>
                            <AppButton icon="fa-solid fa-notes-medical" @click="abrirProntuario">
                                Abrir prontuário
                            </AppButton>
                        </template>
                    </AppEmptyState>

                    <div v-else class="anamn-grid">
                        <div
                            v-for="secao in evolucaoMaisAntiga.modeloSnapshot" :key="secao.chave"
                            class="pd-card an-card"
                        >
                            <h4><i class="fa-solid fa-circle-dot"></i> {{ secao.titulo }}</h4>
                            <p>{{ valorSecao(secao.chave, evolucaoMaisAntiga.conteudo) || "—" }}</p>
                        </div>
                    </div>
                </section>

                <!-- Orçamentos -->
                <section v-else-if="aba === 'orcamentos'">
                    <div class="prontuario-head">
                        <div>
                            <h2>Orçamentos do paciente</h2>
                            <p>Histórico de orçamentos cirúrgicos e propostas comerciais.</p>
                        </div>
                        <AppButton variant="secondary" icon="fa-solid fa-plus" @click="criarOrcamento">
                            Criar orçamento
                        </AppButton>
                    </div>

                    <p v-if="carregandoOrc" class="msg-info">Carregando…</p>

                    <AppEmptyState
                        v-else-if="orcamentos.length === 0"
                        icone="💰"
                        titulo="Nenhum orçamento cadastrado"
                        descricao="Crie o primeiro orçamento para este paciente."
                    >
                        <template #acao>
                            <AppButton icon="fa-solid fa-plus" @click="criarOrcamento">Criar orçamento</AppButton>
                        </template>
                    </AppEmptyState>

                    <div v-else class="budgets-list">
                        <div
                            v-for="o in orcamentos" :key="o.id"
                            class="budget-card"
                            @click="abrirOrcamento(o)"
                        >
                            <div class="bc-head">
                                <div class="bc-title">
                                    <span class="bc-num">ORÇAMENTO #{{ o.numero }}</span>
                                    <span class="bc-name">{{ o.pacienteNome }}</span>
                                    <span class="bc-meta">
                                        Criado em {{ fmtData(o.criadoEm) }} ·
                                        Validade {{ fmtData(o.validade) }}
                                    </span>
                                </div>
                                <div class="bc-status">
                                    <span class="bc-money">{{ fmtMoeda(o.total) }}</span>
                                    <span class="orc-status" :class="`orc-status--${orcStatusClass(o.status)}`">
                                        {{ orcStatusLabel(o.status) }}
                                    </span>
                                </div>
                            </div>
                        </div>
                    </div>
                </section>

                <!-- Anexos -->
                <section v-else-if="aba === 'anexos'">
                    <div class="prontuario-head">
                        <div>
                            <h2>Anexos do prontuário</h2>
                            <p>Exames, laudos, imagens e documentos enviados.</p>
                        </div>
                    </div>

                    <p v-if="carregandoAnexos" class="msg-info">Carregando…</p>

                    <AppEmptyState
                        v-else-if="anexos.length === 0"
                        icone="📎"
                        titulo="Nenhum anexo"
                        descricao="Os anexos aparecem aqui conforme forem enviados ao prontuário."
                    />

                    <div v-else class="att-grid">
                        <div v-for="a in anexos" :key="a.id" class="att-card">
                            <div class="att-icon" :class="a.mimeType.startsWith('image') ? 'image' : 'pdf'">
                                <i class="fa-solid" :class="a.mimeType.startsWith('image') ? 'fa-image' : 'fa-file-pdf'"></i>
                            </div>
                            <div class="att-info">
                                <b>{{ a.nomeOriginal }}</b>
                                <span>{{ fmtData(a.criadoEm) }} · {{ Math.round(a.tamanhoBytes / 1024) }} KB</span>
                            </div>
                        </div>
                    </div>
                </section>

                <!-- Financeiro / Convênios / Termos -->
                <section v-else-if="aba === 'financeiro'">
                    <AppEmptyState
                        icone="💳"
                        titulo="Financeiro do paciente"
                        descricao="Cobranças, recebimentos e saldo em aberto deste paciente serão exibidos aqui em breve."
                    />
                </section>

                <section v-else-if="aba === 'convenios'">
                    <AppEmptyState
                        icone="🛡️"
                        titulo="Convênios e autorizações"
                        descricao="Histórico de autorizações de plano de saúde e glosas serão exibidos aqui em breve."
                    />
                </section>

                <section v-else-if="aba === 'termos'">
                    <AppEmptyState
                        icone="📝"
                        titulo="Termos de consentimento"
                        descricao="Termos LGPD, consentimentos cirúrgicos e demais documentos assinados serão listados aqui em breve."
                    />
                </section>
            </div>
        </template>

        <!-- Modal de edição -->
        <PacienteFormModal
            :aberto="modalEditarAberto"
            :paciente="paciente"
            @fechar="modalEditarAberto = false"
            @salvo="onPacienteSalvo"
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
.detalhe-paciente { min-height: 100%; }

.pd-back {
    align-self: flex-start;
    display: inline-flex; align-items: center; gap: 6px;
    font-size: 12px; font-weight: 600;
    color: hsl(var(--secondary) / 0.7);
    text-decoration: none;
    transition: color 150ms;
}
.pd-back:hover { color: hsl(var(--primary)); }

/* Header sticky */
.pd-header {
    position: sticky; top: 0; z-index: 30;
    background: white;
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    padding-top: 16px;
}
.pd-head-main {
    display: flex; gap: 20px; align-items: flex-start;
    padding-bottom: 14px; flex-wrap: wrap;
}
.pd-avatar {
    width: 84px; height: 84px; border-radius: 50%;
    color: white; font-weight: 700; font-size: 28px;
    display: flex; align-items: center; justify-content: center;
    flex-shrink: 0;
    box-shadow: 0 6px 18px -6px rgb(0 0 0 / 0.1);
}
.pd-info { flex: 1; min-width: 320px; }
.pd-name-row { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; margin-bottom: 4px; }
.pd-info h1 {
    font-size: 24px; font-weight: 800;
    color: hsl(var(--primary-dark)); margin: 0;
    letter-spacing: -0.01em;
}
.pd-id { font-size: 11px; color: hsl(var(--secondary) / 0.5); font-weight: 600; }

.pd-meta-row {
    display: flex; gap: 6px 14px; flex-wrap: wrap;
    font-size: 12.5px; color: hsl(var(--secondary) / 0.75);
    margin-bottom: 8px; row-gap: 4px;
}
.pd-meta-row > span { display: inline-flex; align-items: center; gap: 5px; }
.pd-meta-row i { font-size: 11px; color: hsl(var(--secondary) / 0.45); }

.pd-tags { display: flex; gap: 5px; flex-wrap: wrap; }
.tag-pill {
    display: inline-flex; align-items: center; gap: 4px;
    font-size: 10px; font-weight: 700;
    padding: 3px 7px; border-radius: 999px;
    white-space: nowrap;
}
.tag-pill i { font-size: 9px; }

.pd-actions {
    display: flex; gap: 8px; flex-shrink: 0; flex-wrap: wrap;
    margin-left: auto;
}

/* Alertas */
.pd-alerts {
    display: flex; align-items: flex-start; gap: 12px;
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-radius: 8px; padding: 10px 14px;
    margin-bottom: 14px;
}
.pd-alerts > i { font-size: 16px; color: hsl(var(--error)); margin-top: 2px; }
.pd-alerts-content { flex: 1; }
.pd-alerts-content b {
    display: block; font-size: 12px; font-weight: 700;
    color: hsl(var(--error)); text-transform: uppercase; letter-spacing: 0.04em;
}
.pd-alerts-content ul { margin: 4px 0 0; padding-left: 18px; font-size: 13px; color: hsl(0 70% 25%); }
.pd-alerts-content li { line-height: 1.5; }

/* Quick stats */
.pd-stats { display: flex; gap: 8px; padding-bottom: 12px; flex-wrap: wrap; }
.pd-stat {
    display: flex; align-items: center; gap: 8px;
    background: hsl(var(--secondary) / 0.03);
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 8px; padding: 6px 12px;
    font-size: 12px;
}
.pd-stat i { color: hsl(var(--secondary) / 0.5); }
.pd-stat b { color: hsl(var(--primary-dark)); font-weight: 700; }
.pd-stat span { color: hsl(var(--secondary) / 0.65); }

/* Tabs */
.pd-tabs {
    display: flex; gap: 0;
    overflow-x: auto;
    border-top: 1px solid hsl(var(--secondary) / 0.06);
}
.pd-tab {
    display: inline-flex; align-items: center; gap: 8px;
    background: transparent; border: none; padding: 12px 16px;
    font-family: inherit; font-size: 13px; font-weight: 600;
    color: hsl(var(--secondary) / 0.6);
    cursor: pointer;
    border-bottom: 2px solid transparent;
    white-space: nowrap;
    transition: color 150ms;
}
.pd-tab:hover { color: hsl(var(--primary-dark)); }
.pd-tab.active { color: hsl(var(--primary)); border-bottom-color: hsl(var(--primary)); }
.pd-tab .badge {
    font-size: 10px; font-weight: 700; padding: 1px 6px;
    border-radius: 999px;
    background: hsl(var(--secondary) / 0.08); color: hsl(var(--secondary) / 0.65);
}
.pd-tab.active .badge { background: hsl(var(--primary) / 0.15); color: hsl(var(--primary)); }

/* Conteúdo das abas */
.pd-content { padding-bottom: 56px; }

/* Cards */
.pd-card {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 12px;
    padding: 20px;
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
}
.pd-card-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }
.pd-card-head h3 {
    font-size: 14px; font-weight: 700;
    color: hsl(var(--primary-dark));
    margin: 0;
    display: inline-flex; align-items: center; gap: 8px;
}
.pd-card-head h3 i { color: hsl(var(--primary)); font-size: 13px; }

/* Resumo */
.resumo-grid {
    display: grid;
    grid-template-columns: 2fr 1fr;
    gap: 16px;
    align-items: start;
}

.next-actions { display: flex; flex-direction: column; gap: 10px; }
.na-item {
    display: flex; align-items: center; gap: 12px;
    padding: 12px;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 8px;
    background: hsl(var(--secondary) / 0.02);
    transition: all 150ms;
    cursor: pointer;
}
.na-item:hover {
    background: white;
    border-color: hsl(var(--primary) / 0.25);
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
}
.na-icon {
    width: 36px; height: 36px; border-radius: 8px;
    display: flex; align-items: center; justify-content: center;
    font-size: 14px; flex-shrink: 0;
}
.na-icon--appointment { background: hsl(var(--primary) / 0.12); color: hsl(var(--primary)); }
.na-icon--prontuario  { background: hsl(var(--info) / 0.12);    color: hsl(var(--info)); }
.na-icon--budget      { background: hsl(45 95% 50% / 0.15);     color: hsl(40 90% 32%); }
.na-info { flex: 1; }
.na-info b { display: block; font-size: 13px; color: hsl(var(--primary-dark)); font-weight: 700; }
.na-info span { font-size: 12px; color: hsl(var(--secondary) / 0.65); }
.na-when { font-size: 11px; font-weight: 700; color: hsl(var(--secondary) / 0.55); text-align: right; }
.na-arrow { color: hsl(var(--secondary) / 0.3); font-size: 11px; }

.side-card h4 {
    font-size: 12px; font-weight: 700;
    color: hsl(var(--secondary) / 0.55);
    text-transform: uppercase; letter-spacing: 0.05em;
    margin: 0 0 10px;
}
.side-card .stat-line {
    display: flex; justify-content: space-between; align-items: baseline;
    padding: 8px 0;
    border-bottom: 1px solid hsl(var(--secondary) / 0.05);
}
.side-card .stat-line:last-child { border-bottom: none; }
.side-card .stat-line span { font-size: 12px; color: hsl(var(--secondary) / 0.65); }
.side-card .stat-line b { font-size: 13px; color: hsl(var(--primary-dark)); font-weight: 700; }

/* Prontuário head + timeline */
.prontuario-head {
    display: flex; align-items: center; justify-content: space-between;
    gap: 16px; margin-bottom: 16px;
}
.prontuario-head h2 { font-size: 18px; font-weight: 700; color: hsl(var(--primary-dark)); margin: 0; }
.prontuario-head p { font-size: 13px; color: hsl(var(--secondary) / 0.7); margin: 4px 0 0; }

/* Wrapper de timeline (compartilhado com ConsultasAnterioresTab via EvolucaoTimelineItem) */
.ht-timeline-full {
    display: flex; flex-direction: column; gap: 16px;
    position: relative; padding-left: 50px;
}
.ht-timeline-full::before {
    content: ""; position: absolute; left: 21px; top: 8px; bottom: 8px;
    width: 2px;
    background: linear-gradient(to bottom,
        hsl(var(--primary) / 0.3),
        hsl(var(--secondary) / 0.08));
}

/* Anamnese */
.anamn-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.an-card h4 {
    font-size: 13px; font-weight: 700;
    color: hsl(var(--primary-dark));
    margin: 0 0 12px;
    display: inline-flex; align-items: center; gap: 8px;
}
.an-card h4 i { color: hsl(var(--primary)); font-size: 8px; }
.an-card p { font-size: 13px; color: hsl(var(--secondary)); line-height: 1.55; margin: 0; white-space: pre-line; }

/* Orçamentos */
.budgets-list { display: flex; flex-direction: column; gap: 10px; }
.budget-card {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 12px;
    padding: 16px 20px;
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
    transition: all 150ms;
    cursor: pointer;
}
.budget-card:hover { box-shadow: 0 2px 8px -2px rgb(0 0 0 / 0.06); }
.bc-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 14px; }
.bc-title { display: flex; flex-direction: column; gap: 4px; }
.bc-num { font-size: 11px; font-weight: 700; color: hsl(var(--primary)); letter-spacing: 0.04em; }
.bc-name { font-size: 15px; font-weight: 700; color: hsl(var(--primary-dark)); }
.bc-meta { font-size: 12px; color: hsl(var(--secondary) / 0.65); }
.bc-status { display: flex; flex-direction: column; align-items: flex-end; gap: 4px; }
.bc-money { font-size: 18px; font-weight: 800; color: hsl(var(--primary-dark)); }
.orc-status {
    font-size: 11px; font-weight: 700;
    padding: 3px 9px; border-radius: 999px;
}
.orc-status--ok      { background: hsl(var(--success) / 0.12); color: hsl(160 79% 30%); }
.orc-status--warning { background: hsl(var(--warning) / 0.18); color: hsl(40 95% 35%); }
.orc-status--danger  { background: hsl(var(--error) / 0.1);    color: hsl(var(--error)); }
.orc-status--muted   { background: hsl(var(--secondary) / 0.08); color: hsl(var(--secondary) / 0.55); }

/* Anexos */
.att-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 10px; }
.att-card {
    background: white;
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 8px;
    padding: 14px;
    display: flex; gap: 12px; align-items: center;
    cursor: pointer;
    transition: all 150ms;
}
.att-card:hover {
    border-color: hsl(var(--primary) / 0.3);
    box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.04);
}
.att-icon {
    width: 42px; height: 42px; border-radius: 8px;
    display: flex; align-items: center; justify-content: center;
    font-size: 16px; flex-shrink: 0;
}
.att-icon.pdf   { background: hsl(var(--error) / 0.1); color: hsl(var(--error)); }
.att-icon.image { background: hsl(280 55% 50% / 0.12); color: hsl(280 55% 50%); }
.att-info { flex: 1; min-width: 0; }
.att-info b {
    display: block; font-size: 13px; color: hsl(var(--primary-dark));
    font-weight: 600;
    white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}
.att-info span { font-size: 11px; color: hsl(var(--secondary) / 0.6); }

.msg-info { color: hsl(var(--secondary) / 0.7); margin: 24px 28px; }
.msg-erro {
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-radius: 8px; padding: 10px 14px;
    font-size: 13px; margin: 24px 28px;
}

@media (max-width: 1200px) {
    .pd-head-main { flex-direction: column; }
    .pd-actions {
        width: 100%; margin-left: 0;
        justify-content: flex-end;
        padding-top: 4px;
        border-top: 1px solid hsl(var(--secondary) / 0.05);
    }
    .resumo-grid { grid-template-columns: 1fr; }
    .anamn-grid { grid-template-columns: 1fr; }
}
</style>
