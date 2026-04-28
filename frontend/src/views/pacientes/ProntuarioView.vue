<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import {
    prontuarioService,
    type Anexo,
    type ModeloProntuario,
    type ProntuarioCompleto,
} from "@/services/prontuarioService"
import { pacienteService, type Paciente } from "@/services/pacienteService"
import { agendaService, type Agendamento } from "@/services/agendaService"
import { sugerirSecaoProntuario } from "@/services/iaService"
import { useProntuarioPdf } from "@/composables/useProntuarioPdf"
import { useTenantStore } from "@/stores/tenantStore"
import { AppButton, AppField, AppSelect } from "@/components/ui"
import ProntuarioPacienteHeader    from "@/components/prontuario/ProntuarioPacienteHeader.vue"
import ProntuarioTabs, { type AbaProntuario } from "@/components/prontuario/ProntuarioTabs.vue"
import ConsultaAtualTab            from "@/components/prontuario/tabs/ConsultaAtualTab.vue"
import ConsultasAnterioresTab      from "@/components/prontuario/tabs/ConsultasAnterioresTab.vue"
import ExameFisicoTab              from "@/components/prontuario/tabs/ExameFisicoTab.vue"
import ReceitasTab                 from "@/components/prontuario/tabs/ReceitasTab.vue"
import AssistenteTab               from "@/components/prontuario/tabs/AssistenteTab.vue"

const { gerarPdf: gerarPdfProntuario } = useProntuarioPdf()

const route  = useRoute()
const router = useRouter()
const tenant = useTenantStore()

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
const modeloEscolhido     = ref<number | null>(null)   // usado só na tela de iniciar
const modeloConsultaAtual = ref<number | null>(null)   // modelo ativo na tab de consulta atual

const carregando = ref(true)
const erro       = ref<string | null>(null)

const abaAtiva = ref<AbaProntuario>("consulta")

// Seções do modelo atualmente selecionado para a consulta
const secoesConsultaAtual = computed(() => {
    const modelo = modelosDisponiveis.value.find(m => m.id === modeloConsultaAtual.value)
    return modelo?.estrutura ?? pront.value?.prontuario.modeloEstrutura ?? []
})

// Quando o usuário troca de modelo, reinicia o formulário de evolução
watch(modeloConsultaAtual, () => {
    if (pront.value) inicializarFormEvolucao()
})

// ─── Nova evolução ────────────────────────────────────────────────────────────
const novaEvolucao = reactive<Record<string, any>>({})
const salvandoEvolucao = ref(false)

const SECOES_ESTRUTURADAS = new Set([
    "historia_pregressa",
    "historia_familiar",
    "historia_social",
    "exame_fisico",
    "exames_realizados",
    "procedimentos_indicados",
])

// ─── Anexos ───────────────────────────────────────────────────────────────────
const uploadPendente = ref<File | null>(null)
const uploadando     = ref(false)

// ─── IA ──────────────────────────────────────────────────────────────────────
const gerandoIa = ref<Record<string, boolean>>({})
const erroIa    = ref<Record<string, string | null>>({})
const resumoIaGerando = ref(false)
const resumoIa        = ref<string>("")

onMounted(carregar)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        paciente.value = await pacienteService.obter(pacienteId.value)
        if (eventoId.value) {
            try { agendamento.value = await agendaService.obter(eventoId.value) }
            catch { agendamento.value = null }
        }
        const [prontuarioCarregado, modelos] = await Promise.all([
            prontuarioService.obter(pacienteId.value),
            prontuarioService.listarModelos(),
        ])
        modelosDisponiveis.value = modelos
        pront.value = prontuarioCarregado
        if (pront.value) {
            modeloConsultaAtual.value = pront.value.prontuario.modeloDeProntuarioId
            anexos.value = await prontuarioService.listarAnexos(pacienteId.value)
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
            erro.value = "Preencha ao menos uma seção antes de salvar."
            return
        }
        const modeloOverride = modeloConsultaAtual.value !== pront.value?.prontuario.modeloDeProntuarioId
            ? (modeloConsultaAtual.value ?? undefined)
            : undefined
        await prontuarioService.registrarEvolucao(pacienteId.value, conteudoNaoVazio, modeloOverride)
        inicializarFormEvolucao()
        await carregar()
        abaAtiva.value = "anteriores"
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar evolução."
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
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao enviar anexo."
    } finally {
        uploadando.value = false
    }
}

async function baixarAnexo(anexo: Anexo) {
    try {
        const { url } = await prontuarioService.obterUrlAnexo(pacienteId.value, anexo.id)
        window.open(url, "_blank")
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao gerar link do anexo."
    }
}

function voltar() {
    router.push({ name: "PacienteDetalhe", params: { id: pacienteId.value } })
}

// ─── IA — sugestão de seção ───────────────────────────────────────────────────
async function gerarSugestaoIa(secaoChave: string, secaoTitulo: string) {
    gerandoIa.value[secaoChave] = true
    erroIa.value[secaoChave] = null
    novaEvolucao[secaoChave] = ""
    const contexto: Record<string, string> = {}
    for (const s of secoesConsultaAtual.value) {
        if (s.chave !== secaoChave && novaEvolucao[s.chave]?.trim()) {
            contexto[s.titulo] = novaEvolucao[s.chave]
        }
    }
    try {
        for await (const chunk of sugerirSecaoProntuario({
            secaoAlvoTitulo: secaoTitulo,
            secoesContexto: contexto,
        })) {
            novaEvolucao[secaoChave] += chunk
        }
    } catch (e: any) {
        erroIa.value[secaoChave] = e?.message ?? "Erro ao gerar sugestão."
        novaEvolucao[secaoChave] = ""
    } finally {
        gerandoIa.value[secaoChave] = false
    }
}

async function gerarResumoIa() {
    resumoIaGerando.value = true
    resumoIa.value = ""
    try {
        const contexto: Record<string, string> = {}
        for (const s of secoesConsultaAtual.value) {
            if (novaEvolucao[s.chave]?.trim()) {
                contexto[s.titulo] = novaEvolucao[s.chave]
            }
        }
        if (Object.keys(contexto).length === 0) {
            resumoIa.value = "Preencha alguma seção da consulta atual para que o assistente gere um resumo."
            return
        }
        for await (const chunk of sugerirSecaoProntuario({
            secaoAlvoTitulo: "Resumo clínico e insights",
            secoesContexto: contexto,
        })) {
            resumoIa.value += chunk
        }
    } catch (e: any) {
        resumoIa.value = `Erro: ${e?.message ?? "falha ao gerar resumo."}`
    } finally {
        resumoIaGerando.value = false
    }
}
</script>

<template>
    <main class="app-page app-page--wide prontuario">
        <!-- Cabeçalho da página -->
        <header class="page-header">
            <div class="page-header-texto">
                <h1 class="page-titulo">Prontuário</h1>
                <p class="page-sub">Registre evoluções e acompanhe o histórico clínico do paciente.</p>
            </div>
            <AppButton variant="ghost" icon="fa-solid fa-arrow-left" @click="voltar">Voltar</AppButton>
        </header>

        <!-- Cartão com dados do paciente -->
        <ProntuarioPacienteHeader
            :paciente="paciente"
            :agendamento="agendamento"
            :estabelecimento="tenant.ativo?.nomeFantasia ?? null"
        />

        <p v-if="carregando" class="estado-msg">Carregando prontuário...</p>
        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <!-- Prontuário não iniciado: escolher modelo -->
        <section v-if="!carregando && !pront" class="iniciar-card">
            <h2 class="secao-titulo">Iniciar prontuário</h2>
            <p class="secao-sub">Escolha um modelo para estruturar o prontuário deste paciente.</p>
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

        <!-- Prontuário iniciado: abas -->
        <template v-if="pront">
            <ProntuarioTabs
                v-model="abaAtiva"
                :contagem-anteriores="pront.evolucoes.length"
            />

            <!-- TAB: Consulta atual -->
            <ConsultaAtualTab
                v-if="abaAtiva === 'consulta' && modeloConsultaAtual !== null"
                v-model:modelo-id="modeloConsultaAtual"
                :modelos="modelosDisponiveis"
                :secoes="secoesConsultaAtual"
                :nova-evolucao="novaEvolucao"
                :salvando="salvandoEvolucao"
                :gerando-ia="gerandoIa"
                :erro-ia="erroIa"
                @salvar="salvarEvolucao"
                @gerar-ia="(chave, titulo) => gerarSugestaoIa(chave, titulo)"
            />

            <!-- TAB: Consultas anteriores -->
            <ConsultasAnterioresTab
                v-else-if="abaAtiva === 'anteriores'"
                :evolucoes="pront.evolucoes"
                :anexos="anexos"
                :uploadando="uploadando"
                :gerar-pdf="() => gerarPdfProntuario(pront!, paciente?.nomeCompleto ?? 'paciente')"
                @download-anexo="baixarAnexo"
                @selecionar-arquivo="selecionarArquivo"
                @enviar-anexo="enviarAnexo"
            />

            <!-- TAB: Exame físico -->
            <ExameFisicoTab
                v-else-if="abaAtiva === 'exame'"
                :nova-evolucao="novaEvolucao"
                :salvando="salvandoEvolucao"
                @salvar="salvarEvolucao"
            />

            <!-- TAB: Receitas -->
            <ReceitasTab
                v-else-if="abaAtiva === 'receitas' && paciente"
                :paciente-id="pacienteId"
                :paciente-nome="paciente.nomeCompleto"
            />

            <!-- TAB: Assistente -->
            <AssistenteTab
                v-else-if="abaAtiva === 'assistente'"
                :resumo-ia="resumoIa"
                :gerando-resumo="resumoIaGerando"
                :nova-evolucao="novaEvolucao"
                @gerar-resumo="gerarResumoIa"
            />
        </template>
    </main>
</template>

<style scoped>
/* Cabeçalho */
.page-header {
    display: flex; justify-content: space-between; align-items: flex-start;
    gap: 1rem; margin-bottom: 1rem; flex-wrap: wrap;
}
.page-header-texto { display: flex; flex-direction: column; gap: 0.15rem; }
.page-titulo { font-size: 1.5rem; font-weight: 800; margin: 0; color: hsl(var(--primary-dark)); }
.page-sub    { margin: 0; color: var(--text-muted); font-size: 0.875em; max-width: 680px; }

/* Iniciar prontuário */
.iniciar-card {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 2rem; max-width: 500px; margin: 2rem auto;
    display: flex; flex-direction: column; gap: 1rem;
}
.secao-titulo { font-size: 1em; font-weight: 700; margin: 0; }
.secao-sub    { font-size: 0.85em; color: var(--text-muted); margin: 0; }

/* Estados */
.estado-msg { text-align: center; color: var(--text-muted); padding: 2rem 1rem; font-size: 0.9em; }
.msg-erro   { color: hsl(var(--error)); font-size: 0.875em; margin: 0 0 1rem; }
</style>
