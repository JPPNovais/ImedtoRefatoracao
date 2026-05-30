<script setup lang="ts">
/**
 * ConfigsView — gerenciamento de configurações globais do sistema.
 * W2-CA6 a W2-CA13.
 *
 * Cada chave tem input tipado; ao Salvar, pede motivo ≥10 chars antes de enviar.
 */
import { ref, onMounted } from "vue"
import { useConfigsStore } from "../stores/configsStore"
import type { ConfigAdminDto } from "../services/configsService"

const store = useConfigsStore()

const secoesAbertas = ref<Record<string, boolean>>({})
const editando = ref<string | null>(null)
const valorEditando = ref("")
const motivoModal = ref("")
const erroModal = ref("")
const salvando = ref(false)

onMounted(() => store.carregar())

function toggleSecao(secao: string) {
    secoesAbertas.value[secao] = !secoesAbertas.value[secao]
}

function isSecaoAberta(secao: string) {
    return secoesAbertas.value[secao] !== false // default aberta
}

function abrirEdicao(config: ConfigAdminDto) {
    editando.value = config.chave
    valorEditando.value = config.valor.replace(/^"|"$/g, "") // remove aspas do JSON string
    motivoModal.value = ""
    erroModal.value = ""
}

function fecharEdicao() {
    editando.value = null
}

async function salvar(config: ConfigAdminDto) {
    if (motivoModal.value.trim().length < 10) {
        erroModal.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }
    salvando.value = true
    erroModal.value = ""
    try {
        await store.atualizar(config.chave, String(valorEditando.value), motivoModal.value.trim())
        editando.value = null
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroModal.value = msg ?? "Erro ao salvar configuração."
    } finally {
        salvando.value = false
    }
}

function labelTipo(tipo: string): string {
    return { numerico: "Número inteiro", texto: "Texto", email: "E-mail", toggle: "Ativado/Desativado" }[tipo] ?? tipo
}

function displayValor(config: ConfigAdminDto): string {
    const v = config.valor.replace(/^"|"$/g, "")
    if (config.tipo === "toggle") return v === "true" ? "Sim" : "Não"
    return v
}
</script>

<template>
    <div class="admin-page">
        <div class="page-header">
            <h1 class="page-titulo">Configurações do sistema</h1>
            <p class="page-subtitulo">Parâmetros globais que afetam novos estabelecimentos e comportamentos do sistema.</p>
        </div>

        <div v-if="store.carregando" class="estado-vazio">Carregando...</div>

        <div v-else-if="store.erro" class="estado-erro" role="alert">{{ store.erro }}</div>

        <div v-else class="secoes-lista">
            <div
                v-for="secao in store.secoes"
                :key="secao.secao"
                class="secao-card"
            >
                <button
                    class="secao-header"
                    type="button"
                    @click="toggleSecao(secao.secao)"
                >
                    <span class="secao-nome">{{ secao.secao }}</span>
                    <i :class="['fa-solid', isSecaoAberta(secao.secao) ? 'fa-chevron-up' : 'fa-chevron-down']"></i>
                </button>

                <div v-if="isSecaoAberta(secao.secao)" class="secao-conteudo">
                    <div
                        v-for="config in secao.configs"
                        :key="config.chave"
                        class="config-linha"
                    >
                        <div class="config-info">
                            <span class="config-chave">{{ config.chave }}</span>
                            <span class="config-tipo">{{ labelTipo(config.tipo) }}</span>
                            <span v-if="config.descricao" class="config-descricao">{{ config.descricao }}</span>
                        </div>

                        <div class="config-valor-area">
                            <template v-if="editando === config.chave">
                                <!-- Input tipado -->
                                <template v-if="config.tipo === 'toggle'">
                                    <select v-model="valorEditando" class="admin-select">
                                        <option value="true">Sim</option>
                                        <option value="false">Não</option>
                                    </select>
                                </template>
                                <template v-else-if="config.tipo === 'numerico'">
                                    <input type="number" v-model="valorEditando" class="admin-input" min="0" />
                                </template>
                                <template v-else-if="config.tipo === 'email'">
                                    <input type="email" v-model="valorEditando" class="admin-input" />
                                </template>
                                <template v-else>
                                    <input type="text" v-model="valorEditando" class="admin-input" />
                                </template>

                                <!-- Motivo inline -->
                                <div class="motivo-wrap">
                                    <input
                                        v-model="motivoModal"
                                        type="text"
                                        placeholder="Motivo da alteração (mín. 10 caracteres)"
                                        class="admin-input"
                                        :class="{ 'input-erro': erroModal }"
                                    />
                                    <span v-if="erroModal" class="erro-inline">{{ erroModal }}</span>
                                </div>

                                <div class="config-acoes">
                                    <button
                                        class="admin-btn-primario"
                                        :disabled="salvando || motivoModal.trim().length < 10"
                                        @click="salvar(config)"
                                    >
                                        {{ salvando ? "Salvando..." : "Salvar" }}
                                    </button>
                                    <button class="admin-btn-texto" @click="fecharEdicao">Cancelar</button>
                                </div>
                            </template>

                            <template v-else>
                                <span class="config-valor-display">{{ displayValor(config) }}</span>
                                <button class="admin-btn-secondary" @click="abrirEdicao(config)">Editar</button>
                            </template>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.admin-page { padding: 24px 32px; }
.page-header { margin-bottom: 28px; }
.page-titulo { font-size: 22px; font-weight: 700; margin: 0 0 6px; }
.page-subtitulo { font-size: 14px; color: hsl(var(--muted-foreground)); margin: 0; }

.estado-vazio, .estado-erro {
    padding: 48px; text-align: center; color: hsl(var(--muted-foreground)); font-size: 14px;
}
.estado-erro { color: hsl(var(--destructive)); }

.secoes-lista { display: flex; flex-direction: column; gap: 16px; }

.secao-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 8px;
    overflow: hidden;
}

.secao-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    padding: 14px 20px;
    background: none;
    border: none;
    cursor: pointer;
    font-size: 15px;
    font-weight: 600;
    color: hsl(var(--foreground));
    text-align: left;
}

.secao-header:hover { background: hsl(var(--muted) / 0.4); }

.secao-nome { font-size: 15px; font-weight: 600; }

.secao-conteudo {
    border-top: 1px solid hsl(var(--border));
    padding: 0;
}

.config-linha {
    display: flex;
    align-items: flex-start;
    gap: 16px;
    padding: 14px 20px;
    border-bottom: 1px solid hsl(var(--border) / 0.5);
    flex-wrap: wrap;
}
.config-linha:last-child { border-bottom: none; }

.config-info { flex: 1; min-width: 200px; }
.config-chave { display: block; font-weight: 600; font-size: 13px; color: hsl(var(--foreground)); font-family: monospace; }
.config-tipo {
    display: inline-block;
    font-size: 11px;
    background: hsl(var(--muted));
    color: hsl(var(--muted-foreground));
    padding: 2px 6px;
    border-radius: 4px;
    margin-top: 4px;
}
.config-descricao { display: block; font-size: 12px; color: hsl(var(--muted-foreground)); margin-top: 4px; }

.config-valor-area {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    flex-wrap: wrap;
    flex: 2;
}
.config-valor-display { font-size: 14px; font-weight: 600; color: hsl(var(--foreground)); padding: 6px 0; }

.motivo-wrap { display: flex; flex-direction: column; gap: 4px; flex: 1; min-width: 200px; }
.input-erro { border-color: hsl(var(--destructive)) !important; }
.erro-inline { font-size: 12px; color: hsl(var(--destructive)); }

.config-acoes { display: flex; align-items: center; gap: 8px; }

.admin-input {
    padding: 7px 10px;
    border: 1px solid hsl(var(--border));
    border-radius: 6px;
    font-size: 13px;
    background: hsl(var(--background));
    color: hsl(var(--foreground));
    width: 100%;
}
.admin-select {
    padding: 7px 10px;
    border: 1px solid hsl(var(--border));
    border-radius: 6px;
    font-size: 13px;
    background: hsl(var(--background));
    color: hsl(var(--foreground));
}

.admin-btn-primario {
    padding: 7px 16px;
    background: hsl(var(--primary));
    color: hsl(var(--primary-foreground));
    border: none;
    border-radius: 6px;
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    white-space: nowrap;
}
.admin-btn-primario:disabled { opacity: 0.5; cursor: not-allowed; }

.admin-btn-secondary {
    padding: 6px 14px;
    background: hsl(var(--muted));
    color: hsl(var(--foreground));
    border: none;
    border-radius: 6px;
    font-size: 13px;
    cursor: pointer;
    white-space: nowrap;
}

.admin-btn-texto {
    background: none;
    border: none;
    color: hsl(var(--muted-foreground));
    font-size: 13px;
    cursor: pointer;
    padding: 0;
}
</style>
