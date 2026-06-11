import httpClient from "./httpClient"

// ── Tipos ─────────────────────────────────────────────────────────────────────

export type AcaoPendencia =
    | "CriarReceita"
    | "CriarAtestado"
    | "PedirExame"
    | "CriarOrcamento"
    | "MarcarProcedimentoRealizado"
    | "AgendarRetorno"

export interface PendenciaAberta {
    id: number
    evolucaoId: number
    acao: AcaoPendencia
    status: "Pendente"
    criadoEm: string
}

// ── Labels de UI ──────────────────────────────────────────────────────────────

export const ACAO_LABELS: Record<AcaoPendencia, string> = {
    CriarReceita: "Criar receita",
    CriarAtestado: "Criar atestado",
    PedirExame: "Pedir exame",
    CriarOrcamento: "Criar orçamento",
    MarcarProcedimentoRealizado: "Marcar procedimento como realizado",
    AgendarRetorno: "Agendar retorno",
}

/** Rota de destino para cada ação no contexto do paciente. */
export function rotaParaAcao(pacienteId: number, acao: AcaoPendencia): string | null {
    switch (acao) {
        case "CriarReceita":
            return `/pacientes/${pacienteId}?aba=documentos&tipo=Receita`
        case "CriarAtestado":
            return `/pacientes/${pacienteId}?aba=documentos&tipo=Atestado`
        case "PedirExame":
            return `/pacientes/${pacienteId}?aba=documentos&tipo=PedidoExame`
        case "CriarOrcamento":
            return `/pacientes/${pacienteId}?aba=orcamentos`
        case "AgendarRetorno":
            return `/agenda?pacienteId=${pacienteId}`
        case "MarcarProcedimentoRealizado":
            return null // abre modal de confirmação (F4/CA88) — tratado pelo componente
    }
}

// ── Preview modal MarcarProcedimentoRealizado (F4/CA88) ──────────────────────

export interface ProcedimentoPreviewItem {
    catalogoCirurgiaId: number
    descricao: string
    valor: number
    observacao: string | null
}

export interface ProdutoPreviewItem {
    produtoId: number
    produtoNome: string
    quantidade: number
    itemInventarioId: number | null
    itemInventarioNome: string | null
    /** True quando produto não tem item de estoque vinculado — sinalizar no modal (CA94). */
    semVinculo: boolean
}

export interface PreviewProcedimentoRealizado {
    pendenciaId: number
    evolucaoId: number
    procedimentos: ProcedimentoPreviewItem[]
    valorTotal: number
    produtosABaixar: ProdutoPreviewItem[]
    /** True se ao menos 1 produto não tem item de estoque vinculado. */
    temProdutoSemVinculo: boolean
}

// ── Service ───────────────────────────────────────────────────────────────────

export const pendenciaService = {
    /** Lista pendências abertas de um paciente. Alimenta o painel persistente (CA74). */
    async listarAbertas(pacienteId: number): Promise<PendenciaAberta[]> {
        const resp = await httpClient.get<PendenciaAberta[]>(`/api/paciente/${pacienteId}/pendencias`)
        return resp.data
    },

    /** Conclui manualmente uma pendência pelo painel (CA67). */
    async concluirManual(pacienteId: number, pendenciaId: number): Promise<void> {
        await httpClient.post(`/api/paciente/${pacienteId}/pendencias/${pendenciaId}/concluir`)
    },

    /** Carrega o preview para o modal MarcarProcedimentoRealizado (CA88). Leitura pura. */
    async previewProcedimentoRealizado(
        pacienteId: number,
        pendenciaId: number,
    ): Promise<PreviewProcedimentoRealizado> {
        const resp = await httpClient.get<PreviewProcedimentoRealizado>(
            `/api/paciente/${pacienteId}/pendencias/${pendenciaId}/preview-procedimento`,
        )
        return resp.data
    },

    /** Confirma a marcação do procedimento como realizado: cria cobrança + baixa estoque. */
    async marcarProcedimentoRealizado(
        pacienteId: number,
        pendenciaId: number,
    ): Promise<{ cobrancaId: number }> {
        const resp = await httpClient.post<{ cobrancaId: number }>(
            `/api/paciente/${pacienteId}/pendencias/${pendenciaId}/marcar-procedimento-realizado`,
        )
        return resp.data
    },
}
