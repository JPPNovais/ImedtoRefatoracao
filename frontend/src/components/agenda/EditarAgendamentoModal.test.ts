/**
 * Testes do EditarAgendamentoModal — CA13: aviso de re-confirmação.
 *
 * Foco comportamental: o aviso ".aviso-reagendamento" aparece quando o agendamento
 * está Confirmado E o usuário altera horário ou profissional, e NÃO aparece quando
 * altera apenas observações/tipo.
 */
import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import { createTestingPinia } from "@pinia/testing"
import EditarAgendamentoModal from "./EditarAgendamentoModal.vue"
import type { Agendamento } from "@/services/agendaService"

// Mocks de dependências externas
vi.mock("@/services/agendaService", () => ({
    agendaService: {
        consultarDisponibilidade: vi.fn().mockResolvedValue({ dias: [] }),
        atualizar: vi.fn().mockResolvedValue(undefined),
        alocarSala: vi.fn().mockResolvedValue(undefined),
    },
}))

vi.mock("@/services/salaService", () => ({
    salaService: {
        listar: vi.fn().mockResolvedValue([]),
    },
}))

vi.mock("@/stores/tenantStore", () => ({
    useTenantStore: vi.fn(() => ({
        estabelecimentoAtivoId: 1,
    })),
}))

const PROFISSIONAL_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
const PROFISSIONAL_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"

function agendamentoConfirmado(overrides: Partial<Agendamento> = {}): Agendamento {
    return {
        id: 1,
        pacienteId: 10,
        pacienteNome: "Maria Silva",
        profissionalUsuarioId: PROFISSIONAL_A,
        profissionalNome: "Dr. João",
        tipoServico: "Consulta",
        observacoes: null,
        status: "Confirmado",
        inicioPrevisto: new Date(Date.now() + 2 * 60 * 60 * 1000).toISOString(),
        fimPrevisto: new Date(Date.now() + 3 * 60 * 60 * 1000).toISOString(),
        criadoEm: new Date().toISOString(),
        lembretePorEmailEnviado: false,
        checkInEm: null,
        salaId: null,
        ...overrides,
    } as Agendamento
}

function agendamentoAgendado(overrides: Partial<Agendamento> = {}): Agendamento {
    return agendamentoConfirmado({ status: "Agendado", ...overrides })
}

async function criarWrapper(agendamento: Agendamento | null, focoReagendar = false) {
    // Monta com aberto=false para permitir que o watch dispare ao setar aberto=true,
    // garantindo que inicializar() seja chamado e form.profissionalUsuarioId seja setado.
    const w = mount(EditarAgendamentoModal, {
        props: {
            aberto: false,
            agendamento,
            profissionais: [
                { usuarioId: PROFISSIONAL_A, nomeCompleto: "Dr. João", fotoUrl: null, status: "Ativo" },
                { usuarioId: PROFISSIONAL_B, nomeCompleto: "Dra. Ana", fotoUrl: null, status: "Ativo" },
            ],
            focoReagendar,
        },
        global: {
            plugins: [createTestingPinia({ createSpy: vi.fn })],
            stubs: {
                AppAvatarSelect: {
                    template: '<div class="avatar-select-stub" />',
                    props: ["modelValue", "opcoes"],
                    emits: ["update:modelValue"],
                },
            },
        },
    })
    // Aciona o watch setando aberto=true, o que chama inicializar() e popula form.
    await w.setProps({ aberto: true })
    await w.vm.$nextTick()
    return w
}

describe("EditarAgendamentoModal — CA13: aviso de re-confirmação", () => {
    it("NAO exibe aviso quando agendamento esta Agendado (independente de alteração)", async () => {
        const w = await criarWrapper(agendamentoAgendado(), true)
        expect(w.find(".aviso-reagendamento").exists()).toBe(false)
    })

    it("NAO exibe aviso quando agendamento Confirmado sem alteracoes de horario/profissional", async () => {
        const w = await criarWrapper(agendamentoConfirmado())
        // Não altera nada — form foi inicializado com valores do agendamento
        expect(w.find(".aviso-reagendamento").exists()).toBe(false)
    })

    it("exibe aviso quando Confirmado e usuario seleciona nova hora", async () => {
        const w = await criarWrapper(agendamentoConfirmado(), true)

        const vm = w.vm as any
        const origHora = vm.form.origHora
        // Muda para horário diferente do original
        vm.form.hora = origHora === "10:00" ? "11:00" : "10:00"
        await w.vm.$nextTick()

        expect(w.find(".aviso-reagendamento").exists()).toBe(true)
    })

    it("exibe aviso quando Confirmado e data eh alterada", async () => {
        const w = await criarWrapper(agendamentoConfirmado(), true)

        const vm = w.vm as any
        const [ano, mes, dia] = vm.form.origData.split("-").map(Number)
        const novaData = new Date(ano, mes - 1, dia + 1)
        vm.form.data = `${novaData.getFullYear()}-${String(novaData.getMonth() + 1).padStart(2, "0")}-${String(novaData.getDate()).padStart(2, "0")}`
        await w.vm.$nextTick()

        expect(w.find(".aviso-reagendamento").exists()).toBe(true)
    })

    it("NAO exibe aviso quando Confirmado altera so observacoes (sem mudar horario/profissional)", async () => {
        const w = await criarWrapper(agendamentoConfirmado())

        const vm = w.vm as any
        // Altera apenas observações, mantendo horário e profissional idênticos
        vm.form.observacoes = "nova observação"
        await w.vm.$nextTick()

        expect(w.find(".aviso-reagendamento").exists()).toBe(false)
    })

    it("NAO exibe aviso quando Confirmado altera so tipo de servico", async () => {
        const w = await criarWrapper(agendamentoConfirmado())

        const vm = w.vm as any
        vm.form.tipo = "Retorno"
        await w.vm.$nextTick()

        expect(w.find(".aviso-reagendamento").exists()).toBe(false)
    })
})
