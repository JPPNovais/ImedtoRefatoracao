import axios from "axios"

/**
 * Service do fluxo público de aceite/recusa de termo (Fase 4).
 *
 * Espelho 1:1 do `TermoPublicoController` — `/api/publico/termos/aceite/{token}`.
 * Acesso 100% anônimo: o próprio token (256 bits) é a credencial. Por isso este
 * service NÃO usa o `httpClient` padrão:
 *  - sem `withCredentials` — não envia cookies de sessão pra um endpoint público.
 *  - sem interceptor de 401 → refresh → redirect /login (esse fluxo é só pra
 *    requests autenticados; em endpoint público, um 401 não existe e qualquer
 *    redirecionamento aqui quebraria a UX do paciente).
 *
 * Erros (LGPD/anti-enumeração):
 *  - 410 Gone genérico → link inválido / expirado / já respondido / revogado.
 *    O caller mapeia para o estado "expirado" da view.
 *  - 422 → erro de validação (ex: nomeConfirmado não bate). Mensagem em
 *    `data.mensagem`. NÃO confundir com 410.
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

/** Payload retornado pelo GET — só o mínimo (sem PII do paciente). */
export interface TermoPublicoDto {
    tituloModelo:          string
    conteudoSnapshotHtml:  string
    estabelecimentoNome:   string
    profissionalEmissor:   string
    emitidoEm:             string
}

/** Resposta do POST de aceite/recusa. */
export interface AceitePublicoResposta {
    /** "registrado" (mudou status agora) ou "ja_respondido" (idempotência). */
    resultado: "registrado" | "ja_respondido"
    mensagem:  string
}

export interface ResponderTermoPublicoPayload {
    aceito:           boolean
    /** Opcional — confirmação de nome (case/acentos ignorados no back). */
    nomeConfirmado?:  string
}

export const termoAceitePublicoService = {
    /**
     * Obtém o conteúdo do termo via token público.
     * @throws Erro com `response.status === 410` quando link inválido/expirado/já respondido.
     */
    async obter(token: string): Promise<TermoPublicoDto> {
        const { data } = await publicClient.get<TermoPublicoDto>(
            `/publico/termos/aceite/${encodeURIComponent(token)}`,
        )
        return data
    },

    /**
     * Registra aceite ou recusa. Idempotente — chamadas repetidas sobre um
     * token já respondido devolvem 200 com `resultado = "ja_respondido"`,
     * NÃO 410. Apenas link nunca-emitido/expirado/revogado dispara 410.
     */
    async responder(
        token: string,
        payload: ResponderTermoPublicoPayload,
    ): Promise<AceitePublicoResposta> {
        const { data } = await publicClient.post<AceitePublicoResposta>(
            `/publico/termos/aceite/${encodeURIComponent(token)}`,
            payload,
        )
        return data
    },
}

/**
 * Monta a URL pública pra compartilhar com o paciente.
 * Em produção, `window.location.origin` resolve para https://app.imedto.com.
 * Em dev/preview, pega o host atual — funciona em qualquer ambiente.
 */
export function montarUrlAceitePublico(token: string): string {
    const base =
        (import.meta.env.VITE_APP_BASE_URL as string | undefined)?.replace(/\/+$/, "")
        ?? window.location.origin
    return `${base}/termos/aceite/${encodeURIComponent(token)}`
}
