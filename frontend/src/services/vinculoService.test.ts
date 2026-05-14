import { describe, it, expect, beforeEach, vi } from "vitest"
import { setActivePinia, createPinia } from "pinia"

vi.mock("@/services/httpClient", () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
    },
}))

import httpClient from "@/services/httpClient"
import { useTenantStore } from "@/stores/tenantStore"
import { vinculoService } from "./vinculoService"

/**
 * Bug #1 (LGPD) — garante que os seletores de profissional usados em
 * agenda/prontuario/orcamento batam no endpoint PUBLICO/minimizado, nao
 * no endpoint completo (que vaza email/permissoes/datas).
 */
describe("vinculoService", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.mocked(httpClient.get).mockReset()

        const tenant = useTenantStore()
        tenant.ativo = {
            id: 42,
            nomeFantasia: "Clínica X",
            papel: "Profissional",
            permissoes: [],
            permissoesExtras: [],
        }
    })

    describe("listarProfissionaisPublico (Bug #1 — LGPD)", () => {
        it("chama o endpoint /publico — DTO minimizado para seletores", async () => {
            vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [] })

            await vinculoService.listarProfissionaisPublico()

            expect(httpClient.get).toHaveBeenCalledWith(
                "/estabelecimento/42/profissionais/publico",
            )
        })

        it("retorna array vazio sem chamar a API quando nao ha tenant", async () => {
            const tenant = useTenantStore()
            tenant.ativo = null

            const r = await vinculoService.listarProfissionaisPublico()

            expect(r).toEqual([])
            expect(httpClient.get).not.toHaveBeenCalled()
        })
    })

    describe("listarProfissionais (DTO completo — somente equipe)", () => {
        it("chama o endpoint completo — restrito a Dono/equipe.ver no backend", async () => {
            vi.mocked(httpClient.get).mockResolvedValueOnce({ data: [] })

            await vinculoService.listarProfissionais({ incluirInativos: true })

            expect(httpClient.get).toHaveBeenCalledWith(
                "/estabelecimento/42/profissionais",
                { params: { incluirInativos: true } },
            )
        })
    })
})
