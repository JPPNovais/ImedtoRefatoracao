# Assinatura Digital de Receitas — MVP ICP-Brasil

**ID**: 2026-06-01_001
**Status**: Aprovado por usuário em 2026-06-01
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G (nova integração OAuth2 com provedor ICP-Brasil, PAdES no backend, máquina de estados, webhook, polling, onboarding de certificado, listagem com status — sem schema de receita existente como base — cobre frontend + backend + migration)
**Áreas regressivas tocadas**: geração de PDF de receitas, módulo de prontuário/receita, autenticação (claims de papel), storage S3, jobs recorrentes.
**Discovery de referência**: [`Docs/Discoverys/assinatura-digital-receitas/01_discovery.md`](../Docs/Discoverys/assinatura-digital-receitas/01_discovery.md)
**Agentes downstream esperados**: `imedto-database` → `imedto-developer` → `imedto-qa`

---

## 1. Contexto e motivação

O Imedto já gera receitas médicas em PDF (`usePdfHeader.ts` + endpoint de geração existente). Hoje esses PDFs **não têm assinatura digital com validade jurídica** — são documentos visualmente formatados, sem a assinatura criptográfica ICP-Brasil exigida pela CFM Res. 2.299/2021 e pela Lei 14.063/2020 para que uma receita eletrônica substitua legalmente a receita em papel.

A demanda é permitir que o **médico prescritor** assine digitalmente a receita usando seu **certificado em nuvem ICP-Brasil** (NFG S2), sem hardware físico (A3 USB/cartão), de forma que:
- a farmácia aceite legalmente (incluso controle especial, mas SNCR é Fase 2),
- o paciente possa validar a autenticidade em `validar.iti.gov.br`,
- o documento tenha autoria, integridade e não-repúdio garantidos.

**Decisão arquitetural aprovada (usuário, 2026-06-01)**: Opção A do discovery — BYOC (médico traz o próprio certificado), integração via API REST do BirdID (Soluti) como provedor MVP, com camada de abstração `IAssinaturaDigitalProvider` para plugar VIDaaS / outros no futuro.

**Decisão de fluxo assíncrono (usuário, 2026-06-01)**: a assinatura em nuvem é inerentemente assíncrona (backend chama o provedor → provedor dispara PUSH no celular do médico → médico confirma → provedor chama webhook de callback com resultado). O frontend **não** fica bloqueado: ao disparar a assinatura recebe `202 Accepted` com status `AssinaturaPendente` e faz **polling leve** (~4s) em `GET /api/receitas/{id}/status-assinatura`. O provedor chama o webhook `POST /api/webhooks/assinatura/{receita_id}` no backend .NET, que atualiza o banco; o polling do frontend pega a atualização. Sem Lambda, sem WebSocket — solução em processo único .NET, suficiente para o volume MVP.

---

## 2. Escopo

### 2.1 Inclui (IN)

1. **Onboarding de certificado por médico** — fluxo guiado para vincular o certificado em nuvem (BirdID) ao cadastro do médico. Vínculo é **por conta de usuário** (vale em todos os estabelecimentos do médico). Persiste `AssinaturaCertificado` na tabela `assinatura_certificados` (médico_id, provedor, refresh_token cifrado, expira_em, criado_em).
2. **Máquina de estados da receita** — `NaoAssinada → AssinaturaPendente → AssinadaIcp | FalhaAssinatura | AssinaturaExpirada`. A assinatura é uma transição de estado da receita; a receita não-assinada continua válida. Coluna `status_assinatura` adicionada à tabela existente de receitas.
3. **Disparo de assinatura** — endpoint `POST /api/receitas/{id}/assinar`. Disponível **apenas** para o médico prescritor da receita (RBAC). Retorna `202 Accepted` com status `AssinaturaPendente`.
4. **Ação opcional na UI** — botão "Assinar digitalmente" aparece **se e somente se** o médico logado tiver certificado vinculado **e** for o prescritor da receita. Médico sem certificado vê botão "Cadastrar certificado" (inicia onboarding). Receita já assinada exibe badge de status + link de download do PDF assinado.
5. **Webhook de callback** — `POST /api/webhooks/assinatura/{receita_id}` (sem autenticação de usuário; autenticação por header assinado do provedor conforme BirdID docs). Backend valida a assinatura do callback, atualiza status no banco e persiste o PDF assinado no S3.
6. **Polling do frontend** — `GET /api/receitas/{id}/status-assinatura` — retorna `{ status, pdfAssinadoUrl? }`. Frontend polling a cada 4 segundos enquanto status = `AssinaturaPendente`. Para quando resolve (AssinadaIcp / FalhaAssinatura / AssinaturaExpirada) ou após timeout de 5 minutos (exibe alerta de "verificar novamente depois").
7. **Persistência do PDF assinado** — PDF assinado (PAdES AD_RB, perfil sem carimbo do tempo no MVP) salvo em bucket S3 existente (`imedto-anexos-*`) com presigned URL TTL 5 min. PDF original não-assinado continua existindo.
8. **Listagem com status de assinatura** — na tela de receitas do médico, coluna "Assinatura" com badge de status (NaoAssinada / Pendente / Assinada / Falha / Expirada). Médico pode **re-disparar** a assinatura a partir de status `FalhaAssinatura` ou `AssinaturaExpirada` via botão "Reenviar assinatura".
9. **Validação ITI** — geração do PDF deve garantir que passa no `validar.iti.gov.br`. Verificar com certificado real CFM em ambiente de homologação antes do QA dar aceite.
10. **Abstração de provedor** — interface `IAssinaturaDigitalProvider` com implementação `BirdIdAssinaturaProvider` (MVP). Registro por nome no container. Tabela `assinatura_certificados` guarda campo `provedor` (enum: `BirdId | VIDaaS`).
11. **Multi-tenant** — todo endpoint filtra por `estabelecimento_id` do contexto da request. Endpoint de webhook não exige tenant claim (é callback externo), mas valida que a `receita_id` pertence a um estabelecimento ativo antes de processar.
12. **LGPD** — PDF assinado é dado de saúde (Art. 11 LGPD). Presigned URL com TTL curto (5 min). Sem CPF do paciente em log. Refresh token do certificado cifrado em repouso (AES-256 via `IDataProtectionProvider`). Audit trail de disparo de assinatura e de acesso ao PDF assinado.
13. **Documentação viva** — `Docs/ARQUITETURA.md`, `Docs/LGPD.md` e `Docs/INFRA.md` atualizados nesta entrega.

### 2.2 Não inclui (OUT — track futuro)

- **Receitas de controle especial (SNCR/ANVISA)** — dependente de manuais técnicos da ANVISA (out. 2026). Discovery separado antes de prometê-lo.
- **Carimbo do tempo (AD-RT)** — MVP usa perfil AD_RB (sem carimbo). Fase 2: AD-RT quando buscar certificação SBIS.
- **Envio por WhatsApp/e-mail** — Fase 2. MVP: somente download via link assinado.
- **Lambda AWS para webhook** — julgado desnecessário para o volume MVP. O .NET absorve o callback diretamente. Reavaliar em Fase 2 se volume exigir isolamento de processo.
- **Multi-provedor simultâneo com Lacuna SDK** — camada de abstração está pronta; novo provedor entra como nova implementação de `IAssinaturaDigitalProvider` sem alterar o restante.
- **Plataforma Memed** — atalho descartado; diferenciação e controle de dado são premissas do produto.
- **Certificação SBIS** — opcional, sem data. MVP garante validade jurídica sem o selo.
- **PSC próprio** — descartado (9 meses de credenciamento ITI, inviável no estágio atual).
- **Revenda de certificado** — modelo BYOC: a plataforma **não** emite nem revende cert. O médico traz o seu.

---

## 3. Decisões cravadas

| # | Decisão | Por quê |
|---|---|---|
| D1 | Provedor MVP: **BirdID (Soluti)** | Ambiente de homologação aberto (`apihom.birdid.com.br`), certificado de teste grátis, docs públicas — menor fricção para iniciar dev e validar o ciclo técnico sem gate comercial. |
| D2 | Abstração via `IAssinaturaDigitalProvider` no Domain | Isola o code de integração. VIDaaS entra sem mexer nos handlers. |
| D3 | Formato: **PAdES AD_RB** (sem carimbo) | CFM Res. 2.299/2021 não exige carimbo explicitamente. AD_RB é o mínimo juridicamente válido. Carimbo (AD_RT) entra em Fase 2. |
| D4 | Vínculo do certificado: **por médico (usuário), não por estabelecimento** | Um médico pode atuar em vários estabelecimentos e não deve precisar vincular N vezes. Claim do tenant ainda é exigida em todo endpoint de receita (multi-tenant). |
| D5 | Apenas o **médico prescritor** pode assinar | Regra de negócio: assinatura é ato clínico pessoal. RBAC enforced no handler (422 se caller não é o prescritor). |
| D6 | Fluxo assíncrono: **polling + webhook no .NET** | Mais simples que WebSocket/SSE. Sem Lambda para o MVP. Polling de 4s é confortável para o volume de uso. |
| D7 | PDF assinado em **S3 `imedto-anexos`** (bucket existente, LGPD sensível) | Reutiliza infra existente (presigned URL TTL 5 min, Glacier após 90d). Não criar bucket novo. |
| D8 | Refresh token do certificado cifrado com `IDataProtectionProvider` | Dado sensível em repouso. Nunca persiste o token em claro. |
| D9 | Webhook sem autenticação de usuário, mas **com validação de assinatura do provedor** | É callback externo. Autenticação de usuário não se aplica; o contrato de segurança é a assinatura HMAC/JWT do payload do BirdID. |
| D10 | **Polling timeout 5 min no frontend** com mensagem orientativa | Médico pode não ter respondido o PUSH. Após timeout, UI exibe "Confirme no app do BirdID e recarregue a página" em vez de loop eterno. |
| D11 | Status `AssinaturaExpirada` quando o provedor informa expiração OU quando o webhook não chega em X horas | Job periódico (1×/hora) marca pendentes com mais de 30 minutos como `AssinaturaExpirada`. Evita "fantasmas" presos em `AssinaturaPendente`. |
| D12 | Audit trail para disparo e para download do PDF assinado | Dado de saúde + PII indireta do paciente (via receita). Audit mínimo: `quem`, `quando`, `qual receita_id`, `acao`. Sem PII de paciente no log. |
| D13 | MVP: somente **receita não-controlada** | Zero dependência de SNCR/ANVISA. Controladas são Fase 2. |
| D14 | `appsettings` guarda `AssinaturaDigital:Provedor` = `BirdId` | Trocar provedor em runtime via config sem recompilar. Registro no container lê a config. |

---

## 4. Arquitetura proposta

### 4.1 Schema (novas tabelas/colunas)

**Nova coluna em tabela existente de receitas:**
```
ALTER TABLE receitas ADD COLUMN status_assinatura text NOT NULL DEFAULT 'NaoAssinada';
ALTER TABLE receitas ADD COLUMN pdf_assinado_s3_key text NULL;
ALTER TABLE receitas ADD COLUMN assinatura_solicitada_em timestamptz NULL;
ALTER TABLE receitas ADD COLUMN assinada_em timestamptz NULL;
```

**Nova tabela `assinatura_certificados`:**
```
assinatura_certificados
  id              uuid PK
  medico_id       bigint NOT NULL FK usuarios(id) ON DELETE CASCADE
  provedor        text NOT NULL  -- 'BirdId' | 'VIDaaS'
  refresh_token   text NOT NULL  -- cifrado com IDataProtectionProvider
  expira_em       timestamptz NULL
  criado_em       timestamptz NOT NULL DEFAULT now()
  UNIQUE (medico_id, provedor)
```

**Nova tabela `assinatura_audit_log`:**
```
assinatura_audit_log
  id              bigint PK GENERATED ALWAYS AS IDENTITY
  receita_id      bigint NOT NULL  -- sem FK física (log permanece)
  estabelecimento_id bigint NOT NULL
  usuario_id      bigint NOT NULL  -- quem disparou
  acao            text NOT NULL    -- 'DISPARO_ASSINATURA' | 'DOWNLOAD_PDF_ASSINADO' | 'WEBHOOK_CALLBACK' | 'EXPIRAR_PENDENTE'
  status_anterior text NULL
  status_novo     text NULL
  criado_em       timestamptz NOT NULL DEFAULT now()
```

**Índices:**
```sql
CREATE INDEX CONCURRENTLY ix_assinatura_certificados_medico ON assinatura_certificados (medico_id);
CREATE INDEX CONCURRENTLY ix_assinatura_audit_log_receita ON assinatura_audit_log (receita_id);
CREATE INDEX CONCURRENTLY ix_assinatura_audit_log_estab_criado ON assinatura_audit_log (estabelecimento_id, criado_em DESC);
CREATE INDEX CONCURRENTLY ix_receitas_status_assinatura ON receitas (status_assinatura) WHERE status_assinatura = 'AssinaturaPendente';
```

### 4.2 Backend — bounded context `AssinaturaDigital`

**Domain:**
- `AssinaturaCertificado` (aggregate root) — `Vincular(medicoId, provedor, refreshTokenCifrado, expiraEm)`, domain event `CertificadoVinculadoEvent`.
- `AssinaturaDigitalStatus` (enum) — `NaoAssinada | AssinaturaPendente | AssinadaIcp | FalhaAssinatura | AssinaturaExpirada`.
- `IAssinaturaDigitalProvider` (interface) — `Task<DisparoAssinaturaResult> DispararAssinaturaAsync(receita, medicoId, ct)` + `Task<ValidacaoCallbackResult> ValidarCallbackAsync(payload, ct)`.
- `IAssinaturaCertificadoRepository` — `ObterPorMedicoAsync(medicoId)`, `Salvar`, `Remover`.

**Infrastructure:**
- `BirdIdAssinaturaProvider : IAssinaturaDigitalProvider` — integração OAuth2 + PKCE BirdID, monta PDF, chama API de assinatura PAdES, persiste PDF assinado no S3.
- `AssinaturaCertificadoRepository` (EF Core, escrita) + `AssinaturaQueryRepository` (Dapper, leitura).

**Commands / handlers:**
- `VincularCertificadoCommand` → `VincularCertificadoCommandHandler`
- `DispararAssinaturaCommand` → `DispararAssinaturaCommandHandler` (valida prescritor, chama provider, grava `AssinaturaPendente`, audit)
- `ProcessarCallbackAssinaturaCommand` → `ProcessarCallbackAssinaturaCommandHandler` (valida payload do BirdID, atualiza status, persiste PDF S3, audit)
- `ExpirarAssinaturasPendentesCommand` → `ExpirarAssinaturasPendentesCommandHandler` (job: marca pendentes > 30 min como `AssinaturaExpirada`)

**Queries / handlers:**
- `ObterStatusAssinaturaQuery` → `ObterStatusAssinaturaQueryHandler` (usado no polling; retorna `{ status, pdfAssinadoUrl? }`)
- `ListarReceitasComStatusAssinaturaQuery` → parte da listagem de receitas existente (adiciona `status_assinatura` à query Dapper)

**Controllers:**
- `POST /api/receitas/{id}/assinar` → `DispararAssinaturaCommand` — autenticado, RBAC médico prescritor.
- `GET /api/receitas/{id}/status-assinatura` → `ObterStatusAssinaturaQuery` — autenticado, multi-tenant.
- `POST /api/webhooks/assinatura/{receita_id}` — sem `[Authorize]`; valida assinatura do payload do BirdID no handler.
- `POST /api/medico/certificado/vincular` → `VincularCertificadoCommand` — autenticado.
- `DELETE /api/medico/certificado` → `RemoverCertificadoCommand` — autenticado.
- `GET /api/medico/certificado` → `ObterCertificadoVinculadoQuery` — autenticado (retorna apenas `provedor` e `expiraEm`, nunca o token).

**Job:**
- `ExpirarAssinaturasPendentesJob : IJobHandler` — `Nome = "expirar-assinaturas-pendentes"`, intervalo `60 * 60` (1×/hora). Chama `ExpirarAssinaturasPendentesCommandHandler`.

### 4.3 Frontend

**Service:** `assinaturaService.ts`
- `dispararAssinatura(receitaId)` → `POST /api/receitas/{id}/assinar`
- `obterStatus(receitaId)` → `GET /api/receitas/{id}/status-assinatura`
- `vincularCertificado(payload)` → `POST /api/medico/certificado/vincular`
- `removerCertificado()` → `DELETE /api/medico/certificado`
- `obterCertificadoVinculado()` → `GET /api/medico/certificado`

**Store:** `assinaturaStore.ts` (Pinia)
- `certificadoVinculado: { provedor, expiraEm } | null`
- `statusAssinatura: Record<receitaId, AssinaturaStatus>`
- `iniciarPolling(receitaId)` — `setInterval` de 4s, limpa após resolução ou 5 min
- `pararPolling(receitaId)`

**Componentes novos (design system primeiro):**
- `AssinaturaStatusBadge.vue` em `components/ui/` — badge reutilizável com variante por status (NaoAssinada=muted, Pendente=warning, Assinada=success, Falha=error, Expirada=muted). Usa `AppBadge` internamente.
- `AssinaturaOnboardingModal.vue` em `components/receitas/` — modal guiado de vinculação de certificado (step 1: instrução + link BirdID/CFM grátis; step 2: campo OAuth callback). Usa `AppModal`.
- `AssinaturaPollingIndicator.vue` em `components/receitas/` — spinner com mensagem "Aguardando confirmação no app" + botão "Cancelar" + contador regressivo de 5 min.

**Fluxo na tela de receita:**
1. Se médico é o prescritor E tem certificado → botão "Assinar digitalmente".
2. Se médico é o prescritor E **não** tem certificado → botão "Cadastrar certificado" → abre `AssinaturaOnboardingModal`.
3. Ao clicar "Assinar digitalmente" → chama `dispararAssinatura()` → exibe `AssinaturaPollingIndicator` → inicia polling.
4. Polling resolve em `AssinadaIcp` → exibe `AssinaturaStatusBadge` (Assinada) + botão "Baixar PDF assinado".
5. Polling resolve em `FalhaAssinatura` → exibe badge (Falha) + botão "Tentar novamente".
6. Timeout 5 min → exibe mensagem + botão "Verificar status" (força polling manual).

---

## 5. Modelo de dados (resumo)

| Entidade | Tabela | Chave |
|---|---|---|
| Certificado do médico | `assinatura_certificados` | `medico_id + provedor` (unique) |
| Status de assinatura da receita | coluna `status_assinatura` em `receitas` | FK implícita |
| PDF assinado | `pdf_assinado_s3_key` em `receitas` | S3 key — presigned URL gerada on-demand |
| Audit de assinatura | `assinatura_audit_log` | append-only |

---

## 6. LGPD e segurança

- **PDF assinado = dado de saúde (Art. 11 LGPD).** Trata-se de documento clínico com identificação do paciente. Armazenado em `imedto-anexos` (bucket privado, Glacier após 90d) — mesma política dos anexos de prontuário.
- **Presigned URL TTL 5 min** para download do PDF assinado. Nunca URL pública permanente.
- **Refresh token do certificado cifrado** com `IDataProtectionProvider` antes de persistir em `assinatura_certificados`. A string no banco é indecifrável sem a chave da aplicação.
- **Nunca expor refresh token** em endpoint de leitura (`GET /api/medico/certificado` retorna apenas `provedor` e `expiraEm`).
- **Sem PII em log**: audit registra apenas `receita_id`, `usuario_id`, `estabelecimento_id`, `acao`, `status_anterior`, `status_novo`. Sem nome de paciente, CPF, dados do prontuário.
- **Webhook externo sem usuário**: autenticado por validação de assinatura HMAC/JWT do payload do BirdID (conforme docs do provedor). Sem `[Authorize]` no método, mas o handler valida o payload antes de qualquer mutação. Falha na validação → 401 sem detalhe.
- **Multi-tenant no webhook**: mesmo sem token de usuário, o handler resolve o `estabelecimento_id` a partir da `receita_id` antes de processar. Receita de tenant inativo → ignora callback (sem mutação).
- **Audit obrigatório** para: disparo de assinatura, callback de conclusão, download de PDF assinado, expiração de pendente.
- **Checklist multi-tenant**: toda query de domínio filtra por `estabelecimento_id`. Mensagem genérica em erro ("receita não encontrada"). Repositório falha-fechada: sem claim de tenant → retorna vazio.

---

## 7. Critérios de aceite (Dado / Quando / Então)

### CA-01 — Onboarding: médico vincula certificado BirdID

- **Dado** um médico autenticado sem certificado vinculado
- **Quando** acessa o fluxo de vinculação e conclui o OAuth com o BirdID
- **Então** um registro é criado em `assinatura_certificados` com `provedor = 'BirdId'` e `refresh_token` cifrado
- **E** `GET /api/medico/certificado` retorna `{ provedor: 'BirdId', expiraEm: ... }` sem expor o token

### CA-02 — Multi-tenant: certificado vale em todos os estabelecimentos do médico

- **Dado** um médico com certificado vinculado que atua em 2 estabelecimentos
- **Quando** faz login em cada estabelecimento
- **Então** em ambos o botão "Assinar digitalmente" está disponível nas receitas dele
- **E** o vínculo do certificado **não** está associado a um `estabelecimento_id` específico

### CA-03 — RBAC: somente o médico prescritor pode assinar

- **Dado** uma receita criada pelo médico A no estabelecimento X
- **Quando** o médico B (mesmo estabelecimento, mesmo papel) tenta `POST /api/receitas/{id}/assinar`
- **Então** o backend retorna 422 com `BusinessException("Somente o médico prescritor pode assinar esta receita.")`
- **E** nenhuma linha é adicionada em `assinatura_audit_log`

### CA-04 — RBAC: usuário não-médico não pode disparar assinatura

- **Dado** um usuário com papel de recepcionista autenticado
- **Quando** tenta `POST /api/receitas/{id}/assinar`
- **Então** o backend retorna 403 (policy de papel)

### CA-05 — Disparo assíncrono: frontend recebe 202 e status AssinaturaPendente

- **Dado** um médico prescritor com certificado BirdID vinculado
- **Quando** chama `POST /api/receitas/{id}/assinar`
- **Então** o backend retorna 202 com `{ status: 'AssinaturaPendente' }`
- **E** `GET /api/receitas/{id}/status-assinatura` retorna `{ status: 'AssinaturaPendente', pdfAssinadoUrl: null }`
- **E** uma linha é registrada em `assinatura_audit_log` com `acao = 'DISPARO_ASSINATURA'`

### CA-06 — Polling: frontend atualiza status quando webhook chega

- **Dado** uma receita em status `AssinaturaPendente` e o frontend fazendo polling a cada 4s
- **Quando** o BirdID chama `POST /api/webhooks/assinatura/{receita_id}` com resultado de sucesso
- **Então** o backend valida a assinatura do payload, atualiza status para `AssinadaIcp`, salva o PDF assinado no S3
- **E** a próxima chamada do polling retorna `{ status: 'AssinadaIcp', pdfAssinadoUrl: 'https://...' }` (presigned URL TTL 5 min)
- **E** o frontend para o polling e exibe o badge "Assinada" + botão de download

### CA-07 — Webhook: validação de assinatura do provedor

- **Dado** uma requisição `POST /api/webhooks/assinatura/{receita_id}` com payload adulterado (HMAC inválido)
- **Quando** o handler processa
- **Então** retorna 401 sem detalhe e **nenhuma** mutação é feita no banco

### CA-08 — Webhook: multi-tenant sem claim de usuário

- **Dado** o webhook chegando para uma receita de tenant inativo
- **Quando** o handler processa após validar a assinatura do provedor
- **Então** o handler descarta silenciosamente (sem mutação, sem erro 5xx)
- **E** a resposta é 200 (evitar retry infinito do provedor)

### CA-09 — Falha: status FalhaAssinatura quando médico recusa no app

- **Dado** uma receita em `AssinaturaPendente` e o médico recusa o PUSH no BirdID
- **Quando** o BirdID chama o webhook com resultado de recusa
- **Então** o backend atualiza o status para `FalhaAssinatura`
- **E** o frontend exibe badge "Falha" + botão "Tentar novamente"
- **E** `GET /api/receitas/{id}/status-assinatura` retorna `{ status: 'FalhaAssinatura', pdfAssinadoUrl: null }`

### CA-10 — Expiração: job marca pendentes > 30 min como AssinaturaExpirada

- **Dado** uma receita em `AssinaturaPendente` com `assinatura_solicitada_em` há 31 minutos
- **Quando** o job `ExpirarAssinaturasPendentesJob` executa
- **Então** o status é atualizado para `AssinaturaExpirada`
- **E** uma linha é registrada em `assinatura_audit_log` com `acao = 'EXPIRAR_PENDENTE'`
- **E** receitas com pendência < 30 min **não** são afetadas

### CA-11 — Re-disparo: médico pode re-assinar após FalhaAssinatura ou AssinaturaExpirada

- **Dado** uma receita com status `FalhaAssinatura` ou `AssinaturaExpirada`
- **Quando** o médico prescritor chama `POST /api/receitas/{id}/assinar` novamente
- **Então** o backend aceita e transiciona para `AssinaturaPendente`
- **E** o fluxo de polling recomeça

### CA-12 — Estado imutável: receita AssinadaIcp não pode ser re-assinada

- **Dado** uma receita com status `AssinadaIcp`
- **Quando** qualquer usuário tenta `POST /api/receitas/{id}/assinar`
- **Então** o backend retorna 422 com `BusinessException("Esta receita já está assinada digitalmente.")`

### CA-13 — Listagem com status de assinatura

- **Dado** um médico com receitas em diferentes estados de assinatura
- **Quando** acessa a listagem de receitas
- **Então** cada receita exibe o badge de status correto (`AssinaturaStatusBadge`)
- **E** receitas com status `FalhaAssinatura` ou `AssinaturaExpirada` exibem botão "Reenviar assinatura"
- **E** receitas com status `AssinadaIcp` exibem botão "Baixar PDF assinado"

### CA-14 — LGPD: refresh token nunca exposto

- **Dado** um médico com certificado vinculado
- **Quando** chama `GET /api/medico/certificado`
- **Então** a resposta contém apenas `{ provedor: 'BirdId', expiraEm: '...' }`
- **E** o campo `refresh_token` **nunca** aparece em nenhum payload retornado ao cliente

### CA-15 — LGPD: sem PII em logs

- **Dado** qualquer operação de assinatura (disparo, callback, expiração, download)
- **Quando** o sistema registra audit ou logs estruturados
- **Então** nenhum campo de nome de paciente, CPF, data de nascimento ou dado clínico aparece nos logs
- **E** o audit contém apenas `receita_id`, `usuario_id`, `estabelecimento_id`, `acao`, `status_anterior`, `status_novo`, `criado_em`

### CA-16 — LGPD: presigned URL com TTL 5 min

- **Dado** uma receita com status `AssinadaIcp`
- **Quando** o frontend obtém a URL de download do PDF assinado
- **Então** a URL é uma presigned URL S3 com TTL de 5 minutos
- **E** a URL expira após 5 minutos (requisição após TTL retorna 403 do S3)

### CA-17 — Validação ITI: PDF assinado passa no validador

- **Dado** uma assinatura concluída com certificado ICP-Brasil real (certificado de homologação CFM ou BirdID real)
- **Quando** o PDF assinado é enviado ao `validar.iti.gov.br`
- **Então** o resultado é "Assinatura válida" para ICP-Brasil
- **E** o signatário identificado é o médico prescritor (CPF + nome do CRM)

### CA-18 — Abstração de provedor: registro via config

- **Dado** `appsettings.json` com `AssinaturaDigital:Provedor = "BirdId"`
- **Quando** o container resolve `IAssinaturaDigitalProvider`
- **Então** a implementação `BirdIdAssinaturaProvider` é injetada
- **E** nenhum outro código conhece o tipo concreto (handler usa apenas a interface)

### CA-19 — Receita não-assinada coexiste normalmente

- **Dado** uma receita gerada antes da feature de assinatura
- **Quando** o médico acessa o prontuário
- **Então** a receita exibe status `NaoAssinada` e continua sendo baixável normalmente (PDF original)
- **E** o botão "Assinar digitalmente" aparece como ação opcional (não substitui nem bloqueia o download original)

### CA-20 — Polling timeout: mensagem orientativa após 5 min

- **Dado** uma receita em `AssinaturaPendente` e o médico não respondeu o PUSH
- **Quando** o frontend atinge 5 minutos de polling sem resolução
- **Então** o polling para automaticamente
- **E** a UI exibe mensagem "Não recebemos confirmação do BirdID. Verifique o app e clique em 'Verificar status' para atualizar."
- **E** um botão "Verificar status" permite disparar polling manual

### CA-21 — Multi-tenant: query de status filtra por estabelecimento

- **Dado** dois médicos em estabelecimentos diferentes com receitas distintas
- **Quando** o médico A tenta `GET /api/receitas/{id_receita_medico_B}/status-assinatura`
- **Então** o backend retorna 404 ("receita não encontrada") sem revelar que a receita existe em outro tenant

### CA-22 — Segurança: usuário não-autenticado não acessa status de assinatura

- **Dado** uma requisição sem token para `GET /api/receitas/{id}/status-assinatura`
- **Quando** processada pelo backend
- **Então** retorna 401

### CA-23 — Remoção de certificado

- **Dado** um médico com certificado vinculado
- **Quando** chama `DELETE /api/medico/certificado`
- **Então** o registro é removido de `assinatura_certificados`
- **E** receitas em `AssinaturaPendente` do médico são mantidas (o webhook ainda pode chegar)
- **E** novas assinaturas do médico requerem novo onboarding

### CA-24 — Build e testes verdes

- **Dado** a entrega completa
- **Quando** roda `dotnet build` e `dotnet test` e `npm run build`
- **Então** todos retornam sucesso sem warning de compilação novo

---

## 8. Riscos e mitigações

| # | Risco | Probabilidade | Mitigação |
|---|---|---|---|
| R1 | **Gate comercial BirdID** — acesso ao ambiente de produção exige aprovação da Soluti | Média | Desenvolver e validar em homologação (`apihom.birdid.com.br`) com cert de teste. Solicitar acesso de produção em paralelo. O deploy pode ficar em homologação até o gate liberar. |
| R2 | **Fluxo OAuth PKCE do BirdID difere da documentação** | Média | Ambiente de homologação + cert de teste permitem validação completa antes de entrar em produção. QA valida CA-17 com certificado real antes de release. |
| R3 | **PDF PAdES gerado não passa no Validar ITI** | Média | Validar manualmente no `validar.iti.gov.br` durante o desenvolvimento (CA-17). Biblioteca recomendada: iText 7 (community + bouncy castle) ou Lacuna PKI SDK. |
| R4 | **Webhook do BirdID chega depois do timeout do polling** | Baixa | Status `AssinaturaExpirada` pelo job é sobrescrito por `AssinadaIcp` se o webhook chegar depois (transição de estado permissiva apenas de expirada→assinada). Médico não perde a assinatura. |
| R5 | **Refresh token do BirdID expira antes do médico assinar** | Baixa | `expira_em` em `assinatura_certificados`. UI exibe aviso "Certificado expirado — renovar vínculo" quando `expira_em < now + 7d`. |
| R6 | **Volume alto de polling simultaneamente** | Baixa (MVP) | Polling de 4s por usuário ativo na tela. Endpoint `GET /status-assinatura` é Dapper puro (leitura rápida). Sem risco no volume MVP. |
| R7 | **Job `expirar-assinaturas-pendentes` falha silenciosamente** | Baixa | Padrão existente: scheduler loga warning se handler não registrado. Job tem log estruturado por execução. QA valida CA-10. |
| R8 | **Receita de controlado acidentalmente submetida à assinatura** | Baixa | MVP não bloqueia por tipo de receita no código (SNCR é Fase 2). UI pode exibir aviso informativo se o tipo de receita for controlado, mas sem gate técnico no MVP. |

---

## 9. Hand-off

### Sequência obrigatória

1. **`imedto-database`**:
   - Inspecionar tabela `receitas` existente para confirmar nome exato e PK.
   - Gerar migration EF + SQL idempotente para: (a) coluna `status_assinatura` + `pdf_assinado_s3_key` + `assinatura_solicitada_em` + `assinada_em` em `receitas`; (b) tabela `assinatura_certificados`; (c) tabela `assinatura_audit_log`; (d) índices CONCURRENTLY.
   - Aplicar em dev. Validar com `\d receitas`, `\d assinatura_certificados`, `\d assinatura_audit_log`.
   - Devolver ao developer confirmando nomes de tabela/coluna exatos.

2. **`imedto-developer`**:
   - Implementar domain, contracts, application (handlers), infrastructure (BirdId provider, repositórios, job).
   - Implementar controllers e registros DI.
   - Implementar frontend: service, store, componentes novos, fluxo de polling, onboarding modal.
   - Adicionar testes unitários (handlers: RBAC, máquina de estados, expiração) e integração leve (webhook handler).
   - Atualizar `Docs/ARQUITETURA.md`, `Docs/LGPD.md`, `Docs/INFRA.md` (S3, SSM com novo parameter `assinatura/birdid-client-secret`).

3. **`imedto-qa`**:
   - Validar CA-01 a CA-24.
   - Atenção especial: CA-03 (RBAC), CA-07 (webhook HMAC), CA-15 (sem PII em log), CA-17 (Validar ITI — requer certificado real de homologação), CA-21 (multi-tenant).
   - Commit com referência ao briefing `2026-06-01_001`.
   - 1 push ao final da sessão.

### Configurações novas necessárias (SSM / appsettings)

| Parameter | Onde | Conteúdo |
|---|---|---|
| `AssinaturaDigital:Provedor` | appsettings | `"BirdId"` |
| `AssinaturaDigital:BirdId:ClientId` | SSM `/imedto/dev/assinatura/birdid-client-id` | client_id da aplicação no BirdID |
| `AssinaturaDigital:BirdId:ClientSecret` | SSM `/imedto/dev/assinatura/birdid-client-secret` | SecureString |
| `AssinaturaDigital:BirdId:BaseUrl` | appsettings | `https://apihom.birdid.com.br` (dev) / `https://api.birdid.com.br` (prod) |
| `AssinaturaDigital:BirdId:WebhookSecret` | SSM `/imedto/dev/assinatura/birdid-webhook-secret` | SecureString — validar HMAC do callback |
| `AssinaturaDigital:ExpiracaoPendenteMinutos` | appsettings | `30` |

---

## 10. Atualização de documentação

### `Docs/ARQUITETURA.md`

Adicionar seção "Bounded Context: AssinaturaDigital" cobrindo:
- Abstração `IAssinaturaDigitalProvider` (Domain) — padrão de extensão para novos provedores.
- Job `expirar-assinaturas-pendentes` (1×/hora, batch).
- Endpoint de webhook sem `[Authorize]` (único caso no sistema — justificativa: callback externo autenticado por HMAC do provedor).
- Polling strategy: frontend faz GET a cada 4s, timeout 5 min.

### `Docs/LGPD.md`

Adicionar na seção de dados sensíveis:
- `assinatura_certificados.refresh_token` — dado sensível cifrado em repouso (IDataProtectionProvider). Nunca exposto em payload.
- `pdf_assinado_s3_key` — PDF assinado contém dados de saúde (Art. 11 LGPD). Acesso via presigned URL TTL 5 min.
- Audit trail `assinatura_audit_log` — sem PII de paciente; retenção 730 dias (implicação legal de documento médico assinado).

### `Docs/INFRA.md`

Adicionar na seção SSM:
- Novos parameters: `assinatura/birdid-client-id`, `assinatura/birdid-client-secret`, `assinatura/birdid-webhook-secret`.

Adicionar nota sobre uso do bucket `imedto-anexos` para PDFs assinados (mesmo bucket, prefixo `receitas-assinadas/`).

---

## 11. Próximos briefings sugeridos (backlog)

- **Assinatura Fase 2** — Carimbo do tempo (AD-RT), multi-provedor com VIDaaS, envio por WhatsApp/e-mail do PDF assinado.
- **Assinatura Fase 3** — Integração SNCR/ANVISA para receitas de controle especial (depende de manuais técnicos ANVISA, previsão pós set/2026).
- **Discovery: Lacuna Rest PKI** — avaliar custo de licença vs. manutenção de integrações diretas com N provedores. Gatilho: quando segundo provedor for adicionado.
