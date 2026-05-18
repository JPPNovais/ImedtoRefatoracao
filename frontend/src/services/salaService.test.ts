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
import { salaService } from "./salaService"

describe("salaService", () => {
    beforeEach(() => {
        vi.mocked(httpClient.get).mockReset()
        vi.mocked(httpClient.put).mockReset()
    })

    it("listar sem filtro: GET sem params", async () => {
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [] })
        await salaService.listar(7)
        expect(httpClient.get).toHaveBeenCalledWith(
            "/estabelecimento/7/salas",
            { params: undefined },
        )
    })

    it("listar(apenasAtivas=true): GET com query string apenasAtivas=true", async () => {
        vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [] })
        await salaService.listar(7, true)
        expect(httpClient.get).toHaveBeenCalledWith(
            "/estabelecimento/7/salas",
            { params: { apenasAtivas: true } },
        )
    })

    it("desativar: PUT /salas/{id}/desativar", async () => {
        vi.mocked(httpClient.put).mockResolvedValueOnce({ data: null })
        await salaService.desativar(7, 42)
        expect(httpClient.put).toHaveBeenCalledWith("/estabelecimento/7/salas/42/desativar")
    })

    it("reativar: PUT /salas/{id}/reativar", async () => {
        vi.mocked(httpClient.put).mockResolvedValueOnce({ data: null })
        await salaService.reativar(7, 42)
        expect(httpClient.put).toHaveBeenCalledWith("/estabelecimento/7/salas/42/reativar")
    })
})
