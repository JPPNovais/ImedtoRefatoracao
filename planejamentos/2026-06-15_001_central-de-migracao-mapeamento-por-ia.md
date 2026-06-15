# Central de Migração com mapeamento de schema por IA (MVP)

**ID**: 2026-06-15_001
**Status**: Aprovado por usuário em 2026-06-15
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G (épico — recomenda-se fatiar a execução por ondas; ver §9)
**Áreas regressivas tocadas**: permissionamento (admin), prontuário, estoque, orçamento, paciente, agenda, relatório

> Materializa a FASE 2B (Central de Migração) evoluindo a estratégia de "adaptadores determinísticos por origem" para **mapeamento de schema assistido por IA**. Fonte de decisão: `Docs/Discoverys/migracao-dados-ia/01_discovery.md` (D1–D14, §10).

## 1. Contexto e motivação

Cliente novo, vindo de outro sistema (iClinic, Feegow, Clinicorp, Ninsaúde, planilha solta) precisa trazer os dados dele para o Imedto com o menor atrito possível. Cada sistema exporta num formato próprio (CSV, XLSX, JSON), com nomes de coluna e estruturas diferentes. Escrever um parser dedicado por origem não escala.

**Ideia central (D1):** usar IA **uma vez por arquivo, sobre metadados** (cabeçalhos + amostra mascarada), para inferir o de-para coluna→campo canônico. A IA **nunca transforma dado registro a registro** — ela só diz "para onde a coluna vai". A carga é determinística, roda pelos commands de domínio existentes, e registro sem campo obrigatório é **rejeitado com motivo**, nunca adivinhado.

Dor de mercado atacada: portabilidade quebrada + lock-in (diferencial D1 da FASE 2B). Benefício de negócio: trial que importa dados converte mais.

## 2. Persona-alvo

- **Cliente (dono/admin do estabelecimento)**: faz upload de um ZIP com seus dados na Configuração do Estabelecimento. Momento da jornada: onboarding / migração de sistema. Frequência: 1 a poucas vezes na vida do cliente.
- **Operador Imedto (admin da plataforma)**: revisa o mapa proposto pela IA no painel admin, confere o preview, dispara a importação com 1 clique, lê o relatório e, se preciso, desfaz. Frequência: recorrente (toda nova migração).

## 3. Escopo

**Inclui (MVP):**
- Upload de **um ZIP** (≤ 50MB) pela Configuração do Estabelecimento; job nasce vinculado ao `estabelecimento_id` logado.
- Descompactação + separação dos arquivos por entidade (CSV / XLSX / JSON).
- Inferência de mapa de schema via porta `IMapeadorDeMigracao` (amostra mascarada à IA).
- Painel admin: fila de migrações, revisão/edição do mapa (com confiança e dúvidas), preview de importados/rejeitados/duplicados, disparo em 1 clique, relatório, desfazer.
- Carga assíncrona em lotes via `JobScheduler`, reusando os **commands de domínio existentes** (criar E atualizar) — upsert por chave de negócio.
- **Onda 1**: pacientes, agendas, cadastros de estoque (fornecedor/categoria/fabricante/local), itens de estoque, produtos de orçamento, procedimentos de orçamento.
- **Onda 2**: prontuário, vinculado por CPF/documento aos pacientes da Onda 1.
- **Template de mapa por origem (D13)**: operador salva o de-para aprovado como template nomeado por origem; próximo job da mesma origem pré-carrega o template (operador ainda revisa).
- Audit trail completo da migração; arquivo bruto criptografado no S3 com retenção 30 dias.

**Não inclui (registrar como backlog / dependência):**
- Self-service de ponta a ponta (disparo continua sendo do operador admin). A porta + staging devem servir a um wizard self-service futuro **sem retrabalho** (premissa de design, não entrega).
- Formato XML (fica para depois; MVP é CSV/XLSX/JSON — D8).
- Detecção automática de origem por fingerprint (operador escolhe/confirma a origem; o template por origem é manual).
- DPA jurídico com o provider de IA (dependência externa de jurídico — não bloqueia o código, mas o go-live de produção depende dele).
- Reverter (snapshot "antes") de registros **atualizados** — desfazer só reverte os **criados** (D12).
- Importação de identificadores técnicos da origem (proibido por D11 — ver R1).

## 4. Regras de negócio

- **R1 (D11 — princípio das chaves)**: O Imedto **gera suas próprias PKs** no padrão dele. É **proibido** importar, gravar ou usar como chave o identificador técnico do sistema de origem (ex.: "id do agendamento no iClinic"). Mora em: pipeline de carga (Application/Infrastructure de migração). Validada em: back (a pipeline nunca persiste id externo; dedupe só por chave de negócio).

- **R2 (dedupe por chave de negócio + nunca duplicar — D7/D11)**: Upsert sempre dentro do tenant (`estabelecimento_id`). Chave existe → atualiza (chama command de atualização). Não existe → cria (chama command de criação). Registro **sem chave que garanta unicidade → rejeitado com motivo**, nunca duplicado. Chaves por entidade (tabela canônica §10 do discovery):
  - **Paciente**: 1º CPF ou documento internacional → 2º fallback nome + telefone (ambos obrigatórios, combinação única) → senão **log + pula** (não bloqueia o job).
  - **Fornecedor de estoque**: CNPJ → fallback Nome único no tenant → senão rejeita.
  - **Item de estoque**: `Codigo` → fallback Nome único no tenant → senão rejeita.
  - **Categoria / Fabricante / Local de estoque**: Nome único no tenant → senão rejeita.
  - **Produto / Procedimento de orçamento**: Código → fallback Nome único do catálogo → senão rejeita.
  - **Agenda**: paciente + profissional + data/hora → senão rejeita.
  - **Prontuário**: vínculo resolvido por paciente via CPF/documento (R8).
  Mora em: resolvedor de existência da pipeline de carga (por entidade). Validada em: back.

- **R3 (carga só pelos commands de domínio — D5)**: A importação **nunca** faz INSERT direto. Cada linha vira chamada a um command existente (`CriarPacienteCommand`, command de atualização correspondente, etc.). "Obrigatório" é o que o `BusinessException` do command já define — não há lista paralela a manter. Mora em: pipeline de carga. Validada em: back (o próprio domínio rejeita; a pipeline captura o `BusinessException` e marca a linha como rejeitada com o motivo).

- **R4 (envio parcial — só migra o que veio)**: Entidade não enviada não migra e não gera erro. FK opcional ausente (ex.: `ItemInventario.FornecedorPadraoId`, `FabricanteId`, `LocalPadraoId`) → insere sem ela. Campo/FK obrigatório ausente (ex.: `ItemInventario.CategoriaId`, agenda sem paciente correspondente) → rejeita com motivo. Mora em: pipeline de carga. Validada em: back.

- **R5 (ordem de FK)**: Cadastros-base (fornecedor, categoria, fabricante, local, catálogos) entram **antes** dos itens que dependem deles; paciente entra **antes** de agenda e prontuário. Job de prontuário (Onda 2) sem pacientes confirmados fica **bloqueado**. Mora em: orquestração da pipeline. Validada em: back.

- **R6 (IA só sobre amostra mascarada — D1/D2)**: A porta `IMapeadorDeMigracao` recebe **apenas** cabeçalhos + amostra de N linhas com PII mascarada (reusar `IAnonimizacaoService`). A IA devolve `{ col_origem → campo_canônico, confiança, dúvidas }` — nunca o volume de dados, nunca o dado real. 1 chamada por arquivo. Mora em: porta na Application/Domain; adapter na Infrastructure usando `IaService` existente. Validada em: back (domínio não conhece prompt nem provider).

- **R7 (humano no loop antes de gravar)**: Nada é gravado em tabela de domínio antes de o operador revisar o mapa e disparar. O mapa proposto pela IA é **editável** no painel; confiança e dúvidas ficam visíveis. Mora em: painel admin + estado do job. Validada em: back + front.

- **R8 (prontuário — vínculo e honestidade de estrutura — D9)**: Prontuário casa com o paciente por **CPF ou documento internacional**. Sem identificador que resolva o vínculo → **rejeita com motivo** ("paciente não identificado"); nunca cria paciente a partir do prontuário. Origem com campos identificáveis → evolução estruturada via mapa. Origem sem estrutura (PDF/relatório corrido) → entra como **anexo/documento histórico pesquisável** no paciente, não como evolução estruturada falsa. Receita controlada/ANVISA: combinação inválida (tipo+notificação) **bloqueia** o registro — a IA nunca adivinha o tipo. Mora em: pipeline de carga da Onda 2. Validada em: back.

- **R9 (desfazer — D12)**: O desfazer reverte **somente os registros criados** pelo job (marcados com `migracao_job_id`). Registros **atualizados** (já existiam) **não são tocados**; o relatório de desfazer avisa explicitamente quantos atualizados não foram revertidos. Mora em: command de desfazer. Validada em: back + front (aviso).

- **R10 (template de mapa por origem — D13)**: Ao aprovar um mapa, o operador pode **salvá-lo como template** nomeado por origem (escopo: plataforma, reutilizável entre tenants — é metadado de schema, não dado de paciente). Próximo job cuja origem case com um template existente **pré-carrega** o de-para; o operador **ainda revisa e pode editar** antes de disparar. Mora em: tabela de templates + UI admin. Validada em: back + front.

- **R11 (limite de upload — D14)**: ZIP **≤ 50MB** por job. Acima, o upload é **rejeitado com mensagem clara** ("Arquivo acima de 50MB. Divida a migração em partes ou contate o suporte para migração assistida."). Mora em: validação do endpoint de upload (back) + checagem no front antes do envio. Validada em: back + front.

- **R12 (LGPD do arquivo bruto)**: ZIP bruto criptografado no S3, **retenção 30 dias**, apagado por job ao expirar. Termo de responsabilidade do cliente sobre a licitude dos dados enviados, aceito no upload. Mora em: provider de storage + job de expiração + tela de upload. Validada em: back + front.

## 5. Modelo de dados

Staging multi-tenant (herda da FASE 2B). Schema final é responsabilidade do `imedto-database`; o desenho-alvo:

- **`migracao_jobs`** — `id` (PK Imedto), `estabelecimento_id` (multi-tenant, NOT NULL), `origem` (texto livre / template), `status` (enum: `aguardando_arquivo`, `aguardando_mapa`, `mapa_em_revisao`, `preview_pronto`, `migrando`, `concluido`, `concluido_com_erros`, `desfeito`, `rejeitado`), `arquivo_s3_key`, `arquivo_expira_em` (criado_em + 30d), `termo_aceito_em`, `template_origem_id` (FK nullable para `migracao_templates`), `criado_por_usuario_id`, `disparado_por_usuario_id` (operador admin), `criado_em`, `atualizado_em`. Índice: `(estabelecimento_id, status)`, `(arquivo_expira_em)` para o job de expiração.
- **`migracao_registros`** — `id`, `migracao_job_id` (FK), `estabelecimento_id`, `entidade` (paciente/agenda/item_estoque/...), `payload_bruto` (jsonb), `status` (`pendente`, `importado_criado`, `importado_atualizado`, `rejeitado`, `pulado`), `motivo_rejeicao` (texto genérico, **sem PII**), `entidade_alvo_id` (PK gerada no domínio quando importado — permite o desfazer), `criado_em`. Índice: `(migracao_job_id, status)`, `(estabelecimento_id, entidade)`.
- **`migracao_mapas`** — de-para revisado por arquivo/entidade do job: `id`, `migracao_job_id`, `entidade`, `mapa_json` (col_origem→campo_canônico + confiança + dúvidas), `revisado_por_usuario_id`, `criado_em`.
- **`migracao_templates`** (D13/R10) — `id`, `origem` (nome único — "iClinic", "Clinicorp"), `entidade`, `mapa_json`, `criado_por_usuario_id`, `criado_em`, `atualizado_em`. **Escopo plataforma** (metadado de schema, não PII), sem `estabelecimento_id`. Índice único `(origem, entidade)`.

**Vínculo multi-tenant**: `migracao_jobs`/`migracao_registros`/`migracao_mapas` carregam `estabelecimento_id`; a importação só grava no tenant destino. `migracao_templates` é cross-tenant por ser schema, não dado.

**Audit / LGPD**: `motivo_rejeicao` e logs nunca contêm PII (mensagem genérica por categoria: "CPF ausente", "data inválida", "paciente não identificado"). `payload_bruto` é dado do tenant, fica no staging multi-tenant e é apagado junto com o job na expiração. Acesso a prontuário durante a Onda 2 gera linha de audit (reusar trilha de acesso a prontuário existente).

## 6. UX e fluxo

**Lado do cliente — Configuração do Estabelecimento:**
- Seção "Migrar meus dados" → upload de um ZIP. Estados: vazio (instrução + limite 50MB), enviando (progresso), enviado (status do job), erro (>50MB, formato inválido). Aceite do termo de responsabilidade (checkbox obrigatório) antes de habilitar o envio. Componentes do design system: `AppPageHeader`, `AppButton`, `AppEmptyState`, área de upload reusando o padrão de anexo existente.

**Lado do admin — `modules/admin`:**
1. **Fila de migrações** — lista paginada de jobs por estabelecimento de origem + status. Reusar `AppPagination`, padrão de lista admin. Estados: loading, vazio (`AppEmptyState`), com dados.
2. **Revisão do mapa** — de-para por arquivo/entidade, **confiança e dúvidas destacadas**, editável. Se houver template da origem, pré-carregado com aviso "pré-preenchido pelo template X — revise". Botão "Salvar como template desta origem".
3. **Preview** — contadores: "1.243 pacientes, 3.481 agendamentos, 512 itens; **37 rejeitados** (motivo por linha); 8 itens sem fornecedor". Lista de rejeitados com motivo, antes do disparo.
4. **Disparo (1 clique)** — botão "Migrar" → carga assíncrona em lotes, barra de progresso ao vivo, respeitando ordem de FK.
5. **Relatório** — importados (criados / atualizados) / rejeitados (com motivo por registro) / pulados / duplicados; botão **Desfazer** com aviso de R9.

**Estados do job (auditados a cada transição):** `aguardando_arquivo → aguardando_mapa → mapa_em_revisao → preview_pronto → migrando → concluido | concluido_com_erros → desfeito`. Caminho de erro: `rejeitado` (ex.: ZIP > 50MB).

Mobile-ready: o lado cliente (upload) é responsivo; o painel admin segue o padrão admin existente (desktop-first é aceitável para operador interno).

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — Onda 1)**: Dado um ZIP válido (≤50MB) com `pacientes.csv` de 1.000 linhas únicas por CPF, Quando o operador revisa o mapa e clica "Migrar", Então 1.000 pacientes são criados no tenant via `CriarPacienteCommand` e o relatório mostra "1.000 importados (criados), 0 rejeitados".

- **CA2 (multi-tenant — staging)**: Dado um operador/usuário do estabelecimento B, Quando tenta acessar um `migracao_job` do estabelecimento A, Então recebe 404 genérico e nada é logado com PII. (Templates de origem são cross-tenant e não contêm PII — não se aplica o isolamento aqui.)

- **CA3 (RBAC — disparo é do admin)**: Dado um usuário comum do estabelecimento (não-operador da plataforma), Quando tenta acessar a fila de migrações ou disparar uma importação no painel admin, Então recebe 403 e a entrada de menu/botão fica oculta no front. O upload do ZIP na Config do Estabelecimento é permitido a dono/admin do tenant; o disparo só ao operador admin da plataforma.

- **CA4 (LGPD — mensagem genérica)**: Dado um paciente rejeitado por falta de CPF e de nome+telefone, Quando o relatório é gerado, Então o `motivo_rejeicao` é genérico ("identificador ausente") e **não contém** o nome, CPF ou telefone do registro; nenhum log de erro carrega PII.

- **CA5 (LGPD — amostra mascarada à IA)**: Dado um arquivo com coluna de CPF e nome, Quando a inferência de mapa é disparada, Então a porta `IMapeadorDeMigracao` recebe apenas cabeçalhos + amostra de N linhas com PII mascarada (via `IAnonimizacaoService`), e o volume real nunca é enviado à LLM (verificável no payload da chamada ao provider).

- **CA6 (proibição de id externo — R1/D11)**: Dado um arquivo de agendas que contém uma coluna "id_agendamento_origem", Quando a importação roda, Então esse id **não** é gravado em nenhuma coluna do domínio nem usado como chave; o dedupe da agenda usa paciente + profissional + data/hora.

- **CA7 (upsert — não duplica em tenant que já tem dados — R2/D7)**: Dado um tenant que já tem o paciente com CPF X, Quando o ZIP traz outro registro com o mesmo CPF X, Então o paciente é **atualizado** (não duplicado) via command de atualização e o relatório conta "1 atualizado", não "2 importados".

- **CA8 (rejeita quando não há chave única — R2)**: Dado um item de estoque sem `Codigo` e cujo Nome já existe duplicado no tenant (Nome não garante unicidade), Quando a importação roda, Então o item é **rejeitado com motivo** ("sem chave única para dedupe") e nada é duplicado.

- **CA9 (paciente — fallback nome+telefone e pulo — R2/D10)**: Dado um paciente sem CPF mas com nome+telefone completos e únicos, Quando importa, Então entra como pré-cadastro (upsert por nome+telefone). E dado um paciente sem CPF e sem nome+telefone completos, Quando importa, Então é **pulado com log** ("identificador ausente"), o job **não falha**, e o relatório mostra "N pacientes pulados por falta de identificador".

- **CA10 (envio parcial — R4)**: Dado um ZIP só com `pacientes.csv` (sem fornecedores nem itens), Quando importa, Então só pacientes são migrados, sem erro, e as demais entidades são ignoradas silenciosamente no relatório.

- **CA11 (FK opcional ausente vs. obrigatória — R4)**: Dado um item de estoque com `CategoriaId` (obrigatório) ausente, Quando importa, Então é **rejeitado com motivo**. E dado um item com `FornecedorPadraoId` (opcional) ausente mas categoria presente, Quando importa, Então é **criado sem fornecedor**.

- **CA12 (ordem de FK — R5)**: Dado um ZIP com fornecedores e itens de estoque que referenciam esses fornecedores, Quando importa, Então os fornecedores são criados **antes** dos itens, e os itens resolvem a FK para o fornecedor recém-criado (nenhum item falha por "fornecedor inexistente" quando o fornecedor veio no mesmo ZIP).

- **CA13 (prontuário bloqueado sem Onda 1 — R5/R8)**: Dado um job de prontuário (Onda 2) cujo tenant ainda não teve pacientes migrados/confirmados, Quando o operador tenta dispará-lo, Então o job fica **bloqueado** com mensagem clara, sem gravar nada.

- **CA14 (prontuário — vínculo por CPF e rejeição — R8)**: Dado um registro de prontuário com CPF que casa com um paciente já migrado, Quando importa, Então a evolução/anexo é vinculada a esse paciente. E dado um prontuário sem CPF/documento que resolva o vínculo, Quando importa, Então é **rejeitado com motivo** ("paciente não identificado") e **nenhum paciente novo é criado**.

- **CA15 (prontuário sem estrutura vira anexo — R8)**: Dado um prontuário de origem sem campos identificáveis (PDF/relatório corrido), Quando importa, Então entra como **anexo/documento histórico pesquisável** no paciente, **não** como evolução estruturada inventada.

- **CA16 (receita controlada bloqueia — R8)**: Dado um registro de receita cuja combinação tipo+notificação (A/B/C/Especial) é inválida segundo o command de domínio, Quando importa, Então o `BusinessException` é capturado e o registro é **rejeitado com motivo**, sem a IA adivinhar o tipo.

- **CA17 (desfazer só os criados — R9/D12)**: Dado um job concluído que **criou** 800 pacientes e **atualizou** 200, Quando o operador clica "Desfazer", Então os 800 criados são revertidos (marcados por `migracao_job_id`), os 200 atualizados **permanecem intactos**, e o relatório avisa "200 registros atualizados não foram revertidos".

- **CA18 (template por origem — R10/D13)**: Dado um operador que aprovou e salvou um mapa como template "iClinic", Quando um novo job de origem "iClinic" entra em revisão, Então o de-para vem **pré-carregado** do template com aviso "pré-preenchido — revise", e o operador **ainda pode editar** antes de disparar.

- **CA19 (limite 50MB — R11/D14)**: Dado um ZIP de 60MB, Quando o cliente tenta enviar, Então o upload é **rejeitado** com mensagem clara sobre o limite de 50MB, tanto no front (antes do envio) quanto no back (422 se burlar o front), e nenhum job é criado.

- **CA20 (estados do job + audit)**: Dado um job percorrendo `aguardando_mapa → mapa_em_revisao → preview_pronto → migrando → concluido`, Quando cada transição ocorre, Então uma linha de audit é registrada com {usuario_id, estabelecimento_id, job_id, status_anterior, status_novo, timestamp}, sem PII.

- **CA21 (audit de acesso a prontuário — Onda 2)**: Dado o disparo de uma migração de prontuário, Quando os registros são gravados nos pacientes, Então cada acesso/escrita de prontuário gera linha na trilha de audit de prontuário existente com {usuario_id, paciente_id, estabelecimento_id, timestamp}.

- **CA22 (performance — carga assíncrona em lotes)**: Dado um ZIP com 5.000 registros, Quando o operador dispara "Migrar", Então a carga roda **assíncrona** via `JobScheduler` em lotes (nunca síncrona no request), com barra de progresso ao vivo, e o request de disparo retorna imediatamente sem timeout.

- **CA23 (IA — 1 chamada por arquivo — R6)**: Dado um ZIP com 3 arquivos de entidades diferentes, Quando a inferência de mapa roda, Então há no máximo **1 chamada à IA por arquivo** (3 no total), não 1 por linha; a carga das linhas é determinística e sem IA.

- **CA24 (retenção do arquivo bruto — R12)**: Dado um job criado, Quando 30 dias se passam, Então o ZIP bruto no S3 é **apagado** pelo job de expiração e `arquivo_s3_key` é marcado como expirado; o staging do job permanece auditável (sem o bruto).

## 8. Riscos e dependências

- **IA erra o de-para** → humano no loop revisa o mapa antes de qualquer gravação (R7); confiança + dúvidas destacadas.
- **Header genérico/ambíguo** (`campo1`) → amostra mascarada ajuda; operador corrige; template salva o ajuste para reuso (R10).
- **Volume grande trava** → carga 100% assíncrona em lotes (CA22); IA só vê amostra (CA23).
- **Alucinação em dado clínico** → IA não escreve dado, só mapeia coluna; obrigatório ausente = rejeita (R3/R8).
- **PII vaza para a LLM** → só amostra mascarada (R6/CA5); **DPA com provider de IA é dependência externa de jurídico** para go-live de produção.
- **Reimportar duplica** → idempotência por chave de negócio (R2).
- **Áreas regressivas**: a carga usa os commands reais de paciente/agenda/estoque/orçamento/prontuário — qualquer regressão nesses commands afeta a migração. QA deve validar que a importação respeita as mesmas validações da criação manual.
- **Dependência de design**: a porta `IMapeadorDeMigracao` + staging devem servir a um wizard self-service futuro sem retrabalho (FASE 2B §5) — premissa de arquitetura a preservar.

## 9. Observações para execução

- **Reuso obrigatório (não construir do zero)**: `JobScheduler` (background + advisory lock), `IaService`/`RateLimitedIaService` (provider de IA atrás de porta), provider S3 (storage), área admin (`Controllers/Admin/*` + `frontend/src/modules/admin`), `IAnonimizacaoService` (mascaramento da amostra), commands de domínio existentes (carga). **Não** há Lambda nova, SDK de IA novo nem fila nova.
- **Porta nova (não-negociável)**: `IMapeadorDeMigracao` definida na Application/Domain; adapter concreto na Infrastructure usa `IaService`. Domínio não conhece prompt nem provider (ports & adapters).
- **Carga só por command (não-negociável)**: nunca INSERT direto. A pipeline resolve a existência por chave (R2) e decide criar vs. atualizar; "obrigatório" é o `BusinessException` do domínio.
- **Fatiamento sugerido da execução** (este briefing é um épico G — recomenda-se quebrar em PRs por marco, todos sob este mesmo ID):
  1. Schema staging (`migracao_*`) + porta `IMapeadorDeMigracao` + upload/termo/limite (CA19, CA24).
  2. Inferência de mapa + painel de revisão (CA5, CA7-base, CA18, CA23).
  3. Carga Onda 1 (pacientes, estoque, orçamento, agenda) com upsert + ordem de FK + preview/relatório (CA1, CA6-CA12, CA20, CA22).
  4. Desfazer (CA17).
  5. Onda 2 prontuário (CA13-CA16, CA21).
- **Liberdade técnica**: estrutura interna do `payload_bruto`, formato do `mapa_json`, granularidade dos lotes, e a estratégia de parsing por formato (CSV/XLSX/JSON) ficam a critério do dev/db, desde que respeitem os CAs.

## 10. Atualização de documentação

- **`Docs/Roadmap/FASE_2B_CENTRAL_DE_MIGRACAO.md`** — registrar a evolução "adaptadores determinísticos por origem → mapeamento de schema por IA" como estratégia adotada para o MVP, apontando para o discovery e para este briefing. Atualização incremental (nova seção / nota no topo), sem reescrever a pesquisa de mercado existente (que continua válida como contexto). **Feito nesta entrega.**
- **`Docs/Discoverys/migracao-dados-ia/01_discovery.md`** — já atualizado com D1–D14 (não é entrega deste briefing; é a fonte).
- **`Docs/ARQUITETURA.md`** — a ser atualizado pelo `imedto-developer` no marco que introduzir a porta `IMapeadorDeMigracao` (novo padrão de porta de mapeamento) e o bounded context de migração, conforme a documentação viva. Sinalizado aqui como dependência de doc do dev, não do BA.
- **`Docs/LGPD.md`** — a ser atualizado quando a retenção de 30 dias do arquivo bruto + amostra mascarada à IA forem implementadas (nova regra de retenção + novo fluxo que toca PII). Sinalizado como dependência; o `imedto-business-analyst` atualiza junto do marco 1 se o dev pedir, ou o dev atualiza no PR.
