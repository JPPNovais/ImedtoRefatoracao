<script setup lang="ts">
/**
 * AdminDashboard.vue — placeholder.
 *
 * Devs paralelos preencherão este dashboard com widgets de:
 * - Lista de estabelecimentos recentes
 * - Contadores globais
 * - Atividade recente do audit log
 *
 * Por ora, exibe mensagem de boas-vindas e o menu de navegação disponível.
 */
import { useAdminAuthStore } from "../stores/adminAuthStore"

const store = useAdminAuthStore()
</script>

<template>
    <div class="admin-dashboard">
        <h1 class="admin-dashboard-title">Bem-vindo, {{ store.admin?.nome }}</h1>
        <p class="admin-dashboard-sub">
            Área administrativa do Imedto. Toda ação é registrada em audit log.
        </p>

        <div class="admin-dashboard-cards">
            <!-- Placeholder — devs paralelos substituem por cards reais -->
            <div class="admin-placeholder-card">
                <p>Estabelecimentos, planos e admins serão listados aqui.</p>
            </div>
        </div>
    </div>
</template>

<style scoped>
.admin-dashboard {
    max-width: 1200px;
}

.admin-dashboard-title {
    font-size: 1.75rem;
    font-weight: 700;
    color: #f8fafc;
    margin: 0 0 0.5rem;
}

.admin-dashboard-sub {
    font-size: 0.9375rem;
    color: #94a3b8;
    margin: 0 0 2rem;
}

.admin-dashboard-cards {
    display: grid;
    gap: 1.5rem;
}

.admin-placeholder-card {
    background: #1e293b;
    border: 1px dashed #334155;
    border-radius: 10px;
    padding: 2rem;
    color: #64748b;
    font-size: 0.9rem;
    text-align: center;
}
</style>
