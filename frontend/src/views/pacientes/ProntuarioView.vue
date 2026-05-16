<script setup lang="ts">
/**
 * Prontuário do paciente — visual care/prontuário do design Imedto.
 *
 * Estrutura:
 *   - Sticky header (ProntuarioPacienteHeader): paciente + alertas + ações
 *     (modo foco, imprimir, receita, finalizar e assinar) e timer ao vivo.
 *   - 4 abas (mantidas): Consulta atual / Consultas anteriores / Exame físico
 *     / Receitas. A "Consulta atual" usa layout 3 colunas com módulos.
 *   - "Finalizar e assinar" chama POST /agendamentos/:id/concluir e limpa a
 *     marca local de atendimento ativo.
 */
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import {
    prontuarioService,
    type Anexo,
    type ModeloProntuario,
    type ProntuarioCompleto,
} from "@/services/prontuarioService"
import { pacienteService, type Paciente } from "@/services/pacienteService"
import { agendaService, type Agendamento } from "@/services/agendaService"
import { useProntuarioPdf } from "@/composables/useProntuarioPdf"
import { useTenantStore } from "@/stores/tenantStore"
import { useAtendimentoAtivo } from "@/composables/useAtendimentoAtivo"
import { AppButton, AppField, AppSelect, AppToast } from "@/components/ui"
import ProntuarioPacienteHeader    from "@/components/prontuario/ProntuarioPacienteHeader.vue"
import ProntuarioTabs, { type AbaProntuario } from "@/components/prontuario/ProntuarioTabs.vue"
import ConsultaAtualTab            from "@/components/prontuario/tabs/ConsultaAtualTab.vue"
import ConsultasAnterioresTab      from "@/components/prontuario/tabs/ConsultasAnterioresTab.vue"
import ExameFisicoTab              from "@/components/prontuario/tabs/ExameFisicoTab.vue"
import ReceitasTab                 from "@/components/prontuario/tabs/ReceitasTab.vue"

const { gerarPdf: gerarPdfProntuario } = useProntuarioPdf()

const route  = useRoute()
const router = useRouter()
const tenant = useTenantStore()
const { ehEsteAtendimento, finalizar: limparAtendimentoLocal } = useAtendimentoAtivo()

const pacienteId = computed(() => Number(route.params.id))
const eventoId   = computed(() => {
    const v = route.query.eventoId
    return v ? Number(v) : null
})
const paciente    = ref<Paciente | null>(null)
const agendamento = ref<Agendamento | null>(null)
const pront       = ref<ProntuarioCompleto | null>(null)
const anexos      = ref<Anexo[]>([])
const modelosDisponiveis  = ref<ModeloProntuario[]>([])
const modeloEscolhido     = ref<number | null>(null)
const modeloConsultaAtual = ref<number | null>(null)

const carregando = ref(true)
const erro       = ref<string | null>(null)

const abaAtiva = ref<AbaProntuario>("consulta")
const focus    = ref(false)

// Toast simples
const toast = ref<{ msg: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(msg: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { msg, variante }
}

// Seções do modelo atualmente selecionado
const secoesConsultaAtual = computed(() => {
    const modelo = modelosDisponiveis.value.find(m => m.id === modeloConsultaAtual.value)
    return modelo?.estrutura ?? pront.value?.prontuario.modeloEstrutura ?? []
})

watch(modeloConsultaAtual, () => {
    if (pront.value) inicializarFormEvolucao()
})

const novaEvolucao = reactive<Record<string, any>>({})
const salvandoEvolucao = ref(false)

const SECOES_ESTRUTURADAS = new Set([
    "hpp",
    "h-familiar",
    "h-social",
    "exame-fisico",
    "exames-realizados",
    "procedimentos-indicados",
])

const uploadPendente = ref<File | null>(null)
const uploadando     = ref(false)

onMounted(carregar)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const [pacienteCarregado, agendamentoCarregado, prontuarioCarregado, modelos] = await Promise.all([
            pacienteService.obter(pacienteId.value),
            eventoId.value
                ? agendaService.obter(eventoId.value).catch(() => null)
                : Promise.resolve(null),
            prontuarioService.obter(pacienteId.value),
            prontuarioService.listarModelos(),
        ])
        paciente.value    = pacienteCarregado
        agendamento.value = agendamentoCarregado
        modelosDisponiveis.value = modelos
        pront.value = prontuarioCarregado
        if (pront.value) {
            modeloConsultaAtual.value = pront.value.prontuario.modeloDeProntuarioId
            inicializarFormEvolucao()
        } else {
            modeloEscolhido.value = modelos.length > 0 ? modelos[0].id : null
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar prontuário."
    } finally {
        carregando.value = false
    }
}

const abasCarregadas = new Set<AbaProntuario>()

async function garantirAba(a: AbaProntuario) {
    if (abasCarregadas.has(a)) return
    if (a === "anteriores" && pront.value) {
        try {
            anexos.value = await prontuarioService.listarAnexos(pacienteId.value)
        } catch { /* sem anexos */ }
        abasCarregadas.add("anteriores")
    }
}

watch(abaAtiva, garantirAba, { immediate: true })

function inicializarFormEvolucao() {
    for (const k of Object.keys(novaEvolucao)) delete novaEvolucao[k]
    for (const secao of secoesConsultaAtual.value) {
        novaEvolucao[secao.chave] = SECOES_ESTRUTURADAS.has(secao.chave) ? {} : ""
    }
}

function temConteudo(valor: any): boolean {
    if (valor === null || valor === undefined) return false
    if (typeof valor === "string") return valor.trim().length > 0
    if (typeof valor === "object") return Object.values(valor).some(v => temConteudo(v))
    if (typeof valor === "boolean") return true
    if (Array.isArray(valor)) return valor.length > 0
    return false
}

async function iniciarProntuario() {
    if (!modeloEscolhido.value) return
    try {
        await prontuarioService.iniciar(pacienteId.value, modeloEscolhido.value)
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao iniciar prontuário."
    }
}

async function salvarEvolucao() {
    salvandoEvolucao.value = true
    erro.value = null
    try {
        const conteudoNaoVazio = Object.fromEntries(
            Object.entries(novaEvolucao).filter(([, v]) => temConteudo(v)),
        )
        if (Object.keys(conteudoNaoVazio).length === 0) {
            notificar("Preencha ao menos uma seção antes de salvar.", "error")
            return
        }
        const modeloOverride = modeloConsultaAtual.value !== pront.value?.prontuario.modeloDeProntuarioId
            ? (modeloConsultaAtual.value ?? undefined)
            : undefined
        await prontuarioService.registrarEvolucao(pacienteId.value, conteudoNaoVazio, modeloOverride)
        inicializarFormEvolucao()
        await carregar()
        abaAtiva.value = "anteriores"
        notificar("Evolução salva com sucesso.", "success")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao salvar evolução.", "error")
    } finally {
        salvandoEvolucao.value = false
    }
}

function selecionarArquivo(event: Event) {
    const input = event.target as HTMLInputElement
    uploadPendente.value = input.files?.[0] ?? null
}

async function enviarAnexo() {
    if (!uploadPendente.value) return
    uploadando.value = true
    erro.value = null
    try {
        await prontuarioService.uploadAnexo(pacienteId.value, uploadPendente.value)
        uploadPendente.value = null
        anexos.value = await prontuarioService.listarAnexos(pacienteId.value)
        notificar("Anexo enviado.", "success")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao enviar anexo.", "error")
    } finally {
        uploadando.value = false
    }
}

async function baixarAnexo(anexo: Anexo) {
    try {
        const { url } = await prontuarioService.obterUrlAnexo(pacienteId.value, anexo.id)
        window.open(url, "_blank")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao gerar link do anexo.", "error")
    }
}

function voltar() {
    router.back()
}

// ─── Handlers do header sticky ───────────────────────────────────────────────
function toggleFocus() {
    focus.value = !focus.value
}

function imprimir() {
    if (!pront.value) {
        notificar("Inicie o prontuário antes de imprimir.", "error")
        return
    }
    gerarPdfProntuario(pront.value, paciente.value ?? "paciente")
}

function abrirReceita() {
    abaAtiva.value = "receitas"
}

async function finalizarAtendimento() {
    if (!agendamento.value) {
        // Sem agendamento: só limpa marca local
        if (eventoId.value) limparAtendimentoLocal()
        notificar("Atendimento encerrado.", "success")
        router.push({ name: "MinhasConsultas" })
        return
    }
    try {
        await agendaService.concluir(agendamento.value.id)
        if (ehEsteAtendimento(agendamento.value.id)) limparAtendimentoLocal()
        notificar("Atendimento concluído.", "success")
        router.push({ name: "MinhasConsultas" })
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao concluir atendimento.", "error")
    }
}

// Atalhos: F = toggle focus
function onKey(e: KeyboardEvent) {
    if (e.target && (e.target as HTMLElement).matches("input, textarea, select, [contenteditable]")) return
    if (e.key === "F" || e.key === "f") {
        e.preventDefault()
        toggleFocus()
    } else if (e.key === "Escape" && focus.value) {
        focus.value = false
    }
}
onMounted(() => window.addEventListener("keydown", onKey))
onBeforeUnmount(() => window.removeEventListener("keydown", onKey))
</script>

<template>
    <main class="app-page app-page--wide pront-shell" :class="{ 'focus-mode': focus }">
        <!-- Sticky header do paciente -->
        <ProntuarioPacienteHeader
            :paciente="paciente"
            :agendamento="agendamento"
            :estabelecimento="tenant.ativo?.nomeFantasia ?? null"
            :focus="focus"
            :sem-acoes="!pront"
            @voltar="voltar"
            @toggle-focus="toggleFocus"
            @imprimir="imprimir"
            @receita="abrirReceita"
            @finalizar="finalizarAtendimento"
        />

        <p v-if="carregando" class="estado-msg">Carregando prontuário...</p>
        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <!-- Prontuário não iniciado: escolher modelo -->
        <section v-if="!carregando && !pront" class="iniciar-card">
            <h2 class="iniciar-titulo">
                <i class="fa-solid fa-stethoscope"></i>
                Iniciar prontuário
            </h2>
            <p class="iniciar-sub">Escolha um modelo para estruturar o prontuário deste paciente.</p>
            <AppField label="Modelo">
                <AppSelect v-model.number="modeloEscolhido">
                    <option :value="null" disabled>Selecione um modelo</option>
                    <option v-for="m in modelosDisponiveis" :key="m.id" :value="m.id">
                        {{ m.nome }}{{ m.ehPadraoSistema ? " (sistema)" : "" }}
                    </option>
                </AppSelect>
            </AppField>
            <AppButton :disabled="!modeloEscolhido" @click="iniciarProntuario">
                Iniciar prontuário
            </AppButton>
        </section>

        <!-- Prontuário iniciado: tabs + conteúdo -->
        <template v-if="pront">
            <ProntuarioTabs
                v-show="!focus"
                v-model="abaAtiva"
                :contagem-anteriores="pront.evolucoes.length"
            />

            <ConsultaAtualTab
                v-if="abaAtiva === 'consulta' && modeloConsultaAtual !== null"
                v-model:modelo-id="modeloConsultaAtual"
                :modelos="modelosDisponiveis"
                :secoes="secoesConsultaAtual"
                :nova-evolucao="novaEvolucao"
                :salvando="salvandoEvolucao"
                :focus="focus"
                @salvar="salvarEvolucao"
            />

            <ConsultasAnterioresTab
                v-else-if="abaAtiva === 'anteriores'"
                :evolucoes="pront.evolucoes"
                :anexos="anexos"
                :uploadando="uploadando"
                :gerar-pdf="() => gerarPdfProntuario(pront!, paciente ?? 'paciente')"
                @download-anexo="baixarAnexo"
                @selecionar-arquivo="selecionarArquivo"
                @enviar-anexo="enviarAnexo"
            />

            <ExameFisicoTab
                v-else-if="abaAtiva === 'exame'"
                :paciente-id="String(pacienteId)"
                :paciente-sexo="paciente?.genero ?? null"
                :salvando="salvandoEvolucao"
                @salvar="salvarEvolucao"
            />

            <ReceitasTab
                v-else-if="abaAtiva === 'receitas' && paciente"
                :paciente-id="pacienteId"
                :paciente-nome="paciente.nomeCompleto"
            />
        </template>

        <AppToast
            v-if="toast"
            :mensagem="toast.msg"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </main>
</template>

<style scoped>
.pront-shell { gap: 0; }

/* Iniciar prontuário */
.iniciar-card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius-lg);
    padding: 2rem;
    max-width: 500px; margin: 2rem auto;
    display: flex; flex-direction: column; gap: 1rem;
}
.iniciar-titulo {
    font-size: 1.05rem; font-weight: 700; margin: 0;
    color: hsl(var(--primary-dark));
    display: inline-flex; align-items: center; gap: 8px;
}
.iniciar-titulo i { color: hsl(var(--primary)); }
.iniciar-sub { font-size: 0.85em; color: var(--text-muted); margin: 0; }

/* Estados */
.estado-msg { text-align: center; color: var(--text-muted); padding: 2rem 1rem; font-size: 0.9em; }
.msg-erro   { color: hsl(var(--error)); font-size: 0.875em; margin: 0 0 1rem; }
</style>
