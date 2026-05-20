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
 */

/** Status persistido no banco (EF salva com PascalCase). */
export type StatusTermoEmitido =
    | "Pendente"
    | "Assinado"
    | "Recusado"
    | "Revogado"
    | "Expirado"

/** Tipo de assinatura persistido (EF salva com PascalCase). */
export type AssinaturaTipoTermo = "PdfAnexado" | "AceiteLink"

/** Payload aceito pelo POST de emissão (kebab/snake — convertido pelo backend). */
export type AssinaturaTipoEmitir = "pdf_anexado" | "aceite_link"

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
    assinaturaTipo: AssinaturaTipoEmitir
    /**
     * Canal de envio do link público — só lido pelo backend quando
     * `assinaturaTipo = "aceite_link"`:
     *  - "email" (default): dispara o e-mail "termo aguardando seu aceite".
     *  - "copia": suprime o e-mail; o front exibe o link pra emissor copiar.
     * Ignorado em `pdf_anexado`.
     */
    canalEnvio?: "email" | "copia"
    /**
     * Usuário-profissional que assina o termo — usado pra resolver as variáveis
     * `{{profissional.*}}` (nome, conselho, especialidade). Quando ausente, o
     * backend deixa os placeholders em fallback `___________`. O emissor da
     * requisição (recepção/Dono) NÃO é tratado automaticamente como profissional.
     * Precisa ser um profissional com vínculo Ativo no estabelecimento atual.
     */
    profissionalUsuarioId?: string | null
}

export interface EmitirTermoResposta {
    termoEmitidoId: number
    /** Preenchido quando `assinaturaTipo = aceite_link`; null em pdf_anexado. */
    tokenAceite: string | null
}

export interface ReenviarLinkResposta {
    /** Token atualizado/regenerado — useful pra reidratar o link copiado. */
    tokenAceite: string
    canal: "email" | "copia"
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
     * Emite um termo (snapshot HTML + hash). Header `Idempotency-Key` opcional
     * evita duplo-clique. O backend retorna 422 se o modelo estiver inativo,
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
     * Anexa o PDF assinado (multipart). Só funciona com status = Pendente
     * (regra do aggregate). Após anexar, o backend muda status para "Assinado".
     */
    async anexarPdf(termoId: number, arquivo: File): Promise<void> {
        const form = new FormData()
        form.append("arquivo", arquivo, arquivo.name)
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
     * Fase 4 — reenvia o link público (`aceite_link`) por e-mail ou apenas
     * devolve o token pra ser exibido/copiado pelo emissor.
     *
     * Comportamento por canal:
     *  - "email" (default): backend envia o e-mail. Cooldown de 5 min entre
     *    envios — 422 com mensagem amigável se ainda dentro do cooldown.
     *  - "copia": SEM cooldown — útil pra reabrir o modal "Copiar link" quando
     *    o emissor fechou a tela após emitir.
     *
     * Permissão: `termos.emitir`. Termo precisa estar Pendente.
     */
    async reenviarLink(
        termoId: number,
        canal: "email" | "copia" = "email",
    ): Promise<ReenviarLinkResposta> {
        const { data } = await httpClient.post<ReenviarLinkResposta>(
            `/termos/${termoId}/reenviar-link`,
            { canal },
        )
        return data
    },
}

/**
 * Gera um id curto para o header `Idempotency-Key`. crypto.randomUUID() está
 * disponível em todos os browsers que o app suporta. Fallback simples para
 * ambientes onde não existe.
 */
function cryptoIdempotencyKey(): string {
    try {
        return crypto.randomUUID()
    } catch {
        return `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`
    }
}
