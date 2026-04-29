<script setup lang="ts">
/**
 * OrcamentoCompletoView — single-page com seções colapsáveis.
 * Conforme ADR 03A: seções colapsáveis em 2 colunas desktop,
 * resumo sticky com indicador de integridade (soma formas vs. total).
 */
import { ref, computed, onMounted, watch } from "vue"
import { useRoute } from "vue-router"
import {
    orcamentoCompletoService,
    type OrcamentoCompleto,
    type OrcamentoEquipe,
    type OrcamentoImplante,
    type OrcamentoFormaPagamento,
} from "@/services/orcamentoCompletoService"
import {
    AppPageHeader, AppButton, AppBadge, AppCollapsible,
    AppField, AppInput, AppTextarea, AppSelect, AppModal, AppCard,
} from "@/components/ui"
import { formaPagamentoService, type FormaPagamento } from "@/services/categoriaFinanceiraService"

const route = useRoute()
const orcamentoId = Number(route.params.id)

const orcamento = ref<OrcamentoCompleto | null>(null)
const carregando = ref(false)
const erro = ref<string | null>(null)
const salvando = ref(false)
const formasPagamentoDisponiveis = ref<FormaPagamento[]>([])

// ── Equipe ───────────────────────────────────────────────────────────────────
const equipe = ref<OrcamentoEquipe[]>([])
const novoMembro = ref<Omit<OrcamentoEquipe, "id">>({
    profissionalUsuarioId: "",
    papel: "Auxiliar",
    valor: 0,
})
const adicionandoMembro = ref(false)

// ── Implantes ────────────────────────────────────────────────────────────────
const implantes = ref<OrcamentoImplante[]>([])
const novoImplante = ref<Omit<OrcamentoImplante, "id">>({
    descricao: "",
    quantidade: 1,
    custoUnitario: 0,
    custoTotal: 0,
})
const adicionandoImplante = ref(false)

watch(
    () => [novoImplante.value.quantidade, novoImplante.value.custoUnitario],
    ([qtd, cu]) => {
        novoImplante.value.custoTotal = Number(qtd) * Number(cu)
    },
)

// ── Formas de pagamento ───────────────────────────────────────────────────────
const formas = ref<OrcamentoFormaPagamento[]>([])
const novaForma = ref<Omit<OrcamentoFormaPagamento, "id">>({
    formaPagamentoId: 0,
    valor: 0,
    parcelas: 1,
})
const adicionandoForma = ref(false)

// ── Integridade ───────────────────────────────────────────────────────────────
const totalFormas = computed(() =>
    formas.value.reduce((acc, f) => acc + Number(f.valor), 0),
)

const totalOrcamento = computed(() => orcamento.value?.totalLiquido ?? 0)

const diferenca = computed(() => totalOrcamento.value - totalFormas.value)

const integridadeOk = computed(() => Math.abs(diferenca.value) < 0.01)

function formatarMoeda(v: number) {
    return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
}

// ── Carregamento ─────────────────────────────────────────────────────────────
async function carregar() {
    carregando.value = true
    erro.value = null
    try {
        const [orcamentoData, formasData] = await Promise.all([
            orcamentoCompletoService.obter(orcamentoId),
            formaPagamentoService.listar(),
        ])
        orcamento.value = orcamentoData
        equipe.value = [...orcamentoData.equipe]
        implantes.value = [...orcamentoData.implantes]
        formas.value = [...orcamentoData.formasPagamento]
        formasPagamentoDisponiveis.value = formasData
    } catch {
        erro.value = "Nao foi possivel carregar o orcamento."
    } finally {
        carregando.value = false
    }
}

// ── Equipe CRUD ───────────────────────────────────────────────────────────────
function adicionarMembro() {
    if (!novoMembro.value.profissionalUsuarioId || novoMembro.value.valor < 0) return
    equipe.value.push({ ...novoMembro.value, ordem: equipe.value.length })
    novoMembro.value = { profissionalUsuarioId: "", papel: "Auxiliar", valor: 0 }
    adicionandoMembro.value = false
}
function removerMembro(idx: number) { equipe.value.splice(idx, 1) }

// ── Implantes CRUD ────────────────────────────────────────────────────────────
function adicionarImplante() {
    if (!novoImplante.value.descricao || novoImplante.value.quantidade <= 0) return
    implantes.value.push({ ...novoImplante.value })
    novoImplante.value = { descricao: "", quantidade: 1, custoUnitario: 0, custoTotal: 0 }
    adicionandoImplante.value = false
}
function removerImplante(idx: number) { implantes.value.splice(idx, 1) }

// ── Formas CRUD ───────────────────────────────────────────────────────────────
function adicionarForma() {
    if (!novaForma.value.formaPagamentoId || novaForma.value.valor <= 0) return
    const fp = formasPagamentoDisponiveis.value.find(f => f.id === novaForma.value.formaPagamentoId)
    formas.value.push({
        ...novaForma.value,
        formaPagamentoNome: fp?.nome,
        ordem: formas.value.length,
    })
    novaForma.value = { formaPagamentoId: 0, valor: 0, parcelas: 1 }
    adicionandoForma.value = false
}
function removerForma(idx: number) { formas.value.splice(idx, 1) }

// ── Salvar ───────────────────────────────────────────────────────────────────
async function salvar() {
    if (!orcamento.value) return
    if (!integridadeOk.value) {
        erro.value = "A soma das formas de pagamento deve ser igual ao total do orcamento."
        return
    }
    salvando.value = true
    erro.value = null
    try {
        await orcamentoCompletoService.atualizar(orcamentoId, {
            pacienteId: orcamento.value.pacienteId,
            tipo: orcamento.value.tipo,
            equipe: equipe.value,
            implantes: implantes.value,
            formasPagamento: formas.value,
            itens: [],
        })
        await carregar()
    } catch (e: any) {
        erro.value = e?.response?.data?.mensagem ?? "Erro ao salvar."
    } finally {
        salvando.value = false
    }
}

onMounted(carregar)
</script>

<template>
    <main class="app-page app-page--wide">
        <div v-if="carregando" class="carregando">
            <i class="fa-solid fa-spinner fa-spin" aria-hidden="true"></i>
            Carregando orcamento...
        </div>

        <div v-else-if="erro && !orcamento" class="erro-banner" role="alert">
            {{ erro }}
            <AppButton variant="ghost" size="sm" @click="carregar">Tentar novamente</AppButton>
        </div>

        <template v-else-if="orcamento">
            <AppPageHeader
                :titulo="`Orcamento #${orcamentoId}`"
                :subtitulo="orcamento.pacienteNome"
            >
                <template #acoes>
                    <AppBadge :status="orcamento.status" />
                    <AppButton
                        icon="fa-solid fa-save"
                        :loading="salvando"
                        :disabled="!integridadeOk"
                        :title="integridadeOk ? '' : 'Corrija a integridade das formas de pagamento'"
                        @click="salvar"
                    >
                        Salvar alteracoes
                    </AppButton>
                </template>
            </AppPageHeader>

            <div v-if="erro" class="erro-banner" role="alert">{{ erro }}</div>

            <div class="layout-orcamento">
                <!-- Coluna esquerda: seções colapsáveis -->
                <div class="secoes">
                    <!-- Equipe -->
                    <AppCollapsible
                        titulo="Equipe cirurgica"
                        subtitulo="Profissionais envolvidos no procedimento"
                        icone="fa-solid fa-users"
                    >
                        <template #header-aside>
                            <span class="badge-count">{{ equipe.length }}</span>
                        </template>

                        <div class="secao-conteudo">
                            <table v-if="equipe.length > 0" class="tabela">
                                <thead>
                                    <tr>
                                        <th>Profissional</th>
                                        <th>Papel</th>
                                        <th>Comissao</th>
                                        <th></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(m, idx) in equipe" :key="idx">
                                        <td>{{ m.profissionalNome ?? m.profissionalUsuarioId }}</td>
                                        <td>{{ m.papel }}</td>
                                        <td>{{ formatarMoeda(m.valor) }}</td>
                                        <td>
                                            <button class="btn-icon btn-icon-excluir" title="Remover" @click="removerMembro(idx)">
                                                <i class="fa-solid fa-trash" aria-hidden="true"></i>
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            <p v-else class="vazio-msg">Nenhum membro de equipe adicionado.</p>

                            <div v-if="adicionandoMembro" class="form-inline">
                                <AppField label="ID do profissional" for="mem-id">
                                    <AppInput id="mem-id" v-model="novoMembro.profissionalUsuarioId" placeholder="UUID do profissional" />
                                </AppField>
                                <AppField label="Papel" for="mem-papel">
                                    <AppSelect id="mem-papel" v-model="novoMembro.papel">
                                        <option value="Cirurgiao">Cirurgiao</option>
                                        <option value="Auxiliar">Auxiliar</option>
                                        <option value="Anestesista">Anestesista</option>
                                        <option value="Instrumentador">Instrumentador</option>
                                        <option value="Circulante">Circulante</option>
                                    </AppSelect>
                                </AppField>
                                <AppField label="Comissao (R$)" for="mem-valor">
                                    <AppInput id="mem-valor" v-model="novoMembro.valor" type="number" :min="0" :step="0.01" />
                                </AppField>
                                <div class="form-inline-acoes">
                                    <AppButton variant="ghost" size="sm" @click="adicionandoMembro = false">Cancelar</AppButton>
                                    <AppButton size="sm" @click="adicionarMembro">Adicionar</AppButton>
                                </div>
                            </div>
                            <AppButton
                                v-else
                                variant="ghost"
                                size="sm"
                                icon="fa-solid fa-plus"
                                @click="adicionandoMembro = true"
                            >
                                Adicionar membro
                            </AppButton>
                        </div>
                    </AppCollapsible>

                    <!-- Implantes -->
                    <AppCollapsible
                        titulo="Implantes"
                        subtitulo="Materiais e implantes utilizados"
                        icone="fa-solid fa-microchip"
                    >
                        <template #header-aside>
                            <span class="badge-count">{{ implantes.length }}</span>
                        </template>

                        <div class="secao-conteudo">
                            <table v-if="implantes.length > 0" class="tabela">
                                <thead>
                                    <tr>
                                        <th>Descricao</th>
                                        <th>Qtd</th>
                                        <th>Custo unit.</th>
                                        <th>Total</th>
                                        <th></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(imp, idx) in implantes" :key="idx">
                                        <td>{{ imp.descricao }}</td>
                                        <td>{{ imp.quantidade }}</td>
                                        <td>{{ formatarMoeda(imp.custoUnitario) }}</td>
                                        <td>{{ formatarMoeda(imp.custoTotal) }}</td>
                                        <td>
                                            <button class="btn-icon btn-icon-excluir" title="Remover" @click="removerImplante(idx)">
                                                <i class="fa-solid fa-trash" aria-hidden="true"></i>
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            <p v-else class="vazio-msg">Nenhum implante adicionado.</p>

                            <div v-if="adicionandoImplante" class="form-inline">
                                <AppField label="Descricao" for="imp-desc" class="col-span-2">
                                    <AppInput id="imp-desc" v-model="novoImplante.descricao" placeholder="Nome do implante" />
                                </AppField>
                                <AppField label="Quantidade" for="imp-qtd">
                                    <AppInput id="imp-qtd" v-model="novoImplante.quantidade" type="number" :min="1" :step="0.001" />
                                </AppField>
                                <AppField label="Custo unitario" for="imp-cu">
                                    <AppInput id="imp-cu" v-model="novoImplante.custoUnitario" type="number" :min="0" :step="0.01" />
                                </AppField>
                                <AppField label="Total" for="imp-total">
                                    <AppInput id="imp-total" :model-value="novoImplante.custoTotal" readonly />
                                </AppField>
                                <div class="form-inline-acoes col-span-2">
                                    <AppButton variant="ghost" size="sm" @click="adicionandoImplante = false">Cancelar</AppButton>
                                    <AppButton size="sm" @click="adicionarImplante">Adicionar</AppButton>
                                </div>
                            </div>
                            <AppButton
                                v-else
                                variant="ghost"
                                size="sm"
                                icon="fa-solid fa-plus"
                                @click="adicionandoImplante = true"
                            >
                                Adicionar implante
                            </AppButton>
                        </div>
                    </AppCollapsible>

                    <!-- Formas de pagamento -->
                    <AppCollapsible
                        titulo="Formas de pagamento"
                        subtitulo="Como o orcamento sera pago"
                        icone="fa-solid fa-credit-card"
                    >
                        <template #header-aside>
                            <span
                                class="badge-integridade"
                                :class="integridadeOk ? 'badge-integridade--ok' : 'badge-integridade--erro'"
                            >
                                {{ integridadeOk ? "Integro" : "Diferenca: " + formatarMoeda(diferenca) }}
                            </span>
                        </template>

                        <div class="secao-conteudo">
                            <table v-if="formas.length > 0" class="tabela">
                                <thead>
                                    <tr>
                                        <th>Forma</th>
                                        <th>Valor</th>
                                        <th>Parcelas</th>
                                        <th></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(f, idx) in formas" :key="idx">
                                        <td>{{ f.formaPagamentoNome ?? `Forma ${f.formaPagamentoId}` }}</td>
                                        <td>{{ formatarMoeda(f.valor) }}</td>
                                        <td>{{ f.parcelas }}x</td>
                                        <td>
                                            <button class="btn-icon btn-icon-excluir" title="Remover" @click="removerForma(idx)">
                                                <i class="fa-solid fa-trash" aria-hidden="true"></i>
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                            <p v-else class="vazio-msg">Nenhuma forma de pagamento adicionada.</p>

                            <div v-if="adicionandoForma" class="form-inline">
                                <AppField label="Forma de pagamento" for="fp-id">
                                    <AppSelect id="fp-id" v-model="novaForma.formaPagamentoId">
                                        <option :value="0">Selecione...</option>
                                        <option v-for="fp in formasPagamentoDisponiveis" :key="fp.id" :value="fp.id">
                                            {{ fp.nome }}
                                        </option>
                                    </AppSelect>
                                </AppField>
                                <AppField label="Valor" for="fp-val">
                                    <AppInput id="fp-val" v-model="novaForma.valor" type="number" :min="0.01" :step="0.01" />
                                </AppField>
                                <AppField label="Parcelas" for="fp-parc">
                                    <AppInput id="fp-parc" v-model="novaForma.parcelas" type="number" :min="1" />
                                </AppField>
                                <div class="form-inline-acoes">
                                    <AppButton variant="ghost" size="sm" @click="adicionandoForma = false">Cancelar</AppButton>
                                    <AppButton size="sm" @click="adicionarForma">Adicionar</AppButton>
                                </div>
                            </div>
                            <AppButton
                                v-else
                                variant="ghost"
                                size="sm"
                                icon="fa-solid fa-plus"
                                @click="adicionandoForma = true"
                            >
                                Adicionar forma
                            </AppButton>
                        </div>
                    </AppCollapsible>
                </div>

                <!-- Coluna direita: resumo sticky -->
                <aside class="resumo-sticky">
                    <AppCard title="Resumo financeiro" elevated>
                        <div class="resumo-linhas">
                            <div class="resumo-linha">
                                <span>Total bruto</span>
                                <strong>{{ formatarMoeda(orcamento.totalBruto) }}</strong>
                            </div>
                            <div v-if="orcamento.desconto > 0" class="resumo-linha resumo-linha--desconto">
                                <span>Desconto</span>
                                <strong>-{{ formatarMoeda(orcamento.desconto) }}</strong>
                            </div>
                            <div v-if="implantes.length > 0" class="resumo-linha">
                                <span>Implantes</span>
                                <strong>{{ formatarMoeda(implantes.reduce((a, i) => a + i.custoTotal, 0)) }}</strong>
                            </div>
                            <div class="resumo-linha resumo-linha--total">
                                <span>Total</span>
                                <strong>{{ formatarMoeda(totalOrcamento) }}</strong>
                            </div>
                            <div class="resumo-divider"></div>
                            <div class="resumo-linha">
                                <span>Formas (soma)</span>
                                <strong>{{ formatarMoeda(totalFormas) }}</strong>
                            </div>
                            <div
                                class="resumo-integridade"
                                :class="integridadeOk ? 'resumo-integridade--ok' : 'resumo-integridade--erro'"
                            >
                                <i
                                    :class="integridadeOk ? 'fa-solid fa-circle-check' : 'fa-solid fa-circle-exclamation'"
                                    aria-hidden="true"
                                ></i>
                                <span v-if="integridadeOk">Soma das formas confere.</span>
                                <span v-else>Falta {{ formatarMoeda(diferenca) }} para fechar.</span>
                            </div>
                        </div>

                        <template #footer>
                            <AppButton
                                block
                                icon="fa-solid fa-save"
                                :loading="salvando"
                                :disabled="!integridadeOk"
                                @click="salvar"
                            >
                                Salvar alteracoes
                            </AppButton>
                        </template>
                    </AppCard>
                </aside>
            </div>
        </template>
    </main>
</template>

<style scoped>
.carregando {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: hsl(var(--muted-foreground));
    padding: 2rem 0;
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
    font-size: 0.9em;
    margin-bottom: 1rem;
}

.layout-orcamento {
    display: grid;
    grid-template-columns: 1fr 280px;
    gap: 1.25rem;
    align-items: start;
}

.secoes {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.resumo-sticky {
    position: sticky;
    top: 80px;
}

.secao-conteudo {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    padding-top: 0.85rem;
}

.vazio-msg {
    font-size: 0.85em;
    color: hsl(var(--muted-foreground));
    padding: 0.75rem;
    text-align: center;
    background: hsl(var(--muted) / 0.4);
    border-radius: var(--radius-sm);
    margin: 0;
}

.tabela {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.88em;
}
.tabela th, .tabela td {
    padding: 0.55rem 0.7rem;
    text-align: left;
    border-bottom: 1px solid hsl(var(--border));
}
.tabela th {
    font-weight: 600;
    color: hsl(var(--muted-foreground));
    font-size: 0.78em;
    text-transform: uppercase;
    letter-spacing: 0.03em;
}

.form-inline {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.65rem;
    padding: 0.85rem;
    background: hsl(var(--accent) / 0.5);
    border-radius: var(--radius-sm);
    border: 1px solid hsl(var(--primary) / 0.2);
}
.col-span-2 { grid-column: span 2; }
.form-inline-acoes {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
}

.badge-count {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 20px;
    height: 20px;
    padding: 0 5px;
    border-radius: 999px;
    background: hsl(var(--muted));
    color: hsl(var(--muted-foreground));
    font-size: 0.7em;
    font-weight: 700;
}

.badge-integridade {
    font-size: 0.75em;
    font-weight: 600;
    padding: 0.15rem 0.5rem;
    border-radius: 999px;
}
.badge-integridade--ok {
    background: hsl(var(--success) / 0.15);
    color: hsl(var(--success));
}
.badge-integridade--erro {
    background: hsl(var(--warning) / 0.15);
    color: hsl(var(--warning));
}

/* Resumo lateral */
.resumo-linhas {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}
.resumo-linha {
    display: flex;
    justify-content: space-between;
    font-size: 0.88em;
}
.resumo-linha--desconto { color: hsl(var(--success)); }
.resumo-linha--total {
    font-size: 1em;
    font-weight: 700;
    padding-top: 0.25rem;
    border-top: 1px solid hsl(var(--border));
}
.resumo-divider {
    height: 1px;
    background: hsl(var(--border));
    margin: 0.25rem 0;
}
.resumo-integridade {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.82em;
    font-weight: 600;
    padding: 0.5rem;
    border-radius: var(--radius-sm);
}
.resumo-integridade--ok {
    color: hsl(var(--success));
    background: hsl(var(--success) / 0.1);
}
.resumo-integridade--erro {
    color: hsl(var(--warning));
    background: hsl(var(--warning) / 0.1);
}

@media (max-width: 1024px) {
    .layout-orcamento {
        grid-template-columns: 1fr;
    }
    .resumo-sticky {
        position: static;
    }
}
</style>
