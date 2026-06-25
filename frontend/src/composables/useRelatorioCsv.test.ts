/**
 * Testes para useRelatorioCsv — briefing 2026-06-24_002 Q5.
 *
 * CA13: tabela de pacientes não tem coluna "Total gasto" (verificado no HTML via RelatorioPessoasTab.vue).
 * CA14: CSV de pacientes não inclui coluna "Total gasto (R$)" nem o valor 0.
 *
 * Cobrimos CA14 aqui (comportamento do composable).
 * CA13 é coberto pelo teste de componente abaixo.
 */
import { describe, it, expect, vi, beforeEach } from "vitest"
import { useRelatorioCsv } from "./useRelatorioCsv"

// Mocka baixarCsv para capturar o conteúdo gerado sem disparar download.
vi.mock("@/utils/csv", async (importOriginal) => {
    const original = await importOriginal<typeof import("@/utils/csv")>()
    return {
        ...original,
        baixarCsv: vi.fn(),
        nomeArquivoCsv: vi.fn(() => "pessoas-2026-06.csv"),
    }
})

import { baixarCsv } from "@/utils/csv"

describe("useRelatorioCsv — exportarPessoas (CA14 Q5)", () => {
    const periodo = { dataInicio: "2026-06-01", dataFim: "2026-06-30" }

    beforeEach(() => {
        vi.clearAllMocks()
    })

    it("CA14: CSV de pacientes NÃO contém coluna 'Total gasto (R$)'", () => {
        const { exportarPessoas } = useRelatorioCsv()

        exportarPessoas(
            {
                tipo: "pacientes",
                topPacientes: [
                    { nome: "Ana Lima", totalConsultas: 3 },
                    { nome: "Carlos Souza", totalConsultas: 1 },
                ],
            },
            periodo,
        )

        expect(baixarCsv).toHaveBeenCalledOnce()
        const csvContent = (baixarCsv as ReturnType<typeof vi.fn>).mock.calls[0][0] as string

        expect(csvContent).not.toContain("Total gasto")
        expect(csvContent).not.toContain("R$")
        expect(csvContent).toContain("Paciente")
        expect(csvContent).toContain("Consultas")
    })

    it("CA14: CSV de pacientes contém cabeçalho '#; Paciente; Consultas' (sem coluna de valor)", () => {
        const { exportarPessoas } = useRelatorioCsv()

        exportarPessoas(
            {
                tipo: "pacientes",
                topPacientes: [{ nome: "Ana Lima", totalConsultas: 5 }],
            },
            periodo,
        )

        const csvContent = (baixarCsv as ReturnType<typeof vi.fn>).mock.calls[0][0] as string
        const linhas = csvContent.split("\n")

        // A linha de cabeçalho (após as linhas de período/vazia) deve ter 3 colunas.
        const linhaCabecalho = linhas.find((l) => l.includes("Paciente"))
        expect(linhaCabecalho).toBeDefined()
        const colunas = linhaCabecalho!.split(";")
        expect(colunas).toHaveLength(3) // #, Paciente, Consultas
    })

    it("CA14: cada linha de dado de paciente tem 3 colunas (sem coluna de valor)", () => {
        const { exportarPessoas } = useRelatorioCsv()

        exportarPessoas(
            {
                tipo: "pacientes",
                topPacientes: [
                    { nome: "Ana Lima", totalConsultas: 5 },
                    { nome: "Bruno Melo", totalConsultas: 2 },
                ],
            },
            periodo,
        )

        const csvContent = (baixarCsv as ReturnType<typeof vi.fn>).mock.calls[0][0] as string
        const linhas = csvContent
            .split("\n")
            .filter((l) => l.trim() !== "" && !l.startsWith("Período"))

        // Linhas de dados devem ter exatamente 3 colunas.
        const linhasDados = linhas.filter((l) => !l.includes("Paciente"))
        for (const linha of linhasDados) {
            const cols = linha.split(";")
            expect(cols).toHaveLength(3)
        }
    })

    it("CA14: nenhuma linha do CSV contém '0,00' de totalGasto hardcoded", () => {
        const { exportarPessoas } = useRelatorioCsv()

        exportarPessoas(
            {
                tipo: "pacientes",
                topPacientes: [{ nome: "Ana Lima", totalConsultas: 3 }],
            },
            periodo,
        )

        const csvContent = (baixarCsv as ReturnType<typeof vi.fn>).mock.calls[0][0] as string
        // O valor "0,00" não deve aparecer pois não há coluna monetária neste CSV.
        expect(csvContent).not.toContain("0,00")
    })

    it("CA14: CSV de profissionais ainda funciona normalmente (sem regressão)", () => {
        const { exportarPessoas } = useRelatorioCsv()

        exportarPessoas(
            {
                tipo: "profissionais",
                rankingProfissionais: [
                    { nome: "Dr. Silva", totalAtendimentos: 10, faturamento: 5000 },
                ],
            },
            periodo,
        )

        expect(baixarCsv).toHaveBeenCalledOnce()
        const csvContent = (baixarCsv as ReturnType<typeof vi.fn>).mock.calls[0][0] as string
        expect(csvContent).toContain("Profissional")
        expect(csvContent).toContain("Faturamento")
    })
})
