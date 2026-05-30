/**
 * Service HTTP para a seção de Estabelecimentos da área admin.
 *
 * Usa adminApi (axios isolado com cookie admin-access-token).
 * Sem importação de services ou stores do app principal.
 */
import adminApi from "./adminApi"

export interface EstabelecimentoAdminListaItemDto {
    id: number
    nomeFantasia: string
    razaoSocial: string
    cnpj: string
    status: string
    donoNome: string
    donoEmail: string
    /** CPF mascarado: "123.***.***-45" */
    donoCpfMascarado: string
    planoNome: string
    criadoEm: string
    totalProfissionaisAtivos: number
    totalPacientes: number
    agendamentosNoMes: number
}

export interface PaginaEstabelecimentosAdminDto {
    itens: EstabelecimentoAdminListaItemDto[]
    total: number
    pagina: number
    tamanhoPagina: number
}

export interface EstabelecimentoAdminDetalheDto {
    id: number
    nomeFantasia: string
    razaoSocial: string
    cnpj: string
    status: string
    telefone: string | null
    email: string | null
    cidade: string | null
    estado: string | null
    criadoEm: string
    donoUsuarioId: string
    donoNome: string
    donoEmail: string
    /** CPF mascarado por padrão. Revelar via revelarCpfDono(). */
    donoCpfMascarado: string
    planoNome: string
    assinaturaGratuita: boolean
    assinaturaDataFim: string | null
    totalProfissionaisAtivos: number
    totalPacientes: number
    agendamentosNoMes: number
    totalProntuarios: number
}

export interface CpfDonoReveladoDto {
    cpf: string
}

export interface ListarEstabelecimentosParams {
    busca?: string
    status?: string
    page?: number
    size?: number
}

export const estabelecimentosService = {
    async listar(params: ListarEstabelecimentosParams = {}): Promise<PaginaEstabelecimentosAdminDto> {
        const { data } = await adminApi.get<PaginaEstabelecimentosAdminDto>(
            "/estabelecimentos",
            { params: { busca: params.busca, status: params.status, page: params.page ?? 1, size: params.size ?? 25 } },
        )
        return data
    },

    async obter(id: number): Promise<EstabelecimentoAdminDetalheDto> {
        const { data } = await adminApi.get<EstabelecimentoAdminDetalheDto>(`/estabelecimentos/${id}`)
        return data
    },

    async revelarCpfDono(id: number, motivo: string): Promise<CpfDonoReveladoDto> {
        const { data } = await adminApi.post<CpfDonoReveladoDto>(
            `/estabelecimentos/${id}/revelar-cpf-dono`,
            { motivo },
        )
        return data
    },

    async resetTenant(id: number, motivo: string, confirmarNomeFantasia: string): Promise<void> {
        await adminApi.post(`/estabelecimentos/${id}/reset`, { motivo, confirmarNomeFantasia })
    },
}
