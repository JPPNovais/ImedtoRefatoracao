<script setup lang="ts">
import { computed } from "vue"
import { useRouter } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import { useAssinaturaStore } from "@/stores/assinaturaStore"
import { AppButton } from "@/components/ui"

/**
 * Tela de bloqueio para assinatura inativa (trial expirado / suspensa / cancelada / expirada).
 *
 * O router guard global redireciona o usuário para cá quando `assinaturaStore.isBlocked` é true.
 * Daqui o usuário escolhe: ver planos, sair, ou contatar suporte.
 *
 * Espelha `SubscriptionExpired.vue` do legado, adaptado para o design system novo.
 */
const router = useRouter()
const auth = useAuthStore()
const assinatura = useAssinaturaStore()

const mensagem = computed(() => {
    switch (assinatura.statusEfetivo) {
        case "Expirada":
            return {
                titulo: "Seu período de avaliação encerrou",
                descricao:
                    "Seu período de experiência chegou ao fim. Contrate um plano para continuar usando o Imedto.",
            }
        case "Suspensa":
            return {
                titulo: "Pagamento pendente",
                descricao:
                    "Identificamos um problema com o pagamento da sua assinatura. Entre em contato para regularizar.",
            }
        case "Cancelada":
            return {
                titulo: "Assinatura cancelada",
                descricao: "Sua assinatura foi cancelada. Reative para continuar usando o Imedto.",
            }
        default:
            return {
                titulo: "Assinatura inativa",
                descricao:
                    "Sua assinatura não está ativa no momento. Contrate um plano para continuar usando o Imedto.",
            }
    }
})

function verPlanos() {
    router.push({ name: "Planos" })
}

async function sair() {
    await auth.logout()
    router.push({ name: "Login" })
}
</script>

<template>
    <div class="tela-expirada">
        <div class="caixa">
            <div class="icone">
                <i class="fa-solid fa-triangle-exclamation" />
            </div>

            <h1 class="titulo">{{ mensagem.titulo }}</h1>
            <p class="descricao">{{ mensagem.descricao }}</p>

            <div class="acoes">
                <AppButton size="lg" icon="fa-solid fa-credit-card" @click="verPlanos">
                    Ver planos disponíveis
                </AppButton>
                <AppButton variant="ghost" size="sm" @click="sair">
                    Sair da conta
                </AppButton>
            </div>
        </div>
    </div>
</template>

<style scoped>
.tela-expirada {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 1.5rem;
    background: var(--bg-page, var(--bg-card));
}

.caixa {
    width: 100%;
    max-width: 28rem;
    text-align: center;
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 2.25rem 1.75rem;
    box-shadow: var(--shadow-sm, 0 1px 3px rgba(0, 0, 0, 0.05));
}

.icone {
    width: 3.5rem;
    height: 3.5rem;
    margin: 0 auto 1.25rem;
    border-radius: 1rem;
    display: flex;
    align-items: center;
    justify-content: center;
    background: hsl(var(--warning) / 0.12);
    color: hsl(var(--warning));
}

.icone i {
    font-size: 1.5rem;
}

.titulo {
    font-size: 1.15em;
    font-weight: 700;
    color: var(--text);
    margin: 0 0 0.5rem;
}

.descricao {
    font-size: 0.88em;
    color: var(--text-muted);
    line-height: 1.5;
    margin: 0 0 1.5rem;
}

.acoes {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}
</style>
