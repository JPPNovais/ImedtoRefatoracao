# Relatório de acessos LGPD no detalhe do paciente (diferencial A3)

**ID**: 2026-06-10_007
**Status**: Aprovado por usuário em 2026-06-10 (modo autônomo — decisões de produto fornecidas pelo orquestrador; ambiguidade residual fechada com default mais simples e registrada em §11)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P-M
**Áreas regressivas tocadas**: prontuário (leitura de audit trail), permissionamento (gate Dono), relatório (PDF). Não toca: escrita de prontuário, orçamento, financeiro, estoque, agenda.

> Roadmap: [`Docs/Roadmap/FASE_1_COMPLETUDE.md`](../Docs/Roadmap/FASE_1_COMPLETUDE.md) item **1.8** (diferencial LGPD A3). LGPD: [`Docs/LGPD.md`](../Docs/LGPD.md). O sistema **já grava** o audit trail de acesso — esta demanda apenas o **expõe** numa tela e em PDF entregável ao titular.

## 1. Contexto e motivação

A LGPD dá ao titular o direito de saber **quem acessou seus dados pessoais, quando e para quê** (Art. 9º, Art. 18). O sistema já cumpre a metade difícil disso: cada leitura/edição/exportação de dados de paciente e prontuário **já gera** uma linha de auditoria append-only. Mas hoje esses dados ficam **invisíveis** — não há tela que os mostre. Quando um paciente exerce o direito de pedir o relatório de acessos, não há como entregá-lo sem consultar o banco manualmente.

Esta demanda fecha o ciclo: uma **aba "Acessos"** no detalhe do paciente que lista, em **linguagem leiga**, quem acessou os dados daquele paciente e quando, com **export em PDF institucional** para entregar ao titular. É um diferencial de confiança e conformidade vendável (roadmap A3) e tem esforço pequeno porque os dados já existem.

**Estado atual do audit (investigado)**:
- **`paciente_acesso_log`** (`Domain/Pacientes/PacienteAcessoLog.cs`, config `PacienteAcessoLogConfiguration`): `{ id, paciente_id, usuario_id, estabelecimento_id, tipo_acesso, ocorrido_em, ip_origem }`. `tipo_acesso` é o enum **`TipoAcessoPaciente`** = `Leitura | Edicao | Exclusao | Export | Anonimizacao` (gravado como string, 20 chars). **Já tem o índice `ix_paciente_acesso_log_paciente_data` em `(paciente_id, ocorrido_em)`** — exatamente a consulta do relatório.
- **`prontuario_acesso_log`** (`Domain/Prontuarios/ProntuarioAcessoLog.cs`): `{ id, prontuario_id, usuario_id, estabelecimento_id, tipo_acesso, ocorrido_em }`. `tipo_acesso` é o enum **`TipoAcessoProntuario`** = `Leitura | Escrita | Exportacao`. **Não tem `paciente_id`** — mapeia ao paciente via `prontuarios.paciente_id` (1:1). Índice `ix_acesso_log_prontuario_data` em `(prontuario_id, ocorrido_em)`.
- **`termo_emitido_acesso_log`** e **`agendamento_confirmacao_acesso_log`**: são logs de **acesso público por link** (IP, UserAgent, ação — **sem `usuario_id`**). Registram o **próprio titular** confirmando presença/aceitando termo via token, **não** acesso de equipe a dados do paciente. Ver decisão de escopo em §3/§11.
- Serviços de gravação: `IPacienteAcessoLogService` e `IProntuarioAcessoLogService` (append-only, best-effort, nunca quebram o fluxo).
- A tabela `usuarios` guarda o nome (`NomeCompleto`) — fonte do "quem" em linguagem leiga (JOIN por `usuario_id`).
- **Padrão de tela**: o detalhe do paciente (`PacienteDetalheView.vue`) já é organizado em abas lazy-loaded (`type Aba = "resumo" | "prontuario" | ... | "documentos" | "anexos"`; cada aba só consulta ao ser clicada). A aba **"Documentos"** (briefing `2026-06-09_009`, já implementada) é o **padrão exato a seguir** para a nova aba "Acessos".
- **Padrão de PDF de relatório**: `useRelatorioPdf.ts` + `usePdfHeader.ts` (fonte Nunito, `desenharCabecalho`, `autoTable`, `finalizarPaginas`, marca d'água, rodapé "Documento de gestão"). É o padrão institucional para **relatório tabular** — base do PDF de acessos.

## 2. Persona-alvo

- **Dono** do estabelecimento (no MVP, **apenas** este papel — ver R3) atendendo a uma solicitação de titular (paciente pede "quero saber quem viu meus dados") ou auditando acessos internamente.
- Momento da jornada: **pós-atendimento / resposta a solicitação de direito do titular**. Evento raro mas de alto valor de conformidade.
- Frequência: baixa, sob demanda.

## 3. Escopo

**Inclui**:
- **Nova aba "Acessos"** em `PacienteDetalheView.vue` (posição sugerida: ao lado de "Documentos"/"Anexos" — dev escolhe coerente, ver §6), **somente-leitura**.
- **Lazy-load**: a aba só consulta o backend ao ser clicada pela 1ª vez (premissa de performance do projeto).
- **Lista paginada server-side** com, por linha: **quem** (nome do usuário que acessou), **quando** (data/hora), **o quê** (recurso + ação **em linguagem leiga**).
- **Endpoint de leitura novo**: `GET /api/pacientes/{pacienteId}/acessos?pagina=&tamanho=`. Consolida `paciente_acesso_log` + `prontuario_acesso_log` daquele paciente (UNION), ordena por `ocorrido_em` desc, pagina server-side, retorna DTO de resumo com nome do usuário resolvido (JOIN `usuarios`).
- **Export PDF** institucional (jsPDF + `usePdfHeader`, padrão de `useRelatorioPdf`) em **linguagem leiga**, entregável ao titular. O PDF traz **o conjunto completo** do período/filtro vigente (ver §11 sobre paginação × export).
- **O próprio acesso a este relatório é auditado** (consultar a trilha de acessos de um paciente é, também, acesso a dado do paciente).
- **Multi-tenant** + **gate Dono** + **LGPD** + **estados** + **performance** (CAs obrigatórios).

**Não inclui** (explicitamente fora):
- **Logs de acesso público por link** (`termo_emitido_acesso_log`, `agendamento_confirmacao_acesso_log`): são o **próprio titular** agindo via token, sem `usuario_id`, e o foco do MVP é "qual **membro da equipe** acessou os dados". Backlog separado se o usuário quiser incluir "o paciente acessou via link" no relatório. (Registrado em §11.)
- **Acessos de sistema/jobs** (ex.: `usuario_id` de processo automático). MVP mostra acesso humano. Backlog se solicitado.
- **Filtros avançados** (por usuário, por tipo de ação, por intervalo customizado fino). MVP é lista cronológica paginada. (Ver §11 — período no export.)
- **Retenção configurável** do audit trail. A retenção é tema de política, não desta tela.
- **Qualquer CRUD** sobre o audit (é append-only e read-only por design).
- **Nova ação RBAC** (ex.: `ver_relatorio_acessos`). Decisão: gate por papel **Dono** direto, sem criar chave de permissão nova (ver R3/§11).

## 4. Regras de negócio

> **Premissa global**: regra no backend; front é UX. Multi-tenant e minimização são não-negociáveis (Docs/LGPD.md).

- **R1 — Consolidação dos dois logs**. O relatório une `paciente_acesso_log` (filtrado por `paciente_id`) e `prontuario_acesso_log` (filtrado pelo `prontuario_id` do paciente, resolvido via `prontuarios.paciente_id`). Resultado ordenado por `ocorrido_em` **desc**, paginado sobre o conjunto unificado (não duas paginações). Mora em: **Query/Dapper** (UNION ALL + ORDER BY + LIMIT/OFFSET). Se o paciente não tiver prontuário, o sub-select de prontuário simplesmente não contribui linhas (não quebra).
- **R2 — Multi-tenant**. Ambas as sub-consultas filtram `estabelecimento_id = @EstabelecimentoId`. Validação prévia de existência do paciente no tenant (`IPacienteRepository.ObterPorIdOuNulo(pacienteId, estabelecimentoId)` → `BusinessException("Paciente não encontrado.")` se ausente), espelhando o padrão do endpoint de Documentos (`ListarDocumentosDoPacienteQueryHandler`). Repositório falha-fechada: sem tenant claim → vazio/throws. Mora em: **Handler + Query**.
- **R3 — RBAC: apenas Dono (MVP)**. O endpoint exige papel **Dono** do estabelecimento ativo. **Decisão**: usar o gate de papel direto (mesmo mecanismo `[RequiresPapel]`/policy que outros endpoints exclusivos de Dono já usam — dev confirma o atributo/policy canônico), **sem** criar uma chave de ação RBAC nova. No front, a aba "Acessos" **só aparece** para Dono. Backend é a fonte da verdade: papel ≠ Dono → 403/SemAcesso. Mora em: **Controller (atributo de papel) + guard/condição de render no front**. Validado em: back + front.
- **R4 — O acesso ao relatório é auditado**. Carregar a lista de acessos de um paciente registra **uma** linha em `paciente_acesso_log` com `tipo_acesso = Leitura` (`{ paciente_id, usuario_id = solicitante, estabelecimento_id, ocorrido_em, ip_origem }`), via `IPacienteAcessoLogService.RegistrarAsync`. **Decisão**: registrar **1 por carga de página** (não 1 por linha exibida), coerente com o precedente de listagem clínica (briefing 2026-06-09_009). O export PDF também é um acesso → registra **uma** linha `Leitura` ao gerar o PDF (ver R5). Mora em: **Handler**. Validado em: back.
- **R5 — Export auditado**. O PDF é gerado a partir de uma chamada ao backend (mesmo endpoint, ou um modo "tudo"/sem paginação para o período — ver §11). Essa chamada **audita** o acesso (`Leitura`) como em R4. **Não** introduzir lógica de audit no front. Mora em: **Handler**. Validado em: back.
- **R6 — Linguagem leiga do "o quê"**. O `tipo_acesso` técnico vira texto leigo, montado de forma consistente (back no SELECT/DTO **ou** front a partir do par `{recurso, tipo_acesso}` — dev decide; sem PII clínica):
  - `paciente_acesso_log`: recurso = "Cadastro/dados do paciente". `Leitura` → "Visualizou os dados"; `Edicao` → "Atualizou os dados"; `Exclusao` → "Removeu o cadastro"; `Export` → "Exportou os dados (portabilidade)"; `Anonimizacao` → "Anonimizou os dados".
  - `prontuario_acesso_log`: recurso = "Prontuário". `Leitura` → "Consultou o prontuário"; `Escrita` → "Registrou no prontuário"; `Exportacao` → "Exportou o prontuário (PDF)".
  - Mapa textual canônico definido em **um lugar só** (constante/dicionário) e reusado por lista e PDF. Mora em: **back (DTO) ou front (helper)** — não duplicar nos dois.
- **R7 — Minimização**. O DTO de cada linha contém apenas `{ quem (nome do usuário), quando (timestamp), recurso (rótulo), acao (rótulo leigo) }`. **Não** expõe `usuario_id` cru, `ip_origem`, `prontuario_id`, nem qualquer conteúdo clínico (o audit trail por design já não guarda conteúdo — é log de **acesso**, não de mutação). Mora em: **DTO de resumo**. Validado em: back.
- **R8 — "Quem" resolvido de forma resiliente**. JOIN `usuarios` por `usuario_id` para o nome. Se o usuário foi removido/anonimizado e o nome não resolver, exibir rótulo neutro ("Usuário removido") — **nunca** vazar `usuario_id` cru nem quebrar a linha. Mora em: **Query + DTO**. Validado em: back.
- **R9 — LGPD: mensagens genéricas e sem PII em log**. Erros (paciente fora do tenant, falha de query) retornam mensagem genérica ("Paciente não encontrado." / "Erro ao carregar os acessos."). Nenhum log estruturado com PII do paciente. Mora em: **Handler + front (tratamento de erro)**. Validado em: back + front.
- **R10 — Performance: paginação obrigatória**. A lista é sempre paginada server-side (default **20** por página, alinhado ao padrão de Documentos). Sem `SELECT *` global. O índice `(paciente_id, ocorrido_em)` em `paciente_acesso_log` já cobre o lado paciente; confirmar cobertura equivalente no lado prontuário (`(prontuario_id, ocorrido_em)` existe; o JOIN `prontuarios.paciente_id` precisa ser eficiente — ver §5). Mora em: **Query**. Validado em: back + medição.

## 5. Modelo de dados

**Schema NÃO muda (provavelmente)** — todas as tabelas e os índices necessários já existem:
- `public.paciente_acesso_log` — `{ id, paciente_id, usuario_id, estabelecimento_id, tipo_acesso, ocorrido_em, ip_origem }`. Índice `ix_paciente_acesso_log_paciente_data (paciente_id, ocorrido_em)` ✅ cobre a consulta.
- `public.prontuario_acesso_log` — `{ id, prontuario_id, usuario_id, estabelecimento_id, tipo_acesso, ocorrido_em }`. Índice `ix_acesso_log_prontuario_data (prontuario_id, ocorrido_em)` ✅.
- `public.prontuarios` — usado para mapear `prontuario_id → paciente_id`. Confirmar que há índice/PK eficiente em `prontuarios.paciente_id` (1:1) para o JOIN do sub-select de prontuário. Se faltar, é o **único** índice que pode ser necessário.
- `public.usuarios` — LEFT JOIN por `usuario_id` para o nome (`NomeCompleto`).

**Ação para `imedto-database`**: inspecionar o schema e **confirmar** que (a) os dois índices de acesso_log existem como descrito e (b) o JOIN paciente↔prontuário↔acesso é coberto por índice. **Criar migration idempotente apenas se faltar** (esperado: nada a criar). **Não** criar índice especulativo se já houver cobertura.

**DTO unificado de resumo** (novo, em `Imedto.Backend.Contracts`):
```
AcessoPacienteResumoDto {
    Quem: string            // nome do usuário (ou "Usuário removido")
    Quando: DateTime        // ocorrido_em
    Recurso: string         // "Cadastro/dados do paciente" | "Prontuário"
    Acao: string            // rótulo leigo (R6) — ou enviar { tipoAcesso, recurso } e montar no front
}
PaginaAcessosDto { Itens: AcessoPacienteResumoDto[]; Total: int; Pagina: int; TamanhoPagina: int }
```

**Sem nova audit table** — o audit deste relatório (R4/R5) reusa `paciente_acesso_log` via `IPacienteAcessoLogService` (já existe). **Sem novo tipo de dado pessoal** — apenas expõe o que já é coletado.

## 6. UX e fluxo

**Aba "Acessos"** (`PacienteDetalheView.vue`) — espelhar a estrutura da aba **"Documentos"**:
- Ícone sugerido: `fa-solid fa-clipboard-list` ou `fa-solid fa-user-shield` (dev escolhe coerente com o set). A aba **só renderiza para Dono** (R3).
- Lazy-load: registrar a aba no mesmo mecanismo `abasCarregadas` / `onClick` que Documentos usa; só dispara `GET /api/pacientes/{id}/acessos` na 1ª abertura.

**Cabeçalho da seção** (reusar `prontuario-head`/padrão da view):
- `<h2 class="ds-section-title">` "Acessos aos dados do paciente" + `<p>` "Registro de quem acessou os dados deste paciente e quando, conforme a LGPD. Apenas visualização." + `AppButton` "Exportar PDF" no canto.

**Lista** (linha por acesso — reusar o padrão de card de lista enxuto da aba Documentos):
- Coluna 1: **quem** (nome) + ícone do recurso.
- Coluna 2: **o quê** (rótulo leigo, R6) — ex.: "Consultou o prontuário".
- Coluna 3: **quando** (`fmtData` com hora — ex.: "09/06/2026 14:32").

**Paginação**: `AppPagination` (`components/ui/AppPagination.vue`), default 20/página, server-side.

**Export PDF**:
- `AppButton` "Exportar PDF" → gera PDF via novo composable `useAcessosPdf` (espelhando `useRelatorioPdf.gerarAgendamentosPdf`): cabeçalho institucional (`desenharCabecalho` com `docTitle: "RELATÓRIO DE ACESSOS — LGPD"`, `docSubtitle` com nome do paciente e período), `autoTable` com colunas **Quem | O quê | Quando**, `finalizarPaginas` com aviso de rodapé adequado (ex.: "Relatório de acessos — Art. 9º/18 LGPD."). Nunito, marca d'água, mesmo tema (`PDF_THEME`).
- **Nome do paciente no PDF**: é dado do próprio titular a quem o relatório se destina → pode constar no cabeçalho (não é vazamento — é entrega ao próprio titular). Confirmar que o front já tem o nome carregado no detalhe (tem). **Não** incluir CPF/telefone do paciente no PDF (minimização — só o nome para identificar o relatório).

**Estados**:
- **Loading**: indicador padrão da view enquanto carrega a página.
- **Erro**: mensagem **genérica** (`msg-erro`/toast), sem PII.
- **Vazio**: `AppEmptyState` (ícone 🛡️, título "Nenhum acesso registrado", descrição "Os acessos aos dados deste paciente aparecerão aqui."). Caso real e legítimo (paciente recém-criado, ou só acessado por jobs fora de escopo).
- **PDF gerando**: botão "Exportar PDF" com `:loading` durante a geração; erro genérico em falha.

**Tipografia**: CLAUDE.md §5 — sem `font-size`/`font-weight` literais; usar tokens/classes do DS.

**Mobile-ready**: lista colapsa em coluna única (mesma media query da view); botão de export acessível.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — listagem)**: Dado um paciente com 3 acessos em `paciente_acesso_log` e 2 em `prontuario_acesso_log`, Quando o Dono clica na aba "Acessos", Então a lista exibe 5 linhas ordenadas por data/hora desc, cada uma com quem (nome), o quê (rótulo leigo) e quando.
- **CA2 (lazy-load)**: Dado que o Dono abre o detalhe do paciente em outra aba, Quando ele **não** clica em "Acessos", Então **nenhuma** requisição a `/api/pacientes/{id}/acessos` é disparada; ao clicar pela 1ª vez, exatamente **uma** requisição (página 1) é disparada; clicar de novo na mesma aba **não** refaz a busca.
- **CA3 (consolidação dos dois logs)**: Dado acessos nas duas tabelas para o mesmo paciente, Quando a lista carrega, Então linhas de "Cadastro/dados do paciente" **e** de "Prontuário" aparecem entremeadas na ordem cronológica desc correta (sem duplicar, sem pular registro na borda entre páginas).
- **CA4 (paginação server-side)**: Dado um paciente com 45 acessos consolidados, Quando a aba carrega, Então a 1ª requisição traz `tamanho=20` e `total=45`; ao ir para a página 2, nova requisição `pagina=2` traz os próximos 20; a ordenação desc é contínua entre páginas.
- **CA5 (linguagem leiga)**: Dado uma linha de `prontuario_acesso_log` com `tipo_acesso = Leitura`, Quando exibida, Então o texto é "Consultou o prontuário" (e **não** "Leitura" ou "prontuario_acesso_log"); idem para os demais mapeamentos de R6.
- **CA6 (RBAC — só Dono no front)**: Dado um usuário com papel **Profissional/Recepção**, Quando abre o detalhe do paciente, Então a aba "Acessos" **não** aparece.
- **CA7 (RBAC — só Dono no back)**: Dado um usuário **não-Dono**, Quando chama `GET /api/pacientes/{id}/acessos` diretamente, Então recebe **403/SemAcesso** e nenhuma linha de acesso é retornada.
- **CA8 (multi-tenant)**: Dado um Dono autenticado no estabelecimento B, Quando tenta `GET /api/pacientes/{id}/acessos` de um paciente do estabelecimento A, Então recebe erro genérico ("Paciente não encontrado.") e **nenhum** acesso do estabelecimento A é retornado; nada é logado com PII.
- **CA9 (LGPD — minimização)**: Dado a resposta do endpoint, Quando inspecionada, Então cada item contém apenas `{ quem, quando, recurso, acao }` (ou `{ quem, quando, recurso, tipoAcesso }` se o rótulo for montado no front) — **sem** `usuario_id` cru, `ip_origem`, `prontuario_id`, CPF, telefone ou conteúdo clínico.
- **CA10 (LGPD — o relatório é auditado)**: Dado que o Dono abre a aba "Acessos", Quando a lista carrega, Então **uma** nova linha é inserida em `paciente_acesso_log` com `tipo_acesso = Leitura`, `usuario_id = Dono`, `paciente_id` e `estabelecimento_id` corretos — registrando que o relatório foi consultado.
- **CA11 (LGPD — export auditado)**: Dado o Dono na aba "Acessos", Quando clica em "Exportar PDF", Então o PDF é gerado **e** o acesso é auditado (linha `Leitura`), sem que o front contenha qualquer lógica de audit (o registro nasce no backend).
- **CA12 (export — conteúdo do PDF)**: Dado uma lista de acessos, Quando o Dono exporta, Então o PDF tem cabeçalho institucional Imedto (Nunito, logo), título "RELATÓRIO DE ACESSOS — LGPD", o nome do paciente no subtítulo, e uma tabela **Quem | O quê | Quando** em linguagem leiga; **sem** CPF/telefone do paciente e **sem** `usuario_id`/IP.
- **CA13 (quem removido — resiliência)**: Dado um acesso cujo `usuario_id` não resolve em `usuarios` (usuário removido/anonimizado), Quando a linha é exibida, Então mostra "Usuário removido" (rótulo neutro) e **não** vaza o `usuario_id` cru nem quebra a renderização.
- **CA14 (estado vazio)**: Dado um paciente sem nenhum acesso registrado, Quando a aba carrega, Então exibe `AppEmptyState` "Nenhum acesso registrado" com a descrição apropriada; o botão "Exportar PDF" fica desabilitado ou gera um PDF com tabela vazia (dev decide; preferir desabilitar quando `total = 0`).
- **CA15 (LGPD — erro genérico)**: Dado um erro 422/500 do backend ao listar ou exportar, Quando o front trata o erro, Então a mensagem é genérica e **não** contém PII (nome do paciente, conteúdo clínico).
- **CA16 (read-only)**: Dado a aba "Acessos", Quando renderizada, Então **não** há nenhuma ação de criar/editar/excluir acesso — apenas visualizar e exportar.
- **CA17 (regressão — abas existentes)**: Dado as abas atuais do detalhe do paciente (incluindo "Documentos"), Quando "Acessos" é adicionada, Então a ordem e os lazy-loads das demais abas continuam inalterados.
- **CA18 (performance)**: Dado um paciente com 1.000 acessos, Quando a aba carrega, Então a 1ª requisição traz só 20 registros (paginada), a consulta usa os índices `(paciente_id, ocorrido_em)` / `(prontuario_id, ocorrido_em)` (sem full scan), e a tela responde sem travar.

## 8. Riscos e dependências

- **JOIN prontuário↔acesso**: `prontuario_acesso_log` é keyado por `prontuario_id`; o relatório é por `paciente_id`. O sub-select precisa mapear `prontuarios.paciente_id = @PacienteId` → `prontuario_id` e então puxar o log. Validar que esse mapeamento é eficiente (1:1, índice em `prontuarios.paciente_id`). É o único ponto onde pode faltar índice.
- **UNION de duas tabelas com colunas diferentes**: `paciente_acesso_log` tem `ip_origem`; `prontuario_acesso_log` não; os enums de `tipo_acesso` são **diferentes** (valores distintos). A query precisa normalizar (projetar `recurso` literal por sub-select + um `acao_raw` por sub-select, montando o rótulo leigo de forma consistente). Risco de erro de tipo/coluna no Dapper — `imedto-database` valida contra o schema real. (Mesma classe de risco do endpoint de Documentos, que já foi resolvida lá — reusar a abordagem.)
- **Paginação sobre UNION**: cuidado com `ORDER BY ocorrido_em DESC` + `LIMIT/OFFSET` sobre o conjunto unificado (não paginar cada tabela). Validar borda entre páginas (CA4/CA3).
- **Volume do audit**: o audit trail cresce indefinidamente (append-only, sem retenção configurada). Um paciente muito acessado pode ter milhares de linhas. A paginação protege a tela; o PDF "tudo" pode ficar grande — ver §11 (período no export) para mitigar.
- **Export auditado vira meta-acesso**: cada vez que o Dono abre/exporta o relatório, gera uma linha de `Leitura` — ou seja, o relatório passa a conter os próprios acessos a ele. Isso é **correto** (acesso a dado sobre acessos é acesso) e esperado; QA não deve tratar como bug. Apenas garantir que é **1 linha por carga**, não loop.
- **Área regressiva — gravação de audit**: **não** alterar os serviços de gravação (`IPacienteAcessoLogService`, `IProntuarioAcessoLogService`) nem os pontos que já gravam. Esta demanda só **lê** + grava o meta-acesso do próprio relatório (reusando o service existente).

## 9. Observações para execução

**Não-negociável**:
- Read-only: zero CRUD sobre audit (CA16).
- Multi-tenant nas duas sub-consultas + validação de paciente no tenant (R2/CA8).
- Gate **Dono** no back (atributo de papel) e no front (render condicional) — **sem** criar ação RBAC nova (R3/CA6/CA7).
- O relatório e o export **auditam** o acesso, **1 por carga**, no backend (R4/R5/CA10/CA11).
- Minimização: DTO sem `usuario_id`/IP/`prontuario_id`/conteúdo clínico (R7/CA9).
- Linguagem leiga via mapa canônico **único** (R6/CA5).
- PDF institucional via `usePdfHeader`/padrão `useRelatorioPdf`, sem CPF/telefone do paciente (CA12).

**Liberdade técnica (dev decide)**:
- Onde montar o rótulo leigo (back no DTO ou front a partir de `{recurso, tipoAcesso}`) — desde que consistente entre lista e PDF e sem duplicar o mapa.
- Endpoint: novo controller ou action nova em `PacienteController`/controller de paciente — o que for coerente com a base (o endpoint de Documentos é o vizinho a imitar).
- Posição/ícone da aba.
- Estratégia do export (mesmo endpoint com modo "tudo"/período vs reusar a paginação) — ver §11.
- Componente de linha: reusar o card de lista da aba Documentos.

**Reuso obrigatório (grep antes de criar)**:
- Backend: padrão de `ListarDocumentosDoPacienteQueryHandler` (validação tenant + audit + UNION paginado). `IPacienteAcessoLogService.RegistrarAsync`. `IPacienteRepository.ObterPorIdOuNulo`.
- Front: estrutura da aba "Documentos" em `PacienteDetalheView.vue` (lazy-load, paginação, estados). `useRelatorioPdf`/`usePdfHeader` para o PDF. `AppPagination`, `AppEmptyState`, `AppButton`.

**Aciona `imedto-database`**: **apenas para confirmar índices** (§5) — esperado: nada a criar, exceto possivelmente índice em `prontuarios.paciente_id` se faltar. Schema das tabelas de audit **não** muda. Se nada faltar, registrar a confirmação e seguir sem migration.

## 10. Atualização de documentação

- **`Docs/LGPD.md`** — adicionar uma subseção curta "**Relatório de acessos ao titular (Art. 9º/18)**" documentando: o endpoint `GET /api/pacientes/{id}/acessos` consolida `paciente_acesso_log` + `prontuario_acesso_log` em linguagem leiga; é **gate Dono**; **o próprio acesso ao relatório é auditado** (`Leitura`); o DTO é minimizado (sem `usuario_id`/IP/`prontuario_id`); o PDF traz só o nome do paciente (sem CPF/telefone). Reforçar que nenhum **novo** dado pessoal é coletado — apenas exposto. Mudança incremental.
- **`Docs/ARQUITETURA.md`** — **somente se** o padrão de "query agregada multi-tabela de leitura" ainda não tiver sido documentado pelo briefing 2026-06-09_009 (Documentos). Se já estiver lá, apenas citar este endpoint como segundo exemplo numa frase; **não** reescrever.
- **`Docs/DESIGN.md`** — atualizar **somente se** nascer componente de DS novo. Reusando a estrutura da aba Documentos e `useRelatorioPdf`, **não** deve nascer; se o dev criar algo reutilizável (ex.: `AppAuditTimeline`), atualizar e o QA valida.

## 11. Premissas dos pontos abertos (resolvidas pelo BA; usuário pode corrigir via addendum antes do dev)

1. **Escopo das fontes** = `paciente_acesso_log` + `prontuario_acesso_log` (acesso de **equipe** a dados do paciente). **Excluídos** `termo_emitido_acesso_log` e `agendamento_confirmacao_acesso_log` (acesso público do próprio titular via token, sem `usuario_id`). Se o usuário quiser "o paciente acessou via link" no relatório, vira addendum (precisa de coluna/rótulo distinto, pois não há `usuario_id`).
2. **RBAC** = **gate por papel Dono direto**, **sem** criar chave de ação RBAC nova (decisão fornecida). Quando/se outros papéis precisarem ver, vira addendum (aí provavelmente compensa uma ação `ver_relatorio_acessos`).
3. **Audit do próprio relatório** = reusa `paciente_acesso_log` com `tipo_acesso = Leitura`, **1 linha por carga de página** e **1 por export** (não 1 por linha exibida). Coerente com o precedente de listagem clínica.
4. **Export × paginação** = o PDF entrega o **período vigente**. **Default mais simples do MVP**: como não há filtro de período na UI (fora de escopo), o export traz **os últimos N registros** (sugestão: até um teto seguro, ex. 500 mais recentes, com nota no rodapé se houver mais) **ou** o backend ganha um modo "tudo" limitado. Dev/db decidem o teto para não gerar PDF gigante. Se o usuário quiser "relatório por intervalo de datas", isso adiciona o filtro de período (hoje fora de escopo) e vira addendum.
5. **Tamanho de página** = **20** (alinhado ao endpoint de Documentos).
6. **Nome do paciente no PDF** = incluído no cabeçalho (entrega ao próprio titular — não é vazamento). CPF/telefone **não** entram (minimização).
