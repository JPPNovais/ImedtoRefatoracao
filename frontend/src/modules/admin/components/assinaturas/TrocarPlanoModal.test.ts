import { describe, it, expect, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import TrocarPlanoModal from "./TrocarPlanoModal.vue"

describe("TrocarPlanoModal — botão habilita ao preencher", () => {
    beforeEach(() => { setActivePinia(createPinia()) })

    it("habilita 'Confirmar' com plano selecionado + motivo >= 10 chars", async () => {
        mount(TrocarPlanoModal, {
            props: {
                estabelecimentoId: 1,
                planos: [
                    { id: "p-1", nome: "Plano Pro", ativo: true } as never,
                    { id: "p-2", nome: "Plano Free", ativo: true } as never,
                ],
            },
            attachTo: document.body,
        })
        await new Promise(r => setTimeout(r, 50))

        const select = document.querySelector("select") as HTMLSelectElement
        expect(select, "select de plano não encontrado").toBeTruthy()
        select.value = "p-1"
        select.dispatchEvent(new Event("change"))

        const textarea = document.querySelector("textarea") as HTMLTextAreaElement
        expect(textarea, "textarea de motivo não encontrada").toBeTruthy()
        textarea.value = "Troca para teste interno"
        textarea.dispatchEvent(new Event("input"))

        await new Promise(r => setTimeout(r, 0))

        const confirmar = Array.from(document.querySelectorAll("button"))
            .find(b => (b.textContent ?? "").trim() === "Confirmar")
        expect(confirmar, "botão Confirmar não encontrado").toBeTruthy()
        expect(confirmar!.hasAttribute("disabled")).toBe(false)
    })
})
