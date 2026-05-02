# Auditoria LGPD — Módulo Auth + Usuarios (Fase 3.3)

**Data**: 2026-05-02
**Escopo**: Auth (`Domain/Auth`, `Infrastructure/Auth`, `AuthController`) + Usuarios (`Domain/Usuarios`, `Application/Usuarios`, `UsuarioController`, `MinhaContaController`) + reflexo no front (`authStore`, `usuarioService`, `LoginView`).

> Auth/Usuarios é o **plano de controle** de identidade do sistema. Toda operação clínica passa pelo `sub` do JWT que sai daqui. Auditoria com a mesma régua das Fases 3.1/3.2 — tolerância zero a vazamento, enumeração ou IDOR.

## Sumário

| Categoria | Achados |
|---|---|
| Campos PII vazando em DTO/payload | **5** (1 crítico — service_role_key como apikey default; 1 alto — CPF/Telefone em /me-/refresh) |
| Logs com PII (email) | **5** (`SupabaseAuthService` × 4 + `UsuarioCriadoEventHandler` × 1) |
| Endpoints de auth/PII sem rate limit | **5** (cpf-disponivel, minha-conta GET/DELETE, auth/me, usuario/* PATCH/POST) |
| Vetores de enumeração | **2** (CPF via `motivo` da query; e-mail via mensagem `BusinessException` no signup) |
| Gaps de autorização / IDOR | **2** (handlers de Usuarios aceitam `UsuarioId` arbitrário no command; `RegistrarAuth` envia `service_role_key` em todas as chamadas) |
| Audit trail (LGPD Art. 37) | **5 lacunas** (AtualizarPerfil, CompletarOnboarding, Anonimizar, Exportar, RegistrarConsentimento) |
| Bugs latentes | **3** (KeyNotFoundException → 500; refresh declara 401 mas devolve 422; Anonimizar não nulifica e-mail) |
| Status do logout | **fire-and-forget no servidor** — token continua válido até `expiresAt` (≈1 h) se Supabase falhar |

---

## 1. DTOs e payloads de resposta — minimização

### 1.1 — `AuthController.Me` / `AuthController.Refresh` (linhas 136-149, 185-198)

Payload retornado:

```
{ id, email, nomeCompleto, cpf, telefone, status, onboardingCompleto, ultimoAcessoEm }
```

| Campo | Front consome? | Severidade | Ação |
|---|---|---|---|
| `id` | sim (`authStore.usuario.id`) | — | manter |
| `email` | sim (avatar/footer) | — | manter |
| `nomeCompleto` | sim | — | manter |
| **`cpf`** | front só checa `!== null` para gating de onboarding (`onboardingPendente` já existe) | **alto LGPD** | **trocar por boolean `temCpf`** ou omitir. CPF crú no payload de toda chamada `/me` e `/refresh` (chamadas em **toda navegação**) é exposição contínua a vazamento via XSS, telemetria, log de browser, screenshot. CPF é dado pessoal sensível por correlação. |
| **`telefone`** | usado em tela de "minha conta" só nessa view; chega em `/me` indevidamente | **médio LGPD** | mover para endpoint dedicado `GET /api/usuario/me/contato` chamado só na tela de perfil. Mesma justificativa do CPF — minimização Art. 6º III. |
| `status` | sim (gate onboarding) | — | manter |
| `onboardingCompleto` | sim | — | manter (este já é a info que o front usa para gate, CPF não precisa) |
| `ultimoAcessoEm` | **não lido** | baixo | remover |

### 1.2 — `AuthController.Login` / `Signup` retornam `signup.User` cru (`UserInfo`)

`UserInfo` (Domain/Auth/AuthResult.cs:14-18) tem `Roles: string[]`. Em login (linha 105-107) e signup (linha 65-78), o objeto inteiro vai pro front:

```
{ usuario: { Id, Email, Roles: [...] } }
```

| Campo | Severidade | Ação |
|---|---|---|
| `Roles` (string[] vindo de `app_metadata.roles` no Supabase Auth) | **médio LGPD/segurança** — frontStore (`Usuario` interface, `authStore.ts:14-23`) **NÃO tem o campo**, então é payload "fantasma" descartado pelo TypeScript. Mas vai pelo wire e fica em log/dev tools. | substituir o retorno por `{ usuario: { id, email } }` ou `{ usuario: usuarioLocal }` (registro local após `CriarRegistroLocalUsuarioCommand`). Dois benefícios: (a) consistência com `/me` e `/refresh` que retornam o registro local; (b) não vaza role schema do Supabase para o cliente. |

### 1.3 — `AuthController.Login`/`Signup` em **dev** retornam `accessToken` no body (linha 105, 75)

Documentado no controller (`if (_env.IsDevelopment())`). **OK** para dev/Swagger. Riscos:
- Se `ASPNETCORE_ENVIRONMENT` for incorretamente setado em staging/preview Vercel → token vaza no body.
- Recomendação **defesa-em-profundidade**: também checar `Request.Host` ou um flag `Auth:ExposeAccessTokenInBody=true` explícito em `appsettings.Development.json` para evitar mistake em config.

### 1.4 — `MeusDadosLgpdDto` (`Contracts/Lgpd/Queries/MeusDadosLgpdDto.cs`)

| Campo | Tela usa? | Severidade | Ação |
|---|---|---|---|
| `Email`, `NomeCompleto` | sim (export do titular) | — | manter (titular pediu seus dados) |
| `Profissional.Crm` | sim | — | manter |
| `Vinculos.EstabelecimentoId` (long) | sim (export) | baixo | manter — titular precisa saber a quais clínicas está vinculado |
| `Notificacoes.Titulo` | sim | médio | revisar — títulos de notificação podem conter PII de outros pacientes ("Lembrete: Maria S. agendou para 10h"). Garantir que `Titulo` no banco já está desidentificado. |

### 1.5 — `SupabaseOptions.ServiceRoleKey` enviado como `apikey` em **todas** as chamadas REST

`Infrastructure/Container.cs:119-124`:

```csharp
services.AddHttpClient("supabase", (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<SupabaseOptions>>().Value;
    client.BaseAddress = new Uri(opts.Url);
    client.DefaultRequestHeaders.Add("apikey", opts.ServiceRoleKey);  // <-- CRÍTICO
});
```

**CRÍTICO LGPD/segurança**:
- `apikey` default = `ServiceRoleKey` significa que **toda** chamada `/auth/v1/signup`, `/auth/v1/token`, `/auth/v1/recover` etc. apresenta a chave de admin para o Supabase.
- Por design do Supabase, o `apikey` em `/auth/v1/*` **deve ser o anon key** (público) — o `service_role` só deve ser usado em rotas `/auth/v1/admin/*` (`generate_link`, `admin/users`, `admin/users/{id}`).
- Risco operacional: se um dia o Auth do Supabase introduzir hooks/restrições baseadas no `apikey` (rate limit, captcha, audit), enviar `service_role_key` **bypassa toda essa proteção** silenciosamente — perdemos defesa-em-profundidade da própria Supabase.
- Risco de exposição: o `ServiceRoleKey` é o cofre. Se vazar (log do HttpClient com header dump, telemetria OTel com headers, mensagem de erro upstream com echo de header), é game-over: atacante pode listar/excluir todo usuário, ler todo dado bypassando RLS.

**Ação obrigatória**:
1. Adicionar `SupabaseOptions.AnonKey` (público) e usar **anon key** como apikey default.
2. `service_role_key` só é setada por chamada quando explicitamente necessário (já é assim para o `Authorization: Bearer` em `DeleteUserAsync`, `CriarConviteAsync`, `BuscarUsuarioPorEmailAsync` — basta adicionar o header `apikey` separadamente nessas 3 rotas, ou criar 2 named clients: `"supabase-anon"` e `"supabase-admin"`).

---

## 2. Logs com PII

### 2.1 — `SupabaseAuthService` loga **email** em 4 lugares

| Linha | Trecho | Severidade | Ação |
|---|---|---|---|
| 44 | `_logger.LogWarning("Signup falhou para {Email}: HTTP {Status} — {Body}", email, ...)` | **alto LGPD** | trocar por hash SHA-256 truncado do email (já há precedente: hash de IP no `Program.cs:277`). Bonus: `{Body}` pode também conter o email de volta — sanitizar. |
| 80 | `_logger.LogWarning("Login falhou para {Email}: HTTP {Status}", email, ...)` | **alto LGPD** — em fluxo de brute force, log enche de emails reais de tentativa | hash do email |
| 168 | `_logger.LogWarning("Convite falhou para {Email}: HTTP {Status} — {Body}", ...)` | alto | hash do email |
| 209 | `_logger.LogWarning(ex, "Falha ao enviar recuperação de senha para {Email} (ignorado).", email)` | **alto LGPD** | hash do email — pior caso aqui: log mostra "tentei enviar reset para fulano@x.com" → vaza enumeração mesmo no log |

> Nota: o `Body` do erro do Supabase frequentemente inclui o e-mail de volta. Para logs, usar `_logger.BeginScope` com `EmailHash` e omitir o `Body` cru em prod (ou truncar/sanitizar). Em dev pode permanecer.

### 2.2 — `UsuarioCriadoEventHandler.cs:22-24`

```csharp
_logger.LogInformation(
    "Usuário criado: Id={UsuarioId}, Email={Email}, OcorridoEm={OcorridoEm}",
    domainEvent.UsuarioId, domainEvent.Email, domainEvent.OcorridoEm);
```

**Alto LGPD**. Email é PII. Trocar por `Id` apenas (já é único e correlacionável) ou hash do email. Severidade alta porque este log roda em **toda criação de usuário** → fica em log structured (Datadog/CloudWatch) por meses.

### 2.3 — `Program.cs:124-141` (`OnAuthenticationFailed`)

Já implementado com `tokenPreview = authHeader[..20] + "…"` — **boa prática preservada**. Comentário explícito menciona LGPD. ✅

### 2.4 — `Program.cs:266-292` (rate limit `OnRejected`)

Hash SHA-256 truncado do IP (`hashIp`). ✅ **Padrão ouro** — replicar para SupabaseAuthService.

---

## 3. Mensagens de erro / vetores de enumeração

### 3.1 — Enumeração de e-mail no signup

`SupabaseAuthService.cs:47-48`:

```csharp
if (... && erro.Contains("already", StringComparison.OrdinalIgnoreCase))
    throw new BusinessException("Já existe uma conta com este e-mail.");
```

**Médio LGPD**. Permite "user-discovery" via signup: atacante envia signup, vê 422 "já existe" → confirma que `victim@empresa.com` é cliente do Imedto. Combinado com vazamento jornalístico/médico, expõe relação com sistema de saúde.

**Mitigação**:
- Login e ForgotPassword **já estão genéricos** — `"Credenciais inválidas."` e `204 NoContent` independente de o e-mail existir. ✅
- Signup é o único vetor remanescente. Opções:
  - (recomendada) trocar por mensagem genérica `"Não foi possível criar a conta. Verifique os dados ou tente recuperar a senha."` — front exibe link para `/forgot-password`.
  - alternativa pragmática: aceitar o trade-off (UX > segurança) mas **rate-limitar agressivo já feito (3 req/min com `auth-sensitive`)** — ok.
- `auth-sensitive` permite 3 tentativas / 60s / IP — o suficiente para enumerar 4.320 emails/dia/IP. Considerar reduzir para 1 req / 10s ou aplicar **CAPTCHA** após 1 falha.

### 3.2 — **Enumeração de CPF via `VerificarCpfDisponivelQuery`** — CRÍTICO

`/api/usuario/me/cpf-disponivel?cpf=XXXXXXXXXXX` retorna `{ valido, disponivel, motivo }`.
- `motivo = "CPF já cadastrado em outra conta."` quando ocupado.
- **Sem `[EnableRateLimiting]`** no controller (`UsuarioController.cs:69-78`).

**CRÍTICO LGPD**:
1. CPFs válidos são finitos (~2 bilhões com DV correto, mas em circulação ~250 milhões).
2. Atacante autenticado (qualquer conta criada) consegue enumerar CPFs de toda a base via 1 request HTTP por CPF — sem throttle.
3. Resposta diz se o CPF **existe na plataforma** = mapeia "fulano usa Imedto" (sistema de saúde) → vazamento de associação a sistema clínico = risco de chantagem/fraude.
4. Pior cenário: atacante registra conta nova, gera 250 milhões de CPFs com DV correto, descobre quais são clientes do Imedto.

**Ações** (em ordem de prioridade):
1. **`[EnableRateLimiting("auth-sensitive")]`** no endpoint (3 req/min/IP — já existe a policy).
2. Não devolver `motivo` distinguindo "CPF inválido" de "CPF já cadastrado" — devolver só `{ valido, disponivel }` (boolean) e tratar mensagem amigável no front via interpolação local: "esse CPF não pode ser usado".
3. Auditar **toda chamada** desse endpoint (quem? quando? CPF buscado HASHEADO). Se um usuário fizer 50 verificações em 1h, é flag de abuso.
4. Considerar mover a verificação para o **submit do onboarding** (server valida na transação) e remover o endpoint inline. Front exibe erro só no submit. Custo: UX pior, mas elimina o vetor.

### 3.3 — Outras mensagens

`SupabaseAuthService.RefreshAsync:96` → `"Sessão expirada. Faça login novamente."` ✅ genérica.
`AuthController.Me:183` → `"Registro local do usuário não encontrado."` ✅ — nunca acionada em fluxo normal (signup cria registro local).
`MinhaContaController` → não retorna mensagem com PII. ✅

---

## 4. Cookies — review

`AuthController.SetAuthCookies` (linha 220-253):

| Atributo | Access-token | Refresh-token | Status |
|---|---|---|---|
| `HttpOnly` | true | true | ✅ |
| `Secure` | true em prod / false em dev | true em prod / false em dev | ✅ |
| `SameSite` | `None` em prod, `Lax` em dev | `None` em prod, `Lax` em dev | ⚠️ — `SameSite=None` exige `Secure` (ok em prod) e amplia superfície CSRF. Justificativa documentada (frontend Vercel ↔ backend Render — domínios diferentes). **Aceitável**, mas considerar `SameSite=Strict` se topologia permitir um único origin (e.g., proxy reverso unificado em produção). |
| `Expires` access-token | `result.ExpiresAt` (Supabase, ≈1h) | — | ✅ |
| `Expires` refresh-token | — | `UtcNow.AddDays(7)` (hardcoded) | ⚠️ — Supabase pode rotacionar refresh tokens e o tempo real de validade pode divergir. Não é crítico (browser apaga após 7 dias, mas Supabase pode aceitar refresh velho até a config dele). Acceptable. |
| `Path` access-token | `"/"` (largo, ok pra SignalR) | — | ✅ documentado |
| `Path` refresh-token | — | `"/api/auth/refresh"` (escopo mínimo) | ✅ ótimo |
| `Domain` | não setado (host-only) | não setado (host-only) | ✅ — não compartilha com subdomínios |

`ClearAuthCookies` (linha 255-261) replica `Path` correto. ✅

**Defense-in-depth recomendada (médio)**: adicionar `__Host-` prefix nos nomes em prod (`__Host-access-token`) — força `Secure`, host-only, `Path=/` (válido para o access-token; **não** funciona para refresh-token por requerer `Path=/`). Browsers rejeitam silenciosamente cookies marcados `__Host-` que violem essas regras.

---

## 5. Rate limiting — gaps

| Endpoint | Policy hoje | Severidade | Ação |
|---|---|---|---|
| `POST /api/auth/signup` | `auth-sensitive` (3/min) | — | ✅ |
| `POST /api/auth/login` | `auth-login` (5/min) | — | ✅ |
| `POST /api/auth/refresh` | `auth-refresh` (10/min) | — | ✅ |
| `POST /api/auth/forgot-password` | `auth-sensitive` | — | ✅ |
| `POST /api/auth/logout` | **nenhum** | baixo | aceitável (autenticado), mas pode-se aplicar `auth-sensitive` para evitar denial-of-logout via flood |
| `GET /api/auth/me` | **nenhum** | baixo | autenticado; consumer principal é o front (1 chamada na bootstrap). Considerar policy genérica `authenticated-burst` (60/min/usuário). |
| `GET /api/usuario/me/cpf-disponivel` | **nenhum** | **CRÍTICO** | aplicar `auth-sensitive` — vetor de enumeração (item 3.2) |
| `PATCH /api/usuario/me` | **nenhum** | baixo | autenticado; aceitável |
| `POST /api/usuario/me/onboarding` | **nenhum** | baixo | autenticado e idempotente (status check no domain); aceitável |
| `GET /api/minha-conta/exportar-dados` | **nenhum** | médio LGPD | rota pesada (carrega vínculos, notificações, consentimentos). Aplicar `auth-sensitive` ou `lgpd-export` (1 req / 5 min) — evita scrape e custo de DB. |
| `DELETE /api/minha-conta` | **nenhum** | médio | aplicar `auth-sensitive` — evita anonimização acidental por bug de cliente; +`[ConfirmacaoExplicita]` (header `X-Confirma-Anonimizacao: <password do usuário ou OTP>`) |

---

## 6. Autorização — IDOR e gating

### 6.1 — `UsuarioController.AtualizarPerfil` / `CompletarOnboarding`

```csharp
[HttpPatch("me")]
public async Task<IActionResult> AtualizarPerfil([FromBody] AtualizarPerfilRequest request)
{
    var userId = Guid.Parse(User.FindFirst("sub")!.Value);
    await _commandBus.Send(new AtualizarPerfilUsuarioCommand
    {
        UsuarioId = userId,           // ← controller passa o sub do JWT
        ...
    });
}
```

**Controller está correto** — `userId` vem do JWT, não do body. ✅

**Mas o command (`AtualizarPerfilUsuarioCommand`) expõe `UsuarioId` mutável**. Se outro caller dispatch o command com `UsuarioId` arbitrário (e.g., handler interno, job, futuro endpoint admin), **edita o perfil de outro usuário**. Severidade **médio** (defense-in-depth).

**Mitigações** (escolher uma):
1. Não passar `UsuarioId` no command — handler obtém via `ICurrentUserAccessor` (similar ao `ICurrentTenantAccessor` da Fase 3.2). Padrão: comando "self-action" não declara o sujeito.
2. Marcar `UsuarioId` como `init`-only e validar no handler que `command.UsuarioId == currentUser.Id`.

Mesmo se aplica a `CompletarOnboardingUsuarioCommand` (`CompletarOnboardingUsuarioCommandHandler.cs:19`).

### 6.2 — `MinhaContaController.AnonimizarConta`

```csharp
private Guid ObterUsuarioId() => Guid.Parse(User.FindFirst("sub")!.Value);
```

✅ — sempre usa o `sub` do JWT, não aceita parâmetro do body.

`AnonimizarMinhaContaCommand` tem `UsuarioId` mutável — mesma mitigação do 6.1. **Importante aqui**: anonimização é destrutiva e irreversível, qualquer IDOR é game-over.

### 6.3 — `[Authorize]` global em `UsuarioController` e `MinhaContaController`

✅ ambos. Nenhum endpoint é `[AllowAnonymous]` indevidamente.

### 6.4 — Falta de `[RequiresPapel]` ou `[RequiresEstabelecimento]`

**Correto omitir**: estes endpoints são "self-service do titular" e não dependem de tenant. ✅ — alinhado com a premissa "usuários globais".

### 6.5 — Outros papéis em `app_metadata.roles`

`UserInfo.Roles` é parsed mas **não usado pelo backend** para autorização (autorização hoje vai via `RequiresPapel` em vínculo com estabelecimento). O array de roles vai ao frontend como ruído (item 1.2). Confirmar se é intencional ou se existe algum decisor Supabase Auth Hook que escreve nessa propriedade — se não, **considerar não popular** para reduzir superfície.

---

## 7. Audit trail (LGPD Art. 37)

Operações sensíveis hoje **NÃO REGISTRADAS** em audit:

| Operação | Hoje | Severidade | Ação |
|---|---|---|---|
| `AtualizarPerfilUsuarioCommandHandler` (mudança de nome/telefone) | sem audit | **alto LGPD** — alteração de PII do titular precisa rastro (Art. 37) | inserir em `usuario_audit` ou `lgpd_audit_log` (criar se não existe): `{ usuario_id, evento: "perfil_atualizado", campos_alterados: ["nomeCompleto", "telefone"], ip_origem, user_agent, ocorrido_em }` |
| `CompletarOnboardingUsuarioCommandHandler` (CPF entrou no sistema) | sem audit | **alto LGPD** — primeira coleta de CPF (dado pessoal sensível por correlação) | audit: `{ usuario_id, evento: "onboarding_completado", ocorrido_em, ip_origem }`. Não logar CPF cru no audit — só evento. |
| `AnonimizarMinhaContaCommandHandler` | sem audit dedicado (apenas o `MotivoAnonimizacao.DireitoEsquecimento` é gravado pelo `IAnonimizacaoService.AnonimizarPaciente` para cada paciente) | **CRÍTICO LGPD** — exclusão sob direito ao esquecimento PRECISA de prova (ANPD pode pedir) | audit dedicado: `{ usuario_id, evento: "minha_conta_anonimizada", pacientes_afetados: int, ip_origem, ocorrido_em }`. Idealmente assinado/imutável (append-only). |
| `ExportarMeusDadosLgpdQueryHandlers` | sem audit | **alto LGPD** — Art. 9º (titular pode pedir relatório de quem acessou seus dados, inclusive ele próprio) | audit: `{ usuario_id, evento: "meus_dados_exportados", ocorrido_em, ip_origem }`. |
| `RegistrarConsentimentoCommandHandler` (Lgpd) | não auditado nesse review (verificar fora) | médio | confirmar |

**Estrutura mínima** sugerida (uma tabela por aggregate ou uma genérica `lgpd_audit_log`):
- `id` (long)
- `usuario_id` (uuid, FK)
- `evento` (text — enum: `perfil_atualizado`, `onboarding_completado`, `minha_conta_anonimizada`, `dados_exportados`, `senha_resetada`, `cpf_verificado`)
- `metadados` (jsonb — dados não-PII; e.g., quais campos mudaram)
- `ip_origem` (inet)
- `user_agent` (text)
- `ocorrido_em` (timestamptz)
- Append-only (sem UPDATE/DELETE — RLS bloqueia, ou trigger).

---

## 8. Bugs latentes

### B1 — `UsuarioRepository.ObterPorId(Guid)` lança `KeyNotFoundException` → 500

`UsuarioRepository.cs:15-21`:

```csharp
public async Task<Usuario> ObterPorId(Guid id)
{
    var usuario = await _context.Usuarios.FindAsync(id);
    if (usuario is null)
        throw new KeyNotFoundException($"Usuário {id} não encontrado.");
    return usuario;
}
```

**Mesmo padrão da Fase 3.2 (B1)**. `GlobalExceptionFilter` mapeia só `BusinessException` → 500 com stack trace + vazamento de `id` na mensagem.

**Consumidores** que vão direto para 500:
- `AtualizarPerfilUsuarioCommandHandler.cs:18`
- `CompletarOnboardingUsuarioCommandHandler.cs:19`
- `AnonimizarMinhaContaCommandHandler.cs:40`

**Ação**: trocar `KeyNotFoundException` por `BusinessException("Usuário não encontrado.")` (sem o id) — alinhado com Fase 3.2.

### B2 — `AuthController.Refresh` declara `[ProducesResponseType(401)]` mas pode retornar 422

`AuthController.cs:117` declara 401. `SupabaseAuthService.RefreshAsync:95-96` lança `BusinessException` → `GlobalExceptionFilter` mapeia para **422**. O front (`httpClient.ts` interceptor) só roda o ciclo de refresh em 401 — se o backend retornar 422, ciclo não acontece e o usuário fica com sessão "morta" mas nunca é deslogado pelo interceptor.

**Severidade médio** (UX + segurança). Quando o refresh expira, usuário continua "logado" no front com sessão inválida; comportamento depende de cada chamada subsequente retornar 401.

**Ação**: no `RefreshAsync` falhado, **retornar 401 explícito** (não jogar `BusinessException`). Trocar para algo como:

```csharp
catch (BusinessException) { return Unauthorized(new { mensagem = "Sessão expirada." }); }
```

Ou, melhor, trocar `BusinessException` por uma exceção mais específica (`AuthException`) e mapear para 401 no `GlobalExceptionFilter`.

### B3 — `Usuario.Anonimizar()` não nulifica `Email`

`Domain/Usuarios/Usuario.cs:82-92`:

```csharp
public virtual void Anonimizar()
{
    NomeCompleto = $"Usuário Anonimizado";
    Cpf = null;
    Telefone = null;
    Status = UsuarioStatus.Inativo;
    AtualizadoEm = DateTime.UtcNow;
    // E-mail: não nulificamos aqui pois é FK de autenticação no Supabase Auth.
}
```

**Médio LGPD**. Após anonimização, o `email` (PII) **continua para sempre** em `public.usuarios`. Comentário explica que é necessário para FK com `auth.users` — mas o titular tem direito ao esquecimento total (Art. 18 LGPD).

**Solução proposta**:
1. Combinar com `IAuthService.DeleteUserAsync(userId)` — apaga o `auth.users.id`, e por `ON DELETE CASCADE` (CLAUDE.md confirma) o `public.usuarios` também é apagado.
2. Hoje o `MinhaContaController.AnonimizarConta` **não chama `DeleteUserAsync`** — só `AnonimizarMinhaContaCommand`. O Supabase Auth fica com email + senha hash do usuário "anonimizado" indefinidamente. Login continua funcional.
3. Plano:
   a. `AnonimizarMinhaContaCommandHandler` continua operando (anonimiza pacientes ligados).
   b. **Após** o command, o `MinhaContaController` chama `_authService.DeleteUserAsync(userId.ToString())`.
   c. Se a FK for `ON DELETE CASCADE`, o `public.usuarios` também desaparece — aí o `Anonimizar()` no aggregate fica redundante (pode-se manter como fallback se delete no auth falhar).
   d. Auditar a operação **antes** do delete (audit trail precisa sobreviver ao delete cascade — usar tabela dedicada `lgpd_audit_log` SEM FK para `auth.users`).

### B4 — `UsuarioRepository.Salvar` faz UPDATE sem versionamento

`UsuarioRepository.cs:31-38` — `Update` direto sem rowversion / concurrency token. Race condition: dois `AtualizarPerfil` simultâneos podem fazer "last write wins" silencioso. Severidade **baixo** (perfil é raramente concorrente), mas considerar `[Timestamp]` ou `xmin` token no aggregate.

---

## 9. Bug de SerializeUserInfo no `Refresh`

`AuthController.Refresh` (linha 124-149) faz **2 queries**:
1. `_authService.RefreshAsync(refreshToken)` (HTTP para Supabase)
2. `_usuarioRepository.ObterPorIdOuNulo(userId)` (DB local)

Performance **médio** — refresh roda em **toda renovação** (a cada 1h por usuário ativo). Considerar cachear o registro local por curto TTL (5 min) ou retornar só o que o Supabase já retorna (sem buscar registro local). Trade-off: o front precisa de `onboardingCompleto` → vale a query. ✅ aceitável.

---

## 10. Cross-check com front

| Front field | Backend serve em | OK? |
|---|---|---|
| `usuario.id` | `/me`, `/refresh` | ✅ |
| `usuario.email` | `/me`, `/refresh` | ✅ |
| `usuario.nomeCompleto` | `/me`, `/refresh` | ✅ |
| `usuario.cpf` | `/me`, `/refresh` (PII desnecessário em /me) | ⚠️ — minimização |
| `usuario.telefone` | `/me`, `/refresh` (PII desnecessário em /me) | ⚠️ — minimização |
| `usuario.status` | `/me`, `/refresh` | ✅ |
| `usuario.onboardingCompleto` | `/me`, `/refresh` | ✅ |
| `usuario.ultimoAcessoEm` | `/me`, `/refresh` | dead — front não consome, remover |

`authStore.ts` interface `Usuario` (linhas 14-23) declara CPF/telefone como `string | null` — confirmando que o front sabe receber, mas **não há tela que exibe `usuario.cpf` direto do `authStore`**. (Tela de "minha conta" deveria buscar via endpoint dedicado.) Para confirmar definitivamente, sugiro grep `authStore.usuario.cpf` no front antes de remover. Na minha leitura, **a única referência é o gating de onboarding** que pode ser feito por `onboardingCompleto`.

`usuarioService.ts` é minimal e correto — não re-emite CPF para servidor sem necessidade.

---

## 11. Top 5 ações priorizadas

1. **CRÍTICO LGPD/segurança** — `Infrastructure/Container.cs:119-124`: trocar `apikey: ServiceRoleKey` default por **anon key**. Criar `SupabaseOptions.AnonKey` ou separar em 2 named clients (`supabase-anon` para `/auth/v1/*` user-facing, `supabase-admin` para `/auth/v1/admin/*` com `service_role`). Risco hoje = vazamento da chave-mestra em qualquer log de header / OTel / Datadog.

2. **CRÍTICO LGPD** — `UsuarioController.cs:69-78` (`/api/usuario/me/cpf-disponivel`): aplicar `[EnableRateLimiting("auth-sensitive")]` + remover diferenciação de `motivo` ("CPF inválido" vs "CPF já cadastrado") + auditar todas as chamadas. Sem isso, qualquer conta autenticada pode enumerar a base inteira de CPFs em horas.

3. **ALTO LGPD** — Logs com email em `SupabaseAuthService` (linhas 44, 80, 168, 209) e `UsuarioCriadoEventHandler.cs:22-24`: trocar `{Email}` por hash SHA-256 truncado (replicar padrão `hashIp` do `Program.cs:277`). Email persistido em log structured por meses é exposição contínua.

4. **ALTO LGPD** — Audit trail ausente em 4 operações (`AtualizarPerfil`, `CompletarOnboarding`, `AnonimizarConta`, `ExportarMeusDados`). Criar `lgpd_audit_log` (append-only, sem FK para `auth.users`) e instrumentar os handlers. Anonimização sem trilha de auditoria é descumprimento direto do Art. 37.

5. **ALTO LGPD/UX** — `AuthController.Me`/`Refresh`: remover `cpf`, `telefone`, `ultimoAcessoEm` do payload. Substituir gating de onboarding por `onboardingCompleto` (já existe). Mover `/me/contato` para endpoint dedicado consumido só na tela de perfil. Reduz superfície de exposição em **toda chamada de bootstrap e refresh** (CPF/telefone hoje trafega ~1×/h por usuário ativo + ~1× por reload de página).

**Bonus (médio)**: B1 (KeyNotFoundException → 500 em `UsuarioRepository.ObterPorId`) + B2 (refresh declara 401 mas devolve 422) + B3 (`Anonimizar()` não nulifica e-mail; precisa orquestrar `DeleteUserAsync` + audit antes do cascade).
