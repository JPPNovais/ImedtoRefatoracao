<script setup lang="ts">
/**
 * Prontuário do paciente — visual care/prontuário do design Imedto.
 *
 * Estrutura:
 *   - Sticky header (ProntuarioPacienteHeader): paciente + alertas + ações
 *     (modo foco, imprimir, receita, finalizar e assinar) e timer ao vivo.
 *   - 5 abas: Consulta atual / Consultas anteriores / Receitas / Atestado /
 *     Pedidos de exame. A "Consulta atual" usa layout 3 colunas com módulos.
 *     A antiga aba "Exame físico" foi absorvida pela seção de mesmo nome
 *     dentro de Consulta atual — mapa corporal + regiões anatômicas convivem
 *     com sinais vitais/antropometria/ectoscopia no mesmo lugar (PO 2026-05-18).
 *   - "Finalizar e assinar" chama POST /agendamentos/:id/concluir e limpa a
 *     marca local de atendimento ativo.
 */
import { computed, onMounted, reactive, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import {
    prontuarioService,
    type Anexo,
    type Evolucao,
    type ModeloProntuario,
    type ProntuarioCompleto,
} from "@/services/prontuarioService"
import { pacienteService, type Paciente } from "@/services/pacienteService"
import { agendaService, type Agendamento } from "@/services/agendaService"
import { useProntuarioPdf, type PdfSaidaModo } from "@/composables/useProntuarioPdf"
import { useTenantStore } from "@/stores/tenantStore"
import { useAtendimentoAtivo } from "@/composables/useAtendimentoAtivo"
import { AppButton, AppField, AppSelect, AppToast } from "@/components/ui"
import ProntuarioPacienteHeader    from "@/components/prontuario/ProntuarioPacienteHeader.vue"
import ProntuarioTabs, { type AbaProntuario } from "@/components/prontuario/ProntuarioTabs.vue"
import ConsultaAtualTab            from "@/components/prontuario/tabs/ConsultaAtualTab.vue"
import ConsultasAnterioresTab      from "@/components/prontuario/tabs/ConsultasAnterioresTab.vue"
import ReceitasTab                 from "@/components/prontuario/tabs/ReceitasTab.vue"
import AtestadoTab                 from "@/components/prontuario/tabs/AtestadoTab.vue"
import PedidoExameTab              from "@/components/prontuario/tabs/PedidoExameTab.vue"
import { exameFisicoService }      from "@/services/exameFisicoService"
import { useProximosPassosStore }  from "@/stores/proximosPassosStore"
import { type AcaoPendencia }     from "@/services/pendenciaService"

const { gerarPdf: gerarPdfProntuario, gerarPdfEvolucao } = useProntuarioPdf()

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

// Total de evoluções — vem do endpoint paginado ao abrir "Consultas anteriores"
// (ou do contagem-evolucoes na carga inicial), em vez de pront.evolucoes.length
// (que era limitado a timeline=50 e não escalava).
const totalEvolucoes = ref(0)

// Toast simples
const toast = ref<{ msg: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(msg: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { msg, variante }
}

// Widget "Próximos passos" — estado global em Pinia store (addendum 2, R24/R25).
// A montagem do componente foi movida para AppLayout.vue; esta view apenas dispara a store.
const proximosPassosStore = useProximosPassosStore()

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

// PDF individual de evolução — id em geração (pra mostrar spinner só no card certo).
const evolucaoSendoBaixada = ref<number | null>(null)

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
            totalEvolucoes.value = pront.value.evolucoes.length
            inicializarFormEvolucao()
            // Carrega o total real (sem ficar preso ao cap timeline=50 do payload inicial).
            prontuarioService.contarEvolucoes(pacienteId.value)
                .then(n => { totalEvolucoes.value = n })
                .catch(() => { /* mantém o cap como aproximação */ })
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

// Mapeia lateralidade do modelo local p/ backend (mesmo mapeamento que vivia em ExameFisicoTab).
const LATERALIDADE_LOCAL_PARA_BACKEND: Record<string, string | null> = {
    D: "Direita", E: "Esquerda", bilateral: "Bilateral",
    // "misto" = lados diferentes nas sub-partes — backend não tem este conceito;
    // envia null para o campo estruturado (detalhe fica no texto do card, R8).
    misto: null,
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

        const { evolucaoId } = await prontuarioService.registrarEvolucao(
            pacienteId.value, conteudoNaoVazio, modeloOverride, eventoId.value,
        )

        // Se o usuário marcou regiões anatômicas no mapa corporal dentro da
        // seção Exame físico, registra-as no domínio dedicado de exame físico.
        // Falha aqui não invalida a evolução já salva — apenas avisa.
        const ef = novaEvolucao["exame-fisico"] as
            | { regioes?: Array<{ regiao_id: string; lateralidade: string | null; achados: string }>; observacoesExame?: string }
            | undefined
        const regioes = ef?.regioes ?? []
        if (evolucaoId && regioes.length > 0) {
            try {
                await exameFisicoService.registrar(evolucaoId, {
                    observacoesGerais: ef?.observacoesExame || undefined,
                    regioes: regioes.map((r, idx) => ({
                        codigo: r.regiao_id,
                        lateralidade: r.lateralidade
                            ? (LATERALIDADE_LOCAL_PARA_BACKEND[r.lateralidade] ?? null)
                            : null,
                        achados: r.achados || undefined,
                        severidade: "Normal",
                        ordem: idx,
                    })),
                })
            } catch (e: any) {
                // Evolução foi salva mas regiões falharam — não regredir o salvar.
                notificar(e?.response?.data?.mensagem ?? "Evolução salva, mas regiões anatômicas falharam.", "info")
            }
        }

        // Extrai ações marcadas da conduta para disparar o widget global (CA70/R25).
        // Só dispara se houver ≥1 ação marcada.
        const conduta = conteudoNaoVazio["conduta"]
        if (conduta && typeof conduta === "object" && "acoesMarcadas" in conduta) {
            const acoes = (conduta as { acoesMarcadas?: AcaoPendencia[] }).acoesMarcadas ?? []
            if (acoes.length > 0) {
                // F5/R1: evolucaoId para pré-preenchimento do form de orçamento (CA97/CA195).
                void proximosPassosStore.iniciar({
                    pacienteId: pacienteId.value,
                    evolucaoId: evolucaoId ?? undefined,
                    acoesMarcadas: acoes,
                })
            }
        }

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

/**
 * Para o modo "visualizar", precisamos chamar `window.open` SINCRONICAMENTE
 * ao clique do usuário — qualquer `await` antes dispara popup blocker em
 * Chrome/Safari. Abrimos `about:blank` aqui mesmo e devolvemos a referência;
 * depois do PDF gerado, redirecionamos a janela para o blob URL. Se retornar
 * null (popup bloqueado), avisamos o usuário e caímos para download.
 *
 * NÃO usar "noopener,noreferrer": no Chrome 88+ a janela retorna handle não
 * nulo mas ignora silenciosamente `janela.location.href = blobUrl` posterior,
 * deixando a aba travada em about:blank. O blob é same-origin (sem risco de
 * tabnabbing) e browsers modernos já aplicam noopener implícito em cross-origin.
 */
function abrirJanelaParaVisualizacao(): Window | null {
    return window.open("about:blank", "_blank")
}

async function exportarHistorico(modo: PdfSaidaModo) {
    if (!pront.value) {
        notificar("Inicie o prontuário antes de exportar.", "error")
        return
    }
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
        // Audit LGPD: registra a exportação antes de gerar o PDF.
        // Se falhar (422/permissão), o doc não é produzido.
        await prontuarioService.registrarExportacaoHistorico(pacienteId.value)
        const { blobUrl } = await gerarPdfProntuario(pront.value, paciente.value ?? "paciente", modoEfetivo)
        if (modoEfetivo === "visualizar" && janela && blobUrl) {
            janela.location.href = blobUrl
        }
    } catch (e: any) {
        if (janela) janela.close()
        notificar(e?.response?.data?.mensagem ?? "Erro ao exportar prontuário.", "error")
    }
}

async function exportarPdfEvolucao(evolucao: Evolucao, modo: PdfSaidaModo = "download") {
    if (!pront.value) return
    if (evolucaoSendoBaixada.value !== null) return
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
        await prontuarioService.registrarExportacaoEvolucao(pacienteId.value, evolucao.id)
        const { blobUrl } = await gerarPdfEvolucao(pront.value, evolucao, paciente.value ?? "paciente", modoEfetivo)
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

</script>

<template>
    <main class="app-page app-page--wide pront-shell">
        <!-- Sticky header do paciente -->
        <ProntuarioPacienteHeader
            :paciente="paciente"
            :agendamento="agendamento"
            :estabelecimento="tenant.ativo?.nomeFantasia ?? null"
            :sem-acoes="!pront"
            @voltar="voltar"
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
                v-model="abaAtiva"
                :contagem-anteriores="totalEvolucoes"
            />

            <ConsultaAtualTab
                v-if="abaAtiva === 'consulta' && modeloConsultaAtual !== null"
                v-model:modelo-id="modeloConsultaAtual"
                :modelos="modelosDisponiveis"
                :secoes="secoesConsultaAtual"
                :nova-evolucao="novaEvolucao"
                :salvando="salvandoEvolucao"
                :paciente-sexo="paciente?.genero ?? null"
                @salvar="salvarEvolucao"
            />

            <ConsultasAnterioresTab
                v-else-if="abaAtiva === 'anteriores'"
                :paciente-id="pacienteId"
                :anexos="anexos"
                :uploadando="uploadando"
                :evolucao-sendo-baixada="evolucaoSendoBaixada"
                :gerar-historico="exportarHistorico"
                @download-anexo="baixarAnexo"
                @selecionar-arquivo="selecionarArquivo"
                @enviar-anexo="enviarAnexo"
                @gerar-pdf-evolucao="exportarPdfEvolucao($event.evolucao, $event.modo)"
                @total-atualizado="totalEvolucoes = $event"
            />

            <ReceitasTab
                v-else-if="abaAtiva === 'receitas' && paciente"
                :paciente-id="pacienteId"
                :paciente-nome="paciente.nomeCompleto"
            />

            <AtestadoTab
                v-else-if="abaAtiva === 'atestado' && paciente"
                :paciente-id="pacienteId"
                :paciente-nome="paciente.nomeCompleto"
            />

            <PedidoExameTab
                v-else-if="abaAtiva === 'pedidos-exame' && paciente"
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

        <!-- Widget "Próximos passos" movido para AppLayout.vue (addendum 2, R25/CA202). -->
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
