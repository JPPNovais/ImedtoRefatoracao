<script setup lang="ts">
import { ref, watch, onMounted } from "vue"
import {
    AppSearchInput, AppButton, AppEmptyState, AppPagination, AppDrawer,
    AppField, AppInput, AppSelect, AppStatusPill, AppToast, AppConfirmDialog,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import {
    estoqueCadastrosService,
    type LocalEstoque,
    type LocalPayload,
    type TipoLocalEstoque,
    TIPOS_LOCAL_ESTOQUE,
} from "@/services/estoqueCadastrosService"

const emit = defineEmits<{ "total-change": [total: number] }>()

const itens = ref<LocalEstoque[]>([])
const total = ref(0)
const pagina = ref(1)
const tamanho = ref(20)
const carregando = ref(false)
const erro = ref<string | null>(null)

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
const apenasAtivos = ref(true)

const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}
const confirmacao = ref<{ aberto: boolean, alvo: LocalEstoque | null, executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

const drawerAberto = ref(false)
const editando = ref<LocalEstoque | null>(null)
const form = ref<LocalPayload>({ nome: "", tipo: "Armario", andarSetor: "", responsavel: "" })
const erroForm = ref<string | null>(null)
const salvando = ref(false)

function abrirCriar() {
    editando.value = null
    form.value = { nome: "", tipo: "Armario", andarSetor: "", responsavel: "" }
    erroForm.value = null
    drawerAberto.value = true
}

function abrirEditar(l: LocalEstoque) {
    editando.value = l
    form.value = { nome: l.nome, tipo: l.tipo, andarSetor: l.andarSetor ?? "", responsavel: l.responsavel ?? "" }
    erroForm.value = null
    drawerAberto.value = true
}

async function salvar() {
    erroForm.value = null
    if (!form.value.nome.trim()) { erroForm.value = "Nome é obrigatório."; return }
    if (!form.value.tipo) { erroForm.value = "Tipo é obrigatório."; return }
    salvando.value = true
    try {
        const payload: LocalPayload = {
            nome: form.value.nome,
            tipo: form.value.tipo,
            andarSetor: form.value.andarSetor?.trim() || null,
            responsavel: form.value.responsavel?.trim() || null,
        }
        if (editando.value) {
            await estoqueCadastrosService.locais.atualizar(editando.value.id, payload)
        } else {
            await estoqueCadastrosService.locais.criar(payload)
        }
        drawerAberto.value = false
        await carregar()
    } catch (e: any) {
        erroForm.value = e?.response?.data?.mensagem ?? "Erro ao salvar local."
    } finally {
        salvando.value = false
    }
}

function pedirInativacao(l: LocalEstoque) {
    confirmacao.value = { aberto: true, alvo: l, executando: false }
}

async function executarInativacao() {
    const alvo = confirmacao.value.alvo
    if (!alvo) return
    confirmacao.value.executando = true
    try {
        await estoqueCadastrosService.locais.inativar(alvo.id)
        confirmacao.value = { aberto: false, alvo: null, executando: false }
        notificar("Local inativado.")
        await carregar()
    } catch (e: any) {
        confirmacao.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Erro ao inativar.", "error")
    }
}

async function reativar(l: LocalEstoque) {
    try {
        await estoqueCadastrosService.locais.reativar(l.id)
        await carregar()
        notificar("Local reativado.")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao reativar.", "error")
    }
}

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const pg = await estoqueCadastrosService.locais.listar({
            busca: busca.value || undefined,
            apenasAtivos: apenasAtivos.value ? true : undefined,
            pagina: pagina.value,
            tamanho: tamanho.value,
        })
        itens.value = pg.itens
        total.value = pg.total
        emit("total-change", pg.total)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar locais."
    } finally {
        carregando.value = false
    }
}

watch(busca, () => { pagina.value = 1 })
watch([busca, pagina, tamanho, apenasAtivos], () => carregar(), { immediate: false })

onMounted(carregar)

function iconePorTipo(t: TipoLocalEstoque): string {
    switch (t) {
        case "Armario":     return "fa-cabinet-filing"
        case "Gaveta":      return "fa-table-cells"
        case "Refrigerado": return "fa-snowflake"
        case "Cofre":       return "fa-vault"
        case "Estante":     return "fa-bookmark"
        case "Sala":        return "fa-door-open"
    }
}
</script>

<template>
    <div class="cad-tab">
        <div class="filtros-bar">
            <AppSearchInput v-model="buscaInput" placeholder="Buscar local..." />
            <label class="filtro-ativos">
                <input type="checkbox" v-model="apenasAtivos" /> Mostrar só ativos
            </label>
            <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Novo local</AppButton>
        </div>

        <div v-if="erro" class="msg-erro">{{ erro }}</div>

        <div class="tabela">
            <div class="thead" aria-hidden="true">
                <div>Local</div>
                <div>Tipo</div>
                <div>Andar / setor</div>
                <div>Responsável</div>
                <div>Itens</div>
                <div>Status</div>
                <div></div>
            </div>

            <div v-if="carregando" class="tabela-loading"><i class="fa-solid fa-spinner fa-spin"></i> Carregando…</div>

            <div v-else-if="itens.length === 0" class="tabela-vazio">
                <AppEmptyState
                    icone="fa-solid fa-warehouse"
                    titulo="Nenhum local cadastrado"
                    descricao="Cadastre locais físicos para organizar onde cada item fica armazenado."
                    :compacto="true"
                >
                    <template #acao>
                        <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Novo local</AppButton>
                    </template>
                </AppEmptyState>
            </div>

            <div v-else>
                <div v-for="l in itens" :key="l.id" class="row" :class="{ inativo: !l.ativo }">
                    <div class="local-nome">
                        <i :class="`fa-solid ${iconePorTipo(l.tipo)}`" aria-hidden="true"></i>
                        <b>{{ l.nome }}</b>
                    </div>
                    <div class="muted">{{ l.tipo }}</div>
                    <div class="muted">{{ l.andarSetor || "—" }}</div>
                    <div class="muted">{{ l.responsavel || "—" }}</div>
                    <div class="muted">{{ l.quantidadeItens }}</div>
                    <div>
                        <AppStatusPill :label="l.ativo ? 'Ativo' : 'Inativo'" :variante="l.ativo ? 'success' : 'muted'" />
                    </div>
                    <div class="acoes">
                        <button type="button" class="btn-icon btn-icon-editar" title="Editar" @click="abrirEditar(l)">
                            <i class="fa-solid fa-pen"></i>
                        </button>
                        <button v-if="l.ativo" type="button" class="btn-icon btn-icon-excluir" title="Inativar" @click="pedirInativacao(l)">
                            <i class="fa-solid fa-ban"></i>
                        </button>
                        <button v-else type="button" class="btn-icon" title="Reativar" @click="reativar(l)">
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
            rotulo-itens="locais"
            class="paginacao"
            @update:pagina="(p: number) => (pagina = p)"
            @update:tamanho="(t: number) => (tamanho = t)"
        />

        <AppConfirmDialog
            v-model:aberto="confirmacao.aberto"
            titulo="Inativar local?"
            :mensagem="confirmacao.alvo ? `Inativar ${confirmacao.alvo.nome}?` : ''"
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
            :titulo="editando ? `Editar — ${editando.nome}` : 'Novo local'"
            :largura="500"
            @fechar="drawerAberto = false"
        >
            <div class="form-drawer">
                <AppField label="Nome" required>
                    <AppInput v-model="form.nome" placeholder="Ex: Armário sala 2" />
                </AppField>
                <AppField label="Tipo" required>
                    <AppSelect v-model="form.tipo">
                        <option v-for="t in TIPOS_LOCAL_ESTOQUE" :key="t" :value="t">{{ t }}</option>
                    </AppSelect>
                </AppField>
                <AppField label="Andar / Setor">
                    <AppInput v-model="form.andarSetor" placeholder="Ex: 2º andar, recepção" />
                </AppField>
                <AppField label="Responsável">
                    <AppInput v-model="form.responsavel" placeholder="Nome do responsável" />
                </AppField>

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
    grid-template-columns: 2fr 1fr 1.2fr 1.2fr 0.6fr 110px auto;
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
.local-nome { display: flex; align-items: center; gap: 8px; }
.local-nome b { font-size: 14px; color: hsl(var(--primary-dark)); }
.local-nome i { color: hsl(var(--primary)); width: 18px; text-align: center; }
.muted { color: hsl(var(--secondary) / 0.65); font-size: 13px; }
.acoes { display: flex; gap: 4px; }
.tabela-loading, .tabela-vazio { padding: 32px; text-align: center; color: hsl(var(--secondary) / 0.6); }
.paginacao { margin-top: 4px; }
.form-drawer { display: flex; flex-direction: column; gap: 16px; padding: 20px; }
.acoes-form { display: flex; gap: 8px; justify-content: flex-end; margin-top: 8px; }
</style>
