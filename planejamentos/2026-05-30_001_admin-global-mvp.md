# Área Admin Global do Sistema — MVP

**ID**: 2026-05-30_001
**Data**: 2026-05-30
**Autor**: imedto-business-analyst
**Status**: Aprovado para implementação
**Estimativa de esforço**: G (envolve auth dedicada, 6 tabelas novas, módulo frontend isolado e blindagem multi-tenant adicional)
**Áreas regressivas tocadas**: auth (LocalJwt), permissionamento, assinatura/planos, infra (config global), nenhum módulo clínico (somente leitura agregada + reset destrutivo controlado)
**Agentes downstream esperados**: `imedto-database` → `imedto-developer` → `imedto-qa`

---

## 1. Contexto e motivação

Hoje o Imedto possui um único plano vitalício de teste atrelado a estabelecimentos, e o time de suporte/produto (`contato.imedto@gmail.com`) não tem ferramenta interna para:

- Conceder gratuidade vitalícia ou estendida a um cliente específico (ex.: parceiro, beta tester, profissional convidado).
- Resetar dados operacionais de um tenant quando o cliente pede ("apaguei tudo errado, reseta o teste").
- Listar quais estabelecimentos existem, ver dono, plano vigente, contagens agregadas.
- Criar/gerenciar contas com poder de admin global de forma rastreável.
- Estabelecer fundação para futura cobrança (planos, assinaturas) e governança (audit, MFA, impersonate).

Já existe esqueleto parcial no backend, mas **sem auth dedicada, sem persistência de admins, sem UI e sem audit estruturado** — o caminho atual é `[Authorize]` puro com gate `_env.IsDevelopment()`. Em prod, qualquer usuário autenticado bate na rota e é barrado apenas pelo ambiente. Inseguro para virar feature de uso real.

**Referências do legado levantadas** (apenas como inspiração, não como código a portar — legado é Vue+Supabase com RLS, refactor é .NET CQRS):

- [`ReferenciaLegado/Imedto/admin/`](../ReferenciaLegado/Imedto/admin/) — SPA Vue dedicada do admin no legado (separada do app principal). Confirma viabilidade do isolamento por pasta.
- [`ReferenciaLegado/Imedto/supabase/migrations/20260310000001_add_is_imedto_admin.sql`](../ReferenciaLegado/Imedto/supabase/migrations/20260310000001_add_is_imedto_admin.sql) — flag booleana no perfil. **Não vamos seguir esse padrão** (admin acoplado a usuário comum = risco de escape de privilégio). Vamos para tabela física separada.
- [`ReferenciaLegado/Imedto/supabase/migrations/20260311000004_platform_admin_models.sql`](../ReferenciaLegado/Imedto/supabase/migrations/20260311000004_platform_admin_models.sql) — modelos globais (catálogo). Fora do MVP, vira backlog.
- [`ReferenciaLegado/Imedto/supabase/migrations/20260312000001_admin_reset_estabelecimento_dados.sql`](../ReferenciaLegado/Imedto/supabase/migrations/20260312000001_admin_reset_estabelecimento_dados.sql) — function SQL com módulos opt-in. Lógica de qual módulo apaga o quê **já foi portada** para `AdminResetService.cs` no refactor.
- [`ReferenciaLegado/Imedto/supabase/functions/admin-reset-estabelecimento/`](../ReferenciaLegado/Imedto/supabase/functions/admin-reset-estabelecimento/) — edge function que disparava o reset. No refactor isso é endpoint `/api/admin/estabelecimentos/{id}/reset` (já existe esqueleto).

**Esqueleto refactor atual** (a evoluir):

- [`backend/src/Services/Imedto.Backend.API/Controllers/AdminController.cs`](../backend/src/Services/Imedto.Backend.API/Controllers/AdminController.cs) — único endpoint hoje (reset), gateado por `IsDevelopment()`. Será movido para `Controllers/Admin/EstabelecimentosController.cs` e blindado por policy real.
- [`backend/src/Services/Imedto.Backend.Domain/Admin/`](../backend/src/Services/Imedto.Backend.Domain/Admin/) — contratos `IAdminResetService` e `ResetModulos`. Reusados como estão.
- [`backend/src/Services/Imedto.Backend.Infrastructure/Admin/AdminResetService.cs`](../backend/src/Services/Imedto.Backend.Infrastructure/Admin/AdminResetService.cs) — implementação. Reusada como está, apenas passa a receber `adminId` (Guid de `imedto_admins`) em vez de `userId` de usuário comum.

---

## 2. Persona-alvo

**Você (dono do produto / suporte de primeira linha)** — `contato.imedto@gmail.com`. Acessa esporadicamente (~2-10x/semana no início, conforme onboardings/reclamações). Conhece o domínio profundamente; precisa de velocidade e rastreabilidade, não de tutoriais.

**Futuros operadores** — pessoas que você delegar acesso depois (técnicos de suporte, parceiros). MVP precisa permitir criar a conta deles e desativar quando saírem, sem que isso vire ticket de "esqueci a senha do admin@imedto".

---

## 3. Objetivo

Criar uma área administrativa global, fisicamente isolada do app principal (frontend e backend), autenticada por conta dedicada, auditada em toda mutação, capaz de no MVP: gerenciar admins, listar/inspecionar estabelecimentos, resetar dados de tenant e gerir planos/assinaturas — com arquitetura preparada para extração futura para projeto separado.

---

## 4. Escopo

### 4.1 Inclui (IN)

1. **Auth admin dedicada** — endpoint `/api/admin/auth/login`, tabela `imedto_admins` separada, claim JWT `imedto_admin = true`, policy `ImedtoAdmin`, refresh próprio (`/api/admin/auth/refresh`, `/api/admin/auth/logout`).
2. **CRUD de admins** — listar, criar novo admin, desativar admin, reset de senha (gera e-mail + força reset no próximo login), trava do "último admin ativo".
3. **Bootstrap** — seed `admin@imedto.com / 123123` em Development (migration condicional). Em prod: comando CLI `dotnet run -- seed-admin --email X` que cria 1 admin com senha temporária aleatória impressa no terminal + `force_password_reset = true`.
4. **Lista de estabelecimentos** — paginada (25/pg), com busca por nome/CNPJ/e-mail do dono e filtros básicos (plano vigente, status ativo/inativo, criados em <período>). Colunas: nome, dono (nome + e-mail), plano vigente, criado em, contagens agregadas (profissionais ativos, pacientes, agendamentos do mês).
5. **Detalhe de estabelecimento** — metadados completos (não-PII de paciente): identificação, dono, datas, plano vigente, histórico de assinaturas, contagens agregadas. CPF de dono mascarado por padrão; botão "revelar" gera linha de audit.
6. **Reset de dados de tenant** — reuso do `AdminResetService` existente, agora com confirmação dupla obrigatória (digitar nome do estabelecimento + motivo livre) e audit.
7. **Planos** — CRUD básico de planos (nome, descrição curta, preço mensal sugerido, limites se houver, ativo s/n). É um catálogo simples; sem billing real.
8. **Assinaturas** — vincular estabelecimento a plano. Alterar plano = nova linha em `assinaturas` preservando histórico (não atualiza in-place). Conceder gratuidade = caso especial (plano "Gratuidade vitalícia" ou flag `gratuita = true` com `motivo` obrigatório e `data_fim` opcional).
9. **`imedto_config`** — tabela chave-valor para configs globais do sistema (futuro: SMTP override, feature flags, etc.). MVP entrega só a tabela + leitura, sem UI dedicada (uso interno por outros serviços).
10. **Audit trail** — tabela `imedto_admin_audit_log` populada em toda mutação e em leitura de detalhe sensível (reveal CPF). Retenção 2 anos.
11. **Frontend isolado** — módulo `frontend/src/modules/admin/` autocontido (views, stores, services, components, router). Rota `/admin/*` com guard separado. Sem importar de outros módulos do app. Banner visual distinto (vermelho em prod) deixando claro que está em área privilegiada.
12. **Documentação viva** — `Docs/ARQUITETURA.md` e `Docs/LGPD.md` atualizados como parte da entrega.

### 4.2 Não inclui (OUT — viram próximos briefings)

- **Impersonate** (entrar como cliente para reproduzir bug). Backlog priorizado #2.
- **PII de paciente** (lista de pacientes, prontuários, evoluções). Fora do MVP — admin só vê metadados agregados do tenant.
- **Catálogos globais** (modelos de prontuário padrão, exames físicos padrão, variáveis pool). Backlog #4.
- **Dashboard de métricas** (MRR, churn, ativações, gráficos). Backlog #5.
- **Fila Resend / status de e-mails enviados**. Backlog #8.
- **Logs centralizados / visualização de erros do sistema**. Backlog #9.
- **MFA TOTP**. Backlog #1 (mais prioritário antes de abrir admin para terceiros).
- **IP allow-list**. Backlog #6.
- **RBAC granular** (perfis de admin com poderes diferentes — ex.: read-only suporte vs. full). MVP é all-or-nothing. Backlog #3.
- **Billing real** (cobrança automatizada, gateway, NF). Backlog #7.

---

## 5. Decisões de produto cravadas

| # | Decisão | Resumo | Justificativa |
|---|---|---|---|
| 1 | Mesma SPA + namespace de rota | `/admin/*` na mesma SPA Vue, mas em `frontend/src/modules/admin/` isolado. Backend: `Controllers/Admin/*` e namespaces `Admin.*`. | MVP rápido sem overhead de 2º deploy. Extração futura = `git mv` da pasta + ajuste de `vite.config.ts`. |
| 2 | Escopo MVP enxuto | Login + Lista de tenants + Reset + Planos/Assinaturas + Gratuidade + CRUD de admins. Sem impersonate, sem catálogos. | Foco no que destrava o suporte hoje. Resto é backlog priorizado. |
| 3 | Tabela `imedto_admins` separada + reuso parametrizado de `LocalJwtAuthService` | Admins não são `usuarios`. JWT carrega claim `imedto_admin = true` e **nunca** `estabelecimento_id`. | Isolamento físico evita escape de privilégio. Reuso do serviço de JWT acelera entrega sem duplicar criptografia. |
| 4 | Sem PII de paciente no MVP | Admin vê só metadados agregados de tenant. Nada de listar pacientes, abrir prontuário. | LGPD: minimização. Quando precisar acessar caso real, vai por impersonate (backlog) com motivo + audit + janela limitada. |
| 5 | CRUD de admin via UI, trava do "último ativo" | Qualquer admin ativo cria/desativa outros. Reset de senha gera e-mail + força reset. Tentar desativar/excluir o último ativo = 422. | Velocidade operacional; trava impede tiro no pé. |
| 6 | Bootstrap híbrido | Dev: migration `Imedto_Seed_Admin_Development` insere `admin@imedto.com / 123123` com `force_password_reset = false`. Prod: comando CLI `dotnet run -- seed-admin --email X` imprime senha temporária aleatória no terminal e marca `force_password_reset = true`. | Dev produtivo, prod nunca tem senha conhecida em código. |
| 7 | Sessão curta + sem MFA no MVP | Access 15min, refresh 2h, inatividade no front desloga em 15min sem ação. MFA fica para backlog #1. | Janela curta reduz risco enquanto MFA não chega. Sem MFA, admin permanece de uso pessoal/restrito. |

---

## 6. Arquitetura proposta (alto nível)

### 6.1 Separação física

```
backend/src/Services/
├── Imedto.Backend.API/Controllers/
│   ├── ... (controllers normais — multi-tenant, claim usuario)
│   └── Admin/                              ← NOVO. Toda rota /api/admin/*
│       ├── AuthController.cs               (login, refresh, logout, forçar reset)
│       ├── AdminsController.cs             (CRUD de imedto_admins)
│       ├── EstabelecimentosController.cs   (lista, detalhe, reset — herda do AdminController atual)
│       ├── PlanosController.cs             (CRUD de planos)
│       └── AssinaturasController.cs        (alterar plano, conceder gratuidade)
│
├── Imedto.Backend.Domain/Admin/            ← Existe parcial. Expande:
│   ├── ImedtoAdmin.cs                      (agregado, value objects de senha, regras)
│   ├── Plano.cs
│   ├── Assinatura.cs
│   ├── ImedtoConfig.cs
│   ├── ImedtoAdminAuditLog.cs              (entidade de audit)
│   ├── IAdminResetService.cs               (já existe)
│   └── ResetModulos.cs                     (já existe)
│
├── Imedto.Backend.Application/Admin/       ← NOVO. Handlers CQRS:
│   ├── Auth/ (LoginAdmin, RefreshAdminToken, LogoutAdmin, ForceResetPassword)
│   ├── Admins/ (CriarAdmin, DesativarAdmin, ResetarSenhaAdmin, ListarAdmins)
│   ├── Estabelecimentos/ (ListarEstabelecimentosAdmin, ObterDetalheAdmin, RevelarCpfDono)
│   ├── Planos/ (CriarPlano, EditarPlano, ListarPlanos)
│   └── Assinaturas/ (AlterarPlanoTenant, ConcederGratuidade)
│
└── Imedto.Backend.Infrastructure/Admin/    ← Existe parcial. Expande:
    ├── AdminResetService.cs                (existe)
    ├── ImedtoAdminRepository.cs
    ├── ImedtoAdminAuditWriter.cs           (helper único, sem PII)
    └── ImedtoAdminTokenIssuer.cs           (wrapper sobre LocalJwt com claim imedto_admin)

frontend/src/modules/admin/                 ← NOVO. Autocontido:
├── router.ts                               (rotas /admin/*)
├── stores/
│   ├── adminAuthStore.ts
│   ├── adminEstabelecimentosStore.ts
│   ├── adminPlanosStore.ts
│   └── adminAdminsStore.ts
├── services/
│   └── adminApi.ts                         (axios instance com baseURL /api/admin)
├── components/
│   ├── AdminLayout.vue                     (banner vermelho em prod)
│   ├── AdminLoginView.vue
│   ├── EstabelecimentosListView.vue
│   ├── EstabelecimentoDetalheView.vue
│   ├── ConfirmarResetDialog.vue            (input que exige digitar nome do tenant)
│   ├── PlanosView.vue
│   ├── AdminsView.vue
│   └── ForcarResetSenhaView.vue
└── README.md                               (regras de isolamento + roadmap de extração)
```

### 6.2 Auth admin (espinha dorsal)

- **Tabela `imedto_admins`** com `id (uuid)`, `email`, `senha_hash` (bcrypt/argon2 — DB agent decide consistente com `usuarios`), `ativo`, `force_password_reset`, `criado_em`, `criado_por`, `desativado_em`, `desativado_por`, `ultimo_login_em`.
- **Endpoint `/api/admin/auth/login`** valida `email + senha`, gera access token (15min) com claim `imedto_admin = true`, `sub = imedto_admin.id`, e refresh token (2h) gravado em `imedto_admin_refresh_tokens`. **Nunca** carrega `estabelecimento_id`.
- **Wrapper `ImedtoAdminTokenIssuer`** chama `LocalJwtAuthService` parametrizado (mesmo ECDSA P-256 — chave reutilizada), injetando a claim distinta.
- **Policy `ImedtoAdmin`** (em `Program.cs`) exige claim `imedto_admin == "true"`. Toda rota `/api/admin/*` (exceto `/auth/login`) usa `[Authorize(Policy = "ImedtoAdmin")]`.
- **Blindagem cruzada — não-negociável**:
  - Middleware/filter nos endpoints normais (`Controllers/*` fora de `Admin/`) **rejeita com 403** qualquer JWT contendo `imedto_admin = true`. Admin global não é usuário do tenant.
  - Endpoints `/api/admin/*` rejeitam com 403 qualquer JWT que NÃO tenha `imedto_admin = true`. Não há "permissão sobreposta".
  - Nada de "se é admin global, pula filtro de tenant" no domínio — queries admin têm endpoints próprios e usam Dapper sem o `IEstabelecimentoContext` injetado.

### 6.3 Audit centralizado

- `ImedtoAdminAuditWriter` é o **único** caminho para registrar audit admin. Toda chamada de mutação (handler de comando) e leitura de detalhe sensível (reveal CPF, abrir detalhe de tenant) invoca o writer antes do retorno.
- Linha contém: `id`, `admin_id`, `acao` (enum string), `recurso_tipo` (`estabelecimento`, `plano`, `assinatura`, `admin`), `recurso_id`, `tenant_afetado_id` (nullable), `motivo`, `ip`, `user_agent`, `criado_em`, `payload_resumo` (JSON sem PII — ex.: `{"plano_antigo":"X","plano_novo":"Y"}`).
- **Listagem geral NÃO gera audit** (volume alto, sem valor forense). Apenas leitura de detalhe individual de tenant e reveal de CPF.

### 6.4 Frontend isolado

- `frontend/src/modules/admin/` tem seu próprio `router.ts` adicionado ao router global apenas no boot (`main.ts`).
- Guard de rota: ao entrar em `/admin/*` que não seja `/admin/login`, valida `adminAuthStore.isAutenticado`. Sem isso → redireciona para `/admin/login`.
- Banner visual: componente `AdminLayout.vue` exibe banner vermelho quando `import.meta.env.PROD === true`. Em dev, banner amarelo "DEV". Texto: "Área administrativa — uso interno Imedto".
- Inatividade: composable `useInatividadeAdmin` zera timer a cada `mousemove`/`keydown`. Após 15min sem evento, dispara `adminAuthStore.logout()`.
- **Regra de isolamento (verificável por lint/grep)**: nenhum arquivo em `frontend/src/modules/admin/` pode importar de `frontend/src/views/`, `frontend/src/stores/` (exceto `assinaturaStore` se DB agent decidir reuso — ver §10), `frontend/src/services/` (exceto reuso do design system em `frontend/src/components/ui/`). README do módulo registra essa regra.

---

## 7. Modelo de dados proposto (alto nível)

> Detalhamento técnico (tipos exatos, defaults, constraints, índices compostos) é responsabilidade do `imedto-database`. Aqui descrevemos forma e intenção.

### 7.1 `imedto_admins` (global — sem `estabelecimento_id`)

- `id (uuid)` PK
- `email (citext)` único, indexed
- `senha_hash (text)`
- `ativo (bool)` default true
- `force_password_reset (bool)` default false
- `criado_em (timestamptz)`, `criado_por (uuid nullable)` (auto-referência a `imedto_admins.id`; null para o seed inicial)
- `desativado_em (timestamptz nullable)`, `desativado_por (uuid nullable)`
- `ultimo_login_em (timestamptz nullable)`
- **Índices**: `(email)` único parcial `WHERE ativo`, `(ativo)`.

### 7.2 `imedto_admin_refresh_tokens` (global)

- `id (uuid)` PK
- `admin_id (uuid)` FK → `imedto_admins`, indexed
- `token_hash (text)` (sha256 do token, não o token em claro)
- `expira_em (timestamptz)`
- `revogado_em (timestamptz nullable)`
- `ip (inet nullable)`, `user_agent (text nullable)`
- **Índices**: `(admin_id, expira_em)`, `(token_hash)` único.

### 7.3 `imedto_admin_audit_log` (global, append-only, retenção 2 anos)

- `id (uuid)` PK
- `admin_id (uuid)` FK
- `acao (text)` (enum: `LOGIN_OK`, `LOGIN_FAIL`, `LOGOUT`, `CRIAR_ADMIN`, `DESATIVAR_ADMIN`, `RESETAR_SENHA_ADMIN`, `ABRIR_DETALHE_TENANT`, `REVELAR_CPF_DONO`, `RESETAR_TENANT`, `CRIAR_PLANO`, `EDITAR_PLANO`, `ALTERAR_ASSINATURA`, `CONCEDER_GRATUIDADE`)
- `recurso_tipo (text)`, `recurso_id (text)` (text pra aceitar uuid e bigint)
- `tenant_afetado_id (bigint nullable)`
- `motivo (text nullable)` (obrigatório no domínio para mutações destrutivas)
- `ip (inet nullable)`, `user_agent (text nullable)`
- `payload_resumo (jsonb nullable)` (sem PII)
- `criado_em (timestamptz)` default `now()`
- **Índices**: `(admin_id, criado_em desc)`, `(tenant_afetado_id, criado_em desc)`, `(acao, criado_em desc)`.
- **Política de retenção**: documentada em `Docs/LGPD.md`. Job de limpeza não é parte do MVP — vira backlog quando volume justificar.

### 7.4 `planos` (global)

- `id (uuid)` PK
- `nome (text)` único
- `descricao_curta (text nullable)`
- `preco_mensal_centavos (int nullable)` (null = preço sob consulta / customizado)
- `gratuito (bool)` default false (flag de catálogo, não confundir com "concessão de gratuidade")
- `ativo (bool)` default true (plano fora de catálogo não pode ser atribuído a novas assinaturas)
- `criado_em`, `criado_por (uuid)` FK → `imedto_admins`
- **Índices**: `(nome)` único, `(ativo)`.
- **Não é multi-tenant** (catálogo global).

### 7.5 `assinaturas` (global, histórico imutável)

- `id (uuid)` PK
- `estabelecimento_id (bigint)` FK, indexed
- `plano_id (uuid)` FK → `planos`
- `iniciada_em (timestamptz)`
- `fim_em (timestamptz nullable)` (null = vigente)
- `gratuita (bool)` default false
- `motivo (text nullable)` (obrigatório se `gratuita = true`)
- `criada_por (uuid nullable)` FK → `imedto_admins` (null para assinaturas geradas por self-signup)
- `criada_em (timestamptz)`
- **Regra de domínio**: alterar plano = inserir nova linha + fechar `fim_em` da anterior em transação. Nunca atualizar in-place.
- **Índices**: `(estabelecimento_id, fim_em nulls first)` para "qual a assinatura vigente?", `(plano_id)`.

### 7.6 `imedto_config` (global, key-value)

- `chave (text)` PK
- `valor (jsonb)`
- `descricao (text nullable)`
- `atualizado_em`, `atualizado_por (uuid nullable)` FK → `imedto_admins`
- MVP entrega só a tabela + leitura via `IImedtoConfigReader`. Sem UI.

### 7.7 Migration de bootstrap

Migration condicional `20260530XXXXXX_seed_imedto_admin_dev.sql`:
```sql
DO $$
BEGIN
  IF current_setting('app.environment', true) = 'Development' THEN
    INSERT INTO imedto_admins (id, email, senha_hash, ativo, force_password_reset, criado_em)
    VALUES (gen_random_uuid(), 'admin@imedto.com', '<hash de 123123>', true, false, now())
    ON CONFLICT (email) DO NOTHING;
  END IF;
END $$;
```
> DB agent escolhe o algoritmo de hash final (consistente com `usuarios`). `app.environment` setado no boot da API via `SET app.environment = 'Development'` na primeira conexão da sessão de migration — DB agent ajusta se preferir outro mecanismo (ex.: GUC do appsettings, ou seed em código C# rodando só se `env.IsDevelopment()`).

---

## 8. Critérios de Aceite (CAs)

> Todos os CAs são **Dado / Quando / Então** verificáveis. QA valida cada um com evidência.

### Auth admin

- **CA1** (login válido): Dado `admin@imedto.com / 123123` em dev, Quando POST `/api/admin/auth/login`, Então recebe 200 com `accessToken` (15min) com claim `imedto_admin = "true"` e `sub = <uuid>`, refresh token setado em cookie HttpOnly de 2h, e linha `LOGIN_OK` em `imedto_admin_audit_log`.
- **CA2** (login inválido): Dado e-mail inexistente OU senha errada, Quando POST `/api/admin/auth/login`, Então recebe 401 com mensagem genérica `"Credenciais inválidas"` (sem distinguir e-mail vs. senha), e linha `LOGIN_FAIL` em audit (sem expor o e-mail tentado em log de aplicação).
- **CA3** (refresh): Dado refresh válido em cookie, Quando POST `/api/admin/auth/refresh`, Então recebe novo access token e o refresh é rotacionado (novo `token_hash` em `imedto_admin_refresh_tokens`, antigo marcado `revogado_em`).
- **CA4** (logout): Dado admin logado, Quando POST `/api/admin/auth/logout`, Então cookie é limpo, refresh é marcado `revogado_em`, linha `LOGOUT` em audit.
- **CA5** (inatividade): Dado admin logado no front sem mover mouse/teclado por 15min, Quando timer dispara, Então `adminAuthStore.logout()` é chamado e usuário vai para `/admin/login`.
- **CA6** (primeiro login força reset): Dado admin com `force_password_reset = true`, Quando faz login com sucesso, Então recebe 200 mas com flag `must_reset_password = true` no payload, e o front redireciona obrigatoriamente para `/admin/auth/redefinir-senha` antes de liberar qualquer outra tela.
- **CA7** (política de senha em prod): Dado fluxo de reset/criação em produção, Quando admin tenta gravar senha `123` ou `password`, Então recebe 422 com mensagem: `"Senha deve ter no mínimo 10 caracteres, incluindo maiúscula, minúscula, número e caractere especial."`. Em Development, a política é apenas comprimento ≥ 6 para facilitar dev (DB agent + dev confirmam mecanismo de toggle por env).

### Multi-tenant blindagem

- **CA8** (usuário comum em admin = 403): Dado JWT de usuário comum (`estabelecimento_id` presente, sem claim `imedto_admin`), Quando GET `/api/admin/estabelecimentos`, Então recebe 403 com mensagem genérica `"Acesso negado."`.
- **CA9** (admin em rota normal = 403): Dado JWT com `imedto_admin = "true"`, Quando GET `/api/agenda` (ou qualquer outro endpoint normal), Então recebe 403, mesmo se o token tiver `sub` válido. Admin não opera o app cliente.
- **CA10** (isolamento físico do reset): Dado admin executando reset do tenant A, Quando o `AdminResetService` roda, Então não há nenhum filtro automático de `IEstabelecimentoContext` herdado de sessão admin — o `estabelecimento_id` vem **explicitamente** do parâmetro do endpoint, validado por existência prévia.

### RBAC (MVP all-or-nothing)

- **CA11** (todo admin tem todos os poderes): Dado qualquer admin ativo, Quando acessa qualquer rota `/api/admin/*` autorizada pela policy, Então o acesso é concedido sem checagem adicional de papel. MVP é flat.
- **CA12** (toda rota admin valida claim): Dado um endpoint novo em `Controllers/Admin/`, Quando QA grepa `[Authorize(Policy = "ImedtoAdmin")]` em todos os controllers admin, Então 100% dos endpoints (exceto `/auth/login` e `/auth/refresh`) carregam a anotação.

### Audit

- **CA13** (mutação sem motivo = 422): Dado POST `/api/admin/estabelecimentos/{id}/reset` SEM `motivo` (ou string vazia/whitespace), Quando o handler valida, Então recebe 422 com `"Motivo é obrigatório para operações destrutivas."`. Vale também para `conceder-gratuidade` e `alterar-plano` quando `gratuita = true`.
- **CA14** (mutação gera linha de audit): Dado qualquer mutação admin bem-sucedida (criar admin, desativar, reset tenant, criar plano, alterar assinatura, conceder gratuidade), Quando a transação commita, Então existe exatamente 1 linha em `imedto_admin_audit_log` com `acao` correspondente, `admin_id`, `recurso_tipo`, `recurso_id`, `motivo`, `ip`, `user_agent` populados.
- **CA15** (leitura de detalhe gera audit): Dado admin abre detalhe de estabelecimento (GET `/api/admin/estabelecimentos/{id}`), Então gera linha `ABRIR_DETALHE_TENANT`. Reveal de CPF gera linha `REVELAR_CPF_DONO` adicional.
- **CA16** (listagem NÃO gera audit): Dado GET `/api/admin/estabelecimentos?page=1`, Então **nenhuma** linha é gerada em audit.

### LGPD

- **CA17** (zero campo de paciente em DTO admin): Dado QA inspeciona TODOS os DTOs sob `Imedto.Backend.Application/Admin/`, Quando busca por nomes `paciente`, `prontuario`, `evolucao`, `cpf_paciente`, Então não encontra nenhuma referência.
- **CA18** (CPF de dono mascarado por padrão): Dado GET `/api/admin/estabelecimentos/{id}`, Então `cpfDono` no payload retorna `"***.***.***-XX"` (apenas 2 últimos dígitos). Para revelar, admin chama POST `/api/admin/estabelecimentos/{id}/revelar-cpf-dono` com `motivo` obrigatório.
- **CA19** (reveal de CPF audita): Dado admin chama o endpoint de reveal, Então recebe o CPF completo na resposta E gera linha `REVELAR_CPF_DONO` em audit com `motivo`.
- **CA20** (sem PII em log de aplicação): Dado erro em qualquer fluxo admin (login fail, reset fail, etc.), Quando QA inspeciona `Serilog` output, Então não há e-mail, CPF, nome de paciente ou senha em log. Apenas IDs (uuid/bigint) e códigos.

### Estados de erro (front)

- **CA21** (lista vazia): Dado nenhum estabelecimento cadastrado (ou filtro retorna 0), Quando lista carrega, Então renderiza `AppEmptyState` com texto `"Nenhum estabelecimento encontrado."`.
- **CA22** (rede): Dado API offline, Quando lista tenta carregar, Então renderiza `AppErrorState` com botão `"Tentar novamente"`.
- **CA23** (401 expirado): Dado access token expirado, Quando front faz request, Então interceptor tenta refresh; se refresh falhar, derruba para `/admin/login` com toast `"Sessão expirada. Faça login novamente."`.
- **CA24** (403): Dado admin acessa `/api/admin/admins` após ter sido desativado em paralelo, Quando recebe 403, Então front exibe `"Acesso revogado."` e força logout.
- **CA25** (422 com BusinessException): Dado tentativa de desativar último admin ativo, Quando 422 retorna, Então front exibe a mensagem do back diretamente (genérica, sem PII).

### Performance

- **CA26** (lista < 500ms a 10k tenants): Dado 10.000 estabelecimentos no banco, Quando GET `/api/admin/estabelecimentos?page=1&pageSize=25`, Então resposta volta em < 500ms (p95) — DB agent garante índices `(nome)`, `(created_at desc)` e índice trigram em `nome` se busca textual ILIKE for usada.
- **CA27** (paginação 25/pg padrão, limite 100): Dado `pageSize > 100`, Quando GET, Então o back força `pageSize = 100`. `pageSize` default = 25.
- **CA28** (debounce em busca): Dado input de busca, Quando usuário digita, Então usa `useDebouncedRef` (~300ms) antes de disparar request.

### Reset de tenant

- **CA29** (confirmação dupla): Dado admin clica em "Resetar dados" no detalhe do tenant A (nome `Clínica Sol`), Quando abre dialog, Então só libera botão `"Confirmar reset"` após admin digitar exatamente `Clínica Sol` no input de confirmação **e** preencher campo `motivo` (≥ 10 chars).
- **CA30** (reuso do AdminResetService): Dado reset confirmado, Quando handler executa, Então delega para `IAdminResetService.ResetEstabelecimentoAsync` existente, passando `adminId` (uuid de `imedto_admins`) no parâmetro que hoje recebe `userId`. Lógica de quais tabelas apagar não é re-implementada.
- **CA31** (audit do reset): Após reset OK, Então existe linha `RESETAR_TENANT` em audit com `tenant_afetado_id = id`, `motivo` populado, `payload_resumo = {"modulos": [...]}`.

### Planos e assinaturas

- **CA32** (criar plano): Dado POST `/api/admin/planos` com `{nome, descricao_curta, preco_mensal_centavos, gratuito, ativo}`, Quando handler valida, Então grava em `planos`, gera audit `CRIAR_PLANO`, retorna 201 com payload do plano criado.
- **CA33** (nome único): Dado plano `"Profissional"` já existe ativo, Quando POST com mesmo nome, Então recebe 422 `"Já existe um plano com este nome."`.
- **CA34** (alterar plano de tenant preserva histórico): Dado tenant A com assinatura vigente plano X, Quando POST `/api/admin/estabelecimentos/{id}/assinaturas` com `plano_id = Y`, Então em uma única transação: linha antiga recebe `fim_em = now()`, nova linha é inserida com `iniciada_em = now()` e `fim_em = null`, audit `ALTERAR_ASSINATURA` é gerado com `payload_resumo = {"plano_antigo":"X","plano_novo":"Y"}`.
- **CA35** (conceder gratuidade): Dado POST `/api/admin/estabelecimentos/{id}/assinaturas/gratuidade` com `{motivo, data_fim?}`, Quando handler valida `motivo` ≥ 10 chars, Então cria nova linha em `assinaturas` com `gratuita = true`, `plano_id` apontando para plano "Gratuidade vitalícia" (seed inicial), `motivo` populado. Audit `CONCEDER_GRATUIDADE` com payload `{"data_fim":"..."}`.
- **CA36** (gratuidade sem motivo): Dado payload sem `motivo` ou com `motivo` curto, Quando POST, Então 422 `"Motivo é obrigatório para concessão de gratuidade (mínimo 10 caracteres)."`.

### CRUD de admins

- **CA37** (criar admin): Dado admin logado, Quando POST `/api/admin/admins` com `{email, nome}`, Então cria com senha temporária aleatória (20 chars), `force_password_reset = true`, envia e-mail via `Resend` para o novo admin com link para login + senha temporária, audit `CRIAR_ADMIN` gerado.
- **CA38** (e-mail duplicado): Dado e-mail já existe (ativo ou inativo), Quando POST, Então 422 `"Já existe um admin com este e-mail."`.
- **CA39** (desativar admin): Dado POST `/api/admin/admins/{id}/desativar` com `motivo`, Então marca `ativo = false`, `desativado_em = now()`, `desativado_por = <admin_id_chamador>`, revoga todos os refresh tokens dele em `imedto_admin_refresh_tokens`, audit `DESATIVAR_ADMIN`.
- **CA40** (trava do último admin ativo): Dado existe apenas 1 admin com `ativo = true`, Quando tenta desativar esse admin, Então 422 `"Não é possível desativar o último administrador ativo do sistema."`. Vale também para tentativa de auto-desativação se o admin é o único ativo.
- **CA41** (auto-desativação permitida se houver outro): Dado existem 2+ admins ativos, Quando admin desativa a si mesmo, Então operação OK; refresh é revogado; próxima request do admin recebe 403.
- **CA42** (resetar senha de outro admin): Dado POST `/api/admin/admins/{id}/resetar-senha`, Então gera senha temporária, atualiza `senha_hash`, marca `force_password_reset = true`, revoga refresh tokens, envia e-mail, audit `RESETAR_SENHA_ADMIN`.
- **CA43** (listar admins): Dado GET `/api/admin/admins`, Então retorna lista paginada (25/pg) com `{id, email, nome, ativo, force_password_reset, criado_em, ultimo_login_em}`. **Sem audit.**

### Bootstrap

- **CA44** (seed só em Development): Dado app sobe em Development pela primeira vez (banco vazio), Quando migrations rodam, Então existe `admin@imedto.com / 123123` em `imedto_admins`, `ativo = true`, `force_password_reset = false`.
- **CA45** (seed NÃO roda em prod): Dado app sobe em Production com banco vazio, Quando migrations rodam, Então `imedto_admins` está vazia.
- **CA46** (CLI em prod): Dado `dotnet run -- seed-admin --email contato.imedto@gmail.com`, Quando comando executa, Então: imprime no terminal `Senha temporária: <random 20 chars>`, insere em `imedto_admins` com `force_password_reset = true`, audit `CRIAR_ADMIN` com `admin_id = self`, `recurso_tipo = "admin"`, `recurso_id = <id>`, `motivo = "Bootstrap CLI"`.

### Frontend isolado

- **CA47** (módulo não importa de fora): Dado QA roda grep `from ['"]@/views\|@/stores\|@/services\|@/composables` em `frontend/src/modules/admin/`, Quando inspeciona resultados, Então só são permitidos imports de `@/components/ui/` (design system) e `@/composables/useDebouncedRef`. Qualquer outro import cruzado é falha.
- **CA48** (rota guard separado): Dado usuário não autenticado acessa `/admin/estabelecimentos`, Quando guard de rota dispara, Então redireciona para `/admin/login`. Guard do app principal não interfere em rotas `/admin/*` (e vice-versa).
- **CA49** (banner em prod): Dado build de produção, Quando admin acessa qualquer rota `/admin/*`, Então renderiza banner vermelho no topo com texto `"Área administrativa — uso interno Imedto"`.
- **CA50** (banner em dev): Dado build de dev, Quando admin acessa, Então banner amarelo com texto `"Área administrativa (DEV) — uso interno Imedto"`.

### Documentação viva

- **CA51** (`Docs/ARQUITETURA.md` atualizado): Dado QA inspeciona `Docs/ARQUITETURA.md` na entrega, Quando busca seção `"Área Admin Global"`, Então encontra: claim `imedto_admin`, policy `ImedtoAdmin`, namespace `/api/admin/*`, separação física (Controllers/Admin/*, modules/admin/*), regra de blindagem cruzada, fluxo de bootstrap (dev vs prod).
- **CA52** (`Docs/LGPD.md` atualizado): Dado QA inspeciona `Docs/LGPD.md`, Quando busca seção `"Acesso de admin global"`, Então encontra: política de zero PII de paciente no MVP, mascaramento de CPF de dono, audit obrigatório por mutação + leitura de detalhe, retenção 2 anos, regra de sem PII em log.

---

## 9. Pontos de extensão futura (preparação para extrair em projeto separado)

Checklist a ser respeitado pelo dev durante a implementação:

- [ ] Zero import cruzado: `frontend/src/modules/admin/` → fora dele, exceto design system.
- [ ] Zero import cruzado: `backend/.../Admin/` → fora dele, exceto `SharedKernel`, `LocalJwt` e `IAdminResetService` (já isolado).
- [ ] Tabelas admin (`imedto_admins`, `imedto_admin_refresh_tokens`, `imedto_admin_audit_log`, `planos`, `assinaturas`, `imedto_config`) sem dependência de schema do tenant — usam apenas FKs para `estabelecimentos.id` (referência fraca, futuramente substituível por integração).
- [ ] `ImedtoAdminTokenIssuer` é wrapper, não acoplamento — pode ser substituído por outro emissor sem mexer no resto.
- [ ] Audit é tabela própria (`imedto_admin_audit_log`), não compartilha com audit do app principal (se existir no futuro).
- [ ] README do módulo `frontend/src/modules/admin/README.md` documenta regras de isolamento + roadmap de extração.

---

## 10. Riscos e mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Escape de privilégio (admin global vira usuário comum por engano) | Baixa | Crítico | Blindagem cruzada (CA8, CA9) com testes; nunca decidir tenant a partir de claim admin. |
| Reset destrutivo errado (apagou tenant errado) | Média | Alto | Confirmação dupla com digitação do nome + motivo (CA29); audit completo (CA31). Backup do RDS continua sendo a rede de segurança. |
| Senha fraca em prod por descuido | Média | Alto | Política de senha validada no back (CA7), força reset no primeiro login (CA6). |
| Refresh token vazado vira sessão eterna | Baixa | Alto | Rotação obrigatória no refresh (CA3); revogação no logout (CA4) e desativação (CA39); janela curta (2h). |
| Volume da audit log explode em 6-12 meses | Baixa | Médio | Listagem NÃO gera audit (CA16); retenção 2 anos documentada; job de limpeza vira backlog quando necessário. |
| Acoplamento acidental ao app principal dificulta extração | Média | Médio | CA47 + checklist §9 validados pelo QA. |
| `assinaturaStore` do front (já existente em `frontend/src/stores/assinaturaStore.ts`) tentar ser reusado dentro do admin | Média | Médio | Decisão: **NÃO reusar**. Admin tem seu próprio `adminPlanosStore` e `adminAssinaturasStore`. O `assinaturaStore` antigo é do POV do cliente final, não do admin. Dev cria duplicata se necessário, com nota no README do módulo. |

---

## 11. Próximos briefings sugeridos (backlog priorizado)

1. **MFA TOTP para admin** — antes de abrir acesso a terceiros.
2. **Impersonate** — entrar como cliente com motivo + janela limitada + audit + banner persistente "Você está vendo como X".
3. **RBAC granular** — perfis de admin (read-only suporte vs. full).
4. **Catálogos globais** — modelos de prontuário padrão, exames físicos padrão, variáveis pool.
5. **Dashboard de métricas** — MRR estimado, ativações, churn, tenants em risco.
6. **IP allow-list** — restringir login admin por CIDR.
7. **Billing real** — cobrança automatizada via gateway.
8. **Fila Resend / status de e-mails** — observar entregabilidade.
9. **Logs centralizados** — visualizar erros do sistema agregados.
10. **Job de limpeza de audit > 2 anos** — quando volume justificar.

---

## 12. Atualizações em `Docs/` exigidas (executadas pelo `imedto-developer`, validadas pelo `imedto-qa`)

### `Docs/ARQUITETURA.md`

Adicionar nova seção `## Área Admin Global` contendo:

- **Visão geral**: área administrativa para suporte e gestão do produto, fisicamente isolada do app principal, com tabela de admins própria (`imedto_admins`) e claim JWT distinta (`imedto_admin = "true"`).
- **Endpoints**: namespace `/api/admin/*`. Controllers em `backend/src/Services/Imedto.Backend.API/Controllers/Admin/`. Toda rota carrega `[Authorize(Policy = "ImedtoAdmin")]` exceto `auth/login` e `auth/refresh`.
- **Frontend**: módulo isolado em `frontend/src/modules/admin/`. Rota `/admin/*` com router e guard separados. Sem imports cruzados (regra documentada no README do módulo).
- **Auth admin**: reuso de `LocalJwtAuthService` (mesma chave ECDSA P-256) parametrizado via `ImedtoAdminTokenIssuer`. Access 15min, refresh 2h, inatividade 15min no front. Refresh rotacionado a cada uso; tokens hash-armazenados em `imedto_admin_refresh_tokens`.
- **Blindagem cruzada (não-negociável)**: JWT admin **nunca** carrega `estabelecimento_id`. Endpoints normais rejeitam (403) qualquer JWT com `imedto_admin = true`. Endpoints admin rejeitam (403) qualquer JWT sem essa claim. Admin não é "usuário com superpoderes" — é universo separado.
- **Audit**: writer único `ImedtoAdminAuditWriter` invocado por todos os handlers de mutação e leitura de detalhe. Tabela `imedto_admin_audit_log` é append-only, retenção 2 anos.
- **Bootstrap**: dev via seed de migration condicional (`admin@imedto.com / 123123`); prod via comando CLI `dotnet run -- seed-admin --email X` com senha temporária aleatória + `force_password_reset = true`.
- **Política de senha**: dev ≥ 6 chars; prod ≥ 10 chars com maiúscula+minúscula+número+especial.
- **Banner visual**: vermelho em prod, amarelo em dev.
- **Roadmap de extração**: checklist em §9 do briefing `2026-05-30_001`.

### `Docs/LGPD.md`

Adicionar nova seção `## Acesso de admin global` contendo:

- **Princípio**: admin global é exceção forte ao multi-tenant. Mantemos minimização e auditabilidade rigorosas.
- **Zero PII de paciente no MVP**: DTOs admin não retornam dados de paciente/prontuário/evolução. Acesso a caso real só por impersonate (backlog) com motivo + audit + janela limitada.
- **CPF de dono mascarado por padrão**: `"***.***.***-XX"`. Endpoint dedicado de reveal exige motivo e gera audit `REVELAR_CPF_DONO`.
- **Audit obrigatório**: toda mutação admin e toda leitura de detalhe individual de tenant gera linha em `imedto_admin_audit_log`. Listagem não gera (volume sem valor forense).
- **Conteúdo do audit**: `admin_id`, `acao`, `recurso_tipo`, `recurso_id`, `tenant_afetado_id`, `motivo`, `ip`, `user_agent`, `payload_resumo` (JSON sem PII), `criado_em`.
- **Retenção**: 2 anos para audit log admin. Job de limpeza fora do MVP (backlog #10).
- **Sem PII em log de aplicação**: Serilog admin loga apenas IDs (uuid/bigint) e códigos. E-mails de admin tentado em login (CA2) NÃO vão para log de aplicação — vão apenas para audit no banco.
- **Mensagens genéricas**: 401 `"Credenciais inválidas"` sem distinguir e-mail vs. senha. 403 `"Acesso negado."` sem revelar motivo. 404 não usado em admin (somos all-or-nothing).

---

## 13. Hand-off

**Próximo agente**: `imedto-database`.

**Escopo da tarefa do `imedto-database`**:

1. Modelar e gerar migration EF Core + SQL idempotente em `db/migrations/` para as 6 tabelas:
   - `imedto_admins`
   - `imedto_admin_refresh_tokens`
   - `imedto_admin_audit_log`
   - `planos`
   - `assinaturas`
   - `imedto_config`
2. Definir tipos exatos, defaults, constraints, índices (incluindo trigram em `estabelecimentos.nome` se busca ILIKE for usada pela lista admin — confirmar com dev).
3. Migration condicional de seed Development: `admin@imedto.com / 123123` (hash consistente com `usuarios`).
4. Seed inicial de plano `"Gratuidade vitalícia"` (gratuito = true, ativo = true).
5. Validar via psql/MCP RDS que migrations rodam limpas em banco vazio E sobre banco existente (idempotência).
6. Atualizar `Docs/INFRA.md` ou `Docs/COMANDOS.md` se introduzir extensão Postgres nova (ex.: `pg_trgm` se ainda não habilitada).
7. **Não implementa endpoints/handlers** — devolve ao `imedto-developer` com schema pronto.

**Depois**: `imedto-developer` implementa backend + frontend conforme CAs, atualiza `Docs/ARQUITETURA.md` e `Docs/LGPD.md` (§12), e aciona `imedto-qa` para validação.

**Depois**: `imedto-qa` valida cada CA com evidência (chrome-devtools MCP + suíte automatizada + grep estrutural para isolamento), classifica eventuais bugs (Tipo A volta dev, Tipo B escala BA), commita e empurra.
