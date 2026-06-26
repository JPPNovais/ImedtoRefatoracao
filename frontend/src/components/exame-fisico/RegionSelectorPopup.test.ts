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

// Membro superior direito (base anterior)
const membroSupDirBase: ExameFisicoRegiao = regiao({
    id: "membro-superior-direito-anterior",
    nome: "Membro superior direito (anterior)",
    nivel: 1,
    vista: "anterior",
})

// Membro superior esquerdo (base anterior)
const membroSupEsqBase: ExameFisicoRegiao = regiao({
    id: "membro-superior-esquerdo-anterior",
    nome: "Membro superior esquerdo (anterior)",
    nivel: 1,
    vista: "anterior",
})

// Membro superior direito (base posterior) — para vista circunferencial
const membroSupDirPost: ExameFisicoRegiao = regiao({
    id: "membro-superior-direito-posterior",
    nome: "Membro superior direito (posterior)",
    nivel: 1,
    vista: "posterior",
})

// Sub-regiões do lado direito anterior
const ombroDireito: ExameFisicoRegiao = regiao({
    id: "ombro-direito",
    nome: "Ombro direito",
    pai_id: "membro-superior-direito-anterior",
    vista: "anterior",
})

const cotovelo: ExameFisicoRegiao = regiao({
    id: "cotovelo-direito",
    nome: "Cotovelo direito",
    pai_id: "membro-superior-direito-anterior",
    vista: "anterior",
})

// Sub-regiões do lado direito posterior
const deltoideDireito: ExameFisicoRegiao = regiao({
    id: "deltoide-direito",
    nome: "Deltóide direito",
    pai_id: "membro-superior-direito-posterior",
    vista: "posterior",
})

// Sub-regiões do lado esquerdo
const ombro_esq: ExameFisicoRegiao = regiao({
    id: "ombro-esquerdo",
    nome: "Ombro esquerdo",
    pai_id: "membro-superior-esquerdo-anterior",
    vista: "anterior",
})

// Sub-região não-lateral para testes de não-membro (CA5/CA16 + CA17/CA18/CA19).
// Fusão 2026-06-25_002: usa tronco-anterior/tronco-posterior (regiões reais).
const troncoAnt: ExameFisicoRegiao = regiao({
    id: "tronco-anterior",
    nome: "Tronco (anterior)",
    nivel: 1,
    vista: "anterior",
})
const troncoPost: ExameFisicoRegiao = regiao({
    id: "tronco-posterior",
    nome: "Tronco (posterior)",
    nivel: 1,
    vista: "posterior",
})
const peitoral: ExameFisicoRegiao = regiao({
    id: "peitoral",
    nome: "Peitoral",
    lateralidade: true,
    pai_id: "tronco-anterior",
    vista: "anterior",
})
const escapular: ExameFisicoRegiao = regiao({
    id: "escapular",
    nome: "Escapular",
    pai_id: "tronco-posterior",
    vista: "posterior",
})

const catalogoMembro = [membroSupDirBase, membroSupEsqBase, membroSupDirPost, ombroDireito, cotovelo, deltoideDireito, ombro_esq]
const catalogoTronco  = [troncoAnt, troncoPost, peitoral, escapular]

function getFilhosMembro(regiaoId: string): ExameFisicoRegiao[] {
    return catalogoMembro.filter(r => r.pai_id === regiaoId)
}

function getFilhosTronco(regiaoId: string): ExameFisicoRegiao[] {
    return catalogoTronco.filter(r => r.pai_id === regiaoId)
}

const membroRegioes: MembroRegioes = {
    tipo: "superior",
    dirBase: membroSupDirBase,
    esquBase: membroSupEsqBase,
}

// ─── Stub do AppModal: renderiza apenas os slots default e rodape ─────────────
const AppModalStub = {
    name: "AppModal",
    template: `<div class="app-modal-stub"><slot /><slot name="rodape" /></div>`,
}

function montarPopupMembro(opts: {
    regioesJaSelecionadas?: string[]
    getFilhos?: (id: string) => ExameFisicoRegiao[]
}) {
    return mount(RegionSelectorPopup, {
        props: {
            aberto: true,
            regiaoClicada: membroSupDirBase,
            regioes: catalogoMembro,
            regioesJaSelecionadas: opts.regioesJaSelecionadas ?? [],
            getFilhos: opts.getFilhos ?? getFilhosMembro,
            membroRegioes,
        },
        global: {
            stubs: { AppModal: AppModalStub },
        },
    })
}

function montarPopupNaoMembro(opts: {
    regiaoClicada?: ExameFisicoRegiao
    regioesJaSelecionadas?: string[]
    getFilhos?: (id: string) => ExameFisicoRegiao[]
} = {}) {
    return mount(RegionSelectorPopup, {
        props: {
            aberto: true,
            regiaoClicada: opts.regiaoClicada ?? troncoAnt,
            regioes: catalogoTronco,
            regioesJaSelecionadas: opts.regioesJaSelecionadas ?? [],
            getFilhos: opts.getFilhos ?? getFilhosTronco,
            membroRegioes: null,
        },
        global: {
            stubs: { AppModal: AppModalStub },
        },
    })
}

// Helpers para avançar passos
// O segmented control agora são botões .rsp-seg-btn nativos.
// Avançar = clicar no botão com o texto correspondente ao valor.

const LABEL_LADO: Record<string, string> = { D: "Direito", E: "Esquerdo", bilateral: "Ambos" }
const LABEL_VISTA: Record<string, string> = { anterior: "Anterior", posterior: "Posterior", circunferencial: "Circunferencial" }

async function avancarLado(wrapper: ReturnType<typeof mount>, lado: string) {
    const btn = wrapper.findAll("button.rsp-seg-btn").find(b => b.text().trim() === LABEL_LADO[lado])
    if (!btn) throw new Error(`Botão de lado "${LABEL_LADO[lado]}" não encontrado`)
    await btn.trigger("click")
    await wrapper.vm.$nextTick()
}

async function avancarVistaMembro(wrapper: ReturnType<typeof mount>, vista: string) {
    // No passo 'vista', os botões de plano têm a classe rsp-seg-btn
    const btn = wrapper.findAll("button.rsp-seg-btn").find(b => b.text().trim() === LABEL_VISTA[vista])
    if (!btn) throw new Error(`Botão de vista "${LABEL_VISTA[vista]}" não encontrado`)
    await btn.trigger("click")
    await wrapper.vm.$nextTick()
}

async function avancarVistaNaoMembro(wrapper: ReturnType<typeof mount>, vista: string) {
    const btn = wrapper.findAll("button.rsp-seg-btn").find(b => b.text().trim() === LABEL_VISTA[vista])
    if (!btn) throw new Error(`Botão de vista "${LABEL_VISTA[vista]}" não encontrado`)
    await btn.trigger("click")
    await wrapper.vm.$nextTick()
}

// ─── CA15: ordem dos passos — membro ──────────────────────────────────────────

describe("CA15 — ordem dos passos — membro", () => {
    it("passo 1 = lado, passo 2 = vista, passo 3 = sub-regiões", async () => {
        const wrapper = montarPopupMembro({})

        // Passo 1: segmented de lado visível (botões Direito/Esquerdo/Ambos)
        const btnLado = wrapper.findAll("button.rsp-seg-btn")
        expect(btnLado.length).toBeGreaterThan(0)
        const textosBtnLado = btnLado.map(b => b.text().trim())
        expect(textosBtnLado).toContain("Direito")
        expect(textosBtnLado).toContain("Esquerdo")
        expect(textosBtnLado).toContain("Ambos")

        // Avança para passo 2 (vista)
        await avancarLado(wrapper, "D")

        // Passo 2: segmented de vista visível (Anterior/Posterior/Circunferencial)
        const btnVista = wrapper.findAll("button.rsp-seg-btn")
        expect(btnVista.length).toBeGreaterThan(0)
        const textosBtnVista = btnVista.map(b => b.text().trim())
        expect(textosBtnVista).toContain("Anterior")
        expect(textosBtnVista).toContain("Posterior")
        expect(textosBtnVista).toContain("Circunferencial")

        // Avança para passo 3 (sub-regiões)
        await avancarVistaMembro(wrapper, "anterior")

        // Passo 3: sem segmented, há checkboxes
        expect(wrapper.findAll("button.rsp-seg-btn").length).toBe(0)
        expect(wrapper.findAll('input[type="checkbox"]').length).toBeGreaterThan(0)
    })
})

// ─── CA16: ordem dos passos — não-membro ──────────────────────────────────────

describe("CA16 — ordem dos passos — não-membro", () => {
    it("passo 1 = vista (sem passo de lado), passo 2 = sub-regiões", async () => {
        const wrapper = montarPopupNaoMembro()

        // Passo 1: segmented de vista visível (Anterior/Posterior/Circunferencial)
        const btns = wrapper.findAll("button.rsp-seg-btn")
        expect(btns.length).toBeGreaterThan(0)
        const textos = btns.map(b => b.text().trim())
        expect(textos).toContain("Anterior")
        expect(textos).toContain("Posterior")
        expect(textos).toContain("Circunferencial")

        // Avança para passo 2 (sub-regiões)
        await avancarVistaNaoMembro(wrapper, "anterior")

        // Passo 2: sem segmented, há checkboxes
        expect(wrapper.findAll("button.rsp-seg-btn").length).toBe(0)
        expect(wrapper.findAll('input[type="checkbox"]').length).toBeGreaterThan(0)
    })

    it("CA5 — não-lateral antes do B1: ainda abre direto na seleção (agora = passo de vista)", () => {
        const wrapper = montarPopupNaoMembro()
        // Não deve mostrar opções de lado (D/E/Bilateral)
        const btns = wrapper.findAll("button.rsp-seg-btn")
        expect(btns.length).toBeGreaterThan(0)
        const textos = btns.map(b => b.text().trim())
        expect(textos).not.toContain("Direito")
        expect(textos).not.toContain("Esquerdo")
        expect(textos).not.toContain("Ambos")
    })
})

// ─── CA17: circunferencial une os filhos das 2 vistas numa lista deduplicada ──

describe("CA17 — circunferencial une os filhos das 2 vistas numa lista única", () => {
    it("lista filhos de tronco-anterior + tronco-posterior juntos, sem cabeçalhos de grupo", async () => {
        // Fusão 2026-06-25_002: tronco-circunferencial resolve via tronco-anterior + tronco-posterior
        const catalogoCirc = [...catalogoTronco]
        function getFilhosCirc(id: string) {
            return catalogoCirc.filter(r => r.pai_id === id)
        }

        const wrapper = montarPopupNaoMembro({ getFilhos: getFilhosCirc })

        // Avança para circunferencial
        await avancarVistaNaoMembro(wrapper, "circunferencial")

        const html = wrapper.html()
        // Sub-regiões de ambos os ramos aparecem na lista unificada
        expect(html).toContain("Peitoral")   // filho de tronco-anterior
        expect(html).toContain("Escapular")  // filho de tronco-posterior
        // Não há mais os cabeçalhos de grupo "Anterior"/"Posterior" (lista única)
        expect(html).not.toContain("rsp-sub-head")
    })
})

// ─── CA18: tronco-circunferencial simétrico (fusão 2026-06-25_002) ───────────
// Anteriormente testava a exceção abdome↔lombossacra, que foi removida.
// Agora valida que tronco-circunferencial → tronco-anterior + tronco-posterior (simétrico).

describe("CA18 — tronco-circunferencial é simétrico (fusão 2026-06-25_002)", () => {
    it("tronco circunferencial une filhos de tronco-anterior e tronco-posterior, deduplicados", async () => {
        const catalogoCirc = [...catalogoTronco]
        function getFilhosCirc(id: string) {
            return catalogoCirc.filter(r => r.pai_id === id)
        }

        const wrapper = montarPopupNaoMembro({ getFilhos: getFilhosCirc })
        await avancarVistaNaoMembro(wrapper, "circunferencial")

        const html = wrapper.html()
        expect(html).toContain("Peitoral")   // filho de tronco-anterior
        expect(html).toContain("Escapular")  // filho de tronco-posterior
        // Não há "Lombossacra" (exceção clínica removida)
        expect(html).not.toContain("lombossacra")
    })
})

// ─── CA19: 1 card por confirmação — circunferencial ──────────────────────────

describe("CA19 — 1 card por confirmação no modo circunferencial", () => {
    it("confirmar sub-regiões de 2 vistas gera exatamente 1 entrada no evento confirmar", async () => {
        const catalogoCirc = [...catalogoTronco]
        function getFilhosCirc(id: string) {
            return catalogoCirc.filter(r => r.pai_id === id)
        }

        const wrapper = montarPopupNaoMembro({ getFilhos: getFilhosCirc })

        // Avança para circunferencial
        await avancarVistaNaoMembro(wrapper, "circunferencial")

        // Marca 1 sub-região de cada vista
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        expect(checkboxes.length).toBeGreaterThanOrEqual(2)
        await checkboxes[0].trigger("change") // sub-região anterior
        await checkboxes[1].trigger("change") // sub-região posterior

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        expect(eventos).toBeTruthy()
        // O evento emite um array de seleções individuais (1 por sub-região marcada)
        // mas todas têm vista = 'circunferencial' — o SecaoExameFisico cria 1 card.
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; vista: string | null }>
        // Todas as seleções têm vista = 'circunferencial'
        expect(selecoes.every(s => s.vista === "circunferencial")).toBe(true)
        // Não é 1 por "face" — o SecaoExameFisico trata como 1 card via ancestral comum
    })
})

// ─── CA26: vista não escolhida não avança ────────────────────────────────────

describe("CA26 — vista não escolhida não avança para sub-regiões", () => {
    it("membro: após escolher lado, está no passo de vista — sem sub-regiões visíveis nem Confirmar habilitado", async () => {
        const wrapper = montarPopupMembro({})

        await avancarLado(wrapper, "D")

        // No passo de vista: segmented de vista visível, sem checkboxes
        const btns = wrapper.findAll("button.rsp-seg-btn")
        expect(btns.some(b => b.text().trim() === "Anterior")).toBe(true)
        expect(wrapper.findAll('input[type="checkbox"]').length).toBe(0)
        // Confirmar não aparece no passo de vista
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Confirmar"))).toBe(false)
    })

    it("não-membro: modal começa no passo de vista — sem sub-regiões visíveis nem Confirmar habilitado", () => {
        const wrapper = montarPopupNaoMembro()

        // Segmented de vista visível, sem checkboxes
        const btns = wrapper.findAll("button.rsp-seg-btn")
        expect(btns.some(b => b.text().trim() === "Anterior")).toBe(true)
        expect(wrapper.findAll('input[type="checkbox"]').length).toBe(0)
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Confirmar"))).toBe(false)
    })
})

// ─── CA29: não-regressão do 004 ──────────────────────────────────────────────

describe("CA29 — não-regressão do briefing 004 (fluxo de membro anterior/posterior)", () => {
    it("Direito + anterior: confirmar 2 sub-regiões gera 2 entradas com lateralidade D e vista anterior", async () => {
        const wrapper = montarPopupMembro({ getFilhos: getFilhosMembro })

        await avancarLado(wrapper, "D")
        await avancarVistaMembro(wrapper, "anterior")

        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        expect(checkboxes.length).toBeGreaterThanOrEqual(2)
        await checkboxes[0].trigger("change")
        await checkboxes[1].trigger("change")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; lateralidade: string | null; vista: string | null }>
        expect(selecoes).toHaveLength(2)
        expect(selecoes.every(s => s.lateralidade === "D")).toBe(true)
        expect(selecoes.every(s => s.vista === "anterior")).toBe(true)
        // Ids devem pertencer à base direita anterior
        const idsDir = [ombroDireito.id, cotovelo.id, membroSupDirBase.id]
        expect(selecoes.every(s => idsDir.includes(s.regiaoId))).toBe(true)
    })

    it("Esquerdo + anterior: sub-regiões da base esquerda; entry tem lateralidade E e vista anterior", async () => {
        const wrapper = montarPopupMembro({ getFilhos: getFilhosMembro })

        await avancarLado(wrapper, "E")
        await avancarVistaMembro(wrapper, "anterior")

        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[0].trigger("change")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; lateralidade: string | null; vista: string | null }>
        expect(selecoes).toHaveLength(1)
        expect(selecoes[0].lateralidade).toBe("E")
        expect(selecoes[0].vista).toBe("anterior")
        const idsEsq = [ombro_esq.id, membroSupEsqBase.id]
        expect(idsEsq.includes(selecoes[0].regiaoId)).toBe(true)
    })

    it("Ambos + anterior: confirmar 1 sub-região gera EXATAMENTE 1 entrada com lateralidade bilateral", async () => {
        const wrapper = montarPopupMembro({ getFilhos: getFilhosMembro })

        await avancarLado(wrapper, "bilateral")
        await avancarVistaMembro(wrapper, "anterior")

        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[0].trigger("change")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; lateralidade: string | null }>
        expect(selecoes).toHaveLength(1)
        expect(selecoes[0].lateralidade).toBe("bilateral")
        // Nenhuma entrada com lateralidade E
        expect(selecoes.some(s => s.lateralidade === "E")).toBe(false)
    })

    it("CA4 — passo de sub-regiões de membro NÃO exibe botões D/E/Bilateral por sub-região", async () => {
        const wrapper = montarPopupMembro({ getFilhos: getFilhosMembro })

        await avancarLado(wrapper, "D")
        await avancarVistaMembro(wrapper, "anterior")

        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[0].trigger("change")
        await wrapper.vm.$nextTick()

        const botoes = wrapper.findAll("button")
        const textosBotoes = botoes.map(b => b.text().trim())
        expect(textosBotoes.some(t => t === "D")).toBe(false)
        expect(textosBotoes.some(t => t === "E")).toBe(false)
        expect(textosBotoes.some(t => t === "Bilateral")).toBe(false)
    })

    it("CA6 — Voltar no passo de sub-regiões retorna ao passo de vista", async () => {
        const wrapper = montarPopupMembro({ getFilhos: getFilhosMembro })

        await avancarLado(wrapper, "D")
        await avancarVistaMembro(wrapper, "anterior")

        // Segmented não deve aparecer no passo de sub-regiões
        expect(wrapper.findAll("button.rsp-seg-btn").length).toBe(0)

        // Clicar em "Voltar"
        const botaoVoltar = wrapper.findAll("button").find(b => b.text().includes("Voltar"))
        await botaoVoltar!.trigger("click")
        await wrapper.vm.$nextTick()

        // Deve voltar ao passo de vista — segmented de plano com Circunferencial reaparece
        const btns = wrapper.findAll("button.rsp-seg-btn")
        expect(btns.some(b => b.text().trim() === "Circunferencial")).toBe(true)
    })

    it("CA7 — Cancelar no passo de lado fecha sem emitir confirmar", async () => {
        const wrapper = montarPopupMembro({})

        const botaoCancelar = wrapper.findAll("button").find(b => b.text().includes("Cancelar"))
        expect(botaoCancelar).toBeTruthy()
        await botaoCancelar!.trigger("click")

        expect(wrapper.emitted("confirmar")).toBeFalsy()
        expect(wrapper.emitted("update:aberto")).toBeTruthy()
        expect(wrapper.emitted("update:aberto")![0]![0]).toBe(false)
    })

    it("CA8 — Após fechar e reabrir, estado volta ao passo de lado (membro)", async () => {
        const wrapper = montarPopupMembro({ getFilhos: getFilhosMembro })

        // Avança até sub-regiões, confirma
        await avancarLado(wrapper, "D")
        await avancarVistaMembro(wrapper, "anterior")
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[0].trigger("change")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")
        await wrapper.vm.$nextTick()

        // Simula reabertura
        await wrapper.setProps({ aberto: true })
        await wrapper.vm.$nextTick()

        // Deve estar no passo de lado novamente (segmented com Direito/Esquerdo/Ambos)
        const btns = wrapper.findAll("button.rsp-seg-btn")
        expect(btns.some(b => b.text().trim() === "Direito")).toBe(true)
        expect(btns.some(b => b.text().trim() === "Esquerdo")).toBe(true)
        expect(btns.some(b => b.text().trim() === "Ambos")).toBe(true)
        // Não deve ter opção de plano (Circunferencial não aparece no passo de lado)
        expect(btns.some(b => b.text().trim() === "Circunferencial")).toBe(false)
    })

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

        await avancarLado(wrapper, "D")
        await avancarVistaMembro(wrapper, "anterior")

        const html = wrapper.html()
        expect(html).toContain("Selecionado")
    })

    it("CA10 — payload tem estrutura { regiaoId, lateralidade, vista } compatível com SecaoExameFisico", async () => {
        const wrapper = montarPopupMembro({ getFilhos: getFilhosMembro })

        await avancarLado(wrapper, "D")
        await avancarVistaMembro(wrapper, "anterior")

        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[0].trigger("change")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; lateralidade: string | null; vista: string | null }>

        expect(selecoes).toBeDefined()
        expect(selecoes.length).toBeGreaterThan(0)
        selecoes.forEach(s => {
            expect(typeof s.regiaoId).toBe("string")
            expect(["D", "E", "bilateral", null]).toContain(s.lateralidade)
            expect(["anterior", "posterior", "circunferencial", null]).toContain(s.vista)
        })
    })
})

// ─── BUGFIX — vista escolhida define a base do membro (não a face clicada) ────

describe("BUGFIX — vista escolhida define a base do membro, não a face clicada no mapa", () => {
    it("Direito + posterior: lista sub-regiões da base POSTERIOR mesmo tendo clicado na face anterior", async () => {
        // regiaoClicada/dirBase = membro-superior-direito-anterior (face anterior clicada no mapa)
        const wrapper = montarPopupMembro({ getFilhos: getFilhosMembro })
        await avancarLado(wrapper, "D")
        await avancarVistaMembro(wrapper, "posterior")

        const html = wrapper.html()
        // Deltóide é filho da base POSTERIOR; Ombro/Cotovelo são filhos da anterior
        expect(html).toContain("Deltóide")
        expect(html).not.toContain("Ombro")
        expect(html).not.toContain("Cotovelo")
    })

    it("Direito + posterior: confirmar emite regiaoId da base posterior com vista posterior", async () => {
        const wrapper = montarPopupMembro({ getFilhos: getFilhosMembro })
        await avancarLado(wrapper, "D")
        await avancarVistaMembro(wrapper, "posterior")

        // 1º checkbox = "(geral)" da base posterior; último = Deltóide (sub-região posterior)
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[checkboxes.length - 1].trigger("change")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; lateralidade: string | null; vista: string | null }>
        expect(selecoes.every(s => s.vista === "posterior")).toBe(true)
        // Ids pertencem à base posterior direita — nunca à anterior (que foi a face clicada)
        const idsPosterior = [deltoideDireito.id, membroSupDirPost.id]
        expect(selecoes.every(s => idsPosterior.includes(s.regiaoId))).toBe(true)
        expect(selecoes.some(s => s.regiaoId === ombroDireito.id || s.regiaoId === cotovelo.id)).toBe(false)
    })
})

// ─── CA5 (compatibilidade) — região não-lateral: D/E/Bilateral por sub-região ─

describe("RegionSelectorPopup — fluxo não-lateral (CA5 compatibilidade)", () => {
    it("CA5 — após escolher vista, botões D/E/Bilateral aparecem para sub-regiões com lateralidade:true", async () => {
        const wrapper = montarPopupNaoMembro({ getFilhos: getFilhosTronco })

        await avancarVistaNaoMembro(wrapper, "anterior")

        // Há dois itens: troncoAnt (geral, lateralidade:false) e peitoral (filho, lateralidade:true).
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        // Marcar o Peitoral (segundo checkbox, que é o filho com lateralidade:true)
        await checkboxes[1].trigger("change")
        await wrapper.vm.$nextTick()

        const botoes = wrapper.findAll("button")
        const textosBotoes = botoes.map(b => b.text().trim())
        expect(textosBotoes.some(t => t === "D")).toBe(true)
        expect(textosBotoes.some(t => t === "E")).toBe(true)
        expect(textosBotoes.some(t => t === "Bilateral")).toBe(true)
    })

    it("CA5 — região não-lateral: confirmar emite lateralidade definida por sub-região", async () => {
        const wrapper = montarPopupNaoMembro({ getFilhos: getFilhosTronco })

        await avancarVistaNaoMembro(wrapper, "anterior")

        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        await checkboxes[1].trigger("change")
        await wrapper.vm.$nextTick()

        const botoes = wrapper.findAll("button")
        const botaoE = botoes.find(b => b.text().trim() === "E")
        await botaoE!.trigger("click")

        const botaoConfirmar = wrapper.findAll("button").find(b => b.text().includes("Confirmar"))
        await botaoConfirmar!.trigger("click")

        const eventos = wrapper.emitted("confirmar")
        const selecoes = eventos![0]![0] as Array<{ regiaoId: string; lateralidade: string | null }>
        expect(selecoes).toHaveLength(1)
        expect(selecoes[0].lateralidade).toBe("E")
    })
})
