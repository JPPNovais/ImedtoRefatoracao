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

## Lembrete por WhatsApp — consentimento e envio a terceiro (briefing 2026-06-18_005)

O lembrete de consulta ganha um canal WhatsApp complementar ao e-mail (mesmo job recorrente). Isso introduz **(a)** um novo dado pessoal — o **consentimento explícito (opt-in)** do titular — e **(b)** o **uso do telefone do paciente em compartilhamento com terceiro** (Meta / WhatsApp Cloud API).

| Dado | Tabela / campo | Classificação | Regra |
|---|---|---|---|
| Opt-in de lembrete WhatsApp | `pacientes.whatsapp_lembrete_opt_in`, `..._opt_in_em`, `..._opt_in_por_usuario_id` | Consentimento do titular (Art. 7º I / Art. 11 I LGPD) | **Base legal = consentimento explícito.** Sem opt-in marcado, o sistema **nunca** envia WhatsApp — mesmo com telefone válido e canal habilitado. Marcar/desmarcar grava data/hora + `usuario_id` de quem registrou. |
| Telefone do paciente (uso para envio) | `pacientes.telefone` (já existente) | Dado pessoal | **Compartilhamento com terceiro** (Meta): o telefone só é normalizado a E.164 e enviado ao provedor **quando** há opt-in + canal habilitado + telefone válido. Minimização: nenhum outro dado do paciente vai ao provedor além do necessário para o template (nome, tipo de serviço, profissional, data/hora — corpo identifica o estabelecimento). |

- **Audit do consentimento**: marcar/desmarcar o opt-in registra 1 linha de **Edição** em `paciente_acesso_log` via `IPacienteAcessoLogService.RegistrarAsync(paciente_id, usuario_id, estabelecimento_id, TipoAcessoPaciente.Edicao)` — reuso do serviço existente, best-effort (falha do audit não bloqueia o salvar). **Sem tabela/serviço de audit novo.**
- **Sem PII em log de envio**: o adapter/handler de WhatsApp loga **apenas o hash SHA-256 truncado do destinatário** — espelho exato de `ResendEmailService`/`NoOpEmailService`. Nunca telefone, nome, e-mail ou corpo da mensagem, em sucesso ou falha. Falha de entrega → `LogWarning` sem PII, não marca enviado, não relança.
- **Sem log de delivery/leitura no MVP**: o MVP não grava tabela de envios/entregas WhatsApp (webhooks de status são fase futura). O único audit do MVP é o do **consentimento** (acima).
- **Interação com anonimização LGPD**: o job `anonimizar-pacientes-inativos` zera `pacientes.telefone`; consentimento sem telefone é inerte (nada é enviado por R2 do briefing). Avaliar resetar o opt-in junto à anonimização — comportamento decidido no PR da entrega, sem schema adicional.
- **Multi-tenant**: o corpo do template **sempre** identifica o estabelecimento de origem (`{{nome_estabelecimento}}`); o envio só ocorre sobre agendamentos já filtrados por `estabelecimento_id`. Nenhum dado de outro tenant transita.

## CNPJ alfanumérico — PII e redação de log (briefing 2026-06-19_002)

CNPJ agora inclui letras (IN RFB 2.229/2024): primeiros 12 positions `[A-Z0-9]`, últimos 2 (DV) numéricos.

- **Regex PII atualizada**: `PiiSanitizer.cs` e `RemovePIIEnricher.cs` usam `[A-Z0-9]{2}\.?[A-Z0-9]{3}\.?[A-Z0-9]{3}/?[A-Z0-9]{4}-?\d{2}` com `RegexOptions.IgnoreCase` — cobre tanto o formato numérico legado quanto o alfanumérico novo.
- **Sem PII em log de CNPJ**: a regra anterior de nunca logar CNPJ em formato de log estruturado se mantém — o campo `Cnpj` (canônico, 14 chars) nunca entra em `_logger.*`.
- **Normalização dedicada**: `CnpjValidator.Normalizar` (backend) e `normalizarCnpj` (frontend) são distintos de `SomenteDigitos`/`apenasDigitos` (que ficam digits-only para CPF/CEP/telefone). Trocar de lugar as funções criaria risco de vazar letras em campos que não as esperam.
- **DV sempre numérico**: posições 13–14 do canônico são `IsAsciiDigit`. Qualquer letra nas posições de DV é rejeitada por `CnpjValidator.EhValido` com `BusinessException("CNPJ inválido.")`.
- **Retrocompatibilidade**: CNPJs 100% numéricos continuam válidos. Nenhuma migração de dado necessária — campo `TEXT` no Postgres aceita os dois formatos.

## Expiração automática de agendamentos — log sem PII (briefing 2026-06-19_001)

O job `ExpirarAgendamentosNaoFinalizadosJob` (diário, 03:00 BRT) aplica o status `Expirado` a agendamentos não finalizados do dia anterior. Regras LGPD:

- **Sem PII em log**: o handler loga apenas `{ estabelecimento_id, quantidade, timestamp }` por estabelecimento. Nunca nome do paciente, `paciente_id`, CPF ou motivo individual.
- **Mensagem de domínio genérica**: `Agendamento.ExpirarPorFimDoDia(motivo)` grava `MotivoCancelamento` com a string fixa `"Não finalizado até o fim do dia."` — nenhum dado do paciente entra no motivo.
- **Sem notificação ao paciente**: R3 do briefing: o job é silencioso, não publica `AgendamentoCanceladoEvent`, não envia e-mail/WhatsApp ao paciente. Expiração é evento operacional, não comunicação ao titular.
- **Cross-tenant controlado**: o job varre todos os estabelecimentos (sem filtro `estabelecimento_id` na query de IDs) porque é uma operação administrativa global. Porém, cada agendamento mantém seu próprio `estabelecimento_id` intocado — não há cross-tenant de dados, apenas cross-tenant de operação de manutenção. O mesmo padrão de `ExpirarAssinaturasPendentesJob` e `LimparAuditAdminJob`.

## Pool de variáveis do prontuário — minimização de dados (briefing 2026-06-05_001)

A tabela `prontuario_variaveis_pool` guarda nomes genéricos de itens clínicos (ex.: "Dipirona", "Hipertensão") — **não é PII de paciente**. Regras de uso:

- **Só o campo `nome` vira item de pool.** A extração automática ao salvar evolução (`PoolExtratorEvolucao`) coleta apenas os campos `nome`/`parentesco` dos arrays mapeados. Campos livres (`observacao`, `dose`, `frequencia`, `motivo`, `ano`, `doencas`, `comentario`) **jamais** viram itens de pool — contêm contexto clínico específico de paciente.
- **Sem audit extra para criação no pool.** A criação automática ocorre dentro do fluxo de `RegistrarEvolucao`, que já audita a escrita no prontuário. Itens de pool são dados de catálogo (não de paciente), portanto não exigem linha adicional em audit table.
- **Dedup canônica é LGPD-segura.** A normalização (trim + lower + sem acento) ocorre em memória antes de criar item; não persiste a forma bruta digitada pelo profissional quando colide com existente.
- **Sem PII em log.** Nenhum campo livre (que pode conter nome/contexto do paciente) transita por `_logger.*`. `PoolExtratorEvolucao` opera em silêncio — falha-suave sem log de dados da evolução.

## PDF probatório de termo de consentimento — briefing 2026-06-10_002 (atualizado em 2026-06-12_002)

Geração server-side do documento probatório do termo de consentimento.

- **Endpoint**: `GET /api/termos/{id}/pdf-gerado` — gera e devolve `application/pdf` com o snapshot da versão aceita + bloco de evidência. Gate: `[RequiresAcao("termos", "emitir")]`.
- **Bloco de evidência por estado (briefing 2026-06-12_002)**: termo `Assinado` por **documento físico** (foto/PDF anexado) exibe o **hash do PDF anexado** (`PdfHash`) como evidência de integridade — **sem** token/IP de aceite público. Termo legado `Assinado` por **link** (histórico) preserva a evidência antiga: **últimos 6 caracteres** do token (`TokenAceite[^6..]`) + **hash SHA-256** do conteúdo (`HashIntegridade`). O token completo **nunca** transita em payload, log ou PDF.
- **Audit**: cada geração bem-sucedida registra 1 linha em `termo_audit_log` via `ITermoAuditLogger` com `{ estabelecimento_id, usuario_id, acao = "termo-pdf-gerado", entidade = "TermoEmitido", entidade_id }`. O audit é **best-effort** — falha não bloqueia o download do PDF.
- **Minimização — nome do arquivo**: `Content-Disposition` usa `termo-{id}.pdf` — sem nome/CPF/dados do paciente.
- **Dados degradados graciosamente**: campos opcionais nulos (`IpAssinatura`/`UserAgentAssinatura` de termos antigos) são omitidos do bloco de evidência sem quebrar o documento (null-check antes de renderizar).
- **Multi-tenant**: query filtra `estabelecimento_id = @EstabelecimentoId`; termo de outro tenant → `BusinessException("Termo não encontrado.")` (mensagem genérica — não vaza existência).

## Termo de consentimento físico-primeiro — remoção do aceite por link (briefing 2026-06-12_002)

O **aceite por link público de termo foi removido** por completo (endpoint anônimo `*/publico/termos/aceite/{token}`, e-mail de link, reenvio, recusa e expiração por link). Implicações LGPD:

- **`termo_emitido_acesso_log` passa a ser tabela legada**: deixa de receber novas escritas (a fonte — visualização/aceite/recusa públicos via token — não existe mais). **Não é dropada** — o histórico de acessos públicos já registrado é dado de auditoria LGPD. O relatório de acessos ao titular (briefing 2026-06-10_007) já a excluía do MVP. Drop é backlog separado.
- **Anexo de documento físico** (foto JPG/PNG convertida em PDF, ou PDF direto): validação por **magic bytes reais** (não por extensão/MIME declarado — iPhone pode enviar HEIC com extensão `.jpg`; HEIC é rejeitado com 422 orientando converter). Nome do arquivo no S3 é gerado por **GUID** (`termos/est_{id}/{termoId}_{guid}.pdf`) — sem nome/CPF do paciente; o nome de origem do usuário **não** é persistido em coluna nem em log. Hash SHA-256 do PDF resultante registrado como evidência de integridade.
- **Audit em dois trilhos no anexo pela evolução**: além de `termo_audit_log` (`termo-pdf-anexado`), o anexo feito **dentro da evolução do prontuário** registra 1 linha de **escrita** em `prontuario_acesso_log` via `IProntuarioAcessoLogService.RegistrarAsync(..., TipoAcessoProntuario.Escrita)` — anexar documento ao prontuário é escrita sensível. Ambos best-effort (falha não bloqueia o anexo).
- **Mensagens genéricas**: todos os 422 de upload (tipo inválido, magic bytes divergentes, HEIC, tamanho) são genéricos, sem PII e sem ecoar o nome do arquivo.

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

## Carteirinha de convênio — dado pessoal (briefing 2026-06-10_016, F6)

`paciente_convenios.numero_carteirinha` é dado pessoal do paciente (número de identificação junto à operadora).

- **Minimização**: DTO de carteirinha retorna apenas os campos exibidos na tela (`convenioNome`, `planoNome`, `numeroCarteirinha`, `validade`, `ativo`). Sem CPF/telefone/dado clínico no payload.
- **Sem PII em log/erro**: mensagens de 422/404 de convênio/carteirinha são genéricas ("Convênio não encontrado.", "Carteirinha não encontrada.") — não revelam número, nome do paciente nem tenant alheio.
- **Audit de acesso (best-effort)**: a aba Convênios do paciente registra 1 linha por carga em `paciente_acesso_log` via `IPacienteAcessoLogService.RegistrarAsync(paciente_id, usuario_id, estabelecimento_id, TipoAcessoPaciente.Leitura)`. Falha do log **não** bloqueia a aba — idêntico ao padrão da aba Financeiro (F2/F8). **Sem tabela/serviço de audit novo.**
- **Guia de autorização** (`guia_numero`/`guia_senha`/`guia_autorizada_em`): colunas na própria tabela `cobrancas`, é metadado operacional de faturamento — **sem PII de paciente**. Mensagens de erro genéricas ("Cobrança não encontrada.", "Guia só disponível para cobranças de convênio.").
- **Alerta de validade vencida**: calculado exclusivamente no frontend a partir do campo `validade` (tipo `string | null` ISO date). O backend **nunca** expõe campo derivado `validadeVencida` — minimização de DTO. Helper `estaVencida(validade)` em `convenioService.ts` é a fonte única para esse cálculo (R6 do briefing). Não bloqueia operação — é puramente informativo.

## Recibo de pagamento em PDF — briefing 2026-06-10_015 (F8)

O recibo de pagamento é um **documento interno** (sem valor fiscal) gerado on-the-fly pelo servidor para um `Pagamento` quitado.

- **Audit de emissão (best-effort)**: cada emissão de recibo bem-sucedida registra 1 linha em `paciente_acesso_log` via `IPacienteAcessoLogService.RegistrarAsync(paciente_id, usuario_id, estabelecimento_id, TipoAcessoPaciente.Leitura)`. Falha do audit **não** bloqueia o download — best-effort idêntico ao padrão da aba Financeiro (F2).
- **Flag de 1ª emissão**: `Pagamento.recibo_emitido_em` (`timestamptz NULL`) — gravado apenas na **primeira** emissão (idempotente; reemissões são gratuitas). Não é dado de paciente: é metadado operacional do próprio `Pagamento`.
- **Conteúdo do PDF — minimização**: o recibo contém apenas **dado financeiro/identificação** necessário para a função de comprovante (estabelecimento, nome do paciente, valor pago, forma de pagamento, parcelas, data, quem registrou, origem da cobrança). **Nunca** contém dado clínico (CID, diagnóstico, procedimento, anamnese) — esses são dados de saúde sensíveis sem vínculo com a quitação financeira.
- **Minimização — nome do arquivo**: `Content-Disposition` usa `recibo-{pagamentoId}.pdf` — **sem nome/CPF/dados do paciente** no nome do arquivo.
- **RBAC**: gate `[RequiresAcao("financeiro_paciente", "ver")]` — mesmo gate da consulta financeira; não exige permissão extra.
- **Pagamento estornado → 422 genérico**: "Pagamento estornado não pode gerar recibo." — regra de negócio no domínio (`Pagamento.RegistrarEmissaoRecibo()`). O domínio é a fonte da verdade; trava de front é UX.
- **Multi-tenant falha-fechada**: query Dapper filtra `estabelecimento_id` via JOIN em `cobrancas`; aggregate `Cobranca` carregado via `ICobrancaRepository.ObterPorIdOuNulo(cobrancaId, estabelecimentoId)` (tenant enforced). Pagamento de outro tenant → "Não encontrado." genérico.
- **Rótulo "RECIBO — documento sem valor fiscal"**: obrigatório em destaque no PDF para evitar confusão com NFS-e.

## Assinaturas/Planos SaaS — nota LGPD (briefing 2026-06-11_003)

Assinatura de plano, features e limites são **dados operacionais do SaaS** (do estabelecimento como cliente do Imedto), **não** dados de saúde do paciente. Não há nova categoria de PII sensível neste módulo.

- **Mensagens genéricas**: erros do enforcement (`AssinaturaInativa`, `FeatureBloqueada`) e do admin não revelam PII nem tenant alheio — os códigos genéricos atuais são mantidos.
- **Sem audit de paciente**: mudanças de plano/estado afetam o estabelecimento, não o paciente — sem nova linha em `paciente_acesso_log`.
- **`referencia_externa` (coluna dormente)**: quando o gateway de pagamento futuro existir, esse campo guardará um ID de gateway — **não é PII de paciente**; é metadado operacional de faturamento do cliente SaaS. Quando o gateway for implementado, a nota de retenção e minimização de dados de cobrança do cliente deverá ser revisada neste documento.

## Export de extrato financeiro — audit best-effort (briefing 2026-06-11_002)

Export `GET /financeiro/extrato/export` retorna CSV com dados financeiros agregados (sem campo clínico, sem CPF, sem dado sensível de paciente além de nome). Regras LGPD:

- **Minimização**: CSV contém data, descrição, categoria, forma de pagamento, valor, status, nome do paciente (texto, sem ID). **Nunca** CPF, diagnóstico, CID, conteúdo de prontuário.
- **Audit de export (best-effort)**: cada export bem-sucedido registra 1 linha em `financeiro_export_log (id, usuario_id, estabelecimento_id, acao, periodo_inicio, periodo_fim, total_linhas, ocorrido_em)`. O registro é feito por `ConsolidacaoFinanceiraQueryRepository.GravarExportAuditAsync(...)` — captura toda exceção silenciosamente (não bloqueia o download). A tabela precisa ser criada pelo `imedto-database` (migration pendente).
- **RBAC**: gate `[RequiresAcao("financeiro", "ver")]` — mesmo gate do extrato paginado.
- **Multi-tenant**: endpoint extrai `estabelecimento_id` do claim (`TenantIdFromClaims`); repositório filtra na query SQL. Sem tenant → `BusinessException("Estabelecimento inválido.")` → 422.
- **Nome do arquivo**: `extrato-financeiro-{dataFim}.csv` — sem PII no nome.
- **Encoding**: UTF-8 com BOM (compatibilidade Excel pt-BR). Decimal com vírgula, separador `;`.

## Central de Migração — LGPD e retenção (briefing 2026-06-15_001 — Marco 1)

### Arquivo ZIP bruto — retenção de 30 dias (CA24, R12)

O arquivo ZIP enviado pelo cliente é armazenado no S3 (`BucketAnexosProntuario`, prefixo `migracao/{estabelecimentoId}/{jobId}/arquivo.zip`) **exclusivamente** para uso pelo time Imedto no processo de mapeamento e importação. Regras:

| Dado | Local | Regra |
|---|---|---|
| ZIP bruto | S3 `migracao/{estab}/{job}/arquivo.zip` | Retenção **30 dias** a partir do upload. Apagado pelo job `expirar-arquivos-migracao` (1×/dia). `ArquivoExpirado=true` fica no staging como auditoria. |
| `migracao_jobs` | Postgres `migracao_jobs` | Mantida indefinidamente (auditável). Não contém PII direta — apenas metadados (origin, status, timestamps). |
| `migracao_registros` | Postgres `migracao_registros` | Contém payload bruto das linhas a importar (potencialmente PII). Retenção: a definir nos marcos seguintes (fase de mapeamento + importação). |

### Amostra mascarada ao mapeador IA (Marco 2 — premissa já fixada)

Quando o Marco 2 implementar `IMapeadorDeMigracao`, o adapter **deve** extrair os cabeçalhos e uma amostra de até 5 linhas, passando os valores **mascarados** via `IAnonimizacaoService` antes de enviar ao `IaService`. O contrato `EsquemaDeArquivo.AmostraMascarada` já reflete isso: `IReadOnlyList<IReadOnlyDictionary<string, string>>` — nenhum valor real de paciente trafega para a IA. Nunca enviar CSV original nem PII real ao provedor de IA.

### Sem PII em logs e mensagens de erro

- `ExpirarArquivosMigracaoJob`: log de falha individual registra apenas `JobId` — nunca `EstabelecimentoId`, nome do arquivo ou conteúdo.
- `IniciarMigracaoCommandHandler`: mensagem de rejeição é genérica ("Falha ao processar o arquivo. Tente novamente.") — sem detalhes do S3 nem nome do arquivo original.
- Endpoint `GET /api/migracao/{jobId}`: job de outro tenant retorna 404 com mensagem genérica (CA2).

### Termo de responsabilidade (R12)

O aceite do termo é registrado em `migracao_jobs.termo_aceito_em` no momento do upload (via `RegistrarArquivoRecebido`). O frontend exige o checkbox antes de habilitar o envio — a API não precisa receber o aceite separadamente (o POST `/api/migracao/upload` implica aceite, que é gravado no aggregate).

### Onda 2 — Prontuário histórico (Marco 5, CA21): audit de escrita

A carga de Onda 2 (`CarregarOnda2JobHandler`) grava evoluções e anexos históricos através dos **handlers de prontuário existentes** (`RegistrarEvolucaoCommand`, `AdicionarAnexoCommand`), que internamente chamam `IProntuarioAcessoLogService.RegistrarAsync(..., TipoAcessoProntuario.Escrita)`. O campo `AutorUsuarioId` é preenchido com o Guid fixo `00000000-0000-0000-0000-000000000001` (constante `CarregarOnda2JobHandler.AutorSistemaId`) — identifica o "usuário-migração" sem PII real, separando linhas de migração das evoluções clínicas normais nas consultas de audit.

Regras LGPD específicas da Onda 2:
- **Motivo de rejeição sem PII (CA4/CA20):** mensagem padronizada "paciente não identificado" — nunca revela CPF/documento tentado.
- **Sem PII em log:** `_logger.LogInformation` registra apenas `JobId` e `EstabelecimentoId` (nunca CPF, nome, conteudo_json do prontuário).
- **Minimização de payload:** `IMigracaoPacienteLookup` retorna apenas `(PacienteId, ProntuarioId)` — nunca dados sensíveis do paciente.
- **CA15 — honestidade estrutural:** prontuário sem campos identificáveis não gera evolução inventada; entra como anexo `text/plain` pesquisável. Isso evita PII falso ou distorcido nos prontuários.

## Alertas clínicos — visibilidade restrita (briefing 2026-06-22_002)

Os **alertas clínicos** do paciente (`pacientes.alertas text[]` — ex.: "alergia a penicilina", "anticoagulado", comorbidades) são **dado pessoal sensível de saúde** (Art. 5º II / Art. 11 LGPD) — equiparável ao CPF: não podem aparecer para qualquer um. O **conteúdo** e qualquer **derivado** (inclusive a contagem) têm visibilidade restrita.

- **Exposto apenas no cabeçalho do prontuário**, no contexto do atendimento. **Removido** de: lista de pacientes (badge + `qtdAlertas` do DTO de listagem), informações básicas do detalhe do paciente, resumo do check-in da agenda e formulário geral do paciente (`PacienteFormModal`, usado pela recepção). Não pode ser exposto em nenhum lugar novo (hover de lista, tooltip de agenda, busca rápida).
- **Quem pode LER** (sempre dentro do tenant do paciente): **Dono** (sempre) **ou** **Profissional que está atendendo** (atendimento ativo atribuído a ele — agendamento com check-in e status não terminal — ou conduzindo a evolução atual) **ou** **Profissional que já atendeu** (evolução passada autorada por ele no prontuário). **Recepção nunca lê.** Profissional sem vínculo de atendimento e não-Dono **não lê**. Sinais técnicos: `ProntuarioEvolucao.AutorUsuarioId` (atendeu) e `Agendamento.ProfissionalUsuarioId`/`CheckInEm`/`Status` (atendendo).
- **Segurança clínica (não-negociável):** o ato de iniciar/conduzir o atendimento atual **conta** como "está atendendo" — médico em **primeira consulta de paciente novo** (sem histórico) **precisa** ver os alertas (ex.: alergia antes de prescrever). O bloqueio recai apenas sobre abrir o prontuário **sem contexto de atendimento e sem histórico, não sendo Dono**.
- **Gestão** (criar/editar/remover) restrita às **mesmas personas** (Dono + atende/atendeu) e feita **dentro do prontuário** — a recepção não cria/edita alertas; a seção saiu do formulário geral do paciente.
- **Enforcement no backend, não só na UI:** a API **não devolve** o conteúdo nem a contagem de alertas a quem não tem direito (chamada direta inclusa). A negativa de leitura é **indistinguível** de "paciente sem alertas" — não revela que existe alerta oculto. Ocultar o bloco no front é UX; a fonte da verdade é o handler/query.
- **Audit reusa o trilho do prontuário** (sem tabela/serviço novo): a leitura do prontuário que exibe alertas já registra `TipoAcessoProntuario.Leitura` via `IProntuarioAcessoLogService`; a **gestão** de alerta (escrita sensível) registra 1 linha de `TipoAcessoProntuario.Escrita` (best-effort — falha não bloqueia a operação).
- **Multi-tenant** rígido (filtro `estabelecimento_id`; paciente de outro tenant → "não encontrado" genérico) e **mensagens genéricas sem PII** (negativa não ecoa conteúdo de alerta, nome ou CPF, e não distingue "não existe" de "não pode ver").

---

## Confidencialidade da evolução e do prontuário — leitura autor-ou-dono (briefing 2026-06-27_001)

Cada **evolução de prontuário** e tudo vinculado a ela (documentos clínicos, anexos, fotos) é visível **apenas pelo profissional que a criou** (`AutorUsuarioId` / `profissional_usuario_id`) **ou pelo Dono** do estabelecimento. Médicos autônomos do mesmo estabelecimento **não compartilham** conteúdo clínico entre si.

**Predicado de visibilidade (R1 — fonte única):**
```
registro_visível ⟺ (papel == Dono) OU (id_autor_do_registro == solicitante.usuarioId)
```

**Superfícies cobertas:**
- Evoluções: timeline gated (`ObterDoPacienteGated`), listagem paginada (`ListarEvolucoesPaginadas`) e contagem (`ContarEvolucoes`).
- Documentos clínicos: lista unificada (`DocumentoQueryRepository.ListarDoPaciente`), lista de receitas (`ReceitaQueryRepository.ListarDoPaciente`), leitura/PDF individual (`ObterReceitaQuery`, `ObterAtestadoQuery`, `ObterPedidoExameQuery`).
- Anexos: listagem (`ListarAnexosDoProntuario`), URL individual (`ObterUrlAnexo`) e lote (`ObterUrlsAnexos`). Regra COALESCE de autoria: anexo com evolução → usa `autor_usuario_id` da evolução; anexo órfão (`evolucao_id IS NULL`) → usa `criado_por_usuario_id` do próprio anexo; Dono sempre passa.

**Falha-fechada (R2):** claim ausente, papel não-reconhecido como Dono nem como autor → retorna vazio (lista) ou "não encontrado" genérico (registro individual). Nunca "abre" por falta de informação.

**Mensagem genérica (R5):** acesso negado retorna "não encontrado" (não "sem permissão"). Não vaza a existência do registro nem o nome do colega autor. PII fora de mensagem de erro.

**Contagem coerente (R6):** `ContarEvolucoes` aplica o mesmo predicado que a listagem — senão paginação/contador denuncia a existência de registros de colegas.

**Distinção vs alertas clínicos:** os alertas clínicos têm regra própria **mais ampla** (Dono + profissional que atende/atendeu, inclusive primeira consulta) por razão de segurança do paciente. O gating de evolução é **mais restrito** (só autor ou Dono) por sigilo entre médicos autônomos. As duas regras coexistem sem conflito — não tocar em `ObterDoPacienteGated` no que se refere a `VerificarVinculoAtendimento` / `PodeGerirAlertas`.

**Audit sem PII (R8):** `prontuario_acesso_log` registra `{usuario_id, prontuario_id, estabelecimento_id, acao, timestamp}` — sem nome do colega autor, sem conteúdo clínico. Best-effort: falha de audit não bloqueia o acesso legítimo.

**Multi-tenant:** filtro `estabelecimento_id` precede o gating por autor. Dono de um tenant nunca acessa dados de outro tenant.

---

## Responsável legal do paciente — PII de terceiro (briefing 2026-06-23_002)

Pacientes menores de 18 anos precisam de um **responsável legal** cadastrado. Os dados do responsável (`responsavelNome`, `responsavelParentesco`, `responsavelTelefone`) são **dados pessoais de terceiro** (Art. 5º, I LGPD) — não dados do próprio titular.

**Minimização obrigatória:**
- Os 3 campos (`responsavelNome`, `responsavelParentesco`, `responsavelTelefone`) aparecem **apenas no `PacienteDto`** (endpoint de detalhe `/api/paciente/{id}`) e na **exportação LGPD** (`PacienteExportPessoalDto`, Art. 18).
- **Não** aparecem em `PacienteListaItemDto` (listagem paginada), `PacienteBuscaRapidaDto` (autocomplete) nem em qualquer DTO de agendamento — minimização: a recepção não precisa ver o responsável ao agendar.
- O campo `PacienteFaixaEtaria` (`"menor" | "idoso" | null`) nos DTOs de agendamento é **derivado** da data de nascimento via SQL — expõe apenas a classificação necessária para UX, sem expor a data completa nem os dados do responsável.

**Validação obrigatória no backend (invariante do aggregate):**
- `Paciente.Cadastrar()` e `Paciente.AtualizarDados()` lançam `BusinessException` (422) se `dataNascimento` < 18 anos E `responsavelNome` ou `responsavelParentesco` estão ausentes.
- Frontend bloqueia o "Salvar" com a mesma regra (UX) — mas o backend é a fonte de verdade.
- Em modo de cadastro rápido (formulário simplificado), ao detectar menor: expande automaticamente para o formulário completo com a seção de Responsável — sem bloquear com alerta.

**Anonimização:**
- `Paciente.Anonimizar()` limpa `responsavelNome`, `responsavelParentesco` e `responsavelTelefone` junto com os demais dados PII do titular — nenhum dado de terceiro permanece após anonimização.

**Audit trail:**
- As operações de criação e edição de paciente (inclusive os campos do responsável) já geram `TipoAcessoPaciente.Edicao` via `IPacienteAcessoLogService` — sem audit trail separado.

**Exportação Art. 18:**
- `PacienteExportPessoalDto` inclui os 3 campos do responsável em `PacienteExportPessoalDto` (subcampo `Pessoal`), assegurando a portabilidade completa dos dados ao titular (ou ao responsável legal, quando o titular for menor).

---

## Checklist multi-tenant — premissa não-negociável

Antes de cada commit que toca dados de domínio (paciente, agendamento, prontuário, financeiro, equipe, estoque, orçamento, **assinatura digital**), valide:

1. **Filtro por `estabelecimento_id`** em todo `WHERE`/join de domínio.
2. **Verificação de vínculo** do usuário com o estabelecimento (papel + escopo do vínculo).
3. **Mensagem genérica em erro** ("não encontrado") — nunca revelar se o registro existe em outro tenant.
4. **Repositório falha-fechada**: ausência de tenant claim → retorna vazio/throws, nunca query global.
