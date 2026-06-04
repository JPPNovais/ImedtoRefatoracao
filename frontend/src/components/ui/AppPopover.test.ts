import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import { nextTick } from "vue"
import AppPopover from "./AppPopover.vue"

/**
 * CA10 — foco retorna ao elemento gatilho após fechar o popover via Esc.
 *
 * O span wrapper (.app-popover-host) usa display:contents e não é focável,
 * por isso fechar() busca o primeiro elemento focável dentro dele via querySelector.
 */
describe("AppPopover — CA10: foco retorna ao gatilho ao fechar", () => {
    it("devolve foco ao botão gatilho ao fechar com Esc", async () => {
        const w = mount(AppPopover, {
            attachTo: document.body,
            slots: {
                gatilho: `<button class="contador-clicavel">Abrir</button>`,
                conteudo: `<p>Conteúdo</p>`,
            },
        })

        // Abre via API do componente
        await w.vm.abrir()
        expect(w.vm.aberto).toBe(true)

        // Simula Esc — o listener global do componente captura e chama fechar()
        document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true }))

        // Aguarda nextTick interno do fechar() + o do vue-test-utils
        await nextTick()
        await nextTick()

        expect(w.vm.aberto).toBe(false)
        expect(document.activeElement).toBe(w.find(".contador-clicavel").element)

        w.unmount()
    })

    it("foco não retorna se não há elemento focável no gatilho (sem erro)", async () => {
        const w = mount(AppPopover, {
            attachTo: document.body,
            slots: {
                gatilho: `<span>não focável</span>`,
                conteudo: `<p>Conteúdo</p>`,
            },
        })

        // Abre via API
        await w.vm.abrir()
        expect(w.vm.aberto).toBe(true)

        // Fecha via API — não deve lançar mesmo sem elemento focável
        await w.vm.fechar()
        await nextTick()

        expect(w.vm.aberto).toBe(false)

        w.unmount()
    })
})
