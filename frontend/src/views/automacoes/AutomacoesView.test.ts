/**
 * Testes do AutomacoesView — toggle de WhatsApp (CA7/CA19).
 */
import { describe, it, expect, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import { createTestingPinia } from "@pinia/testing"
import AutomacoesView from "./AutomacoesView.vue"

vi.mock("@/services/automacaoService", () => ({
    automacaoService: {
        obterConfiguracao: vi.fn().mockResolvedValue({
            lembretesHabilitados: true,
            lembretesWhatsappHabilitados: false,
            horasAntecedenciaLembrete: 24,
            expiracaoOrcamentosHabilitada: true,
            emailRemetente: null,
        }),
        salvarConfiguracao: vi.fn().mockResolvedValue(undefined),
        expirarOrcamentos: vi.fn().mockResolvedValue(undefined),
        enviarLembretes: vi.fn().mockResolvedValue(undefined),
    },
}))

describe("AutomacoesView — toggle WhatsApp", () => {
    async function montar() {
        const wrapper = mount(AutomacoesView, {
            global: {
                plugins: [createTestingPinia()],
                stubs: { AppButton: true, AppPageHeader: true },
            },
        })
        // Aguarda o onMounted + promise de carregamento resolver
        await flushPromises()
        return wrapper
    }

    it("exibe o toggle de WhatsApp quando lembretes estão habilitados", async () => {
        const wrapper = await montar()
        const toggle = wrapper.find("[data-testid='toggle-whatsapp']")
        expect(toggle.exists()).toBe(true)
    })

    it("toggle de WhatsApp inicia desmarcado (config padrão false)", async () => {
        const wrapper = await montar()
        const toggle = wrapper.find<HTMLInputElement>("[data-testid='toggle-whatsapp']")
        expect(toggle.element.checked).toBe(false)
    })

    it("ao marcar o toggle, o valor é refletido no model", async () => {
        const wrapper = await montar()
        const toggle = wrapper.find<HTMLInputElement>("[data-testid='toggle-whatsapp']")
        await toggle.setValue(true)
        expect(toggle.element.checked).toBe(true)
    })

    it("toggle de WhatsApp não aparece quando lembretes estão desabilitados", async () => {
        const { automacaoService } = await import("@/services/automacaoService")
        vi.mocked(automacaoService.obterConfiguracao).mockResolvedValueOnce({
            lembretesHabilitados: false,
            lembretesWhatsappHabilitados: false,
            horasAntecedenciaLembrete: 24,
            expiracaoOrcamentosHabilitada: true,
            emailRemetente: null,
        })

        const wrapper = mount(AutomacoesView, {
            global: {
                plugins: [createTestingPinia()],
                stubs: { AppButton: true, AppPageHeader: true },
            },
        })
        await flushPromises()

        const toggle = wrapper.find("[data-testid='toggle-whatsapp']")
        expect(toggle.exists()).toBe(false)
    })
})
