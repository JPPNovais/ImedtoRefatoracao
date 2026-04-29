<script setup lang="ts">
import { ref, computed } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { useProfissionalStore } from "@/stores/profissionalStore"
import AppHeaderNotifications from "@/components/notifications/AppHeaderNotifications.vue"
import logoBranco from "@/assets/imedto-logo-branco.png"

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const tenant = useTenantStore()
const profissional = useProfissionalStore()

const sidebarAberta = ref(false)

const navItemsBase = [
    { name: "Home",            label: "Painel inicial",   icon: "fa-solid fa-house",                  donoOnly: false },
    { name: "Agenda",          label: "Agendamentos",     icon: "fa-solid fa-calendar-days",          donoOnly: false },
    { name: "MinhasConsultas", label: "Minhas consultas", icon: "fa-solid fa-stethoscope",            donoOnly: false },
    { name: "Pacientes",       label: "Pacientes",        icon: "fa-solid fa-people-group",           donoOnly: false },
    { name: "Profissionais",   label: "Profissionais",    icon: "fa-solid fa-user-doctor",            donoOnly: true  },
    { name: "Financeiro",      label: "Financeiro",       icon: "fa-solid fa-chart-line",             donoOnly: false },
    { name: "Orcamentos",      label: "Orçamentos",       icon: "fa-solid fa-file-invoice-dollar",    donoOnly: false },
    { name: "Inventario",      label: "Estoque",          icon: "fa-solid fa-boxes-stacked",          donoOnly: false },
    { name: "Relatorios",      label: "Relatórios",       icon: "fa-solid fa-chart-pie",              donoOnly: false },
    { name: "Automacoes",      label: "Automação",        icon: "fa-solid fa-bolt",                   donoOnly: false },
]

const navItems = computed(() =>
    navItemsBase.filter(i => !i.donoOnly || tenant.papel === "Dono"),
)

const activeNavMap: Record<string, string> = {
    Home: "Home",
    Agenda: "Agenda",
    MinhasConsultas: "MinhasConsultas",
    Pacientes: "Pacientes",
    PacienteDetalhe: "Pacientes",
    Prontuario: "Pacientes",
    Profissionais: "Profissionais",
    Financeiro: "Financeiro",
    CategoriasFinanceiras: "Financeiro",
    FormasPagamento: "Financeiro",
    Orcamentos: "Orcamentos",
    Inventario: "Inventario",
    Relatorios: "Relatorios",
    Automacoes: "Automacoes",
    Notificacoes: "Notificacoes",
}

const activeNav = computed(() => activeNavMap[route.name as string] ?? null)
const userInicial = computed(() => {
    const n = auth.usuario?.nomeCompleto ?? auth.usuario?.email ?? ""
    return n.charAt(0).toUpperCase()
})

function fecharSidebar() { sidebarAberta.value = false }

function trocarEstabelecimento() {
    tenant.limpar()
    router.push({ name: "SelecionarEstabelecimento" })
}

async function sair() {
    await auth.logout()
    router.push({ name: "Login" })
}
</script>

<template>
    <div class="layout">
        <div v-if="sidebarAberta" class="overlay" @click="fecharSidebar" />

        <aside class="sidebar" :class="{ aberta: sidebarAberta }">
            <router-link :to="{ name: 'Home' }" class="sidebar-logo" @click="fecharSidebar">
                <img :src="logoBranco" alt="Imedto" class="logo-img" />
            </router-link>

            <div class="user-bloco">
                <router-link :to="{ name: 'MinhaConta' }" class="user-info" @click="fecharSidebar">
                    <div class="avatar">
                        <img v-if="profissional.fotoUrl" :src="profissional.fotoUrl" :alt="auth.usuario?.nomeCompleto ?? 'Foto do profissional'" />
                        <template v-else>{{ userInicial }}</template>
                    </div>
                    <div class="user-texto">
                        <span class="user-nome">{{ auth.usuario?.nomeCompleto ?? auth.usuario?.email ?? "Profissional" }}</span>
                        <span class="user-clinica" v-if="tenant.ativo">{{ tenant.ativo.nomeFantasia }}</span>
                        <span class="user-papel" v-if="tenant.ativo">{{ tenant.ativo.papel }}</span>
                    </div>
                </router-link>
            </div>

            <nav class="nav">
                <router-link
                    v-for="item in navItems"
                    :key="item.name"
                    :to="{ name: item.name }"
                    class="nav-item"
                    :class="{ 'nav-item--ativo': activeNav === item.name }"
                    @click="fecharSidebar"
                >
                    <span class="nav-icon"><i :class="item.icon" aria-hidden="true"></i></span>
                    <span>{{ item.label }}</span>
                </router-link>
            </nav>

            <div class="nav-footer">
                <div class="nav-divider"></div>

                <router-link
                    v-if="tenant.ativo?.papel === 'Dono'"
                    :to="{ name: 'Estabelecimento' }"
                    class="nav-item"
                    :class="{ 'nav-item--ativo': route.name === 'Estabelecimento' }"
                    @click="fecharSidebar"
                >
                    <span class="nav-icon"><i class="fa-solid fa-building" aria-hidden="true"></i></span>
                    <span>Estabelecimento</span>
                </router-link>

                <router-link
                    v-if="tenant.ativo?.papel === 'Dono'"
                    :to="{ name: 'ModelosPermissao' }"
                    class="nav-item"
                    :class="{ 'nav-item--ativo': route.name === 'ModelosPermissao' }"
                    @click="fecharSidebar"
                >
                    <span class="nav-icon"><i class="fa-solid fa-user-group" aria-hidden="true"></i></span>
                    <span>Permissões</span>
                </router-link>

                <router-link
                    v-if="tenant.ativo?.papel === 'Dono'"
                    :to="{ name: 'IaSettings' }"
                    class="nav-item"
                    :class="{ 'nav-item--ativo': route.name === 'IaSettings' }"
                    @click="fecharSidebar"
                >
                    <span class="nav-icon"><i class="fa-solid fa-robot" aria-hidden="true"></i></span>
                    <span>Config. IA</span>
                </router-link>

                <router-link
                    :to="{ name: 'MinhaAssinatura' }"
                    class="nav-item"
                    :class="{ 'nav-item--ativo': route.name === 'MinhaAssinatura' || route.name === 'Planos' }"
                    @click="fecharSidebar"
                >
                    <span class="nav-icon"><i class="fa-solid fa-star" aria-hidden="true"></i></span>
                    <span>Assinatura</span>
                </router-link>

                <router-link
                    :to="{ name: 'MeusConvites' }"
                    class="nav-item"
                    :class="{ 'nav-item--ativo': route.name === 'MeusConvites' }"
                    @click="fecharSidebar"
                >
                    <span class="nav-icon"><i class="fa-solid fa-envelope" aria-hidden="true"></i></span>
                    <span>Meus convites</span>
                </router-link>

                <button class="nav-item" @click="trocarEstabelecimento">
                    <span class="nav-icon"><i class="fa-solid fa-arrow-right-arrow-left" aria-hidden="true"></i></span>
                    <span>Trocar estabelecimento</span>
                </button>

                <button class="nav-item nav-item--sair" @click="sair">
                    <span class="nav-icon"><i class="fa-solid fa-right-from-bracket" aria-hidden="true"></i></span>
                    <span>Sair</span>
                </button>
            </div>
        </aside>

        <div class="conteudo">
            <header class="topbar">
                <button class="btn-menu" @click="sidebarAberta = !sidebarAberta" aria-label="Menu">
                    <span></span><span></span><span></span>
                </button>
                <span class="topbar-titulo">{{ tenant.ativo?.nomeFantasia ?? "Imedto" }}</span>
                <div class="topbar-acoes">
                    <AppHeaderNotifications />
                </div>
            </header>

            <main class="pagina">
                <slot />
            </main>
        </div>
    </div>
</template>

<style scoped>
.layout {
    display: flex;
    min-height: 100vh;
}

/* Sidebar */
.sidebar {
    width: var(--sidebar-w);
    flex-shrink: 0;
    background: linear-gradient(to bottom, #241554, #452b97, #241554);
    color: #ffffff;
    display: flex;
    flex-direction: column;
    position: fixed;
    top: 0;
    left: 0;
    bottom: 0;
    z-index: 100;
    overflow-y: auto;
    scrollbar-width: none;
}
.sidebar::-webkit-scrollbar { display: none; }

.sidebar-logo {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 1.5rem 1rem 1.25rem;
    cursor: pointer;
    transition: opacity 0.15s;
}
.sidebar-logo:hover { opacity: 0.8; }
.logo-img { height: 28px; width: auto; object-fit: contain; }

/* User block */
.user-bloco {
    padding: 0 0.75rem 0.75rem;
}
.user-info {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    padding: 0.5rem 0.5rem;
    border-radius: 10px;
    cursor: pointer;
    transition: background 0.15s;
    text-decoration: none;
    color: inherit;
}
.user-info:hover { background: rgba(255,255,255,0.08); }
.avatar {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    border: 2px solid rgba(255,255,255,0.6);
    background: rgba(255,255,255,0.15);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.1rem;
    font-weight: 700;
    flex-shrink: 0;
    overflow: hidden;
}
.avatar img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}
.user-texto { display: flex; flex-direction: column; min-width: 0; }
.user-nome {
    font-size: 0.82em;
    font-weight: 600;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    color: #fff;
}
.user-clinica {
    font-size: 0.72em;
    color: #86efac;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.user-papel { font-size: 0.7em; color: rgba(255,255,255,0.55); }

/* Nav */
.nav {
    flex: 1;
    padding: 0.25rem 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.nav-footer {
    padding: 0 0.75rem 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.nav-divider {
    height: 1px;
    background: rgba(255,255,255,0.1);
    margin: 0.5rem 0.25rem 0.5rem;
}

.nav-item {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    width: 100%;
    padding: 0.5rem 0.75rem;
    font-size: 0.85em;
    font-weight: 500;
    color: rgba(255,255,255,0.75);
    text-decoration: none;
    border: none;
    background: none;
    text-align: left;
    cursor: pointer;
    border-radius: 8px;
    transition: background 0.12s, color 0.12s;
}
.nav-item:hover {
    background: rgba(255,255,255,0.1);
    color: #fff;
}
.nav-item--ativo {
    background: rgba(255,255,255,0.15);
    color: #fff;
    font-weight: 600;
}
.nav-icon {
    width: 20px;
    text-align: center;
    flex-shrink: 0;
    font-size: 0.9em;
}
.nav-item--sair { color: rgba(248, 113, 113, 0.85); }
.nav-item--sair:hover { color: #fca5a5; background: rgba(248,113,113,0.1); }

/* Content */
.conteudo {
    margin-left: var(--sidebar-w);
    flex: 1;
    display: flex;
    flex-direction: column;
    min-height: 100vh;
    min-width: 0;
}

.topbar {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.5rem 1rem;
    background: var(--bg-card);
    border-bottom: 1px solid var(--border);
    position: sticky;
    top: 0;
    z-index: 50;
}
.topbar-titulo { font-size: 0.9em; font-weight: 600; color: var(--text); }
.topbar-acoes {
    margin-left: auto;
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.btn-menu {
    display: none;
    flex-direction: column;
    gap: 4px;
    background: none;
    border: none;
    cursor: pointer;
    padding: 4px;
}
.btn-menu span {
    display: block;
    width: 20px;
    height: 2px;
    background: var(--text);
    border-radius: 2px;
}

.pagina { flex: 1; }

.overlay {
    display: none;
    position: fixed;
    inset: 0;
    background: rgba(0,0,0,0.5);
    z-index: 99;
}

@media (max-width: 768px) {
    .sidebar {
        transform: translateX(-100%);
        transition: transform 0.22s ease;
    }
    .sidebar.aberta { transform: translateX(0); }
    .conteudo { margin-left: 0; }
    .btn-menu { display: flex; }
    .overlay { display: block; }
}
</style>
