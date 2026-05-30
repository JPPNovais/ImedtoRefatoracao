<script setup lang="ts">
/**
 * EstabelecimentoDetalheView — detalhe completo do estabelecimento.
 *
 * W3-CA7 a W3-CA12, W3-CA16, W3-CA17: app-page + AppPageHeader + AppCard
 *   + AppButton + AppStatusPill + AppModal.
 * CA17–CA19: CPF mascarado por padrão; ModalRevelarCpf abre via AppModal.
 * CA32–CA36: reset com confirmação dupla via ModalResetTenant (AppModal).
 * W2-CA1 a W2-CA4: AssinaturaCard integrado.
 */
import { ref, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { AppPageHeader, AppCard, AppButton, AppStatusPill, AppBadge } from "@/components/ui"
import { useEstabelecimentosStore } from "../stores/estabelecimentosStore"
import ModalRevelarCpf from "../components/estabelecimentos/ModalRevelarCpf.vue"
import ModalResetTenant from "../components/estabelecimentos/ModalResetTenant.vue"
import AssinaturaCard from "../components/assinaturas/AssinaturaCard.vue"

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

function statusVariante(status: string): "success" | "error" | "warning" | "muted" {
    if (status === "Ativo") return "success"
    if (status === "Suspenso") return "warning"
    return "muted"
}
</script>

<template>
    <main class="app-page">
        <!-- Breadcrumb simples -->
        <nav class="breadcrumb" aria-label="Caminho de navegação">
            <button class="breadcrumb-link" type="button" @click="router.push({ name: 'AdminEstabelecimentos' })">
                Estabelecimentos
            </button>
            <span class="breadcrumb-sep" aria-hidden="true">/</span>
            <span>{{ store.detalhe?.nomeFantasia ?? "Detalhe" }}</span>
        </nav>

        <!-- Loading -->
        <AppCard v-if="store.carregandoDetalhe">
            <p class="estado-info"><i class="fa-solid fa-spinner fa-spin"></i> Carregando...</p>
        </AppCard>

        <!-- Não encontrado -->
        <AppCard v-else-if="store.erroDetalhe">
            <p class="estado-erro" role="alert">{{ store.erroDetalhe }}</p>
        </AppCard>

        <template v-else-if="store.detalhe">
            <AppPageHeader :titulo="store.detalhe.nomeFantasia">
                <template #acoes>
                    <AppStatusPill :label="store.detalhe.status" :variante="statusVariante(store.detalhe.status)" />
                    <AppButton variant="danger" @click="modalResetAberto = true">
                        <i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i>
                        Resetar dados
                    </AppButton>
                </template>
            </AppPageHeader>

            <!-- Grid de cards de informação -->
            <div class="detalhe-grid">
                <!-- Identificação -->
                <AppCard title="Identificação">
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
                </AppCard>

                <!-- Dono -->
                <AppCard title="Dono">
                    <dl class="dl-grid">
                        <dt>Nome</dt><dd>{{ store.detalhe.donoNome }}</dd>
                        <dt>E-mail</dt><dd>{{ store.detalhe.donoEmail }}</dd>
                        <dt>CPF</dt>
                        <dd class="cpf-row">
                            <template v-if="store.cpfRevelado">
                                <span class="cpf-revelado">{{ store.cpfRevelado }}</span>
                                <AppButton variant="ghost" size="sm" @click="store.limparCpfRevelado()">
                                    Ocultar
                                </AppButton>
                            </template>
                            <template v-else>
                                <span class="cpf-mascarado">{{ store.detalhe.donoCpfMascarado }}</span>
                                <button
                                    class="btn-icon btn-icon-ver"
                                    type="button"
                                    title="Revelar CPF completo"
                                    :disabled="store.revelandoCpf"
                                    aria-label="Revelar CPF completo do dono"
                                    @click="modalRevelarAberto = true"
                                />
                            </template>
                        </dd>
                        <template v-if="store.erroRevelarCpf">
                            <dt></dt>
                            <dd class="campo-erro">{{ store.erroRevelarCpf }}</dd>
                        </template>
                    </dl>
                </AppCard>

                <!-- Plano / Assinatura sumário -->
                <AppCard title="Plano / Assinatura">
                    <dl class="dl-grid">
                        <dt>Plano</dt><dd>{{ store.detalhe.planoNome }}</dd>
                        <dt>Gratuita</dt>
                        <dd>
                            <AppBadge v-if="store.detalhe.assinaturaGratuita" variant="success">Sim</AppBadge>
                            <span v-else>Não</span>
                        </dd>
                        <dt>Fim da vigência</dt><dd>{{ formatarData(store.detalhe.assinaturaDataFim) }}</dd>
                    </dl>
                </AppCard>

                <!-- Estatísticas (CA20: apenas contagens, sem dados de paciente) -->
                <AppCard title="Estatísticas">
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
                </AppCard>
            </div>

            <!-- Card de assinatura completo com histórico e ações (W2-CA1 a W2-CA4) -->
            <AssinaturaCard :estabelecimento-id="id" style="margin-top:1.5rem;" />
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
    </main>
</template>

<style scoped>
.breadcrumb {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    font-size: 0.8125rem;
    color: hsl(var(--muted-foreground));
    margin-bottom: 1rem;
}

.breadcrumb-link {
    background: none;
    border: none;
    color: hsl(var(--primary));
    cursor: pointer;
    padding: 0;
    font-size: 0.8125rem;
    text-decoration: underline;
}

.breadcrumb-sep { color: hsl(var(--border)); }

.estado-info {
    text-align: center;
    color: hsl(var(--muted-foreground));
    padding: 2rem 0;
    font-size: 0.875rem;
}

.estado-erro {
    color: hsl(var(--destructive));
    padding: 2rem 0;
    text-align: center;
    font-size: 0.875rem;
}

.detalhe-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 1.25rem;
}

.dl-grid {
    display: grid;
    grid-template-columns: auto 1fr;
    gap: 0.375rem 1rem;
    margin: 0;
    font-size: 0.875rem;
}

.dl-grid dt {
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    white-space: nowrap;
}

.dl-grid dd { margin: 0; }

.cpf-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.cpf-mascarado {
    font-family: monospace;
    font-size: 0.8rem;
}

.cpf-revelado {
    font-family: monospace;
    font-size: 0.875rem;
    color: hsl(142 60% 28%);
    font-weight: 600;
}

.campo-erro {
    color: hsl(var(--destructive));
    font-size: 0.75rem;
}

.stats-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 1rem;
}

.stat-item {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    background: hsl(var(--muted) / 0.5);
    border-radius: calc(var(--radius) - 2px);
    padding: 0.875rem;
}

.stat-valor {
    font-size: 1.75rem;
    font-weight: 800;
    color: hsl(var(--foreground));
}

.stat-label {
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: hsl(var(--muted-foreground));
    font-weight: 600;
}
</style>
