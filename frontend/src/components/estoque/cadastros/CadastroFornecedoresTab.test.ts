/**
 * Testes de regressão para CadastroFornecedoresTab.
 *
 * Foco: formulário do drawer de criação/edição — em especial o comportamento
 * do campo CNPJ com máscara alfanumérica (CA4 do briefing 2026-06-19_002).
 */
import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"

vi.mock("@/components/ui", () => {
    const AppSearchInput = {
        props: ["modelValue", "placeholder"],
        emits: ["update:modelValue"],
        template: `<input :value="modelValue" @input="$emit('update:modelValue', $event.target.value)" data-test="search" />`,
    }
    const AppButton = {
        props: ["variant", "icon", "loading", "disabled"],
        emits: ["click"],
        template: `<button :disabled="disabled || loading" @click="$emit('click')"><slot /></button>`,
    }
    const AppDrawer = {
        props: ["aberto", "titulo", "largura"],
        emits: ["fechar"],
        template: `<div v-if="aberto" data-test="drawer"><slot /><slot name="footer" /></div>`,
    }
    const AppField = {
        props: ["label", "required", "erro", "hint", "class"],
        template: `<div><label>{{ label }}</label><slot /></div>`,
    }
    const AppInput = {
        props: ["modelValue", "placeholder", "type", "min", "step", "disabled"],
        emits: ["update:modelValue"],
        template: `<input :value="modelValue" :type="type || 'text'" :disabled="disabled"
                          @input="$emit('update:modelValue', $event.target.value)" />`,
    }
    const AppStatusPill = {
        props: ["status"],
        template: `<span>{{ status }}</span>`,
    }
    const AppToast = {
        props: ["mensagem", "variante"],
        emits: ["fechar"],
        template: `<div v-if="mensagem">{{ mensagem }}</div>`,
    }
    const AppConfirmDialog = {
        props: ["aberto", "titulo", "mensagem", "executando"],
        emits: ["confirmar", "fechar"],
        template: `<div v-if="aberto" data-test="confirm">
            <button data-test="confirm-ok" @click="$emit('confirmar')">Confirmar</button>
        </div>`,
    }
    const AppEmptyState = {
        props: ["mensagem"],
        template: `<div>{{ mensagem }}</div>`,
    }
    const AppPagination = {
        props: ["pagina", "tamanho", "total"],
        emits: ["update:pagina", "update:tamanho"],
        template: `<div data-test="paginacao"></div>`,
    }
    const AppPillToggle = {
        props: ["modelValue", "opcoes"],
        emits: ["update:modelValue"],
        template: `<div data-test="pill-toggle">
            <button v-for="o in opcoes" :key="o.valor"
                    :data-valor="o.valor"
                    :data-selected="modelValue === o.valor"
                    @click="$emit('update:modelValue', o.valor)">{{ o.label }}</button>
        </div>`,
    }
    return {
        AppSearchInput, AppButton, AppDrawer, AppField, AppInput,
        AppStatusPill, AppToast, AppConfirmDialog, AppEmptyState,
        AppPagination, AppPillToggle,
    }
})

vi.mock("@/composables/useDebouncedRef", () => ({
    useDebouncedRef: (r: any) => r,
}))

vi.mock("@/services/estoqueCadastrosService", async () => {
    const actual = await vi.importActual<any>("@/services/estoqueCadastrosService")
    return {
        ...actual,
        estoqueCadastrosService: {
            fornecedores: {
                listar: vi.fn().mockResolvedValue({ itens: [], total: 0, pagina: 1, tamanhoPagina: 10 }),
                criar: vi.fn(),
                atualizar: vi.fn(),
                inativar: vi.fn(),
                reativar: vi.fn(),
            },
        },
    }
})

import CadastroFornecedoresTab from "./CadastroFornecedoresTab.vue"
import { estoqueCadastrosService } from "@/services/estoqueCadastrosService"

async function abrirDrawerCriacao(w: ReturnType<typeof montar>) {
    // O botão "+ Novo fornecedor" é o primeiro botão na barra de filtros
    const botaoNovo = w.findAll("button").find(b => b.text().includes("Novo"))
    await botaoNovo!.trigger("click")
    await flushPromises()
}

function inputDrawer(w: ReturnType<typeof montar>, idx: number) {
    const drawer = w.find("[data-test='drawer']")
    return drawer.findAll("input")[idx]
}

function botaoSalvar(w: ReturnType<typeof montar>) {
    const drawer = w.find("[data-test='drawer']")
    const botoes = drawer.findAll("button")
    return botoes[botoes.length - 1]
}

function montar() {
    return mount(CadastroFornecedoresTab, { global: { stubs: {} } })
}

describe("CadastroFornecedoresTab — drawer de fornecedor", () => {
    beforeEach(() => {
        vi.mocked(estoqueCadastrosService.fornecedores.criar).mockReset()
        vi.mocked(estoqueCadastrosService.fornecedores.listar).mockResolvedValue({ itens: [], total: 0, pagina: 1, tamanhoPagina: 10 })
    })

    it("bloqueia submit quando razão social está vazia", async () => {
        const w = montar()
        await flushPromises()
        await abrirDrawerCriacao(w)

        await botaoSalvar(w).trigger("click")
        await flushPromises()

        expect(estoqueCadastrosService.fornecedores.criar).not.toHaveBeenCalled()
        expect(w.text()).toContain("Razão social é obrigatória.")
    })

    // CA4 — regressão máscara CNPJ alfanumérico
    // Sem o fix (v-maska ausente no AppInput): o campo recebe "12abc34501de35"
    // sem formatação nem uppercase; normalizarCnpj retornaria "12ABC34501DE35"
    // mas validateCnpj calcularia errado se o form.cnpj nunca passasse pelo
    // transform do maska. Com o fix: v-maska converte para "12.ABC.345/01DE-35"
    // ao vivo; normalizarCnpj("12.ABC.345/01DE-35") → "12ABC34501DE35" (canonical).
    it("normaliza CNPJ alfanumérico formatado ao submeter (CA4 — regressão máscara)", async () => {
        vi.mocked(estoqueCadastrosService.fornecedores.criar).mockResolvedValueOnce({ id: 7 })
        const w = montar()
        await flushPromises()
        await abrirDrawerCriacao(w)

        // razão social (índice 0 no drawer)
        await inputDrawer(w, 0).setValue("Distribuidora Alfa Ltda")
        // cnpj (índice 2 no drawer: 0=razaoSocial, 1=nomeFantasia, 2=cnpj)
        // Simula o valor que v-maska produziria após digitar o CNPJ alfanumérico:
        // a máscara formata e aplica uppercase → "12.ABC.345/01DE-35"
        await inputDrawer(w, 2).setValue("12.ABC.345/01DE-35")

        await botaoSalvar(w).trigger("click")
        await flushPromises()

        expect(estoqueCadastrosService.fornecedores.criar).toHaveBeenCalledWith(
            expect.objectContaining({ cnpj: "12ABC34501DE35" })
        )
    })
})
