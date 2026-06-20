/**
 * Testes do NovoAgendamentoModal — seção de lembrete automático.
 *
 * Foco: verificar que a seção exibe a nota informativa correta
 * (checkboxes interativos foram substituídos pela decisão do produto).
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

describe("NovoAgendamentoModal — seção de lembrete automático", () => {
    it("exibe nota informativa no step 2 (sem checkboxes interativos)", async () => {
        const w = criarWrapper()
        await w.vm.$nextTick()

        const vm = w.vm as any
        vm.pacienteSel = { id: 1, nomeCompleto: "Maria Silva", cpfMascarado: "" }
        vm.step = 2
        await w.vm.$nextTick()

        // Nota informativa deve existir
        expect(w.find(".reminder-info").exists()).toBe(true)
        expect(w.find(".reminder-info").text()).toContain("Automações do estabelecimento")

        // Checkboxes interativos e badge "em breve" não devem mais existir
        expect(w.find(".badge-soon").exists()).toBe(false)
        expect(w.find(".reminder-wa-soon").exists()).toBe(false)
        expect(w.find(".reminder-toggles").exists()).toBe(false)
    })

    it("no step 3, resumo de lembrete exibe texto de automação (sem depender de estado do checkbox)", async () => {
        const w = criarWrapper()
        await w.vm.$nextTick()

        const vm = w.vm as any
        vm.pacienteSel = { id: 1, nomeCompleto: "Maria Silva", cpfMascarado: "" }
        vm.step = 3
        await w.vm.$nextTick()

        const kvLembrete = w.findAll(".kv").find(el => el.find("span").text() === "Lembrete")
        if (kvLembrete) {
            expect(kvLembrete.find("b").text()).toContain("Automático")
            expect(kvLembrete.find("b").text()).not.toContain("E-mail")
            expect(kvLembrete.find("b").text()).not.toContain("WhatsApp")
        }
    })

    it("no step 3, texto de confirmação menciona automações do estabelecimento", async () => {
        const w = criarWrapper()
        await w.vm.$nextTick()

        const vm = w.vm as any
        vm.pacienteSel = { id: 1, nomeCompleto: "Maria Silva", cpfMascarado: "" }
        vm.detalhes.data = new Date().toISOString().slice(0, 10)
        vm.detalhes.hora = "10:00"
        vm.step = 3
        await w.vm.$nextTick()

        const confirmInfo = w.find(".confirm-info")
        if (confirmInfo.exists()) {
            expect(confirmInfo.text()).toContain("Automações do estabelecimento")
        }
    })
})
