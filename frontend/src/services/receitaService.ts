import httpClient from "./httpClient"

export type TipoReceita = "Comum" | "Controlada" | "Antibiotico" | "Especial"
export type StatusReceita = "Rascunho" | "Emitida" | "Cancelada" | "Substituida"

export interface ReceitaItem {
    id?: number
    ordem: number
    medicamento: string
    posologia: string
    quantidade?: string
    concentracao?: string
    viaAdministracao?: string
    formaFarmaceutica?: string
    duracao?: string
    observacao?: string
}

export interface Receita {
    id: number
    prontuarioId: number
    pacienteId: number
    profissionalUsuarioId: string
    estabelecimentoId: number
    tipo: TipoReceita
    emitidaEm: string
    validadeAte: string | null
    observacoes: string | null
    status: StatusReceita
    itens: ReceitaItem[]
    criadaEm: string
}

export interface PaginaReceitas {
    total: number
    pagina: number
    tamanho: number
    itens: Receita[]
}

export interface EmitirReceitaPayload {
    prontuarioId: number
    pacienteId: number
    tipo: TipoReceita
    observacoes?: string
    itens: Omit<ReceitaItem, "id">[]
}

export const receitaService = {
    async listarDoPaciente(
        pacienteId: number,
        pagina = 1,
        tamanho = 20,
    ): Promise<PaginaReceitas> {
        const { data } = await httpClient.get<PaginaReceitas>("/receitas", {
            params: { pacienteId, pagina, tamanho },
        })
        return data
    },

    async obter(id: number): Promise<Receita> {
        const { data } = await httpClient.get<Receita>(`/receitas/${id}`)
        return data
    },

    async emitir(payload: EmitirReceitaPayload): Promise<Receita> {
        const { data } = await httpClient.post<Receita>("/receitas", payload)
        return data
    },

    async cancelar(id: number, motivo: string): Promise<void> {
        await httpClient.post(`/receitas/${id}/cancelar`, { motivo })
    },

    async duplicar(id: number): Promise<Receita> {
        const { data } = await httpClient.post<Receita>(`/receitas/${id}/duplicar`)
        return data
    },

    async baixarPdf(id: number): Promise<Blob> {
        const { data } = await httpClient.get(`/receitas/${id}/pdf`, {
            responseType: "blob",
        })
        return data
    },
}

export default receitaService

// Constantes reusadas do legado
export const FORMAS_FARMACEUTICAS = [
    "Comprimido", "Capsula", "Dragea", "Solucao oral", "Suspensao oral",
    "Xarope", "Gotas", "Pomada", "Creme", "Gel", "Spray",
    "Injecao", "Supositorio", "Adesivo", "Colitrio",
]

export const VIAS_ADMINISTRACAO = [
    "Oral", "Sublingual", "Retal", "Vaginal",
    "Intramuscular (IM)", "Intravenosa (IV)", "Subcutanea (SC)",
    "Topica", "Inalatoria", "Oftalmica", "Otologica", "Nasal",
]

export const TIPOS_RECEITA: { valor: TipoReceita; label: string }[] = [
    { valor: "Comum",      label: "Comum" },
    { valor: "Controlada", label: "Controlada" },
    { valor: "Antibiotico",label: "Antibiotico" },
    { valor: "Especial",   label: "Especial" },
]
