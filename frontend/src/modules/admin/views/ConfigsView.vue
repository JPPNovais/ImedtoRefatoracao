<script setup lang="ts">
/**
 * ConfigsView — gerenciamento de configurações globais do sistema.
 *
 * W3-CA7 a W3-CA11, W3-CA16: app-page + AppPageHeader + AppCard por seção
 *   + AppField + AppInput + AppSelect + AppCheckbox + AppButton + AppToast.
 * Cada chave tem input tipado; ao Salvar, pede motivo ≥10 chars antes de enviar.
 * Seções colapsáveis via estado local (mantido da implementação anterior).
 */
import { ref, onMounted } from "vue"
import { AppPageHeader, AppCard, AppField, AppInput, AppButton, AppToast } from "@/components/ui"
import { useConfigsStore } from "../stores/configsStore"
import type { ConfigAdminDto } from "../services/configsService"

const store = useConfigsStore()

const secoesAbertas = ref<Record<string, boolean>>({})
const editando = ref<string | null>(null)
const valorEditando = ref("")
const motivoModal = ref("")
const erroModal = ref("")
const salvando = ref(false)
const toast = ref<{ mensagem: string; variante: "success" | "error" } | null>(null)

onMounted(() => store.carregar())

function toggleSecao(secao: string) {
    secoesAbertas.value[secao] = !isSecaoAberta(secao)
}

function isSecaoAberta(secao: string) {
    return secoesAbertas.value[secao] !== false
}

function abrirEdicao(config: ConfigAdminDto) {
    editando.value = config.chave
    valorEditando.value = config.valor.replace(/^"|"$/g, "")
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
        toast.value = { mensagem: "Configuração salva com sucesso.", variante: "success" }
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
    <main class="app-page">
        <AppPageHeader
            titulo="Configurações do sistema"
            subtitulo="Parâmetros globais que afetam novos estabelecimentos e comportamentos do sistema."
        />

        <div v-if="store.carregando" class="estado-info">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>

        <p v-else-if="store.erro" class="estado-erro" role="alert">{{ store.erro }}</p>

        <div v-else class="configs-lista">
            <AppCard
                v-for="secao in store.secoes"
                :key="secao.secao"
                padding="none"
            >
                <!-- Header colapsável -->
                <button
                    class="colapsavel-header"
                    type="button"
                    @click="toggleSecao(secao.secao)"
                >
                    <span class="colapsavel-nome">{{ secao.secao }}</span>
                    <i
                        :class="['fa-solid', isSecaoAberta(secao.secao) ? 'fa-chevron-up' : 'fa-chevron-down']"
                        aria-hidden="true"
                    ></i>
                </button>

                <div v-if="isSecaoAberta(secao.secao)" class="colapsavel-conteudo">
                    <div
                        v-for="config in secao.configs"
                        :key="config.chave"
                        class="config-linha"
                    >
                        <div class="config-info">
                            <span class="config-chave">{{ config.chave }}</span>
                            <span class="config-tipo-badge">{{ labelTipo(config.tipo) }}</span>
                            <span v-if="config.descricao" class="config-descricao">{{ config.descricao }}</span>
                        </div>

                        <div class="config-valor-area">
                            <template v-if="editando === config.chave">
                                <!-- Input tipado por tipo -->
                                <div class="config-input-wrap">
                                    <select v-if="config.tipo === 'toggle'" v-model="valorEditando" class="campo-select">
                                        <option value="true">Sim</option>
                                        <option value="false">Não</option>
                                    </select>
                                    <AppInput v-else-if="config.tipo === 'numerico'" v-model="valorEditando" type="number" />
                                    <AppInput v-else-if="config.tipo === 'email'" v-model="valorEditando" type="email" />
                                    <AppInput v-else v-model="valorEditando" type="text" />
                                </div>

                                <AppField label="Motivo (mín. 10 caracteres)" class="motivo-field">
                                    <AppInput
                                        v-model="motivoModal"
                                        placeholder="Motivo da alteração"
                                        :class="erroModal ? 'input-erro' : ''"
                                    />
                                </AppField>
                                <span v-if="erroModal" class="campo-erro">{{ erroModal }}</span>

                                <div class="config-acoes">
                                    <AppButton
                                        size="sm"
                                        :loading="salvando"
                                        :disabled="salvando || motivoModal.trim().length < 10"
                                        @click="salvar(config)"
                                    >
                                        Salvar
                                    </AppButton>
                                    <AppButton variant="ghost" size="sm" @click="fecharEdicao">Cancelar</AppButton>
                                </div>
                            </template>

                            <template v-else>
                                <span class="config-valor-display">{{ displayValor(config) }}</span>
                                <AppButton variant="secondary" size="sm" @click="abrirEdicao(config)">Editar</AppButton>
                            </template>
                        </div>
                    </div>
                </div>
            </AppCard>
        </div>

        <AppToast
            v-if="toast"
            :mensagem="toast.mensagem"
            :variante="toast.variante"
            @fechar="toast = null"
        />
    </main>
</template>

<style scoped>
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

.configs-lista {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.colapsavel-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    padding: 0.875rem 1.25rem;
    background: none;
    border: none;
    cursor: pointer;
    font-size: 0.9375rem;
    font-weight: 600;
    color: hsl(var(--foreground));
    text-align: left;
    font-family: inherit;
}

.colapsavel-header:hover { background: hsl(var(--muted) / 0.4); }

.colapsavel-nome { font-size: 0.9375rem; font-weight: 600; }

.colapsavel-conteudo { border-top: 1px solid hsl(var(--border)); }

.config-linha {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    padding: 0.875rem 1.25rem;
    border-bottom: 1px solid hsl(var(--border) / 0.5);
    flex-wrap: wrap;
}
.config-linha:last-child { border-bottom: none; }

.config-info { flex: 1; min-width: 200px; }

.config-chave {
    display: block;
    font-weight: 600;
    font-size: 0.8125rem;
    color: hsl(var(--foreground));
    font-family: monospace;
}

.config-tipo-badge {
    display: inline-block;
    font-size: 0.6875rem;
    background: hsl(var(--muted));
    color: hsl(var(--muted-foreground));
    padding: 0.125rem 0.375rem;
    border-radius: 4px;
    margin-top: 0.25rem;
}

.config-descricao {
    display: block;
    font-size: 0.75rem;
    color: hsl(var(--muted-foreground));
    margin-top: 0.25rem;
}

.config-valor-area {
    display: flex;
    align-items: flex-start;
    gap: 0.625rem;
    flex-wrap: wrap;
    flex: 2;
}

.config-valor-display {
    font-size: 0.875rem;
    font-weight: 600;
    color: hsl(var(--foreground));
    padding: 0.375rem 0;
}

.config-input-wrap { flex: 1; min-width: 160px; }

.campo-select {
    width: 100%;
    padding: 0.5rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
    font-size: 0.875rem;
    background: hsl(var(--background));
    color: hsl(var(--foreground));
}

.motivo-field { flex: 1; min-width: 200px; }

.campo-erro {
    font-size: 0.75rem;
    color: hsl(var(--destructive));
    width: 100%;
}

.config-acoes {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}
</style>
