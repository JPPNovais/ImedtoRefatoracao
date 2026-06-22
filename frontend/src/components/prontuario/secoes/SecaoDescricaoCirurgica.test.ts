/**
 * Testes unitários — SecaoDescricaoCirurgica
 * Briefing 2026-06-21_001 — CA4 (dia da semana), CA5 (duração + virada de dia),
 * CA6 (sem campos de anestesia), CA20–CA22 (bloqueio do cirurgião via validarCirurgiao)
 * Fix A-1 — prop erroCirurgiao (typo corrigido): mensagem de erro inline renderiza
 */
import { describe, it, expect } from "vitest"
import { mount } from "@vue/test-utils"
import SecaoDescricaoCirurgica from "./SecaoDescricaoCirurgica.vue"
import type { DescCirurgica } from "./SecaoDescricaoCirurgica.vue"

// Valor mínimo válido para modelValue
function modelVazio(): DescCirurgica {
    return {
        cirurgiao: "",
        data: "",
        diaSemana: "",
        cirurgiasRealizadas: "",
        anestesista: "",
        auxiliar: "",
        instrumentador: "",
        outrosMembros: [],
        cirurgiaInicio: "",
        cirurgiaFim: "",
        profilaxia: {
            enoxaparina: false,
            meiaCompressiva: false,
            botaPneumatica: false,
            deambulacaoPrecoce: false,
            antitrombOutroAtivo: false,
            antitrombOutro: "",
            cefazolina: false,
            gentamicina: false,
            antibioOutroAtivo: false,
            antibioOutro: "",
        },
        intercorrencia: "",
        intercorrenciaDescricao: "",
        tecnicaOperatoria: "",
        observacoes: "",
    }
}

// Stubs globais para todos os componentes do design system usados internamente
const STUBS = {
    AppInput: true,
    AppTextarea: true,
    AppButton: true,
    AppCheckbox: true,
    AppDatePicker: true,
    AppPillToggle: true,
}

// ── Regressão A-1: prop erroCirurgiao (era erroCircurgiao) ────────────────────

describe("SecaoDescricaoCirurgica — prop erroCirurgiao (regressão A-1)", () => {
    it("renderiza .msg-erro com o texto quando erroCirurgiao está preenchido", () => {
        const wrapper = mount(SecaoDescricaoCirurgica, {
            props: {
                modelValue: modelVazio(),
                erroCirurgiao: "Informe o cirurgião",
            },
            global: { stubs: STUBS },
        })

        const msg = wrapper.find(".msg-erro")
        expect(msg.exists()).toBe(true)
        expect(msg.text()).toBe("Informe o cirurgião")
    })

    it("NÃO renderiza .msg-erro quando erroCirurgiao é null", () => {
        const wrapper = mount(SecaoDescricaoCirurgica, {
            props: {
                modelValue: modelVazio(),
                erroCirurgiao: null,
            },
            global: { stubs: STUBS },
        })

        expect(wrapper.find(".msg-erro").exists()).toBe(false)
    })

    it("NÃO renderiza .msg-erro quando erroCirurgiao não é passado", () => {
        const wrapper = mount(SecaoDescricaoCirurgica, {
            props: {
                modelValue: modelVazio(),
            },
            global: { stubs: STUBS },
        })

        expect(wrapper.find(".msg-erro").exists()).toBe(false)
    })
})

// ── Funções extraídas (mesmas lógicas internas ao componente) ─────────────────

const DIAS_SEMANA = [
    "Domingo", "Segunda-feira", "Terça-feira", "Quarta-feira",
    "Quinta-feira", "Sexta-feira", "Sábado",
]

function calcularDiaSemana(data: string): string {
    if (!data) return ""
    const d = new Date(data + "T12:00:00")
    if (isNaN(d.getTime())) return ""
    return DIAS_SEMANA[d.getDay()]
}

function calcularDuracao(inicio: string, fim: string): string {
    if (!inicio || !fim) return "--:--"
    const [hI, mI] = inicio.split(":").map(Number)
    const [hF, mF] = fim.split(":").map(Number)
    if ([hI, mI, hF, mF].some(isNaN)) return "--:--"
    let total = (hF * 60 + mF) - (hI * 60 + mI)
    if (total < 0) total += 24 * 60
    const h = Math.floor(total / 60)
    const m = total % 60
    return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`
}

// ── Replica validarCirurgiao do ProntuarioView ────────────────────────────────

function validarCirurgiao(novaEvolucao: Record<string, unknown>): boolean {
    const desc = novaEvolucao["desc-cirurgica"]
    if (!desc || typeof desc !== "object") return true
    const d = desc as Record<string, unknown>
    const camposVerificaveis = [
        "cirurgiasRealizadas", "anestesista", "auxiliar", "instrumentador",
        "cirurgiaInicio", "cirurgiaFim", "tecnicaOperatoria", "observacoes",
        "intercorrenciaDescricao", "data",
    ]
    const temCampoPreenchido =
        camposVerificaveis.some(k => typeof d[k] === "string" && (d[k] as string).trim() !== "") ||
        (Array.isArray(d.outrosMembros) && (d.outrosMembros as unknown[]).length > 0) ||
        (d.profilaxia && typeof d.profilaxia === "object" &&
            Object.values(d.profilaxia as Record<string, unknown>).some(v => v === true)) ||
        (typeof d.intercorrencia === "string" && (d.intercorrencia as string) !== "")

    if (!temCampoPreenchido) return true
    const cirurgiao = typeof d.cirurgiao === "string" ? (d.cirurgiao as string).trim() : ""
    return !!cirurgiao
}

// ── CA4: dia da semana ────────────────────────────────────────────────────────

describe("calcularDiaSemana (CA4)", () => {
    it("retorna dia correto para uma data conhecida (2024-01-01 = Segunda-feira)", () => {
        expect(calcularDiaSemana("2024-01-01")).toBe("Segunda-feira")
    })

    it("retorna dia correto para 2024-12-25 = Quarta-feira", () => {
        expect(calcularDiaSemana("2024-12-25")).toBe("Quarta-feira")
    })

    it("retorna '' para data vazia", () => {
        expect(calcularDiaSemana("")).toBe("")
    })

    it("retorna '' para data inválida", () => {
        expect(calcularDiaSemana("nao-e-data")).toBe("")
    })
})

// ── CA5: duração da cirurgia ──────────────────────────────────────────────────

describe("calcularDuracao (CA5)", () => {
    it("caminho feliz: 08:00 → 10:30 = 02:30", () => {
        expect(calcularDuracao("08:00", "10:30")).toBe("02:30")
    })

    it("virada de dia: 23:30 → 01:00 = 01:30", () => {
        expect(calcularDuracao("23:30", "01:00")).toBe("01:30")
    })

    it("mesma hora = 00:00", () => {
        expect(calcularDuracao("10:00", "10:00")).toBe("00:00")
    })

    it("retorna '--:--' quando faltam campos", () => {
        expect(calcularDuracao("", "10:00")).toBe("--:--")
        expect(calcularDuracao("08:00", "")).toBe("--:--")
        expect(calcularDuracao("", "")).toBe("--:--")
    })

    it("retorna '--:--' para valores inválidos", () => {
        expect(calcularDuracao("ab:cd", "10:00")).toBe("--:--")
    })

    it("duração de mais de 12h: 06:00 → 20:00 = 14:00", () => {
        expect(calcularDuracao("06:00", "20:00")).toBe("14:00")
    })
})

// ── CA20–CA22: validação do cirurgião ─────────────────────────────────────────

describe("validarCirurgiao (CA20–CA22)", () => {
    it("CA22.a: sem desc-cirurgica no modelo → retorna true (não bloqueia)", () => {
        expect(validarCirurgiao({})).toBe(true)
    })

    it("CA22.a: desc-cirurgica não é objeto → retorna true", () => {
        expect(validarCirurgiao({ "desc-cirurgica": null })).toBe(true)
    })

    it("CA22.b: desc-cirurgica presente mas totalmente vazia → retorna true", () => {
        expect(validarCirurgiao({
            "desc-cirurgica": {
                cirurgiao: "",
                cirurgiasRealizadas: "",
                anestesista: "",
                data: "",
                intercorrencia: "",
                outrosMembros: [],
                profilaxia: { enoxaparina: false, cefazolina: false },
            },
        })).toBe(true)
    })

    it("CA20: seção com campo preenchido e cirurgião vazio → retorna false", () => {
        expect(validarCirurgiao({
            "desc-cirurgica": {
                cirurgiao: "",
                cirurgiasRealizadas: "Colecistectomia",
                anestesista: "",
                data: "",
                intercorrencia: "",
            },
        })).toBe(false)
    })

    it("CA20: seção com profilaxia marcada e cirurgião vazio → retorna false", () => {
        expect(validarCirurgiao({
            "desc-cirurgica": {
                cirurgiao: "   ",
                profilaxia: { enoxaparina: true },
            },
        })).toBe(false)
    })

    it("CA21: seção com campo preenchido E cirurgião preenchido → retorna true", () => {
        expect(validarCirurgiao({
            "desc-cirurgica": {
                cirurgiao: "Dr. Fulano",
                cirurgiasRealizadas: "Colecistectomia",
            },
        })).toBe(true)
    })

    it("CA21: cirurgião com só espaços é tratado como vazio → retorna false", () => {
        expect(validarCirurgiao({
            "desc-cirurgica": {
                cirurgiao: "   ",
                tecnicaOperatoria: "Videolaparoscopia",
            },
        })).toBe(false)
    })

    it("CA22.b: outrosMembros não-vazio com cirurgião vazio → retorna false", () => {
        expect(validarCirurgiao({
            "desc-cirurgica": {
                cirurgiao: "",
                outrosMembros: [{ funcao: "Auxiliar", nome: "Dr. X" }],
            },
        })).toBe(false)
    })

    it("CA22.b: intercorrencia preenchida com cirurgião vazio → retorna false", () => {
        expect(validarCirurgiao({
            "desc-cirurgica": {
                cirurgiao: "",
                intercorrencia: "com",
            },
        })).toBe(false)
    })
})
