import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"

/**
 * ProfissionalDetalhesModal — RBAC do campo especialidade editável.
 *
 * CA7: campo editável só aparece para usuário logado com papel Dono.
 *      Qualquer outro papel (Profissional, Recepção) deve ver o campo oculto,
 *      mesmo que o profissional listado no modal seja um colega comum.
 *
 * CA9: a linha sintética do Dono (vinculoId == null) nunca exibe o campo,
 *      independentemente do papel do usuário logado.
 */

// Mocks içados — consultados pelos factories abaixo.
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
        inativarVinculo: vi.fn(),
        reativarVinculo: vi.fn(),
    },
}))

vi.mock("@/stores/permissoesStore", () => ({ usePermissoesStore: vi.fn(() => mocks.permissoes) }))
vi.mock("@/stores/authStore",       () => ({ useAuthStore:       vi.fn(() => mocks.auth) }))
vi.mock("@/services/permissaoService", () => ({ permissaoService: mocks.permissaoService }))
vi.mock("@/services/vinculoService",   () => ({ vinculoService: mocks.vinculoService }))

// Stub dos componentes de UI para simplificar a renderização nos testes.
vi.mock("@/components/ui", () => ({
    AppAvatar:           { template: "<span />" },
    AppButton:           { template: "<button><slot /></button>", props: ["size", "icon", "loading", "disabled", "variant"] },
    AppConfirmDialog:    { template: "<div />", props: ["aberto", "titulo", "mensagem", "confirmarRotulo", "variante", "icone", "executando"], emits: ["update:aberto", "confirmar"] },
    AppInput:            { template: "<input />", props: ["modelValue", "disabled", "placeholder", "maxlength"], emits: ["update:modelValue"] },
    AppModal:            { template: "<div v-if='aberto'><slot name='titulo'/><slot/><slot name='rodape'/></div>", props: ["aberto", "largura", "semPaddingCorpo"] },
    AppPermissionMatrix: { template: "<div />", props: ["modelValue", "readOnly"] },
    AppRolePill:         { template: "<span />", props: ["nome", "icone", "cor"] },
    AppSelect:           { template: "<select><slot /></select>", props: ["modelValue", "disabled"] },
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

describe("ProfissionalDetalhesModal — podeEditarEspecialidade", () => {
    beforeEach(() => {
        vi.clearAllMocks()
        mocks.permissoes.ehDono = false
        mocks.permissoes.tudo = false
    })

    describe("CA7 — RBAC: campo só visível para o usuário logado com papel Dono", () => {
        it("usuário não-Dono NÃO vê o campo editável ao abrir modal de profissional vinculado", async () => {
            mocks.permissoes.ehDono = false

            const w = montar(profissionalVinculado())
            await flushPromises()

            // O bloco v-if="podeEditarEspecialidade" usa a classe .especialidade-edit.
            expect(w.find(".especialidade-edit").exists()).toBe(false)
        })

        it("usuário Dono VÊ o campo editável ao abrir modal de profissional vinculado", async () => {
            mocks.permissoes.ehDono = true

            const w = montar(profissionalVinculado())
            await flushPromises()

            expect(w.find(".especialidade-edit").exists()).toBe(true)
        })
    })

    describe("CA9 — vinculoId null (linha sintética do Dono): campo sempre oculto", () => {
        it("usuário Dono logado abrindo a própria linha sintética NÃO vê o campo (vinculoId == null)", async () => {
            mocks.permissoes.ehDono = true

            // Linha do Dono no backend usa vinculoId == null (UNION sintético).
            const w = montar(profissionalVinculado({ vinculoId: null, status: "Dono" }))
            await flushPromises()

            expect(w.find(".especialidade-edit").exists()).toBe(false)
        })

        it("usuário não-Dono abrindo linha sintética NÃO vê o campo", async () => {
            mocks.permissoes.ehDono = false

            const w = montar(profissionalVinculado({ vinculoId: null, status: "Dono" }))
            await flushPromises()

            expect(w.find(".especialidade-edit").exists()).toBe(false)
        })
    })
})
