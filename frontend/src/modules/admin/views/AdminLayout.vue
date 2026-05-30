<script setup lang="ts">
/**
 * AdminLayout.vue — shell da área administrativa.
 *
 * Isolamento total: só importa do próprio módulo admin + vue/vue-router.
 * Timer de inatividade reseta a cada click/keydown.
 * Faixa de acento 2px warning no topo do header (distinção visual da área admin — W2-CA32).
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
        <!-- Faixa de identificação: vermelho prod, warning dev -->
        <div
            class="admin-banner"
            :class="isProd ? 'admin-banner--prod' : 'admin-banner--dev'"
            role="banner"
            aria-label="Área administrativa"
        >
            {{ isProd ? "Área administrativa — uso interno Imedto" : "Área administrativa (DEV) — uso interno Imedto" }}
        </div>

        <div class="admin-shell">
            <!-- Sidebar -->
            <nav class="admin-sidebar" aria-label="Menu administrativo">
                <div class="admin-sidebar-brand">
                    <span class="admin-sidebar-logo">Imedto</span>
                    <span class="admin-sidebar-badge">Admin</span>
                </div>

                <ul class="admin-sidebar-nav" role="list">
                    <li>
                        <router-link :to="{ name: 'AdminDashboard' }" class="admin-nav-link">
                            <i class="fa-solid fa-gauge-high nav-icon" aria-hidden="true"></i>
                            <span class="nav-label">Dashboard</span>
                        </router-link>
                    </li>
                    <li>
                        <router-link :to="{ name: 'AdminEstabelecimentos' }" class="admin-nav-link">
                            <i class="fa-solid fa-building nav-icon" aria-hidden="true"></i>
                            <span class="nav-label">Estabelecimentos</span>
                        </router-link>
                    </li>
                    <li>
                        <router-link :to="{ name: 'AdminPlanos' }" class="admin-nav-link">
                            <i class="fa-solid fa-credit-card nav-icon" aria-hidden="true"></i>
                            <span class="nav-label">Planos</span>
                        </router-link>
                    </li>
                    <li>
                        <router-link :to="{ name: 'AdminAdminsList' }" class="admin-nav-link">
                            <i class="fa-solid fa-user-shield nav-icon" aria-hidden="true"></i>
                            <span class="nav-label">Administradores</span>
                        </router-link>
                    </li>
                    <li>
                        <router-link :to="{ name: 'AdminConfiguracoes' }" class="admin-nav-link">
                            <i class="fa-solid fa-gear nav-icon" aria-hidden="true"></i>
                            <span class="nav-label">Configurações</span>
                        </router-link>
                    </li>

                    <li class="nav-secao" aria-hidden="true">Catálogos</li>
                    <li>
                        <router-link :to="{ name: 'AdminModelosGlobais' }" class="admin-nav-link">
                            <i class="fa-solid fa-file-medical nav-icon" aria-hidden="true"></i>
                            <span class="nav-label">Modelos de prontuário</span>
                        </router-link>
                    </li>
                    <li>
                        <router-link :to="{ name: 'AdminVariaveisGlobais' }" class="admin-nav-link">
                            <i class="fa-solid fa-list-check nav-icon" aria-hidden="true"></i>
                            <span class="nav-label">Variáveis pool</span>
                        </router-link>
                    </li>
                    <li>
                        <router-link :to="{ name: 'AdminRegioesGlobais' }" class="admin-nav-link">
                            <i class="fa-solid fa-person-rays nav-icon" aria-hidden="true"></i>
                            <span class="nav-label">Regiões anatômicas</span>
                        </router-link>
                    </li>
                </ul>
            </nav>

            <!-- Área de conteúdo -->
            <div class="admin-content">
                <!-- Topbar com faixa de acento 2px (W2-CA33) -->
                <header class="admin-header">
                    <div class="admin-header-identity">
                        <span class="admin-header-name">{{ store.admin?.nome }}</span>
                        <span class="admin-header-email">{{ store.admin?.email }}</span>
                    </div>
                    <button class="admin-logout-btn" type="button" @click="handleLogout">
                        <i class="fa-solid fa-arrow-right-from-bracket" aria-hidden="true"></i>
                        Sair
                    </button>
                </header>

                <!-- Rota renderizada -->
                <main class="admin-main">
                    <router-view />
                </main>
            </div>
        </div>
    </div>
</template>

<style scoped>
/* ── Layout raiz ── */
.admin-layout {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    background: hsl(var(--background));
    color: hsl(var(--foreground));
    font-family: system-ui, -apple-system, sans-serif;
}

/* ── Faixa de identificação ── */
.admin-banner {
    width: 100%;
    padding: 0.4rem 1.5rem;
    text-align: center;
    font-size: 0.75rem;
    font-weight: 700;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    flex-shrink: 0;
}

.admin-banner--prod {
    background: hsl(var(--destructive));
    color: hsl(var(--destructive-foreground));
}

.admin-banner--dev {
    background: hsl(var(--warning));
    color: hsl(var(--warning-foreground, var(--foreground)));
}

/* ── Shell (sidebar + content) ── */
.admin-shell {
    display: flex;
    flex: 1;
    overflow: hidden;
}

/* ── Sidebar ── */
.admin-sidebar {
    width: 248px;
    min-height: 100%;
    background: hsl(var(--card));
    border-right: 1px solid hsl(var(--border));
    display: flex;
    flex-direction: column;
    padding: 1.25rem 0.75rem;
    flex-shrink: 0;
    gap: 0.25rem;
}

.admin-sidebar-brand {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 1.5rem;
    padding: 0 0.5rem;
}

.admin-sidebar-logo {
    font-size: 1.125rem;
    font-weight: 700;
    color: hsl(var(--foreground));
}

/* Badge "Admin" — acento visual para distinguir da área normal (W2-CA31) */
.admin-sidebar-badge {
    background: hsl(var(--destructive));
    color: hsl(var(--destructive-foreground));
    font-size: 0.6rem;
    font-weight: 800;
    padding: 0.1rem 0.45rem;
    border-radius: 3px;
    text-transform: uppercase;
    letter-spacing: 0.06em;
}

.admin-sidebar-nav {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
}

/* Seção de categoria (Catálogos) */
.nav-secao {
    font-size: 0.65rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: hsl(var(--muted-foreground));
    padding: 1rem 0.75rem 0.2rem;
    user-select: none;
}

.nav-icon {
    width: 16px;
    text-align: center;
    font-size: 0.8rem;
    flex-shrink: 0;
}

.admin-nav-link {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    padding: 0.55rem 0.75rem;
    border-radius: 6px;
    color: hsl(var(--muted-foreground));
    text-decoration: none;
    font-size: 0.875rem;
    font-weight: 500;
    transition: background 0.12s, color 0.12s;
}

.admin-nav-link:hover {
    background: hsl(var(--muted));
    color: hsl(var(--foreground));
}

.admin-nav-link.router-link-active {
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary));
    font-weight: 600;
}

/* ── Área de conteúdo ── */
.admin-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
}

/* ── Topbar ── */
/* Faixa de acento 2px warning no topo (W2-CA33 — sinal discreto de área admin) */
.admin-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.875rem 1.75rem;
    background: hsl(var(--card));
    border-bottom: 1px solid hsl(var(--border));
    border-top: 2px solid hsl(var(--warning));
    flex-shrink: 0;
}

.admin-header-identity {
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
}

.admin-header-name {
    font-size: 0.875rem;
    font-weight: 600;
    color: hsl(var(--foreground));
}

.admin-header-email {
    font-size: 0.75rem;
    color: hsl(var(--muted-foreground));
}

.admin-logout-btn {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    background: transparent;
    border: 1px solid hsl(var(--border));
    color: hsl(var(--muted-foreground));
    border-radius: 6px;
    padding: 0.375rem 0.75rem;
    font-size: 0.8125rem;
    cursor: pointer;
    transition: border-color 0.12s, color 0.12s, background 0.12s;
}

.admin-logout-btn:hover {
    border-color: hsl(var(--destructive));
    color: hsl(var(--destructive));
    background: hsl(var(--destructive) / 0.06);
}

/* ── Main ── */
.admin-main {
    flex: 1;
    overflow: auto;
}
</style>
