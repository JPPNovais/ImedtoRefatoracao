import { describe, it, expect } from "vitest"
import { useConfirm } from "./useConfirm"

describe("useConfirm", () => {
    it("inicia fechado com valores padrao", () => {
        const { dialogo } = useConfirm()
        expect(dialogo.value.aberto).toBe(false)
        expect(dialogo.value.variant).toBe("danger")
        expect(dialogo.value.labelConfirmar).toBe("Confirmar")
    })

    it("confirmar() abre o dialogo com a mensagem", () => {
        const { confirmar, dialogo } = useConfirm()
        const promise = confirmar("Tem certeza?")
        expect(dialogo.value.aberto).toBe(true)
        expect(dialogo.value.mensagem).toBe("Tem certeza?")
        // Cleanup: resolve para nao deixar promise pendente
        promise.then(() => {})
    })

    it("confirmar() aceita opcoes customizadas (titulo, variant, label)", () => {
        const { confirmar, dialogo } = useConfirm()
        confirmar("Excluir?", {
            titulo: "Atencao",
            variant: "primary",
            labelConfirmar: "Sim, excluir",
        })
        expect(dialogo.value.titulo).toBe("Atencao")
        expect(dialogo.value.variant).toBe("primary")
        expect(dialogo.value.labelConfirmar).toBe("Sim, excluir")
    })

    it("responder(true) resolve a promise com true e fecha o dialogo", async () => {
        const { confirmar, responder, dialogo } = useConfirm()
        const promise = confirmar("Confirma?")
        responder(true)
        await expect(promise).resolves.toBe(true)
        expect(dialogo.value.aberto).toBe(false)
    })

    it("responder(false) resolve com false", async () => {
        const { confirmar, responder } = useConfirm()
        const promise = confirmar("Confirma?")
        responder(false)
        await expect(promise).resolves.toBe(false)
    })

    it("multiplas chamadas de confirmar resolvem apenas a ultima", async () => {
        const { confirmar, responder } = useConfirm()
        const primeira = confirmar("Primeira?")
        const segunda = confirmar("Segunda?")
        responder(true)
        // A primeira NAO deve resolver (resolver foi sobrescrito).
        await expect(segunda).resolves.toBe(true)
        // Verificar que primeira ainda esta pendente — usar Promise.race com timeout
        const aindaPendente = await Promise.race([
            primeira.then(() => "resolveu"),
            new Promise((r) => setTimeout(() => r("pendente"), 30)),
        ])
        expect(aindaPendente).toBe("pendente")
    })
})
