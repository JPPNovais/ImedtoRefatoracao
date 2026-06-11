<script setup lang="ts">
/**
 * Aba Convênios do detalhe do paciente (F6/R11-R15).
 *
 * Exibe carteirinhas de convênio do paciente neste estabelecimento.
 * Lazy: dispara HTTP na primeira vez que `ativa` passa a true (CA154).
 * LGPD: audit de acesso (Leitura) registrado no handler (CA151).
 *
 * Regras:
 *  - Validade vencida → alerta visual na carteirinha (CA141).
 *  - RBAC pacientes.ver (leitura) / pacientes.editar (escrita). Gate via permissoesStore.
 *  - "Em breve": cards Coparticipação / Conciliação / Glosas (CA155).
 */
import { ref, watch } from "vue"
import { AppEmptyState, AppButton, AppBadge, AppDrawer, AppField, AppConfirmDialog, AppToast } from "@/components/ui"
import {
    convenioService,
    estaVencida,
    type PacienteConvenioDto,
    type ConvenioSelect,
} from "@/services/convenioService"
import { usePermissoesStore } from "@/stores/permissoesStore"

const props = defineProps<{
    pacienteId: number
    ativa: boolean
}>()

// ── Permissões ────────────────────────────────────────────────────────────────

const permissoes = usePermissoesStore()
const podeEditar = () => permissoes.pode("pacientes.editar") || permissoes.ehDono

// ── Estado ─────────────────────────────────────────────────────────────────────

const carteirinhas = ref<PacienteConvenioDto[]>([])
const conveniosDisponiveis = ref<ConvenioSelect[]>([])
const carregando = ref(false)
const carregado = ref(false)
const toast = ref<{ texto: string; tipo: "success" | "error" } | null>(null)

async function carregar() {
    if (carregado.value) return
    carregando.value = true
    try {
        const [c, conv] = await Promise.all([
            convenioService.listarCarteirinhasPaciente(props.pacienteId),
            convenioService.listarAtivos(),
        ])
        carteirinhas.value = c
        conveniosDisponiveis.value = conv
        carregado.value = true
    } catch {
        // silencioso — AppEmptyState já trata o vazio
    } finally {
        carregando.value = false
    }
}

// CA154: lazy — carrega só quando a aba é aberta pela primeira vez
watch(() => props.ativa, (ativa) => { if (ativa) void carregar() }, { immediate: true })

// ── Drawer adicionar / editar carteirinha ────────────────────────────────────

interface FormCarteirinha {
    convenioId: number | null
    planoId: number | null
    numeroCarteirinha: string
    validade: string
    ativo: boolean
}

const drawerAberto = ref(false)
const editandoId = ref<number | null>(null)
const form = ref<FormCarteirinha>({ convenioId: null, planoId: null, numeroCarteirinha: "", validade: "", ativo: true })
const salvando = ref(false)
const erroForm = ref<string | null>(null)

const planosDoConvenioSelecionado = () => {
    if (!form.value.convenioId) return []
    return conveniosDisponiveis.value.find(c => c.id === form.value.convenioId)?.planos.filter(p => p.ativo) ?? []
}

function abrirNovaCarteirinha() {
    editandoId.value = null
    form.value = { convenioId: null, planoId: null, numeroCarteirinha: "", validade: "", ativo: true }
    erroForm.value = null
    drawerAberto.value = true
}

function abrirEditarCarteirinha(c: PacienteConvenioDto) {
    editandoId.value = c.id
    form.value = {
        convenioId: c.convenioId,
        planoId: c.planoId,
        numeroCarteirinha: c.numeroCarteirinha,
        validade: c.validade ?? "",
        ativo: c.ativo,
    }
    erroForm.value = null
    drawerAberto.value = true
}

async function salvarCarteirinha() {
    if (!form.value.convenioId) {
        erroForm.value = "Selecione o convênio."
        return
    }
    if (!form.value.numeroCarteirinha.trim()) {
        erroForm.value = "Número da carteirinha é obrigatório."
        return
    }
    salvando.value = true
    erroForm.value = null
    try {
        const payload = {
            convenioId: form.value.convenioId,
            planoId: form.value.planoId || null,
            numeroCarteirinha: form.value.numeroCarteirinha.trim(),
            validade: form.value.validade || null,
        }
        if (editandoId.value) {
            await convenioService.atualizarCarteirinha(props.pacienteId, editandoId.value, {
                ...payload,
                ativo: form.value.ativo,
            })
        } else {
            await convenioService.criarCarteirinha(props.pacienteId, payload)
        }
        drawerAberto.value = false
        toast.value = { texto: "Carteirinha salva.", tipo: "success" }
        carregado.value = false
        await carregar()
    } catch (e: any) {
        erroForm.value = e?.response?.data?.detail ?? "Erro ao salvar carteirinha."
    } finally {
        salvando.value = false
    }
}

// ── Excluir carteirinha ───────────────────────────────────────────────────────

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
        await convenioService.excluirCarteirinha(props.pacienteId, excluindoId.value)
        toast.value = { texto: "Carteirinha removida.", tipo: "success" }
        carregado.value = false
        await carregar()
    } catch (e: any) {
        toast.value = {
            texto: e?.response?.data?.detail ?? "Não foi possível remover a carteirinha.",
            tipo: "error",
        }
    } finally {
        excluindo.value = false
        confirmarExclusao.value = false
        excluindoId.value = null
    }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function validadeLabel(validade: string | null) {
    if (!validade) return null
    // validade vem no formato "YYYY-MM-DD" do JSON
    const [ano, mes, dia] = validade.split("-")
    return `${dia}/${mes}/${ano}`
}
</script>

<template>
    <div class="convenios-tab">
        <!-- Cabeçalho com botão de ação -->
        <header class="aba-header">
            <h3 class="ds-card-title">Carteirinhas de convênio</h3>
            <AppButton
                v-if="podeEditar()"
                variante="secundario"
                icone="fa-solid fa-plus"
                tamanho="sm"
                @click="abrirNovaCarteirinha"
            >
                Adicionar
            </AppButton>
        </header>

        <!-- Carregando -->
        <div v-if="carregando" class="tab-loading" aria-label="Carregando convênios…">
            <span class="spinner-inline" />
        </div>

        <!-- Vazio -->
        <AppEmptyState
            v-else-if="!carregando && carteirinhas.length === 0"
            icone="fa-solid fa-shield-halved"
            titulo="Nenhuma carteirinha cadastrada"
            descricao="Adicione as carteirinhas de convênio do paciente para facilitar o check-in."
        />

        <!-- Lista de carteirinhas -->
        <ul v-else class="carteirinhas-lista">
            <li
                v-for="c in carteirinhas"
                :key="c.id"
                class="carteirinha-item"
                :class="{ 'carteirinha-item--inativa': !c.ativo }"
            >
                <div class="carteirinha-dados">
                    <div class="carteirinha-convenio">
                        <strong>{{ c.convenioNome }}</strong>
                        <span v-if="c.planoNome" class="carteirinha-plano">— {{ c.planoNome }}</span>
                    </div>
                    <div class="carteirinha-info-row">
                        <span class="carteirinha-numero">Nº {{ c.numeroCarteirinha }}</span>
                        <span v-if="c.validade" class="carteirinha-validade">
                            Válida até {{ validadeLabel(c.validade) }}
                        </span>
                    </div>
                    <!-- CA141: alerta vencida — calculado no front a partir de `validade` (R6) -->
                    <div v-if="estaVencida(c.validade)" class="carteirinha-alerta" role="alert">
                        <i class="fa-solid fa-triangle-exclamation" aria-hidden="true" />
                        Carteirinha vencida — verifique a renovação com o paciente.
                    </div>
                </div>
                <div class="carteirinha-acoes">
                    <AppBadge :variante="c.ativo ? 'sucesso' : 'neutro'">
                        {{ c.ativo ? "Ativa" : "Inativa" }}
                    </AppBadge>
                    <button
                        v-if="podeEditar()"
                        type="button"
                        class="btn-icon btn-icon-editar"
                        :aria-label="`Editar carteirinha ${c.convenioNome}`"
                        @click="abrirEditarCarteirinha(c)"
                    >
                        <i class="fa-solid fa-pencil" />
                    </button>
                    <button
                        v-if="podeEditar()"
                        type="button"
                        class="btn-icon btn-icon-excluir"
                        :aria-label="`Remover carteirinha ${c.convenioNome}`"
                        @click="solicitarExclusao(c.id)"
                    >
                        <i class="fa-solid fa-trash" />
                    </button>
                </div>
            </li>
        </ul>

        <!-- Em breve: Coparticipação / Conciliação / Glosas (CA155) -->
        <div class="em-breve-cards">
            <div class="em-breve-card">
                <i class="fa-solid fa-percent em-breve-icone" aria-hidden="true" />
                <div>
                    <strong class="em-breve-titulo">Coparticipação</strong>
                    <p class="em-breve-desc">Controle de valores pagos pelo paciente por procedimento.</p>
                </div>
                <AppBadge variante="neutro">Em breve</AppBadge>
            </div>
            <div class="em-breve-card">
                <i class="fa-solid fa-arrows-left-right em-breve-icone" aria-hidden="true" />
                <div>
                    <strong class="em-breve-titulo">Conciliação</strong>
                    <p class="em-breve-desc">Conferência de valores pagos pelo convênio com os lançamentos.</p>
                </div>
                <AppBadge variante="neutro">Em breve</AppBadge>
            </div>
            <div class="em-breve-card">
                <i class="fa-solid fa-circle-exclamation em-breve-icone" aria-hidden="true" />
                <div>
                    <strong class="em-breve-titulo">Glosas</strong>
                    <p class="em-breve-desc">Itens não pagos pelo convênio com motivo e recurso.</p>
                </div>
                <AppBadge variante="neutro">Em breve</AppBadge>
            </div>
        </div>

        <!-- Drawer adicionar / editar carteirinha -->
        <AppDrawer
            v-model:aberto="drawerAberto"
            :titulo="editandoId ? 'Editar carteirinha' : 'Adicionar carteirinha'"
        >
            <div class="carteirinha-form">
                <AppField label="Convênio" required>
                    <select
                        v-model.number="form.convenioId"
                        class="form-input"
                        @change="form.planoId = null"
                    >
                        <option :value="null">Selecione o convênio</option>
                        <option v-for="c in conveniosDisponiveis" :key="c.id" :value="c.id">{{ c.nome }}</option>
                    </select>
                </AppField>

                <AppField v-if="planosDoConvenioSelecionado().length > 0" label="Plano">
                    <select
                        v-model.number="form.planoId"
                        class="form-input"
                    >
                        <option :value="null">Sem plano específico</option>
                        <option v-for="p in planosDoConvenioSelecionado()" :key="p.id" :value="p.id">{{ p.nome }}</option>
                    </select>
                </AppField>

                <AppField label="Número da carteirinha" required>
                    <input
                        v-model="form.numeroCarteirinha"
                        class="form-input"
                        placeholder="Número da carteirinha"
                        maxlength="50"
                    />
                </AppField>

                <AppField label="Validade">
                    <input
                        v-model="form.validade"
                        class="form-input"
                        type="date"
                    />
                </AppField>

                <div v-if="editandoId" class="form-row-toggle">
                    <label class="form-label">Status</label>
                    <div class="toggle-grupo">
                        <button
                            type="button"
                            :class="['toggle-btn', form.ativo ? 'toggle-btn--on' : 'toggle-btn--off']"
                            :aria-label="form.ativo ? 'Desativar carteirinha' : 'Ativar carteirinha'"
                            @click="form.ativo = !form.ativo"
                        >
                            <span class="toggle-thumb" />
                        </button>
                        <span class="toggle-label-status">{{ form.ativo ? "Ativa" : "Inativa" }}</span>
                    </div>
                </div>

                <p v-if="erroForm" class="msg-erro-inline">{{ erroForm }}</p>

                <div class="form-actions">
                    <AppButton variante="ghost" @click="drawerAberto = false">Cancelar</AppButton>
                    <AppButton variante="primario" :executando="salvando" @click="salvarCarteirinha">
                        {{ editandoId ? "Salvar" : "Adicionar" }}
                    </AppButton>
                </div>
            </div>
        </AppDrawer>

        <!-- Confirmação de exclusão -->
        <AppConfirmDialog
            v-model:aberto="confirmarExclusao"
            titulo="Remover carteirinha?"
            mensagem="A carteirinha será removida permanentemente do cadastro do paciente."
            confirmar-rotulo="Remover"
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
.convenios-tab {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.aba-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
}

.tab-loading {
    display: flex;
    justify-content: center;
    padding: 2rem;
}

.carteirinhas-lista {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.carteirinha-item {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    padding: 0.875rem 1rem;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    background: var(--color-surface);
}

.carteirinha-item--inativa {
    opacity: 0.6;
}

.carteirinha-dados {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.carteirinha-convenio {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: var(--color-text-primary);
}

.carteirinha-plano {
    font-weight: var(--font-weight-regular);
    color: var(--color-text-secondary);
}

.carteirinha-info-row {
    display: flex;
    gap: 0.75rem;
    align-items: center;
}

.carteirinha-numero,
.carteirinha-validade {
    font-size: var(--text-xs);
    color: var(--color-text-secondary);
}

.carteirinha-alerta {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    font-size: var(--text-xs);
    color: var(--color-danger);
    font-weight: var(--font-weight-medium);
}

.carteirinha-acoes {
    display: flex;
    align-items: center;
    gap: 0.375rem;
}

/* Em breve */
.em-breve-cards {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-top: 0.5rem;
}

.em-breve-card {
    display: flex;
    align-items: center;
    gap: 0.875rem;
    padding: 0.875rem 1rem;
    border: 1px dashed var(--color-border);
    border-radius: var(--radius-md);
    background: var(--color-bg);
    color: var(--color-text-muted);
}

.em-breve-icone {
    font-size: var(--text-lg);
    width: 1.5rem;
    text-align: center;
    flex-shrink: 0;
}

.em-breve-card > div { flex: 1; }

.em-breve-titulo {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: var(--color-text-secondary);
    display: block;
}

.em-breve-desc {
    font-size: var(--text-xs);
    color: var(--color-text-muted);
    margin: 0.125rem 0 0;
}

/* Formulário */
.carteirinha-form {
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
