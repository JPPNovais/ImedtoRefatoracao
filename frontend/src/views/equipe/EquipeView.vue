<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import {
    AppButton, AppPageHeader, AppToast,
} from "@/components/ui"
import AbaProfissionais     from "@/components/equipe/AbaProfissionais.vue"
import AbaPapeis            from "@/components/equipe/AbaPapeis.vue"
import AbaConvites          from "@/components/equipe/AbaConvites.vue"
import ConvidarProfissionalModal from "@/components/equipe/ConvidarProfissionalModal.vue"
import PapelEditorModal     from "@/components/equipe/PapelEditorModal.vue"
import ProfissionalDetalhesModal from "@/components/equipe/ProfissionalDetalhesModal.vue"
import { vinculoService, type ProfissionalVinculado } from "@/services/vinculoService"
import { permissaoService, type ModeloPermissao } from "@/services/permissaoService"
import { useTenantStore } from "@/stores/tenantStore"

/**
 * Tela "Equipe e permissões" — substitui as antigas ProfissionaisView e
 * ModelosPermissaoView. 3 abas:
 *  - Profissionais (lista, filtros, bulk actions)
 *  - Papéis e permissões (sistema + customizados)
 *  - Convites pendentes
 *
 * Acesso restrito ao Dono.
 */
const router = useRouter()
const route = useRoute()
const tenant = useTenantStore()

// Defense-in-depth front: o backend já bloqueia. Reagimos em watchEffect para
// não redirecionar antes do tenant resolver — entrar com `papel: null` é o
// estado inicial legítimo durante o boot.
import { watchEffect } from "vue"
watchEffect(() => {
    if (tenant.temTenantSelecionado && tenant.papel && tenant.papel !== "Dono") {
        router.replace({ name: "Home" })
    }
})

type Aba = "profissionais" | "papeis" | "convites"

const aba = ref<Aba>(parseAbaInicial())
function parseAbaInicial(): Aba {
    const q = String(route.query.aba ?? "")
    if (q === "papeis" || q === "convites") return q
    return "profissionais"
}

// ─── Estado central ─────────────────────────────────────────────────────────
const profissionais = ref<ProfissionalVinculado[]>([])
const modelos = ref<ModeloPermissao[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)

// Toast (estado mínimo).
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}

// Modais.
const convidarAberto = ref(false)
const papelEditorAberto = ref(false)
const papelEmEdicao = ref<ModeloPermissao | null>(null)
const detalhesAberto = ref(false)
const detalhesProfissional = ref<ProfissionalVinculado | null>(null)

// ─── Derivados ─────────────────────────────────────────────────────────────
const convitesPendentes = computed(() =>
    profissionais.value.filter(p => p.status === "Convidado"),
)

const profissionaisAtivos = computed(() =>
    profissionais.value.filter(p => p.status !== "Convidado" && p.status !== "Removido"),
)

// ─── Carregamento ──────────────────────────────────────────────────────────
async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const [pros, mods] = await Promise.all([
            vinculoService.listarProfissionais({ incluirInativos: true }),
            permissaoService.listar(),
        ])
        profissionais.value = pros
        modelos.value = mods
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível carregar a equipe."
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)

// ─── Handlers ───────────────────────────────────────────────────────────────
function abrirConvite() {
    convidarAberto.value = true
}

function onConviteEnviado(payload: { nome: string, email: string, actionLink?: string | null }) {
    convidarAberto.value = false
    aba.value = "convites"
    void carregar()
    notificar(`Convite enviado para ${payload.nome || payload.email}.`)
    if (payload.actionLink) {
        // Em dev o backend devolve link clicável — útil para teste manual.
        // eslint-disable-next-line no-console
        console.info("Link de ativação (dev):", payload.actionLink)
    }
}

function abrirNovoPapel() {
    papelEmEdicao.value = null
    papelEditorAberto.value = true
}

function abrirEditarPapel(m: ModeloPermissao) {
    papelEmEdicao.value = m
    papelEditorAberto.value = true
}

function onPapelSalvo(m: ModeloPermissao) {
    papelEditorAberto.value = false
    papelEmEdicao.value = null
    void carregar()
    notificar(`Permissão "${m.nome}" salva.`)
}

function onPapelExcluido(m: ModeloPermissao) {
    papelEditorAberto.value = false
    papelEmEdicao.value = null
    void carregar()
    notificar(`Permissão "${m.nome}" excluída.`)
}

function abrirDetalhes(p: ProfissionalVinculado) {
    detalhesProfissional.value = p
    detalhesAberto.value = true
}

function onDetalhesAtualizado(p: ProfissionalVinculado) {
    // Atualiza local sem refetch para resposta imediata.
    const idx = profissionais.value.findIndex(x => x.vinculoId === p.vinculoId)
    if (idx >= 0) profissionais.value[idx] = p
    detalhesAberto.value = false
    notificar(`Permissões de ${p.nomeCompleto || p.email} atualizadas.`)
}

function onDetalhesRemovido(p: ProfissionalVinculado) {
    detalhesAberto.value = false
    void carregar()
    notificar(`${p.nomeCompleto || p.email} foi removido(a) do estabelecimento.`)
}

function onDetalhesReativado(p: ProfissionalVinculado) {
    detalhesAberto.value = false
    void carregar()
    notificar(`${p.nomeCompleto || p.email} reativado(a) com sucesso.`)
}

async function onAcaoMassa(payload: { acao: "ativar" | "desativar" | "remover", ids: number[] }) {
    // "Desativar" e "remover" reusam o mesmo endpoint (inativar). "Ativar" usa o
    // endpoint dedicado de reativar (Inativo → Ativo, sem novo convite).
    const verbo = payload.acao === "ativar" ? "Reativar"
        : payload.acao === "remover" ? "Remover" : "Desativar"
    if (!confirm(`${verbo} ${payload.ids.length} profissional(is)?`)) return

    // Filtra ids que realmente fazem sentido para a ação — botões já escondem o
    // que não aplica, mas a seleção pode ser mista (ex: Ativos + Inativos).
    const alvos = profissionais.value.filter(p => p.vinculoId != null && payload.ids.includes(p.vinculoId as number))
    const aplicaveis = alvos
        .filter(p => payload.acao === "ativar" ? p.status === "Inativo" : p.status !== "Inativo")
        .map(p => p.vinculoId as number)

    if (!aplicaveis.length) {
        notificar("Nenhum profissional aplicável para esta ação na seleção.", "info")
        return
    }

    const erros: string[] = []
    for (const id of aplicaveis) {
        try {
            if (payload.acao === "ativar") await vinculoService.reativarVinculo(id)
            else                            await vinculoService.inativarVinculo(id)
        } catch (e: any) {
            erros.push(e?.response?.data?.mensagem ?? "Falha ao processar um dos vínculos.")
        }
    }
    await carregar()

    const sucesso = aplicaveis.length - erros.length
    const sufixo = payload.acao === "ativar" ? "reativado(s)"
        : payload.acao === "remover" ? "removido(s)" : "desativado(s)"
    if (sucesso > 0) notificar(`${sucesso} profissional(is) ${sufixo}.`)
    if (erros.length) notificar(erros[0], "error")
}

async function cancelarConvite(c: ProfissionalVinculado) {
    if (c.vinculoId == null) return  // Dono não tem vínculo; nunca chega aqui via UI.
    if (!confirm(`Cancelar convite enviado para ${c.email}?`)) return
    try {
        await vinculoService.inativarVinculo(c.vinculoId)
        await carregar()
        notificar(`Convite para ${c.email} cancelado.`)
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Não foi possível cancelar o convite.", "error")
    }
}

const reenviandoConviteId = ref<number | null>(null)
async function reenviarConvite(c: ProfissionalVinculado) {
    if (c.vinculoId == null) return
    reenviandoConviteId.value = c.vinculoId
    try {
        await vinculoService.reenviarConvite(c.vinculoId)
        notificar(`Convite reenviado para ${c.email}.`)
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Não foi possível reenviar o convite.", "error")
    } finally {
        reenviandoConviteId.value = null
    }
}
</script>

<template>
    <main class="app-page app-page--wide equipe">
        <AppPageHeader
            titulo="Equipe e permissões"
            :subtitulo="'Gerencie profissionais, permissões de acesso e convites de ' + (tenant.ativo?.nomeFantasia ?? 'sua clínica') + '.'"
        >
            <template #acoes>
                <AppButton icon="fa-solid fa-user-plus" @click="abrirConvite">
                    Convidar profissional
                </AppButton>
            </template>
        </AppPageHeader>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>

        <!-- Tabs -->
        <div class="tabs">
            <button
                type="button"
                class="tab"
                :class="{ active: aba === 'profissionais' }"
                @click="aba = 'profissionais'"
            >
                <i class="fa-solid fa-users"></i>
                Profissionais
                <span class="tab-count">{{ profissionaisAtivos.length }}</span>
            </button>
            <button
                type="button"
                class="tab"
                :class="{ active: aba === 'papeis' }"
                @click="aba = 'papeis'"
            >
                <i class="fa-solid fa-shield-halved"></i>
                Permissões
                <span class="tab-count">{{ modelos.length }}</span>
            </button>
            <button
                type="button"
                class="tab"
                :class="{ active: aba === 'convites' }"
                @click="aba = 'convites'"
            >
                <i class="fa-solid fa-envelope-open-text"></i>
                Convites pendentes
                <span
                    v-if="convitesPendentes.length > 0"
                    class="tab-count tab-count--warning"
                >
                    {{ convitesPendentes.length }}
                </span>
            </button>
        </div>

        <p v-if="carregando" class="msg-info">Carregando…</p>

        <template v-else>
            <AbaProfissionais
                v-if="aba === 'profissionais'"
                :profissionais="profissionais"
                :modelos="modelos"
                @abrir-detalhes="abrirDetalhes"
                @abrir-convite="abrirConvite"
                @acao-massa="onAcaoMassa"
            />
            <AbaPapeis
                v-else-if="aba === 'papeis'"
                :modelos="modelos"
                :profissionais="profissionais"
                @criar-papel="abrirNovoPapel"
                @editar-papel="abrirEditarPapel"
            />
            <AbaConvites
                v-else
                :convites="convitesPendentes"
                :modelos="modelos"
                :reenviando-id="reenviandoConviteId"
                @abrir-convite="abrirConvite"
                @cancelar="cancelarConvite"
                @reenviar="reenviarConvite"
            />
        </template>

        <!-- Modais -->
        <ConvidarProfissionalModal
            :aberto="convidarAberto"
            :modelos="modelos"
            @fechar="convidarAberto = false"
            @enviado="onConviteEnviado"
        />
        <PapelEditorModal
            :aberto="papelEditorAberto"
            :modelo="papelEmEdicao"
            @fechar="papelEditorAberto = false; papelEmEdicao = null"
            @salvo="onPapelSalvo"
            @excluido="onPapelExcluido"
        />
        <ProfissionalDetalhesModal
            :aberto="detalhesAberto"
            :profissional="detalhesProfissional"
            :modelos="modelos"
            @fechar="detalhesAberto = false; detalhesProfissional = null"
            @atualizado="onDetalhesAtualizado"
            @removido="onDetalhesRemovido"
            @reativado="onDetalhesReativado"
        />

        <AppToast
            v-if="toast"
            :mensagem="toast.mensagem"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </main>
</template>

<style scoped>
.equipe {
    display: flex;
    flex-direction: column;
    gap: 18px;
}

.tabs {
    display: flex; gap: 4px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.1);
    margin-bottom: 4px;
    overflow-x: auto;
}
.tab {
    display: inline-flex; align-items: center; gap: 8px;
    background: transparent; border: none; padding: 12px 18px;
    font-family: inherit; font-size: 14px; font-weight: 600;
    color: hsl(var(--secondary) / 0.6);
    cursor: pointer;
    border-bottom: 2px solid transparent;
    transition: color 150ms;
    white-space: nowrap;
}
.tab:hover { color: hsl(var(--primary-dark)); }
.tab.active {
    color: hsl(var(--primary));
    border-bottom-color: hsl(var(--primary));
}
.tab-count {
    display: inline-flex; align-items: center; justify-content: center;
    min-width: 20px; height: 20px; padding: 0 6px;
    border-radius: 999px;
    background: hsl(var(--secondary) / 0.08);
    font-size: 11px; font-weight: 700; color: hsl(var(--secondary) / 0.65);
}
.tab.active .tab-count {
    background: hsl(var(--primary) / 0.15);
    color: hsl(var(--primary));
}
.tab-count--warning {
    background: hsl(var(--warning) / 0.18) !important;
    color: hsl(40 90% 35%) !important;
}

.msg-info { color: hsl(var(--secondary) / 0.7); margin: 0; }
.msg-erro {
    color: hsl(var(--error));
    background: hsl(var(--error) / 0.06);
    border: 1px solid hsl(var(--error) / 0.2);
    border-radius: 8px; padding: 10px 14px;
    font-size: 13px; margin: 0;
}
</style>
