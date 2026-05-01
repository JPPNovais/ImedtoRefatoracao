<script setup lang="ts">
/**
 * AppLayout — layout global com TopBar (gradient roxo) + Sidebar colapsável.
 * Estrutura inspirada no design Anthropic (handoff Agenda.html).
 *
 * Composição:
 *   - <AppTopBar> com brand, contexto (tenant), notificações e perfil
 *   - <AppSidebar> com items roteáveis e footer (Dono only + utilitários)
 *   - <main> com `padding-top: var(--topbar-h)` e margem esquerda dinâmica
 *     baseada no estado pin do sidebar (controlado por `body.has-pinned-sidebar`)
 */
import { computed } from "vue"
import { useRouter, useRoute } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { useProfissionalStore } from "@/stores/profissionalStore"
import { useNotificacoesStore } from "@/stores/notificacoesStore"
import { AppTopBar, AppSidebar } from "@/components/ui"
import logoBranco from "@/assets/imedto-logo-branco.png"

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const tenant = useTenantStore()
const profissional = useProfissionalStore()
const notificacoes = useNotificacoesStore()

const navMain = computed(() => {
    const ehDono = tenant.papel === "Dono"
    const items = [
        { name: "Home",            label: "Painel inicial",   icon: "fa-solid fa-house",                  to: { name: "Home" } },
        { name: "Agenda",          label: "Agendamentos",     icon: "fa-solid fa-calendar-days",          to: { name: "Agenda" } },
        { name: "MinhasConsultas", label: "Minhas consultas", icon: "fa-solid fa-stethoscope",            to: { name: "MinhasConsultas" } },
        { name: "Pacientes",       label: "Pacientes",        icon: "fa-solid fa-people-group",           to: { name: "Pacientes" } },
        ...(ehDono ? [{ name: "Profissionais", label: "Profissionais", icon: "fa-solid fa-user-doctor", to: { name: "Profissionais" } }] : []),
        { name: "Financeiro",      label: "Financeiro",       icon: "fa-solid fa-chart-line",             to: { name: "Financeiro" } },
        { name: "Orcamentos",      label: "Orçamentos",       icon: "fa-solid fa-file-invoice-dollar",    to: { name: "Orcamentos" } },
        { name: "Inventario",      label: "Estoque",          icon: "fa-solid fa-boxes-stacked",          to: { name: "Inventario" } },
        { name: "Relatorios",      label: "Relatórios",       icon: "fa-solid fa-chart-pie",              to: { name: "Relatorios" } },
        { name: "Automacoes",      label: "Automação",        icon: "fa-solid fa-bolt",                   to: { name: "Automacoes" } },
    ]
    return items
})

// Mapa de rotas filhas → item de nav ativo (ex: PacienteDetalhe → Pacientes).
const activeMap: Record<string, string> = {
    PacienteDetalhe: "Pacientes",
    Prontuario: "Pacientes",
    CategoriasFinanceiras: "Financeiro",
    FormasPagamento: "Financeiro",
    OrcamentoDetalhe: "Orcamentos",
    OrcamentoForm: "Orcamentos",
    OrcamentoSettings: "Orcamentos",
}

// Itens do footer da sidebar (Dono only + perfil/conta).
const navFooter = computed(() => {
    const ehDono = tenant.papel === "Dono"
    const items: Array<{ name?: string; label: string; icon: string; to?: { name: string }; onClick?: () => void; danger?: boolean }> = []
    if (ehDono) {
        items.push({ name: "Estabelecimento",   label: "Estabelecimento",  icon: "fa-solid fa-building",   to: { name: "Estabelecimento" } })
        items.push({ name: "ModelosPermissao",  label: "Permissões",       icon: "fa-solid fa-user-group", to: { name: "ModelosPermissao" } })
        items.push({ name: "IaSettings",        label: "Config. IA",       icon: "fa-solid fa-robot",      to: { name: "IaSettings" } })
        items.push({ name: "OrcamentoSettings", label: "Config. orçamento", icon: "fa-solid fa-file-invoice-dollar", to: { name: "OrcamentoSettings" } })
    }
    items.push({ name: "MinhaAssinatura", label: "Assinatura",   icon: "fa-solid fa-star",     to: { name: "MinhaAssinatura" } })
    items.push({ name: "MeusConvites",    label: "Meus convites", icon: "fa-solid fa-envelope", to: { name: "MeusConvites" } })
    return items
})

const userInicial = computed(() => {
    const n = auth.usuario?.nomeCompleto ?? auth.usuario?.email ?? ""
    return n.charAt(0).toUpperCase() || "?"
})

const subtituloUsuario = computed(() => {
    if (tenant.ativo) return `${tenant.ativo.papel} · ${tenant.ativo.nomeFantasia}`
    return ""
})

function trocarEstabelecimento() {
    tenant.limpar()
    router.push({ name: "SelecionarEstabelecimento" })
}

async function sair() {
    await auth.logout()
    router.push({ name: "Login" })
}

function irMinhaConta() {
    router.push({ name: "MinhaConta" })
}

function irNotificacoes() {
    router.push({ name: "Notificacoes" })
}
</script>

<template>
    <AppTopBar
        :nome-usuario="auth.usuario?.nomeCompleto ?? auth.usuario?.email ?? 'Profissional'"
        :subtitulo-usuario="subtituloUsuario"
        :inicial-usuario="userInicial"
        :foto-url="profissional.fotoUrl"
        :contador-notificacoes="notificacoes.naoLidas"
    >
        <template #brand>
            <router-link :to="{ name: 'Home' }" class="brand-link">
                <img :src="logoBranco" alt="Imedto" class="brand-logo" />
            </router-link>
        </template>

        <template #notificacoes="{ fechar }">
            <div class="pop-head">
                <b>Notificações</b>
                <router-link :to="{ name: 'Notificacoes' }" class="lnk" @click="fechar">Ver todas</router-link>
            </div>
            <div class="pop-list">
                <template v-if="notificacoes.notificacoes.length > 0">
                    <button
                        v-for="n in notificacoes.notificacoes.slice(0, 5)"
                        :key="n.id"
                        type="button"
                        class="notif-item"
                        :class="{ 'nao-lida': !n.lida }"
                        @click="() => { fechar(); irNotificacoes() }"
                    >
                        <strong>{{ n.titulo }}</strong>
                        <span>{{ n.mensagem }}</span>
                    </button>
                </template>
                <div v-else class="vazio">Sem notificações.</div>
            </div>
        </template>

        <template #perfil="{ fechar }">
            <div class="perfil-card">
                <div class="av-lg">
                    <img v-if="profissional.fotoUrl" :src="profissional.fotoUrl" alt="Foto" />
                    <template v-else>{{ userInicial }}</template>
                </div>
                <div class="perfil-info">
                    <b>{{ auth.usuario?.nomeCompleto ?? auth.usuario?.email }}</b>
                    <span v-if="auth.usuario?.email">{{ auth.usuario.email }}</span>
                    <span v-if="tenant.ativo" class="clinic">● {{ tenant.ativo.nomeFantasia }}</span>
                </div>
            </div>
            <div class="pop-list">
                <button type="button" class="pop-item" @click="() => { fechar(); irMinhaConta() }">
                    <i class="fa-solid fa-user" aria-hidden="true"></i>Minha conta
                </button>
                <button type="button" class="pop-item" @click="() => { fechar(); trocarEstabelecimento() }">
                    <i class="fa-solid fa-arrow-right-arrow-left" aria-hidden="true"></i>Trocar estabelecimento
                </button>
            </div>
            <div class="pop-foot">
                <button type="button" class="logout" @click="() => { fechar(); sair() }">
                    <i class="fa-solid fa-arrow-right-from-bracket" aria-hidden="true"></i>Sair
                </button>
            </div>
        </template>
    </AppTopBar>

    <AppSidebar :items="navMain" :active-map="activeMap">
        <template #footer="{ expanded }">
            <router-link
                v-for="item in navFooter"
                :key="item.name"
                :to="item.to!"
                class="foot-item"
                :class="{ active: route.name === item.name }"
                :title="!expanded ? item.label : ''"
            >
                <i :class="item.icon" aria-hidden="true"></i>
                <span class="lbl">{{ item.label }}</span>
            </router-link>

            <button
                type="button"
                class="foot-item"
                :title="!expanded ? 'Trocar estabelecimento' : ''"
                @click="trocarEstabelecimento"
            >
                <i class="fa-solid fa-arrow-right-arrow-left" aria-hidden="true"></i>
                <span class="lbl">Trocar estabelecimento</span>
            </button>

            <button
                type="button"
                class="foot-item danger"
                :title="!expanded ? 'Sair' : ''"
                @click="sair"
            >
                <i class="fa-solid fa-arrow-right-from-bracket" aria-hidden="true"></i>
                <span class="lbl">Sair</span>
            </button>
        </template>
    </AppSidebar>

    <main class="conteudo">
        <slot />
    </main>
</template>

<style scoped>
:global(:root) {
    --topbar-h: 64px;
    --sidebar-w-collapsed: 64px;
    --sidebar-w-expanded: 240px;
}
:global(body) {
    background: hsl(var(--primary-light, 240 33% 99%));
}

.brand-link {
    display: flex;
    align-items: center;
    text-decoration: none;
}
.brand-logo { height: 26px; width: auto; }

/* Conteúdo principal */
.conteudo {
    padding-top: var(--topbar-h);
    margin-left: var(--sidebar-w-collapsed);
    transition: margin-left 220ms cubic-bezier(.2,.8,.2,1);
    min-height: 100vh;
}
:global(body.has-pinned-sidebar) .conteudo {
    margin-left: var(--sidebar-w-expanded);
}

/* Popovers (notificações + perfil) */
.pop-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 12px 14px;
    border-bottom: 1px solid hsl(0 0% 0% / 0.08);
    font-size: 13px;
}
.pop-head b { color: hsl(var(--primary-dark, 254 56% 21%)); }
.lnk {
    font-size: 11px;
    color: hsl(var(--primary, 254 56% 38%));
    cursor: pointer;
    font-weight: 600;
    text-decoration: none;
}
.lnk:hover { text-decoration: underline; }

.pop-list {
    padding: 6px;
    display: flex;
    flex-direction: column;
    gap: 1px;
    max-height: 360px;
    overflow-y: auto;
}
.notif-item {
    text-align: left;
    background: transparent;
    border: 0;
    padding: 8px 10px;
    border-radius: 6px;
    cursor: pointer;
    font-family: inherit;
    display: flex;
    flex-direction: column;
    gap: 2px;
}
.notif-item:hover { background: hsl(0 0% 0% / 0.04); }
.notif-item strong { font-size: 12px; color: hsl(var(--primary-dark, 254 56% 21%)); font-weight: 600; }
.notif-item span { font-size: 11px; color: hsl(0 0% 0% / 0.65); }
.notif-item.nao-lida strong { color: hsl(var(--primary, 254 56% 38%)); }
.vazio {
    padding: 1.5rem;
    text-align: center;
    color: hsl(0 0% 0% / 0.5);
    font-size: 13px;
}

.perfil-card {
    display: flex;
    gap: 12px;
    padding: 14px;
    border-bottom: 1px solid hsl(0 0% 0% / 0.08);
}
.av-lg {
    width: 48px;
    height: 48px;
    border-radius: 50%;
    background: linear-gradient(135deg, hsl(var(--primary, 254 56% 38%)) 0%, hsl(var(--primary-dark, 254 56% 21%)) 100%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 16px;
    font-weight: 700;
    flex-shrink: 0;
    overflow: hidden;
}
.av-lg img { width: 100%; height: 100%; object-fit: cover; }
.perfil-info b {
    display: block;
    font-size: 13px;
    color: hsl(var(--primary-dark, 254 56% 21%));
    margin-bottom: 2px;
}
.perfil-info span {
    display: block;
    font-size: 11px;
    color: hsl(0 0% 0% / 0.7);
    line-height: 1.4;
}
.perfil-info .clinic { color: hsl(160 79% 35%); margin-top: 2px; }

.pop-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 8px 10px;
    border-radius: 6px;
    font-size: 12px;
    color: hsl(var(--primary-dark, 254 56% 21%));
    cursor: pointer;
    background: transparent;
    border: 0;
    text-align: left;
    width: 100%;
    font-family: inherit;
}
.pop-item i {
    width: 14px;
    color: hsl(0 0% 0% / 0.6);
    font-size: 12px;
}
.pop-item:hover { background: hsl(0 0% 0% / 0.05); }

.pop-foot {
    padding: 10px 14px;
    border-top: 1px solid hsl(0 0% 0% / 0.08);
}
.logout {
    width: 100%;
    padding: 8px;
    border: none;
    background: hsl(0 84% 60% / 0.08);
    color: hsl(0 84% 60%);
    border-radius: 6px;
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    font-family: inherit;
}
.logout:hover { background: hsl(0 84% 60% / 0.14); }

/* Footer items da sidebar (sobrescreve estilo do AppSidebar via slot) */
.foot-item {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 8px 12px;
    border-radius: 8px;
    font-size: 12px;
    font-weight: 500;
    color: hsl(0 0% 0% / 0.7);
    text-decoration: none;
    border: 0;
    background: transparent;
    cursor: pointer;
    text-align: left;
    width: 100%;
    font-family: inherit;
    white-space: nowrap;
}
.foot-item i { width: 18px; text-align: center; font-size: 13px; flex-shrink: 0; }
.foot-item .lbl {
    flex: 1;
    opacity: 0;
    transition: opacity 160ms;
    pointer-events: none;
}
:global(.side.expanded) .foot-item .lbl {
    opacity: 1;
    pointer-events: auto;
}
.foot-item:hover {
    color: hsl(var(--primary-dark, 254 56% 21%));
    background: hsl(0 0% 0% / 0.06);
}
.foot-item.active {
    background: hsl(var(--primary, 254 56% 38%) / 0.12);
    color: hsl(var(--primary-dark, 254 56% 21%));
    font-weight: 600;
}
.foot-item.danger { color: hsl(0 70% 50%); }
.foot-item.danger:hover { background: hsl(0 70% 50% / 0.1); color: hsl(0 70% 45%); }

@media (max-width: 768px) {
    .conteudo { margin-left: 0; }
}
</style>
