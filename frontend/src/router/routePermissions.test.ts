import { describe, it, expect } from "vitest"
import {
    podeAcessarRota,
    regraDaRota,
    rotaRestrita,
    ROTAS_RESTRITAS,
} from "./routePermissions"

/**
 * Bug A/C — testes do catálogo centralizado de permissões por rota.
 *
 * Garante que:
 *  - Dono passa em qualquer rota (incluindo as `somenteDono`).
 *  - Profissional sem permissão é bloqueado nas restritas.
 *  - Profissional com permissão de área (`equipe.ver`) ou extra
 *    (`gerir_profissionais` / `gerir_permissoes`) entra em /equipe.
 *  - Rotas livres (Home, MeusConvites, Notificacoes) não exigem nada.
 */

const PROFISSIONAL_SEM_NADA = {
    ehDono: false,
    pode: () => false,
    podeExtra: () => false,
}

const DONO = {
    ehDono: true,
    pode: () => true,
    podeExtra: () => true,
}

function profissionalCom(opts: { permissoes?: string[]; extras?: string[] } = {}) {
    const perms = opts.permissoes ?? []
    const extras = opts.extras ?? []
    return {
        ehDono: false,
        pode: (k: string) => perms.includes(k),
        podeExtra: (k: string) => extras.includes(k),
    }
}

describe("routePermissions — catálogo central", () => {
    it("expõe regra para todas as áreas sensíveis do menu", () => {
        // Espelha o nav do AppLayout — se um item aparecer no menu, a regra precisa existir.
        const esperadas = [
            "Agenda", "MinhasConsultas", "Pacientes", "Equipe",
            "Financeiro", "Orcamentos", "Inventario", "Relatorios", "Automacoes",
        ]
        for (const nome of esperadas) {
            expect(rotaRestrita(nome), `rota ${nome} deve estar no catálogo`).toBe(true)
            expect(regraDaRota(nome)).not.toBeNull()
        }
    })

    it("Home, MeusConvites e MinhaConta NÃO são restritas (todo usuário com tenant acessa)", () => {
        for (const nome of ["Home", "MeusConvites", "MinhaConta", "Notificacoes", "MinhaContaLgpd"]) {
            expect(rotaRestrita(nome)).toBe(false)
        }
    })

    it("rota desconhecida ou vazia é tratada como livre", () => {
        expect(podeAcessarRota("RotaInexistente", PROFISSIONAL_SEM_NADA)).toBe(true)
        expect(podeAcessarRota(null, PROFISSIONAL_SEM_NADA)).toBe(true)
        expect(podeAcessarRota(undefined, PROFISSIONAL_SEM_NADA)).toBe(true)
    })
})

describe("routePermissions — Dono passa em tudo", () => {
    it.each(Object.keys(ROTAS_RESTRITAS))("Dono entra em %s", (nome) => {
        expect(podeAcessarRota(nome, DONO)).toBe(true)
    })
})

describe("routePermissions — Profissional sem permissões é bloqueado", () => {
    it.each(Object.keys(ROTAS_RESTRITAS))("Profissional zerado é bloqueado em %s", (nome) => {
        expect(podeAcessarRota(nome, PROFISSIONAL_SEM_NADA)).toBe(false)
    })
})

describe("routePermissions — gates específicos", () => {
    it("Profissional com `agenda.ver` entra em Agenda mas não em Financeiro", () => {
        const user = profissionalCom({ permissoes: ["agenda.ver"] })
        expect(podeAcessarRota("Agenda", user)).toBe(true)
        expect(podeAcessarRota("MinhasConsultas", user)).toBe(true)
        expect(podeAcessarRota("Financeiro", user)).toBe(false)
        expect(podeAcessarRota("Relatorios", user)).toBe(false)
        expect(podeAcessarRota("Equipe", user)).toBe(false)
    })

    it("Profissional com `equipe.ver` entra em Equipe", () => {
        const user = profissionalCom({ permissoes: ["equipe.ver"] })
        expect(podeAcessarRota("Equipe", user)).toBe(true)
    })

    it("Profissional com extra `gerir_profissionais` entra em Equipe (sem precisar de equipe.ver)", () => {
        const user = profissionalCom({ extras: ["gerir_profissionais"] })
        expect(podeAcessarRota("Equipe", user)).toBe(true)
    })

    it("Profissional com extra `gerir_permissoes` entra em Equipe", () => {
        const user = profissionalCom({ extras: ["gerir_permissoes"] })
        expect(podeAcessarRota("Equipe", user)).toBe(true)
    })

    it("Profissional sem nenhuma das três permissões de Equipe é bloqueado", () => {
        const user = profissionalCom({ permissoes: ["agenda.ver", "pacientes.ver"] })
        expect(podeAcessarRota("Equipe", user)).toBe(false)
    })

    it("Profissional com `config_estabelecimento` entra em Estabelecimento/IA", () => {
        const user = profissionalCom({ extras: ["config_estabelecimento"] })
        expect(podeAcessarRota("Estabelecimento", user)).toBe(true)
        expect(podeAcessarRota("IaSettings", user)).toBe(true)
        expect(podeAcessarRota("ModelosProntuario", user)).toBe(false)
    })

    it("Profissional Médico padrão NÃO entra em telas administrativas", () => {
        // Conjunto MedicoPadrao do backend (sem extras).
        const user = profissionalCom({
            permissoes: [
                "agenda.ver", "agenda.criar", "agenda.editar", "agenda.excluir",
                "prontuario.ver", "prontuario.editar", "prontuario.assinar",
                "prescricao.criar", "prescricao.assinar",
                "pacientes.ver", "pacientes.criar", "pacientes.editar",
                "orcamento.ver", "orcamento.criar",
                "relatorios.ver",
            ],
        })

        // Acessíveis ao Médico:
        expect(podeAcessarRota("Agenda", user)).toBe(true)
        expect(podeAcessarRota("MinhasConsultas", user)).toBe(true)
        expect(podeAcessarRota("Pacientes", user)).toBe(true)
        expect(podeAcessarRota("Prontuario", user)).toBe(true)
        expect(podeAcessarRota("Orcamentos", user)).toBe(true)
        expect(podeAcessarRota("Relatorios", user)).toBe(true)

        // Bloqueadas para o Médico (eram o vazamento do Bug A):
        expect(podeAcessarRota("Equipe", user)).toBe(false)
        expect(podeAcessarRota("Financeiro", user)).toBe(false)
        expect(podeAcessarRota("Inventario", user)).toBe(false)
        expect(podeAcessarRota("Automacoes", user)).toBe(false)
        expect(podeAcessarRota("Estabelecimento", user)).toBe(false)
        expect(podeAcessarRota("IaSettings", user)).toBe(false)
        expect(podeAcessarRota("ModelosProntuario", user)).toBe(false)
    })

    it("Profissional Recepção padrão entra em Agenda/Pacientes/Financeiro só leitura", () => {
        const user = profissionalCom({
            permissoes: [
                "agenda.ver", "agenda.criar", "agenda.editar", "agenda.excluir",
                "pacientes.ver", "pacientes.criar", "pacientes.editar",
                "prontuario.ver",
                "convenios.ver",
                "financeiro.ver",
            ],
        })
        expect(podeAcessarRota("Agenda", user)).toBe(true)
        expect(podeAcessarRota("Pacientes", user)).toBe(true)
        expect(podeAcessarRota("Financeiro", user)).toBe(true)
        expect(podeAcessarRota("CategoriasFinanceiras", user)).toBe(true)
        expect(podeAcessarRota("Equipe", user)).toBe(false)
        expect(podeAcessarRota("Orcamentos", user)).toBe(false)
        expect(podeAcessarRota("Relatorios", user)).toBe(false)
        expect(podeAcessarRota("Automacoes", user)).toBe(false)
    })
})
