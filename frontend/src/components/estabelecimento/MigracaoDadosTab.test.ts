/**
 * Testes de MigracaoDadosTab (briefing 2026-06-15_001 — Marco 1).
 *
 * CA19: validação de formato (só ZIP) e tamanho (máx 50MB) no front.
 * CA19: botão desabilitado se arquivo não selecionado ou termo não aceito.
 * R12:  termo de responsabilidade obrigatório antes do envio.
 * CA4:  componente chama migracaoService, não httpClient diretamente.
 */

import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount } from "@vue/test-utils"
import MigracaoDadosTab from "./MigracaoDadosTab.vue"
import { LIMITE_UPLOAD_BYTES, MENSAGEM_LIMITE } from "@/services/migracaoService"

// ─── Mocks ───────────────────────────────────────────────────────────────────

vi.mock("@/services/migracaoService", async (importOriginal) => {
    const original = await importOriginal<typeof import("@/services/migracaoService")>()
    return {
        ...original,
        default: {
            iniciarUpload: vi.fn(),
            obterStatus:   vi.fn(),
        },
    }
})

// AppButton: stub que renderiza um <button> com os atributos (:disabled) passados.
const AppButtonStub = {
    name: "AppButton",
    template: `<button v-bind="$attrs" @click="$emit('click')"><slot /></button>`,
    inheritAttrs: true,
    emits: ["click"],
    props: ["disabled", "loading", "variante"],
}

import migracaoService from "@/services/migracaoService"

// ─── Helpers ─────────────────────────────────────────────────────────────────

function criarArquivo(nome: string, tamanhoBytes: number, type = "application/zip") {
    const file = new File([""], nome, { type })
    Object.defineProperty(file, "size", { value: tamanhoBytes })
    return file
}

function montarComponente() {
    return mount(MigracaoDadosTab, {
        props: { estabelecimentoId: 10 },
        global: {
            components: { AppButton: AppButtonStub },
            // Stub ícones FA para evitar overhead de render.
            stubs: { "i": true },
        },
    })
}

// ─── Testes ──────────────────────────────────────────────────────────────────

describe("MigracaoDadosTab", () => {
    beforeEach(() => {
        vi.mocked(migracaoService.iniciarUpload).mockReset()
    })

    // ── Estado inicial ────────────────────────────────────────────────────────

    it("renderiza formulário de upload inicialmente", () => {
        const wrapper = montarComponente()

        expect(wrapper.find(".upload-card").exists()).toBe(true)
        expect(wrapper.find(".job-criado").exists()).toBe(false)
    })

    it("botão de envio desabilitado sem arquivo selecionado", () => {
        const wrapper = montarComponente()

        const btn = wrapper.find("button")
        // disabled = true sem arquivo nem termo
        expect(btn.attributes("disabled")).toBeDefined()
    })

    // ── CA19 — Validação de formato ───────────────────────────────────────────

    it("CA19: arquivo não-ZIP exibe mensagem de erro e não seleciona", async () => {
        const wrapper = montarComponente()
        const input = wrapper.find("input[type='file']")

        const arquivo = criarArquivo("dados.csv", 1024, "text/csv")
        Object.defineProperty(input.element, "files", {
            value: { 0: arquivo, length: 1, item: () => arquivo },
        })
        await input.trigger("change")

        expect(wrapper.find(".erro-upload").exists()).toBe(true)
        expect(wrapper.find(".erro-upload").text()).toContain("ZIP")
        // Arquivo não foi selecionado.
        expect(wrapper.find(".dropzone-titulo").text()).toContain("Clique para selecionar")
    })

    // ── CA19 — Validação de tamanho ───────────────────────────────────────────

    it("CA19: arquivo acima de 50MB exibe MENSAGEM_LIMITE e não seleciona", async () => {
        const wrapper = montarComponente()
        const input = wrapper.find("input[type='file']")

        const arquivo = criarArquivo("dados.zip", LIMITE_UPLOAD_BYTES + 1)
        Object.defineProperty(input.element, "files", {
            value: { 0: arquivo, length: 1, item: () => arquivo },
        })
        await input.trigger("change")

        expect(wrapper.find(".erro-upload").text()).toContain("50MB")
        // Mensagem do front deve ser a mesma constante exportada.
        expect(wrapper.find(".erro-upload").text()).toContain(MENSAGEM_LIMITE.slice(0, 20))
        expect(wrapper.find(".dropzone-titulo").text()).toContain("Clique para selecionar")
    })

    it("CA19: arquivo válido (50MB) exibe nome no dropzone", async () => {
        const wrapper = montarComponente()
        const input = wrapper.find("input[type='file']")

        const arquivo = criarArquivo("migracao.zip", LIMITE_UPLOAD_BYTES)
        Object.defineProperty(input.element, "files", {
            value: { 0: arquivo, length: 1, item: () => arquivo },
        })
        await input.trigger("change")

        expect(wrapper.find(".dropzone-titulo").text()).toBe("migracao.zip")
        expect(wrapper.find(".erro-upload").exists()).toBe(false)
    })

    // ── R12 — Termo obrigatório ───────────────────────────────────────────────

    it("R12: botão desabilitado com arquivo selecionado mas sem termo aceito", async () => {
        const wrapper = montarComponente()
        const input = wrapper.find("input[type='file']")

        const arquivo = criarArquivo("ok.zip", 1024)
        Object.defineProperty(input.element, "files", {
            value: { 0: arquivo, length: 1, item: () => arquivo },
        })
        await input.trigger("change")

        const checkbox = wrapper.find("input[type='checkbox']")
        expect((checkbox.element as HTMLInputElement).checked).toBe(false)

        const btn = wrapper.find("button")
        expect(btn.attributes("disabled")).toBeDefined()
    })

    it("R12: botão habilitado após arquivo válido + termo aceito", async () => {
        const wrapper = montarComponente()
        const fileInput = wrapper.find("input[type='file']")

        const arquivo = criarArquivo("ok.zip", 1024)
        Object.defineProperty(fileInput.element, "files", {
            value: { 0: arquivo, length: 1, item: () => arquivo },
        })
        await fileInput.trigger("change")

        const checkbox = wrapper.find("input[type='checkbox']")
        await checkbox.setValue(true)

        const btn = wrapper.find("button")
        expect(btn.attributes("disabled")).toBeUndefined()
    })

    // ── Fluxo de envio ────────────────────────────────────────────────────────

    it("envio bem-sucedido exibe estado job-criado com status aguardando_aprovacao (CA40/CA47)", async () => {
        // Addendum 003: backend agora retorna aguardando_aprovacao (não mais aguardando_mapa).
        vi.mocked(migracaoService.iniciarUpload).mockResolvedValueOnce({
            jobId: 7,
            status: "aguardando_aprovacao",
        })

        const wrapper = montarComponente()
        const fileInput = wrapper.find("input[type='file']")
        const arquivo = criarArquivo("ok.zip", 1024)
        Object.defineProperty(fileInput.element, "files", {
            value: { 0: arquivo, length: 1, item: () => arquivo },
        })
        await fileInput.trigger("change")

        await wrapper.find("input[type='checkbox']").setValue(true)
        await wrapper.find("button").trigger("click")
        await vi.waitFor(() => wrapper.find(".job-criado").exists())

        expect(wrapper.find(".job-criado").exists()).toBe(true)
        // CA47: label honesto para o cliente (D-A2): "aguardando aprovação da equipe Imedto".
        expect(wrapper.find(".job-info-valor").text()).toContain("Aguardando aprovação")
    })

    it("falha no upload exibe mensagem de erro", async () => {
        vi.mocked(migracaoService.iniciarUpload).mockRejectedValueOnce(
            new Error("Falha ao processar o arquivo. Tente novamente.")
        )

        const wrapper = montarComponente()
        const fileInput = wrapper.find("input[type='file']")
        const arquivo = criarArquivo("ok.zip", 1024)
        Object.defineProperty(fileInput.element, "files", {
            value: { 0: arquivo, length: 1, item: () => arquivo },
        })
        await fileInput.trigger("change")

        await wrapper.find("input[type='checkbox']").setValue(true)
        await wrapper.find("button").trigger("click")
        await vi.waitFor(() => wrapper.find(".erro-upload").exists())

        expect(wrapper.find(".erro-upload").text()).toContain("Falha ao processar")
        expect(wrapper.find(".job-criado").exists()).toBe(false)
    })
})
