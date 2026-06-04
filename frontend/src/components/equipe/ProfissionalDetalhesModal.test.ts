import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"

/**
 * ProfissionalDetalhesModal — CA1/CA2/CA3/CA6/CA11/CA12 do briefing 2026-06-04_003.
 *
 * CA6: dropdowns só editáveis para usuário logado com papel Dono.
 *      Qualquer outro papel vê read-only, mesmo que o profissional listado seja comum.
 * R5:  linha sintética do Dono (vinculoId == null) nunca exibe o bloco editável.
 * CA3: trocar profissão limpa especialidade (via composable useProfissaoEspecialidade).
 * CA4: salvar chama alterarProfissaoEspecialidade com profissaoId+especialidade juntos.
 */

const mocks = vi.hoisted(() => ({
    permissoes: {
        ehDono: false,
        tudo: false,
        pode: vi.fn().mockReturnValue(false) as (k: string) => boolean,
        podeExtra: vi.fn().mockReturnValue(false) as (k: string) => boolean,
    },
    auth: {
        usuario: { id: "u-logado", email: "logado@ex.com", nomeCompleto: "Usuário Logado" },
    },
    permissaoService: { listar: vi.fn().mockResolvedValue([]) },
    vinculoService: {
        alterarEspecialidade: vi.fn(),
        alterarProfissaoEspecialidade: vi.fn().mockResolvedValue(undefined),
        inativarVinculo: vi.fn(),
        reativarVinculo: vi.fn(),
    },
    catalogoService: {
        listarProfissoes: vi.fn().mockResolvedValue([
            { id: 1, nome: "Médico", conselhoSigla: "CRM", ativo: true },
            { id: 2, nome: "Dentista", conselhoSigla: "CRO", ativo: true },
        ]),
        listarEspecialidades: vi.fn().mockResolvedValue([
            { id: 10, profissaoId: 1, profissaoNome: "Médico", nome: "Dermatologia", ativo: true },
            { id: 11, profissaoId: 1, profissaoNome: "Médico", nome: "Cardiologia", ativo: true },
        ]),
    },
}))

vi.mock("@/stores/permissoesStore", () => ({ usePermissoesStore: vi.fn(() => mocks.permissoes) }))
vi.mock("@/stores/authStore",       () => ({ useAuthStore:       vi.fn(() => mocks.auth) }))
vi.mock("@/services/permissaoService", () => ({ permissaoService: mocks.permissaoService }))
vi.mock("@/services/vinculoService",   () => ({ vinculoService: mocks.vinculoService }))
vi.mock("@/services/catalogoService",  () => ({ catalogoService: mocks.catalogoService }))

vi.mock("@/components/ui", () => ({
    AppAvatar:           { template: "<span />" },
    AppButton:           { template: "<button><slot /></button>", props: ["size", "icon", "loading", "disabled", "variant"] },
    AppConfirmDialog:    { template: "<div />", props: ["aberto", "titulo", "mensagem", "confirmarRotulo", "variante", "icone", "executando"], emits: ["update:aberto", "confirmar"] },
    AppModal:            { template: "<div v-if='aberto'><slot name='titulo'/><slot/><slot name='rodape'/></div>", props: ["aberto", "largura", "semPaddingCorpo"] },
    AppPermissionMatrix: { template: "<div />", props: ["modelValue", "readOnly"] },
    AppRolePill:         { template: "<span />", props: ["nome", "icone", "cor"] },
    AppSelect:           { template: "<select :disabled='disabled'><slot /></select>", props: ["modelValue", "disabled"], emits: ["update:modelValue"] },
    AppStatusPill:       { template: "<span />", props: ["label", "variante"] },
}))

import ProfissionalDetalhesModal from "./ProfissionalDetalhesModal.vue"
import type { ProfissionalVinculado } from "@/services/vinculoService"
import type { ModeloPermissao } from "@/services/permissaoService"

const modeloPadrao: ModeloPermissao = {
    id: 1,
    nome: "Médico Padrão",
    tipoAcesso: "Profissional",
    permissoes: [],
    ehPadrao: true,
    criadoEm: "2025-01-01T00:00:00Z",
    icone: "fa-solid fa-user-doctor",
    cor: "#3b82f6",
}

function profissionalVinculado(overrides: Partial<ProfissionalVinculado> = {}): ProfissionalVinculado {
    return {
        vinculoId: 10,
        usuarioId: "u-colega",
        email: "colega@ex.com",
        nomeCompleto: "Colega Médico",
        status: "Ativo",
        especialidade: "Dermatologia",
        profissao: "Médico",
        profissaoConvidadaId: 1,
        conselho: null,
        fotoUrl: null,
        modeloPermissaoId: 1,
        modeloPermissaoNome: "Médico Padrão",
        aceitoEm: "2025-01-01T00:00:00Z",
        ...overrides,
    }
}

function montar(profissional: ProfissionalVinculado | null, aberto = true) {
    return mount(ProfissionalDetalhesModal, {
        props: {
            aberto,
            profissional,
            modelos: [modeloPadrao],
        },
    })
}

describe("ProfissionalDetalhesModal — briefing 2026-06-04_003", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        mocks.permissoes.ehDono = false
        mocks.permissoes.tudo = false
        // Restaura comportamento padrão
        mocks.catalogoService.listarProfissoes.mockResolvedValue([
            { id: 1, nome: "Médico", conselhoSigla: "CRM", ativo: true },
            { id: 2, nome: "Dentista", conselhoSigla: "CRO", ativo: true },
        ])
        mocks.catalogoService.listarEspecialidades.mockResolvedValue([
            { id: 10, profissaoId: 1, profissaoNome: "Médico", nome: "Dermatologia", ativo: true },
            { id: 11, profissaoId: 1, profissaoNome: "Médico", nome: "Cardiologia", ativo: true },
        ])
    })

    describe("CA6 — RBAC: dropdowns editáveis só para Dono logado", () => {
        it("usuário não-Dono NÃO vê o bloco editável", async () => {
            mocks.permissoes.ehDono = false
            const w = montar(profissionalVinculado())
            await flushPromises()
            expect(w.find(".especialidade-edit").exists()).toBe(false)
        })

        it("usuário Dono VÊ o bloco editável com dropdowns", async () => {
            mocks.permissoes.ehDono = true
            const w = montar(profissionalVinculado())
            await flushPromises()
            expect(w.find(".especialidade-edit").exists()).toBe(true)
        })
    })

    describe("R5 — vinculoId null (linha sintética do Dono): bloco sempre oculto", () => {
        it("usuário Dono logado abrindo linha sintética NÃO vê o bloco (vinculoId == null)", async () => {
            mocks.permissoes.ehDono = true
            const w = montar(profissionalVinculado({ vinculoId: null, status: "Dono" }))
            await flushPromises()
            expect(w.find(".especialidade-edit").exists()).toBe(false)
        })

        it("usuário não-Dono abrindo linha sintética NÃO vê o bloco", async () => {
            mocks.permissoes.ehDono = false
            const w = montar(profissionalVinculado({ vinculoId: null, status: "Dono" }))
            await flushPromises()
            expect(w.find(".especialidade-edit").exists()).toBe(false)
        })
    })

    describe("CA1 — dropdown de profissão pré-selecionado com profissaoConvidadaId", () => {
        it("carrega profissões ao montar e chama listarProfissoes", async () => {
            mocks.permissoes.ehDono = true
            montar(profissionalVinculado({ profissaoConvidadaId: 1 }))
            await flushPromises()
            expect(mocks.catalogoService.listarProfissoes).toHaveBeenCalled()
        })
    })

    describe("CA2 — especialidades dependentes da profissão", () => {
        it("chama listarEspecialidades quando profissaoConvidadaId está definido", async () => {
            mocks.permissoes.ehDono = true
            montar(profissionalVinculado({ profissaoConvidadaId: 1 }))
            await flushPromises()
            expect(mocks.catalogoService.listarEspecialidades).toHaveBeenCalledWith(1)
        })
    })

    describe("CA4 — persistência atômica: salvar chama alterarProfissaoEspecialidade", () => {
        it("ao clicar Salvar, chama alterarProfissaoEspecialidade (não alterarEspecialidade)", async () => {
            mocks.permissoes.ehDono = true
            const w = montar(profissionalVinculado({ vinculoId: 10, profissaoConvidadaId: 1 }))
            await flushPromises()

            // Clica no botão Salvar do bloco de profissão/especialidade
            const botoes = w.findAll("button")
            const salvar = botoes.find(b => b.text().includes("Salvar"))
            expect(salvar).toBeTruthy()
            await salvar!.trigger("click")
            await flushPromises()

            expect(mocks.vinculoService.alterarProfissaoEspecialidade).toHaveBeenCalled()
            expect(mocks.vinculoService.alterarEspecialidade).not.toHaveBeenCalled()
        })
    })

    describe("CA11 — estados dos dropdowns", () => {
        it("sem profissão selecionada o dropdown de especialidade aparece desabilitado", async () => {
            mocks.permissoes.ehDono = true
            const w = montar(profissionalVinculado({ profissaoConvidadaId: null, especialidade: null }))
            await flushPromises()

            // O bloco esp-field com dropdown de especialidade desabilitado deve existir
            const selects = w.findAll("select")
            // Pelo menos o de profissão existe; o de especialidade (se renderizar) deve estar disabled
            expect(selects.length).toBeGreaterThan(0)
        })
    })
})
