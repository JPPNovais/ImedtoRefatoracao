import { describe, it, expect, beforeEach } from "vitest"
import { mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import ConcederGratuidadeModal from "./ConcederGratuidadeModal.vue"

describe("ConcederGratuidadeModal — botão habilita ao preencher", () => {
    beforeEach(() => { setActivePinia(createPinia()) })

    it("habilita 'Conceder gratuidade' com motivos válidos", async () => {
        mount(ConcederGratuidadeModal, {
            props: { estabelecimentoId: 1 },
            attachTo: document.body,
        })
        // espera o teleport do Dialog (reka-ui) montar o conteúdo no body
        await new Promise(r => setTimeout(r, 50))

        const textareas = Array.from(document.querySelectorAll("textarea"))
        expect(textareas.length).toBe(2)

        // campo 1: motivo da gratuidade (>= 20 chars), campo 2: motivo admin (>= 10 chars)
        textareas[0].value = "Parceiro estrategico beta tester de longa data"
        textareas[0].dispatchEvent(new Event("input"))
        textareas[1].value = "Registro administrativo de teste"
        textareas[1].dispatchEvent(new Event("input"))
        await new Promise(r => setTimeout(r, 0))

        const confirmar = Array.from(document.querySelectorAll("button"))
            .find(b => (b.textContent ?? "").includes("Conceder gratuidade"))
        expect(confirmar, "botão Conceder gratuidade não encontrado").toBeTruthy()

        // Se 'disabled' continuar presente, o bug está reproduzido.
        expect(confirmar!.hasAttribute("disabled")).toBe(false)
    })
})
