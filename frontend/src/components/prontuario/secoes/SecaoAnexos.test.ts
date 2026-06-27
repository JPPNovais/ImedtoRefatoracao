/**
 * Testes do SecaoAnexos — briefing 2026-06-27_002
 * Valida: validação de tipo/tamanho, modo readOnly, renderização sem backend.
 */
import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import SecaoAnexos from "./SecaoAnexos.vue"
import { prontuarioService } from "@/services/prontuarioService"

// Stub do service — evita chamadas de rede.
vi.mock("@/services/prontuarioService", () => ({
    prontuarioService: {
        listarAnexos: vi.fn(),
        uploadAnexoComMarcador: vi.fn(),
        obterUrlAnexo: vi.fn(),
        removerAnexo: vi.fn(),
    },
}))

// Stub do AppEmptyState para simplificar asserções.
vi.mock("@/components/ui", () => ({
    AppEmptyState: {
        name: "AppEmptyState",
        props: ["mensagem"],
        template: `<div class="empty-state">{{ mensagem }}</div>`,
    },
}))

describe("SecaoAnexos", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    it("readOnly: exibe exemplos fictícios sem chamar o service (CA prévia do builder)", async () => {
        vi.mocked(prontuarioService.listarAnexos).mockResolvedValue([])
        const wrapper = mount(SecaoAnexos, {
            props: { modelValue: {}, readOnly: true },
        })
        await flushPromises()

        // Não deve chamar o backend
        expect(prontuarioService.listarAnexos).not.toHaveBeenCalled()
        // Deve exibir exemplos fictícios (2 itens)
        const itens = wrapper.findAll(".item-anexo")
        expect(itens.length).toBe(2)
        // Botão de upload não aparece em readOnly
        expect(wrapper.find(".btn-upload").exists()).toBe(false)
    })

    it("modo pendente (evolucaoId nulo): upload habilitado sem aviso de 'Salve primeiro' (CA27 addendum)", async () => {
        // Addendum briefing 2026-06-27_002: upload diferido — arquivos ficam pendentes no front
        // até o salvar, sem gating por evolucaoId. "Salve a evolução primeiro" foi removido.
        const wrapper = mount(SecaoAnexos, {
            props: { modelValue: {}, readOnly: false, pacienteId: 1, evolucaoId: null },
        })
        await flushPromises()

        expect(wrapper.text()).not.toContain("Salve a evolução primeiro")
        const input = wrapper.find("input[type=file]")
        expect(input.exists()).toBe(true)
        expect((input.element as HTMLInputElement).disabled).toBe(false)
    })

    it("com contexto e sem anexos: exibe empty state", async () => {
        vi.mocked(prontuarioService.listarAnexos).mockResolvedValue([])
        const wrapper = mount(SecaoAnexos, {
            props: { modelValue: {}, readOnly: false, pacienteId: 1, evolucaoId: 10 },
        })
        await flushPromises()

        expect(prontuarioService.listarAnexos).toHaveBeenCalledWith(1, 10)
        expect(wrapper.find(".empty-state").exists()).toBe(true)
    })

    it("lista apenas anexos com marcador='anexo' (filtra foto-paciente)", async () => {
        vi.mocked(prontuarioService.listarAnexos).mockResolvedValue([
            { id: 1, prontuarioId: 10, evolucaoId: null, nomeOriginal: "doc.pdf",
              mimeType: "application/pdf", tamanhoBytes: 500_000,
              criadoEm: new Date().toISOString(), autorNome: null, marcador: "anexo" },
            { id: 2, prontuarioId: 10, evolucaoId: null, nomeOriginal: "foto.jpg",
              mimeType: "image/jpeg", tamanhoBytes: 200_000,
              criadoEm: new Date().toISOString(), autorNome: null, marcador: "foto-paciente" },
        ])
        const wrapper = mount(SecaoAnexos, {
            props: { modelValue: {}, readOnly: false, pacienteId: 1, evolucaoId: 10 },
        })
        await flushPromises()

        // Apenas o doc.pdf (marcador="anexo") deve aparecer
        expect(wrapper.findAll(".item-anexo").length).toBe(1)
        expect(wrapper.text()).toContain("doc.pdf")
        expect(wrapper.text()).not.toContain("foto.jpg")
    })

    it("valida tipo inválido antes de enviar (CA3)", async () => {
        vi.mocked(prontuarioService.listarAnexos).mockResolvedValue([])
        const wrapper = mount(SecaoAnexos, {
            props: { modelValue: {}, readOnly: false, pacienteId: 1, evolucaoId: 10 },
        })
        await flushPromises()

        // Simula upload de arquivo com tipo inválido
        const input = wrapper.find("input[type=file]")
        const arquivoInvalido = new File(["x"], "malware.exe", { type: "application/x-msdownload" })
        Object.defineProperty(input.element, "files", { value: [arquivoInvalido] })
        await input.trigger("change")

        expect(prontuarioService.uploadAnexoComMarcador).not.toHaveBeenCalled()
        expect(wrapper.find(".msg-erro").text()).toContain("não permitido")
    })

    it("valida tamanho > 2MB antes de enviar (CA4)", async () => {
        vi.mocked(prontuarioService.listarAnexos).mockResolvedValue([])
        const wrapper = mount(SecaoAnexos, {
            props: { modelValue: {}, readOnly: false, pacienteId: 1, evolucaoId: 10 },
        })
        await flushPromises()

        const input = wrapper.find("input[type=file]")
        const arquivo = new File([new ArrayBuffer(3 * 1024 * 1024)], "grande.pdf",
            { type: "application/pdf" })
        Object.defineProperty(input.element, "files", { value: [arquivo] })
        await input.trigger("change")

        expect(prontuarioService.uploadAnexoComMarcador).not.toHaveBeenCalled()
        expect(wrapper.find(".msg-erro").text()).toContain("grande")
    })

    it("aceita Office (docx) e envia com marcador='anexo'", async () => {
        vi.mocked(prontuarioService.listarAnexos).mockResolvedValue([])
        vi.mocked(prontuarioService.uploadAnexoComMarcador).mockResolvedValue({ anexoId: 5 })
        const wrapper = mount(SecaoAnexos, {
            props: { modelValue: {}, readOnly: false, pacienteId: 1, evolucaoId: 10 },
        })
        await flushPromises()

        const input = wrapper.find("input[type=file]")
        const arquivo = new File(["x"], "relatorio.docx", {
            type: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        })
        Object.defineProperty(input.element, "files", { value: [arquivo] })
        await input.trigger("change")
        await flushPromises()

        expect(prontuarioService.uploadAnexoComMarcador).toHaveBeenCalledWith(
            1, arquivo, "anexo", 10,
        )
    })

    /**
     * Teste de regressão — bug Tipo A (QA 2026-06-27):
     * Dado modelo com seção Anexos na consulta atual,
     * Quando o usuário salva a evolução (evolucaoId recebido do backend),
     * Então SecaoAnexos exibe o input de upload habilitado (sem o aviso 'Salve a evolução primeiro').
     *
     * O bug: ProntuarioView.vue passava :evolucao-id="null" fixo;
     * agora passa evolucaoIdAtual (ref atualizado após registrarEvolucao).
     */
    it("regressão: dado evolucaoId não-nulo após salvar evolução, upload é habilitado", async () => {
        vi.mocked(prontuarioService.listarAnexos).mockResolvedValue([])

        // Simula estado após salvar: evolucaoId=42 propagado pelo ProntuarioView.
        const wrapper = mount(SecaoAnexos, {
            props: { modelValue: {}, readOnly: false, pacienteId: 1, evolucaoId: 42 },
        })
        await flushPromises()

        // Upload deve estar habilitado — aviso NÃO pode aparecer.
        expect(wrapper.text()).not.toContain("Salve a evolução primeiro")
        // Input de arquivo deve existir e não estar desabilitado.
        const input = wrapper.find("input[type=file]")
        expect(input.exists()).toBe(true)
        expect((input.element as HTMLInputElement).disabled).toBe(false)
    })
})
