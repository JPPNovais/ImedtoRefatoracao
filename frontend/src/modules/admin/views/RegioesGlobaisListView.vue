<script setup lang="ts">
/**
 * RegioesGlobaisListView — árvore de regiões anatômicas (Wave 4 live-link).
 * B3 (2026-06-08_007): inativar é ação primária; reativar exposto para inativos;
 * hard-delete permanece como ação secundária.
 */
import { ref, onMounted } from "vue"
import { useRouter } from "vue-router"
import {
    AppPageHeader, AppCard, AppEmptyState,
    AppButton, AppModal, AppField, AppTextarea,
} from "@/components/ui"
import { useRegioesGlobaisStore } from "../stores/regioesGlobaisStore"
import RegiaoTreeView from "../components/regioes/RegiaoTreeView.vue"
import type { RegiaoAnatomicaNoDto } from "../services/catalogosService"

const router = useRouter()
const store = useRegioesGlobaisStore()

const filtroInativos = ref(false)

// ─── Modal de inativar ───────────────────────────────────────────────────────

const modalInativar = ref(false)
const inativarItem = ref<RegiaoAnatomicaNoDto | null>(null)
const motivoInativacao = ref("")
const erroInativacao = ref("")
const inativando = ref(false)

function abrirInativar(no: RegiaoAnatomicaNoDto) {
    inativarItem.value = no
    motivoInativacao.value = ""
    erroInativacao.value = ""
    modalInativar.value = true
}

function fecharModalInativar() {
    modalInativar.value = false
    inativarItem.value = null
}

async function confirmarInativacao() {
    if (!inativarItem.value) return
    if (motivoInativacao.value.trim().length < 10) {
        erroInativacao.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }
    inativando.value = true
    erroInativacao.value = ""
    try {
        await store.inativar(inativarItem.value.id, motivoInativacao.value.trim())
        fecharModalInativar()
        await carregar()
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroInativacao.value = msg ?? "Não foi possível inativar a região."
    } finally {
        inativando.value = false
    }
}

// ─── Modal de reativar ───────────────────────────────────────────────────────

const modalReativar = ref(false)
const reativarItem = ref<RegiaoAnatomicaNoDto | null>(null)
const motivoReativacao = ref("")
const erroReativacao = ref("")
const reativando = ref(false)

function abrirReativar(no: RegiaoAnatomicaNoDto) {
    reativarItem.value = no
    motivoReativacao.value = ""
    erroReativacao.value = ""
    modalReativar.value = true
}

function fecharModalReativar() {
    modalReativar.value = false
    reativarItem.value = null
}

async function confirmarReativacao() {
    if (!reativarItem.value) return
    if (motivoReativacao.value.trim().length < 10) {
        erroReativacao.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }
    reativando.value = true
    erroReativacao.value = ""
    try {
        await store.reativar(reativarItem.value.id, motivoReativacao.value.trim())
        fecharModalReativar()
        await carregar()
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroReativacao.value = msg ?? "Não foi possível reativar a região."
    } finally {
        reativando.value = false
    }
}

// ─── Modal de exclusão ───────────────────────────────────────────────────────

const modalExcluir = ref(false)
const excluirItem = ref<RegiaoAnatomicaNoDto | null>(null)
const motivoExclusao = ref("")
const erroExclusao = ref("")
const excluindo = ref(false)

function abrirExcluir(no: RegiaoAnatomicaNoDto) {
    excluirItem.value = no
    motivoExclusao.value = ""
    erroExclusao.value = ""
    modalExcluir.value = true
}

function fecharModalExcluir() {
    modalExcluir.value = false
    excluirItem.value = null
}

async function confirmarExclusao() {
    if (!excluirItem.value) return
    if (motivoExclusao.value.trim().length < 10) {
        erroExclusao.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }
    excluindo.value = true
    erroExclusao.value = ""
    try {
        await store.excluir(excluirItem.value.id, motivoExclusao.value.trim())
        fecharModalExcluir()
        await carregar()
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroExclusao.value = msg ?? "Não foi possível excluir a região."
    } finally {
        excluindo.value = false
    }
}

// ─── Listagem ────────────────────────────────────────────────────────────────

onMounted(() => carregar())

async function carregar() {
    await store.carregarArvore(filtroInativos.value)
}

function irParaForm(id?: number) {
    if (id !== undefined) {
        router.push({ name: "AdminRegioesGlobaisEditar", params: { id } })
    } else {
        router.push({ name: "AdminRegioesGlobaisNovo" })
    }
}
</script>

<template>
    <main class="app-page">
        <AppPageHeader
            titulo="Regiões anatômicas"
            subtitulo="Catálogo hierárquico de regiões anatômicas disponível para o exame físico."
        >
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="irParaForm()">Nova região</AppButton>
            </template>
        </AppPageHeader>

        <AppCard>
            <!-- Filtros -->
            <div class="filtros-row">
                <label class="label-check">
                    <input type="checkbox" v-model="filtroInativos" @change="carregar" />
                    Incluir inativas
                </label>
                <AppButton variant="secondary" @click="carregar">Atualizar</AppButton>
            </div>

            <div v-if="store.carregando" class="estado-info">
                <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
            </div>

            <p v-else-if="store.erro" class="estado-erro" role="alert">{{ store.erro }}</p>

            <AppEmptyState
                v-else-if="store.arvore.length === 0"
                titulo="Nenhuma região cadastrada."
                descricao="Crie a primeira região usando o botão acima."
            />

            <RegiaoTreeView
                v-else
                :nos="store.arvore"
                @editar="irParaForm($event)"
                @inativar="abrirInativar($event)"
                @reativar="abrirReativar($event)"
                @excluir="abrirExcluir($event)"
            />
        </AppCard>

        <!-- Modal de inativar (reversível) -->
        <AppModal
            :aberto="modalInativar"
            titulo="Inativar região anatômica"
            @fechar="fecharModalInativar"
        >
            <p class="modal-desc">
                Inativar <strong>{{ inativarItem?.codigo }}</strong> — {{ inativarItem?.nome }}?
                <br><span class="modal-aviso-info">Esta ação é reversível: a região pode ser reativada depois.</span>
            </p>

            <AppField label="Motivo" required hint="Mínimo 10 caracteres.">
                <AppTextarea
                    v-model="motivoInativacao"
                    :rows="3"
                    placeholder="Descreva o motivo..."
                    :disabled="inativando"
                />
            </AppField>

            <p v-if="erroInativacao" class="campo-erro">{{ erroInativacao }}</p>

            <template #rodape>
                <AppButton variant="secondary" :disabled="inativando" @click="fecharModalInativar">Cancelar</AppButton>
                <AppButton
                    variant="danger"
                    :loading="inativando"
                    :disabled="motivoInativacao.trim().length < 10"
                    @click="confirmarInativacao"
                >
                    Inativar
                </AppButton>
            </template>
        </AppModal>

        <!-- Modal de reativar -->
        <AppModal
            :aberto="modalReativar"
            titulo="Reativar região anatômica"
            @fechar="fecharModalReativar"
        >
            <p class="modal-desc">
                Reativar <strong>{{ reativarItem?.codigo }}</strong> — {{ reativarItem?.nome }}?
                <br><span class="modal-aviso-info">A região voltará a aparecer nas seleções de exame físico.</span>
            </p>

            <AppField label="Motivo" required hint="Mínimo 10 caracteres.">
                <AppTextarea
                    v-model="motivoReativacao"
                    :rows="3"
                    placeholder="Descreva o motivo..."
                    :disabled="reativando"
                />
            </AppField>

            <p v-if="erroReativacao" class="campo-erro">{{ erroReativacao }}</p>

            <template #rodape>
                <AppButton variant="secondary" :disabled="reativando" @click="fecharModalReativar">Cancelar</AppButton>
                <AppButton
                    :loading="reativando"
                    :disabled="motivoReativacao.trim().length < 10"
                    @click="confirmarReativacao"
                >
                    Reativar
                </AppButton>
            </template>
        </AppModal>

        <!-- Modal de exclusão permanente (ação secundária) -->
        <AppModal
            :aberto="modalExcluir"
            titulo="Excluir permanentemente"
            @fechar="fecharModalExcluir"
        >
            <p class="modal-desc">
                Excluir permanentemente <strong>{{ excluirItem?.codigo }}</strong> — {{ excluirItem?.nome }}?
                <br><span class="modal-aviso">Esta ação não pode ser desfeita. Prefira inativar para preservar o histórico.</span>
            </p>

            <AppField label="Motivo" required hint="Mínimo 10 caracteres.">
                <AppTextarea
                    v-model="motivoExclusao"
                    :rows="3"
                    placeholder="Descreva o motivo..."
                    :disabled="excluindo"
                />
            </AppField>

            <p v-if="erroExclusao" class="campo-erro">{{ erroExclusao }}</p>

            <template #rodape>
                <AppButton variant="secondary" :disabled="excluindo" @click="fecharModalExcluir">Cancelar</AppButton>
                <AppButton
                    variant="danger"
                    :loading="excluindo"
                    :disabled="motivoExclusao.trim().length < 10"
                    @click="confirmarExclusao"
                >
                    Excluir permanentemente
                </AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.filtros-row {
    display: flex;
    gap: 0.75rem;
    margin-bottom: 1.25rem;
    flex-wrap: wrap;
    align-items: center;
}

.label-check {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    font-size: var(--text-sm);
    color: hsl(var(--foreground));
    cursor: pointer;
}

.estado-info {
    text-align: center;
    padding: 2rem 0;
    color: hsl(var(--muted-foreground));
    font-size: var(--text-sm);
}

.estado-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
}

.modal-desc {
    font-size: var(--text-sm);
    line-height: 1.5;
    margin-bottom: 0.5rem;
}

.modal-aviso {
    color: hsl(var(--destructive));
    font-size: var(--text-xs);
}

.modal-aviso-info {
    color: hsl(var(--muted-foreground));
    font-size: var(--text-xs);
}

.campo-erro {
    color: hsl(var(--destructive));
    font-size: var(--text-xs);
    margin-top: 0.25rem;
}
</style>
