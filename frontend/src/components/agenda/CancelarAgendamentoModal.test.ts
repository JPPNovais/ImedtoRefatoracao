import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"

// Mocka o barrel do design system pra evitar resolver @imedto/ui (pacote externo).
vi.mock("@/components/ui", () => {
    const AppModal = {
        props: ["aberto", "largura", "titulo"],
        emits: ["fechar"],
        template: `
            <div v-if="aberto" data-test="modal">
                <slot name="titulo" />
                <slot />
                <div data-test="rodape"><slot name="rodape" /></div>
            </div>
        `,
    }
    const AppButton = {
        props: ["variant", "icon", "loading", "disabled"],
        emits: ["click"],
        template: `<button :disabled="disabled || loading" :data-loading="loading || undefined" @click="$emit('click')"><slot /></button>`,
    }
    return { AppModal, AppButton }
})

vi.mock("@/services/agendaService", () => ({
    agendaService: {
        cancelar: vi.fn(),
    },
}))

vi.mock("@/utils/datetime", () => ({
    formatDataHora: (iso: string) => iso,  // determinístico, sem locale.
}))

import CancelarAgendamentoModal from "./CancelarAgendamentoModal.vue"
import { agendaService } from "@/services/agendaService"

const AGENDAMENTO_PADRAO = {
    id: 42,
    estabelecimentoId: 1,
    pacienteId: 7,
    pacienteNome: "João Silva",
    profissionalUsuarioId: "prof-1",
    profissionalNome: "Dra. Ana",
    criadoPorNome: "Dono",
    inicioPrevisto: "2026-05-14T09:00:00Z",
    fimPrevisto:    "2026-05-14T09:30:00Z",
    tipoServico:    "Consulta",
    observacoes:    null,
    status:         "Agendado" as const,
    motivoCancelamento: null,
    criadoEm:       "2026-05-13T10:00:00Z",
    atualizadoEm:   null,
    checkInEm:      null,
    salaId:         null,
    salaNome:       null,
    salaTipoNome:   null,
}

function montar(aberto = true) {
    return mount(CancelarAgendamentoModal, {
        props: { aberto, agendamento: AGENDAMENTO_PADRAO },
    })
}

function botoesMotivo(wrapper: ReturnType<typeof montar>) {
    return wrapper.findAll(".motivos button.motivo-btn")
}

function botaoCancelar(wrapper: ReturnType<typeof montar>) {
    // "Voltar" é o primeiro botão do rodapé; "Cancelar agendamento" é o segundo.
    return wrapper.findAll("[data-test='rodape'] button")[1]
}

function botaoVoltar(wrapper: ReturnType<typeof montar>) {
    return wrapper.findAll("[data-test='rodape'] button")[0]
}

describe("CancelarAgendamentoModal", () => {
    beforeEach(() => {
        vi.mocked(agendaService.cancelar).mockReset()
        vi.mocked(agendaService.cancelar).mockResolvedValue(undefined)
    })

    it("renderiza os 6 motivos pré-definidos", () => {
        const w = montar()
        const botoes = botoesMotivo(w)

        expect(botoes).toHaveLength(6)
        // Garante que pelo menos os labels conhecidos estão presentes (snapshot semântico).
        const labels = botoes.map(b => b.text())
        expect(labels).toEqual(expect.arrayContaining([
            "Paciente desistiu",
            "Reagendado",
            "Sem comparecimento",
            "Emergência médica",
            "Profissional indisponível",
            "Outro",
        ]))
    })

    it("botão 'Cancelar agendamento' começa desabilitado (sem motivo selecionado)", () => {
        const w = montar()
        expect((botaoCancelar(w).element as HTMLButtonElement).disabled).toBe(true)
    })

    it("seleciona motivo + clica → chama agendaService.cancelar com apenas o label", async () => {
        const w = montar()
        const desistiu = botoesMotivo(w).find(b => b.text() === "Paciente desistiu")!
        await desistiu.trigger("click")
        await botaoCancelar(w).trigger("click")
        await flushPromises()

        expect(agendaService.cancelar).toHaveBeenCalledTimes(1)
        expect(agendaService.cancelar).toHaveBeenCalledWith(42, "Paciente desistiu")
    })

    it("seleciona motivo + observação → concatena com ' — '", async () => {
        const w = montar()
        const desistiu = botoesMotivo(w).find(b => b.text() === "Paciente desistiu")!
        await desistiu.trigger("click")

        const obs = w.find("textarea.obs-input")
        await obs.setValue("paciente avisou ontem")
        await botaoCancelar(w).trigger("click")
        await flushPromises()

        expect(agendaService.cancelar).toHaveBeenCalledWith(
            42, "Paciente desistiu — paciente avisou ontem",
        )
    })

    it("trimma observação antes de concatenar (só espaços = sem observação)", async () => {
        const w = montar()
        await botoesMotivo(w).find(b => b.text() === "Reagendado")!.trigger("click")
        await w.find("textarea.obs-input").setValue("   ")
        await botaoCancelar(w).trigger("click")
        await flushPromises()

        expect(agendaService.cancelar).toHaveBeenCalledWith(42, "Reagendado")
    })

    it("a observação tem maxlength 400 no DOM (UX corta a digitação)", () => {
        const w = montar()
        const obs = w.find("textarea.obs-input")
        expect(obs.attributes("maxlength")).toBe("400")
    })

    it("respeita o limite de 500 chars do motivo final (slice defensivo)", async () => {
        const w = montar()
        await botoesMotivo(w).find(b => b.text() === "Outro")!.trigger("click")
        // Como o textarea tem maxlength 400, simulamos um valor pelo store (caso CSP/script
        // bypasse o maxlength) — programaticamente setamos string longa e validamos slice.
        const obs = w.find("textarea.obs-input")
        const longa = "x".repeat(600)
        await obs.setValue(longa)
        await botaoCancelar(w).trigger("click")
        await flushPromises()

        const motivoEnviado = vi.mocked(agendaService.cancelar).mock.calls[0][1]
        expect(motivoEnviado.length).toBeLessThanOrEqual(500)
    })

    it("erro 422 do backend exibe a mensagem inline (sem alert)", async () => {
        vi.mocked(agendaService.cancelar).mockRejectedValueOnce({
            response: { status: 422, data: { mensagem: "Motivo é obrigatório." } },
        })

        const w = montar()
        await botoesMotivo(w).find(b => b.text() === "Outro")!.trigger("click")
        await botaoCancelar(w).trigger("click")
        await flushPromises()

        expect(w.text()).toContain("Motivo é obrigatório.")
        // Não emite 'cancelado' em erro.
        expect(w.emitted("cancelado")).toBeFalsy()
    })

    it("usa fallback se backend não enviar mensagem", async () => {
        vi.mocked(agendaService.cancelar).mockRejectedValueOnce(new Error("network"))
        const w = montar()
        await botoesMotivo(w).find(b => b.text() === "Reagendado")!.trigger("click")
        await botaoCancelar(w).trigger("click")
        await flushPromises()

        expect(w.text()).toContain("Erro ao cancelar agendamento.")
    })

    it("emite 'cancelado' em sucesso", async () => {
        const w = montar()
        await botoesMotivo(w).find(b => b.text() === "Sem comparecimento")!.trigger("click")
        await botaoCancelar(w).trigger("click")
        await flushPromises()

        expect(w.emitted("cancelado")).toBeTruthy()
        expect(w.emitted("cancelado")).toHaveLength(1)
    })

    it("ESC/clique em 'Voltar' enquanto executando NÃO fecha o modal", async () => {
        // Bloqueia a Promise para manter o estado 'executando'.
        let resolver: () => void = () => {}
        vi.mocked(agendaService.cancelar).mockReturnValueOnce(
            new Promise<void>((r) => { resolver = r }),
        )

        const w = montar()
        await botoesMotivo(w).find(b => b.text() === "Outro")!.trigger("click")
        await botaoCancelar(w).trigger("click")
        await flushPromises()
        // Aqui está em executando=true; clique em "Voltar" deve ser ignorado.

        await botaoVoltar(w).trigger("click")
        await flushPromises()

        expect(w.emitted("fechar")).toBeFalsy()

        // Limpeza: resolve a Promise pra não vazar entre testes.
        resolver()
        await flushPromises()
    })

    it("emite 'fechar' quando Voltar é clicado em estado idle", async () => {
        const w = montar()
        await botaoVoltar(w).trigger("click")
        expect(w.emitted("fechar")).toBeTruthy()
    })

    it("reseta seleção e observação quando 'aberto' vira false e abre de novo", async () => {
        const w = montar()
        await botoesMotivo(w).find(b => b.text() === "Outro")!.trigger("click")
        await w.find("textarea.obs-input").setValue("alguma coisa")

        await w.setProps({ aberto: false, agendamento: AGENDAMENTO_PADRAO })
        await w.setProps({ aberto: true, agendamento: AGENDAMENTO_PADRAO })

        // Sem motivo selecionado: botão desabilita.
        expect((botaoCancelar(w).element as HTMLButtonElement).disabled).toBe(true)
        // Textarea limpa.
        expect((w.find("textarea.obs-input").element as HTMLTextAreaElement).value).toBe("")
    })
})
