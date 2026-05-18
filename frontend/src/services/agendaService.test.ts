import { describe, it, expect, beforeEach, vi } from "vitest"

vi.mock("@/services/httpClient", () => ({
    default: {
        get:    vi.fn(),
        post:   vi.fn(),
        put:    vi.fn(),
        delete: vi.fn(),
    },
}))

import httpClient from "@/services/httpClient"
import { agendaService } from "./agendaService"

describe("agendaService — sala/check-in", () => {
    beforeEach(() => {
        vi.mocked(httpClient.post).mockReset()
        vi.mocked(httpClient.put).mockReset()
    })

    it("registrarCheckIn sem salaId: POST sem body", async () => {
        vi.mocked(httpClient.post).mockResolvedValueOnce({ data: null })
        await agendaService.registrarCheckIn(11)
        expect(httpClient.post).toHaveBeenCalledWith("/agendamentos/11/checkin", undefined)
    })

    it("registrarCheckIn com salaId: POST com body { salaId }", async () => {
        vi.mocked(httpClient.post).mockResolvedValueOnce({ data: null })
        await agendaService.registrarCheckIn(11, 5)
        expect(httpClient.post).toHaveBeenCalledWith("/agendamentos/11/checkin", { salaId: 5 })
    })

    it("registrarCheckIn com salaId=null: POST com body { salaId: null } (desalocar no check-in)", async () => {
        vi.mocked(httpClient.post).mockResolvedValueOnce({ data: null })
        await agendaService.registrarCheckIn(11, null)
        expect(httpClient.post).toHaveBeenCalledWith("/agendamentos/11/checkin", { salaId: null })
    })

    it("alocarSala: PUT /agendamentos/{id}/sala com { salaId }", async () => {
        vi.mocked(httpClient.put).mockResolvedValueOnce({ data: null })
        await agendaService.alocarSala(7, 11, 5)
        expect(httpClient.put).toHaveBeenCalledWith("/agendamentos/11/sala", { salaId: 5 })
    })

    it("alocarSala(null): PUT envia salaId=null (desalocar)", async () => {
        vi.mocked(httpClient.put).mockResolvedValueOnce({ data: null })
        await agendaService.alocarSala(7, 11, null)
        expect(httpClient.put).toHaveBeenCalledWith("/agendamentos/11/sala", { salaId: null })
    })
})
