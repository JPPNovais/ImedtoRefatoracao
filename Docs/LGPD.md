# LGPD e segurança de dados sensíveis

> **Quando ler**: ao tocar feature que envolve paciente, prontuário, agendamento, financeiro, anexo, audit, dado pessoal de usuário, mensagem de erro, log estruturado, payload exposto ao cliente.
>
> **Quando atualizar**: ao introduzir novo tipo de dado pessoal, novo endpoint que expõe PII, nova regra de retenção/exclusão, novo audit. **Responsabilidade primária: `imedto-business-analyst`** (LGPD é premissa de produto, não apenas técnica).

---

Sistema de saúde → dados pessoais sensíveis (Art. 5º II e Art. 11 LGPD). LGPD é **premissa de design**, não checklist de fim de PR — a cada feature nova pergunte: *"este dado é necessário? Está minimizado? Tem RLS? Há audit trail? Pode vazar em log/erro?"*.

## Requisitos obrigatórios

- **Minimização**: query/DTO retorna apenas os campos que a tela usa. Não trazer `cpf`, `data_nascimento`, `telefone`, etc. se a tela não exibe.
- **Direitos do titular**: endpoints de export (`GET /api/minha-conta/exportar-dados`) e exclusão (`DELETE /api/minha-conta`).
- **Audit trail**: log de acesso a dados de paciente/prontuário em audit table (quem, quando, qual registro). Operações auditadas em `prontuario_acesso_log` (`TipoAcessoProntuario`): `Leitura` (consulta ao prontuário/timeline), `Escrita` (registro de evolução, início de prontuário) e `Exportacao` — exportação completa do prontuário (`GET /api/prontuario/{id}/pdf`), exportação de evolução individual e **download do PDF oficial de receita** (`GET /api/receitas/{id}/pdf`, briefing 2026-06-10_001). Audit de exportação de receita é best-effort (falha não bloqueia o download) e condicional à existência de prontuário para o paciente.
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

## PDF probatório de termo de consentimento — briefing 2026-06-10_002

Geração server-side do documento probatório de aceite digital do termo de consentimento.

- **Endpoint**: `GET /api/termos/{id}/pdf-gerado` — gera e devolve `application/pdf` com o snapshot da versão aceita + bloco de evidência do aceite. Gate: `[RequiresAcao("termos", "emitir")]`.
- **Token de aceite**: nunca exposto completo no PDF. O bloco de evidência exibe apenas os **últimos 6 caracteres** (`TokenAceite[^6..]`) e o **hash SHA-256** (`HashIntegridade`). O token completo nunca transita em payload, log ou PDF.
- **Audit**: cada geração bem-sucedida registra 1 linha em `termo_audit_log` via `ITermoAuditLogger` com `{ estabelecimento_id, usuario_id, acao = "termo-pdf-gerado", entidade = "TermoEmitido", entidade_id }`. O audit é **best-effort** — falha não bloqueia o download do PDF.
- **Minimização — nome do arquivo**: `Content-Disposition` usa `termo-{id}.pdf` — sem nome/CPF/dados do paciente.
- **Dados degradados graciosamente**: `IpAssinatura`/`UserAgentAssinatura` nulos (termos antigos) são omitidos do bloco de evidência sem quebrar o documento. Todos os campos opcionais fazem null-check antes de renderizar.
- **Multi-tenant**: query filtra `estabelecimento_id = @EstabelecimentoId`; termo de outro tenant → `BusinessException("Termo não encontrado.")` (mensagem genérica — não vaza existência).

## Relatório de acessos ao titular (Art. 9º/18) — briefing 2026-06-10_007

Implementa o direito do titular de saber quem acessou seus dados (Art. 9º e Art. 18 LGPD).

- **Endpoint**: `GET /api/paciente/{id}/acessos?pagina=&tamanho=` — consolida `paciente_acesso_log` + `prontuario_acesso_log` em lista paginada (server-side) em linguagem leiga. Gate: `[RequiresPapel(TenantPapel.Dono)]`.
- **O próprio acesso ao relatório é auditado**: cada carga de página registra 1 linha em `paciente_acesso_log` com `TipoAcessoPaciente.Leitura` (`{ paciente_id, usuario_id, estabelecimento_id, ocorrido_em, ip_origem }`). O export PDF também aciona o endpoint (gera 1 linha). Acesso a dado sobre acessos é acesso — correto por design.
- **DTO minimizado**: cada item contém apenas `{ quem, quando, recurso, acao }` — sem `usuario_id` cru, `ip_origem`, `prontuario_id`, CPF, telefone ou conteúdo clínico. Rótulo leigo montado via CASE WHEN no SQL (fonte única — lista e PDF reusam o mesmo valor). Usuário removido/anonimizado exibe "Usuário removido" — nunca vaza o ID cru.
- **Multi-tenant rígido**: ambas as subconsultas filtram `estabelecimento_id`; validação de posse do paciente no tenant antes de qualquer leitura; mensagem genérica ("Paciente não encontrado.").
- **PDF**: composable `useAcessosPdf.ts` — cabeçalho institucional (Nunito), título "RELATÓRIO DE ACESSOS — LGPD", nome do paciente no subtítulo (entregue ao próprio titular — não é vazamento), tabela **Quem | O quê | Quando**, rodapé "Relatório de acessos — Art. 9º/18 LGPD.". Sem CPF/telefone do paciente. Teto de 500 registros por PDF com nota de rodapé se houver mais.
- **Nenhum dado pessoal novo é coletado** — apenas expõe o que já é gravado em audit trail existente.
- **Fontes excluídas do MVP**: `termo_emitido_acesso_log` e `agendamento_confirmacao_acesso_log` (acesso público do próprio titular via token, sem `usuario_id`). Backlog se solicitado.

## Autenticação de dois fatores (2FA TOTP) — dados sensíveis (briefing 2026-06-10_006)

A feature de 2FA introduz novos dados sensíveis com tratamento obrigatório:

| Dado | Tabela / campo | Classificação | Regra |
|---|---|---|---|
| Segredo TOTP | `usuario_2fa.segredo_cifrado` | Credencial de 2º fator — equivale à chave privada do app autenticador do usuário | Cifrado com `IDataProtector.CreateProtector("auth.totp.secret")` → `.Protect()` antes de persistir. Decifrado em memória **apenas** no momento de validar um código. **Nunca** retornado em payload pós-ativação — `otpauthUri`/base32 só transitam durante o fluxo de ativação, antes da confirmação (passo 1 do `iniciar`). |
| Códigos de recuperação | `usuario_2fa_codigo_recuperacao.codigo_hash` | Credencial one-time de emergência | Armazenados **exclusivamente como hash**, usando o mesmo hasher de senha do projeto (BCrypt + pepper). Códigos em claro exibidos ao usuário **uma única vez** (passo 3 da ativação). Consumo idempotente-seguro: um código marcado com `usado_em` nunca mais autentica. |
| Desafio de login | Não persistido | Token efêmero de coordenação entre passo 1 e passo 2 do login | Token opaco cifrado via `IDataProtector.CreateProtector("auth.totp.challenge")` contendo `{ usuario_id, exp }`. TTL 5 minutos. Sem tabela — zero lixo acumulado. `usuario_id` nunca transita em claro no desafio. |
| Audit de segurança de conta | `usuario_seguranca_audit` | Metadado operacional — sem PII | Registra: `Ativou2fa`, `Desativou2fa`, `UsouCodigoRecuperacao`. Campos: `{ id, usuario_id, acao, ocorrido_em, ip_origem? }`. **Nunca** nome, CPF, telefone, e-mail, segredo ou código. Retenção: **365 dias** (alinhado a `paciente_acesso_log`). Logins bem-sucedidos com TOTP **não** geram linha — são login normal, auditados pelo fluxo de sessão já existente. |

**Anti-bypass como INVARIANTE de segurança**: nenhum caminho emite cookies de sessão para usuário com 2FA ativo sem o passo 2 concluído com sucesso. Qualquer desvio é vulnerabilidade.

**Mensagens de erro genéricas**: erros de 2FA (código inválido, desafio expirado, falha de desativação) retornam mensagens genéricas ("Código inválido.", "Não foi possível completar a verificação.") sem revelar se o usuário existe, se o código estava próximo de válido ou qual fator falhou.

**Separação de lockout**: falhas no passo 2 (código TOTP/recuperação) **não** incrementam `TentativasFalhas` da senha em `auth_credenciais` — isso permitiria que um atacante travasse a conta da vítima errando apenas códigos. O rate limit do passo 2 é a política `auth-sensitive` (3 req/IP, sliding window), independente do lockout de senha.

**Multi-tenant do estado de 2FA**: o estado ativo/inativo é **global** (da conta do usuário, não por tenant). O toggle de exigência (`exigir_2fa_dono`) é por estabelecimento. Um Dono com N estabelecimentos tem um único segredo TOTP que serve para todos os contextos.

## Dado financeiro do paciente — sensível (briefing 2026-06-10_009, Módulo Cobranças F1)

Contas a receber do paciente (`Cobranca`/`Pagamento`) são dado **sensível** — vinculam valor cobrado/pago a um paciente identificado.

- **Minimização**: DTOs de cobrança/pagamento contêm **apenas** os campos da tela (saldo, valores, forma, status). Nunca CPF/diagnóstico/conteúdo clínico no payload financeiro.
- **Sem PII em log/erro**: mensagens de 422/404 de cobrança/pagamento são **genéricas** ("Cobrança não encontrada.", "Valor excede o saldo.") — não revelam nome/CPF do paciente nem valores de tenant alheio. Logs de domínio sem PII.
- **Multi-tenant falha-fechada**: `Cobranca`/`Pagamento`/`TabelaPrecoConsulta`/`ConfigTaxaFormaPagamento` filtram `estabelecimento_id` do tenant ativo; acesso a id de outro tenant → "não encontrado" genérico.
- **Permissões dedicadas**: `financeiro_paciente.ver` / `financeiro_paciente.registrar` — **separadas** de `financeiro.*` (Financeiro da clínica, agregado, mais amplo). Não confundir.
- **Audit de acesso por paciente** (F2, briefing 2026-06-10_010): a **aba Financeiro do paciente** (`PacienteDetalheView.vue`, porta direta ao dado financeiro por paciente identificado) registra **1 linha por carga** em `paciente_acesso_log` via `IPacienteAcessoLogService.RegistrarAsync(paciente_id, usuario_id, estabelecimento_id, TipoAcessoPaciente.Leitura)` — reuso do serviço existente (best-effort; falha do log não quebra o fluxo). **Sem tabela/serviço de audit novo.** Na F1, criação de cobrança/pagamento já registra `criado_por`/`registrado_por_usuario_id`+timestamps (rastreabilidade de quem cobrou/recebeu).
- **Estorno (F2)**: o `motivo` do estorno é texto operacional — **não** carrega PII; mensagens 422/404 do fluxo de estorno são genéricas. DTO da aba é mínimo (só cobranças/pagamentos/estornos do paciente no tenant — sem dado clínico). `financeiro_paciente.registrar` cobre registrar pagamento **e** estornar (sem permissão de estorno separada).

---

## Checklist multi-tenant — premissa não-negociável

Antes de cada commit que toca dados de domínio (paciente, agendamento, prontuário, financeiro, equipe, estoque, orçamento, **assinatura digital**), valide:

1. **Filtro por `estabelecimento_id`** em todo `WHERE`/join de domínio.
2. **Verificação de vínculo** do usuário com o estabelecimento (papel + escopo do vínculo).
3. **Mensagem genérica em erro** ("não encontrado") — nunca revelar se o registro existe em outro tenant.
4. **Repositório falha-fechada**: ausência de tenant claim → retorna vazio/throws, nunca query global.
