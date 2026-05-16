import httpClient from "./httpClient"

// ─────────────────────────────────────────────────────────────────────────────
// Catálogo de cirurgias (alias UI nova: "Procedimentos")
// ─────────────────────────────────────────────────────────────────────────────
export interface CatalogoCirurgia {
    id: number
    estabelecimentoId: number
    descricao: string
    valorBase: number
    duracaoPadraoMinutos: number | null
    /** Adicionados em 2026-05-16 (UI nova). Nullable em catálogos pré-existentes. */
    codigoInterno: string | null
    codigoTuss: string | null
    categoria: string | null
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
}

export interface CatalogoCirurgiaPayload {
    descricao: string
    valorBase: number
    duracaoPadraoMinutos: number | null
    codigoInterno?: string | null
    codigoTuss?: string | null
    categoria?: string | null
}

// ─────────────────────────────────────────────────────────────────────────────
// Valor profissional (legado)
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
// Configuração local (legado)
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
// Equipes especializadas (legado)
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
// Implantes (legado)
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
// Configuração pagamento (legado)
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
// Produtos
// ─────────────────────────────────────────────────────────────────────────────
export type TipoOrcamentoProduto = "Outros" | "OPME" | "Descartavel" | "Curativo"

export interface CatalogoProduto {
    id: number
    estabelecimentoId: number
    nome: string
    descricao: string | null
    valorReferencia: number | null
    usoUnico: boolean
    tipo: TipoOrcamentoProduto
    marca: string | null
    unidade: string
    fornecedorNome: string | null
    codigoSku: string | null
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
}

export interface CatalogoProdutoPayload {
    nome: string
    descricao: string | null
    valorReferencia: number | null
    usoUnico: boolean
    tipo?: TipoOrcamentoProduto
    marca?: string | null
    unidade?: string | null
    fornecedorNome?: string | null
    codigoSku?: string | null
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
    /** UI nova: somado ao total (true) ou cobrado à parte (false). Default true. */
    incluido: boolean
    criadaEm: string
}

export interface VincularProdutoPayload {
    produtoId: number
    quantidadePadrao: number
    obrigatorio: boolean
    incluido?: boolean
}

export interface AtualizarVinculoProdutoPayload {
    quantidadePadrao: number
    obrigatorio: boolean
    incluido?: boolean
}

// ─────────────────────────────────────────────────────────────────────────────
// Team Roles
// ─────────────────────────────────────────────────────────────────────────────
export type TipoHonorario = "Percentual" | "Fixo"

export interface OrcamentoTeamRole {
    id: number
    estabelecimentoId: number
    papel: string
    profissionalUsuarioId: string | null
    profissionalNome: string | null
    nomePadrao: string | null
    tipoHonorario: TipoHonorario
    valor: number
    baseCalculo: string
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
}

export interface TeamRolePayload {
    papel: string
    profissionalUsuarioId: string | null
    nomePadrao: string | null
    tipoHonorario: TipoHonorario
    valor: number
    baseCalculo: string
}

// ─────────────────────────────────────────────────────────────────────────────
// Anestesistas
// ─────────────────────────────────────────────────────────────────────────────
export interface FaixaAnestesista {
    id?: number
    descricao: string
    valor: number
    ordem?: number
}

/**
 * Detalhe completo do anestesista (GET /anestesistas/{id}). Inclui telefone (PII).
 */
export interface OrcamentoAnestesista {
    id: number
    estabelecimentoId: number
    profissionalUsuarioId: string | null
    nome: string
    crm: string | null
    especialidade: string | null
    telefone: string | null
    tabelaHonorarios: string | null
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
    faixas: FaixaAnestesista[]
}

/**
 * Versão de listagem (GET /anestesistas). LGPD: SEM telefone.
 * O drawer de edição faz GET /anestesistas/{id} para puxar o telefone.
 */
export interface OrcamentoAnestesistaLista {
    id: number
    estabelecimentoId: number
    profissionalUsuarioId: string | null
    nome: string
    crm: string | null
    especialidade: string | null
    tabelaHonorarios: string | null
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
    faixas: FaixaAnestesista[]
}

export interface AnestesistaPayload {
    nome: string
    profissionalUsuarioId: string | null
    crm: string | null
    especialidade: string | null
    telefone: string | null
    tabelaHonorarios: string | null
    faixas: { descricao: string; valor: number }[]
}

// ─────────────────────────────────────────────────────────────────────────────
// Pacotes
// ─────────────────────────────────────────────────────────────────────────────
export interface OrcamentoPacoteResumo {
    id: number
    estabelecimentoId: number
    nome: string
    descricao: string | null
    anestesistaId: number | null
    anestesistaNome: string | null
    valorTotalSugerido: number | null
    ativo: boolean
    totalProcedimentos: number
    totalProdutos: number
    totalTeamRoles: number
    criadaEm: string
    atualizadaEm: string | null
}

export interface OrcamentoPacoteDetalhe {
    id: number
    estabelecimentoId: number
    nome: string
    descricao: string | null
    anestesistaId: number | null
    anestesistaNome: string | null
    anestesistaAtivo: boolean | null
    valorTotalSugerido: number | null
    ativo: boolean
    criadaEm: string
    atualizadaEm: string | null
    procedimentos: { catalogoCirurgiaId: number; descricao: string; ordem: number }[]
    produtos: { catalogoProdutoId: number; nome: string; quantidade: number }[]
    teamRoles: { teamRoleId: number; papel: string }[]
}

export interface PacotePayload {
    nome: string
    descricao: string | null
    anestesistaId: number | null
    valorTotalSugerido: number | null
    procedimentoIds: number[]
    produtos: { produtoId: number; quantidade: number }[]
    teamRoleIds: number[]
}

const BASE = "/orcamentos/configuracoes"

export const orcamentoCatalogoService = {
    // ─── Cirurgias / Procedimentos (aliases) ───
    listarCirurgias(ativas?: boolean) {
        return httpClient.get<CatalogoCirurgia[]>(`${BASE}/cirurgias`, { params: { ativas } }).then(r => r.data)
    },
    listarProcedimentos(ativas?: boolean) {
        return httpClient.get<CatalogoCirurgia[]>(`${BASE}/procedimentos`, { params: { ativas } }).then(r => r.data)
    },
    criarCirurgia(payload: CatalogoCirurgiaPayload) {
        return httpClient.post<{ id: number }>(`${BASE}/cirurgias`, payload).then(r => r.data)
    },
    criarProcedimento(payload: CatalogoCirurgiaPayload) {
        return httpClient.post<{ id: number }>(`${BASE}/procedimentos`, payload).then(r => r.data)
    },
    atualizarCirurgia(id: number, payload: CatalogoCirurgiaPayload) {
        return httpClient.put(`${BASE}/cirurgias/${id}`, payload).then(() => undefined)
    },
    atualizarProcedimento(id: number, payload: CatalogoCirurgiaPayload) {
        return httpClient.put(`${BASE}/procedimentos/${id}`, payload).then(() => undefined)
    },
    removerCirurgia(id: number) {
        return httpClient.delete(`${BASE}/cirurgias/${id}`).then(() => undefined)
    },
    removerProcedimento(id: number) {
        return httpClient.delete(`${BASE}/procedimentos/${id}`).then(() => undefined)
    },

    // ─── Valor profissional ───
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

    // ─── Local cirurgia ───
    listarLocais() {
        return httpClient.get<ConfiguracaoLocalCirurgia[]>(`${BASE}/local-cirurgia`).then(r => r.data)
    },
    salvarLocal(tipo: string, payload: SalvarLocalPayload) {
        return httpClient.put<{ id: number }>(`${BASE}/local-cirurgia/${tipo}`, payload).then(r => r.data)
    },

    // ─── Equipes (legado) ───
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

    // ─── Implantes ───
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

    // ─── Produtos ───
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

    // ─── Vínculo cirurgia × produto ───
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

    // ─── Configuração pagamento ───
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

    // ─── Team Roles (novo) ───
    listarTeamRoles(ativos?: boolean) {
        return httpClient.get<OrcamentoTeamRole[]>(`${BASE}/team-roles`, { params: { ativos } }).then(r => r.data)
    },
    criarTeamRole(payload: TeamRolePayload) {
        return httpClient.post<{ id: number }>(`${BASE}/team-roles`, payload).then(r => r.data)
    },
    atualizarTeamRole(id: number, payload: TeamRolePayload) {
        return httpClient.put(`${BASE}/team-roles/${id}`, payload).then(() => undefined)
    },
    removerTeamRole(id: number) {
        return httpClient.delete(`${BASE}/team-roles/${id}`).then(() => undefined)
    },

    // ─── Anestesistas (novo) ───
    listarAnestesistas(ativos?: boolean) {
        return httpClient.get<OrcamentoAnestesistaLista[]>(`${BASE}/anestesistas`, { params: { ativos } }).then(r => r.data)
    },
    obterAnestesista(id: number) {
        return httpClient.get<OrcamentoAnestesista>(`${BASE}/anestesistas/${id}`).then(r => r.data)
    },
    criarAnestesista(payload: AnestesistaPayload) {
        return httpClient.post<{ id: number }>(`${BASE}/anestesistas`, payload).then(r => r.data)
    },
    atualizarAnestesista(id: number, payload: AnestesistaPayload) {
        return httpClient.put(`${BASE}/anestesistas/${id}`, payload).then(() => undefined)
    },
    removerAnestesista(id: number) {
        return httpClient.delete(`${BASE}/anestesistas/${id}`).then(() => undefined)
    },

    // ─── Pacotes (novo) ───
    listarPacotes(ativos?: boolean) {
        return httpClient.get<OrcamentoPacoteResumo[]>(`${BASE}/pacotes`, { params: { ativos } }).then(r => r.data)
    },
    obterPacote(id: number) {
        return httpClient.get<OrcamentoPacoteDetalhe>(`${BASE}/pacotes/${id}`).then(r => r.data)
    },
    criarPacote(payload: PacotePayload) {
        return httpClient.post<{ id: number }>(`${BASE}/pacotes`, payload).then(r => r.data)
    },
    atualizarPacote(id: number, payload: PacotePayload) {
        return httpClient.put(`${BASE}/pacotes/${id}`, payload).then(() => undefined)
    },
    removerPacote(id: number) {
        return httpClient.delete(`${BASE}/pacotes/${id}`).then(() => undefined)
    },
}

export default orcamentoCatalogoService
