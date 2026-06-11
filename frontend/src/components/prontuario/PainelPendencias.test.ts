import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import PainelPendencias from "./PainelPendencias.vue"

// Stub vue-router
vi.mock("vue-router", async () => {
    const actual = await vi.importActual("vue-router")
    return {
        ...(actual as object),
        useRouter: () => ({ push: vi.fn() }),
    }
})

// Mock do pendenciaService
vi.mock("@/services/pendenciaService", () => ({
    pendenciaService: {
        listarAbertas: vi.fn(),
        concluirManual: vi.fn(),
    },
    ACAO_LABELS: {
        CriarReceita:                "Criar receita",
        CriarAtestado:               "Criar atestado",
        PedirExame:                  "Pedir exame",
        CriarOrcamento:              "Criar orçamento",
        MarcarProcedimentoRealizado: "Marcar procedimento como realizado",
        AgendarRetorno:              "Agendar retorno",
    },
    rotaParaAcao: (pacienteId: number, acao: string) => {
        if (acao === "MarcarProcedimentoRealizado") return null
        return `/pacientes/${pacienteId}?acao=${acao}`
    },
}))

// Mock do permissoesStore (CA70)
vi.mock("@/stores/permissoesStore", () => ({
    usePermissoesStore: vi.fn(),
}))

import { pendenciaService, type AcaoPendencia, type PendenciaAberta } from "@/services/pendenciaService"
import { usePermissoesStore } from "@/stores/permissoesStore"

function mockPermissoes(pode: (k: string) => boolean) {
    (usePermissoesStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({ pode })
}

const pendenciaAberta = (id: number, acao: AcaoPendencia = "CriarReceita"): PendenciaAberta => ({
    id,
    evolucaoId: 100,
    acao,
    status: "Pendente",
    criadoEm: "2026-06-10T10:00:00Z",
})

describe("PainelPendencias", () => {
    beforeEach(() => {
        vi.mocked(pendenciaService.listarAbertas).mockReset()
        vi.mocked(pendenciaService.concluirManual).mockReset()
        // Por padrão concede prontuario.editar (testes pré-CA70 não mudam comportamento)
        mockPermissoes(() => true)
    })

    it("não renderiza o painel quando não há pendências (CA74)", async () => {
        vi.mocked(pendenciaService.listarAbertas).mockResolvedValueOnce([])
        const wrapper = mount(PainelPendencias, { props: { pacienteId: 1 } })
        await flushPromises()
        expect(wrapper.find(".painel-pendencias").exists()).toBe(false)
    })

    it("renderiza o painel com badge quando há pendências", async () => {
        vi.mocked(pendenciaService.listarAbertas).mockResolvedValueOnce([
            pendenciaAberta(1, "CriarReceita"),
        ])
        const wrapper = mount(PainelPendencias, { props: { pacienteId: 1 } })
        await flushPromises()
        expect(wrapper.find(".painel-pendencias").exists()).toBe(true)
        expect(wrapper.find(".pp-badge").text()).toBe("1")
    })

    it("exibe label pt-BR de cada ação", async () => {
        vi.mocked(pendenciaService.listarAbertas).mockResolvedValueOnce([
            pendenciaAberta(1, "CriarReceita"),
            pendenciaAberta(2, "AgendarRetorno"),
        ])
        const wrapper = mount(PainelPendencias, { props: { pacienteId: 1 } })
        await flushPromises()
        expect(wrapper.text()).toContain("Criar receita")
        expect(wrapper.text()).toContain("Agendar retorno")
    })

    it("pendência com rota exibe botão 'Fazer agora'", async () => {
        vi.mocked(pendenciaService.listarAbertas).mockResolvedValueOnce([
            pendenciaAberta(1, "CriarReceita"),
        ])
        const wrapper = mount(PainelPendencias, { props: { pacienteId: 1 } })
        await flushPromises()
        expect(wrapper.find(".ppi-ir").exists()).toBe(true)
    })

    it("pendência MarcarProcedimentoRealizado exibe 'Fazer agora' (abre modal F4) e não exibe 'Concluir' manual", async () => {
        vi.mocked(pendenciaService.listarAbertas).mockResolvedValueOnce([
            pendenciaAberta(1, "MarcarProcedimentoRealizado"),
        ])
        const wrapper = mount(PainelPendencias, { props: { pacienteId: 1 } })
        await flushPromises()
        // F4: MarcarProcedimentoRealizado tem modal próprio — "Fazer agora" sempre aparece (CA88)
        expect(wrapper.find(".ppi-ir").exists()).toBe(true)
        // "Concluir" manual NÃO aparece para MarcarProcedimentoRealizado (CA88)
        expect(wrapper.find(".ppi-concluir").exists()).toBe(false)
    })

    it("exibe confirmação ao clicar em 'Concluir' (CA68)", async () => {
        vi.mocked(pendenciaService.listarAbertas).mockResolvedValueOnce([
            pendenciaAberta(5, "CriarReceita"),
        ])
        const wrapper = mount(PainelPendencias, { props: { pacienteId: 1 } })
        await flushPromises()
        await wrapper.find(".ppi-concluir").trigger("click")
        expect(wrapper.text()).toContain("Confirmar conclusão?")
    })

    it("ao cancelar confirmação, volta ao estado normal", async () => {
        vi.mocked(pendenciaService.listarAbertas).mockResolvedValueOnce([
            pendenciaAberta(5, "CriarReceita"),
        ])
        const wrapper = mount(PainelPendencias, { props: { pacienteId: 1 } })
        await flushPromises()
        await wrapper.find(".ppi-concluir").trigger("click") // abre confirmação
        expect(wrapper.text()).toContain("Confirmar conclusão?")
        // Clica em "Não"
        const btns = wrapper.findAll(".btn-icon-sm")
        const naoBtn = btns.find(b => b.text() === "Não")
        await naoBtn?.trigger("click")
        expect(wrapper.text()).not.toContain("Confirmar conclusão?")
    })

    it("chama concluirManual ao confirmar e remove item da lista", async () => {
        vi.mocked(pendenciaService.listarAbertas).mockResolvedValueOnce([
            pendenciaAberta(5, "CriarReceita"),
        ])
        vi.mocked(pendenciaService.concluirManual).mockResolvedValueOnce(undefined)
        const wrapper = mount(PainelPendencias, { props: { pacienteId: 1 } })
        await flushPromises()
        await wrapper.find(".ppi-concluir").trigger("click") // abre confirmação
        const simBtn = wrapper.findAll(".btn-icon-sm").find(b => b.text() === "Sim")
        await simBtn?.trigger("click")
        await flushPromises()
        expect(pendenciaService.concluirManual).toHaveBeenCalledWith(1, 5)
        // Item removido da lista → painel some
        expect(wrapper.find(".painel-pendencias").exists()).toBe(false)
    })

    // ── CA70: RBAC no front ────────────────────────────────────────────────────

    it("CA70: sem prontuario.editar → botão .ppi-concluir ausente", async () => {
        mockPermissoes(() => false)
        vi.mocked(pendenciaService.listarAbertas).mockResolvedValueOnce([
            pendenciaAberta(1, "CriarReceita"),
        ])
        const wrapper = mount(PainelPendencias, { props: { pacienteId: 1 } })
        await flushPromises()
        expect(wrapper.find(".ppi-concluir").exists()).toBe(false)
    })

    it("CA70: com prontuario.editar → botão .ppi-concluir presente", async () => {
        mockPermissoes((k) => k === "prontuario.editar")
        vi.mocked(pendenciaService.listarAbertas).mockResolvedValueOnce([
            pendenciaAberta(1, "CriarReceita"),
        ])
        const wrapper = mount(PainelPendencias, { props: { pacienteId: 1 } })
        await flushPromises()
        expect(wrapper.find(".ppi-concluir").exists()).toBe(true)
    })
})
