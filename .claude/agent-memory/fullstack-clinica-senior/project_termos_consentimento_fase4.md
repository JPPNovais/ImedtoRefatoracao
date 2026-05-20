---
name: termos-consentimento-fase4
description: Frontend Fase 4 (aceite via link público) implementado em 2026-05-20 — view anônima, cliente axios dedicado, EmitirTermoModal habilitado.
metadata:
  type: project
---

Frontend da Fase 4 dos Termos de Consentimento concluído em 2026-05-20. Backend (commit `1fbcf65`) já tinha tudo pronto — endpoints públicos `/api/publico/termos/aceite/{token}` (GET + POST), `/api/termos/{id}/reenviar-link`, `EmitirTermoCommand` aceitando `assinaturaTipo: "aceite_link"` + `canalEnvio`.

**Why:** habilitar o fluxo de aceite digital pelo paciente sem login — paciente recebe link por e-mail (ou copiado pelo emissor), abre a view pública, marca "Li e estou de acordo" + nome opcional, clica "Aceito" ou "Não aceito". O backend valida hash de integridade e registra audit log.

**How to apply:**

Arquivos criados:
- `frontend/src/views/publico/AceiteTermoPublicoView.vue` — view anônima fora do AppLayout (rota sem `meta.layout === "app"`, então `App.vue` renderiza `<router-view v-else />`). Estados: carregando/pronto/sucesso/ja_respondido/expirado/erro. Foco inicial no h1 (acessibilidade).
- `frontend/src/services/termoAceitePublicoService.ts` — cliente axios **dedicado**, sem `withCredentials`, sem interceptor de 401 (não passa pelo httpClient padrão pra não disparar refresh-token → redirect /login num endpoint público). Expõe `obter`, `responder`, `montarUrlAceitePublico`.
- `frontend/src/views/publico/AceiteTermoPublicoView.test.ts` — smoke: pronto, expirado, aceite, recusa (precisa `attachTo: document.body` + buscar no body porque AppConfirmDialog teleporta), 422 nome não bate.

Arquivos modificados:
- `frontend/src/router/index.ts` — adicionada rota `AceiteTermoPublico` no bloco de públicas (sem `requiresAuth`, sem layout). NÃO precisa entrar em `ROTAS_RESTRITAS`.
- `frontend/src/services/pacienteTermoService.ts` — `EmitirTermoPayload` ganhou `canalEnvio?: "email"|"copia"`; novo método `reenviarLink(termoId, canal)`.
- `frontend/src/components/termos/EmitirTermoModal.vue` — card "Enviar link" reativado (sem badge "Em breve"). Sub-opções e-mail/copia. Passo 4 (pós-emissão) mostra link + botão "Copiar". `pacienteSemEmail` desabilita opção e-mail.
- `frontend/src/components/termos/PacienteTermosTab.vue` — botões `fa-link` (Copiar link) e `fa-envelope` (Reenviar e-mail) só pra `Pendente + AceiteLink`. `onEmitido` pula geração de PDF quando o termo recém-emitido é aceite_link (modal já mostrou tela de sucesso).

**Decisões importantes:**

1. **Token NÃO vem na listagem** (LGPD — `TermoEmitidoResumoDto` não expõe). Para o botão "Copiar link" funcionar, usei `reenviarLink(id, "copia")` que retorna o token sem enviar e-mail e sem cooldown. Endpoint perfeito pro caso.

2. **Cliente axios separado** (`termoAceitePublicoService`) em vez de reusar `httpClient`. O httpClient padrão dispara refresh + redirect em 401, e cookies HttpOnly em request anônimo é vazamento.

3. **Erros do POST de aceite:**
   - 410 → estado `expirado` (link inválido/expirado/revogado)
   - 422 → erro inline no campo nome (única validação que retorna 422 nesse endpoint)
   - 429 → toast de rate-limit
   - Resposta com `resultado: "ja_respondido"` → estado `ja_respondido` (não é erro)

4. **CSS do AppConfirmDialog teleporta para body** — testes precisam `attachTo: document.body` e `document.body.querySelectorAll("button")` em vez de `wrapper.findAll("button")`.

5. **`VITE_APP_BASE_URL` opcional** — `montarUrlAceitePublico` usa essa env var em prod (https://app.imedto.com) com fallback para `window.location.origin`. Hoje a env var não está setada — fallback resolve em qualquer ambiente.

Vê [[termos-consentimento-fase3]] para a Fase 3 (PDF anexado + revogação + drawer) e [[termos-consentimento-fase1]]/[[termos-consentimento-fase2]] pro backend + modelos.
