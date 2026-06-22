/**
 * Testes do PacienteFormModal — checkbox de opt-in WhatsApp (CA8/R3/CA19).
 * Foco: renderização do campo e emissão correta do payload.
 */
import { describe, it, expect, vi } from "vitest"
import { mount } from "@vue/test-utils"
import { createTestingPinia } from "@pinia/testing"
import PacienteFormModal from "./PacienteFormModal.vue"
import type { Paciente } from "@/services/pacienteService"

vi.mock("@/services/pacienteService", () => ({
    pacienteService: {
        criar: vi.fn().mockResolvedValue(undefined),
        atualizar: vi.fn().mockResolvedValue(undefined),
        obter: vi.fn().mockResolvedValue({ id: 1, nomeCompleto: "Teste", whatsappLembreteOptIn: true }),
        listar: vi.fn().mockResolvedValue({ itens: [], total: 0, pagina: 1, tamanhoPagina: 5 }),
    },
}))

vi.mock("@/composables/useCepAutofill", () => ({
    useCepAutofill: () => ({ buscando: { value: false }, marcarCarga: vi.fn() }),
}))

const stubs = {
    AppModal: { template: "<div><slot /><slot name='titulo' /><slot name='rodape' /></div>" },
    AppButton: { template: "<button><slot /></button>" },
    AppField: { template: "<div><slot /></div>" },
    AppInput: { template: "<input />" },
    AppDatePicker: { template: "<input />" },
    AppSelect: { template: "<select><slot /></select>" },
    AppTextarea: { template: "<textarea />" },
}

function criarPaciente(overrides: Partial<Paciente> = {}): Paciente {
    return {
        id: 1,
        estabelecimentoId: 1,
        nomeCompleto: "Ana Lima",
        cpf: null,
        documentoInternacional: null,
        dataNascimento: null,
        genero: "Feminino",
        telefone: "11999998888",
        email: null,
        endereco: null,
        observacoes: null,
        tags: [],
        criadoEm: "2024-01-01T00:00:00Z",
        atualizadoEm: null,
        whatsappLembreteOptIn: false,
        ...overrides,
    }
}

describe("PacienteFormModal — opt-in WhatsApp", () => {
    it("exibe o checkbox de opt-in WhatsApp no modo editar", async () => {
        const wrapper = mount(PacienteFormModal, {
            props: { aberto: true, paciente: criarPaciente() },
            global: { stubs, plugins: [createTestingPinia()] },
        })
        await wrapper.vm.$nextTick()

        const checkbox = wrapper.find("[data-testid='checkbox-whatsapp-opt-in']")
        expect(checkbox.exists()).toBe(true)
    })

    it("checkbox reflete o opt-in do paciente existente (false → desmarcado)", async () => {
        const wrapper = mount(PacienteFormModal, {
            props: { aberto: true, paciente: criarPaciente({ whatsappLembreteOptIn: false }) },
            global: { stubs, plugins: [createTestingPinia()] },
        })
        await wrapper.vm.$nextTick()

        const checkbox = wrapper.find<HTMLInputElement>("[data-testid='checkbox-whatsapp-opt-in']")
        expect(checkbox.element.checked).toBe(false)
    })

    it("checkbox reflete o opt-in do paciente existente (true → marcado)", async () => {
        const wrapper = mount(PacienteFormModal, {
            props: { aberto: true, paciente: criarPaciente({ whatsappLembreteOptIn: true }) },
            global: { stubs, plugins: [createTestingPinia()] },
        })
        await wrapper.vm.$nextTick()

        const checkbox = wrapper.find<HTMLInputElement>("[data-testid='checkbox-whatsapp-opt-in']")
        expect(checkbox.element.checked).toBe(true)
    })

    it("ao marcar o checkbox, o payload inclui whatsappLembreteOptIn: true", async () => {
        const { pacienteService } = await import("@/services/pacienteService")
        const paciente = criarPaciente({ nomeCompleto: "Ana Lima" })

        const wrapper = mount(PacienteFormModal, {
            props: { aberto: true, paciente },
            global: { stubs, plugins: [createTestingPinia()] },
        })
        await wrapper.vm.$nextTick()

        const checkbox = wrapper.find<HTMLInputElement>("[data-testid='checkbox-whatsapp-opt-in']")
        await checkbox.setValue(true)

        // Disparar salvar via o método interno
        // @ts-expect-error acessando método interno para teste
        await wrapper.vm.salvar()
        await wrapper.vm.$nextTick()

        expect(pacienteService.atualizar).toHaveBeenCalledWith(
            1,
            expect.objectContaining({ whatsappLembreteOptIn: true })
        )
    })

    it("no modo criar, checkbox aparece quando o formulário é expandido", async () => {
        const wrapper = mount(PacienteFormModal, {
            props: { aberto: true, paciente: null },
            global: { stubs, plugins: [createTestingPinia()] },
        })
        await wrapper.vm.$nextTick()

        // No cadastro rápido, o checkbox não está visível (form não expandido)
        let checkbox = wrapper.find("[data-testid='checkbox-whatsapp-opt-in']")
        expect(checkbox.exists()).toBe(false)

        // Expandir o cadastro
        // @ts-expect-error acessando ref interna para teste
        wrapper.vm.expandirCadastro = true
        await wrapper.vm.$nextTick()

        checkbox = wrapper.find("[data-testid='checkbox-whatsapp-opt-in']")
        expect(checkbox.exists()).toBe(true)
    })
})
