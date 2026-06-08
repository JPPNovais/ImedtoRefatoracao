<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue"
import { useRouter } from "vue-router"
import { prontuarioService, type ModeloProntuario } from "@/services/prontuarioService"
import { useTenantStore } from "@/stores/tenantStore"
import {
    AppBadge, AppButton, AppDrawer, AppEmptyState, AppPageHeader, ModeloProntuarioBuilder,
    AppToast, AppConfirmDialog,
} from "@/components/ui"

const router = useRouter()
const tenant = useTenantStore()

if (tenant.papel !== "Dono") {
    router.replace({ name: "Home" })
}

type FormState = {
    id: number | null
    nome: string
    descricao: string
    estruturaJson: string
}

const form = reactive<FormState>({
    id: null,
    nome: "",
    descricao: "",
    estruturaJson: "",
})

const builderValido = ref(false)
const modelos = ref<ModeloProntuario[]>([])
const carregando = ref(false)
const carregandoModelos = ref(false)
const salvando = ref(false)
const excluindoId = ref<number | null>(null)
const drawerAberto = ref(false)

const toast = ref<{ mensagem: string, variante: "info" | "success" | "error" } | null>(null)
function notificar(mensagem: string, variante: "info" | "success" | "error" = "success") {
    toast.value = { mensagem, variante }
}
const confirmacaoExcluir = ref<{ aberto: boolean, alvo: ModeloProntuario | null, executando: boolean }>({
    aberto: false, alvo: null, executando: false,
})

const modelosPadroes = computed(() => modelos.value.filter(m => m.ehPadraoSistema))
const modelosPersonalizados = computed(() => modelos.value.filter(m => !m.ehPadraoSistema))

function contarSecoes(m: ModeloProntuario) {
    const n = Array.isArray(m.estrutura) ? m.estrutura.length : 0
    return `${n} ${n === 1 ? "seção" : "seções"}`
}

function resetarForm() {
    form.id = null
    form.nome = ""
    form.descricao = ""
    form.estruturaJson = ""
}

function abrirNovo() {
    resetarForm()
    drawerAberto.value = true
}

function editarModelo(modelo: ModeloProntuario) {
    if (modelo.ehPadraoSistema) return
    form.id = modelo.id
    form.nome = modelo.nome
    form.descricao = modelo.descricao ?? ""
    const estrutura = Array.isArray(modelo.estrutura) ? modelo.estrutura : []
    form.estruturaJson = JSON.stringify(estrutura)
    drawerAberto.value = true
}

function fecharDrawer() {
    drawerAberto.value = false
    resetarForm()
}

async function carregarModelos() {
    carregandoModelos.value = true
    try {
        modelos.value = await prontuarioService.listarModelos()
    } catch {
        // silencioso — lista vazia
    } finally {
        carregandoModelos.value = false
    }
}

async function salvar() {
    if (!builderValido.value) return

    const payload = {
        nome: form.nome.trim(),
        descricao: form.descricao.trim() || undefined,
        estruturaJson: form.estruturaJson,
    }

    salvando.value = true
    try {
        if (form.id) {
            await prontuarioService.atualizarModelo(form.id, payload)
        } else {
            await prontuarioService.criarModelo(payload)
        }
        await carregarModelos()
        fecharDrawer()
        notificar("Modelo salvo com sucesso.")
    } catch (e: any) {
        notificar(e?.response?.data?.mensagem ?? "Erro ao salvar modelo.", "error")
    } finally {
        salvando.value = false
    }
}

function excluir(modelo: ModeloProntuario) {
    if (modelo.ehPadraoSistema) return
    confirmacaoExcluir.value = { aberto: true, alvo: modelo, executando: false }
}

async function executarExcluir() {
    const alvo = confirmacaoExcluir.value.alvo
    if (!alvo) return
    confirmacaoExcluir.value.executando = true
    excluindoId.value = alvo.id
    try {
        await prontuarioService.excluirModelo(alvo.id)
        confirmacaoExcluir.value = { aberto: false, alvo: null, executando: false }
        if (form.id === alvo.id) fecharDrawer()
        await carregarModelos()
    } catch (e: any) {
        confirmacaoExcluir.value.executando = false
        notificar(e?.response?.data?.mensagem ?? "Não foi possível excluir o modelo.", "error")
    } finally {
        excluindoId.value = null
    }
}

onMounted(async () => {
    carregando.value = true
    try {
        await carregarModelos()
    } finally {
        carregando.value = false
    }
})
</script>

<template>
    <div class="app-page">
        <AppPageHeader
            titulo="Modelos de prontuário"
            subtitulo="Configure os modelos de prontuário utilizados nos atendimentos do estabelecimento. Eles definem os campos e seções que o profissional preenche durante a consulta."
        >
            <template #acoes>
                <AppButton icon="fa-solid fa-plus" @click="abrirNovo">Novo modelo</AppButton>
            </template>
        </AppPageHeader>

        <div v-if="carregando || carregandoModelos" class="estado-loading">Carregando modelos...</div>

        <AppEmptyState
            v-else-if="!modelos.length"
            icone="📋"
            titulo="Nenhum modelo cadastrado"
            descricao="Crie o primeiro modelo de prontuário para começar."
        />

        <div v-else class="lista-grupos">
            <!-- ── Padrões do sistema ── -->
            <section v-if="modelosPadroes.length" class="grupo">
                <div class="grupo-label">Padrões do sistema</div>
                <div class="lista">
                    <article v-for="m in modelosPadroes" :key="m.id" class="linha">
                        <div class="linha-ico"><i class="fa-solid fa-clipboard-list" /></div>
                        <div class="linha-corpo">
                            <div class="linha-titulo">
                                <span class="linha-nome">{{ m.nome }}</span>
                                <AppBadge variant="muted" label="Padrão do sistema" />
                            </div>
                            <p v-if="m.descricao" class="linha-desc">{{ m.descricao }}</p>
                            <div class="linha-meta">
                                <i class="fa-regular fa-layer-group" />{{ contarSecoes(m) }}
                            </div>
                        </div>
                    </article>
                </div>
            </section>

            <!-- ── Personalizados ── -->
            <section v-if="modelosPersonalizados.length" class="grupo">
                <div class="grupo-label">Personalizados</div>
                <div class="lista">
                    <article v-for="m in modelosPersonalizados" :key="m.id" class="linha">
                        <div class="linha-ico"><i class="fa-solid fa-clipboard-list" /></div>
                        <div class="linha-corpo">
                            <div class="linha-titulo">
                                <span class="linha-nome">{{ m.nome }}</span>
                            </div>
                            <p v-if="m.descricao" class="linha-desc">{{ m.descricao }}</p>
                            <div class="linha-meta">
                                <i class="fa-regular fa-layer-group" />{{ contarSecoes(m) }}
                            </div>
                        </div>
                        <div class="linha-acoes">
                            <button class="ico-btn" title="Editar" @click="editarModelo(m)">
                                <i class="fa-regular fa-pen-to-square" />
                            </button>
                            <button
                                class="ico-btn danger"
                                title="Excluir"
                                :disabled="excluindoId === m.id"
                                @click="excluir(m)"
                            >
                                <i class="fa-regular fa-trash-can" />
                            </button>
                        </div>
                    </article>
                </div>
            </section>
        </div>

        <!-- ── Drawer: novo / editar modelo ── -->
        <AppDrawer :aberto="drawerAberto" :largura="500" @fechar="fecharDrawer">
            <template #titulo>
                <div class="drawer-titulo">
                    <h2>{{ form.id ? "Editar modelo" : "Novo modelo" }}</h2>
                    <p>Defina o nome e organize as seções que o profissional preencherá durante o atendimento.</p>
                </div>
            </template>

            <ModeloProntuarioBuilder
                v-model:nome="form.nome"
                v-model:descricao="form.descricao"
                v-model:estrutura-json="form.estruturaJson"
                :disabled="salvando"
                @update:valido="builderValido = $event"
            />

            <template #rodape>
                <AppButton variant="ghost" type="button" @click="fecharDrawer">Cancelar</AppButton>
                <AppButton
                    :disabled="salvando || !builderValido"
                    :loading="salvando"
                    @click="salvar"
                >
                    {{ salvando ? "Salvando..." : "Salvar modelo" }}
                </AppButton>
            </template>
        </AppDrawer>

        <AppConfirmDialog
            v-model:aberto="confirmacaoExcluir.aberto"
            titulo="Excluir modelo?"
            :mensagem="confirmacaoExcluir.alvo ? `Deseja excluir o modelo ${confirmacaoExcluir.alvo.nome}?` : ''"
            confirmar-rotulo="Excluir"
            variante="danger"
            :executando="confirmacaoExcluir.executando"
            @confirmar="executarExcluir"
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
.estado-loading {
    text-align: center;
    color: var(--text-muted);
    padding: 3rem 1rem;
    font-size: 0.9em;
}

/* ── Grupos ── */
.lista-grupos {
    display: flex;
    flex-direction: column;
    gap: 1.75rem;
}

.grupo-label {
    display: flex;
    align-items: center;
    gap: 0.65rem;
    font-size: 0.7em;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--text-faint);
    margin-bottom: 0.7rem;
}

.grupo-label::after {
    content: "";
    flex: 1;
    height: 1px;
    background: var(--border);
}

/* ── Linhas ── */
.lista {
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
}

.linha {
    display: flex;
    align-items: center;
    gap: 0.875rem;
    padding: 0.95rem 1rem;
    border: 1px solid var(--border);
    border-radius: var(--radius-lg);
    background: var(--bg-card);
    transition: border-color 0.12s, box-shadow 0.12s;
}

.linha:hover {
    border-color: hsl(var(--primary) / 0.35);
    box-shadow: 0 1px 2px hsl(var(--primary) / 0.06);
}

.linha-ico {
    width: 42px;
    height: 42px;
    flex: none;
    border-radius: var(--radius-lg);
    background: hsl(var(--primary) / 0.08);
    color: hsl(var(--primary));
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.05em;
}

.linha-corpo {
    min-width: 0;
    flex: 1;
}

.linha-titulo {
    display: flex;
    align-items: center;
    gap: 0.55rem;
    flex-wrap: wrap;
}

.linha-nome {
    font-size: 0.9em;
    font-weight: 700;
    color: var(--text);
}

.linha-desc {
    font-size: 0.8em;
    color: var(--text-muted);
    margin: 0.25rem 0 0;
    line-height: 1.4;
}

.linha-meta {
    font-size: 0.76em;
    color: var(--text-muted);
    margin-top: 0.3rem;
}

.linha-meta i {
    font-size: 0.85em;
    margin-right: 0.25rem;
}

.linha-acoes {
    display: flex;
    gap: 0.4rem;
    flex: none;
}

.ico-btn {
    width: 34px;
    height: 34px;
    border-radius: var(--radius-md);
    border: 1px solid var(--border);
    background: var(--bg-card);
    color: var(--text-muted);
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 0.8em;
    transition: all 0.12s;
}

.ico-btn:hover:not(:disabled) {
    background: hsl(var(--primary) / 0.06);
    color: hsl(var(--primary));
    border-color: hsl(var(--primary) / 0.3);
}

.ico-btn.danger:hover:not(:disabled) {
    background: hsl(var(--error) / 0.08);
    color: hsl(var(--error));
    border-color: hsl(var(--error) / 0.3);
}

.ico-btn:disabled {
    cursor: not-allowed;
    opacity: 0.5;
}

/* ── Drawer ── */
.drawer-titulo h2 {
    font-size: var(--text-lg);
    font-weight: 800;
    color: hsl(var(--primary-dark));
    margin: 0;
}

.drawer-titulo p {
    font-size: 0.8em;
    color: var(--text-muted);
    margin: 0.3rem 0 0;
}
</style>
