import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import { createRouter, createMemoryHistory } from "vue-router"

import OrcamentoListaView from "./OrcamentoListaView.vue"

// Mock dos services — capturamos chamadas para verificar que NADA de POST acontece
// quando o usuário clica em "Novo orçamento" (CA-1 do plano de paridade).
const mockListar = vi.fn().mockResolvedValue([])
const mockCriar = vi.fn()

vi.mock("@/services/orcamentoService", () => ({
    orcamentoService: {
        listar: () => mockListar(),
        criar: (...args: any[]) => mockCriar(...args),
    },
}))

vi.mock("@/composables/useDebouncedRef", () => ({
    useDebouncedRef: <T>(r: T) => r,
}))

// Controle de permissões por teste — padrão do projeto (ver HomeView.test.ts).
const mockPode = vi.fn()
vi.mock("@/stores/permissoesStore", () => ({
    usePermissoesStore: vi.fn(() => ({ pode: mockPode })),
}))

function criarRouter() {
    return createRouter({
        history: createMemoryHistory(),
        routes: [
            { path: "/orcamentos", name: "Orcamentos", component: { template: "<div/>" } },
            { path: "/orcamentos/novo", name: "OrcamentoNovo", component: { template: "<div/>" } },
            { path: "/orcamentos/:id", name: "OrcamentoDetalhe", component: { template: "<div/>" } },
            { path: "/configuracoes/orcamento", name: "OrcamentoSettings", component: { template: "<div/>" } },
        ],
    })
}

const STUBS = {
    AppPageHeader: { template: "<header><slot/><slot name='acoes'/></header>" },
    AppButton: { template: "<button @click=\"$emit('click')\"><slot/></button>" },
    AppSelect: { template: "<select><slot/></select>" },
    AppSearchInput: { template: "<input/>" },
    AppPagination: { template: "<div/>" },
    OrcamentoKpis: { template: "<div/>" },
    OrcamentoTabela: { template: "<table/>" },
}

describe("OrcamentoListaView — fluxo de novo orçamento", () => {
    beforeEach(() => {
        mockListar.mockClear()
        mockCriar.mockClear()
        // Por padrão, usuário possui permissão orcamento.configurar.
        mockPode.mockReturnValue(true)
    })

    it("CA-1: clicar em 'Novo orçamento' navega para /orcamentos/novo SEM chamar POST de criação", async () => {
        const router = criarRouter()
        await router.push("/orcamentos")
        await router.isReady()

        const wrapper = mount(OrcamentoListaView, {
            global: { plugins: [router], stubs: STUBS },
        })
        await flushPromises()

        // Lista carregou (GET — esperado), nada de criar.
        expect(mockListar).toHaveBeenCalledTimes(1)
        expect(mockCriar).not.toHaveBeenCalled()

        // Encontra o botão "Novo orçamento" e clica.
        const botoes = wrapper.findAll("button")
        const btnNovo = botoes.find(b => b.text().includes("Novo orçamento"))
        expect(btnNovo, "botão 'Novo orçamento' deve existir").toBeTruthy()
        await btnNovo!.trigger("click")
        await flushPromises()

        // CRÍTICO: nenhuma chamada de criação foi feita.
        expect(mockCriar).not.toHaveBeenCalled()
        // Navegou para a rota nova.
        expect(router.currentRoute.value.name).toBe("OrcamentoNovo")
    })

    it("CA-2: sem permissão 'orcamento.configurar', botão 'Novo orçamento' não aparece", async () => {
        // Simula usuário sem a permissão (ex: profissional sem acesso a orçamentos).
        mockPode.mockReturnValue(false)

        const router = criarRouter()
        await router.push("/orcamentos")
        await router.isReady()

        const wrapper = mount(OrcamentoListaView, {
            global: { plugins: [router], stubs: STUBS },
        })
        await flushPromises()

        const botoes = wrapper.findAll("button")
        const btnNovo = botoes.find(b => b.text().includes("Novo orçamento"))
        expect(btnNovo, "botão 'Novo orçamento' NÃO deve existir sem permissão").toBeFalsy()
    })
})
