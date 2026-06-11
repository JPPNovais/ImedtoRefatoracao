<script setup lang="ts">
/**
 * EstabelecimentoView — layout master-detail de configurações do estabelecimento.
 *
 * Sub-nav (~248px) à esquerda com 3 grupos e 10 seções + busca client-side.
 * Painel de detalhe à direita que monta o componente da seção ativa sob demanda.
 *
 * Deep-link bidirecional via ?secao= (router.replace, espelhando OrcamentoSettingsView).
 * Seção default: "dados". Seção inválida/sem permissão → cai em "dados".
 *
 * Painéis pesados (5 views externas) carregados via defineAsyncComponent + v-if:
 * painel não-ativo não monta nem dispara consulta (R1).
 *
 * RBAC por ocultação: gates reaproveitados de routePermissions.ts/permissoesStore (R4).
 */
import { type ComputedRef, computed, defineAsyncComponent, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { vMaska } from "maska/vue"
import { estabelecimentoService, type Estabelecimento } from "@/services/estabelecimentoService"
import { auth2faService } from "@/services/auth2faService"
import { redimensionarImagem } from "@/services/imageUtils"
import { invalidarCacheEstabelecimentoAtivo } from "@/composables/usePdfHeader"
import { useTenantStore } from "@/stores/tenantStore"
import { usePermissoesStore } from "@/stores/permissoesStore"
import { AppButton, AppPhotoUpload, AppConfirmDialog, AppToast, AppPageHeader, AppSearchInput } from "@/components/ui"
import FuncionamentoTab from "@/components/estabelecimento/FuncionamentoTab.vue"
import UnidadesTab from "@/components/estabelecimento/UnidadesTab.vue"
import ReparticoesTab from "@/components/estabelecimento/ReparticoesTab.vue"
import ListasVariaveisTab from "@/components/estabelecimento/ListasVariaveisTab.vue"

// ─── Painéis lazy (defineAsyncComponent → não montam até a seção ser aberta) ─
const PainelAutomacoes      = defineAsyncComponent(() => import("@/views/automacoes/AutomacoesView.vue"))
const PainelModelosPront    = defineAsyncComponent(() => import("@/views/configuracoes/ModelosProntuarioView.vue"))
const PainelIa              = defineAsyncComponent(() => import("@/views/configuracoes/MinhaIaSettingsView.vue"))
const PainelAssinatura      = defineAsyncComponent(() => import("@/views/assinatura/MinhaAssinaturaView.vue"))
const PainelTermos          = defineAsyncComponent(() => import("@/components/termos/TermosPainelEmbutido.vue"))
const PainelFinanceiro      = defineAsyncComponent(() => import("@/views/financeiro/FinanceiroConfigView.vue"))
const PainelConvenios       = defineAsyncComponent(() => import("@/views/estabelecimento/ConveniosConfigView.vue"))

// ─── Stores e router ──────────────────────────────────────────────────────────
const route   = useRoute()
const router  = useRouter()
const tenant  = useTenantStore()
const permissoes = usePermissoesStore()

// ─── Permissões (gates reaproveitados de routePermissions.ts — R4) ────────────
// podeExtra e pode já retornam true para Dono (ver permissoesStore).
const podeVerTermos    = computed(() => permissoes.pode("termos.gerenciar_modelos"))
const podeVerIa        = computed(() => permissoes.podeExtra("config_estabelecimento"))
const podeVerModelos   = computed(() => permissoes.podeExtra("modelos_prontuario"))
const podeVerAutomacoes= computed(() => permissoes.podeExtra("automacao_config"))
const podeVerConvenios = computed(() => permissoes.pode("convenios.ver") || permissoes.ehDono)
const podeEditar       = ref(false)

// ─── Definição dos grupos e seções ────────────────────────────────────────────
type SecaoId =
    | "dados" | "funcionamento" | "unidades" | "reparticoes"
    | "modelos-prontuario" | "termos" | "variaveis"
    | "automacoes" | "ia" | "assinatura" | "seguranca"
    | "financeiro" | "convenios"

interface SecaoItem {
    id: SecaoId
    label: string
    icone: string
    // Visibilidade pode ser um computed ref booleano ou literal true (sempre visível)
    visivel: ComputedRef<boolean> | true
}

interface GrupoNav {
    label: string
    secoes: SecaoItem[]
}

const GRUPOS_NAV: GrupoNav[] = [
    {
        label: "Estabelecimento",
        secoes: [
            { id: "dados",          label: "Dados",                icone: "fa-solid fa-building",       visivel: true },
            { id: "funcionamento",  label: "Funcionamento",        icone: "fa-solid fa-clock",           visivel: true },
            { id: "unidades",       label: "Unidades",             icone: "fa-solid fa-location-dot",    visivel: true },
            { id: "reparticoes",    label: "Repartições",          icone: "fa-solid fa-sitemap",         visivel: true },
        ],
    },
    {
        label: "Modelos e listas",
        secoes: [
            { id: "modelos-prontuario", label: "Modelos de prontuário",    icone: "fa-solid fa-notes-medical",  visivel: podeVerModelos },
            { id: "termos",             label: "Termos de consentimento",  icone: "fa-solid fa-file-signature", visivel: podeVerTermos },
            { id: "variaveis",          label: "Listas de variáveis",      icone: "fa-solid fa-list",           visivel: true },
        ],
    },
    {
        label: "Recursos",
        secoes: [
            { id: "automacoes", label: "Automações",          icone: "fa-solid fa-bolt",    visivel: podeVerAutomacoes },
            { id: "ia",         label: "Configurações de IA", icone: "fa-solid fa-wand-magic-sparkles", visivel: podeVerIa },
            { id: "assinatura", label: "Assinatura",          icone: "fa-solid fa-star",    visivel: true },
            { id: "seguranca",  label: "Segurança",           icone: "fa-solid fa-shield-halved", visivel: computed(() => podeEditar.value) },
        ],
    },
    {
        label: "Faturamento",
        secoes: [
            { id: "financeiro", label: "Financeiro", icone: "fa-solid fa-hand-holding-dollar", visivel: true },
            { id: "convenios",  label: "Convênios",  icone: "fa-solid fa-handshake",           visivel: podeVerConvenios },
        ],
    },
]

// ─── Busca client-side no sub-nav ──────────────────────────────────────────────
const buscaNav = ref("")

const gruposFiltrados = computed(() => {
    const q = buscaNav.value.trim().toLowerCase()
    return GRUPOS_NAV.map(g => ({
        ...g,
        secoes: g.secoes.filter(s => {
            const visivel = s.visivel === true ? true : s.visivel.value
            if (!visivel) return false
            if (!q) return true
            return s.label.toLowerCase().includes(q)
        }),
    })).filter(g => g.secoes.length > 0)
})

// ─── Seção ativa (deep-link bidirecional com ?secao=) ─────────────────────────
const TODAS_SECOES: SecaoId[] = [
    "dados", "funcionamento", "unidades", "reparticoes",
    "modelos-prontuario", "termos", "variaveis",
    "automacoes", "ia", "assinatura", "seguranca",
    "financeiro", "convenios",
]

function secaoValida(s: string | null | undefined): s is SecaoId {
    return TODAS_SECOES.includes(s as SecaoId)
}

function secaoPermitida(id: SecaoId): boolean {
    switch (id) {
        case "termos":             return podeVerTermos.value
        case "ia":                 return podeVerIa.value
        case "modelos-prontuario": return podeVerModelos.value
        case "automacoes":         return podeVerAutomacoes.value
        case "convenios":          return podeVerConvenios.value
        default:                   return true
    }
}

function resolverSecaoInicial(): SecaoId {
    const q = route.query.secao as string | undefined
    if (secaoValida(q) && secaoPermitida(q)) return q
    return "dados"
}

const secaoAtiva = ref<SecaoId>(resolverSecaoInicial())

// Sincroniza seção → URL (replace para não poluir histórico)
watch(secaoAtiva, (s) => {
    const q = route.query.secao
    if (q !== s) {
        router.replace({ query: { ...route.query, secao: s } })
    }
})

// Sincroniza URL → seção (navegação por link externo / botão voltar)
watch(() => route.query.secao, (q) => {
    const s = q as string | undefined
    if (secaoValida(s) && secaoPermitida(s) && s !== secaoAtiva.value) {
        secaoAtiva.value = s
    } else if (!secaoValida(s)) {
        secaoAtiva.value = "dados"
    }
})

function navegarPara(id: SecaoId) {
    if (!secaoPermitida(id)) return
    secaoAtiva.value = id
}

// ─── Dados do estabelecimento (aba Dados) ─────────────────────────────────────
const carregando = ref(false)
const salvando   = ref(false)
const erro       = ref<string | null>(null)
const msg        = ref<string | null>(null)
const estab      = ref<Estabelecimento | null>(null)

const nomeFantasia = ref("")
const razaoSocial  = ref("")
const cnpj         = ref("")
const telefone     = ref("")
const endereco     = ref("")
const cidade       = ref("")
const estado       = ref("")

const UFS_BRASIL = [
    { value: "", label: "—" },
    { value: "AC", label: "AC - Acre" }, { value: "AL", label: "AL - Alagoas" },
    { value: "AP", label: "AP - Amapá" }, { value: "AM", label: "AM - Amazonas" },
    { value: "BA", label: "BA - Bahia" }, { value: "CE", label: "CE - Ceará" },
    { value: "DF", label: "DF - Distrito Federal" }, { value: "ES", label: "ES - Espírito Santo" },
    { value: "GO", label: "GO - Goiás" }, { value: "MA", label: "MA - Maranhão" },
    { value: "MT", label: "MT - Mato Grosso" }, { value: "MS", label: "MS - Mato Grosso do Sul" },
    { value: "MG", label: "MG - Minas Gerais" }, { value: "PA", label: "PA - Pará" },
    { value: "PB", label: "PB - Paraíba" }, { value: "PR", label: "PR - Paraná" },
    { value: "PE", label: "PE - Pernambuco" }, { value: "PI", label: "PI - Piauí" },
    { value: "RJ", label: "RJ - Rio de Janeiro" }, { value: "RN", label: "RN - Rio Grande do Norte" },
    { value: "RS", label: "RS - Rio Grande do Sul" }, { value: "RO", label: "RO - Rondônia" },
    { value: "RR", label: "RR - Roraima" }, { value: "SC", label: "SC - Santa Catarina" },
    { value: "SP", label: "SP - São Paulo" }, { value: "SE", label: "SE - Sergipe" },
    { value: "TO", label: "TO - Tocantins" },
] as const

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const todos = await estabelecimentoService.listarMeus()
        const ativoId = tenant.ativo?.id
        const atual = todos.find(e => e.id === ativoId) ?? todos[0] ?? null
        estab.value = atual
        if (atual) {
            nomeFantasia.value = atual.nomeFantasia
            razaoSocial.value  = atual.razaoSocial ?? ""
            cnpj.value         = atual.cnpj ?? ""
            telefone.value     = atual.telefone ?? ""
            endereco.value     = atual.endereco ?? ""
            cidade.value       = atual.cidade ?? ""
            estado.value       = atual.estado ?? ""
        }
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar dados."
    } finally {
        carregando.value = false
    }
}

async function salvar() {
    if (!estab.value) return
    salvando.value = true
    erro.value = null
    msg.value = null
    try {
        await estabelecimentoService.atualizar(estab.value.id, {
            nomeFantasia: nomeFantasia.value,
            razaoSocial:  razaoSocial.value || undefined,
            cnpj:         cnpj.value || undefined,
            telefone:     telefone.value || undefined,
            endereco:     endereco.value || undefined,
            cidade:       cidade.value || undefined,
            estado:       estado.value || undefined,
        })
        if (tenant.ativo && tenant.ativo.id === estab.value.id) {
            tenant.selecionar({
                id: tenant.ativo.id,
                nomeFantasia: nomeFantasia.value,
                papel: tenant.ativo.papel,
                permissoes: tenant.ativo.permissoes,
                permissoesExtras: tenant.ativo.permissoesExtras,
            })
        }
        msg.value = "Dados do estabelecimento atualizados."
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar."
    } finally {
        salvando.value = false
    }
}

// ─── Foto / logo do estabelecimento ──────────────────────────────────────────
const enviandoFoto    = ref(false)
const erroFoto        = ref<string | null>(null)
const toastFoto       = ref<{ texto: string; tipo: "success" | "error" } | null>(null)
const confirmarRemocao = ref(false)

async function aoEnviarFoto(arquivo: File) {
    if (!estab.value) return
    erroFoto.value = null
    enviandoFoto.value = true
    try {
        const reduzida = await redimensionarImagem(arquivo, 512, 0.85)
        const novaUrl = await estabelecimentoService.uploadFoto(estab.value.id, reduzida)
        estab.value = { ...estab.value, fotoUrl: novaUrl }
        invalidarCacheEstabelecimentoAtivo()
        toastFoto.value = { texto: "Foto atualizada com sucesso.", tipo: "success" }
    } catch (e: any) {
        erroFoto.value = e?.response?.data?.mensagem ?? "Não foi possível enviar a foto."
    } finally {
        enviandoFoto.value = false
    }
}

function aoErroValidacaoFoto(mensagem: string) {
    erroFoto.value = mensagem
}

function pedirRemocaoFoto() {
    erroFoto.value = null
    confirmarRemocao.value = true
}

async function confirmarRemocaoFoto() {
    if (!estab.value) return
    enviandoFoto.value = true
    try {
        await estabelecimentoService.removerFoto(estab.value.id)
        estab.value = { ...estab.value, fotoUrl: null }
        invalidarCacheEstabelecimentoAtivo()
        confirmarRemocao.value = false
        toastFoto.value = { texto: "Foto removida.", tipo: "success" }
    } catch (e: any) {
        erroFoto.value = e?.response?.data?.mensagem ?? "Não foi possível remover a foto."
    } finally {
        enviandoFoto.value = false
    }
}

// ─── Segurança — toggle exigir 2FA do Dono ───────────────────────────────────
const salvandoExigir2fa = ref(false)
const erroExigir2fa     = ref<string | null>(null)

async function toggleExigirDono2fa() {
    if (!estab.value || !podeEditar.value) return
    const novoValor = !estab.value.exigirDono2fa
    salvandoExigir2fa.value = true
    erroExigir2fa.value = null
    try {
        await auth2faService.atualizarExigirDono2fa(estab.value.id, novoValor)
        estab.value = { ...estab.value, exigirDono2fa: novoValor }
    } catch (e: any) {
        erroExigir2fa.value = e?.response?.data?.mensagem ?? "Não foi possível atualizar a configuração."
    } finally {
        salvandoExigir2fa.value = false
    }
}

onMounted(async () => {
    podeEditar.value = tenant.papel === "Dono"
    await carregar()
})
</script>

<template>
    <div class="app-page app-page--wide estab-config">
        <AppPageHeader
            titulo="Configurações do estabelecimento"
            subtitulo="Gerencie dados, funcionamento, recursos e assinatura do estabelecimento."
        />

        <div class="md-layout">
            <!-- ══ SUB-NAV (~248px) ══════════════════════════════════════════ -->
            <nav class="md-subnav" aria-label="Seções de configuração">
                <div class="subnav-busca">
                    <AppSearchInput v-model="buscaNav" placeholder="Buscar seção…" />
                </div>

                <div v-for="grupo in gruposFiltrados" :key="grupo.label" class="subnav-grupo">
                    <p class="subnav-grupo-label">{{ grupo.label }}</p>
                    <ul class="subnav-lista">
                        <li v-for="s in grupo.secoes" :key="s.id">
                            <button
                                type="button"
                                class="subnav-item"
                                :class="{ ativo: secaoAtiva === s.id }"
                                @click="navegarPara(s.id)"
                            >
                                <i :class="[s.icone, 'subnav-icone']" aria-hidden="true" />
                                <span>{{ s.label }}</span>
                            </button>
                        </li>
                    </ul>
                </div>

                <div v-if="gruposFiltrados.length === 0" class="subnav-sem-resultado">
                    Nenhuma seção encontrada.
                </div>
            </nav>

            <!-- ══ PAINEL DE DETALHE ══════════════════════════════════════════ -->
            <div class="md-painel">
                <!-- ── Dados ─────────────────────────────────────────────── -->
                <section v-if="secaoAtiva === 'dados'" class="painel-secao">
                    <header class="secao-head">
                        <h2 class="ds-section-title">Dados do estabelecimento</h2>
                        <p class="secao-head-sub">Informações que aparecem nos PDFs de receita, prontuário, orçamentos e relatórios.</p>
                    </header>
                    <div v-if="carregando" class="estado-msg">Carregando...</div>

                    <div v-else-if="!estab" class="estado-msg">Nenhum estabelecimento selecionado.</div>

                    <div v-else class="card">
                        <p v-if="!podeEditar" class="aviso-somente-leitura">
                            Apenas o dono pode alterar estes dados. Você está visualizando em modo leitura.
                        </p>

                        <AppPhotoUpload
                            :foto-url="estab.fotoUrl"
                            :iniciais-fallback="estab.nomeFantasia"
                            titulo="Logo do estabelecimento"
                            descricao="Aparece nos PDFs de receita, prontuário, orçamentos e relatórios. JPG, PNG, WebP ou GIF até 2 MB. Recomendado: imagem quadrada de 400×400px."
                            :loading="enviandoFoto"
                            :disabled="!podeEditar"
                            motivo-disabled="Apenas o dono pode alterar a foto."
                            :erro="erroFoto"
                            @upload="aoEnviarFoto"
                            @remover="pedirRemocaoFoto"
                            @erro-validacao="aoErroValidacaoFoto"
                        />

                        <div class="separador-foto" />

                        <div class="grade-2">
                            <div class="campo">
                                <label class="field-label">Nome fantasia <span class="obrig">*</span></label>
                                <input v-model="nomeFantasia" class="input-field" :disabled="!podeEditar" />
                            </div>
                            <div class="campo">
                                <label class="field-label">Razão social</label>
                                <input v-model="razaoSocial" class="input-field" :disabled="!podeEditar" />
                            </div>
                        </div>

                        <div class="grade-2">
                            <div class="campo">
                                <label class="field-label">CNPJ</label>
                                <input
                                    v-model="cnpj"
                                    v-maska="'##.###.###/####-##'"
                                    class="input-field"
                                    placeholder="00.000.000/0000-00"
                                    :disabled="!podeEditar"
                                />
                            </div>
                            <div class="campo">
                                <label class="field-label">Telefone</label>
                                <input
                                    v-model="telefone"
                                    v-maska="'(##) #####-####'"
                                    class="input-field"
                                    type="tel"
                                    placeholder="(00) 00000-0000"
                                    :disabled="!podeEditar"
                                />
                            </div>
                        </div>

                        <div class="campo">
                            <label class="field-label">Endereço</label>
                            <input
                                v-model="endereco"
                                class="input-field"
                                placeholder="Rua, número, bairro"
                                :disabled="!podeEditar"
                            />
                        </div>

                        <div class="grade-cidade-uf">
                            <div class="campo">
                                <label class="field-label">Cidade</label>
                                <input
                                    v-model="cidade"
                                    class="input-field"
                                    maxlength="100"
                                    placeholder="Ex.: São Paulo"
                                    :disabled="!podeEditar"
                                />
                            </div>
                            <div class="campo">
                                <label class="field-label">Estado / UF</label>
                                <select
                                    v-model="estado"
                                    class="input-field"
                                    :disabled="!podeEditar"
                                >
                                    <option v-for="uf in UFS_BRASIL" :key="uf.value" :value="uf.value">
                                        {{ uf.label }}
                                    </option>
                                </select>
                            </div>
                        </div>

                        <p v-if="erro" class="msg-erro">{{ erro }}</p>
                        <p v-if="msg"  class="msg-ok">{{ msg }}</p>

                        <div class="card-footer">
                            <AppButton
                                :disabled="salvando || !podeEditar || !nomeFantasia.trim()"
                                :loading="salvando"
                                @click="salvar"
                            >{{ salvando ? "Salvando..." : "Salvar alterações" }}</AppButton>
                        </div>
                    </div>
                </section>

                <!-- ── Funcionamento ──────────────────────────────────────── -->
                <section v-else-if="secaoAtiva === 'funcionamento'" class="painel-secao">
                    <header class="secao-head">
                        <h2 class="ds-section-title">Funcionamento</h2>
                        <p class="secao-head-sub">Defina os horários e dias em que o estabelecimento está disponível para agendamento.</p>
                    </header>
                    <div v-if="carregando" class="estado-msg">Carregando...</div>
                    <div v-else-if="!estab" class="estado-msg">Nenhum estabelecimento selecionado.</div>
                    <FuncionamentoTab
                        v-else
                        :estabelecimento="estab"
                        :pode-editar="podeEditar"
                        @atualizado="carregar"
                    />
                </section>

                <!-- ── Unidades ───────────────────────────────────────────── -->
                <section v-else-if="secaoAtiva === 'unidades'" class="painel-secao">
                    <header class="secao-head">
                        <h2 class="ds-section-title">Unidades</h2>
                        <p class="secao-head-sub">Cadastre as unidades físicas do estabelecimento.</p>
                    </header>
                    <div v-if="!estab" class="estado-msg">Nenhum estabelecimento selecionado.</div>
                    <UnidadesTab
                        v-else
                        :estabelecimento-id="estab.id"
                        :pode-editar="podeEditar"
                    />
                </section>

                <!-- ── Repartições ────────────────────────────────────────── -->
                <section v-else-if="secaoAtiva === 'reparticoes'" class="painel-secao">
                    <header class="secao-head">
                        <h2 class="ds-section-title">Repartições</h2>
                        <p class="secao-head-sub">Organize salas, consultórios e setores dentro de cada unidade.</p>
                    </header>
                    <div v-if="!estab" class="estado-msg">Nenhum estabelecimento selecionado.</div>
                    <ReparticoesTab
                        v-else
                        :estabelecimento-id="estab.id"
                        :pode-editar="podeEditar"
                    />
                </section>

                <!-- ── Modelos de prontuário (lazy) ───────────────────────── -->
                <section v-else-if="secaoAtiva === 'modelos-prontuario'" class="painel-secao">
                    <div v-if="!podeVerModelos" class="estado-sem-permissao">
                        <i class="fa-solid fa-lock" />
                        <p>Você não tem permissão para acessar esta seção.</p>
                    </div>
                    <PainelModelosPront v-else />
                </section>

                <!-- ── Termos de consentimento (lazy) ─────────────────────── -->
                <section v-else-if="secaoAtiva === 'termos'" class="painel-secao">
                    <div v-if="!podeVerTermos" class="estado-sem-permissao">
                        <i class="fa-solid fa-lock" />
                        <p>Você não tem permissão para acessar esta seção.</p>
                    </div>
                    <PainelTermos v-else />
                </section>

                <!-- ── Listas de variáveis ────────────────────────────────── -->
                <section v-else-if="secaoAtiva === 'variaveis'" class="painel-secao">
                    <header class="secao-head">
                        <h2 class="ds-section-title">Listas de variáveis</h2>
                        <p class="secao-head-sub">Padronize as opções que aparecem nos formulários do prontuário. Configure uma vez e toda a equipe usa os mesmos valores.</p>
                    </header>
                    <ListasVariaveisTab :pode-editar="podeEditar" />
                </section>

                <!-- ── Automações (lazy) ──────────────────────────────────── -->
                <section v-else-if="secaoAtiva === 'automacoes'" class="painel-secao">
                    <div v-if="!podeVerAutomacoes" class="estado-sem-permissao">
                        <i class="fa-solid fa-lock" />
                        <p>Você não tem permissão para acessar esta seção.</p>
                    </div>
                    <PainelAutomacoes v-else />
                </section>

                <!-- ── Configurações de IA (lazy) ─────────────────────────── -->
                <section v-else-if="secaoAtiva === 'ia'" class="painel-secao">
                    <div v-if="!podeVerIa" class="estado-sem-permissao">
                        <i class="fa-solid fa-lock" />
                        <p>Você não tem permissão para acessar esta seção.</p>
                    </div>
                    <PainelIa v-else />
                </section>

                <!-- ── Assinatura (lazy) ──────────────────────────────────── -->
                <section v-else-if="secaoAtiva === 'assinatura'" class="painel-secao">
                    <PainelAssinatura />
                </section>

                <!-- ── Financeiro — tabela de preços + taxa (lazy) ────────── -->
                <section v-else-if="secaoAtiva === 'financeiro'" class="painel-secao">
                    <PainelFinanceiro />
                </section>

                <!-- ── Convênios — CRUD de convênios e planos (lazy) ─────── -->
                <section v-else-if="secaoAtiva === 'convenios'" class="painel-secao">
                    <PainelConvenios />
                </section>

                <!-- ── Segurança — 2FA do Dono ────────────────────────────── -->
                <section v-else-if="secaoAtiva === 'seguranca'" class="painel-secao">
                    <header class="secao-head">
                        <h2 class="ds-section-title">Segurança</h2>
                        <p class="secao-head-sub">Configure requisitos de segurança para o acesso ao estabelecimento.</p>
                    </header>

                    <div class="config-card">
                        <div class="config-row">
                            <div class="config-info">
                                <strong class="config-label">Exigir 2FA do Dono</strong>
                                <p class="config-desc">
                                    Quando ativado, o dono do estabelecimento é obrigado a configurar
                                    a verificação em duas etapas antes de acessar o sistema.
                                </p>
                            </div>
                            <div class="config-toggle">
                                <button
                                    type="button"
                                    :class="['toggle-btn', estab?.exigirDono2fa ? 'toggle-btn--on' : 'toggle-btn--off']"
                                    :disabled="salvandoExigir2fa || !podeEditar"
                                    :aria-label="estab?.exigirDono2fa ? 'Desativar exigência de 2FA' : 'Ativar exigência de 2FA'"
                                    @click="toggleExigirDono2fa"
                                >
                                    <span class="toggle-thumb"></span>
                                </button>
                                <span class="toggle-label-status">
                                    {{ estab?.exigirDono2fa ? "Ativado" : "Desativado" }}
                                </span>
                            </div>
                        </div>
                        <p v-if="erroExigir2fa" class="msg-erro-inline">{{ erroExigir2fa }}</p>
                    </div>
                </section>
            </div>
        </div>

        <!-- Confirmação destrutiva da remoção de foto -->
        <AppConfirmDialog
            v-model:aberto="confirmarRemocao"
            titulo="Remover foto?"
            mensagem="A logo será removida dos PDFs e relatórios do estabelecimento. Você pode enviar outra a qualquer momento."
            confirmar-rotulo="Remover"
            variante="danger"
            icone="fa-solid fa-trash"
            :executando="enviandoFoto"
            @confirmar="confirmarRemocaoFoto"
        />

        <AppToast
            v-if="toastFoto"
            :mensagem="toastFoto.texto"
            :variante="toastFoto.tipo"
            @fechar="toastFoto = null"
        />
    </div>
</template>

<style scoped>
/* ─── Layout master-detail ──────────────────────────────────────────────────── */
.md-layout {
    display: grid;
    grid-template-columns: 248px 1fr;
    gap: 1.5rem;
    align-items: start;
}

/* O header externo (PageHeader) traz mb-6 próprio; somado ao gap do app-page
   sobrava muito espaço acima do menu/painel. Zeramos o mb-6 do header externo
   — o gap do app-page (1.5rem) já separa o header do conteúdo. */
.estab-config > :deep(.mb-6:first-child) {
    margin-bottom: 0;
}

@media (max-width: 860px) {
    .md-layout {
        grid-template-columns: 1fr;
    }
}

/* ─── Sub-nav ───────────────────────────────────────────────────────────────── */
.md-subnav {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    padding: 0.75rem 0.5rem;
    position: sticky;
    top: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.subnav-busca {
    padding: 0 0.25rem 0.5rem;
    border-bottom: 1px solid hsl(var(--border));
    margin-bottom: 0.25rem;
}
/* AppSearchInput tem min-width: 280px (contexto de filtros de lista largos);
   no sub-nav de 248px isso transborda — neutralizamos só aqui. */
.subnav-busca :deep(.app-search-input) {
    min-width: 0;
}

.subnav-grupo { margin-top: 0.5rem; }
.subnav-grupo-label {
    font-size: 0.72em;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: hsl(var(--muted-foreground));
    padding: 0 0.75rem;
    margin: 0 0 0.25rem;
}

.subnav-lista { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 1px; }

.subnav-item {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    width: 100%;
    padding: 0.5rem 0.75rem;
    border-radius: calc(var(--radius) - 2px);
    border: none;
    background: transparent;
    color: hsl(var(--foreground) / 0.75);
    font-size: 0.875em;
    font-family: inherit;
    font-weight: 500;
    cursor: pointer;
    text-align: left;
    transition: background 0.12s, color 0.12s;
}
.subnav-item:hover {
    background: hsl(var(--muted));
    color: hsl(var(--foreground));
}
.subnav-item.ativo {
    background: hsl(var(--primary) / 0.1);
    color: hsl(var(--primary));
    font-weight: 600;
}
.subnav-item.ativo .subnav-icone { color: hsl(var(--primary)); }

.subnav-icone { width: 16px; text-align: center; font-size: 0.85em; flex-shrink: 0; }

.subnav-sem-resultado {
    font-size: 0.82em;
    color: hsl(var(--muted-foreground));
    text-align: center;
    padding: 1rem;
}

/* ─── Painel de detalhe ─────────────────────────────────────────────────────── */
.md-painel { min-width: 0; }

/* As views reusadas (Assinatura, IA, Automações, Modelos) trazem o wrapper
   `.app-page` (max-width + margin:0 auto + padding de página). Aninhado no
   painel isso centraliza o conteúdo e cria vãos enormes — a página de
   Configurações já é o `.app-page` externo. Neutralizamos o chrome interno
   para o conteúdo preencher o painel. */
.md-painel :deep(.app-page) {
    max-width: none;
    margin: 0;
    padding: 0;
    gap: 1.25rem;
}
/* O PageHeader das views reusadas traz mb-6 (24px) próprio; somado ao gap do
   app-page, dobrava o espaço header→conteúdo dentro do painel. Zeramos só o
   mb-6 do header (1º filho) — o gap do painel cuida do ritmo. Mantém o header
   e seus botões de ação (ex.: "Ver planos" na Assinatura). */
.md-painel :deep(.app-page > .mb-6:first-child) {
    margin-bottom: 0;
}
/* Igualar o tamanho do título dos painéis reusados ao contexto de painel interno.
   PageHeader agora usa --text-3xl (30px); dentro dos painéis embedded, reduz para
   se adequar ao container menor. */
.md-painel :deep(.app-page > .mb-6:first-child h1) {
    font-size: var(--text-xl);
    line-height: var(--line-height-tight);
}

.painel-secao { animation: fadein 0.18s ease-out; }

/* Cabeçalho padronizado das seções nativas (espelha o PageHeader das views
   internalizadas: título de seção 21px + subtítulo suave). */
.secao-head { margin-bottom: 1.25rem; }
.secao-head-sub {
    font-size: var(--text-base);
    color: hsl(var(--secondary) / 0.6);
    margin: 0.25rem 0 0;
    max-width: 70ch;
}
@keyframes fadein {
    from { opacity: 0; transform: translateY(4px); }
    to   { opacity: 1; transform: translateY(0); }
}

.estado-msg {
    text-align: center; color: hsl(var(--muted-foreground));
    padding: 3rem 1rem; font-size: 0.9em;
}

.estado-sem-permissao {
    display: flex; flex-direction: column; align-items: center;
    gap: 0.75rem; padding: 3rem 1rem;
    color: hsl(var(--muted-foreground)); font-size: 0.9em;
    text-align: center;
}
.estado-sem-permissao i { font-size: 2rem; opacity: 0.4; }
.estado-sem-permissao p { margin: 0; }

/* ─── Card padrão (seção Dados) ─────────────────────────────────────────────── */
.card {
    background: hsl(var(--card)); border: 1px solid hsl(var(--border));
    border-radius: var(--radius); padding: 1.75rem;
    display: flex; flex-direction: column; gap: 1.25rem;
}
.card-footer { display: flex; justify-content: flex-end; }

.aviso-somente-leitura {
    background: #fef3c7; color: #92400e; padding: 0.65rem 0.9rem;
    border-radius: var(--radius); font-size: 0.82em; margin: 0;
}

.separador-foto { height: 1px; background: hsl(var(--border)); margin: 0.25rem 0; }

.grade-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.grade-cidade-uf { display: grid; grid-template-columns: 1fr 220px; gap: 1rem; }

.campo       { display: flex; flex-direction: column; gap: 0.3rem; }
.obrig       { color: hsl(var(--destructive)); }

.input-field {
    padding: 0.5rem 0.75rem; border: 1px solid hsl(var(--border-strong, var(--border)));
    border-radius: var(--radius); font-family: inherit; font-size: 0.875em;
    background: hsl(var(--card)); color: hsl(var(--foreground));
}
.input-field:focus    { outline: none; border-color: hsl(var(--primary)); }
.input-field:disabled { background: hsl(var(--muted)); color: hsl(var(--muted-foreground)); cursor: not-allowed; }

.msg-erro { color: hsl(var(--destructive)); font-size: 0.875em; margin: 0; }
.msg-ok   { color: hsl(var(--success, 142 76% 36%)); font-size: 0.875em; margin: 0; }

@media (max-width: 860px) {
    .grade-2 { grid-template-columns: 1fr; }
    .grade-cidade-uf { grid-template-columns: 1fr; }
    .md-subnav { position: static; }
}

/* ─── Card de configuração (seção Segurança) ────────────────────────────────── */
.config-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    padding: 1.25rem 1.5rem;
}

.config-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 2rem;
}

.config-info {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
    min-width: 0;
}

.config-label {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: var(--text);
}

.config-desc {
    font-size: var(--text-xs);
    color: var(--text-muted);
    margin: 0;
    line-height: 1.5;
}

.config-toggle {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    flex-shrink: 0;
}

/* Toggle switch */
.toggle-btn {
    position: relative;
    width: 44px;
    height: 24px;
    border-radius: 99px;
    border: none;
    cursor: pointer;
    transition: background 0.2s;
    padding: 0;
    outline-offset: 2px;
}

.toggle-btn--on  { background: hsl(var(--primary)); }
.toggle-btn--off { background: hsl(var(--border-strong, 220 14% 70%)); }

.toggle-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.toggle-thumb {
    position: absolute;
    top: 3px;
    left: 3px;
    width: 18px;
    height: 18px;
    background: white;
    border-radius: 50%;
    box-shadow: 0 1px 3px hsl(0 0% 0% / 0.15);
    transition: transform 0.2s;
}

.toggle-btn--on .toggle-thumb {
    transform: translateX(20px);
}

.toggle-label-status {
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: var(--text-muted);
    min-width: 60px;
}

.msg-erro-inline {
    font-size: var(--text-xs);
    color: var(--danger);
    margin: 0.75rem 0 0;
}
</style>
