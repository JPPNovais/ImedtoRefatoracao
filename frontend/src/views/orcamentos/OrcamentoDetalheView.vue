<script setup lang="ts">
/**
 * OrcamentoDetalheView — visualização read-only do orçamento + ações de status.
 * As transições disponíveis dependem do status atual:
 *   - Rascunho → Enviar / Cancelar
 *   - Enviado  → Aprovar / Recusar / Cancelar
 *   - Aprovado → Cancelar / (Fase 6.4: Converter em cirurgia)
 *   - Recusado / Cancelado / Expirado → terminais (só Editar/PDF)
 */
import { ref, computed, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { orcamentoService, type Orcamento } from "@/services/orcamentoService"
import { useOrcamentoPdf } from "@/composables/useOrcamentoPdf"
import {
    AppPageHeader, AppButton, AppBadge, AppCard,
} from "@/components/ui"

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
    if (!orcamento.value) return
    if (acaoEmCurso.value) return
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

const podeEnviar  = computed(() => orcamento.value?.status === "Rascunho")
const podeAprovar = computed(() => orcamento.value?.status === "Enviado")
const podeRecusar = computed(() => orcamento.value?.status === "Enviado")
const podeCancelar = computed(() => orcamento.value && !["Recusado", "Cancelado", "Expirado"].includes(orcamento.value.status))
const podeEditar  = computed(() => orcamento.value && ["Rascunho", "Enviado"].includes(orcamento.value.status))
const podeConverter = computed(() =>
    orcamento.value
    && orcamento.value.status === "Aprovado"
    && !orcamento.value.procedimentoCirurgicoId
    && orcamento.value.cirurgias.length > 0
)

async function converter() {
    if (!orcamento.value) return
    if (!confirm("Converter este orçamento em uma cirurgia planejada?")) return
    await executarAcao(
        async () => { await orcamentoService.converterEmCirurgia(orcamentoId) },
        "Erro ao converter."
    )
}

async function baixarPdf() {
    if (orcamento.value) await gerarPdf(orcamento.value)
}

function fmtMoeda(v: number) { return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) }
function fmtData(s: string) { return new Date(s).toLocaleDateString("pt-BR") }

function editar() {
    router.push({ name: "OrcamentoForm", params: { id: String(orcamentoId) } })
}

function voltar() {
    router.push({ name: "Orcamentos" })
}

onMounted(carregar)
</script>

<template>
    <main class="app-page app-page--wide">
        <div v-if="carregando" class="estado">
            <i class="fa-solid fa-spinner fa-spin"></i> Carregando...
        </div>

        <div v-else-if="erro && !orcamento" class="erro-banner" role="alert">
            {{ erro }}
            <AppButton size="sm" variant="ghost" @click="carregar">Tentar novamente</AppButton>
        </div>

        <template v-else-if="orcamento">
            <AppPageHeader
                :titulo="`Orçamento ${orcamento.numero || `#${orcamento.id}`}`"
                :subtitulo="orcamento.pacienteNome"
            >
                <template #acoes>
                    <AppBadge :status="orcamento.status" />
                    <AppButton variant="ghost" icon="fa-solid fa-arrow-left" @click="voltar">Voltar</AppButton>
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
                    <AppButton
                        variant="ghost"
                        icon="fa-solid fa-file-pdf"
                        @click="baixarPdf"
                    >PDF</AppButton>
                </template>
            </AppPageHeader>

            <div v-if="erro" class="erro-banner">{{ erro }}</div>

            <div class="grid-detalhe">
                <div class="col-principal">
                    <AppCard title="Cabeçalho">
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
                                        class="link-cirurgia"
                                    >
                                        Procedimento #{{ orcamento.procedimentoCirurgicoId }}
                                        <i class="fa-solid fa-arrow-up-right-from-square"></i>
                                    </router-link>
                                </dd>
                            </div>
                        </dl>
                        <p v-if="orcamento.observacoes" class="observacoes">
                            <strong>Observações:</strong> {{ orcamento.observacoes }}
                        </p>
                    </AppCard>

                    <AppCard v-if="orcamento.cirurgias.length" title="Cirurgias">
                        <table class="tabela">
                            <thead>
                                <tr><th>Descrição</th><th>Qtd</th><th>Duração</th><th>Total</th></tr>
                            </thead>
                            <tbody>
                                <tr v-for="c in orcamento.cirurgias" :key="c.id">
                                    <td>{{ c.descricao }}</td>
                                    <td>{{ c.quantidade }}</td>
                                    <td>{{ c.duracaoMinutos ? `${c.duracaoMinutos} min` : "—" }}</td>
                                    <td>{{ fmtMoeda(c.valorTotal) }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </AppCard>

                    <AppCard v-if="orcamento.equipe.length" title="Equipe profissional">
                        <table class="tabela">
                            <thead>
                                <tr><th>Profissional</th><th>Função</th><th>Honorário</th></tr>
                            </thead>
                            <tbody>
                                <tr v-for="e in orcamento.equipe" :key="e.id">
                                    <td>{{ e.profissionalNome ?? e.profissionalUsuarioId }}</td>
                                    <td>{{ e.papel }}</td>
                                    <td>{{ fmtMoeda(e.valor) }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </AppCard>

                    <AppCard v-if="orcamento.implantes.length" title="Implantes">
                        <table class="tabela">
                            <thead>
                                <tr><th>Descrição</th><th>Qtd</th><th>Custo unit.</th><th>Total</th></tr>
                            </thead>
                            <tbody>
                                <tr v-for="imp in orcamento.implantes" :key="imp.id">
                                    <td>{{ imp.descricao }}</td>
                                    <td>{{ imp.quantidade }}</td>
                                    <td>{{ fmtMoeda(imp.custoUnitario) }}</td>
                                    <td>{{ fmtMoeda(imp.custoTotal) }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </AppCard>

                    <AppCard v-if="orcamento.internacao || orcamento.anestesia" title="Local & anestesia">
                        <dl class="meta-grid">
                            <div v-if="orcamento.internacao">
                                <dt>Internação</dt>
                                <dd>{{ orcamento.internacao.tipoInternacao }} ({{ orcamento.internacao.dias }}d × {{ fmtMoeda(orcamento.internacao.valorDiaria) }} = {{ fmtMoeda(orcamento.internacao.valorTotal) }})</dd>
                            </div>
                            <div v-if="orcamento.anestesia">
                                <dt>Anestesia</dt>
                                <dd>{{ orcamento.anestesia.tipoAnestesia }} — {{ fmtMoeda(orcamento.anestesia.valor) }}</dd>
                            </div>
                        </dl>
                    </AppCard>

                    <AppCard v-if="orcamento.formasPagamento.length" title="Formas de pagamento">
                        <table class="tabela">
                            <thead>
                                <tr>
                                    <th>Forma</th><th>Valor</th><th>Parcelas</th>
                                    <th>Acréscimo %</th><th>Entrada %</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="f in orcamento.formasPagamento" :key="f.id">
                                    <td>{{ f.formaPagamentoNome }}</td>
                                    <td>{{ fmtMoeda(f.valor) }}</td>
                                    <td>{{ f.parcelas }}x</td>
                                    <td>{{ f.acrescimoPercentual }}%</td>
                                    <td>{{ f.entradaPercentual }}%</td>
                                </tr>
                            </tbody>
                        </table>
                    </AppCard>
                </div>

                <aside class="col-resumo">
                    <AppCard title="Resumo" elevated>
                        <div class="resumo-linhas">
                            <div v-if="orcamento.cirurgias.length" class="linha">
                                <span>Cirurgias</span>
                                <strong>{{ fmtMoeda(orcamento.cirurgias.reduce((a, c) => a + c.valorTotal, 0)) }}</strong>
                            </div>
                            <div v-if="orcamento.equipe.length" class="linha">
                                <span>Honorários</span>
                                <strong>{{ fmtMoeda(orcamento.equipe.reduce((a, e) => a + e.valor, 0)) }}</strong>
                            </div>
                            <div v-if="orcamento.implantes.length" class="linha">
                                <span>Implantes</span>
                                <strong>{{ fmtMoeda(orcamento.custoImplantesTotal) }}</strong>
                            </div>
                            <div v-if="orcamento.internacao" class="linha">
                                <span>Internação</span>
                                <strong>{{ fmtMoeda(orcamento.internacao.valorTotal) }}</strong>
                            </div>
                            <div v-if="orcamento.anestesia" class="linha">
                                <span>Anestesia</span>
                                <strong>{{ fmtMoeda(orcamento.anestesia.valor) }}</strong>
                            </div>
                            <div v-if="orcamento.itens.length" class="linha">
                                <span>Itens avulsos</span>
                                <strong>{{ fmtMoeda(orcamento.itens.reduce((a, i) => a + i.subtotal, 0)) }}</strong>
                            </div>
                            <div class="linha total">
                                <span>Total</span>
                                <strong>{{ fmtMoeda(orcamento.total) }}</strong>
                            </div>
                        </div>
                    </AppCard>
                </aside>
            </div>
        </template>
    </main>
</template>

<style scoped>
.estado {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-muted);
    padding: 2rem 0;
}
.erro-banner {
    padding: 0.85rem 1rem;
    background: hsl(var(--destructive) / 0.08);
    border: 1px solid hsl(var(--destructive) / 0.2);
    border-radius: var(--radius);
    color: hsl(var(--destructive));
    margin-bottom: 1rem;
}
.grid-detalhe {
    display: grid;
    grid-template-columns: 1fr 280px;
    gap: 1.25rem;
    align-items: start;
}
@media (max-width: 1024px) {
    .grid-detalhe { grid-template-columns: 1fr; }
}
.col-principal {
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
}
.col-resumo { position: sticky; top: 80px; }

.meta-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 0.75rem;
}
.meta-grid dt { font-size: 0.72em; font-weight: 600; color: var(--text-muted); text-transform: uppercase; }
.link-cirurgia { color: hsl(var(--primary)); text-decoration: none; font-weight: 500; }
.link-cirurgia:hover { text-decoration: underline; }
.link-cirurgia i { font-size: 0.8em; margin-left: 0.25rem; }
.meta-grid dd { font-size: 0.92em; margin: 2px 0 0; }
.observacoes { margin-top: 0.85rem; font-size: 0.9em; }

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.88em;
}
.tabela th, .tabela td {
    padding: 0.5rem 0.7rem;
    text-align: left;
    border-bottom: 1px solid var(--border);
}
.tabela th {
    font-weight: 600;
    font-size: 0.78em;
    text-transform: uppercase;
    color: var(--text-muted);
}
.tabela tr:last-child td { border-bottom: none; }

.resumo-linhas { display: flex; flex-direction: column; gap: 0.45rem; }
.linha { display: flex; justify-content: space-between; font-size: 0.88em; }
.linha.total {
    border-top: 1px solid var(--border);
    padding-top: 0.45rem;
    margin-top: 0.25rem;
    font-size: 1em;
    font-weight: 700;
}
</style>
