import httpClient from "./httpClient"

// ─────────────────────────────────────────────────────────────────────────────
// Catálogo de cirurgias
// ─────────────────────────────────────────────────────────────────────────────
export interface CatalogoCirurgia {
    id: number
    estabelecimentoId: number
    descricao: string
    valorBase: number
    duracaoPadraoMinutos: number | null
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
}

export interface CatalogoCirurgiaPayload {
    descricao: string
    valorBase: number
    duracaoPadraoMinutos: number | null
}

// ─────────────────────────────────────────────────────────────────────────────
// Valor profissional
// ─────────────────────────────────────────────────────────────────────────────
export interface ValorProfissionalOrcamentoCatalogo {
    id: number
    estabelecimentoId: number
    profissionalUsuarioId: string | null
    profissionalNome: string | null
    funcao: string
    tempoBaseMinutos: number
    valorTempoBase: number
    tempoAdicionalMinutos: number
    valorAdicional: number
    valorPlus: number
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
}

export interface CriarValorProfissionalPayload {
    profissionalUsuarioId: string | null
    funcao: string
    tempoBaseMinutos: number
    valorTempoBase: number
    tempoAdicionalMinutos: number
    valorAdicional: number
    valorPlus: number
}

export interface AtualizarValorProfissionalPayload {
    funcao: string
    tempoBaseMinutos: number
    valorTempoBase: number
    tempoAdicionalMinutos: number
    valorAdicional: number
    valorPlus: number
}

// ─────────────────────────────────────────────────────────────────────────────
// Configuração local de cirurgia (1 por tipo de internação)
// ─────────────────────────────────────────────────────────────────────────────
export interface ConfiguracaoLocalCirurgia {
    id: number
    estabelecimentoId: number
    tipoInternacao: string
    tempoBaseMinutos: number
    valorBase: number
    tempoAdicionalMinutos: number
    valorAdicional: number
    criadaEm: string
    atualizadaEm: string | null
}

export interface SalvarLocalPayload {
    tempoBaseMinutos: number
    valorBase: number
    tempoAdicionalMinutos: number
    valorAdicional: number
}

// ─────────────────────────────────────────────────────────────────────────────
// Equipes especializadas
// ─────────────────────────────────────────────────────────────────────────────
export interface CatalogoEquipe {
    id: number
    estabelecimentoId: number
    descricao: string
    valorPadrao: number
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
}

export interface CatalogoEquipePayload {
    descricao: string
    valorPadrao: number
}

// ─────────────────────────────────────────────────────────────────────────────
// Implantes
// ─────────────────────────────────────────────────────────────────────────────
export interface CatalogoImplante {
    id: number
    estabelecimentoId: number
    itemInventarioId: number | null
    itemInventarioNome: string | null
    descricao: string
    custoUnitario: number
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
}

export interface CatalogoImplantePayload {
    itemInventarioId: number | null
    descricao: string
    custoUnitario: number
}

// ─────────────────────────────────────────────────────────────────────────────
// Configuração de pagamento
// ─────────────────────────────────────────────────────────────────────────────
export interface ConfiguracaoPagamentoCatalogo {
    id: number
    estabelecimentoId: number
    formaPagamentoId: number
    formaPagamentoNome: string | null
    acrescimoPercentual: number
    entradaPercentualPadrao: number
    taxaParcela: number
    parcelasMaximas: number
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
}

export interface CriarConfigPagamentoPayload {
    formaPagamentoId: number
    acrescimoPercentual: number
    entradaPercentualPadrao: number
    taxaParcela: number
    parcelasMaximas: number
}

export interface AtualizarConfigPagamentoPayload {
    acrescimoPercentual: number
    entradaPercentualPadrao: number
    taxaParcela: number
    parcelasMaximas: number
}

// ─────────────────────────────────────────────────────────────────────────────
// Produtos (catálogo) + vínculo com cirurgias
// ─────────────────────────────────────────────────────────────────────────────
export interface CatalogoProduto {
    id: number
    estabelecimentoId: number
    nome: string
    descricao: string | null
    valorReferencia: number | null
    usoUnico: boolean
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
}

export interface CatalogoProdutoPayload {
    nome: string
    descricao: string | null
    valorReferencia: number | null
    usoUnico: boolean
}

export interface CatalogoCirurgiaProdutoVinculo {
    id: number
    catalogoCirurgiaId: number
    catalogoProdutoId: number
    produtoNome: string
    produtoUsoUnico: boolean
    produtoValorReferencia: number | null
    quantidadePadrao: number
    obrigatorio: boolean
    criadaEm: string
}

export interface VincularProdutoPayload {
    produtoId: number
    quantidadePadrao: number
    obrigatorio: boolean
}

export interface AtualizarVinculoProdutoPayload {
    quantidadePadrao: number
    obrigatorio: boolean
}

const BASE = "/orcamentos/configuracoes"

export const orcamentoCatalogoService = {
    // Cirurgias
    listarCirurgias(ativas?: boolean) {
        return httpClient.get<CatalogoCirurgia[]>(`${BASE}/cirurgias`, { params: { ativas } }).then(r => r.data)
    },
    criarCirurgia(payload: CatalogoCirurgiaPayload) {
        return httpClient.post<{ id: number }>(`${BASE}/cirurgias`, payload).then(r => r.data)
    },
    atualizarCirurgia(id: number, payload: CatalogoCirurgiaPayload) {
        return httpClient.put(`${BASE}/cirurgias/${id}`, payload).then(() => undefined)
    },
    removerCirurgia(id: number) {
        return httpClient.delete(`${BASE}/cirurgias/${id}`).then(() => undefined)
    },

    // Valor profissional
    listarValoresProfissional(ativos?: boolean) {
        return httpClient.get<ValorProfissionalOrcamentoCatalogo[]>(`${BASE}/valores-profissional`, { params: { ativos } }).then(r => r.data)
    },
    criarValorProfissional(payload: CriarValorProfissionalPayload) {
        return httpClient.post<{ id: number }>(`${BASE}/valores-profissional`, payload).then(r => r.data)
    },
    atualizarValorProfissional(id: number, payload: AtualizarValorProfissionalPayload) {
        return httpClient.put(`${BASE}/valores-profissional/${id}`, payload).then(() => undefined)
    },
    removerValorProfissional(id: number) {
        return httpClient.delete(`${BASE}/valores-profissional/${id}`).then(() => undefined)
    },

    // Local cirurgia
    listarLocais() {
        return httpClient.get<ConfiguracaoLocalCirurgia[]>(`${BASE}/local-cirurgia`).then(r => r.data)
    },
    salvarLocal(tipo: string, payload: SalvarLocalPayload) {
        return httpClient.put<{ id: number }>(`${BASE}/local-cirurgia/${tipo}`, payload).then(r => r.data)
    },

    // Equipes
    listarEquipes(ativas?: boolean) {
        return httpClient.get<CatalogoEquipe[]>(`${BASE}/equipes`, { params: { ativas } }).then(r => r.data)
    },
    criarEquipe(payload: CatalogoEquipePayload) {
        return httpClient.post<{ id: number }>(`${BASE}/equipes`, payload).then(r => r.data)
    },
    atualizarEquipe(id: number, payload: CatalogoEquipePayload) {
        return httpClient.put(`${BASE}/equipes/${id}`, payload).then(() => undefined)
    },
    removerEquipe(id: number) {
        return httpClient.delete(`${BASE}/equipes/${id}`).then(() => undefined)
    },

    // Implantes
    listarImplantes(ativos?: boolean) {
        return httpClient.get<CatalogoImplante[]>(`${BASE}/implantes`, { params: { ativos } }).then(r => r.data)
    },
    criarImplante(payload: CatalogoImplantePayload) {
        return httpClient.post<{ id: number }>(`${BASE}/implantes`, payload).then(r => r.data)
    },
    atualizarImplante(id: number, payload: CatalogoImplantePayload) {
        return httpClient.put(`${BASE}/implantes/${id}`, payload).then(() => undefined)
    },
    removerImplante(id: number) {
        return httpClient.delete(`${BASE}/implantes/${id}`).then(() => undefined)
    },

    // Produtos
    listarProdutos(ativos?: boolean) {
        return httpClient.get<CatalogoProduto[]>(`${BASE}/produtos`, { params: { ativos } }).then(r => r.data)
    },
    criarProduto(payload: CatalogoProdutoPayload) {
        return httpClient.post<{ id: number }>(`${BASE}/produtos`, payload).then(r => r.data)
    },
    atualizarProduto(id: number, payload: CatalogoProdutoPayload) {
        return httpClient.put(`${BASE}/produtos/${id}`, payload).then(() => undefined)
    },
    removerProduto(id: number) {
        return httpClient.delete(`${BASE}/produtos/${id}`).then(() => undefined)
    },

    // Vínculo cirurgia × produto
    listarProdutosDaCirurgia(cirurgiaId: number) {
        return httpClient.get<CatalogoCirurgiaProdutoVinculo[]>(`${BASE}/cirurgias/${cirurgiaId}/produtos`).then(r => r.data)
    },
    vincularProdutoCirurgia(cirurgiaId: number, payload: VincularProdutoPayload) {
        return httpClient.post<{ id: number }>(`${BASE}/cirurgias/${cirurgiaId}/produtos`, payload).then(r => r.data)
    },
    atualizarVinculoProduto(vinculoId: number, payload: AtualizarVinculoProdutoPayload) {
        return httpClient.put(`${BASE}/cirurgias/produtos/${vinculoId}`, payload).then(() => undefined)
    },
    desvincularProdutoCirurgia(vinculoId: number) {
        return httpClient.delete(`${BASE}/cirurgias/produtos/${vinculoId}`).then(() => undefined)
    },

    // Configuração pagamento
    listarConfigPagamento(ativas?: boolean) {
        return httpClient.get<ConfiguracaoPagamentoCatalogo[]>(`${BASE}/pagamento`, { params: { ativas } }).then(r => r.data)
    },
    criarConfigPagamento(payload: CriarConfigPagamentoPayload) {
        return httpClient.post<{ id: number }>(`${BASE}/pagamento`, payload).then(r => r.data)
    },
    atualizarConfigPagamento(id: number, payload: AtualizarConfigPagamentoPayload) {
        return httpClient.put(`${BASE}/pagamento/${id}`, payload).then(() => undefined)
    },
    removerConfigPagamento(id: number) {
        return httpClient.delete(`${BASE}/pagamento/${id}`).then(() => undefined)
    },
}

export default orcamentoCatalogoService
