<script setup lang="ts">
/**
 * AssinaturaCard.vue
 *
 * Sub-componente reutilizável para exibir o plano vigente + histórico de assinaturas
 * de um estabelecimento dentro da área admin.
 *
 * USO (para Dev B — tela de detalhe do estabelecimento):
 *   <AssinaturaCard :estabelecimento-id="estabelecimento.id" />
 *
 * O componente gerencia seu próprio estado via assinaturasStore e planosStore.
 * Expõe modais de Trocar Plano / Conceder Gratuidade / Encerrar — totalmente
 * autocontidos. Não requer props além do estabelecimento-id.
 */
import { ref, computed, onMounted, watch } from "vue"
import { AppCard, AppButton, AppBadge } from "@/components/ui"
import { useAssinaturasStore } from "../../stores/assinaturasStore"
import { usePlanosStore } from "../../stores/planosStore"
import TrocarPlanoModal from "./TrocarPlanoModal.vue"
import ConcederGratuidadeModal from "./ConcederGratuidadeModal.vue"
import EncerrarAssinaturaModal from "./EncerrarAssinaturaModal.vue"

const props = defineProps<{
    estabelecimentoId: number
}>()

const assinaturasStore = useAssinaturasStore()
const planosStore = usePlanosStore()

const modalTrocar = ref(false)
const modalGratuidade = ref(false)
const modalEncerrar = ref(false)
const assinaturaParaEncerrar = ref<string | null>(null)

const vigente = computed(() => assinaturasStore.vigente())

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

async function aoTrocar() {
    modalTrocar.value = false
    await assinaturasStore.carregarHistorico(props.estabelecimentoId)
}

async function aoGratuidade() {
    modalGratuidade.value = false
    await assinaturasStore.carregarHistorico(props.estabelecimentoId)
}

async function aoEncerrar() {
    modalEncerrar.value = false
    assinaturaParaEncerrar.value = null
    await assinaturasStore.carregarHistorico(props.estabelecimentoId)
}
</script>

<template>
    <AppCard title="Plano / Assinatura — Histórico">
        <template #header-aside>
            <div class="card-acoes">
                <AppButton variant="secondary" size="sm" @click="modalTrocar = true">Trocar plano</AppButton>
                <AppButton variant="success" size="sm" @click="modalGratuidade = true">Gratuidade</AppButton>
            </div>
        </template>

        <!-- Vigente -->
        <div class="vigente-row">
            <div v-if="assinaturasStore.carregando" class="estado-info">
                <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
            </div>
            <template v-else-if="vigente">
                <span class="vigente-nome">{{ vigente.planoNome }}</span>
                <AppBadge v-if="vigente.gratuita" variant="success" label="Gratuidade" />
                <AppBadge variant="success" label="Vigente" />
                <span class="vigente-datas">
                    desde {{ formatarData(vigente.iniciadaEm) }}
                    <template v-if="vigente.fimEm"> até {{ formatarData(vigente.fimEm) }}</template>
                </span>
            </template>
            <span v-else class="estado-info">Sem plano ativo</span>
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
                            <th>Tipo</th>
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
                            <td>
                                <AppBadge v-if="a.gratuita" variant="success" label="Gratuidade" />
                                <AppBadge v-else variant="info" label="Padrão" />
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
                            <td colspan="5" class="historico-vazio">Sem histórico.</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- Modais -->
        <TrocarPlanoModal
            v-if="modalTrocar"
            :estabelecimento-id="estabelecimentoId"
            :planos="planosStore.lista"
            @fechar="modalTrocar = false"
            @sucesso="aoTrocar"
        />

        <ConcederGratuidadeModal
            v-if="modalGratuidade"
            :estabelecimento-id="estabelecimentoId"
            @fechar="modalGratuidade = false"
            @sucesso="aoGratuidade"
        />

        <EncerrarAssinaturaModal
            v-if="modalEncerrar && assinaturaParaEncerrar"
            :assinatura-id="assinaturaParaEncerrar"
            :estabelecimento-id="estabelecimentoId"
            @fechar="modalEncerrar = false"
            @sucesso="aoEncerrar"
        />
    </AppCard>
</template>

<style scoped>
.card-acoes {
    display: flex;
    gap: 0.5rem;
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
    font-size: 1rem;
    font-weight: 700;
    color: hsl(var(--foreground));
}

.vigente-datas {
    font-size: 0.8125rem;
    color: hsl(var(--muted-foreground));
}

.historico-wrap {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.historico-titulo {
    font-size: 0.75rem;
    font-weight: 600;
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
    font-size: 0.8125rem;
}

.tabela th,
.tabela td {
    text-align: left;
    padding: 0.5rem 0.75rem;
    border-bottom: 1px solid hsl(var(--border));
}

.tabela th {
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.5);
    font-size: 0.75rem;
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
    font-size: 0.75rem;
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
    font-size: 0.875rem;
}

.estado-erro {
    color: hsl(var(--destructive));
    font-size: 0.875rem;
}
</style>
