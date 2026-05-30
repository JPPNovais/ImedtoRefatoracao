<script setup lang="ts">
/**
 * RegioesGlobaisListView — árvore de regiões anatômicas (Wave 4 live-link).
 * Renderiza a estrutura hierárquica via RegiaoTreeView recursivo.
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

// Modal de exclusão
const modalExcluir = ref(false)
const excluirItem = ref<RegiaoAnatomicaNoDto | null>(null)
const motivoExclusao = ref("")
const erroExclusao = ref("")
const excluindo = ref(false)

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

function abrirExcluir(no: RegiaoAnatomicaNoDto) {
    excluirItem.value = no
    motivoExclusao.value = ""
    erroExclusao.value = ""
    modalExcluir.value = true
}

function fecharModal() {
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
        fecharModal()
        await carregar()
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroExclusao.value = msg ?? "Não foi possível excluir a região."
    } finally {
        excluindo.value = false
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
                @excluir="abrirExcluir($event)"
            />
        </AppCard>

        <!-- Modal de exclusão -->
        <AppModal
            :aberto="modalExcluir"
            titulo="Excluir região anatômica"
            @fechar="fecharModal"
        >
            <p class="modal-desc">
                Excluir permanentemente <strong>{{ excluirItem?.codigo }}</strong> — {{ excluirItem?.nome }}?
                <br><span class="modal-aviso">Esta ação não pode ser desfeita.</span>
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
                <AppButton variant="secondary" :disabled="excluindo" @click="fecharModal">Cancelar</AppButton>
                <AppButton
                    variant="danger"
                    :loading="excluindo"
                    :disabled="motivoExclusao.trim().length < 10"
                    @click="confirmarExclusao"
                >
                    Excluir
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
    font-size: 0.875rem;
    color: hsl(var(--foreground));
    cursor: pointer;
}

.estado-info {
    text-align: center;
    padding: 2rem 0;
    color: hsl(var(--muted-foreground));
    font-size: 0.875rem;
}

.estado-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
}

.modal-desc {
    font-size: 0.875rem;
    line-height: 1.5;
    margin-bottom: 0.5rem;
}

.modal-aviso {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
}

.campo-erro {
    color: hsl(var(--destructive));
    font-size: 0.8125rem;
    margin-top: 0.25rem;
}
</style>
