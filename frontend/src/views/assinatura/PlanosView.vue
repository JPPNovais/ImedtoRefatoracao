<script setup lang="ts">
import { ref, onMounted } from "vue"
import { assinaturaService, type Plano } from "@/services/assinaturaService"
import { AppPageHeader, AppCard, AppButton, AppEmptyState } from "@/components/ui"

const planos = ref<Plano[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)

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
        planos.value = await assinaturaService.listarPlanos()
    } catch {
        erro.value = "Nao foi possivel carregar os planos."
    } finally {
        carregando.value = false
    }
}

function formatarPreco(preco: number) {
    if (preco === 0) return "Gratuito"
    return preco.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) + "/mês"
}

onMounted(carregar)
</script>

<template>
    <main class="app-page">
        <AppPageHeader titulo="Planos" subtitulo="Compare os planos e escolha o melhor para sua clínica." />

        <div v-if="carregando" class="carregando">
            <i class="fa-solid fa-spinner fa-spin"></i>
            Carregando planos...
        </div>

        <div v-else-if="erro" class="erro-banner">
            <i class="fa-solid fa-triangle-exclamation"></i>
            {{ erro }}
            <AppButton variant="ghost" size="sm" @click="carregar">Tentar novamente</AppButton>
        </div>

        <AppEmptyState
            v-else-if="planos.length === 0"
            icone="fa-solid fa-rectangle-list"
            titulo="Nenhum plano disponível"
            descricao="Entre em contato com o suporte para mais informações."
        />

        <div v-else class="planos-grid">
            <AppCard
                v-for="plano in planos"
                :key="plano.id"
                :elevated="plano.nome === 'Pro'"
            >
                <div class="plano-conteudo">
                    <div class="plano-header">
                        <h3 class="plano-nome">{{ plano.nome }}</h3>
                        <p class="plano-preco">{{ formatarPreco(plano.precoMensal) }}</p>
                    </div>

                    <div class="plano-limites">
                        <span v-if="plano.limiteProfissionais !== null" class="limite">
                            <i class="fa-solid fa-user-doctor" aria-hidden="true"></i>
                            Ate {{ plano.limiteProfissionais }} profissional{{ plano.limiteProfissionais !== 1 ? "is" : "" }}
                        </span>
                        <span v-else class="limite">
                            <i class="fa-solid fa-user-doctor" aria-hidden="true"></i>
                            Profissionais ilimitados
                        </span>
                        <span v-if="plano.limitePacientes !== null" class="limite">
                            <i class="fa-solid fa-people-group" aria-hidden="true"></i>
                            Ate {{ plano.limitePacientes }} pacientes
                        </span>
                        <span v-else class="limite">
                            <i class="fa-solid fa-people-group" aria-hidden="true"></i>
                            Pacientes ilimitados
                        </span>
                    </div>

                    <ul v-if="plano.featuresJson.length > 0" class="features-lista">
                        <li v-for="f in plano.featuresJson" :key="f" class="feature-item">
                            <i class="fa-solid fa-circle-check" aria-hidden="true"></i>
                            <span>{{ labelFeature[f] ?? f }}</span>
                        </li>
                    </ul>
                    <p v-else class="sem-features">Recursos basicos incluídos.</p>

                    <div class="plano-footer">
                        <p class="plano-nota">
                            Para contratar ou trocar de plano, entre em contato com o suporte.
                        </p>
                    </div>
                </div>
            </AppCard>
        </div>
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

.planos-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
    gap: 1.25rem;
}

.plano-conteudo {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    height: 100%;
}

.plano-header {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.plano-nome {
    font-size: 1.1rem;
    font-weight: 700;
    margin: 0;
    color: hsl(var(--primary));
}

.plano-preco {
    font-size: 1.5rem;
    font-weight: 800;
    margin: 0;
    color: hsl(var(--foreground));
}

.plano-limites {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
}

.limite {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.85em;
    color: hsl(var(--muted-foreground));
}

.features-lista {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    flex: 1;
}

.feature-item {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.85em;
    color: hsl(var(--foreground));
}

.feature-item i { color: hsl(var(--success)); }

.sem-features {
    font-size: 0.85em;
    color: hsl(var(--muted-foreground));
    margin: 0;
    flex: 1;
}

.plano-footer {
    border-top: 1px solid hsl(var(--border));
    padding-top: 0.75rem;
}

.plano-nota {
    font-size: 0.78em;
    color: hsl(var(--muted-foreground));
    margin: 0;
    text-align: center;
}
</style>
