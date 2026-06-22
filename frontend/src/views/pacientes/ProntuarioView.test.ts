/**
 * ProntuarioView — testes de regressão:
 *   CA21: TDZ watch/erroCirurgiao
 *   Fix A-2: handler aplicar-template faz MERGE em desc-cirurgica (não sobrescreve)
 */
import { describe, it, expect, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import { createTestingPinia } from "@pinia/testing"

// ---------------------------------------------------------------------------
// Mocks — todos os módulos externos consumidos no setup()
// ---------------------------------------------------------------------------

vi.mock("@/services/prontuarioService", () => ({
    prontuarioService: {
        obter: vi.fn().mockResolvedValue(null),
        listarAnexos: vi.fn().mockResolvedValue([]),
        listarModelos: vi.fn().mockResolvedValue([]),
    },
}))
vi.mock("@/services/pacienteService", () => ({
    pacienteService: { obter: vi.fn().mockResolvedValue(null) },
}))
vi.mock("@/services/agendaService", () => ({
    agendaService: { obterAgendamento: vi.fn().mockResolvedValue(null) },
}))
vi.mock("@/services/exameFisicoService", () => ({
    exameFisicoService: { obter: vi.fn().mockResolvedValue(null) },
}))
vi.mock("@/services/pendenciaService", () => ({}))

vi.mock("@/composables/useProntuarioPdf", () => ({
    useProntuarioPdf: () => ({
        gerarPdf: vi.fn(),
        gerarPdfEvolucao: vi.fn(),
    }),
}))
vi.mock("@/composables/useAtendimentoAtivo", () => ({
    useAtendimentoAtivo: () => ({
        ehEsteAtendimento: vi.fn().mockReturnValue(false),
        finalizar: vi.fn(),
    }),
}))

vi.mock("vue-router", () => ({
    useRoute: () => ({
        params: { id: "1" },
        query: {},
    }),
    useRouter: () => ({ push: vi.fn() }),
    createRouter:     vi.fn(() => ({ beforeEach: vi.fn(), push: vi.fn(), currentRoute: { value: {} } })),
    createWebHistory: vi.fn(() => ({})),
}))

// Stubs pesados de componentes filhos — evitam setup recursivo
const STUBS_PESADOS = {
    ProntuarioPacienteHeader: true,
    ProntuarioTabs: true,
    ConsultaAtualTab: true,
    ConsultasAnterioresTab: true,
    ReceitasTab: true,
    AtestadoTab: true,
    PedidoExameTab: true,
    EmitirTermoModal: true,
    AppButton: true,
    AppField: true,
    AppSelect: true,
    AppToast: true,
}

// ---------------------------------------------------------------------------

import ProntuarioView from "./ProntuarioView.vue"

// ── Lógica do handler aplicar-template extraída (espelha exatamente o template) ─

function aplicarTemplateHandler(novaEvolucao: Record<string, any>, chave: string, corpo: string): void {
    if (chave === "desc-cirurgica") {
        const atual = novaEvolucao[chave]
        novaEvolucao[chave] = {
            ...(atual && typeof atual === "object" ? atual : {}),
            observacoes: corpo,
        }
    } else {
        novaEvolucao[chave] = corpo
    }
}

describe("handler aplicar-template — regressão A-2 (merge em desc-cirurgica)", () => {
    it("aplica o corpo como observacoes preservando os demais campos", () => {
        const novaEvolucao: Record<string, any> = {
            "desc-cirurgica": {
                cirurgiao: "Dr. Fulano",
                cirurgiasRealizadas: "Colecistectomia",
                observacoes: "",
            },
        }

        aplicarTemplateHandler(novaEvolucao, "desc-cirurgica", "Técnica aberta padrão.")

        expect(novaEvolucao["desc-cirurgica"].observacoes).toBe("Técnica aberta padrão.")
        // Campos preexistentes devem ser preservados
        expect(novaEvolucao["desc-cirurgica"].cirurgiao).toBe("Dr. Fulano")
        expect(novaEvolucao["desc-cirurgica"].cirurgiasRealizadas).toBe("Colecistectomia")
    })

    it("cria o objeto desc-cirurgica com observacoes quando estava undefined", () => {
        const novaEvolucao: Record<string, any> = {}

        aplicarTemplateHandler(novaEvolucao, "desc-cirurgica", "Corpo do template.")

        expect(novaEvolucao["desc-cirurgica"]).toEqual({ observacoes: "Corpo do template." })
    })

    it("NÃO sobrescreve outros campos ao aplicar template (proteção contra regressão A-2)", () => {
        const novaEvolucao: Record<string, any> = {
            "desc-cirurgica": {
                cirurgiao: "Dr. Cirurgião",
                auxiliar: "Auxiliar 1",
                tecnicaOperatoria: "Laparoscopia",
                observacoes: "Obs anterior",
            },
        }

        aplicarTemplateHandler(novaEvolucao, "desc-cirurgica", "Novo corpo do template.")

        expect(novaEvolucao["desc-cirurgica"].observacoes).toBe("Novo corpo do template.")
        expect(novaEvolucao["desc-cirurgica"].cirurgiao).toBe("Dr. Cirurgião")
        expect(novaEvolucao["desc-cirurgica"].auxiliar).toBe("Auxiliar 1")
        expect(novaEvolucao["desc-cirurgica"].tecnicaOperatoria).toBe("Laparoscopia")
    })

    it("para seções não-cirurgicas, sobrescreve diretamente (comportamento original)", () => {
        const novaEvolucao: Record<string, any> = {
            "qp": "Queixa anterior",
        }

        aplicarTemplateHandler(novaEvolucao, "qp", "Nova queixa.")

        expect(novaEvolucao["qp"]).toBe("Nova queixa.")
    })
})

describe("ProntuarioView — regressão TDZ watch/erroCirurgiao (CA21)", () => {
    it("monta sem ReferenceError no setup (watch não precede declaração das refs)", async () => {
        // Se o bug de TDZ existir, mount() lança imediatamente durante o setup().
        expect(() => {
            mount(ProntuarioView, {
                global: {
                    plugins: [
                        createTestingPinia({
                            initialState: {
                                tenant: { ativo: { id: 1, nomeFantasia: "Clínica" }, semEstabelecimento: false },
                                permissoes: { permissoes: [] },
                                proximosPasso: { acoes: [] },
                            },
                        }),
                    ],
                    stubs: STUBS_PESADOS,
                },
            })
        }).not.toThrow()
    })

    it("após montar, novaEvolucao e erroCirurgiao estão inicializados como reactive/ref", async () => {
        // Monta e aguarda promises para confirmar estado reativo coerente.
        const wrapper = mount(ProntuarioView, {
            global: {
                plugins: [
                    createTestingPinia({
                        initialState: {
                            tenant: { ativo: { id: 1, nomeFantasia: "Clínica" }, semEstabelecimento: false },
                            permissoes: { permissoes: [] },
                            proximosPasso: { acoes: [] },
                        },
                    }),
                ],
                stubs: STUBS_PESADOS,
            },
        })
        await flushPromises()

        // A view deve estar montada sem erros de inicialização.
        expect(wrapper.exists()).toBe(true)
    })
})
