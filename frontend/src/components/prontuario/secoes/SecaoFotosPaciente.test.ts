/**
 * Testes do SecaoFotosPaciente — briefing 2026-06-27_002
 * Valida: modo readOnly (sem backend), filtragem por marcador, tipo inválido.
 */
import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import SecaoFotosPaciente from "./SecaoFotosPaciente.vue"
import { prontuarioService } from "@/services/prontuarioService"
import * as imageUtils from "@/services/imageUtils"

vi.mock("@/services/prontuarioService", () => ({
    prontuarioService: {
        listarAnexos: vi.fn(),
        uploadAnexoComMarcador: vi.fn(),
        obterUrlsLote: vi.fn(),
        removerAnexo: vi.fn(),
    },
}))

vi.mock("@/services/imageUtils", () => ({
    redimensionarImagem: vi.fn(),
}))

vi.mock("@/components/ui", () => ({
    AppEmptyState: {
        name: "AppEmptyState",
        props: ["mensagem"],
        template: `<div class="empty-state">{{ mensagem }}</div>`,
    },
}))

describe("SecaoFotosPaciente", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    it("readOnly: exibe placeholders fictícios sem chamar o service", async () => {
        const wrapper = mount(SecaoFotosPaciente, {
            props: { modelValue: {}, readOnly: true },
        })
        await flushPromises()

        expect(prontuarioService.listarAnexos).not.toHaveBeenCalled()
        // 3 exemplos fictícios
        const thumbnails = wrapper.findAll(".thumbnail-wrap")
        expect(thumbnails.length).toBe(3)
    })

    it("modo pendente (evolucaoId nulo): upload habilitado sem aviso 'Salve primeiro' (CA27 addendum)", async () => {
        // Addendum briefing 2026-06-27_002: upload diferido — foto fica pendente no front
        // até o salvar. Gating por evolucaoId removido (CA27/R12).
        const wrapper = mount(SecaoFotosPaciente, {
            props: { modelValue: {}, readOnly: false, pacienteId: 1, evolucaoId: null },
        })
        await flushPromises()

        expect(wrapper.text()).not.toContain("Salve a evolução primeiro")
        const input = wrapper.find("input[type=file]")
        expect(input.exists()).toBe(true)
        expect((input.element as HTMLInputElement).disabled).toBe(false)
    })

    it("lista apenas fotos com marcador='foto-paciente'", async () => {
        vi.mocked(prontuarioService.listarAnexos).mockResolvedValue([
            { id: 1, prontuarioId: 10, evolucaoId: null, nomeOriginal: "foto.jpg",
              mimeType: "image/jpeg", tamanhoBytes: 200_000,
              criadoEm: new Date().toISOString(), autorNome: null, marcador: "foto-paciente" },
            { id: 2, prontuarioId: 10, evolucaoId: null, nomeOriginal: "doc.pdf",
              mimeType: "application/pdf", tamanhoBytes: 500_000,
              criadoEm: new Date().toISOString(), autorNome: null, marcador: "anexo" },
        ])
        vi.mocked(prontuarioService.obterUrlsLote).mockResolvedValue([
            { id: 1, nomeOriginal: "foto.jpg", mimeType: "image/jpeg",
              url: "https://s3.example/foto.jpg", expiraEm: new Date().toISOString() },
        ])
        const wrapper = mount(SecaoFotosPaciente, {
            props: { modelValue: {}, readOnly: false, pacienteId: 1, evolucaoId: 10 },
        })
        await flushPromises()

        // Apenas foto-paciente (id=1) deve aparecer; doc.pdf filtrado.
        const thumbs = wrapper.findAll(".thumbnail-wrap")
        expect(thumbs.length).toBe(1)
    })

    it("valida tipo inválido (pdf não é imagem)", async () => {
        vi.mocked(prontuarioService.listarAnexos).mockResolvedValue([])
        const wrapper = mount(SecaoFotosPaciente, {
            props: { modelValue: {}, readOnly: false, pacienteId: 1, evolucaoId: 10 },
        })
        await flushPromises()

        const input = wrapper.find("input[type=file]")
        const arquivoPdf = new File(["x"], "laudo.pdf", { type: "application/pdf" })
        Object.defineProperty(input.element, "files", { value: [arquivoPdf] })
        await input.trigger("change")

        expect(prontuarioService.uploadAnexoComMarcador).not.toHaveBeenCalled()
        expect(wrapper.find(".msg-erro").text()).toContain("não permitido")
    })

    it("redimensiona imagem antes do upload com 1600px e qualidade 0.8", async () => {
        vi.mocked(prontuarioService.listarAnexos).mockResolvedValue([])
        vi.mocked(prontuarioService.obterUrlsLote).mockResolvedValue([])
        const arquivoOriginal = new File(["x"], "foto.jpg", { type: "image/jpeg" })
        const arquivoReduzido = new File(["x"], "foto.jpg", { type: "image/jpeg" })
        vi.mocked(imageUtils.redimensionarImagem).mockResolvedValue(arquivoReduzido)
        vi.mocked(prontuarioService.uploadAnexoComMarcador).mockResolvedValue({ anexoId: 99 })

        const wrapper = mount(SecaoFotosPaciente, {
            props: { modelValue: {}, readOnly: false, pacienteId: 1, evolucaoId: 10 },
        })
        await flushPromises()

        const input = wrapper.find("input[type=file]")
        Object.defineProperty(input.element, "files", { value: [arquivoOriginal] })
        await input.trigger("change")
        await flushPromises()

        // CA6: deve redimensionar para 1600px com qualidade 0.8 antes do upload.
        expect(imageUtils.redimensionarImagem).toHaveBeenCalledWith(arquivoOriginal, 1600, 0.8)
        // E enviar o arquivo reduzido com marcador='foto-paciente'
        expect(prontuarioService.uploadAnexoComMarcador).toHaveBeenCalledWith(
            1, arquivoReduzido, "foto-paciente", 10,
        )
    })
})
