import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"

// Stub do design system para isolar comportamento do componente
vi.mock("@/components/ui", () => {
    const AppCard = {
        props: ["padding"],
        template: `<div data-test="app-card"><slot /></div>`,
    }
    const AppField = {
        props: ["label", "erro"],
        template: `<div><label>{{ label }}</label><slot /></div>`,
    }
    const AppInput = {
        props: ["modelValue", "placeholder", "maxlength"],
        emits: ["update:modelValue"],
        template: `<input :value="modelValue" v-bind="$attrs" @input="$emit('update:modelValue', $event.target.value)" />`,
    }
    const AppTextarea = {
        props: ["modelValue", "rows", "placeholder"],
        emits: ["update:modelValue"],
        template: `<textarea :value="modelValue" v-bind="$attrs" @input="$emit('update:modelValue', $event.target.value)" />`,
    }
    const AppButton = {
        props: ["variant", "size", "loading", "disabled", "icon"],
        emits: ["click"],
        template: `<button :disabled="disabled || loading" @click="$emit('click')"><slot /></button>`,
    }
    const AppEmptyState = {
        props: ["icone", "titulo", "descricao"],
        template: `<div data-test="empty-state"><p>{{ titulo }}</p><slot name="acoes" /></div>`,
    }
    const AppConfirmDialog = {
        props: ["aberto", "titulo", "mensagem", "confirmarRotulo", "variante", "executando"],
        emits: ["confirmar", "cancelar", "update:aberto"],
        template: `
            <div v-if="aberto" data-test="confirm-dialog">
                <span>{{ titulo }}</span>
                <button data-test="btn-confirmar" @click="$emit('confirmar')">{{ confirmarRotulo }}</button>
                <button data-test="btn-cancelar" @click="$emit('cancelar')">Cancelar</button>
            </div>
        `,
    }
    const AppToast = {
        props: ["mensagem", "variante"],
        emits: ["fechar"],
        template: `<div data-test="toast">{{ mensagem }}</div>`,
    }
    return { AppCard, AppField, AppInput, AppTextarea, AppButton, AppEmptyState, AppConfirmDialog, AppToast }
})

vi.mock("@/services/modeloDescricaoCirurgicaService", () => ({
    modeloDescricaoCirurgicaService: {
        listar: vi.fn(),
        criar: vi.fn(),
        editar: vi.fn(),
        excluir: vi.fn(),
    },
}))

import { modeloDescricaoCirurgicaService } from "@/services/modeloDescricaoCirurgicaService"
import ModelosDescricaoCirurgicaTab from "./ModelosDescricaoCirurgicaTab.vue"

const modeloDoEstab = { id: 1, titulo: "Rinoplastia", corpo: "Técnica aberta.", ativo: true, ehPadraoSistema: false }
const modeloPadrao  = { id: 2, titulo: "Padrão", corpo: "Descrição padrão.", ativo: true, ehPadraoSistema: true }

describe("ModelosDescricaoCirurgicaTab", () => {
    beforeEach(() => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockReset()
        vi.mocked(modeloDescricaoCirurgicaService.criar).mockReset()
        vi.mocked(modeloDescricaoCirurgicaService.editar).mockReset()
        vi.mocked(modeloDescricaoCirurgicaService.excluir).mockReset()
    })

    it("carrega e exibe lista de modelos ao montar", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([modeloDoEstab])
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: true },
        })
        await flushPromises()
        expect(modeloDescricaoCirurgicaService.listar).toHaveBeenCalledTimes(1)
        expect(wrapper.text()).toContain("Rinoplastia")
    })

    it("sem podeEditar — exibe aviso de leitura e oculta form", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([modeloDoEstab])
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: false },
        })
        await flushPromises()
        expect(wrapper.find(".aviso-leitura").exists()).toBe(true)
        expect(wrapper.find(".mdc-form").exists()).toBe(false)
    })

    it("com podeEditar — exibe form de criação", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([])
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: true },
        })
        await flushPromises()
        expect(wrapper.find(".mdc-form").exists()).toBe(true)
        expect(wrapper.find(".aviso-leitura").exists()).toBe(false)
    })

    it("lista vazia exibe estado vazio", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([])
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: true },
        })
        await flushPromises()
        expect(wrapper.find("[data-test='empty-state']").exists()).toBe(true)
        expect(wrapper.find("[data-test='empty-state']").text()).toContain("Nenhum modelo cadastrado")
    })

    it("modelo padrão do sistema exibe badge e NÃO mostra botões de editar/excluir (CA6)", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([modeloPadrao])
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: true },
        })
        await flushPromises()
        expect(wrapper.find(".badge-padrao").exists()).toBe(true)
        expect(wrapper.find(".btn-icon-editar").exists()).toBe(false)
        expect(wrapper.find(".btn-icon-excluir").exists()).toBe(false)
    })

    it("modelo do estabelecimento exibe botões de editar e excluir", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([modeloDoEstab])
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: true },
        })
        await flushPromises()
        expect(wrapper.find(".btn-icon-editar").exists()).toBe(true)
        expect(wrapper.find(".btn-icon-excluir").exists()).toBe(true)
    })

    it("clicar em editar preenche o form com dados do modelo", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([modeloDoEstab])
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: true },
        })
        await flushPromises()
        await wrapper.find(".btn-icon-editar").trigger("click")
        await flushPromises()

        // O form agora deve mostrar "Editar modelo"
        expect(wrapper.text()).toContain("Editar modelo")
        // O input deve ter o título do modelo
        const inputs = wrapper.findAll("input")
        expect((inputs[0].element as HTMLInputElement).value).toBe("Rinoplastia")
    })

    it("valida campos obrigatórios — título vazio bloqueia submit", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([])
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: true },
        })
        await flushPromises()

        // Clica em "Criar modelo" sem preencher título
        const btns = wrapper.findAll("button")
        const salvarBtn = btns.find(b => b.text().includes("Criar modelo"))
        await salvarBtn?.trigger("click")

        expect(modeloDescricaoCirurgicaService.criar).not.toHaveBeenCalled()
        expect(wrapper.text()).toContain("Título é obrigatório")
    })

    it("chama criar() com título e corpo corretos", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([])
        vi.mocked(modeloDescricaoCirurgicaService.criar).mockResolvedValue(undefined as any)
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: true },
        })
        await flushPromises()

        const inputs = wrapper.findAll("input")
        await inputs[0].setValue("Nova Rinoplastia")
        const textareas = wrapper.findAll("textarea")
        await textareas[0].setValue("Descrição do procedimento.")

        const btns = wrapper.findAll("button")
        const salvarBtn = btns.find(b => b.text().includes("Criar modelo"))
        await salvarBtn?.trigger("click")
        await flushPromises()

        expect(modeloDescricaoCirurgicaService.criar).toHaveBeenCalledWith("Nova Rinoplastia", "Descrição do procedimento.")
    })

    it("clicar em excluir abre diálogo de confirmação", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([modeloDoEstab])
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: true },
        })
        await flushPromises()
        await wrapper.find(".btn-icon-excluir").trigger("click")
        await flushPromises()

        expect(wrapper.find("[data-test='confirm-dialog']").exists()).toBe(true)
        expect(wrapper.find("[data-test='confirm-dialog']").text()).toContain("Excluir modelo?")
    })

    it("confirmar exclusão chama excluir() e recarrega lista", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar)
            .mockResolvedValueOnce([modeloDoEstab])
            .mockResolvedValueOnce([])
        vi.mocked(modeloDescricaoCirurgicaService.excluir).mockResolvedValue(undefined as any)
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: true },
        })
        await flushPromises()
        await wrapper.find(".btn-icon-excluir").trigger("click")
        await flushPromises()
        await wrapper.find("[data-test='btn-confirmar']").trigger("click")
        await flushPromises()

        expect(modeloDescricaoCirurgicaService.excluir).toHaveBeenCalledWith(modeloDoEstab.id)
        // Lista recarregada — agora vazia
        expect(wrapper.find("[data-test='empty-state']").exists()).toBe(true)
    })

    it("CA6/multi-tenant — itens padrão sem ações, itens do estab com ações", async () => {
        vi.mocked(modeloDescricaoCirurgicaService.listar).mockResolvedValue([modeloPadrao, modeloDoEstab])
        const wrapper = mount(ModelosDescricaoCirurgicaTab, {
            props: { podeEditar: true },
        })
        await flushPromises()

        const itens = wrapper.findAll(".mdc-item")
        // Primeiro item: padrão — sem ações
        expect(itens[0].find(".mdc-item-acoes").exists()).toBe(false)
        // Segundo item: do estab — com ações
        expect(itens[1].find(".mdc-item-acoes").exists()).toBe(true)
    })
})
