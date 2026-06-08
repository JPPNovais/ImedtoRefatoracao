<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue"
import { vMaska } from "maska/vue"
import { AppButton, AppCard, AppField, AppInput, AppModal, AppSelect } from "@/components/ui"
import { unidadeService, type Unidade, type UnidadePayload } from "@/services/unidadeService"
import { buscarCep } from "@/utils/viaCep"

const props = defineProps<{
    estabelecimentoId: number
    podeEditar: boolean
}>()

const unidades = ref<Unidade[]>([])
const carregando = ref(false)
const erro = ref<string | null>(null)
const msgOk = ref<string | null>(null)

const UFS = [
    "AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS","MG","PA","PB",
    "PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO",
]

// ─── Form de criação ──────────────────────────────────────────────────────────
const formNovo = reactive<UnidadePayload>({
    nome: "",
    isPrincipal: false,
    cep: "",
    logradouro: "",
    numero: "",
    complemento: "",
    bairro: "",
    cidade: "",
    estado: "",
    telefone: "",
})
const salvandoNovo = ref(false)
const buscandoCepNovo = ref(false)

async function onCepBlurNovo() {
    if (!formNovo.cep) return
    buscandoCepNovo.value = true
    try {
        const r = await buscarCep(formNovo.cep)
        if (r) {
            formNovo.logradouro = r.logradouro || formNovo.logradouro || ""
            formNovo.bairro     = r.bairro     || formNovo.bairro     || ""
            formNovo.cidade     = r.localidade || formNovo.cidade     || ""
            formNovo.estado     = r.uf         || formNovo.estado     || ""
            if (!formNovo.complemento && r.complemento) formNovo.complemento = r.complemento
        }
    } finally {
        buscandoCepNovo.value = false
    }
}

async function adicionar() {
    erro.value = null
    msgOk.value = null
    if (!formNovo.nome.trim()) {
        erro.value = "Informe o nome da unidade."
        return
    }
    salvandoNovo.value = true
    try {
        await unidadeService.criar(props.estabelecimentoId, {
            ...formNovo,
            nome: formNovo.nome.trim(),
        })
        Object.assign(formNovo, {
            nome: "", isPrincipal: false, cep: "", logradouro: "", numero: "",
            complemento: "", bairro: "", cidade: "", estado: "", telefone: "",
        })
        msgOk.value = "Unidade adicionada."
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao adicionar unidade."
    } finally {
        salvandoNovo.value = false
    }
}

// ─── Edição inline ─────────────────────────────────────────────────────────────
const editandoId = ref<number | null>(null)
const formEdit = reactive<UnidadePayload>({
    nome: "", isPrincipal: false,
    cep: "", logradouro: "", numero: "", complemento: "",
    bairro: "", cidade: "", estado: "", telefone: "",
})
const salvandoEdit = ref(false)
const buscandoCepEdit = ref(false)

function iniciarEdicao(u: Unidade) {
    editandoId.value = u.id
    Object.assign(formEdit, {
        nome: u.nome,
        isPrincipal: u.isPrincipal,
        cep: u.cep ?? "",
        logradouro: u.logradouro ?? "",
        numero: u.numero ?? "",
        complemento: u.complemento ?? "",
        bairro: u.bairro ?? "",
        cidade: u.cidade ?? "",
        estado: u.estado ?? "",
        telefone: u.telefone ?? "",
    })
}

function cancelarEdicao() {
    editandoId.value = null
}

async function onCepBlurEdit() {
    if (!formEdit.cep) return
    buscandoCepEdit.value = true
    try {
        const r = await buscarCep(formEdit.cep)
        if (r) {
            formEdit.logradouro = r.logradouro || formEdit.logradouro || ""
            formEdit.bairro     = r.bairro     || formEdit.bairro     || ""
            formEdit.cidade     = r.localidade || formEdit.cidade     || ""
            formEdit.estado     = r.uf         || formEdit.estado     || ""
        }
    } finally {
        buscandoCepEdit.value = false
    }
}

async function salvarEdicao() {
    if (editandoId.value == null) return
    erro.value = null
    msgOk.value = null
    if (!formEdit.nome.trim()) {
        erro.value = "Informe o nome da unidade."
        return
    }
    salvandoEdit.value = true
    try {
        await unidadeService.atualizar(props.estabelecimentoId, editandoId.value, {
            ...formEdit,
            nome: formEdit.nome.trim(),
        })
        editandoId.value = null
        msgOk.value = "Unidade atualizada."
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao atualizar unidade."
    } finally {
        salvandoEdit.value = false
    }
}

// ─── Exclusão ─────────────────────────────────────────────────────────────────
const idParaExcluir = ref<number | null>(null)
const excluindo = ref(false)

const unidadeParaExcluir = computed(() => unidades.value.find(u => u.id === idParaExcluir.value) ?? null)

async function confirmarExclusao() {
    if (idParaExcluir.value == null) return
    excluindo.value = true
    erro.value = null
    msgOk.value = null
    try {
        await unidadeService.excluir(props.estabelecimentoId, idParaExcluir.value)
        idParaExcluir.value = null
        msgOk.value = "Unidade excluída."
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao excluir unidade."
    } finally {
        excluindo.value = false
    }
}

// ─── Loaders ─────────────────────────────────────────────────────────────────
async function carregar() {
    carregando.value = true
    try {
        unidades.value = await unidadeService.listar(props.estabelecimentoId)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar unidades."
    } finally {
        carregando.value = false
    }
}

function fmtCep(s: string | null) {
    if (!s) return ""
    const d = s.replace(/\D/g, "")
    return d.length === 8 ? `${d.slice(0,5)}-${d.slice(5)}` : s
}

function fmtTel(s: string | null) {
    if (!s) return ""
    const d = s.replace(/\D/g, "")
    if (d.length === 11) return `(${d.slice(0,2)}) ${d.slice(2,7)}-${d.slice(7)}`
    if (d.length === 10) return `(${d.slice(0,2)}) ${d.slice(2,6)}-${d.slice(6)}`
    return s
}

function enderecoCompleto(u: Unidade): string {
    const partes = [
        u.logradouro,
        u.numero,
        u.complemento,
        u.bairro,
        u.cidade && u.estado ? `${u.cidade} - ${u.estado}` : u.cidade ?? u.estado,
        u.cep ? `CEP ${fmtCep(u.cep)}` : null,
    ].filter(Boolean)
    return partes.length ? partes.join(", ") : "Endereço não informado"
}

onMounted(carregar)
</script>

<template>
    <div class="unidades">
        <p v-if="!podeEditar" class="aviso-leitura">
            Apenas o dono pode gerenciar unidades. Você está visualizando em modo leitura.
        </p>

        <p v-if="erro" class="msg-erro">{{ erro }}</p>
        <p v-if="msgOk" class="msg-ok">{{ msgOk }}</p>

        <!-- ── Formulário de criação ── -->
        <AppCard v-if="podeEditar" padding="md">
            <h3 class="ds-card-title">Adicionar nova unidade</h3>

            <div class="grade-2">
                <AppField label="Nome da unidade *">
                    <AppInput
                        v-model="formNovo.nome"
                        placeholder="ex: Matriz, Filial Centro"
                    />
                </AppField>
                <AppField label="Telefone">
                    <AppInput
                        v-model="formNovo.telefone"
                        v-maska="'(##) #####-####'"
                        type="tel"
                        placeholder="(00) 00000-0000"
                    />
                </AppField>
            </div>

            <label class="checkbox-row">
                <input v-model="formNovo.isPrincipal" type="checkbox" />
                <span>Marcar como unidade principal (matriz)</span>
            </label>

            <div class="grade-3">
                <AppField label="CEP">
                    <AppInput
                        v-model="formNovo.cep"
                        v-maska="'#####-###'"
                        placeholder="00000-000"
                        :disabled="buscandoCepNovo"
                        @blur="onCepBlurNovo"
                    />
                </AppField>
                <AppField label="Logradouro" class="col-span-2">
                    <AppInput v-model="formNovo.logradouro" placeholder="Rua, Avenida..." />
                </AppField>
            </div>

            <div class="grade-3">
                <AppField label="Número">
                    <AppInput v-model="formNovo.numero" />
                </AppField>
                <AppField label="Complemento" class="col-span-2">
                    <AppInput v-model="formNovo.complemento" placeholder="Sala, andar..." />
                </AppField>
            </div>

            <div class="grade-3">
                <AppField label="Bairro">
                    <AppInput v-model="formNovo.bairro" />
                </AppField>
                <AppField label="Cidade">
                    <AppInput v-model="formNovo.cidade" />
                </AppField>
                <AppField label="UF">
                    <AppSelect v-model="formNovo.estado">
                        <option value="">—</option>
                        <option v-for="uf in UFS" :key="uf" :value="uf">{{ uf }}</option>
                    </AppSelect>
                </AppField>
            </div>

            <div class="acoes-direita">
                <AppButton
                    icon="fa-solid fa-plus"
                    :disabled="salvandoNovo"
                    @click="adicionar"
                >{{ salvandoNovo ? "Salvando..." : "Adicionar unidade" }}</AppButton>
            </div>
        </AppCard>

        <!-- ── Lista de unidades ── -->
        <div v-if="carregando" class="estado-msg">Carregando unidades...</div>

        <div v-else-if="unidades.length === 0" class="estado-msg">
            Nenhuma unidade cadastrada ainda.
        </div>

        <div v-else class="lista">
            <AppCard v-for="u in unidades" :key="u.id" padding="md">
                <!-- Modo edição -->
                <template v-if="editandoId === u.id">
                    <h3 class="ds-card-title">Editar unidade</h3>

                    <div class="grade-2">
                        <AppField label="Nome *">
                            <AppInput v-model="formEdit.nome" />
                        </AppField>
                        <AppField label="Telefone">
                            <AppInput
                                v-model="formEdit.telefone"
                                v-maska="'(##) #####-####'"
                                type="tel"
                            />
                        </AppField>
                    </div>

                    <label class="checkbox-row">
                        <input v-model="formEdit.isPrincipal" type="checkbox" />
                        <span>Marcar como unidade principal (matriz)</span>
                    </label>

                    <div class="grade-3">
                        <AppField label="CEP">
                            <AppInput
                                v-model="formEdit.cep"
                                v-maska="'#####-###'"
                                :disabled="buscandoCepEdit"
                                @blur="onCepBlurEdit"
                            />
                        </AppField>
                        <AppField label="Logradouro" class="col-span-2">
                            <AppInput v-model="formEdit.logradouro" />
                        </AppField>
                    </div>

                    <div class="grade-3">
                        <AppField label="Número">
                            <AppInput v-model="formEdit.numero" />
                        </AppField>
                        <AppField label="Complemento" class="col-span-2">
                            <AppInput v-model="formEdit.complemento" />
                        </AppField>
                    </div>

                    <div class="grade-3">
                        <AppField label="Bairro">
                            <AppInput v-model="formEdit.bairro" />
                        </AppField>
                        <AppField label="Cidade">
                            <AppInput v-model="formEdit.cidade" />
                        </AppField>
                        <AppField label="UF">
                            <AppSelect v-model="formEdit.estado">
                                <option value="">—</option>
                                <option v-for="uf in UFS" :key="uf" :value="uf">{{ uf }}</option>
                            </AppSelect>
                        </AppField>
                    </div>

                    <div class="acoes-direita acoes-edicao">
                        <AppButton variant="secondary" @click="cancelarEdicao">Cancelar</AppButton>
                        <AppButton :disabled="salvandoEdit" @click="salvarEdicao">
                            {{ salvandoEdit ? "Salvando..." : "Salvar alterações" }}
                        </AppButton>
                    </div>
                </template>

                <!-- Modo visualização -->
                <template v-else>
                    <div class="card-cabecalho">
                        <h3 class="unidade-nome">
                            {{ u.nome }}
                            <span v-if="u.isPrincipal" class="badge-principal">Principal</span>
                        </h3>
                        <div v-if="podeEditar" class="card-acoes">
                            <button
                                type="button"
                                class="btn-icon"
                                title="Editar"
                                @click="iniciarEdicao(u)"
                            >
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button
                                v-if="!u.isPrincipal"
                                type="button"
                                class="btn-icon btn-danger"
                                title="Excluir"
                                @click="idParaExcluir = u.id"
                            >
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    </div>
                    <p class="unidade-endereco">{{ enderecoCompleto(u) }}</p>
                    <p v-if="u.telefone" class="unidade-tel">📞 {{ fmtTel(u.telefone) }}</p>
                </template>
            </AppCard>
        </div>

        <!-- ── Modal de confirmação ── -->
        <AppModal
            :aberto="idParaExcluir != null"
            titulo="Excluir unidade"
            largura="sm"
            @fechar="idParaExcluir = null"
        >
            <p class="modal-texto">
                Excluir a unidade <strong>{{ unidadeParaExcluir?.nome }}</strong>?
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
.unidades { display: flex; flex-direction: column; gap: 1rem; }

.aviso-leitura {
    background: #fef3c7; color: #92400e;
    padding: 0.65rem 0.9rem; border-radius: var(--radius);
    font-size: 0.82em; margin: 0;
}


.grade-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 0.75rem; }
.grade-3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 0.75rem; margin-bottom: 0.75rem; }

@media (max-width: 720px) {
    .grade-2, .grade-3 { grid-template-columns: 1fr; }
}

.col-span-2 { grid-column: span 2; }
@media (max-width: 720px) {
    .col-span-2 { grid-column: span 1; }
}

.checkbox-row {
    display: flex; align-items: center; gap: 0.5rem;
    font-size: 0.85em;
    margin: 0.25rem 0 0.85rem;
    cursor: pointer;
}
.checkbox-row input { cursor: pointer; }

.acoes-direita {
    display: flex; justify-content: flex-end; gap: 0.5rem;
    margin-top: 0.5rem;
}
.acoes-edicao { gap: 0.6rem; }

.lista { display: flex; flex-direction: column; gap: 0.85rem; }

.card-cabecalho {
    display: flex; align-items: center; justify-content: space-between;
    gap: 0.75rem; flex-wrap: wrap;
    margin-bottom: 0.4rem;
}
.unidade-nome {
    font-size: 1rem; font-weight: 700; margin: 0;
    display: flex; align-items: center; gap: 0.6rem; flex-wrap: wrap;
}

.badge-principal {
    display: inline-flex; align-items: center;
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary-dark));
    padding: 0.15rem 0.55rem;
    border-radius: 999px;
    font-size: 0.7em; font-weight: 700;
    letter-spacing: 0.02em;
}

.unidade-endereco { font-size: 0.85em; color: var(--text-muted); margin: 0 0 0.2rem; }
.unidade-tel      { font-size: 0.82em; color: var(--text-muted); margin: 0; }

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
