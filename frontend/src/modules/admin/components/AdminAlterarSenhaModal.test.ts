import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import { setActivePinia, createPinia } from "pinia"

// Mock do design system.
vi.mock("@/components/ui", () => {
    const AppModal = {
        props: ["aberto", "largura"],
        emits: ["fechar"],
        template: `
            <div v-if="aberto" data-test="modal">
                <slot name="titulo" />
                <slot />
                <div data-test="rodape"><slot name="rodape" /></div>
            </div>
        `,
    }
    const AppButton = {
        props: ["variant", "icon", "loading", "disabled"],
        emits: ["click"],
        template: `<button :disabled="disabled || loading" :data-loading="loading || undefined" @click="$emit('click')"><slot /></button>`,
    }
    return { AppModal, AppButton }
})

// Mock do adminApi — isola o componente do HTTP.
vi.mock("../services/adminApi", () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
    },
}))

import adminApi from "../services/adminApi"
import AdminAlterarSenhaModal from "./AdminAlterarSenhaModal.vue"

function montar(props: Partial<{ aberto: boolean }> = {}) {
    return mount(AdminAlterarSenhaModal, {
        props: { aberto: true, ...props },
    })
}

function inputs(wrapper: ReturnType<typeof montar>) {
    const all = wrapper.findAll("input[type='password'], input[type='text']")
    return {
        senhaAtual:  all[0],
        novaSenha:   all[1],
        confirmacao: all[2],
    }
}

function botaoTrocar(wrapper: ReturnType<typeof montar>) {
    const btns = wrapper.findAll("[data-test='rodape'] button")
    return btns[1] // primeiro é Cancelar, segundo é Trocar senha
}

function botaoCancelar(wrapper: ReturnType<typeof montar>) {
    const btns = wrapper.findAll("[data-test='rodape'] button")
    return btns[0]
}

describe("AdminAlterarSenhaModal", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.mocked(adminApi.post).mockReset()
        vi.mocked(adminApi.get).mockReset()
    })

    // ── CA5 — Botão desabilitado até campos válidos ──────────────────────────

    it("CA5: botão 'Trocar senha' começa desabilitado", async () => {
        const w = montar()
        await flushPromises()
        expect((botaoTrocar(w).element as HTMLButtonElement).disabled).toBe(true)
    })

    it("habilita botão com todos os campos preenchidos, nova >= mínimo e iguais", async () => {
        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("novavalida")
        await confirmacao.setValue("novavalida")

        expect((botaoTrocar(w).element as HTMLButtonElement).disabled).toBe(false)
    })

    it("CA5: mantém botão desabilitado se confirmação não confere", async () => {
        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("novavalida")
        await confirmacao.setValue("diferente")

        expect((botaoTrocar(w).element as HTMLButtonElement).disabled).toBe(true)
        expect(w.text()).toContain("confirmação não confere")
    })

    it("CA4/CA5: mantém botão desabilitado e mostra dica quando nova == atual", async () => {
        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("mesmasenha")
        await novaSenha.setValue("mesmasenha")
        await confirmacao.setValue("mesmasenha")

        expect((botaoTrocar(w).element as HTMLButtonElement).disabled).toBe(true)
        expect(w.text()).toContain("A nova senha precisa ser diferente da atual.")
    })

    // ── CA1 / CA7 — Chamada ao store com senhaAtual ──────────────────────────

    it("CA1/CA7: chama adminApi.post com senhaAtual e novaSenha na troca voluntária", async () => {
        vi.mocked(adminApi.post).mockResolvedValueOnce({}) // change-password
        vi.mocked(adminApi.get).mockResolvedValueOnce({
            data: { id: "uuid", email: "a@b.com", nome: "Admin", ativo: true, forcePasswordReset: false, ultimoLoginEm: null },
        }) // recarregarMe

        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("novavalida")
        await confirmacao.setValue("novavalida")
        await botaoTrocar(w).trigger("click")
        await flushPromises()

        expect(vi.mocked(adminApi.post)).toHaveBeenCalledWith(
            "/auth/change-password",
            { novaSenha: "novavalida", senhaAtual: "atual123" },
        )
    })

    it("emite 'alterada' em sucesso", async () => {
        vi.mocked(adminApi.post).mockResolvedValueOnce({})
        vi.mocked(adminApi.get).mockResolvedValueOnce({
            data: { id: "uuid", email: "a@b.com", nome: "Admin", ativo: true, forcePasswordReset: false, ultimoLoginEm: null },
        })

        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("novavalida")
        await confirmacao.setValue("novavalida")
        await botaoTrocar(w).trigger("click")
        await flushPromises()

        expect(w.emitted("alterada")).toBeTruthy()
    })

    // ── CA2 — Erro do backend exibido (422) ──────────────────────────────────

    it("CA2: exibe mensagem de erro genérica vinda do backend", async () => {
        vi.mocked(adminApi.post).mockRejectedValueOnce({
            response: { status: 422, data: { mensagem: "Senha inválida." } },
        })

        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("errada")
        await novaSenha.setValue("novavalida")
        await confirmacao.setValue("novavalida")
        await botaoTrocar(w).trigger("click")
        await flushPromises()

        expect(w.text()).toContain("Senha inválida.")
        expect(w.emitted("alterada")).toBeFalsy()
    })

    it("usa fallback genérico se backend não enviar mensagem", async () => {
        vi.mocked(adminApi.post).mockRejectedValueOnce(new Error("network"))

        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("novavalida")
        await confirmacao.setValue("novavalida")
        await botaoTrocar(w).trigger("click")
        await flushPromises()

        expect(w.text()).toContain("Não foi possível alterar a senha.")
    })

    // ── Botão Cancelar fecha modal ────────────────────────────────────────────

    it("emite 'fechar' ao clicar em Cancelar", async () => {
        const w = montar()
        await botaoCancelar(w).trigger("click")
        expect(w.emitted("fechar")).toBeTruthy()
    })

    it("não emite 'fechar' durante execução (loading)", async () => {
        // Simula request em andamento: post não resolve.
        let resolvePost: () => void
        vi.mocked(adminApi.post).mockReturnValueOnce(
            new Promise<void>((res) => { resolvePost = res }),
        )
        vi.mocked(adminApi.get).mockResolvedValueOnce({ data: {} })

        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("novavalida")
        await confirmacao.setValue("novavalida")
        await botaoTrocar(w).trigger("click")
        // Request em andamento — clicar Cancelar não deve fechar.
        await botaoCancelar(w).trigger("click")
        await flushPromises()

        expect(w.emitted("fechar")).toBeFalsy()
        resolvePost!()
    })

    // ── Reset de campos ao fechar/reabrir ────────────────────────────────────

    it("reseta campos ao fechar e reabrir o modal", async () => {
        const w = montar()
        const { senhaAtual, novaSenha } = inputs(w)
        await senhaAtual.setValue("preenchida")
        await novaSenha.setValue("preenchida123")

        await w.setProps({ aberto: false })
        await w.setProps({ aberto: true })

        const novosInputs = inputs(w)
        expect((novosInputs.senhaAtual.element as HTMLInputElement).value).toBe("")
        expect((novosInputs.novaSenha.element as HTMLInputElement).value).toBe("")
    })
})
