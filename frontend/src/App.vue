<script setup lang="ts">
import { computed } from "vue"
import { useRoute, useRouter } from "vue-router"
import AppLayout from "@/layouts/AppLayout.vue"
import { AppModal, AppButton } from "@/components/ui"
import { useUpsellStore } from "@/stores/upsellStore"

const route = useRoute()
const router = useRouter()
const usaLayout = computed(() => route.meta.layout === "app")
const upsell = useUpsellStore()

function irParaAssinatura() {
    upsell.fechar()
    router.push({ name: "MinhaAssinatura" })
}
</script>

<template>
    <AppLayout v-if="usaLayout">
        <router-view />
    </AppLayout>
    <router-view v-else />

    <!-- Modal global de upsell — disparado por qualquer 402 no httpClient -->
    <AppModal
        :aberto="upsell.visivel"
        titulo="Recurso indisponivel no seu plano"
        largura="sm"
        @fechar="upsell.fechar"
    >
        <div class="upsell-corpo">
            <i class="fa-solid fa-lock upsell-icone" aria-hidden="true"></i>
            <p class="upsell-mensagem">{{ upsell.mensagem }}</p>
            <p class="upsell-dica">Faca upgrade do seu plano para desbloquear este recurso.</p>
        </div>
        <template #rodape>
            <AppButton variant="secondary" @click="upsell.fechar">Fechar</AppButton>
            <AppButton icon="fa-solid fa-arrow-up" @click="irParaAssinatura">
                Ver planos
            </AppButton>
        </template>
    </AppModal>
</template>

<style scoped>
.upsell-corpo {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
    padding: 1rem 0;
    text-align: center;
}
.upsell-icone {
    font-size: 2.5rem;
    color: hsl(var(--warning));
}
.upsell-mensagem {
    margin: 0;
    font-size: 0.95em;
    color: hsl(var(--foreground));
    font-weight: 500;
}
.upsell-dica {
    margin: 0;
    font-size: 0.85em;
    color: hsl(var(--muted-foreground));
}
</style>
