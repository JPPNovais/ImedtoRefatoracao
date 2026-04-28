/**
 * Service local de receitas médicas.
 *
 * Enquanto o backend não tem endpoints de receitas (o legado tem tabelas
 * `receitas` e `receita_itens`; o novo ainda não implementou), armazenamos
 * as receitas localmente por estabelecimento+paciente no `localStorage` do
 * navegador. A estrutura de dados e o fluxo são iguais ao legado, de modo
 * que quando o backend for implementado basta trocar a persistência.
 */

export type StatusReceita = "DRAFT" | "FINALIZED" | "CANCELED"
export type TipoReceita   = "SIMPLES" | "CONTROLADA"

export interface ReceitaItem {
    id: string                    // uuid local
    medicamento: string
    concentracao: string          // "500mg"
    formaFarmaceutica: string     // "Comprimido", "Cápsula", ...
    quantidade: string            // "1 caixa"
    viaAdministracao: string      // "Oral", "IV", ...
    posologia: string             // "Tomar 1 comprimido de 8/8h"
    duracao: string               // "7 dias"
    instrucoes: string            // texto livre
}

export interface Receita {
    id: string                    // uuid local
    estabelecimentoId: number
    pacienteId: number
    autor: string                 // nome do profissional logado
    tipo: TipoReceita
    status: StatusReceita
    versao: number
    criadaEm: string              // ISO
    atualizadaEm: string
    finalizadaEm: string | null
    observacoes: string
    incluirDataNoPdf: boolean
    itens: ReceitaItem[]
}

// ────────────────────────────────────────────────────────────────────────────

const KEY = "imedto.receitas.v1"

function uid() {
    return "r_" + Math.random().toString(36).slice(2, 10) + Date.now().toString(36)
}

function carregarTodas(): Receita[] {
    try {
        const raw = localStorage.getItem(KEY)
        if (!raw) return []
        const parsed = JSON.parse(raw)
        return Array.isArray(parsed) ? parsed : []
    } catch {
        return []
    }
}

function salvarTodas(lista: Receita[]) {
    localStorage.setItem(KEY, JSON.stringify(lista))
}

export const receitaLocalService = {
    listarDoPaciente(estabelecimentoId: number, pacienteId: number): Receita[] {
        return carregarTodas()
            .filter(r => r.estabelecimentoId === estabelecimentoId && r.pacienteId === pacienteId)
            .sort((a, b) => b.criadaEm.localeCompare(a.criadaEm))
    },

    obter(id: string): Receita | null {
        return carregarTodas().find(r => r.id === id) ?? null
    },

    criar(dados: {
        estabelecimentoId: number
        pacienteId: number
        autor: string
        tipo?: TipoReceita
    }): Receita {
        const agora = new Date().toISOString()
        const nova: Receita = {
            id: uid(),
            estabelecimentoId: dados.estabelecimentoId,
            pacienteId: dados.pacienteId,
            autor: dados.autor,
            tipo: dados.tipo ?? "SIMPLES",
            status: "DRAFT",
            versao: 1,
            criadaEm: agora,
            atualizadaEm: agora,
            finalizadaEm: null,
            observacoes: "",
            incluirDataNoPdf: true,
            itens: [],
        }
        const todas = carregarTodas()
        todas.push(nova)
        salvarTodas(todas)
        return nova
    },

    atualizar(id: string, patch: Partial<Receita>): Receita | null {
        const todas = carregarTodas()
        const idx = todas.findIndex(r => r.id === id)
        if (idx === -1) return null
        const atualizada = { ...todas[idx], ...patch, atualizadaEm: new Date().toISOString() }
        todas[idx] = atualizada
        salvarTodas(todas)
        return atualizada
    },

    adicionarItem(id: string, item: Omit<ReceitaItem, "id">): Receita | null {
        const r = this.obter(id); if (!r) return null
        const novoItem: ReceitaItem = { ...item, id: uid() }
        return this.atualizar(id, { itens: [...r.itens, novoItem] })
    },

    atualizarItem(receitaId: string, itemId: string, patch: Partial<ReceitaItem>): Receita | null {
        const r = this.obter(receitaId); if (!r) return null
        const itens = r.itens.map(it => (it.id === itemId ? { ...it, ...patch } : it))
        return this.atualizar(receitaId, { itens })
    },

    removerItem(receitaId: string, itemId: string): Receita | null {
        const r = this.obter(receitaId); if (!r) return null
        return this.atualizar(receitaId, { itens: r.itens.filter(it => it.id !== itemId) })
    },

    finalizar(id: string): Receita | null {
        const agora = new Date().toISOString()
        return this.atualizar(id, { status: "FINALIZED", finalizadaEm: agora })
    },

    cancelar(id: string): Receita | null {
        return this.atualizar(id, { status: "CANCELED" })
    },

    excluir(id: string): void {
        const todas = carregarTodas().filter(r => r.id !== id)
        salvarTodas(todas)
    },

    novaVersao(id: string): Receita | null {
        const origem = this.obter(id); if (!origem) return null
        const agora = new Date().toISOString()
        const nova: Receita = {
            ...origem,
            id: uid(),
            versao: origem.versao + 1,
            status: "DRAFT",
            criadaEm: agora,
            atualizadaEm: agora,
            finalizadaEm: null,
            itens: origem.itens.map(it => ({ ...it, id: uid() })),
        }
        const todas = carregarTodas()
        todas.push(nova)
        salvarTodas(todas)
        return nova
    },
}

// ────────────────────────────────────────────────────────────────────────────
// Constantes usadas nos dropdowns (iguais ao legado)

export const FORMAS_FARMACEUTICAS = [
    "Comprimido", "Cápsula", "Drágea", "Solução oral", "Suspensão oral",
    "Xarope", "Gotas", "Pomada", "Creme", "Gel", "Spray",
    "Injeção", "Supositório", "Adesivo", "Colírio",
]

export const VIAS_ADMINISTRACAO = [
    "Oral", "Sublingual", "Retal", "Vaginal",
    "Intramuscular (IM)", "Intravenosa (IV)", "Subcutânea (SC)",
    "Tópica", "Inalatória", "Oftálmica", "Otológica", "Nasal",
]
