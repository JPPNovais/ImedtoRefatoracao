<script setup lang="ts">
import { ref, onMounted } from "vue"
import { useRouter } from "vue-router"
import { useModelosGlobaisStore } from "../stores/modelosGlobaisStore"
import type { ModeloGlobalListaItemDto } from "../services/catalogosService"

const router = useRouter()
const store = useModelosGlobaisStore()

const filtroInativos = ref(false)
const filtroBusca = ref("")

const modalAcao = ref(false)
const acaoTipo = ref<"desativar" | "reativar">("desativar")
const acaoItem = ref<ModeloGlobalListaItemDto | null>(null)
const motivoTexto = ref("")
const erroMotivo = ref("")
const salvando = ref(false)

onMounted(() => carregar())

async function carregar() {
    await store.carregar({
        incluirInativos: filtroInativos.value,
        busca: filtroBusca.value || undefined,
        page: store.pagina,
        size: store.tamanho,
    })
}

function irParaForm(id?: string) {
    if (id) {
        router.push({ name: "AdminModelosGlobaisEditar", params: { id } })
    } else {
        router.push({ name: "AdminModelosGlobaisNovo" })
    }
}

function abrirAcao(tipo: "desativar" | "reativar", item: ModeloGlobalListaItemDto) {
    acaoTipo.value = tipo
    acaoItem.value = item
    motivoTexto.value = ""
    erroMotivo.value = ""
    modalAcao.value = true
}

function fecharModal() {
    modalAcao.value = false
    acaoItem.value = null
}

async function confirmarAcao() {
    if (!acaoItem.value) return
    if (motivoTexto.value.trim().length < 10) {
        erroMotivo.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }
    salvando.value = true
    erroMotivo.value = ""
    try {
        if (acaoTipo.value === "desativar") {
            await store.desativar(acaoItem.value.id, motivoTexto.value.trim())
        } else {
            await store.reativar(acaoItem.value.id, motivoTexto.value.trim())
        }
        fecharModal()
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroMotivo.value = msg ?? "Não foi possível realizar a operação."
    } finally {
        salvando.value = false
    }
}

function mudarPagina(p: number) {
    store.pagina = p
    carregar()
}

function formatarData(iso: string | null): string {
    if (!iso) return "—"
    return new Date(iso).toLocaleDateString("pt-BR")
}
</script>

<template>
    <div class="catalog-page">
        <div class="page-header">
            <div>
                <h1 class="page-titulo">Modelos de prontuário globais</h1>
                <p class="page-subtitulo">Templates de estrutura de prontuário disponíveis para importação pelos estabelecimentos.</p>
            </div>
            <button class="btn-primario" @click="irParaForm()">+ Novo modelo</button>
        </div>

        <div class="filtros">
            <input
                v-model="filtroBusca"
                class="input-busca"
                placeholder="Buscar por nome..."
                @keyup.enter="carregar"
            />
            <label class="label-checkbox">
                <input type="checkbox" v-model="filtroInativos" @change="carregar" />
                Incluir inativos
            </label>
            <button class="btn-secundario" @click="carregar">Buscar</button>
        </div>

        <div v-if="store.carregando" class="estado-centro">Carregando...</div>
        <div v-else-if="store.erro" class="estado-erro" role="alert">{{ store.erro }}</div>
        <div v-else>
            <table class="tabela">
                <thead>
                    <tr>
                        <th>Nome</th>
                        <th>Descrição</th>
                        <th>Status</th>
                        <th>Atualizado em</th>
                        <th>Ações</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="item in store.lista" :key="item.id">
                        <td class="col-nome">{{ item.nome }}</td>
                        <td class="col-desc">{{ item.descricao ?? "—" }}</td>
                        <td>
                            <span :class="item.ativo ? 'badge-ativo' : 'badge-inativo'">
                                {{ item.ativo ? "Ativo" : "Inativo" }}
                            </span>
                        </td>
                        <td>{{ formatarData(item.atualizadoEm) }}</td>
                        <td class="col-acoes">
                            <button class="btn-icon btn-icon-editar" title="Editar" @click="irParaForm(item.id)">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button
                                v-if="item.ativo"
                                class="btn-icon btn-icon-excluir"
                                title="Desativar"
                                @click="abrirAcao('desativar', item)"
                            >
                                <i class="fa-solid fa-ban"></i>
                            </button>
                            <button
                                v-else
                                class="btn-icon btn-icon-ver"
                                title="Reativar"
                                @click="abrirAcao('reativar', item)"
                            >
                                <i class="fa-solid fa-rotate-left"></i>
                            </button>
                        </td>
                    </tr>
                    <tr v-if="store.lista.length === 0">
                        <td colspan="5" class="estado-centro">Nenhum modelo encontrado.</td>
                    </tr>
                </tbody>
            </table>

            <!-- Paginação simples -->
            <div v-if="store.total > store.tamanho" class="paginacao">
                <button
                    class="btn-secundario"
                    :disabled="store.pagina <= 1"
                    @click="mudarPagina(store.pagina - 1)"
                >Anterior</button>
                <span class="paginacao-info">
                    Página {{ store.pagina }} de {{ Math.ceil(store.total / store.tamanho) }}
                </span>
                <button
                    class="btn-secundario"
                    :disabled="store.pagina >= Math.ceil(store.total / store.tamanho)"
                    @click="mudarPagina(store.pagina + 1)"
                >Próxima</button>
            </div>
        </div>

        <!-- Modal motivo -->
        <div v-if="modalAcao" class="modal-overlay" @click.self="fecharModal">
            <div class="modal">
                <h2 class="modal-titulo">
                    {{ acaoTipo === "desativar" ? "Desativar modelo" : "Reativar modelo" }}
                </h2>
                <p class="modal-desc">{{ acaoItem?.nome }}</p>
                <label class="campo-label">Motivo <span class="obrigatorio">*</span></label>
                <textarea
                    v-model="motivoTexto"
                    class="campo-textarea"
                    rows="3"
                    placeholder="Descreva o motivo (mín. 10 caracteres)"
                    :class="{ 'campo-erro': erroMotivo }"
                />
                <p v-if="erroMotivo" class="erro-msg">{{ erroMotivo }}</p>
                <div class="modal-acoes">
                    <button class="btn-secundario" @click="fecharModal">Cancelar</button>
                    <button
                        class="btn-primario"
                        :disabled="salvando || motivoTexto.trim().length < 10"
                        @click="confirmarAcao"
                    >{{ salvando ? "Salvando..." : "Confirmar" }}</button>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.catalog-page { padding: 24px 32px; }

.page-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    margin-bottom: 24px;
    gap: 16px;
}
.page-titulo { font-size: 22px; font-weight: 700; margin: 0 0 4px; color: hsl(var(--foreground)); }
.page-subtitulo { font-size: 13px; color: hsl(var(--muted-foreground)); margin: 0; }

.filtros { display: flex; gap: 10px; margin-bottom: 20px; flex-wrap: wrap; align-items: center; }
.input-busca {
    flex: 1; min-width: 200px;
    padding: 7px 10px; border: 1px solid hsl(var(--border)); border-radius: 6px;
    font-size: 13px; background: hsl(var(--background)); color: hsl(var(--foreground));
}
.label-checkbox { display: flex; align-items: center; gap: 6px; font-size: 13px; color: hsl(var(--foreground)); cursor: pointer; }

.tabela { width: 100%; border-collapse: collapse; font-size: 13px; }
.tabela th, .tabela td { text-align: left; padding: 10px 12px; border-bottom: 1px solid hsl(var(--border)); }
.tabela th { font-weight: 600; font-size: 12px; color: hsl(var(--muted-foreground)); text-transform: uppercase; letter-spacing: 0.04em; background: hsl(var(--muted) / 0.3); }
.tabela tbody tr:hover { background: hsl(var(--muted) / 0.2); }

.col-nome { font-weight: 600; color: hsl(var(--foreground)); max-width: 220px; }
.col-desc { color: hsl(var(--muted-foreground)); max-width: 280px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.col-acoes { display: flex; gap: 6px; }

.badge-ativo {
    display: inline-block; padding: 2px 8px; border-radius: 9999px; font-size: 11px; font-weight: 600;
    background: hsl(var(--success) / 0.15); color: hsl(var(--success));
}
.badge-inativo {
    display: inline-block; padding: 2px 8px; border-radius: 9999px; font-size: 11px; font-weight: 600;
    background: hsl(var(--destructive) / 0.12); color: hsl(var(--destructive));
}

.estado-centro { text-align: center; padding: 48px; color: hsl(var(--muted-foreground)); font-size: 14px; }
.estado-erro { text-align: center; padding: 48px; color: hsl(var(--destructive)); font-size: 14px; }

.paginacao { display: flex; align-items: center; gap: 12px; padding: 16px 0; justify-content: flex-end; }
.paginacao-info { font-size: 13px; color: hsl(var(--muted-foreground)); }

.btn-primario {
    padding: 8px 18px; background: hsl(var(--primary)); color: hsl(var(--primary-foreground));
    border: none; border-radius: 6px; font-size: 13px; font-weight: 600; cursor: pointer; white-space: nowrap;
}
.btn-primario:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-secundario {
    padding: 7px 14px; background: hsl(var(--muted)); color: hsl(var(--foreground));
    border: none; border-radius: 6px; font-size: 13px; cursor: pointer;
}
.btn-secundario:disabled { opacity: 0.5; cursor: not-allowed; }

.btn-icon {
    background: none; border: none; cursor: pointer; padding: 5px 7px;
    border-radius: 5px; font-size: 13px; color: hsl(var(--muted-foreground));
}
.btn-icon:hover { background: hsl(var(--muted)); }
.btn-icon-ver { color: hsl(var(--primary)); }
.btn-icon-editar { color: hsl(220 80% 55%); }
.btn-icon-excluir { color: hsl(var(--destructive)); }

.modal-overlay {
    position: fixed; inset: 0; background: hsl(var(--foreground) / 0.4);
    display: flex; align-items: center; justify-content: center; z-index: 1000;
}
.modal {
    background: hsl(var(--card)); border: 1px solid hsl(var(--border)); border-radius: 10px;
    padding: 24px; width: 100%; max-width: 440px;
}
.modal-titulo { font-size: 16px; font-weight: 700; margin: 0 0 4px; color: hsl(var(--foreground)); }
.modal-desc { font-size: 13px; color: hsl(var(--muted-foreground)); margin: 0 0 16px; }
.campo-label { display: block; font-size: 12px; font-weight: 600; color: hsl(var(--foreground)); margin-bottom: 6px; }
.obrigatorio { color: hsl(var(--destructive)); }
.campo-textarea {
    width: 100%; padding: 8px 10px; border: 1px solid hsl(var(--border)); border-radius: 6px;
    font-size: 13px; background: hsl(var(--background)); color: hsl(var(--foreground));
    resize: vertical; box-sizing: border-box;
}
.campo-textarea.campo-erro { border-color: hsl(var(--destructive)); }
.erro-msg { font-size: 12px; color: hsl(var(--destructive)); margin: 4px 0 0; }
.modal-acoes { display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px; }
</style>
