<script setup lang="ts">
/**
 * FinanceiroConfigTab — Configurações financeiras embutidas no painel /financeiro.
 *
 * Layout: grid 2-col.
 *  - Card comissão (full-width / cfg-span): funcional, seleciona profissional, percentuais.
 *  - Card taxa de cartão: "Em breve" (decisão D4 do briefing 2026-06-11_002).
 *  - Card tabela de preços: "Em breve".
 *
 * Apenas Dono vê o card de comissão (CA178).
 */
import { ref, onMounted } from "vue"
import { AppButton, AppField, AppInputDecimal, AppToast } from "@/components/ui"
import { financeiroService, type ConfigComissao } from "@/services/financeiroService"
import { vinculoService, type ProfissionalPublico } from "@/services/vinculoService"

const props = defineProps<{ ehDono: boolean }>()

// ─── Config de comissão ────────────────────────────────────────────────────────
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

        <!-- Card comissão (apenas Dono) -->
        <div v-if="ehDono" class="cfg-card cfg-span">
            <div class="cfg-card-h">
                <div class="cfg-card-title-block">
                    <span class="cfg-ic comissoes"><i class="fa-solid fa-percent" aria-hidden="true" /></span>
                    <div>
                        <b>Comissões por profissional</b>
                        <p>Defina o percentual de consulta e procedimento por profissional. Padrão: 30%.</p>
                    </div>
                </div>
            </div>

            <div class="cfg-card-body">
                <!-- Seletor de profissional -->
                <div class="prof-selector">
                    <span class="prof-selector-label">Profissional</span>
                    <div v-if="carregandoProf" class="info">Carregando profissionais...</div>
                    <div v-else class="prof-list">
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
                    <div v-if="carregandoConfig" class="info">Carregando configuração...</div>
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
                <p v-else class="info sel-hint">
                    <i class="fa-regular fa-hand-pointer" aria-hidden="true" />
                    Selecione um profissional acima para configurar.
                </p>
            </div>
        </div>

        <!-- Grid 2-col para cards Em breve -->
        <div class="cfg-grid">

            <!-- Taxa de cartão — Em breve (D4) -->
            <div class="cfg-card cfg-soon">
                <div class="cfg-card-h">
                    <div class="cfg-card-title-block">
                        <span class="cfg-ic taxa"><i class="fa-solid fa-credit-card" aria-hidden="true" /></span>
                        <div>
                            <b>Taxa de cartão</b>
                            <p>Registre taxas de adquirente para desconto automático no faturamento líquido.</p>
                        </div>
                    </div>
                </div>
                <div class="cfg-soon-body">
                    <span class="soon-pill">
                        <i class="fa-solid fa-clock" aria-hidden="true" />
                        Em breve
                    </span>
                    <p>Esta funcionalidade estará disponível em breve.</p>
                </div>
            </div>

            <!-- Tabela de preços — Em breve (D4) -->
            <div class="cfg-card cfg-soon">
                <div class="cfg-card-h">
                    <div class="cfg-card-title-block">
                        <span class="cfg-ic precos"><i class="fa-solid fa-tag" aria-hidden="true" /></span>
                        <div>
                            <b>Tabela de preços</b>
                            <p>Gerencie os preços padrão por procedimento e plano de saúde.</p>
                        </div>
                    </div>
                </div>
                <div class="cfg-soon-body">
                    <span class="soon-pill">
                        <i class="fa-solid fa-clock" aria-hidden="true" />
                        Em breve
                    </span>
                    <p>Esta funcionalidade estará disponível em breve.</p>
                </div>
            </div>

        </div>
    </div>

    <AppToast v-if="toast" :mensagem="toast.mensagem" :variante="toast.variante" @fechar="toast = null" />
</template>

<style scoped>
.config-tab { display: flex; flex-direction: column; gap: 16px; }

/* Grid 2-col */
.cfg-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
}
@media (max-width: 800px) {
    .cfg-grid { grid-template-columns: 1fr; }
}

/* Card base */
.cfg-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.1);
    border-radius: 12px;
    overflow: hidden;
}
/* Full-width: span das 2 colunas */
.cfg-span { grid-column: 1 / -1; }

/* Cabeçalho do card */
.cfg-card-h {
    padding: 16px 18px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.07);
}
.cfg-card-title-block {
    display: flex;
    align-items: flex-start;
    gap: 13px;
}
.cfg-ic {
    flex-shrink: 0;
    width: 36px;
    height: 36px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--text-base);
}
.cfg-ic.comissoes { background: hsl(var(--primary) / 0.1); color: hsl(var(--primary)); }
.cfg-ic.taxa       { background: hsl(var(--warning) / 0.12); color: hsl(28 90% 45%); }
.cfg-ic.precos     { background: hsl(142 71% 45% / 0.1); color: hsl(142 71% 35%); }

.cfg-card-title-block > div > b {
    display: block;
    font-size: var(--text-base);
    font-weight: var(--font-weight-bold);
    color: var(--c-primary-dark);
    margin-bottom: 2px;
}
.cfg-card-title-block > div > p {
    font-size: var(--text-xs);
    color: hsl(var(--secondary) / 0.6);
    margin: 0;
    line-height: 1.5;
}

/* Corpo do card comissão */
.cfg-card-body { padding: 18px; display: flex; flex-direction: column; gap: 16px; }

/* Seletor de profissional */
.prof-selector { display: flex; flex-direction: column; gap: 10px; }
.prof-selector-label {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: var(--c-primary-dark);
}
.prof-list { display: flex; gap: 8px; flex-wrap: wrap; }
.prof-chip {
    padding: 6px 14px;
    border: 1px solid hsl(var(--secondary) / 0.15);
    border-radius: 999px;
    font-size: var(--text-sm);
    background: hsl(var(--secondary) / 0.04);
    cursor: pointer;
    color: hsl(var(--secondary) / 0.8);
    transition: background 0.12s, border-color 0.12s, color 0.12s;
    border-style: solid;
}
.prof-chip.ativo {
    background: hsl(var(--primary));
    border-color: hsl(var(--primary));
    color: #fff;
}
.prof-chip:hover:not(.ativo) {
    background: hsl(var(--secondary) / 0.08);
    color: var(--c-primary-dark);
}

/* Formulário de comissão */
.comissao-form {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
    max-width: 600px;
}
.comissao-form .msg-erro { grid-column: 1 / -1; }
.form-acoes { grid-column: 1 / -1; display: flex; justify-content: flex-end; }
@media (max-width: 600px) {
    .comissao-form { grid-template-columns: 1fr; }
}

/* Hint de seleção */
.sel-hint {
    display: inline-flex;
    align-items: center;
    gap: 8px;
}
.sel-hint i { color: hsl(var(--secondary) / 0.4); }

/* Card Em breve */
.cfg-soon-body {
    padding: 24px 18px;
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    gap: 10px;
}
.soon-pill {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 5px 14px;
    background: hsl(var(--secondary) / 0.06);
    border: 1px solid hsl(var(--secondary) / 0.12);
    border-radius: 999px;
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--secondary) / 0.6);
}
.cfg-soon-body > p {
    font-size: var(--text-sm);
    color: hsl(var(--secondary) / 0.55);
    margin: 0;
}

.info { color: hsl(var(--secondary) / 0.6); font-size: var(--text-sm); }
.msg-erro { color: hsl(var(--destructive)); font-size: var(--text-sm); margin: 0; }
</style>
