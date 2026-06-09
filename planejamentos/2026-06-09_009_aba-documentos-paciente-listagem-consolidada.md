# Aba "Documentos" no detalhe do paciente — listagem consolidada (receitas, atestados, pedidos de exame)

**ID**: 2026-06-09_009
**Status**: Aprovado por usuário em 2026-06-09 (decisões 1-3 fechadas pelo orquestrador; pontos abertos resolvidos com premissas explícitas — ver §11)
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (leitura), permissionamento, relatório (PDF). Não toca: orçamento, estoque, escrita de receita/atestado/pedido.

## 1. Contexto e motivação

Hoje, para ver os documentos clínicos finalizados de um paciente (receita, atestado, pedido de exame), o usuário precisa abrir a página de prontuário e navegar entre abas internas — cada tipo de documento vive numa aba separada (`ReceitasPacienteTab`, `AtestadoTab`, `PedidoExameTab`), todas com CRUD. A dor relatada pelo usuário: *"facilitar a visualização sem precisar abrir a página de prontuário"*.

A demanda cria uma aba **somente-leitura** "Documentos" no detalhe do paciente (`PacienteDetalheView.vue`) que consolida os três tipos numa **lista única, paginada no servidor, com filtros**, e permite visualizar/baixar o PDF de cada documento reusando os composables de PDF já existentes.

## 2. Persona-alvo

- **Recepção / Profissional / Dono / Admin** consultando o paciente fora do contexto de atendimento (ex.: paciente liga pedindo 2ª via de receita; recepção precisa reenviar atestado).
- Momento da jornada: **pós-consulta / retorno** — o documento já foi emitido; o usuário só precisa relê-lo ou reentregá-lo.
- Frequência: média-alta na recepção; baixa no profissional (que usa o prontuário direto).

## 3. Escopo

**Inclui**:
- Nova aba **"Documentos"** em `PacienteDetalheView.vue`, posicionada **antes da aba "Anexos"** (ordem: …Termos → **Documentos** → Anexos).
- Endpoint agregado novo no backend: `GET /api/pacientes/{pacienteId}/documentos` com filtro `tipo`, filtro de período (`dataInicio`/`dataFim`), `pagina`, `tamanho`. Faz UNION das três tabelas, ordena por data desc, pagina server-side, retorna DTO de resumo unificado.
- Listagem unificada no front: linha por documento, badge de tipo, título descritivo, data, profissional. Ações por linha: **Visualizar** (abre PDF em nova aba) e **Baixar** (download do PDF).
- Reuso dos três composables de PDF existentes (`useReceitaPdf`, `useAtestadoPdf`, `usePedidoExamePdf`) — ao acionar ver/baixar, o front busca o documento COMPLETO via `obter(id)` do service do tipo correspondente e gera o PDF.
- Lazy-load da aba (só carrega a 1ª página ao clicar na aba pela 1ª vez).
- Estados: loading, erro, vazio (AppEmptyState), paginação.

**Não inclui** (explicitamente fora):
- Qualquer **CRUD** na aba: emitir, editar, cancelar, duplicar continuam exclusivamente dentro do prontuário. A aba é read-only.
- Rascunhos e documentos cancelados/substituídos. Só documentos **finalizados/emitidos** aparecem.
- Anexos (continuam na própria aba "Anexos") e termos (aba própria).
- Busca textual livre por conteúdo do documento (medicamento, CID). Backlog separado se solicitado.
- Filtro por profissional. Backlog separado se solicitado.

## 4. Regras de negócio

- **R1 — Só documentos finalizados/emitidos**. Receitas: somente `status = 'Emitida'`. Atestados e pedidos de exame **não possuem coluna de status** no schema atual — são criados já emitidos (`criado_em`), portanto todos os existentes entram. Mora em: **Query/Dapper** (filtro `WHERE` na query agregada). Não há trava de front correspondente porque o front nunca recebe rascunho/cancelado deste endpoint — fonte da verdade é o backend.
  - **Nota de descoberta**: a listagem de receitas no prontuário (`ReceitaQueryRepository.ListarDoPaciente`) traz `Rascunho` e ordena rascunhos no topo. A aba Documentos NÃO reusa essa query — usa a query agregada nova que filtra `status = 'Emitida'` para receitas.
- **R2 — Multi-tenant**. A query agregada filtra `estabelecimento_id = @EstabelecimentoId` em **todas as três** sub-consultas (receitas, atestados, pedidos). Validação prévia de existência do paciente no tenant (`IPacienteRepository.ObterPorIdOuNulo(pacienteId, estabelecimentoId)` → `BusinessException("Paciente não encontrado.")` se ausente), seguindo o padrão de `ListarReceitasDoPacienteQueryHandlers`. Mora em: **Handler + Query**. Repositório falha-fechada: sem tenant claim → vazio/throws.
- **R3 — Ordenação**. Resultado unificado ordenado por **data desc** (receita: `COALESCE(emitida_em, criada_em)`; atestado/pedido: `criado_em`), com paginação aplicada sobre o conjunto unificado (não 3 paginações separadas). Mora em: **Query/Dapper** (UNION ALL + ORDER BY + LIMIT/OFFSET).
- **R4 — Audit LGPD na listagem**. Listar documentos clínicos é leitura de PII (medicação, CID, afastamento). Seguindo o precedente de `ListarReceitasDoPacienteQueryHandlers`, **a listagem registra acesso ao prontuário** (`IProntuarioAcessoLogService.RegistrarAsync(prontuarioId, solicitanteUsuarioId, estabelecimentoId, TipoAcessoProntuario.Leitura)`) quando existe prontuário para o paciente. Mora em: **Handler**.
- **R5 — Audit LGPD ao abrir documento completo**. Ao Visualizar/Baixar, o front chama o `obter(id)` do service do tipo. Esses endpoints já existem e já passam `SolicitanteUsuarioId` ao backend (ver `AtestadoController`/`PedidoExameController`/`ReceitaController`). **Não introduzir audit novo no front** — o registro de acesso ao documento completo é responsabilidade do handler do `obter`, que já é a fonte da verdade. Mora em: **backend já existente** (sem mudança).
- **R6 — RBAC**. A aba é visível/acionável para todos os papéis que já têm acesso à página de detalhe do paciente (mesma permissão da aba Prontuário/Anexos). **Não há restrição de papel adicional** específica para "Documentos" — quem entra no detalhe do paciente já passou pelo gate. Não replicar a regra de "autor da evolução ou Dono" que existe para evoluções (R1 do briefing 2026-05-25_001): documento emitido é entregue ao paciente, sua 2ª via é operação de recepção. Mora em: **rota/guard já existente**; sem espelho novo.
- **R7 — Conteúdo completo só sob demanda**. A listagem retorna **apenas resumo** (sem itens de receita, sem texto de atestado, sem lista de exames) — minimização LGPD. O documento completo só é buscado no clique de ver/baixar. Mora em: **DTO de resumo** (back) + **fluxo do front**.

## 5. Modelo de dados

**Schema NÃO muda** — todas as tabelas já existem:
- `public.receitas` — colunas usadas: `id, paciente_id, estabelecimento_id, tipo, status, emitida_em, criada_em, profissional_usuario_id`.
- `public.atestados` — `id, paciente_id, estabelecimento_id, tipo, criado_em, profissional_usuario_id`.
- `public.pedidos_exame` — `id, paciente_id, estabelecimento_id, tipo, criado_em, profissional_usuario_id`.
- `public.usuarios` — LEFT JOIN para `profissionalNome`.

**Índices** — avaliar com `imedto-database`:
- A query agregada filtra por `(paciente_id, estabelecimento_id)` e ordena por data nas três tabelas. As listagens por paciente já existem hoje (`ListarDoPaciente` de cada repo), então **provavelmente os índices `(estabelecimento_id, paciente_id)` já existem**. **Ação para o dev**: ao acionar `imedto-database`, confirmar via inspeção do schema se existe índice cobrindo `(estabelecimento_id, paciente_id, <coluna_de_data>)` em cada tabela; criar migration idempotente apenas se faltar. Não criar índice especulativo se já houver cobertura.

**DTO unificado de resumo** (novo, em `Imedto.Backend.Contracts`):
```
DocumentoResumoDto {
    Tipo: string            // "Receita" | "Atestado" | "PedidoExame"
    Id: long                // id na tabela de origem
    Titulo: string          // descrição derivada (ver §6 — montada no back ou no front; ver R7)
    Data: DateTime          // emitida_em/criado_em conforme tipo
    ProfissionalNome: string?
}
PaginaDocumentosDto { Itens: DocumentoResumoDto[]; Total: int; Pagina: int; TamanhoPagina: int }
```

**Sem novo audit table** — reusa `IProntuarioAcessoLogService` existente (R4).

## 6. UX e fluxo

**Aba "Documentos"** (entre Termos e Anexos):
- Ícone sugerido: `fa-solid fa-file-lines` ou `fa-solid fa-file-prescription` (dev escolhe coerente com o set existente). Badge com a contagem total quando > 0 (mesmo padrão de Orçamentos/Anexos).

**Cabeçalho da seção** (`prontuario-head` reutilizado):
- `<h2>` "Documentos do paciente" + `<p>` "Receitas, atestados e pedidos de exame emitidos. Apenas visualização."

**Barra de filtros** (acima da lista):
- Filtro de **tipo**: segmented/select com `Todos | Receitas | Atestados | Pedidos de exame`. Default `Todos`.
- Filtro de **período**: dois campos de data (`De` / `Até`), opcionais. Reusar componentes de input de data do design system existentes (dev confirma `AppField`/input date em uso na base).
- Mudar filtro reseta para página 1 e refaz a busca server-side.

**Lista unificada** (linha por documento — pode reusar o padrão visual de `budget-card`/`att-card` existente na própria view, ou um card de lista mais enxuto):
- Coluna 1: ícone + **badge de tipo** colorido (Receita / Atestado / Pedido de exame).
- Coluna 2: **título** descritivo do documento. Sugestão por tipo (texto derivado, sem PII clínica):
  - Receita → `Receita {tipo}` (ex.: "Receita Controlada").
  - Atestado → `Atestado de {tipo}` (ex.: "Atestado de Afastamento").
  - Pedido de exame → `Pedido de exame {tipo}` (ex.: "Pedido de exame Laboratorial").
  - O `tipo` da tabela NÃO é PII (não revela diagnóstico). Pode ir no resumo.
- Coluna 3: **data** (`fmtData`) + **profissional** (`profissionalNome`).
- Coluna 4: ações **Visualizar** (`fa-eye`) e **Baixar** (`fa-download`).

**Ação Visualizar** (modo "visualizar"):
- Reusar o padrão já existente em `PacienteDetalheView.exportarPdfEvolucao`: abrir `window.open("about:blank", "_blank")` SINCRONICAMENTE ao clique (evita popup blocker), buscar o documento completo via `obter(id)`, gerar PDF com o composable do tipo, apontar a janela para o `blobUrl`. Popup bloqueado → fallback download + toast informativo.

**Ação Baixar** (modo "download"):
- Buscar documento completo via `obter(id)`, gerar PDF em modo "download" pelo composable do tipo.

**Estados**:
- **Loading**: `<p class="msg-info">Carregando…</p>` (padrão da view) enquanto carrega a página.
- **Loading por linha**: indicador de "gerando PDF" na linha acionada (espelhar `evolucaoSendoBaixada` → `documentoSendoBaixado`), desabilitando o botão durante a geração.
- **Erro de listagem**: mensagem genérica de erro (toast ou `msg-erro`), sem PII.
- **Erro ao gerar PDF**: toast com mensagem genérica (`e?.response?.data?.mensagem ?? "Erro ao gerar documento."`).
- **Vazio**: `AppEmptyState` (ícone 📄, título "Nenhum documento emitido", descrição "Receitas, atestados e pedidos de exame emitidos aparecem aqui. Emita pelo prontuário do paciente."). Quando há filtro ativo e zero resultados: variação "Nenhum documento para o filtro selecionado."

**Paginação**:
- Reusar componente de paginação do design system (dev confirma `AppPagination` em `components/ui/`). Tamanho de página default **20** (alinhado ao default do handler de receitas). Controles de página chamam o endpoint com `pagina` atualizada.

**Mobile-ready**: lista colapsa em coluna única (mesma media query da view, `max-width: 1200px`); ações ficam acessíveis (não esconder atrás de hover-only).

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — listagem)**: Dado um paciente com 2 receitas emitidas, 1 atestado e 1 pedido de exame, Quando o usuário clica na aba "Documentos", Então a lista exibe 4 linhas ordenadas por data desc, cada uma com badge de tipo, título, data e profissional, e a contagem total no badge da aba é 4.
- **CA2 (lazy-load)**: Dado que o usuário abre o detalhe do paciente na aba "Resumo", Quando ele NÃO clica na aba "Documentos", Então nenhuma requisição a `/api/pacientes/{id}/documentos` é disparada; e ao clicar na aba pela 1ª vez, exatamente uma requisição da página 1 é disparada; clicar de novo na mesma aba não refaz a busca.
- **CA3 (somente emitidos)**: Dado um paciente com 1 receita `Rascunho`, 1 receita `Cancelada` e 1 receita `Emitida`, Quando a aba Documentos carrega, Então apenas a receita `Emitida` aparece (rascunho e cancelada não aparecem).
- **CA4 (filtro por tipo, server-side)**: Dado a lista carregada com os 3 tipos, Quando o usuário seleciona o filtro "Receitas", Então a requisição inclui `tipo=Receita`, a página volta para 1 e a lista mostra apenas receitas emitidas; selecionar "Todos" volta a trazer os três tipos.
- **CA5 (filtro por período, server-side)**: Dado documentos em datas variadas, Quando o usuário informa `De`/`Até`, Então a requisição inclui `dataInicio`/`dataFim` e a lista traz apenas documentos no intervalo (inclusivo); limpar o período remove os parâmetros e refaz a busca.
- **CA6 (paginação server-side)**: Dado um paciente com 45 documentos emitidos, Quando a aba carrega, Então a 1ª requisição traz `tamanho=20` e `total=45`; ao ir para a página 2, nova requisição com `pagina=2` traz os próximos 20; a ordenação desc é contínua entre páginas (sem duplicar nem pular registro na borda).
- **CA7 (visualizar PDF)**: Dado uma receita emitida na lista, Quando o usuário clica em "Visualizar", Então uma nova aba é aberta sincronicamente ao clique, o documento completo é buscado via `receitaService.obter(id)`, o PDF é gerado por `useReceitaPdf` e exibido na aba; durante a geração o botão da linha fica desabilitado.
- **CA8 (baixar PDF por tipo)**: Dado um atestado e um pedido de exame na lista, Quando o usuário clica em "Baixar" em cada um, Então o documento completo é buscado via o `obter(id)` do service correto e o PDF é gerado em modo "download" pelo composable correspondente (`useAtestadoPdf` / `usePedidoExamePdf`) — cada tipo usa seu próprio composable, sem cruzar.
- **CA9 (popup bloqueado)**: Dado que o navegador bloqueia pop-ups, Quando o usuário clica em "Visualizar", Então o sistema cai no fallback de download e exibe toast "Permita pop-ups para visualizar o PDF. Baixando como alternativa."
- **CA10 (multi-tenant)**: Dado um usuário autenticado no estabelecimento B, Quando ele tenta acessar `/api/pacientes/{id}/documentos` de um paciente do estabelecimento A, Então recebe erro genérico ("Paciente não encontrado.") e nenhum documento do estabelecimento A é retornado; nada é logado com PII.
- **CA11 (LGPD — minimização na listagem)**: Dado a resposta de `/api/pacientes/{id}/documentos`, Quando inspecionada, Então cada item contém apenas `{ tipo, id, titulo, data, profissionalNome }` — sem itens de receita, sem texto de atestado, sem lista de exames, sem CID, sem diagnóstico.
- **CA12 (LGPD — audit na listagem)**: Dado um paciente que possui prontuário, Quando a aba Documentos carrega a lista, Então é registrada uma linha de acesso ao prontuário com `{ prontuarioId, solicitanteUsuarioId, estabelecimentoId, TipoAcessoProntuario.Leitura }`.
- **CA13 (LGPD — mensagem de erro genérica)**: Dado um erro 422/500 do backend ao listar ou ao obter um documento, Quando o front trata o erro, Então a mensagem exibida é genérica e não contém PII (nome de medicamento, CID, diagnóstico).
- **CA14 (estado vazio)**: Dado um paciente sem nenhum documento emitido, Quando a aba carrega, Então exibe `AppEmptyState` com título "Nenhum documento emitido" e a descrição apontando para emitir pelo prontuário; o badge de contagem da aba não aparece.
- **CA15 (vazio com filtro)**: Dado um paciente com receitas mas o usuário filtra por "Pedidos de exame" e não há nenhum, Quando a lista atualiza, Então exibe estado vazio "Nenhum documento para o filtro selecionado." (sem some-zerar a contagem total da aba).
- **CA16 (read-only)**: Dado a aba Documentos, Quando renderizada, Então não há nenhum botão/ação de emitir, editar, cancelar ou duplicar documento — apenas Visualizar e Baixar.
- **CA17 (regressão — abas existentes)**: Dado as abas atuais (Resumo, Prontuário, Anamnese, Orçamentos, Financeiro, Convênios, Termos, Anexos), Quando a aba Documentos é adicionada antes de Anexos, Então a ordem das demais abas e seus lazy-loads continuam funcionando inalterados, e Anexos continua imediatamente após Documentos.

## 8. Riscos e dependências

- **Dependência não commitada**: `useReceitaPdf.ts` foi criado nesta sessão e ainda não foi commitado. O dev deve confirmar que ele está presente/commitado antes de depender dele; se não estiver, sinalizar ao orquestrador.
- **UNION de tipos com schemas diferentes**: atestados/pedidos não têm `status`; a query precisa normalizar colunas (ex.: projetar literal de tipo e a coluna de data correta por sub-select). Risco de erro de tipo/coluna no Dapper — `imedto-database` valida a query contra o schema real.
- **Paginação sobre UNION**: cuidado com `ORDER BY` + `LIMIT/OFFSET` sobre o conjunto unificado (não paginar cada tabela isoladamente). Validar borda entre páginas (CA6).
- **Performance**: três sub-consultas + COUNT. Confirmar índices (§5) antes de assumir custo baixo. Sem índice adequado, ordenação por data sobre UNION pode degradar com volume.
- **Audit em massa**: a listagem registra 1 acesso de leitura por carga de página (R4/CA12) — coerente com o precedente de receitas; não registrar 1 por documento.
- **Área regressiva — prontuário**: não alterar as abas internas de prontuário nem suas queries (`ReceitaQueryRepository.ListarDoPaciente` continua trazendo rascunhos para o contexto do prontuário). A query agregada é nova e separada.

## 9. Observações para execução

**Não-negociável**:
- Read-only: zero CRUD na aba (CA16).
- Filtro `status = 'Emitida'` para receitas na query agregada (R1/CA3).
- Multi-tenant nas três sub-consultas + validação de paciente no tenant (R2/CA10).
- Audit de leitura na listagem reusando `IProntuarioAcessoLogService` (R4/CA12).
- Minimização: DTO de resumo sem PII clínica (R7/CA11).
- Cada tipo usa seu próprio composable de PDF (CA8) — reuso, sem duplicar lógica de PDF.
- Padrão de `window.open` síncrono para visualizar (copiar o comportamento de `exportarPdfEvolucao`, incluindo o comentário sobre não usar `noopener`).

**Liberdade técnica (dev decide)**:
- Onde montar o `titulo` (back no SELECT ou front a partir de `tipo`) — desde que sem PII e consistente.
- Endpoint agregado: novo controller `DocumentoController` OU action nova em controller existente — o que for mais coerente com o padrão da base. Query handler novo + Dapper query repository novo (ou método novo em repo existente).
- Componente de linha: reusar card existente da view ou criar item de lista enxuto; manter tokens tipográficos (CLAUDE.md §5 — sem `font-size`/`font-weight` literais).
- Componente de paginação e de filtro de data: confirmar e reusar os do design system (`components/ui/`).

**Reuso obrigatório (grep antes de criar)**:
- Composables de PDF: `useReceitaPdf`, `useAtestadoPdf`, `usePedidoExamePdf`.
- Services de leitura: `receitaService.obter`, `atestadoService.obter`, `pedidoExameService.obter`.
- Padrão de visualização/popup: `PacienteDetalheView.exportarPdfEvolucao`.
- AppEmptyState, AppButton, AppToast, AppPagination (se existir), AppField.
- Backend: padrão de `ListarReceitasDoPacienteQueryHandlers` (validação tenant + audit).

**Aciona `imedto-database`**: somente para confirmar/criar índice (§5). Schema das tabelas não muda.

## 10. Atualização de documentação

- **`Docs/ARQUITETURA.md`** — adicionar, na seção de leitura/CQRS (read-side Dapper), uma nota curta sobre o **padrão de query agregada multi-tabela** (UNION paginado server-side com audit de leitura reusado), citando `GET /api/pacientes/{id}/documentos` como exemplo de endpoint de leitura que cruza três aggregates somente para consolidação read-only. É um padrão novo (até aqui as queries de leitura eram por aggregate único). Mudança incremental, cirúrgica — não reescrever o doc.
- **`Docs/LGPD.md`** — sem alteração estrutural: a regra (audit de leitura na listagem clínica, minimização do DTO) já está documentada e este endpoint apenas a aplica. Não atualizar.
- **`Docs/DESIGN.md`** — atualizar **somente se** o dev criar um componente novo de design system (ex.: item de lista de documento ou filtro de período reutilizável). Se reusar componentes existentes, não atualizar. Decisão fica com o dev no momento da implementação; o QA valida que, se nasceu componente novo, o doc foi atualizado.

## 11. Premissas dos pontos abertos (resolvidas pelo BA; usuário pode corrigir antes do dev)

Os quatro pontos abertos foram fechados com defaults de domínio, grounded no código. Se o usuário discordar de qualquer um, vira addendum antes da implementação:

1. **Filtros** = por **tipo** (Todos/Receitas/Atestados/Pedidos) + por **período** (De/Até). Sem filtro por profissional ou busca textual (backlog).
2. **Colunas por linha** = badge de tipo + título descritivo + data + profissional + ações (Visualizar/Baixar). **Sem badge de status** — como só "Emitida" aparece, status seria sempre o mesmo e é ruído.
3. **Tamanho de página default** = **20** (igual ao default do handler de receitas no backend).
4. **Empty state** = dois textos: sem documentos ("Nenhum documento emitido"…) e sem resultado de filtro ("Nenhum documento para o filtro selecionado.").
