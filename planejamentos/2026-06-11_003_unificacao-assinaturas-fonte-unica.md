# Unificação do sistema de Assinaturas/Planos — estrutura nova (`imedto_*`) como fonte única + porta para gateway futuro

**ID**: 2026-06-11_003
**Status**: Aprovado por usuário em 2026-06-11
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G (épico cross-cutting, faseado em F1–F6)
**Áreas regressivas tocadas**: permissionamento (enforcement de assinatura/feature/limites em ~17 controllers), criação de estabelecimento (trial automático), admin global (gestão de planos/assinaturas), front (router guard + upsell). Prontuário/financeiro do paciente: **não** (módulo Financeiro/Cobranças é cobrança de PACIENTE — conceito distinto, não confundir).

> **Diagnóstico-âncora**: hoje existem **duas** estruturas paralelas e dessincronizadas. A **legada** (bigint: `assinaturas`/`planos`) é a **única** que o enforcement consome. A **nova/admin** (uuid: `imedto_assinaturas`/`imedto_planos`) só o admin escreve/lê. Consequência: trocar plano/gratuidade no admin **não afeta** o usuário. Esta demanda elege a **estrutura nova como fonte única de verdade** e migra o enforcement para ela, com paridade comportamental e sem bloquear ninguém na virada.
>
> Este briefing é **imutável** — gap vira addendum. Cada fase (F1–F6) tem CAs próprios e ordem de execução segura.

---

## 1. Contexto e motivação

O dono do Imedto (produto em fase de testes) quer **controlar assinaturas/planos manualmente pelo admin interno AGORA**, deixando a estrutura **pronta para plugar um gateway de pagamento automático DEPOIS** — sem construir o gateway agora.

O obstáculo é técnico e confirmado no código: **duas estruturas paralelas sem sincronização**.

- **LEGADA (bigint) — única que o enforcement lê hoje:**
  - `assinaturas` (1:1 com estabelecimento): `status` enum (`Trial`/`Ativa`/`Suspensa`/`Cancelada`/`Expirada`), `ExpiraEm`.
  - `planos`: `Nome`, `PrecoMensal`, `LimiteProfissionais`, `LimitePacientes` (nullable = ilimitado), `FeaturesJson`.
  - Domain: `Imedto.Backend.Domain.Assinaturas.{Assinatura, Plano, StatusAssinatura, Features}`.
  - Consumida por: `AssinaturaService` (`ObterPorEstabelecimentoOuNulo` via `AssinaturaRepository` EF), filtro `[RequiresAssinaturaAtiva]` (~17 controllers → 402 `AssinaturaInativa`), filtro `[FeatureGate]` (Receitas/Ia/ExameFisico/ProcedimentosCirurgicos/OrcamentoCompleto/RelatoriosAvancados → 402 `FeatureBloqueada`), `LimiteAtingidoAsync` (`CadastrarPacienteCommandHandler`, `ConvidarProfissionalCommandHandler`).
  - Trial automático de 14d na criação do estabelecimento (`IniciarTrialAoCriarEstabelecimentoHandler`).
  - Front: router guard `useAssinaturaStore.isBlocked → AssinaturaExpirada`, `upsellStore` intercepta 402.

- **NOVA/ADMIN (uuid) — só o admin escreve/lê:**
  - `imedto_assinaturas` (histórico imutável 1:N; vigência por `fim_em IS NULL`; `gratuita`, `motivo`).
  - `imedto_planos`: `Nome`, `DescricaoCurta`, `PrecoMensalCentavos`, `Gratuito` (bool), `Ativo`, `LimitesJson` (`{"profissionais","pacientes"}`) — **sem status, sem features**.
  - Domain: `Imedto.Backend.Domain.Admin.{ImedtoAssinatura, ImedtoPlano}`.
  - Escrita por `TrocarPlanoAdminCommandHandler` e `ConcederGratuidadeAdminCommandHandler` (plano "Gratuidade Vitalícia" UUID fixo `00000000-0000-0000-0000-000000000001`).
  - Lida só por `AssinaturaCard.vue` / `AdminEstabelecimentosQueryRepository.cs`.

**Dor concreta**: o admin troca plano/concede gratuidade e o usuário continua bloqueado/liberado pelo estado da legada. Estabelecimentos antigos aparecem "Sem plano" no admin (só têm registro legado). É um caos de duas verdades que precisa virar uma só.

## 2. Persona-alvo

- **Dono do produto / admin global do Imedto** (principal): opera o admin interno para liberar/suspender/dar trial/conceder gratuidade e cadastrar planos pagos (nome, preço, features, limites). Uso recorrente durante a fase de testes e onboarding manual de cada cliente.
- **Dono/equipe do estabelecimento** (afetado indiretamente): sofre o enforcement (bloqueio por assinatura inativa, feature bloqueada, limite atingido). Não opera nada aqui — só **sente** o efeito. A virada **não pode bloqueá-lo** indevidamente.
- **Futuro: o próprio cliente em self-service** (não nesta entrega): o gateway de pagamento criará/atualizará assinaturas via a porta — por isso as colunas dormentes (`origem`, `referencia_externa`, `status_cobranca`) já nascem.

## 3. Escopo

**Inclui (IN)**:
- Eleição da estrutura **nova (`imedto_*`, uuid) como fonte única** de verdade de assinatura/plano/feature/limite.
- **F1** — Schema novo: colunas de vigência/suspensão em `imedto_assinaturas`; features+limites em `imedto_planos`; colunas dormentes de pagamento (`origem`, `referencia_externa`, `status_cobranca`); config global de trial (plano + duração + on/off). Migration idempotente.
- **F2** — Backfill idempotente de **todos** os estabelecimentos existentes da legada → nova, espelhando o estado atual (ninguém bloqueado na virada).
- **F3** — Enforcement migra para ler a estrutura nova (`AssinaturaService` + `[RequiresAssinaturaAtiva]` + `[FeatureGate]` + `LimiteAtingidoAsync`) com **paridade comportamental total** (mesmos 402, mesmas features bloqueadas, mesmos limites).
- **F4** — Admin: ações de estado por estabelecimento (Liberar vitalício / Liberar até data / Iniciar trial de N dias / Suspender / Reativar) + CRUD de planos com features (checkbox) e limites (número, vazio = ilimitado) + edição da config de trial. **Invalidação de cache** ao mudar plano/estado (CA explícito).
- **F5** — Trial automático na criação de estabelecimento passa a gravar na estrutura nova usando a config global (plano + duração configuráveis; ou desligado → nasce bloqueado).
- **F6** — Descontinuar a legada: torná-la **read-only** (sem mais escrita pelo domínio); remoção física fica prevista mas **opcional/posterior** para não quebrar nada.
- **Porta de integração futura** (`IProvedorAssinaturaExterna` / `IGatewayPagamento`) **definida como contrato na Application — sem implementação** — para que as colunas dormentes nasçam coerentes com ela.
- Atualização de `Docs/ARQUITETURA.md` (modelo unificado + premissa de provider) e `Docs/LGPD.md` (sem mudança de PII, mas registra a nota — ver §10).

**Não inclui (OUT)**:
- **Gateway de pagamento real / cobrança automática / webhook / SDK concreto.** Só a **porta** (contrato) e o **espaço de dados** (colunas dormentes). Zero lógica de cobrança.
- **Catálogo de planos pagos seedado** (99/149/199). O seed entrega **só** "Gratuidade Vitalícia". O dono cria os planos pagos pelo CRUD do admin depois.
- **Self-service do cliente** (assinar/pagar pela própria conta) — fica para quando o gateway existir.
- **Remoção física** das tabelas/Domain legados na mesma entrega — F6 deixa read-only; o drop é fase posterior.
- **Telas de fatura/recibo de assinatura, fluxo de caixa do SaaS, NFS-e** — fora de escopo.
- **Módulo Financeiro/Cobranças (cobrança de paciente)** — conceito diferente; **não tocar nem fundir** com assinatura do SaaS.

## 4. Regras de negócio

> Toda regra de **estado/derivação** mora no **Domain** (agregados `ImedtoAssinatura`/`ImedtoPlano` + métodos puros) e é validada no **back (422/402 conforme o caso)**. A trava de front é UX e tem espelho obrigatório. O enforcement é **server-side** — o front só reflete.

- **R1 — Fonte única.** A estrutura nova (`imedto_*`) é a **única** fonte de verdade de assinatura/plano/feature/limite. A legada deixa de ser lida pelo enforcement ao fim da F3 e deixa de ser escrita na F6. Mora em: Domain.Admin + `AssinaturaService`. Validada em: back.

- **R2 — Estado por vigência + suspensão (não por enum de status).** A assinatura **vigente** de um estabelecimento é a que tem `fim_em IS NULL` (histórico imutável 1:N). Sobre ela:
  - `iniciada_em` (timestamp de início da vigência).
  - `expira_em` (timestamp; **NULL = vitalício**).
  - `suspensa_em` (timestamp nullable; preenchido = suspensão manual ativa).
  Mora em: Domain.Admin (`ImedtoAssinatura`). Validada em: back.

- **R3 — Estado derivado (substitui `StatusAssinatura`).** O estado efetivo do estabelecimento é **derivado**, nunca setado à mão:
  - **BLOQUEADO** se: não há assinatura vigente, **OU** `expira_em` no passado, **OU** `suspensa_em` preenchido.
  - **LIBERADO VITALÍCIO** se: `expira_em IS NULL` **e** `suspensa_em IS NULL`.
  - **TRIAL / LIBERADO TEMPORÁRIO** se: `expira_em` no futuro **e** `suspensa_em IS NULL`.
  O método de domínio `EstaVigente`/`EstaAtiva` (consumido pelo `AssinaturaService` e pelos filtros) passa a derivar disso. Mora em: Domain.Admin. Validada em: back.

- **R4 — Ações do admin → efeito determinístico.** Cada ação do admin mapeia para um efeito **único e auditável** sobre a vigência. O padrão de mudança de vigência é **encerrar a vigente (`fim_em = now`) e abrir uma nova** (histórico imutável), salvo onde indicado:
  | Ação admin | Efeito |
  |---|---|
  | **Liberar vitalício** | nova vigência: `iniciada_em=now`, `expira_em=NULL`, `suspensa_em=NULL`, plano escolhido |
  | **Liberar até data X** | nova vigência: `iniciada_em=now`, `expira_em=X`, `suspensa_em=NULL`, plano escolhido |
  | **Iniciar trial de N dias** | nova vigência: `iniciada_em=now`, `expira_em=now+N`, `suspensa_em=NULL`, plano de trial configurado |
  | **Suspender agora** | na **própria** vigência atual: `suspensa_em=now` (não abre nova; suspensão é reversível) |
  | **Reativar** | na **própria** vigência atual: `suspensa_em=NULL` (volta ao estado que `expira_em` ditar) |
  Mora em: Domain.Admin (métodos `LiberarVitalicio`/`LiberarAteData`/`IniciarTrial`/`Suspender`/`Reativar`) + Handlers admin. Validada em: back.

- **R5 — Features e limites moram no plano vigente da estrutura nova.** `imedto_planos` ganha:
  - **Features** — as 8 chaves existentes: `receitas`, `exame_fisico`, `procedimentos_cirurgicos`, `orcamento_completo`, `ia`, `relatorios_avancados`, `automacoes_ilimitadas`, `anexos_ilimitados`.
  - **Limites** — `profissionais`, `pacientes` (número; **vazio/NULL = ilimitado**).
  O `[FeatureGate]` passa a consultar a feature **no plano da assinatura vigente** (nova). `LimiteAtingidoAsync` passa a ler os limites do plano vigente (nova). Mora em: Domain.Admin (`ImedtoPlano`) + `AssinaturaService`. Validada em: back.

- **R6 — Paridade comportamental do enforcement (não-negociável).** Após a F3, para qualquer estabelecimento **os mesmos 402 de hoje** ocorrem nos mesmos cenários: `AssinaturaInativa` quando bloqueado (R3), `FeatureBloqueada` quando a feature não está no plano vigente, e o mesmo comportamento de limite atingido em paciente/profissional. Códigos de erro, mensagens e contrato de API **não mudam** para o front. Mora em: filtros + `AssinaturaService`. Validada em: back + teste de paridade.

- **R7 — Trial automático configurável.** Existe uma **config global do admin** com: `plano_trial_id` (uuid), `duracao_trial_dias` (int), `trial_habilitado` (bool). Na criação de um estabelecimento:
  - se `trial_habilitado = true` → cria assinatura vigente nova com `expira_em = now + duracao_trial_dias` e plano `plano_trial_id`.
  - se `trial_habilitado = false` → nasce **sem assinatura vigente** (estado BLOQUEADO por R3).
  `IniciarTrialAoCriarEstabelecimentoHandler` passa a gravar na estrutura nova usando essa config. Mora em: config global (Domain.Admin) + Handler. Validada em: back.

- **R8 — Backfill não bloqueia ninguém.** Todo estabelecimento existente recebe uma assinatura vigente na estrutura nova espelhando o estado legado:
  - legado `Ativa` com `ExpiraEm = NULL` → vitalício (`expira_em=NULL`, não suspenso).
  - legado `Ativa`/`Trial` com `ExpiraEm` no futuro → `expira_em = ExpiraEm`, não suspenso.
  - legado `Suspensa`/`Cancelada`/`Expirada` **OU** `ExpiraEm` no passado → **mantém o mesmo estado bloqueado** (`suspensa_em=now` para Suspensa/Cancelada; `expira_em` no passado para Expirada). Não "liberar" indevidamente, mas também não bloquear quem hoje está liberado.
  - **Mapeamento de plano** (decisão de segurança — §9): como o catálogo pago novo ainda **não existe**, todo estabelecimento é mapeado para **"Gratuidade Vitalícia"** na estrutura nova, **preservando o estado de vigência** derivado do legado. Isso garante que ninguém perca acesso por falta de plano-espelho. O dono reclassifica para planos pagos manualmente depois (F4).
  Mora em: migration de dados idempotente (`imedto-database`). Validada em: back + contagem.

- **R9 — Colunas dormentes de pagamento (sem lógica agora).** `imedto_assinaturas` ganha:
  - `origem` (`admin_manual` | `self_service`; **default `admin_manual`**).
  - `referencia_externa` (text nullable — id do gateway).
  - `status_cobranca` (`nao_aplicavel` | `pendente` | `pago` | `inadimplente`; **default `nao_aplicavel`**).
  **Nenhuma regra de negócio** lê/escreve essas colunas nesta entrega além dos defaults. Elas existem **coerentes com a porta** (R10). Mora em: schema + Domain.Admin (propriedades). Validada em: back (defaults) + presença das colunas.

- **R10 — Porta para integração externa (contrato, sem implementação).** Toda futura integração com o gateway é consumida pelo domínio/handler **apenas via uma porta** definida na Application: `IProvedorAssinaturaExterna` (ou `IGatewayPagamento`). O contrato é **especificado** neste briefing (§6.5) mas **não implementado** — nenhuma classe concreta, nenhum SDK, nenhum adapter agora. **Nenhuma regra de assinatura referencia SDK/gateway concreto.** Mora em: Application (interface). Validada em: back (CA de ausência de acoplamento).

- **R11 — Invalidação de cache (hoje há TODO não feito).** Sempre que o admin muda plano ou estado de um estabelecimento (qualquer ação de R4 + troca de plano + edição de features/limites de um plano em uso), `AssinaturaService.InvalidarCache(estabelecimentoId)` **deve ser chamado** ao fim do handler, na mesma transação lógica. Mora em: Handlers admin. Validada em: back.

- **R12 — Multi-tenant + RBAC global do admin.** As queries de domínio do estabelecimento (enforcement) filtram `estabelecimento_id` do tenant ativo (falha-fechada: sem tenant claim → vazio/bloqueado). O **admin é global** (cross-tenant por natureza), mas **só admin global** pode mudar plano/estado/conceder gratuidade/editar planos/config de trial; operação é sempre **por estabelecimento alvo**. Sem permissão de admin → 403/404 genérico. Mora em: Repositório + Handlers + guard admin. Validada em: back + front.

- **R13 — Mensagens genéricas (LGPD/segurança).** Erros do enforcement e do admin não revelam dado de outro tenant nem PII; mantêm os códigos genéricos atuais (`AssinaturaInativa`, `FeatureBloqueada`). Mora em: filtros + handlers. Validada em: back.

## 5. Modelo de dados (resumo — detalhe para o DB agent em §11)

> Estrutura nova é a fonte única. Alterações sobre tabelas **existentes** (`imedto_assinaturas`, `imedto_planos`) + nova config global. **Não** dropar a legada nesta entrega (F6 = read-only; drop posterior).

- **ALTER `imedto_assinaturas`** (histórico imutável 1:N; vigência por `fim_em IS NULL`):
  - ADD `iniciada_em timestamptz NULL` (R2 — backfill popula).
  - ADD `expira_em timestamptz NULL` (NULL = vitalício).
  - ADD `suspensa_em timestamptz NULL`.
  - ADD `origem text NOT NULL DEFAULT 'admin_manual'` (dormente — R9).
  - ADD `referencia_externa text NULL` (dormente — R9).
  - ADD `status_cobranca text NOT NULL DEFAULT 'nao_aplicavel'` (dormente — R9).
  - Confirmar reaproveitamento de `inicio_em`/`fim_em`/`gratuita`/`motivo` já existentes (não duplicar semântica — alinhar `iniciada_em` com o `inicio_em` existente se houver; o DB agent decide reusar coluna vs. adicionar — ver §11).
  - Índice para a vigência: `(estabelecimento_id) WHERE fim_em IS NULL` (uma vigente por estabelecimento) + `(expira_em)` para varreduras de expiração.

- **ALTER `imedto_planos`**:
  - ADD `features jsonb NOT NULL DEFAULT '{}'` (as 8 chaves booleanas — R5). Confirmar se reusa/renomeia algo equivalente.
  - Confirmar `LimitesJson` (`{"profissionais","pacientes"}`) já existe — manter; `NULL`/ausente = ilimitado.

- **Nova tabela `imedto_config_trial`** (config global — singleton, 1 linha):
  - `id uuid PK`, `plano_trial_id uuid NOT NULL` (FK `imedto_planos`), `duracao_trial_dias int NOT NULL DEFAULT 14`, `trial_habilitado boolean NOT NULL DEFAULT true`, audit/timestamps. (R7)

- **Seed**: apenas "Gratuidade Vitalícia" (UUID fixo `00000000-0000-0000-0000-000000000001`) com `features` = todas habilitadas, limites ilimitados, `Gratuito=true`, `Ativo=true`. `imedto_config_trial` semeada apontando para esse plano com 14 dias habilitado (espelha o comportamento legado atual). **Nenhum plano pago seedado.**

- **Backfill (F2)**: data migration idempotente que, para cada estabelecimento existente, garante exatamente uma `imedto_assinaturas` vigente espelhando o legado (R8). Reexecutável sem duplicar (checar existência por `estabelecimento_id WHERE fim_em IS NULL`).

- **Multi-tenant**: `imedto_assinaturas` é por `estabelecimento_id`; o enforcement filtra por tenant. Admin lê cross-tenant via repositório admin dedicado.

- **LGPD**: assinatura/plano são dados **operacionais do SaaS**, não dados de saúde do paciente — sem PII sensível. Mensagens genéricas mantidas. Sem novo audit de paciente. (Ver §10.)

## 6. UX e fluxo

> O front do **usuário do estabelecimento não muda** (router guard + upsell continuam reagindo aos mesmos 402 — R6). As mudanças de UI são **no admin**.

**6.1 Admin — ações de estado por estabelecimento (`AssinaturaCard.vue` + view de detalhe do estabelecimento)**
- Card de assinatura exibe o **estado derivado** (BLOQUEADO / VITALÍCIO / TRIAL até dd/mm / SUSPENSO) + plano vigente + features/limites do plano.
- Botões de ação (reusar componentes DS de botão/modal): **Liberar vitalício**, **Liberar até data** (date picker), **Iniciar trial de N dias** (input de dias, default da config), **Suspender**, **Reativar**. Cada ação requer escolha/confirmação e, quando aplicável, seleção do plano.
- Estabelecimento que hoje aparece "Sem plano" deixa de aparecer assim após o backfill (terá Gratuidade Vitalícia espelhando o legado).
- Estados: loading, erro (genérico), sucesso (atualiza o card e o estado derivado).

**6.2 Admin — CRUD de planos (`imedto_planos`)**
- Lista de planos (Nome, preço, Gratuito, Ativo). CRUD: criar/editar/inativar.
- Form do plano: nome, descrição curta, preço mensal (centavos), toggle Gratuito/Ativo, **8 checkboxes de feature** (R5), **2 campos de limite** (profissionais, pacientes — vazio = ilimitado).
- Editar features/limites de um plano **em uso** dispara invalidação de cache dos estabelecimentos nesse plano (R11).
- Estados: vazio (só Gratuidade Vitalícia → empty state com CTA "criar primeiro plano pago"), loading, erro, sucesso.

**6.3 Admin — config de trial**
- Tela/seção única: select de **plano de trial**, input **duração em dias**, toggle **trial habilitado**. Salvar persiste em `imedto_config_trial`.
- Hint quando desabilitado: "Novos estabelecimentos nascem bloqueados (sem trial)".

**6.4 Front do usuário (sem mudança funcional — só regressão a vigiar)**
- `useAssinaturaStore.isBlocked → AssinaturaExpirada`, `upsellStore` em 402: **comportamento idêntico**. Nenhuma mudança de tela esperada; CA de regressão garante.

**6.5 Porta `IProvedorAssinaturaExterna` (especificação futura — NÃO implementar)**
Contrato a ser **definido** na Application (interface vazia de implementação concreta), coerente com as colunas dormentes (R9). Assinatura prevista dos métodos (nomes podem ser refinados pelo dev; a forma é o que importa):
- `CriarCobrancaAsync(estabelecimentoId, planoId, ...) → ResultadoCobrancaExterna` (retorna `referencia_externa` e `status_cobranca` inicial).
- `ConsultarStatusCobrancaAsync(referenciaExterna) → StatusCobrancaExterna`.
- `ConfirmarPagamentoPorWebhookAsync(payloadAssinado) → ConfirmacaoPagamento` (futura entrada de webhook do gateway).
A implementação concreta (adapter do provider) **vive na Infraestrutura quando existir** — espelhando Resend/SES (e-mail), S3 (storage), LocalJwt (auth). Hoje: **só a interface**, sem registro de DI obrigatório, sem nenhum consumidor no domínio.

## 7. Critérios de aceite (testáveis)

> CAs agrupados por fase. Ordem de execução: F1 → F2 → F3 → F4 → F5 → F6. Nenhum CA pode resultar em estabelecimento bloqueado indevidamente na virada.

### F1 — Schema novo + porta
- **CA1 (colunas de vigência)**: Dado a migration da F1 aplicada, Quando inspeciono `imedto_assinaturas`, Então existem `iniciada_em`, `expira_em` (nullable = vitalício), `suspensa_em` (nullable).
- **CA2 (colunas dormentes + defaults)**: Dado a migration aplicada, Quando insiro uma `imedto_assinaturas` sem informar os campos de pagamento, Então `origem='admin_manual'`, `referencia_externa=NULL`, `status_cobranca='nao_aplicavel'` por default.
- **CA3 (features+limites no plano)**: Dado `imedto_planos`, Quando inspeciono o schema, Então existe `features` (jsonb com as 8 chaves) e os limites `profissionais`/`pacientes` (vazio/NULL = ilimitado).
- **CA4 (config de trial)**: Dado a F1 aplicada, Quando leio `imedto_config_trial`, Então existe 1 linha semeada com `plano_trial_id` = Gratuidade Vitalícia, `duracao_trial_dias=14`, `trial_habilitado=true`.
- **CA5 (seed mínimo)**: Dado o seed da F1, Quando listo `imedto_planos`, Então existe **apenas** "Gratuidade Vitalícia" (UUID fixo) com features todas habilitadas e limites ilimitados; **nenhum** plano pago (99/149/199) foi seedado.
- **CA6 (porta sem implementação — sem acoplamento)**: Dado a base de código após a F1, Quando faço grep por SDK/gateway concreto no Domain/Application de assinatura, Então existe **apenas** a interface `IProvedorAssinaturaExterna` (sem classe concreta, sem registro de DI obrigatório) e **nenhuma** regra de assinatura referencia gateway/SDK concreto.
- **CA7 (idempotência da migration de schema)**: Dado a migration F1, Quando reexecutada, Então não falha nem duplica colunas/linhas (SQL idempotente).

### F2 — Backfill
- **CA8 (todo estabelecimento ganha vigência)**: Dado N estabelecimentos com registro legado, Quando o backfill roda, Então cada um tem **exatamente uma** `imedto_assinaturas` vigente (`fim_em IS NULL`).
- **CA9 (espelhamento de estado — ativo)**: Dado estabelecimento legado `Ativa` com `ExpiraEm=NULL`, Quando backfilled, Então a vigente fica vitalícia (`expira_em=NULL`, `suspensa_em=NULL`) → estado derivado LIBERADO VITALÍCIO.
- **CA10 (espelhamento — trial/ativo com expiração futura)**: Dado legado `Trial`/`Ativa` com `ExpiraEm` no futuro, Quando backfilled, Então `expira_em=ExpiraEm`, não suspenso → estado derivado TRIAL/LIBERADO TEMPORÁRIO.
- **CA11 (espelhamento — bloqueado)**: Dado legado `Suspensa`/`Cancelada` ou `Expirada`/`ExpiraEm` no passado, Quando backfilled, Então o estado derivado permanece BLOQUEADO (suspenso ou expirado), nunca "liberado por engano".
- **CA12 (ninguém bloqueado indevidamente)**: Dado o conjunto de estabelecimentos hoje liberados (pela legada), Quando o backfill conclui, Então **todos** continuam com estado derivado liberado na estrutura nova (contagem de "liberados antes" = "liberados depois").
- **CA13 (mapeamento de plano seguro)**: Dado que não há catálogo pago novo, Quando backfilled, Então todo estabelecimento aponta para "Gratuidade Vitalícia" preservando o estado de vigência derivado do legado.
- **CA14 (idempotência do backfill)**: Dado o backfill já executado, Quando reexecutado, Então não cria segunda vigência nem altera as existentes (checa `estabelecimento_id WHERE fim_em IS NULL`).
- **CA15 ("Sem plano" some)**: Dado um estabelecimento antigo que aparecia "Sem plano" no admin, Quando o backfill conclui, Então o admin passa a exibir Gratuidade Vitalícia + estado derivado correto.

### F3 — Enforcement lê a estrutura nova (paridade)
- **CA16 (paridade — assinatura inativa)**: Dado um estabelecimento com estado derivado BLOQUEADO (R3) na estrutura nova, Quando chama qualquer rota `[RequiresAssinaturaAtiva]`, Então recebe **402 `AssinaturaInativa`** — idêntico ao comportamento legado.
- **CA17 (paridade — liberado)**: Dado estado derivado LIBERADO (vitalício ou trial futuro, não suspenso), Quando chama rota protegida, Então passa normalmente (sem 402).
- **CA18 (paridade — feature bloqueada)**: Dado o plano vigente **sem** a feature `ia` (por ex.), Quando chama rota `[FeatureGate(Ia)]`, Então recebe **402 `FeatureBloqueada`**; e com a feature habilitada no plano, passa. Vale para as 6 features hoje gated (Receitas/Ia/ExameFisico/ProcedimentosCirurgicos/OrcamentoCompleto/RelatoriosAvancados).
- **CA19 (paridade — limite de paciente)**: Dado plano vigente com `pacientes=50` e 50 cadastrados, Quando `CadastrarPacienteCommandHandler` roda, Então retorna limite atingido (mesmo erro de hoje); com limite vazio/NULL (ilimitado), nunca atinge.
- **CA20 (paridade — limite de profissional)**: Dado plano vigente com `profissionais=3` e 3 vínculos ativos, Quando `ConvidarProfissionalCommandHandler` roda, Então retorna limite atingido; ilimitado nunca atinge.
- **CA21 (suspensão manual bloqueia)**: Dado estabelecimento com `expira_em` no futuro **mas** `suspensa_em` preenchido, Quando chama rota protegida, Então recebe 402 `AssinaturaInativa` (suspensão vence a vigência).
- **CA22 (trial expirado bloqueia)**: Dado `expira_em` no passado, Quando chama rota protegida, Então 402 `AssinaturaInativa`.
- **CA23 (front sem regressão)**: Dado o front do usuário, Quando o back retorna 402 nos cenários acima, Então `useAssinaturaStore.isBlocked` redireciona para `AssinaturaExpirada` e `upsellStore` intercepta — **comportamento idêntico** ao de antes da migração.

### F4 — Admin: ações de estado + CRUD de planos + config de trial
- **CA24 (liberar vitalício)**: Dado um estabelecimento bloqueado, Quando admin clica "Liberar vitalício" com plano X, Então nova vigência abre com `expira_em=NULL`, `suspensa_em=NULL`, plano X, e o estado derivado vira LIBERADO VITALÍCIO; a vigência anterior é encerrada (`fim_em=now`).
- **CA25 (liberar até data)**: Dado um estabelecimento, Quando admin escolhe "Liberar até 31/12", Então `expira_em=31/12`, estado TRIAL/LIBERADO TEMPORÁRIO até lá.
- **CA26 (iniciar trial N dias)**: Dado um estabelecimento, Quando admin escolhe "Iniciar trial de 7 dias", Então `expira_em=now+7d`, plano de trial configurado.
- **CA27 (suspender/reativar)**: Dado vigência ativa, Quando admin "Suspende", Então `suspensa_em=now` na própria vigência e estado vira BLOQUEADO; Quando "Reativa", Então `suspensa_em=NULL` e volta ao estado que `expira_em` ditar (sem abrir nova vigência).
- **CA28 (CRUD de plano com features/limites)**: Dado o CRUD admin, Quando crio um plano "Pro" com features `receitas`+`ia` marcadas e limite `profissionais=5`, `pacientes` vazio, Então o plano persiste com essas 2 features true, as 6 restantes false, limite 5/ilimitado.
- **CA29 (efeito real do plano no enforcement)**: Dado um estabelecimento no plano "Pro" (sem `relatorios_avancados`), Quando acessa rota `[FeatureGate(RelatoriosAvancados)]`, Então recebe 402 `FeatureBloqueada` — provando que admin agora **afeta** o usuário (o bug-âncora foi resolvido).
- **CA30 (config de trial editável)**: Dado a tela de config, Quando admin define plano=Gratuidade, duração=30, habilitado=true e salva, Então `imedto_config_trial` reflete os valores.
- **CA31 (config de trial desligada)**: Dado `trial_habilitado=false` salvo, Quando um novo estabelecimento é criado (F5), Então nasce sem assinatura vigente → estado BLOQUEADO.
- **CA32 (INVALIDAÇÃO DE CACHE — explícito)**: Dado um estabelecimento com plano em cache no `AssinaturaService`, Quando admin executa qualquer ação de estado (R4) ou troca de plano ou edita features/limites de um plano em uso, Então `AssinaturaService.InvalidarCache(estabelecimentoId)` é chamado e a **próxima** requisição do usuário reflete o novo estado/plano **sem** esperar expiração de cache.
- **CA33 (RBAC admin)**: Dado um usuário **não** admin global, Quando tenta chamar qualquer endpoint de ação de estado / CRUD de plano / config de trial, Então recebe 403/404 genérico e os controles ficam ocultos no front; só admin global executa.

### F5 — Trial automático na nova estrutura
- **CA34 (trial automático grava na nova)**: Dado `trial_habilitado=true`, duração=14, plano=Gratuidade, Quando um novo estabelecimento é criado, Então `IniciarTrialAoCriarEstabelecimentoHandler` cria a assinatura **na estrutura nova** com `expira_em=now+14d`, plano configurado, `origem='admin_manual'` — e **não** escreve na legada.
- **CA35 (trial respeita config alterada)**: Dado a config alterada para duração=30, Quando novo estabelecimento é criado, Então `expira_em=now+30d`.

### F6 — Descontinuar legada
- **CA36 (legada read-only)**: Dado a F6 concluída, Quando qualquer fluxo do domínio (criação de estabelecimento, ação admin, enforcement) roda, Então **nenhuma** escrita nova ocorre em `assinaturas`/`planos` legadas; o enforcement lê 100% da estrutura nova.
- **CA37 (sem regressão de bloqueio)**: Dado o sistema após F6, Quando comparo o estado de bloqueio de todos os estabelecimentos antes (legada) e depois (nova), Então são equivalentes — ninguém ficou bloqueado/liberado por engano.

### Transversais (todas as fases)
- **CA38 (multi-tenant falha-fechada)**: Dado uma query de enforcement sem tenant claim, Quando executada, Então retorna vazio/bloqueado (falha-fechada), nunca dado de outro tenant; admin cross-tenant usa repositório admin dedicado.
- **CA39 (mensagens genéricas/LGPD)**: Dado qualquer erro de enforcement ou admin, Quando o back responde, Então a mensagem é genérica (`AssinaturaInativa`/`FeatureBloqueada`/"não encontrado"), sem PII e sem revelar tenant alheio.
- **CA40 (doc viva)**: Dado a entrega concluída, Quando reviso `Docs/`, Então `Docs/ARQUITETURA.md` documenta o modelo unificado de assinaturas (estrutura nova como fonte única, estado por vigência+suspensão) e a premissa de **porta de provider** para o gateway futuro; `Docs/LGPD.md` registra a nota do §10.

## 8. Riscos e dependências

- **Virada do enforcement (F3) é o risco máximo**: trocar a fonte de leitura de ~17 controllers + 2 limites + 6 feature gates. Mitigação: F2 (backfill) **antes** de F3; teste de paridade comportamental (CA16–CA23); rollout só após o backfill validado em produção. Nada de F3 antes de F2 verde.
- **Bloqueio indevido na virada**: maior medo do usuário. Mitigação: mapeamento conservador para Gratuidade Vitalícia (R8/CA13) + contagem antes/depois (CA12/CA37).
- **Cache não invalidado (TODO existente)**: se R11/CA32 não entrar, o admin volta a "não afetar" o usuário até o cache expirar — repetindo o bug-âncora de forma sutil. CA32 é não-negociável.
- **Coluna duplicada de vigência**: `imedto_assinaturas` já tem `inicio_em`/`fim_em`/`gratuita`/`motivo`. Risco de criar semântica duplicada com `iniciada_em`. O `imedto-database` deve **decidir reusar vs. adicionar** e documentar (ver §11) — não criar duas verdades dentro da própria tabela nova.
- **Acoplamento acidental ao gateway**: tentação de "já deixar começado" o provider. Não. Só a interface (CA6). Implementar adapter agora é scope creep.
- **Dependências**: schema/backfill → `imedto-database` (ALTER em `imedto_assinaturas`/`imedto_planos` + nova `imedto_config_trial` + data migration idempotente). Reusa `AssinaturaService`, filtros existentes, `AssinaturaCard.vue`, handlers admin existentes (`TrocarPlanoAdminCommandHandler`, `ConcederGratuidadeAdminCommandHandler`), `IniciarTrialAoCriarEstabelecimentoHandler`.

## 9. Observações para execução — decisões de corte (não-negociáveis)

- **Estrutura nova é a fonte única.** A legada não some nesta entrega: vira read-only (F6); o **drop físico é fase posterior** — não dropar `assinaturas`/`planos`/Domain legado agora (segurança de rollback).
- **Estado é derivado de vigência+suspensão, NÃO enum.** Não recriar um `status` setável em `imedto_assinaturas`. O estado vem de `expira_em`/`suspensa_em`/existência de vigente (R3). O enum legado `StatusAssinatura` deixa de ser fonte de verdade.
- **Backfill mapeia todo mundo para Gratuidade Vitalícia preservando o estado** (R8). Não inventar planos pagos espelho — o catálogo pago é criado pelo dono no CRUD (F4). O que **não** pode acontecer: alguém liberado hoje ficar bloqueado amanhã.
- **Seed = só Gratuidade Vitalícia.** Nada de 99/149/199 hardcoded. A estrutura/CRUD tem que estar pronta para o dono criar os pagos.
- **Porta sem implementação** (R10/CA6): definir `IProvedorAssinaturaExterna` na Application como contrato; **zero** adapter/SDK/DI obrigatório. As colunas dormentes (`origem`/`referencia_externa`/`status_cobranca`) nascem coerentes com ela.
- **Invalidação de cache é obrigatória** (R11/CA32): todo handler admin que muda plano/estado chama `AssinaturaService.InvalidarCache`. Resolver o TODO existente.
- **Paridade comportamental** (R6): mesmos códigos 402, mesmas features bloqueadas, mesmos limites. O contrato de API para o front **não muda**; o front do usuário não deve precisar de alteração (só o admin muda de UI).
- **Não confundir com Financeiro/Cobranças do paciente.** São módulos distintos. Nenhuma fusão de conceitos, tabelas ou telas.
- **Liberdade técnica do dev/db**: nomes EF/coluna exatos, reuso vs. adição de coluna de vigência, forma do cache (chave/TTL), estrutura dos handlers admin novos, layout fino do admin. **Não-negociável**: fonte única nova, estado derivado, backfill seguro idempotente, paridade do enforcement, invalidação de cache, porta sem implementação, multi-tenant falha-fechada, RBAC admin global.
- **Ordem de fases**: F1 (schema) → F2 (backfill) → F3 (enforcement) → F4 (admin) → F5 (trial) → F6 (read-only). F3 nunca antes de F2 verde. F6 só após F3/F4/F5 estáveis.

## 10. Nota LGPD

- Assinatura/plano/limite/feature são **dados operacionais do SaaS** (do estabelecimento como cliente), **não** dados de saúde do paciente. Não há nova categoria de PII sensível.
- Mensagens de erro do enforcement e do admin permanecem **genéricas** (`AssinaturaInativa`, `FeatureBloqueada`, "não encontrado") — sem PII, sem revelar tenant alheio (R13/CA39).
- `referencia_externa` (futura, dormente) guardará um id de gateway — **não é PII de paciente**; quando o gateway existir, dado de cobrança do cliente (não do paciente) entra na discussão de retenção. Registrar essa nota em `Docs/LGPD.md` para o futuro, sem ação agora.

## 11. Schema para o `imedto-database`

> Migration EF + SQL idempotente em `db/migrations/`. Estrutura nova é a fonte única. **Não dropar** a legada. Reusar colunas existentes onde a semântica casar (decisão do DB agent, documentada).

**ALTER `imedto_assinaturas`**
- Inspecionar primeiro as colunas atuais (`inicio_em`, `fim_em`, `gratuita`, `motivo`, FKs). **Decidir**: `iniciada_em` reusa `inicio_em` existente ou é coluna nova? Documentar a escolha para evitar dupla semântica (risco §8).
- ADD `expira_em timestamptz NULL` (NULL = vitalício).
- ADD `suspensa_em timestamptz NULL`.
- ADD `origem text NOT NULL DEFAULT 'admin_manual'`.
- ADD `referencia_externa text NULL`.
- ADD `status_cobranca text NOT NULL DEFAULT 'nao_aplicavel'`.
- Índices: parcial de vigência `(estabelecimento_id) WHERE fim_em IS NULL` (garantir 1 vigente por estabelecimento — avaliar `UNIQUE` parcial), e `(expira_em)` para varreduras de expiração.

**ALTER `imedto_planos`**
- ADD `features jsonb NOT NULL DEFAULT '{}'` (8 chaves booleanas: `receitas`, `exame_fisico`, `procedimentos_cirurgicos`, `orcamento_completo`, `ia`, `relatorios_avancados`, `automacoes_ilimitadas`, `anexos_ilimitados`). Confirmar se já há `FeaturesJson` equivalente a reusar.
- Confirmar `LimitesJson` (`{"profissionais","pacientes"}`) existente; vazio/NULL = ilimitado. Manter.

**Nova `imedto_config_trial`** (singleton — 1 linha)
- `id uuid PK`
- `plano_trial_id uuid NOT NULL` (FK `imedto_planos`)
- `duracao_trial_dias int NOT NULL DEFAULT 14`
- `trial_habilitado boolean NOT NULL DEFAULT true`
- `atualizado_em timestamptz NOT NULL`, `atualizado_por_usuario_id uuid NULL`
- Garantir singleton (constraint de linha única ou convenção de id fixo).

**Seed (idempotente)**
- Garantir "Gratuidade Vitalícia" (UUID `00000000-0000-0000-0000-000000000001`): `features` todas `true`, limites ilimitados (NULL), `Gratuito=true`, `Ativo=true`.
- Semear `imedto_config_trial` apontando para esse plano, 14 dias, habilitado.
- **Nenhum** plano pago.

**Data migration de backfill (F2 — idempotente)**
- Para cada estabelecimento existente, se **não** houver `imedto_assinaturas` vigente (`fim_em IS NULL`), criar uma espelhando o legado conforme R8:
  - `Ativa` + `ExpiraEm=NULL` → `expira_em=NULL`, `suspensa_em=NULL`.
  - `Ativa`/`Trial` + `ExpiraEm` futuro → `expira_em=ExpiraEm`, `suspensa_em=NULL`.
  - `Suspensa`/`Cancelada` → `suspensa_em=now`.
  - `Expirada`/`ExpiraEm` passado → `expira_em` no passado (mantém bloqueado).
  - plano = "Gratuidade Vitalícia" para todos (catálogo pago ainda inexistente).
  - `origem='admin_manual'`, `status_cobranca='nao_aplicavel'`.
- Reexecutável: checar existência por `estabelecimento_id WHERE fim_em IS NULL` antes de inserir. Logar contagem (antes/depois) para o CA12/CA37.

> Observações ao DB agent: FKs com `ON DELETE RESTRICT` (assinatura não cascateia). Sem trigger/function com regra de negócio (estado derivado vive no Domain/`AssinaturaService`, não no SQL). Confirmar tipo de `estabelecimento_id` em `imedto_assinaturas` (uuid? bigint?) e alinhar o backfill ao join com a legada (bigint).

## 12. Arquivos-alvo prováveis por fase (sem código — orientação ao dev/db)

**F1 — Schema + porta**
- `db/migrations/` (nova migration: ALTER `imedto_assinaturas`/`imedto_planos`, nova `imedto_config_trial`, seed).
- `Imedto.Backend.Domain.Admin.ImedtoAssinatura` / `ImedtoPlano` (novas propriedades: vigência, suspensão, features, dormentes).
- Nova config global de trial no Domain.Admin (`ImedtoConfigTrial` ou equivalente).
- **Application** — nova interface `IProvedorAssinaturaExterna` (contrato, sem implementação).

**F2 — Backfill**
- `db/migrations/` (data migration idempotente).

**F3 — Enforcement**
- `AssinaturaService.cs` (passa a ler `imedto_assinaturas`/`imedto_planos`; `EstaVigente`/`EstaAtiva` derivam de vigência+suspensão; features/limites do plano novo).
- Repositório de leitura da assinatura vigente (novo query repository sobre `imedto_assinaturas`, multi-tenant).
- Filtros `[RequiresAssinaturaAtiva]` / `[FeatureGate]` (sem mudar contrato — só a fonte muda via `AssinaturaService`).
- `CadastrarPacienteCommandHandler` / `ConvidarProfissionalCommandHandler` (`LimiteAtingidoAsync` lê limites do plano novo).

**F4 — Admin**
- `TrocarPlanoAdminCommandHandler` / `ConcederGratuidadeAdminCommandHandler` (ações de estado R4 + chamar `InvalidarCache` R11).
- Novos handlers admin para Liberar/Suspender/Reativar/Trial + CRUD de planos (features/limites) + config de trial.
- `AdminEstabelecimentosQueryRepository.cs` (expor estado derivado + plano vigente).
- Front: `AssinaturaCard.vue` (ações + estado derivado), views admin de CRUD de planos e config de trial.

**F5 — Trial automático**
- `IniciarTrialAoCriarEstabelecimentoHandler` (grava na estrutura nova usando `imedto_config_trial`; respeita `trial_habilitado=false`).

**F6 — Descontinuar legada**
- `AssinaturaRepository`/escrita legada: tornar read-only (remover pontos de escrita do domínio). Domain/tabelas legadas permanecem (drop = fase posterior).

## 13. Atualização de documentação (parte da entrega)

- **`Docs/ARQUITETURA.md`** — atualizar a seção de domínios/contexto de **Assinaturas**: documentar que a estrutura **nova (`imedto_*`, uuid) é a fonte única** de assinatura/plano/feature/limite; o estado é **derivado de vigência (`expira_em`) + suspensão (`suspensa_em`)** e não de enum de status; o enforcement (`AssinaturaService` + filtros + limites) lê a estrutura nova; a legada está descontinuada (read-only). Adicionar a **premissa de porta de provider** (`IProvedorAssinaturaExterna`) espelhando Resend/SES/S3/LocalJwt: integração externa só via porta na Application, adapter isolado na Infraestrutura quando existir. Mudança cirúrgica — não reescrever o doc.
- **`Docs/LGPD.md`** — adicionar a nota do §10: assinatura/plano são dados operacionais do SaaS (não PII de paciente), mensagens genéricas mantidas, e nota futura sobre `referencia_externa`/cobrança do cliente quando o gateway existir.
- **`Docs/COMANDOS.md`** — **nenhuma** mudança prevista (sem script novo; backfill é migration).
- **`Docs/INFRA.md`** — **nenhuma** mudança nesta entrega (gateway de pagamento real = recurso futuro; quando entrar, INFRA.md documenta o provider).
- **`Docs/DESIGN.md`** — avaliar na F4: se as ações de estado do admin nascerem como componente reutilizável do DS, registrar; caso contrário, nenhuma mudança.
