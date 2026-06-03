import axios from "axios"

/**
 * Service do fluxo público de confirmação de presença em agendamento (Fase 2).
 *
 * Espelho 1:1 do `AgendamentoPublicoController` — `/api/publico/agendamentos/confirmar/{token}`.
 * Acesso 100% anônimo: o próprio token (256 bits) é a credencial.
 *
 * NÃO usa o `httpClient` padrão (sem cookies de sessão, sem interceptor de auth —
 * padrão idêntico ao `termoAceitePublicoService`).
 *
 * Erros (LGPD/anti-enumeração):
 *  - 410 Gone genérico → link inválido / expirado / cancelado.
 *  - 429 → rate-limit (10 req/min por IP).
 */

const baseURL = import.meta.env.VITE_API_BASE_URL
    ? `${(import.meta.env.VITE_API_BASE_URL as string).replace(/\/+$/, "")}/api`
    : "/api"

const publicClient = axios.create({
    baseURL,
    headers: { "Content-Type": "application/json" },
    withCredentials: false,
    timeout: 30_000,
})

/** Payload retornado pelo GET — apenas dados mínimos, sem PII do paciente (CA17/CA23). */
export interface ConfirmacaoPublicaDto {
    estabelecimentoNome: string
    profissionalNome:    string
    tipoServico:         string
    inicioPrevisto:      string
    fimPrevisto:         string
    statusAgendamento:   string
}

/** Resposta do POST de confirmação de presença. */
export interface ConfirmacaoPresencaResposta {
    /** "confirmado" ou "ja_confirmado" (idempotência). */
    resultado: "confirmado" | "ja_confirmado"
    mensagem:  string
}

export const agendamentoConfirmacaoPublicaService = {
    /**
     * Obtém o resumo do agendamento via token público (CA17).
     * @throws Erro com `response.status === 410` quando link inválido/expirado/cancelado.
     */
    async consultar(token: string): Promise<ConfirmacaoPublicaDto> {
        const { data } = await publicClient.get<ConfirmacaoPublicaDto>(
            `/publico/agendamentos/confirmar/${encodeURIComponent(token)}`,
        )
        return data
    },

    /**
     * Confirma presença via token público (CA18). Idempotente:
     * se já confirmado retorna 200 com `resultado = "ja_confirmado"`,
     * NÃO 410. Apenas link inválido/expirado/cancelado dispara 410.
     */
    async confirmar(token: string): Promise<ConfirmacaoPresencaResposta> {
        const { data } = await publicClient.post<ConfirmacaoPresencaResposta>(
            `/publico/agendamentos/confirmar/${encodeURIComponent(token)}`,
        )
        return data
    },
}
