<script setup lang="ts">
/**
 * Tab de pedidos de compra.
 * A API atual não oferece endpoints de pedidos de compra — esta tab exibe um
 * estado placeholder informativo. Gap documentado no resumo final.
 */
import { AppEmptyState, AppButton } from "@/components/ui"
</script>

<template>
    <div class="compras-tab">
        <AppEmptyState
            icone="fa-solid fa-truck-fast"
            titulo="Pedidos de compra"
            descricao="O módulo de pedidos de compra ainda não está disponível nesta versão. Em breve você poderá criar, acompanhar e receber pedidos de fornecedores diretamente aqui."
        >
            <template #acao>
                <AppButton
                    variant="secondary"
                    icon="fa-solid fa-bell"
                    disabled
                >
                    Disponível em breve
                </AppButton>
            </template>
        </AppEmptyState>
    </div>
</template>

<style scoped>
.compras-tab {
    padding: 40px 0;
}
</style>
