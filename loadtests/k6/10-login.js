// Login load — exercita o BFF de auth e o Supabase upstream.
// Ramp-up até 50 VUs em 1min, sustenta 50 VUs por 2min, ramp-down.
import http from "k6/http"
import { check } from "k6"
import { BASE_URL, EMAIL, PASSWORD } from "./lib/auth.js"

export const options = {
    stages: [
        { duration: "30s", target: 20 },
        { duration: "1m",  target: 50 },
        { duration: "30s", target: 0  },
    ],
    thresholds: {
        // Login envolve roundtrip ao Supabase Auth → tolerar ate 1.5s p95.
        "http_req_duration{name:login}": ["p(95)<1500"],
        "http_req_failed{name:login}":   ["rate<0.02"],
    },
}

export default function () {
    const r = http.post(
        `${BASE_URL}/auth/login`,
        JSON.stringify({ email: EMAIL, password: PASSWORD }),
        {
            headers: { "Content-Type": "application/json" },
            tags: { name: "login" },
        },
    )
    check(r, {
        "login 200": (resp) => resp.status === 200,
        "tem cookie access-token": (resp) =>
            (resp.cookies["access-token"] || []).length > 0,
    })
}
