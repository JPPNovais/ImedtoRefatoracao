# Imedto Mobile — app do médico

App mobile cross-platform (iOS + Android) do Imedto, implementado a partir do
**Design Brief Mobile** (`Docs/Discoverys/DESIGN_BRIEF_MOBILE.md`) e da prototipação
de design (handoff do Claude Design). É um projeto **separado** do `frontend/` web,
mas herda a **alma visual** (tokens, Nunito, roxo `#442B97`, cantos suaves) e os
**padrões de arquitetura** do projeto.

> Princípio-âncora do brief: *mobile ≠ web espremido*. O app resolve o médico **em
> movimento** — glanceável, read-first/write-light, polegar manda, capabilities
> nativas (push, câmera, biometria, voz, share) como diferencial.

## Stack

- **Capacitor 6** (iOS + Android neutro) + **Vue 3 + TypeScript + Vite + Pinia + Vue Router**
- **Backend via API**: o app **só fala com a API do backend Imedto** — nunca com o
  banco direto. Auth via cookie HttpOnly (BFF, espelho do web), header
  `X-Estabelecimento-Id` para multi-tenant, refresh em 401, bloqueio em 402.
- **SQLite local** (`@capacitor-community/sqlite`): cache offline leve + rascunho de
  evolução (§7 do brief). Nada de regra de negócio no device — a fonte de verdade é a API.
- Plugins nativos: Camera, PushNotifications, Share, Network, Preferences, Haptics,
  Biometric (`capacitor-native-biometric`), SpeechRecognition.

## Premissas do projeto respeitadas

- **Regra de negócio sempre no backend.** O app é UX; o 422 (`BusinessException`) é a verdade.
- **Multi-tenant em camadas.** `X-Estabelecimento-Id` em toda request; switcher recarrega o contexto.
- **LGPD é design.** Listas só com marcador de alerta (nunca o texto); ficha mascara PII
  com revelação por biometria; abrir a ficha audita o acesso; mensagens genéricas.
- **Degradação por permissão (G2).** O que o RBAC nega **some** da UI (ações do FAB,
  aprovar orçamento), não vira erro.
- **Reuso > duplicação.** Um `app.css` portado do design + componentes finos.

## Estrutura

```
src/
  lib/        http (client BFF), db (SQLite cache), mockApi (dev), format
  native/     useBiometric, useCamera, useShare, usePush, useVoice
  services/   auth, estabelecimento, agenda, paciente, prontuario, notificacao, documentos, orcamento
  stores/     ui, permissoes, tenant, auth, notificacoes (Pinia composition API)
  components/ ui/ (design system mobile) · layout/ (TabsLayout, BottomTabBar, FAB, sheets) · AssinaturaFlow
  views/      Login, Seletor, Agenda, AgendamentoDetalhe, NovoAgendamento, Pacientes,
              PacienteFicha, Prontuario, Avisos, Receita, Atestado, Exame, Orcamento,
              Mais, AssinaturaExpirada
  styles/     tokens.css (marca) + app.css (componentes, claro/escuro)
  router/     rotas + guards (auth, tenant)
_design-reference/   handoff original do Claude Design (fonte de verdade visual)
```

## Telas implementadas (cobertura do brief)

Login + biometria · Seletor de estabelecimento · **Agenda** (date strip, stats, próximo,
swipe atendido/faltou, pull-to-refresh, busca + filtro contextual) · Detalhe do agendamento
(+ share de confirmação) · Novo agendamento · **Pacientes** (busca debounce, filtros,
skeleton, marcador de alerta) · **Ficha** (alerta clínico, PII com biometria, abas, auditoria) ·
**Prontuário** (timeline + nova evolução com câmera e voz, rascunho offline) · **Avisos**
(grupos, não-lidos, deep-link, push banner) · **Receita / Atestado / Exame** (favoritos,
assinatura ANVISA animada, share) · **Orçamento** (aprovar/recusar com degradação por papel) ·
**Mais** (tema claro/escuro/automático, trocar estabelecimento, abrir-no-navegador) ·
Estados globais **G1** (assinatura expirada), **G2** (sem permissão), **G3** (offline).

## Rodar

> **MVP: o app já aponta para PRODUÇÃO** (`https://app.imedto.com`).
> - Nativo/produção: `.env.production` baka `VITE_API_BASE_URL=https://app.imedto.com`.
> - Dev no browser: `.env.development` deixa a base relativa e o proxy do Vite aponta
>   para produção (server-side, sem CORS no browser; o cookie é reescrito para o host local).

```bash
cd mobile
npm install

# Dev contra a PRODUÇÃO (proxy do Vite):
npm run dev

# Navegar tudo sem backend (dados de demonstração):
VITE_USE_MOCKS=true npm run dev

npm run typecheck           # vue-tsc
npm run build               # build (produção) para empacotar no Capacitor
```

### Empacotar nativo (iOS/Android)

```bash
npm run build
npx cap add ios && npx cap add android   # gera os projetos nativos (1ª vez)
npm run cap:sync
npx cap open ios          # Xcode
npx cap open android      # Android Studio
```

## Integração com o backend de produção (cookies/CORS) — resolvido

O app autentica via **cookie HttpOnly** (BFF), igual ao web.

- **No device (iOS/Android):** as chamadas saem pelo **`CapacitorHttp`** (HTTP nativo,
  URLSession/OkHttp). Isso **não passa por CORS** (CORS é regra de browser) e usa o
  **cookie jar nativo** — imune ao bloqueio de cookies de terceiros do WKWebView. Ou
  seja: no app real, CORS não é um problema. Só é preciso a API estar acessível em
  HTTPS (está: `https://app.imedto.com`, Caddy roteia `/api` e `/hubs` pro backend).
- **No browser (dev/preview):** usa `fetch` + proxy do Vite (mesma origem → sem CORS),
  com `cookieDomainRewrite` para a sessão persistir no host local.
- **CORS do backend já liberado** para as origens do Capacitor
  (`capacitor://localhost`, `https://localhost`, `http://localhost`, `ionic://localhost`)
  em `deploy/docker-compose.yml` (`Cors__AllowedOrigins`), cobrindo o caso de WebView/preview.
  **Requer redeploy do backend** para entrar em vigor.
- O cookie de auth já é **`SameSite=None; Secure`** em produção (`AuthController`),
  pré-requisito para sessão cross-site.

Push real (APNs/FCM) ainda exige configurar certificados/keys no projeto nativo.

## Modo mock (apenas dev)

`VITE_USE_MOCKS=true` roteia as chamadas para `src/lib/mockApi.ts` com os mesmos dados
da prototipação, para navegar o app inteiro sem backend. **Nunca** roda em produção.
