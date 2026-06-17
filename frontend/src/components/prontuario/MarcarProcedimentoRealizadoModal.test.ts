import { describe, it, expect, vi, afterEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import MarcarProcedimentoRealizadoModal from "./MarcarProcedimentoRealizadoModal.vue"
import { pendenciaService } from "@/services/pendenciaService"

// ── Mocks ──────────────────────────────────────────────────────────────────────

vi.mock("@/services/pendenciaService", () => ({
    pendenciaService: {
        previewProcedimentoRealizado: vi.fn(),
        marcarProcedimentoRealizado: vi.fn(),
    },
}))

// Stubs: renderiza os slots sem comportamento de modal real
const globalStubs = {
    AppModal: {
        template: `
            <div v-if="aberto" class="stub-modal">
                <slot />
                <slot name="rodape" />
            </div>
        `,
        props: ["aberto", "largura"],
        emits: ["fechar"],
    },
    AppButton: {
        template: `<button :disabled="disabled || loading" @click="$emit('click')"><slot /></button>`,
        props: ["variant", "disabled", "loading"],
        emits: ["click"],
    },
}

// ── Fixtures ───────────────────────────────────────────────────────────────────

const previewSimples = {
    pendenciaId: 1,
    evolucaoId: 500,
    procedimentos: [
        { catalogoCirurgiaId: 10, descricao: "Cirurgia A", valor: 1500, observacao: null },
        { catalogoCirurgiaId: 20, descricao: "Cirurgia B", valor: 800, observacao: null },
    ],
    valorTotal: 2300,
    produtosABaixar: [],
    temProdutoSemVinculo: false,
}

const previewComSemVinculo = {
    ...previewSimples,
    produtosABaixar: [
        {
            produtoId: 1, produtoNome: "Prótese A", quantidade: 1,
            itemInventarioId: 42, itemInventarioNome: "ITEM-01 — Prótese Mamária", semVinculo: false,
        },
        {
            produtoId: 2, produtoNome: "Curativo X", quantidade: 2,
            itemInventarioId: null, itemInventarioNome: null, semVinculo: true,
        },
    ],
    temProdutoSemVinculo: true,
}

// ── Helpers ────────────────────────────────────────────────────────────────────

function montar(props = { aberto: true, pacienteId: 1, pendenciaId: 1 }) {
    return mount(MarcarProcedimentoRealizadoModal, {
        props,
        global: { stubs: globalStubs },
    })
}

// ── Testes ─────────────────────────────────────────────────────────────────────

describe("MarcarProcedimentoRealizadoModal", () => {
    afterEach(() => { vi.clearAllMocks() })

    // ── Renderização condicional ──────────────────────────────────────────────

    it("não renderiza conteúdo quando fechado", () => {
        const wrapper = montar({ aberto: false, pacienteId: 1, pendenciaId: 1 })
        expect(wrapper.find(".stub-modal").exists()).toBe(false)
    })

    it("exibe estado de loading ao abrir (antes de receber preview)", async () => {
        vi.mocked(pendenciaService.previewProcedimentoRealizado).mockReturnValue(new Promise(() => {}))
        const wrapper = montar()
        await flushPromises()
        expect(wrapper.text()).toContain("Carregando preview")
    })

    // ── Preview renderizado (CA88) ────────────────────────────────────────────

    it("exibe procedimentos e valor total após carregar preview (CA88)", async () => {
        vi.mocked(pendenciaService.previewProcedimentoRealizado).mockResolvedValue(previewSimples)
        const wrapper = montar()
        await flushPromises()

        expect(wrapper.text()).toContain("Cirurgia A")
        expect(wrapper.text()).toContain("Cirurgia B")
        expect(wrapper.text()).toContain("2.300")
    })

    it("exibe aviso e tag 'sem estoque vinculado' quando produto sem vínculo (CA94)", async () => {
        vi.mocked(pendenciaService.previewProcedimentoRealizado).mockResolvedValue(previewComSemVinculo)
        const wrapper = montar()
        await flushPromises()

        expect(wrapper.text()).toContain("não serão baixados automaticamente")
        expect(wrapper.text()).toContain("Curativo X")
        expect(wrapper.text()).toContain("Sem estoque vinculado")
    })

    it("não exibe aviso quando todos os produtos têm vínculo de estoque", async () => {
        const previewSemAviso = {
            ...previewComSemVinculo,
            produtosABaixar: [previewComSemVinculo.produtosABaixar[0]],
            temProdutoSemVinculo: false,
        }
        vi.mocked(pendenciaService.previewProcedimentoRealizado).mockResolvedValue(previewSemAviso)
        const wrapper = montar()
        await flushPromises()

        expect(wrapper.text()).not.toContain("não serão baixados automaticamente")
    })

    // ── Erro de preview ───────────────────────────────────────────────────────

    it("exibe mensagem de erro quando preview falha", async () => {
        vi.mocked(pendenciaService.previewProcedimentoRealizado).mockRejectedValue({
            response: { data: { mensagem: "Não encontrado." } },
        })
        const wrapper = montar()
        await flushPromises()

        expect(wrapper.text()).toContain("Não encontrado.")
    })

    // ── Confirmação (CA76/D1) ─────────────────────────────────────────────────

    it("emite 'concluido' com cobrancaId após confirmação bem-sucedida", async () => {
        vi.mocked(pendenciaService.previewProcedimentoRealizado).mockResolvedValue(previewSimples)
        vi.mocked(pendenciaService.marcarProcedimentoRealizado).mockResolvedValue({ cobrancaId: 77 })

        const wrapper = montar()
        await flushPromises()

        // Clica no botão Confirmar via wrapper
        const botoes = wrapper.findAll("button")
        const confirmar = botoes.find(b => b.text().includes("Confirmar"))
        expect(confirmar).toBeTruthy()
        await confirmar!.trigger("click")
        await flushPromises()

        expect(wrapper.emitted("concluido")).toBeTruthy()
        expect(wrapper.emitted("concluido")![0]).toEqual([77])
    })

    it("exibe erro de 422 ao falhar na confirmação e NÃO emite 'concluido'", async () => {
        vi.mocked(pendenciaService.previewProcedimentoRealizado).mockResolvedValue(previewSimples)
        vi.mocked(pendenciaService.marcarProcedimentoRealizado).mockRejectedValue({
            response: { data: { mensagem: "Estoque insuficiente." } },
        })

        const wrapper = montar()
        await flushPromises()

        const botoes = wrapper.findAll("button")
        const confirmar = botoes.find(b => b.text().includes("Confirmar"))
        await confirmar!.trigger("click")
        await flushPromises()

        expect(wrapper.text()).toContain("Estoque insuficiente.")
        expect(wrapper.emitted("concluido")).toBeFalsy()
    })

    // ── Fechar ────────────────────────────────────────────────────────────────

    it("emite 'fechar' ao clicar em Cancelar", async () => {
        vi.mocked(pendenciaService.previewProcedimentoRealizado).mockResolvedValue(previewSimples)
        const wrapper = montar()
        await flushPromises()

        const botoes = wrapper.findAll("button")
        const cancelar = botoes.find(b => b.text().includes("Cancelar"))
        expect(cancelar).toBeTruthy()
        await cancelar!.trigger("click")

        expect(wrapper.emitted("fechar")).toBeTruthy()
    })
})
