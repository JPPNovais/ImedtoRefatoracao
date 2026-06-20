/* ─────────────────────────────────────────────────────────────
   HTTP client — espelha o BFF do frontend web:
   - autenticação via cookie HttpOnly (sem token em memória/JS)
   - header X-Estabelecimento-Id injeta o tenant ativo
   - 401 → tenta /auth/refresh uma vez e repete a request
   - 402 → assinatura inativa / feature bloqueada (callbacks)
   - 422 → BusinessException é a fonte de verdade da regra de negócio

   Na plataforma nativa usa CapacitorHttp (cookie jar nativo).
   Na web usa fetch com credentials:'include' (proxy do Vite cuida do CORS).
   Regra do projeto: o app só fala com a API; nunca com o banco direto.
   ───────────────────────────────────────────────────────────── */
import { Capacitor, CapacitorHttp } from "@capacitor/core"
import type { ApiError } from "@/types"
import { USE_MOCKS, mockRoute } from "@/lib/mockApi"
import { API_BASE } from "@/lib/config"

const BASE_URL = API_BASE

// ── Hooks de integração (setados em main.ts para evitar dependência circular) ──
let tenantIdProvider: () => number | null = () => null
let onAuthExpired: () => void = () => {}
let onAssinaturaBloqueada: (tipo: string) => void = () => {}

export function configureHttp(opts: {
  tenantIdProvider?: () => number | null
  onAuthExpired?: () => void
  onAssinaturaBloqueada?: (tipo: string) => void
}) {
  if (opts.tenantIdProvider) tenantIdProvider = opts.tenantIdProvider
  if (opts.onAuthExpired) onAuthExpired = opts.onAuthExpired
  if (opts.onAssinaturaBloqueada) onAssinaturaBloqueada = opts.onAssinaturaBloqueada
}

interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE" | "PATCH"
  params?: Record<string, string | number | boolean | undefined | null>
  body?: unknown
  /** desativa o retry de refresh (usado pelo próprio /auth/refresh) */
  skipRefresh?: boolean
  headers?: Record<string, string>
  /** AbortSignal para cancelar requests obsoletos (somente web; no nativo é ignorado). */
  signal?: AbortSignal
}

interface RawResponse {
  status: number
  data: unknown
}

function buildUrl(path: string, params?: RequestOptions["params"]): string {
  const url = new URL(BASE_URL + path, "http://placeholder")
  if (params) {
    for (const [k, v] of Object.entries(params)) {
      if (v !== undefined && v !== null) url.searchParams.set(k, String(v))
    }
  }
  // Reconstrói preservando base relativa quando VITE_API_BASE_URL é vazio (proxy do Vite).
  const qs = url.search
  return BASE_URL + path + qs
}

function baseHeaders(extra?: Record<string, string>): Record<string, string> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    Accept: "application/json",
    ...extra,
  }
  const tenantId = tenantIdProvider()
  if (tenantId) headers["X-Estabelecimento-Id"] = String(tenantId)
  return headers
}

async function rawRequest(path: string, opts: RequestOptions): Promise<RawResponse> {
  const method = opts.method || "GET"

  // Dev/preview: roteia para a mock API quando ligada. Produção nunca entra aqui.
  if (USE_MOCKS) {
    const mock = await mockRoute(method, path, opts.params as Record<string, unknown>)
    if (mock) return mock
  }

  const url = buildUrl(path, opts.params)
  const headers = baseHeaders(opts.headers)

  if (Capacitor.isNativePlatform()) {
    const res = await CapacitorHttp.request({
      url,
      method,
      headers,
      data: opts.body,
      webFetchExtra: { credentials: "include" },
    })
    return { status: res.status, data: res.data }
  }

  const res = await fetch(url, {
    method,
    headers,
    credentials: "include",
    body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
    signal: opts.signal,
  })
  let data: unknown = null
  const text = await res.text()
  if (text) {
    try {
      data = JSON.parse(text)
    } catch {
      data = text
    }
  }
  return { status: res.status, data }
}

function toApiError(status: number, data: unknown): ApiError {
  const obj = (typeof data === "object" && data ? (data as Record<string, unknown>) : {}) as Record<
    string,
    unknown
  >
  return {
    status,
    tipo: (obj.tipo as string) || (obj.type as string) || undefined,
    // Mensagem genérica por padrão (LGPD: sem vazar detalhe técnico/PII).
    mensagem:
      (obj.mensagem as string) ||
      (obj.message as string) ||
      (obj.detail as string) ||
      "Não foi possível concluir. Tente novamente.",
  }
}

async function request<T>(path: string, opts: RequestOptions = {}): Promise<T> {
  let res = await rawRequest(path, opts)

  // 401 → refresh + retry único
  if (res.status === 401 && !opts.skipRefresh) {
    const refreshed = await tryRefresh()
    if (refreshed) {
      res = await rawRequest(path, opts)
    }
    if (res.status === 401) {
      onAuthExpired()
      throw toApiError(401, res.data)
    }
  }

  // 402 → assinatura inativa ou feature bloqueada
  if (res.status === 402) {
    const err = toApiError(402, res.data)
    onAssinaturaBloqueada(err.tipo || "FeatureBloqueada")
    throw err
  }

  if (res.status >= 200 && res.status < 300) {
    return res.data as T
  }

  throw toApiError(res.status, res.data)
}

/** Envia FormData via POST, reutilizando baseHeaders (SEM Content-Type — boundary automático),
 *  cookies BFF, header de tenant, e o mesmo fluxo de refresh em 401 + normalização ApiError. */
async function postForm<T>(path: string, form: FormData, extraHeaders?: Record<string, string>): Promise<T> {
  // baseHeaders sem Content-Type para que o browser/nativo injete o boundary correto.
  const headers: Record<string, string> = { Accept: "application/json", ...extraHeaders }
  const tenantId = tenantIdProvider()
  if (tenantId) headers["X-Estabelecimento-Id"] = String(tenantId)

  const url = buildUrl(path)

  async function doPost(): Promise<RawResponse> {
    if (Capacitor.isNativePlatform()) {
      // CapacitorHttp não suporta FormData diretamente; cai no fetch.
      const res = await fetch(url, { method: "POST", headers, credentials: "include", body: form })
      let data: unknown = null
      const text = await res.text()
      if (text) { try { data = JSON.parse(text) } catch { data = text } }
      return { status: res.status, data }
    }
    const res = await fetch(url, { method: "POST", headers, credentials: "include", body: form })
    let data: unknown = null
    const text = await res.text()
    if (text) { try { data = JSON.parse(text) } catch { data = text } }
    return { status: res.status, data }
  }

  let res = await doPost()

  if (res.status === 401) {
    const refreshed = await tryRefresh()
    if (refreshed) res = await doPost()
    if (res.status === 401) { onAuthExpired(); throw toApiError(401, res.data) }
  }
  if (res.status === 402) {
    const err = toApiError(402, res.data)
    onAssinaturaBloqueada(err.tipo || "FeatureBloqueada")
    throw err
  }
  if (res.status >= 200 && res.status < 300) return res.data as T
  throw toApiError(res.status, res.data)
}

let refreshing: Promise<boolean> | null = null
async function tryRefresh(): Promise<boolean> {
  if (refreshing) return refreshing
  refreshing = (async () => {
    try {
      const r = await rawRequest("/auth/refresh", { method: "POST", skipRefresh: true })
      return r.status >= 200 && r.status < 300
    } catch {
      return false
    } finally {
      // libera após resolver
      setTimeout(() => (refreshing = null), 0)
    }
  })()
  return refreshing
}

/** Baixa um recurso binário (PDF, etc.) respeitando cookie BFF + tenant header.
    Web: fetch com credentials:'include'. Nativo: CapacitorHttp com responseType blob.
    Lança ApiError em status >= 400. */
export async function getBlob(path: string): Promise<Blob> {
  const url = buildUrl(path)
  const headers = baseHeaders()

  if (Capacitor.isNativePlatform()) {
    const res = await CapacitorHttp.request({
      url,
      method: "GET",
      headers,
      responseType: "blob",
      webFetchExtra: { credentials: "include" },
    })
    if (res.status >= 400) throw toApiError(res.status, res.data)
    // CapacitorHttp retorna base64 para blob no nativo; converte para Blob
    const base64 = res.data as string
    const binary = atob(base64)
    const bytes = new Uint8Array(binary.length)
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i)
    return new Blob([bytes], { type: "application/pdf" })
  }

  const res = await fetch(url, { method: "GET", headers, credentials: "include" })
  if (res.status >= 400) {
    let data: unknown = null
    try { data = await res.json() } catch { /* ignora */ }
    throw toApiError(res.status, data)
  }
  return res.blob()
}

export const http = {
  get: <T>(path: string, params?: RequestOptions["params"], signal?: AbortSignal) =>
    request<T>(path, { params, signal }),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: "POST", body }),
  put: <T>(path: string, body?: unknown) => request<T>(path, { method: "PUT", body }),
  patch: <T>(path: string, body?: unknown) => request<T>(path, { method: "PATCH", body }),
  del: <T>(path: string) => request<T>(path, { method: "DELETE" }),
  /** Envia FormData (multipart) com refresh, tenant e normalização de erro — sem fetch cru nas views. */
  postForm,
  raw: rawRequest,
  getBlob,
}
