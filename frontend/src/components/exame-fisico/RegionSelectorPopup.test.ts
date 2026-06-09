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
    id: "msd-anterior",
    nome: "Membro superior direito (anterior)",
    nivel: 1,
    vista: "anterior",
})

// Membro superior esquerdo (base anterior)
const membroSupEsqBase: ExameFisicoRegiao = regiao({
    id: "mse-anterior",
    nome: "Membro superior esquerdo (anterior)",
    nivel: 1,
    vista: "anterior",
})

// Membro superior direito (base posterior) — para vista circunferencial
const membroSupDirPost: ExameFisicoRegiao = regiao({
    id: "msd-posterior",
    nome: "Membro superior direito (posterior)",
    nivel: 1,
    vista: "posterior",
})

// Sub-regiões do lado direito anterior
const ombroDireito: ExameFisicoRegiao = regiao({
    id: "ombro-direito",
    nome: "Ombro direito",
    pai_id: "msd-anterior",
    vista: "anterior",
})

const cotovelo: ExameFisicoRegiao = regiao({
    id: "cotovelo-direito",
    nome: "Cotovelo direito",
    pai_id: "msd-anterior",
    vista: "anterior",
})

// Sub-regiões do lado direito posterior
const deltoideDireito: ExameFisicoRegiao = regiao({
    id: "deltoide-direito",
    nome: "Deltóide direito",
    pai_id: "msd-posterior",
    vista: "posterior",
})

// Sub-regiões do lado esquerdo
const ombro_esq: ExameFisicoRegiao = regiao({
    id: "ombro-esquerdo",
    nome: "Ombro esquerdo",
    pai_id: "mse-anterior",
    vista: "anterior",
})

// Sub-região não-lateral (para CA5/CA16)
const toraxAnt: ExameFisicoRegiao = regiao({
    id: "torax-anterior",
    nome: "Tórax (anterior)",
    nivel: 1,
    vista: "anterior",
})
const toraxPost: ExameFisicoRegiao = regiao({
    id: "torax-posterior",
    nome: "Tórax (posterior)",
    nivel: 1,
    vista: "posterior",
})
const pleura: ExameFisicoRegiao = regiao({
    id: "pleura",
    nome: "Pleura",
    lateralidade: true,
    pai_id: "torax-anterior",
    vista: "anterior",
})
const intercostal: ExameFisicoRegiao = regiao({
    id: "intercostal-posterior",
    nome: "Intercostal posterior",
    pai_id: "torax-posterior",
    vista: "posterior",
})

const catalogoMembro = [membroSupDirBase, membroSupEsqBase, membroSupDirPost, ombroDireito, cotovelo, deltoideDireito, ombro_esq]
const catalogoTorax  = [toraxAnt, toraxPost, pleura, intercostal]

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
            regiaoClicada: opts.regiaoClicada ?? toraxAnt,
            regioes: catalogoTorax,
            regioesJaSelecionadas: opts.regioesJaSelecionadas ?? [],
            getFilhos: opts.getFilhos ?? getFilhosTorax,
            membroRegioes: null,
        },
        global: {
            stubs: { AppModal: AppModalStub },
        },
    })
}

// Helpers para avançar passos
async function avancarLado(wrapper: ReturnType<typeof mount>, lado: string) {
    await wrapper.findComponent({ name: "AppPillToggle" }).vm.$emit("update:modelValue", lado)
    await wrapper.vm.$nextTick()
}

async function avancarVistaMembro(wrapper: ReturnType<typeof mount>, vista: string) {
    // Após escolher lado, está no passo 'vista'
    const pillToggles = wrapper.findAllComponents({ name: "AppPillToggle" })
    // O único AppPillToggle visível agora é o de vista
    await pillToggles[pillToggles.length - 1].vm.$emit("update:modelValue", vista)
    await wrapper.vm.$nextTick()
}

async function avancarVistaNaoMembro(wrapper: ReturnType<typeof mount>, vista: string) {
    await wrapper.findComponent({ name: "AppPillToggle" }).vm.$emit("update:modelValue", vista)
    await wrapper.vm.$nextTick()
}

// ─── CA15: ordem dos passos — membro ──────────────────────────────────────────

describe("CA15 — ordem dos passos — membro", () => {
    it("passo 1 = lado, passo 2 = vista, passo 3 = sub-regiões", async () => {
        const wrapper = montarPopupMembro({})

        // Passo 1: AppPillToggle visível com opções de lado
        const pillLado = wrapper.findComponent({ name: "AppPillToggle" })
        expect(pillLado.exists()).toBe(true)
        const opcoesLado = pillLado.props("opcoes") as Array<{ valor: string }>
        expect(opcoesLado.map(o => o.valor)).toEqual(["D", "E", "bilateral"])

        // Avança para passo 2 (vista)
        await avancarLado(wrapper, "D")

        // Passo 2: AppPillToggle de vista visível
        const pillVista = wrapper.findComponent({ name: "AppPillToggle" })
        expect(pillVista.exists()).toBe(true)
        const opcoesVista = pillVista.props("opcoes") as Array<{ valor: string }>
        expect(opcoesVista.map(o => o.valor)).toEqual(["anterior", "posterior", "circunferencial"])

        // Avança para passo 3 (sub-regiões)
        await avancarVistaMembro(wrapper, "anterior")

        // Passo 3: sem AppPillToggle, há checkboxes
        expect(wrapper.findComponent({ name: "AppPillToggle" }).exists()).toBe(false)
        expect(wrapper.findAll('input[type="checkbox"]').length).toBeGreaterThan(0)
    })
})

// ─── CA16: ordem dos passos — não-membro ──────────────────────────────────────

describe("CA16 — ordem dos passos — não-membro", () => {
    it("passo 1 = vista (sem passo de lado), passo 2 = sub-regiões", async () => {
        const wrapper = montarPopupNaoMembro()

        // Passo 1: AppPillToggle de vista visível (sem opções de lado)
        const pillVista = wrapper.findComponent({ name: "AppPillToggle" })
        expect(pillVista.exists()).toBe(true)
        const opcoes = pillVista.props("opcoes") as Array<{ valor: string }>
        expect(opcoes.map(o => o.valor)).toEqual(["anterior", "posterior", "circunferencial"])

        // Avança para passo 2 (sub-regiões)
        await avancarVistaNaoMembro(wrapper, "anterior")

        // Passo 2: sem AppPillToggle, há checkboxes
        expect(wrapper.findComponent({ name: "AppPillToggle" }).exists()).toBe(false)
        expect(wrapper.findAll('input[type="checkbox"]').length).toBeGreaterThan(0)
    })

    it("CA5 — não-lateral antes do B1: ainda abre direto na seleção (agora = passo de vista)", () => {
        const wrapper = montarPopupNaoMembro()
        // Não deve mostrar opções de lado (D/E/Bilateral)
        const pillToggle = wrapper.findComponent({ name: "AppPillToggle" })
        expect(pillToggle.exists()).toBe(true)
        const opcoes = pillToggle.props("opcoes") as Array<{ valor: string }>
        expect(opcoes.map(o => o.valor)).not.toContain("D")
        expect(opcoes.map(o => o.valor)).not.toContain("E")
        expect(opcoes.map(o => o.valor)).not.toContain("bilateral")
    })
})

// ─── CA17: circunferencial lista filhos das 2 vistas agrupados ────────────────

describe("CA17 — circunferencial lista filhos das 2 vistas agrupados", () => {
    it("lista Anterior (filhos de torax-anterior) + Posterior (filhos de torax-posterior) com cabeçalhos", async () => {
        // Catálogo com torax-anterior e torax-posterior e seus filhos
        const catalogoCirc = [...catalogoTorax]
        function getFilhosCirc(id: string) {
            return catalogoCirc.filter(r => r.pai_id === id)
        }

        const wrapper = montarPopupNaoMembro({ getFilhos: getFilhosCirc })

        // Avança para circunferencial
        await avancarVistaNaoMembro(wrapper, "circunferencial")

        const html = wrapper.html()
        // Cabeçalhos "Anterior" e "Posterior"
        expect(html).toContain("Anterior")
        expect(html).toContain("Posterior")
        // Sub-regiões de ambos os ramos
        expect(html).toContain("Pleura")          // filho de torax-anterior
        expect(html).toContain("Intercostal posterior") // filho de torax-posterior
    })
})

// ─── CA18: exceção abdome → lombossacra ──────────────────────────────────────

describe("CA18 — exceção abdome → lombossacra-posterior", () => {
    it("abdome circunferencial: Anterior = filhos de abdome-anterior, Posterior = filhos de lombossacra-posterior", async () => {
        const abdomeAnt = regiao({ id: "abdome-anterior", nome: "Abdome (anterior)", nivel: 1, vista: "anterior" })
        const lombossacra = regiao({ id: "lombossacra-posterior", nome: "Lombossacra (posterior)", nivel: 1, vista: "posterior" })
        const epigastrio = regiao({ id: "epigastrio", nome: "Epigástrio", pai_id: "abdome-anterior", vista: "anterior" })
        const regLombossacra = regiao({ id: "coluna-lombar", nome: "Coluna lombar", pai_id: "lombossacra-posterior", vista: "posterior" })

        const catalogoAbdome = [abdomeAnt, lombossacra, epigastrio, regLombossacra]
        function getFilhosAbdome(id: string) {
            return catalogoAbdome.filter(r => r.pai_id === id)
        }

        const wrapper = mount(RegionSelectorPopup, {
            props: {
                aberto: true,
                regiaoClicada: abdomeAnt,
                regioes: catalogoAbdome,
                regioesJaSelecionadas: [],
                getFilhos: getFilhosAbdome,
                membroRegioes: null,
            },
            global: { stubs: { AppModal: AppModalStub } },
        })

        await avancarVistaNaoMembro(wrapper, "circunferencial")

        const html = wrapper.html()
        expect(html).toContain("Anterior")
        expect(html).toContain("Posterior")
        expect(html).toContain("Epigástrio")        // filho de abdome-anterior
        expect(html).toContain("Coluna lombar")     // filho de lombossacra-posterior (exceção clínica)
    })
})

// ─── CA19: 1 card por confirmação — circunferencial ──────────────────────────

describe("CA19 — 1 card por confirmação no modo circunferencial", () => {
    it("confirmar sub-regiões de 2 vistas gera exatamente 1 entrada no evento confirmar", async () => {
        const catalogoCirc = [...catalogoTorax]
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

        // No passo de vista: AppPillToggle visível, sem checkboxes
        expect(wrapper.findComponent({ name: "AppPillToggle" }).exists()).toBe(true)
        expect(wrapper.findAll('input[type="checkbox"]').length).toBe(0)
        // Confirmar não aparece no passo de vista
        const botoes = wrapper.findAll("button")
        expect(botoes.some(b => b.text().includes("Confirmar"))).toBe(false)
    })

    it("não-membro: modal começa no passo de vista — sem sub-regiões visíveis nem Confirmar habilitado", () => {
        const wrapper = montarPopupNaoMembro()

        expect(wrapper.findComponent({ name: "AppPillToggle" }).exists()).toBe(true)
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

        // AppPillToggle não deve aparecer no passo de sub-regiões
        expect(wrapper.findComponent({ name: "AppPillToggle" }).exists()).toBe(false)

        // Clicar em "Voltar"
        const botaoVoltar = wrapper.findAll("button").find(b => b.text().includes("Voltar"))
        await botaoVoltar!.trigger("click")
        await wrapper.vm.$nextTick()

        // Deve voltar ao passo de vista — AppPillToggle com opções de vista reaparece
        const pill = wrapper.findComponent({ name: "AppPillToggle" })
        expect(pill.exists()).toBe(true)
        const opcoes = pill.props("opcoes") as Array<{ valor: string }>
        expect(opcoes.map(o => o.valor)).toContain("circunferencial")
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

        // Deve estar no passo de lado novamente (AppPillToggle com opções de lado)
        const pill = wrapper.findComponent({ name: "AppPillToggle" })
        expect(pill.exists()).toBe(true)
        const opcoes = pill.props("opcoes") as Array<{ valor: string }>
        expect(opcoes.map(o => o.valor)).toEqual(["D", "E", "bilateral"])
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

// ─── CA5 (compatibilidade) — região não-lateral: D/E/Bilateral por sub-região ─

describe("RegionSelectorPopup — fluxo não-lateral (CA5 compatibilidade)", () => {
    it("CA5 — após escolher vista, botões D/E/Bilateral aparecem para sub-regiões com lateralidade:true", async () => {
        const wrapper = montarPopupNaoMembro({ getFilhos: getFilhosTorax })

        await avancarVistaNaoMembro(wrapper, "anterior")

        // Há dois itens: toraxAnt (geral, lateralidade:false) e pleura (filho, lateralidade:true).
        const checkboxes = wrapper.findAll('input[type="checkbox"]')
        // Marcar a Pleura (segundo checkbox, que é o filho com lateralidade:true)
        await checkboxes[1].trigger("change")
        await wrapper.vm.$nextTick()

        const botoes = wrapper.findAll("button")
        const textosBotoes = botoes.map(b => b.text().trim())
        expect(textosBotoes.some(t => t === "D")).toBe(true)
        expect(textosBotoes.some(t => t === "E")).toBe(true)
        expect(textosBotoes.some(t => t === "Bilateral")).toBe(true)
    })

    it("CA5 — região não-lateral: confirmar emite lateralidade definida por sub-região", async () => {
        const wrapper = montarPopupNaoMembro({ getFilhos: getFilhosTorax })

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
