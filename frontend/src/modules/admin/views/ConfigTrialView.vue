<script setup lang="ts">
/**
 * ConfigTrialView — F4/CA30
 *
 * Edição do singleton imedto_config_trial: plano de trial, duração e habilitar/desabilitar.
 * Rota: /admin/config-trial
 */
import { ref, onMounted } from "vue"
import {
    AppPageHeader, AppCard, AppField, AppInput, AppCheckbox, AppButton,
} from "@/components/ui"
import { usePlanosStore } from "../stores/planosStore"
import { configTrialService } from "../services/configTrialService"

const planosStore = usePlanosStore()

const planoTrialId = ref("")
const duracaoTrialDias = ref("14")
const trialHabilitado = ref(true)
const motivo = ref("")
const erroGeral = ref("")
const salvando = ref(false)
const carregando = ref(false)
const sucesso = ref(false)

onMounted(async () => {
    carregando.value = true
    await planosStore.carregar({ ativo: true })
    try {
        const config = await configTrialService.obter()
        planoTrialId.value = config.planoTrialId
        duracaoTrialDias.value = String(config.duracaoTrialDias)
        trialHabilitado.value = config.trialHabilitado
    } catch {
        erroGeral.value = "Não foi possível carregar a configuração de trial."
    } finally {
        carregando.value = false
    }
})

async function salvar() {
    const dias = parseInt(duracaoTrialDias.value, 10)
    if (!planoTrialId.value) {
        erroGeral.value = "Selecione o plano de trial."
        return
    }
    if (isNaN(dias) || dias < 1) {
        erroGeral.value = "Duração em dias inválida."
        return
    }
    if (motivo.value.trim().length < 10) {
        erroGeral.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }

    salvando.value = true
    erroGeral.value = ""
    sucesso.value = false

    try {
        await configTrialService.atualizar({
            planoTrialId: planoTrialId.value,
            duracaoTrialDias: dias,
            trialHabilitado: trialHabilitado.value,
            motivo: motivo.value.trim(),
        })
        sucesso.value = true
        motivo.value = ""
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroGeral.value = msg ?? "Não foi possível salvar a configuração."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            titulo="Configuração de trial"
            subtitulo="Define o plano e a duração do trial automático para novos estabelecimentos."
        />

        <div v-if="carregando" class="estado-info">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>

        <AppCard v-else>
            <form @submit.prevent="salvar" class="form-campos">
                <AppField label="Plano de trial" required hint="Plano aplicado automaticamente no cadastro.">
                    <select v-model="planoTrialId" class="form-select" :disabled="salvando">
                        <option value="">— selecione —</option>
                        <option
                            v-for="p in planosStore.lista"
                            :key="p.id"
                            :value="p.id"
                        >{{ p.nome }}</option>
                    </select>
                </AppField>

                <AppField label="Duração (dias)" required hint="Número de dias do período de trial.">
                    <AppInput
                        v-model="duracaoTrialDias"
                        type="number"
                        placeholder="14"
                        :disabled="salvando"
                    />
                </AppField>

                <AppCheckbox
                    v-model="trialHabilitado"
                    label="Trial automático habilitado"
                    :disabled="salvando"
                />

                <AppField label="Motivo da alteração" required hint="Mínimo 10 caracteres.">
                    <AppInput
                        v-model="motivo"
                        placeholder="Ex: ajuste de política comercial..."
                        :disabled="salvando"
                    />
                </AppField>

                <p v-if="sucesso" class="campo-sucesso" role="status">Configuração salva com sucesso.</p>
                <p v-if="erroGeral" class="campo-erro" role="alert">{{ erroGeral }}</p>

                <div class="form-acoes">
                    <AppButton
                        type="submit"
                        :loading="salvando"
                        :disabled="!planoTrialId || motivo.trim().length < 10"
                    >
                        Salvar configuração
                    </AppButton>
                </div>
            </form>
        </AppCard>
    </main>
</template>

<style scoped>
.estado-info {
    text-align: center;
    padding: 2rem 0;
    color: hsl(var(--muted-foreground));
    font-size: var(--text-sm);
}

.form-campos {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.form-select {
    width: 100%;
    padding: 0.5rem 0.75rem;
    border: 1px solid hsl(var(--border));
    border-radius: calc(var(--radius) - 2px);
    background: hsl(var(--background));
    color: hsl(var(--foreground));
    font-size: var(--text-sm);
    font-family: inherit;
}

.campo-erro {
    padding: 0.75rem 1rem;
    background: hsl(var(--destructive) / 0.1);
    color: hsl(var(--destructive));
    border: 1px solid hsl(var(--destructive) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
    margin: 0;
}

.campo-sucesso {
    padding: 0.75rem 1rem;
    background: hsl(var(--success) / 0.1);
    color: hsl(var(--success));
    border: 1px solid hsl(var(--success) / 0.3);
    border-radius: calc(var(--radius) - 2px);
    font-size: var(--text-sm);
    margin: 0;
}

.form-acoes {
    display: flex;
    justify-content: flex-end;
    padding-top: 0.5rem;
}
</style>
