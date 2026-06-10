# PDF oficial de receita gerado no servidor (destravar download do documento auditável)

**ID**: 2026-06-10_001
**Status**: Aprovado por usuário em 2026-06-10 (execução autônoma — decisões de produto fornecidas pelo orquestrador; ambiguidades residuais resolvidas no §4 "Decisões e assunções")
**Autor**: imedto-business-analyst
**Estimativa de esforço**: P
**Áreas regressivas tocadas**: prontuário (leitura/exportação de receita), permissionamento (gate de receita), relatório (PDF). Não toca: escrita/emissão de receita, assinatura digital BirdID, financeiro, estoque.

> **Nota de descoberta crítica (mudou o escopo original)**: a investigação revelou que o endpoint `GET /api/receitas/{id}/pdf` **já está implementado e em produção** — não retorna mais 501. Ele é servido por `IReceitaPdfService` → `QuestPdfReceitaService` (registrado em DI, `Container.cs:518`), que já reproduz a identidade institucional (Nunito embarcada no assembly, cabeçalho com logo do estabelecimento, bloco do paciente, lista numerada, rodapé, marca d'água por status), já filtra `estabelecimento_id` (multi-tenant) e já lê `assinatura_digital_status`. O front `receitaService.baixarPdf(id)` também já existe. **Portanto este briefing NÃO é "destravar um 501" — é fechar as 3 lacunas reais que restam** para o PDF do servidor ser o documento oficial conforme a decisão de produto. Ver §3.

## 1. Contexto e motivação

O item 1.1 da [FASE_1_COMPLETUDE](../Docs/Roadmap/FASE_1_COMPLETUDE.md) pede que o PDF de receita seja o **documento oficial**: consistente, auditável e independente do browser. Hoje o produto tem dois geradores:

- **Servidor** (`QuestPdfReceitaService`): já gera o PDF institucional, mas **(a)** não registra a exportação no audit LGPD e **(b)** gera PDF até para receita em `Rascunho` (apenas com marca d'água), o que contradiz a regra clínica de "documento só de receita emitida".
- **Front** (`useReceitaPdf` via jsPDF): usado em "Imprimir" na aba de receitas do prontuário, como visualização rápida.

Além disso, **(c)** não existe na UI um botão que baixe o PDF oficial do servidor para uma receita comum (não assinada digitalmente). O único botão que chama o backend para PDF é "Baixar PDF assinado", que pertence ao fluxo BirdID (`assinaturaDigitalService.obterStatus` → `pdfAssinadoUrl`) e só aparece quando `assinaturaDigitalStatus === 'AssinadaIcp'`. A consequência prática é que a "fonte oficial auditável" existe no backend mas o usuário não tem como acessá-la no caminho comum.

A demanda fecha essas três lacunas, mantendo o jsPDF do front como visualização rápida (não remover) e desenhando o caminho para que, quando a assinatura ICP existir (item 1.3), o **mesmo endpoint** passe a servir o arquivo assinado armazenado.

## 2. Persona-alvo

- **Profissional / Dono** (papéis com acesso a receita — `Recepcionista` não emite nem visualiza receita, conforme `[RequiresPapel(Profissional, Dono)]` no `ReceitaController`).
- Momento da jornada: **pós-emissão** — a receita já está `Emitida`; o profissional quer o documento oficial para entregar/imprimir/arquivar, ou para reimprimir uma 2ª via auditável.
- Frequência: alta no profissional que prescreve.

## 3. Escopo

**Inclui**:
- **Lacuna (a) — Audit LGPD na exportação**: `GerarAsync` do `QuestPdfReceitaService` passa a registrar a exportação seguindo o padrão existente de `RegistrarExportacaoEvolucaoCommandHandler` (`IProntuarioAcessoLogService.RegistrarAsync(..., TipoAcessoProntuario.Exportacao)`), quando o paciente da receita possuir prontuário. Medicação é PII clínico — baixar o PDF é exportação de PII e precisa de trilha.
- **Lacuna (b) — Rascunho → 422**: o backend rejeita a geração de PDF para receita em `Rascunho` com `BusinessException` (vira 422), porque PDF é documento; rascunho não é documento. Receita `Cancelada` e `Substituida` continuam gerando PDF **com a marca d'água correspondente** (já implementado — "CANCELADA"/"SUBSTITUÍDA").
- **Lacuna (c) — Botão de download do PDF oficial no front**: na aba de receitas do prontuário (`ReceitasPacienteTab.vue`), receita `Emitida` ganha uma ação **"Baixar PDF oficial"** que consome `receitaService.baixarPdf(id)` (blob) e dispara o download. Distinto e adicional ao "Imprimir" (jsPDF, visualização rápida) e ao "Baixar PDF assinado" (fluxo BirdID).
- **Preparação para assinatura (design, sem implementar)**: documentar no código/briefing que, quando `assinatura_digital_status` indicar assinatura ativa (`AssinadaIcp`/`AssinadaMemed`), o endpoint `GET /api/receitas/{id}/pdf` deve passar a **servir o arquivo assinado armazenado** em vez de regerar o não assinado. Nesta entrega o endpoint continua gerando o PDF **não assinado** (não há assinatura ativa no ambiente). Apenas deixar o ponto de extensão claro — não construir o fetch do arquivo assinado agora.

**Não inclui** (explicitamente fora):
- Remover ou alterar o `useReceitaPdf` (jsPDF) do front — permanece como visualização rápida ("Imprimir").
- Implementar a leitura/entrega do PDF assinado armazenado (item 1.3 — assinatura ICP). Apenas prever o ponto de extensão.
- Redesenhar o layout do PDF do servidor — ele já espelha a identidade institucional. Ajustes visuais só se um CA falhar.
- Qualquer mudança de schema (validado: ver §5).
- Mudar o fluxo "Baixar PDF assinado" (BirdID) já existente.
- Botão de PDF oficial na nova aba "Documentos" (briefing 2026-06-09_009) — aquela aba já reusa `useReceitaPdf`; trocá-la para o PDF do servidor é backlog separado se desejado.

## 4. Decisões e assunções (execução autônoma)

Decisões fornecidas pelo orquestrador, complementadas por defaults grounded no código onde havia ambiguidade:

1. **PDF do servidor = documento oficial; jsPDF = visualização rápida.** Ambos coexistem. (Fornecida.)
2. **Sem assinatura ICP ativa, o endpoint serve o PDF oficial NÃO assinado**; o design prevê troca para o arquivo assinado quando a assinatura existir. (Fornecida.)
3. **Cancelada → marca d'água "CANCELADA"** (já implementado); **Rascunho → 422.** (Fornecida.)
4. **Download registra exportação no audit LGPD** seguindo o padrão `registrar-exportacao` do prontuário. (Fornecida.)
5. **Mantém `FeatureGate("receitas")` + `[RequiresAcao("prescricao")]` + `[RequiresPapel(Profissional, Dono)]` + filtro multi-tenant.** (Fornecida — já presentes no controller; não remover.)
6. **Front: botão baixa o blob de `receitaService.baixarPdf`** (já existe). (Fornecida.)
7. **[Assunção] Onde mora o audit de exportação**: dentro do `QuestPdfReceitaService.GerarAsync` (após carregar os dados, antes/depois de gerar os bytes), reusando `IProntuarioAcessoLogService`. Alternativa rejeitada: criar um command handler `RegistrarExportacaoReceitaCommand` separado e o controller orquestrar dois passos — rejeitada por acoplar o controller a duas chamadas e divergir do fato de que o serviço já carrega `paciente_id`. **Liberdade do dev**: se preferir extrair um helper/command para manter o serviço de PDF "puro" (sem dependência de audit), pode — desde que o efeito observável (1 linha de audit `Exportacao` por download bem-sucedido de receita emitida) seja idêntico. O `QuestPdfReceitaService` hoje carrega só `estabelecimento_id` e dados da receita; precisará também resolver o `prontuario_id` do paciente (via `IProntuarioRepository.ObterPorPaciente(paciente_id, estabelecimento_id)`) — `paciente_id` já está disponível na query do serviço (basta projetá-lo no `ReceitaRow`).
8. **[Assunção] Receita sem prontuário**: se o paciente não tiver prontuário iniciado, o PDF é gerado normalmente e **nenhuma** linha de audit de prontuário é inserida (espelha o `if (prontuario is not null)` de `ListarReceitasDoPacienteQueryHandlers`). O download não é bloqueado por falta de prontuário.
9. **[Assunção] Falha do audit não bloqueia o download**: `IProntuarioAcessoLogService` é best-effort (engole exceção + LogError). Mantém-se esse contrato — a exportação nunca falha por causa do log.
10. **[Assunção] Rótulo do botão**: "Baixar PDF oficial" (ícone `fa-solid fa-file-pdf` ou `fa-file-arrow-down` coerente com o set). Fica ao lado de "Imprimir" no rodapé de ações da receita `Emitida`. Liberdade do dev no ícone, desde que não colida visualmente com "Baixar PDF assinado".
11. **[Assunção] Nome do arquivo**: mantém o atual `receita-{id}.pdf` do controller. Não usar nome com PII (nome do paciente) no header `Content-Disposition` do servidor — minimização LGPD. (O jsPDF do front usa slug do nome; o servidor, sendo o oficial/auditável, fica com o id.)
12. **[Assunção] Marca d'água "não assinada"**: o PDF do servidor já traz no rodapé "Assine manualmente no espaço acima" quando não há assinatura digital. Não duplicar a caixa de aviso CFM do jsPDF — manter o layout do servidor como está. Ajuste só se o usuário pedir.

## 5. Modelo de dados

**Schema NÃO muda.** Validado na investigação:
- `public.receitas` — colunas já usadas pelo `QuestPdfReceitaService`: `id, tipo, tipo_notificacao, status, assinatura_digital_status, emitida_em, validade_ate, observacoes, motivo_cancelamento, estabelecimento_id, paciente_id, profissional_usuario_id, deletado_em`.
- `public.receita_itens`, `public.estabelecimentos`, `public.pacientes`, `public.usuarios`, `public.profissionais`, `public.receitas_configuracao_estabelecimento` — já lidas na query do PDF.
- `public.prontuario_acesso_log` — destino do audit de exportação (já existe; usado por `IProntuarioAcessoLogService`). A coluna `assinatura_digital_status` (varchar(20), default `'NaoAssinada'`) já existe (migration `20260514111139`).

**Ação para o dev**: a única adição na query Dapper de `CarregarDadosAsync` é projetar `r.paciente_id AS PacienteId` no `ReceitaRow` (hoje não é selecionado), para que o serviço possa resolver o prontuário. **Não cria coluna, não cria índice** — `(estabelecimento_id, paciente_id)` em `receitas` e a busca de prontuário por paciente já são cobertas por índices existentes (mesma rota de leitura de `ListarReceitasDoPacienteQueryHandlers`). Não acionar `imedto-database` salvo se a inspeção revelar ausência de índice na busca de prontuário por paciente — improvável, pois o prontuário já é lido por paciente em todo o módulo.

**Sem novo audit table** — reusa `IProntuarioAcessoLogService` + `TipoAcessoProntuario.Exportacao` (enum já existente).

## 6. UX e fluxo

**Aba de receitas do prontuário (`ReceitasPacienteTab.vue`), editor de receita `Emitida`** — rodapé de ações (`.acoes-footer`), bloco `v-else-if="receitaAberta.status === 'Emitida'"`:

Ordem proposta dos botões (mantendo os existentes):
1. "Cancelar receita" (ghost) — existente.
2. "Nova versão" (secondary, `fa-copy`) — existente.
3. "Imprimir" (`fa-print`) — existente (jsPDF, visualização rápida via `imprimir()`).
4. **"Baixar PDF oficial"** (secondary, `fa-file-pdf`) — **novo**. Chama `receitaService.baixarPdf(id)`, recebe `Blob`, cria object URL e dispara download (padrão `<a download>` + `URL.createObjectURL`/`revokeObjectURL`). Durante a chamada, o botão fica em `:loading`/`:disabled` (reusar `salvandoAcao` ou um ref dedicado `baixandoPdfOficial`).
5. Bloco BirdID ("Assinar"/"Baixar PDF assinado" etc.) — existente, inalterado.

**Estados**:
- **Loading por ação**: botão "Baixar PDF oficial" desabilitado + spinner durante o fetch do blob.
- **Erro 422 (Rascunho)**: na prática o botão só aparece em receita `Emitida`, então o 422 de Rascunho não é alcançável pela UI normal; ainda assim, se a chamada retornar erro, exibir toast genérico `e?.response?.data?.mensagem ?? "Erro ao baixar o PDF."` (sem PII).
- **Erro de rede/500**: toast genérico, sem PII.
- **Sucesso**: download inicia; opcionalmente toast discreto "PDF gerado." (dev decide; não obrigatório).

**Mobile-ready**: o `.acoes-footer` já tem `flex-wrap: wrap`; o botão novo acompanha.

**Sem mudança visual no PDF** — o documento já está no padrão institucional.

## 7. Critérios de aceite (testáveis)

- **CA1 (caminho feliz — PDF oficial de emitida)**: Dado uma receita `Emitida`, Quando o usuário clica em "Baixar PDF oficial", Então `GET /api/receitas/{id}/pdf` retorna `200` com `Content-Type: application/pdf` e o front dispara o download de um arquivo PDF válido renderizado com a identidade institucional (Nunito, logo do estabelecimento ou placeholder de iniciais, bloco do paciente, prescrição numerada, rodapé).
- **CA2 (Rascunho → 422)**: Dado uma receita em `Rascunho`, Quando `GET /api/receitas/{id}/pdf` é chamado, Então o backend responde `422` com mensagem genérica de negócio (ex.: "Receita em rascunho não pode ser exportada em PDF.") e **nenhum** byte de PDF é retornado.
- **CA3 (Cancelada → marca d'água)**: Dado uma receita `Cancelada`, Quando o PDF é gerado, Então o documento é produzido com a marca d'água "CANCELADA" e o rodapé indica "Receita cancelada." (com o motivo, se houver), conforme o comportamento já existente.
- **CA4 (audit LGPD na exportação)**: Dado uma receita `Emitida` de um paciente **que possui prontuário**, Quando o PDF é baixado com sucesso, Então é inserida **exatamente uma** linha em `prontuario_acesso_log` com `{ prontuario_id, usuario_id = solicitante, estabelecimento_id, tipo_acesso = Exportacao }`.
- **CA5 (audit — paciente sem prontuário não quebra)**: Dado uma receita `Emitida` de um paciente **sem prontuário iniciado**, Quando o PDF é baixado, Então o download conclui com `200` e **nenhuma** linha de audit de prontuário é inserida (sem erro).
- **CA6 (audit best-effort)**: Dado que a gravação do audit falhe (ex.: serviço de log indisponível), Quando o PDF é baixado, Então o usuário ainda recebe o `200` com o PDF (o download nunca é bloqueado pela falha de auditoria).
- **CA7 (multi-tenant)**: Dado um usuário autenticado no estabelecimento B, Quando chama `GET /api/receitas/{id}/pdf` para uma receita do estabelecimento A, Então recebe erro genérico ("Receita não encontrada.") e **nenhum** dado da receita de A vaza; nada é logado com PII.
- **CA8 (RBAC)**: Dado um usuário com papel `Recepcionista` (ou qualquer papel sem `prescricao`/sem `Profissional`/`Dono`), Quando tenta acessar `GET /api/receitas/{id}/pdf`, Então recebe `403` e o botão "Baixar PDF oficial" não é renderizado/alcançável no front (a aba de receitas já é restrita por papel).
- **CA9 (FeatureGate)**: Dado um estabelecimento sem a feature `receitas` habilitada, Quando o endpoint é chamado, Então o `FeatureGate` bloqueia a requisição (comportamento já existente, não regredir).
- **CA10 (LGPD — sem PII no transporte/log)**: Dado o download do PDF, Quando inspecionados o header `Content-Disposition` e os logs do servidor, Então o nome do arquivo é `receita-{id}.pdf` (sem nome/CPF do paciente) e nenhum log contém medicamento, CID, nome ou CPF.
- **CA11 (jsPDF preservado)**: Dado a aba de receitas, Quando o usuário clica em "Imprimir", Então o fluxo jsPDF (`useReceitaPdf`) continua funcionando como visualização rápida, independente do novo botão.
- **CA12 (loading da ação)**: Dado o clique em "Baixar PDF oficial", Quando o blob está sendo buscado, Então o botão fica desabilitado/com indicador de carregamento e volta ao normal ao concluir (sucesso ou erro).
- **CA13 (ponto de extensão para assinatura — documentado)**: Dado o código do endpoint/serviço, Quando revisado, Então há um ponto de extensão explícito (comentário + ramo condicional ou TODO estruturado) indicando que, com `assinatura_digital_status ∈ {AssinadaIcp, AssinadaMemed}`, o endpoint deverá servir o arquivo assinado armazenado; nesta entrega o caminho não assinado continua sendo servido (o teste apenas verifica que o ponto de extensão existe e que receita assinada ainda baixa o PDF gerado sem erro).
- **CA14 (regressão — fluxo BirdID)**: Dado uma receita com `assinaturaDigitalStatus === 'AssinadaIcp'`, Quando o usuário usa "Baixar PDF assinado", Então o fluxo BirdID (`assinaturaDigitalService.obterStatus` → `pdfAssinadoUrl`) continua funcionando inalterado e coexiste com "Baixar PDF oficial".

## 8. Riscos e dependências

- **Risco de duplicação de audit**: garantir que o audit de `Exportacao` é registrado **uma vez por download bem-sucedido**, não por carga de página nem duas vezes (uma no serviço e outra em eventual command). Validar com CA4.
- **Acoplamento do serviço de PDF a repositórios scoped**: hoje `QuestPdfReceitaService` é `Scoped` (registrado em `Container.cs:518`) e usa `AppReadConnectionString` + `IHttpClientFactory`. Adicionar `IProntuarioRepository` + `IProntuarioAcessoLogService` ao construtor é compatível com o ciclo scoped — confirmar que ambos estão registrados como scoped/disponíveis no escopo da request (estão, são usados em handlers scoped).
- **Ponto de extensão da assinatura**: não implementar o fetch do arquivo assinado agora evita dependência do item 1.3, mas o comentário/estrutura precisa ser claro para não virar dívida esquecida.
- **Área regressiva — prontuário/relatório**: não alterar o layout nem as outras chamadas de `QuestPdfReceitaService`; a única mudança de comportamento é (a) rejeitar Rascunho e (b) auditar. Validar que receita `Emitida`/`Cancelada`/`Substituida` continuam gerando.
- **Front — não confundir os dois botões de PDF**: "Baixar PDF oficial" (servidor, sempre disponível em emitida) vs "Baixar PDF assinado" (BirdID, só em `AssinadaIcp`). Rótulos e condições de exibição distintos (CA14).
- **Dependência**: nenhuma externa. Item 1.3 (assinatura ICP) é dependente *deste* design (ponto de extensão), não o contrário.

## 9. Observações para execução

**Não-negociável**:
- Rascunho → 422 no backend (CA2). É regra de negócio no serviço/handler, não trava só de front.
- Audit de `Exportacao` por download bem-sucedido de receita emitida, reusando `IProntuarioAcessoLogService` (CA4), best-effort (CA6), condicional à existência de prontuário (CA5).
- Multi-tenant e RBAC preservados (CA7/CA8/CA9) — não remover atributos do controller.
- jsPDF do front preservado (CA11).
- Sem PII no nome do arquivo do servidor nem em logs (CA10).
- Ponto de extensão da assinatura explícito (CA13).

**Liberdade técnica (dev decide)**:
- Onde exatamente registrar o audit (dentro do `GerarAsync` vs helper/command extraído), desde que o efeito observável seja idêntico.
- Ícone e posição exata do botão "Baixar PDF oficial" no rodapé, desde que distinto de "Baixar PDF assinado".
- Mecânica de download do blob no front (`<a download>` temporário vs lib utilitária existente) — reusar padrão já presente na base se houver.
- Mensagem exata do 422 de Rascunho (genérica, em PT-BR).

**Reuso obrigatório (grep antes de criar)**:
- `IReceitaPdfService` / `QuestPdfReceitaService` (estender, não recriar).
- `IProntuarioAcessoLogService` + `TipoAcessoProntuario.Exportacao` (padrão de `RegistrarExportacaoEvolucaoCommandHandler`).
- `IProntuarioRepository.ObterPorPaciente` (resolver prontuário do paciente).
- `receitaService.baixarPdf` (front — já existe).
- Padrão de download de blob já usado na base, se existir; caso não exista, criar utilitário mínimo.

**Aciona `imedto-database`**: não — schema não muda; índices de leitura por paciente já existem. Acionar **apenas** se a inspeção do schema revelar ausência de índice para a busca de prontuário por paciente (improvável).

## 10. Atualização de documentação

- **`Docs/LGPD.md`** — adicionar, na seção de audit trail/exportação, uma linha curta registrando que **exportação de PDF de receita** também gera audit de `Exportacao` em `prontuario_acesso_log` (junto com prontuário completo e evolução individual, que já constam). Mudança incremental, cirúrgica.
- **`Docs/ARQUITETURA.md`** — sem alteração estrutural obrigatória. O padrão de geração de PDF server-side (QuestPDF) já existe no código; **se** o dev formalizar o "ponto de extensão para servir arquivo assinado", adicionar uma nota de uma linha na seção de leitura/serviços sobre o contrato `IReceitaPdfService` servir o arquivo assinado quando houver assinatura ativa. Decisão do dev no momento; o QA valida que, se o padrão foi formalizado, o doc reflete.
- **`Docs/DESIGN.md`** — sem alteração (reusa AppButton e padrão de ação existente; nenhum componente novo de design system).
- **`Docs/INFRA.md` / `Docs/COMANDOS.md`** — sem alteração (sem recurso novo de infra, sem script/migration novo).

## 11. Critério de pronto (Definition of Done)

- Todos os CA1–CA14 verdes (validados por QA via análise de código + suíte automatizada; validação visual do PDF fica para o usuário em produção, conforme limitação de browser no sandbox).
- Suíte de testes do backend cobre: Rascunho → 422 (CA2), audit inserido para emitida com prontuário (CA4), ausência de audit sem prontuário (CA5), multi-tenant (CA7).
- Front: botão "Baixar PDF oficial" presente em receita `Emitida`, com estado de loading, sem regredir "Imprimir" nem "Baixar PDF assinado".
- `Docs/LGPD.md` atualizado.
- Sem mudança de schema; sem migration nova.
