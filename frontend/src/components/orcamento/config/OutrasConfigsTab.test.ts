/**
 * Testes de OutrasConfigsTab.vue
 * Cobrem CAs: 1 (criar implante), 3 (excluir implante com confirm), 4 (equipe legado),
 *             5 (criar pagamento), 6 (editar/excluir pagamento), 7 (vínculo inventário),
 *             11/12 (sem placeholders/texto antigo), 13 (estado vazio aponta criar).
 */
import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"

// ─── Stubs do design system — sem referências externas (hoisting do vi.mock) ──
vi.mock("@/components/ui", () => {
    return {
        AppTabs: {
            props: ["modelValue", "abas", "variante"],
            emits: ["update:modelValue"],
            template: `<div><slot /></div>`,
        },
        AppButton: {
            props: ["icon", "size", "variant", "loading"],
            emits: ["click"],
            template: `<button @click="$emit('click')"><slot /></button>`,
        },
        AppEmptyState: {
            props: ["icone", "titulo", "descricao"],
            template: `<div data-test="empty-state"><slot name="acao" /></div>`,
        },
        AppDrawer: {
            props: ["aberto", "titulo", "largura"],
            emits: ["fechar"],
            template: `<div v-if="aberto" data-testid="drawer"><slot /><slot name="rodape" /></div>`,
        },
        AppField: {
            props: ["label", "required"],
            template: `<div><slot /></div>`,
        },
        AppInput: {
            props: ["modelValue", "type", "step", "min", "placeholder"],
            emits: ["update:modelValue"],
            template: `<input :value="modelValue" @input="$emit('update:modelValue', $event.target.value)" />`,
        },
        AppInputDecimal: {
            props: ["modelValue", "decimals", "placeholder"],
            emits: ["update:modelValue"],
            template: `<input :value="modelValue" @input="$emit('update:modelValue', $event.target.value)" />`,
        },
        AppSelect: {
            props: ["modelValue", "options", "placeholder", "disabled"],
            emits: ["update:modelValue"],
            template: `<select :value="modelValue" :disabled="disabled" @change="$emit('update:modelValue', $event.target.value)"><option v-for="o in options" :key="o.value" :value="o.value">{{ o.label }}</option></select>`,
        },
        AppStatusPill: {
            props: ["label", "variante"],
            template: `<span>{{ label }}</span>`,
        },
        AppToast: {
            props: ["mensagem", "variante"],
            emits: ["fechar"],
            template: `<div data-testid="toast">{{ mensagem }}</div>`,
        },
        AppConfirmDialog: {
            props: ["aberto", "titulo", "mensagem", "confirmarRotulo", "variante", "icone", "executando"],
            emits: ["update:aberto", "confirmar"],
            template: `<div v-if="aberto" data-testid="confirm-dialog"><button data-testid="btn-confirmar" @click="$emit('confirmar')">Confirmar</button></div>`,
        },
    }
})

// ─── Mocks dos services — factory puro sem referências externas ──────────────
vi.mock("@/services/orcamentoCatalogoService", () => ({
    orcamentoCatalogoService: {
        listarLocais: vi.fn().mockResolvedValue([]),
        listarImplantes: vi.fn().mockResolvedValue([
            { id: 1, estabelecimentoId: 10, descricao: "Tela 15x15", custoUnitario: 250, itemInventarioId: null, itemInventarioNome: null, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null },
        ]),
        listarEquipes: vi.fn().mockResolvedValue([
            { id: 2, estabelecimentoId: 10, descricao: "Equipe Alpha", valorPadrao: 1500, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null },
        ]),
        listarConfigPagamento: vi.fn().mockResolvedValue([
            { id: 3, estabelecimentoId: 10, formaPagamentoId: 1, formaPagamentoNome: "Cartão", acrescimoPercentual: 2, entradaPercentualPadrao: 20, taxaParcela: 1, parcelasMaximas: 12, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null },
        ]),
        salvarLocal: vi.fn().mockResolvedValue({}),
        criarImplante: vi.fn().mockResolvedValue({ id: 99 }),
        atualizarImplante: vi.fn().mockResolvedValue(undefined),
        removerImplante: vi.fn().mockResolvedValue(undefined),
        criarEquipe: vi.fn().mockResolvedValue({ id: 100 }),
        atualizarEquipe: vi.fn().mockResolvedValue(undefined),
        removerEquipe: vi.fn().mockResolvedValue(undefined),
        criarConfigPagamento: vi.fn().mockResolvedValue({ id: 101 }),
        atualizarConfigPagamento: vi.fn().mockResolvedValue(undefined),
        removerConfigPagamento: vi.fn().mockResolvedValue(undefined),
    },
}))

vi.mock("@/services/categoriaFinanceiraService", () => ({
    formaPagamentoService: {
        listar: vi.fn().mockResolvedValue([
            { id: 1, nome: "Cartão" },
            { id: 2, nome: "Dinheiro" },
        ]),
    },
}))

vi.mock("@/services/inventarioService", () => ({
    inventarioService: {
        listarItens: vi.fn().mockResolvedValue({ itens: [], total: 0, pagina: 1, tamanhoPagina: 500 }),
    },
}))

// ─── Imports após os mocks ───────────────────────────────────────────────────
import { orcamentoCatalogoService } from "@/services/orcamentoCatalogoService"
import OutrasConfigsTab from "./OutrasConfigsTab.vue"

// ─── Helper ──────────────────────────────────────────────────────────────────
// <script setup> não expõe variáveis internas no tipo do vm; cast para any para acesso direto.
type Vm = any

async function montar() {
    const w = mount(OutrasConfigsTab, { global: { stubs: { teleport: true } } })
    await flushPromises()
    return w
}

function vm(w: ReturnType<typeof mount>): Vm {
    return w.vm as Vm
}

// ─── Testes ──────────────────────────────────────────────────────────────────
describe("OutrasConfigsTab", () => {
    beforeEach(() => {
        vi.mocked(orcamentoCatalogoService.criarImplante).mockClear()
        vi.mocked(orcamentoCatalogoService.atualizarImplante).mockClear()
        vi.mocked(orcamentoCatalogoService.removerImplante).mockClear()
        vi.mocked(orcamentoCatalogoService.listarImplantes).mockClear().mockResolvedValue([
            { id: 1, estabelecimentoId: 10, descricao: "Tela 15x15", custoUnitario: 250, itemInventarioId: null, itemInventarioNome: null, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null },
        ])
        vi.mocked(orcamentoCatalogoService.criarEquipe).mockClear()
        vi.mocked(orcamentoCatalogoService.removerEquipe).mockClear()
        vi.mocked(orcamentoCatalogoService.listarEquipes).mockClear().mockResolvedValue([
            { id: 2, estabelecimentoId: 10, descricao: "Equipe Alpha", valorPadrao: 1500, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null },
        ])
        vi.mocked(orcamentoCatalogoService.criarConfigPagamento).mockClear()
        vi.mocked(orcamentoCatalogoService.atualizarConfigPagamento).mockClear()
        vi.mocked(orcamentoCatalogoService.removerConfigPagamento).mockClear()
        vi.mocked(orcamentoCatalogoService.listarConfigPagamento).mockClear().mockResolvedValue([
            { id: 3, estabelecimentoId: 10, formaPagamentoId: 1, formaPagamentoNome: "Cartão", acrescimoPercentual: 2, entradaPercentualPadrao: 20, taxaParcela: 1, parcelasMaximas: 12, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null },
        ])
    })

    describe("CA1 — criar implante (caminho feliz)", () => {
        it("chama criarImplante e recarrega a lista ao salvar", async () => {
            const w = await montar()
            const listarCalls = vi.mocked(orcamentoCatalogoService.listarImplantes).mock.calls.length

            vm(w).novoImplante()
            vm(w).formImplante.descricao = "Prótese total de quadril"
            vm(w).formImplante.custoUnitario = 3500
            vm(w).formImplante.itemInventarioIdStr = ""

            await vm(w).salvarImplante()
            await flushPromises()

            expect(orcamentoCatalogoService.criarImplante).toHaveBeenCalledWith({
                descricao: "Prótese total de quadril",
                custoUnitario: 3500,
                itemInventarioId: null,
            })
            expect(vi.mocked(orcamentoCatalogoService.listarImplantes).mock.calls.length).toBeGreaterThan(listarCalls)
        })

        it("exibe toast de erro e mantém drawer aberto quando descrição está vazia", async () => {
            const w = await montar()
            vm(w).novoImplante()
            vm(w).formImplante.descricao = "   "

            await vm(w).salvarImplante()

            expect(orcamentoCatalogoService.criarImplante).not.toHaveBeenCalled()
            expect(vm(w).toast?.variante).toBe("error")
            expect(vm(w).drawerImplante).toBe(true)
        })
    })

    describe("CA7 — vínculo opcional implante × inventário", () => {
        it("envia itemInventarioId=null quando campo está vazio", async () => {
            const w = await montar()
            vm(w).novoImplante()
            vm(w).formImplante.descricao = "Parafuso cortical"
            vm(w).formImplante.custoUnitario = 80
            vm(w).formImplante.itemInventarioIdStr = ""

            await vm(w).salvarImplante()
            await flushPromises()

            expect(orcamentoCatalogoService.criarImplante).toHaveBeenCalledWith(
                expect.objectContaining({ itemInventarioId: null }),
            )
        })

        it("envia itemInventarioId como número quando item selecionado", async () => {
            const w = await montar()
            vm(w).novoImplante()
            vm(w).formImplante.descricao = "Gancho de sutura"
            vm(w).formImplante.custoUnitario = 40
            vm(w).formImplante.itemInventarioIdStr = "7"

            await vm(w).salvarImplante()
            await flushPromises()

            expect(orcamentoCatalogoService.criarImplante).toHaveBeenCalledWith(
                expect.objectContaining({ itemInventarioId: 7 }),
            )
        })
    })

    describe("CA3 — excluir implante com confirmação", () => {
        it("pedirRemocaoImplante apenas abre o confirm sem excluir", async () => {
            const w = await montar()
            vm(w).pedirRemocaoImplante({ id: 1, estabelecimentoId: 10, descricao: "Tela", custoUnitario: 100, itemInventarioId: null, itemInventarioNome: null, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null })
            await w.vm.$nextTick()

            expect(vm(w).confirmImplante.aberto).toBe(true)
            expect(orcamentoCatalogoService.removerImplante).not.toHaveBeenCalled()
        })

        it("chama removerImplante e fecha confirm ao executar remoção", async () => {
            const w = await montar()
            const listarCalls = vi.mocked(orcamentoCatalogoService.listarImplantes).mock.calls.length

            vm(w).pedirRemocaoImplante({ id: 1, estabelecimentoId: 10, descricao: "Tela", custoUnitario: 100, itemInventarioId: null, itemInventarioNome: null, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null })
            await vm(w).executarRemocaoImplante()
            await flushPromises()

            expect(orcamentoCatalogoService.removerImplante).toHaveBeenCalledWith(1)
            expect(vi.mocked(orcamentoCatalogoService.listarImplantes).mock.calls.length).toBeGreaterThan(listarCalls)
            expect(vm(w).confirmImplante.aberto).toBe(false)
        })
    })

    describe("CA4 — CRUD equipe legado", () => {
        it("chama criarEquipe e recarrega ao salvar nova equipe", async () => {
            const w = await montar()
            const listarCalls = vi.mocked(orcamentoCatalogoService.listarEquipes).mock.calls.length

            vm(w).novaEquipe()
            vm(w).formEquipe.descricao = "Equipe Beta"
            vm(w).formEquipe.valorPadrao = 2000

            await vm(w).salvarEquipe()
            await flushPromises()

            expect(orcamentoCatalogoService.criarEquipe).toHaveBeenCalledWith({
                descricao: "Equipe Beta",
                valorPadrao: 2000,
            })
            expect(vi.mocked(orcamentoCatalogoService.listarEquipes).mock.calls.length).toBeGreaterThan(listarCalls)
        })

        it("chama removerEquipe ao executar remoção e fecha confirm", async () => {
            const w = await montar()
            vm(w).pedirRemocaoEquipe({ id: 2, estabelecimentoId: 10, descricao: "Alpha", valorPadrao: 1500, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null })
            await vm(w).executarRemocaoEquipe()
            await flushPromises()

            expect(orcamentoCatalogoService.removerEquipe).toHaveBeenCalledWith(2)
            expect(vm(w).confirmEquipe.aberto).toBe(false)
        })

        it("bloqueia salvar equipe sem descrição", async () => {
            const w = await montar()
            vm(w).novaEquipe()
            vm(w).formEquipe.descricao = ""

            await vm(w).salvarEquipe()

            expect(orcamentoCatalogoService.criarEquipe).not.toHaveBeenCalled()
            expect(vm(w).toast?.variante).toBe("error")
        })
    })

    describe("CA5 — criar configuração de pagamento", () => {
        it("chama criarConfigPagamento com payload correto", async () => {
            const w = await montar()
            const listarCalls = vi.mocked(orcamentoCatalogoService.listarConfigPagamento).mock.calls.length

            vm(w).novoPagamento()
            vm(w).formPagamento.formaPagamentoIdStr = "1"
            vm(w).formPagamento.acrescimoPercentual = 5
            vm(w).formPagamento.entradaPercentualPadrao = 30
            vm(w).formPagamento.taxaParcela = 2
            vm(w).formPagamento.parcelasMaximas = 10

            await vm(w).salvarPagamento()
            await flushPromises()

            expect(orcamentoCatalogoService.criarConfigPagamento).toHaveBeenCalledWith({
                formaPagamentoId: 1,
                acrescimoPercentual: 5,
                entradaPercentualPadrao: 30,
                taxaParcela: 2,
                parcelasMaximas: 10,
            })
            expect(vi.mocked(orcamentoCatalogoService.listarConfigPagamento).mock.calls.length).toBeGreaterThan(listarCalls)
        })

        it("bloqueia salvar pagamento sem forma de pagamento selecionada", async () => {
            const w = await montar()
            vm(w).novoPagamento()
            vm(w).formPagamento.formaPagamentoIdStr = ""

            await vm(w).salvarPagamento()

            expect(orcamentoCatalogoService.criarConfigPagamento).not.toHaveBeenCalled()
            expect(vm(w).toast?.variante).toBe("error")
        })
    })

    describe("CA6 — editar e excluir configuração de pagamento", () => {
        it("chama atualizarConfigPagamento (sem formaPagamentoId) ao editar", async () => {
            const w = await montar()
            vm(w).editarPagamento({ id: 3, estabelecimentoId: 10, formaPagamentoId: 1, formaPagamentoNome: "Cartão", acrescimoPercentual: 2, entradaPercentualPadrao: 20, taxaParcela: 1, parcelasMaximas: 12, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null })
            await w.vm.$nextTick()

            vm(w).formPagamento.taxaParcela = 3

            await vm(w).salvarPagamento()
            await flushPromises()

            expect(orcamentoCatalogoService.atualizarConfigPagamento).toHaveBeenCalledWith(3, {
                acrescimoPercentual: 2,
                entradaPercentualPadrao: 20,
                taxaParcela: 3,
                parcelasMaximas: 12,
            })
        })

        it("chama removerConfigPagamento ao executar remoção", async () => {
            const w = await montar()
            vm(w).pedirRemocaoPagamento({ id: 3, estabelecimentoId: 10, formaPagamentoId: 1, formaPagamentoNome: "Cartão", acrescimoPercentual: 2, entradaPercentualPadrao: 20, taxaParcela: 1, parcelasMaximas: 12, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null })
            await vm(w).executarRemocaoPagamento()
            await flushPromises()

            expect(orcamentoCatalogoService.removerConfigPagamento).toHaveBeenCalledWith(3)
            expect(vm(w).confirmPagamento.aberto).toBe(false)
        })
    })

    describe("CA6 — campo forma-de-pagamento desabilitado no drawer de edição", () => {
        it("ao editar pagamento, o AppSelect de forma de pagamento está desabilitado", async () => {
            const w = await montar()
            vm(w).editarPagamento({ id: 3, estabelecimentoId: 10, formaPagamentoId: 1, formaPagamentoNome: "Cartão", acrescimoPercentual: 2, entradaPercentualPadrao: 20, taxaParcela: 1, parcelasMaximas: 12, ativo: true, criadaEm: "2026-01-01T00:00:00Z", atualizadaEm: null })
            await w.vm.$nextTick()

            // O drawer de pagamento está aberto e idEditandoPagamento !== null
            expect(vm(w).drawerPagamento).toBe(true)
            expect(vm(w).idEditandoPagamento).toBe(3)

            // O select de forma de pagamento deve estar desabilitado
            const select = w.find("select")
            expect(select.element.disabled).toBe(true)
        })

        it("no modo criação, o AppSelect de forma de pagamento está habilitado", async () => {
            const w = await montar()
            vm(w).novoPagamento()
            await w.vm.$nextTick()

            // idEditandoPagamento === null no modo criação
            expect(vm(w).idEditandoPagamento).toBeNull()

            // O select de forma de pagamento deve estar habilitado
            const select = w.find("select")
            expect(select.element.disabled).toBe(false)
        })
    })

    describe("CA11 + CA12 — sem placeholders, sem texto formulário antigo", () => {
        it("não contém texto 'Importar planilha' ou 'Exportar' no componente de aba", async () => {
            const w = await montar()
            expect(w.html()).not.toContain("Importar planilha")
            expect(w.html()).not.toContain("Exportar")
        })

        it("não exibe texto 'formulário antigo' nas sub-seções editáveis", async () => {
            const w = await montar()
            const subAbas = ["implantes", "equipes", "pagamento"] as const
            for (const s of subAbas) {
                vm(w).subAba = s
                await w.vm.$nextTick()
                expect(w.html()).not.toContain("formulário antigo")
            }
        })
    })

    describe("CA13 — estado vazio aponta para criar", () => {
        it("exibe AppEmptyState quando implantes está vazio", async () => {
            vi.mocked(orcamentoCatalogoService.listarImplantes).mockResolvedValue([])
            const w = await montar()
            vm(w).subAba = "implantes"
            await w.vm.$nextTick()

            expect(w.find("[data-test='empty-state']").exists()).toBe(true)
        })

        it("exibe AppEmptyState quando equipes está vazio", async () => {
            vi.mocked(orcamentoCatalogoService.listarEquipes).mockResolvedValue([])
            const w = await montar()
            vm(w).subAba = "equipes"
            await w.vm.$nextTick()

            expect(w.find("[data-test='empty-state']").exists()).toBe(true)
        })

        it("exibe AppEmptyState quando pagamentos está vazio", async () => {
            vi.mocked(orcamentoCatalogoService.listarConfigPagamento).mockResolvedValue([])
            const w = await montar()
            vm(w).subAba = "pagamento"
            await w.vm.$nextTick()

            expect(w.find("[data-test='empty-state']").exists()).toBe(true)
        })
    })
})
