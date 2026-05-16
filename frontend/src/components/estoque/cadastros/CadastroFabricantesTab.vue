<script setup lang="ts">
import { ref, watch, onMounted } from "vue"
import {
    AppSearchInput, AppButton, AppEmptyState, AppPagination, AppDrawer,
    AppField, AppInput, AppStatusPill,
} from "@/components/ui"
import { useDebouncedRef } from "@/composables/useDebouncedRef"
import {
    estoqueCadastrosService,
    type FabricanteEstoque,
    type FabricantePayload,
} from "@/services/estoqueCadastrosService"

const emit = defineEmits<{ "total-change": [total: number] }>()

const itens = ref<FabricanteEstoque[]>([])
const total = ref(0)
const pagina = ref(1)
const tamanho = ref(20)
const carregando = ref(false)
const erro = ref<string | null>(null)

const buscaInput = ref("")
const busca = useDebouncedRef(buscaInput)
const apenasAtivos = ref(true)

const drawerAberto = ref(false)
const editando = ref<FabricanteEstoque | null>(null)
const form = ref<FabricantePayload>({ nome: "", pais: "" })
const erroForm = ref<string | null>(null)
const salvando = ref(false)

function abrirCriar() {
    editando.value = null
    form.value = { nome: "", pais: "" }
    erroForm.value = null
    drawerAberto.value = true
}

function abrirEditar(f: FabricanteEstoque) {
    editando.value = f
    form.value = { nome: f.nome, pais: f.pais ?? "" }
    erroForm.value = null
    drawerAberto.value = true
}

async function salvar() {
    erroForm.value = null
    if (!form.value.nome.trim()) { erroForm.value = "Nome é obrigatório."; return }
    salvando.value = true
    try {
        const payload: FabricantePayload = {
            nome: form.value.nome,
            pais: form.value.pais?.trim() || null,
        }
        if (editando.value) {
            await estoqueCadastrosService.fabricantes.atualizar(editando.value.id, payload)
        } else {
            await estoqueCadastrosService.fabricantes.criar(payload)
        }
        drawerAberto.value = false
        await carregar()
    } catch (e: any) {
        erroForm.value = e?.response?.data?.mensagem ?? "Erro ao salvar fabricante."
    } finally {
        salvando.value = false
    }
}

async function inativar(f: FabricanteEstoque) {
    if (!confirm(`Inativar "${f.nome}"?`)) return
    try {
        await estoqueCadastrosService.fabricantes.inativar(f.id)
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao inativar.")
    }
}

async function reativar(f: FabricanteEstoque) {
    try {
        await estoqueCadastrosService.fabricantes.reativar(f.id)
        await carregar()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao reativar.")
    }
}

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const pg = await estoqueCadastrosService.fabricantes.listar({
            busca: busca.value || undefined,
            apenasAtivos: apenasAtivos.value ? true : undefined,
            pagina: pagina.value,
            tamanho: tamanho.value,
        })
        itens.value = pg.itens
        total.value = pg.total
        emit("total-change", pg.total)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao carregar fabricantes."
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
            <AppSearchInput v-model="buscaInput" placeholder="Buscar fabricante..." />
            <label class="filtro-ativos">
                <input type="checkbox" v-model="apenasAtivos" /> Mostrar só ativos
            </label>
            <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Novo fabricante</AppButton>
        </div>

        <div v-if="erro" class="msg-erro">{{ erro }}</div>

        <div class="tabela">
            <div class="thead" aria-hidden="true">
                <div>Fabricante</div>
                <div>País</div>
                <div>Itens</div>
                <div>Status</div>
                <div></div>
            </div>

            <div v-if="carregando" class="tabela-loading"><i class="fa-solid fa-spinner fa-spin"></i> Carregando…</div>

            <div v-else-if="itens.length === 0" class="tabela-vazio">
                <AppEmptyState
                    icone="fa-solid fa-industry"
                    titulo="Nenhum fabricante encontrado"
                    descricao="Cadastre os fabricantes para vinculá-los aos seus itens."
                    :compacto="true"
                >
                    <template #acao>
                        <AppButton icon="fa-solid fa-plus" @click="abrirCriar">Novo fabricante</AppButton>
                    </template>
                </AppEmptyState>
            </div>

            <div v-else>
                <div v-for="f in itens" :key="f.id" class="row" :class="{ inativo: !f.ativo }">
                    <div><b>{{ f.nome }}</b></div>
                    <div class="muted">{{ f.pais || "—" }}</div>
                    <div class="muted">{{ f.quantidadeItens }} {{ f.quantidadeItens === 1 ? "item" : "itens" }}</div>
                    <div>
                        <AppStatusPill :label="f.ativo ? 'Ativo' : 'Inativo'" :variante="f.ativo ? 'success' : 'muted'" />
                    </div>
                    <div class="acoes">
                        <button type="button" class="btn-icon btn-icon-editar" title="Editar" @click="abrirEditar(f)">
                            <i class="fa-solid fa-pen"></i>
                        </button>
                        <button v-if="f.ativo" type="button" class="btn-icon btn-icon-excluir" title="Inativar" @click="inativar(f)">
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
            rotulo-itens="fabricantes"
            class="paginacao"
            @update:pagina="(p: number) => (pagina = p)"
            @update:tamanho="(t: number) => (tamanho = t)"
        />

        <AppDrawer
            :aberto="drawerAberto"
            :titulo="editando ? `Editar — ${editando.nome}` : 'Novo fabricante'"
            :largura="500"
            @fechar="drawerAberto = false"
        >
            <div class="form-drawer">
                <AppField label="Nome" required>
                    <AppInput v-model="form.nome" placeholder="Ex: Pfizer" />
                </AppField>
                <AppField label="País">
                    <AppInput v-model="form.pais" placeholder="Ex: Brasil" />
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
    grid-template-columns: 2fr 1fr 0.8fr 110px auto;
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
.muted { color: hsl(var(--secondary) / 0.65); font-size: 13px; }
.acoes { display: flex; gap: 4px; }
.tabela-loading, .tabela-vazio { padding: 32px; text-align: center; color: hsl(var(--secondary) / 0.6); }
.paginacao { margin-top: 4px; }
.form-drawer { display: flex; flex-direction: column; gap: 16px; padding: 20px; }
.acoes-form { display: flex; gap: 8px; justify-content: flex-end; margin-top: 8px; }
</style>
