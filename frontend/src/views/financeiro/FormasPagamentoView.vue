<script setup lang="ts">
import { ref, onMounted } from "vue"
import {
    formaPagamentoService,
    type FormaPagamento,
} from "@/services/categoriaFinanceiraService"
import {
    AppPageHeader, AppButton, AppDrawer, AppField, AppInput,
    AppEmptyState, AppModal,
} from "@/components/ui"

const formas = ref<FormaPagamento[]>([])
const carregando = ref(false)
const drawerAberto = ref(false)
const excluindoId = ref<number | null>(null)
const confirmandoExcluirId = ref<number | null>(null)
const salvando = ref(false)
const erro = ref<string | null>(null)

const modo = ref<"criar" | "editar">("criar")
const editandoId = ref<number | null>(null)

const formDados = ref({ nome: "" })
const erroForm = ref<string | null>(null)

async function carregar() {
    carregando.value = true
    try {
        formas.value = await formaPagamentoService.listar()
    } catch {
        // silencioso
    } finally {
        carregando.value = false
    }
}

function abrirCriar() {
    modo.value = "criar"
    editandoId.value = null
    formDados.value = { nome: "" }
    erroForm.value = null
    drawerAberto.value = true
}

function abrirEditar(forma: FormaPagamento) {
    if (forma.padrao) return
    modo.value = "editar"
    editandoId.value = forma.id
    formDados.value = { nome: forma.nome }
    erroForm.value = null
    drawerAberto.value = true
}

function fecharDrawer() {
    drawerAberto.value = false
    erroForm.value = null
}

async function salvar() {
    if (!formDados.value.nome.trim()) {
        erroForm.value = "Informe o nome da forma de pagamento."
        return
    }
    salvando.value = true
    erroForm.value = null
    try {
        if (modo.value === "criar") {
            await formaPagamentoService.criar(formDados.value)
        } else if (editandoId.value !== null) {
            await formaPagamentoService.atualizar(editandoId.value, formDados.value)
        }
        fecharDrawer()
        await carregar()
    } catch (e: any) {
        erroForm.value = e?.response?.data?.mensagem ?? "Erro ao salvar."
    } finally {
        salvando.value = false
    }
}

async function excluir(id: number) {
    excluindoId.value = id
    confirmandoExcluirId.value = null
    try {
        await formaPagamentoService.excluir(id)
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao excluir."
        setTimeout(() => (erro.value = null), 4000)
    } finally {
        excluindoId.value = null
    }
}

onMounted(carregar)
</script>

<template>
    <main class="app-page">
        <AppPageHeader titulo="Formas de pagamento" subtitulo="Gerencie as formas de pagamento aceitas pelo estabelecimento.">
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Nova forma</AppButton>
            </template>
        </AppPageHeader>

        <div v-if="erro" class="erro-banner" role="alert">
            <i class="fa-solid fa-triangle-exclamation"></i>
            {{ erro }}
        </div>

        <div v-if="carregando" class="carregando">
            <i class="fa-solid fa-spinner fa-spin"></i>
            Carregando...
        </div>

        <AppEmptyState
            v-else-if="formas.length === 0"
            icone="fa-solid fa-credit-card"
            titulo="Nenhuma forma de pagamento"
            descricao="Crie a primeira forma de pagamento."
        >
            <template #acao>
                <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Nova forma</AppButton>
            </template>
        </AppEmptyState>

        <table v-else class="tabela">
            <thead>
                <tr>
                    <th>Nome</th>
                    <th>Padrão</th>
                    <th class="acoes-th">Ações</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="forma in formas" :key="forma.id">
                    <td>{{ forma.nome }}</td>
                    <td>
                        <span v-if="forma.padrao" class="tag-padrao">Padrão</span>
                    </td>
                    <td class="acoes">
                        <button
                            v-if="!forma.padrao"
                            class="btn-icon btn-icon-editar"
                            title="Editar"
                            @click="abrirEditar(forma)"
                        >
                            <i class="fa-solid fa-pen" aria-hidden="true"></i>
                        </button>
                        <button
                            v-if="!forma.padrao"
                            class="btn-icon btn-icon-excluir"
                            title="Excluir"
                            :disabled="excluindoId === forma.id"
                            @click="confirmandoExcluirId = forma.id"
                        >
                            <i class="fa-solid fa-trash" aria-hidden="true"></i>
                        </button>
                    </td>
                </tr>
            </tbody>
        </table>

        <!-- Drawer criar/editar -->
        <AppDrawer
            :aberto="drawerAberto"
            :titulo="modo === 'criar' ? 'Nova forma de pagamento' : 'Editar forma de pagamento'"
            @fechar="fecharDrawer"
        >
            <div class="form-campos">
                <AppField label="Nome" required :erro="erroForm ?? undefined" for="fp-nome">
                    <AppInput id="fp-nome" v-model="formDados.nome" placeholder="Ex: PIX" />
                </AppField>
            </div>

            <template #rodape>
                <AppButton variant="secondary" @click="fecharDrawer">Cancelar</AppButton>
                <AppButton :loading="salvando" @click="salvar">
                    {{ modo === "criar" ? "Criar" : "Salvar" }}
                </AppButton>
            </template>
        </AppDrawer>

        <!-- Modal confirmar exclusão -->
        <AppModal
            :aberto="confirmandoExcluirId !== null"
            titulo="Excluir forma de pagamento?"
            largura="sm"
            :acima-de-drawer="drawerAberto"
            @fechar="confirmandoExcluirId = null"
        >
            <p>Esta ação é irreversível.</p>
            <template #rodape>
                <AppButton variant="secondary" @click="confirmandoExcluirId = null">Cancelar</AppButton>
                <AppButton
                    variant="danger"
                    :loading="excluindoId !== null"
                    @click="confirmandoExcluirId && excluir(confirmandoExcluirId)"
                >
                    Excluir
                </AppButton>
            </template>
        </AppModal>
    </main>
</template>

<style scoped>
.carregando {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: hsl(var(--muted-foreground));
    padding: 2rem 0;
    font-size: 0.9em;
}

.erro-banner {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    font-size: 0.9em;
    margin-bottom: 1rem;
}

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.9em;
}
.tabela th, .tabela td {
    padding: 0.7rem 0.9rem;
    text-align: left;
    border-bottom: 1px solid hsl(var(--border));
}
.tabela th {
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    background: hsl(var(--muted) / 0.4);
    font-size: 0.82em;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.tabela tbody tr:hover { background: hsl(var(--muted) / 0.3); }

.acoes-th { width: 80px; }
.acoes { display: flex; gap: 0.25rem; }

.tag-padrao {
    display: inline-block;
    padding: 0.1rem 0.45rem;
    border-radius: 999px;
    font-size: 0.72em;
    font-weight: 600;
    background: hsl(var(--muted));
    color: hsl(var(--muted-foreground));
}

.form-campos {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 0.25rem 0;
}
</style>
