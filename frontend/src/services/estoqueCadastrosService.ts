import httpClient from "./httpClient"

// ─── Tipos compartilhados ─────────────────────────────────────────────────

export interface Pagina<T> {
    itens: T[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export interface CategoriaEstoque {
    id: number
    nome: string
    cor: string
    icone: string
    ativo: boolean
    quantidadeItens: number
}

export interface FabricanteEstoque {
    id: number
    nome: string
    pais: string | null
    ativo: boolean
    quantidadeItens: number
}

export interface FornecedorEstoque {
    id: number
    razaoSocial: string
    nomeFantasia: string | null
    /** 14 dígitos sem formatação (formatar via utils/validateCnpj.formatarCnpj). */
    cnpj: string | null
    contatoNome: string | null
    contatoTelefone: string | null
    contatoEmail: string | null
    prazoEntregaDias: number
    ativo: boolean
    quantidadeItens: number
}

export type TipoLocalEstoque = "Armario" | "Gaveta" | "Refrigerado" | "Cofre" | "Estante" | "Sala"
export const TIPOS_LOCAL_ESTOQUE: TipoLocalEstoque[] = ["Armario", "Gaveta", "Refrigerado", "Cofre", "Estante", "Sala"]

export interface LocalEstoque {
    id: number
    nome: string
    tipo: TipoLocalEstoque
    andarSetor: string | null
    responsavel: string | null
    ativo: boolean
    quantidadeItens: number
}

// ─── Parâmetros de listagem (compartilhados) ──────────────────────────────

export interface ListarParams {
    busca?: string
    apenasAtivos?: boolean
    pagina?: number
    tamanho?: number
}

// ─── Payloads (input dos forms) ───────────────────────────────────────────

export interface CategoriaPayload {
    nome: string
    cor: string
    icone: string
}

export interface FabricantePayload {
    nome: string
    pais?: string | null
}

export interface FornecedorPayload {
    razaoSocial: string
    nomeFantasia?: string | null
    cnpj?: string | null
    contatoNome?: string | null
    contatoTelefone?: string | null
    contatoEmail?: string | null
    prazoEntregaDias: number
}

export interface LocalPayload {
    nome: string
    tipo: TipoLocalEstoque
    andarSetor?: string | null
    responsavel?: string | null
}

// ─── Service ──────────────────────────────────────────────────────────────

const base = "/inventario/cadastros"

function paramsLimpos(p?: ListarParams) {
    if (!p) return undefined
    return {
        busca: p.busca?.trim() || undefined,
        apenasAtivos: p.apenasAtivos,
        pagina: p.pagina,
        tamanho: p.tamanho,
    }
}

export const estoqueCadastrosService = {
    // ── Categorias ──
    categorias: {
        async listar(params?: ListarParams): Promise<Pagina<CategoriaEstoque>> {
            const { data } = await httpClient.get<Pagina<CategoriaEstoque>>(`${base}/categorias`, { params: paramsLimpos(params) })
            return data
        },
        async criar(payload: CategoriaPayload): Promise<{ id: number }> {
            const { data } = await httpClient.post<{ id: number }>(`${base}/categorias`, payload)
            return data
        },
        async atualizar(id: number, payload: CategoriaPayload): Promise<void> {
            await httpClient.put(`${base}/categorias/${id}`, payload)
        },
        async inativar(id: number): Promise<void> {
            await httpClient.post(`${base}/categorias/${id}/inativar`)
        },
        async reativar(id: number): Promise<void> {
            await httpClient.post(`${base}/categorias/${id}/reativar`)
        },
    },

    // ── Fabricantes ──
    fabricantes: {
        async listar(params?: ListarParams): Promise<Pagina<FabricanteEstoque>> {
            const { data } = await httpClient.get<Pagina<FabricanteEstoque>>(`${base}/fabricantes`, { params: paramsLimpos(params) })
            return data
        },
        async criar(payload: FabricantePayload): Promise<{ id: number }> {
            const { data } = await httpClient.post<{ id: number }>(`${base}/fabricantes`, payload)
            return data
        },
        async atualizar(id: number, payload: FabricantePayload): Promise<void> {
            await httpClient.put(`${base}/fabricantes/${id}`, payload)
        },
        async inativar(id: number): Promise<void> {
            await httpClient.post(`${base}/fabricantes/${id}/inativar`)
        },
        async reativar(id: number): Promise<void> {
            await httpClient.post(`${base}/fabricantes/${id}/reativar`)
        },
    },

    // ── Fornecedores ──
    fornecedores: {
        async listar(params?: ListarParams): Promise<Pagina<FornecedorEstoque>> {
            const { data } = await httpClient.get<Pagina<FornecedorEstoque>>(`${base}/fornecedores`, { params: paramsLimpos(params) })
            return data
        },
        async criar(payload: FornecedorPayload): Promise<{ id: number }> {
            const { data } = await httpClient.post<{ id: number }>(`${base}/fornecedores`, payload)
            return data
        },
        async atualizar(id: number, payload: FornecedorPayload): Promise<void> {
            await httpClient.put(`${base}/fornecedores/${id}`, payload)
        },
        async inativar(id: number): Promise<void> {
            await httpClient.post(`${base}/fornecedores/${id}/inativar`)
        },
        async reativar(id: number): Promise<void> {
            await httpClient.post(`${base}/fornecedores/${id}/reativar`)
        },
    },

    // ── Locais ──
    locais: {
        async listar(params?: ListarParams): Promise<Pagina<LocalEstoque>> {
            const { data } = await httpClient.get<Pagina<LocalEstoque>>(`${base}/locais`, { params: paramsLimpos(params) })
            return data
        },
        async criar(payload: LocalPayload): Promise<{ id: number }> {
            const { data } = await httpClient.post<{ id: number }>(`${base}/locais`, payload)
            return data
        },
        async atualizar(id: number, payload: LocalPayload): Promise<void> {
            await httpClient.put(`${base}/locais/${id}`, payload)
        },
        async inativar(id: number): Promise<void> {
            await httpClient.post(`${base}/locais/${id}/inativar`)
        },
        async reativar(id: number): Promise<void> {
            await httpClient.post(`${base}/locais/${id}/reativar`)
        },
    },
}

// ─── Constantes auxiliares ────────────────────────────────────────────────

/** Lista pré-aprovada de ícones FA disponíveis para categoria. */
export const ICONES_CATEGORIA: { valor: string; rotulo: string }[] = [
    { valor: "fa-pills", rotulo: "Medicamentos" },
    { valor: "fa-syringe", rotulo: "Injetáveis" },
    { valor: "fa-bandage", rotulo: "Curativos" },
    { valor: "fa-user-shield", rotulo: "EPIs" },
    { valor: "fa-spray-can-sparkles", rotulo: "Higiene" },
    { valor: "fa-paperclip", rotulo: "Escritório" },
    { valor: "fa-bone", rotulo: "Ortopedia" },
    { valor: "fa-shield-halved", rotulo: "Cirúrgico" },
    { valor: "fa-hand-dots", rotulo: "Materiais médicos" },
    { valor: "fa-flask", rotulo: "Laboratório" },
    { valor: "fa-stethoscope", rotulo: "Diagnóstico" },
    { valor: "fa-vials", rotulo: "Análises clínicas" },
    { valor: "fa-tag", rotulo: "Geral" },
]

/** Paleta HSL pré-aprovada — corresponde aos tokens do design system. */
export const CORES_CATEGORIA: { valor: string; rotulo: string }[] = [
    { valor: "hsl(218 70% 50%)", rotulo: "Azul" },
    { valor: "hsl(160 79% 39%)", rotulo: "Verde" },
    { valor: "hsl(40 90% 50%)",  rotulo: "Amarelo" },
    { valor: "hsl(0 70% 50%)",   rotulo: "Vermelho" },
    { valor: "hsl(280 60% 55%)", rotulo: "Roxo" },
    { valor: "hsl(200 75% 50%)", rotulo: "Ciano" },
    { valor: "hsl(20 80% 55%)",  rotulo: "Laranja" },
    { valor: "hsl(340 70% 50%)", rotulo: "Rosa" },
    { valor: "hsl(240 6% 45%)",  rotulo: "Cinza" },
]
