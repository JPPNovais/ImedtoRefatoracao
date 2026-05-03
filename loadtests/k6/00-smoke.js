// Smoke test — 1 VU, 30s. Confirma que a API responde antes de testes maiores.
import http from "k6/http"
import { check, sleep } from "k6"
import { BASE_URL } from "./lib/auth.js"

export const options = {
    vus: 1,
    duration: "30s",
    thresholds: {
        http_req_duration: ["p(95)<500"],
        http_req_failed:   ["rate<0.01"],
    },
}

export default function () {
    const r = http.get(`${BASE_URL}/health`)
    check(r, {
        "health 200": (resp) => resp.status === 200,
    })
    sleep(1)
}
