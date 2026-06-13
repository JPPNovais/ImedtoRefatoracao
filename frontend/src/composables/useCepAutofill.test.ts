import { describe, it, expect, vi, beforeEach, afterEach } from "vitest"
import { ref, nextTick } from "vue"
import { useCepAutofill } from "./useCepAutofill"
import * as viaCepService from "@/services/viaCepService"

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

const enderecoFixture = {
    cep: "01311-100",
    logradouro: "Avenida Paulista",
    bairro: "Bela Vista",
    complemento: "",
    cidade: "São Paulo",
    uf: "SP",
}

// ─────────────────────────────────────────────────────────────────────────────
// Testes
// ─────────────────────────────────────────────────────────────────────────────

describe("useCepAutofill", () => {
    beforeEach(() => {
        vi.useFakeTimers()
        vi.spyOn(viaCepService, "buscarPorCep").mockResolvedValue(enderecoFixture)
    })

    afterEach(() => {
        vi.useRealTimers()
        vi.restoreAllMocks()
    })

    // ── CA1 / CA2 (caminho feliz + debounce) ─────────────────────────────────

    it("CA1/CA2: dispara busca com debounce ao atingir 8 dígitos", async () => {
        const cep = ref("")
        const onEndereco = vi.fn()
        useCepAutofill(cep, onEndereco, { delay: 300 })

        cep.value = "01311-10"
        await nextTick()
        vi.advanceTimersByTime(200)
        expect(viaCepService.buscarPorCep).not.toHaveBeenCalled()

        cep.value = "01311-100"  // 8 dígitos
        await nextTick()
        vi.advanceTimersByTime(299)
        expect(viaCepService.buscarPorCep).not.toHaveBeenCalled()

        vi.advanceTimersByTime(1)  // completa 300ms
        await nextTick()
        // espera a promise interna
        await vi.runAllTimersAsync()
        await nextTick()

        expect(viaCepService.buscarPorCep).toHaveBeenCalledWith("01311100")
        expect(onEndereco).toHaveBeenCalledWith(enderecoFixture)
    })

    // ── CA3 (limpeza imediata síncrona ao encolher) ───────────────────────────

    it("CA3: chama onLimpar imediatamente ao CEP ficar < 8 dígitos (síncrono)", async () => {
        const cep = ref("01311-100")
        const onLimpar = vi.fn()
        const onEndereco = vi.fn()
        useCepAutofill(cep, onEndereco, { onLimpar, delay: 300 })

        // Apaga um dígito — deve limpar imediatamente, sem esperar debounce
        cep.value = "01311-10"
        await nextTick()

        expect(onLimpar).toHaveBeenCalledTimes(1)
        // O debounce NÃO completou ainda — a limpeza foi síncrona
        expect(onEndereco).not.toHaveBeenCalled()
    })

    it("CA3: não chama onLimpar quando CEP chega a 8 dígitos (crescimento)", async () => {
        const cep = ref("")
        const onLimpar = vi.fn()
        useCepAutofill(cep, vi.fn(), { onLimpar, delay: 300 })

        cep.value = "01311-100"
        await nextTick()

        expect(onLimpar).not.toHaveBeenCalled()
    })

    // ── CA4 (guard de race — última requisição vence) ─────────────────────────

    it("CA4: descarta resposta do CEP A quando CEP B já foi digitado antes da resposta", async () => {
        const cep = ref("")
        const onEndereco = vi.fn()

        // Simula duas chamadas com respostas em ordem invertida:
        // chamada do CEP A resolve depois da chamada do CEP B
        const enderecoA = { ...enderecoFixture, cidade: "Rio de Janeiro", uf: "RJ" }
        const enderecoB = { ...enderecoFixture, cidade: "São Paulo", uf: "SP" }

        let resolveA!: (v: typeof enderecoA) => void
        let resolveB!: (v: typeof enderecoB) => void
        const promiseA = new Promise<typeof enderecoA>(r => { resolveA = r })
        const promiseB = new Promise<typeof enderecoB>(r => { resolveB = r })

        vi.mocked(viaCepService.buscarPorCep)
            .mockReturnValueOnce(promiseA)
            .mockReturnValueOnce(promiseB)

        useCepAutofill(cep, onEndereco, { delay: 300 })

        // Digita CEP A — dispara requisição A
        cep.value = "21941-901"
        await nextTick()
        vi.advanceTimersByTime(300)
        await nextTick()

        // Antes de A resolver, digita CEP B — dispara requisição B
        cep.value = "01311-100"
        await nextTick()
        vi.advanceTimersByTime(300)
        await nextTick()

        // B resolve primeiro (mais recente)
        resolveB(enderecoB)
        await nextTick()
        await nextTick()

        // A resolve depois (obsoleto — deve ser descartado)
        resolveA(enderecoA)
        await nextTick()
        await nextTick()

        // onEndereco deve ter sido chamado apenas com B
        const calls = onEndereco.mock.calls
        const chamadasComRj = calls.filter(c => c[0]?.uf === "RJ")
        expect(chamadasComRj).toHaveLength(0)
        expect(onEndereco).toHaveBeenCalledWith(expect.objectContaining({ uf: "SP" }))
    })

    // ── CA6 (erro silencioso — CEP inexistente) ───────────────────────────────

    it("CA6: não chama onEndereco quando ViaCEP retorna null (CEP inexistente)", async () => {
        vi.mocked(viaCepService.buscarPorCep).mockResolvedValue(null)
        const cep = ref("")
        const onEndereco = vi.fn()
        useCepAutofill(cep, onEndereco, { delay: 300 })

        cep.value = "99999-999"
        await nextTick()
        await vi.runAllTimersAsync()
        await nextTick()

        expect(onEndereco).not.toHaveBeenCalled()
    })

    // ── CA7 (erro silencioso — falha de rede) ────────────────────────────────

    it("CA7: não propaga exceção de rede ao chamador", async () => {
        vi.mocked(viaCepService.buscarPorCep).mockRejectedValue(new Error("network error"))
        const cep = ref("")
        const onEndereco = vi.fn()

        // useCepAutofill tem try/catch interno — não relança (R6)
        useCepAutofill(cep, onEndereco, { delay: 300 })

        cep.value = "01311-100"
        await nextTick()
        await expect(vi.runAllTimersAsync()).resolves.not.toThrow()
        expect(onEndereco).not.toHaveBeenCalled()
    })

    // ── CA8 (loading) ─────────────────────────────────────────────────────────

    it("CA8: buscando é true durante a requisição e false após resolver", async () => {
        // Este teste usa real timers para evitar conflito entre fake timers e microtasks.
        vi.useRealTimers()

        let resolveBusca!: (v: typeof enderecoFixture) => void
        const promise = new Promise<typeof enderecoFixture>(r => { resolveBusca = r })
        vi.mocked(viaCepService.buscarPorCep).mockReturnValue(promise)

        const cep = ref("")
        const { buscando } = useCepAutofill(cep, vi.fn(), { delay: 50 })

        expect(buscando.value).toBe(false)

        cep.value = "01311-100"
        await nextTick()

        // Aguarda o debounce de 50ms disparar
        await new Promise(r => setTimeout(r, 80))
        await nextTick()

        // Promise ainda pendente — buscando deve ser true
        expect(buscando.value).toBe(true)

        // Resolve a promise e aguarda microtasks + ticks do Vue
        resolveBusca(enderecoFixture)
        await new Promise(r => setTimeout(r, 10))
        await nextTick()

        expect(buscando.value).toBe(false)
    })

    // ── Não dispara busca com menos de 8 dígitos ─────────────────────────────

    it("não dispara busca quando CEP tem 7 dígitos", async () => {
        const cep = ref("0131110")  // 7 dígitos
        useCepAutofill(cep, vi.fn(), { delay: 300 })

        await vi.runAllTimersAsync()
        await nextTick()

        expect(viaCepService.buscarPorCep).not.toHaveBeenCalled()
    })

    // ── marcarCarga — prevenção de disparo no mount em modo edição ────────────

    it("marcarCarga: não dispara busca quando o form é populado programaticamente com CEP de 8 dígitos (modo edição)", async () => {
        // Simula o cenário: composable criado com form vazio, depois popularComPaciente
        // seta form.cep = "31555-190" e chama marcarCarga — o disparo do debounce
        // com esse valor deve ser ignorado (CA12 / ponto de atenção briefing).
        const cep = ref("")
        const onEndereco = vi.fn()
        const { marcarCarga } = useCepAutofill(cep, onEndereco, { delay: 300 })

        // Simula popularComPaciente: seta o CEP e marca como carga
        cep.value = "31555-190"
        marcarCarga("31555-190")

        await nextTick()
        await vi.runAllTimersAsync()
        await nextTick()

        expect(viaCepService.buscarPorCep).not.toHaveBeenCalled()
        expect(onEndereco).not.toHaveBeenCalled()
    })

    it("marcarCarga: após ignorar o disparo de carga, mudança posterior do usuário dispara busca normalmente", async () => {
        // Garante CA3 / CA1 em modo edição: trocar CEP após carga deve funcionar.
        const cep = ref("")
        const onEndereco = vi.fn()
        const { marcarCarga } = useCepAutofill(cep, onEndereco, { delay: 300 })

        // Carga inicial (modo edição)
        cep.value = "31555-190"
        marcarCarga("31555-190")
        await nextTick()
        await vi.runAllTimersAsync()
        await nextTick()

        expect(onEndereco).not.toHaveBeenCalled()  // não disparou no mount

        // Usuário digita um novo CEP
        cep.value = "01311-100"
        await nextTick()
        await vi.runAllTimersAsync()
        await nextTick()

        expect(viaCepService.buscarPorCep).toHaveBeenCalledWith("01311100")
        expect(onEndereco).toHaveBeenCalledWith(enderecoFixture)
    })

    it("marcarCarga: sem marcarCarga, CEP com 8 dígitos no form (sem carga marcada) dispara busca normalmente", async () => {
        // Garante que marcarCarga é opt-in: sem chamá-la, tudo funciona como antes.
        const cep = ref("")
        const onEndereco = vi.fn()
        useCepAutofill(cep, onEndereco, { delay: 300 })

        cep.value = "01311-100"
        await nextTick()
        await vi.runAllTimersAsync()
        await nextTick()

        expect(viaCepService.buscarPorCep).toHaveBeenCalledWith("01311100")
        expect(onEndereco).toHaveBeenCalledWith(enderecoFixture)
    })

    it("marcarCarga: após onLimpar (usuário apagou), cepDeCarga é resetado e novo CEP igual ao anterior de carga busca normalmente", async () => {
        // Caso: modo edição, carga com CEP "31555-190", usuário apaga, redigita o mesmo CEP
        // — deve buscar (não deve ser bloqueado pelo guard de carga já expirado).
        const cep = ref("")
        const onLimpar = vi.fn()
        const onEndereco = vi.fn()
        const { marcarCarga } = useCepAutofill(cep, onEndereco, { onLimpar, delay: 300 })

        // Carga inicial — guard pula o disparo
        cep.value = "31555-190"
        marcarCarga("31555-190")
        await nextTick()
        await vi.runAllTimersAsync()
        await nextTick()
        expect(onEndereco).not.toHaveBeenCalled()

        // Usuário apaga o CEP (limpeza — reseta cepDeCarga)
        // Deixa o debounce rodar para que cepDebounced mude de "31555-190" para
        // "31555-18" — isso é necessário para que, quando o usuário redigitar
        // "31555-190", o watch do debounced detecte mudança de valor.
        cep.value = "31555-18"
        await nextTick()
        expect(onLimpar).toHaveBeenCalledTimes(1)
        await vi.runAllTimersAsync()  // cepDebounced muda para "31555-18"
        await nextTick()

        // Usuário redigita o mesmo CEP que era o de carga — deve buscar agora
        // (cepDeCarga foi resetado pelo onLimpar; e cepDebounced estava em "31555-18"
        // então muda para "31555-190" disparando o watch)
        cep.value = "31555-190"
        await nextTick()
        await vi.runAllTimersAsync()
        await nextTick()

        expect(viaCepService.buscarPorCep).toHaveBeenCalledWith("31555190")
        expect(onEndereco).toHaveBeenCalledWith(enderecoFixture)
    })
})
