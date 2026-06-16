# Central de Migração — Observabilidade no admin: timeline, progresso ao vivo, filtros e ordenação (addendum 3)

**ID**: 2026-06-15_004
**Refere-se a**: 2026-06-15_001_central-de-migracao-mapeamento-por-ia.md (e aos addendums 2026-06-15_002 e 2026-06-15_003)
**Status**: Aprovado por usuário em 2026-06-15
**Autor**: imedto-business-analyst
**Estimativa de esforço**: G (UI de detalhe + lista + polling + 1 endpoint de progresso + 1 endpoint de timeline + filtros backend + 1 migration de índice/coluna leve)
**Áreas regressivas tocadas**: nenhuma de domínio (paciente/estoque/orçamento/prontuário intactos) — toca **só** o bounded context de migração (UI admin de fila + detalhe, query de listagem, leitura de progresso/relatório). Migration leve de schema (índice para filtro de período; +1 coluna opcional de timestamp de início de carga — ver §6).

> O épico Central de Migração (2026-06-15_001, CA1–CA24), o addendum 002 (falha visível + motivos no relatório, CA25–CA39) e o addendum 003 (gate de aprovação por IA, CA40–CA50) estão entregues em `main`. Este addendum **melhora a observabilidade e o controle do operador** na fila e no detalhe do job. O briefing original e os addendums anteriores permanecem intocados e seus CAs continuam válidos. Os CAs deste addendum começam em **CA51** (o addendum 003 terminou em CA50).

---

## 1. Contexto e motivação

O operador Imedto opera as migrações pela fila (`MigracoesListView`) e pelo detalhe (`MigracaoRevisaoView`), mas **enxerga pouco do andamento**:

- **Não há linha do tempo dos passos.** O operador vê o `status` atual como um rótulo solto, mas não vê *onde* o job está na máquina de estados nem *o que falta* para terminar. Cada estado (upload → aguardando aprovação → aprovado → inferindo → mapa em revisão → migrando → concluído/falhou) é um passo, mas a UI não os apresenta como progressão.
- **Não há histórico de "quem fez o quê, quando".** O operador não tem como ver quando o job foi aprovado, por quem, quando a inferência terminou, quando a carga começou — informação útil para suporte e auditoria operacional.
- **A carga é uma caixa-preta.** Durante `migrando`, o detalhe mostra só uma mensagem genérica de "carga em background" + um botão "Atualizar status" **manual**. O operador não vê o progresso subir (quantos pacientes já entraram, quantos faltam), e tem que clicar repetidamente para descobrir se terminou.
- **A fila tem só filtro de status.** Com múltiplos clientes e múltiplos uploads, o operador não consegue achar rapidamente os jobs de um estabelecimento, de uma origem, de uma onda, ou de um período.

**Benefício de negócio:** migração é o onboarding do cliente novo — o operador precisa conduzir várias migrações em paralelo com confiança. Observabilidade boa = operador resolve sozinho, sem abrir o banco, sem ficar recarregando a tela, e acha o job certo na hora. Ataca o atrito de portabilidade que é o diferencial da FASE 2B.

### Nota de premissa — "o audit das transições já existe" (CORREÇÃO IMPORTANTE)

A demanda partiu da premissa de que "já existe audit dessas transições (CA20/CA32/CA49); basta expor como timeline". **Isso não é verdade no código atual.** Investigação ([MigracaoJob.cs](../backend/src/Services/Imedto.Backend.Domain/Migracao/MigracaoJob.cs)):

- O aggregate `MigracaoJob` **não emite eventos de domínio** nem mantém lista de transições. Cada método de transição apenas muta `Status` + `AtualizadoEm`.
- **Não existe** tabela `migracao_job_transicoes` / `migracao_eventos` / audit persistido e consultável das transições. O comentário "Status auditado a cada transição (CA20)" no código é **aspiracional** — descreve a intenção dos CAs, não uma implementação existente.
- O único dado de tempo persistido por job é: `criado_em`, `atualizado_em` (sobrescrito a cada transição — não guarda histórico), `motivo_falha` e `status_antes_falha` (só quando falhou).

**Consequência (decisão D-T2 abaixo):** a "timeline de passos" (onde o job está / o que falta) é **derivável** do `status` atual + a máquina de estados conhecida, sem backend novo. Já o "log de eventos com quem/quando" (histórico granular com ator e timestamp por transição) **exige persistir os eventos** — não dá para reconstruir o passado de um job já existente. Este addendum decide persistir as transições **a partir de agora** (forward-only), em tabela leve, escrita pelos pontos de transição já existentes. Jobs antigos mostram o que for derivável (sem histórico retroativo).

## 2. Persona-alvo

- **Operador Imedto (admin da plataforma)**, no painel `modules/admin`, conduzindo migrações. Ganha: a timeline de passos (sabe onde está e o que falta), o log de eventos (quem aprovou/disparou e quando), o progresso ao vivo durante a carga (vê os números subirem sem recarregar), e filtros na fila (acha o job certo). Frequência: recorrente, toda migração.

## 3. Escopo

**Inclui:**

**Bloco A — Timeline de passos (máquina de estados) no detalhe:**
- Componente de progressão (stepper/timeline vertical) no detalhe do job mostrando os passos da máquina de estados, marcando cada um como **concluído / atual / pendente**, derivado do `status` atual (sem backend novo).
- Passos exibidos: Upload → Aguardando aprovação → Análise por IA (inferência) → Revisão do mapa → Preview → Migrando → Concluído. Caminhos terminais alternativos: **Falhou** e **Rejeitado** (substituem o passo final quando aplicável); **Desfeito** como estado pós-conclusão.

**Bloco B — Log de eventos (histórico de transições com ator + timestamp):**
- Persistência **forward-only** de cada transição de estado do job numa tabela leve `migracao_job_eventos` (job_id, estabelecimento_id, status_anterior, status_novo, usuario_id nullable, criado_em). **Sem PII** (só status + ator técnico).
- Endpoint admin de leitura dos eventos do job (`GET /{jobId}/eventos`), exibido como log/timeline cronológico no detalhe ("Aprovado por admin em 15/06 14:32", "Inferência concluída em 15/06 14:33", etc.).
- Os eventos são gravados pelos **pontos de transição já existentes** (no aggregate ou no handler que persiste a transição) — reuso dos pontos, sem reescrever a máquina de estados.

**Bloco C — Progresso parcial ao vivo durante a carga:**
- Endpoint admin leve de progresso (`GET /{jobId}/progresso`) que **agrega `migracao_registros` por (entidade, status)** — funciona **durante** `migrando` (diferente do relatório, que só roda após concluir). Retorna, por entidade: total, processados (não-pendentes), e quebra criados/atualizados/rejeitados/pulados/pendentes, mais um percentual agregado.
- Exibição no detalhe (estado `migrando`): barra/contadores por entidade que sobem em tempo real ("Pacientes: 643/1000 — 64%", "Agendas: 0/3481 — 0%"), substituindo a mensagem genérica atual.

**Bloco D — Atualização ao vivo (polling no front):**
- Polling no detalhe que recarrega job + progresso **apenas enquanto o job está em estado ativo** (`aguardando_mapa` / `mapa_em_revisao` durante inferência, e `migrando` durante carga). Intervalo **4 segundos** (D-P1). Para de pollar ao atingir estado terminal/inativo (`concluido`, `concluido_com_erros`, `desfeito`, `rejeitado`, `falhou`, `aguardando_aprovacao`, `preview_pronto`).

**Bloco E — Lista: ordenação e filtros:**
- **Ordenação**: garantir/explicitar `ORDER BY criado_em DESC` (mais recente primeiro) na query de listagem. **Já está correto** no código atual ([MigracaoAdminQueryRepository.cs](../backend/src/Services/Imedto.Backend.Infrastructure/Admin/QueryRepositories/MigracaoAdminQueryRepository.cs):46) — este addendum só **blinda com CA explícito** (CA60) e mantém.
- **Filtros novos** (combináveis entre si e com o status já existente), via WHERE dinâmico multi-tenant:
  - **Estabelecimento** (já existe o param `estabelecimentoId` no backend e no service — falta a UI de seleção na fila).
  - **Período** (data de criação): intervalo **de / até** (D-F1).
  - **Onda** (1 / 2): a coluna `onda` já existe (`null` = Onda 1, `"prontuario"` = Onda 2).
  - **Origem** (texto do sistema de origem).
- UI de filtros na fila + backend (WHERE dinâmico) + service.

**Não inclui (registrar como backlog / fora deste addendum):**

- **Histórico retroativo de transições de jobs antigos.** A persistência de eventos é forward-only; jobs já existentes não ganham log histórico (só timeline derivada do status atual). D-T2.
- **Polling no lado do cliente** (Config do Estabelecimento). O cliente vê o status do próprio job ao abrir a tela; auto-refresh do lado cliente é backlog. Este addendum só faz polling no painel **admin**.
- **Notificação ativa** (sino/e-mail) ao operador quando a carga conclui/falha. Mantido como backlog (mesma decisão dos addendums 002/003).
- **Detalhamento linha-a-linha** dos registros (lista de cada registro com seu status). O progresso e o relatório são **agregados** por entidade/status/motivo — não a lista linha-a-linha. Backlog se o operador pedir.
- **Filtro por texto livre/busca** na fila e **presets de período** (7/30 dias). O filtro de período é **intervalo de/até** explícito (D-F1); presets são açúcar opcional, fora do CA obrigatório.
- **Ordenação configurável pelo operador** (clicar coluna para ordenar). A ordenação é fixa `criado_em DESC` (mais recente primeiro) — requisito do usuário. Ordenação por outras colunas é backlog.
- **WebSocket / push em tempo real.** O "ao vivo" é **polling** simples no front (D-P1), não streaming. Adequado para um painel admin interno.

## 4. Decisões de produto (fechadas neste addendum)

> Os pontos que o BA fechou. Padrão conservador: reuso, LGPD-safe, mínimo viável que serve. Onde a premissa do usuário conflitou com o código real (timeline/audit), a decisão está explícita e foi sinalizada na nota da §1.

- **D-T1 — Timeline de passos é derivada do status, client-side, sem backend.** O front conhece a ordem canônica dos estados (a mesma que já mapeia em `STATUS_LABELS`) e renderiza o stepper marcando concluído/atual/pendente a partir do `status` atual do job. **Por quê:** zero custo de backend, satisfaz "onde está / o que falta" (pontos 2 da demanda), e a máquina de estados é estável e conhecida. Caminhos terminais (`falhou`, `rejeitado`, `desfeito`) trocam o passo final em vez de inventar um passo intermediário.

- **D-T2 — Log de eventos com ator+timestamp EXIGE persistir transições (forward-only).** Como **não existe** histórico persistido (ver §1), a timeline "quem fez, quando" não pode ser derivada do passado. Decisão: persistir cada transição numa tabela leve `migracao_job_eventos` **a partir do deploy** (forward-only). Jobs antigos mostram só a timeline de passos (D-T1) sem o log granular; jobs novos mostram o log completo. **Por quê:** é o mínimo honesto que entrega o "quem/quando" pedido sem reconstruir um passado que não foi gravado; tabela leve, escrita nos pontos de transição já existentes, sem reescrever a máquina de estados. **Alternativa descartada:** tentar derivar do `atualizado_em` — ele é sobrescrito a cada transição, não guarda histórico, e não tem o ator nem o status anterior por etapa.

- **D-T3 — Eventos são metadados sem PII.** Cada evento guarda `status_anterior`, `status_novo`, `usuario_id` (admin no caso de aprovação/disparo/desfazer; **null/sistema** quando a transição é feita por job recorrente sem usuário humano — inferência, carga, falha automática) e `criado_em`. **Nunca** nome de arquivo, valor de coluna, CPF, nome de paciente. **Por quê:** mesma garantia LGPD do `motivo_falha`/`motivo_rejeicao` (categorias, sem PII) — espelha CA4 do original e R-B2 do addendum 002. O `usuario_id` é o id técnico do ator (não PII de paciente); o front exibe "admin"/"sistema" sem expor o id cru se preferir.

- **D-P1 — Polling no front, 4 segundos, só em estados ativos.** O detalhe faz polling a cada **4s** que recarrega `obter(job)` + `progresso(job)` **enquanto** o job está em `aguardando_mapa`, `mapa_em_revisao` ou `migrando` (estados onde algo muda sozinho por job recorrente). Para de pollar (cleanup do timer) ao entrar em qualquer estado terminal/inativo ou ao desmontar a view. **Por quê:** 4s é responsivo o bastante para o operador ver o progresso subir, sem martelar o backend; só os estados ativos pollam porque os demais não mudam sem ação do próprio operador (que já recarrega ao agir). `aguardando_aprovacao` e `preview_pronto` **não** pollam — esperam ação humana, não progresso automático. (Intervalo é liberdade técnica entre 3–5s; 4s é o default cravado.)

- **D-F1 — Filtro de período é intervalo de/até (data de criação), não preset.** Dois campos de data (`criadoDe`, `criadoAte`), ambos opcionais e combináveis (só de, só até, ou ambos). Filtra por `criado_em`. **Por quê:** intervalo explícito é o mais simples que serve a "achar jobs daquele dia/semana específica" sem a ambiguidade de presets; presets (7/30 dias) são açúcar opcional de UI, fora do CA. O filtro é **inclusivo** (`criado_em >= criadoDe` e `criado_em < criadoAte + 1 dia` para cobrir o dia inteiro — liberdade técnica de como tratar a borda, desde que o dia "até" seja incluído).

- **D-F2 — Todos os 4 filtros são combináveis e multi-tenant-safe.** Estabelecimento + Período + Onda + Origem + Status (já existente) combinam num WHERE dinâmico. Nenhum filtro vaza cross-tenant: a query continua sendo de leitura admin (sem PII), e o filtro de estabelecimento **restringe**, nunca expande o escopo. **Por quê:** o operador precisa cruzar critérios ("jobs de prontuário do estabelecimento X criados ontem que falharam"); WHERE dinâmico já é o padrão da query atual (estabelecimento + status), só estende.

- **D-C1 — Progresso é endpoint separado do relatório, agrega `migracao_registros` por (entidade, status).** O relatório atual ([RelatorioMigracaoQueryHandler.cs](../backend/src/Services/Imedto.Backend.Application/Admin/Migracao/RelatorioMigracaoQueryHandler.cs):27) **bloqueia** leitura enquanto o job não concluiu (422). Para o progresso ao vivo, um endpoint leve novo (`GET /{jobId}/progresso`) agrega `SELECT entidade, status, COUNT(*) GROUP BY entidade, status` **sem** o gate de status — funciona durante `migrando`. **Por quê:** não afrouxar o relatório (que tem semântica de "final"); o progresso é uma leitura barata e distinta. O índice `(migracao_job_id, status)` em `migracao_registros` já existe e serve a agregação.

- **D-C2 — "Total" por entidade = total de registros daquela entidade no job; "% = não-pendentes / total".** O denominador é o número de registros já em staging para a entidade (todas as linhas do arquivo daquela entidade, materializadas como `migracao_registros` no preview). O numerador do progresso é a contagem de registros **não-`pendente`** (criados + atualizados + rejeitados + pulados). Percentual por entidade = processados/total; percentual agregado do job = soma(processados)/soma(total). **Por quê:** os registros já são materializados em staging antes da carga (no preview), então o "total esperado" é conhecido e estável durante `migrando` — dá uma barra honesta. Se uma entidade ainda não tem registros em staging, aparece como 0/0 (não renderiza barra, evita divisão por zero).

## 5. Regras de negócio

**Bloco A/B — Timeline e log de eventos:**

- **R-T1 (timeline de passos derivada — D-T1)**: O detalhe renderiza a progressão dos estados a partir do `status` atual, marcando concluído/atual/pendente. A ordem canônica é a da máquina de estados (`aguardando_arquivo → aguardando_aprovacao → aguardando_mapa → mapa_em_revisao → preview_pronto → migrando → concluido`). `falhou`/`rejeitado`/`desfeito` são terminais que substituem o passo final. Mora em: front (componente de timeline). Validada em: front. (Sem backend novo.)

- **R-T2 (persistência forward-only de transições — D-T2)**: Cada transição de estado do `MigracaoJob` grava uma linha em `migracao_job_eventos` com `{job_id, estabelecimento_id, status_anterior, status_novo, usuario_id (nullable), criado_em}`. A gravação acontece no ponto onde a transição é **persistida** (handler/repositório que salva o job após a mutação), reusando os pontos existentes — **não** se reescreve a máquina de estados do aggregate. Transições por job recorrente (inferência, carga, falha automática) gravam `usuario_id = null` (sistema). Mora em: ponto de persistência da transição (Application/Infrastructure) + tabela nova. Validada em: back.

- **R-T3 (eventos sem PII — D-T3 / LGPD)**: Nenhum evento contém PII — só `status_anterior`, `status_novo`, `usuario_id` técnico do ator e `criado_em`. A leitura (`GET /{jobId}/eventos`) retorna só esses campos. O front exibe rótulos legíveis ("Aprovação por admin", "Inferência concluída pelo sistema") derivados do par de status, sem expor valor de dado do cliente. Mora em: tabela + query de leitura + front. Validada em: back + front (QA confere ausência de PII).

- **R-T4 (multi-tenant — eventos)**: `migracao_job_eventos` carrega `estabelecimento_id` (herdado do job). A leitura usa o repositório admin existente que escopa por job; job de outro contexto → "não encontrado" genérico. Mora em: repositório admin. Validada em: back.

**Bloco C — Progresso ao vivo:**

- **R-C1 (progresso agrega registros por entidade/status — D-C1/D-C2)**: O endpoint `GET /{jobId}/progresso` retorna, por entidade do job: `total`, `pendentes`, `criados`, `atualizados`, `rejeitados`, `pulados`, e `percentual` (= (total − pendentes) / total, ou 0 se total = 0). Agrega **apenas** os `migracao_registros` daquele job (que carregam `estabelecimento_id` do tenant). Diferente do relatório, **não** exige status concluído — responde durante `migrando` (e também antes, retornando o que houver em staging). Mora em: query de leitura (Infrastructure) + Result DTO (Contracts). Validada em: back.

- **R-C2 (progresso é multi-tenant e sem PII — herdada)**: A agregação conta só registros do job (escopo de tenant herdado); nenhuma PII é exposta (só contagens por categoria de status). Mora em: query de leitura. Validada em: back.

**Bloco D — Polling:**

- **R-D1 (polling só em estados ativos — D-P1)**: O detalhe inicia um timer de 4s que recarrega job + progresso **somente** quando `status ∈ {aguardando_mapa, mapa_em_revisao, migrando}`. Ao detectar transição para qualquer outro estado (terminal/inativo) ou ao desmontar a view, o timer é **limpo** (sem vazamento de timer, sem requisições após sair). Mora em: front (composable/lifecycle da view). Validada em: front (QA confere que o polling cessa).

**Bloco E — Lista, ordenação e filtros:**

- **R-E1 (ordenação fixa mais-recente-primeiro)**: A listagem ordena por `criado_em DESC` sempre (já implementado). Nenhuma ordenação alternativa neste addendum. Mora em: query de listagem. Validada em: back (CA60 blinda contra regressão).

- **R-E2 (filtros combináveis em WHERE dinâmico multi-tenant — D-F1/D-F2)**: A query de listagem aceita, além de `estabelecimentoId` e `status` (já existentes), os filtros opcionais `criadoDe`, `criadoAte`, `onda` e `origem`. Todos entram como condições `AND` no WHERE dinâmico; ausência de um filtro = não restringe por ele. O filtro de estabelecimento sempre **restringe** o escopo (nunca expande). `onda`: `null`/"onda1" filtra `onda IS NULL`; "prontuario"/"onda2" filtra `onda = 'prontuario'` (mapeamento de rótulo amigável → valor de coluna é liberdade técnica, desde que o CA passe). `origem`: match por igualdade ou `ILIKE` (liberdade técnica). Mora em: query de listagem (Infrastructure) + controller (params) + service (front). Validada em: back + front.

- **R-E3 (filtros não vazam cross-tenant)**: Como a query é de leitura admin sem PII e o filtro de estabelecimento restringe, nenhuma combinação de filtros retorna job/registro de outro contexto indevidamente; e nenhum campo de PII de paciente é exposto na listagem (só metadados de job). Mora em: query de listagem. Validada em: back (CA59).

## 6. Modelo de dados

**Nova tabela `migracao_job_eventos`** (staging multi-tenant — Bloco B):
- `id` (PK Imedto), `migracao_job_id` (FK → `migracao_jobs`), `estabelecimento_id` (multi-tenant, NOT NULL, herdado do job), `status_anterior` (text, nullable — null no evento de criação), `status_novo` (text, NOT NULL), `usuario_id` (uuid, **nullable** — null = sistema/job recorrente), `criado_em` (timestamptz, NOT NULL).
- Índice: `(migracao_job_id, criado_em)` para ler a timeline cronológica de um job.
- **Sem PII** — só metadados de transição. `payload`/motivo **não** entram aqui (motivo de falha já vive em `migracao_jobs.motivo_falha`).

**`migracao_jobs` — índice para filtro de período (Bloco E):**
- Hoje existe `(estabelecimento_id, status)` e `(estabelecimento_id, onda, status)`. **Não existe** índice que cubra ordenação/filtro por `criado_em`. Como a ordenação é sempre `criado_em DESC` e o filtro de período é por `criado_em`, **avaliar com `imedto-database`** um índice `(estabelecimento_id, criado_em DESC)` (ou `(criado_em DESC)` quando sem filtro de estabelecimento). O `imedto-database` decide a forma exata conforme o `EXPLAIN`; o volume de jobs é baixo (1 a poucos por cliente), então o índice é "performance dia 1" preventivo, não urgência. Decisão de criar/qual índice = `imedto-database`.

**`migracao_registros` — sem alteração de schema (Bloco C):**
- O progresso **reusa** `migracao_registros` (já tem `migracao_job_id`, `estabelecimento_id`, `entidade`, `status`) e o índice `(migracao_job_id, status)` já existente. **Sem migration** para o progresso.

**Coluna opcional `carga_iniciada_em` em `migracao_jobs`** (liberdade técnica, **não obrigatória**): se o dev/db quiser exibir "carga iniciada há X" no progresso, pode adicionar um timestamp de início de carga. **Não é exigência de produto** — o evento `→ migrando` em `migracao_job_eventos` já carrega esse timestamp. Default: **não** adicionar coluna; usar o evento.

**Migration**: 1 migration idempotente para `migracao_job_eventos` (CREATE TABLE IF NOT EXISTS + índice) + (avaliação do) índice de `criado_em` em `migracao_jobs`. **Atenção ao gotcha conhecido**: DDL de EF tem que ser idempotente (`Sql` com `IF NOT EXISTS`) — schema pode ser aplicado fora da pipeline na validação local do QA. Acionar `imedto-database`.

**Audit / LGPD**: `migracao_job_eventos` é o próprio mecanismo de audit das transições (materializa o que os CAs 20/32/49 descreviam). Sem PII. O progresso e os filtros não tocam dado novo de paciente.

## 7. UX e fluxo

**Detalhe / revisão (`MigracaoRevisaoView`):**

- **Timeline de passos (topo do detalhe, sempre visível)**: stepper vertical/horizontal com os passos da máquina de estados; passo atual destacado, anteriores marcados como concluídos, próximos como pendentes. Em `falhou`/`rejeitado`, o passo final vira o terminal correspondente (cor de erro). Reusar tokens de cor do design system; tipografia **sempre por tokens** (`var(--text-*)`, `var(--font-weight-*)`), nunca literais (CLAUDE.md §5).
- **Log de eventos (seção colapsável ou abaixo da timeline)**: lista cronológica dos eventos do job (`GET /{jobId}/eventos`), formato "Recebido • 15/06 14:30", "Aprovado (admin) • 15/06 14:32", "Inferência concluída (sistema) • 15/06 14:33", etc. Estado vazio (job antigo sem eventos persistidos): mensagem honesta ("Histórico detalhado disponível a partir desta migração") em vez de tabela vazia. Componentes: reusar padrão de lista/`AppEmptyState`.
- **Progresso ao vivo (no ramo `statusJob === 'migrando'`)**: substitui a mensagem genérica atual + botão manual por contadores/barras por entidade que sobem sozinhos via polling: "Pacientes 643/1000 (64%)", "Agendas 0/3481 (0%)". Reusar componente de barra de progresso do DS se houver; senão, barra simples com tokens. Mantém o botão "Atualizar" manual como fallback (não obrigatório se o polling cobre).
- **Polling (D-P1)**: ao montar a view, se o status for ativo, inicia o timer de 4s (recarrega job + progresso); ao status virar terminal ou ao desmontar, limpa o timer. Sem flicker (atualização in-place dos números).
- **Relatório final (estados `concluido`/`concluido_com_erros`)**: **inalterado** — continua mostrando contagens + motivos agregados (addendum 002). A timeline e o log de eventos ficam visíveis também aqui (histórico do que aconteceu).

**Fila de migrações (`MigracoesListView`):**

- **Barra de filtros** acima da tabela, combináveis: seletor de **Estabelecimento** (reusar o seletor de estabelecimento do admin se existir; senão, input/select simples), intervalo de **Período** (`criadoDe`/`criadoAte` — dois date inputs), seletor de **Onda** (Todas / Onda 1 / Onda 2), input de **Origem**, além do seletor de **Status** já existente. Botão "Limpar filtros". Aplicar filtro → recarrega a lista na página 1.
- **Ordenação**: mantém mais-recente-primeiro (sem UI de ordenação).
- **Colunas**: inalteradas (#, Estabelecimento, Origem, Status, Criado em, Ver). Reusar `AppPagination`, `AppBadge`. Tipografia por tokens.
- **Estados**: loading, vazio (`AppEmptyState` — "Nenhum job com esses filtros"), com dados.

**Service/DTO front (`migracaoAdminService` / `migracaoService.ts`):**
- `listar` ganha os params `criadoDe`, `criadoAte`, `onda`, `origem` (além de `estabelecimentoId`, `status`, `page`, `size` existentes).
- Novo `obterEventos(jobId)` → `MigracaoEventoDto[]` (`statusAnterior`, `statusNovo`, `ator` ("admin"/"sistema"), `criadoEm`).
- Novo `obterProgresso(jobId)` → `ProgressoMigracaoResult` (`porEntidade: Record<string, { total, pendentes, criados, atualizados, rejeitados, pulados, percentual }>`, `percentualAgregado`).

Mobile: painel admin é desktop-first (aceitável, herdado dos briefings anteriores).

## 8. Critérios de aceite (testáveis) — começam em CA51

**Bloco A — Timeline de passos:**

- **CA51 (timeline reflete o status atual — R-T1)**: Dado um job em `migrando`, Quando o operador abre o detalhe, Então a timeline mostra Upload, Aguardando aprovação, Análise por IA, Revisão do mapa e Preview como **concluídos**, "Migrando" como **atual**, e "Concluído" como **pendente** — derivado do status, sem chamada de backend dedicada à timeline de passos.

- **CA52 (terminais na timeline)**: Dado um job em `falhou` (ou `rejeitado`), Quando o detalhe carrega, Então o passo final da timeline mostra o **terminal correspondente** (Falhou/Rejeitado) em estado de erro, e os passos já cumpridos antes da falha permanecem marcados como concluídos.

**Bloco B — Log de eventos:**

- **CA53 (evento persistido a cada transição — R-T2)**: Dado um job novo que percorre `aguardando_arquivo → aguardando_aprovacao → aguardando_mapa → mapa_em_revisao → ... → concluido`, Quando cada transição ocorre, Então uma linha é gravada em `migracao_job_eventos` com `{job_id, estabelecimento_id, status_anterior, status_novo, usuario_id (admin na aprovação/disparo; null no que é feito por job recorrente), criado_em}`.

- **CA54 (log exibido no detalhe)**: Dado um job com eventos persistidos, Quando o operador abre o detalhe, Então vê o log cronológico legível (ex.: "Aprovado (admin) • 15/06 14:32", "Inferência concluída (sistema) • 15/06 14:33"), em ordem por `criado_em`.

- **CA55 (log sem PII — R-T3/LGPD)**: Dado qualquer evento de transição, Quando o log é lido/exibido, Então cada linha contém **apenas** status anterior/novo, ator (admin/sistema) e timestamp — **nunca** nome de arquivo do cliente, CPF, nome de paciente, valor de coluna ou mensagem técnica crua.

- **CA56 (job antigo sem histórico — D-T2)**: Dado um job criado **antes** desta entrega (sem eventos persistidos), Quando o operador abre o detalhe, Então a timeline de passos (derivada do status) aparece normalmente, e o log de eventos mostra um estado honesto ("histórico detalhado disponível a partir desta migração") em vez de tabela vazia ou erro.

**Bloco C — Progresso ao vivo:**

- **CA57 (progresso durante a carga — R-C1/D-C1)**: Dado um job em `migrando` com 1.000 pacientes em staging, dos quais 643 já foram processados (criados/atualizados/rejeitados/pulados) e 357 pendentes, Quando o operador consulta `GET /{jobId}/progresso`, Então a resposta traz, na entidade `paciente`, `total=1000`, `pendentes=357`, `percentual=64` (≈), e a tela mostra "Pacientes 643/1000 (64%)" — **sem** exigir que o job esteja concluído (diferente do relatório).

- **CA58 (progresso agrega por entidade e status — D-C2)**: Dado um job com pacientes e agendas em staging, Quando o progresso é consultado, Então cada entidade traz sua própria quebra (`criados/atualizados/rejeitados/pulados/pendentes`) e o percentual agregado do job = soma(processados)/soma(total); entidade sem registros em staging aparece como 0/0 e a tela não renderiza barra para ela (sem divisão por zero).

- **CA59 (progresso multi-tenant e sem PII — R-C2)**: Dado o progresso de um job, Quando a agregação roda, Então conta **apenas** registros daquele job (escopo de tenant herdado), nenhum registro de outro job/tenant entra, e nenhuma PII é exposta — só contagens por status.

**Bloco D — Polling:**

- **CA60 (polling só em estados ativos, e cessa — R-D1/D-P1)**: Dado um job em `migrando` aberto no detalhe, Quando a view está montada, Então o front recarrega job + progresso a cada ~4s e os números sobem ao vivo; e quando o job transiciona para `concluido` (ou qualquer terminal) ou o operador sai da view, Então o polling **cessa** (nenhuma requisição adicional após o estado terminal / após desmontar). E dado um job em `aguardando_aprovacao` ou `concluido`, Quando o detalhe abre, Então **não** há polling (estado inativo).

**Bloco E — Lista, ordenação e filtros:**

- **CA61 (ordenação mais-recente-primeiro — R-E1)**: Dado vários jobs com `criado_em` diferentes, Quando a fila carrega (com ou sem filtros), Então os jobs vêm ordenados por `criado_em DESC` (o mais recente primeiro), em todas as páginas.

- **CA62 (filtro de estabelecimento — R-E2/multi-tenant)**: Dado jobs de vários estabelecimentos, Quando o operador filtra por estabelecimento X, Então só jobs do estabelecimento X aparecem; nenhum job de outro estabelecimento vaza; e a contagem/paginação reflete só o escopo filtrado.

- **CA63 (filtro de período de/até — R-E2/D-F1)**: Dado jobs criados em datas diferentes, Quando o operador define `criadoDe = 14/06` e `criadoAte = 15/06`, Então só jobs criados nesse intervalo (inclusive o dia "até" inteiro) aparecem; só `de` ou só `até` também funcionam (limite aberto do outro lado).

- **CA64 (filtro de onda — R-E2)**: Dado jobs de Onda 1 (`onda IS NULL`) e de Onda 2 (`onda = 'prontuario'`), Quando o operador filtra por "Onda 2", Então só os jobs de prontuário aparecem; "Onda 1" mostra só os de `onda IS NULL`; "Todas" não filtra por onda.

- **CA65 (filtro de origem — R-E2)**: Dado jobs com origens "iClinic" e "Feegow", Quando o operador filtra por origem "iClinic", Então só jobs dessa origem aparecem.

- **CA66 (filtros combinados — R-E2/D-F2)**: Dado o conjunto de jobs, Quando o operador combina estabelecimento X + Onda 2 + status `falhou` + período de ontem, Então só os jobs que satisfazem **todas** as condições aparecem (AND), e a ordenação `criado_em DESC` é mantida.

- **CA67 (filtros não vazam cross-tenant nem PII — R-E3)**: Dado qualquer combinação de filtros, Quando a lista é retornada, Então nenhum job/registro de tenant não solicitado aparece e nenhuma coluna de PII de paciente é exposta (só metadados de job).

**Regressão e RBAC:**

- **CA68 (RBAC — tudo é ImedtoAdmin)**: Dado um usuário sem a policy `ImedtoAdmin`, Quando chama `GET /{jobId}/eventos`, `GET /{jobId}/progresso`, ou a listagem com filtros, Então recebe **403**, e os elementos de UI (fila admin, detalhe, filtros) ficam ocultos no front. (Reuso da policy já aplicada em todo o `AdminMigracaoController`.)

- **CA69 (regressão — fluxos anteriores intactos)**: Dado os CAs dos briefings 001/002/003 (upload, aprovação, inferência, revisão de mapa, preview, carga, relatório com motivos, falha + reprocessar, gate anti-IA CA44), Quando esta entrega de observabilidade é aplicada, Então **nada** desses comportamentos regride — em especial: o relatório final continua só após concluir (o progresso é endpoint **separado**), o gate anti-IA (CA44) continua valendo, e a ordenação `criado_em DESC` (já existente) é preservada.

## 9. Riscos e dependências

- **Premissa quebrada do "audit já existe"**: a demanda assumiu histórico de transições persistido — **não existe** (§1). O log granular "quem/quando" só vale **forward-only** (D-T2/CA56). Jobs antigos não terão log retroativo. **Confirmar com o usuário** que está OK entregar o histórico a partir de agora (não retroativo) — é a única decisão que muda expectativa.
- **Polling pode martelar o backend se mal-finalizado**: o timer **tem** que ser limpo ao sair da view e ao atingir terminal (CA60). Risco clássico de timer vazado em SPA. QA valida que não há requisição após desmontar/concluir.
- **Progresso vs. relatório (semântica)**: não afrouxar o gate de status do **relatório** (ele significa "final"). O progresso é endpoint próprio, sem gate (D-C1). Misturar os dois quebraria a semântica do relatório. CA69 cobre.
- **Índice de período**: filtro/ordenação por `criado_em` sem índice dedicado funciona com volume baixo, mas o `imedto-database` deve avaliar `(estabelecimento_id, criado_em DESC)` para não regredir quando a base de jobs crescer. Não bloqueia, mas é "performance dia 1".
- **Eventos forward-only e jobs em voo no deploy**: jobs em estados intermediários no momento do deploy não terão os eventos das transições já ocorridas — só das que ocorrerem após o deploy. Aceitável (D-T2); não há backfill.
- **Área regressiva**: nenhuma de domínio. Risco contido no bounded context de migração (UI + leitura + 1 tabela de eventos + índice). Commands de paciente/estoque/orçamento/prontuário **não** são tocados.

## 10. Observações para execução

- **Reuso obrigatório (não construir do zero)**: a timeline de passos reusa o mapa de estados (`STATUS_LABELS`) já existente no front; os filtros estendem o **WHERE dinâmico já existente** na `ListarJobsAsync` (que já trata estabelecimento + status) — só adicionar condições; o param `estabelecimentoId` já existe no backend e no service (falta só a UI). O endpoint de eventos e o de progresso espelham o padrão dos endpoints admin existentes (`relatorio`, `reprocessar`, `aprovar-analise`) no `AdminMigracaoController` (mesma policy `ImedtoAdmin`, mesma forma de handler). Badges/labels reusam `AppBadge`; paginação reusa `AppPagination`.
- **Não-negociável**: eventos e progresso são **sem PII** (R-T3, R-C2) — só status/contagens/ator técnico. Mensagem técnica nunca aparece na UI (espelha CA4 do original, R-B2 do addendum 002).
- **Não-negociável (anti-regressão)**: não afrouxar o gate de status do **relatório** (continua só após concluir); o **progresso** é endpoint separado. E o gate anti-IA do addendum 003 (CA44) permanece — esta entrega não toca a query de seleção do recorrente.
- **Não-negociável**: tipografia **sempre por tokens** (CLAUDE.md §5) na timeline, no log, no progresso e na barra de filtros — zero `font-size`/`font-weight` literal. O gate `npm run check:typography -- --ci` tem que passar.
- **Liberdade técnica**: o intervalo exato do polling entre 3–5s (default cravado: **4s**); o componente de timeline/stepper (novo no DS ou local na view — se for reutilizável, vai pro DS e documenta em `Docs/DESIGN.md`); o formato de borda do filtro de período (inclusivo do dia "até"); o mapeamento de rótulo de onda → valor de coluna; igualdade vs. ILIKE no filtro de origem; e a decisão de adicionar ou não a coluna `carga_iniciada_em` (default: não, usar o evento). Tudo desde que respeite os CAs.
- **Acionar `imedto-database`** para: (1) tabela `migracao_job_eventos` (PK Imedto, FK para `migracao_jobs`, `estabelecimento_id`, índice `(migracao_job_id, criado_em)`); (2) avaliar índice `(estabelecimento_id, criado_em DESC)` em `migracao_jobs` para o filtro/ordenação de período; (3) confirmar que `migracao_registros` não precisa de índice novo para o progresso (o `(migracao_job_id, status)` já existe). Migration idempotente (gotcha `AddColumn`/`CREATE TABLE` não-idempotente).
- **Fatiamento sugerido** (PRs pequenos sob este mesmo ID): PR 1 = Bloco E (filtros + ordenação na lista — backend + UI, sem schema além do índice opcional). PR 2 = Bloco C+D (progresso + polling). PR 3 = Bloco A+B (timeline de passos + tabela de eventos + log). Podem ir juntos se o QA preferir um ciclo só; são independentes.

## 11. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — atualizar a descrição do **bounded context de migração** para registrar: (1) a tabela `migracao_job_eventos` como **trilha de transições persistida** do job (materializa o audit que os CAs 20/32/49 descreviam mas que não estava persistido); (2) o padrão "endpoint de progresso leve agregando staging por status, separado do relatório final"; (3) o padrão de **polling no front só em estados ativos** para jobs assíncronos. Incremental: nota na seção de migração. **A ser feito no PR pelo dev/BA no marco correspondente.**
- **`Docs/DESIGN.md`** — **se** o componente de timeline/stepper for criado como componente reutilizável do design system (em `frontend/src/components/ui/`), adicioná-lo à seção de componentes do DS. Se ficar local na view, não atualizar. Avaliar no PR do Bloco A. (Também conferir se o `AppPillToggle`/seletor usado nos filtros já está documentado — backlog de doc conhecido.)
- **`Docs/LGPD.md`** — nota incremental: `migracao_job_eventos` é metadado de transição **sem PII** (mesma garantia do `motivo_falha`/`motivo_rejeicao`); o progresso ao vivo expõe só contagens por status. Só atualizar se a seção de migração já listar as garantias de não-PII; caso contrário, coberto pela regra geral. Avaliar no marco.
- **`Docs/INFRA.md`** — sem mudança de infra. **Não atualizar.**
- **`Docs/Discoverys/migracao-dados-ia/01_discovery.md`** — não alterar (é a fonte do original; este addendum é melhoria de observabilidade pós-entrega, não muda D1–D14).
