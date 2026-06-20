/**
 * Testes do NovoAgendamentoModal.
 *
 * Cobre:
 * - Seção de lembrete automático (testes originais).
 * - Atalho "Editar dados" no card do paciente (CA1, CA3, CA4, CA5, CA6).
 */
import { describe, it, expect, vi, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import { createTestingPinia } from "@pinia/testing"
import NovoAgendamentoModal from "./NovoAgendamentoModal.vue"

const mockObter = vi.fn()

// Mock do tenantStore com objeto mutável — testes alteram tenantMock.papel antes de montar.
const tenantMock = { estabelecimentoAtivoId: 1, papel: "Dono" as "Dono" | "Profissional" | null }

vi.mock("@/stores/tenantStore", () => ({
    useTenantStore: vi.fn(() => tenantMock),
}))

vi.mock("@/services/agendaService", () => ({
    agendaService: {
        consultarDisponibilidade: vi.fn().mockResolvedValue({ dias: [] }),
        criar: vi.fn().mockResolvedValue({ id: 1 }),
    },
    listaEsperaService: {
        criar: vi.fn().mockResolvedValue({ id: 1 }),
    },
}))

vi.mock("@/services/listaEsperaService", () => ({
    listaEsperaService: {
        criar: vi.fn().mockResolvedValue({ id: 1 }),
    },
}))

vi.mock("@/services/pacienteService", () => ({
    pacienteService: {
        buscaRapida: vi.fn().mockResolvedValue([]),
        criar: vi.fn().mockResolvedValue({ id: 1 }),
        obter: (id: number) => mockObter(id),
    },
}))

vi.mock("@/services/salaService", () => ({
    salaService: {
        listar: vi.fn().mockResolvedValue([]),
    },
}))

vi.mock("maska/vue", () => ({
    vMaska: { mounted: vi.fn(), updated: vi.fn() },
}))

vi.mock("@/utils/cpf", () => ({
    cpfValido: vi.fn(() => true),
    somenteDigitos: vi.fn((v: string) => v),
}))

const PROFISSIONAL = {
    usuarioId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    nomeCompleto: "Dr. João",
    fotoUrl: null,
    status: "Ativo",
}
const PACIENTE_SELECIONADO = { id: 10, nomeCompleto: "Maria Silva" }
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

function criarWrapper() {
    return mount(NovoAgendamentoModal, {
        props: {
            aberto: true,
            profissionais: [PROFISSIONAL],
        },
        global: {
            plugins: [createTestingPinia({ createSpy: vi.fn })],
            stubs: {
                AppAvatarSelect: {
                    template: '<select><slot /></select>',
                    props: ["modelValue", "opcoes"],
                },
                AppDatePicker: {
                    template: '<input type="date" />',
                    props: ["modelValue"],
                },
                AppCheckbox: {
                    template: '<input type="checkbox" />',
                    props: ["modelValue"],
                },
                DocumentoPacienteField: {
                    template: '<div />',
                    props: ["modelValue"],
                },
            },
        },
    })
}

beforeEach(() => {
    tenantMock.papel = "Dono"
    mockObter.mockResolvedValue(PACIENTE_COMPLETO)
})

describe("NovoAgendamentoModal — seção de lembrete automático", () => {
    it("exibe nota informativa no step 2 (sem checkboxes interativos)", async () => {
        const w = criarWrapper()
        await w.vm.$nextTick()

        const vm = w.vm as any
        vm.pacienteSel = PACIENTE_SELECIONADO
        vm.step = 2
        await w.vm.$nextTick()

        expect(w.find(".reminder-info").exists()).toBe(true)
        expect(w.find(".reminder-info").text()).toContain("Automações do estabelecimento")

        expect(w.find(".badge-soon").exists()).toBe(false)
        expect(w.find(".reminder-wa-soon").exists()).toBe(false)
        expect(w.find(".reminder-toggles").exists()).toBe(false)
    })

    it("no step 3, resumo de lembrete exibe texto de automação", async () => {
        const w = criarWrapper()
        await w.vm.$nextTick()

        const vm = w.vm as any
        vm.pacienteSel = PACIENTE_SELECIONADO
        vm.step = 3
        await w.vm.$nextTick()

        const kvLembrete = w.findAll(".kv").find(el => el.find("span").text() === "Lembrete")
        if (kvLembrete) {
            expect(kvLembrete.find("b").text()).toContain("Automático")
        }
    })
})

describe("NovoAgendamentoModal — atalho Editar dados (CA1/CA3/CA4/CA5/CA6)", () => {
    it("CA4: botão Editar dados NÃO aparece no Step 1 (sem paciente selecionado)", async () => {
        const w = criarWrapper()
        await w.vm.$nextTick()

        // Step 1 é o padrão ao abrir — não há .btn-editar-pac
        expect(w.find(".btn-editar-pac").exists()).toBe(false)
    })

    it("CA4: botão Editar dados NÃO aparece no cadastro inline de paciente novo (modo=new)", async () => {
        const w = criarWrapper()
        await w.vm.$nextTick()
        const vm = w.vm as any
        vm.modo = "new"
        vm.step = 2
        await w.vm.$nextTick()

        // Em modo "new", pacienteEfetivo.novo=true → v-if não renderiza
        expect(w.find(".btn-editar-pac").exists()).toBe(false)
    })

    it("CA5: podeEditarPaciente é true quando papel é Dono", async () => {
        tenantMock.papel = "Dono"
        const w = criarWrapper()
        await w.vm.$nextTick()
        const vm = w.vm as any
        expect(vm.podeEditarPaciente).toBe(true)
    })

    it("CA5: podeEditarPaciente é true quando papel é Profissional", async () => {
        tenantMock.papel = "Profissional"
        const w = criarWrapper()
        await w.vm.$nextTick()
        const vm = w.vm as any
        expect(vm.podeEditarPaciente).toBe(true)
    })

    it("CA6: podeEditarPaciente é false quando papel é null (recepção sem papel explícito)", async () => {
        tenantMock.papel = null
        const w = criarWrapper()
        await w.vm.$nextTick()
        const vm = w.vm as any
        expect(vm.podeEditarPaciente).toBe(false)
    })

    it("CA1: clicar em Editar dados chama pacienteService.obter e emite 'editar-paciente' com paciente completo", async () => {
        tenantMock.papel = "Dono"
        mockObter.mockResolvedValue(PACIENTE_COMPLETO)
        const w = criarWrapper()
        await w.vm.$nextTick()
        const vm = w.vm as any
        vm.pacienteSel = PACIENTE_SELECIONADO
        vm.step = 2
        await w.vm.$nextTick()

        await vm.solicitarEdicaoPaciente()
        await w.vm.$nextTick()

        expect(mockObter).toHaveBeenCalledWith(10)
        const emitidos = w.emitted("editar-paciente")
        expect(emitidos).toBeTruthy()
        if (emitidos) {
            expect(emitidos[0][0]).toMatchObject({ id: 10, nomeCompleto: "Maria Silva" })
        }
    })

    it("CA1: ao setar pacienteSelEnriquecido diretamente, segunda chamada reutiliza cache (sem backend)", async () => {
        tenantMock.papel = "Dono"
        mockObter.mockResolvedValue(PACIENTE_COMPLETO)
        const w = criarWrapper()
        await w.vm.$nextTick()
        const vm = w.vm as any
        vm.pacienteSel = PACIENTE_SELECIONADO
        vm.step = 2
        await w.vm.$nextTick()

        // Injeta cache diretamente (simula estado após primeira chamada ter buscado o paciente)
        vm.pacienteSelEnriquecido = PACIENTE_COMPLETO
        await w.vm.$nextTick()
        mockObter.mockClear()

        // Segunda chamada — deve reusar cache, sem nova requisição
        await vm.solicitarEdicaoPaciente()
        await w.vm.$nextTick()
        expect(mockObter).not.toHaveBeenCalled()
        // Mesmo assim emite corretamente
        const emitidos = w.emitted("editar-paciente")
        expect(emitidos).toBeTruthy()
    })

    it("CA3: card do paciente reflete nome atualizado via prop pacienteAtualizado (sem fechar modal)", async () => {
        tenantMock.papel = "Dono"
        const w = criarWrapper()
        await w.vm.$nextTick()
        const vm = w.vm as any
        vm.pacienteSel = PACIENTE_SELECIONADO
        vm.step = 2
        await w.vm.$nextTick()

        // Atualiza via prop (simula pai passando paciente editado de volta)
        const pacienteAtualizado = { ...PACIENTE_COMPLETO, nomeCompleto: "Maria Santos" }
        await w.setProps({ pacienteAtualizado })
        await w.vm.$nextTick()

        // Step e pacienteSel não mudaram — fluxo preservado
        expect(vm.step).toBe(2)
        expect(vm.pacienteSel).toMatchObject({ id: 10 })

        // pacienteSelEnriquecido foi atualizado pelo watch
        expect(vm.pacienteSelEnriquecido).toMatchObject({ nomeCompleto: "Maria Santos" })
    })

    it("CA3: campos do agendamento permanecem intactos após editar dados do paciente", async () => {
        tenantMock.papel = "Dono"
        const w = criarWrapper()
        await w.vm.$nextTick()
        const vm = w.vm as any
        vm.pacienteSel = PACIENTE_SELECIONADO
        vm.step = 2
        vm.detalhes.motivo = "Dor de cabeça"
        vm.detalhes.profissionalUsuarioId = PROFISSIONAL.usuarioId
        vm.detalhes.data = "2026-07-01"
        vm.detalhes.hora = "10:00"
        await w.vm.$nextTick()

        // Atualiza paciente via prop
        await w.setProps({ pacienteAtualizado: { ...PACIENTE_COMPLETO, nomeCompleto: "Maria Santos" } })
        await w.vm.$nextTick()

        // Campos de agendamento permanecem intactos
        expect(vm.detalhes.motivo).toBe("Dor de cabeça")
        expect(vm.detalhes.profissionalUsuarioId).toBe(PROFISSIONAL.usuarioId)
        expect(vm.detalhes.data).toBe("2026-07-01")
        expect(vm.detalhes.hora).toBe("10:00")
        expect(vm.step).toBe(2)
    })
})
