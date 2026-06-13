import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"

const pushMock = vi.fn()

vi.mock("vue-router", async () => {
    const actual = await vi.importActual("vue-router")
    return {
        ...(actual as object),
        useRouter: () => ({ push: pushMock }),
    }
})

// Stub do design system para isolar comportamento do componente
vi.mock("@/components/ui", () => {
    const AppDrawer = {
        props: ["aberto", "titulo", "largura"],
        emits: ["fechar"],
        template: `
            <div v-if="aberto" data-test="drawer">
                <slot />
                <slot name="rodape" />
            </div>
        `,
    }
    const AppConfirmDialog = {
        props: ["aberto", "titulo", "mensagem", "confirmarRotulo", "variante"],
        emits: ["confirmar", "cancelar"],
        template: `
            <div v-if="aberto" data-test="confirm-dialog">
                <span>{{ titulo }}</span>
                <button data-test="btn-confirmar" @click="$emit('confirmar')">{{ confirmarRotulo }}</button>
                <button data-test="btn-cancelar" @click="$emit('cancelar')">Cancelar</button>
            </div>
        `,
    }
    const AppEmptyState = {
        props: ["icone", "titulo", "descricao"],
        template: `<div data-test="empty-state"><p>{{ titulo }}</p><slot name="acoes" /></div>`,
    }
    const AppButton = {
        props: ["variant", "size", "icon"],
        template: `<button v-bind="$attrs" @click="$emit('click')"><slot /></button>`,
        emits: ["click"],
    }
    const AppToast = {
        props: ["mensagem", "variante"],
        emits: ["fechar"],
        template: `<div data-test="toast">{{ mensagem }}</div>`,
    }
    return { AppDrawer, AppConfirmDialog, AppEmptyState, AppButton, AppToast }
})

vi.mock("@/services/modeloDescricaoCirurgicaService", () => ({
    modeloDescricaoCirurgicaService: {
        listar: vi.fn(),
    },
}))

import { modeloDescricaoCirurgicaService } from "@/services/modeloDescricaoCirurgicaService"
import SeletorTemplateCirurgico from "./SeletorTemplateCirurgico.vue"

const modeloSimples = { id: 1, titulo: "Rinoplastia", corpo: "Técnica aberta com enxerto de cartilagem.", ativo: true, ehPadraoSistema: false }
const modeloPadrao  = { id: 2, titulo: "Procedimento padrão", corpo: "Descrição padrão do sistema.", ativo: true, ehPadraoSistema: true }

/** Monta o componente fechado e o abre — garante que o watch dispara e os modelos são carregados. */
async function montarAberto(valorAtual: string, modelos: typeof modeloSimples[]) {
    vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue(modelos)
    const wrapper = mount(SeletorTemplateCirurgico, {
        props: { aberto: false, valorAtual },
    })
    await wrapper.setProps({ aberto: true })
    await flushPromises()
    return wrapper
}

describe("SeletorTemplateCirurgico", () => {
    beforeEach(() => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockReset()
        pushMock.mockReset()
    })

    it("CA15 — não busca modelos enquanto fechado (aberto=false)", () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([modeloSimples])
        mount(SeletorTemplateCirurgico, {
            props: { aberto: false, valorAtual: "" },
        })
        expect(modeloDescricaoCirurgicaService.listar).not.toHaveBeenCalled()
    })

    it("CA15 — busca modelos ao abrir (aberto muda de false para true)", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([modeloSimples])
        const wrapper = mount(SeletorTemplateCirurgico, {
            props: { aberto: false, valorAtual: "" },
        })
        await wrapper.setProps({ aberto: true })
        await flushPromises()
        expect(modeloDescricaoCirurgicaService.listar).toHaveBeenCalledTimes(1)
    })

    it("CA2 — campo vazio → aplica template direto sem diálogo de confirmação", async () => {
        const wrapper = await montarAberto("", [modeloSimples])

        await wrapper.find(".stc-item").trigger("click")
        await flushPromises()

        // Deve ter emitido "aplicar" com o corpo do modelo diretamente
        expect(wrapper.emitted("aplicar")).toBeTruthy()
        expect(wrapper.emitted("aplicar")![0]).toEqual([modeloSimples.corpo])
        // E deve fechar o drawer
        expect(wrapper.emitted("update:aberto")).toBeTruthy()
        expect(wrapper.emitted("update:aberto")![0]).toEqual([false])
        // NÃO deve ter aberto confirmação
        expect(wrapper.find("[data-test='confirm-dialog']").exists()).toBe(false)
    })

    it("CA3 — campo com texto → abre diálogo de confirmação antes de aplicar", async () => {
        const wrapper = await montarAberto("Texto já preenchido pelo profissional", [modeloSimples])

        await wrapper.find(".stc-item").trigger("click")
        await flushPromises()

        // NÃO deve ter emitido "aplicar" ainda
        expect(wrapper.emitted("aplicar")).toBeFalsy()
        // Deve mostrar o diálogo de confirmação
        expect(wrapper.find("[data-test='confirm-dialog']").exists()).toBe(true)
    })

    it("CA3 — confirmar substituição emite 'aplicar' e fecha", async () => {
        const wrapper = await montarAberto("Texto existente", [modeloSimples])

        await wrapper.find(".stc-item").trigger("click")
        await flushPromises()
        await wrapper.find("[data-test='btn-confirmar']").trigger("click")
        await flushPromises()

        expect(wrapper.emitted("aplicar")).toBeTruthy()
        expect(wrapper.emitted("aplicar")![0]).toEqual([modeloSimples.corpo])
        // Diálogo deve fechar
        expect(wrapper.find("[data-test='confirm-dialog']").exists()).toBe(false)
    })

    it("CA13 — lista vazia exibe estado vazio com mensagem", async () => {
        const wrapper = await montarAberto("", [])

        expect(wrapper.find("[data-test='empty-state']").exists()).toBe(true)
        expect(wrapper.find("[data-test='empty-state']").text()).toContain("Nenhum modelo cadastrado")
    })

    it("exibe badge 'Padrão do sistema' para modelo padrão", async () => {
        const wrapper = await montarAberto("", [modeloPadrao])

        expect(wrapper.find(".badge-padrao").exists()).toBe(true)
        expect(wrapper.find(".badge-padrao").text()).toBe("Padrão do sistema")
    })

    it("CA17 — botão 'Cadastrar novo modelo' faz router.push para /estabelecimento?secao=modelos-cirurgia", async () => {
        const wrapper = await montarAberto("", [])

        // Estado vazio tem botão "Cadastrar novo modelo"
        const btns = wrapper.findAll("button")
        const btn = btns.find(b => b.text().includes("Cadastrar novo modelo"))
        expect(btn).toBeTruthy()
        await btn!.trigger("click")

        expect(pushMock).toHaveBeenCalledWith({ path: "/estabelecimento", query: { secao: "modelos-cirurgia" } })
    })

    it("erro de carregamento exibe classe .stc-erro", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockRejectedValue(new Error("timeout"))
        const wrapper = mount(SeletorTemplateCirurgico, {
            props: { aberto: false, valorAtual: "" },
        })
        await wrapper.setProps({ aberto: true })
        await flushPromises()

        expect(wrapper.find(".stc-erro").exists()).toBe(true)
        expect(wrapper.find(".stc-erro").text()).toContain("Não foi possível carregar")
    })
})
