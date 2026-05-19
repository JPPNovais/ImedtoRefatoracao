import httpClient from "./httpClient"

/**
 * Status do orçamento — alinhado com o legado e o backend novo.
 * Rascunho (inicial) → Enviado → Aprovado | Recusado | Cancelado.
 * Expirado é definido pela validade vencida (job/scheduler).
 */
export type OrcamentoStatus =
    | "Rascunho"
    | "Enviado"
    | "Aprovado"
    | "Recusado"
    | "Cancelado"
    | "Expirado"

export interface ItemOrcamento {
    id: number
    descricao: string
    quantidade: number
    valorUnitario: number
    descontoPercent: number
    subtotal: number
}

export interface OrcamentoEquipe {
    id?: number
    profissionalUsuarioId: string
    profissionalNome?: string
    papel: string
    valor: number
    ordem?: number
}

export interface OrcamentoImplante {
    id?: number
    itemInventarioId?: number | null
    descricao: string
    quantidade: number
    custoUnitario: number
    custoTotal: number
}

export interface OrcamentoFormaPagamento {
    id?: number
    formaPagamentoId: number
    formaPagamentoNome?: string
    valor: number
    parcelas: number
    acrescimoPercentual: number
    entradaPercentual: number
    observacao?: string | null
    ordem?: number
}

export interface OrcamentoCirurgia {
    id?: number
    procedimentoCirurgicoId?: number | null
    descricao?: string | null
    quantidade: number
    duracaoMinutos?: number | null
    valorTotal: number
    ordem?: number
}

/**
 * Local cirúrgico do orçamento (substitui a antiga "Internação"). Os 5 tipos vêm do legado:
 * - `IntLocal`, `IntPeridural`, `IntGeral` calculam o valor por tempo × tabela do estabelecimento;
 * - `SemInternacao`, `Ambulatorio` têm valor fixo.
 */
export type TipoLocalCirurgia =
    | "IntLocal"
    | "IntPeridural"
    | "IntGeral"
    | "SemInternacao"
    | "Ambulatorio"

export interface OrcamentoLocalCirurgia {
    tipo: TipoLocalCirurgia
    tempoMinutos: number
    /** Valor calculado server-side (snapshot). Frontend não tenta calcular sozinho. */
    valor: number
}

export interface OrcamentoAnestesia {
    tipoAnestesia: string
    valor: number
    observacao?: string | null
}

export interface OrcamentoResumo {
    id: number
    estabelecimentoId: number
    pacienteId: number
    pacienteNome: string
    numero: string
    titulo: string | null
    status: OrcamentoStatus
    validade: string
    total: number
    criadoPorNome: string
    criadoEm: string
    atualizadoEm: string | null
    agendamentoId: number | null
}

/**
 * Detalhe completo do orçamento (aggregate único). Não há mais distinção
 * "resumo simples vs completo" — todas as collections vêm sempre.
 */
export interface Orcamento extends OrcamentoResumo {
    observacoes: string | null
    procedimentoCirurgicoId: number | null
    custoImplantesTotal: number
    itens: ItemOrcamento[]
    equipe: OrcamentoEquipe[]
    implantes: OrcamentoImplante[]
    formasPagamento: OrcamentoFormaPagamento[]
    cirurgias: OrcamentoCirurgia[]
    localCirurgia: OrcamentoLocalCirurgia | null
    anestesia: OrcamentoAnestesia | null
}

export interface CriarOrcamentoPayload {
    pacienteId: number
    validade: string
    observacoes?: string | null
    titulo?: string | null
    procedimentoCirurgicoId?: number | null
    agendamentoId?: number | null
    itens?: Array<{
        descricao: string
        quantidade: number
        valorUnitario: number
        descontoPercent: number
    }>
    equipe?: Array<Pick<OrcamentoEquipe, "profissionalUsuarioId" | "papel" | "valor">>
    implantes?: Array<Pick<OrcamentoImplante, "itemInventarioId" | "descricao" | "quantidade" | "custoUnitario">>
    formasPagamento?: Array<Pick<OrcamentoFormaPagamento,
        "formaPagamentoId" | "valor" | "parcelas" | "acrescimoPercentual" | "entradaPercentual" | "observacao">>
    cirurgias?: Array<Pick<OrcamentoCirurgia,
        "procedimentoCirurgicoId" | "descricao" | "quantidade" | "duracaoMinutos" | "valorTotal">>
    /** Local cirúrgico — backend calcula o valor a partir da config do estabelecimento. */
    localCirurgia?: { tipo: TipoLocalCirurgia; tempoMinutos: number } | null
    anestesia?: { tipo: string; valor: number; observacao?: string | null } | null
}

export type AtualizarOrcamentoPayload = Omit<CriarOrcamentoPayload, "pacienteId">

// ─────────────────────────── Preview (Fase 6.3)
export interface FormaPagamentoCalculada {
    formaPagamentoId: number
    formaPagamentoNome: string | null
    valor: number
    parcelas: number
    acrescimoPercentual: number
    entradaPercentual: number
    totalBruto: number
    entrada: number
    valorParcela: number
}

export interface PreviewOrcamento {
    totalCirurgias: number
    totalEquipe: number
    totalImplantes: number
    /** Valor calculado do local cirúrgico (substitui o antigo totalInternacao). */
    totalLocal: number
    totalAnestesia: number
    totalItens: number
    totalGeral: number
    somaFormas: number
    diferenca: number
    integridadeOk: boolean
    formas: FormaPagamentoCalculada[]
    equipes: EquipeCalculada[]
}

export interface EquipeCalculada {
    valorProfissionalId: number
    tempoMinutos: number
    quantidade: number
    valorUnitario: number
    valorTotal: number
}

/**
 * Payload do preview — mesma estrutura da criação/atualização, mas só campos
 * que influem no cálculo (sem paciente/validade/observações). Inclui também
 * a variante `equipeComCatalogo` que pede ao backend para calcular honorário
 * por tempo a partir da tabela do catálogo.
 */
export type PreviewOrcamentoPayload = Pick<CriarOrcamentoPayload,
    "itens" | "equipe" | "implantes" | "formasPagamento" | "cirurgias" | "localCirurgia" | "anestesia"> & {
    equipeComCatalogo?: Array<{ valorProfissionalId: number; quantidade: number; tempoMinutos: number }>
}

export const orcamentoService = {
    async listar(params?: {
        pacienteId?: number
        status?: OrcamentoStatus
    }): Promise<OrcamentoResumo[]> {
        const { data } = await httpClient.get<OrcamentoResumo[]>("/orcamentos", { params })
        return data
    },

    async obter(id: number): Promise<Orcamento> {
        const { data } = await httpClient.get<Orcamento>(`/orcamentos/${id}`)
        return data
    },

    async criar(payload: CriarOrcamentoPayload): Promise<{ orcamentoId: number }> {
        const { data } = await httpClient.post<{ orcamentoId: number }>("/orcamentos", payload)
        return data
    },

    async atualizar(id: number, payload: AtualizarOrcamentoPayload): Promise<void> {
        await httpClient.put(`/orcamentos/${id}`, payload)
    },

    async enviar(id: number): Promise<void> {
        await httpClient.post(`/orcamentos/${id}/enviar`)
    },

    async aprovar(id: number): Promise<void> {
        await httpClient.post(`/orcamentos/${id}/aprovar`)
    },

    async recusar(id: number): Promise<void> {
        await httpClient.post(`/orcamentos/${id}/recusar`)
    },

    async cancelar(id: number): Promise<void> {
        await httpClient.post(`/orcamentos/${id}/cancelar`)
    },

    async converterEmCirurgia(id: number, dataAgendada?: string | null): Promise<{ procedimentoCirurgicoId: number }> {
        const { data } = await httpClient.post<{ procedimentoCirurgicoId: number }>(
            `/orcamentos/${id}/converter-em-cirurgia`,
            { dataAgendada: dataAgendada ?? null },
        )
        return data
    },

    /**
     * Preview dos cálculos sem persistir. Usar com debounce — frontend chama
     * a cada edição relevante do form de orçamento.
     */
    async preview(payload: PreviewOrcamentoPayload): Promise<PreviewOrcamento> {
        const { data } = await httpClient.post<PreviewOrcamento>("/orcamentos/preview", payload)
        return data
    },

    /**
     * Consolida produtos das cirurgias selecionadas — devolve a lista pronta para
     * exibição na tabela "Produtos das cirurgias" do form. Backend aplica MAX (uso único)
     * ou SOMA (não único) entre cirurgias.
     */
    async consolidarProdutos(cirurgias: Array<{ catalogoCirurgiaId: number; quantidade: number }>):
        Promise<ProdutoConsolidado[]>
    {
        const { data } = await httpClient.post<ProdutoConsolidado[]>(
            "/orcamentos/consolidar-produtos",
            { cirurgias })
        return data
    },

    /**
     * Retorna o orçamento ativo vinculado a um agendamento, ou null quando não há.
     * Usado pela ficha do agendamento para alternar entre "Criar orçamento" e "Ver existente".
     */
    async obterPorAgendamento(agendamentoId: number): Promise<OrcamentoResumo | null> {
        const r = await httpClient.get<OrcamentoResumo>(
            `/orcamentos/por-agendamento/${agendamentoId}`,
            // 204 vira data === "" — convertemos para null abaixo.
            { validateStatus: (s) => s === 200 || s === 204 })
        return r.status === 204 ? null : r.data
    },
}

export interface ProdutoConsolidado {
    produtoId: number
    produtoNome: string
    quantidade: number
    valorUnitario: number
    subtotal: number
    usoUnico: boolean
    origemCirurgiaIds: number[]
    origemCirurgiaNomes: string[]
}

export default orcamentoService
