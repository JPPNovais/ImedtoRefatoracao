<script setup lang="ts">
import { onMounted, reactive, ref } from "vue"
import {
    AppButton, AppCard, AppConfirmDialog, AppEmptyState, AppField,
    AppInput, AppTextarea, AppToast,
} from "@/components/ui"
import {
    modeloDescricaoCirurgicaService,
    type ModeloDescricaoCirurgica,
} from "@/services/modeloDescricaoCirurgicaService"

defineProps<{
    podeEditar: boolean
}>()

const modelos = ref<ModeloDescricaoCirurgica[]>([])
const carregando = ref(false)

const toast = ref<{ mensagem: string; variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}

// ─── Form criar/editar ─────────────────────────────────────────────────────────
const editandoId = ref<number | null>(null)
const form = reactive({ titulo: "", corpo: "" })
const salvando = ref(false)
const erroForm = ref<string | null>(null)

function resetForm() {
    editandoId.value = null
    form.titulo = ""
    form.corpo = ""
    erroForm.value = null
}

function iniciarEdicao(m: ModeloDescricaoCirurgica) {
    editandoId.value = m.id
    form.titulo = m.titulo
    form.corpo = m.corpo
    erroForm.value = null
}

async function salvar() {
    erroForm.value = null
    if (!form.titulo.trim()) { erroForm.value = "Título é obrigatório."; return }
    if (!form.corpo.trim()) { erroForm.value = "Corpo é obrigatório."; return }

    salvando.value = true
    try {
        if (editandoId.value !== null) {
            await modeloDescricaoCirurgicaService.editar(editandoId.value, form.titulo, form.corpo)
            notificar("Modelo atualizado.")
        } else {
            await modeloDescricaoCirurgicaService.criar(form.titulo, form.corpo)
            notificar("Modelo criado.")
        }
        resetForm()
        await carregar()
    } catch (e: any) {
        erroForm.value = e?.response?.data?.mensagem ?? "Erro ao salvar modelo."
    } finally {
        salvando.value = false
    }
}

// ─── Exclusão ─────────────────────────────────────────────────────────────────
const confirmExcluir = ref<{ aberto: boolean; alvo: ModeloDescricaoCirurgica | null; executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

function pedirExcluir(m: ModeloDescricaoCirurgica) {
    confirmExcluir.value = { aberto: true, alvo: m, executando: false }
}

async function confirmarExcluir() {
    const alvo = confirmExcluir.value.alvo
    if (!alvo) return
    confirmExcluir.value.executando = true
    try {
        await modeloDescricaoCirurgicaService.excluir(alvo.id)
        confirmExcluir.value = { aberto: false, alvo: null, executando: false }
        notificar("Modelo excluído.")
        await carregar()
    } catch (e: any) {
        confirmExcluir.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Erro ao excluir modelo.", "error")
    }
}

// ─── Carregamento ──────────────────────────────────────────────────────────────
async function carregar() {
    carregando.value = true
    try {
        modelos.value = await modeloDescricaoCirurgicaService.listar()
    } catch {
        notificar("Erro ao carregar modelos.", "error")
    } finally {
        carregando.value = false
    }
}

onMounted(carregar)
</script>

<template>
    <div class="mdc-tab">
        <p v-if="!podeEditar" class="aviso-leitura">
            Você está visualizando em modo leitura. Para criar ou editar modelos, é necessária a permissão "Modelos de prontuário".
        </p>

        <!-- ─── Form criar/editar ─────────────────────────────────────────── -->
        <AppCard v-if="podeEditar" padding="md">
            <h3 class="ds-card-title">{{ editandoId !== null ? "Editar modelo" : "Novo modelo" }}</h3>

            <div class="mdc-form">
                <AppField label="Título" :erro="erroForm && !form.titulo.trim() ? erroForm : undefined">
                    <AppInput
                        v-model="form.titulo"
                        placeholder="ex: Rinoplastia estruturada"
                        maxlength="200"
                    />
                </AppField>

                <AppField label="Corpo do modelo" :erro="erroForm && form.titulo.trim() && !form.corpo.trim() ? erroForm : undefined">
                    <AppTextarea
                        v-model="form.corpo"
                        :rows="6"
                        placeholder="Descreva o procedimento cirúrgico padrão..."
                    />
                </AppField>

                <p v-if="erroForm" class="msg-erro">{{ erroForm }}</p>

                <div class="mdc-form-acoes">
                    <AppButton v-if="editandoId !== null" variant="ghost" @click="resetForm">
                        Cancelar
                    </AppButton>
                    <AppButton
                        :loading="salvando"
                        :disabled="salvando"
                        @click="salvar"
                    >
                        {{ editandoId !== null ? "Atualizar" : "Criar modelo" }}
                    </AppButton>
                </div>
            </div>
        </AppCard>

        <!-- ─── Lista ────────────────────────────────────────────────────── -->
        <div v-if="carregando" class="estado-msg">Carregando...</div>

        <AppEmptyState
            v-else-if="modelos.length === 0"
            icone="fa-solid fa-file-pen"
            titulo="Nenhum modelo cadastrado ainda."
            descricao="Crie um modelo de descrição cirúrgica para reutilizá-lo na evolução."
        >
            <template v-if="podeEditar" #acoes>
                <AppButton icon="fa-solid fa-plus" @click="resetForm">
                    Novo modelo
                </AppButton>
            </template>
        </AppEmptyState>

        <ul v-else class="mdc-lista">
            <li v-for="m in modelos" :key="m.id" class="mdc-item">
                <div class="mdc-item-info">
                    <span class="mdc-titulo">{{ m.titulo }}</span>
                    <span v-if="m.ehPadraoSistema" class="badge-padrao">Padrão do sistema</span>
                    <p class="mdc-preview">{{ m.corpo.slice(0, 120) }}{{ m.corpo.length > 120 ? "…" : "" }}</p>
                </div>
                <div v-if="podeEditar && !m.ehPadraoSistema" class="mdc-item-acoes">
                    <button type="button" class="btn-icon btn-icon-editar" title="Editar" @click="iniciarEdicao(m)">
                        <i class="fa-solid fa-pen"></i>
                    </button>
                    <button type="button" class="btn-icon btn-icon-excluir" title="Excluir" @click="pedirExcluir(m)">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </div>
            </li>
        </ul>

        <AppConfirmDialog
            v-model:aberto="confirmExcluir.aberto"
            titulo="Excluir modelo?"
            :mensagem="confirmExcluir.alvo ? `Deseja realmente excluir o modelo &quot;${confirmExcluir.alvo.titulo}&quot;?` : ''"
            confirmar-rotulo="Excluir"
            variante="danger"
            :executando="confirmExcluir.executando"
            @confirmar="confirmarExcluir"
        />

        <AppToast
            v-if="toast"
            :mensagem="toast.mensagem"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </div>
</template>

<style scoped>
.mdc-tab { display: flex; flex-direction: column; gap: 1rem; }

.aviso-leitura {
    background: hsl(45 100% 90%); color: hsl(30 90% 30%);
    padding: 0.65rem 0.9rem; border-radius: var(--radius);
    font-size: var(--text-xs); margin: 0;
}

.mdc-form { display: flex; flex-direction: column; gap: 0.75rem; }
.mdc-form-acoes { display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 0.25rem; }

.estado-msg { text-align: center; color: var(--text-muted); padding: 1.5rem 1rem; font-size: var(--text-sm); }

.mdc-lista { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.5rem; }
.mdc-item {
    display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem;
    padding: 0.75rem 1rem;
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius-lg);
}
.mdc-item-info { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 4px; }
.mdc-titulo { font-weight: var(--font-weight-semibold); font-size: var(--text-sm); color: hsl(var(--primary-dark)); }

.badge-padrao {
    display: inline-flex; align-items: center;
    background: hsl(var(--secondary) / 0.08); color: hsl(var(--secondary));
    padding: 0.1rem 0.5rem; border-radius: 999px;
    font-size: var(--text-xs); font-weight: var(--font-weight-semibold);
    align-self: flex-start;
}

.mdc-preview { font-size: var(--text-xs); color: var(--text-muted); margin: 0; white-space: pre-line; }
.mdc-item-acoes { display: flex; gap: 0.4rem; flex-shrink: 0; }
.msg-erro { color: var(--danger); font-size: var(--text-xs); margin: 0; }
</style>
