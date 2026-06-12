import httpClient from "./httpClient"
import type { CategoriaTermo } from "./termoModeloService"

/**
 * Service de termos emitidos para um paciente.
 * Espelho 1:1 dos endpoints `/api/pacientes/{pacienteId}/termos/*` e
 * `/api/termos/{id}/*` (PacienteTermoController).
 *
 * Permissões (espelho do back — UX defense-in-depth):
 *   - Listar/Obter/Emitir/Anexar PDF/Baixar PDF: `termos.emitir`
 *   - Revogar: `termos.gerenciar_modelos`
 *
 * Multi-tenant: o backend extrai `EstabelecimentoId` do tenant claim — o front
 * não envia esse id. Listagem e detalhe são auditados server-side.
 *
 * Briefing 2026-06-12_002: AceiteLink removido. O único fluxo é PdfAnexado.
 * `anexarPdf` passa a aceitar 1-2 arquivos (JPG/PNG ou PDF).
 */

/** Status persistido no banco. */
export type StatusTermoEmitido =
    | "Pendente"
    | "Assinado"
    | "Recusado"
    | "Revogado"
    | "Expirado"

/** Tipo de assinatura persistido. AceiteLink é legado (read-only). */
export type AssinaturaTipoTermo = "PdfAnexado" | "AceiteLink"

export interface TermoEmitidoResumo {
    id: number
    pacienteId: number
    estabelecimentoId: number
    termoModeloId: number
    termoModeloTitulo: string
    categoria: CategoriaTermo
    versaoModelo: number
    status: StatusTermoEmitido
    assinaturaTipo: AssinaturaTipoTermo
    assinadoEm: string | null
    tokenExpiraEm: string | null
    temPdf: boolean
    criadoEm: string
    evolucaoId: number | null
    emitidoPorUsuarioId: string
    emitidoPorNome: string | null
}

export interface TermoEmitidoDetalhe extends TermoEmitidoResumo {
    conteudoSnapshotHtml: string
    conteudoSnapshotTexto: string | null
    hashIntegridade: string
    ipAssinatura: string | null
    userAgentAssinatura: string | null
    revogadoEm: string | null
    revogadoMotivo: string | null
}

export interface EmitirTermoPayload {
    modeloId: number
    /**
     * Usuário-profissional cujo nome/conselho/especialidade resolve as variáveis
     * `{{profissional.*}}` no template. Opcional — quando ausente, fallback é
     * `___________`. Precisa ser profissional com vínculo Ativo no estabelecimento.
     */
    profissionalUsuarioId?: string | null
    /** Quando emitido dentro de uma evolução, vincula o termo à evolução (CA-C1). */
    evolucaoId?: number | null
}

export interface EmitirTermoResposta {
    termoEmitidoId: number
}

export const pacienteTermoService = {
    /**
     * Lista termos do paciente. `status` (opcional) usa o valor cru do banco
     * — "Pendente" | "Assinado" | "Recusado" | "Revogado" | "Expirado".
     */
    async listar(
        pacienteId: number,
        filtros: { status?: StatusTermoEmitido } = {},
    ): Promise<TermoEmitidoResumo[]> {
        const { data } = await httpClient.get<TermoEmitidoResumo[]>(
            `/pacientes/${pacienteId}/termos`,
            { params: { status: filtros.status || undefined } },
        )
        return data
    },

    async obter(pacienteId: number, termoId: number): Promise<TermoEmitidoDetalhe> {
        const { data } = await httpClient.get<TermoEmitidoDetalhe>(
            `/pacientes/${pacienteId}/termos/${termoId}`,
        )
        return data
    },

    /**
     * Emite um termo (snapshot HTML + hash). Header `Idempotency-Key` evita
     * duplo-clique. O backend retorna 422 se o modelo estiver inativo,
     * incompatível com o tenant ou se o paciente não existir.
     */
    async emitir(
        pacienteId: number,
        payload: EmitirTermoPayload,
    ): Promise<EmitirTermoResposta> {
        const { data } = await httpClient.post<EmitirTermoResposta>(
            `/pacientes/${pacienteId}/termos`,
            payload,
            { headers: { "Idempotency-Key": cryptoIdempotencyKey() } },
        )
        return data
    },

    /**
     * Anexa o documento assinado. Aceita:
     * - 1 PDF (application/pdf)
     * - 1 ou 2 imagens JPG/PNG (frente e verso opcionais)
     *
     * O backend converte imagens em PDF multi-página via QuestPDF.
     * Só funciona com status = Pendente. Após anexar, status vira "Assinado".
     */
    async anexarPdf(termoId: number, arquivos: File | File[]): Promise<void> {
        const lista = Array.isArray(arquivos) ? arquivos : [arquivos]
        const form = new FormData()
        for (const arq of lista) {
            form.append("arquivos", arq, arq.name)
        }
        await httpClient.post(`/termos/${termoId}/pdf`, form, {
            headers: {
                "Content-Type": "multipart/form-data",
                "Idempotency-Key": cryptoIdempotencyKey(),
            },
        })
    },

    /** Devolve uma URL S3 presigned (TTL ~5 min) para baixar o PDF anexado. */
    async obterUrlPdf(termoId: number): Promise<{ url: string; ttlSegundos: number }> {
        const { data } = await httpClient.get<{ url: string; ttlSegundos: number }>(
            `/termos/${termoId}/pdf`,
        )
        return data
    },

    /** Revoga termo assinado. `motivo` é obrigatório (10-500 chars no back). */
    async revogar(termoId: number, motivo: string): Promise<void> {
        await httpClient.post(`/termos/${termoId}/revogar`, { motivo })
    },

    /**
     * Briefing 2026-06-10_002 — Baixa o PDF probatório gerado pelo servidor
     * (snapshot da versão aceita + bloco de evidência + marca d'água por status).
     *
     * Usado apenas quando o termo NÃO tem PDF anexado manualmente (`temPdf = false`).
     * Quando `temPdf = true`, usar `obterUrlPdf` (presigned URL do anexo).
     */
    async baixarPdfGerado(termoId: number): Promise<void> {
        const response = await httpClient.get(`/termos/${termoId}/pdf-gerado`, {
            responseType: "blob",
        })
        const blob = new Blob([response.data as BlobPart], { type: "application/pdf" })
        const url = URL.createObjectURL(blob)
        const a = document.createElement("a")
        a.href = url
        a.download = `termo-${termoId}.pdf`
        document.body.appendChild(a)
        a.click()
        document.body.removeChild(a)
        setTimeout(() => URL.revokeObjectURL(url), 30_000)
    },
}

/**
 * Gera um id curto para o header `Idempotency-Key`. crypto.randomUUID() está
 * disponível em todos os browsers que o app suporta.
 */
function cryptoIdempotencyKey(): string {
    try {
        return crypto.randomUUID()
    } catch {
        return `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`
    }
}
