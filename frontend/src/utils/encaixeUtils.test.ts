import { describe, it, expect } from "vitest"
import { calcularPodeEncaixar } from "./encaixeUtils"
import type { DisponibilidadeDia } from "@/services/agendaService"

// Fábrica de DisponibilidadeDia mínima para os testes
function makeDia(overrides: Partial<DisponibilidadeDia> = {}): DisponibilidadeDia {
    return {
        data: "2026-06-03",
        diaSemana: "TER",
        status: "disponivel",
        slots: [],
        ...overrides,
    }
}

describe("calcularPodeEncaixar", () => {
    // (a) CA1 — agenda cheia de "agendado" dentro do expediente → HABILITADO
    // Bug corrigido: antes retornava false porque s.disponivel era false para "agendado"
    it("retorna true quando todos slots futuros têm motivo agendado e status é indisponivel", () => {
        const dia = makeDia({
            status: "indisponivel",
            slots: [
                { hora: "08:00", disponivel: false, motivo: "passado", pacienteNome: null },
                { hora: "08:30", disponivel: false, motivo: "passado", pacienteNome: null },
                { hora: "14:00", disponivel: false, motivo: "agendado", pacienteNome: "Ana" },
                { hora: "14:30", disponivel: false, motivo: "agendado", pacienteNome: "Bruno" },
                { hora: "15:00", disponivel: false, motivo: "agendado", pacienteNome: "Carla" },
                { hora: "18:00", disponivel: false, motivo: "agendado", pacienteNome: "Diego" },
            ],
        })
        // horaAtual = 14:05 — slot vigente = 14:00 (agendado), há futuros não-passados
        expect(calcularPodeEncaixar(dia, "14:05")).toBe(true)
    })

    // (b) expediente encerrado (todos slots passado, sem nenhum futuro não-passado) → DESABILITADO
    it("retorna false quando todos slots têm motivo passado e não há slots futuros", () => {
        const dia = makeDia({
            status: "indisponivel",
            slots: [
                { hora: "08:00", disponivel: false, motivo: "passado", pacienteNome: null },
                { hora: "08:30", disponivel: false, motivo: "passado", pacienteNome: null },
                { hora: "17:30", disponivel: false, motivo: "passado", pacienteNome: null },
            ],
        })
        // horaAtual = 18:30 — após o último slot, nenhum futuro não-passado
        expect(calcularPodeEncaixar(dia, "18:30")).toBe(false)
    })

    // (c) status "fechado" (dia não-funcional/feriado) → DESABILITADO
    it("retorna false quando status do dia é fechado", () => {
        const dia = makeDia({ status: "fechado", slots: [] })
        expect(calcularPodeEncaixar(dia, "10:00")).toBe(false)
    })

    // (d) slot vigente com motivo "bloqueado" (intervalo/almoço) → DESABILITADO
    it("retorna false quando slot vigente tem motivo bloqueado", () => {
        const dia = makeDia({
            status: "disponivel",
            slots: [
                { hora: "08:00", disponivel: true, motivo: null, pacienteNome: null },
                { hora: "12:00", disponivel: false, motivo: "bloqueado", pacienteNome: null },
                { hora: "13:00", disponivel: true, motivo: null, pacienteNome: null },
            ],
        })
        // horaAtual = 12:15 — slot vigente = 12:00 (bloqueado)
        expect(calcularPodeEncaixar(dia, "12:15")).toBe(false)
    })

    // (e) dia funcional dentro do expediente com slot disponível → HABILITADO
    it("retorna true quando há slot disponível futuro dentro do expediente", () => {
        const dia = makeDia({
            status: "disponivel",
            slots: [
                { hora: "08:00", disponivel: false, motivo: "passado", pacienteNome: null },
                { hora: "10:00", disponivel: false, motivo: "agendado", pacienteNome: "Eva" },
                { hora: "10:30", disponivel: true, motivo: null, pacienteNome: null },
                { hora: "11:00", disponivel: true, motivo: null, pacienteNome: null },
            ],
        })
        // horaAtual = 10:05 — slot vigente = 10:00 (agendado, não bloqueado), há disponíveis à frente
        expect(calcularPodeEncaixar(dia, "10:05")).toBe(true)
    })

    // Regressão: dia null (falha de consulta) → falha-fechada não-bloqueante → HABILITADO
    it("retorna true quando dia é null (falha na consulta)", () => {
        expect(calcularPodeEncaixar(null, "10:00")).toBe(true)
    })

    // Regressão: antes do primeiro slot (ex: 07:00 antes de 08:00) → DESABILITADO
    it("retorna false quando horaAtual é anterior ao primeiro slot", () => {
        const dia = makeDia({
            status: "disponivel",
            slots: [
                { hora: "08:00", disponivel: true, motivo: null, pacienteNome: null },
            ],
        })
        expect(calcularPodeEncaixar(dia, "07:00")).toBe(false)
    })
})
