/**
 * Testes do EditarAgendamentoModal.
 *
 * Cobre:
 * - CA13: aviso de re-confirmação (testes originais).
 * - Atalho "Editar dados" no header do paciente (CA2, CA3, CA5, CA6).
 */
import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import { createTestingPinia } from "@pinia/testing"
import EditarAgendamentoModal from "./EditarAgendamentoModal.vue"
import type { Agendamento } from "@/services/agendaService"

const mockObterPaciente = vi.fn()

// Mock do tenantStore com objeto mutável — testes alteram tenantMock.papel antes de montar.
const tenantMock = { estabelecimentoAtivoId: 1, papel: "Dono" as "Dono" | "Profissional" | null }

vi.mock("@/stores/tenantStore", () => ({
    useTenantStore: vi.fn(() => tenantMock),
}))

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

vi.mock("@/services/pacienteService", () => ({
    pacienteService: {
        obter: (id: number) => mockObterPaciente(id),
    },
}))

const PROFISSIONAL_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
const PROFISSIONAL_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"

const PACIENTE_COMPLETO = {
    id: 10,
    estabelecimentoId: 1,
    nomeCompleto: "Maria Silva",
    cpf: "123.456.789-09",
    documentoInternacional: null,
    dataNascimento: null,
    genero: "F",
    telefone: "(11) 99999-0000",
    email: null,
    endereco: null,
    observacoes: null,
    tags: [] as string[],
    alertas: [] as string[],
    criadoEm: "2026-01-01T00:00:00Z",
    atualizadoEm: null,
    whatsappLembreteOptIn: false,
}

function agendamentoBase(overrides: Partial<Agendamento> = {}): Agendamento {
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

function agendamentoConfirmado(overrides: Partial<Agendamento> = {}): Agendamento {
    return agendamentoBase(overrides)
}

function agendamentoAgendado(overrides: Partial<Agendamento> = {}): Agendamento {
    return agendamentoBase({ status: "Agendado", ...overrides })
}

async function criarWrapper(
    agendamento: Agendamento | null,
    opcoes: { focoReagendar?: boolean; pacienteAtualizado?: any } = {},
) {
    const { focoReagendar = false, pacienteAtualizado } = opcoes

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
            ...(pacienteAtualizado !== undefined ? { pacienteAtualizado } : {}),
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

beforeEach(() => {
    tenantMock.papel = "Dono"
    mockObterPaciente.mockResolvedValue(PACIENTE_COMPLETO)
})

describe("EditarAgendamentoModal — CA13: aviso de re-confirmação", () => {
    it("NAO exibe aviso quando agendamento esta Agendado (independente de alteração)", async () => {
        const w = await criarWrapper(agendamentoAgendado(), { focoReagendar: true })
        expect(w.find(".aviso-reagendamento").exists()).toBe(false)
    })

    it("NAO exibe aviso quando agendamento Confirmado sem alteracoes de horario/profissional", async () => {
        const w = await criarWrapper(agendamentoConfirmado())
        expect(w.find(".aviso-reagendamento").exists()).toBe(false)
    })

    it("exibe aviso quando Confirmado e usuario seleciona nova hora", async () => {
        const w = await criarWrapper(agendamentoConfirmado(), { focoReagendar: true })

        const vm = w.vm as any
        const origHora = vm.form.origHora
        vm.form.hora = origHora === "10:00" ? "11:00" : "10:00"
        await w.vm.$nextTick()

        expect(w.find(".aviso-reagendamento").exists()).toBe(true)
    })

    it("exibe aviso quando Confirmado e data eh alterada", async () => {
        const w = await criarWrapper(agendamentoConfirmado(), { focoReagendar: true })

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

describe("EditarAgendamentoModal — atalho Editar dados (CA2/CA3/CA5/CA6)", () => {
    it("CA5: podeEditarPaciente é true quando papel é Dono", async () => {
        tenantMock.papel = "Dono"
        const w = await criarWrapper(agendamentoConfirmado())
        const vm = w.vm as any
        expect(vm.podeEditarPaciente).toBe(true)
    })

    it("CA5: podeEditarPaciente é true quando papel é Profissional", async () => {
        tenantMock.papel = "Profissional"
        const w = await criarWrapper(agendamentoConfirmado())
        const vm = w.vm as any
        expect(vm.podeEditarPaciente).toBe(true)
    })

    it("CA6: podeEditarPaciente é false quando papel é null (recepção sem papel explícito)", async () => {
        tenantMock.papel = null
        const w = await criarWrapper(agendamentoConfirmado())
        const vm = w.vm as any
        expect(vm.podeEditarPaciente).toBe(false)
    })

    it("CA2: clicar em Editar dados chama pacienteService.obter e emite 'editar-paciente'", async () => {
        tenantMock.papel = "Dono"
        mockObterPaciente.mockResolvedValue(PACIENTE_COMPLETO)
        const w = await criarWrapper(agendamentoConfirmado())
        const vm = w.vm as any

        await vm.solicitarEdicaoPaciente()
        await w.vm.$nextTick()

        expect(mockObterPaciente).toHaveBeenCalledWith(10)
        const emitidos = w.emitted("editar-paciente")
        expect(emitidos).toBeTruthy()
        if (emitidos) {
            expect(emitidos[0][0]).toMatchObject({ id: 10, nomeCompleto: "Maria Silva" })
        }
    })

    it("CA2: quando pacienteAtualizado já está disponível (mesmo ID), reutiliza sem nova chamada ao backend", async () => {
        tenantMock.papel = "Dono"
        mockObterPaciente.mockClear()
        const pacienteAtualizado = { ...PACIENTE_COMPLETO, nomeCompleto: "Maria Santos" }
        const w = await criarWrapper(agendamentoConfirmado(), { pacienteAtualizado })
        const vm = w.vm as any

        await vm.solicitarEdicaoPaciente()
        await w.vm.$nextTick()

        // Não deve ter feito nova chamada ao backend
        expect(mockObterPaciente).not.toHaveBeenCalled()
        const emitidos = w.emitted("editar-paciente")
        expect(emitidos).toBeTruthy()
        if (emitidos) {
            expect(emitidos[0][0]).toMatchObject({ nomeCompleto: "Maria Santos" })
        }
    })

    it("CA3: pacienteNomeLocal reflete nome atualizado via prop pacienteAtualizado", async () => {
        tenantMock.papel = "Dono"
        const w = await criarWrapper(agendamentoConfirmado())
        const vm = w.vm as any

        // Antes da atualização
        expect(vm.pacienteNomeLocal).toBe("Maria Silva")

        // Atualiza via prop (simula pai passando paciente editado de volta)
        const pacienteAtualizado = { ...PACIENTE_COMPLETO, nomeCompleto: "Maria Santos" }
        await w.setProps({ pacienteAtualizado })
        await w.vm.$nextTick()

        // Após atualização, nome deve refletir o novo valor
        expect(vm.pacienteNomeLocal).toBe("Maria Santos")
    })

    it("CA3: form.profissionalUsuarioId permanece intacto após atualização do paciente", async () => {
        tenantMock.papel = "Dono"
        const w = await criarWrapper(agendamentoConfirmado())
        const vm = w.vm as any
        const profOriginal = vm.form.profissionalUsuarioId

        await w.setProps({ pacienteAtualizado: { ...PACIENTE_COMPLETO, nomeCompleto: "Maria Santos" } })
        await w.vm.$nextTick()

        expect(vm.form.profissionalUsuarioId).toBe(profOriginal)
        expect(vm.form.tipo).toBe("Consulta")
    })
})
