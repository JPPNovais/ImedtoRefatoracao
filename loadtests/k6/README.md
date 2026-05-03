# Load tests — k6

Cenários de carga contra a API (`Imedto.Backend.API`). Os scripts são pensados
para rodar contra **staging/QA** ou contra um docker-compose local — **não** mire
em produção.

## Pré-requisitos

```sh
brew install k6                    # macOS
# ou: docker run -i grafana/k6 run - < script.js
```

## Variáveis de ambiente

| Variável             | Default                       | Uso                                        |
|----------------------|-------------------------------|--------------------------------------------|
| `BASE_URL`           | `http://localhost:5050/api`   | URL base da API                            |
| `EMAIL`              | `dono@imedto.com`             | Usuário para login                         |
| `PASSWORD`           | (nenhum)                      | Senha — obrigatória                        |
| `ESTABELECIMENTO_ID` | `1`                           | Tenant alvo das requisições autenticadas   |

## Scripts

- `00-smoke.js` — 1 VU por 30s. Sanity check antes de qualquer teste maior.
- `10-login.js` — POST /auth/login. Mede latência e taxa de erro (cookies HttpOnly).
- `20-listar-pacientes.js` — GET /paciente paginado. Caminho HOT do dashboard.
- `30-criar-agendamento.js` — POST /agendamento. Fluxo de escrita com UoW completo.

## Executar

```sh
k6 run -e BASE_URL=http://localhost:5050/api -e PASSWORD=xxx 00-smoke.js
k6 run -e PASSWORD=xxx 10-login.js
```

## Critérios de sucesso (thresholds)

Cada script declara seus thresholds. Padrão geral:
- `http_req_duration: p(95)<500` — 95% das requests abaixo de 500ms
- `http_req_failed: rate<0.01` — menos de 1% de erros

A execução **falha** (exit 99) se algum threshold for violado — útil para CI/CD.
