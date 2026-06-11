import { describe, it, expect, vi, afterEach } from "vitest"
import { mount } from "@vue/test-utils"
import ProximosPassosModal from "./ProximosPassosModal.vue"

// Stub do vue-router (useRouter não tem navigate real em testes unitários)
vi.mock("vue-router", async () => {
    const actual = await vi.importActual("vue-router")
    return {
        ...(actual as object),
        useRouter: () => ({ push: vi.fn() }),
    }
})

/**
 * O componente usa <Teleport to="body">, então o conteúdo renderizado
 * fica em document.body — não dentro do wrapper Vue. Usamos wrapper.getComponent()
 * ou buscamos diretamente via document.body.querySelector().
 */
describe("ProximosPassosModal", () => {
    afterEach(() => {
        // Limpa elementos do Teleport deixados no body entre testes
        document.body.innerHTML = ""
    })

    it("não renderiza nada quando aberto=false", () => {
        const wrapper = mount(ProximosPassosModal, {
            attachTo: document.body,
            props: { aberto: false, acoesMarcadas: ["CriarReceita"], pacienteId: 1 },
        })
        expect(document.body.querySelector(".proximos-modal")).toBeNull()
        wrapper.unmount()
    })

    it("renderiza o modal quando aberto=true", () => {
        const wrapper = mount(ProximosPassosModal, {
            attachTo: document.body,
            props: { aberto: true, acoesMarcadas: ["CriarReceita", "AgendarRetorno"], pacienteId: 1 },
        })
        expect(document.body.querySelector(".proximos-modal")).not.toBeNull()
        wrapper.unmount()
    })

    it("exibe label pt-BR para cada ação marcada", () => {
        const wrapper = mount(ProximosPassosModal, {
            attachTo: document.body,
            props: { aberto: true, acoesMarcadas: ["CriarReceita", "CriarAtestado"], pacienteId: 1 },
        })
        expect(document.body.textContent).toContain("Criar receita")
        expect(document.body.textContent).toContain("Criar atestado")
        wrapper.unmount()
    })

    it("emite fechar ao clicar no botão 'Fazer depois'", async () => {
        const wrapper = mount(ProximosPassosModal, {
            attachTo: document.body,
            props: { aberto: true, acoesMarcadas: ["CriarReceita"], pacienteId: 1 },
        })
        const btn = document.body.querySelector(".ns-footer button") as HTMLButtonElement
        btn?.click()
        await wrapper.vm.$nextTick()
        expect(wrapper.emitted("fechar")).toBeTruthy()
        wrapper.unmount()
    })

    it("emite fechar ao clicar no botão X (ns-fechar)", async () => {
        const wrapper = mount(ProximosPassosModal, {
            attachTo: document.body,
            props: { aberto: true, acoesMarcadas: ["CriarReceita"], pacienteId: 1 },
        })
        const btnFechar = document.body.querySelector(".ns-fechar") as HTMLButtonElement
        btnFechar?.click()
        await wrapper.vm.$nextTick()
        expect(wrapper.emitted("fechar")).toBeTruthy()
        wrapper.unmount()
    })

    it("MarcarProcedimentoRealizado exibe 'Conclusão manual pelo painel' (CA66)", () => {
        const wrapper = mount(ProximosPassosModal, {
            attachTo: document.body,
            props: { aberto: true, acoesMarcadas: ["MarcarProcedimentoRealizado"], pacienteId: 1 },
        })
        expect(document.body.textContent).toContain("Conclusão manual pelo painel")
        wrapper.unmount()
    })

    it("CriarReceita tem botão de ação (tem rota)", () => {
        const wrapper = mount(ProximosPassosModal, {
            attachTo: document.body,
            props: { aberto: true, acoesMarcadas: ["CriarReceita"], pacienteId: 1 },
        })
        expect(document.body.querySelector(".nsi-ir")).not.toBeNull()
        wrapper.unmount()
    })

    it("mostra contador de pendências geradas", () => {
        const wrapper = mount(ProximosPassosModal, {
            attachTo: document.body,
            props: { aberto: true, acoesMarcadas: ["CriarReceita", "CriarOrcamento"], pacienteId: 1 },
        })
        expect(document.body.textContent).toContain("2 pendências geradas")
        wrapper.unmount()
    })
})
