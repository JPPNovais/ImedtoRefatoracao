<script setup lang="ts">
/**
 * MigracaoRevisaoView — painel de revisão de mapeamento de migração.
 * Admin revisa o de-para gerado pela IA, edita e salva. Pode salvar como template.
 * Addendum 4 (CA77-79): classificação por bloco + seletor de entidade + warnings.
 */
import { computed, onMounted, onUnmounted, ref, watch } from "vue"
import { useRouter } from "vue-router"
import {
    AppPageHeader,
    AppCard,
    AppButton,
    AppBadge,
    AppModal,
    AppField,
    AppInput,
} from "@/components/ui"
import { useMigracaoAdminStore } from "../stores/migracaoAdminStore"
import {
    ENTIDADES_CANONICAS,
    type MigracaoMapaDto,
    type MigracaoEventoDto,
} from "../services/migracaoAdminService"

const props = defineProps<{ jobId: string }>()

const router = useRouter()
const store  = useMigracaoAdminStore()

// Chave única por mapa: "entidade|nomeBlocoOrigem" para distinguir blocos do mesmo dump.
function chaveBloco(mapa: MigracaoMapaDto): string {
    return `${mapa.entidade}|${mapa.nomeBlocoOrigem ?? ""}`
}

// Estado local de edição — um dict por chaveBloco → { col_origem: campo_canonico }
const edicoes = ref<Record<string, Record<string, string>>>({})
const salvando = ref<Record<string, boolean>>({})
const erros = ref<Record<string, string>>({})

// Addendum 4 — reclassificação e ignorar por bloco (CA77/CA78)
const entidadeReclassificada = ref<Record<string, string>>({})
const ignoradoBloco = ref<Record<string, boolean>>({})

// Marco 3 — preview + disparo + relatório
const erroPreview = ref("")
const gerandoPreview = ref(false)
const erroDisparar = ref("")
const erroRelatorio = ref("")
const carregandoRelatorio = ref(false)

// Marco 4 — desfazer (CA17, R9)
const modalDesfazer = ref(false)
const erroDesfazer = ref("")

// Addendum 002 — reprocessar (CA30)
const erroReprocessar = ref("")

// Addendum 003 — aprovar análise (CA41)
const erroAprovar = ref("")

// Modal de template
const modalTemplate = ref(false)
const nomeTemplate  = ref("")
const erroTemplate  = ref("")
const salvandoTemplate = ref(false)

onMounted(async () => {
    await store.carregarJob(Number(props.jobId))
    inicializarEdicoes()
    await store.carregarEventos(Number(props.jobId))
    if (STATUS_ATIVOS.has(statusJob.value)) {
        await store.carregarProgresso(Number(props.jobId))
    }
    iniciarPolling()
})

function inicializarEdicoes() {
    if (!store.jobAtual) return
    for (const mapa of store.jobAtual.mapas) {
        const chave = chaveBloco(mapa)
        try {
            const parsed = parsedMapa(mapa.mapaJson)
            edicoes.value[chave] = { ...(parsed.de_para ?? {}) }

            // Addendum 4 — pré-seleciona entidade e flag ignorado (CA78)
            if (parsed.entidade_classificada) {
                entidadeReclassificada.value[chave] = parsed.entidade_operador ?? parsed.entidade_classificada
            }
            ignoradoBloco.value[chave] = parsed.ignorado ?? parsed.entidade_classificada === "sem_equivalente"
        } catch {
            edicoes.value[chave] = {}
        }
    }
}

function parsedMapa(mapaJson: string) {
    try {
        return JSON.parse(mapaJson) as {
            de_para: Record<string, string>
            confianca: number
            duvidas: string[]
            entidade_classificada?: string
            entidade_operador?: string
            confianca_classificacao?: number
            ignorado?: boolean
            encoding_suspeito?: boolean
            eh_config?: boolean
        }
    } catch {
        return {
            de_para: {} as Record<string, string>,
            confianca: 0,
            duvidas: [] as string[],
        }
    }
}

function confiancaVariant(c: number): "success" | "warning" | "error" {
    if (c >= 0.8) return "success"
    if (c >= 0.6) return "warning"
    return "error"
}

function isDuvida(mapa: MigracaoMapaDto, col: string): boolean {
    return parsedMapa(mapa.mapaJson).duvidas?.includes(col) ?? false
}

async function salvarMapa(mapa: MigracaoMapaDto) {
    const chave = chaveBloco(mapa)
    salvando.value[chave] = true
    erros.value[chave] = ""
    try {
        const entReclassificada = entidadeReclassificada.value[chave] ?? null
        await store.salvarMapa(
            Number(props.jobId),
            mapa.entidade,
            edicoes.value[chave] ?? {},
            mapa.nomeBlocoOrigem || undefined,
            entReclassificada,
            ignoradoBloco.value[chave] ?? false,
        )
    } catch {
        erros.value[chave] = "Não foi possível salvar o mapa."
    } finally {
        salvando.value[chave] = false
    }
}

async function confirmarTemplate() {
    if (!nomeTemplate.value.trim()) {
        erroTemplate.value = "Nome é obrigatório."
        return
    }
    salvandoTemplate.value = true
    erroTemplate.value = ""
    try {
        await store.salvarTemplate(Number(props.jobId), nomeTemplate.value.trim())
        modalTemplate.value = false
        nomeTemplate.value = ""
    } catch {
        erroTemplate.value = "Não foi possível salvar o template."
    } finally {
        salvandoTemplate.value = false
    }
}

const temMapas = computed(() => (store.jobAtual?.mapas?.length ?? 0) > 0)

const statusJob = computed(() => store.jobAtual?.status ?? "")

// ─── Bloco A — Stepper ────────────────────────────────────────────────────────

const PASSOS_ORDEM = [
    { status: "aguardando_arquivo", label: "Arquivo enviado" },
    { status: "aguardando_aprovacao", label: "Aguardando aprovação" },
    { status: "aguardando_mapa", label: "Análise por IA" },
    { status: "mapa_em_revisao", label: "Revisão do mapa" },
    { status: "preview_pronto", label: "Preview gerado" },
    { status: "migrando", label: "Migrando" },
    { status: "concluido", label: "Concluído" },
] as const

const TERMINAIS_ALTERNATIVOS: Record<string, string> = {
    falhou: "Falhou",
    rejeitado: "Rejeitado",
    desfeito: "Desfeito",
    concluido_com_erros: "Concluído c/ erros",
}

const stepperPassos = computed(() => {
    const status = statusJob.value
    const idxAtual = PASSOS_ORDEM.findIndex(p => p.status === status)

    if (idxAtual === -1 && TERMINAIS_ALTERNATIVOS[status]) {
        return [
            ...PASSOS_ORDEM.map(p => ({
                label: p.label,
                estado: "concluido" as const,
            })),
            { label: TERMINAIS_ALTERNATIVOS[status], estado: "erro" as const },
        ]
    }

    return PASSOS_ORDEM.map((p, i) => ({
        label: p.label,
        estado: i < idxAtual ? "concluido" as const
               : i === idxAtual ? "atual" as const
               : "pendente" as const,
    }))
})

// ─── Bloco B — Eventos ────────────────────────────────────────────────────────

const STATUS_LABELS_EVENTO: Record<string, string> = {
    aguardando_arquivo: "Arquivo aguardando",
    aguardando_aprovacao: "Aguardando aprovação",
    aguardando_mapa: "Liberado para IA",
    mapa_em_revisao: "Mapa gerado",
    preview_pronto: "Preview aprovado",
    migrando: "Carga iniciada",
    concluido: "Concluído",
    concluido_com_erros: "Concluído c/ erros",
    desfeito: "Desfeito",
    rejeitado: "Rejeitado",
    falhou: "Falhou",
}

function labelEvento(evt: MigracaoEventoDto): string {
    const ator = evt.usuarioId ? "(admin)" : "(sistema)"
    const label = STATUS_LABELS_EVENTO[evt.statusNovo] ?? evt.statusNovo
    const data = new Date(evt.criadoEm).toLocaleString("pt-BR", {
        day: "2-digit", month: "2-digit",
        hour: "2-digit", minute: "2-digit",
    })
    return `${label} ${ator} • ${data}`
}

// ─── Bloco D — Polling ───────────────────────────────────────────────────────

const STATUS_ATIVOS = new Set(["aguardando_mapa", "mapa_em_revisao", "migrando"])
let pollingTimer: ReturnType<typeof setTimeout> | null = null

function pararPolling() {
    if (pollingTimer !== null) {
        clearTimeout(pollingTimer)
        pollingTimer = null
    }
}

function iniciarPolling() {
    pararPolling()
    if (!STATUS_ATIVOS.has(statusJob.value)) return
    pollingTimer = setTimeout(async () => {
        await store.carregarJob(Number(props.jobId))
        if (STATUS_ATIVOS.has(statusJob.value)) {
            await store.carregarProgresso(Number(props.jobId))
        }
        iniciarPolling()
    }, 4000)
}

watch(statusJob, (novo) => {
    if (!STATUS_ATIVOS.has(novo)) {
        pararPolling()
    }
})

onUnmounted(() => {
    pararPolling()
})

async function gerarPreview() {
    gerandoPreview.value = true
    erroPreview.value = ""
    try {
        await store.gerarPreview(Number(props.jobId))
    } catch {
        erroPreview.value = "Não foi possível gerar o preview."
    } finally {
        gerandoPreview.value = false
    }
}

async function disparar() {
    erroDisparar.value = ""
    try {
        await store.disparar(Number(props.jobId))
    } catch {
        erroDisparar.value = "Não foi possível disparar a carga."
    }
}

async function verRelatorio() {
    carregandoRelatorio.value = true
    erroRelatorio.value = ""
    try {
        await store.carregarRelatorio(Number(props.jobId))
    } catch {
        erroRelatorio.value = "Não foi possível carregar o relatório."
    } finally {
        carregandoRelatorio.value = false
    }
}

async function confirmarDesfazer() {
    erroDesfazer.value = ""
    try {
        await store.desfazer(Number(props.jobId))
        modalDesfazer.value = false
    } catch {
        erroDesfazer.value = "Não foi possível desfazer a migração."
    }
}

/** CA30 — Reprocessa job em falhou, restaurando o status anterior para reprocessamento automático. */
async function reprocessar() {
    erroReprocessar.value = ""
    try {
        await store.reprocessar(Number(props.jobId))
    } catch {
        erroReprocessar.value = "Não foi possível reprocessar o job."
    }
}

/** CA41 — Aprova a análise por IA do job em aguardando_aprovacao → aguardando_mapa. */
async function aprovarAnalise() {
    erroAprovar.value = ""
    try {
        await store.aprovarAnalise(Number(props.jobId))
    } catch {
        erroAprovar.value = "Não foi possível aprovar a análise."
    }
}
</script>

<template>
    <div class="app-page">
        <AppPageHeader
            :titulo="`Revisão de Mapa — Job #${props.jobId}`"
            :subtitulo="store.jobAtual ? `Estabelecimento ${store.jobAtual.estabelecimentoId} · ${store.jobAtual.origem ?? 'sem origem'}` : ''"
        >
            <template #acoes>
                <AppButton
                    v-if="temMapas"
                    variant="secondary"
                    @click="modalTemplate = true"
                >
                    <i class="fa-solid fa-floppy-disk" aria-hidden="true" />
                    Salvar como template
                </AppButton>
                <AppButton variant="ghost" @click="router.back()">
                    Voltar
                </AppButton>
            </template>
        </AppPageHeader>

        <!-- Carregando -->
        <div v-if="store.carregando" class="loading-msg">Carregando...</div>

        <!-- Erro -->
        <div v-else-if="store.erro" class="erro-msg">{{ store.erro }}</div>

        <!-- Bloco A — Stepper de status (CA51/CA52) -->
        <div v-else-if="store.jobAtual" class="stepper" role="list" aria-label="Progresso da migração">
            <div
                v-for="(passo, idx) in stepperPassos"
                :key="idx"
                class="stepper-item"
                :class="`stepper-item--${passo.estado}`"
                role="listitem"
            >
                <div class="stepper-indicador">
                    <i v-if="passo.estado === 'concluido'" class="fa-solid fa-check" aria-hidden="true" />
                    <i v-else-if="passo.estado === 'erro'" class="fa-solid fa-xmark" aria-hidden="true" />
                    <span v-else>{{ idx + 1 }}</span>
                </div>
                <span class="stepper-label">{{ passo.label }}</span>
                <div v-if="idx < stepperPassos.length - 1" class="stepper-linha" aria-hidden="true" />
            </div>
        </div>

        <!-- Sem mapas ainda -->
        <AppCard v-if="store.jobAtual && !temMapas" class="sem-mapas">
            <p class="muted">Nenhum mapa gerado ainda. Aguarde a inferência da IA.</p>
        </AppCard>

        <!-- Mapas por entidade/bloco (addendum 4: cada bloco do dump é um card) -->
        <template v-if="temMapas">
            <!-- CA18 — Banner de template pré-preenchido -->
            <div v-if="store.jobAtual?.nomeTemplate" class="banner-template" role="status">
                <i class="fa-solid fa-circle-info" aria-hidden="true" />
                Mapeamento pré-preenchido pelo template <strong>{{ store.jobAtual.nomeTemplate }}</strong> — revise antes de confirmar.
            </div>
            <AppCard
                v-for="mapa in store.jobAtual!.mapas"
                :key="chaveBloco(mapa)"
                class="mapa-card"
            >
                <div class="mapa-header">
                    <!-- Título: bloco de origem (dump) ou entidade (CSV/JSON-array) -->
                    <h2 class="ds-section-title">
                        {{ mapa.nomeBlocoOrigem || mapa.entidade }}
                    </h2>
                    <div class="mapa-badges">
                        <!-- Badge de confiança do de-para -->
                        <AppBadge :variant="confiancaVariant(parsedMapa(mapa.mapaJson).confianca)">
                            Mapeamento {{ Math.round(parsedMapa(mapa.mapaJson).confianca * 100) }}%
                        </AppBadge>
                        <AppBadge v-if="mapa.revisadoEm" variant="success">
                            Revisado
                        </AppBadge>
                        <!-- Addendum 4 — badge de config (CA70) -->
                        <AppBadge v-if="parsedMapa(mapa.mapaJson).eh_config" variant="muted">
                            Configuração (não migrável)
                        </AppBadge>
                    </div>
                </div>

                <!-- Addendum 4 — Alerta encoding suspeito (CA81/D-E1) -->
                <div
                    v-if="parsedMapa(mapa.mapaJson).encoding_suspeito"
                    class="bloco-alerta bloco-alerta--warning"
                    role="alert"
                >
                    <i class="fa-solid fa-triangle-exclamation" aria-hidden="true" />
                    Encoding suspeito detectado neste bloco. Verifique os valores antes de confirmar.
                </div>

                <!-- Addendum 4 — Bloco de classificação de entidade por IA (CA77/CA78) -->
                <div
                    v-if="parsedMapa(mapa.mapaJson).entidade_classificada && !parsedMapa(mapa.mapaJson).eh_config"
                    class="bloco-classificacao"
                >
                    <div class="classificacao-linha">
                        <span class="classificacao-label">Entidade identificada pela IA:</span>
                        <AppBadge
                            :variant="confiancaVariant(parsedMapa(mapa.mapaJson).confianca_classificacao ?? 0)"
                            class="classificacao-badge"
                        >
                            {{ parsedMapa(mapa.mapaJson).entidade_classificada }}
                            <span v-if="parsedMapa(mapa.mapaJson).confianca_classificacao != null">
                                ({{ Math.round((parsedMapa(mapa.mapaJson).confianca_classificacao ?? 0) * 100) }}%)
                            </span>
                        </AppBadge>
                    </div>

                    <!-- Addendum 4 — Aviso sem_equivalente (CA78): bloco ignorado por padrão -->
                    <div
                        v-if="parsedMapa(mapa.mapaJson).entidade_classificada === 'sem_equivalente'"
                        class="bloco-alerta bloco-alerta--info"
                        role="status"
                    >
                        <i class="fa-solid fa-circle-info" aria-hidden="true" />
                        Nenhuma entidade equivalente foi identificada. Este bloco será ignorado por padrão — reclassifique se necessário.
                    </div>

                    <!-- Seletor de reclassificação (CA77) -->
                    <div class="classificacao-reclassificar">
                        <AppField label="Reclassificar como">
                            <select
                                v-model="entidadeReclassificada[chaveBloco(mapa)]"
                                class="form-input classificacao-select"
                                :aria-label="`Reclassificar bloco ${mapa.nomeBlocoOrigem || mapa.entidade}`"
                            >
                                <option value="">— Manter classificação da IA —</option>
                                <option
                                    v-for="ent in ENTIDADES_CANONICAS"
                                    :key="ent"
                                    :value="ent"
                                >
                                    {{ ent }}
                                </option>
                            </select>
                        </AppField>
                        <label class="ignorar-label">
                            <input
                                type="checkbox"
                                v-model="ignoradoBloco[chaveBloco(mapa)]"
                                class="ignorar-checkbox"
                            />
                            Ignorar este bloco (não migrar)
                        </label>
                    </div>
                </div>

                <!-- Tabela de-para editável (oculta se bloco é config ou ignorado) -->
                <template v-if="!parsedMapa(mapa.mapaJson).eh_config && !ignoradoBloco[chaveBloco(mapa)]">
                    <table class="depara-table">
                        <thead>
                            <tr>
                                <th>Coluna de origem</th>
                                <th>Campo canônico</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr
                                v-for="(campo, col) in parsedMapa(mapa.mapaJson).de_para"
                                :key="col"
                                :class="{ 'row-duvida': isDuvida(mapa, String(col)) }"
                            >
                                <td>
                                    <span class="col-origem">{{ col }}</span>
                                    <i
                                        v-if="isDuvida(mapa, String(col))"
                                        class="fa-solid fa-triangle-exclamation duvida-icon"
                                        title="Coluna com baixa confiança — revise"
                                        aria-hidden="true"
                                    />
                                </td>
                                <td>
                                    <AppInput
                                        v-if="edicoes[chaveBloco(mapa)]"
                                        v-model="edicoes[chaveBloco(mapa)][String(col)]"
                                        placeholder="campo_canonico"
                                    />
                                    <span v-else>{{ campo }}</span>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </template>

                <!-- Bloco de config: apenas informativo, sem tabela editável -->
                <p v-if="parsedMapa(mapa.mapaJson).eh_config" class="muted">
                    Este bloco contém dados de configuração do sistema de origem e não será migrado.
                </p>

                <div v-if="erros[chaveBloco(mapa)]" class="erro-msg">{{ erros[chaveBloco(mapa)] }}</div>

                <!-- Botão Salvar: omitido para blocos de config -->
                <div v-if="!parsedMapa(mapa.mapaJson).eh_config" class="mapa-acoes">
                    <AppButton
                        variant="primary"
                        :loading="salvando[chaveBloco(mapa)]"
                        @click="salvarMapa(mapa)"
                    >
                        Salvar mapa revisado
                    </AppButton>
                </div>
            </AppCard>
        </template>

        <!-- Addendum 003 — Gate de aprovação humana (status: aguardando_aprovacao) — CA41/CA43/CA45 -->
        <AppCard
            v-if="statusJob === 'aguardando_aprovacao'"
            class="marco3-card"
        >
            <h2 class="ds-section-title">Arquivo recebido — aguardando aprovação</h2>
            <AppBadge variant="warning">Aguardando aprovação</AppBadge>
            <p class="muted">
                O arquivo foi enviado pelo cliente. Aprove para liberar a análise por IA — a inferência do mapa de migração só inicia após aprovação explícita.
            </p>
            <div v-if="erroAprovar" class="erro-msg">{{ erroAprovar }}</div>
            <div class="marco3-acoes">
                <AppButton
                    variant="primary"
                    :loading="store.aprovando"
                    @click="aprovarAnalise"
                >
                    <i class="fa-solid fa-check" aria-hidden="true" />
                    Aprovar análise
                </AppButton>
            </div>
        </AppCard>

        <!-- Marco 3 — Painel de preview (status: mapa_em_revisao) -->
        <AppCard
            v-if="statusJob === 'mapa_em_revisao'"
            class="marco3-card"
        >
            <h2 class="ds-section-title">Gerar preview da carga</h2>
            <p class="muted">Confirme os mapas revisados e gere o preview para ver quantos registros serão importados por entidade.</p>
            <div v-if="store.preview" class="preview-resultado">
                <p class="preview-total">Total de registros: <strong>{{ store.preview.totalRegistros }}</strong></p>
                <table class="relatorio-table">
                    <thead>
                        <tr>
                            <th>Entidade</th>
                            <th>Pendentes</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="(ent, nome) in store.preview.porEntidade" :key="nome">
                            <td>{{ nome }}</td>
                            <td>{{ ent.pendentes }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div v-if="erroPreview" class="erro-msg">{{ erroPreview }}</div>
            <div class="marco3-acoes">
                <AppButton
                    variant="primary"
                    :loading="gerandoPreview"
                    @click="gerarPreview"
                >
                    Gerar preview
                </AppButton>
            </div>
        </AppCard>

        <!-- Marco 3 — Painel de disparo (status: preview_pronto) -->
        <AppCard
            v-if="statusJob === 'preview_pronto'"
            class="marco3-card"
        >
            <h2 class="ds-section-title">Disparar carga</h2>
            <p class="muted">O preview foi gerado. Confirme para iniciar a importação em background.</p>
            <div v-if="store.preview" class="preview-resultado">
                <p class="preview-total">Total de registros: <strong>{{ store.preview.totalRegistros }}</strong></p>
                <table class="relatorio-table">
                    <thead>
                        <tr>
                            <th>Entidade</th>
                            <th>Pendentes</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="(ent, nome) in store.preview.porEntidade" :key="nome">
                            <td>{{ nome }}</td>
                            <td>{{ ent.pendentes }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div v-if="erroDisparar" class="erro-msg">{{ erroDisparar }}</div>
            <div class="marco3-acoes">
                <AppButton
                    variant="primary"
                    :loading="store.disparando"
                    @click="disparar"
                >
                    <i class="fa-solid fa-play" aria-hidden="true" />
                    Disparar carga
                </AppButton>
            </div>
        </AppCard>

        <!-- Bloco C + D — Painel de progresso (status: migrando) com polling -->
        <AppCard v-if="statusJob === 'migrando'" class="marco3-card">
            <h2 class="ds-section-title">Carga em andamento</h2>
            <p class="muted">A importação está sendo processada em background.</p>

            <div v-if="store.progresso" class="progresso-container">
                <div class="progresso-agregado">
                    Progresso geral: <strong>{{ store.progresso.percentualAgregado }}%</strong>
                </div>
                <div
                    v-for="(ent, nome) in store.progresso.porEntidade"
                    :key="nome"
                    class="progresso-entidade"
                >
                    <div class="progresso-entidade-header">
                        <span class="progresso-nome">{{ nome }}</span>
                        <span class="progresso-nums">{{ ent.total - ent.pendentes }}/{{ ent.total }} ({{ ent.percentual }}%)</span>
                    </div>
                    <div class="progresso-barra-bg">
                        <div class="progresso-barra-fill" :style="{ width: `${ent.percentual}%` }" />
                    </div>
                </div>
            </div>
            <div v-else class="muted">Aguardando dados de progresso...</div>

            <div class="marco3-acoes">
                <AppButton variant="secondary" @click="store.carregarJob(Number(props.jobId))">
                    <i class="fa-solid fa-rotate" aria-hidden="true" />
                    Atualizar status
                </AppButton>
            </div>
        </AppCard>

        <!-- Addendum 002 — Painel de falha (status: falhou) — CA25/CA29/CA30 -->
        <AppCard
            v-if="statusJob === 'falhou'"
            class="marco3-card"
        >
            <h2 class="ds-section-title">Job com falha</h2>
            <AppBadge variant="error">Falhou</AppBadge>
            <div class="falha-detalhe">
                <p v-if="store.jobAtual?.motivoFalha" class="falha-motivo">
                    <strong>Motivo:</strong> {{ store.jobAtual.motivoFalha }}
                </p>
                <p v-else class="falha-motivo">Motivo não registrado.</p>
                <p class="muted">
                    Após reprocessar, o job voltará ao estado anterior e será retomado automaticamente pelo próximo ciclo do agendador.
                </p>
            </div>
            <div v-if="erroReprocessar" class="erro-msg">{{ erroReprocessar }}</div>
            <div class="marco3-acoes">
                <AppButton
                    variant="primary"
                    :loading="store.reprocessando"
                    @click="reprocessar"
                >
                    <i class="fa-solid fa-rotate-right" aria-hidden="true" />
                    Reprocessar
                </AppButton>
            </div>
        </AppCard>

        <!-- Marco 3 — Relatório final (status: concluido / concluido_com_erros) -->
        <AppCard
            v-if="statusJob === 'concluido' || statusJob === 'concluido_com_erros'"
            class="marco3-card"
        >
            <h2 class="ds-section-title">Relatório da carga</h2>
            <AppBadge v-if="statusJob === 'concluido'" variant="success">Concluído</AppBadge>
            <AppBadge v-else variant="warning">Concluído com erros</AppBadge>
            <div v-if="store.relatorio" class="relatorio-resumo">
                <p>Criados: <strong>{{ store.relatorio.totalCriados }}</strong> · Atualizados: <strong>{{ store.relatorio.totalAtualizados }}</strong> · Rejeitados: <strong>{{ store.relatorio.totalRejeitados }}</strong> · Pulados: <strong>{{ store.relatorio.totalPulados }}</strong></p>
                <table class="relatorio-table">
                    <thead>
                        <tr>
                            <th>Entidade</th>
                            <th>Criados</th>
                            <th>Atualizados</th>
                            <th>Rejeitados</th>
                            <th>Pulados</th>
                        </tr>
                    </thead>
                    <tbody>
                        <template v-for="(ent, nome) in store.relatorio.porEntidade" :key="nome">
                            <tr>
                                <td>{{ nome }}</td>
                                <td>{{ ent.criados }}</td>
                                <td>{{ ent.atualizados }}</td>
                                <td>{{ ent.rejeitados }}</td>
                                <td>{{ ent.pulados }}</td>
                            </tr>
                            <!-- CA34 — motivos de rejeição agregados -->
                            <tr
                                v-if="Object.keys(ent.motivosRejeicao ?? {}).length > 0"
                                class="row-motivos"
                            >
                                <td colspan="5" class="motivos-cell">
                                    <span class="motivos-label">Motivos de rejeição:</span>
                                    <span
                                        v-for="(qtd, motivo) in ent.motivosRejeicao"
                                        :key="String(motivo)"
                                        class="motivo-item"
                                    >{{ motivo }}: {{ qtd }}</span>
                                </td>
                            </tr>
                            <!-- CA35 — motivos de pulo agregados -->
                            <tr
                                v-if="Object.keys(ent.motivosPulo ?? {}).length > 0"
                                class="row-motivos"
                            >
                                <td colspan="5" class="motivos-cell">
                                    <span class="motivos-label">Motivos de pulo:</span>
                                    <span
                                        v-for="(qtd, motivo) in ent.motivosPulo"
                                        :key="String(motivo)"
                                        class="motivo-item"
                                    >{{ motivo }}: {{ qtd }}</span>
                                </td>
                            </tr>
                        </template>
                    </tbody>
                </table>
            </div>
            <div v-if="erroRelatorio" class="erro-msg">{{ erroRelatorio }}</div>
            <div class="marco3-acoes">
                <AppButton
                    variant="secondary"
                    :loading="carregandoRelatorio"
                    @click="verRelatorio"
                >
                    <i class="fa-solid fa-chart-bar" aria-hidden="true" />
                    Carregar relatório
                </AppButton>
                <!-- CA17 — Botão Desfazer: reverte só os criados -->
                <AppButton
                    variant="danger"
                    @click="modalDesfazer = true"
                >
                    <i class="fa-solid fa-rotate-left" aria-hidden="true" />
                    Desfazer
                </AppButton>
            </div>
        </AppCard>

        <!-- Marco 4 — Relatório de desfazimento (status: desfeito) -->
        <AppCard v-if="statusJob === 'desfeito'" class="marco3-card">
            <h2 class="ds-section-title">Migração desfeita</h2>
            <AppBadge variant="warning">Desfeito</AppBadge>
            <div v-if="store.relatorioDesfazimento" class="relatorio-resumo">
                <p class="aviso-desfazimento" role="status">
                    {{ store.relatorioDesfazimento.aviso }}
                </p>
                <p>
                    Revertidos: <strong>{{ store.relatorioDesfazimento.totalRevertidos }}</strong>
                    · Não revertidos: <strong>{{ store.relatorioDesfazimento.totalNaoRevertidos }}</strong>
                    · Atualizados mantidos: <strong>{{ store.relatorioDesfazimento.totalAtualizadosMantidos }}</strong>
                </p>
            </div>
        </AppCard>

        <!-- Bloco B — Histórico de transições (CA53-CA56) -->
        <AppCard v-if="store.jobAtual" class="eventos-card">
            <h2 class="ds-section-title">Histórico de transições</h2>
            <div v-if="store.carregandoEventos" class="muted">Carregando histórico...</div>
            <div v-else-if="store.eventos.length === 0" class="muted eventos-vazio">
                Histórico detalhado disponível a partir desta migração.
            </div>
            <ul v-else class="eventos-lista">
                <li v-for="(evt, i) in store.eventos" :key="i" class="evento-item">
                    <i class="fa-solid fa-circle-dot evento-icone" aria-hidden="true" />
                    <span class="evento-label">{{ labelEvento(evt) }}</span>
                </li>
            </ul>
        </AppCard>

        <!-- Marco 4 — Modal de confirmação de desfazer (CA17) -->
        <AppModal
            :aberto="modalDesfazer"
            titulo="Desfazer migração"
            @fechar="modalDesfazer = false"
        >
            <p class="desfazer-aviso">
                Esta ação reverterá <strong>somente os registros criados</strong> por esta migração.
                Registros que já existiam e foram atualizados pelo upsert
                <strong>não serão tocados</strong>.
            </p>
            <div v-if="erroDesfazer" class="erro-msg">{{ erroDesfazer }}</div>
            <template #rodape>
                <AppButton variant="ghost" @click="modalDesfazer = false">Cancelar</AppButton>
                <AppButton
                    variant="danger"
                    :loading="store.desfazendo"
                    @click="confirmarDesfazer"
                >
                    Confirmar desfazer
                </AppButton>
            </template>
        </AppModal>

        <!-- Modal: salvar como template -->
        <AppModal
            :aberto="modalTemplate"
            titulo="Salvar como template"
            @fechar="modalTemplate = false"
        >
            <AppField label="Nome do sistema de origem (template)">
                <AppInput
                    v-model="nomeTemplate"
                    placeholder="Ex.: iClinic, Feegow..."
                />
            </AppField>
            <div v-if="erroTemplate" class="erro-msg">{{ erroTemplate }}</div>
            <template #rodape>
                <AppButton variant="ghost" @click="modalTemplate = false">Cancelar</AppButton>
                <AppButton
                    variant="primary"
                    :loading="salvandoTemplate"
                    @click="confirmarTemplate"
                >
                    Salvar template
                </AppButton>
            </template>
        </AppModal>
    </div>
</template>

<style scoped>
.loading-msg,
.muted {
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
    padding: 1rem 0;
}

.erro-msg {
    font-size: var(--text-sm);
    color: hsl(var(--destructive));
    margin-top: 0.5rem;
}

/* CA18 — Banner de template pré-preenchido */
.banner-template {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    background: hsl(var(--info, 210 100% 96%));
    border: 1px solid hsl(var(--info-border, 210 100% 85%));
    border-radius: var(--radius);
    padding: 0.75rem 1rem;
    margin-bottom: 1.25rem;
    font-size: var(--text-sm);
    color: hsl(var(--foreground));
}

.mapa-card {
    margin-bottom: 1.5rem;
}

/* Addendum 4 — alertas de bloco (encoding suspeito, sem_equivalente) */
.bloco-alerta {
    display: flex;
    align-items: flex-start;
    gap: 0.5rem;
    padding: 0.625rem 1rem;
    border-radius: var(--radius);
    font-size: var(--text-sm);
    margin-bottom: 0.75rem;
}

.bloco-alerta--warning {
    background: hsl(var(--warning) / 0.1);
    border-left: 3px solid hsl(var(--warning));
    color: hsl(var(--foreground));
}

.bloco-alerta--info {
    background: hsl(var(--info, 210 100% 96%));
    border-left: 3px solid hsl(var(--info-border, 210 100% 85%));
    color: hsl(var(--foreground));
}

/* Addendum 4 — painel de classificação de entidade por IA */
.bloco-classificacao {
    background: hsl(var(--muted) / 0.4);
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    padding: 0.75rem 1rem;
    margin-bottom: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.classificacao-linha {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
}

.classificacao-label {
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
    white-space: nowrap;
}

.classificacao-badge {
    font-family: monospace;
}

.classificacao-reclassificar {
    display: flex;
    flex-wrap: wrap;
    gap: 0.75rem;
    align-items: flex-end;
    margin-top: 0.25rem;
}

.classificacao-select {
    min-width: 220px;
}

.ignorar-label {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: var(--text-sm);
    cursor: pointer;
    color: hsl(var(--foreground));
    padding-bottom: 0.5rem;
}

.ignorar-checkbox {
    width: 1rem;
    height: 1rem;
    cursor: pointer;
}

.mapa-header {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 1rem;
}

.mapa-badges {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
}

.depara-table {
    width: 100%;
    border-collapse: collapse;
    font-size: var(--text-sm);
    margin-bottom: 1rem;
}

.depara-table th,
.depara-table td {
    padding: 0.5rem 0.75rem;
    border-bottom: 1px solid hsl(var(--border));
    text-align: left;
    vertical-align: middle;
}

.depara-table th {
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--muted-foreground));
}

.depara-table tr:last-child td {
    border-bottom: none;
}

.row-duvida {
    background: hsl(var(--warning) / 0.08);
}

.col-origem {
    font-family: monospace;
}

.duvida-icon {
    color: hsl(var(--warning));
    margin-left: 0.4rem;
    font-size: var(--text-xs);
}

.mapa-acoes {
    display: flex;
    justify-content: flex-end;
}

.sem-mapas {
    padding: 2rem;
}

.marco3-card {
    margin-bottom: 1.5rem;
}

.marco3-acoes {
    display: flex;
    justify-content: flex-end;
    margin-top: 1rem;
}

.preview-resultado,
.relatorio-resumo {
    margin: 1rem 0;
}

.preview-total {
    font-size: var(--text-sm);
    margin-bottom: 0.5rem;
}

.relatorio-table {
    width: 100%;
    border-collapse: collapse;
    font-size: var(--text-sm);
    margin-top: 0.5rem;
}

.relatorio-table th,
.relatorio-table td {
    padding: 0.4rem 0.75rem;
    border-bottom: 1px solid hsl(var(--border));
    text-align: left;
}

.relatorio-table th {
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--muted-foreground));
}

.relatorio-table tr:last-child td {
    border-bottom: none;
}

/* Addendum 002 — falhou */
.falha-detalhe {
    margin: 1rem 0;
}

.falha-motivo {
    font-size: var(--text-sm);
    background: hsl(var(--destructive) / 0.08);
    border-left: 3px solid hsl(var(--destructive));
    padding: 0.75rem 1rem;
    border-radius: var(--radius);
    margin-bottom: 0.75rem;
    color: hsl(var(--foreground));
}

/* CA34/CA35 — motivos agregados no relatório */
.row-motivos td {
    background: hsl(var(--muted) / 0.4);
    padding-top: 0.25rem;
    padding-bottom: 0.25rem;
}

.motivos-cell {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    align-items: center;
}

.motivos-label {
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--foreground));
    margin-right: 0.25rem;
}

.motivo-item {
    background: hsl(var(--muted));
    border-radius: var(--radius);
    padding: 0.125rem 0.5rem;
    white-space: nowrap;
}

/* Bloco A — Stepper */
.stepper {
    display: flex;
    align-items: flex-start;
    gap: 0;
    margin-bottom: 1.5rem;
    flex-wrap: wrap;
}

.stepper-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    position: relative;
    flex: 1;
    min-width: 80px;
}

.stepper-indicador {
    width: 2rem;
    height: 2rem;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    border: 2px solid hsl(var(--border));
    background: hsl(var(--background));
    color: hsl(var(--muted-foreground));
    z-index: 1;
}

.stepper-item--concluido .stepper-indicador {
    background: hsl(var(--success, 142 76% 36%));
    border-color: hsl(var(--success, 142 76% 36%));
    color: hsl(var(--background));
}

.stepper-item--atual .stepper-indicador {
    background: hsl(var(--primary));
    border-color: hsl(var(--primary));
    color: hsl(var(--primary-foreground));
}

.stepper-item--erro .stepper-indicador {
    background: hsl(var(--destructive));
    border-color: hsl(var(--destructive));
    color: hsl(var(--background));
}

.stepper-label {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    margin-top: 0.25rem;
    text-align: center;
    max-width: 90px;
}

.stepper-item--atual .stepper-label {
    color: hsl(var(--primary));
    font-weight: var(--font-weight-semibold);
}

.stepper-linha {
    position: absolute;
    top: 1rem;
    left: 50%;
    width: 100%;
    height: 2px;
    background: hsl(var(--border));
    z-index: 0;
}

.stepper-item--concluido .stepper-linha {
    background: hsl(var(--success, 142 76% 36%));
}

/* Bloco C — Progresso */
.progresso-container {
    margin: 1rem 0;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.progresso-agregado {
    font-size: var(--text-sm);
    margin-bottom: 0.25rem;
}

.progresso-entidade-header {
    display: flex;
    justify-content: space-between;
    margin-bottom: 0.25rem;
}

.progresso-nome {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
}

.progresso-nums {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
}

.progresso-barra-bg {
    height: 6px;
    background: hsl(var(--muted));
    border-radius: 3px;
    overflow: hidden;
}

.progresso-barra-fill {
    height: 100%;
    background: hsl(var(--primary));
    border-radius: 3px;
    transition: width 0.3s ease;
}

/* Bloco B — Eventos */
.eventos-card {
    margin-bottom: 1.5rem;
}

.eventos-vazio {
    padding: 0.5rem 0;
}

.eventos-lista {
    list-style: none;
    padding: 0;
    margin: 0.5rem 0 0;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.evento-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: var(--text-sm);
}

.evento-icone {
    font-size: var(--text-xs);
    color: hsl(var(--muted-foreground));
    flex-shrink: 0;
}

.evento-label {
    color: hsl(var(--foreground));
}

/* Marco 4 — desfazer */
.desfazer-aviso {
    font-size: var(--text-sm);
    color: hsl(var(--foreground));
    line-height: 1.6;
}

.aviso-desfazimento {
    font-size: var(--text-sm);
    background: hsl(var(--warning) / 0.1);
    border-left: 3px solid hsl(var(--warning));
    padding: 0.75rem 1rem;
    border-radius: var(--radius);
    margin-bottom: 0.75rem;
    color: hsl(var(--foreground));
}
</style>
