/**
 * Load test de rate limit dos endpoints de autenticação.
 *
 * Valida que:
 *  - /api/auth/login: as primeiras 5 tentativas retornam 422/401 (não 429)
 *                     a partir da 6ª retornam 429
 *  - /api/auth/refresh: primeiras 10 OK, da 11ª em diante 429
 *  - /api/auth/signup (auth-sensitive): primeiras 3 OK, da 4ª em diante 429
 *
 * Pré-requisito: k6 instalado (https://grafana.com/docs/k6/latest/set-up/install-k6/)
 *
 * Como rodar (requer a API rodando):
 *   ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/src/Services/Imedto.Backend.API --no-launch-profile
 *   k6 run scripts/load-tests/auth-rate-limit.js
 *
 * Para apontar para outro ambiente:
 *   k6 run -e BASE_URL=https://api.staging.imedto.com.br scripts/load-tests/auth-rate-limit.js
 *
 * Nota: o rate limit é por IP (janela deslizante de 60s). Rodando localmente, todos os requests
 * vêm do mesmo IP (127.0.0.1), o que replica fielmente o cenário de brute force.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

const HEADERS = { 'Content-Type': 'application/json' };

const PAYLOAD_LOGIN = JSON.stringify({
    email: 'teste-rate-limit@imedto.com.br',
    senha: 'SenhaErradaIntencional123!',
});

const PAYLOAD_SIGNUP = JSON.stringify({
    email: 'novo-rate-limit@imedto.com.br',
    senha: 'SenhaErradaIntencional123!',
});

export const options = {
    scenarios: {
        // Valida /auth/login — limite 5/60s
        login_burst: {
            executor: 'per-vu-iterations',
            vus: 1,
            iterations: 10,
            maxDuration: '30s',
            exec: 'loginBurst',
        },
        // Valida /auth/refresh — limite 10/60s
        // Deve rodar após o login_burst para não compartilhar a janela de rate limit
        // (em ambiente real, o rate limit é por IP; separar por cenário ajuda na leitura).
        refresh_burst: {
            executor: 'per-vu-iterations',
            vus: 1,
            iterations: 12,
            maxDuration: '30s',
            startTime: '35s',   // aguarda a janela do login_burst expirar
            exec: 'refreshBurst',
        },
        // Valida /auth/signup (auth-sensitive) — limite 3/60s
        signup_burst: {
            executor: 'per-vu-iterations',
            vus: 1,
            iterations: 5,
            maxDuration: '30s',
            startTime: '70s',   // aguarda a janela do refresh_burst expirar
            exec: 'signupBurst',
        },
    },

    thresholds: {
        // Respostas 429 devem ser rápidas (o middleware rejeita antes de chegar ao handler)
        'http_req_duration{status:429}': ['p(95)<200'],
        // Respostas de negócio (4xx não-429) também devem ser razoáveis
        'http_req_duration{status:422}': ['p(95)<1000'],
        'http_req_duration{status:401}': ['p(95)<1000'],
    },
};

/** Cenário: 10 tentativas de login em sequência rápida — primeiras 5 não-429, restantes 429 */
export function loginBurst() {
    const res = http.post(`${BASE_URL}/api/auth/login`, PAYLOAD_LOGIN, { headers: HEADERS });

    if (__ITER < 5) {
        check(res, {
            'login: até a 5ª tentativa não retorna 429': (r) => r.status !== 429,
            'login: dentro do limite retorna 422 ou 401': (r) => [422, 401].includes(r.status),
        });
    } else {
        check(res, {
            'login: da 6ª em diante retorna 429': (r) => r.status === 429,
        });
    }

    sleep(0.05); // 50ms entre chamadas — burst intencional
}

/** Cenário: 12 tentativas de refresh — primeiras 10 não-429, restantes 429 */
export function refreshBurst() {
    const res = http.post(`${BASE_URL}/api/auth/refresh`, null, { headers: HEADERS });

    if (__ITER < 10) {
        check(res, {
            'refresh: até a 10ª tentativa não retorna 429': (r) => r.status !== 429,
        });
    } else {
        check(res, {
            'refresh: da 11ª em diante retorna 429': (r) => r.status === 429,
        });
    }

    sleep(0.05);
}

/** Cenário: 5 tentativas de signup — primeiras 3 não-429, restantes 429 */
export function signupBurst() {
    const res = http.post(`${BASE_URL}/api/auth/signup`, PAYLOAD_SIGNUP, { headers: HEADERS });

    if (__ITER < 3) {
        check(res, {
            'signup: até a 3ª tentativa não retorna 429': (r) => r.status !== 429,
        });
    } else {
        check(res, {
            'signup: da 4ª em diante retorna 429': (r) => r.status === 429,
        });
    }

    sleep(0.05);
}
