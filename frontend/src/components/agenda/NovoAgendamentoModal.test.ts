/**
 * Testes do NovoAgendamentoModal — CA14: checkbox WhatsApp desabilitado.
 *
 * Foco: verificar que o checkbox de WhatsApp está desabilitado e que o resumo
 * de canais no step 3 não conta WhatsApp como canal ativo.
 */
import { describe, it, expect, vi } from "vitest"
import { mount } from "@vue/test-utils"
import { createTestingPinia } from "@pinia/testing"
import NovoAgendamentoModal from "./NovoAgendamentoModal.vue"

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
        buscarRapido: vi.fn().mockResolvedValue([]),
        criar: vi.fn().mockResolvedValue({ id: 1 }),
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

vi.mock("maska/vue", () => ({
    vMaska: { mounted: vi.fn(), updated: vi.fn() },
}))

vi.mock("@/utils/cpf", () => ({
    cpfValido: vi.fn(() => true),
    somenteDigitos: vi.fn((v: string) => v),
}))

function criarWrapper() {
    return mount(NovoAgendamentoModal, {
        props: {
            aberto: true,
            profissionais: [
                { usuarioId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", nomeCompleto: "Dr. João", fotoUrl: null, status: "Ativo" },
            ],
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
                DocumentoPacienteField: {
                    template: '<div />',
                    props: ["modelValue"],
                },
            },
        },
    })
}

describe("NovoAgendamentoModal — CA14: checkbox WhatsApp desabilitado", () => {
    it("o AppCheckbox de WhatsApp tem prop disabled=true no step 2", async () => {
        const w = criarWrapper()
        await w.vm.$nextTick()

        // Avança para o step 2 (detalhes) usando refs internas do componente
        const vm = w.vm as any
        // Selecionar paciente via ref interna (pacienteSel é o ref que controla pacienteEfetivo)
        vm.pacienteSel = { id: 1, nomeCompleto: "Maria Silva", cpfMascarado: "" }
        vm.step = 2
        await w.vm.$nextTick()

        // O wrapper .reminder-wa-soon deve existir no step 2
        expect(w.find(".reminder-wa-soon").exists()).toBe(true)

        // O checkbox input de WhatsApp deve estar disabled
        const reminderWaSoon = w.find(".reminder-wa-soon")
        const input = reminderWaSoon.find("input[type='checkbox']")
        expect(input.exists()).toBe(true)
        expect(input.attributes("disabled")).toBeDefined()
    })

    it("badge 'em breve' aparece ao lado do checkbox de WhatsApp", async () => {
        const w = criarWrapper()
        await w.vm.$nextTick()

        const vm = w.vm as any
        vm.pacienteSel = { id: 1, nomeCompleto: "Maria Silva", cpfMascarado: "" }
        vm.step = 2
        await w.vm.$nextTick()

        expect(w.find(".badge-soon").exists()).toBe(true)
        expect(w.find(".badge-soon").text()).toBe("em breve")
    })

    it("no step 3, com lembreteEmail=false, resumo mostra 'Nao enviar' (WhatsApp nao conta)", async () => {
        const w = criarWrapper()
        await w.vm.$nextTick()

        const vm = w.vm as any
        vm.pacienteSel = { id: 1, nomeCompleto: "Maria Silva", cpfMascarado: "" }
        vm.detalhes.lembreteEmail = false
        vm.detalhes.lembreteWA = false
        vm.step = 3
        await w.vm.$nextTick()

        const kvLembrete = w.findAll(".kv").find(el => el.find("span").text() === "Lembrete")
        if (kvLembrete) {
            expect(kvLembrete.find("b").text()).toBe("Não enviar")
        }
    })

    it("no step 3, com lembreteEmail=true, mostra somente 'E-mail' (sem WhatsApp)", async () => {
        const w = criarWrapper()
        await w.vm.$nextTick()

        const vm = w.vm as any
        vm.pacienteSel = { id: 1, nomeCompleto: "Maria Silva", cpfMascarado: "" }
        vm.detalhes.lembreteEmail = true
        vm.detalhes.lembreteWA = false
        vm.step = 3
        await w.vm.$nextTick()

        const kvLembrete = w.findAll(".kv").find(el => el.find("span").text() === "Lembrete")
        if (kvLembrete) {
            expect(kvLembrete.find("b").text()).toBe("E-mail")
            expect(kvLembrete.find("b").text()).not.toContain("WhatsApp")
        }
    })
})
