import httpClient from "./httpClient"

/**
 * Receitas/prescrições. Backend é fonte de verdade — toda regra (validade,
 * tipo notificação, retenção, finalização) vive no aggregate Receita. Frontend
 * consome paginação server-side.
 *
 * Fluxo: novo rascunho → autosave (itens + observações) → finalizar → PDF.
 * Cancelar/duplicar disponíveis após finalização.
 */

export type TipoReceita = "Comum" | "Controlada" | "Antibiotico" | "Especial"
export type TipoNotificacao = "A" | "B" | "C" | "Especial"
export type StatusReceita = "Rascunho" | "Emitida" | "Cancelada" | "Substituida"
export type StatusAssinatura = "NaoAssinada" | "AssinadaIcp" | "AssinadaMemed"

export interface ItemReceita {
    id?: number
    ordem?: number
    medicamento: string
    posologia: string
    quantidade?: string | null
    via?: string | null
    observacao?: string | null
    concentracao?: string | null
    formaFarmaceutica?: string | null
    duracao?: string | null
}

export interface ReceitaResumo {
    id: number
    pacienteId: number
    prontuarioId: number
    tipo: TipoReceita
    tipoNotificacao: TipoNotificacao | null
    status: StatusReceita
    emitidaEm: string | null
    validadeAte: string | null
    requerRetencao: boolean
    quantidadeItens: number
    profissionalNome: string | null
}

export interface Receita {
    id: number
    prontuarioId: number
    pacienteId: number
    estabelecimentoId: number
    profissionalUsuarioId: string
    profissionalNome: string | null
    tipo: TipoReceita
    tipoNotificacao: TipoNotificacao | null
    status: StatusReceita
    emitidaEm: string | null
    validadeAte: string | null
    requerRetencao: boolean
    assinaturaDigitalStatus: StatusAssinatura
    observacoes: string | null
    canceladaEm: string | null
    motivoCancelamento: string | null
    itens: ItemReceita[]
}

export interface PaginaReceitas {
    itens: ReceitaResumo[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export interface IniciarRascunhoInput {
    pacienteId: number
    tipo: TipoReceita
    tipoNotificacao?: TipoNotificacao | null
    validadeAte?: string | null
    observacoes?: string | null
    itens?: Omit<ItemReceita, "id" | "ordem">[]
}

export interface AtualizarRascunhoInput {
    observacoes?: string | null
    itens: Omit<ItemReceita, "id" | "ordem">[]
}

export const receitaService = {
    async listarDoPaciente(
        pacienteId: number,
        params: { pagina?: number; tamanho?: number } = {},
    ): Promise<PaginaReceitas> {
        const { data } = await httpClient.get<PaginaReceitas>(
            `/pacientes/${pacienteId}/receitas`,
            { params: { pagina: params.pagina ?? 1, tamanho: params.tamanho ?? 10 } },
        )
        return data
    },

    async obter(id: number): Promise<Receita> {
        const { data } = await httpClient.get<Receita>(`/receitas/${id}`)
        return data
    },

    async iniciarRascunho(input: IniciarRascunhoInput): Promise<{ receitaId: number }> {
        const { data } = await httpClient.post<{ receitaId: number }>(
            "/receitas/rascunho",
            input,
        )
        return data
    },

    async atualizarRascunho(id: number, input: AtualizarRascunhoInput): Promise<void> {
        await httpClient.put(`/receitas/${id}/rascunho`, input)
    },

    async finalizar(id: number): Promise<void> {
        await httpClient.post(`/receitas/${id}/finalizar`)
    },

    async cancelar(id: number, motivo: string): Promise<void> {
        await httpClient.post(`/receitas/${id}/cancelar`, { motivo })
    },

    async duplicar(id: number): Promise<{ receitaId: number }> {
        const { data } = await httpClient.post<{ receitaId: number }>(
            `/receitas/${id}/duplicar`,
        )
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

// ─── Constantes de UI ────────────────────────────────────────────────────────

export const FORMAS_FARMACEUTICAS = [
    "Comprimido", "Cápsula", "Drágea", "Solução oral", "Suspensão oral",
    "Xarope", "Gotas", "Pomada", "Creme", "Gel", "Spray",
    "Injeção", "Supositório", "Adesivo", "Colírio",
]

export const VIAS_ADMINISTRACAO: { valor: string; label: string }[] = [
    { valor: "Oral",       label: "Oral" },
    { valor: "Sublingual", label: "Sublingual" },
    { valor: "Retal",      label: "Retal" },
    { valor: "Vaginal",    label: "Vaginal" },
    { valor: "IM",         label: "Intramuscular (IM)" },
    { valor: "EV",         label: "Endovenosa (EV)" },
    { valor: "SC",         label: "Subcutânea (SC)" },
    { valor: "Topica",     label: "Tópica" },
    { valor: "Inalatoria", label: "Inalatória" },
    { valor: "Oftalmica",  label: "Oftálmica" },
    { valor: "Otologica",  label: "Otológica" },
    { valor: "Nasal",      label: "Nasal" },
    { valor: "Outra",      label: "Outra" },
]

export const TIPOS_RECEITA: { valor: TipoReceita; label: string }[] = [
    { valor: "Comum",       label: "Comum" },
    { valor: "Controlada",  label: "Controlada" },
    { valor: "Antibiotico", label: "Antibiótico" },
    { valor: "Especial",    label: "Especial" },
]

export const TIPOS_NOTIFICACAO: { valor: TipoNotificacao; label: string }[] = [
    { valor: "A",        label: "A (amarela — entorpecentes)" },
    { valor: "B",        label: "B (azul — psicotrópicos)" },
    { valor: "C",        label: "C (branca — controle especial)" },
    { valor: "Especial", label: "Especial (anabolizantes/C5)" },
]
