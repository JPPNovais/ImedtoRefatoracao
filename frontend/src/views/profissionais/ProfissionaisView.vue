<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRouter } from "vue-router"
import AppBadge from "@/components/ui/AppBadge.vue"
import { AppButton } from "@/components/ui"
import { vinculoService, type ProfissionalVinculado } from "@/services/vinculoService"
import { permissaoService, type ModeloPermissao }     from "@/services/permissaoService"
import { useTenantStore } from "@/stores/tenantStore"
import { useAuthStore }   from "@/stores/authStore"

const router = useRouter()

const tenant = useTenantStore()
const auth   = useAuthStore()

const profissionais = ref<ProfissionalVinculado[]>([])
const modelos       = ref<ModeloPermissao[]>([])
const carregando    = ref(false)
const erro          = ref<string | null>(null)

// ─── Convite inline (form no topo — padrão do legado) ─────────────────────────
const emailConvite       = ref("")
const nomeConvite        = ref("")
const telefoneConvite    = ref("")
const especialidadeConvite = ref("")
const modeloPermissaoId  = ref<number | null>(null)
const enviando           = ref(false)
const mensagemSucesso    = ref<string | null>(null)
const actionLinkDev      = ref<string | null>(null)

// ─── Tabs + filtro ────────────────────────────────────────────────────────────
type Aba = "vinculados" | "solicitacoes"
const abaAtiva     = ref<Aba>("vinculados")
const filtroStatus = ref<"ativos" | "todos">("ativos")

const vinculados = computed(() => {
    // Sempre mostra Ativos; "Todos" inclui inativos e expirados etc.
    return profissionais.value.filter(p => {
        if (filtroStatus.value === "ativos") return p.status === "Ativo"
        return p.status !== "Pendente"
    })
})

const solicitacoes = computed(() =>
    profissionais.value.filter(p => p.status === "Pendente"),
)

// ─── Ações ────────────────────────────────────────────────────────────────────
async function convidar() {
    if (!emailConvite.value.trim()) return
    enviando.value = true
    erro.value = null
    mensagemSucesso.value = null
    actionLinkDev.value = null
    try {
        const r = await vinculoService.convidarProfissional({
            email: emailConvite.value.trim(),
            modeloPermissaoId: modeloPermissaoId.value,
            nome: nomeConvite.value.trim() || null,
            telefone: telefoneConvite.value.trim() || null,
            especialidade: especialidadeConvite.value.trim() || null,
        })
        mensagemSucesso.value = `Convite enviado para ${emailConvite.value}.`
        actionLinkDev.value   = r.actionLink ?? null
        emailConvite.value    = ""
        nomeConvite.value     = ""
        telefoneConvite.value = ""
        especialidadeConvite.value = ""
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao enviar convite."
    } finally {
        enviando.value = false
    }
}

async function revogar(p: ProfissionalVinculado) {
    if (!confirm(`Revogar vínculo de ${p.nomeCompleto || p.email}?`)) return
    try {
        await vinculoService.inativarVinculo(p.vinculoId)
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao revogar."
    }
}

async function carregar() {
    carregando.value = true
    try {
        const [prof, mods] = await Promise.all([
            vinculoService.listarProfissionais(),
            permissaoService.listar(),
        ])
        profissionais.value = prof
        modelos.value       = mods
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar."
    } finally {
        carregando.value = false
    }
}

function iniciais(nome: string | null | undefined, email: string) {
    const base = (nome && nome.trim()) || email || "?"
    return base.charAt(0).toUpperCase()
}

const estabelecimentoIniciais = computed(() =>
    (tenant.ativo?.nomeFantasia ?? "E").charAt(0).toUpperCase(),
)

function ehVinculoProprio(p: ProfissionalVinculado) {
    return p.usuarioId === auth.usuario?.id
}

onMounted(carregar)
</script>

<template>
    <div class="app-page app-page--wide prof">
        <!-- Cabeçalho -->
        <div class="page-header">
            <div>
                <h1 class="page-titulo">Profissionais do estabelecimento</h1>
                <p class="page-sub">Gerencie os profissionais vinculados, convites e permissões de acesso.</p>
            </div>
        </div>

        <!-- Card com estabelecimento ativo -->
        <div class="estab-card">
            <div class="estab-avatar">{{ estabelecimentoIniciais }}</div>
            <div class="estab-info">
                <span class="estab-label">Estabelecimento</span>
                <span class="estab-nome">{{ tenant.ativo?.nomeFantasia ?? "—" }}</span>
            </div>
        </div>

        <!-- Atalhos: permissões + meus convites -->
        <div class="atalhos-row">
            <button type="button" class="atalho-link" @click="router.push({ name: 'ModelosPermissao' })">
                <i class="fa-solid fa-user-group" aria-hidden="true"></i>
                <div class="atalho-text">
                    <span class="atalho-titulo">Modelos de permissão</span>
                    <span class="atalho-desc">Defina o que cada perfil pode fazer.</span>
                </div>
                <i class="fa-solid fa-chevron-right chev" aria-hidden="true"></i>
            </button>
            <button type="button" class="atalho-link" @click="router.push({ name: 'MeusConvites' })">
                <i class="fa-solid fa-envelope" aria-hidden="true"></i>
                <div class="atalho-text">
                    <span class="atalho-titulo">Meus convites</span>
                    <span class="atalho-desc">Convites recebidos de outros estabelecimentos.</span>
                </div>
                <i class="fa-solid fa-chevron-right chev" aria-hidden="true"></i>
            </button>
        </div>

        <!-- Form inline de convite -->
        <div class="card card-convite">
            <h3 class="secao-titulo">Enviar convite para profissional</h3>
            <p class="secao-sub">
                E-mail é o único campo obrigatório. Os demais são opcionais —
                quando preenchidos, aparecem pré-cadastrados para o convidado no onboarding.
            </p>
            <div class="form-grid">
                <div class="campo">
                    <label class="campo-label">E-mail do profissional</label>
                    <input
                        v-model="emailConvite"
                        type="email"
                        class="input-field"
                        placeholder="profissional@exemplo.com"
                        :disabled="enviando"
                    />
                </div>
                <div class="campo">
                    <label class="campo-label">Nome completo <span class="opt">(opcional)</span></label>
                    <input
                        v-model="nomeConvite"
                        type="text"
                        class="input-field"
                        placeholder="Como o profissional se chama"
                        :disabled="enviando"
                    />
                </div>
                <div class="campo">
                    <label class="campo-label">Telefone <span class="opt">(opcional)</span></label>
                    <input
                        v-model="telefoneConvite"
                        type="tel"
                        class="input-field"
                        placeholder="(11) 99999-0000"
                        :disabled="enviando"
                    />
                </div>
                <div class="campo">
                    <label class="campo-label">Especialidade <span class="opt">(opcional)</span></label>
                    <input
                        v-model="especialidadeConvite"
                        type="text"
                        class="input-field"
                        placeholder="Ex: Cardiologia"
                        :disabled="enviando"
                    />
                </div>
                <div class="campo campo-full">
                    <label class="campo-label">Modelo de permissão <span class="opt">(opcional)</span></label>
                    <select v-model="modeloPermissaoId" class="input-field" :disabled="enviando">
                        <option :value="null">Sem permissão (atribuir depois)</option>
                        <option v-for="m in modelos" :key="m.id" :value="m.id">
                            {{ m.nome }} ({{ m.tipoAcesso }})
                        </option>
                    </select>
                </div>
            </div>

            <p v-if="erro" class="msg-erro">{{ erro }}</p>
            <p v-if="mensagemSucesso" class="msg-ok">{{ mensagemSucesso }}</p>

            <!-- Link de ativação em dev -->
            <div v-if="actionLinkDev" class="dev-link">
                <span class="dev-label">Link de ativação (dev):</span>
                <code class="dev-url">{{ actionLinkDev }}</code>
            </div>

            <div class="form-footer">
                <AppButton
                    :disabled="enviando || !emailConvite.trim()"
                    :loading="enviando"
                    @click="convidar"
                >Enviar convite</AppButton>
            </div>
        </div>

        <!-- Tabs + filtro -->
        <div class="card card-lista">
            <div class="tabs-bar">
                <nav class="abas">
                    <button
                        class="aba"
                        :class="{ ativa: abaAtiva === 'vinculados' }"
                        @click="abaAtiva = 'vinculados'"
                    >Profissionais vinculados</button>
                    <button
                        class="aba"
                        :class="{ ativa: abaAtiva === 'solicitacoes' }"
                        @click="abaAtiva = 'solicitacoes'"
                    >Solicitações enviadas</button>
                </nav>

                <div v-if="abaAtiva === 'vinculados'" class="filtro-status">
                    <label class="campo-label">Status:</label>
                    <select v-model="filtroStatus" class="input-field input-inline">
                        <option value="ativos">Ativos</option>
                        <option value="todos">Todos</option>
                    </select>
                </div>
            </div>

            <div class="tabela-wrap">
                <div v-if="carregando" class="estado-msg">Carregando...</div>

                <!-- Lista de vinculados -->
                <template v-else-if="abaAtiva === 'vinculados'">
                    <div v-if="vinculados.length === 0" class="estado-msg">
                        Nenhum profissional vinculado no momento.
                    </div>
                    <table v-else class="tabela">
                        <thead>
                            <tr>
                                <th>Profissional</th>
                                <th>Especialidade</th>
                                <th>E-mail</th>
                                <th>Perfil / status</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="p in vinculados" :key="p.vinculoId">
                                <td class="cel-prof">
                                    <div class="avatar-sm">{{ iniciais(p.nomeCompleto, p.email) }}</div>
                                    <span class="nome">{{ p.nomeCompleto || "—" }}</span>
                                </td>
                                <td>{{ p.especialidade || "—" }}</td>
                                <td class="cel-email">{{ p.email }}</td>
                                <td>
                                    <AppBadge :status="p.status" />
                                </td>
                                <td class="cel-acoes">
                                    <AppButton
                                        v-if="!ehVinculoProprio(p)"
                                        variant="danger"
                                        size="sm"
                                        @click="revogar(p)"
                                    >Desativar vínculo</AppButton>
                                    <span v-else class="seu-vinculo">Seu vínculo</span>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </template>

                <!-- Solicitações enviadas -->
                <template v-else>
                    <div v-if="solicitacoes.length === 0" class="estado-msg">
                        Nenhum convite pendente. Envie um novo convite acima.
                    </div>
                    <table v-else class="tabela">
                        <thead>
                            <tr>
                                <th>E-mail convidado</th>
                                <th>Status</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="p in solicitacoes" :key="p.vinculoId">
                                <td>{{ p.email }}</td>
                                <td><AppBadge :status="p.status" /></td>
                                <td class="cel-acoes">
                                    <AppButton variant="danger" size="sm" @click="revogar(p)">
                                        Cancelar convite
                                    </AppButton>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </template>
            </div>
        </div>
    </div>
</template>

<style scoped>

.page-header { margin-bottom: 1.25rem; }
.page-titulo { font-size: 1.5rem; font-weight: 800; margin: 0 0 0.2rem; }
.page-sub    { margin: 0; color: var(--text-muted); font-size: 0.875em; }

/* Card do estabelecimento */
.estab-card {
    display: flex; align-items: center; gap: 1rem;
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 1rem 1.25rem; margin-bottom: 1rem;
}
.estab-avatar {
    width: 44px; height: 44px; border-radius: 50%;
    background: var(--primary-light, #ede9fe); color: var(--primary-dark, #4c1d95);
    display: flex; align-items: center; justify-content: center;
    font-size: 1.1em; font-weight: 700; flex-shrink: 0;
}
.estab-info { display: flex; flex-direction: column; }
.estab-label { font-size: 0.72em; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.04em; }
.estab-nome  { font-size: 1em; font-weight: 700; color: var(--text); }

/* Atalhos (permissões / convites) */
.atalhos-row {
    display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; margin-bottom: 1rem;
}
@media (max-width: 700px) { .atalhos-row { grid-template-columns: 1fr; } }
.atalho-link {
    display: flex; align-items: center; gap: 0.85rem;
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 0.85rem 1rem;
    cursor: pointer; text-align: left; font-family: inherit;
    transition: border-color 0.12s, background 0.12s, transform 0.12s;
}
.atalho-link:hover {
    border-color: hsl(var(--primary, 254 56% 38%) / 0.4);
    background: hsl(var(--primary, 254 56% 38%) / 0.04);
}
.atalho-link > i:first-child {
    width: 36px; height: 36px; border-radius: 8px;
    background: hsl(var(--primary, 254 56% 38%) / 0.1);
    color: hsl(var(--primary, 254 56% 38%));
    display: flex; align-items: center; justify-content: center;
    font-size: 0.95em; flex-shrink: 0;
}
.atalho-text { display: flex; flex-direction: column; gap: 0.1rem; flex: 1; min-width: 0; }
.atalho-titulo { font-size: 0.88em; font-weight: 700; color: var(--text); }
.atalho-desc   { font-size: 0.75em; color: var(--text-muted); }
.atalho-link .chev { color: var(--text-muted); font-size: 0.7em; }

/* Card genérico */
.card {
    background: var(--bg-card); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 1.25rem; margin-bottom: 1rem;
}
.card-convite { display: flex; flex-direction: column; gap: 0.9rem; }
.card-lista   { padding: 0.5rem 1.25rem 1.25rem; }

.secao-titulo { font-size: 0.92em; font-weight: 700; margin: 0; }
.secao-sub { font-size: 0.78em; color: var(--text-muted); margin: -0.4rem 0 0.4rem; line-height: 1.4; }
.opt { font-weight: 400; color: var(--text-muted); margin-left: 0.25rem; }

.form-grid {
    display: grid; grid-template-columns: 1fr 1fr; gap: 1rem;
}
.form-grid .campo-full { grid-column: 1 / -1; }
@media (max-width: 700px) { .form-grid { grid-template-columns: 1fr; } }

.form-footer { display: flex; justify-content: flex-end; margin-top: 0.25rem; }

/* Tabs */
.tabs-bar {
    display: flex; justify-content: space-between; align-items: center; gap: 1rem;
    padding: 0.75rem 0; margin-bottom: 0.5rem; flex-wrap: wrap;
}
.abas {
    display: inline-flex; padding: 4px;
    background: rgba(30, 27, 75, 0.05); border-radius: 999px;
}
.aba {
    border: none; background: none; cursor: pointer;
    padding: 0.35rem 0.95rem; border-radius: 999px;
    font-family: inherit; font-size: 0.78em; font-weight: 600;
    color: rgba(30, 27, 75, 0.55); transition: all 0.12s;
    white-space: nowrap;
}
.aba:hover:not(.ativa) { color: rgba(30, 27, 75, 0.8); }
.aba.ativa {
    background: var(--primary-light, #ede9fe);
    color: var(--primary-dark, #4c1d95);
}

.filtro-status { display: flex; align-items: center; gap: 0.5rem; }

/* Tabela */
.tabela-wrap { overflow-x: auto; }
.tabela { width: 100%; border-collapse: collapse; font-size: 0.875em; }
.tabela th {
    background: #f9fafb; text-align: left; padding: 0.65rem 1rem;
    font-size: 0.75em; font-weight: 700; text-transform: uppercase;
    letter-spacing: 0.04em; color: var(--text-muted);
    border-bottom: 1px solid var(--border);
}
.tabela td { padding: 0.85rem 1rem; border-bottom: 1px solid var(--border); vertical-align: middle; }
.tabela tr:last-child td { border-bottom: none; }

.cel-prof { display: flex; align-items: center; gap: 0.75rem; }
.avatar-sm {
    width: 32px; height: 32px; border-radius: 50%;
    background: var(--primary-light, #ede9fe); color: var(--primary-dark, #4c1d95);
    display: flex; align-items: center; justify-content: center;
    font-size: 0.85em; font-weight: 700; flex-shrink: 0;
}
.nome { font-weight: 600; }
.cel-email { color: var(--text-muted); }
.cel-acoes { white-space: nowrap; text-align: right; }

.seu-vinculo {
    color: var(--text-muted); font-size: 0.85em; font-style: italic;
}

.estado-msg { text-align: center; color: var(--text-muted); padding: 2rem 1rem; font-size: 0.9em; }

/* Form */
.campo       { display: flex; flex-direction: column; gap: 0.3rem; }
.campo-label { font-size: 0.78em; font-weight: 600; color: var(--text-muted); }

.input-field {
    padding: 0.5rem 0.75rem; border: 1px solid var(--border-strong);
    border-radius: var(--radius); font-family: inherit; font-size: 0.875em;
    background: var(--bg-card); color: var(--text);
}
.input-field:focus { outline: none; border-color: var(--primary); }
.input-inline { padding: 0.35rem 0.6rem; font-size: 0.8em; min-width: 110px; }

.msg-erro { color: var(--danger); font-size: 0.875em; margin: 0; }
.msg-ok   { color: #15803d;      font-size: 0.875em; margin: 0; }

.dev-link {
    padding: 0.75rem;
    background: #fef3c7; color: #92400e;
    border-radius: var(--radius); font-size: 0.78em;
    display: flex; flex-direction: column; gap: 0.25rem;
}
.dev-label { font-weight: 700; }
.dev-url   { word-break: break-all; font-family: monospace; font-size: 0.95em; }

</style>
