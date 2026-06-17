<script setup lang="ts">
/**
 * AuditLogFeed — feed paginado do audit log com filtros.
 * W6-CA10 a CA18, CA14 (mapa PT-BR), CA15 (truncamento), CA16 (tenant), CA17 (admin).
 *
 * Filtros: ação, admin (dropdown), período (AppFilterPills, default 7d).
 * Paginação: AppPagination 10/pg.
 * Botão "Atualizar" mantém filtros.
 */
import { ref, watch, onMounted } from "vue"
import { AppCard, AppButton, AppSelect, AppFilterPills, AppPagination, AppEmptyState } from "@/components/ui"
import { useDashboardStore } from "../../stores/dashboardStore"
import { adminsService, type AdminListItem } from "../../services/adminsService"

const store = useDashboardStore()

// ── Mapa PT-BR das ações de audit (W6-CA14 / §6.5 do briefing) ───────────────
const ACOES_PT: Record<string, string> = {
    LOGIN_OK: "Login realizado",
    LOGIN_FAIL: "Tentativa de login",
    LOGOUT: "Logout",
    CRIAR_ADMIN: "Admin criado",
    DESATIVAR_ADMIN: "Admin desativado",
    REATIVAR_ADMIN: "Admin reativado",
    RESETAR_SENHA_ADMIN: "Senha de admin resetada",
    ABRIR_DETALHE_TENANT: "Detalhe de tenant aberto",
    REVELAR_CPF_DONO: "CPF do dono revelado",
    RESETAR_TENANT: "Tenant resetado",
    CRIAR_PLANO: "Plano criado",
    ATUALIZAR_PLANO: "Plano atualizado",
    ATIVAR_PLANO: "Plano ativado",
    DESATIVAR_PLANO: "Plano desativado",
    EDITAR_PLANO: "Plano editado",
    TROCAR_PLANO: "Plano alterado em assinatura",
    ALTERAR_ASSINATURA: "Assinatura alterada",
    CONCEDER_GRATUIDADE: "Gratuidade concedida",
    ENCERRAR_ASSINATURA: "Assinatura encerrada",
    RESET_SENHA_PROPRIA: "Senha própria alterada",
    ATUALIZAR_CONFIG: "Configuração global atualizada",
    CRIAR_MODELO_PADRAO_SISTEMA: "Modelo de prontuário criado",
    ATUALIZAR_MODELO_PADRAO_SISTEMA: "Modelo de prontuário atualizado",
    INATIVAR_MODELO_PADRAO_SISTEMA: "Modelo de prontuário inativado",
    REATIVAR_MODELO_PADRAO_SISTEMA: "Modelo de prontuário reativado",
    CRIAR_VARIAVEL_PADRAO_SISTEMA: "Variável criada",
    ATUALIZAR_VARIAVEL_PADRAO_SISTEMA: "Variável atualizada",
    INATIVAR_VARIAVEL_PADRAO_SISTEMA: "Variável inativada",
    REATIVAR_VARIAVEL_PADRAO_SISTEMA: "Variável reativada",
    CRIAR_REGIAO_ANATOMICA: "Região anatômica criada",
    ATUALIZAR_REGIAO_ANATOMICA: "Região anatômica atualizada",
    INATIVAR_REGIAO_ANATOMICA: "Região anatômica inativada",
    EXCLUIR_REGIAO_ANATOMICA: "Região anatômica excluída",
}
// Fallback: ação não mapeada mostra a constante crua (forward-compat).
function traduzir(acao: string): string {
    return ACOES_PT[acao] ?? acao
}

// Lista de opções de ações para o dropdown (AppSelect usa { value, label })
const ACOES_OPCOES = [
    { value: "", label: "Todas as ações" },
    ...Object.entries(ACOES_PT).map(([value, label]) => ({ value, label })),
]

// ── Estado dos filtros ────────────────────────────────────────────────────────

const acaoFiltro = ref("")
const adminIdFiltro = ref("")
const periodoFiltro = ref("7d")
const pagina = ref(1)
const tamanhoPagina = 10

const periodoOpcoes = [
    { valor: "hoje", label: "Hoje" },
    { valor: "7d",   label: "7 dias" },
    { valor: "30d",  label: "30 dias" },
    { valor: "90d",  label: "90 dias" },
    { valor: "todos", label: "Todos" },
]

// ── Lista de admins ativos para o dropdown ────────────────────────────────────

// AppSelect usa { value, label }
const adminsOpcoes = ref<Array<{ value: string; label: string }>>([
    { value: "", label: "Todos os admins" },
])

async function carregarAdmins() {
    try {
        const result = await adminsService.listar({ pagina: 1, tamanho: 100 })
        const ativos = result.itens.filter((a: AdminListItem) => a.ativo)
        adminsOpcoes.value = [
            { value: "", label: "Todos os admins" },
            ...ativos.map((a: AdminListItem) => ({ value: a.id, label: `${a.nome} (${a.email})` })),
        ]
    } catch {
        // silencioso — fallback para "Todos os admins"
    }
}

// ── Carregamento ──────────────────────────────────────────────────────────────

function carregarFeed() {
    store.carregarAuditLog({
        acao: acaoFiltro.value || undefined,
        adminId: adminIdFiltro.value || undefined,
        periodo: periodoFiltro.value,
        pagina: pagina.value,
        tamanhoPagina,
    })
}

// Ao mudar filtro → resetar paginação e recarregar
watch([acaoFiltro, adminIdFiltro, periodoFiltro], () => {
    pagina.value = 1
    carregarFeed()
})

// Ao mudar página → recarregar mantendo filtros
watch(pagina, () => carregarFeed())

onMounted(() => {
    carregarAdmins()
    carregarFeed()
})

// ── Helpers de formatação ─────────────────────────────────────────────────────

function formatarDataHora(iso: string): string {
    return new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" })
}

function formatarRelativo(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime()
    const min = Math.floor(diff / 60_000)
    if (min < 1) return "agora"
    if (min < 60) return `há ${min} min`
    const h = Math.floor(min / 60)
    if (h < 24) return `há ${h}h`
    const d = Math.floor(h / 24)
    return `há ${d}d`
}

function truncar(texto: string | null, max: number): string {
    if (!texto) return "—"
    if (texto.length <= max) return texto
    return texto.slice(0, max) + "…"
}

function nomeAdmin(item: { adminNome: string | null; adminEmail: string | null; adminId: string | null; adminAtivo: boolean }): string {
    if (!item.adminId) return "Sistema"
    const nome = item.adminNome ?? "Admin"
    if (!item.adminAtivo) return `${nome} (desativado)`
    return nome
}
</script>

<template>
    <AppCard>
        <!-- Cabeçalho com botão Atualizar -->
        <div class="feed-header">
            <h3 class="bloco-titulo">Histórico de atividades</h3>
            <AppButton variant="ghost" size="sm" @click="carregarFeed()">
                <i class="fa-solid fa-rotate-right"></i>
                Atualizar
            </AppButton>
        </div>

        <!-- Filtros -->
        <div class="feed-filtros">
            <!-- Período -->
            <AppFilterPills v-model="periodoFiltro" :opcoes="periodoOpcoes" />

            <!-- Ação -->
            <AppSelect
                v-model="acaoFiltro"
                :options="ACOES_OPCOES"
                style="min-width: 220px;"
            />

            <!-- Admin -->
            <AppSelect
                v-model="adminIdFiltro"
                :options="adminsOpcoes"
                style="min-width: 200px;"
            />
        </div>

        <!-- Loading -->
        <p v-if="store.carregandoAuditLog" class="estado-info" aria-live="polite">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </p>

        <!-- Erro -->
        <p v-else-if="store.erroAuditLog" class="bloco-erro" role="alert">
            {{ store.erroAuditLog }}
        </p>

        <template v-else>
            <!-- Vazio -->
            <AppEmptyState
                v-if="!store.auditLog?.itens.length"
                titulo="Nenhuma atividade encontrada no período."
            />

            <template v-else>
                <!-- Tabela -->
                <div class="tabela-wrap">
                    <table class="tabela">
                        <thead>
                            <tr>
                                <th>Quando</th>
                                <th>Admin</th>
                                <th>Ação</th>
                                <th>Recurso</th>
                                <th>Tenant</th>
                                <th>Motivo</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="item in store.auditLog!.itens" :key="item.id">
                                <!-- Quando -->
                                <td>
                                    <span :title="formatarDataHora(item.criadoEm)" class="data-relativa">
                                        {{ formatarRelativo(item.criadoEm) }}
                                    </span>
                                </td>

                                <!-- Admin -->
                                <td>
                                    <span class="admin-nome">{{ nomeAdmin(item) }}</span>
                                    <span v-if="item.adminEmail" class="admin-email">{{ item.adminEmail }}</span>
                                </td>

                                <!-- Ação traduzida -->
                                <td>{{ traduzir(item.acao) }}</td>

                                <!-- Recurso tipo + id -->
                                <td class="recurso-cell">
                                    <span v-if="item.recursoTipo" class="recurso-tipo">{{ item.recursoTipo }}</span>
                                    <span v-if="item.recursoId" class="recurso-id">{{ item.recursoId }}</span>
                                    <span v-if="!item.recursoTipo && !item.recursoId">—</span>
                                </td>

                                <!-- Tenant afetado -->
                                <td>{{ item.tenantNomeFantasia ?? "—" }}</td>

                                <!-- Motivo truncado com tooltip -->
                                <td>
                                    <span
                                        v-if="item.motivo"
                                        :title="item.motivo"
                                        class="motivo-texto"
                                    >{{ truncar(item.motivo, 100) }}</span>
                                    <span v-else>—</span>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>

                <!-- Paginação -->
                <div class="paginacao-wrap">
                    <AppPagination
                        v-model:pagina="pagina"
                        :tamanho="tamanhoPagina"
                        :total="store.auditLog!.total"
                        :ocultar-tamanhos="true"
                        rotulo-itens="atividades"
                    />
                </div>
            </template>
        </template>
    </AppCard>
</template>

<style scoped>
.feed-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 1rem;
}

.bloco-titulo {
    font-size: 0.875rem;
    font-weight: 600;
    color: hsl(var(--foreground));
    margin: 0;
}

.feed-filtros {
    display: flex;
    flex-wrap: wrap;
    gap: 0.75rem;
    align-items: center;
    margin-bottom: 1.25rem;
}

.estado-info {
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
}

.bloco-erro {
    color: hsl(var(--error));
    font-size: 0.875rem;
    padding: 0.75rem 1rem;
    border-radius: 8px;
    background: hsl(var(--error) / 0.08);
}

.tabela-wrap {
    overflow-x: auto;
    border-radius: 8px;
    border: 1px solid hsl(var(--foreground) / 0.07);
}

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.8rem;
}

.tabela th {
    text-align: left;
    padding: 0.5rem 0.75rem;
    background: hsl(var(--muted) / 0.4);
    font-size: 0.75rem;
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    text-transform: uppercase;
    letter-spacing: 0.04em;
    white-space: nowrap;
}

.tabela td {
    padding: 0.5rem 0.75rem;
    border-top: 1px solid hsl(var(--foreground) / 0.05);
    color: hsl(var(--foreground));
    vertical-align: top;
}

.tabela tbody tr:hover td {
    background: hsl(var(--muted) / 0.2);
}

.data-relativa {
    cursor: default;
    color: hsl(var(--muted-foreground));
}

.admin-nome {
    display: block;
    font-weight: 500;
}

.admin-email {
    display: block;
    font-size: 0.72rem;
    color: hsl(var(--muted-foreground));
}

.recurso-cell {
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
}

.recurso-tipo {
    font-weight: 500;
    text-transform: capitalize;
}

.recurso-id {
    font-size: 0.72rem;
    color: hsl(var(--muted-foreground));
    font-family: monospace;
}

.motivo-texto {
    cursor: default;
    color: hsl(var(--muted-foreground));
}

.paginacao-wrap {
    margin-top: 1rem;
    display: flex;
    justify-content: flex-end;
}
</style>
