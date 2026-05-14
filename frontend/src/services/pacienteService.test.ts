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
import { pacienteService } from "./pacienteService"

describe("pacienteService.buscaRapida — Correção 5 (autocomplete LGPD-friendly)", () => {
    beforeEach(() => {
        vi.mocked(httpClient.get).mockReset()
    })

    it("chama GET /paciente/busca-rapida com q e limite", async () => {
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [] })

        await pacienteService.buscaRapida("maria", 20)

        expect(httpClient.get).toHaveBeenCalledWith("/paciente/busca-rapida", {
            params: { q: "maria", limite: 20 },
        })
    })

    it("sem q (undefined) envia params.q = undefined", async () => {
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [] })

        await pacienteService.buscaRapida(undefined, 10)

        expect(httpClient.get).toHaveBeenCalledWith("/paciente/busca-rapida", {
            params: { q: undefined, limite: 10 },
        })
    })

    it("q string vazia → params.q = undefined (axios omite da query string)", async () => {
        // O service faz `q || undefined`. String vazia é falsy.
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [] })

        await pacienteService.buscaRapida("", 10)

        expect(httpClient.get).toHaveBeenCalledWith("/paciente/busca-rapida", {
            params: { q: undefined, limite: 10 },
        })
    })

    it("limite default é 10 quando não passado", async () => {
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [] })

        await pacienteService.buscaRapida("ana")

        expect(httpClient.get).toHaveBeenCalledWith("/paciente/busca-rapida", {
            params: { q: "ana", limite: 10 },
        })
    })

    it("retorna apenas {id, nomeCompleto} — não desserializa CPF/telefone", async () => {
        // Defensive: mesmo que o backend mande PII por engano, o tipo TS limita o
        // consumo (cliente só lê .id e .nomeCompleto). Aqui validamos o pass-through.
        vi.mocked(httpClient.get).mockResolvedValueOnce({
            data: [
                { id: 1, nomeCompleto: "Maria Souza" },
                { id: 2, nomeCompleto: "Marcos Rocha" },
            ],
        })

        const r = await pacienteService.buscaRapida("ma", 5)

        expect(r).toHaveLength(2)
        expect(r[0]).toEqual({ id: 1, nomeCompleto: "Maria Souza" })
        expect(r[1]).toEqual({ id: 2, nomeCompleto: "Marcos Rocha" })
    })

    it("propaga erro do backend (cliente decide UX)", async () => {
        vi.mocked(httpClient.get).mockRejectedValueOnce(new Error("500"))

        await expect(pacienteService.buscaRapida("x", 10)).rejects.toThrow("500")
    })
})
