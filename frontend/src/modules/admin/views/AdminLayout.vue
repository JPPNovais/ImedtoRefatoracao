<script setup lang="ts">
/**
 * AdminLayout.vue — shell da área administrativa.
 *
 * Usa AppTopBar + AppSidebar do design system (W3-CA1, W3-CA2).
 * Isolamento total: só importa do próprio módulo admin + @/components/ui.
 * Timer de inatividade reseta a cada click/keydown (mantido da Wave 2).
 * Faixa banner "Área administrativa" e acento 2px warning são as únicas CSS custom (W3-CA18).
 */
import { onMounted, onUnmounted } from "vue"
import { useRouter } from "vue-router"
import { AppTopBar, AppSidebar, AppBadge, AppButton } from "@/components/ui"
import { useAdminAuthStore } from "../stores/adminAuthStore"

const router = useRouter()
const store = useAdminAuthStore()

const isProd = import.meta.env.PROD

const menuItems = [
    { name: "AdminDashboard",       label: "Dashboard",              icon: "fa-solid fa-gauge-high",   to: { name: "AdminDashboard" } },
    { name: "AdminEstabelecimentos", label: "Estabelecimentos",       icon: "fa-solid fa-building",     to: { name: "AdminEstabelecimentos" } },
    { name: "AdminPlanos",           label: "Planos",                 icon: "fa-solid fa-credit-card",  to: { name: "AdminPlanos" } },
    { name: "AdminAdmins",           label: "Administradores",        icon: "fa-solid fa-user-shield",  to: { name: "AdminAdminsList" } },
    { name: "AdminConfiguracoes",    label: "Configurações",          icon: "fa-solid fa-gear",         to: { name: "AdminConfiguracoes" } },
    { name: "AdminModelosGlobais",   label: "Modelos de prontuário",  icon: "fa-solid fa-file-medical", to: { name: "AdminModelosGlobais" } },
    { name: "AdminVariaveisGlobais", label: "Variáveis pool",         icon: "fa-solid fa-list-check",   to: { name: "AdminVariaveisGlobais" } },
    { name: "AdminRegioesGlobais",   label: "Regiões anatômicas",     icon: "fa-solid fa-person-rays",  to: { name: "AdminRegioesGlobais" } },
]

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
        <!-- Banner de identificação: vermelho prod, warning dev (W3-CA6) -->
        <div
            class="admin-banner"
            :class="isProd ? 'admin-banner--prod' : 'admin-banner--dev'"
            role="banner"
            aria-label="Área administrativa"
        >
            {{ isProd ? "Área administrativa — uso interno Imedto" : "Área administrativa (DEV) — uso interno Imedto" }}
        </div>

        <!-- Faixa de acento 2px warning sobre o topbar (W3-CA5) -->
        <div class="admin-topbar-wrap">
            <AppTopBar :nome-usuario="store.admin?.nome" :subtitulo-usuario="store.admin?.email">
                <template #brand>
                    <span style="font-size:1.1rem;font-weight:700;color:white;margin-right:0.5rem;">Imedto</span>
                    <AppBadge variant="error">Admin</AppBadge>
                </template>

                <template #perfil="{ fechar }">
                    <div style="padding:0.75rem 1rem;display:flex;flex-direction:column;gap:0.5rem;">
                        <span style="font-size:0.8125rem;color:hsl(var(--muted-foreground));">{{ store.admin?.email }}</span>
                        <AppButton variant="danger" @click="() => { fechar(); handleLogout() }">
                            <i class="fa-solid fa-arrow-right-from-bracket" aria-hidden="true"></i>
                            Sair
                        </AppButton>
                    </div>
                </template>
            </AppTopBar>
        </div>

        <div class="admin-shell">
            <!-- Sidebar DS (W3-CA1) -->
            <AppSidebar :items="menuItems" />

            <!-- Área de conteúdo — cada view monta seu próprio app-page -->
            <main class="admin-main">
                <router-view />
            </main>
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
}

/* ── Banner (única CSS custom além do acento) ── */
.admin-banner {
    width: 100%;
    padding: 0.35rem 1.5rem;
    text-align: center;
    font-size: 0.7rem;
    font-weight: 700;
    letter-spacing: 0.05em;
    text-transform: uppercase;
    flex-shrink: 0;
    position: relative;
    z-index: 60;
}
.admin-banner--prod {
    background: hsl(var(--destructive));
    color: hsl(var(--destructive-foreground));
}
.admin-banner--dev {
    background: hsl(var(--warning));
    color: hsl(var(--warning-foreground, var(--foreground)));
}

/* ── Faixa de acento 2px warning sobre o AppTopBar (W3-CA5) ── */
.admin-topbar-wrap {
    border-top: 2px solid hsl(var(--warning));
    position: sticky;
    top: 0;
    z-index: 50;
}

/* ── Shell (sidebar + conteúdo) ── */
.admin-shell {
    display: flex;
    flex: 1;
    padding-top: 64px; /* altura do topbar */
}

/* ── Main — recebe app-page de cada view ── */
.admin-main {
    flex: 1;
    min-width: 0;
    padding-left: 64px; /* largura collapsed do AppSidebar */
    overflow-y: auto;
}
</style>
