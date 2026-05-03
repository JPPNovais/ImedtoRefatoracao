import { describe, it, expect, beforeEach, afterEach, vi } from "vitest"
import { buscarCep } from "./viaCep"

describe("buscarCep", () => {
    let fetchMock: ReturnType<typeof vi.fn>

    beforeEach(() => {
        fetchMock = vi.fn()
        globalThis.fetch = fetchMock as any
    })

    afterEach(() => {
        vi.restoreAllMocks()
    })

    it("retorna null se CEP tem menos de 8 digitos", async () => {
        const r = await buscarCep("123")
        expect(r).toBeNull()
        expect(fetchMock).not.toHaveBeenCalled()
    })

    it("retorna null se CEP eh nulo/vazio", async () => {
        expect(await buscarCep("")).toBeNull()
        expect(await buscarCep(null as any)).toBeNull()
    })

    it("normaliza CEP mascarado para somente digitos", async () => {
        fetchMock.mockResolvedValue({
            ok: true,
            json: async () => ({ cep: "01310-100", logradouro: "Av Paulista" }),
        })
        await buscarCep("01310-100")
        expect(fetchMock).toHaveBeenCalledWith("https://viacep.com.br/ws/01310100/json/")
    })

    it("retorna endereco normalizado quando ViaCEP responde 200", async () => {
        fetchMock.mockResolvedValue({
            ok: true,
            json: async () => ({
                cep: "01310-100",
                logradouro: "Av Paulista",
                complemento: "lado par",
                bairro: "Bela Vista",
                localidade: "Sao Paulo",
                uf: "SP",
            }),
        })

        const r = await buscarCep("01310100")
        expect(r).not.toBeNull()
        expect(r!.uf).toBe("SP")
        expect(r!.logradouro).toBe("Av Paulista")
    })

    it("retorna null quando ViaCEP devolve erro:true (CEP inexistente)", async () => {
        fetchMock.mockResolvedValue({ ok: true, json: async () => ({ erro: true }) })
        expect(await buscarCep("99999999")).toBeNull()
    })

    it("retorna null quando fetch lanca", async () => {
        fetchMock.mockRejectedValue(new Error("network"))
        expect(await buscarCep("01310100")).toBeNull()
    })

    it("retorna null quando ViaCEP responde nao-ok", async () => {
        fetchMock.mockResolvedValue({ ok: false, json: async () => ({}) })
        expect(await buscarCep("01310100")).toBeNull()
    })

    it("preenche campos faltantes com string vazia", async () => {
        fetchMock.mockResolvedValue({
            ok: true,
            json: async () => ({ cep: "12345678" }),
        })
        const r = await buscarCep("12345678")
        expect(r!.logradouro).toBe("")
        expect(r!.uf).toBe("")
    })
})
