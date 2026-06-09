# LGPD e segurança de dados sensíveis

> **Quando ler**: ao tocar feature que envolve paciente, prontuário, agendamento, financeiro, anexo, audit, dado pessoal de usuário, mensagem de erro, log estruturado, payload exposto ao cliente.
>
> **Quando atualizar**: ao introduzir novo tipo de dado pessoal, novo endpoint que expõe PII, nova regra de retenção/exclusão, novo audit. **Responsabilidade primária: `imedto-business-analyst`** (LGPD é premissa de produto, não apenas técnica).

---

Sistema de saúde → dados pessoais sensíveis (Art. 5º II e Art. 11 LGPD). LGPD é **premissa de design**, não checklist de fim de PR — a cada feature nova pergunte: *"este dado é necessário? Está minimizado? Tem RLS? Há audit trail? Pode vazar em log/erro?"*.

## Requisitos obrigatórios

- **Minimização**: query/DTO retorna apenas os campos que a tela usa. Não trazer `cpf`, `data_nascimento`, `telefone`, etc. se a tela não exibe.
- **Direitos do titular**: endpoints de export (`GET /api/minha-conta/exportar-dados`) e exclusão (`DELETE /api/minha-conta`).
- **Audit trail**: log de acesso a dados de paciente/prontuário em audit table (quem, quando, qual registro).
- **Não vazar PII**:
  - Nunca incluir CPF, telefone, e-mail ou nome completo em log estruturado ou em mensagem de erro retornada ao cliente.
  - Nunca retornar token, hash de senha ou ID interno de auth em payload.
  - Mensagens de erro de validação devem ser genéricas ("paciente não encontrado") em vez de descrever o dado consultado.
- **Sem RLS** no Postgres (decisão arquitetural — ver [INFRA.md §Decisões](INFRA.md#decisões-já-tomadas-não-reabrir-sem-motivo-forte)). Defense-in-depth fica no backend (filter multi-tenant + `[RequiresPapel]` + mensagens genéricas).
- **Filtro por estabelecimento**: queries de domínio sempre exigem `estabelecimento_id` no `WHERE` ou no `Include`/join. Multi-tenant é regra, não exceção.
- **Consentimento**: se a feature coleta dado novo do titular (ex: novo campo de paciente), avaliar se precisa de aviso/consentimento explícito antes de armazenar.

## Regras de segurança e performance

- As regras do front que dependem de retorno ou input da API precisam estar sendo validadas também no backend, para evitar de pessoas tentando passar informação direto pela API. Defense-in-depth: front é UX, **back é a fonte da verdade**.
- As APIs precisam respeitar segurança de autenticação — não pode deixar que uma pessoa altere registros caso ela não tenha a devida permissão para isso.
- Todo o site precisa ser pensado de forma que ele pode escalar, ter muitas requisições ao mesmo tempo. Precisa ter resiliência e pensar em performance — não pode demorar o retorno, mas precisa estar muito bem otimizado para não pesar para o usuário.
- Deve buscar basicamente as informações necessárias para aquele momento — caso não tenha clicado em uma aba que ainda não vai utilizar, não precisa ficar fazendo consulta desnecessária (busca apenas quando precisar de fato).
- As rotas que puderem ser reutilizadas precisam ser feitas, para evitar criação de novos endpoints desnecessários que fazem praticamente a mesma coisa.
- O código precisa ser simples de entendimento e componentizado para reutilizar o que for possível em outras partes evitando códigos duplicados.
- Páginas do site precisam estar centralizadas no meio, evitando deixar espaço em branco só na direita ou só na esquerda (ver [DESIGN.md §Container](DESIGN.md#experiência-consistente-em-todo-o-site)).
- Os componentes do front precisam estar padronizados e reutilizados como um todo de acordo com sua necessidade — design system primeiro.
- As regras de negócio devem estar todas no backend, transparente para o front, a fim de evitar problemas de segurança também.

## Acesso de admin global — regras de privacidade e isolamento

O admin global (`imedto_admin`) opera fora do contexto de tenant. Regras específicas:

- **Sem `estabelecimento_id` no JWT admin** — o token nunca carrega claim de tenant. Qualquer rota de domínio que leia `estabelecimento_id` do token vai negar acesso automaticamente (falha-fechada).
- **Auditoria obrigatória** — toda ação sensível do admin (login, falha de login, troca de senha, criação/desativação de admin) registra linha em `imedto_admin_audit_logs` via `ImedtoAdminAuditWriter`. Log inclui `admin_id`, `acao`, `recurso_tipo`, `recurso_id`, `ip`, `user_agent`, `motivo` e `criado_em`. Nunca PII de paciente.
- **Audit do catálogo global de regiões anatômicas** — toda mutação no catálogo (criar, editar, inativar, reativar, excluir) é auditada em `imedto_admin_audit_log` com `{ admin_id, recurso_tipo='regiao_anatomica', recurso_id, motivo, timestamp }` via `AcoesAuditAdmin` (constantes `CRIAR_REGIAO_ANATOMICA`, `ATUALIZAR_REGIAO_ANATOMICA`, `INATIVAR_REGIAO_ANATOMICA`, `REATIVAR_REGIAO_ANATOMICA`, `EXCLUIR_REGIAO_ANATOMICA`). Motivo obrigatório (≥10 chars) em toda mutação. Política padrão: **soft-delete (inativar) é a ação de remoção preferida** — preserva a integridade de prontuários que referenciam o código da região (prontuários são imutáveis; o nó inativado não aparece em *novas* seleções mas continua legível no histórico). O catálogo é global e não contém PII de paciente — mensagens de erro são genéricas ("não encontrada"; "agregadores e não aceitam sub-regiões.") e não revelam dados de tenant ou de paciente. Retenção: 365 dias (definida em `AuditLogRetencao.PorAcao`). Adicionado em briefing `2026-06-08_007`.
- **Mensagem genérica em credenciais inválidas** — "Credenciais inválidas." independentemente de o e-mail existir ou não. Não revelar existência do registro.
- **Sem PII em logs do admin** — nenhum dado de paciente, CPF, telefone ou dado de estabelecimento em `_logger.*`. Apenas IDs e ações.
- **Refresh token admin** — TTL 2h, armazenado como hash SHA-256 em `imedto_admin_refresh_tokens`. Token cru nunca persistido.
- **Inatividade** — sessão admin expira automaticamente após 15 min de inatividade no frontend (`adminAuthStore`).
- **Cross-blindagem** — admin não acessa endpoints de tenant (policy + filtro global `AdminBlindagemFilter`). Usuário de tenant não acessa `/api/admin/*` (policy `ImedtoAdmin`).

## Assinatura digital de receitas — dados sensíveis (briefing 2026-06-01_001)

A feature de assinatura digital introduz novos dados sensíveis com tratamento obrigatório:

| Dado | Tabela / campo | Classificação | Regra |
|---|---|---|---|
| Refresh token do certificado BirdID | `assinatura_certificados.refresh_token` | Credencial de acesso ao certificado ICP-Brasil do médico | Cifrado com `IDataProtectionProvider` antes de persistir. **Nunca** retornado em payload de API — `GET /api/medico/certificado` expõe apenas `provedor` e `expiraEm`. |
| PDF assinado digitalmente | `receitas.pdf_assinado_s3_key` → S3 `imedto-anexos` | Dado de saúde (Art. 11 LGPD) — documento clínico com identificação do paciente | Bucket privado. Acesso exclusivamente via presigned URL TTL **5 minutos**. Sem URL pública permanente. |
| Audit de assinatura | `assinatura_audit_log` | Metadado operacional — sem PII de paciente | Retenção **730 dias** (implicação de documento médico assinado com validade jurídica). Contém apenas: `receita_id`, `usuario_id`, `estabelecimento_id`, `acao`, `status_anterior`, `status_novo`, `criado_em`. Nunca nome/CPF do paciente. |

**Webhook de callback (BirdID)**: endpoint sem autenticação JWT de usuário (`POST /api/webhooks/assinatura/{receita_id}`). A ausência de `[Authorize]` é deliberada (callback externo). Segurança é feita no handler via validação de HMAC/JWT do payload do provedor. **Nenhuma PII do paciente transita no callback** — apenas IDs e resultado da assinatura.

**Multi-tenant no webhook**: mesmo sem token de usuário, o handler resolve o `estabelecimento_id` a partir da `receita_id` e valida que o tenant está ativo antes de qualquer mutação. Tenant inativo → descarte silencioso, sem mutação.

## Confirmação por link público de agendamento — log de acesso (briefing 2026-06-02_001, Fase 2)

A Fase 2 introduz um novo ponto de coleta de dado operacional para o fluxo público de confirmação de presença:

| Dado | Tabela / campo | Classificação | Regra |
|---|---|---|---|
| Log de acesso ao link público | `agendamento_confirmacao_acesso_log` (`ip_origem`, `user_agent`, `acao`, `acessado_em`, `agendamento_id`, `estabelecimento_id`) | Metadado operacional — **sem PII do paciente** | Idêntico ao padrão de `termo_emitido_acesso_log` (Fase 4, Termos). Registra IP, UserAgent e ação (`visualizou_publico` / `confirmou_presenca` / `tentativa_invalida` / `tentativa_idempotente`). Retenção alinhada à de Termos. Sem `paciente_id`, nome, CPF ou e-mail. |
| Token de confirmação | `agendamentos.token_confirmacao` | Credencial temporária (256 bits) | Token **nunca logado em texto claro**. Expira em `min(agora + 7 dias, InicioPrevisto)`. Único via índice parcial. Sobrescrito a cada reagendamento. Não exposto em payload de API (só embutido no link do e-mail). |

**Endpoint público** (`GET`/`POST /api/publico/agendamentos/confirmar/{token}`):
- Resolve o agendamento **pelo token**, sem tenant claim (o token é o único segredo).
- Payload de resposta: apenas `estabelecimentoNome`, `profissionalNome`, `tipoServico`, `inicioPrevisto`, `fimPrevisto`. **Nunca** `paciente_id`, `estabelecimento_id`, nome do paciente, CPF ou e-mail.
- Erros (token inválido/expirado/cancelado) → 410 Gone com mensagem **genérica idêntica** em todos os casos.
- Rate limit: 10 req/min por IP (política `agendamentos-publico`).

**Referência cruzada**: padrão idêntico ao já documentado para Termos (`termo_emitido_acesso_log`). Se a política de retenção for alterada para Termos, aplicar o mesmo ajuste aqui.

## Pool de variáveis do prontuário — minimização de dados (briefing 2026-06-05_001)

A tabela `prontuario_variaveis_pool` guarda nomes genéricos de itens clínicos (ex.: "Dipirona", "Hipertensão") — **não é PII de paciente**. Regras de uso:

- **Só o campo `nome` vira item de pool.** A extração automática ao salvar evolução (`PoolExtratorEvolucao`) coleta apenas os campos `nome`/`parentesco` dos arrays mapeados. Campos livres (`observacao`, `dose`, `frequencia`, `motivo`, `ano`, `doencas`, `comentario`) **jamais** viram itens de pool — contêm contexto clínico específico de paciente.
- **Sem audit extra para criação no pool.** A criação automática ocorre dentro do fluxo de `RegistrarEvolucao`, que já audita a escrita no prontuário. Itens de pool são dados de catálogo (não de paciente), portanto não exigem linha adicional em audit table.
- **Dedup canônica é LGPD-segura.** A normalização (trim + lower + sem acento) ocorre em memória antes de criar item; não persiste a forma bruta digitada pelo profissional quando colide com existente.
- **Sem PII em log.** Nenhum campo livre (que pode conter nome/contexto do paciente) transita por `_logger.*`. `PoolExtratorEvolucao` opera em silêncio — falha-suave sem log de dados da evolução.

## Checklist multi-tenant — premissa não-negociável

Antes de cada commit que toca dados de domínio (paciente, agendamento, prontuário, financeiro, equipe, estoque, orçamento, **assinatura digital**), valide:

1. **Filtro por `estabelecimento_id`** em todo `WHERE`/join de domínio.
2. **Verificação de vínculo** do usuário com o estabelecimento (papel + escopo do vínculo).
3. **Mensagem genérica em erro** ("não encontrado") — nunca revelar se o registro existe em outro tenant.
4. **Repositório falha-fechada**: ausência de tenant claim → retorna vazio/throws, nunca query global.
