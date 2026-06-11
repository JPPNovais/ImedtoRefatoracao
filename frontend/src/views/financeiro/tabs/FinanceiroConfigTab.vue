<script setup lang="ts">
/**
 * FinanceiroConfigTab — Configurações financeiras embutidas no painel /financeiro.
 *
 * Layout: grid 2-col.
 *  - Card comissão (full-width / cfg-span): lista de profissionais com percentuais + modal de edição.
 *  - Card taxa de cartão: "Em breve" (decisão D4 do briefing 2026-06-11_002).
 *  - Card tabela de preços: "Em breve".
 *
 * Apenas Dono vê o card de comissão (CA178 / briefing 2026-06-11_002).
 * Redesign: lista + modal — briefing 2026-06-11_004.
 */
import { ref, onMounted } from "vue"
import { AppButton, AppField, AppInputDecimal, AppToast, AppModal, AppBadge } from "@/components/ui"
import { financeiroService, type ConfigComissao } from "@/services/financeiroService"
import { vinculoService, type ProfissionalPublico } from "@/services/vinculoService"

const props = defineProps<{ ehDono: boolean }>()

// ─── Tipo para linha da lista ────────────────────────────────────────────────
interface LinhaComissao {
    profissional: ProfissionalPublico
    config: ConfigComissao
}

// ─── Estado da lista ─────────────────────────────────────────────────────────
const linhas = ref<LinhaComissao[]>([])
const percentualPadrao = ref<number>(30)
const carregando = ref(false)
const erroCarregar = ref<string | null>(null)

// ─── Estado do modal de edição ───────────────────────────────────────────────
const modalAberto = ref(false)
const profEditando = ref<ProfissionalPublico | null>(null)
// AppInputDecimal emite string; convertemos para Number só no payload (null permanece null)
const formEdicao = ref<{ percentualConsulta: string | null; percentualProcedimento: string | null }>({
    percentualConsulta: null,
    percentualProcedimento: null,
})
const salvando = ref(false)
const erroSalvar = ref<string | null>(null)

// ─── Toast ───────────────────────────────────────────────────────────────────
const toast = ref<{ mensagem: string; variante: "success" | "error" } | null>(null)

// ─── Helpers ─────────────────────────────────────────────────────────────────
function ePadrao(linha: LinhaComissao): boolean {
    // R1: badge PADRÃO quando ambos percentuais iguais ao padrão do sistema
    const padrao = linha.config.percentualPadrao
    const consulta = linha.config.percentualConsulta ?? padrao
    const procedimento = linha.config.percentualProcedimento ?? padrao
    return consulta === padrao && procedimento === padrao
}

function formatarPct(valor: number | null, padrao: number): string {
    return String(valor ?? padrao)
}

// ─── Carregamento ────────────────────────────────────────────────────────────
async function carregarTodos() {
    if (!props.ehDono) return
    carregando.value = true
    erroCarregar.value = null
    try {
        const profissionais = await vinculoService.listarProfissionaisPublico()
        // Opção A: N requisições em paralelo (seguro — paralelismo no front, não na mesma conexão Npgsql)
        const configs = await Promise.all(
            profissionais.map(p => financeiroService.obterConfigComissao(p.usuarioId))
        )
        linhas.value = profissionais.map((p, i) => ({ profissional: p, config: configs[i] }))
        if (configs.length > 0) {
            percentualPadrao.value = configs[0].percentualPadrao
        }
    } catch {
        erroCarregar.value = "Erro ao carregar comissões."
    } finally {
        carregando.value = false
    }
}

// ─── Modal de edição ─────────────────────────────────────────────────────────
function abrirModal(linha: LinhaComissao) {
    profEditando.value = linha.profissional
    const padrao = linha.config.percentualPadrao
    const c = linha.config.percentualConsulta ?? padrao
    const p = linha.config.percentualProcedimento ?? padrao
    formEdicao.value = {
        percentualConsulta: c != null ? String(c) : null,
        percentualProcedimento: p != null ? String(p) : null,
    }
    erroSalvar.value = null
    modalAberto.value = true
}

function fecharModal() {
    modalAberto.value = false
    profEditando.value = null
    erroSalvar.value = null
}

async function salvarComissao() {
    if (!profEditando.value) return
    salvando.value = true
    erroSalvar.value = null
    try {
        const toNum = (v: string | null): number | null =>
            v === null || v === "" ? null : Number(v)
        await financeiroService.salvarConfigComissao({
            profissionalUsuarioId: profEditando.value.usuarioId,
            percentualConsulta: toNum(formEdicao.value.percentualConsulta),
            percentualProcedimento: toNum(formEdicao.value.percentualProcedimento),
        })
        // Atualiza linha localmente re-buscando config do profissional editado
        const novaConfig = await financeiroService.obterConfigComissao(profEditando.value.usuarioId)
        const idx = linhas.value.findIndex(l => l.profissional.usuarioId === profEditando.value!.usuarioId)
        if (idx !== -1) {
            linhas.value[idx] = { ...linhas.value[idx], config: novaConfig }
        }
        toast.value = { mensagem: "Comissão salva.", variante: "success" }
        fecharModal()
    } catch (e: any) {
        erroSalvar.value = "Erro ao salvar comissão."
    } finally {
        salvando.value = false
    }
}

onMounted(carregarTodos)
</script>

<template>
    <div class="config-tab">

        <!-- Card comissão (apenas Dono) — lista + modal (briefing 2026-06-11_004) -->
        <div v-if="ehDono" class="cfg-card cfg-span">
            <div class="cfg-card-h">
                <div class="cfg-card-title-block">
                    <span class="cfg-ic comissoes"><i class="fa-solid fa-percent" aria-hidden="true" /></span>
                    <div>
                        <b>Comissões por profissional</b>
                        <p>Defina o percentual de consulta e procedimento por profissional.</p>
                    </div>
                </div>
                <span class="cfg-padrao-tag">Padrão do sistema: {{ percentualPadrao }}%</span>
            </div>

            <div class="cfg-comm-lista">
                <!-- Estado: carregando -->
                <p v-if="carregando" class="comm-state-msg">
                    <i class="fa-solid fa-circle-notch fa-spin" aria-hidden="true" />
                    Carregando comissões...
                </p>

                <!-- Estado: erro ao carregar -->
                <p v-else-if="erroCarregar" class="comm-state-msg comm-state-erro">
                    {{ erroCarregar }}
                </p>

                <!-- Estado: vazio -->
                <p v-else-if="linhas.length === 0" class="comm-state-msg">
                    Nenhum profissional cadastrado neste estabelecimento.
                </p>

                <!-- Lista de profissionais -->
                <template v-else>
                    <div
                        v-for="linha in linhas"
                        :key="linha.profissional.usuarioId"
                        class="comm-row"
                    >
                        <span class="comm-name">
                            <i class="fa-solid fa-user-doctor" aria-hidden="true" />
                            {{ linha.profissional.nomeCompleto }}
                        </span>
                        <span class="comm-vals">
                            Consulta {{ formatarPct(linha.config.percentualConsulta, linha.config.percentualPadrao) }}%
                            · Procedimento {{ formatarPct(linha.config.percentualProcedimento, linha.config.percentualPadrao) }}%
                        </span>
                        <AppBadge v-if="ePadrao(linha)" variant="muted" label="PADRÃO" />
                        <span v-else class="comm-badge-placeholder" />
                        <button
                            class="btn-icon btn-icon-editar"
                            title="Editar comissão"
                            @click="abrirModal(linha)"
                        >
                            <i class="fa-solid fa-pen" aria-hidden="true" />
                        </button>
                    </div>
                </template>
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

    <!-- Modal de edição de comissão -->
    <AppModal
        :aberto="modalAberto"
        largura="sm"
        :titulo="profEditando ? `Editar comissão — ${profEditando.nomeCompleto}` : 'Editar comissão'"
        @fechar="fecharModal"
    >
        <AppField label="Percentual — Consultas (%)">
            <AppInputDecimal
                v-model="formEdicao.percentualConsulta"
                :decimals="0"
                :placeholder="`Padrão: ${percentualPadrao}%`"
            />
        </AppField>
        <AppField label="Percentual — Procedimentos (%)">
            <AppInputDecimal
                v-model="formEdicao.percentualProcedimento"
                :decimals="0"
                :placeholder="`Padrão: ${percentualPadrao}%`"
            />
        </AppField>
        <p v-if="erroSalvar" class="modal-erro">{{ erroSalvar }}</p>

        <template #rodape>
            <AppButton variant="secondary" @click="fecharModal">Cancelar</AppButton>
            <AppButton :loading="salvando" @click="salvarComissao">Salvar</AppButton>
        </template>
    </AppModal>

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
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
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

/* Tag "Padrão do sistema: X%" no cabeçalho */
.cfg-padrao-tag {
    flex-shrink: 0;
    font-size: var(--text-xs);
    font-weight: var(--font-weight-semibold);
    color: hsl(var(--secondary) / 0.6);
    background: hsl(var(--secondary) / 0.06);
    padding: 4px 11px;
    border-radius: 999px;
    white-space: nowrap;
}

/* Lista de comissão */
.cfg-comm-lista { padding: 8px; }

.comm-state-msg {
    padding: 16px 12px;
    font-size: var(--text-sm);
    color: hsl(var(--secondary) / 0.6);
    margin: 0;
    display: flex;
    align-items: center;
    gap: 8px;
}
.comm-state-erro { color: hsl(var(--destructive)); }

/* Linha de profissional */
.comm-row {
    display: grid;
    grid-template-columns: 1fr auto auto auto;
    gap: 14px;
    align-items: center;
    padding: 11px 12px;
    border-radius: 8px;
    transition: background 0.1s;
}
.comm-row:hover { background: hsl(var(--secondary) / 0.03); }

.comm-name {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-semibold);
    color: var(--c-primary-dark);
    display: inline-flex;
    align-items: center;
    gap: 8px;
}
.comm-name i { color: hsl(var(--secondary) / 0.4); }

.comm-vals {
    font-size: var(--text-sm);
    color: hsl(var(--secondary) / 0.75);
    white-space: nowrap;
}

/* Placeholder para manter alinhamento quando não há badge */
.comm-badge-placeholder { display: inline-block; width: 56px; }

/* Responsivo: em telas estreitas, percentuais vão abaixo do nome */
@media (max-width: 600px) {
    .comm-row {
        grid-template-columns: 1fr auto auto;
        grid-template-rows: auto auto;
    }
    .comm-name { grid-column: 1; grid-row: 1; }
    .comm-vals { grid-column: 1; grid-row: 2; font-size: var(--text-xs); }
    .comm-badge-placeholder,
    :deep(.count-badge) { grid-column: 2; grid-row: 1; }
    .btn-icon { grid-column: 3; grid-row: 1; }
}

/* Erro dentro do modal */
.modal-erro {
    font-size: var(--text-sm);
    color: hsl(var(--destructive));
    margin: 0;
}

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
</style>
