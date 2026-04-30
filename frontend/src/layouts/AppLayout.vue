<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from "vue"
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
const userMenuAberto = ref(false)
const userMenuEl = ref<HTMLElement | null>(null)

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
    OrcamentoDetalhe: "Orcamentos",
    OrcamentoForm: "Orcamentos",
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

function fecharUserMenuFora(ev: MouseEvent) {
    if (!userMenuEl.value) return
    if (!userMenuEl.value.contains(ev.target as Node)) userMenuAberto.value = false
}

function irParaConta() {
    userMenuAberto.value = false
    router.push({ name: "MinhaConta" })
}

function trocarEstabelecimentoFromMenu() {
    userMenuAberto.value = false
    trocarEstabelecimento()
}

async function sairFromMenu() {
    userMenuAberto.value = false
    await sair()
}

onMounted(() => document.addEventListener("click", fecharUserMenuFora))
onBeforeUnmount(() => document.removeEventListener("click", fecharUserMenuFora))
</script>

<template>
    <div class="layout">
        <div v-if="sidebarAberta" class="overlay" @click="fecharSidebar" />

        <aside class="sidebar" :class="{ aberta: sidebarAberta }">
            <router-link :to="{ name: 'Home' }" class="sidebar-logo" @click="fecharSidebar">
                <img :src="logoBranco" alt="Imedto" class="logo-img" />
            </router-link>

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
                    v-if="tenant.ativo?.papel === 'Dono'"
                    :to="{ name: 'OrcamentoSettings' }"
                    class="nav-item"
                    :class="{ 'nav-item--ativo': route.name === 'OrcamentoSettings' }"
                    @click="fecharSidebar"
                >
                    <span class="nav-icon"><i class="fa-solid fa-file-invoice-dollar" aria-hidden="true"></i></span>
                    <span>Config. orçamento</span>
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

                <div class="topbar-contexto" v-if="tenant.ativo">
                    <i class="fa-solid fa-building topbar-contexto-icone" aria-hidden="true"></i>
                    <span class="topbar-contexto-nome">{{ tenant.ativo.nomeFantasia }}</span>
                </div>

                <div class="topbar-acoes">
                    <AppHeaderNotifications />

                    <div ref="userMenuEl" class="user-menu">
                        <button
                            type="button"
                            class="user-menu-trigger"
                            :aria-expanded="userMenuAberto"
                            aria-haspopup="menu"
                            @click="userMenuAberto = !userMenuAberto"
                        >
                            <div class="user-menu-avatar">
                                <img
                                    v-if="profissional.fotoUrl"
                                    :src="profissional.fotoUrl"
                                    :alt="auth.usuario?.nomeCompleto ?? 'Foto do profissional'"
                                />
                                <template v-else>{{ userInicial }}</template>
                            </div>
                            <span class="user-menu-nome">
                                {{ auth.usuario?.nomeCompleto ?? auth.usuario?.email ?? "Profissional" }}
                            </span>
                            <i class="fa-solid fa-chevron-down user-menu-chevron" aria-hidden="true"></i>
                        </button>

                        <div v-if="userMenuAberto" class="user-menu-dropdown" role="menu">
                            <div class="user-menu-header">
                                <div class="user-menu-avatar user-menu-avatar--lg">
                                    <img
                                        v-if="profissional.fotoUrl"
                                        :src="profissional.fotoUrl"
                                        :alt="auth.usuario?.nomeCompleto ?? 'Foto do profissional'"
                                    />
                                    <template v-else>{{ userInicial }}</template>
                                </div>
                                <div class="user-menu-info">
                                    <span class="user-menu-info-nome">
                                        {{ auth.usuario?.nomeCompleto ?? auth.usuario?.email ?? "Profissional" }}
                                    </span>
                                    <span class="user-menu-info-email" v-if="auth.usuario?.email">
                                        {{ auth.usuario.email }}
                                    </span>
                                    <span class="user-menu-info-papel" v-if="tenant.ativo">
                                        {{ tenant.ativo.papel }} · {{ tenant.ativo.nomeFantasia }}
                                    </span>
                                </div>
                            </div>

                            <div class="user-menu-divider"></div>

                            <button type="button" class="user-menu-item" @click="irParaConta">
                                <i class="fa-solid fa-user user-menu-item-icone" aria-hidden="true"></i>
                                Minha conta
                            </button>
                            <button type="button" class="user-menu-item" @click="trocarEstabelecimentoFromMenu">
                                <i class="fa-solid fa-arrow-right-arrow-left user-menu-item-icone" aria-hidden="true"></i>
                                Trocar estabelecimento
                            </button>

                            <div class="user-menu-divider"></div>

                            <button type="button" class="user-menu-item user-menu-item--danger" @click="sairFromMenu">
                                <i class="fa-solid fa-right-from-bracket user-menu-item-icone" aria-hidden="true"></i>
                                Sair
                            </button>
                        </div>
                    </div>
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
    padding: 0.5rem 1.25rem;
    height: 56px;
    background: var(--bg-card);
    border-bottom: 1px solid hsl(var(--primary) / 0.12);
    box-shadow: 0 2px 4px rgba(36, 21, 84, 0.06), 0 8px 24px rgba(36, 21, 84, 0.04);
    position: sticky;
    top: 0;
    z-index: 50;
}

/* Contexto (estabelecimento) à esquerda */
.topbar-contexto {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.3rem 0.65rem;
    background: var(--bg-muted);
    border: 1px solid var(--border);
    border-radius: 999px;
    font-size: 0.78em;
    font-weight: 600;
    color: var(--text);
    max-width: 260px;
}
.topbar-contexto-icone {
    color: hsl(var(--primary));
    font-size: 0.85em;
}
.topbar-contexto-nome {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.topbar-acoes {
    margin-left: auto;
    display: flex;
    align-items: center;
    gap: 0.35rem;
}

/* User menu */
.user-menu { position: relative; }

.user-menu-trigger {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.25rem 0.55rem 0.25rem 0.3rem;
    background: transparent;
    border: 1px solid transparent;
    border-radius: 999px;
    cursor: pointer;
    transition: background 0.15s, border-color 0.15s;
    color: var(--text);
    font-family: inherit;
}
.user-menu-trigger:hover {
    background: var(--bg-muted);
    border-color: var(--border);
}

.user-menu-avatar {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary));
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.85em;
    font-weight: 700;
    flex-shrink: 0;
    overflow: hidden;
}
.user-menu-avatar img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}
.user-menu-avatar--lg {
    width: 44px;
    height: 44px;
    font-size: 1.05em;
}

.user-menu-nome {
    font-size: 0.82em;
    font-weight: 600;
    color: var(--text);
    max-width: 180px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.user-menu-chevron {
    font-size: 0.7em;
    color: var(--text-muted);
}

.user-menu-dropdown {
    position: absolute;
    top: calc(100% + 8px);
    right: 0;
    width: 280px;
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 12px;
    box-shadow: 0 12px 32px rgba(0, 0, 0, 0.18);
    z-index: 200;
    padding: 0.4rem;
    display: flex;
    flex-direction: column;
}

.user-menu-header {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    padding: 0.55rem 0.6rem;
}
.user-menu-info {
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 1px;
}
.user-menu-info-nome {
    font-size: 0.85em;
    font-weight: 600;
    color: var(--text);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.user-menu-info-email {
    font-size: 0.72em;
    color: var(--text-muted);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.user-menu-info-papel {
    font-size: 0.7em;
    color: hsl(var(--primary));
    font-weight: 600;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.user-menu-divider {
    height: 1px;
    background: var(--border);
    margin: 0.25rem 0;
}

.user-menu-item {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    width: 100%;
    padding: 0.5rem 0.6rem;
    background: transparent;
    border: none;
    border-radius: 8px;
    cursor: pointer;
    font-family: inherit;
    font-size: 0.83em;
    color: var(--text);
    text-align: left;
    transition: background 0.12s;
}
.user-menu-item:hover { background: var(--bg-muted); }
.user-menu-item-icone {
    width: 16px;
    text-align: center;
    color: var(--text-muted);
    font-size: 0.85em;
}
.user-menu-item--danger { color: hsl(0 70% 45%); }
.user-menu-item--danger .user-menu-item-icone { color: hsl(0 70% 45%); }
.user-menu-item--danger:hover { background: hsl(0 70% 95%); }

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

    .user-menu-nome,
    .user-menu-chevron { display: none; }
    .topbar-contexto { max-width: 160px; }
}
</style>
