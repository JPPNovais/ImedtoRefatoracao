<script setup lang="ts">
/**
 * AssinaturaCard.vue — F4 atualizado
 *
 * Exibe o estado derivado (BLOQUEADO/VITALÍCIO/TRIAL/SUSPENSO) da assinatura vigente
 * e expõe os 5 botões de ação admin: Liberar Vitalício, Liberar até Data, Iniciar Trial,
 * Suspender, Reativar — além dos anteriores Trocar Plano, Gratuidade, Encerrar.
 */
import { ref, computed, onMounted, watch } from "vue"
import { AppCard, AppButton, AppBadge } from "@/components/ui"
import { useAssinaturasStore } from "../../stores/assinaturasStore"
import { usePlanosStore } from "../../stores/planosStore"
import TrocarPlanoModal from "./TrocarPlanoModal.vue"
import ConcederGratuidadeModal from "./ConcederGratuidadeModal.vue"
import EncerrarAssinaturaModal from "./EncerrarAssinaturaModal.vue"
import LiberarVitalicioModal from "./LiberarVitalicioModal.vue"
import LiberarAteDataModal from "./LiberarAteDataModal.vue"
import IniciarTrialModal from "./IniciarTrialModal.vue"
import SuspenderReativarModal from "./SuspenderReativarModal.vue"

const props = defineProps<{
    estabelecimentoId: number
}>()

const assinaturasStore = useAssinaturasStore()
const planosStore = usePlanosStore()

const modalTrocar = ref(false)
const modalGratuidade = ref(false)
const modalEncerrar = ref(false)
const modalVitalicio = ref(false)
const modalAteData = ref(false)
const modalTrial = ref(false)
const modalSuspenderReativar = ref<"suspender" | "reativar" | null>(null)
const assinaturaParaEncerrar = ref<string | null>(null)

const vigente = computed(() => assinaturasStore.vigente())

/** Badge visual do estado derivado */
const estadoBadge = computed(() => {
    const e = vigente.value?.estado ?? ""
    switch (e) {
        case "Vitalicia": return { label: "Vitalício", variant: "success" } as const
        case "Temporaria": return { label: "Temporário", variant: "info" } as const
        case "Suspensa": return { label: "Suspenso", variant: "warning" } as const
        case "Expirada": return { label: "Expirado", variant: "error" } as const
        case "Encerrada": return { label: "Encerrado", variant: "error" } as const
        default: return null
    }
})

const estaAtivo = computed(() => vigente.value?.estado === "Vitalicia" || vigente.value?.estado === "Temporaria")
const estaSuspenso = computed(() => vigente.value?.estado === "Suspensa")
const semVigencia = computed(() => !vigente.value)

onMounted(() => carregarDados())

watch(() => props.estabelecimentoId, () => carregarDados())

async function carregarDados() {
    await Promise.all([
        assinaturasStore.carregarHistorico(props.estabelecimentoId),
        planosStore.carregar({ ativo: true }),
    ])
}

function formatarData(iso: string | null): string {
    if (!iso) return "—"
    return new Date(iso).toLocaleDateString("pt-BR")
}

function abrirEncerrar(assinaturaId: string) {
    assinaturaParaEncerrar.value = assinaturaId
    modalEncerrar.value = true
}

async function aoRecarregar() {
    await assinaturasStore.carregarHistorico(props.estabelecimentoId)
}
</script>

<template>
    <AppCard title="Plano / Assinatura — Histórico">
        <template #header-aside>
            <div class="card-acoes">
                <AppButton variant="secondary" size="sm" @click="modalVitalicio = true">Liberar vitalício</AppButton>
                <AppButton variant="secondary" size="sm" @click="modalAteData = true">Liberar até data</AppButton>
                <AppButton variant="secondary" size="sm" @click="modalTrial = true">Iniciar trial</AppButton>
                <AppButton variant="secondary" size="sm" @click="modalTrocar = true">Trocar plano</AppButton>
                <AppButton variant="success" size="sm" @click="modalGratuidade = true">Gratuidade</AppButton>
            </div>
        </template>

        <!-- Estado vigente -->
        <div class="vigente-row">
            <div v-if="assinaturasStore.carregando" class="estado-info">
                <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
            </div>
            <template v-else-if="vigente">
                <span class="vigente-nome">{{ vigente.planoNome }}</span>
                <AppBadge v-if="estadoBadge" :variant="estadoBadge.variant" :label="estadoBadge.label" />
                <AppBadge v-if="vigente.gratuita" variant="success" label="Gratuidade" />
                <span class="vigente-datas">
                    desde {{ formatarData(vigente.iniciadaEm) }}
                    <template v-if="vigente.expiraEm"> · expira {{ formatarData(vigente.expiraEm) }}</template>
                </span>
                <div class="acoes-estado">
                    <AppButton
                        v-if="estaAtivo"
                        variant="danger"
                        size="sm"
                        @click="modalSuspenderReativar = 'suspender'"
                    >Suspender</AppButton>
                    <AppButton
                        v-if="estaSuspenso"
                        variant="primary"
                        size="sm"
                        @click="modalSuspenderReativar = 'reativar'"
                    >Reativar</AppButton>
                </div>
            </template>
            <div v-else class="sem-vigencia">
                <span class="estado-info">Sem plano ativo</span>
                <AppBadge variant="error" label="BLOQUEADO" />
            </div>
        </div>

        <!-- Histórico -->
        <div class="historico-wrap">
            <p class="historico-titulo">Histórico de assinaturas</p>
            <div v-if="assinaturasStore.erro" class="estado-erro">{{ assinaturasStore.erro }}</div>
            <div v-else class="tabela-wrap">
                <table class="tabela">
                    <thead>
                        <tr>
                            <th>Plano</th>
                            <th>Início</th>
                            <th>Fim</th>
                            <th>Expira</th>
                            <th>Estado</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr
                            v-for="a in assinaturasStore.historico"
                            :key="a.id"
                            :class="{ 'row-vigente': a.vigente }"
                        >
                            <td>{{ a.planoNome }}</td>
                            <td>{{ formatarData(a.iniciadaEm) }}</td>
                            <td>{{ a.fimEm ? formatarData(a.fimEm) : "—" }}</td>
                            <td>{{ a.expiraEm ? formatarData(a.expiraEm) : "Nunca" }}</td>
                            <td>
                                <AppBadge
                                    v-if="a.estado === 'Vitalicia'"
                                    variant="success" label="Vitalício"
                                />
                                <AppBadge
                                    v-else-if="a.estado === 'Temporaria'"
                                    variant="info" label="Temporário"
                                />
                                <AppBadge
                                    v-else-if="a.estado === 'Suspensa'"
                                    variant="warning" label="Suspenso"
                                />
                                <AppBadge
                                    v-else-if="a.estado === 'Expirada'"
                                    variant="error" label="Expirado"
                                />
                                <AppBadge v-else variant="muted" :label="a.estado || 'Encerrado'" />
                            </td>
                            <td>
                                <button
                                    v-if="a.vigente"
                                    class="btn-encerrar"
                                    type="button"
                                    @click="abrirEncerrar(a.id)"
                                    title="Encerrar assinatura"
                                >
                                    Encerrar
                                </button>
                            </td>
                        </tr>
                        <tr v-if="assinaturasStore.historico.length === 0">
                            <td colspan="6" class="historico-vazio">Sem histórico.</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- Modais -->
        <LiberarVitalicioModal
            v-if="modalVitalicio"
            :estabelecimento-id="estabelecimentoId"
            :planos="planosStore.lista"
            @fechar="modalVitalicio = false"
            @sucesso="modalVitalicio = false; aoRecarregar()"
        />

        <LiberarAteDataModal
            v-if="modalAteData"
            :estabelecimento-id="estabelecimentoId"
            :planos="planosStore.lista"
            @fechar="modalAteData = false"
            @sucesso="modalAteData = false; aoRecarregar()"
        />

        <IniciarTrialModal
            v-if="modalTrial"
            :estabelecimento-id="estabelecimentoId"
            :planos="planosStore.lista"
            @fechar="modalTrial = false"
            @sucesso="modalTrial = false; aoRecarregar()"
        />

        <SuspenderReativarModal
            v-if="modalSuspenderReativar"
            :estabelecimento-id="estabelecimentoId"
            :acao="modalSuspenderReativar"
            @fechar="modalSuspenderReativar = null"
            @sucesso="modalSuspenderReativar = null; aoRecarregar()"
        />

        <TrocarPlanoModal
            v-if="modalTrocar"
            :estabelecimento-id="estabelecimentoId"
            :planos="planosStore.lista"
            @fechar="modalTrocar = false"
            @sucesso="modalTrocar = false; aoRecarregar()"
        />

        <ConcederGratuidadeModal
            v-if="modalGratuidade"
            :estabelecimento-id="estabelecimentoId"
            @fechar="modalGratuidade = false"
            @sucesso="modalGratuidade = false; aoRecarregar()"
        />

        <EncerrarAssinaturaModal
            v-if="modalEncerrar && assinaturaParaEncerrar"
            :assinatura-id="assinaturaParaEncerrar"
            :estabelecimento-id="estabelecimentoId"
            @fechar="modalEncerrar = false"
            @sucesso="modalEncerrar = false; assinaturaParaEncerrar = null; aoRecarregar()"
        />
    </AppCard>
</template>

<style scoped>
.card-acoes {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
}

.vigente-row {
    display: flex;
    align-items: center;
    gap: 0.625rem;
    flex-wrap: wrap;
    padding-bottom: 1rem;
    border-bottom: 1px solid hsl(var(--border));
    margin-bottom: 1rem;
}

.vigente-nome {
    font-size: var(--text-base);
    font-weight: var(--font-weight-bold);
    color: hsl(var(--foreground));
}

.vigente-datas {
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
}

.acoes-estado {
    display: flex;
    gap: 0.5rem;
}

.sem-vigencia {
    display: flex;
    align-items: center;
    gap: 0.625rem;
}

.historico-wrap {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.historico-titulo {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--muted-foreground));
    text-transform: uppercase;
    letter-spacing: 0.05em;
    margin: 0;
}

.tabela-wrap {
    overflow-x: auto;
    border: 1px solid hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
}

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: var(--text-sm);
}

.tabela th,
.tabela td {
    text-align: left;
    padding: 0.5rem 0.75rem;
    border-bottom: 1px solid hsl(var(--border));
}

.tabela th {
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.5);
    font-size: var(--text-xs);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.tabela tbody tr:last-child td { border-bottom: none; }

.row-vigente {
    background: hsl(var(--success) / 0.08);
}

.btn-encerrar {
    background: none;
    border: 1px solid hsl(var(--destructive));
    color: hsl(var(--destructive));
    border-radius: calc(var(--radius) - 2px);
    padding: 0.2rem 0.5rem;
    font-size: var(--text-xs);
    cursor: pointer;
    font-family: inherit;
}

.btn-encerrar:hover {
    background: hsl(var(--destructive) / 0.1);
}

.historico-vazio,
.estado-info {
    color: hsl(var(--muted-foreground));
    text-align: center;
    padding: 1rem 0;
    font-size: var(--text-sm);
}

.estado-erro {
    color: hsl(var(--destructive));
    font-size: var(--text-sm);
}
</style>
