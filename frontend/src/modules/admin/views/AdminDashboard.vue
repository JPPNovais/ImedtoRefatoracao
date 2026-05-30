<script setup lang="ts">
/**
 * AdminDashboard.vue — placeholder de boas-vindas.
 *
 * W3-CA7, W3-CA8, W3-CA9: app-page + AppPageHeader + AppCard.
 * Devs paralelos substituirão os placeholders por widgets reais.
 */
import { AppPageHeader, AppCard } from "@/components/ui"
import { useAdminAuthStore } from "../stores/adminAuthStore"

const store = useAdminAuthStore()
</script>

<template>
    <main class="app-page">
        <AppPageHeader
            :titulo="`Bem-vindo, ${store.admin?.nome ?? 'Administrador'}`"
            subtitulo="Área administrativa do Imedto. Toda ação é registrada em audit log."
        />

        <AppCard>
            <p style="color:hsl(var(--muted-foreground));font-size:0.9rem;text-align:center;padding:1rem 0;">
                Estabelecimentos, planos e admins serão listados aqui.
            </p>
        </AppCard>
    </main>
</template>
