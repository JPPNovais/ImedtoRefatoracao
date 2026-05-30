<script setup lang="ts">
/**
 * EstabelecimentoDetalheView — detalhe completo do estabelecimento.
 *
 * CA17–CA19: CPF mascarado por padrão; botão olho abre modal de motivo.
 * CA20: zero campo de paciente.
 * CA21: estado de não encontrado.
 * CA32–CA36: reset com confirmação dupla.
 */
import { ref, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useEstabelecimentosStore } from "../stores/estabelecimentosStore"
import ModalRevelarCpf from "../components/estabelecimentos/ModalRevelarCpf.vue"
import ModalResetTenant from "../components/estabelecimentos/ModalResetTenant.vue"
import BadgeStatusEstabelecimento from "../components/estabelecimentos/BadgeStatusEstabelecimento.vue"

const route = useRoute()
const router = useRouter()
const store = useEstabelecimentosStore()

const id = Number(route.params.id)

const modalRevelarAberto = ref(false)
const modalResetAberto = ref(false)

onMounted(() => {
    store.carregarDetalhe(id)
    store.limparCpfRevelado()
})

async function aoRevelar(motivo: string) {
    await store.revelarCpf(id, motivo)
}

async function aoConfirmarReset(motivo: string, confirmarNomeFantasia: string) {
    const ok = await store.resetTenant(id, motivo, confirmarNomeFantasia)
    if (ok) {
        modalResetAberto.value = false
        // Recarrega detalhe após reset.
        store.carregarDetalhe(id)
    }
}

function formatarData(iso: string | null): string {
    if (!iso) return "—"
    return new Date(iso).toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric" })
}

function formatarDataHora(iso: string): string {
    return new Date(iso).toLocaleString("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" })
}
</script>

<template>
    <div class="admin-page">
        <!-- Breadcrumb -->
        <nav class="breadcrumb">
            <button class="breadcrumb-link" type="button" @click="router.push({ name: 'AdminEstabelecimentos' })">
                Estabelecimentos
            </button>
            <span class="breadcrumb-sep">/</span>
            <span>{{ store.detalhe?.nomeFantasia ?? "Detalhe" }}</span>
        </nav>

        <!-- Loading -->
        <div v-if="store.carregandoDetalhe" class="estado-loading">Carregando...</div>

        <!-- Não encontrado (CA21) -->
        <div v-else-if="store.erroDetalhe" class="estado-erro" role="alert">
            <p>{{ store.erroDetalhe }}</p>
        </div>

        <template v-else-if="store.detalhe">
            <div class="detalhe-header">
                <div class="detalhe-titulo-wrap">
                    <h1 class="detalhe-titulo">{{ store.detalhe.nomeFantasia }}</h1>
                    <BadgeStatusEstabelecimento :status="store.detalhe.status" />
                </div>
                <button
                    class="btn-perigo-destaque"
                    type="button"
                    @click="modalResetAberto = true"
                >
                    Resetar dados
                </button>
            </div>

            <!-- Abas simples -->
            <div class="abas-grid">
                <!-- Metadados -->
                <section class="card-secao">
                    <h2 class="card-titulo">Identificação</h2>
                    <dl class="dl-grid">
                        <dt>ID</dt><dd>{{ store.detalhe.id }}</dd>
                        <dt>Nome fantasia</dt><dd>{{ store.detalhe.nomeFantasia }}</dd>
                        <dt>Razão social</dt><dd>{{ store.detalhe.razaoSocial }}</dd>
                        <dt>CNPJ</dt><dd>{{ store.detalhe.cnpj || "—" }}</dd>
                        <dt>Telefone</dt><dd>{{ store.detalhe.telefone || "—" }}</dd>
                        <dt>E-mail</dt><dd>{{ store.detalhe.email || "—" }}</dd>
                        <dt>Cidade/UF</dt>
                        <dd>{{ [store.detalhe.cidade, store.detalhe.estado].filter(Boolean).join(" / ") || "—" }}</dd>
                        <dt>Criado em</dt><dd>{{ formatarDataHora(store.detalhe.criadoEm) }}</dd>
                    </dl>
                </section>

                <!-- Dono -->
                <section class="card-secao">
                    <h2 class="card-titulo">Dono</h2>
                    <dl class="dl-grid">
                        <dt>Nome</dt><dd>{{ store.detalhe.donoNome }}</dd>
                        <dt>E-mail</dt><dd>{{ store.detalhe.donoEmail }}</dd>
                        <dt>CPF</dt>
                        <dd class="cpf-row">
                            <template v-if="store.cpfRevelado">
                                <span class="cpf-revelado">{{ store.cpfRevelado }}</span>
                                <button
                                    class="btn-icon-texto"
                                    type="button"
                                    @click="store.limparCpfRevelado()"
                                    title="Ocultar CPF"
                                >
                                    Ocultar
                                </button>
                            </template>
                            <template v-else>
                                <span class="cpf-mascarado">{{ store.detalhe.donoCpfMascarado }}</span>
                                <button
                                    class="btn-icon btn-icon-ver"
                                    type="button"
                                    @click="modalRevelarAberto = true"
                                    title="Revelar CPF completo"
                                    :disabled="store.revelandoCpf"
                                    aria-label="Revelar CPF completo do dono"
                                />
                            </template>
                        </dd>
                        <template v-if="store.erroRevelarCpf">
                            <dt></dt><dd class="erro-inline">{{ store.erroRevelarCpf }}</dd>
                        </template>
                    </dl>
                </section>

                <!-- Plano/assinatura -->
                <section class="card-secao">
                    <h2 class="card-titulo">Plano / Assinatura</h2>
                    <dl class="dl-grid">
                        <dt>Plano</dt><dd>{{ store.detalhe.planoNome }}</dd>
                        <dt>Gratuita</dt>
                        <dd>
                            <span v-if="store.detalhe.assinaturaGratuita" class="badge-gratuita">Sim</span>
                            <span v-else>Não</span>
                        </dd>
                        <dt>Fim da vigência</dt><dd>{{ formatarData(store.detalhe.assinaturaDataFim) }}</dd>
                    </dl>
                </section>

                <!-- Estatísticas (CA20: apenas contagens, sem dados de paciente) -->
                <section class="card-secao">
                    <h2 class="card-titulo">Estatísticas</h2>
                    <div class="stats-grid">
                        <div class="stat-item">
                            <span class="stat-valor">{{ store.detalhe.totalProfissionaisAtivos }}</span>
                            <span class="stat-label">Profissionais ativos</span>
                        </div>
                        <div class="stat-item">
                            <span class="stat-valor">{{ store.detalhe.totalPacientes }}</span>
                            <span class="stat-label">Pacientes cadastrados</span>
                        </div>
                        <div class="stat-item">
                            <span class="stat-valor">{{ store.detalhe.agendamentosNoMes }}</span>
                            <span class="stat-label">Agendamentos no mês</span>
                        </div>
                        <div class="stat-item">
                            <span class="stat-valor">{{ store.detalhe.totalProntuarios }}</span>
                            <span class="stat-label">Prontuários</span>
                        </div>
                    </div>
                </section>
            </div>
        </template>

        <!-- Modais -->
        <ModalRevelarCpf
            :open="modalRevelarAberto"
            :estabelecimento-id="id"
            @close="modalRevelarAberto = false"
            @revelado="(motivo) => { aoRevelar(motivo); modalRevelarAberto = false }"
        />

        <ModalResetTenant
            :open="modalResetAberto"
            :nome-fantasia="store.detalhe?.nomeFantasia ?? ''"
            :carregando="store.resetando"
            :erro="store.erroReset"
            @close="modalResetAberto = false"
            @confirmar="aoConfirmarReset"
        />
    </div>
</template>

<style scoped>
.admin-page { padding: 24px 32px; }

.breadcrumb {
    display: flex; align-items: center; gap: 6px;
    font-size: 13px; color: #6b7280; margin-bottom: 20px;
}
.breadcrumb-link {
    background: none; border: none; color: #6366f1; cursor: pointer; padding: 0; font-size: 13px;
    text-decoration: underline;
}
.breadcrumb-sep { color: #d1d5db; }

.estado-loading, .estado-erro {
    padding: 48px; text-align: center; color: #6b7280; font-size: 14px;
}
.estado-erro { color: #ef4444; }

.detalhe-header {
    display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px;
    flex-wrap: wrap; gap: 12px;
}
.detalhe-titulo-wrap { display: flex; align-items: center; gap: 12px; }
.detalhe-titulo { font-size: 22px; font-weight: 700; margin: 0; }

.btn-perigo-destaque {
    padding: 8px 18px; border: none; border-radius: 6px;
    background: #ef4444; color: #fff; font-size: 13px; font-weight: 700; cursor: pointer;
}

.abas-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 20px;
}
.card-secao {
    background: #fff; border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px;
}
.card-titulo { font-size: 14px; font-weight: 700; margin: 0 0 14px; color: #374151; }
.dl-grid { display: grid; grid-template-columns: auto 1fr; gap: 6px 16px; margin: 0; font-size: 13px; }
.dl-grid dt { font-weight: 600; color: #6b7280; }
.dl-grid dd { margin: 0; }

.cpf-row { display: flex; align-items: center; gap: 8px; }
.cpf-mascarado { font-family: monospace; font-size: 12px; color: #374151; }
.cpf-revelado { font-family: monospace; font-size: 13px; color: #059669; font-weight: 600; }
.btn-icon-texto {
    background: none; border: none; color: #6366f1; font-size: 12px; cursor: pointer; padding: 0;
    text-decoration: underline;
}
.erro-inline { color: #ef4444; font-size: 12px; }

.badge-gratuita {
    background: #d1fae5; color: #065f46; padding: 2px 8px;
    border-radius: 10px; font-size: 11px; font-weight: 600;
}

.stats-grid {
    display: grid; grid-template-columns: repeat(2, 1fr); gap: 16px;
}
.stat-item {
    display: flex; flex-direction: column; gap: 4px;
    background: #f9fafb; border-radius: 8px; padding: 14px;
}
.stat-valor { font-size: 28px; font-weight: 800; color: #1f2937; }
.stat-label { font-size: 11px; text-transform: uppercase; letter-spacing: 0.06em; color: #6b7280; font-weight: 600; }
</style>
