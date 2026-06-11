<script setup lang="ts">
/**
 * PlanosFormView — criar/editar plano (F4 atualizado).
 *
 * Substitui o textarea raw de features_json por 8 checkboxes legíveis.
 * Limites de profissionais e pacientes têm campos próprios (vazio = ilimitado).
 * Os demais limites avançados mantêm o textarea de JSON para futura extensão.
 */
import { ref, computed, onMounted } from "vue"
import { useRouter, useRoute } from "vue-router"
import {
    AppPageHeader, AppCard, AppField, AppInput, AppTextarea, AppCheckbox, AppButton,
} from "@/components/ui"
import { usePlanosStore } from "../stores/planosStore"

const router = useRouter()
const route = useRoute()
const store = usePlanosStore()

const isEdicao = computed(() => !!route.params.id)
const idEdicao = computed(() => route.params.id as string | undefined)

const nome = ref("")
const descricaoCurta = ref("")
const precoTexto = ref("")
const gratuito = ref(false)

// Features — 8 flags
const featReceitas = ref(false)
const featExameFisico = ref(false)
const featProcedimentosCirurgicos = ref(false)
const featOrcamentoCompleto = ref(false)
const featIa = ref(false)
const featRelatoriosAvancados = ref(false)
const featAutomacoesIlimitadas = ref(false)
const featAnexosIlimitados = ref(false)

// Limites fixos (vazio = ilimitado)
const limiteProfissionais = ref("")
const limitePacientes = ref("")

// Limites avançados (JSON bruto)
const limitesJsonAvancado = ref("{}")
const motivo = ref("")
const erroLimitesJson = ref("")
const erroGeral = ref("")
const salvando = ref(false)

function parsearFeatures(json: string): void {
    try {
        const f = JSON.parse(json || "{}")
        featReceitas.value = !!f.receitas
        featExameFisico.value = !!f.exame_fisico
        featProcedimentosCirurgicos.value = !!f.procedimentos_cirurgicos
        featOrcamentoCompleto.value = !!f.orcamento_completo
        featIa.value = !!f.ia
        featRelatoriosAvancados.value = !!f.relatorios_avancados
        featAutomacoesIlimitadas.value = !!f.automacoes_ilimitadas
        featAnexosIlimitados.value = !!f.anexos_ilimitados
    } catch {
        // se não parsear, mantém tudo false
    }
}

function parsearLimites(json: string): void {
    try {
        const l = JSON.parse(json || "{}")
        limiteProfissionais.value = l.profissionais != null ? String(l.profissionais) : ""
        limitePacientes.value = l.pacientes != null ? String(l.pacientes) : ""
        // remove os dois campos conhecidos para não duplicar no JSON avançado
        const rest = { ...l }
        delete rest.profissionais
        delete rest.pacientes
        limitesJsonAvancado.value = Object.keys(rest).length > 0 ? JSON.stringify(rest, null, 2) : "{}"
    } catch {
        limitesJsonAvancado.value = json ?? "{}"
    }
}

function montarFeaturesJson(): string {
    return JSON.stringify({
        receitas: featReceitas.value,
        exame_fisico: featExameFisico.value,
        procedimentos_cirurgicos: featProcedimentosCirurgicos.value,
        orcamento_completo: featOrcamentoCompleto.value,
        ia: featIa.value,
        relatorios_avancados: featRelatoriosAvancados.value,
        automacoes_ilimitadas: featAutomacoesIlimitadas.value,
        anexos_ilimitados: featAnexosIlimitados.value,
    })
}

function montarLimitesJson(): string {
    try {
        const base = JSON.parse(limitesJsonAvancado.value || "{}")
        if (limiteProfissionais.value.trim())
            base.profissionais = parseInt(limiteProfissionais.value, 10)
        if (limitePacientes.value.trim())
            base.pacientes = parseInt(limitePacientes.value, 10)
        return JSON.stringify(base)
    } catch {
        erroLimitesJson.value = "JSON avançado inválido."
        return limitesJsonAvancado.value
    }
}

onMounted(async () => {
    if (isEdicao.value && idEdicao.value) {
        await store.carregarPlano(idEdicao.value)
        const p = store.planoAtual
        if (p) {
            nome.value = p.nome
            descricaoCurta.value = p.descricaoCurta ?? ""
            precoTexto.value = p.precoMensalCentavos != null
                ? (p.precoMensalCentavos / 100).toFixed(2)
                : ""
            gratuito.value = p.gratuito
            parsearFeatures(p.featuresJson ?? "{}")
            parsearLimites(p.limitesJson ?? "{}")
        }
    }
})

function validarJson(): boolean {
    try {
        JSON.parse(limitesJsonAvancado.value)
        erroLimitesJson.value = ""
        return true
    } catch {
        erroLimitesJson.value = "JSON avançado inválido. Corrija o formato antes de salvar."
        return false
    }
}

function calcularCentavos(): number | null {
    if (!precoTexto.value.trim()) return null
    const valor = parseFloat(precoTexto.value.replace(",", "."))
    if (isNaN(valor) || valor < 0) return null
    return Math.round(valor * 100)
}

async function salvar() {
    if (!validarJson()) return
    if (motivo.value.trim().length < 10) {
        erroGeral.value = "Motivo deve ter ao menos 10 caracteres."
        return
    }

    salvando.value = true
    erroGeral.value = ""

    const payload = {
        nome: nome.value.trim(),
        descricaoCurta: descricaoCurta.value.trim() || null,
        precoMensalCentavos: calcularCentavos(),
        gratuito: gratuito.value,
        limitesJson: montarLimitesJson(),
        featuresJson: montarFeaturesJson(),
        motivo: motivo.value.trim(),
    }

    try {
        if (isEdicao.value && idEdicao.value) {
            await store.atualizar(idEdicao.value, payload)
        } else {
            await store.criar(payload)
        }
        router.push({ name: "AdminPlanosList" })
    } catch (err: unknown) {
        const msg = (err as { response?: { data?: { mensagem?: string } } })?.response?.data?.mensagem
        erroGeral.value = msg ?? "Não foi possível salvar o plano."
    } finally {
        salvando.value = false
    }
}
</script>

<template>
    <main class="app-page app-page--narrow">
        <AppPageHeader
            :titulo="isEdicao ? 'Editar plano' : 'Novo plano'"
            subtitulo="Configure os dados e funcionalidades do plano."
        />

        <div v-if="store.carregando" class="estado-info">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>

        <AppCard v-else>
            <form @submit.prevent="salvar" class="form-campos">
                <!-- Identificação -->
                <AppField label="Nome do plano" required>
                    <AppInput
                        v-model="nome"
                        placeholder="Ex: Plano Profissional"
                        :disabled="salvando"
                        maxlength="100"
                    />
                </AppField>

                <AppField label="Descrição curta">
                    <AppInput
                        v-model="descricaoCurta"
                        placeholder="Descrição opcional"
                        :disabled="salvando"
                        maxlength="200"
                    />
                </AppField>

                <AppField label="Preço mensal (R$)" hint="Deixe vazio para 'sob consulta'.">
                    <AppInput
                        v-model="precoTexto"
                        placeholder="Ex: 99.90"
                        :disabled="salvando"
                    />
                </AppField>

                <AppCheckbox v-model="gratuito" label="Plano gratuito (sem cobrança)" :disabled="salvando" />

                <!-- Funcionalidades -->
                <div class="secao-titulo">
                    <h3 class="ds-card-title">Funcionalidades habilitadas</h3>
                    <p class="secao-hint">Selecione as funcionalidades disponíveis neste plano.</p>
                </div>

                <div class="features-grid">
                    <AppCheckbox v-model="featReceitas" label="Receitas" :disabled="salvando" />
                    <AppCheckbox v-model="featExameFisico" label="Exame físico" :disabled="salvando" />
                    <AppCheckbox v-model="featProcedimentosCirurgicos" label="Procedimentos cirúrgicos" :disabled="salvando" />
                    <AppCheckbox v-model="featOrcamentoCompleto" label="Orçamento completo" :disabled="salvando" />
                    <AppCheckbox v-model="featIa" label="IA (inteligência artificial)" :disabled="salvando" />
                    <AppCheckbox v-model="featRelatoriosAvancados" label="Relatórios avançados" :disabled="salvando" />
                    <AppCheckbox v-model="featAutomacoesIlimitadas" label="Automações ilimitadas" :disabled="salvando" />
                    <AppCheckbox v-model="featAnexosIlimitados" label="Anexos ilimitados" :disabled="salvando" />
                </div>

                <!-- Limites -->
                <div class="secao-titulo">
                    <h3 class="ds-card-title">Limites</h3>
                    <p class="secao-hint">Deixe vazio para ilimitado.</p>
                </div>

                <div class="limites-row">
                    <AppField label="Profissionais">
                        <AppInput
                            v-model="limiteProfissionais"
                            type="number"
                            placeholder="Ilimitado"
                            :disabled="salvando"
                        />
                    </AppField>
                    <AppField label="Pacientes">
                        <AppInput
                            v-model="limitePacientes"
                            type="number"
                            placeholder="Ilimitado"
                            :disabled="salvando"
                        />
                    </AppField>
                </div>

                <AppField
                    label="Limites avançados (JSON)"
                    :hint="erroLimitesJson || 'Outros limites e configurações avançadas do plano.'"
                >
                    <AppTextarea
                        v-model="limitesJsonAvancado"
                        :rows="4"
                        placeholder="{}"
                        :disabled="salvando"
                        @blur="validarJson"
                    />
                </AppField>

                <!-- Motivo -->
                <AppField label="Motivo da alteração" required hint="Mínimo 10 caracteres.">
                    <AppTextarea
                        v-model="motivo"
                        :rows="2"
                        placeholder="Informe o motivo..."
                        :disabled="salvando"
                    />
                </AppField>

                <p v-if="erroGeral" class="campo-erro" role="alert">{{ erroGeral }}</p>

                <div class="form-acoes">
                    <AppButton variant="ghost" type="button" @click="router.back()">Cancelar</AppButton>
                    <AppButton
                        type="submit"
                        :loading="salvando"
                        :disabled="motivo.trim().length < 10"
                    >
                        {{ isEdicao ? "Salvar alterações" : "Criar plano" }}
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

.secao-titulo {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    padding-top: 0.5rem;
    border-top: 1px solid hsl(var(--border));
}

.secao-hint {
    font-size: var(--text-sm);
    color: hsl(var(--muted-foreground));
    margin: 0;
}

.features-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.75rem 1.5rem;
}

.limites-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
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

.form-acoes {
    display: flex;
    gap: 0.75rem;
    justify-content: flex-end;
    padding-top: 0.5rem;
}
</style>
