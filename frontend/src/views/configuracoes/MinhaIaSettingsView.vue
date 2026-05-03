<script setup lang="ts">
import { ref, onMounted } from "vue"
import { iaSettingsService, type IaSettings } from "@/services/iaSettingsService"
import { useTenantStore } from "@/stores/tenantStore"
import { AppPageHeader, AppCard, AppField, AppInput, AppSelect, AppButton } from "@/components/ui"

const tenant = useTenantStore()

const settings = ref<IaSettings>({
    aiEnabled: true,
    aiProvider: "anthropic",
    aiModel: "claude-sonnet-4-6",
    rateLimitPerMinute: 10,
    rateLimitPerDay: 200,
    dataMinimizationLevel: "standard",
})

const carregando = ref(false)
const salvando = ref(false)
const erro = ref<string | null>(null)
const sucesso = ref(false)

const modelosDisponiveis = [
    { valor: "claude-sonnet-4-6", label: "Claude Sonnet 4.6 (Recomendado)" },
    { valor: "claude-opus-4-5", label: "Claude Opus 4.5 (Avancado)" },
    { valor: "claude-haiku-3-5", label: "Claude Haiku 3.5 (Rapido)" },
]

const niveisMinimizacao = [
    { valor: "standard", label: "Padrão — dados clínicos incluídos" },
    { valor: "minimized", label: "Minimizado — dados anonimizados (LGPD+" },
]

async function carregar() {
    if (tenant.papel !== "Dono") return
    carregando.value = true
    try {
        settings.value = await iaSettingsService.obter()
    } catch {
        // usa defaults
    } finally {
        carregando.value = false
    }
}

async function salvar() {
    salvando.value = true
    erro.value = null
    sucesso.value = false
    try {
        await iaSettingsService.salvar(settings.value)
        sucesso.value = true
        setTimeout(() => (sucesso.value = false), 3000)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar configurações."
    } finally {
        salvando.value = false
    }
}

onMounted(carregar)
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            titulo="Configurações de IA"
            subtitulo="Controle o uso do assistente de inteligência artificial no estabelecimento."
        />

        <div v-if="tenant.papel !== 'Dono'" class="acesso-negado">
            <i class="fa-solid fa-lock" aria-hidden="true"></i>
            Apenas o dono do estabelecimento pode alterar as configurações de IA.
        </div>

        <div v-else-if="carregando" class="carregando">
            <i class="fa-solid fa-spinner fa-spin"></i>
            Carregando configurações...
        </div>

        <form v-else @submit.prevent="salvar" class="form-settings">
            <!-- Toggle IA -->
            <AppCard title="Assistente de IA">
                <div class="toggle-row">
                    <div class="toggle-info">
                        <strong>Habilitar assistente de IA</strong>
                        <p class="toggle-desc">
                            Quando desabilitado, nenhum profissional do estabelecimento pode usar o assistente de IA,
                            independente das permissões individuais.
                        </p>
                    </div>
                    <label class="toggle" :aria-label="settings.aiEnabled ? 'IA habilitada' : 'IA desabilitada'">
                        <input type="checkbox" v-model="settings.aiEnabled" role="switch" :aria-checked="settings.aiEnabled" />
                        <span class="toggle-slider"></span>
                    </label>
                </div>
            </AppCard>

            <!-- Modelo -->
            <AppCard title="Modelo e provedor">
                <div class="campos-grid">
                    <AppField label="Modelo de IA" for="ia-model">
                        <AppSelect id="ia-model" v-model="settings.aiModel" :disabled="!settings.aiEnabled">
                            <option v-for="m in modelosDisponiveis" :key="m.valor" :value="m.valor">
                                {{ m.label }}
                            </option>
                        </AppSelect>
                    </AppField>

                    <AppField label="Nível de minimização de dados (LGPD)" for="ia-minimizacao">
                        <AppSelect id="ia-minimizacao" v-model="settings.dataMinimizationLevel" :disabled="!settings.aiEnabled">
                            <option v-for="n in niveisMinimizacao" :key="n.valor" :value="n.valor">
                                {{ n.label }}
                            </option>
                        </AppSelect>
                    </AppField>
                </div>
            </AppCard>

            <!-- Rate limits -->
            <AppCard title="Limites de uso">
                <div class="campos-grid">
                    <AppField
                        label="Requisições por minuto (por usuário)"
                        for="ia-rpm"
                        hint="Recomendado: 10. Máximo: 60."
                    >
                        <AppInput
                            id="ia-rpm"
                            v-model="settings.rateLimitPerMinute"
                            type="number"
                            :min="1"
                            :max="60"
                            :disabled="!settings.aiEnabled"
                        />
                    </AppField>

                    <AppField
                        label="Requisições por dia (por usuário)"
                        for="ia-rpd"
                        hint="Recomendado: 200. Sem limite máximo fixo."
                    >
                        <AppInput
                            id="ia-rpd"
                            v-model="settings.rateLimitPerDay"
                            type="number"
                            :min="1"
                            :disabled="!settings.aiEnabled"
                        />
                    </AppField>
                </div>
            </AppCard>

            <div class="acoes">
                <p v-if="erro" class="msg-erro" role="alert">{{ erro }}</p>
                <p v-if="sucesso" class="msg-sucesso" aria-live="polite">Configurações salvas com sucesso.</p>
                <AppButton type="submit" :loading="salvando">
                    {{ salvando ? "Salvando..." : "Salvar configurações" }}
                </AppButton>
            </div>
        </form>
    </main>
</template>

<style scoped>
.acesso-negado {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    padding: 1rem;
    background: hsl(var(--muted));
    border-radius: var(--radius);
    color: hsl(var(--muted-foreground));
    font-size: 0.9em;
}

.carregando {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: hsl(var(--muted-foreground));
    padding: 2rem 0;
    font-size: 0.9em;
}

.form-settings {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.toggle-row {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
}

.toggle-info { flex: 1; }
.toggle-info strong { font-size: 0.9em; }
.toggle-desc {
    margin: 0.25rem 0 0;
    font-size: 0.82em;
    color: hsl(var(--muted-foreground));
    line-height: 1.5;
}

.campos-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
}

@media (max-width: 640px) {
    .campos-grid { grid-template-columns: 1fr; }
}

.acoes {
    display: flex;
    align-items: center;
    gap: 1rem;
    flex-wrap: wrap;
}

.msg-erro {
    font-size: 0.85em;
    color: hsl(var(--destructive));
    margin: 0;
}

.msg-sucesso {
    font-size: 0.85em;
    color: hsl(var(--success));
    margin: 0;
}

/* Toggle switch */
.toggle {
    position: relative;
    display: inline-block;
    width: 44px;
    height: 24px;
    flex-shrink: 0;
    cursor: pointer;
}
.toggle input { opacity: 0; width: 0; height: 0; position: absolute; }
.toggle-slider {
    position: absolute;
    inset: 0;
    background: hsl(var(--muted-foreground) / 0.4);
    border-radius: 999px;
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
.toggle input:checked + .toggle-slider { background: hsl(var(--primary)); }
.toggle input:checked + .toggle-slider::before { transform: translateX(20px); }
</style>
