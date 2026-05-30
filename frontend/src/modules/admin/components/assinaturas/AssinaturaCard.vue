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
    <div class="assinatura-card">
        <!-- Cabeçalho: plano vigente -->
        <div class="assinatura-card-header">
            <div class="assinatura-card-info">
                <div v-if="assinaturasStore.carregando" class="assinatura-carregando">Carregando...</div>
                <template v-else-if="vigente">
                    <span class="assinatura-plano-nome">{{ vigente.planoNome }}</span>
                    <span v-if="vigente.gratuita" class="badge-gratuidade">Gratuidade</span>
                    <span class="assinatura-vigente-badge">Vigente</span>
                    <span class="assinatura-datas">
                        desde {{ formatarData(vigente.iniciadaEm) }}
                        <template v-if="vigente.fimEm"> até {{ formatarData(vigente.fimEm) }}</template>
                    </span>
                </template>
                <span v-else class="assinatura-sem-plano">Sem plano ativo</span>
            </div>

            <div class="assinatura-card-acoes">
                <button class="admin-btn-secondary" @click="modalTrocar = true">Trocar plano</button>
                <button class="admin-btn-gratuidade" @click="modalGratuidade = true">Gratuidade</button>
            </div>
        </div>

        <!-- Histórico -->
        <div class="assinatura-historico">
            <h3 class="assinatura-historico-titulo">Histórico de assinaturas</h3>
            <div v-if="assinaturasStore.erro" class="admin-erro">{{ assinaturasStore.erro }}</div>
            <table v-else class="admin-table-mini">
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
                            <span v-if="a.gratuita" class="badge-gratuidade-sm">Gratuidade</span>
                            <span v-else class="badge-normal">Padrão</span>
                        </td>
                        <td>
                            <button
                                v-if="a.vigente"
                                class="btn-encerrar"
                                @click="abrirEncerrar(a.id)"
                                title="Encerrar assinatura"
                            >
                                Encerrar
                            </button>
                        </td>
                    </tr>
                    <tr v-if="assinaturasStore.historico.length === 0">
                        <td colspan="5" class="assinatura-vazio">Sem histórico.</td>
                    </tr>
                </tbody>
            </table>
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
    </div>
</template>

<style scoped>
.assinatura-card {
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 12px;
    overflow: hidden;
}

.assinatura-card-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1rem 1.25rem;
    border-bottom: 1px solid #334155;
    gap: 1rem;
    flex-wrap: wrap;
}

.assinatura-card-info {
    display: flex;
    align-items: center;
    gap: 0.625rem;
    flex-wrap: wrap;
}

.assinatura-plano-nome {
    font-size: 1rem;
    font-weight: 700;
    color: #f8fafc;
}

.assinatura-vigente-badge {
    background: #14532d;
    color: #4ade80;
    font-size: 0.6875rem;
    font-weight: 600;
    padding: 0.15rem 0.5rem;
    border-radius: 9999px;
}

.badge-gratuidade {
    background: #065f46;
    color: #6ee7b7;
    font-size: 0.6875rem;
    font-weight: 600;
    padding: 0.15rem 0.5rem;
    border-radius: 9999px;
}

.badge-gratuidade-sm {
    background: #065f46;
    color: #6ee7b7;
    font-size: 0.6875rem;
    padding: 0.1rem 0.4rem;
    border-radius: 9999px;
}

.badge-normal {
    background: #1e3a5f;
    color: #93c5fd;
    font-size: 0.6875rem;
    padding: 0.1rem 0.4rem;
    border-radius: 9999px;
}

.assinatura-datas {
    font-size: 0.8125rem;
    color: #94a3b8;
}

.assinatura-sem-plano {
    color: #94a3b8;
    font-size: 0.875rem;
}

.assinatura-card-acoes {
    display: flex;
    gap: 0.5rem;
}

.admin-btn-secondary {
    background: #334155;
    color: #f8fafc;
    border: none;
    border-radius: 6px;
    padding: 0.4375rem 0.875rem;
    font-size: 0.8125rem;
    cursor: pointer;
}

.admin-btn-secondary:hover {
    background: #475569;
}

.admin-btn-gratuidade {
    background: #065f46;
    color: #6ee7b7;
    border: none;
    border-radius: 6px;
    padding: 0.4375rem 0.875rem;
    font-size: 0.8125rem;
    font-weight: 600;
    cursor: pointer;
}

.admin-btn-gratuidade:hover {
    background: #047857;
}

.assinatura-historico {
    padding: 1rem 1.25rem;
}

.assinatura-historico-titulo {
    font-size: 0.875rem;
    font-weight: 600;
    color: #94a3b8;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    margin: 0 0 0.75rem;
}

.admin-table-mini {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.8125rem;
}

.admin-table-mini th,
.admin-table-mini td {
    text-align: left;
    padding: 0.5rem 0.75rem;
    border-bottom: 1px solid #0f172a;
}

.admin-table-mini th {
    color: #64748b;
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

.row-vigente {
    background: rgba(20, 83, 45, 0.15);
}

.btn-encerrar {
    background: none;
    border: 1px solid #f87171;
    color: #f87171;
    border-radius: 4px;
    padding: 0.2rem 0.5rem;
    font-size: 0.75rem;
    cursor: pointer;
}

.btn-encerrar:hover {
    background: rgba(248, 113, 113, 0.1);
}

.assinatura-vazio,
.assinatura-carregando,
.admin-erro {
    color: #94a3b8;
    text-align: center;
    padding: 1rem 0;
    font-size: 0.875rem;
}

.admin-erro {
    color: #f87171;
}
</style>
