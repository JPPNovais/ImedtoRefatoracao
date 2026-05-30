import { describe, it, expect, vi, beforeEach } from "vitest"
import { setActivePinia, createPinia } from "pinia"
import { useEstabelecimentosStore } from "./estabelecimentosStore"
import * as service from "../services/estabelecimentosService"
import type {
    PaginaEstabelecimentosAdminDto,
    EstabelecimentoAdminDetalheDto,
    CpfDonoReveladoDto,
} from "../services/estabelecimentosService"

vi.mock("../services/estabelecimentosService", () => ({
    estabelecimentosService: {
        listar: vi.fn(),
        obter: vi.fn(),
        revelarCpfDono: vi.fn(),
        resetTenant: vi.fn(),
    },
}))

const mockService = service.estabelecimentosService as {
    listar: ReturnType<typeof vi.fn>
    obter: ReturnType<typeof vi.fn>
    revelarCpfDono: ReturnType<typeof vi.fn>
    resetTenant: ReturnType<typeof vi.fn>
}

describe("estabelecimentosStore", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    // ── carregarLista ────────────────────────────────────────────────────────

    it("carregarLista: popula itens e total em sucesso", async () => {
        const mockPagina: PaginaEstabelecimentosAdminDto = {
            itens: [
                {
                    id: 1, nomeFantasia: "Clínica A", razaoSocial: "R.S.", cnpj: "", status: "Ativo",
                    donoNome: "João", donoEmail: "joao@ex.com", donoCpfMascarado: "123.***.***-00",
                    planoNome: "Grátis", criadoEm: "2024-01-01T00:00:00Z",
                    totalProfissionaisAtivos: 2, totalPacientes: 10, agendamentosNoMes: 5,
                },
            ],
            total: 1,
            pagina: 1,
            tamanhoPagina: 25,
        }
        mockService.listar.mockResolvedValue(mockPagina)

        const store = useEstabelecimentosStore()
        await store.carregarLista()

        expect(store.itens).toHaveLength(1)
        expect(store.total).toBe(1)
        expect(store.erroLista).toBeNull()
        expect(store.carregandoLista).toBe(false)
    })

    it("carregarLista: seta erroLista em falha de rede", async () => {
        mockService.listar.mockRejectedValue(new Error("Falha"))

        const store = useEstabelecimentosStore()
        await store.carregarLista()

        expect(store.itens).toHaveLength(0)
        expect(store.erroLista).toBeTruthy()
        expect(store.carregandoLista).toBe(false)
    })

    // ── carregarDetalhe ──────────────────────────────────────────────────────

    it("carregarDetalhe: popula detalhe em sucesso", async () => {
        const mockDetalhe: EstabelecimentoAdminDetalheDto = {
            id: 1, nomeFantasia: "Clínica A", razaoSocial: "R.S.", cnpj: "", status: "Ativo",
            telefone: null, email: null, cidade: null, estado: null, criadoEm: "2024-01-01T00:00:00Z",
            donoUsuarioId: "uuid-123", donoNome: "João", donoEmail: "joao@ex.com",
            donoCpfMascarado: "123.***.***-00", planoNome: "Grátis",
            assinaturaGratuita: false, assinaturaDataFim: null,
            totalProfissionaisAtivos: 2, totalPacientes: 10, agendamentosNoMes: 5, totalProntuarios: 8,
        }
        mockService.obter.mockResolvedValue(mockDetalhe)

        const store = useEstabelecimentosStore()
        await store.carregarDetalhe(1)

        expect(store.detalhe).not.toBeNull()
        expect(store.detalhe?.nomeFantasia).toBe("Clínica A")
        expect(store.erroDetalhe).toBeNull()
    })

    it("carregarDetalhe: seta erroDetalhe em falha", async () => {
        mockService.obter.mockRejectedValue(new Error("404"))

        const store = useEstabelecimentosStore()
        await store.carregarDetalhe(999)

        expect(store.detalhe).toBeNull()
        expect(store.erroDetalhe).toBeTruthy()
    })

    // ── revelarCpf ───────────────────────────────────────────────────────────

    it("revelarCpf: armazena cpf revelado em sucesso", async () => {
        const mockCpf: CpfDonoReveladoDto = { cpf: "123.456.789-00" }
        mockService.revelarCpfDono.mockResolvedValue(mockCpf)

        const store = useEstabelecimentosStore()
        const ok = await store.revelarCpf(1, "Verificar cadastro do parceiro")

        expect(ok).toBe(true)
        expect(store.cpfRevelado).toBe("123.456.789-00")
        expect(store.erroRevelarCpf).toBeNull()
    })

    it("revelarCpf: seta erro em falha e retorna false", async () => {
        mockService.revelarCpfDono.mockRejectedValue({
            response: { data: { mensagem: "Motivo obrigatório." } },
        })

        const store = useEstabelecimentosStore()
        const ok = await store.revelarCpf(1, "")

        expect(ok).toBe(false)
        expect(store.cpfRevelado).toBeNull()
        expect(store.erroRevelarCpf).toBeTruthy()
    })

    it("limparCpfRevelado: limpa cpf e erro", () => {
        const store = useEstabelecimentosStore()
        store.cpfRevelado = "123.456.789-00"
        store.erroRevelarCpf = "algum erro"

        store.limparCpfRevelado()

        expect(store.cpfRevelado).toBeNull()
        expect(store.erroRevelarCpf).toBeNull()
    })

    // ── resetTenant ──────────────────────────────────────────────────────────

    it("resetTenant: retorna true em sucesso", async () => {
        mockService.resetTenant.mockResolvedValue(undefined)

        const store = useEstabelecimentosStore()
        const ok = await store.resetTenant(1, "Reset do demo antes do treinamento", "Clínica A")

        expect(ok).toBe(true)
        expect(store.erroReset).toBeNull()
        expect(mockService.resetTenant).toHaveBeenCalledWith(1, "Reset do demo antes do treinamento", "Clínica A")
    })

    it("resetTenant: seta erro e retorna false em falha", async () => {
        mockService.resetTenant.mockRejectedValue({
            response: { data: { mensagem: "Nome fantasia não confere." } },
        })

        const store = useEstabelecimentosStore()
        const ok = await store.resetTenant(1, "Motivo de reset", "Nome Errado")

        expect(ok).toBe(false)
        expect(store.erroReset).toBe("Nome fantasia não confere.")
    })
})
