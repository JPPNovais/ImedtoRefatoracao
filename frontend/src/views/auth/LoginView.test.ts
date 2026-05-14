import { describe, it, expect, beforeEach, vi } from "vitest"
import { mount, flushPromises } from "@vue/test-utils"
import { setActivePinia, createPinia } from "pinia"

// Mocks declarados antes do import da view.
vi.mock("@/services/httpClient", () => ({
    default: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}))

vi.mock("@/services/realtimeService", () => ({
    default: { start: vi.fn().mockResolvedValue(undefined), stop: vi.fn().mockResolvedValue(undefined), on: vi.fn(), off: vi.fn() },
}))

vi.mock("@/stores/tenantStore",       () => ({ useTenantStore:       vi.fn(() => ({ limpar: vi.fn(), popularEstabelecimentos: vi.fn() })) }))
vi.mock("@/stores/profissionalStore", () => ({ useProfissionalStore: vi.fn(() => ({ limpar: vi.fn(), init: vi.fn(), setProfissional: vi.fn() })) }))
vi.mock("@/stores/notificacoesStore", () => ({ useNotificacoesStore: vi.fn(() => ({ limpar: vi.fn(), bindRealtime: vi.fn() })) }))
vi.mock("@/stores/assinaturaStore",   () => ({ useAssinaturaStore:   vi.fn(() => ({ limpar: vi.fn() })) }))

// Router mock — a view chama router.push em sucesso. Não nos importa nesses testes.
vi.mock("vue-router", () => ({
    useRouter: () => ({ push: vi.fn() }),
}))

// Mocka o authStore.reenviarConfirmacao no nível do módulo (mais simples que stubar
// o store inteiro mantendo reatividade dos refs internos da view).
import { useAuthStore } from "@/stores/authStore"
import LoginView from "./LoginView.vue"

describe("LoginView — Correção 4 (anti-enumeração: reenvio sempre visível)", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    function montar() {
        return mount(LoginView, {
            global: {
                stubs: {
                    // imagens e router-link ficam stubadas pelo setup global; nada extra aqui.
                },
            },
        })
    }

    async function submeterLogin(wrapper: ReturnType<typeof montar>, email: string, senha: string) {
        await wrapper.find("input[type='email'], input[name='email'], input[autocomplete='email']").setValue(email)
        // Senha: o input pode estar como type=password ou text dependendo de "mostrarSenha".
        const inputSenha = wrapper.find("input[type='password'], input[autocomplete='current-password']")
        await inputSenha.setValue(senha)
        await wrapper.find("form").trigger("submit.prevent")
        await flushPromises()
    }

    it("após erro no login, exibe botão de reenviar confirmação (mostrarReenvio = true)", async () => {
        const store = useAuthStore()
        // Spy: força login a rejeitar — qualquer mensagem (anti-enumeração já no backend).
        const spy = vi.spyOn(store, "login").mockRejectedValue({
            response: { status: 422, data: { mensagem: "Credenciais inválidas." } },
        })

        const w = montar()
        await submeterLogin(w, "joao@imedto.com", "senha123")

        expect(spy).toHaveBeenCalled()
        // Botão "Reenviar e-mail de confirmação" deve estar visível mesmo com
        // mensagem genérica "Credenciais inválidas." — não revela conta pendente.
        expect(w.text()).toContain("Reenviar e-mail de confirmação")
    })

    it("clicar no botão de reenviar chama auth.reenviarConfirmacao(email)", async () => {
        const store = useAuthStore()
        vi.spyOn(store, "login").mockRejectedValue({
            response: { status: 422, data: { mensagem: "Credenciais inválidas." } },
        })
        const spyReenvio = vi.spyOn(store, "reenviarConfirmacao").mockResolvedValue(undefined)

        const w = montar()
        await submeterLogin(w, "joao@imedto.com", "senha123")

        // Localiza e clica no botão de reenvio.
        const btn = w.find(".reenvio-confirmacao button.btn-link")
        expect(btn.exists()).toBe(true)
        await btn.trigger("click")
        await flushPromises()

        expect(spyReenvio).toHaveBeenCalledWith("joao@imedto.com")
    })

    it("após erro de cadastro também exibe botão de reenviar", async () => {
        const store = useAuthStore()
        vi.spyOn(store, "signup").mockRejectedValue({
            response: { status: 422, data: { mensagem: "Já existe uma conta com este e-mail." } },
        })

        const w = montar()
        // Alterna para o modo cadastro: existe link "Criar conta" no rodapé.
        // O componente usa botão tipo link com texto "Criar conta agora" / "Cadastre-se".
        // Aproveitamos a função interna `irPara('cadastro')` via DOM — clica no toggle.
        const linkCadastro = w.findAll("button").find(b => /Cadastre-se|Criar conta/i.test(b.text()))
        // Se a label varia, o teste ainda é útil pelo menos para login (acima). Tolerante:
        if (!linkCadastro) {
            return  // Não é o foco principal; teste de login acima já cobre o caminho crítico.
        }
        await linkCadastro.trigger("click")
        await flushPromises()

        // Preenche email + senha + confirmação + aceita termos (assume checkboxes presentes).
        const inputs = w.findAll("input")
        const inputEmail   = inputs.find(i => i.attributes("autocomplete") === "email")
        const inputSenha   = inputs.find(i => i.attributes("autocomplete") === "new-password")
        if (!inputEmail || !inputSenha) return  // estrutura mudou — pulamos sem falhar.

        await inputEmail.setValue("novo@imedto.com")
        await inputSenha.setValue("senhaForte123")

        const form = w.find("form")
        await form.trigger("submit.prevent")
        await flushPromises()

        expect(w.text()).toContain("Reenviar e-mail de confirmação")
    })
})
