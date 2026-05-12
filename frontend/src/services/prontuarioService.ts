import httpClient from "./httpClient"

export interface ModeloProntuario {
    id: number
    estabelecimentoId: number | null
    nome: string
    descricao: string | null
    estrutura: SecaoModelo[]
    ehPadraoSistema: boolean
    ativo: boolean
}

export interface SecaoModelo {
    chave: string
    titulo: string
    tipo: "texto" | "texto_longo" | string
    ordem: number
}

export interface ProntuarioResumo {
    id: number
    pacienteId: number
    estabelecimentoId: number
    modeloDeProntuarioId: number
    modeloNome: string
    modeloEstrutura: SecaoModelo[]
    criadoEm: string
}

export interface Evolucao {
    id: number
    prontuarioId: number
    autorUsuarioId: string
    autorNome: string
    modeloNome: string | null
    conteudo: Record<string, unknown>
    modeloSnapshot: SecaoModelo[]
    modeloDeProntuarioIdOrigem: number
    criadaEm: string
}

export interface ProntuarioCompleto {
    prontuario: ProntuarioResumo
    evolucoes: Evolucao[]
}

export interface Anexo {
    id: number
    prontuarioId: number
    evolucaoId: number | null
    nomeOriginal: string
    mimeType: string
    tamanhoBytes: number
    criadoEm: string
    autorNome: string | null
}

export interface AnexoUrl {
    id: number
    nomeOriginal: string
    mimeType: string
    url: string
    expiraEm: string
}

export const prontuarioService = {
    async listarModelos(): Promise<ModeloProntuario[]> {
        const { data } = await httpClient.get<ModeloProntuario[]>("/prontuario/modelos")
        return data
    },

    async criarModelo(payload: { nome: string; descricao?: string; estruturaJson: string }): Promise<void> {
        await httpClient.post("/prontuario/modelos", payload)
    },

    async atualizarModelo(id: number, payload: { nome: string; descricao?: string; estruturaJson: string }): Promise<void> {
        await httpClient.put(`/prontuario/modelos/${id}`, payload)
    },

    async excluirModelo(id: number): Promise<void> {
        await httpClient.delete(`/prontuario/modelos/${id}`)
    },

    async obter(pacienteId: number): Promise<ProntuarioCompleto | null> {
        // Backend retorna 200 com body null quando o prontuário ainda não foi iniciado —
        // o front exibe o CTA "Iniciar prontuário" nesse caso. Sem a transição de 404 → 200,
        // o browser logava "Failed to load resource" no console em toda navegação.
        const { data } = await httpClient.get<ProntuarioCompleto | null>(`/paciente/${pacienteId}/prontuario`)
        return data ?? null
    },

    async contarEvolucoes(pacienteId: number): Promise<number> {
        const { data } = await httpClient.get<{ total: number }>(
            `/paciente/${pacienteId}/prontuario/contagem-evolucoes`,
        )
        return data.total
    },

    async iniciar(pacienteId: number, modeloDeProntuarioId: number): Promise<void> {
        await httpClient.post(`/paciente/${pacienteId}/prontuario`, { modeloDeProntuarioId })
    },

    async registrarEvolucao(
        pacienteId: number,
        conteudo: Record<string, unknown>,
        modeloDeProntuarioId?: number,
    ): Promise<void> {
        await httpClient.post(`/paciente/${pacienteId}/prontuario/evolucoes`, {
            conteudoJson: JSON.stringify(conteudo),
            modeloDeProntuarioId: modeloDeProntuarioId ?? null,
        })
    },

    async listarAnexos(pacienteId: number, evolucaoId?: number): Promise<Anexo[]> {
        const { data } = await httpClient.get<Anexo[]>(
            `/paciente/${pacienteId}/prontuario/anexos`,
            { params: { evolucaoId } },
        )
        return data
    },

    async uploadAnexo(
        pacienteId: number,
        arquivo: File,
        evolucaoId?: number,
    ): Promise<{ anexoId: number }> {
        const form = new FormData()
        form.append("arquivo", arquivo)
        if (evolucaoId) form.append("evolucaoId", String(evolucaoId))

        const { data } = await httpClient.post<{ anexoId: number }>(
            `/paciente/${pacienteId}/prontuario/anexos`,
            form,
            { headers: { "Content-Type": "multipart/form-data" } },
        )
        return data
    },

    async obterUrlAnexo(pacienteId: number, anexoId: number): Promise<AnexoUrl> {
        const { data } = await httpClient.get<AnexoUrl>(
            `/paciente/${pacienteId}/prontuario/anexos/${anexoId}/url`,
        )
        return data
    },
}
