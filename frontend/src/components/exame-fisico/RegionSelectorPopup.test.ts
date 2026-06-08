import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import RegionSelectorPopup from "./RegionSelectorPopup.vue"
import type { ExameFisicoRegiao, MembroRegioes } from "./RegionSelectorPopup.vue"

// ─── Catálogo mínimo para os testes ──────────────────────────────────────────

function regiao(overrides: Partial<ExameFisicoRegiao> & { id: string; nome: string }): ExameFisicoRegiao {
    return {
        nivel: 2,
        lateralidade: false,
        pai_id: null,
        vista: null,
        template_texto: null,
        ...overrides,
    }
}

// Membro superior direito (base)
const membroSupDirBase: ExameFisicoRegiao = regiao({
    id: "membro-superior-direito-anterior",
    nome: "Membro superior direito (anterior)",
    nivel: 1,
})

// Membro superior esquerdo (base)
const membroSupEsqBase: ExameFisicoRegiao = regiao({
    id: "membro-superior-esquerdo-anterior",
    nome: "Membro superior esquerdo (anterior)",
    nivel: 1,
})

// Sub-regiões do lado direito
const ombroDireito: ExameFisicoRegiao = regiao({
    id: "ombro-direito",
    nome: "Ombro direito",
    pai_id: "membro-superior-direito-anterior",
})

const cotovelo: ExameFisicoRegiao = regiao({
    id: "cotovelo-direito",
    nome: "Cotovelo direito",
    pai_id: "membro-superior-direito-anterior",
})

// Sub-regiões do lado esquerdo
const ombro_esq: ExameFisicoRegiao = regiao({
    id: "ombro-esquerdo",
    nome: "Ombro esquerdo",
    pai_id: "membro-superior-esquerdo-anterior",
})

// Sub-região não-lateral (para CA5)
const toraxBase: ExameFisicoRegiao = regiao({
    id: "torax-anterior",
    nome: "Tórax (anterior)",
    nivel: 1,
})
const pleura: ExameFisicoRegiao = regiao({
    id: "pleura",
    nome: "Pleura",
    lateralidade: true,
    pai_id: "torax-anterior",
})

const catalogoMembro = [membroSupDirBase, membroSupEsqBase, ombroDireito, cotovelo, ombro_esq]
const catalogoTorax  = [toraxBase, pleura]

function getFilhosMembro(regiaoId: string): ExameFisicoRegiao[] {
    return catalogoMembro.filter(r => r.pai_id === regiaoId)
}

function getFilhosTorax(regiaoId: string): ExameFisicoRegiao[] {
    return catalogoTorax.filter(r => r.pai_id === regiaoId)
}

const membroRegioes: MembroRegioes = {
    tipo: "superior",
    dirBase: membroSupDirBase,
    esquBase: membroSupEsqBase,
}

// ─── Stub do AppModal: renderiza apenas os slots default e rodape ─────────────
// O Dialog do design system usa Teleport; o stub evita erros de DOM no happy-dom.
const AppModalStub = {
    name: "AppModal",
    template: `<div class="app-modal-stub"><slot /><slot name="rodape" /></div>`,
}

function montarPopup(opts: {
    regiaoClicada: ExameFisicoRegiao | null
    membroRegioes?: MembroRegioes | null
    regioesJaSelecionadas?: string[]
    getFilhos?: (id: string) => ExameFisicoRegiao[]
}) {
    return mount(RegionSelectorPopup, {
        props: {
            aberto: true,
            regiaoClicada: opts.regiaoClicada,
            regioes: opts.membroRegioes ? catalogoMembro : catalogoTorax,
            regioesJaSelecionadas: opts.regioesJaSelecionadas ?? [],
            getFilhos: opts.getFilhos ?? getFilhosMembro,
            membroRegioes: opts.membroRegioes ?? null,
        },
        global: {
            stubs: { AppModal: AppModalStub },
        },
    })
}

// ─── Testes ───────────────────────────────────────────────────────────────────

describe("RegionSelectorPopup — fluxo de membro", () => {
    it("CA1 — Direito: confirmar 2 sub-regiões gera 2 entradas com lateralidade D e ids da base direita", async () => {
        const wrapper = montarPopup({
            regiaoClicada: membroSupDirBase,
            membroRegioes,
            getFilhos: getFilhosMembro,
        })

        // Passo 1: clicar em "Direito" (AppPillToggle emite update:modelValue)
        await wrapper.findComponent({ name: "AppPillToggle" }).vm.$emit("update:modelValue", "D")
        await wrapper.vm.$nextTick()

        // Passo 2: marcar ombro e cotovelo
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        expect(checkboxes.length).toBeGreaterThanOrEqual(2)

        // Clica nos dois primeiros checkboxes
        await checkboxes[0].trigger("change")
        await checkboxes[1].trigger("change")

        // Confirmar
        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        expect(botaoConfirmar).toBeTruthy()
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        expect(eventos).toBeTruthy()
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; lateralidade: string | null }>
        expect(selecoes).toHaveLength(2)
        expect(selecoes.every(s => s.lateralidade === "D")).toBe(true)
        // Ids devem pertencer à base direita
        const idsDir = [ombroDireito.id, cotovelo.id, membroSupDirBase.id]
        expect(selecoes.every(s => idsDir.includes(s.regiaoId))).toBe(true)
    })

    it("CA2 — Esquerdo: sub-regiões exibidas são da base esquerda; entry gerada tem lateralidade E", async () => {
        const wrapper = montarPopup({
            regiaoClicada: membroSupDirBase,
            membroRegioes,
            getFilhos: getFilhosMembro,
        })

        // Avançar com "Esquerdo"
        await wrapper.findComponent({ name: "AppPillToggle" }).vm.$emit("update:modelValue", "E")
        await wrapper.vm.$nextTick()

        // O passo de sub-regiões deve listar filhos de esquBase
        // Verificação indireta: confirmar 1 sub-região (ombro esquerdo)
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[0].trigger("change")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; lateralidade: string | null }>
        expect(selecoes).toHaveLength(1)
        expect(selecoes[0].lateralidade).toBe("E")
        // Id deve pertencer à base esquerda
        const idsEsq = [ombro_esq.id, membroSupEsqBase.id]
        expect(idsEsq.includes(selecoes[0].regiaoId)).toBe(true)
    })

    it("CA3 — Ambos: confirmar 1 sub-região gera EXATAMENTE 1 entrada com lateralidade bilateral (não 2)", async () => {
        const wrapper = montarPopup({
            regiaoClicada: membroSupDirBase,
            membroRegioes,
            getFilhos: getFilhosMembro,
        })

        // Avançar com "Ambos"
        await wrapper.findComponent({ name: "AppPillToggle" }).vm.$emit("update:modelValue", "bilateral")
        await wrapper.vm.$nextTick()

        // Marcar 1 sub-região
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[0].trigger("change")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; lateralidade: string | null }>

        // Exatamente 1 entrada — não 2 (R5/CA3)
        expect(selecoes).toHaveLength(1)
        expect(selecoes[0].lateralidade).toBe("bilateral")
        // Id da base direita (canônica)
        const idsDir = [ombroDireito.id, cotovelo.id, membroSupDirBase.id]
        expect(idsDir.includes(selecoes[0].regiaoId)).toBe(true)
        // Verificação negativa: nenhuma entrada com lateralidade E
        expect(selecoes.some(s => s.lateralidade === "E")).toBe(false)
    })

    it("CA4 — Passo de sub-regiões de membro NÃO exibe botões D/E/Bilateral por sub-região", async () => {
        const wrapper = montarPopup({
            regiaoClicada: membroSupDirBase,
            membroRegioes,
            getFilhos: getFilhosMembro,
        })

        await wrapper.findComponent({ name: "AppPillToggle" }).vm.$emit("update:modelValue", "D")
        await wrapper.vm.$nextTick()

        // Marcar uma sub-região para que os botões aparecessem caso existissem
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[0].trigger("change")
        await wrapper.vm.$nextTick()

        // Botões D, E, Bilateral (tamanho tiny inline) NÃO devem aparecer
        const botoes = wrapper.findAll("button")
        const textosBotoes = botoes.map(b => b.text().trim())
        expect(textosBotoes.some(t => t === "D")).toBe(false)
        expect(textosBotoes.some(t => t === "E")).toBe(false)
        expect(textosBotoes.some(t => t === "Bilateral")).toBe(false)
    })

    it("CA6 — Voltar no passo de sub-regiões retorna ao passo de lado (vê as 3 opções)", async () => {
        const wrapper = montarPopup({
            regiaoClicada: membroSupDirBase,
            membroRegioes,
            getFilhos: getFilhosMembro,
        })

        // Avançar para sub-regiões
        await wrapper.findComponent({ name: "AppPillToggle" }).vm.$emit("update:modelValue", "D")
        await wrapper.vm.$nextTick()

        // AppPillToggle não deve aparecer no passo de sub-regiões
        expect(wrapper.findComponent({ name: "AppPillToggle" }).exists()).toBe(false)

        // Clicar em "Voltar"
        const botaoVoltar = wrapper.findAll("button").find(b => b.text().includes("Voltar"))
        expect(botaoVoltar).toBeTruthy()
        await botaoVoltar!.trigger("click")
        await wrapper.vm.$nextTick()

        // Deve voltar ao passo de lado — AppPillToggle reaparece
        expect(wrapper.findComponent({ name: "AppPillToggle" }).exists()).toBe(true)
    })

    it("CA7 — Cancelar no passo de lado fecha sem emitir confirmar", async () => {
        const wrapper = montarPopup({
            regiaoClicada: membroSupDirBase,
            membroRegioes,
            getFilhos: getFilhosMembro,
        })

        // No passo de lado, clicar Cancelar
        const botaoCancelar = wrapper.findAll("button").find(b => b.text().includes("Cancelar"))
        expect(botaoCancelar).toBeTruthy()
        await botaoCancelar!.trigger("click")

        expect(wrapper.emitted("confirmar")).toBeFalsy()
        expect(wrapper.emitted("update:aberto")).toBeTruthy()
        expect(wrapper.emitted("update:aberto")![0]![0]).toBe(false)
    })

    it("CA8 — Após fechar, estado é limpo (passo volta a lado, sem lado pré-selecionado)", async () => {
        const wrapper = montarPopup({
            regiaoClicada: membroSupDirBase,
            membroRegioes,
            getFilhos: getFilhosMembro,
        })

        // Avança para sub-regiões, marca algo, confirma
        await wrapper.findComponent({ name: "AppPillToggle" }).vm.$emit("update:modelValue", "D")
        await wrapper.vm.$nextTick()

        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[0].trigger("change")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")
        await wrapper.vm.$nextTick()

        // Simula reabertura: seta aberto de volta
        await wrapper.setProps({ aberto: true })
        await wrapper.vm.$nextTick()

        // Deve estar no passo de lado novamente (AppPillToggle visível)
        expect(wrapper.findComponent({ name: "AppPillToggle" }).exists()).toBe(true)
        // Nenhum checkbox marcado (estado limpo)
        const checkboxesApos = wrapper.findAll('input[type="checkbox"]')
        checkboxesApos.forEach(c => {
            expect((c.element as HTMLInputElement).checked).toBe(false)
        })
    })

    it("CA1 — passo inicial de membro exibe os 3 botões de lado via AppPillToggle", async () => {
        const wrapper = montarPopup({
            regiaoClicada: membroSupDirBase,
            membroRegioes,
            getFilhos: getFilhosMembro,
        })

        const pillToggle = wrapper.findComponent({ name: "AppPillToggle" })
        expect(pillToggle.exists()).toBe(true)

        // Verifica que as 3 opções estão no prop
        const opcoes = pillToggle.props("opcoes") as Array<{ valor: string; label: string }>
        expect(opcoes.map(o => o.valor)).toEqual(["D", "E", "bilateral"])
        expect(opcoes.map(o => o.label)).toEqual(["Direito", "Esquerdo", "Ambos"])
    })
})

describe("RegionSelectorPopup — fluxo não-lateral (CA5)", () => {
    it("CA5 — região não-lateral: abre direto na seleção de sub-regiões, sem AppPillToggle", () => {
        const wrapper = montarPopup({
            regiaoClicada: toraxBase,
            membroRegioes: null,
            getFilhos: getFilhosTorax,
        })

        // Não deve exibir AppPillToggle no fluxo não-lateral
        expect(wrapper.findComponent({ name: "AppPillToggle" }).exists()).toBe(false)

        // Deve exibir sub-regiões diretamente
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        expect(checkboxes.length).toBeGreaterThan(0)
    })

    it("CA5 — região não-lateral: botões D/E/Bilateral aparecem para sub-regiões com lateralidade:true", async () => {
        const wrapper = montarPopup({
            regiaoClicada: toraxBase,
            membroRegioes: null,
            getFilhos: getFilhosTorax,
        })

        // Há dois itens: toraxBase (geral, lateralidade:false) e pleura (filho, lateralidade:true).
        // O primeiro checkbox é o "(geral)" do toraxBase — sem lateralidade.
        // O segundo checkbox é a Pleura — tem lateralidade:true.
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        // Marcar a Pleura (segundo checkbox, que é o filho com lateralidade:true)
        await checkboxes[1].trigger("change")
        await wrapper.vm.$nextTick()

        // Botões D, E, Bilateral devem aparecer (sub-região tem lateralidade:true)
        const botoes = wrapper.findAll("button")
        const textosBotoes = botoes.map(b => b.text().trim())
        expect(textosBotoes.some(t => t === "D")).toBe(true)
        expect(textosBotoes.some(t => t === "E")).toBe(true)
        expect(textosBotoes.some(t => t === "Bilateral")).toBe(true)
    })

    it("CA5 — região não-lateral: confirmar emite lateralidade definida por sub-região", async () => {
        const wrapper = montarPopup({
            regiaoClicada: toraxBase,
            membroRegioes: null,
            getFilhos: getFilhosTorax,
        })

        // Marcar Pleura (segundo checkbox)
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[1].trigger("change")
        await wrapper.vm.$nextTick()

        // Definir lateralidade E
        const botoes = wrapper.findAll("button")
        const botaoE = botoes.find(b => b.text().trim() === "E")
        expect(botaoE).toBeTruthy()
        await botaoE!.trigger("click")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; lateralidade: string | null }>
        expect(selecoes).toHaveLength(1)
        expect(selecoes[0].lateralidade).toBe("E")
    })
})

describe("RegionSelectorPopup — CA9/CA10 dedup e payload", () => {
    it("CA9 — sub-região já selecionada aparece como 'Selecionado' (não recriável)", async () => {
        const wrapper = mount(RegionSelectorPopup, {
            props: {
                aberto: true,
                regiaoClicada: membroSupDirBase,
                regioes: catalogoMembro,
                regioesJaSelecionadas: [ombroDireito.id],
                getFilhos: getFilhosMembro,
                membroRegioes,
            },
            global: { stubs: { AppModal: AppModalStub } },
        })

        // Avança para sub-regiões (aguarda nextTick para o template reatualizar)
        await wrapper.findComponent({ name: "AppPillToggle" }).vm.$emit("update:modelValue", "D")
        await wrapper.vm.$nextTick()

        // A região ombro-direito deve exibir "Selecionado" sem checkbox
        const html = wrapper.html()
        expect(html).toContain("Selecionado")
    })

    it("CA10 — payload tem estrutura { regiaoId, lateralidade } compatível com exameFisicoService", async () => {
        const wrapper = montarPopup({
            regiaoClicada: membroSupDirBase,
            membroRegioes,
            getFilhos: getFilhosMembro,
        })

        await wrapper.findComponent({ name: "AppPillToggle" }).vm.$emit("update:modelValue", "D")
        await wrapper.vm.$nextTick()

        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[0].trigger("change")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; lateralidade: string | null }>

        // Verificar shape do payload
        expect(selecoes).toBeDefined()
        expect(selecoes.length).toBeGreaterThan(0)
        selecoes.forEach(s => {
            expect(typeof s.regiaoId).toBe("string")
            expect(["D", "E", "bilateral", null]).toContain(s.lateralidade)
        })
    })
})
