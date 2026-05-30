<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue"
import { useRouter } from "vue-router"
import { prontuarioService, type ModeloProntuario } from "@/services/prontuarioService"
import { useTenantStore } from "@/stores/tenantStore"
import {
    AppBadge, AppButton, AppEmptyState, AppPageHeader, ModeloProntuarioBuilder,
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

const modelosPadroes = computed(() => modelos.value.filter(m => m.ehPadraoSistema))
const modelosPersonalizados = computed(() => modelos.value.filter(m => !m.ehPadraoSistema))

function resetarForm() {
    form.id = null
    form.nome = ""
    form.descricao = ""
    form.estruturaJson = ""
}

function preencherForm(modelo: ModeloProntuario) {
    form.id = modelo.id
    form.nome = modelo.nome
    form.descricao = modelo.descricao ?? ""
    const estrutura = Array.isArray(modelo.estrutura) ? modelo.estrutura : []
    form.estruturaJson = JSON.stringify(estrutura)
}

function editarModelo(modelo: ModeloProntuario) {
    if (modelo.ehPadraoSistema) return
    preencherForm(modelo)
    window.scrollTo({ top: 0, behavior: "smooth" })
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
        resetarForm()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Erro ao salvar modelo.")
    } finally {
        salvando.value = false
    }
}

async function excluir(modelo: ModeloProntuario) {
    if (modelo.ehPadraoSistema) return
    if (!confirm(`Deseja excluir o modelo "${modelo.nome}"?`)) return

    excluindoId.value = modelo.id
    try {
        await prontuarioService.excluirModelo(modelo.id)
        if (form.id === modelo.id) resetarForm()
        await carregarModelos()
    } catch (e: any) {
        alert(e?.response?.data?.mensagem ?? "Não foi possível excluir o modelo.")
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
            subtitulo="Crie e organize os modelos de prontuário utilizados nos atendimentos."
        />

        <div v-if="carregando" class="estado-loading">Carregando...</div>

        <div v-else class="layout-dois-paineis">
            <!-- ── Painel esquerdo: lista de modelos ── -->
            <aside class="painel painel-lista">
                <div class="painel-cabecalho">
                    <span class="painel-titulo">Modelos cadastrados</span>
                    <AppButton variant="ghost" size="sm" @click="resetarForm">+ Novo modelo</AppButton>
                </div>

                <div v-if="carregandoModelos" class="estado-loading-sm">Carregando modelos...</div>

                <div v-else-if="!modelos.length">
                    <AppEmptyState
                        icone="📋"
                        titulo="Nenhum modelo cadastrado"
                        descricao="Clique em &quot;Novo modelo&quot; para criar o primeiro."
                    />
                </div>

                <div v-else class="lista-modelos">
                    <!-- Modelos padrão do sistema -->
                    <div v-if="modelosPadroes.length">
                        <div class="grupo-titulo">
                            <span>Padrões do sistema</span>
                            <div class="grupo-linha" />
                        </div>
                        <ul class="lista-itens">
                            <li v-for="m in modelosPadroes" :key="m.id" class="item-modelo item-padrao">
                                <div class="item-info">
                                    <div class="item-nome-row">
                                        <span class="item-nome">{{ m.nome }}</span>
                                        <AppBadge variant="info" label="Padrão do sistema" />
                                    </div>
                                    <p v-if="m.descricao" class="item-desc">{{ m.descricao }}</p>
                                </div>
                            </li>
                        </ul>
                    </div>

                    <!-- Modelos personalizados -->
                    <div v-if="modelosPersonalizados.length">
                        <div class="grupo-titulo">
                            <span>Personalizados</span>
                            <div class="grupo-linha" />
                        </div>
                        <ul class="lista-itens">
                            <li
                                v-for="m in modelosPersonalizados"
                                :key="m.id"
                                class="item-modelo"
                                :class="{ 'item-selecionado': form.id === m.id }"
                            >
                                <div class="item-info">
                                    <span class="item-nome">{{ m.nome }}</span>
                                    <p v-if="m.descricao" class="item-desc">{{ m.descricao }}</p>
                                </div>
                                <div class="item-acoes">
                                    <button class="btn-icon btn-icon-editar" title="Editar" @click="editarModelo(m)">
                                        <i class="fa-solid fa-pen" />
                                    </button>
                                    <button
                                        class="btn-icon btn-icon-excluir"
                                        title="Excluir"
                                        :disabled="excluindoId === m.id"
                                        @click="excluir(m)"
                                    >
                                        <i class="fa-solid fa-trash" />
                                    </button>
                                </div>
                            </li>
                        </ul>
                    </div>
                </div>
            </aside>

            <!-- ── Painel direito: formulário ── -->
            <section class="painel painel-form">
                <h2 class="painel-titulo painel-titulo-form">
                    {{ form.id ? "Editar modelo" : "Novo modelo" }}
                </h2>

                <form class="form-corpo" @submit.prevent="salvar">
                    <ModeloProntuarioBuilder
                        v-model:nome="form.nome"
                        v-model:descricao="form.descricao"
                        v-model:estrutura-json="form.estruturaJson"
                        :disabled="salvando"
                        @update:valido="builderValido = $event"
                    />

                    <div class="form-rodape">
                        <AppButton variant="ghost" type="button" @click="resetarForm">Cancelar</AppButton>
                        <AppButton
                            type="submit"
                            :disabled="salvando || !builderValido"
                            :loading="salvando"
                        >
                            {{ salvando ? "Salvando..." : "Salvar modelo" }}
                        </AppButton>
                    </div>
                </form>
            </section>
        </div>
    </div>
</template>

<style scoped>
.estado-loading {
    text-align: center;
    color: var(--text-muted);
    padding: 3rem 1rem;
    font-size: 0.9em;
}

.layout-dois-paineis {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.5rem;
    align-items: start;
}

@media (max-width: 860px) {
    .layout-dois-paineis { grid-template-columns: 1fr; }
}

/* ── Painéis ── */
.painel {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.painel-cabecalho {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
}

.painel-titulo {
    font-size: 0.88em;
    font-weight: 700;
    color: var(--text);
}

.painel-titulo-form {
    margin: 0 0 0.25rem;
}

.estado-loading-sm {
    font-size: 0.82em;
    color: var(--text-muted);
    text-align: center;
    padding: 1rem 0;
}

/* ── Grupos de modelos ── */
.grupo-titulo {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.72em;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-muted);
    margin-bottom: 0.5rem;
}

.grupo-linha {
    flex: 1;
    height: 1px;
    background: var(--border);
}

.lista-modelos { display: flex; flex-direction: column; gap: 1rem; }

.lista-itens { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.4rem; }

.item-modelo {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 0.75rem;
    padding: 0.6rem 0.75rem;
    border-radius: calc(var(--radius) - 2px);
    border: 1px solid var(--border);
    background: var(--bg);
    transition: border-color 0.12s;
}

.item-modelo:hover { border-color: var(--border-strong); }

.item-padrao { background: color-mix(in srgb, var(--info) 5%, transparent); border-color: color-mix(in srgb, var(--info) 20%, transparent); }

.item-selecionado { border-color: var(--primary); background: color-mix(in srgb, var(--primary) 5%, transparent); }

.item-info { flex: 1; display: flex; flex-direction: column; gap: 0.2rem; min-width: 0; }

.item-nome-row { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }

.item-nome { font-size: 0.85em; font-weight: 600; color: var(--text); }

.item-desc { font-size: 0.78em; color: var(--text-muted); margin: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.item-acoes { display: flex; align-items: center; gap: 0.25rem; flex-shrink: 0; }

/* ── Formulário ── */
.form-corpo { display: flex; flex-direction: column; gap: 1rem; }

/* ── Rodapé ── */
.form-rodape {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
    padding-top: 0.5rem;
}
</style>
