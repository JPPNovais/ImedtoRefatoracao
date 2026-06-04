import { describe, it, expect, beforeEach, vi } from "vitest"
import { nextTick } from "vue"
import { useProfissaoEspecialidade } from "./useProfissaoEspecialidade"

/**
 * Testes do composable useProfissaoEspecialidade — cobertura dos CAs do briefing 2026-06-04_003:
 * CA3: trocar profissão limpa especialidade
 * CA11: profissaoTemEspecialidades, estados de loading
 * CA12: conselhoSigla derivado da profissão
 */

const mocks = vi.hoisted(() => ({
    catalogoService: {
        listarProfissoes: vi.fn(),
        listarEspecialidades: vi.fn(),
    },
}))

vi.mock("@/services/catalogoService", () => ({ catalogoService: mocks.catalogoService }))

const PROFISSOES = [
    { id: 1, nome: "Médico", conselhoSigla: "CRM", ativo: true },
    { id: 2, nome: "Dentista", conselhoSigla: "CRO", ativo: true },
    { id: 3, nome: "Nutricionista", conselhoSigla: null, ativo: true },
]

const ESPECIALIDADES_MEDICO = [
    { id: 10, profissaoId: 1, profissaoNome: "Médico", nome: "Dermatologia", ativo: true },
    { id: 11, profissaoId: 1, profissaoNome: "Médico", nome: "Cardiologia", ativo: true },
]

describe("useProfissaoEspecialidade", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        mocks.catalogoService.listarProfissoes.mockResolvedValue(PROFISSOES)
        mocks.catalogoService.listarEspecialidades.mockResolvedValue(ESPECIALIDADES_MEDICO)
    })

    it("carregarProfissoes preenche profissoes via catalogoService", async () => {
        const { profissoes, carregarProfissoes } = useProfissaoEspecialidade()
        await carregarProfissoes()
        expect(profissoes.value).toHaveLength(3)
        expect(mocks.catalogoService.listarProfissoes).toHaveBeenCalled()
    })

    describe("CA3 — trocar profissão limpa especialidade", () => {
        it("ao mudar profissaoId limpa especialidade e carrega novas especialidades", async () => {
            const { profissaoId, especialidade } = useProfissaoEspecialidade()

            // Situação inicial: profissão + especialidade definidas
            profissaoId.value = 1
            especialidade.value = "Dermatologia"
            await nextTick()

            // Troca de profissão
            profissaoId.value = 2
            await nextTick()
            await nextTick() // flush do watch assíncrono

            expect(especialidade.value).toBe("")
        })

        it("setar profissaoId pela primeira vez (undefined→value) também dispara carregamento", async () => {
            const { profissaoId, especialidades } = useProfissaoEspecialidade()

            profissaoId.value = 1
            await nextTick()
            await nextTick()

            expect(mocks.catalogoService.listarEspecialidades).toHaveBeenCalledWith(1)
        })
    })

    describe("CA11 — profissaoTemEspecialidades", () => {
        it("false quando profissaoId é null", async () => {
            const { profissaoTemEspecialidades } = useProfissaoEspecialidade()
            expect(profissaoTemEspecialidades.value).toBe(false)
        })

        it("true após carregar especialidades para a profissão", async () => {
            const { profissaoId, profissaoTemEspecialidades } = useProfissaoEspecialidade()
            profissaoId.value = 1
            await nextTick()
            await nextTick()
            expect(profissaoTemEspecialidades.value).toBe(true)
        })

        it("false quando profissão não tem especialidades no catálogo (após promise resolver)", async () => {
            mocks.catalogoService.listarEspecialidades.mockResolvedValue([])
            const { profissaoId, profissaoTemEspecialidades } = useProfissaoEspecialidade()
            profissaoId.value = 3
            // Aguarda: watch dispara (nextTick), promise resolve (microtask), Vue reativa (nextTick)
            await nextTick()
            await Promise.resolve()
            await nextTick()
            expect(profissaoTemEspecialidades.value).toBe(false)
        })
    })

    describe("CA12 — conselhoSigla derivado da profissão", () => {
        it("retorna sigla correta quando profissão tem conselhoSigla", async () => {
            const { profissoes, profissaoId, conselhoSigla, carregarProfissoes } = useProfissaoEspecialidade()
            await carregarProfissoes()
            profissaoId.value = 1
            await nextTick()
            expect(conselhoSigla.value).toBe("CRM")
        })

        it("muda de CRM para CRO ao trocar profissão", async () => {
            const { profissoes, profissaoId, conselhoSigla, carregarProfissoes } = useProfissaoEspecialidade()
            await carregarProfissoes()
            profissaoId.value = 1
            await nextTick()
            expect(conselhoSigla.value).toBe("CRM")

            profissaoId.value = 2
            await nextTick()
            expect(conselhoSigla.value).toBe("CRO")
        })

        it("null quando profissão não tem conselhoSigla", async () => {
            const { profissoes, profissaoId, conselhoSigla, carregarProfissoes } = useProfissaoEspecialidade()
            await carregarProfissoes()
            profissaoId.value = 3  // Nutricionista — conselhoSigla: null
            await nextTick()
            expect(conselhoSigla.value).toBeNull()
        })

        it("null quando nenhuma profissão selecionada", () => {
            const { conselhoSigla } = useProfissaoEspecialidade()
            expect(conselhoSigla.value).toBeNull()
        })
    })

    describe("inicializarComVinculo — pré-seleção sem limpar", () => {
        it("mantém especialidade após inicializar com profissão+especialidade", async () => {
            mocks.catalogoService.listarEspecialidades.mockResolvedValue(ESPECIALIDADES_MEDICO)
            const { profissaoId, especialidade, inicializarComVinculo } = useProfissaoEspecialidade()

            await inicializarComVinculo(1, "Dermatologia")

            expect(profissaoId.value).toBe(1)
            expect(especialidade.value).toBe("Dermatologia")
        })

        it("profissao nula → limpa profissao e especialidade", async () => {
            const { profissaoId, especialidade, inicializarComVinculo } = useProfissaoEspecialidade()
            await inicializarComVinculo(null, null)
            expect(profissaoId.value).toBeNull()
            expect(especialidade.value).toBe("")
        })
    })

    describe("reset — limpa tudo", () => {
        it("reset limpa profissaoId, especialidade e especialidades", async () => {
            const { profissaoId, especialidade, especialidades, reset } = useProfissaoEspecialidade()
            profissaoId.value = 1
            especialidade.value = "Dermatologia"
            await nextTick()
            await nextTick()

            reset()
            await nextTick()

            expect(profissaoId.value).toBeNull()
            expect(especialidade.value).toBe("")
            expect(especialidades.value).toHaveLength(0)
        })
    })
})
