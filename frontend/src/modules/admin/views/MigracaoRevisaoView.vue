<script setup lang="ts">
/**
 * MigracaoRevisaoView — painel de revisão de mapeamento de migração.
 * Admin revisa o de-para gerado pela IA, edita e salva. Pode salvar como template.
 */
import { computed, onMounted, ref } from "vue"
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

const props = defineProps<{ jobId: string }>()

const router = useRouter()
const store  = useMigracaoAdminStore()

// Estado local de edição — um dict por entidade { entidade: { col_origem: campo_canonico } }
const edicoes = ref<Record<string, Record<string, string>>>({})
const salvando = ref<Record<string, boolean>>({})
const erros = ref<Record<string, string>>({})

// Marco 3 — preview + disparo + relatório
const erroPreview = ref("")
const gerandoPreview = ref(false)
const erroDisparar = ref("")
const erroRelatorio = ref("")
const carregandoRelatorio = ref(false)

// Modal de template
const modalTemplate = ref(false)
const nomeTemplate  = ref("")
const erroTemplate  = ref("")
const salvandoTemplate = ref(false)

onMounted(async () => {
    await store.carregarJob(Number(props.jobId))
    inicializarEdicoes()
})

function inicializarEdicoes() {
    if (!store.jobAtual) return
    for (const mapa of store.jobAtual.mapas) {
        try {
            const parsed = JSON.parse(mapa.mapaJson) as {
                de_para?: Record<string, string>
                confianca?: number
                duvidas?: string[]
            }
            edicoes.value[mapa.entidade] = { ...(parsed.de_para ?? {}) }
        } catch {
            edicoes.value[mapa.entidade] = {}
        }
    }
}

function parsedMapa(mapaJson: string) {
    try {
        return JSON.parse(mapaJson) as {
            de_para: Record<string, string>
            confianca: number
            duvidas: string[]
        }
    } catch {
        return { de_para: {}, confianca: 0, duvidas: [] }
    }
}

function confiancaVariant(c: number): "success" | "warning" | "error" {
    if (c >= 0.8) return "success"
    if (c >= 0.6) return "warning"
    return "error"
}

function isDuvida(entidade: string, col: string): boolean {
    const mapa = store.jobAtual?.mapas.find(m => m.entidade === entidade)
    if (!mapa) return false
    return parsedMapa(mapa.mapaJson).duvidas?.includes(col) ?? false
}

async function salvarMapa(entidade: string) {
    salvando.value[entidade] = true
    erros.value[entidade] = ""
    try {
        await store.salvarMapa(Number(props.jobId), entidade, edicoes.value[entidade] ?? {})
    } catch {
        erros.value[entidade] = "Não foi possível salvar o mapa."
    } finally {
        salvando.value[entidade] = false
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

        <!-- Sem mapas ainda -->
        <AppCard v-else-if="!temMapas" class="sem-mapas">
            <p class="muted">Nenhum mapa gerado ainda. Aguarde a inferência da IA.</p>
        </AppCard>

        <!-- Mapas por entidade -->
        <template v-else>
            <!-- CA18 — Banner de template pré-preenchido -->
            <div v-if="store.jobAtual?.nomeTemplate" class="banner-template" role="status">
                <i class="fa-solid fa-circle-info" aria-hidden="true" />
                Mapeamento pré-preenchido pelo template <strong>{{ store.jobAtual.nomeTemplate }}</strong> — revise antes de confirmar.
            </div>
            <AppCard
                v-for="mapa in store.jobAtual!.mapas"
                :key="mapa.entidade"
                class="mapa-card"
            >
                <div class="mapa-header">
                    <h2 class="ds-section-title">{{ mapa.entidade }}</h2>
                    <div class="mapa-badges">
                        <AppBadge :variant="confiancaVariant(parsedMapa(mapa.mapaJson).confianca)">
                            Confiança {{ Math.round(parsedMapa(mapa.mapaJson).confianca * 100) }}%
                        </AppBadge>
                        <AppBadge v-if="mapa.revisadoEm" variant="success">
                            Revisado
                        </AppBadge>
                    </div>
                </div>

                <!-- Tabela de-para editável -->
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
                            :class="{ 'row-duvida': isDuvida(mapa.entidade, col) }"
                        >
                            <td>
                                <span class="col-origem">{{ col }}</span>
                                <i
                                    v-if="isDuvida(mapa.entidade, col)"
                                    class="fa-solid fa-triangle-exclamation duvida-icon"
                                    title="Coluna com baixa confiança — revise"
                                    aria-hidden="true"
                                />
                            </td>
                            <td>
                                <AppInput
                                    v-if="edicoes[mapa.entidade]"
                                    v-model="edicoes[mapa.entidade][col]"
                                    placeholder="campo_canonico"
                                />
                                <span v-else>{{ campo }}</span>
                            </td>
                        </tr>
                    </tbody>
                </table>

                <div v-if="erros[mapa.entidade]" class="erro-msg">{{ erros[mapa.entidade] }}</div>

                <div class="mapa-acoes">
                    <AppButton
                        variant="primary"
                        :loading="salvando[mapa.entidade]"
                        @click="salvarMapa(mapa.entidade)"
                    >
                        Salvar mapa revisado
                    </AppButton>
                </div>
            </AppCard>
        </template>

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

        <!-- Marco 3 — Painel de progresso (status: migrando) -->
        <AppCard
            v-if="statusJob === 'migrando'"
            class="marco3-card"
        >
            <h2 class="ds-section-title">Carga em andamento</h2>
            <p class="muted">A importação está sendo processada em background. Aguarde e recarregue a página para ver o resultado.</p>
            <div class="marco3-acoes">
                <AppButton variant="secondary" @click="store.carregarJob(Number(props.jobId))">
                    <i class="fa-solid fa-rotate" aria-hidden="true" />
                    Atualizar status
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
                        <tr v-for="(ent, nome) in store.relatorio.porEntidade" :key="nome">
                            <td>{{ nome }}</td>
                            <td>{{ ent.criados }}</td>
                            <td>{{ ent.atualizados }}</td>
                            <td>{{ ent.rejeitados }}</td>
                            <td>{{ ent.pulados }}</td>
                        </tr>
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
            </div>
        </AppCard>

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
</style>
