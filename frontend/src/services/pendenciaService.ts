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
            return null // só conclusão manual pelo painel nesta fase (CA66)
    }
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
}
