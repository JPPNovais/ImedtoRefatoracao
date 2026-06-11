import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import PlanosFormView from "./PlanosFormView.vue"

// Mock do vue-router
const mockPush = vi.fn()
vi.mock("vue-router", () => ({
    useRouter: () => ({ push: mockPush, back: vi.fn() }),
    useRoute: () => ({ params: {} }),
}))

// Mock do store de planos
const mockCriar = vi.fn()
vi.mock("../stores/planosStore", () => ({
    usePlanosStore: () => ({
        carregando: false,
        planoAtual: null,
        carregarPlano: vi.fn(),
        criar: mockCriar,
        atualizar: vi.fn(),
    }),
}))

// Mock dos componentes UI
vi.mock("@/components/ui", () => ({
    AppPageHeader: { template: "<div><slot /><slot name=\"acoes\" /></div>" },
    AppCard: { template: "<div><slot /></div>" },
    AppField: { template: "<div><slot /></div>" },
    AppInput: { template: "<input />" },
    AppTextarea: { template: "<textarea />" },
    AppCheckbox: { template: "<input type=\"checkbox\" />" },
    AppButton: { template: "<button type=\"button\"><slot /></button>" },
}))

// Acessa o setupState via proxy (Vue 3 script setup expõe refs como valores
// unwrapped — a atribuição via proxy aciona o setter do ref internamente)
function setupState(wrapper: ReturnType<typeof mount>): Record<string, unknown> {
    return (wrapper.vm as unknown as { $: { setupState: Record<string, unknown> } }).$.setupState
}

describe("PlanosFormView — navegação pós-salvar", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        mockPush.mockClear()
        mockCriar.mockClear()
    })

    it("após criar com sucesso, redireciona para 'AdminPlanos' (nome correto da lista)", async () => {
        mockCriar.mockResolvedValueOnce(undefined)

        const wrapper = mount(PlanosFormView, { attachTo: document.body })
        const ss = setupState(wrapper)

        // Preenche campos mínimos para passar as validações de salvar()
        ss["motivo"] = "motivo valido de teste"
        ss["nome"] = "Plano Teste"

        await (ss["salvar"] as () => Promise<void>)()

        expect(mockCriar).toHaveBeenCalledOnce()
        expect(mockPush).toHaveBeenCalledWith({ name: "AdminPlanos" })
        // Garante que o typo histórico NÃO é usado
        expect(mockPush).not.toHaveBeenCalledWith({ name: "AdminPlanosList" })

        wrapper.unmount()
    })

    it("após criar com sucesso, erroGeral permanece vazio (catch não acionado)", async () => {
        mockCriar.mockResolvedValueOnce(undefined)

        const wrapper = mount(PlanosFormView, { attachTo: document.body })
        const ss = setupState(wrapper)

        ss["motivo"] = "motivo valido de teste"
        ss["nome"] = "Plano Teste"

        await (ss["salvar"] as () => Promise<void>)()

        expect(ss["erroGeral"]).toBe("")

        wrapper.unmount()
    })
})
