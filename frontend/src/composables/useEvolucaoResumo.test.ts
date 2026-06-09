import { describe, it, expect } from "vitest"
import { formatarSecaoLegivel } from "./useEvolucaoResumo"

// Briefing 2026-06-09_008 — renderização legível das seções de evolução.
// Cobre CA1..CA17 no nível do ponto único de formatação. CA18 (multi-tenant)
// e CA19 (suíte sem regressão) são validados fora deste arquivo.

describe("formatarSecaoLegivel — HPP", () => {
    it("CA1 — alergia preenchida sem ruído técnico", () => {
        const out = formatarSecaoLegivel("hpp", {
            alergiasTem: true,
            alergias: [{ nome: "TESTEE", observacao: "" }],
        })
        expect(out).toContain("Alergias: TESTEE")
        expect(out).not.toContain("alergiasTem")
        expect(out).not.toContain("nome:")
        expect(out).not.toContain("observacao")
        expect(out).not.toContain("true")
    })

    it("CA2 — negativas clínicas registram 'Nega'", () => {
        const out = formatarSecaoLegivel("hpp", { alergiasTem: false, medicacoesTem: false })
        expect(out).toContain("Alergias: Nega")
        expect(out).toContain("Medicações de uso: Nega")
        expect(out).not.toContain("false")
        expect(out).not.toContain("alergiasTem")
        expect(out).not.toContain("medicacoesTem")
    })

    it("CA3 — negativas não-clínicas omitidas; seção vazia vira string vazia", () => {
        const out = formatarSecaoLegivel("hpp", { cirurgiasTem: false, doencasTem: false })
        expect(out).toBe("")
    })

    it("CA4 — flag positiva sem itens é omitida (sem 'Sim' solto)", () => {
        const out = formatarSecaoLegivel("hpp", {
            cirurgiasTem: true,
            cirurgias: [{ nome: "", ano: "", observacao: "" }],
        })
        expect(out).toBe("")
        expect(out).not.toContain("Sim")
        expect(out).not.toContain("Cirurgias")
    })

    it("CA5 — medicação com detalhes parciais", () => {
        const out = formatarSecaoLegivel("hpp", {
            medicacoesTem: true,
            medicacoes: [{ nome: "Losartana", dose: "50mg", frequencia: "1x/dia", motivo: "Hipertensão", observacoes: "" }],
        })
        expect(out).toContain("Losartana 50mg, 1x/dia — Hipertensão")
        expect(out).not.toContain("dose:")
        expect(out).not.toContain("motivo:")
    })

    it("CA6 — cirurgia sem ano não imprime parênteses vazios", () => {
        const out = formatarSecaoLegivel("hpp", {
            cirurgiasTem: true,
            cirurgias: [{ nome: "Apendicectomia", ano: "", observacao: "" }],
        })
        expect(out).toContain("Cirurgias: Apendicectomia")
        expect(out).not.toContain("()")
        expect(out).not.toContain("(/)")
    })

    it("alergia com observação usa parênteses", () => {
        const out = formatarSecaoLegivel("hpp", {
            alergiasTem: true,
            alergias: [{ nome: "Dipirona", observacao: "reação leve" }],
        })
        expect(out).toContain("Alergias: Dipirona (reação leve)")
    })
})

describe("formatarSecaoLegivel — História familiar", () => {
    it("CA7 — pai/mãe/parente legíveis", () => {
        const out = formatarSecaoLegivel("h-familiar", {
            paiDoencas: "Hipertensão",
            paiDescricao: "",
            maeDoencas: "",
            parentes: [{ parentesco: "Avó materna", doencas: "Diabetes", comentario: "tipo 2" }],
            observacao: "",
        })
        expect(out).toContain("Pai: Hipertensão")
        expect(out).toContain("Avó materna: Diabetes — tipo 2")
        expect(out).not.toContain("paiDoencas")
        expect(out).not.toContain("maeDoencas")
        expect(out).not.toContain("parentesco:")
        expect(out).not.toContain("Mãe:")
    })
})

describe("formatarSecaoLegivel — História social", () => {
    it("CA8 — negativas e positivas clínicas mistas", () => {
        const out = formatarSecaoLegivel("h-social", {
            estadoCivil: "Casado(a)",
            tabagismoTem: false,
            etilismoTem: true,
            etilismoStatus: "Social",
            drogasTem: false,
            atividadeFisicaTem: false,
        })
        expect(out).toContain("Estado civil: Casado(a)")
        expect(out).toContain("Tabagismo: Não")
        expect(out).toContain("Etilismo: Social")
        expect(out).toContain("Drogas: Não")
        expect(out).not.toContain("Atividade física")
        expect(out).not.toContain("tabagismoTem")
        expect(out).not.toContain("false")
    })
})

describe("formatarSecaoLegivel — Exame físico", () => {
    it("CA9 — sinais vitais agrupados com unidades", () => {
        const out = formatarSecaoLegivel("exame-fisico", {
            paSistolica: "120", paDiastolica: "80", fc: "72", temperatura: "36.5", spo2: "98",
        })
        expect(out).toContain("PA: 120/80 mmHg, FC: 72 bpm, Temp: 36.5 °C, SpO₂: 98%")
        expect(out).not.toContain("paSistolica")
        expect(out).not.toContain("paDiastolica")
        expect(out).not.toContain("FR:")
        expect(out).not.toContain("Glicemia:")
    })

    it("CA10 — região anatômica detalhada", () => {
        const out = formatarSecaoLegivel("exame-fisico", {
            regioes: [{ caminho: "Tórax", lateralidade: "bilateral", vista: "anterior", texto_exame: "Murmúrio vesicular presente", achados: "", observacoes: "" }],
        })
        expect(out).toContain("Tórax (anterior), bilateral: Murmúrio vesicular presente")
        expect(out).not.toContain("regiao_id")
        expect(out).not.toContain("timestamp")
        expect(out).not.toContain("caminho:")
        expect(out).not.toContain("lateralidade:")
    })

    it("CA11 — lateralidade nula omitida", () => {
        const out = formatarSecaoLegivel("exame-fisico", {
            regioes: [{ caminho: "Abdome", lateralidade: null, vista: "anterior", texto_exame: "Indolor", achados: "", observacoes: "" }],
        })
        expect(out).toContain("Abdome (anterior): Indolor")
        expect(out).not.toContain("null")
        expect(out).not.toContain(", :")
        expect(out).not.toContain("()")
    })
})

describe("formatarSecaoLegivel — Exames realizados", () => {
    it("CA12 — exame com tipo e material", () => {
        const out = formatarSecaoLegivel("exames-realizados", {
            itens: [{ tipo: "Laboratorial", material: "Sangue", nome: "Hemograma", comentario: "em jejum" }],
            observacoes: "",
        })
        expect(out).toContain("Hemograma (Laboratorial, Sangue) — em jejum")
        expect(out).not.toContain("tipo:")
        expect(out).not.toContain("material:")
        expect(out).not.toContain("comentario:")
    })
})

describe("formatarSecaoLegivel — Procedimentos indicados", () => {
    it("CA13 — procedimento com observação", () => {
        const out = formatarSecaoLegivel("procedimentos-indicados", {
            procedimentos: [{ descricao: "Infiltração", observacao: "joelho direito" }],
            observacoes: "",
        })
        expect(out).toContain("Infiltração — joelho direito")
    })
})

describe("formatarSecaoLegivel — fallback genérico e texto puro", () => {
    it("CA14 — humaniza chave não mapeada e omite boolean", () => {
        const out = formatarSecaoLegivel("secao-desconhecida", {
            atividadeFisicaNivel: "Moderado",
            algumFlag: false,
        })
        expect(out).toContain("Moderado")
        expect(out).not.toContain("false")
        expect(out).not.toContain("algumFlag")
        expect(out).not.toContain("atividadeFisicaNivel")
        // rótulo humanizado (separa camelCase, sem chave técnica crua)
        expect(out.toLowerCase()).toContain("atividade")
    })

    it("CA15 — texto puro inalterado", () => {
        const out = formatarSecaoLegivel("queixa", "  Paciente refere dor há 3 dias.  ")
        expect(out).toBe("Paciente refere dor há 3 dias.")
    })
})

describe("formatarSecaoLegivel — robustez de schema", () => {
    it("CA17 — seção sem conteúdo legível retorna string vazia", () => {
        expect(formatarSecaoLegivel("hpp", {})).toBe("")
        expect(formatarSecaoLegivel("hpp", null)).toBe("")
        expect(formatarSecaoLegivel("exame-fisico", { regioes: [] })).toBe("")
    })

    it("não lança com campos ausentes ou tipos inesperados", () => {
        expect(() => formatarSecaoLegivel("hpp", { alergias: "não-array" })).not.toThrow()
        expect(() => formatarSecaoLegivel("h-social", { tabagismoTem: "talvez" })).not.toThrow()
        expect(() => formatarSecaoLegivel("exame-fisico", { regioes: [null, 42] })).not.toThrow()
    })
})

describe("formatarSecaoLegivel — paridade modal ↔ PDF (CA16)", () => {
    // Modal e PDF chamam a MESMA função com a MESMA assinatura — a paridade é
    // garantida por construção. Verificamos determinismo da saída.
    it("mesma entrada produz string idêntica em chamadas repetidas", () => {
        const conteudo = {
            alergiasTem: true,
            alergias: [{ nome: "Dipirona", observacao: "reação leve" }],
            medicacoesTem: false,
        }
        const a = formatarSecaoLegivel("hpp", conteudo)
        const b = formatarSecaoLegivel("hpp", conteudo)
        expect(a).toBe(b)
        expect(a).toContain("Alergias: Dipirona (reação leve)")
        expect(a).toContain("Medicações de uso: Nega")
    })
})
