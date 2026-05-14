import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import { setActivePinia, createPinia } from "pinia"

// Mocka o barrel do design system pra evitar resolver @imedto/ui (pacote externo).
vi.mock("@/components/ui", () => {
    const AppModal = {
        props: ["aberto", "largura", "titulo"],
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

import AlterarSenhaModal from "./AlterarSenhaModal.vue"

// Mocks do httpClient e dependências de outras stores (authStore.alterarSenha chama POST).
vi.mock("@/services/httpClient", () => ({
    default: {
        get:    vi.fn(),
        post:   vi.fn(),
        delete: vi.fn(),
    },
}))

vi.mock("@/services/realtimeService", () => ({
    default: {
        start: vi.fn().mockResolvedValue(undefined),
        stop:  vi.fn().mockResolvedValue(undefined),
        on:    vi.fn(),
        off:   vi.fn(),
    },
}))

vi.mock("@/stores/tenantStore",         () => ({ useTenantStore:         vi.fn(() => ({ limpar: vi.fn(), popularEstabelecimentos: vi.fn() })) }))
vi.mock("@/stores/profissionalStore",   () => ({ useProfissionalStore:   vi.fn(() => ({ limpar: vi.fn(), init: vi.fn(), setProfissional: vi.fn() })) }))
vi.mock("@/stores/notificacoesStore",   () => ({ useNotificacoesStore:   vi.fn(() => ({ limpar: vi.fn(), bindRealtime: vi.fn() })) }))
vi.mock("@/stores/assinaturaStore",     () => ({ useAssinaturaStore:     vi.fn(() => ({ limpar: vi.fn() })) }))

import httpClient from "@/services/httpClient"

function montar(props: Partial<{ aberto: boolean }> = {}) {
    return mount(AlterarSenhaModal, {
        props: { aberto: true, ...props },
    })
}

/** Acessa os inputs por ordem de declaração (senha atual, nova, confirmar). */
function inputs(wrapper: ReturnType<typeof montar>) {
    const all = wrapper.findAll("input[type='password'], input[type='text']")
    // Os 3 primeiros são senha atual / nova / confirmar; o 4o é o checkbox "mostrar senhas".
    return {
        senhaAtual:  all[0],
        novaSenha:   all[1],
        confirmacao: all[2],
    }
}

function botaoTrocar(wrapper: ReturnType<typeof montar>) {
    // O botão "Trocar senha" é o segundo no rodapé (o primeiro é Cancelar).
    const btns = wrapper.findAll("[data-test='rodape'] button")
    return btns[1]
}

function botaoCancelar(wrapper: ReturnType<typeof montar>) {
    const btns = wrapper.findAll("[data-test='rodape'] button")
    return btns[0]
}

describe("AlterarSenhaModal", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.mocked(httpClient.post).mockReset()
    })

    it("botão 'Trocar senha' começa desabilitado", async () => {
        const w = montar()
        await flushPromises()
        const btn = botaoTrocar(w)
        expect((btn.element as HTMLButtonElement).disabled).toBe(true)
    })

    it("habilita o botão quando os 3 campos estão preenchidos, nova ≥ 8 e iguais", async () => {
        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("novaforte9")
        await confirmacao.setValue("novaforte9")

        const btn = botaoTrocar(w)
        expect((btn.element as HTMLButtonElement).disabled).toBe(false)
    })

    it("mantém botão desabilitado se nova senha tem menos de 8 caracteres", async () => {
        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("curta1!")  // 7 chars
        await confirmacao.setValue("curta1!")

        const btn = botaoTrocar(w)
        expect((btn.element as HTMLButtonElement).disabled).toBe(true)
    })

    it("mantém botão desabilitado se confirmação não confere", async () => {
        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("novaforte9")
        await confirmacao.setValue("diferente1")

        const btn = botaoTrocar(w)
        expect((btn.element as HTMLButtonElement).disabled).toBe(true)
    })

    it("mostra dica local quando nova senha == atual", async () => {
        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("mesmasenha123")
        await novaSenha.setValue("mesmasenha123")
        await confirmacao.setValue("mesmasenha123")

        expect(w.text()).toContain("A nova senha precisa ser diferente da atual.")
        // Trava local também: botão fica desabilitado mesmo com confirmação batendo.
        expect((botaoTrocar(w).element as HTMLButtonElement).disabled).toBe(true)
    })

    it("chama authStore.alterarSenha ao clicar em 'Trocar senha'", async () => {
        vi.mocked(httpClient.post).mockResolvedValueOnce({})
        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("novaforte9")
        await confirmacao.setValue("novaforte9")

        await botaoTrocar(w).trigger("click")
        await flushPromises()

        expect(httpClient.post).toHaveBeenCalledWith("/auth/alterar-senha", {
            senhaAtual: "atual123",
            novaSenha:  "novaforte9",
        })
    })

    it("emite 'alterada' em sucesso", async () => {
        vi.mocked(httpClient.post).mockResolvedValueOnce({})
        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("novaforte9")
        await confirmacao.setValue("novaforte9")
        await botaoTrocar(w).trigger("click")
        await flushPromises()

        expect(w.emitted("alterada")).toBeTruthy()
        expect(w.emitted("alterada")).toHaveLength(1)
    })

    it("mostra a mensagem de erro vinda do backend (422 com 'mensagem')", async () => {
        vi.mocked(httpClient.post).mockRejectedValueOnce({
            response: { status: 422, data: { mensagem: "Senha atual incorreta." } },
        })
        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("erradinha")
        await novaSenha.setValue("novaforte9")
        await confirmacao.setValue("novaforte9")
        await botaoTrocar(w).trigger("click")
        await flushPromises()

        expect(w.text()).toContain("Senha atual incorreta.")
        // Não emite 'alterada' em erro.
        expect(w.emitted("alterada")).toBeFalsy()
    })

    it("usa fallback genérico se backend não enviar 'mensagem'", async () => {
        vi.mocked(httpClient.post).mockRejectedValueOnce(new Error("network"))
        const w = montar()
        const { senhaAtual, novaSenha, confirmacao } = inputs(w)

        await senhaAtual.setValue("atual123")
        await novaSenha.setValue("novaforte9")
        await confirmacao.setValue("novaforte9")
        await botaoTrocar(w).trigger("click")
        await flushPromises()

        expect(w.text()).toContain("Não foi possível alterar a senha.")
    })

    it("reseta os campos quando aberto vira false", async () => {
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
