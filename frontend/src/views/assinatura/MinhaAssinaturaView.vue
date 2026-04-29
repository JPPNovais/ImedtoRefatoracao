<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRouter } from "vue-router"
import { assinaturaService, type MinhaAssinatura } from "@/services/assinaturaService"
import { AppPageHeader, AppCard, AppBadge, AppButton, AppEmptyState } from "@/components/ui"

const router = useRouter()
const assinatura = ref<MinhaAssinatura | null>(null)
const carregando = ref(false)
const erro = ref<string | null>(null)

const statusTexto: Record<string, string> = {
    Trial: "Trial",
    Ativa: "Ativo",
    Suspensa: "Suspenso",
    Cancelada: "Cancelado",
    Expirada: "Expirado",
}

const diasRestantesTexto = computed(() => {
    if (!assinatura.value?.diasRestantes) return null
    const d = assinatura.value.diasRestantes
    if (d <= 0) return "Expirado"
    if (d === 1) return "1 dia restante"
    return `${d} dias restantes`
})

const alertaDias = computed(() => {
    const d = assinatura.value?.diasRestantes ?? null
    if (d === null) return false
    return d <= 5
})

const labelFeature: Record<string, string> = {
    receitas: "Receitas medicas",
    exame_fisico: "Exame fisico",
    ia: "Assistente de IA",
    procedimentos_cirurgicos: "Procedimentos cirurgicos",
    orcamento_completo: "Orcamento completo",
    automacoes: "Automacoes",
    relatorios: "Relatorios avancados",
}

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        assinatura.value = await assinaturaService.obterMinha()
    } catch {
        erro.value = "Nao foi possivel carregar sua assinatura."
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader titulo="Minha assinatura" subtitulo="Status atual do seu plano e recursos disponíveis.">
            <template #acoes>
                <AppButton
                    variant="secondary"
                    icon="fa-solid fa-arrow-up"
                    @click="router.push({ name: 'Planos' })"
                >
                    Ver planos
                </AppButton>
            </template>
        </AppPageHeader>

        <div v-if="carregando" class="carregando">
            <i class="fa-solid fa-spinner fa-spin"></i>
            Carregando...
        </div>

        <div v-else-if="erro" class="erro-banner">
            <i class="fa-solid fa-triangle-exclamation"></i>
            {{ erro }}
            <AppButton variant="ghost" size="sm" @click="carregar">Tentar novamente</AppButton>
        </div>

        <template v-else-if="assinatura">
            <!-- Alerta de trial expirando -->
            <div v-if="alertaDias" class="alerta-trial">
                <i class="fa-solid fa-clock" aria-hidden="true"></i>
                <span>
                    Seu trial expira em breve.
                    <strong>{{ diasRestantesTexto }}</strong>.
                    Escolha um plano para continuar usando todos os recursos.
                </span>
                <AppButton size="sm" icon="fa-solid fa-arrow-up" @click="router.push({ name: 'Planos' })">
                    Fazer upgrade
                </AppButton>
            </div>

            <!-- Card status -->
            <AppCard title="Status da assinatura">
                <div class="status-grid">
                    <div class="status-item">
                        <span class="status-label">Plano</span>
                        <strong class="status-valor">{{ assinatura.planoNome }}</strong>
                    </div>
                    <div class="status-item">
                        <span class="status-label">Status</span>
                        <AppBadge :status="assinatura.status" :label="statusTexto[assinatura.status] ?? assinatura.status" />
                    </div>
                    <div v-if="assinatura.expiraEm" class="status-item">
                        <span class="status-label">Validade</span>
                        <span class="status-valor" :class="{ 'status-alerta': alertaDias }">
                            {{ new Date(assinatura.expiraEm).toLocaleDateString("pt-BR") }}
                            <span v-if="diasRestantesTexto" class="status-dias">
                                ({{ diasRestantesTexto }})
                            </span>
                        </span>
                    </div>
                    <div v-if="assinatura.limiteProfissionais !== null" class="status-item">
                        <span class="status-label">Limite de profissionais</span>
                        <strong class="status-valor">{{ assinatura.limiteProfissionais }}</strong>
                    </div>
                    <div v-if="assinatura.limitePacientes !== null" class="status-item">
                        <span class="status-label">Limite de pacientes</span>
                        <strong class="status-valor">{{ assinatura.limitePacientes }}</strong>
                    </div>
                </div>
            </AppCard>

            <!-- Features disponíveis -->
            <AppCard title="Recursos incluídos">
                <AppEmptyState
                    v-if="assinatura.features.length === 0"
                    icone="fa-solid fa-lock"
                    titulo="Nenhum recurso premium"
                    descricao="Seu plano atual não inclui recursos premium. Escolha um plano para desbloquear."
                    compacto
                />
                <ul v-else class="features-lista">
                    <li v-for="f in assinatura.features" :key="f" class="feature-item">
                        <i class="fa-solid fa-circle-check feature-check" aria-hidden="true"></i>
                        <span>{{ labelFeature[f] ?? f }}</span>
                    </li>
                </ul>
            </AppCard>
        </template>
    </main>
</template>

<style scoped>
.carregando {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: hsl(var(--muted-foreground));
    padding: 2rem 0;
    font-size: 0.9em;
}

.erro-banner {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    font-size: 0.9em;
}

.alerta-trial {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: hsl(var(--warning) / 0.12);
    border: 1px solid hsl(var(--warning) / 0.3);
    border-radius: var(--radius);
    color: hsl(45 70% 30%);
    font-size: 0.88em;
    flex-wrap: wrap;
    margin-bottom: 1rem;
}

.status-grid {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.status-item {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid hsl(var(--border));
}
.status-item:last-child {
    border-bottom: none;
}

.status-label {
    font-size: 0.85em;
    color: hsl(var(--muted-foreground));
    min-width: 160px;
    flex-shrink: 0;
}

.status-valor {
    font-size: 0.9em;
    color: hsl(var(--foreground));
}

.status-alerta { color: hsl(var(--warning)); font-weight: 600; }
.status-dias { font-size: 0.85em; color: hsl(var(--muted-foreground)); margin-left: 0.25rem; }

.features-lista {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}

.feature-item {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    font-size: 0.9em;
    color: hsl(var(--foreground));
}

.feature-check { color: hsl(var(--success)); }
</style>
