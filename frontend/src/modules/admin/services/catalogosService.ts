import adminApi from "./adminApi"

// ─── Modelos de prontuário padrão sistema ─────────────────────────────────────

export interface ModeloPadraoSistemaListaItemDto {
    id: number
    nome: string
    descricao: string | null
    ativo: boolean
    criadoEm: string
    atualizadoEm: string | null
}

export interface ModeloPadraoSistemaDetalheDto extends ModeloPadraoSistemaListaItemDto {
    estruturaJson: string
}

export interface ListaPaginada<T> {
    itens: T[]
    total: number
    pagina: number
    tamanhoPagina: number
}

// ─── Variáveis pool padrão sistema ──────────────────────────────────────────

export interface VariavelPadraoSistemaListaItemDto {
    id: number
    nome: string
    tipo: string
    ativo: boolean
    criadoEm: string
    atualizadoEm: string | null
}

export interface VariavelPadraoSistemaDetalheDto extends VariavelPadraoSistemaListaItemDto {
    // sem campos extras além da lista — backend retorna o mesmo
}

export const TIPOS_VARIAVEL_POOL: Record<string, string> = {
    Alergia: "Alergia",
    Medicamento: "Medicamento",
    Doenca: "Doença",
    Cirurgia: "Cirurgia",
    Droga: "Droga",
    RelacaoFamiliar: "Relação familiar",
    Expectativa: "Expectativa",
    AtividadeFisica: "Atividade física",
}

// ─── Regiões anatômicas (árvore) ─────────────────────────────────────────────

export interface RegiaoAnatomicaNoDto {
    id: number
    codigo: string
    nome: string
    paiCodigo: string | null
    nivel: number
    vista: string | null
    templateTexto: string | null
    ordem: number
    lateralidade: boolean
    ativo: boolean
    filhos: RegiaoAnatomicaNoDto[]
}

// ─── Service: Modelos ────────────────────────────────────────────────────────

export const modelosGlobaisService = {
    async listar(params: { incluirInativos?: boolean; busca?: string; pagina?: number; tamanhoPagina?: number } = {}): Promise<ListaPaginada<ModeloPadraoSistemaListaItemDto>> {
        const { data } = await adminApi.get("/catalogos/modelos-prontuario", { params })
        return data
    },
    async obter(id: number): Promise<ModeloPadraoSistemaDetalheDto> {
        const { data } = await adminApi.get(`/catalogos/modelos-prontuario/${id}`)
        return data
    },
    async criar(payload: { nome: string; descricao: string | null; estruturaJson: string; motivo: string }): Promise<{ id: number }> {
        const { data } = await adminApi.post("/catalogos/modelos-prontuario", payload)
        return data
    },
    async atualizar(id: number, payload: { nome: string; descricao: string | null; estruturaJson: string; motivo: string }): Promise<void> {
        await adminApi.put(`/catalogos/modelos-prontuario/${id}`, payload)
    },
    async inativar(id: number, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/modelos-prontuario/${id}/inativar`, { motivo })
    },
    async reativar(id: number, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/modelos-prontuario/${id}/reativar`, { motivo })
    },
}

// ─── Service: Variáveis ──────────────────────────────────────────────────────

export const variaveisGlobaisService = {
    async listar(params: { incluirInativos?: boolean; busca?: string; categoria?: string; pagina?: number; tamanhoPagina?: number } = {}): Promise<ListaPaginada<VariavelPadraoSistemaListaItemDto>> {
        const { data } = await adminApi.get("/catalogos/variaveis-pool", { params })
        return data
    },
    async obter(id: number): Promise<VariavelPadraoSistemaDetalheDto> {
        const { data } = await adminApi.get(`/catalogos/variaveis-pool/${id}`)
        return data
    },
    async criar(payload: { nome: string; tipo: string; motivo: string }): Promise<{ id: number }> {
        const { data } = await adminApi.post("/catalogos/variaveis-pool", payload)
        return data
    },
    async atualizar(id: number, payload: { nome: string; tipo: string; motivo: string }): Promise<void> {
        await adminApi.put(`/catalogos/variaveis-pool/${id}`, payload)
    },
    async inativar(id: number, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/variaveis-pool/${id}/inativar`, { motivo })
    },
    async reativar(id: number, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/variaveis-pool/${id}/reativar`, { motivo })
    },
}

// ─── Service: Regiões ────────────────────────────────────────────────────────

export const regioesGlobaisService = {
    async listarArvore(incluirInativas = false): Promise<RegiaoAnatomicaNoDto[]> {
        const { data } = await adminApi.get("/catalogos/regioes-anatomicas", { params: { incluirInativas } })
        return data
    },
    async obter(id: number): Promise<RegiaoAnatomicaNoDto> {
        const { data } = await adminApi.get(`/catalogos/regioes-anatomicas/${id}`)
        return data
    },
    async criar(payload: {
        codigo: string
        nome: string
        paiCodigo: string | null
        nivel: number
        vista: string | null
        templateTexto: string | null
        ordem: number
        lateralidade: boolean
        motivo: string
    }): Promise<{ id: number }> {
        const { data } = await adminApi.post("/catalogos/regioes-anatomicas", payload)
        return data
    },
    async atualizar(id: number, payload: { nome: string; templateTexto: string | null; motivo: string }): Promise<void> {
        await adminApi.put(`/catalogos/regioes-anatomicas/${id}`, payload)
    },
    async inativar(id: number, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/regioes-anatomicas/${id}/inativar`, { motivo })
    },
    async excluir(id: number, motivo: string): Promise<void> {
        await adminApi.delete(`/catalogos/regioes-anatomicas/${id}`, { data: { motivo } })
    },
}
