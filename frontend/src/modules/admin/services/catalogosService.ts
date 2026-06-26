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

// Briefing 2026-06-05_001: Droga e AtividadeFisica removidos do pool.
// Expectativa permanece válida mas sem campo de prontuário nesta entrega.
export const TIPOS_VARIAVEL_POOL: Record<string, string> = {
    Alergia: "Alergia",
    Medicamento: "Medicamento",
    Doenca: "Doença",
    Cirurgia: "Cirurgia",
    RelacaoFamiliar: "Relação familiar",
    Expectativa: "Expectativa",
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

// ─── Modelos de permissão padrão sistema (briefing 2026-06-04_001) ──────────

export interface ModeloPermissaoPadraoListaItemDto {
    id: number
    nome: string
    tipoAcesso: string
    descricao: string | null
    icone: string | null
    cor: string | null
    criadoEm: string
    atualizadoEm: string | null
}

export interface ModeloPermissaoPadraoDetalheDto extends ModeloPermissaoPadraoListaItemDto {
    permissoes: string[]
    permissoesExtras: string[]
}

export const permissoesGlobaisService = {
    async listar(params: { busca?: string; pagina?: number; tamanhoPagina?: number } = {}): Promise<ListaPaginada<ModeloPermissaoPadraoListaItemDto>> {
        const { data } = await adminApi.get("/catalogos/permissoes", { params })
        return data
    },
    async obter(id: number): Promise<ModeloPermissaoPadraoDetalheDto> {
        const { data } = await adminApi.get(`/catalogos/permissoes/${id}`)
        return data
    },
    async criar(payload: {
        nome: string
        tipoAcesso: string
        permissoes: string[]
        icone: string | null
        cor: string | null
        descricao: string | null
    }): Promise<{ id: number }> {
        const { data } = await adminApi.post("/catalogos/permissoes", payload)
        return data
    },
    async atualizar(id: number, payload: {
        nome: string
        tipoAcesso: string
        permissoes: string[]
        icone: string | null
        cor: string | null
        descricao: string | null
    }): Promise<void> {
        await adminApi.put(`/catalogos/permissoes/${id}`, payload)
    },
    async excluir(id: number): Promise<void> {
        await adminApi.delete(`/catalogos/permissoes/${id}`)
    },
}

// ─── Categorias financeiras padrão sistema (briefing 2026-06-22_003) ────────

export type TipoCategoriaFinanceira = "Receita" | "Despesa"

export interface CategoriaFinanceiraPadraoListaItemDto {
    id: number
    nome: string
    tipo: TipoCategoriaFinanceira
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
}

export const categoriasFinanceirasGlobaisService = {
    async listar(params: {
        tipo?: TipoCategoriaFinanceira
        ativas?: boolean
        pagina?: number
        tamanhoPagina?: number
    } = {}): Promise<ListaPaginada<CategoriaFinanceiraPadraoListaItemDto>> {
        const { data } = await adminApi.get("/catalogos/categorias-financeiras", { params })
        return data
    },

    async criar(payload: { nome: string; tipo: TipoCategoriaFinanceira }): Promise<{ id: number; instanciasPropagadas?: number }> {
        const { data } = await adminApi.post("/catalogos/categorias-financeiras", payload)
        return data
    },

    async inativar(id: number): Promise<void> {
        await adminApi.post(`/catalogos/categorias-financeiras/${id}/inativar`)
    },

    async reativar(id: number): Promise<void> {
        await adminApi.post(`/catalogos/categorias-financeiras/${id}/reativar`)
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
    async reativar(id: number, motivo: string): Promise<void> {
        await adminApi.post(`/catalogos/regioes-anatomicas/${id}/reativar`, { motivo })
    },
    async excluir(id: number, motivo: string): Promise<void> {
        await adminApi.delete(`/catalogos/regioes-anatomicas/${id}`, { data: { motivo } })
    },
    async invalidarCache(): Promise<void> {
        await adminApi.post("/catalogos/regioes-anatomicas/invalidar-cache")
    },
}
