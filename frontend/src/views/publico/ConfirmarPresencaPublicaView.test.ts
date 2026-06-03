import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"

// Mocks antes do import da view.
vi.mock("@/services/agendamentoConfirmacaoPublicaService", () => ({
    agendamentoConfirmacaoPublicaService: {
        consultar: vi.fn(),
        confirmar: vi.fn(),
    },
}))

vi.mock("vue-router", () => ({
    useRoute: () => ({ params: { token: "token-valido-abc123" } }),
}))

import { agendamentoConfirmacaoPublicaService } from "@/services/agendamentoConfirmacaoPublicaService"
import ConfirmarPresencaPublicaView from "./ConfirmarPresencaPublicaView.vue"

const DTO_VALIDO = {
    estabelecimentoNome: "Clínica Imedto",
    profissionalNome:    "Dr. Fulano",
    tipoServico:         "Consulta",
    inicioPrevisto:      "2026-06-10T14:00:00Z",
    fimPrevisto:         "2026-06-10T15:00:00Z",
    statusAgendamento:   "Agendado",
}

/**
 * CA25 — estados da página pública de confirmação de presença:
 *   carregando / válido / confirmado / inválido-expirado.
 */
describe("ConfirmarPresencaPublicaView — CA25", () => {
    beforeEach(() => {
        vi.mocked(agendamentoConfirmacaoPublicaService.consultar).mockReset()
        vi.mocked(agendamentoConfirmacaoPublicaService.confirmar).mockReset()
    })

    // ── estado válido: mostra resumo mínimo + botão ───────────────────────

    it("estado válido: renderiza resumo sem PII e exibe botão 'Confirmar presença'", async () => {
        vi.mocked(agendamentoConfirmacaoPublicaService.consultar).mockResolvedValueOnce(DTO_VALIDO)

        const wrapper = mount(ConfirmarPresencaPublicaView)
        await flushPromises()

        const texto = wrapper.text()
        expect(texto).toContain("Clínica Imedto")
        expect(texto).toContain("Dr. Fulano")
        expect(texto).toContain("Consulta")
        expect(texto).toContain("Confirmar presença")
        // CA17/CA23: sem dados sensíveis
        expect(texto).not.toContain("paciente_id")
        expect(texto).not.toContain("CPF")
    })

    // ── estado inválido/expirado: 410 → mensagem genérica (CA19) ──────────

    it("estado expirado: 410 do GET → mensagem genérica 'Link inválido ou expirado'", async () => {
        vi.mocked(agendamentoConfirmacaoPublicaService.consultar).mockRejectedValueOnce({
            response: { status: 410, data: { mensagem: "Este link expirou." } },
        })

        const wrapper = mount(ConfirmarPresencaPublicaView)
        await flushPromises()

        expect(wrapper.text()).toContain("Link inválido ou expirado")
    })

    // ── estado confirmado após clicar no botão (CA18) ─────────────────────

    it("após clicar 'Confirmar presença': chama confirmar() e mostra 'Presença confirmada'", async () => {
        vi.mocked(agendamentoConfirmacaoPublicaService.consultar).mockResolvedValueOnce(DTO_VALIDO)
        vi.mocked(agendamentoConfirmacaoPublicaService.confirmar).mockResolvedValueOnce({
            resultado: "confirmado",
            mensagem:  "Presença confirmada com sucesso! Você pode fechar esta página.",
        })

        const wrapper = mount(ConfirmarPresencaPublicaView)
        await flushPromises()

        const btn = wrapper.find("button")
        expect(btn.exists()).toBe(true)
        await btn.trigger("click")
        await flushPromises()

        expect(agendamentoConfirmacaoPublicaService.confirmar).toHaveBeenCalledWith("token-valido-abc123")
        expect(wrapper.text()).toContain("Presença confirmada")
    })

    // ── CA20: idempotência — já_confirmado também mostra "Presença confirmada" ──

    it("CA20: POST retorna ja_confirmado → mostra estado confirmado", async () => {
        vi.mocked(agendamentoConfirmacaoPublicaService.consultar).mockResolvedValueOnce(DTO_VALIDO)
        vi.mocked(agendamentoConfirmacaoPublicaService.confirmar).mockResolvedValueOnce({
            resultado: "ja_confirmado",
            mensagem:  "Presença já confirmada. Você pode fechar esta página.",
        })

        const wrapper = mount(ConfirmarPresencaPublicaView)
        await flushPromises()

        const btn = wrapper.find("button")
        await btn.trigger("click")
        await flushPromises()

        expect(wrapper.text()).toContain("Presença confirmada")
    })

    // ── estado carregando: spinner visível antes da resposta ─────────────

    it("estado carregando: exibe spinner enquanto GET está em andamento", async () => {
        // Promessa nunca resolve durante este teste — foca em estado inicial.
        vi.mocked(agendamentoConfirmacaoPublicaService.consultar).mockReturnValueOnce(
            new Promise(() => {}),
        )

        const wrapper = mount(ConfirmarPresencaPublicaView)
        // Sem flushPromises — estado deve ser "carregando".
        expect(wrapper.find(".aceite-spinner").exists()).toBe(true)
    })

    // ── erro técnico (5xx) ─────────────────────────────────────────────────

    it("erro técnico 500: exibe mensagem de erro técnico", async () => {
        vi.mocked(agendamentoConfirmacaoPublicaService.consultar).mockRejectedValueOnce({
            response: { status: 500 },
        })

        const wrapper = mount(ConfirmarPresencaPublicaView)
        await flushPromises()

        expect(wrapper.text()).toContain("Não foi possível carregar este link")
    })
})
