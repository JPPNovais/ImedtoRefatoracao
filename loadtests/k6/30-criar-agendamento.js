// Criar agendamento — fluxo de escrita com UoW completo.
// Cada VU cria um agendamento, idempotency key única por iteração.
//
// Pré-requisito: o estabelecimento alvo (ESTABELECIMENTO_ID) precisa ter
// pelo menos 1 paciente e 1 vínculo profissional ativo previamente cadastrados
// (use o ambiente de QA seedado).
import http from "k6/http"
import { check, sleep } from "k6"
import { uuidv4 } from "https://jslib.k6.io/k6-utils/1.4.0/index.js"
import { BASE_URL, login, tenantHeaders } from "./lib/auth.js"

const PACIENTE_ID = __ENV.PACIENTE_ID || "1"
const PROFISSIONAL_ID = __ENV.PROFISSIONAL_ID

export const options = {
    stages: [
        { duration: "20s", target: 5  },
        { duration: "1m",  target: 20 },
        { duration: "20s", target: 0  },
    ],
    thresholds: {
        "http_req_duration{name:criar}": ["p(95)<800"],
        "http_req_failed{name:criar}":   ["rate<0.02"],
    },
}

if (!PROFISSIONAL_ID) {
    throw new Error("PROFISSIONAL_ID env var é obrigatória.")
}

export default function () {
    login()

    // Janela futura aleatória para reduzir colisão de horário.
    const offsetMinutos = 30 + Math.floor(Math.random() * 60 * 24 * 30)
    const inicio = new Date(Date.now() + offsetMinutos * 60_000)
    const fim = new Date(inicio.getTime() + 30 * 60_000)

    const body = {
        pacienteId: Number(PACIENTE_ID),
        profissionalUsuarioId: PROFISSIONAL_ID,
        inicioPrevisto: inicio.toISOString(),
        fimPrevisto: fim.toISOString(),
        tipoServico: "Consulta",
    }

    const r = http.post(`${BASE_URL}/agendamento`, JSON.stringify(body), {
        ...tenantHeaders(),
        headers: {
            ...tenantHeaders().headers,
            "Idempotency-Key": uuidv4(),
        },
        tags: { name: "criar" },
    })

    check(r, {
        "criar 200/201": (resp) => resp.status === 200 || resp.status === 201,
    })
    sleep(0.5)
}
