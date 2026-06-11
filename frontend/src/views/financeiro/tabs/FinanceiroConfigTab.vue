<script setup lang="ts">
/**
 * FinanceiroConfigTab — Configurações financeiras embutidas no painel /financeiro (CA183).
 *
 * Reutiliza FinanceiroConfigView (preços de consulta + taxa de cartão) sem duplicação.
 * Adiciona seção de config de comissão (percentual por tipo) visível apenas para Dono.
 *
 * Links rápidos para /financeiro/categorias e /financeiro/formas-pagamento (CA184).
 */
import { ref, onMounted } from "vue"
import FinanceiroConfigView from "../FinanceiroConfigView.vue"
import { AppButton, AppField, AppInputDecimal, AppToast } from "@/components/ui"
import { financeiroService, type ConfigComissao } from "@/services/financeiroService"
import { vinculoService, type ProfissionalPublico } from "@/services/vinculoService"

const props = defineProps<{ ehDono: boolean }>()

// ─── Profissionais + config de comissão ───────────────────────────────────────
const profissionais = ref<ProfissionalPublico[]>([])
const profSelecionado = ref<ProfissionalPublico | null>(null)
const config = ref<ConfigComissao | null>(null)
const carregandoProf = ref(false)
const carregandoConfig = ref(false)
const salvando = ref(false)
const formComissao = ref({ percentualConsulta: null as number | null, percentualProcedimento: null as number | null })
const erroComissao = ref<string | null>(null)
const toast = ref<{ mensagem: string; variante: "success" | "error" } | null>(null)

async function carregarProfissionais() {
    if (!props.ehDono) return
    carregandoProf.value = true
    try {
        profissionais.value = await vinculoService.listarProfissionaisPublico()
    } finally {
        carregandoProf.value = false
    }
}

async function selecionarProfissional(prof: ProfissionalPublico) {
    profSelecionado.value = prof
    carregandoConfig.value = true
    erroComissao.value = null
    try {
        config.value = await financeiroService.obterConfigComissao(prof.usuarioId)
        formComissao.value = {
            percentualConsulta: config.value.percentualConsulta,
            percentualProcedimento: config.value.percentualProcedimento,
        }
    } catch {
        config.value = null
    } finally {
        carregandoConfig.value = false
    }
}

async function salvarComissao() {
    if (!profSelecionado.value) return
    salvando.value = true; erroComissao.value = null
    try {
        await financeiroService.salvarConfigComissao({
            profissionalUsuarioId: profSelecionado.value.usuarioId,
            percentualConsulta: formComissao.value.percentualConsulta,
            percentualProcedimento: formComissao.value.percentualProcedimento,
        })
        toast.value = { mensagem: "Comissão salva.", variante: "success" }
        // Recarregar config.
        config.value = await financeiroService.obterConfigComissao(profSelecionado.value.usuarioId)
    } catch (e: any) {
        erroComissao.value = e?.response?.data?.mensagem ?? "Erro ao salvar comissão."
    } finally {
        salvando.value = false
    }
}

onMounted(carregarProfissionais)
</script>

<template>
    <div class="config-tab">
        <!-- Configurações de cobrança (reuso completo — CA183) -->
        <FinanceiroConfigView />

        <!-- Configurações de comissão (apenas Dono) -->
        <section v-if="ehDono" class="comissao-config-section">
            <h2 class="ds-section-title">Comissões por profissional</h2>
            <p class="descricao-secao">
                Defina o percentual de consulta e procedimento por profissional.
                Se não configurado, o padrão de 30% é aplicado.
            </p>

            <div class="prof-selector">
                <label class="prof-selector-label">Profissional</label>
                <div class="prof-list">
                    <button
                        v-for="p in profissionais"
                        :key="p.usuarioId"
                        class="prof-chip"
                        :class="{ ativo: profSelecionado?.usuarioId === p.usuarioId }"
                        @click="selecionarProfissional(p)"
                    >
                        {{ p.nomeCompleto }}
                    </button>
                </div>
            </div>

            <template v-if="profSelecionado">
                <p v-if="carregandoConfig" class="info">Carregando...</p>
                <div v-else class="comissao-form">
                    <AppField label="Percentual — Consultas (%)">
                        <AppInputDecimal
                            v-model="formComissao.percentualConsulta"
                            :min="0"
                            :max="100"
                            :placeholder="`Padrão: ${config?.percentualPadrao ?? 30}%`"
                        />
                    </AppField>
                    <AppField label="Percentual — Procedimentos (%)">
                        <AppInputDecimal
                            v-model="formComissao.percentualProcedimento"
                            :min="0"
                            :max="100"
                            :placeholder="`Padrão: ${config?.percentualPadrao ?? 30}%`"
                        />
                    </AppField>
                    <p v-if="erroComissao" class="msg-erro">{{ erroComissao }}</p>
                    <div class="form-acoes">
                        <AppButton :loading="salvando" @click="salvarComissao">Salvar comissão</AppButton>
                    </div>
                </div>
            </template>
            <p v-else class="info">Selecione um profissional para configurar a comissão.</p>
        </section>

        <!-- Links para rotas filhas (CA184) -->
        <section class="links-config">
            <h2 class="ds-section-title">Outras configurações</h2>
            <div class="links-list">
                <a href="/financeiro/categorias" class="config-link">
                    <i class="fa-solid fa-tags" />
                    Categorias financeiras
                </a>
                <a href="/financeiro/formas-pagamento" class="config-link">
                    <i class="fa-solid fa-credit-card" />
                    Formas de pagamento
                </a>
            </div>
        </section>
    </div>

    <AppToast v-if="toast" :mensagem="toast.mensagem" :variante="toast.variante" @fechar="toast = null" />
</template>

<style scoped>
.config-tab { display: flex; flex-direction: column; gap: 2rem; }

.comissao-config-section {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 10px;
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}
.descricao-secao { font-size: var(--text-sm); color: hsl(var(--muted-foreground)); margin: 0; }

.prof-selector-label { font-size: var(--text-sm); font-weight: var(--font-weight-medium); margin-bottom: 0.5rem; display: block; }
.prof-list { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.prof-chip {
    padding: 0.3rem 0.85rem;
    border: 1px solid hsl(var(--border));
    border-radius: 999px;
    font-size: var(--text-sm);
    background: hsl(var(--background));
    cursor: pointer;
    color: hsl(var(--foreground));
    transition: background 0.15s, border-color 0.15s;
}
.prof-chip.ativo {
    background: hsl(var(--primary));
    border-color: hsl(var(--primary));
    color: hsl(var(--primary-foreground));
}
.prof-chip:hover:not(.ativo) { background: hsl(var(--muted)); }

.comissao-form { display: flex; flex-direction: column; gap: 0.75rem; max-width: 480px; }
.form-acoes { display: flex; justify-content: flex-end; }

.links-config { display: flex; flex-direction: column; gap: 0.75rem; }
.links-list { display: flex; gap: 1rem; flex-wrap: wrap; }
.config-link {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.65rem 1.1rem;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--border));
    border-radius: 8px;
    text-decoration: none;
    color: hsl(var(--foreground));
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    transition: background 0.15s;
}
.config-link:hover { background: hsl(var(--muted)); }

.info { color: hsl(var(--muted-foreground)); font-size: var(--text-sm); }
.msg-erro { color: hsl(var(--destructive)); font-size: var(--text-sm); margin: 0; }
</style>
