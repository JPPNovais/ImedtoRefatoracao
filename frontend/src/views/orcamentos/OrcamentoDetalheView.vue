<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { orcamentoService, type Orcamento } from "@/services/orcamentoService"
import { useOrcamentoPdf } from "@/composables/useOrcamentoPdf"
import { AppButton, AppCard } from "@/components/ui"
import OrcamentoStatusPill from "@/components/orcamento/OrcamentoStatusPill.vue"

const { gerarPdf } = useOrcamentoPdf()

const route = useRoute()
const router = useRouter()
const orcamentoId = Number(route.params.id)

const orcamento = ref<Orcamento | null>(null)
const carregando = ref(false)
const erro = ref<string | null>(null)
const acaoEmCurso = ref(false)

async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        orcamento.value = await orcamentoService.obter(orcamentoId)
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Não foi possível carregar o orçamento."
    } finally {
        carregando.value = false
    }
}

async function executarAcao(fn: () => Promise<void>, msgErro: string) {
    if (!orcamento.value || acaoEmCurso.value) return
    acaoEmCurso.value = true
    erro.value = null
    try {
        await fn()
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? msgErro
    } finally {
        acaoEmCurso.value = false
    }
}

const podeEnviar   = computed(() => orcamento.value?.status === "Rascunho")
const podeAprovar  = computed(() => orcamento.value?.status === "Enviado")
const podeRecusar  = computed(() => orcamento.value?.status === "Enviado")
const podeCancelar = computed(() =>
    orcamento.value && !["Recusado", "Cancelado", "Expirado"].includes(orcamento.value.status)
)
const podeEditar   = computed(() =>
    orcamento.value && ["Rascunho", "Enviado"].includes(orcamento.value.status)
)
const podeConverter = computed(() =>
    orcamento.value &&
    orcamento.value.status === "Aprovado" &&
    !orcamento.value.procedimentoCirurgicoId &&
    orcamento.value.cirurgias.length > 0
)

async function converter() {
    if (!orcamento.value) return
    if (!confirm("Converter este orçamento em uma cirurgia planejada?")) return
    await executarAcao(
        () => orcamentoService.converterEmCirurgia(orcamentoId).then(() => {}),
        "Erro ao converter."
    )
}

async function baixarPdf() {
    if (orcamento.value) await gerarPdf(orcamento.value)
}

function fmtBRL(v: number) { return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) }
function fmtData(s: string) { return new Date(s + "T00:00:00").toLocaleDateString("pt-BR") }

function editar() {
    router.push({ name: "OrcamentoForm", params: { id: String(orcamentoId) } })
}

function voltar() { router.push({ name: "Orcamentos" }) }

onMounted(carregar)
</script>

<template>
    <div class="app-page app-page--wide">
        <!-- Loading -->
        <div v-if="carregando && !orcamento" class="estado-loading">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando orçamento...
        </div>

        <!-- Erro fatal -->
        <div v-else-if="erro && !orcamento" class="erro-banner" role="alert">
            <i class="fa-solid fa-circle-exclamation"></i>
            {{ erro }}
            <AppButton size="sm" variant="ghost" @click="carregar">Tentar novamente</AppButton>
        </div>

        <template v-else-if="orcamento">
            <!-- Header -->
            <div class="det-header">
                <div class="det-header-l">
                    <button type="button" class="btn-back" @click="voltar" aria-label="Voltar para lista">
                        <i class="fa-solid fa-arrow-left"></i>
                    </button>
                    <div>
                        <div class="det-crumb">Orçamentos / {{ orcamento.numero || `#${orcamento.id}` }}</div>
                        <h1 class="det-titulo">{{ orcamento.pacienteNome }}</h1>
                    </div>
                    <OrcamentoStatusPill :status="orcamento.status" />
                </div>
                <div class="det-header-r">
                    <AppButton variant="ghost" icon="fa-solid fa-file-pdf" @click="baixarPdf">PDF</AppButton>
                    <AppButton
                        v-if="podeEditar"
                        variant="secondary"
                        icon="fa-solid fa-pen"
                        @click="editar"
                    >Editar</AppButton>
                    <AppButton
                        v-if="podeEnviar"
                        icon="fa-solid fa-paper-plane"
                        :loading="acaoEmCurso"
                        @click="executarAcao(() => orcamentoService.enviar(orcamentoId), 'Erro ao enviar.')"
                    >Enviar</AppButton>
                    <AppButton
                        v-if="podeAprovar"
                        variant="success"
                        icon="fa-solid fa-check"
                        :loading="acaoEmCurso"
                        @click="executarAcao(() => orcamentoService.aprovar(orcamentoId), 'Erro ao aprovar.')"
                    >Aprovar</AppButton>
                    <AppButton
                        v-if="podeRecusar"
                        variant="ghost"
                        icon="fa-solid fa-xmark"
                        :loading="acaoEmCurso"
                        @click="executarAcao(() => orcamentoService.recusar(orcamentoId), 'Erro ao recusar.')"
                    >Recusar</AppButton>
                    <AppButton
                        v-if="podeCancelar"
                        variant="danger"
                        icon="fa-solid fa-ban"
                        :loading="acaoEmCurso"
                        @click="executarAcao(() => orcamentoService.cancelar(orcamentoId), 'Erro ao cancelar.')"
                    >Cancelar</AppButton>
                    <AppButton
                        v-if="podeConverter"
                        variant="success"
                        icon="fa-solid fa-scalpel"
                        :loading="acaoEmCurso"
                        @click="converter"
                    >Converter em cirurgia</AppButton>
                </div>
            </div>

            <!-- Erro de ação -->
            <div v-if="erro" class="erro-banner" role="alert">
                <i class="fa-solid fa-circle-exclamation"></i>
                {{ erro }}
            </div>

            <!-- Grid principal -->
            <div class="det-grid">
                <div class="det-main">
                    <!-- Cabeçalho do orçamento -->
                    <AppCard title="Paciente e responsável">
                        <dl class="meta-grid">
                            <div><dt>Paciente</dt><dd>{{ orcamento.pacienteNome }}</dd></div>
                            <div><dt>Número</dt><dd>{{ orcamento.numero || "—" }}</dd></div>
                            <div><dt>Validade</dt><dd>{{ fmtData(orcamento.validade) }}</dd></div>
                            <div><dt>Criado em</dt><dd>{{ fmtData(orcamento.criadoEm) }}</dd></div>
                            <div><dt>Criado por</dt><dd>{{ orcamento.criadoPorNome }}</dd></div>
                            <div v-if="orcamento.procedimentoCirurgicoId">
                                <dt>Cirurgia vinculada</dt>
                                <dd>
                                    <router-link
                                        :to="{ name: 'CirurgiaDetalhe', params: { pacienteId: String(orcamento.pacienteId), id: String(orcamento.procedimentoCirurgicoId) } }"
                                        class="link-interno"
                                    >
                                        Procedimento #{{ orcamento.procedimentoCirurgicoId }}
                                        <i class="fa-solid fa-arrow-up-right-from-square" aria-hidden="true"></i>
                                    </router-link>
                                </dd>
                            </div>
                        </dl>
                        <p v-if="orcamento.observacoes" class="obs-text">
                            <strong>Observações:</strong> {{ orcamento.observacoes }}
                        </p>
                    </AppCard>

                    <!-- Cirurgias -->
                    <AppCard v-if="orcamento.cirurgias.length" title="Cirurgias">
                        <table class="tabela">
                            <thead>
                                <tr><th>Descrição</th><th>Qtd</th><th>Duração</th><th class="r">Total</th></tr>
                            </thead>
                            <tbody>
                                <tr v-for="c in orcamento.cirurgias" :key="c.id">
                                    <td>{{ c.descricao }}</td>
                                    <td>{{ c.quantidade }}</td>
                                    <td>{{ c.duracaoMinutos ? `${c.duracaoMinutos} min` : "—" }}</td>
                                    <td class="r">{{ fmtBRL(c.valorTotal) }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </AppCard>

                    <!-- Equipe -->
                    <AppCard v-if="orcamento.equipe.length" title="Equipe profissional">
                        <table class="tabela">
                            <thead>
                                <tr><th>Profissional</th><th>Função</th><th class="r">Honorário</th></tr>
                            </thead>
                            <tbody>
                                <tr v-for="e in orcamento.equipe" :key="e.id">
                                    <td>{{ e.profissionalNome ?? e.profissionalUsuarioId }}</td>
                                    <td>{{ e.papel }}</td>
                                    <td class="r">{{ fmtBRL(e.valor) }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </AppCard>

                    <!-- Implantes -->
                    <AppCard v-if="orcamento.implantes.length" title="Implantes">
                        <table class="tabela">
                            <thead>
                                <tr><th>Descrição</th><th>Qtd</th><th class="r">Custo unit.</th><th class="r">Total</th></tr>
                            </thead>
                            <tbody>
                                <tr v-for="imp in orcamento.implantes" :key="imp.id">
                                    <td>{{ imp.descricao }}</td>
                                    <td>{{ imp.quantidade }}</td>
                                    <td class="r">{{ fmtBRL(imp.custoUnitario) }}</td>
                                    <td class="r">{{ fmtBRL(imp.custoTotal) }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </AppCard>

                    <!-- Local cirúrgico & anestesia -->
                    <AppCard v-if="orcamento.localCirurgia || orcamento.anestesia" title="Local cirúrgico e anestesia">
                        <dl class="meta-grid">
                            <div v-if="orcamento.localCirurgia">
                                <dt>Local cirúrgico</dt>
                                <dd>
                                    {{ orcamento.localCirurgia.tipo }}
                                    ({{ orcamento.localCirurgia.tempoMinutos }} min
                                    = {{ fmtBRL(orcamento.localCirurgia.valor) }})
                                </dd>
                            </div>
                            <div v-if="orcamento.anestesia">
                                <dt>Anestesia</dt>
                                <dd>{{ orcamento.anestesia.tipoAnestesia }} — {{ fmtBRL(orcamento.anestesia.valor) }}</dd>
                            </div>
                        </dl>
                    </AppCard>

                    <!-- Formas de pagamento -->
                    <AppCard v-if="orcamento.formasPagamento.length" title="Formas de pagamento">
                        <table class="tabela">
                            <thead>
                                <tr>
                                    <th>Forma</th><th class="r">Valor</th>
                                    <th class="r">Parcelas</th><th class="r">Acréscimo</th><th class="r">Entrada</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="f in orcamento.formasPagamento" :key="f.id">
                                    <td>{{ f.formaPagamentoNome }}</td>
                                    <td class="r">{{ fmtBRL(f.valor) }}</td>
                                    <td class="r">{{ f.parcelas }}x</td>
                                    <td class="r">{{ f.acrescimoPercentual }}%</td>
                                    <td class="r">{{ f.entradaPercentual }}%</td>
                                </tr>
                            </tbody>
                        </table>
                    </AppCard>
                </div>

                <!-- Sidebar: resumo financeiro -->
                <aside class="det-side">
                    <div class="resumo-card">
                        <div class="resumo-titulo">Resumo financeiro</div>
                        <div class="resumo-linhas">
                            <div v-if="orcamento.cirurgias.length" class="rl">
                                <span>Cirurgias</span>
                                <strong>{{ fmtBRL(orcamento.cirurgias.reduce((a, c) => a + c.valorTotal, 0)) }}</strong>
                            </div>
                            <div v-if="orcamento.equipe.length" class="rl">
                                <span>Honorários</span>
                                <strong>{{ fmtBRL(orcamento.equipe.reduce((a, e) => a + e.valor, 0)) }}</strong>
                            </div>
                            <div v-if="orcamento.implantes.length" class="rl">
                                <span>Implantes</span>
                                <strong>{{ fmtBRL(orcamento.custoImplantesTotal) }}</strong>
                            </div>
                            <div v-if="orcamento.localCirurgia" class="rl">
                                <span>Local cirúrgico</span>
                                <strong>{{ fmtBRL(orcamento.localCirurgia.valor) }}</strong>
                            </div>
                            <div v-if="orcamento.anestesia" class="rl">
                                <span>Anestesia</span>
                                <strong>{{ fmtBRL(orcamento.anestesia.valor) }}</strong>
                            </div>
                            <div v-if="orcamento.itens.length" class="rl">
                                <span>Itens avulsos</span>
                                <strong>{{ fmtBRL(orcamento.itens.reduce((a, i) => a + i.subtotal, 0)) }}</strong>
                            </div>
                            <div class="rl rl-total">
                                <span>Total</span>
                                <strong>{{ fmtBRL(orcamento.total) }}</strong>
                            </div>
                        </div>

                        <!-- Ações rápidas -->
                        <div class="resumo-acoes">
                            <AppButton
                                v-if="podeEnviar"
                                block
                                icon="fa-solid fa-paper-plane"
                                :loading="acaoEmCurso"
                                @click="executarAcao(() => orcamentoService.enviar(orcamentoId), 'Erro ao enviar.')"
                            >Enviar ao paciente</AppButton>
                            <AppButton
                                v-if="podeEditar"
                                block
                                variant="secondary"
                                icon="fa-solid fa-pen"
                                @click="editar"
                            >Editar orçamento</AppButton>
                        </div>
                    </div>
                </aside>
            </div>
        </template>
    </div>
</template>

<style scoped>
.estado-loading {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-muted);
    padding: 3rem 0;
    font-size: 0.9em;
}

.erro-banner {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    font-size: 0.875rem;
}

/* Header */
.det-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 16px;
    flex-wrap: wrap;
}
.det-header-l {
    display: flex;
    align-items: center;
    gap: 14px;
}
.det-header-r {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}

.btn-back {
    width: 40px;
    height: 40px;
    border-radius: 10px;
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.12);
    display: flex;
    align-items: center;
    justify-content: center;
    color: hsl(var(--secondary));
    cursor: pointer;
    flex-shrink: 0;
    font-size: 14px;
    transition: background 0.12s;
}
.btn-back:hover { background: hsl(var(--secondary) / 0.04); }

.det-crumb  { font-size: 11.5px; color: hsl(var(--secondary) / 0.55); margin-bottom: 2px; }
.det-titulo { font-size: 20px; font-weight: 700; color: hsl(var(--secondary)); margin: 0; }

/* Grid */
.det-grid {
    display: grid;
    grid-template-columns: 1fr 300px;
    gap: 22px;
    align-items: start;
}
@media (max-width: 1100px) {
    .det-grid { grid-template-columns: 1fr; }
}

.det-main { display: flex; flex-direction: column; gap: 16px; }
.det-side { position: sticky; top: 84px; }

/* Card icon */
.card-ico { color: hsl(var(--primary)); margin-right: 6px; }

/* Meta grid */
.meta-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
    gap: 0.75rem;
}
.meta-grid dt { font-size: 0.72em; font-weight: 700; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.04em; }
.meta-grid dd { font-size: 0.9em; margin: 2px 0 0; }

.obs-text { margin-top: 0.85rem; font-size: 0.88em; color: hsl(var(--secondary) / 0.85); }

.link-interno { color: hsl(var(--primary)); text-decoration: none; font-weight: 500; font-size: 0.9em; }
.link-interno:hover { text-decoration: underline; }
.link-interno i { font-size: 0.75em; margin-left: 4px; }

/* Tabela */
.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.875em;
}
.tabela th, .tabela td {
    padding: 0.5rem 0.75rem;
    text-align: left;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.tabela th {
    font-size: 0.75em;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: var(--text-muted);
    background: hsl(var(--secondary) / 0.03);
}
.tabela tr:last-child td { border-bottom: none; }
.tabela .r { text-align: right; }

/* Resumo sidebar */
.resumo-card {
    background: hsl(var(--card));
    border: 1px solid hsl(var(--secondary) / 0.08);
    border-radius: 12px;
    padding: 18px;
}
.resumo-titulo {
    font-size: 13px;
    font-weight: 600;
    color: hsl(var(--secondary));
    padding-bottom: 12px;
    margin-bottom: 12px;
    border-bottom: 1px solid hsl(var(--secondary) / 0.06);
}
.resumo-linhas { display: flex; flex-direction: column; gap: 8px; }
.rl {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    font-size: 13px;
    color: hsl(var(--secondary));
}
.rl-total {
    border-top: 1px solid hsl(var(--secondary) / 0.08);
    padding-top: 8px;
    margin-top: 4px;
}
.rl-total span { color: hsl(var(--secondary) / 0.7); }
.rl-total strong { font-size: 22px; font-weight: 700; color: hsl(var(--primary)); }

.resumo-acoes {
    margin-top: 16px;
    display: flex;
    flex-direction: column;
    gap: 8px;
}
</style>
