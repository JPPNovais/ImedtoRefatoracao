# Painel admin global — opção "Alterar senha" (troca voluntária da própria senha)

**ID**: 2026-06-16_002
**Status**: Aprovado por usuário em 2026-06-16
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: permissionamento (auth admin global) — força-reset de primeiro login é o principal risco de regressão

## 1. Contexto e motivação

O painel admin global do Imedto (`/api/admin/*`, `AdminLayout`) hoje só permite ao administrador trocar a própria senha em **um** cenário: a troca **obrigatória** no primeiro login / senha temporária (`must_reset_password = true`), via a view full-screen `AdminChangePassword.vue` e o endpoint `POST /api/admin/auth/change-password` (policy `ImedtoAdminChangePassword`).

Não existe caminho para o administrador **já logado e regular** trocar a própria senha de forma **voluntária**, a qualquer momento (boa prática de segurança: rotação periódica, suspeita de comprometimento, etc.). Essa lacuna obriga hoje um workaround operacional (outro admin reseta a senha → gera senha temporária → força reset), o que é atrito e expõe a senha a um terceiro.

A demanda adiciona uma opção fixa **"Alterar senha"** no dropdown de perfil do `AdminLayout`, abrindo um modal dentro do shell, **sem regredir** o fluxo de força-reset existente.

## 2. Persona-alvo

Administrador global do Imedto (claim `imedto_admin = "true"`, sem `estabelecimento_id`), **já autenticado e sem pendência de reset** (`must_reset_password = false`). Uso pontual/esporádico (rotação de senha, higiene de segurança). Não é o usuário de estabelecimento — o admin global é um perfil interno distinto.

## 3. Escopo

**Inclui**:
- Opção fixa "Alterar senha" no dropdown de perfil/topbar do `AdminLayout` (slot `#perfil`, junto do botão "Sair").
- Modal de troca de senha dentro do shell admin, espelhando o layout/UX de `frontend/src/components/minhaConta/AlterarSenhaModal.vue` (senha atual + nova + confirmação + "mostrar senhas").
- Extensão do contrato `AdminChangePasswordRequest` para incluir `SenhaAtual` **opcional**.
- Extensão do handler do endpoint `POST /api/admin/auth/change-password` para validar `SenhaAtual` na troca voluntária (token normal), preservando o comportamento atual do força-reset (token com `must_reset_password = true`).
- Ajuste de autorização para que a rota aceite tanto o token de força-reset (atual) quanto o admin regular (policy `ImedtoAdmin`), sem enfraquecer o força-reset.
- Auditoria distinta para a troca voluntária.
- Espelho back+front das validações de senha (política, "nova ≠ atual", confirmação).

**Não inclui**:
- Qualquer alteração na view full-screen `AdminChangePassword.vue` (continua exclusiva do força-reset).
- Qualquer mudança no fluxo de reset administrativo (admin A reseta senha do admin B → `ResetarSenhaAdmin`).
- "Esqueci minha senha" / recuperação por e-mail (fora de escopo).
- Multi-fator / verificação adicional.
- Qualquer alteração de schema de banco.

## 4. Regras de negócio

- **R1 — Troca voluntária exige senha atual.** Quando a requisição vem de um admin **regular** (token sem `must_reset_password`), `SenhaAtual` é **obrigatória** e validada via `IPasswordHasher.Verificar(SenhaAtual, admin.SenhaHash)`. Senha atual incorreta → `BusinessException` → 422 com mensagem genérica. Mora em: Controller/Handler do `change-password` (`AdminAuthController.ChangePassword`). Validada em: backend (fonte da verdade) + front (UX).

- **R2 — Força-reset permanece sem senha atual.** Quando o token carrega `must_reset_password = true` (primeiro login / senha temporária), `SenhaAtual` é **ignorada** mesmo se enviada — o comportamento atual é preservado integralmente (admin que esqueceu/recebeu senha temporária não precisa informar a atual). Mora em: Controller/Handler do `change-password`. A distinção é feita por presença/ausência da claim `must_reset_password` no token (`ImedtoAdminTokenIssuer.MustResetPasswordClaim`), **não** por presença do campo `SenhaAtual` no body.

- **R3 — Nova senha respeita a política de senha admin.** Reusa `AdminSenhaPolicy.Validar(NovaSenha, _env.IsDevelopment())` (10+ chars com maiúscula/minúscula/número/especial em prod; 6 em dev). Viola política → 422. Mora em: Handler. Validada em: backend (fonte) + front (UX, paridade de mensagem).

- **R4 — Nova senha ≠ senha atual (troca voluntária).** Quando `SenhaAtual` veio (troca voluntária) e `NovaSenha == SenhaAtual` → `BusinessException` → 422. Esta regra **não** se aplica ao força-reset (onde `SenhaAtual` não é fornecida). Mora em: Handler. Validada em: backend (fonte) + front (UX).

- **R5 — Autorização acomoda os dois casos sem enfraquecer o força-reset.** A rota `change-password` deve ser acessível por:
  - admin com `must_reset_password = true` (força-reset — **comportamento atual, inalterado**), e
  - admin regular sob policy `ImedtoAdmin` (troca voluntária — **novo**).

  A policy atual `ImedtoAdminChangePassword` já é a mais permissiva (exige apenas `imedto_admin = "true"`, aceitando ou não a claim `must_reset_password`) — portanto **já cobre ambos os casos** e a rota **continua sob `ImedtoAdminChangePassword`**. **Não rebaixar** para `AllowAnonymous` nem afrouxar a exigência da claim `imedto_admin`. A diferença entre os dois fluxos é decidida **dentro do handler** pela leitura da claim `must_reset_password`, não pela policy. Mora em: Program.cs (policies — sem mudança) + Handler (ramificação por claim).

  > Nota de não-regressão: a única razão de `ImedtoAdminChangePassword` existir é aceitar o token de reset; ela já aceita o token regular. Trocar para `ImedtoAdmin` quebraria o força-reset (que exige a claim `must_reset_password` ausente em `ImedtoAdmin`). Por isso a rota permanece em `ImedtoAdminChangePassword` e a obrigatoriedade da senha atual é regra de domínio, não de autorização.

- **R6 — A troca revoga todas as sessões e reemite a sessão atual.** Comportamento atual do endpoint, **mantido** para os dois fluxos: `RevogarTodosDoAdminAsync` + emissão de novo access/refresh com `forceReset:false` + reset de cookies. O admin segue logado nesta sessão; demais dispositivos são desconectados.

- **R7 — Auditoria distinta para troca voluntária.** A troca voluntária audita com uma ação **distinta** do força-reset: criar a constante `AlterarSenhaPropria = "ALTERAR_SENHA_PROPRIA"` em `AcoesAuditAdmin` (`ImedtoAdminAuditLog.cs`) e usá-la quando `SenhaAtual` foi exigida (token regular). O força-reset continua auditando como `ResetSenhaPropria` (`RESET_SENHA_PROPRIA`). Toda nova constante de `AcoesAuditAdmin` **exige** entrada correspondente no mapa de retenção `AuditLogRetencao` (TTL) — o teste `PorAcao_CobreTodasAsConstantesDeAcoesAuditAdmin` falha sem isso. TTL sugerido para `AlterarSenhaPropria`: **365 dias** (mesmo de `ResetSenhaPropria`, por ser evento de segurança). Mora em: Domain (`AcoesAuditAdmin`, `AuditLogRetencao`) + Handler. Auditoria sem PII e sem senha (já é o padrão do `ImedtoAdminAuditWriter`).

## 5. Modelo de dados

**Nenhuma mudança de schema.** A senha do admin já é persistida em `SenhaHash` (atualizada via `admin.AtualizarSenha`), as sessões em `admin_refresh_tokens`, e a auditoria em tabela de audit log existente (`ImedtoAdminAuditWriter`). A nova constante `AlterarSenhaPropria` é uma string de aplicação — **não** há CHECK/enum no banco a alterar.

- **Multi-tenant**: **NÃO se aplica.** O admin global é um perfil **sem `estabelecimento_id`** (a própria doc de auth registra: "Nunca carrega `estabelecimento_id`"). A entidade alterada é o `ImedtoAdmin` global, identificada pela claim `sub` do token admin. Nenhuma query/comando desta feature filtra ou referencia `estabelecimento_id`. Registrado explicitamente como premissa.
- **PII/LGPD**: senha e senha atual nunca vão para log nem para mensagem de erro; auditoria grava apenas `{ acao, adminId, recursoTipo="admin", recursoId=adminId }` — padrão atual mantido.
- **`imedto-database` NÃO precisa ser acionado.**

## 6. UX e fluxo

**Entrada** — Dropdown de perfil do `AdminLayout` (slot `#perfil` do `AppTopBar`), acima do botão "Sair":
```
┌─ perfil ──────────────────────────┐
│ admin@imedto.com                  │
│ [ 🔑  Alterar senha ]  (variant ghost / neutro)
│ [ ⇥  Sair ]            (variant danger — já existe)
└───────────────────────────────────┘
```
Clicar em "Alterar senha" fecha o dropdown e abre o modal dentro do shell.

**Modal** — espelha `AlterarSenhaModal.vue` (componente do app regular). NÃO importar/compartilhar o componente diretamente se ele depende do `useAuthStore` do app de estabelecimento — o admin usa o **store de auth admin** (`store` do `AdminLayout`) e o **endpoint admin** (`/api/admin/auth/change-password`). Reusar o **layout/UX** (campos, toggle "mostrar senhas", textos, validação local, estados), trocando a chamada de API para a do admin. Campos:
- Senha atual (`autocomplete="current-password"`)
- Nova senha (`autocomplete="new-password"`)
- Confirmar nova senha (`autocomplete="new-password"`)
- Checkbox "Mostrar senhas"

Título: "Trocar senha". Subtítulo: "Você continuará logado nesta sessão. Os demais dispositivos serão desconectados."

**Estados**:
- **Loading**: botão "Trocar senha" em `:loading`, campos `:disabled`, modal não fecha durante execução.
- **Erro (422)**: exibe a mensagem genérica retornada pelo backend (`erro` em `.msg-erro`). Não revelar qual validação falhou de forma que vaze PII.
- **Validação local (UX, antes do submit)**: nova < mínimo de caracteres; nova == atual; confirmação não confere → `.dica` e botão desabilitado. Espelha o backend.
- **Sucesso**: fecha o modal, mantém a sessão (cookies reemitidos), mostra confirmação visual leve (toast/mensagem). Admin permanece logado.

**Mobile-ready**: o `AppModal` largura `sm` já é responsivo; reusar.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — troca voluntária)**: Dado um admin global autenticado e **sem** `must_reset_password`, Quando abre o dropdown de perfil, clica em "Alterar senha", informa a senha atual correta + uma nova senha válida + confirmação igual e confirma, Então o backend retorna 200, a senha é atualizada (`SenhaHash` muda), as demais sessões são revogadas, a sessão atual é reemitida (cookies novos sem claim de reset) e o admin **permanece logado** no painel.

- **CA2 (senha atual incorreta)**: Dado um admin regular no modal de troca voluntária, Quando informa uma **senha atual incorreta** (demais campos válidos), Então o backend retorna **422** com mensagem **genérica** (sem revelar PII, sem dizer "senha atual errada" de forma que vaze hash/política), a senha **não** é alterada e nenhuma sessão é revogada.

- **CA3 (nova senha fraca)**: Dado um admin regular no modal, Quando informa senha atual correta mas uma `NovaSenha` que **viola `AdminSenhaPolicy`** (ex.: curta/sem caractere especial em prod), Então o backend retorna **422** e a senha não é alterada. (Em dev a política exige 6 chars; em prod 10+ com complexidade — o CA é avaliado conforme o ambiente de teste.)

- **CA4 (nova == atual)**: Dado um admin regular no modal, Quando informa senha atual correta e `NovaSenha == SenhaAtual`, Então o backend retorna **422** e a senha não é alterada. O front também bloqueia o submit (botão desabilitado) com a dica "A nova senha precisa ser diferente da atual".

- **CA5 (confirmação não confere — bloqueio no front)**: Dado um admin no modal, Quando `NovaSenha != Confirmação`, Então o botão "Trocar senha" fica **desabilitado** e exibe a dica "A confirmação não confere" — o request **não** é disparado.

- **CA6 (força-reset NÃO regrediu)**: Dado um admin com `must_reset_password = true` (primeiro login / senha temporária) na view full-screen `AdminChangePassword.vue`, Quando informa apenas a nova senha válida (**sem** senha atual) e confirma, Então o backend retorna 200 e troca a senha exatamente como antes — `SenhaAtual` ausente **não** causa 422 neste fluxo. Adicionalmente: a opção "Alterar senha" do dropdown **não** fica acessível indevidamente para esse usuário (ele está em força-reset, não no shell com dropdown de perfil — o gate de rota de força-reset continua valendo).

- **CA7 (autorização — admin regular acessa a troca voluntária)**: Dado um admin regular autenticado (token válido, sem `must_reset_password`), Quando chama `POST /api/admin/auth/change-password` com senha atual + nova válidas, Então recebe 200 (a policy `ImedtoAdminChangePassword` aceita o token regular e o handler exige a senha atual).

- **CA8 (autorização — caminho não vaza para usuário de estabelecimento)**: Dado um usuário de **estabelecimento** (token normal de app, **sem** claim `imedto_admin`), Quando tenta chamar `POST /api/admin/auth/change-password`, Então recebe **401/403** (a policy exige `imedto_admin = "true"`); a opção "Alterar senha" do admin **não** aparece em nenhuma tela do app de estabelecimento.

- **CA9 (audit)**: Dado uma troca **voluntária** bem-sucedida, Quando ocorre, Então uma linha é inserida no audit log admin com `{ acao = "ALTERAR_SENHA_PROPRIA", adminId, recursoTipo = "admin", recursoId = adminId }`, **sem** senha nem PII; e o mapa `AuditLogRetencao` contém a entrada de TTL para `AlterarSenhaPropria` (teste `PorAcao_CobreTodasAsConstantesDeAcoesAuditAdmin` verde). Dado um **força-reset** bem-sucedido, Então a auditoria continua sendo `RESET_SENHA_PROPRIA` (inalterada).

- **CA10 (multi-tenant não se aplica — admin global)**: Dado que o admin global não possui `estabelecimento_id`, Quando a troca de senha ocorre, Então nenhuma query/comando desta feature lê, filtra ou grava `estabelecimento_id`; a entidade afetada é o `ImedtoAdmin` identificado pela claim `sub` do token admin. (Verificável por inspeção: o handler não recebe nem usa tenant claim.)

## 8. Riscos e dependências

- **Risco principal — regressão do força-reset**: a mudança mais sensível é tornar `SenhaAtual` obrigatória sem quebrar o primeiro login. Mitigação: a obrigatoriedade é decidida pela claim `must_reset_password` (presença → ignora `SenhaAtual`; ausência → exige), **não** pela presença do campo. CA6 é o gate de não-regressão e deve ser validado com o app rodando (subir força-reset real e trocar sem senha atual).
- **Risco — rate limiting**: o endpoint hoje **não** declara `[EnableRateLimiting]` (diferente de login/refresh). A validação de senha atual abre superfície para tentativa de força bruta da senha atual de um admin já autenticado. **Recomendação (não-negociável de segurança)**: aplicar `[EnableRateLimiting("auth-sensitive")]` (3 req — partição já existente em Program.cs) na rota `change-password`. Decisão de produto: **aplicar**. Se o dev julgar que muda comportamento de força-reset (improvável — 3 tentativas é folgado para troca legítima), reportar antes.
- **Dependência**: nenhuma de schema/infra. `AdminSenhaPolicy`, `IPasswordHasher`, `ImedtoAdminAuditWriter`, `RevogarTodosDoAdminAsync`, `EmitirAccessToken` já existem.
- **Áreas regressivas a vigiar**: fluxo de primeiro login admin (força-reset); sessões admin (revogação/reemissão de cookies); suíte de auditoria (cobertura de TTL).

## 9. Observações para execução

**Não-negociável**:
- Rota permanece sob `ImedtoAdminChangePassword` (R5). **Não** trocar para `ImedtoAdmin` nem afrouxar a claim `imedto_admin`.
- Distinção força-reset × voluntária por claim `must_reset_password`, **não** por presença do campo `SenhaAtual`.
- `SenhaAtual` opcional no contrato; obrigatória apenas no ramo voluntário do handler.
- Validação real no backend para R1/R3/R4 (front é UX). Mensagem 422 genérica, sem PII/senha.
- Nova constante de audit `AlterarSenhaPropria` **com** entrada em `AuditLogRetencao` (TTL 365) — senão o teste de cobertura quebra.
- Aplicar `[EnableRateLimiting("auth-sensitive")]` na rota (segurança).

**Liberdade técnica do dev**:
- Decidir se o modal admin é um componente novo em `frontend/src/modules/admin/` espelhando `AlterarSenhaModal.vue` (recomendado, pois o original acopla `useAuthStore` do app de estabelecimento) ou uma refatoração que parametrize a chamada de API. **Preferência: componente próprio do módulo admin reusando o layout/UX**, para não acoplar o app de estabelecimento ao painel admin.
- Onde colocar o método de chamada (`store` de auth admin vs. service dedicado), seguindo o padrão já existente no módulo admin.
- Forma exata do feedback de sucesso (toast vs. mensagem inline), desde que o admin perceba o sucesso e permaneça logado.

**Reuso obrigatório (grep antes de criar)**: `AppModal`, `AppButton`, tokens tipográficos (CLAUDE.md §5 — sem literais de `font-size`/`font-weight`; o componente original tem alguns literais legados — no componente **novo** usar tokens), `AdminSenhaPolicy`, `IPasswordHasher`, `ImedtoAdminAuditWriter`, `RevogarTodosDoAdminAsync`. Endpoint **estendido**, não duplicado.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md` §Autenticação (BFF / LocalJwt)** — adicionar nota curta sobre o contrato estendido de `POST /api/admin/auth/change-password`: agora atende dois fluxos sob a mesma rota/policy (`ImedtoAdminChangePassword`) — (1) **força-reset** (`must_reset_password = true`, `SenhaAtual` ignorada, audit `RESET_SENHA_PROPRIA`) e (2) **troca voluntária** (token regular, `SenhaAtual` obrigatória e validada, audit `ALTERAR_SENHA_PROPRIA`); a diferença é resolvida no handler pela claim, não pela policy; rota passa a ter rate limit `auth-sensitive`. Ajuste cirúrgico (parágrafo/nota), não reescrita da seção.
- **`Docs/DESIGN.md`** — registrar o padrão de **modal de ação no painel admin** acionado pelo slot `#perfil` do `AppTopBar` no `AdminLayout` (entrada de "Alterar senha" ao lado de "Sair"), reusando `AppModal` — apenas se for um padrão novo no painel admin; se já houver precedente de modal nesse slot, não duplicar a doc.
- **`Docs/LGPD.md`** — não requer atualização: nenhum novo tipo de PII, endpoint de PII ou regra de retenção (a nova ação de audit segue o TTL/padrão de minimização já documentado). Se a seção de auditoria admin listar ações nominalmente, adicionar `ALTERAR_SENHA_PROPRIA` à lista (ajuste de uma linha).

**Quem atualiza**: `imedto-business-analyst` aponta aqui; a edição cirúrgica dos docs acompanha a entrega antes do hand-off ao QA (o QA valida no CA que o doc reflete o contrato estendido).
