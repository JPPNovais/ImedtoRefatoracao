// Listar pacientes — caminho HOT do dashboard.
// Login uma vez por VU, depois N requests autenticados.
import http from "k6/http"
import { check, sleep } from "k6"
import { BASE_URL, login, tenantHeaders } from "./lib/auth.js"

export const options = {
    stages: [
        { duration: "20s", target: 10  },
        { duration: "2m",  target: 100 }, // pico
        { duration: "30s", target: 0   },
    ],
    thresholds: {
        "http_req_duration{name:listar}": ["p(95)<500", "p(99)<1000"],
        "http_req_failed{name:listar}":   ["rate<0.01"],
    },
}

// Login por VU (k6 chama setup global, mas precisamos de cookies por iteração).
export default function () {
    login()

    // Faz 5 requests por iteração (paginação tipica do front).
    for (let p = 1; p <= 5; p++) {
        const r = http.get(`${BASE_URL}/paciente?pagina=${p}&tamanho=20`, {
            ...tenantHeaders(),
            tags: { name: "listar" },
        })
        check(r, {
            "listar 200": (resp) => resp.status === 200,
        })
    }
    sleep(1)
}
