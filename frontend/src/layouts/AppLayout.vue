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
import { computed, ref } from "vue"
import { useRouter, useRoute } from "vue-router"
import { useAuthStore } from "@/stores/authStore"
import { useTenantStore } from "@/stores/tenantStore"
import { usePermissoesStore } from "@/stores/permissoesStore"
import { useProfissionalStore } from "@/stores/profissionalStore"
import { useNotificacoesStore } from "@/stores/notificacoesStore"
import { useTheme, type Theme } from "@/composables/useTheme"
import { AppTopBar, AppSidebar } from "@/components/ui"
import EstabelecimentoSeletorModal from "@/components/ui/EstabelecimentoSeletorModal.vue"
import { podeAcessarRota } from "@/router/routePermissions"
import { usuarioService } from "@/services/usuarioService"
import logoBranco from "@/assets/imedto-logo-branco.png"

const { tema, definirTema } = useTheme()

const TEMAS: { v: Theme; label: string; icon: string }[] = [
    { v: "light", label: "Claro",  icon: "fa-solid fa-sun" },
    { v: "dark",  label: "Escuro", icon: "fa-solid fa-moon" },
    { v: "auto",  label: "Auto",   icon: "fa-solid fa-circle-half-stroke" },
]

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const tenant = useTenantStore()
const permissoes = usePermissoesStore()
const profissional = useProfissionalStore()
const notificacoes = useNotificacoesStore()

// Catálogo do menu principal — a permissão de cada item segue o que está
// declarado em `router/routePermissions.ts`. Mantém menu e router guard em
// sincronia: se um Profissional não pode abrir /equipe, ele também não vê
// "Equipe" no nav. Home aparece sempre (é a landing do tenant ativo).
const ITENS_MENU: { name: string; label: string; icon: string; to: { name: string } }[] = [
    { name: "Home",            label: "Painel inicial",   icon: "fa-solid fa-house",                  to: { name: "Home" } },
    { name: "Agenda",          label: "Agendamentos",     icon: "fa-solid fa-calendar-days",          to: { name: "Agenda" } },
    { name: "MinhasConsultas", label: "Minhas consultas", icon: "fa-solid fa-stethoscope",            to: { name: "MinhasConsultas" } },
    { name: "Pacientes",       label: "Pacientes",        icon: "fa-solid fa-people-group",           to: { name: "Pacientes" } },
    { name: "Equipe",          label: "Equipe",           icon: "fa-solid fa-user-doctor",            to: { name: "Equipe" } },
    { name: "Financeiro",      label: "Financeiro",       icon: "fa-solid fa-chart-line",             to: { name: "Financeiro" } },
    { name: "Orcamentos",      label: "Orçamentos",       icon: "fa-solid fa-file-invoice-dollar",    to: { name: "Orcamentos" } },
    { name: "Inventario",      label: "Estoque",          icon: "fa-solid fa-boxes-stacked",          to: { name: "Inventario" } },
    { name: "Relatorios",      label: "Relatórios",       icon: "fa-solid fa-chart-pie",              to: { name: "Relatorios" } },
]

const navMain = computed(() => {
    // Sem vínculo: sidebar vazia. Acesso a perfil/convites é via dropdown da topbar.
    if (tenant.semEstabelecimento || !tenant.ativo) return []

    const helpers = {
        ehDono: permissoes.ehDono,
        pode: (k: string) => permissoes.pode(k),
        podeExtra: (k: string) => permissoes.podeExtra(k),
    }
    return ITENS_MENU.filter(i => podeAcessarRota(i.name, helpers))
})

// Configurações no footer aponta sempre para /estabelecimento (master-detail unificado).
// Mostrar enquanto qualquer das seções de configuração estiver acessível.
// IaSettings/ModelosProntuario/TermosModelos são agora redirects — checamos
// a permissão pelo gate real (Estabelecimento já abarca Dono + config_estabelecimento).
const ROTAS_CONFIG = ["Estabelecimento", "IaSettings", "ModelosProntuario", "TermosModelos"] as const
const configDestino = computed(() => {
    const helpers = {
        ehDono: permissoes.ehDono,
        pode: (k: string) => permissoes.pode(k),
        podeExtra: (k: string) => permissoes.podeExtra(k),
    }
    return ROTAS_CONFIG.find(n => podeAcessarRota(n, helpers)) ? "Estabelecimento" : null
})
const podeVerConfig = computed(() => configDestino.value !== null)

const brandTo = computed(() =>
    tenant.semEstabelecimento ? { name: "MeusConvites" } : { name: "Home" }
)

// Mapa de rotas filhas → item de nav ativo (ex: PacienteDetalhe → Pacientes).
const activeMap: Record<string, string> = {
    PacienteDetalhe: "Pacientes",
    Prontuario: "Pacientes",
    CategoriasFinanceiras: "Financeiro",
    FormasPagamento: "Financeiro",
    OrcamentoDetalhe: "Orcamentos",
    OrcamentoForm: "Orcamentos",
    OrcamentoSettings: "Orcamentos",
    // Permissões e convites pertencem ao escopo Equipe (rotas legadas redirecionam).
    Profissionais: "Equipe",
    ModelosPermissao: "Equipe",
    MeusConvites: "Equipe",
}

// "Configurações" no footer — ativo quando a rota ativa é Estabelecimento (master-detail)
// ou qualquer das rotas que agora redirecionam para lá. Planos mantém como
// sub-rota autônoma de assinatura.
const configuracoesAtiva = computed(() => {
    const sub = ["Estabelecimento", "IaSettings", "MinhaAssinatura", "ModelosProntuario", "TermosModelos", "TermosNovo", "TermosEditar", "Automacoes"]
    return sub.includes(route.name as string)
})

const userInicial = computed(() => {
    const n = auth.usuario?.nomeCompleto ?? auth.usuario?.email ?? ""
    return n.charAt(0).toUpperCase() || "?"
})

const subtituloUsuario = computed(() => {
    if (tenant.ativo) return `${tenant.ativo.papel} · ${tenant.ativo.nomeFantasia}`
    return ""
})

const modalSeletorAberto = ref(false)

// R3: item de menu visível somente quando há 2+ estabelecimentos acessíveis.
const temMultiEstabelecimento = computed(() => tenant.estabelecimentos.length > 1)

function trocarEstabelecimento() {
    modalSeletorAberto.value = true
}

async function confirmarTrocaEstabelecimento(novoId: number) {
    // R7: falha na gravação não bloqueia a troca.
    await usuarioService.gravarUltimoEstabelecimento(novoId).catch(() => {
        // Falha silenciosa — sem erro bloqueante para o usuário.
    })
    tenant.selecionar(
        tenant.estabelecimentos.find(e => e.id === novoId)
            ? {
                id: novoId,
                nomeFantasia: tenant.estabelecimentos.find(e => e.id === novoId)!.nomeFantasia,
                papel: tenant.estabelecimentos.find(e => e.id === novoId)!.papelDoUsuario,
                permissoes: tenant.estabelecimentos.find(e => e.id === novoId)!.permissoes ?? [],
                permissoesExtras: tenant.estabelecimentos.find(e => e.id === novoId)!.permissoesExtras ?? [],
            }
            : tenant.ativo!,
    )
    // Reload completo para reidratar tudo do zero via /auth/bootstrap (CA11).
    window.location.href = "/home"
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

async function sincronizarNotificacoes() {
    await Promise.all([
        notificacoes.carregar({ tamanho: 5, lidas: false }),
        notificacoes.atualizarContador(),
    ])
}
</script>

<template>
    <AppTopBar
        :nome-usuario="auth.usuario?.nomeCompleto ?? auth.usuario?.email ?? 'Profissional'"
        :subtitulo-usuario="subtituloUsuario"
        :inicial-usuario="userInicial"
        :foto-url="profissional.fotoUrl"
        :contador-notificacoes="notificacoes.naoLidas"
        @abrir-notificacoes="sincronizarNotificacoes"
    >
        <template #brand>
            <router-link :to="brandTo" class="brand-link">
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
                <button type="button" class="pop-item" @click="() => { fechar(); router.push({ name: 'MeusConvites' }) }">
                    <i class="fa-solid fa-envelope" aria-hidden="true"></i>Meus convites
                </button>
                <button
                    v-if="temMultiEstabelecimento"
                    type="button"
                    class="pop-item"
                    @click="() => { fechar(); trocarEstabelecimento() }"
                >
                    <i class="fa-solid fa-arrow-right-arrow-left" aria-hidden="true"></i>Trocar estabelecimento
                </button>

                <div class="pop-tema">
                    <span class="pop-tema-label">Tema</span>
                    <div class="pop-tema-row" role="radiogroup" aria-label="Tema">
                        <button
                            v-for="t in TEMAS"
                            :key="t.v"
                            type="button"
                            class="pop-tema-btn"
                            :class="{ ativo: tema === t.v }"
                            :aria-checked="tema === t.v"
                            role="radio"
                            :title="t.label"
                            @click="definirTema(t.v)"
                        >
                            <i :class="t.icon" aria-hidden="true"></i>
                            <span>{{ t.label }}</span>
                        </button>
                    </div>
                </div>
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
                v-if="podeVerConfig && configDestino"
                :to="{ name: configDestino }"
                class="foot-item"
                :class="{ active: configuracoesAtiva, 'is-expanded': expanded }"
                :title="!expanded ? 'Configurações' : ''"
            >
                <i class="fa-solid fa-gear" aria-hidden="true"></i>
                <span class="lbl">Configurações</span>
            </router-link>

            <a
                href="mailto:contato.imedto@gmail.com?subject=Suporte%20Imedto"
                class="foot-item"
                :class="{ 'is-expanded': expanded }"
                :title="!expanded ? 'Ajuda' : ''"
            >
                <i class="fa-solid fa-circle-question" aria-hidden="true"></i>
                <span class="lbl">Ajuda</span>
            </a>
        </template>
    </AppSidebar>

    <main class="conteudo">
        <slot />
    </main>

    <EstabelecimentoSeletorModal
        :aberto="modalSeletorAberto"
        :estabelecimentos="tenant.estabelecimentos"
        :ativo-id="tenant.estabelecimentoAtivoId"
        @fechar="modalSeletorAberto = false"
        @selecionar="confirmarTrocaEstabelecimento"
    />
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
    border-bottom: 1px solid hsl(var(--border));
    font-size: 13px;
}
.pop-head b { color: hsl(var(--primary-dark)); }
.lnk {
    font-size: 11px;
    color: hsl(var(--primary));
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
.notif-item:hover { background: hsl(var(--muted)); }
.notif-item strong { font-size: 12px; color: hsl(var(--primary-dark)); font-weight: 600; }
.notif-item span { font-size: 11px; color: hsl(var(--muted-foreground)); }
.notif-item.nao-lida strong { color: hsl(var(--primary)); }
.vazio {
    padding: 1.5rem;
    text-align: center;
    color: hsl(var(--muted-foreground));
    font-size: 13px;
}

.perfil-card {
    display: flex;
    gap: 12px;
    padding: 14px;
    border-bottom: 1px solid hsl(var(--border));
}
.av-lg {
    width: 48px;
    height: 48px;
    border-radius: 50%;
    background: linear-gradient(135deg, hsl(var(--primary)) 0%, hsl(var(--primary-dark)) 100%);
    color: hsl(var(--primary-foreground));
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
    color: hsl(var(--primary-dark));
    margin-bottom: 2px;
}
.perfil-info span {
    display: block;
    font-size: 11px;
    color: hsl(var(--muted-foreground));
    line-height: 1.4;
}
.perfil-info .clinic { color: hsl(var(--success)); margin-top: 2px; }

.pop-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 8px 10px;
    border-radius: 6px;
    font-size: 12px;
    color: hsl(var(--primary-dark));
    cursor: pointer;
    background: transparent;
    border: 0;
    text-align: left;
    width: 100%;
    font-family: inherit;
}
.pop-item i {
    width: 14px;
    color: hsl(var(--muted-foreground));
    font-size: 12px;
}
.pop-item:hover { background: hsl(var(--muted)); }

.pop-foot {
    padding: 10px 14px;
    border-top: 1px solid hsl(var(--border));
}

/* Seletor de tema (claro/escuro/auto) dentro do dropdown do perfil */
.pop-tema {
    display: flex;
    flex-direction: column;
    gap: 6px;
    margin-top: 6px;
    padding: 10px;
    border-top: 1px solid hsl(var(--border));
}
.pop-tema-label {
    font-size: 11px;
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    text-transform: uppercase;
    letter-spacing: 0.05em;
    padding: 0 2px;
}
.pop-tema-row {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 4px;
    background: hsl(var(--muted));
    border-radius: 8px;
    padding: 3px;
}
.pop-tema-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    padding: 6px 8px;
    border-radius: 6px;
    border: 0;
    background: transparent;
    color: hsl(var(--muted-foreground));
    font-size: 11px;
    font-weight: 600;
    font-family: inherit;
    cursor: pointer;
    transition: background 0.15s, color 0.15s;
}
.pop-tema-btn i { font-size: 11px; }
.pop-tema-btn:hover:not(.ativo) {
    color: hsl(var(--primary-dark));
}
.pop-tema-btn.ativo {
    background: hsl(var(--card));
    color: hsl(var(--primary-dark));
    box-shadow: 0 1px 2px hsl(0 0% 0% / 0.1);
}
.logout {
    width: 100%;
    padding: 8px;
    border: none;
    background: hsl(var(--destructive) / 0.12);
    color: hsl(var(--destructive));
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
.logout:hover { background: hsl(var(--destructive) / 0.2); }

/* Footer items da sidebar (mesmo padrão visual dos itens principais do nav). */
.foot-item {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 9px 12px;
    border-radius: 8px;
    font-size: 13px;
    font-weight: 500;
    color: hsl(var(--foreground) / 0.78);
    text-decoration: none;
    border: 0;
    background: transparent;
    cursor: pointer;
    text-align: left;
    width: 100%;
    font-family: inherit;
    white-space: nowrap;
    transition: background 0.15s, color 0.15s;
}
.foot-item i { width: 20px; text-align: center; font-size: 15px; flex-shrink: 0; }
.foot-item .lbl {
    flex: 1;
    opacity: 0;
    transition: opacity 160ms;
    pointer-events: none;
}
.foot-item.is-expanded .lbl {
    opacity: 1;
    pointer-events: auto;
}
.foot-item:hover {
    color: hsl(var(--primary-dark));
    background: hsl(var(--muted));
}
.foot-item.active {
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary-dark));
    font-weight: 600;
}
.foot-item.active i { color: hsl(var(--primary)); }
.foot-item.danger { color: hsl(var(--destructive)); }
.foot-item.danger:hover { background: hsl(var(--destructive) / 0.1); color: hsl(var(--destructive)); }

.foot-item--locked {
    opacity: 0.35;
    cursor: not-allowed;
    pointer-events: none;
}
.foot-lock-icon {
    font-size: 10px;
    opacity: 0;
    transition: opacity 160ms;
    margin-left: auto;
    flex-shrink: 0;
}
.foot-item.is-expanded .foot-lock-icon,
.foot-item--locked.is-expanded .foot-lock-icon { opacity: 1; }

@media (max-width: 768px) {
    .conteudo { margin-left: 0; }
}
</style>
