<script setup lang="ts">
import { ref, onMounted } from "vue"
import { automacaoService, type ConfiguracaoAutomacao } from "@/services/automacaoService"
import { AppButton, AppPageHeader } from "@/components/ui"

const config = ref<ConfiguracaoAutomacao>({
    lembretesHabilitados: false,
    horasAntecedenciaLembrete: 24,
    expiracaoOrcamentosHabilitada: true,
    emailRemetente: null,
})

const carregando = ref(false)
const salvando = ref(false)
const erroSalvar = ref<string | null>(null)
const sucessoSalvar = ref(false)

const acionandoExpirar = ref(false)
const acionandoLembretes = ref(false)
const msgAcao = ref<string | null>(null)

onMounted(async () => {
    carregando.value = true
    try {
        config.value = await automacaoService.obterConfiguracao()
    } catch {
        // ignora — usa defaults
    } finally {
        carregando.value = false
    }
})

async function salvar() {
    salvando.value = true
    erroSalvar.value = null
    sucessoSalvar.value = false
    try {
        await automacaoService.salvarConfiguracao(config.value)
        sucessoSalvar.value = true
        setTimeout(() => (sucessoSalvar.value = false), 3000)
    } catch (e: any) {
        erroSalvar.value = e?.response?.data?.mensagem ?? "Erro ao salvar configurações."
    } finally {
        salvando.value = false
    }
}

async function expirarOrcamentos() {
    acionandoExpirar.value = true
    msgAcao.value = null
    try {
        await automacaoService.expirarOrcamentos()
        msgAcao.value = "Orçamentos vencidos expirados com sucesso."
    } catch {
        msgAcao.value = "Erro ao expirar orçamentos."
    } finally {
        acionandoExpirar.value = false
        setTimeout(() => (msgAcao.value = null), 4000)
    }
}

async function enviarLembretes() {
    acionandoLembretes.value = true
    msgAcao.value = null
    try {
        await automacaoService.enviarLembretes()
        msgAcao.value = "Lembretes enviados (verifique os logs do servidor)."
    } catch {
        msgAcao.value = "Erro ao enviar lembretes."
    } finally {
        acionandoLembretes.value = false
        setTimeout(() => (msgAcao.value = null), 4000)
    }
}
</script>

<template>
    <main class="app-page app-page--narrow automacoes">
        <AppPageHeader titulo="Automações" subtitulo="Configure processos automáticos do estabelecimento." />

        <p v-if="carregando" class="info">Carregando configurações...</p>

        <form v-else @submit.prevent="salvar" class="form-config">
            <!-- Lembretes -->
            <section class="bloco">
                <div class="bloco-header">
                    <div>
                        <h2 class="ds-card-title">Lembretes de consulta</h2>
                        <p class="desc">Envia e-mail automático para pacientes antes da consulta agendada.</p>
                    </div>
                    <label class="toggle">
                        <input type="checkbox" v-model="config.lembretesHabilitados" />
                        <span class="toggle-slider"></span>
                    </label>
                </div>

                <div v-if="config.lembretesHabilitados" class="bloco-campos">
                    <label class="campo">
                        <span>Horas de antecedência</span>
                        <input
                            type="number"
                            v-model.number="config.horasAntecedenciaLembrete"
                            min="1" max="72"
                            class="input-sm"
                        />
                        <small>Entre 1 e 72 horas antes da consulta</small>
                    </label>
                    <label class="campo">
                        <span>E-mail remetente (opcional)</span>
                        <input
                            type="email"
                            v-model="config.emailRemetente"
                            placeholder="noreply@suaclinica.com.br"
                            class="input-md"
                        />
                        <small>Deixe em branco para usar o padrão do sistema</small>
                    </label>
                </div>
            </section>

            <!-- Expiração de orçamentos -->
            <section class="bloco">
                <div class="bloco-header">
                    <div>
                        <h2 class="ds-card-title">Expiração automática de orçamentos</h2>
                        <p class="desc">Marca automaticamente como "Expirado" os orçamentos cujo prazo de validade passou.</p>
                    </div>
                    <label class="toggle">
                        <input type="checkbox" v-model="config.expiracaoOrcamentosHabilitada" />
                        <span class="toggle-slider"></span>
                    </label>
                </div>
            </section>

            <div class="acoes-salvar">
                <AppButton type="submit" :loading="salvando">
                    {{ salvando ? "Salvando..." : "Salvar configurações" }}
                </AppButton>
                <span v-if="sucessoSalvar" class="msg-sucesso">Configurações salvas.</span>
                <span v-if="erroSalvar" class="msg-erro">{{ erroSalvar }}</span>
            </div>
        </form>

        <!-- Execução manual -->
        <section class="bloco bloco-manual">
            <h2 class="ds-card-title">Execução manual</h2>
            <p class="desc">Acione os jobs de automação imediatamente (útil para testes).</p>

            <div class="acoes-manual">
                <AppButton
                    variant="secondary"
                    :loading="acionandoExpirar"
                    @click="expirarOrcamentos"
                >
                    {{ acionandoExpirar ? "Executando..." : "Expirar orçamentos vencidos" }}
                </AppButton>
                <AppButton
                    variant="secondary"
                    :loading="acionandoLembretes"
                    @click="enviarLembretes"
                >
                    {{ acionandoLembretes ? "Enviando..." : "Enviar lembretes agora" }}
                </AppButton>
            </div>

            <p v-if="msgAcao" class="msg-acao">{{ msgAcao }}</p>
        </section>
    </main>
</template>

<style scoped>
.bloco {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1.25rem;
    margin-bottom: 1rem;
    box-shadow: var(--shadow);
}

.bloco-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
}

.desc {
    font-size: 0.84em;
    color: var(--text-muted);
    margin: 0;
    line-height: 1.5;
}

.bloco-campos {
    margin-top: 1.1rem;
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
    border-top: 1px solid var(--border);
    padding-top: 1rem;
}

.campo {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
    font-size: 0.88em;
}

.campo span {
    font-weight: 500;
    color: var(--text);
}

.campo small {
    color: var(--text-faint);
    font-size: 0.82em;
}

.input-sm {
    width: 90px;
    padding: 0.35rem 0.6rem;
    border: 1px solid var(--border-strong);
    border-radius: var(--radius-sm);
    font-size: 0.88em;
}

.input-md {
    width: 100%;
    max-width: 320px;
    padding: 0.35rem 0.6rem;
    border: 1px solid var(--border-strong);
    border-radius: var(--radius-sm);
    font-size: 0.88em;
}

/* Toggle switch */
.toggle {
    position: relative;
    display: inline-block;
    width: 44px;
    height: 24px;
    flex-shrink: 0;
}

.toggle input {
    opacity: 0;
    width: 0;
    height: 0;
}

.toggle-slider {
    position: absolute;
    inset: 0;
    background: #d1d5db;
    border-radius: 999px;
    cursor: pointer;
    transition: background 0.2s;
}

.toggle-slider::before {
    content: "";
    position: absolute;
    height: 18px;
    width: 18px;
    left: 3px;
    bottom: 3px;
    background: hsl(var(--card));
    border-radius: 50%;
    transition: transform 0.2s;
}

.toggle input:checked + .toggle-slider {
    background: var(--primary);
}

.toggle input:checked + .toggle-slider::before {
    transform: translateX(20px);
}

/* Ações salvar */
.acoes-salvar {
    display: flex;
    align-items: center;
    gap: 1rem;
    margin-top: 0.5rem;
}

.msg-sucesso {
    font-size: 0.85em;
    color: var(--success);
}

.msg-erro {
    font-size: 0.85em;
    color: var(--danger);
}

/* Manual */
.bloco-manual {
    margin-top: 0.5rem;
}

.acoes-manual {
    display: flex;
    gap: 0.75rem;
    flex-wrap: wrap;
    margin-top: 1rem;
}

.msg-acao {
    margin-top: 0.75rem;
    font-size: 0.85em;
    color: var(--text-muted);
}

.info {
    color: var(--text-faint);
    font-size: 0.9em;
}
</style>
