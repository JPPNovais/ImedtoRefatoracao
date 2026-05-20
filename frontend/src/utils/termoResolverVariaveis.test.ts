import { describe, it, expect } from "vitest"
import {
    resolverVariaveis,
    formatarCpf,
    formatarCnpj,
    formatarTelefone,
    formatarData,
    formatarDataExtenso,
    formatarDataCurta,
    calcularIdade,
    type ContextoResolucaoTermo,
} from "./termoResolverVariaveis"

const ctxCompleto: ContextoResolucaoTermo = {
    paciente: {
        nomeCompleto: "João da Silva",
        cpf: "12345678900",
        documentoInternacional: "AB123456",
        dataNascimento: "1990-05-19",
        telefone: "11999998888",
        email: "joao@example.com",
        endereco: "Rua das Flores, 123",
        genero: "Masculino",
    },
    estabelecimento: {
        nomeFantasia: "Clínica Exemplo",
        razaoSocial: "Clínica Exemplo Ltda",
        cnpj: "12345678000190",
        telefone: "1133334444",
        endereco: "Av. Paulista, 1000",
        cidade: "São Paulo",
    },
    profissional: {
        nome: "Dra. Maria Souza",
        conselho: "CRM",
        uf: "SP",
        numeroRegistro: "123456",
        especialidade: "Cardiologia",
    },
    dataAtual: new Date("2026-05-19T12:00:00"),
}

describe("resolverVariaveis", () => {
    it("substitui todas as variáveis do contexto completo", () => {
        const html = `
            Eu, {{paciente.nome}} (CPF {{paciente.cpf}}), nascido em {{paciente.data_nascimento}}
            ({{paciente.idade}}), atendido em {{estabelecimento.nome}} (CNPJ
            {{estabelecimento.cnpj}}, {{cidade_atual}}), pelo profissional
            {{profissional.nome}} — {{profissional.conselho_completo}}.
            Data: {{data_atual}}.
        `
        const r = resolverVariaveis(html, ctxCompleto)
        expect(r.htmlResolvido).toContain("João da Silva")
        expect(r.htmlResolvido).toContain("123.456.789-00")
        expect(r.htmlResolvido).toContain("19/05/1990")
        expect(r.htmlResolvido).toContain("36 anos")
        expect(r.htmlResolvido).toContain("Clínica Exemplo")
        expect(r.htmlResolvido).toContain("12.345.678/0001-90")
        expect(r.htmlResolvido).toContain("São Paulo")
        expect(r.htmlResolvido).toContain("Dra. Maria Souza")
        expect(r.htmlResolvido).toContain("CRM-SP 123456")
        expect(r.htmlResolvido).toContain("19 de maio de 2026")
        // Nenhum {{...}} sobrou.
        expect(r.htmlResolvido).not.toContain("{{")
    })

    it("deduplica variáveis no log (mesma chave usada N vezes aparece uma vez)", () => {
        const html = "{{paciente.nome}} e {{paciente.nome}} de novo."
        const r = resolverVariaveis(html, ctxCompleto)
        const nomes = r.variaveisAplicadas.filter(v => v.chave === "{{paciente.nome}}")
        expect(nomes).toHaveLength(1)
    })

    it("preserva chaves desconhecidas (não tocadas)", () => {
        const html = "Olá {{paciente.nome}}, dado: {{nao_existe}}."
        const r = resolverVariaveis(html, ctxCompleto)
        expect(r.htmlResolvido).toContain("João da Silva")
        expect(r.htmlResolvido).toContain("{{nao_existe}}")
    })

    it("usa fallback para paciente sem CPF/telefone/data nascimento", () => {
        const ctx: ContextoResolucaoTermo = {
            ...ctxCompleto,
            paciente: {
                ...ctxCompleto.paciente,
                cpf: null,
                telefone: null,
                dataNascimento: null,
            },
        }
        const r = resolverVariaveis(
            "{{paciente.cpf}} | {{paciente.telefone}} | {{paciente.data_nascimento}} | {{paciente.idade}}",
            ctx,
        )
        expect(r.htmlResolvido).toContain("___.___.___-__")
        expect(r.htmlResolvido).toContain("__/__/____")
        const aplicadas = new Map(r.variaveisAplicadas.map(v => [v.chave, v]))
        expect(aplicadas.get("{{paciente.cpf}}")?.fallback).toBe(true)
        expect(aplicadas.get("{{paciente.data_nascimento}}")?.fallback).toBe(true)
    })

    it("usa fallback de linha para nome vazio (defesa de UI)", () => {
        const ctx: ContextoResolucaoTermo = {
            ...ctxCompleto,
            paciente: { ...ctxCompleto.paciente, nomeCompleto: "   " },
        }
        const r = resolverVariaveis("Eu, {{paciente.nome}}, declaro.", ctx)
        expect(r.htmlResolvido).toContain("___________")
    })

    it("resolve estabelecimento ausente com fallback", () => {
        const ctx: ContextoResolucaoTermo = { ...ctxCompleto, estabelecimento: null }
        const r = resolverVariaveis("{{estabelecimento.nome}} - {{cidade_atual}}", ctx)
        expect(r.htmlResolvido).toContain("___________")
    })

    it("retorna string vazia / log vazio para HTML vazio", () => {
        const r = resolverVariaveis("", ctxCompleto)
        expect(r.htmlResolvido).toBe("")
        expect(r.variaveisAplicadas).toHaveLength(0)
    })

    it("retorna HTML inalterado se não houver placeholders", () => {
        const html = "<p>Texto fixo sem variáveis.</p>"
        const r = resolverVariaveis(html, ctxCompleto)
        expect(r.htmlResolvido).toBe(html)
        expect(r.variaveisAplicadas).toHaveLength(0)
    })
})

describe("formatadores isolados", () => {
    it("formatarCpf aceita 11 dígitos e devolve fallback fora disso", () => {
        expect(formatarCpf("12345678900").valor).toBe("123.456.789-00")
        expect(formatarCpf("123.456.789-00").valor).toBe("123.456.789-00")
        expect(formatarCpf("123").fallback).toBe(true)
        expect(formatarCpf(null).fallback).toBe(true)
    })

    it("formatarCnpj aceita 14 dígitos", () => {
        expect(formatarCnpj("12345678000190").valor).toBe("12.345.678/0001-90")
        expect(formatarCnpj("nada").fallback).toBe(true)
    })

    it("formatarTelefone aceita 10 ou 11 dígitos", () => {
        expect(formatarTelefone("11999998888").valor).toBe("(11) 99999-8888")
        expect(formatarTelefone("1133334444").valor).toBe("(11) 3333-4444")
        expect(formatarTelefone(null).fallback).toBe(true)
    })

    it("formatarData formata yyyy-mm-dd em dd/mm/yyyy", () => {
        expect(formatarData("1990-05-19").valor).toBe("19/05/1990")
        expect(formatarData(null).fallback).toBe(true)
        expect(formatarData("invalida").fallback).toBe(true)
    })

    it("formatarDataExtenso usa mês em minúsculo pt-BR", () => {
        expect(formatarDataExtenso(new Date(2026, 4, 19))).toBe("19 de maio de 2026")
        expect(formatarDataExtenso(new Date(2026, 0, 1))).toBe("1 de janeiro de 2026")
    })

    it("formatarDataCurta usa dd/MM/yyyy", () => {
        expect(formatarDataCurta(new Date(2026, 4, 9))).toBe("09/05/2026")
    })

    it("calcularIdade considera mês/dia para ano corrente", () => {
        // Hoje 19/05/2026. Aniversário no mesmo dia = ano completo.
        const hoje = new Date(2026, 4, 19)
        expect(calcularIdade("1990-05-19", hoje).valor).toBe("36 anos")
        // Aniversário no dia seguinte → ainda não fez = 35 anos.
        expect(calcularIdade("1990-05-20", hoje).valor).toBe("35 anos")
        // 1 ano (singular).
        expect(calcularIdade("2025-05-19", hoje).valor).toBe("1 ano")
    })
})
