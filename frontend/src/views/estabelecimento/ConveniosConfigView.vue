<script setup lang="ts">
/**
 * ConveniosConfigView — CRUD de convênios e planos do estabelecimento (F6/R1-R4).
 * Carregada lazily pelo EstabelecimentoView (?secao=convenios).
 *
 * Layout:
 *   - Lista de convênios (tabela com badge Ativo/Inativo).
 *   - Botão "Novo convênio" → drawer de criação/edição.
 *   - Planos do convênio expandíveis inline (lista com botão de inativar/renomear).
 *
 * Soft-delete via Ativo=false (R3). Exclusão física só disponível se CA134 (sem uso).
 * RBAC: convenios.gerenciar para escrita (controlado pelo backend — 403 impede silenciosamente).
 */
import { ref, onMounted } from "vue"
import { AppButton, AppEmptyState, AppField, AppBadge, AppToast, AppDrawer, AppConfirmDialog } from "@/components/ui"
import { convenioService, type ConvenioListado, type ConvenioDetalhe } from "@/services/convenioService"
import { usePermissoesStore } from "@/stores/permissoesStore"

const permissoes = usePermissoesStore()
const podeGerenciar = () => permissoes.pode("convenios.gerenciar") || permissoes.ehDono

// ── Estado principal ──────────────────────────────────────────────────────────

const convenios = ref<ConvenioListado[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)

const toast = ref<{ texto: string; tipo: "success" | "error" } | null>(null)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        convenios.value = await convenioService.listar()
    } catch {
        erro.value = "Não foi possível carregar os convênios."
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)

// ── Drawer convênio (criar / editar) ─────────────────────────────────────────

const drawerAberto = ref(false)
const editandoId = ref<number | null>(null)
const formNome = ref("")
const formAns = ref("")
const formAtivo = ref(true)
const salvando = ref(false)
const erroForm = ref<string | null>(null)

function abrirNovoConvenio() {
    editandoId.value = null
    formNome.value = ""
    formAns.value = ""
    formAtivo.value = true
    erroForm.value = null
    drawerAberto.value = true
}

async function abrirEditarConvenio(id: number) {
    editandoId.value = id
    erroForm.value = null
    try {
        const det = await convenioService.obter(id)
        if (!det) return
        formNome.value = det.nome
        formAns.value = det.registroAns ?? ""
        formAtivo.value = det.ativo
        drawerAberto.value = true
    } catch {
        toast.value = { texto: "Não foi possível carregar dados do convênio.", tipo: "error" }
    }
}

async function salvarConvenio() {
    if (!formNome.value.trim()) {
        erroForm.value = "Nome é obrigatório."
        return
    }
    salvando.value = true
    erroForm.value = null
    try {
        const ans = formAns.value.trim() || null
        if (editandoId.value) {
            await convenioService.atualizar(editandoId.value, formNome.value.trim(), ans, formAtivo.value)
        } else {
            await convenioService.criar(formNome.value.trim(), ans)
        }
        drawerAberto.value = false
        toast.value = { texto: "Convênio salvo com sucesso.", tipo: "success" }
        await carregar()
    } catch (e: any) {
        erroForm.value = e?.response?.data?.detail ?? "Erro ao salvar convênio."
    } finally {
        salvando.value = false
    }
}

// ── Excluir convênio (CA134) ──────────────────────────────────────────────────

const confirmarExclusao = ref(false)
const excluindoId = ref<number | null>(null)
const excluindo = ref(false)

function solicitarExclusao(id: number) {
    excluindoId.value = id
    confirmarExclusao.value = true
}

async function confirmarExcluir() {
    if (!excluindoId.value) return
    excluindo.value = true
    try {
        await convenioService.excluir(excluindoId.value)
        toast.value = { texto: "Convênio excluído.", tipo: "success" }
        await carregar()
    } catch (e: any) {
        toast.value = {
            texto: e?.response?.data?.detail ?? "Não foi possível excluir. O convênio pode estar em uso.",
            tipo: "error",
        }
    } finally {
        excluindo.value = false
        confirmarExclusao.value = false
        excluindoId.value = null
    }
}

// ── Planos (expansão inline) ──────────────────────────────────────────────────

const detalheAberto = ref<Record<number, ConvenioDetalhe | null>>({})
const carregandoDetalhe = ref<Record<number, boolean>>({})

async function toggleDetalhe(id: number) {
    if (detalheAberto.value[id] !== undefined) {
        delete detalheAberto.value[id]
        return
    }
    carregandoDetalhe.value[id] = true
    try {
        detalheAberto.value[id] = await convenioService.obter(id)
    } catch {
        toast.value = { texto: "Não foi possível carregar planos.", tipo: "error" }
    } finally {
        carregandoDetalhe.value[id] = false
    }
}

const editandoPlanoNome = ref<Record<number, string>>({})
const novoPlanoNome = ref<Record<number, string>>({})
const salvandoPlano = ref<Record<number, boolean>>({})

async function adicionarPlano(convenioId: number) {
    const nome = novoPlanoNome.value[convenioId]?.trim()
    if (!nome) return
    salvandoPlano.value[convenioId] = true
    try {
        await convenioService.adicionarPlano(convenioId, nome)
        novoPlanoNome.value[convenioId] = ""
        detalheAberto.value[convenioId] = await convenioService.obter(convenioId)
        await carregar()
        toast.value = { texto: "Plano adicionado.", tipo: "success" }
    } catch (e: any) {
        toast.value = { texto: e?.response?.data?.detail ?? "Erro ao adicionar plano.", tipo: "error" }
    } finally {
        salvandoPlano.value[convenioId] = false
    }
}

async function inativarPlano(convenioId: number, planoId: number) {
    try {
        await convenioService.inativarPlano(convenioId, planoId)
        detalheAberto.value[convenioId] = await convenioService.obter(convenioId)
        toast.value = { texto: "Plano inativado.", tipo: "success" }
    } catch (e: any) {
        toast.value = { texto: e?.response?.data?.detail ?? "Erro ao inativar plano.", tipo: "error" }
    }
}

async function salvarNomePlano(convenioId: number, planoId: number) {
    const nome = editandoPlanoNome.value[planoId]?.trim()
    if (!nome) return
    try {
        await convenioService.atualizarPlano(convenioId, planoId, nome)
        delete editandoPlanoNome.value[planoId]
        detalheAberto.value[convenioId] = await convenioService.obter(convenioId)
        toast.value = { texto: "Plano atualizado.", tipo: "success" }
    } catch (e: any) {
        toast.value = { texto: e?.response?.data?.detail ?? "Erro ao atualizar plano.", tipo: "error" }
    }
}
</script>

<template>
    <div>
        <header class="secao-head">
            <div class="secao-head-left">
                <h2 class="ds-section-title">Convênios</h2>
                <p class="secao-head-sub">Cadastre os convênios e planos aceitos pelo estabelecimento.</p>
            </div>
            <AppButton v-if="podeGerenciar()" variante="primario" icone="fa-solid fa-plus" @click="abrirNovoConvenio">
                Novo convênio
            </AppButton>
        </header>

        <div v-if="carregando" class="convenios-loading">
            <span class="spinner-inline" aria-label="Carregando convênios…" />
        </div>

        <p v-else-if="erro" class="msg-erro-inline">{{ erro }}</p>

        <AppEmptyState
            v-else-if="!convenios.length"
            icone="fa-solid fa-handshake"
            titulo="Nenhum convênio cadastrado"
            descricao="Adicione os convênios aceitos pelo estabelecimento para habilitar atendimentos de convênio."
        />

        <div v-else class="convenios-lista">
            <div
                v-for="c in convenios"
                :key="c.id"
                class="convenio-card"
            >
                <div class="convenio-card-header">
                    <div class="convenio-card-info">
                        <strong class="convenio-nome">{{ c.nome }}</strong>
                        <span v-if="c.registroAns" class="convenio-ans">ANS {{ c.registroAns }}</span>
                        <AppBadge :variante="c.ativo ? 'sucesso' : 'neutro'">
                            {{ c.ativo ? "Ativo" : "Inativo" }}
                        </AppBadge>
                        <span class="convenio-planos-count">{{ c.totalPlanos }} plano(s)</span>
                    </div>
                    <div class="convenio-card-acoes">
                        <button
                            type="button"
                            class="btn-icon btn-icon-ver"
                            :aria-label="`${detalheAberto[c.id] !== undefined ? 'Ocultar' : 'Ver'} planos de ${c.nome}`"
                            @click="toggleDetalhe(c.id)"
                        >
                            <i :class="detalheAberto[c.id] !== undefined ? 'fa-solid fa-chevron-up' : 'fa-solid fa-chevron-down'" />
                        </button>
                        <button
                            v-if="podeGerenciar()"
                            type="button"
                            class="btn-icon btn-icon-editar"
                            :aria-label="`Editar ${c.nome}`"
                            @click="abrirEditarConvenio(c.id)"
                        >
                            <i class="fa-solid fa-pencil" />
                        </button>
                        <button
                            v-if="podeGerenciar()"
                            type="button"
                            class="btn-icon btn-icon-excluir"
                            :aria-label="`Excluir ${c.nome}`"
                            @click="solicitarExclusao(c.id)"
                        >
                            <i class="fa-solid fa-trash" />
                        </button>
                    </div>
                </div>

                <!-- Planos (expandível) -->
                <div v-if="detalheAberto[c.id] !== undefined" class="planos-area">
                    <div v-if="carregandoDetalhe[c.id]" class="planos-loading">
                        <span class="spinner-inline" aria-label="Carregando planos…" />
                    </div>
                    <template v-else-if="detalheAberto[c.id]">
                        <ul v-if="detalheAberto[c.id]!.planos.length" class="planos-lista">
                            <li
                                v-for="p in detalheAberto[c.id]!.planos"
                                :key="p.id"
                                class="plano-item"
                            >
                                <template v-if="editandoPlanoNome[p.id] !== undefined">
                                    <input
                                        v-model="editandoPlanoNome[p.id]"
                                        class="form-input plano-input-nome"
                                        @keyup.enter="salvarNomePlano(c.id, p.id)"
                                        @keyup.escape="delete editandoPlanoNome[p.id]"
                                    />
                                    <AppButton variante="primario" tamanho="sm" @click="salvarNomePlano(c.id, p.id)">
                                        Salvar
                                    </AppButton>
                                    <AppButton variante="ghost" tamanho="sm" @click="delete editandoPlanoNome[p.id]">
                                        Cancelar
                                    </AppButton>
                                </template>
                                <template v-else>
                                    <span :class="['plano-nome', !p.ativo && 'plano-nome--inativo']">{{ p.nome }}</span>
                                    <AppBadge v-if="!p.ativo" variante="neutro" tamanho="sm">Inativo</AppBadge>
                                    <button
                                        v-if="podeGerenciar()"
                                        type="button"
                                        class="btn-icon btn-icon-editar"
                                        :aria-label="`Renomear ${p.nome}`"
                                        @click="editandoPlanoNome[p.id] = p.nome"
                                    >
                                        <i class="fa-solid fa-pencil" />
                                    </button>
                                    <button
                                        v-if="p.ativo && podeGerenciar()"
                                        type="button"
                                        class="btn-icon btn-icon-excluir"
                                        :aria-label="`Inativar plano ${p.nome}`"
                                        @click="inativarPlano(c.id, p.id)"
                                    >
                                        <i class="fa-solid fa-ban" />
                                    </button>
                                </template>
                            </li>
                        </ul>
                        <p v-else class="planos-vazio">Nenhum plano cadastrado.</p>

                        <!-- Adicionar plano -->
                        <div class="plano-add-row">
                            <input
                                v-model="novoPlanoNome[c.id]"
                                class="form-input plano-input-nome"
                                placeholder="Nome do novo plano"
                                @keyup.enter="adicionarPlano(c.id)"
                            />
                            <AppButton
                                variante="secundario"
                                tamanho="sm"
                                :executando="salvandoPlano[c.id]"
                                @click="adicionarPlano(c.id)"
                            >
                                Adicionar plano
                            </AppButton>
                        </div>
                    </template>
                </div>
            </div>
        </div>

        <!-- Drawer criar/editar convênio -->
        <AppDrawer v-model:aberto="drawerAberto" :titulo="editandoId ? 'Editar convênio' : 'Novo convênio'">
            <div class="convenio-form">
                <AppField label="Nome do convênio" required>
                    <input
                        v-model="formNome"
                        class="form-input"
                        placeholder="Ex: Unimed, SulAmérica…"
                        maxlength="100"
                    />
                </AppField>

                <AppField label="Registro ANS (opcional)">
                    <input
                        v-model="formAns"
                        class="form-input"
                        placeholder="Número do registro ANS"
                        maxlength="20"
                    />
                </AppField>

                <div v-if="editandoId" class="form-row-toggle">
                    <label class="form-label">Status</label>
                    <div class="toggle-grupo">
                        <button
                            type="button"
                            :class="['toggle-btn', formAtivo ? 'toggle-btn--on' : 'toggle-btn--off']"
                            :aria-label="formAtivo ? 'Desativar convênio' : 'Ativar convênio'"
                            @click="formAtivo = !formAtivo"
                        >
                            <span class="toggle-thumb" />
                        </button>
                        <span class="toggle-label-status">{{ formAtivo ? "Ativo" : "Inativo" }}</span>
                    </div>
                </div>

                <p v-if="erroForm" class="msg-erro-inline">{{ erroForm }}</p>

                <div class="form-actions">
                    <AppButton variante="ghost" @click="drawerAberto = false">Cancelar</AppButton>
                    <AppButton variante="primario" :executando="salvando" @click="salvarConvenio">
                        {{ editandoId ? "Salvar alterações" : "Criar convênio" }}
                    </AppButton>
                </div>
            </div>
        </AppDrawer>

        <!-- Confirmação de exclusão -->
        <AppConfirmDialog
            v-model:aberto="confirmarExclusao"
            titulo="Excluir convênio?"
            mensagem="Esta ação não pode ser desfeita. Se o convênio tiver carteirinhas ou cobranças, você deve inativá-lo em vez de excluir."
            confirmar-rotulo="Excluir"
            variante="danger"
            icone="fa-solid fa-trash"
            :executando="excluindo"
            @confirmar="confirmarExcluir"
        />

        <AppToast
            v-if="toast"
            :mensagem="toast.texto"
            :variante="toast.tipo"
            @fechar="toast = null"
        />
    </div>
</template>

<style scoped>
.secao-head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 1.5rem;
}

.secao-head-left { flex: 1; }

.convenios-loading,
.planos-loading {
    display: flex;
    justify-content: center;
    padding: 2rem;
}

.convenios-lista {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.convenio-card {
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    background: var(--color-surface);
    overflow: hidden;
}

.convenio-card-header {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 0.875rem 1rem;
}

.convenio-card-info {
    flex: 1;
    display: flex;
    align-items: center;
    gap: 0.625rem;
    flex-wrap: wrap;
}

.convenio-nome {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: var(--color-text-primary);
}

.convenio-ans {
    font-size: var(--text-xs);
    color: var(--color-text-secondary);
}

.convenio-planos-count {
    font-size: var(--text-xs);
    color: var(--color-text-muted);
}

.convenio-card-acoes {
    display: flex;
    gap: 0.25rem;
}

/* Planos expandidos */
.planos-area {
    border-top: 1px solid var(--color-border);
    background: var(--color-bg);
    padding: 0.75rem 1rem;
}

.planos-lista {
    list-style: none;
    margin: 0 0 0.75rem;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
}

.plano-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.plano-nome {
    flex: 1;
    font-size: var(--text-sm);
    color: var(--color-text-primary);
}

.plano-nome--inativo {
    color: var(--color-text-muted);
    text-decoration: line-through;
}

.plano-input-nome {
    flex: 1;
}

.planos-vazio {
    font-size: var(--text-sm);
    color: var(--color-text-muted);
    margin: 0 0 0.75rem;
}

.plano-add-row {
    display: flex;
    gap: 0.5rem;
    align-items: center;
}

/* Formulário no drawer */
.convenio-form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.form-row-toggle {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
}

.toggle-grupo {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.form-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
    margin-top: 0.5rem;
}

/* Spinner reutilizado */
.spinner-inline {
    display: inline-block;
    width: 1.25rem;
    height: 1.25rem;
    border: 2px solid var(--color-border);
    border-top-color: var(--color-primary);
    border-radius: 50%;
    animation: spin 0.7s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }
</style>
