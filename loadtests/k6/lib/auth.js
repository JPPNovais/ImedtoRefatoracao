// Helpers compartilhados pelos cenários de carga.
import http from "k6/http"
import { check, fail } from "k6"

export const BASE_URL = __ENV.BASE_URL || "http://localhost:5050/api"
export const EMAIL = __ENV.EMAIL || "dono@imedto.com"
export const PASSWORD = __ENV.PASSWORD || ""
export const ESTABELECIMENTO_ID = __ENV.ESTABELECIMENTO_ID || "1"

if (!PASSWORD) {
    fail("PASSWORD env var é obrigatória. Ex: k6 run -e PASSWORD=xxx script.js")
}

/**
 * Faz login e devolve o cookieJar populado. Cookies HttpOnly (access-token,
 * refresh-token) ficam no jar e são reenviados automaticamente pelo k6.
 */
export function login() {
    const jar = http.cookieJar()
    const r = http.post(
        `${BASE_URL}/auth/login`,
        JSON.stringify({ email: EMAIL, password: PASSWORD }),
        {
            headers: { "Content-Type": "application/json" },
            cookieJar: jar,
        },
    )
    check(r, { "login 200": (resp) => resp.status === 200 })
    if (r.status !== 200) fail(`login falhou: ${r.status} ${r.body}`)
    return jar
}

/**
 * Headers padrão para requests autenticados de tenant. O k6 preenche cookies
 * automaticamente via cookieJar, então só anexamos o header de tenant.
 */
export function tenantHeaders() {
    return {
        headers: {
            "Content-Type": "application/json",
            "X-Estabelecimento-Id": ESTABELECIMENTO_ID,
        },
    }
}
