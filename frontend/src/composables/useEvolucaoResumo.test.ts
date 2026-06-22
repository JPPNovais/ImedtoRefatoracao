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

// ── F3 — Procedimentos indicados: snapshot + retrocompat (CA58) ───────────────

describe("formatarSecaoLegivel — procedimentos-indicados (F3/CA58)", () => {
    it("CA58 — formato novo: exibe descrição + valor do snapshot, não re-resolve catálogo", () => {
        const out = formatarSecaoLegivel("procedimentos-indicados", {
            procedimentos: [
                { catalogoCirurgiaId: 1, descricao: "Infiltração articular", valor: 350, observacao: "joelho D" },
            ],
        })
        expect(out).toContain("Infiltração articular")
        expect(out).toContain("R$ 350,00")
        expect(out).toContain("joelho D")
        // Não deve conter catalogoCirurgiaId cru
        expect(out).not.toContain("catalogoCirurgiaId")
    })

    it("CA58 — formato legado: exibe texto livre sem valor (sem quebrar)", () => {
        const out = formatarSecaoLegivel("procedimentos-indicados", {
            procedimentos: [
                { descricao: "Curativo simples", observacao: "tecido granulado" },
            ],
        })
        expect(out).toContain("Curativo simples")
        expect(out).toContain("tecido granulado")
        // Sem valor (legado não tem)
        expect(out).not.toContain("R$")
    })

    it("CA58 — coexistência: novo e legado no mesmo render (sem quebrar)", () => {
        const out = formatarSecaoLegivel("procedimentos-indicados", {
            procedimentos: [
                { catalogoCirurgiaId: 1, descricao: "Infiltração articular", valor: 350, observacao: "" },
                { descricao: "Curativo", observacao: "" },
            ],
        })
        expect(out).toContain("Infiltração articular")
        expect(out).toContain("R$ 350")
        expect(out).toContain("Curativo")
    })

    it("CA58 — observações gerais da seção aparecem no render", () => {
        const out = formatarSecaoLegivel("procedimentos-indicados", {
            procedimentos: [],
            observacoes: "Aguardar autorização do plano.",
        })
        expect(out).toContain("Aguardar autorização do plano.")
    })

    it("CA58 — observação vazia não imprime parênteses vazios", () => {
        const out = formatarSecaoLegivel("procedimentos-indicados", {
            procedimentos: [
                { catalogoCirurgiaId: 2, descricao: "Drenagem", valor: 120, observacao: "" },
            ],
        })
        expect(out).toContain("Drenagem")
        expect(out).not.toContain("()")
    })
})

// ── Briefing 2026-06-21_001 — evolucao-pos-op ─────────────────────────────────

describe("formatarSecaoLegivel — evolucao-pos-op (CA9/CA10/CA11)", () => {
    it("CA9 — string legada continua legível (retrocompatibilidade)", () => {
        const out = formatarSecaoLegivel("evolucao-pos-op", "Paciente evoluindo bem, sem intercorrências.")
        expect(out).toBe("Paciente evoluindo bem, sem intercorrências.")
        expect(out).not.toContain("[object Object]")
        expect(out).not.toContain("{")
    })

    it("CA10 — objeto estruturado: campos preenchidos aparecem com rótulo legível", () => {
        const out = formatarSecaoLegivel("evolucao-pos-op", {
            evolucaoPaciente: "boa",
            evolucaoComentario: "sem dor",
            seguindoOrientacoes: "sim",
            orientacoesComentario: "",
            dataCirurgia: "2024-01-10",
            dpo: "5",
            destino: "Alta",
            dieta: "Livre",
            observacao: "Retorno em 7 dias",
        })
        expect(out).toContain("Evolução: Boa")
        expect(out).toContain("sem dor")
        expect(out).toContain("Seguindo orientações: Sim")
        expect(out).toContain("DPO: 5")
        expect(out).toContain("Destino: Alta")
        expect(out).toContain("Dieta: Livre")
        expect(out).toContain("Retorno em 7 dias")
        // Sem chave técnica
        expect(out).not.toContain("evolucaoPaciente")
        expect(out).not.toContain("seguindoOrientacoes")
        expect(out).not.toContain('"boa"')
    })

    it("CA11 — objeto totalmente vazio retorna string vazia (seção omitida)", () => {
        expect(formatarSecaoLegivel("evolucao-pos-op", {})).toBe("")
        expect(formatarSecaoLegivel("evolucao-pos-op", {
            evolucaoPaciente: "", seguindoOrientacoes: "", dataCirurgia: "", dpo: "", destino: "", dieta: "", observacao: "",
        })).toBe("")
    })

    it("CA10 — sem fields: true/false cru na saída", () => {
        const out = formatarSecaoLegivel("evolucao-pos-op", { evolucaoPaciente: "ruim" })
        expect(out).not.toContain("true")
        expect(out).not.toContain("false")
    })
})

// ── Briefing 2026-06-21_001 — desc-cirurgica ──────────────────────────────────

describe("formatarSecaoLegivel — desc-cirurgica (CA9/CA10/CA11)", () => {
    it("CA9 — string legada continua legível (retrocompatibilidade)", () => {
        const out = formatarSecaoLegivel("desc-cirurgica", "Colecistectomia videolaparoscópica realizada sem intercorrências.")
        expect(out).toBe("Colecistectomia videolaparoscópica realizada sem intercorrências.")
        expect(out).not.toContain("[object Object]")
    })

    it("CA10 — objeto estruturado: campos principais aparecem legíveis", () => {
        const out = formatarSecaoLegivel("desc-cirurgica", {
            cirurgiao: "Dr. Fulano de Tal",
            data: "2024-03-15",
            diaSemana: "Sexta-feira",
            cirurgiasRealizadas: "Colecistectomia videolaparoscópica",
            anestesista: "Dr. Anestesista",
            auxiliar: "Dr. Auxiliar",
            instrumentador: "",
            outrosMembros: [{ funcao: "Circulante", nome: "Enf. Joana" }],
            cirurgiaInicio: "08:00",
            cirurgiaFim: "10:00",
            profilaxia: {
                enoxaparina: true, meiaCompressiva: true,
                cefazolina: true, gentamicina: false,
                antitrombOutroAtivo: false, antitrombOutro: "",
                antibioOutroAtivo: false, antibioOutro: "",
                botaPneumatica: false, deambulacaoPrecoce: false,
            },
            intercorrencia: "sem",
            intercorrenciaDescricao: "",
            tecnicaOperatoria: "Técnica de 4 portais",
            observacoes: "",
        })
        expect(out).toContain("Cirurgião: Dr. Fulano de Tal")
        expect(out).toContain("Sexta-feira")
        expect(out).toContain("Colecistectomia videolaparoscópica")
        expect(out).toContain("Dr. Anestesista")
        expect(out).toContain("Circulante")
        expect(out).toContain("Enf. Joana")
        expect(out).toContain("Duração: 02:00")
        expect(out).toContain("Enoxaparina 40mg SC")
        expect(out).toContain("Meia compressiva")
        expect(out).toContain("Cefazolina")
        expect(out).toContain("Sem intercorrências")
        expect(out).toContain("Técnica de 4 portais")
        // Sem chaves técnicas
        expect(out).not.toContain("cirurgiao:")
        expect(out).not.toContain("antitrombOutroAtivo")
        expect(out).not.toContain("true")
        expect(out).not.toContain("false")
    })

    it("CA6 — sem campos de anestesia na saída (R7)", () => {
        const out = formatarSecaoLegivel("desc-cirurgica", {
            cirurgiao: "Dr. X",
            anestesista: "Dr. Anest",
        })
        // Anestesista (nome, dado de equipe) deve aparecer
        expect(out).toContain("Dr. Anest")
        // Tipos e horários de anestesia nunca devem aparecer
        expect(out).not.toContain("tipoAnestesia")
        expect(out).not.toContain("anestesiaInicio")
        expect(out).not.toContain("anestesiaFim")
    })

    it("CA11 — objeto totalmente vazio retorna string vazia", () => {
        expect(formatarSecaoLegivel("desc-cirurgica", {})).toBe("")
        expect(formatarSecaoLegivel("desc-cirurgica", {
            cirurgiao: "", data: "", cirurgiasRealizadas: "",
            anestesista: "", auxiliar: "", instrumentador: "",
            outrosMembros: [], cirurgiaInicio: "", cirurgiaFim: "",
            intercorrencia: "", tecnicaOperatoria: "", observacoes: "",
        })).toBe("")
    })

    it("intercorrência 'com' exibe descrição", () => {
        const out = formatarSecaoLegivel("desc-cirurgica", {
            cirurgiao: "Dr. X",
            intercorrencia: "com",
            intercorrenciaDescricao: "Sangramento intraoperatório controlado",
        })
        expect(out).toContain("Sangramento intraoperatório controlado")
        expect(out).not.toContain('"com"')
    })
})
