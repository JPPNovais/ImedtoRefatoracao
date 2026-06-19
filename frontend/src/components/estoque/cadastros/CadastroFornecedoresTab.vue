<script setup lang="ts">
import { ref, watch, onMounted, computed } from "vue"
import { vMaska } from "maska/vue"
import type { MaskInputOptions } from "maska"
import {
    AppSearchInput, AppButton, AppEmptyState, AppPagination, AppDrawer,
    AppField, AppInput, AppStatusPill, AppToast, AppConfirmDialog, AppPillToggle,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import {
    estoqueCadastrosService,
    type FornecedorEstoque,
    type FornecedorPayload,
} from "@/services/estoqueCadastrosService"
import { validateCnpj, formatarCnpj, normalizarCnpj } from "@/utils/validateCnpj"

// Máscara inteligente CNPJ alfanumérico (IN RFB 2.229/2024):
// posições 1-12 aceitam [A-Z0-9] com uppercase; posições 13-14 (DV) só dígitos.
const cnpjMaskaOpts: MaskInputOptions = {
    mask: "XX.XXX.XXX/XXXX-##",
    tokens: {
        X: { pattern: /[A-Z0-9]/i, transform: (c: string) => c.toUpperCase() },
    },
}

const emit = defineEmits<{ "total-change": [total: number] }>()

const itens = ref<FornecedorEstoque[]>([])
const total = ref(0)
const pagina = ref(1)
const tamanho = ref(10)
const carregando = ref(false)
const erro = ref<string | null>(null)

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
const apenasAtivos = ref(true)

// Form
const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}
const confirmacao = ref<{ aberto: boolean, alvo: FornecedorEstoque | null, executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

const drawerAberto = ref(false)
const editando = ref<FornecedorEstoque | null>(null)
const opcoesTipoPrazo = [
    { valor: 'corridos' as string, label: 'Corridos' },
    { valor: 'uteis' as string, label: 'Úteis' },
]

const form = ref<FornecedorPayload>({
    razaoSocial: "",
    nomeFantasia: "",
    cnpj: "",
    contatoNome: "",
    contatoTelefone: "",
    contatoEmail: "",
    prazoEntregaDias: 5,
    tipoPrazoEntrega: 'corridos',
})
const erroForm = ref<string | null>(null)
const salvando = ref(false)

const cnpjValido = computed(() => validateCnpj(form.value.cnpj))

function abrirCriar() {
    editando.value = null
    form.value = { razaoSocial: "", nomeFantasia: "", cnpj: "", contatoNome: "", contatoTelefone: "", contatoEmail: "", prazoEntregaDias: 5, tipoPrazoEntrega: 'corridos' }
    erroForm.value = null
    drawerAberto.value = true
}

function abrirEditar(f: FornecedorEstoque) {
    editando.value = f
    form.value = {
        razaoSocial: f.razaoSocial,
        nomeFantasia: f.nomeFantasia ?? "",
        cnpj: f.cnpj ? formatarCnpj(f.cnpj) : "",
        contatoNome: f.contatoNome ?? "",
        contatoTelefone: f.contatoTelefone ?? "",
        contatoEmail: f.contatoEmail ?? "",
        prazoEntregaDias: f.prazoEntregaDias,
        tipoPrazoEntrega: f.tipoPrazoEntrega,
    }
    erroForm.value = null
    drawerAberto.value = true
}

async function salvar() {
    erroForm.value = null
    if (!form.value.razaoSocial.trim()) { erroForm.value = "Razão social é obrigatória."; return }
    if (!cnpjValido.value) { erroForm.value = "CNPJ inválido."; return }
    if (form.value.prazoEntregaDias < 0) { erroForm.value = "Prazo de entrega não pode ser negativo."; return }
    salvando.value = true
    try {
        const cnpjCanônico = normalizarCnpj(form.value.cnpj)
        const payload: FornecedorPayload = {
            razaoSocial: form.value.razaoSocial,
            nomeFantasia: form.value.nomeFantasia?.trim() || null,
            cnpj: cnpjCanônico || null,
            contatoNome: form.value.contatoNome?.trim() || null,
            contatoTelefone: form.value.contatoTelefone?.trim() || null,
            contatoEmail: form.value.contatoEmail?.trim() || null,
            prazoEntregaDias: form.value.prazoEntregaDias,
            tipoPrazoEntrega: form.value.tipoPrazoEntrega ?? 'corridos',
        }
        if (editando.value) {
            await estoqueCadastrosService.fornecedores.atualizar(editando.value.id, payload)
        } else {
            await estoqueCadastrosService.fornecedores.criar(payload)
        }
        drawerAberto.value = false
        await carregar()
    } catch (e: any) {
        erroForm.value = e?.response?.data?.mensagem ?? "Erro ao salvar fornecedor."
    } finally {
        salvando.value = false
    }
}

function pedirInativacao(f: FornecedorEstoque) {
    confirmacao.value = { aberto: true, alvo: f, executando: false }
}

async function executarInativacao() {
    const alvo = confirmacao.value.alvo
    if (!alvo) return
    confirmacao.value.executando = true
    try {
        await estoqueCadastrosService.fornecedores.inativar(alvo.id)
        confirmacao.value = { aberto: false, alvo: null, executando: false }
        notificar("Fornecedor inativado.")
        await carregar()
    } catch (e: any) {
        confirmacao.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Erro ao inativar.", "error")
    }
}

async function reativar(f: FornecedorEstoque) {
    try {
        await estoqueCadastrosService.fornecedores.reativar(f.id)
        await carregar()
        notificar("Fornecedor reativado.")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao reativar.", "error")
    }
}

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const pg = await estoqueCadastrosService.fornecedores.listar({
            busca: busca.value || undefined,
            apenasAtivos: apenasAtivos.value ? true : undefined,
            pagina: pagina.value,
            tamanho: tamanho.value,
        })
        itens.value = pg.itens
        total.value = pg.total
        emit("total-change", pg.total)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar fornecedores."
    } finally {
        carregando.value = false
    }
}

watch(busca, () => { pagina.value = 1 })
watch([busca, pagina, tamanho, apenasAtivos], () => carregar(), { immediate: false })

onMounted(carregar)
</script>

<template>
    <div class="cad-tab">
        <div class="filtros-bar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar por razão social ou fantasia..." />
            <label class="filtro-ativos">
                <input type="checkbox" v-model="apenasAtivos" /> Mostrar só ativos
            </label>
            <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Novo fornecedor</AppButton>
        </div>

        <div v-if="erro" class="msg-erro">{{ erro }}</div>

        <div class="tabela">
            <div class="thead" aria-hidden="true">
                <div>Fornecedor</div>
                <div>CNPJ</div>
                <div>Contato</div>
                <div>Prazo</div>
                <div>Itens</div>
                <div>Status</div>
                <div></div>
            </div>

            <div v-if="carregando" class="tabela-loading"><i class="fa-solid fa-spinner fa-spin"></i> Carregando…</div>

            <div v-else-if="itens.length === 0" class="tabela-vazio">
                <AppEmptyState
                    icone="fa-solid fa-truck"
                    titulo="Nenhum fornecedor cadastrado"
                    descricao="Cadastre os fornecedores para acelerar a criação de pedidos de compra."
                    :compacto="true"
                >
                    <template #acao>
                        <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Novo fornecedor</AppButton>
                    </template>
                </AppEmptyState>
            </div>

            <div v-else>
                <div v-for="f in itens" :key="f.id" class="row" :class="{ inativo: !f.ativo }">
                    <div>
                        <b>{{ f.razaoSocial }}</b>
                        <small v-if="f.nomeFantasia">{{ f.nomeFantasia }}</small>
                    </div>
                    <div class="muted mono">{{ f.cnpj ? formatarCnpj(f.cnpj) : "—" }}</div>
                    <div>
                        <div v-if="f.contatoNome" class="contato-nome">{{ f.contatoNome }}</div>
                        <small class="muted">{{ f.contatoTelefone || f.contatoEmail || "—" }}</small>
                    </div>
                    <div class="muted">{{ f.prazoEntregaDias }}d {{ f.tipoPrazoEntrega === 'uteis' ? 'úteis' : 'corridos' }}</div>
                    <div class="muted">{{ f.quantidadeItens }}</div>
                    <div>
                        <AppStatusPill :label="f.ativo ? 'Ativo' : 'Inativo'" :variante="f.ativo ? 'success' : 'muted'" />
                    </div>
                    <div class="acoes">
                        <button type="button" class="btn-icon btn-icon-editar" title="Editar" @click="abrirEditar(f)">
                            <i class="fa-solid fa-pen"></i>
                        </button>
                        <button v-if="f.ativo" type="button" class="btn-icon btn-icon-excluir" title="Inativar" @click="pedirInativacao(f)">
                            <i class="fa-solid fa-ban"></i>
                        </button>
                        <button v-else type="button" class="btn-icon" title="Reativar" @click="reativar(f)">
                            <i class="fa-solid fa-rotate-left"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <AppPagination
            v-if="total > 0 && !carregando"
            :pagina="pagina"
            :tamanho="tamanho"
            :total="total"
            rotulo-itens="fornecedores"
            class="paginacao"
            @update:pagina="(p: number) => (pagina = p)"
            @update:tamanho="(t: number) => (tamanho = t)"
        />

        <AppConfirmDialog
            v-model:aberto="confirmacao.aberto"
            titulo="Inativar fornecedor?"
            :mensagem="confirmacao.alvo ? `Inativar ${confirmacao.alvo.razaoSocial}?` : ''"
            confirmar-rotulo="Inativar"
            variante="danger"
            :executando="confirmacao.executando"
            @confirmar="executarInativacao"
        />

        <AppToast
            v-if="toast"
            :mensagem="toast.mensagem"
            :variante="toast.variante"
            @fechar="toast = null"
        />

        <AppDrawer
            :aberto="drawerAberto"
            :titulo="editando ? `Editar — ${editando.razaoSocial}` : 'Novo fornecedor'"
            :largura="600"
            @fechar="drawerAberto = false"
        >
            <div class="form-drawer">
                <div class="grid-2">
                    <AppField label="Razão social" required class="full">
                        <AppInput v-model="form.razaoSocial" placeholder="Ex: Empresa LTDA" />
                    </AppField>
                    <AppField label="Nome fantasia" class="full">
                        <AppInput v-model="form.nomeFantasia" />
                    </AppField>

                    <AppField
                        label="CNPJ"
                        :erro="form.cnpj && !cnpjValido ? 'CNPJ inválido' : null"
                    >
                        <AppInput v-model="form.cnpj" v-maska="cnpjMaskaOpts" placeholder="00.000.000/0000-00" />
                    </AppField>
                    <AppField label="Prazo de entrega (dias)">
                        <AppInput v-model="form.prazoEntregaDias" type="number" :min="0" />
                    </AppField>
                    <AppField label="Tipo de prazo">
                        <AppPillToggle :model-value="form.tipoPrazoEntrega ?? 'corridos'" :opcoes="opcoesTipoPrazo" @update:model-value="(v: string) => form.tipoPrazoEntrega = v as 'corridos' | 'uteis'" />
                    </AppField>

                    <div class="sub-titulo full">Contato</div>
                    <AppField label="Nome do contato">
                        <AppInput v-model="form.contatoNome" />
                    </AppField>
                    <AppField label="Telefone">
                        <AppInput v-model="form.contatoTelefone" placeholder="(11) 99999-9999" />
                    </AppField>
                    <AppField label="E-mail" class="full">
                        <AppInput v-model="form.contatoEmail" type="email" placeholder="contato@empresa.com" />
                    </AppField>
                </div>

                <p v-if="erroForm" class="msg-erro">{{ erroForm }}</p>

                <div class="acoes-form">
                    <AppButton variant="secondary" @click="drawerAberto = false">Cancelar</AppButton>
                    <AppButton
                        :icon="editando ? 'fa-solid fa-check' : 'fa-solid fa-plus'"
                        :loading="salvando"
                        :disabled="salvando"
                        @click="salvar"
                    >
                        {{ editando ? "Salvar" : "Criar" }}
                    </AppButton>
                </div>
            </div>
        </AppDrawer>
    </div>
</template>

<style scoped>
.cad-tab { display: flex; flex-direction: column; gap: 12px; }
.filtros-bar { display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }
.filtro-ativos { display: inline-flex; align-items: center; gap: 6px; font-size: 13px; color: hsl(var(--secondary) / 0.7); }
.msg-erro { color: hsl(var(--error)); font-size: 13px; }

.tabela {
    background: var(--bg-card);
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: var(--radius-lg);
    overflow: hidden;
    box-shadow: var(--shadow);
}
.thead, .row {
    display: grid;
    grid-template-columns: 2.2fr 1.4fr 1.5fr 0.6fr 0.6fr 110px auto;
    gap: 12px; align-items: center; padding: 11px 16px;
}
.thead {
    background: hsl(var(--secondary) / 0.025);
    border-bottom: 1px solid hsl(var(--secondary) / 0.08);
    font-size: 10px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.05em;
    color: hsl(var(--secondary) / 0.55);
}
.row { border-bottom: 1px solid hsl(var(--secondary) / 0.05); }
.row:last-child { border-bottom: 0; }
.row.inativo { opacity: 0.6; }
.row:hover { background: hsl(var(--primary) / 0.025); }
.row b { font-size: 13px; color: hsl(var(--primary-dark)); display: block; }
.row small { font-size: 11px; color: hsl(var(--secondary) / 0.55); }
.contato-nome { font-size: 13px; color: hsl(var(--primary-dark)); }
.muted { color: hsl(var(--secondary) / 0.65); font-size: 13px; }
.mono { font-family: monospace; font-size: 12px; }
.acoes { display: flex; gap: 4px; }
.tabela-loading, .tabela-vazio { padding: 32px; text-align: center; color: hsl(var(--secondary) / 0.6); }
.paginacao { margin-top: 4px; }
.form-drawer { display: flex; flex-direction: column; gap: 16px; padding: 20px; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.grid-2 .full { grid-column: 1 / -1; }
.sub-titulo {
    font-size: 12px; font-weight: 700; color: hsl(var(--secondary) / 0.55);
    text-transform: uppercase; letter-spacing: 0.05em; margin-top: 8px;
}
.acoes-form { display: flex; gap: 8px; justify-content: flex-end; margin-top: 8px; }
</style>
