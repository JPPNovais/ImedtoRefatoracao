<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue"
import { AppButton, AppCard, AppField, AppInput, AppModal, AppSelect, AppToast } from "@/components/ui"
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

const toast = ref<{ msg: string; variante: "info" | "success" | "error" } | null>(null)
function notificar(msg: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { msg, variante }
}

const filtroUnidadeId = ref<string>("")
const incluirInativas = ref(false)

const salasFiltradas = computed(() => {
    let arr = salas.value
    if (!incluirInativas.value) arr = arr.filter(s => s.ativo)
    if (filtroUnidadeId.value !== "") {
        const uid = Number(filtroUnidadeId.value)
        arr = arr.filter(s => s.unidadeId === uid)
    }
    return arr
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
    if (!formNovo.nome.trim()) {
        notificar("Informe o nome da repartição.", "error")
        return
    }
    if (!formNovo.unidadeId) {
        notificar("Selecione a unidade onde a repartição está localizada.", "error")
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
        notificar("Repartição adicionada.", "success")
        await carregarSalas()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao adicionar repartição.", "error")
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
    if (!formEdit.nome.trim()) {
        notificar("Informe o nome da repartição.", "error")
        return
    }
    if (!formEdit.unidadeId) {
        notificar("Selecione a unidade onde a repartição está localizada.", "error")
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
        notificar("Repartição atualizada.", "success")
        await carregarSalas()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao atualizar repartição.", "error")
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
    try {
        await salaService.excluir(props.estabelecimentoId, idParaExcluir.value)
        idParaExcluir.value = null
        notificar("Repartição excluída.", "success")
        await carregarSalas()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao excluir repartição.", "error")
    } finally {
        excluindo.value = false
    }
}

// ─── Desativar / Reativar ─────────────────────────────────────────────────────
const alternandoId = ref<number | null>(null)

async function alternarAtivo(s: Sala) {
    alternandoId.value = s.id
    try {
        if (s.ativo) await salaService.desativar(props.estabelecimentoId, s.id)
        else         await salaService.reativar(props.estabelecimentoId, s.id)
        notificar(s.ativo ? "Repartição desativada." : "Repartição reativada.", "success")
        await carregarSalas()
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao alterar status da repartição.", "error")
    } finally {
        alternandoId.value = null
    }
}

// ─── Loaders ─────────────────────────────────────────────────────────────────
async function carregarTudo() {
    carregando.value = true
    try {
        const [u, t, s] = await Promise.all([
            unidadeService.listar(props.estabelecimentoId),
            salaService.listarTipos(),
            salaService.listar(props.estabelecimentoId),
        ])
        unidades.value = u
        tipos.value = t
        salas.value = s
        const principal = u.find(x => x.isPrincipal) ?? u[0]
        if (principal) formNovo.unidadeId = principal.id
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao carregar dados.", "error")
    } finally {
        carregando.value = false
    }
}

async function carregarSalas() {
    try {
        salas.value = await salaService.listar(props.estabelecimentoId)
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao recarregar.", "error")
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

        <!-- ── Filtros (unidade + incluir inativas) ── -->
        <div v-if="salas.length > 0" class="filtros-row">
            <AppField v-if="unidades.length > 1" label="Filtrar por unidade" class="filt-unidade">
                <AppSelect v-model="filtroUnidadeId">
                    <option value="">Todas as unidades</option>
                    <option v-for="u in unidades" :key="u.id" :value="u.id">{{ u.nome }}</option>
                </AppSelect>
            </AppField>
            <label class="check-inativas">
                <input type="checkbox" v-model="incluirInativas" />
                Incluir inativas
            </label>
        </div>

        <!-- ── Lista de salas ── -->
        <div v-if="carregando" class="estado-msg">Carregando...</div>

        <div v-else-if="salas.length === 0 && !semUnidades" class="estado-msg">
            Nenhuma repartição cadastrada ainda.
        </div>

        <div v-else-if="salasFiltradas.length === 0" class="estado-msg">
            Nenhuma repartição visível com os filtros atuais.
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
                            <span v-if="!s.ativo" class="tag-inativa">Inativa</span>
                        </h3>
                        <div v-if="podeEditar" class="card-acoes">
                            <button
                                v-if="s.ativo"
                                type="button"
                                class="btn-icon btn-icon-editar"
                                title="Editar"
                                @click="iniciarEdicao(s)"
                            >
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button
                                type="button"
                                class="btn-icon"
                                :title="s.ativo ? 'Desativar' : 'Reativar'"
                                :disabled="alternandoId === s.id"
                                @click="alternarAtivo(s)"
                            >
                                <i :class="['fa-solid', s.ativo ? 'fa-toggle-on' : 'fa-toggle-off']"></i>
                            </button>
                            <button
                                v-if="s.ativo"
                                type="button"
                                class="btn-icon btn-icon-excluir"
                                title="Excluir"
                                @click="idParaExcluir = s.id"
                            >
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    </div>
                    <p class="sala-meta">
                        <i class="fa-solid fa-building" aria-hidden="true"></i>
                        {{ s.unidadeNome }}
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

        <AppToast
            v-if="toast"
            :mensagem="toast.msg"
            :variante="toast.variante"
            @fechar="toast = null"
        />
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

.filtros-row {
    display: flex; gap: 1rem; align-items: flex-end; flex-wrap: wrap;
}
.filt-unidade { max-width: 320px; flex: 1; }
.check-inativas {
    display: inline-flex; align-items: center; gap: 0.4rem;
    font-size: 0.85em; color: var(--text-muted); cursor: pointer;
    padding-bottom: 0.5rem;
}

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
.tag-inativa {
    display: inline-flex; align-items: center;
    background: hsl(var(--secondary) / 0.12);
    color: hsl(var(--secondary) / 0.75);
    padding: 0.15rem 0.55rem;
    border-radius: 999px;
    font-size: 0.7em; font-weight: 700;
}
.sala-meta {
    font-size: 0.85em; color: var(--text-muted); margin: 0;
    display: inline-flex; align-items: center; gap: 0.4rem;
}
.sala-meta > i { font-size: 0.9em; opacity: 0.7; }
.sala-desc { font-style: italic; }

.card-acoes { display: flex; gap: 0.4rem; }

.estado-msg { text-align: center; color: var(--text-muted); padding: 2rem 1rem; font-size: 0.9em; }

.modal-texto { margin: 0; font-size: 0.9em; }
</style>
