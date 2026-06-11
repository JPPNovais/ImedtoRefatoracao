/**
 * Testes do WidgetProximosPassos — addendum CA190–CA201.
 *
 * Cobre:
 *  CA191 — não aparece sem ações (controlado pela view via v-if; aqui valida
 *          que lista vazia não exibe itens).
 *  CA194 — header mostra título, contador e botões minimizar/fechar.
 *  CA195 — links de ação reusam rotaParaAcao; MarcarProcedimentoRealizado sem link.
 *  CA196 — re-fetch quando rotaAtual muda (watch de rota).
 *  CA197 — minimizar colapsa para pílula; clicar na pílula expande e re-busca.
 *  CA198 — fechar emite evento "fechar".
 *  CA201 — regressão: PainelPendencias é componente separado (não tocado pelo widget).
 */
import { describe, it, expect, vi, afterEach, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import WidgetProximosPassos from "./WidgetProximosPassos.vue"
import type { AcaoPendencia, PendenciaAberta } from "@/services/pendenciaService"
import { pendenciaService } from "@/services/pendenciaService"

// ── Mocks ─────────────────────────────────────────────────────────────────────
// vi.mock é hoistado antes de toda declaração de variável. Por isso usamos
// vi.fn() diretamente no factory — não podemos referenciar variáveis locais.

const routerPushMock = vi.fn()

vi.mock("vue-router", async () => {
    const actual = await vi.importActual("vue-router")
    return {
        ...(actual as object),
        useRouter: () => ({ push: routerPushMock }),
    }
})

vi.mock("@/services/pendenciaService", async () => {
    const actual = await vi.importActual("@/services/pendenciaService")
    return {
        ...(actual as object),
        pendenciaService: {
            listarAbertas: vi.fn().mockResolvedValue([]),
        },
    }
})

// ── Helpers ───────────────────────────────────────────────────────────────────

function propsBase() {
    return {
        acoesMarcadas: ["CriarReceita", "AgendarRetorno"] as AcaoPendencia[],
        pacienteId: 42,
        rotaAtual: "/pacientes/42",
    }
}

function montarWidget(overrides: Record<string, unknown> = {}) {
    return mount(WidgetProximosPassos, {
        attachTo: document.body,
        props: { ...propsBase(), ...overrides },
    })
}

// Atalho tipado para o mock de listarAbertas
function mockListar() {
    return vi.mocked(pendenciaService.listarAbertas)
}

// ── Setup / teardown ──────────────────────────────────────────────────────────

/** Pendências abertas padrão — CriarReceita e AgendarRetorno abertas (não concluídas). */
const abertosPadrao: PendenciaAberta[] = [
    { id: 1, evolucaoId: 5, acao: "CriarReceita",    status: "Pendente", criadoEm: "" },
    { id: 2, evolucaoId: 5, acao: "AgendarRetorno",  status: "Pendente", criadoEm: "" },
]

beforeEach(() => {
    // Por padrão todas as ações estão abertas (nenhuma concluída)
    mockListar().mockResolvedValue(abertosPadrao)
    routerPushMock.mockClear()
})

afterEach(() => {
    document.body.innerHTML = ""
    vi.clearAllMocks()
})

// ── Testes ────────────────────────────────────────────────────────────────────

describe("WidgetProximosPassos", () => {

    // CA194 ─────────────────────────────────────────────────────────────────────
    it("CA194 — renderiza título 'Próximos passos' e contador no header", async () => {
        const wrapper = montarWidget()
        // Aguarda fetch inicial (listarAbertas retorna as 2 ações abertas)
        await new Promise(r => setTimeout(r, 0))
        await wrapper.vm.$nextTick()

        const titulo = document.body.querySelector(".wpp-titulo")
        expect(titulo?.textContent?.trim()).toBe("Próximos passos")

        // Contador: 0 de 2 concluídas (ambas ainda abertas)
        const contador = document.body.querySelector(".wpp-contador")
        expect(contador?.textContent?.trim()).toBe("0 de 2 concluídas")

        wrapper.unmount()
    })

    it("CA194 — exibe botões minimizar e fechar no header", () => {
        const wrapper = montarWidget()

        const btns = document.body.querySelectorAll(".wpp-btn-icone")
        expect(btns.length).toBe(2)
        expect(btns[0].getAttribute("aria-label")).toBe("Minimizar")
        expect(btns[1].getAttribute("aria-label")).toBe("Fechar — Fazer depois")

        wrapper.unmount()
    })

    // CA195 ─────────────────────────────────────────────────────────────────────
    it("CA195 — exibe label pt-BR para cada ação marcada", () => {
        const wrapper = montarWidget()

        expect(document.body.textContent).toContain("Criar receita")
        expect(document.body.textContent).toContain("Agendar retorno")

        wrapper.unmount()
    })

    it("CA195 — MarcarProcedimentoRealizado exibe 'Pelo painel' (sem botão ir)", async () => {
        // Garante que MarcarProcedimentoRealizado aparece como aberta (não concluída)
        mockListar().mockResolvedValue([
            { id: 3, evolucaoId: 5, acao: "MarcarProcedimentoRealizado", status: "Pendente", criadoEm: "" },
        ])

        const wrapper = montarWidget({
            acoesMarcadas: ["MarcarProcedimentoRealizado"] as AcaoPendencia[],
        })
        await wrapper.vm.$nextTick()
        await new Promise(r => setTimeout(r, 0))
        await wrapper.vm.$nextTick()

        expect(document.body.querySelector(".wpp-ir")).toBeNull()
        expect(document.body.textContent).toContain("Pelo painel")

        wrapper.unmount()
    })

    it("CA195 — CriarOrcamento com evolucaoId navega para rota de pré-preenchimento", async () => {
        mockListar().mockResolvedValue([
            { id: 4, evolucaoId: 99, acao: "CriarOrcamento", status: "Pendente", criadoEm: "" },
        ])

        const wrapper = montarWidget({
            acoesMarcadas: ["CriarOrcamento"] as AcaoPendencia[],
            evolucaoId: 99,
        })
        // Aguarda resolução do fetch inicial
        await new Promise(r => setTimeout(r, 0))
        await wrapper.vm.$nextTick()

        const btnIr = document.body.querySelector(".wpp-ir") as HTMLButtonElement | null
        expect(btnIr).not.toBeNull()
        btnIr!.click()
        await wrapper.vm.$nextTick()

        expect(routerPushMock).toHaveBeenCalledWith(
            "/orcamentos/novo?evolucaoId=99&pacienteId=42",
        )
        wrapper.unmount()
    })

    it("CA195 — CriarReceita tem botão ir que usa rotaParaAcao", async () => {
        mockListar().mockResolvedValue([
            { id: 5, evolucaoId: 5, acao: "CriarReceita", status: "Pendente", criadoEm: "" },
        ])

        const wrapper = montarWidget({
            acoesMarcadas: ["CriarReceita"] as AcaoPendencia[],
            pacienteId: 7,
        })
        // Aguarda resolução do fetch inicial
        await new Promise(r => setTimeout(r, 0))
        await wrapper.vm.$nextTick()

        const btnIr = document.body.querySelector(".wpp-ir") as HTMLButtonElement | null
        expect(btnIr).not.toBeNull()
        btnIr!.click()
        await wrapper.vm.$nextTick()

        expect(routerPushMock).toHaveBeenCalledWith("/pacientes/7?aba=documentos&tipo=Receita")
        wrapper.unmount()
    })

    // CA197 ─────────────────────────────────────────────────────────────────────
    it("CA197 — clicar em minimizar exibe a pílula compacta", async () => {
        const wrapper = montarWidget()
        await wrapper.vm.$nextTick()

        expect(document.body.querySelector(".wpp-widget")).not.toBeNull()
        expect(document.body.querySelector(".wpp-pilula")).toBeNull()

        const btnMinimizar = document.body.querySelectorAll(".wpp-btn-icone")[0] as HTMLButtonElement
        btnMinimizar.click()
        await wrapper.vm.$nextTick()

        expect(document.body.querySelector(".wpp-widget")).toBeNull()
        expect(document.body.querySelector(".wpp-pilula")).not.toBeNull()

        wrapper.unmount()
    })

    it("CA197 — clicar na pílula expande o widget e re-busca pendências", async () => {
        const wrapper = montarWidget()
        await wrapper.vm.$nextTick()

        // Minimiza
        const btnMin = document.body.querySelectorAll(".wpp-btn-icone")[0] as HTMLButtonElement
        btnMin.click()
        await wrapper.vm.$nextTick()

        mockListar().mockClear()

        // Expande via pílula
        const pilula = document.body.querySelector(".wpp-pilula") as HTMLButtonElement
        pilula.click()
        await wrapper.vm.$nextTick()

        expect(document.body.querySelector(".wpp-widget")).not.toBeNull()
        expect(mockListar()).toHaveBeenCalledWith(42)

        wrapper.unmount()
    })

    // CA198 ─────────────────────────────────────────────────────────────────────
    it("CA198 — clicar em fechar emite evento 'fechar'", async () => {
        const wrapper = montarWidget()
        await wrapper.vm.$nextTick()

        const btnFechar = document.body.querySelectorAll(".wpp-btn-icone")[1] as HTMLButtonElement
        btnFechar.click()
        await wrapper.vm.$nextTick()

        expect(wrapper.emitted("fechar")).toBeTruthy()
        wrapper.unmount()
    })

    // CA196 ─────────────────────────────────────────────────────────────────────
    it("CA196 — re-fetch quando rotaAtual muda (usuário voltou à página)", async () => {
        const wrapper = montarWidget()
        await wrapper.vm.$nextTick()

        mockListar().mockClear()

        await wrapper.setProps({ rotaAtual: "/pacientes/42?aba=documentos" })
        await wrapper.vm.$nextTick()

        expect(mockListar()).toHaveBeenCalledWith(42)
        wrapper.unmount()
    })

    // Contador atualiza após re-fetch ───────────────────────────────────────────
    it("contador atualiza para '1 de 2 concluídas' após re-fetch que remove uma ação", async () => {
        mockListar().mockResolvedValue([
            { id: 10, evolucaoId: 5, acao: "AgendarRetorno", status: "Pendente", criadoEm: "" },
        ])

        const wrapper = montarWidget()
        await wrapper.vm.$nextTick()
        // Aguarda a promise do buscarAbertas inicial
        await new Promise(r => setTimeout(r, 0))
        await wrapper.vm.$nextTick()

        const contador = document.body.querySelector(".wpp-contador")
        expect(contador?.textContent?.trim()).toBe("1 de 2 concluídas")

        wrapper.unmount()
    })

    // Pílula mostra contador compacto ───────────────────────────────────────────
    it("pílula mostra contador '0/2' quando nenhuma foi concluída", async () => {
        const wrapper = montarWidget()
        // Aguarda fetch inicial
        await new Promise(r => setTimeout(r, 0))
        await wrapper.vm.$nextTick()

        const btnMin = document.body.querySelectorAll(".wpp-btn-icone")[0] as HTMLButtonElement
        btnMin.click()
        await wrapper.vm.$nextTick()

        const pilula = document.body.querySelector(".wpp-pilula-contador")
        expect(pilula?.textContent?.trim()).toBe("0/2")

        wrapper.unmount()
    })

    // CA191 ─────────────────────────────────────────────────────────────────────
    it("CA191 — com acoesMarcadas vazia, lista não exibe itens", () => {
        const wrapper = montarWidget({ acoesMarcadas: [] as AcaoPendencia[] })

        const itens = document.body.querySelectorAll(".wpp-item")
        expect(itens.length).toBe(0)

        wrapper.unmount()
    })
})
