<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue"
import { AppButton, AppCard, AppField, AppInput, AppModal, AppSelect } from "@/components/ui"
import { salaService, type Sala, type SalaPayload, type TipoSala } from "@/services/salaService"
import { unidadeService, type Unidade } from "@/services/unidadeService"

const props = defineProps<{
    estabelecimentoId: number
    podeEditar: boolean
}>()

const salas = ref<Sala[]>([])
const unidades = ref<Unidade[]>([])
const tipos = ref<TipoSala[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)
const msgOk = ref<string | null>(null)

// Filtro por unidade (só mostrado se houver mais de uma).
const filtroUnidadeId = ref<number | null>(null)

const salasFiltradas = computed(() => {
    if (filtroUnidadeId.value == null) return salas.value
    return salas.value.filter(s => s.unidadeId === filtroUnidadeId.value)
})

// ─── Form de criação ──────────────────────────────────────────────────────────
const formNovo = reactive<SalaPayload>({
    unidadeId: 0,
    tipoSalaId: null,
    nome: "",
    descricao: "",
})
const salvandoNovo = ref(false)

async function adicionar() {
    erro.value = null
    msgOk.value = null
    if (!formNovo.nome.trim()) {
        erro.value = "Informe o nome da repartição."
        return
    }
    if (!formNovo.unidadeId) {
        erro.value = "Selecione a unidade onde a repartição está localizada."
        return
    }
    salvandoNovo.value = true
    try {
        await salaService.criar(props.estabelecimentoId, {
            ...formNovo,
            nome: formNovo.nome.trim(),
            descricao: formNovo.descricao?.trim() || null,
        })
        formNovo.unidadeId = unidades.value[0]?.id ?? 0
        formNovo.tipoSalaId = null
        formNovo.nome = ""
        formNovo.descricao = ""
        msgOk.value = "Repartição adicionada."
        await carregarSalas()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao adicionar repartição."
    } finally {
        salvandoNovo.value = false
    }
}

// ─── Edição inline ─────────────────────────────────────────────────────────────
const editandoId = ref<number | null>(null)
const formEdit = reactive<SalaPayload>({
    unidadeId: 0,
    tipoSalaId: null,
    nome: "",
    descricao: "",
})
const salvandoEdit = ref(false)

function iniciarEdicao(s: Sala) {
    editandoId.value = s.id
    Object.assign(formEdit, {
        unidadeId: s.unidadeId,
        tipoSalaId: s.tipoSalaId,
        nome: s.nome,
        descricao: s.descricao ?? "",
    })
}

function cancelarEdicao() {
    editandoId.value = null
}

async function salvarEdicao() {
    if (editandoId.value == null) return
    erro.value = null
    msgOk.value = null
    if (!formEdit.nome.trim()) {
        erro.value = "Informe o nome da repartição."
        return
    }
    if (!formEdit.unidadeId) {
        erro.value = "Selecione a unidade onde a repartição está localizada."
        return
    }
    salvandoEdit.value = true
    try {
        await salaService.atualizar(props.estabelecimentoId, editandoId.value, {
            ...formEdit,
            nome: formEdit.nome.trim(),
            descricao: formEdit.descricao?.trim() || null,
        })
        editandoId.value = null
        msgOk.value = "Repartição atualizada."
        await carregarSalas()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao atualizar repartição."
    } finally {
        salvandoEdit.value = false
    }
}

// ─── Exclusão ─────────────────────────────────────────────────────────────────
const idParaExcluir = ref<number | null>(null)
const excluindo = ref(false)
const salaParaExcluir = computed(() => salas.value.find(s => s.id === idParaExcluir.value) ?? null)

async function confirmarExclusao() {
    if (idParaExcluir.value == null) return
    excluindo.value = true
    erro.value = null
    msgOk.value = null
    try {
        await salaService.excluir(props.estabelecimentoId, idParaExcluir.value)
        idParaExcluir.value = null
        msgOk.value = "Repartição excluída."
        await carregarSalas()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao excluir repartição."
    } finally {
        excluindo.value = false
    }
}

// ─── Loaders ─────────────────────────────────────────────────────────────────
async function carregarTudo() {
    carregando.value = true
    erro.value = null
    try {
        const [u, t, s] = await Promise.all([
            unidadeService.listar(props.estabelecimentoId),
            salaService.listarTipos(),
            salaService.listar(props.estabelecimentoId),
        ])
        unidades.value = u
        tipos.value = t
        salas.value = s
        // Pré-seleciona a unidade principal (ou a primeira) no form de criação.
        const principal = u.find(x => x.isPrincipal) ?? u[0]
        if (principal) formNovo.unidadeId = principal.id
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar dados."
    } finally {
        carregando.value = false
    }
}

async function carregarSalas() {
    try {
        salas.value = await salaService.listar(props.estabelecimentoId)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao recarregar."
    }
}

const semUnidades = computed(() => unidades.value.length === 0)

onMounted(carregarTudo)
</script>

<template>
    <div class="reparticoes">
        <p v-if="!podeEditar" class="aviso-leitura">
            Apenas o dono pode gerenciar repartições. Você está visualizando em modo leitura.
        </p>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>
        <p v-if="msgOk" class="msg-ok">{{ msgOk }}</p>

        <p v-if="semUnidades && podeEditar" class="alerta-info">
            Cadastre pelo menos uma unidade na aba <strong>Unidades</strong> antes de criar repartições.
        </p>

        <!-- ── Formulário de criação ── -->
        <AppCard v-if="podeEditar && !semUnidades" padding="md">
            <h3 class="secao-titulo">Adicionar nova repartição</h3>

            <div class="grade-3">
                <AppField label="Nome *">
                    <AppInput
                        v-model="formNovo.nome"
                        placeholder="ex: Consultório 01, Sala de Exames"
                    />
                </AppField>
                <AppField label="Unidade *">
                    <AppSelect v-model.number="formNovo.unidadeId">
                        <option :value="0" disabled>Selecione...</option>
                        <option v-for="u in unidades" :key="u.id" :value="u.id">
                            {{ u.nome }}{{ u.isPrincipal ? " (principal)" : "" }}
                        </option>
                    </AppSelect>
                </AppField>
                <AppField label="Tipo">
                    <AppSelect v-model="formNovo.tipoSalaId">
                        <option :value="null">— Sem tipo —</option>
                        <option v-for="t in tipos" :key="t.id" :value="t.id">{{ t.nome }}</option>
                    </AppSelect>
                </AppField>
            </div>

            <AppField label="Descrição (opcional)">
                <AppInput
                    v-model="formNovo.descricao"
                    placeholder="ex: Sala equipada com ultrassom"
                />
            </AppField>

            <div class="acoes-direita">
                <AppButton
                    icon="fa-solid fa-plus"
                    :disabled="salvandoNovo"
                    @click="adicionar"
                >{{ salvandoNovo ? "Salvando..." : "Adicionar repartição" }}</AppButton>
            </div>
        </AppCard>

        <!-- ── Filtro por unidade ── -->
        <div v-if="unidades.length > 1 && salas.length > 0" class="filtro-row">
            <AppField label="Filtrar por unidade">
                <AppSelect v-model.number="filtroUnidadeId">
                    <option :value="null">Todas as unidades</option>
                    <option v-for="u in unidades" :key="u.id" :value="u.id">{{ u.nome }}</option>
                </AppSelect>
            </AppField>
        </div>

        <!-- ── Lista de salas ── -->
        <div v-if="carregando" class="estado-msg">Carregando...</div>

        <div v-else-if="salas.length === 0 && !semUnidades" class="estado-msg">
            Nenhuma repartição cadastrada ainda.
        </div>

        <div v-else-if="salasFiltradas.length === 0" class="estado-msg">
            Nenhuma repartição na unidade selecionada.
        </div>

        <div v-else class="lista">
            <AppCard v-for="s in salasFiltradas" :key="s.id" padding="md">
                <!-- Modo edição -->
                <template v-if="editandoId === s.id">
                    <h3 class="secao-titulo">Editar repartição</h3>
                    <div class="grade-3">
                        <AppField label="Nome *">
                            <AppInput v-model="formEdit.nome" />
                        </AppField>
                        <AppField label="Unidade *">
                            <AppSelect v-model.number="formEdit.unidadeId">
                                <option v-for="u in unidades" :key="u.id" :value="u.id">{{ u.nome }}</option>
                            </AppSelect>
                        </AppField>
                        <AppField label="Tipo">
                            <AppSelect v-model="formEdit.tipoSalaId">
                                <option :value="null">— Sem tipo —</option>
                                <option v-for="t in tipos" :key="t.id" :value="t.id">{{ t.nome }}</option>
                            </AppSelect>
                        </AppField>
                    </div>
                    <AppField label="Descrição">
                        <AppInput v-model="formEdit.descricao" />
                    </AppField>
                    <div class="acoes-direita acoes-edicao">
                        <AppButton variant="secondary" @click="cancelarEdicao">Cancelar</AppButton>
                        <AppButton :disabled="salvandoEdit" @click="salvarEdicao">
                            {{ salvandoEdit ? "Salvando..." : "Salvar alterações" }}
                        </AppButton>
                    </div>
                </template>

                <!-- Modo visualização -->
                <template v-else>
                    <div class="card-titulo">
                        <h3 class="sala-nome">
                            {{ s.nome }}
                            <span v-if="s.tipoSalaNome" class="tag-tipo">{{ s.tipoSalaNome }}</span>
                        </h3>
                        <div v-if="podeEditar" class="card-acoes">
                            <button type="button" class="btn-icon" title="Editar" @click="iniciarEdicao(s)">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button
                                type="button"
                                class="btn-icon btn-danger"
                                title="Excluir"
                                @click="idParaExcluir = s.id"
                            >
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    </div>
                    <p class="sala-meta">
                        🏢 {{ s.unidadeNome }}
                        <span v-if="s.descricao" class="sala-desc">— {{ s.descricao }}</span>
                    </p>
                </template>
            </AppCard>
        </div>

        <!-- ── Modal de confirmação ── -->
        <AppModal
            :aberto="idParaExcluir != null"
            titulo="Excluir repartição"
            largura="sm"
            @fechar="idParaExcluir = null"
        >
            <p class="modal-texto">
                Excluir a repartição <strong>{{ salaParaExcluir?.nome }}</strong>?
                Esta ação não pode ser desfeita.
            </p>
            <template #rodape>
                <AppButton variant="secondary" @click="idParaExcluir = null">Cancelar</AppButton>
                <AppButton variant="danger" :disabled="excluindo" @click="confirmarExclusao">
                    {{ excluindo ? "Excluindo..." : "Excluir" }}
                </AppButton>
            </template>
        </AppModal>
    </div>
</template>

<style scoped>
.reparticoes { display: flex; flex-direction: column; gap: 1rem; }

.aviso-leitura {
    background: #fef3c7; color: #92400e;
    padding: 0.65rem 0.9rem; border-radius: var(--radius);
    font-size: 0.82em; margin: 0;
}
.alerta-info {
    background: hsl(var(--primary) / 0.08); color: hsl(var(--primary-dark));
    padding: 0.7rem 0.95rem; border-radius: var(--radius);
    font-size: 0.85em; margin: 0;
}

.secao-titulo { font-size: 0.95em; font-weight: 700; margin: 0 0 0.85rem; }

.grade-3 {
    display: grid; grid-template-columns: 1fr 1fr 1fr;
    gap: 0.75rem; margin-bottom: 0.75rem;
}
@media (max-width: 720px) {
    .grade-3 { grid-template-columns: 1fr; }
}

.acoes-direita {
    display: flex; justify-content: flex-end; gap: 0.5rem;
    margin-top: 0.5rem;
}
.acoes-edicao { gap: 0.6rem; }

.filtro-row { max-width: 320px; }

.lista { display: flex; flex-direction: column; gap: 0.85rem; }

.card-titulo {
    display: flex; align-items: center; justify-content: space-between;
    gap: 0.75rem; flex-wrap: wrap;
    margin-bottom: 0.4rem;
}
.sala-nome {
    font-size: 1rem; font-weight: 700; margin: 0;
    display: flex; align-items: center; gap: 0.6rem; flex-wrap: wrap;
}
.tag-tipo {
    display: inline-flex; align-items: center;
    background: hsl(var(--secondary) / 0.08);
    color: hsl(var(--secondary));
    padding: 0.15rem 0.55rem;
    border-radius: 999px;
    font-size: 0.7em; font-weight: 600;
}
.sala-meta { font-size: 0.85em; color: var(--text-muted); margin: 0; }
.sala-desc { font-style: italic; }

.card-acoes { display: flex; gap: 0.4rem; }
.btn-icon {
    width: 32px; height: 32px;
    display: inline-flex; align-items: center; justify-content: center;
    border-radius: var(--radius);
    border: 1px solid var(--border-strong);
    background: var(--bg-card);
    color: var(--text-muted);
    cursor: pointer;
    transition: all 0.15s;
    font-size: 0.85em;
}
.btn-icon:hover { color: hsl(var(--primary)); border-color: hsl(var(--primary)); }
.btn-icon.btn-danger:hover {
    color: var(--danger); border-color: var(--danger);
    background: hsl(0 90% 60% / 0.06);
}

.estado-msg { text-align: center; color: var(--text-muted); padding: 2rem 1rem; font-size: 0.9em; }

.msg-erro { color: var(--danger); font-size: 0.85em; margin: 0; }
.msg-ok   { color: #15803d;       font-size: 0.85em; margin: 0; }

.modal-texto { margin: 0; font-size: 0.9em; }
</style>
