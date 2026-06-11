import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"

// Mocks declarados antes do import da view.
vi.mock("@/services/termoAceitePublicoService", () => ({
    termoAceitePublicoService: {
        obter: vi.fn(),
        responder: vi.fn(),
    },
    montarUrlAceitePublico: (token: string) => `https://app.imedto.test/termos/aceite/${token}`,
}))

vi.mock("vue-router", () => ({
    useRoute:         () => ({ params: { token: "tok-valido-123" } }),
    createRouter:     vi.fn(() => ({ beforeEach: vi.fn(), push: vi.fn(), currentRoute: { value: {} } })),
    createWebHistory: vi.fn(() => ({})),
}))

import { termoAceitePublicoService } from "@/services/termoAceitePublicoService"
import AceiteTermoPublicoView from "./AceiteTermoPublicoView.vue"

const DTO_VALIDO = {
    tituloModelo:         "Termo de consentimento cirúrgico",
    conteudoSnapshotHtml: "<p>Conteúdo de exemplo do termo.</p>",
    estabelecimentoNome:  "Clínica Imedto",
    profissionalEmissor:  "Dr. Fulano",
    emitidoEm:            "2026-05-20T10:00:00Z",
}

describe("AceiteTermoPublicoView", () => {
    beforeEach(() => {
        vi.mocked(termoAceitePublicoService.obter).mockReset()
        vi.mocked(termoAceitePublicoService.responder).mockReset()
    })

    it("estado pronto: renderiza título do termo e dados do estabelecimento", async () => {
        vi.mocked(termoAceitePublicoService.obter).mockResolvedValueOnce(DTO_VALIDO)

        const wrapper = mount(AceiteTermoPublicoView)
        await flushPromises()

        const texto = wrapper.text()
        expect(texto).toContain("Termo de consentimento cirúrgico")
        expect(texto).toContain("Clínica Imedto")
        expect(texto).toContain("Dr. Fulano")
        // O conteúdo HTML também é renderizado no corpo
        expect(wrapper.html()).toContain("Conteúdo de exemplo do termo")
    })

    it("estado expirado: 410 do GET → renderiza mensagem de link inválido", async () => {
        vi.mocked(termoAceitePublicoService.obter).mockRejectedValueOnce({
            response: { status: 410, data: { mensagem: "Link inválido." } },
        })

        const wrapper = mount(AceiteTermoPublicoView)
        await flushPromises()

        expect(wrapper.text()).toContain("Este link expirou ou já foi respondido")
    })

    it("aceite: marca checkbox + clica Aceito → chama responder({ aceito: true })", async () => {
        vi.mocked(termoAceitePublicoService.obter).mockResolvedValueOnce(DTO_VALIDO)
        vi.mocked(termoAceitePublicoService.responder).mockResolvedValueOnce({
            resultado: "registrado",
            mensagem: "Termo aceito. Você pode fechar esta página.",
        })

        const wrapper = mount(AceiteTermoPublicoView)
        await flushPromises()

        // Marca o checkbox de leitura
        const checkbox = wrapper.find("input[type='checkbox']")
        expect(checkbox.exists()).toBe(true)
        await checkbox.setValue(true)

        // Encontra o botão "Aceito" e clica
        const botoes = wrapper.findAll("button")
        const aceitar = botoes.find(b => b.text().trim() === "Aceito")
        expect(aceitar, "botão Aceito deve existir").toBeTruthy()
        await aceitar!.trigger("click")
        await flushPromises()

        expect(termoAceitePublicoService.responder).toHaveBeenCalledWith(
            "tok-valido-123",
            expect.objectContaining({ aceito: true }),
        )
        expect(wrapper.text()).toContain("Termo aceito")
    })

    it("recusa: clica 'Não aceito' → abre confirmação → confirmar chama responder({ aceito: false })", async () => {
        vi.mocked(termoAceitePublicoService.obter).mockResolvedValueOnce(DTO_VALIDO)
        vi.mocked(termoAceitePublicoService.responder).mockResolvedValueOnce({
            resultado: "registrado",
            mensagem: "Recusa registrada. Você pode fechar esta página.",
        })

        const wrapper = mount(AceiteTermoPublicoView, { attachTo: document.body })
        await flushPromises()

        const botoes = wrapper.findAll("button")
        const recusar = botoes.find(b => b.text().trim() === "Não aceito")
        expect(recusar, "botão Não aceito deve existir").toBeTruthy()
        await recusar!.trigger("click")
        await flushPromises()

        // AppConfirmDialog usa AppModal que teleporta pra body — buscar no DOM real.
        const botoesNoBody = Array.from(document.body.querySelectorAll("button"))
        const confirmar = botoesNoBody.find(b => (b.textContent || "").trim() === "Recusar termo")
        expect(confirmar, "botão Recusar termo deve existir no diálogo").toBeTruthy()
        confirmar!.click()
        await flushPromises()

        expect(termoAceitePublicoService.responder).toHaveBeenCalledWith(
            "tok-valido-123",
            expect.objectContaining({ aceito: false }),
        )
        expect(wrapper.text()).toContain("Recusa registrada")

        wrapper.unmount()
    })

    it("erro 422 no aceite (nome não bate): mostra mensagem inline e não troca de estado", async () => {
        vi.mocked(termoAceitePublicoService.obter).mockResolvedValueOnce(DTO_VALIDO)
        vi.mocked(termoAceitePublicoService.responder).mockRejectedValueOnce({
            response: { status: 422, data: { mensagem: "Nome não confere com o cadastro." } },
        })

        const wrapper = mount(AceiteTermoPublicoView)
        await flushPromises()

        const checkbox = wrapper.find("input[type='checkbox']")
        await checkbox.setValue(true)
        const aceitar = wrapper.findAll("button").find(b => b.text().trim() === "Aceito")
        await aceitar!.trigger("click")
        await flushPromises()

        expect(wrapper.text()).toContain("Nome não confere com o cadastro.")
        // Ainda no card de aceite (não trocou para sucesso)
        expect(wrapper.text()).toContain("Termo de consentimento cirúrgico")
    })
})
