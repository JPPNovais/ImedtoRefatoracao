import adminApi from "./adminApi"

// ─── Modelos de prontuário globais ───────────────────────────────────────────

export interface ModeloGlobalListaItemDto {
    id: string
    nome: string
    descricao: string | null
    ativo: boolean
    criadoEm: string
    atualizadoEm: string | null
}

export interface ModeloGlobalDetalheDto extends ModeloGlobalListaItemDto {
    conteudoJson: string
    criadoPorAdminId: string | null
    atualizadoPorAdminId: string | null
}

export interface ListaPaginada<T> {
    itens: T[]
    total: number
    pagina: number
    tamanhoPagina: number
}

// ─── Variáveis pool globais ──────────────────────────────────────────────────

export interface VariavelGlobalListaItemDto {
    id: string
    nome: string
    tipo: string
    descricao: string | null
    ativo: boolean
    criadoEm: string
    atualizadoEm: string | null
}

export interface VariavelGlobalDetalheDto extends VariavelGlobalListaItemDto {
    valoresJson: string | null
    criadoPorAdminId: string | null
    atualizadoPorAdminId: string | null
}

// ─── Regiões anatômicas globais ──────────────────────────────────────────────

export interface RegiaoGlobalListaItemDto {
    id: string
    nome: string
    sinonimos: string[] | null
    sistemaCorporal: string | null
    ativo: boolean
    criadoEm: string
    atualizadoEm: string | null
}

// ─── Service: Modelos ────────────────────────────────────────────────────────

export const modelosGlobaisService = {
    async listar(params: { incluirInativos?: boolean; busca?: string; pagina?: number; tamanhoPagina?: number } = {}): Promise<ListaPaginada<ModeloGlobalListaItemDto>> {
        const { data } = await adminApi.get("/catalogos/modelos-prontuario", { params })
        return data
    },
    async obter(id: string): Promise<ModeloGlobalDetalheDto> {
        const { data } = await adminApi.get(`/catalogos/modelos-prontuario/${id}`)
        return data
    },
    async criar(payload: { nome: string; descricao: string | null; conteudoJson: string; motivo: string }): Promise<{ id: string }> {
        const { data } = await adminApi.post("/catalogos/modelos-prontuario", payload)
        return data
    },
    async atualizar(id: string, payload: { nome: string; descricao: string | null; conteudoJson: string; motivo: string }): Promise<void> {
        await adminApi.put(`/catalogos/modelos-prontuario/${id}`, payload)
    },
    async desativar(id: string, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/modelos-prontuario/${id}/desativar`, { motivo })
    },
    async reativar(id: string, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/modelos-prontuario/${id}/reativar`, { motivo })
    },
}

// ─── Service: Variáveis ──────────────────────────────────────────────────────

export const variaveisGlobaisService = {
    async listar(params: { incluirInativos?: boolean; busca?: string; tipo?: string; pagina?: number; tamanhoPagina?: number } = {}): Promise<ListaPaginada<VariavelGlobalListaItemDto>> {
        const { data } = await adminApi.get("/catalogos/variaveis-pool", { params })
        return data
    },
    async obter(id: string): Promise<VariavelGlobalDetalheDto> {
        const { data } = await adminApi.get(`/catalogos/variaveis-pool/${id}`)
        return data
    },
    async criar(payload: { nome: string; tipo: string; valoresJson: string | null; descricao: string | null; motivo: string }): Promise<{ id: string }> {
        const { data } = await adminApi.post("/catalogos/variaveis-pool", payload)
        return data
    },
    async atualizar(id: string, payload: { nome: string; tipo: string; valoresJson: string | null; descricao: string | null; motivo: string }): Promise<void> {
        await adminApi.put(`/catalogos/variaveis-pool/${id}`, payload)
    },
    async desativar(id: string, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/variaveis-pool/${id}/desativar`, { motivo })
    },
    async reativar(id: string, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/variaveis-pool/${id}/reativar`, { motivo })
    },
}

// ─── Service: Regiões ────────────────────────────────────────────────────────

export const regioesGlobaisService = {
    async listar(params: { incluirInativos?: boolean; busca?: string; sistemaCorporal?: string; pagina?: number; tamanhoPagina?: number } = {}): Promise<ListaPaginada<RegiaoGlobalListaItemDto>> {
        const { data } = await adminApi.get("/catalogos/regioes-anatomicas", { params })
        return data
    },
    async obter(id: string): Promise<RegiaoGlobalListaItemDto> {
        const { data } = await adminApi.get(`/catalogos/regioes-anatomicas/${id}`)
        return data
    },
    async criar(payload: { nome: string; sinonimos: string[] | null; sistemaCorporal: string | null; motivo: string }): Promise<{ id: string }> {
        const { data } = await adminApi.post("/catalogos/regioes-anatomicas", payload)
        return data
    },
    async atualizar(id: string, payload: { nome: string; sinonimos: string[] | null; sistemaCorporal: string | null; motivo: string }): Promise<void> {
        await adminApi.put(`/catalogos/regioes-anatomicas/${id}`, payload)
    },
    async desativar(id: string, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/regioes-anatomicas/${id}/desativar`, { motivo })
    },
    async reativar(id: string, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/regioes-anatomicas/${id}/reativar`, { motivo })
    },
}
