<script setup lang="ts">
/**
 * AdminLayout.vue — shell da área administrativa.
 *
 * Inclui:
 * - Banner de identificação (vermelho em prod, amarelo em dev).
 * - Header com nome/email do admin e botão de logout.
 * - Sidebar vazia — devs paralelos adicionarão itens via router-children.
 * - Timer de inatividade: registra eventos de interação para resetar o timer.
 *
 * Isolamento: usa apenas stores e composables do módulo admin.
 */
import { onMounted, onUnmounted } from "vue"
import { useRouter } from "vue-router"
import { useAdminAuthStore } from "../stores/adminAuthStore"

const router = useRouter()
const store = useAdminAuthStore()

const isProd = import.meta.env.PROD

async function handleLogout() {
    await store.logout()
    await router.push({ name: "AdminLogin" })
}

function handleInteraction() {
    store.resetarTimerInatividade()
}

onMounted(() => {
    window.addEventListener("click", handleInteraction, { passive: true })
    window.addEventListener("keydown", handleInteraction, { passive: true })
})

onUnmounted(() => {
    window.removeEventListener("click", handleInteraction)
    window.removeEventListener("keydown", handleInteraction)
})
</script>

<template>
    <div class="admin-layout">
        <!-- Banner de identificação (CA49/CA50) -->
        <div class="admin-banner" :class="isProd ? 'admin-banner--prod' : 'admin-banner--dev'">
            <span v-if="isProd">
                Área administrativa — uso interno Imedto
            </span>
            <span v-else>
                Área administrativa (DEV) — uso interno Imedto
            </span>
        </div>

        <div class="admin-shell">
            <!-- Sidebar — extensível pelos devs de feature -->
            <nav class="admin-sidebar">
                <div class="admin-sidebar-brand">
                    <span class="admin-sidebar-logo">Imedto</span>
                    <span class="admin-sidebar-badge">Admin</span>
                </div>

                <ul class="admin-sidebar-nav">
                    <li>
                        <router-link :to="{ name: 'AdminDashboard' }" class="admin-nav-link">
                            Dashboard
                        </router-link>
                    </li>
                    <li>
                        <router-link :to="{ name: 'AdminEstabelecimentos' }" class="admin-nav-link">
                            Estabelecimentos
                        </router-link>
                    </li>
                    <li>
                        <router-link :to="{ name: 'AdminPlanos' }" class="admin-nav-link">
                            Planos
                        </router-link>
                    </li>
                    <li>
                        <router-link :to="{ name: 'AdminAdminsList' }" class="admin-nav-link">
                            Administradores
                        </router-link>
                    </li>
                </ul>
            </nav>

            <div class="admin-content">
                <!-- Header -->
                <header class="admin-header">
                    <div class="admin-header-identity">
                        <span class="admin-header-name">{{ store.admin?.nome }}</span>
                        <span class="admin-header-email">{{ store.admin?.email }}</span>
                    </div>
                    <button class="admin-logout-btn" @click="handleLogout" type="button">
                        Sair
                    </button>
                </header>

                <!-- Conteúdo da rota -->
                <main class="admin-main">
                    <router-view />
                </main>
            </div>
        </div>
    </div>
</template>

<style scoped>
.admin-layout {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    background: #0f172a;
    color: #f8fafc;
    font-family: system-ui, -apple-system, sans-serif;
}

/* Banner */
.admin-banner {
    width: 100%;
    padding: 0.5rem 1.5rem;
    text-align: center;
    font-size: 0.8125rem;
    font-weight: 600;
    letter-spacing: 0.03em;
}

.admin-banner--prod {
    background: #dc2626;
    color: white;
}

.admin-banner--dev {
    background: #ca8a04;
    color: #0f172a;
}

/* Shell (sidebar + content) */
.admin-shell {
    display: flex;
    flex: 1;
    overflow: hidden;
}

/* Sidebar */
.admin-sidebar {
    width: 240px;
    min-height: 100%;
    background: #1e293b;
    border-right: 1px solid #334155;
    display: flex;
    flex-direction: column;
    padding: 1.5rem 1rem;
    flex-shrink: 0;
}

.admin-sidebar-brand {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 2rem;
    padding: 0 0.5rem;
}

.admin-sidebar-logo {
    font-size: 1.125rem;
    font-weight: 700;
    color: #f8fafc;
}

.admin-sidebar-badge {
    background: #dc2626;
    color: white;
    font-size: 0.6rem;
    font-weight: 700;
    padding: 0.1rem 0.4rem;
    border-radius: 3px;
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.admin-sidebar-nav {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.admin-nav-link {
    display: block;
    padding: 0.625rem 0.875rem;
    border-radius: 8px;
    color: #94a3b8;
    text-decoration: none;
    font-size: 0.9rem;
    font-weight: 500;
    transition: background 0.15s, color 0.15s;
}

.admin-nav-link:hover,
.admin-nav-link.router-link-active {
    background: #334155;
    color: #f8fafc;
}

/* Content */
.admin-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
}

/* Header */
.admin-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1rem 2rem;
    background: #1e293b;
    border-bottom: 1px solid #334155;
}

.admin-header-identity {
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
}

.admin-header-name {
    font-size: 0.9375rem;
    font-weight: 600;
    color: #f8fafc;
}

.admin-header-email {
    font-size: 0.8125rem;
    color: #94a3b8;
}

.admin-logout-btn {
    background: transparent;
    border: 1px solid #475569;
    color: #94a3b8;
    border-radius: 6px;
    padding: 0.4375rem 0.875rem;
    font-size: 0.875rem;
    cursor: pointer;
    transition: border-color 0.15s, color 0.15s;
}

.admin-logout-btn:hover {
    border-color: #64748b;
    color: #f8fafc;
}

/* Main content area */
.admin-main {
    flex: 1;
    overflow: auto;
    padding: 2rem;
}
</style>
